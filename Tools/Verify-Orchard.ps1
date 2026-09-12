param([string]$Executable,[string]$OutputDirectory)
$ErrorActionPreference='Stop'
$project=Split-Path $PSScriptRoot -Parent
if (!$Executable) { $Executable=Join-Path $project 'Builds\Orchard\RivetReach.exe' }
if (!$OutputDirectory) { $OutputDirectory=Join-Path $project ('Logs\OrchardVerification-'+[DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss')) }
$legacy=Join-Path $project '.docs\verification\orchard-2026-09-12\legacy-fixtures'
foreach($scenario in @('orchard','orchard-legacy')) {
    $result=Join-Path $OutputDirectory $scenario
    New-Item -ItemType Directory -Force $result | Out-Null
    $process=Start-Process -FilePath $Executable -ArgumentList @('-rr-verify',('-rr-'+$scenario+'-review'),'-rr-output',('"'+$result+'"'),'-rr-legacy-directory',('"'+$legacy+'"'),'-screen-fullscreen','0','-screen-width','1280','-screen-height','800','-force-d3d11','-logFile',('"'+(Join-Path $result 'player.log')+'"')) -PassThru
    if (!$process.WaitForExit(360000)) { throw 'Orchard verification exceeded six minutes; player retained for diagnosis.' }
    $report=Get-Content (Join-Path $result 'runtime-report.json') -Raw | ConvertFrom-Json
    $report | Select-Object result,assertions,workload
    if ($process.ExitCode -ne 0 -or $report.result -ne 'PASS') { throw ($report.errors -join "`n") }
}
