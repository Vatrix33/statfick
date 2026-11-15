# The Blender script family `humanoid` (GP-4, GM2-P9; Docs/Game/Hero.md, "The Blender script family `humanoid`" and
# "How it works", step 3a): every humanoid model of the game made by script in Blender 5.2, its outputs committed.
#   rig.py        the skeleton and its sockets (step 4)
#   bodies/       each body: 9 races × 2 sexes × 3 builds (steps 4 and 9)
#   parts/        the maker's parts by family, fitted by script to the bodies (steps 4 and 9)
#   clips/        the clip set, keyed by script on the human average body (step 4)
#   emblems/      the nine race emblems (step 10)
#   gear.py       the sample gear: a sword, a shield, a chain shirt (step 4)
#   look.py       a look's numbers and the palette slot names; the look files (looks/) both sides read
#   assemble.py   a look.hero assembled in Blender, for the comparisons with the engine's figures (step 5)
#   build.py      builds every model manifest.json names (--check: rebuilt and compared with what is committed)
#   render.py     renders a figure or a scene file in a look and views (with compose.py)
#   sample.py     the frame's sample, a cube figure on two bones (test data)
# Run through Tools/Scripts/make-humanoid.sh (build, check, render, look). The scripts run inside Blender and import
# their modules from this folder and Tools/BlenderReference (make_models' export and helpers, render_scenes' look).
