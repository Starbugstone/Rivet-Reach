# Audio and dated Editor startup verification

This retains the useful **2026-09-08 audio and Editor-startup evidence**. The 2026-09-10 [avatar rework report](AVATAR_REWORK_RESULTS.md) now owns current player models, hands, equipment and animation. Superseded character screenshots and measurements are retained in Git history.

## Audio content

Original 48 kHz / 24-bit sources include 60 material footsteps/hits/breaks, 16 swing/equip/pickup/landing variants and one stereo wind loop. Footsteps depend on distance and surface; mining impacts follow the swing, with spatial break/placement sounds. Twelve reusable voices bound effects. Shelter attenuates/muffles wind. Settings exposes a persistent master volume.

[create_sound_assets.py](../../Tools/create_sound_assets.py) authors deterministic audio without external samples; [the manifest](../../ArtSource/Audio/sound-manifest.json) records signal measurements and provenance. Effects import as PCM; wind streams as Vorbis. No third-party audio or runtime dependencies were added.

## Measured evidence — 2026-09-08

The pinned Unity 6000.4.4f1 Windows development player [built cleanly](hifi-build.txt), with zero errors/warnings and 211,950,836 bytes. The [audio suite](hifi-audio-report.json) passed 109 checks without logged errors. It covers all 77 clips, sample rates/channels, finite and unclipped sampled PCM, smooth effect endpoints, nonrepeating footsteps, twelve effect voices, disabled Doppler, spatial impacts and mute of already playing sources.

The same dated [terrain/inventory runtime suite](hifi-runtime-report.json) passed 263 checks. Its 1280×720, view-radius-10, 90-fps-capped terrain sample measured 11.112 ms median / 11.470 ms p95 on the i7-10750H / RTX 2060 workstation. Those measurements describe that earlier build, not the current avatar build.

Two ordinary Editor Play startups [passed](hifi-editor-startup.txt), exercising appearance, movement/mining, standing/crouching look-down and held blocks. That result establishes the stated earlier startup workload only; current avatar evidence is linked above.

## Listening review

![Persistent volume control in the dated audio build](hifi-unity-settings.png)

Listen to the [eight-second audio audition](hifi-audio-preview.wav): wind, grass/stone footsteps, equipment, stone/wood contact and breakage, pickup and landing. It is an arranged mix of generated masters, **not runtime audio capture**. [preview_sound_assets.py](../../Tools/preview_sound_assets.py) reproduces it. Runtime distance, shelter and pitch variation are applied separately.

The checks do not measure perceived mix quality, headphone/speaker translation or user acceptance. Audio is original synthesis rather than recorded foley. Use `Tools/Verify-POC.ps1 -Audio -Executable <review-player>` to test a newly built player; do not attribute these dated numbers to a later build.
