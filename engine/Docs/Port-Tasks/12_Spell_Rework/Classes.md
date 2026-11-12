# The two new classes (task 12)

**Status: a draft for the user's review (SP2).** The task's two classes, the Spellweaver and the Ritual Caster, for the
port. Once approved they are added to task 10's drafts (`../10_Class_Rework/`), as task 11's racial classes were, and
built with them. They follow task 10's rules: one passive and one skill (C21), the passive or the skill at the last level
(the capstone rule), requirements within the game's kinds (C28), and its rarities (no colour, blue, purple, gold, red).
The mechanics they use were decided first: Spell Augmentation and its weavings (SP9–SP16, SP27, SP63) and the ritual's
delay (SP64–SP66).

## Spellweaver

7 levels · **rarity purple**

- **Requires:** Level 50 · Mage 5 · at least 30 augmentation ranks bought across your spells (a new count the game
  keeps, like task 10's caravan trips and runes fitted)
- **Identity:** The augmenter: reshapes the spells it knows, strengthening them anywhere and weaving their element,
  scaling and tier into new forms.

| Level | Feature |
|---|---|
| 1 | Augment and reset your own spells anywhere, from the spellbook (SP9); augmentation costs you 10% less |
| 2 | **Element weaving** unlocked: change a spell's element (SP27: 100% of its price) |
| 3 | Your augmented spells gain 1% power for each rank they hold (up to +10%) |
| 4 | Augmentation costs you 20% less (instead of 10%) |
| 5 | **Scaling weaving** unlocked: change a spell's primary or secondary stat (50% of its price each) |
| 6 | **Weave in Battle** (skill, once per battle, 1 AP): your next spell this turn takes any element you choose, for that cast |
| 7 | **Master Weaver** (passive, capstone): **tier weaving** unlocked (raise a spell one tier, 200% of its price), and your woven spells gain 10% power; augmentation costs you 30% less (instead of 20%) |

- **Discounts together** (SP9 left it to the class): the Spellweaver's discount adds to the Institute of Magic's (25%)
  and the Church of Talos's (10%, 40% on Lightning and Wind), up to 50% off in all. **Proposal.**
- **Why purple:** tier weaving lets one spell climb a tier and element weaving lets any spell carry the element a
  party needs; both reach far beyond a blue class's power, and the class asks for Level 50 and a real investment in
  augmentation first.

## Ritual Caster

5 levels · **rarity blue**

- **Requires:** Level 35 · Mage 5 · Know at least 15 different spells
- **Identity:** The patient caster: begins its spells turns ahead, for a fraction of their Mana, and lets them go off
  when the time comes.

| Level | Feature |
|---|---|
| 1 | **Rituals:** any spell you cast can be delayed by 1 turn for 30% less Mana (SP64); you can have 1 ritual waiting |
| 2 | Delays of up to 2 turns (60% less Mana) |
| 3 | **Patient Circle** (passive): a ritual spell gains 10% power for every turn it waited |
| 4 | Delays of up to 3 turns (90% less Mana); you can have 2 rituals waiting |
| 5 | **Grand Rite** (skill, capstone; once per battle, 1 AP): every ritual you have waiting goes off now, keeping its full Mana discount and its Patient Circle power for the turns it waited |

How a ritual works (SP64–SP66): you choose the spell, its target and the delay; you pay the spell's AP now and its Mana
less the discount; the spell goes off at the start of your turn that many turns later, costing no AP then; if its
target has fallen it strikes another random enemy (a heal or a buff another random ally); a stun (or anything else
that stops your casting) breaks every ritual you have waiting, and their Mana is lost. A ritual can be augmented like
any spell; Augmentation's Mana cost applies before the ritual's discount. **Proposal.**

- **Why blue:** at 90% off, a Tier 9 spell costs 25 Mana, but it waits three turns and a stun wastes it; strong for a
  patient caster, within a blue class's reach.

## What the port builds

- The Spellweaver's augmenting anywhere, its discounts, the three weavings unlocking by level, Weave in Battle, Master
  Weaver; the count of augmentation ranks bought, for its requirement.
- The Ritual Caster's rituals (a delay picked when casting, up to its level's limit), the waiting rituals shown in
  battle, their breaking by a stun, Patient Circle, Grand Rite.
