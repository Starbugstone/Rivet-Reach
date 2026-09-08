# Rivet Reach - Design Resolutions and Remaining Validation

> **Status:** working decision register, updated 2026-09-08. The user requested solutions to the conflicting/problematic points. The resolutions below are selected working specifications, not claims that the user personally chose each numeric default or that implementation/playtesting has validated them.

Related: [DEVELOPMENT_STRATEGY.md](DEVELOPMENT_STRATEGY.md), [PROJECT_PLAN.md](PROJECT_PLAN.md), [GAMEPLAY.md](GAMEPLAY.md), [SIMULATION.md](SIMULATION.md), [TRANSPORT.md](TRANSPORT.md), [ECONOMY.md](ECONOMY.md), [CONTENT_PIPELINE.md](CONTENT_PIPELINE.md), [DELIVERY.md](DELIVERY.md).

## 1. Decision status and ownership

- **Agreed direction:** explicit existing project intent, including playable real foundations, world-item stacks and sink-by-default water behaviour.
- **Working resolution:** a selected solution made under the user's request to resolve design gaps. Use it consistently until deliberately revised; keep its tuning/evidence needs visible.
- **Tuning:** numerical values that can change without changing the selected rule.
- **Open dependency:** missing external information or a later content/implementation choice. Do not fabricate an answer.
- **Validated:** a named experiment establishes behaviour for its stated workload. Nothing in this register is validated yet.

The authoritative specialist document owns the full rule. This register records conflict, selected outcome, rationale and remaining evidence. A change updates affected summaries together. `DEVELOPMENT_STRATEGY.md` alone owns implementation stage order; `PROJECT_PLAN.md` mirrors it.

## 2. Analysis of the pulled changes

The update through `cf8379a` added physical world-item stacks, compatible pile merging, sleeping movement, block-based water currents and the explicit content property `buoyant=true`. It kept inventory and pipe quantities separate from world physics and added questions S09-S11.

Those changes resolve the desired feel and representation direction; they did not yet resolve lifetime, merge contention, source-water semantics, dormant water boundaries or unattended Gate transport. The current resolution preserves every added core rule and fills those gaps. Ordinary mining/mob/manual drops remain physical stacks. Powered extraction reserves drops into machine storage as an explicit collection operation, preventing factories from needing a physics entity per transfer.

## 3. Simulation decisions

All decisions below were selected on 2026-09-08. Full details and acceptance cases are in [SIMULATION.md](SIMULATION.md).

| ID | Conflict / missing rule | Working resolution | Rationale and remaining evidence |
|---|---|---|---|
| S01 | Offline elapsed time could conflict with owner-online production | Universe time and eligible production time are separate; dormant/offline production freezes; single-player pause stops universe time | Preserve one set of game rules. Test pause, inventory UI, disconnect/reconnect and Gate cooldowns |
| S02 | One loaded component could implicitly wake an unlimited network | Eligible subgraphs only; dormant intermediate chunks disconnect supply/routes | Make quotas and stopped machines explainable. Test every endpoint/middle eligibility combination |
| S03 | Update ordering could change resource allocation and yield | Stable step order, next-step results, inventory escrow/reservations, priority plus fair allocation | Prevent duplication and starvation by accident. Test contention, proportional shortage, splitting stored fluids and cancellation |
| S04 | Independent elapsed-time arithmetic could invent coupled factory output | Reference simulation first; batch only to the next relevant event with equivalence evidence | Batching is an optimization, not different rules. Test shared batteries, outputs and controls across batch sizes |
| S05 | Async saves/transfers could restore partial state | Journaled operations, coherent checkpoints and one committed entity owner | Choose recoverable semantics now; test actual filesystem durability, replay and interruption later |
| S06 | Seed-only regeneration could change old geography | Persist/pin generator, definitions and content mapping; migrate explicitly with backup | Protect existing builds/anchors. Test old-world regeneration and incompatible content handling |
| S07 | “Responsive/scalable” had no certified hardware/workload | Retain measurable candidate budgets and scaling matrix; DELIVERY.md requires an actual available reference machine before certification | Hardware/team facts remain external inputs. No performance promise can be made from documentation |
| S08 | Infinite coordinates/height had no numeric/storage contract | Int64 authority, initial 32-cubed chunks, configured finite Y, checked supported X/Z envelope and local origin shifts | Avoid float/negative-coordinate errors. Validate seams, overflow, generation limits and origin shifts |
| S09 | Merging could reset despawn or duplicate contested piles | Local compatible merges, normal stack-size cap, oldest eligible lifetime, ordered pickup/merge/expiry; ordinary lifetime initially 20 active minutes | Prevent immortal piles and lost amounts. Death caches are durable containers. Tune radius/cadence/timer through load tests |
| S10 | General physics cost was unspecified | Start with swept custom voxel-item movement, sleeping and interpolated presentation | Share collision truth with edits; compare measured alternatives only if needed |
| S11 | Water forces/source rules could imply an expensive fluid solver | Scheduled source/level block flow; default sink, tagged buoyancy, capped current response; no automatic source multiplication | Retain readable channels and renewable industrial water. Test drainage loops, surfaces/bottoms and dormant boundaries |
| S12 | Terrain removal, collision and visuals could disagree | Atomic voxel/collision edits with versioned visual patches, queued meshes/light and revalidated local paths | No invisible collision lag or stale job publication. Test rapid edits while moving/streaming |

## 4. Transport decisions

Full rules are in [TRANSPORT.md](TRANSPORT.md), particularly sections 37-40.

| ID | Conflict / missing rule | Working resolution | Rationale and remaining evidence |
|---|---|---|---|
| T01 | Last healthy endpoint could be destroyed after departure | Protected cores/sanctuaries; established pair retains minimum return after optional modules are removed | Repair improves convenience without revoking safety. Test removal, sabotage and fallback after reload |
| T02 | Mob inventory and physical items could become unattended Gate cargo | Living entities require player accompaniment/following lease; player/companion inventories cross; loose piles remain at threshold | Preserve pursuit and escorted livestock while preventing unattended shipment. Test cargo companions, dropped stacks and water loops |
| T03 | Repeated mob traversal could keep realms active forever | Non-player crossings never extend following leases; bounded preparation queue; tickets carry separate capabilities | No hidden factory loading from safety tickets. Test sustained traffic and expired queues |
| T04 | Blocked/hazardous receiving geometry could kill or strand arrivals | Seeded reserved sanctuary rejects construction/fluid; safe arrival/cancellation with journaled transfer | No silent demolition of builds. Test occupied exits, invalid legacy data and pre/post-commit failure |
| T05 | Recovery timers could reset or require loaded Gate chunks | Persist next-ready universe tick; use defined pause semantics | No continuous unloaded simulation. Tune 1/10/15-second defaults after playtests |
| T06 | Spacing checks could depend on exploration order | One major anchor per jittered macro-region, balanced network assignment and terrain accommodation | Guaranteed minimum separation without order-dependent rejection. Test negative regions, boundaries and discovery time |
| T07 | Shared realm exits could bypass aerospace | One physical planet per realm instance/network identity; themes can repeat | Keep rockets necessary for reaching other physical worlds; revisit only through an explicit future design change |
| T08 | Invalid landing, full pad or disconnect could lose cargo | Prepare before launch; persistent flight owner; bounded retries/holding; owner-offline pause and reserved return capability | Preserve one manifest without distant physics. Test deleted pads and recovery after delivery commit |
| T09 | Safe landing offsets could accumulate into lateral travel | Persist canonical route X/Z and reserved footprints; returns reuse endpoints; no re-anchoring from repeated offsets | Movement to a genuinely different launch site must be physical. Test rebuilds, alternate destinations and repeated first-landings |

## 5. Gameplay and production decisions

| ID | Conflict / missing rule | Working resolution | Rationale and remaining evidence |
|---|---|---|---|
| G01 | Realm materials could make exploration a recurring factory chore | Durable optional artifacts/modules, recovered on dismantling; no mandatory realm operating consumables | Expeditions buy lasting capability. Tune yield and demand for new optional equipment in ECONOMY.md |
| G02 | Random Gate access could block core industry or require its own inaccessible repair part | Core aerospace/physical teleporters use physical-world supply; first-Gate repair uses surface-accessible salvage/substitutes | No circular access dependency; verify core and optional recipe graphs |
| G03 | Named ages lacked concrete progression/demand | Manual bootstrap table, steam-to-electric starter chain and finite drill extraction | Test the complete gather/build/automate/expand loop from spawn, not a supplied chest |
| G04 | Recipe discovery could become a hidden lock | Discovered view with pinned prerequisites and optional artifact silhouettes; selectable full ordinary recipe view | Same registry/validation in both modes; observe unaided first-session progression |
| G05 | Sparse Gates and preserved geography could mean aimless travel | Guaranteed regional anchors plus progressively local clues, early personal waypoints and durable recovery | Test unlucky-seed discovery/return burden; adjust density/clues before erasing geography |
| G06 | Survival consequences were undefined | Health/food healing, no baseline hunger/raids/tool wear; durable death caches and safe shelter respawn | Consequence without cargo teleportation or permanent loss from a random return fault. Tune combat through play |
| G07 | Controls/accessibility/platform claims were ambiguous | Windows keyboard/mouse first, rebinding/UI scaling/non-colour feedback; other controls/platforms explicit later tracks | Test actual supported inputs/builds; do not infer support from export options |
| G08 | Advanced output lacked a purpose | Durable settlements, multi-world industry and optional projects/collections; no forced narrative ending | Supply concrete advanced construction goals before declaring full-game completion |
| G09 | Living settlements could expand into a simulation project of their own | Small local behaviours/trade catalogue, explicit restock sources, farming for healing/plant materials | Deliver understandable inhabitants without mandatory quest/lore gates; tune economy/AI content later |
| G10 | Shared bases conflicted with personal owner-online loaders | Shared permissions separate from one accountable loader owner; explicit consent/quota-checked transfer | Another member's login never silently wakes absent owners' industry |
| G11 | Automated extraction itself was missing | Drill/quarry remove real finite blocks with atomic output reservation and eligible target chunks | No hidden duplicate deposit reserve; test depletion and manual/miner contention |
| G12 | “Responsive controls” lacked operation rules | Concrete reach/movement/mining/placement, partial pickup, footprints and dismantle recovery | Test against authoritative terrain during mesh/light updates; numerical feel values remain tunable |
| P01 | PROJECT_PLAN and DEVELOPMENT_STRATEGY used different orders | One Stage 0-5 roadmap; crafting and industrial play precede lifecycle/multi-world expansion | Review both roadmaps together when changing order; do not reintroduce a separate architecture-only project |
| P02 | Installed art tools were mistaken for an asset pipeline | Grid/pivot/port conventions, explicit export/import, stable identities and binary-source policy | Round-trip one kit and a fresh checkout before broad asset production |
| P03 | “Complete game” lacked a delivery boundary | Explicit staged preview/full-game scope, initial platform/content floor, cooperative verification and recovery criteria | Exact staffing, hardware and dates remain external; distinguish planned scope from validated delivery |

Full detail: [GAMEPLAY.md](GAMEPLAY.md), [ECONOMY.md](ECONOMY.md), [CONTENT_PIPELINE.md](CONTENT_PIPELINE.md), [DELIVERY.md](DELIVERY.md).

## 6. Alternatives deliberately not selected

- Hidden extraction reserves alongside mineable ore blocks: would require duplicate depletion accounting and weaken terrain excavation. The baseline extracts real terrain.
- A factory of physical items inside pipes: would unnecessarily couple ordinary logistics throughput to world physics. Physical channels remain supported separately.
- Automatic source-water creation from adjacent source cells: complicates source/drain predictability. Renewable pumping uses explicit sources instead.
- Arbitrary offline catch-up: contradicts owner-online eligibility. Only eligible work can batch.
- Infinite realm-loader refresh through creatures: conflicts with personal exploration. Player accompaniment and capability-specific tickets bound it.
- Emergency arbitrary-coordinate teleportation as the main Gate safety rule: would undermine geography. Protected paired sanctuaries preserve return at the established anchors.
- Mandatory realm fuel for every advanced machine: risks repeat errands. Durable optional capability preserves exploration value without maintaining core throughput manually.
- Defining a deadline or certifying hardware from document length: lacks evidence. Record the external inputs and measure actual increments.

These alternatives can be revisited with a concrete reason and affected-rule review; they are not simultaneously active policies.

## 7. Remaining open dependencies and tuning

**External facts:** actual reference machine, available team time, funding/distribution accounts, supported remote binary storage and a chosen exact Unity Editor installation. The documented target remains Unity 6.3 LTS; the discovered local installation was 6000.4.4f1. Project creation must select/pin a compatible version explicitly rather than silently converting that mismatch into a decision. No integration installation or game scaffolding is authorized by this documentation pass.

**Numerical tuning:** movement/reach, tool hardness, recipe costs, fuel/pump/drill rates, pile lifetime/radius/cadence, water speeds, Gate spacing/cooldowns, ticket concurrency and render/memory budgets. The specifications provide starting values so experiments are concrete; evidence may change them.

**Later content/implementation:** final art palette, final recipe layouts, planets/biomes beyond the release floor, creature/trade catalogues, surface vehicles, deep mechanical gearing, advanced voltage/control tiers, satellite coverage, binary save packing and optimized pathfinding/meshing. Select these when their stage needs them.

**Intentionally mysterious:** Gatebuilder origin/disappearance and ultimate realm cosmology. Observable repair, traversal and reward rules are precise without answering those lore questions.

## 8. Next implementation-readiness review

Remain documentation-only until implementation is requested. Before that request is executed, confirm the exact Editor/project location and reference machine, then use Stage 0's real interaction/coordinate model. Add Stage 1's supplied bootstrap specification and persist its real state. Grow into the complete starter factory, stress the same systems, then add the second world.

Every working decision now has a concrete behaviour and acceptance case. Only measurements can establish performance, feel and final balance; this register must not mark them validated on the strength of a Markdown review.
