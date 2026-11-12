# Element statuses and reactions (task 12)

**Status: approved (2026-11-11, SP32), with the user's note applied: each spell sets its own chance and duration.**
Each element gets a signature status (new) that its spells apply and build on, and the elements react with each other
(SP30), so mixing elements in a party pays off. All of it is new and for the port only (SP1, SP6).

**Each spell sets its status's chance and duration (SP32).** A spell whose goal is damage has a low chance to apply its
element's status (a simple Fire Bolt rarely sets Burning); a spell built around the status has a high one (a wall of
fire nearly always does). How long the status lasts is the spell's too. The table below gives what each status does;
the spells in the element batches say whom they put it on, with what chance and for how many turns.

## The twelve statuses

| Element | Status | On | What it does | Lasts | Stacks |
|---|---|---|---|---|---|
| Basic | **Exposed** | enemies | takes 5% more spell damage per stack | the spell's turns (a new stack refreshes them) | up to 3 (+15%) |
| Fire | **Burning** | enemies | takes Fire damage at the start of each of its turns: the share of the hit the spell names | the spell's turns | no: a stronger Burning replaces a weaker one |
| Water | **Soaked** | enemies | Speed −10%, Evasion −10% | the spell's turns | no |
| Earth | **Cracked** | enemies | Armor −8% per stack | the spell's turns (a new stack refreshes them) | up to 3 (−24%) |
| Wind | **Off-Balance** | enemies | Evasion −15%, Accuracy −15% | the spell's turns | no |
| Lightning | **Shocked** | enemies | loses 1 AP at the start of its next turn | until then | no |
| Ice | **Chilled** | enemies | Speed −10% per stack; the 3rd stack makes it **Frozen**: stunned for its next turn, then its Chill ends and it can't be Chilled for 1 turn | the spell's turns (a new stack refreshes them); Frozen always 1 turn | up to 3, then Frozen |
| Holy | **Blessed** | allies | restores HP at the start of each of its turns: the share of the heal the spell names | the spell's turns | no: a stronger one replaces a weaker one |
| Dark | **Hexed** | enemies | deals 5% less damage per stack | the spell's turns (a new stack refreshes them) | up to 3 (−15%) |
| Nature | **Rooted** | enemies | Evasion halved, can't flee | the spell's turns | no |
| Death | **Withered** | enemies | healing received −30% | the spell's turns | no |
| Blood | **Bleeding** | enemies | loses 3% of its maximum HP at the start of each of its turns; the caster heals for half of it | the spell's turns | no |

Rules for all of them:
- **They work both ways:** enemies' spells put them on heroes too (enemies' spells are revamped at the port, SP16).
- **Chance and duration are the spell's** (SP32): low for spells whose goal is damage, high for spells built around
  the status; Shocked (until its next turn) and Frozen (1 turn) are the only fixed durations.
- **Augmentation:** Effect strength raises a status's numbers, Effect duration its turns, Effect chance the spell's
  chance to apply it (SP14). Effect duration doesn't lengthen Shocked or Frozen, which act once.
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
| Nature | Cracked | **Overgrowth** | the target is Rooted for certain, whatever the spell's chance (for the spell's turns) |
| Death | Bleeding | **Exsanguinate** | the Bleeding's remaining turns are dealt at once; Bleeding ends |
| Blood | Withered | **Blood Harvest** | the caster heals for 30% of the damage dealt |

A reaction needs the spell to hit; an all-enemies spell reacts on each target separately. Effects that read "spell
damage" (classes, gear) apply to a reaction's extra damage as to the rest.

## Borrowed statuses (SP60, SP61, 2026-11-12)

A spell may apply another element's status where it fits (the user's idea, from Boiling Blood; the list below
approved as proposed). The rules:
- **The status keeps its own element:** Burning from a Blood spell still deals Fire damage, and a creature immune to Fire
  (resisting it by 100 or more) can't Burn.
- **Reactions follow the spell's element,** not the status's: a Blood spell on a Withered enemy still sets off Blood
  Harvest. The borrowed status sets up its own element's partners (a Blood spell's Burning can be spread by Wind,
  steamed by Water).
- **Augmentation** treats it like the spell's own status (Effect strength, duration, chance), and it costs a little of
  the spell's power, like any effect.
- **Sparingly:** each element's own status stays its main one; only these twenty-eight borrow (fourteen in SP60,
  fourteen more in SP61), about one spell in twenty.

| Spell | Element | Borrowed status | In |
|---|---|---|---|
| Boiling Blood | Blood | Burning (certain, replacing its own scald) | `New-12-Blood.md` |
| Sacred Flame | Holy | Burning (certain, replacing its holy fire) | `New-08-Holy.md` |
| Freezing Spray | Ice | Soaked (30%) | `Batch-07-Ice.md` |
| Mudslide | Earth | Soaked (30%) | `New-04-Earth.md` |
| Riptide | Water | Off-Balance (30%) | `New-03-Water.md` |
| Boulder | Earth | Off-Balance (40%, replacing its AP loss) | `New-04-Earth.md` |
| Thunderclap | Lightning | Off-Balance (25%) | `New-06-Lightning.md` |
| Grave Chill | Death | Chilled (30%) | `Batch-11-Death.md` |
| Shadow Rend | Dark | Bleeding (30%) | `Batch-09-Dark.md` |
| Razor Squall | Wind | Bleeding (15% per gust) | `Batch-05-Wind.md` |
| Crystal Shards | Earth | Bleeding (15% per shard) | `New-04-Earth.md` |
| Briar Volley | Nature | Bleeding (25%) | `Batch-10-Nature.md` |
| Hellfire Wave | Fire | Hexed (30%) | `Batch-02-Fire.md` |
| Spore Cloud | Nature | Withered (30%) | `New-10-Nature.md` |
| Prismatic Bolt (SP61) | Basic | the status of the element it strikes as (30%) | `New-01-Basic.md` |
| Ashfall (SP61) | Fire | Off-Balance (40%, replacing its Accuracy loss) | `Batch-02-Fire.md` |
| Meteor Spire (SP61) | Earth | Burning (30%) | `Batch-04-Earth.md` |
| Avalanche (SP61) | Earth | Chilled (30%) | `Batch-04-Earth.md` |
| Sandblast (SP61) | Earth | Off-Balance (25%) | `New-04-Earth.md` |
| Tsunami (SP61) | Water | Off-Balance (30%) | `New-03-Water.md` |
| Sky Scythe (SP61) | Wind | Bleeding (25%) | `Batch-05-Wind.md` |
| Raging Storm (SP61) | Lightning | Soaked (30%) | `New-06-Lightning.md` |
| Icicle Volley (SP61) | Ice | Bleeding (15% per icicle) | `New-07-Ice.md` |
| Dawnbringer (SP61) | Holy | Burning (40%) | `New-08-Holy.md` |
| Umbral Hook (SP61) | Dark | Off-Balance (30%) | `Batch-09-Dark.md` |
| Thorn Tyrant (SP61) | Nature | Bleeding (40%) | `Batch-10-Nature.md` |
| Pale Requiem (SP61) | Death | Chilled (30%) | `Batch-11-Death.md` |
| Crimson Rain (SP61) | Blood | Soaked (25%) | `New-12-Blood.md` |
