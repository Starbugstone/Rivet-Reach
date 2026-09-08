# Building, movement and dropped items — 2026-09-08

This revision implements the user's placement, dropped-stack, sprint, seed and debug-panel feedback. The final requested jump height is **1.6 blocks**. It does not introduce half blocks, tools, structures or persistent saves. Behaviour is implemented in Rivet Reach's own code; no third-party game source or assets were copied.

## Changes and measured checks

| Area | Result |
|---|---|
| Placement through loose items | A real aimed placement with full inventory commits one block and consumes exactly one selected item. Sleeping piles, multiple item types and a pile straddling the target edge leave the new solid in the same turn. Low-ceiling items take a clear side across the X=31/32 chunk seam. A small enclosure also preserves the drops. Quantities, identity, pickup delay and lifetime survive repeated displacement and subsequent settling. |
| Same-item stacking | 100 one-item dirt drops and 100 one-item grass drops in one cell become two 100-item piles. **200 records/views become 2**, removing 198 redundant GameObjects. Different identities remain separate on the same block. Overflow produces 500 + 50 dirt while grass remains unchanged; a delayed extra dirt item merges only when eligible. The oldest elapsed lifetime survives, and a solid corner prevents merging despite sub-metre proximity. |
| Underfoot building | Placement uses the movement collider's occupied-cell calculation and collision skin. A block touching the feet is legal; actual leg overlap remains blocked. Failed attempts no longer spend the successful-placement cooldown. Real mapped jump/held-place input builds one block underneath and lands on it without penetration at 20, 60 and 120 FPS targets. |
| Jump height | Each tested frame-rate target measured a **1.599 m** apex above the block face, consistent with the requested 1.6 m jump and the 1 mm collision skin. Each run placed exactly one block and could not add a second block during the same jump. |
| Double-tap sprint | A rapid second press of the mapped Forward action starts sprinting. Measured sustained speed was **6.50 m/s**. Single/slow presses walk. Release, crouch and inventory cancel the gesture; the dedicated sprint key and a rebound Forward action still work. |
| World seed | Normal startup prepares a random seed and leaves the optional title field blank. Separate launches recorded seeds **73377394** and **1199919539**. Blank Start uses the prepared world; entering **246813** explicitly starts that reproducible terrain. The first seed is retained in the earlier focused report, before the F12 addition. |
| F12 diagnostics | The debug action opens a panel containing the most recent placement rejection; toggling it off hides the panel. Body-overlap rejection remains absent from the normal HUD at every jump test rate. The play guide records the panel contents and controls. |
| Compiler alerts | Both deprecated `FindFirstObjectByType` calls in the grip preview use `FindAnyObjectByType`. The avatar verification camera field is renamed to avoid shadowing Unity's inherited member. Final import/compilation logs contain no C# warning or error entries. |

The authoritative rules remain in [gameplay](../GAMEPLAY.md#first-step-terrain-placement--user-feedback-extension) and [simulation](../SIMULATION.md#first-poc-placement-and-view-distance-revision). [The F12 play guide](../FIRST_POC.md#f12-debug-panel) documents player-facing diagnostics.

## Verification

- **263 full runtime assertions passed**, with no logged Unity errors: [full report](building-runtime-report.json). It includes the earlier terrain, inventory, grass, character visibility and held-item regressions as well as these new interaction fixtures.
- **22,380 domain assertions passed**: [domain record](building-domain-checks.txt).
- Windows development build: **0 errors and 0 warnings**, with a separate check for C# compilation diagnostics: [build](building-build.txt), [compiler record](building-compiler-checks.txt).
- The preceding focused interaction run passed **90 assertions** before the final F12 checks were added: [focused report](building-focused-report.json). The complete final implementation is covered by the full report above.

The matching Unity 6000.4.4f1 Editor built an isolated project copy because the user's real Editor held an active expedition. Its Play session was preserved. The verified standalone output was copied to `Builds/PlayerRevision4/`. No normal Editor Play/Stop cycle was forced during this revision. The changes load in the real Editor after it recompiles and Play restarts.

Run `Tools/Build-Windows.ps1` then `Tools/Verify-POC.ps1` for the complete regression. The focused runtime flags are `-rr-verify -rr-placement-items-review -rr-output <directory>`. Verification uses virtual Input System keyboard/mouse devices so desktop focus and real keystrokes cannot alter its queued gestures; ordinary play uses the normal devices. Fixtures exist only in explicit verification mode and are restored after the tests.

## Visual evidence and limits

Actual Unity captures: [before placement](building-placement-items-before.png), [popped above a block](building-placement-items-pop-up.png), [clear side under a ceiling](building-placement-items-sideways.png), [two item types sharing a block](building-dropped-items-mixed-stacks.png), [standing on the newly placed block](building-underfoot-jump-placement.png), [F12 rejection panel](building-f12-placement-debug.png), [random seed title](building-random-seed-title.png).

The stack fixture measures object/record reduction, not an exact RAM saving or a sustained thousands-of-items benchmark. Merge scratch buckets reuse their capacity and clear pile references after each pass. Completely sealed/unready escape regions retain the pile and retry; ordinary eligible lifetime still applies. Wider movement feel remains subject to playtesting, and half-block geometry is future work. The full report retains its finite frame sample and hardware information; it is not an uncapped performance guarantee.
