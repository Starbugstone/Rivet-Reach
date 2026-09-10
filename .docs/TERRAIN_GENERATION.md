# Terrain, caves and biomes

The user requested a terrain rework with caves, varied landscapes and biomes. The current working profile is generator **`terrain-5-seas-rivers`**, in the existing `surface` world. It replaces the previous height/cave algorithm while preserving the [finite ore profile and bedrock base](ECONOMY.md#current-ore-generation-and-bedrock), integer coordinates, chunk streaming and authoritative session edits. Numerical shapes, frequencies and material choices below are implementation defaults for review, not user-selected tuning or proven gameplay quality.

## Surface profile

| Biome | Landscape | Ground and vegetation |
| --- | --- | --- |
| Grassland | Low rolling hills | Grass over dirt; scattered broadleaf trees |
| Forest | Stronger rolling relief | Grass over dirt; denser broadleaf trees |
| Dunes | Wind-shaped sandy ridges | Sand over sandstone; no generated trees |
| Badlands | Terraced hills and mesas | Red clay over stone; no generated trees |
| Alpine | Tall ridges and steep slopes | Snow above the working snow line, exposed rock and grassy foothills; occasional conical trees on supported grass |
| Sea | Broad low basins with a shared water datum | Sand over sandstone, filled source water |
| River | Meandering low channels through land and into basins | Sand over sandstone, filled source water |

`TerrainProfile` places seeded, perturbed biome sites on a 224-block grid. Site offsets stay within 0.3–0.7 of their cells, guaranteeing coverage by the one-cell influence radius. Several nearby sites contribute smoothly weighted heights; the strongest site determines the surface material and tree profile. Low-frequency coordinate warping breaks up regular boundaries. Rolling, ridge, dune and terrace functions give the regions distinct shapes. Materials change at block boundaries while blended heights avoid vertical walls caused solely by biome selection. Regions may repeat or blend; these are not climate or watershed simulations.

The initial spawn blends smoothly toward temperate grassland within 96 blocks. Near-surface cave carving and vegetation exclude the player start volume. This gives every seed supported spawn terrain without an artificial platform or discovery-order repair. Broadleaf tree density depends on biome; alpine trees use the existing log/leaf IDs with a taller conical canopy. Candidates require solid grass support and the existing slope clearance. Axe felling and placed-wood provenance remain governed by [GAMEPLAY.md](GAMEPLAY.md#trees-and-axe-felling--user-feedback-extension).

The coordinated survival integration supplies sparse mature wild potatoes on supported, open grass outside the start volume. A candidate occupies one seeded location within an eight-block grid cell, with a 12% cell chance before support/clearance rejection. These use survival's crop ID 36; harvesting, yields and growth remain owned by the survival system. They cannot replace trunks, leaves or ground. Both point reads and chunk workers use the same candidate rule.

## Seas and rivers

Generator `terrain-5-seas-rivers` adds low-frequency seeded ocean basins and a continuous warped noise contour for meandering river channels. The terrain is carved before surface materials, caves, ores and vegetation are evaluated. Rivers lower terrain toward a shared **Y=32 water-cell datum**, with sloped banks rather than abruptly blending a mountain down over a narrow band. Seas deepen broad basins. The dry temperate spawn blend remains in force.

Columns below the datum receive source water from one cell above their seabed through Y=32. Sand/sandstone beds suppress cave carving through their upper four-block cover so generated seas do not immediately pour into every cave. Deeper caves and all ore bands remain unchanged. Surface residency bounds include the water level even where the bed lies in a lower chunk. Point reads and worker pages use identical rules and signed coordinates. Biome ID `Sea` or `River` labels wet columns; the five existing land biomes remain available. These are procedural basins and channels, not a watershed or rainfall simulation.

## Underground

Two intersecting 3D density fields form winding passages; a separate room field and detail mask add larger caverns. Fields are sampled on a globally aligned eight-block lattice and interpolated identically for point reads and chunk generation. This supports cross-chunk cavities without maintaining tunnel objects or a discovery-order random stream.

Carving narrows in the upper twelve blocks of cover. A separate regional entrance mask allows some passages and caverns to intersect natural hillsides and the surface. Entrances, blind branches, chambers and steep drops are generated geometry. The current profile does not guarantee that every chamber is reachable from the surface or that every entrance is safe to descend without mining/building.

Bedrock remains continuous at **Y = −256**. Carving stops through the three blocks above it, leaving an uncarved rock buffer. Eligible stone in that buffer can still contain ore and can be mined; bedrock itself remains unbreakable. Ore placement runs after carving and replaces only stone; soil, snow, sand, clay, cave air and bedrock cannot become ore. Ore Y ranges and relative rarity are unchanged. More exposed cave walls change discovery opportunities, so resource availability still needs playtesting.

## Data, streaming and compatibility

`TerrainGenerator.Generate` builds a 32³ chunk and its one-cell halo from immutable seed/profile data, then applies ores and vegetation. `At` uses the same surface, interpolated cave, ore and vegetation rules. Worker and point-query caches include the seed and signed coordinates, and overwrite colliding entries. Per thread, the column cache has 4,096 entries, the density cache 4,096 and the tree cache 256; none grow with exploration. Each generated page can allocate one 7³ immutable density snapshot, reused in its inner loop.

Each worker also returns the minimum surface height and a conservative maximum surface-plus-canopy bound across all 34 × 34 sampled columns. `VoxelWorld` uses these bounds to load the complete nearby surface band, including mountain slopes and treetops across vertical chunks. It also loads a bounded vertical view volume around the player, narrowing with horizontal distance and capped above by each column's surface/canopy bound. This keeps large cavern floors and ceilings resident beyond the original three-chunk player band, including when looking down through an entrance. Surface coverage outside that local volume remains separate; distant gaps between a deep player and high surface need not be filled. The surface-range cache is pruned with horizontal residency; immutable edit snapshots, residency tokens, revision checks and closed unready frontiers remain in force.

The version change deliberately changes regenerated terrain for a given seed. Current progress is session-only, so there is no on-disk world migration in this change. Durable worlds must retain the [generator-version compatibility policy](SIMULATION.md#5-coordinates-and-generation-compatibility). The subsequently authorized liquid extension adds seas, rivers and [world-fluid simulation](FLUIDS.md). Generated buildings, structures and new planets remain outside this extension.

## Materials and authoring

| Block | Stable ID | Runtime ID | Texture layer |
| --- | --- | --- | --- |
| Sand | `rivet:sand` | 90 | 40 |
| Sandstone | `rivet:sandstone` | 91 | 41 |
| Snow | `rivet:snow` | 92 | 42 |
| Red clay | `rivet:red_clay` | 93 | 43 |

All four use the existing item definition, mining/drop, placement, inventory and session-edit paths. They drop their corresponding block and are placeable. The existing voxel system has no sand gravity or snow melting. The material IDs and texture layers are separate identities; lower texture layers remain available to the existing terrain, minerals and coordinated survival content.

`BiomeTerrainAssets.Items` appends missing definitions before registry access. `BiomeTerrainAssets.Tiles` writes original repeating 64² swatches into an array of at least 44 layers. The shared `ArcadeTerrainArt` pass retains their colors and derives matching normal/roughness layers. Terrain, held blocks, drops and inventory therefore share their material identity. No third-party source, texture, package or asset is added.

## Verification and replay

- `Tools/Check-Terrain.ps1` compiles the actual generator source with the pinned Editor's Mono and runs the focused checks without launching Unity. Its report is `Logs/terrain-generation-checks.txt`.
- With the pinned Unity editor open and shared work coordinated, `Tools/Verify-Terrain.ps1 -Build` requests asset preparation, focused terrain checks and a Windows Development build at `Builds/Terrain/RivetReach.exe`, then runs `-rr-terrain-review`. Omit `-Build` to replay that artifact.
- The runtime review visits naturally generated biome and cave sites for seed 246813, captures ground and elevated survey views, checks full surface residency and cave collision, mines/places the four biome blocks and tests edits across unloading/origin shifts. Teleports and survey cameras belong only to explicit verification mode.

Measured results and limitations belong in [terrain verification](verification/TERRAIN_GENERATION_RESULTS.md). Biome scale, height variety, cave density, entrance safety and navigation remain user-review questions. This request does not automatically select another roadmap stage.
