# Player model and animation rebuild

The user requested a complete player-model revision toward the [approved male/female concept](../concept-art/player-male-female-turnaround-v3.png), corrected first-person hand orientation/placement, and a skeleton with proper animation. This revision rebuilds both original explorer assets in Blender 5.2 and imports their authored animations into Unity 6000.4.4f1. Artistic acceptance remains with the user.

## Delivered assets

- Shaped face/jaw/nose/eyes, swept male hair and female low ponytail, fitted teal waistcoat, rolled linen sleeves, joined trouser silhouette and leather boots.
- A common 40-bone skeleton retaining the previous body/limb names and adding neck, clavicles, and two joints for each finger/thumb. Blended weights articulate the elbows, wrists, knees and ankles; welded seam vertices use at most three influences.
- Eight editable Blender actions per model: `Idle` (3 s), `Walk` (1 s), `Run` (0.8 s), `Airborne` (1 s), `Mine` (0.6 s), `FP_Idle` (3 s), `FP_Walk` (1 s), and `FP_Mine` (0.6 s).
- In-place Unity Playables blending. A masked mining layer leaves the locomotion legs active. Actual horizontal movement drives the gait; sprint/grounded state selects run/airborne presentation. Animation does not control collision, reach, inventory or block removal.
- First-person fists with their backs up and thumbs inward, authored wrist rotation, and lower camera placement. Camera-relative projection compensation keeps the hand framing stable when the world FOV changes, without a second rendering camera. Both hands remain available; the right hand performs the mining stroke. This uses the familiar lower-corner framing as inspiration and includes no Minecraft assets or animations.
- Both variants retain both local test skins, one material, and the existing palette-region semantics. Existing Unity asset GUIDs are preserved.

## Actual visual evidence

Male remains on the left and female on the right of the comparison images.

| Source meshes in Blender | Imported meshes in Unity |
|---|---|
| [Front](player-v3-blender-front.png) | [Front](player-v3-unity-front.png) |
| [Back](player-v3-blender-back.png) | [Back](player-v3-unity-back.png) |
| [Face detail](player-v3-blender-detail.png) | [Walking pose](player-v3-unity-walk.png) |
| [First-person rest](player-v3-blender-hands.png) | [First-person rest](player-v3-unity-hands.png) |
| | [Mining pose](player-v3-unity-strike.png) |

[Terrain session](player-v3-world.png) and [appearance screen](player-v3-appearance.png) show the same imported assets in the playable slice. The studio captures expose pose and silhouette; they are not generated concept illustrations. The verification output also includes both skins on both models, running/airborne poses, [60°](player-v3-fov-60.png)/[100°](player-v3-fov-100.png) camera comparisons and a sequence sampled through the live animation graph.

## Reproduction and checks

Editable sources: `ArtSource/Characters/ExplorerMale.blend` and `ExplorerFemale.blend`. Explicit runtime exports: `Assets/RivetReach/Resources/Characters/`. The game does not invoke Blender.

From Blender's CLI, run these scripts with their absolute Windows paths:

1. [create_player_assets.py](../../Tools/create_player_assets.py): rebuild sources, skin PNGs, rig/actions and FBX exports.
2. [check_player_assets.py](../../Tools/check_player_assets.py): check normalized weights, one UV/material, relative texture references and finite evaluated geometry across five sample frames for each action. The edge-length check catches severe deformation but is not a complete intersection detector.
3. [render_player_review.py](../../Tools/render_player_review.py) and [render_player_hands.py](../../Tools/render_player_hands.py): render actual source geometry for comparison.

Use [Build-Windows.ps1](../../Tools/Build-Windows.ps1), then [Verify-Player.ps1](../../Tools/Verify-Player.ps1) for the isolated imported-character checks. [Verify-POC.ps1](../../Tools/Verify-POC.ps1) exercises the character inside the terrain/inventory slice. The player output is `Builds/PlayerRevision3/RivetReach.exe`; the previous review executable is preserved separately. Verification modes are explicit command-line options and are not enabled in ordinary play.

Measured reports: [source checks](player-v3-source-checks.json), [Unity character checks](player-v3-avatar-report.json), [terrain-session checks](player-v3-runtime-report.json), and [Windows build](player-v3-build.txt).

## Measured import results

| Geometry submitted | Triangles | Bones | Material/submesh |
|---|---:|---:|---:|
| Male full body | 4,544 | 40 | 1 / 1 |
| Female full body | 4,760 | 40 | 1 / 1 |
| Selected first-person arm pair | 1,168 | shared rig | 1 / 1 |

The full models and derived arms meet the existing provisional 5,000/1,500 triangle budgets. Unity removes degenerate triangles during import, so the source report can differ slightly. At the verification camera's 78° vertical FOV and 1280×900 frame, resting wrist centres project to x=0.217/0.783 and y=0.097 (viewport origin at bottom left), 0.36 m in front of the camera. Wrist centres are below the targeting area; hand silhouette and reach were also inspected in the captures.

The final character verification passes 25 assertions with no logged Unity errors, including the live upper-body mining layer over locomotion. The Windows development build completed with zero errors and zero warnings. Source checks pass for both models and all eight actions. The build also runs the existing 22,371 domain assertions; these concern the terrain/inventory foundations rather than artistic quality.

The terrain-session verification passes all 52 checks, including mining/placement quantities, mapped movement/jump/crouch, inventory operations, appearance height, and unload/reload behavior. The initial run exposed a frame-timing dependency in the partial-pickup fixture: the preceding mined drop could compete with its controlled pile. The fixture now temporarily delays existing piles during its two synchronous steps and restores their delays afterward; ordinary pickup/merge rules are unchanged.

The first-person body subset contributes 1,466 triangles alongside the 1,168-triangle arm pair (2,634 submitted triangles combined, before visibility/shadow passes). The terrain-run report records whole-scene timings on the stated machine at a 90 FPS cap; it does not isolate the character's CPU/GPU cost, and its draw-call recorder was unavailable (`-1`).

## Review limits

The source/Unity comparisons were visually inspected and used to correct clothing intersections, hair clumps, the trouser seam, wrist roll, thumb curl and leg/ankle poses. Automated checks establish the listed import/state properties; they do not establish professional artistic quality or user acceptance of likeness and animation feel.

The palette remains a shared 256×256 region atlas with reused UVs, rather than a finished face-by-face paint template. Facial expressions, foot placement over arbitrary slopes, tool/combat animations, and a complete customization catalogue are not supplied. The airborne action is a held pose, and the existing crouch body-height presentation remains. No crowd or isolated animation CPU/GPU benchmark was performed; geometry counts alone do not prove whole-game performance.
