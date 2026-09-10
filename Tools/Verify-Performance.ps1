param(
    [switch]$Build,
    [ValidateSet('Gameplay','Fluids','Multiblocks','Crafting','Movement')][string]$Scenario='Gameplay',
    [string]$Executable,
    [string]$OutputDirectory,
    [int]$TimeoutSeconds=900
)
$ErrorActionPreference='Stop'
$project=Split-Path $PSScriptRoot -Parent
if($Build) {
    $request=Join-Path $project 'Logs\performance-request.txt'
    $result=Join-Path $project 'Logs\Performance\build-result.txt'
    if(Test-Path $request){throw 'A performance request is already pending.'}
    if(Test-Path $result){Remove-Item $result}
    Set-Content -Encoding ascii $request 'build'
    $deadline=[DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
    while(!(Test-Path $result) -and [DateTime]::UtcNow -lt $deadline){Start-Sleep -Seconds 2}
    if(!(Test-Path $result) -or (Get-Content $result -Raw).Trim() -ne 'PASS'){throw 'Build did not complete. Inspect the open pinned Editor and Logs/Performance.'}
}
if(!$Executable){$Executable=Join-Path $project 'Builds\Performance\RivetReach.exe'}
if(!$OutputDirectory){$OutputDirectory=Join-Path $project ('Logs\Performance\'+$Scenario)}
New-Item -ItemType Directory -Force $OutputDirectory | Out-Null
if(!(Test-Path $Executable)){throw 'Build the performance player in the pinned Editor first.'}
$arguments=@('-screen-fullscreen','0','-screen-width','1280','-screen-height','720','-rr-verify','-rr-output',('"'+$OutputDirectory+'"'),'-logFile',('"'+(Join-Path $OutputDirectory 'player.log')+'"'))
if($Scenario -eq 'Fluids'){$arguments+='-rr-fluid-review'}
if($Scenario -eq 'Multiblocks'){$arguments+='-rr-multiblock-review'}
if($Scenario -eq 'Movement'){$arguments+='-rr-placement-items-review'}
if($Scenario -eq 'Crafting'){$arguments+='-rr-crafting-review'}
$process=Start-Process -FilePath $Executable -ArgumentList $arguments -PassThru
if(!$process.WaitForExit($TimeoutSeconds*1000)){throw 'Verification timed out; the player was left available for diagnosis.'}
$report=Get-Content (Join-Path $OutputDirectory 'runtime-report.json') -Raw | ConvertFrom-Json
if($process.ExitCode -ne 0 -or $report.result -ne 'PASS'){throw ($report.errors -join "`n")}
Write-Output "$Scenario PASS: $($report.assertions) assertions. $OutputDirectory"
