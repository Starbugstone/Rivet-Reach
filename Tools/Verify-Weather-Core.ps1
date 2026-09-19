param([string]$EditorDirectory='D:\Unity\Hub\6000.4.4f1\Editor')
$ErrorActionPreference='Stop'
$project=Split-Path $PSScriptRoot -Parent
Push-Location $project
try {
  $mono=Join-Path $EditorDirectory 'Data\MonoBleedingEdge\bin\mono.exe'
  $compiler=Join-Path $EditorDirectory 'Data\MonoBleedingEdge\lib\mono\4.5\csc.exe'
  New-Item -ItemType Directory -Force 'Logs\Weather' | Out-Null
  & $mono $compiler /nologo /optimize+ /langversion:latest /target:exe /out:Logs/Weather/VerifyWeatherCore.exe Assets/RivetReach/Code/World/WeatherState.cs Tools/Verify-Weather-Core.cs
  if($LASTEXITCODE -ne 0){throw 'Weather core compilation failed.'}
  & $mono Logs/Weather/VerifyWeatherCore.exe
  if($LASTEXITCODE -ne 0){throw 'Weather core verification failed.'}
} finally {Pop-Location}
