
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
