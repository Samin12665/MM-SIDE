import math, unittest
from software.arv1.control import DifferentialOdometry, DriveCommand, SafetyInputs, SafetySupervisor, ackermann_wheel_angles
from software.hcra.control import HandMapper, HandPose, JOINT_LIMITS

class Arv1Tests(unittest.TestCase):
    def test_ackermann(self):
        left,right=ackermann_wheel_angles(math.radians(20)); self.assertGreater(left,right); self.assertGreater(right,0)
    def test_watchdog_and_override(self):
        s=SafetySupervisor(); auto=DriveCommand(1.,.2)
        self.assertEqual(s.arbitrate(auto,SafetyInputs(0.,0.)),auto)
        manual=s.arbitrate(auto,SafetyInputs(.1,.1,rc_manual=True,rc_command=DriveCommand(-.2,-.1)))
        self.assertLess(manual.speed_mps,1.)
        self.assertAlmostEqual(s.arbitrate(auto,SafetyInputs(1.,0.)).speed_mps,0.)
    def test_estop_latches(self):
        s=SafetySupervisor(); c=DriveCommand(.5,0.)
        s.arbitrate(c,SafetyInputs(0.,0.,estop_closed=False)); self.assertFalse(s.arbitrate(c,SafetyInputs(.1,.1)).speed_mps)
        self.assertTrue(s.reset_estop(True))
    def test_odometry(self):
        x,y,yaw=DifferentialOdometry().update(1216,1216)
        self.assertAlmostEqual(x,2*math.pi*.0425); self.assertEqual(y,0.); self.assertEqual(yaw,0.)

class HcraTests(unittest.TestCase):
    def test_timeout_estop_limits(self):
        m=HandMapper(alpha=1.); self.assertFalse(m.command(None,1.).enabled)
        p=HandPose(9,9,9,9,9,9,9,1.); self.assertEqual(m.command(p,1.,estop=True).reason,"estop")
        c=m.command(p,1.); self.assertTrue(c.enabled); self.assertEqual(c.gripper,1.)
        for value,limits in zip(c.joints,JOINT_LIMITS): self.assertTrue(limits[0] <= value <= limits[1])

if __name__ == "__main__": unittest.main()
