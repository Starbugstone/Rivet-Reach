# Fitted armor and ingot presentation

Working implementation under the user's 2026-09-12 request to bring ingots, armor, held items and the equipped 3D avatar into the established visual style. This authorizes fitted armor on both explorers, superseding the earlier visual deferral. [Gameplay](GAMEPLAY.md#survival-progression-farming-health-and-armor) retains equipment slots, recipes, protection, death recovery and save rules.

## Artwork

Original copper, iron and gold ingots use a tapered casting, beveled edges and a foundry stamp. Copper, iron and diamond armor share a workshop construction: open-face helmet, shaped breastplate, leather straps and bracers, articulated thigh/knee/shin plates and reinforced boots. Rolled edges, small fasteners and restrained metal palettes follow the existing equipment direction. The underlying explorer faces, waistcoat, linen sleeves, gloves and changeable skins remain recognizable. Numerical dimensions and palette values are working art decisions for visual review.

`Tools/create_armor_assets.py` reads the current character sources without changing them, authors separate fitted meshes for male/female proportions and exports `Resources/Equipment/MaleArmor.fbx` and `FemaleArmor.fbx`. The editable combined source is `ArtSource/Equipment/Equipment.blend`; its character copies are fit references. Explicit FBX exports mean playing does not require Blender. Texture paths in the saved source are relative.

Inventory icons and the five static item meshes come from the same armor/ingot artwork. All three armor tiers share those item meshes; worn armor uses the corresponding fitted variant. The shared palette changes by tier. Each worn slot is a single skinned mesh/material; the original character mesh, rig, animations and GUIDs remain unchanged.

## Runtime contract

`EquipmentVisuals` owns the item-model, icon and palette mapping for three ingots and twelve existing armor items. The inventory/hotbar/browser, palm-held objects and world drops consume this mapping. No flat armor/ingot cards remain in these paths.

`AvatarEquipment` rebinds imported armor to the explorer's existing animated bones. Each equipped slot selects its registered material, supporting mixed tiers. Revision-driven synchronization updates the body, first-person bracers and inventory portrait after equipment changes. Appearance rebuilds bind the same equipment authority to the new model/skin. Helmet-covered crown hair is hidden while equipped and restored on removal; lower hair remains visible. First-person body armor retains a full armor shadow while omitting the head and duplicate arms from the visible body. Knee guards blend across the existing thigh/shin joint to retain coverage during crouching. Bracers follow the dominant arm and the existing pickaxe support-hand visibility.

Armor is presentation derived from `EquipmentState`; it adds no save fields, hitbox changes, inventory authority, damage rules or extra Animator. Existing armor persistence and death removal drive the visual state. World materials and imported mesh data are shared; first-person materials and extracted body/arm meshes belong to the avatar and are released on rebuild/destruction.

## Reproduction and evidence

Run the authoring script in Blender 5.2 background mode. In Unity 6000.4.4f1 use **Rivet Reach → Prepare armor and ingot art**, then **Build armor and ingot art review**. An idle, refreshed Editor also accepts `build` in `Logs/equipment-art-request.txt`; the result is recorded in `Logs/ArmorArt/result.txt`. This creates `Builds/EquipmentArt/RivetReach.exe`.

Run `Tools/Verify-EquipmentArt.ps1` for the native-player review. [Equipment verification](verification/EQUIPMENT_ART_RESULTS.md) records the measured import/build/runtime checks, source renders and remaining limits. Refresh the [wiki export](WIKI_AUTHORING.md#refresh-the-reference) after changing artwork.
