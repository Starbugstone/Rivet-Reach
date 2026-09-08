# Player fidelity and movement polish

The user authorized a higher polygon count and requested closer concept likeness, smooth movement and responsive controls after the [revision 3 rebuild](PLAYER_REBUILD_RESULTS.md). This revision refines both original explorers in Blender 5.2 and integrates them into the existing Unity 6000.4.4f1 playable slice. The [approved turnaround](../concept-art/player-male-female-turnaround-v3.png) remains the visual reference; artistic acceptance and preferred movement feel remain with the user.

## Changes delivered

- Dense facial loops shape the cheeks, eye sockets, muzzle and jaw, with inset almond eyes, skin eyelids, defined nose planes and shaped brows. Curved hair locks retain longitudinal ridges and the female low ponytail.
- Slimmer sleeves, open collars, fitted waistcoats, broad cloth folds, a continuous trouser saddle, conforming pocket stitching and curved leather boot straps. A welded wrist/palm surface removes the detached-hand seam. Shading distinguishes soft skin/cloth from harder trim and hair ridges.
- The shared 40-bone skeleton has blended joints and articulated fingers/thumbs. Sixteen authored clips replace the eight-clip library: standing idle/walk/run, airborne, crouch idle/walk, landing and mining, with corresponding first-person clips. The standing/crouch gait uses authored two-bone leg solves; gameplay still owns translation and collision.
- Exponential movement and mining blends give a quick attack and softer settling. Signed travel advances the gait, backpedalling reverses it without resetting phase, and a filtered strafe stance turns the feet while the upper body stays oriented toward the view. Presentation offsets are restored before the next graph evaluation so they cannot accumulate.
- Crouch uses bent knees/hips rather than scaling the mesh. The eye transitions smoothly while the collider changes immediately; a ceiling probe limits camera clearance. Landing activates a brief impact pose. Small first-person turn sway affects the hands while mouse aim remains direct.
- The mining mask now uses paths relative to the Animator. A stronger regression check distinguishes a raised mining hand from an ordinary walking swing. Mining remains an upper-body layer over moving legs.
- First-person clips place the shoulders lower and outward, keeping the connected upper sleeve below the camera during extension. All vertex streams are compacted, so hidden face/hair vertices are not skinned again by the arms renderer. Both skins, both variants, their shared material and Unity asset GUIDs are preserved.

## Visual evidence

These are actual source renders and Windows-player captures, not generated illustrations. Male is on the left and female on the right in comparison sheets.

| Blender sources | Unity imports / live game |
|---|---|
| [Front](player-v4-blender-front.png) | [Front](player-v4-unity-front.png) |
| [Back](player-v4-blender-back.png) | [Back](player-v4-unity-back.png) |
| [Face detail](player-v4-blender-detail.png) | [Run](player-v4-unity-run.png), [strafe](player-v4-unity-strafe.png) |
| [First-person rest](player-v4-blender-hands.png) | [First-person rest](player-v4-unity-hands.png), [strike](player-v4-unity-strike.png) |
| | [Crouch](player-v4-unity-crouch.png), [landing](player-v4-unity-landing.png) |
| | [Terrain session](player-v4-world.png), [appearance screen](player-v4-appearance.png) |

The verification output also checks both skins on both variants, 60°/100° hand framing, and successive frames through the live graph. Renders were used to correct eye protrusion, hair shape, the trouser seam, detached wrists, pocket/boot details and the upper sleeve in first person.

## Measurements

| Imported geometry | Triangles | Notes |
|---|---:|---|
| Male full body | 31,734 | 40 bones, one material/submesh |
| Female full body | 32,126 | Same rig/material contract |
| Selected first-person arm pair | 10,496 | Compacted derived mesh |
| First-person lower body | 5,686 | Excludes duplicate arms and upper body |
| Combined first-person geometry | 16,182 | Before visibility/shadow passes |

The full male import has 47,667 vertices, including splits required by UVs/normals; the derived arm pair has 11,783. It no longer retains the hidden face/body vertex streams. Counts stay within the revised [35,000/12,000 review ceilings](../CONTENT_PIPELINE.md#7-player-geometry-and-performance-review).

The final [character run](player-v4-avatar-report.json) passes **37 checks**, the [terrain run](player-v4-runtime-report.json) passes **54 checks**, and the [Windows development build](player-v4-build.txt) has zero errors/warnings. The existing domain suite passes 22,371 assertions. [Source checks](player-v4-source-checks.json) pass on both models and all sixteen clips, including loop seams below 0.1 mm and sampled crouch mesh height below 1.30 m.

At deterministic 120 Hz updates, movement blend weight reaches 90% in **83.3 ms** and mining blend weight in **50.0 ms**. These measure animation presentation weights, not end-to-end input latency or the mining impact time. The 30/144 Hz pose comparison differs by less than 1 mm, stopping settles within 300 ms, and the actual crouch eye transition reaches its target within 300 ms. Camera yaw/pitch are still assigned directly from input.

A warm main-thread benchmark of 1,000 paired body/arm `Animate` calls measured **0.049 ms mean / 0.071 ms p95**. It measures graph evaluation and presentation adjustments; GPU skinning, draw submission, terrain and rendering are excluded.

The Windows terrain run used an **Intel(R) Core(TM) i7-10750H CPU @ 2.60GHz**, **NVIDIA GeForce RTX 2060**, 32,553 MB system RAM, 1280×720 and a 90 FPS cap, with view radius 10 and 1,182 peak resident chunks. Its sampled whole-frame times were **11.11 ms median / 11.17 ms p95 / 12.43 ms maximum**; the separate mining sample peaked at **18.03 ms**. Total allocated Unity memory peaked at 200.9 MiB. The draw-call recorder returned unavailable (`-1`); isolated GPU/texture memory and crowd costs were not measured.

## Reproduction

The editable sources remain [ExplorerMale.blend](../../ArtSource/Characters/ExplorerMale.blend) and [ExplorerFemale.blend](../../ArtSource/Characters/ExplorerFemale.blend). Their texture paths are relative. Explicit FBX/PNG exports remain under `Assets/RivetReach/Resources/Characters`; Blender is not a runtime dependency.

Run [create_player_assets.py](../../Tools/create_player_assets.py) through Blender's `--background --python` CLI with an absolute Windows script path. Then run [check_player_assets.py](../../Tools/check_player_assets.py), [render_player_review.py](../../Tools/render_player_review.py) and [render_player_hands.py](../../Tools/render_player_hands.py). The source checks cover normalized weights, one UV/material, portable textures, sampled finite deformation, crouch headroom and loop endpoint continuity. They are not a complete mesh-intersection detector.

[Build-Windows.ps1](../../Tools/Build-Windows.ps1) builds in the existing pinned Editor without replacing its open scene. [Verify-Player.ps1](../../Tools/Verify-Player.ps1) exercises the imported character, and [Verify-POC.ps1](../../Tools/Verify-POC.ps1) exercises the terrain/inventory slice. The current executable is `Builds/PlayerRevision4/RivetReach.exe`; keep its adjacent data/runtime files together. The earlier revision 3 build is preserved separately. Normal play does not enable either verification mode.

## Scope of the evidence

The source and imported poses were inspected alongside the concept. The resulting model uses smoother surfaces and simpler material painting than the illustration's painted facets. The shared 256×256 semantic atlas remains a palette skin contract, not a finished uniquely unwrapped paint template. Facial expressions, secondary ponytail simulation, arbitrary-slope foot placement, multiplayer crowd LODs and new tool/combat systems are outside this revision. The in-place gait does not guarantee zero foot sliding at every gameplay speed.

The checks establish the listed import, response and gameplay properties on the measured machine. They do not prove universal frame rates, final professional artistic quality or user acceptance of likeness and movement feel.
