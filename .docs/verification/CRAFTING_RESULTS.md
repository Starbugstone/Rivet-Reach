# Current crafting recipes and block interaction

The implemented starter recipes use the same ingredient positions and output quantities as the corresponding Minecraft recipes. This verifies actual versioned recipe assets and a Windows player run. Superseded stone/log starter recipes and old screenshots are no longer presented as the current game.

## Recipe audit

All **52 active grid recipes** pass **1,744 independent acceptance checks**. [The checked recipe matrix](crafting/starter-recipe-checks.txt) lists each output, quantity and layout. Fixtures are authored independently of the catalog; they exercise the compiled matcher, every fitting translation across 2×2/3×3/4×4 grids, permitted mirrors and exact input consumption/output. They reject horizontal-plank sticks, logs substituted for workbench planks, and the obsolete stone/log starter axe.

A separate [reference comparison](crafting/minecraft-recipe-comparison.json) compares all 52 selected assets with [Mojang's official recipe samples](https://github.com/Mojang/bedrock-samples/tree/736072450c26a7c67f07b1661f29d9a5ebaa14b1/behavior_pack/recipes), pinned to commit `736072450c26a7c67f07b1661f29d9a5ebaa14b1`. The report records source URLs and hashes. Wood/stone tags map to the material types implemented in Rivet Reach; the single-cell shaped and shapeless forms are behaviorally equivalent here. No external source, recipe asset, artwork or dependency was imported into the game.

| Beginning recipe | Exact layout and output |
| --- | --- |
| Planks | One log in any cell → four planks |
| Sticks | Two vertically adjacent planks → four sticks |
| Workbench / crafting table | Four planks filling a 2×2 square → one workbench |
| Pickaxe | Three materials across the top, two sticks down the center → one tool |
| Axe | Three materials in an upper corner L, two sticks as its handle → one tool; mirror accepted |
| Sword | Two materials vertically above one stick → one tool |
| Shovel | One material vertically above two sticks → one tool |
| Hoe | Two materials across the top above a two-stick handle → one tool; mirror accepted |
| Furnace | Eight cobblestones around an empty center → one furnace |
| Chest | Eight planks around an empty center → one chest |

Tool materials are planks, cobblestone, copper ingots, iron ingots and diamonds. The audit also covers copper/iron/diamond armor and coal/copper/iron/gold/diamond storage-block packing/unpacking. This is the selected 52-recipe subset, not Minecraft's complete item catalog or all alternative ingredient variants. Durability, combat tuning, artwork and inventory size are separate project rules. [Minecraft's crafting guide](https://www.minecraft.net/en-us/article/how-craft), [sticks](https://www.minecraft.net/en-us/article/taking-inventory--stick) and [the official shaped-recipe documentation](https://learn.microsoft.com/en-us/minecraft/creator/reference/content/recipereference/examples/recipedefinitions/minecraftrecipe_shaped?view=minecraft-bedrock-stable) explain the starting/reference behavior.

## Actual input and screenshots

The final Windows run passed **44 assertions with zero runtime errors** on 2026-09-09 at 17:07 UTC. It exercises three input logs → twelve planks → workbench and sticks → world placement → opening the nine-slot workbench → a wooden pickaxe. Only input logs and a supported test floor are supplied as fixtures. Actual virtual keyboard/mouse events go through UI raycasting, result transfer, hotbar placement and player interaction; the test does not spawn the finished workbench or directly fill its pickaxe grid.

**E** is the default rebindable Interact key. Mouse Use (right-click by default) also opens a station before placing a held block. Crouch + Use places against stations. Tests cover an empty hand, holding a block, all nine slots, occlusion, interaction with an exposed top whose center is hidden, five-block reach and rebinding. Direct station-open commands also reject an obstructed address. The same station authority serves workbenches, furnaces and chests.

| Personal crafting | Workbench |
| --- | --- |
| ![One log makes four planks](crafting/starter-planks.png) | ![Placed workbench interaction prompt](crafting/starter-interact.png) |
| ![Four planks make one workbench](crafting/starter-workbench.png) | ![Three planks and two sticks make a wooden pickaxe in the nine-slot workbench](crafting/starter-pickaxe.png) |
| ![Two vertical planks make four sticks](crafting/starter-sticks.png) | ![Rebindable interaction control](crafting/starter-controls.png) |

## Verification and reproduction

The final Unity build succeeded in **17.881 seconds with zero errors and zero warnings**. The compiled crafting suite passed **1,158,103 assertions**, the integrated domain suite **92,338**, and survival checks **47,146**, including the 1,744 independent recipe checks. The same executable passed the **78-check survival runtime**.

[Build context](crafting/build-context.json) identifies the exact executable and final input reports. [Build summary](crafting/build-summary.txt), [crafting core checks](crafting/crafting-checks.txt), [domain checks](crafting/domain-checks.txt), [starter input checks](crafting/runtime-report.json) and [survival regression](survival/runtime-report.json) preserve measured evidence. Focused reports do not measure complete frame performance; zero-valued unused frame fields mean unmeasured.

The reviewed player used an i7-10750H, RTX 2060, 32 GB RAM, 1280×720 and view radius 10. The starter workload waited 26.605 seconds for complete initial terrain demand and peaked at 3,301 resident chunks; that barrier is not the earliest playable-frame time. An earlier capture attempt ended in a native UnityPlayer crash while another automated Unity job was running. The recorded final runs completed with those jobs serialized; the native crash cause was not established.

Build the pinned Unity **6000.4.4f1** project, then run:

```powershell
.\Tools\Verify-POC.ps1 -StarterCrafting -Executable Builds/Survival/RivetReach.exe
.\Tools\Verify-POC.ps1 -Survival -Executable Builds/Survival/RivetReach.exe
```

[CRAFTING.md](../CRAFTING.md) owns editable recipe assets, the factory/compiled registry and transaction boundaries. [Current run instructions](../FIRST_POC.md) identify the review executable and controls. The 4×4 engine is checked, but only personal 2×2 and workbench 3×3 interfaces are implemented. Session progress resets on quit; play balance and visual acceptance remain subject to user review.
