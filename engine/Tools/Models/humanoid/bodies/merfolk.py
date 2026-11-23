# The Merfolk's bodies (GM2 step 9d; T3: "on legs on land, with fin ears, scales and webbed hands, so armour and boots fit
# as everyone's"): the human's bodies (the old looks set no build; size 1) with webbed hands; the fin ears are the faces'
# (parts/face.py), the scales marks (parts/mark.py), the fin crest a part (parts/crest.py).
from bodies import as_build
from bodies import human


def male_average():
    return dict(human.male_average(), outfit="merfolk", webbed=True)


def female_average():
    return dict(human.female_average(), outfit="merfolk", webbed=True)


BODIES = {"merfolk-male-average": male_average, "merfolk-female-average": female_average,
          "merfolk-male-slender": lambda: as_build(male_average(), human.SLENDER_MAN), "merfolk-male-strong": lambda: as_build(male_average(), human.STRONG_MAN),
          "merfolk-female-slender": lambda: as_build(female_average(), human.SLENDER_WOMAN), "merfolk-female-strong": lambda: as_build(female_average(), human.STRONG_WOMAN)}
