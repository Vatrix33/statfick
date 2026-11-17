# The two detail levels a model is built at (GM2-P20, the manager, 2026-11-16; the user approved the lighter figures the
# same day), both from one sculpt by the same scripts; the screen that builds the actor chooses the level, the engine has
# no level-of-detail machinery:
#   "full"    the sheet's, the maker's and the carousel's figure (one on screen, seen large, no scenery): lighter than
#             the first sculpt only where it can't be seen. GM2 ships this level.
#   "battle"  the stage's figure (up to sixteen on screen, small, casting shadows). Proven and measured on the first
#             humanoid (the tests' data); every body's battle models are built when the battle comes (GM5).
# The numbers: the body's and each hand's quads (lighten.light_body; `body_limit` how far the rest of the body may stray
# from the sculpt, 12 mm when not given), the head's quads and how much its face is enlarged for the remesh (`face`, `face_limit`;
# lighten.light_head), the hair's fineness (forms.build_hair: its cap cut from a quad sphere of `cap_cuts`,
# locks of `lock_sides` sides subdivided `lock_levels` times), a skirt's faces around and rings down (outfit.Maker.skirt),
# and how far apart the mail's rows of rings are drawn (gear.chain_shirt, metres).
LEVELS = {
    "full": {"body": 6500, "hand": 1000, "head": 4500, "face": 1.9,
             "hair": {"cap_cuts": 14, "lock_levels": 1, "lock_sides": 10},
             "skirt": {"n": 72, "rings": 9}, "rows": 0.022},
    "battle": {"body": 1900, "hand": 150, "body_limit": 0.025, "head": 900, "face": 1.6, "face_limit": 0.012,
               "hair": {"cap_cuts": 8, "lock_levels": 0, "lock_sides": 6},
               "skirt": {"n": 36, "rings": 5}, "rows": 0.044},
}
