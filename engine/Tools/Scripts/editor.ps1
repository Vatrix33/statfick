# Windows: runs the Statventure Editor (M11, Docs/Editor.md) from the repo, in the dev setup (P5), as
# Tools/Scripts/editor.sh does on Linux: the game's packs are engine\Assets\Packs, which it can save into, and the
# game's record kinds those of the data library built beside it (Samples/Statventure.Game.Data, M12 step 6). A Release
# build of both, as the other run scripts. Arguments go to the editor (--player-folder <folder>: the player's game
# folder it uses, by default this machine's; a check runs it with a scratch folder).
#   Tools\Scripts\editor.ps1 [--player-folder <folder>] [--capture <file.png> [--frames <n>]]
$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "env.ps1")
Push-Location $env:STATVENTURE_SOURCE
try {
    foreach ($project in "Tools/Editor", "Samples/Statventure.Game.Data") {
        $output = dotnet build $project -c Release -v quiet -nologo
        if ($LASTEXITCODE) {
            [Console]::Error.WriteLine(($output | Out-String))
            exit 1
        }
    }
    dotnet Tools/Editor/bin/Release/net10.0/Statventure.Editor.dll --repo $env:STATVENTURE_SOURCE @args
    exit $LASTEXITCODE
}
finally {
    Pop-Location
}
