# Windows: prepares a machine to build, run and test the engine (run once, no admin needed):
# installs the .NET 10 SDK into %LOCALAPPDATA%\stat-fick-build\dotnet unless one is installed, and
# checks for a Vulkan driver (it comes with the GPU driver).
#   powershell -ExecutionPolicy Bypass -File Tools\Scripts\setup.ps1
$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "env.ps1")
$haveSdk = $false
if (Get-Command dotnet -ErrorAction SilentlyContinue) {
    $haveSdk = [bool]((dotnet --list-sdks) -match '^10\.')
}
if (-not $haveSdk) {
    New-Item -ItemType Directory -Force -Path $env:STATVENTURE_CACHE | Out-Null
    $installer = Join-Path $env:STATVENTURE_CACHE "dotnet-install.ps1"
    Invoke-WebRequest -Uri "https://dot.net/v1/dotnet-install.ps1" -OutFile $installer
    & $installer -Channel 10.0 -InstallDir (Join-Path $env:STATVENTURE_CACHE "dotnet")
    . (Join-Path $PSScriptRoot "env.ps1")
}
dotnet --version
if (-not (Test-Path (Join-Path $env:WINDIR "System32\vulkan-1.dll"))) {
    Write-Warning "No Vulkan runtime (System32\vulkan-1.dll): install or update the GPU driver."
}
Write-Host "Ready: Tools\Scripts\test.ps1 builds and runs the tests."
