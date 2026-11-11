# Batch 7: Ice (task 12)

**Status: a draft for the user's review (SP2).** Every Ice spell players can learn, reworked: 31 spells (Ice has no
boss-only spell). They follow the approved system and curve (`Spell-Augmentation.md`), the sample's style
(`Spell-Sample.md`) and the element statuses (`Element-Statuses.md`): each spell sets its status's chance and duration
(SP32). Tiers and prices stay. "(new)" marks a mechanic the game doesn't have yet. [X] values are for the raw caster of
the system draft (no classes or gear) at the level the tier unlocks and at Level 100; Ice's spells scale with
Intelligence and Mind.

## Ice's identity

The element of slowing to a stop:
- **Chilled** (its status: Speed −10% per stack; the third stack makes the target **Frozen**, stunned for its next
  turn, then free of Chill for a turn) and **Flash Freeze** (its reaction: on a Soaked enemy its Chill adds 2 stacks).
  Fire's **Melt** ends Chill and Frozen for extra damage.
- **Stacking:** spells with several hits (Frost Shard's two shards, Hail Curtain's three hailstorms) roll for Chill
  each time; Glacial Prison, Absolute Zero, Glacial Eclipse and Zero Horizon chill for certain; Permafrost Torrent keeps
  Chill from thawing; the Ice Elemental makes enemies freeze at two stacks.
- **Freezing and shattering:** Frost Prison and Glacial Silence freeze Chilled enemies; Shatterstorm and Glacial
  Lance shatter Frozen ones for 50% more; Everfrost Spike keeps a Frozen enemy frozen a turn longer; The Unending
  Winter freezes every enemy.
- **A stand against Fire:** Icebound Aegis, Frozen Sanctuary, Crystal Bulwark and Frozen Mercy cut Fire damage and put
  out Burning, now with percentages or a barrier instead of flat Armor.
- Today almost every Ice spell lowers Speed a little or has a stun chance; the Speed losses become Chill, and stuns
  stay on the Dragonbreath spells, Glacial Spear and Absolute Spear.

New mechanics this batch adds for the port: several hits rolling for a status, a freeze at once on a Chilled target,
shattering Frozen enemies, Chill that lasts longer on a whole field, a Frozen kept a turn longer, a summon that makes
enemies freeze at two stacks, guarding against the next two weapon hits.

## At a glance

| Tier | Spell | Kind | AP | Mana | What changes |
|---|---|---|---|---|---|
| 1 | Frost Shard | offensive, one target | 3 | 13 | two shards, 25% Chill each |
| 1 | Ice Bolt | offensive, one target | 3 | 12 | 20% chance to Chill |
| 1 | Minor Cold | offensive, all enemies | 3 | 6 | 30% chance to Chill each enemy, no damage |
| 1 | Ice Shield | defensive, yourself | 3 | 14 | next 2 weapon hits −30%, 50% Chill on melee attackers |
| 2 | Icy Burst | offensive, all enemies | 3 | 25 | 25% chance to Chill each enemy |
| 2 | Dragonbreath - Frost Form | offensive, one target | 3 | 27 | 15% stun kept (Dragonbreath), 25% Chill |
| 2 | Sudden Spike | offensive, one target | 2 | 15 | 2 AP kept, 30% Chill |
| 2 | Freezing Spray | offensive, all enemies | 3 | 26 | 50% chance to Chill each enemy |
| 3 | Dragonbreath - Rimefang | offensive, one target | 3 | 38 | 25% stun kept (Dragonbreath), 25% Chill |
| 3 | Glacial Prison | offensive, one target | 3 | 35 | always Chills twice (3 turns) |
| 3 | Icebound Aegis | defensive, yourself | 3 | 32 | Armor +30%, Fire −25%, 40% Chill on melee attackers |
| 3 | Hail Curtain | offensive, all enemies | 3 | 38 | three hailstorms on every enemy, 15% Chill each |
| 4 | Absolute Zero | offensive, all enemies | 3 | 54 | always Chills every enemy (3 turns) |
| 4 | Glacial Spear | offensive, one target | 3 | 51 | 20% stun kept, 50% chance to Chill twice |
| 4 | Frozen Sanctuary | defensive, yourself or an ally | 3 | 45 | a barrier and Fire −40% |
| 4 | Shatterstorm | offensive, all enemies | 3 | 52 | Frozen enemies shatter (+50%), Chilled +10% per stack |
| 5 | Dragonbreath - Winter Dominion | offensive, all enemies | 3 | 77 | Speed −20% kept (Dragonbreath), 25% Chill |
| 5 | Frost Prison | offensive, one target | 3 | 78 | freezes a Chilled target; Chills others twice |
| 5 | Crystal Bulwark | defensive, all allies | 3 | 70 | every ally's Armor +20% and Fire −30%, 3 turns |
| 5 | Summon - Ice Elemental | summon | 3 | 82 | while it stands, enemies freeze at 2 Chill stacks |
| 6 | Glacial Eclipse | offensive, all enemies | 3 | 96 | always Chills every enemy, 40% a second time |
| 6 | Absolute Spear | offensive, one target | 3 | 104 | 30% stun kept, 30% Chill |
| 6 | Frozen Mercy | defensive, yourself or an ally | 3 | 100 | heals, ends Burning, Fire −35% |
| 7 | Permafrost Torrent | offensive, all enemies | 3 | 134 | Chills every enemy; their Chill lasts 2 turns longer |
| 7 | Glacial Lance | offensive, one target | 3 | 146 | shatters a Frozen target (+50%), 40% Chill |
| 7 | Whiteout Veil | defensive, yourself or an ally | 3 | 140 | Evasion +25% and weapon damage −15% for 3 turns |
| 8 | Zero Horizon | offensive, all enemies | 3 | 170 | always Chills every enemy twice |
| 8 | Everfrost Spike | offensive, one target | 3 | 190 | always Chills; a Frozen target stays Frozen a turn longer |
| 9 | Glacial Silence | offensive, all enemies | 3 | 240 | every Chilled enemy freezes |
| 9 | Winter's Needle | offensive, one target | 3 | 260 | +15% per Chill stack, then Chills |
| 10 | The Unending Winter | offensive, all enemies | 3 | 350 | freezes every enemy; Speed −40% kept |

## The spells

### Frost Shard: Tier 1, offensive, one target

- **Now:** 3 AP, 13 Mana, power 6 + Int ×0.3 + Mind ×0.2. Cut one enemy with ice for [X] Ice damage and reduce its Speed by 5% for 2 turns.
- **Reworked:** 3 AP, 13 Mana; power 3 + Int ×0.16 + Mind ×0.05 (45% of the tier's reference, per shard; two shards, 90% in all). Two shards (new) each strike the target for [X], each with a 25% chance to **Chill** it (1 stack) for 2 turns (was one hit and Speed −5%).
- **Description:** "Cut one enemy with two shards of ice, each dealing [X] Ice damage with a 25% chance to Chill it for 2 turns."
- **[X], now → reworked:** Level 1: 11 → 5; Level 100: 159 → 72 (each shard).
- **Augments:** Power, Resistance penetration, Effect duration, Effect chance, Mana cost.
- **Why:** Two rolls for Chill on one target: Ice's stacking status likes many hits, and it differs now from Ice Bolt.

### Ice Bolt: Tier 1, offensive, one target

- **Now:** 3 AP, 12 Mana, power 0 + Int ×0.3 + Mind ×0.2. A bolt of ice that deals [X] Ice damage and decreases the target's Speed by 5% for 2 turns.
- **Reworked:** 3 AP, 12 Mana; power 6 + Int ×0.33 + Mind ×0.1 (95% of the tier's reference). A 20% chance to **Chill** the target (1 stack) for 2 turns (was Speed −5%).
- **Description:** "A bolt of ice that deals [X] Ice damage, with a 20% chance to Chill the target for 2 turns."
- **[X], now → reworked:** Level 1: 5 → 10; Level 100: 153 → 147.
- **Augments:** Power, Resistance penetration, Effect duration, Effect chance, Mana cost.
- **Why:** Ice's plain bolt (today it has no base power at all).

### Minor Cold: Tier 1, offensive, all enemies (no damage)

- **Now:** 3 AP, 5 Mana, power none. A small wind of frost that decreases all enemies' Speed by 5% for 2 turns.
- **Reworked:** 3 AP, 6 Mana. Each enemy has a 30% chance to be **Chilled** (1 stack) for 2 turns (was Speed −5% for 2 turns).
- **Description:** "Send a small wind of frost over all enemies; each has a 30% chance to be Chilled for 2 turns."
- **Augments:** Effect duration, Effect chance, Mana cost.
- **Why:** Kept as the cheapest way to touch every enemy, now with the status.

### Ice Shield: Tier 1, defensive, yourself

- **Now:** 3 AP, 14 Mana, power none. Increase your Armor by 50% against the next ranged attack against you.
- **Reworked:** 3 AP, 14 Mana. For 3 turns, the next 2 weapon attacks that hit you deal 30% less, and each melee attacker among them has a 50% chance to be **Chilled** (1 stack) for 2 turns (was +50% Armor against the next ranged attack).
- **Description:** "Raise a shield of ice for 3 turns: the next two weapon attacks that hit you deal 30% less, and melee attackers have a 50% chance to be Chilled."
- **Augments:** Effect strength, Effect duration, Effect chance, Mana cost.
- **Why:** One ranged attack was too narrow; it now guards against any two blows.

### Icy Burst: Tier 2, offensive, all enemies

- **Now:** 3 AP, 25 Mana, power 8 + Int ×0.3 + Mind ×0.1. An explosion of ice that deals [X] Ice damage to all enemies and decreases their Speed by 5% for 2 turns.
- **Reworked:** 3 AP, 25 Mana; power 6 + Int ×0.38 + Mind ×0.11 (70% of the tier's reference, 75% for all enemies, less its effect). Each enemy struck has a 25% chance to be **Chilled** (1 stack) for 2 turns (was Speed −5%).
- **Description:** "An explosion of ice that deals [X] Ice damage to all enemies, each with a 25% chance to be Chilled for 2 turns."
- **[X], now → reworked:** Level 10: 23 → 25; Level 100: 138 → 167.
- **Augments:** Power, Resistance penetration, Effect duration, Effect chance, Mana cost.
- **Why:** The cheap group spell, with the element's status.

### Dragonbreath - Frost Form: Tier 2, offensive, one target (Dragonbreath)

- **Now:** 3 AP, 27 Mana, power 12 + Int ×0.4 + Mind ×0.2. Exhale a gust of ice that deals [X] Ice damage and has a 15% chance to stun the target.
- **Reworked:** 3 AP, 27 Mana; power 7 + Int ×0.43 + Mind ×0.13 (80% of the tier's reference). Its weakening effect stays (a 15% chance to stun for 1 turn); a 25% chance to **Chill** the target (1 stack) for 2 turns.
- **Description:** "Exhale a gust of ice at one enemy for [X] Ice damage, with a 15% chance to stun it and a 25% chance to Chill it."
- **[X], now → reworked:** Level 10: 34 → 29; Level 100: 201 → 191.
- **Augments:** Power, Resistance penetration, Effect duration, Effect chance, Mana cost.
- **Why:** Dragonbreath spells keep their weakening effect.

### Sudden Spike: Tier 2, offensive, one target

- **Now:** 2 AP, 17 Mana, power 8 + Int ×0.4 + Mind ×0.1. A sudden icy spike that deals [X] Ice damage. This spell costs 2 Action Points.
- **Reworked:** 2 AP, 15 Mana; power 6 + Int ×0.35 + Mind ×0.1 (65% of the tier's reference, the 2 AP rule). Stays a 2 AP spell; a 30% chance to **Chill** the target (1 stack) for 2 turns.
- **Description:** "A sudden icy spike that deals [X] Ice damage, with a 30% chance to Chill the target. This spell costs 2 AP."
- **[X], now → reworked:** Level 10: 27 → 24; Level 100: 174 → 154.
- **Augments:** Power, Resistance penetration, Effect duration, Effect chance, Mana cost.
- **Why:** Ice's fast spell; its power and Mana follow the 2 AP rule.

### Freezing Spray: Tier 2, offensive, all enemies

- **Now:** 3 AP, 26 Mana, power 9 + Int ×0.4 + Mind ×0.2. Spray all enemies with ice for [X] Ice damage and reduce their Speed by 10% for 2 turns.
- **Reworked:** 3 AP, 26 Mana; power 5 + Int ×0.32 + Mind ×0.1 (60% of the tier's reference, 75% for all enemies, less its likely Chill). Each enemy struck has a 50% chance to be **Chilled** (1 stack) for 2 turns (was Speed −10%).
- **Description:** "Spray all enemies with freezing water for [X] Ice damage; each has a 50% chance to be Chilled for 2 turns."
- **[X], now → reworked:** Level 10: 31 → 21; Level 100: 198 → 143.
- **Augments:** Power, Resistance penetration, Effect duration, Effect chance, Mana cost.
- **Why:** Built around chilling, so it often does.

### Dragonbreath - Rimefang: Tier 3, offensive, one target (Dragonbreath)

- **Now:** 3 AP, 38 Mana, power 32 + Int ×0.5 + Mind ×0.2. Exhale a concentrated cone of killing frost that deals [X] Ice damage and has a 25% chance to stun the target.
- **Reworked:** 3 AP, 38 Mana; power 10 + Int ×0.62 + Mind ×0.18 (80% of the tier's reference). Its weakening effect stays (a 25% chance to stun for 1 turn); a 25% chance to **Chill** the target (1 stack) for 2 turns.
- **Description:** "Exhale a cone of killing frost at one enemy for [X] Ice damage, with a 25% chance to stun it and a 25% chance to Chill it."
- **[X], now → reworked:** Level 20: 81 → 67; Level 100: 257 → 273.
- **Augments:** Power, Resistance penetration, Effect duration, Effect chance, Mana cost.
- **Why:** Dragonbreath spells keep their weakening effect.

### Glacial Prison: Tier 3, offensive, one target

- **Now:** 3 AP, 35 Mana, power 34 + Int ×0.5 + Mind ×0.2. Encase an enemy in cracking ice, dealing [X] Ice damage and decreasing Speed by 20% for 3 turns.
- **Reworked:** 3 AP, 35 Mana; power 10 + Int ×0.58 + Mind ×0.17 (75% of the tier's reference). Always **Chills** the target with 2 stacks for 3 turns (was Speed −20% for 3 turns).
- **Description:** "Encase one enemy in cracking ice for [X] Ice damage, Chilling it twice over for 3 turns."
- **[X], now → reworked:** Level 20: 83 → 64; Level 100: 259 → 257.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Mana cost.
- **Why:** One step from Frozen: the next Chill freezes it.

### Icebound Aegis: Tier 3, defensive, yourself

- **Now:** 3 AP, 32 Mana, power 24 + Int ×0.5 + Mind ×0.2. Create a frozen shield that increases Armor by [X] and reduces Fire spell damage by 25% for 3 turns.
- **Reworked:** 3 AP, 32 Mana. For 3 turns: Armor +30%, 25% less Fire damage, and melee attackers have a 40% chance to be **Chilled** (1 stack) for 2 turns (was Armor +[X], which outgrew armor pieces).
- **Description:** "Create a frozen aegis for 3 turns: your Armor rises by 30%, Fire damage against you drops by 25%, and melee attackers have a 40% chance to be Chilled."
- **Augments:** Effect strength, Effect duration, Effect chance, Mana cost.
- **Why:** Ice's self-guard, as a percentage like Earth's.

### Hail Curtain: Tier 3, offensive, all enemies

- **Now:** 3 AP, 38 Mana, power 28 + Int ×0.5 + Mind ×0.2. Rake all enemies with hail for [X] Ice damage and reduce their Speed by 15% for 2 turns.
- **Reworked:** 3 AP, 38 Mana; power 3 + Int ×0.19 + Mind ×0.06 (25% of the tier's reference, per hailstorm; three, 75% in all). Three hailstorms (new) each strike every enemy for [X], each with a 15% chance to **Chill** it (1 stack) for 2 turns (was one strike and Speed −15%).
- **Description:** "Rake all enemies with three hailstorms, each dealing [X] Ice damage with a 15% chance to Chill each enemy for 2 turns."
- **[X], now → reworked:** Level 20: 77 → 21; Level 100: 253 → 85 (each hailstorm).
- **Augments:** Power, Resistance penetration, Effect duration, Effect chance, Mana cost.
- **Why:** Many small rolls stack Chill across a group.

### Absolute Zero: Tier 4, offensive, all enemies

- **Now:** 3 AP, 54 Mana, power 35 + Int ×0.6 + Mind ×0.2. Flash-freeze the enemy ranks for [X] Ice damage and decrease their Speed by 20% for 3 turns.
- **Reworked:** 3 AP, 54 Mana; power 10 + Int ×0.56 + Mind ×0.16 (60% of the tier's reference, 75% for all enemies, less its sure Chill). Always **Chills** every enemy struck (1 stack) for 3 turns (was Speed −20% for 3 turns).
- **Description:** "Flash-freeze the enemy ranks for [X] Ice damage, Chilling every one of them for 3 turns."
- **[X], now → reworked:** Level 30: 117 → 85; Level 100: 295 → 247.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Mana cost.
- **Why:** Ice's sure group Chill.

### Glacial Spear: Tier 4, offensive, one target

- **Now:** 3 AP, 51 Mana, power 45 + Int ×0.6 + Mind ×0.2. Impale an enemy with ancient ice for [X] Ice damage and gain a 20% chance to stun it.
- **Reworked:** 3 AP, 51 Mana; power 14 + Int ×0.8 + Mind ×0.23 (85% of the tier's reference). Its 20% chance to stun for 1 turn stays; a 50% chance to **Chill** the target with 2 stacks for 2 turns.
- **Description:** "Impale one enemy with ancient ice for [X] Ice damage, with a 20% chance to stun it and a 50% chance to Chill it twice over."
- **[X], now → reworked:** Level 30: 127 → 121; Level 100: 305 → 353.
- **Augments:** Power, Resistance penetration, Effect duration, Effect chance, Mana cost.
- **Why:** Kept as a mid-tier stun, with a strong chance at the status.

### Frozen Sanctuary: Tier 4, defensive, yourself or an ally

- **Now:** 3 AP, 45 Mana, power 40 + Int ×0.6 + Mind ×0.2. Shelter yourself or an ally in enchanted ice, increasing Armor by [X] and reducing Fire spell damage by 40% for 3 turns.
- **Reworked:** 3 AP, 45 Mana; power 13 + Int ×0.75 + Mind ×0.22 (80% of the tier's reference, as a barrier). A **barrier** of [X] for 3 turns, and 40% less Fire damage for 3 turns (was Armor +[X]).
- **Description:** "Shelter yourself or an ally in enchanted ice: a barrier that absorbs [X] damage and 40% less Fire damage for 3 turns."
- **[X], now → reworked:** Level 30: 122 → 114; Level 100: 300 → 332.
- **Augments:** Power, Effect strength, Effect duration, Mana cost.
- **Why:** Flat Armor became a barrier, as in the Basic batch; Ice keeps its stand against Fire.

### Shatterstorm: Tier 4, offensive, all enemies

- **Now:** 3 AP, 52 Mana, power 36 + Int ×0.6 + Mind ×0.2. Shatter ice over all enemies for [X] Ice damage and reduce their Evasion by 15% for 2 turns.
- **Reworked:** 3 AP, 52 Mana; power 10 + Int ×0.56 + Mind ×0.16 (60% of the tier's reference, 75% for all enemies, less the bonus). Frozen enemies **shatter**: they take 50% more and their Frozen ends; Chilled ones take 10% more per stack (new; was Evasion −15%).
- **Description:** "Shatter ice over all enemies for [X] Ice damage; Frozen ones shatter for 50% more, Chilled ones take 10% more for each stack of Chill."
- **[X], now → reworked:** Level 30: 118 → 85; Level 100: 296 → 247.
- **Augments:** Power, Resistance penetration, Mana cost.
- **Why:** Ice's own group payoff for a chilled field (Fire's Melt is the other).

### Dragonbreath - Winter Dominion: Tier 5, offensive, all enemies (Dragonbreath)

- **Now:** 3 AP, 77 Mana, power 42 + Int ×0.7 + Mind ×0.2. Exhale killing frost across all enemies for [X] Ice damage and reduce their Speed by 20% for 2 turns.
- **Reworked:** 3 AP, 77 Mana; power 14 + Int ×0.79 + Mind ×0.23 (65% of the tier's reference, 75% for all enemies, less its effects). Its weakening effect stays (Speed −20% for 2 turns); each enemy struck has a 25% chance to be **Chilled** (1 stack) for 2 turns.
- **Description:** "Exhale killing frost across all enemies for [X] Ice damage, reducing their Speed by 20% for 2 turns, with a 25% chance to Chill each."
- **[X], now → reworked:** Level 40: 164 → 153; Level 100: 338 → 350.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Effect chance, Mana cost.
- **Why:** Dragonbreath spells keep their weakening effect.

### Frost Prison: Tier 5, offensive, one target

- **Now:** 3 AP, 78 Mana, power 60 + Int ×0.7 + Mind ×0.2. Lock one enemy in crushing ice for [X] Ice damage and gain a 25% chance to stun it.
- **Reworked:** 3 AP, 78 Mana; power 17 + Int ×0.98 + Mind ×0.28 (80% of the tier's reference). A target that is already **Chilled** is Frozen at once; any other is Chilled with 2 stacks for 2 turns (new; was a 25% stun chance).
- **Description:** "Lock one enemy in crushing ice for [X] Ice damage: if it is Chilled, it freezes; if not, it is Chilled twice over."
- **[X], now → reworked:** Level 40: 182 → 188; Level 100: 356 → 432.
- **Augments:** Power, Resistance penetration, Effect duration, Mana cost.
- **Why:** Ice's sure freeze for a prepared target.

### Crystal Bulwark: Tier 5, defensive, all allies

- **Now:** 3 AP, 70 Mana, power 48 + Int ×0.7 + Mind ×0.2. Give all allies [X] Armor and reduce Fire spell damage they take by 35% for 2 turns.
- **Reworked:** 3 AP, 70 Mana. Every ally's Armor +20% and 30% less Fire damage for 3 turns (was Armor +[X] and Fire −35% for 2 turns).
- **Description:** "Raise a crystal bulwark around the party for 3 turns: every ally's Armor rises by 20% and Fire damage against them drops by 30%."
- **Augments:** Effect strength, Effect duration, Mana cost.
- **Why:** The party's guard against Fire, as a percentage.

### Summon - Ice Elemental: Tier 5, summon

- **Now:** 3 AP, 82 Mana, power none. Summon a friendly Ice Elemental. Only one may be active at a time.
- **Reworked:** 3 AP, 82 Mana; power 21 + Int ×1.22 + Mind ×0.35 (100% of the tier's reference, as a summon). Summons an Ice Elemental (Level 25, 273 HP today); its maximum HP rises by 200% of [X] and every attack it makes by 25% of [X] (SP29). Its hits have a 40% chance to **Chill** their target (1 stack, 2 turns); while it stands, enemies freeze at 2 stacks of Chill instead of 3 (new). One at a time.
- **Description:** "Summon a friendly Ice Elemental whose maximum HP rises by twice [X] and whose attacks deal a quarter of [X] more, each with a 40% chance to Chill; while it stands, enemies freeze at two stacks of Chill instead of three. Only one may be active at a time."
- **[X], now → reworked:** Level 40: - → 235; Level 100: - → 538.
- **Augments:** Mana cost, Summon Vitality, Summon Might, Summon Speed.
- **Why:** Ice's only summon makes the whole party's chills freeze sooner while it lives.

### Glacial Eclipse: Tier 6, offensive, all enemies

- **Now:** 3 AP, 96 Mana, power 52 + Int ×0.8 + Mind ×0.2. Blanket all enemies in black ice for [X] Ice damage and reduce their Speed by 25% for 2 turns.
- **Reworked:** 3 AP, 96 Mana; power 15 + Int ×0.88 + Mind ×0.25 (60% of the tier's reference, 75% for all enemies, less its Chills). Always **Chills** every enemy struck (1 stack) for 2 turns, with a 40% chance of a second stack (was Speed −25%).
- **Description:** "Blanket all enemies in black ice for [X] Ice damage, Chilling each for 2 turns, with a 40% chance to Chill it twice."
- **[X], now → reworked:** Level 50: 221 → 205; Level 100: 384 → 387.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Effect chance, Mana cost.
- **Why:** Absolute Zero's stronger sibling.

### Absolute Spear: Tier 6, offensive, one target

- **Now:** 3 AP, 104 Mana, power 70 + Int ×0.8 + Mind ×0.2. Skewer one enemy with absolute frost for [X] Ice damage and gain a 30% chance to stun it.
- **Reworked:** 3 AP, 104 Mana; power 21 + Int ×1.25 + Mind ×0.36 (85% of the tier's reference). Its 30% chance to stun for 1 turn stays; a 30% chance to **Chill** the target (1 stack) for 2 turns.
- **Description:** "Skewer one enemy with absolute frost for [X] Ice damage, with a 30% chance to stun it and a 30% chance to Chill it."
- **[X], now → reworked:** Level 50: 239 → 292; Level 100: 402 → 551.
- **Augments:** Power, Resistance penetration, Effect duration, Effect chance, Mana cost.
- **Why:** Kept as Ice's high-tier stun.

### Frozen Mercy: Tier 6, defensive, yourself or an ally

- **Now:** 3 AP, 100 Mana, power 58 + Int ×0.8 + Mind ×0.2. Restore [X] HP plus 15% of maximum HP to yourself or an ally and reduce Fire spell damage they take by 35% for 2 turns.
- **Reworked:** 3 AP, 100 Mana; power 21 + Int ×1.25 + Mind ×0.36 (85% of the tier's reference). Heals [X] plus 10% of maximum HP, ends their Burning and Fire damage against them −35% for 2 turns (was [X] plus 15%).
- **Description:** "Restore [X] HP plus 10% of maximum HP to yourself or an ally, put out any Burning and reduce the Fire damage they take by 35% for 2 turns."
- **[X], now → reworked:** Level 50: 227 → 292; Level 100: 390 → 551.
- **Augments:** Power, Effect strength, Effect duration, Mana cost.
- **Why:** Ice's one heal, kept, with Fire put out.

### Permafrost Torrent: Tier 7, offensive, all enemies

- **Now:** 3 AP, 134 Mana, power 64 + Int ×0.8 + Mind ×0.3. Bury all enemies under ice for [X] Ice damage and lower Speed by 25% for 2 turns.
- **Reworked:** 3 AP, 134 Mana; power 19 + Int ×1.09 + Mind ×0.31 (60% of the tier's reference, 75% for all enemies, less its effects). Always **Chills** every enemy struck (1 stack), and every enemy's Chill lasts 2 turns longer (new; was Speed −25%).
- **Description:** "Bury all enemies under a permafrost torrent for [X] Ice damage, Chilling each and making every Chill on them last 2 turns longer."
- **[X], now → reworked:** Level 60: 280 → 300; Level 100: 419 → 480.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Mana cost.
- **Why:** Keeps a chilled field from thawing until it freezes.

### Glacial Lance: Tier 7, offensive, one target

- **Now:** 3 AP, 146 Mana, power 82 + Int ×0.8 + Mind ×0.3. Pierce one enemy for [X] Ice damage with a 20% chance to stun for 1 turn.
- **Reworked:** 3 AP, 146 Mana; power 28 + Int ×1.64 + Mind ×0.47 (90% of the tier's reference). A Frozen target **shatters**: it takes 50% more and its Frozen ends; any other has a 40% chance to be **Chilled** (1 stack) for 2 turns (was a 20% stun chance).
- **Description:** "Pierce one enemy with a glacial lance for [X] Ice damage; a Frozen one shatters for 50% more, any other has a 40% chance to be Chilled."
- **[X], now → reworked:** Level 60: 298 → 451; Level 100: 437 → 723.
- **Augments:** Power, Resistance penetration, Effect duration, Effect chance, Mana cost.
- **Why:** Ice's single-target payoff for a freeze.

### Whiteout Veil: Tier 7, defensive, yourself or an ally

- **Now:** 3 AP, 140 Mana, power 64 + Int ×0.8 + Mind ×0.3. Grant yourself or an ally [X] Armor and 15% less incoming weapon damage for 2 turns.
- **Reworked:** 3 AP, 140 Mana. For 3 turns: Evasion +25% and 15% less weapon damage (was Armor +[X] and −15% weapon damage for 2 turns).
- **Description:** "Hide yourself or an ally in a whiteout for 3 turns: Evasion rises by 25% and weapon damage against them drops by 15%."
- **Augments:** Effect strength, Effect duration, Mana cost.
- **Why:** Flat Armor goes; a whiteout hides its target instead.

### Zero Horizon: Tier 8, offensive, all enemies

- **Now:** 3 AP, 170 Mana, power 70 + Int ×0.9 + Mind ×0.4. Freeze all enemies for [X] Ice damage and lower Speed by 30% for 2 turns.
- **Reworked:** 3 AP, 170 Mana; power 22 + Int ×1.26 + Mind ×0.36 (60% of the tier's reference, 75% for all enemies, less its sure Chills). Always **Chills** every enemy struck with 2 stacks for 2 turns (was Speed −30%).
- **Description:** "Freeze the horizon over all enemies for [X] Ice damage, Chilling each twice over for 2 turns."
- **[X], now → reworked:** Level 70: 362 → 399; Level 100: 484 → 556.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Mana cost.
- **Why:** Ice's great group Chill: one more Chill freezes them all.

### Everfrost Spike: Tier 8, offensive, one target

- **Now:** 3 AP, 190 Mana, power 90 + Int ×0.9 + Mind ×0.4. Pierce one enemy for [X] Ice damage with a 25% chance to stun for 1 turn.
- **Reworked:** 3 AP, 190 Mana; power 34 + Int ×1.99 + Mind ×0.57 (95% of the tier's reference). Always **Chills** the target (1 stack) for 3 turns; a Frozen target stays Frozen 1 turn longer instead (new; was a 25% stun chance).
- **Description:** "Pierce one enemy with an everfrost spike for [X] Ice damage, Chilling it for 3 turns; a Frozen one stays Frozen a turn longer."
- **[X], now → reworked:** Level 70: 382 → 630; Level 100: 504 → 877.
- **Augments:** Power, Resistance penetration, Effect duration, Mana cost.
- **Why:** Keeps the strongest enemy frozen once it is.

### Glacial Silence: Tier 9, offensive, all enemies

- **Now:** 3 AP, 240 Mana, power 85 + Int ×1 + Mind ×0.4. Freeze all enemies for [X] Ice damage and lower Speed by 20% for 2 turns.
- **Reworked:** 3 AP, 240 Mana; power 27 + Int ×1.57 + Mind ×0.45 (60% of the tier's reference, 75% for all enemies, less the freezes). Every enemy that is already **Chilled** is Frozen at once (new; was Speed −20%).
- **Description:** "Bring a glacial silence over all enemies for [X] Ice damage; every one that is Chilled freezes."
- **[X], now → reworked:** Level 80: 447 → 562; Level 100: 535 → 692.
- **Augments:** Power, Resistance penetration, Mana cost.
- **Why:** The great payoff for a chilled field: Zero Horizon, then Glacial Silence.

### Winter's Needle: Tier 9, offensive, one target

- **Now:** 3 AP, 260 Mana, power 95 + Int ×1 + Mind ×0.4. Skewer one enemy for [X] Ice damage with a 25% chance to stun for 1 turn.
- **Reworked:** 3 AP, 260 Mana; power 43 + Int ×2.49 + Mind ×0.71 (95% of the tier's reference). Deals 15% more for each **Chill** stack on the target (up to +45%), then Chills it (1 stack) for 2 turns (was a 25% stun chance).
- **Description:** "Skewer one enemy with winter's needle for [X] Ice damage, 15% more for each stack of Chill on it; it is then Chilled for 2 turns."
- **[X], now → reworked:** Level 80: 457 → 891; Level 100: 545 → 1,097.
- **Augments:** Power, Resistance penetration, Effect duration, Mana cost.
- **Why:** Ice's great single-target strike, stronger the colder its target.

### The Unending Winter: Tier 10, offensive, all enemies

- **Now:** 3 AP, 350 Mana, power 220 + Int ×1.1 + Mind ×0.5. Enclose all enemies in [X] Ice damage, lower their Speed by 40% for 3 turns and have a 20% chance to stun each for 1 turn.
- **Reworked:** 3 AP, 350 Mana; power 38 + Int ×2.21 + Mind ×0.63 (55% of the tier's reference, 75% for all enemies, less its effects). Every enemy struck is Frozen, even one that has just thawed, and its Speed −40% for 3 turns stays (its 20% stun chance becomes the sure freeze).
- **Description:** "Bring the unending winter on all enemies for [X] Ice damage: every one of them freezes, and their Speed drops by 40% for 3 turns."
- **[X], now → reworked:** Level 90: 679 → 882; Level 100: 729 → 974.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Mana cost.
- **Why:** Ice's Tier 10: the whole enemy side frozen at once.
