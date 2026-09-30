
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
