# 13 — ARV-1 T1 "Tactical UGV" — Onshape build (as built)

This document records what actually exists in Onshape as of 2026-09-30 (document version **V5.1**).
It replaces the plan in `06_onshape_build_guide.md` for the T1 revision.
Everything below was built, regenerated and checked in Onshape.

![iso](../out/t1_renders/01_iso_front.png)

## 1. Onshape document

| Item | Value |
|---|---|
| Document | **SIDE ARV-1 Autonomous RC Car** — `https://cad.onshape.com/documents/495cc3804c007e87f5b21cee` |
| Workspace | Main `d35a802f9506c7c27b1f7854` |
| Versions | `V1 all studios regenerate` → `V2 audit clean` → `V3 T1 complete` → `V4 suspension + wiring` → `V5 steering motion + exports + drawings` → **`V5.1 part drawings`** |
| Library | Feature Studio `01_FS_ARV_LIB` (≈192 k chars, build hash `4bec66d3`). Local source is `cad/onshape_t1/src/*.fs`, built by `tools/build.js` into `ARV_LIB.fs`. |

## 2. Master-skeleton / derived architecture

```
00_CAR_MASTER   26 native #variables  +  6 "ARV Master Layer" features
      │   (every master sketch body carries {arvMaster: layer, P: parameter map})
      ├── Derived ──► 10_CHASSIS           (SK01, SK02, SK04)
      ├── Derived ──► 20_FRONT_STEERING    (SK01, SK02, SK03)
      ├── Derived ──► 30_DRIVETRAIN        (SK01, SK02)
      ├── Derived ──► 40_BATTERY_SYSTEM    (SK02, SK04)
      ├── Derived ──► 50_ELECTRONICS_TRAY  (SK01, SK04)
      ├── Derived ──► 55_WIRING            (SK01, SK02, SK04)
      ├── Derived ──► 99_AUDIT_* (5)       (SK01–SK04)
      └── Derived ──► 60_BODY_MASTER       (SK02, SK03, SK05) + 4 "ARV Body Master" layers
                          └── Derived ──► 61_FRONT_FASCIA_BUMPERS, 62_BODY_MAIN,
                                          63_CENTER_SERVICE_PANEL, 64_REAR_BATTERY_PANEL,
                                          65_CAMERA_POD, 66_FENDERS, 70_LIDAR_OPTION  (BK01–BK03)
```

- Every subsystem studio starts with a **native Onshape Derived feature**. It pulls only the master sketches that studio needs.
- The studio's `ARV Studio Builder` feature then reads the master parameter map from the derived geometry. It errors out if the Derived feature is missing.
- If a builder throws, the studio shows a `BUILD ERRORS …` marker part instead of failing silently.
- No shared dimension is re-typed in any subsystem studio.
- Local detail stays local: ribs, fillets, insert pockets, counterbores, clip slots.

### Master variables (native, `00_CAR_MASTER`)

- **Chassis and wheels:** `#wheelbase 300`, `#trackWidth 250`, `#wheelDiameter 85`, `#wheelWidth 35`, `#groundClearance 25`, `#chassisLength 230`, `#chassisWidth 152`.
- **Body:** `#bodyWidth 224`, `#bodyHeight 142`, `#bodyLength 420`, `#noseX 205`, `#shoulderZ 126`, `#roofWidth 176`, `#fenderClearance 8`, `#fenderWidth 152`.
- **Battery:** `#batteryLength 139`, `#batteryWidth 47`, `#batteryHeight 37`, `#batteryFrontX -54`.
- **Tray and sensors:** `#trayDeckZ 84`, `#cameraX 170`, `#cameraHeight 152`, `#cameraTilt 5°`, `#frontSensorZ 86`, `#frontSensorSplay 25°`, `#steerLockInner 26°`.

### Master sketches / references

| Layer | Content |
|---|---|
| SK01_TOP_LAYOUT | Centreline, axles, wheel centres, tyre footprints, front-wheel sweep circles, kingpins, tub, bulkhead, body envelope, pods, bumper, electronics region, battery region, camera point, 3 front + 1 rear ToF points |
| SK02_SIDE_PROFILE | Ground, tyres (static and full bump), axle line, clearance line, tub, electronics envelope, battery, hull / pod / camera-pod / bumper / fascia profiles, shoulder line, hatch lines, battery-panel line |
| SK03_FRONT_TRACK | Tyres, kingpin axes, upper and lower ball points, upper and lower A-arm lines, fender sections, chassis width, body section, nose section, fascia width, ground-clearance line |
| SK04_INTERNAL_PACKAGING | Component envelopes: MDD10A, 19 V DC-DC, fuse block, 5 V buck, 6 V BEC, Jetson Orin Nano, Teensy carrier, BNO085, receiver, battery, relay, D435i, servo, IMX219, E-stop, battery tunnel, tray deck |
| SK05_BODY_SERVICE | Shell plan, centre-hatch opening, battery-panel opening, fascia, camera pod, 4 fender boundaries, 6 shell/chassis mount points |
| REF_DATUMS | Datum planes and interface connectors (see below) |

- **Planes (6):** `PL_FRONT_AXLE`, `PL_REAR_AXLE`, `PL_AXLE_HEIGHT`, `PL_TUB_TOP`, `PL_TRAY_DECK`, `PL_SHOULDER`.
- **Interface mate connectors (8):** 4 wheel centres, 2 caster-tilted steering axes, chassis origin, camera.

### Parametric validation (performed)

I changed `#wheelbase` from 300 to 330 mm in `00_CAR_MASTER` and measured the Part Studio bounding boxes:

| Studio | 300 mm (X extent) | 330 mm (X extent) |
|---|---|---|
| 20_FRONT_STEERING | 107 … 219 | **107 … 234** (+15, the front axle moved) |
| 30_DRIVETRAIN | −192.7 … −22 | **−207.7 … −37** (−15, the rear axle moved) |
| 66_FENDERS | ±226.5 | **±241.5** (fenders follow the wheels) |
| 10_CHASSIS / 62_BODY_MAIN | unchanged extents | unchanged extents (driven by `#chassisLength`, `#noseX`, `#bodyLength`) |

Reverting to 300 mm restored every studio exactly.
The side pods, shock-tower positions and body mount points also move inside the body, because they are defined from the axle positions.

## 3. Part Studios and physical parts (42 printed parts, 306 part instances)

| Studio | Printed parts (part number) | Purchased / hardware in the studio |
|---|---|---|
| 10_CHASSIS | MAIN_CHASSIS (10-001), LOWER_SKID (10-002), REAR_SHOCK_TOWER_L/R (10-003/004), REAR_BUMPER_FRAME (10-005) | inserts, skid screws, frame screws |
| 20_FRONT_STEERING | FRONT_BULKHEAD with integral servo cradle (20-001), FRONT_SHOCK_TOWER with upper-arm clevises (20-002), FRONT_KNUCKLE_L/R (20-003/004), FRONT_LOWER_ARM_L/R (20-005/006), BELLCRANK_L/R (20-007/008), DRAG_LINK (20-009), SERVO_LINK (20-010), **FRONT_UPPER_ARM_L/R (20-011/012)** | 20 kg servo, 25T horn, tie rods, M3 ball ends ×4, front shocks (body + rod), M5 axles, 685ZZ ×4, 12 mm hex with 3.3 mm spacer, 2.2" rims + tyres |
| 30_DRIVETRAIN | TRAILING_ARM_L/R with motor cups (30-001/002), HEX_ADAPTER_L/R (30-003/004) | Pololu 37D 19:1 ×2 (16 mm shaft), Pololu hubs, rear shocks (body + rod), 686ZZ ×4, ISO 7379 pivots, 2.2" rims + tyres |
| 40_BATTERY_SYSTEM | BATTERY_TUNNEL (40-001) | 3S 5000 battery, XT60 pair, foam stop |
| 50_ELECTRONICS_TRAY | TRAY_FRAME (50-001), TRAY_UPPER_DECK with IMU plinth (50-002) | Jetson Orin Nano Super, Teensy 4.1 carrier, MDD10A, 19 V DC-DC, 5 V buck, 6 V BEC, fuse block, SBUS receiver, BNO085, watchdog relay |
| **55_WIRING** | CABLE_CLIP_1…5 (55-001) | 10 harness bundles (55-100…109), see §5 |
| 61_FRONT_FASCIA_BUMPERS | FRONT_SENSOR_FASCIA (61-001), FRONT_BUMPER TPU (61-002), REAR_BUMPER TPU (61-003) | VL53L1X ×4 (3 front, 1 rear) |
| 62_BODY_MAIN | BODY_MAIN_FRONT (62-001), BODY_MAIN_REAR (62-002) | E-stop 22 mm, inserts, screws |
| 63_CENTER_SERVICE_PANEL | CENTER_SERVICE_PANEL (63-001) | 4 × M3 button head |
| 64_REAR_BATTERY_PANEL | REAR_BATTERY_PANEL (64-001) | 2 × M3 thumb screws |
| 65_CAMERA_POD | CAMERA_POD (65-001) | D435i, IMX219 160°, camera window |
| 66_FENDERS | FENDER_FL/FR/RL/RR (66-001…004) | 8 × M3 button head |
| **70_LIDAR_OPTION** | LIDAR_ADAPTER_PLATE (70-001) | RPLIDAR A1M8, 4 × M3 into roof inserts, USB harness |

- Every part is a single named solid with a part number, material, description and appearance.
- Each description ends with `[LNK_<link> #n]`, which identifies the kinematic link and a stable serial.
- Hardware is modelled as real parts: **80 heat-set inserts and 113 screws**.
- BOM: `bom/BOM_T1_onshape.csv`, regenerated from the `ASM_RC_CAR_COMPLETE` BOM: 121 lines, 306 instances.

**Print bed:** all 42 printed parts fit 256 × 256 × 256. This was checked on the exported STLs, see `out/reports/t1_print_bed_check.json`.
The largest is BODY_MAIN_REAR at 245 × 224 × 100.
The body shell is therefore split into front and rear halves. They join under the centre hatch with bolted splice plates (4 × M3), so the split line is hidden when the vehicle is closed.

## 4. Body system

| Element | Status | Engineering |
|---|---|---|
| Main shell | Built (2 printed halves) | Hard-surface three-view hull with chamfered roof and shoulders. Armoured side pods between the wheels hide the tub, pivots and arm roots; they open inboard below the tub top. The nose box hides the servo top and bulkhead. The tail box houses the battery tunnel. The hull underside over each wheel well is closed, with 8 mm clearance to the tyre at full bump. Grommet holes in the bay front wall and nose carry the harnesses. |
| Centre electronics service panel | Built | Roof, chamfers and 10 mm side walls. The opening X −48…112 passes the whole electronics tray. Two locating lips per side; 4 × M3 button heads go into inserts in tabs on the main body. Jetson exhaust slots are on both chamfers, plus a rear vent strip. |
| Rear battery service panel | Built | Tail panel with 2 locating tabs that bear on the tunnel bay. A push-stop holds the battery forward; a grip bar; 2 × M3 thumb screws go into inserts in the tunnel mouth. |
| Structural lower bumper | Built | TPU 95A crush cells. 2 × M3 × 16 with washers into the bulkhead servo-pillar inserts. It protects the steering and fascia. |
| Upper sensor fascia | Built | Faceted nose sitting above the bumper (split-level nose). Three recessed VL53L1X apertures: centre, plus two corners splayed 25°. The floor is opened over the inverted servo. |
| Low-profile camera pod | Built | Roof-conforming base, D435i on a 2 × M3 rear-face bracket, IMX219 lens port, protective window. 2 × M3 × 30 into roof inserts. Ø18 roof cable pass-through. |
| Partial tactical fenders | Built (4) | Angular eyebrows that frame the tyre top and leave the tyre exposed around the outer and lower side. The inboard wall is trimmed inside the tyre band so the tyre clears it at full bump. |
| Body ↔ chassis | Built | 6 tabs onto tub flange inserts (X −20 / 40 / 80). 2 tail screws into the tunnel flange. 2 nose locating pins into the front shock-tower top. |
| Passive cooling | Built | Pod louvres sit above the tub vent slots (no line of sight to the PCBs). Tub side vents. Tray airflow slots under the Jetson. Hatch chamfer and rear exhaust vents. |
| LiDAR option | Built (separate studio) | 4 roof bosses with inserts and a Ø12 cable hole are always present in the shell. `70_LIDAR_OPTION` adds the adapter plate and an RPLIDAR A1M8 at the rear roof. The scan plane (≈ Z 42 mm above the roof) clears the camera pod and E-stop. |

## 5. Internal packaging and wiring (final)

| Item | Location |
|---|---|
| Battery (3S 5000 hard case) | Rear, **above the gearmotors** in `BATTERY_TUNNEL` (X −193…−54, Z 86…123). Removed rearward through the rear panel. The XT60 sits at the bay corner, clear of the battery exit. |
| Jetson Orin Nano Super | Tray upper deck, front. Dev-kit envelope 100 × 79 × 21. Bosses inset 7 mm. |
| Teensy 4.1 carrier, SBUS receiver, BNO085 | Tray upper deck, rear. The IMU sits on a rigid plinth near the CG. |
| MDD10A (84.5 × 62 × 25), 19 V DC-DC, 5 V buck, 6 V BEC, fuse block | Tray lower level inside the tub. Power on −Y, logic power on +Y, cable corridor on the centreline. |
| Watchdog relay | Rear tub bay on floor bosses |
| Steering servo | Inverted in the bulkhead cradle, under the fascia |
| D435i + IMX219 | Camera pod (optical centre Z 152) |
| ToF | 3 in the fascia, 1 in the rear bumper (rear-bumper cable channel) |
| E-stop | Rear deck. It stays exposed for safety. |

**Harness geometry (`55_WIRING`)** — 10 bundles are modelled as tube chains at their real bundle diameters, plus 5 printed clips:

| Harness | Route |
|---|---|
| MAIN_POWER (Ø6, 12 AWG pair) | XT60 at the bay corner → along the tunnel outside wall (2 clips) → tray corridor → 15 A fuse |
| MOTOR_L / MOTOR_R (Ø5) | Motor end cap → tub rear-wall slot → MDD10A / Teensy, with a service loop at the arm |
| SERVO (Ø3.5) | Under the nose box and tower plate, over the drag link, through the bulkhead corridor → 6 V BEC / Teensy |
| TOF_FRONT (Ø4) | Daisy chain of the 3 fascia sensors → tower window → bay grommet → Teensy |
| TOF_REAR (Ø3) | Bumper channel → tub rear slot → deck pass-through → Teensy |
| CAMERA_USB (Ø5), CAMERA_CSI (Ø3) | Pod → roof pass-through → bay front-wall grommet → Jetson |
| ESTOP (Ø4) | E-stop NC pair → watchdog relay |
| JETSON_POWER (Ø4) | 19 V DC-DC → Jetson barrel jack |
| LIDAR (option, Ø4) | Roof grommet → Jetson USB |

All harnesses pass the static interference audit with no volume clashes.
They are excluded from the battery and tray sweeps, because they are flexible and are unplugged for service.

**Vendor dimensions:** the Jetson, MDD10A and Pololu 37D (12.5 mm encoder, 16 mm shaft) envelopes were updated from vendor data. Hole patterns are **not** verified, so the stand-offs stay slotted.

## 6. Assemblies

| Assembly | Instances | Mates (all OK) |
|---|---|---|
| **ASM_RC_CAR_COMPLETE** | 306 | 315 |
| ASM_FRONT_STEERING | 64 | 71 |
| ASM_DRIVETRAIN | 44 | 45 |
| ASM_CHASSIS (chassis + battery + electronics) | 111 | 110 |
| ASM_BODY | 87 | 86 |

**How the mates are built:**
- Whole Part Studios are inserted as individual part instances.
- Every part carries part-owned mate connectors (feature `ARV Link Connectors` in each studio).
- Parts inside a kinematic link are Fastened to the link's primary part.
- Links are joined as follows:
  - **Revolute:** servo horn, horn–link, bellcranks, `DRAG_L`, both lower and upper A-arm hinges, rear trailing-arm pivots and all 4 wheels.
  - **Ball:** lower and upper ball joints, both ends of each tie rod, both ends of each shock, and `DRAG_R` / `LINK_DRAG`.
  - **Revolute (ride-height lock):** the shock body ↔ rod slides.
  - **Slider:** the battery in its tunnel, limited to −165…0 mm (the removal stroke).
- `MAIN_CHASSIS` is fixed in `ASM_RC_CAR_COMPLETE`, and `FRONT_BULKHEAD` is fixed in `ASM_FRONT_STEERING`.

**Steering motion (verified in the assembly):** `ASM_FRONT_STEERING`, then right-click `STEER_SERVO` → Animate, from −28° to +28°.
The horn drives the servo link, the drag link, both bellcranks, the tie rods and the knuckles. Both wheels sweep through full lock with Ackermann (inner wheel steers more).
The same mate animates in `ASM_RC_CAR_COMPLETE`, but the wheels are mostly hidden by the fenders there.

Fixes that were needed for this:
- `STEER_SERVO` had anti-aligned connector Z axes, so the solver tried to flip the horn 180°. The primary axis was realigned.
- `DRAG_R` and `LINK_DRAG` were changed from revolute to ball. This removes the redundant planar loop that stalled the solver at about 10°.
- The shock slides were set to revolute. With no spring model, the free heave degree of freedom let the solver wander.
- Joint limits written through the REST API locked the joints. They were removed, except on the battery slider.

The generator (`tools/asm.js`) now produces these mate types directly.
To study suspension travel, set a `*SHOCK_SLIDE_*` mate back to Cylindrical.

## 7. Tests performed (FeatureScript `ARV Clearance Audit`)

Each audit studio rebuilds the complete vehicle from the master and runs `evCollision`:

| Test | Result |
|---|---|
| Straight-ahead: moving corner vs everything | **0 clashes** |
| **Full left lock** (crank +31°): inner 26.0° / outer 19.5° | **0 clashes** (tyres vs fenders, body, bumper, chassis, linkage) |
| **Full right lock** (crank −31°): inner 26.0° / outer 19.5° | **0 clashes** |
| **Bump left / bump right** (18 mm, knuckles and wheels translated) | **0 clashes**. This round found an inboard fender-wall clash, which was fixed by the trim. |
| Battery removal sweep (battery section extruded rearward past the tail, panel off) | **PASS**, no obstruction |
| Electronics service: tray lift sweep through the hatch opening (hatch off) | **PASS**, no obstruction |
| Static clashes between links (hardware included, < 2 mm³ ignored) | Only zero-volume face contacts remain (seated panels, ball studs). |

**Kinematic finding:** the ARV-1 single camber link left the knuckle under-constrained, with a free rotation about the ball-to-link axis. It was replaced by **upper A-arms** (ARV-20-011/012) with a clevis on the front tower and M3 × 25 pins.

## 8. Exports and drawings

| Output | Location (repo) | Also stored in Onshape |
|---|---|---|
| STEP, complete vehicle (7.1 MB, assembly structure kept) | `out/t1_exports/ARV1_ASM_RC_CAR_COMPLETE_V5.step` | blob element `ARV1_ASM_RC_CAR_COMPLETE_V5` |
| STL, all 42 printed parts (ASCII, **mm**, vehicle frame) | `out/t1_exports/stl/<studio>/<PART>.stl` | `STL_<studio>` blob elements (these also contain 8 purchased items that carry a printed material; they were dropped from the repo set) |
| Printed-part manifest | `out/t1_exports/printed_parts.json` | — |
| Assembly drawings (4 views, 1:3…1:5) | `out/t1_drawings/DRW_{COMPLETE,FRONT_STEERING,DRIVETRAIN,CHASSIS,BODY}.pdf` | `DRW_ASM_*` drawing elements + `PDF_DRW_*` |
| Part drawings (18 printed designs) | `out/t1_drawings/parts/DRW_<PART>.pdf` | `DRW_<PART>` + `PDF_DRW_<PART>` |
| Renders | `out/t1_renders/01…13_*.png` | — |

- The drawings are view sheets: orthographic plus iso, with a title note.
- They are **not dimensioned** manufacturing drawings. The parametric master and the STEP are the dimensional source.
- Onshape's STL export returns metres even when mm is requested. The repo STLs were rescaled ×1000 and verified; see the print-bed report.

## 9. Manufacturing notes / open issues

- **Purchased envelopes:** hole patterns for the MDD10A, Jetson and DC-DC are unverified (slotted stand-offs), as are the 37D face thread depth, the shock stroke and the rim ID. Measure the real parts before printing the parts that use them.
- **Body halves:** print nose-down / tail-down with tree supports under the hull floor over the wheel wells. The ASA walls are 2.4 mm.
- **Fender screws** are reached from the wheel well with a ball-end hex key.
- **Suspension in assemblies:** the shocks are locked at ride height. The 18 mm bump is proven in the audit studios, not by assembly motion.
- **Harness material** reuses the rubber material entry. Harness masses in the BOM are therefore approximate.

## 10. DONE IN ONSHAPE vs REMAINING

**Done in Onshape**
- `00_CAR_MASTER`: variables, SK01–SK05, datums and connectors
- Native Derived features in every subsystem studio
- `60_BODY_MASTER` derived layer
- 42 printed parts and all purchased envelopes, with inserts and screws
- Split structural chassis (tub + bulkhead, bolted), front steering with upper and lower A-arms, drivetrain, split shocks, battery tunnel with retention and rear removal, two-level removable electronics tray
- Enclosed tactical body: split-level nose with structural bumper and sensor fascia, camera pod, 4 partial fenders, centre hatch, rear battery panel, body ↔ chassis fastening, passive vents
- Wiring harness geometry and clips
- LiDAR option studio
- 5 assemblies, fully mated. Steering animates through full lock.
- Audit studios: centre, full left, full right, bump left, bump right
- Parametric propagation test
- STEP, STL, drawings (5 assembly + 18 part), BOM, renders
- Versions V1–V5.1

**Remaining (needs physical parts or a design decision)**
- Verify the vendor hole patterns and slot sizes against real parts
- Dimensioned manufacturing drawings, if a shop needs them (the view sheets are in place)
- Combined bump + full-lock audit, and suspension travel in assemblies with a spring model
