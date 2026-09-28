"""30_REAR_SUSPENSION (rear shock tower + E-stop panel + bumper hoop), 31_REAR_TRAILING_ARM (L/R),
32_REAR_HEX_ADAPTER, 51_MOTOR_MOUNT (integral motor cup in the trailing arm).

Each trailing arm pivots on an M5 shoulder bolt running in two 685ZZ bearings, threaded into an
M5 insert in the tub side boss. The Pololu 37D gearmotor is face-bolted (6x M3 on Ø31) into a cup
at the rear of the arm, so drive torque reacts inside the arm and never loads the pivot in torsion."""
import math
import cadquery as cq
from lib import M, box, cyl, insert_hole, mirror_y

XR, ZA = M.xRearAxle, M.axleHeight
PX, PZ = M.rearArmPivotX, M.rearArmPivotZ
RAIL_REAR_STATIONS = ()          # tower uses tub stations -80 / -105
FACE_Y = M.motorFaceY                    # 92.5 gearbox face
PLATE = (FACE_Y, FACE_Y + 6.0)           # face plate Y 92.5..98.5
CUP_Y0 = 80.0
TOWER_X = M.rearTowerX
TOWER_TOP = M.rearShockUpperZ + 8.0
HOOP_Z = (86.0, 94.0)


def beam_z(x):
    return PZ + (ZA - PZ) * (x - PX) / (XR - PX)


def trailing_arm_left():
    # pivot boss with 685 bearing seats
    y0, y1 = 79.5, 101.0
    a = cyl(20.0, y1 - y0, (PX, (y0 + y1) / 2, PZ), "Y")
    # beam: lofted box from pivot to the motor cup (height 24 -> 26, width tapers)
    xs = PX - 6
    xe = XR + 17.0
    beam = (cq.Workplane("YZ", origin=(xs, 0, 0)).center((y0 + y1) / 2, beam_z(xs)).rect(y1 - y0, 22)
            .workplane(offset=-(xs - xe)).center((CUP_Y0 + FACE_Y) / 2 - (y0 + y1) / 2, beam_z(xe) - beam_z(xs) + 6)
            .rect(FACE_Y - CUP_Y0, 26).loft())
    a = a.union(beam)
    # motor cup: ring around gearbox + face plate
    cup = cyl(46.0, PLATE[1] - CUP_Y0, (XR, (CUP_Y0 + PLATE[1]) / 2, ZA), "Y")
    a = a.union(cup)
    a = a.cut(cyl(37.4, FACE_Y - CUP_Y0 + 0.01, (XR, (CUP_Y0 + FACE_Y) / 2 - 0.005, ZA), "Y"))   # gearbox bore
    a = a.cut(cyl(12.4, 20, (XR, PLATE[0] + 3, ZA), "Y"))                                    # spigot
    for i in range(6):
        t = math.radians(30 + 60 * i)
        c = (XR + 15.5 * math.cos(t), (PLATE[0] + PLATE[1]) / 2, ZA + 15.5 * math.sin(t))
        a = a.cut(cyl(M.boltClearance, 10, c, "Y"))
        a = a.cut(cyl(6.2, 2.5, (c[0], PLATE[1] - 1.25 + 0.01, c[2]), "Y"))                  # counterbore M3 SHCS
    # clamp slit + pinch bolt so the cup grips the gearbox body (anti-rotation backup)
    a = a.cut(box(XR - 1, XR + 1, CUP_Y0 - 1, FACE_Y - 2, ZA + 18, ZA + 24))
    a = a.union(box(XR - 7, XR + 7, CUP_Y0, CUP_Y0 + 9, ZA + 21, ZA + 29).cut(box(XR - 1, XR + 1, CUP_Y0 - 1, CUP_Y0 + 10, ZA + 20, ZA + 30)))
    a = a.cut(cyl(M.boltClearance, 20, (XR, CUP_Y0 + 4.5, ZA + 25.5), "X"))
    # pivot bores: 2x 686ZZ (6x13x5) on an ISO 7379 Ø6 x 25 shoulder bolt (M5 thread into the tub insert)
    a = a.cut(cyl(7.0, 40, (PX, 90, PZ), "Y"))
    a = a.cut(cyl(13.0 + M.bearingFitAllowance, 5.2, (PX, y0 + 2.6 - 0.01, PZ), "Y"))
    a = a.cut(cyl(13.0 + M.bearingFitAllowance, 5.2, (PX, y1 - 2.6 + 0.01, PZ), "Y"))
    # shock ball-stud boss on top of the beam
    sx = M.rearShockLowerX
    top = beam_z(sx) + 12.0
    a = a.union(cyl(11.0, M.rearShockLowerZ - 4.0 - top + 3.0, (sx, M.rearShockY, (M.rearShockLowerZ - 4.0 + top - 3.0) / 2), "Z"))
    a = insert_hole(a, (sx, M.rearShockY, M.rearShockLowerZ - 4.0), "Z")
    # beam lightening window (vertical, keeps top/bottom flanges)
    a = a.cut(cq.Workplane("XZ").center(PX - 45, beam_z(PX - 45)).slot2D(34, 9).extrude(40, both=True))
    return a


def hex_adapter_left():
    """Printed PA-CF: bolts to the Pololu 6 mm hub (4x M3), drives the 12 mm wheel hex.
    Wheel retained by M3x16 + 3x12 washer into an axial insert."""
    y_hub = FACE_Y + 6.5 + 11.0          # hub outer face (hub sits 0.5 off the plate)
    h = cyl(22.0, 4.0, (XR, y_hub + 2.0, ZA), "Y")
    hexp = (cq.Workplane("XZ").polygon(6, 12.0 / math.cos(math.radians(30))).extrude(-6.0)
            .translate((XR, y_hub + 4.0, ZA)))
    h = h.union(hexp)
    for i in range(4):
        t = math.radians(45 + 90 * i)
        h = h.cut(cyl(M.boltClearance, 10, (XR + 9.525 * math.cos(t), y_hub + 2, ZA + 9.525 * math.sin(t)), "Y"))
    h = h.cut(cyl(6.3, 10.0, (XR, y_hub, ZA), "Y"))                  # gearmotor shaft end clearance
    h = insert_hole(h, (XR, y_hub + 10.0, ZA), "Y", sign=+1, depth=4.0)   # M3x4 short insert, wheel screw
    return h


def tower():
    """Rear shock tower: plate X -83..-78 on the rails, gussets back to rail station -105,
    E-stop shelf on top, bumper hoop arms over the motors."""
    t0, t1 = TOWER_X
    z_rail = M.railZ + M.railSize / 2                  # 82
    t = box(t0, t1, -96, 96, z_rail, TOWER_TOP).edges("|X").fillet(8)
    t = t.cut(box(t0 - 1, t1 + 1, -40, 40, z_rail - 1, z_rail + 26))       # wiring arch to rear bay / deck
    for s in (1, -1):
        # feet on rails (X -80) and gusset to X -105
        t = t.union(box(-110, t1, s * M.railY - 6 if s > 0 else -M.railY - 6, s * M.railY + 6 if s > 0 else -M.railY + 6, z_rail, z_rail + 6))
        g = (cq.Workplane("XZ").polyline([(-110, z_rail), (t0, z_rail), (t0, TOWER_TOP - 10)]).close()
             .extrude(4).translate((0, s * (M.railY + 2) if s > 0 else -M.railY + 2, 0)))
        t = t.union(g)
        for x in (-80.0, -105.0):
            t = t.cut(cyl(M.boltClearance, 30, (x, s * M.railY, z_rail + 3), "Z"))
        # shock stud boss (9 mm stand-off towards -X)
        t = t.union(cyl(10.0, 5.0, (t0 - 2.5, s * M.rearShockY, M.rearShockUpperZ), "X"))
        t = insert_hole(t, (t0 - 5.0, s * M.rearShockY, M.rearShockUpperZ), "X", sign=-1)
    # E-stop shelf (22.3 mm hole) with guard lips
    shelf = box(-116, t0, -32, 32, TOWER_TOP - 4, TOWER_TOP)
    shelf = shelf.cut(cyl(22.4, 10, (-100, 0, TOWER_TOP - 2), "Z"))
    t = t.union(shelf)
    # bumper hoop arms (over the motors, clear of full-bump motor top Z 79)
    for s in (1, -1):
        arm = box(-226, t0, s * 50 - 5, s * 50 + 5, HOOP_Z[0], HOOP_Z[1])
        t = t.union(arm)
        t = insert_hole(t, (-226.0, s * 50.0, (HOOP_Z[0] + HOOP_Z[1]) / 2), "X", sign=-1)
    return t


def build():
    al = trailing_arm_left()
    hx = hex_adapter_left()
    return {
        "31_REAR_TRAILING_ARM_L": al, "31_REAR_TRAILING_ARM_R": mirror_y(al),
        "32_REAR_HEX_ADAPTER_L": hx, "32_REAR_HEX_ADAPTER_R": mirror_y(hx),
        "30_REAR_SHOCK_TOWER": tower(),
    }
