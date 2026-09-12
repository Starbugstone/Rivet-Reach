param([string]$Executable,[string]$OutputDirectory)
$ErrorActionPreference='Stop'
$project=Split-Path $PSScriptRoot -Parent
if (!$Executable) { $Executable=Join-Path $project 'Builds\HandCrank\RivetReach.exe' }
if (!$OutputDirectory) { $OutputDirectory=Join-Path $project 'Logs\HandCrankVerification' }
New-Item -ItemType Directory -Force $OutputDirectory | Out-Null
$process=Start-Process -FilePath $Executable -ArgumentList @('-rr-verify','-rr-hand-crank-review','-rr-output',('"'+$OutputDirectory+'"'),'-screen-fullscreen','0','-screen-width','1280','-screen-height','800','-force-d3d11','-logFile',('"'+(Join-Path $OutputDirectory 'player.log')+'"')) -PassThru
if (!$process.WaitForExit(240000)) { Stop-Process -Id $process.Id; throw 'Hand crank verification exceeded four minutes; inspect player.log.' }
$report=Get-Content (Join-Path $OutputDirectory 'runtime-report.json') -Raw | ConvertFrom-Json
$report | Select-Object result,assertions,workload
if ($process.ExitCode -ne 0 -or $report.result -ne 'PASS') { throw ($report.errors -join "`n") }
