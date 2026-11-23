# The Half-Beasts' bodies (GM2 step 9e; T2, T14, Q22): the human's bodies (the old looks set no build; size 1) with claws
# on the hands (outfit.claws). Their beast ears and tails are parts with free lists (parts/ears.py, parts/tail.py), in the
# slots `fur` and `fur.tip`.
from bodies import as_build
from bodies import human


def male_average():
    return dict(human.male_average(), outfit="half-beast")


def female_average():
    return dict(human.female_average(), outfit="half-beast")


BODIES = {"half-beast-male-average": male_average, "half-beast-female-average": female_average,
          "half-beast-male-slender": lambda: as_build(male_average(), human.SLENDER_MAN), "half-beast-male-strong": lambda: as_build(male_average(), human.STRONG_MAN),
          "half-beast-female-slender": lambda: as_build(female_average(), human.SLENDER_WOMAN), "half-beast-female-strong": lambda: as_build(female_average(), human.STRONG_WOMAN)}
