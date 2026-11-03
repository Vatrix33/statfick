# Windows: builds Statventure: compiles the engine shaders, then the solution, then lays out the game
# folder ($env:STATVENTURE_GAME): the sandbox, the game's stand-in until the port, with the committed packs
# in packs\ beside it (Docs/Assets.md, step 9; S9-A).
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
    # The game folder: the sandbox as the build just made it (framework-dependent), then packs\ made the
    # committed packs exactly: a pack no longer committed goes, a changed one is copied beside its place and
    # renamed over it, so a game running from the folder keeps reading the old one until it reloads.
    dotnet publish Samples/Statventure.Sandbox -c $Config --no-build -o $env:STATVENTURE_GAME -v quiet -nologo
    if ($LASTEXITCODE) { throw "the sandbox didn't publish" }
    $committed = Join-Path $env:STATVENTURE_SOURCE "Assets\Packs"
    $packs = Join-Path $env:STATVENTURE_GAME "packs"
    New-Item -ItemType Directory -Force -Path $packs | Out-Null
    # (-Filter *.pak also matches longer extensions through 8.3 names: hence the extension test.)
    foreach ($pak in Get-ChildItem -LiteralPath $packs -Filter *.pak -File | Where-Object { $_.Extension -eq ".pak" }) {
        if (-not (Test-Path -LiteralPath (Join-Path $committed $pak.Name) -PathType Leaf)) { Remove-Item -LiteralPath $pak.FullName }
    }
    foreach ($pak in Get-ChildItem -LiteralPath $committed -Filter *.pak -File | Where-Object { $_.Extension -eq ".pak" }) {
        $target = Join-Path $packs $pak.Name
        if ((Test-Path -LiteralPath $target -PathType Leaf) -and (Get-FileHash -LiteralPath $target).Hash -eq (Get-FileHash -LiteralPath $pak.FullName).Hash) { continue }
        $temporary = "$target.$PID.tmp"
        Copy-Item -LiteralPath $pak.FullName -Destination $temporary -Force
        # Windows PowerShell's .NET Framework has no File.Move that overwrites: Replace puts it in place in one call.
        if (Test-Path -LiteralPath $target -PathType Leaf) { [System.IO.File]::Replace($temporary, $target, $null) }
        else { [System.IO.File]::Move($temporary, $target) }
    }
}
finally {
    Pop-Location
}
