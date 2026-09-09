# Continuous hands, character materials and audio revision

Working revision, 2026-09-08, following the user’s high-fidelity visuals, sounds, rig and animation request. Artistic acceptance remains with the user; “AAA” is a target, not a measured result.

Current model/hand/audio evidence is retained here. [Crafting](CRAFTING_RESULTS.md), [survival](SURVIVAL_RESULTS.md) and [terrain](TERRAIN_GENERATION_RESULTS.md) own their current interfaces and gameplay evidence. The dated reports below are the latest measurements for these character/audio workloads, not measurements of the current complete game.

## Changes

- One continuous closed skin surface per forearm/palm/five-finger assembly, replacing separate finger/thumb shells. Quad retopology and subdivision provide smooth silhouettes; transferred weights are relaxed across the anatomical joins.
- Three articulated joints per finger, two per thumb: 48 deforming bones plus the existing two attachment sockets. The 28 authored clips retain locomotion, crouch, landing, fist, block and tool poses, with updated distal curls, skinned nails and shallow knuckle details.
- Both body variants and both skins remain available. Shared 1024×1024 colour atlases, normal and material maps distinguish skin, cloth, leather and metal. Body and first-person hands use the same URP material response. Tool metal now has specular response.
- Original 48 kHz / 24-bit audio sources: 60 material footsteps/hits/breaks, 16 swing/equip/pickup/landing variants, and one stereo wind loop. Footsteps depend on distance and surface; mining impacts follow the swing, with spatial material break/placement sounds. Twelve reusable voices bound effects. Shelter attenuates/muffles wind. Settings exposes a persistent master volume.

## Source evidence

The source geometry measures 67,070 male and 68,610 female triangles. Both retain one material/UV set and at most four skinning influences. Each skin arm is one closed manifold component before the separate nail plates are attached.

The [source checks](hifi-source-checks.json) pass normalized weights, portable textures, finite sampled deformations, crouch headroom and loop endpoints for both models and all 28 clips. [Grip measurements](hifi-grip-contacts.json) check the evaluated palm/shaft contacts and both wrists throughout the authored grip actions. These checks are not a complete self-intersection detector.

## Unity evidence

The pinned Unity 6000.4.4f1 Windows development player builds with **zero errors and zero warnings**. The [final clean build](hifi-build.txt) reports 211,950,836 bytes. The three feature suites below pass, with no logged errors:

| Suite | Passed checks | Evidence |
| --- | ---: | --- |
| Character import, animation, appearance and grip transitions | 55 | [Avatar report](hifi-avatar-report.json) |
| Terrain, movement, inventory, placement, appearance and held items | 263 | [Runtime report](hifi-runtime-report.json) |
| Imported audio, variation, spatial settings, voice bound and immediate mute | 109 | [Audio report](hifi-audio-report.json) |

Audio checks cover all 77 imported clips, sample rates/channels, finite and unclipped sampled PCM, smooth effect endpoints, nonrepeating footsteps, twelve effect voices, disabled Doppler, spatial impacts and mute of already playing sources. They do not measure perceived mix quality or headphone/speaker translation.

Two consecutive ordinary Editor Play startups also [pass](hifi-editor-startup.txt), including both model/skin choices, animation graph initialization, movement, mining, standing/crouching look-down and held blocks. The smoke test now starts its second cycle after the exit transition settles and uses an explicit crouch fixture, matching its existing movement/mining fixtures, so Game-view focus cannot discard an injected key. Ordinary gameplay continues to read the bound crouch key; the standalone regression covers that input path.

At 1280×720, view radius 10 and a 90 fps cap, the measured terrain run on the i7-10750H / RTX 2060 workstation has **11.112 ms median / 11.470 ms p95** frame time, with 27.486 ms maximum during that sample. The separately sampled mining workload reaches 35.306 ms maximum; first spawn becomes ready in 9.375 seconds. These are development-player samples on a shared workstation, not a general performance guarantee. Draw-call counters were unavailable (`-1` in the raw report).

The paired body/hand animation benchmark measures 0.0896 ms mean / 0.1382 ms p95 for 1,000 warmed main-thread graph evaluations, excluding rendering and terrain. Motion/mining blend weights reach 90% in 83.33/25.00 ms respectively. Imported dominant first-person geometry is 22,598 male / 21,370 female triangles; the normal look-down body uses 8,544 triangles, while the body shadow retains the full model. The report's larger full-mesh vertex count includes UV/normal splits.

## Visual and listening review

These are actual Blender renders and Unity screenshots of the delivered meshes. The [original character direction](../concept-art/README.md) remains the reference; the full-body result still simplifies its facial forms, garment construction and material detail.

| Blender source | Unity import |
| --- | --- |
| ![Blender continuous fist close-up](hifi-blender-fist.png) | ![Unity bare fist](hifi-unity-fist.png) |
| ![Blender male and female front](hifi-blender-front.png) | ![Unity male and female front](hifi-unity-front.png) |
| ![Blender rear clothing and hair](hifi-blender-back.png) | ![Unity rear clothing and hair](hifi-unity-back.png) |

The close fist render uses Cycles with denoising; the full-body source views use EEVEE. Neither replaces inspection under the gameplay camera. The final source and Unity views show smooth joined finger roots and wrists. Remaining broad facial bands, simple nose/ear forms and stylized clothing are visible in the full-body comparison.

| Gameplay grip | Settings |
| --- | --- |
| ![Dagger grip in the world](hifi-unity-dagger.png) | ![Persistent sound volume control](hifi-unity-settings.png) |
| ![Pickaxe support and dominant hands](hifi-unity-pickaxe.png) | ![Held terrain block](hifi-unity-block.png) |

Listen to the [eight-second audio audition](hifi-audio-preview.wav): wind, grass/stone footsteps, equipment, stone/wood contact and breakage, pickup and landing. This is an arranged mix of the generated masters, **not a runtime audio capture**. [preview_sound_assets.py](../../Tools/preview_sound_assets.py) reproduces it. In-game distance, shelter and pitch variation are applied separately.

## Reproduction and limits

[create_player_assets.py](../../Tools/create_player_assets.py) creates the base character and invokes the surface, distal rig and material stages. [refine_player_surfaces.py](../../Tools/refine_player_surfaces.py) and [polish_player_rig.py](../../Tools/polish_player_rig.py) can also update existing sources. Run scripts with Blender 5.2 background mode and absolute script paths. [create_sound_assets.py](../../Tools/create_sound_assets.py) authors all audio from deterministic seeds; [the audio manifest](../../ArtSource/Audio/sound-manifest.json) records signal measurements and provenance. No third-party art, audio samples or runtime dependencies were added.

Use [Build-Windows.ps1](../../Tools/Build-Windows.ps1), [Verify-Player.ps1](../../Tools/Verify-Player.ps1) and [Verify-POC.ps1](../../Tools/Verify-POC.ps1) for the pinned Unity 6000.4.4f1 import/build and player checks. Run `Verify-POC.ps1 -Audio` separately for the audio suite. First-person depth and sockets preserve the existing placement and held-item occlusion conventions. The [content contract](../CONTENT_PIPELINE.md#high-fidelity-hand-and-sound-authoring) owns the revised provisional geometry limits and map semantics.

The full body retains the existing stylized facial/clothing design. Face/neck UVs now share continuous coordinates to reduce repeated polygon colour bands; the clothing retains palette UV mapping. Skin wrap approximates soft light transport; it is not subsurface scattering. Audio is original synthesis, not recorded foley. Facial performance, cloth/hair simulation, crowd LODs and production skin painting remain further work. The existing in-place locomotion does not establish zero foot sliding at every speed or slope. Smooth connected topology and passing automated checks do not establish universal absence of pose intersections, studio-level artistic quality or user acceptance.
