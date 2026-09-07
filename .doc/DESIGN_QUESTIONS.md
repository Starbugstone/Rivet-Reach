# Rivet Reach - Brainstorming Questions and Decision Register

> **Status:** open working register. Questions and candidate answers below are not approved decisions. The project is documentation/specification only.

## 1. Decision workflow

- **Agreed direction:** existing explicit project intent; change deliberately and update its authoritative document.
- **Proposal:** a concrete candidate that can be discussed or tested; not permission to implement it.
- **Open question:** alternatives or consequences still need resolution.
- **Validated:** use only when a named experiment or playtest supplies evidence within a stated scope.

When resolving a question, record the chosen behaviour, rationale, affected document, date and remaining validation. Keep rejected alternatives only when they explain a meaningful tradeoff. Nothing in this register is validated yet.

Priority means when an answer becomes necessary, not a demand to answer everything now. Technical questions should be resolved before the relevant prototype; content mysteries can remain open much longer.

## 2. Before the first simulation prototype

| ID | Question | Candidate direction / alternatives | Why it matters and needed evidence |
|---|---|---|---|
| S01 | When does production advance, including pause and owner disconnect? | Eligible simulation time only; dormant/offline time freezes | Prevent contradictory background results; compare active/background/reconnect scenarios |
| S02 | What happens when a logical network crosses dormant chunks? | Initially stop at eligible boundaries; compare lightweight retained topology | Avoid invisible quota bypass and missing resource accounting; test split eligibility |
| S03 | What are tick ordering, shortage and competing-consumer rules? | Stable ordering, next-step outputs, explicit reservations; fairness policy open | Reproduce coupled factories without update-order accidents |
| S04 | Which processes can be batched safely? | Require equivalence to a reference simulation | Test shared buffers, power starvation, controls and different batch sizes |
| S05 | How are saves and world transfers committed consistently? | Coherent snapshots and recoverable operation IDs; storage mechanism open | Interrupt saves/transfers and verify no duplicate or lost inventory/entity |
| S06 | How are old generated worlds preserved after updates? | Pin generator/definition versions; explicit migration when needed | Regenerate untouched terrain and anchors under an older save version |
| S07 | What hardware/workload defines acceptable performance? | Candidate budgets in SIMULATION.md; actual reference machine undecided | Measure frame distributions, memory, queue age and scale limits |
| S08 | What coordinate and vertical bounds are supported? | Integer world/chunk coordinates; width and height policy open | Test negative seams, far locations, overflow and origin shifts |

Authoritative proposals: [SIMULATION.md](SIMULATION.md).

## 3. Before the first Gate/transport prototype

| ID | Question | Candidate direction / alternatives | Why it matters and needed evidence |
|---|---|---|---|
| T01 | What preserves return if the last healthy endpoint is destroyed? | Protected minimum return, protected core, or emergency recovery | Resolve safety versus destructibility and multiplayer sabotage |
| T02 | How can creatures cross without enabling automated realm logistics? | Companion window or player proximity; inventory/dropped-item rules open | Test livestock, hostile pursuit, cargo creatures and automated loops |
| T03 | What limits portal tickets and repeated crossings? | Bounded preparations, entity re-entry guard and restricted non-player refresh | Prove traffic cannot keep unlimited realm factories running |
| T04 | How does arrival handle blocked or occupied space? | Reserved exit volume versus safe local fallback | Preserve player builds and prevent arrival deaths/duplicates |
| T05 | Which clock drives damaged-side recovery? | Simulation clock or explicit unloaded-time recovery policy | Save, pause, unload and reload without surprising cooldown resets |
| T06 | How do Gate anchors enforce spacing without generation-order dependence? | Bounded regional candidates with stable neighbour tie-breaks | Test boundaries, negative regions, rejection voids and terrain accommodation |
| T07 | Can one realm link several physical planets? | Separate instances initially versus constrained shared networks | Prevent accidental aerospace bypass; retain later design freedom |
| T08 | What happens when a rocket cannot land or unload? | Bounded preparation, holding/retry or safe return | Handle full/destroyed pads, invalid terrain and owner disconnect |
| T09 | What coordinate tolerance applies to first landings and established pads? | Small bounded adjustment; exact rule undecided | Prevent cumulative lateral shortcuts while allowing safe landings |

Authoritative proposals: [TRANSPORT.md](TRANSPORT.md).

## 4. Before committing to progression/content production

| ID | Question | Candidate direction / alternatives | Why it matters and needed evidence |
|---|---|---|---|
| G01 | Are realm resources required for ongoing production or primarily durable advances? | Prefer reusable/durable rewards; recurring consumables remain an option | Compare expedition yield and consumption to prevent repetitive errands |
| G02 | Must core aerospace/endgame progression enter a realm? | Required exploration versus optional powerful discoveries | Clarify the connection between both pillars and repair dependencies |
| G03 | How does each technology age change construction decisions? | Define one meaningful capability/layout change per age | Prototype progression benefits before a large recipe library |
| G04 | How does a new player find the next useful recipe without spoilers? | Prerequisite hints, silhouettes or a spoiler preference | Observe unaided first-session progress; retain knowledgeable beelines |
| G05 | How sparse can Gates be while remaining discoverable? | Tune density together with regional clues and travel options | Measure unlucky-seed discovery time and return burden |
| G06 | What are survival, death, recovery and factory-damage rules? | No preferred answer yet; avoid assumed hunger/raid mechanics | Establish consequence and recovery without accidental soft locks |
| G07 | Which movement, input and accessibility options belong to release scope? | Responsive FPS baseline; controller/platform scope undecided | Define audience/platforms and test essential actions/readability |
| G08 | What makes late play satisfying once logistics is largely solved? | Optional megaprojects, mastery or continued discovery | Test motivation without forcing a scripted ending |
| G09 | How deep are villagers, trading, farming and ecology? | Readable reusable behaviours before broad content | Bound complete-game scope while preserving a living world |
| G10 | How do cooperative ownership and permissions work? | Individual owner-online rule is baseline; shared ownership open | Resolve shared factories, absent owners, claims and transport access |

Player-experience proposals: [GAMEPLAY.md](GAMEPLAY.md). Existing vision: [PROJECT_PLAN.md](PROJECT_PLAN.md).

## 5. Intentionally later or mysterious

Final Gatebuilder name/origin/disappearance, ultimate realm nature, final planet/biome/mob catalogue, exact art palette, satellite coverage and late teleporter balance can remain open. Resolve them earlier only if a concrete dependency requires it. Mystery in the fiction must not make action feedback or save behaviour unpredictable.

## 6. Suggested next brainstorming sequence

1. Agree production time and chunk-boundary behaviour (S01-S03).
2. Choose realm reward expectations and return/companion rules (G01-G02, T01-T03).
3. Describe one first-session loop and one complete progression path (G03-G04).
4. Choose reference workloads and refine candidate performance budgets (S07).
5. Review the complete-game coverage table and deliberately include/defer unresolved systems.

This sequence is a proposal for discussion, not authorization to begin implementation. Continue recording useful detail without requiring every open question to be answered in one session.
