# Floater verification — 2026-09-13

[Working rules](../MOBS.md#floater--2026-09-13) · [Player guide](../wiki/Floater.md)

The playable review build is **`Builds/Floater/RivetReach.exe`**, using Unity **6000.4.4f1 / URP**. [Build summary](floater-2026-09-13/build-summary.txt): succeeded with **0 errors and 0 warnings**. [Artifact identity](floater-2026-09-13/artifacts.json) records the final managed assembly and model hashes. The Floater itself adds no save payload fields; this shared checkout also contains the separately authorized portable-storage schema-8 work.

## Measured checks

- [Full standalone mob run](floater-2026-09-13/mob-runtime-report.json): **117 assertions passed**, no errors, at **07:41:24 UTC**. Covers the existing beetle/prowler regression suite and Floater hover, ceiling rejection, bounded wall detours, one-block ascent, support removal, hostile pursuit, telegraphed attacks, avoidance and shared damage.
- [Final focused run](floater-2026-09-13/focused-runtime-report.json): **40 assertions passed**, no errors, at **07:44:46 UTC**, after the final screenshot framing and held-rock rotation changes. Also exercises exact damaged-mob identity/position restoration, one drop on lethal damage, repeated-hit rejection, death-checkpoint reload without duplicate loot, body removal, ordinary pickup and inventory save/load conservation.
- [Asset and legacy checks](floater-2026-09-13/asset-and-legacy-checks.txt): seven checks passed, including a real retained pre-Floater schema-7 checkpoint. Deliberately changing an existing beetle health value or stone stack limit still rejects that checkpoint. The historical checkpoint path is recorded; it is a local fixture, not bundled player save data.
- [Unity import report](floater-2026-09-13/import-report.txt): **1,266 triangles, 7 bones, one material and four actions** for the Floater. The collectible rock imports **116 triangles and one material**. Explicit source triangulation removed an intermediate FBX polygon-import warning; the final Floater preserves the source count. Existing beetle/prowler mesh counts remain 904/1,476.

The full suite predates only the last capture framing, held-rock orientation and test-driver changes; its artifact timestamp is retained rather than presenting it as another full run on the final executable. The final focused run uses the final review executable. Runtime report timing/search counters refer to the last restored mob system after save/load, so they are **not aggregate performance measurements**.

## Reviewed visuals

Actual Blender source renders were reviewed from the [front](floater-2026-09-13/blender-front.png) and [back](floater-2026-09-13/blender-back.png). Source files and repeatable authoring remain in `ArtSource/Mobs` and `Tools/create_floater_assets.py`. The matching [inventory icon](../wiki/icons/175.png) is rendered from the collectible model.

![Floater hovering over real voxel terrain](floater-2026-09-13/floater-hover.png)

![A dropped Floater Rock after defeat](floater-2026-09-13/floater-rock-drop.png)

![The rock and its mineral vein in the player's hand](floater-2026-09-13/floater-rock-held.png)

[Inventory capture](floater-2026-09-13/floater-rock-inventory.png). The daylight encounter is an explicit test fixture for clear inspection; ordinary natural spawning remains night-only. The drop simulation is briefly frozen for the close capture, then resumed for the real proximity-pickup assertion.

## Reproduce and remaining review

Run the Blender authoring script in a separate background Blender 5.2 process. Request `floater-build` through `Logs/build-request.txt` in the already-open pinned Editor. Run `Tools/Verify-Mobs.ps1 -Player <Floater executable> -OutputDirectory <fresh directory>` for the full regression suite, or `Tools/Verify-Floater.ps1` for the focused checks. Verification enables background updates so switching applications does not freeze a test player.

This is a bounded surface-hover melee creature, not unrestricted aerial navigation. Balance, animation appeal, extended-arm intersections in cramped spaces and long-session mob performance remain playtest/art-review concerns. No broader mob loot tables, recipes for Floater Rock or extra ecology were added.
