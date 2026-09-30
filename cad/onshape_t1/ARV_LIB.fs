FeatureScript 3083;
import(path : "onshape/std/common.fs", version : "3083.0");

// =====================================================================================================
//  ARV-1  SIDE Autonomous Research Vehicle  --  tactical UGV body revision (T1)
//  Onshape FeatureScript library: master skeleton layers + subsystem studio builders + audit.
//
//  Workflow (see docs/ONSHAPE_CAD.md):
//    00_CAR_MASTER   native #variables  ->  "ARV Master Layer" features (SK01..SK05, datums, connectors)
//                    every master sketch body carries the attribute { arvMaster : <layer>, P : <map> }
//    NN_<studio>     native Derived feature pulling ONLY the master layers it needs
//                    ->  "ARV Studio Builder" reads P from the derived master geometry (never re-typed)
//    60_BODY_MASTER  derives SK02/SK03/SK05, adds "ARV Body Master" hull layers; body studios derive it.
//
//  Frame: X forward, Y left, Z up. Origin = ground plane, vehicle centreline, mid-wheelbase. mm.
// =====================================================================================================

// ------------------------------------------------------------------ master defaults
// These are ONLY fall-backs used inside 00_CAR_MASTER when a native variable is missing. Downstream
// studios never read these: they read the P map carried by the derived master sketches.
export const ARV_DEFAULTS = {
        // SK01/SK03 global
        "wheelbase" : 300,
        "trackWidth" : 250,
        "wheelDiameter" : 85,
        "wheelWidth" : 35,
        "rimDiameter" : 56,
        "groundClearance" : 25,
        // chassis
        "chassisLength" : 230,
        "chassisWidth" : 152,
        "tubWallHeight" : 55,
        // body
        "bodyWidth" : 224,
        "bodyHeight" : 142,
        "bodyLength" : 420,
        "noseX" : 205,
        "shoulderZ" : 126,
        "roofWidth" : 176,
        "fenderClearance" : 8,
        "fenderWidth" : 152,
        // battery (3S 5000 mAh hard case, max accepted envelope)
        "batteryLength" : 139,
        "batteryWidth" : 47,
        "batteryHeight" : 37,
        "batteryFrontX" : -54,
        // electronics tray
        "trayDeckZ" : 84,
        // sensors
        "cameraX" : 170,
        "cameraHeight" : 152,
        "cameraTilt" : 5,
        "frontSensorZ" : 86,
        "frontSensorSplay" : 25,
        "rearSensorZ" : 60,
        // steering / suspension (ARV-1 solved kinematics, analysis/front_kinematics.py)
        "steerLockInner" : 26,
        "casterAngle" : 5,
        "scrub" : 12,
        "bumpTravel" : 18,
        "droopTravel" : 12
    };

// derived geometry: everything downstream uses this (computed from P, never re-typed)
export function geo(P is map) returns map
{
    var g = {};
    g.xF = P.wheelbase / 2;
    g.xR = -P.wheelbase / 2;
    g.zA = P.wheelDiameter / 2;
    g.yW = P.trackWidth / 2;                           // wheel centre plane
    g.KY = g.yW - P.scrub;                             // kingpin Y (113)
    g.X0 = -P.chassisLength / 2;                       // tub
    g.X1 = P.chassisLength / 2;
    g.HW = P.chassisWidth / 2;
    g.ZF = P.groundClearance + 2;                      // skid top / tub floor underside (27)
    g.ZFT = g.ZF + 3;                                  // tub floor top (30)
    g.ZTOP = g.ZF + P.tubWallHeight;                   // tub top (82)
    g.T = 3;                                           // tub wall
    // front suspension (ARV-1 SK04, relative to wheel/kingpin)
    g.kLowZ = g.zA - 16;                               // 26.5
    g.kUpZ = g.zA + 17.5;                              // 60
    g.hingeY = g.KY - 79;                              // 34
    g.hingeX0 = g.xF - 20;
    g.hingeX1 = g.xF + 20;
    g.camberInY = g.KY - 57;                           // 56
    g.shockFX = g.xF - 23;                             // 127
    g.shockFY = g.KY - 33;                             // 80
    g.shockFLowZ = g.kLowZ + 7;
    g.shockFUpZ = 103;
    g.towerF0 = g.xF - 11;                             // 139
    g.towerF1 = g.xF - 6;                              // 144
    g.towerFTop = g.shockFUpZ + 6;                     // 109
    g.floorF0 = 18;                                    // bulkhead floor
    g.floorF1 = 21.5;
    // steering (ARV-1 solved: crank pivot, arm, tie rod)
    g.crankX = g.xF + 26;
    g.crankY = g.KY - 64.1;                            // 48.9
    g.crankZ = 36.5;
    g.crankR = 18;
    g.crankBeta = -18.35;
    g.crankDragR = 16;
    g.armLen = 20;
    g.armAng = 3.03;
    g.armZ = g.zA - 6;
    g.servoX = g.xF + 58;                              // 208
    g.servoZ = 46;                                     // output (inverted servo)
    g.hornR = 18;
    g.crankPlateZ0 = g.crankZ - 8;
    g.crankPlateZ1 = g.crankZ - 4.5;
    g.crankDragZ0 = 40;
    g.crankDragZ1 = 43;
    g.dragZ0 = 43.3;
    g.dragZ1 = 46.3;
    g.slinkZ0 = 46.6;
    g.slinkZ1 = 49.6;
    // rear suspension / drivetrain (ARV-1 SK05/SK07)
    g.pivX = g.xR + 118;                               // -32
    g.pivZ = g.zA + 6;                                 // 48.5
    g.faceY = g.yW - P.wheelWidth / 2 - 15;            // 92.5 gearbox face
    g.shockRLowX = g.xR + 36;
    g.shockRLowZ = g.zA + 20;
    g.shockRUpX = g.xR + 58;
    g.shockRUpZ = 130.2;
    g.shockRY = g.faceY - 6.5;                         // 86
    g.towerR0 = g.xR + 67;                             // -83
    g.towerR1 = g.xR + 72;                             // -78
    // wheel sweep (front, plan, about kingpin) and keep-outs
    g.tyreTopBump = P.wheelDiameter + P.bumpTravel;    // 103
    g.sweepR = sqrt((P.wheelDiameter / 2) ^ 2 + (P.scrub + P.wheelWidth / 2) ^ 2);
    // body
    g.zHB = g.tyreTopBump + P.fenderClearance;         // hull underside over the wheels (111)
    g.zSh = P.shoulderZ;
    g.zRoof = P.bodyHeight;
    g.ys = P.bodyWidth / 2;                            // 112
    g.yr = P.roofWidth / 2;                            // 88
    g.xNose = P.noseX;
    g.xTail = P.noseX - P.bodyLength;                  // -215
    g.podX1 = g.xF - g.sweepR - P.fenderClearance - 4; // front face of side pods
    g.podX0 = g.xR + P.wheelDiameter / 2 + P.fenderClearance + 4.5;
    g.podZ0 = 42;
    g.wallB = 2.4;                                     // body wall
    // service openings
    g.hatchX0 = -48;
    g.hatchX1 = 112;
    // battery + tunnel
    g.batX1 = P.batteryFrontX;
    g.batX0 = P.batteryFrontX - P.batteryLength;
    g.batZ0 = g.ZTOP + 4;                              // 86
    g.tunY = P.batteryWidth / 2 + 1.5;
    g.tunZ0 = g.batZ0 - 1;
    g.tunZ1 = g.batZ0 + P.batteryHeight + 2.5;
    g.tunX1 = P.batteryFrontX + 2;                     // front stop wall (inner)
    g.tunWall = 2.5;
    // electronics tray
    g.deckZ0 = P.trayDeckZ;
    g.deckZ1 = P.trayDeckZ + 3;
    g.deckX0 = g.tunX1 + g.tunWall + 5;                // -46.5 : clear of the tunnel front wall
    g.deckX1 = g.X1 - g.T - 3;
    g.lowZ0 = g.ZFT + 5;
    g.lowZ1 = g.lowZ0 + 3;
    return g;
}

// ------------------------------------------------------------------ read P
// master: variables -> P (with defaults); downstream: P from derived master geometry
export function masterP(context is Context) returns map
{
    var p = ARV_DEFAULTS;
    for (var k, v in ARV_DEFAULTS)
    {
        try silent
        {
            const val = getVariable(context, k);
            if (val is ValueWithUnits)
            {
                if (isAngle(val))
                    p[k] = val / degree;
                else
                    p[k] = val / millimeter;
            }
            else if (val is number)
                p[k] = val;
        }
    }
    return p;
}

// find the P map carried by derived master geometry (optionally a specific layer)
export function derivedP(context is Context, key is string) returns map
{
    const q = qEverything(EntityType.BODY);
    var best = undefined;
    for (var a in getAttributes(context, { "entities" : q }))
    {
        if (a is map && a[key] != undefined && a.P is map)
            best = a.P;
    }
    if (best == undefined)
        throw regenError("ARV: no derived " ~ key ~ " geometry found. Insert a Derived feature from 00_CAR_MASTER (or 60_BODY_MASTER) before this feature.");
    return best;
}

// ------------------------------------------------------------------ small helpers
const X = vector(1, 0, 0);
const Y = vector(0, 1, 0);
const Z = vector(0, 0, 1);

function v(x, y, z) returns Vector
{
    return vector(x, y, z) * millimeter;
}

function WCS() returns CoordSystem
{
    return coordSystem(v(0, 0, 0), X, Z);
}

// frame: origin (mm), local z dir, local x dir
function fr(o is array, zd is Vector, xd is Vector) returns CoordSystem
{
    return coordSystem(v(o[0], o[1], o[2]), normalize(xd), normalize(zd));
}

// side frame (local x = world X, local y = world Z, local z = -world Y)  -> profiles in the XZ plane
function SIDE() returns CoordSystem
{
    return coordSystem(v(0, 0, 0), X, -Y);
}

// front frame (local x = world Y, local y = world Z, local z = world X) -> profiles in the YZ plane
function FRONT() returns CoordSystem
{
    return coordSystem(v(0, 0, 0), Y, X);
}

function rotZ(cs is CoordSystem, c is array, deg is number) returns CoordSystem
{
    const t = rotationAround(line(v(c[0], c[1], 0), Z), deg * degree);
    return coordSystem(t * cs.origin, t.linear * cs.xAxis, t.linear * cs.zAxis);
}

// ------------------------------------------------------------------ CSG builder (HCRA pattern)
function bld(id is Id) returns map
{
    return { "id" : id, "n" : new box(0), "pos" : new box([]), "neg" : new box([]) };
}

function nx(B is map) returns string
{
    var b = B.n;
    b[] = b[] + 1;
    return "k" ~ b[];
}

function sub(B is map, add is boolean) returns Id
{
    const s = B.id + nx(B);
    var lst = add ? B.pos : B.neg;
    lst[] = append(lst[], s);
    return s;
}

function qIds(ids is array) returns Query
{
    var qs = [];
    for (var i in ids)
        qs = append(qs, qCreatedBy(i, EntityType.BODY));
    return qUnion(qs);
}

function posQ(B is map) returns Query
{
    return qIds(B.pos[]);
}

// box by local ranges in cs
function bx(context is Context, B is map, cs is CoordSystem, x0 is number, x1 is number, y0 is number, y1 is number, z0 is number, z1 is number, add is boolean)
{
    const s = sub(B, add);
    fCuboid(context, s + "c", { "corner1" : v(min(x0, x1), min(y0, y1), min(z0, z1)), "corner2" : v(max(x0, x1), max(y0, y1), max(z0, z1)) });
    if (cs != WCS())
        opTransform(context, s + "t", { "bodies" : qCreatedBy(s + "c", EntityType.BODY), "transform" : toWorld(cs) });
}

// world box
function wb(context is Context, B is map, x0 is number, x1 is number, y0 is number, y1 is number, z0 is number, z1 is number, add is boolean)
{
    bx(context, B, WCS(), x0, x1, y0, y1, z0, z1, add);
}

// cylinder between two world points
function cyl(context is Context, B is map, p0 is array, p1 is array, r is number, add is boolean)
{
    const s = sub(B, add);
    fCylinder(context, s + "c", { "bottomCenter" : v(p0[0], p0[1], p0[2]), "topCenter" : v(p1[0], p1[1], p1[2]), "radius" : r * millimeter });
}

// cylinder along world axis 'ax' ("X","Y","Z") centred at c, length L
function cax(context is Context, B is map, ax is string, c is array, d is number, L is number, add is boolean)
{
    var p0 = c;
    var p1 = c;
    const i = ax == "X" ? 0 : (ax == "Y" ? 1 : 2);
    p0[i] = c[i] - L / 2;
    p1[i] = c[i] + L / 2;
    cyl(context, B, p0, p1, d / 2, add);
}

// cylinder in local frame along local z
function cz(context is Context, B is map, cs is CoordSystem, x is number, y is number, r is number, z0 is number, z1 is number, add is boolean)
{
    const s = sub(B, add);
    fCylinder(context, s + "c", { "bottomCenter" : toWorld(cs, v(x, y, z0)), "topCenter" : toWorld(cs, v(x, y, z1)), "radius" : r * millimeter });
}

// prism: closed polygon in local (x,y) of cs extruded along local z from z0 to z1
function pr(context is Context, B is map, cs is CoordSystem, pts is array, z0 is number, z1 is number, add is boolean)
{
    const skId = B.id + nx(B);
    const org = toWorld(cs, v(0, 0, z0));
    const sk = newSketchOnPlane(context, skId, { "sketchPlane" : plane(org, cs.zAxis, cs.xAxis) });
    const n = size(pts);
    for (var i = 0; i < n; i += 1)
    {
        const a = pts[i];
        const b = pts[(i + 1) % n];
        skLineSegment(sk, "l" ~ i, { "start" : vector(a[0], a[1]) * millimeter, "end" : vector(b[0], b[1]) * millimeter });
    }
    skSolve(sk);
    const s = sub(B, add);
    opExtrude(context, s, { "entities" : qSketchRegion(skId), "direction" : cs.zAxis, "endBound" : BoundingType.BLIND, "endDepth" : (z1 - z0) * millimeter });
    opDeleteBodies(context, B.id + nx(B), { "entities" : qCreatedBy(skId, EntityType.BODY) });
}

// stadium slot (2D) in local frame from point a to point b (local x,y), width w, extruded z0..z1
function slot(context is Context, B is map, cs is CoordSystem, a is array, b is array, w is number, z0 is number, z1 is number, add is boolean)
{
    cz(context, B, cs, a[0], a[1], w / 2, z0, z1, add);
    cz(context, B, cs, b[0], b[1], w / 2, z0, z1, add);
    const d = [b[0] - a[0], b[1] - a[1]];
    const L = sqrt(d[0] * d[0] + d[1] * d[1]);
    if (L > 1e-6)
    {
        const nrm = [-d[1] / L * w / 2, d[0] / L * w / 2];
        pr(context, B, cs, [[a[0] + nrm[0], a[1] + nrm[1]], [b[0] + nrm[0], b[1] + nrm[1]], [b[0] - nrm[0], b[1] - nrm[1]], [a[0] - nrm[0], a[1] - nrm[1]]], z0, z1, add);
    }
}

// three-view solid: side profile (x=X,y=Z) ∩ plan profile (x=X,y=Y) ∩ front profile (x=Y,y=Z)
function tri(context is Context, B is map, side is array, plan is array, front is array, add is boolean)
{
    var T = bld(B.id + nx(B));
    pr(context, T, SIDE(), side, -600, 600, true);
    pr(context, T, WCS(), plan, -100, 600, true);
    pr(context, T, FRONT(), front, -600, 600, true);
    const bi = B.id + nx(B);
    opBoolean(context, bi, { "tools" : posQ(T), "operationType" : BooleanOperationType.INTERSECTION });
    var lst = add ? B.pos : B.neg;
    lst[] = concatenateArrays([lst[], T.pos[], [bi]]);
}

// polygon inward offset (simple polygon, CCW or CW), d > 0 shrinks
function offsetPoly(pts is array, d is number) returns array
{
    const n = size(pts);
    var area = 0;
    for (var i = 0; i < n; i += 1)
    {
        const a = pts[i];
        const b = pts[(i + 1) % n];
        area += a[0] * b[1] - b[0] * a[1];
    }
    const sgn = area > 0 ? 1 : -1;       // CCW -> inward normal is left of edge
    var lines = [];
    for (var i = 0; i < n; i += 1)
    {
        const a = pts[i];
        const b = pts[(i + 1) % n];
        const dx = b[0] - a[0];
        const dy = b[1] - a[1];
        const L = sqrt(dx * dx + dy * dy);
        const nn = [-dy / L * sgn, dx / L * sgn];
        lines = append(lines, [[a[0] + nn[0] * d, a[1] + nn[1] * d], [dx, dy]]);
    }
    var out = [];
    for (var i = 0; i < n; i += 1)
    {
        const l0 = lines[(i + n - 1) % n];
        const l1 = lines[i];
        const den = l0[1][0] * l1[1][1] - l0[1][1] * l1[1][0];
        if (abs(den) < 1e-9)
            out = append(out, l1[0]);
        else
        {
            const t = ((l1[0][0] - l0[0][0]) * l1[1][1] - (l1[0][1] - l0[0][1]) * l1[1][0]) / den;
            out = append(out, [l0[0][0] + l0[1][0] * t, l0[0][1] + l0[1][1] * t]);
        }
    }
    return out;
}

// run union of positives and subtraction of pending negatives
function stg(context is Context, B is map)
{
    const pq = posQ(B);
    const nq = qIds(B.neg[]);
    if (size(evaluateQuery(context, pq)) > 1)
        opBoolean(context, B.id + nx(B), { "tools" : pq, "operationType" : BooleanOperationType.UNION });
    if (size(evaluateQuery(context, nq)) > 0)
    {
        if (size(evaluateQuery(context, pq)) > 0)
            opBoolean(context, B.id + nx(B), { "tools" : nq, "targets" : pq, "operationType" : BooleanOperationType.SUBTRACTION });
        else
            opDeleteBodies(context, B.id + nx(B), { "entities" : nq });
    }
    var ng = B.neg;
    ng[] = [];
}

export enum ArvMat
{
    PETG,
    PETGCF,
    PACF,
    ASA,
    TPU,
    STEEL,
    ALU,
    BRASS,
    RUBBER,
    PCB,
    SERVO,
    MOTOR,
    LIPO,
    GLASS
}

function matOf(m is ArvMat) returns map
{
    if (m == ArvMat.PETG)
        return { "n" : "PETG (printed)", "d" : 1.27 * 0.55, "c" : color(0.18, 0.19, 0.20) };
    if (m == ArvMat.PETGCF)
        return { "n" : "PETG-CF (printed)", "d" : 1.30 * 0.60, "c" : color(0.14, 0.15, 0.16) };
    if (m == ArvMat.PACF)
        return { "n" : "PA-CF (printed, structural)", "d" : 1.18 * 0.70, "c" : color(0.10, 0.10, 0.11) };
    if (m == ArvMat.ASA)
        return { "n" : "ASA (printed, UV-stable body)", "d" : 1.07 * 0.50, "c" : color(0.36, 0.40, 0.33) };
    if (m == ArvMat.TPU)
        return { "n" : "TPU 95A (printed)", "d" : 1.21 * 0.60, "c" : color(0.08, 0.08, 0.08) };
    if (m == ArvMat.STEEL)
        return { "n" : "Steel", "d" : 7.85, "c" : color(0.30, 0.30, 0.32) };
    if (m == ArvMat.ALU)
        return { "n" : "Aluminium 6061", "d" : 2.70, "c" : color(0.72, 0.73, 0.75) };
    if (m == ArvMat.BRASS)
        return { "n" : "Brass", "d" : 8.5, "c" : color(0.80, 0.62, 0.25) };
    if (m == ArvMat.RUBBER)
        return { "n" : "Rubber tyre + foam insert (effective)", "d" : 0.55, "c" : color(0.06, 0.06, 0.06) };
    if (m == ArvMat.SERVO)
        return { "n" : "Servo (effective density)", "d" : 1.9, "c" : color(0.05, 0.05, 0.06) };
    if (m == ArvMat.MOTOR)
        return { "n" : "Gearmotor (effective density)", "d" : 2.4, "c" : color(0.55, 0.56, 0.58) };
    if (m == ArvMat.LIPO)
        return { "n" : "LiPo hard case (effective density)", "d" : 1.55, "c" : color(0.10, 0.20, 0.55) };
    if (m == ArvMat.GLASS)
        return { "n" : "Optical window / lens", "d" : 2.5, "c" : color(0.02, 0.02, 0.03, 0.9) };
    return { "n" : "PCB assembly (effective)", "d" : 0.6, "c" : color(0.05, 0.35, 0.18) };
}

export function arvColor(m is ArvMat) returns Color
{
    return matOf(m).c;
}

// finish a part: boolean, name, part number, description, material, appearance, studio attribute
function fin(context is Context, B is map, name is string, pn is string, desc is string, m is ArvMat) returns Query
{
    stg(context, B);
    const q = posQ(B);
    props(context, q, name, pn, desc, m);
    return q;
}

function props(context is Context, q is Query, name is string, pn is string, desc is string, m is ArvMat)
{
    const mm = matOf(m);
    setProperty(context, { "entities" : q, "propertyType" : PropertyType.NAME, "value" : name });
    setProperty(context, { "entities" : q, "propertyType" : PropertyType.PART_NUMBER, "value" : pn });
    const lk = curLink(context);
    var sn = 0;
    try silent
    {
        sn = getVariable(context, "arvSerial");
    }
    sn += 1;
    setVariable(context, "arvSerial", sn);
    setProperty(context, { "entities" : q, "propertyType" : PropertyType.DESCRIPTION, "value" : lk == "" ? desc : desc ~ " [" ~ lk ~ " #" ~ sn ~ "]" });
    setProperty(context, { "entities" : q, "propertyType" : PropertyType.MATERIAL, "value" : material(mm.n, mm.d * gram / centimeter ^ 3) });
    setProperty(context, { "entities" : q, "propertyType" : PropertyType.APPEARANCE, "value" : mm.c });
    setAttribute(context, { "entities" : q, "attribute" : { "arvPart" : name, "pn" : pn, "link" : curLink(context), "hw" : (m == ArvMat.STEEL || m == ArvMat.BRASS), "serial" : sn } });
}

// fillet edges of B parallel to dir; silently skipped if the kernel refuses
function soft(context is Context, B is map, dir is Vector, r is number)
{
    try silent
    {
        opFillet(context, B.id + nx(B), { "entities" : qParallelEdges(qOwnedByBody(posQ(B), EntityType.EDGE), dir), "radius" : r * millimeter });
    }
}

// chamfer edges of B parallel to dir
function chm(context is Context, B is map, dir is Vector, d is number)
{
    try silent
    {
        opChamfer(context, B.id + nx(B), { "entities" : qParallelEdges(qOwnedByBody(posQ(B), EntityType.EDGE), dir), "chamferType" : ChamferType.EQUAL_OFFSETS, "width" : d * millimeter });
    }
}

// ------------------------------------------------------------------ hardware (purchased) bodies
// Hardware is collected in the context so a builder can switch it off; every body is a real part
// (name, part number, material) so the BOM and MM FORGE see each screw and insert.
function hwOn(context is Context) returns boolean
{
    var r = true;
    try silent
    {
        r = getVariable(context, "arvHardware") != false;
    }
    return r;
}

// socket/button head screw: head top at point h, shank along unit direction dv (into the parts), length L
export function screw(context is Context, id is Id, h is array, dv is array, L is number, d is number, button is boolean)
{
    if (!hwOn(context))
        return;
    const hd = button ? (d == 3 ? 5.7 : 7.6) : (d == 3 ? 5.5 : (d == 2 ? 3.8 : (d == 4 ? 7 : 8.5)));
    const hh = button ? d * 0.55 : d;
    const e = normalize(vector(dv[0], dv[1], dv[2]));
    const H = v(h[0], h[1], h[2]);
    fCylinder(context, id + "h", { "bottomCenter" : H - e * hh * millimeter, "topCenter" : H, "radius" : hd / 2 * millimeter });
    fCylinder(context, id + "s", { "bottomCenter" : H, "topCenter" : H + e * L * millimeter, "radius" : d / 2 * 0.98 * millimeter });
    opBoolean(context, id + "u", { "tools" : qUnion([qCreatedBy(id + "h", EntityType.BODY), qCreatedBy(id + "s", EntityType.BODY)]), "operationType" : BooleanOperationType.UNION });
    const nm = (button ? "SCREW M" ~ d ~ "x" ~ L ~ " ISO 7380" : "SCREW M" ~ d ~ "x" ~ L ~ " ISO 4762");
    const pn = (button ? "PUR-FAS-M" ~ d ~ "x" ~ L ~ "-BHCS" : "PUR-FAS-M" ~ d ~ "x" ~ L ~ "-SHCS");
    props(context, qUnion([qCreatedBy(id + "h", EntityType.BODY), qCreatedBy(id + "s", EntityType.BODY)]), nm, pn, nm ~ ", steel 10.9 black oxide", ArvMat.STEEL);
}

// heat-set insert: mouth at m, axis direction dv (into the part), M3 x 5.7 (OD 4.6)
export function insert(context is Context, id is Id, m is array, dv is array)
{
    if (!hwOn(context))
        return;
    const e = normalize(vector(dv[0], dv[1], dv[2]));
    const M = v(m[0], m[1], m[2]);
    fCylinder(context, id + "c", { "bottomCenter" : M, "topCenter" : M + e * 5.7 * millimeter, "radius" : 2.3 * millimeter });
    fCylinder(context, id + "b", { "bottomCenter" : M - e * 0.5 * millimeter, "topCenter" : M + e * 6.2 * millimeter, "radius" : 1.5 * millimeter });
    opBoolean(context, id + "s", { "tools" : qCreatedBy(id + "b", EntityType.BODY), "targets" : qCreatedBy(id + "c", EntityType.BODY), "operationType" : BooleanOperationType.SUBTRACTION });
    props(context, qCreatedBy(id + "c", EntityType.BODY), "INSERT M3x5.7 HEAT-SET", "PUR-FAS-M3-INSERT", "M3 x 5.7 brass knurled heat-set insert (OD 4.6)", ArvMat.BRASS);
}

// screw + insert pair (clearance hole and insert pocket are cut by the part builders)
function joint(context is Context, id is Id, h is array, dv is array, L is number, grip is number)
{
    screw(context, id + "s", h, dv, L, 3, false);
    insert(context, id + "i", [h[0] + dv[0] * grip, h[1] + dv[1] * grip, h[2] + dv[2] * grip], dv);
}

// ------------------------------------------------------------------ diagnostics
// builders record their last step; a caught failure is stored in the variable arvError (read via the eval API)
function trace(context is Context, what is string)
{
    setVariable(context, "arvTrace", what);
}

function lastTrace(context is Context) returns string
{
    var r = "";
    try silent
    {
        r = getVariable(context, "arvTrace");
    }
    return r;
}

function recordError(context is Context, id is Id, where is string, e)
{
    var prev = "";
    try silent
    {
        prev = getVariable(context, "arvError");
    }
    setVariable(context, "arvError", prev ~ " || " ~ where ~ " @ " ~ lastTrace(context) ~ " : " ~ toString(e) ~ (e is map && e.message != undefined ? " / " ~ toString(e.message) : ""));
    reportFeatureWarning(context, id, "ARV: " ~ where ~ " failed at " ~ lastTrace(context));
}

// ------------------------------------------------------------------ mate connectors
// part-owned connector with world-aligned orientation (z along 'zd'); used by assembly mates
function mc(context is Context, id is Id, owner is Query, o is array, zd is Vector, xd is Vector)
{
    try silent
    {
        opMateConnector(context, id, { "coordSystem" : coordSystem(v(o[0], o[1], o[2]), normalize(xd), normalize(zd)), "owner" : owner });
    }
}


// =====================================================================================================
//  PACKAGING (SK04): purchased-component envelopes at their installed positions.
//  One table, used by the master sketch SK04 and by every builder that places or clears a component.
//  Box = [x0, x1, y0, y1, z0, z1] in the vehicle frame. Dimensions = vendor data (VERIFY flags in BOM).
// =====================================================================================================
export function pack(P is map, g is map) returns map
{
    var k = {};
    const lz = g.lowZ1;                                  // lower tray plate top (38)
    const dz = g.deckZ1;                                 // upper deck top (87)
    // lower tray level: power + motor driver (Y- = power side, Y+ = logic power)
    k.MDD10A = [-16, 68, -66, -4, lz + 4, lz + 4 + 20];                  // Cytron MDD10A 84 x 62 x 20 on 4 mm stand-offs
    k.DCDC19 = [-16, 44, 16, 66, lz + 1, lz + 1 + 22];                   // 19 V buck-boost 60 x 50 x 22
    k.FUSE = [48, 103, 26, 66, lz, lz + 25];                             // 2-way ATO fuse holder 55 x 40 x 25
    k.BUCK5 = [66, 101.6, -66, -27.9, lz + 4, lz + 4 + 10];              // 5 V 5 A buck 35.6 x 38.1
    k.BEC6 = [70, 95, -22, 18, lz + 4, lz + 4 + 12];                     // 6 V 5 A servo BEC 25 x 40
    // upper deck: compute
    k.JETSON = [g.deckX1 - 103, g.deckX1, -45.25, 45.25, dz + 6, dz + 6 + 34.8];   // Orin Nano Super dev kit
    k.TEENSY = [g.deckX0 + 2, g.deckX0 + 52, -35, 35, dz + 5, dz + 5 + 18];         // Teensy 4.1 carrier 50 x 70
    k.IMU = [-20, 5.4, 46, 63.8, dz + 12, dz + 12 + 4];                  // BNO085 on plinth (near CG)
    k.RX = [g.deckX0 + 2, g.deckX0 + 42, -66, -44, dz + 2, dz + 2 + 10]; // SBUS receiver 40 x 22
    // battery (removable, rear tunnel)
    k.BATTERY = [g.batX0, g.batX1, -P.batteryWidth / 2, P.batteryWidth / 2, g.batZ0, g.batZ0 + P.batteryHeight];
    // relay / E-stop
    k.RELAY = [-100, -60, -26, 26, g.ZFT + 4.5, g.ZFT + 4.5 + 19];          // watchdog relay module in rear tub bay
    k.ESTOP_X = g.xTail + 40;
    k.ESTOP_Y = 58;
    // sensors
    k.D435 = [P.cameraX - 12.5, P.cameraX + 12.5, -45, 45, P.cameraHeight - 12.5, P.cameraHeight + 12.5];
    k.CAM_Y = 58;                                        // IMX219 160 deg beside the depth camera (left)
    k.TOF_X = g.xNose + 19;                              // fascia front plane
    k.TOF_Z = P.frontSensorZ;
    k.SERVO = [g.servoX - 10, g.servoX + 10, -30.3, 10.2, g.servoZ + 4, g.servoZ + 42.5];
    return k;
}

// ------------------------------------------------------------------ sketch helpers
function skl(sk is Sketch, n is string, a is array, b is array, c is boolean)
{
    skLineSegment(sk, n, { "start" : vector(a[0], a[1]) * millimeter, "end" : vector(b[0], b[1]) * millimeter, "construction" : c });
}

function skc(sk is Sketch, n is string, c is array, r is number, cons is boolean)
{
    skCircle(sk, n, { "center" : vector(c[0], c[1]) * millimeter, "radius" : r * millimeter, "construction" : cons });
}

function skr(sk is Sketch, n is string, x0 is number, y0 is number, x1 is number, y1 is number, c is boolean)
{
    skRectangle(sk, n, { "firstCorner" : vector(x0, y0) * millimeter, "secondCorner" : vector(x1, y1) * millimeter, "construction" : c });
}

function skp(sk is Sketch, n is string, c is array)
{
    skPoint(sk, n, { "position" : vector(c[0], c[1]) * millimeter });
}

function skpoly(sk is Sketch, n is string, pts is array, c is boolean)
{
    const m = size(pts);
    for (var i = 0; i < m; i += 1)
        skl(sk, n ~ i, pts[i], pts[(i + 1) % m], c);
}

function skBox(sk is Sketch, n is string, b is array, plan is boolean)
{
    if (plan)
        skr(sk, n, b[0], b[2], b[1], b[3], false);
    else
        skr(sk, n, b[0], b[4], b[1], b[5], false);
}

// plan-view mirror across the XZ plane (x, y) -> (x, -y)
function mirPlan(pts is array) returns array
{
    var o = [];
    for (var p in pts)
        o = append(o, [p[0], -p[1]]);
    return o;
}

// front-view mirror (local x = world Y): (y, z) -> (-y, z)
function mirY(pts is array) returns array
{
    var o = [];
    for (var p in pts)
        o = append(o, [-p[0], p[1]]);
    return o;
}

// ------------------------------------------------------------------ body profiles (shared by SK02/SK05 and all body parts)
// hull (upper shell over the full length, underside zHB)
export function hullSide(P is map, g is map) returns array
{
    return [[g.xTail, g.zHB], [g.xNose, g.zHB], [g.xNose, g.zHB + 5], [g.xF - 30, g.zRoof], [g.xR - 15, g.zRoof], [g.xTail, g.zRoof - 8]];
}

export function hullPlan(P is map, g is map) returns array
{
    return [[g.xTail, -g.ys + 16], [g.xTail + 18, -g.ys], [g.xNose - 22, -g.ys], [g.xNose, -g.ys + 40], [g.xNose, g.ys - 40], [g.xNose - 22, g.ys], [g.xTail + 18, g.ys], [g.xTail, g.ys - 16]];
}

export function hullFront(P is map, g is map) returns array
{
    return [[-g.ys, g.zHB], [g.ys, g.zHB], [g.ys, g.zSh], [g.yr, g.zRoof], [-g.yr, g.zRoof], [-g.ys, g.zSh]];
}

// side pods between the wheels (armoured flanks, hide chassis + trailing-arm pivots)
export function podSide(P is map, g is map) returns array
{
    return [[g.podX0, g.podZ0 + 10], [g.podX0 + 10, g.podZ0], [g.podX1 - 10, g.podZ0], [g.podX1, g.podZ0 + 12], [g.podX1, g.zSh + 1], [g.podX0, g.zSh + 1]];
}

export function podPlan(P is map, g is map) returns array
{
    return [[g.podX0, 78], [g.podX1, 78], [g.podX1, g.ys], [g.podX0, g.ys]];
}

export function podFront(P is map, g is map) returns array
{
    return [[78, g.podZ0], [g.ys - 8, g.podZ0], [g.ys, g.podZ0 + 10], [g.ys, g.zSh + 1], [78, g.zSh + 1]];
}

// centre bay above the tub (joins pods, tail and rear-upper zones into one cavity; narrow ahead of the pods)
export function bayX1(g is map) returns number
{
    return g.X1 - 1.1;
}

export function baySide(P is map, g is map) returns array
{
    return [[g.podX0 - 27, g.ZTOP + 0.5], [bayX1(g), g.ZTOP + 0.5], [bayX1(g), g.zSh + 1], [g.podX0 - 27, g.zSh + 1]];
}

export function bayPlan(P is map, g is map) returns array
{
    return [[g.podX0 - 27, -100], [g.podX1, -100], [g.podX1, -80], [bayX1(g), -80], [bayX1(g), 80], [g.podX1, 80], [g.podX1, 100], [g.podX0 - 27, 100]];
}

export function bayFront(P is map, g is map) returns array
{
    return [[-100, g.ZTOP + 0.5], [100, g.ZTOP + 0.5], [100, g.zSh + 1], [-100, g.zSh + 1]];
}

// nose box (between the front wheel wells, above the steering linkage: hides servo top + bulkhead)
export function noseSide(P is map, g is map) returns array
{
    return [[g.towerF1 + 16, 52], [g.xNose - 9, 52], [g.xNose - 9, g.zHB + 1], [g.towerF1 + 16, g.zHB + 1]];
}

export function nosePlan(P is map, g is map) returns array
{
    return [[g.towerF1 + 16, -62], [g.xNose - 9, -62], [g.xNose - 9, 62], [g.towerF1 + 16, 62]];
}

export function noseFront(P is map, g is map) returns array
{
    return [[-54, 52], [54, 52], [62, 60], [62, g.zHB + 1], [-62, g.zHB + 1], [-62, 60]];
}

// tail box (between the rear wheel wells, above the motors: battery tunnel + E-stop)
export function tailSide(P is map, g is map) returns array
{
    return [[g.xTail, g.ZTOP + 4], [g.podX0 + 1, g.ZTOP + 4], [g.podX0 + 1, g.zHB + 1], [g.xTail, g.zHB + 1]];
}

export function tailPlan(P is map, g is map) returns array
{
    return [[g.xTail, -64], [g.podX0 + 1, -72], [g.podX0 + 1, 72], [g.xTail, 64]];
}

export function tailFront(P is map, g is map) returns array
{
    return [[-66, g.ZTOP + 4], [66, g.ZTOP + 4], [72, g.ZTOP + 12], [72, g.zHB + 1], [-72, g.zHB + 1], [-72, g.ZTOP + 12]];
}

// front fascia (upper sensor fascia, faceted)
export function fasciaPlan(P is map, g is map) returns array
{
    const xf = g.xNose + 19;
    const a = 32 * tan(P.frontSensorSplay * degree);
    return [[g.xNose - 9, -62], [xf - a, -62], [xf, -30], [xf, 30], [xf - a, 62], [g.xNose - 9, 62]];
}

export function fasciaSide(P is map, g is map) returns array
{
    const xf = g.xNose + 19;
    return [[g.xNose - 9, 76], [xf, 76], [xf, g.zHB - 12], [g.xNose + 4, g.zHB], [g.xNose - 9, g.zHB]];
}

// structural lower bumper
export function bumperPlan(P is map, g is map) returns array
{
    const x0 = g.servoX + 11;
    return [[x0, -64], [x0 + 8, -100], [x0 + 18, -100], [x0 + 27, -60], [x0 + 27, 60], [x0 + 18, 100], [x0 + 8, 100], [x0, 64]];
}

export function bumperSide(P is map, g is map) returns array
{
    const x0 = g.servoX + 11;
    return [[x0, 22], [x0 + 20, 22], [x0 + 27, 30], [x0 + 27, 68], [x0 + 20, 76], [x0, 76]];
}

// camera pod (low-profile, on the front roof slope)
export function podCamSide(P is map, g is map) returns array
{
    const xc = P.cameraX;
    const zc = P.cameraHeight;
    return [[xc - 44, zc - 16], [xc + 16, zc - 16], [xc + 16, zc + 12], [xc + 8, zc + 17], [xc - 44, zc + 17]];
}

export function podCamPlan(P is map, g is map) returns array
{
    const xc = P.cameraX;
    return [[xc - 44, -66], [xc + 6, -66], [xc + 16, -56], [xc + 16, 72], [xc + 6, 80], [xc - 44, 80]];
}

// fender (partial tactical eyebrow) for a wheel at x = xc
export function fenderSide(P is map, g is map, xc is number) returns array
{
    const L = P.wheelDiameter / 2 + 34;
    return [[xc - L, g.zHB - 14], [xc - L + 16, g.zHB + 6], [xc - L + 30, g.zHB + 16], [xc + L - 30, g.zHB + 16], [xc + L - 16, g.zHB + 6], [xc + L, g.zHB - 14], [xc + L - 8, g.zHB - 14], [xc - L + 8, g.zHB - 14]];
}

export function fenderFront(P is map, g is map) returns array
{
    const yo = P.fenderWidth;
    return [[g.ys - 3, g.zHB - 16], [yo, g.zHB - 16], [yo, g.zHB + 2], [yo - 12, g.zHB + 12], [g.ys - 3, g.zHB + 18]];
}

// shell/chassis interface points (plan): body screws into tub flange inserts
export function bodyMounts(P is map, g is map) returns array
{
    return [[-20, g.HW - 4.5], [-20, -g.HW + 4.5], [40, g.HW - 4.5], [40, -g.HW + 4.5], [80, g.HW - 4.5], [80, -g.HW + 4.5]];
}

// =====================================================================================================
//  00_CAR_MASTER layers
// =====================================================================================================
export enum ArvLayer
{
    annotation { "Name" : "SK01 top vehicle layout" }
    SK01_TOP_LAYOUT,
    annotation { "Name" : "SK02 side profile" }
    SK02_SIDE_PROFILE,
    annotation { "Name" : "SK03 front / track interface" }
    SK03_FRONT_TRACK,
    annotation { "Name" : "SK04 internal packaging" }
    SK04_INTERNAL_PACKAGING,
    annotation { "Name" : "SK05 body / service architecture" }
    SK05_BODY_SERVICE,
    annotation { "Name" : "REF datums + interface connectors" }
    REF_DATUMS
}

function tagSketch(context is Context, id is Id, l is string, P is map)
{
    setAttribute(context, { "entities" : qCreatedBy(id, EntityType.BODY), "attribute" : { "arvMaster" : l, "P" : P } });
}

annotation { "Feature Type Name" : "ARV Master Layer", "Feature Name Template" : "#layer", "Feature Type Description" : "One controlled layer of 00_CAR_MASTER (sketch / datums / connectors), driven by the master #variables. Every body carries the master parameter map for downstream Derived features." }
export const arvMaster = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation { "Name" : "Layer" }
        definition.layer is ArvLayer;
    }
    {
        const P = masterP(context);
        const g = geo(P);
        const k = pack(P, g);
        const l = definition.layer;
        const ln = toString(l);
        const skId = id + "sk";
        setFeatureComputedParameter(context, id, { "name" : "layer", "value" : ln });
        if (l == ArvLayer.SK01_TOP_LAYOUT)
        {
            const sk = newSketchOnPlane(context, skId, { "sketchPlane" : plane(v(0, 0, 0), Z, X) });
            skl(sk, "centerline", [g.xTail - 30, 0], [g.xNose + 45, 0], true);
            skl(sk, "frontAxle", [g.xF, -g.yW - 30], [g.xF, g.yW + 30], true);
            skl(sk, "rearAxle", [g.xR, -g.yW - 30], [g.xR, g.yW + 30], true);
            for (var s in [1, -1])
            {
                const t = s > 0 ? "L" : "R";
                skp(sk, "wheelF" ~ t, [g.xF, s * g.yW]);
                skp(sk, "wheelR" ~ t, [g.xR, s * g.yW]);
                skr(sk, "tyreF" ~ t, g.xF - g.zA, s * g.yW - P.wheelWidth / 2, g.xF + g.zA, s * g.yW + P.wheelWidth / 2, false);
                skr(sk, "tyreR" ~ t, g.xR - g.zA, s * g.yW - P.wheelWidth / 2, g.xR + g.zA, s * g.yW + P.wheelWidth / 2, false);
                skc(sk, "sweepF" ~ t, [g.xF, s * g.KY], g.sweepR, true);
                skp(sk, "kingpin" ~ t, [g.xF, s * g.KY]);
            }
            skr(sk, "chassisTub", g.X0, -g.HW, g.X1, g.HW, false);
            skr(sk, "bulkhead", g.X1, -62, g.servoX + 11, 62, true);
            skpoly(sk, "bodyEnvelope", hullPlan(P, g), false);
            skpoly(sk, "podEnvelope", podPlan(P, g), true);
            skpoly(sk, "bumperF", bumperPlan(P, g), false);
            skr(sk, "electronicsRegion", g.deckX0, -70, g.deckX1, 70, true);
            skBox(sk, "batteryRegion", k.BATTERY, true);
            skp(sk, "camera", [P.cameraX, 0]);
            skp(sk, "tofFC", [k.TOF_X, 0]);
            skp(sk, "tofFL", [k.TOF_X - 16 * tan(P.frontSensorSplay * degree), 46]);
            skp(sk, "tofFR", [k.TOF_X - 16 * tan(P.frontSensorSplay * degree), -46]);
            skp(sk, "tofRear", [g.xTail - 16, 0]);
            skSolve(sk);
            tagSketch(context, skId, ln, P);
        }
        else if (l == ArvLayer.SK02_SIDE_PROFILE)
        {
            const sk = newSketchOnPlane(context, skId, { "sketchPlane" : plane(v(0, 0, 0), -Y, X) });
            skl(sk, "ground", [g.xTail - 40, 0], [g.xNose + 50, 0], false);
            skl(sk, "axleLine", [g.xR - 60, g.zA], [g.xF + 60, g.zA], true);
            skc(sk, "tyreF", [g.xF, g.zA], g.zA, false);
            skc(sk, "tyreR", [g.xR, g.zA], g.zA, false);
            skc(sk, "tyreFbump", [g.xF, g.zA + P.bumpTravel], g.zA, true);
            skc(sk, "tyreRbump", [g.xR, g.zA + P.bumpTravel], g.zA, true);
            skl(sk, "clearance", [g.X0, P.groundClearance], [g.X1, P.groundClearance], true);
            skr(sk, "tub", g.X0, g.ZF, g.X1, g.ZTOP, false);
            skr(sk, "electronicsEnvelope", g.deckX0, g.lowZ0, g.deckX1, k.JETSON[5], true);
            skBox(sk, "battery", k.BATTERY, false);
            skpoly(sk, "hullProfile", hullSide(P, g), false);
            skpoly(sk, "podProfile", podSide(P, g), false);
            skpoly(sk, "cameraPod", podCamSide(P, g), false);
            skpoly(sk, "bumper", bumperSide(P, g), false);
            skpoly(sk, "fascia", fasciaSide(P, g), false);
            skl(sk, "shoulder", [g.xTail, g.zSh], [g.xNose, g.zSh], true);
            skl(sk, "hatchFront", [g.hatchX1, g.zSh], [g.hatchX1, g.zRoof], true);
            skl(sk, "hatchRear", [g.hatchX0, g.zSh], [g.hatchX0, g.zRoof], true);
            skl(sk, "batteryPanel", [g.xTail, g.ZTOP + 6], [g.xTail, g.zRoof - 12], true);
            skSolve(sk);
            tagSketch(context, skId, ln, P);
        }
        else if (l == ArvLayer.SK03_FRONT_TRACK)
        {
            const sk = newSketchOnPlane(context, skId, { "sketchPlane" : plane(v(g.xF, 0, 0), X, Y) });
            skl(sk, "ground", [-g.yW - 60, 0], [g.yW + 60, 0], false);
            for (var s in [1, -1])
            {
                const t = s > 0 ? "L" : "R";
                skr(sk, "tyre" ~ t, s * g.yW - P.wheelWidth / 2, 0, s * g.yW + P.wheelWidth / 2, P.wheelDiameter, false);
                skl(sk, "kingpinAxis" ~ t, [s * g.KY, g.kLowZ], [s * g.KY, g.kUpZ], true);
                skp(sk, "lowerBall" ~ t, [s * g.KY, g.kLowZ]);
                skp(sk, "upperBall" ~ t, [s * g.KY, g.kUpZ]);
                skl(sk, "lowerArm" ~ t, [s * g.hingeY, g.kLowZ], [s * g.KY, g.kLowZ], true);
                skl(sk, "camberLink" ~ t, [s * g.camberInY, g.kUpZ], [s * g.KY, g.kUpZ], true);
                skpoly(sk, "fender" ~ t, s > 0 ? fenderFront(P, g) : mirY(fenderFront(P, g)), false);
            }
            skr(sk, "chassisWidth", -g.HW, g.ZF, g.HW, g.ZTOP, false);
            skpoly(sk, "bodySection", hullFront(P, g), false);
            skpoly(sk, "noseSection", noseFront(P, g), true);
            skl(sk, "fasciaWidth", [-62, P.frontSensorZ], [62, P.frontSensorZ], true);
            skl(sk, "groundClearance", [-g.HW, P.groundClearance], [g.HW, P.groundClearance], true);
            skSolve(sk);
            tagSketch(context, skId, ln, P);
        }
        else if (l == ArvLayer.SK04_INTERNAL_PACKAGING)
        {
            const sk = newSketchOnPlane(context, skId, { "sketchPlane" : plane(v(0, 0, g.deckZ1), Z, X) });
            for (var n in ["MDD10A", "DCDC19", "FUSE", "BUCK5", "BEC6", "JETSON", "TEENSY", "IMU", "RX", "BATTERY", "RELAY", "D435", "SERVO"])
                skBox(sk, n, k[n], true);
            skr(sk, "IMX219", P.cameraX - 6, k.CAM_Y - 12.5, P.cameraX + 6, k.CAM_Y + 12.5, false);
            skc(sk, "ESTOP", [k.ESTOP_X, k.ESTOP_Y], 20, false);
            skr(sk, "tunnel", g.xTail, -g.tunY - g.tunWall, g.tunX1 + g.tunWall, g.tunY + g.tunWall, true);
            skr(sk, "trayDeck", g.deckX0, -70, g.deckX1, 70, true);
            skSolve(sk);
            tagSketch(context, skId, ln, P);
        }
        else if (l == ArvLayer.SK05_BODY_SERVICE)
        {
            const sk = newSketchOnPlane(context, skId, { "sketchPlane" : plane(v(0, 0, g.zSh), Z, X) });
            skpoly(sk, "shell", hullPlan(P, g), false);
            skr(sk, "centerHatch", g.hatchX0, -g.ys, g.hatchX1, g.ys, false);
            skr(sk, "batteryPanel", g.xTail - 3, -36, g.xTail + 3, 36, false);
            skpoly(sk, "fascia", fasciaPlan(P, g), false);
            skpoly(sk, "cameraPod", podCamPlan(P, g), false);
            var fi = 0;
            for (var s in [1, -1])
                for (var xc in [g.xF, g.xR])
                {
                    skr(sk, "fender" ~ fi, xc - P.wheelDiameter / 2 - 34, s * (g.ys - 3), xc + P.wheelDiameter / 2 + 34, s * P.fenderWidth, false);
                    fi += 1;
                }
            var mi = 0;
            for (var p in bodyMounts(P, g))
            {
                skp(sk, "mount" ~ mi, [p[0], p[1]]);
                mi += 1;
            }
            skSolve(sk);
            tagSketch(context, skId, ln, P);
        }
        else
        {
            // datum planes and interface connectors
            const planes = [["PL_FRONT_AXLE", plane(v(g.xF, 0, 0), X, Y)], ["PL_REAR_AXLE", plane(v(g.xR, 0, 0), X, Y)],
                    ["PL_AXLE_HEIGHT", plane(v(0, 0, g.zA), Z, X)], ["PL_TUB_TOP", plane(v(0, 0, g.ZTOP), Z, X)],
                    ["PL_TRAY_DECK", plane(v(0, 0, g.deckZ1), Z, X)], ["PL_SHOULDER", plane(v(0, 0, g.zSh), Z, X)]];
            var i = 0;
            for (var pl in planes)
            {
                opPlane(context, id + ("pl" ~ i), { "plane" : pl[1], "width" : 0.4 * meter, "height" : 0.3 * meter });
                setProperty(context, { "entities" : qCreatedBy(id + ("pl" ~ i), EntityType.BODY), "propertyType" : PropertyType.NAME, "value" : pl[0] });
                setAttribute(context, { "entities" : qCreatedBy(id + ("pl" ~ i), EntityType.BODY), "attribute" : { "arvMaster" : ln, "P" : P } });
                i += 1;
            }
            // implicit interface mate connectors: wheel centres, steering axes, chassis origin, camera
            const cas = P.casterAngle * degree;
            const kz = normalize(vector(-sin(cas), 0, cos(cas)));
            const cons = [[[g.xF, g.yW, g.zA], Y], [[g.xF, -g.yW, g.zA], Y], [[g.xR, g.yW, g.zA], Y], [[g.xR, -g.yW, g.zA], Y],
                    [[g.xF, g.KY, g.zA], kz], [[g.xF, -g.KY, g.zA], kz], [[0, 0, g.ZF], Z], [[P.cameraX + 12.5, 0, P.cameraHeight], X]];
            var j = 0;
            for (var c in cons)
            {
                const zz = normalize(c[1]);
                const xx = abs(dot(zz, Y)) > 0.9 ? X : normalize(cross(Y, zz));
                opMateConnector(context, id + ("mc" ~ j), { "coordSystem" : coordSystem(v(c[0][0], c[0][1], c[0][2]), xx, zz) });
                j += 1;
            }
            const sk = newSketchOnPlane(context, skId, { "sketchPlane" : plane(v(0, 0, 0), Z, X) });
            skp(sk, "origin", [0, 0]);
            skSolve(sk);
            tagSketch(context, skId, ln, P);
        }
    });


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
        cax(context, B, "X", [t1 + 1.5, s * g.camberInY, g.kUpZ], 10, 3, true);
    }
    stg(context, B);
    for (var s in [1, -1])
    {
        for (var lx in [t0 - 1.5, t1 + 1.5])
            hClr(context, B, [lx, s * 20, 29], [lx, s * 20, 35]);
        hIns(context, B, [t0 - 3, s * g.shockFY, g.shockFUpZ], [1, 0, 0], 6);
        hIns(context, B, [t1 + 3, s * g.camberInY, g.kUpZ], [-1, 0, 0], 6);
    }
    // body nose locating pin holes (Ø4.2 x 8) in the top edge
    for (var y in [-30, 30])
        cyl(context, B, [(t0 + t1) / 2, y, top + 0.1], [(t0 + t1) / 2, y, top - 8], 2.1, false);
    wb(context, B, t0 - 1, t1 + 1, -8, 8, 56, top - 14, false);
    for (var y in [-44, 44])
        wb(context, B, t0 - 1, t1 + 1, y - 8, y + 8, 76, top - 12, false);
    fin(context, B, "FRONT_SHOCK_TOWER", "ARV-20-002", "Front shock tower: shock ball studs (rearward) + camber-link studs (forward), 4x M3 into bulkhead, body-nose locating pins. PA-CF.", ArvMat.PACF);
    var n = 0;
    for (var s in [1, -1])
    {
        for (var lx in [t0 - 1.5, t1 + 1.5])
        {
            screw(context, id + ("s" ~ n), [lx, s * 20, 34], [0, 0, -1], 10, 3, false);
            n += 1;
        }
        insert(context, id + ("a" ~ n), [t0 - 3, s * g.shockFY, g.shockFUpZ], [1, 0, 0]);
        insert(context, id + ("c" ~ n), [t1 + 3, s * g.camberInY, g.kUpZ], [-1, 0, 0]);
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
            ["arml", function(i) { pLowerArm(context, i, P, g, 1); }, "LNK_FRONT_FRAME"],
            ["armr", function(i) { pLowerArm(context, i, P, g, -1); }, "LNK_FRONT_FRAME"]];
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
            // camber link: steered upper ball -> tower stud
            const ub = arr(Tst * (casterT(P, g) * v(g.xF, s * g.KY, g.kUpZ)));
            link(context, "LNK_CAMBER_LINK_" ~ t);
            rodLink(context, cid + "cam", ub, [g.towerF1 + 7.5, s * g.camberInY, g.kUpZ], "CAMBER_LINK_" ~ t, "PUR-STR-CAMBER-M3", "M3 turnbuckle camber link, 57 mm ball-to-ball");
            link(context, "LNK_FRONT_FRAME");
            shockBody(context, cid + "shk", [g.shockFX, s * g.shockFY, g.kLowZ + 4 + 3.4], [g.towerF0 - 12, s * g.shockFY, g.shockFUpZ], "SHOCK_F" ~ t, "PUR-SUS-SHOCK-80");
        }
        catch (e)
        {
            recordError(context, id, "corner " ~ t, e);
        }
    }
}


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


// =====================================================================================================
//  BODY SYSTEM  (60_BODY_MASTER -> 61..66 body studios)
//  Hard-surface shell = union of zones, each zone = side ∩ plan ∩ front profile (three-view solid),
//  hollowed with offset profiles (2.4 mm wall). Service openings are cut from the same shell so every
//  panel matches its opening exactly.
// =====================================================================================================
export function bodyT() returns number
{
    return 2.4;
}

// rear upper pod extension (covers the rear shock tops, stays above the trailing arms)
export function podRearSide(P is map, g is map) returns array
{
    return [[g.podX0 - 27, 90], [g.podX0 + 10, 90], [g.podX0 + 10, g.zSh + 1], [g.podX0 - 27, g.zSh + 1]];
}

export function podRearPlan(P is map, g is map) returns array
{
    return [[g.podX0 - 27, -100], [g.podX0 + 10, -100], [g.podX0 + 10, 100], [g.podX0 - 27, 100]];
}

export function podRearFront(P is map, g is map) returns array
{
    return [[-100, 90], [100, 90], [100, g.zSh + 1], [-100, g.zSh + 1]];
}

export function tailSide2(P is map, g is map) returns array
{
    return [[g.xTail, g.ZTOP + 4], [g.podX0 + 15, g.ZTOP + 4], [g.podX0 + 15, g.zSh + 1], [g.xTail, g.zSh + 1]];
}

export function tailFront2(P is map, g is map) returns array
{
    return [[-66, g.ZTOP + 4], [66, g.ZTOP + 4], [72, g.ZTOP + 12], [72, g.zSh + 1], [-72, g.zSh + 1], [-72, g.ZTOP + 12]];
}

export function bodyZones(P is map, g is map) returns array
{
    return [[hullSide(P, g), hullPlan(P, g), hullFront(P, g)],
            [podSide(P, g), podPlan(P, g), podFront(P, g)],
            [podSide(P, g), mirPlan(podPlan(P, g)), mirY(podFront(P, g))],
            [baySide(P, g), bayPlan(P, g), bayFront(P, g)],
            [podRearSide(P, g), podRearPlan(P, g), podRearFront(P, g)],
            [noseSide(P, g), nosePlan(P, g), noseFront(P, g)],
            [tailSide2(P, g), tailPlan(P, g), tailFront2(P, g)]];
}

// closed outer solid of the whole body (all zones)
function bodyOuter(context is Context, B is map, P is map, g is map, add is boolean)
{
    for (var z in bodyZones(P, g))
        tri(context, B, z[0], z[1], z[2], add);
}

// hollow shell (outer - inner), open underneath the pods / nose / tail, open nose front for the fascia
function bodyShell(context is Context, B is map, P is map, g is map)
{
    const t = bodyT();
    trace(context, "shell outer");
    bodyOuter(context, B, P, g, true);
    stg(context, B);
    trace(context, "shell inner");
    for (var z in bodyZones(P, g))
        tri(context, B, offsetPoly(z[0], t), offsetPoly(z[1], t), offsetPoly(z[2], t), false);
    stg(context, B);
    trace(context, "shell openings");
    // open bottoms (flat parts only, chamfers keep their full wall)
    for (var s in [1, -1])
    {
        wb(context, B, g.podX0 + t + 0.5, g.podX1 - t - 0.5, s * (78 + t + 0.5), s * (g.ys - 8.5), g.podZ0 - 5, g.podZ0 + t + 0.5, false);
        wb(context, B, g.podX0 + t + 0.5, g.podX1 - t - 0.5, s * 76.5, s * 81, g.podZ0 - 5, g.ZTOP + 0.6, false);
    }
    // trailing-arm passage through the pod rear walls (clear to full bump)
    for (var s in [1, -1])
        wb(context, B, g.podX0 - 1, g.podX0 + 13, s * 78, s * 104, g.podZ0 - 5, g.ZTOP - 4, false);
    // tail wall slots for the rear-frame arms
    for (var s in [1, -1])
        wb(context, B, g.xTail - 1, g.xTail + t + 1, s * 44.3, s * 56, g.ZTOP + 2.7, g.ZTOP + 12.6, false);
    wb(context, B, g.podX0 - 27 + t + 0.5, g.podX1, -100 + t + 0.5, 100 - t - 0.5, g.ZTOP - 3, g.ZTOP + 0.5 + t + 0.5, false);
    wb(context, B, g.podX1 - 1, bayX1(g) - t - 0.5, -80 + t + 0.5, 80 - t - 0.5, g.ZTOP - 3, g.ZTOP + 0.5 + t + 0.5, false);
    wb(context, B, g.podX0 - 27 + t + 0.5, g.podX0 + 10, -100 + t + 0.5, 100 - t - 0.5, 85, 90 + t + 0.5, false);
    wb(context, B, g.towerF1 + 16 + t + 0.5, g.xNose - 9 + 1, -53.5, 53.5, 45, 52 + t + 0.5, false);
    wb(context, B, g.xTail + t + 0.5, g.podX0 + 15, -65.5, 65.5, g.ZTOP, g.ZTOP + 4 + t + 0.5, false);
    // nose front opening (closed by the fascia)
    wb(context, B, g.xNose - 9 - t - 1, g.xNose - 9 + 1, -62 + t, 62 - t, 76 - 0.01, g.zHB - 0.5, false);
    stg(context, B);
}

// point on the front roof slope (hull side profile) at x
function roofZ(P is map, g is map, x is number) returns number
{
    const x0 = g.xF - 30;
    if (x <= x0)
        return g.zRoof;
    return g.zRoof - (g.zRoof - g.zHB - 5) * (x - x0) / (g.xNose - x0);
}

export function hatchSplitZ(P is map, g is map) returns number
{
    return g.zSh - 10;
}

export function bodySplitX(P is map, g is map) returns number
{
    return 20;
}

// hatch side-screw positions (x, z) on both sides: horizontal M3 into tabs on the main body
export function hatchScrews(P is map, g is map) returns array
{
    const zs = hatchSplitZ(P, g) + 5;
    return [[g.hatchX0 + 16, zs], [g.podX1 - 16, zs]];
}

// fender screw positions (x offsets from wheel centre, z)
export function fenderScrews(P is map, g is map) returns array
{
    return [[-40, g.zHB + 7], [25, g.zHB + 7]];
}

// =====================================================================================================
//  60_BODY_MASTER feature: derived from 00_CAR_MASTER, publishes the body layers for the body studios
// =====================================================================================================
export enum ArvBodyLayer
{
    annotation { "Name" : "BK01 body side profiles" }
    BK01_SIDE,
    annotation { "Name" : "BK02 body plan + service openings" }
    BK02_PLAN,
    annotation { "Name" : "BK03 body sections" }
    BK03_SECTIONS,
    annotation { "Name" : "BK04 outer envelope (reference solid)" }
    BK04_ENVELOPE
}

annotation { "Feature Type Name" : "ARV Body Master", "Feature Name Template" : "#layer", "Feature Type Description" : "Body master layer: hull / pod / nose / tail / fascia / pod / fender profiles and service splits, driven by the derived 00_CAR_MASTER." }
export const arvBodyMaster = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation { "Name" : "Layer" }
        definition.layer is ArvBodyLayer;
    }
    {
        const P = derivedP(context, "arvMaster");
        const g = geo(P);
        const l = definition.layer;
        const ln = toString(l);
        setFeatureComputedParameter(context, id, { "name" : "layer", "value" : ln });
        const skId = id + "sk";
        if (l == ArvBodyLayer.BK01_SIDE)
        {
            const sk = newSketchOnPlane(context, skId, { "sketchPlane" : plane(v(0, 0, 0), -Y, X) });
            var i = 0;
            for (var pl in [hullSide(P, g), podSide(P, g), podRearSide(P, g), noseSide(P, g), tailSide2(P, g), fasciaSide(P, g), bumperSide(P, g), podCamSide(P, g), fenderSide(P, g, g.xF), fenderSide(P, g, g.xR)])
            {
                skpoly(sk, "p" ~ i ~ "_", pl, false);
                i += 1;
            }
            skl(sk, "hatchSplit", [g.hatchX0, hatchSplitZ(P, g)], [g.hatchX1, hatchSplitZ(P, g)], true);
            skl(sk, "bodySplit", [bodySplitX(P, g), g.podZ0], [bodySplitX(P, g), g.zRoof], true);
            skSolve(sk);
        }
        else if (l == ArvBodyLayer.BK02_PLAN)
        {
            const sk = newSketchOnPlane(context, skId, { "sketchPlane" : plane(v(0, 0, g.zSh), Z, X) });
            var i = 0;
            for (var pl in [hullPlan(P, g), podPlan(P, g), podRearPlan(P, g), nosePlan(P, g), tailPlan(P, g), fasciaPlan(P, g), bumperPlan(P, g), podCamPlan(P, g)])
            {
                skpoly(sk, "p" ~ i ~ "_", pl, false);
                i += 1;
            }
            skr(sk, "centerHatch", g.hatchX0, -g.ys, g.hatchX1, g.ys, true);
            skr(sk, "batteryPanel", g.xTail - 1, -44, g.xTail + 3, 44, true);
            skSolve(sk);
        }
        else if (l == ArvBodyLayer.BK03_SECTIONS)
        {
            const sk = newSketchOnPlane(context, skId, { "sketchPlane" : plane(v(g.xF, 0, 0), X, Y) });
            var i = 0;
            for (var pl in [hullFront(P, g), podFront(P, g), noseFront(P, g), tailFront2(P, g), fenderFront(P, g), mirY(fenderFront(P, g))])
            {
                skpoly(sk, "p" ~ i ~ "_", pl, false);
                i += 1;
            }
            skSolve(sk);
        }
        else
        {
            try
            {
                var B = bld(id + "env");
                bodyOuter(context, B, P, g, true);
                stg(context, B);
                setProperty(context, { "entities" : posQ(B), "propertyType" : PropertyType.NAME, "value" : "REF_BODY_OUTER_ENVELOPE" });
                setProperty(context, { "entities" : posQ(B), "propertyType" : PropertyType.APPEARANCE, "value" : color(0.4, 0.45, 0.4, 0.25) });
                setAttribute(context, { "entities" : posQ(B), "attribute" : { "arvBody" : ln, "P" : P, "reference" : true } });
            }
            catch (e)
            {
                recordError(context, id, "BK04 envelope", e);
            }
            const sk = newSketchOnPlane(context, skId, { "sketchPlane" : plane(v(0, 0, 0), Z, X) });
            skp(sk, "o", [0, 0]);
            skSolve(sk);
        }
        setAttribute(context, { "entities" : qCreatedBy(skId, EntityType.BODY), "attribute" : { "arvBody" : ln, "P" : P } });
    });

// =====================================================================================================
//  62_BODY_MAIN: front + rear halves (split under the centre hatch), splices, chassis tabs, fender bosses
// =====================================================================================================
function mainBodyFeatures(context is Context, B is map, P is map, g is map)
{
    const t = bodyT();
    const zS = hatchSplitZ(P, g);
    const k = pack(P, g);
    trace(context, "main cuts");
    // service openings
    wb(context, B, g.hatchX0 - 0.3, g.hatchX1 + 0.3, -200, 200, zS, 300, false);
    wb(context, B, g.xTail - 5, g.xTail + t + 1, -44.3, 44.3, g.ZTOP + 2.7, g.zRoof - 13.7, false);
    stg(context, B);
    trace(context, "main adds");
    // hatch end ledges (under the roof line, the hatch roof edge rests on them)
    for (var e in [[g.hatchX0 - 6, g.hatchX0 + 5], [g.hatchX1 - 5, g.hatchX1 + 6]])
        for (var s in [1, -1])
            wb(context, B, e[0], e[1], s * 73, s * (g.yr - 6), g.zRoof - t - 3, g.zRoof - t, true);
    // hatch side tabs (locate + carry the horizontal hatch screws)
    for (var s in [1, -1])
        for (var h in hatchScrews(P, g))
        {
            wb(context, B, h[0] - 7, h[0] + 7, s * (g.ys - t - 7.5), s * (g.ys - t + 0.01), zS - 16, zS - 0.5, true);
            wb(context, B, h[0] - 7, h[0] + 7, s * (g.ys - t - 7.5), s * (g.ys - t - 0.2), zS - 1, zS + 9, true);
        }
    // chassis mount tabs (onto the tub flange inserts)
    for (var m in bodyMounts(P, g))
    {
        const s = m[1] > 0 ? 1 : -1;
        wb(context, B, m[0] - 7, m[0] + 7, s * (g.HW - 9), s * (g.ys - t + 0.01), g.ZTOP, g.ZTOP + 3.5, true);
        wb(context, B, m[0] - 1.5, m[0] + 1.5, s * (g.HW + 4), s * (g.ys - t + 0.01), g.ZTOP + 3.5, g.ZTOP + 20, true);
    }
    // front/rear splice plates (fused to the rear half, screwed through the front half)
    const xs = bodySplitX(P, g);
    for (var s in [1, -1])
        wb(context, B, xs - 10, xs + 10, s * (g.ys - t - 3), s * (g.ys - t + 0.01), g.podZ0 + 14, zS - 4, true);
    // fender bosses inside the hull side wall
    for (var xc in [g.xF, g.xR])
        for (var s in [1, -1])
            for (var f in fenderScrews(P, g))
                cax(context, B, "Y", [xc + f[0], s * (g.ys - t - 3), f[1]], 9, 6.02, true);
    // camera-pod roof bosses
    for (var p in camPodScrews(P, g))
        cyl(context, B, [p[0], p[1], roofZ(P, g, p[0]) - 12], [p[0], p[1], roofZ(P, g, p[0]) - 0.5], 4.5, true);
    // E-stop panel boss ring
    cyl(context, B, [k.ESTOP_X, k.ESTOP_Y, roofZ(P, g, k.ESTOP_X) - 10], [k.ESTOP_X, k.ESTOP_Y, roofZ(P, g, k.ESTOP_X) - 0.5], 16, true);
    stg(context, B);
    trace(context, "main holes");
    // hatch screws (M3 inserts in the tabs, along Y)
    for (var s in [1, -1])
        for (var h in hatchScrews(P, g))
            hIns(context, B, [h[0], s * (g.ys - t), h[1]], [0, -s, 0], 6);
    for (var m in bodyMounts(P, g))
        hClr(context, B, [m[0], m[1], g.ZTOP - 1], [m[0], m[1], g.ZTOP + 5]);
    for (var s in [1, -1])
        for (var z in [g.podZ0 + 24, zS - 14])
            hClr(context, B, [xs + 5, s * (g.ys + 1), z], [xs + 5, s * (g.ys - t - 0.1), z]);
    for (var xc in [g.xF, g.xR])
        for (var s in [1, -1])
            for (var f in fenderScrews(P, g))
                hIns(context, B, [xc + f[0], s * (g.ys + 0.01), f[1]], [0, -s, 0], 6);
    for (var s in [1, -1])
        hClr(context, B, [g.xTail - 1, s * 51, 104], [g.xTail + t + 1, s * 51, 104]);
    // fascia tongue screws through the nose-box walls
    for (var s in [1, -1])
        for (var z in [84, 92])
            hClr(context, B, [g.xNose - 15, s * 63, z], [g.xNose - 15, s * (62 - t - 1), z]);
    for (var p in camPodScrews(P, g))
        hIns(context, B, [p[0], p[1], roofZ(P, g, p[0])], [0, 0, -1], 6);
    // camera cable pass-through under the pod
    cyl(context, B, [P.cameraX - 30, 0, roofZ(P, g, P.cameraX - 30) - 8], [P.cameraX - 30, 0, roofZ(P, g, P.cameraX - 30) + 8], 9, false);
    // E-stop 22.3 mm panel hole
    cyl(context, B, [k.ESTOP_X, k.ESTOP_Y, roofZ(P, g, k.ESTOP_X) - 20], [k.ESTOP_X, k.ESTOP_Y, roofZ(P, g, k.ESTOP_X) + 5], 11.2, false);
    // passive ventilation: louvre intakes low on the pods (behind the front wheels)
    for (var s in [1, -1])
        for (var i = 0; i < 4; i += 1)
        {
            const x = 5 + i * 16;
            wb(context, B, x, x + 10, s * (g.ys - t - 1), s * (g.ys + 1), 64, 75, false);
        }
    stg(context, B);
    // trim anything that grew outside the outer skin: B - (space - outer)
    trace(context, "main trim");
    var O = bld(B.id + "trimO");
    bodyOuter(context, O, P, g, true);
    stg(context, O);
    var XS = bld(B.id + "trimX");
    wb(context, XS, -1000, 1000, -1000, 1000, -1000, 1000, true);
    stg(context, XS);
    opBoolean(context, B.id + "trim1", { "tools" : posQ(O), "targets" : posQ(XS), "operationType" : BooleanOperationType.SUBTRACTION });
    opBoolean(context, B.id + "trim2", { "tools" : posQ(XS), "targets" : posQ(B), "operationType" : BooleanOperationType.SUBTRACTION });
    // nose locating pins (below the hull floor, into the front shock-tower top)
    for (var y in [-30, 30])
        cyl(context, B, [(g.towerF0 + g.towerF1) / 2, y, g.zHB + 1], [(g.towerF0 + g.towerF1) / 2, y, g.towerFTop - 6], 1.95, true);
    stg(context, B);
}

export function camPodScrews(P is map, g is map) returns array
{
    const xc = P.cameraX;
    return [[xc - 36, 40], [xc - 36, -40]];
}

function buildBodyMain(context is Context, id is Id, P is map, g is map)
{
    const t = bodyT();
    link(context, "LNK_BODY_MAIN");
    var B = bld(id + "shell");
    bodyShell(context, B, P, g);
    mainBodyFeatures(context, B, P, g);
    // split front / rear
    trace(context, "split");
    const xs = bodySplitX(P, g);
    var F = bld(id + "front");
    var R = bld(id + "rear");
    const shellQ = posQ(B);
    opPattern(context, id + "copy", { "entities" : shellQ, "transforms" : [identityTransform()], "instanceNames" : ["c"] });
    const copyQ = qCreatedBy(id + "copy", EntityType.BODY);
    wb(context, F, xs + 0.15, 1000, -1000, 1000, -1000, 1000, false);
    wb(context, R, -1000, xs - 0.15, -1000, 1000, -1000, 1000, false);
    // front half keeps x > xs except the splice plates (they belong to the rear half)
    opBoolean(context, id + "fcut", { "tools" : qIds(R.neg[]), "targets" : shellQ, "operationType" : BooleanOperationType.SUBTRACTION });
    opBoolean(context, id + "rcut", { "tools" : qIds(F.neg[]), "targets" : copyQ, "operationType" : BooleanOperationType.SUBTRACTION });
    // re-attach splice plates to the rear half
    var S = bld(id + "spl");
    for (var s in [1, -1])
        wb(context, S, xs - 0.15, xs + 10, s * (g.ys - t - 3), s * (g.ys - t - 0.2), g.podZ0 + 14, hatchSplitZ(P, g) - 4, true);
    stg(context, S);
    for (var s in [1, -1])
        for (var z in [g.podZ0 + 24, hatchSplitZ(P, g) - 14])
            hIns(context, S, [xs + 5, s * (g.ys - t - 0.2), z], [0, -s, 0], 3);
    stg(context, S);
    // front half must clear the splice plates by 0.2
    var C = bld(id + "splc");
    for (var s in [1, -1])
        wb(context, C, xs, xs + 10.3, s * (g.ys - t - 3.3), s * (g.ys - t - 0.2 + 0.01), g.podZ0 + 13.7, hatchSplitZ(P, g) - 3.7, true);
    stg(context, C);
    opBoolean(context, id + "fclr", { "tools" : posQ(C), "targets" : shellQ, "operationType" : BooleanOperationType.SUBTRACTION });
    opBoolean(context, id + "radd", { "tools" : qUnion([copyQ, posQ(S)]), "operationType" : BooleanOperationType.UNION });
    props(context, shellQ, "BODY_MAIN_FRONT", "ARV-62-001", "Main shell, front half: hull nose, camera-pod seat, front pods, fender bosses, nose pins, chassis tabs (2x M3). Print ASA/PETG, nose down, tree supports under the hull floor.", ArvMat.ASA);
    props(context, qUnion([copyQ, posQ(S)]), "BODY_MAIN_REAR", "ARV-62-002", "Main shell, rear half: hull tail, battery-panel opening, E-stop panel, rear pods over the shock tops, splice plates, chassis tabs + tunnel screws. Print ASA/PETG.", ArvMat.ASA);
    // hardware: splice screws, chassis-tab screws, fender inserts, hatch inserts
    var n = 0;
    for (var s in [1, -1])
    {
        for (var z in [g.podZ0 + 24, hatchSplitZ(P, g) - 14])
        {
            screw(context, id + ("sp" ~ n), [xs + 5, s * g.ys, z], [0, -s, 0], 8, 3, true);
            insert(context, id + ("si" ~ n), [xs + 5, s * (g.ys - t - 0.2), z], [0, -s, 0]);
            n += 1;
        }
        for (var h in hatchScrews(P, g))
        {
            insert(context, id + ("hi" ~ n), [h[0], s * (g.ys - t), h[1]], [0, -s, 0]);
            n += 1;
        }
        screw(context, id + ("ts" ~ n), [g.xTail, s * 51, 104], [1, 0, 0], 10, 3, true);
        n += 1;
    }
    for (var m in bodyMounts(P, g))
    {
        screw(context, id + ("cm" ~ n), [m[0], m[1], g.ZTOP + 3.5], [0, 0, -1], 8, 3, false);
        n += 1;
    }
    for (var xc in [g.xF, g.xR])
        for (var s in [1, -1])
            for (var f in fenderScrews(P, g))
            {
                insert(context, id + ("fi" ~ n), [xc + f[0], s * g.ys, f[1]], [0, -s, 0]);
                n += 1;
            }
    for (var p in camPodScrews(P, g))
    {
        insert(context, id + ("ci" ~ n), [p[0], p[1], roofZ(P, g, p[0])], [0, 0, -1]);
        n += 1;
    }
    // E-stop (purchased, safety: stays exposed on the rear deck)
    const k = pack(P, g);
    var E = bld(id + "estop");
    const zr = roofZ(P, g, k.ESTOP_X);
    cyl(context, E, [k.ESTOP_X, k.ESTOP_Y, zr + 2], [k.ESTOP_X, k.ESTOP_Y, zr + 20], 20, true);
    cyl(context, E, [k.ESTOP_X, k.ESTOP_Y, zr - 8], [k.ESTOP_X, k.ESTOP_Y, zr + 2], 11, true);
    wb(context, E, k.ESTOP_X - 15, k.ESTOP_X + 15, k.ESTOP_Y - 20, k.ESTOP_Y + 20, zr - 38, zr - 8, true);
    stg(context, E);
    fin(context, E, "ESTOP_22MM_LATCHING", "PUR-SAF-ESTOP22", "22 mm latching mushroom E-stop, NC, 10 A (breaks the motor branch); red head", ArvMat.PETG);
    setProperty(context, { "entities" : posQ(E), "propertyType" : PropertyType.APPEARANCE, "value" : color(0.8, 0.08, 0.06) });
}

// =====================================================================================================
//  63_CENTER_SERVICE_PANEL
// =====================================================================================================
function buildCenterPanel(context is Context, id is Id, P is map, g is map)
{
    const t = bodyT();
    const zS = hatchSplitZ(P, g);
    link(context, "LNK_CENTER_PANEL");
    var B = bld(id + "hatch");
    bodyShell(context, B, P, g);
    trace(context, "hatch keep");
    var K = bld(B.id + "keep");
    wb(context, K, g.hatchX0 + 0.3, g.hatchX1 - 0.3, -200, 200, zS, 300, true);
    stg(context, K);
    opBoolean(context, B.id + "int", { "tools" : qUnion([posQ(B), posQ(K)]), "operationType" : BooleanOperationType.INTERSECTION });
    var lst = B.pos;
    lst[] = concatenateArrays([lst[], K.pos[], [B.id + "int"]]);
    trace(context, "hatch features n=" ~ size(evaluateQuery(context, posQ(B))));
    // locating lips on the long sides (drop inside the main walls, beside the tabs)
    for (var s in [1, -1])
        for (var x in [g.hatchX0 + 45, g.podX1 - 45])
        {
            {
            trace(context, "lip " ~ s ~ " " ~ x);
            wb(context, B, x - 12, x + 12, s * (g.ys - t - 1.8), s * (g.ys - t - 0.2), zS - 6, zS + 0.01, true);
            wb(context, B, x - 12, x + 12, s * (g.ys - t - 1.8), s * (g.ys - t + 0.6), zS, zS + 1.5, true);
        }
        }
    trace(context, "hatch rib");
    // stiffening rib under the roof (also a finger grip for lifting)
    wb(context, B, g.hatchX0 + 8, g.hatchX1 - 8, -1.5, 1.5, g.zRoof - t - 6, g.zRoof - t + 0.01, true);
    trace(context, "hatch stg1");
    stg(context, B);
    trace(context, "hatch screws");
    // side screws (counterbored) into the main-body tabs
    for (var s in [1, -1])
        for (var h in hatchScrews(P, g))
        {
            hClr(context, B, [h[0], s * (g.ys + 1), h[1]], [h[0], s * (g.ys - t - 1), h[1]]);
            cyl(context, B, [h[0], s * (g.ys + 1), h[1]], [h[0], s * (g.ys - 0.8), h[1]], 3.2, false);
        }
    // Jetson exhaust: slotted vents on both roof chamfers above the compute deck
    trace(context, "hatch vents");
    const k = pack(P, g);
    for (var s in [1, -1])
        for (var i = 0; i < 6; i += 1)
        {
            const x = k.JETSON[0] + 10 + i * 14;
            const ym = (g.ys + g.yr) / 2;
            const zm = (g.zSh + g.zRoof) / 2;
            const cs = coordSystem(v(x, s * ym, zm), vector(1, 0, 0), normalize(vector(0, s * (g.zRoof - g.zSh), g.ys - g.yr)));
            bx(context, B, cs, -3, 3, -7, 7, -10, 10, false);
        }
    trace(context, "hatch topvents");
    // top vent strip toward the rear (warm air from the power level)
    for (var i = 0; i < 5; i += 1)
    {
        const y = -40 + i * 20;
        wb(context, B, g.hatchX0 + 14, g.hatchX0 + 44, y - 2.5, y + 2.5, g.zRoof - t - 5, g.zRoof + 1, false);
    }
    trace(context, "hatch fin");
    fin(context, B, "CENTER_SERVICE_PANEL", "ARV-63-001", "Removable centre electronics hatch (roof + chamfers + 10 mm sides), locating lips, 4x M3 button heads, Jetson exhaust vents. Opening passes the whole electronics tray. ASA/PETG.", ArvMat.ASA);
    var n = 0;
    for (var s in [1, -1])
        for (var h in hatchScrews(P, g))
        {
            screw(context, id + ("s" ~ n), [h[0], s * (g.ys - 0.8), h[1]], [0, -s, 0], 10, 3, true);
            n += 1;
        }
}

// =====================================================================================================
//  64_REAR_BATTERY_PANEL
// =====================================================================================================
function buildBatteryPanel(context is Context, id is Id, P is map, g is map)
{
    const t = bodyT();
    link(context, "LNK_BATTERY_PANEL");
    var B = bld(id + "panel");
    bodyShell(context, B, P, g);
    var K = bld(B.id + "keep");
    wb(context, K, g.xTail - 5, g.xTail + t + 1, -44, 44, g.ZTOP + 3, g.zRoof - 14, true);
    stg(context, K);
    opBoolean(context, B.id + "int", { "tools" : qUnion([posQ(B), posQ(K)]), "operationType" : BooleanOperationType.INTERSECTION });
    var lst = B.pos;
    lst[] = concatenateArrays([lst[], K.pos[], [B.id + "int"]]);
    trace(context, "panel features");
    const zc = (g.ZTOP + 8 + g.zRoof - 14) / 2;
    // locating tabs (bear on the tunnel-bay side walls), push-stop boss (holds the battery forward), grip bar
    for (var s in [1, -1])
        wb(context, B, g.xTail + t - 0.1, g.xTail + t + 4, s * 33, s * 35.2, zc - 10, zc + 10, true);
    wb(context, B, g.xTail + t - 0.1, g.batX0 - 1.5, 14, 24, zc - 8, zc + 8, true);
    wb(context, B, g.xTail - 8, g.xTail + 0.01, -22, 22, zc + 10, zc + 16, true);
    stg(context, B);
    for (var s in [1, -1])
    {
        hClr(context, B, [g.xTail - 5, s * 41.5, (g.tunZ0 + g.tunZ1) / 2], [g.xTail + t + 1, s * 41.5, (g.tunZ0 + g.tunZ1) / 2]);
    }
    fin(context, B, "REAR_BATTERY_PANEL", "ARV-64-001", "Removable rear battery-access panel: locating lips, battery push-stop, grip bar, 2x M3 thumb screws into tunnel-mouth inserts. ASA/PETG.", ArvMat.ASA);
    for (var s in [1, -1])
        screw(context, id + ("s" ~ s), [g.xTail, s * 41.5, (g.tunZ0 + g.tunZ1) / 2], [1, 0, 0], 12, 3, true);
}

// =====================================================================================================
//  61_FRONT_FASCIA + BUMPERS (split-level tactical nose) and front/rear ToF sensors
// =====================================================================================================
export function tofPoses(P is map, g is map) returns array
{
    const k = pack(P, g);
    const xf = k.TOF_X;
    const sp = P.frontSensorSplay;
    // [position, outward normal] for the 3 front sensors (centre + splayed corners)
    const a = 32 * tan(sp * degree) / 2;
    return [[[xf, 0, k.TOF_Z], vector(1, 0, 0)],
            [[xf - a, 46, k.TOF_Z], vector(cos(sp * degree), sin(sp * degree), 0)],
            [[xf - a, -46, k.TOF_Z], vector(cos(sp * degree), -sin(sp * degree), 0)]];
}

function tofEnvelope(context is Context, id is Id, p is array, n is Vector, name is string)
{
    var B = bld(id);
    const cs = coordSystem(v(p[0], p[1], p[2]), normalize(cross(Z, n)), n);
    bx(context, B, cs, -6.5, 6.5, -9, 9, -4.6, -3.0, true);
    bx(context, B, cs, -2.5, 2.5, -2, 2, -3.0, -1.6, true);
    fin(context, B, name, "PUR-SNS-VL53L1X", "VL53L1X ToF breakout 13 x 18, sensor window flush behind a 10 x 6 aperture", ArvMat.PCB);
}

function buildFascia(context is Context, id is Id, P is map, g is map)
{
    const t = bodyT();
    const xf = g.xNose + 19;
    // ---- upper sensor fascia
    try
    {
        link(context, "LNK_FASCIA");
        var B = bld(id + "fascia");
        trace(context, "fascia");
        const fr = [[-62, 76], [62, 76], [62, g.zHB], [-62, g.zHB]];
        tri(context, B, fasciaSide(P, g), fasciaPlan(P, g), fr, true);
        stg(context, B);
        tri(context, B, offsetPoly(fasciaSide(P, g), t), offsetPoly(fasciaPlan(P, g), t), offsetPoly(fr, t), false);
        // open back (faces the nose box)
        wb(context, B, g.xNose - 12, g.xNose - 9 + t + 0.5, -62 + t, 62 - t, 76 + t, g.zHB - t, false);
        // floor opening over the inverted servo (tabs + case rise into the fascia)
        wb(context, B, g.xNose - 12, g.servoX + 12, -39, 19, 74, 81, false);
        stg(context, B);
        // side tongues into the nose box (screwed from the wheel wells)
        for (var s in [1, -1])
        {
            wb(context, B, g.xNose - 22, g.xNose - 8, s * (62 - t - 3.2), s * (62 - t - 0.05), 80, 96, true);
            wb(context, B, g.xNose - 9 + 0.02, g.xNose - 7, s * (62 - t - 3.2), s * (62 - 0.01), 80, 96, true);
        }
        // sensor bosses behind the apertures
        for (var tp in tofPoses(P, g))
        {
            const cs = coordSystem(v(tp[0][0], tp[0][1], tp[0][2]), normalize(cross(Z, tp[1])), tp[1]);
            bx(context, B, cs, -8, 8, -9, 9, -5, -0.5, true);
        }
        stg(context, B);
        for (var tp in tofPoses(P, g))
        {
            const cs = coordSystem(v(tp[0][0], tp[0][1], tp[0][2]), normalize(cross(Z, tp[1])), tp[1]);
            bx(context, B, cs, -5, 5, -3, 3, -3.1, 1, false);          // sensing aperture 10 x 6
            bx(context, B, cs, -7, 7, -9.5, 9.5, -5.5, -3.0, false);   // board pocket (rear installation)
            for (var dx in [-8.5, 8.5])
                cz(context, B, cs, dx, 0, 0.9, -5.5, -3.5, false);       // M2 retainer screws
        }
        for (var s in [1, -1])
            for (var z in [84, 92])
                hIns(context, B, [g.xNose - 15, s * (62 - t), z], [0, -s, 0], 3);
        // subtle intake slots on the upper chamfer
        for (var i = 0; i < 5; i += 1)
        {
            const y = -36 + i * 18;
            wb(context, B, xf - 9, xf + 1, y - 3, y + 3, g.zHB - 11, g.zHB - 7, false);
        }
        fin(context, B, "FRONT_SENSOR_FASCIA", "ARV-61-001", "Upper sensor fascia: 3 recessed VL53L1X apertures (centre + 25 deg corners), rear-loaded board pockets, side tongues (4x M3 from the wheel wells). ASA/PETG, printed face-down.", ArvMat.PETGCF);
        var n = 0;
        for (var s in [1, -1])
            for (var z in [84, 92])
            {
                screw(context, id + ("fs" ~ n), [g.xNose - 15, s * 62, z], [0, -s, 0], 6, 3, true);
                n += 1;
            }
        var i = 0;
        for (var tp in tofPoses(P, g))
        {
            tofEnvelope(context, id + ("tof" ~ i), tp[0], tp[1], "TOF_FRONT_" ~ (i == 0 ? "C" : (i == 1 ? "L" : "R")));
            i += 1;
        }
    }
    catch (e)
    {
        recordError(context, id, "FRONT_SENSOR_FASCIA", e);
    }
    // ---- structural lower front bumper
    try
    {
        link(context, "LNK_FRONT_BUMPER");
        var Bm = bld(id + "bumper");
        trace(context, "front bumper");
        const x0 = g.servoX + 11;
        const fr = [[-100, 22], [100, 22], [100, 76], [-100, 76]];
        tri(context, Bm, bumperSide(P, g), bumperPlan(P, g), fr, true);
        stg(context, Bm);
        // honeycomb-style crush cells open to the rear (3 mm walls)
        for (var y in [-78, -52, -26, 0, 26, 52, 78])
            wb(context, Bm, x0 - 1, x0 + 17, y - 10, y + 10, 26, 72, false);
        stg(context, Bm);
        // mounting: 2x M3 into the bulkhead servo-pillar inserts, tool access through the face
        for (var yc in [-34.85, 14.65])
        {
            wb(context, Bm, x0, x0 + 8, yc - 6, yc + 6, 34, 46, true);
        }
        stg(context, Bm);
        for (var yc in [-34.85, 14.65])
        {
            hClr(context, Bm, [x0 - 1, yc, 40], [x0 + 9, yc, 40]);
            cyl(context, Bm, [x0 + 4, yc, 40], [x0 + 30, yc, 40], 3.5, false);
        }
        fin(context, Bm, "FRONT_BUMPER", "ARV-61-002", "Structural lower bumper, TPU 95A crush cells, 2x M3x16 + 3x12 washers into the bulkhead servo-pillar inserts; protects the steering and fascia.", ArvMat.TPU);
        var n = 0;
        for (var yc in [-34.85, 14.65])
        {
            screw(context, id + ("bs" ~ n), [x0 + 4, yc, 40], [-1, 0, 0], 16, 3, false);
            n += 1;
        }
    }
    catch (e)
    {
        recordError(context, id, "FRONT_BUMPER", e);
    }
    // ---- rear bumper with rear ToF
    try
    {
        link(context, "LNK_REAR_BUMPER");
        var Rb = bld(id + "rbumper");
        trace(context, "rear bumper");
        const xe = g.xTail - 8;
        const side = [[xe, 44], [xe, g.ZTOP + 2], [xe - 14, g.ZTOP + 2], [xe - 20, g.ZTOP - 6], [xe - 20, 52], [xe - 12, 44]];
        const plan = [[xe, -86], [xe, 86], [xe - 10, 86], [xe - 20, 60], [xe - 20, -60], [xe - 10, -86]];
        tri(context, Rb, side, plan, [[-86, 44], [86, 44], [86, g.ZTOP + 2], [-86, g.ZTOP + 2]], true);
        stg(context, Rb);
        for (var y in [-62, -18, 18, 62])
            wb(context, Rb, xe - 12, xe + 1, y - 8, y + 8, 54, g.ZTOP - 8, false);
        stg(context, Rb);
        for (var y in [-40, 40])
        {
            wb(context, Rb, xe - 8, xe + 0.01, y - 6, y + 6, g.ZTOP - 12, g.ZTOP, true);
        }
        stg(context, Rb);
        for (var y in [-40, 40])
        {
            hClr(context, Rb, [xe - 9, y, g.ZTOP - 6], [xe + 1, y, g.ZTOP - 6]);
            cyl(context, Rb, [xe - 4, y, g.ZTOP - 6], [xe - 30, y, g.ZTOP - 6], 3.5, false);
        }
        // rear ToF aperture + pocket
        const rp = [xe - 20, 0, P.rearSensorZ];
        const cs = coordSystem(v(rp[0], rp[1], rp[2]), Y, vector(-1, 0, 0));
        bx(context, Rb, cs, -5, 5, -3, 3, -3.1, 1, false);
        bx(context, Rb, cs, -7, 7, -9.5, 9.5, -12, -3.0, false);
        fin(context, Rb, "REAR_BUMPER", "ARV-61-003", "Rear bumper, TPU 95A, 2x M3x16 into the rear-frame cross bar inserts; recessed rear VL53L1X.", ArvMat.TPU);
        var n = 0;
        for (var y in [-40, 40])
        {
            screw(context, id + ("rs" ~ n), [xe - 4, y, g.ZTOP - 6], [1, 0, 0], 16, 3, false);
            n += 1;
        }
        tofEnvelope(context, id + "tofr", rp, vector(-1, 0, 0), "TOF_REAR");
    }
    catch (e)
    {
        recordError(context, id, "REAR_BUMPER", e);
    }
}

// =====================================================================================================
//  65_CAMERA_POD  (low-profile upper-front pod: D435i + IMX219 160 deg)
// =====================================================================================================
function buildCameraPod(context is Context, id is Id, P is map, g is map)
{
    const t = 2;
    const xc = P.cameraX;
    const zc = P.cameraHeight;
    const k = pack(P, g);
    try
    {
        link(context, "LNK_CAMERA_POD");
        var B = bld(id + "pod");
        trace(context, "pod outer");
        const fr = [[-66, zc - 40], [80, zc - 40], [80, zc + 12], [72, zc + 17], [-58, zc + 17], [-66, zc + 12]];
        var side = podCamSide(P, g);
        side[0] = [xc - 44, roofZ(P, g, xc - 44) - 3];
        side[1] = [xc + 16, roofZ(P, g, xc + 16) - 3];
        tri(context, B, side, podCamPlan(P, g), fr, true);
        stg(context, B);
        trace(context, "pod inner");
        tri(context, B, offsetPoly(side, t), offsetPoly(podCamPlan(P, g), t), offsetPoly(fr, t), false);
        stg(context, B);
        // conform the base to the roof: remove everything inside the body outer skin
        var O = bld(B.id + "hull");
        bodyOuter(context, O, P, g, true);
        stg(context, O);
        opBoolean(context, B.id + "seat", { "tools" : posQ(O), "targets" : posQ(B), "operationType" : BooleanOperationType.SUBTRACTION, "keepTools" : true });
        trace(context, "pod mounts");
        // D435i bracket (2x M3 on the camera rear face, 45 mm apart) and screw columns
        wb(context, B, xc - 12.5 - 3, xc - 12.5, -40, 40, roofZ(P, g, xc - 12.5) - 1, zc + 15, true);
        for (var p in camPodScrews(P, g))
            cyl(context, B, [p[0], p[1], roofZ(P, g, p[0]) + 0.2], [p[0], p[1], zc + 15.5], 4, true);
        // IMX219 board plate (M2 on 21 x 12.5)
        wb(context, B, xc + 8, xc + 10, k.CAM_Y - 14, k.CAM_Y + 14, zc - 14, zc + 14, true);
        stg(context, B);
        opBoolean(context, B.id + "seat2", { "tools" : posQ(O), "targets" : posQ(B), "operationType" : BooleanOperationType.SUBTRACTION });
        trace(context, "pod windows");
        // D435i window (full face incl. emitter + both imagers), IMX219 lens port
        wb(context, B, xc + 10, xc + 20, -46, 46, zc - 13.5, zc + 13.5, false);
        cax(context, B, "X", [xc + 13, k.CAM_Y, zc], 16, 12, false);
        for (var y in [-22.5, 22.5])
            hClr(context, B, [xc - 17, y, zc], [xc - 11, y, zc]);
        for (var dy in [-10.5, 10.5])
            for (var dz in [-6.25, 6.25])
                cyl(context, B, [xc + 7, k.CAM_Y + dy, zc + dz], [xc + 11, k.CAM_Y + dy, zc + dz], 0.9, false);
        for (var p in camPodScrews(P, g))
        {
            hClr(context, B, [p[0], p[1], roofZ(P, g, p[0]) - 1], [p[0], p[1], zc + 18]);
            cyl(context, B, [p[0], p[1], zc + 12], [p[0], p[1], zc + 18], 3.1, false);
        }
        // cable exit through the base (onto the roof pass-through)
        cyl(context, B, [xc - 30, 0, roofZ(P, g, xc - 30) - 3], [xc - 30, 0, roofZ(P, g, xc - 30) + 12], 9, false);
        fin(context, B, "CAMERA_POD", "ARV-65-001", "Low-profile camera pod: D435i on a 2x M3 rear-face bracket, IMX219 160 deg lens port, roof-conforming base, 2x M3 from the top into roof inserts, internal cable exit. PETG-CF.", ArvMat.PETGCF);
        var n = 0;
        for (var p in camPodScrews(P, g))
        {
            screw(context, id + ("s" ~ n), [p[0], p[1], zc + 12], [0, 0, -1], 30, 3, false);
            n += 1;
        }
        for (var y in [-22.5, 22.5])
        {
            screw(context, id + ("c" ~ n), [xc - 15.5, y, zc], [1, 0, 0], 6, 3, false);
            n += 1;
        }
    }
    catch (e)
    {
        recordError(context, id, "CAMERA_POD", e);
    }
    try
    {
        // camera bezel (transparent-look window frame): clips into the pod face
        var W = bld(id + "win");
        wb(context, W, xc + 14.6, xc + 16.2, -45.5, 45.5, zc - 13, zc + 13, true);
        stg(context, W);
        fin(context, W, "CAMERA_WINDOW", "ARV-65-002", "Protective window, 1.5 mm clear polycarbonate (laser-cut) or clear PETG, bonded into the pod face.", ArvMat.GLASS);
        var D = bld(id + "d435");
        wb(context, D, xc - 12.5, xc + 12.5, -45, 45, zc - 12.5, zc + 12.5, true);
        stg(context, D);
        soft(context, D, Y, 5);
        fin(context, D, "DEPTH_CAMERA_D435I", "PUR-SNS-D435I", "Intel RealSense D435i (90 x 25 x 25), 2x M3 on the rear face", ArvMat.ALU);
        var C = bld(id + "imx");
        wb(context, C, xc + 1, xc + 13, k.CAM_Y - 12.5, k.CAM_Y + 12.5, zc - 12, zc + 12, true);
        stg(context, C);
        fin(context, C, "CAMERA_IMX219_160", "PUR-SNS-IMX219-160", "IMX219 160 deg CSI camera (25 x 24 board, M2 on 21 x 12.5)", ArvMat.PCB);
    }
    catch (e)
    {
        recordError(context, id, "camera envelopes", e);
    }
}

// =====================================================================================================
//  66_FENDERS  (partial tactical eyebrows over each wheel)
// =====================================================================================================
function fenderPlan(P is map, g is map, xc is number) returns array
{
    const L = P.wheelDiameter / 2 + 34;
    const yo = P.fenderWidth;
    return [[xc - L, g.ys], [xc + L, g.ys], [xc + L, yo - 14], [xc + L - 14, yo], [xc - L + 14, yo], [xc - L, yo - 14]];
}

function fenderFront2(P is map, g is map) returns array
{
    const yo = P.fenderWidth;
    return [[g.ys, g.zHB - 14], [yo, g.zHB - 14], [yo, g.zHB + 2], [yo - 12, g.zHB + 12], [g.ys, g.zHB + 18]];
}

function pFender(context is Context, id is Id, P is map, g is map, xc is number, s is number, tag is string, pn is string)
{
    const t = 2.5;
    var B = bld(id);
    trace(context, "fender " ~ tag);
    tri(context, B, fenderSide(P, g, xc), fenderPlan(P, g, xc), fenderFront2(P, g), true);
    stg(context, B);
    var sideIn = offsetPoly(fenderSide(P, g, xc), t);
    var frIn = offsetPoly(fenderFront2(P, g), t);
    tri(context, B, sideIn, offsetPoly(fenderPlan(P, g, xc), t), frIn, false);
    // open underside
    const L = P.wheelDiameter / 2 + 34;
    wb(context, B, xc - L + 8, xc + L - 8, g.ys + t, P.fenderWidth - t, g.zHB - 20, g.zHB - 14 + t + 0.5, false);
    stg(context, B);
    // inner mounting flange against the hull side (screws from the wheel well)
    wb(context, B, xc - 50, xc + 32, g.ys, g.ys + 3, g.zHB - 2, g.zHB + 16, true);
    // ribs
    for (var dx in [-25, 25])
        wb(context, B, xc + dx - 1, xc + dx + 1, g.ys + 2, P.fenderWidth - 6, g.zHB + 6, g.zHB + 14, true);
    stg(context, B);
    for (var f in fenderScrews(P, g))
    {
        hClr(context, B, [xc + f[0], g.ys - 1, f[1]], [xc + f[0], g.ys + 4, f[1]]);
    }
    // tactical detail: stepped edge light-bar recess on the outer face (functional reflector / LED strip)
    wb(context, B, xc - 30, xc + 30, P.fenderWidth - 1.2, P.fenderWidth + 1, g.zHB - 6, g.zHB - 2, false);
    stg(context, B);
    if (s < 0)
        opTransform(context, id + "mir", { "bodies" : posQ(B), "transform" : mirrorAcross(plane(v(0, 0, 0), Y)) });
    fin(context, B, "FENDER_" ~ tag, pn, "Partial tactical fender (angular eyebrow), frames the tyre top, clears full lock + full bump; 2x M3 into hull-side inserts from the wheel well. PETG-CF.", ArvMat.PETGCF);
    var n = 0;
    for (var f in fenderScrews(P, g))
    {
        screw(context, id + ("s" ~ n), [xc + f[0], s * (g.ys + 3), f[1]], [0, -s, 0], 8, 3, true);
        n += 1;
    }
}

function buildFenders(context is Context, id is Id, P is map, g is map)
{
    const list = [["fl", g.xF, 1, "FL", "ARV-66-001"], ["fr", g.xF, -1, "FR", "ARV-66-002"], ["rl", g.xR, 1, "RL", "ARV-66-003"], ["rr", g.xR, -1, "RR", "ARV-66-004"]];
    for (var f in list)
        try
        {
            link(context, "LNK_FENDER_" ~ f[3]);
            pFender(context, id + f[0], P, g, f[1], f[2], f[3], f[4]);
        }
        catch (e)
        {
            recordError(context, id, "FENDER_" ~ f[3], e);
        }
}


// =====================================================================================================
//  ARV Studio Builder  --  one feature per subsystem Part Studio, placed AFTER that studio's Derived
//  feature. It reads the master parameter map from the derived master geometry (no re-typed numbers).
// =====================================================================================================
export enum ArvStudio
{
    annotation { "Name" : "10 Chassis" }
    CHASSIS,
    annotation { "Name" : "20 Front steering" }
    FRONT_STEERING,
    annotation { "Name" : "30 Drivetrain" }
    DRIVETRAIN,
    annotation { "Name" : "40 Battery system" }
    BATTERY,
    annotation { "Name" : "50 Electronics tray" }
    ELECTRONICS,
    annotation { "Name" : "61 Front fascia + bumpers" }
    FASCIA,
    annotation { "Name" : "62 Body main" }
    BODY_MAIN,
    annotation { "Name" : "63 Center service panel" }
    CENTER_PANEL,
    annotation { "Name" : "64 Rear battery panel" }
    BATTERY_PANEL,
    annotation { "Name" : "65 Camera pod" }
    CAMERA_POD,
    annotation { "Name" : "66 Fenders" }
    FENDERS
}

function isBody(s is ArvStudio) returns boolean
{
    return s == ArvStudio.FASCIA || s == ArvStudio.BODY_MAIN || s == ArvStudio.CENTER_PANEL || s == ArvStudio.BATTERY_PANEL || s == ArvStudio.CAMERA_POD || s == ArvStudio.FENDERS;
}

export function buildStudio(context is Context, id is Id, s is ArvStudio, P is map, g is map, steer is number)
{
    if (s == ArvStudio.CHASSIS)
        buildChassis(context, id, P, g);
    else if (s == ArvStudio.FRONT_STEERING)
        buildFront(context, id, P, g, steer);
    else if (s == ArvStudio.DRIVETRAIN)
        buildDrive(context, id, P, g);
    else if (s == ArvStudio.BATTERY)
        buildBattery(context, id, P, g);
    else if (s == ArvStudio.ELECTRONICS)
        buildElectronics(context, id, P, g);
    else if (s == ArvStudio.BODY_MAIN)
        buildBodyMain(context, id, P, g);
    else if (s == ArvStudio.CENTER_PANEL)
        buildCenterPanel(context, id, P, g);
    else if (s == ArvStudio.BATTERY_PANEL)
        buildBatteryPanel(context, id, P, g);
    else if (s == ArvStudio.FASCIA)
        buildFascia(context, id, P, g);
    else if (s == ArvStudio.CAMERA_POD)
        buildCameraPod(context, id, P, g);
    else if (s == ArvStudio.FENDERS)
        buildFenders(context, id, P, g);
    else
        throw regenError("ARV: studio " ~ toString(s) ~ " not implemented yet");
}

annotation { "Feature Type Name" : "ARV Studio Builder", "Feature Name Template" : "#studio", "Feature Type Description" : "Builds every manufactured and purchased part of one ARV-1 subsystem from the derived 00_CAR_MASTER geometry." }
export const arvStudio = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation { "Name" : "Studio" }
        definition.studio is ArvStudio;
        annotation { "Name" : "Purchased hardware (screws, inserts)", "Default" : true }
        definition.hardware is boolean;
        annotation { "Name" : "Steering pose (crank deg, + = left turn)" }
        isAngle(definition.steer, { (degree) : [-40, 0, 40] } as AngleBoundSpec);
    }
    {
        const P = derivedP(context, isBody(definition.studio) ? "arvBody" : "arvMaster");
        const g = geo(P);
        setVariable(context, "arvHardware", definition.hardware);
        setVariable(context, "arvError", "");
        setFeatureComputedParameter(context, id, { "name" : "studio", "value" : toString(definition.studio) });
        try
        {
            buildStudio(context, id, definition.studio, P, g, definition.steer / degree);
        }
        catch (e)
        {
            recordError(context, id, toString(definition.studio), e);
        }
        setVariable(context, "arvStatus", toString(definition.studio) ~ " built");
        // hide the derived master reference sketches' clutter is left to the user; they are construction only
    });


// =====================================================================================================
//  KINEMATIC LINKS: every builder tags its bodies with the link they move with (variable arvLink).
//  After a studio is built (steer = 0), each link becomes an open composite part carrying
//    - an origin mate connector (world frame)  -> Fastened mates reproduce the as-designed positions
//    - joint mate connectors (identical frames on both links) -> Revolute / Ball / Slider mates
//  The assembly reads the op ids from the variable arvLinks through the FeatureScript eval API.
// =====================================================================================================
export function link(context is Context, name is string)
{
    setVariable(context, "arvLink", name);
}

function curLink(context is Context) returns string
{
    var r = "";
    try silent
    {
        r = getVariable(context, "arvLink");
    }
    return r;
}

// joints: [name, linkA, linkB, type, origin, zAxis]
export function jointTable(P is map, g is map) returns array
{
    var j = [];
    j = append(j, ["STEER_SERVO", "LNK_FRONT_FRAME", "LNK_SERVO_HORN", "REVOLUTE", [g.servoX, 0, g.servoZ], Z]);
    const hp = hornPin(P, g, 0);
    const pl = crankDragPin(P, g, 1, 0);
    const pq = crankDragPin(P, g, -1, 0);
    const mid = [(pl[0] + pq[0]) / 2, (pl[1] + pq[1]) / 2];
    j = append(j, ["HORN_LINK", "LNK_SERVO_HORN", "LNK_SERVO_LINK", "REVOLUTE", [hp[0], hp[1], g.slinkZ0], Z]);
    j = append(j, ["LINK_DRAG", "LNK_DRAG_LINK", "LNK_SERVO_LINK", "REVOLUTE", [mid[0], mid[1], g.dragZ1], Z]);
    for (var s in [1, -1])
    {
        const t = s > 0 ? "L" : "R";
        const dp = crankDragPin(P, g, s, 0);
        j = append(j, ["CRANK_" ~ t, "LNK_FRONT_FRAME", "LNK_CRANK_" ~ t, "REVOLUTE", [g.crankX, s * g.crankY, g.crankZ], Z]);
        j = append(j, ["DRAG_" ~ t, "LNK_CRANK_" ~ t, "LNK_DRAG_LINK", "REVOLUTE", [dp[0], dp[1], g.dragZ0], Z]);
        const kl = kingpinLine(P, g, s);
        j = append(j, ["KINGPIN_" ~ t, "LNK_FRONT_FRAME", "LNK_KNUCKLE_" ~ t, "REVOLUTE", arr(kl.origin), kl.direction]);
        j = append(j, ["WHEEL_F" ~ t, "LNK_KNUCKLE_" ~ t, "LNK_WHEEL_F" ~ t, "REVOLUTE", [g.xF, s * g.yW, g.zA], Y]);
        j = append(j, ["TIE_OUT_" ~ t, "LNK_KNUCKLE_" ~ t, "LNK_TIE_ROD_" ~ t, "BALL", armTip0(P, g, s), Z]);
        j = append(j, ["TIE_IN_" ~ t, "LNK_CRANK_" ~ t, "LNK_TIE_ROD_" ~ t, "BALL", crankBall(P, g, s, 0), Z]);
        const ub = arr(casterT(P, g) * v(g.xF, s * g.KY, g.kUpZ));
        j = append(j, ["CAMBER_OUT_" ~ t, "LNK_KNUCKLE_" ~ t, "LNK_CAMBER_LINK_" ~ t, "BALL", ub, Z]);
        j = append(j, ["CAMBER_IN_" ~ t, "LNK_FRONT_FRAME", "LNK_CAMBER_LINK_" ~ t, "BALL", [g.towerF1 + 7.5, s * g.camberInY, g.kUpZ], Z]);
        j = append(j, ["WHEEL_R" ~ t, "LNK_REAR_ARM_" ~ t, "LNK_WHEEL_R" ~ t, "REVOLUTE", [g.xR, s * g.yW, g.zA], Y]);
    }
    const k = pack(P, g);
    j = append(j, ["BATTERY_SLIDE", "LNK_BATTERY_TUNNEL", "LNK_BATTERY", "SLIDER", [(k.BATTERY[0] + k.BATTERY[1]) / 2, 0, (k.BATTERY[4] + k.BATTERY[5]) / 2], X]);
    return j;
}

function frameX(zd is Vector) returns Vector
{
    const z = normalize(zd);
    return abs(dot(z, Y)) > 0.9 ? X : normalize(cross(Y, z));
}

export function makeLinks(context is Context, id is Id, P is map, g is map)
{
    // group solid bodies by their link attribute
    var groups = {};
    var hwList = [];
    var order = [];
    for (var b in evaluateQuery(context, qBodyType(qEverything(EntityType.BODY), BodyType.SOLID)))
    {
        var ln = undefined;
        var hw = false;
        for (var a in getAttributes(context, { "entities" : b }))
            if (a is map && a.link != undefined && a.link != "")
            {
                ln = a.link;
                hw = a.hw == true;
            }
        if (ln == undefined)
            continue;
        if (groups[ln] == undefined)
        {
            groups[ln] = [];
            order = append(order, ln);
        }
        groups[ln] = append(groups[ln], b);
        if (hw)
            hwList = append(hwList, b);
    }
    trace(context, "links grouped " ~ size(order));
    var info = {};
    var owners = {};
    var i = 0;
    for (var ln in order)
    {
        // primary part = largest non-hardware body of the link
        var best = undefined;
        var bestV = -1;
        for (var b in groups[ln])
        {
            var vol = evVolume(context, { "entities" : b }) / (millimeter ^ 3);
            if (size(evaluateQuery(context, qIntersection([b, qUnion(hwList)]))) > 0)
                vol = vol / 1000000;
            if (vol > bestV)
            {
                bestV = vol;
                best = b;
            }
        }
        trace(context, "links owner " ~ ln);
        owners[ln] = best;
        trace(context, "links mc " ~ ln);
        const mo = "mo" ~ i;
        opMateConnector(context, id + mo, { "coordSystem" : coordSystem(v(0, 0, 0), X, Z), "owner" : best });
        trace(context, "links info " ~ ln);
        info[ln] = { "mo" : mo, "n" : size(groups[ln]) };
        trace(context, "links attr " ~ ln);
        setAttribute(context, { "entities" : best, "attribute" : { "arvPrimary" : ln } });
        i += 1;
    }
    trace(context, "links joints");
    var joints = [];
    var jn = 0;
    for (var j in jointTable(P, g))
    {
        if (info[j[1]] != undefined || info[j[2]] != undefined)
        {
            trace(context, "joint " ~ j[0]);
            var rec = { "name" : j[0], "a" : j[1], "b" : j[2], "type" : j[3] };
            const cs = coordSystem(v(j[4][0], j[4][1], j[4][2]), frameX(j[5]), normalize(j[5]));
            for (var side in ["a", "b"])
            {
                const ln = side == "a" ? j[1] : j[2];
                if (info[ln] != undefined)
                {
                    const op = "mj" ~ jn ~ side;
                    opMateConnector(context, id + op, { "coordSystem" : cs, "owner" : owners[ln] });
                    rec[side ~ "op"] = op;
                }
            }
            joints = append(joints, rec);
            jn += 1;
        }
    }
    setVariable(context, "arvLinks", { "feature" : id[0], "links" : info, "joints" : joints });
}


// =====================================================================================================
//  ARV Link Connectors: placed after the ARV Studio Builder. Creates part-owned mate connectors with
//  predictable ids on the primary part of every kinematic link:
//     <featureId>.mo_<LINK>             origin frame (Fastened mates)
//     <featureId>.mj_<JOINT>_a / _b     joint frames (Revolute / Ball / Slider mates)
//  Link membership comes from the part attribute "link"; the primary part is named in linkPrimary().
// =====================================================================================================
export function linkPrimary() returns map
{
    return { "LNK_CHASSIS" : "MAIN_CHASSIS", "LNK_FRONT_FRAME" : "FRONT_BULKHEAD", "LNK_CRANK_L" : "BELLCRANK_L", "LNK_CRANK_R" : "BELLCRANK_R",
            "LNK_DRAG_LINK" : "DRAG_LINK", "LNK_SERVO_LINK" : "SERVO_LINK", "LNK_SERVO_HORN" : "SERVO_HORN_25T",
            "LNK_KNUCKLE_L" : "FRONT_KNUCKLE_L", "LNK_KNUCKLE_R" : "FRONT_KNUCKLE_R", "LNK_WHEEL_FL" : "RIM_FL", "LNK_WHEEL_FR" : "RIM_FR",
            "LNK_TIE_ROD_L" : "TIE_ROD_L", "LNK_TIE_ROD_R" : "TIE_ROD_R", "LNK_CAMBER_LINK_L" : "CAMBER_LINK_L", "LNK_CAMBER_LINK_R" : "CAMBER_LINK_R",
            "LNK_REAR_ARM_L" : "TRAILING_ARM_L", "LNK_REAR_ARM_R" : "TRAILING_ARM_R", "LNK_WHEEL_RL" : "RIM_RL", "LNK_WHEEL_RR" : "RIM_RR",
            "LNK_REAR_SHOCK_L" : "SHOCK_RL", "LNK_REAR_SHOCK_R" : "SHOCK_RR", "LNK_BATTERY_TUNNEL" : "BATTERY_TUNNEL", "LNK_BATTERY" : "BATTERY_3S_5000",
            "LNK_ELECTRONICS_TRAY" : "TRAY_UPPER_DECK", "LNK_FASCIA" : "FRONT_SENSOR_FASCIA", "LNK_FRONT_BUMPER" : "FRONT_BUMPER", "LNK_REAR_BUMPER" : "REAR_BUMPER",
            "LNK_BODY_MAIN" : "BODY_MAIN_REAR", "LNK_CENTER_PANEL" : "CENTER_SERVICE_PANEL", "LNK_BATTERY_PANEL" : "REAR_BATTERY_PANEL", "LNK_CAMERA_POD" : "CAMERA_POD",
            "LNK_FENDER_FL" : "FENDER_FL", "LNK_FENDER_FR" : "FENDER_FR", "LNK_FENDER_RL" : "FENDER_RL", "LNK_FENDER_RR" : "FENDER_RR" };
}

annotation { "Feature Type Name" : "ARV Link Connectors", "Feature Type Description" : "Part-owned mate connectors (origin + joint frames) on the primary part of every ARV kinematic link in this studio." }
export const arvLinkConnectors = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
    }
    {
        var P = {};
        try silent
        {
            P = derivedP(context, "arvMaster");
        }
        if (size(P) == 0)
            P = derivedP(context, "arvBody");
        const g = geo(P);
        const prim = linkPrimary();
        var owner = {};
        for (var b in evaluateQuery(context, qBodyType(qEverything(EntityType.BODY), BodyType.SOLID)))
            for (var a in getAttributes(context, { "entities" : b }))
                if (a is map && a.link != undefined && a.arvPart != undefined && prim[a.link] == a.arvPart && owner[a.link] == undefined)
                    owner[a.link] = b;
        var made = [];
        // one origin connector per part (Fastened to the link primary in the assemblies)
        var used = {};
        for (var b in evaluateQuery(context, qBodyType(qEverything(EntityType.BODY), BodyType.SOLID)))
            for (var a in getAttributes(context, { "entities" : b }))
                if (a is map && a.serial != undefined && a.link != undefined && used[a.serial] == undefined)
                {
                    used[a.serial] = true;
                    opMateConnector(context, id + ("mp" ~ a.serial), { "coordSystem" : coordSystem(v(0, 0, 0), X, Z), "owner" : b });
                }
        for (var ln, q in owner)
        {
            opMateConnector(context, id + ("mo_" ~ ln), { "coordSystem" : coordSystem(v(0, 0, 0), X, Z), "owner" : q });
            made = append(made, ln);
        }
        for (var j in jointTable(P, g))
        {
            const cs = coordSystem(v(j[4][0], j[4][1], j[4][2]), frameX(j[5]), normalize(j[5]));
            if (owner[j[1]] != undefined)
                opMateConnector(context, id + ("mj_" ~ j[0] ~ "_a"), { "coordSystem" : cs, "owner" : owner[j[1]] });
            if (owner[j[2]] != undefined)
                opMateConnector(context, id + ("mj_" ~ j[0] ~ "_b"), { "coordSystem" : cs, "owner" : owner[j[2]] });
        }
        reportFeatureInfo(context, id, "ARV connectors for " ~ size(made) ~ " links");
    });


// =====================================================================================================
//  99_AUDIT: ARV Clearance Audit -- builds the complete vehicle (all subsystem builders) in one context
//  at a steering pose, then checks:
//    STEER   : moving front-corner bodies (knuckles, wheels, tie rods, camber links, cranks, links)
//              against every static body (fenders, body, fascia, bumper, chassis...)
//    BATTERY : removal sweep of the battery out through the rear panel opening (panel removed)
//    TRAY    : vertical lift sweep of the electronics tray through the centre hatch (hatch removed)
//    STATIC  : solid clashes between different kinematic links at the design pose
//  Results are written as names of tiny marker bodies (readable through the parts list) and in the
//  feature status (WARNING when anything clashes).
// =====================================================================================================
function linkOf(context is Context, b is Query) returns string
{
    for (var a in getAttributes(context, { "entities" : b }))
        if (a is map && a.link != undefined)
            return a.link;
    return "";
}

function nameOf(context is Context, b is Query) returns string
{
    for (var a in getAttributes(context, { "entities" : b }))
        if (a is map && a.arvPart != undefined)
            return a.arvPart;
    return "?";
}

function isHw(context is Context, b is Query) returns boolean
{
    for (var a in getAttributes(context, { "entities" : b }))
        if (a is map && a.hw == true)
            return true;
    return false;
}

function marker(context is Context, id is Id, k is number, text is string)
{
    fCuboid(context, id + ("mk" ~ k), { "corner1" : v(0, 0, -200 - k * 3), "corner2" : v(1, 1, -199 - k * 3) });
    setProperty(context, { "entities" : qCreatedBy(id + ("mk" ~ k), EntityType.BODY), "propertyType" : PropertyType.NAME, "value" : text });
}

// clashing pairs between tools and targets (volume > 1 mm^3), as "A x B" strings
function clashes(context is Context, tools is array, targets is array) returns array
{
    var out = [];
    if (size(tools) == 0 || size(targets) == 0)
        return out;
    const res = evCollision(context, { "tools" : qUnion(tools), "targets" : qUnion(targets) });
    for (var c in res)
    {
        if (c["type"] == ClashType.INTERFERE || c["type"] == ClashType.TARGET_IN_TOOL || c["type"] == ClashType.TOOL_IN_TARGET)
        {
            var s = nameOf(context, c.toolBody) ~ " x " ~ nameOf(context, c.targetBody);
            var seen = false;
            for (var o in out)
                if (o[0] == s)
                    seen = true;
            if (!seen)
                out = append(out, [s, c.toolBody, c.targetBody]);
        }
    }
    return out;
}

// overlap volume and box of a clashing pair (copies intersected, then deleted)
function clashDetail(context is Context, id is Id, k is number, a is Query, b is Query) returns string
{
    var r = "";
    try silent
    {
        opPattern(context, id + ("ca" ~ k), { "entities" : a, "transforms" : [identityTransform()], "instanceNames" : ["a"] });
        opPattern(context, id + ("cb" ~ k), { "entities" : b, "transforms" : [identityTransform()], "instanceNames" : ["b"] });
        const q = qUnion([qCreatedBy(id + ("ca" ~ k), EntityType.BODY), qCreatedBy(id + ("cb" ~ k), EntityType.BODY)]);
        opBoolean(context, id + ("ci" ~ k), { "tools" : q, "operationType" : BooleanOperationType.INTERSECTION });
        const res = qUnion([q, qCreatedBy(id + ("ci" ~ k), EntityType.BODY)]);
        const bb = evBox3d(context, { "topology" : res });
        const vol = evVolume(context, { "entities" : res }) / (millimeter ^ 3);
        const lo = bb.minCorner / millimeter;
        const hi = bb.maxCorner / millimeter;
        r = vol < 2 ? "SKIP" : " vol=" ~ round(vol) ~ " x[" ~ round(lo[0]) ~ "," ~ round(hi[0]) ~ "] y[" ~ round(lo[1]) ~ "," ~ round(hi[1]) ~ "] z[" ~ round(lo[2]) ~ "," ~ round(hi[2]) ~ "]";
        opDeleteBodies(context, id + ("cd" ~ k), { "entities" : res });
    }
    if (r == "")
    {
        const ba = evBox3d(context, { "topology" : a });
        const bb = evBox3d(context, { "topology" : b });
        var lo = [];
        var hi = [];
        for (var i = 0; i < 3; i += 1)
        {
            lo = append(lo, round(max(ba.minCorner[i], bb.minCorner[i]) / millimeter));
            hi = append(hi, round(min(ba.maxCorner[i], bb.maxCorner[i]) / millimeter));
        }
        r = " (touch) bbox-overlap x[" ~ lo[0] ~ "," ~ hi[0] ~ "] y[" ~ lo[1] ~ "," ~ hi[1] ~ "] z[" ~ lo[2] ~ "," ~ hi[2] ~ "]";
    }
    return r;
}

annotation { "Feature Type Name" : "ARV Clearance Audit", "Feature Type Description" : "Whole-vehicle interference audit at a steering pose + battery removal and tray service sweeps." }
export const arvAudit = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation { "Name" : "Steering crank angle (deg, +/-31 = full lock)" }
        isAngle(definition.steer, { (degree) : [-40, 0, 40] } as AngleBoundSpec);
        annotation { "Name" : "Include purchased hardware", "Default" : false }
        definition.hardware is boolean;
    }
    {
        const P = derivedP(context, "arvMaster");
        const g = geo(P);
        const theta = definition.steer / degree;
        setVariable(context, "arvHardware", definition.hardware);
        setVariable(context, "arvError", "");
        buildChassis(context, id + "a", P, g);
        buildFront(context, id + "b", P, g, theta);
        buildDrive(context, id + "c", P, g);
        buildBattery(context, id + "d", P, g);
        buildElectronics(context, id + "e", P, g);
        buildBodyMain(context, id + "f", P, g);
        buildCenterPanel(context, id + "g", P, g);
        buildBatteryPanel(context, id + "h", P, g);
        buildFascia(context, id + "i", P, g);
        buildCameraPod(context, id + "j", P, g);
        buildFenders(context, id + "k", P, g);
        const st = getVariable(context, "arvSteer");
        // classify bodies
        const moving = ["LNK_KNUCKLE_L", "LNK_KNUCKLE_R", "LNK_WHEEL_FL", "LNK_WHEEL_FR", "LNK_TIE_ROD_L", "LNK_TIE_ROD_R", "LNK_CAMBER_LINK_L", "LNK_CAMBER_LINK_R"];
        var mv = [];
        var stat = [];
        var byLink = {};
        for (var b in evaluateQuery(context, qBodyType(qEverything(EntityType.BODY), BodyType.SOLID)))
        {
            const ln = linkOf(context, b);
            if (ln == "")
                continue;
            if (byLink[ln] == undefined)
                byLink[ln] = [];
            byLink[ln] = append(byLink[ln], b);
            if (isIn(ln, moving))
                mv = append(mv, b);
            else if (!isIn(ln, ["LNK_CRANK_L", "LNK_CRANK_R", "LNK_DRAG_LINK", "LNK_SERVO_LINK", "LNK_SERVO_HORN"]))
                stat = append(stat, b);
        }
        var k = 0;
        var warn = false;
        // STEER: moving corner vs static world
        const sc = clashes(context, mv, stat);
        marker(context, id, k, "AUDIT STEER theta=" ~ round(theta * 10) / 10 ~ " psiL=" ~ round(st.psiL * 10) / 10 ~ " psiR=" ~ round(st.psiR * 10) / 10 ~ " clashes=" ~ size(sc));
        k += 1;
        for (var s in sc)
        {
            marker(context, id, k, "CLASH " ~ s[0] ~ clashDetail(context, id, k, s[1], s[2]));
            k += 1;
            warn = true;
        }
        if (abs(theta) < 1e-6)
        {
            // BATTERY removal sweep: battery footprint extruded rearward past the tail (panel removed)
            const kk = pack(P, g);
            const bt = kk.BATTERY;
            var Sw = bld(id + "sweepB");
            wb(context, Sw, g.xTail - 160, bt[0], bt[2] + 0.3, bt[3] - 0.3, bt[4] + 0.3, bt[5] - 0.3, true);
            stg(context, Sw);
            var others = [];
            for (var ln, bs in byLink)
                if (ln != "LNK_BATTERY" && ln != "LNK_BATTERY_PANEL")
                    others = concatenateArrays([others, bs]);
            var bc = [];
            for (var c in evCollision(context, { "tools" : posQ(Sw), "targets" : qUnion(others) }))
                if (c["type"] == ClashType.INTERFERE || c["type"] == ClashType.TARGET_IN_TOOL || c["type"] == ClashType.TOOL_IN_TARGET)
                {
                    const nm = nameOf(context, c.targetBody);
                    if (!isIn(nm, bc))
                        bc = append(bc, nm);
                }
            marker(context, id, k, "AUDIT BATTERY_REMOVAL obstructions=" ~ size(bc) ~ (size(bc) > 0 ? " " ~ toString(bc) : " PASS"));
            k += 1;
            warn = warn || size(bc) > 0;
            opDeleteBodies(context, id + "sweepBdel", { "entities" : posQ(Sw) });
            // TRAY lift sweep through the centre hatch opening (hatch removed)
            var St = bld(id + "sweepT");
            wb(context, St, g.deckX0 + 0.3, g.deckX1 - 0.3, -g.HW + 4.5, g.HW - 4.5, g.deckZ1 + 0.5, 400, true);
            stg(context, St);
            var oth2 = [];
            for (var ln, bs in byLink)
                if (ln != "LNK_ELECTRONICS_TRAY" && ln != "LNK_CENTER_PANEL")
                    oth2 = concatenateArrays([oth2, bs]);
            var tc = [];
            for (var c in evCollision(context, { "tools" : posQ(St), "targets" : qUnion(oth2) }))
                if (c["type"] == ClashType.INTERFERE || c["type"] == ClashType.TARGET_IN_TOOL || c["type"] == ClashType.TOOL_IN_TARGET)
                {
                    const nm = nameOf(context, c.targetBody);
                    if (!isIn(nm, tc))
                        tc = append(tc, nm);
                }
            marker(context, id, k, "AUDIT TRAY_LIFT obstructions=" ~ size(tc) ~ (size(tc) > 0 ? " " ~ toString(tc) : " PASS"));
            k += 1;
            warn = warn || size(tc) > 0;
            opDeleteBodies(context, id + "sweepTdel", { "entities" : posQ(St) });
            // STATIC: clashes between different links at the design pose (hardware excluded)
            var names = [];
            for (var ln, bs in byLink)
                names = append(names, ln);
            var sclash = [];
            for (var i = 0; i < size(names); i += 1)
            {
                var ti = [];
                for (var b in byLink[names[i]])
                    if (!isHw(context, b))
                        ti = append(ti, b);
                var rest = [];
                for (var j = i + 1; j < size(names); j += 1)
                    for (var b in byLink[names[j]])
                        if (!isHw(context, b))
                            rest = append(rest, b);
                for (var s in clashes(context, ti, rest))
                    sclash = append(sclash, s);
            }
            marker(context, id, k, "AUDIT STATIC_CLASHES=" ~ size(sclash));
            k += 1;
            for (var s in sclash)
            {
                const det = clashDetail(context, id, k, s[1], s[2]);
                if (det == "SKIP")
                    continue;
                marker(context, id, k, "SCLASH " ~ s[0] ~ det);
                k += 1;
            }
            warn = warn || size(sclash) > 0;
        }
        var err = "";
        try silent
        {
            err = getVariable(context, "arvError");
        }
        if (err != "")
            marker(context, id, k, "BUILD ERRORS " ~ err);
        if (warn)
            reportFeatureWarning(context, id, "ARV audit found clashes (see AUDIT/CLASH marker part names)");
    });
