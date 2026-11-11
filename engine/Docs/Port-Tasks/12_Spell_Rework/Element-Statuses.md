# Element statuses and reactions (task 12)

**Status: a draft for the user's review, with the sample (SP30).** Each element gets a signature status (new) that its
spells apply and build on, and the elements react with each other (SP30), so mixing elements in a party pays off. All
of it is new and for the port only (SP1, SP6). The spells in the element batches say which status they apply, to
whom and with what chance.

## The twelve statuses

| Element | Status | On | What it does | Lasts | Stacks |
|---|---|---|---|---|---|
| Basic | **Exposed** | enemies | takes 5% more spell damage per stack | 2 turns (a new stack refreshes) | up to 3 (+15%) |
| Fire | **Burning** | enemies | takes Fire damage at the start of each of its turns: the share of the hit the spell names | 2 turns | no: a stronger Burning replaces a weaker one |
| Water | **Soaked** | enemies | Speed −10%, Evasion −10% | 2 turns | no |
| Earth | **Cracked** | enemies | Armor −8% per stack | 3 turns | up to 3 (−24%) |
| Wind | **Off-Balance** | enemies | Evasion −15%, Accuracy −15% | 2 turns | no |
| Lightning | **Shocked** | enemies | loses 1 AP at the start of its next turn | until then | no |
| Ice | **Chilled** | enemies | Speed −10% per stack; the 3rd stack makes it **Frozen**: stunned for its next turn, then its Chill ends and it can't be Chilled for 1 turn | 2 turns (a new stack refreshes) | up to 3, then Frozen |
| Holy | **Blessed** | allies | restores HP at the start of each of its turns: the share of the heal the spell names | 2 turns | no: a stronger one replaces a weaker one |
| Dark | **Hexed** | enemies | deals 5% less damage per stack | 3 turns | up to 3 (−15%) |
| Nature | **Rooted** | enemies | Evasion halved, can't flee | 2 turns | no |
| Death | **Withered** | enemies | healing received −30% | 3 turns | no |
| Blood | **Bleeding** | enemies | loses 3% of its maximum HP at the start of each of its turns; the caster heals for half of it | 3 turns | no |

Rules for all of them:
- **They work both ways:** enemies' spells put them on heroes too (enemies' spells are revamped at the port, SP16).
- **Augmentation:** Effect strength raises a status's numbers, Effect duration its turns, Effect chance the chance to
  apply it (SP14). Effect duration doesn't lengthen Shocked or Frozen, which act once.
- **Immunity:** a creature that resists an element by 100 or more is immune to that element's status (a Fire
  Elemental never Burns).
- **Removing them:** anything that removes debuffs (Regrowth, potions, classes) removes them.
- How each looks and sounds in battle is made in the port.

## The reactions

When a spell hits a target that carries another element's status, they react. Each element sets off one reaction.

| Spell | On a target that is | Reaction | What happens |
|---|---|---|---|
| Basic | carrying any statuses | **Resonance** | +10% damage for each different status on the target (up to +30%) |
| Fire | Chilled or Frozen | **Melt** | +40% damage; the Chill and the Frozen end |
| Water | Burning | **Steam** | the Burning ends; the target's Accuracy −30% for 1 turn. On an ally, any Water spell ends their Burning |
| Earth | Off-Balance | **Topple** | +30% damage; the target loses 1 AP at its next turn; Off-Balance ends |
| Wind | Burning | **Wildfire** | the Burning spreads to every other enemy, with its strength and turns left |
| Lightning | Soaked | **Electrocute** | every other Soaked enemy is struck for 50% of the damage; every one struck is Shocked |
| Ice | Soaked | **Flash Freeze** | the spell's Chill adds 2 stacks instead of 1; Soaked ends |
| Holy | Hexed or Withered | **Purify** | +30% damage; the status ends. On an ally, any Holy spell ends their Hexed and Withered |
| Dark | Exposed | **Devour** | the Exposed stacks are used up, +15% damage for each |
| Nature | Cracked | **Overgrowth** | the target is Rooted for certain, whatever the spell's chance |
| Death | Bleeding | **Exsanguinate** | the Bleeding's remaining turns are dealt at once; Bleeding ends |
| Blood | Withered | **Blood Harvest** | the caster heals for 30% of the damage dealt |

A reaction needs the spell to hit; an all-enemies spell reacts on each target separately. Effects that read "spell
damage" (classes, gear) apply to a reaction's extra damage as to the rest.
