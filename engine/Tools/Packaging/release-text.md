Stat Fick {{version}} for Linux and Windows: the game and its editor, each a download of its own. **Statventure Sandbox stands in for the game until the port:** the game's download holds the Statventure engine's sandbox where Stat Fick will be.

## The downloads

- **The game:** `{{stat-fick-linux}}` for Linux, `{{stat-fick-windows}}` for Windows. Until the port, the program in it is Statventure Sandbox, standing in for Stat Fick.
- **The editor:** `{{statventure-editor-linux}}` for Linux, `{{statventure-editor-windows}}` for Windows. The Statventure Editor, for making mods; it needs the game's download of the same version.

Each holds one folder named with its version (`stat-fick-{{version}}`, `statventure-editor-{{version}}`), so a new version unpacks beside the old one. Keep the folder whole: each program needs the files beside it.

## Starting them

**Linux:** unpack the archive (`tar -xzf {{stat-fick-linux}}`) and run `stat-fick` in its folder (`./stat-fick-{{version}}/stat-fick`); the editor likewise, `statventure-editor` in `statventure-editor-{{version}}`.

**Windows:** extract the whole zip (Extract All; a program run from inside the zip can't find its files) and run `Stat Fick.exe` in its folder; the editor likewise, `Statventure Editor.exe`. The programs are unsigned, so SmartScreen may stop their first start ("Windows protected your PC"): choose **More info**, then **Run anyway**. Their Properties show no version details; the version is in the folder's name, in the editor's title and in the logs.

**The editor needs the game:** on its first start it asks "Where is Stat Fick?"; choose the game's folder (`stat-fick-{{version}}`, the one holding `stat-fick` or `Stat Fick.exe` and `packs/`). It remembers the folder, and asks again when that folder no longer holds the game (after a new version was unpacked beside the old, for example). It opens only the game of its own version.

## Your files

Settings, saves, mods and logs are kept in your own folder, never in the program's, so a new version finds them:

- Linux: `~/.local/share/stat-fick` (or `$XDG_DATA_HOME/stat-fick`)
- Windows: `%APPDATA%\Stat Fick`

In it: `saves/`; `mods/`, the mods the editor makes, which the game loads; `logs/`, a log of each run (the last 10 kept) and, after a crash, a `crash-….txt` file to send with a report; `settings.json`; and the editor's own settings in `editor/`.

## What they need

- A graphics card with a Vulkan 1.3 driver.
- **Linux:** 64-bit (x86-64), with glibc 2.34 and GCC 12's libstdc++ or later: Ubuntu 22.04, Debian 12, Fedora 36 or later (not RHEL 9 or its rebuilds); X11 or Wayland.
- **Windows:** Windows 10 or 11, 64-bit. The Visual C++ runtime comes with the programs.

## Licences

Built with BRUTAL. `NOTICE.txt` and `licenses/` in each folder hold the licences of the parts the programs are made of; each program's About lists them.
