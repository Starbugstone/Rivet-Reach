# Second-generation explorer verification

Working review candidate dated 2026-10-02. [EXPLORER_V2.md](../EXPLORER_V2.md) owns the design, construction, layered-appearance and revert rules. This report records actual source, import and in-game evidence. It does not claim final artistic acceptance, which remains with the user.

## Source checks (Blender 5.2.0 LTS)

[create_explorer_v2.py](../../Tools/create_explorer_v2.py) authored both bodies from the unchanged first-generation rig, clips and hand skin. The first-generation `.blend`, FBX, skin PNG and meta files remain byte-identical to the previous commit.

- **Triangles:** **82,032 male / 85,211 female**, compared with 88,182 / 89,622 for the first generation.
- **Contract:** 50 bones (48 deforming plus 2 sockets), 50 clips, one material, the single `SkinUV` set and at most four influences.
- **Occlusion:** a baked `Occlusion` vertex colour per body.
- **[Source checks](explorer-v2-2026-10-02-source-checks.json):** normalized weights, relative texture paths, a single UV set, finite deformation across sampled frames of all 50 actions, loop seams and crouch headroom all pass. The maximum sampled animated edge is 0.18 m.
- **[Grip contacts](explorer-v2-2026-10-02-grip-contacts.json):** all **68 evaluated grip combinations** pass.
  - Maximum shaft or block contact penetration is **0.69 mm**, under the 1 mm limit.
  - Maximum dominant wrist flexion is 29.25°, and maximum support wrist flexion is 0.35°.
  - Maximum pickaxe support-contact error is below 0.001 mm.
  - Gloves leave the digits bare and use a thinner palm side, which keeps the verified finger and thumb contact skin in place.

A back-face-culled Blender review found and corrected inverted pieces, and a skin-weight seam that opened at the trouser seat. The generator now orients every connected piece automatically: closed pieces by signed volume, open shells by a decisive vote against the nearest bone. It also welds coincident garment vertices with averaged weights. These checks do not prove the absence of every intersection.

Source renders: [front](explorer-v2-2026-10-02-blender-front.png), [back](explorer-v2-2026-10-02-blender-back.png), [first-person arms](explorer-v2-2026-10-02-blender-first-person.png).

## Unity import and gameplay (6000.4.4f1)

`Tools/Build-AvatarRework.ps1` built `Builds/AvatarRework/RivetReach.exe` at **2026-10-02 20:51:48 UTC** with **0 errors / 0 warnings**. `ExplorerV2Assets.Prepare` applied the import contract. The [manifest](explorer-v2-2026-10-02-manifest.json) records SHA-256 hashes of the executable, the gameplay assembly, the runtime FBX/maps/palette and the editable sources. Measurements ran on a shared workstation (Intel Core i7-10750H, NVIDIA GeForce RTX 2060, Direct3D 11) at 1280×720.

| Suite | Result |
| --- | --- |
| [Studio import review](explorer-v2-2026-10-02-studio-report.json) (`Verify-AvatarRework.ps1`) | PASS, 133 checks: both imports, 50 clips, 50 bones, triangle budgets, poses, crouch/landing, both presets, grips, FOV framing |
| [Live gameplay review](explorer-v2-2026-10-02-gameplay-report.json) (`-Gameplay`) | PASS, 1,355 assertions: both bodies and presets, seven grip families, rest/strike/guard, selection transitions and attachments |
| [Equipment and armor review](explorer-v2-2026-10-02-equipment-report.json) (`Verify-EquipmentArt.ps1` on the same build) | PASS, 134 assertions: worn armor on both bodies, portrait, bracers, mixed tiers, held/dropped ingots and armor |

| Measured workload | Result |
| --- | --- |
| Imported male / female body | 82,032 / 85,211 triangles; 50 bones; 50 clips |
| Dominant first-person arm / maximum visible arm pair | 21,252 / 46,035 triangles |
| Visible first-person body / full-body shadow | 19,720 / 85,211 triangles |
| 1,000 paired body/arms pickaxe animation evaluations after warm-up | 0.124 / 0.133 ms mean, 0.170 / 0.187 ms p95 in two runs; main thread only |
| Live view-radius-4 scene, 60 fps cap | 16.668 ms median / 16.745 ms p95 frame interval |
| Live maximum pickaxe support-contact error, first person / body | 0.0056 / 0.0069 mm |

An earlier animation sample on this same build measured 1.55 ms mean while another project's Unity batch job held the CPU at 100%. It is retained as a contended outlier, not a representative cost. These frame figures describe a capped, shared machine. They do not establish the 0.1.0 steady-60 FPS target in busy factories.

In-game captures:
- [Front](explorer-v2-2026-10-02-unity-front.png), [back](explorer-v2-2026-10-02-unity-back.png) and [face](explorer-v2-2026-10-02-unity-face.png) with the Foundry / Steel preset, plus the [Verdigris / Ochre preset](explorer-v2-2026-10-02-unity-verdigris.png), which shows independently tinted skin, hair, eyes and clothing.
- First person: [pickaxe](explorer-v2-2026-10-02-first-person-pickaxe.png) and [Verdigris blade](explorer-v2-2026-10-02-first-person-blade-verdigris.png).
- [Fitted armor](explorer-v2-2026-10-02-armor.png) with helmets hiding goggles and crown hair, and the [inventory portrait](explorer-v2-2026-10-02-inventory-portrait.png).

## Not re-run and remaining review

The broad POC, Alpha-playtest and release suites were not re-run for this art change. Their appearance assertions are updated to accept the current generation. `ShaderEquivalenceChecks` was not run; the new `ExplorerSkin` inputs default to neutral values so the first-generation output is unchanged. No custom-skin import, presets beyond the two saved skin indices or new gameplay capabilities were added.

The Unity wiki item export was not re-run. Its source fingerprints were updated for the five changed avatar-presentation files and the new `AvatarAppearance.cs`. Exported item data, recipes and icons do not depend on these files. The committed catalog was checked against the committed file contents, and `publish_wiki.py` validation passed for 234 pages and 9,984 local links/images.

Faces, proportions, goggles, palette choices and the leather/canvas detail are working artistic choices. The user retains visual acceptance and may revert with `AvatarAppearance.Generation = 1`.
