
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
        const lb = arr(casterT(P, g) * v(g.xF, s * g.KY, g.kLowZ));
        const ub = arr(casterT(P, g) * v(g.xF, s * g.KY, g.kUpZ));
        j = append(j, ["ARM_HINGE_" ~ t, "LNK_FRONT_FRAME", "LNK_ARM_" ~ t, "REVOLUTE", [g.xF, s * g.hingeY, g.kLowZ], X]);
        j = append(j, ["UARM_HINGE_" ~ t, "LNK_FRONT_FRAME", "LNK_UPPER_ARM_" ~ t, "REVOLUTE", [g.xF, s * g.camberInY, g.kUpZ], X]);
        j = append(j, ["LBALL_" ~ t, "LNK_ARM_" ~ t, "LNK_KNUCKLE_" ~ t, "BALL", lb, Z]);
        j = append(j, ["UBALL_" ~ t, "LNK_UPPER_ARM_" ~ t, "LNK_KNUCKLE_" ~ t, "BALL", ub, Z]);
        j = append(j, ["WHEEL_F" ~ t, "LNK_KNUCKLE_" ~ t, "LNK_WHEEL_F" ~ t, "REVOLUTE", [g.xF, s * g.yW, g.zA], Y]);
        j = append(j, ["TIE_OUT_" ~ t, "LNK_KNUCKLE_" ~ t, "LNK_TIE_ROD_" ~ t, "BALL", armTip0(P, g, s), Z]);
        j = append(j, ["TIE_IN_" ~ t, "LNK_CRANK_" ~ t, "LNK_TIE_ROD_" ~ t, "BALL", crankBall(P, g, s, 0), Z]);
        const fsl = [g.shockFX, s * g.shockFY, g.kLowZ + 4 + 3.4];
        const fsu = [g.towerF0 - 12, s * g.shockFY, g.shockFUpZ];
        j = append(j, ["FSHOCK_TOP_" ~ t, "LNK_FRONT_FRAME", "LNK_SHOCKB_F" ~ t, "BALL", fsu, Z]);
        j = append(j, ["FSHOCK_BOT_" ~ t, "LNK_ARM_" ~ t, "LNK_SHOCKR_F" ~ t, "BALL", fsl, Z]);
        j = append(j, ["FSHOCK_SLIDE_" ~ t, "LNK_SHOCKB_F" ~ t, "LNK_SHOCKR_F" ~ t, "CYLINDRICAL", [(fsl[0] + fsu[0]) / 2, (fsl[1] + fsu[1]) / 2, (fsl[2] + fsu[2]) / 2], pt(fsu) - pt(fsl)]);
        j = append(j, ["RARM_PIVOT_" ~ t, "LNK_CHASSIS", "LNK_REAR_ARM_" ~ t, "REVOLUTE", [g.pivX, s * (g.HW + 10), g.pivZ], Y]);
        j = append(j, ["WHEEL_R" ~ t, "LNK_REAR_ARM_" ~ t, "LNK_WHEEL_R" ~ t, "REVOLUTE", [g.xR, s * g.yW, g.zA], Y]);
        const rsl = [g.shockRLowX, s * g.shockRY, g.shockRLowZ];
        const rsu = [g.towerR0 - 9 - 3.4, s * g.shockRY, g.shockRUpZ];
        j = append(j, ["RSHOCK_TOP_" ~ t, "LNK_CHASSIS", "LNK_SHOCKB_R" ~ t, "BALL", rsu, Z]);
        j = append(j, ["RSHOCK_BOT_" ~ t, "LNK_REAR_ARM_" ~ t, "LNK_SHOCKR_R" ~ t, "BALL", rsl, Z]);
        j = append(j, ["RSHOCK_SLIDE_" ~ t, "LNK_SHOCKB_R" ~ t, "LNK_SHOCKR_R" ~ t, "CYLINDRICAL", [(rsl[0] + rsu[0]) / 2, (rsl[1] + rsu[1]) / 2, (rsl[2] + rsu[2]) / 2], pt(rsu) - pt(rsl)]);
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
