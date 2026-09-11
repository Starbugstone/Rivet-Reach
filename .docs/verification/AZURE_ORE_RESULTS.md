# Azure ore art verification — 2026-09-11

The user's concept-alignment revision replaces the smooth flecked cube with irregular dark stone facets, connected blue fractures and larger crystal pockets, following the [Blue Signal sheet](../concepts/signal-power/blue-signal-components.jpg). The authored mesh is used for held and dropped ore; the actual mesh's orthographic face render supplies terrain tile 44. Blue terrain seams have a small self-emission term, without added lights or per-ore objects. Mining, ore bands, recipes, voxel collision and save IDs are unchanged.

## Source and import

- [Reproducible Blender authoring](../../Tools/create_azure_ore.py), [editable source](../../ArtSource/Industry/AzureOre.blend), [actual source render](../../ArtSource/Industry/azure-ore-review.png), [source geometry](../../ArtSource/Industry/azure-ore-geometry.json).
- Blender 5.2.0 LTS: **4,129 triangles**, one mesh, one atlas material. Six relief surfaces and a closed inner rock shell fit inside a one-metre cell. Unity FBX, mesh and prefab GUIDs are preserved.
- The general [workshop generator](../../Tools/create_industry_assets.py) excludes Azure ore. Its older `WorkshopKit.blend` and overview render remain useful for the other components; the Azure entry there is superseded by this dedicated source.
- Terrain colour/detail arrays were compared bytewise with the prior assets: only layer **44** changed. Other workshop atlas textures and icons are unchanged.

## Windows player evidence

Review executable: `Builds/AzureOre/RivetReach.exe`, Unity **6000.4.4f1**. Build and checks use the existing open Editor through `Logs/build-request.txt` containing `azure-art-build`; the request refreshes the focused asset, runs the existing industry checks, exports wiki data and builds the player. It preserves the user's open scenes.

Run the focused player check with `-rr-verify -rr-azure-review -rr-output <absolute-output-directory>`. [AzureOreVerification.cs](../../Assets/RivetReach/Code/AzureOreVerification.cs) finds a naturally generated deposit in seed 246813 and clears only its approach. The tunnel has an explicit warm review light for readability. The enlarged standalone model is an inspection fixture, not a placeable ore block.

- [Build result](azure-ore/build.txt): zero errors and warnings.
- [Industry checks](azure-ore/industry-checks.txt): 324 assertions, including finite Azure generation and copper-tier mining.
- [Player report](azure-ore/runtime-report.json): 15 assertions, no reported errors. Checks imported bounds, normals, UV coverage, one submesh, natural ore, rejection of stone-tier mining, copper-tier extraction with exactly one drop, and the actual held mesh.
- [Runtime import count](azure-ore/azure-import.txt) records the imported triangle count and bounds.

![Natural Azure deposit in the cleared review tunnel](../wiki/images/azure-natural-deposit.png)

![Azure model held in game](../wiki/images/azure-held.png)

[Unity inspection model](azure-ore/azure-unity-model.png) · [Inventory and sidebar](azure-ore/azure-inventory.png)

## Wiki and limits

The [Azure ore page](../wiki/Item-azure-ore.md), catalog and ingredient references use refreshed icon 120 exported from the actual game UI source. The page also includes the current deposit and held-model screenshots. Azure Crystal and the other items retain their existing artwork. Wiki sources are checked with `Tools/generate_wiki_items.py` and `Tools/publish_wiki.py --check` before publishing.

Source renders and actual Unity screenshots were visually inspected against the concept. The low-resolution concept is a direction, not an exact surface specification; final artistic acceptance remains with the user. The terrain representation remains a textured chunk mesh, so its silhouette is cubic while the held/dropped model has geometric relief. These focused checks do not measure sustained performance or revalidate every gameplay system. The existing downloadable alpha is not rebuilt or replaced by this asset review build.
