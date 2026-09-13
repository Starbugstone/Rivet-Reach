# Stability and abstraction review — 2026-09-13

Scope: the farming/tag integration and its crafting, machine, save and presentation boundaries; existing survival/industry/mob regression suites. This is not a proof that every repository subsystem is free of defects or duplication.

## Changes from the review

- Ingredient selectors previously scanned Unity Resources and rebuilt tag arrays during every cooking tick. `ItemRegistry.Select` now compiles immutable exact/tag membership once. `IngredientMatcher` shares bounded matching between input staging and final consumption; cooking simulation supplies reusable buffers.
- Piped ingredients previously used a union/count approximation that could block overlapping tags or exhaust input slots prematurely. Staging now verifies that the incoming unit satisfies another requirement and that the rest can fit, while supporting manually loaded surplus and stack limits.
- Cooker state and execution are separated from catalog authoring. Fuel lookup uses the existing compiled processing registry; signatures rebuild only when recipe/input identities change. Only cookers allocate the cooking work buffer.
- Invalid or duplicate tags and empty selectors reject at compilation. Cooking validates against its supplied registry rather than global assets. Crop validation finishes before publishing any growth defaults.
- Added `log`, `planks`, `raw_ore` and `ingot` tags and intersecting `#tag`/name search. Existing crafting identities, quantities and tool tiers remain exact; typed tool/armor/crop capabilities retain their current authorities. Material-tag save compatibility removes only the known additions when comparing pre-review schema-10 content.
- Immature potatoes return **one potato for eating or replanting**, as explicitly clarified by the user after this review. This is the intended exception to the other crops' seed-only immature harvest. Mature potatoes still yield 2–4. The original review reports below predate that clarification; the focused follow-up records the restored rule.
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

[Farming and cooking](../wiki/Farming-and-cooking.md) explains tag searches, pipe staging and the immature-potato exception. Item pages expose the new material tags. The exporter refreshed 175 items and 143 recipes; `python3 Tools/publish_wiki.py --check` validated 196 pages and 7,511 local links/images. GitHub-rendered previews were reviewed in Chromium with no missing images or document overflow on the beetle, prowler, farming, iron-ingot and potato pages. [Deployment run 34754049579](https://github.com/Starbugstone/Rivet-Reach/actions/runs/34754049579) succeeded for code commit `3757a0a`, publishing wiki commit `03b745e`. All 432 deployed pages/assets matched freshly generated publisher output; the wiki checkout remained clean after comparison. [Deployment metadata](stability-2026-09-13/wiki-deployment.json). Live Chromium followed the overview → beetle → prowler links and loaded all 72 content images across the two species, farming, iron-ingot and potato pages, with no broken images or document overflow. [Live review data](stability-2026-09-13/wiki-live-review.json), [published overview](stability-2026-09-13/wiki-live-mobs.png), [published prowler guide](stability-2026-09-13/wiki-live-prowler-guide.png).

[Issue #10](https://github.com/Starbugstone/Rivet-Reach/issues/10) was updated and read back: **23 checked entries, 60 remaining, still open**. [Update evidence](stability-2026-09-13/issue-update.json).

## Potato clarification follow-up — 2026-09-13

The user confirmed that immature potato plants must return one potato, usable as immediate food or planting stock. `CropRules.Harvest` again returns exactly one planting item for every immature cultivable crop; only potatoes use edible produce as that planting item. This applies to wild and cultivated plants through the shared harvest transaction. Mature potato yield stays at 2–4. No definition values, save schema, generator version or artwork changed.

The rebuilt Direct3D 11 player completed in **20.763 seconds**, with **zero errors and warnings**. The focused player suite passed **190 assertions with exit 0**, including actual immature-potato mining returning exactly one potato, growth, tagged cooking, pipes and save/reload. Editor farming checks passed **4,312 assertions**, requiring exactly one planting item at every immature stage; selector/matcher checks passed **21**. [Build](stability-2026-09-13/potato-clarification/build-summary.txt), [player report](stability-2026-09-13/potato-clarification/runtime-report.json), [exit](stability-2026-09-13/potato-clarification/exit-code.txt), [farming checks](stability-2026-09-13/potato-clarification/farming-checks.txt), [tag checks](stability-2026-09-13/potato-clarification/tag-checks.txt).

The earlier broad regression and mob captures above retain their original build identity. This focused follow-up supersedes their immature-potato expectation without claiming those full suites ran again. The guide and potato/stage item pages now explain eating one immediately versus growing extra potatoes, and issue #10 records the explicit potato exception.

## Remaining limits

Long-session hunger cadence, broad balance, very large farms/factories, exhaustive future tag combinations and every graphics device remain unverified. Cooking uses a bounded nine-unit matcher and three input slots; future consumers must choose appropriate bounds. No livestock, fishing, compost, bed, warehouse, weather or renewable-generation gameplay is implied by this cleanup or the issue checklist.
