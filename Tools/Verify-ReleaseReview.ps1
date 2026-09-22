param(
    [Parameter(Mandatory=$true)][string]$Executable,
    [Parameter(Mandatory=$true)][string]$OutputDirectory,
    [string[]]$Scenarios=@('alpha-survival','release-review','alpha-playtest','crafting','browser','inventory','inventory-gestures','tools','saves','multiblock','fluid','weather','renewables','crates','chicken'),
    [string]$FixtureDirectory,
    [int]$Width=1920,[int]$Height=1080,
    [int]$FrameLimit=-1,
    [switch]$GpuTelemetry,
    [int]$TimeoutSeconds=1200
)
$ErrorActionPreference='Stop'
if (Test-Path $OutputDirectory) { throw 'Use a fresh evidence directory.' }
if (!(Test-Path $Executable)) { throw 'Missing review executable.' }
New-Item -ItemType Directory -Force $OutputDirectory | Out-Null
$identity=[ordered]@{executable=$Executable;sha256=(Get-FileHash $Executable -Algorithm SHA256).Hash;started=[DateTime]::UtcNow.ToString('o');width=$Width;height=$Height;reviewFrameLimit=$FrameLimit;gpuTelemetry=[bool]$GpuTelemetry}
if ($GpuTelemetry) { $identity.gpuTimestampTimezone=[TimeZoneInfo]::Local.Id;$identity.gpuTimestampOffset=[DateTimeOffset]::Now.Offset.ToString() }
$assembly=Join-Path (Split-Path $Executable) 'RivetReach_Data\Managed\Assembly-CSharp.dll'
if (Test-Path $assembly) { $identity.assemblySha256=(Get-FileHash $assembly -Algorithm SHA256).Hash }
$identity | ConvertTo-Json | Set-Content (Join-Path $OutputDirectory 'build-identity.json')
$failed=@()
foreach ($scenario in $Scenarios) {
    $destination=Join-Path $OutputDirectory $scenario
    New-Item -ItemType Directory -Force $destination | Out-Null
    $known=@('factory-cpu','factory-isolation','fluid-stress','alpha-survival','release-review','release-legacy','alpha-playtest','crafting','browser','inventory','inventory-gestures','tools','saves','multiblock','fluid','weather','renewables','crates','chicken','bed','farming','compost','fishing','lighting','industry','connections','portable-storage','bridges','ranged-pump','electric-furnace','door','hand-crank','lava','interaction','food-balance','performance')
    if ($scenario -notin $known) { throw ('Unknown verification scenario: '+$scenario) }
    $flag=if ($scenario -eq 'fluid-stress') {'-rr-fluid-stress'} elseif ($scenario -eq 'release-legacy') {'-rr-release-legacy'} elseif ($scenario -in @('release-review','factory-isolation','factory-cpu')) {'-rr-release-review'} elseif ($scenario -eq 'saves') {'-rr-save-review'} else {'-rr-'+$scenario+'-review'}
    $arguments=@('-force-d3d11','-screen-fullscreen','1','-window-mode','borderless','-screen-width',$Width,'-screen-height',$Height,'-rr-verify',$flag,'-rr-output',('"'+$destination+'"'),'-rr-save-directory',('"'+(Join-Path $destination 'saves')+'"'),'-logFile',('"'+(Join-Path $destination 'player.log')+'"'))
    if ($scenario -eq 'release-legacy') {
        if (!(Test-Path $FixtureDirectory)) { throw 'Historical fixture directory is required.' }
        $arguments+=@('-rr-fixture-path',('"'+$FixtureDirectory+'"'))
    }
    if ($scenario -in @('release-review','factory-isolation','factory-cpu')) { $arguments+=@('-rr-frame-limit',$FrameLimit) }
    if ($scenario -in @('factory-isolation','factory-cpu')) { $arguments+='-rr-factory-isolation' }
    if ($scenario -eq 'factory-cpu') { $arguments+='-rr-factory-cpu-only' }
    $start=New-Object System.Diagnostics.ProcessStartInfo
    $start.FileName=$Executable;$start.Arguments=$arguments -join ' ';$start.UseShellExecute=$false
    $monitor=$null
    try {
        if ($GpuTelemetry) {
            $nvidia=Get-Command nvidia-smi -ErrorAction SilentlyContinue
            if ($nvidia) {
                $monitor=Start-Process -FilePath $nvidia.Source -ArgumentList '--query-gpu=timestamp,temperature.gpu,clocks.gr,utilization.gpu,power.draw,clocks_event_reasons.sw_thermal_slowdown','--format=csv','--loop=2' -NoNewWindow -PassThru -RedirectStandardOutput (Join-Path $destination 'gpu-telemetry.csv') -RedirectStandardError (Join-Path $destination 'gpu-telemetry-error.txt')
            } else { 'NVIDIA telemetry unavailable on this machine.' | Set-Content (Join-Path $destination 'gpu-telemetry-error.txt') }
        }
        $process=[System.Diagnostics.Process]::Start($start)
        if (!$process.WaitForExit($TimeoutSeconds*1000)) { throw "$scenario timed out; player preserved for diagnosis." }
    } finally {
        if ($monitor -and !$monitor.HasExited) { $monitor.Kill();$monitor.WaitForExit() }
    }
    $process.ExitCode | Set-Content (Join-Path $destination 'exit-code.txt')
    $path=Join-Path $destination 'runtime-report.json'
    if (!(Test-Path $path)) { $failed+=$scenario;Write-Output "$scenario : MISSING REPORT, exit $($process.ExitCode)";continue }
    $report=Get-Content -Raw $path | ConvertFrom-Json
    Write-Output "$scenario : $($report.result), $($report.assertions) assertions, exit $($process.ExitCode)"
    if ($process.ExitCode -ne 0 -or $report.result -ne 'PASS') { $failed+=$scenario;$report.errors | Write-Output }
}
if ($failed.Count -gt 0) { throw ('Failed scenarios: '+($failed -join ', ')) }
