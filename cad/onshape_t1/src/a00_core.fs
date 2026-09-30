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
