"""70_COMPUTE_MOUNT, 71_MOTOR_CONTROLLER_MOUNT, 72_POWER_MODULE_MOUNT, 73_FRONT_POWER_TRAY,
74_MCU_IMU_PLATE.  Placement strategy: power electronics live in the lower tub bays (heat, heavy
current, short battery leads), signal/compute on the upper deck.  Every tray bolts to the
ARV interface (floor bosses or 20 mm deck grid) so revisions never touch the chassis."""
import cadquery as cq
from lib import M, box, cyl, insert_hole

ZFT = M.floorZ + M.chassisThickness          # 30
BOSS_TOP = ZFT + 7.0                         # 37
DECK_TOP = M.electronicsDeckHeight + M.deckThickness   # 96
JETSON_X = -14.0
JETSON_Z0 = DECK_TOP + 3.0 + 6.0             # tray 3 mm + 6 mm stand-offs
T = 3.0


def _tray(x0, x1, y0, y1, bolt_pts, z0=BOSS_TOP):
    t = box(x0, x1, y0, y1, z0, z0 + T).edges("|Z").fillet(4)
    t = t.cut(cq.Workplane("XY").pushPoints(bolt_pts).circle(M.boltClearance / 2).extrude(10).translate((0, 0, z0 - 1)))
    return t


def _slotted_standoffs(t, pts, z0, h=6.0, slot=4.0):
    """Stand-offs with slotted M3 holes (+/- slot/2 in X and Y via cross slot) for boards whose
    hole pattern is VERIFY-flagged."""
    t = t.union(cq.Workplane("XY").pushPoints(pts).circle(4.5).extrude(h).translate((0, 0, z0)))
    t = t.cut(cq.Workplane("XY").pushPoints(pts).slot2D(3.4 + slot, 3.4, 0).extrude(h + T + 2).translate((0, 0, z0 - T - 1)))
    t = t.cut(cq.Workplane("XY").pushPoints(pts).slot2D(3.4 + slot, 3.4, 90).extrude(h + T + 2).translate((0, 0, z0 - T - 1)))
    return t


def motor_driver_tray():
    # board 84 (X) x 62 (Y) at X -106..-22, Y 1..63; tray avoids rear-arm pivot gussets (X -46..-18, |Y|>62)
    t = _tray(-110, -18, -6, 60, [(-100, 0), (-40, 0)])
    t = t.union(box(-110, -50, 60, 66, BOSS_TOP, BOSS_TOP + T))
    t = t.cut(cq.Workplane("XY").pushPoints([(-100, 60)]).circle(1.7).extrude(10).translate((0, 0, BOSS_TOP - 1)))
    bx, by = -62.0, 32.0
    pts = [(bx + dx, by + dy) for dx in (-38.0, 38.0) for dy in (-27.0, 27.0)]     # VERIFY MDD10A pattern
    t = _slotted_standoffs(t, pts, BOSS_TOP + T)
    # cable-tie slots for motor leads
    t = t.cut(cq.Workplane("XY").pushPoints([(-24, 10), (-24, 50)]).rect(3, 8).extrude(10).translate((0, 0, BOSS_TOP - 1)))
    return t


def power_tray():
    """DC-DC 19 V for the Jetson (60 x 50) at X -102..-42, Y -62..-12 (clear of pivot gussets)."""
    t = _tray(-110, -42, -64, -8, [(-100, -60), (-70, -25)])
    t = _slotted_standoffs(t, [(-96, -58), (-48, -58), (-96, -16), (-48, -16)], BOSS_TOP + T)
    return t


def front_power_tray():
    """Front bay: fuse block (X 64..104, Y 12..67), servo BEC (X 64..104, Y -40..-15), 5 V buck (X 64..100, Y -70..-44)."""
    t = _tray(60, 104, -70, 70, [(80, 60), (80, -60), (100, 0)])
    t = _slotted_standoffs(t, [(68, 12), (100, 12), (68, 59), (100, 59)], BOSS_TOP + T, h=4)
    t = _slotted_standoffs(t, [(68, -36), (100, -36)], BOSS_TOP + T, h=4)
    t = _slotted_standoffs(t, [(66, -66), (98, -66), (66, -46), (98, -46)], BOSS_TOP + T, h=4)
    return t


def compute_tray():
    """Jetson Orin Nano dev kit: 3 mm plate on the deck grid (6x M3 into grid inserts), 4 slotted
    stand-offs for the carrier (nominal 86 x 58, VERIFY), side cut-outs for airflow to the fan."""
    x0, x1 = JETSON_X - 56, JETSON_X + 56
    grid = [(x, y) for x in (-62.0, -22.0, 18.0) for y in (-50.0, 50.0)]      # deck grid nodes
    t = _tray(x0, x1, -56, 56, grid, z0=DECK_TOP)
    pts = [(JETSON_X + dx, dy) for dx in (-43.0, 43.0) for dy in (-29.0, 29.0)]
    t = _slotted_standoffs(t, pts, DECK_TOP + T)
    t = t.cut(cq.Workplane("XY").pushPoints([(JETSON_X, -28), (JETSON_X, 28)]).slot2D(60, 14).extrude(10).translate((0, 0, DECK_TOP - 1)))
    return t


def mcu_plate():
    """Teensy 4.1 carrier (70x50 perfboard, holes 66x46) + receiver + IMU plinth.
    IMU sits on a solid 8 mm plinth close to the vehicle CG line, isolated from the Jetson fan."""
    grid = [(78.0, 10.0), (118.0, 10.0), (78.0, 50.0), (118.0, 50.0), (58.0, -50.0), (98.0, -50.0)]   # deck grid nodes
    t = _tray(52, 130, -62, 70, grid, z0=DECK_TOP)
    t = t.cut(box(102, 131, -6, 6, DECK_TOP - 1, DECK_TOP + T + 1))          # over deck cable slot (118, 0)
    pcb = [(92 + dx, 42 + dy) for dx in (-33.0, 33.0) for dy in (-23.0, 23.0)]
    t = t.union(cq.Workplane("XY").pushPoints(pcb).circle(3.5).extrude(6).translate((0, 0, DECK_TOP + T)))
    t = t.cut(cq.Workplane("XY").pushPoints(pcb).circle(1.1).extrude(10).translate((0, 0, DECK_TOP + T)))   # M2 self-tap
    # IMU plinth (25.4 x 17.8 board, 4x M2.5 on 20.3 x 12.7, VERIFY)
    t = t.union(box(102, 128, -58, -34, DECK_TOP + T, DECK_TOP + T + 8))
    t = t.cut(cq.Workplane("XY").pushPoints([(115 + dx, -46 + dy) for dx in (-10.15, 10.15) for dy in (-6.35, 6.35)])
              .circle(1.1).extrude(10).translate((0, 0, DECK_TOP + T + 1)))
    # receiver clip (40 x 22) with antenna tube holes
    t = t.union(box(62, 102, -32, -8, DECK_TOP + T, DECK_TOP + T + 2))
    t = t.union(cq.Workplane("XY").pushPoints([(56, -26), (56, -12)]).circle(3).extrude(30).translate((0, 0, DECK_TOP + T)))
    t = t.cut(cq.Workplane("XY").pushPoints([(56, -26), (56, -12)]).circle(1.6).extrude(40).translate((0, 0, DECK_TOP + T)))
    return t


def build():
    return {
        "70_COMPUTE_MOUNT": compute_tray(),
        "71_MOTOR_CONTROLLER_MOUNT": motor_driver_tray(),
        "72_POWER_MODULE_MOUNT": power_tray(),
        "73_FRONT_POWER_TRAY": front_power_tray(),
        "74_MCU_IMU_PLATE": mcu_plate(),
    }
