"""90_FRONT_BUMPER (TPU 95A), 91_REAR_BUMPER (TPU 95A), 92_BODY_SIDE_SKIRTS (PETG),
100_CABLE_MANAGEMENT (rail clip, grid clip).  Bumpers are sacrificial and bolt on with 2 screws
each; ToF sensor pockets are moulded into them so perimeter sensing survives impacts."""
import math
import cadquery as cq
from lib import M, box, cyl, insert_hole

FB_X = (222.0, M.frontBumperX)
RB_X = (M.rearBumperX, -226.0)
BZ = (32.0, 85.0)


def front_bumper():
    x0, x1 = FB_X
    b = box(x0, x1, -115, 115, BZ[0], BZ[1]).edges("|Z").fillet(5)
    b = b.cut(box(x0 + 2, x1 - 2, -110, 110, BZ[0] + 2, BZ[1] - 2))           # hollow TPU box = crush zone
    for y in range(-90, 91, 45):                                                  # webs
        b = b.union(box(x0, x1, y - 1, y + 1, BZ[0], BZ[1]))
    # legs back to bulkhead nose (X 200) at Y +/-40
    for y in (-34.85, 14.65):                                   # to servo-pillar front inserts
        leg = box(M.servoX + 11.0, x0 + 0.01, y - 5, y + 5, 34.0, 46.0)
        leg = leg.cut(cyl(M.boltClearance, 60, (x0, y, 40.0), "X"))
        b = b.union(leg)
        b = b.cut(cyl(6.5, 6, (x1 - 2, y, 40.0), "X"))                # counterbore access through the front skin
    # ToF pockets: centre + two corners (VL53L1X, 13 x 18 board, 6 mm aperture)
    for y in (0.0, -75.0, 75.0):
        b = b.cut(cyl(8.0, 10, (x1 - 1.5, y, M.tofZ), "X"))
        b = b.cut(box(x0 + 2, x1 - 2.5, y - 9.5, y + 9.5, M.tofZ - 7, M.tofZ + 7))
    return b


def rear_bumper():
    x0, x1 = RB_X
    b = box(x0, x1, -115, 115, BZ[0], 95.0).edges("|Z").fillet(5)
    b = b.cut(box(x0 + 2, x1 - 2, -110, 110, BZ[0] + 2, 95.0 - 2))
    for y in range(-90, 91, 45):
        b = b.union(box(x0, x1, y - 1, y + 1, BZ[0], 95.0))
    for s in (1, -1):
        b = b.cut(cyl(M.boltClearance, 30, (x1, s * 50.0, 90.0), "X"))
        b = b.cut(cyl(6.5, 8, (x0 + 4, s * 50.0, 90.0), "X"))                     # counterbore
    b = b.cut(cyl(8.0, 10, (x0 + 1.5, 0, M.tofZ), "X"))
    b = b.cut(box(x0 + 2.5, x1 - 2, -9.5, 9.5, M.tofZ - 7, M.tofZ + 7))
    return b


def side_skirt(side=+1):
    """Deck-edge splash skirt with cable-tie slots; clips over the deck edge, 2x M3 into grid."""
    y0 = side * 80.0
    ya, yb = sorted((y0, y0 + side * 2.5))
    s = box(-76, 114, ya, yb, 84.0, 110.0)
    lip = box(-76, 114, min(y0 - side * 8, y0), max(y0 - side * 8, y0), 96.0, 98.0)
    lip = lip.cut(box(-72, -12, -90, 90, 90, 100))                        # clear LiDAR-mast feet
    s = s.union(lip)
    s = s.cut(cq.Workplane("XZ").pushPoints([(x, 103.0) for x in range(-60, 111, 30)]).rect(4, 8).extrude(20, both=True).translate((0, y0, 0)))
    return s


def rail_clip():
    """Saddle clip over the 10x10 rail top between deck stand-offs, zip-tie slot (print 12)."""
    y, zt = M.railY, M.railZ + M.railSize / 2           # rail top Z 82
    c = box(-5, 5, y - 7.5, y + 7.5, zt, zt + 2.5)
    c = c.union(box(-5, 5, y + 5, y + 7.5, zt - 8, zt)).union(box(-5, 5, y - 7.5, y - 5, zt - 8, zt))
    c = c.union(box(-5, 5, y + 5, y + 7.5, zt - 8, zt - 6.5).translate((0, -0.8, 0)))   # snap lip
    c = c.cut(box(-2, 2, y - 3, y + 3, zt - 1, zt + 4))
    return c.translate((0.0, 0, 0))


def build():
    sk = side_skirt(+1)
    return {
        "90_FRONT_BUMPER": front_bumper(),
        "91_REAR_BUMPER": rear_bumper(),
        "92_BODY_SIDE_SKIRT_L": sk,
        "92_BODY_SIDE_SKIRT_R": sk.mirror("XZ"),
        "100_RAIL_CABLE_CLIP": rail_clip(),
    }
