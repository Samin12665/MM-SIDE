# cad/onshape_t1 — ARV-1 T1 Onshape library and tools

The FeatureScript library in `01_FS_ARV_LIB` of the Onshape document **SIDE ARV-1 Autonomous RC Car**
(`https://cad.onshape.com/documents/495cc3804c007e87f5b21cee`). See `docs/13_T1_ONSHAPE_BUILD.md`.

| File | Content |
|---|---|
| `src/a00_core.fs` | Master defaults, `geo()` derived geometry, CSG helpers, materials, hardware (screws / inserts) |
| `src/a10_master.fs` | Packaging table `pack()`, body profiles, **ARV Master Layer** (SK01–SK05, datums, connectors) |
| `src/a20_chassis.fs` | 10_CHASSIS builders |
| `src/a30_front.fs` | 20_FRONT_STEERING builders + linkage solver (`solvePsi`), wheels |
| `src/a40_drive.fs` | 30_DRIVETRAIN, 40_BATTERY_SYSTEM, 50_ELECTRONICS_TRAY builders |
| `src/a50_body.fs` | Body zones / shell, **ARV Body Master**, 61–66 body builders |
| `src/a90_builder.fs` | **ARV Studio Builder** feature (one per subsystem studio, placed after its Derived feature) |
| `src/a95_links.fs`, `src/a96_connectors.fs` | Kinematic joint table, **ARV Link Connectors** (part-owned mate connectors) |
| `src/a97_audit.fs` | **ARV Clearance Audit** (steering pose, battery removal sweep, tray lift sweep, static clashes) |
| `ARV_LIB.fs` | Concatenated library (`node tools/build.js`), identical to the Feature Studio contents |
| `tools/helpers.js`, `tools/asm.js` | Browser-session REST helpers: push library, create studios / Derived features, build assemblies and mates |
| `tools/serve.js`, `tools/carry.html` | Local file relay (the Onshape tab's CSP blocks localhost fetches; files travel through `window.name`) |

To change the car, edit a `#variable` in `00_CAR_MASTER`: every studio updates through its Derived feature.
To change part detail, edit `src/*.fs`, run `node tools/build.js`, and paste or push `ARV_LIB.fs` into `01_FS_ARV_LIB`.
