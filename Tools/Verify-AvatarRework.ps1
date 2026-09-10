param([switch]$Gameplay)
$ErrorActionPreference='Stop'
$project=Split-Path $PSScriptRoot -Parent
$mode=if ($Gameplay) { 'gameplay' } else { 'studio' }
$output=Join-Path $project ('Logs\AvatarRework\'+$mode)
New-Item -ItemType Directory -Force $output | Out-Null
$flag=if ($Gameplay) { '-rr-avatar-rework' } else { '-rr-avatar-verify' }
$process=Start-Process (Join-Path $project 'Builds\AvatarRework\RivetReach.exe') -ArgumentList @('-screen-fullscreen','0','-screen-width','1280','-screen-height','720',$flag,'-rr-output',('"'+$output+'"'),'-logFile',('"'+(Join-Path $output 'player.log')+'"')) -PassThru
if (!$process.WaitForExit(180000)) { throw 'Avatar verification timed out; player left available for diagnosis.' }
Get-Content (Join-Path $output 'avatar-report.json')
if ($process.ExitCode -ne 0) { throw "Avatar verification exited $($process.ExitCode)" }
