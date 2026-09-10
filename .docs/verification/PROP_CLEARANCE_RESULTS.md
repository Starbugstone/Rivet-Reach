# Pass-through props and Editor icons

The 2026-09-10 fix separates camera clearance from interaction targeting. The existing movement collision already treated torches and potato plants as passable, but the upward camera ray selected these objects and clamped the eye down as though beneath a ceiling. Both first-person clearance and the inspection camera now trace movement-solid cells; interaction rays still select props for harvesting/removal. Unloaded cells remain a camera boundary, matching movement.

Unity Light and AudioSource icons are hidden on Editor initialization and Play entry. Lighting and audio components remain active. The markers are Editor annotations, not gameplay indicators. The existing paused Game view was visually inspected with the light icons present, then with them absent. Its script reload also invalidated runtime references, so that session requires a fresh Play start and is not runtime verification evidence.

## Verification

A separate project copy at `D:\Dev\UnityChecks\RivetReach-Clearance` is used to avoid stopping the user's main Editor Play session. The [Windows build](prop-clearance-2026-09-10-build.txt) succeeded with **0 errors / 0 warnings**, at **2026-09-10 15:53 UTC**. The [artifact manifest](prop-clearance-2026-09-10-manifest.json) identifies the executable and gameplay assembly; the local copy is `Builds/PropClearance/RivetReach.exe`. This is a focused snapshot taken before the concurrent crafting/performance revisions, not a verification of those later changes.

The [runtime suite](prop-clearance-2026-09-10-runtime.json) passed **168 assertions**, completed at **2026-09-10T15:55:30.5684745Z**, at 1280×720 with view radius 4. Every floor-prop traversal retained a minimum standing eye height of **1.640 m**, and feet remained within 2 cm of the floor. Crouching/standing within each prop, the head-height wall torch, solid-wall occlusion and the crouch-ceiling check passed. Underfoot placement passed at 20/60/120 fps targets, with a measured 1.599 m jump apex in each run, followed by the sprint regressions. No runtime errors were recorded. The build and checks use pinned Unity 6000.4.4f1.

The [Editor check](prop-clearance-2026-09-10-editor-icons.txt) passed **four icon-state assertions across two fresh Play sessions**, deliberately enabling both icons before each entry. The final Editor helper waits until annotations are registered, then hides Light and AudioSource icons; AudioListener has no registered icon and is not queried. A headless/early-start probe could not inspect the uninitialized icon registry, so the successful check used a normal Editor with its Scene view initialized. The regular Editor startup suite also includes the two icon assertions. The runtime build is unchanged by this final Editor-only adjustment.

Reproduce against the delivered executable with:

```powershell
.\Tools\Verify-POC.ps1 -Clearance -Executable Builds/PropClearance/RivetReach.exe -OutputDirectory Logs/PropClearance
```

The focused suite exercises the actual player update while walking through floor torches, four potato growth stages and signal wire; it measures feet and eye height, crouching and standing inside each prop, and interaction targeting through the unchanged selection ray. It additionally checks a wall torch at head height, solid-wall camera occlusion and a low solid ceiling. Existing underfoot placement at 20/60/120 fps targets and double-tap sprint checks run afterward. These focused checks do not certify the wider game or performance.
