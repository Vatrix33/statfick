# Packaging (M12)

The game and the editor as players get them: two downloads per platform (Linux and Windows), each a
self-contained program in an archive; per-run logs and crash files in the player's per-user folder and a crash
window drawn by the engine; date-based versions; draft releases on the private GitHub repo that the user
publishes. M12 builds and proves all of it with the sandbox standing in for the game (M12-12); the port replaces
the stand-in.

## Decisions (user, 2026-11-08)

Asked with the question tool before this doc was written, on the user's go ("Then start with M12"); in full,
with the options not chosen, in `Docs/Engine-Plan.md`, "Decisions for M12". Every answer was the recommended
option except the shipped folders (M12-1), icons (M12-5) and versioning (M12-6). **The rule to ask stands in
full again:** every design choice of M12 goes to the user, the editor's packaging included (M11's exception was
for M11's editor only). The details these answers leave open are under "Questions for the user" below, not
asked yet.

- **Two separate downloads (M12-1, not the recommendation):** the game and the editor are packaged apart; a
  modder downloads both, and the editor must be told where the game is.
- **Executable names (M12-2):** the game `stat-fick` on Linux and `Stat Fick.exe` on Windows (the old game's
  names); the editor `statventure-editor` and `Statventure Editor.exe`.
- **The runtime (M12-3):** self-contained .NET per platform, not trimmed (the record system finds its kinds by
  reflection).
- **The package form (M12-4):** an archive per platform, `.tar.gz` for Linux and `.zip` for Windows; no
  installer, no AppImage (either can come later as a step of its own).
- **Icons (M12-5, not the recommendation): none in M12.** New icons for both programs come with the port's art
  pass; until then the executables show the system's generic icon.
- **Versions (M12-6, not the recommendation): date-based** (in the style of 2026.11.8); the exact form is
  Q1–Q3 below.
- **Updates (M12-7):** an in-game update check later, at port time (once the game is public somewhere, a check
  against that place); meanwhile no check: a release script builds both platforms' archives and uploads them
  as a draft GitHub Release with the version's tag on the private repo, which the user publishes.
- **Logs (M12-8):** `logs/` in the player's per-user folder (`~/.local/share/stat-fick/logs`,
  `%APPDATA%\Stat Fick\logs`), one file per run, `logs/<program>-<date-time>.log`, the last 10 kept; on a crash
  the exception and the log's end also go to `logs/crash-<date-time>.txt`; the editor logs the same way.
- **The data library (M12-9; closes G3):** the port puts its record kinds in a library of their own,
  `Statventure.Game.Data`, which the game links and the editor loads from the game's folder; one file in each
  download.
- **On a crash (M12-10):** a plain window drawn by the engine (no OS dialog, the game's rule) naming what went
  wrong and the crash file's path, shown for copying; then the program ends.
- **No portable mode (M12-11):** the player's folder is always the per-user one.
- **The first packages (M12-12):** the sandbox as the game's stand-in, plus the editor; the whole pipeline
  (publish, archives, versions, logs, crash handling, releases) is built and proven now; the port later
  replaces the stand-in.

Decided earlier: Linux and Windows only (2026-10-16); no OS dialogs (2026-10-17); the per-user game folder
(M9: `settings.json`, `saves/`, `mods/`; M11's G6: the editor's `editor/`); `packs/` and `classes/` beside the
game's executable and mods in the per-user `mods/` (M9, Q2, Q8); the editor shipped for modding (M11-4) and
called "Statventure Editor" (M11-19); everything on GitHub stays private (2026-10-30); "Give the shipped game a
proper game-like folder structure" (the user's working rule, `Docs/Notes.md`).

## What exists (checked against the code, 2026-11-08)

Nothing of M12 yet: no packaging script, no program version, no log file written by any program, no crash
handling beyond the log line.

### Building and publishing today

- **`Tools/Scripts/build.sh` and `build.ps1`** compile the shaders, build the solution (Debug unless told), then
  `dotnet publish Samples/Statventure.Sandbox -c <config> --no-build -o $STATVENTURE_GAME`: framework-dependent,
  no runtime id, so every runtime's native libraries come along (S9-A); then `packs/` is made to hold exactly
  `Assets/Packs/*.pak`. `$STATVENTURE_GAME` is `<repo>/runtime` (git-ignored), on the PC
  `/run/media/system/M.2_2TB/Stat Fick/` (the git-ignored `env.local.sh`; that folder is empty today, the worker
  worktrees each publish into their own `runtime/`). A worktree's `runtime/` (159 MB): the apphost
  `Statventure.Sandbox`, `Statventure.Sandbox.dll` (40 KB) with its `.pdb`, `.deps.json` and
  `.runtimeconfig.json`, `Statventure.dll` (3.1 MB) and `.pdb` (1 MB), 19 `Brutal.*.dll`, 14
  `Microsoft.Extensions.*.dll` (BRUTAL's logging pulls them in), `CommunityToolkit.HighPerformance.dll`,
  `HarfBuzzSharp.dll`, `runtimes/<rid>/native/` for 18 runtime ids (linux-x64 19 MB; win-x64 30 MB, 23 MB of it
  `libHarfBuzzSharp.pdb`), the sandbox's own `Data/` (5.7 MB: the test fonts, the proof cutscene and its
  models, which `Statventure.Sandbox.csproj` copies) and `packs/` (`audio.pak` 58 KB, `ui.pak` 1 MB).
- **Nothing publishes the editor.** `Tools/Scripts/editor.sh` builds `Tools/Editor` in Release and runs
  `dotnet Tools/Editor/bin/Release/net10.0/Statventure.Editor.dll --repo <engine>`; there is no `editor.ps1`
  ("Windows check", item 8d). `play-cutscene.sh` runs `Samples/Statventure.Sandbox/bin/Release/net10.0/Statventure.Sandbox.dll`.
- **No self-contained or runtime-specific publish has been made.** On 2026-11-03 every project then in the
  solution was *built* (`dotnet build -r win-x64`, per project) for Windows from Linux with 0 errors and the
  Windows natives copied (Engine-Plan, Progress, "Small tasks"); the editor and the packer weren't among them
  (the editor came with M11). The NuGet cache holds the Windows apphost pack and the Linux runtime pack, not the
  Windows runtime pack: a self-contained win-x64 publish downloads it at its first restore.
- **Projects:** `Directory.Build.props` (net10.0, nullable, unsafe, deterministic, the BRUTAL and HarfBuzzSharp
  versions; no version, no publish settings); no `Directory.Build.targets`, no `global.json`. The sandbox is an
  `Exe` with the assembly name `Statventure.Sandbox` (its project's), the editor an `Exe` with
  `AssemblyName Statventure.Editor`; the test project references the editor's project.
- **The repo's `.gitignore`** already ignores `/runtime/` and `/release/`.
- **Tools:** on the PC `tar`, `zip`, `unzip`, `7z`, `cage` and `gh` (logged in since 2026-10-30); no PowerShell
  on the PC or in the cloud's setup (`cloud-setup.sh`, which installs no `zip` either), so the `.ps1` scripts run
  only on the Windows PC.

### The executables' names

- The SDK names the apphost after the assembly, `$(AssemblyName)` (with `.exe` on Windows), with no property
  for the executable's own name (`Microsoft.NET.Sdk.targets`, `_CreateAppHost`; `Microsoft.NET.Publish.targets`).
  The apphost carries the name of the program file it starts (`AppBinaryName`, `$(AssemblyName).dll`), so an
  executable renamed after publishing still starts its program. Two ways to M12-2's names: the executable
  renamed when packaging (the program's own files keep their names), or the assembly named per platform
  (`stat-fick`, `Stat Fick`: an assembly name with a space, different on each system, and every script naming
  the `.dll` changed). Left to the steps; the first is proposed.
- What names the current names: `editor.sh` (`Statventure.Editor.dll`), `play-cutscene.sh`
  (`Statventure.Sandbox.dll`), `EditorArguments.Usage` ("usage: Statventure.Editor …"), `EditorView.Name`
  ("Statventure Editor", the window's title; `ShellTests` checks it), the sandbox's title "Statventure Sandbox".
  The engine assumes no program name: its default window title and Vulkan's application name are
  `EngineInfo.Name`, "Statventure".
- **A Windows `.exe` built on Linux carries none of the program's Windows resources** (its version details,
  later an icon): the .NET 10.0.402 SDK says so (NETSDK1074, "The application host executable will not be
  customized because adding resources requires that the build be performed on Windows (excluding Nano
  Server)"). Console or not is the project's `OutputType`: `Exe` (a console window opens beside the program on
  Windows) or `WinExe` (the SDK marks the Windows executable a GUI program); both programs are `Exe` today.
- A name with a space (`Stat Fick.exe`, `Statventure Editor.exe`) needs nothing special in .NET; scripts quote
  it.

### Native libraries

What each program loads, from the engine's `obj/project.assets.json` and the packages in the NuGet cache:

| Library | From | Linux | Windows |
| --- | --- | --- | --- |
| GLFW 3.5 (window, input) | Ahwoo.BRUTAL.Glfw 2026.10.4 | `libglfw.so` (and `.so.3`, `.so.3.5`) | `glfw3.dll` |
| Vulkan loader | not shipped: the system's | `libvulkan.so.1` (`NativeLibraries` maps BRUTAL's `vulkan` to it) | `vulkan-1.dll`, which BRUTAL loads from System32 (it comes with the GPU driver) |
| BRUTAL's Vulkan helpers | Ahwoo.BRUTAL.Vulkan | `libVulkanEx.so` | `VulkanEx.dll` |
| VMA | Ahwoo.BRUTAL.Vulkan.Vma | `libvma.so` | `vma.dll` |
| stb (images) | Ahwoo.BRUTAL.Stb | `libstb.so` | `stb.dll` |
| FreeType, libpng | Ahwoo.BRUTAL.FreeType 2026.10.1 | `libfreetype.so` (`.so.6`, `.so.6.20.6`), `libpng18.so` (`.so.18`, `.so.18.0.git`) | `freetype.dll`, `png18.dll` |
| zlib | Ahwoo.BRUTAL.ZLib | `libzlib.so` (`.so.1`, `.so.1.3.2.1`) | `zlib.dll` |
| OpenAL Soft 1.25.2 (LGPL-2.0) | Ahwoo.BRUTAL.OpenAl | `libopenal.so` (`.so.1`, `.so.1.25.2`) | `OpenAL32.dll` |
| HarfBuzz | HarfBuzzSharp 14.2.1.301 | `libHarfBuzzSharp.so` | `libHarfBuzzSharp.dll` (and a 23 MB `.pdb`) |
| gli, KTX | Ahwoo.BRUTAL.Texture (through Gltf) | `libgli.so`, `libKtx.so` | `gli.dll`, `Ktx.dll` |
| Brotli, the runtime's own | .NET (self-contained: in the package) | `libSystem.*.so` | `System.*.dll` |

- **Linux:** every library carries `RUNPATH $ORIGIN`, so `libfreetype` finds `libpng18.so.18` and `libzlib.so.1`
  beside itself: the names under their sonames are needed. NuGet holds no symbolic links, so each library is
  there up to three times as a full copy (about 8 MB of copies). From the system they need libc, libm,
  libstdc++ and libgcc_s, `libvulkan.so.1` with a Vulkan 1.3 driver, X11 or Wayland (GLFW loads them at run
  time) and the sound server's libraries (OpenAL Soft loads them). The highest glibc version any of them asks
  for is **GLIBC_2.34** (libglfw, libKtx, libopenal, libvma): Ubuntu 22.04, Debian 12, Fedora 35 and later. .NET
  itself loads the system's **OpenSSL** for SHA-256 (the packs' hashes) and the system's **ICU** for its
  globalization (unless it runs in invariant mode).
- **Windows: every BRUTAL library except OpenAL Soft needs the Visual C++ runtime** (`VCRUNTIME140.dll`; VMA,
  gli and KTX also `MSVCP140.dll` and `VCRUNTIME140_1.dll`; checked with `objdump -p`), which isn't part of
  Windows and isn't in any package: a machine without the Visual C++ 2015–2022 redistributable can't load
  GLFW, so the program can't open its window. The UCRT (`api-ms-win-crt-*`) is part of Windows 10 and 11;
  `libHarfBuzzSharp.dll` needs only KERNEL32, OpenAL Soft only Windows' own libraries.
- ShaderC (the shader compiler's) isn't among the engine's packages: the shaders are compiled at build time
  and embedded.
- With a runtime id (`-r linux-x64`, `-r win-x64`) a publish copies only that runtime's natives, beside the
  executable; `NativeLibraries` also looks there (`AppContext.BaseDirectory`).

### Logging and crashes today

- **`Core/Log.cs`:** lines `"{seconds since start} {LEVEL} [{category}] {message}"` to the console (warnings and
  errors to stderr) and, after `Log.OpenFile(path)`, to one file (made anew, UTF-8, every line flushed);
  `CloseFile()`; thread-safe; `Sink` for tests.
- **`ApplicationOptions.LogFile`:** `Run()` opens it first; **no program sets it** (the sandbox and the editor
  log to the console only); `Dispose()` closes it.
- **A crash in `Run()`** (`Core/Application.cs`): `NativeLibraries.Install()`, the log file, then `Storage`,
  `Data` (a damaged pack throws here, before the window), `Window`, `GpuDevice`, `Swapchain`, `AudioSystem`,
  `Load()`, the loop, `Unload()`. An exception anywhere in that is logged as an Error with its stack and
  rethrown; `finally Dispose()` disposes the swapchain, the device, the sound and the window (GLFW is
  terminated with the last window), writes the pending saves (S9-B), closes the data and the log. The
  exception then leaves `Main`: .NET prints "Unhandled exception." and the stack to stderr and aborts (on Linux
  SIGABRT, status 134). Nothing is shown; a Windows GUI program would show nothing at all.
- **Other threads:** the audio thread catches a rendering failure, logs it once and plays silence (M8); the
  storage writer logs a failed write and raises `WriteFailed`; a UI listener's or script task's exception is
  logged and the rest runs, as in browsers (`UiEvents`, `UiEventLoop`, `UiDocument.Scripting`). The audio's
  warm-up and room-preparing threads have no catch, so an exception there ends the process at once (.NET's rule
  for any thread: `AppDomain.UnhandledException` is raised, then the process ends without the main thread's
  `finally`). No handler for `AppDomain.UnhandledException` or `TaskScheduler.UnobservedTaskException` is
  installed anywhere.
- **What a crash window needs:** by the time `Run()` has failed, the window, device and swapchain are gone (and
  the failure may be theirs), so a crash window needs a new window (`Window.Create`: GLFW initialises again,
  since `Window` counts its users), a new `GpuDevice` and `Swapchain`, a `UiDocument` with fonts and a `UiHost`,
  and a small loop of its own: not `Application.Run`, which mounts the data first (a damaged pack would throw
  again). The engine embeds no font (only the shaders and two PNGs; M9, "embedded stays embedded"): every font
  comes from `ui.pak` or a file. Copying text: `Window.Clipboard` and the UI's text selection exist. Opening a
  folder: `GameStorage.OpenInFileManager()` (`xdg-open`, Explorer; not a dialog: the old game's "Open game
  folder").
- **Faults .NET can't catch** (a native library's crash, a stack overflow, `Environment.FailFast`) end the
  process without a handler running; the log file has every line written until then.

### The per-user folder

- `Storage/GameFolder.cs`: `$XDG_DATA_HOME/stat-fick` (when absolute), else `~/.local/share/stat-fick`; Windows
  `%APPDATA%\Stat Fick` (the RoamingAppData known folder); `Locate` is a pure function, `ForThisMachine()` reads
  the machine; made on first use, mode 0700 on Linux. It holds `settings.json`, `saves/`, `mods/` (`GameStorage`)
  and the editor's `editor/` (`shell.json`, `recent-places.json`, `volume.json`, `stand-ins.json`; G6).
  `ApplicationOptions.GameFolder` points it elsewhere; the editor's `--player-folder` sets that; the sandbox has
  no option for it (only `XDG_DATA_HOME`).
- The tests point .NET's per-user variables at a scratch folder of the run (`Infrastructure/PerUserFolders.cs`);
  a child process a test starts inherits them; on Linux the C library keeps the real ones. Whether Windows'
  known-folder lookup follows the variables isn't known ("Windows check", item 7). `mutate.py` compares the real
  game folder and the Documents folder's top level around each mutation.

### Versions today

- None for the programs: no `Version` in any project, so every assembly is 1.0.0.0 (the SDK's default). The
  engine has `Core/EngineInfo.Version`, 0.1.0, given to Vulkan as the engine's version (`GpuDevice`).
- The SDK puts the commit into the informational version by default since .NET 8 (Source Link, part of the
  SDK: `AssemblyInformationalVersion` "<version>+<commit>"); step 1 confirms it in this repo and in worktrees.
- A single place for a version: `Directory.Build.props` (`Version`), overridden from the command line
  (`-p:Version=…`) by a publish.
- Titles: the editor's window title is `EditorView.Title` ("Statventure Editor", " — <pack>" with a pack open,
  "•" for unsaved work); the sandbox's "Statventure Sandbox".

### The editor as a download of its own

- `EditorArguments.Parse`: `--repo <engine folder>` (the dev setup: `Assets/Packs`, writable; P5), `--game <game
  folder>` (`<folder>/packs`, read-only), by default `packs/` beside the program; `--player-folder`.
  `GameData.Problem()`: no `ui.pak` there gives the usage text on stderr and exit code 2, before any window.
- **The editor's own UI takes its fonts from the game's `ui.pak`** (Lato 400 and 700; `Shell/Fonts.css`,
  `EditorApplication.Load`: a file system of `Mount.Pack(_game.UiPack)`). So an editor downloaded apart can't
  draw anything until it knows where the game is.
- It mounts the game's area packs only (`GamePacksOnly`, P6) and saves mods into the per-user `mods/`.
- Record kinds: `RecordTypes.Of(assemblies)` and `Importers.Find(assemblies)` take any assemblies; the editor uses
  the engine's today. Nothing loads an assembly from a path yet.
- Its file browser (M11 step 8) and dialogs exist; a screen asking for the game's folder would reuse them.

### Releases

- The private repo `github.com/Vatrix33/statfick` has one release: the tag `old-game-2026-10-16`, titled "Stat Fick
  (JavaScript version), 2026-10-16 build", with `stat-fick-linux.zip` (127 MB: `Stat Fick.AppImage` and the game
  files it reads) and `stat-fick-windows.zip` (102 MB: `Stat Fick.exe`), a description of how to run each
  ("unsigned: choose More info, Run anyway"), target `main`.
- The old game's Electron packaging (`old-game/desktop/package.json`): "Stat Fick", version 1.0.0; Linux an
  AppImage `Stat Fick.AppImage` whose executable is `stat-fick`; Windows a portable `Stat Fick.exe`
  (`requestExecutionLevel: user`); icons `icons/icon.png` and `icons/icon.ico`.
- `gh release create <tag> --draft` uploads files to a draft release; a draft's tag is made when the user
  publishes it, at its target (`--target`, a branch or a commit).

### Licences

- **BRUTAL License v1.0** (`LICENSE.txt`, the same in every BRUTAL package and in the BRUTAL source in the build
  cache): a product distributed to end users (2.1) includes the copyright notice in its credits screen or
  equivalent, (2.2) shows the BRUTAL logo (ahwoo.com/brutal-branding) before gameplay or the software's primary
  function, unaltered, legible and no less prominent than other logos, "unless permission has been granted in
  writing by Ahwoo Limited or the Product is not capable of displaying graphics", and (2.3) ships `NOTICE.txt`
  alongside; (4.0) the third-party parts keep their licences, listed in each package's `LICENSES/`. BRUTAL's
  README adds: "We also waive the logo requirement for non-profit projects and where displaying it would be
  detrimental to the application's purpose" (the "waived for non-profit" in `Docs/Notes.md` is the README's;
  the licence text itself names written permission). Nothing of this is done yet: no credits, no logo.
- **Third-party texts a download must carry:** each BRUTAL package's `LICENSES/` (FreeType's FTL, GLFW, OpenAL
  Soft's LGPL-2.0 and its four parts: BSD-3-Clause, PFFFT, {fmt}, GSL; stb, VMA, Vulkan, zlib, gli, KTX, glTF),
  HarfBuzzSharp's `LICENSE.txt` and `THIRD-PARTY-NOTICES.txt`, the .NET runtime's licence and third-party notices
  (a self-contained program carries the runtime), Microsoft.Extensions and CommunityToolkit.HighPerformance (MIT),
  and the fonts' OFL texts (inside `ui.pak` as `ui/fonts/lato-ofl.txt` and `stix-ofl.txt`, S7-6; beside the
  sandbox's `Data/Fonts` as `Lato-OFL.txt`, `STIX-OFL.txt`). OpenAL Soft is linked as a separate library the
  player can replace, as the LGPL allows; its source is named in BRUTAL's notices file (upstream and the commit).

### Checks there are

- `test.sh` (build.sh, then `dotnet test`); `mutate.py` mutates text in files and runs a `dotnet test --filter`:
  it has no way to check a script's rule. Full runs take the shared lock inside a headless `cage`
  (`Docs/Engine-Plan.md`, "How each step is finished").
- The sandbox's `--capture <png> --frames N` draws N frames, writes the last and quits; it has no crash trigger.
  The editor has no option that ends it by itself (its tests drive it in-process: a hidden window,
  `AfterLoad`, `AfterUpdate`).

## Method

Packaging has no Chromium or Blender reference for most of it: it is checked against its own rules, and what
the engine draws for it is checked against Chromium as the editor's screens are (M11-20).

- **The engine's and the editor's rules are ordinary tests** in `test.sh`: the version text (a build given a
  version and one without), the log's name, first lines and rotation (scratch folders, files dated older), the
  crash file for each kind of crash (the start, `Load`, `Update`, `Render`, `Unload`, another thread), the crash
  window's document and buttons (a hidden window; the clipboard and the file-manager launcher caught), the
  editor's lookup of the game (scratch game folders, a remembered folder gone, another version). Each rule shown
  able to fail with `mutate.py`.
- **Screens against Chromium:** the crash window's document and any new editor screen as fixtures captured at 1
  and 1.65, compared at the editor's limits (12 levels, 0.2 % of pixels).
- **The packages are checked from their archives** (where these checks run is Q28): each archive unpacked into a
  scratch folder and compared with an expected listing committed in the repo (every path, file or folder, the
  executable bit; nothing missing, nothing extra); the packs byte for byte against `Assets/Packs`; the
  executables' formats (ELF x86-64; PE32+ x86-64 with the subsystem Q6 gives); every native library's needs
  resolved inside the folder or to the expected system libraries (`objdump -p`); the licence files there. Each
  archive's size is reported, not judged.
- **The Linux packages are run:** unpacked into a scratch folder and started headless (the PC: a headless
  `cage` with the user's displays unset; the cloud: Xvfb), with a scratch per-user folder (`XDG_DATA_HOME`, and
  the editor's `--player-folder`), nothing playing; each draws and ends by itself (the sandbox's `--capture`,
  the editor's way from Q20), and its log is read back from the scratch folder; a mod made with the editor
  against the unpacked game's folder is mounted by the packaged stand-in (its log names it).
- **The Windows packages** are built and checked on Linux (listing, formats, imports); they run only on the
  user's Windows PC, through the items step 7 adds to "Windows check". `publish.ps1` and `editor.ps1` can't run
  here (no PowerShell on the PC or in the cloud).
- **The scripts' rules:** each rule of the publish and check scripts broken once must make the check fail (a
  file left out or added, a name changed, a pack altered, the executable bit lost, the wrong subsystem); the
  release script runs its whole path without uploading (a dry run). How these are run (a `mutate.py` list with a
  command in place of the test filter, or by hand with a record) is left to the steps.
- **Never the player's real folders:** no archive holds anything of the per-user folder; no script reads or
  writes it; every run of a program from a script or a session uses a scratch per-user folder; `mutate.py`'s
  check stays. No window on the user's screen; nothing audible.
- Every step ends as `Docs/Engine-Plan.md`, "How each step is finished", says: the full suite on the GPU and on
  lavapipe under the shared lock, every new rule shown able to fail.

## Design

The design the steps work from, as far as the decisions settle it; **(Qn)** marks a point that waits for the
question of that number below.

### The two downloads (M12-1–M12-4, M12-12)

Four archives per release, two per platform, each holding one program (names and the folder inside: Q4):

```
stat-fick-<version>/                 the game (until the port: the sandbox standing in, Q13)
  stat-fick | Stat Fick.exe          the executable (M12-2), self-contained, not trimmed (M12-3); single-file or not: Q5
  <native libraries>                 as the table above; on Windows also the Visual C++ runtime: Q9
  packs/audio.pak, packs/ui.pak      exactly the committed packs, as build.sh lays them out (M9 Q2, S9-A)
  Data/                              the sandbox's own files (fonts, the proof cutscene); stand-in only
  Statventure.Game.Data.dll          with the port (M12-9); a stand-in now or not: Q19
  NOTICE.txt, licences               Q10, Q11
  README.txt                         Q12
statventure-editor-<version>/        the editor
  statventure-editor | Statventure Editor.exe
  <native libraries>, NOTICE.txt, licences, README.txt
  its own fonts                      Q17
```

`classes/` comes with the port (M9 Q27); the stand-in has none. Nothing of the player's per-user folder is in
either. The Windows programs are GUI or console programs as Q6 says; symbols as Q7; .NET's globalization as Q8.

### Building them (publish)

- `Tools/Scripts/publish.sh [--version <v>]` (and `publish.ps1` for the Windows PC) builds, for each program and
  each platform: `dotnet publish -c Release -r linux-x64|win-x64 --self-contained -p:Version=<v>` into a scratch
  folder, renames the executable to M12-2's name (the program's own files keep theirs), lays out `packs/` from
  `Assets/Packs`, adds the licences and the readme, leaves out native `.pdb` files, and makes the archive in
  `release/` at the repo's root (already git-ignored); then the check (Q28) runs on it.
- `build.sh` and `build.ps1` stay as they are: the dev layout in `$STATVENTURE_GAME`, framework-dependent (S9-A);
  the tests use it. The packages are a separate path.
- Without `--version` a publish is a dev build (Q1's dev text). The archives' contents don't depend on the
  machine (no machine paths in the program files: Q7).

### The version (M12-6)

- One place: `Directory.Build.props` holds the dev default; a release's version comes in as `-p:Version=…` from
  the release script, which computes it (Q1, Q2). Each program reads its own version and commit from its
  program file at run time (the informational version, "<version>+<commit>").
- Shown where Q3 says: the log's first lines and each crash file in every case; the editor's title, perhaps the
  game's.
- `EngineInfo.Version` (the engine's own, 0.1.0, given to Vulkan) is not the program's version (left to the
  steps whether it follows).

### Logs (M12-8)

- At the start of `Run()`, once the per-user folder is known (`ApplicationOptions.GameFolder` or this machine's),
  the program opens `logs/<program>-<date-time>.log` there (the name's form and the rotation: Q21) and writes
  its first lines: the program, its version and commit, the system and .NET, then what it logs today (the game
  folder, the mounts, the window). Every line is flushed as written, as now.
- The program names itself for its logs (the stand-in as the game). Old logs beyond the kept number are removed
  at the start, only files of that program's name form; nothing else in `logs/` is touched.
- Tests' applications write no log unless a test asks for one (the redirect may not hold on Windows,
  "Windows check", item 7). Scripts and sessions that run the programs pass a scratch per-user folder.

### Crashes (M12-10)

- **What counts:** an exception out of the start (the data's mount, the window, the device, the sound), `Load`,
  `Update`, `Render` or `Unload`; an unhandled exception on any other thread. Not a crash, as now: a UI
  listener's or script task's exception (logged, the rest runs), the audio thread's rendering failure (logged,
  silence), a failed save write (logged, `WriteFailed`).
- **Then:** (1) the crash file, `logs/crash-<date-time>.txt`: the program, version and commit, the time, the
  exception with its stack (Q7 for lines), the log's last lines; written first, so it exists whatever follows;
  (2) the application torn down as today (the pending saves written, S9-B); (3) the crash window (Q22–Q23): a
  new window, device, swapchain and UI host with a small document and a loop of its own, no data, no sound, no
  storage; (4) the program ends with a non-zero exit code.
- **Another thread's crash:** the crash file at once; the main loop is woken and shows the crash window on the
  main thread (GLFW's windows live there); if the main loop doesn't answer in time, the process ends with the
  crash file written.
- **No window possible** (no Vulkan driver, no display, the Visual C++ runtime missing on Windows, or the crash
  window failing itself): Q24. **Faults .NET can't catch:** Q25.
- The crash path is testable in-process: the window hidden, the exit and the launcher injectable.

### The editor on its own (M12-1, M12-9)

- How it learns where the game is: Q16; `--game <folder>` and the dev setup's `--repo` stay as they are.
- Its own fonts, needed before it knows the game: Q17.
- A game of another version: Q18. The editor loads `Statventure.Game.Data.dll` from the game's folder (M12-9),
  which is built against the game's engine, so the two must come from one release.
- Its log in the same per-user `logs/` as the game's (M12-8); a way to end by itself for checks: Q20.

### Releases (M12-7)

- `Tools/Scripts/release.sh` on the PC (where `gh` is logged in): checks first (Q27), computes the version (Q1,
  Q2), runs the publish for all four archives and checks each, then `gh release create <tag> --draft --target
  <commit> --title … --notes-file …` with the four archives (tag, title and text: Q26), and prints the draft's
  address. The user reads the draft and publishes it; publishing makes the tag at that commit. The repo is
  private, so the release is too.
- Where the Windows archives are built: Q15. Signing: Q14.

### What packaging never touches (M12-11)

The player's per-user folder (`settings.json`, `saves/`, `mods/`, `editor/`, `logs/`) is the player's: the
archives hold none of it, the scripts never read or write it, unpacking a new version never reaches it, and
every check runs the programs on a scratch per-user folder.

## Questions for the user (Q1–Q28)

The design choices these decisions leave open, each in plain words with its options, the recommended one first.
None is asked yet: the manager asks them all at once with the question tool (four per call) before the steps
start; the answers go into `Docs/Engine-Plan.md`, "Decisions for M12", and here.

### Versions

- **Q1. The version's exact form.** (a) `2026.11.8`: year, month and day without leading zeros; a second release
  on the same day `2026.11.8.2`, a third `2026.11.8.3` (the first has no fourth number); a build from the repo
  that isn't a release says `dev`. *Recommended:* it reads as the date and is a valid version number everywhere
  (.NET's and Windows' versions are up to four numbers, each below 65535), so versions sort and compare
  correctly. (b) `2026.11.08`, month and day with two digits in the text (archives, tag, titles); the program's
  own version number is `2026.11.8` (version numbers drop leading zeros); same-day releases `.2`. (c) Always four
  numbers, `2026.11.8.1` for the first release of the day.
- **Q2. Which date.** (a) The date (UTC) of the commit the release is built from: the same commit always gives the
  same version; the same-day number counts the releases already made with that date. *Recommended:* a version
  names the code exactly, also when a release is made again. (b) The day the release script runs (the PC's local
  date).
- **Q3. Where the version and the commit show.** (a) The editor's window title, "Statventure Editor 2026.11.8 —
  ember-rite.pak"; the game's title stays the game's name (the port shows its version where the game decides,
  for example its title screen); the first lines of every log and every crash file give the version and the
  commit, which shows nowhere else. *Recommended:* a modder sees which editor runs, the player's window stays
  clean, and a log or crash file sent in names the exact code. (b) Both titles show the version (the stand-in's
  "Statventure Sandbox 2026.11.8"); the commit in logs and crash files only. (c) Logs and crash files only; no
  title. (d) As (a), with the commit in the editor's title too ("2026.11.8 (bb2e443)").

### The downloads

- **Q4. The archives' names and the folder inside.** (a) `stat-fick-2026.11.8-linux.tar.gz`,
  `stat-fick-2026.11.8-windows.zip`, `statventure-editor-2026.11.8-linux.tar.gz`,
  `statventure-editor-2026.11.8-windows.zip`, each holding one folder named like the archive without the
  platform (`stat-fick-2026.11.8/`): a new version unpacks beside the old, never into it. *Recommended:* no
  stale files from an older version (a pack the new one dropped would still be mounted), and the names say what
  and which. (b) The same archives, the folder inside named for the program only (`Stat Fick/`, `Statventure
  Editor/`): a new version unpacked over the old (what the new one dropped stays behind). (c) No folder inside:
  the files at the archive's top.
- **Q5. The program's files in the folder.** (a) Single-file: each program is one executable holding its own code
  and .NET; beside it only the native libraries (about a dozen on each system, plus the Linux soname copies, and
  whatever of .NET's own native files the single-file host doesn't hold: the step lists them), `packs/` and the
  licences; the data library (M12-9) stays a file of its own so the editor can load it.
  *Recommended:* the game-like folder the user asked for (`Docs/Notes.md`, "Give the shipped game a proper
  game-like folder structure"), behaving the same (nothing trimmed, M12-3). (b) The usual self-contained folder:
  the executable among about 200 files of .NET (`System.*.dll`, `libcoreclr.so` …). (c) Single-file with the
  native libraries inside too, which .NET unpacks into a temporary folder at a version's first start.
- **Q6. Windows: a console window or not.** (a) Both are Windows GUI programs (`WinExe`): started from Explorer only
  their window opens; what they log goes to the log file; a problem with the editor's command line shows in a
  plain window like the crash window (on Linux also in the terminal, as now). *Recommended:* what players expect
  of a game and a tool, and the log keeps everything the console showed. (b) Console programs as now: a console
  window opens beside each, showing the log as it runs. (c) The game without a console, the editor with one
  (modders see its log live). (On Linux this changes nothing.)
- **Q7. Debug symbols.** (a) Embedded in the program files: a crash file's stack names each call's source file and
  line (paths from the repo's root, not the build machine's folders); no `.pdb` files in the folder.
  *Recommended:* a crash file a player sends points at the line, and the folder stays tidy. (b) Separate `.pdb`
  files beside the program (the same stacks, more files). (c) None: stacks with method names only. In every case
  the native `libHarfBuzzSharp.pdb` (23 MB) is left out.
- **Q8. Linux: .NET's globalization (ICU).** (a) Invariant mode for both programs and for the tests: no ICU needed
  on the player's system, and text compares and formats the same everywhere (the engine passes the invariant
  culture wherever it formats or parses; a search found no culture-dependent call); the full suite shown to pass
  in that mode first. *Recommended:* one system dependency fewer, and Linux, Windows and the tests alike. (b) The
  system's ICU, as now: every desktop Linux has it; a system without it can't start the program (.NET's message
  in a terminal, before any log or window). (c) ICU shipped with the programs (about 30 MB more per download).
- **Q9. Windows: the Visual C++ runtime that BRUTAL's libraries need.** (a) Shipped beside the program in both
  Windows downloads (`vcruntime140.dll`, `vcruntime140_1.dll`, `msvcp140.dll`: Microsoft's documented local
  deployment), taken once from Microsoft's Visual C++ redistributable into the build cache, never into git.
  *Recommended:* the zip runs as unpacked, also on a fresh Windows, without an installer (M12-4). (b) Required of
  the player: the readme names the Visual C++ 2015–2022 redistributable (x64) and Microsoft's download; without
  it the program can't open its window (Q24 then). (c) Microsoft's redistributable installer put in the zip for
  the player to run.
- **Q10. The licence files.** (a) `NOTICE.txt` (BRUTAL's, 2.3) at the top of each download and a `licenses/`
  folder with every licence and notice as its package gives it (BRUTAL's `LICENSE.txt`, each BRUTAL package's
  `LICENSES/`, HarfBuzzSharp's two, .NET's licence and third-party notices, Microsoft.Extensions and
  CommunityToolkit's, the fonts' OFL texts, the Visual C++ runtime's terms if Q9 (a)). *Recommended:* each text
  as its authors wrote it, easy to check against the packages. (b) One `THIRD-PARTY-NOTICES.txt` joining them
  all, beside `NOTICE.txt`.
- **Q11. The BRUTAL licence's logo and credits** (2.1, 2.2; "What exists", "Licences"). (a) M12 ships `NOTICE.txt`
  in both downloads; the copyright notice in a credits screen and the logo before gameplay are port-time items
  (the game's start and credits are the port's screens), and until they are in, the packages stay the user's
  own, not shared beyond the user, unless the user settles the logo first (the README's waiver for non-profit
  projects, or Ahwoo's written permission). *Recommended:* nothing in M12 waits on the art or the game's start,
  and nothing is distributed against the licence. (b) M12 also shows the BRUTAL logo when each program starts
  and the copyright notice in an About view of each (the editor's top bar, the stand-in's page). (c) The user
  asks Ahwoo for written permission not to show the logo; M12 ships `NOTICE.txt` and puts the notice in the
  readme.
- **Q12. A readme in each download.** (a) A short `README.txt`: what it is, how to start it, what the system needs
  (a Vulkan 1.3 GPU driver; Linux with glibc 2.34 or later; the Visual C++ runtime if Q9 (b)), where the player's
  folder and the logs are; for the editor that it needs the game and how it is told where; for the stand-in that
  it is the engine's sandbox. *Recommended:* the archives explain themselves wherever they end up. (b) Only the
  release's description on GitHub, as the old game's. (c) None.
- **Q13. Saying that the game's download is the stand-in.** (a) It keeps its title "Statventure Sandbox", and the
  release's description (and the readme, if Q12 (a)) says that the game's download is the engine's sandbox
  standing in for Stat Fick until the port. *Recommended:* clear to whoever downloads it, nothing added to the
  sandbox. (b) Also a line on the sandbox's own page. (c) Nothing beyond its title.
- **Q14. Signing the Windows programs.** (a) Unsigned in M12, as the old game's (SmartScreen's "More info, Run
  anyway"); signing looked at again when the game goes public, with the update check. *Recommended:* a
  certificate costs money every year and the packages are private for now. (b) Look into a code-signing
  certificate now.
- **Q15. Where the Windows archives are built.** (a) On Linux with the rest (the release script on the PC builds
  all four): the `.exe` files then carry no Windows version details (NETSDK1074; the version is in the title,
  the log and the archive's name), and the icons, when they come, need a Windows build or a resource tool then.
  *Recommended:* one machine makes the whole release, and nothing in M12 needs the resources. (b) On the Windows
  PC by `publish.ps1`, uploaded from there to the same draft. (c) On GitHub's Windows runners (GitHub Actions on
  the private repo, from its monthly minutes).

### The editor

- **Q16. How the separately downloaded editor finds the game.** (a) It asks: on its first start, and whenever the
  folder it remembers no longer holds the game (for example after a new version was unpacked beside the old), a
  screen of its own, "Where is Stat Fick?", with its file browser (M11-9); the player picks the game's folder;
  the editor remembers it in `editor/` (G6); `--game <folder>` still overrides, and the dev setup's `--repo`
  stays. *Recommended:* "the editor must be told where the game is" (M12-1) without a command line, and it
  survives updates. (b) As (a), but first it looks in the usual places itself (a game folder beside the editor's
  own folder; `packs/` beside the program) and asks only when it finds none. (c) Always on the command line
  (`--game <folder>`; on Windows through a shortcut), else it stops with its usage message.
- **Q17. The editor's own fonts** (for Q16 (a) or (b): its first screen is drawn before it knows the game, and
  today its fonts are the game's `ui.pak`'s). (a) Lato regular and bold (the faces it uses, P4) inside its own
  program file, with the OFL text among its licences; it always uses its own. *Recommended:* the editor looks the
  same whatever game folder it is given, or none, and its folder gets no extra file. (b) A small pack of its own
  beside it (`editor.pak`: the two faces and the OFL text). (c) Its own Lato only for that first screen, the
  game's `ui.pak` once the game is known (as now). (If Q23 (a) puts Lato regular into the engine, the editor adds
  only the bold face.)
- **Q18. A game folder of another version than the editor.** (a) Refused: the editor names both versions and asks
  for a matching game (the data library and the packs must come from the editor's release); the version read
  from the game's program file, no extra file. *Recommended:* an editor never shows or saves against kinds it
  wasn't built with. (b) Accepted with a warning in the status line. (c) No check.
- **Q19. The data library's loading: now or at port time.** (a) Now: the editor loads `Statventure.Game.Data.dll`
  from the game's folder, proven with a stand-in library of that name (one test record kind, in `Samples/`)
  shipped in the stand-in's download; the port replaces the stand-in library with its own. *Recommended:* M12-12
  proves the whole pipeline now, and the version check (Q18) gets its real case. (b) At port time: M12 only keeps
  the file's place in the layout.
- **Q20. A way for checks to start a packaged editor and have it end by itself** (the end-to-end check, step 7).
  (a) The editor gets the sandbox's option: `--capture <file.png> --frames N` (draws N frames, saves the last,
  quits), in its usage text. *Recommended:* the same option as the sandbox's, and harmless for players. (b) An
  environment variable read only for checks, not in the usage text. (c) No option: the check ends the process
  once its log says the window is open.

### Logs and crashes

- **Q21. The logs' names and how many are kept** (M12-8: one per run, the last 10). (a)
  `stat-fick-2026-11-08_14-03-22.log` and `statventure-editor-…` (the program's Linux name, local time, no colons
  for Windows); the last 10 runs per program (the game's and the editor's counted apart); crash files
  (`crash-2026-11-08_14-03-22.txt`) the last 10 too; the oldest removed when a program starts. *Recommended:*
  starting the editor often never pushes out the game's logs, and the folder can't grow. (b) The last 10 runs in
  all, both programs counted together, crash files likewise. (c) As (a), with crash files never removed.
- **Q22. What the crash window says and offers.** (a) A heading "Stat Fick stopped because of an error" (the
  editor: "Statventure Editor stopped …"), one line naming the error (its kind and message, e.g. "audio.pak: the
  index doesn't match its hash (damaged)"), "The details are in:" and the crash file's full path in a read-only
  field, selected for copying, with a Copy button; then "Open logs folder" (the system's file manager, as the
  game's "Open game folder"; not a dialog) and "Close"; in English. *Recommended:* the player can copy the path,
  or open the folder and send the file, in one click each. (b) The same without "Open logs folder": the path for
  copying only. (c) As (a), with the stack trace shown in a scrolling box too.
- **Q23. The crash window's look and font.** (a) A plain dark look in the editor's colours (P4: `#1a1c1a` ground,
  `#eeeae1` text, the ember accent) for both programs, a small fixed window; its text in Lato from a copy
  embedded in the engine, so it draws even when the game's packs are damaged or missing. *Recommended:* one
  look the engine owns, which works when the crash is the data's. (b) The same font, in the game's parchment look
  for the game and the editor's look for the editor. (c) The editor's look with Lato from the game's `ui.pak`;
  when that can't be read, no window (the crash file only).
- **Q24. When no crash window can be drawn** (no Vulkan driver, no display, the Visual C++ runtime missing, or the
  crash window failing itself). (a) The crash file only, its message also on the terminal (seen when started
  from one); the program ends with an error code. *Recommended:* the game's rule of no OS dialogs holds; the
  readme (Q12) names what the system needs. (b) On Windows only, the system's message box as a last resort (an
  OS dialog, against the game's rule) naming the error and the crash file; on Linux as (a).
- **Q25. A crash .NET can't catch** (a native library's fault, a stack overflow). (a) The run's log as written up
  to the fault (each line is flushed as written); nothing else. *Recommended:* simple, and the log already holds
  what led to it. (b) At its next start the program notices that the last run's log has no closing line and
  writes a crash file for it ("ended unexpectedly"); nothing shown. (c) As (b), and the crash window shown at
  that next start.

### Releases

- **Q26. The release's tag, title and text.** (a) Tag `v2026.11.8`, title "Stat Fick 2026.11.8", a fixed short
  text written by the script (the four archives, how to start each, the stand-in note, the unsigned `.exe`) that
  the user can edit in the draft before publishing. *Recommended:* the usual tag form, and the description of the
  old game's release carried on. (b) Tag `stat-fick-2026.11.8`, like the old game's `old-game-2026-10-16`; the
  same title and text. (c) As (a), the text listing the commits since the last release.
- **Q27. What the release script checks before it builds.** (a) It refuses unless the work tree is clean and its
  commit is on GitHub's `main`; it runs no tests itself (every step ran them); then it builds, checks each
  archive and uploads the draft. *Recommended:* the draft's tag can then be made at a commit GitHub has, and a
  release takes minutes. (b) Also the full suite first, refusing on any failure. (c) No checks: it builds
  whatever is checked out.

### Method

- **Q28. Where the package checks run.** (a) A script of their own (`Tools/Scripts/check-packages.sh`), run by
  the packaging steps, the end-to-end step and the release script, not by `test.sh` (publishing four packages
  takes minutes); the engine's and the editor's rules (logs, crash files, the crash window, the version, the
  game's lookup) are ordinary tests in `test.sh`. *Recommended:* the full suite stays as fast as it is; the
  packages are checked whenever they are made. (b) Everything in `test.sh` (every full run publishes and checks
  the four packages). (c) `test.sh` publishes and checks the Linux packages, the script the Windows ones.

## Left to the steps

Small internal choices, each recorded by the step that makes it (a step that finds one visible to players
stops and asks):
- The logging and crash API (an option naming the program for its logs, how the crash is handed between
  threads, the crash window's code), the first lines' exact content (with the GPU's name once the device is
  made), how many of the log's last lines go into a crash file, exit codes (non-zero after a crash; the editor's
  2 for its command line, as now).
- Renaming the executable at publish rather than naming the assembly per platform (proposed above), and the
  usage text naming the new executables.
- Whether the Linux soname copies are pruned to the names actually loaded (about 8 MB); the archives' expected
  listings record the outcome either way.
- Archive mechanics: `tar` with numeric owner 0 and kept modes; how the zip is made on Linux (the PC's `zip`; the
  cloud needs `zip` in `cloud-setup.sh` or Python's `zipfile`); the expected listings' format and folder.
- The dev default version in `Directory.Build.props`; whether `EngineInfo.Version` follows the program's version.
- Where the editor remembers the game's folder in `editor/` (if Q16 (a) or (b)); how dev versions match in Q18.
- A crash trigger for the stand-in only, for the end-to-end check (the sandbox's own option, as its
  `--capture`), and a way for scripts to give the sandbox a scratch per-user folder (`XDG_DATA_HOME`, or a
  `--player-folder` as the editor has).
- `mutate.py`'s way to mutate a script's rule (a command in place of the test filter).

## Steps

Each step ends as every step does (`Docs/Engine-Plan.md`, "How each step is finished"): its tests, the full
suite on the GPU and on lavapipe, a mutation list covering every rule it adds (each killed or accounted for),
the package checks of "Method" where it makes packages, its Progress line, its "How it works" and Status entry
here, `Docs/Notes.md`. At most four workers at once, each in its own worktree; **only one at a time runs the
full `Tools/Scripts/test.sh` or `mutate.py`**, serialised by the shared lock (from the repo's root, `flock -o
~/.cache/stat-fick-build/claude-work/full-run.lock engine/Tools/Scripts/test.sh`, the same lock for the
lavapipe run and for `mutate.py`, MSBuild's node reuse off, inside a headless `cage`); filtered runs in a
worker's own worktree need no lock. Everything headless (no window on the user's screen), nothing audible,
nothing written into the player's real folders (scratch per-user folders for every program run from a script).
Workers don't touch Engine-Plan's "Current task" (the merge worker does). Steps side by side touch no file in
common besides those docs (each its own entry). No step starts before the questions it needs are answered.

Order: **wave 1:** 1, 2 and 3 side by side; **wave 2:** 4 (after 2), 5 (after 1, 2 and 3) and 6 (after 1 and
3) side by side; **wave 3:** 7 (after all). Then the manager ticks M12 and stops.

1. **The version (engine, editor's title).** Independent. Needs Q1–Q3, Q8.
   - Owns: `engine/Directory.Build.props` (the dev default version; `InvariantGlobalization` for every project if
     Q8 (a)), a new `Engine/Statventure/Core/ProgramInfo.cs` (the program's name, version and commit, read from
     its program file; the dev text), `Tools/Editor/Shell/EditorView.cs` (the title, Q3), tests in
     `Tests/Statventure.Tests/Core/` and step 4's `ShellTests` (the title).
   - Must not touch: the record layouts, the editor's document (the title is the window's, so no screen is
     captured again), the scripts.
   - Finished: a build given `-p:Version=2026.11.8` reads that version and the commit; a dev build reads the dev
     text; the title as Q3 says; with Q8 (a) the full suite passes in invariant mode. Mutations: the reading of
     each part, the dev text, the title's form.
2. **Logs and crash files (engine).** Independent (the first lines take step 1's `ProgramInfo` once it is
   merged: rebase for that one line). Needs Q21, Q25.
   - Owns: `Engine/Statventure/Core/Log.cs`, a new `Core/LogFiles.cs` (folder, names, rotation),
     `Core/Application.cs` (the run's log once the per-user folder is known; the crash file; the handler for other
     threads), one line each in `Samples/Statventure.Sandbox/Program.cs` and `Tools/Editor/EditorApplication.cs`
     (their names for the logs), tests in `Tests/Statventure.Tests/Core/`, `Docs/Assets.md`'s line on the
     per-user folder (`logs/` beside the rest).
   - Must not touch: the window, device and UI (the crash window is step 4), the save writer's rules (a crash
     still writes the pending saves, S9-B), the tests' redirect.
   - Finished: one log per run with its first lines; rotation as Q21 (only that program's files, nothing else in
     `logs/`); a crash file for each kind of crash with its parts; tests' applications write no log unless asked;
     nothing outside the scratch folder. Mutations: the kept number, which files count, the names, each part of
     the crash file, each crash source, the order (crash file before the teardown).
3. **Publishing both programs for both platforms, the archives and their check (scripts).** Independent. Needs
   Q4–Q15, Q28.
   - Owns: new `Tools/Scripts/publish.sh`, `publish.ps1` and (Q28 (a)) `check-packages.sh`, a new
     `Tools/Packaging/` (the expected listings, the readme texts, the licence files' list),
     `Samples/Statventure.Sandbox/Statventure.Sandbox.csproj` and `Tools/Editor/Editor.csproj` (the output type,
     Q6; publish settings, Q5, Q7), `Tools/Scripts/editor.ps1` (the Windows counterpart of `editor.sh`, "Windows
     check" item 8d, whose line it updates), `cloud-setup.sh` if the cloud needs a tool.
   - Must not touch: engine code, `build.sh` and `build.ps1`'s dev layout (S9-A), the per-user folders.
   - Finished: `publish.sh` makes the four archives into `release/` from a clean tree; the check passes on each
     and fails on each broken rule (a file missing or extra, a name, a pack changed, the executable bit, the
     subsystem, a licence missing, a native library's need unmet); the unpacked Linux stand-in starts headless,
     draws and quits (`--capture`) on a scratch per-user folder; sizes reported.
4. **The crash window (engine UI).** After 2. Needs Q22–Q24 (and Q6 for the Windows command-line message).
   - Owns: a new `Engine/Statventure/Core/CrashWindow.cs` (its document, loop and buttons), the embedded font if
     Q23 (a) (`Engine/Statventure/UI/Resources/`, `Statventure.csproj`'s resource line, its OFL text),
     `Core/Application.cs` (showing it after the teardown; the other threads' crash brought to the main loop),
     tests in `Tests/Statventure.Tests/Core/` with its Chromium fixture at 1 and 1.65.
   - Must not touch: step 2's crash file rules, the UI's own code.
   - Finished: after each kind of crash the window shows the error and the path (hidden window); Copy fills the
     clipboard; "Open logs folder" (if chosen) launches the file manager on `logs/` (caught); Close ends with the
     error code; the window failing falls back as Q24; another thread's crash shown by the main loop; the document
     matches Chromium within the limits. Mutations: each.
5. **The editor as a download of its own (editor).** After 1, 2 and 3. Needs Q16–Q20.
   - Owns: `Tools/Editor/**` (the arguments, `GameData`, the game's lookup and its screen if Q16 (a)/(b), the
     remembered folder, the fonts (Q17), the version check (Q18), the data library's loading (Q19), the end-by-itself
     option (Q20)), its tests and Chromium fixtures (`Tests/Statventure.Tests/Editor/`), `Tools/Scripts/editor.sh`
     if the arguments change, the stand-in data library if Q19 (a) (a new `Samples/Statventure.Game.Data`,
     `Statventure.slnx`, the sandbox's project reference, the stand-in's expected listing), `Docs/Editor.md` (its
     section).
   - Must not touch: the engine, the editor's other screens (none captured again unless the shell changes).
   - Finished: the lookup's rules on scratch game folders (none, remembered, gone, another version, `--game`,
     `--repo`); the new screen against Chromium; the stand-in's kinds read from a game folder (Q19 (a));
     mutations: each.
6. **The release script.** After 1 and 3. Needs Q1, Q2, Q15, Q26, Q27.
   - Owns: a new `Tools/Scripts/release.sh` (the version from the commit's date and the repo's release tags, the
     checks, the publish and check of all four, the draft through `gh`, a dry run that uploads nothing) and its
     tests (the dry run against a scratch repository with a stand-in `gh` that records its arguments).
   - Must not touch: the publish and check scripts (step 3's), any real release (the first real draft is made
     only on the user's word).
   - Finished: the dry run's version for a first and a same-day release, its refusals (a dirty tree, a commit
     not on GitHub), the `gh` call with the tag, target, title, text and four archives. Mutations: each.
7. **End to end, and the Windows items.** After all.
   - The Linux archives unpacked into scratch folders: both programs started headless on a scratch per-user
     folder, each ending by itself; their logs (one each, version and commit; the eleventh run removing the
     oldest); the stand-in made to crash (its own trigger) gives the crash file and the crash window (captured
     headless); the editor told the unpacked game's folder (Q16's way) makes a mod (step 14's in-process flow on
     that folder), and the packaged stand-in mounts it (its log names it); the Windows archives checked; the
     Windows items added to "Windows check" (both zips unpacked and started from Explorer: no console if Q6 (a),
     the names with spaces, the logs in `%APPDATA%\Stat Fick\logs`, the crash window, the editor finding the game,
     a mod made and loaded, `publish.ps1` and `editor.ps1`, the Visual C++ runtime on a machine without it if one
     is at hand, SmartScreen).
   - Owns: tests or a script of its own for the flow, the "Windows check" items, this doc's Status.
   - Finished: the flow passes on the PC (GPU and lavapipe) and in the cloud's setup if it can run there; then the
     manager ticks M12 in Engine-Plan's Milestones, updates the notes and stops for the user.

## How it works

Filled in as the steps are done.

## Status

**M12 started (2026-11-08): the decisions recorded (M12-1–M12-12), what exists checked against the code, the
method, the design proposed with 28 questions for the user (Q1–Q28, not asked yet), seven steps proposed with
their dependencies.** No step started; nothing implemented.

Found by the survey (each in "What exists"): the build publishes only the sandbox, framework-dependent, with
every runtime's natives; nothing publishes the editor; the executable takes the assembly's name; no version
anywhere but the engine's own; no program writes a log file; a crash leaves `Main` after the window is gone,
so a crash window needs a window and device of its own; the editor's fonts are the game's `ui.pak`'s, so an
editor downloaded apart can't draw before it knows the game; BRUTAL's Windows libraries need the Visual C++
runtime, which nothing ships; a Windows `.exe` built on Linux has no version resources (NETSDK1074); the Linux
libraries need glibc 2.34; the BRUTAL licence asks for `NOTICE.txt`, the copyright in the credits and the logo
before the program's primary function (its README waives the logo for non-profit projects, the licence text
names written permission).
