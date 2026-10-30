# Windows: builds Statventure: compiles the engine shaders, then the solution.
#   Tools\Scripts\build.ps1 [-Config Debug|Release]
param([string]$Config = "Debug")
$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "env.ps1")
Push-Location $env:STATVENTURE_SOURCE
try {
    dotnet build Tools/ShaderCompiler -c Release -v quiet -nologo | Out-Null
    if ($LASTEXITCODE) { throw "the shader compiler didn't build" }
    dotnet Tools/ShaderCompiler/bin/Release/net10.0/ShaderCompiler.dll Engine/Statventure/Shaders/Source Engine/Statventure/Shaders/Bin
    if ($LASTEXITCODE) { throw "the shaders didn't compile" }
    dotnet build Statventure.slnx -c $Config -v quiet -nologo
    if ($LASTEXITCODE) { throw "the build failed" }
}
finally {
    Pop-Location
}
