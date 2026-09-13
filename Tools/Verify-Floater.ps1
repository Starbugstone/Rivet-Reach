param([string]$Player,[string]$OutputDirectory)
$ErrorActionPreference = 'Stop'
$project = Split-Path $PSScriptRoot -Parent
if (!$Player) { $Player = Join-Path $project 'Builds\Floater\RivetReach.exe' }
if (!$OutputDirectory) { $OutputDirectory = Join-Path $project 'Logs\FloaterFocused' }
New-Item -ItemType Directory -Force $OutputDirectory | Out-Null
$arguments = @('-force-d3d11','-screen-fullscreen','0','-screen-width','1280','-screen-height','720','-rr-mob-verify','-rr-floater-only','-rr-output',('"'+$OutputDirectory+'"'),'-logFile',('"'+(Join-Path $OutputDirectory 'player.log')+'"'))
$process = Start-Process -FilePath $Player -ArgumentList $arguments -PassThru
if (!$process.WaitForExit(240000)) { throw 'Floater checks exceeded 240 seconds; player left running for diagnosis.' }
Get-Content (Join-Path $OutputDirectory 'mob-runtime-report.json')
if ($process.ExitCode -ne 0) { throw "Floater verification failed: $($process.ExitCode)." }
