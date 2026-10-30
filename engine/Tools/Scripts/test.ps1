# Windows: builds and runs the engine tests (headless GPU tests included). Extra arguments go to
# dotnet test, e.g. Tools\Scripts\test.ps1 --filter "FullyQualifiedName~Canvas".
$ErrorActionPreference = "Stop"
& (Join-Path $PSScriptRoot "build.ps1") -Config Debug
. (Join-Path $PSScriptRoot "env.ps1")
Push-Location $env:STATVENTURE_SOURCE
try {
    dotnet test Tests/Statventure.Tests --no-build -v quiet -nologo @args
    if ($LASTEXITCODE) { exit $LASTEXITCODE }
}
finally {
    Pop-Location
}
