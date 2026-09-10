# Stationary shading stability — 2026-09-10

The standalone fixture reproduced animated shading on stationary terrain and tree contact areas. Changing PC renderer SSAO from Blue Noise to Interleaved Gradient eliminated all measured frame differences with camera and sun frozen. The shadows-off control also became stable. One enabled directional light and zero duplicate chunk views were found in this fixture.

The user subsequently reported a moving jagged cast-shadow edge after this change. This frozen-light AO check did not certify moving-sun stability. The [sun-shadow follow-up](SUN_SHADOW_RESULTS.md) records that separate correction and lighting-cost measurements.

## Change and diagnosis

`PC_Renderer.asset` changes only `AOMethod` from 0 to 1. Unity regenerated the two matching SSAO shader prefilter flags in `PC_RPAsset.asset`. AO remains enabled at full resolution, intensity/radius 0.65 and eight samples. Shadow resolution, cascades, distance, bias, filtering, antialiasing and normal gameplay lighting settings remain intact. No gameplay rules changed.

[Unity's SSAO reference](https://docs.unity3d.com/6000.0/Documentation/Manual/urp/ssao-renderer-feature-reference.html) describes Blue Noise as dynamic and Interleaved Gradient as static. The installed URP 17.4 `ScreenSpaceAmbientOcclusionPass` changes the Blue Noise texture and random offsets every frame. Its `UNITY_INCLUDE_TESTS` branch freezes those values; that define was present in the Editor compilation and masked the animated noise in the initial Editor experiment. Native player captures were therefore necessary. This reproduces one source of the reported symptom; it does not establish that every possible Editor shadow artifact has the same cause.

## Controlled native results

Unity 6000.4.4f1, Windows development player, NVIDIA GeForce RTX 2060. Seed 246813, view radius 4, resident terrain, fixed camera and a sandstone corner with a block tree canopy. Readback is 960×540 RGB, MSAA 4 with the camera's existing SMAA. Postprocess dithering is temporarily disabled **only in the probe** to isolate AO. Natural spawning and player motion are disabled in the fixture; normal sessions are unaffected.

Each frozen case warms for 12 frames and compares 30 frames. The moving-sun case advances the clock by 1/60 second for 120 frames. Mean delta averages absolute adjacent-frame RGB code differences on the 0–255 scale across the full image. Changed pixels exceed two codes in at least one channel; percentages average across adjacent-frame pairs.

| Case | Blue Noise mean delta | Static AO mean delta | Blue Noise changed pixels | Static AO changed pixels |
| --- | ---: | ---: | ---: | ---: |
| Camera and sun frozen, shadows on | 0.039648 | 0.000000 | 0.224337% | 0.000000% |
| Camera and sun frozen, shadows off | 0.038353 | 0.000000 | 0.175407% | 0.000000% |
| Camera frozen, sun advancing | 0.050747 | 0.012216 | 0.388501% | 0.157936% |

The frozen baseline still changed with directional shadows disabled, isolating animated AO from cast-shadow movement. The fixed moving-sun case retains ordinary shadow motion. Visual inspection of the matching captures confirms terrain/tree shadows and contact shading remain present. The static sampling pattern differs slightly from Blue Noise; it is not a promise of identical shading pixels between methods.

[Baseline capture](shadow-baseline-2026-09-10.png) · [Fixed capture](shadow-fixed-2026-09-10.png)

Baseline capture timestamp: **18:23:19 UTC**; fixed: **18:25:48 UTC**. Both native runs completed with one successful harness assertion and no recorded runtime errors. Both builds completed with zero errors and warnings. These are focused rendering checks, not a rerun of the earlier broad gameplay suite.

Raw evidence: [baseline shading](shadows/baseline-shadow-report.json), [fixed shading](shadows/fixed-shadow-report.json), [baseline runtime](shadows/baseline-runtime-report.json), [fixed runtime](shadows/fixed-runtime-report.json), [baseline build](shadows/baseline-build-summary.txt), [fixed build](shadows/fixed-build-summary.txt), [artifact hashes](shadows/artifact-hashes.json).

The A/B used the same compiled game/probe DLL over source base `13be675`, with concurrent workshop edits present. The manifest distinguishes baseline source hashes from the later workspace snapshot; the other task regenerated Terrain.mat after both captures. The tested fixed player is `Builds/Shadows/RivetReach.exe`; existing older player folders require rebuilding to include the setting.

## Reproduction and limits

With the pinned project Editor open and idle, run `powershell -ExecutionPolicy Bypass -File Tools/Verify-Shadows.ps1 -Build`. The runner builds to `Builds/Shadows`, then launches a disposable verification session using `-rr-verify -rr-shadow-review`. Reports and captures go to `Logs/Shadows/Player`; an explicit `-OutputDirectory` preserves a separate run. `-Baseline` permits the expected Blue Noise baseline result when deliberately reviewing that setting; the default runner requires PASS. Ordinary play never invokes the probe.

This checks temporal stability, not GPU timing or an FPS improvement. Runtime report timing fields remain zero because this workload does not populate them. It also does not certify every viewing angle, moving-camera cascade boundary, torch arrangement or GPU. Normal sun motion and subtle postprocess dithering remain. The earlier [performance pass](PERFORMANCE_RESULTS.md) retains its own dated optimization and gameplay evidence.
