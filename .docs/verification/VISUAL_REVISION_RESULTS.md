# First POC visual and placement revision — 2026-09-08

This revision responds to the user’s rejection of the original runtime graphics, the near fog and missing placement. It revises the existing first slice; later gameplay systems remain unselected. **Work ownership update:** the user assigned actual player modeling and animation to another agent. This change publishes terrain, placement, UI rendering and the Blender workflow. Local interim player sources/exports remain uncommitted for that agent to replace or reuse; they are not presented as the final character delivery. [FIRST_POC.md](../FIRST_POC.md) contains the current controls and build instructions. The [original verification](FIRST_POC_RESULTS.md) remains historical evidence for the earlier build.

## What changed

- The local interim Blender pass used for these captures has shaped faces, jaw/nose/eyes, swept short hair or longer tied-back hair, fitted teal V-neck waistcoats, collars/lapels/buttons, shaped forearms and hands, and layered leather boots. Male/female selection and both skins retain the shared rig and gameplay dimensions.
- Reviewed actual Blender front/back renders and corrected palette UV-layer mismatches, garment intersections and stray trouser detail. Unity’s narrow UI portraits were squeezing the character horizontally; the revised 768×1024 portrait crops the studio background while preserving model proportions. Four-sample MSAA reduces edge aliasing in the world and portrait.
- Terrain has a restrained grass/dirt/stone palette, bilinear tile filtering, directional shadow sampling and URP depth/normal participation for contact shading. Shadow distance is 160 m. The underlying terrain generator is unchanged.
- Default view radius is ten chunks instead of four. Terrain fog previously began at 48 m and ended at 96 m at the old default; it now begins at 236.8 m and ends at 304 m. Settings allow four through fourteen chunks and fog follows that radius. The camera far plane is 640 m. This is actual streamed terrain, not a backdrop.
- Collected terrain blocks can be placed with the opposite mouse button from mining (right mouse by default). The current target face determines the adjacent cell; the preview is green/red, each committed placement consumes one selected item, and occupancy/player/pile/residency failures consume none. Held repeat uses a 0.22-second delay. [GAMEPLAY.md](../GAMEPLAY.md#first-step-terrain-placement--user-feedback-extension) owns the contract.
- Added, locally installed and validated the [Blender game-art skill](../skills/blender-game-art/SKILL.md). Both installed and versioned copies contain the same workflow. It records actual source/render/import checks and the UV/portrait problems found during this work.

## Verification

The final antialiasing/portrait build completed with **0 errors and 0 warnings**: [build summary](v2-build-summary.txt). [Domain checks](v2-domain-checks.txt) passed **22,371 assertions**. The [runtime report](v2-runtime-report.json) passed **52 checks**, exited 0 and captured no errors. These results use the local interim models described above; rerun visual/geometry checks when the separate asset work lands. Native input was additionally exercised in an ordinary session without test fixtures: Start Expedition, fist-mine grass, collect its item into the hotbar, close inventory and place that one item back into terrain. The resulting hotbar was empty and the world edit count increased. The original user’s running build and interactive Blender scene were preserved; the task’s separate smoke-test window was closed normally before rebuilding.

The runtime test drives mapped keyboard/mouse states, not just direct placement calls. It validates empty/occupied/unready destinations, player collision, menu suppression, conserved inventory, immediate placed-block collision, persistence across unload/reload/origin shifts, and remine-to-item recovery. Existing spawn, movement, mining, inventory overflow, partial pickup, drop merge, seam, origin and cave checks remain exercised. Test mode alone creates fixture stacks and controlled movement.

During test maintenance, stale mouse state was explicitly cleared around the automated mining hold, and the inventory-close assertion was corrected to follow the actual held item ID rather than assume grass occupied slot zero. Neither adjustment relaxes the quantity invariant.

## Measured final local build

Windows x64 Mono development build, Unity 6000.4.4f1 / URP 17.4.0. Intel Core i7-10750H, RTX 2060, 32,553 MB reported system memory. 1280×720, 78° FOV, ten-chunk radius, four-sample MSAA, two terrain workers, vsync off, 90 FPS cap. The working Unity Editor and the original game instance remained open. Blender background rendering had finished before this timing run.

Seed 246813, terrain-1 generator. The test edits near spawn and the x=31/32 seam, relocates to x=640, samples 180 metres of controlled flight, returns through an origin shift and checks a generated cave. It is the same short scenario described in [the original report](FIRST_POC_RESULTS.md#measured-workload-and-limits), now including placement/remine checks at the larger radius.

| Measurement | Observed result |
|---|---:|
| Full nearby demand drain, including settling delay | 8.36 s |
| Streaming frame median / p95 / maximum | 11.11 / 11.18 / 13.15 ms |
| Mining local remesh / maximum mining frame | 9.42 / 18.23 ms |
| Placement local remesh | 3.85 ms |
| Peak resident chunks | 1,182 |
| Terrain triangles after sampled flight | 697,628 |
| Peak Unity allocation in streaming sample | 200,588,431 bytes (~191.3 MiB) |
| Draw-call recorder | Unavailable (−1) |

The first nearby chunks are playable before the full view finishes loading. Allocation above is Unity’s reported allocated memory, not total Windows/driver memory. The cap limits interpretation of the frame timings. An earlier revision run measured a 24.6 ms mining frame; final-run improvement does not establish a worst-case bound. The larger resident view trades greater startup/allocation cost for the requested distant landscape.

Interim source counts are 4,034 male / 4,198 female triangles. Unity imports report **3,938 male / 4,102 female**, 17 bones and one material each; the derived male first-person arms use **1,064**, the visible lower body **1,412**. These are observed source/import counts for the local pass, not performance or art acceptance of the other agent’s forthcoming work.

Verified managed assembly SHA256:

```text
de0dc6798dde123f9586e042e562357adec4a953c21e6b96c2e91d2a4f1581ca
```

## Actual images

These are renders/screenshots of the tested local meshes and game; the separate model/animation agent owns final character integration, not generated concept paintings. Test-mode inventory screenshots contain declared fixture quantities.

- [Blender front: male left, female right](v2-player-blender-front.png)
- [Blender back: female left, male right](v2-player-blender-back.png)
- [World and first-person hands](v2-01-world.png), [placed terrain block](v2-02b-placement.png)
- [Inventory](v2-03-inventory.png)
- [Male / Field skin](v2-04-male.png), [male / Ochre skin](v2-04b-male-alternate.png)
- [Female / Field skin](v2-05-female.png), [female / Ochre skin](v2-06-alternate-skin.png)
- [Generated cave](v2-09-cave.png), [view-distance settings](v2-07-settings.png)

## Remaining limits and review

This is a revised art candidate, not a declaration that the published concept has been reproduced perfectly or accepted by the user. The rig still uses rigid part weights and procedural poses. Skin regions repeat across faces; there is no independent front/back skin-painting template or custom-file importer. Faces, hands, shoulders and animation still need the user’s in-game art/feel review.

Terrain lighting uses directional shadows and ambient fill, not propagated voxel light. Deep caves can still receive inappropriate fill/light leakage. Terrain remains grass/dirt/stone with no vegetation, structures or water. The streaming run is short and capped; separate GPU timings, draw calls, long memory soaks, many-player crowds and factory workloads remain unmeasured. Mining/placement remesh synchronously around the edit and can still produce an isolated frame hitch; measurements below describe this workload, not a guarantee on every frame or machine.

Edits and inventory survive chunk unloading within the session, but quitting resets world progress. Placement does not imply durable saving, functioning crafting, equipment or further roadmap implementation.
