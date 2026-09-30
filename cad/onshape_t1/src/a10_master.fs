
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
