param([string]$OutputDirectory)
$ErrorActionPreference = 'Stop'
$project = Split-Path $PSScriptRoot -Parent
if (!$OutputDirectory) { $OutputDirectory = Join-Path $project 'Logs\POCVerification' }
New-Item -ItemType Directory -Force $OutputDirectory | Out-Null
$executable = Join-Path $project 'Builds\FirstPOC\RivetReach.exe'
if (!(Test-Path $executable)) { throw 'Build first with Tools/Build-Windows.ps1.' }
$process = Start-Process -FilePath $executable -ArgumentList @('-screen-fullscreen','0','-screen-width','1280','-screen-height','720','-rr-verify','-rr-output',('"'+$OutputDirectory+'"'),'-logFile',('"'+(Join-Path $OutputDirectory 'player.log')+'"')) -PassThru
if (!$process.WaitForExit(180000)) { throw 'Verification exceeded 180 seconds. The player was left running for diagnosis.' }
Get-Content (Join-Path $OutputDirectory 'runtime-report.json')
if ($process.ExitCode -ne 0) { throw "Player verification failed: $($process.ExitCode)." }
