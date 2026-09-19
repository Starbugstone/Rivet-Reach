param([string]$OutputDirectory,[switch]$Build,[string]$SaveDirectory)
$ErrorActionPreference='Stop'
$project=Split-Path $PSScriptRoot -Parent
if (!$OutputDirectory) { $OutputDirectory=Join-Path $project ('Logs\Renewables-'+[DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss')) }
New-Item -ItemType Directory -Force $OutputDirectory | Out-Null
if ($Build) {
    $request=Join-Path $project 'Logs\build-request.txt';$result=Join-Path $project 'Logs\build-result.txt'
    if (Test-Path $request) { throw 'Another Editor build is pending.' }
    if (Test-Path $result) { Remove-Item $result }
    Set-Content $request 'renewables-player' -NoNewline
    $deadline=(Get-Date).AddMinutes(15)
    while (!(Test-Path $result) -and (Get-Date) -lt $deadline) { Start-Sleep -Seconds 2 }
    if (!(Test-Path $result)) { throw 'Editor build timed out; pending request preserved.' }
    if (!(Get-Content $result -Raw).StartsWith('SUCCESS')) { throw (Get-Content $result -Raw) }
}
$player=Join-Path $project 'Builds\Renewables\RivetReach.exe'
$arguments=@('-screen-fullscreen','0','-screen-width','1280','-screen-height','800','-rr-verify','-rr-renewables-review','-rr-output',('"'+$OutputDirectory+'"'),'-logFile',('"'+$OutputDirectory+'\player.log"'))
if ($SaveDirectory) {
    $arguments=$arguments | Where-Object { $_ -ne '-rr-renewables-review' }
    $arguments+=@('-rr-renewables-resume-review','-rr-save-directory',('"'+$SaveDirectory+'"'))
}
$process=Start-Process -FilePath $player -ArgumentList $arguments -PassThru
if (!$process.WaitForExit(300000)) { throw 'Renewables review timed out; player preserved for diagnosis.' }
Set-Content (Join-Path $OutputDirectory 'exit-code.txt') $process.ExitCode
$report=Get-Content -Raw (Join-Path $OutputDirectory 'runtime-report.json') | ConvertFrom-Json
$report | Select-Object result,assertions,workload
if ($process.ExitCode -ne 0 -or $report.result -ne 'PASS') { throw ($report.errors -join "`n") }
