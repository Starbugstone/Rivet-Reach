param([string]$Executable='Builds\EquipmentArt\RivetReach.exe')
$ErrorActionPreference='Stop'
$project=Split-Path $PSScriptRoot -Parent
$output=Join-Path $project 'Logs\ArmorArt\Runtime'
New-Item -ItemType Directory -Force $output | Out-Null
$process=Start-Process (Join-Path $project $Executable) -ArgumentList @('-force-d3d11','-screen-fullscreen','0','-screen-width','1280','-screen-height','720','-rr-verify','-rr-equipment-art-review','-rr-output',('"'+$output+'"'),'-logFile',('"'+$output+'\player.log"')) -PassThru
if (!$process.WaitForExit(240000)) { throw 'Equipment review timed out; player preserved for diagnosis.' }
Get-Content (Join-Path $output 'runtime-report.json')
if ($process.ExitCode -ne 0) { throw "Equipment review exited $($process.ExitCode)" }
