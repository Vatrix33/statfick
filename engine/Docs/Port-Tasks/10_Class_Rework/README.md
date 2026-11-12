# Task 10's approved design: the Class Rework

The Class Rework the user approved between 2026-11-10 and 2026-11-11 (`../../Engine-Plan.md`, "Decisions for task 10,
Class Rework", C1–C32; the task is `../10_Class_Rework.txt`). Unlike tasks 1–9 it was **not built in the old game**
(C23): the old game's classes stay as they are, and the port builds the reworked set from these two files.

- `Class-Rework-Draft.md`: all 170 classes (the 130 in the old game, each reviewed, and 40 new) with each one's
  identity (a short playstyle), number of levels, requirements and rarity (no colour, blue, purple, gold, and red,
  above gold, for Unreality alone). Where a current class changes (levels, rarity, a sharpened identity, a rename), the
  entry shows the old value, the new one and why; the user's review notes stand word for word under their classes.
- `Class-Rework-Features.md`: every class's features level by level, with the rules they follow at the top. For the
  current classes the old level stands beside the remade one, with why; the new classes have one column and a "How it
  plays". It was approved in a sample of four (Warrior, Vampire, Hierophant, Reaver) and eleven batches; each batch's
  notes and the questions they raised are recorded in `Engine-Plan.md`.

**Added by task 11** (2026-11-11, T1, T17 in `Engine-Plan.md`, "Decisions for task 11"): the racial classes of the
three new races, Wildheart (Half-Beast), Deepcaller (Merfolk) and Peakwarden (Goliath), at the end of both files, so
the drafts hold 173 classes. The races themselves are in the old game.

**Added by task 12** (2026-11-12, SP63–SP69 in `Engine-Plan.md`, "Decisions for task 12"): the Spellweaver (purple)
and the Ritual Caster (blue), at the end of both files, so the drafts hold 175 classes. The spell mechanics they use
are in `../12_Spell_Rework/`.

**What the port must do with them** (the user, 2026-11-11, C32):
1. The classes are unchanged in the old game. During the port, the classes in these drafts are added.
2. During porting, every class is inspected for possible errors and inconsistencies.
3. After the port, every class feature is tested so that it works as intended.

What the port still decides or builds besides: the classes themselves (the class files' data from these tables; "(new)" marks
each mechanic the old game doesn't have), the red rarity's look (C16), and the full rework of every enemy that uses
classes (C2). The numbers are proposals to tune when built. Ids stay as they are where a class is renamed (Element
Siphon → Elemental Siphon keeps `element-siphon`).
