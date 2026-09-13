# Floater Rock bridges and chunk loaders

Working implementation selected by the user on 2026-09-13. Floater Rocks contain the magic that keeps Floaters airborne; their harvested stones enable remote industrial connections and chunk loading. Numerical recipe defaults and artwork are working choices for play review.

## Crafting

All four recipes are shapeless at the 4×4 Machinist’s Bench. Existing beginning recipes are unchanged.

| Result | Ingredients | Yield |
|---|---|---|
| Item Bridge | 2 Floater Rocks, 2 Machine Casings, 4 Item Pipes | 2 |
| Liquid Bridge | 2 Floater Rocks, 2 Machine Casings, 4 Fluid Pipes | 2 |
| Power Bridge | 2 Floater Rocks, 2 Machine Casings, 4 Power Cables | 2 |
| Chunk Loader | 4 Floater Rocks, 1 Machine Casing, 4 Azure Crystals | 1 |

## Names, ownership and pairing

Use or Interact opens a bridge. Enter a network name and choose Apply Name; configure a second bridge of the same kind with the same name to pair them. Names ignore case and trim leading/trailing spaces, allow up to 32 characters, and reject control characters/markup. Empty names are unlinked. A third bridge is rejected without modifying the existing pair. Unlink or rename an endpoint to release its old membership; mining does the same. Replacing a mined bridge requires configuring it again.

The key is **owning world + persistent owner identity + channel + normalized name**. The visible name is never an identity or permission credential. The current single-player expedition has a saved owner ID; placement assigns that owner and configuration validates the supplied actor against it. Future multiplayer must supply the authenticated player identity in server-side commands. A second owner's identically named network cannot acquire these endpoints. This supplies an authority boundary, not an implemented multiplayer connection or land-protection system.

The same owner may use “Quarry” for the item, liquid and electrical pair without connecting the channels. Each pair has exactly two endpoints, with no distance cap inside the current surface world. Multi-world travel and shared-team access are not added.

## Network behavior

Matching pipes/cables connect to all six bridge faces. Both sides of a bridge route the matching channel, joining local runs and the paired remote endpoint into the existing logical graph. Pipe ends at ordinary machines keep their independent wrench Input → Output → No connection rules; a pipe-to-bridge connection is a route rather than an inventory endpoint. Blue Signal never teleports through an item, liquid or power bridge, including signal fittings on connected transport pipes.

Bridges store and generate no cargo, liquid or electricity. The shared allocation services retain their source budgets, snapshot ordering, compatible-liquid checks, battery identities and fair distribution. Connecting opposite sides of an already connected graph adds no duplicate source budget. Breaking or renaming a bridge invalidates topology; while rebuilding the existing production gate applies.

A pair only carries resources while **both endpoints are resident**. A named but sleeping partner remains paired in the save and UI, with a waiting status. Bridges do not implicitly load the destination. Remote factories need loader coverage for their machines, stations, pipe/cable paths and any world extraction targets they use. Ordinary dormant boundaries remain closed.

## Chunk loaders

An enabled loader keeps its own **32×32×32 chunk** resident and eligible for the existing simulation at any player distance. It needs no fuel or electricity, avoiding a bootstrapping cycle in which an unloaded power source must first power its loader. It defaults enabled, can be toggled through its interface and returns its ordinary item when mined.

The loader registry supplies deduplicated chunk tickets to world demand. Multiple enabled loaders in one chunk retain one ticket; disabling/mining the last releases it when normal player demand no longer includes the chunk. Tickets are derived from persistent machine records, so a saved remote loader restores its chunk without requiring a player visit. Neighboring chunks are not implicitly loaded. Crops/furnaces/industry use their existing resident-world authorities; ticketed chunks also enter the terrain growth set. Spawn distance rules are unchanged; loading chunks does not grant remote hostile-mob spawning or Floater farming.

Only the open world's normal eligible simulation advances. Pause/death and closing the game stop production; there is no offline catch-up. Existing bounded terrain workers and topology rebuild budgets remain. The first version retains ordinary resident chunk meshes; remote machine views remain distance culled. Arbitrary loader-count performance is not established.

## Player feedback and art

Bridge panels show network name, owner, local coordinates and linked partner coordinates or waiting/unlinked status. Nearby floating labels repeat type, name and status; green denotes a loaded partner and amber a waiting/unlinked bridge. Loader labels and controls show active/disabled state and exact chunk origin/coverage. Text conveys status independently of color. Each bridge has an original suspended rocky core in the workshop's iron/brass material family; square item mouths, cyan liquid unions and copper electrical markings distinguish channels. The loader has enclosing field rings. Matching held/dropped art and inventory icons use the same meshes.

## Persistence and verification

Schema 9 adds the single-player owner identity and per-machine owner/name/loader settings. Missing pre-schema-9 fields default safely; earlier saves contain none of these new item identities. Compatibility fingerprints exclude only the explicitly additive items/recipes for historical content variants, retaining checks on existing definitions, processing, recipes and mobs. Runtime graph edges and residency tickets rebuild from validated saved machine records; duplicate third endpoints, invalid names or malformed owner IDs reject restoration under the existing failed-load rollback.

[Bridge verification](verification/BRIDGE_RESULTS.md) records measured evidence and limits. [The player guide](wiki/Bridges-and-chunk-loaders.md) owns setup instructions. Industry networking remains specified in [INDUSTRY.md](INDUSTRY.md), electrical conservation in [BATTERIES.md](BATTERIES.md), and save envelopes/recovery in [SAVES.md](SAVES.md).
