param([string]$Executable,[string]$OutputDirectory,[int]$Width=1280,[int]$Height=720)
$ErrorActionPreference='Stop'
$project=Split-Path $PSScriptRoot -Parent
if (!$Executable) { $Executable=Join-Path $project 'Builds\Inventory\RivetReach.exe' }
if (!$OutputDirectory) { $OutputDirectory=Join-Path $project ('Logs\Inventory\Runtime-'+[DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss')) }
if (Test-Path $OutputDirectory) { throw 'Use a fresh output directory; existing evidence and saves are preserved.' }
New-Item -ItemType Directory -Force $OutputDirectory | Out-Null
$start=New-Object System.Diagnostics.ProcessStartInfo
$start.FileName=$Executable
$start.UseShellExecute=$false
$start.Arguments=@('-rr-verify','-rr-inventory-review','-rr-output',('"'+$OutputDirectory+'"'),'-screen-fullscreen','0','-screen-width',$Width,'-screen-height',$Height,'-force-d3d11','-logFile',('"'+(Join-Path $OutputDirectory 'player.log')+'"')) -join ' '
$process=[System.Diagnostics.Process]::Start($start)
if (!$process.WaitForExit(360000)) { throw 'Inventory verification exceeded six minutes; player retained for diagnosis.' }
$report=Get-Content (Join-Path $OutputDirectory 'runtime-report.json') -Raw | ConvertFrom-Json
Set-Content (Join-Path $OutputDirectory 'exit-code.txt') $process.ExitCode
$report | Select-Object result,assertions,workload
if ($process.ExitCode -ne 0 -or $report.result -ne 'PASS') { throw ($report.errors -join "`n") }
