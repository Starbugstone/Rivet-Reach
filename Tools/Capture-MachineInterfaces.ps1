param([string]$Executable,[string]$OutputDirectory,[int]$Width=1280,[int]$Height=720)
$ErrorActionPreference='Stop'
$project=Split-Path $PSScriptRoot -Parent
if (!$Executable) { $Executable=Join-Path $project 'Builds\MachineInterface\RivetReach.exe' }
if (!$OutputDirectory) { $OutputDirectory=Join-Path $project ('Logs\MachineInterface\Runtime-'+[DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss')) }
if (Test-Path $OutputDirectory) { throw 'Choose a fresh output directory to preserve prior captures.' }
New-Item -ItemType Directory -Force $OutputDirectory | Out-Null
$process=Start-Process -FilePath $Executable -PassThru -ArgumentList @('-rr-verify','-rr-machine-interface-review','-rr-output',('"'+$OutputDirectory+'"'),'-screen-fullscreen','0','-screen-width',$Width,'-screen-height',$Height,'-force-d3d11','-logFile',('"'+(Join-Path $OutputDirectory 'player.log')+'"'))
if (!$process.WaitForExit(360000)) { throw 'Capture exceeded six minutes; player preserved for diagnosis.' }
Set-Content (Join-Path $OutputDirectory 'exit-code.txt') $process.ExitCode
$report=Get-Content (Join-Path $OutputDirectory 'runtime-report.json') -Raw | ConvertFrom-Json
$report | Select-Object result,assertions,width,height,workload
if ($process.ExitCode -ne 0 -or $report.result -ne 'PASS') { throw ($report.errors -join "`n") }
