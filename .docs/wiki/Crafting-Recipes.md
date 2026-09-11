# Crafting recipes

This page lists the current playable crafting and processing recipes in **Rivet Reach**. It is intended as a player-facing recipe book; the in-game recipe browser remains the live source of truth when recipes change.

Last checked against `main`: **2026-09-11**.

## Crafting in game

The screenshots below come from the Windows player verification run and show the same crafting interface used in-game.

<table>
<tr>
<td align="center"><img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/.docs/verification/crafting/starter-planks.png" width="280"><br><b>Log → Planks</b></td>
<td align="center"><img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/.docs/verification/crafting/starter-sticks.png" width="280"><br><b>Planks → Sticks</b></td>
</tr>
<tr>
<td align="center"><img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/.docs/verification/crafting/starter-workbench.png" width="280"><br><b>Build a Workbench</b></td>
<td align="center"><img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/.docs/verification/crafting/starter-pickaxe.png" width="280"><br><b>Craft your first Pickaxe</b></td>
</tr>
</table>

## Crafting stations

| Station | Grid / process | Used for |
|---|---:|---|
| **Personal crafting** | 2×2 | Early wood recipes, torches and unpacking storage blocks |
| **Workbench** | 3×3 | Tools, armor, storage, furnace/chest, bucket and the Machinist's Bench |
| <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/130.png" width="54"><br>**Machinist's Bench** | 4×4 | Industrial components, machines, networks, tanks and batteries |
| **Furnace** | Processing | Smelting, charcoal, stone, glass and food |
| <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/142.png" width="54"><br>**Crusher** | Powered processing | Turns one raw metal into two crushed ore |

For shaped recipes, `·` means an empty slot. A shaped recipe may be moved around inside a larger compatible grid. **Axe and hoe recipes may also be mirrored horizontally.** Industrial recipes are shapeless unless a layout is explicitly shown.

---

## Personal crafting — 2×2

The first few minutes of an expedition follow a simple chain: **Log → Planks → Sticks → Workbench → tools**. The screenshots at the top of this page show that flow in the actual crafting UI.

| Output | Ingredients / layout | Output count |
|---|---|---:|
| **Planks** | 1 Log, shapeless | 4 |
| **Sticks** | 2 Planks vertically | 4 |
| **Torch** | 1 Coal directly above 1 Stick | 4 |
| **Torch** | 1 Charcoal directly above 1 Stick | 4 |
| **Workbench** | 2×2 Planks | 1 |
| **Coal** | 1 Coal Block | 9 |
| **Copper Ingot** | 1 Copper Block | 9 |
| **Iron Ingot** | 1 Iron Block | 9 |
| **Gold Ingot** | 1 Gold Block | 9 |
| **Diamond** | 1 Diamond Block | 9 |

### Basic layouts

<table>
<tr>
<th>Sticks ×4</th>
<th>Torch ×4</th>
<th>Workbench ×1</th>
</tr>
<tr>
<td>

```text
P
P
```

</td>
<td>

```text
F
S
```

</td>
<td>

```text
PP
PP
```

</td>
</tr>
</table>

`P` = Planks · `S` = Stick · `F` = Coal or Charcoal.

---

## Workbench — 3×3

The Workbench opens the full 3×3 grid and is where survival crafting expands into tools, armor and the first industrial station.

<p align="center">
  <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/.docs/verification/crafting/starter-pickaxe.png" alt="Pickaxe recipe in the Rivet Reach workbench" width="520">
</p>

### Utility blocks

| In-game item | Ingredients / layout | Output |
|---|---|---:|
| **Furnace** | 8 Cobblestone in a 3×3 ring, centre empty | 1 |
| **Chest** | 8 Planks in a 3×3 ring, centre empty | 1 |
| **Bucket** | 3 Iron Ingots in a bucket shape | 1 |
| <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/130.png" width="64"><br>**Machinist's Bench** | 1 Workbench + 4 Iron Ingots + 2 Copper Ingots, shapeless | 1 |
| <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/134.png" width="64"><br>**Lever** | 1 Stick + 1 Cobblestone + 1 Azure Crystal, shapeless | 1 |
| <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/135.png" width="64"><br>**Button** | 1 Stone + 1 Azure Crystal, shapeless | 1 |

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

All five tool types exist in **Wood, Stone, Copper, Iron and Diamond** tiers. The shape stays the same; only the head/blade material changes.

| Tier | `M` material |
|---|---|
| Wood | Planks |
| Stone | Cobblestone |
| Copper | Copper Ingot |
| Iron | Iron Ingot |
| Diamond | Diamond |

<table>
<tr><th>Pickaxe</th><th>Axe</th><th>Sword</th><th>Shovel</th><th>Hoe</th></tr>
<tr>
<td>

```text
MMM
·S·
·S·
```

</td>
<td>

```text
MM·
MS·
·S·
```

</td>
<td>

```text
M
M
S
```

</td>
<td>

```text
M
S
S
```

</td>
<td>

```text
MM·
·S·
·S·
```

</td>
</tr>
</table>

`S` = Stick. Axe and Hoe may be mirrored horizontally.

### Armor

Armor can be crafted from **Copper Ingots, Iron Ingots or Diamonds**. Wood and Stone armor do not exist.

<table>
<tr><th>Helmet</th><th>Chestplate</th><th>Leggings</th><th>Boots</th></tr>
<tr>
<td>

```text
MMM
M·M
```

</td>
<td>

```text
M·M
MMM
MMM
```

</td>
<td>

```text
MMM
M·M
M·M
```

</td>
<td>

```text
M·M
M·M
```

</td>
</tr>
</table>

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

<img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/130.png" width="96">

The **Machinist's Bench** is the main industrial crafting station. Most recipes below are **shapeless**, so ingredient position does not matter. The icons below are the same rendered item graphics used by the game.

### Core industrial components

<p align="center">
  <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/122.png" alt="Copper Wire" width="68">
  <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/123.png" alt="Copper Plate" width="68">
  <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/124.png" alt="Iron Plate" width="68">
  <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/125.png" alt="Iron Cog" width="68">
  <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/126.png" alt="Rivets" width="68">
  <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/127.png" alt="Machine Casing" width="68">
</p>

| In-game item | Recipe | Output |
|---|---|---:|
| <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/122.png" width="64"><br>**Copper Wire** | 1 Copper Ingot | 4 |
| <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/123.png" width="64"><br>**Copper Plate** | 3 Copper Ingots in one horizontal row | 3 |
| <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/124.png" width="64"><br>**Iron Plate** | 3 Iron Ingots in one horizontal row | 3 |
| <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/125.png" width="64"><br>**Iron Cog** | 2 Iron Ingots in one horizontal row | 1 |
| <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/126.png" width="64"><br>**Rivets** | 1 Iron Ingot | 8 |
| <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/127.png" width="64"><br>**Machine Casing** | 4 Iron Plates + 4 Rivets | 1 |
| <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/131.png" width="64"><br>**Signal Wire** | 1 Copper Wire + 1 Azure Crystal | 4 |
| <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/132.png" width="64"><br>**Signal Conduit** | 2 Copper Plates + 1 Signal Wire | 2 |
| <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/138.png" width="64"><br>**Power Cable** | 4 Copper Wire + 1 Plank | 4 |

The three shaped component recipes are:

```text
Copper / Iron Plate:  MMM
Iron Cog:             II
```

### Blue Signal and workshop controls

<p align="center">
  <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/131.png" alt="Signal Wire" width="68">
  <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/132.png" alt="Signal Conduit" width="68">
  <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/133.png" alt="Signal Relay" width="68">
  <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/136.png" alt="Signal Indicator" width="68">
  <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/137.png" alt="Workshop Hatch" width="68">
  <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/139.png" alt="Workshop Lamp" width="68">
</p>

| In-game item | Ingredients | Output |
|---|---|---:|
| <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/133.png" width="64"><br>**Signal Relay** | 2 Signal Conduits + 1 Azure Crystal + 1 Iron Plate | 1 |
| <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/136.png" width="64"><br>**Signal Indicator** | 1 Glass + 1 Azure Crystal + 1 Copper Wire | 1 |
| <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/137.png" width="64"><br>**Workshop Hatch** | 4 Iron Plates + 1 Iron Cog + 1 Signal Wire | 1 |
| <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/139.png" width="64"><br>**Workshop Lamp** | 2 Glass + 2 Copper Wire + 1 Iron Plate | 1 |

> The **Lever** and **Button** are 3×3 Workbench recipes and are listed above.

### Machines and transport

<p align="center">
  <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/140.png" alt="Boiler Engine" width="72">
  <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/141.png" alt="Alternator" width="72">
  <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/142.png" alt="Crusher" width="72">
  <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/143.png" alt="Pump" width="72">
  <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/144.png" alt="Drill" width="72">
  <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/145.png" alt="Water Tank" width="72">
</p>

| In-game item | Ingredients | Output |
|---|---|---:|
| <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/140.png" width="64"><br>**Boiler Engine** | 1 Machine Casing + 4 Copper Plates + 2 Iron Cogs | 1 |
| <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/141.png" width="64"><br>**Alternator** | 1 Machine Casing + 2 Iron Cogs + 8 Copper Wire | 1 |
| <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/142.png" width="64"><br>**Crusher** | 1 Machine Casing + 2 Iron Cogs + 4 Iron Plates | 1 |
| <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/143.png" width="64"><br>**Pump** | 1 Machine Casing + 1 Iron Cog + 4 Copper Wire + 2 Copper Plates | 1 |
| <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/144.png" width="64"><br>**Drill** | 1 Machine Casing + 2 Iron Cogs + 4 Iron Plates + 4 Copper Wire | 1 |
| <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/145.png" width="64"><br>**Water Tank** | 4 Copper Plates + 2 Iron Plates | 1 |
| <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/146.png" width="64"><br>**Item Pipe** | 2 Iron Plates + 2 Copper Wire | 4 |
| <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/147.png" width="64"><br>**Fluid Pipe** | 2 Copper Plates | 4 |
| <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/148.png" width="64"><br>**Extractor** | 1 Iron Cog + 2 Copper Wire | 1 |
| <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/149.png" width="64"><br>**Inventory Sensor** | 2 Signal Wire + 1 Copper Plate + 1 Glass | 1 |

### Multiblock tank parts

These are the same tank-part graphics used throughout the [multiblock tank guide](Tanks.md).

<p align="center">
  <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/160.png" alt="Reinforced Tank Frame" width="70">
  <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/161.png" alt="Tank Wall" width="70">
  <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/162.png" alt="Reinforced Tank Glass" width="70">
  <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/163.png" alt="Tank Controller" width="70">
  <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/164.png" alt="Tank Fluid Port" width="70">
  <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/165.png" alt="Tank Access Hatch" width="70">
  <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/166.png" alt="Signal Valve Port" width="70">
  <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/167.png" alt="Tank Level Sensor" width="70">
</p>

| In-game item | Ingredients | Output |
|---|---|---:|
| <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/160.png" width="64"><br>**Reinforced Tank Frame** | 3 Iron Plates + 4 Rivets + 1 Copper Plate | 4 |
| <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/161.png" width="64"><br>**Tank Wall** | 1 Machine Casing + 4 Iron Plates | 4 |
| <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/162.png" width="64"><br>**Reinforced Tank Glass** | 4 Glass + 4 Rivets | 4 |
| <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/163.png" width="64"><br>**Tank Controller** | 1 Tank Wall + 1 Iron Cog + 1 Azure Crystal | 1 |
| <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/164.png" width="64"><br>**Tank Fluid Port** | 1 Tank Wall + 1 Fluid Pipe | 1 |
| <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/165.png" width="64"><br>**Tank Access Hatch** | 1 Tank Wall + 2 Copper Plates | 1 |
| <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/166.png" width="64"><br>**Signal Valve Port** | 1 Tank Fluid Port + 1 Signal Conduit | 1 |
| <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/167.png" width="64"><br>**Tank Level Sensor** | 1 Tank Wall + 1 Signal Wire + 1 Glass | 1 |

For shell construction, orientation and capacity rules, see **[Build a multiblock tank](Tanks.md)**.

### Batteries

<p align="center">
  <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/168.png" alt="Battery Block" width="84">
  <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/169.png" alt="Battery Bank Controller" width="84">
</p>

| In-game item | Ingredients | Output |
|---|---|---:|
| <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/168.png" width="68"><br>**Battery Block** | 1 Machine Casing + 2 Copper Plates + 4 Copper Wire + 2 Coal | 1 |
| <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/169.png" width="68"><br>**Battery Bank Controller** | 1 Machine Casing + 4 Copper Wire + 1 Azure Crystal + 1 Glass | 1 |

For bank construction and electrical behaviour, see **[Electricity and batteries](Electricity-and-batteries.md)**.

---

## Furnace recipes

All current Furnace recipes take **200 simulation ticks / 10 seconds** per item.

| Input | Output |
|---|---|
| Log | Charcoal |
| Raw Copper | Copper Ingot |
| Raw Iron | Iron Ingot |
| Raw Gold | Gold Ingot |
| Cobblestone | Stone |
| Potato | Baked Potato |
| Azure Ore | <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/121.png" width="48"><br>Azure Crystal |
| Sand | <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/128.png" width="48"><br>Glass |
| <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/150.png" width="48"><br>Crushed Copper | Copper Ingot |
| <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/151.png" width="48"><br>Crushed Iron | Iron Ingot |
| <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/152.png" width="48"><br>Crushed Gold | Gold Ingot |

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

Fuel keeps burning after ignition even if the Furnace becomes temporarily idle.

---

## Crusher recipes

<img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/142.png" width="96">

The Crusher draws **160 W** and takes **5 seconds at full power** for one operation. It doubles the raw-metal yield before smelting.

<table>
<tr>
<th>Input</th><th></th><th>Crusher output</th><th></th><th>Then smelt</th>
</tr>
<tr>
<td><b>Raw Copper ×1</b></td><td>→</td><td align="center"><img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/150.png" width="64"><br><b>Crushed Copper ×2</b></td><td>→</td><td><b>Copper Ingot ×2</b></td>
</tr>
<tr>
<td><b>Raw Iron ×1</b></td><td>→</td><td align="center"><img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/151.png" width="64"><br><b>Crushed Iron ×2</b></td><td>→</td><td><b>Iron Ingot ×2</b></td>
</tr>
<tr>
<td><b>Raw Gold ×1</b></td><td>→</td><td align="center"><img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/152.png" width="64"><br><b>Crushed Gold ×2</b></td><td>→</td><td><b>Gold Ingot ×2</b></td>
</tr>
</table>

Directly smelting a Raw Metal gives one ingot. **Crusher → Furnace gives two ingots from the same raw item.**

---

## Using the in-game recipe browser

Open the inventory or a station and use the item catalog on the right.

- **Click** an item to show recipes.
- **Shift-click / right-click** an item to show uses.
- Hover an item and press **R** for recipes or **U** for uses.
- **Ctrl-click** can fill one compatible recipe into the currently open crafting grid when you have the ingredients.

The browser shows the exact required station, layout, quantities and alternative recipes. It is particularly useful for 4×4 industrial recipes and for following intermediate components such as plates, wire, rivets and Machine Casings.

## Notes

- Only recipes registered in the active catalogs are listed here. Historical `Starter axe`, `Starter pickaxe` and `Starter dagger` definition files are legacy verification content and are **not normal playable recipes**.
- Grid recipes consume the quantities shown in each occupied slot. A recipe cannot be substituted with a different material unless that alternative has its own registered recipe.
- Horizontal mirroring is only available where the recipe explicitly allows it.
- Machine processing is separate from grid crafting: Furnace and Crusher recipes cannot be performed by simply arranging their input in a crafting grid.
- The survival screenshots above are from the repository's Windows player crafting verification evidence. Industrial item graphics are loaded from the same `Industry/Icons` resources used by the game.

For implementation details, the authoritative content lives in `Assets/RivetReach/Resources/Definitions/Recipes/`, `Definitions/Recipes.asset` and `Definitions/Processing.asset`.