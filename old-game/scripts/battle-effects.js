/* Visual effects on the animated battlefield: spells for every element as
   offensive, support and summon magic, unique tier X spectacles, weapon
   projectiles and hit sparks, items and potions, stances, and masterwork
   activations. Effects never change the battle; they only decorate beats.
   Every function returns quickly with nothing to wait for when animations
   are skipped. */
(() => {
  "use strict";
  const ELEMENTS = {
    basic: { main: "#c3b2ff", light: "#f4efff", dark: "#6a58c8" },
    fire: { main: "#ff7a2e", light: "#ffd86b", dark: "#c22a14" },
    water: { main: "#4fa8e8", light: "#dcf3ff", dark: "#1f5fa8" },
    holy: { main: "#ffe08a", light: "#fffbea", dark: "#d0a23a" },
    lightning: { main: "#ffe66b", light: "#ffffff", dark: "#b89a20" },
    blood: { main: "#d8284a", light: "#ff8a9a", dark: "#6e0f20" },
    dark: { main: "#8a5ad0", light: "#dcc6ff", dark: "#24123a" },
    ice: { main: "#9fe0ff", light: "#ffffff", dark: "#4f9fd0" },
    earth: { main: "#b8864f", light: "#ecd09a", dark: "#6a4a28" },
    wind: { main: "#a8f0d0", light: "#f2fff8", dark: "#4fa888" },
    nature: { main: "#6fcf5a", light: "#dcffb4", dark: "#2f7a2a" },
    death: { main: "#8fe0a0", light: "#e4ffea", dark: "#24382c" },
    physical: { main: "#ffffff", light: "#ffffff", dark: "#9aa0a8" },
  };
  const colors = key => ELEMENTS[key] || ELEMENTS.basic;
  const rnd = (a, b) => a + Math.random() * (b - a);
  const svg = (inner, box = "-50 -50 100 100") => `<svg viewBox="${box}" style="overflow:visible">${inner}</svg>`;
  const units = api => Math.max(0.8, api.stage.clientHeight / 300);

  /* ---------------- Plumbing ---------------- */
  function add(api, x, y, size, markup, cls = "", color = "") {
    const node = document.createElement("div");
    node.className = "fx " + cls;
    node.style.left = x + "px";
    node.style.top = y + "px";
    node.style.width = size + "px";
    node.style.height = size + "px";
    if (color) node.style.setProperty("--c", color);
    node.innerHTML = markup;
    api.fxLayer.appendChild(node);
    return node;
  }
  function animate(api, node, frames, ms, options = {}) {
    const animation = node.animate(frames, { duration: ms * api.pace(), easing: options.easing || "ease-out", fill: "both", delay: (options.delay || 0) * api.pace() });
    return animation.finished.catch(() => {}).then(() => { if (!options.keep) node.remove(); });
  }
  const wait = (api, ms) => api.sleep(ms);
  const feet = (api, id) => { const record = api.sprites.get(id); return record ? { x: record.x, y: record.y, record } : null; };
  const point = (api, id, which = "chest") => api.anchor(id, which);

  function aliveIds(api, ally) {
    return (api.snapshot?.actors || []).filter(actor => actor.alive && actor.ally === ally && api.sprites.has(actor.id)).map(actor => actor.id);
  }
  function isAlly(api, id) { return (api.snapshot?.actors || []).find(actor => actor.id === id)?.ally ?? true; }
  function offensiveTargets(api, d) {
    const casterAlly = isAlly(api, d.actorId);
    if (d.area === "enemies") return aliveIds(api, !casterAlly);
    if (d.area === "all") return [...aliveIds(api, !casterAlly), ...aliveIds(api, casterAlly)].filter(id => id !== d.actorId);
    return d.targetId && api.sprites.has(d.targetId) ? [d.targetId] : aliveIds(api, !casterAlly).slice(0, 1);
  }
  function supportTargets(api, d) {
    const casterAlly = isAlly(api, d.actorId);
    if (d.area === "allies") return aliveIds(api, casterAlly);
    if (d.selfOnly || !d.targetId || !api.sprites.has(d.targetId)) return [d.actorId];
    return [d.targetId];
  }

  /* ---------------- Shapes ---------------- */
  const glowOrb = c => `<circle r="16" fill="${c.main}" opacity=".35"/><circle r="10" fill="${c.main}"/><circle r="5" fill="${c.light}"/>`;
  const burst = (c, points = 8, outer = 40, inner = 14) => {
    const steps = [];
    for (let i = 0; i < points * 2; i++) { const r = i % 2 ? inner : outer, a = Math.PI * i / points; steps.push(`${(Math.cos(a) * r).toFixed(1)} ${(Math.sin(a) * r).toFixed(1)}`); }
    return `<path d="M${steps.join(" L")}Z" fill="${c.main}" opacity=".85"/><circle r="${inner}" fill="${c.light}"/>`;
  };
  const ring = (c, r = 40, width = 4) => `<circle r="${r}" fill="none" stroke="${c.main}" stroke-width="${width}"/>`;
  const runeRing = c => `<g class="fx-spin">${ring(c, 42, 2.5)}<circle r="34" fill="none" stroke="${c.light}" stroke-width="1.4" stroke-dasharray="6 5"/><path d="M0 -34 L29.4 17 L-29.4 17Z M0 34 L29.4 -17 L-29.4 -17Z" fill="none" stroke="${c.main}" stroke-width="1.6"/></g>`;
  const flame = (c, s = 1) => `<path transform="scale(${s})" d="M0 -30 Q14 -10 12 6 Q10 20 0 22 Q-10 20 -12 6 Q-12 -6 -4 -14 Q-2 -6 2 -6 Q-4 -18 0 -30Z" fill="${c.main}"/><path transform="scale(${s})" d="M0 -12 Q7 0 6 8 Q4 16 0 16 Q-4 16 -6 8 Q-6 0 0 -12Z" fill="${c.light}"/>`;
  const droplet = (c, s = 1) => `<path transform="scale(${s})" d="M0 -18 Q12 0 10 8 Q8 18 0 18 Q-8 18 -10 8 Q-12 0 0 -18Z" fill="${c.main}"/><ellipse transform="scale(${s})" cx="-3" cy="4" rx="2.5" ry="4" fill="${c.light}" opacity=".9"/>`;
  const shard = (c, s = 1) => `<path transform="scale(${s})" d="M0 -26 L7 0 L0 26 L-7 0Z" fill="${c.main}" stroke="${c.light}" stroke-width="1.5"/>`;
  const leaf = (c, s = 1) => `<path transform="scale(${s})" d="M0 0 Q10 -10 20 0 Q10 10 0 0Z" fill="${c.main}" stroke="${c.dark}" stroke-width="1"/>`;
  const skull = c => `<path d="M-14 -4 Q-14 -22 0 -22 Q14 -22 14 -4 Q14 6 8 8 L8 14 L-8 14 L-8 8 Q-14 6 -14 -4Z" fill="${c.light}" opacity=".9"/><circle cx="-6" cy="-6" r="4" fill="${c.dark}"/><circle cx="6" cy="-6" r="4" fill="${c.dark}"/><path d="M-4 8 v6 M0 8 v6 M4 8 v6" stroke="${c.dark}" stroke-width="1.4"/>`;
  const bolt = c => `<path d="M6 -48 L-8 -6 L4 -6 L-6 48 L14 -12 L2 -12 L14 -48Z" fill="${c.light}" stroke="${c.main}" stroke-width="3"/>`;
  const crescent = (c, s = 1) => `<path transform="scale(${s})" d="M-6 -30 Q24 0 -6 30 Q8 0 -6 -30Z" fill="${c.main}" opacity=".9"/><path transform="scale(${s})" d="M-2 -20 Q14 0 -2 20" stroke="${c.light}" stroke-width="2" fill="none"/>`;
  const swirl = c => `<path d="M0 0 m-30 0 a30 12 0 1 0 60 0 a26 10 0 1 0 -52 0 a20 8 0 1 0 40 0" fill="none" stroke="${c.main}" stroke-width="4" stroke-linecap="round"/>`;
  const snowflake = c => `<g stroke="${c.light}" stroke-width="2.4" stroke-linecap="round">${[0, 60, 120].map(a => `<path transform="rotate(${a})" d="M0 -16 V16 M-5 -12 L0 -7 L5 -12 M-5 12 L0 7 L5 12"/>`).join("")}</g>`;
  const rock = c => `<path d="M-12 8 L-14 -4 L-4 -14 L10 -10 L14 4 L4 12Z" fill="${c.main}" stroke="${c.dark}" stroke-width="2"/><path d="M-4 -14 L10 -10 L2 -4Z" fill="${c.light}" opacity=".6"/>`;
  const spike = (c, h = 40) => `<path d="M-9 0 L0 ${-h} L9 0Z" fill="${c.main}" stroke="${c.dark}" stroke-width="2"/><path d="M0 ${-h} L4 -4 L0 0Z" fill="${c.light}" opacity=".6"/>`;
  const sparkle = c => `<path d="M0 -12 L2.5 -2.5 L12 0 L2.5 2.5 L0 12 L-2.5 2.5 L-12 0 L-2.5 -2.5Z" fill="${c.light}"/>`;
  const cross = c => `<path d="M-3 -12 h6 v9 h9 v6 h-9 v9 h-6 v-9 h-9 v-6 h9Z" fill="${c.light}" stroke="${c.main}" stroke-width="1"/>`;

  /* ---------------- Building blocks ---------------- */
  function scatter(api, x, y, c, shape, count = 8, spread = 60, ms = 600, cls = "") {
    const u = units(api), jobs = [];
    for (let i = 0; i < count; i++) {
      const node = add(api, x, y, 18 * u, svg(shape(c)), "fx-glow " + cls, c.main);
      const angle = rnd(0, Math.PI * 2), distance = rnd(spread * 0.4, spread) * u;
      jobs.push(animate(api, node, [{ transform: "translate(0,0) scale(.6) rotate(0deg)", opacity: 1 }, { transform: `translate(${Math.cos(angle) * distance}px, ${Math.sin(angle) * distance - 10 * u}px) scale(1) rotate(${rnd(-180, 180)}deg)`, opacity: 0 }], ms * rnd(0.8, 1.2)));
    }
    return Promise.all(jobs);
  }
  function rise(api, x, y, c, shape, count = 8, height = 70, ms = 900, width = 40) {
    const u = units(api), jobs = [];
    for (let i = 0; i < count; i++) {
      const node = add(api, x + rnd(-width, width) * u, y, 14 * u, svg(shape(c)), "fx-glow", c.main);
      jobs.push(animate(api, node, [{ transform: "translateY(0) scale(.6)", opacity: 0 }, { offset: 0.25, opacity: 1 }, { transform: `translateY(${-height * rnd(0.6, 1.1) * u}px) scale(1)`, opacity: 0 }], ms * rnd(0.8, 1.2), { delay: i * 40 }));
    }
    return Promise.all(jobs);
  }
  function groundCircle(api, x, y, c, size = 90, ms = 900) {
    const u = units(api);
    const node = add(api, x, y, size * u, svg(`<g transform="scale(1 .36)">${runeRing(c)}<circle r="46" fill="${c.main}" opacity=".18"/></g>`), "fx-glow fx-ground", c.main);
    return animate(api, node, [{ transform: "scale(.2)", opacity: 0 }, { offset: 0.25, transform: "scale(1)", opacity: 1 }, { offset: 0.75, opacity: 1 }, { transform: "scale(1.15)", opacity: 0 }], ms);
  }
  function pop(api, x, y, c, markup, size = 70, ms = 480, cls = "fx-glow") {
    const node = add(api, x, y, size * units(api), svg(markup), cls, c.main);
    return animate(api, node, [{ transform: "scale(.3)", opacity: 1 }, { offset: 0.4, transform: "scale(1.05)", opacity: 1 }, { transform: "scale(1.35)", opacity: 0 }], ms);
  }
  function flight(api, from, to, c, markup, size = 34, arc = 0.18, ms = null, spin = false) {
    const u = units(api);
    const node = add(api, from.x, from.y, size * u, svg(markup), "fx-glow", c.main);
    const dx = to.x - from.x, dy = to.y - from.y, distance = Math.hypot(dx, dy);
    const lift = -Math.min(90 * u, distance * arc);
    const angle = Math.atan2(dy, dx) * 180 / Math.PI;
    const duration = ms || Math.min(560, 220 + distance * 0.35);
    const turn = deg => spin ? `rotate(${deg}deg)` : `rotate(${angle}deg)`;
    return animate(api, node, [
      { transform: `translate(0,0) ${turn(0)} scale(.6)`, opacity: 0 },
      { offset: 0.12, opacity: 1, transform: `translate(${dx * 0.12}px, ${dy * 0.12 + lift * 0.45}px) ${turn(90)} scale(1)` },
      { offset: 0.5, transform: `translate(${dx * 0.5}px, ${dy * 0.5 + lift}px) ${turn(360)} scale(1)` },
      { transform: `translate(${dx}px, ${dy}px) ${turn(720)} scale(1)`, opacity: 1 },
    ], duration, { easing: "ease-in" });
  }
  function tint(api, color, ms = 1200, peak = 0.35) {
    const node = document.createElement("div");
    node.className = "fx-tint";
    node.style.background = color;
    api.fxLayer.appendChild(node);
    return animate(api, node, [{ opacity: 0 }, { offset: 0.2, opacity: peak }, { offset: 0.7, opacity: peak }, { opacity: 0 }], ms);
  }
  function flash(api, color = "#ffffff", ms = 260, peak = 0.7) { return tint(api, color, ms, peak); }

  /* ---------------- Element motifs ---------------- */
  // Each element has offensive impacts, a projectile look and support particles.
  const MOTIFS = {
    basic: { travel: c => glowOrb(c) + `<g class="fx-spin">${ring(c, 16, 1.5)}</g>`, impact: (api, p, c) => Promise.all([pop(api, p.x, p.y, c, runeRing(c), 90, 520), scatter(api, p.x, p.y, c, sparkle, 6, 50)]), particle: sparkle },
    fire: { travel: c => `<g transform="rotate(90)">${flame(c, 1.1)}</g>`, impact: (api, p, c) => Promise.all([pop(api, p.x, p.y, c, burst(c, 10, 44, 18), 100, 520), scatter(api, p.x, p.y, c, x => flame(x, 0.5), 8, 60)]), particle: x => flame(x, 0.45) },
    water: { travel: c => glowOrb(c), impact: (api, p, c) => Promise.all([pop(api, p.x, p.y, c, ring(c, 30, 5) + ring(c, 42, 2), 90, 520), scatter(api, p.x, p.y, c, x => droplet(x, 0.7), 10, 64)]), particle: x => droplet(x, 0.55) },
    holy: { from: "sky", impact: (api, p, c) => Promise.all([pop(api, p.x, p.y, c, ring(c, 34, 4) + burst(c, 6, 30, 10), 90, 560), rise(api, p.x, p.y + 20, c, cross, 5, 60, 700, 20)]), particle: cross },
    lightning: { from: "bolt", impact: (api, p, c) => Promise.all([pop(api, p.x, p.y, c, burst(c, 7, 46, 10), 90, 400), scatter(api, p.x, p.y, c, sparkle, 7, 70, 420)]), particle: sparkle },
    blood: { travel: c => `<g transform="rotate(0)">${crescent(c, 0.9)}</g>`, impact: (api, p, c) => Promise.all([pop(api, p.x, p.y, c, burst(c, 9, 40, 12), 90, 480), scatter(api, p.x, p.y, c, x => droplet(x, 0.6), 10, 60)]), particle: x => droplet(x, 0.5) },
    dark: { travel: c => `<circle r="14" fill="${c.dark}"/><g class="fx-spin">${swirl(c)}</g><circle r="5" fill="${c.light}"/>`, impact: (api, p, c) => Promise.all([pop(api, p.x, p.y, c, `<circle r="40" fill="${c.dark}" opacity=".8"/>` + ring(c, 40, 3), 90, 520), scatter(api, p.x, p.y, c, x => `<path d="M0 0 q8 -14 0 -26" stroke="${x.main}" stroke-width="4" fill="none"/>`, 7, 50)]), particle: x => `<circle r="7" fill="${x.dark}" opacity=".7"/><circle r="3" fill="${x.main}"/>` },
    ice: { travel: c => `<g transform="rotate(90)">${shard(c, 0.9)}</g>`, impact: (api, p, c, target) => Promise.all([groundSpikes(api, target, c, x => spike(x, 34)), scatter(api, p.x, p.y, c, snowflake, 6, 56)]), particle: snowflake },
    earth: { from: "ground", impact: (api, p, c, target) => Promise.all([groundSpikes(api, target, c, rock, true), scatter(api, p.x, p.y + 20, c, rock, 6, 50)]), particle: rock },
    wind: { travel: c => crescent(c, 1.1), impact: (api, p, c) => Promise.all([pop(api, p.x, p.y, c, `<g class="fx-spin">${swirl(c)}</g>`, 110, 620), scatter(api, p.x, p.y, c, x => leaf(x, 0.6), 6, 64)]), particle: x => `<path d="M-12 0 q12 -10 24 0" stroke="${x.light}" stroke-width="3" fill="none"/>` },
    nature: { from: "ground", impact: (api, p, c, target) => Promise.all([groundSpikes(api, target, c, x => `<path d="M0 0 C-14 -14 10 -24 0 -40" stroke="${x.dark}" stroke-width="5" fill="none"/><path d="M2 -26 q10 -6 14 2 q-8 4 -14 -2Z" fill="${x.main}"/>`), scatter(api, p.x, p.y, c, leaf, 9, 64)]), particle: leaf },
    death: { travel: c => skull(c), impact: (api, p, c) => Promise.all([pop(api, p.x, p.y, c, `<circle r="38" fill="${c.dark}" opacity=".7"/>` + skull(c), 90, 640), scatter(api, p.x, p.y, c, x => `<circle r="8" fill="${x.main}" opacity=".5"/>`, 8, 60, 800)]), particle: x => `<circle r="6" fill="${x.main}" opacity=".6"/>` },
  };
  function groundSpikes(api, id, c, shape, rocky = false) {
    const base = feet(api, id);
    if (!base) return Promise.resolve();
    const u = units(api), jobs = [];
    for (let i = 0; i < 4; i++) {
      const node = add(api, base.x + (i - 1.5) * 16 * u, base.y + 4 * u, 46 * u, svg(shape(c), "-50 -100 100 110"), "fx-glow fx-spike", c.main);
      jobs.push(animate(api, node, [{ transform: "translateY(30%) scaleY(.1)", opacity: 0 }, { offset: 0.3, transform: "translateY(-30%) scaleY(1.1)", opacity: 1 }, { offset: 0.7, transform: "translateY(-28%) scaleY(1)", opacity: 1 }, { transform: "translateY(10%) scaleY(.5)", opacity: 0 }], rocky ? 720 : 640, { delay: i * 40 }));
    }
    return Promise.all(jobs);
  }

  // Strikes for elements that come from above or below instead of flying.
  function skyStrike(api, id, c, motif) {
    const p = point(api, id, "chest"), base = feet(api, id);
    if (!p || !base) return Promise.resolve();
    const u = units(api);
    if (motif === "bolt") {
      const node = add(api, base.x, base.y - 120 * u, 120 * u, svg(bolt(c), "-30 -50 60 100"), "fx-glow fx-bolt", c.main);
      node.style.height = 240 * u + "px";
      flash(api, "#fffbe0", 220, 0.45);
      return animate(api, node, [{ opacity: 0, transform: "scaleY(.3)" }, { offset: 0.15, opacity: 1, transform: "scaleY(1)" }, { offset: 0.3, opacity: 0.3 }, { offset: 0.45, opacity: 1 }, { opacity: 0 }], 460);
    }
    if (motif === "sky") {
      const node = document.createElement("div");
      node.className = "fx fx-beam";
      node.style.cssText = `left:${base.x}px;top:0;width:${46 * u}px;height:${base.y}px;--c:${c.main};translate:-50% 0`;
      api.fxLayer.appendChild(node);
      return animate(api, node, [{ opacity: 0, transform: "scaleX(.2)" }, { offset: 0.3, opacity: 1, transform: "scaleX(1)" }, { offset: 0.7, opacity: 0.9 }, { opacity: 0, transform: "scaleX(1.4)" }], 560);
    }
    return Promise.resolve();
  }

  /* ---------------- Spells ---------------- */
  function castCircle(api, d) {
    if (api.instant()) return;
    const base = feet(api, d.actorId);
    if (!base) return;
    groundCircle(api, base.x, base.y, colors(d.element), d.tier >= 7 ? 130 : 100, 780);
    const hand = point(api, d.actorId, "hand");
    if (hand) pop(api, hand.x, hand.y, colors(d.element), glowOrb(colors(d.element)), 40, 520);
  }

  function offensive(api, d) {
    const c = colors(d.element), motif = MOTIFS[d.element] || MOTIFS.basic;
    const targets = offensiveTargets(api, d);
    const from = point(api, d.actorId, "hand") || point(api, d.actorId, "chest");
    let resolveImpact;
    const impact = new Promise(resolve => { resolveImpact = resolve; });
    const jobs = targets.map(async (id, index) => {
      const to = point(api, id, "chest");
      if (!to || !from) return;
      await wait(api, index * 70);
      if (motif.from === "sky") await skyStrike(api, id, c, "sky").then(() => null);
      else if (motif.from === "bolt") { skyStrike(api, id, c, "bolt"); await wait(api, 140); }
      else if (motif.from === "ground") {
        // Spikes and vines rise beneath the target; the hit lands as they break the ground.
        const rising = motif.impact(api, to, c, id);
        await wait(api, 200);
        if (index === 0) resolveImpact();
        await rising;
        return;
      }
      else await flight(api, from, to, c, motif.travel(c), d.tier >= 6 ? 44 : 34, d.element === "blood" || d.element === "wind" ? 0.05 : 0.18);
      if (index === 0) resolveImpact();
      await motif.impact(api, to, c, id);
    });
    if (!targets.length) resolveImpact();
    const done = Promise.all(jobs).then(() => resolveImpact());
    return { impact, done };
  }

  function support(api, d) {
    const c = colors(d.element), motif = MOTIFS[d.element] || MOTIFS.basic;
    const recipients = supportTargets(api, d);
    let resolveImpact;
    const impact = new Promise(resolve => { resolveImpact = resolve; });
    const jobs = recipients.map(async (id, index) => {
      const base = feet(api, id), top = point(api, id, "top");
      if (!base) return;
      await wait(api, index * 60);
      groundCircle(api, base.x, base.y, c, 90, 900);
      rise(api, base.x, base.y - 6, c, motif.particle, 9, 90, 900, 26);
      if (d.element === "holy" && top) pop(api, top.x, top.y - 8 * units(api), c, `<ellipse rx="20" ry="7" fill="none" stroke="${c.main}" stroke-width="3"/>`, 60, 800);
      if (d.element === "water") pop(api, base.x, base.y - 30 * units(api), c, `<g class="fx-spin">${swirl(c)}</g>`, 90, 800);
      if (d.element === "earth") scatter(api, base.x, base.y - 30 * units(api), c, rock, 5, 34, 800);
      await wait(api, 380);
      if (index === 0) resolveImpact();
      await wait(api, 340);
    });
    if (!recipients.length) resolveImpact();
    const done = Promise.all(jobs).then(() => resolveImpact());
    return { impact, done };
  }

  function summon(api, d) {
    const c = colors(d.element);
    const ids = (d.summonIds || []).filter(id => api.sprites.has(id));
    const spots = ids.length ? ids : [];
    let resolveImpact;
    const impact = new Promise(resolve => { resolveImpact = resolve; });
    const u = units(api);
    const jobs = spots.map(async (id, index) => {
      const base = feet(api, id);
      if (!base) return;
      await wait(api, index * 90);
      groundCircle(api, base.x, base.y, c, 120, 1100);
      const pillar = document.createElement("div");
      pillar.className = "fx fx-pillar fx-pillar-" + d.element;
      pillar.style.cssText = `left:${base.x}px;top:${base.y - 150 * u}px;width:${60 * u}px;height:${150 * u}px;--c:${c.main};--l:${c.light};translate:-50% 0`;
      api.fxLayer.appendChild(pillar);
      animate(api, pillar, [{ opacity: 0, transform: "scaleY(0)" }, { offset: 0.35, opacity: 1, transform: "scaleY(1)" }, { offset: 0.7, opacity: 0.8 }, { opacity: 0, transform: "scaleX(.3)" }], 900);
      rise(api, base.x, base.y, c, (MOTIFS[d.element] || MOTIFS.basic).particle, 8, 120, 900, 24);
      if (d.element === "dark" || d.element === "death") pop(api, base.x, base.y - 40 * u, c, `<ellipse rx="26" ry="36" fill="${c.dark}"/><ellipse rx="20" ry="30" fill="#000" opacity=".6"/>`, 100, 900, "");
      if (d.element === "lightning") skyStrike(api, id, c, "bolt");
      await wait(api, 480);
      if (index === 0) resolveImpact();
      await wait(api, 300);
    });
    if (!spots.length) { setTimeout(resolveImpact, 0); }
    const done = Promise.all(jobs).then(() => resolveImpact());
    return { impact, done };
  }

  /* ---------------- Tier X ---------------- */
  function enemySideCenter(api, d) {
    const ids = offensiveTargets(api, { ...d, area: "enemies" });
    const points = ids.map(id => point(api, id, "chest")).filter(Boolean);
    if (!points.length) return { x: api.stage.clientWidth * 0.7, y: api.stage.clientHeight * 0.55, ids };
    return { x: points.reduce((sum, p) => sum + p.x, 0) / points.length, y: points.reduce((sum, p) => sum + p.y, 0) / points.length, ids };
  }
  function allySideCenter(api, d) {
    const ids = aliveIds(api, isAlly(api, d.actorId));
    const points = ids.map(id => point(api, id, "chest")).filter(Boolean);
    if (!points.length) return { x: api.stage.clientWidth * 0.25, y: api.stage.clientHeight * 0.55, ids };
    return { x: points.reduce((sum, p) => sum + p.x, 0) / points.length, y: points.reduce((sum, p) => sum + p.y, 0) / points.length, ids };
  }
  function big(api, x, y, size, markup, frames, ms, cls = "fx-glow", color = "#fff", options = {}) {
    return animate(api, add(api, x, y, size * units(api), svg(markup), cls, color), frames, ms, options);
  }
  const TIER_TEN = {
    "axiom-unbound-basic-damage": async (api, d, hit) => {
      const c = colors("basic"), side = enemySideCenter(api, d);
      tint(api, "#3a2a6a", 1500, 0.35);
      big(api, side.x, side.y - 40, 300, `<g class="fx-spin">${runeRing(c)}${ring(c, 48, 1.5)}</g>`, [{ transform: "scale(.2) rotate(0deg)", opacity: 0 }, { offset: 0.35, transform: "scale(1) rotate(90deg)", opacity: 1 }, { offset: 0.7, transform: "scale(1.05) rotate(140deg)", opacity: 1 }, { transform: "scale(1.6) rotate(200deg)", opacity: 0 }], 1300);
      await wait(api, 780);
      flash(api, "#efe6ff", 300, 0.8);
      hit();
      for (const id of side.ids) { const p = point(api, id); if (p) scatter(api, p.x, p.y, c, x => shard(x, 0.6), 10, 90, 700); }
      await wait(api, 600);
    },
    "the-first-flame-fire-damage": async (api, d, hit) => {
      const c = colors("fire"), side = enemySideCenter(api, d);
      tint(api, "#5a1a08", 1700, 0.4);
      big(api, side.x, 40 * units(api), 150, `<circle r="40" fill="${c.main}"/><circle r="28" fill="${c.light}"/>` + burst(c, 14, 50, 40), [{ transform: "translateY(-60%) scale(.3)", opacity: 0 }, { offset: 0.4, transform: "translateY(0) scale(1)", opacity: 1 }, { offset: 0.65, transform: `translateY(${side.y - 40 * units(api)}px) scale(1.3)`, opacity: 1 }, { transform: `translateY(${side.y - 40 * units(api)}px) scale(2.4)`, opacity: 0 }], 1300);
      await wait(api, 820);
      flash(api, "#ffd08a", 320, 0.75);
      hit();
      for (const id of side.ids) { const p = point(api, id); if (p) { pop(api, p.x, p.y, c, burst(c, 12, 46, 18), 130, 600); scatter(api, p.x, p.y, c, x => flame(x, 0.6), 10, 80, 800); } }
      await wait(api, 600);
    },
    "ocean-without-shores-water-support": async (api, d, hit) => {
      const c = colors("water"), side = allySideCenter(api, d), u = units(api);
      tint(api, "#0a3a6a", 1600, 0.3);
      const wave = document.createElement("div");
      wave.className = "fx fx-wave";
      wave.style.cssText = `left:0;top:${api.stage.clientHeight * 0.25}px;width:${api.stage.clientWidth * 0.55}px;height:${api.stage.clientHeight * 0.75}px;--c:${c.main};--l:${c.light};translate:0 0`;
      api.fxLayer.appendChild(wave);
      animate(api, wave, [{ transform: "translateX(-100%) scaleY(.4)", opacity: 0 }, { offset: 0.45, transform: "translateX(0) scaleY(1)", opacity: 0.9 }, { transform: "translateX(25%) scaleY(.2)", opacity: 0 }], 1300);
      await wait(api, 640);
      hit();
      for (const id of side.ids) { const base = feet(api, id); if (base) { rise(api, base.x, base.y, c, sparkle, 8, 110, 900, 20); groundCircle(api, base.x, base.y, c, 90, 800); } }
      await wait(api, 700);
    },
    "worldheart-bastion-earth-support": async (api, d, hit) => {
      const c = colors("earth"), side = allySideCenter(api, d), u = units(api);
      tint(api, "#4a3010", 1500, 0.25);
      big(api, side.x + 60 * u, side.y - 10 * u, 220, `<path d="M0 -46 L38 -28 L32 22 L0 46 L-32 22 L-38 -28Z" fill="${c.main}" opacity=".5" stroke="${c.light}" stroke-width="3"/><path d="M0 -30 V30 M-22 -10 H22" stroke="${c.light}" stroke-width="4"/>`, [{ transform: "translateY(40%) scale(.4)", opacity: 0 }, { offset: 0.4, transform: "translateY(0) scale(1)", opacity: 1 }, { offset: 0.8, opacity: 1 }, { transform: "scale(1.2)", opacity: 0 }], 1400);
      await wait(api, 600);
      hit();
      for (const id of side.ids) { groundSpikes(api, id, c, rock, true); const base = feet(api, id); if (base) groundCircle(api, base.x, base.y, c, 100, 900); }
      await wait(api, 760);
    },
    "crown-of-the-open-sky-wind-damage": async (api, d, hit) => {
      const c = colors("wind"), side = enemySideCenter(api, d);
      tint(api, "#1a4a3a", 1500, 0.22);
      big(api, side.x, side.y - 120 * units(api), 220, `<g class="fx-spin">${[0, 45, 90, 135, 180, 225, 270, 315].map(a => `<g transform="rotate(${a}) translate(0 -36)">${crescent(c, 0.5)}</g>`).join("")}</g>`, [{ transform: "scale(.3)", opacity: 0 }, { offset: 0.4, transform: "scale(1)", opacity: 1 }, { offset: 0.7, transform: "translateY(40%) scale(1.3)", opacity: 1 }, { transform: "translateY(60%) scale(2)", opacity: 0 }], 1300);
      await wait(api, 760);
      hit();
      for (const id of side.ids) { const p = point(api, id); if (p) { pop(api, p.x, p.y, c, `<g class="fx-spin">${swirl(c)}${swirl({ ...c, main: c.light })}</g>`, 150, 800); scatter(api, p.x, p.y, c, x => leaf(x, 0.6), 8, 90, 700); } }
      await wait(api, 600);
    },
    "the-unending-winter-ice-damage": async (api, d, hit) => {
      const c = colors("ice"), side = enemySideCenter(api, d);
      tint(api, "#d8f0ff", 1700, 0.35);
      for (let i = 0; i < 30; i++) { const x = rnd(0, api.stage.clientWidth); big(api, x, -10, 18, snowflake(c), [{ transform: "translate(0,0) rotate(0deg)", opacity: 0.9 }, { transform: `translate(${rnd(-160, -60)}px, ${api.stage.clientHeight}px) rotate(360deg)`, opacity: 0.4 }], rnd(900, 1500), "fx-glow", c.main, { delay: rnd(0, 400) }); }
      big(api, side.x, side.y - 40, 260, `<g class="fx-spin">${snowflake(c)}</g>`, [{ transform: "scale(.2)", opacity: 0 }, { offset: 0.5, transform: "scale(1.6)", opacity: 0.9 }, { transform: "scale(2.2)", opacity: 0 }], 1200);
      await wait(api, 760);
      hit();
      for (const id of side.ids) { groundSpikes(api, id, c, x => spike(x, 48)); const p = point(api, id); if (p) pop(api, p.x, p.y, c, `<path d="M0 -44 L26 -14 L18 34 L-18 34 L-26 -14Z" fill="${c.main}" opacity=".45" stroke="${c.light}" stroke-width="2"/>`, 110, 900, "fx-glow"); }
      await wait(api, 700);
    },
    "judgment-of-the-storm-king-lightning-damage": async (api, d, hit) => {
      const c = colors("lightning"), side = enemySideCenter(api, d);
      tint(api, "#10142a", 1700, 0.55);
      big(api, side.x, 50 * units(api), 200, `<path d="M-40 10 L-30 -20 L-14 0 L0 -30 L14 0 L30 -20 L40 10Z" fill="${c.main}" stroke="${c.light}" stroke-width="2"/>`, [{ transform: "scale(.4)", opacity: 0 }, { offset: 0.4, transform: "scale(1)", opacity: 1 }, { offset: 0.8, opacity: 1 }, { transform: "scale(1.2)", opacity: 0 }], 1100);
      await wait(api, 620);
      for (const id of side.ids) skyStrike(api, id, c, "bolt");
      flash(api, "#ffffff", 260, 0.85);
      hit();
      await wait(api, 220);
      for (const id of side.ids) { const p = point(api, id); if (p) scatter(api, p.x, p.y, c, sparkle, 8, 80, 500); }
      flash(api, "#fffbe0", 200, 0.5);
      await wait(api, 500);
    },
    "genesis-dawn-holy-damage": async (api, d, hit) => {
      const c = colors("holy"), side = enemySideCenter(api, d), allies = allySideCenter(api, d), u = units(api);
      tint(api, "#ffcf7a", 1800, 0.3);
      big(api, api.stage.clientWidth / 2, api.stage.clientHeight * 0.52, 220, `<circle r="40" fill="${c.light}" opacity=".9"/><circle r="52" fill="${c.main}" opacity=".25"/>` + [0, 20, 40, 60, 80, 100, 120, 140, 160].map(a => `<rect x="-2" y="-90" width="4" height="50" fill="${c.main}" transform="rotate(${a - 80})"/>`).join(""), [{ transform: "translateY(40%) scale(.4)", opacity: 0 }, { offset: 0.45, transform: "translateY(0) scale(1)", opacity: 1 }, { offset: 0.8, opacity: 0.9 }, { transform: "scale(1.4)", opacity: 0 }], 1500);
      await wait(api, 700);
      for (const id of side.ids) skyStrike(api, id, c, "sky");
      flash(api, "#fffbe8", 300, 0.7);
      hit();
      for (const id of allies.ids) { const base = feet(api, id); if (base) rise(api, base.x, base.y, c, cross, 6, 90, 900, 20); }
      await wait(api, 700);
    },
    "eventide-absolute-dark-damage": async (api, d, hit) => {
      const c = colors("dark"), side = enemySideCenter(api, d);
      tint(api, "#05030c", 1900, 0.7);
      big(api, side.x, 70 * units(api), 160, `<circle r="40" fill="#ffe8b0"/><circle cx="10" r="38" fill="#05030c"/>`, [{ transform: "scale(.5)", opacity: 0 }, { offset: 0.4, transform: "scale(1)", opacity: 1 }, { offset: 0.85, opacity: 1 }, { transform: "scale(1.1)", opacity: 0 }], 1500);
      await wait(api, 820);
      hit();
      for (const id of side.ids) { const p = point(api, id); if (p) { pop(api, p.x, p.y, c, `<circle r="44" fill="${c.dark}"/>` + ring(c, 44, 3), 120, 800, ""); scatter(api, p.x, p.y, c, x => `<path d="M0 0 q12 -18 0 -36" stroke="${x.main}" stroke-width="5" fill="none"/>`, 9, 70, 800); } }
      await wait(api, 700);
    },
    "worldseed-miracle-nature-support": async (api, d, hit) => {
      const c = colors("nature"), allies = allySideCenter(api, d), u = units(api);
      tint(api, "#1a4a1a", 1600, 0.25);
      const trunkX = Math.max(60 * u, allies.x - 150 * u);
      big(api, trunkX, api.stage.clientHeight * 0.5, 210, `<path d="M-6 50 Q-10 0 0 -20 Q10 0 6 50Z" fill="#6a4a2a"/><circle cy="-34" r="36" fill="${c.main}"/><circle cx="-24" cy="-24" r="22" fill="${c.dark}"/><circle cx="24" cy="-40" r="22" fill="${c.light}" opacity=".8"/>`, [{ transform: "translateY(30%) scale(.05)", opacity: 0 }, { offset: 0.45, transform: "translateY(0) scale(1)", opacity: 0.92 }, { offset: 0.85, opacity: 0.92 }, { transform: "scale(1.05)", opacity: 0 }], 1600);
      await wait(api, 760);
      hit();
      for (const id of allies.ids) { const base = feet(api, id); if (base) { rise(api, base.x, base.y, c, leaf, 8, 100, 900, 24); groundCircle(api, base.x, base.y, c, 90, 900); } }
      for (let i = 0; i < 16; i++) big(api, rnd(0, api.stage.clientWidth * 0.55), -10, 16, `<circle r="6" fill="#ffc0d8"/><circle r="2.5" fill="#fff4a0"/>`, [{ transform: "translate(0,0)", opacity: 1 }, { transform: `translate(${rnd(-40, 60)}px, ${api.stage.clientHeight * 0.9}px) rotate(300deg)`, opacity: 0.2 }], rnd(1000, 1600), "", "#fff", { delay: rnd(0, 300) });
      await wait(api, 700);
    },
    "blood-sun-apotheosis-blood-damage": async (api, d, hit) => {
      const c = colors("blood"), target = d.targetId && point(api, d.targetId), caster = point(api, d.actorId);
      tint(api, "#3a0610", 1700, 0.45);
      if (target) big(api, target.x, 60 * units(api), 150, `<circle r="40" fill="${c.main}"/><circle r="30" fill="${c.dark}"/>` + burst(c, 12, 52, 42), [{ transform: "scale(.3)", opacity: 0 }, { offset: 0.4, transform: "scale(1)", opacity: 1 }, { offset: 0.85, opacity: 1 }, { transform: "scale(1.3)", opacity: 0 }], 1400);
      await wait(api, 640);
      if (target) for (let i = 0; i < 5; i++) flight(api, { x: target.x + rnd(-40, 40), y: 60 * units(api) }, target, c, shard(c, 0.8), 30, 0, 260);
      await wait(api, 260);
      hit();
      if (target && caster) for (let i = 0; i < 6; i++) flight(api, target, caster, c, droplet(c, 0.6), 18, 0.25, 520 + i * 40);
      await wait(api, 700);
    },
    "silence-at-the-end-death-damage": async (api, d, hit) => {
      const c = colors("death"), target = d.targetId && point(api, d.targetId);
      api.stage.classList.add("fx-desaturate");
      tint(api, "#0a0f0c", 1700, 0.5);
      await wait(api, 300);
      if (target) big(api, target.x + 40 * units(api), target.y - 60 * units(api), 220, `<path d="M-40 -40 Q20 -60 50 -10 Q20 -36 -30 -26Z" fill="${c.light}" stroke="${c.main}" stroke-width="2"/><path d="M-30 -30 L30 50" stroke="#3a3f3a" stroke-width="6"/>`, [{ transform: "rotate(-40deg)", opacity: 0 }, { offset: 0.25, transform: "rotate(-40deg)", opacity: 1 }, { offset: 0.6, transform: "rotate(70deg)", opacity: 1 }, { transform: "rotate(80deg)", opacity: 0 }], 900);
      await wait(api, 520);
      hit();
      if (target) pop(api, target.x, target.y, c, skull(c), 120, 900);
      await wait(api, 700);
      api.stage.classList.remove("fx-desaturate");
    },
  };

  function spell(api, d) {
    if (api.instant()) return null;
    if (d.tier === 10 && TIER_TEN[d.spellId]) {
      let resolveImpact;
      const impact = new Promise(resolve => { resolveImpact = resolve; });
      const done = TIER_TEN[d.spellId](api, d, resolveImpact).catch(() => {}).then(() => resolveImpact());
      return { impact, done };
    }
    if (d.category === "summon") return summon(api, d);
    if (d.category === "support") return support(api, d);
    return offensive(api, d);
  }

  /* ---------------- Weapons ---------------- */
  function projectile(api, fromId, toId, kind = "arrow", element = null) {
    if (api.instant()) return Promise.resolve();
    const from = point(api, fromId, "hand") || point(api, fromId, "chest"), to = point(api, toId, "chest");
    if (!from || !to) return Promise.resolve();
    const c = element ? colors(element) : { main: "#e8dcc4", light: "#ffffff", dark: "#6a4a2a" };
    const arrow = `<path d="M-22 0 H16" stroke="#8a6a42" stroke-width="3"/><path d="M16 -5 L26 0 L16 5Z" fill="#b8c0c8" stroke="#2b231e" stroke-width="1"/><path d="M-22 0 l-6 -6 M-22 0 l-6 6 M-17 0 l-6 -6 M-17 0 l-6 6" stroke="${element ? c.main : "#d8d0c0"}" stroke-width="2"/>`;
    return flight(api, from, to, c, arrow, 40, 0.1, Math.min(380, 160 + Math.hypot(to.x - from.x, to.y - from.y) * 0.3));
  }
  function hitSpark(api, id, element, critical) {
    if (api.instant()) return;
    const p = point(api, id, "chest");
    if (!p) return;
    const c = colors(element && ELEMENTS[element] ? element : "physical");
    pop(api, p.x + rnd(-6, 6), p.y + rnd(-6, 6), c, burst(c, critical ? 10 : 6, critical ? 46 : 32, critical ? 12 : 8), critical ? 80 : 50, critical ? 360 : 260);
    if (critical) pop(api, p.x, p.y, c, ring(c, 42, 3), 110, 420);
  }

  /* ---------------- Items, stances and skills ---------------- */
  function holdInHand(api, id, markup, ms) {
    const record = api.sprites.get(id);
    const arm = record?.el.querySelector(".sp-arm-front");
    if (!arm) return false;
    const handY = Number(arm.dataset.handY) || 22;
    const group = document.createElementNS("http://www.w3.org/2000/svg", "g");
    group.setAttribute("class", "fx-held");
    group.setAttribute("transform", `translate(0 ${handY}) rotate(160)`);
    group.innerHTML = markup;
    arm.appendChild(group);
    setTimeout(() => group.remove(), ms * api.pace());
    return true;
  }
  function item(api, d) {
    if (api.instant()) return;
    const hand = point(api, d.actorId, "hand") || point(api, d.actorId, "chest"), base = feet(api, d.actorId);
    if (!hand) return;
    if (d.potion) {
      const liquid = d.resource === "mana" ? "#3f7fd8" : "#d8344a";
      const bottle = `<path d="M-3 -14 h6 v5 q6 3 6 10 q0 8 -9 8 q-9 0 -9 -8 q0 -7 6 -10Z" fill="#e8f2f6" fill-opacity=".85" stroke="#2b231e" stroke-width="1"/><path d="M-8 0 q0 7 8 7 q8 0 8 -7Z" fill="${liquid}"/><rect x="-3.5" y="-17" width="7" height="4" rx="1" fill="#8a6a42" stroke="#2b231e" stroke-width=".8"/>`;
      if (!holdInHand(api, d.actorId, bottle, 760)) pop(api, hand.x, hand.y - 20, { main: liquid }, `<g transform="scale(2)">${bottle}</g>`, 60, 760, "");
      const c = d.resource === "mana" ? colors("water") : { main: "#ff8a9a", light: "#ffe0e6" };
      setTimeout(() => { const head = point(api, d.actorId, "head"); if (head) rise(api, head.x, head.y, c, x => `<circle r="5" fill="${x.main}" opacity=".7"/>`, 6, 50, 700, 10); }, 420 * api.pace());
      return;
    }
    if (d.trap && base) {
      const ahead = base.x + (isAlly(api, d.actorId) ? 50 : -50) * units(api);
      const trap = `<ellipse rx="22" ry="8" fill="#5a4a3a" stroke="#2b231e" stroke-width="2"/><path d="M-18 -2 l4 -8 l4 6 l4 -8 l4 6 l4 -8 l4 6 l4 -8 l2 6" stroke="#b8c0c8" stroke-width="2" fill="none"/>`;
      animate(api, add(api, ahead, base.y, 60 * units(api), svg(trap), "", "#fff"), [{ transform: "translateY(-30px) scale(.5)", opacity: 0 }, { offset: 0.35, transform: "translateY(0) scale(1)", opacity: 1 }, { offset: 0.8, opacity: 1 }, { opacity: 0 }], 900);
      return;
    }
    const c = colors("holy");
    pop(api, hand.x, hand.y - 10, c, `<path d="M-10 -6 q10 -10 20 0 v12 q-10 6 -20 0Z" fill="#a8845a" stroke="#2b231e" stroke-width="1.5"/>` + sparkle(c), 50, 600, "fx-glow");
    scatter(api, hand.x, hand.y - 10, c, sparkle, 6, 40, 600);
  }
  function stance(api, d) {
    if (api.instant()) return;
    const p = point(api, d.actorId, "chest");
    if (!p) return;
    const ahead = (isAlly(api, d.actorId) ? 1 : -1) * 26 * units(api);
    if (d.stance === "Guard") pop(api, p.x + ahead, p.y, { main: "#bcd2ff" }, `<path d="M0 -40 L32 -26 L28 16 L0 40 L-28 16 L-32 -26Z" fill="#bcd2ff" fill-opacity=".28" stroke="#e6efff" stroke-width="3"/>`, 110, 560);
    else for (let i = 0; i < 3; i++) animate(api, add(api, p.x - ahead * (0.6 + i * 0.5), p.y + (i - 1) * 12, 60 * units(api), svg(`<path d="M-30 0 H30" stroke="#e8fff4" stroke-width="3" stroke-linecap="round"/>`), "", "#fff"), [{ opacity: 0, transform: "scaleX(.2)" }, { offset: 0.3, opacity: 0.9, transform: "scaleX(1)" }, { opacity: 0, transform: "scaleX(1.4)" }], 460, { delay: i * 50 });
  }
  function meditate(api, d) {
    if (api.instant()) return;
    const base = feet(api, d.actorId);
    if (!base) return;
    const c = colors("water");
    groundCircle(api, base.x, base.y, { ...c, main: "#8ec7ff" }, 90, 900);
    rise(api, base.x, base.y - 10, { ...c, main: "#8ec7ff" }, x => `<circle r="5" fill="${x.main}"/>`, 10, 90, 900, 22);
  }
  function skill(api, d) {
    if (api.instant()) return;
    const p = point(api, d.actorId, "chest");
    if (!p) return;
    const c = { main: "#ffd36b", light: "#fff6d0", dark: "#c99a2a" };
    pop(api, p.x, p.y, c, ring(c, 36, 4) + sparkle(c), 100, 520);
    scatter(api, p.x, p.y, c, sparkle, 6, 50, 560);
    if (d.targetId && d.targetId !== d.actorId) { const t = point(api, d.targetId, "chest"); if (t) pop(api, t.x, t.y, c, burst(c, 8, 36, 10), 80, 420); }
  }

  /* ---------------- Masterwork activations ---------------- */
  const MASTERWORK_FX = {
    starscale(api, d, c) { const t = d.targetId && point(api, d.targetId); if (t) { pop(api, t.x, t.y, c, `<path d="M0 -40 L8 -8 L40 0 L8 8 L0 40 L-8 8 L-40 0 L-8 -8Z" fill="#ffffff"/>`, 110, 520); scatter(api, t.x, t.y, { main: "#dfe9ff", light: "#ffffff" }, sparkle, 8, 70, 600); } },
    "golden-apex"(api, d) { const p = point(api, d.actorId, "top"); if (p) pop(api, p.x, p.y, { main: "#ffe27a" }, `<ellipse rx="36" ry="12" fill="none" stroke="#ffe27a" stroke-width="4"/>` + [0, 60, 120].map(a => `<path transform="rotate(${a})" d="M-44 0 H44" stroke="#fff4c8" stroke-width="2"/>`).join(""), 120, 800); },
    giantslayer(api, d) { const t = d.targetId && point(api, d.targetId); if (t) pop(api, t.x, t.y, { main: "#7fd0ff" }, `<path d="M-20 -20 l10 -10 l10 10 l10 -10 l10 10 M-20 10 l10 -10 l10 10 l10 -10 l10 10" stroke="#9fe0ff" stroke-width="3" fill="none"/>`, 90, 520); },
    "stunted-sword"(api, d) { const t = d.targetId && point(api, d.targetId); if (t) pop(api, t.x, t.y, { main: "#9fd06a" }, sparkle({ light: "#d8ff9a" }), 60, 420); },
    "forest-branch"(api, d) { const p = point(api, d.actorId, "hand"); if (p) scatter(api, p.x, p.y, colors("nature"), leaf, 8, 60, 700); },
    keening(api, d) { const t = d.targetId && point(api, d.targetId); if (t) for (let i = 0; i < 3; i++) animate(api, add(api, t.x, t.y, 60 * units(api), svg(ring({ main: "#bfe8ff" }, 30, 3)), "fx-glow", "#bfe8ff"), [{ transform: "scale(.3)", opacity: 1 }, { transform: "scale(3)", opacity: 0 }], 700, { delay: i * 120 }); },
    sunder(api, d) { const t = d.targetId && point(api, d.targetId); if (!t) return; for (const id of aliveIds(api, isAlly(api, d.actorId)).slice(0, 4)) { const a = point(api, id); if (a) flight(api, t, a, colors("water"), `<circle r="6" fill="#86b8ff"/>`, 16, 0.25, 600); } },
    "anvil-hammer"(api, d) { const t = d.targetId && point(api, d.targetId); if (t) { scatter(api, t.x, t.y, { main: "#ffb46b", light: "#fff0c0" }, sparkle, 10, 60, 420); pop(api, t.x, t.y, { main: "#c8ccd4" }, ring({ main: "#e8ecf0" }, 30, 4), 90, 380); } },
    beowulf(api, d) { const t = d.targetId && point(api, d.targetId, "top"); if (t) pop(api, t.x, t.y - 10, colors("lightning"), `<circle r="18" fill="none" stroke="#9fb4ff" stroke-width="3"/><path d="M4 -16 L-6 2 L2 2 L-4 16 L8 -4 L0 -4Z" fill="#ffe66b"/>`, 70, 900); },
    "royal-trident"(api, d) { const base = feet(api, d.actorId); if (base) { groundCircle(api, base.x, base.y, colors("water"), 90, 700); rise(api, base.x, base.y, colors("water"), x => droplet(x, 0.5), 7, 70, 700, 20); } },
    "guild-staff"(api, d) { const p = point(api, d.actorId, "top"); if (p) pop(api, p.x, p.y - 20, { main: "#7cff9a" }, `<path d="M-14 -14 h28 v16 q-14 14 -28 0Z" fill="#c9b46a" stroke="#7cff9a" stroke-width="2"/><path d="M-8 -10 L8 4 M8 -10 L-8 4" stroke="#3a2f22" stroke-width="2.5"/>`, 70, 900); },
    enricher(api, d) { const t = d.targetId && point(api, d.targetId); if (t) scatter(api, t.x, t.y, { main: "#f0c548", light: "#fff3c4" }, x => `<circle r="6" fill="${x.main}" stroke="#9a7224" stroke-width="1.5"/>`, 7, 60, 600); },
    "bloody-mary"(api, d) { const t = d.targetId && point(api, d.targetId); if (t) scatter(api, t.x, t.y, colors("blood"), x => droplet(x, 0.7), 10, 70, 600); },
    "hellish-rebuke"(api, d) { const from = point(api, d.actorId, "hand"), to = d.targetId && point(api, d.targetId); if (from && to) flight(api, from, to, colors("fire"), `<g transform="rotate(90)">${flame(colors("fire"), 1.2)}</g>`, 40, 0.12, 360).then(() => pop(api, to.x, to.y, colors("fire"), burst(colors("fire"), 10, 44, 16), 90, 480)); },
    jackhammer(api, d) { const base = feet(api, d.actorId); if (!base) return; const dir = isAlly(api, d.actorId) ? 1 : -1; for (let i = 0; i < 3; i++) animate(api, add(api, base.x + dir * 40 * units(api), base.y, 120 * units(api), svg(`<ellipse rx="40" ry="10" fill="none" stroke="#e8c75e" stroke-width="4"/>`), "fx-glow", "#e8c75e"), [{ transform: "translateX(0) scale(.4)", opacity: 1 }, { transform: `translateX(${dir * 260}px) scale(1.6)`, opacity: 0 }], 700, { delay: i * 90 }); },
    impshield(api, d) { const p = point(api, d.actorId, "chest"); if (p) pop(api, p.x, p.y, { main: "#a8263b" }, `<path d="M-4 -4 Q-34 -26 -42 -6 Q-28 -8 -24 4 Q-14 -6 -4 4Z M4 -4 Q34 -26 42 -6 Q28 -8 24 4 Q14 -6 4 4Z" fill="#c43a4a" opacity=".8"/>`, 110, 600); },
    "forest-plate"(api, d) { const p = point(api, d.actorId, "chest"); if (p) { pop(api, p.x, p.y, colors("nature"), ring(colors("nature"), 36, 3), 100, 600); scatter(api, p.x, p.y, colors("nature"), leaf, 6, 50, 600); } },
    godforged(api, d) { const p = point(api, d.actorId, "chest"); if (p) pop(api, p.x, p.y, colors("holy"), burst(colors("holy"), 12, 40, 10), 80, 500); },
    stormhide(api, d) { const p = point(api, d.actorId, "chest"); if (p) scatter(api, p.x, p.y, colors("lightning"), x => `<path d="M2 -10 L-4 0 L2 0 L-2 10" stroke="${x.main}" stroke-width="2.5" fill="none"/>`, 8, 40, 420); },
    transformer(api, d) { const p = point(api, d.actorId, "chest"); if (p) { pop(api, p.x, p.y, { main: "#eab089" }, ring({ main: "#c27a47" }, 32, 4), 100, 600); rise(api, p.x, p.y + 20, { main: "#ffb46b" }, () => `<path d="M0 -10 L8 2 H3 V10 H-3 V2 H-8Z" fill="#ffb46b"/>`, 5, 60, 700, 18); } },
    "seventh-cycle"(api, d) {
      const record = api.sprites.get(d.actorId);
      if (record) record.el.dataset.scCharges = String(d.charges ?? 0);
      if (d.released) { const p = point(api, d.actorId, "chest"); if (p) { pop(api, p.x, p.y, colors("fire"), [0, 1, 2, 3, 4, 5, 6].map(i => `<circle cx="${Math.cos(i * 0.9) * 26}" cy="${Math.sin(i * 0.9) * 26}" r="6" fill="#ff6a2a"/>`).join("") + ring(colors("fire"), 40, 3), 130, 700); flash(api, "#ff6a2a", 260, 0.25); } }
    },
    soulbound(api, d) { const p = point(api, d.actorId, "chest"); if (p) pop(api, p.x, p.y, { main: "#d8f4ff" }, `<circle r="40" fill="#d8f4ff" opacity=".25" stroke="#ffffff" stroke-width="2"/>`, 120, 620); },
  };
  function masterwork(api, d) {
    const item = typeof getItem === "function" ? getItem(d.itemId) : null;
    const unique = item ? window.Sprites?.itemVisual(item)?.unique : null;
    const record = api.sprites.get(d.actorId);
    if (unique === "seventh-cycle" && record) record.el.dataset.scCharges = String(d.charges ?? 0);
    if (api.instant() || !unique || !MASTERWORK_FX[unique]) return;
    // The item itself flares on the sprite while its effect plays.
    const part = record?.el.querySelector(`[data-item="${CSS.escape(d.itemId)}"]`);
    if (part) { part.classList.remove("mw-active"); void part.getBoundingClientRect(); part.classList.add("mw-active"); setTimeout(() => part.classList.remove("mw-active"), 900 * api.pace()); }
    if (record) { record.el.classList.add("mw-armor-active"); setTimeout(() => record.el.classList.remove("mw-armor-active"), 700 * api.pace()); }
    MASTERWORK_FX[unique](api, d, colors("basic"));
  }

  window.BattleFx = { spell, castCircle, projectile, hitSpark, item, stance, meditate, skill, masterwork, ELEMENTS, TIER_TEN };
})();
