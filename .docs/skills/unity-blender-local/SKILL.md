---
name: unity-blender-local
description: Locate, start and check local Windows Unity and Blender applications from WSL for editor work, asset tasks or connectivity diagnostics. Use when these applications are needed, not for ordinary game-design brainstorming.
---

# Local Unity and Blender workflow

Start the required application when the user's task needs it; routine application launch does not require another confirmation. Reuse a suitable running instance. Installing integrations, creating a Unity project and changing engine versions are separate actions whose scope must come from the user's task.

## Access and readiness

Distinguish these states in reports:

1. Executable found and version identified.
2. Process started or already running.
3. Intended project/file loaded and ready.
4. Live integration responds to a read-only query identifying that project/file.

A successful launch or an open port does not prove editor control. Inspect available tools for Unity/Blender capabilities before claiming a live connection. If unavailable, use command-line automation when it can complete the task; explain the specific missing capability only when it blocks the task.

## Locate applications across Windows and WSL

Read the current repository's AGENTS.md and task scope. Validate cached paths before use. Windows applications may be installed on D: and absent from both Linux and Windows PATH. Empty Program Files directories do not establish that an application is missing.

Useful discovery order: known executable paths, running Windows process paths, Unity Hub configuration, Windows installed-application registry entries and application shortcuts. Avoid whole-drive recursive searches and broad configuration dumps. Query only relevant app records and avoid exposing credentials.

Use `powershell.exe -NoProfile -NonInteractive` for Windows process inspection and launch. Linux `ps` alone does not enumerate Windows editors. Convert WSL paths to Windows paths with `wslpath -w` before passing them to Windows programs. Quote arguments as code; for complex launches write a temporary PowerShell script and pass its Windows path with `-File` rather than nesting shell interpolation.

Machine-specific starting points, verified on 2026-09-08; rediscover if absent:

| Application | Windows executable |
|---|---|
| Unity Editor 6000.4.4f1 | `D:\Unity\Hub\6000.4.4f1\Editor\Unity.exe` |
| Unity Hub 3.20.0 | `D:\Unity\Unity Hub\Unity Hub.exe` |
| Blender 5.2.0 LTS | `D:\Program Files\Blender Foundation\Blender 5.2\blender.exe` |

Blender's `--version` command succeeded during discovery. Unity's installed executable/version was inspected; consult Rivet Reach's `.docs/UNITY_SETUP.md` for subsequent project initialization/import evidence. Neither live editor integration was exposed at that time; recheck availability rather than assuming that remains true.

## Unity

Find the actual project root from `ProjectSettings/ProjectVersion.txt`, `Assets/` and `Packages/`; do not assume the Git root is a Unity project. Read its pinned Editor version and use the matching installation. A documentation-only repository is not launchable as a Unity project. Do not create scaffolding merely to pass a connectivity check.

Rivet Reach was subsequently initialized at `D:\Dev\github-desktop\Rivet-Reach` using the installed 6000.4.4f1 Editor with URP 17.4.0, superseding its earlier 6.3 LTS target. Read its current ProjectVersion.txt before launch; do not silently upgrade it to another installed Editor. The initial scene is `Assets/RivetReach/Scenes/Main.unity`.

Inspect Windows Unity process command lines to identify an instance already using the target project. Do not open the same project in another Editor/batch process or remove lock files to bypass a running instance. Launch the matching executable with `-projectPath` and a properly quoted Windows project path. Use an explicit temporary `-logFile` when useful for diagnostics.

For authorized batch tasks, use the task's existing entry point and capture exit code plus logs; do not invent an `-executeMethod` that does not exist. Startup can involve import, compilation or license prompts. Check logs/process state and, when available, an integration query for the loaded project and readiness. A license/sign-in prompt may require user action; report the actual prompt rather than promising automatic recovery.

Reference for launch/batch options: [Unity Editor command-line arguments](https://docs.unity3d.com/6000.0/Documentation/Manual/EditorCommandLineArguments.html). Check documentation matching the installed version when adding version-specific options.

## Blender

Use `blender.exe --version` for a lightweight executable check. Prefer background execution for asset generation, export or inspection that does not require a visible editor. Consult the installed executable's `--help` for exact flags and ordering; pass script/file paths as Windows paths. A script run in a separate process does not control an already-open interactive session.

Start the GUI when the task requires interactive scene work or a configured integration needs it. Preserve existing unsaved scenes: do not replace or close them to attach to a different file. Identify the intended file through the integration or explicit process/file evidence before editing. Verify results through the resulting artifact and application output; process existence alone is insufficient.

## Startup and recovery

Launch only the application needed. Inspect an existing process before retrying. Poll readiness with bounded waits and progress updates; after a concrete failure, diagnose logs and make at most one justified automatic restart of a process started by this task. Do not terminate user-owned editors or discard unsaved work. Report unresolved startup or integration errors with the executable, project/file and relevant error.

A skill supplies workflow instructions, not a live bridge. If integration setup is requested, inspect persistent configuration and the chosen bridge's actual startup requirements. Configure only supported auto-start/reconnect behaviour, then verify a read-only query and repeat after a client/application restart before claiming persistence. Do not fabricate MCP entries, assume a localhost port, or install an arbitrary bridge simply because tools are absent.
