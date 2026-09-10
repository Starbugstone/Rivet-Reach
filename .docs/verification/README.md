# Current verification evidence

This directory maintains the latest report and useful evidence for each feature. Superseded reports, obsolete recipe screenshots, earlier character models and intermediate failing captures are kept in Git history rather than maintained here. Dates and artifact identities matter: a focused check of one feature does not certify every system or whole-game performance.

| Feature | Current evidence |
| --- | --- |
| Item sidebar, recipe/uses navigation and Ctrl-click placement | [Recipe browser verification](RECIPE_BROWSER_RESULTS.md) |
| Performance and live Editor script reloads | [Performance pass](PERFORMANCE_RESULTS.md) — measured UI/meshing costs, streaming checks and preserved gameplay |
| Stationary terrain/tree shading | [Shadow stability](SHADOW_RESULTS.md) — native AO sampling A/B and duplicate-view control |
| Multiblock tanks and shared pipe channel fittings | [Multiblock verification](MULTIBLOCK_RESULTS.md) |
| Blue Signal, power and the first workshop | [Industry verification](INDUSTRY_RESULTS.md) — screenshots, gameplay clip, machine interfaces and measured limits |
| Pass-through props, camera clearance and Editor icons | [Clearance verification](PROP_CLEARANCE_RESULTS.md) |
| Craftable torches and local lighting | [Torch verification](TORCH_RESULTS.md) |
| Creative sidebar dragging, flight controls and full workshop run | [Creative verification](CREATIVE_RESULTS.md) — current build, Creative industry/tanks and regressions |
| Batteries, pump intake/power, outward controllers, held items and wiki | [Workshop follow-up](WORKSHOP_FOLLOWUP_RESULTS.md) — electrical storage, construction and rendered item checks |
| Starter recipes and block interaction | [Crafting verification](CRAFTING_RESULTS.md) — exact layouts/quantities, placed workbench and 3×3 UI |
| Furnaces, farming, hunger, health and armor | [Survival verification](SURVIVAL_RESULTS.md), including the latest ore and placement regressions |
| Seas, rivers, buckets and fluid physics | [Fluid verification](FLUID_RESULTS.md) — per-fluid renewal, bucket controls, source/flow and session streaming |
| Biomes, caves and streaming | [Terrain verification](TERRAIN_GENERATION_RESULTS.md) |
| Creatures and combat | [Mob verification](MOB_RESULTS.md), plus the current [beetle wall-climbing extension](BEETLE_WALL_CLIMB_RESULTS.md) |
| Sun, moon and time | [Day/night verification](DAY_NIGHT_RESULTS.md) |
| Player model, hands, held equipment and animation | [Avatar rework verification](AVATAR_REWORK_RESULTS.md) |
| Audio and dated Editor startup | [Audio and earlier startup verification](HIFI_PLAYER_AND_AUDIO_RESULTS.md) |
| Mining, placement and pickup effects | [Effects verification](ARCADE_VISUAL_RESULTS.md) |
| Generated trees, felling and placed-wood protection | [Tree verification](TREE_RESULTS.md) |

[Run instructions and controls](../FIRST_POC.md) identify the current review executable. Feature reports preserve their own tested build and workload; older measurements are not relabeled as tests of the latest executable. Artistic acceptance and long-session play balance still need play review. World progress remains session-only.
