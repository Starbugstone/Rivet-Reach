# Player wiki authoring and publishing

The maintained player guides and generated item pages live in [wiki/](wiki/Home.md). Publish that copy to the separate [GitHub wiki](https://github.com/Starbugstone/Rivet-Reach/wiki) with [publish_wiki.py](../Tools/publish_wiki.py). Keep player-facing explanations here and detailed gameplay specifications in their existing authoritative documents.

## Item pages and artwork

[Items](wiki/Items.md) is the visual catalog; [Crafting recipes](wiki/Crafting-Recipes.md) explains crafting and lists results by station. Each registered item has an `Item-<stable-key>.md` page with its real inventory icon, purpose, acquisition, statistics, recipes and links to results that consume it. Station pages also list the recipes they host. World blocks, crop stages and legacy test tools explicitly describe their acquisition/placement limits.

The exported [catalog](wiki-data/catalog.json) comes from Unity's compiled `RecipeBrowserIndex`, including only registered crafting/furnace recipes and the simulation's crusher mappings. Shaped grids retain their canonical layout and mirroring rules; shapeless grids show one possible arrangement with the actual quantity in each occupied slot. Ingredient tables total a single operation. Fuels and reusable stations are separate from consumed ingredients.

[WikiExport.cs](../Assets/RivetReach/Editor/WikiExport.cs) exports the actual `GameUI.BuildIcons` results, including procedural survival silhouettes and textured terrain icons. Authored PNG icons are copied byte-for-byte; item-browser icon overrides take precedence. Temporary components use an inactive preview scene in Edit mode. The exporter does not start an expedition or change the user's open scene, definitions or import settings. Its reflection calls intentionally fail if the private UI entry points change, rather than substituting approximate artwork.

Player descriptions and acquisition/use notes live in [item-notes.json](wiki-data/item-notes.json). [generate_wiki_items.py](../Tools/generate_wiki_items.py) combines those notes with the export. Edit these inputs, not generated item pages. Generated headings and page names are stable; recipe links point to the corresponding numbered recipe on the output page.

The illustrated construction guides were reconciled from wiki commit `96c6a74` on 2026-09-11. Their existing screenshots were copied into `wiki/images/` so both repository and published copies are self-contained. The dated images retain their original 2026-09-10 feature evidence; this documentation update is not a new gameplay verification run. No third-party artwork or dependency was added.

## Refresh the reference

1. When item data, recipes or icon rendering changes, open this project in the pinned Unity Editor and choose **Rivet Reach → Export wiki items and icons** in Edit mode. In an already refreshed Editor, writing `Logs/wiki-export-request.txt` also requests the export; `Logs/wiki-export-result.txt` reports success or the exception. A new exporter requires an Editor asset refresh first. Preserve any active Play session and wait for Edit mode.
2. Update player notes when acquisition or use changes. Keep links relative, using an item-page target such as `Item-copper-ingot.md`.
3. Run `python3 Tools/generate_wiki_items.py`.
4. Run `python3 Tools/publish_wiki.py --check` and `git diff --check`. The publisher checks generated output, source fingerprints, page/section/image links, icon alternative text, and each icon's item-page destination before writing anything.
5. Review representative GitHub-rendered item pages: shaped and shapeless recipes, processing, noncraftable resources, item index, and construction-guide icon links. Inspect images and click through ingredient/output links. Documentation work does not require a game build or game test suite.

The checked-in export makes prose/layout work possible without launching Unity. If a fingerprinted source changed, refresh the export first. Export source hashes cover definitions, runtime code and icon inputs. Review newly added inputs when extending the exporter.

## Publish

Use a clean, up-to-date checkout of `https://github.com/Starbugstone/Rivet-Reach.wiki.git`. Review and reconcile remote-only changes into `.docs/wiki` before copying, so direct wiki edits are not lost. The publisher deliberately does not fetch, commit or push.

```bash
python3 Tools/publish_wiki.py /path/to/Rivet-Reach.wiki
git -C /path/to/Rivet-Reach.wiki diff --check
git -C /path/to/Rivet-Reach.wiki status --short
```

The tool converts local `.md` page links, including HTML icon links and section fragments, into GitHub wiki page URLs. It copies image directories and uses raw wiki image URLs for reliable GitHub embedding. A manifest tracks published files so later removals affect only previously managed files; unrelated wiki files are preserved. The first publish removes Unity icon `.meta` files left by the older workflow. Screenshots use Git LFS in the main repository; published images must be real PNG bytes, never LFS pointer text.

Commit and push the owned repository changes to the current branch, then commit and push the reviewed wiki checkout. This is covered by the repository's standing publishing authorization. Verify the live item index and representative ingredient links after deployment. If a push races with remote edits, fetch and reconcile; do not force-push.

On `main`, [Sync GitHub Wiki](../.github/workflows/sync-wiki.yml) runs the same publishing tool automatically when wiki pages, exported data or publishing scripts change. It checks out LFS content, validates the sources, copies all inventory icons, converts links and pushes the wiki commit. A successful workflow can fulfill the wiki deployment step; inspect its result before also pushing a manual checkout.

## Verification — 2026-09-11

- Unity 6000.4.4f1 export completed: **127 item icons**, **91 crafting**, **11 furnace**, **3 crusher recipes**.
- Generated **127 item pages** plus the item and crafting indexes; the complete wiki has **136 Markdown pages** including navigation/footer.
- Source and link checks passed for **5,405 local links/images**, including recipe sections and all linked item icons. The export uses the real registered catalogs, so obsolete starter recipes are excluded.
- GitHub's Markdown API rendered the reviewed pages in a local Chromium preview. The visual index loaded all 127 icons with no missing or unlinked images; clicking a pickaxe ingredient opened its ingot page. Reviewed the battery's 4×4 shapeless grid at 1280×1000 and 390×844; the narrow page had no horizontal document overflow. Preview styling approximates the GitHub wiki; deployment checks remain separate.
- No gameplay rules, runtime implementation, scenes, player assets or save-verification artifacts were changed for this documentation task.
