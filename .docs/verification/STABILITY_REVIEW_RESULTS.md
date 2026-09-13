# Stability and abstraction review — 2026-09-13

Scope: the farming/tag integration and its crafting, machine, save and presentation boundaries; existing survival/industry/mob regression suites. This is not a proof that every repository subsystem is free of defects or duplication.

## Changes from the review

- Ingredient selectors previously scanned Unity Resources and rebuilt tag arrays during every cooking tick. `ItemRegistry.Select` now compiles immutable exact/tag membership once. `IngredientMatcher` shares bounded matching between input staging and final consumption; cooking simulation supplies reusable buffers.
- Piped ingredients previously used a union/count approximation that could block overlapping tags or exhaust input slots prematurely. Staging now verifies that the incoming unit satisfies another requirement and that the rest can fit, while supporting manually loaded surplus and stack limits.
- Cooker state and execution are separated from catalog authoring. Fuel lookup uses the existing compiled processing registry; signatures rebuild only when recipe/input identities change. Only cookers allocate the cooking work buffer.
- Invalid or duplicate tags and empty selectors reject at compilation. Cooking validates against its supplied registry rather than global assets. Crop validation finishes before publishing any growth defaults.
- Added `log`, `planks`, `raw_ore` and `ingot` tags and intersecting `#tag`/name search. Existing crafting identities, quantities and tool tiers remain exact; typed tool/armor/crop capabilities retain their current authorities. Material-tag save compatibility removes only the known additions when comparing pre-review schema-10 content.
- Immature potatoes no longer return an edible resource, correcting issue #10's maturity requirement. Other immature crops return at most one seed; mature yields and renewable planting remain unchanged.
- The Windows build path now shares the alpha's explicit Direct3D 11 selection and restores Editor graphics settings afterward. The first Direct3D 12 restart wrote a passing gameplay report but exited `-1073741819`; Windows Event 1000 identified `D3D12Core.dll`, exception `0xc0000005`, offset `0xa1f5`. It is excluded from clean-pass totals. This repeats the [previously documented renderer shutdown fault](SAVE_RESULTS.md#reproduce-and-limits), rather than proving Direct3D 12 stable.

## Verification

Unity 6000.4.4f1 Windows development player, September 13, 2026: **zero build errors and warnings**, **34.228 seconds**, **323,595,321 bytes**, Direct3D 11. [Build summary](stability-2026-09-13/build-summary.txt).

| Check | Result | Evidence |
|---|---|---|
| Selector, catalog and ingredient edge cases | 21 assertions | [Editor checks](stability-2026-09-13/tag-checks.txt) |
| Farming definitions and harvest rules | 4,312 assertions | [Checks](stability-2026-09-13/farming-checks.txt) |
| Survival, industry, electric furnace, shared allocation | PASS | [Survival](stability-2026-09-13/survival-checks.txt), [industry](stability-2026-09-13/industry-checks.txt), [electric furnace](stability-2026-09-13/electric-furnace-checks.txt), [allocation](stability-2026-09-13/grid-allocation-checks.txt) |
| Wild/cultivated crops, immature-potato mining, tag search, coal/electric cooking, pipes, generation ledger | 190 assertions; exit 0 | [Player report](stability-2026-09-13/farming-runtime.json) |
| Durable saves, corruption/recovery, conservation and rollback | 183 assertions; exit 0 | [Save report](stability-2026-09-13/save-runtime.json) |
| Separate-process Continue Latest Save | 5 assertions; exit 0 | [Restart report](stability-2026-09-13/resume-runtime.json) |
| Beetle/prowler/Floater presentation, AI, collision, melee, wall climbing, loot and persistence | 117 assertions; exit 0 | [Mob report](stability-2026-09-13/mob-runtime.json) |
| Original schema-9 bridge save | 17 assertions; exit 0 | [Legacy report](stability-2026-09-13/legacy9-runtime.json) |
| Pre-material-tag schema-10 farming save | 17 assertions; exit 0 | [Additive-tag report](stability-2026-09-13/legacy10-runtime.json) |

Verification exercises real Unity/player code, with synthetic inventories only for isolated matcher edge cases. After warm-up, 10,000 planning/staging pairs took **13.380 ms** and allocated **zero managed bytes** in the Editor sample. This excludes station/network/UI work and is not a large-factory performance claim. Legacy runs loaded untouched copies of the original fixtures into isolated save directories, then exercised retained/new generation and re-saving.

Reproduce using `Tools/Verify-Farming.ps1 -Build -FullReview -OutputDirectory <fresh-directory>` with the pinned Editor open in Edit mode; it reuses the existing save and mob runners. Use `-LegacySaveDirectory <isolated-fixture-copy>` separately for historical saves. The [renderer identity](stability-2026-09-13/renderer.txt) and [mob process-exit evidence](stability-2026-09-13/mob-exit-evidence.txt) are retained. The native Direct3D 12 failure described above is not counted as a pass; all final rows used the rebuilt Direct3D 11 player and clean process exits.

## Player wiki

The wiki now has a [Mobs overview](../wiki/Mobs.md), [Rustback beetle](../wiki/Rustback-Beetle.md) and [Dusk prowler](../wiki/Dusk-Prowler.md) guides linked with [Floater](../wiki/Floater.md). Fresh in-game captures show both original creatures and actual wall climbing. Species values were checked against the current definitions and AI. Passive animals and chickens remain future scope.

[Farming and cooking](../wiki/Farming-and-cooking.md) explains tag searches, pipe staging and the immature-potato correction. Item pages expose the new material tags. The exporter refreshed 175 items and 143 recipes; `python3 Tools/publish_wiki.py --check` validated 196 pages and 7,511 local links/images. GitHub-rendered previews were reviewed in Chromium; deployment verification follows publication.

## Remaining limits

Long-session hunger cadence, broad balance, very large farms/factories, exhaustive future tag combinations and every graphics device remain unverified. Cooking uses a bounded nine-unit matcher and three input slots; future consumers must choose appropriate bounds. No livestock, fishing, compost, bed, warehouse, weather or renewable-generation gameplay is implied by this cleanup or the issue checklist.
