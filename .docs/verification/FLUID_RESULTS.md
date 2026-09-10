# Fluid increment verification — 2026-09-10

This report covers `Builds/Fluids/RivetReach.exe`, built with Unity **6000.4.4f1 / URP 17.4.0**, using generator `terrain-5-seas-rivers`. [Fluid rules](../FLUIDS.md) own behaviour and exclusions. Earlier terrain, survival, crafting and character reports retain their own artifact identities.

## Checks

The final domain run passed **92,296 assertions**, plus **49 focused fluid checks** and **1,770 independent recipe assertions**. The Windows build reported zero errors and zero warnings. The final Windows player review passed **36 assertions**.

- [Domain regression report](fluids/domain-checks.txt): coordinates, immutable worker generation/halos, greedy terrain meshes, inventory conservation, grass/trees, ores, bedrock and the expanded sea/river terrain profile. The terrain suite retains all five land biomes and verifies every new wet biome, dry supported spawn and wild-food hooks.
- [Fluid checks](fluids/fluid-checks.txt): source reach, attenuation, settled sleeping, source removal/recession, supported renewal, unsupported nonrenewal, a second fluid with renewal disabled and independent reach/tick delay, downward flow, landing reach reset, drop routing, dams, negative coordinates, closed unready frontiers, chunk resume, bucket transactions and exposed mesh faces. The second fluid is a test definition; this does not implement lava.
- [Starter recipes](fluids/starter-recipe-checks.txt): 1,770 independent acceptance assertions for all **53** layouts/outputs, including the new three-iron bucket. Existing catalog-driven crafting and survival checks also ran through the domain build path.
- [Runtime report](fluids/runtime-report.json): actual Windows player, generated sea/river water, source targeting through flow, a full-inventory bucket exchange, mouse Use placement, recession, supported two-source renewal, immersion/swimming controls, item currents/buoyancy and source edits across unloading and a 1,600-block origin shift.

[Build context](fluids/build-context.json), [build summary](fluids/build-summary.txt) and [fluid state](fluids/fluid-state.txt) identify the artifact and terminal workload. The review starts empty-handed and introduces fixtures only inside explicit `-rr-fluid-review` mode. It disables mobs for the focused water scenario.

The settled-view sample measured a median **11.112 ms**, p95 **11.130 ms**, and maximum **12.611 ms** per frame under the scope described below.

## Visual evidence

The following are actual player captures, inspected after the review:

- [Sea](fluids/fluid-sea.png)
- [River channel](fluids/fluid-river.png)
- [Seven-cell source spread](fluids/fluid-seven-cell-flow.png)
- [Renewable pool and bucket hotbar](fluids/fluid-renewable-pool.png)

Water surfaces appear with their own transparent material; the terrain underneath remains visible. The spread fixture shows decreasing fluid levels and the bounded footprint. No third-party water texture, shader or model was introduced.

## Limits and replay

Run `Tools/Verify-Fluids.ps1 -Build` with the pinned Editor available, or omit `-Build` to replay the existing artifact. The script preserves a pending build owned by another task and uses the existing Editor request path. The full regression and focused fluid suite run before building.

Frame samples in the runtime report cover two settled two-second sea/river views at 1280×720 and a four-chunk view radius on the recorded i7-10750H / RTX 2060 machine, with a 90 FPS application cap. They exclude startup, fixture construction and the whole active-fluid stress envelope. Zero-valued unrelated fields in the shared report schema were not measured; draw-call peak `-1` means unavailable. These are focused observations, not a performance or gameplay-quality certification.

Still needed: broad seed/play review of river composition, coastlines, swimming feel and transparent-water appearance; sustained large edits and extended fluid-frontier loading. Source/flow meshes currently step between heights. Crops/stations block water, and there is no waterlogging, irrigation, drowning, fluid mixing, lava gameplay or industrial fluid transport. Progress remains session-only, with no durable save claim.
