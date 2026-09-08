# First playable POC — terrain expedition

Implementation record for the user-authorized first slice, 2026-09-08. This is the playable review candidate for [Stage 0](DEVELOPMENT_STRATEGY.md#6-locked-first-step-and-provisional-later-sequence). The next milestone remains unselected. The full-game specifications remain targets; machinery, recipes, structures and persistent saves are not implied by this delivery.

## Run and build

Open the repository root with Unity **6000.4.4f1**, open `Assets/RivetReach/Scenes/Main.unity`, then press Play. The scene's runtime entry point is `Expedition.Bootstrap`; the terrain and interface are constructed from versioned definitions and assets when the scene runs. The Editor's unplayed scene is therefore not a populated voxel map.

The Windows build is generated at `Builds/PlayerRevision4/RivetReach.exe`. Keep its adjacent data folder, DLLs and Mono runtime together. Start Expedition creates a terrain session using the seed entered on the title screen. Nearby terrain prepares before movement can enter it. No developer commands are needed to play.

Build through **Rivet Reach → Build Windows first POC** in Unity, or from Windows PowerShell:

```powershell
.\Tools\Build-Windows.ps1
.\Tools\Verify-POC.ps1
```

The build script reuses this project's open Editor through an explicit local file request, or starts the pinned Editor in batch mode when the project is closed. It does not close scenes, discard unsaved work or install a live editor integration. Runtime assets are committed, so ordinary Play does not require Blender or an asset-generation step. After cloning, retrieve binary assets with `git lfs pull`.

For an Editor startup regression check, open the saved Main scene outside Play mode and choose **Rivet Reach → Verify Editor Play startup**. This runs two ordinary Play/Stop cycles, checks the title-to-game transition and character animation, and writes results/screenshots to `Logs/EditorPlayVerification/`. It uses the current scene and imports without an asset preparation or build step. See [the Editor startup fix](verification/EDITOR_PLAY_STARTUP_RESULTS.md) for the blue-screen diagnosis and evidence.

## Play

| Action | Default |
|---|---|
| Move / look | WASD / mouse |
| Sprint / jump / crouch | Left Shift / Space / Left Ctrl |
| Mine | Hold left mouse on a block within 5 m |
| Place collected terrain | Right mouse on a block face; uses the selected hotbar stack |
| Inventory / pause | Tab / Escape |
| Select hotbar | Mouse wheel or [ / ] across all 12 slots; 1–0 for the first ten |
| Drop one / full selected stack | Q / Shift+Q |
| Inspect body / diagnostics | F5 / F3 |
| Move an inventory stack | Click source then destination, or drag |
| Split / place one | Right-click |
| Transfer between hotbar and main storage | Shift-click |
| Rotate the appearance preview | Drag the player portrait |

The settings screens expose look sensitivity, FOV, view distance, interface scale and keyboard action rebinding. Existing mapped keyboard conflicts swap keys. Mining can use left or right mouse; placement uses the opposite button. Appearance and settings are remembered locally.

Inventory has **12 hotbar + 48 main slots**, with a 500-item limit per stack in this palette. Grass, dirt and stone use real stable definitions. Selected terrain stacks can be placed against a targeted block face. A green/red preview shows destination validity; blocked cells, player/pile overlaps and unavailable terrain reject placement without consuming the stack. Held placement repeats every 0.22 seconds. Fists remain the mining method. [The placement contract](GAMEPLAY.md#first-step-terrain-placement--user-feedback-extension) owns the detailed rules. The crafting area is clearly inactive and accepts no items.

**World progress is session-only.** Edits, inventory and unexpired dropped items survive chunk unloading in the running game. Quitting/restarting resets them. The title, pause menu and HUD disclose this boundary; appearance/settings persist independently.

## Implemented foundation

- One `surface` world instance, deterministic seed and `terrain-1` generator version. Natural grass/dirt/stone hills and underground noise caves; no structure generation, water, vegetation, ores or other biomes.
- 32³ chunks with a one-cell immutable worker halo. The generator evaluates integer-coordinate noise, so adjacent chunks share terrain independently of discovery order. Authoritative block addresses use signed 64-bit horizontal coordinates and integer height; Unity floating positions are presentation only.
- Runtime chunk demand follows the player and nearby surface. The default horizontal radius is ten chunks (320 m per axis), adjustable from four to fourteen. Vertical demand covers the player's vicinity and the local surface without retaining the entire intervening deep column. Supported design bounds are Y −256 through 767 and horizontal ±1,000,000,000 blocks.
- Two background generation/greedy-mesh jobs, nearest-first dispatch, main-thread mesh publication with a nominal 4 ms budget, stale-result rejection using both edit revision and residency generation. Unloaded chunk meshes and occupancy arrays are released. Compact session edit records and dormant item records remain; travelling without edits does not retain an unbounded full-chunk cache.
- Rendering-origin shifts at 512 local horizontal metres, using whole-chunk offsets. Player, resident chunks and physical stack views retain their world addresses across the shift.
- Voxel queries own movement/collision, including unready frontiers. Swept movement substeps prevent stepping through cells; no MeshCollider or GameObject is created per voxel. Mining changes authoritative occupancy before drops are emitted. Placement commits occupancy and consumes one selected item in the same local authority turn, using the same edit revision and neighbour-halo update path.
- Held fist mining, selection outline, progress bar, fist motion and original procedural impact/pickup/footstep audio. Grass/dirt/stone fist times are working defaults of 0.45 / 0.60 / 0.95 seconds.
- Physical stack entities use 20 Hz custom movement, dry-ground sleep, 1.5 m proximity pickup with an obstruction check, partial pickup, drop-owner delay of 0.75 s, compatible merge checks every 0.5 s and 500-item pile limits. Merge keeps the oldest eligible lifetime. Twenty minutes of active physical eligibility expires ordinary piles; unloaded and paused time does not count. No piles are discarded simply to enforce a population cap.

Detailed future contracts remain owned by [SIMULATION.md](SIMULATION.md) and [GAMEPLAY.md](GAMEPLAY.md). This first implementation has one local authority; it does not implement multiplayer transactions, durable journals or the factory scheduler.

## Actual first visual kit

Actual front/back renders can be reproduced with [render_player_review.py](../Tools/render_player_review.py). The [Blender game-art skill](skills/blender-game-art/SKILL.md) records the reference-to-runtime review workflow.

The original Blender asset script is [create_player_assets.py](../Tools/create_player_assets.py). Editable `.blend` sources live in `ArtSource/Characters/`; explicit FBX and PNG outputs are under `Assets/RivetReach/Resources/Characters/`. Re-run in Blender 5.2 background mode with the script's absolute path. The process updates source/export content while existing Unity `.meta` files retain their identities.

The player rebuild uses a shared **40-bone skeleton**, including clavicles, neck and two joints per finger/thumb. It retains one material/submesh and the same skin-region layout on both variants. Elbows, knees, wrists and ankles have blended weights (at most three influences per vertex after welding shared seams). The source library contains sixteen in-place actions: `Idle`, `Walk`, `Run`, `Airborne`, `Mine`, `CrouchIdle`, `CrouchWalk`, `Land`, plus the corresponding eight `FP_` actions (the first-person resting crouch is named `FP_Crouch`). Unity blends the imported clips with Playables; an upper-body mask lets mining run over locomotion. Signed horizontal travel drives the gait, and visual animation does not remove blocks or move the collision body. Exponential blend weights give a fast attack and softer settling; strafe stance and upper-body banking follow movement direction. Crouching bends the skeleton rather than scaling the body, landing has a transient impact pose, and first-person sway follows turns while camera aim stays immediate. Camera eye height transitions independently of the immediate crouch collider change and respects ceiling clearance.

The models were rebuilt against the turnaround sheet, then refined under the user’s higher-polygon authorization with dense facial loops, inset almond eyes and eyelids, curved swept hair, fitted waistcoats, cloth folds, joined wrist/palm surfaces, continuous trouser shells and shaped leather boots. The geometry review budgets are owned by [the content pipeline](CONTENT_PIPELINE.md#7-player-geometry-and-performance-review). First-person poses keep dorsal fist surfaces up, thumbs inward and wrists low in the camera frame. Their geometry is derived from the selected body, including its articulated fingers. The body visibility mask excludes duplicate arms and the upper body from the first-person camera; derived meshes compact all vertex streams so hidden head/hair vertices are not skinned in the arms renderer. Male/female appearance and both skins retain identical gameplay dimensions. [Player polish results](verification/PLAYER_POLISH_RESULTS.md) contain source renders, Unity imports, counts, animation checks and remaining artistic review.

Two **256×256 opaque PNG skins** use a shared 4×4 tile layout (64×64 per region). Regions in Blender image coordinates, from the bottom row upward:

| Row | Column 0 | Column 1 | Column 2 | Column 3 |
|---|---|---|---|---|
| 0 | Face/neck | Shirt | Left sleeve | Right sleeve |
| 1 | Left arm/hand | Right arm/hand | Left trousers | Right trousers |
| 2 | Boots/belt | Hair | Eye whites | Pupils/soles |
| 3 | Buckle/buttons | Waistcoat | Mouth | Reserved |

Each polygon samples its assigned region with an inset to limit mip bleeding. This is an initial paintable palette layout, not a production face-by-face body unwrap. Multiple faces within a region reuse UVs; independent front/back painting and a clean artist template remain visual-review refinements. Field/Teal and Ochre/Slate differ in skin, face and clothing colours and work on both variants. Custom PNG import, overlay layers and Minecraft-format compatibility are not provided.

Terrain uses four original **32×32** bilinear-filtered tile-array layers: grass top, grass side, dirt and stone. Greedy quads repeat the tile per metre. Fixed directional light, a procedural sky and distance haze provide a stable review setting. The revised shader participates in URP shadow/depth passes and samples directional shadows and screen-space ambient occlusion. Shadow distance is 160 m. Four-sample MSAA reduces silhouette/terrain-edge aliasing, and a 768×1024 portrait is cropped to each UI panel’s aspect ratio so the player is not squeezed horizontally. Fog now begins at 236.8 m and ends at 304 m with the default ten-chunk radius; it follows the selected streaming radius rather than obscuring nearby hills. Surface palettes use restrained coarse variation. Propagated voxel light and fully occluded cave lighting are not implemented.

## Verification and review boundary

[Terrain/UI revision results](verification/VISUAL_REVISION_RESULTS.md) and [player polish results](verification/PLAYER_POLISH_RESULTS.md) record the final build's actual checks, machine, settings, measured workload and screenshots. Automation separates test fixtures from ordinary sessions: the explicit `-rr-verify` command-line mode injects inventory stacks, controlled movement and seam edits; the explicit Editor startup check also briefly drives movement and mining animation. Ordinary Play enables neither test mode.

This delivery establishes the first playable review candidate. The user still needs to assess movement/mining feel, player appearance and terrain composition in play before the milestone is accepted or subsequent work is selected. In particular, the initial rig/skin layout and lighting remain open to refinement. Durable saves, functional crafting and every later roadmap group require a separate scope decision.
