param(
    [string]$UnityEditor = 'D:\Unity\Hub\6000.4.4f1\Editor\Unity.exe',
    [int]$TimeoutSeconds = 600
)
$ErrorActionPreference = 'Stop'
$project = Split-Path $PSScriptRoot -Parent
$logs = Join-Path $project 'Logs'
New-Item -ItemType Directory -Force $logs | Out-Null
$result = Join-Path $logs 'build-result.txt'
if (Test-Path $result) { Remove-Item $result }
$running = Get-CimInstance Win32_Process -Filter "Name='Unity.exe'" | Where-Object { $_.CommandLine -and $_.CommandLine.Replace('/','\').Contains($project) }
if ($running) {
    Set-Content -Path (Join-Path $logs 'build-request.txt') -Value 'build' -NoNewline
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while (!(Test-Path $result) -and (Get-Date) -lt $deadline) { Start-Sleep -Seconds 2 }
    if (!(Test-Path $result)) { throw 'The open Editor did not finish the build. Inspect its Console; finish any current dialog or Play session. Unsaved scenes were preserved.' }
    $status = Get-Content $result -Raw
    if (!$status.StartsWith('SUCCESS')) { throw $status }
} else {
    if (!(Test-Path $UnityEditor)) { throw 'Supply -UnityEditor with the pinned 6000.4.4f1 Editor executable.' }
    $process = Start-Process -FilePath $UnityEditor -ArgumentList @('-batchmode','-quit','-projectPath',('"'+$project+'"'),'-executeMethod','RivetReach.Editor.ProjectBuild.PrepareAndBuild','-logFile',('"'+(Join-Path $logs 'build-batch.log')+'"')) -PassThru -Wait
    if ($process.ExitCode -ne 0) { throw "Unity build exited $($process.ExitCode). See Logs/build-batch.log." }
}
Get-Content (Join-Path $logs 'build-summary.txt')
Write-Output "Player: $(Join-Path $project 'Builds\FirstPOC\RivetReach.exe')"
