# The Goliaths' bodies (GM2 step 9e): the human's proportions with the old Goliaths' ratios (the old build: legs 25 of 24,
# torso 30 of 27, head 19 of 20, shoulders 14 of 11.5, hips 11.5 of 10, arm 24 of 22, arm width 10 of 8, leg width 10.5 of 9)
# and a size with which the Goliath stands as tall beside the human man as in the old battles (1.23 of him; the old size
# 1.18, here 1.162, see dwarf.py): a head taller, heavy-built, a long broad trunk, thick limbs, a head small for the body.
from bodies import as_build, proportioned
from bodies import human

RATIOS = dict(legs=1.042, torso=1.111, shoulders=1.217, hips=1.15, girth=1.20, arms=1.091, neck=1.22, head=0.95, hands=1.14, feet=1.10, size=1.162, masses=True)


def male_average():
    return dict(proportioned(human.male_average(), **RATIOS), outfit="goliath")


def female_average():
    return dict(proportioned(human.female_average(), **RATIOS), outfit="goliath")


BODIES = {"goliath-male-average": male_average, "goliath-female-average": female_average,
          "goliath-male-slender": lambda: as_build(male_average(), human.SLENDER_MAN), "goliath-male-strong": lambda: as_build(male_average(), human.STRONG_MAN),
          "goliath-female-slender": lambda: as_build(female_average(), human.SLENDER_WOMAN), "goliath-female-strong": lambda: as_build(female_average(), human.STRONG_WOMAN)}
