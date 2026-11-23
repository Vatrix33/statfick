# The dark elves' bodies (GM2 step 9b): the elves' proportions (the old game gave both the same build) in their own outfit.
from bodies import as_build
from bodies import elf, human


def male_average():
    return dict(elf.male_average(), outfit="dark-elven")


def female_average():
    return dict(elf.female_average(), outfit="dark-elven")


BODIES = {"dark-elf-male-average": male_average, "dark-elf-female-average": female_average,
          "dark-elf-male-slender": lambda: as_build(male_average(), human.SLENDER_MAN), "dark-elf-male-strong": lambda: as_build(male_average(), human.STRONG_MAN),
          "dark-elf-female-slender": lambda: as_build(female_average(), human.SLENDER_WOMAN),
          "dark-elf-female-strong": lambda: as_build(female_average(), human.STRONG_WOMAN)}
