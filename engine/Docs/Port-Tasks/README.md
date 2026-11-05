# Tasks for the port

The user's tasks to add to the game during the port (user decision 2026-11-05), done in their number order (a plain
file listing puts `10_` before `1_`; the list below is the order). The `.txt` files are the user's own text, unchanged
(only their line endings were made LF, as the repo stores text). Ten so far; four more are to come.

1. `1_Masterwork_Elementals.txt`: six masterworks remade from the elementals' former unique items (Plasma Blade,
   Tidepiercer, First Comet, Peace and Balance, Kugelblitz, Avalanche); the elementals' placeholder weapons stay.
2. `2_Guild.txt`: the Adventurers Guild's quest system rework (Hunt, Cull, Deliver, Search, Escort, Travel; Great Hunts
   kept) and its illustrated, clickable interior (castle town and smaller guilds), tavern recruitment, the hooded man,
   and a class information button.
3. `3_Shop_Rework.txt`: reforging at the Blacksmith, a Tailor in every town, the Institute of Magic on the lake, Hearth
   Home at the volcano (Ornn as its boss instead of a Great Hunt), the Doctor's Orders masterwork, five new traps.
4. `4_Artifact_Rework.txt`: a look for every artifact, new effects for the non-masterwork ones (at least 32 uncommon,
   24 rare, 16 epic, 8 legendary), level scaling for masterwork artifacts.
5. `5_New_Masterworks.txt`: Sparkling enemies with Treasure Seeker and Moneyshot, the Lament and Noble Hero
   masterworks, a new masterwork armor, the Shadow Crystal as a rare drop from every enemy.
6. `6_New_Areas.txt`: a demon/abyss area in the marsh with corrupted enemies (at least three demon/impid and three
   corrupted enemy types).
7. `7_New_Game_Plus.txt`: Limitless, unlocked by the first defeat of Anatemis and switchable on and off (level cap 200,
   enemies scaled to the player and 25% stronger, bosses with level-100 class perks, doubled masterwork drops), the
   Exotic rarity and its material Exium, five Exotic items, and an Exotic reward for beating Anatemis at level 200.
8. `8_Map_Rework.txt`: one continuous continent about 40% larger instead of separate regional maps, music that crosses
   over between regions, a boat-travel animation, fog and clouds as the mountain rises, caravan fast travel between
   visited castle towns, and a horse carriage to buy.
9. `9_Potion_Rework.txt`: an Apothecary in every town, 40 new potions and 12 poisons, at least 50 ingredients, foraging
   on the map, brewing and recipes, the Alchemist and Assimilator classes, and a Collect quest for the guild.
10. `10_Class_Rework.txt`: every class reviewed and reworked where needed (identity, requirements, passive and skill, a
    capstone, its colour rarity), then 26 named new classes and 15 more; for the new classes the user is asked about
    each one's identity, levels, requirements and rarity, in that order.

**How to use them:** read them when the port starts (`Engine-Plan.md`, "Open questions for later milestones", "At port
time" points here). Before each task's code, ask its design questions with the question tool and record the answers
in `Engine-Plan.md`, as for every step.

**Still open:** whether the tasks come after the game is ported faithfully and checked against the old game, or while
porting, each system they change ported straight into its new form (asked 2026-11-05; the user will answer later).

**Found so far:**
- Tasks 1 and 2 were checked against the engine work on 2026-11-03: neither touches it (the engine's tests use their
  own copy of the stylesheet and saved captures; the game's code and tables come over at port time).
- Task 1 (looked into 2026-11-03, then set aside):
  - Elementals are race `elemental`, but most have no `element` in their files; their element shows only in their
    names and spells. Which enemies count as "fire-based" and so on needs asking.
  - The Plasma Blade has to combine a wand's spells with a longsword's attacks.
  - Avalanche's light armor type needs choosing: scale armor, brigandine or leather.
  - Masterworks have no pre-set runes yet; whether those runes can be removed needs asking.
  - Related mechanics exist: `cleave-percent`, `mark-on-hit`, `damage-charge`, `stun-chance`, `shift-turn-meters`,
    and masterwork part lists that swap a pommel or crossguard (Quasar Jet).
