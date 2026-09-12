# Batteries and battery banks

> **2026-09-11 persistence extension:** [SAVES.md](SAVES.md) owns the implemented Save Game, Load Game and Continue Latest Save behavior. Its bounded surface-world persistence supersedes earlier session-only/durable-save exclusions below; older verification retains its original artifact identity.

## Authorization and working rules

The user requested electrical storage as a standalone battery block and as cells in a battery-bank multiblock on 2026-09-10, alongside the then-current powered-pump rule. The user superseded that rule on 2026-09-12: [pumps now require no electricity](INDUSTRY.md) so they can start and recover water-dependent generation. This document owns battery semantics; [INDUSTRY.md](INDUSTRY.md) owns generators and electrical consumers, and [MULTIBLOCKS.md](MULTIBLOCKS.md) owns the shared validation/lifecycle infrastructure. [The player guide](wiki/Electricity-and-batteries.md) provides construction instructions.

Working defaults chosen for this increment, not individually user-approved balance values:

- Battery Block, runtime ID 168: 100 kJ capacity, 400 W maximum input or output, initially empty. Its six faces share one electrical storage endpoint.
- Battery Bank Controller, ID 169: no capacity by itself, one electrical storage endpoint on all six faces, initially Automatic mode.
- Four modes: Automatic, ChargeOnly, DischargeOnly and Isolated. A formed bank's controller mode overrides its cells' modes; those cell modes resume after release.
- Storage is lossless in this first increment. No passive decay, offline progress or free Creative charge. All stored charge is session state.
- Crafting at the 4×4 Machinist's Bench: cell = casing ×1, copper plate ×2, copper wire ×4, coal ×2; controller = casing ×1, copper wire ×4, Azure crystal ×1, glass ×1. These are original industry assemblies; beginning survival recipes remain unchanged.

## Electrical allocation and conservation

Existing generator watts first serve ordinary machine demand, using the established priorities and proportional shortfall allocation. Only generation exceeding that demand may charge storage. When demand exceeds generation, eligible batteries discharge just enough to reduce that shortfall, limited by their rate and current charge. Storage never charges other storage within this allocation. Isolated and direction-restricted endpoints obey their selected mode.

`BatteryStorage` holds exact integer millijoules. A 20 Hz step uses 50 mJ per supplied watt, with per-cell capacity checks before mutation. Full cells accept only available room; exhausted cells stop delivery and may provide partial watts on their final step. Idle networks, full pump buffers and disabled consumers do not discharge batteries. Generator surplus may be unused when storage is full, as electricity generation is a power budget rather than a stored resource itself.

`PortRole.Storage` is one bidirectional endpoint, never simultaneous input/output vertices that could count its capacity twice. An eligible formed bank exposes only its controller. All cell sockets are removed from topology until those cells are released. Rebuilding networks suspend allocation; dormant cells never generate offline credit.

Battery order is deterministic by topology/position. This increment does not promise balanced cell wear, equal state of charge or charging priorities. Stored energy and input/output limits are separate quantities; displayed watts are actual transfer during the latest fixed step.

## Bank shape and lifecycle

The shared `MultiblockService` hosts `BatteryBankValidator` as a second production validator, alongside hollow tanks. A bank is one face-connected, completely filled rectangular pack of Battery Blocks and exactly one Battery Bank Controller. Each dimension is 1–5 inclusive, with at least one cell plus the controller. The controller's front must point outside the bounding box. The bounded scan allows at most 1,024 reads; a largest 5×5×5 pack has 124 storage cells, 12.4 MJ capacity and 49.6 kW transfer limit.

This solid pack is a working construction choice: players reuse the functional battery block itself, without an additional shell or hidden storage item. Separate banks must not touch; a connected pack containing two controllers is invalid. Missing cells, inward controls, oversized packs and unavailable neighbouring terrain produce actionable validation failures. Successful validation claims members exclusively through the existing world service.

Charge belongs to each `MachineState` cell, not to an aggregate copied into the controller. `BatteryBankData` holds references to the claimed cells in deterministic order. Formation, repair, enlargement and controller removal cannot create or erase charge. The controller is removable even when the pack is charged because it owns no energy. An individual cell is mineable only at zero charge; it must first discharge into a real load.

A pending bank suspends its endpoints. Invalid or waiting validation releases member claims: ready unclaimed cells resume standalone operation, and their individual attached cables can carry power. The bank controller remains inactive until its complete pack validates. A repaired pack reclaims existing cell storage without offline credit. This differs deliberately from tank breach recovery: a tank's liquid stays at its controller, while battery energy has durable identity within each session cell.

## Presentation and diagnostics

The original Blender kit and icons come from [create_battery_assets.py](../Tools/create_battery_assets.py), with editable [BatteryKit.blend](../ArtSource/Batteries/BatteryKit.blend). It reuses the project's original Workshop atlas and geometry helpers. Battery cells expose visible accumulator cans and six electrical fittings; the controller has a front gauge, power socket and pilot light. Geometry is imported as two shared mesh groups per item, without a controller animation pretending to generate power.

**Visible charge — 2026-09-12:** placed Battery Blocks contain a translucent amber, softly emissive fill. Its height is proportional to the physical cell's exact stored energy: empty hides the fill, 11.8 kJ / 100 kJ fills 11.8% of the internal height, and full reaches the top of the chamber. It updates on energy changes, including charging, discharge and reconstruction after load. A bank member shows its own cell charge, even while its electrical endpoint belongs to the controller; the controller UI remains the aggregate display.

This is an electrical indicator with a liquid-like appearance, not a bucketable fluid or a new stored resource. `IndustryPresentation` adds one shared 12-triangle cube and the shared `BatteryCharge` material to each nearby cell view, inside the original Blender housing. It adds no light, collider, simulation component or save field. The fill inherits rotation, origin shifts and the existing 64 m/residency view lifecycle. The existing source meshes, icons and empty held items remain the battery kit. [Charge-fill verification](verification/BATTERY_FILL_RESULTS.md) records the focused playable build and remaining limits.

Machine interfaces show exact stored kJ, capacity, charge/output watts, mode and bank status. A member cell indicates controller ownership. The same item registry feeds Creative catalog grants, crafting and held rendering. [Verification](verification/WORKSHOP_FOLLOWUP_RESULTS.md) records measured checks and remaining limits.

## Manual early-game charging — 2026-09-12

The [Hand Crank](HAND_CRANK.md) attaches directly to a battery side and generates a small amount of electricity with Interact or right-click, repeating while mouse Use is held. It uses ordinary generator allocation, capacity limits and battery modes. Its workbench recipe, 50 J turns, rear-socket placement and save compatibility are owned by that specification.
