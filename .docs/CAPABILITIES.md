# Capability interfaces

Working implementation for the pre-0.1.0 review, authorized 2026-09-20. C# objects can implement several interfaces. Use small interfaces to describe supported operations or item capabilities; compose them without a subclass for every combination.

## Items and recipe categories

`ItemRegistry.Capability<T>(id)` returns a cached, immutable capability or null. `IEdible` exposes food points; `IBurnable`, `IBoilerFuel`, `ICompostable` and the ingredient/material interfaces identify independent capabilities. `IBoilerFuel` also implements `IBurnable`. An item can expose edible, vegetable and compostable capabilities together. Do not make every item implement every optional interface and then depend on a boolean to discover whether it really supports it.

The registry builds the capability tables transactionally when content loads. There are no capability objects per stack, interface discovery by reflection, or string-array scans in game ticks. Invalidate the registry after authoring changes. Food, fuel validation, boiler acceptance and compost validation use typed interfaces. Fuel energy and compost quantities retain their existing authoritative catalogs; an interface does not invent a default quantity.

Existing serialized `tags` and `#category` recipe/search syntax are compatibility inputs. Changing their spelling or removing their serialized definitions would change historical content fingerprints. They compile into cached membership tables; gameplay behavior must use typed contracts. New gameplay capabilities need an explicit interface and a registry mapping. Arbitrary data-only search/recipe labels remain supported, including injected test catalogs, but must not become a second gameplay type system.

`IItemSelector` is the immutable recipe membership contract. `ItemRegistry.Select<IVegetable>()` and `Select("#vegetable")` return the same compiled selector. The 25 shipped labels resolve through their interface tables, so recipe membership cannot drift from typed capability discovery. Recipe compilation resolves selectors once; matching and staging use the existing bounded matcher and conserve exact ingredient quantities. This migration does not change recipes, stack IDs, saved payloads or current/legacy content fingerprints.

Inheritance is explicit: the same boiler-fuel component is registered under both `IBoilerFuel` and `IBurnable`, and its serialized definition must include both existing labels. Other category combinations are independent; for example, `IRawMeat` does not silently add `IMeat` or `IEdible`. Preserve the authored memberships rather than inferring extra recipe alternatives. Query registered capability interfaces, not implementation classes or the common `IItemCapability` marker.

| Serialized category | Runtime interface |
| --- | --- |
| `edible` | `IEdible` |
| `burnable`, `boiler_fuel` | `IBurnable`, `IBoilerFuel` |
| `vegetable`, `fruit`, `grain`, `mushroom` | `IVegetable`, `IFruit`, `IGrain`, `IMushroom` |
| `prepared_food` | `IPreparedFood` |
| `meat`, `raw_meat`, `fish`, `raw_fish`, `egg`, `raw_egg` | `IMeat`, `IRawMeat`, `IFish`, `IRawFish`, `IEgg`, `IRawEgg` |
| `seed`, `fibre`, `cordage`, `fabric`, `feather` | `ISeed`, `IFibre`, `ICordage`, `IFabric`, `IFeather` |
| `raw_ore`, `ingot`, `log`, `planks` | `IRawOre`, `IIngot`, `ILogMaterial`, `IPlankMaterial` |
| `fishing_rod`, `compostable` | `IFishingRod`, `ICompostable` |

Registry invalidation replaces its complete capability/selector snapshot on the next lookup. Already compiled recipe catalogs retain their immutable snapshot and must be recompiled as part of content reload; mutating authoring definitions is not a supported way to change live recipes mid-tick. Failed compilation publishes no partial index. Independent injected registries cannot alter the live registry's cached components.

## Item transport

`IItemPipeInventory` owns live reads, extraction, input requests and insertion. `QueryInput` returns `Reject`, `Accept` or `Prefer` for an item and local face. It declares interest, not a reservation: insertion must recheck live capacity and restrictions. `IItemPipeStackReceiver` is an optional exact-payload insertion capability. Recipe consumers do not accept instance payloads merely because a transport pipe is connected.

The shared allocator visits configured priority tiers first. Within a tier, receivers take round-robin turns and choose preferred cargo before other accepted cargo. Preference selects **cargo within a receiver's turn**; it does not grant a receiver an extra turn. Thus existing chest contents and processor output affinity coexist with equal sharing between destinations. Source snapshots, physical storage identities, alias exclusion and one-unit source budgets cover every graph and tier. Transfers cannot steal protected input/fuel slots, circulate through a warehouse alias, or forward newly arrived cargo in the same phase.

## Network lifecycle

Budgeted topology construction suspends the affected automation components while unrelated components continue their ordinary ticks. Retain completed graphs until their replacement publishes atomically. Suspended machines clear transient delivered/received generation and battery flow while retaining their last completed demand for diagnostics; dormant machines also clear derived demand and connection state. `IndustrySimulation.IsSimulating` supplies the same activity boundary to presentation, so paused components cannot keep spinning or illuminating through stale running flags. Keep demand, actual flow, residency and topology validity distinct; presentation must not manufacture demand from a temporarily incomplete graph. [Industry network lifecycle](INDUSTRY.md) owns the reconstruction budget and publication rules.

## Verification

The release review includes interface contract, routing fairness, source-order preference, all-face fuel, metadata conservation, topology rebuild, capability allocation, recipe and historical-save checks. `TagReviewChecks` checks every shipped category against its serialized membership, typed/authored selector identity, missing and inherited capabilities, failed compilation recovery, invalidation, registry isolation and zero managed allocation after warm-up. Allocation assertions first calibrate the managed byte counter with a retained 4096-byte array; if unavailable, they calibrate the Editor GC.Alloc event recorder against both positive and empty controls. Unsupported counters produce explicit `UNVERIFIED` results and cannot establish zero allocation. These checks must run in the pinned Unity runtime; their presence is not a substitute for recording their results. Native large-factory and weather results belong in the release review evidence, including build identity and remaining limitations. An interface alone is not evidence of speed or correctness.
