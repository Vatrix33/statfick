# UI resources

- `search_cancel.png`, `search_cancel_pressed.png`: the search field's cancel button images from
  Chromium 152 (`third_party/blink/public/default_100_percent/blink/`), drawn as Chromium draws
  them. Chromium is under a BSD-style license (Copyright The Chromium Authors).
- `lato-regular.ttf`, `lato-bold.ttf`, `lato-ofl.txt` (M12 step 4, `UI/EngineFonts.cs`): the old game's
  `objects/fonts/Lato-Regular.ttf`, `Lato-Bold.ttf` and `Lato-OFL.txt`, byte for byte (the files
  `Tools/Scripts/make-ui-pack.sh` packs into ui.pak's `ui/fonts/`). Lato is under the SIL Open Font License 1.1
  (`lato-ofl.txt`; Copyright (c) 2010-2015, Łukasz Dziedzic, with Reserved Font Name Lato; the text's "Ł" is
  mis-encoded as it came).
- `LOGO_BRUTAL_WHT.png` (M12 step 4, `UI/Splash.cs`): BRUTAL's white logo, 4096 × 896, byte for byte as
  ahwoo.com/brutal-branding (a 301 to github.com/ahwootech/brutal-branding) offers it at commit `5d38373a24`
  (2026-10-21), SHA-256 `59768c2ab60fc835af29b1d686e5a76604e959fb4e84adf2bfb7fd26e7355d0c`. Used under the BRUTAL
  License v1.0, 2.2: shown before the program's primary function, unaltered except for scaling, legible, no less
  prominent than other parties' logos (`Docs/Packaging.md`, "Decided (the logo's file)").
- `brutal-notice.txt` (M12 step 4, `UI/Credits.cs`): BRUTAL's `NOTICE.txt`, byte for byte as every BRUTAL package
  (and the branding repository) gives it, shown verbatim in the credits (the licence's 2.1 and 2.3).
