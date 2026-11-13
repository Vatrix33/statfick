# The icons' generator

It writes the outlines and the box table in `Assets/UI/icons/` (GM1 step 3; `Docs/Game/Foundation.md`, "The symbols
as vector icons (GM1-7)"). The game draws those files. Nothing in the build, the tests or the packaging runs this
folder. It holds the icons' style, and later milestones add their icons with it.

- `iconlib.py`: the geometry and the style, "Engraved" (the user's pick, 2026-11-13). A filled shape is an ink contour
  with its shadow half solid and its lit half open. An open shape is a line that gets heavier on its shadow side. Thin
  parts are solid ink. The light comes from the upper right. The weights are in `W`, in 1/1000 em, the same for every
  icon.
- `figures.py`: one figure per symbol, built at about the size of the glyph it replaces.
- `build.py`: places each figure where the old glyph's ink sat and writes `<name>.svg` and `icons.json`. Each icon's box
  comes from the repo's copy of the font Chromium drew the glyph with (`Tests/Statventure.Tests/Data/SystemFonts`).

It needs Python 3 with shapely, fonttools and pillow (pillow only for `--preview`). No virtual environment is kept in
the repo; make one anywhere, for example `python3 -m venv <dir> && <dir>/bin/pip install shapely fonttools pillow`. The
committed outlines were written with shapely 2.2.0 and fonttools 4.68.0 on Python 3.15.

    <python> build.py ../../../Assets/UI/icons            # write the outlines and icons.json
    <python> build.py --check ../../../Assets/UI/icons    # compare the committed files with what it writes, byte for byte
    <python> build.py --fit                               # each figure's ink box against the old glyph's
    <python> build.py --preview <file.png>                # every icon beside the old glyph

The committed files must be exactly what `build.py` writes, so run `--check` after any change here. Another shapely
or GEOS version may round a coordinate differently; if `--check` then reports a difference, look at the changed icons
on the sheet (`Tools/Scripts/make-icon-sheet.sh`) before committing them.

To add an icon:
1. Add the symbol to `ICONS` in `build.py`, with the font the old game's Chromium drew it with
   (`Tools/Scripts/make-icon-sheet.sh --uses` measures every use again).
2. Add its figure to `figures.py`.
3. Write the files, then show it on the sheet at every size the game uses it.
