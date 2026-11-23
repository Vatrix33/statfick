# The elves' bodies (GM2 step 9b): the human's proportions with the old elves' ratios (the old build: legs 26 of 24, shoulders
# 10.5 of 11.5, hips 9.5 of 10; a head taller by its legs: size 1.04): long legs, narrow shoulders and hips, slim limbs, a
# slender neck; the whole figure the old size, 1.04 (with the longer legs it stands 1.08 of the human, as in the old
# battles). Their three builds as the human's (bodies.as_build with the human's shapes).
from bodies import as_build, proportioned
from bodies import human


def male_average():
    return dict(proportioned(human.male_average(), legs=1.083, shoulders=0.913, hips=0.95, girth=0.93, arms=1.03, neck=0.92, size=1.04), outfit="elven")


def female_average():
    return dict(proportioned(human.female_average(), legs=1.083, shoulders=0.925, hips=0.96, girth=0.94, arms=1.03, neck=0.93, size=1.04), outfit="elven")


BODIES = {"elf-male-average": male_average, "elf-female-average": female_average,
          "elf-male-slender": lambda: as_build(male_average(), human.SLENDER_MAN), "elf-male-strong": lambda: as_build(male_average(), human.STRONG_MAN),
          "elf-female-slender": lambda: as_build(female_average(), human.SLENDER_WOMAN), "elf-female-strong": lambda: as_build(female_average(), human.STRONG_WOMAN)}
