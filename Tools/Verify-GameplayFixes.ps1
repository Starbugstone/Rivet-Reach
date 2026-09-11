param([Parameter(Mandatory=$true)][string]$Executable,[Parameter(Mandatory=$true)][string]$OutputDirectory)
$ErrorActionPreference='Stop'
if (Test-Path $OutputDirectory) { throw 'Use a fresh verification directory; existing evidence and saves are preserved.' }
New-Item -ItemType Directory -Force $OutputDirectory | Out-Null
$start=New-Object System.Diagnostics.ProcessStartInfo
$start.FileName=$Executable
$start.Arguments=@('-screen-fullscreen','0','-screen-width','1280','-screen-height','720','-rr-verify','-rr-gameplay-fixes-review','-rr-output',('"'+$OutputDirectory+'"'),'-logFile',('"'+(Join-Path $OutputDirectory 'player.log')+'"')) -join ' '
$start.UseShellExecute=$false
$process=[System.Diagnostics.Process]::Start($start)
if (!$process.WaitForExit(600000)) { throw 'Gameplay verification exceeded ten minutes; process retained for diagnosis.' }
$report=Join-Path $OutputDirectory 'runtime-report.json'
if (!(Test-Path $report)) { throw 'Missing gameplay report; inspect player.log.' }
$data=Get-Content -Raw $report | ConvertFrom-Json
if ($process.ExitCode -ne 0 -or $data.result -ne 'PASS') { $data.errors | Write-Output;throw "Gameplay verification failed: exit $($process.ExitCode)." }
$files=@(Get-ChildItem (Join-Path $OutputDirectory 'Saves') -File)
if ($files.Count -ne 1) { throw 'Quit created an unexpected checkpoint or backup.' }
$before=(Get-FileHash (Join-Path $OutputDirectory 'checkpoint-before.rrsave') -Algorithm SHA256).Hash
$after=(Get-FileHash $files[0].FullName -Algorithm SHA256).Hash
if ($before -ne $after) { throw 'Quit Without Saving changed the existing checkpoint.' }
$result="PASS: $($data.assertions) runtime assertions; actual pause-menu Quit exited $($process.ExitCode); checkpoint SHA256 unchanged: $after"
Set-Content (Join-Path $OutputDirectory 'quit-result.txt') $result
Write-Output $result
