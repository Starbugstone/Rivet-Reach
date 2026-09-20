# Bulk crates and crate controllers

The user authorized bulk crates and a physical crate-controller warehouse as the next issue #10 increment. This specification records the working contract. [Verification](verification/CRATE_RESULTS.md) separates measured checks from remaining limits.

## Bulk crate

A **Bulk Crate** is a one-block, single-item storage endpoint. It holds up to **16,384 ordinary items** of one type. Inserting the first item assigns its type; it continues to accept only that type until emptied. The crate interface can also lock an empty crate to the item held on the cursor or selected in the hotbar. A locked empty crate stays assigned until unlocked.

Crates accept ordinary items, including unstackable equipment; withdrawals obey each item’s normal stack limit. Filled batteries and tanks carry distinct stored contents and are rejected with a visible instruction to use a Chest. Empty portable-storage items remain ordinary items and may be stored normally.

A nonempty crate cannot be mined. The player must empty it first; this prevents a large count from becoming an unsafe field of loose items. An empty crate mines and recovers normally. The crate's timber/iron construction, front item label and interface are original project assets.

The 3×3 Workbench recipe surrounds one Chest with eight Planks. It produces one Bulk Crate.

## Crate controller and warehouse topology

A **Crate Controller** has no independent inventory. It exposes the physical contents of up to **64 face-connected, resident Bulk Crates** as one paged interface. One connected bank may contain at most one controller. A second controller, more than 64 connected crates, no crate connection, or unavailable terrain makes that controller aggregate unavailable; individual crates remain usable directly.

Controller insertion deterministically fills already assigned matching crates before unlocked empty crates. It never moves or duplicates contents merely because the bank changes. Removing a controller leaves every crate and its stored contents intact.

Crate residency invalidation uses reference-counted chunk dependencies for registered crates/controllers and their face neighbors. Unrelated terrain pages do not invalidate warehouse membership or industry topology. A relevant unload/reload invalidates the cached aggregate and industry graph after the whole unload batch is unavailable; membership continues to include only resident physical stores. Save restoration rebuilds these dependencies from the physical station records. Physical warehouse cells and all connected inventory endpoints belong to the same conservative automation dependency component, so direct-crate and controller aliases cannot continue spending a shared store through a suspended route. Reconstruction pauses that component, preserves unrelated factories and publishes all replacement channel graphs together. It adds no automatic residency or catch-up production.

The 4×4 Machinist's Bench recipe consumes one Bulk Crate, one Machine Casing, two Gold Ingots, two Iron Ingots, two Item Pipes and one Iron Cog. Gold and the industrial components place the controller after the basic storage/bootstrap chain, while its consumed crate keeps the warehouse physically grounded.

## Pipes and priorities

Item Pipes may connect to a direct crate or a controller on any face. A physical crate remains one source even when reachable both directly and through its controller: it cannot send twice in the same transfer phase, and a controller never inserts an item back into its originating crate.

For compatible pipe delivery, each receiver has an editable, persisted priority from **0 through 100**. Defaults are Machine/Furnace **50**, Crate Controller **40**, direct Bulk Crate **30**, and Chest **20**. Higher eligible priorities receive first; equal-priority receivers share turns round-robin. Existing machine recipe/input compatibility still filters candidates, but does not override round-robin among equal priorities. A full, mismatched, locked-to-another-type, metadata-bearing, invalid or unloaded crate rejects the transfer without consuming the source item. Unloaded crates cannot join two resident portions of a controller bank. See the [pipe-routing guide](wiki/Item-pipe-routing.md).

## Save compatibility

Schema **15** adds compact crate state for the assigned item, count, lock state and persisted receiver priority. Controllers store their own routing priority but no duplicate aggregate contents; membership is rebuilt from current resident topology. Schema 14 and earlier saves retain their exact layout and load with no crates. Save validation bounds counts at 16,384, requires a registered ordinary item for nonempty/locked records, and preserves the existing whole-session failed-load rollback.

No terrain generation changes are introduced. Crates, controllers and their recipes are content-fingerprint additions; recognized schema-14 content remains accepted only through the explicit crate projection. [Industry transport](INDUSTRY.md#bulk-crates-and-physical-warehouses--2026-09-19), [economy progression](ECONOMY.md#bulk-crate-progression--2026-09-19) and [the player guide](wiki/Crates-and-warehouses.md) cover their respective boundaries.
