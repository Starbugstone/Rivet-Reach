# Arcade visuals and dynamic effects

Implemented on 2026-09-09 (Europe/Paris) following the user's request for AAA-style arcade presentation. This report maintains the effects workload. [Terrain](TERRAIN_GENERATION_RESULTS.md) and [day/night](DAY_NIGHT_RESULTS.md) own current landscape/sky evidence; artistic acceptance remains a review decision.

**2026-09-12 follow-up:** the user requested removal of the cyan glow above the punching hand. `ArcadePresentation` no longer creates or updates the first-person swing ribbon, including held-item swings. The [2026-09-12 build](swing-ribbon-removal-build.txt) of `Builds/Performance/RivetReach.exe` passed with zero errors/warnings, and the existing [effects suite](swing-ribbon-removal-runtime-report.json) passed all 32 assertions with no Unity errors. The [actual punching capture](swing-ribbon-removal-punch.png) was inspected and shows no cyan trail. This local build includes concurrent workspace changes; the focused change owns only the ribbon removal. The captures and measurements below describe the original 2026-09-09 treatment and still show that former effect; they are not verification of this removal.

## Actual player views

[Watch the actual Unity action sequence](arcade-action-preview.mp4): mining, block break, placement, collection and a pickaxe swing. The silent 2.5-second clip contains 150 consecutive player captures at a fixed 60 fps simulation step. It is a scripted gameplay fixture rendered by Unity, not a concept render or a real-time performance recording.

| Mining contact and cracks | Accepted block placement |
|---|---|
| ![Mining cracks and a swing ribbon](arcade-mining.png) | ![Placement ring on the new block](arcade-placement.png) |

[Break debris](arcade-break.png), [sustained impact effects](arcade-impact.png) and [effect-intensity settings](arcade-settings.png) provide further actual player views.

## Implemented treatment

- Richer natural terrain with surface normal/roughness detail, warm sun, cool shade, ridge haze and drifting clouds. The additional mineral/resource tiles retain their identities.
- Nearby grass blades bend in the wind and follow exposed natural grass occupancy. The captured observer region contained **156 tufts / 1,872 triangles**, in one mesh.
- Live mining cracks, shaped chips, splinters, leaf fragments, dust and contact glints. Short swing ribbons follow the first-person animation. Committed placement and actual pickup trigger distinct effects.
- Textured world drops and real tool silhouettes, rotating at their physical display position.
- Six reusable particle systems, a **728-particle ceiling**, local burst throttling, floating-origin handling, immediate pause support and a persistent **Effect intensity** setting. Zero suppresses optional bursts and ribbons while mining progress remains available.
- Original Blender sources, explicit FBX imports and original shaders. The added runtime module is the pinned Editor's built-in particle system.

The [content pipeline](../CONTENT_PIPELINE.md#arcade-presentation-and-dynamic-feedback) owns detailed authoring and effect boundaries. The [debris source report](../../ArtSource/Effects/asset-report.json) records **44 stone-chip, 44 wood-splinter and 104 leaf triangles**, with one material each. The [actual Blender source render](arcade-debris-blender.png) was checked separately from the Unity views. No external art or effect pack was imported.

## Verification

The [Windows build](arcade-build.txt) succeeded in Unity **6000.4.4f1**, with **0 errors and 0 warnings**. The final import contains all 18 terrain/resource colour and detail layers and the three Blender debris meshes. New assets have Unity metadata; the source and export are tracked with Git LFS.

The [arcade runtime suite](arcade-runtime-report.json) passed **32 assertions**. It exercises real mining, committed placement, actual collection, burst overload, fixed particle/object bounds, zero intensity, immediate pause and a real 640 m observer move that triggers floating-origin rebasing with live debris. Direct effect calls are checked synchronously for unchanged terrain/item authority, independently of the ordinary grass simulation's later edits. All six effect/render shaders have supported passes. No Unity errors were recorded.

The [full POC regression](arcade-poc-report.json) passed **263 assertions**, covering terrain residency, mining and placement, inventory, both player models and skins, first-person framing, dropped-item collision/merging/collection, held-block contacts, jump placement and sprint gestures. No terrain worker or Unity errors were recorded.

The [ore regression](arcade-ore-report.json) passed **87 assertions**, preserving depth bands, pickaxe mining, raw-resource drops, bedrock and depletion across streaming under the new material treatment.

Measured on an **Intel i7-10750H / RTX 2060**, at **1280×720**, seed **246813**, ten-chunk view radius and the normal **90 fps cap**:

| Workload | Median frame | P95 frame | Maximum frame | Unity allocated memory peak |
|---|---:|---:|---:|---:|
| Four-second sustained action-effect sample | 11.11 ms | 11.70 ms | 18.03 ms | 219.5 MiB |
| Existing POC sampled workload | 11.11 ms | 11.54 ms | 15.81 ms | 248.3 MiB |

The action/capture/overload workload reached **186 live particles**. Performance sampling excludes screenshot encoding and fixture construction. The 90 fps cap sets the approximately 11.11 ms floor; these results do not isolate GPU time or establish uncapped throughput. The draw-call recorder returned unavailable (`-1`), so no measured draw-call claim is made. Zero-valued report fields belonging to other suites are unmeasured. These are short local workload samples, not broad hardware or long-session certification.

## Reproduction and review limits

Use [Build-Windows.ps1](../../Tools/Build-Windows.ps1) to import, prepare and build in Unity 6000.4.4f1. Run [Verify-POC.ps1](../../Tools/Verify-POC.ps1) with `-Arcade` and an explicit output directory to generate the arcade report and motion frames; run without a suite switch for the full POC regression, or with `-Ores` for the mineral regression. The explicit player flags are `-rr-verify -rr-arcade-review`. Ordinary gameplay does not receive test fixtures or automated inputs.

Run [encode_arcade_review.py](../../Tools/encode_arcade_review.py) through Blender 5.2 with the capture directory after `--` to encode the actual screenshot sequence. Blender is an authoring/encoding tool, with no new game runtime dependency. [create_arcade_effect_assets.py](../../Tools/create_arcade_effect_assets.py) rebuilds the editable debris source, FBX and neutral source plate.

The visual review covered the final landscape/sky, cracks, break debris, placement ring, settings and consecutive motion frames. Voxel canopies and repeating terrain swatches remain visible parts of this style. These changes do not establish a production weather/day-night system, physically simulated debris or an accepted full-game art standard. The preceding [character and audio report](HIFI_PLAYER_AND_AUDIO_RESULTS.md) retains its separate measurements and character-art limitations.
