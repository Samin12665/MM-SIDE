
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
