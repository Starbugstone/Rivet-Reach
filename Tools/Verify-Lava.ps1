param([string]$OutputDirectory,[switch]$Build)
$ErrorActionPreference='Stop'
$project=Split-Path $PSScriptRoot -Parent
if (!$OutputDirectory) { $OutputDirectory=Join-Path $project ('Logs\Lava-'+[DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss')) }
if (Test-Path $OutputDirectory) { throw 'Use a fresh verification directory.' }
if ($Build) {
    $request=Join-Path $project 'Logs\lava-build-request.txt'
    $result=Join-Path $project 'Logs\lava-build-result.txt'
    if (Test-Path $request) { throw 'A lava build request is already pending.' }
    if (Test-Path $result) { Remove-Item $result }
    Set-Content -Encoding ascii $request 'build'
    $deadline=[DateTime]::UtcNow.AddMinutes(15)
    while (!(Test-Path $result) -and [DateTime]::UtcNow -lt $deadline) { Start-Sleep -Seconds 2 }
    if (!(Test-Path $result)) { throw 'Lava editor build timed out; inspect the editor log.' }
    if (!(Get-Content -Raw $result).StartsWith('SUCCESS')) { throw (Get-Content -Raw $result) }
}
New-Item -ItemType Directory -Force $OutputDirectory | Out-Null
$exe=Join-Path $project 'Builds\Lava\RivetReach.exe'
$arguments=@('-screen-fullscreen','0','-screen-width','1280','-screen-height','720','-rr-verify','-rr-lava-review','-rr-output',('"'+$OutputDirectory+'"'),'-logFile',('"'+(Join-Path $OutputDirectory 'player.log')+'"'))
$process=Start-Process -FilePath $exe -ArgumentList $arguments -PassThru
if (!$process.WaitForExit(900000)) { throw 'Lava review exceeded fifteen minutes; player preserved for diagnosis.' }
Set-Content (Join-Path $OutputDirectory 'exit-code.txt') $process.ExitCode
$report=Get-Content -Raw (Join-Path $OutputDirectory 'runtime-report.json') | ConvertFrom-Json
Write-Output "$($report.result): $($report.assertions) assertions; exit $($process.ExitCode)"
if ($process.ExitCode -ne 0 -or $report.result -ne 'PASS') { $report.errors | Write-Output;throw 'Lava review failed.' }
