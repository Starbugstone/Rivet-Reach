param([string]$OutputDirectory,[string]$PlayerPath)
$ErrorActionPreference='Stop'
$project=Split-Path $PSScriptRoot -Parent
if(!$OutputDirectory){$OutputDirectory=Join-Path $project ('Logs\StarterStationsRuntime-'+(Get-Date -Format 'yyyyMMdd-HHmmss'))}
if(!$PlayerPath){$PlayerPath=Join-Path $project 'Builds\StarterStations\RivetReach.exe'}
if(!(Test-Path $PlayerPath)){throw 'Build with Rivet Reach > Build starter station graphics review in the pinned Unity Editor.'}
if(Test-Path (Join-Path $OutputDirectory 'runtime-report.json')){throw 'Choose a fresh output directory.'}
New-Item -ItemType Directory -Force $OutputDirectory | Out-Null
$arguments=@('-screen-fullscreen','0','-screen-width','1600','-screen-height','900','-rr-verify','-rr-starter-stations-review','-rr-output',('"'+$OutputDirectory+'"'),'-logFile',('"'+(Join-Path $OutputDirectory 'player.log')+'"'))
$process=Start-Process -FilePath $PlayerPath -ArgumentList $arguments -PassThru
if(!$process.WaitForExit(600000)){throw 'Station review exceeded ten minutes; player remains available for diagnosis.'}
$path=Join-Path $OutputDirectory 'runtime-report.json'
if(!(Test-Path $path)){throw 'No runtime report; inspect player.log.'}
$report=Get-Content -Raw $path | ConvertFrom-Json
if($process.ExitCode -ne 0 -or $report.result -ne 'PASS'){throw (Get-Content -Raw $path)}
$report | Select-Object result,assertions,unity,workload
