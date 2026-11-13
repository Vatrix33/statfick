# Windows: builds Statventure: compiles the engine shaders, then the solution, then lays out the game
# folder ($env:STATVENTURE_GAME): the game's program (Game/Statventure.Game, since GM1 step 2) with the committed
# packs in packs\ beside it (Docs/Assets.md, step 9; S9-A).
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
    # The game folder: the game as the build just made it (framework-dependent), then packs\ made the
    # committed packs exactly: a pack no longer committed goes, a changed one is copied beside its place and
    # renamed over it, so a game running from the folder keeps reading the old one until it reloads.
    dotnet publish Game/Statventure.Game -c $Config --no-build -o $env:STATVENTURE_GAME -v quiet -nologo
    if ($LASTEXITCODE) { throw "the game didn't publish" }
    # The sandbox stood in for the game there until GM1 step 2: its program files and its own Data\ (the tests'
    # fonts, the proof cutscene and its models) go; nothing else of the folder is touched.
    foreach ($name in "Statventure.Sandbox.exe", "Statventure.Sandbox", "Statventure.Sandbox.dll", "Statventure.Sandbox.pdb",
                      "Statventure.Sandbox.deps.json", "Statventure.Sandbox.runtimeconfig.json") {
        $leftover = Join-Path $env:STATVENTURE_GAME $name
        if (Test-Path -LiteralPath $leftover -PathType Leaf) { Remove-Item -LiteralPath $leftover }
    }
    $sandboxData = Join-Path $env:STATVENTURE_GAME "Data"
    foreach ($name in "Fonts", "Cutscenes", "Models") {
        $leftover = Join-Path $sandboxData $name
        if (Test-Path -LiteralPath $leftover -PathType Container) { Remove-Item -LiteralPath $leftover -Recurse }
    }
    if ((Test-Path -LiteralPath $sandboxData -PathType Container) -and -not (Get-ChildItem -LiteralPath $sandboxData -Force)) {
        Remove-Item -LiteralPath $sandboxData
    }
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
