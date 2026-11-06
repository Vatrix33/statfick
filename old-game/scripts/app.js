const STORAGE_KEY = "emberfall-character-stats";
const LEVEL_CAP = 100;
const BASE_ARMOR = 5;
const BASE_EVASION = 10;
const SPELL_FILE = "objects/spells.json";
const SPELL_CACHE_KEY = "emberfall-spells-cache";
const SPELL_HANDLE_DB = "emberfall-spell-file";
// Weapons and armor have no catalogue files: every weapon and armor piece is forged (masterworks included).
const ITEM_FILES = { artifacts: "objects/artifacts.json" };
const ITEM_FILE_LABELS = { artifacts: "artifacts.json" };
const ITEM_CACHE_KEYS = { artifacts: "emberfall-artifacts-cache" };
// The retired armor.json and weapons.json may still sit in the browser cache; they must never come back from there.
try { localStorage.removeItem("emberfall-armor-cache"); localStorage.removeItem("emberfall-weapons-cache"); } catch { /* storage unavailable */ }
const ITEM_HANDLE_DB = "emberfall-item-json-files";
const RACE_FILE = "objects/races.json";
const RACE_CACHE_KEY = "emberfall-races-cache";
const CLASS_FILES = [
  { id: "warrior", path: "classes/Warrior.json", label: "Warrior.json" },
  { id: "archer", path: "classes/Archer.json", label: "Archer.json" },
  { id: "mage", path: "classes/Mage.json", label: "Mage.json" },
  { id: "rogue", path: "classes/Rogue.json", label: "Rogue.json" },
  { id: "monster", path: "classes/Monster.json", label: "Monster.json" },
  { id: "beast", path: "classes/Beast.json", label: "Beast.json" },
  { id: "great-beast", path: "classes/Great Beast.json", label: "Great Beast.json" },
  { id: "druid", path: "classes/Druid.json", label: "Druid.json" },
  { id: "warlock", path: "classes/Warlock.json", label: "Warlock.json" },
  { id: "priest", path: "classes/Priest.json", label: "Priest.json" },
  { id: "knight", path: "classes/Knight.json", label: "Knight.json" },
  { id: "spellblade", path: "classes/Spellblade.json", label: "Spellblade.json" },
  { id: "hunter", path: "classes/Hunter.json", label: "Hunter.json" },
  { id: "trap-layer", path: "classes/Trap Layer.json", label: "Trap Layer.json" },
  { id: "trap-master", path: "classes/Trap Master.json", label: "Trap Master.json" },
  { id: "traveler", path: "classes/Traveler.json", label: "Traveler.json" },
  { id: "raid-boss", path: "classes/Raid Boss.json", label: "Raid Boss.json" },
  { id: "sharmat", path: "classes/Sharmat.json", label: "Sharmat.json" },
  { id: "dragonkin", path: "classes/Dragonkin.json", label: "Dragonkin.json" },
  { id: "cleric", path: "classes/Cleric.json", label: "Cleric.json" },
  { id: "paladin", path: "classes/Paladin.json", label: "Paladin.json" },
  { id: "sharpshooter", path: "classes/Sharpshooter.json", label: "Sharpshooter.json" },
  { id: "thief", path: "classes/Thief.json", label: "Thief.json" },
  { id: "vampire", path: "classes/Vampire.json", label: "Vampire.json" },
  { id: "vampire-lord", path: "classes/Vampire Lord.json", label: "Vampire Lord.json" },
  { id: "monk", path: "classes/Monk.json", label: "Monk.json" },
  { id: "noble", path: "classes/Noble.json", label: "Noble.json" },
  { id: "assassin", path: "classes/Assassin.json", label: "Assassin.json" },
  { id: "barbarian", path: "classes/Barbarian.json", label: "Barbarian.json" },
  { id: "stonebreaker", path: "classes/Stonebreaker.json", label: "Stonebreaker.json" },
  { id: "summoner", path: "classes/Summoner.json", label: "Summoner.json" },
  { id: "wizard", path: "classes/Wizard.json", label: "Wizard.json" },
  { id: "spellcaster", path: "classes/Spellcaster.json", label: "Spellcaster.json" },
  { id: "sorcerer", path: "classes/Sorcerer.json", label: "Sorcerer.json" },
  { id: "heavy-knight", path: "classes/Heavy Knight.json", label: "Heavy Knight.json" },
  { id: "jack-of-all-trades", path: "classes/Jack of all Trades.json", label: "Jack of all Trades.json" },
  { id: "jester", path: "classes/Jester.json", label: "Jester.json" },
  { id: "joker", path: "classes/Joker.json", label: "Joker.json" },
  { id: "cultist", path: "classes/Cultist.json", label: "Cultist.json" },
  { id: "chaos-cultist", path: "classes/Chaos Cultist.json", label: "Chaos Cultist.json" },
  { id: "brawler", path: "classes/Brawler.json", label: "Brawler.json" },
  { id: "spell-sniper", path: "classes/Spell Sniper.json", label: "Spell Sniper.json" },
  { id: "bard", path: "classes/Bard.json", label: "Bard.json" },
  { id: "beastmaster", path: "classes/Beastmaster.json", label: "Beastmaster.json" },
  { id: "salesman", path: "classes/Salesman.json", label: "Salesman.json" },
  { id: "scholar", path: "classes/Scholar.json", label: "Scholar.json" },
  { id: "slayer", path: "classes/Slayer.json", label: "Slayer.json" },
  { id: "energist", path: "classes/Energist.json", label: "Energist.json" },
  { id: "gladiator", path: "classes/Gladiator.json", label: "Gladiator.json" },
  { id: "champion", path: "classes/Champion.json", label: "Champion.json" },
  { id: "weaponmaster", path: "classes/Weaponmaster.json", label: "Weaponmaster.json" },
  { id: "wise-man", path: "classes/Wise Man.json", label: "Wise Man.json" },
  { id: "guardian", path: "classes/Guardian.json", label: "Guardian.json" },
  { id: "pyromancer", path: "classes/Pyromancer.json", label: "Pyromancer.json" },
  { id: "aquamancer", path: "classes/Aquamancer.json", label: "Aquamancer.json" },
  { id: "geomancer", path: "classes/Geomancer.json", label: "Geomancer.json" },
  { id: "aeromancer", path: "classes/Aeromancer.json", label: "Aeromancer.json" },
  { id: "electromancer", path: "classes/Electromancer.json", label: "Electromancer.json" },
  { id: "cryomancer", path: "classes/Cryomancer.json", label: "Cryomancer.json" },
  { id: "luxomancer", path: "classes/Luxomancer.json", label: "Luxomancer.json" },
  { id: "umbramancer", path: "classes/Umbramancer.json", label: "Umbramancer.json" },
  { id: "botanomancer", path: "classes/Botanomancer.json", label: "Botanomancer.json" },
  { id: "hemomancer", path: "classes/Hemomancer.json", label: "Hemomancer.json" },
  { id: "necromancer", path: "classes/Necromancer.json", label: "Necromancer.json" },
  { id: "hierophant", path: "classes/Hierophant.json", label: "Hierophant.json" },
  { id: "avatar", path: "classes/Avatar.json", label: "Avatar.json" },
  { id: "berserker", path: "classes/Berserker.json", label: "Berserker.json" },
  { id: "madman", path: "classes/Madman.json", label: "Madman.json" },
  { id: "great-mage", path: "classes/Great Mage.json", label: "Great Mage.json" },
  { id: "reveler", path: "classes/Reveler.json", label: "Reveler.json" },
  { id: "templar", path: "classes/Templar.json", label: "Templar.json" },
  { id: "monster-hunter", path: "classes/Monster Hunter.json", label: "Monster Hunter.json" },
  { id: "blood-hunter", path: "classes/Blood Hunter.json", label: "Blood Hunter.json" },
  { id: "cardinal-priest", path: "classes/Cardinal Priest.json", label: "Cardinal Priest.json" },
  { id: "soldier", path: "classes/Soldier.json", label: "Soldier.json" },
  { id: "black-knight", path: "classes/Black Knight.json", label: "Black Knight.json" },
  { id: "death-paladin", path: "classes/Death Paladin.json", label: "Death Paladin.json" },
  { id: "ambusher", path: "classes/Ambusher.json", label: "Ambusher.json" },
  { id: "demon-hunter", path: "classes/Demon Hunter.json", label: "Demon Hunter.json" },
  { id: "enlightened", path: "classes/Enlightened.json", label: "Enlightened.json" },
  { id: "diviner", path: "classes/Diviner.json", label: "Diviner.json" },
  { id: "sylvan", path: "classes/Sylvan.json", label: "Sylvan.json" },
  { id: "stalker", path: "classes/Stalker.json", label: "Stalker.json" },
  { id: "strategist", path: "classes/Strategist.json", label: "Strategist.json" },
  { id: "general", path: "classes/General.json", label: "General.json" },
  { id: "elder-monk", path: "classes/Elder Monk.json", label: "Elder Monk.json" },
  { id: "ki-warrior", path: "classes/Ki Warrior.json", label: "Ki Warrior.json" },
  { id: "psykic", path: "classes/Psykic.json", label: "Psykic.json" },
  { id: "sage", path: "classes/Sage.json", label: "Sage.json" },
  { id: "oathsworn", path: "classes/Oathsworn.json", label: "Oathsworn.json" },
  { id: "dragonblood", path: "classes/Dragonblood.json", label: "Dragonblood.json" },
  { id: "shadow", path: "classes/Shadow.json", label: "Shadow.json" },
  { id: "eminence", path: "classes/Eminence.json", label: "Eminence.json" },
  { id: "lich", path: "classes/Lich.json", label: "Lich.json" },
  { id: "hero", path: "classes/Hero.json", label: "Hero.json" },
  { id: "demon-lord", path: "classes/Demon Lord.json", label: "Demon Lord.json" },
  { id: "archivist", path: "classes/Archivist.json", label: "Archivist.json" },
  { id: "bounty-broker", path: "classes/Bounty Broker.json", label: "Bounty Broker.json" },
  { id: "bulwark", path: "classes/Bulwark.json", label: "Bulwark.json" },
  { id: "cartographer", path: "classes/Cartographer.json", label: "Cartographer.json" },
  { id: "chronomancer", path: "classes/Chronomancer.json", label: "Chronomancer.json" },
  { id: "courier", path: "classes/Courier.json", label: "Courier.json" },
  { id: "drakeslayer", path: "classes/Drakeslayer.json", label: "Drakeslayer.json" },
  { id: "dreamweaver", path: "classes/Dreamweaver.json", label: "Dreamweaver.json" },
  { id: "echo-knight", path: "classes/Echo Knight.json", label: "Echo Knight.json" },
  { id: "element-siphon", path: "classes/Element Siphon.json", label: "Element Siphon.json" },
  { id: "fencer", path: "classes/Fencer.json", label: "Fencer.json" },
  { id: "flagbearer", path: "classes/Flagbearer.json", label: "Flagbearer.json" },
  { id: "gambit", path: "classes/Gambit.json", label: "Gambit.json" },
  { id: "hexblade", path: "classes/Hexblade.json", label: "Hexblade.json" },
  { id: "lifewarden", path: "classes/Lifewarden.json", label: "Lifewarden.json" },
  { id: "mirror-mage", path: "classes/Mirror Mage.json", label: "Mirror Mage.json" },
  { id: "nullblade", path: "classes/Nullblade.json", label: "Nullblade.json" },
  { id: "pactkeeper", path: "classes/Pactkeeper.json", label: "Pactkeeper.json" },
  { id: "pilgrim", path: "classes/Pilgrim.json", label: "Pilgrim.json" },
  { id: "plaguebinder", path: "classes/Plaguebinder.json", label: "Plaguebinder.json" },
  { id: "poisoner", path: "classes/Poisoner.json", label: "Poisoner.json" },
  { id: "quartermaster", path: "classes/Quartermaster.json", label: "Quartermaster.json" },
  { id: "reaver", path: "classes/Reaver.json", label: "Reaver.json" },
  { id: "relic-warden", path: "classes/Relic Warden.json", label: "Relic Warden.json" },
  { id: "riverstrider", path: "classes/Riverstrider.json", label: "Riverstrider.json" },
  { id: "sanguine-medic", path: "classes/Sanguine Medic.json", label: "Sanguine Medic.json" },
  { id: "scavenger", path: "classes/Scavenger.json", label: "Scavenger.json" },
  { id: "soul-shepherd", path: "classes/Soul Shepherd.json", label: "Soul Shepherd.json" },
  { id: "spellbreaker", path: "classes/Spellbreaker.json", label: "Spellbreaker.json" },
  { id: "stormcaller", path: "classes/Stormcaller.json", label: "Stormcaller.json" },
  { id: "sunstrider", path: "classes/Sunstrider.json", label: "Sunstrider.json" },
  { id: "wildwarden", path: "classes/Wildwarden.json", label: "Wildwarden.json" },
];
const CLASS_CACHE_KEYS = {
  warrior: "emberfall-warrior-cache",
  archer: "emberfall-archer-cache",
  mage: "emberfall-mage-cache",
  rogue: "emberfall-rogue-cache",
  monster: "emberfall-monster-cache",
  beast: "emberfall-beast-cache",
  "great-beast": "emberfall-great-beast-cache",
  druid: "emberfall-druid-cache",
  warlock: "emberfall-warlock-cache",
  priest: "emberfall-priest-cache",
  knight: "emberfall-knight-cache",
  spellblade: "emberfall-spellblade-cache",
  hunter: "emberfall-hunter-cache",
  "trap-layer": "emberfall-trap-layer-cache",
  "trap-master": "emberfall-trap-master-cache",
  traveler: "emberfall-traveler-cache",
  "raid-boss": "emberfall-raid-boss-cache",
  sharmat: "emberfall-sharmat-cache",
  dragonkin: "emberfall-dragonkin-cache",
  cleric: "emberfall-cleric-cache",
  paladin: "emberfall-paladin-cache",
  sharpshooter: "emberfall-sharpshooter-cache",
  thief: "emberfall-thief-cache",
  vampire: "emberfall-vampire-cache",
  "vampire-lord": "emberfall-vampire-lord-cache",
  monk: "emberfall-monk-cache",
  noble: "emberfall-noble-cache",
  assassin: "emberfall-assassin-cache",
  barbarian: "emberfall-barbarian-cache",
  stonebreaker: "emberfall-stonebreaker-cache",
  summoner: "emberfall-summoner-cache",
  wizard: "emberfall-wizard-cache",
  spellcaster: "emberfall-spellcaster-cache",
  sorcerer: "emberfall-sorcerer-cache",
  "heavy-knight": "emberfall-heavy-knight-cache",
  "jack-of-all-trades": "emberfall-jack-of-all-trades-cache",
  jester: "emberfall-jester-cache",
  joker: "emberfall-joker-cache",
  cultist: "emberfall-cultist-cache",
  "chaos-cultist": "emberfall-chaos-cultist-cache",
  brawler: "emberfall-brawler-cache",
  "spell-sniper": "stat-fick-spell-sniper-cache",
  bard: "stat-fick-bard-cache",
  beastmaster: "stat-fick-beastmaster-cache",
  salesman: "stat-fick-salesman-cache",
  scholar: "stat-fick-scholar-cache",
  slayer: "stat-fick-slayer-cache",
  energist: "stat-fick-energist-cache",
  gladiator: "stat-fick-gladiator-cache",
  champion: "stat-fick-champion-cache",
  weaponmaster: "stat-fick-weaponmaster-cache",
  "wise-man": "stat-fick-wise-man-cache",
  guardian: "stat-fick-guardian-cache",
  pyromancer: "stat-fick-pyromancer-cache",
  aquamancer: "stat-fick-aquamancer-cache",
  geomancer: "stat-fick-geomancer-cache",
  aeromancer: "stat-fick-aeromancer-cache",
  electromancer: "stat-fick-electromancer-cache",
  cryomancer: "stat-fick-cryomancer-cache",
  luxomancer: "stat-fick-luxomancer-cache",
  umbramancer: "stat-fick-umbramancer-cache",
  botanomancer: "stat-fick-botanomancer-cache",
  hemomancer: "stat-fick-hemomancer-cache",
  necromancer: "stat-fick-necromancer-cache",
  hierophant: "stat-fick-hierophant-cache",
  avatar: "stat-fick-avatar-cache",
  berserker: "stat-fick-berserker-cache",
  madman: "stat-fick-madman-cache",
  "great-mage": "stat-fick-great-mage-cache",
  reveler: "stat-fick-reveler-cache",
  templar: "stat-fick-templar-cache",
  "monster-hunter": "stat-fick-monster-hunter-cache",
  "blood-hunter": "stat-fick-blood-hunter-cache",
  "cardinal-priest": "stat-fick-cardinal-priest-cache",
  soldier: "stat-fick-soldier-cache",
  "black-knight": "stat-fick-black-knight-cache",
  "death-paladin": "stat-fick-death-paladin-cache",
  ambusher: "stat-fick-ambusher-cache",
  "demon-hunter": "stat-fick-demon-hunter-cache",
  enlightened: "stat-fick-enlightened-cache",
  diviner: "stat-fick-diviner-cache",
  sylvan: "stat-fick-sylvan-cache",
  stalker: "stat-fick-stalker-cache",
  strategist: "stat-fick-strategist-cache",
  general: "stat-fick-general-cache",
  "elder-monk": "stat-fick-elder-monk-cache",
  "ki-warrior": "stat-fick-ki-warrior-cache",
  psykic: "stat-fick-psykic-cache",
  sage: "stat-fick-sage-cache",
  oathsworn: "stat-fick-oathsworn-cache",
  dragonblood: "stat-fick-dragonblood-cache",
  shadow: "stat-fick-shadow-cache",
  eminence: "stat-fick-eminence-cache",
  lich: "stat-fick-lich-cache",
  hero: "stat-fick-hero-cache",
  "demon-lord": "stat-fick-demon-lord-cache",
};
const STAT_CODES = { S: "strength", V: "vitality", M: "mind", D: "dexterity", I: "intelligence", W: "wisdom", P: "speed", L: "luck" };
const SPELL_ELEMENTS = ["basic", "fire", "water", "holy", "lightning", "blood", "dark", "ice", "earth", "wind", "nature", "death"];
/* Port task 4 (the Artifact Rework, 2026-11-06; decisions A1–A28 in engine/Docs/Engine-Plan.md): the effect kinds the
   new artifacts bring, and the extra fields each takes. Their rules live in calculateArtifactEffects and combat.js. */
const ARTIFACT_EFFECT_KINDS = [
  "money-percent", "exp-percent", "weapon-lifesteal-percent", "damage-lifesteal-percent", "melee-thorns-percent",
  "turn-start-heal-percent", "turn-start-mana-percent", "first-hit-guard-percent", "first-strike-damage-percent",
  "damage-percent-vs-race", "element-resistance-percent", "healing-received-percent", "healing-given-percent",
  "incoming-spell-damage-percent", "spell-hit-mana-percent", "damage-percent-vs-low-hp", "damage-percent-vs-larger-max-hp",
  "battle-start-weapon-power-percent", "free-spell-chance", "battle-start-barrier-percent", "battle-start-party-stats-percent",
  "on-hit-burn", "on-hit-slow", "on-hit-armor-shred", "turn-start-cleanse-chance", "kill-restore-percent",
  "evade-next-attack-percent", "death-save", "spell-chain-percent", "first-turn-ap", "ally-low-hp-barrier",
  "on-hit-stun-chance", "on-hit-poison", "low-hp-fury", "party-revive", "turn-start-star", "party-damage-armor-percent",
  "crit-damage-percent",
  // Treasure Seeker (port task 5, M13–M17).
  "all-money-percent", "luck-per-platinum",
];
const ARTIFACT_EFFECT_FIELDS = {
  "damage-percent-vs-race": { raceIds: "races" }, "element-resistance-percent": { element: "element" },
  "turn-start-star": { element: "element" }, "damage-percent-vs-low-hp": { thresholdPercent: "percent" },
  "ally-low-hp-barrier": { thresholdPercent: "percent" }, "low-hp-fury": { thresholdPercent: "percent", evasion: "number" },
  "battle-start-weapon-power-percent": { durationTurns: "turns" }, "battle-start-party-stats-percent": { durationTurns: "turns" },
  "on-hit-burn": { chance: "percent", durationTurns: "turns" }, "on-hit-slow": { chance: "percent", durationTurns: "turns" },
  "on-hit-armor-shred": { durationTurns: "turns", maxStacks: "count" }, "on-hit-poison": { durationTurns: "turns" },
  "kill-restore-percent": { mana: "number" }, "party-damage-armor-percent": { armor: "number" },
  "low-hp-summon": { strengthPercent: "number" },
};
/* Masterwork artifacts are fixed at the level they were obtained (A5): "<base id>-lv<level>". Their numbers rise on a
   straight line from their Level 1 value to their Level 80 value and keep rising at that rate (A6), rounded to one
   decimal; the price likewise, to the nearest 100 silver. */
const MASTERWORK_ARTIFACT_BASE_LEVEL = 80;
const MASTERWORK_ARTIFACT_MAX_LEVEL = 200;
const masterworkArtifactLevelPattern = /^(artifact-.+)-lv(\d{1,3})$/;
// Rounded to one decimal, or two where an effect asks for it (Treasure Seeker's +0.15 Luck per Platinum at Level 1, M17).
const rampedValue = (pair, level, decimals = 1) => Math.round((pair[0] + (pair[1] - pair[0]) * (level - 1) / 79) * 10 ** decimals) / 10 ** decimals;
const leveledArtifactCache = new Map();

const statDefinitions = [
  { key: "strength", label: "Strength", short: "STR", color: "#c7683f" },
  { key: "vitality", label: "Vitality", short: "VIT", color: "#bb7b4b" },
  { key: "mind", label: "Mind", short: "MND", color: "#8c6f83" },
  { key: "dexterity", label: "Dexterity", short: "DEX", color: "#738f88" },
  { key: "intelligence", label: "Intelligence", short: "INT", color: "#68849a" },
  { key: "wisdom", label: "Wisdom", short: "WIS", color: "#858b6c" },
  { key: "speed", label: "Speed", short: "SPD", color: "#9c7c58" },
  { key: "luck", label: "Luck", short: "LCK", color: "#ae6f73" },
];

const currencyDefinitions = ["silver", "gold", "platinum"];
const defaultStats = Object.fromEntries(statDefinitions.map(({ key }) => [key, 10]));
const defaultAllocations = Object.fromEntries(statDefinitions.map(({ key }) => [key, 0]));
const defaultCurrencies = { silver: 0, gold: 5, platinum: 0 };
const bonusDefinitions = statDefinitions.map(({ key }) => key);
const defenseBonusDefinitions = ["armor", "evasion"];
const defaultBonuses = Object.fromEntries(bonusDefinitions.map((key) => [key, 0]));
const defaultDefenseBonuses = Object.fromEntries(defenseBonusDefinitions.map((key) => [key, 0]));
const equipmentSlotDefinitions = [
  { key: "primaryHand", label: "Primary hand", elementId: "equipment-primary-hand" },
  { key: "offHand", label: "Off-hand", elementId: "equipment-off-hand" },
  { key: "armor", label: "Armor", elementId: "equipment-armor" },
  { key: "artifact1", label: "Artifact I", elementId: "equipment-artifact-1" },
  { key: "artifact2", label: "Artifact II", elementId: "equipment-artifact-2" },
];
const defaultEquipment = Object.fromEntries(equipmentSlotDefinitions.map(({ key }) => [key, null]));
const statsGrid = document.querySelector("#stats-grid");
const levelValue = document.querySelector("#level-value");
const hpCurrent = document.querySelector("#hp-current");
const hpMax = document.querySelector("#hp-max");
const manaCurrent = document.querySelector("#mana-current");
const manaMax = document.querySelector("#mana-max");
const armorBaseValue = document.querySelector("#armor-base-value");
const armorModifiedValue = document.querySelector("#armor-modified-value");
const armorValue = document.querySelector("#armor-value");
const evasionBaseValue = document.querySelector("#evasion-base-value");
const evasionModifiedValue = document.querySelector("#evasion-modified-value");
const evasionValue = document.querySelector("#evasion-value");
const expCurrent = document.querySelector("#exp-current");
const expThreshold = document.querySelector("#exp-threshold");
const expHelper = document.querySelector("#exp-helper");
const expBar = document.querySelector("#exp-bar");
const expTrack = document.querySelector("#exp-track");
const expForm = document.querySelector("#exp-form");
const expInput = document.querySelector("#exp-input");
const backupExportButton = document.querySelector("#backup-export-button");
const backupRestoreButton = document.querySelector("#backup-restore-button");
const backupFileInput = document.querySelector("#backup-file-input");
const backupStatus = document.querySelector("#backup-status");
const restoreButton = document.querySelector("#restore-button");
const resetButton = document.querySelector("#reset-button");
const levelUpButton = document.querySelector("#level-up-button");
const levelIcon = document.querySelector("#level-icon");
const editingToggle = document.querySelector("#editing-toggle");
const pointsValue = document.querySelector("#points-value");
const respecButton = document.querySelector("#respec-button");
const themeToggle = document.querySelector("#theme-toggle");
const themeIcon = document.querySelector("#theme-icon");
const statusDot = document.querySelector(".status-dot");
const spellbookOpenButton = document.querySelector("#spellbook-open-button");
const spellbookModal = document.querySelector("#spellbook-modal");
const spellPicker = document.querySelector("#spell-picker");
const spellAddButton = document.querySelector("#spell-add-button");
const spellRefreshButton = document.querySelector("#spell-refresh-button");
const spellFileButton = document.querySelector("#spell-file-button");
const spellFileInput = document.querySelector("#spell-file-input");
const knownSpellsList = document.querySelector("#known-spells-list");
const availableSpellsList = document.querySelector("#available-spells-list");
const spellDetailPanel = document.querySelector("#spell-detail-panel");
const spellSourceStatus = document.querySelector("#spell-source-status");
const spellbookStatus = document.querySelector("#spellbook-status");
const spellbookManaCurrent = document.querySelector("#spellbook-mana-current");
const spellbookManaMax = document.querySelector("#spellbook-mana-max");
const spellFilterButtons = document.querySelectorAll("[data-spell-filter]");
const spellCatalogueSearchInput = document.querySelector("#spell-catalogue-search");
const equipmentSlots = Object.fromEntries(equipmentSlotDefinitions.map(({ key, elementId }) => [key, document.querySelector(`#${elementId}`)]));
const inventoryOpenButton = document.querySelector("#inventory-open-button");
const inventoryModal = document.querySelector("#inventory-modal");
const itemPicker = document.querySelector("#item-picker");
const itemAddButton = document.querySelector("#item-add-button");
const itemRefreshButton = document.querySelector("#item-refresh-button");
const artifactsFileButton = document.querySelector("#artifacts-file-button");
const artifactsFileInput = document.querySelector("#artifacts-file-input");
const ownedItemsList = document.querySelector("#owned-items-list");
const availableItemsList = document.querySelector("#available-items-list");
const itemDetailPanel = document.querySelector("#item-detail-panel");
const itemSourceStatus = document.querySelector("#item-source-status");
const inventoryStatus = document.querySelector("#inventory-status");
const inventoryCount = document.querySelector("#inventory-count");
const itemFilterButtons = document.querySelectorAll("[data-item-filter]");
const itemPickerFilterButtons = document.querySelectorAll("[data-item-picker-filter]");
const itemCatalogueSearchInput = document.querySelector("#item-catalogue-search");
const classesOpenButton = document.querySelector("#classes-open-button");
const classesModal = document.querySelector("#classes-modal");
const classRefreshButton = document.querySelector("#class-refresh-button");
const classList = document.querySelector("#class-list");
const availableClassList = document.querySelector("#available-class-list");
const classDetailPanel = document.querySelector("#class-detail-panel");
const classesStatus = document.querySelector("#classes-status");
const perkPointsValue = document.querySelector("#perk-points-value");
const classSpellChoiceModal = document.querySelector("#class-spell-choice-modal");
const classSpellChoiceList = document.querySelector("#class-spell-choice-list");
const classSpellChoiceIntro = document.querySelector("#class-spell-choice-intro");
const classSpellChoiceStatus = document.querySelector("#class-spell-choice-status");
const classSpellChoiceConfirm = document.querySelector("#class-spell-choice-confirm");
const skillsOpenButton = document.querySelector("#skills-open-button");
const skillsModal = document.querySelector("#skills-modal");
const skillList = document.querySelector("#skill-list");
const skillDetailPanel = document.querySelector("#skill-detail-panel");
const skillsStatus = document.querySelector("#skills-status");
const passivesOpenButton = document.querySelector("#passives-open-button");
const passivesModal = document.querySelector("#passives-modal");
const passiveList = document.querySelector("#passive-list");
const passiveDetailPanel = document.querySelector("#passive-detail-panel");
const passivesStatus = document.querySelector("#passives-status");
const raceSelectButton = document.querySelector("#race-select-button");
const raceIcon = document.querySelector("#race-icon");
const heroRaceName = document.querySelector("#hero-race-name");
const raceModal = document.querySelector("#race-modal");
const raceList = document.querySelector("#race-list");
const raceSourceStatus = document.querySelector("#race-source-status");
const raceRefreshButton = document.querySelector("#race-refresh-button");
const raceStatus = document.querySelector("#race-status");

let spells = [];
let spellTypeFilter = "all";
let itemCatalog = { weapons: [], armor: [], artifacts: [], consumables: [], keyItems: [] };
let items = [];
let itemsById = new Map();
let itemFilter = "all";
let itemPickerFilter = "all";
let inventoryEquipSlot = null;
let itemPickerSlot = null;
const itemSourceStates = { artifacts: "loading" };
let classCatalog = [];
let selectedClassId = null;
let classLevels = {};
let activeSkills = {};
let classSourceState = "loading";
let raceCatalog = [];
let raceSourceState = "loading";

function calculateMaxHp(values) {
  return Math.max(0, Math.floor(((1.5 * values.vitality) + (values.strength / 2) + (values.mind / 2)) * 1.25));
}

function calculateMaxMana(values) {
  return Math.max(0, Math.floor(values.mind + (values.intelligence / 2) + (values.wisdom / 2)));
}

function calculateEvasion(values) {
  return (BASE_EVASION + (1.5 * values.dexterity) + (0.5 * values.luck) + (0.5 * values.wisdom)) / 2;
}

function calculateCurrentStats(baseValues, percentageValues) {
  return Object.fromEntries(statDefinitions.map(({ key }) => [
    key,
    Math.floor(baseValues[key] * (1 + (toNumber(percentageValues[key], 0) / 100))),
  ]));
}

function bonusValues(values, definitions = bonusDefinitions) {
  return Object.fromEntries(definitions.map((key) => [key, toNumber(values[key], 0)]));
}

function emptyArtifactEffects() {
  return {
    statFlat: { ...defaultBonuses },
    statPercent: { ...defaultBonuses },
    statPermanentPercent: { ...defaultBonuses },
    maxHpFlat: 0,
    maxHpPercent: 0,
    maxManaFlat: 0,
    maxManaPercent: 0,
    weaponPowerFlat: 0,
    weaponPowerPercent: 0,
    weaponCritFlat: 0,
    weaponModifiers: [],
    armorFlat: 0,
    armorPercent: 0,
    evasionFlat: 0,
    evasionPercent: 0,
    weaponHitFlat: 0,
    weaponHitPercent: 0,
    spellCostFlat: 0,
    spellCostPercent: 0,
    spellPowerFlat: 0,
    spellPowerPercent: 0,
    spellModifiers: [],
    trapLimitFlat: 0,
  };
}

function equippedArtifactItems() {
  return [equipment.artifact1, equipment.artifact2]
    .map((itemId) => getItem(itemId))
    .filter((item) => item && item.category === "artifact");
}

// Treasure Seeker (port task 5, M13): battle silver, quest rewards and sales rise by the best copy worn in the party;
// two copies don't add up.
function treasureMoneyPercent() {
  const worn = [equipment, ...companions.map(companion => companion.character?.equipment || {})];
  const amounts = worn.flatMap(slots => [slots?.artifact1, slots?.artifact2]).filter(Boolean).map(getItem)
    .filter(item => item?.category === "artifact").flatMap(item => item.effects.filter(effect => effect.kind === "all-money-percent").map(effect => toNumber(effect.amount, 0)));
  return Math.max(0, ...amounts);
}

function calculateArtifactEffects() {
  return window.Combat?.cacheCalculation?.("artifact-effects", calculateArtifactEffectsUncached) ?? calculateArtifactEffectsUncached();
}

function calculateArtifactEffectsUncached() {
  const effects = emptyArtifactEffects();
  equippedArtifactItems().forEach((artifact) => {
    artifact.effects.forEach((effect) => {
      const amount = toNumber(effect.amount, 0);
      if (effect.kind === "stat-flat" && effect.target in effects.statFlat) effects.statFlat[effect.target] += amount;
      if (effect.kind === "stat-percent" && effect.target in effects.statPercent) effects.statPercent[effect.target] += amount;
      if (effect.kind === "stat-permanent-percent" && effect.target in effects.statPermanentPercent) effects.statPermanentPercent[effect.target] += amount;
      // Treasure Seeker: Luck in proportion to the money you hold, per whole Platinum's worth (M14).
      if (effect.kind === "luck-per-platinum") effects.statFlat.luck += Math.floor(ShopRules.walletValue(currencies) / ShopRules.SILVER_PER_PLATINUM * amount);
      if (effect.kind === "max-hp-flat") effects.maxHpFlat += amount;
      if (effect.kind === "max-hp-percent") effects.maxHpPercent += amount;
      if (effect.kind === "max-mana-flat") effects.maxManaFlat += amount;
      if (effect.kind === "max-mana-percent") effects.maxManaPercent += amount;
      if (["weapon-power-flat", "weapon-power-percent", "weapon-hit-flat", "weapon-hit-percent", "weapon-crit-flat"].includes(effect.kind) && effect.weaponType) {
        effects.weaponModifiers.push(effect);
      } else {
        if (effect.kind === "weapon-power-flat") effects.weaponPowerFlat += amount;
        if (effect.kind === "weapon-power-percent") effects.weaponPowerPercent += amount;
        if (effect.kind === "weapon-hit-flat") effects.weaponHitFlat += amount;
        if (effect.kind === "weapon-hit-percent") effects.weaponHitPercent += amount;
        if (effect.kind === "weapon-crit-flat") effects.weaponCritFlat += amount;
      }
      if (effect.kind === "armor-flat") effects.armorFlat += amount;
      if (effect.kind === "armor-percent") effects.armorPercent += amount;
      if (effect.kind === "evasion-flat") effects.evasionFlat += amount;
      if (effect.kind === "evasion-percent") effects.evasionPercent += amount;
      if (["spell-cost-flat", "spell-cost-percent", "spell-power-flat", "spell-power-percent"].includes(effect.kind) && (effect.element || effect.maxTier != null)) {
        effects.spellModifiers.push(effect);
      } else {
        if (effect.kind === "spell-cost-flat") effects.spellCostFlat += amount;
        if (effect.kind === "spell-cost-percent") effects.spellCostPercent += amount;
        if (effect.kind === "spell-power-flat") effects.spellPowerFlat += amount;
        if (effect.kind === "spell-power-percent") effects.spellPowerPercent += amount;
      }
      if (effect.kind === "trap-limit-flat") effects.trapLimitFlat += amount;
    });
  });
  if (toInteger(classLevels["relic-warden"], 0) >= 4) {
    for (const key of bonusDefinitions) {
      effects.statFlat[key] *= 1.1;
      effects.statPercent[key] *= 1.1;
      effects.statPermanentPercent[key] *= 1.1;
    }
  }
  return effects;
}

function artifactWeaponModifier(item, effects, kind) {
  return effects.weaponModifiers.reduce((total, effect) => {
    const exactType = String(item.type || "").toLowerCase() === effect.weaponType;
    const mechanicalType = normalizeWeaponType(item.type) === normalizeWeaponType(effect.weaponType);
    return effect.kind === kind && (exactType || mechanicalType) ? total + toNumber(effect.amount, 0) : total;
  }, 0);
}

function artifactSpellModifier(effects, kind, spell) {
  return effects.spellModifiers.reduce((total, effect) => {
    const elementMatches = !effect.element || effect.element === spell.element;
    const tierMatches = effect.maxTier == null || spell.tier <= effect.maxTier;
    return effect.kind === kind && elementMatches && tierMatches ? total + toNumber(effect.amount, 0) : total;
  }, 0);
}

function emptyClassEffects() {
  return {
    statPermanentPercent: { ...defaultBonuses },
    defensePermanentPercent: { ...defaultDefenseBonuses },
    maxHpPercent: 0,
    maxManaPercent: 0,
    defenseFlat: { armor: 0, evasion: 0 },
    defenseCap: { armor: Infinity, evasion: Infinity },
    heavyArmorEvasionDebuffReduction: 0,
    heavyArmorEvasionPenaltyReduction: 0,
    weaponHitFlat: 0,
    weaponHitFlatByType: {},
    weaponHitFlatByItem: {},
    weaponHitPercent: 0,
    weaponHitPercentByType: {},
    weaponHitPercentByItem: {},
    weaponPowerFlat: 0,
    weaponPowerFlatByType: {},
    weaponPowerFlatByItem: {},
    weaponPowerPercent: 0,
    weaponPowerPercentByType: {},
    weaponPowerPercentByItem: {},
    weaponPowerMultiplier: 1,
    weaponScalingModifiers: [],
    weaponCritFlat: 0,
    weaponCritFlatByType: {},
    weaponCritFlatByItem: {},
    weaponCritDisabled: false,
    weaponCritAlways: false,
    weaponHitAlways: false,
    incomingWeaponHitPercentByType: {},
    unarmedPowerPercent: 0,
    unarmedPowerFlat: 0,
    unarmedHitFlat: 0,
    unarmedHitPercent: 0,
    unarmedScalingModifiers: [],
    spellCostPercentModifiers: [],
    spellActionPointSetModifiers: [],
    spellActionPointFlatModifiers: [],
    spellLearningCostPercentModifiers: [],
    spellCostMultiplier: 1,
    spellPowerFlat: 0,
    spellPowerPercent: 0,
    spellPowerPercentModifiers: [],
    spellDamagePercentModifiers: [],
    spellAdditionalElementModifiers: [],
    incomingSpellDamagePercentByElement: {},
    incomingSpellDamagePercent: 0,
    passives: [],
    partyStatPercentModifiers: [],
    skills: [],
    classRequirementBypasses: [],
    trapLimitFlat: 0,
    trapDamagePercent: 0,
    trapArmorPiercePercent: 0,
    trapRetentionChance: 0,
    trapAdditionalTriggers: 0,
    travelSpeedFlat: 0,
    riverSpeedFlat: 0,
    encounterChancePercent: 0,
    surveyorsEye: false,
    consumableRetentionChance: 0,
    partyConsumablePowerPercent: 0,
    artifactStatPowerPercent: 0,
    poisonOnHitDamage: 0,
    poisonOnHitPercentMaxHp: 0,
    poisonHealingReductionPercent: 0,
    echoWeaponHitPercent: 0,
    campingSupplyRetentionChance: 0,
    incomingDamagePercent: 0,
    incomingAttackDamagePercentByKind: {},
    outgoingDamagePercent: 0,
    damagePercentByClass: {},
    damagePercentByRace: {},
    incomingDamagePercentByClass: {},
    weaponArmorPiercePercent: 0,
    weaponArmorPiercePercentByType: {},
    criticalDamagePercent: 0,
    equipmentArmorPercentByType: {},
    stunImmune: false,
    actionRestrictions: [],
    experienceRewardMultiplier: 1,
    actionPointsFlat: 0,
    stanceBonusPercent: 0,
    questMoneyPercent: 0,
    questReputationPercent: 0,
    guildQuestChoiceFlat: 0,
    enemyMoneyPercent: 0,
    salePricePercent: 0,
    summonBaseStatsPercent: 0,
    additionalSummonCount: 0,
    summonSpellElement: null,
    masterworkDropChanceFlat: 0,
    skillUpgrades: [],
    passiveUpgrades: [],
  };
}

function normalizeWeaponType(type) {
  const normalized = String(type || "").trim().toLowerCase();
  return ["meele", "magic-wand", "spell-staff"].includes(normalized) ? "melee" : normalized;
}

function classSkillKey(skill) {
  return `${skill.classId}:${skill.id}`;
}

function classWeaponBonus(item, totals, field) {
  const type = normalizeWeaponType(item.type);
  const bonuses = totals[field + "ByType"];
  const itemBonuses = totals[field + "ByItem"] || {};
  let bonus = totals[field] + (bonuses[type] || 0) + (itemBonuses[item.id] || 0);
  // The weapon catalogue uses "ranged" for bows; recognize their names too.
  const isBow = item.isBow === true || ["bow", "shortbow", "longbow"].includes(type) ||
    (type === "ranged" && /\b(?:bow|shortbow|longbow)\b/i.test(item.name));
  if (isBow && type !== "bow") bonus += bonuses.bow || 0;
  if (isBow && type !== "ranged") bonus += bonuses.ranged || 0;
  if (item.handed === 1) bonus += bonuses["one-handed"] || 0;
  if (item.handed === 1 || isBow) bonus += bonuses["one-handed-or-bow"] || 0;
  return bonus;
}

function addClassEffect(effect, totals) {
  const type = effect?.type || effect?.kind;
  const amount = toNumber(effect?.amount, 0);
  if (type === "all-stats-permanent-percent") {
    bonusDefinitions.forEach((key) => { totals.statPermanentPercent[key] += amount; });
  }
  if (type === "stat-permanent-percent" && effect.target in totals.statPermanentPercent) {
    totals.statPermanentPercent[effect.target] += amount;
  }
  if (type === "conditional-stat-permanent-percent" && effect.target in totals.statPermanentPercent && classEffectConditionMet(effect)) {
    totals.statPermanentPercent[effect.target] += amount;
  }
  if (type === "stat-permanent-percent-per-enemy" && effect.target in totals.statPermanentPercent) {
    totals.statPermanentPercent[effect.target] += amount * Math.max(0, enemyCount);
  }
  if (type === "combat-all-stats-percent-per-gold" && window.Combat?.active()) {
    const goldPerStep = Math.max(1, toNumber(effect.goldPerStep, 5));
    const maximum = Math.max(0, toNumber(effect.maximum, 20));
    const heldGold = ShopRules.walletValue(currencies) / ShopRules.SILVER_PER_GOLD;
    const bonus = Math.min(maximum, Math.floor(heldGold / goldPerStep) * amount);
    bonusDefinitions.forEach((key) => { totals.statPermanentPercent[key] += bonus; });
  }
  if (type === "conditional-all-stats-permanent-percent" && classEffectConditionMet(effect)) {
    bonusDefinitions.forEach((key) => { totals.statPermanentPercent[key] += amount; });
  }
  if (type === "max-hp-percent") totals.maxHpPercent += amount;
  if (type === "max-mana-percent") totals.maxManaPercent += amount;
  if (type === "trap-limit-flat") totals.trapLimitFlat += amount;
  if (type === "trap-damage-percent") totals.trapDamagePercent += amount;
  if (type === "trap-armor-pierce-percent") totals.trapArmorPiercePercent += amount;
  if (type === "trap-retention-chance") totals.trapRetentionChance += amount;
  if (type === "trap-additional-triggers") totals.trapAdditionalTriggers += amount;
  if (type === "travel-speed-flat") totals.travelSpeedFlat += amount;
  if (type === "river-speed-flat") totals.riverSpeedFlat += amount;
  if (type === "encounter-chance-percent") totals.encounterChancePercent += amount;
  if (type === "surveyors-eye") totals.surveyorsEye = true;
  if (type === "consumable-retention-chance") totals.consumableRetentionChance += amount;
  if (type === "party-consumable-power-percent") totals.partyConsumablePowerPercent += amount;
  if (type === "artifact-stat-power-percent") totals.artifactStatPowerPercent += amount;
  if (type === "poison-on-hit") {
    totals.poisonOnHitDamage += amount;
    totals.poisonOnHitPercentMaxHp += Math.max(0, toNumber(effect.percentMaxHp, 0));
  }
  if (type === "poison-healing-reduction-percent") totals.poisonHealingReductionPercent += amount;
  if (type === "echo-weapon-hit") totals.echoWeaponHitPercent += amount;
  if (type === "artifact-count-defense-flat" && equippedArtifactItems().length >= Math.max(1, toInteger(effect.minimum, 1))) totals.defenseFlat.armor += amount;
  if (type === "artifact-count-defense-percent" && equippedArtifactItems().length >= Math.max(1, toInteger(effect.minimum, 1))) totals.defensePermanentPercent.armor += amount;
  if (type === "fencer-evasion-flat" && fencerLoadoutActive()) totals.defenseFlat.evasion += amount;
  if (type === "fencer-evasion-percent" && fencerLoadoutActive()) totals.defensePermanentPercent.evasion += amount;
  if (fencerLoadoutActive() && type === "fencer-weapon-hit-flat") totals.weaponHitFlat += amount;
  if (fencerLoadoutActive() && type === "fencer-weapon-hit-percent") totals.weaponHitPercent += amount;
  if (fencerLoadoutActive() && type === "fencer-weapon-power-percent") totals.weaponPowerPercent += amount;
  if (fencerLoadoutActive() && type === "fencer-weapon-crit-flat") totals.weaponCritFlat += amount;
  if (type === "camping-supply-retention-chance") totals.campingSupplyRetentionChance += amount;
  if (type === "incoming-damage-percent") totals.incomingDamagePercent += amount;
  if (type === "incoming-attack-damage-percent") {
    for (const kind of effect.attackKinds || []) {
      const normalized = normalizeWeaponType(kind);
      totals.incomingAttackDamagePercentByKind[normalized] = (totals.incomingAttackDamagePercentByKind[normalized] || 0) + amount;
    }
  }
  if (type === "outgoing-damage-percent") totals.outgoingDamagePercent += amount;
  if (type === "party-stat-percent") totals.partyStatPercentModifiers.push({ ...effect });
  if (type === "damage-percent-vs-class") for (const classId of effect.classIds || [effect.classId]) {
    if (!classId) continue;
    const key = slugify(classId);
    totals.damagePercentByClass[key] = (totals.damagePercentByClass[key] || 0) + amount;
  }
  if (type === "damage-percent-vs-race") for (const raceId of effect.raceIds || [effect.raceId]) {
    if (!raceId) continue;
    const key = slugify(raceId);
    totals.damagePercentByRace[key] = (totals.damagePercentByRace[key] || 0) + amount;
  }
  if (type === "incoming-damage-percent-vs-class") for (const classId of effect.classIds || [effect.classId]) {
    if (!classId) continue;
    const key = slugify(classId);
    totals.incomingDamagePercentByClass[key] = (totals.incomingDamagePercentByClass[key] || 0) + amount;
  }
  if (type === "incoming-damage-percent-vs-race") totals.incomingDamagePercentByRace ||= {};
  if (type === "incoming-damage-percent-vs-race") for (const raceId of effect.raceIds || [effect.raceId]) {
    if (!raceId) continue;
    const key = slugify(raceId);
    totals.incomingDamagePercentByRace[key] = (totals.incomingDamagePercentByRace[key] || 0) + amount;
  }
  if (type === "outgoing-damage-percent-per-enemy") {
    const maximumEnemies = Math.max(0, toInteger(effect.maximumEnemies, 5));
    totals.outgoingDamagePercent += amount * Math.min(Math.max(0, enemyCount), maximumEnemies);
  }
  if (type === "weapon-armor-pierce-percent") {
    const weaponType = effect.weaponType ? normalizeWeaponType(effect.weaponType) : "all";
    if (weaponType === "all") totals.weaponArmorPiercePercent += amount;
    else totals.weaponArmorPiercePercentByType[weaponType] = (totals.weaponArmorPiercePercentByType[weaponType] || 0) + amount;
  }
  if (type === "critical-damage-percent") totals.criticalDamagePercent += amount;
  if (type === "equipment-armor-percent") {
    const armorType = String(effect.armorType || "all").toLowerCase();
    totals.equipmentArmorPercentByType[armorType] = (totals.equipmentArmorPercentByType[armorType] || 0) + amount;
  }
  if (type === "stun-immunity") totals.stunImmune = true;
  if (type === "action-restriction" && Array.isArray(effect.allowedActions)) {
    totals.actionRestrictions.push(effect.allowedActions.map(slugify));
  }
  if (type === "experience-reward-multiplier") totals.experienceRewardMultiplier *= Math.max(0, amount);
  if (type === "action-points-flat") totals.actionPointsFlat += amount;
  if (type === "stance-bonus-percent") totals.stanceBonusPercent += amount;
  if (type === "quest-money-percent") totals.questMoneyPercent += amount;
  if (type === "quest-reputation-percent") totals.questReputationPercent += amount;
  if (type === "guild-quest-choice-flat") totals.guildQuestChoiceFlat += amount;
  if (type === "enemy-money-percent") totals.enemyMoneyPercent += amount;
  if (type === "sale-price-percent") totals.salePricePercent += amount;
  if (type === "summon-base-stats-percent") totals.summonBaseStatsPercent += amount;
  if (type === "additional-summon-count") totals.additionalSummonCount += Math.max(0, Math.floor(amount));
  if (type === "summon-spells-treated-as-element") totals.summonSpellElement = String(effect.element || "").toLowerCase() || totals.summonSpellElement;
  if (type === "masterwork-drop-chance-flat") totals.masterworkDropChanceFlat += amount;
  if (type === "skill-upgrade") totals.skillUpgrades.push({ ...effect });
  if (type === "passive-upgrade") totals.passiveUpgrades.push({ ...effect });
  if (type === "unarmed-power-percent") totals.unarmedPowerPercent += amount;
  if (type === "unarmed-power-flat") totals.unarmedPowerFlat += amount;
  if (type === "unarmed-hit-flat") totals.unarmedHitFlat += amount;
  if (type === "unarmed-hit-percent") totals.unarmedHitPercent += amount;
  if (type === "unarmed-scaling" && bonusDefinitions.includes(effect.stat)) totals.unarmedScalingModifiers.push({ stat: effect.stat, amount });
  if (type === "incoming-weapon-hit-percent") {
    const weaponType = effect.weaponType ? normalizeWeaponType(effect.weaponType) : "all";
    totals.incomingWeaponHitPercentByType[weaponType] = (totals.incomingWeaponHitPercentByType[weaponType] || 0) + amount;
  }
  if (type === "class-requirement-bypass" && Array.isArray(effect.classes)) {
    totals.classRequirementBypasses.push({ classes: effect.classes.map(slugify), requiredLevel: Math.max(1, toInteger(effect.requiredLevel, 1)) });
  }
  if (type === "defense-permanent-percent" && effect.target in totals.defensePermanentPercent) {
    totals.defensePermanentPercent[effect.target] += amount;
  }
  if (type === "defense-flat" && effect.target in totals.defenseFlat) {
    totals.defenseFlat[effect.target] += amount;
  }
  if (type === "defense-cap" && effect.target in totals.defenseCap) {
    totals.defenseCap[effect.target] = Math.min(totals.defenseCap[effect.target], Math.max(0, amount));
  }
  if (type === "defense-percent" && effect.target in totals.defensePermanentPercent && enemyCount >= 2) {
    totals.defensePermanentPercent[effect.target] += amount;
  }
  if (type === "conditional-evasion-debuff-reduction" && effect.condition === "heavy-armor") {
    totals.heavyArmorEvasionDebuffReduction += amount;
  }
  // Shrinks the Evasion penalty of worn Heavy Armor by a share (forged or old-style).
  if (type === "heavy-armor-evasion-penalty-reduction") totals.heavyArmorEvasionPenaltyReduction += amount;
  if (type === "conditional-defense-flat" && effect.target in totals.defenseFlat) {
    if (classEffectConditionMet(effect)) totals.defenseFlat[effect.target] += amount;
  }
  if (type === "conditional-defense-percent" && effect.target in totals.defensePermanentPercent) {
    if (classEffectConditionMet(effect)) totals.defensePermanentPercent[effect.target] += amount;
  }
  if (["weapon-hit-flat", "weapon-hit-percent", "weapon-power-flat", "weapon-power-percent", "weapon-crit-flat"].includes(type)) {
    const field = type === "weapon-hit-flat" ? "weaponHitFlat" : type === "weapon-hit-percent" ? "weaponHitPercent" : type === "weapon-power-flat" ? "weaponPowerFlat" : type === "weapon-power-percent" ? "weaponPowerPercent" : "weaponCritFlat";
    if (effect.itemId) {
      const bonuses = totals[field + "ByItem"];
      bonuses[effect.itemId] = (bonuses[effect.itemId] || 0) + amount;
      return;
    }
    const weaponType = effect.weaponType ? normalizeWeaponType(effect.weaponType) : "all";
    if (weaponType === "all") {
      totals[field] += amount;
    } else {
      const bonuses = totals[field + "ByType"];
      bonuses[weaponType] = (bonuses[weaponType] || 0) + amount;
    }
  }
  if (type === "weapon-power-multiplier") totals.weaponPowerMultiplier *= Math.max(0, amount);
  if (type === "weapon-scaling" && bonusDefinitions.includes(effect.stat)) {
    totals.weaponScalingModifiers.push({
      weaponType: effect.weaponType ? normalizeWeaponType(effect.weaponType) : "all",
      stat: effect.stat,
      amount,
    });
  }
  if (type === "weapon-crit-disabled") totals.weaponCritDisabled = true;
  if (type === "weapon-crit-always") totals.weaponCritAlways = true;
  if (type === "weapon-hit-always") totals.weaponHitAlways = true;
  if (type === "conditional-weapon-hit-percent-per-enemy") {
    const minimum = toNumber(effect.minimum, 0);
    const reduction = Math.max(0, toNumber(effect.reductionPerEnemy, 0)) * Math.max(0, enemyCount);
    const activeAmount = Math.max(minimum, amount - reduction);
    const weaponType = effect.weaponType ? normalizeWeaponType(effect.weaponType) : "all";
    if (weaponType === "all") {
      totals.weaponHitPercent += activeAmount;
    } else {
      totals.weaponHitPercentByType[weaponType] = (totals.weaponHitPercentByType[weaponType] || 0) + activeAmount;
    }
  }
  if (type === "spell-cost-percent") {
    totals.spellCostPercentModifiers.push({
      amount,
      maxTier: effect.maxTier == null ? null : Math.max(1, toInteger(effect.maxTier, 1)),
      element: effect.element ? String(effect.element).toLowerCase() : null,
      elements: Array.isArray(effect.elements) ? effect.elements.map(element => String(element).toLowerCase()) : [],
      summonOnly: effect.summonOnly === true,
    });
  }
  if (type === "daylight-spell-cost-percent") addClassEffect({ ...effect, type: "spell-cost-percent", amount: isDaylight() ? amount : toNumber(effect.nightAmount, 0) }, totals);
  if (type === "daylight-spell-power-percent") addClassEffect({ ...effect, type: "spell-power-percent", amount: isDaylight() ? amount : toNumber(effect.nightAmount, 0) }, totals);
  if (type === "daylight-stat-permanent-percent" && isDaylight()) addClassEffect({ ...effect, type: "stat-permanent-percent" }, totals);
  if (type === "spell-action-points-set") {
    totals.spellActionPointSetModifiers.push({
      amount: Math.max(1, toInteger(effect.amount, 1)),
      element: effect.element ? String(effect.element).toLowerCase() : null,
      elements: Array.isArray(effect.elements) ? effect.elements.map(element => String(element).toLowerCase()) : [],
      summonOnly: effect.summonOnly === true,
      exact: effect.exact === true,
    });
  }
  if (type === "spell-action-points-flat") {
    totals.spellActionPointFlatModifiers.push({
      amount: toInteger(effect.amount, 0),
      element: effect.element ? String(effect.element).toLowerCase() : null,
      elements: Array.isArray(effect.elements) ? effect.elements.map(element => String(element).toLowerCase()) : [],
      summonOnly: effect.summonOnly === true,
    });
  }
  if (type === "spell-learning-cost-percent") {
    totals.spellLearningCostPercentModifiers.push({
      amount,
      maxTier: effect.maxTier == null ? null : Math.max(1, toInteger(effect.maxTier, 1)),
      element: effect.element ? String(effect.element).toLowerCase() : null,
    });
  }
  if (type === "spell-cost-multiplier") totals.spellCostMultiplier *= Math.max(0, amount);
  if (type === "spell-power-flat") totals.spellPowerFlat += amount;
  if (type === "spell-power-percent") {
    if (effect.element || Array.isArray(effect.elements)) totals.spellPowerPercentModifiers.push({ amount, element: effect.element ? String(effect.element).toLowerCase() : null, elements: Array.isArray(effect.elements) ? effect.elements.map(element => String(element).toLowerCase()) : [] });
    else totals.spellPowerPercent += amount;
  }
  if (type === "spell-damage-percent") {
    totals.spellDamagePercentModifiers.push({ amount, element: effect.element ? String(effect.element).toLowerCase() : null, elements: Array.isArray(effect.elements) ? effect.elements.map(element => String(element).toLowerCase()) : [] });
  }
  if (type === "spell-add-elements") {
    totals.spellAdditionalElementModifiers.push({
      sourceElements: Array.isArray(effect.sourceElements) ? effect.sourceElements.map(element => String(element).toLowerCase()) : [],
      elements: Array.isArray(effect.elements) ? effect.elements.map(element => String(element).toLowerCase()) : [],
    });
  }
  if (type === "incoming-spell-damage-percent") {
    if (effect.element) {
      const element = String(effect.element).toLowerCase();
      totals.incomingSpellDamagePercentByElement[element] = (totals.incomingSpellDamagePercentByElement[element] || 0) + amount;
    } else totals.incomingSpellDamagePercent += amount;
  }
}

function classEffectConditionMet(effect) {
  if (effect.condition === "river-zone") return WorldRules.zoneAt(worldState.position, worldState.regionId).startsWith("River");
  if (effect.condition === "forest-zone") return WorldRules.zoneAt(worldState.position, worldState.regionId).includes("Forest");
  if (effect.condition === "guild-travel-quest") return ["travel", "escort", "deliver"].includes(worldState.guild.quest?.type) && !worldState.guild.quest.complete;
  if (effect.condition === "heavy-armor") return equippedDefenseItems().some((item) => item.type === "heavy");
  if (effect.condition === "no-heavy-armor") return !equippedDefenseItems().some((item) => item.type === "heavy");
  if (effect.condition === "light-or-clothing") return equippedDefenseItems().some((item) => item.type === "light" || item.type === "clothing");
  if (effect.condition === "clothing") return equippedDefenseItems().some((item) => item.type === "clothing");
  if (effect.condition === "opponent-race") return (effect.raceIds || []).some((raceId) => window.Combat?.fightingRace?.(slugify(raceId)));
  if (effect.condition === "no-party-allies") return window.Combat?.hasLivingPartyAlly?.() === false;
  if (effect.condition === "single-opponent") return window.Combat?.livingOpponentCount?.() === 1;
  if (effect.condition === "hp-below-percent") {
    const threshold = Math.max(0, toNumber(effect.threshold, 0));
    const combatResult = window.Combat?.healthBelowPercent?.(threshold);
    if (combatResult != null) return combatResult;
    const maximum = Math.max(0, toNumber(hpMax.textContent, 0));
    return maximum > 0 && currentHp < maximum * threshold / 100;
  }
  return false;
}

function isDaylight() {
  const hour = (worldState.minutes % 1440 + 1440) % 1440 / 60;
  return hour >= 6 && hour < 18;
}

function fencerLoadoutActive() {
  const sword = getItem(equipment.primaryHand);
  return sword?.category === "weapon" && sword.handed === 1 && /sword|rapier/i.test(sword.name) && !equipment.offHand;
}

function calculateClassEffects() {
  return window.Combat?.cacheCalculation?.("class-effects", calculateClassEffectsUncached) ?? calculateClassEffectsUncached();
}

function calculateClassEffectsUncached() {
  const totals = emptyClassEffects();
  classCatalog.forEach((classDefinition) => {
    const currentLevel = toInteger(classLevels[classDefinition.id], 0);
    classDefinition.levels
      .filter((levelDefinition) => levelDefinition.level <= currentLevel)
      .forEach((levelDefinition) => {
        levelDefinition.effects.forEach((effect) => {
          const type = effect?.type || effect?.kind;
          if (type === "passive") {
            totals.passives.push({ ...effect, classId: classDefinition.id, className: classDefinition.name, level: levelDefinition.level });
            if (effect.effect && typeof effect.effect === "object" && effect.effect.type !== "party-stat-percent") addClassEffect(effect.effect, totals);
            if (Array.isArray(effect.effects)) effect.effects.filter(passiveEffect => passiveEffect.type !== "party-stat-percent").forEach((passiveEffect) => addClassEffect(passiveEffect, totals));
            return;
          }
          if (type === "skill") {
            const skill = {
              ...effect,
              classId: classDefinition.id,
              className: classDefinition.name,
              level: levelDefinition.level,
              effects: Array.isArray(effect.effects) ? effect.effects.map((entry) => ({ ...entry })) : [],
              onHitEffects: Array.isArray(effect.onHitEffects) ? effect.onHitEffects.map((entry) => ({ ...entry })) : [],
              onActivateEffects: Array.isArray(effect.onActivateEffects) ? effect.onActivateEffects.map((entry) => ({ ...entry })) : [],
              afterAttackEffects: Array.isArray(effect.afterAttackEffects) ? effect.afterAttackEffects.map((entry) => ({ ...entry })) : [],
              onExpireEffects: Array.isArray(effect.onExpireEffects) ? effect.onExpireEffects.map((entry) => ({ ...entry })) : [],
            };
            if (!totals.skills.some((knownSkill) => classSkillKey(knownSkill) === classSkillKey(skill))) totals.skills.push(skill);
            return;
          }
          addClassEffect(effect, totals);
        });
      });
  });
  for (const upgrade of totals.skillUpgrades) {
    const skill = totals.skills.find((candidate) => classSkillKey(candidate) === upgrade.skillId || upgrade.skillId === "paladin:divine-smite" && classSkillKey(candidate) === "oathsworn:divine-smite");
    if (!skill) continue;
    if (Number.isFinite(upgrade.usesPerCombat)) skill.usesPerCombat = Math.max(1, toInteger(upgrade.usesPerCombat, 1));
    if (upgrade.removeEffectId) skill.effects = skill.effects.filter((effect) => effect.id !== upgrade.removeEffectId);
    if (Number.isFinite(upgrade.onHitAmountFlat) && upgrade.onHitType) for (const effect of skill.onHitEffects.filter(entry => entry.type === upgrade.onHitType)) effect.amount = toNumber(effect.amount, 0) + toNumber(upgrade.onHitAmountFlat, 0);
    if (Number.isFinite(upgrade.markDamagePercentFlat) || Number.isFinite(upgrade.markHitFlat) || Number.isFinite(upgrade.markHitPercent)) {
      for (const effect of skill.effects.filter((entry) => entry.type === "mark-target")) {
        effect.damagePercent = toNumber(effect.damagePercent, 0) + toNumber(upgrade.markDamagePercentFlat, 0);
        if (Number.isFinite(upgrade.markHitFlat)) effect.hitFlat = toNumber(effect.hitFlat, 0) + toNumber(upgrade.markHitFlat, 0);
        if (Number.isFinite(upgrade.markHitPercent)) effect.hitPercent = toNumber(effect.hitPercent, 0) + toNumber(upgrade.markHitPercent, 0);
      }
    }
    if (Number.isFinite(upgrade.bonusDamageMaxManaPercentFlat) || Array.isArray(upgrade.addElements)) {
      for (const effect of skill.onHitEffects.filter((entry) => entry.type === "bonus-damage-max-mana-percent")) {
        effect.amount = toNumber(effect.amount, 0) + toNumber(upgrade.bonusDamageMaxManaPercentFlat, 0);
        const elements = [effect.element, ...(effect.elements || []), ...(upgrade.addElements || [])].filter(Boolean).map((element) => String(element).toLowerCase());
        effect.elements = [...new Set(elements)];
      }
    }
    if (Number.isFinite(upgrade.tacticalDamagePercentFlat) || Number.isFinite(upgrade.tacticalTurnAdvanceFlat)) {
      for (const effect of skill.effects.filter((entry) => entry.type === "tactical-mark")) {
        effect.damagePercent = toNumber(effect.damagePercent, 0) + toNumber(upgrade.tacticalDamagePercentFlat, 0);
        effect.turnAdvance = toNumber(effect.turnAdvance, 0) + toNumber(upgrade.tacticalTurnAdvanceFlat, 0);
      }
    }
  }
  const hunterMark = totals.skills.find((skill) => classSkillKey(skill) === "hunter:hunters-mark");
  const markEffect = hunterMark?.effects.find((effect) => effect.type === "mark-target");
  if (hunterMark && markEffect) {
    hunterMark.description = "Mark the targeted enemy. You deal " + formatValue(markEffect.damagePercent) + "% more weapon and spell damage to it, and weapon attacks against it gain " + formatValue(markEffect.hitPercent) + "% Accuracy. If your weapon attack or spell defeats it, this skill refreshes; otherwise it can only be used once per day.";
  }
  const divineSmite = totals.skills.find((skill) => ["paladin:divine-smite", "oathsworn:divine-smite"].includes(classSkillKey(skill)));
  const smiteEffect = divineSmite?.onHitEffects.find((effect) => effect.type === "bonus-damage-max-mana-percent");
  if (divineSmite && smiteEffect) {
    const elements = smiteEffect.elements?.length ? smiteEffect.elements : [smiteEffect.element || "holy"];
    divineSmite.description = "Spend 10% maximum Mana. Your next attack deals additional " + elements.map(spellElementLabel).join(" and ") + " damage equal to " + formatValue(smiteEffect.amount) + "% maximum Mana.";
  }
  for (const skill of totals.skills) {
    if (activeSkills[classSkillKey(skill)]?.active) skill.effects.forEach((skillEffect) => addClassEffect(skillEffect, totals));
  }
  const trainedAtLevel = (minimumLevel) => Object.values(classLevels).filter((classLevel) => toInteger(classLevel, 0) >= minimumLevel).length;
  for (const passive of totals.passives) {
    for (const effect of [...(passive.effects || []), ...(passive.effect ? [passive.effect] : [])]) {
      if (effect.type !== "all-stats-permanent-percent-per-class-level") continue;
      const bonus = toNumber(effect.amount, 0) * trainedAtLevel(Math.max(1, toInteger(effect.minimumLevel, 1)));
      bonusDefinitions.forEach((key) => { totals.statPermanentPercent[key] += bonus; });
    }
  }
  return totals;
}

function emptyRaceEffects() {
  return {
    maxHpFlat: 0,
    maxManaFlat: 0,
    spellCostPercent: 0,
    spellCostPercentModifiers: [],
    statPermanentPercent: { ...defaultBonuses },
    defensePermanentPercent: { ...defaultDefenseBonuses },
    defenseFlat: { armor: 0, evasion: 0 },
    defenseCap: { armor: Infinity, evasion: Infinity },
    equipmentRestrictions: [],
    passives: [],
    trapLimitFlat: 0,
    healingDisabled: false,
    spellcastingDisabled: false,
    debuffImmune: false,
  };
}

function getSelectedRace() {
  return raceCatalog.find((race) => race.id === selectedRaceId) || null;
}

function raceStatModifier(statKey) {
  const race = getSelectedRace();
  let amount = toNumber(race?.statModifiers?.[statKey], 0);
  for (const effect of race?.passive?.effects || []) {
    if ((effect.type || effect.kind) === "all-stats-flat-per-level-up") amount += Math.max(0, level - 1) * toNumber(effect.amount, 0);
  }
  return amount;
}

function displayedBaseStat(statKey) {
  return toNumber(stats[statKey], 0) + raceStatModifier(statKey);
}

function calculateRaceEffects() {
  return window.Combat?.cacheCalculation?.("race-effects", calculateRaceEffectsUncached) ?? calculateRaceEffectsUncached();
}

function calculateRaceEffectsUncached() {
  const totals = emptyRaceEffects();
  const race = getSelectedRace();
  if (!race) return totals;
  const passive = race.passive;
  if (passive) {
    totals.passives.push({
      ...passive,
      raceId: race.id,
      raceName: race.name,
      sourceType: "race",
    });
    passive.effects.forEach((effect) => {
      const type = effect?.type || effect?.kind;
      const amount = toNumber(effect?.amount, 0);
      if (type === "max-hp-per-levels") {
        const interval = Math.max(1, toInteger(effect.interval, 1));
        totals.maxHpFlat += Math.floor(level / interval) * amount;
      }
      if (type === "max-mana-per-levels") {
        const interval = Math.max(1, toInteger(effect.interval, 1));
        totals.maxManaFlat += Math.floor(level / interval) * amount;
      }
      if (type === "spell-cost-percent") {
        if (effect.namePrefix || effect.element) totals.spellCostPercentModifiers.push({
          amount,
          namePrefix: effect.namePrefix ? String(effect.namePrefix).toLowerCase() : null,
          element: effect.element ? String(effect.element).toLowerCase() : null,
        });
        else totals.spellCostPercent += amount;
      }
      if (type === "trap-limit-flat") totals.trapLimitFlat += amount;
      if (type === "healing-disabled") totals.healingDisabled = true;
      if (type === "spellcasting-disabled") totals.spellcastingDisabled = true;
      if (type === "debuff-immunity") totals.debuffImmune = true;
      if (type === "stat-permanent-percent" && effect.target in totals.statPermanentPercent) {
        totals.statPermanentPercent[effect.target] += amount;
      }
      if (type === "defense-permanent-percent" && effect.target in totals.defensePermanentPercent) {
        totals.defensePermanentPercent[effect.target] += amount;
      }
      if (type === "defense-flat-per-enemy" && effect.target in totals.defenseFlat) {
        totals.defenseFlat[effect.target] += amount * Math.max(0, enemyCount);
      }
      if (type === "defense-cap" && effect.target in totals.defenseCap) {
        totals.defenseCap[effect.target] = Math.min(totals.defenseCap[effect.target], Math.max(0, amount));
      }
      if (type === "equipment-restriction") {
        totals.equipmentRestrictions.push({
          category: String(effect.category || "").toLowerCase(),
          type: String(effect.typeName || effect.target || "").toLowerCase(),
        });
      }
    });
  }
  return totals;
}

function calculateTrapLimit() {
  const addedSlots = calculateArtifactEffects().trapLimitFlat + calculateClassEffects().trapLimitFlat + calculateRaceEffects().trapLimitFlat;
  return Math.max(0, 1 + Math.floor(addedSlots));
}

function effectivePermanentBonus(statKey) {
  const artifactEffects = calculateArtifactEffects();
  const classEffects = calculateClassEffects();
  const raceEffects = calculateRaceEffects();
  return toNumber(permanentBonuses[statKey], 0) + artifactEffects.statPermanentPercent[statKey] + classEffects.statPermanentPercent[statKey] + raceEffects.statPermanentPercent[statKey] + (window.Combat?.permanentStatPercent?.(statKey) || 0);
}

function effectivePermanentDefenseBonus(defenseKey) {
  const classEffects = calculateClassEffects();
  const raceEffects = calculateRaceEffects();
  return toNumber(permanentDefenseBonuses[defenseKey], 0) + classEffects.defensePermanentPercent[defenseKey] + raceEffects.defensePermanentPercent[defenseKey] + (window.Combat?.permanentDefensePercent?.(defenseKey) || 0);
}

function currentStats() {
  // Callers may apply combat auras to their copy of these basic values.
  return { ...(window.Combat?.cacheCalculation?.("current-stats", currentStatsUncached) ?? currentStatsUncached()) };
}

// Peace and Balance (a Masterwork Elemental): nothing pushes its wielder's turn meter back or lowers their
// Speed, whether from enemies or their own skills and gear; increases still count. (Race stat modifiers
// are the base, not a reduction.)
function wieldsTurnAndSpeedLock() {
  return equippedWeaponItems().some(item => (item.effects || []).some(effect => effect.kind === "turn-and-speed-lock"));
}

function currentStatsUncached() {
  const artifactEffects = calculateArtifactEffects();
  const classEffects = calculateClassEffects();
  const raceEffects = calculateRaceEffects();
  const speedLock = wieldsTurnAndSpeedLock();
  const counted = (key, amount) => speedLock && key === "speed" ? Math.max(0, amount) : amount;
  const baseValues = Object.fromEntries(statDefinitions.map(({ key }) => [key, displayedBaseStat(key) + counted(key, artifactEffects.statFlat[key])]));
  const percentageValues = bonusValues(permanentBonuses);
  statDefinitions.forEach(({ key }) => {
    percentageValues[key] = [percentageValues[key], artifactEffects.statPercent[key], artifactEffects.statPermanentPercent[key], classEffects.statPermanentPercent[key], raceEffects.statPermanentPercent[key], (window.Combat?.permanentStatPercent?.(key) || 0)]
      .reduce((sum, amount) => sum + counted(key, amount), 0);
  });
  const values = calculateCurrentStats(baseValues, percentageValues);
  if (window.Combat) statDefinitions.forEach(({ key }) => { values[key] = window.Combat.statValue(key, values[key]); });
  return values;
}

function calculateMaxHpWithArtifacts(values) {
  const battleMaximum = window.Combat?.resourceMaximum?.("hp");
  if (battleMaximum != null) return battleMaximum;
  const artifactEffects = calculateArtifactEffects();
  const raceEffects = calculateRaceEffects();
  const classEffects = calculateClassEffects();
  const baseMaxHp = calculateMaxHp(values) + artifactEffects.maxHpFlat + raceEffects.maxHpFlat;
  const armorHpPercent = equippedDefenseItems().reduce((sum, item) => sum + toNumber(item.hpPercent, 0), 0);
  return Math.max(0, Math.floor(baseMaxHp * (1 + ((artifactEffects.maxHpPercent + classEffects.maxHpPercent + armorHpPercent) / 100))));
}

function calculateMaxManaWithEffects(values) {
  const battleMaximum = window.Combat?.resourceMaximum?.("mana");
  if (battleMaximum != null) return battleMaximum;
  const artifactEffects = calculateArtifactEffects();
  const baseMaxMana = calculateMaxMana(values) + calculateRaceEffects().maxManaFlat + artifactEffects.maxManaFlat;
  return Math.max(0, Math.floor(baseMaxMana * (1 + ((calculateClassEffects().maxManaPercent + artifactEffects.maxManaPercent) / 100))));
}

function equippedDefenseItems() {
  return [equipment.armor, equipment.offHand]
    .map((itemId) => getItem(itemId))
    .filter((item) => item && item.category === "armor");
}

function equippedDefenseCap(key) {
  return equippedDefenseItems().flatMap(item => item.effects || [])
    .filter(effect => effect.kind === "defense-cap" && effect.target === key)
    .reduce((cap, effect) => Math.min(cap, effect.amount), Infinity);
}

function equippedArmorSpellCostPercent(spell) {
  return equippedDefenseItems().flatMap((item) => item.effects || []).reduce((total, effect) => {
    if (effect.kind !== "spell-cost-percent") return total;
    if (effect.minTier != null && spell.tier < effect.minTier) return total;
    if (effect.maxTier != null && spell.tier > effect.maxTier) return total;
    if (effect.summonOnly && !isSummonSpell(spell)) return total;
    return total + toNumber(effect.amount, 0);
  }, 0);
}

function equippedWeaponItems() {
  const primary = getItem(equipment.primaryHand);
  const offHand = getItem(equipment.offHand);
  const weapons = [];
  if (primary?.category === "weapon") weapons.push(primary);
  if (offHand?.category === "weapon" && !(primary?.id === offHand.id && primary.handed === 2)) weapons.push(offHand);
  return weapons;
}

// Offensive (Damage) spells, summoning spells and the remaining Support spells.
function spellCategory(spell) {
  return spell?.type === "Damage" ? "damage" : isSummonSpell(spell) ? "summon" : "support";
}

// Whether a weapon's spell effect reaches this spell: its element, its category and the
// Vampiric exception for Blood spells. Elements added by equipment (such as Branch of
// the Forest's Nature) do not count; only the spell's own element does.
function weaponSpellEffectApplies(effect, spell) {
  if (effect.element && effect.element !== spell.element) return false;
  if (effect.excludeElement && effect.excludeElement === spell.element) return false;
  if (effect.categories && !effect.categories.includes(spellCategory(spell))) return false;
  return true;
}

function equippedWeaponSpellCostPercent(spell) {
  return equippedWeaponItems().flatMap((item) => item.effects || []).reduce((total, effect) => {
    if (effect.kind !== "spell-cost-percent") return total;
    if (effect.minTier != null && spell.tier < effect.minTier) return total;
    if (effect.maxTier != null && spell.tier > effect.maxTier) return total;
    if (effect.summonOnly && !isSummonSpell(spell)) return total;
    if (!weaponSpellEffectApplies(effect, spell)) return total;
    return total + toNumber(effect.amount, 0);
  }, 0);
}

function equippedWeaponSpellPowerPercent(spell) {
  return equippedWeaponItems().flatMap((item) => item.effects || []).reduce((total, effect) => {
    if (effect.kind !== "spell-power-percent") return total;
    if (effect.maxTier != null && spell.tier > effect.maxTier) return total;
    if (!weaponSpellEffectApplies(effect, spell)) return total;
    return total + toNumber(effect.amount, 0);
  }, 0);
}

function calculateEquipmentDefense(values) {
  const armorItems = equippedDefenseItems();
  const artifactEffects = calculateArtifactEffects();
  const classEffects = calculateClassEffects();
  // Forged armor carries its own armor percentage (materials, makers, Heft runes) and
  // Empower runes raise the wearer's whole equipment armor.
  const baseEquipmentArmor = armorItems.reduce((total, item) => {
    const itemArmor = (item.armorValue + Math.floor(calculateItemPower(item, values))) * (1 + toNumber(item.armorPercent, 0) / 100);
    const classPercent = toNumber(classEffects.equipmentArmorPercentByType.all, 0) + toNumber(classEffects.equipmentArmorPercentByType[item.type], 0);
    return total + Math.floor(itemArmor * (1 + classPercent / 100));
  }, 0);
  const totalArmorPercent = armorItems.reduce((sum, item) => sum + toNumber(item.armorTotalPercent, 0), 0);
  const equipmentArmor = Math.floor((baseEquipmentArmor + artifactEffects.armorFlat) * (1 + ((artifactEffects.armorPercent + totalArmorPercent) / 100)));
  const heavyPenaltyShare = 1 - Math.min(100, Math.max(0, classEffects.heavyArmorEvasionPenaltyReduction)) / 100;
  const baseEvasionDebuff = armorItems.reduce((total, item) => total + item.evasionDebuff * (item.type === "heavy" ? heavyPenaltyShare : 1), 0);
  const hasHeavyArmor = armorItems.some((item) => item.type === "heavy");
  const evasionDebuff = Math.max(0, Math.round(baseEvasionDebuff) - (hasHeavyArmor ? classEffects.heavyArmorEvasionDebuffReduction : 0));
  const equipmentEvasion = armorItems.flatMap(item => item.effects || []).filter(effect => effect.kind === "evasion-flat").reduce((sum, effect) => sum + effect.amount, 0)
    + armorItems.reduce((sum, item) => sum + toNumber(item.evasionFlat, 0), 0);
  // Forged armor changes Evasion by its type: clothing raises it, heavy armor lowers it.
  const armorEvasionPercent = armorItems.reduce((sum, item) => {
    const percent = toNumber(item.evasionPercent, 0);
    return sum + (item.type === "heavy" && percent < 0 ? percent * heavyPenaltyShare : percent);
  }, 0);
  // Winged Hussar's +15% Evasion (task 7): a held weapon's own Evasion percentage.
  const weaponEvasionPercent = equippedWeaponItems().flatMap(item => item.effects || []).filter(effect => effect.kind === "evasion-percent").reduce((sum, effect) => sum + toNumber(effect.amount, 0), 0);
  return { armor: equipmentArmor, evasionDebuff, evasionFlat: artifactEffects.evasionFlat + equipmentEvasion, evasionPercent: artifactEffects.evasionPercent + armorEvasionPercent + weaponEvasionPercent };
}

function defaultCharacter() {
  return {
    worldState: WorldRules.normalize(),
    limitless: LimitlessRules.normalize(null),
    shopState: { version: 4, refreshWindow: -1, locations: {} },
    raceId: null,
    appearance: null,
    level: 1,
    exp: 0,
    availablePoints: 0,
    perkPoints: 1,
    startingPerkPointGranted: true,
    trapsUsed: 0,
    unarmedAttacks: 0,
    summonSpellsUsed: 0,
    summonSpellsUsedByElement: {},
    bloodSupplies: 0,
    itemsSold: 0,
    soloSurvivorVictories: 0,
    multiKillCombats: 0,
    classKills: {},
    raceKills: {},
    classProgress: { oneHandedAttacks: 0, summonCasts: {}, lootEnemiesCollected: 0, boundFamiliar: null, claimedGuaranteedDrops: [], firstKillDrops: [], fulfilledClassOtherRequirements: [], statChoiceBonuses: {} },
    defensiveActionsUsed: 0,
    flawlessVictories: 0,
    enemyCount: 0,
    editingEnabled: true,
    darkMode: false,
    currentHp: calculateMaxHp(defaultStats),
    currentMana: calculateMaxMana(defaultStats),
    manualAllocations: { ...defaultAllocations },
    currencies: { ...defaultCurrencies },
    permanentBonuses: { ...defaultBonuses },
    permanentDefenseBonuses: { ...defaultDefenseBonuses },
    inventory: [],
    favoriteItems: [],
    equipment: { ...defaultEquipment },
    knownSpells: [],
    classLevels: {},
    activeSkills: {},
    companions: [],
    recruitOffers: [],
    pendingClassSpellChoice: null,
    completedClassChoices: [],
    stats: { ...defaultStats },
  };
}

// A chosen battle sprite: a gender and one of that gender's four looks.
function normalizeAppearanceRecord(value) {
  return value && ["male", "female"].includes(value.gender) && Number.isInteger(value.variant) && value.variant >= 0 && value.variant < 4 ? { gender: value.gender, variant: value.variant } : null;
}
// Companions keep the same look across saves without storing a random roll.
function stableLookVariant(text) {
  let value = 0;
  for (const char of String(text)) value = (value * 31 + char.charCodeAt(0)) >>> 0;
  return value % 4;
}

// context: { companion, limitlessActive } for a companion (it follows the player's Limitless, L4); { levelCap } for a
// saved battle's actors (enemies scaled by Limitless may be above Level 100).
function normalizeCharacter(saved, context = {}) {
  if (!saved || typeof saved !== "object") return defaultCharacter();
  const savedPerkPoints = Math.max(0, toInteger(saved.perkPoints, 0));
  const perkPointsWithStartingPoint = savedPerkPoints + (saved.startingPerkPointGranted === true ? 0 : 1);
  const savedStats = Object.fromEntries(statDefinitions.map(({ key }) => [key, toInteger(saved.stats?.[key] ?? saved[key], 10)]));
  const savedAllocations = Object.fromEntries(statDefinitions.map(({ key }) => [key, Math.max(0, toInteger(saved.manualAllocations?.[key], 0))]));
  const savedCurrencies = Object.fromEntries(currencyDefinitions.map((key) => [key, Math.max(0, toInteger(saved.currencies?.[key] ?? saved[key], defaultCurrencies[key]))]));
  const rawShopState = saved.shopState && typeof saved.shopState === "object" ? saved.shopState : {};
  const savedShopLocations = rawShopState.locations && typeof rawShopState.locations === "object" ? rawShopState.locations : {};
  const normalizeStockItems = (entries) => Array.isArray(entries) ? entries.filter(entry => entry && typeof entry.id === "string").map(entry => ({ id: entry.id, quantity: Math.max(0, toInteger(entry.quantity, 0)) })) : [];
  const savedShopState = {
    version: toInteger(rawShopState.version, 0),
    refreshWindow: toInteger(rawShopState.refreshWindow, -1),
    locations: Object.fromEntries(Object.entries(savedShopLocations).map(([placeId, stock]) => [placeId, {
      blacksmith: normalizeStockItems(stock?.blacksmith),
      general: normalizeStockItems(stock?.general || stock?.items),
      tower: normalizeStockItems(stock?.tower),
      relics: normalizeStockItems(stock?.relics),
      spells: Array.isArray(stock?.spells) ? stock.spells.filter(id => typeof id === "string") : [],
    }])),
  };
  const savedLegacyModifiers = saved.modifiers && typeof saved.modifiers === "object" ? saved.modifiers : {};
  const savedPermanentBonuses = Object.fromEntries(bonusDefinitions.map((key) => [
    key,
    toNumber(saved.permanentBonuses?.[key] ?? savedLegacyModifiers[key], 0),
  ]));
  const savedStatChoiceBonuses = Object.fromEntries(Object.entries(saved.classProgress?.statChoiceBonuses || {}).filter(([key, value]) => key.length < 180 && bonusDefinitions.includes(value?.stat) && Number.isFinite(value.amount)).map(([key, value]) => [key, { stat: value.stat, amount: value.amount }]));
  // Barbarian level 7 now grants both bonuses through the class definition.
  // Remove only its tracked legacy choice reward, preserving unrelated bonuses.
  const legacyBarbarianChoice = savedStatChoiceBonuses["barbarian:7:0"];
  if (legacyBarbarianChoice && ["strength", "vitality"].includes(legacyBarbarianChoice.stat)) {
    savedPermanentBonuses[legacyBarbarianChoice.stat] -= legacyBarbarianChoice.amount;
    delete savedStatChoiceBonuses["barbarian:7:0"];
  }
  const savedPermanentDefenseBonuses = Object.fromEntries(defenseBonusDefinitions.map((key) => [
    key,
    toNumber(saved.permanentDefenseBonuses?.[key] ?? saved.defenseModifiers?.[key] ?? savedLegacyModifiers[key], 0),
  ]));
  const savedInventory = Array.isArray(saved.inventory) ? saved.inventory
    .filter((itemId) => typeof itemId === "string" && !itemId.startsWith("consumable-spell-tome-"))
    .map((itemId) => itemId === "consumable-potion-of-supreme-health" ? "consumable-potion-of-ultimate-health" : itemId === "consumable-potion-of-supreme-mana" ? "consumable-potion-of-ultimate-mana" : itemId) : [];
  const savedFavoriteItems = Array.isArray(saved.favoriteItems) ? [...new Set(saved.favoriteItems
    .filter(itemId => typeof itemId === "string" && itemId.length > 0 && !itemId.startsWith("consumable-spell-tome-"))
    .map(itemId => itemId === "consumable-potion-of-supreme-health" ? "consumable-potion-of-ultimate-health" : itemId === "consumable-potion-of-supreme-mana" ? "consumable-potion-of-ultimate-mana" : itemId))] : [];
  const savedEquipment = Object.fromEntries(equipmentSlotDefinitions.map(({ key }) => [
    key,
    typeof saved.equipment?.[key] === "string" ? saved.equipment[key] : null,
  ]));
  // The Artifact Rework (port task 4, A8, A9): the replaced artifacts leave packs, slots and favorites, silently.
  const retiredArtifact = itemId => Boolean(window.WorldRules?.RETIRED_ARTIFACT_IDS?.has(itemId));
  for (let index = savedInventory.length - 1; index >= 0; index--) if (retiredArtifact(savedInventory[index])) savedInventory.splice(index, 1);
  for (let index = savedFavoriteItems.length - 1; index >= 0; index--) if (retiredArtifact(savedFavoriteItems[index])) savedFavoriteItems.splice(index, 1);
  for (const key of Object.keys(savedEquipment)) if (retiredArtifact(savedEquipment[key])) savedEquipment[key] = null;
  // The old catalogue weapons and armor were retired; old saves get forged pieces of the same kind.
  window.ForgeRules?.migrateLegacyItems({ inventory: savedInventory, equipment: savedEquipment, favorites: savedFavoriteItems }, Math.max(1, toInteger(saved.level, 1)), [saved.raceId, saved.name, saved.exp, saved.level].join("|"));
  const savedKnownSpells = Array.isArray(saved.knownSpells) ? saved.knownSpells.filter((spellId) => typeof spellId === "string") : [];
  const savedClassLevels = Object.fromEntries(Object.entries(saved.classLevels || {}).map(([classId, classLevel]) => [classId === "luxmancer" ? "luxomancer" : classId, Math.max(0, toInteger(classLevel, 0))]));
  const savedActiveSkills = Object.fromEntries(Object.entries(saved.activeSkills || {}).map(([skillId, state]) => [skillId.startsWith("luxmancer:") ? "luxomancer:" + skillId.slice("luxmancer:".length) : skillId, {
    active: state === true || state?.active === true,
    lastUsedDay: typeof state?.lastUsedDay === "string" ? (!saved.worldState && state.lastUsedDay ? "world-day-1" : state.lastUsedDay) : "",
    usedThisCombat: state?.usedThisCombat === true,
    usesThisCombat: Math.max(0, toInteger(state?.usesThisCombat, state?.usedThisCombat ? 1 : 0)),
    remainingUses: Number.isFinite(state?.remainingUses) ? state.remainingUses : undefined,
    charges: Math.max(0, toInteger(state?.charges, 0)),
  }]));
  const rawSpellChoice = saved.pendingClassSpellChoice;
  const savedPendingClassSpellChoice = rawSpellChoice && typeof rawSpellChoice === "object" && Array.isArray(rawSpellChoice.candidateIds)
    ? {
        classId: String(rawSpellChoice.classId || ""),
        effectId: String(rawSpellChoice.effectId || ""),
        candidateIds: rawSpellChoice.candidateIds.filter(id => typeof id === "string"),
        selectedIds: Array.isArray(rawSpellChoice.selectedIds) ? rawSpellChoice.selectedIds.filter(id => typeof id === "string") : [],
        requiredPicks: Math.max(1, toInteger(rawSpellChoice.requiredPicks, 2)),
        optionCount: Math.max(1, toInteger(rawSpellChoice.optionCount, 5)),
      }
    : null;
  const savedCompletedClassChoices = Array.isArray(saved.completedClassChoices) ? [...new Set(saved.completedClassChoices.filter(key => typeof key === "string"))] : [];
  // Limitless (port task 7): its record; the level cap and EXP follow whether it is on (L2, L4, L18).
  const savedLimitless = LimitlessRules.normalize(saved?.limitless, context.companion === true);
  const limitlessOn = context.companion ? context.limitlessActive === true : savedLimitless.active === true;
  const levelCap = Math.max(LimitlessRules.cap(limitlessOn), Math.min(LimitlessRules.LIMITLESS_CAP, toInteger(context.levelCap, 0)));
  const normalizedLevel = Math.min(levelCap, Math.max(1, toInteger(saved?.level, 1)));
  const normalizeCompanion = entry => {
    if (!entry || typeof entry.id !== "string" || typeof entry.name !== "string" || !entry.character || typeof entry.character !== "object") return null;
    const gender = entry.gender === "female" ? "female" : "male";
    const companionCharacter = normalizeCharacter({ ...entry.character, companions: [], recruitOffers: [], combatState: null }, { companion: true, limitlessActive: limitlessOn });
    // A companion's look always matches the gender of their name.
    const savedLook = normalizeAppearanceRecord(entry.character.appearance);
    companionCharacter.appearance = { gender, variant: savedLook?.gender === gender ? savedLook.variant : stableLookVariant(entry.id) };
    return { id: entry.id, name: entry.name.slice(0, 80), gender, character: companionCharacter };
  };
  const savedCompanions = Array.isArray(saved.companions) ? saved.companions.map(normalizeCompanion).filter(Boolean).slice(0, 3) : [];
  // Recruit offers remember the guild that made them (port task 2: five at a castle city, two at a town guild).
  const savedRecruitOffers = Array.isArray(saved.recruitOffers) ? saved.recruitOffers.map(entry => {
    const offer = normalizeCompanion(entry);
    if (offer && typeof entry.guildId === "string" && entry.guildId.trim()) offer.guildId = entry.guildId.trim();
    return offer;
  }).filter(Boolean).slice(0, 5) : [];
  const savedCurrentStats = calculateCurrentStats(savedStats, bonusValues(savedPermanentBonuses));
  const savedMaxHp = calculateMaxHp(savedCurrentStats);
  const savedMaxMana = calculateMaxMana(savedCurrentStats);
  return {
    raceId: typeof saved.raceId === "string" ? slugify(saved.raceId) : null,
    level: normalizedLevel,
    // At the cap (Level 100 without Limitless on) no EXP is gained, and none an old save banked there is kept (L18).
    exp: normalizedLevel >= LimitlessRules.cap(limitlessOn) && !context.levelCap ? 0 : Math.max(0, toNumber(saved.exp, 0)),
    limitless: savedLimitless,
    availablePoints: Math.max(0, toInteger(saved.availablePoints, 0)),
    perkPoints: perkPointsWithStartingPoint,
    startingPerkPointGranted: true,
    trapsUsed: Math.max(0, toInteger(saved.trapsUsed, 0)),
    unarmedAttacks: Math.max(0, toInteger(saved.unarmedAttacks, 0)),
    summonSpellsUsed: Math.max(0, toInteger(saved.summonSpellsUsed, 0)),
    summonSpellsUsedByElement: Object.fromEntries(Object.entries(saved.summonSpellsUsedByElement || {}).map(([element, count]) => [String(element).toLowerCase(), Math.max(0, toInteger(count, 0))])),
    bloodSupplies: Math.max(0, toInteger(saved.bloodSupplies, 0)),
    itemsSold: Math.max(0, toInteger(saved.itemsSold, 0)),
    soloSurvivorVictories: Math.max(0, toInteger(saved.soloSurvivorVictories, 0)),
    multiKillCombats: Math.max(0, toInteger(saved.multiKillCombats, 0)),
    classKills: Object.fromEntries(Object.entries(saved.classKills || {}).map(([classId, count]) => [slugify(classId), Math.max(0, toInteger(count, 0))])),
    raceKills: Object.fromEntries(Object.entries(saved.raceKills || {}).map(([raceId, count]) => [slugify(raceId), Math.max(0, toInteger(count, 0))])),
    classProgress: {
      oneHandedAttacks: Math.max(0, toInteger(saved.classProgress?.oneHandedAttacks, 0)),
      summonCasts: Object.fromEntries(Object.entries(saved.classProgress?.summonCasts || {}).map(([spellId, count]) => [slugify(spellId), Math.max(0, toInteger(count, 0))])),
      lootEnemiesCollected: Math.max(0, toInteger(saved.classProgress?.lootEnemiesCollected, 0)),
      claimedGuaranteedDrops: [...new Set((Array.isArray(saved.classProgress?.claimedGuaranteedDrops) ? saved.classProgress.claimedGuaranteedDrops : []).filter(id => typeof id === "string" && (id.startsWith("weapon-") || id.startsWith("masterwork-")) && id.length < 120))],
      // A retired item's first-kill record carries over to the masterwork that replaced it.
      firstKillDrops: [...new Set((Array.isArray(saved.classProgress?.firstKillDrops) ? saved.classProgress.firstKillDrops : []).filter(id => typeof id === "string" && id.length < 120).map(id => window.ForgeRules?.LEGACY_ITEMS?.[id]?.startsWith("masterwork-") ? window.ForgeRules.LEGACY_ITEMS[id] : id))],
      fulfilledClassOtherRequirements: [...new Set((Array.isArray(saved.classProgress?.fulfilledClassOtherRequirements) ? saved.classProgress.fulfilledClassOtherRequirements : []).filter(id => typeof id === "string" && id.length < 120))],
      statChoiceBonuses: savedStatChoiceBonuses,
      boundFamiliar: saved.classProgress?.boundFamiliar?.spellId ? {
        spellId: slugify(saved.classProgress.boundFamiliar.spellId),
        victories: Math.max(0, toInteger(saved.classProgress.boundFamiliar.victories, 0)),
      } : null,
    },
    defensiveActionsUsed: Math.max(0, toInteger(saved.defensiveActionsUsed, 0)),
    flawlessVictories: Math.max(0, toInteger(saved.flawlessVictories, 0)),
    enemyCount: 0,
    combatState: saved.combatState || null,
    worldState: WorldRules.normalize(saved.worldState),
    shopState: savedShopState,
    editingEnabled: saved.editingEnabled !== false,
    darkMode: saved.darkMode === true,
    currentHp: Math.max(0, toInteger(saved.currentHp, savedMaxHp)),
    currentMana: Math.max(0, toInteger(saved.currentMana, savedMaxMana)),
    manualAllocations: savedAllocations,
    currencies: savedCurrencies,
    permanentBonuses: savedPermanentBonuses,
    permanentDefenseBonuses: savedPermanentDefenseBonuses,
    inventory: savedInventory,
    favoriteItems: savedFavoriteItems,
    equipment: savedEquipment,
    knownSpells: savedKnownSpells,
    classLevels: savedClassLevels,
    activeSkills: savedActiveSkills,
    companions: savedCompanions,
    recruitOffers: savedRecruitOffers,
    pendingClassSpellChoice: savedPendingClassSpellChoice,
    completedClassChoices: savedCompletedClassChoices,
    appearance: normalizeAppearanceRecord(saved.appearance),
    stats: savedStats,
  };
}

function readSavedCharacter() {
  try {
    return normalizeCharacter(JSON.parse(localStorage.getItem(STORAGE_KEY)));
  } catch {
    return defaultCharacter();
  }
}

const character = readSavedCharacter();
let worldState = character.worldState;
let shopState = character.shopState;
let selectedRaceId = character.raceId;
let appearance = character.appearance;
let level = character.level;
let exp = character.exp;
let availablePoints = character.availablePoints;
let perkPoints = character.perkPoints;
let trapsUsed = character.trapsUsed;
let unarmedAttacks = character.unarmedAttacks;
let summonSpellsUsed = character.summonSpellsUsed;
let summonSpellsUsedByElement = character.summonSpellsUsedByElement;
let bloodSupplies = character.bloodSupplies;
let itemsSold = character.itemsSold;
let soloSurvivorVictories = character.soloSurvivorVictories;
let multiKillCombats = character.multiKillCombats;
let classKills = character.classKills;
let raceKills = character.raceKills;
let classProgress = character.classProgress;
let defensiveActionsUsed = character.defensiveActionsUsed;
let flawlessVictories = character.flawlessVictories;
let enemyCount = character.enemyCount;
let editingEnabled = character.editingEnabled;
let darkMode = character.darkMode;
let currentHp = character.currentHp;
let currentMana = character.currentMana;
let manualAllocations = character.manualAllocations;
let limitless = character.limitless;
let currencies = character.currencies;
let permanentBonuses = character.permanentBonuses;
let permanentDefenseBonuses = character.permanentDefenseBonuses;
let inventory = character.inventory;
let favoriteItems = character.favoriteItems;
let equipment = character.equipment;
let knownSpells = character.knownSpells;
classLevels = character.classLevels;
activeSkills = character.activeSkills;
let companions = character.companions;
let recruitOffers = character.recruitOffers;
let pendingClassSpellChoice = character.pendingClassSpellChoice;
let completedClassChoices = character.completedClassChoices;
let stats = character.stats;

function toNumber(value, fallback = 0) {
  const number = Number(value);
  return Number.isFinite(number) ? number : fallback;
}

function toInteger(value, fallback = 0) {
  return Math.floor(toNumber(value, fallback));
}

function formatValue(value) {
  const rounded = Math.round(value * 10) / 10;
  return Number.isInteger(rounded) ? String(rounded) : rounded.toFixed(1);
}

function updateTheme() {
  document.documentElement.classList.toggle("dark-mode", darkMode);
  themeIcon.textContent = darkMode ? "☀" : "☾";
  themeToggle.title = darkMode ? "Switch to light mode" : "Switch to dark mode";
  themeToggle.setAttribute("aria-label", themeToggle.title);
  themeToggle.setAttribute("aria-pressed", String(darkMode));
}

function createStatCards() {
  const activeValues = currentStats();
  statsGrid.innerHTML = statDefinitions.map(({ key, label, short, color }, index) => `
    <article class="stat-card" style="--stat-color: ${color}">
      <div class="stat-topline">
        <label class="stat-name" for="stat-${key}">${label}</label>
        <span class="stat-index">0${index + 1}</span>
      </div>
      <div class="stat-value-labels"><span>Base</span><span>Current</span></div>
      <div class="stat-value-row">
        <div class="stat-value-column stat-base-column">
          <div class="stat-input-wrap">
            <button class="stat-stepper" data-action="decrease" data-stat="${key}" type="button" aria-label="Decrease base ${label}">−</button>
            <input class="stat-input" id="stat-${key}" data-stat="${key}" type="number" inputmode="numeric" step="1" value="${displayedBaseStat(key)}" aria-label="Base ${label} value" />
            <button class="stat-stepper" data-action="increase" data-stat="${key}" type="button" aria-label="Increase base ${label}">+</button>
            <span class="stat-unit">${short}</span>
          </div>
        </div>
        <div class="stat-value-column stat-current-column">
          <strong class="stat-current-value" id="stat-current-${key}">${activeValues[key]}</strong>
          <span class="stat-unit">${short}</span>
        </div>
      </div>
      <div class="stat-bonuses">
        <div class="stat-modifier-wrap">
          <label for="stat-permanent-bonus-${key}">Permanent bonus</label>
          <div class="stat-modifier-field">
            <input class="modifier-input stat-modifier-input stat-bonus-input" id="stat-permanent-bonus-${key}" data-bonus="permanent" data-stat="${key}" type="number" inputmode="decimal" step="any" value="${effectivePermanentBonus(key)}" aria-label="${label} permanent percentage bonus" />
            <span>%</span>
          </div>
        </div>
      </div>
      <div class="stat-bar" aria-hidden="true"><span></span></div>
    </article>
  `).join("");

  statsGrid.querySelectorAll(".stat-input").forEach((input) => {
    input.addEventListener("input", () => {
      stats[input.dataset.stat] = toInteger(input.value, 0) - raceStatModifier(input.dataset.stat);
      input.value = displayedBaseStat(input.dataset.stat);
      updateDerivedValues();
      saveCharacter();
    });
  });

  statsGrid.querySelectorAll(".stat-bonus-input").forEach((input) => {
    input.addEventListener("input", () => {
      const artifactBonus = calculateArtifactEffects().statPermanentPercent[input.dataset.stat];
      const classBonus = calculateClassEffects().statPermanentPercent[input.dataset.stat];
      permanentBonuses[input.dataset.stat] = toNumber(input.value, 0) - artifactBonus - classBonus;
      input.value = effectivePermanentBonus(input.dataset.stat);
      updateDerivedValues();
      saveCharacter();
    });
  });

  statsGrid.querySelectorAll(".stat-stepper").forEach((button) => {
    button.addEventListener("click", () => {
      const key = button.dataset.stat;
      const change = button.dataset.action === "increase" ? 1 : -1;
      const spendsPoint = change > 0 && availablePoints > 0;
      if (!editingEnabled && !spendsPoint) return;
      stats[key] = toInteger(stats[key], 0) + change;
      if (spendsPoint) {
        availablePoints -= 1;
        manualAllocations[key] += 1;
        LimitlessRules.noteSpent(limitless, key, 1, level);
      }
      document.querySelector(`#stat-${key}`).value = displayedBaseStat(key);
      updateEditingControls();
      updateDerivedValues();
      saveCharacter();
    });
  });

  updateEditingControls();
}

function updateEditingControls() {
  editingToggle.classList.toggle("is-on", editingEnabled);
  editingToggle.setAttribute("aria-checked", String(editingEnabled));
  const devModeAction = editingEnabled ? "Disable Dev-mode" : "Enable Dev-mode";
  editingToggle.setAttribute("aria-label", devModeAction);
  editingToggle.title = devModeAction;
  pointsValue.textContent = String(availablePoints);
  respecButton.disabled = totalManualAllocations() === 0;
  statsGrid.querySelectorAll(".stat-input, .stat-stepper, .stat-bonus-input").forEach((control) => {
    if (control.classList.contains("stat-input") || control.classList.contains("stat-bonus-input")) {
      control.disabled = !editingEnabled;
    } else if (control.dataset.action === "decrease") {
      control.disabled = !editingEnabled;
    } else {
      control.disabled = !editingEnabled && availablePoints <= 0;
    }
  });
  document.querySelectorAll(".defense-bonus-input").forEach((control) => {
    control.disabled = !editingEnabled;
  });
  // Money is earned and spent in the world; only Dev Mode edits the purse by hand.
  document.querySelectorAll(".currency-input").forEach((control) => {
    control.disabled = !editingEnabled;
    control.closest(".sidebar-currency").title = editingEnabled ? "" : "Enable Dev Mode to edit money";
  });
  document.querySelector(".sidebar-purse").classList.toggle("is-locked", !editingEnabled);
  document.querySelectorAll("[data-dev-only]").forEach((control) => {
    control.hidden = !editingEnabled;
  });
  statsGrid.classList.toggle("is-locked", !editingEnabled);
}

function updateBonusInputs() {
  const artifactPermanentBonuses = calculateArtifactEffects().statPermanentPercent;
  const classEffects = calculateClassEffects();
  const raceEffects = calculateRaceEffects();
  document.querySelectorAll(".stat-bonus-input").forEach((input) => {
    input.value = (permanentBonuses[input.dataset.stat] ?? 0) + artifactPermanentBonuses[input.dataset.stat] + classEffects.statPermanentPercent[input.dataset.stat] + raceEffects.statPermanentPercent[input.dataset.stat] + (window.Combat?.permanentStatPercent?.(input.dataset.stat) || 0);
  });
  document.querySelectorAll(".defense-bonus-input").forEach((input) => {
    input.value = (permanentDefenseBonuses[input.dataset.defense] ?? 0) + classEffects.defensePermanentPercent[input.dataset.defense] + raceEffects.defensePermanentPercent[input.dataset.defense] + (window.Combat?.permanentDefensePercent?.(input.dataset.defense) || 0);
  });
}

function updateDerivedValues() {
  const activeValues = currentStats();
  updateBonusInputs();
  // HP = ((1.5 × Vitality) + (½ × Strength) + (½ × Mind)) × 1.25
  const maxHp = calculateMaxHpWithArtifacts(activeValues);
  // Mana = Mind + (½ × Intelligence) + (½ × Wisdom)
  const maxMana = calculateMaxManaWithEffects(activeValues);
  const equipmentDefense = calculateEquipmentDefense(activeValues);
  const baseArmor = BASE_ARMOR;
  const baseEvasion = Math.floor(calculateEvasion(activeValues));
  const modifiedArmor = baseArmor + equipmentDefense.armor;
  const modifiedEvasion = Math.max(0, Math.floor((baseEvasion - equipmentDefense.evasionDebuff + equipmentDefense.evasionFlat) * (1 + equipmentDefense.evasionPercent / 100)));
  const classEffects = calculateClassEffects();
  const raceEffects = calculateRaceEffects();
  const activeDefenseBonuses = bonusValues(permanentDefenseBonuses, defenseBonusDefinitions);
  defenseBonusDefinitions.forEach((key) => {
    activeDefenseBonuses[key] += classEffects.defensePermanentPercent[key] + raceEffects.defensePermanentPercent[key] + (window.Combat?.permanentDefensePercent?.(key) || 0);
  });
  const rawArmor = Math.floor(modifiedArmor * (1 + (activeDefenseBonuses.armor / 100))) + classEffects.defenseFlat.armor;
  const rawEvasion = Math.floor(modifiedEvasion * (1 + (activeDefenseBonuses.evasion / 100))) + classEffects.defenseFlat.evasion + raceEffects.defenseFlat.evasion;
  const currentArmor = Math.min(equippedDefenseCap("armor"), classEffects.defenseCap.armor, raceEffects.defenseCap.armor, window.Combat ? window.Combat.defense("armor", rawArmor) : rawArmor);
  const currentEvasion = Math.min(equippedDefenseCap("evasion"), classEffects.defenseCap.evasion, raceEffects.defenseCap.evasion, window.Combat ? window.Combat.defense("evasion", rawEvasion) : rawEvasion);
  const selectedRaceIsReady = !selectedRaceId || Boolean(getSelectedRace()) || raceSourceState !== "loading";
  if (selectedRaceIsReady) {
    currentHp = Math.min(maxHp, Math.max(0, currentHp));
    currentMana = Math.min(maxMana, Math.max(0, currentMana));
  }
  const levelCap = LimitlessRules.cap(limitless.active), atLevelCap = level >= levelCap;
  const nextLevel = level + 1;
  const expRequired = CombatRules.experienceRequired(level);
  const expRemaining = Math.max(0, expRequired - exp);
  const expPercent = Math.min(100, Math.max(0, (exp / expRequired) * 100));

  levelValue.textContent = formatValue(level);
  const capLabel = document.querySelector(".level-card .level-cap");
  if (capLabel) capLabel.textContent = `CAP ${levelCap}`;
  hpCurrent.textContent = formatValue(currentHp);
  hpMax.textContent = formatValue(maxHp);
  manaCurrent.textContent = formatValue(currentMana);
  manaMax.textContent = formatValue(maxMana);
  // Presentation only: reuse the already-calculated, combat-safe resource caps.
  document.getElementById("hp-meter").style.transform = "scaleX(" + (maxHp > 0 ? Math.min(1, Math.max(0, currentHp / maxHp)) : 0) + ")";
  document.getElementById("mana-meter").style.transform = "scaleX(" + (maxMana > 0 ? Math.min(1, Math.max(0, currentMana / maxMana)) : 0) + ")";
  armorBaseValue.textContent = formatValue(baseArmor);
  armorModifiedValue.textContent = formatValue(modifiedArmor);
  armorValue.textContent = String(currentArmor);
  evasionBaseValue.textContent = String(baseEvasion);
  evasionModifiedValue.textContent = String(modifiedEvasion);
  evasionValue.textContent = String(currentEvasion);
  spellbookManaCurrent.textContent = formatValue(currentMana);
  spellbookManaMax.textContent = formatValue(maxMana);
  expCurrent.textContent = formatValue(exp);
  expThreshold.textContent = atLevelCap ? "MAX LEVEL" : `/ ${formatValue(expRequired)} EXP`;
  expHelper.textContent = atLevelCap
    ? `Level cap reached · ${levelCap}`
    : editingEnabled
    ? `Dev-mode level up enabled · ${formatValue(expRequired)} EXP target`
    : `${formatValue(expRemaining)} EXP to reach Level ${formatValue(nextLevel)}`;
  expBar.style.width = atLevelCap ? "100%" : `${expPercent}%`;
  expTrack.setAttribute("aria-valuemax", String(atLevelCap ? 1 : expRequired));
  expTrack.setAttribute("aria-valuenow", String(atLevelCap ? 1 : Math.min(exp, expRequired)));
  levelUpButton.disabled = atLevelCap || (!editingEnabled && exp < expRequired);
  levelIcon.classList.toggle("has-level-up", !atLevelCap && exp >= expRequired);
  updateClassPointDisplay();

  statsGrid.querySelectorAll(".stat-card").forEach((card) => {
    const input = card.querySelector(".stat-input");
    const currentValue = card.querySelector(".stat-current-value");
    const bar = card.querySelector(".stat-bar span");
    const currentStatValue = activeValues[input.dataset.stat];
    currentValue.textContent = String(currentStatValue);
    const ratio = Math.min(100, Math.max(7, (Math.abs(currentStatValue) / 25) * 100));
    bar.style.width = `${ratio}%`;
  });
  renderEquipmentSlots();
}

function saveCharacter() {
  window.Combat?.onSheetChange();
  window.WorldMap?.renderClock();
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify({ worldState, shopState, combatState: window.Combat?.serialize() || null, raceId: selectedRaceId, appearance, level, exp, availablePoints, perkPoints, startingPerkPointGranted: true, limitless, trapsUsed, unarmedAttacks, summonSpellsUsed, summonSpellsUsedByElement, bloodSupplies, itemsSold, soloSurvivorVictories, multiKillCombats, classKills, raceKills, classProgress, defensiveActionsUsed, flawlessVictories, enemyCount, editingEnabled, darkMode, currentHp, currentMana, manualAllocations, currencies, permanentBonuses, permanentDefenseBonuses, inventory, favoriteItems, equipment, knownSpells, classLevels, activeSkills, companions, recruitOffers, pendingClassSpellChoice, completedClassChoices, stats }));
    statusDot.classList.add("is-saving");
    window.clearTimeout(saveCharacter.timer);
    saveCharacter.timer = window.setTimeout(() => statusDot.classList.remove("is-saving"), 350);
  } catch {
    // Local storage may be unavailable in a private or restricted browser context.
  }
}

function setBackupStatus(message, state = "") {
  backupStatus.textContent = message;
  backupStatus.className = `backup-status topbar-file-status${state ? ` is-${state}` : ""}`;
}

function backupPayload() {
  return {
    format: "emberfall-stat-block",
    version: 22,
    exportedAt: new Date().toISOString(),
    character: { worldState, shopState, combatState: window.Combat?.serialize() || null, raceId: selectedRaceId, appearance, level, exp, availablePoints, perkPoints, startingPerkPointGranted: true, limitless, trapsUsed, unarmedAttacks, summonSpellsUsed, summonSpellsUsedByElement, bloodSupplies, itemsSold, soloSurvivorVictories, multiKillCombats, classKills, raceKills, classProgress, defensiveActionsUsed, flawlessVictories, enemyCount, editingEnabled, darkMode, currentHp, currentMana, manualAllocations, currencies, permanentBonuses, permanentDefenseBonuses, inventory, favoriteItems, equipment, knownSpells, classLevels, activeSkills, companions, recruitOffers, pendingClassSpellChoice, completedClassChoices, stats },
  };
}

async function exportBackup() {
  const json = JSON.stringify(backupPayload(), null, 2);
  const filename = "stat-fick-character.json";
  try {
    if (window.statFickDesktop) {
      if (await window.statFickDesktop.saveCharacter(json)) setBackupStatus("Character saved", "success");
      return;
    }
    if ("showSaveFilePicker" in window) {
      const handle = await window.showSaveFilePicker({
        suggestedName: filename,
        types: [{ description: "Stat Fick stat block", accept: { "application/json": [".json"] } }],
      });
      const writable = await handle.createWritable();
      await writable.write(json);
      await writable.close();
      setBackupStatus("Character saved", "success");
      return;
    }

    const blob = new Blob([json], { type: "application/json" });
    const url = URL.createObjectURL(blob);
    const link = document.createElement("a");
    link.href = url;
    link.download = filename;
    document.body.appendChild(link);
    link.click();
    link.remove();
    window.setTimeout(() => URL.revokeObjectURL(url), 0);
    setBackupStatus("Character saved to file", "success");
  } catch (error) {
    if (error.name !== "AbortError") setBackupStatus("Character could not be saved", "error");
  }
}

function applyCharacter(characterData) {
  window.WorldMap?.pause();
  worldState = WorldRules.normalize(characterData.worldState);
  shopState = characterData.shopState;
  selectedRaceId = characterData.raceId;
  appearance = characterData.appearance ?? null;
  level = characterData.level;
  exp = characterData.exp;
  availablePoints = characterData.availablePoints;
  perkPoints = characterData.perkPoints;
  limitless = characterData.limitless || LimitlessRules.normalize(null);
  trapsUsed = characterData.trapsUsed;
  unarmedAttacks = characterData.unarmedAttacks;
  summonSpellsUsed = characterData.summonSpellsUsed;
  summonSpellsUsedByElement = characterData.summonSpellsUsedByElement;
  bloodSupplies = characterData.bloodSupplies;
  itemsSold = characterData.itemsSold;
  soloSurvivorVictories = characterData.soloSurvivorVictories;
  multiKillCombats = characterData.multiKillCombats;
  classKills = characterData.classKills;
  raceKills = characterData.raceKills;
  classProgress = characterData.classProgress;
  defensiveActionsUsed = characterData.defensiveActionsUsed;
  flawlessVictories = characterData.flawlessVictories;
  enemyCount = characterData.enemyCount;
  editingEnabled = characterData.editingEnabled;
  darkMode = characterData.darkMode;
  currentHp = characterData.currentHp;
  currentMana = characterData.currentMana;
  manualAllocations = characterData.manualAllocations;
  currencies = characterData.currencies;
  permanentBonuses = characterData.permanentBonuses;
  permanentDefenseBonuses = characterData.permanentDefenseBonuses;
  inventory = characterData.inventory;
  favoriteItems = characterData.favoriteItems || [];
  equipment = characterData.equipment;
  knownSpells = characterData.knownSpells;
  classLevels = characterData.classLevels;
  activeSkills = characterData.activeSkills;
  companions = characterData.companions;
  recruitOffers = characterData.recruitOffers;
  pendingClassSpellChoice = characterData.pendingClassSpellChoice;
  completedClassChoices = characterData.completedClassChoices;
  stats = characterData.stats;
  document.querySelectorAll(".currency-input").forEach((input) => {
    input.value = currencies[input.dataset.currency];
  });
  window.Combat?.restore(characterData.combatState);
  window.Music?.sync();
  updateTheme();
  createStatCards();
  updateBonusInputs();
  updateDerivedValues();
  renderOwnedItems();
  renderAvailableItems();
  renderKnownSpells();
  renderAvailableSpells();
  renderClasses();
  renderSkills();
  renderPassives();
  resumePendingClassSpellChoice();
  updateRaceHero();
  renderRaces();
  window.WorldMap?.resetView();
  window.Shop?.onCharacterApplied();
  window.Guild?.onCharacterApplied();
  window.Companions?.onCharacterApplied();
  window.Limitless?.render();
  saveCharacter();
}

backupExportButton.addEventListener("click", exportBackup);

backupRestoreButton.addEventListener("click", async () => {
  if (!window.statFickDesktop) { backupFileInput.click(); return; }
  try {
    const file = await window.statFickDesktop.loadCharacter();
    if (file) importCharacterText(file.text, file.name);
  } catch { setBackupStatus("That file is not a valid character save", "error"); }
});

function importCharacterText(text, name) {
  const payload = JSON.parse(text);
  const characterData = payload?.character ?? payload;
  const hasStats = characterData && typeof characterData === "object" && (characterData.stats || statDefinitions.some(({ key }) => key in characterData));
  if (!hasStats) throw new Error("Not a character save");
  applyCharacter(normalizeCharacter(characterData));
  setBackupStatus(`Loaded ${name}`, "success");
}

backupFileInput.addEventListener("change", async (event) => {
  const [file] = event.target.files;
  if (!file) return;
  try {
    importCharacterText(await file.text(), file.name);
  } catch {
    setBackupStatus("That file is not a valid character save", "error");
  } finally {
    backupFileInput.value = "";
  }
});

function totalManualAllocations() {
  return Object.values(manualAllocations).reduce((total, value) => total + value, 0);
}

function adjustResource(resource, amount) {
  const activeValues = currentStats();
  const max = resource === "hp" ? calculateMaxHpWithArtifacts(activeValues) : calculateMaxManaWithEffects(activeValues);
  if (resource === "hp") {
    if (amount > 0 && calculateRaceEffects().healingDisabled) return;
    currentHp = Math.min(max, Math.max(0, currentHp + amount));
  } else {
    currentMana = Math.min(max, Math.max(0, currentMana + amount));
  }
  updateDerivedValues();
  saveCharacter();
}

function restoreResource(resource) {
  const activeValues = currentStats();
  if (resource === "hp" && !calculateRaceEffects().healingDisabled) currentHp = calculateMaxHpWithArtifacts(activeValues);
  if (resource === "mana") currentMana = calculateMaxManaWithEffects(activeValues);
  updateDerivedValues();
  saveCharacter();
}

function healResourcesOnLevelUp() {
  const activeValues = currentStats();
  const maxHp = calculateMaxHpWithArtifacts(activeValues);
  const maxMana = calculateMaxManaWithEffects(activeValues);
  if (!calculateRaceEffects().healingDisabled) currentHp = Math.min(maxHp, currentHp + Math.floor(maxHp * 0.5));
  currentMana = Math.min(maxMana, currentMana + Math.floor(maxMana * 0.5));
}

expForm.addEventListener("submit", (event) => {
  event.preventDefault();
  if (!editingEnabled) return;
  const amount = toNumber(expInput.value, 0);
  if (amount <= 0) return;
  exp += amount;
  expInput.value = "";
  updateDerivedValues();
  saveCharacter();
});

document.querySelectorAll(".resource-adjust-button").forEach((button) => {
  button.addEventListener("click", () => {
    const input = document.querySelector(`#${button.dataset.resource}-adjust`);
    const amount = Math.abs(toInteger(input.value, 0));
    if (amount <= 0) return;
    adjustResource(button.dataset.resource, amount * Number(button.dataset.change));
    input.value = "";
  });
});

restoreButton.addEventListener("click", () => {
  const activeValues = currentStats();
  if (!calculateRaceEffects().healingDisabled) currentHp = calculateMaxHpWithArtifacts(activeValues);
  currentMana = calculateMaxManaWithEffects(activeValues);
  updateDerivedValues();
  saveCharacter();
});

document.querySelectorAll(".resource-restore-button").forEach((button) => {
  button.addEventListener("click", () => restoreResource(button.dataset.restoreResource));
});

document.querySelectorAll(".currency-input").forEach((input) => {
  const key = input.dataset.currency;
  input.value = currencies[key];
  input.addEventListener("input", () => {
    currencies[key] = Math.max(0, toInteger(input.value, 0));
    input.value = currencies[key];
    saveCharacter();
  });
});


document.querySelectorAll(".defense-bonus-input").forEach((input) => {
  input.addEventListener("input", () => {
    const classBonus = calculateClassEffects().defensePermanentPercent[input.dataset.defense];
    const raceBonus = calculateRaceEffects().defensePermanentPercent[input.dataset.defense];
    permanentDefenseBonuses[input.dataset.defense] = toNumber(input.value, 0) - classBonus - raceBonus;
    input.value = permanentDefenseBonuses[input.dataset.defense] + classBonus + raceBonus;
    updateDerivedValues();
    saveCharacter();
  });
});

editingToggle.addEventListener("click", () => {
  editingEnabled = !editingEnabled;
  updateEditingControls();
  updateDerivedValues();
  updateRaceHero();
  renderRaces();
  window.Companions?.render(true);
  saveCharacter();
});

respecButton.addEventListener("click", () => {
  const refund = totalManualAllocations();
  if (refund === 0) return;
  statDefinitions.forEach(({ key }) => {
    stats[key] = toInteger(stats[key], 0) - manualAllocations[key];
    manualAllocations[key] = 0;
  });
  limitless.spent = LimitlessRules.normalize(null).spent;
  availablePoints += refund;
  createStatCards();
  updateDerivedValues();
  saveCharacter();
});

themeToggle.addEventListener("click", () => {
  darkMode = !darkMode;
  updateTheme();
  saveCharacter();
});

levelUpButton.addEventListener("click", () => {
  if (level >= LimitlessRules.cap(limitless.active)) return;
  const expRequired = CombatRules.experienceRequired(level);
  if (!editingEnabled && exp < expRequired) return;
  level += 1;
  exp = Math.max(0, exp - expRequired);
  availablePoints += 5;
  // Levels past 100 (Limitless) give no perk point: no new class levels (task 7, 2.1).
  if (level <= LimitlessRules.BASE_CAP) perkPoints += 1;
  statDefinitions.forEach(({ key }) => {
    stats[key] = toInteger(stats[key], 0) + 1;
  });
  healResourcesOnLevelUp();
  createStatCards();
  updateDerivedValues();
  renderClasses();
  saveCharacter();
});

resetButton.addEventListener("click", () => {
  if (!window.confirm("Create a new character and clear the current character sheet?")) return;
  if (!window.confirm("Are you sure? All current character progress will be cleared. Save the character first if you want to keep it.")) return;
  window.WorldMap?.pause();
  worldState = WorldRules.normalize();
  shopState = { version: 4, refreshWindow: -1, locations: {} };
  window.WorldMap?.resetView();
  window.Combat?.restore(null);
  selectedRaceId = null;
  appearance = null;
  stats = { ...defaultStats };
  level = 1;
  exp = 0;
  availablePoints = 0;
  perkPoints = 1;
  trapsUsed = 0;
  unarmedAttacks = 0;
  summonSpellsUsed = 0;
  summonSpellsUsedByElement = {};
  bloodSupplies = 0;
  itemsSold = 0;
  soloSurvivorVictories = 0;
  multiKillCombats = 0;
  classKills = {};
  raceKills = {};
  classProgress = { oneHandedAttacks: 0, summonCasts: {}, lootEnemiesCollected: 0, boundFamiliar: null, claimedGuaranteedDrops: [], firstKillDrops: [] };
  defensiveActionsUsed = 0;
  flawlessVictories = 0;
  enemyCount = 0;
  manualAllocations = { ...defaultAllocations };
  limitless = LimitlessRules.normalize(null);
  currencies = { ...defaultCurrencies };
  permanentBonuses = { ...defaultBonuses };
  permanentDefenseBonuses = { ...defaultDefenseBonuses };
  inventory = [];
  favoriteItems = [];
  equipment = { ...defaultEquipment };
  classLevels = {};
  activeSkills = {};
  companions = [];
  recruitOffers = [];
  pendingClassSpellChoice = null;
  completedClassChoices = [];
  classSpellChoiceModal.hidden = true;
  currentHp = calculateMaxHpWithArtifacts(currentStats());
  currentMana = calculateMaxManaWithEffects(currentStats());
  knownSpells = [];
  document.querySelectorAll(".currency-input").forEach((input) => {
    input.value = currencies[input.dataset.currency];
  });
  createStatCards();
  updateBonusInputs();
  updateDerivedValues();
  renderOwnedItems();
  renderAvailableItems();
  renderKnownSpells();
  renderAvailableSpells();
  renderClasses();
  renderSkills();
  renderPassives();
  updateRaceHero();
  renderRaces();
  window.Shop?.onCharacterApplied();
  window.Guild?.onCharacterApplied();
  window.Companions?.onCharacterApplied();
  saveCharacter();
  raceSelectButton.focus();
});

updateTheme();
createStatCards();
updateBonusInputs();
updateDerivedValues();
renderOwnedItems();
renderAvailableItems();

function slugify(value) {
  return value.toLowerCase().trim().replace(/[^a-z0-9]+/g, "-").replace(/^-|-$/g, "");
}

function spellId(name, element, type) {
  return [slugify(name), element, slugify(type)].join("-");
}

function parseSpellComponent(value) {
  if (value.trim().toUpperCase() === "N") return { ratio: 0, code: "N", key: null, none: true };
  const match = value.trim().match(/^(-?(?:\d+(?:\.\d+)?|\.\d+))([SVMDIWPL])$/i);
  if (!match) return { ratio: 0, code: "M", key: "mind" };
  const code = match[2].toUpperCase();
  return { ratio: Number(match[1]), code, key: STAT_CODES[code] || "mind" };
}

function parseLegacySpellRecord(record) {
  const fields = splitDelimitedRecord(record.trim());
  if (fields.length < 7) return null;
  const hasTier = fields.length >= 8;
  const name = fields[0].trim();
  const element = SPELL_ELEMENTS.includes(fields[1].trim().toLowerCase()) ? fields[1].trim().toLowerCase() : "basic";
  const tier = hasTier ? Math.max(1, toInteger(fields[2].replace(/^tier\s*/i, ""), 1)) : 1;
  const typeIndex = hasTier ? 3 : 2;
  const manaIndex = hasTier ? 4 : 3;
  const primaryIndex = hasTier ? 5 : 4;
  const secondaryIndex = hasTier ? 6 : 5;
  const descriptionIndex = hasTier ? 7 : 6;
  const type = fields[typeIndex].trim();
  return {
    id: spellId(name, element, type),
    name: name.charAt(0).toUpperCase() + name.slice(1),
    element,
    tier,
    type,
    manaCost: Math.max(0, toInteger(fields[manaIndex], 0)),
    priceSilver: 0,
    primary: parseSpellComponent(fields[primaryIndex]),
    secondary: parseSpellComponent(fields[secondaryIndex]),
    description: fields[descriptionIndex].trim(),
  };
}

function parseSpellsFile(text, allowLegacy = false) {
  if (allowLegacy && text.trim() && !/^[\[{]/.test(text.trim())) {
    return text.split(";").map(parseLegacySpellRecord).filter(Boolean);
  }
  const records = JSON.parse(text);
  if (!Array.isArray(records)) throw new Error("The spell catalogue must be a JSON array");
  const seen = new Set();
  return records.map((record, index) => {
    const invalid = (message) => { throw new Error(`Spell ${index + 1}: ${message}`); };
    if (!record || typeof record !== "object" || Array.isArray(record)) invalid("expected a spell object");
    const stringField = (key) => {
      if (typeof record[key] !== "string" || !record[key].trim()) invalid(`${key} must be a nonempty string`);
      return record[key].trim();
    };
    const component = (value, field) => {
      if (value === null) return { ratio: 0, code: "N", key: null, none: true };
      if (!value || !bonusDefinitions.includes(value.stat) || typeof value.multiplier !== "number" || !Number.isFinite(value.multiplier)) {
        invalid(`${field} must be null or an object with a valid stat and numeric multiplier`);
      }
      const code = Object.keys(STAT_CODES).find((key) => STAT_CODES[key] === value.stat);
      return { ratio: value.multiplier, code, key: value.stat };
    };
    const id = stringField("id");
    if (seen.has(id)) invalid(`duplicate id ${id}`);
    seen.add(id);
    const name = stringField("name");
    const element = stringField("element").toLowerCase();
    if (!SPELL_ELEMENTS.includes(element)) invalid("unknown element");
    if (!Number.isInteger(record.tier) || record.tier < 1) invalid("tier must be a positive integer");
    if (!Number.isFinite(record.manaCost) || record.manaCost < 0) invalid("manaCost must be a nonnegative number");
    if (record.basePower != null && (!Number.isFinite(record.basePower) || record.basePower < 0)) invalid("basePower must be a nonnegative number");
    if (!Number.isInteger(record.priceSilver) || record.priceSilver < 0) invalid("priceSilver must be a nonnegative integer");
    if (typeof record.description !== "string") invalid("description must be a string");
    if (record.requiredCasterEnemyId != null && (typeof record.requiredCasterEnemyId !== "string" || !record.requiredCasterEnemyId.trim())) invalid("requiredCasterEnemyId must name an enemy");
    if (record.requiredCasterEnemyIds != null && (!Array.isArray(record.requiredCasterEnemyIds) || !record.requiredCasterEnemyIds.length || record.requiredCasterEnemyIds.some(id => typeof id !== "string" || !id.trim()) || record.requiredCasterEnemyId)) invalid("requiredCasterEnemyIds must be a nonempty list of enemy IDs, without requiredCasterEnemyId");
    return {
      id,
      name: name.charAt(0).toUpperCase() + name.slice(1),
      element,
      tier: record.tier,
      type: stringField("type"),
      manaCost: record.manaCost,
      priceSilver: record.priceSilver,
      catalogueAvailable: record.catalogueAvailable !== false,
      shopAvailable: record.unique === true ? false : record.shopAvailable !== false,
      basePower: record.basePower ?? 0,
      primary: component(record.primary, "primary"),
      secondary: component(record.secondary, "secondary"),
      description: record.description,
      actionPoints: Math.max(1, toInteger(record.actionPoints, record.name.toLowerCase() === "spark" ? 2 : 3)),
      requiredCasterEnemyId: record.requiredCasterEnemyId || null,
      requiredCasterEnemyIds: record.requiredCasterEnemyIds ? [...new Set(record.requiredCasterEnemyIds)] : [],
      effects: Array.isArray(record.effects) ? record.effects : [],
      // A spell whose effects follow the season of the battle (Juila's Wheel of the Year).
      ...(record.seasonVariants ? { seasonVariants: (() => {
        if (typeof record.seasonVariants !== "object" || Array.isArray(record.seasonVariants) || Object.values(record.seasonVariants).some(variant => !variant || typeof variant.name !== "string" || !Array.isArray(variant.effects) || !variant.effects.length)) invalid("seasonVariants must map seasons to a name and effects");
        return record.seasonVariants;
      })() } : {}),
    };
  });
}

function calculateSpellValue(spell) {
  const activeValues = currentStats();
  const primaryValue = spell.primary.none ? 0 : spell.primary.ratio * toInteger(activeValues[spell.primary.key], 0);
  const secondaryValue = spell.secondary.none ? 0 : spell.secondary.ratio * toInteger(activeValues[spell.secondary.key], 0);
  const basePower = toNumber(spell.basePower, 0) + primaryValue + secondaryValue;
  const artifactEffects = calculateArtifactEffects();
  const classEffects = calculateClassEffects();
  const spellElements = effectiveSpellElements(spell, classEffects);
  const elementalClassPower = classEffects.spellPowerPercentModifiers.reduce((total, modifier) => {
    const restricted = [modifier.element, ...(modifier.elements || [])].filter(Boolean);
    return !restricted.length || restricted.some(element => spellElements.includes(element)) ? total + modifier.amount : total;
  }, 0);
  const artifactSpellPowerFlat = artifactEffects.spellPowerFlat + artifactSpellModifier(artifactEffects, "spell-power-flat", spell);
  const artifactSpellPowerPercent = artifactEffects.spellPowerPercent + artifactSpellModifier(artifactEffects, "spell-power-percent", spell);
  return (basePower + artifactSpellPowerFlat + classEffects.spellPowerFlat)
    * (1 + ((artifactSpellPowerPercent + classEffects.spellPowerPercent + elementalClassPower + equippedWeaponSpellPowerPercent(spell) + toNumber(window.Combat?.alliedSpellPowerPercent?.(), 0)) / 100));
}

function isSummonSpell(spell) {
  return Array.isArray(spell?.effects) && spell.effects.some((effect) => effect.type === "summon-friendly");
}

function effectiveSpellElement(spell, classEffects = calculateClassEffects()) {
  return isSummonSpell(spell) && classEffects.summonSpellElement ? classEffects.summonSpellElement : spell.element;
}

function effectiveSpellElements(spell, classEffects = calculateClassEffects()) {
  const primary = effectiveSpellElement(spell, classEffects);
  const elements = [primary];
  for (const modifier of classEffects.spellAdditionalElementModifiers || []) {
    if (modifier.sourceElements.length && !modifier.sourceElements.includes(primary)) continue;
    elements.push(...modifier.elements);
  }
  for (const effect of equippedWeaponItems().flatMap(item => item.effects || [])) {
    if (effect.kind === "spell-add-element" && primary !== effect.element) elements.push(effect.element);
  }
  return [...new Set(elements.filter(element => SPELL_ELEMENTS.includes(element)))];
}

function calculateSpellManaCost(spell) {
  const artifactEffects = calculateArtifactEffects();
  const classEffects = calculateClassEffects();
  const raceEffects = calculateRaceEffects();
  const spellElement = effectiveSpellElement(spell, classEffects);
  const spellElements = effectiveSpellElements(spell, classEffects);
  const adjustedSpell = spellElement === spell.element ? spell : { ...spell, element: spellElement };
  const classCostPercent = classEffects.spellCostPercentModifiers.reduce((total, modifier) => {
    const tierMatches = modifier.maxTier == null || spell.tier <= modifier.maxTier;
    const restricted = [modifier.element, ...(modifier.elements || [])].filter(Boolean);
    const elementMatches = !restricted.length || restricted.some(element => spellElements.includes(element));
    const summonMatches = !modifier.summonOnly || isSummonSpell(spell);
    return tierMatches && elementMatches && summonMatches ? total + modifier.amount : total;
  }, 0);
  const raceCostPercent = raceEffects.spellCostPercent + raceEffects.spellCostPercentModifiers.reduce((total, modifier) => {
    const elementMatches = !modifier.element || modifier.element === spellElement;
    const nameMatches = !modifier.namePrefix || adjustedSpell.name.toLowerCase().startsWith(modifier.namePrefix);
    return elementMatches && nameMatches ? total + modifier.amount : total;
  }, 0);
  const equipmentCostPercent = equippedArmorSpellCostPercent(adjustedSpell) + equippedWeaponSpellCostPercent(adjustedSpell);
  const artifactSpellCostFlat = artifactEffects.spellCostFlat + artifactSpellModifier(artifactEffects, "spell-cost-flat", adjustedSpell);
  const artifactSpellCostPercent = artifactEffects.spellCostPercent + artifactSpellModifier(artifactEffects, "spell-cost-percent", adjustedSpell);
  const partyCostPercent = Math.max(0, toNumber(window.Combat?.partySpellCostPercent?.(), 0));
  const modifiedCost = (spell.manaCost - artifactSpellCostFlat)
    * (1 - ((artifactSpellCostPercent + classCostPercent + raceCostPercent + equipmentCostPercent + partyCostPercent) / 100))
    * classEffects.spellCostMultiplier;
  return Math.max(0, Math.floor(modifiedCost));
}

function calculateSpellActionPoints(spell) {
  const baseCost = Math.max(1, toInteger(spell?.actionPoints, 3));
  const classEffects = calculateClassEffects();
  const spellElements = effectiveSpellElements(spell, classEffects);
  const applies = modifier => {
    const restricted = [modifier.element, ...(modifier.elements || [])].filter(Boolean);
    return (!modifier.summonOnly || isSummonSpell(spell)) && (!restricted.length || restricted.some(element => spellElements.includes(element)));
  };
  const applicableSetModifiers = classEffects.spellActionPointSetModifiers.filter(applies);
  const exactSetModifiers = applicableSetModifiers.filter((modifier) => modifier.exact);
  const setCost = exactSetModifiers.length ? exactSetModifiers.at(-1).amount : applicableSetModifiers.reduce((cost, modifier) => {
    return !modifier.summonOnly || isSummonSpell(spell) ? Math.min(cost, modifier.amount) : cost;
  }, baseCost);
  return Math.max(1, classEffects.spellActionPointFlatModifiers.reduce((cost, modifier) => {
    return applies(modifier) ? cost + modifier.amount : cost;
  }, setCost));
}

function unlockedPartySpellLearningEffects(characterRecord) {
  return classCatalog.flatMap((classDefinition) => classDefinition.levels
    .filter((levelDefinition) => levelDefinition.level <= toInteger(characterRecord.classLevels?.[classDefinition.id], 0))
    .flatMap((levelDefinition) => levelDefinition.effects || []))
    .filter((effect) => effect.type === "passive")
    .flatMap((passive) => [...(passive.effects || []), ...(passive.effect ? [passive.effect] : [])])
    .filter((effect) => effect.type === "party-spell-learning-cost-percent");
}

function partySpellLearningDiscount(spell, learnerKnownSpells = knownSpells) {
  const party = [
    { classLevels, knownSpells },
    ...companions.map((companion) => companion.character),
  ];
  const teacherDiscount = Math.max(0, ...party.flatMap(unlockedPartySpellLearningEffects).map((effect) => toNumber(effect.amount, 0)));
  const knownByAnother = party.some((member) => member.knownSpells !== learnerKnownSpells && member.knownSpells?.includes(spell.id));
  return knownByAnother ? teacherDiscount : 0;
}

function calculateSpellPurchasePrice(spell) {
  const modifiers = calculateClassEffects().spellLearningCostPercentModifiers;
  const discount = modifiers.reduce((total, modifier) => {
    const tierMatches = modifier.maxTier == null || spell.tier <= modifier.maxTier;
    const elementMatches = !modifier.element || modifier.element === spell.element;
    return tierMatches && elementMatches ? total + modifier.amount : total;
  }, 0) + partySpellLearningDiscount(spell);
  return Math.max(0, Math.floor(toNumber(spell.priceSilver, 0) * (1 - discount / 100)));
}

function spellDescription(spell) {
  const numericValue = Math.floor(calculateSpellValue(spell));
  return spell.description
    .replace(/\[(-?\d+(?:\.\d+)?)\s*\+\s*X\]/g, (_, flat) => String(Math.floor(Number(flat) + numericValue)))
    .replace(/\[X\]/g, String(numericValue))
    .replace(/\bX\b/g, String(numericValue));
}

function escapeHtml(value) {
  return String(value).replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;").replace(/"/g, "&quot;");
}

function spellElementLabel(element) {
  return element.charAt(0).toUpperCase() + element.slice(1);
}

function spellElementsLabel(spell) {
  return effectiveSpellElements(spell).map(spellElementLabel).join(" + ");
}

function spellTierLabel(tier) {
  return tier === 10 ? "Tier 10 · Mythical" : tier ? "Tier " + tier : "Tier unknown";
}

function spellStatLabel(component) {
  if (component.none) return "None";
  const definition = statDefinitions.find(({ key }) => key === component.key);
  return definition ? definition.label : component.key;
}

function getSpell(spellIdValue) {
  return spells.find((spell) => spell.id === spellIdValue);
}

function unavailableSpell(spellIdValue) {
  const name = spellIdValue
    .replace(/-(basic|fire|water|holy|lightning|blood|dark|ice|earth|wind|nature|death)-[^-]+$/, "")
    .replace(/-/g, " ")
    .replace(/^./, (letter) => letter.toUpperCase());
  return {
    id: spellIdValue,
    name,
    element: "basic",
    tier: null,
    type: "Unavailable",
    manaCost: 0,
    priceSilver: 0,
    primary: { ratio: 0, code: "M", key: "mind" },
    secondary: { ratio: 0, code: "M", key: "mind" },
    description: "Load spells.json to reveal this spell's details.",
    unavailable: true,
  };
}

function setSpellbookStatus(message, state = "") {
  spellbookStatus.textContent = message;
  spellbookStatus.className = "spellbook-status" + (state ? " is-" + state : "");
}

function showSpellDetails(spell) {
  if (!spell) return;
  spellDetailPanel.innerHTML = spellDetailMarkup(spell);
}

// The full description of a spell, shared by the spellbook and the Magic Tower.
function spellDetailMarkup(spell) {
  if (spell.unavailable) {
    return (
      '<div class="spell-detail spell-element-basic">' +
        '<div class="spell-detail-heading">' +
          '<div><p class="eyebrow">SPELL DETAIL</p><h3>' + escapeHtml(spell.name) + '</h3></div>' +
          '<span class="spell-detail-tier">' + escapeHtml(spellTierLabel(spell.tier)) + '</span>' +
        '</div>' +
        '<div class="spell-description"><p>' + escapeHtml(spell.description) + '</p></div>' +
      '</div>');
  }
  const spellValue = String(Math.floor(calculateSpellValue(spell)));
  const spellCost = calculateSpellManaCost(spell);
  const noStatScaling = spell.primary.none && spell.secondary.none;
  return '<div class="spell-detail spell-element-' + spell.element + '">' +
      '<div class="spell-detail-heading">' +
        '<div><p class="eyebrow">SPELL DETAIL</p><h3>' + escapeHtml(spell.name) + '</h3></div>' +
        '<span class="spell-detail-tier">' + escapeHtml(spellTierLabel(spell.tier)) + '</span>' +
      '</div>' +
      '<div class="spell-badges"><span>' + escapeHtml(spellElementsLabel(spell)) + '</span><span>' + escapeHtml(spell.type) + '</span></div>' +
      '<div class="spell-detail-cost"><span>Mana cost</span><strong>' + spellCost + '</strong></div>' +
      '<div class="spell-detail-cost"><span>Shop price</span><strong>' + escapeHtml(ShopRules.formatPrice(calculateSpellPurchasePrice(spell))) + '</strong></div>' +
      '<dl class="spell-stat-details' + (noStatScaling ? ' is-none' : '') + '">' +
        '<div><dt>Base power</dt><dd>' + formatValue(spell.basePower || 0) + '</dd></div>' +
        '<div><dt>Primary stat</dt><dd>' + escapeHtml(spellStatLabel(spell.primary)) + '</dd></div>' +
        '<div><dt>Secondary stat</dt><dd>' + escapeHtml(spellStatLabel(spell.secondary)) + '</dd></div>' +
        '<div><dt>Power</dt><dd>' + spellValue + '</dd></div>' +
      '</dl>' +
      '<div class="spell-description"><p>' + escapeHtml(spellDescription(spell)) + '</p></div>' +
    '</div>';
}

function renderKnownSpells() {
  if (spellbookModal.hidden) return;
  const known = knownSpells.map((spellIdValue) => getSpell(spellIdValue) || unavailableSpell(spellIdValue));
  const visibleSpells = spellTypeFilter === "all" ? known : known.filter((spell) => spell.type.toLowerCase() === spellTypeFilter);
  if (!visibleSpells.length) {
    const emptyMessage = known.length && spellTypeFilter !== "all"
      ? "No " + spellTypeFilter + " spells known."
      : editingEnabled ? "No spells learned yet.<br />Use + to add one from the catalogue." : "No spells learned yet. Visit the Magic Tower of a castle city to learn one.";
    knownSpellsList.innerHTML = '<div class="spell-list-empty"><span aria-hidden="true">✧</span><p>' + emptyMessage + '</p></div>';
    return;
  }
  knownSpellsList.innerHTML = visibleSpells.map((spell) =>
    '<article class="spell-entry spell-element-' + spell.element + '" data-spell-id="' + escapeHtml(spell.id) + '">' +
      '<button class="spell-select-button" type="button">' +
        '<span class="spell-entry-name">' + escapeHtml(spell.name) + '</span>' +
      '<span class="spell-entry-meta">' + escapeHtml(spellElementsLabel(spell)) + ' · ' + escapeHtml(spellTierLabel(spell.tier)) + ' · ' + escapeHtml(spell.type) + ' · ' + calculateSpellManaCost(spell) + ' MANA</span>' +
      '</button>' +
      '<div class="ability-action-group spell-actions">' +
        '<button class="spell-use-button" type="button" data-spell-id="' + escapeHtml(spell.id) + '"' + (spell.unavailable || currentMana < calculateSpellManaCost(spell) ? " disabled" : "") + '>Use</button>' +
        '<button class="spell-use-button spell-unlearn-button" type="button" data-unlearn-spell="' + escapeHtml(spell.id) + '" aria-label="Unlearn ' + escapeHtml(spell.name) + '" title="Remove this spell from your spell book">Unlearn</button>' +
      '</div>' +
    '</article>'
  ).join("");

  knownSpellsList.querySelectorAll(".spell-entry").forEach((entry) => {
    const spell = getSpell(entry.dataset.spellId) || unavailableSpell(entry.dataset.spellId);
    entry.addEventListener("mouseenter", () => showSpellDetails(spell));
    entry.querySelector(".spell-select-button").addEventListener("focus", () => showSpellDetails(spell));
    entry.querySelector(".spell-use-button").addEventListener("click", () => useSpell(spell));
    entry.querySelector(".spell-unlearn-button").addEventListener("click", () => {
      if (!knownSpells.includes(spell.id) || !window.confirm("Unlearn " + spell.name + "? You will need to learn or purchase it again.")) return;
      knownSpells = knownSpells.filter(id => id !== spell.id);
      renderKnownSpells();
      renderAvailableSpells();
      renderClasses();
      spellDetailPanel.innerHTML = '<div class="spell-detail-empty"><span aria-hidden="true">✧</span><p>Hover over a spell to reveal its details.</p></div>';
      setSpellbookStatus(spell.name + " unlearned", "success");
      saveCharacter();
    });
  });
}

function renderAvailableSpells() {
  if (spellPicker.hidden) return;
  const searchTerm = spellCatalogueSearchInput.value.trim().toLocaleLowerCase();
  const availableSpells = spells.filter((spell) =>
    spell.catalogueAvailable !== false &&
    !knownSpells.includes(spell.id) &&
    (!searchTerm || spell.name.toLocaleLowerCase().includes(searchTerm))
  );
  if (!availableSpells.length) {
    const message = searchTerm
      ? 'No available spells match "' + escapeHtml(spellCatalogueSearchInput.value.trim()) + '".'
      : spells.length
      ? "All spells in spells.json are already known."
      : "No spells loaded from spells.json.";
    availableSpellsList.innerHTML = '<div class="spell-list-empty"><span aria-hidden="true">…</span><p>' + message + '</p></div>';
    return;
  }
  availableSpellsList.innerHTML = availableSpells.map((spell) => {
    return '<article class="available-spell spell-element-' + spell.element + '">' +
      '<div><strong>' + escapeHtml(spell.name) + '</strong><span>' + escapeHtml(spellElementLabel(spell.element)) + ' · ' + escapeHtml(spellTierLabel(spell.tier)) + ' · ' + escapeHtml(spell.type) + ' · ' + calculateSpellManaCost(spell) + ' MANA · ' + escapeHtml(ShopRules.formatPrice(calculateSpellPurchasePrice(spell))) + '</span></div>' +
      '<button class="available-spell-add" type="button" data-spell-id="' + escapeHtml(spell.id) + '">Add</button>' +
    '</article>';
  }).join("");
  availableSpellsList.querySelectorAll(".available-spell-add").forEach((button) => {
    button.addEventListener("click", () => {
      const spell = getSpell(button.dataset.spellId);
      if (!spell || knownSpells.includes(spell.id)) return;
      knownSpells.push(spell.id);
      renderKnownSpells();
      renderAvailableSpells();
      renderClasses();
      showSpellDetails(spell);
      setSpellbookStatus(spell.name + " added to your spell book", "success");
      saveCharacter();
    });
  });
}

function useSpell(spell) {
  if (window.Combat?.active()) { window.Combat.castFromSheet(spell); return; }
  if (!spell || spell.unavailable) return;
  if (spell.requiredCasterEnemyId || spell.requiredCasterEnemyIds?.length) {
    setSpellbookStatus(spell.name + " can only be cast by its designated creature", "error");
    return;
  }
  if ((spell.effects || []).some((effect) => ["stat-permanent-percent", "defense-permanent-percent", "summon-friendly", "stun", "next-melee-attack", "remove-random-debuff"].includes(effect.type) || ["all-enemies", "friendly-target"].includes(effect.recipient))) {
    setSpellbookStatus(spell.name + " can only be cast during combat", "error");
    return;
  }
  const spellCost = calculateSpellManaCost(spell);
  if (currentMana < spellCost) {
    setSpellbookStatus("Not enough Mana to use " + spell.name, "error");
    return;
  }
  currentMana = Math.max(0, currentMana - spellCost);
  const activeElements = effectiveSpellElements(spell);
  const consumedSkills = calculateClassEffects().skills.filter((skill) => {
    const requiredElements = [skill.requiredSpellElement, ...(skill.requiredSpellElements || [])].filter(Boolean).map(element => String(element).toLowerCase());
    const applies = !requiredElements.length || requiredElements.some(element => activeElements.includes(element));
    return applies && (skill.durationSpells || skill.durationSummonSpells && isSummonSpell(spell)) && skillState(skill).active;
  });
  consumedSkills.forEach((skill) => {
    activeSkills[classSkillKey(skill)] = { ...skillState(skill), active: false };
  });
  updateDerivedValues();
  renderKnownSpells();
  if (consumedSkills.length) {
    renderSkills();
    showSkillDetails(consumedSkills[0]);
  }
  setSpellbookStatus(spell.name + " used · " + spellCost + " Mana spent" + (consumedSkills.length ? " · " + consumedSkills.map((skill) => skill.name).join(", ") + " consumed" : ""), "success");
  saveCharacter();
}

function cacheSpellText(text) {
  try {
    localStorage.setItem(SPELL_CACHE_KEY, text);
  } catch {
    // Local storage may be unavailable in a private or restricted browser context.
  }
}

function cachedSpellText() {
  try {
    return localStorage.getItem(SPELL_CACHE_KEY) || "";
  } catch {
    return "";
  }
}

function openSpellHandleDatabase() {
  if (!("indexedDB" in window)) return Promise.resolve(null);
  return new Promise((resolve) => {
    try {
      const request = window.indexedDB.open(SPELL_HANDLE_DB, 1);
      request.onupgradeneeded = () => request.result.createObjectStore("files");
      request.onsuccess = () => resolve(request.result);
      request.onerror = () => resolve(null);
    } catch {
      resolve(null);
    }
  });
}

async function saveSpellFileHandle(handle) {
  const database = await openSpellHandleDatabase();
  if (!database) return;
  await new Promise((resolve) => {
    try {
      const transaction = database.transaction("files", "readwrite");
      transaction.objectStore("files").put(handle, "spell-source");
      transaction.oncomplete = resolve;
      transaction.onerror = resolve;
    } catch {
      resolve();
    }
  });
  database.close();
}

async function readSpellFileHandle() {
  const database = await openSpellHandleDatabase();
  if (!database) return null;
  return new Promise((resolve) => {
    try {
      const transaction = database.transaction("files", "readonly");
      const request = transaction.objectStore("files").get("spell-source");
      request.onsuccess = () => {
        const handle = request.result || null;
        database.close();
        resolve(handle);
      };
      request.onerror = () => {
        database.close();
        resolve(null);
      };
    } catch {
      database.close();
      resolve(null);
    }
  });
}

async function readFileFromHandle(handle) {
  if (!handle || typeof handle.getFile !== "function") return null;
  try {
    let permission = typeof handle.queryPermission === "function"
      ? await handle.queryPermission({ mode: "read" })
      : "granted";
    if (permission !== "granted" && typeof handle.requestPermission === "function") {
      permission = await handle.requestPermission({ mode: "read" });
    }
    if (permission !== "granted") return null;
    return await handle.getFile();
  } catch {
    return null;
  }
}

let loadedSpellText = null;
function applySpellsText(text, source = "file") {
  if (loadedSpellText === text) {
    const sourceLabel = source === "manual" ? " · selected locally" : source === "cached" ? " · saved copy" : source === "remembered" ? " · remembered file" : "";
    spellSourceStatus.textContent = spells.length + " spell" + (spells.length === 1 ? "" : "s") + " loaded from spells.json" + sourceLabel;
    spellFileButton.hidden = source === "automatic";
    return;
  }
  spells = parseSpellsFile(text, source === "cached" || source === "remembered");
  loadedSpellText = text;
  if (source !== "cached") cacheSpellText(text);
  const availableIds = new Set(spells.map((spell) => spell.id));
  const previousCount = knownSpells.length + companions.reduce((total, companion) => total + companion.character.knownSpells.length, 0) + recruitOffers.reduce((total, companion) => total + companion.character.knownSpells.length, 0);
  knownSpells = knownSpells.filter((spellIdValue) => availableIds.has(spellIdValue));
  [...companions, ...recruitOffers].forEach((companion) => {
    companion.character.knownSpells = companion.character.knownSpells.filter((spellIdValue) => availableIds.has(spellIdValue));
    if (companion.character.pendingClassSpellChoice) companion.character.pendingClassSpellChoice = null;
  });
  if (!spells.length) {
    pendingClassSpellChoice = null;
    classSpellChoiceModal.hidden = true;
    for (const stock of Object.values(shopState.locations || {})) stock.spells = [];
    updateModalState();
  }
  const remainingCount = knownSpells.length + companions.reduce((total, companion) => total + companion.character.knownSpells.length, 0) + recruitOffers.reduce((total, companion) => total + companion.character.knownSpells.length, 0);
  if (previousCount !== remainingCount || !spells.length) saveCharacter();
  const sourceLabel = source === "manual" ? " · selected locally" : source === "cached" ? " · saved copy" : source === "remembered" ? " · remembered file" : "";
  spellSourceStatus.textContent = spells.length + " spell" + (spells.length === 1 ? "" : "s") + " loaded from spells.json" + sourceLabel;
  spellFileButton.hidden = source === "automatic";
  renderKnownSpells();
  renderAvailableSpells();
  renderClasses();
  rebuildItems();
  resumePendingClassSpellChoice();
  window.Shop?.onCatalogChange();
  setSpellbookStatus(spells.length ? "Spell catalogue ready" : "The spell catalogue is empty", "success");
  window.Companions?.render();
}

function loadCachedSpells() {
  const text = cachedSpellText();
  if (text) applySpellsText(text, "cached");
}

async function loadRememberedSpellFile() {
  const handle = await readSpellFileHandle();
  const file = await readFileFromHandle(handle);
  if (!file) return false;
  applySpellsText(await file.text(), "remembered");
  return true;
}

async function loadSpells() {
  spellSourceStatus.textContent = "Reading spells.json…";
  try {
    const response = await fetch(SPELL_FILE, { cache: "no-cache" });
    if (!response.ok) throw new Error("Could not read spells.json");
    applySpellsText(await response.text(), "automatic");
  } catch (error) {
    if (await loadRememberedSpellFile()) return;
    if (spells.length) {
      spellSourceStatus.textContent = spells.length + " spell" + (spells.length === 1 ? "" : "s") + " loaded from saved spells.json copy";
      spellFileButton.hidden = false;
      renderKnownSpells();
      renderAvailableSpells();
      setSpellbookStatus("Using the saved spell catalogue", "success");
      return;
    }
    spells = [];
    spellSourceStatus.textContent = "Could not read spells.json";
    renderKnownSpells();
    renderAvailableSpells();
    spellFileButton.hidden = false;
    setSpellbookStatus("Choose spells.json to load the spell catalogue in this browser", "error");
  }
}

function normalizeClassDefinition(raw) {
  if (!raw || typeof raw !== "object" || !raw.name) return null;
  const requirements = raw.requirements && typeof raw.requirements === "object" ? raw.requirements : {};
  const classRequirements = requirements.classes && typeof requirements.classes === "object" ? requirements.classes : {};
  const anyClassRequirements = requirements.anyClasses && typeof requirements.anyClasses === "object" ? requirements.anyClasses : {};
  const levels = Array.isArray(raw.levels) ? raw.levels.map((levelDefinition, index) => ({
    level: Math.max(1, toInteger(levelDefinition?.level, index + 1)),
    description: String(levelDefinition?.description || ""),
    effects: Array.isArray(levelDefinition?.effects) ? levelDefinition.effects.filter((effect) => effect && typeof effect === "object") : [],
  })).sort((a, b) => a.level - b.level) : [];
  return {
    id: slugify(raw.id || raw.name),
    name: String(raw.name),
    color: ["blue", "purple", "gold"].includes(String(raw.color || "").toLowerCase()) ? String(raw.color).toLowerCase() : "",
    requirements: {
      level: Math.max(1, toInteger(requirements.level, 1)),
      classes: Object.fromEntries(Object.entries(classRequirements).map(([classId, requiredLevel]) => [slugify(classId), Math.max(1, toInteger(requiredLevel, 1))])),
      anyClasses: Object.fromEntries(Object.entries(anyClassRequirements).map(([classId, requiredLevel]) => [slugify(classId), Math.max(1, toInteger(requiredLevel, 1))])),
      excludes: Array.isArray(requirements.excludes) ? requirements.excludes.filter(id => typeof id === "string").map(slugify) : [],
      other: requirements.other ?? null,
    },
    maxLevel: Math.max(0, toInteger(raw.maxLevel, levels.length)),
    levels,
  };
}

function getClass(classId) {
  return classCatalog.find((classDefinition) => classDefinition.id === classId);
}

function currentClassLevel(classDefinition) {
  return Math.min(classDefinition.maxLevel, Math.max(0, toInteger(classLevels[classDefinition.id], 0)));
}

function classCacheText(classId) {
  try {
    return localStorage.getItem(CLASS_CACHE_KEYS[classId] || `emberfall-${classId}-cache`) || "";
  } catch {
    return "";
  }
}

const loadedClassText = new Map();

function refreshClassViews() {
  renderClasses();
  renderSkills();
  renderPassives();
  updateDerivedValues();
  resumePendingClassSpellChoice();
  window.Companions?.render();
}

function applyClassText(classFile, text, source = "file") {
  try {
    if (loadedClassText.get(classFile.id) === text) {
      classSourceState = source;
      return "unchanged";
    }
    const classDefinition = normalizeClassDefinition(JSON.parse(text));
    if (!classDefinition) throw new Error("Invalid class definition");
    const previousIndex = classCatalog.findIndex((knownClass) => knownClass.id === classDefinition.id);
    if (previousIndex < 0) classCatalog.push(classDefinition);
    else classCatalog[previousIndex] = classDefinition;
    loadedClassText.set(classFile.id, text);
    classSourceState = source;
    if (source !== "cached") {
      try {
        localStorage.setItem(CLASS_CACHE_KEYS[classFile.id] || `emberfall-${classFile.id}-cache`, text);
      } catch {
        // Local storage may be unavailable in a private or restricted browser context.
      }
    }
    return "changed";
  } catch {
    return false;
  }
}

let classLoadPromise = null;
async function loadClasses() {
  if (classLoadPromise) return classLoadPromise;
  classLoadPromise = (async () => {
    let changed = false;
    await Promise.all(CLASS_FILES.map(async (classFile) => {
      try {
        const response = await fetch(classFile.path, { cache: "no-cache" });
        if (!response.ok) throw new Error("Could not read " + classFile.label);
        const result = applyClassText(classFile, await response.text(), "automatic");
        if (!result) throw new Error("Invalid class definition");
        if (result === "changed") changed = true;
      } catch {
        const cached = classCacheText(classFile.id);
        const result = cached && applyClassText(classFile, cached, "cached");
        if (result === "changed") changed = true;
        if (!result) classSourceState = "error";
      }
    }));
    if (changed) refreshClassViews();
    else if (!classCatalog.length) renderClasses();
  })();
  try { await classLoadPromise; } finally { classLoadPromise = null; }
}

function loadCachedClasses() {
  let changed = false;
  CLASS_FILES.forEach((classFile) => {
    const cached = classCacheText(classFile.id);
    if (cached && applyClassText(classFile, cached, "cached") === "changed") changed = true;
  });
  if (changed) refreshClassViews();
}

function normalizeRaceDefinition(raw, index) {
  const invalid = (message) => { throw new Error(`Race ${index + 1}: ${message}`); };
  if (!raw || typeof raw !== "object" || Array.isArray(raw)) invalid("expected a race object");
  if (typeof raw.name !== "string" || !raw.name.trim()) invalid("name must be a nonempty string");
  const id = slugify(raw.id || raw.name);
  if (!id) invalid("id must be a nonempty string");
  const modifiers = raw.statModifiers && typeof raw.statModifiers === "object" ? raw.statModifiers : {};
  const statModifiers = Object.fromEntries(statDefinitions.map(({ key }) => {
    const amount = modifiers[key] ?? 0;
    if (typeof amount !== "number" || !Number.isFinite(amount)) invalid(`${key} modifier must be a number`);
    return [key, amount];
  }));
  if (!raw.passive || typeof raw.passive !== "object" || typeof raw.passive.name !== "string") invalid("passive must be an object with a name");
  const passive = {
    id: slugify(raw.passive.id || raw.passive.name),
    name: raw.passive.name.trim(),
    condition: String(raw.passive.condition || "Always active"),
    description: String(raw.passive.description || ""),
    effects: Array.isArray(raw.passive.effects) ? raw.passive.effects.filter((effect) => effect && typeof effect === "object") : [],
  };
  return {
    id,
    name: raw.name.trim(),
    playable: raw.playable !== false,
    icon: typeof raw.icon === "string" && raw.icon.trim() ? raw.icon.trim() : null,
    statModifiers,
    passive,
  };
}

function raceAccent(raceId) {
  return ({ human: "#c7683f", elf: "#728577", "dark-elf": "#77607e", dwarf: "#8b684e", hobbit: "#858b6c", lizardman: "#66806a", goblin: "#76904f", impid: "#a14f61", merfolk: "#4c9a8a" })[raceId] || "#c7683f";
}

function setRaceStatus(message, state = "") {
  raceStatus.textContent = message;
  raceStatus.className = `spellbook-status race-status${state ? ` is-${state}` : ""}`;
}

function updateRaceHero() {
  const race = getSelectedRace();
  const fallback = raceSelectButton.querySelector(".hero-sigil-fallback");
  const hasIcon = Boolean(race?.icon);
  const raceIsLocked = Boolean(selectedRaceId) && !editingEnabled;
  const needsLook = needsAppearanceChoice();
  raceSelectButton.classList.toggle("needs-race", !selectedRaceId || needsLook);
  raceSelectButton.disabled = raceIsLocked && !needsLook;
  raceSelectButton.title = race
    ? needsLook ? `${race.name} · Choose your look` : raceIsLocked ? `${race.name} · Race locked` : `${race.name} · Change race`
    : selectedRaceId ? raceIsLocked ? "Race locked" : "Change race" : "Choose a race";
  raceSelectButton.setAttribute("aria-label", raceSelectButton.title);
  heroRaceName.textContent = race?.name || (selectedRaceId ? "Unknown race" : "Choose race");
  raceSelectButton.style.setProperty("--race-accent", race ? raceAccent(race.id) : "var(--ember)");
  raceIcon.hidden = !hasIcon;
  fallback.hidden = hasIcon;
  if (hasIcon) raceIcon.src = race.icon;
  else raceIcon.removeAttribute("src");
}

function renderRaces() {
  if (!raceCatalog.length) {
    raceList.innerHTML = '<div class="spell-list-empty"><span aria-hidden="true">✦</span><p>No races loaded.<br />Refresh races.json to try again.</p></div>';
    return;
  }
  const visibleRaces = raceCatalog.filter((race) => race.playable || editingEnabled);
  raceList.innerHTML = visibleRaces.map((race) => {
    const selected = race.id === selectedRaceId;
    const locked = !race.playable && !editingEnabled;
    const icon = race.icon
      ? '<img src="' + escapeHtml(race.icon) + '" alt="" />'
      : '<span aria-hidden="true">✦</span>';
    const modifiers = statDefinitions.map(({ key, short }) => {
      const amount = toNumber(race.statModifiers[key], 0);
      return '<span><b>' + short + '</b> ' + (amount >= 0 ? "+" : "") + formatValue(amount) + '</span>';
    }).join("");
    const typeLabel = race.playable ? "Playable" : locked ? "Non-playable · Dev Mode required" : "Non-playable · Dev Mode";
    return '<button class="race-option' + (selected ? " is-selected" : "") + '" style="--race-accent:' + raceAccent(race.id) + '" type="button" data-race-id="' + escapeHtml(race.id) + '"' + (locked ? " disabled" : "") + '>' +
      '<span class="race-option-heading"><span class="race-option-icon">' + icon + '</span><span class="race-option-title"><strong>' + escapeHtml(race.name) + '</strong><span>' + escapeHtml(selected ? "Selected" : typeLabel) + '</span></span></span>' +
      '<span class="race-stat-changes">' + modifiers + '</span>' +
      '<span class="race-passive-preview"><strong>' + escapeHtml(race.passive.name) + ':</strong> ' + escapeHtml(race.passive.description) + '</span>' +
    '</button>';
  }).join("");
  raceList.querySelectorAll("[data-race-id]").forEach((button) => {
    button.addEventListener("click", () => {
      const raceId = button.dataset.raceId;
      const race = raceCatalog.find((entry) => entry.id === raceId);
      const changeAllowed = !selectedRaceId || selectedRaceId === raceId || editingEnabled;
      if (race?.playable && changeAllowed && window.Sprites?.hasPlayableLooks(raceId)) openAppearanceStep(raceId);
      else selectRace(raceId);
    });
  });
}

function raceDisallowsItem(item) {
  if (!item) return false;
  return calculateRaceEffects().equipmentRestrictions.some((restriction) =>
    (!restriction.category || restriction.category === item.category) &&
    (!restriction.type || restriction.type === item.type)
  );
}

function enforceRaceEquipmentRestrictions() {
  let changed = false;
  equipmentSlotDefinitions.forEach(({ key }) => {
    const item = getItem(equipment[key]);
    if (!item || !raceDisallowsItem(item)) return;
    equipment[key] = null;
    changed = true;
  });
  return changed;
}

function selectRace(raceId) {
  const race = raceCatalog.find((entry) => entry.id === raceId);
  if (!race || (!race.playable && !editingEnabled)) {
    setRaceStatus("Enable Dev Mode to select a non-playable race", "error");
    return;
  }
  if (selectedRaceId && selectedRaceId !== race.id && !editingEnabled) {
    setRaceStatus("A chosen race can only be changed in Dev Mode", "error");
    return;
  }
  selectedRaceId = race.id;
  const removedRestrictedEquipment = enforceRaceEquipmentRestrictions();
  createStatCards();
  const activeValues = currentStats();
  currentHp = calculateMaxHpWithArtifacts(activeValues);
  currentMana = calculateMaxManaWithEffects(activeValues);
  updateRaceHero();
  renderRaces();
  renderPassives();
  renderClasses();
  renderEquipmentSlots();
  renderOwnedItems();
  renderAvailableItems();
  renderKnownSpells();
  renderAvailableSpells();
  updateDerivedValues();
  setRaceStatus(`${race.name} selected${removedRestrictedEquipment ? " · Restricted equipment removed" : ""}`, "success");
  raceModal.hidden = true;
  updateModalState();
  saveCharacter();
}

/* ---------- Appearance: the battle sprite chosen with the race ---------- */
const raceAppearance = document.querySelector("#race-appearance");
const raceAppearanceOptions = document.querySelector("#race-appearance-options");
const raceAppearanceConfirm = document.querySelector("#race-appearance-confirm");
const raceIntro = document.querySelector(".race-intro");
let appearanceRaceId = null;
let appearanceChoice = null;

function needsAppearanceChoice() {
  return Boolean(selectedRaceId) && !appearance && Boolean(window.Sprites?.hasPlayableLooks(selectedRaceId));
}

function openAppearanceStep(raceId) {
  appearanceRaceId = raceId;
  appearanceChoice = raceId === selectedRaceId && appearance ? { ...appearance } : { gender: "male", variant: 0 };
  raceList.hidden = true;
  if (raceIntro) raceIntro.hidden = true;
  raceAppearance.hidden = false;
  renderAppearanceOptions();
  raceAppearanceOptions.querySelector('[aria-checked="true"]')?.focus();
}

function closeAppearanceStep() {
  appearanceRaceId = null;
  raceAppearance.hidden = true;
  raceList.hidden = false;
  if (raceIntro) raceIntro.hidden = false;
}

function renderAppearanceOptions() {
  const race = raceCatalog.find((entry) => entry.id === appearanceRaceId);
  if (!race || !window.Sprites) return;
  document.querySelector("#race-appearance-eyebrow").textContent = race.name.toUpperCase() + " · APPEARANCE";
  const equipped = { ...equipment };
  const groups = ["male", "female"].map((gender) => {
    const options = [0, 1, 2, 3].map((variant) => {
      const chosen = appearanceChoice.gender === gender && appearanceChoice.variant === variant;
      const label = window.Sprites.lookLabel(race.id, gender, variant);
      return `<button class="race-look${chosen ? " is-selected" : ""}" type="button" role="radio" aria-checked="${chosen}" data-look-gender="${gender}" data-look-variant="${variant}" aria-label="${escapeHtml(label)}, ${gender}"><span class="race-look-figure">${window.Sprites.playablePreview(race.id, gender, variant, equipped)}</span><span class="race-look-label">${escapeHtml(label)}</span></button>`;
    }).join("");
    return `<div class="race-look-group" role="radiogroup" aria-label="${gender === "male" ? "Male" : "Female"} looks"><p class="eyebrow">${gender === "male" ? "MALE" : "FEMALE"}</p><div class="race-look-row">${options}</div></div>`;
  }).join("");
  raceAppearanceOptions.innerHTML = groups;
  const label = window.Sprites.lookLabel(race.id, appearanceChoice.gender, appearanceChoice.variant);
  document.querySelector("#race-appearance-summary").textContent = `${race.name} · ${appearanceChoice.gender === "female" ? "Female" : "Male"} · ${label}`;
  raceAppearanceConfirm.textContent = race.id === selectedRaceId ? "Use this look" : `Begin as ${race.name}`;
}

raceAppearanceOptions.addEventListener("click", (event) => {
  const option = event.target.closest("[data-look-gender]");
  if (!option) return;
  appearanceChoice = { gender: option.dataset.lookGender, variant: Number(option.dataset.lookVariant) };
  renderAppearanceOptions();
  raceAppearanceOptions.querySelector('[aria-checked="true"]')?.focus();
});
raceAppearanceOptions.addEventListener("keydown", (event) => {
  if (!["ArrowRight", "ArrowLeft", "ArrowDown", "ArrowUp"].includes(event.key)) return;
  event.preventDefault();
  const options = [...raceAppearanceOptions.querySelectorAll("[data-look-gender]")];
  const index = options.findIndex((option) => option.getAttribute("aria-checked") === "true");
  const step = ["ArrowRight", "ArrowDown"].includes(event.key) ? 1 : -1;
  options[(index + step + options.length) % options.length]?.click();
});
document.querySelector("#race-appearance-back").addEventListener("click", () => {
  closeAppearanceStep();
  raceList.querySelector(".race-option")?.focus();
});
raceAppearanceConfirm.addEventListener("click", () => {
  const raceId = appearanceRaceId, look = appearanceChoice;
  if (!raceId || !look) return;
  if (raceId !== selectedRaceId) {
    selectRace(raceId);
    if (selectedRaceId !== raceId) return;
  }
  appearance = { gender: look.gender, variant: look.variant };
  closeAppearanceStep();
  raceModal.hidden = true;
  updateModalState();
  updateRaceHero();
  window.Companions?.render();
  setRaceStatus(`${window.Sprites?.lookLabel(raceId, look.gender, look.variant) || "Look"} chosen`, "success");
  saveCharacter();
});

let loadedRaceText = null;
function applyRaceText(text, source = "file") {
  if (loadedRaceText === text) {
    raceSourceState = source;
    raceSourceStatus.textContent = `${raceCatalog.length} races loaded${source === "cached" ? " · saved copy" : ""}`;
    return;
  }
  const records = JSON.parse(text);
  if (!Array.isArray(records)) throw new Error("The race catalogue must be a JSON array");
  const normalized = records.map(normalizeRaceDefinition);
  const ids = new Set();
  normalized.forEach((race) => {
    if (ids.has(race.id)) throw new Error(`Duplicate race id: ${race.id}`);
    ids.add(race.id);
  });
  raceCatalog = normalized;
  loadedRaceText = text;
  raceSourceState = source;
  if (source !== "cached") {
    try {
      localStorage.setItem(RACE_CACHE_KEY, text);
    } catch {
      // Local storage may be unavailable in a private or restricted browser context.
    }
  }
  raceSourceStatus.textContent = `${raceCatalog.length} races loaded${source === "cached" ? " · saved copy" : ""}`;
  const equipmentChanged = enforceRaceEquipmentRestrictions();
  updateRaceHero();
  createStatCards();
  renderRaces();
  renderPassives();
  renderClasses();
  renderEquipmentSlots();
  renderOwnedItems();
  renderAvailableItems();
  renderKnownSpells();
  renderAvailableSpells();
  updateDerivedValues();
  window.Companions?.render();
  if (equipmentChanged) saveCharacter();
}

function loadCachedRaces() {
  try {
    const cached = localStorage.getItem(RACE_CACHE_KEY);
    if (cached) applyRaceText(cached, "cached");
  } catch {
    // Automatic loading will still be attempted.
  }
}

async function loadRaces() {
  raceSourceStatus.textContent = "Reading races.json…";
  try {
    const response = await fetch(RACE_FILE, { cache: "no-cache" });
    if (!response.ok) throw new Error("Could not read races.json");
    applyRaceText(await response.text(), "automatic");
    setRaceStatus("Race catalogue ready", "success");
  } catch (error) {
    raceSourceState = "error";
    raceSourceStatus.textContent = raceCatalog.length ? `${raceCatalog.length} races loaded from saved copy` : "Could not read races.json";
    renderRaces();
    updateRaceHero();
    updateDerivedValues();
    setRaceStatus(raceCatalog.length ? "Using the saved race catalogue" : "Open the desktop shortcut, then refresh the race file", raceCatalog.length ? "success" : "error");
  }
}

function itemId(name, category, type) {
  return [category, slugify(name), slugify(type)].join("-");
}

function splitDelimitedRecord(record) {
  const fields = [];
  let field = "";
  let quoted = false;
  for (let index = 0; index < record.length; index += 1) {
    const character = record[index];
    if (character === '"') {
      if (quoted && record[index + 1] === '"') {
        field += '"';
        index += 1;
      } else {
        quoted = !quoted;
      }
    } else if (character === "," && !quoted) {
      fields.push(field.trim());
      field = "";
    } else {
      field += character;
    }
  }
  fields.push(field.trim());
  return fields;
}

function normalizeRarity(value) {
  const rarity = slugify(value);
  return ["common", "uncommon", "rare", "epic", "legendary", "masterwork", "exotic"].includes(rarity) ? rarity : "common";
}

function parseWeaponRecord(record) {
  const fields = splitDelimitedRecord(record.trim());
  if (fields.length < 7) return null;
  const name = fields[0].trim();
  const type = fields[1].trim().toLowerCase();
  const hasBasePower = fields.length >= 8;
  const primaryIndex = hasBasePower ? 4 : 3;
  const secondaryIndex = hasBasePower ? 5 : 4;
  const descriptionIndex = hasBasePower ? 6 : 5;
  const rarityIndex = hasBasePower ? 7 : 6;
  return {
    id: itemId(name, "weapon", type),
    category: "weapon",
    name: name.charAt(0).toUpperCase() + name.slice(1),
    type,
    handed: toInteger(fields[2], 1) === 2 ? 2 : 1,
    basePower: hasBasePower ? toNumber(fields[3], 0) : 0,
    primary: parseSpellComponent(fields[primaryIndex]),
    secondary: parseSpellComponent(fields[secondaryIndex]),
    description: fields[descriptionIndex].trim(),
    rarity: normalizeRarity(fields[rarityIndex]),
  };
}

function parseArmorRecord(record) {
  const fields = splitDelimitedRecord(record.trim());
  if (fields.length < 7) return null;
  const name = fields[0].trim();
  const type = fields[1].trim().toLowerCase();
  return {
    id: itemId(name, "armor", type),
    category: "armor",
    name: name.charAt(0).toUpperCase() + name.slice(1),
    type,
    armorValue: toNumber(fields[2], 0),
    evasionDebuff: Math.max(0, toNumber(fields[3], 0)),
    scaling: parseSpellComponent(fields[4]),
    description: fields[5].trim(),
    rarity: normalizeRarity(fields[6]),
  };
}

function parseArtifactEffects(specification) {
  const allowedKinds = new Set([
    "stat-flat", "stat-percent", "stat-permanent-percent", "max-hp-flat", "max-hp-percent", "max-mana-flat", "max-mana-percent",
    "weapon-power-flat", "weapon-power-percent", "armor-flat", "armor-percent",
    "evasion-flat", "evasion-percent", "weapon-hit-flat", "weapon-hit-percent", "weapon-crit-flat", "spell-cost-flat", "spell-cost-percent",
    "spell-power-flat", "spell-power-percent",
    "trap-limit-flat", "low-hp-turn-start-heal-percent", "low-hp-summon", "melee-hit-guard-per-rarity", "ranged-splash-percent",
    ...ARTIFACT_EFFECT_KINDS,
  ]);
  return specification.split("|").map((entry) => {
    const parts = entry.trim().split(":");
    const kind = parts[0];
    const target = parts.length === 3 ? parts[1] : null;
    const amount = toNumber(parts[parts.length - 1], NaN);
    if (!allowedKinds.has(kind) || !Number.isFinite(amount)) return null;
    if ((kind === "stat-flat" || kind === "stat-percent" || kind === "stat-permanent-percent") && !bonusDefinitions.includes(target)) return null;
    return { kind, target, amount };
  }).filter(Boolean);
}

function parseArtifactRecord(record) {
  const fields = splitDelimitedRecord(record.trim());
  if (fields.length < 3) return null;
  const name = fields[0].trim();
  return {
    id: itemId(name, "artifact", "artifact"),
    category: "artifact",
    name: name.charAt(0).toUpperCase() + name.slice(1),
    type: "artifact",
    effects: parseArtifactEffects(fields[2]),
    description: (fields[3] || fields[2]).trim(),
    rarity: normalizeRarity(fields[1]),
  };
}

function parseLegacyItemFile(kind, text) {
  const parser = kind === "weapons" ? parseWeaponRecord : kind === "armor" ? parseArmorRecord : parseArtifactRecord;
  return text.split(";").map(parser).filter(Boolean);
}

function parseItemFile(kind, text, allowLegacy = false) {
  // Old browser caches remain usable until the JSON files synchronize.
  if (allowLegacy && text.trim() && !/^[\[{]/.test(text.trim())) return parseLegacyItemFile(kind, text);
  const records = JSON.parse(text);
  if (!Array.isArray(records)) throw new Error("The catalog must be a JSON array of items");
  const category = { weapons: "weapon", armor: "armor", artifacts: "artifact" }[kind];
  if (!category) throw new Error("Unknown item catalog");
  const seen = new Set();
  return records.map((record, index) => {
    const invalid = (message) => { throw new Error(`Item ${index + 1}: ${message}`); };
    if (!record || typeof record !== "object" || Array.isArray(record)) invalid("expected an item object");
    const stringField = (key) => {
      if (typeof record[key] !== "string" || !record[key].trim()) invalid(`${key} must be a nonempty string`);
      return record[key].trim();
    };
    const numberField = (key) => {
      if (typeof record[key] !== "number" || !Number.isFinite(record[key])) invalid(`${key} must be a number`);
      return record[key];
    };
    const component = (value, field) => {
      if (value === null) return { ratio: 0, code: "N", key: null, none: true };
      if (!value || !bonusDefinitions.includes(value.stat) || typeof value.multiplier !== "number" || !Number.isFinite(value.multiplier)) {
        invalid(`${field} must be null or an object with a valid stat and numeric multiplier`);
      }
      const code = Object.keys(STAT_CODES).find((key) => STAT_CODES[key] === value.stat);
      return { ratio: value.multiplier, code, key: value.stat };
    };
    const id = stringField("id");
    if (!id.startsWith(category + "-")) invalid(`id must start with ${category}-`);
    if (seen.has(id)) invalid(`duplicate id ${id}`);
    seen.add(id);
    const name = stringField("name");
    const rarity = stringField("rarity").toLowerCase();
    if (!["common", "uncommon", "rare", "epic", "legendary", "masterwork", "exotic"].includes(rarity)) invalid("unknown rarity");
    if (typeof record.description !== "string") invalid("description must be a string");
    const priceSilver = numberField("priceSilver");
    if (!Number.isInteger(priceSilver) || priceSilver < 0) invalid("priceSilver must be a nonnegative integer");
    const item = {
      id, category, name: name.charAt(0).toUpperCase() + name.slice(1), rarity, description: record.description,
      catalogueAvailable: record.catalogueAvailable !== false,
      shopAvailable: record.shopAvailable !== false,
      playerSellable: record.playerSellable !== false,
      droppable: record.droppable !== false,
      automaticDrop: record.automaticDrop !== false,
      priceSilver
    };
    if (record.shopOffer != null) {
      const offer = record.shopOffer;
      if (!offer || typeof offer !== "object" || Array.isArray(offer) ||
          ["regionId", "settlementId", "costItemId"].some(key => typeof offer[key] !== "string" || !/^[a-z0-9]+(?:-[a-z0-9]+)*$/.test(offer[key])) ||
          !Number.isInteger(offer.costQuantity) || offer.costQuantity < 1) invalid("shopOffer needs a region, settlement, cost item and positive cost quantity");
      item.shopOffer = { regionId: offer.regionId, settlementId: offer.settlementId, costItemId: offer.costItemId, costQuantity: offer.costQuantity };
    }
    const equipmentEffects = () => {
      if (record.effects != null && !Array.isArray(record.effects)) invalid("effects must be an array");
      return (record.effects || []).map((effect) => {
        const allowedKinds = kind === "weapons" ? ["spell-cost-percent", "spell-power-percent", "spell-add-element", "damage-percent-vs-slower", "damage-percent-vs-race", "damage-and-pierce-vs-larger-max-hp", "bonus-basic-attack-percent", "golden-apex-choice", "unreducible-cleave-percent", "cleave-percent", "weapon-power-from-armor-percent", "party-mana-drain-on-hit-percent", "bonus-damage-from-armor-percent", "mark-on-hit", "all-stats-on-damage", "summon-empowerment", "offhand-armor-pierce-percent", "incoming-spell-damage-percent", "weapon-crit-percent-vs-class", "mana-retaliation"] : ["spell-cost-percent", "incoming-race-damage-percent", "incoming-spell-damage-percent", "restore-mana-on-elemental-hit-percent", "defense-cap", "evasion-flat", "all-stats-on-nonbasic-spell-damage-per-tier", "damage-charge", "armor-from-max-hp-percent", "missing-hp-spell-ward"];
        if (!effect || !allowedKinds.includes(effect.kind) || typeof effect.amount !== "number" || !Number.isFinite(effect.amount)) {
          invalid(`equipment effects support ${allowedKinds.join(" or ")} with a numeric amount`);
        }
        if (effect.maxTier != null && (!Number.isInteger(effect.maxTier) || effect.maxTier < 1)) invalid("effect maxTier must be a positive integer");
        if (effect.minTier != null && (effect.kind !== "spell-cost-percent" || !Number.isInteger(effect.minTier) || effect.minTier < 1 || effect.minTier > 10 || effect.maxTier != null && effect.minTier > effect.maxTier)) invalid("minTier must be a valid spell-cost tier from 1 to 10");
        if (effect.summonOnly != null && (effect.kind !== "spell-cost-percent" || typeof effect.summonOnly !== "boolean")) invalid("summonOnly is only supported on spell-cost effects");
        const element = effect.element == null ? null : String(effect.element).trim().toLowerCase();
        if (element && (!SPELL_ELEMENTS.includes(element) || !["spell-cost-percent", "spell-power-percent", "spell-add-element", "incoming-spell-damage-percent", "restore-mana-on-elemental-hit-percent", "mana-retaliation"].includes(effect.kind))) invalid("element is not valid for this equipment effect");
        if (effect.element != null && !element || effect.kind === "spell-add-element" && (!element || effect.amount !== 0) || effect.kind === "restore-mana-on-elemental-hit-percent" && (!element || effect.amount <= 0)) invalid("elemental equipment effect needs a valid element and amount");
        const raceIds = effect.raceIds == null ? null : Array.isArray(effect.raceIds) ? [...new Set(effect.raceIds.filter(id => typeof id === "string" && id.trim()).map(id => slugify(id)))] : [];
        const raceEffect = ["incoming-race-damage-percent", "damage-percent-vs-race"].includes(effect.kind);
        if (raceEffect && !raceIds?.length) invalid(`${effect.kind} needs at least one raceId`);
        if (!raceEffect && effect.raceIds != null) invalid("raceIds are only valid on race damage effects");
        if (effect.kind === "mana-retaliation") {
          if (!element || effect.amount <= 0 || effect.amount > 100 || !Number.isInteger(effect.manaCost) || effect.manaCost < 1) invalid("Mana retaliation needs an element, a 1–100 damage percentage and a positive Mana cost");
          return { kind: effect.kind, amount: effect.amount, element, manaCost: effect.manaCost };
        }
        if (effect.kind === "damage-charge") {
          if (effect.amount <= 0 || !Number.isInteger(effect.maximum) || effect.maximum < 1 || effect.maximum > 100 || !Number.isFinite(effect.fullBonusPercent) || effect.fullBonusPercent < effect.amount * effect.maximum || effect.fullBonusPercent > 1000) invalid("Damage charges need a positive bonus, a 1–100 stack limit and a full-stack bonus of at most 1000%");
          return { kind: effect.kind, amount: effect.amount, maximum: effect.maximum, fullBonusPercent: effect.fullBonusPercent };
        }
        if (effect.kind === "missing-hp-spell-ward") {
          if (effect.amount < 0 || effect.amount > 100 || !Number.isFinite(effect.missingHpPercentPerPoint) || effect.missingHpPercentPerPoint <= 0) invalid("Health-based spell protection needs a 0–100 percentage and positive missingHpPercentPerPoint");
          return { kind: effect.kind, amount: effect.amount, missingHpPercentPerPoint: effect.missingHpPercentPerPoint };
        }
        if (effect.kind === "weapon-crit-percent-vs-class") {
          if (effect.amount < 0 || effect.amount > 500 || !Array.isArray(effect.classIds) || !effect.classIds.length || effect.classIds.some(id => typeof id !== "string" || !id.trim())) invalid("Conditional crit effects need classIds and a percentage from 0 to 500");
          return { kind: effect.kind, amount: effect.amount, classIds: [...new Set(effect.classIds.map(slugify))] };
        }
        if (effect.kind === "damage-and-pierce-vs-larger-max-hp") {
          const growing = effect.stepPercent != null;
          if ((growing ? !(effect.stepPercent > 0) || !(effect.maxAmount > 0) : !Number.isFinite(effect.thresholdPercent) || effect.thresholdPercent < 0) || !Number.isFinite(effect.armorPiercePercent) || effect.armorPiercePercent < 0 || effect.armorPiercePercent > 100) invalid("larger-HP effects need a nonnegative thresholdPercent (or a positive stepPercent and maxAmount) and 0–100 armorPiercePercent");
          return growing ? { kind: effect.kind, amount: effect.amount, stepPercent: effect.stepPercent, maxAmount: effect.maxAmount, armorPiercePercent: effect.armorPiercePercent }
            : { kind: effect.kind, amount: effect.amount, thresholdPercent: effect.thresholdPercent, armorPiercePercent: effect.armorPiercePercent };
        }
        if (effect.kind === "bonus-basic-attack-percent") {
          if (effect.amount < 0 || !Number.isFinite(effect.armorPiercePercent) || effect.armorPiercePercent < 0 || effect.armorPiercePercent > 100) invalid("bonus Basic attack damage needs a nonnegative amount and 0–100 Armor Pierce");
          return { kind: effect.kind, amount: effect.amount, armorPiercePercent: effect.armorPiercePercent };
        }
        if (effect.kind === "golden-apex-choice" && effect.amount !== 0) invalid("Golden Apex choice must have zero amount");
        if (["mark-on-hit", "all-stats-on-nonbasic-spell-damage-per-tier", "all-stats-on-damage"].includes(effect.kind)) {
          if (effect.amount <= 0 || !Number.isInteger(effect.durationTurns) || effect.durationTurns < 1 || effect.durationTurns > 100) invalid("Timed equipment effects need a positive amount and durationTurns");
          if (effect.kind === "mark-on-hit" && (!Number.isFinite(effect.chance) || effect.chance < 0 || effect.chance > 100)) invalid("mark-on-hit needs a 0–100 chance");
          return { kind: effect.kind, amount: effect.amount, durationTurns: effect.durationTurns, ...(effect.kind === "mark-on-hit" ? { chance: effect.chance } : {}), ...(Number.isInteger(effect.maxStacks) && effect.maxStacks > 1 ? { maxStacks: effect.maxStacks } : {}) };
        }
        if (effect.kind === "summon-empowerment") {
          if ([effect.amount, effect.manaPercent, effect.damagePercent].some(amount => !Number.isFinite(amount) || amount < 0 || amount > 500)) invalid("Summon empowerment needs HP, Mana and damage percentages from 0 to 500");
          return { kind: effect.kind, amount: effect.amount, manaPercent: effect.manaPercent, damagePercent: effect.damagePercent };
        }
        if (effect.kind === "offhand-armor-pierce-percent" && (effect.amount < 0 || effect.amount > 100)) invalid("Off-hand Armor Pierce must be from 0 to 100");
        if (effect.kind === "defense-cap") {
          if (!defenseBonusDefinitions.includes(effect.target) || effect.amount < 0) invalid("defense-cap needs a valid defense target and nonnegative amount");
          return { kind: effect.kind, target: effect.target, amount: effect.amount };
        }
        if (["unreducible-cleave-percent", "cleave-percent", "weapon-power-from-armor-percent", "armor-from-max-hp-percent", "party-mana-drain-on-hit-percent", "bonus-damage-from-armor-percent"].includes(effect.kind) && (effect.amount < 0 || effect.amount > 100)) invalid("equipment percentages must be from 0 to 100");
        return { kind: effect.kind, amount: effect.amount, maxTier: effect.maxTier ?? null, ...(effect.minTier != null ? { minTier: effect.minTier } : {}), ...(effect.summonOnly != null ? { summonOnly: effect.summonOnly } : {}), ...(raceIds ? { raceIds } : {}), ...(element ? { element } : {}) };
      });
    };
    if (kind === "weapons") {
      const handed = numberField("handed");
      if (handed !== 1 && handed !== 2) invalid("handed must be 1 or 2");
      if (record.minimumStrength != null && (!Number.isInteger(record.minimumStrength) || record.minimumStrength < 0)) invalid("minimumStrength must be a nonnegative integer");
      return { ...item, type: stringField("type").toLowerCase(), handed, basePower: numberField("basePower"),
        primary: component(record.primary, "primary"), secondary: component(record.secondary, "secondary"), minimumStrength: record.minimumStrength || 0, effects: equipmentEffects() };
    }
    if (kind === "armor") {
      const type = stringField("type").toLowerCase();
      if (!["heavy", "light", "clothing", "shield"].includes(type)) invalid("unknown armor type");
      const evasionDebuff = numberField("evasionDebuff");
      if (evasionDebuff < 0) invalid("evasionDebuff must not be negative");
      const effects = equipmentEffects();
      return { ...item, type, armorValue: numberField("armorValue"), evasionDebuff, scaling: component(record.scaling, "scaling"), effects };
    }
    if (!Array.isArray(record.effects)) invalid("effects must be an array");
    const effects = record.effects.map((effect) => {
      if (!effect || typeof effect.kind !== "string" || typeof effect.amount !== "number" || !Number.isFinite(effect.amount)) {
        invalid("each effect needs a kind and numeric amount");
      }
      const targeted = ["stat-flat", "stat-percent", "stat-permanent-percent"].includes(effect.kind);
      if (targeted && !bonusDefinitions.includes(effect.target)) invalid("stat effect needs a valid target");
      const specification = effect.kind + ":" + (targeted ? effect.target + ":" : "") + effect.amount;
      const parsed = parseArtifactEffects(specification);
      if (parsed.length !== 1 || parsed[0].kind !== effect.kind) invalid(`unknown effect kind ${effect.kind}`);
      if (effect.kind === "low-hp-turn-start-heal-percent" && (!Number.isFinite(effect.thresholdPercent) || effect.thresholdPercent <= 0 || effect.thresholdPercent > 100 || effect.amount <= 0)) invalid("low-HP healing requires a positive thresholdPercent up to 100 and positive healing");
      if (effect.kind === "low-hp-summon" && (!Number.isFinite(effect.thresholdPercent) || effect.thresholdPercent <= 0 || effect.thresholdPercent > 100 || !Number.isInteger(effect.amount) || effect.amount < 1 || effect.amount > 5 || typeof effect.enemyId !== "string" || !/^[a-z0-9]+(?:-[a-z0-9]+)*$/.test(effect.enemyId))) invalid("Low-HP summoning needs a valid enemyId, a 1–100 HP threshold and 1–5 summons");
      if (effect.kind === "melee-hit-guard-per-rarity" && (effect.amount <= 0 || effect.amount > 100)) invalid("melee-hit protection requires a percentage above 0 and up to 100");
      if (effect.kind === "ranged-splash-percent" && (effect.amount < 0 || effect.amount > 100)) invalid("Ranged splash damage must be from 0 to 100");
      const weaponEffect = ["weapon-power-flat", "weapon-power-percent", "weapon-hit-flat", "weapon-hit-percent", "weapon-crit-flat"].includes(effect.kind);
      const spellEffect = ["spell-cost-flat", "spell-cost-percent", "spell-power-flat", "spell-power-percent"].includes(effect.kind);
      const normalized = parsed[0];
      if (effect.weaponType != null) {
        const weaponType = typeof effect.weaponType === "string" ? effect.weaponType.trim().toLowerCase() : "";
        if (!weaponEffect || !["melee", "meele", "ranged", "bow", "magic-wand", "spell-staff"].includes(weaponType)) invalid("weaponType is only valid on weapon effects and must name a supported weapon type");
        normalized.weaponType = weaponType === "meele" ? "melee" : weaponType;
      }
      const extraFields = ARTIFACT_EFFECT_FIELDS[effect.kind] || {};
      if (effect.element != null) {
        const element = typeof effect.element === "string" ? effect.element.trim().toLowerCase() : "";
        if (!spellEffect && !extraFields.element || !SPELL_ELEMENTS.includes(element)) invalid("element is only valid on spell effects and must name a supported spell element");
        normalized.element = element;
      }
      for (const [field, type] of Object.entries(extraFields)) {
        if (field === "element") { if (effect.element == null) invalid(`${effect.kind} needs an element`); continue; }
        const value = effect[field];
        if (type === "races") {
          if (!Array.isArray(value) || !value.length || value.some(race => typeof race !== "string" || !race.trim())) invalid(`${effect.kind} needs raceIds`);
          normalized.raceIds = [...new Set(value.map(race => slugify(race)))];
        } else if (value != null || type !== "number") {
          const valid = Number.isFinite(value) && (type === "percent" ? value > 0 && value <= 100 : type === "turns" || type === "count" ? Number.isInteger(value) && value >= 1 && value <= 20 : true);
          if (!valid) invalid(`${effect.kind} needs a valid ${field}`);
          normalized[field] = value;
        }
      }
      // A masterwork artifact's numbers along the level ramp: { field: [value at Level 1, value at Level 80] } (A5–A7).
      if (effect.ramp != null) {
        if (rarity !== "masterwork" || typeof effect.ramp !== "object" || Array.isArray(effect.ramp)) invalid("only masterwork artifacts have level ramps");
        normalized.ramp = {};
        for (const [field, pair] of Object.entries(effect.ramp)) {
          if (!["amount", "strengthPercent"].includes(field) || !Array.isArray(pair) || pair.length !== 2 || !pair.every(Number.isFinite)) invalid("a ramp gives a field's values at Levels 1 and 80");
          normalized.ramp[field] = [...pair];
        }
      }
      if (effect.rampDecimals != null) {
        if (!normalized.ramp || ![1, 2].includes(effect.rampDecimals)) invalid("rampDecimals (1 or 2) goes with a level ramp");
        normalized.rampDecimals = effect.rampDecimals;
      }
      if (effect.maxTier != null) {
        if (!spellEffect || !Number.isInteger(effect.maxTier) || effect.maxTier < 1 || effect.maxTier > 10) invalid("maxTier is only valid on spell effects and must be an integer from 1 to 10");
        normalized.maxTier = effect.maxTier;
      }
      if (effect.kind === "low-hp-turn-start-heal-percent") normalized.thresholdPercent = effect.thresholdPercent;
      if (effect.kind === "low-hp-summon") { normalized.thresholdPercent = effect.thresholdPercent; normalized.enemyId = effect.enemyId; }
      return normalized;
    });
    // Placeholder looks (A1, A2): an object and its detail, drawn in the rarity's material (forge-art.js).
    const look = record.look == null ? null : record.look;
    if (look && (typeof look.shape !== "string" || !look.shape.trim() || typeof look.glyph !== "string" || !look.glyph.trim())) invalid("look needs a shape and a glyph");
    if (record.priceRamp != null && (rarity !== "masterwork" || !Array.isArray(record.priceRamp) || record.priceRamp.length !== 2 || !record.priceRamp.every(value => Number.isInteger(value) && value >= 0))) invalid("priceRamp gives a masterwork artifact's price at Levels 1 and 80");
    const artifact = { ...item, type: "artifact", effects, look: look ? { shape: look.shape.trim(), glyph: look.glyph.trim() } : null, placeholderArt: record.placeholderArt === true };
    if (record.priceRamp != null) artifact.priceRamp = [...record.priceRamp];
    // A masterwork artifact without a level suffix is a Level 80 copy (old saves, A11; catalogue entries).
    return artifact.priceRamp || effects.some(effect => effect.ramp) ? masterworkArtifactAt(artifact, MASTERWORK_ARTIFACT_BASE_LEVEL) : artifact;
  });
}

function masterworkArtifactAt(base, level) {
  const effects = base.effects.map(effect => {
    if (!effect.ramp) return effect;
    const ramped = { ...effect };
    for (const [field, pair] of Object.entries(effect.ramp)) ramped[field] = rampedValue(pair, level, effect.rampDecimals || 1);
    return ramped;
  });
  // A two-decimal ramp shows both decimals (Treasure Seeker's 0.15 Luck per Platinum).
  const shown = effect => effect?.rampDecimals === 2 ? String(Math.round(toNumber(effect.amount, 0) * 100) / 100) : formatValue(effect?.amount);
  const fill = text => String(text || "").replace(/\{amount\}/g, shown(effects[0])).replace(/\{amount2\}/g, shown(effects[1]))
    .replace(/\{strength\}/g, formatValue(effects[0]?.strengthPercent));
  const template = base.descriptionTemplate || base.description;
  return {
    ...base, id: level === MASTERWORK_ARTIFACT_BASE_LEVEL && !base.masterworkArtifactBaseId ? base.id : `${base.masterworkArtifactBaseId || base.id}-lv${level}`,
    masterworkArtifactBaseId: base.masterworkArtifactBaseId || base.id, level, effects, descriptionTemplate: template, description: fill(template),
    priceSilver: base.priceRamp ? Math.max(0, Math.round(rampedValue(base.priceRamp, level) / 100) * 100) : base.priceSilver,
  };
}
function leveledMasterworkArtifact(itemIdValue) {
  const match = masterworkArtifactLevelPattern.exec(String(itemIdValue || ""));
  if (!match) return null;
  const base = itemsById.get(match[1]), level = Number(match[2]);
  if (!base || base.category !== "artifact" || !base.masterworkArtifactBaseId || level < 1 || level > MASTERWORK_ARTIFACT_MAX_LEVEL) return null;
  const key = itemIdValue + "|" + base.description;
  if (!leveledArtifactCache.has(key)) leveledArtifactCache.set(key, masterworkArtifactAt(base, level));
  return leveledArtifactCache.get(key);
}
// The masterwork artifact a drop or a reward gives at this level.
function masterworkArtifactIdAt(itemIdValue, atLevel) {
  const base = itemsById.get(itemIdValue);
  if (!base?.masterworkArtifactBaseId) return itemIdValue;
  return `${base.masterworkArtifactBaseId}-lv${Math.max(1, Math.min(MASTERWORK_ARTIFACT_MAX_LEVEL, toInteger(atLevel, 1)))}`;
}

function rebuildItems() {
  const catalogueById = new Map();
  const archivedItems = spells.map(spell => ({
    id: `consumable-archived-${spell.id}`,
    name: `Archived: ${spell.name}`,
    description: `Consume to cast ${spell.name} (Tier ${spell.tier}) for 1 AP without spending Mana.`,
    rarity: "rare", category: "consumable", type: "consumable", archivedSpellId: spell.id,
    priceSilver: 1500 + Math.max(1, toInteger(spell.tier, 1)) * 250,
    shopAvailable: false, catalogueAvailable: false,
    effects: [{ type: "cast-archived-spell", amount: 0 }],
  }));
  // Forged masterworks join the catalogue as templates, so drop tables and guild information can name them.
  // The Exotics' templates too (task 7): drop tables name them like masterworks.
  const masterworkTemplates = window.ForgeRules ? [...ForgeRules.MASTERWORK_IDS, ...ForgeRules.EXOTIC_IDS].map(key => ForgeRules.item(ForgeRules.masterworkTemplateId(key))).filter(Boolean) : [];
  items = [...itemCatalog.weapons, ...itemCatalog.armor, ...itemCatalog.artifacts, ...(itemCatalog.consumables || []), ...(itemCatalog.keyItems || []), ...archivedItems, ...masterworkTemplates].filter((item) => {
    if (catalogueById.has(item.id)) return false;
    catalogueById.set(item.id, item);
    return true;
  });
  // Rebuild before any views or equipment calculations read the new catalogue.
  // Keep first-ID-wins semantics, including archived spells and local JSON edits.
  itemsById = catalogueById;
  enforceRaceEquipmentRestrictions();
  renderEquipmentSlots();
  renderOwnedItems();
  renderAvailableItems();
  renderKnownSpells();
  window.Shop?.onCatalogChange();
  renderAvailableSpells();
  updateDerivedValues();
  window.Companions?.render();
}

// A spear wielded with an empty off-hand is held in both hands and scales harder.
function forgedPrimaryBonus(item) {
  return item?.twoHandedPrimaryBonus && !equipment.offHand ? item.twoHandedPrimaryBonus : 0;
}

function calculateItemPower(item, values = currentStats()) {
  const primaryValue = item.primary ? (item.primary.none ? 0 : (item.primary.ratio + forgedPrimaryBonus(item)) * toInteger(values[item.primary.key], 0)) : 0;
  const secondaryComponent = item.secondary || item.scaling;
  const secondaryValue = secondaryComponent ? (secondaryComponent.none ? 0 : secondaryComponent.ratio * toInteger(values[secondaryComponent.key], 0)) : 0;
  let basePower = (item.basePower || 0) + primaryValue + secondaryValue;
  if (item.category === "armor") {
    const hpArmorPercent = (item.effects || []).filter(effect => effect.kind === "armor-from-max-hp-percent").reduce((sum, effect) => sum + effect.amount, 0);
    if (hpArmorPercent) basePower += Math.floor((values.maxHp ?? calculateMaxHpWithArtifacts(values)) * hpArmorPercent / 100);
  }
  if (item.category !== "weapon") return basePower;
  const armorPowerPercent = (item.effects || []).filter(effect => effect.kind === "weapon-power-from-armor-percent").reduce((sum, effect) => sum + effect.amount, 0);
  if (armorPowerPercent) basePower += calculateArmorForWeapon(values) * armorPowerPercent / 100;
  const artifactEffects = calculateArtifactEffects();
  const classEffects = calculateClassEffects();
  const itemType = normalizeWeaponType(item.type);
  basePower += classEffects.weaponScalingModifiers.reduce((total, modifier) => {
    return modifier.weaponType === "all" || modifier.weaponType === itemType
      ? total + modifier.amount * toInteger(values[modifier.stat], 0)
      : total;
  }, 0);
  const classPowerBonus = classWeaponBonus(item, classEffects, "weaponPowerFlat");
  const classPowerPercent = classWeaponBonus(item, classEffects, "weaponPowerPercent");
  const artifactPowerFlat = artifactEffects.weaponPowerFlat + artifactWeaponModifier(item, artifactEffects, "weapon-power-flat");
  const artifactPowerPercent = artifactEffects.weaponPowerPercent + artifactWeaponModifier(item, artifactEffects, "weapon-power-percent");
  // Quarterstaffs gain every bonus that unarmed attacks would.
  const unarmedFlat = item.unarmedBonuses ? classEffects.unarmedPowerFlat + classEffects.unarmedScalingModifiers.reduce((total, modifier) => total + modifier.amount * toInteger(values[modifier.stat], 0), 0) : 0;
  const unarmedPercent = item.unarmedBonuses ? classEffects.unarmedPowerPercent : 0;
  return (basePower + artifactPowerFlat + classPowerBonus + unarmedFlat)
    * (1 + ((artifactPowerPercent + classPowerPercent + unarmedPercent) / 100))
    * classEffects.weaponPowerMultiplier
    * (1 + toNumber(item.finalPowerPercent, 0) / 100);
}

function calculateArmorForWeapon(values) {
  if (Number.isFinite(values.armor)) return values.armor;
  const combatArmor = window.Combat?.armorForEquipment?.();
  if (combatArmor != null) return combatArmor;
  const classes = calculateClassEffects(), race = calculateRaceEffects();
  const bonus = toNumber(permanentDefenseBonuses.armor, 0) + classes.defensePermanentPercent.armor + race.defensePermanentPercent.armor;
  const raw = Math.floor((BASE_ARMOR + calculateEquipmentDefense(values).armor) * (1 + bonus / 100)) + classes.defenseFlat.armor + race.defenseFlat.armor;
  return Math.max(0, Math.min(raw, equippedDefenseCap("armor"), classes.defenseCap.armor, race.defenseCap.armor));
}

function calculateWeaponHitChance(item, values = currentStats()) {
  const primaryValue = item.primary?.none ? 0 : toInteger(values[item.primary?.key], 0);
  const secondaryValue = item.secondary?.none ? 0 : toInteger(values[item.secondary?.key], 0);
  const baseHitChance = 30 + primaryValue + secondaryValue;
  const artifactEffects = calculateArtifactEffects();
  const classEffects = calculateClassEffects();
  if (classEffects.weaponHitAlways) return 100;
  const classHitBonus = classWeaponBonus(item, classEffects, "weaponHitFlat");
  const classHitPercent = classWeaponBonus(item, classEffects, "weaponHitPercent");
  const artifactHitFlat = artifactEffects.weaponHitFlat + artifactWeaponModifier(item, artifactEffects, "weapon-hit-flat");
  const artifactHitPercent = artifactEffects.weaponHitPercent + artifactWeaponModifier(item, artifactEffects, "weapon-hit-percent");
  const unarmedHit = item.unarmedBonuses ? classEffects.unarmedHitFlat : 0;
  const unarmedHitPercent = item.unarmedBonuses ? classEffects.unarmedHitPercent : 0;
  // Percentage bonuses raise Accuracy after the 70 floor, so they count even with low stats.
  // A forged weapon's own hit bonus applies last, so a penalty can lower it.
  const accuracy = Math.max(70, baseHitChance + artifactHitFlat + classHitBonus + unarmedHit);
  return Math.max(0, Math.floor(accuracy * (1 + ((artifactHitPercent + classHitPercent + unarmedHitPercent) / 100)))) + toNumber(item.hitBonus, 0);
}

// Crit Rating: the weapon's base, Luck (more for weapons that scale with Luck),
// and every class, artifact and equipment bonus. CombatRules turns it into a chance.
function weaponCritRating(item, values = currentStats()) {
  const artifactEffects = calculateArtifactEffects();
  const classEffects = calculateClassEffects();
  const luck = Math.max(0, toInteger(values.luck, 0));
  const luckScaling = [item.primary, item.secondary].some(component => component && !component.none && component.key === "luck");
  const base = toNumber(item.critBase, 5);
  const fromLuck = luck * 0.15, affinity = luckScaling ? luck * 0.1 : 0;
  const classes = classWeaponBonus(item, classEffects, "weaponCritFlat") + (item.unarmedBonuses ? toNumber(classEffects.weaponCritFlatByType.unarmed, 0) : 0);
  const artifacts = artifactEffects.weaponCritFlat + artifactWeaponModifier(item, artifactEffects, "weapon-crit-flat");
  const gear = toNumber(item.critBonus, 0);
  return { base, luck: fromLuck, affinity, classes, artifacts, equipment: gear, total: base + fromLuck + affinity + classes + artifacts + gear, disabled: classEffects.weaponCritDisabled };
}

function calculateWeaponCritChance(item, values = currentStats()) {
  const rating = weaponCritRating(item, values);
  if (rating.disabled) return 0;
  return CombatRules.critChanceFromRating(rating.total);
}

// The rating behind a weapon's crit chance, for tooltips.
function critChanceTitle(item, values = currentStats()) {
  const rating = weaponCritRating(item, values);
  const part = (label, value) => value ? `${label} ${value > 0 ? "+" : "−"}${formatValue(Math.abs(value))}` : "";
  const parts = [part("weapon", rating.base), part("Luck", rating.luck), part("Luck-scaling weapon", rating.affinity), part("classes", rating.classes), part("artifacts", rating.artifacts), part("weapon parts and runes", rating.equipment)].filter(Boolean);
  return `Crit Rating ${formatValue(rating.total)} (${parts.join(", ")}). Each point counts a little less as the chance rises; there is no hard cap. Every hit that does not crit makes the next crit more likely. Rolled only after a hit.`;
}

// Critical damage with this weapon: +25%, plus class and weapon bonuses, up to +75%.
function weaponCritDamageLabel(item) {
  const multiplier = CombatRules.criticalMultiplier(true, calculateClassEffects().criticalDamagePercent + toNumber(item.critDamage, 0));
  return "+" + Math.round((multiplier - 1) * 100) + "%";
}

function itemDescription(item, values = currentStats()) {
  return item.description.replace(/\[X\]/g, String(Math.floor(calculateItemPower(item, values)))).replace(/\bX\b/g, String(Math.floor(calculateItemPower(item, values))));
}

function getItem(itemIdValue) {
  const forge = window.ForgeRules;
  return itemsById.get(itemIdValue) || leveledMasterworkArtifact(itemIdValue) || (forge && (forge.isForgedId(itemIdValue) || forge.runeOfItem(itemIdValue) || forge.isTemplateId(itemIdValue) || forge.masterworkTemplateKey(itemIdValue)) ? forge.item(itemIdValue) || undefined : undefined);
}

function unavailableItem(itemIdValue) {
  const category = itemIdValue.startsWith("weapon-") ? "weapon" : itemIdValue.startsWith("artifact-") ? "artifact" : itemIdValue.startsWith("consumable-") ? "consumable" : "armor";
  const name = itemIdValue
    .replace(/^(weapon|armor|artifact|consumable)-/, "")
    .replace(/-[^-]+$/, "")
    .replace(/-/g, " ")
    .replace(/^./, (letter) => letter.toUpperCase());
  return {
    id: itemIdValue,
    category,
    name,
    type: "Unavailable",
    rarity: "common",
    description: "Load the item catalogue to reveal this item's details.",
    unavailable: true,
  };
}

function itemTypeLabel(type) {
  const labels = { artifact: "Artifact", "key-item": "Key Item", meele: "Melee", melee: "Melee", ranged: "Ranged", "magic-wand": "Magic Wand", "spell-staff": "Spell Staff", heavy: "Heavy", light: "Light", clothing: "Clothing", shield: "Shield" };
  return labels[type] || type.charAt(0).toUpperCase() + type.slice(1);
}

// Forged weapons name their weapon type (Longsword, Crossbow…) instead of Melee or Ranged.
function itemKindLabel(item) {
  return item?.forged ? item.forged.typeName : item?.category === "rune" ? item.kindName : itemTypeLabel(item?.type || "");
}

function itemCategoryLabel(category) {
  if (category === "consumable") return "Consumable";
  if (category === "key-item") return "Key Item";
  if (category === "rune") return "Rune";
  return category === "weapon" ? "Weapon" : category === "artifact" ? "Artifact" : "Armor";
}

// Masterwork weapons, armor and artifacts carry the drop rules that unique items once had.
function isMasterworkEquipment(item) {
  return item?.rarity === "masterwork" && ["weapon", "armor", "artifact"].includes(item.category);
}
// The Exotics (port task 7): unique weapons and armor above masterwork. Masterwork rules apply to them wherever no Exotic
// rule is given (they always drop when rolled, Luck raises their chance, their effects show in battle), but Limitless's
// ×2 is for masterworks only, and they drop only while Limitless is on.
function isExoticEquipment(item) {
  return item?.rarity === "exotic" && ["weapon", "armor"].includes(item.category);
}
const isUniqueRarity = rarity => rarity === "masterwork" || rarity === "exotic";
// Gear above Level 100 while Limitless is off (L10, L27): a red border and "Level N required".
const needsLimitless = item => Boolean(item) && !LimitlessRules.wearable(item, limitless.active);
const limitlessNote = item => needsLimitless(item) ? '<span class="needs-limitless-note">Level ' + LimitlessRules.itemLevel(item) + ' required</span>' : "";

function itemRarityLabel(rarity) {
  return rarity.charAt(0).toUpperCase() + rarity.slice(1);
}
// A coloured gem-and-name tag that makes an item's rarity readable at a glance.
function itemRarityTag(rarity) {
  const value = rarity || "common";
  return '<span class="rarity-tag rarity-' + escapeHtml(value) + '">' + escapeHtml(itemRarityLabel(value)) + '</span>';
}
function itemPurchasePriceLabel(item) {
  return item.shopOffer
    ? `${item.shopOffer.costQuantity} × ${getItem(item.shopOffer.costItemId)?.name || item.shopOffer.costItemId}`
    : ShopRules.formatPrice(item.priceSilver);
}

function itemRarityClass(item) {
  return `item-rarity-${item.rarity || "common"}${needsLimitless(item) ? " needs-limitless" : ""}`;
}

function artifactEffectLabel(effect) {
  if ((effect.categories || effect.excludeElement) && ["spell-cost-percent", "spell-power-percent"].includes(effect.kind) && window.ForgeRules) {
    const text = ForgeRules.spellEffectText(effect);
    return text.charAt(0).toUpperCase() + text.slice(1);
  }
  const stat = statDefinitions.find(({ key }) => key === effect.target);
  const statLabel = stat ? stat.short : effect.target;
  const absoluteAmount = formatValue(Math.abs(effect.amount));
  const change = (suffix = "") => (effect.amount >= 0 ? "+" : "−") + absoluteAmount + suffix;
  const reduction = (suffix = "") => (effect.amount >= 0 ? "−" : "+") + absoluteAmount + suffix;
  const weaponLabel = effect.weaponType ? itemTypeLabel(effect.weaponType) : "Weapon";
  const spellLabel = effect.element ? spellElementLabel(effect.element) + " spell" : "Spell";
  const labels = {
    "stat-flat": statLabel + " " + change(),
    "stat-percent": statLabel + " " + change("%"),
    "stat-permanent-percent": statLabel + " " + change("%") + " permanent",
    "max-hp-flat": "Max HP " + change(),
    "max-hp-percent": "Max HP " + change("%"),
    "max-mana-flat": "Max Mana " + change(),
    "max-mana-percent": "Max Mana " + change("%"),
    "weapon-power-flat": weaponLabel + " power " + change(),
    "weapon-power-percent": weaponLabel + " power " + change("%"),
    "armor-flat": "Armor " + change(),
    "armor-percent": "Armor " + change("%"),
    "evasion-flat": "Evasion " + change(),
    "evasion-percent": "Evasion " + change("%"),
    "weapon-hit-flat": weaponLabel + " Accuracy " + change(),
    "weapon-hit-percent": weaponLabel + " Accuracy " + change("%"),
    "weapon-crit-flat": weaponLabel + " Crit Rating " + change(),
    "spell-cost-flat": spellLabel + " cost " + reduction(),
    "spell-cost-percent": spellLabel + " cost " + reduction("%"),
    "spell-power-flat": spellLabel + " power " + change(),
    "spell-power-percent": spellLabel + " power " + change("%"),
    "trap-limit-flat": "Active trap limit " + change(),
    "low-hp-turn-start-heal-percent": "At turn start below " + effect.thresholdPercent + "% HP, restore " + absoluteAmount + "% maximum HP",
    "low-hp-summon": "Once per combat below " + effect.thresholdPercent + "% HP: summon " + absoluteAmount + " Steelguard Golem to protect allies" + (effect.strengthPercent != null ? " (" + formatValue(effect.strengthPercent) + "% of its strength)" : ""),
    "damage-percent-vs-slower": "Damage against slower targets " + change("%"),
    "damage-percent-vs-wounded": "Damage against targets at or below " + toNumber(effect.hpPercent, 30) + "% HP " + change("%") + Object.entries(effect.raceBonus || {}).map(([raceId, amount]) => ", +" + amount + "% against " + (raceCatalog.find(race => race.id === raceId)?.name || raceId) + (/s$/.test(raceCatalog.find(race => race.id === raceId)?.name || raceId) ? "" : "s")).join(""),
    "element-resistance": (effect.elements || []).map(element => element.charAt(0).toUpperCase() + element.slice(1)).join(", ").replace(/, ([^,]*)$/, " and $1") + " damage taken −" + effect.amount + "%",
    "crit-vs-targets": "Against " + [...(effect.classIds || []).map(id => getClass(id)?.name || id), ...(effect.raceIds || []).map(id => raceCatalog.find(race => race.id === id)?.name || id)].join(" or ") + " enemies: crit chance +" + effect.chance + " points, crit damage +" + effect.damage + "%",
    "damage-percent-vs-race": "Damage against " + (effect.raceIds || []).map(id => raceCatalog.find(race => race.id === id)?.name || id).join(" or ") + " " + change("%"),
    "damage-and-pierce-vs-larger-max-hp": effect.stepPercent
      ? "Against bigger targets: damage +" + absoluteAmount + "% for every " + effect.stepPercent + "% more max HP than yours (up to +" + effect.maxAmount + "%), Armor Pierce +" + effect.armorPiercePercent + "%"
      : "Against targets with at least " + effect.thresholdPercent + "% more max HP: damage " + change("%") + ", Armor Pierce +" + effect.armorPiercePercent + "%",
    "bonus-basic-attack-percent": "Each hit: +" + absoluteAmount + "% Basic damage; Armor Pierce +" + effect.armorPiercePercent + "%",
    "golden-apex-choice": "Before each spell: choose all-enemy effects with +35% damage, or +100% unreducible damage",
    "always-hit": "Never misses",
    "critical-chain-damage-percent": "Each critical hit charges your next attack with +" + absoluteAmount + "% damage; every further critical hit in a row adds another +" + absoluteAmount + "%, up to +" + effect.maxAmount + "%. A hit that is not critical uses the charge up",
    "prism-basic-damage": "Your Basic damage splits into four quarters: one stays Basic, the other three become random other elements",
    "unreducible-cleave-percent": "Other enemies take " + absoluteAmount + "% of attack damage, unreducible",
    "cleave-percent": "Other enemies take " + absoluteAmount + "% of attack damage; their defenses apply",
    "spell-reflect-percent": "When an enemy's spell damages you, " + absoluteAmount + "% of the damage strikes its caster back in the spell's element; their defenses apply",
    "extra-target-spell": "A single-target spell can take a second target for " + absoluteAmount + "% more Mana",
    "second-strike": "Deals " + absoluteAmount + "% less damage, but each hit strikes the target again for the same amount. The second strike counts as a weapon attack",
    "fifth-attack-boost": "Every " + formatValue(effect.every) + "th attack with it in a battle deals " + absoluteAmount + "% more damage (" + formatValue(effect.criticalAmount) + "% more if it is a critical hit) and echoes " + formatValue(effect.echoPercent) + "% of the damage dealt to every other enemy; their defenses apply",
    "armor-pierce-bonus": "Attacks ignore " + absoluteAmount + "% more of the target's Armor (added to its Armor Pierce, up to 100%)",
    "stun-on-hit-chance": absoluteAmount + "% on hit: stun the target for 1 turn",
    "turn-and-speed-lock": "Its wielder's turn meter can't be pushed back and their Speed can't be lowered",
    "spell-cost-mark-on-damage": "Enemies it damages are marked until the end of their next turn: their spells cost " + absoluteAmount + "% more Mana",
    // The Exotics (port task 7).
    "armor-break-on-hit": "Every hit removes " + absoluteAmount + "% of the target's Armor for " + effect.durationTurns + " turn" + (effect.durationTurns === 1 ? "" : "s") + " (" + formatValue(effect.critAmount) + "% on a critical hit); each hit stacks",
    "lethal-survive-chance": "When damage would bring you to 0 HP: " + absoluteAmount + "% chance to survive at 1 HP instead",
    "damage-per-evasion": "Damage +" + absoluteAmount + "% for every " + formatValue(effect.evasion) + " Evasion",
    "per-rune-damage-pierce": "Every rune attached: +" + absoluteAmount + "% damage and +" + absoluteAmount + " Armor Pierce (counted in its stats)",
    "shared-burden": "Damage and healing you receive in battle −" + absoluteAmount + "%; the part taken off is shared equally by your living allies, summons included",
    "avalanche-counters": "Each damage taken adds a counter; at " + effect.maximum + " every enemy loses " + absoluteAmount + "% Speed for " + effect.durationTurns + " turns and the counters reset",
    "weapon-power-from-armor-percent": "Gain weapon power equal to " + absoluteAmount + "% of current Armor (Armor is retained)",
    "armor-from-max-hp-percent": "Gain Armor equal to " + absoluteAmount + "% of maximum HP",
    // Doctor's Orders (port task 3, R12, R13, R24, R25).
    "healing-given-percent": "Every heal you cause in battle (spells, skills, items, heals over time, lifesteal, regeneration) " + change("%") + "; a heal you give yourself takes only this bonus",
    "healing-received-percent": "Healing you receive in battle " + change("%") + " (resting and camping are not raised)",
    "missing-hp-spell-ward": "Spell damage reduced by " + absoluteAmount + "% at full HP, decreasing by 1 point per " + effect.missingHpPercentPerPoint + "% HP missing",
    "party-mana-drain-on-hit-percent": "On hit: drain " + absoluteAmount + "% current Mana and share it with your party",
    "bonus-damage-from-armor-percent": "On damaging hit: deal " + absoluteAmount + "% of your Armor as bonus damage",
    "mana-retaliation": "After taking damage: optionally spend " + effect.manaCost + " Mana to return " + absoluteAmount + "% as " + spellElementLabel(effect.element || "fire") + " damage (0 AP)",
    "damage-charge": "Taking damage: +" + absoluteAmount + "% next attack/spell damage per charge; at " + effect.maximum + " charges: +" + effect.fullBonusPercent + "% instead; charges consumed on use",
    "mark-on-hit": effect.chance + "% on hit: mark for " + effect.durationTurns + " turns; melee/ranged damage taken +" + absoluteAmount + "%",
    "all-stats-on-nonbasic-spell-damage-per-tier": "After non-Basic spell damage: all basic stats +" + absoluteAmount + "% per spell tier for " + effect.durationTurns + " turns (strongest bonus; refreshes)",
    "melee-hit-guard-per-rarity": "After a melee hit: −" + absoluteAmount + "% damage taken per weapon rarity until your next turn",
    "all-stats-on-damage": effect.maxStacks > 1
      ? "After taking damage: all basic stats +" + absoluteAmount + "% for " + effect.durationTurns + " turns, stacking up to +" + absoluteAmount * effect.maxStacks + "% (each hit refreshes)"
      : "After taking damage: all basic stats +" + absoluteAmount + "% for " + effect.durationTurns + " turns (refreshes)",
    "ranged-splash-percent": "Ranged hits deal " + absoluteAmount + "% of their damage to all other enemies",
    "summon-empowerment": "Your summons gain +" + absoluteAmount + "% maximum HP, +" + effect.manaPercent + "% maximum Mana and +" + effect.damagePercent + "% damage",
    "offhand-armor-pierce-percent": "Off-hand attacks gain " + absoluteAmount + "% Armor Pierce",
    "weapon-crit-percent-vs-class": "Crit Chance ×" + formatValue(1 + effect.amount / 100) + " against " + (effect.classIds || []).map(id => getClass(id)?.name || id).join(" or ") + " classes",
    "defense-cap": (effect.target === "evasion" ? "Evasion" : "Armor") + " cannot exceed " + absoluteAmount,
    "turning-seasons": "Turning Seasons, changing every " + (effect.turns || 2) + " of your turns: " + (effect.seasons || []).map(season => season.name + ": " + [
      ...(season.regenHpPercent ? ["regain " + season.regenHpPercent + "% max HP each turn"] : []),
      ...(season.effects || []).filter(entry => entry.type !== "spell-power-percent").map(entry => entry.type === "outgoing-attack-damage-percent" ? "+" + entry.amount + "% damage dealt"
        : entry.type === "melee-retaliation-percent" ? "melee attackers take " + entry.amount + "% of their damage back"
        : entry.type === "incoming-weapon-damage-percent" ? (entry.amount < 0 ? "−" : "+") + Math.abs(entry.amount) + "% damage taken" : null).filter(Boolean),
    ].join(", ")).join(" · "),
    "absorb-element-spell-chance": effect.chance + "% chance to absorb a hostile " + (effect.element ? spellElementLabel(effect.element) + " " : "") + "spell and regain the Mana it cost",
    "incoming-race-damage-percent": "Damage taken from " + (effect.raceIds || []).map(id => raceCatalog.find(race => race.id === id)?.name || id).join(" or ") + " " + change("%"),
    "spell-add-element": "Non-" + (effect.element ? spellElementLabel(effect.element) : "") + " spells also gain the " + (effect.element ? spellElementLabel(effect.element) : "") + " element",
    "incoming-spell-damage-percent": "Incoming " + (effect.element ? spellElementLabel(effect.element) : "all") + " spell damage " + change("%"),
    "restore-mana-on-elemental-hit-percent": "After taking " + (effect.element ? spellElementLabel(effect.element) : "elemental") + " damage, restore " + absoluteAmount + "% maximum Mana",
    // The Artifact Rework's effects (port task 4).
    "money-percent": "Silver from battles " + change("%"),
    "all-money-percent": "Money from all sources " + change("%"),
    "luck-per-platinum": "Luck +" + String(Math.round(toNumber(effect.amount, 0) * 100) / 100) + " for every Platinum you hold",
    "exp-percent": "EXP from battles " + change("%"),
    "weapon-lifesteal-percent": "Weapon hits heal you for " + absoluteAmount + "% of the damage dealt",
    "damage-lifesteal-percent": "Damage you deal with weapons and spells heals you for " + absoluteAmount + "% of it",
    "melee-thorns-percent": "Melee attackers take " + absoluteAmount + "% of the damage they deal you",
    "turn-start-heal-percent": "Restore " + absoluteAmount + "% maximum HP at the start of your turn",
    "turn-start-mana-percent": "Restore " + absoluteAmount + "% maximum Mana at the start of your turn",
    "first-hit-guard-percent": "The first hit you take each battle deals " + absoluteAmount + "% less damage",
    "first-strike-damage-percent": "Your first attack or spell that deals damage each battle deals +" + absoluteAmount + "% damage",
    "damage-percent-vs-race": "Damage against " + (effect.raceIds || []).map(id => raceCatalog.find(race => race.id === id)?.name || id).join(" and ") + " " + change("%"),
    "element-resistance-percent": spellElementLabel(effect.element || "fire") + " damage you take −" + absoluteAmount + "%",
    "spell-hit-mana-percent": "When an enemy's spell damages you, restore " + absoluteAmount + "% maximum Mana",
    "damage-percent-vs-low-hp": "Damage against enemies below " + effect.thresholdPercent + "% HP " + change("%"),
    "damage-percent-vs-larger-max-hp": "Damage against enemies with more maximum HP than you " + change("%"),
    "battle-start-weapon-power-percent": "At battle start: weapon power " + change("%") + " for " + effect.durationTurns + " turns",
    "free-spell-chance": absoluteAmount + "% chance a spell costs no Mana",
    "battle-start-barrier-percent": "At battle start: a barrier of " + absoluteAmount + "% maximum HP",
    "battle-start-party-stats-percent": "At battle start: you and your allies get all attributes " + change("%") + " for " + effect.durationTurns + " turns",
    "on-hit-burn": effect.chance + "% on weapon hit: burn for " + absoluteAmount + "% of the target's maximum HP per turn for " + effect.durationTurns + " turns",
    "on-hit-slow": effect.chance + "% on weapon hit: the target's Speed −" + absoluteAmount + "% for " + effect.durationTurns + " turns",
    "on-hit-armor-shred": "Weapon hits: the target's Armor −" + absoluteAmount + "% for " + effect.durationTurns + " turns, stacking up to " + effect.maxStacks + " times",
    "turn-start-cleanse-chance": absoluteAmount + "% at the start of your turn: remove one debuff from you",
    "kill-restore-percent": "When an enemy falls: restore " + absoluteAmount + "% maximum HP" + (effect.mana ? " and " + formatValue(effect.mana) + "% maximum Mana" : ""),
    "evade-next-attack-percent": "After you evade an attack, your next weapon attack deals +" + absoluteAmount + "% damage",
    "death-save": "Once per battle, damage that would defeat you leaves you at " + absoluteAmount + " HP",
    "spell-chain-percent": "Damaging spells also strike one other random enemy for " + absoluteAmount + "% of the damage",
    "first-turn-ap": "+" + absoluteAmount + " AP on your first turn of each battle",
    "ally-low-hp-barrier": "The first time each ally drops below " + effect.thresholdPercent + "% HP: a barrier of " + absoluteAmount + "% of their maximum HP",
    "on-hit-stun-chance": absoluteAmount + "% on weapon hit: stun the target for 1 turn",
    "on-hit-poison": "Weapon hits poison the target for " + absoluteAmount + "% of its maximum HP per turn for " + effect.durationTurns + " turns",
    "low-hp-fury": "Below " + effect.thresholdPercent + "% HP: damage " + change("%") + ", Evasion +" + formatValue(effect.evasion || 0),
    "party-revive": "Once per battle, the first of your heroes to fall rises at " + absoluteAmount + "% HP at the start of your next turn",
    "turn-start-star": "At the start of your turn: " + spellElementLabel(effect.element || "holy") + " damage equal to " + absoluteAmount + "% of your maximum Mana to a random enemy",
    "party-damage-armor-percent": "You and every ally: damage " + change("%") + ", Armor +" + formatValue(effect.armor || 0) + "%",
    "crit-damage-percent": "Critical hits deal " + change("%") + " damage",
  };
  const label = labels[effect.kind] || effect.kind;
  return label + (effect.minTier != null ? ` · Tier ${effect.minTier} or higher` : "") + (effect.maxTier != null ? ` · Tier ${effect.maxTier} or lower` : "") + (effect.summonOnly ? " · Summon spells only" : "");
}

function itemStatLabel(component) {
  if (!component || component.none) return "None";
  return spellStatLabel(component);
}

function itemStatCodeLabel(component) {
  if (!component || component.none) return "None";
  const definition = statDefinitions.find(({ key }) => key === component.key);
  return definition ? String(formatValue(component.ratio)) + " " + definition.short : component.code;
}

function forgedScalingLabel(item, component, values = currentStats()) {
  const definition = statDefinitions.find(({ key }) => key === component.key);
  const ratio = Math.round((component.ratio + (component === item.primary ? forgedPrimaryBonus(item) : 0)) * 100) / 100;
  return `${ratio} ${definition?.short || component.code} (+${Math.floor(ratio * toInteger(values[component.key], 0))})`;
}

function forgedDamageLabel(item) {
  const base = window.ForgeRules.ELEMENT_NAMES[item.element || "physical"];
  if (!item.elementShare) return base;
  const share = item.elementShare.percent;
  return `${100 - share}% ${base} · ${share}% ${window.ForgeRules.ELEMENT_NAMES[item.elementShare.element]}`;
}

// Everything a forged weapon does, as label/value pairs for slots, shops and details.
// What a forged armor piece adds once worn, with the wearer's current stats.
function forgedArmorValue(item) {
  return Math.floor((item.armorValue + Math.floor(calculateItemPower(item))) * (1 + toNumber(item.armorPercent, 0) / 100));
}

function forgedArmorStats(item) {
  const scaling = item.scaling, definition = statDefinitions.find(({ key }) => key === scaling.key);
  const resist = Object.entries(item.resistances || {}).filter(([, amount]) => amount > 0).sort((a, b) => b[1] - a[1]);
  const evasionChange = toNumber(item.evasionPercent, 0);
  return [
    { label: "Armor", value: "+" + forgedArmorValue(item), title: "Base armor plus scaling, then this piece's armor bonuses" },
    { label: "Base armor", value: formatValue(item.armorValue) },
    { label: "Scaling", value: `${Math.round(scaling.ratio * 100) / 100} ${definition?.short || scaling.code} (+${Math.floor(calculateItemPower(item))})` },
    { label: "Armor bonus", value: (item.armorPercent >= 0 ? "+" : "−") + Math.abs(toNumber(item.armorPercent, 0)) + "%" },
    { label: "Evasion", value: (evasionChange >= 0 ? "+" : "−") + Math.abs(evasionChange) + "%" + (item.evasionFlat ? (item.evasionFlat > 0 ? " · +" : " · −") + Math.abs(item.evasionFlat) : ""), title: "Changes your Evasion by this percentage, then adds the flat bonus" },
    { label: "Level", value: String(item.forged.level) },
    ...(item.hpPercent ? [{ label: "Max HP", value: "+" + item.hpPercent + "%" }] : []),
    ...(resist.length ? [{ label: "Resistances", value: resist.map(([element, amount]) => amount + "% " + spellElementLabel(element)).join(" · ") }] : []),
    ...(item.thorns ? [{ label: "Spikes", value: item.thorns + "% of melee damage returned" }] : []),
    ...(item.lifesteal ? [{ label: "Lifesteal", value: "Heal " + item.lifesteal + "% of weapon damage dealt" }] : []),
    ...(item.critDefense ? [{ label: "Crit defense", value: "−" + item.critDefense + " Crit Rating against you" }] : []),
    ...(item.armorTotalPercent ? [{ label: "Total armor", value: "+" + item.armorTotalPercent + "%" }] : []),
    ...(item.effects || []).map((effect) => ({ label: "Effect", value: artifactEffectLabel(effect) })),
  ];
}

function forgedItemStats(item) {
  if (item.forged.armor) return forgedArmorStats(item);
  const spear = item.twoHandedPrimaryBonus;
  if (item.forged.caster) return [
    { label: "Melee power", value: String(Math.floor(calculateItemPower(item))), title: "Power of a melee attack with this weapon, with your current stats and bonuses" },
    { label: "Base power", value: formatValue(item.basePower) },
    { label: "Primary scaling", value: forgedScalingLabel(item, item.primary) },
    { label: "Secondary scaling", value: item.secondary?.none ? "None" : forgedScalingLabel(item, item.secondary) },
    // A wand that attacks as another weapon (the Plasma Blade as a longsword).
    ...(item.attacksAs ? [{ label: "Attacks as", value: ForgeRules.WEAPONS[item.attacksAs].name }, { label: "Damage", value: forgedDamageLabel(item) }] : []),
    { label: "Accuracy", value: String(calculateWeaponHitChance(item)), title: "Accuracy meets the target's Evasion: hit chance = Accuracy ÷ (Accuracy + Evasion). Ranged attacks face half the target's Evasion." },
    { label: "Crit chance", value: calculateWeaponCritChance(item) + "%", title: critChanceTitle(item) },
    { label: "Hand", value: item.handed === 2 ? "Two-handed" : "One-handed" },
    { label: "Level", value: String(item.forged.level) },
    ...(item.effects || []).map((effect) => ({ label: "Spell effect", value: artifactEffectLabel(effect) })),
  ];
  return [
    { label: "Power", value: String(Math.floor(calculateItemPower(item))), title: "Final power with your current stats and bonuses" },
    { label: "Base power", value: formatValue(item.basePower) },
    { label: "Primary scaling", value: forgedScalingLabel(item, item.primary), title: spear ? `+${spear}× while the off-hand is empty` : "" },
    { label: "Secondary scaling", value: forgedScalingLabel(item, item.secondary) },
    { label: "Armor pierce", value: formatValue(item.armorPierce || 0) + "%" },
    { label: "Accuracy", value: String(calculateWeaponHitChance(item)), title: "Accuracy meets the target's Evasion: hit chance = Accuracy ÷ (Accuracy + Evasion). Ranged attacks face half the target's Evasion." },
    { label: "Crit chance", value: calculateWeaponCritChance(item) + "%", title: critChanceTitle(item) },
    { label: "Crit damage", value: weaponCritDamageLabel(item), title: "Critical hits deal 25% more damage, plus class bonuses and this weapon type's own bonus, up to 75% more." },
    { label: "Hand", value: spear ? (equipment.offHand ? "One-handed" : "Two-handed (spear)") : item.handed === 2 ? "Two-handed" : "One-handed" },
    { label: "Damage", value: forgedDamageLabel(item) },
    { label: "Level", value: String(item.forged.level) },
    ...(item.effects || []).map((effect) => ({ label: "Effect", value: artifactEffectLabel(effect) })),
    ...(item.unarmedBonuses ? [{ label: "Special", value: "Gains unarmed attack bonuses" }] : []),
  ];
}

function equipmentItemStats(item) {
  if (item.forged) return forgedItemStats(item);
  if (item.category === "artifact") {
    return [{ label: "Description", value: item.description }];
  }
  if (item.category === "weapon") {
    return [
      { label: "Power", value: String(Math.floor(calculateItemPower(item))) },
      { label: "Accuracy", value: String(calculateWeaponHitChance(item)), title: "Accuracy meets the target's Evasion: hit chance = Accuracy ÷ (Accuracy + Evasion). Ranged attacks face half the target's Evasion." },
      { label: "Hand", value: item.handed === 2 ? "Two-handed" : "One-handed" },
      ...(item.minimumStrength ? [{ label: "Requires", value: item.minimumStrength + " Strength" }] : []),
      { label: "Crit chance", value: calculateWeaponCritChance(item) + "%", title: critChanceTitle(item) },
      ...(item.effects || []).map((effect) => ({ label: "Effect", value: artifactEffectLabel(effect) })),
    ];
  }
  const evasionChange = (item.effects || []).filter(effect => effect.kind === "evasion-flat").reduce((sum, effect) => sum + effect.amount, 0) - item.evasionDebuff;
  return [
    { label: "Armor", value: "+" + formatValue(item.armorValue) },
    { label: "Evasion", value: (evasionChange > 0 ? "+" : "−") + formatValue(Math.abs(evasionChange)) },
    { label: "Scaling power", value: "+" + Math.floor(calculateItemPower(item)) },
    { label: "Scaling", value: itemStatCodeLabel(item.scaling) },
    ...(item.effects || []).map((effect) => ({ label: "Effect", value: artifactEffectLabel(effect) })),
  ];
}

function itemEquipSlots(item) {
  if (!item || item.unavailable) return [];
  if (!["weapon", "armor", "artifact"].includes(item.category)) return [];
  if (raceDisallowsItem(item)) return [];
  if (item.category === "weapon" && currentStats().strength < (item.minimumStrength || 0)) return [];
  if (item.category === "weapon") return item.handed === 2 ? ["primaryHand"] : ["primaryHand", "offHand"];
  if (item.category === "artifact") return ["artifact1", "artifact2"];
  return item.type === "shield" ? ["offHand"] : ["armor"];
}

function equipmentSlotLabel(slotKey) {
  return equipmentSlotDefinitions.find(({ key }) => key === slotKey)?.label || slotKey;
}

function isTwoHandedEquipped(equipmentState = equipment) {
  const primary = getItem(equipmentState.primaryHand);
  return Boolean(primary?.category === "weapon" && primary.handed === 2 && equipmentState.offHand === primary.id);
}

function candidateEquipment(item, slotKey) {
  if (!item || !itemEquipSlots(item).includes(slotKey)) return null;
  if (slotKey === "offHand" && isTwoHandedEquipped()) return null;
  const candidate = { ...equipment };
  const previousPrimary = getItem(candidate.primaryHand);
  if (slotKey === "primaryHand" && previousPrimary?.handed === 2 && candidate.offHand === previousPrimary.id) candidate.offHand = null;
  if (slotKey === "primaryHand") {
    candidate.primaryHand = item.id;
    if (item.category === "weapon" && item.handed === 2) candidate.offHand = item.id;
  } else {
    candidate[slotKey] = item.id;
  }
  return candidate;
}

function requiredEquipmentCounts(equipmentState) {
  const counts = {};
  const primary = getItem(equipmentState.primaryHand);
  equipmentSlotDefinitions.forEach(({ key }) => {
    const itemIdValue = equipmentState[key];
    if (!itemIdValue) return;
    if (key === "offHand" && primary?.category === "weapon" && primary.handed === 2 && equipmentState.offHand === primary.id) return;
    counts[itemIdValue] = (counts[itemIdValue] || 0) + 1;
  });
  return counts;
}

function canEquipItem(item, slotKey) {
  if (item?.category === "weapon" && currentStats().strength < (item.minimumStrength || 0)) return false;
  // Above Level 100 only with Limitless on (task 7, L10, L25).
  if (!LimitlessRules.wearable(item, limitless.active)) return false;
  const candidate = candidateEquipment(item, slotKey);
  if (!candidate) return false;
  const required = requiredEquipmentCounts(candidate);
  return Object.entries(required).every(([itemIdValue, count]) => inventory.filter((ownedItemId) => ownedItemId === itemIdValue).length >= count);
}

function equipItem(item, slotKey) {
  if (!canEquipItem(item, slotKey)) {
    const message = item?.category === "weapon" && currentStats().strength < (item.minimumStrength || 0)
      ? `Requires ${item.minimumStrength} Strength to equip ${item.name}`
      : !LimitlessRules.wearable(item, limitless.active)
      ? `${item.name} is Level ${LimitlessRules.itemLevel(item)}: it can only be worn while Limitless is on`
      : raceDisallowsItem(item)
      ? `${getSelectedRace()?.name || "This race"} cannot equip ${item.name}`
      : slotKey === "offHand" && isTwoHandedEquipped()
      ? "Off-hand is occupied by a two-handed weapon"
      : "You do not own another copy of that item";
    setInventoryStatus(message, "error");
    return;
  }
  equipment = candidateEquipment(item, slotKey);
  renderEquipmentSlots();
  renderOwnedItems();
  renderKnownSpells();
  renderAvailableSpells();
  updateDerivedValues();
  setInventoryStatus(`${item.name} equipped in ${equipmentSlotLabel(slotKey)}`, "success");
  saveCharacter();
}

function unequipItem(slotKey) {
  if (slotKey === "offHand" && isTwoHandedEquipped()) return;
  const equipped = getItem(equipment[slotKey]);
  if (!equipped) return;
  if (slotKey === "primaryHand" && equipped.category === "weapon" && equipped.handed === 2) {
    equipment.primaryHand = null;
    equipment.offHand = null;
  } else {
    equipment[slotKey] = null;
  }
  renderEquipmentSlots();
  renderOwnedItems();
  renderKnownSpells();
  renderAvailableSpells();
  updateDerivedValues();
  setInventoryStatus(`${equipped.name} unequipped`, "success");
  saveCharacter();
}

function renderEquipmentSlots() {
  equipmentSlotDefinitions.forEach((definition) => {
    const content = equipmentSlots[definition.key];
    if (!content) return;
    const card = content.closest(".equipment-slot");
    const blocked = definition.key === "offHand" && isTwoHandedEquipped();
    card.classList.toggle("is-blocked", blocked);
    if (definition.future) {
      content.innerHTML = "";
      return;
    }
    if (blocked) {
      content.innerHTML = '<div class="equipment-slot-empty-button" aria-disabled="true">Occupied by two-handed weapon</div>';
      return;
    }
    const item = getItem(equipment[definition.key]);
    if (item) {
      const itemStats = equipmentItemStats(item);
      content.innerHTML = '<button class="equipment-slot-control ' + itemRarityClass(item) + (item.forged ? ' is-forged' : '') + '" type="button" data-item-category="' + item.category + '" data-unequip-slot="' + definition.key + '"' + (item.forged ? ' aria-describedby="forged-tip-' + definition.key + '"' : '') + '>' +
        '<span class="equipment-slot-item-name">' + (item.forged ? forgedLogo(item.forged.maker, "forged-slot-logo") : '') + escapeHtml(item.name) + '</span>' +
        '<span class="equipment-slot-item-meta">' + itemRarityTag(item.rarity) + escapeHtml(' · Remove') + '</span>' +
        '<span class="equipment-slot-stats">' + itemStats.map((stat) => '<span class="equipment-slot-stat"><small' + (stat.title ? ' title="' + escapeHtml(stat.title) + '"' : "") + '>' + escapeHtml(stat.label) + '</small><strong' + (item.category === "artifact" && stat.label === "Description" ? ' title="' + escapeHtml(stat.value) + '"' : "") + '>' + escapeHtml(stat.value) + '</strong></span>').join("") + '</span>' +
        (item.forged ? forgedTooltipMarkup(item).replace('class="forged-tooltip"', 'class="forged-tooltip" id="forged-tip-' + definition.key + '"') : '') +
        '</button>';
      const control = content.querySelector("[data-unequip-slot]");
      control.addEventListener("click", () => unequipItem(definition.key));
      if (item.forged) for (const type of ["pointerenter", "focus"]) control.addEventListener(type, () => placeForgedTooltip(control));
    } else {
      content.innerHTML = '<button class="equipment-slot-empty-button" type="button" data-open-inventory>Empty · Choose item</button>';
      content.querySelector("[data-open-inventory]").addEventListener("click", () => openInventory(definition.key));
    }
  });
}

function setInventoryStatus(message, state = "") {
  inventoryStatus.textContent = message;
  inventoryStatus.className = `spellbook-status${state ? ` is-${state}` : ""}`;
}

function ownedItemActions(item, slotFilter = null) {
  const slots = slotFilter ? itemEquipSlots(item).filter((slotKey) => slotKey === slotFilter) : itemEquipSlots(item);
  return slots.map((slotKey) =>
    '<button class="spell-use-button item-equip-button" type="button" data-item-id="' + escapeHtml(item.id) + '" data-equip-slot="' + slotKey + '"' + (canEquipItem(item, slotKey) ? "" : " disabled") + '>' + escapeHtml(slotKey === "armor" ? "Armor" : equipmentSlotLabel(slotKey).replace(" hand", "").replace("-", " ")) + '</button>'
  ).join("");
}

function isItemFavorite(itemId) {
  return favoriteItems.includes(itemId);
}

function toggleItemFavorite(itemId) {
  if (!inventory.includes(itemId)) return;
  favoriteItems = isItemFavorite(itemId) ? favoriteItems.filter(id => id !== itemId) : [...favoriteItems, itemId];
  renderOwnedItems();
  const item = getItem(itemId) || unavailableItem(itemId);
  setInventoryStatus(item.name + (isItemFavorite(itemId) ? " favorited. All copies are protected from Sell all non-favorites." : " removed from favorites."), "success");
  window.Shop?.render();
  saveCharacter();
}

function renderOwnedItems() {
  inventoryCount.textContent = String(inventory.length);
  if (inventoryModal.hidden) return;
  const grouped = new Map();
  inventory.forEach((itemIdValue) => {
    const group = grouped.get(itemIdValue) || { item: getItem(itemIdValue) || unavailableItem(itemIdValue), quantity: 0 };
    group.quantity += 1;
    grouped.set(itemIdValue, group);
  });
  const owned = [...grouped.values()];
  const visible = owned.filter(({ item }) =>
    (itemFilter === "all" || item.category === itemFilter) &&
    (!inventoryEquipSlot || itemEquipSlots(item).includes(inventoryEquipSlot))
  );
  if (!visible.length) {
    const message = inventoryEquipSlot
      ? `No items owned for the ${equipmentSlotLabel(inventoryEquipSlot).toLowerCase()} slot.`
      : owned.length && itemFilter !== "all" ? `No ${itemFilter} items owned.` : editingEnabled ? "No items owned yet.<br />Use + to add one from the catalogue." : "No items owned yet. Visit a Blacksmith or General Store to buy equipment and supplies.";
    ownedItemsList.innerHTML = '<div class="spell-list-empty"><span aria-hidden="true">◫</span><p>' + message + '</p></div>';
    return;
  }
  ownedItemsList.innerHTML = visible.map(({ item, quantity }) =>
    '<article class="spell-entry inventory-entry ' + itemRarityClass(item) + '" data-item-id="' + escapeHtml(item.id) + '">' +
      '<button class="spell-select-button item-select-button" type="button">' +
        '<span class="spell-entry-name"><span>' + escapeHtml(item.name) + '</span><span class="item-quantity" aria-label="Quantity ' + quantity + '">×' + quantity + '</span></span>' +
        '<span class="spell-entry-meta item-entry-meta">' + itemRarityTag(item.rarity) + '<span>' + escapeHtml(itemCategoryLabel(item.category)) + '</span><span>· ' + escapeHtml(itemKindLabel(item)) + '</span>' + (item.forged ? '<span>· Lv ' + item.forged.level + '</span>' : '') + limitlessNote(item) + '</span>' +
      '</button>' +
      '<div class="item-equip-actions"><button class="item-favorite-button' + (isItemFavorite(item.id) ? ' is-favorite' : '') + '" type="button" data-favorite-item="' + escapeHtml(item.id) + '" aria-pressed="' + isItemFavorite(item.id) + '" aria-label="' + escapeHtml((isItemFavorite(item.id) ? 'Remove from favorites: ' : 'Favorite: ') + item.name) + '" title="' + escapeHtml((isItemFavorite(item.id) ? 'Unfavorite all copies of ' : 'Favorite all copies of ') + item.name) + '"><span aria-hidden="true">' + (isItemFavorite(item.id) ? '★' : '☆') + '</span></button>' + (item.unavailable ? "" : ownedItemActions(item, inventoryEquipSlot)) + '</div>' +
    '</article>'
  ).join("");

  ownedItemsList.querySelectorAll(".inventory-entry").forEach((entry) => {
    const item = getItem(entry.dataset.itemId) || unavailableItem(entry.dataset.itemId);
    entry.addEventListener("mouseenter", () => showItemDetails(item));
    entry.querySelector(".item-select-button").addEventListener("focus", () => showItemDetails(item));
    entry.querySelector(".item-favorite-button").addEventListener("click", () => {
      toggleItemFavorite(item.id);
      [...ownedItemsList.querySelectorAll("[data-favorite-item]")].find(button => button.dataset.favoriteItem === item.id)?.focus({ preventScroll: true });
    });
    entry.querySelectorAll(".item-equip-button").forEach((button) => {
      button.addEventListener("click", () => equipItem(item, button.dataset.equipSlot));
    });
  });
}

function renderAvailableItems() {
  if (itemPicker.hidden) return;
  const searchTerm = itemCatalogueSearchInput.value.trim().toLocaleLowerCase();
  const visible = items.filter((item) =>
    item.catalogueAvailable !== false &&
    (itemPickerFilter === "all" || item.category === itemPickerFilter) &&
    (!itemPickerSlot || itemEquipSlots(item).includes(itemPickerSlot)) &&
    (!searchTerm || item.name.toLocaleLowerCase().includes(searchTerm))
  );
  // Dev Mode can forge a random weapon of any type at the character's level.
  const forgeTypes = window.ForgeRules && ["all", "weapon"].includes(itemPickerFilter) && (!itemPickerSlot || ["primaryHand", "offHand"].includes(itemPickerSlot))
    ? ForgeRules.WEAPON_IDS.filter(type => (!searchTerm || ForgeRules.WEAPONS[type].name.toLocaleLowerCase().includes(searchTerm)) && (itemPickerSlot !== "offHand" || ForgeRules.WEAPONS[type].handed === 1)) : [];
  if (!visible.length && !forgeTypes.length) {
    const message = searchTerm
      ? 'No available items match "' + escapeHtml(itemCatalogueSearchInput.value.trim()) + '".'
      : items.length
      ? "No items match the selected slot or category."
      : "No items loaded from the item catalogue.";
    availableItemsList.innerHTML = '<div class="spell-list-empty"><span aria-hidden="true">…</span><p>' + message + '</p></div>';
    return;
  }
  const forgeEntries = forgeTypes.map(type => '<article class="available-spell available-item forged-catalogue-entry"><div><strong>Forge: ' + escapeHtml(ForgeRules.WEAPONS[type].name) + '</strong><span>Random parts and materials · Level ' + level + ' · ' + (ForgeRules.WEAPONS[type].ranged ? 'Ranged' : 'Melee') + '</span></div><button class="available-spell-add" type="button" data-forge-type="' + type + '">Forge</button></article>').join("");
  availableItemsList.innerHTML = forgeEntries + visible.map((item) =>
    '<article class="available-spell available-item ' + itemRarityClass(item) + '">' +
      '<div><strong>' + escapeHtml(item.name) + '</strong><span>' + itemRarityTag(item.rarity) + ' ' + escapeHtml(itemCategoryLabel(item.category)) + ' · ' + escapeHtml(itemTypeLabel(item.type)) + ' · ' + escapeHtml(item.shopAvailable === false ? "Not sold by shops" : itemPurchasePriceLabel(item)) + '</span></div>' +
      '<button class="available-spell-add" type="button" data-item-id="' + escapeHtml(item.id) + '">Add</button>' +
    '</article>'
  ).join("");
  availableItemsList.querySelectorAll("[data-forge-type]").forEach((button) => {
    button.addEventListener("click", () => {
      const item = getItem(ForgeRules.encode(ForgeRules.generate({ type: button.dataset.forgeType, level, source: "loot", runeFillChance: 0.3 })));
      if (!item) return;
      inventory.push(item.id);
      renderOwnedItems();
      showItemDetails(item);
      setInventoryStatus(`${item.name} forged and added to your inventory`, "success");
      saveCharacter();
    });
  });
  availableItemsList.querySelectorAll("[data-item-id]").forEach((button) => {
    button.addEventListener("click", () => {
      const item = getItem(button.dataset.itemId);
      if (!item) return;
      inventory.push(item.id);
      renderOwnedItems();
      renderAvailableItems();
      showItemDetails(item);
      setInventoryStatus(`${item.name} added to your inventory`, "success");
      saveCharacter();
    });
  });
}

// A carved rune, or an empty socket when the slot is free.
function runeIcon(filled = true) {
  return filled
    ? '<svg class="rune-icon is-filled" viewBox="0 0 16 16" aria-hidden="true"><path d="M8 1.6 L14.4 8 L8 14.4 L1.6 8Z" fill="#203a5e" stroke="#9fd0ff" stroke-width="1.1"/><path d="M8 4.2 V11.8 M8 7 L5.6 4.6 M8 7 L10.4 4.6" stroke="#dff0ff" stroke-width="1.3" fill="none" stroke-linecap="round"/></svg>'
    : '<svg class="rune-icon" viewBox="0 0 16 16" aria-hidden="true"><path d="M8 1.6 L14.4 8 L8 14.4 L1.6 8Z" fill="none" stroke="currentColor" stroke-width="1.2" stroke-dasharray="2 1.4"/></svg>';
}

function forgedLogo(makerId, className = "") {
  return window.ForgeArt?.logo(makerId, className) || "";
}

function forgedMakerMarkup(item) {
  const maker = ForgeRules.MAKERS[item.forged.maker];
  return '<div class="forged-maker">' + forgedLogo(item.forged.maker, "forged-maker-logo") + '<div><span>Manufacturer</span><strong>' + escapeHtml(maker.name) + '</strong><small>' + escapeHtml(maker.motto) + '</small></div></div>';
}

// Parts and runes are written with spans so the same markup also fits inside buttons.
// Jewels, gems and inks have no rarity; they take their element's colour instead.
function forgedPartClass(part) {
  return part.rarity ? "rarity-" + part.rarity : "spell-element-" + (part.element || "basic") + " forged-part-elemental";
}

function forgedPartsMarkup(item) {
  return '<span class="forged-parts">' + item.forged.parts.map((part, index) => {
    const info = part ? ForgeRules.describePart(item.forged, index) : null;
    if (!info) return "";
    return '<span class="forged-part ' + forgedPartClass(info) + '">' + forgedLogo(part.maker, "forged-part-logo") +
      '<span class="forged-part-text"><span class="forged-part-role">' + escapeHtml(info.shape) + (info.main ? ' · main part' : '') + '</span>' +
      '<strong>' + escapeHtml(info.material) + '</strong><small>' + escapeHtml(info.maker) + '</small>' +
      '<em>' + escapeHtml(info.effects || "No effect") + '</em></span></span>';
  }).join("") + forgedMatchingMarkup(item) + '</span>';
}

// A Matching (masterwork: Flawless) weapon's own bonus, listed after its parts.
function forgedMatchingMarkup(item) {
  const bonus = ForgeRules.matchingBonusText(item.forged);
  if (!bonus) return "";
  const maker = ForgeRules.MAKERS[ForgeRules.matchingMaker(item.forged)];
  return '<span class="forged-part forged-part-matching ' + forgedPartClass(ForgeRules.describePart(item.forged, 0)) + '">' + forgedLogo(item.forged.maker, "forged-part-logo") +
    '<span class="forged-part-text"><span class="forged-part-role">' + (item.forged.masterwork ? 'Flawless' : 'Matching') + ' bonus</span>' +
    '<strong>Every part by ' + escapeHtml(maker.name) + '</strong><em>' + escapeHtml(bonus) + '</em></span></span>';
}

function forgedRunesMarkup(item) {
  const { slots, runes } = item.forged;
  if (item.forged.caster) return '<span class="forged-runes-none">Staffs and wands cannot hold runes</span>';
  // A masterwork's fixed runes (the Masterwork Elementals) come first: part of it, on top of its slots.
  const fixed = ForgeRules.fixedRunes(item.forged).map(rune => '<span class="forged-rune is-fixed rarity-' + ForgeRules.RUNES[rune].rarity + '"><span class="forged-rune-glyph" aria-hidden="true">' + runeIcon() + '</span><span class="forged-part-text"><strong>' + escapeHtml(ForgeRules.RUNES[rune].name) + ' · fixed</strong><em>' + escapeHtml(item.forged.armor ? ForgeRules.armorRuneText(rune) : ForgeRules.runeText(rune)) + ' · part of the masterwork, cannot be removed</em></span></span>').join("");
  if (!slots) return fixed ? '<span class="forged-runes">' + fixed + '</span>' : '<span class="forged-runes-none">No rune slots</span>';
  return '<span class="forged-runes">' + fixed + Array.from({ length: slots }, (_, index) => {
    const rune = runes[index];
    return rune
      ? '<span class="forged-rune rarity-' + ForgeRules.RUNES[rune].rarity + '"><span class="forged-rune-glyph" aria-hidden="true">' + runeIcon() + '</span><span class="forged-part-text"><strong>' + escapeHtml(ForgeRules.RUNES[rune].name) + '</strong><em>' + escapeHtml(item.forged.armor ? ForgeRules.armorRuneText(rune) : ForgeRules.runeText(rune)) + '</em></span></span>'
      : '<span class="forged-rune is-empty"><span class="forged-rune-glyph" aria-hidden="true">' + runeIcon(false) + '</span><span class="forged-part-text"><strong>Empty rune slot</strong><em>Runes are slotted at a Magic Tower</em></span></span>';
  }).join("") + '</span>';
}

/* An equipment slot's pop-up opens beside the slot: to the right when it fits, else to the
   left, and below only when neither side has room. It is shifted up to stay in the window. */
function placeForgedTooltip(control) {
  const tip = control.querySelector(".forged-tooltip");
  if (!tip) return;
  const margin = 12, gap = 10;
  Object.assign(tip.style, { left: "", right: "", top: "", display: "grid", visibility: "hidden" });
  const slot = control.getBoundingClientRect(), width = tip.offsetWidth, height = tip.offsetHeight;
  const parent = tip.offsetParent || document.body, origin = parent.getBoundingClientRect();
  const fitsRight = slot.right + gap + width <= innerWidth - margin, fitsLeft = slot.left - gap - width >= margin;
  Object.assign(tip.style, { display: "", visibility: "" });
  if (!fitsRight && !fitsLeft) return;
  const left = fitsRight ? slot.right + gap : slot.left - gap - width;
  const top = Math.max(margin, Math.min(slot.top, innerHeight - margin - height));
  tip.style.left = Math.round(left - origin.left - parent.clientLeft) + "px";
  tip.style.top = Math.round(top - origin.top - parent.clientTop) + "px";
}

function forgedTooltipMarkup(item) {
  return '<span class="forged-tooltip" role="tooltip">' + '<span class="forged-tooltip-head">' + forgedLogo(item.forged.maker, "forged-part-logo") + '<span><strong>' + escapeHtml(item.name) + '</strong><small>' + escapeHtml(ForgeRules.MAKERS[item.forged.maker].name) + ' · Level ' + item.forged.level + '</small></span></span>' +
    '<span class="forged-tooltip-text">' + escapeHtml(item.description) + '</span>' +
    '<span class="forged-section-title">Parts</span>' + forgedPartsMarkup(item) +
    (item.forged.caster ? '' : '<span class="forged-section-title">Runes</span>' + forgedRunesMarkup(item)) + '</span>';
}

// Where a forged piece can be bought: masterworks are found or traded, Outlaw work only looted.
function forgedAvailability(item) {
  if (item.shopOffer) {
    const place = WorldRules.settlementsForRegion?.(item.shopOffer.regionId)?.find(settlement => settlement.id === item.shopOffer.settlementId);
    return itemPurchasePriceLabel(item) + (place ? " · " + place.name + " blacksmith" : "");
  }
  if (item.forged.masterwork) return item.soldAt || (item.rarity === "exotic" ? "Found only (Exotic, with Limitless on)" : "Found only (Masterwork)");
  return item.shopAvailable ? ShopRules.formatPrice(item.priceSilver) + (item.forged.vampiric ? " · Nocturne Keep only" : "") : "Looted only (Outlaw)";
}

function forgedDetailMarkup(item) {
  const stats = forgedItemStats(item);
  return '<div class="spell-detail item-detail forged-detail ' + itemRarityClass(item) + '">' +
    '<div class="spell-detail-heading"><div><p class="eyebrow">' + (item.forged.masterwork ? (item.rarity === 'exotic' ? 'EXOTIC ' : 'MASTERWORK ') + escapeHtml(item.forged.typeName.toUpperCase()) : item.forged.caster ? 'FORGED ' + escapeHtml(item.forged.typeName.toUpperCase()) : item.forged.armor ? 'FORGED ARMOR' : 'FORGED WEAPON') + ' · LEVEL ' + item.forged.level + '</p><h3>' + escapeHtml(item.name) + '</h3></div></div>' +
    forgedMakerMarkup(item) +
    (window.ForgeArt ? '<div class="forged-art" aria-hidden="true">' + ForgeArt.weapon(item) + '</div>' : '') +
    '<div class="spell-badges">' + itemRarityTag(item.rarity) + '<span>' + (item.forged.armor ? 'Armor' : 'Weapon') + '</span><span>' + escapeHtml(item.forged.typeName) + '</span>' + (item.forged.caster ? '' : '<span>' + escapeHtml(itemTypeLabel(item.type)) + '</span>') + '</div>' + limitlessNote(item) +
    '<div class="spell-detail-cost"><span>' + (item.shopAvailable ? 'Shop price' : 'Shop availability') + '</span><strong>' + escapeHtml(forgedAvailability(item)) + '</strong></div>' +
    '<div class="spell-detail-cost"><span>Sell price</span><strong>' + escapeHtml(ShopRules.formatPrice(ShopRules.salePrice(item.priceSilver))) + '</strong></div>' +
    '<dl class="spell-stat-details item-stat-details">' + stats.map(stat => '<div><dt' + (stat.title ? ' title="' + escapeHtml(stat.title) + '"' : '') + '>' + escapeHtml(stat.label) + '</dt><dd>' + escapeHtml(stat.value) + '</dd></div>').join("") + '</dl>' +
    '<h4 class="forged-section-title">Parts</h4>' + forgedPartsMarkup(item) +
    (item.forged.caster ? '' : '<h4 class="forged-section-title">Runes</h4>' + forgedRunesMarkup(item)) +
    '<div class="spell-description"><p>' + escapeHtml(item.description) + '</p></div>' +
    '</div>';
}

function showItemDetails(item) {
  if (!item) return;
  itemDetailPanel.innerHTML = itemDetailMarkup(item);
}

// The full description of an item, shared by the inventory and the shops.
function itemDetailMarkup(item) {
  if (item.forged) return forgedDetailMarkup(item);
  if (item.category === "rune") return runeDetailMarkup(item);
  if (item.category === "consumable") {
    const usage = item.mapAction === "camp" ? "Travel consumable · Used from the regional map" : "Consumable · 1 AP in combat";
    return '<div class="spell-detail item-detail ' + itemRarityClass(item) + '">' +
      '<div class="spell-detail-heading"><div><p class="eyebrow">CONSUMABLE</p><h3>' + escapeHtml(item.name) + '</h3></div></div>' +
      '<div class="spell-badges">' + itemRarityTag(item.rarity) + '<span>Consumable</span></div>' +
      '<p>' + usage + '</p>' +
      '<div class="spell-detail-cost"><span>' + (item.shopAvailable === false ? "Value" : "Shop price") + '</span><strong>' + escapeHtml(ShopRules.formatPrice(item.priceSilver)) + '</strong></div>' +
      '<div class="spell-detail-cost"><span>Sell price</span><strong>' + escapeHtml(ShopRules.formatPrice(ShopRules.salePrice(item.priceSilver))) + '</strong></div>' +
      '<div class="spell-description"><p>' + escapeHtml(item.description) + '</p></div></div>';
  }
  if (item.category === "key-item") {
    return '<div class="spell-detail item-detail ' + itemRarityClass(item) + '">' +
      '<div class="spell-detail-heading"><div><p class="eyebrow">ITEM DETAIL</p><h3>' + escapeHtml(item.name) + '</h3></div><span class="spell-detail-symbol" aria-hidden="true">◆</span></div>' +
      '<div class="spell-badges">' + itemRarityTag(item.rarity) + '<span>Key Item</span></div>' +
      '<div class="spell-detail-cost"><span>Availability</span><strong>Not sold by shops</strong></div>' +
      '<div class="spell-description"><p>' + escapeHtml(item.description) + '</p></div>' +
      '</div>';
  }
  if (item.unavailable) {
    return '<div class="spell-detail item-detail item-rarity-common"><div class="spell-detail-heading"><div><p class="eyebrow">ITEM DETAIL</p><h3>' + escapeHtml(item.name) + '</h3></div><span class="spell-detail-symbol" aria-hidden="true">◫</span></div><div class="spell-description"><p>' + escapeHtml(item.description) + '</p></div></div>';
  }
  const power = String(Math.floor(calculateItemPower(item)));
  const isWeapon = item.category === "weapon";
  const isArtifact = item.category === "artifact";
  const detailStats = isWeapon
    ? '<div><dt>Base power</dt><dd>' + formatValue(item.basePower || 0) + '</dd></div><div><dt>Primary stat</dt><dd>' + escapeHtml(itemStatLabel(item.primary)) + '</dd></div><div><dt>Secondary stat</dt><dd>' + escapeHtml(itemStatLabel(item.secondary)) + '</dd></div>' + (item.minimumStrength ? '<div><dt>Required Strength</dt><dd>' + item.minimumStrength + '</dd></div>' : '') + '<div><dt>Power</dt><dd>' + power + '</dd></div><div><dt title="Accuracy meets the Evasion of the target: hit chance = Accuracy ÷ (Accuracy + Evasion). Ranged attacks face half of it.">Accuracy</dt><dd>' + calculateWeaponHitChance(item) + '</dd></div><div><dt title="' + escapeHtml(critChanceTitle(item)) + '">Crit chance</dt><dd>' + calculateWeaponCritChance(item) + '%</dd></div>' + (item.effects || []).map((effect) => '<div><dt>Effect</dt><dd>' + escapeHtml(artifactEffectLabel(effect)) + '</dd></div>').join('')
    : isArtifact
    ? item.effects.map((effect) => '<div><dt>Effect</dt><dd>' + escapeHtml(artifactEffectLabel(effect)) + '</dd></div>').join("")
    : '<div><dt>Armor value</dt><dd>+' + formatValue(item.armorValue) + '</dd></div><div><dt>Evasion debuff</dt><dd>-' + formatValue(item.evasionDebuff) + '</dd></div><div><dt>Scaling stat</dt><dd>' + escapeHtml(itemStatLabel(item.scaling)) + '</dd></div><div><dt>Power</dt><dd>+' + power + '</dd></div>' + (item.effects || []).map((effect) => '<div><dt>Effect</dt><dd>' + escapeHtml(artifactEffectLabel(effect)) + '</dd></div>').join('');
  return '<div class="spell-detail item-detail ' + itemRarityClass(item) + '">' +
    '<div class="spell-detail-heading"><div><p class="eyebrow">ITEM DETAIL</p><h3>' + escapeHtml(item.name) + '</h3></div><span class="spell-detail-symbol" aria-hidden="true">◫</span></div>' +
    '<div class="spell-badges">' + itemRarityTag(item.rarity) + '<span>' + escapeHtml(itemCategoryLabel(item.category)) + '</span><span>' + escapeHtml(itemTypeLabel(item.type)) + '</span>' + (isArtifact && item.masterworkArtifactBaseId ? '<span>Level ' + escapeHtml(String(item.level)) + '</span>' : '') + '</div>' + limitlessNote(item) +
    (isArtifact && window.ForgeArt?.goods ? '<div class="forged-art artifact-art" aria-hidden="true">' + ForgeArt.goods(item) + '</div>' : '') +
    '<div class="spell-detail-cost"><span>' + (item.shopAvailable === false ? 'Shop availability' : 'Shop price') + '</span><strong>' + escapeHtml(item.shopAvailable === false ? 'Not sold by shops' : itemPurchasePriceLabel(item)) + '</strong></div>' +
    (isArtifact || item.shopAvailable === false || item.shopOffer ? '<div class="spell-detail-cost"><span>Sell price</span><strong>' + escapeHtml(ShopRules.formatPrice(ShopRules.salePrice(item.priceSilver))) + '</strong></div>' : '') +
    '<dl class="spell-stat-details item-stat-details">' + detailStats + '</dl>' +
    '<div class="spell-description"><p>' + escapeHtml(itemDescription(item)) + '</p></div>' +
    '</div>';
}

// Runes are carried until a Magic Tower runecarver fits them into a forged piece.
function runeDetailMarkup(item, { eyebrow = "RUNE" } = {}) {
  const rows = [["On weapons", item.weaponText], ["On armor", item.armorText], ["Fitting fee", ShopRules.formatPrice(item.fitPrice)], ["Removal fee", ShopRules.formatPrice(item.removalPrice)]];
  return '<div class="spell-detail item-detail rune-detail ' + itemRarityClass(item) + '">' +
    '<div class="spell-detail-heading"><div><p class="eyebrow">' + escapeHtml(eyebrow) + '</p><h3>' + escapeHtml(item.name) + '</h3></div></div>' +
    (window.ForgeArt?.rune ? '<div class="forged-art rune-art" aria-hidden="true">' + ForgeArt.rune(item.rune) + '</div>' : '') +
    '<div class="spell-badges">' + itemRarityTag(item.rarity) + '<span>Rune</span><span>' + escapeHtml(item.kindName) + '</span></div>' +
    '<div class="spell-detail-cost"><span>Tower price</span><strong>' + escapeHtml(ShopRules.formatPrice(item.priceSilver)) + '</strong></div>' +
    '<div class="spell-detail-cost"><span>Sell price</span><strong>' + escapeHtml(ShopRules.formatPrice(ShopRules.salePrice(item.priceSilver))) + '</strong></div>' +
    '<dl class="spell-stat-details item-stat-details">' + rows.map(([label, value]) => '<div><dt>' + escapeHtml(label) + '</dt><dd>' + escapeHtml(value) + '</dd></div>').join("") + '</dl>' +
    '<div class="spell-description"><p>' + escapeHtml(item.description) + ' Removing a piece\'s runes returns them to your pack.</p></div></div>';
}

function cacheItemText(kind, text) {
  try {
    localStorage.setItem(ITEM_CACHE_KEYS[kind], text);
  } catch {
    // Local storage may be unavailable in a private or restricted browser context.
  }
}

function cachedItemText(kind) {
  try {
    return localStorage.getItem(ITEM_CACHE_KEYS[kind]) || "";
  } catch {
    return "";
  }
}

function updateItemSourceStatus() {
  const artifactCount = itemCatalog.artifacts.length;
  const consumableCount = itemCatalog.consumables?.length || 0;
  const keyItemCount = itemCatalog.keyItems?.length || 0;
  itemSourceStatus.textContent = artifactCount + " artifacts · " + consumableCount + " consumables · " + keyItemCount + " key items loaded";
  artifactsFileButton.hidden = itemSourceStates.artifacts === "automatic";
}

const loadedItemText = new Map();
function applyItemText(kind, text, source = "file") {
  if (loadedItemText.get(kind) === text) {
    itemSourceStates[kind] = source;
    updateItemSourceStatus();
    return;
  }
  itemCatalog[kind] = parseItemFile(kind, text, source === "cached");
  loadedItemText.set(kind, text);
  itemSourceStates[kind] = source;
  if (source !== "cached") cacheItemText(kind, text);
  updateItemSourceStatus();
  rebuildItems();
  setInventoryStatus("Item catalogue ready", "success");
}

function openItemHandleDatabase() {
  if (!("indexedDB" in window)) return Promise.resolve(null);
  return new Promise((resolve) => {
    try {
      const request = window.indexedDB.open(ITEM_HANDLE_DB, 1);
      request.onupgradeneeded = () => request.result.createObjectStore("files");
      request.onsuccess = () => resolve(request.result);
      request.onerror = () => resolve(null);
    } catch {
      resolve(null);
    }
  });
}

async function saveItemFileHandle(kind, handle) {
  const database = await openItemHandleDatabase();
  if (!database) return;
  await new Promise((resolve) => {
    try {
      const transaction = database.transaction("files", "readwrite");
      transaction.objectStore("files").put(handle, kind);
      transaction.oncomplete = resolve;
      transaction.onerror = resolve;
    } catch {
      resolve();
    }
  });
  database.close();
}

async function readItemFileHandle(kind) {
  const database = await openItemHandleDatabase();
  if (!database) return null;
  return new Promise((resolve) => {
    try {
      const transaction = database.transaction("files", "readonly");
      const request = transaction.objectStore("files").get(kind);
      request.onsuccess = () => {
        const handle = request.result || null;
        database.close();
        resolve(handle);
      };
      request.onerror = () => {
        database.close();
        resolve(null);
      };
    } catch {
      database.close();
      resolve(null);
    }
  });
}

async function loadRememberedItemFile(kind) {
  try {
    const handle = await readItemFileHandle(kind);
    const file = await readFileFromHandle(handle);
    if (!file) return false;
    applyItemText(kind, await file.text(), "remembered");
    return true;
  } catch {
    return false;
  }
}

async function loadItemFile(kind) {
  const fileName = ITEM_FILES[kind];
  try {
    const response = await fetch(fileName, { cache: "no-cache" });
    if (!response.ok) throw new Error(`Could not read ${fileName}`);
    applyItemText(kind, await response.text(), "automatic");
    return;
  } catch (error) {
    if (await loadRememberedItemFile(kind)) return;
    const cached = cachedItemText(kind);
    if (cached) {
      try {
        applyItemText(kind, cached, "cached");
      } catch {
        // A damaged cache must not replace the last working catalog.
      }
    }
    itemSourceStates[kind] = "error";
    updateItemSourceStatus();
    rebuildItems();
    return `${ITEM_FILE_LABELS[kind]}: ${error.message}. Keeping the last working catalog, if available.`;
  }
}

async function loadCachedItems() {
  ["artifacts"].forEach((kind) => {
    const cached = cachedItemText(kind);
    if (cached) {
      try {
        applyItemText(kind, cached, "cached");
      } catch {
        // Automatic synchronization can recover from an invalid cache.
      }
    }
  });
}

async function loadItems() {
  itemSourceStatus.textContent = "Reading artifacts.json, consumables.json, and key-items.json…";
  const errors = (await Promise.all(["artifacts"].map((kind) => loadItemFile(kind)))).filter(Boolean);
  if (errors.length) setInventoryStatus(errors.join(" "), "error");
  await window.Combat?.loadConsumables();
}

let sourceSyncPromise = null;
let sourceFilesReady = false;
function synchronizeSourceFiles() {
  if (sourceSyncPromise) return sourceSyncPromise;
  sourceSyncPromise = (async () => {
    await Promise.all([loadSpells(), loadItems(), loadClasses(), loadRaces()]);
    sourceFilesReady = true;
    window.Companions?.render();
  })().finally(() => { sourceSyncPromise = null; });
  return sourceSyncPromise;
}

async function waitForSourceFiles() {
  if (sourceSyncPromise) await sourceSyncPromise;
  else if (!sourceFilesReady) await synchronizeSourceFiles();
}

async function chooseItemFile(kind) {
  const fileName = ITEM_FILE_LABELS[kind];
  const input = artifactsFileInput;
  if (!("showOpenFilePicker" in window)) {
    input.click();
    return;
  }
  try {
    const [handle] = await window.showOpenFilePicker({
      multiple: false,
      types: [{ description: `${fileName} catalogue`, accept: { "application/json": [".json"] } }],
    });
    const file = await handle.getFile();
    applyItemText(kind, await file.text(), "manual");
    await saveItemFileHandle(kind, handle);
  } catch (error) {
    if (error.name !== "AbortError") setInventoryStatus(`${fileName}: ${error.message}`, "error");
  }
}

function updateClassPointDisplay() {
  if (perkPointsValue) perkPointsValue.textContent = String(perkPoints);
  const hasPerkPoint = perkPoints > 0;
  classesOpenButton.classList.toggle("has-perk-point", hasPerkPoint);
  const label = hasPerkPoint
    ? `${perkPoints} perk point${perkPoints === 1 ? "" : "s"} available — Classes`
    : "Classes";
  classesOpenButton.title = label;
  classesOpenButton.setAttribute("aria-label", label);
}

function setClassesStatus(message, state = "") {
  classesStatus.textContent = message;
  classesStatus.className = "spellbook-status" + (state ? " is-" + state : "");
}

function setSkillsStatus(message, state = "") {
  skillsStatus.textContent = message;
  skillsStatus.className = "spellbook-status" + (state ? " is-" + state : "");
}

function setPassivesStatus(message, state = "") {
  passivesStatus.textContent = message;
  passivesStatus.className = "spellbook-status" + (state ? " is-" + state : "");
}

function classRequirementSatisfied(requiredClassId, requiredLevel) {
  const requiredClass = getClass(requiredClassId) || { id: requiredClassId, maxLevel: requiredLevel };
  if (currentClassLevel(requiredClass) >= requiredLevel) return true;
  if (requiredClassId === "paladin" && toInteger(classLevels.oathsworn, 0) >= requiredLevel) return true;
  return calculateClassEffects().classRequirementBypasses.some((bypass) =>
    bypass.requiredLevel === requiredLevel && bypass.classes.includes(requiredClassId)
  );
}

function currentResourceRequirementValues() {
  const values = currentStats();
  return {
    maxHp: calculateMaxHpWithArtifacts(values),
    maxMana: calculateMaxManaWithEffects(values),
  };
}

function companionMaximumHpForRequirement(companion) {
  return Math.max(0, toNumber(window.Combat?.previewValues?.(companion.character)?.maxHp, calculateMaxHp(companion.character.stats || defaultStats)));
}

function otherClassRequirementMet(classDefinition) {
  const requirement = classDefinition.requirements.other;
  if (!requirement) return true;
  const type = typeof requirement === "string" ? slugify(requirement) : slugify(requirement.type || "");
  if (type !== "unobtainable" && classProgress.fulfilledClassOtherRequirements?.includes(classDefinition.id)) return true;
  if (type === "non-playable-race" || type === "race-playability") {
    return Boolean(getSelectedRace() && getSelectedRace().playable === false);
  }
  if (type === "known-spells") {
    const element = String(requirement.element || "").toLowerCase();
    const requiredCount = Math.max(1, toInteger(requirement.count, 1));
    const knownCount = knownSpells.map(getSpell).filter((spell) => spell?.element === element).length;
    return knownCount >= requiredCount;
  }
  if (type === "known-summon-spells") {
    const element = String(requirement.element || "").toLowerCase();
    const requiredCount = Math.max(1, toInteger(requirement.count, 1));
    const knownCount = knownSpells.map(getSpell).filter((spell) => spell && isSummonSpell(spell) && (!element || spell.element === element)).length;
    return knownCount >= requiredCount;
  }
  if (type === "known-spell-count") return knownSpells.map(getSpell).filter(Boolean).length >= Math.max(1, toInteger(requirement.count, 1));
  if (type === "known-element-count") return new Set(knownSpells.map(getSpell).filter(Boolean).map(spell => spell.element)).size >= Math.max(1, toInteger(requirement.count, 1));
  if (type === "guild-quests-completed") return worldState.guild.completedQuests >= Math.max(1, toInteger(requirement.count, 1));
  if (type === "guild-travel-quests-completed") return worldState.guild.completedTravelQuests >= Math.max(1, toInteger(requirement.count, 1));
  if (type === "one-handed-attacks") return classProgress.oneHandedAttacks >= Math.max(1, toInteger(requirement.count, 1));
  if (type === "same-summon-casts") return Math.max(0, ...Object.values(classProgress.summonCasts)) >= Math.max(1, toInteger(requirement.count, 1));
  if (type === "loot-enemies-collected") return classProgress.lootEnemiesCollected >= Math.max(1, toInteger(requirement.count, 1));
  if (type === "companion-count") return companions.length >= Math.max(1, toInteger(requirement.count, 1));
  if (type === "equipped-artifacts") return equippedArtifactItems().length >= Math.max(1, toInteger(requirement.count, 1));
  if (type === "known-spell-count-max") return knownSpells.map(getSpell).filter(Boolean).length < Math.max(1, toInteger(requirement.count, 1));
  if (type === "class-level-count") {
    const requiredLevel = Math.max(1, toInteger(requirement.level, 1));
    const requiredCount = Math.max(1, toInteger(requirement.count, 1));
    return (requirement.classes || []).map(slugify).filter(classId => classRequirementSatisfied(classId, requiredLevel)).length >= requiredCount;
  }
  if (type === "stat-comparison") {
    const active = currentStats();
    const left = toNumber(active[requirement.left], 0), right = toNumber(active[requirement.right], 0);
    return requirement.operator === "less-than" ? left < right : left > right;
  }
  if (type === "stat-threshold") return toNumber(currentStats()[requirement.stat], 0) >= toNumber(requirement.amount, 0);
  if (type === "traps-used") return trapsUsed >= Math.max(1, toInteger(requirement.count, 1));
  if (type === "unarmed-attacks") return unarmedAttacks >= Math.max(1, toInteger(requirement.count, 1));
  if (type === "summon-spells-used") {
    const element = String(requirement.element || "").toLowerCase();
    const used = element ? toInteger(summonSpellsUsedByElement[element], 0) : summonSpellsUsed;
    return used >= Math.max(1, toInteger(requirement.count, 1));
  }
  if (type === "items-sold") return itemsSold >= Math.max(1, toInteger(requirement.count, 1));
  if (type === "solo-survivor-victories") return soloSurvivorVictories >= Math.max(1, toInteger(requirement.count, 1));
  if (type === "multi-kill-combats") return multiKillCombats >= Math.max(1, toInteger(requirement.count, 1));
  if (type === "class-kills") return Math.max(0, toInteger(classKills[slugify(requirement.classId || "")], 0)) >= Math.max(1, toInteger(requirement.count, 1));
  if (type === "race-kills") return (requirement.raceIds || []).reduce((sum, id) => sum + Math.max(0, toInteger(raceKills[slugify(id)], 0)), 0) >= Math.max(1, toInteger(requirement.count, 1));
  if (type === "defensive-actions") return defensiveActionsUsed >= Math.max(1, toInteger(requirement.count, 1));
  if (type === "full-party-defensive-actions") return companions.length >= 3 && defensiveActionsUsed >= Math.max(1, toInteger(requirement.count, 1));
  if (type === "flawless-victories") return flawlessVictories >= Math.max(1, toInteger(requirement.count, 1));
  if (type === "mana-exceeds-hp") { const resources = currentResourceRequirementValues(); return resources.maxMana > resources.maxHp; }
  if (type === "highest-stat") { const values = currentStats(); return toNumber(values[requirement.stat], 0) >= Math.max(...Object.values(values).map(value => toNumber(value, 0))); }
  if (type === "race-in-list") return (requirement.raceIds || []).map(slugify).includes(slugify(selectedRaceId || ""));
  if (type === "resource-ratio") {
    const resources = currentResourceRequirementValues();
    return resources.maxMana >= resources.maxHp * Math.max(0, toNumber(requirement.percent, 0)) / 100;
  }
  if (type === "party-highest-max-hp") {
    const ownMaximum = currentResourceRequirementValues().maxHp;
    return companions.every((companion) => ownMaximum >= companionMaximumHpForRequirement(companion));
  }
  if (type === "distance-traveled") return toNumber(worldState.distanceTraveledKm, 0) >= Math.max(0, toNumber(requirement.kilometers, 0));
  if (type === "inventory-item") return inventory.includes(String(requirement.itemId || ""));
  if (type === "race") return slugify(selectedRaceId || "") === slugify(requirement.raceId || requirement.name || "");
  if (type === "armor-condition" && requirement.condition === "no-light-or-heavy-armor") {
    return !equippedDefenseItems().some((item) => item.type === "light" || item.type === "heavy");
  }
  if (type === "equipment-condition" && requirement.condition === "heavy-armor-and-shield") {
    return getItem(equipment.armor)?.type === "heavy" && getItem(equipment.offHand)?.type === "shield";
  }
  if (type === "forbidden-known-spells") {
    const forbidden = new Set((requirement.elements || []).map((element) => String(element).toLowerCase()));
    return !knownSpells.map(getSpell).some((spell) => forbidden.has(spell?.element));
  }
  if (type === "unobtainable") return false;
  return true;
}

function otherClassRequirementLabel(classDefinition) {
  const requirement = classDefinition.requirements.other;
  if (!requirement) return "";
  if (typeof requirement === "object" && requirement.description) return String(requirement.description);
  const type = typeof requirement === "string" ? slugify(requirement) : slugify(requirement.type || "");
  if (type === "non-playable-race" || type === "race-playability") return "Non-playable race";
  if (type === "known-spells") return Math.max(1, toInteger(requirement.count, 1)) + " " + spellElementLabel(String(requirement.element || "")) + " spells known";
  if (type === "known-summon-spells") return Math.max(1, toInteger(requirement.count, 1)) + (requirement.element ? " " + spellElementLabel(requirement.element) : "") + " summon spell" + (Math.max(1, toInteger(requirement.count, 1)) === 1 ? "" : "s") + " known";
  if (type === "known-spell-count") return Math.max(1, toInteger(requirement.count, 1)) + " different spells known";
  if (type === "known-element-count") return "Spells from " + requirement.count + " different elements known";
  if (type === "guild-quests-completed") return requirement.count + " Guild quests completed";
  if (type === "guild-travel-quests-completed") return requirement.count + " Travel, Escort or Deliver Guild quests completed";
  if (type === "one-handed-attacks") return requirement.count + " one-handed weapon attacks used";
  if (type === "same-summon-casts") return "The same summon spell cast " + requirement.count + " times";
  if (type === "loot-enemies-collected") return "Loot collected from " + requirement.count + " defeated enemies";
  if (type === "companion-count") return "At least " + requirement.count + " companions";
  if (type === "equipped-artifacts") return "At least " + requirement.count + " artifacts equipped";
  if (type === "known-spell-count-max") return "Fewer than " + Math.max(1, toInteger(requirement.count, 1)) + " spells known";
  if (type === "class-level-count") return "At least " + Math.max(1, toInteger(requirement.count, 1)) + " listed classes at level " + Math.max(1, toInteger(requirement.level, 1));
  if (type === "stat-comparison") return (statDefinitions.find(({ key }) => key === requirement.left)?.label || requirement.left) + (requirement.operator === "less-than" ? " lower than " : " higher than ") + (statDefinitions.find(({ key }) => key === requirement.right)?.label || requirement.right);
  if (type === "stat-threshold") return toNumber(requirement.amount, 0) + " " + (statDefinitions.find(({ key }) => key === requirement.stat)?.label || requirement.stat);
  if (type === "traps-used") return Math.max(1, toInteger(requirement.count, 1)) + " traps used";
  if (type === "unarmed-attacks") return Math.max(1, toInteger(requirement.count, 1)) + " unarmed attacks used";
  if (type === "summon-spells-used") return Math.max(1, toInteger(requirement.count, 1)) + (requirement.element ? " " + spellElementLabel(requirement.element) : "") + " summon spells used";
  if (type === "items-sold") return Math.max(1, toInteger(requirement.count, 1)) + " items sold";
  if (type === "solo-survivor-victories") return Math.max(1, toInteger(requirement.count, 1)) + " victories with no living ally remaining";
  if (type === "multi-kill-combats") return "Kill at least " + Math.max(1, toInteger(requirement.kills, 3)) + " enemies in one combat " + Math.max(1, toInteger(requirement.count, 1)) + " times";
  if (type === "class-kills") return Math.max(1, toInteger(requirement.count, 1)) + " " + (getClass(slugify(requirement.classId || ""))?.name || requirement.classId || "class") + " enemies slain";
  if (type === "race-kills") return requirement.count + " " + (requirement.raceIds || []).join("/") + " enemies slain";
  if (type === "defensive-actions") return requirement.count + " Guard/Evade actions used";
  if (type === "full-party-defensive-actions") return "3 companions and " + requirement.count + " Guard/Evade actions used";
  if (type === "flawless-victories") return requirement.count + " combats finished without taking damage";
  if (type === "mana-exceeds-hp") return "Maximum Mana higher than maximum HP";
  if (type === "highest-stat") return (statDefinitions.find(stat => stat.key === requirement.stat)?.label || requirement.stat) + " is the highest stat";
  if (type === "race-in-list") return "Race: " + (requirement.raceIds || []).join(" or ");
  if (type === "resource-ratio") return "Maximum Mana at least " + formatValue(Math.max(0, toNumber(requirement.percent, 0))) + "% of maximum HP";
  if (type === "party-highest-max-hp") return "Most maximum HP in the party";
  if (type === "distance-traveled") return formatValue(Math.max(0, toNumber(requirement.kilometers, 0))) + " km traveled";
  if (type === "inventory-item") return getItem(requirement.itemId)?.name || String(requirement.description || "Required item");
  if (type === "race") return "Race: " + (raceCatalog.find((race) => race.id === slugify(requirement.raceId || requirement.name || ""))?.name || requirement.raceId || requirement.name || "Unknown");
  if (type === "armor-condition" && requirement.condition === "no-light-or-heavy-armor") return "No Light or Heavy Armor equipped";
  if (type === "equipment-condition" && requirement.condition === "heavy-armor-and-shield") return "Heavy Armor and a Shield equipped";
  if (type === "forbidden-known-spells") return "No " + (requirement.elements || []).map(spellElementLabel).join(" or ") + " spells known";
  if (type === "unobtainable") return "Unobtainable";
  return String(requirement);
}

function otherClassRequirementReason(classDefinition) {
  const requirement = classDefinition.requirements.other;
  if (!requirement) return "";
  const type = typeof requirement === "string" ? slugify(requirement) : slugify(requirement.type || "");
  if (type === "non-playable-race" || type === "race-playability") return "Requires a non-playable race";
  if (type === "known-spells") {
    const element = spellElementLabel(String(requirement.element || ""));
    return "Requires " + Math.max(1, toInteger(requirement.count, 1)) + " known " + element + " spells";
  }
  if (type === "known-summon-spells") {
    const element = String(requirement.element || "").toLowerCase();
    const knownCount = knownSpells.map(getSpell).filter((spell) => spell && isSummonSpell(spell) && (!element || spell.element === element)).length;
    return "Requires knowing " + Math.max(1, toInteger(requirement.count, 1)) + (element ? " " + spellElementLabel(element) : "") + " summon spell" + (Math.max(1, toInteger(requirement.count, 1)) === 1 ? "" : "s") + " (" + knownCount + " known)";
  }
  if (type === "known-spell-count") return "Requires knowing " + Math.max(1, toInteger(requirement.count, 1)) + " different spells (" + knownSpells.map(getSpell).filter(Boolean).length + " known)";
  if (["known-element-count", "guild-quests-completed", "guild-travel-quests-completed", "one-handed-attacks", "same-summon-casts", "loot-enemies-collected", "companion-count", "equipped-artifacts"].includes(type)) return "Requires " + otherClassRequirementLabel(classDefinition);
  if (type === "known-spell-count-max") return "Requires fewer than " + Math.max(1, toInteger(requirement.count, 1)) + " known spells (" + knownSpells.map(getSpell).filter(Boolean).length + " known)";
  if (type === "class-level-count") {
    const requiredLevel = Math.max(1, toInteger(requirement.level, 1));
    const requiredCount = Math.max(1, toInteger(requirement.count, 1));
    const classes = (requirement.classes || []).map(slugify);
    const currentCount = classes.filter(classId => classRequirementSatisfied(classId, requiredLevel)).length;
    return "Requires at least " + requiredCount + " of " + classes.map(classId => getClass(classId)?.name || classId).join(", ") + " at level " + requiredLevel + " (" + currentCount + "/" + requiredCount + ")";
  }
  if (type === "stat-comparison") return "Requires " + otherClassRequirementLabel(classDefinition);
  if (type === "stat-threshold") return "Requires " + otherClassRequirementLabel(classDefinition) + " (currently " + formatValue(toNumber(currentStats()[requirement.stat], 0)) + ")";
  if (type === "traps-used") return "Requires " + Math.max(1, toInteger(requirement.count, 1)) + " traps used (" + trapsUsed + " used)";
  if (type === "unarmed-attacks") return "Requires " + Math.max(1, toInteger(requirement.count, 1)) + " unarmed attacks (" + unarmedAttacks + " used)";
  if (type === "summon-spells-used") {
    const element = String(requirement.element || "").toLowerCase();
    const used = element ? toInteger(summonSpellsUsedByElement[element], 0) : summonSpellsUsed;
    return "Requires " + Math.max(1, toInteger(requirement.count, 1)) + (element ? " " + spellElementLabel(element) : "") + " summon spells used (" + used + " used)";
  }
  if (type === "items-sold") return "Requires selling " + Math.max(1, toInteger(requirement.count, 1)) + " items (" + itemsSold + " sold)";
  if (type === "solo-survivor-victories") return "Requires " + Math.max(1, toInteger(requirement.count, 1)) + " victories with no living ally remaining (" + soloSurvivorVictories + " won)";
  if (type === "multi-kill-combats") return "Requires killing at least " + Math.max(1, toInteger(requirement.kills, 3)) + " enemies in one combat " + Math.max(1, toInteger(requirement.count, 1)) + " times (" + multiKillCombats + " completed)";
  if (type === "class-kills") {
    const classId = slugify(requirement.classId || ""), current = Math.max(0, toInteger(classKills[classId], 0));
    return "Requires defeating " + Math.max(1, toInteger(requirement.count, 1)) + " " + (getClass(classId)?.name || requirement.classId || "class") + " enemies (" + current + " slain)";
  }
  if (type === "race-kills") return "Requires " + requirement.count + " kills of " + (requirement.raceIds || []).join(", ") + " (" + (requirement.raceIds || []).reduce((sum, id) => sum + Math.max(0, toInteger(raceKills[slugify(id)], 0)), 0) + " slain)";
  if (type === "defensive-actions") return "Requires Guard or Evade " + requirement.count + " times (" + defensiveActionsUsed + " used)";
  if (type === "full-party-defensive-actions") return "Requires 3 companions and Guard or Evade " + requirement.count + " times (" + companions.length + "/3 companions, " + defensiveActionsUsed + " uses)";
  if (type === "flawless-victories") return "Requires " + requirement.count + " victories without taking damage (" + flawlessVictories + " completed)";
  if (type === "mana-exceeds-hp") { const resources = currentResourceRequirementValues(); return "Requires maximum Mana higher than maximum HP (" + resources.maxMana + " Mana / " + resources.maxHp + " HP)"; }
  if (type === "highest-stat") return "Requires " + requirement.stat + " to be your highest stat";
  if (type === "race-in-list") return "Requires race " + (requirement.raceIds || []).join(" or ");
  if (type === "resource-ratio") {
    const resources = currentResourceRequirementValues();
    return "Requires maximum Mana to be at least " + formatValue(Math.max(0, toNumber(requirement.percent, 0))) + "% of maximum HP (" + resources.maxMana + " Mana / " + resources.maxHp + " HP)";
  }
  if (type === "party-highest-max-hp") {
    const ownMaximum = currentResourceRequirementValues().maxHp;
    const partyMaximum = Math.max(ownMaximum, ...companions.map(companionMaximumHpForRequirement));
    return "Requires the most maximum HP in the party (" + ownMaximum + " / " + partyMaximum + ")";
  }
  if (type === "distance-traveled") return "Requires " + formatValue(Math.max(0, toNumber(requirement.kilometers, 0))) + " km traveled (" + formatValue(toNumber(worldState.distanceTraveledKm, 0)) + " km traveled)";
  if (type === "inventory-item") return "Requires " + (getItem(requirement.itemId)?.name || String(requirement.description || "the required item"));
  if (type === "race") return "Requires race " + (raceCatalog.find((race) => race.id === slugify(requirement.raceId || requirement.name || ""))?.name || requirement.raceId || requirement.name || "Unknown");
  if (type === "armor-condition" && requirement.condition === "no-light-or-heavy-armor") return "Requires no Light or Heavy Armor to be equipped";
  if (type === "equipment-condition" && requirement.condition === "heavy-armor-and-shield") return "Requires Heavy Armor and a Shield to be equipped";
  if (type === "forbidden-known-spells") return "Requires knowing no " + (requirement.elements || []).map(spellElementLabel).join(" or ") + " spells";
  if (type === "unobtainable") return "This class cannot be acquired";
  return "Requires " + otherClassRequirementLabel(classDefinition);
}

function classRequirementsMet(classDefinition) {
  if (level < classDefinition.requirements.level) return false;
  if ((classDefinition.requirements.excludes || []).some(id => toInteger(classLevels[id], 0) > 0)) return false;
  const alternatives = Object.entries(classDefinition.requirements.anyClasses || {});
  return otherClassRequirementMet(classDefinition)
    && Object.entries(classDefinition.requirements.classes).every(([requiredClassId, requiredLevel]) => classRequirementSatisfied(requiredClassId, requiredLevel))
    && (!alternatives.length || alternatives.some(([requiredClassId, requiredLevel]) => classRequirementSatisfied(requiredClassId, requiredLevel)));
}

function classRequirementSummary(classDefinition) {
  const classRequirements = Object.entries(classDefinition.requirements.classes);
  const requirementText = classRequirements.length
    ? classRequirements.map(([requiredClassId, requiredLevel]) => {
      const requiredClass = getClass(requiredClassId);
      return (requiredClass ? requiredClass.name : requiredClassId) + " " + requiredLevel;
    }).join(", ")
    : "None";
  const other = otherClassRequirementLabel(classDefinition);
  const alternatives = Object.entries(classDefinition.requirements.anyClasses || {});
  const alternativeText = alternatives.length ? " · Any class: " + alternatives.map(([id, rank]) => (getClass(id)?.name || id) + " " + rank).join(" or ") : "";
  return "Level " + classDefinition.requirements.level + " · Classes: " + requirementText + alternativeText + (other ? " · " + other : "");
}

function classActionReason(classDefinition) {
  const currentLevel = currentClassLevel(classDefinition);
  if (currentLevel >= classDefinition.maxLevel) return "Maximum class level reached";
  if (perkPoints <= 0) return "You need a free perk point";
  if (level < classDefinition.requirements.level) return "Requires character level " + classDefinition.requirements.level;
  const exclusive = (classDefinition.requirements.excludes || []).find(id => toInteger(classLevels[id], 0) > 0);
  if (exclusive) return "Cannot be acquired together with " + (getClass(exclusive)?.name || exclusive);
  if (currentLevel === 0 && !otherClassRequirementMet(classDefinition)) return otherClassRequirementReason(classDefinition);
  const missingClass = Object.entries(classDefinition.requirements.classes).find(([requiredClassId, requiredLevel]) => !classRequirementSatisfied(requiredClassId, requiredLevel));
  if (missingClass) {
    const requiredClass = getClass(missingClass[0]);
    return "Requires " + (requiredClass ? requiredClass.name : missingClass[0]) + " level " + missingClass[1];
  }
  const alternatives = Object.entries(classDefinition.requirements.anyClasses || {});
  if (alternatives.length && !alternatives.some(([requiredClassId, requiredLevel]) => classRequirementSatisfied(requiredClassId, requiredLevel))) {
    return "Requires " + alternatives.map(([id, rank]) => (getClass(id)?.name || id) + " level " + rank).join(" or ");
  }
  return "";
}

function classEffectDescription(effect) {
  if (effect.description) return effect.description;
  const type = effect.type || effect.kind;
  if (type === "all-stats-permanent-percent") return "All basic stat permanent bonuses +" + effect.amount + "%";
  if (type === "stat-permanent-percent") return statDefinitions.find(({ key }) => key === effect.target)?.label + " permanent bonus +" + effect.amount + "%";
  if (type === "stat-percent") return (statDefinitions.find(({ key }) => key === effect.target)?.label || effect.target) + " " + (effect.amount >= 0 ? "+" : "−") + Math.abs(effect.amount) + "%";
  if (type === "conditional-stat-permanent-percent") return (statDefinitions.find(({ key }) => key === effect.target)?.label || effect.target) + " +" + effect.amount + "% while " + String(effect.condition || "condition is met").replace(/-/g, " ");
  if (type === "conditional-all-stats-permanent-percent") {
    if (effect.condition === "no-party-allies") return "All basic stats +" + effect.amount + "% while no living non-summoned ally is present";
    if (effect.condition === "hp-below-percent") return "All basic stats +" + effect.amount + "% while below " + effect.threshold + "% HP";
    return "All basic stats +" + effect.amount + "% while " + String(effect.condition || "the condition is met").replace(/-/g, " ");
  }
  if (type === "stat-permanent-percent-per-enemy") return (statDefinitions.find(({ key }) => key === effect.target)?.label || effect.target) + " +" + effect.amount + "% per enemy";
  if (type === "combat-all-stats-percent-per-gold") return "In combat, all basic stats +" + effect.amount + "% per " + effect.goldPerStep + " Gold held, up to +" + effect.maximum + "%";
  if (type === "max-hp-percent") return "Maximum HP +" + effect.amount + "%";
  if (type === "max-mana-percent") return "Maximum Mana +" + effect.amount + "%";
  if (type === "trap-limit-flat") return "Active trap limit +" + effect.amount;
  if (type === "trap-damage-percent") return "Trap damage +" + effect.amount + "%";
  if (type === "trap-armor-pierce-percent") return "Trap Armor Pierce +" + effect.amount + "%";
  if (type === "trap-retention-chance") return effect.amount + "% chance to retain a placed trap";
  if (type === "trap-additional-triggers") return "Placed traps gain " + effect.amount + " additional trigger" + (effect.amount === 1 ? "" : "s");
  if (type === "travel-speed-flat") return "Travel speed +" + effect.amount + " km/h";
  if (type === "camping-supply-retention-chance") return effect.amount + "% chance to retain Camping Supplies";
  if (type === "incoming-damage-percent") return "Incoming damage " + (effect.amount >= 0 ? "+" : "−") + Math.abs(effect.amount) + "%";
  if (type === "outgoing-damage-percent") return "Damage dealt +" + effect.amount + "%";
  if (type === "outgoing-damage-percent-per-enemy") return "Damage dealt +" + effect.amount + "% per enemy, up to " + effect.maximumEnemies + " enemies";
  if (type === "party-stat-percent") return "All living allies gain +" + effect.amount + "% " + (effect.targets || []).map(target => statDefinitions.find(({ key }) => key === target)?.label || target).join(", ");
  if (type === "weapon-armor-pierce-percent") return "Weapon Armor Pierce +" + effect.amount + "%";
  if (type === "critical-damage-percent") return "Critical-hit damage +" + effect.amount + "%";
  if (type === "equipment-armor-percent") return (effect.armorType || "Equipment") + " Armor +" + effect.amount + "%";
  if (type === "stun-immunity") return "Immune to stun";
  if (type === "action-restriction") return "May only use " + (effect.allowedActions || []).join(" or ");
  if (type === "choose-stat-permanent-percent") return "Choose " + (effect.options || []).map(option => statDefinitions.find(({ key }) => key === option)?.label || option).join(" or ") + " for a +" + effect.amount + "% permanent bonus";
  if (type === "experience-reward-multiplier") return "EXP reward ×" + effect.amount;
  if (type === "action-points-flat") return "Action Points per turn +" + effect.amount;
  if (type === "quest-money-percent") return "Quest money rewards +" + effect.amount + "%";
  if (type === "quest-reputation-percent") return "Quest Reputation rewards +" + effect.amount + "%";
  if (type === "enemy-money-percent") return "Enemy money drops +" + effect.amount + "%";
  if (type === "sale-price-percent") return "Item selling prices +" + effect.amount + "%";
  if (type === "unarmed-power-percent") return "Unarmed attack power +" + effect.amount + "%";
  if (type === "unarmed-power-flat") return "Unarmed attack base power +" + effect.amount;
  if (type === "unarmed-hit-flat") return "Unarmed Accuracy +" + effect.amount;
  if (type === "unarmed-hit-percent") return "Unarmed Accuracy +" + effect.amount + "%";
  if (type === "unarmed-scaling") return "Unarmed attacks gain " + effect.amount + "× " + (statDefinitions.find(({ key }) => key === effect.stat)?.label || effect.stat) + " scaling";
  if (type === "incoming-weapon-hit-percent") return "Incoming " + (effect.weaponType || "weapon") + " attack Accuracy " + effect.amount + "%";
  if (type === "class-requirement-bypass") return "Fulfills listed class prerequisites at level " + effect.requiredLevel;
  if (type === "conditional-evasion-debuff-reduction") return "Heavy Armor evasion debuff −" + effect.amount;
  if (type === "heavy-armor-evasion-penalty-reduction") return "Heavy Armor Evasion penalty −" + effect.amount + "%";
  if (type === "conditional-defense-flat") return (effect.target || "Defense") + " +" + effect.amount + " while " + String(effect.condition || "the condition is met").replace(/-/g, " ");
  if (type === "conditional-defense-percent") return (effect.target === "armor" ? "Armor" : effect.target === "evasion" ? "Evasion" : effect.target || "Defense") + " +" + effect.amount + "% while " + String(effect.condition || "the condition is met").replace(/-/g, " ");
  if (type === "weapon-hit-flat") return (effect.weaponType || "Weapon") + " Accuracy " + (effect.amount >= 0 ? "+" : "") + effect.amount;
  if (type === "weapon-hit-percent") return (effect.weaponType || "Weapon") + " Accuracy " + (effect.amount >= 0 ? "+" : "") + effect.amount + "%";
  if (type === "weapon-power-flat") return (effect.weaponType || "Weapon") + " power +" + effect.amount;
  if (type === "weapon-power-percent") return (effect.weaponType || "Weapon") + " power +" + effect.amount + "%";
  if (type === "weapon-damage-before-first-turn-percent") return "Weapon damage +" + effect.amount + "% before the target's first turn";
  if (type === "weapon-power-multiplier") return "Weapon power ×" + effect.amount;
  if (type === "weapon-scaling") return (effect.weaponType || "Weapon") + " gains " + effect.amount + "× " + (statDefinitions.find(({ key }) => key === effect.stat)?.label || effect.stat) + " scaling";
  if (type === "weapon-crit-flat") return (effect.weaponType || "Weapon") + " Crit Rating +" + effect.amount;
  if (type === "weapon-crit-disabled") return "This attack cannot critically hit";
  if (type === "weapon-crit-always") return "This attack is guaranteed to critically hit";
  if (type === "weapon-hit-always") return "This attack always hits";
  if (type === "defense-flat") return (effect.target || "Defense") + " " + (effect.amount >= 0 ? "+" : "") + effect.amount;
  if (type === "defense-percent" || type === "defense-permanent-percent") return (effect.target === "armor" ? "Armor" : effect.target === "evasion" ? "Evasion" : effect.target || "Defense") + (type === "defense-permanent-percent" ? " permanent bonus " : " ") + (effect.amount >= 0 ? "+" : "−") + Math.abs(effect.amount) + "%";
  if (type === "defense-cap") return (effect.target || "Defense") + " cannot exceed " + effect.amount;
  if (type === "all-stats-flat-per-level-up") return "All basic stats +" + effect.amount + " each time you level up";
  if (type === "spell-cost-percent") return (effect.summonOnly ? "Summon spell" : effect.namePrefix ? effect.namePrefix + " spell" : effect.element ? spellElementLabel(effect.element) + " spell" : effect.elements?.length ? effect.elements.map(spellElementLabel).join("/") + " spells" : "Spell") + " Mana cost " + (effect.amount >= 0 ? "−" : "+") + Math.abs(effect.amount) + "%";
  if (type === "spell-action-points-set") return (effect.summonOnly ? "Summon spell" : effect.element ? spellElementLabel(effect.element) + " spell" : "Spell") + " Action Point cost becomes " + effect.amount;
  if (type === "spell-action-points-flat") return (effect.summonOnly ? "Summon spell" : effect.element ? spellElementLabel(effect.element) + " spell" : "Spell") + " Action Point cost " + (effect.amount >= 0 ? "+" : "−") + Math.abs(effect.amount);
  if (type === "spell-cost-multiplier") return "Spell Mana cost ×" + effect.amount;
  if (type === "spell-power-percent") return (effect.element ? spellElementLabel(effect.element) + " spell" : effect.elements?.length ? effect.elements.map(spellElementLabel).join("/") + " spell" : "Spell") + " power +" + effect.amount + "%";
  if (type === "spell-damage-percent") return (effect.element ? spellElementLabel(effect.element) + " spell" : effect.elements?.length ? effect.elements.map(spellElementLabel).join("/") + " spell" : "Spell") + " damage +" + effect.amount + "%";
  if (type === "spell-add-elements") return "Eligible spells additionally count as " + (effect.elements || []).map(spellElementLabel).join(", ");
  if (type === "spell-learning-cost-percent") return (effect.element ? spellElementLabel(effect.element) + " spell" : "Spell") + " learning cost −" + effect.amount + "%";
  if (type === "incoming-spell-damage-percent") return "Incoming " + spellElementLabel(effect.element || "spell") + " spell damage " + (effect.amount >= 0 ? "+" : "−") + Math.abs(effect.amount) + "%";
  if (type === "incoming-weapon-hit-always") return "Incoming weapon attacks always hit";
  if (type === "incoming-weapon-damage-percent") return "Incoming weapon damage +" + effect.amount + "%";
  if (type === "incoming-attack-damage-percent") return "Incoming " + ((effect.attackKinds || []).join(" and ") || "attack") + " damage " + (effect.amount >= 0 ? "+" : "−") + Math.abs(effect.amount) + "%";
  if (type === "lifesteal-percent") return "Restore HP equal to " + effect.amount + "% of damage dealt";
  if (type === "target-all-stats-percent") return "Target's basic stats " + effect.amount + "% for " + effect.durationTurns + " turns";
  if (type === "blood-supplies-flat") return "Blood Supplies +" + effect.amount;
  if (type === "bonus-damage-max-mana-percent") return "Deal additional " + (effect.elements?.length ? effect.elements.map(spellElementLabel).join(" and ") : spellElementLabel(effect.element || "basic")) + " damage equal to " + effect.amount + "% maximum Mana";
  if (type === "stun") return "Stun for " + Math.max(1, toInteger(effect.durationTurns, 1)) + " turn" + (toInteger(effect.durationTurns, 1) === 1 ? "" : "s");
  if (type === "stun-chance") return effect.amount + "% chance to stun for " + Math.max(1, toInteger(effect.durationTurns, 1)) + " turn" + (toInteger(effect.durationTurns, 1) === 1 ? "" : "s");
  if (type === "steal-equipped-weapon-on-crit") return "Steal the target's equipped weapon on a critical hit";
  if (type === "set-resources-flat") return "When the effect ends, set HP to " + effect.hp + " and Mana to " + effect.mana;
  if (type === "untargetable-while-own-summon-alive") return "Cannot be attacked while one of your summons is alive";
  if (type === "summon-base-stats-percent") return "Your summons' base stats +" + effect.amount + "%";
  if (type === "additional-summon-count") return "Summon one additional creature";
  if (type === "summon-spells-treated-as-element") return "Summon spells are treated as " + spellElementLabel(effect.element);
  if (type === "masterwork-drop-chance-flat") return "Masterwork drop chance +" + effect.amount + "%";
  if (type === "all-stats-permanent-percent-per-class-level") return "All basic stat permanent bonuses +" + effect.amount + "% for each class at level " + effect.minimumLevel;
  if (type === "party-spell-learning-cost-percent") return "Party spell learning cost −" + effect.amount + "% when another party member knows it";
  if (type === "party-spell-cost-percent") return "All party members' spell Mana costs −" + effect.amount + "%";
  if (type === "combat-all-stats-percent-per-turn") return "All basic stats +" + effect.amount + "% per completed turn, up to " + effect.maximumTurns + " turns";
  if (type === "damage-percent-vs-race") return "Damage +" + effect.amount + "% against " + (effect.raceIds || [effect.raceId]).filter(Boolean).map((id) => raceCatalog.find((race) => race.id === slugify(id))?.name || id).join(" and ");
  if (type === "weapon-power-percent-vs-class") return "Weapon power +" + effect.amount + "% against " + (effect.classIds || [effect.classId]).filter(Boolean).map((id) => getClass(slugify(id))?.name || id).join(" and ");
  if (type === "skill-upgrade") return "Upgrade " + (effect.skillId || "a class skill");
  if (type === "combat-start-random-animal") return "Summon a random animal when combat starts";
  if (type === "intercept-ally-attack-chance") return effect.amount + "% chance to intercept a single-target attack against an ally";
  if (type === "cast-random-spell") return "Cast a random spell up to the highest spell tier you know";
  if (type === "start-duel") return "Enter a protected duel with the selected opponent for " + effect.durationTurns + " turns";
  if (type === "guard-ally") return "Redirect attacks against the selected ally to you for " + effect.durationTurns + " turns";
  if (type === "restore-resource-percent") return "Restore " + [effect.hp ? effect.hp + "% maximum HP" : "", effect.mana ? effect.mana + "% maximum Mana" : ""].filter(Boolean).join(" and ") + (effect.recipient === "all-allies" ? " to all living allies" : "");
  if (type === "repeat-spell") return "The next spell is cast " + Math.max(1, toInteger(effect.amount, 1)) + " additional time" + (Math.max(1, toInteger(effect.amount, 1)) === 1 ? "" : "s");
  if (type === "random-damage-percent") return "Damage has a Luck-scaled chance to gain a random Luck-scaled bonus";
  if (type === "sorcery-point-on-spell") return "Gain 1 Sorcery Point whenever you cast a spell";
  if (type === "incoming-spell-damage-percent-per-sorcery-points") return "Take " + effect.amount + "% less spell damage per " + effect.points + " Sorcery Points";
  if (type === "armor-percent-on-weapon-hit") return "Armor +" + effect.amount + "% whenever you take melee or ranged damage, up to +" + effect.maximum + "%";
  if (type === "melee-random-bonus-damage") return effect.chance + "% chance for melee attacks to deal " + effect.minimum + "–" + effect.maximum + " additional damage";
  if (type === "taunt-chance") return effect.amount + "% chance for enemies to target you";
  if (type === "mark-target") return "Marked target takes +" + effect.damagePercent + "% damage" + (effect.hitPercent ? " and grants +" + effect.hitPercent + "% Accuracy" : effect.hitFlat ? " and grants +" + effect.hitFlat + " Accuracy" : "");
  if (type === "restore-full-resources") return "Fully restore HP and Mana";
  if (type === "turn-start-restore-percent") return "At turn start, restore " + (effect.hp || 0) + "% maximum HP and " + (effect.mana || 0) + "% maximum Mana";
  if (type === "outgoing-attack-damage-percent") return "Attack and spell damage " + (effect.amount >= 0 ? "+" : "−") + Math.abs(effect.amount) + "%";
  if (type === "ki-pool-percent-level") return "Start combat with Ki equal to " + effect.amount + "% of character level, rounded down (minimum 1)";
  if (type === "ki-regen-turns") return "Restore " + effect.amount + " Ki every " + effect.turns + " personal turns, up to capacity";
  if (type === "ki-restore-mana") return "Restore " + effect.percent + "% maximum Mana" + (effect.mindRatio ? " + " + effect.mindRatio + "× Mind" : "");
  if (type === "refresh-other-skills") return "Refresh all other skills";
  if (type === "turn-start-max-hp-loss-percent") return "Lose " + effect.amount + "% maximum HP at each turn start";
  if (type === "skill") return effect.name || "Skill unlocked";
  if (type === "passive") return effect.name || "Passive unlocked";
  return type || "Effect";
}

function classLevelDescription(levelDefinition) {
  return levelDefinition.description || levelDefinition.effects.map(classEffectDescription).join(" ");
}

// Shared by the main and companion class browsers, including locked features.
function classLevelAbilityDetails(levelDefinition) {
  return (levelDefinition.effects || []).filter(effect => ["passive", "skill"].includes(effect.type)).map(ability => {
    const skill = ability.type === "skill", badges = [];
    if (ability.condition) badges.push("Condition: " + String(ability.condition).replace(/-/g, " "));
    if (ability.combatOnly) badges.push("Combat only");
    if (ability.mapOnly) badges.push("Map only");
    if (ability.target) badges.push("Target: " + String(ability.target).replace(/-/g, " "));
    if (skill) {
      const cooldown = ability.cooldown || "daily";
      badges.push(cooldown === "daily" ? (ability.cooldownDays > 1 ? "Once every " + ability.cooldownDays + " days" : "Once per day") : cooldown === "combat" ? (ability.usesPerCombat > 1 ? ability.usesPerCombat + " uses per combat" : "Once per combat") : cooldown === "none" ? "No cooldown" : cooldown === "item" ? "Item charge required" : "Cooldown: " + cooldown);
      if (ability.oncePerTurn) badges.push("Once per turn");
      badges.push((ability.actionPoints || 0) + " AP");
      if (ability.kiCost) badges.push(ability.kiCost + " Ki");
      if (ability.manaCostMaxPercent) badges.push(ability.manaCostMaxPercent + "% maximum Mana cost");
    }
    if (ability.durationTurns) badges.push(ability.durationTurns + " turn" + (ability.durationTurns === 1 ? "" : "s"));
    if (ability.durationAttacks && ability.durationSpells) badges.push("Next attack or spell");
    else if (ability.durationAttacks) badges.push("Next " + ability.durationAttacks + " attack" + (ability.durationAttacks === 1 ? "" : "s"));
    else if (ability.durationSpells) badges.push("Next " + ability.durationSpells + " spell" + (ability.durationSpells === 1 ? "" : "s"));
    const groups = [
      ["Effect", [...(ability.effects || []), ...(ability.effect ? [ability.effect] : [])]],
      ["On activation", ability.onActivateEffects || []],
      ["On hit", ability.onHitEffects || []],
      ["After attacking", ability.afterAttackEffects || []],
      ["When it ends", ability.onExpireEffects || []],
      ["Target effect", ability.targetEffects || []],
    ];
    const rows = groups.flatMap(([label, effects]) => effects.map(effect => {
      const description = classEffectDescription(effect);
      // Specialized effects are explained by the ability description; do not
      // expose raw engine identifiers when no readable formatter exists.
      if (description === (effect.type || effect.kind) || description === "Effect") return "";
      return '<li><span>' + label + ':</span> ' + escapeHtml(description) + '</li>';
    })).join("");
    return '<section class="class-feature-ability"><h4>' + (skill ? "Skill" : "Passive") + ' · ' + escapeHtml(ability.name || "Unnamed ability") + '</h4>' +
      (ability.description ? '<p>' + escapeHtml(ability.description) + '</p>' : "") +
      (badges.length ? '<p class="class-feature-meta">' + badges.map(escapeHtml).join(" · ") + '</p>' : "") +
      (rows ? '<ul class="class-feature-effects">' + rows + '</ul>' : "") + '</section>';
  }).join("");
}

function showClassDetails(classDefinition) {
  if (!classDefinition) return;
  selectedClassId = classDefinition.id;
  classesModal.querySelectorAll(".class-entry").forEach(entry => {
    const selected = entry.dataset.classId === selectedClassId;
    entry.classList.toggle("is-selected", selected);
    entry.querySelector(".class-select-button").setAttribute("aria-pressed", String(selected));
  });
  const currentLevel = currentClassLevel(classDefinition);
  const levelRows = classDefinition.levels.map((levelDefinition) => {
    const unlocked = levelDefinition.level <= currentLevel;
    return '<div class="class-level-row' + (unlocked ? " is-unlocked" : " is-locked") + '">' +
      '<div><span class="class-level-number">Level ' + levelDefinition.level + '</span><span class="class-level-state">' + (unlocked ? "Unlocked" : "Locked") + '</span></div>' +
      '<p>' + escapeHtml(classLevelDescription(levelDefinition)) + '</p>' +
      classLevelAbilityDetails(levelDefinition) +
      '</div>';
  }).join("");
  const reason = classActionReason(classDefinition);
  classDetailPanel.innerHTML = '<div class="spell-detail class-detail" data-class-color="' + escapeHtml(classDefinition.color || "") + '">' +
    '<div class="spell-detail-heading"><div><p class="eyebrow">CLASS DETAIL</p><h3>' + escapeHtml(classDefinition.name) + '</h3></div><span class="spell-detail-symbol" aria-hidden="true">♜</span></div>' +
    '<div class="spell-badges"><span>Level ' + currentLevel + " / " + classDefinition.maxLevel + '</span><span>' + escapeHtml(classRequirementSummary(classDefinition)) + '</span></div>' +
    '<div class="spell-description"><p>' + escapeHtml(reason || "This class can be advanced with a free perk point.") + '</p></div>' +
    '<div class="class-level-list">' + levelRows + '</div>' +
    '</div>';
}

function chooseBoundFamiliar() {
  const candidates = knownSpells.map(getSpell).filter(isSummonSpell);
  if (!candidates.length) {
    setClassesStatus("Learn a summon spell before binding a familiar.", "error");
    return;
  }
  const currentId = classProgress.boundFamiliar?.spellId;
  const currentIndex = Math.max(0, candidates.findIndex(spell => spell.id === currentId));
  const answer = window.prompt(`Bind a familiar for the Pactkeeper class. Choosing a different summon resets its growth:\n${candidates.map((spell, index) => `${index + 1}. ${spell.name}`).join("\n")}`, String(currentIndex + 1));
  if (answer === null) return;
  const selected = candidates[Number(answer) - 1] || candidates.find(spell => spell.name.toLowerCase() === answer.trim().toLowerCase());
  if (!selected) {
    setClassesStatus("Choose a listed summon spell.", "error");
    return;
  }
  if (selected.id !== currentId) classProgress.boundFamiliar = { spellId: selected.id, victories: 0 };
  saveCharacter();
  renderClasses();
  setClassesStatus(`${selected.name} is now your bound familiar.`);
}

// The class information tab (port task 2, G32): the classes whose information was bought from the
// guild's hooded man, each showing exactly the page that was bought, nothing more.
let classInfoMode = false, classInfoSelectedId = null;
function renderClassInformationTab() {
  const byName = (first, second) => first.name.localeCompare(second.name);
  const known = (worldState.guild.information?.revealed?.class || []).map(getClass).filter(Boolean).sort(byName);
  const list = document.querySelector("#class-info-list");
  if (!known.some(definition => definition.id === classInfoSelectedId)) classInfoSelectedId = known[0]?.id || null;
  list.innerHTML = known.length ? known.map(definition => '<article class="spell-entry class-entry class-info-entry' + (definition.id === classInfoSelectedId ? " is-selected" : "") + '" data-class-info-id="' + escapeHtml(definition.id) + '" data-class-color="' + escapeHtml(definition.color || "") + '"><button class="spell-select-button class-select-button" type="button"><span class="spell-entry-name">' + escapeHtml(definition.name) + '</span><span class="spell-entry-meta">Requirements revealed</span></button></article>').join("")
    : '<div class="spell-list-empty"><span aria-hidden="true">♜</span><p>No class information bought yet. The hooded man in a castle city\'s Adventurers Guild sells it.</p></div>';
  list.querySelectorAll("[data-class-info-id]").forEach(entry => entry.querySelector("button").addEventListener("click", () => { classInfoSelectedId = entry.dataset.classInfoId; renderClassInformationTab(); }));
  classDetailPanel.innerHTML = classInfoSelectedId && window.Guild?.classInformation ? window.Guild.classInformation(classInfoSelectedId)
    : '<div class="spell-detail-empty"><span aria-hidden="true">♜</span><p>Select a class to see the information you bought.</p></div>';
}
function syncClassInformationMode() {
  const button = document.querySelector("#class-info-button");
  button.setAttribute("aria-pressed", String(classInfoMode));
  button.textContent = classInfoMode ? "Back to classes" : "Class information";
  availableClassList.closest("section").hidden = classInfoMode;
  classList.closest("section").hidden = classInfoMode;
  document.querySelector("#class-info-panel").hidden = !classInfoMode;
}
function renderClasses() {
  updateClassPointDisplay();
  if (classesModal.hidden) return;
  syncClassInformationMode();
  if (classInfoMode) { renderClassInformationTab(); return; }
  const byName = (first, second) => first.name.localeCompare(second.name);
  const learnedClasses = classCatalog.filter(definition => currentClassLevel(definition) > 0).sort(byName);
  const availableClasses = classCatalog.filter(definition => currentClassLevel(definition) === 0 && classRequirementsMet(definition)).sort(byName);
  const classEntry = (classDefinition) => {
    const currentLevel = currentClassLevel(classDefinition);
    const reason = classActionReason(classDefinition);
    const actionLabel = currentLevel === 0 ? "Acquire" : currentLevel >= classDefinition.maxLevel ? "Max level" : "Level up";
    const disabled = Boolean(reason);
    return '<article class="spell-entry class-entry' + (currentLevel > 0 ? " is-acquired" : "") + '" data-class-id="' + escapeHtml(classDefinition.id) + '" data-class-color="' + escapeHtml(classDefinition.color || "") + '">' +
      '<button class="spell-select-button class-select-button" type="button"><span class="spell-entry-name">' + escapeHtml(classDefinition.name) + '</span><span class="spell-entry-meta">Level ' + currentLevel + " / " + classDefinition.maxLevel + '</span></button>' +
      (classDefinition.id === "pactkeeper" && currentLevel >= 3 ? '<button class="available-spell-add class-bind-button" type="button" title="' + escapeHtml(classProgress.boundFamiliar?.spellId ? `Bound: ${getSpell(classProgress.boundFamiliar.spellId)?.name || "Unknown"} · ${Math.min(20, classProgress.boundFamiliar.victories * 2)}% growth` : "Choose a bound summon") + '">Bind</button>' : "") +
      '<button class="available-spell-add class-action-button" type="button" data-class-action="' + escapeHtml(classDefinition.id) + '"' + (disabled ? ' disabled title="' + escapeHtml(reason) + '"' : "") + '>' + actionLabel + '</button>' +
      '</article>';
  };
  const renderList = (list, definitions, emptyMessage) => {
    list.innerHTML = definitions.length ? definitions.map(classEntry).join("")
      : '<div class="spell-list-empty"><span aria-hidden="true">♜</span><p>' + escapeHtml(emptyMessage) + '</p></div>';
  };
  renderList(availableClassList, availableClasses, classCatalog.length ? "No new classes currently meet their acquisition requirements." : "No class definitions loaded yet.");
  renderList(classList, learnedClasses, "Learn a class to start your roster.");
  classesModal.querySelectorAll(".class-entry").forEach((entry) => {
    const classDefinition = getClass(entry.dataset.classId);
    entry.addEventListener("mouseenter", () => showClassDetails(classDefinition));
    entry.querySelector(".class-select-button").addEventListener("focus", () => showClassDetails(classDefinition));
    entry.querySelector(".class-select-button").addEventListener("click", () => showClassDetails(classDefinition));
    entry.querySelector("[data-class-action]").addEventListener("click", () => changeClassLevel(classDefinition.id));
    entry.querySelector(".class-bind-button")?.addEventListener("click", chooseBoundFamiliar);
  });
  const visibleClasses = [...learnedClasses, ...availableClasses];
  const selected = visibleClasses.find(definition => definition.id === selectedClassId) || visibleClasses[0];
  if (selected) showClassDetails(selected);
  else {
    selectedClassId = null;
    classDetailPanel.innerHTML = '<div class="spell-detail-empty"><span aria-hidden="true">♜</span><p>Select a class to reveal its requirements and level effects.</p></div>';
  }
}

function classSpellChoiceEffect(choice = pendingClassSpellChoice) {
  const classDefinition = getClass(choice?.classId);
  return classDefinition?.levels.flatMap(levelDefinition => levelDefinition.effects || []).find(effect =>
    (effect.type || effect.kind) === "choose-random-spells" && String(effect.id || "") === choice?.effectId
  ) || null;
}

function eligibleClassChoiceSpells(effect) {
  const excluded = new Set((effect?.excludedElements || []).map(element => String(element).toLowerCase()));
  const tier = Math.max(1, toInteger(effect?.tier, 1));
  return spells.filter(spell => spell.catalogueAvailable !== false && spell.tier === tier && !excluded.has(spell.element) && !knownSpells.includes(spell.id));
}

function classSpellChoiceKey(classDefinition, effect) {
  return classDefinition.id + ":" + String(effect.id || "spell-choice");
}

function shuffled(values) {
  const result = [...values];
  for (let index = result.length - 1; index > 0; index -= 1) {
    const swapIndex = Math.floor(Math.random() * (index + 1));
    [result[index], result[swapIndex]] = [result[swapIndex], result[index]];
  }
  return result;
}

function beginClassSpellChoice(classDefinition, effect) {
  const candidates = shuffled(eligibleClassChoiceSpells(effect)).slice(0, Math.max(1, toInteger(effect.options, 5)));
  const requiredPicks = Math.min(Math.max(1, toInteger(effect.picks, 2)), candidates.length);
  if (!requiredPicks) {
    const claim = classSpellChoiceKey(classDefinition, effect);
    if (!completedClassChoices.includes(claim)) completedClassChoices.push(claim);
    setClassesStatus("No eligible unknown spells remain for " + classDefinition.name + ".", "error");
    return false;
  }
  pendingClassSpellChoice = {
    classId: classDefinition.id,
    effectId: String(effect.id || classDefinition.id + "-spell-choice"),
    candidateIds: candidates.map(spell => spell.id),
    selectedIds: [],
    requiredPicks,
    optionCount: Math.max(1, toInteger(effect.options, 5)),
  };
  resumePendingClassSpellChoice();
  return true;
}

function resumePendingClassSpellChoice() {
  if (!spells.length || !classCatalog.length) return;
  if (!pendingClassSpellChoice) {
    for (const classDefinition of classCatalog) {
      const unlockedEffects = classDefinition.levels.filter(levelDefinition => levelDefinition.level <= currentClassLevel(classDefinition)).flatMap(levelDefinition => levelDefinition.effects || []);
      const effect = unlockedEffects.find(candidate => (candidate.type || candidate.kind) === "choose-random-spells" && !completedClassChoices.includes(classSpellChoiceKey(classDefinition, candidate)));
      if (effect) {
        beginClassSpellChoice(classDefinition, effect);
        return;
      }
    }
    return;
  }
  if (!getClass(pendingClassSpellChoice.classId)) return;
  const effect = classSpellChoiceEffect();
  if (!effect) {
    pendingClassSpellChoice = null;
    classSpellChoiceModal.hidden = true;
    updateModalState();
    return;
  }
  const optionCount = Math.max(1, toInteger(pendingClassSpellChoice.optionCount, effect.options || 5));
  const candidateIds = pendingClassSpellChoice.candidateIds.filter(id => getSpell(id) && !knownSpells.includes(id));
  const candidateSet = new Set(candidateIds);
  const replacements = shuffled(eligibleClassChoiceSpells(effect).filter(spell => !candidateSet.has(spell.id)));
  while (candidateIds.length < optionCount && replacements.length) candidateIds.push(replacements.shift().id);
  pendingClassSpellChoice.candidateIds = candidateIds;
  pendingClassSpellChoice.requiredPicks = Math.min(Math.max(1, toInteger(pendingClassSpellChoice.requiredPicks, effect.picks || 2)), candidateIds.length);
  pendingClassSpellChoice.selectedIds = pendingClassSpellChoice.selectedIds.filter(id => candidateSet.has(id) || candidateIds.includes(id)).slice(0, pendingClassSpellChoice.requiredPicks);
  if (!candidateIds.length) {
    pendingClassSpellChoice = null;
    classSpellChoiceModal.hidden = true;
    updateModalState();
    return;
  }
  classesModal.hidden = true;
  classSpellChoiceModal.hidden = false;
  renderClassSpellChoice();
  updateModalState();
}

function renderClassSpellChoice() {
  if (!pendingClassSpellChoice) return;
  const selected = new Set(pendingClassSpellChoice.selectedIds);
  const required = pendingClassSpellChoice.requiredPicks;
  const candidates = pendingClassSpellChoice.candidateIds.map(getSpell).filter(Boolean);
  classSpellChoiceIntro.textContent = "Select exactly " + required + " of these " + candidates.length + " randomly offered Tier 1 spells.";
  classSpellChoiceStatus.textContent = selected.size + " / " + required + " selected";
  classSpellChoiceConfirm.disabled = selected.size !== required;
  classSpellChoiceList.innerHTML = candidates.map(spell => {
    const isSelected = selected.has(spell.id);
    const disabled = !isSelected && selected.size >= required;
    return '<button class="class-spell-choice-option spell-element-' + escapeHtml(spell.element) + (isSelected ? ' is-selected' : '') + '" type="button" data-class-spell-choice="' + escapeHtml(spell.id) + '" aria-pressed="' + isSelected + '"' + (disabled ? ' disabled' : '') + '>' +
      '<strong>' + escapeHtml(spell.name) + '</strong><span>' + escapeHtml(spellElementLabel(spell.element)) + ' · ' + escapeHtml(spellTierLabel(spell.tier)) + ' · ' + escapeHtml(spell.type) + '</span><p>' + escapeHtml(spellDescription(spell)) + '</p></button>';
  }).join("");
  classSpellChoiceList.querySelectorAll("[data-class-spell-choice]").forEach(button => button.addEventListener("click", () => {
    const id = button.dataset.classSpellChoice;
    const index = pendingClassSpellChoice.selectedIds.indexOf(id);
    if (index >= 0) pendingClassSpellChoice.selectedIds.splice(index, 1);
    else if (pendingClassSpellChoice.selectedIds.length < required) pendingClassSpellChoice.selectedIds.push(id);
    renderClassSpellChoice();
    saveCharacter();
  }));
}

function confirmClassSpellChoice() {
  if (!pendingClassSpellChoice || pendingClassSpellChoice.selectedIds.length !== pendingClassSpellChoice.requiredPicks) return;
  const learned = pendingClassSpellChoice.selectedIds.map(getSpell).filter(Boolean);
  const completedKey = pendingClassSpellChoice.classId + ":" + pendingClassSpellChoice.effectId;
  learned.forEach(spell => { if (!knownSpells.includes(spell.id)) knownSpells.push(spell.id); });
  if (!completedClassChoices.includes(completedKey)) completedClassChoices.push(completedKey);
  pendingClassSpellChoice = null;
  classSpellChoiceModal.hidden = true;
  renderKnownSpells();
  renderAvailableSpells();
  renderClasses();
  setClassesStatus("Learned " + learned.map(spell => spell.name).join(" and ") + ".", "success");
  setSpellbookStatus("Mage training added " + learned.length + " spells to your spell book.", "success");
  updateModalState();
  saveCharacter();
}

function applyClassLevelRewards(classDefinition, classLevel) {
  const levelDefinition = classDefinition.levels.find((entry) => entry.level === classLevel);
  const granted = [];
  for (const effect of levelDefinition?.effects || []) {
    const type = effect.type || effect.kind;
    if (type === "choose-random-spells") {
      beginClassSpellChoice(classDefinition, effect);
      continue;
    }
    if (type === "choose-stat-permanent-percent") {
      const choices = (effect.options || []).filter((key) => bonusDefinitions.includes(key));
      if (choices.length) {
        const labels = choices.map((key) => statDefinitions.find((stat) => stat.key === key)?.label || key);
        const response = window.prompt("Choose one stat for " + classDefinition.name + " level " + classLevel + ": " + labels.join(" or ") + ".", labels[0]);
        const normalized = slugify(response || labels[0]);
        const selectedIndex = labels.findIndex((label) => slugify(label) === normalized);
        const selected = choices[selectedIndex >= 0 ? selectedIndex : 0];
        permanentBonuses[selected] += toNumber(effect.amount, 0);
        (classProgress.statChoiceBonuses ||= {})[classDefinition.id + ":" + classLevel + ":" + (effect.id || (levelDefinition.effects || []).indexOf(effect))] = { stat: selected, amount: toNumber(effect.amount, 0) };
        granted.push((statDefinitions.find((stat) => stat.key === selected)?.label || selected) + " permanent bonus +" + toNumber(effect.amount, 0) + "%");
      }
      continue;
    }
    if (type !== "grant-and-equip-item") continue;
    const item = getItem(effect.itemId);
    if (!item) continue;
    if (!inventory.includes(item.id)) inventory.push(item.id);
    const slot = effect.slot || itemEquipSlots(item)[0];
    const candidate = candidateEquipment(item, slot);
    if (candidate && canEquipItem(item, slot)) equipment = candidate;
    granted.push(item.name);
  }
  return granted;
}

classSpellChoiceConfirm.addEventListener("click", confirmClassSpellChoice);

function consumeClassAcquisitionRequirement(classDefinition) {
  if (currentClassLevel(classDefinition) > 0) return "";
  if (classProgress.fulfilledClassOtherRequirements?.includes(classDefinition.id)) return "";
  const requirement = classDefinition.requirements.other;
  if (!requirement || slugify(requirement.type || "") !== "inventory-item" || requirement.consume !== true) return "";
  const itemIdValue = String(requirement.itemId || "");
  const itemIndex = inventory.indexOf(itemIdValue);
  if (itemIndex < 0) return "";
  inventory.splice(itemIndex, 1);
  for (const slot of equipmentSlotDefinitions) if (equipment[slot.key] === itemIdValue) equipment[slot.key] = null;
  if (typeof requirement.skillCharge === "string" && requirement.skillCharge) {
    const previous = activeSkills[requirement.skillCharge] || {};
    activeSkills[requirement.skillCharge] = { ...previous, active: false, charges: Math.max(0, toInteger(previous.charges, 0)) + 1 };
  }
  return getItem(itemIdValue)?.name || "required item";
}

function changeClassLevel(classId) {
  const classDefinition = getClass(classId);
  if (!classDefinition) return;
  const reason = classActionReason(classDefinition);
  if (reason) {
    setClassesStatus(reason, "error");
    return;
  }
  const nextLevel = currentClassLevel(classDefinition) + 1;
  const consumedRequirement = consumeClassAcquisitionRequirement(classDefinition);
  classLevels[classDefinition.id] = nextLevel;
  perkPoints -= 1;
  const grantedItems = applyClassLevelRewards(classDefinition, nextLevel);
  renderClasses();
  renderSkills();
  renderPassives();
  showClassDetails(classDefinition);
  updateDerivedValues();
  setClassesStatus(classDefinition.name + " advanced to level " + nextLevel + (consumedRequirement ? " · Consumed " + consumedRequirement : "") + (grantedItems.length ? " · Gained and equipped " + grantedItems.join(", ") : ""), "success");
  saveCharacter();
}

function resetDailyAbilities() {
  // Daily uses and ongoing skill buffs end when a new adventuring day starts.
  const resetStates = (states, skills) => {
    const definitions = new Map(skills.map((skill) => [classSkillKey(skill), skill]));
    return Object.fromEntries(Object.entries(states).filter(([key, state]) => toInteger(state?.charges, 0) > 0 || toInteger(definitions.get(key)?.cooldownDays, 1) > 1).map(([key, state]) => {
      const keepCooldown = toInteger(definitions.get(key)?.cooldownDays, 1) > 1;
      return [key, { ...state, active: false, lastUsedDay: keepCooldown ? state.lastUsedDay : "", usedThisCombat: false, usesThisCombat: 0 }];
    }));
  };
  activeSkills = resetStates(activeSkills, calculateClassEffects().skills);
  for (const companion of companions) {
    companion.character.activeSkills = resetStates(companion.character.activeSkills, window.Combat?.previewClassEffects?.(companion.character)?.skills || []);
  }
  updateDerivedValues();
  renderSkills();
  renderKnownSpells();
  renderAvailableSpells();
  skillDetailPanel.innerHTML = '<div class="spell-detail-empty"><span aria-hidden="true">✦</span><p>Select a skill to reveal its effect.</p></div>';
  setSkillsStatus("New day started · Daily skills are ready to use", "success");
  const dayStatus = document.querySelector("#day-status");
  dayStatus.textContent = "New day started. Daily skill uses restored and active skill effects ended.";
  dayStatus.hidden = false;
}

function advanceWorldTime(minutes) {
  if (!Number.isFinite(minutes) || minutes <= 0 || window.Combat?.active()) return false;
  const previousDay = WorldRules.day(worldState.minutes);
  const previousHour = Math.floor(worldState.minutes / 60);
  worldState.minutes += minutes;
  if (WorldRules.day(worldState.minutes) > previousDay) resetDailyAbilities();
  const hoursPassed = Math.floor(worldState.minutes / 60) - previousHour;
  if (hoursPassed > 0) {
    const activeValues = currentStats();
    const maxHp = calculateMaxHpWithArtifacts(activeValues);
    const maxMana = calculateMaxManaWithEffects(activeValues);
    const hpPerHour = maxHp > 0 ? Math.max(1, Math.floor(maxHp * 0.05)) : 0;
    const manaPerHour = maxMana > 0 ? Math.max(1, Math.floor(maxMana * 0.05)) : 0;
    if (!calculateRaceEffects().healingDisabled) currentHp = Math.min(maxHp, currentHp + hpPerHour * hoursPassed);
    currentMana = Math.min(maxMana, currentMana + manaPerHour * hoursPassed);
    for (const companion of companions) {
      const values = window.Combat?.previewValues?.(companion.character);
      if (!values) continue;
      companion.character.currentHp = Math.min(values.maxHp, companion.character.currentHp + Math.max(1, Math.floor(values.maxHp * .05)) * hoursPassed);
      companion.character.currentMana = Math.min(values.maxMana, companion.character.currentMana + Math.max(1, Math.floor(values.maxMana * .05)) * hoursPassed);
    }
    window.Companions?.render();
    updateDerivedValues();
  }
  window.WorldMap?.renderClock();
  window.Shop?.onTimeChange();
  return true;
}

function todayKey() {
  return "world-day-" + WorldRules.day(worldState.minutes);
}

function skillCooldownDaysRemaining(skill, state) {
  if (!state?.lastUsedDay) return 0;
  const usedDay = Math.max(0, toInteger(String(state.lastUsedDay).replace(/^world-day-/, ""), 0));
  const elapsed = WorldRules.day(worldState.minutes) - usedDay;
  return Math.max(0, Math.max(1, toInteger(skill.cooldownDays, 1)) - elapsed);
}

function skillState(skill) {
  return activeSkills[classSkillKey(skill)] || { active: false, lastUsedDay: "", usedThisCombat: false };
}

function showSkillDetails(skill) {
  if (!skill) return;
  const state = skillState(skill);
  const effectRows = [
    ...skill.effects,
    ...(skill.onActivateEffects || []),
    ...(skill.onHitEffects || []),
    ...(skill.afterAttackEffects || []),
    ...(skill.onExpireEffects || []),
  ].map((effect) => '<div><dt>Effect</dt><dd>' + escapeHtml(classEffectDescription(effect)) + '</dd></div>').join("");
  skillDetailPanel.innerHTML = '<div class="spell-detail skill-detail' + (state.active ? " is-active" : "") + '">' +
    '<div class="spell-detail-heading"><div><p class="eyebrow">SKILL DETAIL</p><h3>' + escapeHtml(skill.name) + '</h3></div><span class="spell-detail-symbol" aria-hidden="true">✦</span></div>' +
    '<div class="spell-badges"><span>' + escapeHtml(skill.className) + ' level ' + skill.level + '</span><span>' + (state.active ? "Active" : "Inactive") + '</span></div>' +
    '<dl class="spell-stat-details item-stat-details">' + effectRows + '</dl>' +
    '<div class="spell-description"><p>' + escapeHtml(skill.description || "No description provided.") + '</p></div>' +
    '</div>';
}

function renderSkills() {
  if (skillsModal.hidden) return;
  const skills = calculateClassEffects().skills;
  if (!skills.length) {
    skillList.innerHTML = '<div class="spell-list-empty"><span aria-hidden="true">✦</span><p>No skills unlocked yet.<br />Advance a class to discover skills.</p></div>';
    return;
  }
  skillList.innerHTML = skills.map((skill) => {
    const state = skillState(skill);
    const combatCooldown = skill.cooldown === "combat";
    const itemCooldown = skill.cooldown === "item";
    const combatActive = Boolean(window.Combat?.active());
    const itemReady = toInteger(state.charges, 0) > 0 || inventory.includes(skill.requiredItemId);
    const cooldownDays = skillCooldownDaysRemaining(skill, state);
    const canUse = !state.active && (itemCooldown ? combatActive && itemReady : combatCooldown ? combatActive && !state.usedThisCombat : skill.combatOnly && !combatActive ? false : cooldownDays === 0) && (!combatActive || window.Combat?.canUseSkill?.(skill) !== false);
    const actionLabel = state.active ? "Active" : canUse ? "Use" : itemCooldown ? combatActive ? "Gem required" : "Combat only" : combatCooldown ? combatActive ? "Used this combat" : "Combat only" : skill.combatOnly && !combatActive ? "Combat only" : cooldownDays > 0 ? "Ready in " + cooldownDays + (cooldownDays === 1 ? " day" : " days") : "Unavailable";
    const deactivateLabel = skill.durationAttacks && skill.durationSpells ? "Cancel" : skill.durationAttacks ? "Attack used" : skill.durationSpells || skill.durationSummonSpells ? "Spell used" : "Deactivate";
    const durationLabel = skill.durationAttacks && skill.durationSpells
      ? "Next attack or spell"
      : skill.durationAttacks
      ? "Next " + skill.durationAttacks + " attack"
      : skill.durationSpells
      ? "Next " + skill.durationSpells + " spell"
      : skill.durationSummonSpells
      ? "Next " + skill.durationSummonSpells + " summon spell"
      : (skill.durationTurns || 0) + " turns";
    const deactivateButton = state.active && skill.canDeactivate !== false ? '<button class="available-spell-add skill-deactivate-button" type="button" data-skill-action="deactivate">' + deactivateLabel + '</button>' : "";
    return '<article class="spell-entry skill-entry' + (state.active ? " is-active" : "") + '" data-skill-key="' + escapeHtml(classSkillKey(skill)) + '">' +
      '<button class="spell-select-button skill-select-button" type="button"><span class="spell-entry-name">' + escapeHtml(skill.name) + '</span><span class="spell-entry-meta">' + escapeHtml(skill.className) + " · " + (state.active ? "Active" + (skill.durationTurns || skill.durationAttacks || skill.durationSpells || skill.durationSummonSpells ? " · " + escapeHtml(durationLabel) : "") : itemCooldown ? itemReady ? "Gem charged" : "Gem required" : combatCooldown ? "Once per combat" : skill.cooldown === "none" ? "No cooldown" : "Once a day") + '</span></button>' +
      '<div class="ability-action-group"><button class="spell-use-button" type="button" data-skill-action="use"' + (canUse ? "" : " disabled") + '>' + actionLabel + '</button>' + deactivateButton + '</div>' +
      '</article>';
  }).join("");
  skillList.querySelectorAll(".skill-entry").forEach((entry) => {
    const skill = skills.find((candidate) => classSkillKey(candidate) === entry.dataset.skillKey);
    entry.addEventListener("mouseenter", () => showSkillDetails(skill));
    entry.querySelector(".skill-select-button").addEventListener("focus", () => showSkillDetails(skill));
    entry.querySelector('[data-skill-action="use"]').addEventListener("click", () => activateSkill(skill));
    const deactivateButton = entry.querySelector('[data-skill-action="deactivate"]');
    if (deactivateButton) deactivateButton.addEventListener("click", () => deactivateSkill(skill));
  });
}

function activateSkill(skill) {
  if (window.Combat?.active()) { window.Combat.skillFromSheet(skill); return; }
  if (!skill) return;
  if (skill.mapOnly) {
    window.WorldMap?.open();
    setSkillsStatus("Use " + skill.name + " from the map's tracking controls.", "success");
    return;
  }
  if (skill.cooldown === "combat" || skill.combatOnly) {
    setSkillsStatus(skill.name + " can only be used during your combat turn", "error");
    return;
  }
  const state = skillState(skill);
  if (state.active) return;
  const cooldownDays = skillCooldownDaysRemaining(skill, state);
  if (cooldownDays > 0) {
    setSkillsStatus(skill.name + " is available again in " + cooldownDays + (cooldownDays === 1 ? " day" : " days"), "error");
    return;
  }
  activeSkills[classSkillKey(skill)] = { active: !skill.instant, lastUsedDay: todayKey() };
  for (const effect of skill.onActivateEffects || []) {
    if (effect.type === "grant-camping-supplies") {
      inventory.push(...Array(Math.max(1, toInteger(effect.amount, 1))).fill("consumable-camping-supplies"));
      renderOwnedItems();
    }
  }
  renderSkills();
  showSkillDetails(skill);
  updateDerivedValues();
  const activationHint = skill.durationAttacks
    ? ' · Click "Attack used" after resolving the attack'
    : skill.durationSpells
    ? " · It will be consumed when you cast a spell"
    : "";
  setSkillsStatus(skill.name + " activated" + activationHint, "success");
  saveCharacter();
}

function deactivateSkill(skill) {
  if (!skill) return;
  const state = skillState(skill);
  activeSkills[classSkillKey(skill)] = { ...state, active: false };
  renderSkills();
  showSkillDetails(skill);
  updateDerivedValues();
  setSkillsStatus(skill.name + " deactivated", "success");
  saveCharacter();
}

function showPassiveDetails(passive) {
  if (!passive) return;
  const source = passive.sourceType === "race"
    ? `${passive.raceName} · Racial passive`
    : `${passive.className} level ${passive.level}`;
  passiveDetailPanel.innerHTML = '<div class="spell-detail passive-detail">' +
    '<div class="spell-detail-heading"><div><p class="eyebrow">PASSIVE DETAIL</p><h3>' + escapeHtml(passive.name || "Passive") + '</h3></div><span class="spell-detail-symbol" aria-hidden="true">◇</span></div>' +
    '<div class="spell-badges"><span>' + escapeHtml(source) + '</span><span>' + escapeHtml(passive.condition || "Always active") + '</span></div>' +
    '<div class="spell-description"><p>' + escapeHtml(passive.description || "No description provided.") + '</p></div>' +
    '</div>';
}

function renderPassives() {
  if (passivesModal.hidden) return;
  const passives = [...calculateRaceEffects().passives, ...calculateClassEffects().passives];
  if (!passives.length) {
    passiveList.innerHTML = '<div class="spell-list-empty"><span aria-hidden="true">◇</span><p>No passives unlocked yet.<br />Choose a race or advance a class.</p></div>';
    return;
  }
  passiveList.innerHTML = passives.map((passive, index) =>
    '<article class="spell-entry passive-entry" data-passive-index="' + index + '">' +
      '<button class="spell-select-button passive-select-button" type="button"><span class="spell-entry-name">' + escapeHtml(passive.name || "Passive") + '</span><span class="spell-entry-meta">' + escapeHtml(passive.sourceType === "race" ? `${passive.raceName} · Racial` : `${passive.className} · Level ${passive.level}`) + '</span><span class="passive-preview">' + escapeHtml(passive.description || "No description provided.") + '</span></button>' +
    '</article>'
  ).join("");
  passiveList.querySelectorAll(".passive-entry").forEach((entry, index) => {
    entry.addEventListener("mouseenter", () => showPassiveDetails(passives[index]));
    entry.querySelector(".passive-select-button").addEventListener("focus", () => showPassiveDetails(passives[index]));
  });
}

function openInventory(slotKey = null) {
  inventoryEquipSlot = slotKey;
  itemPickerSlot = slotKey;
  itemPickerFilter = "all";
  itemPickerFilterButtons.forEach((button) => {
    const isActive = button.dataset.itemPickerFilter === "all";
    button.classList.toggle("is-active", isActive);
    button.setAttribute("aria-pressed", String(isActive));
  });
  if (slotKey) {
    itemFilter = "all";
    itemFilterButtons.forEach((button) => {
      const isActive = button.dataset.itemFilter === "all";
      button.classList.toggle("is-active", isActive);
      button.setAttribute("aria-pressed", String(isActive));
    });
  }
  inventoryModal.hidden = false;
  renderEquipmentSlots();
  renderOwnedItems();
  renderAvailableItems();
  updateModalState();
  inventoryModal.querySelector(".modal-close").focus();
}

function updateModalState() {
  document.body.classList.toggle("modal-open", Boolean(document.querySelector(".modal-layer:not([hidden])")));
}

raceSelectButton.addEventListener("click", () => {
  raceModal.hidden = false;
  closeAppearanceStep();
  renderRaces();
  updateModalState();
  // A race chosen before looks existed goes straight to choosing a look.
  if (needsAppearanceChoice() && !editingEnabled) openAppearanceStep(selectedRaceId);
  else raceModal.querySelector(".modal-close").focus();
});

raceIcon.addEventListener("error", () => {
  raceIcon.hidden = true;
  raceSelectButton.querySelector(".hero-sigil-fallback").hidden = false;
});

spellbookOpenButton.addEventListener("click", () => {
  spellbookModal.hidden = false;
  renderKnownSpells();
  updateModalState();
  spellbookModal.querySelector(".modal-close").focus();
});

spellAddButton.addEventListener("click", () => {
  spellCatalogueSearchInput.value = "";
  spellPicker.hidden = false;
  renderAvailableSpells();
  updateModalState();
  spellCatalogueSearchInput.focus();
});

inventoryOpenButton.addEventListener("click", () => openInventory());

document.querySelector("#class-info-button").addEventListener("click", () => {
  classInfoMode = !classInfoMode;
  renderClasses();
});

classesOpenButton.addEventListener("click", () => {
  classesModal.hidden = false;
  classInfoMode = false;
  renderClasses();
  renderSkills();
  renderPassives();
  updateModalState();
  classesModal.querySelector(".modal-close").focus();
});

skillsOpenButton.addEventListener("click", () => {
  skillsModal.hidden = false;
  renderSkills();
  updateModalState();
  skillsModal.querySelector(".modal-close").focus();
});

passivesOpenButton.addEventListener("click", () => {
  passivesModal.hidden = false;
  renderPassives();
  updateModalState();
  passivesModal.querySelector(".modal-close").focus();
});

classRefreshButton.addEventListener("click", loadClasses);
raceRefreshButton.addEventListener("click", loadRaces);

itemAddButton.addEventListener("click", () => {
  itemPickerSlot = inventoryEquipSlot;
  itemCatalogueSearchInput.value = "";
  itemPicker.hidden = false;
  renderAvailableItems();
  updateModalState();
  itemCatalogueSearchInput.focus();
});

itemRefreshButton.addEventListener("click", loadItems);

spellCatalogueSearchInput.addEventListener("input", renderAvailableSpells);
itemCatalogueSearchInput.addEventListener("input", renderAvailableItems);

itemFilterButtons.forEach((button) => {
  button.addEventListener("click", () => {
    itemFilter = button.dataset.itemFilter;
    itemFilterButtons.forEach((filterButton) => {
      const isActive = filterButton === button;
      filterButton.classList.toggle("is-active", isActive);
      filterButton.setAttribute("aria-pressed", String(isActive));
    });
    renderOwnedItems();
  });
});

itemPickerFilterButtons.forEach((button) => {
  button.addEventListener("click", () => {
    itemPickerFilter = button.dataset.itemPickerFilter;
    itemPickerFilterButtons.forEach((filterButton) => {
      const isActive = filterButton === button;
      filterButton.classList.toggle("is-active", isActive);
      filterButton.setAttribute("aria-pressed", String(isActive));
    });
    renderAvailableItems();
  });
});

spellRefreshButton.addEventListener("click", loadSpells);

spellFilterButtons.forEach((button) => {
  button.addEventListener("click", () => {
    spellTypeFilter = button.dataset.spellFilter;
    spellFilterButtons.forEach((filterButton) => {
      const isActive = filterButton === button;
      filterButton.classList.toggle("is-active", isActive);
      filterButton.setAttribute("aria-pressed", String(isActive));
    });
    renderKnownSpells();
  });
});

spellFileButton.addEventListener("click", async () => {
  if (!("showOpenFilePicker" in window)) {
    spellFileInput.click();
    return;
  }
  try {
    const [handle] = await window.showOpenFilePicker({
      multiple: false,
      types: [{ description: "Spell catalogue", accept: { "application/json": [".json"] } }],
    });
    await saveSpellFileHandle(handle);
    const file = await handle.getFile();
    applySpellsText(await file.text(), "manual");
  } catch (error) {
    if (error.name !== "AbortError") setSpellbookStatus("That file could not be read", "error");
  }
});

spellFileInput.addEventListener("change", async (event) => {
  const [file] = event.target.files;
  if (!file) return;
  try {
    applySpellsText(await file.text(), "manual");
  } catch {
    setSpellbookStatus("That file could not be read", "error");
  } finally {
    spellFileInput.value = "";
  }
});

artifactsFileButton.addEventListener("click", () => chooseItemFile("artifacts"));

artifactsFileInput.addEventListener("change", async (event) => {
  const [file] = event.target.files;
  if (!file) return;
  try {
    applyItemText("artifacts", await file.text(), "manual");
  } catch (error) {
    setInventoryStatus(`artifacts.json: ${error.message}`, "error");
  } finally {
    artifactsFileInput.value = "";
  }
});

document.querySelectorAll("[data-close-modal]").forEach((button) => {
  button.addEventListener("click", () => {
    document.querySelector("#" + button.dataset.closeModal).hidden = true;
    updateModalState();
  });
});

document.addEventListener("keydown", (event) => {
  if (event.key !== "Escape") return;
  if (!raceModal.hidden) {
    raceModal.hidden = true;
  } else if (!passivesModal.hidden) {
    passivesModal.hidden = true;
  } else if (!skillsModal.hidden) {
    skillsModal.hidden = true;
  } else if (!classesModal.hidden) {
    classesModal.hidden = true;
  } else if (!itemPicker.hidden) {
    itemPicker.hidden = true;
  } else if (!inventoryModal.hidden) {
    inventoryModal.hidden = true;
  } else if (!spellPicker.hidden) {
    spellPicker.hidden = true;
  } else if (!spellbookModal.hidden) {
    spellbookModal.hidden = true;
  }
  updateModalState();
});

loadCachedSpells();
loadCachedItems();
loadCachedClasses();
loadCachedRaces();
updateRaceHero();
renderRaces();
renderClasses();
renderSkills();
renderPassives();
synchronizeSourceFiles();
window.addEventListener("pageshow", (event) => {
  if (event.persisted) synchronizeSourceFiles();
});
