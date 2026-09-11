param([Parameter(Mandatory=$true)][string]$Executable,[Parameter(Mandatory=$true)][string]$OutputDirectory,[switch]$PocOnly)
$ErrorActionPreference='Stop'
if (Test-Path $OutputDirectory) { throw 'Use a fresh evidence directory; existing reports are preserved.' }
New-Item -ItemType Directory -Force $OutputDirectory | Out-Null
$scenarios=if ($PocOnly) { @('poc') } else { @('creative','industry','multiblock','placement-items','browser','survival','workshop-followup') }
foreach ($scenario in $scenarios) {
    $result=Join-Path $OutputDirectory $scenario
    New-Item -ItemType Directory -Force $result | Out-Null
    $arguments=@('-screen-fullscreen','0','-screen-width','1280','-screen-height','720','-rr-verify','-rr-output',('"'+$result+'"'),'-logFile',('"'+(Join-Path $result 'player.log')+'"'))
    if ($scenario -ne 'poc') { $arguments+=('-rr-'+$scenario+'-review') }
    if ($scenario -in @('industry','multiblock','workshop-followup')) { $arguments+='-rr-creative-workshop' }
    $start=New-Object System.Diagnostics.ProcessStartInfo
    $start.FileName=$Executable;$start.Arguments=$arguments -join ' ';$start.UseShellExecute=$false
    $process=[System.Diagnostics.Process]::Start($start)
    if (!$process.WaitForExit(900000)) { throw "$scenario exceeded fifteen minutes; player retained for diagnosis." }
    $report=Join-Path $result 'runtime-report.json'
    if (!(Test-Path $report)) { throw "Missing $scenario report; inspect player.log." }
    $data=Get-Content -Raw $report | ConvertFrom-Json
    Write-Output "$scenario : $($data.result), $($data.assertions) assertions, exit $($process.ExitCode)"
    Set-Content (Join-Path $result 'exit-code.txt') $process.ExitCode
    if ($process.ExitCode -ne 0 -or $data.result -ne 'PASS') { $data.errors | Write-Output;throw "$scenario verification failed." }
}
