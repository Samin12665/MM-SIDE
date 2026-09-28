"""
00_CAR_MASTER  --  single source of truth for the ARV-1 autonomous research vehicle.

Every downstream part, analysis script, drawing and the Onshape FeatureScript master
reads its controlling geometry from here. Downstream code must NOT re-declare any of
these values; derive from them instead.

Coordinate system (vehicle frame, ISO 8855-like, also used in Onshape):
    X  forward, Y  left, Z  up
    Origin: ground plane, vehicle centreline, mid-wheelbase.
All lengths in millimetres, angles in degrees, masses in kg.

Values tagged VERIFY are taken from typical vendor data from memory and must be
checked against the purchased part (or its datasheet) before printing the
parts that consume them. They are grouped in PURCHASED so they are easy to audit.
"""
from dataclasses import dataclass, field, asdict
import math

# ---------------------------------------------------------------------------
# SK00 / SK03  GLOBAL DATUMS, WHEELBASE AND TRACK
# ---------------------------------------------------------------------------
wheelbase = 300.0
frontTrack = 250.0            # wheel centre-plane to centre-plane
rearTrack = 250.0
wheelDiameter = 85.0          # 1/10 buggy/rally tyre, 12 mm hex
wheelWidth = 35.0
rimDiameter = 56.0            # 2.2" rim, clearance envelope for uprights
axleHeight = wheelDiameter / 2  # static, tyre deflection neglected (~1 mm)

xFrontAxle = +wheelbase / 2
xRearAxle = -wheelbase / 2

# ---------------------------------------------------------------------------
# SK01 / SK02  VEHICLE ENVELOPES
# ---------------------------------------------------------------------------
groundClearance = 25.0        # underside of skid at static ride height
overallLength = 473.0         # bumper to bumper (+235 / -238)
overallWidth = frontTrack + wheelWidth   # 285
overallHeightNoLidar = 200.0
overallHeightLidar = 262.0

# Main chassis tub
chassisLength = 230.0         # tub only (fits 256 mm printer bed)
chassisWidth = 152.0
chassisXmin = -115.0
chassisXmax = chassisXmin + chassisLength   # +115
chassisThickness = 3.0        # floor
wallThickness = 3.0
skidThickness = 2.0           # separate sacrificial skid (11_LOWER_SKID)
floorZ = groundClearance + skidThickness     # top of skid / underside of tub floor = 27
tubWallHeight = 55.0          # floor underside to top of wall
tubTopZ = floorZ + tubWallHeight              # 82

# Aluminium longerons (10x10x1 6063 square tube) clamped along tub top edges
railSize = 10.0
railWall = 1.0
railLength = 238.0
railY = chassisWidth / 2 - railSize / 2       # rail centre, Y = +/-71
railZ = tubTopZ - railSize / 2                # rail centre Z = 77
railXmin = -120.0                              # rails stop at the tub so rear motors can rise at full bump
railXmax = railXmin + railLength              # +118: rails end at the tub front wall (front shocks pass outboard)

# ---------------------------------------------------------------------------
# SK04  FRONT SUSPENSION  (double wishbone, ball-joint knuckle -- touring-car style)
#   lower joint: M3 ball stud in knuckle -> purchased M3 ball end threaded into printed lower arm
#   upper joint: M3 ball stud in knuckle -> turnbuckle camber link (adjustable camber)
#   kingpin axis = line through the two ball centres (5 deg caster, KPI 0)
# Points are LEFT side (+Y); right side is mirrored.
# ---------------------------------------------------------------------------
kingpinY = frontTrack / 2 - 12.0              # scrub radius 12 mm at hub
casterAngle = 5.0
scrubRadius = frontTrack / 2 - kingpinY
kingpinLowerZ = axleHeight - 16.0              # lower ball centre, Z = 26.5
kingpinUpperZ = axleHeight + 17.5              # upper ball centre, Z = 60
frontLowerOuterY = kingpinY                    # lower ball
frontLowerOuterZ = kingpinLowerZ
lowerBallZ = frontLowerOuterZ                  # legacy alias
frontLowerInnerY = 34.0
frontLowerInnerZ = frontLowerOuterZ            # arm horizontal at ride height
frontLowerArmLength = frontLowerOuterY - frontLowerInnerY   # 79 (to ball centre)
frontLowerPivotSpan = 40.0                     # front-to-rear hinge block spacing (centres)
upperBallZ = kingpinUpperZ
frontUpperOuterY = kingpinY
frontUpperInnerY = 56.0                        # link 57 mm vs arm 79 mm -> -1.4 deg camber at full bump
frontUpperInnerZ = upperBallZ
frontShockX = xFrontAxle - 23.0                # shock plane X = 127, 12 mm behind tower plate (M3 ball stud + 7 mm spacer)
frontShockUpperX = frontShockX
frontShockLowerX = frontShockX
frontShockLowerY = 80.0                        # on lower-arm tab
frontShockLowerZ = frontLowerOuterZ + 7.0
frontShockUpperY = 80.0                        # vertical shock (front view): max tyre clearance at full lock
frontShockUpperZ = 103.0
frontTowerX = (139.0, 144.0)                   # front shock tower plate; camber-link stud points forward to X = 150
frontSkidZ = 18.0                              # underside of front bulkhead (front clearance 18 mm)
bulkheadFloorZ = (frontSkidZ, frontSkidZ + 3.5)

# ---------------------------------------------------------------------------
# SK05  REAR SUSPENSION (independent trailing arm, each arm carries its drive motor)
# ---------------------------------------------------------------------------
rearArmPivotX = xRearAxle + 118.0              # -32
rearArmPivotZ = axleHeight + 6.0               # 48.5 (slight anti-squat)
rearArmY = 92.0                                # arm beam centre plane
rearArmBeamWidth = 18.0
rearArmLength = math.hypot(xRearAxle - rearArmPivotX, axleHeight - rearArmPivotZ)
rearShockLowerX = xRearAxle + 36.0             # -114 on arm
rearShockLowerZ = axleHeight + 20.0             # ball stud on top of arm beam
rearShockUpperX = xRearAxle + 58.0             # -92 on rear tower
rearShockUpperZ = 130.2
rearShockY = 86.0                              # on arm beam centre plane
rearTowerX = (-83.0, -78.0)                    # rear tower plate; shock studs 9 mm long clear the shock body

# Shocks (purchased 1/10 oil shocks, VERIFY on receipt)
shockEyeToEyeExtended = 80.0
shockStroke = 22.0
shockBodyDiameter = 16.0                       # over spring

# ---------------------------------------------------------------------------
# SK14  GROUND CLEARANCE AND TRAVEL
# ---------------------------------------------------------------------------
suspensionTravel = 30.0        # total wheel travel
bumpTravel = 18.0
droopTravel = 12.0

# ---------------------------------------------------------------------------
# SK06  STEERING (front-steer layout, servo ahead of axle, direct horn linkage)
# ---------------------------------------------------------------------------
steeringAngle = 26.0           # inner wheel mechanical lock (28 deg grazed tyre/shock at full bump, see motion study)
steeringArmLength = 20.0       # kingpin axis to tie-rod ball, in plan
steeringArmAngle = 3.03        # outward angle of arm from +X (solved: analysis/front_kinematics.py)
steeringArmZ = axleHeight - 6.0
# Dual bellcranks on vertical M3 shoulder bolts, joined by a drag link (parallelogram)
crankPivotX = xFrontAxle + 26.0          # 176
crankPivotY = 48.9
crankPivotZ = 36.5
crankRadius = 18.0                        # pivot -> tie-rod ball
crankBeta = -18.35                        # tie-rod arm direction at centre (deg from +X, left side)
crankDragRadius = 16.0                    # pivot -> drag-link ball
crankDragBeta = 180.0                     # drag arms point straight rearward on BOTH cranks (true parallelogram)
crankLock = 33.3                          # crank rotation for 28 deg inner lock
tieRodLength = 74.35                      # ball-centre to ball-centre
servoX = xFrontAxle + 58.0                # servo output shaft X (208)
servoOutputZ = 46.0                       # servo mounted inverted: horn below body
servoHornRadius = 18.0

# ---------------------------------------------------------------------------
# SK07  DRIVETRAIN  (dual independent rear gearmotors, electronic differential)
# ---------------------------------------------------------------------------
gearRatio = 19.0 * 1.0         # 19:1 gearbox, direct to wheel (no belt)
motorFaceY = rearTrack / 2 - wheelWidth / 2 - 15.0    # gearbox face plane, Y = 92.5

# ---------------------------------------------------------------------------
# SK08  BATTERY  (transverse, side-loading tunnel through tub)
# ---------------------------------------------------------------------------
batteryLength = 139.0          # along Y (transverse)
batteryWidth = 47.0            # along X
batteryHeight = 37.0
batteryClearance = 1.5
batteryX = 30.0                # centre of battery tunnel
batteryZ = floorZ + chassisThickness + batteryHeight / 2 + 0.5

# ---------------------------------------------------------------------------
# SK09  COMPUTE AND ELECTRONICS
# ---------------------------------------------------------------------------
electronicsDeckHeight = 92.0   # underside of upper deck
deckThickness = 4.0
deckStandoff = electronicsDeckHeight - tubTopZ    # 10 mm wiring gap
deckLength = 208.0
deckWidth = 160.0
deckXmin = -76.0                               # clears rear shock tower plate (X -83..-78)

# ---------------------------------------------------------------------------
# SK10  SENSOR LAYOUT
# ---------------------------------------------------------------------------
cameraX = 178.0
cameraZ = 168.0                # optical centre of main camera
cameraTilt = 12.0              # pitch down
depthX = 172.0
depthZ = 140.0                 # optical centre of depth camera
depthTilt = 5.0
sensorMastHeight = 262.0       # top of LiDAR
lidarX = -40.0
lidarScanZ = 238.0             # scan plane
imuX = 5.0                     # near CG in X/Y, on deck top
imuY = 0.0
tofZ = 55.0                    # bumper ToF/ultrasonic centreline

# ---------------------------------------------------------------------------
# SK11  BODY AND PROTECTION
# ---------------------------------------------------------------------------
frontBumperX = 235.0
rearBumperX = -238.0
bumperWidth = 200.0
bumperHeightZ = (35.0, 85.0)

# ---------------------------------------------------------------------------
# SK13  MOUNTING INTERFACE STANDARD  ("ARV grid")
# ---------------------------------------------------------------------------
gridPitch = 20.0               # M3 heat-set insert grid on decks & mast plates
boltClearance = 3.4            # M3 clearance (print-compensated)
boltClearanceM4 = 4.5
boltClearanceM5 = 5.5
insertDiameter = 4.0           # hole for M3 x 5.7 heat-set insert (OD 4.6-4.7)
insertDepth = 6.0
insertDiameterM4 = 5.6
insertDiameterM5 = 6.4
counterboreM3 = (6.0, 3.2)     # dia, depth
bearingFitAllowance = 0.10     # printed bore oversize for press fit (PA-CF, tune with test coupon)

# ---------------------------------------------------------------------------
# PRINTER
# ---------------------------------------------------------------------------
printerBed = (256.0, 256.0, 256.0)


@dataclass
class Purchased:
    name: str
    dims: tuple
    mass: float
    verify: bool
    note: str = ""


PURCHASED = {
    # drivetrain
    "motor": Purchased("Pololu 37D 19:1 metal gearmotor 12V, 64 CPR encoder (#4751)",
                       (37.0, 82.0), 0.215, True,
                       "D37 x L82 incl. encoder cap, 6 mm D-shaft 22 mm long, 6x M3 on 31 mm PCD, 12 mm spigot"),
    "motor_hub": Purchased("Pololu universal Al hub 6 mm bore, M3 holes (#1999)", (22.0, 11.0), 0.008, True,
                           "4x M3 on 19.05 mm PCD (VERIFY), set screws on 6 mm D-shaft"),
    # steering
    "servo": Purchased("Standard-size 20 kg-cm digital servo (DS3218 class)", (40.5, 20.0, 38.5), 0.060, True,
                       "tabs 54.5 wide, holes 49.5 x 10 mm, 25T spline"),
    "servo_saver": Purchased("25T aluminium servo saver / horn", (30.0, 8.0), 0.010, True, ""),
    # suspension
    "shock": Purchased("1/10 aluminium oil shock 80 mm (eye-eye extended)", (16.0, 80.0), 0.020, True,
                       "stroke ~22 mm, M3 eyes/ball ends"),
    "turnbuckle": Purchased("M3 steel turnbuckle + 6.8 mm ball ends", (3.0, 50.0), 0.006, False, ""),
    "bearing_5x11x4": Purchased("685ZZ 5x11x4 ball bearing", (5.0, 11.0, 4.0), 0.002, False, ""),
    "hex12": Purchased("12 mm wheel hex, 5 mm bore, 6 mm thick", (12.0, 6.0), 0.003, False, ""),
    "wheel": Purchased("1/10 buggy/rally wheel+tyre 85x35, 12 mm hex, 5 mm bore", (85.0, 35.0), 0.070, True, ""),
    # power
    "battery": Purchased("3S 11.1 V 5000 mAh hard-case LiPo, XT60", (139.0, 47.0, 37.0), 0.380, True,
                         "size varies by brand: max 140 x 48 x 38 accepted by tunnel"),
    # compute / electronics
    "compute": Purchased("NVIDIA Jetson Orin Nano Super Developer Kit", (103.0, 90.5, 34.8), 0.250, True,
                         "carrier mounting holes treated as slotted adapter, nominal 86 x 58 M2.5 (VERIFY)"),
    "mcu": Purchased("Teensy 4.1 on 70x50 perfboard carrier", (70.0, 50.0, 18.0), 0.030, False,
                     "perfboard holes 66 x 46, M2"),
    "motor_driver": Purchased("Cytron MDD10A dual 10 A motor driver", (84.0, 62.0, 20.0), 0.060, True,
                              "4 holes, envelope and hole pattern VERIFY; tray uses slots"),
    "buck_5v": Purchased("5 V 5 A buck (Pololu D36V50F5 class)", (35.6, 38.1, 10.0), 0.010, True, ""),
    "buck_servo": Purchased("6.0 V 5 A BEC / buck for servo", (40.0, 25.0, 12.0), 0.015, True, ""),
    "dcdc_compute": Purchased("Buck-boost 19 V >=60 W for Jetson", (60.0, 50.0, 22.0), 0.060, True, ""),
    "imu": Purchased("BNO085 9-DoF IMU breakout (Adafruit 4754 class)", (25.4, 17.8, 4.0), 0.003, True, ""),
    "receiver": Purchased("SBUS RC receiver (FrSky/RadioMaster class)", (40.0, 22.0, 10.0), 0.008, True, ""),
    "estop": Purchased("22 mm latching mushroom E-stop, NC, 10 A", (40.0, 40.0, 50.0), 0.050, False,
                       "22.3 mm panel hole, max 6 mm panel"),
    "fuse_block": Purchased("2-way ATO blade fuse holder", (55.0, 40.0, 25.0), 0.030, True, ""),
    # sensors
    "depth_cam": Purchased("Intel RealSense D435i", (90.0, 25.0, 25.0), 0.072, False,
                           "2x M3 on rear face, 45 mm apart; 1/4-20 bottom"),
    "camera": Purchased("IMX219 160 deg wide camera (RPi Cam v2 form factor)", (25.0, 24.0, 12.0), 0.004, False,
                        "4x M2 holes 21 x 12.5 mm"),
    "lidar": Purchased("Slamtec RPLIDAR A1M8 (optional)", (96.8, 70.3, 55.0), 0.170, True,
                       "adapter plate slotted; scan plane ~ 38 mm above base (VERIFY)"),
    "tof": Purchased("VL53L1X ToF breakout", (13.0, 18.0, 4.0), 0.002, True, ""),
    "wheel_encoder": Purchased("AS5600 magnetic encoder breakout + 6x2.5 diametric magnet", (23.0, 23.0, 5.0), 0.003, True, ""),
}


def derived():
    """Quantities derived from master values (never hand-copied downstream)."""
    d = {}
    d["rearArmLength"] = rearArmLength
    d["overallWidth"] = overallWidth
    d["tubTopZ"] = tubTopZ
    d["deckTopZ"] = electronicsDeckHeight + deckThickness
    d["frontLowerArmLength"] = frontLowerArmLength
    return d


def as_variable_table():
    """Flat dict of scalar master variables -> used for Onshape #variables and docs."""
    out = {}
    for k, v in globals().items():
        if k.startswith("_") or k in ("math", "Purchased", "PURCHASED", "dataclass", "field", "asdict"):
            continue
        if isinstance(v, (int, float)) and not isinstance(v, bool):
            out[k] = float(v)
    return out


if __name__ == "__main__":
    for k, v in as_variable_table().items():
        print(f"{k:28s} {v:10.3f}")
