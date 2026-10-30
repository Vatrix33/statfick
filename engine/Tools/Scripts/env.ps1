# Windows: dot-source this before building (. Tools\Scripts\env.ps1). Paths come from where the repo
# is; the .NET SDK (if setup.ps1 installed one) and the caches live in %LOCALAPPDATA%\stat-fick-build
# (or $env:STATVENTURE_CACHE). A git-ignored env.local.ps1 next to this file can override anything.
$env:STATVENTURE_SCRIPTS = $PSScriptRoot
$env:STATVENTURE_SOURCE = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$env:STATVENTURE_REPO = (Resolve-Path (Join-Path $env:STATVENTURE_SOURCE "..")).Path
if (-not $env:STATVENTURE_OLD_GAME) { $env:STATVENTURE_OLD_GAME = Join-Path $env:STATVENTURE_REPO "old-game" }
if (-not $env:STATVENTURE_CACHE) { $env:STATVENTURE_CACHE = Join-Path $env:LOCALAPPDATA "stat-fick-build" }
if (-not $env:STATVENTURE_GAME) { $env:STATVENTURE_GAME = Join-Path $env:STATVENTURE_REPO "runtime" }
$dotnet = Join-Path $env:STATVENTURE_CACHE "dotnet"
if (Test-Path (Join-Path $dotnet "dotnet.exe")) {
    $env:DOTNET_ROOT = $dotnet
    $env:PATH = "$dotnet;$env:PATH"
}
if (-not $env:NUGET_PACKAGES) { $env:NUGET_PACKAGES = Join-Path $env:STATVENTURE_CACHE "nuget" }
$env:DOTNET_CLI_TELEMETRY_OPTOUT = "1"
$env:DOTNET_NOLOGO = "1"
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = "1"
$local = Join-Path $PSScriptRoot "env.local.ps1"
if (Test-Path $local) { . $local }
