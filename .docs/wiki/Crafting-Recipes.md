# Crafting recipes

This is the player-facing recipe book for the current playable crafting and processing recipes in **Rivet Reach**. Recipes are shown using the same **2×2, 3×3 and 4×4** grid sizes used in game. Where Rivet Reach ships a standalone inventory image, this page uses that exact game asset.

Last checked against `main`: **2026-09-11**.

> **About the artwork:** the industrial/automation items have real exported inventory PNGs under `Assets/RivetReach/Resources/Industry/Icons/`, so those images are used directly below. Early survival items are rendered by the game rather than stored as equivalent standalone inventory PNGs; those slots use the real in-game item names instead of substitute or hand-drawn artwork.

## Crafting stations

| Station | Grid / process | Used for |
|---|:---:|---|
| **Personal crafting** | **2×2** | Early wood recipes, torches and unpacking storage blocks |
| **Workbench** | **3×3** | Tools, armor, storage, furnace/chest, bucket and the Machinist's Bench |
| <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/130.png" width="54" alt="Machinist's Bench"><br>**Machinist's Bench** | **4×4** | Industrial components, machines, networks, tanks and batteries |
| **Furnace** | Processing | Smelting, charcoal, stone, glass and food |
| <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/142.png" width="54" alt="Crusher"><br>**Crusher** | Powered processing | Turns one raw metal into two crushed ore |

### How to read the grids

Each table cell is one crafting slot. Empty cells are empty slots. Shaped recipes may be moved around inside a larger compatible grid. **Axe and Hoe recipes may also be mirrored horizontally.** Shapeless recipes are packed from the top-left below purely for readability; their exact slot position does not matter.

---

## Personal crafting — 2×2

### Planks ×4

**Shapeless**

|  |  |
|:--:|:--:|
| **Log** |  |
|  |  |

→ **Planks ×4**

### Sticks ×4

|  |  |
|:--:|:--:|
| **Planks** |  |
| **Planks** |  |

→ **Sticks ×4**

### Torch ×4 — Coal

|  |  |
|:--:|:--:|
| **Coal** |  |
| **Stick** |  |

→ **Torch ×4**

### Torch ×4 — Charcoal

|  |  |
|:--:|:--:|
| **Charcoal** |  |
| **Stick** |  |

→ **Torch ×4**

### Workbench

|  |  |
|:--:|:--:|
| **Planks** | **Planks** |
| **Planks** | **Planks** |

→ **Workbench ×1**

### Unpacking storage blocks

Storage blocks unpack in Personal Crafting. Put the block in any one slot.

| Put in the grid | Result |
|---|---:|
| Coal Block ×1 | Coal ×9 |
| Copper Block ×1 | Copper Ingot ×9 |
| Iron Block ×1 | Iron Ingot ×9 |
| Gold Block ×1 | Gold Ingot ×9 |
| Diamond Block ×1 | Diamond ×9 |

---

## Workbench — 3×3

### Furnace

|  |  |  |
|:--:|:--:|:--:|
| **Cobblestone** | **Cobblestone** | **Cobblestone** |
| **Cobblestone** |  | **Cobblestone** |
| **Cobblestone** | **Cobblestone** | **Cobblestone** |

→ **Furnace ×1**

### Chest

|  |  |  |
|:--:|:--:|:--:|
| **Planks** | **Planks** | **Planks** |
| **Planks** |  | **Planks** |
| **Planks** | **Planks** | **Planks** |

→ **Chest ×1**

### Bucket

|  |  |  |
|:--:|:--:|:--:|
| **Iron Ingot** |  | **Iron Ingot** |
|  | **Iron Ingot** |  |
|  |  |  |

→ **Bucket ×1**

### Machinist's Bench — shapeless

|  |  |  |
|:--:|:--:|:--:|
| **Workbench ×1** | **Iron Ingot ×4** | **Copper Ingot ×2** |
|  |  |  |
|  |  |  |

→ <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/130.png" width="58" alt="Machinist's Bench"> **Machinist's Bench ×1**

### Lever — shapeless

|  |  |  |
|:--:|:--:|:--:|
| **Stick** | **Cobblestone** | <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/121.png" width="42" alt="Azure Crystal"><br>**Azure Crystal** |
|  |  |  |
|  |  |  |

→ <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/134.png" width="58" alt="Lever"> **Lever ×1**

### Button — shapeless

|  |  |  |
|:--:|:--:|:--:|
| **Stone** | <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/121.png" width="42" alt="Azure Crystal"><br>**Azure Crystal** |  |
|  |  |  |
|  |  |  |

→ <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/135.png" width="58" alt="Button"> **Button ×1**

---

## Tools

All five tool patterns exist in **Wood, Stone, Copper, Iron and Diamond** tiers. Use the corresponding material everywhere `Material` appears.

| Tier | Material |
|---|---|
| Wood | Planks |
| Stone | Cobblestone |
| Copper | Copper Ingot |
| Iron | Iron Ingot |
| Diamond | Diamond |

### Pickaxe

|  |  |  |
|:--:|:--:|:--:|
| **Material** | **Material** | **Material** |
|  | **Stick** |  |
|  | **Stick** |  |

### Axe

|  |  |  |
|:--:|:--:|:--:|
| **Material** | **Material** |  |
| **Material** | **Stick** |  |
|  | **Stick** |  |

The Axe may also be mirrored horizontally.

### Sword

|  |  |  |
|:--:|:--:|:--:|
|  | **Material** |  |
|  | **Material** |  |
|  | **Stick** |  |

### Shovel

|  |  |  |
|:--:|:--:|:--:|
|  | **Material** |  |
|  | **Stick** |  |
|  | **Stick** |  |

### Hoe

|  |  |  |
|:--:|:--:|:--:|
| **Material** | **Material** |  |
|  | **Stick** |  |
|  | **Stick** |  |

The Hoe may also be mirrored horizontally.

---

## Armor

Armor uses the same shaped layouts for **Copper Ingots, Iron Ingots or Diamonds**. Wood and Stone armor do not exist.

### Helmet

|  |  |  |
|:--:|:--:|:--:|
| **Material** | **Material** | **Material** |
| **Material** |  | **Material** |
|  |  |  |

### Chestplate

|  |  |  |
|:--:|:--:|:--:|
| **Material** |  | **Material** |
| **Material** | **Material** | **Material** |
| **Material** | **Material** | **Material** |

### Leggings

|  |  |  |
|:--:|:--:|:--:|
| **Material** | **Material** | **Material** |
| **Material** |  | **Material** |
| **Material** |  | **Material** |

### Boots

|  |  |  |
|:--:|:--:|:--:|
|  |  |  |
| **Material** |  | **Material** |
| **Material** |  | **Material** |

---

## Storage blocks

All five storage blocks use a full 3×3 grid of one material.

|  |  |  |
|:--:|:--:|:--:|
| **Material** | **Material** | **Material** |
| **Material** | **Material** | **Material** |
| **Material** | **Material** | **Material** |

| Material | Output |
|---|---|
| Coal | Coal Block ×1 |
| Copper Ingot | Copper Block ×1 |
| Iron Ingot | Iron Block ×1 |
| Gold Ingot | Gold Block ×1 |
| Diamond | Diamond Block ×1 |

Each storage block can be unpacked back into **9** of its material with Personal Crafting.

---

# Machinist's Bench — 4×4

<img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/130.png" width="90" alt="Machinist's Bench">

The Machinist's Bench is the main industrial crafting station. **Unless a recipe is explicitly shaped, position does not matter.** All industrial images below are the actual inventory PNGs used by Rivet Reach.

## Core industrial components

| In-game item | Recipe | Output |
|---|---|---:|
| <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/122.png" width="60" alt="Copper Wire"><br>**Copper Wire** | 1 Copper Ingot, shapeless | 4 |
| <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/123.png" width="60" alt="Copper Plate"><br>**Copper Plate** | 3 Copper Ingots in one horizontal row | 3 |
| <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/124.png" width="60" alt="Iron Plate"><br>**Iron Plate** | 3 Iron Ingots in one horizontal row | 3 |
| <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/125.png" width="60" alt="Iron Cog"><br>**Iron Cog** | 2 Iron Ingots in one horizontal row | 1 |
| <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/126.png" width="60" alt="Rivets"><br>**Rivets** | 1 Iron Ingot, shapeless | 8 |
| <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/127.png" width="60" alt="Machine Casing"><br>**Machine Casing** | 4 Iron Plates + 4 Rivets | 1 |
| <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/131.png" width="60" alt="Signal Wire"><br>**Signal Wire** | 1 Copper Wire + 1 Azure Crystal | 4 |
| <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/132.png" width="60" alt="Signal Conduit"><br>**Signal Conduit** | 2 Copper Plates + 1 Signal Wire | 2 |
| <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/138.png" width="60" alt="Power Cable"><br>**Power Cable** | 4 Copper Wire + 1 Plank | 4 |

### Shaped component layouts

**Copper / Iron Plate ×3**

|  |  |  |  |
|:--:|:--:|:--:|:--:|
| **Ingot** | **Ingot** | **Ingot** |  |
|  |  |  |  |
|  |  |  |  |
|  |  |  |  |

**Iron Cog ×1**

|  |  |  |  |
|:--:|:--:|:--:|:--:|
| **Iron Ingot** | **Iron Ingot** |  |  |
|  |  |  |  |
|  |  |  |  |
|  |  |  |  |

## Blue Signal and workshop controls

| In-game item | Ingredients | Output |
|---|---|---:|
| <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/133.png" width="60" alt="Signal Relay"><br>**Signal Relay** | 2 Signal Conduits + 1 Azure Crystal + 1 Iron Plate | 1 |
| <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/136.png" width="60" alt="Signal Indicator"><br>**Signal Indicator** | 1 Glass + 1 Azure Crystal + 1 Copper Wire | 1 |
| <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/137.png" width="60" alt="Workshop Hatch"><br>**Workshop Hatch** | 4 Iron Plates + 1 Iron Cog + 1 Signal Wire | 1 |
| <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/139.png" width="60" alt="Workshop Lamp"><br>**Workshop Lamp** | 2 Glass + 2 Copper Wire + 1 Iron Plate | 1 |

> **Lever** and **Button** are Workbench recipes and are shown in the 3×3 section above.

## Machines and transport

| In-game item | Ingredients | Output |
|---|---|---:|
| <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/140.png" width="62" alt="Boiler Engine"><br>**Boiler Engine** | 1 Machine Casing + 4 Copper Plates + 2 Iron Cogs | 1 |
| <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/141.png" width="62" alt="Alternator"><br>**Alternator** | 1 Machine Casing + 2 Iron Cogs + 8 Copper Wire | 1 |
| <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/142.png" width="62" alt="Crusher"><br>**Crusher** | 1 Machine Casing + 2 Iron Cogs + 4 Iron Plates | 1 |
| <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/143.png" width="62" alt="Pump"><br>**Pump** | 1 Machine Casing + 1 Iron Cog + 4 Copper Wire + 2 Copper Plates | 1 |
| <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/144.png" width="62" alt="Drill"><br>**Drill** | 1 Machine Casing + 2 Iron Cogs + 4 Iron Plates + 4 Copper Wire | 1 |
| <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/145.png" width="62" alt="Water Tank"><br>**Water Tank** | 4 Copper Plates + 2 Iron Plates | 1 |
| <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/146.png" width="62" alt="Item Pipe"><br>**Item Pipe** | 2 Iron Plates + 2 Copper Wire | 4 |
| <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/147.png" width="62" alt="Fluid Pipe"><br>**Fluid Pipe** | 2 Copper Plates | 4 |
| <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/148.png" width="62" alt="Extractor"><br>**Extractor** | 1 Iron Cog + 2 Copper Wire | 1 |
| <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/149.png" width="62" alt="Inventory Sensor"><br>**Inventory Sensor** | 2 Signal Wire + 1 Copper Plate + 1 Glass | 1 |

## Multiblock tank parts

| In-game item | Ingredients | Output |
|---|---|---:|
| <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/160.png" width="62" alt="Reinforced Tank Frame"><br>**Reinforced Tank Frame** | 3 Iron Plates + 4 Rivets + 1 Copper Plate | 4 |
| <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/161.png" width="62" alt="Tank Wall"><br>**Tank Wall** | 1 Machine Casing + 4 Iron Plates | 4 |
| <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/162.png" width="62" alt="Reinforced Tank Glass"><br>**Reinforced Tank Glass** | 4 Glass + 4 Rivets | 4 |
| <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/163.png" width="62" alt="Tank Controller"><br>**Tank Controller** | 1 Tank Wall + 1 Iron Cog + 1 Azure Crystal | 1 |
| <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/164.png" width="62" alt="Tank Fluid Port"><br>**Tank Fluid Port** | 1 Tank Wall + 1 Fluid Pipe | 1 |
| <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/165.png" width="62" alt="Tank Access Hatch"><br>**Tank Access Hatch** | 1 Tank Wall + 2 Copper Plates | 1 |
| <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/166.png" width="62" alt="Signal Valve Port"><br>**Signal Valve Port** | 1 Tank Fluid Port + 1 Signal Conduit | 1 |
| <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/167.png" width="62" alt="Tank Level Sensor"><br>**Tank Level Sensor** | 1 Tank Wall + 1 Signal Wire + 1 Glass | 1 |

For shell construction, orientation and capacity rules, see [Build a multiblock tank](Tanks.md).

## Batteries

| In-game item | Ingredients | Output |
|---|---|---:|
| <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/168.png" width="64" alt="Battery Block"><br>**Battery Block** | 1 Machine Casing + 2 Copper Plates + 4 Copper Wire + 2 Coal | 1 |
| <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/169.png" width="64" alt="Battery Bank Controller"><br>**Battery Bank Controller** | 1 Machine Casing + 4 Copper Wire + 1 Azure Crystal + 1 Glass | 1 |

For battery-bank construction, power flow and controller behaviour, see [Electricity and batteries](Electricity-and-batteries.md).

---

# Furnace recipes

All current Furnace recipes take **200 simulation ticks / 10 seconds** per item.

| Input |  | Output |
|---|:---:|---|
| Log | → | Charcoal |
| Raw Copper | → | Copper Ingot |
| Raw Iron | → | Iron Ingot |
| Raw Gold | → | Gold Ingot |
| Cobblestone | → | Stone |
| Potato | → | Baked Potato |
| <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/120.png" width="48" alt="Azure Ore"><br>Azure Ore | → | <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/121.png" width="48" alt="Azure Crystal"><br>Azure Crystal |
| Sand | → | <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/128.png" width="48" alt="Glass"><br>Glass |
| <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/150.png" width="48" alt="Crushed Copper"><br>Crushed Copper | → | Copper Ingot |
| <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/151.png" width="48" alt="Crushed Iron"><br>Crushed Iron | → | Iron Ingot |
| <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/152.png" width="48" alt="Crushed Gold"><br>Crushed Gold | → | Gold Ingot |

## Furnace fuels

| Fuel | Burn time | Approx. full 10-second recipes |
|---|---:|---:|
| Coal | 1,600 ticks | 8 |
| Charcoal | 1,600 ticks | 8 |
| Coal Block | 16,000 ticks | 80 |
| Log | 300 ticks | 1.5 |
| Planks | 300 ticks | 1.5 |
| Stick | 100 ticks | 0.5 |
| Wooden Axe / Pickaxe / Sword / Shovel / Hoe | 200 ticks | 1 |

Fuel keeps burning after ignition even if the Furnace becomes temporarily idle.

---

# Crusher recipes

<img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/142.png" width="80" alt="Crusher">

The Crusher draws **160 W** and takes **5 seconds at full power** for one operation.

| Input |  | Output |
|---|:---:|---|
| Raw Copper | → | <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/150.png" width="52" alt="Crushed Copper"><br>**Crushed Copper ×2** |
| Raw Iron | → | <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/151.png" width="52" alt="Crushed Iron"><br>**Crushed Iron ×2** |
| Raw Gold | → | <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/152.png" width="52" alt="Crushed Gold"><br>**Crushed Gold ×2** |

Each Crushed Ore then smelts into one matching ingot in the Furnace, so routing raw metal through the Crusher **doubles ingot yield** compared with direct smelting.

---

## Using the in-game recipe browser

Open the inventory or a station and use the item catalog on the right.

- Click an item to show its recipes.
- **Shift-click / right-click** an item to show its uses.
- Hover an item and press **R** for recipes or **U** for uses.
- **Ctrl-click** a compatible recipe to fill one craft into the currently open grid when you have the ingredients.

The browser is the live source of truth and shows the required station, exact layout, stack quantities and recipe alternatives.

## Notes

- Only recipes registered in the active catalogs are listed here.
- Historical `Starter axe`, `Starter pickaxe` and `Starter dagger` definition files are legacy verification content and are **not normal playable recipes**.
- A shaped grid can be shifted within a larger compatible grid; horizontal mirroring only applies where the recipe explicitly allows it.
- Furnace and Crusher processing are machine recipes, not crafting-grid recipes.
- This page deliberately does **not** embed the verification-run screenshots: those captures are stored as verification artifacts and are not dependable Wiki image sources.
- No replacement artwork is invented for survival items that do not have a standalone shipped inventory PNG. Industrial images come directly from `Assets/RivetReach/Resources/Industry/Icons/`.

For implementation details, the authoritative recipe content lives in `Assets/RivetReach/Resources/Definitions/Recipes/`, `Definitions/Recipes.asset` and `Definitions/Processing.asset`.
