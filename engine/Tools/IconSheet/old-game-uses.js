// Where the old game shows each of the 28 symbols the port draws as icons (Docs/Game/Foundation.md, "The symbols as
// vector icons (GM1-7)"), with the computed style there in both themes, and what Chromium draws each with: its advance
// and ink box (the canvas measures text with the page's own font fallback) and which installed family gives the same
// pixels (window.__iconCandidates: the families fc-list names for each symbol). Run by
// Tools/Scripts/make-icon-sheet.sh --uses in the old game's headless test client (old-game/test-client, offscreen),
// whose report carries the result as its error ("OUT" + JSON), as Tests/Statventure.Tests/Reference/game-capture.js
// does; Tools/IconSheet/uses.py turns it into uses.json. The markup put into the page is the old game's own, from the
// lines each use names; nothing is saved.
async function iconProbe() {
  try { return await iconProbeInner(); } catch (error) { if (String(error.message).startsWith("OUT")) throw error; throw new Error("PROBE FAILED " + (error.stack || String(error)).slice(0, 3000)); }
}
async function iconProbeInner() {
  const wait = ms => new Promise(resolve => setTimeout(resolve, ms));
  await waitForSourceFiles();
  const freeze = document.createElement("style");
  freeze.textContent = "*, *::before, *::after { animation: none !important; transition: none !important; }";
  document.head.append(freeze);
  const CANDIDATES = window.__iconCandidates || {};
  const $id = id => document.getElementById(id);
  const add = (container, html) => { const target = typeof container === "string" ? document.querySelector(container) : container; if (!target) throw new Error("no container " + container); target.insertAdjacentHTML("beforeend", html); };
  const P = key => `data-probe="${key}"`;
  // Dynamic markup, put into its real container (the old game's own markup, from the lines named).
  add("#combat-actions", [["attack", "⚔", "Attack", "3 AP"], ["guard", "◈", "Guard", "2 AP"], ["meditate", "✺", "Meditate", "4 AP"], ["run", "↶", "Run", "1 AP"]].map(([id, s, label, cost]) =>
    `<button type="button" data-action="${id}"><span class="combat-action-icon" aria-hidden="true" ${P("action-" + id)}>${s}</span>${label}<small>${cost}</small></button>` +
    `<button type="button" data-action="${id}" disabled><span class="combat-action-icon" aria-hidden="true" ${P("action-" + id + "-disabled")}>${s}</span>${label}<small>${cost}</small></button>`).join(""));
  add("#battle-stage", `<div class="battle-actor"><div class="battle-actor-status">${["buff", "debuff", "stun", "guard", "trap", "mark"].map(kind => `<span class="battle-status kind-${kind}" ${P("status-" + kind)}></span>`).join("")}</div></div>`);
  add("#combat-opponents", `<button type="button" class="combatant combat-enemy is-unique"><span class="enemy-name" ${P("enemy-unique")}>Bandit</span></button><button type="button" class="combatant combat-enemy is-sparkling"><span class="enemy-name" ${P("enemy-sparkling")}>Bandit</span></button>`);
  add("#companion-sheet", `<fieldset class="companion-sheet-fields"><div class="companion-sheet-heading"><div class="companion-identity"><div class="companion-portrait"><span aria-hidden="true" ${P("companion-portrait")}>♟</span></div></div></div>
    <nav class="companion-sheet-nav" aria-label="Companion sheet sections"><a href="#companion-section-attributes"><span aria-hidden="true" ${P("companion-nav-attributes")}>◆</span> Attributes</a><a href="#companion-section-equipment"><span aria-hidden="true" ${P("companion-nav-equipment")}>▣</span> Equipment</a><a href="#companion-section-training"><span aria-hidden="true" ${P("companion-nav-training")}>✦</span> Training</a></nav>
    <div class="companion-resources"><article class="companion-resource-card is-hp"><div class="companion-resource-icon" aria-hidden="true" ${P("companion-hp")}>♡</div></article><article class="companion-resource-card is-mana"><div class="companion-resource-icon" aria-hidden="true" ${P("companion-mana")}>◈</div></article></div>
    <details id="companion-probe-attributes" open><summary><span><small>CORE ATTRIBUTES &amp; DEFENSES</small>The eight facets · Guard and grace</span><i aria-hidden="true" ${P("companion-details")}>⌄</i></summary>
    <div class="companion-defenses"><article class="companion-defense-card is-armor"><div class="companion-defense-icon" aria-hidden="true" ${P("companion-armor")}>▣</div></article><article class="companion-defense-card is-evasion"><div class="companion-defense-icon" aria-hidden="true" ${P("companion-evasion")}>◌</div></article></div></details>
    <div class="spell-detail item-detail item-rarity-common"><div class="spell-detail-heading"><div><p class="eyebrow">ITEM DETAIL · 1 OWNED</p><h3>Probe</h3></div><span class="spell-detail-symbol" aria-hidden="true" ${P("companion-item-detail")}>◫</span></div></div></fieldset>`);
  add("#companion-sheet", `<div class="companion-sheet-empty"><span aria-hidden="true" ${P("companion-empty")}>♟</span><p>Select a companion to see their sheet.</p></div>`);
  for (const rarity of ["common", "uncommon", "rare", "epic", "legendary", "masterwork"])
    add("#item-detail-panel", `<div class="spell-detail item-detail item-rarity-${rarity}"><div class="spell-detail-heading"><div><p class="eyebrow">ITEM DETAIL</p><h3>Probe</h3></div><span class="spell-detail-symbol" aria-hidden="true" ${P("item-detail-" + rarity)}>◫</span></div></div>`);
  add("#item-detail-panel", `<div class="spell-detail item-detail item-rarity-legendary"><div class="spell-detail-heading"><div><p class="eyebrow">ITEM DETAIL</p><h3>Key</h3></div><span class="spell-detail-symbol" aria-hidden="true" ${P("key-item-detail")}>◆</span></div></div>`);
  add("#class-detail-panel", `<div class="spell-detail class-detail" data-class-color=""><div class="spell-detail-heading"><div><p class="eyebrow">CLASS DETAIL</p><h3>Probe</h3></div><span class="spell-detail-symbol" aria-hidden="true" ${P("class-detail")}>♜</span></div></div>`);
  add("#skill-detail-panel", `<div class="spell-detail skill-detail"><div class="spell-detail-heading"><div><p class="eyebrow">SKILL DETAIL</p><h3>Probe</h3></div><span class="spell-detail-symbol" aria-hidden="true" ${P("skill-detail")}>✦</span></div></div>`);
  add("#skill-detail-panel", `<div class="spell-detail skill-detail is-active"><div class="spell-detail-heading"><div><p class="eyebrow">SKILL DETAIL</p><h3>Probe</h3></div><span class="spell-detail-symbol" aria-hidden="true" ${P("skill-detail-active")}>✦</span></div></div>`);
  add("#passive-detail-panel", `<div class="spell-detail passive-detail"><div class="spell-detail-heading"><div><p class="eyebrow">PASSIVE DETAIL</p><h3>Probe</h3></div><span class="spell-detail-symbol" aria-hidden="true" ${P("passive-detail")}>◇</span></div></div>`);
  for (const [list, s] of [["known-spells-list", "✧"], ["owned-items-list", "◫"], ["class-list", "♜"], ["class-info-list", "♜"], ["skill-list", "✦"], ["passive-list", "◇"], ["race-list", "✦"]])
    add("#" + list, `<div class="spell-list-empty"><span aria-hidden="true" ${P("list-empty-" + list)}>${s}</span><p>Empty.</p></div>`);
  add("#race-list", `<button class="race-option" style="--race-accent:${raceAccent("human")}" type="button"><span class="race-option-heading"><span class="race-option-icon"><span aria-hidden="true" ${P("race-option")}>✦</span></span><span class="race-option-title"><strong>Probe</strong><span>Playable</span></span></span></button>`);
  add("#owned-items-list", `<article class="owned-item"><div class="item-equip-actions"><button class="item-favorite-button" type="button"><span aria-hidden="true" ${P("favorite-off")}>☆</span></button><button class="item-favorite-button is-favorite" type="button"><span aria-hidden="true" ${P("favorite-on")}>★</span></button></div></article>`);
  add("#shop-stock-list", `<article class="shop-row item-rarity-common" tabindex="0"><span class="shop-row-art" aria-hidden="true"></span><span class="shop-row-main"><strong>Probe</strong><small>Common · <b class="shop-favorite-status" ${P("shop-favorite")}>★ Favorite</b> · ×1 sellable</small></span></article>`);
  add("#magic-tower-list", `<article class="shop-row spell-element-fire" tabindex="0"><span class="shop-row-art" aria-hidden="true"><span class="tower-spell-glyph" aria-hidden="true" ${P("tower-glyph-fire")}>✧</span></span></article>`);
  add("#magic-tower-list", `<article class="shop-row spell-element-frost" tabindex="0"><span class="shop-row-art" aria-hidden="true"><span class="tower-spell-glyph" aria-hidden="true" ${P("tower-glyph-frost")}>✧</span></span></article>`);
  add("#map-guild-markers", `<g class="map-guild-marker"><circle r="31" fill="transparent"/><circle r="19" fill="#c59b3c" stroke="#fff4d0" stroke-width="4"/><text y="6" text-anchor="middle" fill="#fffaf5" font-size="1.25rem" ${P("map-artifact")}>◆</text></g><g class="map-guild-marker"><circle r="31" fill="transparent"/><circle r="19" fill="#7d2135" stroke="#fff4d0" stroke-width="4"/><text y="6" text-anchor="middle" fill="#fffaf5" font-size="1.25rem" ${P("map-great-hunt")}>★</text></g>`);
  $id("map-speed-half").dataset.mapHotkey = "⇧1";
  $id("map-speed-half").setAttribute("data-probe", "map-speed-hotkey");

  // Each use: [key, symbol, source, element or selector, pseudo].
  const q = selector => document.querySelector(selector);
  const probe = key => q(`[data-probe="${key}"]`);
  const uses = [
    ["settings-button", "⚙", "index.html 24", q(".settings-button-icon")],
    ["status-trap", "⚙", "styles.css 3108", probe("status-trap"), "::before"],
    ["brand", "✦", "index.html 69", q("#editing-toggle > span")],
    ["hero-sigil", "✦", "index.html 99", q(".hero-sigil-fallback")],
    ["tool-skills", "✦", "index.html 245", q("#skills-open-button .tool-icon")],
    ["skill-detail-empty", "✦", "index.html 525", q("#skill-detail-panel .spell-detail-empty span")],
    ["skill-detail", "✦", "app.js 5596", probe("skill-detail")],
    ["skill-detail-active", "✦", "app.js 5596 (is-active)", probe("skill-detail-active")],
    ["skill-list-empty", "✦", "app.js 5607", probe("list-empty-skill-list")],
    ["race-list-empty", "✦", "app.js 2987", probe("list-empty-race-list")],
    ["race-option", "✦", "app.js 2996", probe("race-option")],
    ["companion-nav-training", "✦", "companions.js 273", probe("companion-nav-training")],
    ["enemy-sparkling", "✦", "styles.css 3463", probe("enemy-sparkling"), "::before"],
    ["tool-combat", "⚔", "index.html 240", q("#combat-open-button .tool-icon")],
    ["action-attack", "⚔", "combat.js 6245", probe("action-attack")],
    ["action-attack-disabled", "⚔", "combat.js 6245 (disabled)", probe("action-attack-disabled")],
    ["level-icon", "♜", "index.html 140", q("#level-icon")],
    ["tool-classes", "♜", "index.html 244", q("#classes-open-button .tool-icon")],
    ["class-detail-empty", "♜", "index.html 500", q("#class-detail-panel .spell-detail-empty span")],
    ["class-detail", "♜", "app.js 5220", probe("class-detail")],
    ["class-list-empty", "♜", "app.js 5291", probe("list-empty-class-list")],
    ["class-info-list-empty", "♜", "app.js 5257", probe("list-empty-class-info-list")],
    ["hp-icon", "♡", "index.html 146", q(".hp-card .resource-icon")],
    ["companion-hp", "♡", "companions.js 275", probe("companion-hp")],
    ["mana-icon", "◈", "index.html 161", q(".mana-card .resource-icon")],
    ["companion-mana", "◈", "companions.js 275", probe("companion-mana")],
    ["action-guard", "◈", "combat.js 6245", probe("action-guard")],
    ["action-guard-disabled", "◈", "combat.js 6245 (disabled)", probe("action-guard-disabled")],
    ["action-run", "↶", "combat.js 6245", probe("action-run")],
    ["action-run-disabled", "↶", "combat.js 6245 (disabled)", probe("action-run-disabled")],
    ["respec", "↺", "index.html 296 (disabled)", q("#respec-button > span")],
    ["restore-button", "↻", "index.html 136", q("#restore-button > span")],
    ["refresh-race", "↻", "index.html 314", q("#race-refresh-button")],
    ["refresh-spell", "↻", "index.html 350", q("#spell-refresh-button")],
    ["refresh-item", "↻", "index.html 408", q("#item-refresh-button")],
    ["refresh-class", "↻", "index.html 476", q("#class-refresh-button")],
    ["map-speed-hotkey", "⇧", "map.js 14, styles.css 2736", probe("map-speed-hotkey"), "::after"],
    ["companion-details", "⌄", "companions.js 276", probe("companion-details")],
    ["tool-map", "⌖", "index.html 239", q("#map-open-button .tool-icon")],
    ["defense-armor", "▣", "index.html 188", q(".armor-card .defense-icon") || document.querySelectorAll(".defense-icon")[0]],
    ["companion-armor", "▣", "companions.js 260", probe("companion-armor")],
    ["companion-nav-equipment", "▣", "companions.js 273", probe("companion-nav-equipment")],
    ["status-buff", "▲", "styles.css 3097", probe("status-buff"), "::before"],
    ["status-debuff", "▼", "styles.css 3100", probe("status-debuff"), "::before"],
    ["status-guard", "◆", "styles.css 3104", probe("status-guard"), "::before"],
    ["key-item-detail", "◆", "app.js 4511", probe("key-item-detail")],
    ["companion-nav-attributes", "◆", "companions.js 273", probe("companion-nav-attributes")],
    ["map-artifact", "◆", "map.js 545", probe("map-artifact")],
    ["tool-passives", "◇", "index.html 246", q("#passives-open-button .tool-icon")],
    ["passive-detail-empty", "◇", "index.html 550", q("#passive-detail-panel .spell-detail-empty span")],
    ["passive-detail", "◇", "app.js 5702", probe("passive-detail")],
    ["passive-list-empty", "◇", "app.js 5712", probe("list-empty-passive-list")],
    ["defense-evasion", "◌", "index.html 199", document.querySelectorAll(".defense-icon")[1]],
    ["companion-evasion", "◌", "companions.js 260", probe("companion-evasion")],
    ["status-mark", "◎", "styles.css 3110", probe("status-mark"), "::before"],
    ["tool-inventory", "◫", "index.html 242", q("#inventory-open-button .tool-icon")],
    ["item-detail-empty", "◫", "index.html 432", q("#item-detail-panel .spell-detail-empty span")],
    ...["common", "uncommon", "rare", "epic", "legendary", "masterwork"].map(r => ["item-detail-" + r, "◫", "app.js 4529 (" + r + ")", probe("item-detail-" + r)]),
    ["companion-item-detail", "◫", "companion-sheet-details.js 48", probe("companion-item-detail")],
    ["owned-items-empty", "◫", "app.js 4265", probe("list-empty-owned-items-list")],
    ["favorite-on", "★", "app.js 4274 (favourite)", probe("favorite-on")],
    ["shop-favorite", "★", "shop.js 313", probe("shop-favorite")],
    ["map-great-hunt", "★", "map.js 545", probe("map-great-hunt")],
    ["favorite-off", "☆", "app.js 4274", probe("favorite-off")],
    ["battle-log-pin", "☰", "index.html 580", q("#battle-log-pin > span")],
    ["enemy-unique", "♛", "styles.css 3461", probe("enemy-unique"), "::after"],
    ["tool-companions", "♟", "index.html 243", q("#companions-open-button .tool-icon")],
    ["companion-empty", "♟", "companions.js 245", probe("companion-empty")],
    ["companion-portrait", "♟", "companions.js 269", probe("companion-portrait")],
    ["tool-spellbook", "✧", "index.html 241", q("#spellbook-open-button .tool-icon")],
    ["spell-detail-empty", "✧", "index.html 369", q("#spell-detail-panel .spell-detail-empty span")],
    ["known-spells-empty", "✧", "app.js 2543", probe("list-empty-known-spells-list")],
    ["tower-glyph-fire", "✧", "magic-tower.js 105 (fire)", probe("tower-glyph-fire")],
    ["tower-glyph-frost", "✧", "magic-tower.js 105 (frost)", probe("tower-glyph-frost")],
    ["status-stun", "✸", "styles.css 3102", probe("status-stun"), "::before"],
    ["action-meditate", "✺", "combat.js 6245", probe("action-meditate")],
    ["action-meditate-disabled", "✺", "combat.js 6245 (disabled)", probe("action-meditate-disabled")],
  ];
  const missing = uses.filter(use => !use[3]).map(use => use[0]);
  if (missing.length) throw new Error("not found: " + missing.join(", "));
  // The brand is always lit in the port (Q11): its lit (Dev Mode on) look.
  q("#editing-toggle").classList.add("is-on");

  const parse = color => { const m = /rgba?\(([^)]+)\)/.exec(color); if (!m) return null; const p = m[1].split(/[ ,/]+/).filter(Boolean).map(Number); return [p[0], p[1], p[2], p.length > 3 ? p[3] : 1]; };
  const hex = c => "#" + c.slice(0, 3).map(v => Math.round(v).toString(16).padStart(2, "0")).join("");
  function read(el, pseudo) {
    const s = getComputedStyle(el, pseudo || null);
    // The ground behind it: colours composited up the tree to the first opaque one; images noted.
    const layers = [], images = [];
    let opacity = Number(pseudo ? s.opacity : 1), filters = [];
    for (let node = el, first = true; node && node.nodeType === 1; node = node.parentElement, first = false) {
      const ns = first && pseudo ? s : getComputedStyle(node);
      if (!first || !pseudo) opacity *= Number(ns.opacity);
      if (ns.filter !== "none") filters.push(ns.filter);
      if (node instanceof SVGElement && node.localName !== "svg") continue;
      // A gradient counts as its first colour stop (the ground right under the symbol is near enough to it).
      const layer = st => {
        const gradient = /gradient\(/.test(st.backgroundImage) ? (st.backgroundImage.match(/rgba?\([^)]+\)/g) || []).map(parse) : [];
        const opaqueStop = gradient.find(c => c && c[3] > 0.5);
        if (opaqueStop) { layers.push(opaqueStop); if (opaqueStop[3] >= 1) return true; }
        const bg = parse(st.backgroundColor);
        if (bg && bg[3] > 0) { layers.push(bg); if (bg[3] >= 1) return true; }
        return false;
      };
      if (ns.backgroundImage !== "none") images.push({ at: node.id ? "#" + node.id : node.className?.baseVal ?? node.className, image: ns.backgroundImage.slice(0, 300) });
      if (layer(ns)) break;
      if (first && pseudo) { const hs = getComputedStyle(el); opacity *= Number(hs.opacity); if (hs.backgroundImage !== "none") images.push({ at: "host", image: hs.backgroundImage.slice(0, 300) }); if (layer(hs)) break; }
    }
    let ground = [255, 255, 255];
    for (const layer of layers.reverse()) ground = ground.map((v, i) => v * (1 - layer[3]) + layer[i] * layer[3]);
    const color = el instanceof SVGElement ? s.fill : s.color;
    return {
      color: parse(color) ? hex(parse(color)) + (parse(color)[3] < 1 ? " a" + parse(color)[3] : "") : color,
      fillColor: s.webkitTextFillColor !== s.color && !(el instanceof SVGElement) ? s.webkitTextFillColor : undefined,
      fontSize: s.fontSize, fontWeight: s.fontWeight, fontStyle: s.fontStyle, fontFamily: s.fontFamily, lineHeight: s.lineHeight,
      opacity: Math.round(opacity * 1000) / 1000, ground: hex(ground), images: images.length ? images : undefined,
      textShadow: s.textShadow !== "none" ? s.textShadow : undefined, transform: s.transform !== "none" ? s.transform : undefined,
      filters: filters.length ? filters : undefined, display: s.display,
    };
  }
  const results = [];
  for (const [key, sym, source, el, pseudo] of uses) results.push({ key, sym, source, pseudo, light: read(el, pseudo) });
  document.documentElement.classList.add("dark-mode");
  await wait(100);
  for (let i = 0; i < uses.length; i++) results[i].dark = read(uses[i][3], uses[i][4]);
  document.documentElement.classList.remove("dark-mode");
  // The narrow layout (below 840 px) shows the field tools toggle.
  await window.statFickDesktop.captureTestScreen("probe-narrow", 800, 950);
  await wait(300);
  const toggle = q("#field-tools-toggle > span");
  const narrow = { key: "field-tools-toggle", sym: "☰", source: "index.html 21 (windows under 840 px)", light: read(toggle) };
  document.documentElement.classList.add("dark-mode");
  await wait(100);
  narrow.dark = read(toggle);
  document.documentElement.classList.remove("dark-mode");
  results.push(narrow);
  narrow.viewport = [innerWidth, innerHeight];

  // Which font Chromium draws each symbol with: the canvas draws text with the same font fallback as the page; the
  // symbol in the use's font is compared pixel for pixel with it in each installed family that has the character.
  const canvas = document.createElement("canvas");
  canvas.width = 420; canvas.height = 420;
  const ctx = canvas.getContext("2d", { willReadFrequently: true });
  const draw = (font, text) => { ctx.clearRect(0, 0, 420, 420); ctx.font = font; ctx.fillStyle = "#000"; ctx.textBaseline = "alphabetic"; ctx.fillText(text, 110, 290); return ctx.getImageData(0, 0, 420, 420).data; };
  const same = (a, b) => { if (a.length !== b.length) return false; for (let i = 3; i < a.length; i += 4) if (a[i] !== b[i]) return false; return true; };
  const inked = a => { for (let i = 3; i < a.length; i += 4) if (a[i]) return true; return false; };
  const fonts = {};
  for (const result of results) {
    for (const theme of ["light", "dark"]) {
      const st = result[theme];
      const key = [result.sym, st.fontStyle, st.fontWeight, st.fontFamily].join("|");
      st.fontKey = key;
      if (fonts[key]) continue;
      const style = `${st.fontStyle} ${st.fontWeight} `;
      const reference = draw(style + "200px " + st.fontFamily, result.sym);
      // A family name the page can't resolve falls back as the reference may: such a name is left out.
      const unresolved = draw(style + `200px "No Such Family 4711"`, result.sym);
      const unresolvedText = draw(style + `200px "No Such Family 4711"`, "Hamburgefonstiv");
      const matches = [], families = [];
      for (const family of CANDIDATES[result.sym] || []) {
        const candidate = draw(style + `200px "${family}"`, result.sym);
        const resolved = !same(draw(style + `200px "${family}"`, "Hamburgefonstiv"), unresolvedText) || !same(candidate, unresolved);
        if (!resolved) continue;
        ctx.font = style + `1000px "${family}"`;
        const f = ctx.measureText(result.sym);
        families.push({ family, same: inked(candidate) && same(reference, candidate), advance: f.width / 1000, fontBox: [f.fontBoundingBoxAscent / 1000, f.fontBoundingBoxDescent / 1000], ink: [f.actualBoundingBoxLeft, f.actualBoundingBoxRight, f.actualBoundingBoxAscent, f.actualBoundingBoxDescent].map(v => Math.round(v) / 1000) });
        if (inked(candidate) && same(reference, candidate)) matches.push(family);
      }
      ctx.font = style + "1000px " + st.fontFamily;
      const m = ctx.measureText(result.sym);
      const entry = { sym: result.sym, fontStyle: st.fontStyle, fontWeight: st.fontWeight, fontFamily: st.fontFamily, matches,
        advance: m.width / 1000, ink: [m.actualBoundingBoxLeft, m.actualBoundingBoxRight, m.actualBoundingBoxAscent, m.actualBoundingBoxDescent].map(v => Math.round(v) / 1000),
        sameAsUnresolved: same(reference, unresolved), families };
      fonts[key] = entry;
    }
  }
  throw new Error("OUT" + JSON.stringify({ userAgent: navigator.userAgent, viewport: [innerWidth, innerHeight], uses: results, fonts }));
}
