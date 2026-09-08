# Rivet Reach - Concept Art

**Status:** visual drafts, 2026-09-08. These are generated 2D references, not Unity screenshots, measured low-poly meshes, usable skin textures or implemented features. The user requested the character sheet, equipment exploration and multiple world views. The first playable scope remains in [DEVELOPMENT_STRATEGY.md](../DEVELOPMENT_STRATEGY.md#6-locked-first-step-and-provisional-later-sequence).

## Gallery

| Image | Purpose | Milestone relationship |
|---|---|---|
| [Player turnarounds](player-male-female-turnaround-v3.png) | Male left, female right; both with front/back coverage, same practical visual language | First-step player/model reference; both support skins |
| [Tools and weapons](player-tools-weapons-v1.png) | Player poses and first-person grip ideas for pickaxe, axe, sword and crossbow | Future visual proposals only; the first step still mines with fists |
| [World 01 - Grassland traversal](world-grassland-fps-v1.png) | First-person scale, terrain palette, depth and restrained HUD | First-step terrain/FPS target |
| [World 02 - Cave mining](world-cave-mining-fps-v1.png) | Shallow cave visibility, fist targeting, excavation and loose-item readability | First-step mining/streaming target |
| [World 03 - Ridge inventory](world-ridge-inventory-v1.png) | Inventory over the world, player preview and disabled crafting area | First-step inventory/UI draft |

### Player turnarounds

![Male and female player turnarounds](player-male-female-turnaround-v3.png)

Both columns should provide equivalent information for modeling. The female's longer tied-back hair and subtle proportion differences reflect the user's preferred revision. Practical workwear and a capable stance remain common to both. This is one proposed skin for each model, not a fixed player identity. Precise orthographic proportions, UV layouts, topology and animation still need actual authoring and checks.

### Equipment exploration - later scope

![Player holding tools and weapons](player-tools-weapons-v1.png)

Pickaxe and axe explore gathering-tool silhouettes; sword and crossbow are proposed weapon examples, not a locked weapon catalogue. Both characters can use all equipment regardless of which pose is illustrated. This sheet does not add tools, combat, weapon stats, crafting recipes or ammunition to the first milestone.

### World 01 - Grassland traversal

![First-person grassland terrain concept](world-grassland-fps-v1.png)

An approachable terrain-only starting area. The visual target is modular grass/earth/stone, readable block scale and distance fog rather than handcrafted scenic geometry. No structures or factory assets are needed for this view.

### World 02 - Cave mining

![First-person cave mining concept](world-cave-mining-fps-v1.png)

A shallow daylight-accessible cave exercises overhangs, exposed voxel faces and mining feedback without requiring the deferred torch, tool or full lighting progression. Keep the selected block readable; impact particles and item presentation must remain separate from authoritative block removal and quantity accounting.

### World 03 - Ridge inventory

![Inventory over voxel ridge concept](world-ridge-inventory-v1.png)

The player remains in the terrain world while interacting with real inventory. The crafting area is visibly unavailable. The illustration establishes hierarchy, contrast and material language; exact slot counts, hit areas, text and input behaviour must be constructed and verified in Unity against [GAMEPLAY.md](../GAMEPLAY.md#14-locked-first-step-interaction-contract), not inferred from painted pixels.

## Turning these images into the actual game

| Visual reference | Required implementation approach | Evidence still needed |
|---|---|---|
| Stepped terrain and cave walls | Mesh the real one-metre voxel grid in streamed 32-cubed chunks; use the authoritative integer coordinates and seeded definitions | Negative-coordinate seams, generation order, edits across borders and origin shifts |
| Grass/earth/stone surfaces | Small reusable material/texture set, initially test 32 texels per face; select atlas/array handling with the mesher | Repetition, face UVs, seams, batching, and consistent block scale at normal reach |
| Daylight, cave fill and haze | URP baseline with restrained shadows, stable review lighting and distance fog | GPU cost and readability in an actual player build; painted ambient effects do not mandate expensive rendering |
| Visible world depth | Real generation/load/unload around player demand; fog supports presentation but cannot hide missing nearby collision | Declared render distances, queue age, safe frontiers, travel/unload memory and preserved session edits |
| Player body, fists and ponytail | Two compatible rigs/skin layouts, consistent first-person appearance, simple geometry and controlled animation | Actual triangle/material/bone counts, clipping, silhouette readability and unchanged gameplay dimensions |
| Held equipment | Separate reusable meshes with consistent grip/socket conventions and appropriate first-person poses when selected later | Hand contact, wrist/weapon clipping, center-screen visibility and shared use by both models |
| Target/mining effect and item pile | Grid targeting, immediate feedback, versioned mesh updates and one authoritative drop transaction | No stale collision/target, no duplicate yield and correct partial pickup |
| Inventory overlay | Build native Unity UI from the real stack model; 48 main slots and 12 hotbar slots are the working specification | Rebinding, UI scaling, hit targets, full-inventory handling and input suppression while the panel is open |

Keep [CONTENT_PIPELINE.md](../CONTENT_PIPELINE.md) authoritative for scale, export/import, skin and geometry budgets, [SIMULATION.md](../SIMULATION.md#12-first-step-terrain-and-streaming-validation) for streaming evidence, and [GAMEPLAY.md](../GAMEPLAY.md) for interactions. No performance, precise geometry or UI-layout claim can be certified from these images. Minor painted perspective, grip and grid irregularities must be resolved during implementation; do not reproduce them as game rules.

## Provenance and storage

Created with the built-in image-generation tool under the user's concept-art request. Character/style input was the earlier conversation-generated male/female revision with longer female hair; no external third-party art was supplied. These images are project concepts under the repository's [ownership notice](../../LICENSE.md), not a licence change or imported marketplace asset pack. See [generation prompts](PROMPTS.md) for exact instructions and reference lineage. Generative output may differ on rerun.

PNGs are tracked with Git LFS; Markdown stays in ordinary Git. After cloning, install Git LFS if needed and run `git lfs pull` to obtain the full images. These references belong in `.docs/concept-art`, outside Unity's runtime `Assets` tree. Do not import a full concept board as a player skin, terrain texture or UI background. Author those actual assets separately when implementation is requested.

## File verification

All five final PNGs passed signature, chunk-checksum and image-data decompression/size checks and were visually reviewed as concept drafts. The inventory illustration shows the intended 8-by-6 main grid and twelve-slot hotbar; these still require native UI implementation and interaction testing. The corrected traversal view has no out-of-reach selected block, and the cave view removes the unintended water/tree backdrop.

| File | Pixels | Bytes | SHA-256 |
|---|---|---|---|
| `player-male-female-turnaround-v3.png` | 1254 × 1254 | 1,748,680 | `99c17f953bccb9c1f30e9b60b043716c08674cbc38c52f742cf3e434d9301905` |
| `player-tools-weapons-v1.png` | 1536 × 1024 | 1,911,586 | `c2b33e82430bf379a79d13dbbe77e265c113d9eb4b75b2e81f560e927c08073b` |
| `world-cave-mining-fps-v1.png` | 1672 × 941 | 1,594,155 | `c301a6675ae79cc5c1d1159df48b62a97e61a1fc8d85e830d6e3c84e459752ef` |
| `world-grassland-fps-v1.png` | 1672 × 941 | 1,682,767 | `2bfc5fc50fcdabb2f8a1abfff4e3e1ac99f3eeeafee046c67542793413eba502` |
| `world-ridge-inventory-v1.png` | 1672 × 941 | 1,814,525 | `8e675397649371f4588ce8d05cfbfc37ae888567cdaf7a159c08662c5a248459` |
