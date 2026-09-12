param([switch]$Build, [switch]$ReuseSnapshot, [int]$BuildTimeoutSeconds = 1800)
$ErrorActionPreference = 'Stop'
$project = Split-Path $PSScriptRoot -Parent
$snapshot = Join-Path $project 'Builds\PotatoArtVerificationProject'
$output = Join-Path $project 'Logs\PotatoArt'
New-Item -ItemType Directory -Force $output | Out-Null
if ($Build) {
    $running = Get-CimInstance Win32_Process -Filter "Name='Unity.exe'" | Where-Object { $_.CommandLine -and $_.CommandLine.Replace('/','\').Contains($snapshot) }
    if ($running) { throw 'The potato verification project is already running; preserve it.' }
    if (!$ReuseSnapshot) {
    foreach ($folder in @('Assets','Packages','ProjectSettings')) {
        & robocopy (Join-Path $project $folder) (Join-Path $snapshot $folder) /MIR /NFL /NDL /NJH /NJS /NP | Out-Null
        if ($LASTEXITCODE -ge 8) { throw "Snapshot copy failed: $folder" }
    }
    }
    $logs = Join-Path $snapshot 'Logs'; New-Item -ItemType Directory -Force $logs | Out-Null
    $process = Start-Process -FilePath 'D:\Unity\Hub\6000.4.4f1\Editor\Unity.exe' -ArgumentList @('-batchmode','-quit','-projectPath',('"'+$snapshot+'"'),'-executeMethod','RivetReach.Editor.FoodArtBuild.Run','-job-worker-count','4','-logFile',('"'+$logs+'\build.log"')) -PassThru
    if (!$process.WaitForExit($BuildTimeoutSeconds * 1000)) { throw 'Potato build timed out; process and logs preserved.' }
    if ($process.ExitCode -ne 0) { throw "Unity exited $($process.ExitCode); inspect $logs\build.log" }
    & robocopy (Join-Path $snapshot 'Builds\PotatoArt') (Join-Path $project 'Builds\PotatoArt') /E /NFL /NDL /NJH /NJS /NP | Out-Null
    if ($LASTEXITCODE -ge 8) { throw 'Copying the review player failed.' }
    Copy-Item (Join-Path $snapshot 'Logs\PotatoArt\*.txt') $output -Force
    # Return only this task's authoritative import settings and material.
    Copy-Item (Join-Path $snapshot 'Assets\RivetReach\Resources\Food\*.meta') (Join-Path $project 'Assets\RivetReach\Resources\Food') -Force
    Copy-Item (Join-Path $snapshot 'Assets\RivetReach\Resources\Food\Potato.mat') (Join-Path $project 'Assets\RivetReach\Resources\Food') -Force
    Copy-Item (Join-Path $project 'LICENSE.md') (Join-Path $project 'Builds\PotatoArt') -Force
    Copy-Item (Join-Path $project '.docs\THIRD_PARTY_NOTICES.md') (Join-Path $project 'Builds\PotatoArt') -Force
    Copy-Item (Join-Path $project '.docs\licenses') (Join-Path $project 'Builds\PotatoArt') -Recurse -Force
}
$runtime = Join-Path $output 'Runtime'; New-Item -ItemType Directory -Force $runtime | Out-Null
$reportPath = Join-Path $runtime 'runtime-report.json'
if (Test-Path $reportPath) { Remove-Item $reportPath }
$player = Join-Path $project 'Builds\PotatoArt\RivetReach.exe'
$process = Start-Process -FilePath $player -ArgumentList @('-force-d3d11','-screen-fullscreen','0','-screen-width','1280','-screen-height','720','-rr-verify','-rr-potato-art-review','-rr-output',('"'+$runtime+'"'),'-logFile',('"'+$runtime+'\player.log"')) -PassThru
if (!$process.WaitForExit(240000)) { throw 'Potato runtime review timed out; player preserved.' }
$report = Get-Content -Raw $reportPath | ConvertFrom-Json
Get-Content $reportPath
if ($process.ExitCode -ne 0 -or $report.result -ne 'PASS') { throw 'Potato art verification failed.' }
