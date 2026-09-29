from __future__ import annotations
from dataclasses import dataclass
from math import atan, cos, pi, sin, tan

@dataclass(frozen=True)
class VehicleConfig:
    wheelbase_m: float = .300
    rear_track_m: float = .170
    wheel_radius_m: float = .0425
    encoder_counts_per_rev: int = 1216
    max_speed_mps: float = 2.0
    max_accel_mps2: float = 2.0
    max_steer_rad: float = 26*pi/180
    heartbeat_timeout_s: float = .200

@dataclass(frozen=True)
class DriveCommand:
    speed_mps: float
    steering_rad: float

@dataclass(frozen=True)
class SafetyInputs:
    now_s: float
    last_heartbeat_s: float
    estop_closed: bool = True
    rc_manual: bool = False
    rc_command: DriveCommand = DriveCommand(0., 0.)

def clamp(v, lo, hi): return max(lo, min(hi, v))

class SafetySupervisor:
    """Manual-always-wins arbitration, latched E-stop and heartbeat stop."""
    def __init__(self, config=VehicleConfig()):
        self.config, self.estop_latched = config, False
        self._last_speed, self._last_time = 0., None
    def reset_estop(self, estop_closed):
        if estop_closed: self.estop_latched = False
        return not self.estop_latched
    def arbitrate(self, autonomous, inputs):
        if not inputs.estop_closed: self.estop_latched = True
        alive = inputs.now_s-inputs.last_heartbeat_s <= self.config.heartbeat_timeout_s
        requested = (DriveCommand(0., 0.) if self.estop_latched else
                     inputs.rc_command if inputs.rc_manual else
                     autonomous if alive else DriveCommand(0., 0.))
        speed = clamp(requested.speed_mps, -self.config.max_speed_mps, self.config.max_speed_mps)
        steer = clamp(requested.steering_rad, -self.config.max_steer_rad, self.config.max_steer_rad)
        if self._last_time is not None:
            delta = self.config.max_accel_mps2*max(0., inputs.now_s-self._last_time)
            speed = clamp(speed, self._last_speed-delta, self._last_speed+delta)
        self._last_speed, self._last_time = speed, inputs.now_s
        return DriveCommand(speed, steer)

def ackermann_wheel_angles(center_angle, cfg=VehicleConfig()):
    delta = clamp(center_angle, -cfg.max_steer_rad, cfg.max_steer_rad)
    if abs(delta) < 1e-12: return 0., 0.
    r = cfg.wheelbase_m/tan(abs(delta))
    inner = atan(cfg.wheelbase_m/(r-cfg.rear_track_m/2))
    outer = atan(cfg.wheelbase_m/(r+cfg.rear_track_m/2))
    return (inner, outer) if delta > 0 else (-outer, -inner)

class DifferentialOdometry:
    def __init__(self, config=VehicleConfig()):
        self.cfg, self.x, self.y, self.yaw = config, 0., 0., 0.
    def update(self, left_counts, right_counts):
        mpc = 2*pi*self.cfg.wheel_radius_m/self.cfg.encoder_counts_per_rev
        dl, dr = left_counts*mpc, right_counts*mpc
        distance, dyaw = (dl+dr)/2, (dr-dl)/self.cfg.rear_track_m
        heading = self.yaw+dyaw/2
        self.x += distance*cos(heading); self.y += distance*sin(heading); self.yaw += dyaw
        return self.x, self.y, self.yaw
