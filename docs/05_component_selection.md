# 05 — Component Selection (PHASE 5)

Every purchased component is represented in CAD by an envelope in `cad/parts/purchased.py`, placed
in `ASM_CAR_COMPLETE`. The full priced list is in `bom/purchase_list.csv`.

The CAD environment had no internet access, so vendor drawings could not be downloaded. Dimensions
marked **VERIFY** come from typical vendor data and must be checked against the real part before
printing the parts that consume them. Every VERIFY interface is either **slotted** (±2 mm) or
isolated in a **replaceable adapter**, so a mismatch costs one small reprint, never a chassis.

| Function | Selected | Why this one | Key interface used in CAD | Status |
|---|---|---|---|---|
| Drive motors | Pololu 37D 19:1 12 V + 64 CPR encoder (×2) | Integrated quadrature encoder, metal gearbox, documented mounting, ≈ 2 m/s at 11.1 V | Ø37 body, 6 × M3 on Ø31 PCD face, Ø12 spigot, Ø6 D-shaft 22 mm | VERIFY face-hole thread depth |
| Motor driver | Cytron MDD10A | 10 A/ch continuous vs 5.5 A stall; 5–30 V; sign-magnitude PWM | 84 × 62 envelope, slotted stand-offs | VERIFY hole pattern |
| Steering servo | 20 kg·cm standard digital (DS3218 class) | 4.4× the required 4.5 kg·cm; metal gears; standard footprint | 40.5 × 20 × 38.5, tabs 49.5 × 10, 25T | OK |
| Shocks | 1/10 80 mm oil shocks | Commodity; 22 mm stroke covers the 21 mm / 17 mm needed | Eye-to-eye 80 extended, M3 ball cups | VERIFY stroke ≥ 22 |
| Wheels | 1/10 buggy/rally 85 × 35, 12 mm hex | Enough diameter for grass and thresholds; hex standard | 5 mm bore, 12 mm hex, rim ID ≥ 54 | VERIFY rim ID and offset |
| Front axle | M5 × 35 DIN 912 rotating in 2× 685ZZ | Rotating axle lets the AS5600 read the wheel directly; shank covers both bearings | 13 mm shank | OK |
| Rear pivot | ISO 7379 Ø6 × 25 (M5) + 2× 686ZZ | Precision shoulder, no thread in bearings | M5 insert in tub | OK |
| Battery | 3S 5000 mAh hard case | 44 Wh usable → 57 min; hard case is crash-safe | ≤ 140 × 48 × 38 tunnel, foam shims | Size varies by brand |
| Compute | Jetson Orin Nano Super Dev Kit | CUDA for vision and depth, ROS 2 support, 7–25 W | 103 × 90.5 × 34.8; carrier holes via slotted tray | VERIFY 86 × 58 hole pattern |
| MCU | Teensy 4.1 | Hardware quadrature decoders, 600 MHz, many UARTs (SBUS, LiDAR pass-through) | 70 × 50 perfboard carrier | OK |
| Depth | Intel RealSense D435i | Global-shutter stereo depth + IMU, mature ROS 2 driver | 2 × M3 on rear face 45 mm apart | OK (alt: OAK-D Lite = new 81 adapter only) |
| Camera | IMX219 160° CSI | Wide FOV for lane/near-field vision, native Jetson CSI | 25 × 24 board, M2 on 21 × 12.5 | OK |
| LiDAR (opt.) | RPLIDAR A1M8 | Cheapest usable 360° SLAM LiDAR, 12 m | Slotted adapter 82 | VERIFY hole pattern and scan height |
| IMU | BNO085 | On-chip sensor fusion, game rotation vector | Plinth, M2.5 on 20.3 × 12.7 | VERIFY |
| Perimeter ranging | VL53L1X ×4 | 4 m ToF, I2C, cheap | Bumper pockets 19 × 14 | OK |
| E-stop | 22 mm latching mushroom NC | Standard, panel-mount, 10 A | 22.3 mm hole in 4 mm shelf | OK |
| Regulators | 19 V buck-boost (Jetson), 5 V 5 A buck, 6 V 5 A BEC | Isolates servo and motor noise from logic | Slotted trays | VERIFY footprints |

## Rejected alternatives (short)

- **Brushless motor + ESC + purchased gearbox**: faster than needed, extra encoders required, and a
  gearbox whose interfaces couldn't be verified.
- **Raspberry Pi 5 as main compute**: cheaper but no CUDA. The compute tray is slotted, and a Pi 5
  (holes 58 × 49) fits a new 70-series adapter plate.
- **Ultrasonic HC-SR04**: wide beam, slow. ToF is better at 0.05–4 m and fits in the bumper.
