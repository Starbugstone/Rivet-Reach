param([Parameter(Mandatory=$true)][string]$Executable,[string]$OutputDirectory)
$ErrorActionPreference='Stop'
$project=Split-Path $PSScriptRoot -Parent
if (!$OutputDirectory) { $OutputDirectory=Join-Path $project ('Logs\SaveVerification-'+[DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss')) }
if (Test-Path $OutputDirectory) { throw 'Use a fresh verification directory; existing saves are preserved.' }
New-Item -ItemType Directory -Force $OutputDirectory | Out-Null
$saveDirectory=Join-Path $OutputDirectory 'Saves'
foreach ($scenario in @('save','save-resume')) {
    $result=Join-Path $OutputDirectory $scenario
    New-Item -ItemType Directory -Force $result | Out-Null
    $arguments=@('-screen-fullscreen','0','-screen-width','1280','-screen-height','720','-rr-verify',('-rr-'+$scenario+'-review'),'-rr-save-directory',('"'+$saveDirectory+'"'),'-rr-output',('"'+$result+'"'),'-logFile',('"'+(Join-Path $result 'player.log')+'"'))
    $start=New-Object System.Diagnostics.ProcessStartInfo
    $start.FileName=$Executable
    $start.Arguments=$arguments -join ' '
    $start.UseShellExecute=$false
    $process=[System.Diagnostics.Process]::Start($start)
    if (!$process.WaitForExit(900000)) { throw "$scenario exceeded fifteen minutes; player retained for diagnosis." }
    $report=Join-Path $result 'runtime-report.json'
    if (!(Test-Path $report)) { throw "Missing $scenario report; inspect player.log." }
    $data=Get-Content -Raw $report | ConvertFrom-Json
    Write-Output "$scenario : $($data.result), $($data.assertions) assertions, exit $($process.ExitCode)"
    Set-Content (Join-Path $result 'exit-code.txt') $process.ExitCode
    if ($process.ExitCode -ne 0 -or $data.result -ne 'PASS') { $data.errors | Write-Output;throw "$scenario verification failed." }
}
