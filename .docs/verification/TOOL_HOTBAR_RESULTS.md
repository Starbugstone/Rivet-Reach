# Faster swings and starter-tool hotbar — 2026-09-08

The user requested at least 2–2.5× faster punching/axe swings and then asked to make the existing pickaxe and dagger review props available in the hotbar. Runtime strike playback and its attack/recovery transitions now run at **2.5× speed** in both first-person arms and the body. The 0.3-second punch/block clips play over 0.12 seconds; the 0.6-second tool clips over 0.24 seconds. Block break durations remain definition-driven and independent of visual swings.

New expeditions provide a dagger in slot 10 (`0`), pickaxe in slot 11 and axe in slot 12. Mouse wheel, `[ / ]` and inventory selection reach all three. Each item has a one-item stack limit and its own inventory icon. The dagger reuses `GripSword` with the one-hand grip; the pickaxe reuses `GripPickaxe` with the two-hand grip. Their editable sources, geometry and imported clips are preserved. [The icon script](../../Tools/render_starter_tool_icons.py) renders the original prop meshes without rewriting them.

The pickaxe and dagger use ordinary single-block mining and cannot be placed as terrain. Neither has the Axe capability. Generated-tree felling and protection of player-placed wood retain the [tree rules](../GAMEPLAY.md#trees-and-axe-felling--user-feedback-extension). This request adds selectable starter items, not dagger combat, recipes or tool progression. Existing sessions receive the starter items on their next new expedition; there is no durable-save migration.

## Verification

The pinned Unity 6000.4.4f1 Windows development build succeeded with zero warnings/errors in the isolated local checkout, preserving the open Editor. The domain suite passed 54,283 assertions. The [focused runtime suite](starter-tools-runtime.json) passed **165 assertions**, including real hotbar selection, the one-/two-hand grips, swing recovery, new tool capability boundaries and generated/placed-wood protection. The [full regression](starter-tools-full-runtime.json) passed **263 assertions**, covering live punches, held-item contacts, appearance changes, movement, inventory conservation, item placement and grass. Both runs finished without logged errors. The local `Builds/PlayerRevision4` player was replaced with the verified build. Artistic feel remains for user review; timing configuration and passing checks do not establish subjective acceptance or an FPS improvement.

## In-game captures

![Starter dagger selected from the hotbar](starter-dagger-hotbar.png)

![Starter pickaxe selected with its two-hand grip](starter-pickaxe-hotbar.png)
