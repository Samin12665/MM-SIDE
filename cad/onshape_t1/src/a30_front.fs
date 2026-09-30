
// =====================================================================================================
//  20_FRONT_STEERING  (ARV-1 double wishbone + dual-bellcrank Ackermann steering, ported 1:1 from
//  cad/parts/front.py and made relative to the derived master geometry)
// =====================================================================================================
function pt(p is array) returns Vector
{
    return v(p[0], p[1], p[2]);
}

function arr(p) returns array
{
    return [p[0] / millimeter, p[1] / millimeter, p[2] / millimeter];
}

export function casterT(P is map, g is map) returns Transform
{
    return rotationAround(line(v(g.xF, 0, g.zA), Y), -P.casterAngle * degree);
}

export function kingpinLine(P is map, g is map, s is number) returns Line
{
    const T = casterT(P, g);
    const lo = T * v(g.xF, s * g.KY, g.kLowZ);
    const up = T * v(g.xF, s * g.KY, g.kUpZ);
    return line(lo, normalize(up - lo));
}

export function steerT(P is map, g is map, s is number, psi is number) returns Transform
{
    return rotationAround(kingpinLine(P, g, s), psi * degree);
}

// steering-arm ball (castered, straight ahead)
export function armTip0(P is map, g is map, s is number) returns array
{
    const a = g.armAng * PI / 180;
    const cas = P.casterAngle * PI / 180;
    return [g.xF + (g.zA - g.armZ) * tan(cas * radian) + g.armLen * cos(a * radian), s * (g.KY + g.armLen * sin(a * radian)), g.armZ];
}

export function crankBall(P is map, g is map, s is number, theta is number) returns array
{
    const b = (g.crankBeta * s + theta) * degree;
    return [g.crankX + g.crankR * cos(b), s * g.crankY + g.crankR * sin(b), g.crankZ];
}

export function crankDragPin(P is map, g is map, s is number, theta is number) returns array
{
    const b = (180 + theta) * degree;
    return [g.crankX + g.crankDragR * cos(b), s * g.crankY + g.crankDragR * sin(b)];
}

export function tieLen(P is map, g is map, s is number) returns number
{
    return norm(pt(armTip0(P, g, s)) - pt(crankBall(P, g, s, 0))) / millimeter;
}

// knuckle steer angle psi (deg, about the upward kingpin axis) for crank angle theta
export function solvePsi(P is map, g is map, s is number, theta is number) returns number
{
    if (abs(theta) < 1e-9)
        return 0;
    const L = tieLen(P, g, s);
    const ball = pt(crankBall(P, g, s, theta));
    const tip = pt(armTip0(P, g, s));
    var best = 0;
    var found = false;
    var prevPsi = 0;
    var prevF = 0;
    // march outward from 0 in both directions, keep the root closest to 0
    for (var dir in [1, -1])
    {
        prevPsi = 0;
        prevF = norm(steerT(P, g, s, 0) * tip - ball) / millimeter - L;
        for (var i = 1; i <= 60; i += 1)
        {
            const ps = dir * i;
            const f = norm(steerT(P, g, s, ps) * tip - ball) / millimeter - L;
            if (f * prevF <= 0)
            {
                var a = prevPsi;
                var b = ps;
                var fa = prevF;
                for (var k = 0; k < 40; k += 1)
                {
                    const m = (a + b) / 2;
                    const fm = norm(steerT(P, g, s, m) * tip - ball) / millimeter - L;
                    if (fa * fm <= 0)
                        b = m;
                    else
                    {
                        a = m;
                        fa = fm;
                    }
                }
                const r = (a + b) / 2;
                if (!found || abs(r) < abs(best))
                    best = r;
                found = true;
                break;
            }
            prevPsi = ps;
            prevF = f;
        }
    }
    return best;
}

function hornPin(P is map, g is map, theta is number) returns array
{
    const dl = crankDragPin(P, g, 1, theta);
    const dl0 = crankDragPin(P, g, 1, 0);
    const dy = dl[1] - dl0[1];
    const phi = asin(max(-1, min(1, dy / g.hornR)));
    return [g.servoX - g.hornR * cos(phi), g.hornR * sin(phi), phi / degree];
}

// ------------------------------------------------------------------ bulkhead
function pBulkhead(context is Context, id is Id, P is map, g is map)
{
    var B = bld(id);
    const x0 = g.X1;
    const x1 = g.servoX - 8;
    const hw = 62;
    const F0 = g.floorF0;
    const F1 = g.floorF1;
    const tabZ = g.servoZ + 4 + 26;
    trace(context, "bulkhead floor");
    wb(context, B, x0, x1, -hw, hw, F0, F1, true);
    stg(context, B);
    soft(context, B, Z, 6);
    wb(context, B, x0, x0 + 4, -hw, hw, F0, g.ZTOP - 10, true);
    for (var s in [1, -1])
    {
        for (var hx in [g.hingeX0, g.hingeX1])
            wb(context, B, hx - 4, hx + 4, s * g.hingeY - 5.5, s * g.hingeY + 5.5, F1 - 0.01, g.kLowZ + 5, true);
        cax(context, B, "Z", [g.crankX, s * g.crankY, (g.crankPlateZ0 - 0.5 + F1) / 2], 10, g.crankPlateZ0 - 0.5 - F1, true);
        wb(context, B, g.towerF0 - 5, g.towerF1 + 5, s > 0 ? 10 : -29, s > 0 ? 29 : -10, F1 - 0.01, 30, true);
    }
    for (var yc in [-34.85, 14.65])
        wb(context, B, g.servoX - 11, g.servoX + 11, yc - 5, yc + 5, F1 - 0.01, tabZ, true);
    stg(context, B);
    trace(context, "bulkhead cuts");
    for (var b in bulkheadBolts(P, g))
        hClr(context, B, [x0 - 1, b[0], b[1]], [x0 + 5, b[0], b[1]]);
    wb(context, B, x0 - 1, x0 + 5, -15, 15, 30, 55, false);
    for (var s in [1, -1])
    {
        cyl(context, B, [g.hingeX0 - 8, s * g.hingeY, g.kLowZ], [g.hingeX1 + 8, s * g.hingeY, g.kLowZ], 1.5, false);
        hIns(context, B, [g.crankX, s * g.crankY, g.crankPlateZ0 - 0.5], [0, 0, -1], 8);
        for (var lx in [g.towerF0 - 1.5, g.towerF1 + 1.5])
            hIns(context, B, [lx, s * 20, 30], [0, 0, -1], 6);
        wb(context, B, x0 + 4.5, g.hingeX1 - 4.5, s > 0 ? 40 : -70, s > 0 ? 70 : -40, F0 - 1, F1 + 1, false);
        wb(context, B, g.hingeX0 + 4.5, g.hingeX1 - 4.5, s > 0 ? 35 : -41, s > 0 ? 41 : -35, F0 - 1, F1 + 1, false);
    }
    for (var yc in [-34.85, 14.65])
    {
        for (var dx in [-5, 5])
            hIns(context, B, [g.servoX + dx, yc, tabZ], [0, 0, -1], 6);
        hIns(context, B, [g.servoX + 11, yc, 40], [-1, 0, 0], 6);
    }
    wb(context, B, g.servoX - 11.5, g.servoX + 11.5, -30.8, 10.7, F1 + 20, tabZ + 1, false);
    for (var x in [g.xF - 10, g.xF + 10])
        cax(context, B, "Z", [x, 0, 20], 14, 10, false);
    fin(context, B, "FRONT_BULKHEAD", "ARV-20-001", "Front bulkhead: A-arm hinge blocks, bellcrank posts, integral inverted-servo cradle (41), shock-tower pads, bumper inserts. 6x M3 to tub front wall. PA-CF.", ArvMat.PACF);
    var n = 0;
    for (var b in bulkheadBolts(P, g))
    {
        screw(context, id + ("s" ~ n), [x0 + 4, b[0], b[1]], [-1, 0, 0], 12, 3, false);
        n += 1;
    }
    for (var yc in [-34.85, 14.65])
    {
        for (var dx in [-5, 5])
        {
            insert(context, id + ("i" ~ n), [g.servoX + dx, yc, tabZ], [0, 0, -1]);
            n += 1;
        }
        insert(context, id + ("b" ~ n), [g.servoX + 11, yc, 40], [-1, 0, 0]);
        n += 1;
    }
    for (var s in [1, -1])
        for (var lx in [g.towerF0 - 1.5, g.towerF1 + 1.5])
        {
            insert(context, id + ("t" ~ n), [lx, s * 20, 30], [0, 0, -1]);
            n += 1;
        }
}

// ------------------------------------------------------------------ front shock tower
function pFrontTower(context is Context, id is Id, P is map, g is map)
{
    var B = bld(id);
    const t0 = g.towerF0;
    const t1 = g.towerF1;
    const top = g.towerFTop;
    const hy = g.shockFY + 8;
    trace(context, "front tower");
    pr(context, B, FRONT(), [[-hy, 50], [hy, 50], [hy, top - 8], [hy - 8, top], [-hy + 8, top], [-hy, top - 8]], t0, t1, true);
    wb(context, B, t0, t1, -29, 29, 40, 52, true);
    for (var s in [1, -1])
    {
        const y0 = s > 0 ? 10 : -29;
        const y1 = s > 0 ? 29 : -10;
        wb(context, B, t0 - 5, t1 + 5, y0, y1, 30, 34, true);
        wb(context, B, t0, t1, y0, y1, 30, 52, true);
        for (var sg in [-1, 1])
        {
            const gx = sg < 0 ? t0 : t1;
            pr(context, B, SIDE(), [[gx, 34], [gx + sg * 5, 34], [gx, 50]], -y1, -y0, true);
        }
        cax(context, B, "X", [t0 - 1.5, s * g.shockFY, g.shockFUpZ], 9, 3, true);
        // upper A-arm clevis: the tower plate is the rear lug, bridge + front lug carry the X-axis hinge pin
        wb(context, B, t1 - 0.01, t1 + 15.5, s * (g.camberInY - 5), s * (g.camberInY + 5), g.kUpZ + 5, g.kUpZ + 9, true);
        wb(context, B, t1 + 11.8, t1 + 15.5, s * (g.camberInY - 5), s * (g.camberInY + 5), g.kUpZ - 6, g.kUpZ + 9, true);
    }
    stg(context, B);
    for (var s in [1, -1])
    {
        for (var lx in [t0 - 1.5, t1 + 1.5])
            hClr(context, B, [lx, s * 20, 29], [lx, s * 20, 35]);
        hIns(context, B, [t0 - 3, s * g.shockFY, g.shockFUpZ], [1, 0, 0], 6);
        cyl(context, B, [t0 - 1, s * g.camberInY, g.kUpZ], [t1 + 16.5, s * g.camberInY, g.kUpZ], 1.5, false);
    }
    // body nose locating pin holes (Ø4.2 x 8) in the top edge
    for (var y in [-30, 30])
        cyl(context, B, [(t0 + t1) / 2, y, top + 0.1], [(t0 + t1) / 2, y, top - 8], 2.1, false);
    wb(context, B, t0 - 1, t1 + 1, -8, 8, 56, top - 14, false);
    for (var y in [-44, 44])
        wb(context, B, t0 - 1, t1 + 1, y - 8, y + 8, 76, top - 12, false);
    fin(context, B, "FRONT_SHOCK_TOWER", "ARV-20-002", "Front shock tower: shock ball studs (rearward), upper A-arm clevises with M3x25 hinge pins, 4x M3 into bulkhead, body-nose locating pins. PA-CF.", ArvMat.PACF);
    var n = 0;
    for (var s in [1, -1])
    {
        for (var lx in [t0 - 1.5, t1 + 1.5])
        {
            screw(context, id + ("s" ~ n), [lx, s * 20, 34], [0, 0, -1], 10, 3, false);
            n += 1;
        }
        insert(context, id + ("a" ~ n), [t0 - 3, s * g.shockFY, g.shockFUpZ], [1, 0, 0]);
        screw(context, id + ("c" ~ n), [t1 + 15.5, s * g.camberInY, g.kUpZ], [-1, 0, 0], 25, 3, false);
        n += 1;
    }
}

// ------------------------------------------------------------------ knuckle (left built upright, castered, mirrored)
function pKnuckle(context is Context, id is Id, P is map, g is map, s is number) returns Query
{
    var B = bld(id);
    const XF = g.xF;
    const ZA = g.zA;
    const KY = g.KY;
    const y0 = KY - 11;
    const y1 = KY + 1;
    const T = casterT(P, g);
    const Ti = inverse(T);
    const tipU = arr(Ti * pt(armTip0(P, g, 1)));
    const pz0 = tipU[2] + 5;
    trace(context, "knuckle");
    cax(context, B, "Y", [XF, (y0 + y1) / 2, ZA], 16, y1 - y0, true);
    cax(context, B, "Z", [XF, KY, g.kLowZ + 5 + 2.25], 8, 4.5, true);
    wb(context, B, XF - 4, XF + 4, y0 + 2, KY, g.kLowZ + 5, ZA - 6, true);
    cax(context, B, "Z", [XF, KY, g.kUpZ - 9], 8, 4, true);
    wb(context, B, XF - 4, XF + 4, y0 + 2, KY, ZA + 6, g.kUpZ - 9, true);
    // steering arm: square root inside the hex/rim envelope (ARV-1 recipe), round tip
    pr(context, B, WCS(), [[XF + 4, KY - 6], [tipU[0], tipU[1] - 5], [tipU[0] + 3.5, tipU[1] - 3.5], [tipU[0] + 5, tipU[1]], [tipU[0] + 3.5, tipU[1] + 3.5], [tipU[0], tipU[1] + 5], [XF + 10, KY + 1.5], [XF + 10, KY + 0.8], [XF + 4, KY + 0.8]], pz0, pz0 + 4, true);
    wb(context, B, XF - 14, XF - 6, y0 - 2.5, y0 + 1, ZA - 5, ZA + 5, true);
    stg(context, B);
    for (var dz in [-2.5, 2.5])
        cyl(context, B, [XF - 10, y0 - 3, ZA + dz], [XF - 10, y0 + 2, ZA + dz], 0.85, false);
    cyl(context, B, [XF, y0 - 5, ZA], [XF, y1 + 5, ZA], 3.1, false);
    cyl(context, B, [XF, y0 - 0.01, ZA], [XF, y0 + 4.2, ZA], 5.55, false);
    cyl(context, B, [XF, y1 + 0.01, ZA], [XF, y1 - 4.2, ZA], 5.55, false);
    hIns(context, B, [XF, KY, g.kLowZ + 5], [0, 0, 1], 4);
    hIns(context, B, [XF, KY, g.kUpZ - 7], [0, 0, -1], 4);
    hIns(context, B, [tipU[0], tipU[1], pz0 + 4], [0, 0, -1], 4);
    cyl(context, B, [tipU[0], tipU[1], pz0 - 1], [tipU[0], tipU[1], pz0 + 5], 1.5, false);
    stg(context, B);
    opTransform(context, id + "cas", { "bodies" : posQ(B), "transform" : T });
    if (s < 0)
        opTransform(context, id + "mir", { "bodies" : posQ(B), "transform" : mirrorAcross(plane(v(0, 0, 0), Y)) });
    return fin(context, B, s > 0 ? "FRONT_KNUCKLE_L" : "FRONT_KNUCKLE_R", s > 0 ? "ARV-20-003" : "ARV-20-004", "Ball-joint knuckle, 5 deg caster: 2x 685ZZ, rotating M5 axle, M3 ball studs on the kingpin line, steering arm, AS5600 tab. PA-CF 100%.", ArvMat.PACF);
}

// ------------------------------------------------------------------ lower A-arm
function pLowerArm(context is Context, id is Id, P is map, g is map, s is number) returns Query
{
    var B = bld(id);
    const lb = arr(casterT(P, g) * v(g.xF, g.KY, g.kLowZ));
    const tipY = g.KY - 10;
    const z0 = g.kLowZ - 4.2;
    const z1 = g.kLowZ + 4;
    const xi0 = g.hingeX0 + 4.5;
    const xi1 = g.hingeX1 - 4.5;
    const HY = g.hingeY;
    trace(context, "lower arm");
    pr(context, B, WCS(), [[xi0, HY], [xi1, HY], [lb[0] + 7, tipY - 6], [lb[0] + 6, tipY], [lb[0] - 6, tipY], [g.shockFX - 4, g.shockFY + 2], [g.shockFX - 4, g.shockFY - 8], [xi0, HY + 8]], z0, z1, true);
    cyl(context, B, [xi0, HY, g.kLowZ], [xi1, HY, g.kLowZ], 4.3, true);
    cax(context, B, "Z", [g.shockFX, g.shockFY, (z0 + z1) / 2], 12, z1 - z0, true);
    stg(context, B);
    hIns(context, B, [g.shockFX, g.shockFY, z1], [0, 0, -1], 6);
    hIns(context, B, [lb[0], tipY, lb[2]], [0, -1, 0], 6);
    cyl(context, B, [xi0 - 1, HY, g.kLowZ], [xi1 + 1, HY, g.kLowZ], 1.6, false);
    pr(context, B, WCS(), [[xi0 + 7, HY + 8], [xi1 - 7, HY + 8], [lb[0], tipY - 20]], z0 - 1, z1 + 1, false);
    stg(context, B);
    if (s < 0)
        opTransform(context, id + "mir", { "bodies" : posQ(B), "transform" : mirrorAcross(plane(v(0, 0, 0), Y)) });
    return fin(context, B, s > 0 ? "FRONT_LOWER_ARM_L" : "FRONT_LOWER_ARM_R", s > 0 ? "ARV-20-005" : "ARV-20-006", "Printed lower A-arm on an M3 hinge pin; purchased M3 ball end captures the knuckle lower ball; shock ball stud. PA-CF 100%.", ArvMat.PACF);
}

// ------------------------------------------------------------------ steering linkage
function pBellcrank(context is Context, id is Id, P is map, g is map, s is number, theta is number)
{
    var B = bld(id);
    const c = [g.crankX, s * g.crankY];
    const b = crankBall(P, g, s, theta);
    const d = crankDragPin(P, g, s, theta);
    trace(context, "bellcrank");
    cz(context, B, WCS(), c[0], c[1], 5, g.crankPlateZ0, g.crankDragZ1, true);
    slot(context, B, WCS(), c, [b[0], b[1]], 8, g.crankPlateZ0, g.crankPlateZ1, true);
    slot(context, B, WCS(), c, d, 8, g.crankDragZ0, g.crankDragZ1, true);
    stg(context, B);
    cz(context, B, WCS(), c[0], c[1], 4.05, g.crankPlateZ0 - 0.01, g.crankPlateZ0 + 3.1, false);
    cz(context, B, WCS(), c[0], c[1], 4.05, g.crankDragZ1 - 3.1, g.crankDragZ1 + 0.01, false);
    cz(context, B, WCS(), c[0], c[1], 2.5, g.crankPlateZ0 - 1, g.crankDragZ1 + 1, false);
    hIns(context, B, [b[0], b[1], g.crankPlateZ1], [0, 0, -1], g.crankPlateZ1 - g.crankPlateZ0);
    hIns(context, B, [d[0], d[1], g.crankDragZ0], [0, 0, 1], 3);
    cz(context, B, WCS(), d[0], d[1], 1.7, g.crankDragZ0 - 1, g.crankDragZ1 + 1, false);
    fin(context, B, s > 0 ? "BELLCRANK_L" : "BELLCRANK_R", s > 0 ? "ARV-20-007" : "ARV-20-008", "Steering bellcrank on 2x MR84 + M4 shoulder bolt; tie-rod ball stud + drag-link pin (parallelogram, not mirrored). PA-CF 100%.", ArvMat.PACF);
}

function pDragLink(context is Context, id is Id, P is map, g is map, theta is number)
{
    var B = bld(id);
    const pl = crankDragPin(P, g, 1, theta);
    const pq = crankDragPin(P, g, -1, theta);
    const mid = [(pl[0] + pq[0]) / 2, (pl[1] + pq[1]) / 2];
    trace(context, "drag link");
    pr(context, B, WCS(), [[pl[0], pl[1] + 5], [pq[0], pq[1] - 5], [pq[0] + 5, pq[1]], [pl[0] + 5, pl[1]]], g.dragZ0, g.dragZ1, true);
    for (var p in [pl, pq, mid])
        cz(context, B, WCS(), p[0], p[1], 5, g.dragZ0, g.dragZ1, true);
    stg(context, B);
    for (var p in [pl, pq])
        cz(context, B, WCS(), p[0], p[1], 2.025, g.dragZ0 - 1, g.dragZ1 + 1, false);
    hIns(context, B, [mid[0], mid[1], g.dragZ1], [0, 0, -1], 3);
    cz(context, B, WCS(), mid[0], mid[1], 1.7, g.dragZ0 - 1, g.dragZ1 + 1, false);
    fin(context, B, "DRAG_LINK", "ARV-20-009", "Parallelogram drag link joining both bellcranks (M4 shoulder pins), servo-link pin at centre. PA-CF.", ArvMat.PACF);
}

function pServoLink(context is Context, id is Id, P is map, g is map, theta is number)
{
    var B = bld(id);
    const h = hornPin(P, g, theta);
    const pl = crankDragPin(P, g, 1, theta);
    const pq = crankDragPin(P, g, -1, theta);
    const mid = [(pl[0] + pq[0]) / 2, (pl[1] + pq[1]) / 2];
    trace(context, "servo link");
    slot(context, B, WCS(), [h[0], h[1]], mid, 10, g.slinkZ0, g.slinkZ1, true);
    stg(context, B);
    for (var p in [[h[0], h[1]], mid])
        cz(context, B, WCS(), p[0], p[1], 2.025, g.slinkZ0 - 1, g.slinkZ1 + 1, false);
    fin(context, B, "SERVO_LINK", "ARV-20-010", "Servo horn to drag-link link. PA-CF.", ArvMat.PACF);
}

// ------------------------------------------------------------------ purchased: servo, horn, rods, shocks, wheels
function pServo(context is Context, id is Id, P is map, g is map)
{
    var B = bld(id);
    const x = g.servoX;
    const z = g.servoZ;
    wb(context, B, x - 10, x + 10, -30.3, 10.2, z + 4, z + 42.5, true);
    wb(context, B, x - 10, x + 10, -37.3, 17.2, z + 30, z + 32.5, true);
    cax(context, B, "Z", [x, 0, z + 2], 6, 4, true);
    stg(context, B);
    for (var yc in [-34.85, 14.65])
        for (var dx in [-5, 5])
            cz(context, B, WCS(), x + dx, yc, 1.7, z + 29, z + 33.5, false);
    fin(context, B, "STEERING_SERVO_20KG", "PUR-SRV-DS3218", "20 kg-cm standard digital servo (DS3218 class), 25T, mounted inverted in the bulkhead cradle, 4x M3x10.", ArvMat.SERVO);
    var n = 0;
    for (var yc in [-34.85, 14.65])
        for (var dx in [-5, 5])
        {
            screw(context, id + ("s" ~ n), [x + dx, yc, z + 32.5], [0, 0, -1], 10, 3, false);
            n += 1;
        }
}

function pServoHorn(context is Context, id is Id, P is map, g is map, theta is number)
{
    var B = bld(id);
    const h = hornPin(P, g, theta);
    slot(context, B, WCS(), [g.servoX, 0], [h[0], h[1]], 9, g.servoZ - 3, g.servoZ, true);
    cz(context, B, WCS(), g.servoX, 0, 6, g.servoZ - 3, g.servoZ, true);
    stg(context, B);
    cz(context, B, WCS(), h[0], h[1], 1.5, g.servoZ - 4, g.servoZ + 1, false);
    fin(context, B, "SERVO_HORN_25T", "PUR-SRV-HORN25T", "Aluminium 25T servo horn, 18 mm pin radius.", ArvMat.ALU);
}

// rod with ball ends between two points (tie rod / camber link turnbuckles)
function rodLink(context is Context, id is Id, a is array, b is array, name is string, pn is string, desc is string)
{
    var B = bld(id);
    trace(context, "rod " ~ name);
    const d = normalize(pt(b) - pt(a));
    trace(context, "rod d " ~ name);
    const da = arr(pt(a) + d * 5 * millimeter);
    trace(context, "rod da " ~ name);
    const db = arr(pt(b) - d * 5 * millimeter);
    trace(context, "rod db " ~ name);
    cyl(context, B, da, db, 1.5, true);
    trace(context, "rod cyl " ~ name);
    cyl(context, B, arr(pt(a) + d * 3 * millimeter), arr(pt(a) + d * 10 * millimeter), 3, true);
    cyl(context, B, arr(pt(b) - d * 10 * millimeter), arr(pt(b) - d * 3 * millimeter), 3, true);
    for (var p in [a, b])
    {
        const s = sub(B, true);
        fCylinder(context, s + "c", { "bottomCenter" : pt(p) - d * 3.4 * millimeter, "topCenter" : pt(p) + d * 3.4 * millimeter, "radius" : 3.4 * millimeter });
    }
    fin(context, B, name, pn, desc, ArvMat.STEEL);
}

function shockBody(context is Context, id is Id, lo is array, up is array, name is string, pn is string)
{
    var B = bld(id);
    trace(context, "shock " ~ name);
    const d = normalize(pt(up) - pt(lo));
    const L = norm(pt(up) - pt(lo)) / millimeter;
    const bl = 0.58 * L;
    cyl(context, B, arr(pt(up) - d * (bl + 4) * millimeter), arr(pt(up) - d * 1 * millimeter), 8, true);
    cyl(context, B, arr(pt(lo) + d * 1 * millimeter), arr(pt(up) - d * (bl + 3) * millimeter), 1.5, true);
    for (var p in [lo, up])
    {
        const s = sub(B, true);
        fCylinder(context, s + "c", { "bottomCenter" : pt(p) - d * 2.4 * millimeter, "topCenter" : pt(p) + d * 2.4 * millimeter, "radius" : 3.4 * millimeter });
    }
    fin(context, B, name, pn, "1/10 aluminium oil shock 80 mm eye-to-eye (ball ends), 22 mm stroke", ArvMat.ALU);
}

// oil shock split into the two parts that stroke against each other (body + upper eye / rod + lower eye)
function shockParts(context is Context, id is Id, lo is array, up is array, tag is string, linkB is string, linkR is string)
{
    const d = normalize(pt(up) - pt(lo));
    const L = norm(pt(up) - pt(lo)) / millimeter;
    const bl = 0.58 * L;
    trace(context, "shock " ~ tag);
    link(context, linkB);
    var B = bld(id + "b");
    cyl(context, B, arr(pt(up) - d * (bl + 4) * millimeter), arr(pt(up) - d * 1 * millimeter), 8, true);
    cyl(context, B, arr(pt(up) - d * 2.4 * millimeter), arr(pt(up) + d * 2.4 * millimeter), 3.4, true);
    stg(context, B);
    cyl(context, B, arr(pt(up) - d * (bl + 4.1) * millimeter), arr(pt(up) - d * (bl + 4 - 30) * millimeter), 1.7, false);
    fin(context, B, "SHOCK_BODY_" ~ tag, "PUR-SUS-SHOCK-80", "1/10 aluminium oil shock 80 mm eye-to-eye, 22 mm stroke: body, spring and upper ball end", ArvMat.ALU);
    link(context, linkR);
    var R = bld(id + "r");
    cyl(context, R, arr(pt(lo) + d * 1 * millimeter), arr(pt(up) - d * (bl + 4 - 24) * millimeter), 1.5, true);
    cyl(context, R, arr(pt(lo) - d * 2.4 * millimeter), arr(pt(lo) + d * 2.4 * millimeter), 3.4, true);
    fin(context, R, "SHOCK_ROD_" ~ tag, "PUR-SUS-SHOCK-80-ROD", "Shock piston rod + lower ball end (supplied with PUR-SUS-SHOCK-80)", ArvMat.STEEL);
}

// purchased M3 ball end: threaded body from the arm tip to the captured ball on the knuckle stud
function ballEnd(context is Context, id is Id, a is array, b is array, name is string)
{
    var B = bld(id);
    const d = normalize(pt(b) - pt(a));
    cyl(context, B, a, arr(pt(b) - d * 2 * millimeter), 3, true);
    cyl(context, B, arr(pt(b) - d * 3.4 * millimeter), arr(pt(b) + d * 3.4 * millimeter), 3.6, true);
    fin(context, B, name, "PUR-STR-BALLEND-M3", "M3 steel ball end with 6.8 mm ball stud (knuckle side)", ArvMat.STEEL);
}

// printed upper A-arm (replaces the ARV-1 single camber link: determinate double wishbone, same 57 mm geometry)
function pUpperArm(context is Context, id is Id, P is map, g is map, s is number)
{
    var B = bld(id);
    const ub = arr(casterT(P, g) * v(g.xF, g.KY, g.kUpZ));
    const tipY = g.KY - 10;
    const HY = g.camberInY;
    const z0 = g.kUpZ - 4;
    const z1 = g.kUpZ + 4;
    const xi0 = g.towerF1 + 0.5;
    const xi1 = g.towerF1 + 11.3;
    trace(context, "upper arm");
    pr(context, B, WCS(), [[xi0, HY], [xi1, HY], [ub[0] + 6, tipY - 6], [ub[0] + 5, tipY], [max(ub[0] - 5, xi0 + 0.3), tipY], [xi0, HY + 9]], z0, z1, true);
    cyl(context, B, [xi0, HY, g.kUpZ], [xi1, HY, g.kUpZ], 4.3, true);
    stg(context, B);
    hIns(context, B, [ub[0], tipY, ub[2]], [0, -1, 0], 6);
    cyl(context, B, [xi0 - 1, HY, g.kUpZ], [xi1 + 1, HY, g.kUpZ], 1.6, false);
    pr(context, B, WCS(), [[xi0 + 3, HY + 6], [xi1 - 2, HY + 6], [ub[0], tipY - 14]], z0 - 1, z1 + 1, false);
    stg(context, B);
    if (s < 0)
        opTransform(context, id + "mir", { "bodies" : posQ(B), "transform" : mirrorAcross(plane(v(0, 0, 0), Y)) });
    fin(context, B, s > 0 ? "FRONT_UPPER_ARM_L" : "FRONT_UPPER_ARM_R", s > 0 ? "ARV-20-011" : "ARV-20-012", "Printed upper A-arm on an M3x25 hinge pin in the shock-tower clevis; M3 ball end captures the knuckle upper ball (57 mm effective length, ARV-1 camber curve). PA-CF 100%.", ArvMat.PACF);
}

// rugged off-road tyre + rim at wheel centre c, axis along Y, inner face toward sign -s
export function wheel(context is Context, id is Id, P is map, g is map, c is array, s is number, tag is string)
{
    const R = P.wheelDiameter / 2;
    const W = P.wheelWidth;
    const yA = c[1] - W / 2;
    const yB = c[1] + W / 2;
    var T = bld(id + "t");
    trace(context, "tyre " ~ tag);
    cyl(context, T, [c[0], yA + 1, c[2]], [c[0], yB - 1, c[2]], R - 2.2, true);
    cyl(context, T, [c[0], yA, c[2]], [c[0], yB, c[2]], R - 5, true);
    stg(context, T);

    // tread lugs: two staggered rows + shoulder blocks
    const nL = 16;
    for (var i = 0; i < nL; i += 1)
    {
        for (var row in [0, 1])
        {
            const a = (i + row * 0.5) * 360 / nL;
            const yc = row == 0 ? c[1] + W / 4 - 0.5 : c[1] - W / 4 + 0.5;
            const cs = coordSystem(v(c[0], yc, c[2]), vector(cos(a * degree), 0, sin(a * degree)), Y);
            bx(context, T, cs, R - 4, R, -5, 5, -(W / 4 - 0.5), W / 4 - 0.5, true);
        }
    }
    stg(context, T);
    cyl(context, T, [c[0], yA - 1, c[2]], [c[0], yB + 1, c[2]], P.rimDiameter / 2, false);
    stg(context, T);
    fin(context, T, "TYRE_" ~ tag, "PUR-WHL-TYRE-85x35", "1/10 rally/off-road tyre 85 x 35 with foam insert (pre-glued to rim)", ArvMat.RUBBER);
    // rim
    var Rm = bld(id + "r");
    trace(context, "rim " ~ tag);
    const yi = c[1] - s * (W / 2 - 1.5);         // inner rim face
    const yo = c[1] + s * (W / 2 - 1.5);         // outer rim face
    const rr = P.rimDiameter / 2;
    // barrel ring
    cyl(context, Rm, [c[0], yi, c[2]], [c[0], yo, c[2]], rr, true);
    stg(context, Rm);
    cyl(context, Rm, [c[0], yi - s * 0.5, c[2]], [c[0], yo - s * 4, c[2]], rr - 2.5, false);
    stg(context, Rm);
    // hub (inner face to the spoke face) with the 12 mm hex socket and 5 mm bore
    cyl(context, Rm, [c[0], c[1] - s * 7.5, c[2]], [c[0], yo - s * 2, c[2]], 8, true);
    stg(context, Rm);
    // spoke windows in the outer face
    for (var i = 0; i < 6; i += 1)
    {
        const a = (i * 60 + 30) * degree;
        cyl(context, Rm, [c[0] + 17 * cos(a), yo - s * 6, c[2] + 17 * sin(a)], [c[0] + 17 * cos(a), yo + s * 1, c[2] + 17 * sin(a)], 5.5, false);
    }
    cyl(context, Rm, [c[0], c[1] - s * 8.5, c[2]], [c[0], yo + s * 1, c[2]], 2.6, false);
    const hs = coordSystem(v(c[0], c[1] - s * 7.5, c[2]), X, vector(0, s, 0));
    pr(context, Rm, hs, [[6.93, 0], [3.46, 6], [-3.46, 6], [-6.93, 0], [-3.46, -6], [3.46, -6]], -0.5, 6.2, false);
    fin(context, Rm, "RIM_" ~ tag, "PUR-WHL-RIM-2.2", "2.2 in 12 mm-hex rim (pre-glued with tyre)", ArvMat.PETG);
}

// ------------------------------------------------------------------ studio
function buildFront(context is Context, id is Id, P is map, g is map, theta is number)
{
    const psiL = solvePsi(P, g, 1, theta);
    const psiR = solvePsi(P, g, -1, theta);
    setVariable(context, "arvSteer", { "theta" : theta, "psiL" : psiL, "psiR" : psiR, "tieRod" : tieLen(P, g, 1) });
    const parts = [
            ["bulk", function(i) { pBulkhead(context, i, P, g); }, "LNK_FRONT_FRAME"],
            ["tower", function(i) { pFrontTower(context, i, P, g); }, "LNK_FRONT_FRAME"],
            ["crl", function(i) { pBellcrank(context, i, P, g, 1, theta); }, "LNK_CRANK_L"],
            ["crr", function(i) { pBellcrank(context, i, P, g, -1, theta); }, "LNK_CRANK_R"],
            ["drag", function(i) { pDragLink(context, i, P, g, theta); }, "LNK_DRAG_LINK"],
            ["slink", function(i) { pServoLink(context, i, P, g, theta); }, "LNK_SERVO_LINK"],
            ["servo", function(i) { pServo(context, i, P, g); }, "LNK_FRONT_FRAME"],
            ["horn", function(i) { pServoHorn(context, i, P, g, theta); }, "LNK_SERVO_HORN"],
            ["arml", function(i) { pLowerArm(context, i, P, g, 1); }, "LNK_ARM_L"],
            ["armr", function(i) { pLowerArm(context, i, P, g, -1); }, "LNK_ARM_R"],
            ["uarml", function(i) { pUpperArm(context, i, P, g, 1); }, "LNK_UPPER_ARM_L"],
            ["uarmr", function(i) { pUpperArm(context, i, P, g, -1); }, "LNK_UPPER_ARM_R"]];
    for (var p in parts)
        try
        {
            link(context, p[2]);
            p[1](id + p[0]);
        }
        catch (e)
        {
            recordError(context, id, p[0], e);
        }
    // corners: knuckle + wheel + axle hardware move together with the steer angle
    for (var s in [1, -1])
    {
        const t = s > 0 ? "L" : "R";
        const cid = id + ("corner" ~ t);
        try
        {
            link(context, "LNK_KNUCKLE_" ~ t);
            const kq = pKnuckle(context, cid + "k", P, g, s);
            link(context, "LNK_WHEEL_F" ~ t);
            wheel(context, cid + "w", P, g, [g.xF, s * g.yW, g.zA], s, "F" ~ t);
            link(context, "LNK_KNUCKLE_" ~ t);
            // M5 x 35 rotating axle (head inboard, AS5600 magnet on the head), 2x 685ZZ, hex
            trace(context, "axle " ~ t);
            screw(context, cid + "ax", [g.xF, s * (g.KY - 13), g.zA], [0, s, 0], 35, 5, false);
            link(context, "LNK_WHEEL_F" ~ t);
            var Hx = bld(cid + "hx");
            const hs = coordSystem(v(g.xF, s * (g.KY + 4.5), g.zA), X, vector(0, s, 0));
            pr(context, Hx, hs, [[6.93, 0], [3.46, 6], [-3.46, 6], [-6.93, 0], [-3.46, -6], [3.46, -6]], 0, 6, true);
            cyl(context, Hx, [g.xF, s * (g.KY + 4), g.zA], [g.xF, s * (g.KY + 11), g.zA], 2.6, false);
            // 3.3 mm aluminium spacer between the outer bearing and the hex
            cyl(context, Hx, [g.xF, s * (g.KY + 1.1), g.zA], [g.xF, s * (g.KY + 4.5), g.zA], 4, true);
            fin(context, Hx, "WHEEL_HEX_12MM_F" ~ t, "PUR-WHL-HEX12", "12 mm aluminium wheel hex, 5 mm bore, pinned to the axle", ArvMat.ALU);
            link(context, "LNK_KNUCKLE_" ~ t);
            for (var yb in [g.KY - 11 + 2, g.KY + 1 - 2])
            {
                var Br = bld(cid + ("brg" ~ round(yb)));
                cyl(context, Br, [g.xF, s * (yb - 2), g.zA], [g.xF, s * (yb + 2), g.zA], 5.5, true);
                cyl(context, Br, [g.xF, s * (yb - 3), g.zA], [g.xF, s * (yb + 3), g.zA], 2.5, false);
                fin(context, Br, "BEARING_685ZZ", "PUR-BRG-685ZZ", "685ZZ 5x11x4 ball bearing", ArvMat.STEEL);
            }
            trace(context, "steer move " ~ t);
            const psi = s > 0 ? psiL : psiR;
            if (abs(psi) > 1e-6)
            {
                const moving = qUnion([qCreatedBy(cid + "k", EntityType.BODY), qCreatedBy(cid + "w", EntityType.BODY), qCreatedBy(cid + "ax", EntityType.BODY), qCreatedBy(cid + "hx", EntityType.BODY), qCreatedBy(cid + ("brg" ~ round(g.KY - 9)), EntityType.BODY), qCreatedBy(cid + ("brg" ~ round(g.KY - 1)), EntityType.BODY)]);
                opTransform(context, cid + "steer", { "bodies" : moving, "transform" : steerT(P, g, s, psi) });
            }
            const Tst = steerT(P, g, s, psi);
            // tie rod: steered arm ball -> bellcrank ball
            const tip = arr(Tst * pt(armTip0(P, g, s)));
            link(context, "LNK_TIE_ROD_" ~ t);
            rodLink(context, cid + "tie", tip, crankBall(P, g, s, theta), "TIE_ROD_" ~ t, "PUR-STR-TIEROD-M3", "M3 steel turnbuckle + 2 ball ends, set to " ~ round(tieLen(P, g, s) * 10) / 10 ~ " mm ball-to-ball");
            // purchased M3 ball ends: lower on the A-arm, upper on the upper A-arm (balls on the kingpin line)
            const lb = arr(casterT(P, g) * v(g.xF, s * g.KY, g.kLowZ));
            const ub = arr(casterT(P, g) * v(g.xF, s * g.KY, g.kUpZ));
            link(context, "LNK_ARM_" ~ t);
            ballEnd(context, cid + "bel", [lb[0], s * (g.KY - 10), lb[2]], lb, "BALL_END_LOWER_" ~ t);
            link(context, "LNK_UPPER_ARM_" ~ t);
            ballEnd(context, cid + "beu", [ub[0], s * (g.KY - 10), ub[2]], ub, "BALL_END_UPPER_" ~ t);
            shockParts(context, cid + "shk", [g.shockFX, s * g.shockFY, g.kLowZ + 4 + 3.4], [g.towerF0 - 12, s * g.shockFY, g.shockFUpZ], "F" ~ t, "LNK_SHOCKB_F" ~ t, "LNK_SHOCKR_F" ~ t);
        }
        catch (e)
        {
            recordError(context, id, "corner " ~ t, e);
        }
    }
}
