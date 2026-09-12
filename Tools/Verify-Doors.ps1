param([string]$OutputDirectory,[switch]$Build)
$ErrorActionPreference='Stop'
$project=Split-Path $PSScriptRoot -Parent
if (!$OutputDirectory) { $OutputDirectory=Join-Path $project 'Logs\DoorVerification' }
New-Item -ItemType Directory -Force $OutputDirectory | Out-Null
if ($Build) {
    $snapshot=Join-Path $project 'Builds\DoorVerificationProject'
    $running=Get-CimInstance Win32_Process -Filter "Name='Unity.exe'" | Where-Object { $_.CommandLine -and $_.CommandLine.Replace('/','\').Contains($snapshot) }
    if ($running) { throw 'Door verification project is already open; preserve that process.' }
    foreach ($folder in @('Assets','Packages','ProjectSettings','.docs\licenses')) {
        & robocopy (Join-Path $project $folder) (Join-Path $snapshot $folder) /MIR /NFL /NDL /NJH /NJS /NP | Out-Null
        if ($LASTEXITCODE -ge 8) { throw "Snapshot failed: $folder" }
    }
    Copy-Item (Join-Path $project 'LICENSE.md') $snapshot -Force
    Copy-Item (Join-Path $project '.docs\THIRD_PARTY_NOTICES.md') (Join-Path $snapshot '.docs') -Force
    $logs=Join-Path $snapshot 'Logs';New-Item -ItemType Directory -Force $logs | Out-Null
    $process=Start-Process -FilePath 'D:\Unity\Hub\6000.4.4f1\Editor\Unity.exe' -ArgumentList @('-batchmode','-quit','-projectPath',('"'+$snapshot+'"'),'-executeMethod','RivetReach.Editor.DoorBuild.Run','-job-worker-count','2','-logFile',('"'+$logs+'\build.log"')) -PassThru
    if (!$process.WaitForExit(1200000)) { throw 'Door build timed out; process and logs preserved.' }
    if ($process.ExitCode -ne 0) { throw "Unity exited $($process.ExitCode); inspect $logs\build.log" }
    & robocopy (Join-Path $snapshot 'Builds\Doors') (Join-Path $project 'Builds\Doors') /E /NFL /NDL /NJH /NJS /NP | Out-Null
    if ($LASTEXITCODE -ge 8) { throw 'Review build copy failed.' }
}
$player=Join-Path $project 'Builds\Doors\RivetReach.exe'
$process=Start-Process -FilePath $player -ArgumentList @('-screen-fullscreen','0','-screen-width','1280','-screen-height','800','-rr-verify','-rr-door-review','-rr-output',('"'+$OutputDirectory+'"'),'-logFile',('"'+$OutputDirectory+'\player.log"')) -PassThru
if (!$process.WaitForExit(300000)) { throw 'Door review timed out; process preserved for diagnosis.' }
$report=Get-Content -Raw (Join-Path $OutputDirectory 'runtime-report.json') | ConvertFrom-Json
$report | Select-Object result,assertions,workload
if ($process.ExitCode -ne 0 -or $report.result -ne 'PASS') { throw ($report.errors -join "`n") }
