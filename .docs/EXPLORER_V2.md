# Second-generation explorers (machinist rework)

Working revision under the user's 2026-10-02 request for a much more polished player appearance that no longer looks out of place beside the machinery. The user asked for the 3D model to be redone while keeping the previous one for a possible revert. First-person arms are the priority, and the inventory portrait shows the full figure. Both explorers must keep equipping held items and fitted armor. The user also asked for separately chosen hair, cloth and skin colours, to be exposed later by a fuller customisation UI. Selection rules remain in [GAMEPLAY.md](GAMEPLAY.md#15-player-skins); the [content pipeline](CONTENT_PIPELINE.md#6-player-skin-authoring-contract) owns the reusable skin contract.

## Art direction

The machines share one palette atlas: dark blue-steel panels, copper and brass fittings with rivets, warm leather and teal accents, built from bevelled hard-surface forms. The first-generation explorers used soft, clay-like garments in flat cream, teal and charcoal tiles, so they read as a different art set. The second generation keeps the concept identity and the male/female silhouettes, including the waistcoat, rolled sleeves, work trousers, boots and the female low ponytail. These working choices restyle that identity as machinist workwear:

- **Arms (first person):** rolled canvas sleeves with turned bands, stitched seams and a brass-buttoned roll-up tab. The fingerless leather work gloves have stitched panels, a riveted knuckle guard, a brass-buckled wrist strap and a thumb hole that leaves the verified finger and thumb contact skin uncovered.
- **Torso:** a steel-blue waistcoat with clean panel construction, turned hems, copper piping on the front, neckline and hem, brass rivet buttons, riveted flap pockets and a rear cinch buckle. The shirt has a band collar and placket.
- **Lower body:** a leather tool belt with a brass buckle, loops and two snap pouches. The trousers have side seams and riveted double-knee panels. Laced work boots have welted soles, a padded collar, an ankle strap and copper toe caps.
- **Head:** a rebuilt face and neck with recessed layered eyes, lids and lashes, sculpted ears, and new hair. The male has a swept quiff. The female has a side-swept fringe, face-framing locks and a gathered ponytail. Both wear brass-rimmed teal goggles pushed up onto the forehead.

The goggles, pouches and straps add silhouette that a texture cannot remove. They are accepted as part of this proposed appearance; making them optional cosmetics is recorded in [design questions](DESIGN_QUESTIONS.md#explorer-appearance-generation-2). Gameplay dimensions, collision, reach, eye height, movement and capabilities do not change.

## Construction contract

[create_explorer_v2.py](../Tools/create_explorer_v2.py) authors both bodies with [explorer_v2_geometry.py](../Tools/explorer_v2_geometry.py) and paints the shared maps with [explorer_v2_textures.py](../Tools/explorer_v2_textures.py). It reads the first-generation `.blend` sources without modifying them. It keeps the exact 50-bone rig, the `BlockSocket`/`ToolSocket` frames, all 50 authored clips, the sculpted forearm/hand skin and the nails. Grip calibration, FOV compensation, two-hand support solving and armor binding therefore keep their existing inputs.

Garment panels are Coons patches over a polar superellipse torso surface, so hems, armholes and the V-neck are clean boundaries with turned rims, not jagged cuts. The trousers are one continuous pelvis-to-leg mesh. Boots combine a vertical shaft with a D-section foot loft. Skin hidden inside the gloves is removed, and the glove shell is decimated, keeping both bodies below the first generation's triangle counts. Weights are normalized with at most four influences.

Exports keep one material and the single `SkinUV` set. They add a per-model `Occlusion` vertex colour baked in Cycles with the limbs spread apart, so creases darken without baking limb contact. Sources live in `ArtSource/Characters/V2`; runtime FBX files, maps and the palette live in `Assets/RivetReach/Resources/Characters/V2`.

## Layered appearance

Both bodies share one parametric UV layout, so one set of maps fits either model. The 2048 px atlas keeps the first generation's 4×4 semantic tiles (head skin, shirt, sleeves, forearm/hand skin, trousers, leather, hair, eyes, brass, waistcoat, iris, copper). Each tile splits into four **tint cells**, giving an 8×8 grid.

- `SkinBase.png` stores near-white modulation (stitches, twill, leather grain, hair strands, lips, iris fibres and nails) in tinted cells, and finished colour in fixed cells (eye whites, lenses, soles and dark details).
- `SkinSurface.png` packs metallic, roughness and the skin mask. `SkinNormal.png` holds the matching stitch, weave and grain relief.
- [AppearancePalette.json](../Assets/RivetReach/Resources/Characters/V2/AppearancePalette.json) assigns every cell to a layer: skin, hair, eyes, shirt, waistcoat, trousers, leather, boots, brass, copper, accent or fixed. It also lists palettes and the two current presets.

At runtime `AvatarAppearance` builds an 8×8 point-sampled tint texture per preset; the shader multiplies the base map by the cell's tint. Hair, skin, eye and cloth colours are therefore independent inputs. A future customisation UI can combine palette entries, or extra base maps, without repainting textures or editing the mesh. The current appearance menu still offers two skins. **Foundry / Steel** (skin 0) and **Verdigris / Ochre** (skin 1) differ in skin tone, hair, eyes and all clothing colours. They keep the saved 0–1 skin index, so presets beyond two require an explicit save-compatibility change.

## Runtime and revert

`AvatarAppearance.Generation` selects the explorer set (currently `2`). Setting it to `1` restores the first-generation models with `SkinField`/`SkinOchre` and the original hair-under-helmet rule. Those assets, sources and generators remain untouched. `ExplorerSkin` gains a tint map, full-surface detail normals and vertex occlusion, all with neutral defaults. The first-generation material therefore renders exactly as before.

Helmets hide second-generation triangles above the brow when every corner lies in the hair, goggle-strap, lens, brass or copper cells. Eye whites and lashes remain visible, and lower female locks continue below the helmet. Fitted armor still binds to the live explorer bones. [Verification](verification/EXPLORER_V2_RESULTS.md) records the measured fit and remaining limits.

## Reproduction

Run Blender 5.2 in background mode with the absolute script path:

```bash
blender --background --factory-startup --python Tools/create_explorer_v2.py
blender --background --factory-startup --python Tools/check_player_assets.py -- --v2
blender --background --factory-startup --python Tools/check_grip_contacts.py -- --v2
blender --background --factory-startup --python Tools/render_explorer_v2.py -- --views front,back,quarter,face,hands,fp
```

`--preview` skips painting, baking and export, for fast geometry iteration; `--male-only` and `--female-only` limit the build. [Build-AvatarRework.ps1](../Tools/Build-AvatarRework.ps1) applies the import contract (`ExplorerV2Assets.Prepare`) and builds the review player. [Verify-AvatarRework.ps1](../Tools/Verify-AvatarRework.ps1) and [Verify-EquipmentArt.ps1](../Tools/Verify-EquipmentArt.ps1) exercise the imported explorers, held items and armor.
