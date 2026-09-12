# Player-built multiblock machinery

> **2026-09-11 persistence extension:** [SAVES.md](SAVES.md) owns the implemented Save Game, Load Game and Continue Latest Save behavior. Its bounded surface-world persistence supersedes earlier session-only/durable-save exclusions below; older verification retains its original artifact identity.

Working implementation selected by the user from [issue #3](https://github.com/Starbugstone/Rivet-Reach/issues/3) on 2026-09-10, including shared pipe connections, original Blender assets and individually editable connected shell blocks. [In-game showcase](verification/MACHINERY_SHOWCASE.md) presents the tank and working workshop. [Verification](verification/MULTIBLOCK_RESULTS.md) separates measured checks from artistic and survival-play acceptance. Existing [industry](INDUSTRY.md), [world fluids](FLUIDS.md) and session-only world lifetime remain in place.

## Build and operate a tank

Craft components at the Machinist's Bench, or obtain them through the session Creative catalog. Build a hollow rectangular shell with outer dimensions **3–9 blocks on each axis**, independently: 3×3×8 and 5×4×5 are legal. Put Reinforced Tank Frames on every edge and corner, Tank Walls across floor and roof, and walls/glass across the vertical faces. Face-interior cells may contain the controller, ports, hatches or sensors. Exactly one controller is required. Controllers and functional components face outward; placement faces the player, and their existing interface offers **Rotate ports 90°**. Solid walls/glass/frame orientation does not affect validation.

The interior must contain only air. Terrain, machines, protruding tank blocks and world-fluid sources/flows prevent formation. A bounded flood-fill starts behind the controller, discovers the cavity, checks rectangular completeness and validates every shell role. Neighboring tanks remain independent; a formed structure cannot share a shell member with another formed structure. Opening a frame edge also breaks validation even when that edge did not border a flood-filled air cell.

Formation adds a `MultiblockInstance` over the actual placed cells. No prefab replaces the shell and no new world cells represent stored liquid. Mine any ordinary member to recover that component through normal mining. The controller, ports and hatches resolve the same authoritative fluid storage. Inspect a member to find its controller; **Re-scan Structure** schedules manual validation. The controller shows dimensions, quantity/capacity, formation state and the first diagnostic coordinate, with a world-space outline while its interface is open.

Capacity is **250 L per interior cell**: `(width − 2) × (height − 2) × (depth − 2) × 250 L`. The existing compact 100 L tank remains available. These are working content defaults, not final balance. The tank stores exactly one stable `FluidDefinition` identity at a time using integer millilitres and 64-bit quantity/capacity. Emptying to zero clears identity; a different fluid can then be selected. Water is the only registered playable fluid today. No tank needs electricity simply to retain contents.

| Component | Bench ingredients | Output |
|---|---|---:|
| Reinforced Tank Frame | 3 Iron Plate + 4 Rivets + 1 Copper Plate | 4 |
| Tank Wall | 1 Machine Casing + 4 Iron Plate | 4 |
| Reinforced Tank Glass | 4 Glass + 4 Rivets | 4 |
| Tank Controller | 1 Tank Wall + 1 Cog + 1 Azure Crystal | 1 |
| Tank Fluid Port | 1 Tank Wall + 1 Fluid Pipe | 1 |
| Tank Access Hatch | 1 Tank Wall + 2 Copper Plate | 1 |
| Signal Valve Port | 1 Tank Fluid Port + 1 Signal Conduit | 1 |
| Tank Level Sensor | 1 Tank Wall + 1 Signal Wire + 1 Glass | 1 |

Recipes use distinct ingredient layouts in the shared catalog and are available through its recipe browser. Existing beginning survival recipes are preserved.

## Connections and controls

Tank Fluid Ports expose fluid connections on all six faces. Their interface toggles **Enabled / Disabled**; direction is configured separately at each machine-facing pipe end using a held [Wrench](INDUSTRY.md#wrench-and-configurable-pipe-ends--2026-09-12). Blue arrows enter the tank and red arrows exit it. Multiple ports share storage; none has a hidden local buffer. Signal Valve Ports use the same enabled/disabled gate and per-end directions, but require an attached **ON** Blue Signal to pass liquid. OFF or unattached signal closes a valve. Fluid and signal interfaces share the outward face, with a separate small square blue key beside the round nozzle, allowing the valve to work while surrounded by other shell panels.

Tank Level Sensors emit binary Blue Signal on their outward face when fill is at or above the configured threshold. The default is 80%; the interface cycles 10% increments through 100%. Sensors and valves do not consume electrical power. Signal remains a separate graph and never acts as water or electricity.

Both **Item Pipe and Fluid Pipe** can be fitted with one Signal Conduit and/or one Power Cable through their normal interface. Each action consumes that component from the inventory. The same physical pipe then exposes two or three independent channel vertices, with independent connection masks and Blender-authored continuous offset leads. Upgrades survive session chunk unloading and are returned once when the pipe is mined. Ordinary pipes retain only their original transport channel. Power additions conduct electricity and do not grant a fluid or item endpoint to a cable; signal additions conduct control without powering a machine. Uprating throughput or programmable routing is not part of these additions.

`PipeConnections.Ports`, `WorldFaces` and `Matches` provide the shared six-neighbor connection contract used by all four `NetworkTopology` graphs. Connected pipe meshes follow the matching graph's faces; item pipes also show terminating chest connections. Chests do not bridge networks. Changing rotation, channel fittings, endpoint direction, enable state, structure formation or residency rebuilds topology. Existing end directions remain fixed when the machine rotates. No channel discovery scans terrain every render frame.

Fluid transfer reserves all source quantities and destination capacities at the start of the transfer phase, **keyed by the storage object across every port and every fluid graph**. A port may emit at most 100 mL per tick. A pipe component with conflicting stored fluid identities pauses transfers and exposes a fluid-conflict diagnostic. Wrong-fluid reservations reject before withdrawal, including two different candidate types approaching an initially empty storage through different graphs. Commit runs in the same synchronous authority turn after all reservations, with no intervening edits. Newly received fluid cannot be forwarded in that phase. Transfers from a tank to itself are skipped even through different ports. Small vessels use the same exact storage accounting, with water-only machine inputs retaining their current restriction.

## Buckets, breaches and resizing

Controller and Access Hatch **Add 10 L / Take 10 L** buttons use the ordinary fluid bucket registry. Transfers require a compatible complete container and the full quantity/capacity; replacing the same inventory slot works even when every slot is occupied. Rejection changes neither the bucket nor fluid. Multiple hatches are supported.

An affected member, interior or nearby candidate edit immediately marks the instance pending and stops ordinary transfers before the next simulation step. Revalidation runs at most one bounded structure scan per industry tick. A broken shell retains its exact contents at its controller. Repair restores the same structure identity and storage. Expanding the same controller's shell increases capacity; shrinking is allowed only if existing contents fit. Otherwise the controller reports the excess, retains the previous storage capacity and stops normal operation until sufficient contents have been recovered.

Full buckets can be recovered at an invalid controller. **Recovery Out: ON** additionally opens an explicit drain-only outlet on any exposed controller face for a connected Fluid Pipe whose end is set to red Output, including amounts smaller than 10 L. This is an output-only manual recovery choice, initially OFF. It is available for a formed or invalid structure and pauses while validation or chunk readiness is pending. It prevents a sub-bucket remainder from trapping the controller. Ordinary invalid tank ports remain closed. Recovered liquid goes into another compatible storage with normal exact reservations; no world source cells are created.

**Drain before removing the controller.** Both ordinary mining and direct world removal reject dismantling a nonempty controller, with player feedback. An empty controller can be mined, releasing membership. Structural blocks and fitted pipe components retain normal single-drop behavior. V1 does not spill fluid into the terrain, discard a breached tank's fluid or implement recovery parcels.

## Reusable lifecycle and residency

`MultiblockDefinition` supplies stable identity, part roles, scan bounds, an interchangeable `IMultiblockValidator` and a machine-data factory. `MultiblockService` owns identity, claims, edit invalidation, revision checks and the validation queue. `IMultiblockMachineData` owns formation-dependent machine state and dismantle constraints; `TankMachineData` supplies tank capacity/storage. A future furnace, turbine or battery can provide a different validator and machine-data implementation without adopting the tank cavity scan or fluid inventory.

Each owning session has a world GUID; each controller instance has a structure GUID and increasing revision. Integer `BlockPos` addresses survive floating-origin shifts. Scans publish only against their captured revision. Edits use a chunk-indexed bounded controller footprint, including missing and interior cells. The tank definition sets an independent **4,096 unique-cell read budget** as well as its 9-block dimension limit. Unbounded open-world scans terminate with a diagnostic. Only controllers schedule validation; touching shell components do not flood through neighboring machines.

Every cell in the candidate bounds must be ready, including the cavity. An unloaded part pauses formation and normal transfers, while controller identity, amount, fluid identity and machine/pipe configuration remain in session memory. Reload schedules validation and restores the same instance. No offline production is granted. Residency changes currently invalidate all controllers, and graph rebuilding still pauses this world's industry; those are scale limitations rather than arbitrary-size factory guarantees.

This is session persistence across chunk residency, consistent with the existing world. Cross-process durable saves, recovery manifests and save migration require the later whole-world persistence system; this feature does not independently save tanks into a world that otherwise regenerates.

## Connected art

The user's [component sheet](concepts/multiblock/tank-concept.png), [construction/scaling sheet](concepts/multiblock/tank-parts.png) and [additional tank reference](concepts/multiblock/tank-construction.png) from issue #3 guide dark iron, copper/brass rivets, clear broad windows, gauges and round fluid versus square signal fittings.

[create_multiblock_assets.py](../Tools/create_multiblock_assets.py) authors original metric shell parts in Blender using the existing original Workshop atlas. [TankKit.blend](../ArtSource/Multiblocks/TankKit.blend) retains editable geometry and relative textures. Explicit FBX exports, rendered item icons and normalized Unity prefabs are versioned under `Resources/Industry`; playing does not require Blender. Existing player assets and animations are untouched.

Pipe bodies and their channel leads are authored separately by [create_connected_pipes.py](../Tools/create_connected_pipes.py); [industry](INDUSTRY.md#connected-pipe-presentation) owns their shape and connection rules.

Formed views select outward faces, activate the frame’s authored sealing skins and suppress shared borders between matching neighboring panels. Loose frames retain their open construction appearance. Selected Blender meshes are combined and cached by surface configuration, retaining individual per-cell views for mining. Glass has its own shared transparent material; metal retains the industrial atlas. The fluid is one presentation-only volume with a horizontal top surface, scaled from quantity and colored by `FluidDefinition`. Internal world cells remain air. Views follow floating origins and are culled beyond 64 m. Reported source/import triangle counts are distinct from the smaller formed meshes; rendering quality and performance depend on the measured scene in the verification report.

## Authorized electrical storage — 2026-09-10

The user requested standalone battery blocks and a battery-bank multiblock. [BATTERIES.md](BATTERIES.md) owns the solid-pack construction, exact per-cell storage, electrical allocation and dismantling rules. [The player wiki guide](wiki/Home.md) explains tanks, pump intake height, power and signal connections. [Workshop follow-up verification](verification/WORKSHOP_FOLLOWUP_RESULTS.md) records measured evidence.
