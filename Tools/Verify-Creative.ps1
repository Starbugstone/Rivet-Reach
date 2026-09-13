param([string]$OutputDirectory,[switch]$Build,[switch]$FullRun,[switch]$Mining,[string]$Executable)
$ErrorActionPreference = 'Stop'
$project = Split-Path $PSScriptRoot -Parent
if (!$OutputDirectory) { $OutputDirectory = Join-Path $project 'Logs\CreativeVerification' }
New-Item -ItemType Directory -Force $OutputDirectory | Out-Null
if ($Build) {
    $request = Join-Path $project 'Logs\build-request.txt'
    $result = Join-Path $project 'Logs\build-result.txt'
    if (Test-Path $request) { throw 'Another local build request is pending.' }
    if (Test-Path $result) { Remove-Item $result }
    Set-Content -Encoding ascii $request $(if ($Mining) { 'creative-mining-build' } else { 'creative-build' })
    $deadline = [DateTime]::UtcNow.AddMinutes(10)
    while (!(Test-Path $result) -and [DateTime]::UtcNow -lt $deadline) { Start-Sleep -Seconds 2 }
    if (!(Test-Path $result)) { throw 'Editor build timed out; inspect its log.' }
    if (!(Get-Content -Raw $result).StartsWith('SUCCESS')) { throw (Get-Content -Raw $result) }
}
if (!$Executable) { $Executable = Join-Path $project 'Builds\Creative\RivetReach.exe' }
if (!(Test-Path $executable)) { throw 'Use -Build with the pinned Editor open.' }
$report = Join-Path $OutputDirectory 'runtime-report.json'
if (Test-Path $report) { Remove-Item $report }
$scenarioFlag = if ($Mining) { '-rr-creative-mining-review' } else { '-rr-creative-review' }
$arguments = @('-screen-fullscreen','0','-screen-width','1280','-screen-height','720','-rr-verify',$scenarioFlag,'-rr-output',('"'+$OutputDirectory+'"'),'-logFile',('"'+(Join-Path $OutputDirectory 'player.log')+'"'))
$process = Start-Process -FilePath $executable -ArgumentList $arguments -PassThru
if (!$process.WaitForExit(900000)) { throw 'Creative review exceeded fifteen minutes. Player remains available for diagnosis.' }
$report = Join-Path $OutputDirectory 'runtime-report.json'
if (!(Test-Path $report)) { throw 'No runtime report; inspect player.log.' }
Get-Content $report
if ($process.ExitCode -ne 0 -or (Get-Content -Raw $report | ConvertFrom-Json).result -ne 'PASS') { throw 'Creative verification failed.' }

if ($FullRun) {
    # All scenarios use the exact player just checked above. Workshop fixtures opt into Creative.
    foreach ($scenario in @('industry','multiblock','placement-items','browser','survival','workshop-followup')) {
        $scenarioOutput = Join-Path $OutputDirectory $scenario
        New-Item -ItemType Directory -Force $scenarioOutput | Out-Null
        $scenarioReport = Join-Path $scenarioOutput 'runtime-report.json'
        if (Test-Path $scenarioReport) { Remove-Item $scenarioReport }
        $arguments = @('-screen-fullscreen','0','-screen-width','1280','-screen-height','720','-rr-verify',('-rr-'+$scenario+'-review'),'-rr-output',('"'+$scenarioOutput+'"'),'-logFile',('"'+(Join-Path $scenarioOutput 'player.log')+'"'))
        if ($scenario -in @('industry','multiblock','workshop-followup')) { $arguments += '-rr-creative-workshop' }
        $process = Start-Process -FilePath $executable -ArgumentList $arguments -PassThru
        if (!$process.WaitForExit(900000)) { throw "$scenario exceeded fifteen minutes; player preserved for diagnosis." }
        if (!(Test-Path $scenarioReport)) { throw "No $scenario report; inspect player.log." }
        Get-Content $scenarioReport
        if ($process.ExitCode -ne 0 -or (Get-Content -Raw $scenarioReport | ConvertFrom-Json).result -ne 'PASS') { throw "$scenario verification failed." }
    }
}
