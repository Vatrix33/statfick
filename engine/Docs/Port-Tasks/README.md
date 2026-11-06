# Tasks for the port

The user's tasks to add to the game during the port (user decision 2026-11-05), done in their number order (a plain
file listing puts `10_` before `1_`; the list below is the order). The `.txt` files are the user's own text, unchanged
(only their line endings were made LF, as the repo stores text), except three names in task 7 the user confirmed on
2026-11-05: "Laila" is Juila, "Zik" is Zik Lun, "Mythium" is Mythilium. Fourteen tasks.

1. `1_Masterwork_Elementals.txt`: six masterworks remade from the elementals' former unique items (Plasma Blade,
   Tidepiercer, First Comet, Peace and Balance, Kugelblitz, Avalanche); the elementals' placeholder weapons stay.
   **Already done in the old game (2026-11-05): don't build it again.** The port carries it over with the rest of the
   game; only its placeholder art is left to remake (below).
2. `2_Guild.txt`: the Adventurers Guild's quest system rework (Hunt, Cull, Deliver, Search, Escort, Travel; Great Hunts
   kept) and its illustrated, clickable interior (castle town and smaller guilds), tavern recruitment, the hooded man,
   and a class information button.
   **Already done in the old game (2026-11-06): don't build it again.** The port carries it over with the rest of the
   game; only its placeholder art, the two guild interiors, is left to remake (below).
3. `3_Shop_Rework.txt`: reforging at the Blacksmith, a Tailor in every town, the Institute of Magic on the lake, Hearth
   Home at the volcano (Ornn as its boss instead of a Great Hunt), the Doctor's Orders masterwork, five new traps.
   **Already done in the old game (2026-11-06): don't build it again.** The port carries it over with the rest of the
   game; only its placeholder art is left to remake (below).
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
11. `11_New_races.txt`: three new playable races with icons (Half-Beast, Merfolk made playable, Goliath), each with a
    racial class.
12. `12_Spell_Rework.txt`: every spell brought up to date (power, scaling, effects, mana cost; tiers kept), two new
    spells per element in tiers 1–5 and one in tiers 6–9 (at least four new summons), and the Spellweaver and Ritual
    Caster classes.
13. `13_New_classes.txt`: new classes by focus (four each for melee, ranged, spells and the rogue path, two for each of
    six combinations, nine without a focus), then more until players can acquire 250 classes; the user is asked about
    each one's identity, levels, requirements and rarity, in that order.
14. `14_Dungeons.txt`: dungeons (nine named, three more), each with stages and a boss; Camping Supplies between
    stages; defeat penalties and a warning before entry; the Delve guild quest; Saffron and Ool move from Great Hunts
    to dungeon bosses; three masterworks and an Exotic; the Dungeon Delver and Throwing Specialist classes; eleven
    throwing consumables for the General Store.

**How to use them:** read them when the port starts (`Engine-Plan.md`, "Open questions for later milestones", "At port
time" points here). Before each task's code, ask its design questions with the question tool and record the answers
in `Engine-Plan.md`, as for every step. Skip the tasks under "Done in the old game so far": they are part of the game the
port starts from, so their design questions are answered and their rules are already in its code and data. For those,
only the art under "Placeholder art the port remakes fully" is left.

**The order (checked 2026-11-05):** each task builds only on earlier ones (the Cull quest of task 2 before task 5, the
reforging of task 3 before task 9, Sparkling enemies of task 5 before task 7, the new locations of tasks 3
and 6 before the map rework of task 8, the guild quests of task 2 before Collect and Delve, caravans and poisons
before the Caravanist and Poisoner of task 10, the Exotic rarity of task 7 before task 14), with these points to
remember (the user's answers, 2026-11-05):
- Task 7's "dungeon enemies" is meant to be future-proof: Limitless scales every kind of enemy, also those added later.
  Dungeons only come with task 14 (which needs task 7's Exotic rarity), so task 14 gives its dungeon enemies the
  Limitless scaling.
- Task 13 adds classes until there are 250; task 14's two classes come on top (252 in the end), as intended.

**When:** task 1 is done in the old game before the port (user decision 2026-11-05), its new art as placeholders; task 2
is built in the old game too (user decision 2026-11-06, G1), also with placeholder art, and so is task 3 (the user,
2026-11-06, "same rules apply"; R1); the other tasks are decided one at a time (still open for them: before the port,
after the faithful port, or while porting).

**Done in the old game so far:**
- Task 1, Masterwork Elementals (2026-11-05): the six masterworks, their drops and effects, all in the old game's code
  and data, so the faithful port brings them over like any other masterwork. The design answers and where each rule
  lives are in `../../../old-game/Docs/Notes.md`, "Masterwork Elementals"; the old game's checks for it,
  `old-game/test-client/tests/masterwork-elementals-browser.js` (one entry per rule), are the reference the ported
  rules are tested against. Still to do in the port: only the art (below). The task's `.txt` stays as the user wrote it
  (no "done" mark in it); this README and `Engine-Plan.md` record that it's done.
- Task 2, Guild Rework (2026-11-06, a cloud session, merged through a pull request): the six quest types, the daily
  boards, the rewards, the clickable guild interior, the recruits, the map's Quests card and the class information
  button, all in the old game's code and data, so the faithful port brings them over like the rest of the guild. The
  user's 34 design answers are in `Engine-Plan.md`, "Decisions for task 2, Guild Rework" (G1–G34); where each rule lives
  is in `../../../old-game/Docs/Notes.md`, "Guild Rework"; the old game's checks for it,
  `old-game/test-client/tests/guild-rework-browser.js` (one entry per rule), are the reference the ported rules are
  tested against. Still to do in the port: only the art (below). The task's `.txt` stays as the user wrote it (no "done"
  mark in it); this README and `Engine-Plan.md` record that it's done. It touched only `old-game/` and the docs, never
  the engine (the user, 2026-11-06).
- Task 3, Shop Rework (2026-11-06, a cloud session, offered as a pull request): reforging at four anvils (Blacksmith,
  Tailor, Institute of Magic, Hearth Home), the Tailor in every settlement with a Blacksmith, the Institute of Magic on
  its island in Mirrormere's lake, Hearth Home with Ornn as its site boss (no longer a Great Hunt), Doctor's Orders and
  the five traps, all in the old game's code and data. The user's 29 design answers are in `Engine-Plan.md`, "Decisions
  for task 3, Shop Rework" (R1–R29); where each rule lives is in `../../../old-game/Docs/Notes.md`, "Shop Rework"; the old
  game's checks for it, `old-game/test-client/tests/shop-rework-browser.js`, are the reference the ported rules are
  tested against. Still to do in the port: only the art (below). The task's `.txt` stays as the user wrote it. It
  touched only `old-game/` and the docs, never the engine (the user, "same rules apply" as task 2).

**Placeholder art the port remakes fully** (the user wants it fully visually remade during the port; in the old game's
code task 1's and task 3's masterwork definitions carry `placeholderArt: true` and task 2's and task 3's scenes are listed
in `PLACEHOLDER_ART`):
- Task 1: the looks of Plasma Blade, Tidepiercer, First Comet, Peace and Balance, Kugelblitz and Avalanche, in their
  item art and battle sprites. Today each is its type's normal forged masterwork look with simple hints: a short red
  line from the Plasma Blade's gem (the design: a red laser beam from the gem, like a laser sword) and three prongs on
  Tidepiercer's head (the design: a lance shaped like a trident, with a crossguard instead of a pommel).
- Task 2: the two guild interiors (the castle-city guild hall, `guildHall`, and the town guild, `guildTown`, in
  `old-game/scripts/scene-banners.js`). Today they are simple ink rooms made with little effort on purpose (user decision
  G2, "Placeholder, so don't put much effort into it"), marked PLACEHOLDER ART in the code and listed in its
  `PLACEHOLDER_ART` (`SceneBanners.placeholderArt("guild-hall")` and `("guild-town")` return true). The port remakes them
  fully as the detailed illustrations the task asks for (`2_Guild.txt`, sections 10, 17 and 18: a large, elaborate
  castle-town hall and a modest, compact town guild that visibly differ), keeping what the user decided: the guild on
  the left (bulletin board beside the counter and its clerk), the tavern on the right (a hearth, a table of
  adventurers and, in castle cities only, the hooded man alone at a corner table), hunting trophies on the back wall
  (G21); every one of those objects clickable, with a candle-gold outline glow and its name on hover or keyboard focus
  (G23); the room in the upper part, above the panel docked along the bottom (G22). The rules behind the objects are not
  placeholder and stay as they are.
- Task 3 (user decision R1, placeholder like task 2): the Tailor's shop scene (`tailorShop` in `scene-banners.js`:
  bolts of cloth and a dress form in the shop frame), the Institute of Magic's inside (today the Magic Tower's sanctum,
  `institute`) and Hearth Home's (today the Blacksmith's arms hall, `hearth`), all three in `PLACEHOLDER_ART`; their map
  icons (`SITE_ICONS` in `map.js`: a domed tower, a forge with a flame) and Hearth Home's window (a plain panel like the
  Church's); the island and its bridge as painted on Mirrormere's map (`map-regions.js`); Doctor's Orders' look (the
  normal forged robe in white, `placeholderArt: true`); the reforge anvil drawing (`reforge.js`); and the five traps'
  looks (the General Store's generic goods art). The rules behind them are not placeholder.

**Found before the work:**
- Tasks 1 and 2 were checked against the engine work on 2026-11-03 (task 3 on 2026-11-06): none touches it (the
  engine's tests use their own copy of the stylesheet and saved captures; the game's code and tables come over at port
  time).
