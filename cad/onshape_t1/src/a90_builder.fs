
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
    FENDERS,
    annotation { "Name" : "55 Wiring" }
    WIRING,
    annotation { "Name" : "70 LiDAR option" }
    LIDAR
}

function isBody(s is ArvStudio) returns boolean
{
    return s == ArvStudio.FASCIA || s == ArvStudio.BODY_MAIN || s == ArvStudio.CENTER_PANEL || s == ArvStudio.BATTERY_PANEL || s == ArvStudio.CAMERA_POD || s == ArvStudio.FENDERS || s == ArvStudio.LIDAR;
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
    else if (s == ArvStudio.WIRING)
        buildWiring(context, id, P, g);
    else if (s == ArvStudio.LIDAR)
        buildLidar(context, id, P, g);
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
        var err = "";
        try silent
        {
            err = getVariable(context, "arvError");
        }
        if (err != "")
        {
            fCuboid(context, id + "errmk", { "corner1" : v(0, 0, -300), "corner2" : v(1, 1, -299) });
            setProperty(context, { "entities" : qCreatedBy(id + "errmk", EntityType.BODY), "propertyType" : PropertyType.NAME, "value" : "BUILD ERRORS " ~ err });
        }
        // hide the derived master reference sketches' clutter is left to the user; they are construction only
    });
