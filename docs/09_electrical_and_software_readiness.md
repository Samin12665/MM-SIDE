# 09 — Electrical Architecture and Software Readiness

Detailed wiring instructions come in a later phase. This document fixes the **architecture** that
the mechanical packaging was designed around, so the software work can start on real hardware.

## 9.1 Power tree

```
3S LiPo 11.1 V ──XT60── pigtail (left wall) ──> FUSE 15 A (main, front bay)
   ├── FUSE 10 A ──> E-STOP (NC, rear tower) ──> RELAY (Teensy watchdog) ──> MDD10A ──> 2× 37D gearmotors
   ├── FUSE 5 A  ──> 19 V buck-boost (rear bay) ──> Jetson Orin Nano (barrel)
   ├── 5 V 5 A buck (front bay) ──> Teensy, IMU, ToF, encoders, powered USB hub
   └── 6 V 5 A BEC (front bay) ──> steering servo
Common ground star point: front-bay fuse-block negative bus.
```

Routing: the **power trunk runs on the right (−Y)** and the **signal trunk on the left (+Y)**
(SK12). Motor leads stay in the rear bay. Sensor USB/CSI cables use the centre deck slot (118, 0)
up the mast cable window.

## 9.2 Control architecture (supports the future software stack)

| Layer | Hardware | Responsibility |
|---|---|---|
| Safety / real-time | Teensy 4.1 | SBUS decode, MANUAL/AUTO arbitration (manual always wins), wireless E-stop channel, Jetson heartbeat watchdog (200 ms), relay control, 1 kHz wheel PID from 2× quadrature encoders, servo PWM, IMU + ToF polling, battery voltage |
| Autonomy | Jetson Orin Nano | ROS 2 (Humble/Jazzy): `realsense2_camera`, CSI camera (`gscam`/Argus), `rplidar_ros`, `robot_localization` EKF (wheel odometry + BNO085 + D435i IMU), `slam_toolbox`, Nav2 with an Ackermann-aware controller (Regulated Pure Pursuit / MPPI) |
| Link | USB (Teensy ↔ Jetson) | micro-ROS or a simple COBS serial protocol: cmd (v, δ) down; odometry, IMU, ToF, status up |

The mechanical design provides for the software as follows:

- **Wheel odometry**: 1216 counts per rear-wheel revolution (0.22 mm/count). Rear trailing arms
  give zero track or camber change, so the kinematic model stays valid over bumps. Optional AS5600
  sensors on the non-driven front wheels give slip detection.
- **Ackermann model**: wheelbase 300 mm, measured steering map (servo µs → δ from
  `analysis/steering_solution.json`), 86–102 % Ackermann, so a bicycle model is accurate.
- **TF tree (vehicle frame = `base_link` at ground/centreline/mid-wheelbase)**, all taken from CAR_MASTER:

| Frame | x | y | z | Notes |
|---|---:|---:|---:|---|
| base_link | 0 | 0 | 0 | ground plane |
| imu_link (BNO085) | 115 | −46 | 110 | on plinth |
| camera_link (IMX219) | 178 | 0 | 168 | pitch −12° |
| depth_link (D435i) | 172 | 0 | 140 | pitch −5° |
| laser (RPLIDAR) | −40 | 0 | 238 | scan plane (VERIFY optical centre) |
| tof_front_c / _l / _r | 233 | 0 / ±75 | 55 | |
| rear_axle | −150 | 0 | 42.5 | Ackermann reference point |

Export this table as a URDF later: every value is in `car_master.py`.
