# Batch 8: Holy (task 12)

**Status: a draft for the user's review (SP2).** Every Holy spell players can learn, reworked: 32 spells (Holy has no
boss-only spell). They follow the approved system and curve (`Spell-Augmentation.md`), the sample's style
(`Spell-Sample.md`, where Greater Heal was approved) and the element statuses (`Element-Statuses.md`): each spell sets
its status's chance and duration (SP32). Tiers and prices stay. "(new)" marks a mechanic the game doesn't have yet.
[X] values are for the raw caster of the system draft (no classes or gear) at the level the tier unlocks and at Level
100, shown for a Wisdom-first caster as Holy's spells scale with Wisdom and Intelligence.

## Holy's identity

The element of healing over time, wards and judgment:
- **Blessed** (its status, on allies: healing at the start of each of their turns) and **Purify** (its reaction: +30%
  on Hexed or Withered enemies, ending them; on an ally, any Holy spell ends their Hexed and Withered). Holy's heals
  Bless (Lesser Heal sometimes, Greater Heal always), its party spells Bless everyone (Blessing of Dawn, Hallowed
  Ground, Sanctuary Hymn, Seraphic Shelter), and some of its attacks Bless their caster or the party.
- **Judgment of Devils, Demons and Undead** stays the element's old theme on a share of its attacks (Radiant Judgment,
  Luminous Verdict, Dawn's Edict, Celestial Judgment and others keep their bonus); spells that only repeated it now do
  something of their own.
- **Strikes that heal:** Solar Verdict heals the most wounded ally, Consecrated Wave heals the party from the damage it
  deals, Seraphic Lance Blesses everyone.
- **Protection:** wards against spells and against the old foes, immunity to debuffs (Mercy of the Sun), surviving a
  killing blow (Dawn Shelter), and raising every fallen ally at Tier 10 (Genesis Dawn).
- Today Holy repeats itself: five single-target attacks differ only in their bonus against the old foes, Sanctuary
  Hymn repeats Divine Restoration, and Solar Verdict repeats Radiant Judgment. Flat Armor becomes percentages.

New mechanics this batch adds for the port: a field that heals allies and burns some enemies each turn, a party
Blessing as healing over time, immunity to debuffs, surviving a killing blow at 1 HP, a heal shared out from damage
dealt, a target that can't be healed, blinding, raising every fallen ally, a summon whose heals Bless, a summon that
strengthens its side's heals.

## At a glance

| Tier | Spell | Kind | AP | Mana | What changes |
|---|---|---|---|---|---|
| 1 | Holy Bolt | offensive, one target | 3 | 10 | 20% chance to Bless yourself |
| 1 | Lesser Heal | defensive, yourself or an ally | 3 | 12 | heal, 30% chance to Bless |
| 1 | Lesser Restoration | defensive, yourself or an ally | 2 | 7 | 2 AP: removes Hexed or Withered first, else the newest debuff |
| 1 | Blessing of Dawn | defensive, all allies | 3 | 14 | every ally Blessed for 3 turns (3% of max HP each turn) |
| 2 | Minor Ward | defensive, all allies | 3 | 22 | every ally takes 25% less from Devils, Demons and Undead, 3 turns |
| 2 | Contempt | offensive, one target | 3 | 21 | the target deals 15% less to you for 2 turns |
| 2 | Holy Target | offensive, one target | 3 | 22 | the next 2 weapon attacks on it deal +20% |
| 2 | Radiant Seal | defensive, all allies | 3 | 24 | every ally: −15% spell damage, can't be Hexed, 3 turns |
| 3 | Radiant Judgment | offensive, one target | 3 | 36 | +80% against Devils, Demons and Undead (kept) |
| 3 | Greater Heal | defensive, yourself or an ally | 3 | 35 | as approved in the sample |
| 3 | Consecrated Ward | defensive, all allies | 3 | 35 | every ally: Armor +15%, 25% less from Devils, Demons and Undead |
| 3 | Luminous Verdict | offensive, all enemies | 3 | 38 | +30% against Devils, Demons and Undead (kept) |
| 4 | Solar Verdict | offensive, one target | 3 | 50 | heals your most wounded ally for 30% of the damage; +50% on Devils, Demons and Undead |
| 4 | Divine Restoration | defensive, yourself or an ally | 3 | 52 | heals, removes Hexed, Withered and one more debuff, Blesses |
| 4 | Hallowed Ground | defensive and offensive, the whole field | 3 | 48 | 3 turns: allies Blessed (4% HP), Devils, Demons and Undead take [X] each turn |
| 4 | Summon - Radiant Sentinel | summon | 3 | 56 | its heals Bless their target |
| 5 | Dawn's Edict | offensive, all enemies | 3 | 77 | +50% against Devils, Demons and Undead (kept) |
| 5 | Mercy of the Sun | defensive, yourself or an ally | 3 | 76 | heals and makes them immune to debuffs for 2 turns |
| 5 | Radiant Spear | offensive, one target | 3 | 72 | blinds: Accuracy −25% for 2 turns |
| 5 | Dragonbreath - Dawnfire | offensive, all enemies | 3 | 80 | self-heal kept, 30% chance to Bless yourself |
| 5 | Dawn's Shelter | defensive, all allies | 3 | 76 | every ally: −20% spell damage, Armor +10%, 3 turns |
| 6 | Judgment of Noon | offensive, all enemies | 3 | 96 | Evasion −20% on every enemy; +55% on Devils, Demons and Undead kept |
| 6 | Seraphic Lance | offensive, one target | 3 | 104 | Blesses every ally (10% of the damage each turn) |
| 6 | Sanctuary Hymn | defensive, all allies | 3 | 100 | every ally Blessed for 3 turns ([X] each turn) |
| 7 | Consecrated Wave | offensive, all enemies | 3 | 134 | your side is healed for 25% of all the damage; +30% on Devils, Demons and Undead kept |
| 7 | Verdict of Light | offensive, one target | 3 | 146 | ignores barriers, ends the newest buff; +50% on Devils, Demons and Undead kept |
| 7 | Dawn Shelter | defensive, yourself or an ally | 3 | 140 | the next blow that would defeat them leaves them at 1 HP and heals [X] |
| 8 | Summon - Golden Dragon | summon | 3 | 180 | while it stands, your side's heals +15% |
| 8 | Celestial Judgment | offensive, one target | 3 | 190 | +80% on Devils, Demons and Undead kept, and 25% stun on them |
| 9 | Seraphic Shelter | defensive, all allies | 3 | 240 | heals every ally, Blesses for 3 turns, Armor +15% |
| 9 | Solar Condemnation | offensive, one target | 3 | 260 | the target can't be healed for 2 turns; +40% on Devils, Demons and Undead kept |
| 10 | Genesis Dawn | offensive, all enemies; defensive, all allies | 3 | 350 | doubled on Devils, Demons and Undead, heals every ally, raises the fallen |

## The spells

### Holy Bolt: Tier 1, offensive, one target

- **Now:** 3 AP, 8 Mana, power 4 + Wis ×0.3 + Int ×0.2. A bolt of holy Mana that deals [X] Holy damage.
- **Reworked:** 3 AP, 10 Mana; power 6 + Wis ×0.33 + Int ×0.1 (95% of the tier's reference). A 20% chance to **Bless** you for 2 turns, regaining 15% of the hit at the start of each of your turns (was 8 Mana, plain damage).
- **Description:** "A bolt of holy Mana that deals [X] Holy damage, with a 20% chance to Bless you: you regain 15% of that at the start of each of your next 2 turns."
- **[X], now → reworked:** Level 1: 9 → 10; Level 100: 157 → 147.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Effect chance, Mana cost.
- **Why:** Holy's damage spells sometimes Bless their caster, so a Holy attacker heals a little as they smite.

### Lesser Heal: Tier 1, defensive, yourself or an ally

- **Now:** 3 AP, 12 Mana, power 10 + Wis ×0.5 + - ×None. Heal yourself or an ally by [X] HP.
- **Reworked:** 3 AP, 12 Mana; power 6 + Wis ×0.35 + Int ×0.1 (100% of the tier's reference). Heals [X]; a 30% chance to **Bless** them for 2 turns, for 20% of [X] each turn.
- **Description:** "Heal yourself or an ally by [X] HP, with a 30% chance to Bless them: they regain 20% of that at the start of each of their next 2 turns."
- **[X], now → reworked:** Level 1: 15 → 10; Level 100: 188 → 154.
- **Augments:** Power, Effect strength, Effect duration, Effect chance, Mana cost.
- **Why:** Holy's first heal, now able to Bless (Greater Heal Blesses for certain).

### Lesser Restoration: Tier 1, defensive, yourself or an ally

- **Now:** 3 AP, 7 Mana, power none. Remove a random debuff from yourself or an ally.
- **Reworked:** 2 AP, 7 Mana. A 2 AP spell now; removes Hexed or Withered first, otherwise the newest debuff (was a random debuff, 3 AP).
- **Description:** "Restore yourself or an ally, removing one debuff: Hexed or Withered first, otherwise the newest. This spell costs 2 AP."
- **Augments:** Mana cost.
- **Why:** A random cleanse was unreliable; a quick, aimed one fits a 2 AP spell.

### Blessing of Dawn: Tier 1, defensive, all allies

- **Now:** 3 AP, 14 Mana, power none. Increase every ally's Wisdom by 10% for 2 turns.
- **Reworked:** 3 AP, 14 Mana. Every ally is **Blessed** for 3 turns, regaining 3% of their maximum HP at the start of each of their turns (was every ally's Wisdom +10% for 2 turns).
- **Description:** "Bless every ally with the dawn: for 3 turns, each regains 3% of maximum HP at the start of each of their turns."
- **Augments:** Effect strength, Effect duration, Mana cost.
- **Why:** Wisdom +10% for 2 turns did little; the party's first healing over time does more.

### Minor Ward: Tier 2, defensive, all allies

- **Now:** 3 AP, 24 Mana, power none. You and your allies take 20% less damage from Devils, Demons, and Undead for 3 turns.
- **Reworked:** 3 AP, 22 Mana. Every ally takes 25% less damage from Devils, Demons and Undead for 3 turns (was 20%).
- **Description:** "Ward the party: for 3 turns, you and your allies take 25% less damage from Devils, Demons and Undead."
- **Augments:** Effect strength, Effect duration, Mana cost.
- **Why:** Kept as Holy's cheap ward against its old foes.

### Contempt: Tier 2, offensive, one target

- **Now:** 3 AP, 21 Mana, power 10 + Wis ×0.4 + Int ×0.2. Veil a creature in holy light. It takes [X] Holy damage and you take 5% less damage from its spells for 2 turns.
- **Reworked:** 3 AP, 21 Mana; power 8 + Wis ×0.46 + Int ×0.14 (85% of the tier's reference). The target deals 15% less damage to you for 2 turns (was 5% less from its spells).
- **Description:** "Veil a creature in holy light for [X] Holy damage; for 2 turns it deals 15% less damage to you."
- **[X], now → reworked:** Level 10: 32 → 32; Level 100: 199 → 205.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Mana cost.
- **Why:** A 5% cut on one enemy's spells was too small to notice.

### Holy Target: Tier 2, offensive, one target

- **Now:** 3 AP, 25 Mana, power 10 + Wis ×0.4 + Int ×0.2. Deal [X] Holy damage. The next melee or ranged attack against the target deals 10% more damage.
- **Reworked:** 3 AP, 22 Mana; power 8 + Wis ×0.46 + Int ×0.14 (85% of the tier's reference). Marks the target: the next 2 weapon attacks against it deal 20% more (was the next one, 10%).
- **Description:** "Mark one enemy with holy light for [X] Holy damage; the next two weapon attacks against it deal 20% more."
- **[X], now → reworked:** Level 10: 32 → 32; Level 100: 199 → 205.
- **Augments:** Power, Resistance penetration, Effect strength, Mana cost.
- **Why:** Holy's support for the party's weapon users, made worth the turn.

### Radiant Seal: Tier 2, defensive, all allies

- **Now:** 3 AP, 24 Mana, power none. Shield all allies from magic, reducing incoming spell damage by 10% for 2 turns.
- **Reworked:** 3 AP, 24 Mana. Every ally takes 15% less spell damage and can't be Hexed for 3 turns (was −10% for 2 turns).
- **Description:** "Seal the party in radiance for 3 turns: 15% less spell damage, and no Hex can take hold."
- **Augments:** Effect strength, Effect duration, Mana cost.
- **Why:** A ward against casters, with Holy's stand against Dark.

### Radiant Judgment: Tier 3, offensive, one target

- **Now:** 3 AP, 36 Mana, power 34 + Wis ×0.5 + Int ×0.3. Call down judgment for [X] Holy damage. It deals 80% more damage to Devils, Demons, and Undead.
- **Reworked:** 3 AP, 36 Mana; power 12 + Wis ×0.69 + Int ×0.2 (90% of the tier's reference). Deals 80% more to Devils, Demons and Undead (kept).
- **Description:** "Call down judgment on one enemy for [X] Holy damage, 80% more against Devils, Demons and Undead."
- **[X], now → reworked:** Level 20: 88 → 76; Level 100: 282 → 305.
- **Augments:** Power, Resistance penetration, Mana cost.
- **Why:** Kept as Holy's answer to its old foes; Holy's other single-target spells now do something else.

### Greater Heal: Tier 3, defensive, yourself or an ally

- **Now:** 3 AP, 34 Mana, power 30 + Wis ×0.5 + Int ×0.3. Restore [X] HP plus 10% of maximum HP to yourself or an ally.
- **Reworked:** 3 AP, 35 Mana; power 12 + Wis ×0.7 + Int ×0.2 (91% of the tier's reference, as approved in the sample). As approved in the sample (SP31, SP32): heals [X] plus 5% of maximum HP and always **Blesses** them for 2 turns, for 20% of [X] each turn; ends Hexed and Withered on them (Purify).
- **Description:** "Restore [X] HP plus 5% of maximum HP to yourself or an ally and Bless them: they regain 20% of [X] at the start of each of their next 2 turns. Ends Hexed and Withered."
- **[X], now → reworked:** Level 20: 84 → 76; Level 100: 278 → 308.
- **Augments:** Power, Effect strength, Effect duration, Mana cost.
- **Why:** As approved in the sample.

### Consecrated Ward: Tier 3, defensive, all allies

- **Now:** 3 AP, 35 Mana, power 26 + Wis ×0.5 + Int ×0.3. Bless all allies for 3 turns, increasing Armor by [X] and reducing damage from Devils, Demons, and Undead by 25%.
- **Reworked:** 3 AP, 35 Mana. For 3 turns, every ally's Armor +15% and 25% less damage from Devils, Demons and Undead (was Armor +[X], which outgrew armor pieces).
- **Description:** "Bless all allies for 3 turns, increasing their Armor by 15% and reducing damage from Devils, Demons and Undead by 25%."
- **Augments:** Effect strength, Effect duration, Mana cost.
- **Why:** Flat Armor becomes a percentage, as in the earlier batches.

### Luminous Verdict: Tier 3, offensive, all enemies

- **Now:** 3 AP, 38 Mana, power 28 + Wis ×0.5 + Int ×0.3. Strike all enemies for [X] Holy damage, dealing 30% more to Devils, Demons and Undead.
- **Reworked:** 3 AP, 38 Mana; power 9 + Wis ×0.54 + Int ×0.15 (70% of the tier's reference, 75% for all enemies, less the bonus). Deals 30% more to Devils, Demons and Undead (kept).
- **Description:** "Strike all enemies with a luminous verdict for [X] Holy damage, 30% more against Devils, Demons and Undead."
- **[X], now → reworked:** Level 20: 82 → 59; Level 100: 276 → 237.
- **Augments:** Power, Resistance penetration, Mana cost.
- **Why:** Kept as Holy's first group spell.

### Solar Verdict: Tier 4, offensive, one target

- **Now:** 3 AP, 50 Mana, power 45 + Wis ×0.6 + Int ×0.3. Strike with concentrated sunlight for [X] Holy damage. It deals 100% more damage to Devils, Demons, and Undead.
- **Reworked:** 3 AP, 50 Mana; power 14 + Wis ×0.8 + Int ×0.23 (85% of the tier's reference). Your most wounded ally is healed for 30% of the damage dealt (new); 50% more to Devils, Demons and Undead (was 100%, the same spell as Radiant Judgment).
- **Description:** "Strike one enemy with concentrated sunlight for [X] Holy damage, 50% more against Devils, Demons and Undead; your most wounded ally is healed for 30% of the damage dealt."
- **[X], now → reworked:** Level 30: 134 → 121; Level 100: 329 → 353.
- **Augments:** Power, Resistance penetration, Mana cost.
- **Why:** Today it is Radiant Judgment one tier up; it becomes a strike that heals.

### Divine Restoration: Tier 4, defensive, yourself or an ally

- **Now:** 3 AP, 52 Mana, power 40 + Wis ×0.6 + Int ×0.3. Restore [X] HP plus 20% of maximum HP to yourself or an ally and remove one random debuff.
- **Reworked:** 3 AP, 52 Mana; power 14 + Wis ×0.8 + Int ×0.23 (85% of the tier's reference). Heals [X] plus 10% of maximum HP, removes Hexed and Withered and one more debuff, and **Blesses** them for 2 turns for 15% of [X] each turn (was [X] plus 20% and one random debuff).
- **Description:** "Restore [X] HP plus 10% of maximum HP to yourself or an ally, cleanse them of Hex, Withering and one more debuff, and Bless them for 2 turns."
- **[X], now → reworked:** Level 30: 129 → 121; Level 100: 324 → 353.
- **Augments:** Power, Effect strength, Effect duration, Mana cost.
- **Why:** Holy's cleansing heal, aimed at Dark's and Death's statuses first.

### Hallowed Ground: Tier 4, defensive and offensive, the whole field

- **Now:** 3 AP, 48 Mana, power 35 + Wis ×0.6 + Int ×0.3. Consecrate the battlefield for 3 turns, increasing every ally's Armor by [X] and reducing damage from Devils, Demons, and Undead by 35%.
- **Reworked:** 3 AP, 48 Mana; power 5 + Wis ×0.28 + Int ×0.08 (30% of the tier's reference, per turn, on each of the Devils, Demons and Undead). For 3 turns the field is hallowed (new): every ally is **Blessed**, regaining 4% of maximum HP at the start of each of their turns, and Devils, Demons and Undead take [X] Holy damage at the start of each of theirs (was every ally's Armor +[X] and 35% less damage from them).
- **Description:** "Consecrate the battlefield for 3 turns: every ally regains 4% of maximum HP at the start of each of their turns, and Devils, Demons and Undead take [X] Holy damage at the start of each of theirs."
- **[X], now → reworked:** Level 30: 124 → 42; Level 100: 319 → 124.
- **Augments:** Power, Effect strength, Effect duration, Mana cost.
- **Why:** Holy ground that heals friends and burns its old foes.

### Summon - Radiant Sentinel: Tier 4, summon

- **Now:** 3 AP, 56 Mana, power none. Summon a friendly Radiant Sentinel to fight and heal. Only one may be active at a time.
- **Reworked:** 3 AP, 56 Mana; power 16 + Wis ×0.94 + Int ×0.27 (100% of the tier's reference, as a summon). Summons a Radiant Sentinel (Level 18, 215 HP today), which fights and heals; its maximum HP rises by 200% of [X] and every attack and spell it makes by 25% of [X] (SP29). Its heals also **Bless** their target for 2 turns, for 20% of the heal each turn. One at a time.
- **Description:** "Summon a friendly Radiant Sentinel, which fights and heals; its maximum HP rises by twice [X], its attacks and heals by a quarter of [X], and its heals Bless their target. Only one may be active at a time."
- **[X], now → reworked:** Level 30: - → 142; Level 100: - → 414.
- **Augments:** Mana cost, Summon Vitality, Summon Might, Summon Speed.
- **Why:** Holy's healer summon, growing with spell power.

### Dawn's Edict: Tier 5, offensive, all enemies

- **Now:** 3 AP, 77 Mana, power 43 + Wis ×0.7 + Int ×0.3. Flood all enemies with radiance for [X] Holy damage, dealing 50% more damage to Devils, Demons and Undead.
- **Reworked:** 3 AP, 77 Mana; power 15 + Wis ×0.85 + Int ×0.24 (70% of the tier's reference, 75% for all enemies, less the bonus). Deals 50% more to Devils, Demons and Undead (kept).
- **Description:** "Flood all enemies with radiance for [X] Holy damage, 50% more against Devils, Demons and Undead."
- **[X], now → reworked:** Level 40: 175 → 163; Level 100: 362 → 374.
- **Augments:** Power, Resistance penetration, Mana cost.
- **Why:** Kept as Holy's mid-tier group spell.

### Mercy of the Sun: Tier 5, defensive, yourself or an ally

- **Now:** 3 AP, 76 Mana, power 54 + Wis ×0.7 + Int ×0.3. Restore [X] HP plus 25% of maximum HP to yourself or an ally and remove one random debuff.
- **Reworked:** 3 AP, 76 Mana; power 18 + Wis ×1.04 + Int ×0.3 (85% of the tier's reference). Heals [X] plus 15% of maximum HP, removes one debuff, and no debuff can take hold on them for 2 turns (new; was [X] plus 25% and one random debuff).
- **Description:** "Pour the sun's mercy on yourself or an ally: [X] HP plus 15% of maximum HP restored, one debuff removed, and no new debuff can take hold for 2 turns."
- **[X], now → reworked:** Level 40: 186 → 200; Level 100: 373 → 459.
- **Augments:** Power, Effect duration, Mana cost.
- **Why:** A heal that protects its target from the next curses.

### Radiant Spear: Tier 5, offensive, one target

- **Now:** 3 AP, 72 Mana, power 61 + Wis ×0.7 + Int ×0.3. Pierce one enemy with holy light for [X] damage, dealing 80% more to Devils, Demons and Undead.
- **Reworked:** 3 AP, 72 Mana; power 19 + Wis ×1.1 + Int ×0.32 (90% of the tier's reference). Blinds the target: Accuracy −25% for 2 turns (new; was 80% more to Devils, Demons and Undead, as two other spells do).
- **Description:** "Pierce one enemy with holy light for [X] Holy damage, blinding it: its Accuracy drops by 25% for 2 turns."
- **[X], now → reworked:** Level 40: 193 → 212; Level 100: 380 → 486.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Mana cost.
- **Why:** Holy's single-target spells each get their own effect; radiance blinds.

### Dragonbreath - Dawnfire: Tier 5, offensive, all enemies (Dragonbreath)

- **Now:** 3 AP, 80 Mana, power 40 + Wis ×0.7 + Int ×0.3. Bathe all enemies in dawnfire for [X] Holy damage, then restore 20 HP plus 5% of your maximum HP to yourself.
- **Reworked:** 3 AP, 80 Mana; power 15 + Wis ×0.85 + Int ×0.24 (70% of the tier's reference, 75% for all enemies, less the self-heal). Its self-heal stays (20 HP plus 5% of your maximum HP); a 30% chance to **Bless** you for 2 turns, for 10% of the damage dealt each turn. It has no weakening effect for Dragon's Breath to lengthen.
- **Description:** "Bathe all enemies in dawnfire for [X] Holy damage, then restore 20 HP plus 5% of your maximum HP, with a 30% chance to Bless yourself for 2 turns."
- **[X], now → reworked:** Level 40: 172 → 163; Level 100: 359 → 374.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Effect chance, Mana cost.
- **Why:** Holy's Dragonbreath keeps its own effect, a heal.

### Dawn's Shelter: Tier 5, defensive, all allies

- **Now:** 3 AP, 76 Mana, power 48 + Wis ×0.7 + Int ×0.3. Grant all allies [X] Armor and reduce incoming spell damage by 15% for 2 turns.
- **Reworked:** 3 AP, 76 Mana. For 3 turns, every ally takes 20% less spell damage and has 10% more Armor (was Armor +[X] and −15% for 2 turns).
- **Description:** "Shelter the party in the dawn for 3 turns: 20% less spell damage and 10% more Armor."
- **Augments:** Effect strength, Effect duration, Mana cost.
- **Why:** Kept as the party's ward against casters, as percentages.

### Judgment of Noon: Tier 6, offensive, all enemies

- **Now:** 3 AP, 96 Mana, power 52 + Wis ×0.8 + Int ×0.3. Scour all enemies with daylight for [X] Holy damage, dealing 55% more to Devils, Demons and Undead.
- **Reworked:** 3 AP, 96 Mana; power 16 + Wis ×0.96 + Int ×0.27 (65% of the tier's reference, 75% for all enemies, less its effects). At noon nothing hides: every enemy's Evasion −20% for 2 turns (new); 55% more to Devils, Demons and Undead (kept).
- **Description:** "Scour all enemies with noon's light for [X] Holy damage, 55% more against Devils, Demons and Undead; for 2 turns their Evasion drops by 20%."
- **[X], now → reworked:** Level 50: 233 → 223; Level 100: 407 → 422.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Mana cost.
- **Why:** A group strike that also helps the party's weapons.

### Seraphic Lance: Tier 6, offensive, one target

- **Now:** 3 AP, 104 Mana, power 70 + Wis ×0.8 + Int ×0.3. Pierce one enemy with holy light for [X] damage, dealing 85% more to Devils, Demons and Undead.
- **Reworked:** 3 AP, 104 Mana; power 21 + Wis ×1.25 + Int ×0.36 (85% of the tier's reference). Every ally is **Blessed** for 2 turns, regaining 10% of the damage dealt at the start of each of their turns (new; was 85% more to Devils, Demons and Undead).
- **Description:** "Pierce one enemy with a seraphic lance for [X] Holy damage; every ally is Blessed, regaining 10% of the damage dealt at the start of each of their next 2 turns."
- **[X], now → reworked:** Level 50: 251 → 292; Level 100: 425 → 551.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Mana cost.
- **Why:** A strike that heals the whole party a little.

### Sanctuary Hymn: Tier 6, defensive, all allies

- **Now:** 3 AP, 100 Mana, power 58 + Wis ×0.8 + Int ×0.3. Restore [X] HP plus 20% of maximum HP to yourself or an ally and remove one random debuff.
- **Reworked:** 3 AP, 100 Mana; power 6 + Wis ×0.37 + Int ×0.1 (25% of the tier's reference, per ally, each turn). Every ally is **Blessed** for 3 turns, regaining [X] at the start of each of their turns (was a heal and one debuff on one target, the same as Divine Restoration).
- **Description:** "Sing a sanctuary hymn: every ally regains [X] HP at the start of each of their next 3 turns."
- **[X], now → reworked:** Level 50: 239 → 85; Level 100: 413 → 161.
- **Augments:** Power, Effect duration, Mana cost.
- **Why:** Holy's party healing over time; today it repeats Divine Restoration two tiers higher.

### Consecrated Wave: Tier 7, offensive, all enemies

- **Now:** 3 AP, 134 Mana, power 64 + Wis ×0.8 + Int ×0.4. Scour all enemies for [X] Holy damage, increased by 30% against Devils, Demons and Undead.
- **Reworked:** 3 AP, 134 Mana; power 20 + Wis ×1.18 + Int ×0.34 (65% of the tier's reference, 75% for all enemies, less the healing). Your side is healed for 25% of all the damage dealt, shared out among the allies (new); 30% more to Devils, Demons and Undead (kept).
- **Description:** "Scour all enemies with a consecrated wave for [X] Holy damage, 30% more against Devils, Demons and Undead; a quarter of all the damage dealt heals your party, shared among them."
- **[X], now → reworked:** Level 60: 294 → 325; Level 100: 442 → 520.
- **Augments:** Power, Resistance penetration, Mana cost.
- **Why:** A group strike that heals more the more enemies it hits.

### Verdict of Light: Tier 7, offensive, one target

- **Now:** 3 AP, 146 Mana, power 82 + Wis ×0.8 + Int ×0.4. Condemn one enemy for [X] Holy damage, increased by 50% against Devils, Demons and Undead.
- **Reworked:** 3 AP, 146 Mana; power 28 + Wis ×1.64 + Int ×0.47 (90% of the tier's reference). Ignores barriers and ends the target's newest buff (new); 50% more to Devils, Demons and Undead (kept).
- **Description:** "Condemn one enemy with the verdict of light for [X] Holy damage, 50% more against Devils, Demons and Undead; no barrier stops it, and it ends the target's newest buff."
- **[X], now → reworked:** Level 60: 312 → 451; Level 100: 460 → 723.
- **Augments:** Power, Resistance penetration, Mana cost.
- **Why:** Light that strips away an enemy's protection.

### Dawn Shelter: Tier 7, defensive, yourself or an ally

- **Now:** 3 AP, 140 Mana, power 64 + Wis ×0.8 + Int ×0.4. Restore [X] HP plus 15% maximum HP to yourself or an ally, who then takes 20% less spell damage for 2 turns.
- **Reworked:** 3 AP, 140 Mana; power 25 + Wis ×1.46 + Int ×0.42 (80% of the tier's reference, as the heal when it saves them). For 2 turns, the first blow that would defeat them leaves them at 1 HP instead and heals them for [X] (new; was a heal and −20% spell damage). Its name stays one letter from Tier 5's Dawn's Shelter.
- **Description:** "Shelter yourself or an ally in the dawn for 2 turns: the first blow that would defeat them leaves them at 1 HP and heals them for [X]."
- **[X], now → reworked:** Level 60: 294 → 402; Level 100: 442 → 644.
- **Augments:** Power, Effect duration, Mana cost.
- **Why:** A guardian's promise, unlike any heal; the near-twin names are kept, as you kept Electrocute's (SP40).

### Summon - Golden Dragon: Tier 8, summon

- **Now:** 3 AP, 180 Mana, power none. Summon a friendly Golden Dragon. Only one may be active at a time.
- **Reworked:** 3 AP, 180 Mana; power 36 + Wis ×2.1 + Int ×0.6 (100% of the tier's reference, as a summon). Summons a Golden Dragon (Level 60, 1,716 HP today); its maximum HP rises by 200% of [X] and every attack and spell it makes by 25% of [X] (SP29). While it stands, every heal its side gives is 15% stronger (new). One at a time.
- **Description:** "Summon a friendly Golden Dragon whose maximum HP rises by twice [X] and whose attacks deal a quarter of [X] more; while it stands, your side's heals are 15% stronger. Only one may be active at a time."
- **[X], now → reworked:** Level 70: - → 664; Level 100: - → 926.
- **Augments:** Mana cost, Summon Vitality, Summon Might, Summon Speed.
- **Why:** Holy's great summon strengthens the party's healing while it lives.

### Celestial Judgment: Tier 8, offensive, one target

- **Now:** 3 AP, 190 Mana, power 90 + Wis ×0.9 + Int ×0.5. Strike one enemy for [X] Holy damage, increased by 80% against Devils, Demons and Undead.
- **Reworked:** 3 AP, 190 Mana; power 34 + Wis ×1.99 + Int ×0.57 (95% of the tier's reference). Deals 80% more to Devils, Demons and Undead (kept), and has a 25% chance to stun one of them for 1 turn (new).
- **Description:** "Strike one enemy with celestial judgment for [X] Holy damage; against Devils, Demons and Undead it deals 80% more and has a 25% chance to stun."
- **[X], now → reworked:** Level 70: 399 → 630; Level 100: 527 → 877.
- **Augments:** Power, Resistance penetration, Effect chance, Mana cost.
- **Why:** Holy's great judgment of its old foes.

### Seraphic Shelter: Tier 9, defensive, all allies

- **Now:** 3 AP, 240 Mana, power 85 + Wis ×1 + Int ×0.5. Restore [X] HP plus 10% maximum HP to all allies and increase their Armor by 15% for 2 turns.
- **Reworked:** 3 AP, 240 Mana; power 29 + Wis ×1.7 + Int ×0.49 (65% of the tier's reference, 75% for all allies, less its effects). Heals every ally for [X] plus 10% of maximum HP, **Blesses** them for 3 turns for 15% of [X] each turn, and raises their Armor by 15% for 2 turns (was the heal and the Armor).
- **Description:** "Raise a seraphic shelter over the party: every ally regains [X] HP plus 10% of maximum HP, is Blessed for 3 turns and has 15% more Armor for 2 turns."
- **[X], now → reworked:** Level 80: 466 → 609; Level 100: 558 → 750.
- **Augments:** Power, Effect strength, Effect duration, Mana cost.
- **Why:** Holy's great party heal, with the element's status.

### Solar Condemnation: Tier 9, offensive, one target

- **Now:** 3 AP, 260 Mana, power 95 + Wis ×1 + Int ×0.5. Strike one enemy for [X] Holy damage, increased by 40% against Devils, Demons and Undead.
- **Reworked:** 3 AP, 260 Mana; power 43 + Wis ×2.49 + Int ×0.71 (95% of the tier's reference). The target can't be healed for 2 turns (new); 40% more to Devils, Demons and Undead (kept).
- **Description:** "Condemn one enemy under the sun for [X] Holy damage, 40% more against Devils, Demons and Undead; for 2 turns nothing can heal it."
- **[X], now → reworked:** Level 80: 476 → 891; Level 100: 568 → 1,097.
- **Augments:** Power, Resistance penetration, Effect duration, Mana cost.
- **Why:** The answer to an enemy healer or a boss that mends itself.

### Genesis Dawn: Tier 10, offensive, all enemies; defensive, all allies

- **Now:** 3 AP, 350 Mana, power 210 + Wis ×1.1 + Int ×0.6. Bring the first dawn upon all enemies for [X] Holy damage, doubled against Devils, Demons and Undead, then restore 10% maximum HP to all allies.
- **Reworked:** 3 AP, 350 Mana; power 41 + Wis ×2.41 + Int ×0.69 (60% of the tier's reference, 75% for all enemies, less its effects). Doubled against Devils, Demons and Undead and 10% of maximum HP to every ally (kept), and every fallen ally rises with 20% of maximum HP (new).
- **Description:** "Bring the first dawn on all enemies for [X] Holy damage, doubled against Devils, Demons and Undead; every ally regains 10% of maximum HP, and every fallen ally rises with 20%."
- **[X], now → reworked:** Level 90: 690 → 962; Level 100: 742 → 1,062.
- **Augments:** Power, Resistance penetration, Mana cost.
- **Why:** Holy's Tier 10: the dawn that returns the fallen.
