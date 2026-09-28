# 06 — Onshape Build Guide (PHASES 6–14 in Onshape)

**Status:** the cloud session that produced this project could not reach `cad.onshape.com` (its
network policy blocks the host). So the complete design was built as parametric CAD in this repo
(CadQuery, driven by `cad/master/car_master.py`) and exported as STEP. Onshape gets it in one of
three ways, fastest first. Path C lets me (Claude) build the Onshape document directly.

| Path | What you get | Effort for you |
|---|---|---|
| **A. Import STEP** | Full vehicle in Onshape in ~5 min: one Part Studio per sub-assembly, named bodies, assembly with correct positions | Drag-and-drop 8 files, add mates from §4 |
| **B. Native parametric master** | `00_CAR_MASTER` Part Studio driven by `#variables` and SK00–SK14 sketches (FeatureScript), downstream Part Studios deriving from it | Paste one FeatureScript, then follow §3 per part |
| **C. Automated build by Claude via the Onshape API** | Paths A + B created and organised by script (`cad/onshape/onshape_api_build.py`) | Allow the host + add 2 API keys (below), then ask me to run it |

---

## 1. Document structure (all paths)

Document: **`ARV-1 Autonomous Research Vehicle`**. Tabs, in order:

```
00_CAR_MASTER_FS            (Feature Studio: CAR_MASTER.fs)
00_CAR_MASTER               (Part Studio: variables + SK00–SK14 master sketches, no solids)
10_CHASSIS                  (Part Studio: 10_MAIN_CHASSIS, 11_LOWER_SKID, 12_UPPER_ELECTRONICS_DECK, 61_BATTERY_DOOR_L/R)
20_FRONT_SUSPENSION         (20_FRONT_BULKHEAD, 24_FRONT_SHOCK_TOWER, 21/22_FRONT_KNUCKLE_L/R, 23_FRONT_LOWER_ARM_L/R)
30_REAR_SUSPENSION          (30_REAR_SHOCK_TOWER, 31_REAR_TRAILING_ARM_L/R, 32_REAR_HEX_ADAPTER_L/R)
40_STEERING                 (42_BELLCRANK_L/R, 42_DRAG_LINK, 42_SERVO_LINK; 41 servo mount is integral to 20_FRONT_BULKHEAD)
70_ELECTRONICS_MOUNTS       (70–74)
80_SENSOR_MOUNTS            (80–84)
90_PROTECTION               (90, 91, 92_L/R, 100_RAIL_CABLE_CLIP)
110_PURCHASED               (envelopes: motors, wheels, shocks, servo, battery, Jetson, sensors, rails…)
ASM_FRONT_SUSPENSION, ASM_REAR_SUSPENSION, ASM_STEERING, ASM_DRIVETRAIN, ASM_CHASSIS,
ASM_SENSOR_SYSTEM, ASM_ELECTRONICS, ASM_CAR_COMPLETE
DRW_* drawings (one per DWG sheet in docs/drawings/DRAWINGS.md)
```

## 2. Path A: import the STEP files

1. Create the document (name above). Delete the default Part Studio after the imports.
2. **Import** (Insert → Import, or drag the files in): `out/step/ASM_CAR_COMPLETE.step`. In the
   import dialog choose **"Create a Part Studio per top-level assembly member: off"**, **"Flatten
   assembly: off"**, **Units: millimeter**, and tick **"Y axis points up": off** (our frame is Z-up).
3. Onshape creates an assembly tab plus Part Studios containing all 79 named bodies at their
   installed positions. Rename the assembly tab `ASM_CAR_COMPLETE`.
4. Optionally also import the sub-assembly STEP files (`out/step/ASM_*.step`) for the
   sub-assembly tabs, or build them from the same Part Studios.
5. In `ASM_CAR_COMPLETE`, **fix** `10_MAIN_CHASSIS` (right-click → Fix), delete the implicit
   fixed state of all other instances, then add the mates of §4. Motion then works.
6. Assign materials (right-click part → Assign material) from `bom/print_list.csv`: PA-CF →
   "Nylon 12 (carbon fibre)" or a custom 1.15 g/cm³; PETG → 1.27; TPU → 1.21.

Imported bodies are not parametric (you can Direct-Edit them). For parametric work, use Path B for
the parts you want to evolve, starting with the master.

## 3. Path B: native parametric CAR_MASTER

### 3.1 Feature Studio

1. Insert → **Feature Studio**, name it `00_CAR_MASTER_FS`.
2. Keep the first two lines Onshape writes (`FeatureScript NNNN;` and the `import(path :
   "onshape/std/common.fs", …)` line). Delete everything else.
3. Paste everything in `cad/onshape/CAR_MASTER.fs` **below** the `---- paste below ----` marker.
   **Commit** (Ctrl-S). The notices panel must show no errors.
4. The file is generated from `car_master.py` (`python3 cad/onshape/generate_featurescript.py`).
   Never hand-edit numbers in Onshape. Edit the master and regenerate, so Python CAD, analysis and
   Onshape can never disagree.

### 3.2 `00_CAR_MASTER` Part Studio

1. Create the Part Studio `00_CAR_MASTER`.
2. Feature toolbar → **Custom features** (puzzle icon) → add from `00_CAR_MASTER_FS`.
3. Insert **"ARV-1 Master Variables"** as feature 1. All 123 variables (`#wheelbase = 300 mm`,
   `#frontTrack = 250 mm`, `#kingpinY = 113 mm`, …) now exist and show in the Variable table.
4. Insert **"ARV-1 CAR_MASTER sketches"** as feature 2. It creates 15 sketches:

| Sketch | Plane | Contents |
|---|---|---|
| SK00_GLOBAL_DATUMS | Top (XY) | origin, centreline, 4 wheel-centre points |
| SK01_VEHICLE_PLAN_ENVELOPE | Top | overall 473 × 285, tub 230 × 152, deck 208 × 160, rail lines |
| SK02_VEHICLE_SIDE_ENVELOPE | Front-side (XZ) | ground, wheel circles, tub, deck, LiDAR plane, height limit |
| SK03_WHEELBASE_AND_TRACK | Top | axle lines, 4 tyre footprints |
| SK04_FRONT_SUSPENSION_GEOMETRY | YZ at X = 150 | lower arm, camber link, kingpin, shock, spindle, tyre section |
| SK05_REAR_SUSPENSION_GEOMETRY | XZ at Y = 92 | trailing-arm line, pivot, shock, motor circle |
| SK06_STEERING_GEOMETRY | XY at Z = 36.5 | steering arms, tie rods, bellcranks, drag link, servo horn + link, Ackermann rays |
| SK07_DRIVETRAIN_LAYOUT | Top | gearmotor envelopes, shafts |
| SK08_BATTERY_ENVELOPE | Top | tunnel + battery rectangle |
| SK09_COMPUTE_AND_ELECTRONICS | Top | Jetson, driver, DC-DC, fuse, BEC, MCU, IMU zones |
| SK10_SENSOR_LAYOUT | XZ | camera axis, D435 FOV bounds, LiDAR plane, IMU point, ToF axis |
| SK11_BODY_AND_PROTECTION | Top | bumper footprints |
| SK12_CABLE_ROUTING | Top | power trunk (right, −Y), signal trunk (left, +Y), sensor trunk |
| SK13_MOUNTING_INTERFACE_STANDARD | XY at deck top | 20 mm M3 insert grid points |
| SK14_GROUND_CLEARANCE_AND_TRAVEL | YZ at X = 150 | ground, skid line, tyre at droop/ride/bump |

5. Verify: SK06 Ackermann rays from both kingpins meet on the rear axle centreline. SK04 lower-arm
   and camber-link lines meet the kingpin line at Y = 113, Z 26.5 / 60. The SK14 bump tyre stays
   below the deck.

### 3.3 Downstream Part Studios (the pattern, then per-part recipes)

In **every** downstream Part Studio:

1. Insert "ARV-1 Master Variables" first (same Feature Studio, so there is one source of truth).
2. **Derived** (Insert → Derived) → from `00_CAR_MASTER` pick only the sketches you need (e.g. SK04
   + SK06 for the knuckle). Use them as references. Never retype a master number; type
   `#kingpinY`, `#axleHeight`, etc.

The Python modules in `cad/parts/*.py` are the exact recipe for every part: each function is a
feature list with explicit coordinates. Recipes for the four most critical parts, in Onshape terms:

**21_FRONT_KNUCKLE_L** (Part Studio 20_FRONT_SUSPENSION, derive SK04 + SK06)

1. Sketch on the Right plane offset to Y = 102: circle Ø16 centred (X #xFrontAxle, Z #axleHeight).
   Extrude 12 mm toward +Y → housing.
2. Sketch on Top at Z = #kingpinLowerZ + 5: circle Ø10 at (X 150, Y #kingpinY). Extrude up 4.5.
   Add a rectangle X 145–155, Y 104–113 from that face up to Z 36.5 (web to housing).
3. Sketch at Z 49: circle Ø10 at (150, #kingpinY). Extrude up 4 (Z 49–53).
4. Steering arm: sketch on Top at Z 41.5, a slot from (157, 113) to the arm tip (X 150 + 20·cos
   3.03° + caster offset, Y 114.06), width 10. Extrude 4 up.
5. Encoder tab: box X 136–144, Y 99.5–103, Z 37.5–47.5; two Ø3.4 holes along Y at X 140, Z 40/45.
6. Holes: Ø6.2 through along Y. Bearing seats Ø11.1 × 4.2 deep from both ends. Insert holes Ø4 ×
   4 at the lower-ball face (from below), the upper-ball face (from above) and the arm tip (from
   above). Ø3.0 through at the arm tip.
7. **Transform → Rotate** the whole part −5° about the line through (150, 0, 42.5) along +Y. This
   is the caster, and it moves the top rearward.
8. Mirror across the Front (XZ) plane to create `22_FRONT_KNUCKLE_R` (Mirror, "new part").

**23_FRONT_LOWER_ARM_L**

1. Sketch on Top at Z 22.3. Polygon (134.5, 34) → (165.5, 34) → (158.4, 97) → (157.4, 103) →
   (145.4, 103) → (123, 82) → (123, 72) → (134.5, 42) → close. Extrude 8.2 mm.
2. Tube: Ø8.6 cylinder along X, centre (Y 34, Z 26.5), X 134.5–165.5. Ø3.2 through.
3. Shock boss Ø12 at (127, 80), same height; insert Ø4 × 6 from the top.
4. Ball-end insert: Ø4 × 6 along Y at (151.4, 103, 26.5).
5. Lightening triangle (141.5, 42) (158.5, 42) (151.4, 83) cut through.

**31_REAR_TRAILING_ARM_L**

1. Pivot boss: cylinder Ø20 along Y, Y 79.5–101, centre (X −32, Z 48.5).
2. Loft: rectangle 21.5 × 22 at X −38 (centred Y 90.25, Z on the pivot-to-axle line) to rectangle
   12.5 × 26 at X −133 (Y 80–92.5).
3. Cup: cylinder Ø46 along Y from Y 80 to 98.5 at (X −150, Z 42.5). Cut Ø37.4 from Y 80 to 92.5.
   Cut spigot Ø12.4. Circular pattern of 6 holes Ø3.4 on Ø31, starting at 30°, with c'bores Ø6.2 ×
   2.5 on the outer face.
4. Pinch slit 2 mm wide at the top of the cup plus a clamp ear with a Ø3.4 hole along X.
5. Pivot: Ø7 through, 686 seats Ø13.1 × 5.2 at both ends.
6. Shock boss Ø11 on top at (−114, 86) up to Z 58.5, insert Ø4 × 6.
7. Mirror → R.

**42_BELLCRANK_L**

1. Hub Ø10, Z 28.5–43 at (#crankPivotX, #crankPivotY).
2. Tie-rod arm: slot length #crankRadius + 8 at angle #crankBeta, Z 28.5–32. Insert at the ball
   end.
3. Drag arm: slot length #crankDragRadius + 8 pointing −X, Z 40–43; insert from below plus Ø3.4
   through.
4. MR84 seats Ø8.1 × 3.1 at both ends of the hub, Ø5 through.
5. The right crank mirrors the tie-rod arm, but its drag arm still points −X. That is a
   parallelogram, **not** a mirror image.

## 4. Assembly mates (ASM_CAR_COMPLETE)

Fix `10_MAIN_CHASSIS`. Use the named mate connectors below (all coordinates in the vehicle frame).

| # | Instance A | Instance B | Mate | Axis / location | Limits |
|---|---|---|---|---|---|
| 1 | 11_LOWER_SKID, 20_FRONT_BULKHEAD, 12_DECK, 61 doors, rails, trays 70–74, 30, 24, 83, 84, 82, 90, 91, 92 | chassis / parent | Fastened | bolt holes | — |
| 2 | 23_FRONT_LOWER_ARM_L | 20_FRONT_BULKHEAD | **Revolute** | X axis through (·, 34, 26.5) | −9° … +13.5° |
| 3 | 21_FRONT_KNUCKLE_L | 23_FRONT_LOWER_ARM_L | **Ball** | lower ball (151.4, 113, 26.5) | — |
| 4 | 110_CAMBER_LINK_FL | knuckle + 24 tower | Ball ×2 (or **Distance** 57.0 between ball centres) | (148.5, 113, 60) ↔ (150, 56, 60) | — |
| 5 | 110_TIE_ROD_FL | knuckle arm ball ↔ bellcrank ball | Ball ×2 (or Distance 74.35) | (170.5, 114.06, 36.5) ↔ (193.1, 43.2, 36.5) | — |
| 6 | 42_BELLCRANK_L / R | 20_FRONT_BULKHEAD | **Revolute** | Z axis at (176, ±48.9) | ±31° |
| 7 | 42_DRAG_LINK | both cranks | Revolute ×2 (Z) | (160, ±48.9) | — |
| 8 | 42_SERVO_LINK | servo horn + drag link | Revolute ×2 (Z) | (190, 0) and (160, 0) | — |
| 9 | Servo horn | 110_STEERING_SERVO | Revolute (Z) | (208, 0) | ±29° |
| 10 | 110_WHEEL_F*, 110_HEX12, axle bolt | knuckle | Revolute (Y) + Fastened stack | (150, ±125, 42.5) | — |
| 11 | 31_REAR_TRAILING_ARM_L | 10_MAIN_CHASSIS | **Revolute** | Y axis through (−32, ·, 48.5) | −6° … +9° |
| 12 | 110_GEARMOTOR_37D_L + 32 adapter + wheel | trailing arm | Fastened (motor), Revolute (Y) for wheel | (−150, ±125, 42.5) | — |
| 13 | Shocks | arm ball ↔ tower ball | **Slider** body/rod + Ball ends | eye-to-eye 58–80 | slider limits 0 … 22 |
| 14 | Steering relation | Mate 9 ↔ Mate 6 | Gear relation or drive both, the linkage solves | | |

Drive the steering by dragging the servo horn. Drive the suspension by dragging a lower arm. Use
**Motion → Animate** with the limits above and **Check interference** (Assembly toolbar): the
expected result matches `out/reports/interference.md`.

## 5. Drawings in Onshape

Create one drawing per sheet in `docs/drawings/DRAWINGS.md` (Insert → Drawing → pick the part →
Third-angle, A3). Add views Top/Front/Right/Iso. Add hole callouts for all Ø3.4/Ø4.0/Ø11.1/Ø13.1
features and the tolerance notes from the "General fits" table.

## 6. Path C: let Claude build it directly

1. In the Claude Code cloud environment settings (environment menu → Edit → **Network access**),
   add `cad.onshape.com` to the allowed domains, or choose a broader access level.
2. In Onshape → **My account → Developer → API keys**, create a key with read/write scope. Add
   `ONSHAPE_ACCESS_KEY` and `ONSHAPE_SECRET_KEY` as **environment secrets** in the same cloud
   environment settings. Never paste them into chat.
3. Tell me to continue. I will run `python3 cad/onshape/onshape_api_build.py`. It creates the
   document, uploads and imports all STEP files into correctly named tabs, creates the Feature
   Studio with `CAR_MASTER.fs`, instantiates the master features, and reports back.
