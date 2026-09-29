from __future__ import annotations
from dataclasses import dataclass
from math import pi

JOINT_LIMITS = ((-pi,pi),(-.35*pi,.60*pi),(-.75*pi,.75*pi),(-pi,pi),(-.55*pi,.55*pi),(-pi,pi))

@dataclass(frozen=True)
class HandPose:
    x: float; y: float; z: float; roll: float; pitch: float; yaw: float; pinch: float; timestamp_s: float
@dataclass(frozen=True)
class ArmCommand:
    joints: tuple[float,float,float,float,float,float]
    gripper: float
    enabled: bool
    reason: str = "ok"

class LowPass:
    def __init__(self, alpha=.25):
        if not 0 < alpha <= 1: raise ValueError("alpha must be in (0, 1]")
        self.alpha, self.value = alpha, None
    def update(self, sample):
        self.value = sample if self.value is None else self.value+self.alpha*(sample-self.value)
        return self.value

class HandMapper:
    """Calibrated normalized-hand mapping with limits, filtering and timeout."""
    def __init__(self, timeout_s=.25, alpha=.25):
        self.timeout_s, self.filters = timeout_s, [LowPass(alpha) for _ in range(7)]
    @staticmethod
    def _clamp(v, lim): return max(lim[0], min(lim[1], v))
    def command(self, pose, now_s, estop=False):
        if estop: return ArmCommand((0.,)*6, 0., False, "estop")
        if pose is None or now_s-pose.timestamp_s > self.timeout_s:
            return ArmCommand((0.,)*6, 0., False, "tracking_timeout")
        raw = (pose.x*pi, pose.z*.60*pi, pose.y*.75*pi, pose.roll, pose.pitch, pose.yaw)
        joints = tuple(self._clamp(self.filters[i].update(v), JOINT_LIMITS[i]) for i,v in enumerate(raw))
        grip = self._clamp(self.filters[6].update(pose.pinch), (0.,1.))
        return ArmCommand(joints, grip, True)
