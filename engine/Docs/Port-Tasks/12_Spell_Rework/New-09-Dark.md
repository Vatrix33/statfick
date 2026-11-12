# New spells 9: Dark (task 12)

**Status: approved (2026-11-11, SP57), for the port (SP2, SP3).** Dark's fourteen new spells: two in each of Tiers 1–5, one in each
of Tiers 6–9. They follow the approved system and curve (`Spell-Augmentation.md`), the sample's style and the element
statuses (`Element-Statuses.md`). Prices are the tier's usual. [X] values are for the raw caster of the system draft (no
classes or gear) at the level the tier unlocks and at Level 100.

## What Dark gains

- **Fear and mind:** Dread Gaze (an enemy cowers: no attack or spell next turn), Dark Slumber (sleep, broken by the
  first blow, which hits harder), Dominate (an enemy fights its own side for 2 turns; never bosses). Bosses resist fear
  and sleep half the time.
- **Shadows to hide in:** Cloak of Dusk, Shadow Step (no single-target attack can reach you until your next turn),
  Endless Night (a field: enemy Accuracy down, your side's Evasion up).
- **Curses and theft, earlier and wider:** Shadow Flick (2 AP), Creeping Darkness (a group Hex at Tier 2), Cursed Mark
  (+10% damage of every kind), Void Grasp (a barrier from the damage), Umbral Tide (4 AP), Shadow Spike (2 AP, steals
  Mana).
- **Pacts:** Soul Bargain trades HP for Mana; Shadow Clone gives a Dark fighter a short second body.

New mechanics: cowering (no attack or spell for a turn), sleep broken by a blow, domination, being untargetable by
single-target attacks, a shadow copy of the caster, trading HP for Mana, a darkness field.

## At a glance

| Tier | Spell | Kind | AP | Mana | What it does |
|---|---|---|---|---|---|
| 1 | Shadow Flick | offensive, one target | 2 | 8 | 2 AP, 30% Hex |
| 1 | Cloak of Dusk | defensive, yourself | 3 | 10 | Evasion +15% for 2 turns |
| 2 | Dread Gaze | offensive, one target | 3 | 22 | 50% chance it cowers: no attack or spell on its next turn |
| 2 | Creeping Darkness | offensive, all enemies | 3 | 22 | 25% chance to Hex each enemy |
| 3 | Shadow Step | defensive, yourself | 2 | 25 | 2 AP: no single-target attack can reach you until your next turn |
| 3 | Cursed Mark | offensive, one target | 3 | 35 | the target takes 10% more damage of every kind for 3 turns |
| 4 | Shadow Clone | summon-like, yourself | 3 | 50 | a shadow copy of you fights at half strength for 2 turns |
| 4 | Void Grasp | offensive, one target | 3 | 50 | a barrier of 50% of the damage |
| 5 | Dark Slumber | offensive, one target | 3 | 75 | it sleeps for its next turn; the next hit wakes it for +30% |
| 5 | Soul Bargain | defensive, yourself | 2 | 0 | 2 AP: 15% of your HP for 25% of your Mana |
| 6 | Umbral Tide | offensive, all enemies | 4 | 120 | 4 AP: Hexes every enemy twice |
| 7 | Endless Night | the whole field | 3 | 140 | 3 turns: enemy Accuracy −20%, your side's Evasion +10% |
| 8 | Shadow Spike | offensive, one target | 2 | 126 | 2 AP: always Hexes, steals 20% of the damage as Mana |
| 9 | Dominate | offensive, one target | 3 | 250 | it fights for your side for 2 turns (not bosses) |

## The spells

### Shadow Flick: Tier 1, offensive, one target (new spell)

- **The spell:** 2 AP, 8 Mana, 700 silver; power 4 + Int ×0.23 + Mind ×0.07 (65% of the tier's reference, the 2 AP rule). A 30% chance to **Hex** the target (1 stack) for 3 turns.
- **Description:** "Flick a shard of shadow at one enemy for [X] Dark damage, with a 30% chance to Hex it. This spell costs 2 AP."
- **[X]:** Level 1: 7; Level 100: 102.
- **Augments:** Power, Resistance penetration, Effect duration, Effect chance, Mana cost.
- **Why:** Dark's fast spell; it has none today.

### Cloak of Dusk: Tier 1, defensive, yourself (new spell)

- **The spell:** 3 AP, 10 Mana, 700 silver. Evasion +15% for 2 turns.
- **Description:** "Wrap yourself in a cloak of dusk: your Evasion rises by 15% for 2 turns."
- **Augments:** Effect strength, Effect duration, Mana cost.
- **Why:** A cheap early guard, before Night Mantle at Tier 3.

### Dread Gaze: Tier 2, offensive, one target (new spell)

- **The spell:** 3 AP, 22 Mana, 1,450 silver; power 6 + Int ×0.38 + Mind ×0.11 (70% of the tier's reference). A 50% chance that the target cowers (new): on its next turn it can't attack or cast spells (it may still move, defend or use items). A boss resists half the time.
- **Description:** "Fix one enemy with a dread gaze for [X] Dark damage; it has an even chance to cower and neither attack nor cast on its next turn. A boss resists half the time."
- **[X]:** Level 10: 25; Level 100: 167.
- **Augments:** Power, Resistance penetration, Effect chance, Mana cost.
- **Why:** Fear, Dark's oldest weapon, at a low tier.

### Creeping Darkness: Tier 2, offensive, all enemies (new spell)

- **The spell:** 3 AP, 22 Mana, 1,450 silver; power 6 + Int ×0.35 + Mind ×0.1 (65% of the tier's reference, 75% for all enemies, less its effect). Each enemy struck has a 25% chance to be **Hexed** (1 stack) for 3 turns.
- **Description:** "Let darkness creep over all enemies for [X] Dark damage; each has a 25% chance to be Hexed."
- **[X]:** Level 10: 24; Level 100: 154.
- **Augments:** Power, Resistance penetration, Effect duration, Effect chance, Mana cost.
- **Why:** Dark's first group spell is at Tier 4 today.

### Shadow Step: Tier 3, defensive, yourself (new spell)

- **The spell:** 2 AP, 25 Mana, 3,300 silver. You step into the shadows (new): until your next turn, single-target attacks and spells can't target you.
- **Description:** "Step into the shadows: until your next turn, no single-target attack or spell can reach you. This spell costs 2 AP."
- **Augments:** Mana cost.
- **Why:** A rogue-caster's escape, without Ice Block's lost turn, but no guard against group spells.

### Cursed Mark: Tier 3, offensive, one target (new spell)

- **The spell:** 3 AP, 35 Mana, 3,300 silver; power 10 + Int ×0.58 + Mind ×0.17 (75% of the tier's reference). The target takes 10% more damage of every kind for 3 turns (Basic's Exposed is spells only).
- **Description:** "Curse one enemy with a mark for [X] Dark damage: for 3 turns it takes 10% more damage of every kind."
- **[X]:** Level 20: 64; Level 100: 257.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Mana cost.
- **Why:** A curse that helps weapons and spells alike.

### Shadow Clone: Tier 4, summon-like, yourself (new spell)

- **The spell:** 3 AP, 50 Mana, 6,000 silver. A shadow of you appears for 2 turns (new): it acts on your turns after you, with half your stats and your equipped weapon, attacking enemies only; it can't cast spells, and it vanishes when hit for 20% of your maximum HP.
- **Description:** "Call your shadow to fight beside you for 2 turns: it attacks with your weapon at half your strength, and vanishes after a heavy blow."
- **Augments:** Effect duration, Mana cost.
- **Why:** A short second body for a Dark fighter; not a summon (Dark has four), so the summon attributes don't apply.

### Void Grasp: Tier 4, offensive, one target (new spell)

- **The spell:** 3 AP, 50 Mana, 6,000 silver; power 14 + Int ×0.8 + Mind ×0.23 (85% of the tier's reference). You gain a **barrier** of 50% of the damage dealt for 2 turns (the void takes, and keeps it for you).
- **Description:** "Grasp one enemy with the void for [X] Dark damage; half of it becomes a barrier around you for 2 turns."
- **[X]:** Level 30: 121; Level 100: 353.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Mana cost.
- **Why:** Dark's theft turned to defence.

### Dark Slumber: Tier 5, offensive, one target (new spell)

- **The spell:** 3 AP, 75 Mana, 8,800 silver; power 13 + Int ×0.73 + Mind ×0.21 (60% of the tier's reference). The target falls asleep (new): it can't act on its next turn; the first hit that lands on it wakes it and deals 30% more. A boss resists half the time.
- **Description:** "Draw one enemy into a dark slumber for [X] Dark damage: it sleeps through its next turn, and the first blow that wakes it deals 30% more. A boss resists half the time."
- **[X]:** Level 40: 141; Level 100: 323.
- **Augments:** Power, Resistance penetration, Effect chance, Mana cost.
- **Why:** Control that rewards the party for the opening blow.

### Soul Bargain: Tier 5, defensive, yourself (new spell)

- **The spell:** 2 AP, 0 Mana, 8,800 silver. Costs no Mana: you lose 15% of your maximum HP and regain 25% of your maximum Mana (new).
- **Description:** "Strike a bargain with the dark: you lose 15% of your maximum HP and regain 25% of your maximum Mana. This spell costs 2 AP."
- **Augments:** Effect strength.
- **Why:** A Dark caster's last reserve, paid in life, as Mana stays scarce.

### Umbral Tide: Tier 6, offensive, all enemies (new spell)

- **The spell:** 4 AP, 120 Mana, 11,700 silver; power 24 + Int ×1.4 + Mind ×0.4 (95% of the tier's reference, the 4 AP rule on an all-enemies spell). A heavy 4 AP tide; every enemy struck is always **Hexed** with 2 stacks for 3 turns.
- **Description:** "Raise an umbral tide over all enemies for [X] Dark damage, Hexing every one of them twice over. This spell costs 4 AP."
- **[X]:** Level 50: 327; Level 100: 617.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Mana cost.
- **Why:** Dark's biggest group hit below Tier 10.

### Endless Night: Tier 7, the whole field (new spell)

- **The spell:** 3 AP, 140 Mana, 17,500 silver. Night falls for 3 turns (new): every enemy's Accuracy −20% and every ally's Evasion +10%.
- **Description:** "Bring an endless night over the battle for 3 turns: enemies lose 20% Accuracy, and the party gains 10% Evasion."
- **Augments:** Effect strength, Effect duration, Mana cost.
- **Why:** A field that turns the whole fight against enemy weapons.

### Shadow Spike: Tier 8, offensive, one target (new spell)

- **The spell:** 2 AP, 126 Mana, 25,000 silver; power 23 + Int ×1.37 + Mind ×0.39 (65% of the tier's reference, the 2 AP rule). A 2 AP spell; it always **Hexes** the target (1 stack) for 3 turns and **steals Mana:** it loses 20% of the damage as Mana and you gain it.
- **Description:** "Drive a shadow spike into one enemy for [X] Dark damage, Hexing it and stealing Mana equal to 20% of the damage. This spell costs 2 AP."
- **[X]:** Level 70: 433; Level 100: 603.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Mana cost.
- **Why:** Dark's fast spell for the high tiers.

### Dominate: Tier 9, offensive, one target (new spell)

- **The spell:** 3 AP, 250 Mana, 35,000 silver; power 22 + Int ×1.31 + Mind ×0.38 (50% of the tier's reference). The target fights for your side for 2 turns (new): on its turns it attacks or casts at its own allies; the hold breaks early if your side damages it. Bosses and unique enemies can't be dominated.
- **Description:** "Dominate one enemy's mind for [X] Dark damage: for 2 turns it fights its own side, until your side harms it. Bosses can't be dominated."
- **[X]:** Level 80: 469; Level 100: 578.
- **Augments:** Power, Resistance penetration, Effect duration, Mana cost.
- **Why:** Dark's great control: an enemy's strength turned on its allies.
