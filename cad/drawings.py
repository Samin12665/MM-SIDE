"""PHASE 18 drawings: orthographic SVG views of critical custom parts (hidden lines shown) plus a
generated drawing package (docs/drawings/DRAWINGS.md) whose dimension/tolerance tables are pulled
from CAR_MASTER, so drawings never disagree with the model."""
import math, os, sys
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE); sys.path.insert(0, os.path.join(HERE, "parts"))
import cadquery as cq
from lib import M
import chassis, front, rear, electronics, sensors

OUT = os.path.join(HERE, "..", "docs", "drawings")
VIEWS = {"top": (0, 0, 1), "front": (1, 0, 0), "side": (0, -1, 0), "iso": (1, -1, 0.8)}


def svg(shape, name, view):
    d = VIEWS[view]
    opt = dict(width=520, height=380, marginLeft=20, marginTop=20, showAxes=False, projectionDir=d,
               strokeWidth=0.35, strokeColor=(20, 20, 20), hiddenColor=(150, 150, 150), showHidden=view != "iso")
    fn = f"{name}_{view}.svg"
    cq.exporters.export(shape, os.path.join(OUT, fn), opt=opt)
    return fn


def fits():
    b = M.bearingFitAllowance
    return [
        ("Bearing seat 685ZZ (Ø11)", f"Ø{11 + b:.2f} printed, ream to Ø11.00 +0.02/-0.00 for light press", "knuckles"),
        ("Bearing seat 686ZZ (Ø13)", f"Ø{13 + b:.2f} printed, ream to Ø13.00 +0.02/-0.00", "rear trailing arms"),
        ("Bearing seat MR84ZZ (Ø8)", f"Ø{8 + b:.2f} printed, ream to Ø8.00 +0.02/-0.00", "bellcranks"),
        ("Hinge pin Ø3", "Ø3.0 in bulkhead blocks (tight: drill 3.0 after print), Ø3.2 in arm tube (running)", "front lower arms"),
        ("Shoulder pivots Ø4", "Ø4.05 +0.05/-0.00 (drill 4.0, ream 4.05)", "drag link, servo link"),
        ("M3 clearance", f"Ø{M.boltClearance}", "all"),
        ("M3 heat-set insert pocket", f"Ø{M.insertDiameter} x {M.insertDepth} deep (insert OD 4.6–4.7 x 5.7)", "all"),
        ("M5 heat-set insert pocket", f"Ø{M.insertDiameterM5} x 9.5 deep", "tub rear-arm pivots"),
        ("Gearbox bore 37D", "Ø37.4 (slip) + pinch bolt; spigot Ø12.4", "rear trailing arms"),
        ("Default printed tolerance", "±0.2 mm on features ≤ 50 mm, ±0.4 mm > 50 mm; flatness of mating faces 0.3 mm", "all"),
    ]


SHEETS = [
    # (id, title, shape-fn, material, key dims (label, value), notes)
    ("DWG-21", "21/22_FRONT_KNUCKLE (L shown, R mirrored)", lambda: front.knuckle_left(), "PA-CF",
     [("Bearing housing", "Ø16 x 12 (Y 102–114), 2x 685ZZ seats, Ø6.2 through"),
      ("Kingpin line (ball centres)", f"Y {M.kingpinY:.1f}; lower Z {M.kingpinLowerZ:.1f}, upper Z {M.kingpinUpperZ:.1f}; caster {M.casterAngle}°"),
      ("Lower ball stud boss", "Ø10, face 5.0 above ball; M3x4 short insert"),
      ("Upper ball stud boss", "Ø10 x 4 (Z 49–53), face 7.0 below ball; M3x4 short insert"),
      ("Steering arm", f"length {M.steeringArmLength} mm @ {M.steeringArmAngle:.2f}° outboard; ball hangs to Z {M.steeringArmZ:.1f}; M3x4 insert"),
      ("Encoder tab", "2x Ø3.4 (M2 self-tap), 5 mm apart, 10 mm behind axle")],
     ["Axle: M5x35 DIN 912 rotating in bearings; bolt head inboard carries the AS5600 magnet cap.",
      "Scrub radius 12 mm; KPI 0°; caster built into part (rotated 5° about axle centre)."]),
    ("DWG-23", "23_FRONT_LOWER_ARM (L shown)", lambda: front.lower_arm_left(), "PA-CF",
     [("Hinge axis", f"Y {M.frontLowerInnerY}, Z {M.frontLowerInnerZ}; tube Ø8.6, Ø3.2 bore, length 31 between blocks"),
      ("Arm length (hinge → ball)", f"{M.frontLowerArmLength:.1f} mm"),
      ("Ball-end insert", "M3 insert along Y at ball Y − 10 (ball end + jam nut)"),
      ("Shock mount", f"M3 insert at X {M.frontShockLowerX}, Y {M.frontShockLowerY}, ball Z {M.frontShockLowerZ}"),
      ("Plate", "Z 22.3–30.5 (8.2 thick)")],
     ["Print flat; hinge bore drilled Ø3.2 through after printing to stay coaxial."]),
    ("DWG-31", "31_REAR_TRAILING_ARM (L shown)", lambda: rear.trailing_arm_left(), "PA-CF",
     [("Pivot", f"X {M.rearArmPivotX}, Z {M.rearArmPivotZ}; boss Ø20 x 21.5; 2x 686ZZ seats Ø13.1 x 5.2"),
      ("Arm length pivot → axle", f"{M.rearArmLength:.1f} mm"),
      ("Motor cup", "gearbox bore Ø37.4, face plate 6 mm, spigot Ø12.4, 6x Ø3.4 on Ø31 PCD @30°+60°n, c'bore Ø6.2 x 2.5"),
      ("Shock mount", f"M3 insert, ball at X {M.rearShockLowerX}, Z {M.rearShockLowerZ}, Y {M.rearShockY}"),
      ("Pinch clamp", "slit 2 mm, M3x16 + nyloc")],
     ["Pivot bolt: ISO 7379 Ø6x25 (M5) into tub M5 insert; 1.5 mm washer under head, 2 mm thrust washer at chassis."]),
    ("DWG-32", "32_REAR_HEX_ADAPTER", lambda: rear.hex_adapter_left(), "PA-CF",
     [("Flange", "Ø22 x 4, 4x Ø3.4 on Ø19.05 PCD @45° (Pololu #1999, VERIFY)"),
      ("Hex", "12.0 A/F x 6"), ("Shaft relief", "Ø6.3 x 5"), ("Wheel screw", "M3x4 short insert on axis")], []),
    ("DWG-42", "42_BELLCRANK (L shown)", lambda: front.bellcrank(+1), "PA-CF",
     [("Pivot", f"X {M.crankPivotX}, Y ±{M.crankPivotY}, 2x MR84ZZ, ISO 7379 Ø4x12"),
      ("Tie-rod arm", f"R {M.crankRadius} @ {M.crankBeta}° (left), ball Z {M.crankPivotZ}"),
      ("Drag arm", f"R {M.crankDragRadius} @ 180° (both cranks, parallelogram), Z 40–43"),
      ("Crank travel", "±31.0° for 26° inner lock")],
     ["Set crank height with shims so the tie-rod ball is at Z 36.5 ± 0.2 (bump-steer critical)."]),
    ("DWG-20", "20_FRONT_BULKHEAD", lambda: front.bulkhead(), "PA-CF",
     [("Hinge blocks", f"X {front.HINGE_X[0]}/{front.HINGE_X[1]} ± 4, Y ±{M.frontLowerInnerY}, pin Z {M.frontLowerInnerZ}, Ø3.0"),
      ("Bellcrank posts", f"Ø10 at X {M.crankPivotX}, Y ±{M.crankPivotY}, M3 insert"),
      ("Servo pillars", "tab faces Z 76; inserts 49.5 x 10 pattern; case pocket 41.5 x 23"),
      ("Tub flange bolts", "6x Ø3.4 (Y ±55/±20 @ Z 40, ±55 @ Z 62)"),
      ("Tower pads", "4x M3 inserts at X 137.5/145.5, Y ±20")], []),
    ("DWG-24", "24_FRONT_SHOCK_TOWER", lambda: front.shock_tower(), "PA-CF",
     [("Plate", f"X {M.frontTowerX[0]}–{M.frontTowerX[1]}, Y ±90, top Z {M.frontShockUpperZ + 6:.1f}"),
      ("Shock top", f"insert on rear face at Y ±{M.frontShockUpperY}, Z {M.frontShockUpperZ}; ball 12 mm behind face"),
      ("Camber link", f"insert on front face at Y ±{M.frontUpperInnerY}, Z {M.frontUpperInnerZ}"),
      ("Mast inserts", "3x M3 on top edge at Y −30/0/30")], []),
    ("DWG-10", "10_MAIN_CHASSIS (tub)", lambda: chassis.tub(), "PETG-CF",
     [("Envelope", f"{M.chassisLength} x {M.chassisWidth} x {M.tubWallHeight} mm, floor {M.chassisThickness}, walls {M.wallThickness}"),
      ("Battery tunnel", f"X {chassis.TUN_X[0]:.1f}–{chassis.TUN_X[1]:.1f}, Z {M.floorZ + M.chassisThickness:.0f}–{chassis.TUN_Z:.1f}, through both walls"),
      ("Rail stations", "X " + ", ".join(f"{x:.0f}" for x in chassis.RAIL_STATIONS) + f" at Y ±{M.railY}, M3 insert 8 deep"),
      ("Rear-arm pivots", f"M5 insert, X {M.rearArmPivotX}, Z {M.rearArmPivotZ}, boss Ø20"),
      ("Floor bosses", "; ".join(f"({x},{y})" for x, y in chassis.FLOOR_BOSSES))], []),
    ("DWG-12", "12_UPPER_ELECTRONICS_DECK (ARV 20 mm grid)", lambda: chassis.deck(), "PETG",
     [("Plate", f"{M.deckLength} x {M.deckWidth} x {M.deckThickness}, ribs 6 mm on 20 mm grid"),
      ("Grid", "X −62…118 step 20, Y −70…70 step 20, M3 inserts 7 deep at nodes"),
      ("Rail stand-offs", "X " + ", ".join(f"{x:.0f}" for x in chassis.DECK_STATIONS) + f", Y ±{M.railY}, Ø11 x 10, c'bore Ø6.2")], []),
]


def rail_drawing():
    from chassis import RAIL_STATIONS
    xs = sorted(RAIL_STATIONS)
    return ("DWG-110-RAIL", "Aluminium longeron drilling (2 off)",
            [("Stock", f"6063 10x10x1 square tube, length {M.railLength:.0f} ±0.5 (X {M.railXmin:.0f} → {M.railXmax:.0f})"),
             ("Holes", "Ø3.4 through both walls (vertical) at X = " + ", ".join(f"{x:.0f}" for x in xs) +
              f" → from rear end: " + ", ".join(f"{x - M.railXmin:.0f}" for x in xs) + " mm"),
             ("Tolerance", "hole position ±0.3; drill with a printed jig (use the tub as the jig)")])


def main():
    os.makedirs(OUT, exist_ok=True)
    md = ["# ARV-1 Drawing Package\n", "_Generated by `cad/drawings.py` from CAR_MASTER. Dimensions in mm, vehicle frame (X fwd, Y left, Z up)._\n",
          "## General fits and tolerances\n", "| Feature | Specification | Parts |\n|---|---|---|"]
    md += [f"| {a} | {b} | {c} |" for a, b, c in fits()]
    for sid, title, fn, mat, dims, notes in SHEETS:
        shp = fn()
        views = [svg(shp, sid, v) for v in ("top", "front", "side", "iso")]
        md.append(f"\n## {sid} — {title}\n\nMaterial: **{mat}**. Print data: see `bom/print_list.csv`. STEP: `out/step/parts/`.\n")
        md.append("| Top | Front | Side | Iso |\n|---|---|---|---|")
        md.append("| " + " | ".join(f"![]({v})" for v in views) + " |\n")
        md.append("| Feature | Dimension |\n|---|---|")
        md += [f"| {a} | {b} |" for a, b in dims]
        if notes:
            md.append("\nNotes:\n" + "\n".join(f"- {n}" for n in notes))
        print("drawing", sid, flush=True)
    sid, title, dims = rail_drawing()
    md.append(f"\n## {sid} — {title}\n\n| Feature | Dimension |\n|---|---|")
    md += [f"| {a} | {b} |" for a, b in dims]
    open(os.path.join(OUT, "DRAWINGS.md"), "w").write("\n".join(md) + "\n")


if __name__ == "__main__":
    main()
