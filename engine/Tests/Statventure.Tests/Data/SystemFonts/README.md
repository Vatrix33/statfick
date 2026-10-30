# System fonts for the tests

The fonts fontconfig picked on the PC the Chromium 152 references were captured on (Fedora 44,
2026-10), for the tests' generic families (Chromium's Arial, Times New Roman and Monospace) and
for characters the game's fonts lack. `fontconfig.json` holds fontconfig's answers to each query
the tests make; `Infrastructure/SystemFonts.cs` reads it, so every machine resolves fonts the
same way without fontconfig. Re-record on that PC with `STATVENTURE_RECORD_FONTS=1`.

All are under the SIL Open Font License 1.1 (Noto Emoji also Apache 2.0); see `licenses/`.
Liberation Sans/Serif 2.1.5, Noto Sans Mono/Math/Symbols 2 (2025-12-01), Noto Emoji (2026-09-21),
Noto Sans CJK 2.004, Adwaita Sans and Adwaita Mono 50.0.
