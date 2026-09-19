# Food balance

This document records the current food and hunger tuning for Issue #10. These are working defaults. [Food-balance verification](verification/FOOD_BALANCE_RESULTS.md) distinguishes controlled simulation, native Survival checks and remaining human playtesting.

## Hunger and reserve

Hunger has 20 food points. Every four exhaustion spends one point. Issue #12's **0.75** multiplier applies to ordinary passive and activity exhaustion: idle play spends one food point every **136.53 seconds**; walking spends 0.0075 exhaustion per metre, sprinting 0.075, jumping 0.15, and a successful mining or tilling action 0.0375. Healing remains outside that multiplier and costs **six exhaustion** per restored health point.

Prepared food can also grant up to **20 saturation points**. Saturation uses the same units as food: each point absorbs four exhaustion before hunger is spent. A full reserve therefore absorbs 80 exhaustion, equivalent to 20 food points or about 45.51 minutes of idle survival. Food and saturation are each capped at 20. Eating cannot begin while food is full, even if saturation is empty.

The existing health thresholds are unchanged: healing begins at food 12, continues at 11 and stops at 10 or below. Saturation can delay hunger loss while healing, but does not alter thresholds or restore health directly. Starvation, sprint gating, 1.2-second eating, cancellation, Creative hunger freeze and planting precedence remain unchanged.

## Food values

`ItemDefinition.foodPoints` remains the hunger authority. `Resources/Definitions/FoodBalance.json` contains only the additional saturation configuration. Foods absent from that file, including all raw foods, grant zero saturation.

| Food | Hunger | Saturation |
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

The recipe ingredients, outputs, cooker duration (200 ticks at full heat/power), crop growth interval (60 seconds per stage), three-minute crop maturity time, crop yields, fishing timing and chicken lifecycle timers are unchanged. Compost remains optional and cannot create an instant crop-growth loop. Fishing still requires its source-water footprint and manual bite/reel. Chicken eggs, breeding and growth remain active/resident-time systems. [Beds](BEDS.md) advance celestial time only, never skipped hunger or production ticks. [Crates](CRATES.md) store food without changing its creation or consumption.

## Persistence and tuning boundary

Schema **17** appends the bounded saturation integer to player hunger after food and exhaustion. Schemas 1–16 initialize it to zero. The current content fingerprint includes the exact `FoodBalance.json` text; an unknown or altered balance configuration rejects rather than silently reinterpreting a checkpoint. The exact schema-16 catalog with [renewables](RENEWABLES.md) remains an explicit compatibility projection.

The controlled comparisons measure repeat meal cadence under fixed workloads. They do not establish an optimum or replace sustained human play across different terrain, combat, fishing and animal-food routes.
