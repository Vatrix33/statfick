// Captures game UI states: markup, styles, environment and every element's layout box.
async function uiCapture() {
  try { return await captureInner(); } catch (error) { if (String(error.message).startsWith("OUT")) throw error; throw new Error((error.stack || String(error)).slice(0, 3000)); }
}
async function captureInner() {
  const wait = ms => new Promise(resolve => setTimeout(resolve, ms));
  await waitForSourceFiles();
  const freeze = document.createElement("style");
  freeze.id = "statventure-freeze";
  freeze.textContent = "*, *::before, *::after { animation: none !important; transition: none !important; transform: none !important; translate: none !important; rotate: none !important; scale: none !important; caret-color: transparent !important; }";
  if (window.BattleStage) window.BattleStage.enabled = () => false;
  if (window.GameSettings) window.GameSettings.enemyDelay = () => 1e9;
  const F = ForgeRules;
  const sword = F.encode(F.generate({ type: "longsword", level: 30, source: "shop", random: () => 0.5 }));
  const plate = F.encode(F.generateArmor({ type: "full-plate", level: 30, source: "shop", random: () => 0.5 }));
  const c = { ...defaultCharacter(), name: "Ser Alrik", raceId: "human", level: 30, editingEnabled: false, inventory: [sword, plate], equipment: { ...defaultEquipment, primaryHand: sword, armor: plate }, knownSpells: ["spell-bolt-basic-damage"] };
  applyCharacter(normalizeCharacter(c));
  await wait(500);
  const q = v => Math.round(v * 64) / 64;
  const states = [];
  async function snap(name) {
    document.head.append(freeze);
    await wait(150);
    for (const el of document.querySelectorAll("input")) {
      if (el.type === "checkbox" || el.type === "radio") el.toggleAttribute("checked", el.checked);
      else if (el.type !== "file") el.setAttribute("value", el.value);
    }
    for (const el of document.querySelectorAll("option")) el.toggleAttribute("selected", el.selected);
    window.scrollTo(0, 0);
    // Adjacent text nodes would come back as one from the markup: merge them (layout is unchanged).
    document.body.normalize();
    // The screenshot first, the page measured straight after it (a live page may change while a screenshot is taken).
    await window.statFickDesktop.captureTestScreen("ui-" + name, innerWidth, innerHeight);
    const all = [document.documentElement, document.body, ...document.body.querySelectorAll("*")];
    const inSvg = el => el.closest("svg") && el.tagName.toLowerCase() !== "svg";
    const elements = all.map(e => {
      if (inSvg(e)) return null;
      const r = e.getBoundingClientRect();
      const row = [e.tagName.toLowerCase(), q(r.x), q(r.y), q(r.width), q(r.height)];
      if (e.scrollTop || e.scrollLeft) row.push(e.scrollLeft, e.scrollTop);
      return row;
    });
    const texts = [];
    const walker = document.createTreeWalker(document.body, NodeFilter.SHOW_TEXT);
    for (let node = walker.nextNode(); node; node = walker.nextNode()) {
      if (node.parentElement && node.parentElement.closest("svg, script, style")) { texts.push(null); continue; }
      // Empty text nodes don't survive serialization: leave them out on both sides.
      if (node.data.length === 0) continue;
      const range = document.createRange();
      range.selectNodeContents(node);
      texts.push([node.data.slice(0, 24), ...[...range.getClientRects()].map(r => [q(r.x), q(r.y), q(r.width), q(r.height)])]);
    }
    // What the pointer hits on a 10 px grid (from 5, 5): the element's index in the list above
    // (inline SVG parts count as their svg), -1 for nothing.
    const index = new Map(all.map((e, i) => [e, i]));
    const hits = [];
    for (let y = 5; y < innerHeight; y += 10)
      for (let x = 5; x < innerWidth; x += 10) {
        let hit = document.elementFromPoint(x, y);
        if (hit && inSvg(hit)) hit = hit.closest("svg");
        hits.push(hit ? (index.has(hit) ? index.get(hit) : -2) : -1);
      }
    const images = {};
    for (const img of document.querySelectorAll("img")) if (img.naturalWidth) images[img.getAttribute("src")] = [img.naturalWidth, img.naturalHeight];
    const styles = [...document.querySelectorAll("style")].filter(s => s !== freeze).map(s => s.textContent);
    const attrs = el => Object.fromEntries([...el.attributes].map(a => [a.name, a.value]));
    // The viewport unrounded (CSS px): a frame's size in whole device pixels over the device scale.
    const probe = document.createElement("div");
    probe.style.cssText = "position:fixed;left:0;top:0;width:100vw;height:100vh;visibility:hidden;pointer-events:none";
    document.documentElement.append(probe);
    const box = probe.getBoundingClientRect();
    const viewport = [box.width, box.height];
    probe.remove();
    states.push({ name, width: innerWidth, height: innerHeight, viewport, html: attrs(document.documentElement), body: attrs(document.body), markup: document.body.innerHTML, styles, images, elements, texts, hits, reducedMotion: matchMedia("(prefers-reduced-motion: reduce)").matches });
    freeze.remove();
  }
  const click = async id => { document.getElementById(id).click(); await wait(400); };
  const closeAll = async () => { for (const layer of document.querySelectorAll(".modal-layer")) layer.hidden = true; if (window.updateModalState) updateModalState(); await wait(200); };
  await snap("sheet");
  for (const [name, id] of [["inventory", "inventory-open-button"], ["spellbook", "spellbook-open-button"], ["classes", "classes-open-button"], ["skills", "skills-open-button"], ["passives", "passives-open-button"], ["settings", "settings-open-button"], ["companions", "companions-open-button"]]) {
    await click(id); await snap(name); await closeAll();
  }
  // The shop rolls its stock with Math.random as it opens: a fixed seed gives every run (the
  // captures and the layer traces are separate runs) the same stock.
  const random = Math.random;
  let seed = 0x5eed;
  Math.random = () => { seed = (seed + 0x6d2b79f5) | 0; let t = Math.imul(seed ^ (seed >>> 15), 1 | seed); t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t; return ((t ^ (t >>> 14)) >>> 0) / 4294967296; };
  try { Shop.open("blacksmith"); } finally { Math.random = random; }
  await wait(400); await snap("shop"); await closeAll();
  // The map charts the region you're in a slice per frame (port task 8: "Charting …" shown meanwhile): wait until it is done.
  await click("map-open-button");
  for (let i = 0, done = 0; i < 400 && done < 5; i++) { await wait(50); done = document.getElementById("map-charting")?.hidden ? done + 1 : 0; }
  await wait(800); await snap("map"); await closeAll();
  // startEncounter(enemyNames, openingNote, guildObjective, sparkling): no guild objective, no sparkling foes.
  await Combat.startEncounter(["Bandit", "Bandit Archer"], "UI capture"); await wait(1500); await snap("combat");
  const json = JSON.stringify({ userAgent: navigator.userAgent, devicePixelRatio, states });
  throw new Error("OUT" + json);
}
