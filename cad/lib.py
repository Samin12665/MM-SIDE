"""Shared CadQuery helpers for ARV-1. Geometry is modelled in the vehicle frame
(X fwd, Y left, Z up, origin ground/centreline/mid-wheelbase), top-down from CAR_MASTER."""
import os, sys
import cadquery as cq

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "master"))
import car_master as M  # noqa: E402


def box(x0, x1, y0, y1, z0, z1):
    return cq.Workplane("XY").box(x1 - x0, y1 - y0, z1 - z0, centered=False).translate((x0, y0, z0))


def cyl(d, length, center, axis="Z"):
    """Cylinder of diameter d, total length, centred at `center`, along axis X/Y/Z."""
    wp = {"X": "YZ", "Y": "XZ", "Z": "XY"}[axis]
    c = cq.Workplane(wp).circle(d / 2).extrude(length / 2, both=True)
    return c.translate(center)


def hole(body, d, length, center, axis="Z"):
    return body.cut(cyl(d, length, center, axis))


def insert_hole(body, center, axis="Z", size="M3", depth=None, sign=+1):
    """Heat-set insert pocket entering the body along -axis*sign from `center` (the surface point)."""
    dia = {"M3": M.insertDiameter, "M4": M.insertDiameterM4, "M5": M.insertDiameterM5}[size]
    depth = depth or {"M3": M.insertDepth, "M4": 8.0, "M5": 9.5}[size]
    off = [0, 0, 0]
    idx = "XYZ".index(axis)
    off[idx] = -sign * depth / 2
    c = (center[0] + off[0], center[1] + off[1], center[2] + off[2])
    return body.cut(cyl(dia, depth, c, axis))


def mirror_y(shape):
    return shape.mirror("XZ")


def rounded_slot(length, width, height, center, axis_len="X"):
    wp = cq.Workplane("XY").slot2D(length, width, 0 if axis_len == "X" else 90).extrude(height)
    return wp.translate((center[0], center[1], center[2] - height / 2))


def grid_points(x0, x1, y0, y1, pitch=None, margin=10.0):
    pitch = pitch or M.gridPitch
    import math
    pts = []
    nx = int(math.floor((x1 - x0 - 2 * margin) / pitch))
    ny = int(math.floor((y1 - y0 - 2 * margin) / pitch))
    cx, cy = (x0 + x1) / 2, (y0 + y1) / 2
    for i in range(nx + 1):
        for j in range(ny + 1):
            pts.append((cx + (i - nx / 2) * pitch, cy + (j - ny / 2) * pitch))
    return pts
