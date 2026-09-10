param([switch]$Build,[switch]$Baseline,[switch]$SunTicks,[string]$OutputDirectory,[int]$TimeoutSeconds=900)
$ErrorActionPreference='Stop'
$project=Split-Path $PSScriptRoot -Parent
if($Build) {
    $request=Join-Path $project 'Logs\performance-request.txt'
    $result=Join-Path $project 'Logs\Shadows\build-result.txt'
    if(Test-Path $request){throw 'Another performance request is pending.'}
    if(Test-Path $result){Remove-Item $result}
    Set-Content -Encoding ascii ($request+'.pending') 'shadow-build'
    Move-Item -Force ($request+'.pending') $request
    $until=[DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
    while(!(Test-Path $result) -and [DateTime]::UtcNow -lt $until){Start-Sleep -Seconds 2}
    if(!(Test-Path $result) -or (Get-Content $result -Raw).Trim() -ne 'PASS'){throw 'Shadow build failed; see Logs/Shadows and the pinned Editor.'}
}
$executable=Join-Path $project 'Builds\Shadows\RivetReach.exe'
if(!$OutputDirectory){$OutputDirectory=Join-Path $project 'Logs\Shadows\Player'}
New-Item -ItemType Directory -Force $OutputDirectory | Out-Null
$reviewFlag=if($SunTicks){'-rr-sun-shadow-review'}else{'-rr-shadow-review'}
$reportName=if($SunTicks){'sun-shadow-report.json'}else{'shadow-report.json'}
$arguments=@('-screen-fullscreen','0','-screen-width','1280','-screen-height','720','-rr-verify',$reviewFlag,'-rr-output',('"'+$OutputDirectory+'"'),'-logFile',('"'+(Join-Path $OutputDirectory 'player.log')+'"'))
$process=Start-Process -FilePath $executable -ArgumentList $arguments -PassThru
if(!$process.WaitForExit($TimeoutSeconds*1000)){throw 'Shadow check timed out; the fixture player remains available for diagnosis.'}
$runtime=Get-Content (Join-Path $OutputDirectory 'runtime-report.json') -Raw | ConvertFrom-Json
$shadow=Get-Content (Join-Path $OutputDirectory $reportName) -Raw | ConvertFrom-Json
if($process.ExitCode -ne 0 -or $runtime.result -ne 'PASS' -or ($shadow.result -ne 'PASS' -and !($Baseline -and $shadow.result -eq 'BASELINE'))){throw ($runtime.errors -join "`n")}
if($SunTicks){Write-Output "$($shadow.result): sun ticks; held channel delta $($shadow.samples[1].heldMeanDelta). $OutputDirectory"}
else{Write-Output "$($shadow.result): $($shadow.aoMethod); frozen channel delta $($shadow.samples[0].meanChannelDelta). $OutputDirectory"}
