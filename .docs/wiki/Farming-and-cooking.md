# Farming and cooking

Find wild potatoes, wheat, flax, carrots and berry plants on grassy terrain. Mushrooms are also available as forage. Collect planting stock, prepare farmland with a hoe, and plant a reliable food supply. No watering or irrigation is required, and stored food never spoils.

![Crop growth stages in the game](images/farming/crop-stages.png)

## Wild harvests and planting

Cultivable wild plants visibly grow through the same four stages as farm plants. Young wheat, flax, carrot and berry plants return **one seed only**. Immature potatoes return **nothing**: their planting item is the edible crop itself. Wait for maturity to receive resources and enough planting stock to continue farming.

| Plant | Mature harvest | Plant with |
|---|---|---|
| Potato | 2–4 potatoes | A potato |
| Wheat | 2–3 grain + 2 seeds | Wheat Seeds |
| Flax | 2–3 fibre + 2 seeds | Flax Seeds |
| Carrot | 2–3 carrots + 2 seeds | Carrot Seeds |
| Berry plant | 2–3 berries + 2 seeds | Berry Seeds |
| Mushroom cluster | 1–2 mushrooms | Forage only |

Right-click exposed dirt or grass with a hoe to make farmland. Select the planting item and right-click the farmland. Plants need open sky light and loaded terrain. Each stage normally takes one minute; a freshly planted crop needs three growth steps. Growth pauses while the world is closed or its terrain is unloaded.

![A wild plant growing on natural ground](images/farming/wild-growth.png)

New plants appear in **newly explored terrain**. Updates preserve recorded generated chunks. Older saves lack complete exploration history, so the first upgrade conservatively preserves areas around saved building activity, the player and saved entities; travel beyond those areas to find the new plants.

## Fibre and cloth

Three [Flax Fibre](Item-flax-fibre.md) make one [String](Item-string.md), and four Strings make one [Cloth](Item-cloth.md), using personal crafting. These begin the renewable textile chain. Fishing rods and beds are planned follow-ups and are not yet available in this increment.

## Build a cooker

Craft a [Cooker](Item-cooker.md) from one furnace and two copper ingots at a workbench. Upgrade it with one machine casing and four copper wire at a Machinist's Bench to make an [Electric Cooker](Item-electric-cooker.md).

![Basic and electric cookers operating in the game](images/farming/cookers-running.png)

Open either machine with **right-click or Interact**. Click the recipe button to select the food, add ingredients to the three input slots, and collect the result. Changing recipes keeps the items but resets cooking progress.

- **Cooker:** add burnable fuel to its separate fuel slot. Coal, charcoal and configured wood fuels work. Unused heat is retained while cooking is blocked or idle.
- **Electric Cooker:** connect power on any face. It uses **200 W** while cooking. Limited power slows cooking; no power pauses it. It has no fuel slot.
- Both variants accept optional Blue Signal control. Full output, missing ingredients, signal OFF or unloaded terrain pauses production.

![Basic cooker interface with separate fuel and ingredient slots](images/farming/cooker-interface.png)

![Electric cooker interface showing allocated power](images/farming/electric-cooker-interface.png)

## Recipes and food quality

Each recipe takes **10 seconds** at full heat or power. Food tags allow interchangeable ingredients: potatoes and carrots count as `#vegetable`; apples and berries count as `#fruit`. You can mix tag members and split quantities across input slots.

| Ingredients | Result | Hunger points restored |
|---|---|---:|
| 3 Grain | Bread | 7 |
| 1 Potato | Baked Potato | 5 |
| 1 Carrot | Roasted Carrot | 5 |
| 1 Mushroom | Cooked Mushrooms | 4 |
| 2 vegetables + 1 mushroom | Vegetable Stew | 12 |
| 1 grain + 1 fruit | Fruit Porridge | 9 |

Every food you can eat has the `edible` tag and its own hunger value. Hold Use to eat. Meals restore more hunger in a single eating action than raw forage: raw carrots restore two points, berries and mushrooms one. Stockpile cooked food for mining, exploration and building. There is no spoilage or freshness timer.

The recipe browser lists both cookers and the alternatives for tagged ingredients. Search with `#edible` to see foods, `#vegetable #edible` to require both tags, or combine a tag and name such as `#ingot iron`. The existing furnace and Electric Furnace still bake potatoes alongside their normal smelting recipes.

## Automate cooking

![A cooker connected to ingredient, fuel and output chests](images/farming/cooker-pipes.png)

Use [item pipes and a Wrench](Pipes.md) to set machine-facing ends:

1. Supply **fuel through the rear** of the basic Cooker. Supply ingredients through another face.
2. Electric Cooker ingredients can enter on any face.
3. Set a separate machine-facing pipe end to **Output** to extract finished food.

Ingredient pipes add only what the selected recipe still needs, leaving room for each required ingredient. Overlapping tags work too, and manually loaded surplus can remain while pipes supply missing ingredients. Input items and fuel cannot be extracted as finished food. Select the recipe before connecting the supply chests. Pipe arrows are visible while holding the Wrench.

Crop growth, machine ingredients, selected recipes, stored heat, partial cooking work and battery energy survive Save/Load. No offline cooking or crop growth is added.

Captures: September 13, 2026, Farming review build. Quantities and timings are initial balance settings; long-session food cadence remains subject to playtesting.

![The actual item browser filtered to edible foods](images/farming/edible-tag-search.png)
