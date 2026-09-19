param([string]$OutputDirectory,[string]$SaveDirectory,[switch]$Focused,[switch]$Legacy)
$ErrorActionPreference='Stop'
$project=Split-Path $PSScriptRoot -Parent
if (!$OutputDirectory) { $OutputDirectory=Join-Path $project ('Logs\ToolDurability\Runtime-'+[DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss')) }
New-Item -ItemType Directory -Force $OutputDirectory | Out-Null
$scenario=if ($Legacy) {'-rr-tools-legacy'} elseif ($SaveDirectory) {'-rr-tools-resume'} else {'-rr-tools-review'}
$arguments=@('-screen-fullscreen','0','-screen-width','1280','-screen-height','800','-rr-verify',$scenario,'-rr-output',('"'+$OutputDirectory+'"'),'-logFile',('"'+$OutputDirectory+'\player.log"'))
if ($SaveDirectory) { $arguments+=@('-rr-save-directory',('"'+$SaveDirectory+'"')) }
if ($Focused) { $arguments+='-rr-tools-focused' }
$process=Start-Process -FilePath (Join-Path $project 'Builds\Tools\RivetReach.exe') -ArgumentList $arguments -PassThru
if (!$process.WaitForExit(1200000)) { throw 'Tools review exceeded 20 minutes; player preserved for diagnosis.' }
Set-Content (Join-Path $OutputDirectory 'exit-code.txt') $process.ExitCode
$report=Get-Content -Raw (Join-Path $OutputDirectory 'runtime-report.json') | ConvertFrom-Json
$report | Select-Object result,assertions,workload
if ($process.ExitCode -ne 0 -or $report.result -ne 'PASS') { throw ($report.errors -join "`n") }
