param([string]$OutputDirectory,[switch]$Build)
$ErrorActionPreference = 'Stop'
$project = Split-Path $PSScriptRoot -Parent
if (!$OutputDirectory) { $OutputDirectory = Join-Path $project 'Logs\TerrainVerification' }
New-Item -ItemType Directory -Force $OutputDirectory | Out-Null
if ($Build) {
    # Use the already-open pinned Editor; never launch a competing instance for this project.
    $request = Join-Path $project 'Logs\build-request.txt'
    $result = Join-Path $project 'Logs\build-result.txt'
    if (Test-Path $request) { throw 'Another local build request is pending. Preserve it and wait for its owner.' }
    if (Test-Path $result) { Remove-Item $result }
    Set-Content -Encoding ascii $request 'terrain-build'
    $deadline = [DateTime]::UtcNow.AddMinutes(10)
    while (!(Test-Path $result) -and [DateTime]::UtcNow -lt $deadline) { Start-Sleep -Seconds 2 }
    if (!(Test-Path $result)) { throw 'Editor build timed out. Inspect Logs; the request/process was preserved.' }
    $status = Get-Content -Raw $result
    if (!$status.StartsWith('SUCCESS')) { throw $status }
}
$executable = Join-Path $project 'Builds\Terrain\RivetReach.exe'
if (!(Test-Path $executable)) { throw 'Use -Build with the pinned Editor open after coordinating shared build ownership.' }
$arguments = @('-screen-fullscreen','0','-screen-width','1280','-screen-height','720','-rr-verify','-rr-terrain-review','-rr-output',('"'+$OutputDirectory+'"'),'-logFile',('"'+(Join-Path $OutputDirectory 'player.log')+'"'))
$process = Start-Process -FilePath $executable -ArgumentList $arguments -PassThru
if (!$process.WaitForExit(600000)) { throw 'Terrain verification exceeded ten minutes. Player remains running for diagnosis.' }
$report = Join-Path $OutputDirectory 'runtime-report.json'
if (!(Test-Path $report)) { throw 'Player exited without a report; inspect player.log.' }
Get-Content $report
if ($process.ExitCode -ne 0 -or (Get-Content -Raw $report | ConvertFrom-Json).result -ne 'PASS') { throw 'Terrain player verification failed.' }
