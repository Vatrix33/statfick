# A hero's look (look.hero's JSON form, a parameter set: race, sex, body, parts, colours) assembled in Blender (GM2-P8,
# GM2-P9; Docs/Game/Hero.md, "Assembly in Blender"): step 5 writes it here. The body, its parts bound by Armature
# modifiers to the body's armature as the tabard is (Docs/Animation.md, "Rendered gear"), rigid parts on their sockets,
# the palette from the look's colours; rendered with render_scenes.py's rules in the game's look (look.py), and compared
# with the engine's figure built from the same look (ReferenceLimits.M6) for the presets and a fixed list of seeded
# random looks. render.py already renders a body with garments and items in a look (--model, --garment, --item); what
# step 5 adds is the reading of a look.hero into those.
