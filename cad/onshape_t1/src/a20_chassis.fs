
// =====================================================================================================
//  Common feature helpers for part builders
// =====================================================================================================
// insert pocket (Ø4.0 x depth) entering along unit dv from mouth m
function hIns(context is Context, B is map, m is array, dv is array, depth is number)
{
    cyl(context, B, [m[0] - dv[0] * 0.02, m[1] - dv[1] * 0.02, m[2] - dv[2] * 0.02], [m[0] + dv[0] * depth, m[1] + dv[1] * depth, m[2] + dv[2] * depth], 2.0, false);
}

// clearance hole Ø3.4 from a to b
function hClr(context is Context, B is map, a is array, b is array)
{
    cyl(context, B, a, b, 1.7, false);
}

// counterbore Ø6.2 x depth at face point f going along dv (into material)
function hCb(context is Context, B is map, f is array, dv is array, depth is number)
{
    cyl(context, B, [f[0] - dv[0] * 0.02, f[1] - dv[1] * 0.02, f[2] - dv[2] * 0.02], [f[0] + dv[0] * depth, f[1] + dv[1] * depth, f[2] + dv[2] * depth], 3.1, false);
}

function add3(a is array, b is array) returns array
{
    return [a[0] + b[0], a[1] + b[1], a[2] + b[2]];
}

function mul3(a is array, s is number) returns array
{
    return [a[0] * s, a[1] * s, a[2] * s];
}

// =====================================================================================================
//  10_CHASSIS  (structural lower chassis: tub, skid, rear shock towers, rear bumper frame)
// =====================================================================================================
export function tubFlangeInserts(P is map, g is map) returns array
{
    const yi = g.HW - 4.5;
    var pts = [];
    // electronics tray deck (4)
    for (var x in [g.deckX0 + 6, g.deckX1 - 8])
        pts = concatenateArrays([pts, [[x, yi], [x, -yi]]]);
    // body shell mounts
    pts = concatenateArrays([pts, bodyMounts(P, g)]);
    // rear shock tower feet (2 per side)
    for (var x in [g.towerR0 - 9, g.towerR1 + 6])
        pts = concatenateArrays([pts, [[x, yi], [x, -yi]]]);
    // battery tunnel bridge
    pts = concatenateArrays([pts, [[g.tunX1 + g.tunWall / 2 - 1, yi], [g.tunX1 + g.tunWall / 2 - 1, -yi]]]);
    return pts;
}

export function bulkheadBolts(P is map, g is map) returns array
{
    return [[-55, 40], [-20, 40], [20, 40], [55, 40], [-55, 62], [55, 62]];
}

export function floorBosses(P is map, g is map) returns array
{
    // skid screws from below + relay module / cable-clip bosses
    return [[-100, 55], [-100, -55], [-70, 0], [-5, 55], [-5, -55], [60, 60], [60, -60], [100, 0]];
}

function pMainChassis(context is Context, id is Id, P is map, g is map)
{
    var B = bld(id);
    const X0 = g.X0;
    const X1 = g.X1;
    const HW = g.HW;
    const T = g.T;
    trace(context, "tub shell");
    wb(context, B, X0, X1, -HW, HW, g.ZF, g.ZTOP, true);
    wb(context, B, X0 + T, X1 - T, -HW + T, HW - T, g.ZFT, g.ZTOP + 1, false);
    stg(context, B);
    // top flange lip (stiffens the open box, carries the flange inserts)
    wb(context, B, X0, X1, HW - T - 6, HW, g.ZTOP - 3, g.ZTOP, true);
    wb(context, B, X0, X1, -HW, -HW + T + 6, g.ZTOP - 3, g.ZTOP, true);
    wb(context, B, X0, X0 + T + 6, -HW, HW, g.ZTOP - 3, g.ZTOP, true);
    wb(context, B, X1 - T - 4, X1, -HW, HW, g.ZTOP - 3, g.ZTOP, true);
    // flange insert bosses
    trace(context, "tub flange bosses");
    for (var p in tubFlangeInserts(P, g))
        cax(context, B, "Z", [p[0], p[1], g.ZTOP - 5], 9, 10, true);
    // floor ribs (under the tray lower plate)
    for (var y in [-30, 0, 30])
        wb(context, B, X0 + T, X1 - T, y - 1.2, y + 1.2, g.ZFT, g.ZFT + 3, true);
    for (var x in [-80, -40, 20, 70])
        wb(context, B, x - 1.2, x + 1.2, -HW + T, HW - T, g.ZFT, g.ZFT + 3, true);
    // floor bosses
    for (var p in floorBosses(P, g))
        cax(context, B, "Z", [p[0], p[1], g.ZFT + 2], 9, 4, true);
    // rear-arm pivot bosses + internal gusset blocks (M5 insert)
    trace(context, "tub pivots");
    for (var s in [1, -1])
    {
        cax(context, B, "Y", [g.pivX, s * (HW + 1.5), g.pivZ], 20, 3, true);
        wb(context, B, g.pivX - 14, g.pivX + 14, s > 0 ? HW - 12 : -HW, s > 0 ? HW : -HW + 12, g.ZFT, g.pivZ + 12, true);
    }
    // front wall: bulkhead bolt bosses
    for (var b in bulkheadBolts(P, g))
        wb(context, B, X1 - 10, X1, b[0] - 5, b[0] + 5, b[1] - 5, b[1] + 5, true);
    // rear wall: bumper-frame bolt bosses
    for (var y in [-40, 40])
        for (var z in [55, 72])
            wb(context, B, X0, X0 + 10, y - 5, y + 5, z - 5, z + 5, true);
    stg(context, B);
    // ---- cuts
    trace(context, "tub cuts");
    for (var p in tubFlangeInserts(P, g))
        hIns(context, B, [p[0], p[1], g.ZTOP], [0, 0, -1], 6);
    for (var p in floorBosses(P, g))
    {
        hIns(context, B, [p[0], p[1], g.ZFT + 4], [0, 0, -1], 6);
        hClr(context, B, [p[0], p[1], g.ZF - 1], [p[0], p[1], g.ZFT + 1]);
    }
    for (var s in [1, -1])
        cyl(context, B, [g.pivX, s * (HW + 3.1), g.pivZ], [g.pivX, s * (HW + 3 - 12), g.pivZ], 3.2, false);   // M5 insert Ø6.4
    for (var b in bulkheadBolts(P, g))
        hIns(context, B, [X1, b[0], b[1]], [-1, 0, 0], 6);
    for (var y in [-40, 40])
        for (var z in [55, 72])
            hIns(context, B, [X0, y, z], [1, 0, 0], 6);
    // wiring pass-throughs: motor leads (both sides, beside the pivots), front bulkhead corridor
    for (var s in [1, -1])
        cax(context, B, "Y", [g.pivX - 22, s * HW, 64], 11, 10, false);
    wb(context, B, X1 - 4, X1 + 1, -15, 15, g.ZFT + 4, g.ZFT + 22, false);
    // rear wall harness slot (motor leads + rear ToF), between the frame bolt bosses
    wb(context, B, X0 - 1, X0 + T + 1, -14, 14, 54, 68, false);
    // passive ventilation slots (motor driver / DC-DC bay), low on both side walls
    for (var s in [1, -1])
        for (var i = 0; i < 5; i += 1)
        {
            const x = -4 + i * 18;
            wb(context, B, x, x + 9, s * HW - 4, s * HW + 4, g.ZFT + 12, g.ZFT + 30, false);
        }
    // floor drain / weight holes in the front bay
    for (var x in [40, 85])
        for (var y in [-15, 15])
            cax(context, B, "Z", [x, y, g.ZF + 1.5], 10, 6, false);
    fin(context, B, "MAIN_CHASSIS", "ARV-10-001", "Structural tub: floor, 3 mm walls, flange lip, pivot gussets, 30 flange/floor inserts. Print PA-CF/PETG-CF, floor down.", ArvMat.PACF);
    // hardware
    var n = 0;
    for (var p in tubFlangeInserts(P, g))
    {
        insert(context, id + ("fi" ~ n), [p[0], p[1], g.ZTOP], [0, 0, -1]);
        n += 1;
    }
    for (var b in bulkheadBolts(P, g))
    {
        insert(context, id + ("bi" ~ n), [X1, b[0], b[1]], [-1, 0, 0]);
        n += 1;
    }
    for (var y in [-40, 40])
        for (var z in [55, 72])
        {
            insert(context, id + ("ri" ~ n), [X0, y, z], [1, 0, 0]);
            n += 1;
        }
    for (var p in floorBosses(P, g))
    {
        insert(context, id + ("ki" ~ n), [p[0], p[1], g.ZFT + 4], [0, 0, -1]);
        screw(context, id + ("ks" ~ n), [p[0], p[1], P.groundClearance], [0, 0, 1], 10, 3, true);
        n += 1;
    }
}

function pSkid(context is Context, id is Id, P is map, g is map)
{
    var B = bld(id);
    trace(context, "skid");
    wb(context, B, g.X0 + 2, g.X1 - 2, -g.HW + 4, g.HW - 4, P.groundClearance, g.ZF, true);
    stg(context, B);
    soft(context, B, Z, 10);
    for (var p in floorBosses(P, g))
    {
        hClr(context, B, [p[0], p[1], P.groundClearance - 1], [p[0], p[1], g.ZF + 1]);
        cyl(context, B, [p[0], p[1], P.groundClearance - 0.1], [p[0], p[1], P.groundClearance + 1.6], 3.0, false);
    }
    fin(context, B, "LOWER_SKID", "ARV-10-002", "Sacrificial 2 mm skid (PETG or cut HDPE), 8x M3 button head into floor inserts.", ArvMat.PETG);
}

function pRearTower(context is Context, id is Id, P is map, g is map, s is number)
{
    var B = bld(id);
    const yi = g.HW - 4.5;
    const y0 = g.HW - 9;
    const yo = g.shockRY + 8;
    const zt = g.shockRUpZ + 8;
    trace(context, "rear tower " ~ s);
    // foot on the tub flange
    wb(context, B, g.towerR0 - 15, g.towerR1 + 11, s * y0, s * g.HW, g.ZTOP, g.ZTOP + 4, true);
    // upright plate
    const plate = [[y0, g.ZTOP], [yo, g.ZTOP + 4], [yo, zt - 10], [yo - 8, zt], [y0, zt]];
    var pl = [];
    for (var q in plate)
        pl = append(pl, [s * q[0], q[1]]);
    pr(context, B, FRONT(), pl, g.towerR0, g.towerR1, true);
    // fore/aft gussets
    for (var sg in [-1, 1])
    {
        const xg = sg < 0 ? g.towerR0 : g.towerR1;
        pr(context, B, SIDE(), [[xg, g.ZTOP + 4], [xg + sg * 10, g.ZTOP + 4], [xg, g.ZTOP + 40]], -s * (y0 + 3) - 1.5, -s * (y0 + 3) + 1.5, true);
    }
    // shock stud boss (stud points -X)
    cax(context, B, "X", [g.towerR0 - 2.5, s * g.shockRY, g.shockRUpZ], 10, 5, true);
    stg(context, B);
    for (var x in [g.towerR0 - 9, g.towerR1 + 6])
    {
        hClr(context, B, [x, s * yi, g.ZTOP - 1], [x, s * yi, g.ZTOP + 5]);
    }
    hIns(context, B, [g.towerR0 - 5, s * g.shockRY, g.shockRUpZ], [1, 0, 0], 6);
    // lightening window
    pr(context, B, FRONT(), [[s * (y0 + 4), g.ZTOP + 14], [s * (yo - 5), g.ZTOP + 14], [s * (yo - 5), g.shockRUpZ - 12], [s * (y0 + 4), g.shockRUpZ - 12]], g.towerR0 - 1, g.towerR1 + 1, false);
    const nm = s > 0 ? "REAR_SHOCK_TOWER_L" : "REAR_SHOCK_TOWER_R";
    fin(context, B, nm, s > 0 ? "ARV-10-003" : "ARV-10-004", "Rear shock tower post, bolted to the tub flange (2x M3x10), shock ball stud into M3 insert. PA-CF.", ArvMat.PACF);
    var n = 0;
    for (var x in [g.towerR0 - 9, g.towerR1 + 6])
    {
        screw(context, id + ("s" ~ n), [x, s * yi, g.ZTOP + 4], [0, 0, -1], 10, 3, false);
        n += 1;
    }
    insert(context, id + "si", [g.towerR0 - 5, s * g.shockRY, g.shockRUpZ], [1, 0, 0]);
}

function pRearFrame(context is Context, id is Id, P is map, g is map)
{
    var B = bld(id);
    const X0 = g.X0;
    const xe = g.xTail - 8;           // cross bar rear face
    trace(context, "rear frame");
    // plate against the tub rear wall
    wb(context, B, X0 - 4, X0, -60, 60, 48, g.ZTOP + 12, true);
    // arms over the motors (clear of full-bump motor tops)
    for (var s in [1, -1])
        wb(context, B, xe + 6, X0 - 4, s * 50 - 5, s * 50 + 5, g.ZTOP + 4, g.ZTOP + 12, true);
    // rear cross bar: carries the rear bumper and the tail of the body
    wb(context, B, xe, xe + 6, -60, 60, g.ZTOP - 16, g.ZTOP + 2, true);
    for (var s in [1, -1])
        wb(context, B, xe, xe + 6, s * 44, s * 56, g.ZTOP - 16, g.ZTOP + 12, true);
    // arm-to-plate gussets
    for (var s in [1, -1])
        pr(context, B, SIDE(), [[X0 - 4, g.ZTOP + 4], [X0 - 24, g.ZTOP + 4], [X0 - 4, 56]], -s * 50 - 2, -s * 50 + 2, true);
    stg(context, B);
    // bolts to tub rear wall
    for (var y in [-40, 40])
        for (var z in [55, 72])
            hClr(context, B, [X0 - 5, y, z], [X0 + 1, y, z]);
    // bumper inserts (along +X into the cross bar), tunnel ear + tail body inserts (from above)
    for (var y in [-40, 40])
        hIns(context, B, [xe, y, g.ZTOP - 6], [1, 0, 0], 6);
    for (var s in [1, -1])
        for (var x in [g.xTail + 20, g.X0 - 30])
            hIns(context, B, [x, s * 50, g.ZTOP + 12], [0, 0, -1], 6);
    // cable relief in the plate (motor leads, rear ToF) + notch for the battery tunnel passing over
    wb(context, B, X0 - 5, X0 + 1, -18, 18, 50, 66, false);
    wb(context, B, X0 - 5, X0 + 1, -g.tunY - g.tunWall - 1, g.tunY + g.tunWall + 1, g.ZTOP - 1.5, g.ZTOP + 13, false);
    fin(context, B, "REAR_BUMPER_FRAME", "ARV-10-005", "Rear frame: plate on tub rear wall (4x M3), arms over the gearmotors, cross bar for rear bumper, battery tunnel and tail. PA-CF.", ArvMat.PACF);
    var n = 0;
    for (var y in [-40, 40])
        for (var z in [55, 72])
        {
            screw(context, id + ("s" ~ n), [X0 - 4, y, z], [1, 0, 0], 12, 3, false);
            n += 1;
        }
    for (var y in [-40, 40])
    {
        insert(context, id + ("bi" ~ n), [xe, y, g.ZTOP - 6], [1, 0, 0]);
        n += 1;
    }
    for (var s in [1, -1])
        for (var x in [g.xTail + 20, g.X0 - 30])
        {
            insert(context, id + ("ti" ~ n), [x, s * 50, g.ZTOP + 12], [0, 0, -1]);
            n += 1;
        }
}

function buildChassis(context is Context, id is Id, P is map, g is map)
{
    link(context, "LNK_CHASSIS");
    try
    {
        pMainChassis(context, id + "tub", P, g);
    }
    catch (e)
    {
        recordError(context, id, "MAIN_CHASSIS", e);
    }
    try
    {
        pSkid(context, id + "skid", P, g);
    }
    catch (e)
    {
        recordError(context, id, "LOWER_SKID", e);
    }
    for (var s in [1, -1])
        try
        {
            pRearTower(context, id + (s > 0 ? "twl" : "twr"), P, g, s);
        }
        catch (e)
        {
            recordError(context, id, "REAR_SHOCK_TOWER", e);
        }
    try
    {
        pRearFrame(context, id + "frame", P, g);
    }
    catch (e)
    {
        recordError(context, id, "REAR_BUMPER_FRAME", e);
    }
}
