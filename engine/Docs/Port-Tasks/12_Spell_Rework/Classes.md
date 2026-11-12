# The two new classes (task 12)

**Status: a draft for the user's review (SP2), third version: stat bonuses with one passive and one skill (SP67), and
passives that apply in full when gained (SP68: the user, "A passives effect always apply immediatly, not after another
class level has been reached"; Spellweaving at Level 2, Rituals at Level 1 with one ritual).** The task's two classes, the Spellweaver and the Ritual Caster, for the port. Once approved they are
added to task 10's drafts (`../10_Class_Rework/`), as task 11's racial classes were, and built with them. They follow
task 10's rules and shape: levels give stat bonuses, and each class's mechanics sit in its one passive and its one skill
(C21), the passive or the skill at the last level (the capstone rule), requirements within the game's kinds (C28),
task 10's rarities. The mechanics they use were decided first: Spell Augmentation and its weavings (SP9–SP16, SP27,
SP63) and the ritual's delay (SP64–SP66).

## Spellweaver

7 levels · **rarity purple**

- **Requires:** Level 50 · Mage 5 · at least 30 augmentation ranks bought across your spells (a new count the game
  keeps, like task 10's caravan trips and runes fitted)
- **Identity:** The augmenter: reshapes the spells it knows, strengthening them anywhere and weaving their element,
  scaling and tier into new forms.

| Level | Feature |
|---|---|
| 1 | Intelligence +10% |
| 2 | **Spellweaving** (passive): you augment and reset your own spells anywhere, from the spellbook, for 20% less (SP9), and you can weave them: **element**, **scaling** and **tier weaving** (SP27's prices, outside the 10 ranks, SP63); your woven spells gain 10% power |
| 3 | Spell power +10% |
| 4 | Maximum Mana +15% |
| 5 | Wisdom +10% |
| 6 | Spells cost 10% less Mana |
| 7 | **Weave in Battle** (skill, capstone; once per battle, 1 AP): your next spell this turn takes any element you choose, for that cast |

- **Discounts together** (SP9 left it to the class): Spellweaving's 20% adds to the Institute of Magic's (25%) and
  the Church of Talos's (10%, 40% on Lightning and Wind), up to 50% off in all. **Proposal.**
- **Why purple:** tier weaving lets a spell climb a tier and element weaving lets any spell carry the element a party
  needs; both reach far beyond a blue class, and the class asks for Level 50 and a real investment in augmentation
  first. Its stat bonuses sit a little below the Great Mage's (blue, all raw power), as its strength is in the spells
  it reshapes.

## Ritual Caster

5 levels · **rarity blue**

- **Requires:** Level 35 · Mage 5 · Know at least 15 different spells
- **Identity:** The patient caster: begins its spells turns ahead, for a fraction of their Mana, and lets them go off
  when the time comes.

| Level | Feature |
|---|---|
| 1 | **Rituals** (passive): any spell you cast can be delayed by 1, 2 or 3 turns, for 30%, 60% or 90% less Mana (SP64), and gains 10% power for every turn it waited; one ritual can wait at a time |
| 2 | Wisdom +10% |
| 3 | Maximum Mana +15% |
| 4 | Mind +10% |
| 5 | **Grand Rite** (skill, capstone; once per battle, 1 AP): your waiting ritual goes off now, keeping its full Mana discount, with the power of a full 3-turn wait |

How a ritual works (SP64–SP66): you choose the spell, its target and the delay; you pay the spell's AP now and its Mana
less the discount; the spell goes off at the start of your turn that many turns later, costing no AP then; if its target
has fallen it strikes another random enemy (a heal or a buff another random ally); a stun (or anything else that stops
your casting) breaks the waiting ritual, and its Mana is lost. A ritual can be augmented like any spell;
Augmentation's Mana cost applies before the ritual's discount. **Proposal.**

- **Why blue:** at 90% off a Tier 9 spell costs 25 Mana, but it waits three turns and a stun wastes it; strong for a
  patient caster, within a blue class's reach.

## What the port builds

- The Spellweaver's Spellweaving (augmenting anywhere, its discount, the three weavings, woven spells' power), Weave in
  Battle; the count of augmentation ranks bought, for its requirement.
- The Ritual Caster's Rituals (a delay of 1–3 turns picked when casting, the waiting ritual shown in battle, its breaking
  by a stun, the waiting power), Grand Rite.
