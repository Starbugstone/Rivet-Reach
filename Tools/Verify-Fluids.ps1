param([string]$OutputDirectory,[switch]$Build)
$ErrorActionPreference = 'Stop'
$project = Split-Path $PSScriptRoot -Parent
if (!$OutputDirectory) { $OutputDirectory = Join-Path $project 'Logs\FluidVerification' }
New-Item -ItemType Directory -Force $OutputDirectory | Out-Null
if ($Build) {
    $request = Join-Path $project 'Logs\build-request.txt'
    $result = Join-Path $project 'Logs\build-result.txt'
    if (Test-Path $request) { throw 'Another local build request is pending.' }
    if (Test-Path $result) { Remove-Item $result }
    Set-Content -Encoding ascii $request 'fluid-build'
    $deadline = [DateTime]::UtcNow.AddMinutes(10)
    while (!(Test-Path $result) -and [DateTime]::UtcNow -lt $deadline) { Start-Sleep -Seconds 2 }
    if (!(Test-Path $result)) { throw 'Editor build timed out; inspect its log.' }
    if (!(Get-Content -Raw $result).StartsWith('SUCCESS')) { throw (Get-Content -Raw $result) }
}
$executable = Join-Path $project 'Builds\Fluids\RivetReach.exe'
if (!(Test-Path $executable)) { throw 'Use -Build with the pinned Editor open.' }
$arguments = @('-screen-fullscreen','0','-screen-width','1280','-screen-height','720','-rr-verify','-rr-fluid-review','-rr-output',('"'+$OutputDirectory+'"'),'-logFile',('"'+(Join-Path $OutputDirectory 'player.log')+'"'))
$process = Start-Process -FilePath $executable -ArgumentList $arguments -PassThru
if (!$process.WaitForExit(900000)) { throw 'Fluid review exceeded fifteen minutes. Player remains available for diagnosis.' }
$report = Join-Path $OutputDirectory 'runtime-report.json'
if (!(Test-Path $report)) { throw 'No runtime report; inspect player.log.' }
Get-Content $report
if ($process.ExitCode -ne 0 -or (Get-Content -Raw $report | ConvertFrom-Json).result -ne 'PASS') { throw 'Fluid verification failed.' }
