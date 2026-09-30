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

Built natively in the Onshape document **SIDE ARV-1 Autonomous RC Car** (version `V3 T1 complete`).

- `00_CAR_MASTER` holds 26 variables and the SK01–SK05 sketches. 15 studios pull from it through native Derived features.
- 35 printed designs and 272 parts; 5 assemblies with 277 / 63 / 41 / 95 / 75 mates, all OK.
- The clearance audit finds 0 clashes straight ahead and at full left and right lock (26.0° / 19.5°). The battery-removal and tray-lift sweeps pass.
- Changing `#wheelbase` propagates correctly and reverts cleanly.

Open items:
- Drawings and STEP/STL export.
- Harness geometry.
- Suspension travel in the assemblies, and interactive steering drag.
- Checking the vendor `VERIFY` dimensions against real parts.
- LiDAR is not included in T1.
