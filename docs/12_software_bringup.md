# 12 — Control software and hardware bring-up

`software/arv1` and `software/hcra` contain dependency-free control cores with offline tests in
`tests/`. They provide testable safety/kinematic seams for the Teensy and ROS 2 transports; they
do not claim unpowered hardware has been commissioned.

## ARV-1

1. Verify E-stop continuity and the de-energised relay state before connecting motors.
2. Preserve manual-always-wins arbitration, the latched E-stop and the 200 ms Jetson watchdog.
3. With wheels raised, verify encoder polarity and 1216 counts/revolution; tune each 1 kHz PID.
4. Centre steering at 1500 µs, measure its map and cap the inner wheel at 26°.
5. Integrate ROS 2 in stages: manual drive, EKF, cameras/LiDAR, mapping, then low-speed Nav2.
6. Run `docs/10_validation_report.md` physical tests before ground operation.

## HCRA-6

1. With actuator power isolated, verify joint direction and Onshape mate limits.
2. Calibrate a neutral hand pose and normalized workspace. `HandMapper` supplies filtering,
   clamping, gripper mapping, a 250 ms tracking timeout and E-stop behaviour.
3. Command one joint at a time at reduced torque/speed; verify the paired J2 actuators first.
4. Replace staged J1-J3 mapping with geometric IK only after measuring links, zeros and direction.
5. Validate gripper open/closed travel and current limits using a compliant test block.

Run: `python -m unittest discover -s tests -v`.

Physical acceptance remains open: actuator IDs/zeros, servo PWM map, wheel polarity/PID gains,
sensor extrinsics, real-part hole patterns, loaded clearances and E-stop stopping time.
