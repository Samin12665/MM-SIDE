"""80_CAMERA_MOUNT, 81_DEPTH_SENSOR_MOUNT, 82_LIDAR_MOUNT, 83_SENSOR_MAST (front), 84_LIDAR_MAST.

Front mast stands on the front shock tower (the stiffest point near the front axle, short load
path, no deck flex in the camera chain). Sensor adapters are separate so a different camera /
depth sensor / LiDAR only needs a new adapter plate, not a new mast."""
import math
import cadquery as cq
from lib import M, box, cyl, insert_hole

TOWER_TOP = M.frontShockUpperZ + 6.0          # 105.7
TX = sum(M.frontTowerX) / 2                   # 141.5
MAST_X = (151.5, 155.5)
MAST_TOP = 183.0
DECK_TOP = M.electronicsDeckHeight + M.deckThickness
LIDAR_PLATE_Z = (193.0, 197.0)
LIDAR_BASE_Z = 200.0
LIDAR_FRAME_Y = (66.0, 70.0)
DEPTH_PIVOT_Z = M.depthZ - 16.0               # tilt about the bottom-rear edge: plate stays ahead of the mast


def front_mast():
    m = box(TX - 5, MAST_X[1], -40, 40, TOWER_TOP, TOWER_TOP + 4)                       # foot on tower
    m = m.cut(cq.Workplane("XY").pushPoints([(TX, -30), (TX, 0), (TX, 30)]).circle(M.boltClearance / 2).extrude(10).translate((0, 0, TOWER_TOP - 1)))
    m = m.union(box(MAST_X[0], MAST_X[1], -48, 48, TOWER_TOP, MAST_TOP))                   # vertical plate
    for y in (-40.0, 40.0):                                                               # gussets
        g = (cq.Workplane("XZ").polyline([(TX - 5, TOWER_TOP + 4), (MAST_X[0], TOWER_TOP + 4), (MAST_X[0], TOWER_TOP + 40)]).close()
             .extrude(2.0, both=True).translate((0, y, 0)))
        m = m.union(g)
    # depth-sensor adapter slots (vertical adjustment +/-6 mm): 2x M3 inserts rows
    for z in (M.depthZ - 6, M.depthZ + 6):
        m = m.cut(cq.Workplane("YZ").pushPoints([(-30, z), (30, z)]).circle(M.insertDiameter / 2).extrude(-6).translate((MAST_X[1], 0, 0)))
    # visor / sensor guard over camera + depth sensor
    m = m.union(box(MAST_X[0], 192.0, -50, 50, MAST_TOP - 3, MAST_TOP).edges("|Z").fillet(6))
    # camera arm (to 80_CAMERA_MOUNT)
    m = m.cut(cq.Workplane("XY").pushPoints([(165, -8), (165, 8)]).circle(M.insertDiameter / 2).extrude(6).translate((0, 0, MAST_TOP - 6)))
    # cable window
    m = m.cut(box(MAST_X[0] - 1, MAST_X[1] + 1, -12, 12, TOWER_TOP + 10, M.depthZ - 16))
    return m


def depth_mount():
    """81: plate on mast front face, 5 deg down-tilt wedge, 2x M3 @ 45 mm into D435 rear face."""
    x0 = MAST_X[1]
    p = box(x0, x0 + 4, -40, 40, M.depthZ - 16, M.depthZ + 14)
    p = p.cut(cq.Workplane("YZ").pushPoints([(-30, M.depthZ), (30, M.depthZ)]).slot2D(15.4, 3.4, 90).extrude(10).translate((x0 - 1, 0, 0)))
    p = p.cut(cq.Workplane("YZ").pushPoints([(-22.5, M.depthZ), (22.5, M.depthZ)]).circle(1.7).extrude(10).translate((x0 - 1, 0, 0)))
    # lens hood lip below the sensor (stone / bumper-strike guard)
    p = p.union(box(x0, x0 + 30, -46, 46, M.depthZ - 16, M.depthZ - 13))
    return p.rotate((x0, 0, DEPTH_PIVOT_Z), (x0, 1, DEPTH_PIVOT_Z), M.depthTilt)


def depth_sensor_pose():
    x0 = MAST_X[1] + 4.0
    return (x0 + 12.5, M.depthZ)


def camera_mount():
    """80: hangs from the visor; tab holds a 25x24 camera board (M2 on 21 x 12.5), 12 deg down-tilt."""
    c = box(160, 172, -14, 14, MAST_TOP - 6 - 4, MAST_TOP - 6).cut(
        cq.Workplane("XY").pushPoints([(165, -8), (165, 8)]).circle(1.7).extrude(10).translate((0, 0, MAST_TOP - 20)))
    tab = box(169, 172, -14, 14, M.cameraZ - 14, MAST_TOP - 6)
    tab = tab.cut(cq.Workplane("YZ").pushPoints([(dy, M.cameraZ + dz) for dy in (-10.5, 10.5) for dz in (-6.25, 6.25)])
                  .circle(1.1).extrude(10).translate((168, 0, 0)))
    tab = tab.cut(cq.Workplane("YZ").center(0, M.cameraZ).rect(10, 10).extrude(10).translate((168, 0, 0)))   # connector clearance
    c = c.union(tab)
    return c.rotate((170.5, 0, MAST_TOP - 8), (170.5, 1, MAST_TOP - 8), M.cameraTilt)


def lidar_mast():
    fr = cq.Workplane("XY")
    parts = []
    for s in (1, -1):
        y0, y1 = (LIDAR_FRAME_Y if s > 0 else (-LIDAR_FRAME_Y[1], -LIDAR_FRAME_Y[0]))
        f = box(-68, -16, y0, y1, DECK_TOP, LIDAR_PLATE_Z[0])
        f = f.cut(box(-58, -26, y0 - 1, y1 + 1, DECK_TOP + 14, LIDAR_PLATE_Z[0] - 12).edges("|Y").fillet(6))
        foot = box(-70, -14, s * 60 if s > 0 else -76, s * 76 if s > 0 else -60, DECK_TOP, DECK_TOP + 4)
        foot = foot.cut(cq.Workplane("XY").pushPoints([(-62, s * 70.0), (-22, s * 70.0)]).circle(M.boltClearance / 2).extrude(10).translate((0, 0, DECK_TOP - 1)))
        parts.append(f.union(foot))
    top = box(-82, 2, -72, 72, LIDAR_PLATE_Z[0], LIDAR_PLATE_Z[1]).edges("|Z").fillet(8)
    top = top.cut(box(-66, -14, -30, 30, LIDAR_PLATE_Z[0] - 1, LIDAR_PLATE_Z[1] + 1).edges("|Z").fillet(6))
    pts = [(x, y) for x in (-70.0, -50.0, -30.0, -10.0) for y in (-50.0, 50.0)]
    top = top.cut(cq.Workplane("XY").pushPoints(pts).circle(M.insertDiameter / 2).extrude(6).translate((0, 0, LIDAR_PLATE_Z[1] - 6)))
    m = parts[0].union(parts[1]).union(top)
    return m


def lidar_adapter():
    """82: RPLIDAR A1 adapter (slotted, VERIFY 4x M2.5 base pattern); bolts to the mast top grid."""
    a = box(-90, 10, -60, 60, LIDAR_PLATE_Z[1], LIDAR_BASE_Z).edges("|Z").fillet(6)
    a = a.cut(cq.Workplane("XY").pushPoints([(x, y) for x in (-70.0, -50.0, -30.0, -10.0) for y in (-50.0, 50.0)])
              .circle(M.boltClearance / 2).extrude(10).translate((0, 0, LIDAR_PLATE_Z[1] - 1)))
    lx = M.lidarX
    a = a.cut(cq.Workplane("XY").pushPoints([(lx + dx, dy) for dx in (-35.0, 35.0) for dy in (-24.0, 24.0)])
              .slot2D(8, 2.9, 0).extrude(10).translate((0, 0, LIDAR_PLATE_Z[1] - 1)))
    a = a.cut(box(lx - 20, lx + 20, -15, 15, LIDAR_PLATE_Z[1] - 1, LIDAR_BASE_Z + 1))   # cable pass
    return a


def build():
    return {
        "83_SENSOR_MAST": front_mast(),
        "81_DEPTH_SENSOR_MOUNT": depth_mount(),
        "80_CAMERA_MOUNT": camera_mount(),
        "84_LIDAR_MAST": lidar_mast(),
        "82_LIDAR_MOUNT": lidar_adapter(),
    }
