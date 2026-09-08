# Trees, generated-wood protection and axe presentation — 2026-09-08

This verifies the user-requested extension to the current terrain slice: seeded log/leaf trees, axe-only upward felling of generated wood, protection for player-placed wood, improved axe appearance/positioning, and 1.7-block automatic pickup. [Gameplay rules](../GAMEPLAY.md#trees-and-axe-felling--user-feedback-extension) and [simulation/provenance](../SIMULATION.md#14-tree-generation-and-event-driven-felling) own the detailed contracts.

## Build and checks

The pinned Unity 6000.4.4f1 / URP project was built in an isolated local checkout under `Logs/TreeSandbox` while preserving the open Editor's Play session. No Editor upgrade, integration install or character-model/animation rewrite was needed. The Windows development build succeeded with **zero errors and zero warnings**. The [domain suite](tree-domain-checks.txt) passed **54,283 assertions**; the [final focused player run](tree-runtime.json) passed **143 runtime assertions** with no logged errors. The [full terrain/inventory regression](tree-full-runtime.json) passed **263 assertions** with no logged errors, including mining/pickup conservation, partial pickup, item stacking/displacement, grass lighting, movement, underfoot placement and appearance checks. Its empty-air fixtures now sit above generated canopies. The final rebuild changes only those verification fixtures; gameplay code and assets match the focused run. The verified player replaces the local `Builds/PlayerRevision4` output.

The domain suite covers deterministic trees at positive/negative coordinates, independent chunk/halo generation on all six faces, clear spawn space, distinct bark/end-grain/leaf tiles, definition-driven axe capability, connected generated branches, stump preservation, blocked felling through leaves or placed wood, queued replacement protection, bounded log work, natural leaf support/decay and persistent player foliage.

The focused runtime suite exercises actual streamed trees, normal held mining input, single-block mining of player-placed base wood with an axe, non-axe/generated-tree behaviour, mixed axe capabilities, physical drop counts, construction touching a tree, replacement of queued logs, pause/unload/resume, origin shifts, and axe framing/imports on both character variants at 60/78/100-degree FOV. Generated and placed provenance is checked independently of rendered geometry.

## Original axe asset and visual review

[The asset script](../../Tools/create_starter_axe.py) produces the editable Blender source, FBX, 128×128 pixel palette and 64×64 transparent icon. The model has **752 triangles, one material and no rig**, with a shaped wooden shaft, leather binding, stepped forged head, cutting bevel and pins. It uses the existing 36 mm tool-grip contract. First-person placement reframes the whole arm and attached tool together toward the lower right; contact remains on the existing socket. Explicit exported blade/shaft markers orient the cutting edge forward around the handle axis at rest and through the swing. Runtime checks verify its forward direction and framing on both model variants at 60/78/100-degree FOV, plus its forward direction across the animated sweep. The existing character animations remain intact.

The project's [equipment concept](../concept-art/player-tools-weapons-v1.png) informed the material/silhouette direction. The user specifically requested Minecraft-inspired positioning; a [published gameplay screenshot](https://www.curseforge.com/minecraft/mc-mods/manabarlib) was inspected for peripheral framing. No Minecraft mesh or texture was incorporated. Source renders and Unity captures are evidence of this revision, with artistic approval remaining with the user.

## Measured recognition and queued work

The existing sparse edit overlay identifies provenance: a log with no edit record is generated; placed/replaced wood has a record. This check runs on a committed cut and queued tree work, without a per-frame tree scan or additional per-tree scene objects. The [simulation contract](../SIMULATION.md#14-tree-generation-and-event-driven-felling) describes the bounded candidate cache and scheduling.

On the i7-10750H / RTX 2060 machine, at 1280×720 and view radius 10, using reproducible world seed `246813` after the random-title startup check:

| Measurement | Final focused run |
| --- | ---: |
| Generated resident log recognition | 0.542 µs/query |
| Player-placed log recognition | 0.379 µs/query |
| Nonresident generated log recognition | 0.738 µs/query |
| Initial generated-tree cut | 4.48 ms |
| Largest tree update, including remeshing | 7.91 ms |

Recognition uses warm repeated reads of one address: 100,000 resident/placed queries and 1,000 nonresident queries, each after 100 warm-up reads. It is not a random-access or cold-cache benchmark. Queue processing is event-driven, capped at 32 log candidates and 8 leaves per 0.1-second step, with local remeshing batched per touched chunk. No absolute fastest-data-structure claim is made.

The [pre-optimization run](tree-before-optimization-runtime.json) exposed a **78.48 ms** maximum tree update. Caching 256 pure tree candidates per thread, reusing leaf-search buffers and reducing redundant/batched leaf work lowered the [first optimized run](tree-optimized-runtime.json) to **5.41 ms** and this final run to **7.91 ms**. These are workload observations, not a guaranteed frame budget. The final whole-run frame median/p95/max were 11.11/11.17/140.30 ms; screenshot capture and streaming are included, so the run does not prove hitch-free play.

## Visual evidence

![Original Blender axe render](tree-axe-source.png)

![Forward-facing axe beside a generated tree](tree-axe-world.png)

![Axe during its mining swing](tree-axe-swing.png)

[The same tree after felling and natural leaf decay](tree-after-cut.png) shows the resulting terrain edit. These are captures of the tested build.

## Scope and limits

Natural and player-placed logs are distinguished through the existing sparse edit records, with no continuously updated tree objects. Replacing a generated log makes that position player-modified. A future save/diff compressor must preserve this provenance even when block IDs match the generator. The current game still stores world progress only for the running session.

Only generated logs participate in upward felling. Placed wood mines individually, including when touching a tree or replacing a queued log. The correction does not reconstruct blocks already destroyed in an earlier session/build. Natural unsupported foliage decays; placed foliage remains. Foliage currently renders opaque and transmits daylight for the simplified grass-light rule. Saplings, regrowth, fruit, full voxel lighting, recipes and axe durability remain outside this change.

Recognition microbenchmarks measure the tested lookup paths, not unlimited-world FPS. Whole-run frame times include streaming and screenshot capture, with the user's Editor remaining open; they are not an isolated release benchmark. CPU/GPU performance on other machines and artistic acceptance remain unproven.
