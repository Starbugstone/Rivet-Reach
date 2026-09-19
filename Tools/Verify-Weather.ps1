param([string]$OutputDirectory,[switch]$Build,[string]$SaveDirectory,[switch]$Legacy)
$ErrorActionPreference='Stop'
$project=Split-Path $PSScriptRoot -Parent
if (!$OutputDirectory) { $OutputDirectory=Join-Path $project ('Logs\Weather-'+[DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss')) }
New-Item -ItemType Directory -Force $OutputDirectory | Out-Null
if ($Build) {
    $request=Join-Path $project 'Logs\build-request.txt';$result=Join-Path $project 'Logs\build-result.txt'
    if (Test-Path $request) { throw 'Another Editor build is pending.' }
    if (Test-Path $result) { Remove-Item $result }
    Set-Content $request 'weather-player' -NoNewline
    $deadline=(Get-Date).AddMinutes(15)
    while (!(Test-Path $result) -and (Get-Date) -lt $deadline) { Start-Sleep -Seconds 2 }
    if (!(Test-Path $result)) { throw 'Editor build timed out; pending request preserved.' }
    if (!(Get-Content $result -Raw).StartsWith('SUCCESS')) { throw (Get-Content $result -Raw) }
}
$player=Join-Path $project 'Builds\Weather\RivetReach.exe'
$arguments=@('-screen-fullscreen','0','-screen-width','1280','-screen-height','800','-rr-verify','-rr-weather-review','-rr-output',('"'+$OutputDirectory+'"'),'-logFile',('"'+$OutputDirectory+'\player.log"'))
if ($SaveDirectory) {
    $arguments=$arguments | Where-Object { $_ -ne '-rr-weather-review' }
    $arguments+=@('-rr-weather-resume-review','-rr-save-directory',('"'+$SaveDirectory+'"'))
    if ($Legacy) { $arguments+='-rr-weather-legacy-review' }
} elseif ($Legacy) { throw 'Legacy review requires an isolated SaveDirectory.' }
$process=Start-Process -FilePath $player -ArgumentList $arguments -PassThru
if (!$process.WaitForExit(300000)) { throw 'Weather review timed out; player preserved for diagnosis.' }
Set-Content (Join-Path $OutputDirectory 'exit-code.txt') $process.ExitCode
$report=Get-Content -Raw (Join-Path $OutputDirectory 'runtime-report.json') | ConvertFrom-Json
$report | Select-Object result,assertions,workload
if ($process.ExitCode -ne 0 -or $report.result -ne 'PASS') { throw ($report.errors -join "`n") }

$coverage=Join-Path $OutputDirectory 'weather-coverage.txt'
if (Test-Path $coverage) {
    $views=0
    foreach ($line in (Get-Content $coverage)) {
        if ($line -match 'screen bands ([0-9,]+)') {
            $bands=@($Matches[1].Split(',') | ForEach-Object { [int]$_ })
            $total=($bands | Measure-Object -Sum).Sum
            if ($bands.Count -ne 8 -or $bands[0] -lt $total/32 -or $bands[7] -lt $total/32) { throw ('Sparse rain at viewport edge: '+$line) }
            $views++
        }
    }
    if ($views -ne 4) { throw 'Missing camera-angle coverage evidence.' }
    Write-Output 'PASS rain coverage at both viewport edges in four camera directions.'
}
