param([string]$Executable,[switch]$CaptureVideo,[int]$TimeoutSeconds=180)
$ErrorActionPreference='Stop'
$project=Split-Path $PSScriptRoot -Parent
if (!$Executable) { $Executable=Join-Path $project 'Builds\Eating\RivetReach.exe' }
$output=Join-Path $project 'Logs\Eating\gameplay'
New-Item -ItemType Directory -Force $output | Out-Null
$report=Join-Path $output 'report.json'
if (Test-Path $report) { Remove-Item $report }
if ($CaptureVideo) { Get-ChildItem (Join-Path $output 'frames') -Filter 'eat-*.png' -ErrorAction SilentlyContinue | Remove-Item }
$arguments=@('-force-d3d11','-screen-fullscreen','0','-screen-width','1280','-screen-height','720','-rr-eating-review','-rr-output',('"'+$output+'"'),'-logFile',('"'+(Join-Path $output 'player.log')+'"'))
if ($CaptureVideo) { $arguments+='-rr-eating-video' }
$process=Start-Process $Executable -ArgumentList $arguments -PassThru
if (!$process.WaitForExit($TimeoutSeconds*1000)) { throw 'Eating verification timed out; player left available for diagnosis.' }
Get-Content $report
if ($process.ExitCode -ne 0) { throw "Eating verification exited $($process.ExitCode)" }
