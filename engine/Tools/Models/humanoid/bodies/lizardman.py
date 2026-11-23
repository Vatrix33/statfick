# The lizardmen's bodies (GM2 step 9d): the human's bodies (the old looks set no build; size 1) with a thicker neck, larger
# hands and broad bare feet; the women's without a bust. Their head (a muzzle, slit eyes, no ears, no hair, no brows) is
# the faces' (parts/face.py); the tail and the crest are parts (parts/tail.py, parts/crest.py); claws on the hands and the
# feet and the loincloth are the outfit's (outfit.lizardman).
from bodies import as_build, proportioned
from bodies import human


def male_average():
    return dict(proportioned(human.male_average(), neck=1.18, head=1.10, hands=1.06, feet=(1.14, 1.12)), outfit="lizardman")


def female_average():
    p = dict(proportioned(human.female_average(), neck=1.14, head=1.10, hands=1.06, feet=(1.14, 1.12)), outfit="lizardman")
    p["build"] = dict(p.get("build", {}), bust=0.0)
    return p


BODIES = {"lizardman-male-average": male_average, "lizardman-female-average": female_average,
          "lizardman-male-slender": lambda: as_build(male_average(), human.SLENDER_MAN), "lizardman-male-strong": lambda: as_build(male_average(), human.STRONG_MAN),
          "lizardman-female-slender": lambda: as_build(female_average(), human.SLENDER_WOMAN), "lizardman-female-strong": lambda: as_build(female_average(), human.STRONG_WOMAN)}
