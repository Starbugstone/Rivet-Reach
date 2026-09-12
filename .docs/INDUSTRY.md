# Blue Signal and the first industrial workshop

> **2026-09-11 persistence extension:** [SAVES.md](SAVES.md) owns the implemented Save Game, Load Game and Continue Latest Save behavior. Its bounded surface-world persistence supersedes earlier session-only/durable-save exclusions below; older verification retains its original artifact identity.

Working implementation under [issue #2](https://github.com/Starbugstone/Rivet-Reach/issues/2), selected by the user on 2026-09-10. Numerical tuning and the original Blender assets remain subject to play and artistic review. [Verification](verification/INDUSTRY_RESULTS.md) records the measured build and limitations.

**Electricity powers electrical machines. Signal tells machines what to do.** Pumps operate without electricity so water supply can start and recover steam generation. Blue control, electrical power, items and water use four separate networks. Neither ordinary blocks nor ordinary item/fluid pipes conduct blue signal. [Pipe channel fittings and multiblock controls](MULTIBLOCKS.md) subsequently add explicit hybrids. Programmable logic, broader sensors and durable saves remain later extensions.

## Progression and authoring

Azure Ore is another finite stone replacement in `OreGenerator`: Y −96 through +8, peak candidate Y −40, radius 3, chance 38. Existing five bands and protected bedrock remain intact. Copper or better pickaxes extract the ore; the normal furnace converts one ore into one Azure Crystal in ten seconds. It has bright cyan inclusions, without a dynamic light per ore. Generator identity is `terrain-6-azure`; session worlds regenerate on new game, with no durable-save migration.

Craft a Machinist's Bench from a workbench, four iron ingots and two copper ingots at the workbench. Place it and use Interact or mouse Use to open the common inventory with a 4×4 crafting grid. Its recipe browser reads the same compiled registry as personal and workbench crafting. There is no XP or hidden research gate.

The 2026-09-11 Azure art revision follows the [Blue Signal concept sheet](concepts/signal-power/blue-signal-components.jpg): fractured dark stone with connected blue mineral seams. [create_azure_ore.py](../Tools/create_azure_ore.py) owns the dedicated [AzureOre.blend](../ArtSource/Industry/AzureOre.blend), exported mesh and inventory icon. Its orthographic face render supplies terrain tile 44; deposits retain the shared chunk mesher and one-cell collision. Held and dropped ore use the authored mesh. This replaces the old flecked cube in `WorkshopKit.blend`; the general kit generator excludes Azure ore. [Azure art verification](verification/AZURE_ORE_RESULTS.md) records the current source/import and player evidence. Ore generation, mining tiers, processing and saved identities are unchanged.

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

The different component layouts avoid ambiguous matches; a larger stack in the same single cell cannot choose a different recipe. Other industrial recipes are shapeless, with quantities shown on each ingredient cell. Editable assets live in `Assets/RivetReach/Resources/Definitions/Recipes/Industry*.asset`; processing uses `Process*.asset`. Existing survival layouts and quantities remain unchanged. Sand smelts to Glass for indicators and lamps. The complete machine and connector recipes are available through the [item sidebar](CRAFTING.md#item-sidebar-and-recipe-discovery--2026-09-10), including the 4×4 station requirement and upstream processing paths.

## Placement and ports

Each assembly occupies one authoritative cell and supports four horizontal orientations. Initial placement faces the player; its interface offers **Rotate ports 90°**. Rotation retains inventories and work and invalidates topology. Signal and shaft orientation rotate; configured pipe-end directions remain fixed in world space. No independent per-cell inventories or hidden block conduction. Floor Signal Wire requires solid support and drops once when that support is removed. Enclosed conduits and other connectors route through all six orthogonal neighbors, including vertically. No diagonal connection.

Default model front is −Z. The 2026-09-12 all-face connection revision supersedes the original fixed power/item/fluid sockets. The existing model fittings identify channels; they do not restrict which face accepts a cable or pipe.

| Channel | Shape / marking | Supported connection |
|---|---|---|
| Electrical power | Heavy round socket with three contacts; black cable and brass collars | All six faces |
| Blue signal | Small square mounting plate with cyan key | Front |
| Item input / output | Square transfer throat and flange | All six faces; each pipe end configurable |
| Water input / output | Round nozzle and union | All six faces; each pipe end configurable |
| Mechanical shaft | Exposed axle / coupling | Boiler right → alternator left |

Boiler water can enter from any face; item fuel enters only at its rotated rear. Pump world-water intake is still the source cell directly below; placing a pipe in that cell prevents source extraction. Relay receives at its rear and emits at its front. A lever/button connects horizontally; a signal indicator and hatch receive at their front. An unattached control port defaults enabled for controlled machines. Attaching a conductor whose signal is OFF disables processing. Inspecting a machine shows status, received/requested watts, water or fuel where applicable, and its port names. Power priority cycles High / Normal / Low.

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
| Crusher | 160 W; 5 seconds at full allocation; 1 raw copper/iron/gold → 2 crushed corresponding ore; 1 stone or cobblestone → 1 sand |
| Pump | No electricity required; 2 eligible seconds; removes one actual source below for 10 L in its buffer; flowing water is not accepted |
| Drill | 240 W; 6 seconds per block at full allocation; excavates the finite column below, through iron-tier mineable materials; stops at bedrock or an obstacle |
| Water Tank | 100 L; full 10 L bucket transfers or rejection without consuming the bucket |
| Extractor | Adjacent chest on left → configured item output on any face; one item per five ticks; optional signal control |
| Inventory Sensor | Reads the chest behind it; emits ON at 32 total items; fixed initial threshold |

Crushed copper, iron and gold smelt to one corresponding ingot each in the normal furnace. A full output buffer requests no processing power and consumes no new input. Underpower advances work proportionally; no input is destroyed by a blackout or signal shutdown. Changing a crusher's input identity resets its paid progress. A drill revalidates the target before removal and produces its inventory output through the same synchronous authority turn, without also creating a mined world drop.

The user requested electricity-free pumps on 2026-09-12 to remove the water/power startup dependency. Pumps have no electrical endpoint or demand; an adjacent cable or battery cannot power or slow them. Each extraction takes 40 eligible 20 Hz ticks. Optional Blue Signal still pauses processing and retains partial work; full buffers, invalid sources, unloaded intake and dormant machines still block extraction. Fluid pipes can supply an empty fueled boiler before its alternator produces power, and restore water after a blackout. Existing pump buffers and partial work use the same save fields and content identities. [Pump verification](verification/PUMP_RESULTS.md) records measured checks.

The user authorized stone and cobblestone crushing into sand on 2026-09-12. The working yield is one sand per block, with the existing cycle and power requirement. Recipe discovery and machine processing share both output identity and quantity; ore recipes retain their doubled yield. [Crusher sand verification](verification/CRUSHER_SAND_RESULTS.md) records the focused checks.

Item pipes deliver machine outputs, extractor contents or configured chest outputs to compatible machine inputs, furnace ingredients/fuel or a chest inlet. A chest, furnace and each individual machine face terminate a graph; they cannot invisibly bridge separate pipe runs. One source inventory advances at most four items/s across all of its output ends, with deterministic rotating source/destination order. Source candidates are captured before transfers so a newly received item cannot be forwarded by an initially empty chest in the same phase. Fluid pipes carry water quantities, never world flow cells or particles. Each source port transfers at most 100 mL/tick, and all fluid transfers reserve against amounts at the beginning of the transfer phase. A tank cannot forward newly received water in that same phase.

## Machine item inputs by face — 2026-09-12

The user requires the **back to accept fuel only on machines that require item fuel**. Every other input face accepts usable recipe ingredients only. Machines without fuel requirements accept their ordinary supported input on every face, including the back. Faces are relative to the placed machine rotation, including loaded orientations. This changes incoming item routing, not configured output behavior or fluid/electricity channels.

Furnaces choose the fuel slot at the rear and the ingredient slot everywhere else before checking compatibility, capacity and receiver preference. Dual-purpose logs therefore have an explicit route for each purpose, with no fallback between slots. Boilers have only an item-fuel buffer and accept coal/charcoal through the rear; their other item faces have no compatible ingredient slot. Crushers accept raw ingredients on all six faces. Output-only devices still gain no input inventory. The shared inventory boundary carries the destination-local face; inventories and recipes retain authority over accepted items.

Existing saves keep all contents, orientation and pipe settings without new fields or content IDs. Previously side-fed fuel lines need to be moved to the rear; rejected cargo stays at its source. [Fuel-face verification](verification/FUEL_FACE_RESULTS.md) records measured checks and limits.

## Furnace pipes and compatible cargo — 2026-09-12

The user requested automated crusher → furnace transfer and compatibility filtering for item/fluid networks. Furnaces now expose all six faces to Item Pipe, defaulting to blue Input; the selected wrench switches each end independently. Red Output extracts only finished results. Inputs use the compiled furnace recipe/fuel registry: the rotated rear accepts fuel only, and the other five faces accept ingredients only. Logs at the rear burn as fuel; logs at another input face become charcoal. Manual shift-transfer still prefers ingredients. No pipe can insert into the result slot or extract furnace ingredients/fuel. Full or mismatched slots retain cargo at the source. Mixed chests try other available stacks when one is rejected, within the existing shared four-items/s limit. Receivers first request cargo matching their current input or stored product: furnace/ crusher recipes map incoming ingredients to the existing output item, and chests prefer item types already stored. A second pass offers other compatible cargo when no preferred transfer can be made. Both passes share the same source snapshot and throughput budget.

`IItemPipeInventory` is the inventory owner's transport boundary for chests, machines and furnaces. Industrial inputs retain `MachineState.Accepts`: crushers accept their raw metal/stone recipes, boilers accept coal/charcoal, and output-only devices gain no input slot when their arrow changes. `IIndustryItemEndpoints` connects survival stations without duplicating their inventories. Successful furnace insertion and result extraction wake `WorldSurvival` only when work is available. Station placement/removal updates geometry and topology; dormant stations reject transfer. Furnaces terminate pipe graphs and cannot bridge separate runs.

Fluid acceptance is explicit per supported device. Boilers, pumps and the small Water Tank accept water; multiblock fluid endpoints use shared typed storage and reject mixing. Unsupported devices and null fluids reject. Direction settings never bypass compatibility, capacity, valve/signal, formed-structure or drain-only recovery checks. Rejected fluid stays at its source. Only water is playable; alternate-fluid rejection tests use an unregistered fixture definition, adding no game content.

Existing furnace contents and pipe direction fields supply persistence; this fix adds no save fields or content IDs. [Furnace pipe verification](verification/FURNACE_PIPE_RESULTS.md) owns the measured checks and review build; [Pipes](wiki/Pipes.md) teaches the setup.

## Simulation and rendering

`IndustrySimulation` belongs to one `WorldIndustry`, alongside the existing `WorldSurvival`; positions are integer `BlockPos` in that owning world. Future serialization must include world identity. The existing 20 Hz eligible tick stream drives industry while inventory is open; pause and death stop it. Unloaded machines retain input/output, water, fuel, orientation and partial work but do not earn offline production.

`NetworkTopology` shares graph discovery between independent `SignalNetworkService`, `PowerNetworkService`, item and fluid channels. A relay has separate input/output vertices. Topology changes on machine/port edits or chunk residency, never by scanning ordinary voxels each render frame. Rebuild traversal has a 2,048-operation budget per tick; while rebuilding, production pauses. Initial eligible-node collection and sorting still run synchronously, and rebuilding currently pauses all industrial components in this world. These are recorded scale limitations, not a claim of arbitrary-size factory support.

Network registration is independent of electrical supply. A completed topology stays published while a replacement graph is built in private; yielding or cancelling a rebuild cannot erase its connections. Production still pauses during topology work, and eligible machines retain their last completed operating/allocation snapshot until the replacement is ready. Dormant machines clear their transient power immediately. The machine interface shows **Power network: connected / not connected** separately from the received/requested watts and operating status; an empty battery or inactive generator reports **No electrical power**, never a global “Connecting networks” override. [Power-status verification](verification/POWER_STATUS_RESULTS.md) records the focused regression.

Controls evaluate before power allocation; signal changes propagate through cached graphs, and unchanged signal networks avoid reevaluation. Fair allocation serves priority classes in order, divides available whole watts proportionally, and rotates rounding residuals deterministically. Item/fluid transfers precede machine completion, so new processed output is available in a later tick. Rendering cannot produce items, power or water.

`IndustryPresentation` uses nearby eligible assemblies, a shared atlas material plus a shared status-pilot material, and named moving pivots from Blender. Flywheels, crushing rollers, piston, auger, lever and hatch follow authoritative operating state; underpowered machinery animates more slowly. Floor wire arms and connected conduit meshes follow the independent channel topology. Cyan emission indicates active signal independently of electrical availability. Separate pilots show green operation, amber underpower, orange faults and grey signal shutdown; only electrically powered workshop lamps contribute real light, with at most eight enabled nearby lamps. Exact imported triangle counts and workload timings belong in the verification report.

## Asset provenance

The user's [Blue Signal](concepts/signal-power/blue-signal-components.jpg), [electrical components](concepts/signal-power/electric-power-components.jpg) and [workshop](concepts/signal-power/automation-workshop-scene.jpg) sheets guide silhouette, dark iron, copper/brass fittings and cyan signal accents. They are low-resolution visual direction, not production meshes.

`Tools/create_industry_assets.py` creates original metric Blender geometry, moving pivots, a source animation timeline, a shared original atlas and individual rendered icons. `ArtSource/Industry/WorkshopKit.blend` is editable source; runtime FBX/PNG files live under `Assets/RivetReach/Resources/Industry`. Unity uses state-driven pivot animation, avoiding an Animator on every connector; the source timeline remains available to inspect motions. Sources and exports are separate, so running the game does not require Blender. No third-party art or new dependency is introduced.


## Connected pipe presentation

The user's screenshot feedback on 2026-09-10 replaces repeated coupling blocks with continuous runs. Item pipes, fluid pipes, enclosed signal conduits and power cables use the same six-face presentation contract. Straight spans have a uniform section without side stubs, collars or footplates at every cell. Two perpendicular connections select a rounded elbow; three or more select a fitted T, cross or multi-axis branch with only the connected arms. Vertical straights, vertical elbows and all six-way combinations use the same rules. Free ends have a flush cap; a lone unconnected block displays a straight section along its placement rotation until neighbors establish its route.

The topology remains authoritative. Removing a neighbor changes the visible shape after the normal network/presentation refresh; there are no decorative connections to incompatible blocks. `ConnectedPipeVisuals` selects one shared Blender mesh from the matching channel's 6-bit mask. Upgraded item/fluid pipes add at most two more selected meshes, each following its own signal/power mask. Their boundary anchors use world orientation so a differently rotated neighboring pipe still meets the same lead positions. Newly resident views initialize from the current mask even when the topology revision has not changed.

[create_connected_pipes.py](../Tools/create_connected_pipes.py) owns these six exported mesh families, their inventory icons and [ConnectedPipes.blend](../ArtSource/Pipes/ConnectedPipes.blend). It uses the existing original Workshop atlas. Earlier workshop/tank generators exclude these exports so they cannot restore superseded pipe shapes. Each family contains all 64 masks; gameplay instantiates one selected surface per channel, not all variants. `IndustryAssets.PrepareConnectedPipes` normalizes only these families while preserving existing asset GUIDs. [The current showcase and pipe checks](verification/MACHINERY_SHOWCASE.md) record source renders, Unity evidence and remaining review.

## Authorized multiblock and pipe extension — 2026-09-10

The user selected [issue #3](https://github.com/Starbugstone/Rivet-Reach/issues/3): reusable multiblock lifecycle, player-built liquid tanks, connected Blender shell surfaces and shared pipe connections. [MULTIBLOCKS.md](MULTIBLOCKS.md) owns construction, exact shared storage, breach/resize recovery, signal valves/level sensors and independent signal/power fittings on both item and fluid pipes. This supersedes earlier exclusions of those specific hybrid channels and tank controls. [Verification](verification/MULTIBLOCK_RESULTS.md) records the measured build and remaining review. Whole-world durable saves remain later scope.

## Authorized electrical storage — 2026-09-10

The user requested standalone battery blocks and a battery-bank multiblock. [BATTERIES.md](BATTERIES.md) owns the solid-pack construction, exact per-cell storage, electrical allocation and dismantling rules. [The player wiki guide](wiki/Home.md) explains tanks, pump intake height, power and signal connections. [Workshop follow-up verification](verification/WORKSHOP_FOLLOWUP_RESULTS.md) records measured evidence.

## Manual generator — 2026-09-12

The user authorized a [Hand Crank](HAND_CRANK.md) for early-game electrical bootstrap. Its inexpensive workbench recipe, battery-side attachment and click/hold interaction supply 100 W during paid manual turns through one electrical endpoint shared by all six faces. It shares the existing independent electrical network and load-before-storage allocation.

## Wooden door control

[Wooden doors](DOORS.md#blue-signal) accept Blue Signal at the lower cell without electricity. Signal transitions set open/closed, with manual Use available between transitions and occupied-doorway protection. The Workshop Hatch remains a separate assembly.

## Wrench and configurable pipe ends — 2026-09-12

The user requires machine power connections on **all six faces**, including top and bottom. Generators, loads and eligible battery/controller endpoints expose all faces through one electrical vertex, so a machine is allocated once regardless of how many cables touch it. Bank member sockets remain inactive while claimed; bank formation still requires an outward-facing controller. Power remains automatic and has no wrench direction mode.

Item/fluid connections also accept every face of a machine that supports that channel. Each machine-facing **pipe end** independently cycles **Input into the machine → Output from the machine → No connection → Input**. No connection blocks transport at that end and removes its visible arm and arrow. The pipe remains placed; aiming at the same machine-facing side with the wrench reconnects it on the next click. Other ends and fitted power/signal channels remain independent. Directions belong to the pipe's world-facing ends, not the machine's orientation. New connections default from the previous layout where available (e.g. a crusher's left inlet/right outlet), otherwise prefer input for machines with an input buffer and output for output-only machines. Chest ends initially receive items. Once an endpoint exists its default is stored, so rotating/replacing the machine or disabling/re-enabling a tank port does not flip the arrow. An unattached end retains its stored setting for reconnection; mining the pipe removes its settings.

**Only a selected Wrench plus mouse Use (right-click by default) on the machine-facing end can cycle a connection state.** Empty hands, another selected item, a wrench elsewhere in inventory, Interact, a pipe centre, a pipe-to-pipe connection and a power cable do not change it. One press changes once; holding Use does not repeatedly toggle. The normal five-metre, loaded-cell and line-of-sight rules apply. Ordinary Use and Interact continue to open pipe interfaces; crouch-to-place and channel fitting remain available. The HUD explains the current end direction and wrench requirement.

The Wrench is a reusable, nonstacking item (`rivet:wrench`, ID **173**), not a placeable block, mining upgrade or consumable. Its working recipe produces one at a **3×3 workbench** from **three iron ingots**, arranged as `iron iron / empty iron`; horizontal mirroring and translation in the grid are supported. The personal 2×2 grid cannot craft it. [Its generated item page](wiki/Item-wrench.md) shows the exact recipe. It uses the existing tool grip and action swing without changing player models or animation clips.

A **blue arrow enters the machine** for Input; a **red arrow exits the machine** for Output. Arrows are visible **only while the Wrench is selected in hand**, including while processing is idle, storage is empty or a valve is closed. Empty hands, another selected item, or a wrench elsewhere in inventory show no arrows. Disconnected gaps remain visible with any selected item. Both are independent of Blue Signal. Shared code-native arrow glyphs turn around the pipe axis for readability; they are shown on machine-facing item/fluid ends within 32 m, with no per-arrow lights. They do not appear on pipe-to-pipe runs or electrical cables.

Current item endpoints cover crushers (raw input/product output), drill output, boiler fuel input, extractor output and chests. An output-configured crusher never extracts its raw input buffer; an incompatible item is left at its source. An input arrow cannot create a processing input on an output-only device. Fluid directions operate on existing water buffers and shared tank storage. Tank enable/disable, valve signal, formation and drain-only recovery gates still apply. A recovery controller set to input cannot receive fluid, particularly during a breach.

[Save schema 4](SAVES.md#wrench-and-pipe-end-compatibility--2026-09-12) persists the new item and six pipe-end settings while retaining explicit older-save compatibility. [The player pipe guide](wiki/Pipes.md) explains setup and troubleshooting. Original wrench art is authored by [create_wrench.py](../Tools/create_wrench.py), with [Wrench.blend](../ArtSource/Wrench/Wrench.blend), an explicit tool FBX and the matching inventory icon. [Connection verification](verification/CONNECTION_RESULTS.md) records checks and remaining limits.
