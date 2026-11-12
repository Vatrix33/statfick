# Packaging (M12)

The game and the editor as players get them: two downloads per platform (Linux and Windows), each a
self-contained single-file program in an archive; the BRUTAL logo before each program starts and a credits view
in each; per-run logs and crash files in the player's per-user folder and a crash window drawn by the engine;
date-based versions; draft releases on the private GitHub repo that the user publishes. M12 builds and proves all
of it with the sandbox standing in for the game (M12-12); the port replaces the stand-in.

## Decisions (user, 2026-11-08)

In full, with the options not chosen, in `Docs/Engine-Plan.md`, "Decisions for M12". **The rule to ask stands in
full again:** every design choice of M12 goes to the user, the editor's packaging included (M11's exception was
for M11's editor only).

**The main questions (M12-1–M12-12)**, asked before this doc was written, on the user's go ("Then start with
M12"); every answer the recommended option except M12-1, M12-5 and M12-6:
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
- **Versions (M12-6, not the recommendation): date-based** (their form: Q1–Q3).
- **Updates (M12-7):** an in-game update check later, at port time (once the game is public somewhere, a check
  against that place); meanwhile no check: a release script builds both platforms' archives and uploads them
  as a draft GitHub Release with the version's tag on the private repo, which the user publishes.
- **Logs (M12-8):** `logs/` in the player's per-user folder (`~/.local/share/stat-fick/logs`,
  `%APPDATA%\Stat Fick\logs`), one file per run, `logs/<program>-<date-time>.log`, the last 10 kept; on a crash
  the exception and the log's end also go to `logs/crash-<date-time>.txt`; the editor logs the same way.
- **The data library (M12-9; closes G3):** the port puts its record kinds in a library of their own,
  `Statventure.Game.Data`, which the game links and the editor loads from the game's folder. **Clarified by the
  user (2026-11-08):** the editor's download carries no copy of it; the editor loads the game's (the "one file"
  is in the game's download).
- **On a crash (M12-10):** a plain window drawn by the engine (no OS dialog, the game's rule) naming what went
  wrong and the crash file's path, shown for copying; then the program ends.
- **No portable mode (M12-11):** the player's folder is always the per-user one.
- **The first packages (M12-12):** the sandbox as the game's stand-in, plus the editor; the whole pipeline
  (publish, archives, versions, logs, crash handling, releases) is built and proven now; the port later
  replaces the stand-in.

**The detail round (Q1–Q28)**, the questions this doc raised, asked with the question tool after it was written
and answered the same day; every answer the recommended option except Q1, Q11 and Q12. Each is under "Decided"
below with its options; in short: versions always of four numbers, `2026.11.8.1` (Q1), from the released commit's
UTC date (Q2), in the editor's title and in every log's and crash file's first lines with the commit (Q3);
`stat-fick-2026.11.8.1-linux.tar.gz` and the like, a versioned folder inside (Q4); single-file programs with the
native libraries beside them (Q5); no console on Windows (Q6); symbols embedded (Q7); .NET's invariant
globalization for the programs and the tests (Q8); the Visual C++ runtime's three DLLs shipped beside the Windows
programs (Q9); `NOTICE.txt` and `licenses/` (Q10); **a BRUTAL logo splash and a credits view in both programs now
(Q11)**; **no readme in the downloads, the release's text instead (Q12)**; the stand-in named in the release's text
(Q13); unsigned (Q14); the Windows archives built on Linux (Q15); the editor asks "Where is Stat Fick?" and
remembers the answer (Q16), with Lato embedded (Q17), refuses a game of another version (Q18), loads the data
library now through a stand-in (Q19) and gets `--capture` (Q20); logs named by local date and time, 10 per program,
10 crash files (Q21); the crash window as proposed (Q22), dark, in the engine's embedded Lato (Q23); without a
window the crash file and a terminal message (Q24); the log only for what .NET can't catch (Q25); tag
`v2026.11.8.1` (Q26); the release script checks a clean, pushed tree and runs no tests (Q27); package checks in a
script of their own (Q28). Q11's answer raised two questions of its own, answered the same day as recommended:
the splash (Q29) and the credits view (Q30).

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
- **The logo's files:** the BRUTAL source in the build cache has `docs/branding/LOGO_BRUTAL_WHT.png` and
  `LOGO_BRUTAL_BLK.png` (712 × 168, RGBA), which its README shows as the logo; the packages carry only a 32 px
  `brutal-icon-32px.png`. The licence names ahwoo.com/brutal-branding as the logo's source; BRUTAL's
  `CONTRIBUTING.md` names brutal@ahwoo.com for a waiver. (Step 4 found the branding page offering 4096 × 896
  versions since 2026-10-21; the engine embeds the page's white one: "Decided (the logo's file)".)
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
the engine draws for it (the splash, the credits view, the crash window, the editor's new screen) is checked
against Chromium as the editor's screens are (M11-20).

- **The engine's and the editor's rules are ordinary tests** in `test.sh`: the version text (a build given a
  version and one without), invariant globalization in force, the log's name, first lines and rotation (scratch
  folders, files dated older), the crash file for each kind of crash (the start, `Load`, `Update`, `Render`,
  `Unload`, another thread), the splash's timing and skipping, the credits' required lines against the licence
  files, the crash window's document and buttons (a hidden window; the clipboard and the file-manager launcher
  caught), the editor's lookup of the game (scratch game folders, a remembered folder gone, another version, the
  stand-in library's kinds). Each rule shown able to fail with `mutate.py`.
- **Screens against Chromium:** the splash, the credits view, the crash window and the editor's "Where is Stat
  Fick?" screen as fixtures captured at 1 and 1.65, compared at the editor's limits (12 levels, 0.2 % of pixels).
- **The packages are checked from their archives by `Tools/Scripts/check-packages.sh`** (Q28), which the packaging
  steps, the end-to-end step and the release script run; `test.sh` doesn't. Each archive is unpacked into a
  scratch folder and compared with an expected listing committed in the repo (every path, file or folder, the
  executable bit; nothing missing, nothing extra); the packs byte for byte against `Assets/Packs`; the
  executables' formats (ELF x86-64; PE32+ x86-64 with the GUI subsystem, Q6); every native library's needs
  resolved inside the folder or to the expected system libraries (`objdump -p`); the licence files and the
  Visual C++ runtime's DLLs there. Each archive's size is reported, not judged.
- **The Linux packages are run:** unpacked into a scratch folder and started headless (the PC: a headless
  `cage` with the user's displays unset; the cloud: Xvfb), with a scratch per-user folder (`XDG_DATA_HOME`, and
  the editor's `--player-folder`), nothing playing; each shows the splash, draws and ends by itself
  (`--capture <png> --frames N`, the sandbox's and now the editor's, Q20), and its log is read back from the
  scratch folder; a mod made with the editor against the unpacked game's folder is mounted by the packaged
  stand-in (its log names it).
- **The Windows packages** are built and checked on Linux (Q15: listing, formats, imports); they run only on the
  user's Windows PC, through the items step 8 adds to "Windows check". `publish.ps1` and `editor.ps1` can't run
  here (no PowerShell on the PC or in the cloud).
- **The scripts' rules:** each rule of the publish and check scripts broken once must make the check fail (a
  file left out or added, a name changed, a pack altered, the executable bit lost, the console subsystem, a
  licence or runtime DLL missing); the release script runs its whole path without uploading (a dry run). How
  these are run (a `mutate.py` list with a command in place of the test filter, or by hand with a record) is left
  to the steps.
- **Never the player's real folders:** no archive holds anything of the per-user folder; no script reads or
  writes it; every run of a program from a script or a session uses a scratch per-user folder; `mutate.py`'s
  check stays. No window on the user's screen; nothing audible.
- Every step ends as `Docs/Engine-Plan.md`, "How each step is finished", says: the full suite on the GPU and on
  lavapipe under the shared lock, every new rule shown able to fail.

## Design

The design the steps work from, decided (Q1–Q30).

### The two downloads (M12-1–M12-4, M12-12; Q4–Q10, Q12)

Four archives per release, two per platform, each holding one program in a folder named for it and its version
(Q4):

```
stat-fick-2026.11.8.1-linux.tar.gz | stat-fick-2026.11.8.1-windows.zip
  stat-fick-2026.11.8.1/             the game (until the port: the sandbox standing in, Q13)
    stat-fick | Stat Fick.exe        one single-file executable: its code and .NET (Q5), not trimmed (M12-3),
                                     symbols embedded (Q7), invariant globalization (Q8); on Windows a GUI program (Q6)
    <native libraries>               as "Native libraries" lists them; on Windows also vcruntime140.dll,
                                     vcruntime140_1.dll and msvcp140.dll (Q9)
    Statventure.Game.Data.dll        the data library, a file of its own beside the executable (M12-9; until the
                                     port a stand-in with a test kind, Q19)
    packs/audio.pak, packs/ui.pak    exactly the committed packs, as build.sh lays them out (M9 Q2, S9-A)
    Data/                            the sandbox's own files (fonts, the proof cutscene); stand-in only
    NOTICE.txt, licenses/            BRUTAL's notice and every licence text as its package gives it (Q10)
statventure-editor-2026.11.8.1-linux.tar.gz | statventure-editor-2026.11.8.1-windows.zip
  statventure-editor-2026.11.8.1/
    statventure-editor | Statventure Editor.exe   single-file as the game's; Lato inside (Q17, Q23)
    <native libraries>, NOTICE.txt, licenses/     no data library: it loads the game's (M12-9, clarified)
```

`classes/` comes with the port (M9 Q27); the stand-in has none. No readme (Q12): the release's text says what one
would have. Nothing of the player's per-user folder is in either. A new version unpacks beside the old.

### Building them (publish)

- `Tools/Scripts/publish.sh [--version <v>]` (and `publish.ps1` for the Windows PC) builds, for each program and
  each platform: `dotnet publish -c Release -r linux-x64|win-x64 --self-contained -p:PublishSingleFile=true
  -p:Version=<v>` into a scratch folder (the data library left out of the bundle, a file beside it), renames the
  executable to M12-2's name (the program's own files keep theirs), lays out `packs/` from `Assets/Packs`, adds
  `NOTICE.txt` and `licenses/` and, on Windows, the three Visual C++ runtime DLLs from the build cache
  (`~/.cache/stat-fick-build/`, taken once from Microsoft's redistributable, never in git), leaves out native
  `.pdb` files, and makes the archive in `release/` at the repo's root (already git-ignored); then
  `check-packages.sh` runs on it.
- The Windows archives are built on Linux too (Q15): their `.exe` files carry no Windows version details
  (NETSDK1074); the version is in the editor's title, the logs and the archive's name. The icons, when they come
  (M12-5, at port time), need a Windows build or a resource step.
- `build.sh` and `build.ps1` stay as they are: the dev layout in `$STATVENTURE_GAME`, framework-dependent (S9-A);
  the tests use it. The packages are a separate path.
- Without `--version` a publish is a dev build: its version says `dev`. No machine paths in the program files
  (embedded symbols with paths from the repo's root, Q7).

### The version (M12-6; Q1–Q3)

- **Its form (Q1):** always four numbers, year, month, day and the release of that day: `2026.11.8.1`, a second
  release the same day `2026.11.8.2`. A build from the repo that isn't a release says `dev` (its commit in the
  log).
- **Its date (Q2):** the UTC date of the commit released; the fourth number counts the releases already tagged with
  that date (`v2026.11.8.*`), so the same commit always gives the same date, whenever the script runs (a commit
  released a second time gets the next number: "How it works", step 7, the manager's decision).
- **One place:** `Directory.Build.props` holds the dev default; a release's version comes in as `-p:Version=…` from
  the release script. Each program reads its own version and commit from its program file at run time (the
  informational version, "<version>+<commit>").
- **Where it shows (Q3):** the editor's title, "Statventure Editor 2026.11.8.1 — ember-rite.pak"; the first lines
  of every log and every crash file give the version and the commit; the game's title stays the game's name (the
  stand-in's "Statventure Sandbox"); the credits view (Q30) names the version too. The commit shows in no window.
- `EngineInfo.Version` (the engine's own, 0.1.0, given to Vulkan) is not the program's version (left to the
  steps whether it follows).

### The BRUTAL logo and the credits (Q11, Q29, Q30)

The BRUTAL licence's 2.1–2.3 met in M12 for both programs (the ported game shows them again in its own screens):
- **`NOTICE.txt`** beside each program (2.3, Q10).
- **The logo before the program's main function (2.2):** a splash shown by `Application.Run` once the window and
  device are made and before `Load` (so before the sandbox's page and before the editor's first screen, the
  "Where is Stat Fick?" screen included), at every start; not before the crash window. The logo: BRUTAL's own
  `docs/branding/LOGO_BRUTAL_WHT.png` and `LOGO_BRUTAL_BLK.png` (712 × 168, RGBA), in the BRUTAL source in the
  build cache (its README shows them as the logo); not in any BRUTAL package (those carry only a 32 px icon). The
  licence names ahwoo.com/brutal-branding as the logo's source: step 4 checks the repo's files against that page
  and stops to ask if they differ. Embedded in the engine, shown unaltered except for scaling. **Its look (Q29):** the
  white logo (`LOGO_BRUTAL_WHT.png`) centred on the dark ground of the crash window's look (Q23), at its own size
  (scaled down only to fit the window), nothing else on it, for 2 seconds at every start of both programs; a key or a
  click skips it after its first second. Tests' applications don't show it (an option the programs leave on).
  **The file (the user, 2026-11-08, step 4's question):** the branding page's current 4096 × 896 white logo, drawn
  696 CSS px wide (the size Q29 was decided with: the earlier 712 × 168 file's artwork) and scaled down further
  only to fit the window, with Chromium's mipmapped downscaling ported first ("Decided (the logo's file)").
- **The credits (2.1):** one engine view of the credits used by both programs: the program and its version,
  BRUTAL's copyright notice ("BRUTAL game development framework is Copyright (c) 2024 Ahwoo Limited.") and its
  `NOTICE.txt` text verbatim, FreeType's required credit ("Portions of this software are copyright © <year> The
  FreeType Project (https://freetype.org). All rights reserved.", FTL section 2), and each other component with
  its licence and copyright line. **Its list and place (Q30):** the program's name and version; "Built with BRUTAL",
  BRUTAL's copyright line and `NOTICE.txt` verbatim; every component with its licence and copyright line (FreeType
  with its credit line, GLFW, OpenAL Soft under the LGPL with where its source is, HarfBuzz, stb, VMA, zlib, libpng,
  gli, KTX, .NET, the fonts Lato and STIX Two under the OFL); and a pointer to `licenses/`. The editor opens it from
  an "About" button in its top bar (a dialog; the top bar changes, so all 19 editor screens are captured again), the
  sandbox from an "About" button on its page.

### Logs (M12-8; Q21)

- At the start of `Run()`, once the per-user folder is known (`ApplicationOptions.GameFolder` or this machine's),
  the program opens `logs/<program>-<date-time>.log` there, `stat-fick-2026-11-08_14-03-22.log` and
  `statventure-editor-…` (the program's Linux name, local time, no colons for Windows; the stand-in logs as the
  game), and writes its first lines: the program, its version and commit, the system and .NET, then what it logs
  today (the game folder, the mounts, the window). Every line is flushed as written, as now.
- The last 10 runs are kept per program, the game's and the editor's counted apart; the last 10 crash files are
  kept; older ones are removed when a program starts, only files of those name forms; nothing else in `logs/` is
  touched.
- Tests' applications write no log unless a test asks for one (the redirect may not hold on Windows,
  "Windows check", item 7). Scripts and sessions that run the programs pass a scratch per-user folder.

### Crashes (M12-10; Q22–Q25)

- **What counts:** an exception out of the start (the data's mount, the window, the device, the sound), `Load`,
  `Update`, `Render` or `Unload`; an unhandled exception on any other thread. Not a crash, as now: a UI
  listener's or script task's exception (logged, the rest runs), the audio thread's rendering failure (logged,
  silence), a failed save write (logged, `WriteFailed`).
- **Then:** (1) the crash file, `logs/crash-<date-time>.txt`: the program, version and commit, the time, the
  exception with its stack (file and line, Q7), the log's last lines; written first, so it exists whatever
  follows; (2) the application torn down as today (the pending saves written, S9-B); (3) the crash window: a new
  window, device, swapchain and UI host with a small document and a loop of its own, no data, no sound, no
  storage, no splash; (4) the program ends with a non-zero exit code.
- **The crash window (Q22, Q23):** a heading, "Stat Fick stopped because of an error" ("Statventure Editor stopped
  …"), one line naming the error (its kind and message), "The details are in:" and the crash file's full path in a
  read-only field, selected for copying, with a Copy button; then "Open logs folder" (the system's file manager on
  `logs/`, as the game's "Open game folder"; not a dialog) and "Close"; in English; in the editor's dark look (P4's
  colours) for both programs, a small fixed window, its text in the engine's embedded Lato, so it draws even when
  the game's packs are damaged or missing.
- **Another thread's crash:** the crash file at once; the main loop is woken and shows the crash window on the
  main thread (GLFW's windows live there); if the main loop doesn't answer in time, the process ends with the
  crash file written.
- **No window possible (Q24)** (no Vulkan driver, no display, the crash window failing itself): the crash file,
  its message also on the terminal (seen when started from one); the program ends with an error code. The
  release's text names what the system needs.
- **A fault .NET can't catch (Q25)** (a native library's crash, a stack overflow): the run's log as written up to
  it; nothing else.
- The crash path is testable in-process: the window hidden, the exit and the launcher injectable.

### The engine's embedded Lato (Q17, Q23)

One engine resource: Lato regular and bold (the faces the editor uses, P4; the bytes `ui.pak` holds), with their
OFL text, embedded in the engine's assembly and given to any document by the engine (as a mount or a font
library), so every single-file program carries it. The crash window, the splash's and credits' text and the
editor's whole UI use it; the editor no longer takes its fonts from the game's `ui.pak` (Q17), so it can draw its
first screen before it knows the game. It is made before the crash window and the editor's screen (step 4).

### The editor on its own (M12-1, M12-9; Q16–Q20)

- **Finding the game (Q16):** on its first start, and whenever the folder it remembers no longer holds the game
  (for example after a new version was unpacked beside the old), the editor shows "Where is Stat Fick?" with its
  file browser (M11-9); the player picks the game's folder (the one holding `stat-fick` or `Stat Fick.exe` and
  `packs/`); the editor remembers it in `editor/` (G6). `--game <folder>` still overrides, and the dev setup's
  `--repo` stays.
- **Another version (Q18):** a game folder whose version isn't the editor's is refused, the editor naming both;
  the game's version read from its program file (no extra file).
- **The data library (M12-9, Q19):** the editor loads `Statventure.Game.Data.dll` from the game's folder (built
  against the same engine, hence Q18) and takes its record kinds; until the port the sandbox carries a stand-in
  library of that name (`Samples/Statventure.Game.Data`, one test kind); the editor's own download has none.
- **Ending by itself (Q20):** `--capture <file.png> --frames N`, as the sandbox's, in its usage text.
- Its log in the same per-user `logs/` (M12-8); its title with the version (Q3); its credits view (Q30).

### Releases (M12-7; Q14, Q15, Q26, Q27)

- `Tools/Scripts/release.sh` on the PC (where `gh` is logged in) refuses unless the work tree is clean and its
  commit is on GitHub's `main`, and runs no tests (Q27); computes the version (Q1, Q2); runs the publish for all
  four archives and `check-packages.sh` on each; then `gh release create v2026.11.8.1 --draft --target <commit>
  --title "Stat Fick 2026.11.8.1" --notes-file …` with the four archives (Q26), and prints the draft's address. The
  user reads the draft, may edit its text, and publishes it; publishing makes the tag at that commit. The repo is
  private, so the release is too.
- **The release's text (Q12, Q13, Q26)** is fixed, filled in with the version: what each archive is; how to start
  each (Linux: unpack, run `stat-fick` or `statventure-editor`; Windows: unzip, run the `.exe`, unsigned, so
  SmartScreen's "More info, Run anyway", Q14); what the system needs (a Vulkan 1.3 GPU driver; Linux x64 with
  glibc 2.34 or later; Windows 10 or 11, x64); where the player's folder and the logs are; that the editor needs
  the game and asks where it is on its first start; and that the game's download is the engine's sandbox standing
  in for Stat Fick until the port.

### What packaging never touches (M12-11)

The player's per-user folder (`settings.json`, `saves/`, `mods/`, `editor/`, `logs/`) is the player's: the
archives hold none of it, the scripts never read or write it, unpacking a new version never reaches it, and
every check runs the programs on a scratch per-user folder.

## Decided (Q1–Q28, the user, 2026-11-08)

The design choices this doc raised, asked with the question tool after it was written. Each with its options in
plain words (the one first was the recommended one) and the user's answer; every answer the recommended option
except **Q1, Q11 and Q12**.

### Versions

- **Q1. The version's exact form.** (a) `2026.11.8`, a second release the same day `2026.11.8.2`. (b) `2026.11.08`
  as text, the version number `2026.11.8`. (c) Always four numbers, `2026.11.8.1` for the first release of the
  day. **Answer: (c), not the recommendation (a):** always four numbers, `2026.11.8.1`; a build from the repo that
  isn't a release says `dev` (and its commit is in the log).
- **Q2. Which date.** (a) The UTC date of the commit released, the fourth number counting that date's releases.
  (b) The day the release script runs. **Answer: (a).**
- **Q3. Where the version and the commit show.** (a) The editor's title; the version and commit in the first
  lines of every log and crash file; the game's title stays its name. (b) Both titles. (c) Logs and crash files
  only. (d) As (a), with the commit in the editor's title. **Answer: (a).**

### The downloads

- **Q4. The archives' names and the folder inside.** (a) `stat-fick-<version>-linux.tar.gz`, `-windows.zip`,
  `statventure-editor-…` likewise, each holding a folder named with the version: a new version unpacks beside the
  old. (b) The folder inside named for the program only, a new version unpacked over the old. (c) No folder
  inside. **Answer: (a)**, so `stat-fick-2026.11.8.1-linux.tar.gz` holding `stat-fick-2026.11.8.1/`.
- **Q5. The program's files.** (a) Single-file executables, the native libraries beside them, the data library a
  file of its own. (b) The usual self-contained folder of about 200 files. (c) Single-file with the natives inside,
  unpacked into a temporary folder. **Answer: (a).**
- **Q6. Windows: a console window or not.** (a) Both GUI programs, no console; the editor's command-line problems
  in a plain window like the crash window (and on Linux in the terminal). (b) Both console programs. (c) The game
  without, the editor with. **Answer: (a).**
- **Q7. Debug symbols.** (a) Embedded: crash stacks name file and line, paths from the repo's root. (b) Separate
  `.pdb` files. (c) None. **Answer: (a).** The native `libHarfBuzzSharp.pdb` is left out.
- **Q8. Linux: .NET's globalization.** (a) Invariant mode for both programs and the tests, the full suite shown to
  pass in it. (b) The system's ICU. (c) ICU shipped. **Answer: (a).**
- **Q9. Windows: the Visual C++ runtime.** (a) Its three DLLs beside the program (local deployment), kept in the
  build cache. (b) Required of the player. (c) Microsoft's installer in the zip. **Answer: (a).**
- **Q10. The licence files.** (a) `NOTICE.txt` and a `licenses/` folder, each text as its package gives it. (b) One
  joined `THIRD-PARTY-NOTICES.txt`. **Answer: (a).**
- **Q11. The BRUTAL licence's logo and credits.** (a) `NOTICE.txt` now, the logo and credits at port time, the
  packages not shared beyond the user meanwhile. (b) Also a BRUTAL logo splash and an About view in each program
  now. (c) Ask Ahwoo for written permission. **Answer: (b), not the recommendation (a):** a logo splash and an
  About/credits view in both programs now (the sandbox stand-in and the editor; the ported game gets them again in
  its own screens later): "The BRUTAL logo and the credits" above; their details are Q29 and Q30 below.
- **Q12. A readme in each download.** (a) A short `README.txt`. (b) Only the release's text on GitHub. (c) None.
  **Answer: (b), not the recommendation (a):** no readme in the downloads; the release's text carries what the
  readme would have said (Design, "Releases").
- **Q13. Saying that the game's download is the stand-in.** (a) Its title "Statventure Sandbox" and a note in the
  release's text. (b) Also a line on its page. (c) Nothing beyond its title. **Answer: (a)** (the note in the
  release's text only, as there is no readme, Q12).
- **Q14. Signing the Windows programs.** (a) Unsigned in M12, looked at again when the game goes public. (b) A
  certificate now. **Answer: (a).**
- **Q15. Where the Windows archives are built.** (a) On Linux with the rest, the `.exe` without Windows version
  details (NETSDK1074). (b) On the Windows PC. (c) GitHub's Windows runners. **Answer: (a)**, the limit recorded:
  no version resources in the `.exe` for now, and icons later need a Windows build or a resource step.

### The editor

- **Q16. How the separately downloaded editor finds the game.** (a) It asks, "Where is Stat Fick?" with its file
  browser, on its first start and when the remembered folder no longer holds the game; remembered in `editor/`;
  `--game` and `--repo` stay. (b) As (a), looking in the usual places first. (c) The command line only.
  **Answer: (a).**
- **Q17. The editor's own fonts.** (a) Lato regular and bold inside its program. (b) An `editor.pak` beside it.
  (c) Its own Lato only for the first screen. **Answer: (a)**; with Q23 (a) the two share one engine resource,
  which every single-file program carries ("The engine's embedded Lato").
- **Q18. A game folder of another version.** (a) Refused, naming both versions. (b) A warning. (c) No check.
  **Answer: (a).**
- **Q19. The data library's loading.** (a) Now, with a stand-in `Statventure.Game.Data` library (one test kind) in
  `Samples/`, carried by the packaged sandbox. (b) At port time. **Answer: (a).**
- **Q20. A packaged editor ending by itself for checks.** (a) `--capture <png> --frames N`, as the sandbox's. (b) An
  environment variable for checks. (c) No option. **Answer: (a).**

### Logs and crashes

- **Q21. The logs' names and how many are kept.** (a) `stat-fick-2026-11-08_14-03-22.log` in local time, the last 10
  runs per program, the last 10 crash files. (b) 10 in all. (c) Crash files never removed. **Answer: (a).**
- **Q22. What the crash window says and offers.** (a) A heading, one line naming the error, the crash file's path
  with Copy, "Open logs folder" (the system's file manager, not a dialog) and Close. (b) Without "Open logs
  folder". (c) Plus the stack trace. **Answer: (a).**
- **Q23. The crash window's look and font.** (a) The editor's dark look for both, Lato embedded in the engine. (b)
  The game's parchment look for the game. (c) Lato from `ui.pak`, no window when it can't be read. **Answer: (a).**
- **Q24. When no crash window can be drawn.** (a) The crash file and a terminal message. (b) A Windows message box
  as a last resort. **Answer: (a).**
- **Q25. A crash .NET can't catch.** (a) The log only. (b) A crash file at the next start. (c) As (b) with the
  crash window. **Answer: (a).**

### Releases and checks

- **Q26. The release's tag, title and text.** (a) Tag `v<version>`, title "Stat Fick <version>", a fixed text the
  user can edit in the draft. (b) Tag `stat-fick-<version>`. (c) As (a), listing the commits. **Answer: (a)**:
  `v2026.11.8.1`, "Stat Fick 2026.11.8.1".
- **Q27. What the release script checks.** (a) A clean tree and the commit on GitHub's `main`, no test run. (b)
  The full suite too. (c) No checks. **Answer: (a).**
- **Q28. Where the package checks run.** (a) `check-packages.sh`, run by the packaging steps and before a release,
  not in `test.sh`. (b) All in `test.sh`. (c) The Linux packages in `test.sh`. **Answer: (a).**

## Decided (Q29–Q30, the user, 2026-11-08), from Q11's answer

Raised by Q11's answer and asked with the question tool the same day; both answers the recommended option. Steps
4–6 use them (step 4 builds both pieces, step 5 shows the splash, steps 5 and 6 open the credits).

- **Q29. The BRUTAL splash: its look, length and skipping.** (a) The white logo (`LOGO_BRUTAL_WHT.png`) centred on
  the dark ground of the crash window's look (Q23), at its own size (scaled down only to fit the window), for 2
  seconds at every start of both programs; a key or a click skips it after its first second; nothing else on it.
  *Recommended:* legible and unaltered as the licence asks, short, and in the look the engine already owns. (b) The
  same, never skippable. (c) The black logo on a light ground (the game's parchment), 2 seconds, skippable after
  the first. **Answer: (a)**: the white logo centred on the dark ground at its own size, 2 seconds at every start of
  both programs, a key or a click skipping it after the first second.
- **Q30. The credits view: where each program opens it, and what it lists.** (a) One engine view for both: the
  program's name and version; "Built with BRUTAL", BRUTAL's copyright notice and `NOTICE.txt` text verbatim; then
  each component with its licence and copyright line (FreeType with its required credit, GLFW, OpenAL Soft under
  the LGPL with where its source is, HarfBuzz, stb, VMA, zlib, libpng, gli, KTX, .NET, the fonts Lato and STIX Two
  under the OFL); and that the full texts are in `licenses/`. The editor opens it from an "About" button in its top
  bar (a dialog: every editor screen is captured again, the top bar changing), the sandbox from an "About" button
  on its page. *Recommended:* one list kept beside the licence files, each program offering it where its own
  commands are. (b) The same view, opened with F1 in both instead of buttons (no top bar change). (c) As (a), but
  the sandbox shows the credits as a section at the foot of its page instead of a button. **Answer: (a)**: one engine
  credits view for both, an "About" button in the editor's top bar (its 19 screens captured again) and on the
  sandbox's page.

## Decided (the logo's file, the user, 2026-11-08), from step 4's check

Step 4 checked BRUTAL's `docs/branding/LOGO_BRUTAL_WHT.png` against ahwoo.com/brutal-branding, as the design asked, and
stopped: the files differ. **The branding page** is a 301 to `github.com/ahwootech/brutal-branding`, a repository of
three files: `LOGO_BRUTAL_WHT.png`, `LOGO_BRUTAL_BLK.png` and `NOTICE.txt` (the same as every BRUTAL package's), no
licence or terms of its own. Its logos were replaced on 2026-10-21 (commit `5d38373a24`, pull request #3, "Logos: replace
with 4096px-wide versions": "too small for the splash screens we're asking people to use them on"): **4096 × 896**,
cropped to the artwork (2 px margins; the ratio 4.24 : 1 became 4.57 : 1), SHA-256 `59768c2ab60fc835af29b1d686e5a76604e959fb4e84adf2bfb7fd26e7355d0c`
(white) and `e6ccd85325925a2ca3989e19568be30e64c067b7bff710d43ed6ed277ed293f1` (black). **The 712 × 168 files** of BRUTAL's
source (and of its README on its `main`, `7a3b72d843`, 2026-10-26) are byte for byte the branding repository's first
version (`69ef8a855e`, 2026-10-15; white `6e259ecd95b6e0cbc52744b59ffeee3646be9ba4d8b640d9864938c49fdb9556`, black
`f5edbde828ac4a8046ae2d93933a52fb3a44fda21191458b6040870925a8e0b8`), the same artwork with 8 px margins. Everything
fetched is kept in `~/.cache/stat-fick-build/reference-src/brutal-branding-2026-11-08/` (the first version in
`at-69ef8a855e/`).

- **The question (asked with the question tool, 2026-11-08).** (a) Embed the page's current 4096 × 896 white logo
  byte for byte, draw it at the size Q29 was decided with (the 712 px file's artwork, about 696 × 152 CSS px) and
  scale it down further only to fit the window, after porting Chromium's mipmapped downscaling of images into the
  engine (the canvas drew images with plain bilinear filtering, which aliases at a 6× shrink where Chromium uses a
  mip level). *Recommended:* the licence's named source as it stands, the approved look, sharp at 1.65. (b) Embed the
  712 × 168 file at its own size (soft when scaled up 1.65×; the record saying it is the page's earlier version).
  (c) The 4096 file at its own size, scaled down only to fit (filling the window's width). (d) Ask Ahwoo
  (brutal@ahwoo.com). **Answer: (a).**
- **The terms of use** are the BRUTAL License v1.0's 2.2 (the page carries none of its own): "The Brutal logo available
  from ahwoo.com/brutal-branding shall be displayed in all copies of the Product prior to commencement of gameplay or
  the Software's primary function, unless permission has been granted in writing by Ahwoo Limited or the Product is
  not capable of displaying graphics. The logo shall be shown unaltered except for scaling, for long enough to be
  legible, and shall be no less prominent than logos for other parties." The splash shows the file unaltered, scaled
  only, for 2 s (Q29).
- **The 712 × 168 files' status:** not used. They stay in BRUTAL's source and its README; the engine embeds only the
  page's current white logo (`Engine/Statventure/UI/Resources/LOGO_BRUTAL_WHT.png`, its SHA-256 above, checked by
  `EngineScreensTests`).
- The prerequisite, Chromium's downscaling, came first as a commit of its own on step 4's branch ("How it works",
  step 4).

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
  cloud needs `zip` in `cloud-setup.sh` or Python's `zipfile`); the expected listings' format and folder; how the
  Visual C++ runtime's DLLs are taken from Microsoft's redistributable (on the PC from the redistributable's files,
  or once from the Windows PC: the step says which, and asks the user if it needs the Windows PC).
- The dev default version in `Directory.Build.props`; whether `EngineInfo.Version` follows the program's version.
- Where the editor remembers the game's folder in `editor/`; how dev versions match in Q18 (`dev` against `dev`).
- The splash's option for tests and how `--capture`'s frame count meets the splash (counted after it, or a larger
  N in the checks).
- A crash trigger for the stand-in only, for the end-to-end check (the sandbox's own option, as its `--capture`),
  and a way for scripts to give the sandbox a scratch per-user folder (`XDG_DATA_HOME`, or a `--player-folder` as
  the editor has).
- The stand-in data library's test kind; how the embedded Lato is handed to documents (a mount or a font library).
- `mutate.py`'s way to mutate a script's rule (a command in place of the test filter).

## Steps

Each step ends as every step does (`Docs/Engine-Plan.md`, "How each step is finished"): its tests, the full
suite on the GPU and on lavapipe, a mutation list covering every rule it adds (each killed or accounted for),
`check-packages.sh` where it makes packages, its Progress line, its "How it works" and Status entry here,
`Docs/Notes.md`. At most four workers at once, each in its own worktree; **only one at a time runs the full
`Tools/Scripts/test.sh` or `mutate.py`**, serialised by the shared lock (from the repo's root, `flock -o
~/.cache/stat-fick-build/claude-work/full-run.lock engine/Tools/Scripts/test.sh`, the same lock for the lavapipe
run and for `mutate.py`, MSBuild's node reuse off, inside a headless `cage`); filtered runs in a worker's own
worktree need no lock. Everything headless (no window on the user's screen), nothing audible, nothing written into
the player's real folders (scratch per-user folders for every program run from a script). Workers don't touch
Engine-Plan's "Current task" (the merge worker does). Steps side by side touch no file in common besides those
docs (each its own entry).

**Order:** **wave 1:** 1, 2, 3 and 4 side by side; **wave 2:** 5 (after 2 and 4), 6
(after 1, 2, 3 and 4; rebased on 5 for one call) and 7 (after 1 and 3) side by side; **wave 3:** 8 (after all). Then the manager ticks M12 and
stops. **The fonts' order:** the engine's embedded Lato comes first (step 4), because the crash window (step 5)
and the editor's whole UI, its "Where is Stat Fick?" screen included (step 6), are drawn in it; the editor's
screen doesn't wait for the crash window or the splash (both step 5): at run time the splash comes before it, and
the tests run without the splash.

1. **The version and invariant globalization** (engine, the editor's title). Independent.
   - Owns: `engine/Directory.Build.props` (the dev default version; `InvariantGlobalization` for every project,
     Q8), a new `Engine/Statventure/Core/ProgramInfo.cs` (the program's name, version and commit, read from its
     program file; `dev` for an unreleased build), `Tools/Editor/Shell/EditorView.cs` (the title, Q3), tests in
     `Tests/Statventure.Tests/Core/` and step 4's `ShellTests` (the title).
   - Must not touch: the record layouts, the editor's document (the title is the window's: no screen captured
     again), the scripts.
   - Finished: a build given `-p:Version=2026.11.8.1` reads that version and the commit; a dev build reads `dev`
     and the commit; the title "Statventure Editor 2026.11.8.1 — <pack>"; the full suite passes in invariant mode
     (if a test needs ICU, the step stops and reports it). Mutations: the reading of each part, the dev text, the
     title's form, the invariant switch.
   - **Done (2026-11-08, "How it works", step 1).** A test needed ICU: `ImportNames.Fit` (the editor's suggested
     names, accents dropped) used `string.Normalize(FormD)`, which leaves the text as it is in invariant mode; the step
     stopped and reported it. **The manager's decision (2026-11-08, (a); Q8 and the editor's rule unchanged, so an
     implementation choice):** `Fit` takes accents off through HarfBuzz's canonical decompositions
     (`UnicodeFunctions.Default.TryDecompose`, repeated, then the non-spacing marks left out), the three tests as
     they were; same results as ICU's FormD on every sample checked; `Tools/Editor/Import/ImportNames.cs` added to
     the step for that edit only. `EngineInfo.Version` stays 0.1.0 (the manager, 2026-11-08: Vulkan's limit).
2. **Logs and crash files** (engine). Independent (its first lines take step 1's `ProgramInfo` once merged: a
   rebase for that line).
   - Owns: `Engine/Statventure/Core/Log.cs`, a new `Core/LogFiles.cs` (folder, names, rotation),
     `Core/Application.cs` (the run's log once the per-user folder is known; the crash file; the handler for other
     threads), one line each in `Samples/Statventure.Sandbox/Program.cs` and `Tools/Editor/EditorApplication.cs`
     (their names for the logs), tests in `Tests/Statventure.Tests/Core/`, `Docs/Assets.md`'s line on the
     per-user folder (`logs/` beside the rest).
   - Must not touch: the window, device and UI (step 5), the save writer's rules (a crash still writes the pending
     saves, S9-B), the tests' redirect.
   - Finished: one log per run, named as Q21, with its first lines; the last 10 per program and 10 crash files
     kept, only those name forms touched; a crash file for each kind of crash with its parts; tests' applications
     write no log unless asked; nothing outside the scratch folder. Mutations: the kept numbers, which files count,
     the names, each part of the crash file, each crash source, the order (the crash file before the teardown).
   - **Decided by the manager (2026-11-08; implementation details the design leaves to the step, asked by the
     step's worker):** (1) the log's program name is set only on the program's own path (the editor's public
     constructor, which its `Main` uses; the sandbox's options); the tests' construction (the editor's internal
     constructor) leaves it unset, so tests write no log unless they ask (not: set in `OptionsFor` with the editor's
     tests writing logs into their scratch folders; not: a switch for the whole process the tests turn off). (2) The
     cutscene player (`Samples/Statventure.Sandbox/Cutscene.cs`, the sandbox's other application) logs under the
     game's name too; its one line is step 2's.
3. **Publishing, the archives and their check; `editor.ps1`** (scripts). Independent.
   - Owns: new `Tools/Scripts/publish.sh`, `publish.ps1` and `check-packages.sh`, a new `Tools/Packaging/` (the
     expected listings, the licence files' list), `Samples/Statventure.Sandbox/Statventure.Sandbox.csproj` and
     `Tools/Editor/Editor.csproj` (`WinExe`, Q6; single-file settings, Q5; embedded symbols with mapped paths, Q7),
     `Tools/Scripts/editor.ps1` (the Windows counterpart of `editor.sh`; "Windows check" item 8d's line updated),
     `cloud-setup.sh` if the cloud needs a tool; the Visual C++ runtime's DLLs in the build cache (Q9).
   - Must not touch: engine code, the editor's code, `build.sh` and `build.ps1`'s dev layout (S9-A), the
     per-user folders.
   - Finished: `publish.sh` makes the four archives (`stat-fick-<v>-linux.tar.gz` …) into `release/` from a clean
     tree; `check-packages.sh` passes on each and fails on each broken rule (a file missing or extra, a name, a
     pack changed, the executable bit, the console subsystem, a licence or runtime DLL missing, a native library's
     need unmet); the unpacked Linux stand-in starts headless, draws and quits (`--capture`) on a scratch per-user
     folder; sizes reported. Mutations: the check's rules (packages broken one way at a time) and the publish's.
4. **The engine's embedded Lato and BRUTAL logo; the splash and the credits as engine pieces** (engine, new
   files). Independent.
   - Owns: new resources under `Engine/Statventure/UI/Resources/` (Lato regular and bold with the OFL text; the
     BRUTAL logo, checked against ahwoo.com/brutal-branding, stopping to ask if the repo's file differs),
     `Engine/Statventure/Statventure.csproj` (the resource lines), new `UI/EngineFonts.cs` (the embedded Lato for
     any document), `UI/Splash.cs` (the splash's document and timing, Q29), `UI/Credits.cs` (the credits view and
     its list, Q30), tests in `Tests/Statventure.Tests/Ui/` with the splash's and the credits view's Chromium
     fixtures.
   - Must not touch: `Core/Application.cs` (step 5 shows the splash), the editor, the sandbox.
   - Finished: the embedded faces load and equal `ui.pak`'s bytes; the splash's timing and skipping (Q29); the
     credits hold BRUTAL's notice and `NOTICE.txt` verbatim, FreeType's credit and every component of the
     packages' licence lists (checked against the licence files); both documents match Chromium within the limits.
     Mutations: each face, the logo, each timing and skipping rule, each required line.
5. **The application's own screens: the splash before `Load`, the crash window after a crash** (engine; the
   sandbox's credits). After 2 and 4.
   - Owns: `Core/Application.cs` (the splash after the window and device are made and before `Load`, an option
     the tests turn off; the crash window after the teardown; another thread's crash brought to the main loop),
     a new `Core/CrashWindow.cs` (Q22–Q24; also a plain message in the same look that any program can show, for the
     editor's command-line problems on Windows, Q6), `Samples/Statventure.Sandbox/Program.cs` (its credits per Q30; its
     `--capture` meeting the splash; a crash trigger of the stand-in's own for step 8), tests in
     `Tests/Statventure.Tests/Core/` with the crash window's Chromium fixture.
   - Must not touch: step 2's crash-file rules, step 4's pieces (only used), the editor.
   - Finished: the splash shown once per start before `Load`, never before the crash window; after each kind of
     crash the window shows the error and the path (hidden window); Copy fills the clipboard; "Open logs folder"
     launches the file manager on `logs/` (caught); Close ends with the error code; the window failing gives the
     terminal message (Q24); another thread's crash shown by the main loop; the crash window matches Chromium.
     Mutations: each.
   - **Done (2026-11-08, "How it works", step 5).** What the step chose where the design left it (none visible beyond the
     design's words): the splash shown only on a run with a log name (the option on by default; tests' applications, the
     editor's tests included, show none); during it, a close ends the run before anything loads and a crash is the start's;
     the crash window 640 × 240, fixed, in the design's wording; after a crash a program ends with exit code 1 through
     `Environment.Exit`, the crash kept and thrown only after the run's end (rethrown at once, .NET reported it unhandled
     before the teardown and the window ran); `--capture` counting the page's frames after the splash; the sandbox's
     `--crash` and `--crash-thread`; its credits view with a Close of its own and Escape. Found, for the UI's owner: a
     focused field's range selection revealed by its caret end where Blink reveals its bounds.
6. **The editor as a download of its own** (editor; the stand-in data library). After 1, 2, 3 and 4.
   - Owns: `Tools/Editor/**` (its UI in the engine's embedded Lato in place of `ui.pak`'s, Q17; the "Where is Stat
     Fick?" screen with the file browser and the remembered folder, Q16; the version check, Q18; loading
     `Statventure.Game.Data.dll` from the game's folder, Q19; `--capture <png> --frames N`, Q20; its credits per
     Q30; the usage text with the new names), its tests and Chromium fixtures in `Tests/Statventure.Tests/Editor/`
     (every editor screen captured again if the top bar changes, Q30 (a)), a new `Samples/Statventure.Game.Data`
     (the stand-in library, one test kind), `Statventure.slnx`, the sandbox's project reference to it, the
     stand-in's expected listing in `Tools/Packaging/`, `Tools/Scripts/editor.sh` if its arguments change,
     `Docs/Editor.md` (its section). It rebases on step 5 for one call only: a wrong command line shown in the crash
     window's plain look (Q6); nothing else waits for step 5.
   - Must not touch: the engine.
   - Finished: the lookup's rules on scratch game folders (none, remembered, gone, another version, `--game`,
     `--repo`); the new screen against Chromium; the stand-in's kinds read from a game folder; `--capture`; the
     credits opened. Mutations: each.
   - **Done (2026-11-08, "How it works", step 6).** Before its code the step asked three questions, which the manager
     answered (2026-11-08; recorded for the user), each the recommended option (a): **Q-A** under `--repo` the data library
     comes from the library project's own build in the repo (`Samples/Statventure.Game.Data/bin/<the editor's
     configuration>/net10.0/`, one constant the port changes), `editor.sh` builds it beside the editor, no reference from
     the editor; **Q-B** the packer references the library's project, so `pack`, `update`, `extract`, `dump` and
     `textconv.sh` know the game's kinds with no option (`Tools/Packer/` added to the step's files); **Q-C** the engine
     keeps one record kind per name for the process, so a run uses one build of the library: Game folder… switches at once
     for the run's build, otherwise says "Start the editor again to work on the game in <folder>" and remembers it. The
     step's own choices were confirmed by the manager as listed ("How it works", step 6). One rule of step 3's check had
     to change, which the step reported: the game's Windows archive failed on the data library's import of `mscoree.dll`,
     which every managed assembly has and .NET doesn't use; **the manager's decision (a), 2026-11-08:** the check skips
     the native-import rule for IL-only .NET assemblies ("How it works", step 3 and step 6, "The packages").
7. **The release script and its text** (scripts). After 1 and 3.
   - Owns: a new `Tools/Scripts/release.sh` (the version from the commit's UTC date and the repo's `v<date>.*`
     tags, Q1, Q2; the refusals, Q27; the publish and `check-packages.sh` for all four; the draft through `gh`,
     Q26; a dry run that uploads nothing), the release's text template in `Tools/Packaging/` (Design,
     "Releases"), its tests (the dry run against a scratch repository with a stand-in `gh` recording its
     arguments).
   - Must not touch: the publish and check scripts (step 3's), any real release (the first real draft is made only
     on the user's word).
   - Finished: the dry run's version for a first and a same-day release (`.1`, `.2`), its refusals (a dirty tree, a
     commit not on GitHub's `main`), the `gh` call with the tag, target, title, text and four archives. Mutations:
     each.
   - **Done (2026-11-08, "How it works", step 7).**
8. **End to end, and the Windows items.** After all.
   - The Linux archives unpacked into scratch folders: both programs started headless on a scratch per-user folder,
     each showing the splash and ending by itself; their logs (one each, version and commit; an eleventh run
     removing the oldest); the stand-in made to crash (its own trigger) gives the crash file and the crash window
     (captured headless); the packaged editor's first start shows "Where is Stat Fick?", and given the unpacked
     game's folder reads the stand-in library's kinds; a mod made against that folder (step 14's in-process flow
     with `--game`) is mounted by the packaged stand-in (its log names it); the credits of both; the Windows
     archives checked; the Windows items added to "Windows check" (both zips unpacked and started from Explorer
     with no console, the names with spaces, the splash, the logs in `%APPDATA%\Stat Fick\logs`, the crash
     window, the editor finding the game, a mod made and loaded, `publish.ps1` and `editor.ps1`, the Visual C++
     runtime on a machine without its redistributable if one is at hand, SmartScreen).
   - Owns: tests or a script of its own for the flow, the "Windows check" items, this doc's Status.
   - Finished: the flow passes on the PC (GPU and lavapipe) and in the cloud's setup if it can run there; then the
     manager ticks M12 in Engine-Plan's Milestones, updates the notes and stops for the user.
   - **Done (2026-11-12, "How it works", step 8).** The flow is a script, `Tools/Scripts/end-to-end.sh`
     (`Tools/Packaging/end_to_end.py`), with one new test of the suite for its in-process part (`GameFolderModTests`, run
     by the script built with the packages' version); `release.sh` doesn't run it (Q27). The step's choices, none visible
     to players, are listed there (a script, not a test project; not in the release; the mod and the editor's About made
     in-process; the other version's library built with `-p:Version`; the remembered folder written by the flow; one
     per-user folder for both programs; pictures judged by their colours). Not run in the cloud (its setup has Xvfb, which
     the script uses where there is no cage). The Windows items are Engine-Plan.md's "Windows check", item 9. Its one
     finding needing a decision, `--game`'s library line missing from the editor's log, was decided by the manager
     (2026-11-12, (a)): the editor logs it once its log is open (`EditorApplication.LogGivenLibrary`). **M12 is done and
     ticked (2026-11-12).**

## How it works

Filled in as the steps are done.

### Step 1: the version and invariant globalization (2026-11-08)

- **The version in the build (`Directory.Build.props`):** `<InformationalVersion Condition="'$(Version)' ==
  ''">dev</InformationalVersion>`, the dev default; a release is built with `-p:Version=2026.11.8.1` (the release
  script, step 7), which also gives every assembly's version and file version (a dev build keeps the SDK's
  1.0.0.0). `-p:Version` is a global property, so the program, the engine and any library of the same build (the
  data library, M12-9) carry the same version. Nothing checks the version's form (Q1's four numbers are the
  release script's to make): the program reads what its build was given.
- **The commit (left to this step; chosen):** from git at build time by the .NET SDK's own Source Link
  (`Microsoft.Build.Tasks.Git`, in the SDK since .NET 8: `InitializeSourceControlInformation` sets `SourceRevisionId`
  from HEAD, `AddSourceRevisionToInformationalVersion` appends `+<full hash>`), so every build carries it, dev builds
  too, and the publish script needn't pass it (a script may: the SDK keeps a `-p:SourceRevisionId` given). It reads
  `.git` itself, no git program needed; worktrees work (their `.git` file). HEAD's commit only: a dirty tree isn't
  marked (a release comes from a clean tree, Q27). **A build without git** (an unpacked source archive: `git archive`
  of `engine/` at the step's commit, built with its own `build.sh` in a scratch folder): 0 errors and the same 141
  warnings as in the repo (the SDK's implicit Source Link doesn't warn without a repository), the engine's, the
  editor's and the sandbox's informational version `dev` with nothing after it, so the program says `dev` and has no
  commit (`ProgramInfo.Commit` null).
- **`Core/ProgramInfo.cs`:** `record ProgramInfo(string Name, string Version, string? Commit)`. `Of(Assembly)` reads
  the assembly's name and its `AssemblyInformationalVersionAttribute`; `Parse(name, informationalVersion)`: the
  version before the first `+` (none, or no attribute: `Dev`), the commit after it (none or empty: null); `Dev` is
  `"dev"`; `IsDev`. A program passes its own assembly (the one holding its `Main`, or its `Application` subclass's,
  `GetType().Assembly`), not `Assembly.GetEntryAssembly()`, which in the tests is the test host. In a single-file
  program the assembly is inside the executable and reads the same. **For the later steps:** step 2 writes `Version`
  and `Commit` in the first lines of the logs and crash files; the credits (steps 4–6) name `Version`; step 6's
  check of the game's version (Q18): `ProgramInfo.Of` works on any loaded assembly (the game's data library once the
  editor loads it), while reading the version out of the `stat-fick` single-file bundle without running it would
  need the bundle parsed (nothing does that).
- **`EngineInfo.Version` stays the engine's own 0.1.0** (left to the steps; confirmed by the manager, 2026-11-08):
  it goes to Vulkan as the engine's version (`GpuDevice`, `VK_MAKE_VERSION`), whose major number has 10 bits (at most
  1023), so 2026 can't go there; and it is the engine's, not the program's.
- **The editor's title (Q3):** `EditorView.Version` (the editor's own assembly's `ProgramInfo.Version`) and
  `EditorView.TitleOf(version, pack, unsaved)`: "Statventure Editor 2026.11.8.1", then " — <pack>" with a pack open,
  "• " first while there is unsaved work; a dev build's "Statventure Editor dev — my-mod.pak". The commit is in no
  title. The editor's document is unchanged: no screen captured again. The window's title is the view's from `Load`
  on; before it, the window is made with `EditorApplication.OptionsFor`'s `EditorView.Name` (also Vulkan's
  application name). **For step 5 and step 6:** once step 5 shows the splash before `Load`, that title would show
  without the version for the splash's 2 s, so `OptionsFor` should take `EditorView.TitleOf(EditorView.Version,
  null, false)` then (the editor's file, step 6's; the assert in `SettingsTests`, line 160, follows).
- **Invariant globalization (Q8):** `<InvariantGlobalization>true</InvariantGlobalization>` in `Directory.Build.props`,
  so every project's runtime configuration has `System.Globalization.Invariant` (and
  `System.Globalization.PredefinedCulturesOnly`): both programs, the tools and the tests (the test host runs with the
  test project's runtime configuration). What it changes: no culture but the invariant one (a named culture throws),
  culture-aware comparisons and searches by code point (`StartsWith(string)`, `EndsWith(string)` and default string
  sorts: in the engine CSS keywords, an SVG filter reference, a diagnostic set; nothing a test or a player sees),
  casing as before (.NET 8 and later case all of Unicode without ICU). **One rule needed ICU:**
  `string.Normalize(NormalizationForm.FormD)` leaves the text as it is, so `ImportNames.Fit` (the editor's suggested
  names, M11 step 12, accents dropped) made accented letters `-`: "Ünïcode Tales!" gave `n-code-tales`, and three
  tests failed (`SessionNewModTests` "Ünïcode Tales!", `ImportKindsTests` "Icône.SVG", `ImportPathTests` "Ärger über
  Öl"). The step stopped and reported it; the manager's decision (a) (Steps, step 1): `Fit` takes each code point
  apart by HarfBuzz's canonical decompositions (HarfBuzzSharp's `UnicodeFunctions.Default.TryDecompose`, repeated;
  HarfBuzz's Unicode data ships with the engine) and leaves out the non-spacing marks, each code point then as
  before (small letters, the rest `-`). Checked in a scratch program in invariant mode against ICU's FormD: Ü, ï,
  ô, Ä, Ö, ü to their letters, ǖ (two levels) to u, Å to A; ß, æ, ﬁ and Ǆ kept, as FormD keeps them (no canonical
  decomposition). (Not chosen: a hand table of Latin letters; accented letters becoming `-`; ICU again.) Nothing
  else in the suite needed ICU.
- **Tests (`Tests/Statventure.Tests/Core/`):** `ProgramInfoTests`: the parse (9 cases), `IsDev`, the tests' own build
  a dev build of the repo's commit (`git rev-parse HEAD`, so a commit made between the build and the run fails it;
  `test.sh` and `mutate.py` build first; the tests', the editor's and the engine's assemblies),
  the tests running invariant (the switch, the current culture, `de-DE` refused, comparison by code point), the
  editor's runtime configuration (copied beside the tests'). `ProgramBuildTests`: a scratch program built with the
  repo's props (`-p:DirectoryBuildPropsPath`) by the SDK the tests run on (`--disable-build-servers`, git's and
  MSBuild's variables of the run taken out, nothing left running) in a scratch git repository: `-p:Version=2026.11.8.1`
  reads that version and the commit, `2026.11.8.2` likewise, no version `dev` and the commit; a copy without git
  `dev`, and the release's version, with no commit; every runtime configuration invariant (a few seconds).
  `ShellTests` (step 4's): the title with the version (`TitleOf` with a release's and with `dev`, the live view's dev
  titles), the editor's version its own assembly's. The title's text in five other editor tests (`NewModViewTests`,
  `SavingViewTests`, `OpenerTests`, `ModFromScratchTests`, `DevSetupTests`).

### Step 2: logs and crash files (2026-11-08)

- **The option (`ApplicationOptions.LogName`, in place of the unused `LogFile`):** the program's name for its logs.
  `stat-fick` in the sandbox's options (`Program.cs`) and the cutscene player's (`Cutscene.cs`), the stand-in logging
  as the game; `statventure-editor` given by the editor's public constructor only, which its `Main` uses (the
  internal one the tests use leaves it null; the manager's decisions, Steps, step 2). Null, the default: the console
  only, no log file, no crash file, no terminal message; a test that wants a log sets a name and its own game folder.
- **The run's log (`Core/LogFiles.cs`, `Start`):** `Run()` first finds the game folder (`ApplicationOptions.GameFolder`,
  else this machine's), then, with a name, opens `<game folder>/logs/<name>-yyyy-MM-dd_HH-mm-ss.log`, local time (the
  run's `DateTimeOffset.Now`): each missing folder level made 0700 on Linux (`StorageFiles.CreateFolder`), the file
  made new (`FileMode.CreateNew`, 0600 on Linux, as the game folder's other files), UTF-8 without a byte order mark,
  every line flushed (`Log.OpenFile(stream, path)`). **A name already taken** (two starts in one second) gets `-2`,
  `-3` … before `.log` (chosen: never overwritten; rare enough not to be a name the player meets). **The first lines**
  (category `Program`): `stat-fick 2026.11.8.1, commit <full hash>` (the program's log name, then `ProgramInfo.Of`
  of the application class's own assembly, step 1's: `dev` for a build that isn't a release, `commit unknown` for a
  build without git), `started 2026-11-08 14:03:22 +01:00`, and the system: `<OS description> (<OS version>)
  <architecture>, .NET 10.0.13` (on Linux .NET's OS description is the distribution's name, "Bazzite", so the
  kernel's version follows in brackets: "Bazzite (Unix 7.2.15.1) X64, .NET 10.0.13"). Then the lines as before: the
  game folder, the mounts, the window, the GPU's name. **A log that can't be made** (the folder can't be made, a
  file in its place) is a warning on the console ("no log file in …") and the run goes on; **a log that can't be
  written any more** (a full disk) is dropped with one line on standard error and the console goes on (chosen: a
  log never stops the program).
- **Which logs are kept (`Prune`, at each start, after the first lines):** of `<name>-<stamp>[-n].log` the run's own
  log and the 9 newest others, so the last 10 runs of that program (the game's and the editor's counted apart); of
  `crash-<stamp>[-n].txt` the 10 newest, so a crash in this run makes 11 until the next start (removed "when a program
  starts", as the design says). Newest by the stamp in the name, then by the number after it (as a number: `-12`
  after `-3`), not by the files' times. Only files of those exact forms count (anchored patterns, the program's name
  exactly, ASCII digits, files only: `stat-fick.log`, `stat-fick-notes-….log`, `….log.bak`, `crash-old.txt`,
  `Stat-Fick-….log`, a folder with a log's name are all left alone). A file that can't be removed (on Windows another
  running copy's open log) stays, with a warning; a folder that can't be listed, a warning. The run's own log is
  never removed, even when older runs are dated later (a clock set back).
- **What counts as a crash, and the order:** an exception out of the start (once the log is open: the storage, the
  data, the window, the device, the sound), `Load`, `Update`, `Render`, `Unload`, or the teardown (`Dispose`; chosen:
  a teardown that fails is a crash too, and after a crash it is logged as another); an unhandled exception on any
  other thread while the application runs. Not crashes, as before: a UI listener's or script task's exception, the
  audio thread's rendering failure, a failed save write; nor a faulted `Task` (its exception stays in the task, .NET's
  default, `TaskScheduler.UnobservedTaskException` not used). **The order (`Run`):** (1) the crash file, at once, in
  `Run`'s catch before the teardown (`Crashed`), so a failing teardown can't lose it; (2) the teardown, `Dispose`, as
  before (the pending saves written, S9-B; the log closed; `Dispose` closes the log only if this application opened
  it, so another application ending, a test's, leaves a program's log open); (3) the run's end, `Ended` (even after a
  failing teardown): threads waiting are let go, the terminal message (Q24); (4) the exception leaves `Run()`. The
  programs' `Main` doesn't catch it: .NET prints it and ends the process (SIGABRT, 134 on Linux), as before; step 5
  adds the crash window between (3) and (4). The run's first crash is its crash (`Application.Crash`) and writes
  the file; a later one is only logged ("another crash, …").
- **The crash file, `logs/crash-yyyy-MM-dd_HH-mm-ss.txt`** (local time, `-2` … when taken; written whole, flushed to
  the disk, 0600): line 1 the program line; line 2 `crashed 2026-11-08 14:05:10 +01:00 <where>` (`on the main
  thread`, `on the thread "Statventure audio rooms"`, `on a thread-pool thread`, `on an unnamed thread`); line 3 the
  system; line 4 `log <the run's log>`; a blank line; the exception's `ToString()` (its type, message and stack with
  files and lines in a build with symbols, its inner exceptions); a blank line; `The log's last lines:` and the run's
  log up to the crash, at most its last 100 lines (chosen: `Log` keeps its last 100 lines since its file opened,
  `Log.Tail()`; taken before the crash's own error line, which repeats the stack). Then the log gets `crashed <where>:
  <exception>` and `crash file <path>`. A crash file that can't be written is an error in the log; the crash goes on
  without one (`Crash.File` null).
- **Other threads (the handler):** .NET 10's `ExceptionHandling.SetUnhandledExceptionHandler`, set once per process
  at the first `Run()`, is called for an exception no code caught on any thread but the main one (the thread pool
  too; the main thread's are `Run`'s); answering true lets that thread end and the process go on (checked in a
  scratch program first: a thread's and a pool thread's, a blocking handler, the exception rethrown on the main
  thread). It hands the crash to the running application (the first whose `Run` is going on; none: false, .NET's
  own way), which writes the crash file at once and hands it to the main loop: the loop takes it at the top of its
  next iteration and throws it there (`ExceptionDispatchInfo`), so the run ends as for its own crash, in the same
  order (the teardown, the pending saves); a minimized loop asleep in `WaitEvents` is woken (`Window.PostEmptyEvent`,
  callable from any thread). The thread waits until the loop has taken it or the run has ended (true), at most
  `CrashAnswerTime`, 10 s (chosen); a loop that doesn't answer in time (stuck, or in a long `Load`): the terminal
  message and false, so .NET ends the process at once, the crash file written. A crash that arrives after the loop
  (in `Unload` or the teardown) ends the run all the same (`Ended` throws it when the run had no crash of its own).
  The audio's warm-up and room-preparing threads, the thread pool and any other thread are covered alike. **The step
  split:** bringing another thread's crash to the main loop is listed under step 5 ("Steps"); it is done here, because
  ending the program in order (the teardown on the main thread, the pending saves) needs it; step 5 adds only the
  window after the teardown, for both kinds of crash.
- **What step 5 reads:** `Application.Crash`, a `CrashReport` (`Core/LogFiles.cs`): `Exception`, `Where`, `Time`,
  `File` (the crash file's full path; null without a log name, or when it couldn't be written), `Message` (the one
  line naming the error, its kind and message: "InvalidOperationException: …", Q22) and `TerminalText(program)`
  ("<program> stopped because of an error." / the message / "The details are in: <file>", the crash window's words,
  Q22, Q24). The terminal message is printed once in `Ended` (`TellTheTerminal`), with a log name only, the window's
  title as the program's name (the stand-in says "Statventure Sandbox stopped because of an error."), to standard
  error (`Application.Terminal`, internal, which the tests catch): step 5 shows the window there instead and keeps
  this for when no window can be drawn. `CrashAnswerTime` is internal too (the tests' shorter).
- **A fault .NET can't catch (Q25):** the run's log as written up to it (every line flushed); nothing else.
- **Scripts and sessions:** the sandbox (`play-cutscene.sh`, its `--capture` runs) now writes `logs/` in the per-user
  folder; a script or session that runs it sets `XDG_DATA_HOME` to a scratch folder (the editor: `--player-folder`).
- **Tests (`Tests/Statventure.Tests/Core/`, the non-parallel "Storage log" collection, the log at Info while they run:
  the GPU tests' fixture leaves it at Warning):** `LogFilesTests` (12): the names; the run's log and its first lines
  (and modes 0700/0600, no byte order mark); a start keeping the game's last 10 runs, the editor's all, the 10 newest
  crash files and every other name form (a folder of 12 runs of each program, 12 crash files and 17 other names,
  made shuffled with file times the reverse of the names', same-second numbers at the cut); the editor's start; the
  run's own log kept against later dates; a taken name numbered, nothing overwritten; a crash file written whole; a
  folder that can't be made or listed; a crash file's exact text; the crash's message and terminal words; the log's
  last 100 lines; a log that can't be written. `ApplicationCrashTests` (21; real applications in hidden windows, a
  game folder of the test's own; those that run many updates minimized with immediate wake-ups, as a hidden window's
  presents hang after a few frames under wlroots): the run's log in the player's folder with its first lines; tests'
  applications writing nothing (nor in this machine's redirected folder); a log that can't be made; an application
  closing only its own log; the editor's name on the program's path only; a crash in the start, `Load`, `Update`,
  `Render` and `Unload` (each: the crash file's parts in order against the run's log, written before the teardown,
  the log's lines, the terminal message); a failing teardown after a run and after a crash; a crash on a thread of its
  own, a thread-pool thread and the audio's room thread (a room that can't be prepared), each taken by the next
  update, the pending save (held 1.5 s) written and the thread let go when the loop takes the crash, not after the
  teardown (an answer time of 1 s, shorter than the teardown); a minimized loop woken; a loop that doesn't answer in time
  (told at once, false); a crash during the teardown, with and without a crash before; after the run (not the
  application's, and the next application's: an unnamed thread); a crash file that can't be written.

### Step 3: publishing, the archives and their check; `editor.ps1` (2026-11-08)

- **Commands.** `Tools/Scripts/publish.sh [--version 2026.11.8.1] [--no-check] [<package> ...]` makes
  `stat-fick-<v>-linux.tar.gz`, `stat-fick-<v>-windows.zip`, `statventure-editor-<v>-linux.tar.gz` and
  `statventure-editor-<v>-windows.zip` (all four, or the packages named: `stat-fick-linux`, `stat-fick-windows`,
  `statventure-editor-linux`, `statventure-editor-windows`) in `release/` at the repo's root (git-ignored), then runs
  `check-packages.sh` on them; without `--version` the version is `dev` (no `-p:Version`, so step 1's dev default).
  About 20 s for all four on the PC once the packages are in the NuGet cache (the first Windows publish downloads the
  Windows runtime pack). `Tools/Scripts/check-packages.sh [--no-run] <archive>...` or `--version <v>` (the four in
  `release/`); `--self-test <archive>...`; `--listing <archive>` (prints the archive's listing). Windows:
  `Tools\Scripts\publish.ps1 [-Version <v>] [<package> ...]` (no check there). Neither touches build.sh's dev layout
  (`$STATVENTURE_GAME`) or the per-user folders.
- **The publish, `Tools/Packaging/Publish.cs` (left to the step; chosen):** a .NET 10 file-based program (`dotnet run
  Tools/Packaging/Publish.cs -- --engine <engine> --version <v> --time <commit time> [--vc-runtime <folder>]
  <package>...`), so publish.sh and publish.ps1 share one layout and one archive writer (tested here; only the
  PowerShell around it is untested); the scripts compile the shaders first, as build.sh does (a clean tree has
  none), and fetch the Visual C++ runtime. It refuses a version not of Q1's form. Per package: `dotnet publish <the
  program's project> -c Release -r linux-x64|win-x64 -p:DebugType=embedded -p:ContinuousIntegrationBuild=true
  [-p:Version=<v>] --artifacts-path release/.publish/artifacts -o release/.publish/<package>/<program>-<v>`; the
  executable renamed to M12-2's name (`Statventure.Sandbox` → `stat-fick` and `Stat Fick.exe`, `Statventure.Editor` →
  `statventure-editor` and `Statventure Editor.exe`; renaming rather than naming the assembly per platform, as
  proposed; the single-file host starts the program it carries whatever it is called: the renamed stand-in runs);
  every `.pdb` deleted (only the native `libHarfBuzzSharp.pdb`, 23 MB, is there: the managed symbols are embedded);
  the game's `packs/` made `Assets/Packs/*.pak` exactly (the sandbox's project copies `audio.pak`; that folder is
  dropped first), none for the editor; `NOTICE.txt` and `licenses/` from `Tools/Packaging/`; on Windows the three
  Visual C++ runtime DLLs; each licence text checked against the package it came from; the archive written. The work
  folder `release/.publish` is removed before and after.
- **Symbols (Q7):** `DebugType=embedded` and `ContinuousIntegrationBuild=true` (the SDK's DeterministicSourcePaths:
  every source path under git's root becomes `/_/…`, `/_/engine/Engine/Statventure/Core/Application.cs`) go on the
  command line, as global properties, because the engine's assembly is built by its own project, whose settings
  aren't the program's. **Found:** the files the build generates (`AssemblyInfo.cs`, global usings, the source
  generators' files) live in the intermediate folder, and with that outside the repo (a first try in the build cache)
  their full machine paths stayed in the symbols; so the work folder is inside the repo, `release/.publish`, and they
  read `/_/release/.publish/artifacts/obj/…`. The check reads every embedded PDB's documents.
- **The program projects** (`Statventure.Sandbox.csproj`, `Editor.csproj`): a property group for a runtime publish
  only (`'$(RuntimeIdentifier)' != ''`): `SelfContained` (M12-3), `PublishSingleFile` with
  `IncludeNativeLibrariesForSelfExtract` false (Q5: the natives beside it), `PublishTrimmed` false (M12-3), `OutputType`
  `WinExe` for a `win-*` runtime (Q6: the GUI subsystem, no console). Builds without a runtime are as before: build.sh's
  dev layout (framework-dependent, every runtime's natives, S9-A), the tests (the test project references the editor's
  project), editor.sh, play-cutscene.sh; a dev build on Windows keeps its console.
- **What a single-file program holds:** 210 files inside the executable (about 80 MB): the program's assembly and its
  `.deps.json` and `.runtimeconfig.json`, `Statventure.dll`, BRUTAL's 19 assemblies, 13 `Microsoft.Extensions.*`
  (BRUTAL's logging), `CommunityToolkit.HighPerformance.dll`, `HarfBuzzSharp.dll` and .NET's 172 libraries; the same
  210 on Linux and Windows. .NET's own native parts (`System.Native`, the cryptography shim, compression with Brotli
  and zlib-ng) are linked into the host: nothing of .NET lies beside it (the doc's "Native libraries" listed
  `libSystem.*.so` for self-contained; single-file has none). Beside it: BRUTAL's and HarfBuzz's natives only.
- **The archives (left to the step; chosen):** made by `Publish.cs`, so neither `zip` nor Python is needed to make
  them (the cloud's setup gets no `zip`). `.tar.gz`: .NET's `TarWriter` (ustar), entries sorted, owner and group 0
  with no names (numeric owner 0), mode 755 for folders and every ELF file (the program and the native libraries),
  644 for the rest (not "kept modes": NuGet unpacks the natives 744, and modes from the file system would depend on
  the machine), gzip at its smallest. `.zip`: .NET's `ZipArchive`, entries sorted, folders as entries, no Unix modes.
  Both dated at the released commit's time (`git log -1 --format=%ct`), so one commit gives the same archives, byte
  for byte: two publishes of one commit in the worktree, and a fresh clone's publish against the worktree's, compared
  (once Source Link was off: before, the SDK's Source Link put an entry naming the git remote in the embedded symbols,
  `{"documents":{"/_/*":"https://raw.githubusercontent.com/Vatrix33/statfick/<commit>/*"}}` from a clone of the GitHub
  repo, none from a clone of a local path, the only difference between the two; "The repo's address" below).
- **The Linux soname copies (left to the step; kept):** each library under every name its package gives
  (`libfreetype.so`, `.so.6`, `.so.6.20.6`; `libglfw`, `libopenal`, `libpng18`, `libzlib` likewise), as build.sh's
  dev layout has them: no knowledge needed of which name BRUTAL's loader asks for (it can change with a BRUTAL
  update). Their cost: 8.1 MiB unpacked, 1.8 MB in the `.tar.gz`. (Symbolic links would save that, but not every
  archive tool or file system keeps them.) The listings record them.
- **No `classes/`:** the brief named `packs/` and `classes/`; the design says `classes/` comes with the port (M9
  Q27, Q31: the port makes the shipped folder) and the repo has no source for it, so neither package has one yet.
  When the port adds it, `Publish.cs` copies it beside the game's program and the game's two listings gain its paths
  (with a `cover` line in `licenses.txt` for the class files).
- **The licence texts (Q10), `Tools/Packaging/`:** `NOTICE.txt` (BRUTAL's, the same in all 19 BRUTAL packages) beside
  the program and 28 texts in `licenses/`, the same set in all four packages (the credits view, Q30, is one view for
  both programs and points to `licenses/`): `BRUTAL-LICENSE.txt` (the BRUTAL License v1.0, every BRUTAL package's
  `LICENSE.txt`); each BRUTAL package's own `LICENSES/` files as given: FreeType's `BRUTAL-FREETYPE-THIRD-PARTY-NOTICES.txt`
  (FreeType 2.14.3 and libpng 1.8.0.git) and `FTL.txt`; GLFW's (3.5.1 and its Wayland protocols); OpenAL Soft's
  `BRUTAL-OPENAL-THIRD-PARTY-NOTICES.txt` (1.25.2, built from upstream commit `dd4e07de…`, upstream
  openal-soft.org and github.com/kcat/openal-soft: where its source is) with `openal-soft-COPYING.txt` (the LGPL 2.0
  text), `openal-soft-BSD-3Clause.txt`, `openal-soft-LICENSE-pffft.txt`, `openal-soft-fmt-LICENSE.txt` and
  `openal-soft-gsl-LICENSE.txt`; gli's (gli 0.8.3, glm 0.9.9.9), KTX's (KTX-Software and six statically linked
  components, Apache-2.0 and Zstandard's BSD), stb's (MIT or Unlicense), VMA's, zlib's, Vulkan's and Vulkan
  Abstractions' (the Khronos registry, Apache-2.0, in the bindings), glTF's (Khronos' schema licence, in
  `Brutal.Gltf.dll`); `HarfBuzzSharp-LICENSE.txt` (MIT, Microsoft) and `HarfBuzzSharp-THIRD-PARTY-NOTICES.txt`
  (HarfBuzz and the rest built into it); `dotnet-LICENSE.txt` and `dotnet-THIRD-PARTY-NOTICES.txt` (the runtime pack's:
  .NET's MIT licence and its notices, Brotli and zlib-ng among them; the 12 `Microsoft.Extensions.*` 10.0 packages
  give the same notices); `Microsoft.Extensions.ObjectPool-THIRD-PARTY-NOTICES.txt` (**found:** BRUTAL's logging pulls
  `Microsoft.Extensions.ObjectPool` 11.0 preview 5, whose notices are another release's); `CommunityToolkit.HighPerformance-License.md`
  and `-ThirdPartyNotices.txt` (MIT); `Lato-OFL.txt` and `STIX-OFL.txt` (from `old-game/objects/fonts/`, `ui.pak`'s
  sources, the same as `Data/Fonts`'); `Chromium-LICENSE.txt` (Chromium 152's BSD-3-Clause licence, "Copyright 2015
  The Chromium Authors": **found** on the rebase on step 4, the engine has embedded Chromium's two search-field cancel
  images, `search_cancel.png` and `search_cancel_pressed.png`, since before M12 (`UI/Resources/README.md`), and the
  BSD licence asks for its notice with a binary; taken from Chromium's GitHub mirror at 152.0.7977.89, `LICENSE`, into
  the reference sources' `chromium-152-source/license/`; in no package or repo file, so its `text` line's source is
  `fixed:`, a text kept as committed; step 4's credits view doesn't list Chromium yet). Kept byte for byte as given, some with CRLF line ends, by
  `Tools/Packaging/.gitattributes` (`-text`; the repo's rule otherwise makes every text LF).
  `Tools/Packaging/licenses.txt` says where each text came from (`text` lines: `nuget:<package>/<path>`, `*` for any
  part of a name, `{rid}` the runtime; `repo:<path>`) and which texts cover which file (`cover` lines: a pattern on the
  folder's paths, `{program}` the executable, `bundle:<pattern>` the files inside it, `-` none needed: the game's own
  files, the texts, the Visual C++ runtime). **The publish** checks every text against every source at the version
  the program's restore resolved (`project.assets.json`; line ends aside: the Linux runtime pack's are LF, the Windows
  one's CRLF) and refuses a text no line names; **the check** fails on a file nothing covers or a covering text
  missing. Every binary found and its licence: the program (.NET's host and runtime: MIT and .NET's notices);
  inside it BRUTAL's assemblies (BRUTAL License; Vulkan's, Vulkan Abstractions' and glTF's notices for those three),
  `Microsoft.Extensions.*` and .NET's libraries (MIT), CommunityToolkit (MIT), HarfBuzzSharp (MIT and its notices),
  `Statventure.dll` (ours, with step 4's Lato (OFL), BRUTAL's logo and notice (BRUTAL License, `NOTICE.txt`) and
  Chromium's two images (`Chromium-LICENSE.txt`)); beside it `libglfw`/`glfw3.dll` (zlib/libpng
  licence), `libfreetype`/`freetype.dll` (FTL), `libpng18`/`png18.dll` (libpng), `libzlib`/`zlib.dll` (zlib),
  `libopenal`/`OpenAL32.dll` (LGPL-2.0-or-later and its four parts), `libstb`/`stb.dll`, `libvma`/`vma.dll` (MIT),
  `libVulkanEx`/`VulkanEx.dll` (BRUTAL's own native: BRUTAL License), `libgli`/`gli.dll` (MIT), `libKtx`/`Ktx.dll`
  (Apache-2.0, BSD-3-Clause), `libHarfBuzzSharp` (MIT, HarfBuzz's Old MIT); `ui.pak` (Lato and STIX Two, OFL); the
  stand-in's `Data/Fonts` (the same OFL fonts); the Visual C++ runtime (Visual Studio's licence, below).
- **The Visual C++ runtime (Q9):** `Tools/Packaging/vc-runtime.txt` pins Microsoft's redistributable, `VC_redist.x64.exe`
  14.44.35211 (its download URL and SHA-256; `aka.ms/vs/17/release/vc_redist.x64.exe` pointed there on 2026-11-08), and
  the SHA-256 of `vcruntime140.dll`, `vcruntime140_1.dll` and `msvcp140.dll` in it. `publish.sh` takes them, when the
  build cache lacks them or holds others, into `$STATVENTURE_CACHE/vc-runtime/14.44.35211/`: `curl`, the download's
  SHA-256 checked, `cabextract` on the bundle (its payloads are cabinets), then on each payload for `*_amd64`, each DLL
  taken only if its SHA-256 is the pinned one (`vcruntime140.dll_amd64` in the `vcRuntimeMinimum_x64` cabinet).
  `publish.ps1` does the same with the redistributable's own `/layout` (which installs nothing) and Windows'
  `expand.exe` (not run: no Windows here). A newer runtime is a new pin (URL, SHA-256, the three DLLs'). **Never in
  git**, as the design says. **The licence, checked (2026-11-08):** the redistributable's own terms ("MICROSOFT VISUAL
  C++ 2015 - 2022 RUNTIME", its `license.rtf`) grant installing and using it and forbid to "share, publish, rent or
  lease the software" or "provide the software as a stand-alone offering or combined with any of your applications
  for others to use"; the right to ship its DLLs with a program is Visual Studio's: Microsoft's redistribution list for
  Visual Studio 2022 (learn.microsoft.com/visualstudio/releases/2022/redistribution, "Visual C++ Runtime Files") lets
  a "validly licensed" Visual Studio 2022 Enterprise, Professional or Community user "copy and distribute with your
  program" the files of `VC\redist`, and "Distribution of the Visual C++ Runtime Redistributable package, merge
  modules, and individual binaries obtained from those pages is limited to licensed Visual Studio users and is subject
  to its license terms" (its "Distributable Code" conditions; the licence page renders its text by script and wasn't
  read here). **The user's answer (2026-11-08, the question tool): they ship.** The user states they are (or will
  be) a licensed Visual Studio user and ships the three DLLs under Visual Studio's Distributable Code terms; recorded
  in `vc-runtime.txt` too.
- **The check, `Tools/Scripts/check-packages.sh` → `Tools/Packaging/check_packages.py`** (Python 3 and `objdump`;
  Linux): each archive unpacked into a scratch folder in the build cache and held to every rule; all problems
  reported, the size reported (not judged). The rules: the archive's name (`<program>-<version>-linux.tar.gz` or
  `-windows.zip`, the version Q1's form or `dev`) and one folder `<program>-<version>/` (Q4); every path in it, folder
  or file, and the executable bit as `Tools/Packaging/listings/<program>-<platform>.txt` lists them (`dir`, `file`,
  `exec`; nothing missing, nothing extra; a `.pdb`, a wrong name, `classes/` all show here); tar entries of mode 755
  or 644 (folders 755), owned by 0:0, no links; the game's `packs/` the committed packs byte for byte, the editor none;
  `NOTICE.txt` and `licenses/` Tools/Packaging's, all and only them; on Windows the three runtime DLLs with the pinned
  SHA-256; the program an x86-64 ELF, or a PE32+ x86-64 with subsystem 2 (GUI, Q6); single-file (the .NET bundle's
  marker) with exactly the files inside that the listing names (`bundle` lines: a trimmed, framework-dependent or
  native-bundling publish shows here); the program's assembly and `Statventure.dll` with embedded symbols, every
  embedded PDB's documents under `/_/`; the repo's own assemblies (`Statventure*.dll`) and their embedded symbols
  naming no `github` and holding no Source Link entry (the user's answer, below); the program's assembly's informational version (`<version>+<commit>`, read
  from inside the bundle) the archive's version; every ELF file's needs (`objdump -p`) in the folder, found through
  `$ORIGIN` in its RUNPATH, or the system's (libc, libm, libdl, librt, libpthread, libgcc_s, libstdc++, the loader,
  `libvulkan.so.1`), its versions at most `GLIBC_2.34` and `GLIBCXX_3.4.30`; every PE file's imports (read from its
  import table) in the folder or Windows' (KERNEL32, USER32, GDI32, SHELL32, ADVAPI32, ole32, OLEAUT32, AVRT, WINMM,
  `vulkan-1.dll`, the UCRT's `api-ms-win-crt-*`), **except an IL-only .NET assembly's** (added by step 6, the manager's
  decision (a), 2026-11-08: a PE file whose CLI header, data directory 14, has the IL-only flag, `is_il_only`; every
  managed PE file imports `mscoree.dll`, the old CLR's entry, which .NET doesn't use: it loads such an assembly itself,
  never through Windows' loader; the game's data library lies beside its Windows program since step 6; a mixed .NET
  assembly, native code in it, is checked as a native DLL); every file and every file inside the program covered by
  `licenses.txt`. **The run** (Linux packages; `--no-run` skips it): unpacked, started in a headless compositor
  (`cage` on wlroots' headless backend, the user's `DISPLAY` and `WAYLAND_DISPLAY` unset; `xvfb-run` where there is no
  cage, as in the cloud), `XDG_DATA_HOME` a scratch folder (so the per-user folder is scratch: the stand-in logs
  `game folder …/player/stat-fick`, and since step 2 writes its log file in that folder's `logs/`),
  `ALSOFT_DRIVERS=null` (OpenAL Soft's "No Output": nothing audible): the
  stand-in with `--capture <png> --frames 30` must end with 0 having written a PNG of more than one colour (it draws
  its page: `claude-work/m12/step3/capture.png`, about 1.3 s); the editor given `--player-folder <scratch>
  --not-an-option` must end with 2 printing its usage (its `--capture` is step 6's); this machine's game folder and
  Documents folder's top level compared before and after (as `mutate.py` does).
- **The listings, `Tools/Packaging/listings/`:** made by `check-packages.sh --listing` from a good archive (version-free):
  the game's Linux package 84 paths and its program's 210 files, Windows 77 and 210, the editor's 52 and 210, 45 and
  210 (step 4's embedded resources changed no file inside the programs; its rebase added `Chromium-LICENSE.txt`). A change meant to alter the packages (a new native library, a new file in the stand-in's `Data/`, a new package)
  makes its listing again in the same commit; step 6 adds the data library's lines.
- **The check's own test, `check-packages.sh --self-test <archive>...`** (`Tools/Packaging/check_packages_self_test.py`;
  Q28's checks aren't C# tests): each archive packed again unbroken must pass, then is broken one way at a time and
  must fail naming the rule: a licence text, `NOTICE.txt` or a native library left out; a file, a `.pdb`, a text
  `licenses.txt` doesn't name added; the program not renamed; the folder without the version; the archive's name
  with a zero-padded day, or with another version than the program's; a pack changed or left out, a pack in the
  editor's; `NOTICE.txt` changed; the program or a library without its executable bit, a text with it; a file of
  mode 600, a folder of 700, entries owned by 1000, a symbolic link; the Visual C++ runtime left out or changed;
  the console subsystem, a 32-bit or ARM program; the bundle's marker gone; a file inside the program renamed (to
  one `licenses.txt` doesn't cover); the engine's embedded symbols gone, or their paths `/X/…`, or holding a Source
  Link entry, or naming `github`; the engine's assembly naming `github`; a library needing a
  file not there, one without `$ORIGIN`, one needing `GLIBC_2.39`, one `GLIBCXX_3.4.32`, a DLL importing one not
  there; and run: the stand-in drawing nothing, drawing one colour, failing, writing into the (here a scratch) real
  game folder; the editor ending with 0, or with 2 but no usage. On the four `2026.11.8.1` archives (rebased on step
  2): 118 breaks, all caught (about 2 minutes).
- **The publish's rules shown able to fail** (left to the steps: `mutate.py` runs `dotnet test` only, so a runner of
  this step's own, `claude-work/m12/step3-mutate.py`, applies each change, runs `publish.sh` with its check and undoes
  it; the list `claude-work/m12/step3-mutations.json`, made by `step3-make-mutations.py`): 45 changes to `Publish.cs`
  (the renames, the `.pdb`s, the packs, the texts, the runtime DLLs, the symbol properties, Source Link left on, the work folder, the
  version, the folder and archive names, modes and owner), `publish.sh` (the shaders from a tree without them, the
  cached runtime's SHA-256 with a damaged cache), both projects (self-contained, single-file, the natives inside,
  trimmed, a console program, the settings off), the listings, `licenses.txt` and `vc-runtime.txt`: 44 killed (two by
  another rule than expected: with a project's runtime settings off, the publish has no runtime pack and its licence
  check refuses it), 1 accounted for (the publish's own refusal of a version not of Q1's form, off: the check refuses
  the archive's name all the same); and 5 tests of the publish's guards, each passing (a licence text not its
  package's, a text `licenses.txt` doesn't name, a text from a package the program hasn't and a version not of Q1's
  form all refused; a tree without compiled shaders publishing), each guard switched off letting its bad input through
  but the version's.
- **`editor.ps1`:** the Windows counterpart of `editor.sh`: env.ps1, `dotnet build Tools/Editor -c Release`, then
  `dotnet Tools\Editor\bin\Release\net10.0\Statventure.Editor.dll --repo <engine> <arguments>`, its exit code passed
  on (not run here: no PowerShell; "Windows check", item 8d).
- **From a clean tree:** a fresh clone of the branch (no build, no compiled shaders, no `release/`), its own
  `publish.sh --version 2026.11.8.1`: the shaders compiled, the four archives made and checked (the Linux ones run),
  23 s (`claude-work/m12/step3-clean-tree.sh`).
- **The repo's address stays out of the programs (the user, 2026-11-08, the question tool; (a), the recommended
  option).** Found: built from a clone of the GitHub repo (as the release script will be), every program carried in
  its embedded symbols Source Link's `https://raw.githubusercontent.com/Vatrix33/statfick/<commit>/*` (the SDK's
  default; debuggers would fetch the sources there, which a private repo refuses). Options asked: (a) leave it out of
  the packages, (b) keep it. Now `Publish.cs` passes `-p:EnableSourceLink=false`: no Source Link entry, the commit
  still in the version (`2026.11.8.1+<commit>`, from the SDK's git information), the paths still `/_/…`; and the
  check fails on any of the repo's own assemblies (`Statventure*.dll`, inside the program or beside it) whose bytes or
  embedded symbols name `github` (any case; `raw.githubusercontent.com` too) or whose symbols hold a Source Link entry
  (its kind's GUID or a `{"documents"` map). Third-party assemblies keep theirs (CommunityToolkit's symbols carry its
  own public Source Link; .NET's and BRUTAL's assemblies name their public GitHub repos in their metadata).
- **The cloud's setup:** `cloud-setup.sh` installs `binutils` (objdump) and `cabextract`; the run there uses
  `xvfb-run` (already installed). Not run in the cloud: whether Ubuntu's own .NET SDK publishes for `win-x64` (it
  downloads Microsoft's runtime pack) is to see there.
- **What the Linux packages need of the system (for step 7's release text):** x86-64, glibc 2.34 or later **and**
  GCC 12's libstdc++ (`GLIBCXX_3.4.30`, which libKtx and libopenal need): Ubuntu 22.04, Debian 12, Fedora 36 and
  later; not Fedora 35 (GCC 11) and not RHEL 9 or its rebuilds (Rocky, Alma: glibc 2.34 but GCC 11's libstdc++). Also
  `libvulkan.so.1` with a Vulkan 1.3 driver, X11 or Wayland, and the sound server's libraries. The check holds every
  library to `GLIBC_2.34` and `GLIBCXX_3.4.30` at most, so a BRUTAL update needing more fails it. Windows: 10 or 11,
  x64, a Vulkan driver (the Visual C++ runtime is shipped).
- **Found (adding to "What exists"):** `libHarfBuzzSharp.so` carries no RUNPATH (it needs only system libraries:
  harmless); **libKtx and libopenal need `GLIBCXX_3.4.30`, GCC 12's libstdc++** (libgli 3.4.29): Ubuntu 22.04 and
  Debian 12 have it, but Fedora 35 (glibc 2.34, GCC 11) and RHEL 9 and its rebuilds don't, so "glibc 2.34 or later"
  isn't the whole need: the release's text (step 7) should say Ubuntu 22.04, Debian 12, Fedora 36 or later; the
  stand-in's window in a headless cage is maximized to 1280 × 683; nothing writes outside the scratch per-user folder
  (something writes `gvfs-metadata/` into `XDG_DATA_HOME`: the scratch one).
- **For the later steps:** step 4: `licenses.txt` already covers `Statventure.dll` with Lato's OFL. Step 6: the data
  library beside the program, not in the bundle (an `ExcludeFromSingleFile` item metadata on its
  `ResolvedFileToPublish` in the sandbox's project), its lines in the stand-in's listings and a `cover` line; the
  usage text with the new names (`EditorArguments.Usage` still says `Statventure.Editor`); Q18's reading of the game's
  version: `check_packages.py` reads it from inside the single-file bundle (the manifest after the host's bundle
  marker, the program's assembly's informational version). Step 7: `release.sh` runs `publish.sh --version <v>`
  (which checks the four) and uploads them. Step 8: the Windows items (`publish.ps1` with the runtime's `/layout`,
  `editor.ps1`, the zips started from Explorer). Q13 (the stand-in's title) is the sandbox's `Program.cs`, not
  changed here.
- **Open (found by step 7, 2026-11-08; the manager decides who fixes it): publish fails when the repo is reached
  through a symlink.** On the PC `/home` is a symbolic link to `/var/home`; a clone made through `/home/…` gave MSBuild
  the projects under both paths (the shell's `/home/…`, .NET's working folder `/var/home/…`), the program's restore
  came out with no project references (the sandbox's `project.assets.json`: one package, ILLink) and `Publish.cs`'s
  licence check refused every text ("the program has no package Ahwoo.BRUTAL.*"). The repo's own path holds no link.
  `pwd -P` in `env.sh` likely fixes it; not tried.

### Step 4, first: a shrunk raster image as Chromium 152 draws one (2026-11-08)

Step 4's prerequisite, a commit of its own (the user's decision (a), "Decided (the logo's file)"; the sources in
`reference-src/chromium-152-source/mipmap/` and `reference-src/skia/mipmap/`). The references are rastered on the CPU
(`--disable-gpu-compositing`), so the path is Blink → cc's software decode cache → Skia's raster backend:
- **Blink** asks every decoded image (`BitmapImage::IsLazyDecoded`) for medium filter quality
  (`GraphicsContext::ComputeFilterQuality`, `GetDefaultInterpolationQuality`: medium off Android), which only
  `image-rendering: pixelated` would lower to none (the engine ignores `image-rendering`). `ImagePainter::PaintIntoRect`
  draws an `<img>` into its fitted rect pixel-snapped, and where the content box doesn't hold that rect (object-fit:
  cover, none) cuts the source and the destination to the snapped content box instead of clipping
  (`UiPainter.Content`, `DrawFitted`).
- **cc** (`SoftwareImageDecodeCache`, `CacheKey::FromDrawImage`; `Canvas/ImageDecodeScale.cs`): the draw's scale
  from the matrix image → device (its diagonal, or `SkMatrix::decomposeScale`'s column lengths); the target size, the
  source part (rounded out, inside the image; a pattern's is the whole image) times that scale, rounded; the mip
  level, the deepest whose next level (each half the last, **rounded up**, `MipMapUtil`) isn't smaller than the target
  in either direction. Level 0 (anything shrunk by less than half, every upscale): the image itself; deeper: a copy of
  the source part at that level's size; an empty target: nothing drawn. The copy is sampled bilinearly (low quality).
- **Skia** makes the copy with `SkPixmap::scalePixels` at linear filtering and the nearest mipmap
  (`Canvas/SkiaMipmap.cs`): the decoded pixels premultiplied (the PNG codec's `RGBA_to_rgbA`, `(c·a + 127) / 255`);
  mip levels by the HQ downsampler Chromium builds (each half the last, **rounded down**: a 2 × 2 box, widened to
  1 2 1 along an odd side, weights summed in integers and shifted, truncated); the level `SkMipmapAccessor` picks
  from the scale's geometric mean (`SK_SUPPORT_LEGACY_ANISOTROPIC_MIPMAP_SCALE`), log2 less a half, rounded; then
  sampled bilinearly at each target pixel's centre. Where cc's size (rounded up) and Skia's levels (rounded down)
  disagree (odd sizes), the copy is a level resampled, or the image itself (a 201-wide image's first copy, 101 wide,
  is Skia's base resampled).
- **The draw** (`Canvas.DrawImage(CanvasImage, …)`, `canvas.frag`'s `skiaBilerp`): Chromium's raster draws an image
  through Skia's legacy shader context (`SK_ENABLE_LEGACY_SHADERCONTEXT`; `SkBitmapProcState`): the pixel's centre
  mapped to the texture, less half a texel, its fraction's top four bits the weight (sixteenths), the four texels
  summed in integers and divided by 256, truncated; clamped, or wrapped for a pattern. The shader does the same with
  `texelFetch` from a map of canvas pixels to texels the canvas computes in double precision. Before it, the GPU's
  bilinear filtering left 239 pixels beyond 12 levels in one image at 1× and 0.3 % of image-shrink at 1.65.
- **`Canvas/CanvasImage.cs`:** a decoded image kept as Chromium keeps it: premultiplied, its original texture and its
  scaled copies (by source part and size) made on first use, on the device of the first canvas that draws it. The
  UI's images are canvas images now (`UiPainter.ImageTexture` is `Func<string, CanvasImage?>`; `UiHost` makes them
  from its decoded PNGs); they are drawn premultiplied (straight alpha filtered was another difference at alpha
  edges: a straight white beside transparent white).
- **Found on the way:** on RADV a `texelFetch` through a `sampler2D` built from the bindless arrays
  (`sampler2D(Textures[nonuniformEXT(i)], Samplers[...])`) lost the index's non-uniformity: lanes of a wave drawing
  different images read one image's texels (clusters of 4 to 8 wrong pixels; lavapipe was right). Samplerless fetches
  (`GL_EXT_samplerless_texture_functions`, `texelFetch(Textures[nonuniformEXT(i)], …)`) are right on both. The
  painter's existing `sampleTexture` (`texture()` through the same construction) is untouched.
- **Tests:** `Canvas/ImageScalingTests.cs` (each rule against numbers worked from the sources: levels and sizes,
  the scale's decomposition, the copy chosen, premultiplication, the downsampler's filters, Skia's level, scaling
  pixels; on the GPU the first level at half size through the canvas's scale and transform, the image itself past
  half, nothing for an empty target, sixteenths and truncation clamped and wrapped, a part cut before it is shrunk,
  a pattern shrinking the whole image, premultiplied filtering) and `Ui/ImageScalingPaintReferenceTests.cs`, two
  Chromium fixtures (`image-fixtures.json`; images in `Tests/Statventure.Tests/Data/Images/`, made by `make-images.py`
  there: one-pixel checks, stripes, rings, yellow beside transparent white, translucent lines): `image-shrink`
  (shrunk 1.5, 2, 2.7, 3.2, 4 and 6 times, by a transform, rounded, object-fit contain and cover; an odd-sized image
  2, 3.3 and 4.3 times) and `image-shrink-backgrounds` (a single tile shrunk 5 and 3 times, one rounded, a pattern
  8 times). Each draw has an image of its own (a data url with its own parameter): cc scales a second draw of one
  image from a copy it already holds (`FindCachedCandidate`), in whatever order its tiles raster. Against Chromium,
  pixels beyond 12 levels: image-shrink 15 of 251,600 at 1× (0.006 %) and 318 of 684,981 at 1.65 (0.046 %),
  image-shrink-backgrounds 33 of 54,000 (0.06 %) and 19 of 147,015 (0.013 %); the rest a level or two (rounded
  corners and the cover cut's edge, where Skia clamps to the source part, keep up to 40).
- **What moved:** no existing reference draws a raster image (the game screens' images are the six race SVGs), so
  every existing render is the same pixel for pixel. One test changed: `View3DTests`'
  surface-against-image test now uses the shapes' opaque background and an unturned frame (an image is filtered
  premultiplied in sixteenths, a surface's picture by the GPU: they agree where the picture lands on whole pixels).
- **Not ported:** `image-rendering` (no nearest-neighbour draw); Skia's strict source-rect constraint for a cut
  `<img>` (the engine samples one texel past the cut's edge); a second draw of one image at another size taking
  its copy from a copy cc already holds (the engine always scales from the original).

- **Mutations and full runs:** with step 4's (below).

### Step 4: the engine's embedded Lato and BRUTAL logo; the splash and the credits (2026-11-08)

**The engine's embedded Lato (`UI/EngineFonts.cs`, Q17, Q23):** `UI/Resources/lato-regular.ttf`, `lato-bold.ttf`
and `lato-ofl.txt`, copied from `old-game/objects/fonts/` (`Lato-Regular.ttf`, `Lato-Bold.ttf`, `Lato-OFL.txt`, the
files `make-ui-pack.sh` packs), embedded as `Statventure.UI.<name>` (`Statventure.csproj`: `*.ttf` and `*.txt` beside
the existing `*.png`). `EngineFonts.Regular`, `Bold`, `Licence` (the bytes); `EngineFonts.AddTo(FontLibrary)` adds
both faces as the family `Lato` (`EngineFonts.Family`), weights 400 and 700, so a document needs no @font-face (the
engine's own documents); `EngineFonts.Mount()`, a custom mount giving them at ui.pak's paths
(`ui/fonts/lato-regular.ttf`, `lato-bold.ttf`, `lato-ofl.txt`), so a stylesheet whose @font-face rules name those
paths loads them from a document's `Files` unchanged. **For step 6:** the editor switches by making its `_uiData`
`new VirtualFileSystem([EngineFonts.Mount()])` in place of the game's ui.pak (its `Fonts.css` unchanged), or by
`EngineFonts.AddTo` and dropping `Fonts.css`. **For step 5:** the crash window's document takes `EngineFonts.AddTo`.
(Left to the step, "a mount or a font library": both, for those two uses.)

**The splash (`UI/Splash.cs`, Q29, and the logo's decision):** `UI/Resources/LOGO_BRUTAL_WHT.png` is the branding
page's file byte for byte (4096 × 896; no colour chunks, so Chromium draws it untouched under the harness's sRGB
profile). `new Splash()` is a document of its own: the logo as `<img class="logo" src="engine/brutal-logo.png">`
in its own data (`Splash.LogoPath`, a custom mount in its `Files`, so a `UiHost` shows it with no wiring), on
`html { background: #1a1c1a }` (`Splash.Ground`, the editor's `--paper`), centred by flex,
`width: 696px; max-width: 100%; max-height: 100%; object-fit: contain` (`Splash.LogoWidth`): 696 × 152.25 CSS px,
narrower in a narrower window, shorter (kept whole, centred in its box) in a shorter one; the engine's Lato in its
library though no text shows. Its clock: `Advance(time)`, once a frame with the frame's time (after the host updated
the document); the first call starts it; `Elapsed`; `Finished` once 2 s (`Splash.Length`) have passed or it was
skipped; a `keydown` or a `mousedown` (any button) the document gets from 1 s on (`Splash.SkippableAfter`, at the
time of the last `Advance`) skips it (`Skipped`), earlier ones do nothing. Left to the step and chosen: a key's
press and any mouse button's press count ("a key or a click"); events count at the last frame's time, at most a frame
late. On the PC the logo's first draw makes its level-2 copy (1024 × 224 at 1×, level 1, 2048 × 448, at 1.65) from
the decoded 4096 × 896. **For step 5:** `using var splash = new Splash(); var host = new UiHost(window, gpu,
splash.Document);` then each frame `host.Update(t); splash.Advance(t); if (splash.Finished) break; host.Render(…)`.

**The credits (`UI/Credits.cs`, Q30):** `Credits.Markup(title, ProgramInfo)` and `Credits.Css` (rules under
`.credits`) for a program's own dialog; `new Credits(title, program)` a whole document (`Credits.ViewCss`: the
dark ground, a scroller `.credits-view` with the editor's scrollbar colours; `Scroller`), its own library with the
engine's Lato. Layout (left to the step): the title (`h1`, the program's display name: "Statventure Editor",
"Statventure Sandbox") and "Version 2026.11.8.1" (`ProgramInfo.Version`, "Version dev" for a dev build; no commit,
Q3); "Built with BRUTAL" (`h2`) with `NOTICE.txt` verbatim (`UI/Resources/brutal-notice.txt`, every BRUTAL
package's, its first line BRUTAL's copyright line, so the line shows once), `white-space: pre-wrap`; "Components",
each with its name and version (`h3`), its licence and its copyright lines as its licence text states them, and its
notes; "Licences": "The full texts of these licences are in the licenses folder beside the program (BRUTAL's in
licenses/BRUTAL-LICENSE.txt), and BRUTAL's notice in NOTICE.txt." Lato 13/20 px, headings bold, the licence names in `--muted`. **The components**
(`Credits.Components`, 36; 37 with the follow-up's Chromium): every component the shipped texts name: FreeType 2.14.3 (FTL, with its credit, "Portions
of this software are copyright © 2026 The FreeType Project (https://freetype.org). All rights reserved.", the year
BRUTAL's FreeType notice gives, and "This software is based in part of the work of the FreeType Team."), its BDF and
PCF drivers, libpng, zlib, GLFW with SDL_GameControllerDB and the Wayland protocol descriptions, OpenAL Soft 1.25.2
(LGPL-2.0-or-later; its source, `https://github.com/kcat/openal-soft` at commit `dd4e07de…`, unmodified, the
separate library beside the program; its licence's text `licenses/openal-soft-COPYING.txt`) with
Spherical-Harmonic-Transform, PFFFT, {fmt} and GSL, stb, the Vulkan headers and reference pages, VMA, GLI and GLM,
the glTF schemas, KTX-Software with Basis Universal, miniz, Zstandard, astc-encoder, dfdutils and its Vulkan headers,
HarfBuzzSharp and HarfBuzz, .NET 10 (with a pointer to its third-party notices) with Brotli and zlib-ng,
Microsoft.Extensions and the .NET Community Toolkit (each with a pointer to its notices file), Lato and STIX Two
(OFL). Components whose texts state no copyright
line (OpenAL Soft, KTX-Software, Basis Universal, astc-encoder, dfdutils, the Vulkan headers in libktx) show their
licence only. Lato's line spells "Łukasz": the shipped OFL text (the old game's, ui.pak's) has the letter
mis-encoded (U+0106 U+0081, its UTF-8 read as Latin-1 and written again). **Checked against the texts**
(`EngineScreensTests`): each copyright and note line is in the text it names (whitespace runs made single spaces);
every component each BRUTAL package's notices name (their "Name" fields, or KTX's and stb's section titles) is
credited, the count exact; every shipped text is named by a component (its text or a note), `NOTICE.txt` and BRUTAL's
licence by the BRUTAL section and the closing line; and every text a component, a note or the closing line names is
shipped. **The shipped set is step 3's** (since the follow-up, 2026-11-08): the tests read `Tools/Packaging/licenses.txt`'s
`text` lines and the texts kept beside it (`NOTICE.txt`, `licenses/`), and check that the two agree (every text listed
kept, every text kept listed), so the credits and the packages can't drift apart; `NOTICE.txt` and the Lato OFL text
compared byte for byte with the engine's own copies. **The follow-up (2026-11-08, branch `m12-step4-credits` from
`77854d4`):** step 3 found the engine has embedded Chromium 152's two search field cancel images
(`UI/Resources/search_cancel*.png`) since before M12 and ships `licenses/Chromium-LICENSE.txt` (BSD-3-Clause); the
credits list "Chromium 152 (the search field's cancel button images, in the engine)", BSD 3-Clause License, "Copyright
2015 The Chromium Authors", last, with the fonts (what the engine itself embeds besides BRUTAL's logo); 37 components.
`credits` and `credits-end` captured again (the new lines, and the top's scrollbar thumb shorter for them).
**For steps 5 and 6:** the sandbox's About button shows `new Credits("Statventure Sandbox", ProgramInfo.Of(…))`
(its own document) and the editor's dialog `Credits.Markup("Statventure Editor", EditorView's ProgramInfo)` with
`Credits.Css` in its document (its scroller its own).

**Fixtures** (`Ui/EngineScreensPaintReferenceTests.cs`, `engine-screen-fixtures.json`, the documents' own
stylesheets with no base sheet; the logo as a data url for Chromium): `splash` (1000 × 600), `splash-narrow`
(480 × 300), `splash-short` (800 × 100), `credits` (720 × 800, a release's version) and `credits-end` (scrolled to the
foot), each at 1 and 1.65; the engine renders the real documents, their image from their own data. Pixels beyond 12
levels: the splashes none (worst 1 at 1×, 11 at 1.65), credits 2 (1×) and 324 (0.021 %, 1.65), credits-end 199
(0.035 %) and 151 (0.010 %), at the paint fixtures' limits (12 levels, 0.2 %). Renders kept in
`~/.cache/stat-fick-build/claude-work/m12/step4/`.

**Mutations** (the prerequisite's and the pieces' together): 96 (`~/.cache/stat-fick-build/claude-work/m12/step4-mutations.json`,
made by `step4-make-mutations-pre.py`, the prerequisite's 41, and `step4-make-mutations.py`, the pieces' 55, beside it):
94 killed at first; the 2 survivors were gaps, closed: the canvas's own scale left out of a draw's matrix (the GPU
test's checks give the same mean at any level; a test of columns through a canvas scale of 2 added: the UI paints at
the canvas's scale 1, so only direct canvas users see it) and the cover cut left out (a cut from an even column
changes nothing a copy shows, and the first case's 450 pixels stayed under 0.2 %: the cover image now cut from
column 33 and 120 × 76, `image-shrink` captured again); both killed when run again (`step4-rerun.json`,
`step4-rerun2.json`); nothing written to the player's real folders.

**Full runs:** the full suite passes on the GPU (1 min 55 s) and on lavapipe (2 min 27 s), inside a headless Wayland
compositor under the shared lock, on `2911b84` with both commits: 3625 tests (main's 3544 and 81 new), none skipped
(before the rebase, on `345388f`: 3592, 1 min 33 s and 1 min 57 s); the prerequisite's commit alone builds and passes
the UI, canvas, 3D and editor-screen tests (1244); the player's real game folder stayed missing and the Documents
folder's top level unchanged.

### Step 5: the application's own screens: the splash before `Load`, the crash window after a crash (2026-11-08)

- **The splash in `Application.Run` (`Core/Application.cs`, Q29):** `ApplicationOptions.Splash` (true by default) and
  `Application.ShowsSplash`, the option on a program's own run, one with a `LogName`. **The option for tests (left to the
  step; chosen):** only a run with a log name shows it, as only it writes logs (step 2's rule and the manager's decision
  (1): the program's own path), so tests' applications show none, the editor's tests (its internal constructor, no log
  name) included, with no switch for the whole process and nothing changed in the editor; the editor's program (its
  public constructor gives `statventure-editor`), the sandbox and the cutscene player (`stat-fick`) show it with no change
  of theirs; a test that gives a log name turns `Splash` off (step 2's `Crasher`). **Where:** once the window, device and
  swapchain are made, before the sound and `Load`: the engine's `Splash` document in a `UiHost` of its own on the
  application's window, a loop of its own drawing each frame (vsync) at the frame's time until it is `Finished` (2 s, or a
  key or a mouse button after its first second: step 4's rule, now through the window's input); its last frame stays in
  the window while the sound and `Load` run. Minimized, its time goes on and nothing is drawn (the loop sleeps until its
  end, an event or a wake-up). Logged: `the splash shown (2 s)`, or `the splash skipped at 1.3 s`. The application's own
  loop (`Loop`: its clock, its frame numbers) starts after it, as before. A capture asked for before `Run`
  (`CaptureNextFrame`) takes the splash's first frame.
- **During the splash (left to the step, "a crash during the splash"; chosen):** a crash in it (its own, the device's) is
  the start's crash: the crash file, the teardown, the crash window, no `Load`; another thread's crash is taken by the
  splash's loop as by the main loop (its sleep woken by it); a close of the window ends the run before anything loads
  (no sound, no `Load`, no `Unload`; `ConfirmClose` isn't asked, nothing being loaded to ask about; logged `the window closed
  during the splash, at 0.4 s: the run ends without loading`).
- **The end of a crashed run, reworked (`Run`, `Ended`):** the crash is now caught and kept (`ExceptionDispatchInfo`), not
  rethrown, until the run has ended, then thrown. Rethrown at once (step 2's `throw;`), .NET counted it unhandled at its
  first pass, before any `finally` ran: the sandbox printed "Unhandled exception." and the stack before its crash window had
  even opened (seen with `--crash`), and on Windows that is where Windows Error Reporting steps in. **After a crash on a
  program's run** (a log name), after the teardown: the crash window (`ShowCrashWindow`), or, when it can't be shown,
  step 2's message on the terminal (Q24); the run's log closed (now kept open through the window, for its lines: `the crash
  window opens`, `the crash window closed`, a file manager that failed, a window that couldn't be shown with its exception);
  then the program ends with `Application.CrashExitCode`, **1** (chosen; exit codes were left to the steps; the editor's 2
  stays its command line's), through `Environment.Exit` (`Application.Exit`, internal: the tests catch it). So a crashed
  program no longer aborts (no SIGABRT 134, no core dump) and .NET prints nothing of its own. Without a log name (tests'
  applications) nothing changed: no window, no exit, the exception leaves `Run` (a failing teardown's, as C#'s rule gave).
- **`Core/CrashWindow.cs`, the look (`EngineWindows`; Q23):** the editor's dark look (P4's colours: `#1a1c1a` ground,
  `#eeeae1` text, `#aaa9a1` muted, `#292c28` fields and buttons, `#3a3d38` borders, the ember `#d77a50` for the main button,
  a focused field's border and focus rings), Lato 13/20 px from `EngineFonts.AddTo` (no data needed), a 15 px bold heading,
  the content scrolling when long, the buttons at the foot on the right. **Its size (left to the step; chosen): 640 × 240
  CSS px, fixed, not resizable**: a typical crash file's path fits its field (about 85 characters), and an error of three
  lines (longer ones scroll in their own box). `EngineWindows.Run`: a document in a window of its own (a new window, GPU
  device, swapchain and `UiHost`; the application's are gone, and the failure may have been theirs), drawing each frame
  (vsync) until Close or the window's own close, both seen before each time round and after the update, so a closed window
  neither draws nor sleeps; minimized it sleeps until an event or a wake-up; everything disposed when it returns; what fails
  (no display, no Vulkan) is thrown to the caller.
- **`CrashWindow(program, crash, logsFolder)` (Q22):** the heading "Statventure Sandbox stopped because of an error"
  (`CrashWindow.Heading`; the program's name is the application's window title, `Options.Window.Title`, as step 2's terminal
  message, and the window's title too), the error's line (`CrashReport.Message`), "The details are in:" and the crash file's
  full path in a read-only text field, focused with its whole text selected (laid out first: only a rendered field takes
  focus, where Chromium's `focus()` lays out itself), Copy beside it; "Open logs folder" and Close at the foot. **Wording
  (left to the step; chosen):** the design's and the terminal's, as above, "Copy", "Open logs folder", "Close". Copy writes
  the path to the document's clipboard, which a window's host makes the system's (GLFW's); "Open logs folder" starts
  `GameStorage.FileManagerStart` on the logs folder (`xdg-open` from its parent with `MM_NOTTTY=1` on Linux; the shell's open,
  Explorer, on Windows; not waited for), any failure logged and caught, the window staying; Close ends the window, and the
  program with its exit code. Without a crash file (it couldn't be written) the details and Copy are left out; without a
  logs folder (a crash before the log's folder was known) that button. `Show(visible, capture, eachFrame)`: hidden when the
  application's window was (tests); with a capture, its first frame saved and it closes.
- **`Application.CrashCapture`:** a scripted run's crash window saves its first frame there and closes by itself. The
  sandbox (and the cutscene player) set it from `--capture`, so a capture run that crashes ends (exit 1) with a picture of
  the crash window instead of waiting headless for ever (step 3's check, step 8's flow). **For step 6:** the editor's
  `--capture` (Q20) should set it as well.
- **`MessageWindow(heading, text)` (Q6), for step 6:** the same look: the heading, the text with its lines kept
  (`white-space: pre-wrap`, scrolling when long) and Close. `MessageWindow.Show(title, heading, text)` shows it until it is
  closed and gives false (logged) when no window can be shown, so the caller says it on the terminal instead. **For step
  6:** the editor's wrong command line, e.g. `MessageWindow.Show("Statventure Editor", "Statventure Editor can't start",
  $"{problem}\n{EditorArguments.Usage}")` (the wording step 6's), then its exit code 2. **Also for step 6:** the crash window's
  heading names `Options.Window.Title`; if the editor's window takes the title with the version before `Load` (step 1's
  note), the heading reads "Statventure Editor 2026.11.8.1 stopped because of an error" (and step 2's terminal message
  likewise); keep the name alone there or say so. The editor's About: `Credits.Markup`/`Credits.Css` (step 4's note); the
  splash already runs for the editor's program through `Application` (its tests show none: no log name).
- **The sandbox (`Samples/Statventure.Sandbox/Program.cs`):** its title "Statventure Sandbox" (Q13; it was already, now
  `Sandbox.Title`). **The credits (Q30):** an About button at the page's top right shows the engine's `Credits` for
  "Statventure Sandbox" and its own `ProgramInfo` in place of the page (a document of its own in a second `UiHost` on the
  window); **back to the page (left to the step; chosen):** a Close of its own at the view's top right, in the credits' dark
  look, and Escape. **`--capture` and the splash (left to the step; chosen of the two the design named):** the frames are
  counted after the splash (the page's own, `GameTime.Frame`), so the capture waits until the splash is over: the packaged
  stand-in shows the splash at every start as the licence asks, and its checks capture the page; no option skips it.
  `--capture-splash <png>` saves the splash's first frame; `--about` clicks the About button at the start; **the crash
  trigger (for step 8; left to the step):** `--crash` throws in the first update after `Load` (a crash on the main thread),
  `--crash-thread` on a thread of its own, "Sandbox crash" (another thread's, taken by the main loop); with `--capture`, the
  crash window's first frame is saved there and the run ends with 1. From a script: `XDG_DATA_HOME` to a scratch folder,
  OpenAL's null output (`ALSOFT_DRIVERS=null`).
- **Tests (`Tests/Statventure.Tests/Core/`, 32 new):** `ApplicationScreensTests` (11; real applications in hidden windows,
  counted as minimized after at most one presented frame): the splash once before `Load` for its length (its first frame
  captured: the logo on the ground), none with the option off or without a log name (the editor's program shows it, its
  tests' construction doesn't), a click in its first second doing nothing and a key after it skipping it (through the
  window's input), a close during it ending the run without loading, a crash in it (no `Load`, the crash window after the
  teardown, no splash again) and another thread's crash taken by it; after a crash on the main thread and on another thread
  the crash window after the teardown (the pending save written, the window gone, the log open), its error, path, focus and
  selection, Copy on the clipboard (a stand-in), "Open logs folder" on `logs/` (a stand-in launcher), Close giving exit 1, no
  terminal message; a window that fails giving the terminal's message and exit 1; none without a log name; a scripted
  run's window saving its first frame and closing. `CrashWindowTests` (12): the documents (heading, error, read-only
  focused selected path, the buttons in order, text not markup, the parts left out without a file or a folder), Copy, the
  file manager's start on the logs folder (and a failing or refusing launcher), Close, the message window, the look's
  computed colours and Lato; their windows (640 × 240, not resizable, hidden in tests) ended by a click on Close through
  the window's input and by the window's close, a failure thrown, the message's `Show` giving false. `CrashWindowPaintReferenceTests`
  (9): `crash-window`, `crash-window-long` (a FileNotFoundException of four lines in its scrolling box, a Windows path),
  `crash-window-no-file` and `message-window` (the editor's usage) at 1 and 1.65 (`Reference/crash-window-fixtures.json`;
  Chromium's page selects the path with `select()` and focuses it, `data-focus`), and the list. `ApplicationCrashTests`
  (step 2's): its application shows no splash and no crash window (`CrashWindowAllowed`, internal), its program's end caught,
  exit 1 checked after each crash, none without a log name. Tests that drive a window end it after ten frames whatever
  happens, so a broken rule fails rather than hangs.
- **Against Chromium** (beyond 12 levels, at the paint fixtures' limits of 0.2 %; the same on the GPU and on lavapipe):
  crash-window 40 of 153,600 pixels at 1 (0.026 %) and 68 of 418,176 at 1.65 (0.016 %); crash-window-long 219 (0.143 %) and
  205 (0.049 %); crash-window-no-file 16 (0.010 %) and 32 (0.008 %); message-window 16 (0.010 %) and 28 (0.007 %): rounded
  corners' antialiasing, and in crash-window-long the Copy label (below).
- **The sandbox checked by running it** (`claude-work/m12/step5-sandbox-check.py`, headless in a cage, scratch per-user
  folders, the null sound output): the splash's first frame (the logo on the ground) and `--capture` taking the page after
  it; `--about` showing the credits; `--crash` and `--crash-thread` each giving a crash file naming where, the crash window's
  frame, exit 1 and nothing of .NET's "Unhandled exception".
- **Found (for the UI's owner, not changed here):** (1) the engine reveals a focused text field's caret end where Blink's
  `UpdateSelectionOnFocus` reveals a range selection's bounds (`FrameSelection::RevealSelection`, `kDoNotRevealExtent`), which
  don't scroll a field they cover: a path longer than the field shows its end in the engine and its start in Chromium
  (tried as a fixture: 2.5 % of pixels at 1, 2.1 % at 1.65; the fixture's path kept within the field; the evidence in
  `claude-work/m12/step5/finding-long-path/`). (2) In crash-window-long the engine draws "Copy" in grayscale where
  Chromium uses LCD text: likely the error's scrolling box's contents counted past its clip in the engine's overlap test
  (the box's text reaches 4 px into the path's row); within the limits. (3) Under cage at a fractional output scale (1.65)
  any engine window dies of a Wayland protocol error at its first commit (GLFW's integer buffer scale 2 against an odd
  framebuffer height); not new; the 1.65 renders are offscreen.
- **Mutations:** 67 (`~/.cache/stat-fick-build/claude-work/m12/step5-mutations.json`, made by `step5-make-mutations.py`
  beside it): 66 killed; the 67th, the end check after the update left out, hangs the next test (a crash window closed
  while minimized with no wake-up sleeps in `WaitEvents()`), which `mutate.py` reads as "SURVIVED" with no counts (its
  600 s timeout); run by hand it never ended (stopped at 120 s): noticed. Not listed: the launcher not handed to the crash
  window (it would start the real file manager through the session's D-Bus) and the crash window allowed in step 2's tests
  (a test switch: they would show windows they don't drive). The rules only a real process shows, broken one at a time
  against the sandbox's check (`step5-sandbox-mutate.py`, under the lock): 9, all killed (the crash rethrown at once,
  printing .NET's "Unhandled exception."; the application's frames not captured; no crash capture; `--crash`;
  `--crash-thread`; the About button and `--about`; the credits not drawn; `--capture-splash`); `--capture`'s count after
  the splash is structural (the splash's loop runs before `Loop`, whose frame numbers start at 0), the check confirming the
  capture is the page, taken after the splash's end. Nothing written to the player's real folders.
- **Full runs:** the full suite passes on the GPU (1 min 59 s) and on lavapipe (2 min 17 s), inside a headless Wayland
  compositor under the shared lock, on `b08013b` with this step: 3658 tests (main's 3626 and 32 new), none skipped; the
  player's real game folder stayed missing and the Documents folder's top level unchanged. Renders kept in
  `~/.cache/stat-fick-build/claude-work/m12/step5/` (the sandbox's splash, credits and crash window captured headless at 1;
  offscreen renders of the same at 1 and 1.65; the fixtures' renders in `renders-gpu/` and `renders-lavapipe/`).

### Step 6: the editor as a download of its own (2026-11-08)

**The manager's decisions (2026-11-08; asked by the step before its code; Q-A and Q-B repo tooling, Q-C an editor
behaviour under the user's standing delegation of the editor's own design; recorded for the user), each the recommended
option (a):**
- **Q-A, the dev setup's data library:** under `--repo` the library comes from the library project's own build in the
  repo, `<engine>/Samples/Statventure.Game.Data/bin/<the editor's configuration>/net10.0/Statventure.Game.Data.dll`
  (`GameLibrary.InRepo`; one constant, `GameLibrary.RepoProject`, which the port changes when it makes its own project);
  `editor.sh` (and `editor.ps1`) build it beside the editor; it is checked like any game's; the editor's project has no
  reference to it. (Not a copy beside the editor's dev build; not a `--game-data` option.)
- **Q-B, the packer:** the packer references the library's project directly (`Tools/Packer/` added to the step), so
  `pack`, `update`, `extract`, `dump` and `textconv.sh` know the game's kinds with no option. (Not a `--game` option; not
  left to the port.) The brief's mention of a packer `--types` was a slip of the brief, not of the docs: no such option
  existed.
- **Q-C, one build of the library per run:** the engine keeps one record kind per record name for the whole process
  (`Records/RecordFamily.cs`, `ByName`, which throws for two types of one name), so a run can use one build of the
  library. Game folder… in the editor switches at once when the folder's library is the build the run has loaded (any
  copy of one release); otherwise it says "Start the editor again to work on the game in <folder>" and remembers the
  folder for the next start. (Not a restart for every change; not no change from the editor.)

**Files.** `Tools/Editor/`: `EditorProgram.cs` (the start: the command line, the game it names, the editor run, Q6's
message), `Program.cs` (calls it), `EditorArguments.cs`, `EditorApplication.cs`, `Game/` (`GameLibrary.cs`: the library's
file read and loaded, `GameLibraryLoader`; `GameLookup.cs`: the game's folder checked, `GameProblem` and its words,
`RememberedGame`; `GameScreen.cs` and `Game.css`: "Where is Stat Fick?"; `GamePart.cs`: Game folder…), `About/`
(`AboutPart.cs`, `About.css`), `Browser/FileBrowserView.cs` (step 8's dialog's browser, now built into any element, with a
folder mode; `FileBrowserDialog.cs` wraps it), `Session/GameData.cs`, `Session/EditorSession.cs` (the game's kinds),
`Shell/EditorView.cs` (the engine's Lato, the credits' sheet, About and Game folder… in the top bar), `Shell/Shell.css`,
`Shell/Fonts.css` (its comment), `Shell/EditorCommands.cs`, `Browser/Browser.css`. `Samples/Statventure.Game.Data/` (new),
`Statventure.slnx`, `Samples/Statventure.Sandbox/Statventure.Sandbox.csproj`, `Tools/Packer/` (`Packer.csproj`,
`Program.cs`), `Tools/Scripts/editor.sh` and `editor.ps1`, `Tools/Packaging/licenses.txt` and the game's two listings.
Tests in `Tests/Statventure.Tests/Editor/Game/` and `Editor/About/` (and the shell's, the arguments', the harness, the
fixtures' record), `Statventure.Tests.csproj` (builds the library and the packer, references neither).

**The stand-in data library (Q19; `Samples/Statventure.Game.Data`):** a class library on the engine, `Statventure.Game.Data`,
with one test kind, `StandInItem` (left to the step, chosen): `[Record("stand-in.item", 1)]`, `[Kind("Stand-in item",
"stand-in/items")]`, a help text, and three fields, `Title` (text), `Price` (gold, from 0) and `Rarity` (Common, Rare,
Exotic), so the form shows a text field, a number with its unit and a select. The port replaces its contents. **The
game links it:** the sandbox references the project; a target of its own (`KeepTheDataLibraryBeside`, before the SDK's
`_ComputeFilesToBundle`) marks it `ExcludeFromSingleFile`, so a runtime publish leaves it beside the program, a file of its
own (Q5), and build.sh's dev layout (`runtime/`) has it beside `Statventure.Sandbox` too. The editor's projects don't
reference it (M12-9, clarified: no copy in the editor's download). The tests build it (a project reference with
`ReferenceOutputAssembly="false"`) and load it only the editor's way.

**Finding the game (Q16; `Game/GameLookup.cs`):** in this order: `--repo <engine>` (the dev setup: the repo's
`Assets/Packs`, writable, and the library built in the repo, Q-A); `--game <folder>`; else the folder remembered in
`<player folder>/editor/game.json` (`{ "version": 1, "folder": "<full path>" }`, written when the player chooses one on the
screen or with Game folder…, never for `--game` or `--repo`; a file that can't be used, or a folder that isn't a full path,
remembers nothing); else the "Where is Stat Fick?" screen. **The editor no longer looks for `packs/` beside its program**
(the one-folder layout before M12-1; Q16 (a): it asks, rather than looking in the usual places). **A game's folder (left to
the step, chosen)** holds a `packs/` folder and `Statventure.Game.Data.dll`; its program isn't looked at, so build.sh's dev
layout (whose program is `Statventure.Sandbox`) works with `--game` too; `ui.pak` is no longer needed (the editor's fonts
are the engine's, below), so `GameData.UiPack` and `GameData.Problem` are gone. **Its version (Q18; step 1's note):** the
data library's informational version, read from its metadata without loading it (`GameLibrary.Read`: `System.Reflection.
Metadata`, the `AssemblyInformationalVersionAttribute`'s string, `ProgramInfo.Parse`; also its assembly name, which must
be `Statventure.Game.Data`, and its build id, the module's version id), so a library the editor refuses is never loaded.
It must equal the editor's own (`EditorView.Version`): a release's exactly; **dev builds (left to the step, chosen) match
any dev build whatever their commits** (`dev` against `dev`), a dev game refused by a release's editor and the other way
round. **Loading (`GameLibraryLoader`):** into an `AssemblyLoadContext` of its own whose engine, BRUTAL and .NET are the
editor's (its `Load` gives nothing, so what the library names comes from the default context: one engine, so its
`[Record]` and `[Kind]` are the engine's), then `RecordTypes.Of(engine, library)` and `Importers.Find(engine, library)`
(`GameData.Types`, `Importers`, which the session takes). Once per run (Q-C): the run's loader keeps the first build it
loads; another copy of that build (the same module version id) is the same assembly and the same kinds; a different build
is `AnotherBuild`. A library whose kinds can't be loaded (built against another engine, a part it names missing, two kinds
of one name) is "can't be used: its kinds can't be loaded: …", nothing kept. Tests may make a loader of their own.
**The problems and their words (`GameProblem`):** `Missing` "<folder> isn't there" (a remembered one: "Stat Fick isn't in
<folder> any more"); `NotTheGame` "<folder> isn't Stat Fick's folder: its packs folder [and Statventure.Game.Data.dll]
isn't/aren't in it" (remembered: "<folder> doesn't hold Stat Fick any more: …"); `Unreadable` "Stat Fick's data library in
<folder> can't be used: <why>" ("it isn't a .NET assembly", "it is the assembly X, not Statventure.Game.Data", the system's
message, the kinds'); `OtherVersion` "<folder> holds Stat Fick 2026.11.8.1, and this editor works only with Stat Fick
2026.11.9.1" (Q18: both named); `NotBuilt` (`--repo`) "the game's data library isn't built: <file> isn't there
(Tools/Scripts/editor.sh builds it)"; `AnotherBuild` "this editor has already loaded another build of Stat Fick's data
library (from <file>)". On the command line as they are; on the screen and in the browser as sentences.

**The command line and the start (`EditorArguments`, `EditorProgram`; Q6, Q20):** `[--repo <engine folder> | --game <game
folder>] [--player-folder <folder>] [--capture <file.png> [--frames <n>]]`, folders and the capture's file made full paths.
**The usage text (left to the step, chosen)** names the program as M12-2 does: "usage: statventure-editor …" on Linux,
"usage: \"Statventure Editor.exe\" …" on Windows (quoted for its space). Problems: "'x' isn't an option", "--repo needs a
folder" (and `--game`, `--player-folder`), "--capture needs a file", "--frames needs a number", "--frames needs a whole
number from 1, not '0'" (digits only), "--frames goes with --capture", each option given twice, "--repo and --game name the
game's data once". `--repo` and `--game` are found before any window: a game the editor can't work on is a command-line
problem as a wrong option is (left to the step, chosen; Q18's refusal on the command line), and so is a dev setup whose
library isn't built. A problem: "Statventure Editor: <problem>" and the usage on the terminal, exit code 2; on Windows,
where the editor has no console (Q6), also step 5's `MessageWindow.Show` ("Statventure Editor <version>", "Statventure
Editor can't start", the problem as a sentence, a blank line, the usage), the terminal having had it first (so a window
that can't be made leaves the terminal's). Then `EditorApplication(game, player folder, capture, frames)` runs.

**The program (`EditorApplication`):** its window titled "Statventure Editor <version>" from the start (Q3; step 1's note:
`OptionsFor` takes `TitleOf(Version, null, false)`; step 5's splash shows that title, and a crash's window and terminal
message say "Statventure Editor dev stopped because of an error"). Step 5's splash comes first on the program's own runs (its
log name), none in the tests. `Load`: the command line's game, else the remembered one, else the screen (with the
remembered folder's problem). A game chosen on the screen or with Game folder… is shown from the next update: what was
shown closed (the device waited for, its `UiHost` disposed, the view, the session, the screen), a new session, view and
host on the game, the page zoom kept. The screen takes the shell's remembered zoom; closing the window on the screen ends
the run at once (nothing to ask about). Drops reach the view only. **`--capture <file> --frames N` (Q20; left to the step,
chosen: as the sandbox's):** at the loop's frame N (30 by default; the loop's frames start after the splash, step 5), the
frame is saved and the editor quits; `Application.CrashCapture` is set to the same file (step 5's), so a capture run that
crashes saves the crash window's first frame and ends by itself.

**The editor's fonts (Q17):** every editor document reads Lato from `EditorView.FontFiles()`, a file system of the
engine's `EngineFonts.Mount()` (ui.pak's paths, so `Fonts.css` is unchanged), which the view (and the screen) owns; so the
editor draws before it knows the game, and nothing of `ui.pak` is read. `EditorView.MakeDocument` gives a document in the
editor's look (the fonts, every stylesheet in order).

**"Where is Stat Fick?" (`Game/GameScreen.cs`, `Game.css`; Q16; its wording and layout left to the step, chosen):** a page of
its own in the editor's look, its column 760 px wide (the browser dialog's), centred: the heading "Where is Stat Fick?";
"Statventure Editor makes mods for Stat Fick and works on the game's data, so it needs the game: choose the folder Stat
Fick was unpacked into, the one holding stat-fick and its packs folder." ("Stat Fick.exe" on Windows); "This is Statventure
Editor 2026.11.8.1, which works with Stat Fick 2026.11.8.1." (muted); the remembered folder's problem in the problem colour
when there is one; then the file browser built into the page in a box of the dialogs' colour, taking the height left, and
"Use this folder" under it at the right (no Cancel: nothing to go back to; the window's close ends the editor). The
browser's places: the mods folder, Documents, home, the roots and the recent places (no game packs: no game yet); it starts
beside the remembered folder when there is one, else in the most recent place, else at home, the listing focused, files
listed too (the game's folder recognised by them). A folder the editor can't work on is said under the listing in the
problem colour, the browser staying; the game found is remembered, its folder's place becomes the most recent place, and
the program shows the editor on it.

**The browser's folder mode (`FileBrowserView`, `BrowserRequest.Folder`, `Accept`, `Lead`; left to the step, chosen):**
step 8's browser, its markup unchanged (the `browser` fixture's), now built into an element of any host
(`IFileBrowserHost`: `FileBrowserPart` in the editor, the screen before it); the dialog over the shell wraps it. Picking a
folder: the button ("Use this folder") picks what the path field names when something is typed (a folder; a file stands
for the folder holding it), else the selected row when it is a folder, else the folder shown; Enter in the path field still
goes to a typed folder; a file activated (a double click, Enter) stands for the folder holding it. The request's `Accept`
may refuse a pick, said under the listing; a `Lead` line shows above the browser.

**Game folder… (`Game/GamePart.cs`; where it lives left to the step, chosen):** a button at the right end of the top bar,
before About (a filler between Redo and them); off in the dev setup, whose game is the repo; its tooltip "Stat Fick's
folder: <folder>". It shows the browser dialog picking a folder, "Where is Stat Fick?", the screen's lead line with "The
editor works on the game in <folder> now.", starting beside the game's folder; a folder it can't work on is said under the
listing; a game of the run's build: the open pack's unsaved work asked about ("…Discard them and work on the game in
<folder>?", Save, Discard, Cancel), then remembered and shown (a new session and view); another build: "Start the editor
again" — "Start the editor again to work on the game in <folder>: this editor has already loaded another build of Stat
Fick's data library (from <file>). The folder is remembered for the next start." (Q-C), remembered.

**About (`About/AboutPart.cs`, `About.css`; Q30):** the top bar's About (always on) opens a dialog titled "About" holding
the engine's credits as step 4 made them for a dialog, `Credits.Markup("Statventure Editor", ProgramInfo of the editor's own
assembly)` (the version, no commit) with `Credits.Css` among the editor's stylesheets (third, after the shell's, so the
parts' sheets can adjust it: `EditorView.CreditsSheet`), and Close (focused; Escape closes it too). **Its look (left to the
step, chosen):** 640 px wide (the credits' column), at most the window's height less 48 px, its content scrolling on its
own (`flex: 0 1 auto`), the credits without their page padding. One at a time.

**The packer (Q-B):** `Tools/Packer/Program.cs` takes `RecordTypes.Of(engine, Statventure.Game.Data)` and the importers of
both for `pack`, `update`, `extract` and `dump` (the assembly loaded by name, which the port's library keeps); git's diff
driver shows the game's records.

**The packages:** the game's archives gain `Statventure.Game.Data.dll` beside the program (its listings one line each;
`licenses.txt`: `cover Statventure.Game.Data.dll -`, ours); the editor's are unchanged (no library: M12-9). Sizes
(2026.11.8.1): the game's Linux archive 45.1 MB, 72 files (was 71), Windows 41.3 MB, 65 (was 64), the editor's 41.8 MB,
51, and 38.0 MB, 44. **`check-packages.sh`:** at first the editor's two archives and the game's Linux one passed and the game's Windows one
failed on one rule: "needs: Statventure.Game.Data.dll imports mscoree.dll, which is neither in the folder nor Windows'".
Every managed PE file imports `mscoree.dll` (the old CLR's entry, `_CorDllMain`), which .NET doesn't use: it loads an
IL-only assembly itself; until now no managed assembly lay beside a Windows program (all were inside the bundle). The rule
being step 3's, the step reported it with two proposals; **the manager's decision (a), 2026-11-08:** the check skips the
native-import rule for IL-only .NET assemblies (`check_packages.py`, `is_il_only`: a CLI header, data directory 14, whose
flags have the IL-only bit; a mixed assembly, native code in it, is still checked; the reason in the check's and
`check-packages.sh`'s comments and in step 3's entry above), and the self-test gained a break for it (the data library's
IL-only flag cleared must give "needs: Statventure.Game.Data.dll imports mscoree.dll"). With it, the four `2026.11.8.1` archives of the final code pass (the Linux ones run headless), and the self-test on the
four catches all of its 119 breaks (step 3's 118 and the new one).
**The packaged editor run** (`claude-work/m12/step6-packaged.sh`: the two Linux archives unpacked, the editor in a headless
cage on a scratch per-user folder, OpenAL Soft's null device): its first start with `--capture` drew "Where is Stat Fick?"
and ended with 0; with `--game` the unpacked game's folder, the single-file editor loaded the game's library from there
(2026.11.8.1 against its own 2026.11.8.1; 81 record kinds, the engine's 80 and the stand-in's) and drew the editor; the
player's real folders unchanged.

**Screens against Chromium:** five new fixtures, `game-where` (1000 × 640: the first start, the fixture's own folders, a
game's folder selected), `game-where-gone` (800 × 500, the window's smallest: the remembered folder gone, said), `game-folder`
(1000 × 640: Game folder… over the shell of an installed game) in `Editor/Game/GameScreens.cs`, `about` and `about-end`
(1000 × 640: the credits at their top and scrolled to their end, opened as the command does) in `Editor/About/AboutScreens.cs`;
`EditorFixture` gained an optional game (an installed game's look) and the harness a screen in the view's place. **The 19
earlier screens captured again** (the top bar gained Game folder… and About): the numbers before (the base's renders against the earlier captures) and after (the new captures), pixels past 12 levels,
on the GPU (lavapipe within a few pixels: the table in `claude-work/m12/step6/screens-table.md`): at 1× every one of the 19
gives the same count as before (`shell` 0, `dialog` 57, `browser` 28, `list-marks` 0, `list-unreadable` 0, `form-controls`
1,943, `form-controls-end` 2,065, `form-json` 0, `import-path` 57, `import-replace` 57, `import-again` 353,
`checks-problems` 425, `references-list` 390, `preview-effect` 448, `preview-model` 27, `preview-cutscene` 81,
`saving-save-as` 80, `saving-new-mod` 57, `saving-mod-info` 0); at 1.65 those whose top bar isn't under a dialog's layer gain 8
(the two new buttons' rounded corners: `shell` 32 → 40, `checks-problems` 4,340 → 4,348, 0.188 %, the closest to the 0.2 %
limit as before), the others the same. The new ones: `game-where` 28 (0.004 %) and 346 (0.020 %), `game-where-gone` 28
(0.007 %) and 332 (0.030 %), `game-folder` 28 (0.004 %) and 150 (0.009 %), `about` 61 (0.010 %) and 193 (0.011 %),
`about-end` 230 (0.036 %) and 547 (0.031 %), at 1× and 1.65; lavapipe within 11 pixels of these.
Captured with `claude-work/m12/step6-capture.sh` (step 13's: make-editor-references.sh's harness, flags and fonts in a
headless cage, the output 2400 × 2400 at both scales, one fixture per Chromium run).

**Tests:** 61 new (the suite 3,719, step 5's 3,658 and these): `Editor/Game/GameLookupTests` (12: a game's folder, not the game, a library that isn't the game's,
another version refused naming both and never loaded, dev matching dev, the kinds in a load context of their own on the
editor's engine, one build per run and another after a new start (a loader of the test's own), unloadable kinds, a library
changed between its reading and its loading, the dev setup's library built in the repo, `editor/game.json`, every problem's
words); `GameScreenTests` (6, the screen driven as the user drives it in the harness: its content and look, its start
folder, a remembered folder's problem, choosing the game, folders it refuses, the button's and the field's rules);
`GamePartTests` (4: off in the dev setup, a game of the run's build after the unsaved work is let go or kept, folders
refused in the browser, another build's restart); `EditorProgramTests` (4: a wrong command line on the terminal and in
Windows' message window, a game the command line names that can't be used, the editor run with its arguments, the
message window the engine's); `GameApplicationTests` (5, real applications in hidden windows: the first start's screen then
the game chosen, the remembered game, gone, and the command line's winning, the screen's zoom and close, Game folder…
making the session and view anew, `--capture` and the crash capture); `StandInKindTests` (3: the stand-in's entry in the
form and in New entry, unknown without the library, the packer's `dump`, `extract`, `pack` and `update` run on a pack of
it); `Editor/About/AboutTests` (3: the credits in the dialog, their scrolling within the window at 1000 × 640 and 800 × 500,
one at a time); and changed: `EditorArgumentsTests` (rewritten for the new command line: `--repo` with the repo's library,
`--game`, neither, `--capture` and `--frames`, 19 wrong command lines, the usage on each system), `ShellTests` (the top bar's
ten buttons and their places, the stylesheets' order with the credits' third, the engine's Lato at ui.pak's paths),
`SettingsTests` (the title before a view), `DevSetupTests` (the scratch repo with the library's build), the harness (a game
given, the screen) and the fixtures' record (an optional game), and the 10 new screen comparisons.

**Found:**
- *A hidden window's second present hangs under wlroots* (known, step 2): the in-process `--capture` test captures the first
  frame and counts the window minimized from the second update on (no frame acquired then); the 30-frame default is checked
  with the packaged program instead.
- *`flex: 1` (a 0 % basis) in a column flexbox of indefinite height* gave the About dialog's content no height in the engine
  (the dialog 102 px high); the spec and, as far as known, Chromium take a percentage basis there as the content's size.
  Not checked against Chromium; the dialog uses `flex: 0 1 auto`, which both lay out alike (its fixtures). An item for the
  engine's UI.
- *Fixtures taller than 720 px came out cut at 1×* in a capture at the headless cage's default output (1280 × 720): the
1× captures need the large output too (2400 × 2400, as M11's notes say for both scales).

**Mutations:** the step's rules, `~/.cache/stat-fick-build/claude-work/m12/step6-mutations.json` (made by `step6-make-mutations.py`, run
under the shared lock in a headless cage): 157 in the first run on the final base, 146 killed, 10 survived, 1 failed to
build (a mutation of the fixture's places that didn't compile, written again). Of the survivors, 5 were gaps closed by
tests (a library changed between its reading and its loading; a file named `packs` named in the problem; the message
window's capital; the screen's start at home, not the mods folder, when that is there; the recent place written by a
choice), 2 were redundant code, removed (an empty remembered folder refused, which the full-path rule refuses anyway; the
browser's lead line's own margin, which the dialogs' paragraph rule overrode), and 3 are accounted for: the library's
importers left out of the kinds, twice (the loader's and the session's: the stand-in has no importer, so nothing can
differ until the port's library has one), and the usage naming Linux's program on any system (only Windows differs: the
"Windows check"). Rerun (`step6-rerun.json`): the 5 gaps, the replacement of a redundant check and the rewritten mutation,
all 7 killed. The standing list: 155, 152 killed, 3 accounted for. Before the rebase on step 5 a first run, stopped at
its tenth mutation, had found one more redundant check (a file without metadata refused, which the catch of a reading
that fails does anyway), removed. **The packaging's rules** (`step6-publish-mutate.py`: each change applied, the game's
Linux package published and checked, or `editor.sh` run headless with `--capture` from a tree without the library's
Release build, and undone; the unchanged tree passing first): the library bundled into the program (the target off), the
library not linked, no `cover` line, the listing without it, `editor.sh` not building it: 5, all killed (the check's
"missing"/"extra"/"licence" problems; "the game's data library isn't built"). **The check's new rule** (`step6-rule-mutate.py`: each change to `check_packages.py` applied, the check or its
self-test run on the game's Windows archive, the change undone; the unchanged script passing both first): the rule dropped
(the data library's `mscoree.dll` import then a need: the check fails), every PE file counted IL-only (no native DLL's
imports checked: the self-test's "a library importing a DLL not there" uncaught) and any CLI header counted IL-only (the
flag not read: the self-test's new break uncaught): 3, all killed. Nothing written to the player's real
folders (`mutate.py`'s watch; the game folder missing and the Documents folder's top level the same before and after
each run).

**Full runs:** the full suite passes on the GPU (1 min 54 s) and on lavapipe (2 min 12 s), inside a headless Wayland
compositor under the shared lock, rebased on `60ee79b` (step 5's merge, `04dd980`, and the old game's port tasks 10–14,
nothing the engine builds; on `04dd980` the same, 1 min 56 s and 2 min 9 s): 3,719 tests, none skipped; the player's real
game folder stayed missing and the Documents folder's top level unchanged.

**Not checked:** Windows (the message window for a wrong command line, `Statventure Editor.exe`'s usage, `editor.ps1`
building the library); the editor in a real window by hand: the first start's screen, choosing the game's folder with the
mouse and the keyboard, Game folder… and its restart message, About scrolled with the wheel (the user's "to check by hand").

### Step 7: the release script and its text (2026-11-08)

- **Commands.** `Tools/Scripts/release.sh [--dry-run]` on the PC (where `gh` is logged in), from a clean checkout of a
  commit on GitHub's `main` (`main` itself after a pull, or a worktree on a commit of it); `release.sh --self-test` runs
  its tests. It makes the four archives of that commit, checks them, and leaves them on GitHub as a draft release; the
  user then opens the address `gh` prints (or the repo's Releases page), reads the draft, edits its text there if
  wanted, and publishes it, which makes the tag `v<version>` at the commit (the repo is private, so the release is too).
  `--dry-run` does all of it but the upload. Nothing here reads or writes the player's per-user folders.
- **The refusals (Q27).** The work tree clean: `git status --porcelain` empty, so no change staged or not and no
  untracked file (one in a project's folder would be built into the programs); git-ignored files (`release/`, the
  builds) don't count. The commit on GitHub's `main`: `git fetch origin +refs/heads/main:refs/remotes/origin/main`
  (forced, so GitHub's `main` as it is now, even after it moved back), then `git merge-base --is-ancestor <commit>
  origin/main`: any commit of `main`'s history, not only its tip (left to the step; chosen: a commit `main` has moved on
  from is still on it). git reaches GitHub with `GIT_TERMINAL_PROMPT=0 GIT_ASKPASS=/bin/false SSH_ASKPASS=` (the
  repo's push form), so a missing login fails instead of asking on the terminal or opening a password window. No `gh`
  on `PATH` is refused at once. No tests run (Q27).
- **The version (Q1, Q2; how the earlier releases are found, left to the step: chosen).** The date is the commit's
  committer date in UTC (`git log -1 --format=%ct`, the time `publish.sh` dates the archives with; a rebased commit's
  author date is older), `date -u +%Y.%-m.%-d`, never zero-padded (`2026.1.5`). The earlier releases of that date are
  GitHub's tags (`git ls-remote --tags --refs origin`; local tags don't count) **and the tags of GitHub's releases,
  drafts included** (`gh release list --limit 1000 --json tagName --jq '.[].tagName'`, read-only): a draft's tag is
  made only when it is published, so without the drafts a second run before the first draft is published would make a
  second `.1`. Only names of the form `v<date>.<n>` count (`n` from 1, no leading zero; `v2026.11.8`,
  `v2026.11.8.0`, `v2026.11.8.01`, `v2026.11.8.6-rc`, `v2026.11.80.1` don't); the fourth number is one more than the
  highest found, by number (`.11` after `.10`), not the count (so after a deleted `.1` under a `.2`, `.3`, never a
  second `.2`). The day the script runs plays no part. **A commit released twice gets the next number the second time
  (the manager's decision, 2026-11-08, (a), asked by the step):** Q2's fourth number counts the date's releases, and
  the design's "the same commit always gives the same version" holds for the date, which comes from the commit, not
  the day of the run; no refusal, as Q27 names the only two. (Not chosen: (b) refusing a commit that already has a
  `v…` tag or draft at it.)
- **The archives (Q28).** `publish.sh --version <v> --no-check` for the four, then `check-packages.sh --version <v>`
  (the four in `release/`, the Linux ones run headless), each failure named and stopping the release; each of the four
  archives must be in `release/`.
- **The release's text (Q12, Q13; its wording left to the step: chosen).** `Tools/Packaging/release-text.md`, Markdown
  as GitHub shows a release's text: the two downloads and what each is, with Q13's notice "Statventure Sandbox stands in
  for the game until the port"; the versioned folders (a new version beside the old); starting each on Linux (`tar
  -xzf`, `./stat-fick-<v>/stat-fick`, the editor likewise) and on Windows (Extract All, `Stat Fick.exe`, `Statventure
  Editor.exe`; unsigned, so SmartScreen's **More info** and **Run anyway**, Q14; no version details in the programs'
  Properties, the version in the folder's name, the editor's title and the logs, Q15); the editor asking "Where is Stat
  Fick?" on its first start and opening only the game of its own version (Q16, Q18); the player's folder on each system
  (`~/.local/share/stat-fick` or `$XDG_DATA_HOME/stat-fick`, `%APPDATA%\Stat Fick`) and what is in it (`saves/`,
  `mods/`, `logs/` with the crash files, `settings.json`, the editor's `editor/`); what they need (step 3's findings: a
  Vulkan 1.3 driver; Linux x86-64 with glibc 2.34 and GCC 12's libstdc++: Ubuntu 22.04, Debian 12, Fedora 36 or later,
  not RHEL 9 or its rebuilds; X11 or Wayland; Windows 10 or 11, 64-bit, the Visual C++ runtime shipped); the licences
  (Built with BRUTAL, `NOTICE.txt`, `licenses/`, each program's About). Its placeholders: `{{version}}` and the
  archives' names, `{{stat-fick-linux}}`, `{{stat-fick-windows}}`, `{{statventure-editor-linux}}`,
  `{{statventure-editor-windows}}`, filled from the names the upload gives; a `{{…}}` nothing fills is refused. The
  filled text is written to `release/release-text-<v>.md` and given to `gh`; the draft's text is the user's to edit
  (Q26), the template the next release's start.
- **The draft (Q26).** From the repo's root: `gh release create v<v> --draft --target <the commit's full hash> --title
  "Stat Fick <v>" --notes-file release/release-text-<v>.md release/stat-fick-<v>-linux.tar.gz
  release/stat-fick-<v>-windows.zip release/statventure-editor-<v>-linux.tar.gz
  release/statventure-editor-<v>-windows.zip`; `gh` prints the draft's address, and the script what comes next. An
  upload that fails part-way can leave a draft on GitHub, which holds the version's number: the script says to delete it
  before trying again.
- **The dry run's output (left to the step: chosen).** The version line (`release: 2026.11.8.1, the commit <hash>
  (<time> UTC), on GitHub's main`), the publish's and the check's own output, then `release: dry run, so no draft; the
  upload would be (from <repo>):`, the `gh` command as it would be typed (arguments with spaces in single quotes), and
  the release's text.
- **Its tests (`release.sh --self-test`, `Tools/Packaging/release_test.py`; not in `test.sh`, like Q28's package
  checks: no C#, nothing the solution builds).** Each test makes a scratch git repository with a bare one as its
  GitHub, holding copies of this tree's `release.sh`, `env.sh` and `release-text.md`, stand-ins for `publish.sh`
  (writing four stand-in archives), `check-packages.sh` and `test.sh`, and a stand-in `gh` first on `PATH` (an empty
  `gh` configuration, so even the real one would be logged out; it answers only the exact `release list` call and
  `release create`, recording the text it was given and which files were there); every stand-in records its
  arguments and folder; the run's time zone is UTC+14, so a local date is a day off. 31 tests, about 3 s: the version
  for a first release of a date (`.1`, with tags of other dates and forms, and a local tag, not counted) and a same-day
  second (`.2`), the highest by number (`.1`, `.9`, `.10` give `.11`), a draft holding its number, tags and drafts
  together, the commit's UTC date both ways across midnight, the committer's date, no zero padding, an older commit of
  `main`, an ignored file; the refusals: a changed file, a staged change, an untracked file, a commit not pushed, one
  pushed to another branch, GitHub's `main` moved back (the local `origin/main` still holding it), GitHub unreachable,
  its tags unreadable after a good fetch, its releases unreadable (`gh` logged out), no `gh`; a failed publish, check
  or upload, and a missing archive, each stopping it; the draft (`gh release create` with the tag, `--draft`, the
  commit as target, the title, the filled text and the four archives, from the repo's root; its address printed); a
  placeholder nothing fills; every placeholder filled; the text's required parts (each of the points above, the
  archives' names); the usage; git asking for no password (a scratch HTTP server answering 401, the script run on a
  terminal of its own with an askpass program set: no prompt, the askpass never called) and ssh given no askpass (the
  same release through an ssh stand-in that serves the scratch GitHub); and no test run by the release.
- **Run against the real repo (read-only):** a clone of GitHub's `main` at `77854d4` (origin GitHub's address; the
  step's three files copied in and listed in `.git/info/exclude`, so the tree is clean), `release.sh --dry-run` under
  the shared lock (`claude-work/m12/step7-real-dry-run.sh`; log `step7/real-dry-run.log`): `release: 2026.11.8.1, the
  commit 77854d4a639aee6ab0ace04d4f3c197b90571cc2 (2026-11-08 18:26:59 UTC), on GitHub's main` (GitHub had only the tag
  and release `old-game-2026-10-16`), the four published (step 3's sizes: 45.1, 41.3, 41.8 and 38.0 MB) and checked (the
  Linux two run headless), the text written and printed, and the upload printed: `gh release create v2026.11.8.1 --draft
  --target 77854d4a639aee6ab0ace04d4f3c197b90571cc2 --title 'Stat Fick 2026.11.8.1' --notes-file
  release/release-text-2026.11.8.1.md release/stat-fick-2026.11.8.1-linux.tar.gz
  release/stat-fick-2026.11.8.1-windows.zip release/statventure-editor-2026.11.8.1-linux.tar.gz
  release/statventure-editor-2026.11.8.1-windows.zip`; 22 s with the packages cached; GitHub's tags and releases the
  same after; the player's real game folder still missing. From the worktree itself (the branch's commit isn't on
  `main`): refused, "isn't on GitHub's main". **Found:** the clone first made through `/home/…` (on the PC `/home` is a
  symbolic link to `/var/home`) made `publish.sh` fail: MSBuild met the projects under both paths (the shell's
  `/home/…`, .NET's working folder `/var/home/…`), the sandbox's restore came out with no project references (its
  `project.assets.json` holding one package, ILLink), and `Publish.cs`'s licence check refused every text ("the program
  has no package Ahwoo.BRUTAL.*"); the release stopped there, as it should ("publish.sh failed"). The repo's own path
  holds no link, so its releases aren't touched; step 3's scripts are left as they are (an open item under step 3:
  resolving the path, `pwd -P` in `env.sh`, would likely end it; not tried).
- **Mutations** (`~/.cache/stat-fick-build/claude-work/m12/step7-mutations.json`, made by `step7-make-mutations.py`, run
  by `step7-mutate.py` under the shared lock in a headless cage: each change applied, the self-test run, which must
  fail, the change undone; `mutate.py` runs only `dotnet test`): 100 changes to `release.sh` (each refusal and its
  order, the fetch, the prompts, the date's source, zone and form, the tags' and drafts' reading, the pattern, the
  highest number, the publish's and check's calls and failures, the archives, the filling, the `gh` call's every part,
  the dry run) and to `release-text.md` (each required part, a placeholder written out or misspelt). 97 killed at first;
  the 3 survivors were gaps, closed: `SSH_ASKPASS=` left out (with `GIT_ASKPASS` set git never uses it, but ssh does for
  an ssh remote: the ssh stand-in's test), a failed `ls-remote` after a good fetch not refused (the stand-in failing
  from its second call) and `Stat Fick.exe` dropped from the Windows start line (the name is in the editor's paragraph
  too: the required phrase is now the start line's); all 100 killed when run again; nothing written to the player's real
  folders.

### Step 8: end to end, and the Windows items (2026-11-12)

- **Commands.** `Tools/Scripts/end-to-end.sh [--publish] --version <v> [--work <folder>]` takes the four archives of
  that version in `release/` (made first with `--publish`, which runs `publish.sh --version <v>` and so checks them;
  else checked first with `check-packages.sh --version <v>`: the Windows two checked, the Linux two run); `end-to-end.sh
  [--work <folder>] <stat-fick-<v>-linux.tar.gz> <statventure-editor-<v>-linux.tar.gz>` takes the two given (no check:
  the caller's; the break runs give broken ones). It compiles the shaders (for its own build, below) and runs
  `Tools/Packaging/end_to_end.py`, which prints one line per expectation, `ok` or `FAIL` with its name and what it saw,
  a few facts (the archives' sizes, the GPU, the newest logs' first lines, the commit) and `end to end: N of N
  expectations held in <s> s; ALL PASSED` (or `FAILED: n`, exit 1). A part of the flow that stops on a problem of its
  own is a failure ("<part>: the flow went through") and the other parts still run. Its pictures, logs and each start's
  output stay in `<work>/run/` (`captures/`, `runs/NN-<name>/`, `player/stat-fick/logs/`); `<work>/build` and
  `<work>/other-build` keep its two builds for the next run (incremental). The work folder is
  `$STATVENTURE_CACHE/end-to-end/` unless given. About 90 s on the PC's GPU and as long on lavapipe (the starts are
  mostly the splash's 2 s): 23 program starts. Needs Python 3 with Pillow (the cloud's setup installs `python3-pil`) and
  `cage` with `wlr-randr`, else `xvfb-run`.
- **A script, not a test project (left to the step; chosen):** the flow starts the programs from their archives, as
  `check-packages.sh` does (Q28: the package checks are scripts of their own, not in `test.sh`); only its in-process
  part is C#, one test of the suite (below), which the script runs built with the packages' version. **Not run by
  `release.sh` (chosen):** Q27 (the user's answer) is that the release script runs no tests, and the flow builds and
  runs one of the suite's tests and starts the programs; the dry run does what the release does but the upload, so it
  doesn't either. It is run by hand before a release (`end-to-end.sh --version <v>` on the release's archives, or with
  `--publish`).
- **Headless, scratch, silent (the Method's rules; chosen as follows):** every program start in a headless display of
  its own: `cage` on wlroots' headless backend with its output given 2400 × 2400 first (`wlr-randr --output HEADLESS-1
  --custom-mode`, as the editor fixtures' captures; cage makes every window its output's size less libdecor's title bar,
  so the pictures are 2400 × 2363 on the PC, the crash window's too, which is 640 × 240 on a desktop), or `xvfb-run -s
  "-screen 0 2400x2400x24"` where there is no cage (the cloud); `DISPLAY` and `WAYLAND_DISPLAY` taken out; a start that
  doesn't end within 240 s is killed and fails. The per-user folder is scratch and **one for both programs, as on a
  player's machine**: `XDG_DATA_HOME=<work>/run/player` for both and the editor's `--player-folder
  <work>/run/player/stat-fick`, so the stand-in finds the editor's mod in its `mods/` and both programs' logs share one
  `logs/`. `ALSOFT_DRIVERS=null` (OpenAL Soft's "No Output"). `HOME` stays the user's: the editor's "Where is Stat
  Fick?" browser starts at home and lists its folders (read only; the captures stay in the cache). The player's real
  folders (this machine's game folder, whole, and the Documents folder's top level, as `mutate.py` and
  `check-packages.sh` watch them) are compared before and after: one expectation.
- **The stand-in's expectations** (`stat-fick`): a start with `--capture-splash` and `--capture`: exit 0, the splash's
  first frame the BRUTAL logo on the dark ground (the ground most, the logo's yellow and white there), the page (its
  parchment most) captured after "the splash shown (2 s)"; `--about`: "the credits view opened", light text on the dark
  ground; `--crash` and `--crash-thread`: exit 1; one new crash file, its first line `stat-fick <v>, commit <hash>`, its
  second `crashed <time> on the main thread` or `on the thread "Sandbox crash"`, its fourth the run's log, the
  exception's message in it; the log's order (the crash file, the crash window opening, its capture, its closing);
  nothing of .NET's own ("Unhandled exception"); the crash window's picture (the ground, the field's border and Close in
  ember); and once the editor has made the mod, a start whose log names it (`[Data] mod <player>/mods/ember-rite.pak`)
  and whose page is drawn (the stand-in loads the mod's sounds into its music at its start, so a mod it can't read would
  crash it).
- **The editor's expectations** (`statventure-editor`): the first start, no `editor/game.json`: `no game's folder known:
  "Where is Stat Fick?"` in its log, the picture the screen (the dark ground and its ember accents: the listing's
  focused border and "Use this folder"); `--game <the unpacked game>`: exit 0, `the game's data library
  <game>/Statventure.Game.Data.dll: <v>, commit <hash>, 81 record kinds` (the versions one: the archives', the editor's
  own, the library's), `the game in <game>` in its log (the library's line there since the manager's decision on "Found"
  (1)), the picture the editor's shell (its panes' colour most); the remembered folder, `editor/game.json` written by
  the flow in the editor's own form (`{"version": 1, "folder": …}`; choosing it on the screen needs input, which the
  headless display has none of), naming a game of another version: the screen, its log saying `"Where is Stat Fick?":
  <folder> holds Stat Fick <v'>, and this editor works only with Stat Fick <v>`; that folder with `--game` (and
  `--capture`, so an editor that took it would end by itself): exit 2, no log (refused before any window), `Statventure
  Editor: <folder> holds Stat Fick <v'>, and this editor works only with Stat Fick <v>` and the usage naming
  `statventure-editor` on the terminal; the remembered folder naming the game: its library's line and `the game in
  <game>` in the log, the shell drawn.
- **The game of another version (left to the step; chosen):** a copy of the unpacked game whose
  `Statventure.Game.Data.dll` is built with the day's next release number (`<v'>` is `2026.11.12.2` for `2026.11.12.1`;
  for `dev`, today's `.1`): the library's project built with `-p:Version=<v'>`, the publish's property, into
  `<work>/other-build` (not a second publish: `release/` untouched, the build incremental). The editor reads the version
  from the library's metadata and never loads it.
- **The mod, made in-process with `--game` (chosen):** the packaged editor has no scripted way to make a mod, and the
  headless display has no input to drive its window with (that needs a virtual input tool; `ydotool` works through the
  kernel's `uinput`, which reaches the user's real session: not used). So the flow runs the suite's
  `Editor/EndToEnd/GameFolderModTests` (new, below) with `dotnet test --filter`, **built from this tree with the
  packages' version** (`dotnet build Tests/Statventure.Tests -p:Version=<v> --artifacts-path <work>/build`: its own
  artifacts, the repo's builds untouched), because the editor accepts only a game of its own version (Q18): the
  in-process editor and the unpacked game's library are then of one version, as a player's are. The test reads `--game
  <the unpacked game> --player-folder …` with the program's `EditorArguments`, finds the game (its library loaded from
  the unpacked folder, its version the editor's), makes step 14's mod through the UI (`ModFromScratchTests.MakeMod`: New
  mod and its info, the game's hall changed, a sound effect, a particle effect, a stage imported, a readme, a cutscene;
  Save refused, the problem fixed, saved) and, in the same mod, an entry of the game's own kind (New entry, "Stand-in
  item (stand-in.item)", `lantern`, its Title, Price and Rarity in the form: a value of the unpacked library's type),
  saves, opens About (the credits' "Version <v>") and draws it (`EditorScreenTests.Render`, the GPU) into
  `captures/editor-about.png`; then mounts the mod as the game mounts the player's mods over the unpacked game's packs
  and reads every entry back as the editor showed it; and copies `ember-rite.pak` into the flow's per-user `mods/`,
  where the packaged stand-in then mounts it. **The editor's About is captured in-process** for the same reason (the
  program has no option that opens it); the sandbox's credits are its own `--about`.
- **The logs kept:** every start that opens a window must leave exactly one new `<program>-<date>_<time>.log` whose
  first line is `<program> <v>, commit <40 hex>`, then the start time and the system, and "the splash shown (2 s)"
  before the program's own lines; the stand-in and the editor are each started until they have run 11 times (their
  expectations' starts, then plain `--capture` starts): after the 10th, 10 logs and the first there; after the 11th, 10
  kept, the first gone, the newest there, the crash files and the other program's logs untouched. One commit in every
  log, crash file and the game's library's line.
- **`GameFolderModTests` in the suite** (`Tests/Statventure.Tests/Editor/EndToEnd/`, 1 test, `[Collection("GPU")]`):
  without the flow's variables the game's folder is a scratch one laid out as the download (`GameTesting.MakeGame`: the
  repo's packs and the dev build of the library) and everything above is checked but the picture and the copy; with them
  (`STATVENTURE_END_TO_END_GAME`, `_VERSION`, `_MODS`, `_ABOUT`) it works on the unpacked game, checks that the editor's
  version is the packages', draws the About picture and copies the mod. Changed for it: `ModFromScratchTests.MakeMod`'s
  title assertion takes the editor's version (`Statventure Editor {EditorView.Version} — ember-rite.pak`, "dev" in the
  suite as before), and `EditorScreenTests.Render` is internal (was private). **Its mutations** (`mutate.py`,
  `claude-work/m12/step8/step8-test-mutations.json`, made by `make-test-mutations.py`, under the shared lock in a
  headless cage): 10, all killed: `--game`'s folder not kept; `--game` found as the dev setup; an installed game's packs
  folder named otherwise; the library's kinds not joined to the engine's; the game found without its library's file; the
  version check turned round; the player's mods not mounted; the mods folder's packs not found; the credits' version
  line naming the program; the editor's About with another version. Its branches for the flow (the version asserted, the
  picture, the copy, the flow's game folder) are killed by the flow's breaks (below); nothing written to the player's
  real folders.
- **Run (2026-11-12, `2026.11.12.1` published from this step's tree on `9bea4e0`):** `publish.sh --version
  2026.11.12.1`: the four archives (the game's Linux archive 45.1 MB, 47,330,147 bytes, 72 files; Windows 41.3 MB, 65
  files; the editor's Linux 41.8 MB, 43,829,307 bytes, 51 files; Windows 38.0 MB, 44 files; as step 6's),
  `check-packages.sh` ok on all four (the Linux two run). `end-to-end.sh --version 2026.11.12.1` (the check, then the
  flow) under the shared lock: on the GPU (AMD Radeon RX 9070 XT, RADV) 157 of 157 expectations held, 92 s for the flow,
  97 s in all; on lavapipe (llvmpipe, Vulkan 1.4, the flow's in-process build and test on it too) 157 of 157, 94 s, 99 s
  in all; the player's real game folder missing and the Documents folder's top level the same before and after both; and
  `end-to-end.sh --publish --version 2026.11.12.1` on the GPU (the publish, its check and the flow, 111 s): 157 of 157,
  the four archives byte for byte the first publish's. After the manager's decision on "Found" (1) (the editor's new log
  line): published again (the editor's Linux archive 43,829,196 bytes, its Windows one 39,816,404, the game's unchanged
  but for the commit in them), the editor's 631 tests on the GPU, and the flow on the GPU with `--publish`: 157 of 157,
  `--game`'s library line read from the log. The captures of a run (`<work>/run/captures/`): the stand-in's splash,
  page, credits, crash windows and page with the mod; the editor's "Where is Stat Fick?" (first start, and the
  remembered folder of another version, saying both versions in the problem colour), its shell on the game (`--game`,
  remembered) and its About over the finished mod (in-process).
- **The log read-back** (the run on the GPU): `stat-fick-2026-11-12_18-17-42.log` and on, each beginning `stat-fick
  2026.11.12.1, commit 9bea4e09f3e83d902d33ad867b02b09465f66c87`, `started 2026-11-12 18:17:42 +01:00`, `Bazzite (Unix
  7.3.3.1) X64, .NET 10.0.13`; the editor's `statventure-editor 2026.11.12.1, commit 9bea4e0…`; the crash files
  `stat-fick 2026.11.12.1, commit 9bea4e0…` with `crashed 2026-11-12 18:17:52 +01:00 on the main thread` and `… on the
  thread "Sandbox crash"`; the 11th start of each removed its first log (`stat-fick-2026-11-12_18-17-42.log`,
  `statventure-editor-2026-11-12_18-17-56.log`), 10 of each kept, the two crash files kept; the stand-in with the mod:
  `[Data] mod <work>/run/player/stat-fick/mods/ember-rite.pak`.
- **Found:** (1) **With `--game` the log didn't name the game's data library**: the editor finds and loads the game the
  command line names before `Run` opens the log (so that a game it can't use is refused before any window, step 6), so
  the line `the game's data library …: <v>, commit …, 81 record kinds` went to the terminal only and the log had `the
  game in <folder>`; with the remembered folder (found in `Load`) the line was in the log. The step reported it with two
  options; **the manager's decision (a), 2026-11-12:** `EditorApplication.LogGivenLibrary` (the one change to the
  editor's code in this step) logs a given game's library line (`--game`, and `--repo`'s library built in the repo) once
  the log is open, in the loader's words (the library's file, version and commit, the kinds counted), so both starts log
  alike; the flow reads `--game`'s line from the log too, and its break (below) shows it able to fail. (2) **Another
  release's data library can't load against this engine at all**: every assembly of a release carries its version (step
  1), so the library's reference to `Statventure` names the other version; with the version check off (a break, below)
  the start still ends with 2, saying "its kinds can't be loaded: Could not load file or assembly 'Statventure,
  Version=2026.11.12.2'": Q18's check gives the words, the loader is the backstop. (3) A crash rethrown at once (a
  break) has .NET 10 print "Unhandled exception." and the stack on Linux and then still run the run's `finally`: the
  crash window shows and the exit is 1, so only the print tells (step 5's reason to keep the crash and throw it after
  the run: on Windows that print is where Windows Error Reporting steps in, "Windows check", 9c). (4) MSBuild takes
  `SourceRevisionId` from the environment, so a publish with that variable set carries that commit in its programs (a
  break uses it); nothing in the repo sets it. (5) A "command form" of `mutate.py` doesn't exist (`mutate.py` runs
  `dotnet test` only; "Left to the steps" had it as an option): the breaks have a runner of their own, as steps 3, 5, 6
  and 7 did.
- **The expectations shown able to fail:** the first run, 31 breaks (`claude-work/m12/step8/step8-mutations.json`, made
  by `make-mutations.py`, run by `step8-mutate.py` under the shared lock: each applied alone, the Linux packages it
  touches published again or the good archive broken and packed again, `end-to-end.sh` run, the break undone; the good
  archives put back after): the game's `audio.pak` cut to half (the stand-in crashes at its start); the stand-in without
  its log name; both programs of another version (`2026.11.12.3`) in the `2026.11.12.1` archives; the log's start line
  worded otherwise; no splash; the page white; `--about` and `--crash` ignored; the crash thread named otherwise; the
  crash's message otherwise; no crash capture (the crash window waits; each such start killed after 240 s); the crash
  rethrown at once; the crash window without its ember; the editor's first start logged otherwise; the editor's accent
  not ember; the game's archive without its data library; `--game` read but not used; the remembered folder never read;
  no version check (Q18); the usage naming another program; the editor's package from another commit (`SourceRevisionId`
  in the publish's environment); the game mounting no mods; the test keeping the mod to itself; the test drawing no
  About picture; the in-process editor built without the packages' version; the other version's library not built; the
  in-process flow not built; 11, then 9 logs kept; every program's logs counted as one's own; the programs on the
  caller's per-user folder (a scratch folder standing in for the real one, so nothing real is written): **all 31
  killed**, 29 on every line expected, 2 on their own lines but not on further lines the list expected (the rethrow: the
  window and exit 1 came all the same; the version check: the start still ended with 2, "Found" (2) and (3)). The second
  run, 4 (`step8-rerun.json`): those two with the lines they break, the other version's library not built again (the
  refused start now has `--capture`, so an editor that takes the game ends by itself: 91 s, 330 s before), and the test
  ignoring the flow's game folder (a scratch game with the dev library: refused by the packages' editor): all 4 killed.
  The third run, 1 (`step8-logline.json`, made by `make-logline-break.py`), after the manager's decision on "Found" (1):
  the given game's library not logged once the log is open (the call left out, the editor's package published again):
  killed on `editor --game: the game's data library loaded … (the log)`, and on nothing else. Each run's own check of
  the real folders held, and this machine's game folder stayed missing.
- **The Windows items:** Engine-Plan.md, "Windows check", item 9 (a)–(i): both zips started from Explorer with no
  console, the names with spaces, the splash, the logs in `%APPDATA%\Stat Fick\logs` (10 kept), the crash window after
  `--crash` and `--crash-thread` with exit 1 and no Windows Error Reporting dialog, the editor finding the game ("Where
  is Stat Fick?", `--game`, the remembered folder, another version refused in the message window), a wrong command
  line's message window, SmartScreen on an unsigned `.exe`, the Visual C++ runtime on a Windows without its
  redistributable (Windows Sandbox), `publish.ps1` and `editor.ps1`, a mod made and loaded with the packaged programs.

## Status

**M12 done (2026-11-12; ticked in Engine-Plan.md's Milestones):** all eight steps on `main` (step 4 with its follow-up),
3720 tests passing on the GPU and on lavapipe. The two downloads per platform, each a self-contained single-file program
in a versioned folder (the game's, until the port the sandbox standing in, with its data library beside it; the editor,
which finds the game, refuses another version and loads the game's library), published and checked from Linux, the
BRUTAL splash and the credits in both programs, a log per run and a crash file and crash window after a crash, the
version in the logs, the editor's title and the credits, draft releases through `release.sh`, and the whole run end to
end by `end-to-end.sh`. At `2026.11.12.1`: the game's Linux archive 45.1 MB (72 files), Windows 41.3 MB (65), the
editor's Linux 41.8 MB (51), Windows 38.0 MB (44). The first real draft release is made only on the user's word. Not
checked: Windows (Engine-Plan.md, "Windows check", item 9, and items 1–8 before it); the programs by hand in a real
window on Linux; the flow in the cloud.

**Step 8 done (2026-11-12, branch `m12-step8` on `9bea4e0`, 3720 tests on the GPU and on lavapipe):** the packages end
to end. `Tools/Scripts/end-to-end.sh [--publish] --version <v>` (or two Linux archives named;
`Tools/Packaging/end_to_end.py`) unpacks the two Linux archives and starts both programs headless (a cage per start at
2400 × 2400, else Xvfb) on one scratch per-user folder, nothing audible, and holds 157 expectations: the stand-in's
splash, page, credits, `--crash` and `--crash-thread` (the crash file, the crash window's picture, exit 1, nothing of
.NET's own); the editor's first start ("Where is Stat Fick?"), `--game` (the game's data library loaded, the versions
one), the remembered folder, a game of another version refused (exit 2 naming both, and the screen saying it); a mod
made by the editor against the unpacked game (in-process: the suite's new `GameFolderModTests`, built with the packages'
version; step 14's mod and the game's own kind) and mounted by the packaged stand-in (its log names it); every start's
log (the program, version and commit first; after 11 starts 10 kept); one commit; the real folders unchanged. The
editor's About captured in-process (the program has no scripted way to open it). With `2026.11.12.1` (the game's Linux
archive 45.1 MB, 72 files; Windows 41.3 MB, 65; the editor's Linux 41.8 MB, 51; Windows 38.0 MB, 44; `check-packages.sh`
ok on all four, the Linux two run): 157 of 157 on the GPU (92 s) and on lavapipe (94 s), and with `--publish` (111 s,
the archives byte for byte the same). Not run by `release.sh` (Q27). Breaks: 31, then 4, then 1, all killed; the new
test's mutations: 10, all killed. Found: with `--game` the editor's log didn't name the game's library (it is loaded
before the log opens); the manager's decision (a), 2026-11-12: the editor logs it once its log is open
(`EditorApplication.LogGivenLibrary`), its break killed; another release's library can't load against this engine at all
(Q18's check gives the words); `Reference/editor-fixtures.json` on `main` was stale (step 6), committed as the tests
write it. The Windows items: Engine-Plan.md, "Windows check", item 9 (a)–(i). Not checked: Windows (item 9); the cloud
(Xvfb in place of cage, not run here); the programs by hand in a real window. ("How it works", step 8.)

**Step 6 done (2026-11-08, branch `m12-step6`, rebased on step 5's merge, 3719 tests on the GPU and on lavapipe):** the
editor as a download of its own: it finds the game (`--repo` with the library built in the repo, the manager's Q-A;
`--game`; the folder remembered in `editor/game.json`; else "Where is Stat Fick?", a page with the file browser built in,
picking a folder, Q16), refuses a game of another version naming both (the data library's version read from its metadata,
never loaded when refused, Q18), loads `Statventure.Game.Data.dll` from the game's folder for its kinds (once per run, the
manager's Q-C, Q19), draws in the engine's Lato (Q17), has Game folder… and About (the engine's credits in a dialog, Q30)
in its top bar, `--capture` and `--frames` (Q20), the usage naming `statventure-editor`, step 5's message window on Windows
(Q6) and its title with the version from the start. The stand-in library `Samples/Statventure.Game.Data` (one test kind,
`stand-in.item`) beside the sandbox's program, not in its bundle; the packer references it (Q-B). Five new fixtures, the 19
earlier ones captured again (at 1× each as before). The four archives pass `check-packages.sh`, after the check's one
change (the manager's decision (a): an IL-only .NET assembly's `mscoree.dll` import isn't a need; the data library is
the first managed DLL beside a Windows program). Mutations: 155 standing, 152 killed, 3 accounted for (the stand-in has
no importer; the usage on Linux), the 5 packaging rules and the check rule's 3 killed. Not checked: Windows; the editor by hand in a real
window. ("How it works", step 6.)

**Step 5 done (2026-11-08, branch `m12-step5`, rebased on `b08013b`, 3658 tests on the GPU and on lavapipe):** the BRUTAL
splash in `Application.Run` once the window and device are made, before the sound and `Load`, on a program's own run (a
log name; `ApplicationOptions.Splash`, on by default; tests' applications show none); the crash window after the teardown
for a crash on the main thread or another (`Core/CrashWindow.cs`: the error, the crash file's path selected with Copy,
"Open logs folder", Close; 640 × 240 in the dark look and the engine's Lato), the terminal's message when it can't be shown,
then exit 1; the crash kept and thrown only after the run's end (no more "Unhandled exception." from .NET, no abort);
`MessageWindow` for step 6; the sandbox's About (the engine's credits), `--crash` and `--crash-thread`, `--capture-splash`,
`--about`, a crashing `--capture` run ending with the crash window's picture. Four fixtures at 1 and 1.65 within the paint
limits (at most 0.143 % beyond 12 levels). Mutations: 67, 66 killed, 1 killed by hanging the next test (checked by hand);
the sandbox's 9 by its own check. Not checked: Windows; the splash, the crash window, Copy and "Open logs folder" on a real
desktop. ("How it works", step 5.)

**Step 3 done (2026-11-08, branch `m12-step3`, rebased on step 4's merge; the suite unchanged by it, 3511 tests on
the GPU and on lavapipe before the rebases):**
`Tools/Scripts/publish.sh [--version <v>]` (and `publish.ps1`) makes the four archives into `release/` through one
.NET file-based program, `Tools/Packaging/Publish.cs` (self-contained single-file publishes for Linux and Windows made
on Linux, symbols embedded with `/_/` paths, the executables renamed per M12-2, `packs/`, `NOTICE.txt` and 28 licence
texts, the Visual C++ runtime's three DLLs on Windows, no `.pdb`), then `Tools/Scripts/check-packages.sh` checks them
(the listings in `Tools/Packaging/listings/`, the packs, the texts, the runtime DLLs, the formats and the GUI
subsystem, the files inside the single-file program, its symbols and version, every native library's needs, every
file's licence; the Linux programs run headless on a scratch per-user folder). Sizes (2026.11.8.1): the game's Linux
archive 45.1 MB (71 files), Windows 41.3 MB (64), the editor's 41.8 MB (51) and 38.0 MB (44). The check's self-test:
118 breaks, all caught; the publish's rules: 45 mutations, 44 killed, 1 accounted for (a version not of Q1's form
refused by the check's name rule too), and 5 tests of its guards. `editor.ps1` for Windows. Found: libKtx and
libopenal need GCC 12's libstdc++ (`GLIBCXX_3.4.30`), so Fedora 36 rather than 35 and no RHEL 9; the Visual C++
runtime's own licence forbids shipping it with a program, Visual Studio's lets licensed Visual Studio users do it:
**the user's answer (2026-11-08): they ship**, the user a licensed Visual Studio user under its Distributable Code
terms; built from a clone of the GitHub repo, Source Link named the private repo's address in the programs' symbols:
**the user's answer (2026-11-08): left out** (`-p:EnableSourceLink=false`, and a check rule that the repo's own
assemblies and their symbols name no `github` and hold no Source Link entry). From a clean tree (a fresh clone): the
four made and checked, byte for byte the worktree's.
Not checked: Windows (`publish.ps1`, `editor.ps1`, the zips), the cloud's own SDK publishing for Windows. ("How it
works", step 3.)
**Step 4's follow-up done (2026-11-08, branch `m12-step4-credits` from `77854d4`, 3626 tests on the GPU and on
lavapipe):** Chromium (its search field images, `licenses/Chromium-LICENSE.txt`) credited; the credits' tests read the
shipped set from `Tools/Packaging/licenses.txt` and the texts kept there (both directions: every shipped text named,
every named text shipped); `credits`, `credits-end` captured again. Mutations: 8, all killed. ("How it works", step 4.)

**Step 4 done (2026-11-08, branch `m12-step4`, 3625 tests on the GPU and on lavapipe):** the logo's check found
the branding page's 4096 × 896 files newer than BRUTAL's 712 × 168 copies and stopped; the user's decision (a)
("Decided (the logo's file)"): the page's white logo at the approved 696 CSS px, Chromium's shrunk images ported first
(its own commit: cc's decode cache, Skia's mipmaps and legacy bilinear sampling, premultiplied images, Blink's fitted
`<img>`; two image fixtures; every existing render the same). Then `UI/EngineFonts` (ui.pak's Lato bytes, a font
library or a mount), `UI/Splash` (Q29: 2 s, skippable from 1 s, on the document's clock) and `UI/Credits` (Q30:
the program and version, `NOTICE.txt` verbatim, 36 components checked against the shipped texts, FreeType's credit,
OpenAL Soft's source, the pointer to `licenses/`), five fixtures against Chromium at 1 and 1.65 within the paint
limits. Mutations: 96, 94 killed, the 2 survivors gaps closed and killed when run again. Not checked: Windows; the splash and the credits in a real window (steps 5 and 6).
("How it works", step 4.)
**Step 7 done (2026-11-08, branch `m12-step7`; scripts and data, nothing the solution builds):**
`Tools/Scripts/release.sh [--dry-run]` refuses a work tree that isn't clean or a commit not on GitHub's `main` (fetched
now), and runs no tests (Q27); takes the version from the commit's UTC date and one more than the highest of that date's
releases on GitHub, tags and drafts (Q1, Q2); publishes and checks the four; fills `Tools/Packaging/release-text.md`
(what a readme would have said: the downloads, the stand-in, starting each program, SmartScreen, the missing version
details, the editor's question, the player's folders, the system needs, the licences; Q12–Q15) and makes the draft with
`gh release create v<v> --draft --target <commit> --title "Stat Fick <v>"` and the four archives (Q26); `--dry-run`
prints the upload and the text instead. Tests: `release.sh --self-test`, 31 in scratch repositories with stand-ins.
Mutations: 100, 97 killed at first, the 3 survivors gaps closed (ssh's askpass, a failed tag read after a good fetch,
the Windows start line's program name) and all 100 killed when run again. Against the real repo (read-only):
`2026.11.8.1` for `77854d4`, the four published and checked, the upload printed, GitHub's tags and releases unchanged;
the publish fails when the repo is reached through a symbolic link (`/home` → `/var/home`), reported, the repo's own
path holding none. Not checked: a real draft (the first is made only on the user's word); Windows (nothing of it runs
there). ("How it works", step 7.)

**Step 2 done (2026-11-08, branch `m12-step2`, 3544 tests on the GPU and on lavapipe):** one log per run in the
player's `logs/` (`stat-fick-2026-11-08_14-03-22.log`, the editor's `statventure-editor-…`) with its first lines (the
program, version and commit from step 1's `ProgramInfo`, the start time, the system); the last 10 runs per program and
the last 10 crash files kept, only those name forms touched; a crash file for every crash, on the main thread (the
start, `Load`, `Update`, `Render`, `Unload`, the teardown) or any other thread (.NET 10's unhandled-exception handler:
the audio's threads, the thread pool, any other), written before the teardown, the crash then handed to the main loop
so the run ends in order (the pending saves written), its message on the terminal (Q24); `Application.Crash` for step
5's window; tests' applications write nothing unless they ask (the manager's decisions (1), (2), Steps, step 2).
Mutations: 100, 93 killed by mutate.py's reading, 6 killed by crashing the test host (mutate.py reads only the
summary line, which an aborted run prints as "Passed!" with fewer tests; each shown aborted when run again), 1
accounted for (the crash file's flush to the disk can't be seen by a test). Not checked: Windows. ("How it works",
step 2.)

**Step 1 done (2026-11-08, branch `m12-step1`, 3511 tests on the GPU and on lavapipe in invariant mode):** the dev
default `dev` and a release's `-p:Version`, the commit from git by the SDK's Source Link (a build without git says
`dev`, no commit), `Core/ProgramInfo` (name, version, commit) for the later steps, the version in the editor's title
(Q3), invariant globalization for every project (Q8); one rule needed ICU (`ImportNames.Fit`'s accents), fixed by
the manager's decision (a) with HarfBuzz's decompositions; `EngineInfo.Version` stays 0.1.0. Mutations: 36, 34
killed, 2 accounted for (in the tests' dev build the editor's version is `dev`, so the title given the literal `dev`
can't differ; every assembly of one build carries the same version, so the editor's version read from the engine's
assembly can't differ). Not checked: Windows. ("How it works", step 1.)

**M12's design decided (2026-11-08): the main questions (M12-1–M12-12), the detail round (Q1–Q28) and the two
questions Q11's answer raised (Q29, the splash; Q30, the credits view) answered by the user, M12-9 clarified; nothing
is open; the eight steps are the plan, none started.** Nothing implemented.

**M12 started (2026-11-08):** the decisions recorded (M12-1–M12-12), what exists checked against the code, the
method, the design proposed with 28 questions for the user, seven steps proposed. Found by the survey (each in
"What exists"): the build publishes only the sandbox, framework-dependent, with every runtime's natives; nothing
publishes the editor; the executable takes the assembly's name; no version anywhere but the engine's own; no
program writes a log file; a crash leaves `Main` after the window is gone, so a crash window needs a window and
device of its own; the editor's fonts are the game's `ui.pak`'s, so an editor downloaded apart can't draw before it
knows the game; BRUTAL's Windows libraries need the Visual C++ runtime, which nothing ships; a Windows `.exe` built
on Linux has no version resources (NETSDK1074); the Linux libraries need glibc 2.34; the BRUTAL licence asks for
`NOTICE.txt`, the copyright in the credits and the logo before the program's primary function (its README waives
the logo for non-profit projects, the licence text names written permission).

**The detail round answered (2026-11-08):** Q1–Q28, all as recommended except Q1 (always four numbers), Q11 (the
logo splash and the credits now) and Q12 (no readme, the release's text instead); M12-9 clarified (no copy of the
data library in the editor's download). The design and the steps follow them: an eighth step (4, the engine's
embedded Lato and BRUTAL logo, the splash and the credits as engine pieces), the crash window joined with the
splash in step 5, the release's text carrying what a readme would have said. The logo is BRUTAL's own
`docs/branding/` files (in the BRUTAL source, not in any package); step 4 checks them against the branding page.
