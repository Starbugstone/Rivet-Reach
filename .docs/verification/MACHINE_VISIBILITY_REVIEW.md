# Machine visibility distance review — 2026-10-02

> Follow-up: the user authorized implementation and stress testing of [distant factory visibility](../FACTORY_VISIBILITY.md). The rules below record the pre-change source review.

The user's recollection matches the current source and the September factory captures: most industrial models stop being shown at **64 blocks**, even when their terrain remains visible much farther away. This is a presentation cutoff, not evidence that the machine was deleted. This review changes no visibility or simulation behavior.

## Current rules

| Presentation | Current distance / update rule |
| --- | --- |
| Ordinary machines, industrial pipes/cables, batteries and industrial fittings | `IndustryPresentation.ViewRangeSquared` uses 64²; distance at or above 64 is excluded. Membership is refreshed every 0.25 seconds. |
| Workshop Lamp model | Exception: visible to `VoxelWorld.FogEnd`, which is 304 blocks at the default radius ten. Actual lamp illumination has its own range and nearest-eight selection; a visible lamp does not imply illumination over that whole distance. |
| Multiblock tank shell parts | Separate presentation uses 64², excluding distances greater than 64. Filled-liquid presentation also requires the controller to be among the visible parts. |
| Bulk Crates and Crate Controllers | Separate 64² cutoff, checked every 0.25 seconds. |
| Beds | Separate 64² cutoff, checked every 0.25 seconds. |
| Terrain / Settings view distance | Default radius ten, adjustable from four to fourteen chunks. Fog ends at `radius × 32 − 16`: 304 by default, 112–432 across that slider. This slider does not change the fixed machine/crate/tank cutoff. |

Source: [industrial selection and view cache](../../Assets/RivetReach/Code/Industry/IndustryPresentation.cs), [tank presentation](../../Assets/RivetReach/Code/Multiblocks/MultiblockPresentation.cs), [crate presentation](../../Assets/RivetReach/Code/Crates/CratePresentation.cs), [bed presentation](../../Assets/RivetReach/Code/Beds/BedPresentation.cs), [world view/fog settings](../../Assets/RivetReach/Code/World/VoxelWorld.cs), [Settings slider](../../Assets/RivetReach/Code/UI/GameUI.cs).

Distance is full 3D distance from the player transform to the machine's cell origin, rather than horizontal distance or distance to its nearest visible surface. Height therefore counts: a machine 48 blocks horizontally and 48 blocks vertically away is already about 67.9 blocks away and is excluded. Camera gaze is not the cutoff input. The tested comparison has no near-camera exclusion: this explains disappearance at a short *draw distance*, not a newly reproduced defect in which touching a machine makes it vanish.

There is no fade or distant substitute at this boundary. `HideView` disables the model root and retains it in a bounded cache. Returning objects can reuse their own cached view; new models are created nearest-first, with at most 12 attempts per frame and a 1 ms cooperative budget (the first attempt is allowed). Thus crossing the radius can cause an abrupt disappearance, a refresh delay of up to roughly 0.25 seconds, and additional build-queue delay on return. That is a source-derived explanation, not a measured transition-latency result from this review.

## Rendering versus factory operation

The presentation code does not remove the authoritative machine or its contents when hiding a view. A resident eligible factory can continue operating outside the 64-block visual radius. Conversely, a machine in unavailable terrain is excluded even if its distance is small. Actual production still requires the player-provided [chunk-loader coverage](../BRIDGES.md#chunk-loaders) and fully resident valid routes described by [factory residency](../GAMEPLAY.md#factory-residency-and-player-responsibility). Extending model visibility must not grant implicit residency or offline production.

The [September 20 review](RELEASE_REVIEW_RESULTS.md#visuals-documentation-and-remaining-acceptance) and [September 27 readiness audit](RELEASE_READINESS_0_1_0.md#frame-integrity-what-has-and-has-not-been-demonstrated) already record the visible 64-block boundary in the [factory gallery](../wiki/Release-review-gallery.md). Those are dated native captures, not a fresh October 2 moving-camera reproduction. The latter audit explicitly leaves reviewing the transition in motion outstanding.

## Proposed next fix

Make model visibility coherent with terrain visibility using a shared presentation-distance policy, with a cheaper distant representation for machinery/pipes/tanks and a controlled transition from detailed models. Keep state, collision, connections and loader rules authoritative and unchanged. Check vertical viewpoints and a tank whose controller crosses the boundary before its near wall.

A simple increase of 64 to the default 304 would expose substantially more geometry. The [factory isolation measurements](FACTORY_ISOLATION_RESULTS.md#graphics-findings) found a large rendering cost associated with machine/pipe renderers, and [steady 60 FPS readiness](RELEASE_READINESS_0_1_0.md) still fails. Profile the distant representation, shadow cost, draw calls and transition/frame-time distributions in a busy factory before choosing a default. This proposal is the next scoped visual/performance task, not an implemented fix or a performance claim.
