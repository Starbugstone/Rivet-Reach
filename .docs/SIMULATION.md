# Rivet Reach - Simulation and Persistence Contracts

> **Status:** proposed specifications for brainstorming; no implementation or benchmark evidence yet.
>
> Existing agreed constraints come from [PROJECT_PLAN.md](PROJECT_PLAN.md). The contracts below are proposals to make those constraints testable. Open choices are tracked in [DESIGN_QUESTIONS.md](DESIGN_QUESTIONS.md).

## 1. Reference simulation and time

**Agreed direction:** machines and logical networks can operate without rendering; dormant chunks have essentially no continuous simulation cost; player-owned factory tickets require an online owner.

**Proposed contract:** use a fixed-step authoritative industrial simulation with a stable command order. Rendering can update independently. A provisional starting rate is 20 simulation steps per second; benchmark and tune it before adopting it. This is not a requirement that movement, physics and industrial machines all share one update rate.

Distinguish three clocks:

- simulation time: authoritative ordering and industrial process progress;
- eligible production time: intervals during which a machine is allowed to simulate;
- real elapsed time: useful for diagnostics, but not automatic production credit.

A machine advances once per eligible interval, regardless of overlapping tickets. Active and background modes produce the same industrial result for the same inputs and command sequence. Presentation, distant AI and unrelated physics can differ.

Proposed dormant policy: freeze production. Reconnecting does not grant production for owner-offline or otherwise ineligible time. Another player's proximity can still activate the area through a valid independent ticket. Sleeping *eligible* work may be batched only if its result remains equivalent to the reference simulation.

Open: single-player pause behaviour, dedicated-server pause with no players, and the clock used by Gate recovery. These must be explicit; do not substitute operating-system time for simulation time implicitly.

## 2. Tick ordering and resource accounting

**Proposed reference order**, subject to experiments:

1. Apply validated commands and ticket changes at a step boundary.
2. Apply topology mutations and identify simulation-ready components.
3. Evaluate scheduled controls using defined prior-step observations.
4. Allocate power and reserve legal item/fluid transfers using stable ordering.
5. Advance eligible processors against available resources and output capacity.
6. Commit transfers/process results and emit presentation/persistence deltas.

Define whether newly produced material is available this step or the next; the initial proposal is next-step availability. Commands with competing effects must have a stable tie-break, not depend on dictionary iteration or job completion order.

Track inputs, outputs, in-process contents, inventory reservations and transit buffers. Recipes can intentionally transform quantities, but every change must be attributable to a recipe, source, sink or authorized action. A full output must not silently discard material. A failed reservation must not remove its input.

Open: power shortage policy, priority fairness, fluid mixing, pipe transit time and machine interruption/refund rules. Start with the smallest explicit behaviour; do not hide these choices inside scheduling code.

Signal loops need bounded propagation. Proposed starting semantics give changes a defined step delay and retain pending events for later steps. Oscillators may remain valid gameplay, but cannot create an unbounded same-step evaluation loop.

## 3. Networks spanning chunk states

**Problem:** a generator, pipe route and consumer can occupy different eligibility regions. Loading one machine must not silently wake an unlimited connected factory or erase the unloaded part of its network.

**Proposed starting policy:** simulate eligible portions only. Connections through dormant portions stop at explicit boundaries; they do not teleport resources across missing topology. Preserve boundary state, and show the resulting blockage in diagnostics. Each network type must define how its eligible subgraphs are derived, including whether stored power/fluid remains local or partitioned.

Alternatives to investigate: retaining lightweight topology through dormant terrain, or explicit bounded network tickets. Neither alternative should bypass owner quotas or realm restrictions.

Required cases:

- generator active, consumer dormant, then reversed;
- intermediate pipe chunk dormant between two active endpoints;
- one chunk promoted/demoted while transfers are reserved;
- two owners' tickets overlap and one disconnects;
- network split/merge changes inventory or fluid capacity;
- a player edits a network during a pending topology rebuild.

Proposed topology handling: version affected components and apply rebuild results only to the version they were computed from. Pause affected transfers when necessary; unrelated components continue. Schedule rebuilds within a work budget and expose rebuilding state. A single pipe edit can affect a very large component, so “rebuild only the affected network” alone is not a latency guarantee.

## 4. Safe batching and catch-up

Elapsed-time arithmetic is an optimization, not the definition of production.

A batch is eligible only when it accounts for all relevant constraints: inputs, output space, shared power, fluids, controls, competing consumers, recipe transitions and topology/ticket events. Stop at the next event that could change the result.

Compare optimized results against the reference simulation across different batch sizes. Use exact accounting for discrete items; explicitly choose rounding and tolerances for energy/fluid representations. If equivalence cannot be established, use scheduled reference steps within a supported workload instead.

Never independently fast-forward two processors that compete for the same limited input or battery. Never credit wall-clock offline time merely because a saved timestamp is old.

## 5. Coordinates and generation compatibility

**Agreed direction:** authoritative locations include world identity and integer/chunk coordinates; rendering uses local floating-point space.

**Proposed specification needs:** select coordinate widths, supported bounds, negative-coordinate floor division, local-coordinate ranges and overflow behaviour before fixing the save format. Test chunk seams and origin shifts with entities, targeting and pending jobs. “Effectively infinite” means streaming within supported bounds, not unbounded numeric precision.

Persist universe seed, generator version, world-definition version and stable content identities. A seed alone does not preserve terrain when generation algorithms change.

Proposed compatibility policy: pin existing worlds to their generator and definitions until an explicit migration is available. Untouched terrain regenerates under that version. If a required version/content definition is unavailable, report incompatibility clearly rather than silently generating replacement terrain or deleting blocks.

Gate-anchor placement must use bounded, deterministic neighbour queries and stable tie-breaking. Minimum-distance rejection, terrain accommodation and structure footprints must work across macro-region/chunk boundaries without depending on exploration order. Test negative regions and competing candidates. The final distribution algorithm remains open.

## 6. Durable state and recovery

Define a coherent saved revision across chunks, block entities, inventories, tickets where applicable, world metadata and player/entity locations. Asynchronous saving must capture a consistent snapshot rather than mixing unrelated revisions.

**Proposed transfer invariant:** after recovery, a transported entity and its inventory exist at exactly one authoritative location. A durable transfer record with a unique operation ID is one candidate mechanism; the storage implementation remains open.

For a transfer, prepare and reserve the destination, record the recoverable operation, commit ownership/location once, then publish deltas and release obsolete reservations. Retrying an operation must not repeat its resource effects. Recovery must resolve interrupted operations consistently, including when source and destination are stored separately.

Apply equivalent accounting to cargo dispatch, delivery, machine completion and inventory movement. Cargo that cannot unload needs an explicit waiting/return/failure policy; it cannot disappear or generate a second delivery.

Future recovery exercises: interrupted save, interrupted transfer, blocked destination, missing region data, disk-write failure and unsupported schema version. Preserve the last valid save where possible and report recovery outcomes. Choose backups, journal/snapshot design and migration policy before public persistent worlds depend on them.

## 7. Responsiveness and workload budgets

These are **candidate targets**, not measured promises or approved minimum requirements.

| Measure | Initial candidate | Evidence required |
|---|---|---|
| Rendered play | 60 FPS at 1080p; p95 frame time at or below 16.7 ms | Record hardware, quality, view distance and representative traversal/factory workload |
| Visible local interaction feedback | Next rendered frame where practical; p95 below 100 ms | Measure input-to-feedback separately from authoritative completion |
| Industrial simulation | 20 Hz reference; p95 work below 10 ms per step at declared workload | Include signals, transfers, power and topology queues; measure total frame contention too |
| Streaming/topology/save disturbance | No repeated stalls over 50 ms during normal interaction | Record p99/max stalls and queue age, not only average FPS |
| Memory residency | Bounded after repeated travel/unload cycles | Set a concrete RAM budget once a reference machine and view height are selected |
| Transfer readiness | Safe arrival; progress/cancellation behaviour during long waits | Measure cold destination generation separately from warm travel |

Select an actual reference CPU, GPU, RAM, storage device, operating system and build configuration before treating these as pass/fail gates. No hardware purchasing decision is required during brainstorming.

Proposed reproducible workload matrix:

- small functional factory, including a full output and power starvation;
- 100, 1,000 and 10,000 active simple processors as exploratory scaling tiers, not promised supported counts;
- long sparse versus densely connected networks, including repeated split/merge edits;
- dormant factories versus equal-sized active/background factories;
- streaming while production runs, origin shifts and a long travel/unload soak;
- cold cross-world transfer, multiple following entities and repeated ticket refresh attempts;
- save/load and recovery during production and transfer.

Record seed, workload configuration, resident/eligible chunk counts, network nodes/edges, active transfers, frame distributions, simulation time, queue age, memory and save size. Dormant CPU cost can be near zero while persisted metadata and storage still grow; measure those separately.

A benchmark passes only for its stated workload. When overloaded, bounded queues and visible waiting should preserve correctness; silently dropping simulation steps, duplicating transfers or freezing input is not an acceptable optimization.
