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
script of their own (Q28). Q11's answer raises two questions of its own, Q29 and Q30, still to ask.

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
  `CONTRIBUTING.md` names brutal@ahwoo.com for a waiver.
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

The design the steps work from, decided except where Q29 and Q30 (below) are named.

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
  that date (`v2026.11.8.*`), so the same commit always gives the same version.
- **One place:** `Directory.Build.props` holds the dev default; a release's version comes in as `-p:Version=…` from
  the release script. Each program reads its own version and commit from its program file at run time (the
  informational version, "<version>+<commit>").
- **Where it shows (Q3):** the editor's title, "Statventure Editor 2026.11.8.1 — ember-rite.pak"; the first lines
  of every log and every crash file give the version and the commit; the game's title stays the game's name (the
  stand-in's "Statventure Sandbox"); the credits view (Q30) names the version too. The commit shows in no window.
- `EngineInfo.Version` (the engine's own, 0.1.0, given to Vulkan) is not the program's version (left to the
  steps whether it follows).

### The BRUTAL logo and the credits (Q11; Q29, Q30)

The BRUTAL licence's 2.1–2.3 met in M12 for both programs (the ported game shows them again in its own screens):
- **`NOTICE.txt`** beside each program (2.3, Q10).
- **The logo before the program's main function (2.2):** a splash shown by `Application.Run` once the window and
  device are made and before `Load` (so before the sandbox's page and before the editor's first screen, the
  "Where is Stat Fick?" screen included), at every start; not before the crash window. The logo: BRUTAL's own
  `docs/branding/LOGO_BRUTAL_WHT.png` and `LOGO_BRUTAL_BLK.png` (712 × 168, RGBA), in the BRUTAL source in the
  build cache (its README shows them as the logo); not in any BRUTAL package (those carry only a 32 px icon). The
  licence names ahwoo.com/brutal-branding as the logo's source: step 4 checks the repo's files against that page
  and stops to ask if they differ. Embedded in the engine, shown unaltered except for scaling. Its look, length
  and skipping: Q29. Tests' applications don't show it (an option the programs leave on).
- **The credits (2.1):** one engine view of the credits used by both programs: the program and its version,
  BRUTAL's copyright notice ("BRUTAL game development framework is Copyright (c) 2024 Ahwoo Limited.") and its
  `NOTICE.txt` text verbatim, FreeType's required credit ("Portions of this software are copyright © <year> The
  FreeType Project (https://freetype.org). All rights reserved.", FTL section 2), and each other component with
  its licence and copyright line; where each program opens it and the exact list: Q30.

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
  its own screens later): "The BRUTAL logo and the credits" above; their details are Q29 and Q30.
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

## Questions for the user (Q29–Q30), from Q11's answer

Not asked yet; steps 4–6 need them (step 4 builds both pieces, step 5 shows the splash, steps 5 and 6 open the
credits).

- **Q29. The BRUTAL splash: its look, length and skipping.** (a) The white logo (`LOGO_BRUTAL_WHT.png`) centred on
  the dark ground of the crash window's look (Q23), at its own size (scaled down only to fit the window), for 2
  seconds at every start of both programs; a key or a click skips it after its first second; nothing else on it.
  *Recommended:* legible and unaltered as the licence asks, short, and in the look the engine already owns. (b) The
  same, never skippable. (c) The black logo on a light ground (the game's parchment), 2 seconds, skippable after
  the first.
- **Q30. The credits view: where each program opens it, and what it lists.** (a) One engine view for both: the
  program's name and version; "Built with BRUTAL", BRUTAL's copyright notice and `NOTICE.txt` text verbatim; then
  each component with its licence and copyright line (FreeType with its required credit, GLFW, OpenAL Soft under
  the LGPL with where its source is, HarfBuzz, stb, VMA, zlib, libpng, gli, KTX, .NET, the fonts Lato and STIX Two
  under the OFL); and that the full texts are in `licenses/`. The editor opens it from an "About" button in its top
  bar (a dialog: every editor screen is captured again, the top bar changing), the sandbox from an "About" button
  on its page. *Recommended:* one list kept beside the licence files, each program offering it where its own
  commands are. (b) The same view, opened with F1 in both instead of buttons (no top bar change). (c) As (a), but
  the sandbox shows the credits as a section at the foot of its page instead of a button.

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

**Order:** **wave 1:** 1, 2, 3 and 4 side by side (4 needs Q29 and Q30 answered); **wave 2:** 5 (after 2 and 4), 6
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
   files). Independent; needs Q29 and Q30.
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

## How it works

Filled in as the steps are done.

## Status

**M12's design decided (2026-11-08): the main questions (M12-1–M12-12) and the detail round (Q1–Q28) answered by
the user, M12-9 clarified; eight steps are the plan, none started; Q29 and Q30 (the splash's look and the credits
view, from Q11's answer) still to ask before steps 4–6.** Nothing implemented.

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
