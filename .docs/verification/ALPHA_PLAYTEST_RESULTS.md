# Alpha 0.0.2 playtest corrections — issue #12

Tracking [issue #12](https://github.com/Starbugstone/Rivet-Reach/issues/12). This is an **in-progress implementation checkpoint**, not issue closure or a completed playtest.

## Interaction checkpoint — 2026-09-19

Unity 6000.4.4f1 compiled the interaction changes in the primary project. The `alpha-playtest-checks` request completed successfully through the existing open Editor. `AlphaPlaytestChecks` passed 57 assertions covering shape intersections, full-cube default traits, inventory consolidation, portable battery contents, stable ordering, Survival/Creative item picking and the 25% ordinary-hunger reduction. Existing inventory, survival, farming and fishing checks also passed (fishing: 960 assertions).

Implemented code at this checkpoint:

- Held Jump uses the normal grounded gate; Creative double-tap flight retains its press-edge input.
- Ordinary passive, walking, sprinting, jumping and mining/tilling exhaustion costs are multiplied by 0.75. Healing retains six exhaustion per recovered health point.
- Static block definitions cache crop/sapling selection boxes. An attachment provider supplies cached torch shapes. Full cubes keep the DDA fast path; collision remains independent. Selected outlines use the selection bounds.
- Middle-click sorts the hovered personal-inventory section or chest, consolidating compatible stacks and preserving individual stored contents. Hotbar and backpack remain separate sections; recipe grids and typed machine slots retain positional meaning. Sorting is disabled while holding/painting a cursor stack.
- World middle-click selects owned hotbar items, swaps an owned backpack stack into the selected slot, or supplies a Creative stack. Crops resolve to their planting/produce item and upper doors to their ordinary door item.
- Ripe farmland crops accept Use. A hoe inserts the ordinary harvest, drops safe overflow and replants only from a seed actually present afterward. Wild/unripe crops retain their ordinary break interaction.
- Removed the artificial terrain impact-light path and bright mining-border/placement rings. Actual source lighting remains independent.

## Still required

Native runtime checks and captures for these interactions; normal Survival playtest before focused regression; light-source regression; missing-texture reproduction/repair; Lava Rock; hostile-spawn audit and diagnostics; generic spawners and rooms; durable-save compatibility; matching wiki sources, visuals, export, publication and live review. No runtime quality or performance conclusion is implied by the Editor checks.

Concurrent issue #10 work is the chicken increment. Shared input, persistence and Editor/index intervals are coordinated through ignored local logs; those logs are not product specifications or permanent reservations.
