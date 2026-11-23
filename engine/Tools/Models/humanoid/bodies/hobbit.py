# The hobbits' bodies (GM2 step 9c): the human's proportions with the old hobbits' ratios (the old build: legs 15 of 24, torso
# 21 of 27, head 19.5 of 20, shoulders 10 of 11.5, hips 9.5 of 10, arm 17 of 22, arm width 7.2 of 8, leg width 8 of 9) and
# the old size, 0.74: small, short-limbed, with big bare feet (the old `feet: "big-bare"`). The size here is 0.773: with it
# the hobbit stands as tall beside the human man as in the old battles (0.556 of him; see dwarf.py).
from bodies import as_build, proportioned
from bodies import human

RATIOS = dict(legs=0.625, torso=0.778, shoulders=0.87, hips=0.95, girth=0.90, arms=0.773, neck=0.95, head=0.975, feet=(1.16, 1.34), size=0.773, masses=True)


def male_average():
    return dict(proportioned(human.male_average(), **RATIOS), outfit="hobbit")


def female_average():
    return dict(proportioned(human.female_average(), **RATIOS), outfit="hobbit")


BODIES = {"hobbit-male-average": male_average, "hobbit-female-average": female_average,
          "hobbit-male-slender": lambda: as_build(male_average(), human.SLENDER_MAN), "hobbit-male-strong": lambda: as_build(male_average(), human.STRONG_MAN),
          "hobbit-female-slender": lambda: as_build(female_average(), human.SLENDER_WOMAN), "hobbit-female-strong": lambda: as_build(female_average(), human.STRONG_WOMAN)}
