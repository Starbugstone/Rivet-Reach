param([string]$Player,[string]$OutputDirectory,[string]$LegacyDirectory)
$ErrorActionPreference = 'Stop'
$project = Split-Path $PSScriptRoot -Parent
if (!$Player) { $Player = Join-Path $project 'Builds\Bridges\RivetReach.exe' }
if (!$OutputDirectory) { $OutputDirectory = Join-Path $project 'Logs\BridgesFocused' }
New-Item -ItemType Directory -Force $OutputDirectory | Out-Null
$arguments = @('-force-d3d11','-screen-fullscreen','0','-screen-width','1280','-screen-height','720','-rr-verify','-rr-bridges-review','-rr-output',('"'+$OutputDirectory+'"'),'-logFile',('"'+(Join-Path $OutputDirectory 'player.log')+'"'))
if ($LegacyDirectory) { $arguments += @('-rr-bridge-legacy-directory',('"'+$LegacyDirectory+'"')) }
$process = Start-Process -FilePath $Player -ArgumentList $arguments -PassThru
if (!$process.WaitForExit(360000)) { throw 'Bridge checks exceeded 360 seconds; player left running for diagnosis.' }
Get-Content (Join-Path $OutputDirectory 'runtime-report.json')
if ($process.ExitCode -ne 0) { throw "Bridge verification failed: $($process.ExitCode)." }
