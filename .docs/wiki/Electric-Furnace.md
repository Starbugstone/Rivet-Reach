# Electric Furnace

The [Electric Furnace](Item-electric-furnace.md) smelts and cooks everything the [normal Furnace](Item-furnace.md) does, using **electricity instead of item fuel**. Recipes give the same quantities and take **10 seconds at full power**.

## Craft and connect

1. At a [Machinist's Bench](Item-machinist-bench.md), combine **one Furnace, one Machine Casing and four Copper Wire** to make one Electric Furnace. Ingredient positions do not matter.
2. Place it, then connect a [Power Cable](Item-power-cable.md) to any face. Use a connected generator or a charged [Battery](Item-battery-block.md).
3. Open it with **right-click or Interact** (default E). Put a smeltable or cookable ingredient in **INPUT**, then collect its product from **OUTPUT**.

The furnace needs **200 W** while processing, or **2 kJ per current recipe**. Four fully powered electric furnaces can use one 800 W boiler/alternator supply when there are no other loads. A short supply makes it work more slowly. An empty battery or broken cable pauses progress; restoring power resumes it. Idle and output-blocked furnaces draw no electricity.

There is no fuel slot. Logs make charcoal as ingredients; coal is not accepted as furnace fuel. The full recipe list is on the [item page](Item-electric-furnace.md#recipes-made-here) and in the inventory recipe browser.

## Automate

Run **chest → Item Pipe → Electric Furnace → Item Pipe → chest**. Select a [Wrench](Item-wrench.md) and right-click each machine-facing pipe end to set its direction. Blue Input arrows point into a machine; red Output arrows point out. Ingredient input and product output can use any face, including the rear. Fuel-only items stay in the source chest. See [Pipes](Pipes.md) for branching and disconnected ends.

An optional [Blue Signal](Blue-Signal.md) connection at the front lets a lever switch processing off and on. The priority button chooses how the furnace receives power when a grid is short.

Saved checkpoints retain ingredients, completed products, orientation, priority and partial work. Loading grants no offline production.

## In-game examples

Captured in the Unity 6000.4.4f1 Windows review player on **2026-09-13**.

![Electric furnace powered by a battery through the cable behind it](images/electric-furnace-powered.png)

![Electric furnace input, progress and 200 W running status; there is no fuel slot](images/electric-furnace-interface.png)

![With the cable removed, the furnace reports no electrical power and preserves its work](images/electric-furnace-no-power.png)

![Chest-to-furnace-to-chest item pipes with blue input and red output arrows while holding the wrench](images/electric-furnace-pipes.png)
