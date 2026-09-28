# 10 — Mechanical Validation Report and Final Engineering Audit (PHASES 15 & 20)

Evidence files: `docs/04_engineering_calculations.md` (generated), `analysis/steering_solution.json`,
`out/reports/interference.md` (generated), `out/reports/part_volumes.json`, renders in `out/renders/`.

## 1. Audit checklist

| Area | Check | Result | Evidence |
|---|---|---|---|
| **Steering** | Correct motion L/R | ✅ Inner 26.0° / outer 19.5° both directions, symmetric | interference.md ψL/ψR columns |
| | No binding | ✅ Parallelogram drag link; crank ±31°, servo ±28° (servo capable of ±45°) | `front.servo_horn_pin`, STEERING_full_lock_top.png |
| | Ackermann | ✅ 86 % near centre → 102 % at lock | steering_solution.json |
| | Bump steer | ✅ ≤ 0.02° over −12…+18 mm | 04 §4.3 |
| | Servo torque | ✅ Need 4.5 kg·cm, have 20 kg·cm (SF 4.4) | 04 §4.3 |
| | Servo rigidly mounted | ✅ Two pillars integral to the bulkhead, 4× M3 into inserts; bumper bolts to the pillar fronts (servo protected) | DWG-20 |
| **Suspension** | Travel | ✅ +18 / −12 mm front and rear | SK14 |
| | Collisions through travel | ✅ 9-state sweep: no suspension collisions (see §2 for the single tie-rod/rim item) | interference.md |
| | Shocks within stroke | ✅ Front 58.2…76.6 of 58…80; rear 58.5…79.5 | 04 §4.4 |
| | Joints can assemble | ✅ All joints are purchased balls/pins/shoulder bolts with insert threads; assembly order in 08 | 08 |
| | Camber curve | ✅ −0.6° droop … −1.4° full bump (front); 0° rear | 04 §4.4 |
| **Drivetrain** | Shafts aligned | ✅ Gearmotor spigot located in a Ø12.4 bore, face bolted; wheel is coaxial by construction | DWG-31 |
| | Motor supported | ✅ Face plate + gearbox cup + pinch clamp; torque reacts inside the arm | DWG-31 |
| | Wheel retention | ✅ Rear: M3 screw + washer into an axial insert (axial loads only; torque through the 12 mm hex). Front: M5 nyloc on a rotating axle | 08 steps 14, 20 |
| | Shaft stress | ✅ 27 MPa under 3 g (SF > 9) | 04 §4.2 |
| **Structure** | Stiffness | ✅ Torsion box (tub + rails + deck); rails alone 0.51 mm at 3 g | 04 §4.5 |
| | Suspension arm stresses | ✅ Front arm SF 5.3, rear arm SF > 30 at 5 g | 04 §4.5 |
| | Battery retained | ✅ Tunnel walls + ceiling + two doors with 4 thumb screws | DWG-10 |
| | Electronics mounted | ✅ All boards on trays/stand-offs, VERIFY patterns slotted ±2 mm | electronics.py |
| **Autonomy hardware** | Camera FOV clear | ✅ 160° camera at Z 168, pitch −12°, under a visor; bumper outside the D435 lower FOV bound | SENSOR_LAYOUT_side.png |
| | Camera height | ✅ 168 mm (target 120–200) | SK10 |
| | LiDAR visibility | ✅ Scan plane Z 238; highest other part is the visor at Z 183 | SK02/SK10 |
| | Sensors protected | ✅ Visor + lens-hood lip, TPU bumpers with recessed ToF pockets, rear hoop over motors | renders |
| | IMU | ✅ Solid 8 mm plinth on the deck, isolated from the Jetson fan; lever arm to CG known (TF table) | 09 |
| **Serviceability** | Battery removable | ✅ Side-loading, no tools (thumb screws), XT60 outside the car | 03, 08 |
| | Compute accessible | ✅ Jetson on the deck top, 6 screws | 08 |
| | Motor replaceable | ✅ 6 face screws + pinch bolt; arm stays on the car | 08 |
| | Wheels removable | ✅ One screw (rear) / one nut (front) | 08 |
| | Suspension serviceable | ✅ Pins, ball cups, shoulder bolts; every printed arm replaceable individually | 08 |
| **Printability** | Orientations feasible | ✅ All defined; loads along layers for structural parts | 07 |
| | Printer size | ✅ Every part fits 256³ | part_volumes.json (`fits_bed`) |
| | Supports minimised | ✅ Only the rear-tower shelf/hoop and camera arm need them | 07 |

## 2. Engineering problems found and corrected during development

| # | Problem (found by) | Correction |
|---|---|---|
| 1 | Front shocks crossed the aluminium rails and the shock-tower plate (static interference) | Rails now end at the tub front wall (X 118). Shocks moved to a vertical plane at Y ±80, X 127, on 12 mm stand-offs. Tower re-founded on 4-bolt gusseted legs |
| 2 | C-hub lower plate collided with the lower-arm clevis (static) | Upright changed to a touring-car **ball-joint knuckle**; steering re-optimised (Ackermann kept, bump steer 0.00°) |
| 3 | Lower arms hit the bulkhead floor at full droop (motion sweep) | Floor relieved under the arm sweep |
| 4 | Inner tyre grazed the front shock at full lock + full bump (motion sweep) | Shock moved inboard (clearance ≥ 5.2 mm over all travel at lock); mechanical lock reduced 28° → 26° (turning radius 0.67 → 0.72 m, still < 0.8 target) |
| 5 | Encoder posts and knuckle webs hit the camber link and tower in bump (motion sweep) | Encoder interface moved behind the axle; upper-ball boss reduced to a Ø10 × 4 cylinder |
| 6 | Rear arm pivot: M5 threads would have run in a bearing | 686ZZ bearings on an ISO 7379 Ø6 shoulder bolt |
| 7 | Front and rear shocks outside the 22 mm stroke after geometry changes (calc check) | Upper mounts re-solved: front Z 103.0, rear Z 130.2 |
| 8 | Traction limit (0.38 N·m) < motor stall (0.77 N·m) (calc) | Firmware acceleration ramp ≤ 2 m/s² specified; front non-driven encoders provide a slip check |
| 9 | Printed mass 1.3 kg vs 1.1 kg estimate (CAD volumes) | Bumpers slimmed; total 3.62 kg with LiDAR (3.36 kg without); load cases use +15 % |
| 10 | Tie-rod nut and bumper-mount bosses collided with the bellcranks at lock (motion sweep) | Bumper mounts moved into the servo-pillar fronts; bellcrank plate lowered to give ball-end clearance |

## 3. Open items (must verify with real parts; each has a mitigation)

| Item | Risk | Mitigation already in the design |
|---|---|---|
| Tie rod vs **rim lip** at full lock (1–5 mm³ against a sharp-edged generic rim model) | Light rub at full lock only | Check with the purchased wheels; if it touches, reduce the servo endpoint 1–2° or use +3 mm offset wheels |
| Gearmotor data (rpm, torque, face-thread depth) | Speed/torque ±10 %; screw length | Screw length from the real part; calcs regenerate from master |
| Jetson carrier / MDD10A / DC-DC / IMU / LiDAR hole patterns | Board doesn't fit tray | All slotted ±2 mm, or on replaceable adapter plates (70, 71, 72, 74, 82) |
| Shock stroke ≥ 22 mm and 80 mm length | Travel limits | Mount heights are master variables; shift ±1 mm and regenerate |
| Wheel rim ID / offset | Knuckle-in-rim clearance | Check the rim ID ≥ 54 mm before printing knuckles |
| Front clearance 18 mm at bulkhead | Bottoming on large bumps | Sacrificial underside; 3 mm shock spacers limit bump if needed |
| Onshape native model | The Onshape document itself was not created from this session (network) | STEP import path (06 §2) + generated FeatureScript master (06 §3) + API script (06 §6) |

## 4. Bench tests before the first powered drive

1. Unpowered: sweep the steering by hand lock-to-lock. There must be no binding, and the tie rods
   must clear the rims.
2. Compress each corner to the bump stop and release. Nothing may touch except the shock
   internals.
3. Measure static ride height (25 mm tub / 18 mm front) and set the spring collars.
4. Servo only (BEC powered): centre, check endpoints are ≤ 26° inner, no stall buzz at lock.
5. Wheels up: run the motors at 10 % both directions. Check encoder counts are 1216 ±1 per
   wheel revolution and the sign convention is right.
6. E-stop: with the wheels up at 30 %, hit the E-stop. The motors must stop within 0.2 s and the
   Jetson must stay up.
7. RC: MANUAL override takes control from AUTO within one SBUS frame.
