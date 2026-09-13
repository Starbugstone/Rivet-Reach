# Portable storage verification — 2026-09-13

[Battery rules](../BATTERIES.md#portable-stored-contents--2026-09-13), [tank rules](../MULTIBLOCKS.md#portable-tanks--2026-09-13) and [schema 8](../SAVES.md#portable-storage-compatibility--2026-09-13) own behavior. Player instructions are in [Tanks](../wiki/Tanks.md#repairs-and-dismantling) and [Electricity and batteries](../wiki/Electricity-and-batteries.md).

## Measured results

The pinned Unity **6000.4.4f1** built the isolated storage candidate successfully: **0 errors, 0 warnings**, 21.275 seconds. [Build summary](portable-storage-2026-09-13/build-summary.txt), [artifact hashes](portable-storage-2026-09-13/artifact-hashes.json) and [source snapshot](portable-storage-2026-09-13/source-snapshot.json) identify this artifact; concurrent Floater work was excluded. The executable is delivered at `Builds/PortableStorage/RivetReach.exe`.

- **86 focused Editor assertions** pass for exact payloads, incompatible stacks, cursor splits/swaps, full containers, quick transfers, chests, placement, emptying, recipe exclusion, malformed saves, water/lava acceptance and schemas 1–7 stack layouts. [Report](portable-storage-2026-09-13/checks.txt).
- Inventory, crafting, survival, battery, multiblock, industry and shared-grid regression suites pass. The inventory suite exercises formats 1–8, although its retained report uses the older fixed “schemas 1–6” label.
- **180 native-player assertions**, **exit 0**, verify actual mining/re-placement, one returned item, exact 1,234,567 mJ and 12,345 mL recovery, no Creative duplication, bank member removal without draining other cells, invalid-controller recovery with 250,001 mL and retained capacity, lava bucket round trips, rejection of mixed liquid, conserved lava piping, charge-bearing item piping between chests, physical dropped-item pickup, saves and both selected-hand and cursor emptying. Held Shift-left-click does not mine the targeted block. [Runtime report](portable-storage-2026-09-13/runtime/runtime-report.json).
- **58 legacy-player assertions**, **exit 0**, load eight actual historical checkpoints covering **every schema 1–7**, reject unrelated content changes, initialize absent item contents empty, migrate to schema 8 and compare every reserialized state byte after reload. [Legacy report](portable-storage-2026-09-13/legacy/runtime-report.json).
- **183 save/recovery assertions**, **exit 0**, cover the existing complete checkpoint workflow, corruption/recovery and failed-load rollback. A separate process resumes the checkpoint with **5 assertions**, **exit 0**. [Save report](portable-storage-2026-09-13/save/runtime-report.json), [restart report](portable-storage-2026-09-13/resume/runtime-report.json).

The first Direct3D 12 restart run passed its five assertions but exited with Windows access violation `-1073741819` during shutdown. Repeating that restart with `-force-d3d11` passed and exited normally. The focused storage run itself passed under Direct3D 12. This does not establish a general fix for the renderer shutdown issue.

## Visual review and reproduction

Reviewed actual game captures of the [replaced charged battery](../wiki/images/storage-restored-battery.png), [replaced lava tank](../wiki/images/storage-restored-lava-tank.png), [carried-content tooltip and emptying hint](../wiki/images/storage-carried-contents.png), [saved lava quantity](../wiki/images/storage-lava-pipes.png), and [configured tank/chest pipe connections](../wiki/images/storage-lava-connection.png). No item geometry or inventory artwork changed.

Run `Tools/Verify-PortableStorage.ps1 -Build -OutputDirectory <fresh directory>` in the refreshed project. The existing-Editor build request is `portable-storage-build`; batch entry is `RivetReach.Editor.ProjectBuild.BuildPortableStorage`. Legacy review uses `-rr-verify -rr-portable-legacy-review -rr-legacy-directory <earlier checkpoint directory>`. The ordinary save checks use `Tools/Verify-Saves.ps1` against the candidate executable.

This is focused functional evidence, not a large-factory performance or balance playtest. Stored items retain ordinary dropped-item expiry and lava-destruction behavior. Mining preserves contents; the explicit emptying command intentionally destroys them. Recovered tanks still require a valid shell to operate after replacing their controller.

## Wiki

The actual isolated Unity export contains **134 items** and **122 crafting/processing entries**. Generated item pages and updated guides pass **148 pages / 6,111 local links and images** through `Tools/publish_wiki.py --check`. Publication and live-rendering results are recorded after deployment.
