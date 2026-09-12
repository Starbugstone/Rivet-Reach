# Workshop follow-up — 2026-09-10

> The pump power requirement and electrical UI captured here were superseded on 2026-09-12. See [pumps without electricity](PUMP_RESULTS.md) for current behavior and fresh evidence; this report retains its dated checks of other workshop features.

Scope: outward machine placement, held industrial models, pump intake/power diagnosis, electrical batteries and banks, and published player construction guides. [BATTERIES.md](../BATTERIES.md) owns storage rules; [wiki Home](../wiki/Home.md) owns the maintained player-guide entry point.

Final Windows player build: **0 errors, 0 warnings**. All **seven Creative/regression scenarios passed, 1,906 runtime assertions**, including **471** in the [focused workshop report](workshop-runtime-2026-09-10.json). [Build hashes and timestamps](creative-build-identity-2026-09-10.json) and the [full scenario table](CREATIVE_RESULTS.md) identify the evidence.

## What changed

- Newly placed machines use the player-facing quarter turn. The previous additional half turn pointed the functional front away from a builder standing outside a tank. Existing placements retain their rotation and can be corrected through the machine interface.
- Held assemblies use the definition registry rather than the old runtime-ID range ending at 152. Tank parts and battery assemblies therefore resolve real meshes. Industrial held surfaces now use the same near-depth convention as hands/tools, with a separate transparent glass material. A connected pipe instantiates one selected shape, not its complete 64-variant export family. Held industrial fronts are oriented for recognition.
- Pump controls identify whether the cell directly below is a water source. Its existing power gate remains authoritative; this work does not give an unpowered pump a water-creation ability.
- Battery cells and controllers are craftable registry items with original Blender models, Creative placement, interfaces and independent electrical endpoints. Exact per-cell energy survives bank formation, repair, residency changes and controller removal.

## Evidence and limits

The focused player scenario constructs devices through Creative catalog grants and the ordinary aimed placement command. It verifies an unpowered pump retaining its source, natural two-source renewal beneath that pump, a battery-powered 10 L extraction consuming exactly 160 J, a full pump requesting no additional energy, and a pump placed in the water layer correctly reporting no source below its solid intake. It also places a solid battery bank, charges it from a real boiler/alternator cable network, and supplies a lamp after generation stops.

All 127 registry items are selected through inventory transitions. Each check requires an active rendered mesh and measures actual framebuffer differences with only the item's renderers temporarily suppressed. This detects the old empty-object false positive. Appearance rebuild and pause/resume are also exercised. These checks do not establish visibility under every camera angle, resolution, GPU or prolonged-session condition.

The user's broader all-items disappearance did not reproduce in a fresh instance of their prior `Builds/Rivet Reach.exe`: a normal block and sword were visible. That earlier avatar diagnostic separately failed its pickaxe arm-teleport assertion, so it is not recorded as a passing avatar suite. The confirmed tank-part and industrial-depth issues are fixed; no claim is made that the unidentified original session state was diagnosed.

The pump placement trap was reproduced: a placement ray passes through water to the basin floor, placing the machine in the water layer. Correct construction leaves a separate source cell beneath the pump. Natural renewal requires two horizontal source neighbours and a suitable floor; it never replaces a pump or sandstone with water.

## Reproduce

Run `Tools/Verify-Creative.ps1 -Build -FullRun` in Windows with the pinned Editor open on this project. The full run includes controls/catalog, industry, multiblock tanks, item placement, recipe browser, Survival and the workshop follow-up. New battery authoring can be prepared with the explicit `battery-build` Editor request. `BatteryChecks.Run` exercises exact storage, finite depletion, priority-before-charge allocation, isolation, charged-cell mining protection, bank claims, missing cells, repair, inward controllers, touching controllers and residency.

The source models are [BatteryKit.blend](../../ArtSource/Batteries/BatteryKit.blend), [source render](../../ArtSource/Batteries/battery-kit-review.png) and [geometry report](../../ArtSource/Batteries/geometry-report.json): Battery Block 4,264 triangles; controller 2,152, two mesh groups each. The normalized Unity imports measure 4,264 and 2,112 triangles respectively, with two renderers each and one-cell bounds; see the [29 battery checks and imported geometry measurements](workshop-domain-2026-09-10.txt). The battery domain checks ran before the final presentation-only refinements, against unchanged storage, allocation and validator code. Source renders and actual player screenshots are distinct evidence. No frame-rate or large-factory performance claim is made by this report.

The published wiki was verified through its live Home and electricity pages after push, revision `d212d26`. Its five player pages plus sidebar/footer are maintained under `.docs/wiki/`; `Tools/publish_wiki.py` copies and validates page links into an existing wiki checkout without deleting other pages.

## Actual player captures

![Battery bank supplies a lamp after generation stops](workshop-battery-bank-powered-lamp-2026-09-10.png)

![Battery bank interface](workshop-battery-bank-control-2026-09-10.png)

![Held tank controller](workshop-held-163-2026-09-10.png)

Additional reviewed captures: [unpowered pump over renewable water](workshop-pump-unpowered-renewal-pool-2026-09-10.png), [pump intake status](workshop-pump-intake-status-2026-09-10.png), [ordinary block](workshop-held-3-2026-09-10.png), [pickaxe](workshop-held-41-2026-09-10.png), [sword](workshop-held-42-2026-09-10.png), [battery held in hand](workshop-held-168-2026-09-10.png).
