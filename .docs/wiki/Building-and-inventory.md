# Building and inventory shortcuts

## Familiar inventory controls

The default inventory key is **Tab**, with **E** for Interact. For an E-to-open inventory layout, choose **Escape → Controls → E inventory preset**. It assigns **E** to inventory and **F** to Interact, keeps mouse Use and the 15-slot hotbar, and leaves your other controls alone. If a different custom action already uses E or F, the preset explains the conflict and changes nothing until you rebind that action.

Click or drag to move a stack. Right-click takes the rounded-up half of a stack, or places one held item. Hold right-click and drag across compatible inventory, crafting, chest, crate or machine-input slots to place one per visited slot. Full or incompatible slots are skipped; moving back across a slot does not deposit twice. Machine result slots are excluded from painting.

Shift-click transfers between containers. In your personal inventory, armor equips when its matching equipment slot is empty. Spare armor moves between backpack and hotbar when that equipment slot is occupied; it never replaces your worn armor automatically.

![Controls after selecting the optional E inventory preset](images/release-review/controls-e-inventory-preset.png)

Actual standalone capture, September 20, 2026. Native pointer checks cover right-drag placement, spare-armor transfers and selecting the preset button. The older screenshots elsewhere on this page retain their original capture dates.

## Find recipes and furnace fuel

In the item sidebar, click an item to see how to make it, or right-click to see its uses. You can also hover an item and press **R** for recipes or **U** for uses. Follow ingredient, result or station icons to explore related recipes; **Back** restores the previous item and recipe page.

At an open furnace, select **Recipes & Fuel** to start with a recipe the furnace processes and its fuel choices. The fuel icons are alternatives: choose one. The page arrows still reach every furnace usage, including upgrade recipes that consume a furnace. Right-clicking Furnace in the sidebar keeps the complete ordinary Uses order. Browsing does not move items or craft anything.

![The furnace guide opens a smelting recipe and shows alternative fuels](images/release-review/furnace-recipes-and-fuel.png)

Actual Windows player capture, September 20, 2026. The highlighted log is the consumed ingredient; the row below shows alternative fuels.

## Pick the block you are looking at

Middle-click a world block to select it for building. In Survival, an owned hotbar stack is selected first. If the item is in your backpack, it swaps into the selected hotbar slot. If you do not own it, nothing is created. Creative can supply a usable stack.

![Picking an owned block from the backpack into the hotbar](images/alpha-playtest/pick-block-survival.png)

Plants and torches use their smaller selection shapes. Looking through empty space beside them can target the block behind. Mining, using, harvesting and picking share that selection.

## Organise a container

Middle-click a slot in your hotbar, backpack or an open chest. Compatible partial stacks consolidate, then that section sorts consistently. The hotbar and backpack are organised separately. Individually carried batteries and tanks retain their contents.

![Consolidated stacks in the organised backpack](images/alpha-playtest/inventory-organised.png)

Crafting grids and machine input/output slots keep their positional meaning. Finish moving the stack attached to your pointer before organising.

## Harvest planted crops

Right-click a ripe crop on tilled farmland to harvest its ordinary drops. With a hoe selected, the harvest goes directly into your inventory, with any overflow dropped safely. If your resulting inventory contains the crop's planting item, one is consumed to replant at the initial stage.

![A hoe harvest has consumed one potato to replant the crop](images/alpha-playtest/hoe-harvest-replanted.png)

A full backpack can leave the planting items on the ground, so it cannot guarantee replanting. Wild crops and unripe crops do not use this convenience interaction.

## Player appearance

Both explorer models keep their selected skin visible in the world and inventory. The Alpha correction restores the skin shader's colour and detail when fog is disabled.

![Both explorer models with the field skin in the standalone player](images/alpha-playtest/skin-after.png)

Standalone avatar review capture, September 19, 2026. This shows the two model choices with their existing field skin; it is not new character artwork.

## Movement and hunger

Hold Jump to jump again after each landing. It never supplies an additional jump in mid-air. Ordinary activity and passive hunger expenditure are 25% lower than the initial Alpha 0.0.2 values; healing retains its existing food cost.

[Growing and cooking](Farming-and-cooking.md) · [All items](Items.md) · [Home](Home.md)
