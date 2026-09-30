
// =====================================================================================================
//  30_DRIVETRAIN  (ARV-1 independent trailing arms, face-mounted Pololu 37D 19:1 gearmotors, hex drive)
// =====================================================================================================
function beamZ(g is map, x is number) returns number
{
    return g.pivZ + (g.zA - g.pivZ) * (x - g.pivX) / (g.xR - g.pivX);
}

// rectangle (local YZ) at world X for lofts
function yzRect(context is Context, id is Id, x is number, yc is number, zc is number, w is number, h is number)
{
    const sk = newSketchOnPlane(context, id, { "sketchPlane" : plane(v(x, 0, 0), X, Y) });
    skRectangle(sk, "r", { "firstCorner" : vector(yc - w / 2, zc - h / 2) * millimeter, "secondCorner" : vector(yc + w / 2, zc + h / 2) * millimeter });
    skSolve(sk);
}

function pTrailingArm(context is Context, id is Id, P is map, g is map, s is number)
{
    var B = bld(id);
    const PX = g.pivX;
    const PZ = g.pivZ;
    const XR = g.xR;
    const ZA = g.zA;
    const FY = g.faceY;
    const y0 = g.HW + 3.5;
    const y1 = FY + 8.5;
    const cup0 = g.HW + 4;
    const pl1 = FY + 6;
    trace(context, "arm boss");
    cyl(context, B, [PX, y0, PZ], [PX, y1, PZ], 10, true);
    // lofted beam pivot -> cup
    const xs = PX - 6;
    const xe = XR + 17;
    yzRect(context, id + "s0", xs, (y0 + y1) / 2, beamZ(g, xs), y1 - y0, 22);
    yzRect(context, id + "s1", xe, (cup0 + FY) / 2, beamZ(g, xe) + 4, FY - cup0, 22);
    const lb = sub(B, true);
    trace(context, "arm loft");
    opLoft(context, lb, { "profileSubqueries" : [qSketchRegion(id + "s0"), qSketchRegion(id + "s1")], "bodyType" : ToolBodyType.SOLID });
    opDeleteBodies(context, id + "sd", { "entities" : qUnion([qCreatedBy(id + "s0", EntityType.BODY), qCreatedBy(id + "s1", EntityType.BODY)]) });
    // motor cup + face plate
    cyl(context, B, [XR, cup0, ZA], [XR, pl1, ZA], 23, true);
    // clamp ear
    wb(context, B, XR - 7, XR + 7, cup0, cup0 + 9, ZA + 21, ZA + 29, true);
    // shock ball-stud boss
    const sx = g.shockRLowX;
    const top = beamZ(g, sx) + 12;
    cyl(context, B, [sx, g.shockRY, top - 3], [sx, g.shockRY, g.shockRLowZ - 4], 5.5, true);
    stg(context, B);
    trace(context, "arm cuts");
    cyl(context, B, [XR, cup0 - 0.01, ZA], [XR, FY, ZA], 18.7, false);
    cyl(context, B, [XR, FY - 1, ZA], [XR, pl1 + 1, ZA], 6.2, false);
    for (var i = 0; i < 6; i += 1)
    {
        const t = (30 + 60 * i) * degree;
        const cx = XR + 15.5 * cos(t);
        const czz = ZA + 15.5 * sin(t);
        cyl(context, B, [cx, FY - 1, czz], [cx, pl1 + 1, czz], 1.7, false);
        cyl(context, B, [cx, pl1 - 2.5, czz], [cx, pl1 + 0.01, czz], 3.1, false);
    }
    wb(context, B, XR - 1, XR + 1, cup0 - 1, FY - 2, ZA + 18, ZA + 30, false);
    cyl(context, B, [XR - 8, cup0 + 4.5, ZA + 25.5], [XR + 8, cup0 + 4.5, ZA + 25.5], 1.7, false);
    cyl(context, B, [PX, y0 - 1, PZ], [PX, y1 + 1, PZ], 3.5, false);
    cyl(context, B, [PX, y0 - 0.01, PZ], [PX, y0 + 5.2, PZ], 6.55, false);
    cyl(context, B, [PX, y1 + 0.01, PZ], [PX, y1 - 5.2, PZ], 6.55, false);
    hIns(context, B, [sx, g.shockRY, g.shockRLowZ - 4], [0, 0, -1], 6);
    // beam lightening window
    const wx = PX - 45;
    const cs = coordSystem(v(0, 0, 0), X, -Y);
    slot(context, B, cs, [wx - 12.5, beamZ(g, wx)], [wx + 12.5, beamZ(g, wx)], 9, -y1 - 1, -y0 + 1, false);
    // motor lead channel along the top of the beam (leads run to the tub grommet)
    stg(context, B);
    if (s < 0)
        opTransform(context, id + "mir", { "bodies" : posQ(B), "transform" : mirrorAcross(plane(v(0, 0, 0), Y)) });
    fin(context, B, s > 0 ? "TRAILING_ARM_L" : "TRAILING_ARM_R", s > 0 ? "ARV-30-001" : "ARV-30-002", "Rear trailing arm with integral gearmotor cup (6x M3 face bolts + pinch clamp), 2x 686ZZ pivot on ISO 7379 6x25 shoulder bolt into tub M5 insert. PA-CF 100%.", ArvMat.PACF);
    // hardware
    for (var i = 0; i < 6; i += 1)
    {
        const t = (30 + 60 * i) * degree;
        screw(context, id + ("m" ~ i), [XR + 15.5 * cos(t), s * (pl1 - 2.5), ZA + 15.5 * sin(t)], [0, -s, 0], 8, 3, false);
    }
    screw(context, id + "piv", [PX, s * (y1 + 4.5), PZ], [0, -s, 0], 32, 6, false);
    for (var yb in [y0 + 2.5, y1 - 2.5])
    {
        var Br = bld(id + ("b" ~ round(yb)));
        cyl(context, Br, [PX, s * (yb - 2.5), PZ], [PX, s * (yb + 2.5), PZ], 6.5, true);
        cyl(context, Br, [PX, s * (yb - 3), PZ], [PX, s * (yb + 3), PZ], 3, false);
        fin(context, Br, "BEARING_686ZZ", "PUR-BRG-686ZZ", "686ZZ 6x13x5 ball bearing", ArvMat.STEEL);
    }
}

function pGearmotor(context is Context, id is Id, P is map, g is map, s is number)
{
    var B = bld(id);
    const FY = g.faceY;
    cyl(context, B, [g.xR, s * (FY - 82), g.zA], [g.xR, s * FY, g.zA], 18.5, true);
    cyl(context, B, [g.xR, s * FY, g.zA], [g.xR, s * (FY + 4), g.zA], 6, true);
    cyl(context, B, [g.xR, s * FY, g.zA], [g.xR, s * (FY + 22), g.zA], 3, true);
    fin(context, B, s > 0 ? "GEARMOTOR_37D_L" : "GEARMOTOR_37D_R", "PUR-MOT-POLOLU-4751", "Pololu 37D 19:1 12 V gearmotor with 64 CPR encoder (#4751): D37 x 82, 6 mm D-shaft", ArvMat.MOTOR);
}

function pMotorHub(context is Context, id is Id, P is map, g is map, s is number)
{
    var B = bld(id);
    const y0 = g.faceY + 6.5;
    cyl(context, B, [g.xR, s * y0, g.zA], [g.xR, s * (y0 + 11), g.zA], 11, true);
    cyl(context, B, [g.xR, s * (y0 - 1), g.zA], [g.xR, s * (y0 + 12), g.zA], 3, false);
    fin(context, B, s > 0 ? "MOTOR_HUB_L" : "MOTOR_HUB_R", "PUR-MOT-POLOLU-1999", "Pololu universal aluminium mounting hub, 6 mm bore (#1999)", ArvMat.ALU);
}

function pHexAdapter(context is Context, id is Id, P is map, g is map, s is number)
{
    var B = bld(id);
    const yh = g.faceY + 6.5 + 11;
    trace(context, "hex adapter");
    cyl(context, B, [g.xR, s * yh, g.zA], [g.xR, s * (yh + 4), g.zA], 11, true);
    const hs = coordSystem(v(g.xR, s * (yh + 4), g.zA), X, vector(0, s, 0));
    pr(context, B, hs, [[6.93, 0], [3.46, 6], [-3.46, 6], [-6.93, 0], [-3.46, -6], [3.46, -6]], -0.01, 9.5, true);
    stg(context, B);
    for (var i = 0; i < 4; i += 1)
    {
        const t = (45 + 90 * i) * degree;
        const cx = g.xR + 9.525 * cos(t);
        const czz = g.zA + 9.525 * sin(t);
        cyl(context, B, [cx, s * (yh - 1), czz], [cx, s * (yh + 5), czz], 1.7, false);
    }
    cyl(context, B, [g.xR, s * (yh - 1), g.zA], [g.xR, s * (yh + 5), g.zA], 3.15, false);
    hIns(context, B, [g.xR, s * (yh + 13.5), g.zA], [0, -s, 0], 4);
    fin(context, B, s > 0 ? "HEX_ADAPTER_L" : "HEX_ADAPTER_R", s > 0 ? "ARV-30-003" : "ARV-30-004", "Printed 12 mm hex drive adapter: 4x M3 to the Pololu hub, wheel retained by M3x16 + washer into axial insert. PA-CF 100%.", ArvMat.PACF);
    for (var i = 0; i < 4; i += 1)
    {
        const t = (45 + 90 * i) * degree;
        screw(context, id + ("s" ~ i), [g.xR + 9.525 * cos(t), s * (yh + 4), g.zA + 9.525 * sin(t)], [0, -s, 0], 8, 3, false);
    }
    screw(context, id + "w", [g.xR, s * (g.yW + P.wheelWidth / 2 - 1), g.zA], [0, -s, 0], 16, 3, false);
}

function buildDrive(context is Context, id is Id, P is map, g is map)
{
    for (var s in [1, -1])
    {
        const t = s > 0 ? "L" : "R";
        const parts = [["arm", function(i) { pTrailingArm(context, i, P, g, s); }, "LNK_REAR_ARM_" ~ t],
                ["mot", function(i) { pGearmotor(context, i, P, g, s); }, "LNK_REAR_ARM_" ~ t],
                ["hub", function(i) { pMotorHub(context, i, P, g, s); }, "LNK_WHEEL_R" ~ t],
                ["hex", function(i) { pHexAdapter(context, i, P, g, s); }, "LNK_WHEEL_R" ~ t],
                ["whl", function(i) { wheel(context, i, P, g, [g.xR, s * g.yW, g.zA], s, "R" ~ t); }, "LNK_WHEEL_R" ~ t],
                ["shk", function(i) { shockBody(context, i, [g.shockRLowX, s * g.shockRY, g.shockRLowZ], [g.towerR0 - 9 - 3.4, s * g.shockRY, g.shockRUpZ], "SHOCK_R" ~ t, "PUR-SUS-SHOCK-80"); }, "LNK_REAR_SHOCK_" ~ t]];
        for (var p in parts)
            try
            {
                link(context, p[2]);
                p[1](id + (p[0] ~ t));
            }
            catch (e)
            {
                recordError(context, id, p[0] ~ t, e);
            }
    }
}

// =====================================================================================================
//  40_BATTERY_SYSTEM  (rear-loading battery tunnel above the gearmotors, retention, connector bay)
// =====================================================================================================
function pTunnel(context is Context, id is Id, P is map, g is map)
{
    var B = bld(id);
    const w = g.tunWall;
    const xr = g.xTail + 3;                        // tunnel mouth (behind the battery panel)
    const xf = g.tunX1 + w;                        // outer front wall
    const Yi = g.tunY;
    const Z0 = g.tunZ0;
    const Z1 = g.tunZ1;
    trace(context, "tunnel sleeve");
    wb(context, B, xr, xf, -Yi - w, Yi + w, Z0 - w, Z1 + w, true);
    // connector bay flange at the mouth
    wb(context, B, xr, xr + 5, -43, 43, Z0 - w, Z1 + w + 2, true);
    wb(context, B, xr, xr + 5, -57, 57, g.ZTOP + 13.5, Z1 + w + 2, true);
    // front bridge onto the tub flange
    wb(context, B, g.tunX1 - 4, xf + 2, -g.HW, g.HW, g.ZTOP, g.ZTOP + 4, true);
    // ears onto the rear-frame arms
    for (var s in [1, -1])
        for (var x in [g.xTail + 20, g.X0 - 30])
            wb(context, B, x - 6, x + 6, s > 0 ? Yi + 1 : -55, s > 0 ? 55 : -Yi - 1, g.ZTOP + 12, g.ZTOP + 15, true);
    // XT60 pigtail channel on the roof
    wb(context, B, xr + 5, xf, -8, 8, Z1 + w, Z1 + w + 3.5, true);
    stg(context, B);
    trace(context, "tunnel cuts");
    wb(context, B, xr - 1, g.tunX1, -Yi, Yi, Z0, Z1, false);
    // widened connector bay (XT60 pair folds sideways here)
    wb(context, B, xr - 1, g.batX0 - 1, -36, 36, Z0, Z1 + 2, false);
    wb(context, B, xr + 6.5, xf + 1, -6, 6, Z1 + w + 1, Z1 + w + 5, false);
    wb(context, B, xf - 3, xf + 1, -6, 6, Z1 - 4, Z1 + w + 7, false);
    // weight-saving windows in the side walls (battery stays captured by the corner rails)
    for (var s in [1, -1])
        for (var i in [0, 2, 3])
        {
            const x = g.batX0 + 18 + i * 30;
            wb(context, B, x, x + 18, s * Yi - 4 * s, s * (Yi + w + 1), Z0 + 8, Z1 - 8, false);
        }
    // bridge + ear bolt holes, panel inserts
    hClr(context, B, [g.tunX1 + w / 2 - 1, g.HW - 4.5, g.ZTOP - 1], [g.tunX1 + w / 2 - 1, g.HW - 4.5, g.ZTOP + 5]);
    hClr(context, B, [g.tunX1 + w / 2 - 1, -g.HW + 4.5, g.ZTOP - 1], [g.tunX1 + w / 2 - 1, -g.HW + 4.5, g.ZTOP + 5]);
    for (var s in [1, -1])
        for (var x in [g.xTail + 20, g.X0 - 30])
            hClr(context, B, [x, s * 50, g.ZTOP + 11], [x, s * 50, g.ZTOP + 16]);
    for (var s in [1, -1])
    {
        hIns(context, B, [xr, s * 41.5, (Z0 + Z1) / 2], [1, 0, 0], 6);
        hIns(context, B, [xr, s * 51, 104], [1, 0, 0], 6);
    }
    fin(context, B, "BATTERY_TUNNEL", "ARV-40-001", "Rear-loading battery tunnel: captures the pack on 4 sides + front stop, XT60 bay at the mouth, pigtail channel, bolts to tub flange (2x) and rear-frame arms (4x). PETG-CF.", ArvMat.PETGCF);
    var n = 0;
    for (var s in [1, -1])
    {
        screw(context, id + ("b" ~ n), [g.tunX1 + w / 2 - 1, s * (g.HW - 4.5), g.ZTOP + 4], [0, 0, -1], 10, 3, false);
        for (var x in [g.xTail + 20, g.X0 - 30])
        {
            screw(context, id + ("e" ~ n), [x, s * 50, g.ZTOP + 15], [0, 0, -1], 8, 3, false);
            n += 1;
        }
        insert(context, id + ("p" ~ n), [xr, s * 41.5, (Z0 + Z1) / 2], [1, 0, 0]);
        insert(context, id + ("q" ~ n), [xr, s * 51, 104], [1, 0, 0]);
        n += 1;
    }
}

function pBatteryPack(context is Context, id is Id, P is map, g is map)
{
    var B = bld(id);
    const k = pack(P, g);
    const b = k.BATTERY;
    wb(context, B, b[0], b[1], b[2], b[3], b[4], b[5], true);
    stg(context, B);
    soft(context, B, X, 3);
    fin(context, B, "BATTERY_3S_5000", "PUR-PWR-LIPO-3S5000", "3S 11.1 V 5000 mAh hard-case LiPo, XT60, leads toward the rear (max 139 x 47 x 37)", ArvMat.LIPO);
    // XT60 pair in the connector bay + foam pads
    var C = bld(id + "xt");
    wb(context, C, b[0] - 16, b[0] - 8, -10, 6, b[4] + 8, b[4] + 24, true);
    fin(context, C, "XT60_PAIR", "PUR-PWR-XT60", "XT60 male/female pair (battery lead to vehicle pigtail)", ArvMat.PETG);
    var F = bld(id + "fm");
    wb(context, F, b[1], g.tunX1, -15, 15, b[4] + 5, b[5] - 5, true);
    fin(context, F, "BATTERY_FOAM_STOP", "PUR-PWR-FOAM-3", "3 mm closed-cell foam pad, front stop (takes up pack-length tolerance)", ArvMat.TPU);
}

// =====================================================================================================
//  50_ELECTRONICS_TRAY  (removable two-level module under the centre service hatch)
// =====================================================================================================
function trayPillars(g is map) returns array
{
    return [[-33.5, 50], [-33.5, -50], [g.deckX1 - 6.5, 0]];
}

function deckTabs(g is map) returns array
{
    const yi = g.HW - 4.5;
    return [[g.deckX0 + 6, yi], [g.deckX0 + 6, -yi], [g.deckX1 - 8, yi], [g.deckX1 - 8, -yi]];
}

// board stand-offs: 4 bosses inset from the board corners (slotted ±1.5 mm: VERIFY hole patterns)
function boardBosses(context is Context, B is map, bx is array, z0 is number, h is number, inset is number)
{
    for (var cx in [bx[0] + inset, bx[1] - inset])
        for (var cyy in [bx[2] + inset, bx[3] - inset])
            cax(context, B, "Z", [cx, cyy, z0 + h / 2], 6.5, h, true);
}

function boardHoles(context is Context, B is map, bx is array, z0 is number, h is number, inset is number)
{
    for (var cx in [bx[0] + inset, bx[1] - inset])
        for (var cyy in [bx[2] + inset, bx[3] - inset])
            slot(context, B, WCS(), [cx - 1.5, cyy], [cx + 1.5, cyy], 2.6, z0 - 1, z0 + h + 1, false);
}

function pTrayFrame(context is Context, id is Id, P is map, g is map)
{
    var B = bld(id);
    const k = pack(P, g);
    const Z0 = g.lowZ0;
    const Z1 = g.lowZ1;
    trace(context, "tray frame");
    pr(context, B, WCS(), [[-40, -62], [-18, -62], [-16, -66], [103.5, -66], [103.5, -13], [110, -13], [110, 13], [103.5, 13], [103.5, 66], [-16, 66], [-18, 62], [-40, 62]], Z0, Z1, true);
    for (var p in trayPillars(g))
        cax(context, B, "Z", [p[0], p[1], (Z1 + g.deckZ0) / 2], 10, g.deckZ0 - Z1, true);
    for (var n in ["MDD10A", "DCDC19", "BUCK5", "BEC6"])
        boardBosses(context, B, k[n], Z1, k[n][4] - Z1, 3.5);
    // wire-management rails: tie-down loops along the centre corridor
    for (var x in [-8, 22, 52])
        wb(context, B, x - 2, x + 2, -6, 16, Z1, Z1 + 4, true);
    stg(context, B);
    for (var p in trayPillars(g))
        hIns(context, B, [p[0], p[1], g.deckZ0], [0, 0, -1], 6);
    for (var n in ["MDD10A", "DCDC19", "BUCK5", "BEC6"])
        boardHoles(context, B, k[n], Z1, k[n][4] - Z1, 3.5);
    // fuse block: 2x M3 through the plate
    for (var x in [k.FUSE[0] + 8, k.FUSE[1] - 8])
        hClr(context, B, [x, (k.FUSE[2] + k.FUSE[3]) / 2, Z0 - 1], [x, (k.FUSE[2] + k.FUSE[3]) / 2, Z1 + 1]);
    // cable-tie slots in the loops, airflow holes
    for (var x in [-8, 22, 52])
        wb(context, B, x - 3, x + 3, -2, 12, Z1 + 1, Z1 + 3, false);
    for (var x in [5, 35, 65, 88])
        cax(context, B, "Z", [x, 6, (Z0 + Z1) / 2], 8, 10, false);
    fin(context, B, "TRAY_FRAME", "ARV-50-001", "Electronics tray lower plate + 3 pillars (tripod): motor driver, 19 V DC-DC, 5 V buck, servo BEC, fuse block; cable corridor with tie loops. Lifts out with the deck. PETG.", ArvMat.PETG);
}

function pTrayDeck(context is Context, id is Id, P is map, g is map)
{
    var B = bld(id);
    const k = pack(P, g);
    const Z0 = g.deckZ0;
    const Z1 = g.deckZ1;
    const x0 = g.deckX0;
    const x1 = g.deckX1;
    trace(context, "tray deck");
    wb(context, B, x0, x1, -64, 64, Z0, Z1, true);
    for (var t in deckTabs(g))
    {
        wb(context, B, t[0] - 6, t[0] + 6, t[1] > 0 ? 60 : -g.HW + 0.5, t[1] > 0 ? g.HW - 0.5 : -60, Z0, Z1, true);
        cax(context, B, "Z", [t[0], t[1], (g.ZTOP + Z0) / 2], 9, Z0 - g.ZTOP, true);
    }
    // under-ribs (inside the flange lip)
    for (var y in [-40, 0, 40])
        wb(context, B, x0 + 6, x1 - 6, y - 1.2, y + 1.2, Z0 - 4, Z0, true);
    for (var x in [-20, 20, 60, 100])
        wb(context, B, x - 1.2, x + 1.2, -58, 58, Z0 - 4, Z0, true);
    boardBosses(context, B, [k.JETSON[0] + 8.5, k.JETSON[1] - 8.5, -29, 29], Z1, k.JETSON[4] - Z1, 0);
    boardBosses(context, B, k.TEENSY, Z1, k.TEENSY[4] - Z1, 2);
    // IMU plinth (solid, rigid, near CG)
    wb(context, B, k.IMU[0] - 3, k.IMU[1] + 3, k.IMU[2] - 3, k.IMU[3] + 3, Z1, k.IMU[4], true);
    stg(context, B);
    soft(context, B, Z, 4);
    trace(context, "deck cuts");
    for (var t in deckTabs(g))
        hClr(context, B, [t[0], t[1], g.ZTOP - 1], [t[0], t[1], Z1 + 1]);
    for (var p in trayPillars(g))
    {
        hClr(context, B, [p[0], p[1], Z0 - 5], [p[0], p[1], Z1 + 1]);
    }
    boardHoles(context, B, [k.JETSON[0] + 8.5, k.JETSON[1] - 8.5, -29, 29], Z1, k.JETSON[4] - Z1, 0);
    boardHoles(context, B, k.TEENSY, Z1, k.TEENSY[4] - Z1, 2);
    for (var cx in [k.IMU[0] + 2.55, k.IMU[1] - 2.55])
        for (var cyy in [k.IMU[2] + 2.55, k.IMU[3] - 2.55])
            cyl(context, B, [cx, cyy, Z1 - 1], [cx, cyy, k.IMU[4] + 1], 1.25, false);
    // airflow grid under the Jetson + cable pass-throughs (power right, signal left, camera front)
    for (var i = 0; i < 4; i += 1)
        for (var j = 0; j < 3; j += 1)
            slot(context, B, WCS(), [k.JETSON[0] + 18 + i * 20, -20 + j * 20], [k.JETSON[0] + 26 + i * 20, -20 + j * 20], 7, Z0 - 5, Z1 + 1, false);
    for (var p in [[x0 + 60, -54], [x0 + 60, 54], [x1 - 12, -54], [x1 - 12, 54]])
        slot(context, B, WCS(), [p[0] - 8, p[1]], [p[0] + 8, p[1]], 9, Z0 - 5, Z1 + 1, false);
    // cable-tie slot pairs along the deck edges
    for (var x in [x0 + 20, x0 + 100])
        for (var y in [-60, 60])
            for (var dy in [-4, 4])
                wb(context, B, x - 1.5, x + 1.5, y + dy - 2, y + dy + 2, Z0 - 5, Z1 + 1, false);
    fin(context, B, "TRAY_UPPER_DECK", "ARV-50-002", "Electronics tray upper deck: Jetson + Teensy stand-offs, rigid IMU plinth, airflow grid, cable pass-throughs, 4 flange tabs (M3 into tub inserts). PETG-CF.", ArvMat.PETGCF);
    var n = 0;
    for (var t in deckTabs(g))
    {
        screw(context, id + ("t" ~ n), [t[0], t[1], Z1], [0, 0, -1], 12, 3, true);
        n += 1;
    }
    for (var p in trayPillars(g))
    {
        screw(context, id + ("p" ~ n), [p[0], p[1], Z1], [0, 0, -1], 10, 3, true);
        n += 1;
    }
}

function envBox(context is Context, id is Id, b is array, name is string, pn is string, desc is string, m is ArvMat)
{
    var B = bld(id);
    wb(context, B, b[0], b[1], b[2], b[3], b[4], b[5], true);
    fin(context, B, name, pn, desc, m);
}

function pJetson(context is Context, id is Id, P is map, g is map)
{
    const k = pack(P, g);
    const b = k.JETSON;
    var B = bld(id);
    wb(context, B, b[0], b[1], b[2], b[3], b[4], b[4] + 21.6, true);
    wb(context, B, (b[0] + b[1]) / 2 - 35, (b[0] + b[1]) / 2 + 35, -22.5, 22.5, b[4] + 21.6, b[5], true);
    fin(context, B, "JETSON_ORIN_NANO_SUPER", "PUR-CMP-JETSON-ORIN-NANO", "NVIDIA Jetson Orin Nano Super Developer Kit (carrier + module + fan heatsink)", ArvMat.PCB);
}

function buildBattery(context is Context, id is Id, P is map, g is map)
{
    try
    {
        link(context, "LNK_BATTERY_TUNNEL");
        pTunnel(context, id + "tun", P, g);
    }
    catch (e)
    {
        recordError(context, id, "BATTERY_TUNNEL", e);
    }
    try
    {
        link(context, "LNK_BATTERY");
        pBatteryPack(context, id + "bat", P, g);
    }
    catch (e)
    {
        recordError(context, id, "BATTERY", e);
    }
}

function buildElectronics(context is Context, id is Id, P is map, g is map)
{
    const k = pack(P, g);
    try
    {
        link(context, "LNK_ELECTRONICS_TRAY");
        pTrayFrame(context, id + "frame", P, g);
    }
    catch (e)
    {
        recordError(context, id, "TRAY_FRAME", e);
    }
    try
    {
        pTrayDeck(context, id + "deck", P, g);
    }
    catch (e)
    {
        recordError(context, id, "TRAY_UPPER_DECK", e);
    }
    try
    {
        pJetson(context, id + "jet", P, g);
        envBox(context, id + "tee", k.TEENSY, "TEENSY41_CARRIER", "PUR-CMP-TEENSY41", "Teensy 4.1 on 70 x 50 carrier (SBUS, encoders, E-stop watchdog, ToF/IMU hub)", ArvMat.PCB);
        envBox(context, id + "mdd", k.MDD10A, "MOTOR_DRIVER_MDD10A", "PUR-PWR-MDD10A", "Cytron MDD10A dual 10 A motor driver (VERIFY hole pattern: slotted stand-offs)", ArvMat.PCB);
        envBox(context, id + "dc", k.DCDC19, "DCDC_19V_JETSON", "PUR-PWR-DCDC19", "Buck-boost 19 V >= 60 W for the Jetson", ArvMat.PCB);
        envBox(context, id + "b5", k.BUCK5, "BUCK_5V_5A", "PUR-PWR-BUCK5", "5 V 5 A buck (Pololu D36V50F5 class)", ArvMat.PCB);
        envBox(context, id + "b6", k.BEC6, "BEC_6V_SERVO", "PUR-PWR-BEC6", "6.0 V 5 A servo BEC", ArvMat.PCB);
        envBox(context, id + "fu", k.FUSE, "FUSE_BLOCK_2WAY", "PUR-PWR-FUSE2", "2-way ATO blade fuse holder (15 A main / 10 A motor / 5 A compute)", ArvMat.PETG);
        envBox(context, id + "rx", k.RX, "RC_RECEIVER_SBUS", "PUR-CMP-RX-SBUS", "SBUS RC receiver (manual override, wireless E-stop channel), velcro", ArvMat.PCB);
        envBox(context, id + "imu", k.IMU, "IMU_BNO085", "PUR-SNS-BNO085", "BNO085 9-DoF IMU breakout, X forward / Z up on the rigid plinth", ArvMat.PCB);
        envBox(context, id + "rel", k.RELAY, "WATCHDOG_RELAY", "PUR-PWR-RELAY", "Watchdog relay module in series with the motor branch (rear tub bay)", ArvMat.PCB);
    }
    catch (e)
    {
        recordError(context, id, "envelopes", e);
    }
}
