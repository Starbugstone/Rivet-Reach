# Rivet Reach - Resources, Extraction and Progression

> **Status:** working design resolution, 2026-09-08. The user requested concrete solutions to the design gaps. These rules are the current specification for future implementation, not playtest-validated balance. Numerical values below are initial tuning data.

Related: [GAMEPLAY.md](GAMEPLAY.md), [SIMULATION.md](SIMULATION.md), [DEVELOPMENT_STRATEGY.md](DEVELOPMENT_STRATEGY.md), [DESIGN_QUESTIONS.md](DESIGN_QUESTIONS.md).

## 1. Resource economy contract

The industrial loop is **find deposit -> extract -> process -> construct useful capacity -> reach another deposit or capability**. A chest of supplied ore is a diagnostic fixture, not the complete gameplay loop.

Ore and ordinary stone are finite voxel terrain. There is no hidden second reserve behind an ore block and no automatic ore regeneration when a chunk reloads. New deposits extend with the procedural world. Wood/crops are renewable through local growth systems; water sources are renewable under the explicit world-water rules. Neither renewable system creates ordinary metal ore or realm components through a conversion cycle.

Manual mining removes one authoritative block and creates its defined drops as world-item stacks. Industrial extraction removes the same block with the same base yield but reserves that yield into the machine's output buffer. This is deliberate: powered extraction substitutes a machine collection step for manual pickup; it does not change what was mined. Optional physical ejectors can discharge existing inventory into world piles. Pipes do not create physical items merely to display movement.

Resource identities distinguish terrain, raw material, intermediate and finished component. A raw-ore item cannot be cheaply placed as a richer ore block and mined repeatedly. If an ore block is recoverable as a placeable block, its recipe/drop rules must conserve its original extractable yield. Crushing gains yield through a named processing recipe, not a generic multiply-on-transfer mechanic.

## 2. Powered extraction

### First drill

A fixed, oriented drill works through a narrow forward bore. Initial footprint: one machine block; target selection is the first eligible block along its facing axis within 8 blocks. Stone obstructing ore must be excavated too; the drill cannot scan through intact terrain and select only valuables. Extend reach or reposition it through ordinary construction.

Initial requirements: 400 W while cutting, 8 seconds for ordinary stone/ore, one output inventory. Hardness changes duration, and tool grade determines which blocks are eligible. Energy is spent on cutting progress. The initial drill has no consumable bit wear; later durable head upgrades expand capability rather than adding obligatory per-block maintenance.

The machine exposes target, progress, output fullness, power demand and reasons for stopping. Protected blocks, another machine, a Gate core or a claim boundary stop the bore. It never destroys containers or occupied machine footprints by treating them as stone.

### Later quarry

A quarry uses the same extraction command against a player-marked bounded volume, with a deterministic traversal order and a visible preview. It has a larger construction cost, storage/logistics demand and upgradeable work rate. It is not a new resource generator or a separate background-only mining algorithm.

### One extraction commit

1. Identify the target's world, chunk, coordinate, type and revision.
2. Validate tool capability, permissions, source and target production eligibility, and output capacity for the full drop bundle.
3. Reserve the target and destination inventory space; competing miners/manual commands cannot reserve the same revision.
4. Accumulate paid work while the reservation remains valid. If a player changes the target, cancel its reservation and re-evaluate; consumed work/energy does not create output.
5. Commit block removal and buffered drops together at one simulation boundary. Mark mesh, collision, light, water and navigation dependants dirty.
6. If commit validation fails, release reservations and retain unconsumed resources. Never spawn a replacement world drop as a second output.

A full output pauses cutting before more energy is spent. Already paid progress remains if the same target is still valid. Progress and reservations are represented in durable state; recovery either restores the pending operation or the committed result, never both.

### Background mining

A machine and every target chunk need valid production eligibility. A drill does not extend its owner's loading quota by reaching into dormant terrain. The player can see and cover the needed region with explicit factory tickets.

Eligible background miners load the authoritative voxel pages they read/change and update terrain without creating meshes, renderers or general-purpose physics. Water evolution remains a separate capability. On player arrival, collision and visible geometry are prepared from current revisions before access. This preserves actual excavation without making distant quarries run a visible scene.

## 3. First resource and recipe set

The first complete sandbox/industrial slice uses logs, stone, iron ore, copper ore and water. Charcoal provides starter fuel from logs; coal can be added as a naturally found alternative. Surface wood and mineable stone are available near valid spawn locations. Spawn validation also requires reachable iron and copper within a provisional 256-block search region, without replacing player exploration with map markers. An unsuitable candidate spawn is rejected deterministically, not repaired after exploration order changes.

Initial recipes use a 2x2 personal grid and a 3x3 workbench. Shaped layouts are content data; the table specifies material quantities and manufacturing dependencies. The browser shows layouts from that same registry. All recipes below are accessible without knowledge/XP flags when their materials and station are present.

| Output | Inputs | Process / purpose |
|---|---|---|
| 4 planks | 1 log | Personal crafting; first construction material |
| 4 handles | 2 planks | Personal crafting |
| Workbench | 4 planks | Personal crafting; enables larger assemblies |
| Wooden pick | 3 planks + 2 handles | Workbench; mines stone |
| Stone pick | 3 stone + 2 handles | Workbench; mines initial metal ores |
| Furnace | 8 stone | Workbench; fuel processing and basic smelting |
| Chest | 8 planks | Workbench; one inventory model shared with machines |
| 1 charcoal | 1 log | Furnace; one smelting operation |
| 1 iron/copper ingot | 1 matching raw ore | Furnace; direct route needed before powered processing |
| 1 plate | 1 iron or copper ingot | Workbench; initially slow manual shaping, no powered press dependency |
| 4 copper wire | 1 copper ingot | Workbench; manual drawing route for first generator |
| 1 gear | 2 iron ingots | Workbench; mechanical assembly |
| 1 casing | 4 iron plates | Workbench; common machine frame |
| 1 bucket | 3 iron ingots | Workbench; moves one source cell / 10 L under water rules |
| 1 water tank | 4 copper plates + 2 iron plates | Workbench; 100 L capacity |
| 4 fluid pipes | 2 copper plates | Workbench; same fluid type/amount model as tanks |
| Boiler-engine | 1 casing + 4 copper plates + 2 gears | Workbench; charcoal + water -> shaft power |
| Alternator | 1 casing + 2 gears + 8 copper wire | Workbench; adjacent mechanical input -> electricity |
| 4 power cables | 4 copper wire + 1 plank | Workbench; cable item bundle, insulation represented by recipe |
| Crusher | 1 casing + 2 gears + 4 iron plates | Workbench; first powered ore-yield improvement |
| 2 crushed ore | 1 matching raw ore | Crusher; each intermediate smelts into 1 ingot |
| 4 item pipes | 2 iron plates + 2 copper wire | Workbench; inventory-to-inventory logistics |
| Extractor attachment | 1 gear + 2 copper wire | Workbench; chooses extraction side of an item connection |
| Switch + 4 signal conduits | 1 handle + 1 stone + 2 copper wire | Workbench; one small control bundle |
| Pump | 1 casing + 1 gear + 4 copper wire | Workbench; source water to tank/pipe |
| Drill | 1 casing + 2 gears + 4 iron plates + 4 copper wire | Workbench; automates finite terrain extraction |

One plank supplies one furnace operation; one charcoal supplies eight. Operations take an initial 8 seconds. Fuel credit stays in the furnace and is persisted; closing the interface does not reset it. No recipe converts crafted plates, gears or machines back into more raw material than was consumed. Recycling later specifies deliberate losses or exact recovery, not generic arithmetic by item category.

The wooden tool exists to bootstrap stone without a metal dependency. The furnace and manual component recipes bootstrap machinery without already owning a crusher, press, pump or generator. Machine production later improves speed/batching, not permission to make the first machine.

## 4. First-session and industrial sequence

Stage 0 demonstrates movement and editable terrain; Stage 1 adds this real gathering/crafting loop. The first 20-30 minutes should support building, inventory use, smelting and saving without developer commands; it does not promise that every player finishes industrialization in that interval.

The first industrial sequence is:

1. Gather wood and stone, craft tools/workbench/furnace, then find and smelt both ores.
2. Handcraft plates/gears/wire; build a boiler-engine and alternator. Fill a tank manually using a bucket; automation is not needed to bootstrap water.
3. Build a crusher. Process manually gathered ore at better yield and observe power/water/fuel consumption.
4. Add chests, pipes and an extractor; stop a full-output or disabled machine using clear status and signal control.
5. Add a pump to remove water-carrying work. Add a drill to remove repeated manual extraction.
6. Expand because the first drill encounters depletion, the bore needs relocation, or throughput no longer meets a visible construction goal.

Initial tuning: boiler-engine consumes 0.1 L/s of water and one charcoal per 240 seconds while supplying up to 1,000 W shaft power. An adjacent alternator converts at 80% efficiency for up to 800 W electrical output. Crusher demand is 250 W for 4 seconds per ore; pump demand is 100 W at up to 1 L/s; drill demand is 400 W. Thus all three electrical loads total 750 W, fitting one starter source while leaving little expansion margin.

These values are gameplay units, not thermodynamic claims. Boiler output is controlled by load; fuel/water consumption scales with delivered shaft work, with prepaid remaining fuel energy retained when idle. No idle consumption is required in the starter model. The boiler stops safely on no water or fuel; it does not explode. Pump power competes normally with other consumers, and the tank buffers supply interruptions. The direct adjacent shaft connection is the first real mechanical port; shafts/gears for distributed layouts extend it later.

Acceptance: build the entire chain from gathered resources, start it with manual water, sustain it with the pump, fill its output, interrupt power, reload mid-process and deplete a drill target. Account for all resources and clearly communicate each stop. The reason to build a second machine is observable demand from construction or extraction, not a hidden unlock counter.

## 5. Ages and useful demand

| Capability | New decision | Useful output / reason to expand |
|---|---|---|
| Hand tools and metalworking | Where to gather and establish shelter/storage | Tools, building material, initial machine parts |
| Mechanical/steam industry | Place power sources, orient shafts, supply water/fuel | First powered extraction and improved ore yield |
| Electrical industry | Separate generation from consumers; organize distribution | Distributed drills, processors and convenient control |
| Advanced manufacturing | Coordinate several inputs and alternative recipes | Efficient components, large construction projects, aerospace parts |
| Aerospace | Establish a prepared destination and reliable supply route | Physical-world colonies and planet-specific manufacturing inputs |
| Advanced interplanetary industry | Balance routes, buffers and reusable infrastructure | Teleporters, large settlements and optional industrial megaprojects |

The starter alternator demonstrates electricity without replacing the steam age: early generation remains tied to the boiler and short-range mechanical setup. Mature electricity adds better generation, distribution and controlled production. Ages describe capabilities that overlap; no artificial requirement forces every factory to retain obsolete equipment.

Demand comes primarily from durable construction and expansion, not mandatory wear taxes or endless repair errands. Later manufacturing may offer faster or cheaper alternatives while preserving a viable starter route. Concrete aerospace recipes belong to their stage and must pass the same bootstrap/dependency review.

## 6. Realm rewards and progression boundary

Core physical-world industry, rockets and physical-world teleporters are achievable without a realm-exclusive consumable or mandatory realm visit. Realm exploration offers unique decorative materials, durable equipment/modules, unusual optional processes and discoveries. This permits early lucky access without making aerospace depend on an arbitrary ruin roll.

A realm module may be needed for each additional copy of an optional specialized machine, but it is recoverable on dismantling and does not decay during operation. An expedition therefore buys lasting capability. Ordinary factory throughput never continuously consumes realm-only fuel. Repeat visits seek new sites, additional durable modules and different discoveries, rather than maintaining the same operating machine.

Normal manual drops still occur within realms; their transfer restrictions are defined in [TRANSPORT.md](TRANSPORT.md). Local storage and hand crafting are permitted. Automatic drilling, pumping, harvesting and extraction are disabled in realms even with a nearby player; ordinary local processing may run only under player-proximity production permission. This makes the restriction readable and prevents an unattended player standing nearby from legitimizing a remote mining colony.

Gate restoration requires locally obtainable surface-side salvage or physically manufactured substitutes, never a component available only beyond the first closed Gate. Realm prizes do not unlock mandatory recipes through discovery flags. The recipe browser can explain a durable artifact's uses after discovery without imposing a research gate.

## 7. Economy validation and unresolved tuning

Validate every recipe graph from spawn resources: there must be an ingredient/station path to the first instance of each core capability. Test an unlucky but valid seed, exhausted local ore, full buffers and a lost starter tool. Intentional resource sources/sinks are registered explicitly so closed recipe loops cannot generate metal accidentally.

Measure time spent gathering versus building, amount of construction enabled by one ore trip, power/water interruptions and time between useful decisions. Recipe quantities, hardness, bore reach, fuel duration and deposit sizes can change together after tests. The finite-terrain model, manual bootstrap routes and durable realm-reward policy are the working decisions; final balance remains unvalidated.
