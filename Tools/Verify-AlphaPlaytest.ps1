param([ValidateSet('Survival','Focused','Avatar')][string]$Mode='Survival',[string]$OutputDirectory)
$ErrorActionPreference='Stop'
$project=Split-Path $PSScriptRoot -Parent
if (!$OutputDirectory) { $OutputDirectory=Join-Path $project ('Logs\AlphaPlaytest\'+$Mode+'-'+[DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss')) }
New-Item -ItemType Directory -Force $OutputDirectory | Out-Null
$player=Join-Path $project 'Builds\AlphaPlaytest\RivetReach.exe'
if (!(Test-Path $player)) { throw 'Build AlphaPlaytest through the existing Editor dispatcher first.' }
$arguments=@('-screen-fullscreen','0','-screen-width','1280','-screen-height','800')
if ($Mode -eq 'Avatar') { $arguments+='-rr-avatar-verify' }
else { $arguments+='-rr-verify';if ($Mode -eq 'Survival') {$arguments+='-rr-alpha-survival-review'} else {$arguments+='-rr-alpha-playtest-review'} }
$arguments+=@('-rr-output',('"'+$OutputDirectory+'"'),'-logFile',('"'+$OutputDirectory+'\player.log"'))
$process=Start-Process -FilePath $player -ArgumentList $arguments -PassThru
$process.Id | Set-Content (Join-Path $OutputDirectory 'process-id.txt')
if (!$process.WaitForExit(600000)) { throw 'Alpha review timed out; player preserved for diagnosis.' }
$process.ExitCode | Set-Content (Join-Path $OutputDirectory 'exit-code.txt')
$reportName=if ($Mode -eq 'Avatar') {'avatar-report.json'} else {'runtime-report.json'}
$report=Get-Content -Raw (Join-Path $OutputDirectory $reportName) | ConvertFrom-Json
$report | Select-Object result,assertions,workload
if ($process.ExitCode -ne 0 -or $report.result -ne 'PASS') { throw ($report.errors -join "`n") }
