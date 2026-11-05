# Tasks for the port

The user's tasks to add to the game during the port (user decision 2026-11-05), done in their number order. The
`.txt` files are the user's own text, unchanged (only their line endings were made LF, as the repo stores text).

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
