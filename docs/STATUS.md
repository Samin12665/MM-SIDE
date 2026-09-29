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
