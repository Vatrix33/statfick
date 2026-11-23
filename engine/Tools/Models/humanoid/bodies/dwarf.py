# The dwarves' bodies (GM2 step 9c): the human's proportions with the old dwarves' ratios (the old build: legs 17 of 24, torso
# 25 of 27, head 21 of 20, shoulders 13.5 of 11.5, hips 11.5 of 10, arm 19 of 22, arm width 9.2 of 8, leg width 10 of 9)
# and the old size, 0.86: short legs, a long broad trunk, thick limbs, a large head. The size here is 0.871: with it the
# dwarf stands as tall beside the human man as in the old battles (0.734 of him: the old sprites' legs were a smaller share
# of the figure than a body's are, so the same leg ratio shortens a body more).
from bodies import as_build, proportioned
from bodies import human

RATIOS = dict(legs=0.708, torso=0.926, shoulders=1.174, hips=1.15, girth=1.13, arms=0.864, neck=1.15, head=1.05, hands=1.08, feet=1.08, size=0.871, masses=True)


def male_average():
    return dict(proportioned(human.male_average(), **RATIOS), outfit="dwarven")


def female_average():
    return dict(proportioned(human.female_average(), **RATIOS), outfit="dwarven")


BODIES = {"dwarf-male-average": male_average, "dwarf-female-average": female_average,
          "dwarf-male-slender": lambda: as_build(male_average(), human.SLENDER_MAN), "dwarf-male-strong": lambda: as_build(male_average(), human.STRONG_MAN),
          "dwarf-female-slender": lambda: as_build(female_average(), human.SLENDER_WOMAN), "dwarf-female-strong": lambda: as_build(female_average(), human.STRONG_WOMAN)}
