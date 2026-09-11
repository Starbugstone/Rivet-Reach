# Crafting recipes

This page lists the current playable crafting and processing recipes in **Rivet Reach**. It is intended as a quick player reference; the in-game item browser remains the live source of truth when recipes change.

Last checked against `main`: **2026-09-11**.

## Crafting stations

| Station | Grid / process | Used for |
|---|---:|---|
| Personal crafting | 2×2 | Early wood recipes, torches and unpacking storage blocks |
| Workbench | 3×3 | Tools, armor, storage, furnace/chest, bucket and the Machinist's Bench |
| Machinist's Bench | 4×4 | Industrial components, machines, networks, tanks and batteries |
| Furnace | Processing | Smelting, charcoal, stone, glass and food |
| Crusher | Powered processing | Turns raw metal into two crushed ore |

For shaped recipes, `·` means an empty slot. A shaped recipe may be moved around inside a larger compatible grid. **Axe and hoe recipes may also be mirrored horizontally.** Industrial recipes are shapeless unless a layout is explicitly shown.

---

## Personal crafting — 2×2

| Output | Ingredients / layout | Output count |
|---|---|---:|
| Planks | 1 Log, shapeless | 4 |
| Sticks | 2 Planks vertically | 4 |
| Torch | 1 Coal directly above 1 Stick | 4 |
| Torch | 1 Charcoal directly above 1 Stick | 4 |
| Workbench | 2×2 Planks | 1 |
| Coal | 1 Coal Block | 9 |
| Copper Ingot | 1 Copper Block | 9 |
| Iron Ingot | 1 Iron Block | 9 |
| Gold Ingot | 1 Gold Block | 9 |
| Diamond | 1 Diamond Block | 9 |

### Basic layouts

**Sticks**

```text
P
P
```

**Torch** — use Coal or Charcoal for `F`.

```text
F
S
```

**Workbench**

```text
PP
PP
```

---

## Workbench — 3×3

### Utility blocks

| Output | Ingredients / layout | Output count |
|---|---|---:|
| Furnace | 8 Cobblestone in a 3×3 ring, centre empty | 1 |
| Chest | 8 Planks in a 3×3 ring, centre empty | 1 |
| Bucket | 3 Iron Ingots in a bucket shape | 1 |
| Machinist's Bench | 1 Workbench + 4 Iron Ingots + 2 Copper Ingots, shapeless | 1 |
| Lever | 1 Stick + 1 Cobblestone + 1 Azure Crystal, shapeless | 1 |
| Button | 1 Stone + 1 Azure Crystal, shapeless | 1 |

**Furnace / Chest ring** — use Cobblestone for the Furnace or Planks for the Chest.

```text
MMM
M·M
MMM
```

**Bucket**

```text
I·I
·I·
```

### Tools

All five tool types exist in **Wood, Stone, Copper, Iron and Diamond** tiers.

| Tier | `M` material |
|---|---|
| Wood | Planks |
| Stone | Cobblestone |
| Copper | Copper Ingot |
| Iron | Iron Ingot |
| Diamond | Diamond |

**Pickaxe**

```text
MMM
·S·
·S·
```

**Axe** — horizontal mirror is also valid.

```text
MM·
MS·
·S·
```

**Sword**

```text
M
M
S
```

**Shovel**

```text
M
S
S
```

**Hoe** — horizontal mirror is also valid.

```text
MM·
·S·
·S·
```

### Armor

Armor can be crafted from **Copper Ingots, Iron Ingots or Diamonds**. Wood and Stone armor do not exist.

**Helmet**

```text
MMM
M·M
```

**Chestplate**

```text
M·M
MMM
MMM
```

**Leggings**

```text
MMM
M·M
M·M
```

**Boots**

```text
M·M
M·M
```

### Storage blocks

| Output | Recipe | Output count |
|---|---|---:|
| Coal Block | 3×3 Coal | 1 |
| Copper Block | 3×3 Copper Ingots | 1 |
| Iron Block | 3×3 Iron Ingots | 1 |
| Gold Block | 3×3 Gold Ingots | 1 |
| Diamond Block | 3×3 Diamonds | 1 |

Each storage block can be unpacked back into **9** of its original material using personal crafting.

---

## Machinist's Bench — 4×4

The Machinist's Bench is the main industrial crafting station. Most recipes below are **shapeless**, so ingredient position does not matter. The few shaped component recipes are shown explicitly.

### Core industrial components

| Output | Ingredients / layout | Output count |
|---|---|---:|
| Copper Wire | 1 Copper Ingot | 4 |
| Copper Plate | 3 Copper Ingots in one horizontal row | 3 |
| Iron Plate | 3 Iron Ingots in one horizontal row | 3 |
| Iron Cog | 2 Iron Ingots in one horizontal row | 1 |
| Rivets | 1 Iron Ingot | 8 |
| Machine Casing | 4 Iron Plates + 4 Rivets | 1 |
| Signal Wire | 1 Copper Wire + 1 Azure Crystal | 4 |
| Signal Conduit | 2 Copper Plates + 1 Signal Wire | 2 |
| Power Cable | 4 Copper Wire + 1 Plank | 4 |

**Copper / Iron Plate**

```text
MMM
```

**Iron Cog**

```text
II
```

### Blue Signal and workshop controls

| Output | Ingredients | Output count |
|---|---|---:|
| Signal Relay | 2 Signal Conduits + 1 Azure Crystal + 1 Iron Plate | 1 |
| Signal Indicator | 1 Glass + 1 Azure Crystal + 1 Copper Wire | 1 |
| Workshop Hatch | 4 Iron Plates + 1 Iron Cog + 1 Signal Wire | 1 |
| Workshop Lamp | 2 Glass + 2 Copper Wire + 1 Iron Plate | 1 |

> The **Lever** and **Button** are 3×3 Workbench recipes and are listed in the Workbench section above.

### Machines and transport

| Output | Ingredients | Output count |
|---|---|---:|
| Boiler Engine | 1 Machine Casing + 4 Copper Plates + 2 Iron Cogs | 1 |
| Alternator | 1 Machine Casing + 2 Iron Cogs + 8 Copper Wire | 1 |
| Crusher | 1 Machine Casing + 2 Iron Cogs + 4 Iron Plates | 1 |
| Pump | 1 Machine Casing + 1 Iron Cog + 4 Copper Wire + 2 Copper Plates | 1 |
| Drill | 1 Machine Casing + 2 Iron Cogs + 4 Iron Plates + 4 Copper Wire | 1 |
| Water Tank | 4 Copper Plates + 2 Iron Plates | 1 |
| Item Pipe | 2 Iron Plates + 2 Copper Wire | 4 |
| Fluid Pipe | 2 Copper Plates | 4 |
| Extractor | 1 Iron Cog + 2 Copper Wire | 1 |
| Inventory Sensor | 2 Signal Wire + 1 Copper Plate + 1 Glass | 1 |

### Multiblock tank parts

| Output | Ingredients | Output count |
|---|---|---:|
| Reinforced Tank Frame | 3 Iron Plates + 4 Rivets + 1 Copper Plate | 4 |
| Tank Wall | 1 Machine Casing + 4 Iron Plates | 4 |
| Reinforced Tank Glass | 4 Glass + 4 Rivets | 4 |
| Tank Controller | 1 Tank Wall + 1 Iron Cog + 1 Azure Crystal | 1 |
| Tank Fluid Port | 1 Tank Wall + 1 Fluid Pipe | 1 |
| Tank Access Hatch | 1 Tank Wall + 2 Copper Plates | 1 |
| Signal Valve Port | 1 Tank Fluid Port + 1 Signal Conduit | 1 |
| Tank Level Sensor | 1 Tank Wall + 1 Signal Wire + 1 Glass | 1 |

For the actual shell construction rules, see [Build a multiblock tank](Tanks.md).

### Batteries

| Output | Ingredients | Output count |
|---|---|---:|
| Battery Block | 1 Machine Casing + 2 Copper Plates + 4 Copper Wire + 2 Coal | 1 |
| Battery Bank Controller | 1 Machine Casing + 4 Copper Wire + 1 Azure Crystal + 1 Glass | 1 |

For bank construction and electrical behaviour, see [Electricity and batteries](Electricity-and-batteries.md).

---

## Furnace recipes

All current furnace recipes take **200 simulation ticks / 10 seconds** per item.

| Input | Output |
|---|---|
| Log | Charcoal |
| Raw Copper | Copper Ingot |
| Raw Iron | Iron Ingot |
| Raw Gold | Gold Ingot |
| Cobblestone | Stone |
| Potato | Baked Potato |
| Azure Ore | Azure Crystal |
| Sand | Glass |
| Crushed Copper | Copper Ingot |
| Crushed Iron | Iron Ingot |
| Crushed Gold | Gold Ingot |

### Furnace fuels

| Fuel | Burn time | Approx. full 10-second recipes |
|---|---:|---:|
| Coal | 1,600 ticks | 8 |
| Charcoal | 1,600 ticks | 8 |
| Coal Block | 16,000 ticks | 80 |
| Log | 300 ticks | 1.5 |
| Planks | 300 ticks | 1.5 |
| Stick | 100 ticks | 0.5 |
| Wooden Axe / Pickaxe / Sword / Shovel / Hoe | 200 ticks | 1 |

Fuel keeps burning after ignition even if the furnace becomes temporarily idle.

---

## Crusher recipes

The Crusher draws **160 W** and takes **5 seconds at full power** for one operation.

| Input | Output | Output count |
|---|---|---:|
| Raw Copper | Crushed Copper | 2 |
| Raw Iron | Crushed Iron | 2 |
| Raw Gold | Crushed Gold | 2 |

Each Crushed Ore then smelts into one matching ingot in the Furnace, making the Crusher the higher-yield metal-processing route.

---

## Using the in-game recipe browser

Open the inventory or a station and use the item catalog on the right. Clicking an item shows recipes; **Shift-click / right-click** shows uses. Hover an item and press **R** for recipes or **U** for uses. **Ctrl-click** can fill one compatible recipe into the currently open crafting grid when you have the ingredients.

The browser shows the exact required station, layout, quantities and alternative recipes. It is particularly useful for 4×4 industrial recipes and for following intermediate components such as plates, wire, rivets and Machine Casings.

## Notes

- Only recipes registered in the active catalogs are listed here. Historical `Starter axe`, `Starter pickaxe` and `Starter dagger` definition files are legacy verification content and are **not normal playable recipes**.
- Grid recipes consume the quantities shown in each occupied slot. A recipe cannot be substituted with a different material unless that alternative has its own registered recipe.
- Horizontal mirroring is only available where the recipe explicitly allows it.
- Machine processing is separate from grid crafting: Furnace and Crusher recipes cannot be performed by simply arranging their input in a crafting grid.

For implementation details, the authoritative content lives in `Assets/RivetReach/Resources/Definitions/Recipes/`, `Definitions/Recipes.asset` and `Definitions/Processing.asset`.