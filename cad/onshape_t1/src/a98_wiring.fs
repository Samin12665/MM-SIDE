
// =====================================================================================================
//  55_WIRING  (harness geometry: bundle centre-lines as tube chains through the modelled pass-throughs)
//  70_LIDAR_OPTION  (low rear-roof RPLIDAR A1 mount: scan plane above every other body feature)
// =====================================================================================================
// tube chain through points (mm), diameter d; segments overlap at the bends
function harness(context is Context, id is Id, pts is array, d is number, name is string, pn is string, desc is string, c is Color)
{
    var B = bld(id);
    trace(context, "harness " ~ name);
    for (var i = 0; i + 1 < size(pts); i += 1)
    {
        const a = pt(pts[i]);
        const b = pt(pts[i + 1]);
        const u = normalize(b - a);
        cyl(context, B, arr(a - u * d * 0.45 * millimeter), arr(b + u * d * 0.45 * millimeter), d / 2, true);
    }
    fin(context, B, name, pn, desc, ArvMat.RUBBER);
    setProperty(context, { "entities" : posQ(B), "propertyType" : PropertyType.APPEARANCE, "value" : c });
}

// printed snap clip around a bundle: ring + foot (M3 screw into the named host)
function cableClip(context is Context, id is Id, p is array, axis is Vector, d is number, footDir is Vector, name is string)
{
    var B = bld(id);
    const cs = coordSystem(v(p[0], p[1], p[2]), normalize(cross(axis, footDir)), normalize(axis));
    cz(context, B, cs, 0, 0, d / 2 + 1.6, -3, 3, true);
    const fcs = coordSystem(v(p[0], p[1], p[2]) + normalize(footDir) * (d / 2 + 2.2) * millimeter, normalize(cross(axis, footDir)), normalize(axis));
    bx(context, B, fcs, -5, 5, -1.2, 1.2, -3, 3, true);
    stg(context, B);
    cz(context, B, cs, 0, 0, d / 2 + 0.2, -4, 4, false);
    fin(context, B, name, "ARV-55-001", "Printed cable clip (PETG), holds the bundle on its route", ArvMat.PETG);
}

export function harnessRoutes(P is map, g is map) returns array
{
    const k = pack(P, g);
    const xr = g.xTail + 3;
    const zc = g.tunZ1 + g.tunWall + 1.8;          // tunnel roof channel
    const zp = P.cameraHeight;
    const rz = g.zRoof - (g.zRoof - g.zHB - 5) * (P.cameraX - 30 - (g.xF - 30)) / (g.xNose - (g.xF - 30));
    var r = [];
    // [name, points, dia, colour, description]
    r = append(r, ["HARNESS_MAIN_POWER", [[xr + 7, -26, 113.5], [xr + 20, -34, 113.5], [-62, -34, 113.5], [-62, -34, 104], [-62, -34, 50], [-30, 4, 46], [40, 6, 46], [58, 22, 50]], 6, color(0.75, 0.1, 0.08), "XT60 pigtail 12 AWG pair: bay corner (clear of the battery exit) -> along the tunnel outside wall (2 clips) -> tray corridor -> 15 A main fuse"]);
    for (var s in [1, -1])
    {
        const t = s > 0 ? "L" : "R";
        r = append(r, ["HARNESS_MOTOR_" ~ t, [[g.xR, s * (g.faceY - 83.5), g.zA + 6], [g.xR, s * 7, 60], [g.X0 - 6, s * 7, 60], [-60, s * 7, 62], [-24, s * 7, 64], [-20, -20 - 10 * (1 - s), 64]], 5, color(0.1, 0.1, 0.1), "Gearmotor power (2x 18 AWG) + encoder (6-wire) in sleeve: motor end cap -> tub rear wall slot -> MDD10A / Teensy (service loop at the arm)"]);
    }
    r = append(r, ["HARNESS_SERVO", [[g.servoX - 11.5, -26, 50], [170, -26, 48.15], [152, -33, 48.15], [136, -33, 48.15], [125, -15, 47], [113, 0, 45], [100, 0, 58], [88, 0, 58]], 3.5, color(0.9, 0.5, 0.1), "Servo lead (3-wire) under the nose box and the tower plate, over the drag link, through the bulkhead corridor to the 6 V BEC / Teensy"]);
    r = append(r, ["HARNESS_TOF_FRONT", [[k.TOF_X - 18, 44, 92], [k.TOF_X - 13, 0, 94], [k.TOF_X - 18, -44, 92], [190, -46, 92], [(g.towerF0 + g.towerF1) / 2, -44, 88], [118, -48, 92], [100, -42, 100], [-2, -42, 100], [-10, -37.5, 100]], 4, color(0.2, 0.4, 0.9), "I2C + 3V3 daisy chain of the 3 front VL53L1X (XSHUT lines) through the tower window and bay pass-through to the Teensy"]);
    r = append(r, ["HARNESS_TOF_REAR", [[g.xTail - 16, 0, P.rearSensorZ + 1], [g.xTail - 5, 0, 62], [-120, 0, 62], [-45, 0, 62], [g.deckX0 + 60, 52, 70], [g.deckX0 + 60, 52, 95], [7.2, 45, 97], [7.2, 37.5, 100]], 3, color(0.2, 0.4, 0.9), "Rear VL53L1X: bumper channel -> tub rear slot -> deck pass-through -> Teensy"]);
    r = append(r, ["HARNESS_CAMERA_USB", [[P.cameraX, -49, zp], [P.cameraX - 20, -49, zp - 2], [P.cameraX - 30, 0, rz + 2], [P.cameraX - 30, 0, rz - 8], [118, 0, 121], [106, 10, 112]], 5, color(0.1, 0.1, 0.1), "RealSense D435i USB-C -> Jetson USB 3 (roof pass-through under the pod, bay front-wall grommet)"]);
    r = append(r, ["HARNESS_CAMERA_CSI", [[P.cameraX - 1.5, k.CAM_Y, zp], [P.cameraX - 20, 45, zp - 2], [P.cameraX - 30, 3, rz + 2], [P.cameraX - 30, 3, rz - 8], [118, -6, 121], [106, -12, 112]], 3, color(0.85, 0.85, 0.85), "IMX219 CSI-2 flat cable (15/22-pin adapter) -> Jetson CAM0"]);
    r = append(r, ["HARNESS_ESTOP", [[k.ESTOP_X, k.ESTOP_Y, g.zRoof - 41], [k.ESTOP_X, 40, 100], [-120, 40, 100], [-90, 40, 100], [-90, 40, 60], [-80, 30, 46]], 4, color(0.8, 0.1, 0.1), "E-stop NC contact pair -> watchdog relay (motor branch)"]);
    r = append(r, ["HARNESS_JETSON_POWER", [[30, 50, 64], [g.deckX0 + 60, 56, 70], [g.deckX0 + 60, 56, 95], [15, 43, 98]], 4, color(0.75, 0.1, 0.08), "19 V DC-DC -> Jetson DC barrel jack"]);
    return r;
}

function buildWiring(context is Context, id is Id, P is map, g is map)
{
    link(context, "LNK_WIRING");
    var i = 0;
    for (var h in harnessRoutes(P, g))
    {
        try
        {
            harness(context, id + ("h" ~ i), h[1], h[2], h[0], "ARV-55-" ~ (100 + i), h[4], h[3]);
        }
        catch (e)
        {
            recordError(context, id, h[0], e);
        }
        i += 1;
    }
    // cable clips: tunnel roof channel (pigtail), tub wall (E-stop), bulkhead corridor (servo), tray corridor (motor)
    const zc = g.tunZ1 + g.tunWall + 1.8;
    const clips = [[[-150, -34, 113.5], X, 6, Y], [[-100, -34, 113.5], X, 6, Y], [[-105, 40, 100], X, 4, -Z], [[125, 0, 52], normalize(vector(-12, 0, -7)), 3.5, -Z], [[-40, 8, 62], X, 5, -Z]];
    var n = 0;
    for (var c in clips)
    {
        try
        {
            cableClip(context, id + ("c" ~ n), c[0], c[1], c[2], c[3], "CABLE_CLIP_" ~ (n + 1));
        }
        catch (e)
        {
            recordError(context, id, "clip", e);
        }
        n += 1;
    }
}

// ---------------------------------------------------------------- optional LiDAR (rear roof, low profile)
export function lidarPos(P is map, g is map) returns array
{
    return [-118, 0, g.zRoof];
}

export function lidarScrews(P is map, g is map) returns array
{
    const l = lidarPos(P, g);
    return [[l[0] - 40, 30], [l[0] - 40, -30], [l[0] + 40, 30], [l[0] + 40, -30]];
}

function buildLidar(context is Context, id is Id, P is map, g is map)
{
    link(context, "LNK_LIDAR");
    const l = lidarPos(P, g);
    try
    {
        var B = bld(id + "plate");
        trace(context, "lidar plate");
        pr(context, B, WCS(), [[l[0] - 50, -38], [l[0] + 42, -38], [l[0] + 50, -30], [l[0] + 50, 30], [l[0] + 42, 38], [l[0] - 50, 38]], l[2], l[2] + 4, true);
        stg(context, B);
        for (var p in lidarScrews(P, g))
        {
            hClr(context, B, [p[0], p[1], l[2] - 1], [p[0], p[1], l[2] + 5]);
            cyl(context, B, [p[0], p[1], l[2] + 2.5], [p[0], p[1], l[2] + 5], 3.1, false);
        }
        // A1M8 base holes (slotted +-2 mm: VERIFY) and cable pass-through
        for (var dx in [-30, 30])
            for (var dy in [-25, 25])
                slot(context, B, WCS(), [l[0] + dx - 2, dy], [l[0] + dx + 2, dy], 2.8, l[2] - 1, l[2] + 5, false);
        cax(context, B, "Z", [l[0], 0, l[2] + 2], 12, 10, false);
        fin(context, B, "LIDAR_ADAPTER_PLATE", "ARV-70-001", "Optional RPLIDAR A1M8 adapter plate on the rear roof (4x M3 into roof inserts, slotted lidar holes, cable pass-through). PETG-CF.", ArvMat.PETGCF);
        var n = 0;
        for (var p in lidarScrews(P, g))
        {
            screw(context, id + ("s" ~ n), [p[0], p[1], l[2] + 2.5], [0, 0, -1], 10, 3, true);
            n += 1;
        }
        var Ld = bld(id + "lidar");
        wb(context, Ld, l[0] - 48.4, l[0] + 48.4, -35.15, 35.15, l[2] + 4, l[2] + 34, true);
        cyl(context, Ld, [l[0] + 13, 0, l[2] + 34], [l[0] + 13, 0, l[2] + 59], 35, true);
        stg(context, Ld);
        fin(context, Ld, "RPLIDAR_A1M8", "PUR-SNS-RPLIDAR-A1", "Slamtec RPLIDAR A1M8 (optional), scan plane ~Z " ~ round(l[2] + 4 + 38) ~ " mm, clear of pod and E-stop", ArvMat.PCB);
        harness(context, id + "hl", [[l[0], 0, l[2] + 3], [l[0], 0, l[2] - 6], [l[0], -40, l[2] - 6], [l[0] + 20, -44, 124], [-40, -46, 120], [10, -42, 118]], 4, "HARNESS_LIDAR", "ARV-70-100", "RPLIDAR USB -> Jetson (roof grommet)", color(0.1, 0.1, 0.1));
    }
    catch (e)
    {
        recordError(context, id, "LIDAR", e);
    }
}
