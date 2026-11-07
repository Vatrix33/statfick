# Task 9's approved content: potions, poisons, ingredients and recipes

The content of the Potion Rework the user approved on 2026-11-07 (`../../Engine-Plan.md`, "Decisions for task 9, Potion
Rework", P31; the road to it P5 and P30). Kept as the reference for the port, which carries the old game's rules over and
remakes only the placeholder art (P2; `../README.md`, "Placeholder art the port remakes fully").

- `draft.md`: the approved list: the 40 potions and 12 poisons (effects, durations, prices, recipes), the ten Health and
  Mana Potions' recipes, the 56 ingredients (prices, where they are foraged, which enemies drop them, which recipes use
  them), how ingredients are found, the Apothecary, the Alchemist and the Assimilator, and Collect quests.
- `draft.py`: makes `draft.md` (`python3 draft.py`) and checks it: the counts by rarity (8/12/10/6/4 potions, 2/4/4/2
  poisons), at least 50 ingredients, every recipe three different ingredients and unlike any other, every ingredient in a
  recipe and with a source, no recipe needing an ingredient rarer than its product or two legendary ones, and every
  recipe's ingredients costing less than three quarters of its product.

It was built into the old game on 2026-11-07 (`old-game/Docs/Notes.md`, "Potion Rework"): the potions and poisons are in
`old-game/objects/consumables.json`, the ingredients, recipes, forage tables and enemy drops in
`old-game/scripts/alchemy-data.js`, both generated from this draft once. The rules are `old-game/scripts/alchemy-engine.js`;
the old game's checks are `old-game/test-client/tests/potion-rework-browser.js`. If the game's data and this draft ever
differ, the game's data is the newer.

Two things the build settled that the draft left open, both within the user's answers:
- Thrown poisons' damage is lowered by the target's Nature resistance at each tick (the task asks that poisons respect
  resistances); the Poisoner class's own poison keeps ignoring it.
- The draft's worked example for an eaten ingredient (Glass Sand) was corrected: Glass Sand is also in the Piercing
  Draught, so its two kept kinds are critical damage and armor pierce. The rule itself is unchanged.
