# Windows 0.0.1 alpha verification — 2026-09-10

[Download v0.0.1](https://github.com/Starbugstone/Rivet-Reach/releases/tag/v0.0.1) · [Player instructions](../releases/0.0.1.md)

## Build identity

The release player was built from a fresh detached checkout of `7a04193cc8ef862d550384808052914342d5a29c`, tagged `v0.0.1`, using Unity **6000.4.4f1 / URP 17.4.0**, Windows x64 and `BuildOptions.None`. Application version is **0.0.1**. The build completed with **zero errors and zero warnings**, in 308.46 seconds, reporting 228,380,739 bytes. The separate first import automatically updated two bundled Shader Graph GUID references in the ignored package cache; no package version or tracked source change was needed.

The pre-existing global render settings difference removes unused SSAO blue-noise runtime resources, matching the committed switch to static ambient-occlusion sampling. It is retained in the release commit. An earlier local build failure was a running player's locked Burst DLL; this release used a separate checkout/output and completed normally.

## Checks on this artifact

The build entry point ran the existing domain, crafting, starter-recipe, survival, terrain/ore/tree, fluid, industry, multiblock, battery and connected-pipe checks. Recorded results include 1,158,852 crafting assertions, 1,862 starter-recipe acceptance checks, 92,350 domain assertions, 47,318 survival assertions, 324 industry assertions, 48 multiblock assertions, 29 battery assertions and 4,736 connected-pipe assertions. All passed. The registry contains 91 crafting recipes and 11 furnace recipes.

`Tools/Verify-Creative.ps1 -Executable <release-player> -FullRun` completed with exit code 0. Each scenario ran the same non-development player at 1280×720 on an Intel Core i7-10750H / NVIDIA GeForce RTX 2060 workstation with 32,553 MB reported RAM.

| Runtime scenario | Passed assertions |
| --- | ---: |
| Creative controls/catalog and return to Survival | 312 |
| Industrial workshop | 163 |
| Multiblock tanks | 287 |
| Placement, dropped items and movement | 96 |
| Recipe browser and crafting | 491 |
| Survival | 86 |
| Batteries, banks, pumps, controllers and held items | 471 |
| **Total** | **1,906** |

All seven reports returned `PASS` with empty error lists. Title and flight screenshots were inspected. These are automated scenarios and limited visual inspection on one workstation, not extended player acceptance or minimum-hardware certification. Earlier feature reports retain their own build identities.

## Download integrity

`RivetReach-0.0.1-alpha-windows-x64.zip` is **78,238,766 bytes** and contains the complete player, runtime data/libraries, instructions, proprietary license, third-party notices and license texts. Debug symbol files and Unity `DoNotShip` output are excluded. The included `build-manifest.json` identifies the source commit and hashes 202 payload files. The archive contains 203 files including that manifest.

SHA-256: `cce0b0a6ca545c9c6720c4fb28848e56577a954e89a119378eae035a4b80d649`.

ZIP integrity and every payload hash passed after fresh extraction. The archive was downloaded back from GitHub; its SHA-256 matches the local archive and GitHub's asset digest. `SHA256SUMS.txt` and `build-manifest.json` are also separate release assets.

The freshly extracted copy then passed `Tools/Verify-POC.ps1 -Executable <extracted-player>`: **265 assertions**, exit code 0, `PASS` and an empty error list. This adds startup, both player appearances/skins, visual/tool interactions, terrain streaming, mining, inventory and placement coverage. Together with the seven-scenario suite, **2,171 runtime assertions** passed on this release payload.

## Reproduction and limits

Use a clean checkout with Git LFS assets present, run `Tools/Build-Release.ps1` with the pinned Windows Editor, and verify the resulting player with the command above. The script preserves any Editor open on that checkout by refusing a competing batch build. Keep the entire player folder together when packaging or running it.

World progress remains session-only. Durable saves, multiplayer and other operating systems are not included. A second-machine run and extended uninterrupted gameplay remain unverified. The ordinary startup path uses no verification fixtures; automated scenarios opt in through explicit command-line flags.
