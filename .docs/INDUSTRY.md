# Blue Signal and the first industrial workshop

Working implementation under [issue #2](https://github.com/Starbugstone/Rivet-Reach/issues/2), selected by the user on 2026-09-10. Numerical tuning and the original Blender assets remain subject to play and artistic review. [Verification](verification/INDUSTRY_RESULTS.md) records the measured build and limitations.

**Power makes machines work. Signal tells machines what to do.** Blue control, electrical power, items and water use four separate networks. Neither ordinary blocks nor ordinary item/fluid pipes conduct blue signal. [Pipe channel fittings and multiblock controls](MULTIBLOCKS.md) subsequently add explicit hybrids. Programmable logic, broader sensors and durable saves remain later extensions.

## Progression and authoring

Azure Ore is another finite stone replacement in `OreGenerator`: Y −96 through +8, peak candidate Y −40, radius 3, chance 38. Existing five bands and protected bedrock remain intact. Copper or better pickaxes extract the ore; the normal furnace converts one ore into one Azure Crystal in ten seconds. It has bright cyan inclusions, without a dynamic light per ore. Generator identity is `terrain-6-azure`; session worlds regenerate on new game, with no durable-save migration.

Craft a Machinist's Bench from a workbench, four iron ingots and two copper ingots at the workbench. Place it and use Interact or mouse Use to open the common inventory with a 4×4 crafting grid. Its recipe browser reads the same compiled registry as personal and workbench crafting. There is no XP or hidden research gate.

| Component | Ingredients | Output / layout |
|---|---|---|
| Copper Wire | 1 copper ingot | 4; one cell |
| Copper / Iron Plate | 3 corresponding ingots | 3; horizontal row of three |
| Iron Cog | 2 iron ingots | 1; horizontal pair |
| Rivets | 1 iron ingot | 8; one cell |
| Machine Casing | 4 iron plates + 4 rivets | 1 |
| Signal Wire | 1 Copper Wire + 1 Azure Crystal | 4 |
| Signal Conduit | 2 copper plates + 1 Signal Wire | 2 |
| Power Cable | 4 Copper Wire + 1 plank | 4 |

The different component layouts avoid ambiguous matches; a larger stack in the same single cell cannot choose a different recipe. Other industrial recipes are shapeless, with quantities shown on each ingredient cell. Editable assets live in `Assets/RivetReach/Resources/Definitions/Recipes/Industry*.asset`; processing uses `Process*.asset`. Existing survival layouts and quantities remain unchanged. Sand smelts to Glass for indicators and lamps. The complete machine and connector recipes are available through the bench's Recipes button.

## Placement and ports

Each assembly occupies one authoritative cell and supports four horizontal orientations. Initial placement faces the player; its interface offers **Rotate ports 90°**. Rotation retains inventories and work, invalidates topology, and rotates all ports together. No independent per-cell inventories or hidden block conduction. Floor Signal Wire requires solid support and drops once when that support is removed. Enclosed conduits and other connectors route through all six orthogonal neighbors, including vertically. No diagonal connection.

Default model front is −Z. Relative port grammar:

| Channel | Shape / marking | Usual machine face |
|---|---|---|
| Electrical power | Heavy round socket with three contacts; black cable and brass collars | Rear |
| Blue signal | Small square mounting plate with cyan key | Front |
| Item input / output | Square transfer throat and flange | Left / right |
| Water input / output | Round nozzle and union | Left / right |
| Mechanical shaft | Exposed axle / coupling | Boiler right → alternator left |

Boiler water input occupies its rear. Pump intake is the source cell directly below. Relay receives at its rear and emits at its front. A lever/button connects horizontally; a signal indicator and hatch receive at their front. An unattached control port defaults enabled for electrical machines. Attaching a conductor whose signal is OFF disables processing. Inspecting a machine shows status, received/requested watts, water or fuel where applicable, and its port names. Power priority cycles High / Normal / Low.

Mining an assembly removes its network membership, closes its open interface, drops its stored input/output once and returns the placed item through ordinary mining. Already burned fuel, consumed water and spent work are not refunded. Water remaining in a dismantled vessel is discarded; emptying it into buckets first preserves that water. Player and creature overlap checks use the existing placement authority.

## Devices and processes

| Device | Working behavior |
|---|---|
| Lever | Interact / Use toggles ON/OFF |
| Button | One second ON; another press restarts the pulse |
| Signal Relay | Copies its input with at least one 20 Hz step delay |
| Signal Indicator | Tiny blue pilot, signal only; does not illuminate the workshop |
| Workshop Hatch | Opens under signal; avoids closing on a player or creature |
| Workshop Lamp | 20 W; defaults enabled without a signal connection; light scales with received power |
| Boiler Engine | Coal/charcoal burns for 80 eligible seconds; consumes 100 mL water/s; right shaft rotates while fueled and watered |
| Alternator | Correctly aligned adjacent running boiler supplies 400 W electricity; no remote shaft teleportation |
| Crusher | 160 W; 5 seconds at full allocation; 1 raw copper/iron/gold → 2 crushed corresponding ore |
| Pump | 80 W; 2 seconds at full allocation; removes one actual source below for 10 L in its buffer; flowing water is not accepted |
| Drill | 240 W; 6 seconds per block at full allocation; excavates the finite column below, through iron-tier mineable materials; stops at bedrock or an obstacle |
| Water Tank | 100 L; full 10 L bucket transfers or rejection without consuming the bucket |
| Extractor | Adjacent chest on left → item output on right; one item per five ticks; optional signal control |
| Inventory Sensor | Reads the chest behind it; emits ON at 32 total items; fixed initial threshold |

Crushed copper, iron and gold smelt to one corresponding ingot each in the normal furnace. A full output buffer requests no processing power and consumes no new input. Underpower advances work proportionally; no input is destroyed by a blackout or signal shutdown. Changing a crusher's input identity resets its paid progress. A drill revalidates the target before removal and produces its inventory output through the same synchronous authority turn, without also creating a mined world drop.

Item pipes deliver machine outputs or extractor contents to compatible machine inputs or an adjacent chest. A chest is an endpoint, not an invisible bridge. One source advances at most four items/s, with deterministic rotating source/destination order. Fluid pipes carry water quantities, never world flow cells or particles. Each source port transfers at most 100 mL/tick, and all fluid transfers reserve against amounts at the beginning of the transfer phase. A tank cannot forward newly received water in that same phase.

## Simulation and rendering

`IndustrySimulation` belongs to one `WorldIndustry`, alongside the existing `WorldSurvival`; positions are integer `BlockPos` in that owning world. Future serialization must include world identity. The existing 20 Hz eligible tick stream drives industry while inventory is open; pause and death stop it. Unloaded machines retain input/output, water, fuel, orientation and partial work but do not earn offline production.

`NetworkTopology` shares graph discovery between independent `SignalNetworkService`, `PowerNetworkService`, item and fluid channels. A relay has separate input/output vertices. Topology changes on machine/port edits or chunk residency, never by scanning ordinary voxels each render frame. Rebuild traversal has a 2,048-operation budget per tick; while rebuilding, production pauses. Initial eligible-node collection and sorting still run synchronously, and rebuilding currently pauses all industrial components in this world. These are recorded scale limitations, not a claim of arbitrary-size factory support.

Controls evaluate before power allocation; signal changes propagate through cached graphs, and unchanged signal networks avoid reevaluation. Fair allocation serves priority classes in order, divides available whole watts proportionally, and rotates rounding residuals deterministically. Item/fluid transfers precede machine completion, so new processed output is available in a later tick. Rendering cannot produce items, power or water.

`IndustryPresentation` uses nearby eligible assemblies, a shared atlas material plus a shared status-pilot material, and named moving pivots from Blender. Flywheels, crushing rollers, piston, auger, lever and hatch follow authoritative operating state; underpowered machinery animates more slowly. Wire arms select endpoints, straights, corners, tees, crosses and vertical connections from topology. Cyan emission indicates active signal independently of electrical availability. Separate pilots show green operation, amber underpower, orange faults and grey signal shutdown; only electrically powered workshop lamps contribute real light, with at most eight enabled nearby lamps. Exact imported triangle counts and workload timings belong in the verification report.

## Asset provenance

The user's [Blue Signal](concepts/signal-power/blue-signal-components.jpg), [electrical components](concepts/signal-power/electric-power-components.jpg) and [workshop](concepts/signal-power/automation-workshop-scene.jpg) sheets guide silhouette, dark iron, copper/brass fittings and cyan signal accents. They are low-resolution visual direction, not production meshes.

`Tools/create_industry_assets.py` creates original metric Blender geometry, moving pivots, a source animation timeline, a shared original atlas and individual rendered icons. `ArtSource/Industry/WorkshopKit.blend` is editable source; runtime FBX/PNG files live under `Assets/RivetReach/Resources/Industry`. Unity uses state-driven pivot animation, avoiding an Animator on every connector; the source timeline remains available to inspect motions. Sources and exports are separate, so running the game does not require Blender. No third-party art or new dependency is introduced.


## Authorized multiblock and pipe extension — 2026-09-10

The user selected [issue #3](https://github.com/Starbugstone/Rivet-Reach/issues/3): reusable multiblock lifecycle, player-built liquid tanks, connected Blender shell surfaces and shared pipe connections. [MULTIBLOCKS.md](MULTIBLOCKS.md) owns construction, exact shared storage, breach/resize recovery, signal valves/level sensors and independent signal/power fittings on both item and fluid pipes. This supersedes earlier exclusions of those specific hybrid channels and tank controls. [Verification](verification/MULTIBLOCK_RESULTS.md) records the measured build and remaining review. Whole-world durable saves remain later scope.
