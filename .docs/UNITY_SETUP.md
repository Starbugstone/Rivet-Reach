# Rivet Reach - Unity Project Setup

## Project and Editor

The repository root is the Unity project directory. Open `D:\Dev\github-desktop\Rivet-Reach` on this workstation, or the root of a fresh checkout on another machine. Do not create a second nested project or open `.docs` as the project.

- Editor: **6000.4.4f1**, pinned in [ProjectVersion.txt](../ProjectSettings/ProjectVersion.txt).
- Render pipeline: **URP 17.4.0**.
- Initial scene: [Main.unity](../Assets/RivetReach/Scenes/Main.unity).
- Project assets: `Assets/RivetReach/`.
- Package declarations and resolved versions: [manifest.json](../Packages/manifest.json) and [packages-lock.json](../Packages/packages-lock.json).

The initialization request uses the existing installed Editor and supersedes the earlier Unity 6.3 LTS planning target. No Unity Editor update was performed or needed for this initialization. Future upgrades are separate tasks and must update the pinned version with compatibility checks.

## Starting work

In Unity Hub, add/open the repository root and select 6000.4.4f1. Open `Assets/RivetReach/Scenes/Main.unity` if the Editor restores another scene. The scene retains the template camera, directional light and global volume on disk. Press Play to run `Expedition.Bootstrap`, which creates the playable terrain/player/interface and replaces the starter camera. See [FIRST_POC.md](FIRST_POC.md) for controls, Windows build commands and the session-only state boundary.

The installed Windows Editor is `D:\Unity\Hub\6000.4.4f1\Editor\Unity.exe`. Launch from PowerShell with:

```powershell
& 'D:\Unity\Hub\6000.4.4f1\Editor\Unity.exe' -projectPath 'D:\Dev\github-desktop\Rivet-Reach'
```

Use the project's local Unity/Blender skill to identify/reuse running instances. Do not open the same project simultaneously in a GUI Editor and batch Editor.

## Historical initialization scope

The starter assets/settings come from the Editor's bundled 3D URP template. Tutorial/readme scripts and sample branding were omitted; the scene was renamed to Main and assets placed under RivetReach while preserving asset GUIDs. No gameplay scripts, world generation, movement controller, machinery or live-editor integration were added.

Direct packages are URP, Input System, Unity UI, Test Framework and Visual Studio integration. The lockfile records transitive packages. Framework availability does not mean gameplay tests have been implemented or run.

Unity uses visible `.meta` files and text asset serialization. Track `Assets`, `Packages` and `ProjectSettings`; `.gitignore` excludes `Library`, `Temp`, `Logs`, `UserSettings`, builds and generated IDE files. Keep every tracked asset's `.meta` file. Initialization introduced no binary runtime art. The later [concept-art gallery](concept-art/README.md) adds PNG references outside `Assets`, tracked through Git LFS; run `git lfs pull` after cloning to retrieve those images. Actual runtime art/source formats remain governed by CONTENT_PIPELINE.md.

License provenance and retained upstream notices are recorded in [Third-party notices](THIRD_PARTY_NOTICES.md).

## Verification

On 2026-09-08, the initial Editor import exited successfully (code 0). A subsequent Editor verification also exited with code 0, opened Main.unity, confirmed an active UniversalRenderPipelineAsset, and found Main Camera, Directional Light and Global Volume. The verification run contained no C# compiler errors or exceptions. The first import used Unity's automatic API updater on bundled package code; generated package caches are not committed.

At initialization, the temporary verification script was removed and the Assets tree contained no gameplay C# scripts. This historical statement is superseded by the first-POC implementation. The graphical Editor was also opened successfully on Main.unity. Temporary scene-opening helpers were removed after verification. This verifies initialization, not a player build or gameplay. Machine-local diagnostic logs are retained in the ignored `Logs/` directory; they are not committed because they contain local paths and account/environment details. This setup does not establish gameplay correctness, performance targets, release readiness or a live MCP connection.

## First playable implementation

The user subsequently authorized the first POC. Runtime code, reusable Blender/FBX player assets, original skins/terrain materials and build/verification entry points are now versioned. Unity remains pinned to the same Editor and URP versions. Built-in audio, physics and screenshot modules were enabled; no engine upgrade or live MCP installation was needed. The Editor reserialized template settings in its current format during the first player build.

Use **Rivet Reach → Build Windows first POC** or [Build-Windows.ps1](../Tools/Build-Windows.ps1). The local build-request poller waits for asset refresh/compilation before building in an open Editor; batch mode uses the same validated entry point when the project is closed. Build logs and package-generated performance metadata are derived local state. [Current revision results](verification/VISUAL_REVISION_RESULTS.md) now provide standalone gameplay evidence beyond the original initialization checks.
