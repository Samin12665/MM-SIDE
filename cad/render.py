"""Offscreen renders of ASM_CAR_COMPLETE (VTK): iso, exploded iso, top (chassis layout), side (sensor layout + FOV)."""
import math, os, sys
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE); sys.path.insert(0, os.path.join(HERE, "parts"))
import vtk
from vtk.util.numpy_support import numpy_to_vtk
import numpy as np
import build as B
from lib import M

OUT = os.path.join(HERE, "..", "out", "renders")

EXPLODE = [  # (prefix, dx, dy, dz)
    ("12_", 0, 0, 120), ("70_", 0, 0, 170), ("74_", 0, 0, 170), ("110_JETSON", 0, 0, 170), ("110_TEENSY", 0, 0, 170),
    ("110_IMU", 0, 0, 170), ("110_RC_", 0, 0, 170), ("84_", 0, 0, 260), ("82_", 0, 0, 290), ("110_RPLIDAR", 0, 0, 320),
    ("83_", 60, 0, 120), ("81_", 90, 0, 120), ("80_", 90, 0, 150), ("110_REALSENSE", 110, 0, 120), ("110_CAMERA", 110, 0, 150),
    ("90_", 90, 0, 0), ("110_TOF", 90, 0, 0), ("91_", -90, 0, 0), ("30_", -40, 0, 90), ("110_ESTOP", -40, 0, 140),
    ("11_", 0, 0, -60), ("110_BATTERY", 0, 190, 0), ("61_BATTERY_DOOR_L", 0, 110, 0), ("61_BATTERY_DOOR_R", 0, -110, 0),
    ("71_", 0, 0, 60), ("72_", 0, 0, 60), ("73_", 0, 0, 60), ("110_MOTOR_DRIVER", 0, 0, 80), ("110_DCDC", 0, 0, 80),
    ("110_FUSE", 0, 0, 80), ("110_BEC", 0, 0, 80), ("110_BUCK", 0, 0, 80), ("92_BODY_SIDE_SKIRT_L", 0, 60, 60),
    ("92_BODY_SIDE_SKIRT_R", 0, -60, 60), ("24_", 20, 0, 60), ("110_SHOCK_F", 20, 0, 60),
]


def side_of(name):
    return 1 if name.endswith(("_L", "L1", "L2", "FL", "RL")) or "_FL" in name or "_RL" in name else (-1 if name.endswith(("_R", "R1", "R2", "FR", "RR")) or "_FR" in name or "_RR" in name else 0)


def explode_offset(n):
    for pre, dx, dy, dz in EXPLODE:
        if n.startswith(pre):
            return (dx, dy, dz)
    s = side_of(n)
    if n.startswith(("21_", "22_", "23_", "110_WHEEL_F", "110_HEX12_F", "110_AXLE", "110_BRG", "110_CAMBER", "110_TIE")):
        return (40, s * 90, 0)
    if n.startswith(("31_", "32_", "110_WHEEL_R", "110_GEARMOTOR", "110_POLOLU", "110_SHOCK_R")):
        return (-40, s * 90, 0)
    return (0, 0, 0)


def polydata(shape, tol=0.4):
    vs, ts = shape.val().tessellate(tol, 0.3)
    pts = vtk.vtkPoints()
    pts.SetData(numpy_to_vtk(np.array([(v.x, v.y, v.z) for v in vs], dtype=float), deep=True))
    cells = vtk.vtkCellArray()
    for t in ts:
        cells.InsertNextCell(3, t)
    pd = vtk.vtkPolyData(); pd.SetPoints(pts); pd.SetPolys(cells)
    n = vtk.vtkPolyDataNormals(); n.SetInputData(pd); n.SetFeatureAngle(35); n.Update()
    return n.GetOutput()


def color(n):
    if n in B.PRINT:
        return B.COLOR.get(B.mat_key(B.PRINT[n][0]), (0.6, 0.6, 0.6))
    if n.startswith("110_WHEEL"):
        return (0.07, 0.07, 0.07)
    if n.startswith(("110_JETSON", "110_TEENSY", "110_MOTOR_DRIVER", "110_DCDC", "110_FUSE", "110_BEC", "110_BUCK", "110_IMU", "110_RC_", "110_TOF")):
        return (0.1, 0.45, 0.2)
    if n.startswith("110_BATTERY"):
        return (0.15, 0.3, 0.75)
    if n.startswith(("110_REALSENSE", "110_RPLIDAR", "110_CAMERA")):
        return (0.2, 0.2, 0.22)
    if n.startswith("110_ESTOP"):
        return (0.85, 0.1, 0.1)
    if n.startswith("110_SHOCK"):
        return (0.85, 0.65, 0.1)
    return (0.72, 0.74, 0.78)


def render(shapes, fname, view, explode=False, extra=None, size=(1600, 1100)):
    ren = vtk.vtkRenderer(); ren.SetBackground(1, 1, 1)
    for n, s in shapes.items():
        pd = polydata(s)
        m = vtk.vtkPolyDataMapper(); m.SetInputData(pd)
        a = vtk.vtkActor(); a.SetMapper(m)
        a.GetProperty().SetColor(*color(n)); a.GetProperty().SetSpecular(0.2)
        if explode:
            a.SetPosition(*explode_offset(n))
        ren.AddActor(a)
        # silhouette edges for legibility
        fe = vtk.vtkFeatureEdges(); fe.SetInputData(pd); fe.BoundaryEdgesOn(); fe.FeatureEdgesOn(); fe.SetFeatureAngle(40)
        fe.ManifoldEdgesOff(); fe.NonManifoldEdgesOff()
        em = vtk.vtkPolyDataMapper(); em.SetInputConnection(fe.GetOutputPort()); em.ScalarVisibilityOff()
        ea = vtk.vtkActor(); ea.SetMapper(em); ea.GetProperty().SetColor(0.1, 0.1, 0.1); ea.GetProperty().SetLineWidth(1.0)
        if explode:
            ea.SetPosition(*explode_offset(n))
        ren.AddActor(ea)
    for (p0, p1, col) in (extra or []):
        ls = vtk.vtkLineSource(); ls.SetPoint1(*p0); ls.SetPoint2(*p1)
        lm = vtk.vtkPolyDataMapper(); lm.SetInputConnection(ls.GetOutputPort())
        la = vtk.vtkActor(); la.SetMapper(lm); la.GetProperty().SetColor(*col); la.GetProperty().SetLineWidth(3)
        ren.AddActor(la)
    cam = ren.GetActiveCamera()
    cam.SetFocalPoint(0, 0, 90)
    pos = {"iso": (650, 520, 430), "top": (0, 0, 1400), "side": (0, -1400, 110), "front": (1400, 0, 110), "iso_rear": (-650, -520, 430)}[view]
    cam.SetPosition(*pos)
    cam.SetViewUp(*( (1, 0, 0) if view == "top" else (0, 0, 1)))
    if view in ("top", "side", "front"):
        cam.ParallelProjectionOn()
    ren.ResetCamera()
    cam.Zoom(1.25 if not explode else 1.05)
    light = vtk.vtkLight(); light.SetPosition(600, -300, 900); light.SetFocalPoint(0, 0, 0); ren.AddLight(light)
    win = vtk.vtkRenderWindow(); win.SetOffScreenRendering(1); win.AddRenderer(ren); win.SetSize(*size)
    win.Render()
    w2i = vtk.vtkWindowToImageFilter(); w2i.SetInput(win); w2i.Update()
    wr = vtk.vtkPNGWriter(); wr.SetFileName(os.path.join(OUT, fname)); wr.SetInputConnection(w2i.GetOutputPort()); wr.Write()
    print("wrote", fname, flush=True)


def _clip(p0, p1):
    """Clip a ray at the ground plane (Z = 0)."""
    if p1[2] >= 0:
        return p1
    t = p0[2] / (p0[2] - p1[2])
    return tuple(a + t * (b - a) for a, b in zip(p0, p1))


def fov_lines():
    L = 650
    out = []
    cx, cz, ct = M.cameraX, M.cameraZ, math.radians(M.cameraTilt)
    for a in (-45, 45):   # vertical FOV of the 160 deg wide lens ~ +/-45
        t = ct - math.radians(a)
        p0 = (cx, -60, cz); out.append((p0, _clip(p0, (cx + L * math.cos(t), -60, cz - L * math.sin(t))), (0.9, 0.5, 0.0)))
    dx, dz, dt = M.depthX, M.depthZ, math.radians(M.depthTilt)
    for a in (-29, 29):   # D435 depth vertical FOV 58 deg
        t = dt - math.radians(a)
        p0 = (dx, -60, dz); out.append((p0, _clip(p0, (dx + L * math.cos(t), -60, dz - L * math.sin(t))), (0.1, 0.4, 0.9)))
    out.append(((-500, -60, M.lidarScanZ), (700, -60, M.lidarScanZ), (0.8, 0.0, 0.0)))
    out.append(((-500, -60, 0), (800, -60, 0), (0.3, 0.3, 0.3)))
    return out


def main():
    os.makedirs(OUT, exist_ok=True)
    parts = B.build_static()
    shapes, _ = B.assemble_state(parts)
    render(shapes, "ASM_CAR_COMPLETE_iso.png", "iso")
    render(shapes, "ASM_CAR_COMPLETE_iso_rear.png", "iso_rear")
    render(shapes, "ASM_CAR_COMPLETE_exploded.png", "iso", explode=True, size=(1800, 1300))
    hide = ("12_", "70_", "74_", "84_", "82_", "110_JETSON", "110_TEENSY", "110_IMU", "110_RC_", "110_RPLIDAR", "92_", "83_", "81_", "80_", "110_REALSENSE", "110_CAMERA")
    render({n: s for n, s in shapes.items() if not n.startswith(hide)}, "CHASSIS_LAYOUT_top.png", "top")
    render(shapes, "SENSOR_LAYOUT_side.png", "side", extra=fov_lines())
    render(shapes, "FRONT_view.png", "front")
    sh, st = B.assemble_state(parts, (M.bumpTravel, -M.droopTravel), (M.bumpTravel, -M.droopTravel), 1.0)
    render(sh, "MOTION_full_lock_articulated.png", "front")
    render({n: s for n, s in sh.items() if not n.startswith(hide + ("30_", "110_ESTOP", "10_", "11_", "61_", "110_BATTERY", "71_", "72_", "73_", "110_MOTOR_DRIVER", "110_DCDC", "110_FUSE", "110_BEC", "110_BUCK", "110_TOF", "90_", "91_", "110_AL_RAIL", "24_"))},
           "STEERING_full_lock_top.png", "top")


if __name__ == "__main__":
    main()
