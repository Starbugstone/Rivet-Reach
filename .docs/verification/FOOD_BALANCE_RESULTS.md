# Food balance verification — 19 September 2026

Scope: the remaining single-player hunger/food balance work in issue #10. [Current rules](../FOOD_BALANCE.md) and [player guide](../wiki/Food-and-hunger.md) include subsequent issue #12 hunger/harvest changes, cave lighting, revised compost, fish/chicken meals, beds, crates, weather and renewables. Recipes, yields, growth, cooking, fishing and animal timers remain unchanged.

## Controlled food cadence

Unity 6000.4.4f1 ran **69 food-balance assertions**: exactly paid consumption, full-food rejection, bounded reserve, reserve-before-food exhaustion, unchanged healing cost, fractional exhaustion, tick partition equivalence, schema-17 state and schema-16 zero-reserve migration, invalid saved reserve rejection, and 54 one-hour diet comparisons. [Checks](food-balance-2026-09-19/domain-checks.txt) · [all workloads and foods](food-balance-2026-09-19/cadence.csv).

These are accelerated domain simulations, not 54 hours of elapsed gameplay. Each starts at 20 food, zero reserve and full health with stocked food. It eats only when all restored food fits. The baseline uses the same current recipes/food points and issue #12's reduced hunger costs, with zero saturation.

| Base-work diet | Meals/hour before → after | Mean repeat interval after |
|---|---:|---:|
| Raw potato | 51 → 51 | 70.5 s |
| Baked potato | 10 → 7 | 492.3 s |
| Bread | 7 → 4 | 918.0 s |
| Fruit porridge | 5 → 3 | 1,198.5 s |
| Vegetable/fish/chicken stew | 4 → 2 | 1,687.0 s |

Base work is 20 seconds walking at 4.5 m/s, two jumps and 18 successful block actions per minute, plus ordinary passive hunger. Expedition work uses 40 seconds walking, 10 seconds sprinting, four jumps and six actions per minute. A continuous sprint case supplies a deliberate high-demand comparison. No damage/healing is injected into the cadence tables; healing retains its separate six-exhaustion cost and dedicated assertion. Lowest food follows the eat-without-overfill policy; large meals can temporarily put food below the healing threshold. Eating earlier for combat trades some efficiency for readiness.

Regression checks passed **47,725 Survival**, **4,372 Farming**, **960 Fishing**, **563 Chicken** and **2 content-compatibility** assertions. [Survival](food-balance-2026-09-19/survival-checks.txt), [Farming](food-balance-2026-09-19/farming-checks.txt), [Fishing](food-balance-2026-09-19/fishing-checks.txt), [Chicken](food-balance-2026-09-19/chicken-checks.txt).

## Native player and persistence

The ordinary fixed-seed Survival route ran **312.1 seconds**, starting empty-handed with hostile spawning active. It gathered four logs, placed a crafted workbench and crafted/used a wooden pickaxe without supplied resources or teleporting. It ended at **14 food / 20 health**. [Observations](food-balance-2026-09-19/survival-observations.txt) and [capture](food-balance-2026-09-19/survival-five-minutes.png) retain that route. This short route measures early resource work, not a complete wild-food diet.

The controlled kitchen run has already grown wheat and berries through three normal active-time growth stages, harvested/replanted them through the issue #12 hoe transaction, cooked their grain/fruit into one porridge using paid fuel, and consumed it with the real held-Use action. It supplies the platform, initial planting stock, cooker and one fuel log. It does not claim an unassisted farm-building playthrough. The fifteen-minute sustained workload is still running; no completed sustained result is claimed yet.

The final player passed **9 fresh-process food/HUD checks**: exact food/reserve/fractional exhaustion and active tick on restart, no offline advance, and the sprint warning taking precedence over a small reserve at low food. [Report](food-balance-2026-09-19/food-resume-report.json), [low-food warning](food-balance-2026-09-19/low-food-reserve-warning.png) and [reserve display](food-balance-2026-09-19/saved-reserve-hud.png).

The broader save suite passed **183 assertions plus 5 restart assertions** on the earlier food-balance build, before the low-food HUD ordering and verification-only cooker approach were corrected. An actual complete schema-16 renewable checkpoint passed **8 checks**, retaining exact survival tick, both renewable machines, wind gust and battery energy, with zero new reserve. [Save](food-balance-2026-09-19/save-regression-report.json), [restart](food-balance-2026-09-19/save-resume-report.json), [schema-16 migration](food-balance-2026-09-19/legacy-renewables-report.json) and [historical checkpoint identities](food-balance-2026-09-19/legacy-checkpoint-identity.json). These are actual historical files, not newly encoded schema-17 substitutes.

The [final build](food-balance-2026-09-19/build-summary.txt) has zero errors and four existing deprecated object-query warnings in Alpha verification. Its [assembly hash](food-balance-2026-09-19/assembly-sha256.txt) differs from the [earlier route/save build](food-balance-2026-09-19/sustained-assembly-sha256.txt); food accounting/configuration and schema are unchanged between them. The new reserve uses bounded scalar arithmetic and cached item lookup; no whole-world scan or food-decay scheduling was added. This is an implementation observation, not a measured performance claim.

## Evidence boundaries

The food defaults are a measured working balance, not a proven optimum. Scripted Survival, provisioned comparisons and controlled established-base fixtures are distinguished from human playtesting and an entirely unassisted farm-building playthrough. Extended human review across difficult terrain, combat, fishing and chicken-focused diets remains useful. No multiplayer synchronization, release packaging, arbitrary-factory performance claim or terrain regeneration is included.
