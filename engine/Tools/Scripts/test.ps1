# Windows: builds and runs the tests (headless GPU tests included): the engine's and the editor's
# (Tests/Statventure.Tests), then the game's (Tests/Statventure.Game.Tests, since GM1 step 2: GM1-P4), one after the
# other; it fails if either does. Extra arguments go to both dotnet test runs, e.g.
# Tools\Scripts\test.ps1 --filter "FullyQualifiedName~Canvas".
$ErrorActionPreference = "Stop"
& (Join-Path $PSScriptRoot "build.ps1") -Config Debug
. (Join-Path $PSScriptRoot "env.ps1")
Push-Location $env:STATVENTURE_SOURCE
try {
    $status = 0
    foreach ($project in "Tests/Statventure.Tests", "Tests/Statventure.Game.Tests") {
        dotnet test $project --no-build -v quiet -nologo @args
        if ($LASTEXITCODE -and -not $status) { $status = $LASTEXITCODE }
    }
    if ($status) { exit $status }
}
finally {
    Pop-Location
}
