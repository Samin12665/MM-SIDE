"""110_PURCHASED_COMPONENT_ENVELOPES -- simplified but dimensionally faithful envelopes,
placed in the vehicle frame at their installed positions. Interface features that other
parts depend on (spigots, shafts, mounting faces) are modelled; cosmetic detail is not."""
import math
import cadquery as cq
from lib import M, box, cyl, mirror_y


def wheel(y_center, x_center, hub_face_y):
    """Tyre+rim envelope: tyre ring Ø85/Ø56 over full width + hub disc at hub_face_y."""
    tyre = (cq.Workplane("XZ").circle(M.wheelDiameter / 2).circle(M.rimDiameter / 2)
            .extrude(M.wheelWidth / 2, both=True).translate((x_center, y_center, M.axleHeight)))
    s = 1 if y_center > 0 else -1
    hub = cyl(M.rimDiameter, 7.0, (x_center, hub_face_y + s * 3.5, M.axleHeight), "Y")
    hub = hub.cut(cyl(12.4, 30, (x_center, hub_face_y, M.axleHeight), "Y"))
    return tyre.union(hub)


def gearmotor_left():
    """Pololu 37D gearmotor, gearbox face at Y = motorFaceY, body inboard, shaft outboard."""
    y0 = M.motorFaceY
    x, z = M.xRearAxle, M.axleHeight
    body = cyl(37.0, 82.0, (x, y0 - 41.0, z), "Y")
    spigot = cyl(12.0, 4.0, (x, y0 + 2.0, z), "Y")
    shaft = cyl(6.0, 22.0, (x, y0 + 11.0, z), "Y")
    return body.union(spigot).union(shaft)


def pololu_hub_left():
    y0 = M.motorFaceY + 6.5       # hub sits 0.5 mm off the 6 mm face plate
    return cyl(22.0, 11.0, (M.xRearAxle, y0 + 5.5, M.axleHeight), "Y").cut(
        cyl(6.0, 12, (M.xRearAxle, y0 + 5.5, M.axleHeight), "Y"))


def shock(p_low, p_up):
    """Oil shock between two ball centres: body+spring Ø16 on the upper 60 %, shaft Ø3 below."""
    a = cq.Vector(*p_low); b = cq.Vector(*p_up)
    L = (b - a).Length
    d = (b - a).normalized()
    body_len = 0.58 * L
    body = cq.Solid.makeCylinder(M.shockBodyDiameter / 2, body_len, b - d * (body_len + 4), d)
    rod = cq.Solid.makeCylinder(1.5, L - body_len - 8, a + d * 4, d)
    eye_a = cq.Solid.makeSphere(3.4, a)
    eye_b = cq.Solid.makeSphere(3.4, b)
    return cq.Workplane().add(body).union(cq.Workplane().add(rod)).union(cq.Workplane().add(eye_a)).union(cq.Workplane().add(eye_b))


def rod_link(p0, p1, rod_d=3.0, ball_d=6.8):
    """Turnbuckle / tie rod with ball ends (ball-end bodies simplified to spheres + Ø6 sleeves)."""
    a = cq.Vector(*p0); b = cq.Vector(*p1)
    d = (b - a).normalized(); L = (b - a).Length
    rod = cq.Solid.makeCylinder(rod_d / 2, L - 2 * 5, a + d * 5, d)
    s0 = cq.Solid.makeCylinder(3.0, 7.0, a + d * 3.0, d)
    s1 = cq.Solid.makeCylinder(3.0, 7.0, b - d * 10.0, d)
    w = cq.Workplane().add(rod)
    for s in (s0, s1, cq.Solid.makeSphere(ball_d / 2, a), cq.Solid.makeSphere(ball_d / 2, b)):
        w = w.union(cq.Workplane().add(s))
    return w


def servo(horn_deg=0.0):
    """Standard servo, mounted inverted: output shaft points DOWN at (servoX, 0), horn below case.
    Only the horn rotates with horn_deg."""
    x, z = M.servoX, M.servoOutputZ
    # case: 40.5 (Y) x 20 (X) x 38.5 (Z); output shaft 10.2 mm from one end
    case = box(x - 10.0, x + 10.0, -30.3, 10.2, z + 4.0, z + 4.0 + 38.5)
    tabs = box(x - 10.0, x + 10.0, -37.3, 17.2, z + 4.0 + 26.0, z + 4.0 + 28.5)
    spline = cyl(6.0, 4.0, (x, 0, z + 2.0), "Z")
    horn = cq.Workplane("XY").slot2D(M.servoHornRadius * 2 + 10, 9).extrude(3).translate((x - M.servoHornRadius / 2, 0, z - 3.0))
    horn = horn.rotate((x, 0, 0), (x, 0, 1), horn_deg)
    return case.union(tabs).union(spline).union(horn)


def battery():
    return box(M.batteryX - M.batteryWidth / 2, M.batteryX + M.batteryWidth / 2,
               -M.batteryLength / 2, M.batteryLength / 2,
               M.floorZ + M.chassisThickness + 0.5, M.floorZ + M.chassisThickness + 0.5 + M.batteryHeight)


def jetson(z0):
    x0 = -20.0
    carrier = box(x0 - 51.5, x0 + 51.5, -45.25, 45.25, z0, z0 + 1.6 + 20.0)
    module = box(x0 - 35, x0 + 35, -22.5, 22.5, z0 + 21.6, z0 + 34.8)
    return carrier.union(module)


def d435(x, z):
    return box(x - 12.5, x + 12.5, -45.0, 45.0, z - 12.5, z + 12.5).edges("|Y").fillet(5)


def cam_module(x, z):
    return box(x - 6, x + 6, -12.5, 12.5, z - 12, z + 12)


def rplidar(x, z_base):
    base = box(x - 48.4, x + 48.4, -35.15, 35.15, z_base, z_base + 30.0)
    head = cyl(70.0, 25.0, (x + 13.0, 0, z_base + 42.5), "Z")
    return base.union(head)


def board(x, y, z, L, W, H):
    return box(x - L / 2, x + L / 2, y - W / 2, y + W / 2, z, z + H)


def estop(x, y, z_panel):
    head = cyl(40.0, 20.0, (x, y, z_panel + 10.0), "Z")
    neck = cyl(22.0, 8.0, (x, y, z_panel - 2.0), "Z")                  # threaded bushing through panel
    block = box(x - 15, x + 15, y - 20, y + 20, z_panel - 36, z_panel - 6)   # contact block
    return head.union(neck).union(block)


def bearing(center, axis, d=5.0, D=11.0, w=4.0):
    return cyl(D, w, center, axis).cut(cyl(d, w + 1, center, axis))
