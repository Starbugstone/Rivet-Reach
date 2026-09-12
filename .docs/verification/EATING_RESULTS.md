# Eating animation and crumbs verification

The 2026-09-12 change implements [food-to-mouth presentation](../GAMEPLAY.md#eating-presentation--2026-09-12) and falling crumbs using the existing first-person rig. The 1.2-second hunger/inventory transaction is preserved; interrupted bites restart when the selected slot or use action changes.

## Tested candidate

Unity **6000.4.4f1**, Windows x64 Development player, Direct3D 11, 1280×720. The isolated candidate starts from committed `71ace54`, overlays the eating implementation, and includes the concurrently authored potato/baked-potato models and their held-food presentation. Concurrent station, ore, door and hand-crank work is excluded. [Artifact manifest](eating-2026-09-12-artifact.json) records exact source and companion food asset hashes. This is focused evidence for that snapshot, not a full-workspace regression result.

The [build summary](eating-2026-09-12-build-summary.txt) records success with **zero errors and zero warnings**. The standalone process exited 0; its [runtime report](eating-2026-09-12-runtime-report.json), completed at 09:17:48 UTC, records **100 passing checks** and no errors.

## Measured behavior

- Both male/female models, both test skins and FOV 60/78/100: food moves inward and closer from its resting grip, remains below camera centre, and retains hand/socket attachment. The measured eating socket is approximately viewport `(0.537, 0.418)` at depth `0.326` m.
- Each combination emits a bounded crumb burst; releasing Use stops further emission and returns the pose without consuming food. A tracked released particle moves downward under world gravity.
- A completed baked-potato bite consumes exactly one item and restores five hunger points. Same-food slot changes restart the timer; inventory and pause cancel the pose and clear crumbs. Creative mode and full hunger skip eating.
- Zero effect intensity suppresses crumbs. Planting and station use retain precedence and cancel unfinished bites. The last item disappears on its consumption frame without a ghost held item.

## Rendered evidence

[Animation preview](eating-2026-09-12-preview.mp4) contains 110 consecutive game-rendered frames at fixed 60 Hz (1.83 seconds), encoded through Blender without interpolation. It shows the lift, bite pulses, food-coloured fragments falling and recovery. Reviewed stills: [resting grip](eating-2026-09-12-rest.png), [baked potato and crumbs](eating-2026-09-12-baked.png), [raw potato, female alternate skin](eating-2026-09-12-raw.png). The raised food stays below the crosshair and above the status bars; the forearm extends through the lower HUD area.

Crumbs reuse the original authored chip mesh, with a separate fixed pool of 32 particles, short fading lifetimes and no collectible drops. The optional first-person shader depth setting defaults off for existing world debris. The arm solve preserves the authored limb lengths and food socket; no replacement player model, clip or dependency is introduced by this change.

## Reproduce and limits

Build through [EatingBuild.cs](../../Assets/RivetReach/Editor/EatingBuild.cs), then run [Verify-Eating.ps1](../../Tools/Verify-Eating.ps1) with `-CaptureVideo`; the fixture is implemented in [EatingVerification.cs](../../Assets/RivetReach/Code/EatingVerification.cs). The tested executable is locally available at `Builds/Eating/RivetReach.exe`. Use the manifest to distinguish its isolated snapshot from later builds.

Fixed-step capture is not a performance benchmark. User review still owns animation feel. This change covers first-person presentation; third-person eating clips and eating sounds are not added. Floating-origin particle adjustment is implemented but was not exercised by this focused run. Existing dated avatar performance results remain specific to their original build.
