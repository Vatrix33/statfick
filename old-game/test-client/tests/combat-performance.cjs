/* Prefer: tests/"Stat Fick Tests.AppImage" --suite=combat|enemies
 * Development: desktop/node_modules/electron/dist/electron tests/combat-performance.cjs
 * Runs in a fresh temporary profile; never reads the user's character.
 * --compare also measures the identical uncached AI and checks its decisions.
 * --all-enemies tests every enemy file, resource changes and charged off-hands.
 */
"use strict";
const fs = require("node:fs");
const path = require("node:path");
const assert = require("node:assert/strict");
const root = path.resolve(__dirname, "..");
async function runAllEnemyChecks(execute, { gameRoot: root, reportDirectory: profile, compare }) {
  const definitions = fs.readdirSync(path.join(root, "enemies"))
    .filter(file => file.endsWith(".json") && file !== "index.json")
    .sort().map(file => JSON.parse(fs.readFileSync(path.join(root, "enemies", file), "utf8")));
  const results = [];
  for (const definition of definitions) {
    await execute(`(async () => {
      const raw=${JSON.stringify(definition)};
      Combat.restore(null);
      const character={ ...defaultCharacter(), raceId:'human', level:raw.level,
        stats:Object.fromEntries(Object.keys(defaultStats).map(key=>[key,8+raw.level*2])),
        classLevels:{warrior:Math.min(5,raw.level),mage:Math.min(5,raw.level)},
        knownSpells:['spell-bolt-basic-damage','fire-bolt-fire-damage','water-bolt-water-damage','mending-spring-water-support','magic-shield-basic-support'],
        inventory:['weapon-forged-longsword-30-dwiro-hfoak-elque-s0','armor-forged-leather-armor-30-elraw-bbraw-hfraw-s0'],
        equipment:{...defaultEquipment,primaryHand:'weapon-forged-longsword-30-dwiro-hfoak-elque-s0',armor:'armor-forged-leather-armor-30-elraw-bbraw-hfraw-s0'},
        currentHp:50000,currentMana:50000 };
      applyCharacter(normalizeCharacter(character));WorldMap.setAmbientEnabled(false);
      if(!await Combat.startEncounter([raw.name]))throw new Error('Enemy did not load: '+raw.name);
      document.getElementById('combat-sheet-button').click();
      const state=Combat.serialize();state.current='enemy-1';state.ap=100;
      Combat.restore(state);
      window.__enemyOpening=Combat.serialize();
    })()`);
    const variants = ["opening", "low resources", "guard", "charged off-hand"];
    for (const variant of variants) {
      const eligible = await execute(`(() => {
        const state=structuredClone(window.__enemyOpening),enemy=state.actors.find(actor=>actor.id==='enemy-1');
        const variant=${JSON.stringify(variant)};
        if(variant==='low resources') { enemy.character.currentHp=Math.max(1,Math.floor(enemy.resourceMax.hp*.3));enemy.character.currentMana=0; }
        if(variant==='guard')enemy.effects.push({type:'stance',name:'Guard',expiresTurn:enemy.turns+1});
        if(variant==='charged off-hand') {
          const primary=getItem(enemy.character.equipment.primaryHand),offHand=getItem(enemy.character.equipment.offHand);
          const skill=Combat.previewClassEffects(enemy.character).skills.find(skill=>skill.durationAttacks);
          if(!skill||primary?.handed!==1||offHand?.handed!==1)return false;
          enemy.character.activeSkills[classSkillKey(skill)]={active:true,remainingUses:skill.durationAttacks};
        }
        Combat.restore(state);return true;
      })()`);
      if (!eligible) continue;
      const actorId = JSON.stringify("enemy-1");
      const baseline = compare ? await execute(`window.__measure(true,${actorId})`) : null;
      const optimized = await execute(`window.__measure(false,${actorId})`);
      assert(optimized.decision?.kind, definition.name + " " + variant + ": missing action");
      assert(optimized.ms < 1000, definition.name + " " + variant + ": AI stalls");
      if (baseline) assert.deepEqual(optimized.decision, baseline.decision, definition.name + " " + variant + ": cached forecasts changed the decision or score");
      results.push({ enemy: definition.name, level: definition.level, variant, optimized, baseline });
    }
    console.log("PASS", definition.name, "level", definition.level);
  }
  assert(results.some(result => result.variant === "charged off-hand"), "A charged dual-wield skill must be covered");
  const average = field => results.reduce((sum, result) => sum + (result[field]?.ms || 0), 0) / results.length;
  const report = { passed: true, enemyCount: definitions.length, cases: results.length, averageMs: average("optimized"), uncachedAverageMs: compare ? average("baseline") : null, slowestMs: Math.max(...results.map(result => result.optimized.ms)), results };
  fs.writeFileSync(path.join(profile, "all-enemies.json"), JSON.stringify(report, null, 2));
  console.log("PASS all enemies", JSON.stringify({ enemyCount: report.enemyCount, cases: report.cases, averageMs: report.averageMs, uncachedAverageMs: report.uncachedAverageMs, slowestMs: report.slowestMs }));
  console.log("Report:", path.join(profile, "all-enemies.json"));
  return report;
}
async function run({ window, gameRoot = root, reportDirectory, compare = false, allEnemies = false }) {
    const profile = reportDirectory;
    const execute = code => window.webContents.executeJavaScript(code, true);
    await execute(`(async () => {
      const wait = ms => new Promise(resolve => setTimeout(resolve, ms));
      for (let i = 0; i < 600 && !window.Music; i++) await wait(50);
      if (!window.Music) throw new Error('Game scripts did not load');
      await waitForSourceFiles(); WorldMap.setAmbientEnabled(false);
      const definition = await (await fetch('enemies/Anatemis%2C%20the%20White%20Star.json')).json();
      const character = { ...defaultCharacter(), raceId: 'human', level: 100,
        stats: Object.fromEntries(Object.keys(defaultStats).map(key => [key, 240])),
        classLevels: { ...definition.classLevels, 'raid-boss': 0 }, knownSpells: definition.knownSpells,
        inventory: ['weapon-starscale-blade-meele'], equipment: { ...defaultEquipment, primaryHand: 'weapon-starscale-blade-meele' }, currentHp: 50000, currentMana: 50000 };
      applyCharacter(normalizeCharacter(character)); WorldMap.setAmbientEnabled(false);
      if (!await Combat.startEncounter(['Anatemis, the White Star'])) throw new Error('Encounter did not start');
      document.getElementById('combat-sheet-button').click();
      await wait(0);
      window.__opening = Combat.serialize();
      window.__measure = (disable, actorId) => {
        window.__disableCombatCache = disable;
        const before = structuredClone(backupPayload().character);
        const started = performance.now();
        const boss = Combat.serialize().actors.find(actor => actor.enemyDefinitionId === definition.id);
        const decision = Combat.previewAiDecision(actorId || boss?.id);
        const ms = performance.now() - started;
        window.__disableCombatCache = false;
        const after = backupPayload().character;
        const stable = character => ({
          hp: character.currentHp, mana: character.currentMana, stats: character.stats,
          classLevels: character.classLevels, activeSkills: character.activeSkills,
          inventory: character.inventory, bonuses: character.permanentBonuses,
          current: character.combatState.current, ap: character.combatState.ap,
          actors: character.combatState.actors.map(actor => ({ id: actor.id, effects: actor.effects,
            hp: actor.character?.currentHp, mana: actor.character?.currentMana,
            bonuses: actor.character?.permanentBonuses, classes: actor.character?.classLevels,
            skills: actor.character?.activeSkills, caps: actor.resourceMax, turns: actor.turns, counter: actor.counter }))
        });
        if (JSON.stringify(stable(before)) !== JSON.stringify(stable(after))) throw new Error('AI preview changed combat resources or state');
        return { ms, decision };
      };
    })()`);
    if (allEnemies) return runAllEnemyChecks(execute, { gameRoot, reportDirectory, compare });
    const results = [];
    const cases = [
      ["opening", ""],
      ["golden phase", "boss.phaseIndex=1;boss.character.permanentBonuses.intelligence=30;boss.character.permanentBonuses.wisdom=30;boss.character.permanentDefenseBonuses.armor+=15;"],
      ["zenith phase and debuffs", "boss.phaseIndex=2;for(const key of Object.keys(boss.character.stats))boss.character.permanentBonuses[key]+=25;boss.effects.push({type:'defense-percent',target:'armor',amount:-30,name:'Armor break',expiresTurn:100},{type:'stat-percent',target:'speed',amount:-40,name:'Slow',expiresTurn:100});"],
      ["low mana and health", "boss.character.currentMana=0;boss.character.currentHp=Math.floor(boss.resourceMax.hp*.3);"],
    ];
    for (const [name, mutate] of cases) {
      await execute(`(() => { const state=structuredClone(window.__opening);const boss=state.actors.find(actor=>actor.team==='enemy');${mutate}Combat.restore(state); })()`);
      let baseline;
      if (compare) baseline = await execute("window.__measure(true)");
      const optimized = await execute("window.__measure(false)");
      assert(optimized.decision?.kind, name + ": AI can choose an action");
      assert(optimized.ms < 1000, name + ": AI stalls (" + optimized.ms + "ms)");
      if (baseline) assert.deepEqual(optimized.decision, baseline.decision, name + ": cached AI must preserve the uncached decision and score");
      results.push({ name, optimized, baseline });
      console.log("PASS", name, JSON.stringify({ optimizedMs: optimized.ms, uncachedMs: baseline?.ms, decision: optimized.decision }));
    }
    const party = await execute(`(async () => {
      Combat.restore(null);
      const character=normalizeCharacter(backupPayload().character);character.combatState=null;
      character.knownSpells=spells.map(spell=>spell.id);
      character.companions=[1,2,3].map(index=>({id:'companion-perf-'+index,name:'Test Ally '+index,character:{...character,knownSpells:window.__opening.actors[1].character.knownSpells,companions:[],combatState:null}}));
      applyCharacter(normalizeCharacter(character));WorldMap.setAmbientEnabled(false);
      await Combat.startEncounter(['Anatemis, the White Star']);
      document.getElementById('combat-sheet-button').click();
      const state=Combat.serialize(),boss=state.actors.find(actor=>actor.team==='enemy');
      for(let i=0;i<2;i++){const summon=structuredClone(state.actors.find(actor=>actor.companion));summon.id='summon-perf-'+i;summon.summoned=true;summon.companion=false;summon.summonerId='player';state.actors.push(summon);}
      Combat.restore(state);
      const result=window.__measure(false);
      return { ...result, actors:Combat.serialize().actors.length };
    })()`);
    assert.equal(party.actors, 7);
    assert(party.decision?.kind && party.ms < 1000, "Full party and summons remain responsive");
    console.log("PASS full party, all spells and summons", JSON.stringify(party));
    // Verify transformations, cinematics, Hunt progression, boss drops and
    // the Church's temporary item buffs and paid respec compatibility.
    for (const [file, name] of [["anatemis-browser.js", "runAnatemisChecks"], ["anatemis-drops-browser.js", "runAnatemisDropChecks"], ["nygheir-browser.js", "runNygheirChecks"], ["talos-boss-browser.js", "runTalosBossChecks"], ["talos-edge-browser.js", "runTalosEdgeChecks"]]) {
      await execute("Combat.restore(null); WorldMap.pause(false); document.querySelectorAll('.modal-layer').forEach(modal => { modal.hidden = true; }); updateModalState();");
      await execute(fs.readFileSync(path.join(__dirname, file), "utf8"));
      await execute(name + "()");
      console.log("PASS", name);
    }
    const report = { passed: true, results, party };
    fs.writeFileSync(path.join(profile, "results.json"), JSON.stringify(report, null, 2));
    console.log("Report:", path.join(profile, "results.json"));
    return report;
}
module.exports = { run };

// Keep the original development command usable without starting a renderer
// merely because the packaged headless runner imports this module.
if (require.main === module) {
  const { app, BrowserWindow, protocol } = require("electron");
  const os = require("node:os");
  const { createAssetHandler, ORIGIN } = require("../desktop/assets.cjs");
  const profile = fs.mkdtempSync(path.join(os.tmpdir(), "stat-fick-combat-performance-"));
  app.setPath("userData", profile);
  app.disableHardwareAcceleration();
  protocol.registerSchemesAsPrivileged([{ scheme: "statfick", privileges: { standard: true, secure: true, supportFetchAPI: true, corsEnabled: true, stream: true } }]);
  app.whenReady().then(async () => {
    const serve = createAssetHandler(() => [root]);
    protocol.handle("statfick", async request => {
      const response = await serve(request);
      if (!request.url.includes("/scripts/combat.js")) return response;
      const source = (await response.text()).replace("if (!calculationFrame ||", "if (window.__disableCombatCache || !calculationFrame ||");
      const headers = new Headers(response.headers); headers.delete("Content-Length");
      return new Response(source, { status: response.status, headers });
    });
    const window = new BrowserWindow({ show: false, skipTaskbar: true, webPreferences: { offscreen: true, nodeIntegration: false, contextIsolation: true, sandbox: true, backgroundThrottling: false } });
    window.webContents.setAudioMuted(true);
    await window.loadURL(ORIGIN + "/index.html");
    await run({ window, gameRoot: root, reportDirectory: profile, compare: process.argv.includes("--compare"), allEnemies: process.argv.includes("--all-enemies") });
    app.exit(0);
  }).catch(error => { console.error(error.stack); app.exit(1); });
}
