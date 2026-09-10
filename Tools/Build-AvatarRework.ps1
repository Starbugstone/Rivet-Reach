param([int]$TimeoutSeconds=600)
$ErrorActionPreference='Stop'
$project=Split-Path $PSScriptRoot -Parent
$result=Join-Path $project 'Logs\AvatarRework\build-result.txt'
New-Item -ItemType Directory -Force (Split-Path $result) | Out-Null
if (Test-Path $result) { Remove-Item $result }
$running=Get-CimInstance Win32_Process -Filter "Name='Unity.exe'" | Where-Object { $_.CommandLine -and $_.CommandLine.Replace('/','\').Contains($project) }
if ($running) {
    Set-Content (Join-Path $project 'Logs\avatar-build-request.txt') 'build' -NoNewline
    $deadline=(Get-Date).AddSeconds($TimeoutSeconds)
    while (!(Test-Path $result) -and (Get-Date) -lt $deadline) { Start-Sleep -Seconds 2 }
    if (!(Test-Path $result)) { throw 'Avatar build timed out; inspect the open Editor. Its scene was preserved.' }
} else {
    $process=Start-Process 'D:\Unity\Hub\6000.4.4f1\Editor\Unity.exe' -ArgumentList @('-batchmode','-quit','-projectPath',('"'+$project+'"'),'-executeMethod','RivetReach.Editor.AvatarReworkBuild.Build','-logFile',('"'+(Join-Path $project 'Logs\AvatarRework\build.log')+'"')) -PassThru -Wait
    if ($process.ExitCode -ne 0) { throw "Unity exited $($process.ExitCode)" }
}
$status=Get-Content $result -Raw
if (!$status.StartsWith('PASS')) { throw $status }
Get-Content (Join-Path $project 'Logs\AvatarRework\build-summary.txt')
