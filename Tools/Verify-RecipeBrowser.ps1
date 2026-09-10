param([string]$OutputDirectory, [switch]$Build)
$ErrorActionPreference = 'Stop'
$project = Split-Path $PSScriptRoot -Parent
if (!$OutputDirectory) { $OutputDirectory = Join-Path $project 'Logs\RecipeBrowser\Runtime' }
New-Item -ItemType Directory -Force $OutputDirectory | Out-Null
if ($Build) {
    # Isolate imports and building from the user's open Editor and concurrent project tasks.
    $snapshot = Join-Path $project 'Builds\RecipeBrowserVerificationProject'
    $running = Get-CimInstance Win32_Process -Filter "Name='Unity.exe'" | Where-Object { $_.CommandLine -and $_.CommandLine.Replace('/','\').Contains($snapshot) }
    if ($running) { throw 'The browser verification project is already open; preserve that process and retry after its build completes.' }
    foreach ($folder in @('Assets','Packages','ProjectSettings','.docs\licenses')) {
        & robocopy (Join-Path $project $folder) (Join-Path $snapshot $folder) /MIR /NFL /NDL /NJH /NJS /NP | Out-Null
        if ($LASTEXITCODE -ge 8) { throw "Snapshot copy failed: $folder" }
    }
    Copy-Item (Join-Path $project 'LICENSE.md') $snapshot -Force
    Copy-Item (Join-Path $project '.docs\THIRD_PARTY_NOTICES.md') (Join-Path $snapshot '.docs') -Force
    $logs = Join-Path $snapshot 'Logs\RecipeBrowser'; New-Item -ItemType Directory -Force $logs | Out-Null
    $editor = 'D:\Unity\Hub\6000.4.4f1\Editor\Unity.exe'
    $process = Start-Process -FilePath $editor -ArgumentList @('-batchmode','-quit','-projectPath',('"'+$snapshot+'"'),'-executeMethod','RivetReach.Editor.RecipeBrowserBuild.Run','-job-worker-count','4','-logFile',('"'+$logs+'\build.log"')) -PassThru
    if (!$process.WaitForExit(900000)) { throw 'Browser build timed out; process and logs preserved for diagnosis.' }
    if ($process.ExitCode -ne 0) { throw "Unity exited $($process.ExitCode); inspect $logs\build.log" }
    & robocopy (Join-Path $snapshot 'Builds\RecipeBrowser') (Join-Path $project 'Builds\RecipeBrowser') /E /NFL /NDL /NJH /NJS /NP | Out-Null
    if ($LASTEXITCODE -ge 8) { throw 'Copying the review player failed.' }
}
$player = Join-Path $project 'Builds\RecipeBrowser\RivetReach.exe'
if (!(Test-Path $player)) { throw 'No browser review player. Run this script with -Build first.' }
$reportPath = Join-Path $OutputDirectory 'runtime-report.json'
if (Test-Path $reportPath) { Remove-Item $reportPath }
$process = Start-Process -FilePath $player -ArgumentList @('-screen-fullscreen','0','-screen-width','1280','-screen-height','720','-rr-verify','-rr-browser-review','-rr-output',('"'+$OutputDirectory+'"'),'-logFile',('"'+$OutputDirectory+'\player.log"')) -PassThru
if (!$process.WaitForExit(600000)) { throw 'Browser runtime review timed out; player and logs preserved.' }
$reportPath = Join-Path $OutputDirectory 'runtime-report.json'
if (!(Test-Path $reportPath)) { throw 'No runtime report; inspect player.log.' }
$report = Get-Content -Raw $reportPath | ConvertFrom-Json
Get-Content $reportPath
if ($process.ExitCode -ne 0 -or $report.result -ne 'PASS') { throw 'Recipe browser verification failed.' }
