# Spell Augmentation: the system draft (task 12)

**Status: a draft for the user's review (SP15).** It describes the spells' new system and the power curve every
reworked and new spell follows. The sample of four reworked spells comes after it is approved, then the elements
(SP2). Everything here is for the port: the old game stays unchanged (SP1). The decisions it follows are SP1–SP22 in
`engine/Docs/Engine-Plan.md`, "Decisions for task 12, Spell Rework". "(new)" marks a mechanic the game doesn't have yet
(SP6). Points marked **Proposal** are my own and wait for your approval or notes.

## 1. What it is

Spell Augmentation lets a hero pay to boost the spells they know (SP9). Each spell has its own augmentation: you pick
which of its attributes to raise, rank by rank, so the same spell can grow differently for different heroes (SP10).
Augmentation belongs to each hero, the player and every companion (SP13).

## 2. Where you augment

| Place | Which spells | Price |
|---|---|---|
| Every Magic Tower (castle cities) | every spell | full price |
| The Institute of Magic (Mirrormere's lake) | every spell | 25% off (SP12) |
| The Church of Talos (the Storm Reliquary) | **Proposal:** Lightning spells only, as it sells only Lightning spells | 40% off (SP12) |
| Anywhere, from the spellbook | a Spellweaver's own spells | the Spellweaver's discount (set with the class) |

The Spellweaver's own discount, and whether it adds to the Institute's or the Church's, is settled with the class
(SP9, SP11).

## 3. The Augmentation menu (new): what it does

A new menu in every Magic Tower, beside its others; the Institute of Magic and the Storm Reliquary get it too. **How
it looks is asked later (SP23)**; this section says only what it lets you do.

1. **Choose a hero:** the player or a companion in the party.
2. **Choose a spell** that hero knows, seeing how many of its 10 ranks it holds.
3. **See the spell's attributes** that apply to it (SP14): the ranks each holds, what the next rank adds, and its
   price. Buying a rank takes the money at once and changes the spell for every battle after.
4. **Reset** the spell: all of its ranks undone, half of what they cost paid back (SP13).

Wherever a spell's numbers are shown (the spellbook, its description, the shops), they are the augmented ones: the
description's [X] is the augmented power. Unlearning a spell resets it the same way, with half paid back (SP13).

## 4. Ranks and limits

- Each attribute has up to **5 ranks**, and a spell holds at most **10 ranks** in all (SP10). Two attributes can be
  maxed, or the ranks spread.
- **Proposal: Effect duration stops at 3 ranks** (+3 turns), the one exception to 5: five extra turns would make
  almost every lasting effect last the whole fight.
- A rank costs more than the one before it and more for a higher tier (SP10), below.

## 5. The attributes (one common list, SP14)

Every spell uses this list; a spell shows only the attributes that apply to it. **Proposal:** the attributes and their
values per rank.

| Attribute | Applies to | Per rank | At the most |
|---|---|---|---|
| Power | anything the spell's power drives: damage, healing, barriers, retaliation | +6% power | +30% (5 ranks) |
| Resistance penetration (new) | damaging spells | the target's resistance to the spell counts 10 points lower, never below 0 (a weakness isn't deepened) | −50 (5 ranks) |
| Effect strength | the amounts of lasting effects: buffs, debuffs, damage over time, healing received, retaliation shares | +10% of the effect's amount (−20% Armor becomes −22%) | +50% (5 ranks) |
| Effect duration | lasting effects, not stuns or summons | +1 turn | +3 turns (3 ranks, above) |
| Effect chance | chance-based effects: stun chance, instant death, any other "chance to" | +20% of the chance (a 10% stun chance becomes 12%) | double (5 ranks) |
| Mana cost | every spell | −5% Mana | −25% (5 ranks) |
| Summon vitality | summons | the summoned ally's maximum HP +10% | +50% (5 ranks) |
| Summon might | summons | the summoned ally's damage +10% | +50% (5 ranks) |

Resistances, for scale: most resisting enemies have 30 or 50 against an element (139 of the 174 enemy definitions
resist something), so 3–5 ranks of penetration cancel a usual resistance.

Spells never critically hit, so critical chance isn't on the list (SP20). AP isn't either: a spell's AP is part of
its design (SP7).

## 6. Costs

**Proposal:** the k-th rank bought on a spell costs **k × 10% of the spell's Magic Tower price** (its listed price,
before learning discounts). The order of the attributes doesn't matter: the 1st rank costs 10%, the 10th 100%, all 10
cost 5.5× the spell's price. A higher tier costs more because its spells cost more.

| Spell | Price | 1st rank | 10th rank | All 10 ranks | At the Institute (−25%) |
|---|---|---|---|---|---|
| Tier 1 (Fire Bolt) | 600 | 60 | 600 | 3,300 | 2,475 |
| Tier 3 | 4,000 | 400 | 4,000 | 22,000 | 16,500 |
| Tier 5 | 10,000 | 1,000 | 10,000 | 55,000 | 41,250 |
| Tier 7 | 17,000–32,000 | 1,700–3,200 | 17,000–32,000 | 93,500–176,000 | 70,125–132,000 |
| Tier 9 | 33,000–37,000 | 3,300–3,700 | 33,000–37,000 | 181,500–203,500 | 136,125–152,625 |
| Tier 10 | 65,000 | 6,500 | 65,000 | 357,500 | 268,125 |

Prices in silver (100 silver a gold, 10,000 a platinum). A reset pays back half of what was paid, after discounts
(SP13). Spell prices themselves stay as they are unless an element's batch says otherwise.

## 7. Weaving: the Spellweaver's augmentations (new)

Changing a spell's element, its scaling or its tier only a Spellweaver can do (SP11). **Proposal** for how each works;
how many a Spellweaver may weave, from which class level, and whether weavings use any of the 10 ranks are settled
with the class (the last part of the task).

- **Element weaving:** the spell becomes another of the 12 elements. It counts as that element for everything:
  resistances, gear, classes, elemental mages, the storm of Talos. Its effects stay; damage they deal takes the new
  element. Shown as "Fire Bolt (woven: Ice)". Costs 100% of the spell's price.
- **Scaling weaving:** the primary or the secondary scaling stat becomes another stat; the multiplier stays (Int ×0.35
  becomes Luck ×0.35). One weaving per stat slot. Costs 50% of the spell's price each.
- **Tier weaving:** the spell rises one tier (to at most Tier 10): its power and Mana follow the next tier's curve
  (section 8), and anything that reads a spell's tier sees the new one. The hero must be able to learn that tier
  (Tier T from Level 10 × (T − 1)). Costs 200% of the spell's price.

A reset undoes weavings too, with half paid back.

## 8. The power curve every spell follows

Measured first (SP8; a hero of the old game without classes or gear, Levels 1–100, against its 174 enemy definitions):
a spell's damage per turn is about a fighter's two weapon attacks, Mana covers 2–3 casts of the best spell a caster
can learn and heroes regain none in battle, all-enemies spells were almost as strong as single-target ones, and low
tiers were 5.4× as Mana-efficient as Tier 9.

The rework keeps Mana as it is (SP17), sets all-enemies spells to about 75% of a single-target spell (SP18), and
narrows the gap between tiers to about 2.75× (SP19) by raising the high tiers (SP22). The **reference spell** of each
tier, a single-target damage spell at 3 AP scaling with Intelligence and Wisdom:

| Tier | Learnable from | Mana | Base power | Primary ×  | Secondary × | Raw caster at unlock: now → new | At Level 100: now → new | All enemies at 100 (75%) |
|---|---|---|---|---|---|---|---|---|
| 1 | Level 1 | 12 | 6 | 0.35 | 0.10 | 10 → 10 | 141 → 154 | 116 |
| 2 | 10 | 22 | 9 | 0.54 | 0.16 | 31 → 37 | 171 → 239 | 179 |
| 3 | 20 | 35 | 13 | 0.77 | 0.22 | 80 → 84 | 243 → 339 | 255 |
| 4 | 30 | 50 | 16 | 0.94 | 0.27 | 124 → 142 | 290 → 416 | 312 |
| 5 | 40 | 75 | 21 | 1.22 | 0.35 | 174 → 235 | 335 → 540 | 405 |
| 6 | 50 | 100 | 25 | 1.47 | 0.42 | 226 → 343 | 374 → 648 | 486 |
| 7 | 60 | 140 | 31 | 1.82 | 0.52 | 281 → 500 | 408 → 802 | 602 |
| 8 | 70 | 180 | 36 | 2.10 | 0.60 | 354 → 664 | 464 → 925 | 694 |
| 9 | 80 | 250 | 45 | 2.62 | 0.75 | 430 → 939 | 511 → 1,157 | 868 |
| 10 | 90 | 350 | 69 | 4.02 | 1.15 | 740 → 1,607 | 780 → 1,774 | 1,330 |

"Now" is today's average single-target damage spell of that tier; "raw caster" is a Human putting half of every
level's points into Intelligence and a quarter each into Mind and Wisdom, without classes or gear (Level 100:
Intelligence 357, Wisdom 233, 525 Mana).

How the individual spells use it (**Proposal**):
- **All enemies:** 75% of the reference power for the same Mana (SP18). Spells that hit a few targets (two, a row)
  sit between.
- **AP (SP7):** 3 AP is the norm. A 2 AP spell has about 65% of the power for about 70% of the Mana; a 4 AP spell about
  130% for 120%; a 5 AP spell about 160% for 140%; a 1 AP spell about 30% for 40%.
- **Effects cost power:** a spell with a strong lasting effect deals less than the reference; a plain one deals the
  reference. Support spells (heals, barriers, buffs, summons) are valued by what they do, tier by tier, in each
  element's batch.
- **Scaling:** the reference uses Intelligence and Wisdom; a spell may scale with any stat that fits it (healing with
  Wisdom, blood with Vitality, wind with Speed), with the same total weight.
- **Tiers 1–3** keep about today's power (SP22): the early game doesn't change.

## 9. Enemies

An enemy's spells may carry augmentations (and the enemy revamp may give them weavings if it gives them the
Spellweaver class), chosen per enemy in the port's enemy revamp, stronger and later enemies more (SP16).

## 10. What the port builds

- The Augmentation menu in every Magic Tower, the Institute of Magic and the Storm Reliquary; the Spellweaver's
  augmenting from the spellbook.
- Each hero's ranks and weavings per spell, in the character file and the save; enemies' in their definitions.
- The attributes in spell power, Mana cost, resistance, effect amounts, durations, chances and summons.
- Weaving: element, scaling and tier.
- The reworked and new spells (the batches that follow) on the curve above.
