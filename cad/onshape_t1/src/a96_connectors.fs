
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
            "LNK_TIE_ROD_L" : "TIE_ROD_L", "LNK_TIE_ROD_R" : "TIE_ROD_R", "LNK_ARM_L" : "FRONT_LOWER_ARM_L", "LNK_ARM_R" : "FRONT_LOWER_ARM_R",
            "LNK_UPPER_ARM_L" : "FRONT_UPPER_ARM_L", "LNK_UPPER_ARM_R" : "FRONT_UPPER_ARM_R", "LNK_SHOCKB_FL" : "SHOCK_BODY_FL", "LNK_SHOCKB_FR" : "SHOCK_BODY_FR",
            "LNK_SHOCKR_FL" : "SHOCK_ROD_FL", "LNK_SHOCKR_FR" : "SHOCK_ROD_FR", "LNK_SHOCKB_RL" : "SHOCK_BODY_RL", "LNK_SHOCKB_RR" : "SHOCK_BODY_RR",
            "LNK_SHOCKR_RL" : "SHOCK_ROD_RL", "LNK_SHOCKR_RR" : "SHOCK_ROD_RR", "LNK_WIRING" : "HARNESS_MAIN_POWER", "LNK_LIDAR" : "LIDAR_ADAPTER_PLATE",
            "LNK_REAR_ARM_L" : "TRAILING_ARM_L", "LNK_REAR_ARM_R" : "TRAILING_ARM_R", "LNK_WHEEL_RL" : "RIM_RL", "LNK_WHEEL_RR" : "RIM_RR",
            "LNK_BATTERY_TUNNEL" : "BATTERY_TUNNEL", "LNK_BATTERY" : "BATTERY_3S_5000",
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
