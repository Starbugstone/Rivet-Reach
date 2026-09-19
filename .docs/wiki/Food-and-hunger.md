# Food and hunger

Food keeps an explorer ready to sprint and recover after danger. Your food meter holds **20 food points**. Eat with **Use** (right-click by default) for **1.2 seconds**. Releasing Use, opening a menu, changing the selected item or starting another interaction cancels the bite without using the food.

You cannot eat while the food meter is full. This is true even when your saturation reserve is empty, so wait until you have lost food before eating. Food and saturation above their caps are wasted: choose the meal that fits your current meter instead of eating a large meal immediately after a small loss.

![Porridge cooking from harvested grain and berries, controlled game fixture, September 19, 2026](images/food-balance/harvest-cooking.png)

The kitchen capture shows an actual controlled game fixture: wheat and berries grew normally, hoe harvesting replanted both crops, and the harvested ingredients cooked into one meal. The eating and HUD captures use the same saved kitchen with one supplied meal to demonstrate the revised balance. These captures were recorded on September 19, 2026.

![Eating porridge in the controlled kitchen fixture](images/food-balance/eating-prepared-meal.png)

## Food and saturation

Every four exhaustion spends one food point. Ordinary movement, jumping, mining and tilling use the current reduced hunger rate; combat damage still uses the normal health and armor rules. Healing still costs exhaustion, and starvation still cannot reduce you below one health point.

Prepared food can also fill a **20-point saturation reserve**. Saturation points are true food-point equivalents: one point absorbs four exhaustion before the food meter loses a point. A full reserve absorbs 80 exhaustion. It does not heal you directly, remove the need to eat, or raise the food meter above 20.

| Food | Food restored | Saturation |
|---|---:|---:|
| Raw potato, berries, mushroom | 1 | 0 |
| Raw carrot, raw fish, raw chicken | 2 | 0 |
| Apple | 4 | 0 |
| Baked potato, roasted carrot | 5 | 1 |
| Cooked mushrooms, cooked egg | 4 | 1 |
| Cooked fish, cooked chicken | 6 | 2 |
| Bread | 7 | 2 |
| Fruit porridge | 9 | 3 |
| Vegetable stew, fish stew, chicken stew | 12 | 4 |

Raw food is useful when it is all you have. Cooking makes food last longer between meals; stews provide the largest current reserve, but can waste more if eaten too soon.

## Rebalanced cadence

Fruit porridge adds a **3-point** reserve. Replaying a recorded twelve-minute walking/jumping base-work session with this reserve left **14 of 20 food points**—six points spent. Your pace depends on movement, sprinting, mining and healing; this is a measured activity replay, not a fixed timer.

![Three-point reserve after eating porridge](images/food-balance/well-fed-garden.png)

## Keep food useful

Food at 6 or below prevents sprinting. Healing begins at 12 food, can continue at 11, and stops at 10 or below. Saturation is paid before food, so a prepared meal can preserve the meter while you travel or heal; the existing thresholds themselves do not change.

When food visibly falls or health regenerates, the affected icon briefly wobbles for **0.45 seconds**, moving at most **three reference pixels**. It remains still while its meter is idle, static, hidden or loading, and the effect does not replay after those states.

![Actual game capture: food drops, then a heart regenerates](images/food-balance/hud-wobble.png)

[Beds and home spawn](Beds-and-home-spawn.md) move the sky to morning without skipping hunger, crop, animal, fuel, machine or fishing time. Pause screens stop simulation. Save/Load preserves food and saturation, but does not add offline time or production.

For planting, harvests and cooker setup, see [Farming and cooking](Farming-and-cooking.md). [Fishing](Fishing.md) and [Chickens](Chickens.md) cover their food sources.
