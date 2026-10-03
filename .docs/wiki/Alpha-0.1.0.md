# Download and play 0.1.0 Alpha

[Download the Windows x64 ZIP](https://github.com/Starbugstone/Rivet-Reach/releases/tag/v0.1.0), extract the whole folder and run **RivetReach.exe**. Keep the adjacent data folder and runtime libraries together. Unity and Blender are not needed. Use **RivetReach-0.1.0-alpha-windows-x64.zip**, rather than GitHub’s source archives.

This single-player alpha contains the current survival, building, farming and automation features, including named saves. The original 0.0.1 release remains available as an older session-only archive.

## First expedition

Choose **Start Expedition** to begin empty-handed. Move with WASD, look with the mouse, hold left-click to mine and right-click to place/use. Space jumps, Ctrl sprints and Shift crouches. Open inventory with Tab and the pause menu with Escape. Controls can be rebound in Settings.

Punch a tree, collect logs, then craft planks, sticks and a [Workbench](Item-workbench.md). Place the bench and press **E** or right-click to open 3×3 crafting. The [recipe browser](Crafting-Recipes.md) shows ingredients and required stations. Follow the [field guide](Home.md) for machines, pipes, crops and power.

Actual 0.1.0 release-player capture, October 3, 2026: the scripted Survival route gathers its materials and crafts the Workbench and wooden pickaxe without Creative or supplied ingredients.

![The first Workbench in the 0.1.0 Survival route](images/alpha-0.1.0/survival-workbench.png)

## Save your world

Use **Escape → Save Game**, **Load Game** and **Save & Quit**. **Continue Latest Save** resumes the newest checkpoint from the title screen. There is **no periodic autosave**; closing the window without saving loses changes since the last checkpoint.

Saves live in `%USERPROFILE%\AppData\LocalLow\Starbugstone\Rivet Reach\Saves`. Back up existing local playtest saves before upgrading; this release writes schema 19 and older executables cannot read newer formats. Creative mode/flight reset on load, while stored items and construction persist. No offline production occurs.

## Alpha limits and feedback

The owner requested this release for playtesting while performance, progression/balance, distant scenery and extended-session stability remain under review. **Steady 60 FPS at 1080p is not certified.** Busy factories, weather and travel may stutter. [Frame-pacing settings](Performance-diagnostics.md) offer a display-sync option and several caps, but do not guarantee smoothness. Minimum hardware requirements and a second-machine run remain unverified.

Actual 0.1.0 release-player Settings capture, October 3, 2026. The selected frame limit is a preference, not a measured FPS result.

![Frame-pacing controls in the 0.1.0 release player](images/alpha-0.1.0/settings.png)

Multiplayer, controller support and Linux/macOS builds are not included. [Report issues](https://github.com/Starbugstone/Rivet-Reach/issues) with steps, seed, settings and hardware. The default player log is `%USERPROFILE%\AppData\LocalLow\Starbugstone\Rivet Reach\Player.log`.

The release also provides a source/payload manifest and SHA-256 checksum. [Exact release verification](https://github.com/Starbugstone/Rivet-Reach/blob/main/.docs/verification/ALPHA_0_1_0_RESULTS.md) distinguishes this build’s checks from older feature evidence. Rivet Reach remains proprietary, all rights reserved.
