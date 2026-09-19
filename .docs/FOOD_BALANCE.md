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
| Baked potato, roasted carrot | 5 | 1 |
| Cooked mushrooms, cooked egg | 4 | 1 |
| Cooked fish, cooked chicken | 6 | 2 |
| Bread | 7 | 2 |
| Fruit porridge | 9 | 3 |
| Vegetable stew, fish stew, chicken stew | 12 | 4 |

The recipe ingredients, outputs, cooker duration (200 ticks at full heat/power), crop growth interval (60 seconds per stage), three-minute crop maturity time, crop yields, fishing timing and chicken lifecycle timers are unchanged. Compost remains optional and cannot create an instant crop-growth loop. Fishing still requires its source-water footprint and manual bite/reel. Chicken eggs, breeding and growth remain active/resident-time systems. [Beds](BEDS.md) advance celestial time only, never skipped hunger or production ticks. [Crates](CRATES.md) store food without changing its creation or consumption.

## Persistence and tuning boundary

The schema-17 hunger layout appends the bounded saturation integer after food and exhaustion; schemas 1–16 initialize it to zero. Food-balance compatibility accepts the exact original high-reserve configuration and the exact current moderate configuration, while an unknown or altered balance configuration rejects rather than silently reinterpreting a checkpoint. The exact schema-16 catalog with [renewables](RENEWABLES.md) remains an explicit compatibility projection.

The HUD animates only changed food icons on a visible loss and hearts on regeneration. A damped vertical pulse lasts 0.45 seconds with a maximum amplitude of three UI reference pixels. Fixed ten-icon state avoids per-frame collections; only an active pulse requests repeated mesh updates. Reserve-only drain does not pulse. Hidden/loading screens and replaced player state reset the baseline.

The user selected fruit porridge's three-point reserve and a replay target of Food 14 after the recorded twelve-minute workload. Unity Editor checks replayed the measured exhaustion through the current hunger rules and confirmed Food 14 at twelve minutes. New controlled cadence results replace the superseded high-reserve comparison; neither result establishes an optimum or replaces sustained human play across terrain, combat, fishing and animal-food routes.
