param([string]$OutputDirectory,[switch]$Build,[string]$LegacySaveDirectory,[switch]$FullReview)
$ErrorActionPreference='Stop'
$project=Split-Path $PSScriptRoot -Parent
if (!$OutputDirectory) { $OutputDirectory=Join-Path $project 'Logs\FarmingVerification' }
New-Item -ItemType Directory -Force $OutputDirectory | Out-Null
if ($Build) {
    $result=Join-Path $project 'Logs\build-result.txt'
    if (Test-Path (Join-Path $project 'Logs\build-request.txt')) { throw 'Another Editor build is pending.' }
    if (Test-Path $result) { Remove-Item $result }
    $command=if ($FullReview) { 'tag-review' } else { 'farming-player' }
    Set-Content (Join-Path $project 'Logs\build-request.txt') $command -NoNewline
    $deadline=(Get-Date).AddMinutes(15)
    while (!(Test-Path $result) -and (Get-Date) -lt $deadline) { Start-Sleep -Seconds 2 }
    if (!(Test-Path $result)) { throw 'Editor build timed out; pending request and unsaved work preserved.' }
    if (!(Get-Content $result -Raw).StartsWith('SUCCESS')) { throw (Get-Content $result -Raw) }
}
$player=Join-Path $project 'Builds\Farming\RivetReach.exe'
$arguments=@('-screen-fullscreen','0','-screen-width','1280','-screen-height','800','-rr-verify','-rr-farming-review','-rr-output',('"'+$OutputDirectory+'"'),'-logFile',('"'+$OutputDirectory+'\player.log"'))
if ($LegacySaveDirectory) { $arguments=$arguments | Where-Object { $_ -ne '-rr-farming-review' }; $arguments+=@('-rr-farming-legacy','-rr-save-directory',('"'+$LegacySaveDirectory+'"')) }
$process=Start-Process -FilePath $player -ArgumentList $arguments -PassThru
if (!$process.WaitForExit(300000)) { throw 'Farming review timed out; process preserved for diagnosis.' }
Set-Content (Join-Path $OutputDirectory 'exit-code.txt') $process.ExitCode
$report=Get-Content -Raw (Join-Path $OutputDirectory 'runtime-report.json') | ConvertFrom-Json
$report | Select-Object result,assertions,workload
if ($process.ExitCode -ne 0 -or $report.result -ne 'PASS') { throw ($report.errors -join "`n") }

if ($FullReview) {
    & (Join-Path $PSScriptRoot 'Verify-Saves.ps1') -Executable $player -OutputDirectory (Join-Path $OutputDirectory 'SaveRegression')
    & (Join-Path $PSScriptRoot 'Verify-Mobs.ps1') -Player $player -OutputDirectory (Join-Path $OutputDirectory 'Mobs')
}
