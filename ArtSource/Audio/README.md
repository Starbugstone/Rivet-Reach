# Original Rivet Reach foley

Copyright © 2026 Starbugstone. All rights reserved. See [LICENSE.md](../../LICENSE.md).

The editable source is [create_sound_assets.py](../../Tools/create_sound_assets.py), using Blender’s bundled Python/numpy. It synthesizes contact transients, low resonances, material grain, debris, cloth and cyclic stereo wind from deterministic seeds. No third-party samples or recordings are included.

The lossless 48 kHz / 24-bit WAV masters are exported directly to [the audio resource folder](../../Assets/RivetReach/Resources/Audio). [sound-manifest.json](sound-manifest.json) records sample format, duration, peak, RMS and loop continuity. Unity’s build preparation selects PCM for short effects and streaming Vorbis for ambience.
