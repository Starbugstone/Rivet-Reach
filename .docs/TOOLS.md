# Tool speed and durability

Working implementation under the user's 2026-09-19 request: better pickaxes and axes must work faster, tools lose a little durability per use, higher tiers last longer, and worn tools must not stack. Counts below are working balance defaults, not user-selected numbers or playtested optimums.

## Material progression

The existing definition-driven effective speed is retained for all five tiers. `ItemRegistry.MiningSeconds` is the player mining authority: hardness divided by effective tool speed. Pickaxes accelerate eligible stone/ore; axes accelerate wood, planks, workbenches, chests, beds and doors. Using the wrong tool does not grant that speed. Ore requirements and the minimum 25% ore-over-stone work remain unchanged.

| Tier | Effective speed | Durability (successful uses) | Stone | Log with axe |
|---|---:|---:|---:|---:|
| Wood | 2× | 64 | 1.5 s | 0.75 s |
| Stone | 4× | 128 | 0.75 s | 0.375 s |
| Copper | 5× | 256 | 0.6 s | 0.3 s |
| Iron | 6× | 512 | 0.5 s | 0.25 s |
| Diamond | 8× | 1,536 | 0.375 s | 0.1875 s |

Calculated times use current definitions. Actual input timing evidence belongs in [verification](verification/TOOL_DURABILITY_RESULTS.md). Switching selected slots or replacing the held stack resets unfinished mining, including two tools of the same type.

## What counts as use

All five tiered tool families—pickaxe, axe, sword, shovel and hoe—spend one durability after a successful player block break or landed hostile/passive hit. A hoe also spends one on successful tilling or crop harvest/replant. One initiated natural-tree felling costs one use; queued secondary log removals do not charge again. The final use completes normally, then the tool breaks and disappears with a notification. Durability does not reduce speed before breakage.

The Wrench has 256 uses and spends one on a successful pipe-end adjustment; the Fishing Rod has 128 and spends one on a successful catch. Cancelled/failed mining, insufficient ore grade, air swings, invalid interactions, unsuccessful fishing and simply opening an interface do not consume durability. Creative actions preserve wear. Buckets, armor and placed machinery are outside this tool-wear request; no repair/enchantment system is introduced.

The single configuration [ToolDurability.json](../Assets/RivetReach/Resources/Definitions/ToolDurability.json) owns tier/utility capacities and participates in current-save compatibility. Legacy test tools use their authored tier.

## Item ownership and presentation

`ItemStack.Wear` is an integer count of completed uses. Zero means pristine. A tool's remaining durability is maximum minus wear. Wear participates in value identity, inventory revision and saved state. A worn tool is always an individual item, even beside another tool with identical wear. Existing pristine tools already have a one-item stack limit.

All ordinary inventory/cursor/quick-transfer/organization/chest/drop/pickup paths preserve wear. Item pipes use their existing metadata-preserving chest transfer path for worn tools. Bulk crates/controllers reject worn tools because their storage holds item identity and quantity only; use chests. Crafting ingredient matching excludes worn tools so automatic recipe gathering cannot silently erase instance state. Storage-empty gestures cannot repair tools.

Inventory and hotbar slots show a green-to-red remaining-durability bar. Tooltips and the selected-tool label show exact remaining/maximum uses. Bars refresh when wear changes even though item ID and count are unchanged. Item icons and held artwork are unchanged.

## Persistence

Schema **18** appends wear to every stack payload, including player inventory, stations, dropped items and crafting state. Schemas 1–17 initialize tools pristine without changing their byte layout. Schema-17 food saturation and all preceding content projections remain supported; unrelated content changes continue to reject. Schema 18 requires the current complete fingerprint including durability configuration. Negative wear, wear at/above maximum, wear on non-tools and multiple worn items in one stack reject before world publication. Writing worn tools through a legacy writer rejects rather than silently repairing them.

[Save rules](SAVES.md), [player guide](wiki/Tools-and-durability.md), and [measured verification](verification/TOOL_DURABILITY_RESULTS.md) own their corresponding detail.

## Run and verify

The focused Windows player is `Builds/Tools/RivetReach.exe`. `Tools/Verify-Tools.ps1` runs an ordinary Survival route before the focused tool checks; `-Focused` is for rerunning focused checks after that route has already been recorded. `-SaveDirectory` checks a fresh-process continuation; add `-Legacy` for an actual historical checkpoint. Use separate output folders and preserve historical inputs. The Editor request `tools-build` runs tool/inventory/storage/survival/food compatibility checks and builds that player.
