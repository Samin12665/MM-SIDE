"""10_MAIN_CHASSIS, 11_LOWER_SKID, 12_UPPER_ELECTRONICS_DECK, 60_BATTERY_TRAY (integral tunnel),
61_BATTERY_RETAINER (side doors), aluminium longerons.

Structure: the tub (floor + walls + transverse battery-tunnel walls) is closed on top by two
10x10x1 aluminium longerons and the bolted upper deck, giving a torsion box. Suspension loads
enter through the front bulkhead (bolted to the tub front wall + rails) and the rear-arm pivot
bosses on the tub side walls, which are backed by internal gussets."""
import cadquery as cq
from lib import M, box, cyl, insert_hole, grid_points

X0, X1 = M.chassisXmin, M.chassisXmax            # -115, +115
HW = M.chassisWidth / 2                          # 76
ZF = M.floorZ                                    # 27 underside of floor
ZFT = ZF + M.chassisThickness                    # 30 top of floor
T = M.wallThickness
ZTOP = M.railZ - M.railSize / 2                  # 72 : rail seat
TUN_X = (M.batteryX - (M.batteryWidth + 2 * M.batteryClearance) / 2,
         M.batteryX + (M.batteryWidth + 2 * M.batteryClearance) / 2)   # 5 .. 55
TUN_Z = ZFT + M.batteryHeight + M.batteryClearance                          # 68.5
RAIL_STATIONS = (-105.0, -80.0, -60.0, -25.0, 75.0, 105.0)     # M3 bolt stations through rails (X)
DECK_STATIONS = (-60.0, -25.0, 75.0, 105.0)
FLOOR_BOSSES = [(-100, 60), (-100, -60), (-40, 60), (-40, -60), (-100, 0), (-40, 0),
                (-70, -25), (80, 60), (80, -60), (100, 0)]          # skid + tray bolts (M3 inserts)
PIVOT = (M.rearArmPivotX, M.rearArmPivotZ)
BULKHEAD_BOLTS = [(y, z) for y in (-55.0, -20.0, 20.0, 55.0) for z in (40.0,)] + [(-55.0, 62.0), (55.0, 62.0)]


def tub():
    shell = box(X0, X1, -HW, HW, ZF, ZTOP)
    shell = shell.cut(box(X0 + T, X1 - T, -HW + T, HW - T, ZFT, ZTOP + 1))
    # rail seat flanges (inner lip under the longerons)
    for s in (1, -1):
        lip = box(X0, X1, s * (HW - M.railSize) if s > 0 else -HW, HW if s > 0 else -(HW - M.railSize), ZTOP - 5, ZTOP)
        shell = shell.union(lip)
    # battery tunnel walls (transverse), full height; they are the main cross-members
    for x0, x1 in ((TUN_X[0] - T, TUN_X[0]), (TUN_X[1], TUN_X[1] + T)):
        shell = shell.union(box(x0, x1, -HW, HW, ZFT, ZTOP))
    # tunnel openings through side walls
    shell = shell.cut(box(TUN_X[0], TUN_X[1], -HW - 1, HW + 1, ZFT - 0.01, TUN_Z))
    # tunnel ceiling (keeps battery down, carries cable trough above)
    shell = shell.union(box(TUN_X[0] - T, TUN_X[1] + T, -HW, HW, TUN_Z, TUN_Z + 2.5))
    # bay-to-bay cables cross over the tunnel ceiling in the 10 mm gap under the deck
    # floor ribs in rear electronics bay (under trays) and front bay
    for y in (-30.0, 30.0):
        shell = shell.union(box(X0 + T, TUN_X[0] - T, y - 1.2, y + 1.2, ZFT, ZFT + 4))
    shell = shell.union(box(X0 + T, TUN_X[0] - T, -1.2, 1.2, ZFT, ZFT + 4))
    for x in (-80.0, -40.0):
        shell = shell.union(box(x - 1.2, x + 1.2, -HW + T, HW - T, ZFT, ZFT + 4))
    # floor bosses with M3 inserts (from top) for trays; skid bolts come from below into same bosses
    for x, y in FLOOR_BOSSES:
        shell = shell.union(cyl(9.0, 7.0, (x, y, ZFT + 3.5), "Z"))
        shell = insert_hole(shell, (x, y, ZFT + 7.0), "Z")
        shell = shell.cut(cyl(M.boltClearance, 3.2, (x, y, ZF + 1.5), "Z"))   # skid screw from below engages the same through-threaded insert
    # rail/deck clamp bosses on wall tops: M3 inserts from above
    for x in RAIL_STATIONS:
        for s in (1, -1):
            shell = shell.union(box(x - 6, x + 6, s * (HW - 12) - 0 if s > 0 else -HW, HW if s > 0 else -(HW - 12), ZTOP - 12, ZTOP))
            shell = insert_hole(shell, (x, s * M.railY, ZTOP), "Z", depth=8)
    # rear-arm pivot bosses: M5 insert, internal gusset block
    for s in (1, -1):
        px, pz = PIVOT
        shell = shell.union(cyl(20.0, 3.0, (px, s * (HW + 1.5), pz), "Y"))
        shell = shell.union(box(px - 14, px + 14, (HW - 12) if s > 0 else -HW, HW if s > 0 else -(HW - 12), ZFT, pz + 12))
        shell = shell.cut(cyl(M.insertDiameterM5, 12.0, (px, s * (HW + 3 - 6), pz), "Y"))
    # front wall: bulkhead bolts (horizontal M3 inserts from the front)
    for y, z in BULKHEAD_BOLTS:
        shell = shell.union(box(X1 - 10, X1, y - 5, y + 5, z - 5, z + 5))
        shell = insert_hole(shell, (X1, y, z), "X")
    # rear wall: bumper/tower bolts
    for y in (-40.0, 40.0):
        shell = shell.union(box(X0, X0 + 10, y - 5, y + 5, 55 - 5, 55 + 5))
        shell = insert_hole(shell, (X0, y, 55.0), "X", sign=-1)
    # battery door latch bosses (tunnel wall ends) M3 inserts along Y
    for s in (1, -1):
        for x in (TUN_X[0] - T / 2 - 2.5, TUN_X[1] + T / 2 + 2.5):
            shell = shell.union(box(x - 4, x + 4, (HW - 8) if s > 0 else -HW, HW if s > 0 else -(HW - 8), TUN_Z - 6, TUN_Z + 2.5))
            shell = insert_hole(shell, (x, s * HW, TUN_Z - 2.0), "Y", sign=s)
    # ventilation slots in rear-bay side walls (motor driver / DC-DC)
    for s in (1, -1):
        for i in range(4):
            x = -100 + i * 18
            shell = shell.cut(box(x, x + 10, s * HW - 4, s * HW + 4, 40, 58))
    # battery lead grommet (left wall, rear bay) and cable exits to deck
    shell = shell.cut(cyl(12.0, 10, (-8.0, HW, 60.0), "Y"))
    # floor drain / weight holes in front bay
    for x in (70.0, 95.0):
        for y in (-35.0, 35.0):
            shell = shell.cut(cyl(8.0, 10, (x, y, ZF + 1.5), "Z"))
    return shell


def skid():
    """Sacrificial 2 mm skid (PETG print or cut HDPE). Countersunk M3 into floor bosses."""
    s = box(X0 + 2, X1 - 2, -HW + 4, HW - 4, M.groundClearance, M.floorZ)
    s = s.edges("|Z").fillet(10)
    for x, y in FLOOR_BOSSES:
        s = s.cut(cyl(3.4, 10, (x, y, M.floorZ), "Z"))
        s = s.cut(cq.Workplane("XY").circle(3.4).workplane(offset=1.8).circle(1.7).loft()
                  .translate((x, y, M.groundClearance)))
    # lower perimeter edge broken so the skid rides over steps
    s = s.faces("<Z").edges().chamfer(0.8)
    return s


def rails():
    out = []
    for s in (1, -1):
        r = box(M.railXmin, M.railXmax, s * M.railY - 5, s * M.railY + 5, ZTOP, ZTOP + 10)
        r = r.cut(box(M.railXmin - 1, M.railXmax + 1, s * M.railY - 4, s * M.railY + 4, ZTOP + 1, ZTOP + 9))
        from front import RAIL_FRONT_STATIONS
        from rear import RAIL_REAR_STATIONS
        for x in RAIL_STATIONS + RAIL_FRONT_STATIONS + RAIL_REAR_STATIONS:
            r = r.cut(cyl(3.4, 20, (x, s * M.railY, ZTOP + 5), "Z"))
        out.append(r)
    return out


DECK_X = (M.deckXmin, M.deckXmin + M.deckLength)
DECK_Z = (M.electronicsDeckHeight, M.electronicsDeckHeight + M.deckThickness)


def deck():
    """Upper deck: 4 mm plate + 6 mm under-rib grid on the ARV 20 mm pitch.
    Every rib node that is not a rail stand-off carries an M3 heat-set insert (from the top)."""
    x0, x1 = DECK_X
    z0, z1 = DECK_Z
    hw = M.deckWidth / 2
    pts = grid_points(x0, x1, -hw, hw)
    xs = sorted({p[0] for p in pts}); ys = sorted({p[1] for p in pts})
    plate = cq.Workplane("XY").box(x1 - x0, 2 * hw, z1 - z0, centered=False).translate((x0, -hw, z0)).edges("|Z").fillet(8)
    ribs = cq.Workplane("XY")
    for x in xs:
        ribs = ribs.add(cq.Solid.makeBox(2.4, 2 * hw - 12, 6, cq.Vector(x - 1.2, -hw + 6, z0 - 6)))
    for y in ys:
        ribs = ribs.add(cq.Solid.makeBox(x1 - x0 - 12, 2.4, 6, cq.Vector(x0 + 6, y - 1.2, z0 - 6)))
    d = plate.union(cq.Workplane().add(cq.Compound.makeCompound(ribs.vals())).combine(clean=False))
    # clear ribs where the deck sits on the rails
    for s_ in (1, -1):
        d = d.cut(box(x0 - 1, x1 + 1, s_ * M.railY - 6, s_ * M.railY + 6, z0 - 7, z0 - 0.01))
    # rail stand-off bosses (10 mm wiring gap), M3 clearance + counterbore
    st = [(x, s_ * M.railY) for x in DECK_STATIONS for s_ in (1, -1)]
    boss_h = z0 - (ZTOP + 10)
    d = d.union(cq.Workplane("XY").pushPoints(st).circle(5.5).extrude(boss_h).translate((0, 0, ZTOP + 10)))
    d = d.cut(cq.Workplane("XY").pushPoints(st).circle(M.boltClearance / 2).extrude(40).translate((0, 0, ZTOP)))
    d = d.cut(cq.Workplane("XY").pushPoints(st).circle(3.1).extrude(3.5).translate((0, 0, z1 - 3.5)))
    ins = [p for p in pts if not any(abs(p[0] - sx) < 8 and abs(abs(p[1]) - M.railY) < 8 for sx in DECK_STATIONS)]
    d = d.cut(cq.Workplane("XY").pushPoints(ins).circle(M.insertDiameter / 2).extrude(7.0).translate((0, 0, z1 - 7.0)))
    # front corner notches: front shocks pass the deck corners
    for s_ in (1, -1):
        d = d.cut(box(116, x1 + 1, s_ * 70 if s_ > 0 else -hw - 1, hw + 1 if s_ > 0 else -70, z0 - 8, z1 + 1))
    # cable pass-throughs: power (right, -Y) and signal (left, +Y) kept separate
    slots = [(40.0, -50.0), (40.0, 50.0), (-62.0, -50.0), (-62.0, 50.0), (118.0, 0.0)]
    d = d.cut(cq.Workplane("XY").pushPoints(slots).slot2D(30, 10).extrude(20).translate((0, 0, z0 - 10)))
    return d


def battery_door(side=+1):
    """Swing-out retainer: bottom tongue in skid lip, two thumb screws at the top.
    Foam pad (3 mm) on the inside takes up battery length tolerance."""
    y0 = side * HW
    t = 3.0
    ya, yb = (y0, y0 + side * t)
    d = box(TUN_X[0] - T - 5, TUN_X[1] + T + 5, min(ya, yb), max(ya, yb), ZF + 1, TUN_Z + 2.5)
    d = d.edges("|Y").fillet(3)
    for x in (TUN_X[0] - T / 2 - 2.5, TUN_X[1] + T / 2 + 2.5):
        d = d.cut(cyl(M.boltClearance, 10, (x, y0, TUN_Z - 2.0), "Y"))
    # lead slot (XT60 lead exits through the door towards the pigtail)
    d = d.cut(box(M.batteryX - 7, M.batteryX + 7, min(ya, yb) - 1, max(ya, yb) + 1, TUN_Z - 14, TUN_Z - 4))
    # finger pull
    d = d.union(box(M.batteryX - 12, M.batteryX + 12, min(yb, yb + side * 6), max(yb, yb + side * 6), ZF + 4, ZF + 9))
    return d


def build():
    parts = {
        "10_MAIN_CHASSIS": tub(),
        "11_LOWER_SKID": skid(),
        "12_UPPER_ELECTRONICS_DECK": deck(),
        "61_BATTERY_DOOR_L": battery_door(+1),
        "61_BATTERY_DOOR_R": battery_door(-1),
    }
    return parts
