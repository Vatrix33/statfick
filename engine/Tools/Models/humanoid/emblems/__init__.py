# The nine race emblems (Docs/Game/Hero.md, "The nine seals as 3D emblems": medallions in relief on nine new motifs,
# Q35-Q37; GM2 step 10, "How it works"): the user chose one of two sketched motifs per race (Q66) and approved the
# medallions as renders.
#   chosen.json   the chosen motifs' outlines (the models are raised from them), written by chosen.py from motifs.py
#   medallion.py  the builder (the manifest's emblems/<name>: Assets/Models/humanoid/emblems/<name>.glb, models.pak's
#                 models/humanoid/emblems/<name>; the name is a race's `emblem`, Q44)
#   palette.py    the emblems' colours from a race's accent (GM2-P31; the game's rule is Figures/Emblems.cs), plain Python
#   motifs.py, sketchkit.py, chosen.py
#                 how the motifs were sketched (two candidates per race) and written out: for the record and for redrawing
#                 a motif (Pillow draws the sketches)
# The pictures in ui.pak are made from Blender's renders of the emblems by the game's tests (Docs/Game/Hero.md, "How it
# works", step 10).
