param([string]$EditorDirectory = 'D:\Unity\Hub\6000.4.4f1\Editor')
$ErrorActionPreference = 'Stop'
$project = Split-Path $PSScriptRoot -Parent
Push-Location $project
try {
    $mono = Join-Path $EditorDirectory 'Data\MonoBleedingEdge\bin\mono.exe'
    $compiler = Join-Path $EditorDirectory 'Data\MonoBleedingEdge\lib\mono\4.5\csc.exe'
    $engine = Join-Path $EditorDirectory 'Data\Managed\UnityEngine'
    $facade = Join-Path $EditorDirectory 'Data\MonoBleedingEdge\lib\mono\4.5\Facades\netstandard.dll'
    New-Item -ItemType Directory -Force 'Logs\TerrainChecks' | Out-Null
    $options = @('/nologo','/optimize+','/langversion:latest','/target:exe','/out:Logs/TerrainChecks/Generator.exe',('/reference:"'+$engine+'\UnityEngine.CoreModule.dll"'),('/reference:"'+$facade+'"'))
    $sources = @('Fluids/FluidDefinition','Fluids/FluidMesher','World/TerrainGenerator','World/TerrainProfile','World/WorldNoise','World/CaveGenerator','World/OreGenerator','World/ChunkMesher','World/TerrainReviewSites','Core/Coordinates','Core/ItemRegistry','Core/ItemStack','Core/ItemContainer','Survival/HungerState') | ForEach-Object { 'Assets/RivetReach/Code/'+$_+'.cs' }
    $options + $sources + @('Assets/RivetReach/Editor/TerrainGenerationChecks.cs','Tools/TerrainGenerationHarness.cs') | Set-Content -Encoding ascii 'Logs\TerrainChecks\compile.rsp'
    & $mono $compiler '@Logs/TerrainChecks/compile.rsp'
    if ($LASTEXITCODE -ne 0) { throw 'Terrain source compilation failed.' }
    $previousMonoPath = $env:MONO_PATH
    try {
        $env:MONO_PATH = $engine
        & $mono 'Logs/TerrainChecks/Generator.exe'
        if ($LASTEXITCODE -ne 0) { throw 'Terrain generation check failed.' }
    } finally { $env:MONO_PATH = $previousMonoPath }
} finally { Pop-Location }
