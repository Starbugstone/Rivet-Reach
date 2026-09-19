param([string]$OutputDirectory,[string]$SaveDirectory,[switch]$LegacyRenewables,[switch]$FinalBuild,[switch]$KitchenOnly)
$ErrorActionPreference='Stop'
$project=Split-Path $PSScriptRoot -Parent
if (!$OutputDirectory) { $OutputDirectory=Join-Path $project ('Logs\FoodBalance-'+[DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss')) }
New-Item -ItemType Directory -Force $OutputDirectory | Out-Null
$player=Join-Path $project $(if ($FinalBuild) {'Builds\FoodBalanceFinal\RivetReach.exe'} else {'Builds\FoodBalance\RivetReach.exe'})
$scenario='-rr-food-balance-review'
if ($SaveDirectory) { $scenario=if ($LegacyRenewables) {'-rr-renewables-resume-review'} else {'-rr-food-balance-resume-review'} }
$arguments=@('-screen-fullscreen','0','-screen-width','1280','-screen-height','800','-rr-verify',$scenario,'-rr-output',('"'+$OutputDirectory+'"'),'-logFile',('"'+$OutputDirectory+'\player.log"'))
if ($SaveDirectory) { $arguments+=@('-rr-save-directory',('"'+$SaveDirectory+'"')) }
if ($LegacyRenewables) { $arguments+='-rr-food-legacy' }
if ($KitchenOnly) { $arguments+='-rr-food-kitchen-review' }
$process=Start-Process -FilePath $player -ArgumentList $arguments -PassThru
if (!$process.WaitForExit(2100000)) { throw 'Food review exceeded 35 minutes; player preserved for diagnosis.' }
Set-Content (Join-Path $OutputDirectory 'exit-code.txt') $process.ExitCode
$report=Get-Content -Raw (Join-Path $OutputDirectory 'runtime-report.json') | ConvertFrom-Json
$report | Select-Object result,assertions,workload
if ($process.ExitCode -ne 0 -or $report.result -ne 'PASS') { throw ($report.errors -join "`n") }
