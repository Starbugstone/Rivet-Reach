# Creative testing verification — 2026-09-10

Artifact: `Builds/Creative/RivetReach.exe`, Unity **6000.4.4f1**, Windows x64 Development build. Build completed with **0 errors and 0 warnings**. The build includes the existing fluid increment plus Creative changes. Earlier feature evidence retains its original build identity.

The focused runtime check completed at **2026-09-10T06:46:59.1195355Z**, **PASS: 120 assertions, no logged errors/exceptions**, at 1280×720 on an Intel i7-10750H / NVIDIA RTX 2060. [Raw runtime report](creative-runtime-2026-09-10.json).

Verified through the real session, UI, virtual keyboard/mouse and world/container authorities:

- Empty-handed Survival startup; Escape opens pause; pointer clicks enable/disable Creative while retaining pause and existing inventory/health/food.
- Impact, fall and starvation commands rejected in Creative; automatic starvation leaves health unchanged while world simulation continues.
- Catalog contains all **84 current registry items**, grants a legal full stack of each, rejects full-inventory grants without mutation, filters by name and handles empty results. Pointer item selection and access to personal crafting work.
- Flight ascent, descent, hover, sprint at zero food, floor collision, rebound Jump and inventory suppression. Landing checks include the existing collider's 0.001-block contact skin.
- Creative block placement retains the stack; disabling restores consumption, damage and gravity. Survival hides and rejects catalog grants; a replacement session resets Creative and inventory.

The same executable passed the existing Survival runtime regression at **2026-09-10T06:50:06.4527675Z**, **78 assertions, no errors**: crafting, furnace/chest state, crops, hunger, armor, streamed station persistence, death and respawn. [Raw Survival regression report](creative-survival-runtime-2026-09-10.json).

The build also ran the existing domain, crafting, starter-recipe and survival checks successfully. This establishes the covered invariants, not flight feel, long-session performance or full-game acceptance. Creature damage immunity uses the common damage authority; this scenario does not stage a live mob attack. Creative-specific planting, extended streaming flight, and every catalog row's pointer position were not individually exercised.

## Visual evidence

The actual Unity screenshots were inspected for layout, controls, item icons and readability.

![Creative toggle in the Escape menu](creative-menu-2026-09-10.png)

![Searchable catalog alongside inventory and hotbar](creative-catalog-2026-09-10.png)

## Reproduce

With the pinned Editor open on this project, run `Tools/Verify-Creative.ps1 -Build`. Without `-Build`, it checks the existing Creative executable. [Gameplay rules](../GAMEPLAY.md#creative-testing-mode) and [run instructions](../FIRST_POC.md#creative-testing) own player-facing behavior.
