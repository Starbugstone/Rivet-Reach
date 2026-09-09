# First playable POC — terrain expedition

Implementation record for the user-authorized first slice, 2026-09-08. This is the playable review candidate for [Stage 0](DEVELOPMENT_STRATEGY.md#6-locked-first-step-and-provisional-later-sequence). The user subsequently authorized [modular crafting](CRAFTING.md), activating the personal 2×2 grid with a shared 2×2/3×3/4×4 core. The full-game specifications remain targets; machinery, larger station interfaces, structures and persistent saves are not implied by this delivery.

## Run and build

Open the repository root with Unity **6000.4.4f1**, open `Assets/RivetReach/Scenes/Main.unity`, then press Play. The scene's runtime entry point is `Expedition.Bootstrap`; the terrain and interface are constructed from versioned definitions and assets when the scene runs. The Editor's unplayed scene is therefore not a populated voxel map.

The Windows build is generated at `Builds/PlayerRevision4/RivetReach.exe`. Keep its adjacent data folder, DLLs and Mono runtime together. Start Expedition creates a terrain session using a fresh random seed, or the optional seed entered on the title screen. Nearby terrain prepares before movement can enter it. No developer commands are needed to play.

Build through **Rivet Reach → Build Windows first POC** in Unity, or from Windows PowerShell:

```powershell
.\Tools\Build-Windows.ps1
.\Tools\Verify-POC.ps1
.\Tools\Verify-POC.ps1 -Trees # Focused tree/axe/leaf/streaming verification
.\Tools\Verify-POC.ps1 -Crafting # Pointer, recipe, output and conservation verification
.\Tools\Verify-POC.ps1 -Ores # Depth bands, pickaxe drops, bedrock and depletion
```

The build script reuses this project's open Editor through an explicit local file request, or starts the pinned Editor in batch mode when the project is closed. It does not close scenes, discard unsaved work or install a live editor integration. Runtime assets are committed, so ordinary Play does not require Blender or an asset-generation step. After cloning, retrieve binary assets with `git lfs pull`.

For an Editor startup regression check, open the saved Main scene outside Play mode and choose **Rivet Reach → Verify Editor Play startup**. This runs two ordinary Play/Stop cycles, checks the title-to-game transition and character animation, and writes results/screenshots to `Logs/EditorPlayVerification/`. It uses the current scene and imports without an asset preparation or build step. See [the Editor startup fix](verification/EDITOR_PLAY_STARTUP_RESULTS.md) for the blue-screen diagnosis and evidence.

The isolated, verified crafting review build is available at `Builds/Crafting/RivetReach.exe`. Its source baseline and measured checks are recorded in [crafting verification](verification/CRAFTING_RESULTS.md).

## Play

| Action | Default |
|---|---|
| Move / look | WASD / mouse |
| Sprint / jump / crouch | Left Shift or double-tap Forward / Space / Left Ctrl |
| Mine | Hold left mouse on a block within 5 m |
| Place collected terrain | Right mouse on a block face; uses the selected hotbar stack |
| Inventory / pause | Tab / Escape |
| Select hotbar | Mouse wheel or [ / ] across all 12 slots; 1–0 for the first ten |
| Drop one / full selected stack | Q / Shift+Q |
| Inspect body / diagnostics | F5 / F12 |
| Move an inventory stack | Click source then destination, or drag |
| Split / place one | Right-click |
| Transfer between hotbar and main storage | Shift-click |
| Rotate the appearance preview | Drag the player portrait |

The title screen starts with a fresh random world seed on each launch. Leave the optional seed field blank to use it, or enter a signed 32-bit integer to replay that terrain; F12 diagnostics display the active seed.

The settings screens expose look sensitivity, FOV, view distance, interface scale and keyboard action rebinding. Existing mapped keyboard conflicts swap keys. Mining can use left or right mouse; placement uses the opposite button. Appearance and settings are remembered locally.

Inventory has **12 hotbar + 48 main slots**, with a 64-item limit per ordinary stack and one item per tool or armor stack. Grass, dirt, stone, logs and leaves use real stable definitions. Selected terrain stacks can be placed against a targeted block face. One thin dark outline identifies the aimed block. Blocked cells, actual player overlap and unavailable terrain reject placement without consuming the stack. Touching feet allow placement below them; jump and hold Place to build upward. Loose drops pop above a placed block or move to a clear side. The selected terrain item appears in the right hand and follows its swing; empty stacks restore the bare fist. Held placement repeats every 0.22 seconds. Fists mine one block; the starter axe also fells connected generated logs above the cut. Player-placed wood always mines one block at a time. [The placement contract](GAMEPLAY.md#first-step-terrain-placement--user-feedback-extension) owns the detailed rules. The personal 2×2 crafting grid now accepts ingredients. **Recipes** displays layouts from the same editable catalog. Click or right-click the result to craft once; Shift-click with an empty cursor crafts all complete outputs that fit. Shift-click an ingredient or use **Return ingredients** to move it back. Closing also returns ingredients; any that cannot fit stay in the grid for the session. See [starter recipes](ECONOMY.md#current-survival-recipes-and-tiers) and [the crafting contract](CRAFTING.md).

New survival sessions start empty-handed. Punch logs, make planks in the personal grid, craft a workbench, and use its 3×3 grid to make tools. **Recipes** displays the layouts at either grid. A wooden pickaxe unlocks cobblestone and coal; subsequent materials unlock faster tools. An axe fells connected generated logs at or above the cut, leaving the stump. Player-placed wood, fists and other items remain single-block mining. Natural unsupported leaves decay; placed leaves remain. [Current recipes](ECONOMY.md#current-survival-recipes-and-tiers) and [tree rules](GAMEPLAY.md#trees-and-axe-felling--user-feedback-extension) own the details.

Coal and copper occur nearer the surface; iron extends deeper; gold and diamond occur in deeper bands. [The ore table](ECONOMY.md#current-ore-generation-and-bedrock) gives exact Y ranges. F12 displays your height. Pickaxe grade gates extraction, ores yield nonplaceable raw resources, and bedrock at Y = −256 is unbreakable. Smelt raw metals or bake potatoes in a placed furnace. [Survival verification](verification/SURVIVAL_RESULTS.md) records the current build; [ore verification](verification/ORE_RESULTS.md) remains historical evidence for the smaller ore-only slice.

**World progress is session-only.** Edits, inventory and unexpired dropped items survive chunk unloading in the running game. Quitting/restarting resets them. The title, pause menu and HUD disclose this boundary; appearance/settings persist independently.

## F12 debug panel

Press **F12** during an expedition to show or hide the debug panel. The action is listed as **Diagnostics** in Controls and can be rebound. It starts hidden during ordinary play; automated verification can enable it explicitly.

The panel displays:

- FPS and smoothed frame duration.
- Active world identifier, seed and player block coordinates.
- Ready/resident chunk counts and pending terrain work.
- Latest generation/mesh and local edit mesh durations.
- Terrain triangle count, retained terrain edits and dropped-item pile count.
- Rejected stale jobs and the current rendering origin.
- The most recent placement result or rejection reason.

**“Cannot place inside the player” appears only in this panel.** Normal building remains quiet when your body blocks a placement attempt. Collision still prevents placing through your legs; a block that fits below your feet is allowed, and holding Place while jumping builds underneath once there is room. Other useful placement feedback, such as an empty selection or unavailable terrain, remains on the HUD. Closing F12 hides diagnostics without affecting gameplay.

The values are current development diagnostics, not a benchmark result or a save system. Copy the displayed seed to recreate the terrain in a new expedition; placed/mined blocks and inventory still follow the session-only progress boundary above. [The gameplay contract](GAMEPLAY.md#first-step-terrain-placement--user-feedback-extension) owns placement behaviour; recorded checks and screenshots are in [building and movement results](verification/BUILDING_AND_MOVEMENT_RESULTS.md).

## Implemented foundation

- One `surface` world instance, deterministic seed and `terrain-4-biomes-caves` generator version. [Five biomes, varied relief, natural entrances and deep caves](TERRAIN_GENERATION.md), seeded log/leaf trees, five depth-banded ores and an unbreakable base; no structure generation or water.
- 32³ chunks with a one-cell immutable worker halo. The generator evaluates integer-coordinate noise, so adjacent chunks share terrain independently of discovery order. Authoritative block addresses use signed 64-bit horizontal coordinates and integer height; Unity floating positions are presentation only.
- Runtime chunk demand follows the player and nearby surface. The default horizontal radius is ten chunks (320 m per axis), adjustable from four to fourteen. Vertical demand covers the player's vicinity and the local surface without retaining the entire intervening deep column. Supported design bounds are Y −256 through 767 and horizontal ±1,000,000,000 blocks.
- Two background generation/greedy-mesh jobs, nearest-first dispatch, main-thread mesh publication with a nominal 4 ms budget, stale-result rejection using both edit revision and residency generation. Unloaded chunk meshes and occupancy arrays are released. Compact session edit records and dormant item records remain; travelling without edits does not retain an unbounded full-chunk cache.
- Rendering-origin shifts at 512 local horizontal metres, using whole-chunk offsets. Player, resident chunks and physical stack views retain their world addresses across the shift.
- Voxel queries own movement/collision, including unready frontiers. Swept movement substeps prevent stepping through cells; no MeshCollider or GameObject is created per voxel. Mining changes authoritative occupancy before drops are emitted. Placement commits occupancy and consumes one selected item in the same local authority turn, using the same edit revision and neighbour-halo update path.
- Held fist mining, selection outline, progress bar, fist motion and original procedural impact/pickup/footstep audio. Grass/dirt/stone fist times are working defaults of 0.45 / 0.60 / 0.95 seconds.
- Grass drops dirt when fist-mined. Nearby resident grass spreads to exposed dirt and decays under cover on fixed random ticks, using direct sky exposure. These solid-to-solid changes use the session edit/revision system and asynchronous remeshing. [Grass scheduling](SIMULATION.md#13-grass-random-ticks--first-step-feedback) owns the working timing, locality and light limitations.
- Physical stack entities use 20 Hz custom movement, dry-ground sleep, 1.7 m proximity pickup with an obstruction check, partial pickup, drop-owner delay of 0.75 s, same-item merge checks every 0.5 s and item-definition pile limits (64 for ordinary resources, one for tools/armor). Different item types may share one block; merging removes emptied pile records and views, preserves excess quantity in remainder stacks and reuses its spatial lookup storage. Merge keeps the oldest eligible lifetime. Twenty minutes of active physical eligibility expires ordinary piles; unloaded and paused time does not count. No piles are discarded simply to enforce a population cap.

Detailed future contracts remain owned by [SIMULATION.md](SIMULATION.md) and [GAMEPLAY.md](GAMEPLAY.md). This first implementation has one local authority; it does not implement multiplayer transactions, durable journals or the factory scheduler.

## Actual first visual kit

Actual front/back renders can be reproduced with [render_player_review.py](../Tools/render_player_review.py). The [Blender game-art skill](skills/blender-game-art/SKILL.md) records the reference-to-runtime review workflow.

The original Blender asset script is [create_player_assets.py](../Tools/create_player_assets.py). Editable `.blend` sources live in `ArtSource/Characters/`; explicit FBX and PNG outputs are under `Assets/RivetReach/Resources/Characters/`. Re-run in Blender 5.2 background mode with the script's absolute path. The process updates source/export content while existing Unity `.meta` files retain their identities.

The player rebuild uses a shared **48-deformation-bone skeleton plus two grip sockets**, including clavicles, neck, three joints per finger and two per thumb. It retains one material/submesh and the same skin-region layout on both variants. Elbows, knees, wrists and ankles have blended weights (at most four influences per vertex). The source library contains 28 in-place actions. Sixteen form the base library: `Idle`, `Walk`, `Run`, `Airborne`, `Mine`, `CrouchIdle`, `CrouchWalk`, `Land`, plus the corresponding eight `FP_` actions (the first-person resting crouch is named `FP_Crouch`). The twelve additional actions provide block, one-hand handle and two-hand handle holds/swings in body and first-person form. Unity blends the imported clips with Playables; an upper-body mask lets mining run over locomotion. Signed horizontal travel drives the gait, and visual animation does not remove blocks or move the collision body. Exponential blend weights give a fast attack and softer settling; strafe stance and upper-body banking follow movement direction. Crouching bends the skeleton rather than scaling the body, landing has a transient impact pose, and first-person sway follows turns while camera aim stays immediate. Camera eye height transitions independently of the immediate crouch collider change and respects ceiling clearance.

The models were rebuilt against the turnaround sheet, then refined under the user’s higher-polygon authorization with dense facial loops, inset almond eyes and eyelids, curved swept hair, fitted waistcoats, cloth folds, joined wrist/palm surfaces, continuous trouser shells and shaped leather boots. The geometry review budgets are owned by [the content pipeline](CONTENT_PIPELINE.md#7-player-geometry-and-performance-review). First-person poses keep dorsal fist surfaces up, thumbs inward and wrists low in the camera frame. The current first-person view renders the right arm only. Its geometry is derived from the selected body, including its articulated fingers; both arms remain on the full model and shadow. The source `Mine` and `FP_Mine` clips use a 0.3-second diagonal swing. User speed feedback applies 2.5× runtime playback to strikes and their layer transitions: punches take 0.12 seconds, and the 0.6-second tool gestures take 0.24 seconds. Release completes the gesture before blending to idle. [revise_player_interactions.py](../Tools/revise_player_interactions.py) rebakes the library while preserving mesh/skin sources. The body visibility mask excludes the head/neck and duplicate arms while retaining the jacket, waist and legs. A separate shadow-only renderer uses the complete mesh on the same animated bones. First-person body placement follows the crouch blend to keep the open neck outside the camera; camera position, collision and interaction rays are unchanged. Derived meshes compact all vertex streams so hidden head/hair vertices are not skinned in the arms renderer. Male/female appearance and both skins retain identical gameplay dimensions. [Player polish results](verification/PLAYER_POLISH_RESULTS.md) contain source renders, Unity imports, counts, animation checks and remaining artistic review.

Two **1024×1024 opaque PNG skins** use a shared 4×4 tile layout (256×256 per region). Regions in Blender image coordinates, from the bottom row upward:

| Row | Column 0 | Column 1 | Column 2 | Column 3 |
|---|---|---|---|---|
| 0 | Face/neck | Shirt | Left sleeve | Right sleeve |
| 1 | Left arm/hand | Right arm/hand | Left trousers | Right trousers |
| 2 | Boots/belt | Hair | Eye whites | Pupils/soles |
| 3 | Buckle/buttons | Waistcoat | Mouth | Reserved |

Each polygon samples its assigned region with an inset to limit mip bleeding. The continuous hands use cylindrical coordinates within their regions; the high-fidelity pass adds shared normal and material-response maps. This is an initial paintable palette layout, not a production face-by-face body unwrap. Multiple faces within a region reuse UVs; independent front/back painting and a clean artist template remain visual-review refinements. Field/Teal and Ochre/Slate differ in skin, face and clothing colours and work on both variants. Custom PNG import, overlay layers and Minecraft-format compatibility are not provided.

Terrain uses eighteen original **64×64** tile-array layers: seven natural terrain/tree swatches, five ores, bedrock and five raw resources. [TerrainTiles.cs](../Assets/RivetReach/Editor/TerrainTiles.cs) builds the registry; the subsequent [arcade material pass](CONTENT_PIPELINE.md#arcade-presentation-and-dynamic-feedback) refines the natural swatches and adds matching normal/roughness detail, nearby wind grass and bounded action effects. Greedy quads repeat the tile per metre with mipmaps and 8× anisotropy; world-space palette variation preserves its phase through floating-origin shifts. Warmer fixed daylight, three-colour ambient fill, slow-moving procedural clouds and restrained distance haze provide a stable review setting. The gameplay camera enables the scene’s restrained colour grade, bloom and vignette. The HUD has a smaller hotbar, outlined slots and block icons sampled from the actual terrain tiles. The revised shader participates in URP shadow/depth passes and samples directional shadows and screen-space ambient occlusion. Shadow distance is 160 m. Four-sample MSAA and the arcade camera’s SMAA reduce silhouette/terrain-edge aliasing, and a 768×1024 portrait is cropped to each UI panel’s aspect ratio so the player is not squeezed horizontally. Fog now begins at 236.8 m and ends at 304 m with the default ten-chunk radius; it follows the selected streaming radius rather than obscuring nearby hills. The [body and scene polish report](verification/BODY_AND_SCENE_POLISH_RESULTS.md) records the look-down views, rendering costs and subsequent checks. Propagated voxel light and fully occluded cave lighting are not implemented.

## Verification and review boundary

The [arcade visual review](verification/ARCADE_VISUAL_RESULTS.md) records the subsequent lighting/material treatment, dynamic action effects and effect-intensity setting. The [high-fidelity player and audio review](verification/HIFI_PLAYER_AND_AUDIO_RESULTS.md) records the subsequent continuous hands, distal finger joints, material maps and sound revision. The earlier [grass and hand interaction review](verification/GRASS_AND_HAND_INTERACTION_RESULTS.md) records that revision’s checks and screenshots. Earlier [terrain/UI](verification/VISUAL_REVISION_RESULTS.md) and [player polish](verification/PLAYER_POLISH_RESULTS.md) reports retain their historical measurements. Automation separates test fixtures from ordinary sessions: the explicit `-rr-verify` command-line mode injects inventory stacks, controlled movement and seam edits; the explicit Editor startup check also briefly drives movement, crouch and mining animation. Ordinary Play enables neither test mode.

This delivery establishes the first playable review candidate. The user still needs to assess movement/mining feel, player appearance and terrain composition in play before the milestone is accepted or subsequent work is selected. In particular, the initial rig/skin layout and lighting remain open to refinement. Personal crafting and the shared modular recipe core are now explicitly authorized and implemented; [crafting verification](verification/CRAFTING_RESULTS.md) records its checks. The selected survival extension below adds 3×3 workbenches, furnaces and tool progression. Durable saves, 4×4 interfaces, industry and other roadmap groups still require a separate scope decision.

For hand-pose review, enter Play, start an expedition, then open **Rivet Reach → Review hand grips**. Selected terrain blocks use a flat upward palm. The panel also offers cosmetic sword (one-hand) and pickaxe (two-hand) previews with fingers wrapped around the handle. Return to **Selected inventory block / bare hand** to restore normal selection; closing the panel also clears its override. The user subsequently made these models available as starter dagger/pickaxe inventory items. Preview selection remains separate from inventory selection; it does not grant an item or add combat.


## Day/night review

The user-requested [day/night cycle](GAMEPLAY.md#day-night-and-lunar-phases) runs automatically after Start Expedition. The upper-right HUD displays day, time and moon phase. Its clock follows play/inventory and pauses with menus. No debug command is needed to experience the cycle.

Use **Rivet Reach → Verify day and night clock** for focused domain checks, or **Rivet Reach → Build day and night review** to prepare assets, run those checks and build `Builds/DayNight/RivetReach.exe` without replacing the ordinary POC executable. In a closed project, the pinned Editor can run `-batchmode -quit -executeMethod RivetReach.Editor.DayNightChecks.BuildReview`. The existing open-Editor poller accepts `build` or `checks` in `Logs/day-night-request.txt`, writing `Logs/day-night-result.txt`; coordinate use of the shared Editor with other active tasks.

Run the built player with `-rr-verify -rr-day-night-review -rr-output "<absolute review folder>" -logFile "<absolute log path>"` to exercise time progression, menu/inventory behavior, sun/moon lighting, eight phase screenshots and new-world reset. The test changes world time only in this explicit verification mode. [Results and build context](verification/DAY_NIGHT_RESULTS.md) distinguish the isolated day/night evidence from concurrent feature integration.

## Terrain rework review

[TERRAIN_GENERATION.md](TERRAIN_GENERATION.md) owns the current biome/cave profile and material IDs. `Tools/Check-Terrain.ps1` runs generator checks without launching Unity; `Tools/Verify-Terrain.ps1 -Build` uses the open pinned Editor to prepare a dedicated `Builds/Terrain/RivetReach.exe` and run the focused terrain review. Coordinate shared build use before writing its local request. See [terrain results](verification/TERRAIN_GENERATION_RESULTS.md) for the actually tested artifact, generated review coordinates and remaining limits.


## Survival progression review

The user extended the crafting slice with familiar basic recipes and tiers, stations, farming, hunger, health and armor. [Gameplay](GAMEPLAY.md#survival-progression-farming-health-and-armor) owns the rules and [economy](ECONOMY.md#current-survival-recipes-and-tiers) owns recipe layouts/stats. Start empty-handed, gather logs, use personal crafting to make planks/sticks/a workbench, then craft a wooden pickaxe and mine stone for the next tier and furnace.

Use a placed workbench/furnace/chest to open it; crouch-use places against it. The recipe button shows layouts from the same registry used to validate crafting. A furnace needs an ingredient and fuel; drag logs explicitly to its fuel slot if you want to burn them. Find wild ripe potatoes, use a hoe to till grass/dirt, use a potato to plant and harvest when ripe. Hold Use away from farmland to eat. Copper/iron/diamond armor equips into the four inventory armor slots. Health appears as ten hearts; hungry players cannot sprint, food supports healing and the death screen offers Respawn while retaining the world.

Build with `Tools/Build-Windows.ps1`; `Tools/Verify-POC.ps1 -Survival -OutputDirectory <absolute path>` runs the focused input/transaction/lifecycle scenario. [Survival results](verification/SURVIVAL_RESULTS.md) records measured evidence. Worlds and stations still reset when the application quits. Tool/armor wear, fitted armor meshes, enchantments and irrigation remain unimplemented.
