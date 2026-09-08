# Rivet Reach - Content and Asset Workflow

> **Status:** working specification, 2026-09-08. The first POC now supplies the initial runtime kit; detailed production rules remain working specifications. See [FIRST_POC.md](FIRST_POC.md#actual-first-visual-kit) for actual assets, skin layout and measured geometry.

## 1. Scale and source ownership

Use 1 metre per voxel and model modular assets against that grid. Blender working scenes use metric units at unit scale 1. Apply intended object scale before export. Export presets convert axes consistently; do not fix every imported machine with a different corrective rotation.

A block-sized machine's anchor is its lower grid corner, with its footprint explicitly recorded in the content definition. Reusable attachments use a named connector pivot. Visual meshes can extend within their declared bounds but cannot invent solid occupied cells absent from gameplay data. Decorative animation must not move authoritative ports or collision unexpectedly.

Source layout: `ArtSource/` for editable Blender/image/audio originals; `Assets/RivetReach/` for imported runtime content; `Packages/` for pinned dependencies; `ProjectSettings/` for Unity settings. Specifications remain in `.docs/`. The first POC uses this layout for its original character sources and runtime exports.

Keep `.blend` source separate from Unity's imported runtime hierarchy. Publish explicit interchange exports with documented settings so opening a project does not depend on every contributor's local Blender executable or personal defaults. FBX is the initial mesh/animation interchange choice; texture outputs use lossless source images and Unity import compression settings selected per use.

## 2. First visual kit

The versioned [concept-art gallery](concept-art/README.md) contains the balanced male/female turnaround sheet, a later equipment study and three first-person terrain/UI drafts, with implementation notes and generation provenance. Images guide asset work; the gameplay/terrain rules and milestone boundary remain authoritative.

The locked first step requires a cohesive **terrain/player/inventory visual concept**, as scoped in [DEVELOPMENT_STRATEGY.md](DEVELOPMENT_STRATEGY.md#6-locked-first-step-and-provisional-later-sequence). Its kit contains a small natural terrain palette, a 3D player body and first-person fists, basic idle/movement/mining animations, terrain-item icons, selection/mining feedback, hotbar/inventory UI and the visibly inactive crafting area. Use a consistent lighting reference. No structure kit, held tools, machine assets or creatures are required for this step.

The initial player must be an inspectable 3D model, not only an invisible collision capsule or disconnected floating hand placeholders. A reusable simple rig and restrained animation set are sufficient for concept review. Check first-person clipping, fist reach/readability, body proportions and contact with the terrain; graphical pose must remain independent of authoritative movement/mining. Changeable skins are now required by the user; body-shape customization, combat animation and a production-wide character catalogue remain later decisions.

After this slice is reviewed, later selected work can extend the same visual language to tools, chest/workbench/furnace, boiler-engine, alternator, crusher, pipes/cables and creatures. A block is recognizable from silhouette/texture at normal targeting distance. Later port functions use shape, icon and direction alongside colour.

Initial terrain texture density is 32 texels per block face; test readability at 32 and 64 before finalizing. Machines can use more detail near controls, but do not mix unrelated density/material styles by accident. Record selected palette, roughness ranges, edge treatment and lighting reference in the first kit review. Steam devices emphasize readable moving parts and gauges; Gatebuilder devices keep a distinct shape language.

Start with opaque/cutout materials and a small shared material set. Water/transparent geometry uses its own path. Texture arrays/atlases are implementation choices validated with the mesher; faces with different light/material/UV requirements cannot be merged merely because both are solid.

Initial working budgets, to validate: ordinary machine under 5,000 visible triangles at close range, small creature under 10,000, shared texture sheets where practical, simplified distant presentation and no per-pipe authoritative moving mesh requirement. These are review triggers, not reasons to remove necessary visual feedback. Profile the whole representative scene, including shadows, animation and overdraw, before adopting final asset budgets.

## 3. Import and content definition contract

An asset definition records stable content ID, display/localization keys, occupied cells, model reference, rotations, ports, collision category, item/drop rules and any animation/state mappings. Runtime numerical IDs are assigned from a saved registry mapping; renaming a file does not change saved identity.

Imported models have consistent units, axis orientation, pivots, material slots and animation names. Do not derive machine logic from arbitrary child-object order. Port markers may be authoring helpers; the validated definition supplies authoritative coordinates and compatibility.

State presentation supports off/ready/running/blocked with reason-specific feedback. A missing texture/model uses a visible development placeholder without deleting the underlying saved content. Missing content definitions in a release save produce a compatibility error/recovery route, not silent substitution of stone.

## 4. Binary assets and reproducibility

When binaries are first introduced, configure Git LFS for editable `.blend` files and large art/audio sources before committing them. Keep scripts, definitions and Unity `.meta` files in ordinary Git. Ignore generated caches/build products such as `Library/`, `Temp/` and machine-local logs; never ignore `.meta` files belonging to tracked assets. Verify the chosen remote's LFS availability before relying on it; exact storage quota and team accounts are external facts to establish at that time.

Pin Unity and package versions in project metadata. A fresh checkout must import using the chosen Editor with no undocumented manual scene repair. Keep credentials, local executable paths and editor state out of shared project settings. Record third-party asset source/license/attribution with the asset; do not assume an integration-generated file establishes redistribution rights.

## 5. Acceptance and tool readiness

For the first-step player, round-trip Blender source -> export -> Unity import; verify scale against the 1 m terrain grid, orientation, rig/animation, first-person visibility and material appearance. Change the source and repeat import without losing asset identity. Review terrain, player, lighting and inventory together in a built player and verify a fresh checkout imports the kit. Record screenshots and in-motion observations, the selected visual direction and remaining issues; an isolated concept image does not establish in-game readability. The exact palette/model style is deliberately chosen during this step, not claimed finalized by this specification.

When machines are selected later, apply the same round-trip checks to grid fit, rotations, port alignment, collision bounds, material appearance and state animation.

Unity/Blender startup uses the local skill linked from `AGENTS.md`. A working executable, an open editor and a responsive integration are separate checks. Live integrations can improve iteration but are not runtime dependencies of the shipped game. Tool installation and project creation happen only as part of their authorized tasks.

## 6. Player skin authoring contract

Player-facing behaviour is owned by [GAMEPLAY.md](GAMEPLAY.md#15-player-skins). Build male and female player models around a compatible reusable rig and one documented skin texture layout. Model selection changes the visual body; texture selection changes its skin. Both variants must support the same skin regions and gameplay dimensions. Verify animation deformation and UV stretching on both. Skin colours, face details, painted hair and clothing patterns must be replaceable without editing the mesh, animation or gameplay definitions. Separate left/right limb regions so asymmetric designs work. First-person arms/hands use the same skin regions as the body, even if presentation uses a separate mesh.

The generated explorer-builder concept is one proposed appearance, not a locked identity or mandatory teal waistcoat for all players. Revisit its sculpted hair, raised clothing and pouch details before modeling: texture replacement cannot remove a protruding silhouette. Keep the base silhouette sufficiently neutral for different painted outfits; additional hairstyles, garment meshes and accessories would be separate customization features.

The user's accepted female direction uses longer hair and a modestly different figure while retaining practical clothing and a capable stance. Use the current low-ponytail concept as the visual reference; avoid exaggerated proportions or stereotyped costume differences. The male stays on the left and female on the right of comparison sheets, with matching front/back coverage and equal scale. Longer hair is actual silhouette geometry in the female model, not something a texture skin alone can add/remove; include it in the character geometry budget and check back/shoulder clipping during animation.

Provide an original Rivet Reach skin template and preview it on the model before fixing its layout. PNG is the working interchange proposal; exact resolution, UV layout, filtering and optional overlay-layer support remain to be selected through the player visual review. Familiar skin customization does not imply compatibility with Minecraft skin files. Do not advertise direct compatibility without a separately selected and tested mapping.

The initial two test skins must differ in face/skin colours and clothing and be tested on both male and female models so body/first-person mismatches are easy to detect. Verify seams, left/right orientation, hand detail, bending at joints and unchanged collision/mining behaviour. If custom-file import is selected for the first slice, reject invalid formats/dimensions or excessive decoded sizes and preserve the last valid selection. Base-body transparency must not make the player disappear; optional transparent overlays require their own explicit format rule.


## 7. Player geometry and performance review

The user likes the stylized player concept and requires male/female choices with changeable skins, while emphasizing smooth performance. Preserve the broad angular shapes and practical clothing direction. The generated illustrations show an art direction only: painted facets do not establish a mesh's triangle count, material cost or measured performance.

Initial working review budgets are at most 5,000 triangles for one full-body model including hair/clothing and at most 1,500 for a separate pair of first-person arms/hands, if separate geometry is needed. These are provisional implementation targets, not user-selected numbers or validated hardware guarantees. Keep most seams, buttons and clothing wear in the skin texture; use geometry where silhouette or joint motion needs it. Start with one shared material definition for the body/skin, minimal skinned renderers and restrained bone influences; measure actual draw calls and animation cost after import.

Only the chosen model is rendered/animated for a player; the unused male/female variant must not run a second hidden animator. Keep first-person visibility masks consistent so the same arms are not rendered twice. Count the total geometry actually rendered when the body is visible alongside separate first-person hands. Shared rig conventions do not remove the need to test each model's shoulders, elbows, hands, hips and knees during mining/movement.

Record imported triangles, bones, material/submesh counts, texture memory, draw calls and CPU/GPU frame cost in a representative Windows player build while terrain streams. Compare the two models and both test skins under the same workload. A later multiplayer crowd test is separate evidence; meeting a character triangle budget alone does not prove smooth terrain, animation or whole-game performance. Reduce distant character detail and animation work when that later workload requires it.

## Blender reference-to-runtime review

The user rejected the original first-POC player as too crude relative to the approved concept. Use [the Blender game-art skill](skills/blender-game-art/SKILL.md) for further asset work. Review actual front/back source renders, then the same FBX in Unity’s appearance screen and first-person pose. The completed rebuild uses a shared 40-bone skeleton, weighted joints, articulated fingers and eight authored actions, while preserving the one-material contract and skin palette semantics. `Tools/create_player_assets.py` owns the rig/action definitions; `AvatarView` blends the imported clips without runtime wrist-aim overrides. The [earlier asset handoff](PLAYER_ASSET_HANDOFF.md) records the preceding interim state. Do not claim artistic acceptance from triangle counts. Evidence and remaining limits are in [the player rebuild report](verification/PLAYER_REBUILD_RESULTS.md).
