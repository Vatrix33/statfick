# The clips' keying kit (GM2-P23; Docs/Game/Hero.md, "How it works", "The keying kit"): the scripts the family's clips are
# keyed, baked, measured and shown with, so a clip can be keyed again from the repo alone. The 34 approved clips' data
# (clips/<name>.json) is what this kit bakes, byte for byte (keyed.json, make-humanoid.sh keys --check).
#   The sources (plain Python): poses.py (the ready stance, a key, dense keys, a source), grips.py (an arm's grip from its
#   directions; the weapon stances), idle.py, and the clip modules standing.py, moving.py, reacting.py, melee.py,
#   acting.py, each giving its clips' keying sources; sources.py writes them and keeps keyed.json.
#   The figure and the bake (inside Blender 5.2): figure.py (the man the clips are keyed on, by the family's builders),
#   solver.py (an arm solved for a grip), bake.py (a source posed key by key and read back as the clip's data; the clip
#   played on the figure for the ground's measure and the pictures), props.py (the pictures' stand-in weapons), run.py.
#   The checks before anyone sees a clip: measure.py (the arms, the jumps, the ground; measures.json, bounds.json) and
#   video.py (the clip on a floor).
# Run through Tools/Scripts/make-humanoid.sh keys, measure and clip-video.
