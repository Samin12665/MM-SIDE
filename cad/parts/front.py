"""20_FRONT_SUSPENSION (bulkhead + shock tower), 21/22 FRONT C-HUB + KNUCKLE (L/R),
23_FRONT_CONTROL_ARMS, 40_STEERING (41 servo mount, 42 linkage: bellcranks, drag link, servo link).

Left-side parts are built in place and mirrored about XZ for the right side.
Knuckle and C-hub kingpin plates carry 5 deg caster (rotated about the axle centre line);
the C-hub hinge boss stays parallel to the lower-arm inner hinge (no binding)."""
import math
import cadquery as cq
from lib import M, box, cyl, insert_hole, mirror_y

XF = M.xFrontAxle
ZA = M.axleHeight
KY = M.kingpinY
CASTER = -M.casterAngle          # rotation about +Y: negative = top rearward
AX0, AX1 = (XF, 0, ZA), (XF, 1, ZA)
RAIL_FRONT_STATIONS = ()
FLOOR = M.bulkheadFloorZ         # (18, 22)
HINGE_Y = M.frontLowerInnerY     # 34
HINGE_Z = M.frontLowerInnerZ     # 26.5
HINGE_X = (XF - M.frontLowerPivotSpan / 2, XF + M.frontLowerPivotSpan / 2)   # 130, 170
BLOCK_L = 8.0
CRANK = (M.crankPivotX, M.crankPivotY, M.crankPivotZ)
CRANK_PLATE_Z = (M.crankPivotZ - 8.0, M.crankPivotZ - 4.5)     # tie-rod arm plate; ball stud neck puts ball 4.5 mm above plate
CRANK_DRAG_Z = (40.0, 43.0)
DRAG_Z = (43.3, 46.3)
SERVO_LINK_Z = (46.6, 49.6)
TOWER_X = M.frontTowerX
TOWER_TOP = M.frontShockUpperZ + 6.0


def caster(shape):
    return shape.rotate(AX0, AX1, CASTER)


def caster_pt(p):
    th = math.radians(CASTER)
    x, z = p[0] - XF, p[2] - ZA
    return (XF + x * math.cos(th) + z * math.sin(th), p[1], ZA - x * math.sin(th) + z * math.cos(th))


def inv_caster_pt(p):
    th = math.radians(-CASTER)
    x, z = p[0] - XF, p[2] - ZA
    return (XF + x * math.cos(th) + z * math.sin(th), p[1], ZA - x * math.sin(th) + z * math.cos(th))


# ------------------------------------------------------------------ steering-arm tip (from kinematics)
def arm_tip():
    a = math.radians(M.steeringArmAngle)
    cas = math.radians(M.casterAngle)
    base = (XF + (ZA - M.kingpinLowerZ) * math.tan(cas) - (M.steeringArmZ - M.kingpinLowerZ) * math.sin(cas) / math.cos(cas),
            KY, M.steeringArmZ)
    return (base[0] + M.steeringArmLength * math.cos(a), KY + M.steeringArmLength * math.sin(a), M.steeringArmZ)


def crank_ball(theta_deg=0.0, side=+1):
    b = math.radians(M.crankBeta * side + theta_deg)
    return (M.crankPivotX + M.crankRadius * math.cos(b), side * M.crankPivotY + M.crankRadius * math.sin(b), M.crankPivotZ)


def crank_drag_pin(theta_deg=0.0, side=+1):
    b = math.radians(M.crankDragBeta + theta_deg)
    return (M.crankPivotX + M.crankDragRadius * math.cos(b), side * M.crankPivotY + M.crankDragRadius * math.sin(b))


# ------------------------------------------------------------------ knuckle (left, built upright then castered)
LOWER_BALL = (XF, KY, M.kingpinLowerZ)          # upright frame; castered positions via caster_pt()
UPPER_BALL = (XF, KY, M.kingpinUpperZ)


def knuckle_left():
    """Ball-joint knuckle: 2x 685ZZ bearings, rotating M5x35 axle bolt, lower + upper M3 ball studs
    on the kingpin line, steering arm with hanging ball stud, AS5600 encoder ring plate."""
    y0, y1 = 102.0, 114.0                                   # bearing housing
    k = cyl(16.0, y1 - y0, (XF, (y0 + y1) / 2, ZA), "Y")
    lb = cyl(10.0, 4.5, (XF, KY, M.kingpinLowerZ + 5.0 + 2.25), "Z").union(
        box(XF - 5, XF + 5, y0 + 2, KY, M.kingpinLowerZ + 5.0, ZA - 6.0))
    ub = cyl(10.0, 4.0, (XF, KY, M.kingpinUpperZ - 7.0 - 2.0), "Z")          # Z 49..53, clear of the hex below
    k = k.union(lb).union(ub)
    # steering arm: plate above the tie-rod ball, ball stud hangs down to steeringArmZ
    tip_u = inv_caster_pt(arm_tip())
    pz0 = tip_u[2] + 5.0
    arm = (cq.Workplane("XY").moveTo(XF + 7, KY - 5).lineTo(tip_u[0], tip_u[1] - 5)
           .threePointArc((tip_u[0] + 5, tip_u[1]), (tip_u[0], tip_u[1] + 5)).lineTo(XF + 7, KY + 5).close()
           .extrude(4.0).translate((0, 0, pz0)))
    k = k.union(arm)
    # AS5600 ring plate (M2 posts) around the axle-bolt head
    # encoder tab behind the axle: 2x M2 holes for 25_ENCODER_BRACKET (AS5600 over the magnet on the bolt head).
    # Behind (not above) the axle so the camber link clears it at full bump.
    tab = box(XF - 14, XF - 6, y0 - 2.5, y0 + 1, ZA - 5, ZA + 5)
    k = k.union(tab)
    for dz in (-2.5, 2.5):
        k = k.cut(cyl(1.7, 10.0, (XF - 10.0, y0, ZA + dz), "Y"))
    # holes / seats (after all unions)
    k = k.cut(cyl(6.2, 30, (XF, 108, ZA), "Y"))
    k = k.cut(cyl(11.0 + M.bearingFitAllowance, 4.2, (XF, y0 + 2.1 - 0.01, ZA), "Y"))
    k = k.cut(cyl(11.0 + M.bearingFitAllowance, 4.2, (XF, y1 - 2.1 + 0.01, ZA), "Y"))
    k = insert_hole(k, (XF, KY, M.kingpinLowerZ + 5.0), "Z", sign=-1, depth=4.0)     # M3x4 short insert
    k = insert_hole(k, (XF, KY, M.kingpinUpperZ - 7.0), "Z", sign=+1, depth=4.0)     # M3x4 short insert, 7 mm ball neck
    k = insert_hole(k, (tip_u[0], tip_u[1], pz0 + 4.0), "Z", depth=4.0)
    k = k.cut(cyl(3.0, 20, (tip_u[0], tip_u[1], pz0), "Z"))
    return caster(k)


def lower_arm_left():
    """Printed A-arm: Ø8.6 hinge tube on an M3 pin between bulkhead blocks; outer end takes a
    purchased M3 ball end (threaded into a Y-axis insert) that captures the knuckle's lower ball."""
    lb = caster_pt(LOWER_BALL)
    tip_y = KY - 10.0                                  # ball-end body length 9 mm + 1 mm jam-nut gap
    z0, z1 = HINGE_Z - 4.2, HINGE_Z + 4.0
    xi0, xi1 = HINGE_X[0] + BLOCK_L / 2 + 0.5, HINGE_X[1] - BLOCK_L / 2 - 0.5
    tube = cyl(8.6, xi1 - xi0, ((xi0 + xi1) / 2, HINGE_Y, HINGE_Z), "X")
    plate = (cq.Workplane("XY").polyline([(xi0, HINGE_Y), (xi1, HINGE_Y), (lb[0] + 7, tip_y - 6), (lb[0] + 6, tip_y),
                                          (lb[0] - 6, tip_y), (M.frontShockLowerX - 4, M.frontShockLowerY + 2),
                                          (M.frontShockLowerX - 4, M.frontShockLowerY - 8), (xi0, HINGE_Y + 8)]).close()
             .extrude(z1 - z0).translate((0, 0, z0)))
    arm = plate.union(tube)
    arm = arm.union(cyl(12.0, z1 - z0, (M.frontShockLowerX, M.frontShockLowerY, (z0 + z1) / 2), "Z"))
    arm = insert_hole(arm, (M.frontShockLowerX, M.frontShockLowerY, z1), "Z")
    arm = insert_hole(arm, (lb[0], tip_y, lb[2]), "Y")
    arm = arm.cut(cyl(3.2, 60, ((HINGE_X[0] + HINGE_X[1]) / 2, HINGE_Y, HINGE_Z), "X"))
    tri = (cq.Workplane("XY").polyline([(xi0 + 7, HINGE_Y + 8), (xi1 - 7, HINGE_Y + 8), (lb[0], tip_y - 20)]).close()
           .extrude(12).translate((0, 0, z0 - 1)))
    return arm.cut(tri)


# ------------------------------------------------------------------ bulkhead
def bulkhead():
    x0, x1 = M.chassisXmax, 200.0
    hw = 62.0
    b = box(x0, x1, -hw, hw, FLOOR[0], FLOOR[1]).edges("|Z").fillet(6)
    # rear flange to tub front wall (+ rail seat)
    b = b.union(box(x0, x0 + 4, -hw, hw, FLOOR[0], 72.0))
    from chassis import BULKHEAD_BOLTS
    for y, z in BULKHEAD_BOLTS:
        b = b.cut(cyl(M.boltClearance, 20, (x0, y, z), "X"))
    # cut the flange so it doesn't block the lower-arm hinge tubes / tunnel for wiring
    b = b.cut(box(x0 - 1, x0 + 5, -15, 15, 30, 55))
    for s in (1, -1):
        # lower-arm hinge blocks
        for hx in HINGE_X:
            blk = box(hx - BLOCK_L / 2, hx + BLOCK_L / 2, s * HINGE_Y - 5.5, s * HINGE_Y + 5.5, FLOOR[1] - 0.01, HINGE_Z + 5.0)
            b = b.union(blk)
        b = b.cut(cyl(3.0, 60, (XF, s * HINGE_Y, HINGE_Z), "X"))           # M3 hinge pin, tight
        # bellcrank posts (M3 insert, shoulder screw from top)
        b = b.union(cyl(10.0, CRANK_PLATE_Z[0] - 0.5 - FLOOR[1], (CRANK[0], s * CRANK[1], (CRANK_PLATE_Z[0] - 0.5 + FLOOR[1]) / 2), "Z"))
        b = insert_hole(b, (CRANK[0], s * CRANK[1], CRANK_PLATE_Z[0] - 0.5), "Z", depth=8)
        # front shock-tower legs: 20 mm wide pads, 2 inserts each
        b = b.union(box(TOWER_X[0] - 5, TOWER_X[1] + 5, s * 10 if s > 0 else -29, s * 29 if s > 0 else -10, FLOOR[1] - 0.01, 30.0))
        for lx in (TOWER_X[0] - 1.5, TOWER_X[1] + 1.5):
            b = insert_hole(b, (lx, s * 20.0, 30.0), "Z")
    # servo cradle pillars (41_SERVO_MOUNT is integral: tab faces at servo tab underside)
    tab_z = M.servoOutputZ + 4.0 + 26.0
    for yc in (-34.85, 14.65):
        y0, y1 = (yc - 5, yc + 5)
        b = b.union(box(M.servoX - 11, M.servoX + 11, y0, y1, FLOOR[1] - 0.01, tab_z))
        for dx in (-5.0, 5.0):
            b = insert_hole(b, (M.servoX + dx, yc, tab_z), "Z")
    b = b.cut(box(M.servoX - 11.5, M.servoX + 11.5, -30.8, 10.7, FLOOR[1] + 20, tab_z + 1))   # case clearance
    # bumper & sensor mast inserts on nose face
    # bumper mounts: M3 inserts in the servo-pillar front faces (X 219)
    for yc in (-34.85, 14.65):
        b = insert_hole(b, (M.servoX + 11, yc, 40.0), "X")
    # floor relief under the lower arms (droop sweep)
    for s_ in (1, -1):
        b = b.cut(box(119.5, HINGE_X[1] - BLOCK_L / 2 - 0.5,
                      40 if s_ > 0 else -70, 70 if s_ > 0 else -40, FLOOR[0] - 1, FLOOR[1] + 1))
        b = b.cut(box(HINGE_X[0] + BLOCK_L / 2 + 0.5, HINGE_X[1] - BLOCK_L / 2 - 0.5,
                      35 if s_ > 0 else -41, 41 if s_ > 0 else -35, FLOOR[0] - 1, FLOOR[1] + 1))
    # skid-bolt/weight relief holes
    for (x, y) in ((140, 0), (160, 0)):
        b = b.cut(cyl(14, 10, (x, y, 20), "Z"))
    return b


def shock_tower():
    """24_FRONT_SHOCK_TOWER: plate X 139..144 spanning Y +/-90 on two gusseted 19 mm legs (4x M3 into the
    bulkhead). Shock studs point rearward (12 mm stand-off), camber-link studs point forward."""
    t0, t1 = TOWER_X
    t = box(t0, t1, -90, 90, 50.0, TOWER_TOP).edges("|X").fillet(6)
    t = t.union(box(t0, t1, -29, 29, 40.0, 52.0))
    for s_ in (1, -1):
        y0, y1 = (10.0, 29.0) if s_ > 0 else (-29.0, -10.0)
        t = t.union(box(t0 - 5, t1 + 5, y0, y1, 30.0, 34.0))                       # foot
        t = t.union(box(t0, t1, y0, y1, 30.0, 52.0))                                # leg
        for gx, sg in ((t0, -1), (t1, 1)):                                          # fore/aft gussets
            g = (cq.Workplane("XZ").polyline([(gx, 34.0), (gx + sg * 5, 34.0), (gx, 50.0)]).close()
                 .extrude(-(y1 - y0)).translate((0, y0, 0)))
            t = t.union(g)
        for lx in (t0 - 1.5, t1 + 1.5):
            t = t.cut(cyl(M.boltClearance, 30, (lx, s_ * 20.0, 32.0), "Z"))
        t = t.union(cyl(9.0, 3.0, (t0 - 1.5, s_ * M.frontShockUpperY, M.frontShockUpperZ), "X"))
        t = insert_hole(t, (t0 - 3.0, s_ * M.frontShockUpperY, M.frontShockUpperZ), "X", sign=-1)
        t = t.union(cyl(10, 3, (t1 + 1.5, s_ * M.frontUpperInnerY, M.frontUpperInnerZ), "X"))
        t = insert_hole(t, (t1 + 3, s_ * M.frontUpperInnerY, M.frontUpperInnerZ), "X")
    for y in (-30.0, 0.0, 30.0):
        t = insert_hole(t, ((t0 + t1) / 2, y, TOWER_TOP), "Z")
    t = t.cut(box(t0 - 1, t1 + 1, -8, 8, 56, TOWER_TOP - 14))
    for y in (-44.0, 44.0):
        t = t.cut(box(t0 - 1, t1 + 1, y - 8, y + 8, 80, TOWER_TOP - 8))
    return t


# ------------------------------------------------------------------ steering linkage
def bellcrank(side=+1, theta=0.0):
    cx, cy, cz = CRANK[0], side * CRANK[1], CRANK[2]
    hub = cyl(10.0, CRANK_DRAG_Z[1] - CRANK_PLATE_Z[0], (cx, cy, (CRANK_DRAG_Z[1] + CRANK_PLATE_Z[0]) / 2), "Z")
    bx, by, _ = crank_ball(theta, side)
    ang = math.atan2(by - cy, bx - cx)
    tie = (cq.Workplane("XY").center(0, 0).slot2D(M.crankRadius + 8, 8, math.degrees(ang))
           .extrude(CRANK_PLATE_Z[1] - CRANK_PLATE_Z[0])
           .translate(((cx + bx) / 2, (cy + by) / 2, CRANK_PLATE_Z[0])))
    dx, dy = crank_drag_pin(theta, side)
    angd = math.atan2(dy - cy, dx - cx)
    drag = (cq.Workplane("XY").slot2D(M.crankDragRadius + 8, 8, math.degrees(angd))
            .extrude(CRANK_DRAG_Z[1] - CRANK_DRAG_Z[0]).translate(((cx + dx) / 2, (cy + dy) / 2, CRANK_DRAG_Z[0])))
    c = hub.union(tie).union(drag)
    c = c.cut(cyl(8.0 + M.bearingFitAllowance, 3.1, (cx, cy, CRANK_PLATE_Z[0] + 1.55 - 0.01), "Z"))   # MR84 4x8x3
    c = c.cut(cyl(8.0 + M.bearingFitAllowance, 3.1, (cx, cy, CRANK_DRAG_Z[1] - 1.55 + 0.01), "Z"))
    c = c.cut(cyl(5.0, 40, (cx, cy, cz), "Z"))
    c = insert_hole(c, (bx, by, CRANK_PLATE_Z[1]), "Z", depth=CRANK_PLATE_Z[1] - CRANK_PLATE_Z[0])   # ball stud
    c = insert_hole(c, (dx, dy, CRANK_DRAG_Z[0]), "Z", sign=-1, depth=3.0)
    c = c.cut(cyl(M.boltClearance, 10, (dx, dy, CRANK_DRAG_Z[0]), "Z"))
    return c


def drag_link(theta=0.0):
    pl = crank_drag_pin(theta, +1)
    pr = crank_drag_pin(theta, -1)
    mid = ((pl[0] + pr[0]) / 2, (pl[1] + pr[1]) / 2)
    d = (cq.Workplane("XY").polyline([(pl[0], pl[1] + 5), (pr[0], pr[1] - 5), (pr[0] + 5, pr[1]), (pl[0] + 5, pl[1])]).close()
         .extrude(DRAG_Z[1] - DRAG_Z[0]).translate((0, 0, DRAG_Z[0])))
    d = d.edges("|Z").fillet(2)
    for p in (pl, pr):
        d = d.union(cyl(10, DRAG_Z[1] - DRAG_Z[0], (p[0], p[1], sum(DRAG_Z) / 2), "Z"))
        d = d.cut(cyl(4.05, 10, (p[0], p[1], sum(DRAG_Z) / 2), "Z"))          # 4 mm shoulder
    d = d.union(cyl(10, DRAG_Z[1] - DRAG_Z[0], (mid[0], mid[1], sum(DRAG_Z) / 2), "Z"))
    d = insert_hole(d, (mid[0], mid[1], DRAG_Z[1]), "Z", depth=3.0)
    d = d.cut(cyl(M.boltClearance, 10, (mid[0], mid[1], sum(DRAG_Z) / 2), "Z"))
    return d


def servo_horn_pin(theta=0.0):
    """Horn pin for crank angle theta: drag link translates dy = r_d*(sin(180+th)-sin 180); horn follows."""
    dl = crank_drag_pin(theta, +1)
    dl0 = crank_drag_pin(0.0, +1)
    dy = dl[1] - dl0[1]
    phi = math.asin(max(-1, min(1, dy / M.servoHornRadius)))
    return (M.servoX - M.servoHornRadius * math.cos(phi), M.servoHornRadius * math.sin(phi)), math.degrees(phi)


def servo_link(theta=0.0):
    (hx, hy), _ = servo_horn_pin(theta)
    pl, pr = crank_drag_pin(theta, +1), crank_drag_pin(theta, -1)
    mx, my = (pl[0] + pr[0]) / 2, (pl[1] + pr[1]) / 2
    L = math.hypot(hx - mx, hy - my)
    ang = math.degrees(math.atan2(hy - my, hx - mx))
    s = (cq.Workplane("XY").slot2D(L + 10, 10, ang).extrude(SERVO_LINK_Z[1] - SERVO_LINK_Z[0])
         .translate(((hx + mx) / 2, (hy + my) / 2, SERVO_LINK_Z[0])))
    for p in ((hx, hy), (mx, my)):
        s = s.cut(cyl(4.05, 10, (p[0], p[1], sum(SERVO_LINK_Z) / 2), "Z"))
    return s


def build():
    kl, al = knuckle_left(), lower_arm_left()
    parts = {
        "20_FRONT_BULKHEAD": bulkhead(),
        "24_FRONT_SHOCK_TOWER": shock_tower(),
        "21_FRONT_KNUCKLE_L": kl, "22_FRONT_KNUCKLE_R": mirror_y(kl),
        "23_FRONT_LOWER_ARM_L": al, "23_FRONT_LOWER_ARM_R": mirror_y(al),
        "42_BELLCRANK_L": bellcrank(+1), "42_BELLCRANK_R": bellcrank(-1),
        "42_DRAG_LINK": drag_link(), "42_SERVO_LINK": servo_link(),
    }
    return parts
