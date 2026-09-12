# Rivet Reach - Content and Asset Workflow

> **Status:** working specification, 2026-09-08. The first POC now supplies the initial runtime kit; detailed production rules remain working specifications. See [current player evidence](verification/AVATAR_REWORK_RESULTS.md) for actual assets, skin layout and measured geometry.

## 1. Scale and source ownership

Use 1 metre per voxel and model modular assets against that grid. Blender working scenes use metric units at unit scale 1. Apply intended object scale before export. Export presets convert axes consistently; do not fix every imported machine with a different corrective rotation.

A block-sized machine's anchor is its lower grid corner, with its footprint explicitly recorded in the content definition. Reusable attachments use a named connector pivot. Visual meshes can extend within their declared bounds but cannot invent solid occupied cells absent from gameplay data. Decorative animation must not move authoritative ports or collision unexpectedly.

Source layout: `ArtSource/` for editable Blender/image/audio originals; `Assets/RivetReach/` for imported runtime content; `Packages/` for pinned dependencies; `ProjectSettings/` for Unity settings. Specifications remain in `.docs/`. The first POC uses this layout for its original character sources and runtime exports.

Keep `.blend` source separate from Unity's imported runtime hierarchy. Publish explicit interchange exports with documented settings so opening a project does not depend on every contributor's local Blender executable or personal defaults. FBX is the initial mesh/animation interchange choice; texture outputs use lossless source images and Unity import compression settings selected per use.

## 2. First visual kit

The versioned [concept-art gallery](concept-art/README.md) contains the retained male/female turnaround and equipment sheets with generation provenance; current Unity evidence replaces superseded world/UI mockups. Images guide asset work; the gameplay/terrain rules and milestone boundary remain authoritative.

The locked first step requires a cohesive **terrain/player/inventory visual concept**, as scoped in [DEVELOPMENT_STRATEGY.md](DEVELOPMENT_STRATEGY.md#6-locked-first-step-and-provisional-later-sequence). Its kit contains a small natural terrain palette, a 3D player body and first-person fists, basic idle/movement/mining animations, terrain-item icons, selection/mining feedback, hotbar/inventory UI, personal/workbench crafting, station interfaces and survival HUD. Use a consistent lighting reference. The subsequent tree/axe request adds original bark, end-grain and leaf tiles, an axe icon and an original Blender-authored axe mesh using the existing tool socket/grip. The selected survival and creature increments add workbench/furnace/chest tiles, crops, original item icons and native creature models. Structure and industrial machine kits remain later scope.

The initial player must be an inspectable 3D model, not only an invisible collision capsule or disconnected floating hand placeholders. A reusable simple rig and restrained animation set are sufficient for concept review. Check first-person clipping, fist reach/readability, body proportions and contact with the terrain; graphical pose must remain independent of authoritative movement/mining. Changeable skins are now required by the user; body-shape customization, combat animation and a production-wide character catalogue remain later decisions.

The current slice extends the same visual language to tools, chest/workbench/furnace and creatures. Later selected work can add boiler-engine, alternator, crusher and pipes/cables. A block is recognizable from silhouette/texture at normal targeting distance. Later port functions use shape, icon and direction alongside colour.

The first terrain kit began at 32 texels per block face. The subsequent visual polish uses 64 texels with mipmaps and anisotropic filtering, compared against the earlier 32-texel captures; this remains a working art choice rather than a final platform-wide density rule. Machines can use more detail near controls, but do not mix unrelated density/material styles by accident. Record selected palette, roughness ranges, edge treatment and lighting reference in the first kit review. Steam devices emphasize readable moving parts and gauges; Gatebuilder devices keep a distinct shape language.

Start with opaque/cutout materials and a small shared material set. Water/transparent geometry uses its own path. Texture arrays/atlases are implementation choices validated with the mesher; faces with different light/material/UV requirements cannot be merged merely because both are solid.

Initial working budgets, to validate: ordinary machine under 5,000 visible triangles at close range, small creature under 10,000, shared texture sheets where practical, simplified distant presentation and no per-pipe authoritative moving mesh requirement. These are review triggers, not reasons to remove necessary visual feedback. Profile the whole representative scene, including shadows, animation and overdraw, before adopting final asset budgets.

## 3. Import and content definition contract

An asset definition records stable content ID, display/localization keys, occupied cells, model reference, rotations, ports, collision category, item/drop rules and any animation/state mappings. Runtime numerical IDs are assigned from a saved registry mapping; renaming a file does not change saved identity.

Imported models have consistent units, axis orientation, pivots, material slots and animation names. Do not derive machine logic from arbitrary child-object order. Port markers may be authoring helpers; the validated definition supplies authoritative coordinates and compatibility.

State presentation supports off/ready/running/blocked with reason-specific feedback. A missing texture/model uses a visible development placeholder without deleting the underlying saved content. Missing content definitions in a release save produce a compatibility error/recovery route, not silent substitution of stone.

## 4. Binary assets and reproducibility

When binaries are first introduced, configure Git LFS for editable `.blend` files and large art/audio sources before committing them. Keep scripts, definitions and Unity `.meta` files in ordinary Git. Ignore generated caches/build products such as `Library/`, `Temp/` and machine-local logs; never ignore `.meta` files belonging to tracked assets. Verify the chosen remote's LFS availability before relying on it; exact storage quota and team accounts are external facts to establish at that time.

Pin Unity and package versions in project metadata. A fresh checkout must import using the chosen Editor with no undocumented manual scene repair. Keep credentials, local executable paths and editor state out of shared project settings. Record third-party asset source/license/attribution with the asset; do not assume an integration-generated file establishes redistribution rights.

## 5. Acceptance and tool readiness

For the first-step player, round-trip Blender source -> export -> Unity import; verify scale against the 1 m terrain grid, orientation, rig/animation, first-person visibility and material appearance. Change the source and repeat import without losing asset identity. Review terrain, player, lighting and inventory together in a built player and verify a fresh checkout imports the kit. Record screenshots and in-motion observations, the selected visual direction and remaining issues; an isolated concept image does not establish in-game readability. The exact palette/model style is deliberately chosen during this step, not claimed finalized by this specification.

When machines are selected later, apply the same round-trip checks to grid fit, rotations, port alignment, collision bounds, material appearance and state animation.

Unity/Blender startup uses the local skill linked from `AGENTS.md`. A working executable, an open editor and a responsive integration are separate checks. Live integrations can improve iteration but are not runtime dependencies of the shipped game. Tool installation and project creation happen only as part of their authorized tasks.

## 6. Player skin authoring contract

Player-facing behaviour is owned by [GAMEPLAY.md](GAMEPLAY.md#15-player-skins). Build male and female player models around a compatible reusable rig and one documented skin texture layout. Model selection changes the visual body; texture selection changes its skin. Both variants must support the same skin regions and gameplay dimensions. Verify animation deformation and UV stretching on both. Skin colours, face details, painted hair and clothing patterns must be replaceable without editing the mesh, animation or gameplay definitions. Separate left/right limb regions so asymmetric designs work. First-person arms/hands use the same skin regions as the body, even if presentation uses a separate mesh.

The generated explorer-builder concept is one proposed appearance, not a locked identity or mandatory teal waistcoat for all players. Revisit its sculpted hair, raised clothing and pouch details before modeling: texture replacement cannot remove a protruding silhouette. Keep the base silhouette sufficiently neutral for different painted outfits; additional hairstyles, garment meshes and accessories would be separate customization features.

The user's accepted female direction uses longer hair and a modestly different figure while retaining practical clothing and a capable stance. Use the current low-ponytail concept as the visual reference; avoid exaggerated proportions or stereotyped costume differences. The male stays on the left and female on the right of comparison sheets, with matching front/back coverage and equal scale. Longer hair is actual silhouette geometry in the female model, not something a texture skin alone can add/remove; include it in the character geometry budget and check back/shoulder clipping during animation.

Provide an original Rivet Reach skin template and preview it on the model before fixing its layout. PNG is the working interchange proposal; exact resolution, UV layout, filtering and optional overlay-layer support remain to be selected through the player visual review. Familiar skin customization does not imply compatibility with Minecraft skin files. Do not advertise direct compatibility without a separately selected and tested mapping.

The initial two test skins must differ in face/skin colours and clothing and be tested on both male and female models so body/first-person mismatches are easy to detect. Verify seams, left/right orientation, hand detail, bending at joints and unchanged collision/mining behaviour. If custom-file import is selected for the first slice, reject invalid formats/dimensions or excessive decoded sizes and preserve the last valid selection. Base-body transparency must not make the player disappear; optional transparent overlays require their own explicit format rule.


## 7. Player geometry and performance review

The user likes the stylized player concept and requires male/female choices with changeable skins, while emphasizing smooth performance. Preserve the broad angular shapes and practical clothing direction. The generated illustrations show an art direction only: painted facets do not establish a mesh's triangle count, material cost or measured performance.

The user explicitly authorized higher player polygon counts for closer concept likeness and smoother presentation on 2026-09-08. The current working review ceilings are **90,000 triangles per full body** including hair/clothing, **34,000 per dominant first-person arm** and **68,000 per derived first-person arm pair**, following the user’s 2026-09-10 avatar/hand rework request. These revised numbers are implementation review triggers, not user-selected limits or hardware guarantees. Allocate geometry to facial form, shaped hair, garment construction and joint deformation; keep the shared semantic skin layout, one material/submesh and at most four bone influences. The high-fidelity pass uses 1024×1024 base-colour atlases plus shared normal and packed metal/roughness/skin maps. Compact derived first-person vertex streams as well as triangle indices. The later hand-interaction revision renders the dominant right arm for ordinary fist/block interactions and enables a support arm for two-hand grips; record both costs separately. Record actual imported counts and measured animation/scene costs in the [current avatar report](verification/AVATAR_REWORK_RESULTS.md).

Only the chosen model is rendered/animated for a player; the unused male/female variant must not run a second hidden animator. Keep first-person visibility masks consistent so the same arms are not rendered twice in the camera. Retain the torso and jacket when looking down; hide the head/neck and duplicate arms only. Check that the neck opening stays outside the camera while standing/crouching at 60–100° FOV. The complete selected mesh may supply a shadow-only view on the existing bones; record its extra skinning/shadow-pass cost separately from visible body/arm triangles. Count the total geometry actually rendered when the body is visible alongside separate first-person hands. Shared rig conventions do not remove the need to test each model's shoulders, elbows, hands, hips and knees during mining/movement.

Record imported triangles, bones, material/submesh counts, texture memory, draw calls and CPU/GPU frame cost in a representative Windows player build while terrain streams. Compare the two models and both test skins under the same workload. A later multiplayer crowd test is separate evidence; meeting a character triangle budget alone does not prove smooth terrain, animation or whole-game performance. Reduce distant character detail and animation work when that later workload requires it.

## Blender reference-to-runtime review

Use [the Blender game-art skill](skills/blender-game-art/SKILL.md) for character revisions and inspect actual front/back source renders, then the Unity appearance and first-person views. The current rig retains 48 deformation bones, two nondeforming sockets and the one-material/semantic-skin contract. [AVATAR_REWORK.md](AVATAR_REWORK.md) owns the current 50-action library, tool-specific timing, fitted fingerless gloves, grip contacts, equip transition, held equipment and reproducible source/export workflow. Its [verification](verification/AVATAR_REWORK_RESULTS.md) separates measured geometry and behavior from artistic acceptance. Hands and held items continue to share a near depth interval and preserve mutual occlusion. Appearance remains independent of gameplay state.

## Starter axe asset and first-person framing

The user's tree/axe feedback requested better axe graphics and Minecraft-inspired screen positioning. `Tools/create_starter_axe.py` authors the original shaped octagonal shaft, stepped forged head, cutting bevel, leather binding and pins in `ArtSource/Tools/StarterAxe.blend`, exporting `Resources/Tools/StarterAxe.fbx`, a 128×128 pixel palette and a transparent inventory icon. The source has 752 triangles and one material. No third-party mesh or texture is included. Model/animation character sources are unchanged.

The grip origin and 36 mm handle follow the ToolSocket contact contract. Blade-forward and handle-up markers survive import; presentation uses them to keep the cutting edge facing the strike direction while the shaft stays in the grip. The 2026-09-10 [avatar rework](AVATAR_REWORK.md) replaces the former special 22-degree first-person roll and translation with an authored axe pose and trajectory. [Tree results](verification/TREE_RESULTS.md) retain the dated tree/felling evidence; [current avatar results](verification/AVATAR_REWORK_RESULTS.md) own the revised held presentation.

## High-fidelity hand and sound authoring

The user’s subsequent high-fidelity request makes continuous skin across the visible forearm, wrist, palm, finger roots and thumb root an explicit visual target. `Tools/refine_player_surfaces.py` unions the original anatomical surfaces, constructs a quad cage, subdivides it and transfers/relaxes skin weights. Each skin component is checked as a closed manifold before it rejoins the body. `Tools/polish_player_rig.py` adds a third joint to each finger (the thumbs retain two), shallow dorsal details and separately skinned nail plates. The nail plates intentionally remain separate surfaces; skin joints must remain connected. The complete baseline generator now invokes both stages. Blender scenes remain editable, and Unity uses explicit FBX exports with existing GUIDs.

`Tools/create_skin_materials.py` preserves the 4×4 semantic regions at 1024×1024. Hands and the face/neck use continuous cylindrical coordinates across the anatomical surface; the clothing retains its existing palette mapping. Small inset patches in the hand regions colour the nails. The shared surface map stores metallic in R, roughness in G and skin wrap weight in B; the normal map is imported as a normal texture and applied to the continuously mapped hands. Both maps are linear data. `ExplorerSkin.shader` uses URP lighting for the body and first-person arms, with the existing near-depth convention shared with held items. The subtle skin wrap is an approximation, not a claim of subsurface scattering. This remains a palette skin system rather than a uniquely unwrapped production character texture.

The 2026-09-10 [glove extension](AVATAR_REWORK.md#skin-and-cost-contract) reserves the right edge of each hand region for recolourable leather without changing the existing skin and nail coordinates.

`Tools/create_sound_assets.py` creates original deterministic layered foley at 48 kHz / 24-bit PCM, without external samples. `ArtSource/Audio/sound-manifest.json` records every source file and signal measurements. Short effects import as PCM; the stereo canopy/wind ambience streams as Vorbis. Footsteps follow travelled distance and terrain material, impacts use the selected target material, and swings, equipment changes, collection and landings have separate layers. Twelve reusable voices bound effect playback; environmental shelter muffles and reduces the wind. A persistent master control is exposed in Settings. No dialogue, music or real recorded foley is claimed.

See [avatar verification](verification/AVATAR_REWORK_RESULTS.md) and [audio verification](verification/HIFI_PLAYER_AND_AUDIO_RESULTS.md) for their dated evidence and remaining review. Triangle limits are working review triggers selected for this pass; they do not establish GPU cost or artistic acceptance.


## Ore and bedrock material kit

`TerrainTiles.Build` extends the existing original 64×64 texture array with iron/copper/coal/gold/diamond inclusions, a dark fractured bedrock swatch and five raw-resource swatches. Existing layers 0–6 keep their identities; ore layers are 7–11, bedrock 12 and raw resources 13–17. Stable runtime IDs remain separate from these texture indices. Source generation is repository-authored C#; no external textures or models are introduced. Inventory draws faceted resource silhouettes from those swatches; held raw resources use the existing small-block presentation, and physical drops use the textured voxel/tool display described below. [Ore results](verification/SURVIVAL_RESULTS.md) records the actual Unity views. Visual distinction and mining feel remain subject to user review.

## Arcade presentation and dynamic feedback

The user's subsequent request selects a polished, strongly arcade visual direction for the current playable slice. The working treatment uses warm sunlight against cool shaded terrain, a brighter green/ochre/blue-grey material palette, soft layered clouds, atmospheric ridge separation and short, readable action effects. These are implemented art choices for review; the request does not establish that the result has passed a studio-level artistic acceptance gate.

`ArcadeTerrainArt.Apply` runs after the existing tile generator. It repaints the seven natural terrain/tree layers at 64×64, preserves the additional mineral/resource layers, and creates matching linear tangent-normal/roughness layers in `BlockDetail`. The same tile identities feed terrain, held blocks, world drops and inventory icons. Terrain keeps greedy chunk meshes. Leaf light variation and broad moving cloud shade use coordinates periodic with the floating-origin representation; they animate shading while the authoritative voxel boundary stays fixed. The sky uses a paused presentation clock for drifting layered cloud shapes and a warm sun disc. The shared URP profile supplies colour grading and restrained bloom.

`ArcadeGrass` builds one bounded decorative mesh from exposed natural grass cells within 16 m of the observer. At most 289 candidate tufts contain four three-triangle blades each (3,468 triangles before the distance and density filters). It rechecks real voxel occupancy after nearby edits, rebuilds after movement, and fades between 12 and 16 m. Wind bends the tips in the shader; the mesh adds no collider or per-tuft GameObject. The implementation reuses mesh buffers and follows origin shifts. Actual render cost remains part of the scene measurements.

`ArcadePresentation` consumes committed block-break events, accepted placements and actual pickups. Mining progress drives surface cracks; the authored swing phase drives contact chips and a short first-person ribbon. Stone/soil chips, wood splinters, curled leaves, soft dust, glints and rings have six reusable particle systems capped at 728 particles, with at most eight action bursts per frame inside 24 m. Particle collision is disabled. Cosmetic overload may omit effects, never world edits or item quantities. Active particles follow floating-origin shifts and are cleared between world sessions. Pause freezes simulation and the presentation clock. Settings exposes persistent **Effect intensity**; zero suppresses optional bursts/trails while mining progress remains readable.

`Tools/create_arcade_effect_assets.py` authors the original three-mesh debris kit in Blender; editable sources are under `ArtSource/Effects`, and explicit FBX imports are under `Resources/Effects`. The runtime normalizes each mesh for particle-size control. Drops render the actual terrain tile or tool silhouette and rotate at their authoritative display position; rotation does not drive item physics, pickup, merging or expiry. `com.unity.modules.particlesystem` 1.0.0 is the pinned Editor's built-in module, recorded in [third-party notices](THIRD_PARTY_NOTICES.md).

Actual images, motion evidence and measured limits belong in [arcade visual results](verification/ARCADE_VISUAL_RESULTS.md). The visual-only sequence encoder uses Blender's bundled video encoder; it does not add a game runtime dependency. Existing character model, skin and animation contracts remain authoritative above.


## Native creature assets

The subsequently authorized Rustback beetle and Dusk prowler extend the natural stylized kit. [MOBS.md](MOBS.md#source-and-authoring) owns their reproducible Blender sources, shared palette, nine-bone rigs, four actions, explicit FBX import and one-material contract. Compare the actual source front/back renders with Unity review captures; the selected species direction does not establish final artistic acceptance.

## Terrain and biome rework — 2026-09-09

[Biome terrain materials](TERRAIN_GENERATION.md#materials-and-authoring) add original sand, sandstone, snow and red-clay swatches through `BiomeTerrainAssets`, at layers 40–43 of the shared 64² array. The builder retains other owners’ layers; the arcade detail pass matches the expanded array depth. IDs and helper contracts belong to the terrain specification. No external art or dependency was introduced.


## Torch presentation

The [torch extension](GAMEPLAY.md#torches) uses original procedural geometry in `TorchPresentation` (handle, binding and block-shaped flame) and a generated transparent silhouette in `SurvivalItemArt`. Held and dropped displays reuse the existing item-card path. These are repository-authored visuals with no imported third-party art or character-source revisions. The flame uses unlit HDR color; the environment receives separate shadowed point lights through URP's clustered additional-light loop in the terrain shader. The player skin shader compiles the same additional-light variants.

`Resources/TorchLight.prefab` is the authored light template, including the URP low shadow-resolution tier and per-light shadow bias. `TorchAssets.Prepare` creates only a missing initial prefab and preserves subsequent authored tuning. The session instantiates a fixed light pool separately from attachment records. [Verification](verification/TORCH_RESULTS.md) contains actual Unity lit/unlit captures; visual tuning and broader scene cost remain subject to review.

## Authorized industrial extension — 2026-09-10

The user selected implementation of [GitHub issue #2](https://github.com/Starbugstone/Rivet-Reach/issues/2), including original Blender machines, animated operating states, matching interfaces and stability/performance checks. [INDUSTRY.md](INDUSTRY.md) owns the current Azure/Copper unlock, 4×4 Machinist’s Bench, separate signal/power/item/fluid graphs, steam/electrical bootstrap, crusher/pump/drill and fixed sensor/relay behavior. This supersedes earlier statements excluding this bounded industrial content; hybrid transport/control variants, advanced logic and durable saves remain later extensions. [Industry verification](verification/INDUSTRY_RESULTS.md) owns evidence and remaining limits.


## Authorized multiblock and pipe extension — 2026-09-10

The user selected [issue #3](https://github.com/Starbugstone/Rivet-Reach/issues/3): reusable multiblock lifecycle, player-built liquid tanks, connected Blender shell surfaces and shared pipe connections. [MULTIBLOCKS.md](MULTIBLOCKS.md) owns construction, exact shared storage, breach/resize recovery, signal valves/level sensors and independent signal/power fittings on both item and fluid pipes. This supersedes earlier exclusions of those specific hybrid channels and tank controls. [Verification](verification/MULTIBLOCK_RESULTS.md) records the measured build and remaining review. Whole-world durable saves remain later scope.

## Held industrial items and battery kit — 2026-09-10

Held assemblies resolve through `IndustryDefinition.All`, including tank parts and batteries. They use the first-person depth convention shared by hands and tools; glass uses a separate transparent held shader. Connected pipes select one authored connection mesh, not all 64 masks in their export family. [Workshop follow-up evidence](verification/WORKSHOP_FOLLOWUP_RESULTS.md) checks actual item contribution to the player framebuffer rather than treating an active empty object as visible. The battery kit uses the existing original Workshop atlas; [BATTERIES.md](BATTERIES.md) owns source paths, current dimensions and gameplay rules.

## Stable ambient occlusion — 2026-09-10

The PC renderer uses Interleaved Gradient SSAO sampling to keep stationary terrain and tree contact shading stable. Preserve the authored AO intensity/radius and directional shadow quality when adjusting it. The native [shadow stability check](verification/SHADOW_RESULTS.md) records the Blue Noise comparison, temporary fixture controls and remaining limits; Editor test defines can mask Blue Noise animation.

## Lighting cost and shadow stability — 2026-09-10

Keep one shadowed celestial directional light. Its rotation advances on the clock samples specified in [SIMULATION.md](SIMULATION.md#session-world-clock-and-celestial-presentation); do not couple light direction to terrain/chunk simulation ticks or add a directional light per chunk. Preserve soft shadows and the existing 2048 atlas, four cascades and 160 m shadow range unless a measured comparison supports a quality/cost change. The GPU still renders dynamic casters between sun samples; a rotation hold is not a shadow-map cache.

Local lighting already has bounded presentation: at most eight nearby torch lights (10 m light range, active within 24 m, 256 shadow-face tier) and eight powered workshop lamp lights (7 m range, no shadow maps). Torch view selection refreshes at 5 Hz; lamp selection shares the nearby workshop view refresh. Torch geometry does not cast shadows; portrait fill lighting has no shadows and is restricted to its preview layer. Keep these caps when adding content, and preserve wall occlusion and nearby light quality when changing their allocation. Large-factory and densely overlapping light stress remain separate measured workloads; a cap alone does not prove their cost acceptable. [Sun-shadow results](verification/SUN_SHADOW_RESULTS.md) separates edge stability, render timings and remaining limits.

## Shared ore art and rendering — 2026-09-11

The user's accepted Azure texture/mesh is shared by all six ore appearances. [create_ore_variants.py](../Tools/create_ore_variants.py) reads the approved [Azure source](../ArtSource/Industry/AzureOre.blend) and renders five palette variants: silver iron, orange copper, black coal with lighter host stone, gold, and green-tinted diamond. [OreVariants.blend](../ArtSource/Industry/OreVariants.blend) stores linked objects with one mesh datablock and separate object materials. No variant FBX or copied Unity mesh is needed. Azure's authored geometry and appearance remain unchanged.

`OreVisuals` keeps a bounded cache of the single imported prefab and shared material/palette assets. Instances use `sharedMesh` and `sharedMaterial`; the held view keeps one per-player material for its first-person depth settings and reuses its object across ore selections, changing only the palette. Do not mutate a shared world material to recolour one object. **User revision, 2026-09-12:** collected raw iron and copper now share their deposits’ stylised ore-block mesh, palette and inventory icon through `OreVisuals.VisualId`. This covers actual mining drops, discarded inventory stacks and held items, including previously saved resources. Their existing item identities, smelting/crushing recipes and nonplaceable status remain intact. Gold ore still drops its existing raw resource, coal ore drops coal, and diamond ore drops diamond. [Drop verification](verification/ORE_DROPS_RESULTS.md) records the focused runtime check and wiki refresh.

World deposits use the existing terrain texture array and greedy chunk mesher: hidden faces are absent, matching exposed faces can merge, and chunk residency bounds exploration presentation. The detailed 4,129-triangle model is only used where an actual ore item is displayed. Shared geometry saves memory, but each visible instance can still cost draw submissions, vertex processing and shadow work. GPU instancing is a separate optimization, and enabling it on materials does not establish that the current render pipeline actually batches a scene into one draw. See [Unity's instancing documentation](https://docs.unity3d.com/6000.4/Documentation/Manual/GPUInstancing.html).

[Ore sharing verification](verification/ORE_VARIANTS_RESULTS.md) owns measured reference reuse, terrain counts and a bounded 256-prop frame sample. Large mixed factories, shadows, transparent tanks, animation and exploration streaming still require representative uncapped profiling before choosing LODs, instanced drawing or view pooling. Existing item stack merging, 96 m item-view range, 64 m item simulation range and 64 m machine presentation range help bound nearby work; they are not a claim that arbitrarily large factories are already optimized.
