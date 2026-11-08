# Windows: makes the packages players download (M12, Docs/Packaging.md, "How it works", step 3) into release\ at the
# repo's root, as Tools/Scripts/publish.sh does on Linux; the same Tools\Packaging\Publish.cs does the work:
#   Tools\Scripts\publish.ps1 [-Version <v>] [<package> ...]
# The packages are stat-fick-linux, stat-fick-windows, statventure-editor-linux and statventure-editor-windows (all four
# unless named); the version a release's, 2026.11.8.1 (Q1), or "dev" without -Version. The Windows packages need the
# Visual C++ runtime's DLLs (Q9), taken once from Microsoft's redistributable (Tools\Packaging\vc-runtime.txt pins it)
# into the build cache (%LOCALAPPDATA%\stat-fick-build\vc-runtime\<version>\), with the redistributable's own /layout
# (it installs nothing) and Windows' expand.exe. The packages are checked on Linux (check-packages.sh, Q15, Q28): it
# doesn't run here. Nothing here reads or writes the player's per-user folders.
param(
    [string]$Version = "dev",
    [Parameter(ValueFromRemainingArguments = $true)][string[]]$Packages
)
$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "env.ps1")
if (-not $Packages) { $Packages = @("stat-fick-linux", "stat-fick-windows", "statventure-editor-linux", "statventure-editor-windows") }

function Get-Sha256([string]$Path) {
    (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()
}

# The Visual C++ runtime's DLLs in the build cache (Q9), taken from the pinned redistributable when missing; returns
# their folder.
function Get-VcRuntime {
    $runtime = $null; $url = $null; $sha = $null; $files = @()
    foreach ($line in Get-Content -LiteralPath (Join-Path $env:STATVENTURE_SOURCE "Tools\Packaging\vc-runtime.txt")) {
        if (-not $line -or $line.StartsWith("#")) { continue }
        $parts = $line -split '\s+'
        switch ($parts[0]) {
            "version" { $runtime = $parts[1] }
            "url" { $url = $parts[1] }
            "sha256" { $sha = $parts[1] }
            "file" { $files += , @($parts[1], $parts[2], $parts[3]) }
        }
    }
    $folder = Join-Path $env:STATVENTURE_CACHE "vc-runtime\$runtime"
    $ready = $true
    foreach ($file in $files) {
        $path = Join-Path $folder $file[0]
        if (-not (Test-Path -LiteralPath $path -PathType Leaf) -or (Get-Sha256 $path) -ne $file[2]) { $ready = $false }
    }
    if ($ready) { return $folder }
    Write-Host "publish: the Visual C++ runtime $runtime from $url"
    $work = Join-Path $env:STATVENTURE_CACHE "vc-runtime.$PID"
    if (Test-Path -LiteralPath $work) { Remove-Item -LiteralPath $work -Recurse -Force }
    New-Item -ItemType Directory -Force -Path $work | Out-Null
    try {
        $installer = Join-Path $work "vc_redist.x64.exe"
        Invoke-WebRequest -Uri $url -OutFile $installer -UseBasicParsing
        if ((Get-Sha256 $installer) -ne $sha) { throw "the redistributable downloaded isn't the one vc-runtime.txt pins" }
        # Its payloads, the runtime's cabinets among them, written out by the bundle itself; then each DLL taken out of
        # whichever cabinet holds it (named <dll>_amd64), only if its SHA-256 is the pinned one.
        $layout = Join-Path $work "layout"
        $process = Start-Process -FilePath $installer -ArgumentList @("/layout", "`"$layout`"", "/quiet", "/norestart") -Wait -PassThru
        if ($process.ExitCode) { throw "the redistributable's /layout failed ($($process.ExitCode))" }
        $extracted = Join-Path $work "files"
        New-Item -ItemType Directory -Force -Path $extracted | Out-Null
        foreach ($cabinet in Get-ChildItem -LiteralPath $layout -Recurse -Filter *.cab) {
            & expand.exe $cabinet.FullName "-F:*_amd64" $extracted | Out-Null
        }
        New-Item -ItemType Directory -Force -Path $folder | Out-Null
        foreach ($file in $files) {
            $source = Join-Path $extracted $file[1]
            if (-not (Test-Path -LiteralPath $source -PathType Leaf) -or (Get-Sha256 $source) -ne $file[2]) {
                throw "the redistributable holds no $($file[1]) with the SHA-256 vc-runtime.txt pins"
            }
            Copy-Item -LiteralPath $source -Destination (Join-Path $folder $file[0]) -Force
        }
    }
    finally {
        Remove-Item -LiteralPath $work -Recurse -Force -ErrorAction SilentlyContinue
    }
    return $folder
}

Push-Location $env:STATVENTURE_SOURCE
try {
    # The shaders the engine embeds, as build.ps1 compiles them (a clean tree has none).
    dotnet build Tools/ShaderCompiler -c Release -v quiet -nologo | Out-Null
    if ($LASTEXITCODE) { throw "the shader compiler didn't build" }
    dotnet Tools/ShaderCompiler/bin/Release/net10.0/ShaderCompiler.dll Engine/Statventure/Shaders/Source Engine/Statventure/Shaders/Bin
    if ($LASTEXITCODE) { throw "the shaders didn't compile" }
    # The archives are dated at the commit's time, so one commit gives the same archives.
    $time = git log -1 --format=%ct
    if ($LASTEXITCODE) { throw "git couldn't give the commit's time" }
    $arguments = @("--engine", $env:STATVENTURE_SOURCE, "--version", $Version, "--time", $time)
    if ($Packages | Where-Object { $_ -like "*-windows" }) { $arguments += @("--vc-runtime", (Get-VcRuntime)) }
    dotnet run Tools/Packaging/Publish.cs '--' @arguments @Packages
    if ($LASTEXITCODE) { exit $LASTEXITCODE }
}
finally {
    Pop-Location
}
