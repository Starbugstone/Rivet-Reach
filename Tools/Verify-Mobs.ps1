param([string]$Player,[string]$OutputDirectory)
$ErrorActionPreference = 'Stop'
$project = Split-Path $PSScriptRoot -Parent
if (!$Player) { $Player = Join-Path $project 'Builds\Mobs\RivetReach.exe' }
if (!$OutputDirectory) { $OutputDirectory = Join-Path $project 'Logs\MobVerification' }
New-Item -ItemType Directory -Force $OutputDirectory | Out-Null
if (!(Test-Path $Player)) { throw 'Build with Rivet Reach/Mobs/Build Windows review first.' }
$arguments = @('-screen-fullscreen','0','-screen-width','1280','-screen-height','720','-rr-mob-verify','-rr-output',('"'+$OutputDirectory+'"'),'-logFile',('"'+(Join-Path $OutputDirectory 'player.log')+'"'))
$process = Start-Process -FilePath $Player -ArgumentList $arguments -PassThru
if (!$process.WaitForExit(240000)) { throw 'Mob verification exceeded 240 seconds; player left running for diagnosis.' }
Get-Content (Join-Path $OutputDirectory 'mob-runtime-report.json')
if ($process.ExitCode -ne 0) { throw "Mob verification failed: $($process.ExitCode)." }
