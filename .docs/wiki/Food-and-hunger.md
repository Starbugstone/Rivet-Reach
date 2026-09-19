# Food and hunger

Food keeps an explorer ready to sprint and recover after danger. Your food meter holds **20 food points**. Eat with **Use** (right-click by default) for **1.2 seconds**. Releasing Use, opening a menu, changing the selected item or starting another interaction cancels the bite without using the food.

You cannot eat while the food meter is full. This is true even when your saturation reserve is empty, so wait until you have lost food before eating. Food and saturation above their caps are wasted: choose the meal that fits your current meter instead of eating a large meal immediately after a small loss.

![Porridge cooking from harvested grain and berries, controlled game fixture, September 19, 2026](images/food-balance/harvest-cooking.png)

The captures show an actual controlled game fixture: wheat and berries grew normally, hoe harvesting replanted both crops, and the harvested ingredients cooked into one meal. The fixture supplies the platform, planting stock, cooker and fuel.

![Eating the harvested and cooked meal](images/food-balance/eating-prepared-meal.png)

## Food and saturation

Every four exhaustion spends one food point. Ordinary movement, jumping, mining and tilling use the current reduced hunger rate; combat damage still uses the normal health and armor rules. Healing still costs exhaustion, and starvation still cannot reduce you below one health point.

Prepared food can also fill a **20-point saturation reserve**. Saturation points are true food-point equivalents: one point absorbs four exhaustion before the food meter loses a point. A full reserve absorbs 80 exhaustion. It does not heal you directly, remove the need to eat, or raise the food meter above 20.

| Food | Food restored | Saturation |
|---|---:|---:|
| Raw potato, berries, mushroom | 1 | 0 |
| Raw carrot, raw fish, raw chicken | 2 | 0 |
| Apple | 4 | 0 |
| Baked potato, roasted carrot | 5 | 2 |
| Cooked mushrooms, cooked egg | 4 | 2 |
| Cooked fish, cooked chicken | 6 | 4 |
| Bread | 7 | 6 |
| Fruit porridge | 9 | 8 |
| Vegetable stew, fish stew, chicken stew | 12 | 12 |

Raw food is useful when it is all you have. Cooking makes food last longer between meals; stews provide the largest current reserve, but can waste more if eaten too soon.

## One-hour controlled comparison

The table below comes from a controlled simulation in the Unity Editor (69 assertions) recorded on September 19. It simulates one hour with **20 seconds walking, two jumps and 18 successful mining/tilling actions per minute**. It is one fixed workload, not a guarantee for exploration, combat, sprinting, terrain, or player choices. “Average interval” is the fixture's mean time between completed meals; it is not a promised repetition interval. Each simulated diet starts with a full food meter, no reserve and sufficient stocked food; it eats when the meal fits without wasting restored hunger. This comparison measures eating frequency, not the time spent gathering food.

| Food | Meals eaten in hour | Lowest food | Average interval |
|---|---:|---:|---:|
| Raw potato | 51 | 19 | 70.5 seconds |
| Baked potato | 7 | 15 | 492.3 seconds |
| Fruit porridge | 3 | 11 | 1,198.5 seconds |
| Vegetable stew | 2 | 8 | 1,687.0 seconds |

The same comparison applies to the matching stew types. A better meal reduces the number of eating actions in this controlled route; it does not make food generation, crop yields, fishing catches, chicken eggs or cooker time faster.

![Well fed reserve visible after eating; the wheat and berries are already replanted, September 19, 2026](images/food-balance/well-fed-garden.png)

## Keep food useful

Food at 6 or below prevents sprinting. Healing begins at 12 food, can continue at 11, and stops at 10 or below. Saturation is paid before food, so a prepared meal can preserve the meter while you travel or heal; the existing thresholds themselves do not change.

[Beds and home spawn](Beds-and-home-spawn.md) move the sky to morning without skipping hunger, crop, animal, fuel, machine or fishing time. Pause screens stop simulation. Save/Load preserves food and saturation, but does not add offline time or production.

For planting, harvests and cooker setup, see [Farming and cooking](Farming-and-cooking.md). [Fishing](Fishing.md) and [Chickens](Chickens.md) cover their food sources.
