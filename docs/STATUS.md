# Engineering takeover status

## HCRA-6

Audited in Onshape on 2026-09-29 and checkpointed as `CODEX_TAKEOVER_BASELINE`. The document has
the master skeleton, 18 Part Studios, 8 assemblies, 13 drawings, a 69-part audit studio, complete
arm AP242 export and J1-J6 mates. The gripper has real palm, left/right finger, pads, MGN9
carriages/rail, pinion and servo geometry plus an assembly. Physical fit, wiring, load and powered
motion remain build-stage checks. A safe hand-mapping core and offline tests are now included.

## ARV-1

Claude's generated STEP/STL, subassemblies, drawings, BOM/print/purchase lists, motion evidence
and calculations are preserved. Real-part `VERIFY` items remain open. The BOM generator is now
explicitly UTF-8 on Windows. Tested Ackermann geometry, odometry, acceleration limiting, manual
override, heartbeat shutdown and latched E-stop logic are included. ROS 2 and Teensy transports
remain hardware/deployment work.

## ARV-1 T1 (Onshape, 2026-09-30)

Built natively in the Onshape document **SIDE ARV-1 Autonomous RC Car** (latest version `V5.1 part drawings`).

- `00_CAR_MASTER` holds 26 variables and the SK01–SK05 sketches. Every subsystem studio pulls from it through native Derived features.
- 42 printed parts and 306 part instances. 5 assemblies with 315 / 71 / 45 / 110 / 86 mates, all OK.
- Front suspension uses upper and lower A-arms with split shocks. Wiring harness geometry and an optional LiDAR studio are included.
- Clearance audit: 0 clashes straight ahead, at full left and right lock (26.0° / 19.5°), and at 18 mm bump on each side. The battery-removal and tray-lift sweeps pass.
- The steering animates through full lock in `ASM_FRONT_STEERING` (`STEER_SERVO` → Animate, ±28°).
- Exports: STEP (complete vehicle), 42 printed-part STLs in mm, 5 assembly and 18 part drawing PDFs, BOM, 13 renders.
- Changing `#wheelbase` propagates correctly and reverts cleanly.

Open items:
- Verify vendor hole patterns against real parts (the stand-offs are slotted until then).
- Dimensioned shop drawings, if needed (the view sheets exist).
- Combined bump + lock audit, and sprung suspension travel in the assemblies.
