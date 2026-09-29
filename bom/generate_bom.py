"""
PHASE 17 — live BOM for ARV-1.
Printed-part lines are generated from the CAD (volumes, materials, print bounding boxes in
out/reports/part_volumes.json); purchased lines are maintained here. Writes:
  bom/BOM.csv            full BOM grouped by category
  bom/print_list.csv     printable parts with material / orientation / settings
  bom/purchase_list.csv  consolidated shopping list with approximate cost
Costs are approximate street prices (USD, 2026) for planning only.
"""
import csv, json, os, sys
HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.join(HERE, "..")
sys.path.insert(0, os.path.join(ROOT, "cad")); sys.path.insert(0, os.path.join(ROOT, "cad", "parts"))

# ------------------------------------------------------------------ print settings by material/function
SETTINGS = {
    "PA-CF": dict(layer="0.20", walls="5", infill="40% gyroid", top_bottom="6/6", supports="none unless noted",
                  post="dry filament 8 h @ 80 °C before printing; anneal optional 80 °C 4 h; ream bearing seats, hinge holes Ø3.0/3.2 drill"),
    "PETG-CF": dict(layer="0.20", walls="4", infill="30% gyroid", top_bottom="5/5", supports="none",
                    post="deburr; install heat-set inserts at 245 °C"),
    "PETG": dict(layer="0.20", walls="3", infill="20% gyroid", top_bottom="4/4", supports="none",
                 post="install heat-set inserts at 240 °C"),
    "TPU": dict(layer="0.24", walls="3", infill="15% gyroid", top_bottom="3/3", supports="none",
                post="print slow (25 mm/s); no inserts in TPU (screws pass through)"),
    "ASA": dict(layer="0.20", walls="4", infill="30% gyroid", top_bottom="5/5", supports="none", post="enclosure required"),
}
ORIENT_NOTE = {
    "10_MAIN_CHASSIS": "floor on bed (as modelled); brim 5 mm",
    "11_LOWER_SKID": "flat",
    "12_UPPER_ELECTRONICS_DECK": "top face on bed, ribs up",
    "61_BATTERY_DOOR_L": "outer face on bed", "61_BATTERY_DOOR_R": "outer face on bed",
    "20_FRONT_BULKHEAD": "floor on bed; supports only under servo-pillar insert bosses if the slicer flags them",
    "24_FRONT_SHOCK_TOWER": "rear face on bed (plate flat) — layers in the load plane",
    "21_FRONT_KNUCKLE_L": "bearing axis vertical, inboard face on bed", "22_FRONT_KNUCKLE_R": "bearing axis vertical, inboard face on bed",
    "23_FRONT_LOWER_ARM_L": "flat (as modelled)", "23_FRONT_LOWER_ARM_R": "flat (as modelled)",
    "42_BELLCRANK_L": "flat, tie-rod arm down", "42_BELLCRANK_R": "flat, tie-rod arm down",
    "42_DRAG_LINK": "flat", "42_SERVO_LINK": "flat",
    "31_REAR_TRAILING_ARM_L": "inboard face on bed: motor cup + pivot bores vertical, beam layers carry bending",
    "31_REAR_TRAILING_ARM_R": "inboard face on bed",
    "32_REAR_HEX_ADAPTER_L": "flange on bed", "32_REAR_HEX_ADAPTER_R": "flange on bed",
    "30_REAR_SHOCK_TOWER": "front face on bed (plate flat); tree supports under E-stop shelf + hoop arms",
    "70_COMPUTE_MOUNT": "flat", "71_MOTOR_CONTROLLER_MOUNT": "flat", "72_POWER_MODULE_MOUNT": "flat",
    "73_FRONT_POWER_TRAY": "flat", "74_MCU_IMU_PLATE": "flat",
    "83_SENSOR_MAST": "mast plate flat (back on bed)", "81_DEPTH_SENSOR_MOUNT": "mounting face on bed",
    "80_CAMERA_MOUNT": "as modelled (support under arm)", "84_LIDAR_MAST": "top plate on bed, frames up",
    "82_LIDAR_MOUNT": "flat", "90_FRONT_BUMPER": "upright, webs vertical", "91_REAR_BUMPER": "upright, webs vertical",
    "92_BODY_SIDE_SKIRT_L": "outer face on bed", "92_BODY_SIDE_SKIRT_R": "outer face on bed",
    "100_RAIL_CABLE_CLIP": "saddle on bed",
}
QTY = {"100_RAIL_CABLE_CLIP": 12}
INSERTS = {  # M3x5.7 standard / M3x4 short / M5 counts per part (from the CAD insert pockets actually used)
    "10_MAIN_CHASSIS": "38x M3 + 2x M5", "12_UPPER_ELECTRONICS_DECK": "24x M3 (fit only the grid nodes you use; 70 available)",
    "20_FRONT_BULKHEAD": "12x M3", "24_FRONT_SHOCK_TOWER": "7x M3", "21_FRONT_KNUCKLE_L": "3x M3x4 short",
    "22_FRONT_KNUCKLE_R": "3x M3x4 short", "23_FRONT_LOWER_ARM_L": "2x M3", "23_FRONT_LOWER_ARM_R": "2x M3",
    "42_BELLCRANK_L": "2x M3", "42_BELLCRANK_R": "2x M3", "42_DRAG_LINK": "1x M3", "31_REAR_TRAILING_ARM_L": "1x M3",
    "31_REAR_TRAILING_ARM_R": "1x M3", "32_REAR_HEX_ADAPTER_L": "1x M3x4 short", "32_REAR_HEX_ADAPTER_R": "1x M3x4 short",
    "30_REAR_SHOCK_TOWER": "4x M3", "83_SENSOR_MAST": "6x M3", "84_LIDAR_MAST": "8x M3",
}

# ------------------------------------------------------------------ purchased lines
# (category, item, specification, qty, function, sourcing, approx unit USD)
PURCHASED = [
    ("PURCHASED CHASSIS PARTS", "Aluminium square tube", "6063-T5 10x10x1 mm, cut 2x 238 mm, drill 6x Ø3.4 each per drawing DWG-110-RAIL", 2, "Chassis longerons (torsion box top chords)", "Metal supplier / hardware store (1 m length)", 6),
    ("PURCHASED CHASSIS PARTS", "Wheel + tyre, 1/10 buggy/rally", "Ø85 x 35 mm, 12 mm hex, 5 mm bore, 2.2\" rim (VERIFY ID ≥ 54 mm, offset ≥ 0)", 4, "Wheels", "RC hobby shop (e.g. 1/10 rally / 2WD buggy sets)", 7),
    ("SUSPENSION COMPONENTS", "Oil shock absorber 1/10", "80 mm eye-eye extended, ≥ 22 mm stroke, M3 ball-cup ends, springs ~1.0–1.5 N/mm", 4, "Damping + springs (front MR 0.57, rear MR 0.70)", "RC hobby / AliExpress '1/10 80mm shock'", 6),
    ("SUSPENSION COMPONENTS", "Spring set", "Front 1.4 N/mm, rear 1.0 N/mm (≈ 8 lb/in and 5.7 lb/in) to suit shock body", 1, "Ride frequency 3.5 Hz", "RC hobby", 10),
    ("SUSPENSION COMPONENTS", "M3 ball stud 6.8 mm, 4 mm neck", "steel, M3 x 4 thread", 10, "Knuckle upper/lower balls, tower camber balls, steering arms", "RC hobby 'ball stud 6.8mm'", 0.5),
    ("SUSPENSION COMPONENTS", "M3 ball stud 6.8 mm, 7 mm neck", "steel, M3 thread; + 7 mm M3 spacer for front shock tops", 8, "Shock top/bottom mounts", "RC hobby", 0.5),
    ("SUSPENSION COMPONENTS", "M3 ball end 6.8 mm (plastic)", "M3 female thread", 10, "Lower knuckle joints (2) + turnbuckle ends (8)", "RC hobby", 0.4),
    ("SUSPENSION COMPONENTS", "M3 turnbuckle, left/right thread", "~45 mm (camber link 57 mm c-c)", 2, "Adjustable camber links", "RC hobby", 1.5),
    ("SUSPENSION COMPONENTS", "Steel hinge pin", "Ø3 x 50 mm (or M3x50 SHCS + nyloc)", 2, "Front lower-arm hinges", "RC hobby / McMaster", 1),
    ("STEERING COMPONENTS", "Standard digital servo 20 kg·cm", "40.5x20x38.5 mm, 25T spline, 6.0–7.4 V (DS3218 class)", 1, "Steering actuator (required 4.4 kg·cm, SF 4.5)", "Amazon/AliExpress/RC hobby", 18),
    ("STEERING COMPONENTS", "25T aluminium servo horn / servo saver", "18 mm pin radius, M3 hole", 1, "Horn to servo link", "RC hobby", 5),
    ("STEERING COMPONENTS", "M3 turnbuckle, left/right thread", "~60 mm (tie rod 74.35 mm c-c)", 2, "Adjustable tie rods (toe)", "RC hobby", 1.5),
    ("STEERING COMPONENTS", "ISO 7379 shoulder screw", "Ø4 x 12, M3 thread", 2, "Bellcrank pivots", "McMaster/Misumi/AliExpress", 1.2),
    ("STEERING COMPONENTS", "ISO 7379 shoulder screw", "Ø4 x 6, M3 thread", 4, "Drag-link and servo-link pivots", "McMaster/Misumi/AliExpress", 1),
    ("DRIVETRAIN COMPONENTS", "Pololu universal aluminium hub", "6 mm bore, M3 holes (#1999, 2-pack)", 1, "Gearmotor shaft to hex adapter", "Pololu", 8),
    ("DRIVETRAIN COMPONENTS", "12 mm wheel hex", "5 mm bore, 6 mm thick", 2, "Front hubs", "RC hobby", 0.5),
    ("MOTORS", "Pololu 37D metal gearmotor 19:1, 64 CPR encoder, 12 V", "#4751 (VERIFY: 530 rpm, 8.5 kg·cm stall, 5.5 A stall)", 2, "Rear drive, independent L/R, wheel odometry 1216 cnt/rev", "Pololu", 50),
    ("BEARINGS", "685ZZ", "5 x 11 x 4 mm", 4, "Front axle (2 per knuckle)", "Bearing supplier (10-pack)", 0.5),
    ("BEARINGS", "686ZZ", "6 x 13 x 5 mm", 4, "Rear trailing-arm pivots", "Bearing supplier (10-pack)", 0.6),
    ("BEARINGS", "MR84ZZ", "4 x 8 x 3 mm", 4, "Bellcrank pivots", "Bearing supplier (10-pack)", 0.5),
    ("SERVOS", "(see STEERING COMPONENTS)", "", 0, "", "", 0),
    ("BATTERY", "3S LiPo 5000 mAh hard case", "11.1 V, ≤ 140 x 48 x 38 mm, XT60, ≥ 25C", 1, "Main power (≈ 57 min mixed runtime)", "RC hobby", 45),
    ("BATTERY", "LiPo balance charger", "3S balance, ≥ 50 W", 1, "Charging", "RC hobby", 35),
    ("BATTERY", "LiPo voltage alarm / checker", "1–8S", 1, "Low-voltage warning (until firmware monitoring)", "RC hobby", 4),
    ("ELECTRONICS", "NVIDIA Jetson Orin Nano Super Developer Kit", "8 GB, 103 x 90.5 x 34.8 mm", 1, "Main compute (vision, SLAM, planning)", "NVIDIA partners / Arrow / Seeed", 249),
    ("ELECTRONICS", "microSD / NVMe SSD", "NVMe M.2 2280 256 GB", 1, "Jetson storage", "Any", 30),
    ("ELECTRONICS", "Teensy 4.1", "600 MHz, 8x quadrature capable", 1, "Real-time controller: motor PID, encoders, RC input, E-stop logic, IMU", "PJRC / SparkFun", 32),
    ("ELECTRONICS", "Perfboard 70 x 50 mm + headers", "2.54 mm pitch, 4x Ø2 holes 66 x 46", 1, "Teensy carrier", "Any", 3),
    ("ELECTRONICS", "Cytron MDD10A", "dual 10 A, 5–30 V H-bridge (VERIFY hole pattern, slotted tray)", 1, "Drive-motor driver", "Cytron / RobotShop", 25),
    ("ELECTRONICS", "DC-DC buck-boost 19 V", "≥ 60 W, 9–13 V in (VERIFY footprint ≤ 60 x 50)", 1, "Jetson supply", "Amazon/AliExpress (DROK class)", 15),
    ("ELECTRONICS", "5 V 5 A buck", "Pololu D36V50F5 class", 1, "Teensy, sensors, USB hub", "Pololu", 20),
    ("ELECTRONICS", "6 V 5 A BEC", "switching, for servo", 1, "Steering servo supply", "RC hobby", 10),
    ("ELECTRONICS", "SBUS receiver + transmitter", "FrSky/RadioMaster, ≥ 6 ch incl. switch channel", 1, "Manual override + wireless E-stop channel", "RC hobby", 60),
    ("ELECTRONICS", "22 mm latching mushroom E-stop", "NC contact ≥ 10 A, panel ≤ 6 mm", 1, "Hard power cut to motor branch", "Amazon/AliExpress", 8),
    ("ELECTRONICS", "Automotive relay / MOSFET switch 30 A", "12 V coil, driven by Teensy watchdog", 1, "Software E-stop / watchdog cut of motor power", "Any", 5),
    ("ELECTRONICS", "2-way ATO blade fuse holder + fuses", "15 A main, 10 A motor, 5 A compute", 1, "Branch protection", "Automotive", 8),
    ("SENSORS", "Intel RealSense D435i", "90 x 25 x 25, USB-C, with IMU", 1, "Stereo depth + IMU", "Intel / resellers (alt: Luxonis OAK-D Lite w/ new 81 adapter)", 330),
    ("SENSORS", "IMX219 160° CSI camera", "25 x 24 mm board, M2 21 x 12.5", 1, "Wide-angle front camera (Jetson CSI)", "Arducam / Waveshare", 20),
    ("SENSORS", "Slamtec RPLIDAR A1M8 (optional)", "360°, 12 m (VERIFY base hole pattern, slotted 82 adapter)", 1, "2D SLAM / obstacle map", "Slamtec / DFRobot", 99),
    ("SENSORS", "BNO085 9-DoF IMU breakout", "Adafruit 4754 class", 1, "Primary IMU near CG line", "Adafruit", 25),
    ("SENSORS", "VL53L1X ToF breakout", "4 m ToF, I2C (address set at boot)", 4, "Bumper perimeter ranging (3 front, 1 rear)", "Pololu / Adafruit", 12),
    ("SENSORS", "AS5600 magnetic encoder + 6x2.5 diametric magnet (optional)", "I2C/analog", 2, "Front (non-driven) wheel odometry / slip check", "AliExpress / Adafruit", 5),
    ("CONNECTORS/WIRING", "XT60 pair + pigtail", "12 AWG", 2, "Battery connection (pigtail on tub side wall)", "RC hobby", 3),
    ("CONNECTORS/WIRING", "Silicone wire", "14 AWG red/black 1 m, 18 AWG 2 m, 22 AWG 5 m, 26 AWG 5 m", 1, "Power + signal", "RC hobby", 20),
    ("CONNECTORS/WIRING", "JST-GH / JST-XH connector kit", "4–6 pin", 1, "Sensor & encoder leads", "Any", 12),
    ("CONNECTORS/WIRING", "Powered USB 3 hub (compact)", "4-port", 1, "D435i + LiDAR + Teensy to Jetson", "Any", 15),
    ("CONNECTORS/WIRING", "Zip ties 2.5 mm, spiral wrap, 10 mm grommet", "", 1, "Cable management", "Any", 6),
]

FASTENERS = [  # (item, qty, use)
    ("M3 x 8 countersunk (ISO 10642)", 12, "Skid to tub floor bosses"),
    ("M3 x 30 SHCS", 8, "Deck + rail + tub clamp stations"),
    ("M3 x 25 SHCS", 4, "Rear tower feet + rail + tub"),
    ("M3 x 10 SHCS", 30, "Bulkhead-tub (6), tower-bulkhead (4), servo (4), mast (3), lidar mast (4), misc"),
    ("M3 x 8 SHCS", 40, "Trays (20), depth mount (2), camera mount (2), lidar adapter (8), battery-door thumb screws (4 knurled), misc"),
    ("M3 x 6 SHCS", 14, "Gearmotor face (12, VERIFY thread depth), D435i (2)"),
    ("M3 x 16 SHCS + M3 nyloc", 2, "Motor cup pinch bolts"),
    ("M3 x 12 SHCS + 3x12 fender washer", 2, "Rear wheel retention into hex-adapter insert"),
    ("M3 x 16 SHCS", 2, "Front bumper to servo pillars"),
    ("M3 x 20 SHCS", 2, "Rear bumper to tower hoop"),
    ("M3 x 10 SHCS + nut", 18, "Board stand-offs (slotted)"),
    ("M3 nyloc nuts / washers", 40, "General"),
    ("M2 x 6 self-tapping (PT)", 12, "Teensy carrier, camera board, AS5600"),
    ("M2.5 x 6 SHCS", 8, "IMU, LiDAR base (VERIFY)"),
    ("M5 x 35 SHCS partially threaded (DIN 912) + M5 nyloc + washers", 2, "Front rotating axles"),
    ("ISO 7379 Ø6 x 25 shoulder bolt, M5 thread + 6x10x1.5 & 6x10x2 washers", 2, "Rear trailing-arm pivots"),
    ("M3 heat-set insert 5.7 mm (OD 4.6)", 150, "Printed-part threads (≈ 112 used)"),
    ("M3 heat-set insert 4 mm short", 20, "Knuckles, hex adapters (≈ 8 used)"),
    ("M5 heat-set insert", 5, "Rear pivots in tub (2 used)"),
    ("M3 spacer 7 mm", 4, "Front shock top ball studs"),
]


def main():
    vols = json.load(open(os.path.join(ROOT, "out", "reports", "part_volumes.json")))
    rows = []
    for n, d in vols.items():
        mat = d["material"]
        key = next(k for k in ("PETG-CF", "PA-CF", "TPU", "ASA", "PETG") if mat.startswith(k))
        q = QTY.get(n, 1)
        rows.append(dict(category="3D PRINTED PARTS", item=n, specification=f"{mat}; {d['volume_cm3']:.1f} cm³; ≈{d['mass_g_printed']:.0f} g",
                         quantity=q, function="", sourcing="Print (see print_list.csv)",
                         approx_cost_usd=round(d["mass_g_printed"] * q * (0.06 if key in ("PA-CF", "PETG-CF") else 0.025), 2)))
    for c, it, spec, q, fn, src, cost in PURCHASED:
        if q:
            rows.append(dict(category=c, item=it, specification=spec, quantity=q, function=fn, sourcing=src, approx_cost_usd=round(cost * q, 2)))
    for it, q, use in FASTENERS:
        rows.append(dict(category="FASTENERS", item=it, specification="A2 stainless or 12.9 black-oxide", quantity=q, function=use,
                         sourcing="Metric assortment kit + singles", approx_cost_usd=""))
    # Explicit UTF-8 keeps symbols such as ≈ and Ø portable on Windows.
    with open(os.path.join(HERE, "BOM.csv"), "w", newline="", encoding="utf-8") as f:
        w = csv.DictWriter(f, fieldnames=list(rows[0].keys()))
        w.writeheader(); w.writerows(rows)
    # print list
    with open(os.path.join(HERE, "print_list.csv"), "w", newline="", encoding="utf-8") as f:
        w = csv.writer(f)
        w.writerow(["part", "qty", "material", "orientation", "layer_mm", "walls", "infill", "top/bottom", "supports",
                    "inserts", "post_processing", "print_bbox_mm", "est_mass_g", "stl"])
        for n, d in vols.items():
            key = next(k for k in ("PETG-CF", "PA-CF", "TPU", "ASA", "PETG") if d["material"].startswith(k))
            st = SETTINGS[key]
            w.writerow([n, QTY.get(n, 1), d["material"], ORIENT_NOTE.get(n, ""), st["layer"], st["walls"], st["infill"],
                        st["top_bottom"], st["supports"], INSERTS.get(n, "—"), st["post"], "x".join(str(v) for v in d["print_bbox"]),
                        round(d["mass_g_printed"] * QTY.get(n, 1)), f"out/stl/{n}.stl"])
    # purchase list
    total = 0.0
    with open(os.path.join(HERE, "purchase_list.csv"), "w", newline="", encoding="utf-8") as f:
        w = csv.writer(f)
        w.writerow(["category", "item", "specification", "qty", "sourcing", "approx_cost_usd"])
        for c, it, spec, q, fn, src, cost in PURCHASED:
            if q:
                w.writerow([c, it, spec, q, src, round(cost * q, 2)]); total += cost * q
        w.writerow(["FASTENERS", "Metric fastener + insert kit (see BOM)", "", 1, "Assortment", 45]); total += 45
        fil = sum(r["approx_cost_usd"] for r in rows if r["category"] == "3D PRINTED PARTS")
        w.writerow(["FILAMENT", "PA-CF ≈ 0.5 kg, PETG ≈ 1 kg, PETG-CF ≈ 0.2 kg, TPU 95A ≈ 0.3 kg", "", 1, "Filament supplier", round(fil, 2)]); total += fil
        w.writerow(["TOTAL", "", "", "", "", round(total, 2)])
    print(f"BOM rows {len(rows)}; approx total USD {total:.0f}")


if __name__ == "__main__":
    main()
