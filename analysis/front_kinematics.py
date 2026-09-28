"""
Front suspension + steering kinematics for ARV-1 (SK04 / SK06).

Model
-----
* Suspension is solved in the YZ plane (lower A-arm hinge axes and camber link are
  parallel to X; caster only tilts the kingpin).
* C-hub hinges on the lower-arm outer pin and is located by the upper camber link.
* Knuckle rotates about the kingpin axis (5 deg caster) inside the C-hub.
* Steering: dual bellcranks (vertical pivots) joined by a drag link, so both cranks
  rotate by the same angle. Each crank's tie-rod ball drives the steering-arm ball
  on the knuckle through a fixed-length tie rod.

`optimise()` searches the bellcrank tie-rod ball position and the steering-arm angle
to get high Ackermann and near-zero bump steer; the chosen values are written back to
analysis/steering_solution.json, which the steering Part Studio reads.
"""
import json, math, os, sys
import numpy as np
from scipy.optimize import brentq, minimize

sys.path.insert(0, os.path.join(os.path.dirname(__file__), "..", "cad", "master"))
import car_master as M

HERE = os.path.dirname(__file__)

I_L = np.array([M.frontLowerInnerY, M.frontLowerInnerZ])
O_L0 = np.array([M.frontLowerOuterY, M.frontLowerOuterZ])
L_L = np.linalg.norm(O_L0 - I_L)
I_U = np.array([M.frontUpperInnerY, M.frontUpperInnerZ])
O_U0 = np.array([M.frontUpperOuterY, M.upperBallZ])
L_U = np.linalg.norm(O_U0 - I_U)
W0 = np.array([M.frontTrack / 2, M.axleHeight])   # wheel centre (YZ)


def rot2(a):
    c, s = math.cos(a), math.sin(a)
    return np.array([[c, -s], [s, c]])


def corner(travel):
    """Return planar C-hub transform (R, t) for wheel vertical travel (mm, +bump)."""
    def lower_outer(phi):
        return I_L + rot2(phi) @ (O_L0 - I_L)

    def solve_alpha(phi):
        OL = lower_outer(phi)
        f = lambda a: np.linalg.norm(OL + rot2(a) @ (O_U0 - O_L0) - I_U) - L_U
        return _root_near_zero(f, 0.35, 0.05)

    def wheel_z(phi):
        a = solve_alpha(phi)
        return (lower_outer(phi) + rot2(a) @ (W0 - O_L0))[1]

    phi = brentq(lambda p: wheel_z(p) - (W0[1] + travel), -0.5, 0.5)
    a = solve_alpha(phi)
    OL = lower_outer(phi)
    R = rot2(a)
    t = OL - R @ O_L0
    return R, t, math.degrees(a), phi


def to3(R2, t2):
    R = np.eye(3); R[1:, 1:] = R2
    t = np.array([0.0, t2[0], t2[1]])
    return R, t


def axis_rot(axis, ang):
    axis = axis / np.linalg.norm(axis)
    K = np.array([[0, -axis[2], axis[1]], [axis[2], 0, -axis[0]], [-axis[1], axis[0], 0]])
    return np.eye(3) + math.sin(ang) * K + (1 - math.cos(ang)) * K @ K


cas = math.radians(M.casterAngle)
KP_AXIS0 = np.array([-math.sin(cas), 0.0, math.cos(cas)])          # top leans rearward
KP_PT0 = np.array([M.xFrontAxle + (M.axleHeight - M.kingpinLowerZ) * math.tan(cas), M.kingpinY, M.kingpinLowerZ])


def arm_tip0(arm_angle_deg):
    a = math.radians(arm_angle_deg)
    # point on kingpin axis at steering-arm height
    s = (M.steeringArmZ - KP_PT0[2]) / KP_AXIS0[2]
    base = KP_PT0 + s * KP_AXIS0
    return base + np.array([M.steeringArmLength * math.cos(a), M.steeringArmLength * math.sin(a), 0.0])


def crank_ball(p, theta, side=+1):
    """p = dict(bx, by, bz, r, beta). side=+1 left, -1 right (mirror)."""
    B = np.array([p["bx"], side * p["by"], p["bz"]])
    beta = math.radians(p["beta"]) * side
    ang = beta + theta
    return B + np.array([p["r"] * math.cos(ang), p["r"] * math.sin(ang), 0.0])


def mirror(v):
    return np.array([v[0], -v[1], v[2]])


def steer_angle(p, arm_deg, theta, travel, side=+1):
    """Wheel steer angle (deg, +ve = left/CCW) for crank angle theta and travel."""
    R2, t2, camber, _ = corner(travel)
    R, t = to3(R2, t2)
    T0 = arm_tip0(arm_deg)
    P0 = crank_ball(p, 0.0, +1)
    Ltr = np.linalg.norm(T0 - P0)
    # work on left side geometry; for right side mirror the crank ball input
    if side == +1:
        P = crank_ball(p, theta, +1)
    else:
        P = mirror(crank_ball(p, theta, -1))          # right-side ball mirrored to left
    kp_pt = R @ KP_PT0 + t
    kp_ax = R @ KP_AXIS0

    def f(psi):
        Rk = axis_rot(kp_ax, psi)
        T = kp_pt + Rk @ (R @ T0 + t - kp_pt)
        return np.linalg.norm(T - P) - Ltr

    psi = _root_near_zero(f, 0.9)
    d = math.degrees(psi)
    return d if side == +1 else -d


def _root_near_zero(f, lim, step=0.02):
    """Root of f closest to 0 (continuous branch through the ride position)."""
    f0 = f(0.0)
    if abs(f0) < 1e-12:
        return 0.0
    for k in range(1, int(lim / step) + 1):
        for sgn in (+1, -1):
            a, b = sgn * (k - 1) * step, sgn * k * step
            try:
                fa, fb = f(a), f(b)
            except ValueError:
                continue
            if fa * fb <= 0:
                return brentq(f, min(a, b), max(a, b))
    raise ValueError("no root")


def ackermann_ideal(delta_inner_deg):
    L = M.wheelbase
    T = 2 * M.kingpinY
    di = math.radians(delta_inner_deg)
    return math.degrees(math.atan(1.0 / (1.0 / math.tan(di) + T / L)))


def evaluate(p, arm_deg, verbose=False):
    # crank angle giving 28 deg inner lock (left turn: left wheel is inner)
    g = lambda th: steer_angle(p, arm_deg, th, 0) - M.steeringAngle
    th_lock = None
    for k in range(1, 70):
        for sgn in (+1, -1):
            a, b = sgn * (k - 1) * 0.02, sgn * k * 0.02
            try:
                if g(a) * g(b) <= 0:
                    th_lock = brentq(g, min(a, b), max(a, b)); break
            except ValueError:
                pass
        if th_lock is not None:
            break
    if th_lock is None:
        raise ValueError("lock not reachable")
    rows = []
    for frac in np.linspace(0.1, 1.0, 10):
        th = th_lock * frac
        di = steer_angle(p, arm_deg, th, 0, +1)
        do = steer_angle(p, arm_deg, th, 0, -1)
        ideal = ackermann_ideal(di)
        ack_pct = 100 * (di - do) / max(1e-6, (di - ideal))
        rows.append((di, do, ideal, ack_pct))
    bump = [steer_angle(p, arm_deg, 0.0, tr) for tr in np.linspace(-M.droopTravel, M.bumpTravel, 11)]
    bump_range = max(bump) - min(bump)
    if verbose:
        return th_lock, rows, bump
    ack_end = rows[-1][3]
    ack_mid = rows[4][3]
    return (ack_end - 100) ** 2 * 0.02 + (ack_mid - 100) ** 2 * 0.01 + 400 * bump_range ** 2 + 5 * max(0, abs(math.degrees(th_lock)) - 40) ** 2


def optimise():
    # fixed: crank pivot plane and radius chosen for packaging
    base = dict(bx=M.xFrontAxle + 26.0, by=40.0, bz=M.steeringArmZ, r=18.0, beta=-60.0)

    def cost(x):
        p = dict(base)
        p["by"], p["bz"], p["beta"] = x[0], x[1], x[2]
        arm = x[3]
        try:
            return evaluate(p, arm)
        except ValueError:
            return 1e6

    x0 = [M.crankPivotY, M.crankPivotZ, M.crankBeta, M.steeringArmAngle]
    bounds = [(34, 70), (M.steeringArmZ - 10, M.steeringArmZ + 10), (-120, 30), (-20, 22)]
    res = minimize(cost, x0, method="Powell", bounds=bounds, options=dict(maxiter=4000, xtol=1e-3, ftol=1e-6))
    p = dict(base)
    p["by"], p["bz"], p["beta"] = map(float, res.x[:3])
    return p, float(res.x[3])


def master_linkage():
    return dict(bx=M.crankPivotX, by=M.crankPivotY, bz=M.crankPivotZ, r=M.crankRadius, beta=M.crankBeta), M.steeringArmAngle


def main():
    # default: verify the linkage stored in CAR_MASTER; `--optimise` re-runs the search
    p, arm = optimise() if "--optimise" in sys.argv else master_linkage()
    th_lock, rows, bump = evaluate(p, arm, verbose=True)
    T0 = arm_tip0(arm)
    P0 = crank_ball(p, 0, +1)
    sol = dict(crank=p, steeringArmAngle=arm, tieRodLength=float(np.linalg.norm(T0 - P0)),
               crankLockDeg=math.degrees(th_lock), armTip=T0.tolist(), crankBall=P0.tolist(),
               lowerArmLength=float(L_L), upperLinkLength=float(L_U))
    # suspension table
    susp = []
    for tr in np.linspace(-M.droopTravel, M.bumpTravel, 7):
        R2, t2, camber, phi = corner(tr)
        wc = R2 @ W0 + t2
        susp.append(dict(travel=float(tr), camber=float(-camber), trackChangeHalf=float(wc[0] - W0[0]),
                         lowerArmDeg=float(math.degrees(phi))))
    sol["suspension"] = susp
    sol["ackermann"] = [dict(inner=r[0], outer=r[1], ideal_outer=r[2], ackermann_pct=r[3]) for r in rows]
    sol["bumpSteerDeg"] = bump
    with open(os.path.join(HERE, "steering_solution.json"), "w") as f:
        json.dump(sol, f, indent=2)
    print(json.dumps({k: v for k, v in sol.items() if k not in ("suspension", "ackermann", "bumpSteerDeg")}, indent=1))
    print("Ackermann table (inner, outer, ideal outer, %):")
    for r in rows:
        print("  %6.2f %6.2f %6.2f %6.1f" % r)
    print("bump steer over travel (deg):", ["%.2f" % b for b in bump])
    print("suspension:")
    for s in susp:
        print("  travel %+5.1f  camber %+5.2f  half-track change %+5.2f  arm %+5.1f deg" % tuple(s.values()))


if __name__ == "__main__":
    main()
