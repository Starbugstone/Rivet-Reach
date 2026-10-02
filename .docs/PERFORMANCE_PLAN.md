# Performance and code-quality improvement plan — 2026-10-03

> **Status:** proposal only. Nothing below is implemented, built or measured. The patches are drafts written against commit `1811ac1`. Its gameplay code matches `455e0fd`, the code reviewed on 2026-10-02. Each patch must be compiled and verified with its listed checks before use. Performance numbers are taken from the [September 27 readiness audit](verification/RELEASE_READINESS_0_1_0.md). Expected gains are estimates, not results. The user has deferred new frame-rate measurements until the development machine is less busy. Functional checks can proceed; performance acceptance cannot be claimed until the [measurement protocol](#a2--measurement-protocol-deferred-by-the-user) runs.

Related: [readiness audit](verification/RELEASE_READINESS_0_1_0.md), [SIMULATION.md](SIMULATION.md), [INDUSTRY.md](INDUSTRY.md), [LIGHTING.md](LIGHTING.md), [FLUIDS.md](FLUIDS.md), [SAVES.md](SAVES.md), [CAPABILITIES.md](CAPABILITIES.md), [DESIGN_QUESTIONS.md](DESIGN_QUESTIONS.md#performance-and-code-quality-plan--2026-10-03).

## Contents

1. [What the evidence says](#1-what-the-evidence-says)
2. [Invariants every change must preserve](#2-invariants-every-change-must-preserve)
3. [Validation toolkit](#3-validation-toolkit)
4. [Summary and order of work](#4-summary-and-order-of-work)
5. [Decisions needed from the user](#5-decisions-needed-from-the-user)
6. [A — Measurement and attribution](#6-a--measurement-and-attribution)
7. [B — Configuration and presentation](#7-b--configuration-and-presentation)
8. [C — Tick scheduling and frame pacing](#8-c--tick-scheduling-and-frame-pacing)
9. [D — Factory simulation](#9-d--factory-simulation)
10. [E — World data, chunk streaming and lighting](#10-e--world-data-chunk-streaming-and-lighting)
11. [F — Events, creatures and presentation](#11-f--events-creatures-and-presentation)
12. [G — UI and input](#12-g--ui-and-input)
13. [H — Startup, saves and build](#13-h--startup-saves-and-build)
14. [I — Rendering structure](#14-i--rendering-structure)
15. [J — Maintainability: dead code, duplication, readability, design](#15-j--maintainability-dead-code-duplication-readability-design)
16. [Test matrix](#16-test-matrix)

## 1. What the evidence says

Sources: [analysis](verification/release-readiness-2026-09-27/release-review/analysis.json), [raw frames](verification/release-readiness-2026-09-27/release-review/frames.csv.gz) and [GPU telemetry](verification/release-readiness-2026-09-27/release-review/gpu-telemetry.csv) from the September 27 audit. The machine was a laptop with an i7-10750H and RTX 2060, running D3D11 at 1080p with a 90 FPS cap and VSync off.

1. **Many light-scene tails match GPU clock drops rather than game work.**
   - In the idle-inventory and storm windows, the main CPU thread stays near 6 ms.
   - GPU time jumps from a 4.0–4.7 ms median to about 28–30 ms. This happens in runs of 5–13 consecutive frames.
   - That ratio (~6.3×) matches 1890/300 MHz. 116 of 454 telemetry samples were below 600 MHz, mostly at 300 MHz, with thermal slowdown active.
   - The factory windows show the same ratio: an 11 ms GPU median against 66–71 ms tails.
   - This is strong correlation, not proof. Two-second sampling cannot attribute individual frames, so A2 asks for higher-rate telemetry.
2. **The large factory is genuinely CPU-bound.**
   - The main-thread median is 14.3 ms (clear), 17.7 ms (full outputs) and 19.1 ms (inventory open).
   - About half is game code: factory tick 5.5–6.5 ms per tick, machine views 1.4–2.1 ms per frame, chicken views 1.1–1.6 ms per frame.
   - The rest is Unity render submission: about 6,900–7,150 SRP draw calls and 2,090–2,340 shadow casters per frame.
3. **Inside the factory tick**, ranged-pump preparation alone costs 1.07–1.29 ms. Fluid pipes cost 1.24–1.43 ms, item pipes 0.69–1.15 ms and power preparation 0.47–0.60 ms.
4. **Lighting falls behind under load.** Light-solve backlogs reach 1,706–1,940 pages. Only one light solve can be in flight at a time.
5. **Saves and loads are separate from gameplay frames.** A paused save took 134 ms and a synchronous load 2,052 ms.

## 2. Invariants every change must preserve

These come from [AGENTS.md](../AGENTS.md) and the specialist documents. A patch that cannot show these hold is not acceptable, whatever its speed-up.

- **Resources:** exact resource conservation through inventories, pipes, tanks, batteries, drops and saves; the fixed-step tick rate; fair allocation and rotating leftovers.
- **Residency:** the [factory residency mechanic](GAMEPLAY.md#factory-residency-and-player-responsibility). No implicit tickets, catch-up credits, offline production or reduced-rate background work.
- **Saves:** byte-identical save content where a format is unchanged; every legacy schema check; atomic replacement and failed-load rollback.
- **Generation:** terrain generation output is unchanged. New generation only ever affects ungenerated chunks.
- **Fluids:** flow decisions, wake order, budgets, source renewal, lava rules and dormant boundaries. Validate against the prior algorithm (see [runtime profiling](../AGENTS.md#runtime-profiling-and-world-flow-stress)).
- **Visuals:** current graphics (shadows, SSAO, materials, explorers and skins, liquids, view distance). Any visual difference needs image review, and refreshed wiki captures if the change is player-visible.
- **Timing overlay:** the opt-in [timing overlay](SIMULATION.md#runtime-timing-overlay--2026-09-22) contract.
- **Verification flags:** the release-review frame cap (`-rr-frame-limit`) and other verification flags keep working.

## 3. Validation toolkit

Existing gates are reused wherever possible.

| Gate | How to run | Covers |
| --- | --- | --- |
| Release domain gate (48 suites) | Write `release-review-checks` to `Logs/build-request.txt` in the open Editor. The result goes to `Logs/build-result.txt` and the summary to `Logs/ReleaseReview/domain-summary.txt` | Domain, industry, fluids, saves, performance, lighting, crates, renewables and more ([ReleaseReviewChecks.cs](../Assets/RivetReach/Editor/ReleaseReviewChecks.cs)) |
| Shader image equivalence | Create `Logs/shader-comparison-request.txt`; the result goes to `Logs/shader-comparison-result.txt` | WorldLit, HeldTool, HeldBlock and ExplorerSkin against reference shaders |
| Editor play verification | `verify-editor` request | Editor play-mode smoke checks |
| Native scenarios | `Tools/Verify-<Feature>.ps1` (Fluids, Lighting, Industry, Multiblocks, Weather, Chickens, Saves, Crates, Renewables, RangedPump, Performance and others) | Real player-build behaviour |
| Release performance windows | `Tools/Verify-ReleaseReview.ps1 -Scenarios @('alpha-survival','release-review') -FrameLimit 90 -GpuTelemetry` then `Tools/analyze_factory_timings.py` | Frame distributions per workload |

New checks proposed in this plan are listed per item and collected in the [test matrix](#16-test-matrix). Where possible a new check compares the new code against a frozen copy of the old algorithm, or a "golden" output recorded before the change. That turns "exact" into a tested claim.

## 4. Summary and order of work

Priority: **P0** = do first (largest measured cost or zero-risk), **P1** = next, **P2** = after measurement, **P3** = opportunistic. "Exact" means no intended change to gameplay results or images.

| ID | Change | Targets | Behaviour | Risk | Priority |
| --- | --- | --- | --- | --- | --- |
| A1 | Light-invalidation and tick-burst counters | Attribution | None | Very low | P0 |
| A2 | Measurement protocol (60 Hz and high-rate GPU clocks) | Thermal vs code | None | — | P0 (deferred) |
| B1 | Turn off the unused URP opaque texture | GPU, every camera | Exact | Very low | P0 |
| B2 | Anti-aliasing experiment (MSAA+SMAA vs alternatives) | GPU post | Visual, needs a decision | Medium | P2 |
| B3 | Portrait camera without scene copies; optional 30 Hz | GPU in inventory | B3a exact, B3b/c visual | Low | P1 |
| B4 | Physics simulation off (no physics is used) | CPU, tiny | Exact | Very low | P3 |
| B5 | Frame-pacing setting (VSync / 60 / 120 / current 90) | Heat, tearing, pacing | Default unchanged | Medium | P0 |
| B6 | IL2CPP player build variant | All C# CPU | Exact | Medium (build) | P1 |
| C1 | At most 3 factory ticks per rendered frame | Catch-up bursts | Timing only | Low | P0 |
| C2 | Stagger 20 Hz systems across frames | Tick-frame peaks | ±1 frame ordering | Low | P0 |
| D1 | Ranged pumps skip source-free reach | 1.1–1.3 ms/tick | Exact | Low | P0 |
| D2 | Cached scheduler lookup in `CanSimulate` | ~9k dictionary lookups/tick | Exact | Low | P1 |
| D3 | Power generation stored on the machine | Power preparation | Exact | Low | P1 |
| D4 | Per-type machine lists | Machine passes | Exact | Medium | P2 |
| D5 | Skip pipe groups that cannot move anything | Fluid/item pipes | Exact (must be proven) | High | P2 |
| D6 | Incremental topology publication | Unload/return spikes | Exact | High | P2 |
| D7 | Debounced lamp light | Light backlog | Light timing, needs a decision | Medium | P1 (after A1) |
| E1 | `BlockPos.Chunk` by shifts | Every world lookup | Exact | Very low | P1 |
| E2 | Resident-first world reads, one lookup in collision/raycast | Collision, raycast, pumps | Exact | Low | P0 |
| E3 | Precomputed block-trait tables | Hot predicates | Exact | Low | P1 |
| E4 | Direct column walk on light invalidation | Opacity edits | Exact | Very low | P1 |
| E5 | Terrain worker count scales with cores | Streaming throughput | Exact | Medium | P1 |
| E6 | Concurrent light solves | Light backlog | Possible transient differences | Medium | P2 |
| E7 | Packed terrain vertices and 16-bit indices | Upload, VRAM | Near-exact | Medium | P2 |
| E8 | Allocation-free tree lookup in `TerrainGenerator.At` | Worker GC | Exact | Very low | P1 |
| E9 | Allocation-free fluid schedule | GC during flows | Exact, same save bytes | Low | P1 |
| E10 | Pooled worker buffers | GC | Exact | Medium | P2 |
| E11 | Jobs + Burst generation/meshing/lighting | Streaming | Exact if ported exactly | High | P3 |
| F1 | Old/new IDs on block events; filtered listeners | Per-frame refreshes during flows | Exact | Low | P1 |
| F2 | Chicken views: batching-safe tint, clip cache, culled evaluation | 1.1–1.6 ms/frame | F2a/b exact, F2c visual | Low | P1 |
| F3 | Remove duplicate passive target selection | Ray casts | Label edge case | Low | P2 |
| F4 | IMGUI creature labels → uGUI HUD | IMGUI pass | Visual | Low | P2 |
| F5 | Hoist per-instance native calls in distant machines | Machine views | Exact | Very low | P1 |
| F6 | Spread 4–5 Hz refreshes across frames | Periodic spikes | Visual timing | Medium | P2 |
| F7 | Rain animated on the GPU | ~0.4 ms/frame in storms | Near-exact | Medium | P2 |
| F8 | Instanced dropped items | Draw calls | Exact | Medium | P3 |
| G1 | Cache the mine-button preference | Input | Exact | Low | P3 |
| G2 | Crate status text only on change | GC | Exact | Very low | P3 |
| G3 | Cached cable-grid lookup in machine UI | UI | Exact | Very low | P3 |
| H1 | Memoize save content fingerprints | Startup | Exact (golden test) | Low | P1 |
| H2 | Cache the save listing by file length and time | Title/load menus | Exact | Low | P1 |
| H3 | Background load preparation | 2 s load stall | Exact | High | P2 |
| H4 | Verification code out of release players | Size, compile, attack surface | Exact | High (pipeline) | P2 |
| I1 | Merge static machine parts | Draw calls, shadow casters | Exact (image check) | Medium | P1 |
| I2 | GPU Resident Drawer | Submission cost | Exact if supported | High | P2 |
| I3 | Group terrain chunk draws | Draw calls | Exact | High | P3 |
| I4 | SSAO cost evaluation | GPU | Visual, needs a decision | Medium | P2 |
| J1–J8 | Dead code, duplication, readability, structure, workspace | Maintainability | Exact | Low–high | P1–P3 |

Recommended commit order:
1. A1
2. B1, B4, B5 (default unchanged)
3. C1 + C2
4. D1, E2, E1, E3, E4, E8, E9
5. D2, D3, F1, F2a/b, F5, H1, H2, J1, J2
6. Run A2 when the user allows, then decide B2, B3b/c, D7 and I4.
7. E5, B6, I1
8. The remaining P2 items

Use one commit per item. Each commit lists the checks run and stages only its own files.

## 5. Decisions needed from the user

None of these blocks the exact items above. They are recorded in [DESIGN_QUESTIONS.md](DESIGN_QUESTIONS.md#performance-and-code-quality-plan--2026-10-03).

| Question | Options | Recommendation |
| --- | --- | --- |
| Default frame pacing (B5) | Keep 90 cap / VSync / 60 cap / 120 cap | Keep 90 until A2; then prefer VSync **only if** the factory stays under 16.7 ms. VSync turns each missed frame into a 33 ms frame |
| Anti-aliasing (B2) | MSAA 4× + SMAA (current) / MSAA 4× + alpha-to-coverage / SMAA only | Decide from side-by-side captures of foliage, pipes and cables |
| Portrait (B3b/c) | Keep shadows and SSAO / drop them / render at 30 Hz | Keep until reviewed; B3a is free |
| Lamp light debounce (D7) | Immediate (current) / 10-tick settle | Decide after A1 shows lamp invalidations dominate |
| Sustained-overload policy (C1) | Never drop ticks (proposed) / drop backlog beyond 1 s | Never drop; revisit only if long overloads are observed |
| SSAO (I4) | Full resolution (current) / downsampled / depth-only source | Measure first; image review |
| IL2CPP (B6) | Mono (current) / IL2CPP | Adopt if the full native suite passes and A2 shows a gain |

---

## 6. A — Measurement and attribution

### A1 — Light-invalidation and tick-burst counters

- **Why:** the light backlog (1,706–1,940 pages) has several possible sources: block-opacity edits that relight a column, torch/lava/lamp source changes, border propagation, and residency. Fixes D7, E4 and E6 differ by cause. Catch-up bursts (C1) are inferred from code, not counted.
- **Files:** `World/WorldLighting.cs`, `Expedition.cs`, `UI/GameUIHud.cs` (all under `Assets/RivetReach/Code/`).
- **Behaviour:** none. These are integer counters and diagnostics text at the existing 4 Hz overlay refresh.

```diff
--- a/Assets/RivetReach/Code/World/WorldLighting.cs
+++ b/Assets/RivetReach/Code/World/WorldLighting.cs
@@
         public int PendingLightChunks=>dirtyLights.Count+(lightWork==null?0:1);
+        // Cumulative invalidation causes for diagnostics; they never affect solves.
+        public long LightSourceInvalidations,LightOpacityInvalidations,LightBorderInvalidations,LightResidencyInvalidations;
@@
-        public void LightSourceChanged(BlockPos p)=>DirtyLight(p.Chunk);
+        public void LightSourceChanged(BlockPos p){LightSourceInvalidations++;DirtyLight(p.Chunk);}
@@ void LightingChanged(BlockPos p,byte before,byte after)
             if(BlockId.Opaque(before)!=BlockId.Opaque(after))
             {
+                LightOpacityInvalidations++;
                 var col=(p.Chunk.X,p.Chunk.Z);...
             }
-            else if(before==BlockId.Torch||...)DirtyLight(p.Chunk);
+            else if(before==BlockId.Torch||...){LightSourceInvalidations++;DirtyLight(p.Chunk);}
@@ void LightingResidency()
-                foreach(var d in ChunkLighting.Faces)DirtyLight(p.Offset(d.x,d.y,d.z));
+                foreach(var d in ChunkLighting.Faces){LightResidencyInvalidations++;DirtyLight(p.Offset(d.x,d.y,d.z));}
@@ void AdvanceLighting()
-                            if(changed){var d=ChunkLighting.Faces[face];DirtyLight(result.Position.Offset(d.x,d.y,d.z));}
+                            if(changed){LightBorderInvalidations++;var d=ChunkLighting.Faces[face];DirtyLight(result.Position.Offset(d.x,d.y,d.z));}
```

```diff
--- a/Assets/RivetReach/Code/Expedition.cs
+++ b/Assets/RivetReach/Code/Expedition.cs
@@
+        public int LastSimulationTicks {get;private set;}
+        public int MaxSimulationTicks {get;private set;}
@@ void Update()
-                ...int ticks=Survival.Advance(Time.deltaTime);Industry.Advance(ticks);...
+                ...int ticks=Survival.Advance(Time.deltaTime);LastSimulationTicks=ticks;MaxSimulationTicks=Math.Max(MaxSimulationTicks,ticks);Industry.Advance(ticks);...
```

```diff
--- a/Assets/RivetReach/Code/UI/GameUIHud.cs
+++ b/Assets/RivetReach/Code/UI/GameUIHud.cs
@@ void RefreshDiagnostics()
             diagnostics.text=$"{runtimeDiagnostics.Frames}\n...Placement: {game.PlacementDiagnostic??"No attempt yet"}";
+            var w=game.World;
+            diagnostics.text+=$"\nLight invalidations source {w.LightSourceInvalidations} · opacity {w.LightOpacityInvalidations} · border {w.LightBorderInvalidations} · residency {w.LightResidencyInvalidations} · ticks this/max frame {game.LastSimulationTicks}/{game.MaxSimulationTicks}";
```

- **Risks:**
  - The diagnostics panel height is fixed (`GameUI.BuildHUD`, 446 px). Check that the extra line fits at 1080p and at interface scale 0.85.
  - `RuntimeDiagnosticsChecks` may assert exact text; update it if so.
- **Validation:** the release domain gate (`RuntimeDiagnosticsChecks`), plus a manual overlay screenshot. In the stress fixture, read the counters before and after the clear, full-output and power-shortage windows.

### A2 — Measurement protocol (deferred by the user)

- **Why:** item 1 in [the evidence](#1-what-the-evidence-says) shows GPU clock behaviour can dominate p95/p99. Optimizations must be judged on runs that separate heat from code.
- **Protocol** (no code change):
  1. Plug in the laptop and use a stable power plan. Record ambient conditions.
  2. Run the existing release-review windows twice: `-FrameLimit 90` for historical comparison, and with VSync at 60 Hz once B5 exists.
  3. Log GPU clocks at 100 ms, for example `nvidia-smi --query-gpu=timestamp,clocks.gr,clocks.mem,power.draw,temperature.gpu,clocks_event_reasons.active --format=csv -lms 100`. Add PresentMon if available, to get displayed-frame timing.
  4. Report every frame over 16.67 ms and over 22.22 ms, plus p95, p99 and maximum, as the audit requires. Keep save/load stalls separate.
- **Exit criteria:** before/after pairs for each P0/P1 item, run in the same environment. A change that does not move its targeted scope is reverted or re-examined, not kept on faith.

---

## 7. B — Configuration and presentation

### B1 — Turn off the unused URP opaque texture

- **Why:** `PC_RPAsset.asset` sets `m_RequireOpaqueTexture: 1`. No project shader or Shader Graph samples `_CameraOpaqueTexture` (searched: none; only `ArcadeParticle` reads depth). URP still copies (downsampled) colour after opaques for **every camera**, including the inventory portrait.
- **File:** `Assets/RivetReach/Settings/PC_RPAsset.asset`.

```diff
--- a/Assets/RivetReach/Settings/PC_RPAsset.asset
+++ b/Assets/RivetReach/Settings/PC_RPAsset.asset
@@
   m_RequireDepthTexture: 1
-  m_RequireOpaqueTexture: 1
+  m_RequireOpaqueTexture: 0
```

- **Behaviour:** exact. Nothing reads the texture.
- **Risks:** a future transparent/refraction shader would need it re-enabled, either per camera (`requiresColorOption`) or in the asset. Re-check with `grep -rn "_CameraOpaqueTexture\|SampleSceneColor\|DeclareOpaqueTexture" Assets`.
- **Validation:**
  - Shader image equivalence.
  - Native captures of water, glass, tanks and rain (identical images expected).
  - Frame Debugger confirms the `CopyColor` pass is gone.
  - A2 GPU timing before/after.

### B2 — Anti-aliasing experiment (decision)

- **Why:** the main camera runs MSAA 4× (`PC_RPAsset.asset` `m_MSAA: 4`) and SMAA (`Player/ArcadePresentation.cs:84`). SMAA is documented as intentional ([SHADOW_RESULTS.md](verification/SHADOW_RESULTS.md)). It likely smooths alpha-clipped edges that MSAA alone does not resolve: `clip()` in `HeldTool`, `MachineLit` and `ArcadeGrass`. Removing it blindly could lose graphics.
- **Plan:**
  1. Add a hidden developer toggle, not a default change.
  2. Capture fixed viewpoints in four variants:
     - **(a)** current
     - **(b)** MSAA 4× only
     - **(c)** MSAA 4× with `AlphaToMask On` added to the alpha-clipped passes
     - **(d)** SMAA only (MSAA off)
  3. Compare foliage, crop cards, cables/pipes and machine decals, and measure GPU time in A2.
- **Patch** (toggle only; `ArcadePresentation.Bind`):

```diff
-            game.Player.Camera.GetUniversalAdditionalCameraData().antialiasing=AntialiasingMode.SubpixelMorphologicalAntiAliasing;
+            // Developer experiment B2: "visual.smaa" 1 (default) keeps the current camera AA.
+            game.Player.Camera.GetUniversalAdditionalCameraData().antialiasing=PlayerPrefs.GetInt("visual.smaa",1)==1?AntialiasingMode.SubpixelMorphologicalAntiAliasing:AntialiasingMode.None;
```

- **Risks:** visible aliasing on thin geometry or clipped foliage. Alpha-to-coverage changes the look of clipped edges, and the dithered `ArcadeGrass` fade does not benefit from it.
- **Validation:** captured image sets, A2 GPU timing, and user review. If the default changes, refresh affected wiki captures per [AGENTS.md](../AGENTS.md#required-github-wiki-updates-and-visuals).

### B3 — Inventory portrait camera

- **Why:** `UI/GameUI.cs:390-400` creates a 768×1024, 4× MSAA render-texture camera. It renders every frame while the inventory or Appearance screen is open (`UI/GameUIScreenReuse.cs:187`). With no overrides it inherits pipeline depth/colour copies, shadows and the renderer's SSAO feature.
- **B3a (exact):** stop the colour/depth copies; the portrait samples neither.

```diff
--- a/Assets/RivetReach/Code/UI/GameUI.cs
+++ b/Assets/RivetReach/Code/UI/GameUI.cs
@@
 using UnityEngine.UI;
+using UnityEngine.Rendering.Universal;
@@ void CreatePreview()
             var camera=cam.AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=slate;camera.fieldOfView=38;camera.nearClipPlane=.1f;camera.farClipPlane=8;camera.cullingMask=1<<30;
+            // The portrait samples neither scene colour nor depth textures.
+            var portraitData=camera.GetUniversalAdditionalCameraData();
+            portraitData.requiresColorOption=CameraOverrideOption.Off;portraitData.requiresDepthOption=CameraOverrideOption.Off;
+            portraitData.renderPostProcessing=false;
```

- **B3b (visual, decision):**
  - `portraitData.renderShadows=false` stops avatar self-shadowing from the sun.
  - Assigning a renderer without the SSAO feature (`portraitData.SetRenderer(index)`) needs a second renderer in `PC_RPAsset`.
- **B3c (visual, decision):** render at 30 Hz while idle. Keep a field `previewCamera` and a `previewDirty` flag, set by `RotatePreview` and `RefreshPreview`, then in `Update`:

```csharp
if(previewCamera!=null)previewCamera.enabled=previewRoot.activeSelf&&(previewDirty||(Time.frameCount&1)==0);previewDirty=false;
```

- **Risks:**
  - B3a: URP may still create depth for MSAA resolve; the image should be unchanged.
  - B3b/c: a visibly different or choppier portrait.
- **Validation:**
  - Shader equivalence (ExplorerSkin).
  - Before/after portrait captures for both explorers, both skin generations and armor.
  - Frame Debugger pass list for the portrait camera.
  - `ScreenReuseVerification` and `AvatarVerification` native suites.

### B4 — Physics simulation off

- **Why:** no code uses `Physics.*`, rigidbodies or physics raycasters, and Resources prefabs contain no colliders. Primitives' colliders are destroyed in code. Unity still steps an empty physics world at 50 Hz.
- **File:** `Expedition.cs` (`Awake`).

```diff
-            Instance=this;Application.targetFrameRate=90;QualitySettings.vSyncCount=0;
+            Instance=this;Application.targetFrameRate=90;QualitySettings.vSyncCount=0;
+            // Voxel collision is authoritative; the built-in physics world is unused.
+            Physics.simulationMode=SimulationMode.Script;
```

(If B5 lands first, keep its frame-pacing line and add only the physics line.)

- **Risks:** a future feature using physics must re-enable it. uGUI's `GraphicRaycaster` does not use physics.
- **Validation:** release domain gate, `MovementInteractionVerification`, and a Profiler capture showing no `Physics.Simulate`.

### B5 — Frame-pacing setting (default unchanged)

- **Why:**
  - `Expedition.cs:65` forces a 90 FPS cap with VSync off and no player choice.
  - On a 60 Hz display this tears and keeps the GPU 50% busier than needed, which feeds the throttling in §1.
  - The audit lists a persisted frame-cap/display-sync choice as release work.
- **Files:** new `Assets/RivetReach/Code/Core/FramePacing.cs`, `Expedition.cs`, `UI/GameUI.cs` (Settings screen).

```csharp
// Assets/RivetReach/Code/Core/FramePacing.cs
using UnityEngine;

namespace RivetReach
{
    // Persisted presentation policy. Verification flags (-rr-frame-limit) still override it.
    public static class FramePacing
    {
        public enum Mode { Legacy90, DisplaySync, Cap60, Cap120 }
        const string Key="display.framePacing";
        public static Mode Current=>(Mode)Mathf.Clamp(PlayerPrefs.GetInt(Key,(int)Mode.Legacy90),0,3);
        public static void Apply(Mode mode)
        {
            PlayerPrefs.SetInt(Key,(int)mode);
            QualitySettings.vSyncCount=mode==Mode.DisplaySync?1:0;
            Application.targetFrameRate=mode switch{Mode.DisplaySync=>-1,Mode.Cap60=>60,Mode.Cap120=>120,_=>90};
        }
        public static Mode Next(Mode mode)=>(Mode)(((int)mode+1)%4);
        public static string Label(Mode mode)=>mode switch
        {Mode.DisplaySync=>"FRAME PACING: DISPLAY SYNC",Mode.Cap60=>"FRAME PACING: 60 FPS CAP",Mode.Cap120=>"FRAME PACING: 120 FPS CAP",_=>"FRAME PACING: 90 FPS CAP"};
    }
}
```

```diff
--- a/Assets/RivetReach/Code/Expedition.cs
+++ b/Assets/RivetReach/Code/Expedition.cs
@@ void Awake()
-            Instance=this;Application.targetFrameRate=90;QualitySettings.vSyncCount=0;
+            Instance=this;FramePacing.Apply(FramePacing.Current);
```

```diff
--- a/Assets/RivetReach/Code/UI/GameUI.cs
+++ b/Assets/RivetReach/Code/UI/GameUI.cs
@@ else if(game.Mode==ScreenMode.Settings)
                 Slider(p.transform,"Effect intensity",434,ArcadePresentation.Active.Intensity,0,1,ArcadePresentation.Active.SetIntensity);
+                Button(p.transform,FramePacing.Label(FramePacing.Current),36,489,716,40,()=>{FramePacing.Apply(FramePacing.Next(FramePacing.Current));Rebuild();});
```

- **Behaviour:** the default stays the current 90 cap. Switching the default is a separate decision after A2.
- **Risks:**
  - With VSync on, any frame over 16.7 ms is held to the next refresh (33 ms). That is worse pacing than a 60 cap until the factory CPU work (C, D, I) is under budget.
  - Layout: the new button sits between the last slider and the bottom buttons. Check that it doesn't overlap at interface scale 0.85.
  - Verification suites that save/restore `targetFrameRate`/`vSyncCount` are unaffected; `-rr-frame-limit` still overrides.
- **Validation:**
  - `ControlPresetChecks` / settings persistence (add a case for the new key).
  - Manual check of each mode with the Unity frame-interval CSV plus PresentMon.
  - A settings screenshot for the wiki if the Settings page is documented there.

### B6 — IL2CPP player build variant

- **Why:**
  - The Windows player uses the default Mono backend. `ProjectSettings.asset` only lists an Android backend.
  - The hot code is plain C# loops: simulation, meshing, lighting, generation and routing. IL2CPP usually speeds this up, but that must be measured, not assumed.
- **File:** `Assets/RivetReach/Editor/ProjectBuild.cs` (`Build`), plus a new request command.

```diff
-        static void Build(string outputFolder="PlayerRevision4",BuildOptions options=BuildOptions.Development)
+        static void Build(string outputFolder="PlayerRevision4",BuildOptions options=BuildOptions.Development,bool il2cpp=false)
         {
             string output=Path.Combine("Builds",outputFolder);Directory.CreateDirectory(output);
@@
             var graphics=PlayerSettings.GetGraphicsAPIs(BuildTarget.StandaloneWindows64);
+            var standalone=UnityEditor.Build.NamedBuildTarget.Standalone;
+            var backend=PlayerSettings.GetScriptingBackend(standalone);
+            var il2cppConfiguration=PlayerSettings.GetIl2CppCompilerConfiguration(standalone);
             UnityEditor.Build.Reporting.BuildReport report;
             try
             {
+                if(il2cpp){PlayerSettings.SetScriptingBackend(standalone,ScriptingImplementation.IL2CPP);PlayerSettings.SetIl2CppCompilerConfiguration(standalone,Il2CppCompilerConfiguration.Release);}
                 PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64,false);
@@
             finally
             {
+                PlayerSettings.SetScriptingBackend(standalone,backend);PlayerSettings.SetIl2CppCompilerConfiguration(standalone,il2cppConfiguration);
                 PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64,graphics);
```

Add a command beside the existing release-review commands:

```csharp
if(command=="release-review-il2cpp"){ReleaseReviewChecks.Run();Build("ReleaseReviewIl2cpp",BuildOptions.None,il2cpp:true);File.WriteAllText("Logs/build-result.txt","SUCCESS release review il2cpp");return;}
```

- **Prerequisites:** the Unity Hub "Windows Build Support (IL2CPP)" module, plus the Visual Studio C++ build tools. Check through the [unity-blender-local skill](skills/unity-blender-local/SKILL.md) before assuming they are installed. No Editor upgrade is implied.
- **Risks:**
  - Longer builds.
  - Managed code stripping could remove types used only by reflection or serialization: `JsonUtility` types, `Resources.LoadAll<MobDefinition>`, and `ScriptableObject` catalogs. Add `Assets/link.xml` preserving the `RivetReach` namespace if anything goes missing.
  - Verification code uses reflection on private fields; Editor-only use is unaffected.
- **Validation:**
  - Run the complete native scenario set on the IL2CPP player (every `Tools/Verify-*.ps1`, with the legacy save sweep).
  - A2 performance windows paired with a Mono build of the same commit.
  - Confirm the managed assembly identity in `build-identity.json`.

---

## 8. C — Tick scheduling and frame pacing

### C1 — At most three factory ticks per rendered frame

- **Why:**
  - `Survival/WorldSurvival.cs:100` runs `Math.Min(100,fraction)` ticks in one frame. `Expedition.Update` then runs the same number of industry, fishing, weather, health and lava ticks.
  - Unity's maximum timestep (0.333 s) already limits a single hitch to about 7 ticks. Each stress-factory tick costs 5.5–6.5 ms, so one hitch creates two or three more long frames.
  - The audit recorded a 61.66 ms factory scope "which may contain multiple ticks".
- **File:** `Survival/WorldSurvival.cs`.

```diff
--- a/Assets/RivetReach/Code/Survival/WorldSurvival.cs
+++ b/Assets/RivetReach/Code/Survival/WorldSurvival.cs
@@
         public const int TicksPerSecond=20,CropStageTicks=1200,SaplingGrowthTicks=3600;
+        // Catch-up is spread across frames. The saved backlog (fraction) is never
+        // dropped, so total ticks and resource transfers are unchanged.
+        public const int MaxTicksPerFrame=3;
+        public double TickFraction=>fraction;
@@ public int Advance(float seconds)
-            fraction+=seconds*TicksPerSecond;int ticks=(int)Math.Min(100,fraction);fraction-=ticks;
+            fraction+=seconds*TicksPerSecond;int ticks=(int)Math.Min(MaxTicksPerFrame,fraction);fraction-=ticks;
```

- **Behaviour:**
  - Identical tick sequence and totals.
  - Only the frame in which a delayed tick runs changes: catch-up proceeds at up to 3 ticks per frame (9× real time at 60 FPS) until the backlog clears.
  - The save format is unchanged: `fraction` is already persisted (`WorldSaveState.cs:105`).
- **Risks:**
  - Under sustained overload the simulation runs slower than wall-clock time, and the backlog persists in saves. That is the [decision in §5](#5-decisions-needed-from-the-user); the recommendation is no dropping.
  - Native checks that wait real seconds and expect exact tick counts could shift by a frame.
- **Validation:**
  - New Editor check `TickCapChecks`: feed `Advance` sequences (including 0.333 s hitches) and assert totals equal the uncapped version and per-call counts are ≤3.
  - `ResourceTickChecks` and `SurvivalChecks` in the domain gate.
  - Native `Verify-Industry`, `Verify-Saves` (a saved backlog round-trips) and `Verify-Fluids`.
  - A2: count of frames with more than one industry tick (A1 counter).

### C2 — Stagger the 20 Hz systems across frames

- **Why:**
  - Each fixed-step system has its own accumulator starting at zero when the session is created: survival/industry, `MobSystem`, `PassiveSystem`, `DroppedItems`, and the world's fluids and grass.
  - All advance by the same `Time.deltaTime`, so at 60 FPS they all tick on the **same** frame, once every three frames.
  - The audit's `IndustryTick` p95 is 8–11 ms against a 2–3 ms mean.
  - Giving each group a fixed phase offset spreads the work over the three frames, with no change to rates.
- **Files:**
  - new `Assets/RivetReach/Code/Core/SimulationPhase.cs`
  - `Expedition.cs`, `Mobs/MobSystem.cs`, `Animals/PassiveSystem.cs`, `Player/DroppedItems.cs`, `World/VoxelWorld.cs`
  - uses `WorldSurvival.TickFraction` from C1

```csharp
// Assets/RivetReach/Code/Core/SimulationPhase.cs
using System;

namespace RivetReach
{
    // Fixed 20 Hz systems keep their rates but land on different rendered frames:
    // at 60 FPS the factory, creature and environment groups each own one frame of three.
    public static class SimulationPhase
    {
        public const double Factory=0,Creatures=1/3d,Environment=2/3d;
        // Accumulator (seconds) that makes a channel tick `offset` ticks before the
        // factory channel whose pending fraction is `factoryFraction`.
        public static float Accumulator(double factoryFraction,double offset,float stepSeconds)
        {
            double phase=factoryFraction-Math.Floor(factoryFraction)+offset;
            return (float)((phase-Math.Floor(phase))*stepSeconds);
        }
    }
}
```

Derivation: the factory ticks after `1 - frac(f)` ticks of time, and a channel with accumulator `a` and step `S` ticks after `1 - a/S`. Setting `a/S ≡ frac(f) + offset (mod 1)` puts the channel `offset` ticks earlier.

The per-frame caps (`Min(dt,.2)` in creatures, `steps<4` in items/fluids/grass) remove whole multiples of 0.05 s, so the fractional phase survives hitches.

Each system gains a setter (shown for `MobSystem`; `PassiveSystem` and `DroppedItems` are identical):

```diff
--- a/Assets/RivetReach/Code/Mobs/MobSystem.cs
+++ b/Assets/RivetReach/Code/Mobs/MobSystem.cs
@@
         float accumulator,spawnAt=4,elapsed,nextStrike,nextPlayerHit,graceUntil=10;
+        public void AlignTickPhase(double factoryFraction,double offset)=>accumulator=SimulationPhase.Accumulator(factoryFraction,offset,.05f);
```

```diff
--- a/Assets/RivetReach/Code/World/VoxelWorld.cs
+++ b/Assets/RivetReach/Code/World/VoxelWorld.cs
@@
+        public void AlignTickPhases(double factoryFraction,double offset)
+        {
+            fluidAccumulator=SimulationPhase.Accumulator(factoryFraction,offset,FluidSimulation.StepSeconds);
+            grassAccumulator=SimulationPhase.Accumulator(factoryFraction,offset,GrassSimulation.StepSeconds);
+        }
```

```diff
--- a/Assets/RivetReach/Code/Expedition.cs
+++ b/Assets/RivetReach/Code/Expedition.cs
@@ void CreateSession(int seed,string generatorVersion=TerrainGenerator.Version)
             root.AddComponent<WeatherPresentation>().Initialize(this);
+            AlignTickPhases();
         }
+        // Measured costs: factory ~5.5–6.5 ms/tick (stress); creatures ~1 ms; environment small except floods.
+        void AlignTickPhases()
+        {
+            double f=Survival.TickFraction;
+            Mobs.AlignTickPhase(f,SimulationPhase.Creatures);Animals.AlignTickPhase(f,SimulationPhase.Creatures);
+            Items.AlignTickPhase(f,SimulationPhase.Environment);World.AlignTickPhases(f,SimulationPhase.Environment);
+        }
```

In `Persistence/ExpeditionSaves.cs` (`RestoreSave`), after `Navigation.ReadSave(r);SaveReader.Require(...trailing...)`, add `AlignTickPhases();`. Survival's saved fraction gives a loaded session an arbitrary phase, so the others must follow it.

Trees step at 10 Hz and discard fractions (`VoxelWorld.AdvanceTrees`); leave them unchanged.

- **Behaviour:**
  - Rates, budgets and per-system order are unchanged.
  - Cross-system timing shifts by one frame at most. A pump removing a source (factory frame) is seen by the fluid tick one frame later than "same frame". Since mobs and animals already have independently clamped accumulators, this kind of offset already happens today.
- **Risks:**
  - At 30 FPS, phases merge (1.5 frames per tick). That is no worse than today.
  - Tests that compare cross-system state after a fixed number of real frames may shift by one tick. Tests that call `Step()` directly are unaffected.
- **Validation:**
  - New `TickPhaseChecks`: simulate 10,000 frames at 60, 90 and 144 FPS with random hitches and assert the three groups never tick on the same frame at 60 FPS.
  - Assert that loading a save with fraction 0.33 keeps the separation.
  - Domain gate; native `Verify-Mobs`, `Verify-Chickens`, `Verify-Fluids`, `Verify-Lava`, `Verify-Industry`.
  - A2: tick-frame peak (IndustryTick p95/p99) before and after.

---

## 9. D — Factory simulation

### D1 — Ranged pumps skip source-free reach (exact)

- **Why:**
  - `Industry/RangedPump.cs` scans the inclusive 17³ reach at 256 cells per tick while no source is found. That is two dictionary lookups per cell through `TryRead`, and the whole reach is rescanned every 2 s.
  - Measured `RR.RangedPumpPreparation`: 1.07–1.29 ms per factory tick, about 20% of the tick.
- **Idea:** pages remember how many liquid **source** cells they hold. If every page overlapping a pump's reach is resident and source-free, the per-cell loop would only advance `PumpScanIndex`. Do that in one step.
  - Scan order ("drain upper layers first"), unloaded handling and retry timing stay identical.
- **Files:** `Industry/IndustrySimulation.cs` (interface and field), `Industry/RangedPump.cs`, `Industry/WorldIndustry.cs`, `World/VoxelWorld.cs`, `World/ChunkMesher.cs` (`ChunkBuild`).

```diff
--- a/Assets/RivetReach/Code/Industry/IndustrySimulation.cs
+++ b/Assets/RivetReach/Code/Industry/IndustrySimulation.cs
@@
     public interface IIndustryResidentCells
     {
         bool TryRead(BlockPos position,out byte block);
     }
+    // Optional exact acceleration: resident pages report their liquid source count.
+    // False means not resident; callers must then use the ordinary per-cell path.
+    public interface IFluidSourceIndex
+    {
+        bool TryGetFluidSourceCount(ChunkPos chunk,out int sources);
+    }
@@
         readonly IIndustryResidentCells residentCells;
+        readonly IFluidSourceIndex sourceIndex;
@@ public IndustrySimulation(...)
-            this.world=world;residentCells=world as IIndustryResidentCells;...
+            this.world=world;residentCells=world as IIndustryResidentCells;sourceIndex=world as IFluidSourceIndex;...
```

```diff
--- a/Assets/RivetReach/Code/Industry/RangedPump.cs
+++ b/Assets/RivetReach/Code/Industry/RangedPump.cs
@@
+        // Exact shortcut: with every page of the reach resident and source-free, each
+        // cell read below succeeds without a match, so the loop would only advance the index.
+        bool RangedReachHasNoSources(MachineState m)
+        {
+            if(sourceIndex==null)return false;
+            var min=m.Position.Offset(RangedPumpMin,RangedPumpMin,RangedPumpMin).Chunk;
+            var max=m.Position.Offset(RangedPumpMax,RangedPumpMax,RangedPumpMax).Chunk;
+            for(long z=min.Z;z<=max.Z;z++)for(int y=min.Y;y<=max.Y;y++)for(long x=min.X;x<=max.X;x++)
+                if(!sourceIndex.TryGetFluidSourceCount(new ChunkPos(x,y,z),out int sources)||sources>0)return false;
+            return true;
+        }
@@ void PrepareRangedPump(MachineState m)
             if(Tick<m.PumpRetryTick){m.Status=MachineStatus.NoInput;return;}
-            for(int budget=0;budget<RangedPumpScanBudget&&m.PumpScanIndex<RangedPumpCells;budget++)
+            if(RangedReachHasNoSources(m))m.PumpScanIndex+=Math.Min(RangedPumpScanBudget,RangedPumpCells-m.PumpScanIndex);
+            else for(int budget=0;budget<RangedPumpScanBudget&&m.PumpScanIndex<RangedPumpCells;budget++)
             {
```

(`RangedPump.cs` needs `using System;` for `Math`.)

```diff
--- a/Assets/RivetReach/Code/Industry/WorldIndustry.cs
+++ b/Assets/RivetReach/Code/Industry/WorldIndustry.cs
@@
-    public sealed class WorldIndustry : IIndustryWorld, IIndustryItemEndpoints, IRenewableEnvironment, IIndustryResidentCells
+    public sealed class WorldIndustry : IIndustryWorld, IIndustryItemEndpoints, IRenewableEnvironment, IIndustryResidentCells, IFluidSourceIndex
@@
         public bool TryRead(BlockPos p,out byte block)=>game.World.TryRead(p,out block);
+        public bool TryGetFluidSourceCount(ChunkPos chunk,out int sources)=>game.World.TryGetFluidSourceCount(chunk,out sources);
```

```diff
--- a/Assets/RivetReach/Code/World/ChunkMesher.cs
+++ b/Assets/RivetReach/Code/World/ChunkMesher.cs
@@ public sealed class ChunkBuild
         public double Milliseconds;
+        // Interior liquid source cells; set by generation jobs only (-1 otherwise).
+        public int FluidSources=-1;
```

```diff
--- a/Assets/RivetReach/Code/World/VoxelWorld.cs
+++ b/Assets/RivetReach/Code/World/VoxelWorld.cs
@@ sealed class Resident
             public bool Busy,Dirty=true;
+            public int FluidSources; // interior liquid sources; valid while Cells!=null
@@
+        static bool FluidSource(byte id)=>Fluids.Registry.Get(id) is FluidDefinition f&&f.IsSource(id);
+        static int CountFluidSources(byte[] cells)
+        {
+            int count=0;
+            for(int z=0;z<32;z++)for(int y=0;y<32;y++){int row=ChunkMesher.Index(0,y,z);for(int x=0;x<32;x++)if(FluidSource(cells[row+x]))count++;}
+            return count;
+        }
+        public bool TryGetFluidSourceCount(ChunkPos chunk,out int sources)
+        {sources=0;if(!chunks.TryGetValue(chunk,out var c)||c.Cells==null)return false;sources=c.FluidSources;return true;}
@@ bool Change(...)  // after the halo loop that writes c.Cells
+            if(chunks.TryGetValue(p.Chunk,out var owner)&&owner.Cells!=null)
+                owner.FluidSources+=(FluidSource(replacement)?1:0)-(FluidSource(expected)?1:0);
@@ void Launch(...) inside Task.Run, after the edit loop
-                var result=ChunkMesher.Build(p,revision,cells);result.Token=token;...
+                var result=ChunkMesher.Build(p,revision,cells);result.Token=token;...
+                if(snapshot==null)result.FluidSources=CountFluidSources(cells);
@@ void Apply(ChunkBuild result,Resident c)
             c.Cells=result.Cells;c.Dirty=false;pendingMeshes.Remove(result.Position);
+            if(first)c.FluidSources=result.FluidSources>=0?result.FluidSources:CountFluidSources(c.Cells);
```

- **Why the count stays exact:**
  - `Change()` is the only runtime writer of resident cells. It updates the count in the same synchronous turn.
  - A first `Apply` always comes from a generation job whose cells already include all edits; stale jobs are rejected by revision.
  - Remesh applies replace `Cells` with an equal snapshot, so the count is kept.
  - `Fluids.Registry` is immutable and safe to read from workers.
- **Risks:**
  - A future code path that writes `Resident.Cells` without `Change()` would desynchronize the count. Mitigate with a debug check, under `RR_VALIDATE_WORLD`, that recounts after each remesh.
  - The reach can include pages outside the world's Y range. Those are never resident, so the shortcut correctly declines.
- **Validation:**
  - New `RangedPumpShortcutChecks` (Editor): a fake world implementing both interfaces. Run randomized reaches (empty, one source per corner and layer, unloaded pages, sources appearing mid-scan) with the shortcut enabled and disabled. Assert identical `PumpScanIndex`, `PumpTarget`, `Status`, `Work`, `PumpScanUnloaded` and `PumpRetryTick` sequences over 200 ticks.
  - New `FluidSourceCountChecks`: random edits and flows on a small resident world. Assert `FluidSources == CountFluidSources(Cells)` after every change.
  - Existing `RangedPumpChecks` and native `Verify-RangedPump.ps1`.
  - A2: `RR.RangedPumpPreparation` before and after.

### D2 — Cached scheduler lookup in `CanSimulate` (exact)

- **Why:**
  - `CanSimulate` (`Industry/IndustryScheduling.cs:43`) does a `Dictionary<BlockPos,AutomationComponent>` lookup.
  - `IndustrySimulation.Step` calls it 5–6 times per device per tick: about 9,000 hashed 24-byte keys per tick for the 1,518-assembly fixture.
  - `componentAt` only changes at two places: `RegisterComponent` (line 53) and the publication assignment (line 198).
- **Files:** `Industry/IndustryScheduling.cs`, `Industry/IndustryDefinition.cs` (`MachineState`).

```diff
--- a/Assets/RivetReach/Code/Industry/IndustryScheduling.cs
+++ b/Assets/RivetReach/Code/Industry/IndustryScheduling.cs
@@
-        bool CanSimulate(MachineState machine)=>machine.Eligible&&componentAt.TryGetValue(machine.Position,out var component)&&component.Activity.Active;
+        // componentAt changes only in RegisterComponent and publication; both bump this
+        // version, so a cached activity always equals the dictionary lookup's result.
+        long componentIndexVersion;
+        bool CanSimulate(MachineState machine)
+        {
+            if(!machine.Eligible)return false;
+            if(machine.ComponentIndexVersion!=componentIndexVersion)
+            {
+                machine.ComponentActivity=componentAt.TryGetValue(machine.Position,out var component)?component.Activity:null;
+                machine.ComponentIndexVersion=componentIndexVersion;
+            }
+            return machine.ComponentActivity!=null&&machine.ComponentActivity.Active;
+        }
@@ void RegisterComponent(MachineState machine)
-            {component=new AutomationComponent();components.Add(component);componentAt.Add(machine.Position,component);component.Nodes.Add(machine.Position);WatchComponent(component,machine.Position);}
+            {component=new AutomationComponent();components.Add(component);componentAt.Add(machine.Position,component);componentIndexVersion++;component.Nodes.Add(machine.Position);WatchComponent(component,machine.Position);}
@@ IEnumerable<int> Reconstruct(...)
-            components=publishedComponents;componentAt=publishedPositions;componentPages=publishedPages;
+            components=publishedComponents;componentAt=publishedPositions;componentPages=publishedPages;componentIndexVersion++;
```

```diff
--- a/Assets/RivetReach/Code/Industry/IndustryDefinition.cs
+++ b/Assets/RivetReach/Code/Industry/IndustryDefinition.cs
@@ public sealed partial class MachineState
         public bool Signal,SignalAttached,Source,NextSource,Eligible,FluidConflict;
+        // Scheduler cache owned by IndustrySimulation.CanSimulate; never saved.
+        internal NetworkActivity ComponentActivity;internal long ComponentIndexVersion=-1;
```

- **Why exact:** `AutomationComponent.Activity` is `readonly`. `Suspend` only flips `Activity.Active`, which the cached reference observes. Every change to the position→component mapping bumps the version.
- **Risks:** a future third writer of `componentAt` without a version bump. Keep the bump next to every assignment and assert it in a check.
- **Validation:**
  - New `SchedulerCacheChecks`: random register/remove/invalidate/rebuild/residency sequences on an Editor `IndustrySimulation`. After every operation, assert `CanSimulate` equals a fresh dictionary lookup for every machine (via reflection).
  - Existing `ResidencyInvalidationChecks`, `IndustryChecks` and `MultiblockChecks`; native `Verify-Industry`.
  - A2: `RR.MachineStateSetup` and `RR.MachinePreparation`.

### D3 — Power generation stored on the machine (exact)

- **Why:** `PowerNetworkService.Allocate` (`Industry/NetworkTopology.cs:178-245`) clears and refills a `Dictionary<MachineState,int>` every tick. Each read and write hashes the machine. Measured `RR.PowerPrepare`: 0.47–0.60 ms per tick.
- **Files:** `Industry/NetworkTopology.cs`, `Industry/IndustryDefinition.cs`, `Industry/IndustrySimulation.cs`.

```diff
--- a/Assets/RivetReach/Code/Industry/NetworkTopology.cs
+++ b/Assets/RivetReach/Code/Industry/NetworkTopology.cs
@@ public sealed class PowerNetworkService
-        readonly Dictionary<MachineState,int> generation=new Dictionary<MachineState,int>();
@@ public void Allocate(long tick)
-                generation.Clear();
@@
-                        if(p.Port.Role==PortRole.Output)generation[m]=m.SupplyWatts;
+                        if(p.Port.Role==PortRole.Output)m.GenerationRemaining=m.SupplyWatts;
@@ int AvailableGeneration(NetworkTopology.Group group)
-            foreach(var p in group.PowerOutputs)watts+=generation[p.Machine];
+            foreach(var p in group.PowerOutputs)watts+=p.Machine.GenerationRemaining;
@@ void ConsumeGeneration(NetworkTopology.Group group,int watts)
-            {int take=Math.Min(watts,generation[p.Machine]);generation[p.Machine]-=take;p.Machine.DeliveredWatts+=take;watts-=take;}
+            {int take=Math.Min(watts,p.Machine.GenerationRemaining);p.Machine.GenerationRemaining-=take;p.Machine.DeliveredWatts+=take;watts-=take;}
```

```diff
--- a/Assets/RivetReach/Code/Industry/IndustryDefinition.cs  (MachineState)
+        // Per-tick generation budget shared by every grid this generator faces; never saved.
+        internal int GenerationRemaining;
--- a/Assets/RivetReach/Code/Industry/IndustrySimulation.cs
-        {m.ReceivedWatts=m.SupplyWatts=m.DeliveredWatts=m.BatteryWatts=m.BatteryInputWatts=m.BatteryOutputWatts=0;}
+        {m.ReceivedWatts=m.SupplyWatts=m.DeliveredWatts=m.BatteryWatts=m.BatteryInputWatts=m.BatteryOutputWatts=m.GenerationRemaining=0;}
```

- **Why exact:**
  - Every `PowerOutputs` machine of an active group is set in the preparation loop before it is read. Otherwise the dictionary version would throw, and no audit run has thrown.
  - A generator facing several grids is keyed by machine in both versions, so it shares one budget.
  - Resetting in `ResetTransferredPower` means a hypothetical unset read returns 0 rather than a stale value.
- **Validation:** `GridAllocationChecks`, `RenewablePowerChecks`, `BatteryChecks`, `HandCrankChecks` (domain gate); native `Verify-Renewables`, `Verify-BatteryFill`, `Verify-HandCrank`; A2 `RR.PowerPrepare`.

### D4 — Per-type machine lists (sketch)

- **Why:** `Step` iterates every device in six passes. Several passes only act on a few types: relays, buttons, sensors, boilers, alternators, cranks, renewables, batteries and lamps.
- **Approach:**
  - At publication (`Reconstruct`, the same place `devices` is rebuilt), also build filtered lists in `devices` order: `relays`, `buttons`, `sensors`, `boilers`, `alternators`, `batteries`, `lamps`, `processors`.
  - Passes that only touch a type iterate its list, keeping the `CanSimulate` check. The "reset all" pass keeps iterating `devices`.
- **Exactness:** each pass visits the same machines in the same relative order.
- **Risks:** a new machine type omitted from its list. Generate the lists from `IndustryDefinition` traits rather than hand-written ID checks; this aligns with [CAPABILITIES.md](CAPABILITIES.md).
- **Validation:** the D2 randomized harness, extended to compare full machine state after N ticks with and without lists. Domain gate; native `Verify-Industry`.

### D5 — Skip pipe groups that cannot move anything (sketch; prove before adopting)

- **Why:**
  - `TransferFluids` (`Industry/IndustrySimulation.cs:277-345`, 1.2–1.4 ms per tick) and `TransferConfiguredItems` (`Industry/TransportEndpoints.cs:182-252`, 0.7–1.1 ms every 5 ticks) re-evaluate every active group even when all outputs are full or all sources are empty.
  - The "full outputs" window is exactly that case.
- **Approach:**
  - Give `FluidStorage`, `ItemContainer` and crate stores a `Revision` counter, incremented on every amount, type or slot change.
  - At publication, give each group an immutable endpoint array (storage, role, accepts, enabled).
  - After a group's pass transfers nothing, remember a signature: the sum of its storages' revisions, signal and enable states, and conflict state. Skip the group while the signature is unchanged.
- **Hard parts:**
  - `TransferFluids` resets `FluidConflict` for all devices before re-deriving it, so a skipped group must re-apply its last conflict flags.
  - Item routing rotates start indices by `Tick`. Skipping a pass that would move nothing does not change any state, but this needs proving per path.
  - Receivers' `QueryInput` may depend on state outside the storage revision: machine recipe preference and priorities. Include `ItemInputPriority` and the processing recipe identity in the signature.
- **Validation (mandatory):**
  - A **reference-equivalence harness**: keep the current algorithm as `TransferFluidsReference`/`TransferConfiguredItemsReference` in the Editor assembly.
  - Run both on cloned factories over thousands of randomized ticks (fill, drain, toggle signals, rewire, residency changes). Assert identical storages, statuses, conflict flags and allocation rotations each tick.
  - Existing `FluidRoutingChecks`, `ItemPipeChecks`, `CrateRoutingEdgeChecks` and `ItemInputContractChecks`; native `Verify-Industry`, `Verify-Crates`, `Verify-Multiblocks`.

### D6 — Incremental topology publication (sketch)

- **Why:**
  - `Reconstruct` (`Industry/IndustryScheduling.cs:105-207`) republishes `componentAt`, `componentPages`, `eligible` and `devices` by copying every **unaffected** entry, and re-walks all components.
  - Any local edit or page load/unload costs O(factory), time-sliced over frames, and the affected components stay suspended ("Connecting") meanwhile.
- **Approach:** keep persistent indices and apply deltas. Remove replaced components' positions/pages/members, then add the new components'. Keep the yielded, budgeted union-find for affected nodes only. Merge `devices`/`eligible` with stable ordering: affected removed, `ready` inserted by position order with a merge step instead of a full copy.
- **Risks:** high. Ordering determinism, atomic publication of all four network snapshots and bridge partners, and the "single main-thread publication" rule in the comments.
- **Validation:** `ResidencyInvalidationChecks`, `BridgeChecks`, `MultiblockChecks` and the D2 harness extended to compare published structures against a from-scratch rebuild after every random edit. Native unload/return windows (A2: the 61.66 ms factory scope).

### D7 — Debounced lamp light (decision; after A1)

- **Why:**
  - `IndustrySimulation.Step` fires `LightChanged` whenever a lamp's `Running` flips (`Industry/IndustrySimulation.cs:171-175`). `WorldLighting.LightSourceChanged` then dirties the whole page.
  - In a severe shortage the round-robin residual watt (`NetworkTopology.cs:277-287`) can flip lamps every tick.
  - With one light solve in flight, a continually re-dirtied page also makes its in-flight result stale on arrival, so the backlog never drains.
  - A1 will show whether this happens in practice.
- **Patch (draft):**

```diff
--- a/Assets/RivetReach/Code/Industry/IndustrySimulation.cs
@@
+        const int LampSettleTicks=10;
+        readonly Dictionary<BlockPos,int> lampPending=new Dictionary<BlockPos,int>();
+        public bool LampLit(BlockPos p)=>lampLightStates.TryGetValue(p,out bool lit)&&lit;
@@ machine advance loop
-                    if(m.Definition.Id==IndustryId.Lamp)
-                    {
-                        lampLightStates.TryGetValue(m.Position,out bool wasLit);
-                        if(wasLit!=m.Running){lampLightStates[m.Position]=m.Running;LightChanged?.Invoke(m.Position);}
-                    }
+                    if(m.Definition.Id==IndustryId.Lamp)
+                    {
+                        // First observation (placement/load) applies immediately, as before.
+                        // Later flips must persist for LampSettleTicks before relighting.
+                        bool known=lampLightStates.TryGetValue(m.Position,out bool wasLit);
+                        if(!known){lampLightStates[m.Position]=m.Running;if(m.Running)LightChanged?.Invoke(m.Position);}
+                        else if(wasLit==m.Running)lampPending.Remove(m.Position);
+                        else
+                        {
+                            lampPending.TryGetValue(m.Position,out int stable);
+                            if(++stable<LampSettleTicks)lampPending[m.Position]=stable;
+                            else{lampPending.Remove(m.Position);lampLightStates[m.Position]=m.Running;LightChanged?.Invoke(m.Position);}
+                        }
+                    }
--- a/Assets/RivetReach/Code/Industry/WorldIndustry.cs
-            game.World.MachineLight=p=>{var lamp=Simulation.At(p);return lamp!=null&&lamp.Definition.Id==IndustryId.Lamp&&lamp.Running&&Simulation.IsSimulating(lamp)?(byte)14:(byte)0;};
+            game.World.MachineLight=p=>{var lamp=Simulation.At(p);return lamp!=null&&lamp.Definition.Id==IndustryId.Lamp&&Simulation.LampLit(p)&&Simulation.IsSimulating(lamp)?(byte)14:(byte)0;};
```

Also remove `lampPending` entries where `lampLightStates` entries are removed (`Remove(BlockPos)`), and when a lamp stops simulating.

- **Behaviour:** voxel light (crop growth, hostile spawn light) follows a lamp's power with up to 0.5 s delay. The Unity point light (`IndustryPresentation`) already fades and is unchanged. This is a gameplay-timing change and needs the user's decision.
- **Risks:** a lamp powered for less than 0.5 s never lights the voxel field. A first observation after load is immediate, so lit areas are not dark after loading.
- **Validation:** `LightingChecks`; native `Verify-Lighting` and `Verify-Mobs` (spawn light); a power-shortage window with A1 counters before and after.

---

## 10. E — World data, chunk streaming and lighting

### E1 — `BlockPos.Chunk` by arithmetic shifts (exact)

- **Why:** every world read computes `p.Chunk` with three branching 64-bit divisions (`Core/Coordinates.cs:13-14`). An arithmetic right shift equals floor division by 32 for negative values too: `-1>>5 == -1` and `-33>>5 == -2`.

```diff
--- a/Assets/RivetReach/Code/Core/Coordinates.cs
+++ b/Assets/RivetReach/Code/Core/Coordinates.cs
@@
-        public ChunkPos Chunk => new ChunkPos(FloorDiv(X, 32), (int)FloorDiv(Y, 32), FloorDiv(Z, 32));
+        // Arithmetic shifts are floor division by 32, including negative coordinates.
+        public ChunkPos Chunk => new ChunkPos(X >> 5, Y >> 5, Z >> 5);
```

- **Risks:** none known. `FloorDiv` stays for other divisors.
- **Validation:** new `CoordinateChecks`. Compare against `FloorDiv` for every value in ±70,000, around ±2³¹ for Y, around the `HorizontalLimit` (±10⁹) for X/Z, and the long extremes the checked `Offset` allows. Domain gate (`DomainChecks`, `TerrainGenerationChecks`).

### E2 — Resident-first world reads; one lookup in collision and raycasts (exact)

- **Why:**
  - `VoxelWorld.Get` (`World/VoxelWorld.cs:104-110`) looks up the per-page edit dictionary **before** the resident page, and `TryRead` (198-207) does both lookups.
  - `Solid` (111) adds `Ready` and then `Get`. `Overlaps` (585-591) calls `Solid`, `Ready` and `Get` for every candidate cell. `Move` calls `Overlaps` for each axis, step and 9-step bisection.
  - `Trace` calls `Ready` and `Get` per traversed cell.
  - These paths serve collision, targeting, fluids, mobs, pumps and lighting.
- **Invariant that makes this exact:**
  - A resident page's `Cells` always include every edit. `Change()` is the only runtime writer of `edits` and writes the resident cells and halos in the same call.
  - `ReadSave` requires an empty world (`WorldSaveState.cs:22`).
  - Generation jobs apply edits on the worker, and any edit during a job bumps the revision so the stale result is rejected.
  - Remeshing already relies on this invariant (comment at `VoxelWorld.cs:454`).

```diff
--- a/Assets/RivetReach/Code/World/VoxelWorld.cs
+++ b/Assets/RivetReach/Code/World/VoxelWorld.cs
@@
         public byte Get(BlockPos p)
         {
-            if(edits.TryGetValue(p.Chunk,out var e)&&e.TryGetValue(p.Index,out byte b))return b;
-            if(chunks.TryGetValue(p.Chunk,out var c)&&c.Cells!=null)
-            {int i=p.Index;return c.Cells[ChunkMesher.Index(i%32,i/32%32,i/1024)];}
-            return GeneratorFor(p.Chunk).At(p);
+            var key=p.Chunk;
+            // Resident cells already contain every edit (see Change and the revision checks).
+            if(chunks.TryGetValue(key,out var c)&&c.Cells!=null)return ResidentCell(key,c,p.Index);
+            if(edits.TryGetValue(key,out var e)&&e.TryGetValue(p.Index,out byte b))return b;
+            return GeneratorFor(key).At(p);
         }
-        public bool Solid(BlockPos p) => !Ready(p)||BlockId.Solid(Get(p))&&!(IsOpenMachine?.Invoke(p)??false);
+        byte ResidentCell(ChunkPos key,Resident c,int index)
+        {
+            byte value=c.Cells[ChunkMesher.Index(index&31,(index>>5)&31,index>>10)];
+#if RR_VALIDATE_WORLD
+            if(edits.TryGetValue(key,out var page)&&page.TryGetValue(index,out byte edited)&&edited!=value)
+                throw new InvalidOperationException($"Resident cell diverged from its edit at {key.Min} index {index}");
+#endif
+            return value;
+        }
+        public bool Solid(BlockPos p) => !TryRead(p,out byte id)||SolidResident(p,id);
+        bool SolidResident(BlockPos p,byte id)=>BlockId.Solid(id)&&!(IsOpenMachine?.Invoke(p)??false);
@@ public bool TryRead(BlockPos p,out byte id)
             var key=p.Chunk;id=0;
             if(!chunks.TryGetValue(key,out var resident)||resident.Cells==null)return false;
-            int index=p.Index;
-            // Preserve edit authority and the closed nonresident boundary while
-            // avoiding Ready/Get's repeated chunk and coordinate lookups.
-            if(edits.TryGetValue(key,out var page)&&page.TryGetValue(index,out id))return true;
-            id=resident.Cells[ChunkMesher.Index(index&31,(index>>5)&31,index>>10)];return true;
+            id=ResidentCell(key,resident,p.Index);return true;
         }
@@ bool Trace(...)
-                if(!Ready(cell)){if(solidsOnly){hit=new BlockSelectionHit(cell,0,start+direction*distance,face,distance);return true;}return false;}
-                byte b=Get(cell);var fluid=Fluids.Registry.Get(b);
-                if(solidsOnly?Solid(cell):b!=0&&(fluid==null||fluidSources&&fluid.IsSource(b)))
+                if(!TryRead(cell,out byte b)){if(solidsOnly){hit=new BlockSelectionHit(cell,0,start+direction*distance,face,distance);return true;}return false;}
+                var fluid=Fluids.Registry.Get(b);
+                if(solidsOnly?SolidResident(cell,b):b!=0&&(fluid==null||fluidSources&&fluid.IsSource(b)))
@@ public bool Overlaps(Vector3 feet,float width,float height)
-            {var cell=new BlockPos(x,y,z);if(Solid(cell)&&(!Ready(cell)||OccupiesBlock(feet,width,height,cell,Get(cell))))return true;}
+            {
+                var cell=new BlockPos(x,y,z);
+                if(!TryRead(cell,out byte id))return true; // unready terrain is solid, as before
+                if(SolidResident(cell,id)&&OccupiesBlock(feet,width,height,cell,id))return true;
+            }
```

- **Equivalence of the rewritten predicates:**
  - `Overlaps`: if the cell is not ready, `Solid` was true and `!Ready` was true, so it returns true. If ready, it is `Solid(id) && !open && OccupiesBlock(..., id)`.
  - `Trace`: the same reasoning with the single read.
- **Risks:** a future writer that bypasses `Change()`. The `RR_VALIDATE_WORLD` define turns any divergence into an exception during tests.
- **Validation:**
  - New `WorldReadChecks`. On an Editor `VoxelWorld` fixture with random edits, residency changes and in-flight jobs, assert `Get`, `TryRead`, `Solid`, `Overlaps` and the `Raycast`/`RaycastSolid` hits equal a frozen copy of the old implementations over randomized queries.
  - Run the full domain gate and the native movement/interaction suites (`Verify-Player`, `MovementInteractionVerification`, `Verify-Fluids`, `Verify-Mobs`) once with `RR_VALIDATE_WORLD` defined.
  - A2: `RR.SimulationAdvance` and the player-movement share of main-thread time.

### E3 — Precomputed block-trait tables (exact)

- **Why:**
  - `BlockId.Solid`, `Opaque`, `Placeable` and `Crop` (`Core/ItemRegistry.cs:31-46`) are long comparison chains. `Crop` loops over `CropRules.Definitions`.
  - They run per cell in collision, lighting heights, sky columns, generation and mesh classification.
  - All inputs are constants or immutable static registries. The mesher already precomputes `terrainSolid` this way.

```diff
--- a/Assets/RivetReach/Code/Core/ItemRegistry.cs
+++ b/Assets/RivetReach/Code/Core/ItemRegistry.cs
@@ public static class BlockId
-        public static bool Crop(byte id)=>CropRules.For(id)!=null;
+        // Pure functions of constant ids and immutable registries, evaluated once.
+        static class Traits
+        {
+            public static readonly bool[] Crop=Table(id=>CropRules.For(id)!=null);
+            public static readonly bool[] Solid=Table(SolidRule);
+            public static readonly bool[] Opaque=Table(OpaqueRule);
+            public static readonly bool[] Placeable=Table(PlaceableRule);
+            static bool[] Table(Func<byte,bool> rule){var table=new bool[256];for(int i=0;i<256;i++)table[i]=rule((byte)i);return table;}
+        }
+        public static bool Crop(byte id)=>Traits.Crop[id];
@@
-        public static bool Placeable(byte id)=>BuildingBlocks.AddedItem(id)||...||id>=CoalBlock&&id<=DiamondBlock;
-        public static bool Solid(byte id)=>id!=Air&&id!=Torch&&id!=Sapling&&!IndustryId.Thin(id)&&!Crop(id)&&!Fluids.IsFluid(id);
-        public static bool Opaque(byte id)=>id!=IndustryId.Glass&&...&&Solid(id)&&...&&id!=IndustryId.DoorUpper;
+        public static bool Placeable(byte id)=>Traits.Placeable[id];
+        public static bool Solid(byte id)=>Traits.Solid[id];
+        public static bool Opaque(byte id)=>Traits.Opaque[id];
+        static bool PlaceableRule(byte id)=>BuildingBlocks.AddedItem(id)||...||id>=CoalBlock&&id<=DiamondBlock; // unchanged body
+        static bool SolidRule(byte id)=>id!=Air&&id!=Torch&&id!=Sapling&&!IndustryId.Thin(id)&&CropRules.For(id)==null&&!Fluids.IsFluid(id);
+        static bool OpaqueRule(byte id)=>id!=IndustryId.Glass&&!CrateId.Part(id)&&!BedId.Part(id)&&SolidRule(id)&&id!=MobSpawner&&id!=Leaves&&!IndustryId.Placed(id)&&id!=IndustryId.DoorUpper;
```

The rules call each other's *rules*, not tables, so there is no static-initialization ordering hazard. Type initialization is thread-safe for the worker threads.

- **Risks:** a future predicate input that is not immutable (for example a data-driven block registry) must rebuild the tables when that registry is replaced, as [CAPABILITIES.md](CAPABILITIES.md) already requires for catalogs.
- **Validation:**
  - **Before** changing the code, dump `Solid`, `Opaque`, `Crop` and `Placeable` for ids 0–255 to `Assets/RivetReach/Editor/Fixtures/block-traits-1811ac1.txt`.
  - New `BlockTraitChecks` compares against it.
  - Domain gate (`DomainChecks`, `AlphaGuidanceChecks`, `StarterStationBuild` checks); native `Verify-Player`.

### E4 — Direct column walk on light invalidation (exact)

- **Why:** an opacity edit iterates **all** resident page keys to find its column (`World/WorldLighting.cs:92`). That is about 2,000 keys per mined or placed block at radius 10.

```diff
--- a/Assets/RivetReach/Code/World/WorldLighting.cs
+++ b/Assets/RivetReach/Code/World/WorldLighting.cs
@@ void LightingChanged(BlockPos p,byte before,byte after)
-                foreach(var key in chunks.Keys)if(key.X==p.Chunk.X&&key.Z==p.Chunk.Z)DirtyLight(key);
+                var chunk=p.Chunk;
+                // DirtyLight ignores non-resident pages; visit the column's fixed vertical range.
+                for(int y=TerrainGenerator.MinY>>5;y<=TerrainGenerator.MaxY>>5;y++)DirtyLight(new ChunkPos(chunk.X,y,chunk.Z));
```

- **Exactness:** the resident Y range is clamped to `[MinY/32, MaxY/32]` by `Demand`, and loader tickets are world chunks. `DirtyLight` is idempotent and order-insensitive (set add plus revision increment).
- **Possible follow-up (not exact in intermediate frames):** dirty only pages at or below the edited page. Sky columns cannot change above the edit, and border propagation already re-dirties pages whose border light changes. This may briefly delay block-light updates in the page above, so it needs `LightingChecks` plus capture review.
- **Validation:** `LightingChecks`; native `Verify-Lighting`; mining at a column with A1 counters.

### E5 — Terrain worker count scales with cores

- **Why:**
  - `VoxelWorld.Update` dispatches at most two chunk jobs (`work.Count<2`, `World/VoxelWorld.cs:329-334`).
  - The terrain worker is the largest scope in the streaming and unload/return windows, and the pending-terrain queue peaked at 2,023.
  - The i7-10750H has 12 hardware threads.

```diff
--- a/Assets/RivetReach/Code/World/VoxelWorld.cs
@@
+        // Leave two cores for the main and render threads; never fewer than the original two.
+        public static readonly int TerrainWorkers=Math.Clamp(Environment.ProcessorCount-2,2,6);
@@ void Update()
-            if(work.Count<2)
+            if(work.Count<TerrainWorkers)
@@
-                while(work.Count<2)
+                while(work.Count<TerrainWorkers)
--- a/Assets/RivetReach/Code/World/ChunkMesher.cs
-        const int ScratchLimit=3;
+        // Workers plus the synchronous edit path keep their scratch warm.
+        public static readonly int ScratchLimit=VoxelWorld.TerrainWorkers+1;
--- a/Assets/RivetReach/Editor/PerformanceChecks.cs
-            if(pooled>3||retainedBytes>64L*1024*1024||retainedBytes!=accounted)throw ...
+            if(pooled>ChunkMesher.ScratchLimit||retainedBytes>64L*1024*1024||retainedBytes!=accounted)throw ...
```

- **Risks:**
  - More worker threads compete with Unity's job workers and the render thread. On 4-core machines the clamp keeps two.
  - Each dispatch rescans `pendingMeshes` (O(pending) per dispatch). With 6 workers that is up to 6 scans per frame; if A2 shows cost, sort once per frame.
  - Memory: more concurrent 39 KB generation buffers and mesh scratch (the 64 MB scratch cap is unchanged).
- **Validation:** `ChunkWorkPriorityChecks` and `PerformanceChecks` (domain gate); native `Verify-Terrain` and `Verify-Performance`; A2 streaming and unload/return windows, including the oldest queue age the audit asked for.

### E6 — Concurrent light solves (after A1, D7, E4)

- **Why:** `WorldLighting` keeps one `Task<LightResult> lightWork`, so at most one page solve is in flight. At 60 FPS that drains about 60 pages per second, against measured backlogs of 1,700–1,900.
- **Approach (draft):**

```csharp
readonly List<(Task<LightResult> task,ChunkPos position)> lightJobs=new List<(Task<LightResult>,ChunkPos)>();
static readonly int LightWorkers=Math.Clamp(Environment.ProcessorCount/4,1,3);
public int PendingLightChunks=>dirtyLights.Count+lightJobs.Count;
// AdvanceLighting: publish every completed job using the current single-job body
// (keyed by its own position), then launch while lightJobs.Count<LightWorkers,
// choosing the nearest dirty page that is not in flight and not adjacent
// (|dx|,|dy|,|dz| <= 1) to an in-flight page.
```

- **Risks:** chunk-wise relaxation after light *removal* can depend on solve order. A concurrent order may settle on a different, still self-consistent field in rare loops, as the sequential order already can. Keep adjacent pages out of simultaneous solves.
- **Validation:** `LightingChecks`; native `Verify-Lighting` scenarios (torch removal, cave entrance, lamp shutdown) compared against the sequential build's light fields; A2 light-queue peak and drain time.

### E7 — Packed terrain vertices and 16-bit indices (near-exact)

- **Why:**
  - `TerrainVertex` is 40 bytes (`World/ChunkMeshUpload.cs:8-14`: Float32 position, normal, UV and tile), and indices are always 32-bit (line 50).
  - Terrain mesh upload peaked at 10.85 ms per observation.
- **Patch:** keep positions Float32, so geometry and seams are bit-identical. Store normal, UV and tile as Float16.
  - Axis normals, integer UVs up to 32, half-slab 0.5 and tile indices are exact in half precision.
  - Plant-card UVs and normals round to about 0.05%.

```diff
--- a/Assets/RivetReach/Code/World/ChunkMeshUpload.cs
+++ b/Assets/RivetReach/Code/World/ChunkMeshUpload.cs
@@
     [StructLayout(LayoutKind.Sequential)]
     public struct TerrainVertex
     {
-        public Vector3 Position,Normal;
-        public Vector2 UV,Tile;
-        public TerrainVertex(Vector3 position,Vector3 normal,Vector2 uv,Vector2 tile)
-        {Position=position;Normal=normal;UV=uv;Tile=tile;}
+        public Vector3 Position;
+        ushort normalX,normalY,normalZ,normalW,u,v,tileX,tileY; // Float16: 28-byte stride
+        public TerrainVertex(Vector3 position,Vector3 normal,Vector2 uv,Vector2 tile)
+        {
+            Position=position;normalX=Mathf.FloatToHalf(normal.x);normalY=Mathf.FloatToHalf(normal.y);normalZ=Mathf.FloatToHalf(normal.z);normalW=0;
+            u=Mathf.FloatToHalf(uv.x);v=Mathf.FloatToHalf(uv.y);tileX=Mathf.FloatToHalf(tile.x);tileY=Mathf.FloatToHalf(tile.y);
+        }
+        public Vector3 Normal=>new Vector3(Mathf.HalfToFloat(normalX),Mathf.HalfToFloat(normalY),Mathf.HalfToFloat(normalZ));
+        public Vector2 UV=>new Vector2(Mathf.HalfToFloat(u),Mathf.HalfToFloat(v));
+        public Vector2 Tile=>new Vector2(Mathf.HalfToFloat(tileX),Mathf.HalfToFloat(tileY));
     }
@@
         static readonly VertexAttributeDescriptor[] terrainLayout={
             new VertexAttributeDescriptor(VertexAttribute.Position,VertexAttributeFormat.Float32,3),
-            new VertexAttributeDescriptor(VertexAttribute.Normal,VertexAttributeFormat.Float32,3),
-            new VertexAttributeDescriptor(VertexAttribute.TexCoord0,VertexAttributeFormat.Float32,2),
-            new VertexAttributeDescriptor(VertexAttribute.TexCoord1,VertexAttributeFormat.Float32,2)};
+            new VertexAttributeDescriptor(VertexAttribute.Normal,VertexAttributeFormat.Float16,4),
+            new VertexAttributeDescriptor(VertexAttribute.TexCoord0,VertexAttributeFormat.Float16,2),
+            new VertexAttributeDescriptor(VertexAttribute.TexCoord1,VertexAttributeFormat.Float16,2)};
+        static ushort[] shortIndices=new ushort[0]; // main thread only (Apply and held/dropped item meshes)
@@ static Mesh Upload<T>(...)
-            mesh.SetIndexBufferParams(indices.Length,IndexFormat.UInt32);
-            if(indices.Length>0)mesh.SetIndexBufferData(indices,0,0,indices.Length,flags);
+            bool small=vertices.Length<=65536;
+            mesh.SetIndexBufferParams(indices.Length,small?IndexFormat.UInt16:IndexFormat.UInt32);
+            if(indices.Length>0)
+            {
+                if(small)
+                {
+                    if(shortIndices.Length<indices.Length)shortIndices=new ushort[Mathf.NextPowerOfTwo(indices.Length)];
+                    for(int i=0;i<indices.Length;i++)shortIndices[i]=(ushort)indices[i];
+                    mesh.SetIndexBufferData(shortIndices,0,0,indices.Length,flags);
+                }
+                else mesh.SetIndexBufferData(indices,0,0,indices.Length,flags);
+            }
--- a/Assets/RivetReach/Editor/PerformanceChecks.cs
-            if(Marshal.SizeOf<TerrainVertex>()!=40||Marshal.SizeOf<FluidVertex>()!=40)throw new Exception("Packed vertex stride changed");
+            if(Marshal.SizeOf<TerrainVertex>()!=28||Marshal.SizeOf<FluidVertex>()!=40)throw new Exception("Packed vertex stride changed");
```

- **Shader impact:** none. The input assembler converts Float16 to the `float3 normalOS:NORMAL` / `float2:TEXCOORDn` declarations in `Terrain.shader`, `HeldBlock.shader`, `ConnectedGlass.shader` and the URP Lit `UsePass` shadow/depth passes. Shader equality checks such as `i.tile==6` stay exact.
- **Expected:** about 30% less vertex data and 50% less index data per upload and in VRAM. (Float32 position + SNorm8×4 normal would reach 24 bytes, if Unity accepts SNorm8 normals on all target APIs.)
- **Risks:**
  - Unity rejects unsupported attribute formats at `SetVertexBufferParams`; any failure shows immediately.
  - Plant-card UV rounding could shift a texel edge. The fluid upload path also gets 16-bit indices.
  - Tests reading `Vertices[i].Normal` and `.Tile.x` keep compiling through the properties (`DomainChecks`, `OrchardBuild`).
- **Validation:**
  - Shader image equivalence plus native captures of terrain, crops, saplings, slabs, glass, held blocks and dropped blocks at fixed viewpoints, with pixel diffs.
  - `DomainChecks` winding test; `PerformanceChecks`.
  - A2: `RR.TerrainMeshUpload` and `RR.TerrainUpload`.

### E8 — Allocation-free tree lookup in `TerrainGenerator.At` (exact)

- **Why:** `At` (`World/TerrainGenerator.cs:129`) iterates `Trees(p.X,p.Z,p.X,p.Z)`, a `yield` iterator that allocates on every point query. `At` serves non-resident `VoxelWorld.Get`, sky/light column scans (on workers) and halo fills.

```diff
--- a/Assets/RivetReach/Code/World/TerrainGenerator.cs
+++ b/Assets/RivetReach/Code/World/TerrainGenerator.cs
@@ public byte At(BlockPos p)
             byte result=0;
-            foreach(var tree in Trees(p.X,p.Z,p.X,p.Z))
-            {byte id=tree.At(p);if(id==BlockId.Log)return id;if(id==BlockId.Leaves)result=id;}
+            // Same candidate cells and order as Trees(p.X,p.Z,p.X,p.Z), without an iterator per query.
+            long z0=BlockPos.FloorDiv(p.Z-CanopyRadius,TreeSpacing),z1=BlockPos.FloorDiv(p.Z+CanopyRadius,TreeSpacing);
+            long x0=BlockPos.FloorDiv(p.X-CanopyRadius,TreeSpacing),x1=BlockPos.FloorDiv(p.X+CanopyRadius,TreeSpacing);
+            for(long z=z0;z<=z1;z++)for(long x=x0;x<=x1;x++)
+            {
+                if(!TryTree(x,z,out var tree))continue;
+                byte id=tree.At(p);if(id==BlockId.Log)return id;if(id==BlockId.Leaves)result=id;
+            }
```

- **Validation:** `TerrainGenerationChecks` (domain gate) plus the [terrain harness](../Tools/TerrainGenerationHarness.cs). Hash generated chunks for several seeds before and after; they must be byte-identical.

### E9 — Allocation-free fluid schedule (exact; same save bytes)

- **Why:**
  - `FluidSimulation.Step` (`Fluids/FluidSimulation.cs:63-69`) calls `due.GetEnumerator()` on a `SortedDictionary` for every processed cell (up to 512 per tick, 20 ticks per second). It also allocates a new `Queue` per due tick.
  - The sorted-dictionary enumerator allocates its internal stack, which adds GC pressure exactly during floods.
- **Patch:** a small binary min-heap of due ticks, a dictionary of FIFO queues, and pooled queues. Processing order is identical: ascending tick, FIFO within a tick, same budget.

```diff
--- a/Assets/RivetReach/Code/Fluids/FluidSimulation.cs
+++ b/Assets/RivetReach/Code/Fluids/FluidSimulation.cs
@@
-        readonly SortedDictionary<long,Queue<BlockPos>> due=new SortedDictionary<long,Queue<BlockPos>>();
+        // Ascending due ticks without enumerator allocations; FIFO within each tick.
+        readonly TickHeap dueTicks=new TickHeap();
+        readonly Dictionary<long,Queue<BlockPos>> due=new Dictionary<long,Queue<BlockPos>>();
+        readonly Stack<Queue<BlockPos>> spareQueues=new Stack<Queue<BlockPos>>();
+        sealed class TickHeap
+        {
+            long[] items=new long[64];int count;
+            public int Count=>count;public long Min=>items[0];
+            public void Push(long value)
+            {if(count==items.Length)Array.Resize(ref items,count*2);int i=count++;while(i>0){int parent=(i-1)>>1;if(items[parent]<=value)break;items[i]=items[parent];i=parent;}items[i]=value;}
+            public void Pop()
+            {long last=items[--count];int i=0;while(true){int child=i*2+1;if(child>=count)break;if(child+1<count&&items[child+1]<items[child])child++;if(items[child]>=last)break;items[i]=items[child];i=child;}if(count>0)items[i]=last;}
+            public long[] Sorted(){var copy=new long[count];Array.Copy(items,copy,count);Array.Sort(copy);return copy;}
+        }
@@ public void Wake(BlockPos p,int delay=5)
-            long when=tick+delay;if(!due.TryGetValue(when,out var queue)){queue=new Queue<BlockPos>();due.Add(when,queue);}queue.Enqueue(p);
+            long when=tick+delay;
+            if(!due.TryGetValue(when,out var queue)){queue=spareQueues.Count>0?spareQueues.Pop():new Queue<BlockPos>();due.Add(when,queue);dueTicks.Push(when);}
+            queue.Enqueue(p);
@@ public void Step(IFluidWorld world)
-            while(LastWork<WorkBudget&&due.Count>0)
-            {
-                var iterator=due.GetEnumerator();iterator.MoveNext();var entry=iterator.Current;iterator.Dispose();
-                if(entry.Key>tick)break;
-                var p=entry.Value.Dequeue();if(entry.Value.Count==0)due.Remove(entry.Key);scheduled.Remove(p);LastWork++;
-                Evaluate(world,p);
-            }
+            while(LastWork<WorkBudget&&dueTicks.Count>0)
+            {
+                long when=dueTicks.Min;if(when>tick)break;
+                var queue=due[when];var p=queue.Dequeue();
+                if(queue.Count==0){due.Remove(when);dueTicks.Pop();spareQueues.Push(queue);}
+                scheduled.Remove(p);LastWork++;
+                Evaluate(world,p);
+            }
--- a/Assets/RivetReach/Code/Persistence/WorldSaveState.cs   (FluidSimulation partial)
-            w.Write(tick);w.Write(due.Count);foreach(var job in due){w.Write(job.Key);w.Positions(job.Value);}
+            w.Write(tick);w.Write(due.Count);foreach(long when in dueTicks.Sorted()){w.Write(when);w.Positions(due[when]);}
@@
-            for(int i=0;i<n;i++){long when=r.Long();var positions=r.Positions();due.Add(when,new Queue<BlockPos>(positions));foreach(...)}
+            for(int i=0;i<n;i++){long when=r.Long();var positions=r.Positions();due.Add(when,new Queue<BlockPos>(positions));dueTicks.Push(when);foreach(...)}
```

- **Why exact:** `Evaluate` only schedules at `tick+delay` with `delay ≥ 1`, so it never re-adds the key currently being drained. Writing keys in ascending order reproduces the `SortedDictionary` save order byte for byte.
- **Validation:**
  - New `FluidScheduleChecks`: a frozen copy of the old scheduler and the new one, driven by identical random wakes and budgets, must produce identical evaluation sequences.
  - Saves written by both versions must be byte-identical.
  - Existing `FluidChecks`, `LavaChecks` and `SaveCompatibilityChecks`; native `Verify-Fluids`, `Verify-Lava` and the fluid stress scenario.

### E10 — Pooled worker buffers (sketch)

- **Where:**
  - Remesh snapshots: `(byte[])c.Cells.Clone()` (`VoxelWorld.Launch`; 39 KB per remesh, old arrays become garbage).
  - Mesh results: `new TerrainVertex[...]` and `indices.ToArray()` (`ChunkMesher.Build`).
  - Light snapshots: a cells clone, six border arrays, 32 KB of emissions and a dictionary per solve (`WorldLighting.LaunchLight`).
- **Approach:** `ArrayPool<T>` with explicit lengths carried in `ChunkBuild`/`LightResult`, returned after upload or publication.
- **Hard part:** resident `Cells` arrays are shared with `ChunkReady` listeners. Audit `MobSpawners.ScanSpawners` and `WorldSurvival.RegisterWild` before pooling `Cells`; mesh and light buffers are safer first targets.
- **Validation:** `PerformanceChecks` (published buffers never alias another job — already tested); A2 with allocation counters, once `AllocationCounterSupported` is available or through a Development-build profiler.

### E11 — Jobs + Burst generation, meshing and lighting (long term)

- **Why:** generation, greedy meshing and the light solve are tight integer loops, well suited to Burst, and `Mesh.AllocateWritableMeshData` can build meshes off the main thread.
- **Constraints:**
  - Port algorithms exactly. Generator output must stay bit-identical; the hashes and the double-precision noise need care.
  - Packages `com.unity.burst`, `com.unity.collections` and `com.unity.mathematics` are Unity Companion License. Record them in [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md) per the licensing rules.
- **Validation:** generator chunk hashes across seeds; mesh digests (`PerformanceChecks`); light-field comparisons; A2 streaming.

---

## 11. F — Events, creatures and presentation

### F1 — Old/new IDs on block events; filtered listeners (exact)

- **Why:**
  - `VoxelWorld.BlockChanged` fires for every change, including up to 512 fluid cells per tick and grass/crop growth. It has 11 subscribers.
  - `BedPresentation.Changed` (`Beds/BedPresentation.cs:14`) and `StarterStationPresentation.Changed` (`Survival/StarterStationPresentation.cs:28`) reset their 0.25 s throttle on **any** change. During water flow they therefore rebuild every frame; `StarterStationPresentation` walks every station, 230 in the stress fixture.
  - Other listeners re-read the world to learn what changed.
- **Patch:**

```diff
--- a/Assets/RivetReach/Code/World/VoxelWorld.cs
@@
         public event Action<BlockPos> BlockChanged;
+        // Same moments as BlockChanged, with the replaced and new ids for cheap filtering.
+        public event Action<BlockPos,byte,byte> BlockReplaced;
@@ bool Change(...)
-            if(!immediate){BlockChanged?.Invoke(p);return true;}
+            if(!immediate){BlockReplaced?.Invoke(p,expected,replacement);BlockChanged?.Invoke(p);return true;}
@@
-            LastEditMeshMs=...;BlockChanged?.Invoke(p);return true;
+            LastEditMeshMs=...;BlockReplaced?.Invoke(p,expected,replacement);BlockChanged?.Invoke(p);return true;
--- a/Assets/RivetReach/Code/Beds/BedPresentation.cs
-        public void Initialize(Expedition game){...world.BlockChanged+=Changed;}
-        void Changed(BlockPos cell){nextRefresh=0;}
+        public void Initialize(Expedition game){...world.BlockReplaced+=Changed;}
+        void Changed(BlockPos cell,byte before,byte after){if(BedId.Part(before)||BedId.Part(after))nextRefresh=0;}
         (OnDestroy: unsubscribe BlockReplaced)
--- a/Assets/RivetReach/Code/Survival/StarterStationPresentation.cs
-            game.World.OriginShifted+=Shift;game.World.BlockChanged+=Changed;
-        void Changed(BlockPos position){nextRefresh=0;}
+            game.World.OriginShifted+=Shift;game.World.BlockReplaced+=Changed;
+        void Changed(BlockPos position,byte before,byte after){if(StarterStationVisuals.UsesModel(before)||StarterStationVisuals.UsesModel(after))nextRefresh=0;}
```

- **Behaviour:** both presentations still refresh every 0.25 s as before. Bed and station placement or removal still refreshes on the next frame. Only unrelated changes stop forcing per-frame rebuilds.
- **Follow-ups (same pattern):** `WorldIndustry.Changed`, `WorldSurvival.Changed`, `WorldCrates.Changed` and `FactoryDistancePresentation.Changed` can early-out on `before/after` instead of re-reading. `DroppedItems.BlockChanged` can skip work when no pile lies within the changed cell's 3×5×3 neighbourhood (keep a per-frame bounding box of pile cells).
- **Validation:** `BedChecks` and starter-station checks (domain gate); native `Verify-Beds`, `Verify-StarterStations`, `Verify-Fluids` (flow near a bed and a furnace, screenshot); A2 `RR.MachineViews` in the fluid stress scenario.

### F2 — Chicken views

- **Why:**
  - `RR.AnimalViews` costs 1.1–1.6 ms per frame for 64 chickens.
  - `ChickenView.Present` (`Animals/ChickenView.cs:40-47`) sets a `MaterialPropertyBlock` on every renderer every frame. That is a native call per renderer, and with the WorldLit shader it takes those renderers off the SRP Batcher path.
  - Each view evaluates its own `PlayableGraph` synchronously.
  - `Initialize` calls `Resources.LoadAll<AnimationClip>` for every new view, and views are destroyed and recreated when chickens leave or re-enter the 128-active set (`Animals/PassiveSystem.cs:61-68`).
- **F2a — tint only on change; clear it for white (exact):** `Chicken.mat` `_BaseColor` is (1,1,1,1).

```diff
--- a/Assets/RivetReach/Code/Animals/ChickenView.cs
+++ b/Assets/RivetReach/Code/Animals/ChickenView.cs
@@
+        static readonly int BaseColor=Shader.PropertyToID("_BaseColor");
+        Color shownTint=Color.white;bool tintShown;
@@ public void Present(ChickenState c,float dt,BlockPos origin)
             Play(c.PeckTicks>0?2:c.PathIndex<c.Path.Count?1:0,dt);flash=Mathf.Max(0,flash-dt);
-            properties.SetColor("_BaseColor",flash>0?new Color(1.6f,.65f,.45f):c.LoveTicks>0?new Color(1.15f,.85f,.9f):Color.white);
-            foreach(var r in renderers)r.SetPropertyBlock(properties);
+            var tint=flash>0?new Color(1.6f,.65f,.45f):c.LoveTicks>0?new Color(1.15f,.85f,.9f):Color.white;
+            if(tintShown&&tint==shownTint)return;
+            shownTint=tint;tintShown=true;
+            // White equals the shared material; clearing keeps ordinary chickens SRP-batched.
+            if(tint==Color.white){foreach(var r in renderers)r.SetPropertyBlock(null);return;}
+            properties.SetColor(BaseColor,tint);foreach(var r in renderers)r.SetPropertyBlock(properties);
```

- **F2b — cache imported clips (exact):**

```diff
-            var imported=Resources.LoadAll<AnimationClip>(path);string[] names={"Idle","Walk","Peck","Death"};
+            var imported=Clips(path);string[] names={"Idle","Walk","Peck","Death"};
@@
+        static readonly System.Collections.Generic.Dictionary<string,AnimationClip[]> clipCache=new System.Collections.Generic.Dictionary<string,AnimationClip[]>();
+        static AnimationClip[] Clips(string path){if(!clipCache.TryGetValue(path,out var clips))clipCache[path]=clips=Resources.LoadAll<AnimationClip>(path);return clips;}
```

- **F2c — skip pose evaluation while no renderer is visible (visual; review):**
  - In `Play`, keep `clock` advancing but call `SetTime`/`graph.Evaluate(0)` only if any renderer `isVisible` (which includes shadow casting), and at least every 0.25 s otherwise.
  - Risk: a one-frame stale pose when a chicken enters view.
- **F2d — allocation-free passive helpers (exact):**
  - `PassiveSystem.IsFeed` (LINQ `Any` with a captured id, called per chicken per tick): use a `foreach` returning on the first match.
  - `Think`'s `active.FirstOrDefault(...)`: an explicit loop.
  - `RefreshActive`'s `previous`/`Contains` O(n²): a `HashSet<ChickenState>`.
  - Pool `ChickenView` objects per adult/chick model instead of `Destroy` plus re-`Instantiate`. Keep graph teardown on real death or removal.
- **Validation:** `ChickenChecks` and `WorldLitBatchingChecks` (domain gate); native `Verify-Chickens` (breeding, panic flash, love tint, death); Frame Debugger SRP-batch view; A2 `RR.AnimalViews`.

### F3 — Remove duplicate passive target selection

- **Why:** `PassiveSystem.Update` (`Animals/PassiveSystem.cs:59`) runs a full `SelectInteraction` (voxel ray plus both entity sets) every frame. `FirstPersonPlayer.TargetAndMine` already resolves the same target each frame in Play mode and calls `PassiveTargets.Interact`.

```diff
--- a/Assets/RivetReach/Code/Animals/PassiveSystem.cs
-            if(game.Mode!=ScreenMode.Play||game.Player.Inspecting)Target=null;
-            else {var eye=game.Player.Camera.transform;Target=game.SelectInteraction(eye.position,eye.forward,3.2f,out var hit)&&ReferenceEquals(hit.Source,this)?hit.Entity.Target as ChickenState:null;}
+            // FirstPersonPlayer.TargetAndMine resolves the shared target once per frame and calls Interact.
+            if(game.Mode!=ScreenMode.Play||game.Player.Inspecting)Target=null;
--- a/Assets/RivetReach/Code/Player/FirstPersonPlayer.cs  (TargetAndMine, storage-emptying early return)
-            {MiningProgress=0;HasTarget=false;if(Mouse.current.leftButton.wasPressedThisFrame)Game.TryEmptySelectedStorage();return;}
+            {MiningProgress=0;HasTarget=false;Game.Mobs?.Interact(null,false,false,false);Game.PassiveTargets?.Interact(null,false,false,false);if(Mouse.current.leftButton.wasPressedThisFrame)Game.TryEmptySelectedStorage();return;}
```

- **Behaviour edge:**
  - While holding an Empty Bucket, the player's selection treats liquid sources as blocking. A chicken behind a source then shows no label; previously the passive system's own ray ignored sources.
  - While Shift-emptying storage, labels clear (previously the stale label persisted).
- **Validation:** native `Verify-Chickens` (feeding, targeting) and `Verify-Mobs`; `AlphaPlaytestChecks` (shared shape-aware targeting).

### F4 — IMGUI creature labels → uGUI HUD

- **Why:**
  - `PassiveSystem.OnGUI` (`Animals/PassiveSystem.cs:273-278`) allocates a `GUIStyle` on every call.
  - `MobHUD.OnGUI` (`Mobs/MobHUD.cs`) also uses IMGUI.
  - Any `OnGUI` method makes Unity run the IMGUI layout and repaint events every frame, even when it returns early. The rest of the UI is uGUI.
- **Approach:**
  - In `GameUI.BuildHUD`, add a `creaturePanel`, at the same 320×57 area centred under the crosshair. It holds a name `Text` and a two-`Image` health bar.
  - Add `RefreshCreatureTarget()` to `GameUI.Update`: read `game.Mobs.Target` (name, intent suffix, health) or `game.Animals.Target` (chicken status and HP), and update only on change (tuple key, like `RefreshTargetLabel`).
  - Delete both `OnGUI` methods and the `MobHUD` component.
- **Behaviour:** visual change (uGUI font and backing instead of the IMGUI skin). Needs a screenshot and refreshed wiki captures where these labels appear (Floater, chickens, mobs pages).
- **Validation:** native `Verify-Mobs`, `Verify-Floater` and `Verify-Chickens` captures; `ScreenReuseVerification`.

### F5 — Hoist per-instance native calls in distant machines (exact)

- **Why:** `FactoryDistancePresentation.LateUpdate` (`Presentation/FactoryDistancePresentation.cs:156-168`) reads `batch.Mesh.bounds` (a native call) and `game.Player.transform.position` for **every** instance every frame.

```diff
             foreach(var batch in batches.Values)
             {
-                int count=0;
+                int count=0;var meshBounds=batch.Mesh.bounds;var eye=game.Player.transform.position;float fogEnd=game.World.FogEnd;
                 foreach(var matrix in batch.Matrices)
                 {
-                    var bounds=batch.Mesh.bounds;var center=matrix.MultiplyPoint3x4(bounds.center);var e=bounds.extents;
+                    var center=matrix.MultiplyPoint3x4(meshBounds.center);var e=meshBounds.extents;
                     var x=matrix.MultiplyVector(new Vector3(e.x,0,0));var y=matrix.MultiplyVector(new Vector3(0,e.y,0));var z=matrix.MultiplyVector(new Vector3(0,0,e.z));
-                    bounds=new Bounds(center,2*new Vector3(...));
-                    if(!FactoryVisibility.Visible(game.Player.transform.position,bounds,game.World.FogEnd)||!GeometryUtility.TestPlanesAABB(planes,bounds))continue;
+                    var bounds=new Bounds(center,2*new Vector3(...));
+                    if(!FactoryVisibility.Visible(eye,bounds,fogEnd)||!GeometryUtility.TestPlanesAABB(planes,bounds))continue;
```

- **Validation:** `FactoryVisibilityChecks`; native `Verify-ReleaseReview` captures (distant machinery unchanged).

### F6 — Spread periodic refreshes across frames (sketch)

- **Where:**
  - `IndustryPresentation.RefreshVisible` and `FactoryDistancePresentation.Refresh` each walk all eligible machines every 0.25 s. The latter calls `DisconnectedPipeFaces` (6 × `HasPipeEnd`, several dictionary lookups each) per pipe.
  - `TorchPresentation.Refresh` sorts and groups with LINQ every 0.2 s.
  - All of these are frame-time spikes at 4–5 Hz.
- **Approach:**
  - Process 1/8 of the eligible list per frame, with a full-cycle swap so a cycle publishes a consistent instance set.
  - Cache `DisconnectedPipeFaces` per pipe by the industry `Revision`.
  - Replace the torch LINQ with reusable lists and `List.Sort`.
- **Behaviour:** view creation, removal and the near/far switch may lag by up to two frames (60 ms at 60 FPS) instead of one 0.25 s refresh step. Review the transition in motion ([FACTORY_VISIBILITY.md](FACTORY_VISIBILITY.md)).
- **Validation:** `FactoryVisibilityChecks`, `PresentationViewCacheChecks`; factory video review; A2 `RR.MachineViews` p99.

### F7 — Rain animated on the GPU (near-exact)

- **Why:** `WeatherPresentation.RenderRain` (`World/WeatherPresentation.cs:171-189`) recomputes 1,024 streaks on the CPU and re-uploads 4,096 vertices and colours every frame. Measured `RR.WeatherViews`: 0.39 ms mean during storms.
- **Approach:**
  - Build one static mesh once. Corner UVs go in TEXCOORD0; per-streak `(offset.x, offset.y, phase, density)` in TEXCOORD1; the column index in TEXCOORD2.
  - Per frame, set five uniforms and the renderer bounds. The 22×22 floor heights become a float array, updated only when columns refresh (4 Hz).
  - The shader repeats the CPU math exactly, and invisible streaks collapse to zero area. Today they rasterize with alpha 0, so this also saves fill.

```hlsl
// Resources/Materials/WeatherRain.shader — vertex stage (fragment unchanged)
float4 _RainOrigin,_RainEye,_RainRight,_RainMotion,_RainColour; // motion: time, speed, length, strength
float _RainFloors[484];                                         // unknown columns = 1e30
struct A {float4 vertex:POSITION;float2 uv:TEXCOORD0;float4 streak:TEXCOORD1;float2 column:TEXCOORD2;};
struct V {float4 position:SV_POSITION;half4 colour:COLOR;float2 uv:TEXCOORD0;};
V Vert(A i)
{
    V o;float time=_RainMotion.x,speed=_RainMotion.y,len=_RainMotion.z,strength=_RainMotion.w;
    float cycle=time*speed+i.streak.z*18;
    float y=_RainEye.y+8-(cycle-floor(cycle/18)*18);                   // Mathf.Repeat(cycle,18)
    bool visible=y-len>=_RainFloors[(int)i.column.x]&&i.streak.w<strength;
    float3 bottom=float3(_RainOrigin.x+i.streak.x,y,_RainOrigin.z+i.streak.y);
    float3 top=bottom+float3(-.12*strength,len,.025);
    float3 position=lerp(bottom,top,i.uv.y)+_RainRight.xyz*(i.uv.x*2-1);  // ±right as in the CPU quad
    half4 colour=_RainColour;colour.a*=smoothstep(1,3,distance(bottom,_RainEye.xyz));
    if(!visible){position=_RainEye.xyz;colour=0;}                         // zero-area quad
    o.position=TransformWorldToHClip(position);o.colour=colour;o.uv=i.uv;return o;
}
```

C# changes in `WeatherPresentation`:
- `Initialize` builds the static streams once (`SetUVs(1, List<Vector4>)`, `SetUVs(2, List<Vector2>)`).
- `RefreshColumns` pushes `material.SetFloatArray(FloorsId, floorValues)`.
- `RenderRain` becomes `SetVector`/`SetColor` on cached property IDs plus `rainRenderer.bounds=new Bounds(eye,Vector3.one*48)`. `MeshUploads` keeps counting presentation refreshes, so "paused rain reuses its mesh" still holds.
- `VisibleStreaks` and `CountScreenCoverage` (used by `WeatherVerification`) are computed on demand with a shared C# copy of the same streak math.

- **Behaviour:** the same streak positions, visibility and fade. Floating-point evaluation on the GPU can differ in the last bits.
- **Risks:** a shader array is not SRP-Batcher friendly (a single renderer, so irrelevant). Origin shifts are handled because `_RainOrigin` is pushed every frame.
- **Validation:** native `Verify-Weather` (streak counts, coverage bins, roof clipping, pause reuse) with updated expectations; side-by-side storm captures at fixed time; A2 `RR.WeatherViews`.

### F8 — Instanced dropped items (sketch)

- **Why:** each dropped pile is a GameObject hierarchy of up to three cube copies, destroyed and recreated when its copy count changes or it leaves 96 m (`Player/DroppedItems.cs:107-120`).
- **Approach:** draw piles with `Graphics.RenderMeshInstanced` per item mesh and material (the same pattern as `FactoryDistancePresentation`), with matrices from pile state. Keep shadows and receiving as today.
- **Validation:** native `Verify-Inventory`, lava burn and pickup scenarios; captures of piles in light and dark.

---

## 12. G — UI and input

### G1 — Cache the mine-button preference (exact)

- **Why:** `PlayerInput.Mine`, `Place`, `PlacePressed` and `UseButtonName` (`Player/PlayerInput.cs:57-60`) call `PlayerPrefs.GetInt` on every access, several times per frame. PlayerPrefs reads cross into native code and are registry-backed on Windows. The cost is unmeasured and likely small.

```diff
--- a/Assets/RivetReach/Code/Player/PlayerInput.cs
+        int mineButton,mineButtonFrame=-1;
+        // Read at most once per frame; settings call RefreshPreferences() so same-frame text is current.
+        int MineButton{get{int frame=Time.frameCount;if(frame!=mineButtonFrame){mineButtonFrame=frame;mineButton=PlayerPrefs.GetInt("mineButton",0);}return mineButton;}}
+        public void RefreshPreferences()=>mineButtonFrame=-1;
-        public bool Mine => Rebinding==null&&Mouse.current!=null&&(PlayerPrefs.GetInt("mineButton",0)==0?...
+        public bool Mine => Rebinding==null&&Mouse.current!=null&&(MineButton==0?...
         (same replacement in Place, PlacePressed, UseButtonName)
--- a/Assets/RivetReach/Code/UI/GameUI.cs  (Controls screen MINE button)
-                ...()=>{PlayerPrefs.SetInt("mineButton",1-PlayerPrefs.GetInt("mineButton",0));PlayerPrefs.Save();Rebuild();});
+                ...()=>{PlayerPrefs.SetInt("mineButton",1-PlayerPrefs.GetInt("mineButton",0));PlayerPrefs.Save();game.Input.RefreshPreferences();Rebuild();});
```

- **Risks:** verification code that writes `mineButton` and reads input in the **same** frame (`InventoryGestureVerification` writes, then yields). Call `RefreshPreferences()` there too.
- **Validation:** `ControlPresetChecks`; native `Verify-Inventory`, `Verify-Eating` and `MovementInteractionVerification`.

### G2 — Crate status text only on change (exact)

- **Why:** `GameUICrates.RefreshCrates` (`UI/GameUICrates.cs:35-46`) builds the status string every frame while a crate or controller is open.
- **Patch:** add a field `(Text,byte,long,int,int,bool,string) shownCrate;`. Build the key from (label, item, count, member count, page, locked, `endpoint.Status`) and return early when it equals the last shown key.
- **Validation:** `CrateChecks`; native `Verify-Crates` (status text captures).

### G3 — Cached cable-grid lookup in the machine UI (exact)

- **Why:** `RefreshMachine` (10 Hz, `UI/GameUIIndustry.cs:127`) scans every power group's ports with LINQ to find the open cable's grid.
- **Patch:** cache `(cable, Power.Topology.Revision) → group`. `NetworkTopology.Revision` increments on every publish (`NetworkTopology.cs:155`).
- **Validation:** `ConnectionChecks`; native `Verify-Connections` (cable UI text).

---

## 13. H — Startup, saves and build

### H1 — Memoize save content fingerprints (exact; golden test)

- **Why:**
  - `SaveStore`'s constructor (`Persistence/SaveIO.cs:81-142`) computes more than 100 content fingerprints. These include a 32-mask loop for each of two recipe-tier variants.
  - Each one JSON-serializes every item, recipe and mob, reloads `RecipeCatalogAsset`/`ProcessingCatalogAsset`/mob definitions and `TextAsset.text` (a new string per access), and SHA-256s the result.
  - The startup cost is unmeasured; record it first with a `Stopwatch` log.
- **Patch:**
  - Inside the constructor, load catalogs and texts once.
  - Memoize `ItemFingerprint(item, farming, materialTags, compost)`, `RecipeFingerprint(recipe, previousTier, previousComponent)` and `MobFingerprint(definition, floater, habitats, spawnLight, playtest)` in dictionaries keyed by the object plus flags. Each era then joins cached strings and hashes once.

```csharp
var itemCache=new Dictionary<(ItemDefinition,bool,bool,bool),string>();
string Item(ItemDefinition i,bool farming,bool materialTags,bool compost)
{var key=(i,farming,materialTags,compost);if(!itemCache.TryGetValue(key,out var json))itemCache[key]=json=ItemFingerprint(i,farming,materialTags,compost);return json;}
// .Select(i=>ItemFingerprint(i,farming,materialTags,compost)) -> .Select(i=>Item(i,farming,materialTags,compost))
// The same pattern applies to recipes (key includes previousTierRecipes/previousComponentRecipes, which are
// mutated between calls) and mobs. RecipeCatalogAsset.Load(), the mob LoadAll and each TextAsset.text are hoisted.
```

- **Risk:** any change in the hashed bytes breaks every existing save. Hence the mandatory golden test.
- **Validation:**
  - **Before the change**, add an Editor check that writes `content`, `currentSchemaContent`, `legacyContent` and `modernContent` (read through reflection, sorted) to `Assets/RivetReach/Editor/Fixtures/save-fingerprints-1811ac1.txt`.
  - After the change the same check must match exactly.
  - Then `SaveCompatibilityChecks` and the native legacy save sweep (`Verify-Saves`, `-rr-release-legacy`).

### H2 — Cache the save listing by file length and time (exact)

- **Why:** `SaveStore.List()` (`Persistence/SaveIO.cs:260-281`) reads and SHA-256-validates **every** save in full. The title screen calls it on every rebuild (`UI/GameUI.cs:92`), as do the Load menu (`UI/GameUISaves.cs:30`) and Continue.
- **Patch (draft):**

```csharp
readonly Dictionary<string,(long length,DateTime written,SaveEntry entry)> listing=new Dictionary<string,(long,DateTime,SaveEntry)>();
// In List(): for each path, info=new FileInfo(path); if listing has (Length, LastWriteTimeUtc) equal,
// add a clone of the cached entry; otherwise run the existing Open/validate path and cache the result.
// Drop listing keys whose files no longer exist. In Write(), remove target and target+".bak".
// SaveEntry gains: internal SaveEntry Clone()=>(SaveEntry)MemberwiseClone();
```

Failures are not cached, so damaged files are re-checked and still reported in `ScanWarning`.

- **Why safe:** `LoadGame` re-reads and fully validates before restoring, and checks the slot identity. A listing entry is only metadata.
- **Risks:** an external copy that preserves both the timestamp and the length would show stale metadata until the next real change. The load itself stays fully validated.
- **Validation:** new `SaveListingChecks`: write, rename, corrupt in place (different mtime), delete and backup cases with the expected listing each time. `SaveCompatibilityChecks`; native `Verify-Saves` (title Continue/Load flows).

### H3 — Background load preparation (sketch)

- **Why:** the synchronous load transaction took 2,052 ms.
- **Approach:** split `RestoreSave` into (1) read, checksum and parse into plain data on a worker thread, and (2) a main-thread apply that creates the session objects. Keep the "old session alive until everything validates" rollback. Show a progress screen.
- **Risks:** Unity API calls must stay on the main thread; parsing code currently mixes reading and object construction.
- **Validation:** `Verify-Saves` (rollback on failure, every legacy schema), plus timing of both phases.

### H4 — Verification code out of release players (sketch)

- **Why:**
  - About 950 KB of `*Verification`, `*Probe` and `*Showcase` code, including 62 `RuntimeVerification` partials, compiles into every player.
  - In shipped builds it is reachable through `-rr-*` command-line flags, and it slows every compile because there are no assembly definitions.
- **Steps:**
  1. Add `RivetReach.Runtime.asmdef` (Code/) and `RivetReach.Editor.asmdef` (Editor/).
  2. Move verification files to `Code/Verification/` with `RivetReach.Verification.asmdef` (`defineConstraints: ["RR_VERIFICATION"]`), plus `InternalsVisibleTo` for the hooks it needs.
  3. Replace `Expedition`'s direct `AddComponent<RuntimeVerification>()` and `AvatarVerification` bootstraps with a `[RuntimeInitializeOnLoadMethod]` in the verification assembly.
  4. Make `ProjectBuild.Build` add `RR_VERIFICATION` for verification builds only.
  5. Replace the 40+ hand-written flag checks with a scenario table (J4).
- **Risks:** large file moves while other sessions edit the same files. Coordinate ownership first and do it in one dedicated change.
- **Validation:** a release build with no verification types (inspect the assembly); full native suite on a verification build.

---

## 14. I — Rendering structure

### I1 — Merge static machine parts (exact; image check)

- **Why:** nearby machines instantiate prefabs with 3–4 child renderers each. All of them cast shadows into 4 cascades, which drives the roughly 7,000 SRP draw calls and 2,300 shadow casters.
- **Approach:**
  - `DistantMesh.Combine(prefab, Include, TransformPart)` already merges prefab parts for distant LODs. Use the same tool at import time (or first use) to build a **near-detail** mesh per prefab and connection mask.
  - Exclude `Motion*` parts, `Arm*` toggles, `StatusLight`, fills and labels; those stay separate. Body material variant swaps keep working on a single renderer.
- **Validation:** `FactoryVisibilityChecks`, `IndustryMaterialVariantChecks`, `WorldLitBatchingChecks`; captures of every machine type in all states (running, signal, arms per face); A2 draw-call and shadow-caster counts.

### I2 — GPU Resident Drawer (sketch)

- **Why:** Forward+ is already active (`PC_Renderer` `m_RenderingMode: 2`) and `m_GPUResidentDrawerMode` is 0. The Resident Drawer moves per-renderer submission to the GPU.
- **Requirements:**
  - DOTS instancing variants in `WorldLit`, `MachineLit`, `Terrain` and `ExplorerSkin` (`#pragma multi_compile _ DOTS_INSTANCING_ON` and instanced property declarations).
  - "BatchRendererGroup variants: keep all" in Graphics settings.
  - No `MaterialPropertyBlock` on participating renderers (F2a removes the chicken one; `StarterStationPresentation` uses one for embers).
  - Skinned meshes are excluded.
- **Risks:** high. A shader rewrite, possible visual differences, and Editor/player variant stripping.
- **Validation:** shader equivalence extended to the DOTS variants; complete capture review; A2.

### I3 — Group terrain chunk draws (sketch)

- **Why:** plain terrain already issues about 1,700 SRP draws (one renderer per 32³ page, times shadow cascades).
- **Approach:** one `BatchRendererGroup`, or `Graphics.RenderMeshIndirect`, for all terrain pages with per-page matrices. Collision and data pages are unchanged.
- **Validation:** shadow captures (cave openings), streaming and video review, A2.

### I4 — SSAO cost evaluation (decision)

- **Why:** `PC_Renderer` runs SSAO at full resolution with a depth-normals source. That needs a depth-normals prepass, which redraws the opaque geometry.
- **Options:** downsample, or a depth-only source with reconstructed normals. Both change AO slightly.
- **Validation:** `Verify-Shadows` captures ([SHADOW_RESULTS.md](verification/SHADOW_RESULTS.md) fixture); A2 GPU timing; user review.

---

## 15. J — Maintainability: dead code, duplication, readability, design

### J1 — Remove dead code (exact)

Each symbol below occurs only at its declaration across `Assets/` and `Tools/` (checked by repository-wide search, including string-based reflection):

| Symbol | Location |
| --- | --- |
| `ItemContainer.CommitPair` | `Core/ItemContainer.cs:55-59` |
| `GameUI.ResetSurvivalUI` | `UI/GameUISurvival.cs:16` (left from the destroy-and-rebuild UI) |
| `GameUI.ResetBrowserUI` | `UI/GameUIBrowser.cs:32-37` (same) |
| `GameUI.MachinePortSummary` | `UI/GameUIIndustry.cs:73-82` |
| `MachineState.CompostBatchCount` | `Industry/CompostSimulation.cs:12` |
| `IndustrySimulation.SuspendedComponents` | `Industry/IndustryScheduling.cs:33` |
| `VoxelWorld.PendingViewTeardowns` | `World/VoxelWorld.cs:37` |
| `GameUI.RetainedStationLayouts` | `UI/GameUIScreenReuse.cs:66` |
| `HeldBlockView.EquipLowering` (property only; the field is used) | `Player/HeldBlockView.cs:38` |
| `AvatarEquipment.ArmorShadowTriangles`, `EquippedVisual` | `Player/AvatarEquipment.cs:19,21` |

**Keep** `AvatarAppearance.SkinCount`. It is unused today, but it belongs to the open question about mapping more than two saved skin indices to presets ([DESIGN_QUESTIONS.md](DESIGN_QUESTIONS.md#explorer-appearance-generation-2)).

**Validation:** a compile plus the domain gate.

### J2 — Small deduplications (exact)

- **J2a — one writer for the weather flash.** `DayNightCycle.Apply` (`World/DayNightCycle.cs:41,44`) reads the flash through `Expedition.Instance...GetComponent<WeatherPresentation>()` every frame and writes `_RRWeatherFlash`. `WeatherPresentation.LateUpdate` writes the same global later in the same frame. Remove the two lines from `DayNightCycle`.
- **J2b — machine presentation rules in one place.** `IndustryPresentation.cs:227,230` and `FactoryDistancePresentation.cs:119,121` duplicate the "active body" rule and the topology-by-id selection.

```csharp
// IndustrySimulation
public NetworkTopology ConnectionTopology(byte id)=>id==IndustryId.PowerCable?Power.Topology:id==IndustryId.ItemPipe?ItemNetwork:id==IndustryId.FluidPipe?FluidNetwork:Signals.Topology;
// MachineState
internal bool ShowsActiveBody=>Definition.Id==IndustryId.ElectricFurnace||Definition.Id==IndustryId.RangedPump?Running:Signal||Source;
```

- **J2c — one creature/player overlap check in placement.** `Expedition.CanPlace` (`Expedition.cs:241-255`) repeats "inside player / inside creature" for beds and doors, with inconsistent null handling (`Mobs?.Occupies(x)??false` versus `Mobs!=null&&Mobs.Occupies(x)`).

```csharp
bool BodyBlocksCell(BlockPos cell,out string reason)
{
    reason=PlayerOverlapReason;if(World.OccupiesCell(Player.transform.position,.6f,Player.Height,cell))return true;
    reason="Cannot place inside a creature";return (Mobs?.Occupies(cell)??false)||(Animals?.Occupies(cell)??false);
}
```

  The generic path keeps the shape-aware `OccupiesBlock`.
- **J2d — shader property IDs.** 25 `Shader.SetGlobal*` calls and 52 material/MPB sets use string names; fog colour/range is set in both `VoxelWorld.Update` and `DayNightCycle.Apply`. Add a `ShaderIds` static class with `Shader.PropertyToID` fields, and keep one owner for each global.
- **Validation:** domain gate; shader equivalence; native weather and factory captures.

### J3 — Shared creature primitives (sketch)

- **Why:** `MobSystem` and `PassiveSystem` duplicate several pieces, although AGENTS.md asks passive animals to share movement and combat primitives:
  - ray targeting with clear-sight checks (`MobSystem.cs:334-345` / `PassiveSystem.cs:239-248`)
  - body-versus-player constraint (379-400 / 266-271)
  - `Occupies`
  - path following with the same 7.2 jump and pit refusal (298-316 / 210-221)
  - spawn-height search loops
  - the tool-class strike cooldown (356 / 261). One uses simulation time, the other `Time.time`; unify on simulation time.
- **Approach:** extract `CreatureBody` (overlap and constraint), `CreatureTargeting` (ray plus clear sight), `PathFollower` (step, jump, pit refusal) and `MeleeCooldown`. Keep separate lifecycles and populations.
- **Validation:** `AlphaPlaytestChecks`, `FloaterChecks`, `ChickenChecks`; native `Verify-Mobs`, `Verify-Floater`, `Verify-Chickens`.

### J4 — Verification scenario table and one runner (sketch)

- **Why:**
  - `RuntimeVerification.Run` (`RuntimeVerification.cs:89-136`) has 40+ repeated `Environment.GetCommandLineArgs().Contains("-rr-…")` checks for workload names and view distances.
  - `Tools/` has about 50 near-identical `Verify-*.ps1` scripts (1,077 lines) with two different build mechanisms and a hard-coded `D:\Unity\Hub\6000.4.4f1\Editor\Unity.exe`.
- **Approach:**
  - A `static readonly Scenario[]` with `{flag, workload, viewDistance, coroutine}` and one dispatch.
  - One `Tools/Verify-Scenario.ps1 -Scenario doors [-Build]` that reads the table and resolves Unity through the skill's documented discovery.
  - The existing scripts become one-line wrappers until callers move.
- **Validation:** run every scenario through the new runner and compare reports with the old scripts.

### J5 — Readability policy (incremental)

- **Why:** many statements per line; 116 gameplay lines over 250 characters, up to 1,118 (`SaveIO.cs`); nested `?:` chains for UI text (`GameUIHud.cs:45`, `GameUIIndustry.cs:89-120`, `ItemRegistry.cs:35,46`); magic block IDs (`CropRules.cs:28-33`, `runtimeId>=20` in icon building).
- **Approach:**
  - Add an `.editorconfig` (one statement per line for new and modified code, braces on multi-statement blocks, a 160-column guide).
  - Use `switch` expressions for status and label text, and named constants for IDs and UI dimensions.
  - Do **not** mass-reformat. Other sessions edit these files concurrently, and a repository-wide reformat would conflict with their work and obscure history. Reformat a file only in the same commit as a substantive change to it.
- **Validation:** compile; no behaviour change.

### J6 — Save fingerprints as named eras (sketch; after H1)

- **Why:** `Fingerprint(int legacy, bool orchard, … bool building)` takes 24 positional booleans, and it is called dozens of times with long `true,true,…` lists (`SaveIO.cs:107-142`). Miscounting an argument silently breaks compatibility.
- **Approach:** a `[Flags] enum ContentFeature`, plus a table of named eras (`Schema18 = …`, `PreToolWear = …`). The constructor iterates the table.
- **Validation:** the H1 golden fingerprint file must match exactly.

### J7 — Split the largest classes (sketch)

- **Why:**
  - `Expedition` spans 12 partial files (placement, buckets, saves, lava, pipes, tool wear). `VoxelWorld` spans 10 and knows beds, doors, torches and orchards. `GameUI` spans 16, with 40 pass-through properties in `GameUIScreenReuse.cs:67-108`. `IndustrySimulation` spans 9.
  - Partial files share all state, so they don't limit coupling.
  - `MachineState` carries every machine's fields and reuses some: the door latch is stored in `WorkInput` (`IndustrySimulation.cs:77-79`).
- **Approach (incremental, with the capability standard):**
  - Extract services with narrow interfaces: placement, bed/door registries out of `VoxelWorld`, `ScreenWidgets` accessed directly instead of through forwarding properties.
  - Per-capability machine state components.
  - Do one extraction per change, behind the existing checks.
- **Validation:** the domain gate and the relevant native suites for each extraction.

### J8 — Workspace hygiene

- `output/` (34 MB) is untracked and not ignored. Add `/output/` to `.gitignore`, or move its contents under `Logs/`.
- `Builds/` is 52 GB and `Logs/` 16 GB locally, with about 12,000 stray `.cs` copies under `Logs/`. Per-feature verification builds cause much of this. One verification build plus J4 removes the need. Delete superseded local build folders after recording any artifact identities the docs still cite.

---

## 16. Test matrix

### New checks proposed by this plan

All would be Editor checks added to the release domain gate unless noted.

| Check | Items | Asserts |
| --- | --- | --- |
| `TickCapChecks` | C1 | Capped sequences equal uncapped totals; ≤3 per frame; saved fraction round-trips |
| `TickPhaseChecks` | C2 | Groups never share a frame at 60 FPS, including after hitches and loads |
| `RangedPumpShortcutChecks` | D1 | Shortcut on/off produce identical pump state sequences |
| `FluidSourceCountChecks` | D1 | Page source counts equal a recount after every change |
| `SchedulerCacheChecks` | D2, D4, D6 | Cached `CanSimulate` equals the dictionary result; published structures equal a full rebuild |
| `PipeTransferEquivalenceChecks` | D5 | New transfers equal the frozen reference over random factories |
| `CoordinateChecks` | E1 | Shift equals `FloorDiv` over ranges and extremes |
| `WorldReadChecks` | E2 | New reads/collision/raycasts equal a frozen copy; `RR_VALIDATE_WORLD` run |
| `BlockTraitChecks` | E3 | Tables equal the pre-change golden dump |
| `FluidScheduleChecks` | E9 | Identical evaluation order and identical save bytes |
| `SaveFingerprintGolden` | H1, J6 | All fingerprint sets equal the pre-change dump |
| `SaveListingChecks` | H2 | Cache invalidation by length/time/write/delete; failures not cached |

### Existing suites to rerun per area

| Area | Editor (domain gate) | Native |
| --- | --- | --- |
| Configuration and rendering (B, E7, F7, I) | Shader equivalence, `WorldLitBatchingChecks`, `FactoryVisibilityChecks` | Captures and video review, `Verify-Shadows`, `Verify-Weather`, `Verify-ReleaseReview` |
| Scheduling (C) | `ResourceTickChecks`, `SurvivalChecks` | `Verify-Industry`, `Verify-Mobs`, `Verify-Chickens`, `Verify-Fluids`, `Verify-Saves` |
| Factory (D) | `IndustryChecks`, `GridAllocationChecks`, `FluidRoutingChecks`, `ItemPipeChecks`, `ResidencyInvalidationChecks`, `RangedPumpChecks`, `BatteryChecks`, `MultiblockChecks`, `BridgeChecks`, `CrateRoutingEdgeChecks` | `Verify-Industry`, `Verify-RangedPump`, `Verify-Multiblocks`, `Verify-Renewables`, `Verify-Bridges`, `Verify-Crates` |
| World (E) | `DomainChecks`, `TerrainGenerationChecks`, `LightingChecks`, `FluidChecks`, `LavaChecks`, `PerformanceChecks`, `ChunkWorkPriorityChecks` | `Verify-Terrain`, `Verify-Lighting`, `Verify-Fluids`, `Verify-Lava`, `Verify-Player`, `Verify-Performance` |
| Presentation and creatures (F) | `ChickenChecks`, `BedChecks`, `AlphaPlaytestChecks`, `PresentationViewCacheChecks` | `Verify-Chickens`, `Verify-Mobs`, `Verify-Floater`, `Verify-Beds`, `Verify-StarterStations`, `Verify-Weather` |
| UI and input (G) | `ControlPresetChecks`, `CrateChecks`, `ConnectionChecks` | `Verify-Inventory`, `Verify-Crates`, `Verify-Connections` |
| Saves and build (H) | `SaveCompatibilityChecks` | `Verify-Saves` with the legacy sweep; full suite on IL2CPP (B6) |

### Performance acceptance (deferred; A2)

Use the same commit and environment for each before/after pair. Report every frame over 16.67 ms and over 22.22 ms, plus p95, p99 and maximum per workload. Keep save/load stalls separate. Distinguish thermal from code effects with the 100 ms clock log. No item in this plan is "done" for performance until its targeted scope moves in such a pair.
