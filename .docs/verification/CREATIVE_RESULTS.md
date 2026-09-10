# Creative controls and full workshop verification — 2026-09-10

Artifact: **`Builds/Creative/RivetReach.exe`**, Unity **6000.4.4f1**, Windows x64 Development. The final build completed with **0 errors and 0 warnings**. [Build/source identity and hashes](creative-build-identity-2026-09-10.json), [Editor checks](creative-build-checks-2026-09-10.txt).

All six scenarios ran against this executable at 1280×720 on an Intel i7-10750H / NVIDIA RTX 2060: **PASS, 1,431 runtime assertions, no captured Unity errors/exceptions**.

| Scenario | Assertions | Completed (UTC) | Evidence |
| --- | ---: | --- | --- |
| Creative inventory and controls | 308 | 2026-09-10T17:32:10.0871830Z | [Raw report](creative-runtime-2026-09-10.json) |
| Creative industrial workshop | 163 | 2026-09-10T17:33:02.2556039Z | [Raw report](creative-industry-runtime-2026-09-10.json) |
| Creative multiblock tank | 287 | 2026-09-10T17:33:31.2855486Z | [Raw report](creative-multiblock-runtime-2026-09-10.json) |
| Placement and movement regression | 96 | 2026-09-10T17:34:12.5072352Z | [Raw report](creative-placement-items-runtime-2026-09-10.json) |
| Recipe browser and crafting regression | 491 | 2026-09-10T17:34:34.2620690Z | [Raw report](creative-browser-runtime-2026-09-10.json) |
| Survival regression | 86 | 2026-09-10T17:36:08.8045017Z | [Raw report](creative-survival-runtime-2026-09-10.json) |

## Verified behavior

- Native pointer drags for all **125 registered items**, targeting backpack and hotbar; matching-stack top-up; occupied/full target rejection with visible sidebar feedback; canceled/right-button/Survival drags and existing cursor-stack protection. Catalog clicking, filtering, personal crafting and all recipe-browser click/uses/transfer gestures remain covered.
- Creative starts walking. A single Jump press jumps; double-tap Jump toggles flight both ways. Flight ascent/descent/hover, collision, speed at zero food, rebound Jump, inventory suppression and clearing pending gestures pass. Invincibility and unlimited placement survive turning flight off; disabling Creative restores Survival consumption/damage. Ctrl/Shift defaults, partial old preference migration, custom bindings and deliberate later remapping pass. The placement/movement regression checks sustained double-tap Forward sprint, cancellation, rebound Forward and dedicated Sprint.
- Creative industrial components are supplied through the catalog authority and placed through `TryPlaceSelected` with camera targeting and legal stack retention. Boiler/alternator supply, powered crusher animation and ore delivery through item pipes, pump/tank loop, relay/button/hatch and independent Blue Signal/electricity states pass. Disconnecting/reconnecting both the power cable and signal wire produces the expected independent states. Machine interfaces, Machinist crafting and pointer bucket transfers pass.
- Creative construction forms a **6×4×5, 6,000 L tank** across a chunk boundary. Shared ports, wired valve, level sensor, pointer installation of signal/power fittings on an Item Pipe, full-inventory bucket swaps, individually mined glass, retained contents after breach, protected nonempty controller, repair and residency/floating-origin round trip pass. Editor checks cover both transport pipe families and their separate item/fluid/signal/power graphs.
- Survival regression covers actual crafting/recipe inspection, furnace/chest transactions, crop planting/growth/harvest, hunger, armor, damage, death/respawn and station streaming. Its obsolete RECIPES-button test was updated to inspect the current recipe browser.

## Actual player captures

![Creative catalog and draggable sidebar](creative-catalog-2026-09-10.png)

![Creative industrial workshop with separate power and Blue Signal routes](creative-workshop-2026-09-10.png)

![Player-built cross-chunk tank](creative-tank-2026-09-10.png)

Additional captures: [Escape toggle](creative-menu-2026-09-10.png), [flight controls](creative-flight-2026-09-10.png), [powered crusher](creative-crusher-2026-09-10.png), [tank controller](creative-tank-controls-2026-09-10.png), [pipe fittings](creative-pipe-fittings-2026-09-10.png). These Unity captures were visually inspected; source art was not regenerated for this change.

## Reproduce and limits

With the pinned Editor open on this project, run **`Tools/Verify-Creative.ps1 -Build -FullRun`**. Omit `-Build` to use the existing executable; omit `-FullRun` for only Creative controls/catalog. Reports and additional screenshots remain under ignored `Logs/CreativeVerification/`.

This is a scripted run in the actual Windows player, using virtual keyboard/mouse events for input gestures and UI transactions, plus game-facing placement/interaction authorities. Workshop fixtures clear a site, position the observer/camera, add temporary construction supports where needed, and seed fuel/water/ore; mobs are disabled for these focused checks. It is not a manual progression playthrough or an exhaustive test of every possible factory. The small capped frame samples do not establish large-factory or long-session performance. Earlier specialist reports retain their own build identities and broader art/domain evidence. Whole-world durable saves remain outside this task.

[Gameplay rules](../GAMEPLAY.md#creative-testing-mode) and [run instructions](../FIRST_POC.md#creative-testing) own controls and transition semantics.
