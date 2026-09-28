# 07 — Printability Review and Print Guide (PHASE 16)

Per-part settings (material, orientation, layer, walls, infill, supports, inserts, post-processing,
bounding box, mass, STL path) are in **`bom/print_list.csv`**, generated from the CAD. STLs in
`out/stl/` are already in print orientation, sitting on Z = 0.

## Material strategy

| Material | Used for | Why |
|---|---|---|
| **PA-CF** (nylon-CF) | Knuckles, lower arms, trailing arms, bellcranks, links, hex adapters, bulkhead, shock towers | Highest toughness and fatigue resistance of common FDM materials, no creep at bearing seats, 45+ MPa in-plane |
| **PETG-CF** | Main tub, sensor mast, LiDAR mast | Stiff and dimensionally stable for the large parts, easier than PA-CF on big prints, heat-set inserts hold well |
| **PETG** | Deck, trays, doors, skid, skirts, clips, sensor adapters | Cheap, tough, easy; these parts are stiffness- or retention-limited, not strength-limited |
| **TPU 95A** | Front and rear bumpers | Crush zone absorbs impact energy, protects the ToF sensors and the chassis |
| (ASA) | Optional tub/deck for sunny outdoor use | UV and heat resistance |

No PLA anywhere: it creeps under insert preload and softens in a hot car.

## Orientation decisions for loaded parts

| Part | Orientation | Load direction vs layers |
|---|---|---|
| Front lower arm | Flat | Vertical wheel load bends the arm in the XY plane of the layers, so the fibres carry it |
| Knuckle | Bearing axis vertical | Bearing seats print round and accurate; steering-arm bending stays in-plane |
| Trailing arm | Inboard face down | Beam bending (vertical loads) runs along the layers; motor cup and pivot bores print round |
| Bellcranks, links | Flat | Pure in-plane loading |
| Shock towers | Plate flat | Shock loads bend the plate in-plane |
| Tub | Floor down | Walls are layer-stacked but carry mostly in-plane shear; rails and deck take bending |
| Sensor mast | Lying on its back | Fore-aft camera vibration bends along the layers, giving a stiffer camera mount |

## Printer fit

All 35 printed part designs (46 pieces including 12 cable clips) fit a **256 × 256 × 256** bed with margin. The
largest are the tub (230 × 158 × 45), deck (208 × 160 × 14), bumpers (230 long) and rear tower
(56 × 192 × 148). A 220 × 220 bed prints everything except the tub, deck and bumpers. For those,
split the tub at the battery tunnel wall (X = 2 mm) with a bolted lap joint; the split is not
modelled yet.

## Supports

Designed to be support-free except for:

- the rear shock tower E-stop shelf and bumper hoop arms (tree supports);
- the camera-mount arm (tiny support).

Insert pockets all open upward in print orientation, or are drilled after printing where noted.

## Estimated print time and material (0.4 mm nozzle)

| Material | Mass incl. fill (g) | Time (h) |
|---|---:|---:|
| PA-CF | ≈ 370 | ≈ 22 |
| PETG-CF | ≈ 390 | ≈ 18 |
| PETG | ≈ 370 | ≈ 16 |
| TPU | ≈ 135 | ≈ 12 |
