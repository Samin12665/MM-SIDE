# 01 — Vehicle Requirements (PHASE 1)

ARV-1 is a compact autonomous research vehicle: a 1/10-class ground robot built to carry a real
autonomy sensor and compute stack, not a hobby car with a camera taped on. The requirements below
drive every number in `cad/master/car_master.py`.

## 1.1 Mission and use cases

| ID | Use case | Consequence for the mechanical design |
|---|---|---|
| U1 | Indoor lab and corridor autonomy (SLAM, path planning, obstacle avoidance) | Low top speed, tight turning, 360° LiDAR plane clear of the vehicle |
| U2 | Outdoor pavement, park paths, gentle grass, kerb ramps | Real suspension, ≥ 25 mm ground clearance under the tub, sealed-ish electronics |
| U3 | Computer vision (lane/object detection, visual odometry) | Rigid, vibration-damped camera at 150–200 mm, pitched down, clear FOV |
| U4 | Sensor experiments | Modular sensor mounts on a standard hole grid, mast adapters replaceable |
| U5 | Manual RC driving and safety override | RC receiver in the loop, hardware E-stop, watchdog relay |
| U6 | Portfolio demonstration and MM FORGE import | Clean hierarchy, named bodies, STEP AP214/AP242, exploded views, BOM |

## 1.2 Quantified requirements

| Req | Target | Rationale |
|---|---|---|
| Top speed | 1.5–2.5 m/s (software-limited ≈ 1.2 m/s for SLAM) | Keeps motion blur, scan skew and stopping distance manageable; faster adds risk and adds nothing for robotics |
| Acceleration | ≤ 2 m/s² (firmware ramp) | Stays below the rear-tyre traction limit so wheel odometry stays valid |
| Turning radius | ≤ 0.8 m (rear-axle centre) | Corridor U-turns; 1.5 m wide corridors |
| Wheelbase / track | ≈ 300 / 250 mm | Long enough for pitch stability and packaging; narrow enough for doorways |
| Mass | ≤ 3.6 kg with the full sensor suite | Motor sizing, printed-part stress, battery life |
| CG height | ≤ 80 mm | Rollover threshold ≥ 1.5 g |
| Ground clearance | ≥ 25 mm under the main tub | Door thresholds, cable covers, grass |
| Wheel travel | ≥ 25 mm total | Keeps all 4 wheels loaded on uneven ground → stable odometry and sensor attitude |
| Runtime | ≥ 30 min mixed driving | Useful test sessions |
| Camera height | 120–200 mm | Useful ground-plane geometry, below the LiDAR plane |
| LiDAR | 360° scan plane with no vehicle parts above it | SLAM quality |
| Printed parts | Fit a 256 × 256 × 256 mm bed; common FDM materials | Bambu P1S/X1C/A1, Prusa MK4, etc. |
| Serviceability | Battery swap < 30 s without tools; compute removable with 6 screws; bumpers sacrificial | Lab use |
| Hardware | Metric M3 standard (M5 at axles/pivots), heat-set inserts in all printed threads | Standardised tool kit |

## 1.3 Payload list (to be packaged)

Battery, drive-motor driver, embedded computer, microcontroller, IMU, forward camera, stereo depth
camera, optional 2D LiDAR, wheel encoders, RC receiver, power regulation (19 V, 5 V, 6 V servo), fuses,
latching E-stop, wiring with separated power and signal routes.

## 1.4 Environment and robustness

- Indoor/outdoor, dry conditions; splash-resistant, not waterproof.
- Survives a 300 mm drop onto its wheels (≈ 5 g load case) and 1.5 m/s frontal bumper impacts.
- Operating temperature 0–40 °C (PA-CF and PETG-CF parts; no PLA structural parts).
