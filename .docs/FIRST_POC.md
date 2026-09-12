# Rivet Reach — current playable increment

The Unity **6000.4.4f1 / URP 17.4.0** project now includes terrain and caves, FPS movement, inventory, modular crafting and survival progression, day/night and native creatures. [Current verification](verification/README.md) separates measured checks from remaining review; [DEVELOPMENT_STRATEGY.md](DEVELOPMENT_STRATEGY.md#6-locked-first-step-and-provisional-later-sequence) retains the incremental development policy and future milestones.

## Run and build

The downloadable [0.0.1 alpha](releases/0.0.1.md) is a Windows x64 prerelease. Extract its complete ZIP and run `RivetReach.exe`. Build it from a clean checkout with `Tools/Build-Release.ps1`; output is `Builds/Release/0.0.1/RivetReach-0.0.1-alpha-windows-x64/RivetReach.exe`. It uses the pinned Editor and a non-development build. Supply that path to `Tools/Verify-Creative.ps1 -Executable <path> -FullRun` to check the release player.

Open this repository in the pinned Unity Editor, open `Assets/RivetReach/Scenes/Main.unity`, then press Play. `Expedition.Bootstrap` creates the voxel world and UI at runtime; the unplayed scene is not a populated map. After cloning, run `git lfs pull` to retrieve binary assets.

The current local review executable is **`Builds/Creative/RivetReach.exe`**. Keep its adjacent data folder, DLLs and Mono runtime together. Start Expedition uses a fresh random world seed, or the optional signed 32-bit seed entered on the title screen. Nearby terrain prepares before movement can enter it.

For the latest menu/crafting responsiveness changes, use **`Builds/RecipeBrowser/RivetReach.exe`**; [pointer verification](verification/POINTER_RESULTS.md) identifies this focused build and its checks.

For the latest quit, respawn and mining-speed corrections, use **`Builds/GameplayFixes/RivetReach.exe`**; [gameplay verification](verification/GAMEPLAY_FIX_RESULTS.md) records the tested build. In an already-running Editor Play session, stop and restart Play to load the updated scripts.

The normal build entry point is **Rivet Reach → Build Windows first POC**, or Windows PowerShell:

```powershell
.\Tools\Build-Windows.ps1
.\Tools\Verify-POC.ps1 -StarterCrafting
.\Tools\Verify-POC.ps1 -Survival
```

`Tools/Verify-Multiblocks.ps1 -Build` prepares, builds and verifies the current tank/pipe review through the open Editor. `Tools/Verify-Industry.ps1 -Build` retains the earlier workshop scenario. The older general script builds `Builds/PlayerRevision4/RivetReach.exe`. To test the delivered review executable, supply `-Executable Builds/Multiblocks/RivetReach.exe`. `Tools/Verify-Creative.ps1 -Build -FullRun` builds the current Creative review and checks controls, Creative industry/multiblocks, placement/movement, recipe browsing and Survival through the open Editor. Runtime assets are versioned; ordinary Play requires no Blender or asset-generation step.

Light-bulb and speaker markers are Unity Editor annotations, with no gameplay purpose. The project hides these icons on Editor initialization and Play entry; actual lights and sound remain enabled. Camera clearance uses movement solidity so walking through torches and potato plants keeps the normal eye height.

The build script reuses this project's open Editor through its existing local file request, or starts the pinned Editor in batch mode when the project is closed. Preserve open scenes and unsaved work. An explicit Editor startup check is available through **Rivet Reach → Verify Editor Play startup**; the latest [player/audio report](verification/HIFI_PLAYER_AND_AUDIO_RESULTS.md) records its evidence.

## Controls

| Action | Default |
| --- | --- |
| Move / look | WASD / mouse |
| Sprint / jump / crouch | Left Ctrl or double-tap Forward / Space / Left Shift |
| Mine / attack | Hold left mouse while aiming within reach |
| Interact with stations or machines | E, or right-click while aiming at the block |
| Place / use held item | Right-click; Left Shift + right-click places against a station |
| Inventory / pause | Tab / Escape |
| Close inventory or station screen | Tab, E (Interact), or Escape; Tab/E shortcuts are ignored while typing in a text field |
| Select hotbar | Mouse wheel or [ / ]; 1–0 for the first ten slots |
| Drop one / selected stack | Q / Shift+Q |
| Inspect body / diagnostics | F5 / F12 |
| Move an inventory stack | Click source then destination, or drag |
| Split / place one in a slot | Right-click; hold and drag to place one per inventory/crafting slot |
| Transfer / craft all that fit | Shift-click inventory slots / crafting result |
| Fill recipe from the item browser | Shift-click icon: one recipe; Ctrl+Shift-click icon or Shift-click Fill grid: maximum |
| Rotate the appearance preview | Drag the player portrait |

Keyboard controls can be rebound; conflicting actions swap keys. The old default Sprint/Crouch pair migrates to Ctrl/Shift; other customized pairs are preserved. New Interact bindings preserve existing customized keys. Mining can use either mouse button; use/placement follows the opposite button, including the displayed prompts. Appearance, audio and settings are remembered locally.

## Start crafting

1. Punch logs and walk close to collect them. Ordinary sessions start empty-handed.
2. Open inventory with **Tab**. One log in any personal crafting cell makes **four planks**.
3. Put **one plank in each of the four cells** to make one workbench. Two planks arranged vertically make four sticks.
4. Move the workbench to the hotbar, select it and **right-click a ground face to place it**.
5. Aim at the placed workbench and press **E** or **right-click** to open its **3×3 grid**. Interaction needs a visible block within five-block targeting reach.
6. Put **three planks across the top row and two sticks down the center** to make one wooden pickaxe. Mine stone to collect cobblestone for stone tools and a furnace.

Once you have coal or furnace-made charcoal, place **one above one stick** in either crafting grid to make **four torches**. Select a torch and right-click a floor or wall to light the area. Mine it to recover it. [Torch rules](GAMEPLAY.md#torches) describe attachment and lighting limits.

The **item sidebar** displays all registered icons. Click an icon for recipes; right-click for uses. Shift-click (or Ctrl-click) fills one compatible recipe into the open crafting grid from your materials; Ctrl+Shift-click fills the maximum complete batch. The same shortcuts on a recipe output select that exact recipe; Fill grid prepares it once; Shift-click Fill grid prepares the maximum complete batch in the active crafting grid. The detail view shows materials and the required station, with clickable ingredients and Back navigation. Hover inventory slots and press R/U for the same lookups. Try `Builds/RecipeBrowser/RivetReach.exe` for this interface. Click the result to craft once; Shift-click crafts complete outputs that fit. Closing the inventory returns ingredients; leftovers stay in the grid if the inventory is full. [CRAFTING.md](CRAFTING.md) owns the shared 2×2/3×3/4×4 engine and authoring; [ECONOMY.md](ECONOMY.md#current-survival-recipes-and-tiers) owns the 55 survival recipes and progression. The [Machinist’s Bench](INDUSTRY.md) now opens the 4×4 interface and adds 36 industrial recipes, including the [tank component family](MULTIBLOCKS.md).

## Survival and exploration

A wooden pickaxe unlocks cobblestone and coal; stone, copper, iron and diamond tiers extend progression. [Ore bands](ECONOMY.md#current-ore-generation-and-bedrock) determine depth; protected bedrock is at Y = −256. Axes fell connected generated logs above the cut. Placed logs mine individually and placed leaves persist; natural unsupported leaves decay.

Craft a furnace from eight cobblestones around an empty center, and a chest from eight planks in the same layout. Furnaces accept an ingredient and fuel, smelt raw metals and bake potatoes; drag logs explicitly into the fuel slot to burn them. Chests hold 27 stacks. Stations and their contents survive chunk unloading during the session.

Gather wild ripe potatoes. Use a hoe to till grass/dirt, use a potato to plant, and harvest ripe plants. Hold Use away from farmland to eat. Food restores hunger, enables sprinting and supports healing. Health, hunger and armor use ten-icon meters. Hunger slowly drains even at rest and faster during healing. Healing starts at 60% food, continues above 50%, and stops at 50% or below. Copper/iron/diamond armor fits four equipment slots and reduces impact damage by up to 80%, always leaving some damage. Death drops carried items and offers Respawn while retaining the world. The icon/healing review build is **`Builds/SurvivalHUD/RivetReach.exe`**; [verification](verification/SURVIVAL_HUD_RESULTS.md) records its evidence.

[Five biomes and caves](TERRAIN_GENERATION.md), the [day/night clock](GAMEPLAY.md#day-night-and-lunar-phases), and [Rustback beetles and Dusk prowlers](MOBS.md) are integrated. Aim and hold Mine to attack with the selected item or fists. Night affects prowler spawning. Health and equipment remain independent of male/female appearance and skin selection.

**Save progress with Escape → Save Game.** Load a checkpoint through Load Game, or use Continue Latest Save on the title. Save & Quit writes before exiting; closing the window does not autosave. [SAVES.md](SAVES.md) owns storage, recovery and compatibility. Equipment wear, fitted armor meshes, irrigation and generated structures remain future scope.

## Foundation and diagnostics

The world uses deterministic `terrain-4-biomes-caves` generation, integer block addresses, 32³ chunks, bounded worker jobs, stale-result rejection and a floating render origin. The default horizontal view radius is ten chunks; rendering and simulation operate on loaded or active state. [SIMULATION.md](SIMULATION.md) owns these contracts and [CONTENT_PIPELINE.md](CONTENT_PIPELINE.md) owns original assets, materials, player models and skins.

F12 shows frame timing, seed/coordinates, chunk demand, meshing, render triangles, edits, item piles and the most recent placement result. Player-overlap placement rejection appears only in diagnostics. These live counters are not benchmarks. Copying a seed recreates generated terrain, not the session's inventory or edits.

## Verification

Use `Verify-POC.ps1` with `-StarterCrafting`, `-Crafting`, `-Survival`, `-Ores`, `-PlacementItems`, `-Trees`, `-Audio` or `-Arcade` for the relevant scenario. Terrain, day/night, player imports and mobs have their documented dedicated checks. Explicit verification modes supply fixtures and virtual input; ordinary sessions do neither. [The evidence index](verification/README.md) links only the maintained report for each feature.

## Seas, rivers and buckets — liquid increment

The current liquid review executable is **`Builds/Fluids/RivetReach.exe`** (Unity 6000.4.4f1 / URP). Craft a bucket on a workbench using three iron ingots in `I.I / .I.`. Use an empty bucket on source water to collect it; use a filled bucket on a block face to place it. Water renews in a supported two-source pool. Jump rises while immersed; Crouch descends. [Fluid rules](FLUIDS.md) and [verification](verification/FLUID_RESULTS.md) record scope and evidence. Existing feature screenshots retain their original artifact identity.

## Creative testing

The Creative review executable is **`Builds/Creative/RivetReach.exe`**. Start an expedition, then press **Escape → Creative Mode: Off** to enable it. Double-tap **Space** to enable or disable flight. Fly with **WASD**, hold **Space** to rise, **Left Shift** to descend and **Left Ctrl** (or double-tap Forward) to fly faster. While walking, Shift crouches and Ctrl runs. Open **Tab → All Items**, search or scroll, and click an item for a full stack, or drag an icon from the right-hand sidebar into a backpack/hotbar slot. You are invincible, hunger is frozen, and placed blocks are not consumed. Use the same Escape toggle to return to Survival; gravity and damage resume and your items remain. See [the full rules](GAMEPLAY.md#creative-testing-mode) and [verification](verification/CREATIVE_RESULTS.md).

Run `Tools/Verify-Creative.ps1 -Build -FullRun` with this project's pinned Editor open to build and run Creative controls/catalog, Creative industry/multiblocks, placement/movement, recipe-browser and Survival checks against that same executable.


## Torches review build

The dedicated torch review executable is **`Builds/Torches/RivetReach.exe`**. `Tools/Verify-Torches.ps1 -Build` rebuilds through the pinned open Editor and runs the focused crafting, placement, light, water and streaming scenario. [Torch verification](verification/TORCH_RESULTS.md) includes matching lit/unlit screenshots and remaining limits. Earlier dedicated review executables retain their own artifact identities.

## Industrial workshop — issue #2

Run `Builds/Multiblocks/RivetReach.exe` for the current industrial review. [Industry](INDUSTRY.md) owns progression, recipes, machine interfaces, port directions and power/signal rules. Interact / mouse Use opens machines; levers toggle and buttons pulse. Machines rotate through their interface. The Machinist’s Bench opens 4×4 crafting. Creative’s existing catalog includes every new component for session testing. [Verification](verification/INDUSTRY_RESULTS.md) distinguishes measured evidence from remaining review.

## Avatar rework review

The 2026-09-10 [avatar rework](AVATAR_REWORK.md) is available in `Builds/AvatarRework/RivetReach.exe`. Run `Tools/Build-AvatarRework.ps1` to build with the pinned Editor; use `Tools/Verify-AvatarRework.ps1` and its `-Gameplay` option for the focused checks. Existing movement, inventory, appearance and Creative controls apply. Tools rest horizontally below the aim point, rise vertically to attack, and return after recovery. Hold the bound Use button with a blade for its vertical guard stance. [Evidence and limits](verification/AVATAR_REWORK_RESULTS.md) include both skins, held tools and selection transitions.


## Multiblock tanks and pipe fittings

At the Machinist’s Bench, craft tank frames, walls, glass, one controller and any ports/hatches. Build a hollow 3–9 block rectangular shell: frames on edges/corners, solid floor/roof, glass or solid sides. Functional parts face outside. The tank forms automatically, shares capacity across ports, and keeps the actual cells individually mineable. [MULTIBLOCKS.md](MULTIBLOCKS.md) owns the construction, connected art, valve/sensor controls and breach/resize recovery rules.

Interact with either Item Pipe or Fluid Pipe to fit a Signal Conduit and/or Power Cable from your inventory. A tank controller must be emptied before mining it; Recovery Out drains small remainders through its front pipe outlet. World progress remains session-only. [Current tank verification](verification/MULTIBLOCK_RESULTS.md) records the tested executable and evidence.
