# Avatar and held-equipment rework verification

Working review candidate dated 2026-09-10. [AVATAR_REWORK.md](../AVATAR_REWORK.md) owns the selected construction, pose, timing and reproduction rules. This report records actual source/import evidence, not a claim of final artistic acceptance.

## Source revision

Both editable explorer sources contain reshaped anatomy/clothing, constructed seams, boot lacing and fitted fingerless work gloves. The shared library has 50 actions and 50 rig bones. Original dagger/pickaxe sources are revised; shovel/hoe and held torch/bucket sources are added. Existing character and tool FBX GUIDs are preserved. No third-party assets or runtime dependencies are introduced.

The final source models measure **88,182 male / 89,622 female triangles**, one material, one semantic UV set and at most four influences. The revised geometry stays within the documented working review ceilings. The larger count includes fitted glove overlays; it is not a GPU performance result.

The evaluated contact suite checks block support, shaft clearance, dominant wrist flexion and pickaxe support-hand contact across the authored clips on both models. Source checks cover normalized weights, relative texture paths, finite sampled deformation, loop seams and crouch headroom. These checks do not find every possible surface intersection.

## Unity and gameplay evidence

The pinned **Unity 6000.4.4f1** development build completed at **2026-09-10 14:38:35 UTC**, with **0 errors / 0 warnings**. The [build summary](avatar-rework-2026-09-10-build.txt) and [SHA-256 manifest](avatar-rework-2026-09-10-manifest.json) identify the executable, compiled gameplay assembly and assets. Run `Builds/AvatarRework/RivetReach.exe` locally.

The [studio suite](avatar-rework-2026-09-10-studio-report.json) passed **133 checks**. The [live gameplay suite](avatar-rework-2026-09-10-gameplay-report.json) passed **1,368 assertions**, including sampled attachment checks, for both models and both skins. Coverage includes seven grip families, horizontal rest, vertical weapon guard through the bound Use input, forward/down rotational strikes, fixed axe edge orientation, the corrected forward-facing hoe, rapid selection changes, empty-stack removal, torch/buckets, and 60/78/100-degree FOV. Appearance/equip settling is measured in capped animation steps so asset warm-up cannot turn a wall-clock delay into too few animation updates.

The final tool strokes rotate from the shoulder. Source checks explicitly require **both elbow and hand to descend** from anticipation to contact on all ten body/first-person tool actions per model. The complete arm lowers through contact and follow-through; the tool head reaches the lower frame before recovery. There is no raised-elbow framing compensation. Compare the actual [Blender wind-up](avatar-rework-2026-09-10-blender-windup.png) and [downstroke](avatar-rework-2026-09-10-blender-downstroke.png) with the [Unity wind-up](avatar-rework-2026-09-10-action-pickaxe.png), [Unity downstroke](avatar-rework-2026-09-10-downstroke-pickaxe.png) and [Unity motion preview](avatar-rework-2026-09-10-preview.mp4). The preview contains 180 captured frames, encoded at 60 fps without generated/interpolated frames; capture uses a fixed 1/60-second simulation step and is excluded from timing measurements.

| Measured workload | Result |
| --- | --- |
| Imported male / female body | 88,182 / 89,622 triangles; 50 bones / 50 clips |
| Dominant first-person arm / maximum visible arm pair | 29,707 / 64,900 triangles |
| Visible first-person body / maximum full-body shadow | 11,636 / 89,622 additional triangles |
| 1,000 paired body/arms pickaxe animation evaluations after warm-up | 0.1053 ms mean / 0.1211 ms p95; main thread, excludes terrain and rendering |
| Live view-radius-4 scene, 1280×720, 60 fps cap | 16.6670 ms median / 16.6713 ms p95; shared workstation |
| Live maximum pickaxe support contact error, first person / body | 0.0058 / 0.0038 mm |
| Live maximum body support wrist flexion | 23.29 degrees |
| Source maximum dominant / support wrist flexion across 68 grip combinations | 29.26 / 0.35 degrees |

The [source deformation checks](avatar-rework-2026-09-10-source-checks.json) passed for all 50 actions on each model. The [68 evaluated grip combinations](avatar-rework-2026-09-10-grip-contacts.json) passed, with maximum source support error below 0.001 mm and measured shaft-envelope penetration below 0.48 mm. These distances describe the sampled contact probes, not every mesh surface.

Actual asset comparisons: [Blender front](avatar-rework-2026-09-10-blender-front.png), [back](avatar-rework-2026-09-10-blender-back.png), [hand detail](avatar-rework-2026-09-10-blender-hand.png); [Unity front](avatar-rework-2026-09-10-unity-front.png), [back](avatar-rework-2026-09-10-unity-back.png), [hands](avatar-rework-2026-09-10-unity-hands.png). Gameplay: [resting pickaxe](avatar-rework-2026-09-10-rest-pickaxe.png), [axe](avatar-rework-2026-09-10-rest-axe.png), [hoe](avatar-rework-2026-09-10-rest-hoe.png), [blade](avatar-rework-2026-09-10-rest-sword.png), [guard](avatar-rework-2026-09-10-weapon-guard.png), [alternate skin](avatar-rework-2026-09-10-skin-ochre.png), [bucket](avatar-rework-2026-09-10-bucket.png), [torch](avatar-rework-2026-09-10-torch.png).

The separate [general POC regression](avatar-rework-2026-09-10-regression-report.json), completed at 2026-09-10T14:42:34.9318963Z, **failed after 65 passed assertions** at `Terrain demand drained with safe player residency` during later traversal. Startup, visual framing, inventory, mining, placement and item-pile checks ran before that failure. The view-radius-10 terrain demand did not drain within its 60-second bound. This recurring broader failure was also observed on earlier candidates; its cause is not established by the avatar suite, and the full POC regression is not claimed to pass.

## Remaining review

The full-body character retains the stylized explorer direction rather than changing to a photorealistic character. Gloves, garment detail, tool profiles, placement and motion timing are working artistic choices under the user's permission to take liberties. The user retains visual and feel acceptance.

The live gameplay sample and paired animation benchmark cover their stated workload on this workstation. They do not establish crowd performance, GPU cost across hardware, absence of every pose intersection or zero foot sliding on every slope. Body shadows remain an additional geometry cost. Armor still uses its existing presentation; fitted armor meshes remain separate scope. Other small food/resources retain their existing item presentation.
