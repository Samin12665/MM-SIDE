# 08 — Build Sequence

Tools: 2.0/2.5 mm hex drivers, soldering iron with heat-set insert tips (M3, M5), Ø3.0/3.2/4.0 drills,
Ø11/Ø13/Ø8 reamers (or a step drill plus sandpaper on a dowel), a 5.5 mm nut driver, calipers, and
medium thread-locker (metal-to-metal only; never on inserts in PA).

## Stage 0: prepare

1. Print all parts per `bom/print_list.csv`. Dry PA-CF before printing.
2. Install heat-set inserts in every pocket listed in the print list. Insert flush to −0.2 mm and
   keep the axis square (use the flat tip).
3. Ream bearing seats (Ø11, Ø13, Ø8) to a light press fit. Drill the bulkhead hinge blocks Ø3.0
   (tight) and the arm tubes Ø3.2 (running).
4. Cut and drill the two aluminium rails (DWG-110-RAIL). Use the tub itself as the drilling jig.

## Stage 1: chassis (ASM_CHASSIS)

5. Skid → tub: 12× M3 × 8 countersunk from below into the floor inserts.
6. Motor-driver tray (71), power tray (72) and front power tray (73): screw them to the floor
   bosses. Mount the boards now (slotted stand-offs; centre the holes) and pre-wire the power
   branches (§ wiring doc).
7. XT60 battery pigtail through the Ø12 grommet on the left wall (X −8).
8. Rails onto the wall tops. Loosely fit M3 × 25 at the rear-tower stations (−105, −80); the deck
   bolts come later.

## Stage 2: rear suspension and drivetrain (ASM_REAR_SUSPENSION + ASM_DRIVETRAIN)

9. Press 2× 686ZZ into each trailing arm.
10. Gearmotor into the arm cup: gearbox body into the Ø37.4 bore, 6× M3 × 6 through the face plate
    into the gearbox face (VERIFY thread depth first; use M3 × 5 if shallower), then snug the
    pinch bolt.
11. Pololu hub on the shaft (set screw on the D-flat, 0.5 mm off the face plate), then the hex
    adapter with 4× M3 × 10.
12. Arm onto the tub: ISO 7379 Ø6 × 25 shoulder bolt, 1.5 mm washer under the head, 2 mm thrust
    washer between arm and chassis, into the M5 insert. Check the arm swings freely with no end
    play.
13. Rear shock tower (30) onto the rails (M3 × 25 at −80 and −105). Fit the shocks: ball studs in
    the arm and tower. Fit the E-stop in the shelf.
14. Wheels: onto the hex, M3 × 12 + 3 × 12 washer into the adapter insert.

## Stage 3: front suspension and steering (ASM_FRONT_SUSPENSION + ASM_STEERING)

15. Bulkhead flange to the tub front wall: 6× M3 × 10.
16. Bellcranks: 2× MR84ZZ each, ISO 7379 Ø4 × 12 into the post inserts. **Shim with M4 washers**
    so the tie-rod ball centre is at Z 36.5 ± 0.2 (measure from the bench with the chassis on
    25 mm blocks). This sets bump steer.
17. Drag link onto both crank drag arms (Ø4 × 6 shoulder screws). Check both cranks rotate
    together without binding over ±31°.
18. Servo: set the servo to centre (1500 µs), fit the horn pointing straight rearward, then place
    the servo inverted in the pillars (4× M3 × 10). Servo link from the horn to the drag-link
    centre.
19. Lower arms: M3 ball end threaded into each arm tip (jam nut). Pin the arm between the hinge
    blocks with a Ø3 × 50 pin.
20. Knuckles: press 2× 685ZZ each, M5 × 35 axle through (head inboard, magnet cap on the head if
    using AS5600), 12 mm hex, wheel, M5 nyloc (snug, then back off until the wheel spins freely
    with no play).
21. Ball studs: lower (4 mm neck) and upper (7 mm neck) into the knuckle, steering-arm stud
    hanging down. Snap the lower ball into the arm's ball end.
22. Front shock tower (24) onto the bulkhead pads (4× M3 × 10). Camber links from the tower front
    studs to the knuckle top studs: set to 57.0 mm centre-to-centre (0° camber) to start. Tie rods
    set to 74.35 mm (0° toe).
23. Front shocks: arm tab stud to tower rear stud (with a 7 mm spacer).

## Stage 4: deck, electronics, sensors

24. Deck onto the rails: 8× M3 × 30 at stations −60, −25, 75, 105 (clamps deck + rail + tub).
25. Compute tray (70) plus Jetson, MCU/IMU plate (74) plus Teensy carrier, IMU and receiver.
26. LiDAR mast (84) on grid nodes (−62/−22, ±70); LiDAR adapter (82) plus LiDAR.
27. Sensor mast (83) on the front tower top (3× M3 × 10); depth mount (81) plus D435i; camera
    mount (80) plus camera.
28. Cable routing: power trunk on the right (−Y), signal trunk on the left (+Y), sensor trunk
    through the deck slot at (118, 0). Rail clips (100) every 40 mm. Nothing within 10 mm of the
    bellcranks, drag link, shocks or tyres (see SK12).
29. Bumpers: front (2× M3 × 16 into the servo pillars), rear (2× M3 × 20 into the hoop). ToF
    sensors into the pockets.
30. Side skirts (slide onto the deck edge, zip-tie through the slots).
31. Battery: slide in from either side, close both doors with thumb screws, plug the XT60 at the
    left side.

## Stage 5: set-up and checks

32. Ride height: set the spring collars so the tub underside sits at 25 mm and the front
    bulkhead at 18 mm with the full payload.
33. Alignment: camber 0 … −1°, toe 0 … +0.5 mm total toe-in, caster 5° (fixed).
34. Steering endpoints: set the servo travel so the inner wheel reaches 26° and no further. Check
    the tie rod against the rim at full lock (VERIFY item). Reduce the endpoint 1–2° if it touches.
35. Run `docs/10_validation_report.md` §4 bench tests before the first powered drive.
