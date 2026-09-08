# Rivet Reach - Content and Asset Workflow

> **Status:** working specification, 2026-09-08. Applies when asset production/implementation is requested. No assets, Unity scaffolding or integration packages are created by this document.

## 1. Scale and source ownership

Use 1 metre per voxel and model modular assets against that grid. Blender working scenes use metric units at unit scale 1. Apply intended object scale before export. Export presets convert axes consistently; do not fix every imported machine with a different corrective rotation.

A block-sized machine's anchor is its lower grid corner, with its footprint explicitly recorded in the content definition. Reusable attachments use a named connector pivot. Visual meshes can extend within their declared bounds but cannot invent solid occupied cells absent from gameplay data. Decorative animation must not move authoritative ports or collision unexpectedly.

Planned source layout: `ArtSource/` for editable Blender/image/audio originals; `Assets/RivetReach/` for imported runtime content; `Packages/` for pinned dependencies; `ProjectSettings/` for Unity settings. Specifications remain in `.docs/`. These paths are a future convention, not a request to create empty directories now.

Keep `.blend` source separate from Unity's imported runtime hierarchy. Publish explicit interchange exports with documented settings so opening a project does not depend on every contributor's local Blender executable or personal defaults. FBX is the initial mesh/animation interchange choice; texture outputs use lossless source images and Unity import compression settings selected per use.

## 2. First visual kit

The locked first step requires a cohesive **terrain/player/inventory visual concept**, as scoped in [DEVELOPMENT_STRATEGY.md](DEVELOPMENT_STRATEGY.md#6-locked-first-step-and-provisional-later-sequence). Its kit contains a small natural terrain palette, a 3D player body and first-person fists, basic idle/movement/mining animations, terrain-item icons, selection/mining feedback, hotbar/inventory UI and the visibly inactive crafting area. Use a consistent lighting reference. No structure kit, held tools, machine assets or creatures are required for this step.

The initial player must be an inspectable 3D model, not only an invisible collision capsule or disconnected floating hand placeholders. A reusable simple rig and restrained animation set are sufficient for concept review. Check first-person clipping, fist reach/readability, body proportions and contact with the terrain; graphical pose must remain independent of authoritative movement/mining. Final customization, combat animation and a production-wide character catalogue are later decisions.

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
