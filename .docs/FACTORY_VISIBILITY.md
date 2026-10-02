# Factory visibility and distant rendering

Working implementation requested on October 2, 2026, following the [64-block visibility review](verification/MACHINE_VISIBILITY_REVIEW.md). Numerical LOD defaults are implementation choices to validate in native stress captures, not user-selected performance guarantees.

## Shared presentation distance

Industrial machines, pipe/cable runs, battery cells, connected tank shells, crates and beds use terrain's current fog end as their outer presentation distance: `ViewDistance × 32 − 16` blocks (304 at the default ten chunks). Only resident, valid state is eligible. Extending visibility never requests terrain tickets or simulates dormant factories; [factory residency](GAMEPLAY.md#factory-residency-and-player-responsibility) remains authoritative.

Detailed views remain near the player. The working transition band is 48–64 blocks, with detailed roots retained through 72 blocks to cover model bounds and the refresh interval. Complementary screen-door coverage blends the detailed and distant geometry over that band; the distant model then blends into ordinary terrain fog. The full three-dimensional distance applies, including elevated viewpoints. The diagnostic old-cutoff control is explicit verification state, never a saved option or ordinary player setting.

## Distant representation

Distant geometry is derived from the game's own imported models without modifying source assets, held artwork or inventory icons. Vertex clustering at an 8 cm grid removes collapsed/duplicate triangles while retaining UV and normal seams. Instanced submissions share each cached shape/material and are frustum-culled before drawing. Creation uses a cooperative one-millisecond budget, at most two mesh preparations per frame; an individual mesh preparation is indivisible and must be measured separately.

Distant models omit labels, decorative moving parts' animation, per-machine lights and shadow casting. Connection masks, disconnected pipe ends, fitted channels, machine orientation, door state and connected tank shell openings still select the corresponding geometry. Detailed controls, lights, motion and item-count labels return nearby. Material variants retain the authored palette and textures; distant shading omits normal and metallic texture sampling.

The distant cache belongs to a session, independent of authoritative inventories or world persistence. Floating-origin shifts invalidate submitted matrices before the next draw. Changed or removed blocks refresh the presentation; previous graph snapshots cannot resurrect replaced machine identities. Save/load builds a new presentation from the restored world.

## Tanks and contents

Connected shells reuse the same face/trim selection as the detailed tank. Liquid visibility uses the bounds of the whole formed tank, with current fill height/type from shared storage. A controller crossing a presentation boundary cannot hide liquid behind a still-visible near wall. Breached tanks retain their existing recovery rules; presentation never forms, repairs, fills or drains them.

## Verification boundary

Native acceptance must include the large working factory, default terrain distance, camera crossings in both directions, elevated views, distant tanks, world unloading/return, exact resource conservation and saved-state restoration. Record frame distributions, all 16.67 ms and 22.22 ms breaches, draw counts, geometry reduction, cache/build costs and hardware/thermal conditions. Screenshots and a camera video are separate from timed measurements; fixed-rate screenshot capture is not a real-time FPS measurement.

[October 2 native results](verification/FACTORY_VISIBILITY_RESULTS.md) record the passing visibility/conservation checks and remaining performance limits, including a 41.677 ms cold mesh-preparation peak and failure of the steady-60-FPS target. The cooperative preparation budget is not a hard per-job time limit.
