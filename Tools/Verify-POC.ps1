param([string]$OutputDirectory,[switch]$Trees,[switch]$Crafting,[switch]$Audio,[switch]$Ores,[switch]$Arcade,[switch]$Survival,[switch]$PlacementItems,[string]$Executable,[ValidateRange(1,1800)][int]$TimeoutSeconds=600)
$ErrorActionPreference = 'Stop'
$project = Split-Path $PSScriptRoot -Parent
if (!$OutputDirectory) { $OutputDirectory = Join-Path $project 'Logs\POCVerification' }
New-Item -ItemType Directory -Force $OutputDirectory | Out-Null
if (!$Executable) { $Executable = Join-Path $project 'Builds\PlayerRevision4\RivetReach.exe' }
if (!(Test-Path $Executable)) { throw 'Build first with Tools/Build-Windows.ps1, or supply -Executable.' }
$arguments = @('-screen-fullscreen','0','-screen-width','1280','-screen-height','720','-rr-verify','-rr-output',('"'+$OutputDirectory+'"'),'-logFile',('"'+(Join-Path $OutputDirectory 'player.log')+'"'))
if ($Trees) { $arguments += '-rr-tree-review' }
if ($Crafting) { $arguments += '-rr-crafting-review' }
if ($Survival) { $arguments += '-rr-survival-review' }
if ($PlacementItems) { $arguments += '-rr-placement-items-review' }
if ($Audio) { $arguments += '-rr-audio-review' }
if ($Ores) { $arguments += '-rr-ore-review' }
if ($Arcade) { $arguments += '-rr-arcade-review' }
$process = Start-Process -FilePath $Executable -ArgumentList $arguments -PassThru
if (!$process.WaitForExit($TimeoutSeconds*1000)) { throw "Verification exceeded $TimeoutSeconds seconds. The player was left running for diagnosis." }
Get-Content (Join-Path $OutputDirectory 'runtime-report.json')
if ($process.ExitCode -ne 0) { throw "Player verification failed: $($process.ExitCode)." }
