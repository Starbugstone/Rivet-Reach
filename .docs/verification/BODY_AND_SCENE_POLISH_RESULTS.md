# First-person body and scene polish — 2026-09-08

The user reported that looking down cut away half the body and requested a modest improvement across the visuals. The visibility filter removed every torso triangle weighted to `Spine` or `Chest`, leaving only the lower body. Restoring those triangles also required first-person camera clearance: the open collar must remain outside the lens, especially during the forward bend in crouch.

## Delivered presentation

- Retain the jacket, waist and legs in first person, excluding the head/neck and duplicate arms. The body sits behind the eye camera with an offset interpolated by the existing crouch animation weight. Camera position, collision dimensions, movement and mining/placement rays are unchanged. F5 uses the full model in its usual position.
- Cast the complete selected character silhouette with a shadow-only renderer sharing the existing bones and animation graph. First-person hands do not cast a second shadow. Both models and both skin palettes use the same visibility/clearance rules.
- Original 64×64 grass, grass-side, soil and stone texture-array layers, mipmaps, 8× anisotropic filtering and restrained world-space palette variation. The palette noise repeats over 1024 m; reducing the integer rendering origin before float conversion preserves its phase across origin shifts.
- Warmer directional daylight, three-colour ambient fill, adjusted near shadow cascades, slow-moving stylized clouds, a sun aligned to the directional light and light aerial haze before the existing streaming fade. Fog start/end remain 236.8/304 m at radius 10.
- Enable the existing scene's post-processing on the runtime camera; use restrained exposure/contrast, bloom and vignette. Motion blur remains disabled. Smaller HUD/hotbar, outlined selection, a compact crosshair and terrain-textured inventory icons preserve the existing 12+48 slot layout and interactions.

The [Blender source render](player-v4-blender-front.png) confirms the original full models already contained the missing torso. Mesh sources, FBX files, skin atlases and authored animation clips are unchanged; this revision corrects their runtime presentation. The [grassland](../concept-art/world-grassland-fps-v1.png) and [cave](../concept-art/world-cave-mining-fps-v1.png) concepts guided the terrain/material/lighting pass. It does not introduce vegetation, structures, biomes, weather simulation, propagated voxel lighting or later gameplay systems.

## Final verification

All results below are from the final stone-material/HUD revision on 2026-09-08, using Unity 6000.4.4f1.

| Check | Result | Evidence |
| --- | --- | --- |
| Windows development build | Succeeded; zero errors and warnings | [Build and domain results](body-scene-build.txt) |
| Pure domain checks | 22,371 assertions passed | [Build and domain results](body-scene-build.txt) |
| Full standalone terrain/inventory and visual regression | 84 assertions passed; no logged errors | [Runtime report](body-scene-runtime-report.json) |
| Two Editor Play/Stop cycles | 66 checks passed, including standing/crouching at 100° FOV | [Checks](body-scene-editor-checks.txt), [result](body-scene-editor-result.txt) |

Screenshots were visually reviewed for the retained torso, collar clearance, both appearances/skins, terrain surfaces, clouds and inventory readability. Automated checks confirm geometry and camera landmarks; they do not establish that every animation frame is free of intersections.

![Standing look-down in the Unity Editor at 100° FOV](body-scene-editor-lookdown-100.png)

![Crouching look-down in the Unity Editor at 100° FOV](body-scene-editor-crouch-100.png)

![Updated terrain, daylight and sky](body-scene-landscape.png)

![Irregular stone mineral patches in a generated cave](body-scene-cave.png)

Additional captures: [male look-down](body-scene-lookdown-male.png), [female alternate-skin look-down](body-scene-lookdown-female-alternate.png), [crouch at the default FOV](body-scene-crouch.png), [sky](body-scene-sky.png), [textured hotbar](body-scene-hotbar.png), [inventory](body-scene-inventory.png).

The standalone run used an i7-10750H / RTX 2060, 1280×720, radius 10 and a 90 FPS cap. Recorded frame median was **11.11 ms**, p95 **11.21 ms**, maximum **20.72 ms**; the separate mining sample peaked at **17.21 ms**. Peak allocated memory was 216,635,370 bytes (206.6 MiB), with 1,182 resident chunks at peak. Initial readiness took 7.64 seconds; mining/placement mesh jobs recorded 8.45/3.54 ms. Draw-call profiling was unavailable. These are measurements for this workload and machine, not GPU-isolated or uncapped performance claims. Sampling includes the new warm visual review as well as the existing movement workload, so it is not directly equivalent to the earlier revision-4 benchmark.

## Reproduce

Build with `Tools/Build-Windows.ps1`. Ordinary Unity Play still opens Main's title menu and requires no Blender or preparation command. Stop Play before allowing runtime code to recompile, then enter a fresh session.

`Tools/Verify-POC.ps1` now includes both-model/both-skin look-down captures, 60°/85° pitch, actual mapped crouch, 60°/100° FOV, torso retention, full-shadow geometry and neck-opening visibility checks before the original terrain/inventory regressions. For a shorter visual iteration, run the same executable with `-rr-verify -rr-visual-review -rr-output <directory>`; its report identifies that limited workload and does not imply the complete gameplay suite passed.

**Rivet Reach → Verify Editor Play startup** runs two ordinary Editor Play/Stop cycles, checks the runtime colour grade and captures look-down at 100° FOV as well as the title and world. These explicit verification modes briefly drive the player; ordinary sessions never enable them.

Shader compilation errors now fail the build helper even if Unity labels the overall build `Succeeded`. The runtime review additionally checks that the terrain shader has a supported rendering pass. Visual inspection remains necessary: component and animation checks alone cannot certify framing or material quality.

## Rendering cost and remaining review

The imported source meshes remain 31,734 triangles for the male and 32,126 for the female, with 40 bones and one material. The visible first-person body now has **8,544 triangles**, versus 5,686 in revision 4; the arm pair remains **10,496**, for **19,040 visible triangles** together. The shadow-only renderer submits the complete selected mesh to applicable shadow passes, so its additional 31,734/32,126 triangles and skinning are separate from that visible total. It shares the existing bones/graph and does not create a second Animator. These counts exclude repeated shadow cascades and other rendering passes.

The four RGBA32 64×64 terrain layers have seven mip levels: 87,376 bytes of pixel payload for one full CPU/GPU copy, before object/driver overhead. The array remains CPU-readable for the small terrain-icon construction pass. The change increases texture and shadow work; triangle count alone is not a frame-rate guarantee.

Clouds are a stylized sky shader, without volumetric simulation or cloud shadows. The skin atlas, character topology, slope foot placement and full cave-light propagation remain as described in the earlier player/content reports. Textures are an original reusable procedural kit, with remaining repetition visible on large exposed surfaces. Final likeness, atmosphere and movement feel remain subject to user review.
