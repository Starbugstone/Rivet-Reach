# Player asset integration contract

The earlier player-model handoff is complete; its intermediate exports and reservations are preserved in Git history. The user directly authorized the 2026-09-10 [avatar rework](AVATAR_REWORK.md), which owns the revised sources and animation in the current branch. Preserve concurrent terrain/industry work, placement, input and portrait rendering. Establish current ownership before overlapping later character changes.

The runtime explorers are the [second generation](EXPLORER_V2.md): `ArtSource/Characters/V2/*.blend` and `Assets/RivetReach/Resources/Characters/V2`, authored by `Tools/create_explorer_v2.py`. That generator reads, but never writes, the first-generation `ArtSource/Characters/ExplorerMale.blend` and `ExplorerFemale.blend`. Those remain authored by `Tools/create_player_assets.py` and its refinement scripts, with their FBX, skins and maps under `Assets/RivetReach/Resources/Characters`, for reverting. Preserve asset GUIDs and update source, export, import, avatar extraction and documentation together. Do not run an older generator over newer artwork.

[CONTENT_PIPELINE.md](CONTENT_PIPELINE.md#6-player-skin-authoring-contract) owns the model/skin contract; [current avatar verification](verification/AVATAR_REWORK_RESULTS.md) records actual Blender/Unity evidence and limitations. Male/female appearances and both skins use identical gameplay dimensions. Health and equipment remain independent of appearance.

Use the [Blender game-art workflow](skills/blender-game-art/SKILL.md) for actual model work and compare the final source render and Unity import with the approved concept. Visual review and geometric checks do not establish user acceptance or a hardware-independent performance guarantee.
