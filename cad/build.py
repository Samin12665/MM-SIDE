"""
ASM_CAR_COMPLETE builder for ARV-1.

  python3 cad/build.py            -> build all parts, assemblies, STEP/STL exports, BOM volumes
  python3 cad/build.py --check    -> + static interference + steering/suspension motion sweep

Outputs (out/):
  step/ASM_CAR_COMPLETE.step      AP214 assembly (one named body per part, colours)
  step/ASM_*.step                 sub-assemblies (front suspension, rear suspension, steering, ...)
  step/parts/<part>.step          each custom part, vehicle coordinates
  stl/<part>.stl                  each printable part in its PRINT orientation, on Z = 0
  reports/interference.md         interference + motion-study report
  reports/part_volumes.json       volumes -> printed mass in the BOM / calcs
"""
import json, math, os, sys, time
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE); sys.path.insert(0, os.path.join(HERE, "parts"))
sys.path.insert(0, os.path.join(HERE, "..", "analysis"))
import cadquery as cq
from scipy.optimize import brentq
from lib import M, box, cyl, mirror_y
import purchased as P
import chassis, front, rear, electronics, sensors, protection
import front_kinematics as FK

OUT = os.path.join(HERE, "..", "out")

# ---------------------------------------------------------------- materials / print data
PRINT = {
    # name: (material, (rot axis, deg) list for print orientation, qty note)
    "10_MAIN_CHASSIS": ("PETG-CF or ASA", []),
    "11_LOWER_SKID": ("PETG", []),
    "12_UPPER_ELECTRONICS_DECK": ("PETG", [((1, 0, 0), 180)]),
    "61_BATTERY_DOOR_L": ("PETG", [((1, 0, 0), -90)]),
    "61_BATTERY_DOOR_R": ("PETG", [((1, 0, 0), 90)]),
    "20_FRONT_BULKHEAD": ("PA-CF (PETG-CF acceptable)", []),
    "24_FRONT_SHOCK_TOWER": ("PA-CF", [((0, 1, 0), 90)]),
    "21_FRONT_KNUCKLE_L": ("PA-CF", [((1, 0, 0), 90)]),
    "22_FRONT_KNUCKLE_R": ("PA-CF", [((1, 0, 0), -90)]),
    "23_FRONT_LOWER_ARM_L": ("PA-CF", []),
    "23_FRONT_LOWER_ARM_R": ("PA-CF", []),
    "42_BELLCRANK_L": ("PA-CF", []),
    "42_BELLCRANK_R": ("PA-CF", []),
    "42_DRAG_LINK": ("PA-CF", []),
    "42_SERVO_LINK": ("PA-CF", []),
    "31_REAR_TRAILING_ARM_L": ("PA-CF", [((1, 0, 0), 90)]),
    "31_REAR_TRAILING_ARM_R": ("PA-CF", [((1, 0, 0), -90)]),
    "32_REAR_HEX_ADAPTER_L": ("PA-CF", [((1, 0, 0), -90)]),
    "32_REAR_HEX_ADAPTER_R": ("PA-CF", [((1, 0, 0), 90)]),
    "30_REAR_SHOCK_TOWER": ("PA-CF (PETG-CF acceptable)", [((0, 1, 0), 90)]),
    "70_COMPUTE_MOUNT": ("PETG", []),
    "71_MOTOR_CONTROLLER_MOUNT": ("PETG", []),
    "72_POWER_MODULE_MOUNT": ("PETG", []),
    "73_FRONT_POWER_TRAY": ("PETG", []),
    "74_MCU_IMU_PLATE": ("PETG", []),
    "83_SENSOR_MAST": ("PETG-CF", [((0, 1, 0), 90)]),
    "81_DEPTH_SENSOR_MOUNT": ("PETG", [((0, 1, 0), 90)]),
    "80_CAMERA_MOUNT": ("PETG", []),
    "84_LIDAR_MAST": ("PETG-CF", [((1, 0, 0), 180)]),
    "82_LIDAR_MOUNT": ("PETG", []),
    "90_FRONT_BUMPER": ("TPU 95A", []),
    "91_REAR_BUMPER": ("TPU 95A", []),
    "92_BODY_SIDE_SKIRT_L": ("PETG", [((1, 0, 0), -90)]),
    "92_BODY_SIDE_SKIRT_R": ("PETG", [((1, 0, 0), 90)]),
    "100_RAIL_CABLE_CLIP": ("PETG", []),
}
PRINT_FILL = {"PA-CF": 0.85, "PETG-CF": 0.75, "PETG": 0.65, "TPU": 0.80, "ASA": 0.65}   # effective fill (walls + infill)
DENSITY = {"PETG": 1.27, "PETG-CF": 1.30, "PA-CF": 1.15, "TPU": 1.21, "ASA": 1.07}
COLOR = {"PA-CF": (0.15, 0.15, 0.17), "PETG-CF": (0.25, 0.25, 0.28), "PETG": (0.95, 0.45, 0.1),
         "TPU": (0.1, 0.1, 0.1), "ASA": (0.9, 0.9, 0.9)}


def mat_key(m):
    for k in ("PETG-CF", "PA-CF", "TPU", "ASA", "PETG"):
        if m.startswith(k):
            return k
    return "PETG"


# ---------------------------------------------------------------- kinematic state
def crank_theta(frac):
    sol = json.load(open(os.path.join(HERE, "..", "analysis", "steering_solution.json")))
    return frac * sol["crankLockDeg"]


def _linkage():
    return dict(bx=M.crankPivotX, by=M.crankPivotY, bz=M.crankPivotZ, r=M.crankRadius, beta=M.crankBeta)


def front_corner(parts_left, travel, psi_deg):
    """Move left-frame corner parts (dict name->shape). Returns moved dict + hard points."""
    R2, t2, camber, phi = FK.corner(travel)
    a = math.degrees(math.atan2(R2[1, 0], R2[0, 0]))
    out = {}
    hinge0, hinge1 = (0, M.frontLowerInnerY, M.frontLowerInnerZ), (1, M.frontLowerInnerY, M.frontLowerInnerZ)

    def chub_tf(s):
        return s.rotate((0, 0, 0), (1, 0, 0), a).translate((0, float(t2[0]), float(t2[1])))

    kp0 = tuple(FK.KP_PT0); kp1 = tuple(FK.KP_PT0 + FK.KP_AXIS0)
    for n, s in parts_left.items():
        if n.startswith("ARM"):
            out[n] = s.rotate(hinge0, hinge1, math.degrees(phi))
        else:  # knuckle-mounted
            out[n] = chub_tf(s.rotate(kp0, kp1, psi_deg))

    def pt_chub(p):
        v = cq.Vector(*p)
        v = cq.Vector(v.x, v.y * math.cos(math.radians(a)) - v.z * math.sin(math.radians(a)),
                      v.y * math.sin(math.radians(a)) + v.z * math.cos(math.radians(a)))
        return (v.x, v.y + float(t2[0]), v.z + float(t2[1]))

    import numpy as np
    Rk = FK.axis_rot(FK.KP_AXIS0, math.radians(psi_deg))
    tip = np.array(front.arm_tip())
    tip_r = FK.KP_PT0 + Rk @ (tip - FK.KP_PT0)
    hp = {
        "tie_outer": pt_chub(tuple(tip_r)),
        "camber_outer": pt_chub(front.caster_pt(front.UPPER_BALL)),
        "shock_lower": tuple(float(v) for v in (M.frontShockLowerX,
                                                *(FK.I_L + FK.rot2(phi) @ (np.array([M.frontShockLowerY, M.frontShockLowerZ]) - FK.I_L)))),
    }
    return out, hp


def rear_arm_angle(travel):
    PX, PZ = M.rearArmPivotX, M.rearArmPivotZ
    x, z = M.xRearAxle - PX, M.axleHeight - PZ
    f = lambda b: (-x * math.sin(b) + z * math.cos(b)) + PZ - (M.axleHeight + travel)
    return math.degrees(brentq(f, -0.6, 0.6))


def rear_corner(parts_left, travel):
    b = rear_arm_angle(travel)
    ax0, ax1 = (M.rearArmPivotX, 0, M.rearArmPivotZ), (M.rearArmPivotX, 1, M.rearArmPivotZ)
    out = {n: s.rotate(ax0, ax1, b) for n, s in parts_left.items()}
    th = math.radians(b)
    x, z = M.rearShockLowerX - M.rearArmPivotX, M.rearShockLowerZ - M.rearArmPivotZ
    lo = (M.rearArmPivotX + x * math.cos(th) + z * math.sin(th), M.rearShockY, M.rearArmPivotZ - x * math.sin(th) + z * math.cos(th))
    return out, {"shock_lower": lo}


# ---------------------------------------------------------------- build
def build_static():
    parts = {}
    for mod in (chassis, front, rear, electronics, sensors, protection):
        t = time.time()
        parts.update(mod.build())
        print(f"  built {mod.__name__:12s} {time.time() - t:5.1f}s", flush=True)
    return parts


def left_front_moving(parts):
    xf, za = M.xFrontAxle, M.axleHeight
    hub_face = 120.5
    lf = {
        "ARM": parts["23_FRONT_LOWER_ARM_L"],
        "KNUCKLE": parts["21_FRONT_KNUCKLE_L"],
        "WHEEL": front.caster(P.wheel(M.frontTrack / 2, xf, hub_face)) if False else P.wheel(M.frontTrack / 2, xf, hub_face),
        "HEX": cyl(12.0, 6.0, (xf, 117.5, za), "Y").cut(cyl(5.0, 7, (xf, 117.5, za), "Y")),
        "AXLEBOLT": cyl(5.0, 35.0, (xf, 101.5 + 17.5, za), "Y").union(cyl(8.5, 5.0, (xf, 99.0, za), "Y")),
        "BRG1": P.bearing((xf, 104.0, za), "Y"), "BRG2": P.bearing((xf, 112.0, za), "Y"),
    }
    return lf


def left_rear_moving(parts):
    xr, za = M.xRearAxle, M.axleHeight
    return {
        "ARM": parts["31_REAR_TRAILING_ARM_L"],
        "MOTOR": P.gearmotor_left(),
        "HUB": P.pololu_hub_left(),
        "ADAPTER": parts["32_REAR_HEX_ADAPTER_L"],
        "WHEEL": P.wheel(M.rearTrack / 2, xr, 120.5),
    }


def purchased_static(theta=0.0):
    d = {}
    d["110_BATTERY_3S_5000"] = P.battery()
    d["110_JETSON_ORIN_NANO"] = P.jetson(electronics.JETSON_Z0).translate((electronics.JETSON_X + 20.0, 0, 0))
    dx, dz = sensors.depth_sensor_pose()
    d["110_REALSENSE_D435I"] = P.d435(dx, M.depthZ).rotate((sensors.MAST_X[1], 0, sensors.DEPTH_PIVOT_Z), (sensors.MAST_X[1], 1, sensors.DEPTH_PIVOT_Z), M.depthTilt)
    d["110_CAMERA_IMX219"] = P.cam_module(M.cameraX, M.cameraZ).rotate((170.5, 0, sensors.MAST_TOP - 8), (170.5, 1, sensors.MAST_TOP - 8), M.cameraTilt)
    d["110_RPLIDAR_A1"] = P.rplidar(M.lidarX, sensors.LIDAR_BASE_Z)
    d["110_STEERING_SERVO"] = P.servo(-front.servo_horn_pin(theta)[1] if theta else 0.0)
    ez = rear.TOWER_TOP
    d["110_ESTOP_22MM"] = P.estop(-100.0, 0.0, ez)
    d["110_MOTOR_DRIVER_MDD10A"] = P.board(-62, 32, electronics.BOSS_TOP + 3 + 6, 84, 62, 20)
    d["110_DCDC_19V"] = P.board(-72, -37, electronics.BOSS_TOP + 3 + 6, 60, 50, 22)
    d["110_FUSE_BLOCK"] = P.board(84, 35.5, electronics.BOSS_TOP + 3 + 4, 40, 55, 25)
    d["110_BEC_6V"] = P.board(84, -27.5, electronics.BOSS_TOP + 3 + 4, 40, 25, 12)
    d["110_BUCK_5V"] = P.board(82, -56, electronics.BOSS_TOP + 3 + 4, 36, 20, 10)
    d["110_TEENSY_CARRIER"] = P.board(92, 42, electronics.DECK_TOP + 3 + 6, 70, 50, 18)
    d["110_IMU_BNO085"] = P.board(115, -46, electronics.DECK_TOP + 3 + 8, 25.4, 17.8, 4)
    d["110_RC_RECEIVER"] = P.board(82, -20, electronics.DECK_TOP + 3 + 2, 40, 22, 10)
    for i, y in enumerate((0.0, -75.0, 75.0)):
        d[f"110_TOF_VL53L1X_{i}"] = P.board(protection.FB_X[1] - 4.5, y, M.tofZ - 6.5, 3.0, 17.0, 13.0).rotate((0, 0, 0), (0, 0, 1), 0)
    for i, r in enumerate(chassis.rails()):
        d[f"110_AL_RAIL_10x10_{'L' if i == 0 else 'R'}"] = r
    return d


def steering_linkage(theta):
    d = {"42_BELLCRANK_L": front.bellcrank(+1, theta), "42_BELLCRANK_R": front.bellcrank(-1, theta),
         "42_DRAG_LINK": front.drag_link(theta), "42_SERVO_LINK": front.servo_link(theta)}
    return d


def assemble_state(parts, travel_f=(0.0, 0.0), travel_r=(0.0, 0.0), steer_frac=0.0):
    """Return dict name->shape for the complete car at a given state."""
    th = crank_theta(steer_frac)
    p = _linkage()
    psiL = FK.steer_angle(p, M.steeringArmAngle, math.radians(th), travel_f[0], +1)
    psiR = FK.steer_angle(p, M.steeringArmAngle, math.radians(th), travel_f[1], -1)
    shapes = {}
    static_names = [n for n in parts if not any(n.startswith(k) for k in
                    ("21_", "22_", "23_", "31_", "32_", "42_"))]
    for n in static_names:
        shapes[n] = parts[n]
    shapes.update(steering_linkage(th))
    lf = left_front_moving(parts)
    for side, travel, psi in ((+1, travel_f[0], psiL), (-1, travel_f[1], -psiR)):
        mv, hp = front_corner(lf, travel, psi)
        tag = "L" if side > 0 else "R"
        f = (lambda s: s) if side > 0 else mirror_y
        fp = (lambda q: q) if side > 0 else (lambda q: (q[0], -q[1], q[2]))
        names = {"ARM": f"23_FRONT_LOWER_ARM_{tag}",
                 "KNUCKLE": f"{'21' if side > 0 else '22'}_FRONT_KNUCKLE_{tag}", "WHEEL": f"110_WHEEL_F{tag}",
                 "HEX": f"110_HEX12_F{tag}", "AXLEBOLT": f"110_AXLE_M5x35_F{tag}", "BRG1": f"110_BRG_685_F{tag}1",
                 "BRG2": f"110_BRG_685_F{tag}2"}
        for k, s in mv.items():
            shapes[names[k]] = f(s)
        shapes[f"110_SHOCK_F{tag}"] = P.shock(fp(hp["shock_lower"]), fp((M.frontShockUpperX, M.frontShockUpperY, M.frontShockUpperZ)))
        shapes[f"110_CAMBER_LINK_F{tag}"] = P.rod_link(fp(hp["camber_outer"]), fp((M.xFrontAxle, M.frontUpperInnerY, M.frontUpperInnerZ)))
        cb = front.crank_ball(th, side)
        shapes[f"110_TIE_ROD_F{tag}"] = P.rod_link(fp(hp["tie_outer"]), cb)
    lr = left_rear_moving(parts)
    for side, travel in ((+1, travel_r[0]), (-1, travel_r[1])):
        mv, hp = rear_corner(lr, travel)
        tag = "L" if side > 0 else "R"
        f = (lambda s: s) if side > 0 else mirror_y
        fp = (lambda q: q) if side > 0 else (lambda q: (q[0], -q[1], q[2]))
        names = {"ARM": f"31_REAR_TRAILING_ARM_{tag}", "MOTOR": f"110_GEARMOTOR_37D_{tag}", "HUB": f"110_POLOLU_HUB_{tag}",
                 "ADAPTER": f"32_REAR_HEX_ADAPTER_{tag}", "WHEEL": f"110_WHEEL_R{tag}"}
        for k, s in mv.items():
            shapes[names[k]] = f(s)
        shapes[f"110_SHOCK_R{tag}"] = P.shock(fp(hp["shock_lower"]), fp((M.rearShockUpperX, M.rearShockY, M.rearShockUpperZ)))
    shapes.update(purchased_static(th))
    return shapes, dict(psiL=psiL, psiR=psiR, crank=th)


# ---------------------------------------------------------------- interference
ALLOWED = {
    # intentional contact/press fits and fastened stacks (zero-clearance by design)
    frozenset(("10_MAIN_CHASSIS", "11_LOWER_SKID")), frozenset(("10_MAIN_CHASSIS", "20_FRONT_BULKHEAD")),
    frozenset(("100_RAIL_CABLE_CLIP", "110_AL_RAIL_10x10_L")),          # 0.8 mm snap lip, elastic by design
}


def interference(shapes, focus=None, tol=1.0):
    names = list(shapes)
    vals = {n: shapes[n].val() if hasattr(shapes[n], "val") else shapes[n] for n in names}
    bbs = {n: vals[n].BoundingBox() for n in names}
    hits = []
    for i, a in enumerate(names):
        for b in names[i + 1:]:
            if focus and not (a in focus or b in focus):
                continue
            A, B = bbs[a], bbs[b]
            if A.xmax < B.xmin or B.xmax < A.xmin or A.ymax < B.ymin or B.ymax < A.ymin or A.zmax < B.zmin or B.zmax < A.zmin:
                continue
            try:
                v = vals[a].intersect(vals[b]).Volume()
            except Exception:
                v = -1
            if v > tol:
                hits.append((a, b, v))
    return hits


def export_all(parts, shapes):
    os.makedirs(os.path.join(OUT, "step", "parts"), exist_ok=True)
    os.makedirs(os.path.join(OUT, "stl"), exist_ok=True)
    vols = {}
    for n, s in parts.items():
        cq.exporters.export(s, os.path.join(OUT, "step", "parts", f"{n}.step"))
        mat, rots = PRINT.get(n, ("PETG", []))
        ps = s
        for ax, ang in rots:
            ps = ps.rotate((0, 0, 0), ax, ang)
        bb = ps.val().BoundingBox()
        ps = ps.translate((-bb.center.x, -bb.center.y, -bb.zmin))
        cq.exporters.export(ps, os.path.join(OUT, "stl", f"{n}.stl"), tolerance=0.05, angularTolerance=0.1)
        bb = ps.val().BoundingBox()
        c = s.val().Center()
        vols[n] = dict(volume_cm3=s.val().Volume() / 1000, material=mat, centroid=[round(c.x, 1), round(c.y, 1), round(c.z, 1)],
                       print_bbox=[round(bb.xlen, 1), round(bb.ylen, 1), round(bb.zlen, 1)],
                       fits_bed=all(v <= 250 for v in (bb.xlen, bb.ylen, bb.zlen)),
                       mass_g_solid=s.val().Volume() / 1000 * DENSITY[mat_key(mat)],
                       mass_g_printed=s.val().Volume() / 1000 * DENSITY[mat_key(mat)] * PRINT_FILL.get(mat_key(mat), 0.75))
    json.dump(vols, open(os.path.join(OUT, "reports", "part_volumes.json"), "w"), indent=2)
    # assemblies
    groups = {
        "ASM_CHASSIS": ("10_", "11_", "12_", "61_", "110_AL_RAIL", "110_BATTERY"),
        "ASM_FRONT_SUSPENSION": ("20_", "21_", "22_", "23_", "24_", "110_WHEEL_F", "110_HEX12_F", "110_AXLE", "110_BRG", "110_SHOCK_F", "110_CAMBER"),
        "ASM_STEERING": ("42_", "110_STEERING_SERVO", "110_TIE_ROD"),
        "ASM_REAR_SUSPENSION": ("30_", "31_", "110_SHOCK_R", "110_WHEEL_R"),
        "ASM_DRIVETRAIN": ("31_", "32_", "110_GEARMOTOR", "110_POLOLU", "110_WHEEL_R"),
        "ASM_SENSOR_SYSTEM": ("80_", "81_", "82_", "83_", "84_", "110_REALSENSE", "110_CAMERA", "110_RPLIDAR", "110_TOF", "110_IMU"),
        "ASM_ELECTRONICS": ("70_", "71_", "72_", "73_", "74_", "110_JETSON", "110_MOTOR_DRIVER", "110_DCDC", "110_FUSE", "110_BEC", "110_BUCK", "110_TEENSY", "110_RC", "110_ESTOP"),
    }

    def asm(name, sel):
        a = cq.Assembly(name=name)
        for n, s in shapes.items():
            if sel is None or n.startswith(sel):
                mat = PRINT.get(n, ("PURCHASED", []))[0]
                col = COLOR.get(mat_key(mat), (0.6, 0.6, 0.65)) if n in PRINT else (0.55, 0.6, 0.7)
                if n.startswith("110_WHEEL"):
                    col = (0.08, 0.08, 0.08)
                a.add(s, name=n, color=cq.Color(*col))
        a.save(os.path.join(OUT, "step", f"{name}.step"), exportType="STEP")
        return a

    for g, sel in groups.items():
        asm(g, sel)
    return asm("ASM_CAR_COMPLETE", None), vols


def main():
    os.makedirs(os.path.join(OUT, "reports"), exist_ok=True)
    t0 = time.time()
    parts = build_static()
    shapes, st = assemble_state(parts)
    print(f"assembled ride-height state: {len(shapes)} bodies ({time.time() - t0:.0f}s)", flush=True)
    full, vols = export_all(parts, shapes)
    print(f"exported STEP/STL ({time.time() - t0:.0f}s)", flush=True)
    if "--check" in sys.argv:
        rep = ["# Interference and motion study\n", "_Generated by `cad/build.py --check`._\n"]
        hits = [h for h in interference(shapes) if frozenset(h[:2]) not in ALLOWED]
        rep.append("## Static (ride height, straight ahead)\n")
        rep += [f"- **{a} ∩ {b}: {v:.1f} mm³**" for a, b, v in hits] or ["- No interference > 1 mm³."]
        moving_prefix = ("21_", "22_", "23_", "31_", "32_", "42_", "110_WHEEL", "110_TIE", "110_CAMBER", "110_SHOCK",
                         "110_GEARMOTOR", "110_POLOLU", "110_HEX", "110_AXLE", "110_BRG", "110_STEERING_SERVO")
        rep.append("\n## Motion sweep (steering × front travel, rear travel)\n")
        rep.append("| Steer (crank frac) | Front travel L/R | Rear travel L/R | ψL | ψR | Collisions |\n|---|---|---|---:|---:|---|")
        for sf in (-1.0, 0.0, 1.0):
            for tf in ((-M.droopTravel, -M.droopTravel), (M.bumpTravel, M.bumpTravel), (M.bumpTravel, -M.droopTravel)):
                tr = (tf[0], tf[1])
                sh, s2 = assemble_state(parts, tf, tr, sf)
                focus = {n for n in sh if n.startswith(moving_prefix)}
                h = [x for x in interference(sh, focus) if frozenset(x[:2]) not in ALLOWED]
                txt = "; ".join(f"{a}∩{b} {v:.0f} mm³" for a, b, v in h) or "none"
                rep.append(f"| {sf:+.0f} | {tf[0]:+.0f}/{tf[1]:+.0f} | {tr[0]:+.0f}/{tr[1]:+.0f} | {s2['psiL']:+.1f}° | {s2['psiR']:+.1f}° | {txt} |")
                print(rep[-1], flush=True)
        open(os.path.join(OUT, "reports", "interference.md"), "w").write("\n".join(rep) + "\n")
        print("\n".join(rep[:12]))
    print(f"done in {time.time() - t0:.0f}s")


if __name__ == "__main__":
    main()
