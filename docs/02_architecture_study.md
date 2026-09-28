# 02 — Custom vs Commercial Architecture Study (PHASE 2)

## Candidates

**Option A: fully custom vehicle architecture.** A printed chassis with engineered suspension,
steering and drivetrain geometry. Every load-bearing wear item is bought: bearings, shocks, ball
joints, turnbuckles, shoulder bolts, gearmotors and wheels.

**Option B: commercial 1/10 chassis, heavily modified.** For example a Traxxas Slash 4×4 (the
F1TENTH base), a Tamiya TT-02 or a Redcat/HSP 94123. The stock chassis, suspension, steering and
drivetrain are kept, and custom structures carry compute, sensors, electronics and protection.

## Scoring (1 = poor, 5 = excellent; weights reflect this project's goals)

| Criterion | Weight | A: custom | B: commercial | Notes |
|---|---:|---:|---:|---|
| Engineering learning value | 3 | 5 | 2 | A forces real suspension, Ackermann and load-path design |
| Portfolio value | 3 | 5 | 3 | "Designed the vehicle" vs. "bolted a deck to a Slash" |
| Reliability | 2 | 3 | 5 | B is proven; A mitigated by buying all wear items and metal pins |
| Cost | 1 | 4 | 3 | A's chassis cost is mostly filament (~$60 of mechanicals + motors); B's roller is $250–450 |
| Ease of sourcing | 1 | 4 | 4 | A uses commodity RC/robotics parts from several vendors |
| Design complexity (lower = better) | 1 | 2 | 4 | A is much more work, and that is the point |
| CAD depth | 3 | 5 | 2 | A needs master-model-driven design of every interface |
| Suspension quality | 2 | 4 | 5 | A: double wishbone with camber gain + trailing arms; B: mature factory geometry |
| Steering quality | 2 | 5 | 3 | A: 86–102 % Ackermann, ≈ 0° bump steer by design; hobby chassis are often 40–60 % Ackermann and sloppy |
| Drivetrain quality for robotics | 2 | 5 | 3 | A: two encoder gearmotors, no diff backlash, 0.22 mm/count odometry; B: fast brushless + diff, needs added encoders |
| Autonomous sensor integration | 3 | 5 | 3 | A: sensors, CG and cabling designed together; B: bolted on top, high CG |
| Serviceability | 2 | 4 | 4 | A: side-loading battery, lower-bay trays |
| 3D-printing feasibility | 2 | 4 | 5 | A: 36 printable parts, all ≤ 250 mm |
| **Weighted total** | | **120 / 135** | **91 / 135** | |

## Decision: Option A, "custom architecture on commercial wear parts"

Option A wins by a wide margin on the criteria this project exists for: learning, portfolio, CAD
depth and autonomy integration. Its main weakness is reliability, and the design attacks that
directly:

1. **Buy every wear or precision interface.** That covers ball bearings (685, 686, MR84), steel
   ball studs and ball cups, turnbuckles, hinge pins, shoulder bolts, oil shocks, the servo and the
   gearmotors. Printed parts only carry them.
2. **No printed gears, diffs or shafts.** Drive is two Pololu 37D encoder gearmotors, one per rear
   wheel, which gives an electronic differential. This removes the least reliable DIY item (a
   printed or mismatched transmission) and gives each driven wheel its own encoder.
3. **Metal where the analysis says so.** Aluminium longerons form the top chords of the tub
   torsion box. Every printed thread is a brass heat-set insert.
4. **Loads designed to print orientation.** Suspension arms print flat and knuckles print with
   the bearing axis vertical (see `docs/07_print_guide.md`).

### Drivetrain sub-study

| Option | Pros | Cons | Verdict |
|---|---|---|---|
| RWD, single brushless + purchased diff gearbox | fast, classic RC | needs a gearbox whose dimensions can't be verified offline; diff backlash; separate wheel encoders | rejected |
| 4WD commercial transmission | traction | CV joints on steered wheels, much more complexity, all in the purchased part | rejected |
| **Dual independent rear gearmotors on trailing arms** | encoders built in (1216 counts/wheel-rev), electronic differential and torque vectoring, no CV joints (motor rides on the arm), fully documented part | unsprung motor mass (0.21 kg/side); top speed limited to ≈ 2 m/s | **selected**: speed limit is a feature for robotics |

### Suspension sub-study

| Axle | Selected | Why |
|---|---|---|
| Front | Double wishbone: printed lower A-arm + adjustable turnbuckle camber link; ball-joint knuckle (touring-car style) | Correct camber gain (−1.4° at full bump), adjustable camber/toe, standard RC ball joints, kingpin inside the rim |
| Rear | Independent trailing arm carrying the gearmotor | Zero camber/track change (clean odometry), no CV joints, simple 2-bearing pivot |

### Steering sub-study

A direct servo-horn linkage has poor bump steer and fixed Ackermann. The selected layout is **dual
bellcranks joined by a parallelogram drag link, driven by an inverted servo through a short servo
link**. The tie-rod inner ball positions were numerically optimised
(`analysis/front_kinematics.py`) for both Ackermann and bump steer.
