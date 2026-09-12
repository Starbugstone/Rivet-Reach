# Player avatar and held equipment rework

Working revision under the user's 2026-09-10 request for a more polished 3D avatar, detailed hands and improved item/weapon handling. The user authorized original Blender model and animation changes and artistic liberties. This request supersedes the earlier player handoff reservation for the character files changed here. Gameplay dimensions, appearance selection and skin behavior remain governed by [GAMEPLAY.md](GAMEPLAY.md#15-player-skins).

## Appearance and construction

Both explorers retain the practical waistcoat, rolled linen sleeves, boots and distinct hair silhouettes of the [reference](concept-art/README.md). The revision reshapes sleeve/trouser volumes and facial surfaces, adds garment construction and boot lacing, and sculpts the dorsal hand and forearm. Fitted fingerless leather work gloves are a working artistic choice under the user's permission to take liberties; the finger joints and nails remain visible. Gloves change colour with the chosen skin and provide no armor or gameplay benefit.

The existing three finger joints and two thumb joints remain in the shared 50-bone import, including two nondeforming sockets. Finger wraps are fitted to the evaluated geometry around a 38 mm diameter shaft envelope. The palm block socket is raised 1.5 mm for glove thickness. Both source and imported poses require inspection; automated distances do not establish universal absence of intersection or artistic acceptance.

Sources remain editable under `ArtSource/Characters`. [rework_player_avatar.py](../Tools/rework_player_avatar.py) updates the current source idempotently and exports the rig and animation library with existing FBX identities. The baseline [create_player_assets.py](../Tools/create_player_assets.py) invokes the same sculpture stage, so a fresh regeneration includes the revision. Its `--from-backup` option is only a local iteration convenience, not a checkout dependency.

## First-person presentation

Seven grip families cover empty hand, palm-supported block, blade/torch, two-handed pickaxe, axe, shovel and hoe. The 50 authored clips include separate anticipation, contact and recovery paths for the tool families. Unity evaluates imported clips through a masked animation graph with separate rest, ready and strike layers. Tools rest horizontally across the lower view, visibly clear of the aim point. Attacking fades into the vertical ready/strike pose (25 ms half-life) and returns to horizontal after the authored recovery (80 ms half-life). Holding the bound Use button with a blade maintains the vertical guard presentation. This is a presentation stance; it does not introduce damage blocking. Blocks and buckets retain the palm-supported tray pose. This horizontal-rest/vertical-use behavior follows the user’s explicit follow-up request. The tool strike rotates the upper arm, elbow and attached hand/prop forward and down around the shoulder, with an opposite wind-up and complete recovery; it combines this angular arc with the hand path. The hoe head faces forward from the shaft. Both hands remain attached to the same rig. A calibrated two-bone support solve closes the pickaxe grip after blending; it follows the authored handle trajectory and prevents the left palm from sliding between otherwise correct endpoint poses. Imported bone units and axes are calibrated from the authored ready grip, and the solve runs in model space to preserve FOV compensation.

The first-person wrist targets sit farther forward and lower in the frame. The former special axe translation/roll and the support-clavicle lowering offset are removed. The whole assembly retains the existing FOV compensation and restrained movement/look sway. The full-body pose, collision capsule, aim ray, interaction distance and selected inventory authority remain separate.

Changing a selected item lowers the old assembly over 90 ms, exchanges the displayed stack below the view, then raises it over 140 ms. Repeated selection changes converge on the latest selection. The inventory selection still changes immediately and owns gameplay; the cached stack is presentation only. Selecting a different grip cancels its old strike blend. Items remain attached during the visible recovery rather than appearing at a grip-weight threshold.

The tool arm winds back 18 degrees at the shoulder, then rotates forward/down through a 48–52 degree follow-through, depending on the tool. The elbow and wrist descend together, retaining the solved elbow bend; there is no compensating shoulder lift to hold the elbow in view. The tool can dip below the lower frame at the end of the strike before recovering to its visible horizontal rest. These angular defaults are working implementation choices under the user’s explicit request for a whole-arm swinging stroke.

Source durations remain 0.3 s for the empty/block sweep and 0.6 s for tools. Runtime playback is 1.35× for empty/block, 1.55× for blades, 1.2× for axes and 1.05× for pickaxe/shovel/hoe. These are working feel choices for user review, replacing the prior blanket 2.5× playback. Locomotion timing and mining/combat resource rules are unchanged.

## Held models

[rework_held_tools.py](../Tools/rework_held_tools.py) authors the dagger/sword, pickaxe, shovel and hoe with shaped metal sections, edge bevels, ferrules and bound grips. The existing authored axe uses its blade/handle markers to keep its edge fixed to the grip throughout the arc, including when the shaft passes the camera direction. All current tiers use the corresponding model with the registered tint; this does not add tool tiers or new weapon capabilities.

[create_held_utility_items.py](../Tools/create_held_utility_items.py) authors a 3D torch and a double-wall bucket. The torch follows the shaft grip and uses an emissive flame; the bucket rests on the palm, with a water surface when filled. Placed torch lighting and authoritative bucket transactions remain in their existing systems. The later [equipment art revision](EQUIPMENT_ART.md) adds matching 3D ingots and armor items, fitted armor on both explorers and first-person bracers. Other small resources/food retain their respective presentation.

## Skin and cost contract

The later [eating presentation](GAMEPLAY.md#eating-presentation--2026-09-12) adds a camera-relative right-arm pose over the imported block grip. It uses the existing food art and socket, with a limb-length-preserving solve during the consumption timer. [Eating verification](verification/EATING_RESULTS.md) owns the later candidate; the dated avatar measurements below remain specific to their original build.

The [content pipeline](CONTENT_PIPELINE.md#6-player-skin-authoring-contract) retains a single material, one semantic UV set and at most four influences. The right edge of each hand tile (`u > .953` within that tile) now reserves a leather swatch for gloves; the cylindrical skin UVs remain below `.94`, and nail swatches retain their existing lower-left patches. The packed surface map marks the leather as nonmetallic and non-skin.

Working review ceilings for this revision are 90,000 full-body triangles, 34,000 per dominant first-person arm and 68,000 per pair. These are review triggers selected during implementation, not user-chosen performance guarantees. Visible body geometry and the full-body shadow remain separate costs. See [verification](verification/AVATAR_REWORK_RESULTS.md) for measured imports, rendering evidence and limits.

## Reproduction

Use Blender 5.2 background mode with absolute Windows script paths. To update existing sources, run `create_skin_materials.py`, `rework_player_avatar.py`, `rework_held_tools.py` and `create_held_utility_items.py`. Run `check_player_assets.py` and `check_grip_contacts.py`; render `render_player_review.py -- --eevee` and `render_grip_review.py -- --eevee` for actual source comparisons.

[Build-AvatarRework.ps1](../Tools/Build-AvatarRework.ps1) uses the pinned Unity 6000.4.4f1 Editor and writes `Builds/AvatarRework/RivetReach.exe`. It reuses an open Editor with a separate local request file, avoids active Play/build sessions and preserves the user's scene. [Verify-AvatarRework.ps1](../Tools/Verify-AvatarRework.ps1) checks imported characters and animation; add `-Gameplay` for the live equipment review. The ordinary [POC verification](../Tools/Verify-POC.ps1) accepts this executable through `-Executable`.
