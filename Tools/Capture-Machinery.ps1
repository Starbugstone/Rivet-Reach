param([string]$OutputDirectory,[string]$PlayerPath)
$ErrorActionPreference = 'Stop'
$project = Split-Path $PSScriptRoot -Parent
if (!$OutputDirectory) { $OutputDirectory = Join-Path $project ('Logs\MachineryShowcase-' + (Get-Date -Format 'yyyyMMdd-HHmmss')) }
if (!$PlayerPath) { $PlayerPath = Join-Path $project 'Builds\MachineryShowcase\RivetReach.exe' }
if (!(Test-Path $PlayerPath)) { throw 'Build with the pinned Unity Editor: Rivet Reach > Build Machinery Showcase.' }
New-Item -ItemType Directory -Force $OutputDirectory | Out-Null
if (Test-Path (Join-Path $OutputDirectory 'runtime-report.json')) { throw 'Choose a fresh output directory to preserve the previous capture.' }
$arguments = @('-screen-fullscreen','0','-screen-width','1920','-screen-height','1080','-rr-verify','-rr-multiblock-review','-rr-machinery-showcase','-rr-output',('"'+$OutputDirectory+'"'),'-logFile',('"'+(Join-Path $OutputDirectory 'player.log')+'"'))
$process = Start-Process -FilePath $PlayerPath -ArgumentList $arguments -PassThru
if (!$process.WaitForExit(600000)) { throw 'Capture exceeded ten minutes. Player remains available for diagnosis.' }
$reportPath = Join-Path $OutputDirectory 'runtime-report.json'
if (!(Test-Path $reportPath)) { throw 'No capture report; inspect player.log.' }
$report = Get-Content -Raw $reportPath | ConvertFrom-Json
if ($process.ExitCode -ne 0 -or $report.result -ne 'PASS') { throw (Get-Content -Raw $reportPath) }
$images = Get-ChildItem $OutputDirectory -Filter 'machinery-*.png'
if ($images.Count -ne 6) { throw 'Expected six completed showcase screenshots.' }
$images | Select-Object Name,Length,FullName
