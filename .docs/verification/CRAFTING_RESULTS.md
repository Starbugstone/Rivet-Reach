# Starter crafting and block-interaction evidence

The September 9, 2026 results below verify the then-current Rivet Reach starter recipe assets and Windows player. They retain their original build identity and counts. [The current crafting contract](../CRAFTING.md#starter-recipe-and-block-interaction-acceptance) owns recipe acceptance; the October 2 issue #1 cleanup retires external recipe equality as a specification. Familiar layouts remain deliberate project choices.

## Current contract revision — 2026-10-02

[Issue #1 verification](CRAFTING_IDENTITY_RESULTS.md) records the independent recipe contract, Cog/Rivet changes, save compatibility and current UI captures. The dated evidence below remains tied to its original executable.

## Recipe audit

The original **52-recipe starter catalogue** passed **1,744 independent acceptance checks** in that build. [The checked recipe matrix](crafting/starter-recipe-checks.txt) lists each output, quantity and layout. Fixtures are authored independently of the catalog; they exercise the compiled matcher, every fitting translation across 2×2/3×3/4×4 grids, permitted mirrors and exact input consumption/output. They reject horizontal-plank sticks, logs substituted for workbench planks, and the obsolete stone/log starter axe.

The former external recipe-comparison report has been removed from maintained evidence and remains in Git history. The independent project fixtures and dated gameplay reports below remain useful; removing the comparison adds no new gameplay or performance measurement.

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

Tool materials are planks, cobblestone, copper ingots, iron ingots and diamonds. The audit also covers copper/iron/diamond armor and coal/copper/iron/gold/diamond storage-block packing/unpacking. This is the original 52-recipe subset, not the complete current catalogue. Durability, combat tuning, artwork and inventory size follow their own project specifications.

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

[CRAFTING.md](../CRAFTING.md) owns editable recipe assets, the factory/compiled registry and transaction boundaries. [Current run instructions](../FIRST_POC.md) identify the review executable and controls. That historical build checked the 4×4 engine but exposed only personal 2×2 and Workbench 3×3 interfaces, with session-only progress. The current game also has the [4×4 Machinist’s Bench](../INDUSTRY.md) and [durable saves](../SAVES.md); the older captures do not verify those later features. Play balance and visual acceptance remain subject to user review.

## Subsequent bucket recipe

The fluid increment adds the three-iron bucket as recipe 53. [Fluid verification](FLUID_RESULTS.md) records the extended independent acceptance checks and bucket player interaction. Earlier crafting screenshots retain their original 52-recipe build identity.

The torch increment adds separate coal and charcoal recipes, taking the catalog to **55 recipes**. [Torch verification](TORCH_RESULTS.md) records the latest 1,862 independent acceptance checks and the actual lighting/placement build. The screenshots above retain their dated 52-recipe artifact identity.


## Shift crafting with an occupied cursor — 2026-09-10

Shift-clicking a crafting result now sends the batch directly to inventory while preserving the cursor stack. Personal crafting and all crafting benches share the corrected output handler. [GAMEPLAY.md](../GAMEPLAY.md#16-modular-grid-crafting) owns the interaction rule.

The focused Windows player passed **38 personal-crafting checks** and **45 starter/workbench checks**, with **zero runtime errors**. Real pointer input covers an unrelated held workbench, a full matching plank cursor stack, and full-inventory rejection without ingredient consumption or cursor changes. At a placed 3×3 workbench, Shift-click crafts a wooden pickaxe into inventory while two sticks remain on the cursor. The 4×4 interface shares the handler but was not separately pointer-tested in this run.

[Personal input results](crafting/cursor-shift/personal-runtime-report.json), [workbench input results](crafting/cursor-shift/workbench-runtime-report.json), [crafting core checks](crafting/cursor-shift/crafting-checks.txt) and [build summary](crafting/cursor-shift/build-summary.txt) record **1,158,808 core assertions** and a successful Unity 6000.4.4f1 build with zero errors/warnings. [Build identity and limits](crafting/cursor-shift/build-context.json) record the source snapshot and binary hashes. Verification used an isolated project copy to preserve the active Editor Play session; concurrent recipe-browser and UI performance changes made afterwards are outside this evidence. Earlier screenshots above retain their original build identity.

The local verified player is `Builds/CraftingCursor/RivetReach.exe`. Reproduce with `Tools/Verify-POC.ps1 -Crafting` and `-StarterCrafting`, each with `-Executable Builds/CraftingCursor/RivetReach.exe` and a separate output directory.
