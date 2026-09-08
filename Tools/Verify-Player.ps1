param([string]$OutputDirectory)
$ErrorActionPreference='Stop'
$project=Split-Path $PSScriptRoot -Parent
if (!$OutputDirectory) { $OutputDirectory=Join-Path $project 'Logs\PlayerRevision4Verification' }
New-Item -ItemType Directory -Force $OutputDirectory | Out-Null
$executable=Join-Path $project 'Builds\PlayerRevision4\RivetReach.exe'
$process=Start-Process -FilePath $executable -ArgumentList @('-screen-fullscreen','0','-screen-width','1280','-screen-height','900','-rr-avatar-verify','-rr-output',('"'+$OutputDirectory+'"'),'-logFile',('"'+(Join-Path $OutputDirectory 'player.log')+'"')) -PassThru
if (!$process.WaitForExit(90000)) { throw 'Player art verification timed out; process left available for diagnosis.' }
Get-Content (Join-Path $OutputDirectory 'avatar-report.json')
if ($process.ExitCode -ne 0) { throw "Player art verification failed: $($process.ExitCode)." }
