# Crafting identity and component recipes — 2026-10-02

The user confirmed that all starter recipes must retain their original layouts and quantities, with naming cleanup only for those recipes. The user selected completion of [issue #1](https://github.com/Starbugstone/Rivet-Reach/issues/1) and explicitly confirmed one Iron Ingot → one Iron Cog and one Iron Plate → four Rivets. The current recipe contract is owned by Rivet Reach's versioned rules and independent fixtures. The former external recipe-equality report is retired to Git history.

## Delivered behavior and scope

- One Iron Ingot makes one Iron Cog; one Iron Plate makes four Rivets. Both are single-cell shapeless recipes requiring the Machinist's Bench's 4×4 grid. The retained horizontal row of three Iron Ingots makes three Iron Plates.
- All other 110 registered grid recipes remain unchanged. [The complete disposition](../RECIPE_DISPOSITION.md) records each of the 112 recipes, materials, station and KEEP/ADJUST decision. The 56 starter recipes retain their independent fixtures and intentional readable shapes.
- Schema 18's payload and item identities are unchanged. Exact prior component definitions remain compatible; old component stacks are not converted or refunded. Unknown changes to yields, inputs, mirrors, station requirements and unrelated recipes remain rejected. [Save rules](../SAVES.md#component-recipe-compatibility--2026-10-02) own the compatibility boundary.
- Current names and stations are retained: Chest, Furnace, Iron Cog, Machine Casing and the 4×4 Machinist's Bench. The earlier proposed separate Assembly Table and metal-tool/armor intermediate recipes are not added. Ordinary Glass remains a manufacturing component, with Reinforced Tank Glass providing placed industrial windows; a standalone plain Glass building block is not implemented by this reconciliation. This follows the issue's later instruction to reconcile working industry rather than repeat its original proposals.
- Authoring, economy, gameplay, agent instructions, public wording and verification now describe the project-owned catalogue. Original asset provenance and proprietary licensing remain intact. No third-party recipe data, artwork, code or dependency is introduced.

## Build and measured checks

[Build identity](crafting-identity-2026-10-02/build-identity.json) records the base commit, changed source hashes and exact player/managed-assembly hashes. The isolated checkout is `Rivet-Reach-Issue1-20261002`, with player `Builds/RecipeBrowser/RivetReach.exe`. It uses Unity **6000.4.4f1**, Windows x64 Development, D3D11, i7-10750H and RTX 2060. UI scenarios use 1280×720, with the browser also checking its existing 1024×768 layout. Main-workspace artwork edits were preserved; the candidate uses committed artwork.

| Check | Result / evidence |
| --- | --- |
| Windows build | Succeeded, **0 errors / 0 warnings**, 414.46 seconds including cold shader compilation. [Summary](crafting-identity-2026-10-02/build-summary.txt) |
| Component contracts | **137 assertions**: independent Cog/Rivet input/output expectations, all grid positions, smaller-station rejection, exact consumption, no second output, full-cursor preservation, old layout rejection, Glass furnace processing and exact current/previous schema-18 component envelopes. [Results](crafting-identity-2026-10-02/component-recipe-checks.txt) |
| Historical save compatibility | Pinned schema **1–17** envelopes accepted; corrupt/missing/extra fixture and incompatible-content rejection checks retained. Previous-component schema 18 is a **synthetic envelope using the exact former definitions**, not a claimed new full-world historical playthrough. [Results](crafting-identity-2026-10-02/save-compatibility.txt) |
| Crafting core | **1,324,249 assertions**. [Results](crafting-identity-2026-10-02/crafting-checks.txt) |
| Starter recipes | **1,888 independent checks** covering all 56 starter recipes. [Matrix](crafting-identity-2026-10-02/starter-recipe-checks.txt) |
| Survival domain | **47,725 assertions**, including the bootstrap graph, furnace boundaries/conservation, tier gates, hunger, health and equipment. [Results](crafting-identity-2026-10-02/survival-checks.txt) |
| General domain | **92,752 assertions**. [Results](crafting-identity-2026-10-02/domain-checks.txt) |
| Browser index / transfers | **476 index assertions**, **1,342 recipe-transfer assertions**. [Index](crafting-identity-2026-10-02/browser-index-checks.txt), [transfers](crafting-identity-2026-10-02/recipe-transfer-checks.txt) |
| Native browser and component crafting | **937 assertions**, exit 0, no runtime errors. Real pointer input fills the actual placed bench and crafts both components, checking exact inventory changes. Includes the existing navigation, station, cursor, resize and save/UI-rebinding checks. [Report](crafting-identity-2026-10-02/browser-runtime.json) |
| Ordinary Survival route | **7 assertions**, exit 0, no runtime errors: five minutes at 1920×1080, empty-handed gathering, a placed workshop, crafted mining tool and exploration, without supplied resources or Creative. [Report](crafting-identity-2026-10-02/ordinary-survival-runtime.json), [observations](crafting-identity-2026-10-02/ordinary-survival-observations.txt) |
| Native starter crafting | **45 assertions**, exit 0, no runtime errors. Supplied input logs become planks, a placed Workbench and a wooden pickaxe through the controls. [Report](crafting-identity-2026-10-02/starter-runtime.json) |

The focused UI scenarios deliberately supply their fixture inputs/stations. They do not measure the time to gather a complete industrial workshop or establish subjective progression balance. These checks do not certify steady 60 FPS or release readiness.

## Existing legacy smoke-test failure

The additional old `Verify-POC.ps1 -Survival` scenario fails after 57 assertions at “Crops are non-solid scheduled world state” in **both** this candidate and the unchanged September optimized player. [Candidate report](crafting-identity-2026-10-02/legacy-survival-runtime.json), [baseline report](crafting-identity-2026-10-02/legacy-survival-baseline.json). Their binary hashes are in the build identity.

Its source still assumes `ScheduledCrops == 1` for the entire world after planting one potato, and later assumes zero after harvest. The current scheduler also registers naturally generated immature crops. This old fixture needs maintenance against the shared crop/light scheduler; the failure is not hidden or presented as a passing survival runtime suite. Production crop behavior and this old fixture are unchanged by issue #1.

## Naming-only metadata cleanup

Remaining explicit external-game naming was removed from maintained prose, comments and historical workload labels. Historical build hashes, timestamps, assertions and results are unchanged; old build-context references now point to their retained independent starter fixtures. The fluid stress source has a comment-only wording change after the recorded build. No fluid behavior changed or new fluid test run is claimed.

## Player guide and captures

[Workshop components](../wiki/Workshop-components.md) explains the bench bootstrap, Plates, Cogs, Rivets and machinery uses. The actual inventory catalog was re-exported from Unity: **195 items, 112 grid recipes, 199 total recipe entries**. Existing icon bytes are unchanged. Generated Cog, Rivet, Iron Ingot, Iron Plate and Machinist's Bench pages reflect the new inputs and quantities.

![One ingot ready to make one Cog](../wiki/images/components-2026-10-02/cog-recipe.png)

![One plate ready to make four Rivets](../wiki/images/components-2026-10-02/rivets-recipe.png)

These are direct October 2 native screenshots, visually inspected for the bench, single input and output quantities. No new artwork or machine-rendering behavior was introduced.

`python3 Tools/publish_wiki.py --check` passes in the isolated candidate: **233 pages and 9,963 local links/images**. The main workspace check correctly rejects its pre-existing modified `BlockTiles.asset`; that unrelated artwork is not included in this task. All **881** local links/anchors in changed non-wiki Markdown pass. The remote wiki was compared against maintained pages before publication, with no unrelated remote-only page changes. [Documentation checks](crafting-identity-2026-10-02/documentation-checks.txt) record the distinction.

## Publication and live review

Repository commits `a13bc8d` and `f223c69` were pushed to `main`. [Wiki deployment 37038259572](https://github.com/Starbugstone/Rivet-Reach/actions/runs/37038259572) succeeded and published wiki commit `5db220b5d7c2e35f4f6ae07c12e5959c8fdb0967`. The live Workshop components, Cog and Rivet pages show the approved quantities; both native screenshots load at their original 1280-pixel width. The guide-to-Cog and Rivet-to-Iron-Plate links were followed successfully, and the live item index loads all 195 icons with no broken images. The browser’s focused click command failed, so these links were followed through their actual DOM anchors. Issue #1 is closed with the clarified starter-preservation boundary and measured evidence.

## Reproduction

Use the pinned Editor on a clean checkout containing this change:

```text
Unity.exe -batchmode -quit -projectPath <checkout> -executeMethod RivetReach.Editor.ComponentRecipeChecks.VerifyAndBuild -logFile <build-log>
```

Then run `Tools/Verify-RecipeBrowser.ps1` and `Tools/Verify-POC.ps1 -StarterCrafting -Executable Builds/RecipeBrowser/RivetReach.exe`, each with a fresh output directory. The old `-Survival` fixture failure above remains separately recorded.

[Machine visibility review](MACHINE_VISIBILITY_REVIEW.md) answers the user's follow-up about the short rendering distance; it is a source/evidence review and changes no draw-distance or factory-residency rule.
