# Player asset ownership handoff — 2026-09-08

The user clarified during the terrain/fog/placement revision that another agent owns actual player 3D modeling and animation. That work remains with the model agent. The current terrain/placement change does not commit the interim model sources/exports listed below and makes no final artistic acceptance claim.

## Local interim files

A reference-based pass was authored before that clarification. These working-tree files are left available for the model agent to inspect, reuse or replace deliberately:

- `Tools/create_player_assets.py`
- `ArtSource/Characters/ExplorerMale.blend`, `ExplorerFemale.blend`, `asset-report.json`
- `Assets/RivetReach/Resources/Characters/ExplorerMale.fbx`, `ExplorerFemale.fbx`, `SkinField.png`, `SkinOchre.png`

An ignored local backup with SHA256 inventory is at `Logs/player-pass-handoff/`; it includes the generator, source/export files and studio-render helper as they stood at handoff. It is recovery material, not a source of runtime assets. Existing Unity metadata identities were preserved. The versioned [Blender review skill](skills/blender-game-art/SKILL.md) and `Tools/render_player_review.py` can be used with either the baseline or replacement models.

## Integration boundaries

The terrain agent changed `FirstPersonPlayer.cs` only for placement preview/input and camera range; it did not change `AvatarView.cs` animation. Preserve those placement changes when editing player animation. `GameUI.cs` now uses an aspect-preserving crop for portrait panels and a 768×1024 render texture. The PC pipeline uses four-sample MSAA. `ProjectBuild.cs` retains the existing model names and Generic rig import contract, adds bilinear skin filtering and builds into `Builds/FirstPOC-v2` to preserve the old running executable.

The model agent may revise the mesh/rig/skin contract as authorized, but must update dependent avatar extraction/poses, imports and documentation together. Do not blindly rerun the older committed generator over the replacement artwork. Read the actual source and coordinate ownership before regenerating assets in a shared checkout.

## Evidence boundary

[Revision 2 verification](verification/VISUAL_REVISION_RESULTS.md) passed 52 runtime checks and 22,371 domain assertions, using the local interim art. Its images and geometry counts describe that tested configuration. The committed baseline player assets remain the original first-POC versions until the model agent lands their changes. Rebuild, rerun relevant checks and capture final body/skin/first-person views after integrating the replacement models and animation. User review of the player remains pending.
