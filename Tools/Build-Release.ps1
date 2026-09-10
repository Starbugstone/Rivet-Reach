param(
    [string]$UnityEditor = 'D:\Unity\Hub\6000.4.4f1\Editor\Unity.exe'
)
$ErrorActionPreference = 'Stop'
$project = Split-Path $PSScriptRoot -Parent
$running = Get-CimInstance Win32_Process -Filter "Name='Unity.exe'" | Where-Object { $_.CommandLine -and $_.CommandLine.Replace('/','\').Contains($project) }
if ($running) { throw 'Build the release in a separate clean checkout, or close this checkout in Unity first. Unsaved work is preserved.' }
if (!(Test-Path $UnityEditor)) { throw 'Supply the pinned 6000.4.4f1 Editor with -UnityEditor.' }
$logs = Join-Path $project 'Logs'
New-Item -ItemType Directory -Force $logs | Out-Null
$output = Join-Path $project 'Builds\Release\0.0.1\RivetReach-0.0.1-alpha-windows-x64'
if (Test-Path $output) { throw 'Release output already exists. Use a fresh checkout/output to prevent stale files entering the release.' }
$process = Start-Process -FilePath $UnityEditor -ArgumentList @('-batchmode','-quit','-projectPath',('"'+$project+'"'),'-executeMethod','RivetReach.Editor.ProjectBuild.BuildAlpha','-logFile',('"'+(Join-Path $logs 'release-build.log')+'"')) -PassThru -Wait
if ($process.ExitCode -ne 0) { throw "Unity release build exited $($process.ExitCode). See Logs/release-build.log." }
if (!(Test-Path (Join-Path $output 'RivetReach.exe'))) { throw 'Unity did not produce the release executable.' }
Copy-Item (Join-Path $project '.docs\releases\0.0.1.md') (Join-Path $output 'README.md')
Get-Content (Join-Path $logs 'build-summary.txt')
Write-Output "Player: $output\RivetReach.exe"
Write-Output 'Run verification against this executable before packaging and publishing it.'
