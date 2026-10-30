/* Visible equipment for battle sprites. Weapons are drawn in their own frame:
   the grip sits at the origin and the weapon points "up" (negative y) with its
   working edge toward positive x. Armor returns layers for the humanoid rig:
   torso, sleeves, gauntlets, greaves, skirts, head pieces and capes.
   Masterwork items have individual designs and animation hooks. */
(() => {
  "use strict";
  const K = window.SpriteKit;
  const { n, path, flat, line, ellipse, circle, dot, at, capsule, taper, mix, darken, lighten, star } = K;

  const glowAttr = m => m.glow ? ` class="sp-glow" style="color:${m.glow}"` : "";
  const gem = (x, y, r, color) => ellipse(x, y, r, r * 1.15, color, 'stroke-width=".8"') + dot(x - r * 0.3, y - r * 0.35, r * 0.3, "#ffffff", 'opacity=".8"');
  const RARITY_GEMS = ["", "#7fb06a", "#5b8fd8", "#a65bd8", "#e0a43a", "#ff8a3d"];

  // Blade engraving and patterns by material.
  function bladeDetail(m, x1, y1, x2, y2, ornate) {
    let out = line(`M${n(x1)} ${n(y1)} L${n(x2)} ${n(y2)}`, m.dark, 1, 'opacity=".75"');
    if (m.pattern === "waves") for (let i = 0; i < 4; i++) { const t = 0.2 + i * 0.18, x = x1 + (x2 - x1) * t, y = y1 + (y2 - y1) * t; out += line(`M${n(x - 1.4)} ${n(y)} q1.4 -1.6 2.8 0`, m.dark, 0.7, 'opacity=".7"'); }
    if (m.pattern === "runes" || ornate >= 3) for (let i = 0; i < 3; i++) { const t = 0.3 + i * 0.2, x = x1 + (x2 - x1) * t, y = y1 + (y2 - y1) * t; out += line(`M${n(x - 0.8)} ${n(y)} l0.8 -1.2 l0.8 1.2`, m.trim, 0.7); }
    return out;
  }

  /* ---------------- Weapons ---------------- */
  function sword(v, m, length = 28, width = 4.2, guard = 11, gripLength = 6) {
    const tip = -length - 3;
    const blade = path(`M${n(-width / 2)} -3 L${n(-width / 2)} ${n(tip + 5)} L0 ${n(tip)} L${n(width / 2)} ${n(tip + 5)} L${n(width / 2)} -3Z`, m.base);
    const edge = line(`M${n(width / 2 - 0.6)} -4 L${n(width / 2 - 0.6)} ${n(tip + 5)} L0 ${n(tip + 0.8)}`, m.light, 0.9);
    const detail = bladeDetail(m, 0, -5, 0, tip + 6, v.ornate);
    const crossguard = v.ornate >= 2
      ? path(`M${n(-guard / 2)} -1 Q${n(-guard / 2 - 1)} -4 ${n(-guard / 2 + 1)} -4.5 L${n(guard / 2 - 1)} -4.5 Q${n(guard / 2 + 1)} -4 ${n(guard / 2)} -1Z`, m.trim)
      : path(`M${n(-guard / 2)} -1.2 h${n(guard)} v-2.6 h${n(-guard)}Z`, m.trim);
    const handle = path(`M-1.6 -1 h3.2 v${n(gripLength)} h-3.2Z`, m.grip) + line(`M-1.6 ${n(gripLength * 0.35)} h3.2 M-1.6 ${n(gripLength * 0.7)} h3.2`, darken(m.grip, 0.3), 0.6);
    const pommel = circle(0, gripLength + 1.4, 2.1, m.trim);
    const jewel = v.ornate >= 2 ? gem(0, -2.4, 1.3, RARITY_GEMS[v.ornate] || "#7fb4ff") : "";
    return `<g${glowAttr(m)}>${blade}${edge}${detail}</g>${handle}${pommel}${crossguard}${jewel}`;
  }
  function dagger(v, m) { return sword(v, m, 13, 3.4, 7, 5); }
  function greatsword(v, m) { return sword(v, m, 42, 6.2, 16, 11); }

  function axe(v, m, haft = 30, big = false) {
    const head = big
      ? path(`M-1 ${n(-haft + 4)} Q12 ${n(-haft - 6)} 13 ${n(-haft + 6)} Q14 ${n(-haft + 16)} 2 ${n(-haft + 14)} Z M-1 ${n(-haft + 6)} Q-10 ${n(-haft + 2)} -10.5 ${n(-haft + 9)} Q-10 ${n(-haft + 15)} -1 ${n(-haft + 12)}Z`, m.base)
      : path(`M0 ${n(-haft + 5)} Q11 ${n(-haft - 2)} 11.5 ${n(-haft + 7)} Q11 ${n(-haft + 15)} 1 ${n(-haft + 12)}Z`, m.base);
    const edge = line(big ? `M12 ${n(-haft - 2)} Q14.5 ${n(-haft + 6)} 12.5 ${n(-haft + 14)}` : `M10.6 ${n(-haft + 0.5)} Q12.4 ${n(-haft + 7)} 10.4 ${n(-haft + 13)}`, m.light, 1);
    const shaft = path(`M-1.7 8 L-1.7 ${n(-haft)} L1.7 ${n(-haft)} L1.7 8Z`, m.grip === "#2c211c" ? "#4a3426" : lighten(m.grip, 0.12));
    const bands = line(`M-1.7 ${n(-haft + 13)} h3.4 M-1.7 4 h3.4`, m.trim, 1.2);
    return `${shaft}<g${glowAttr(m)}>${head}${edge}</g>${bands}${v.ornate >= 2 ? gem(4.5, -haft + 7, 1.3, RARITY_GEMS[v.ornate]) : ""}`;
  }

  function mace(v, m, handle = 22) {
    const shaft = path(`M-1.6 7 L-1.6 ${n(-handle)} L1.6 ${n(-handle)} L1.6 7Z`, m.grip);
    const head = circle(0, -handle - 4, 5.5, m.base);
    const flanges = [0, 1, 2, 3, 4, 5].map(i => {
      const a = i * Math.PI / 3, x = Math.cos(a) * 5.5, y = -handle - 4 + Math.sin(a) * 5.5;
      return path(`M${n(x * 0.7)} ${n(-handle - 4 + (y + handle + 4) * 0.7)} L${n(x * 1.45)} ${n(-handle - 4 + (y + handle + 4) * 1.45)} L${n(x * 0.85 + Math.cos(a + 0.5) * 2)} ${n(-handle - 4 + (y + handle + 4) * 0.85 + Math.sin(a + 0.5) * 2)}Z`, m.dark, 'stroke-width="1"');
    }).join("");
    return `${shaft}<g${glowAttr(m)}>${flanges}${head}${dot(-1.6, -handle - 6, 1.6, m.light, 'opacity=".8"')}</g>${line(`M-1.6 2 h3.2`, m.trim, 1.2)}${v.ornate >= 2 ? gem(0, -handle - 4, 1.6, RARITY_GEMS[v.ornate]) : ""}`;
  }

  function club(v, m) {
    const wood = m.base === "#8a837a" ? m : K.material("wood");
    return path(`M-1.6 7 L-2.8 -12 Q-6 -26 -1 -31 Q5 -33 5.5 -25 Q5 -16 1.8 -12 L1.6 7Z`, v.material === "stone" ? K.material("stone").base : wood.base) + line("M0 -14 l2 -6 M-2.5 -22 l1.5 2 M2 -27 l-1 2", darken(wood.base, 0.35), 1) + (v.material === "stone" ? circle(1, -26, 4.2, K.material("stone").light) + line("M-1 -28 l3 2", K.material("stone").dark, 0.8) : "");
  }

  function hammer(v, m, handle = 34, big = false) {
    const w = big ? 18 : 13, h = big ? 12 : 8.5;
    return path(`M-1.8 8 L-1.8 ${n(-handle)} L1.8 ${n(-handle)} L1.8 8Z`, m.grip) + `<g${glowAttr(m)}>${path(`M${n(-w / 2)} ${n(-handle - h / 2)} h${n(w)} v${n(h)} h${n(-w)}Z`, m.base)}${line(`M${n(-w / 2 + 1)} ${n(-handle - h / 2 + 1.2)} h${n(w - 2)}`, m.light, 1)}${line(`M${n(-w / 2 + 2)} ${n(-handle + h / 2 - 1)} h${n(w - 4)}`, m.dark, 0.9)}</g>`;
  }

  function spear(v, m, shaft = 46, butt = 12) {
    const wood = v.material === "blacksteel" || v.material === "voidmetal" || v.material === "hellsteel" ? "#3a2b25" : "#7b5634";
    return path(`M-1.3 ${n(butt)} L-1.3 ${n(-shaft)} L1.3 ${n(-shaft)} L1.3 ${n(butt)}Z`, wood) + `<g${glowAttr(m)}>${path(`M0 ${n(-shaft - 14)} Q4.6 ${n(-shaft - 7)} 2.2 ${n(-shaft)} L-2.2 ${n(-shaft)} Q-4.6 ${n(-shaft - 7)} 0 ${n(-shaft - 14)}Z`, m.base)}${line(`M0 ${n(-shaft - 12)} L0 ${n(-shaft - 1)}`, m.dark, 0.8)}${line(`M1 ${n(-shaft - 12)} Q3.2 ${n(-shaft - 7)} 1.6 ${n(-shaft - 1)}`, m.light, 0.8)}</g>${path(`M-2.3 ${n(-shaft + 1)} h4.6 v-2.4 h-4.6Z`, m.trim, 'stroke-width="1"')}${path(`M-1.6 ${n(butt - 1)} h3.2 v2.5 h-3.2Z`, m.dark, 'stroke-width="1"')}`;
  }

  function staff(v, m) {
    const shaft = 44, butt = 14;
    const wood = ["copper", "manacrystal", "crystal", "voidglass", "astral", "celestial", "phoenix", "silver", "obsidian"].includes(v.material) ? lighten(K.material("darkwood").base, 0.1) : m.base;
    const body = path(`M-1.6 ${n(butt)} Q-2.4 ${n(-shaft * 0.5)} -1.4 ${n(-shaft)} L1.4 ${n(-shaft)} Q2.4 ${n(-shaft * 0.5)} 1.6 ${n(butt)}Z`, wood) + line(`M0.4 ${n(butt - 2)} Q1 ${n(-shaft * 0.4)} 0.4 ${n(-shaft + 3)}`, lighten(wood, 0.25), 0.8);
    const top = -shaft;
    let head = "";
    const name = v.name.toLowerCase();
    if (/crystal|manacrystal/.test(name)) head = path(`M-4.5 ${n(top)} Q-6 ${n(top - 6)} -1 ${n(top - 9)} M4.5 ${n(top)} Q6 ${n(top - 6)} 1 ${n(top - 9)}`, "none", `stroke="${m.trim}" stroke-width="1.6"`) + `<g class="sp-glow sp-orb" style="color:${m.glow || m.light}">${path(`M0 ${n(top - 15)} L4 ${n(top - 6)} L0 ${n(top + 1)} L-4 ${n(top - 6)}Z`, m.base)}${line(`M0 ${n(top - 13)} L0 ${n(top - 1)}`, m.light, 0.8)}</g>`;
    else if (/phoenix/.test(name)) head = `<g class="sp-glow sp-flicker" style="color:${m.glow}">${path(`M0 ${n(top + 1)} Q-9 ${n(top - 6)} -5 ${n(top - 16)} Q-2 ${n(top - 8)} 0 ${n(top - 18)} Q2 ${n(top - 8)} 5 ${n(top - 16)} Q9 ${n(top - 6)} 0 ${n(top + 1)}Z`, m.base)}${flat(`M0 ${n(top - 2)} Q-3 ${n(top - 8)} 0 ${n(top - 12)} Q3 ${n(top - 8)} 0 ${n(top - 2)}Z`, m.light)}</g>`;
    else if (/voidglass|arcanist/.test(name)) head = path(`M-5 ${n(top + 2)} L-6 ${n(top - 8)} L0 ${n(top - 3)} L6 ${n(top - 8)} L5 ${n(top + 2)}Z`, m.dark) + `<g class="sp-glow sp-orb" style="color:${m.glow}">${circle(0, top - 7, 4, m.base)}${dot(-1.3, top - 8.4, 1.2, m.light)}</g>`;
    else if (/astral|celestial|starmetal/.test(name)) head = line(`M0 ${n(top + 1)} L0 ${n(top - 4)}`, m.trim, 2) + `<g class="sp-glow sp-orb" style="color:${m.glow}">${path(star(0, top - 10, 7, 2.8, 4), m.trim, 'stroke-width=".8"')}${circle(0, top - 10, 2.2, m.light)}</g>` + path(`M-6 ${n(top - 10)} A6 6 0 0 1 6 ${n(top - 10)}`, "none", `stroke="${m.trim}" stroke-width="1"`);
    else if (/sagewood/.test(name)) head = path(`M0 ${n(top)} Q-6 ${n(top - 6)} -4 ${n(top - 12)} Q1 ${n(top - 9)} 0 ${n(top)}Z`, m.trim) + path(`M0 ${n(top)} Q7 ${n(top - 5)} 5 ${n(top - 11)} Q0 ${n(top - 8)} 0 ${n(top)}Z`, lighten(m.trim, 0.2)) + `<g class="sp-glow sp-orb" style="color:#cfffa0">${circle(0, top - 4, 2.4, "#dff7a8")}</g>`;
    else if (/cinderheart/.test(name)) head = path(`M-5 ${n(top + 1)} L-4 ${n(top - 9)} L0 ${n(top - 13)} L4 ${n(top - 9)} L5 ${n(top + 1)}Z`, m.base) + `<g class="sp-glow sp-flicker" style="color:#ff8a3d">${circle(0, top - 5, 3, "#ff9d4a")}</g>`;
    else if (/copper/.test(name)) head = path(`M-3 ${n(top + 1)} Q-7 ${n(top - 6)} 0 ${n(top - 11)} Q7 ${n(top - 6)} 3 ${n(top + 1)}Z`, "none", `stroke="${K.material("copper").base}" stroke-width="2.2"`) + circle(0, top - 5, 2.6, "#7fd0c0");
    else if (/runed/.test(name)) head = path(`M-3 ${n(top + 2)} L-3 ${n(top - 7)} L0 ${n(top - 11)} L3 ${n(top - 7)} L3 ${n(top + 2)}Z`, m.dark) + `<g class="sp-glow" style="color:#9fd8ff">${line(`M-1 ${n(top - 5)} l1 -2 l1 2 M-1 ${n(top - 1)} h2`, "#bfe8ff", 0.9)}</g>`;
    else if (/pilgrim/.test(name)) head = path(`M1.4 ${n(top)} Q8 ${n(top - 3)} 6 ${n(top - 10)} Q3 ${n(top - 13)} 0 ${n(top - 9)}`, "none", `stroke="${wood}" stroke-width="3"`) + circle(5.5, top + 2, 1.6, "#c9a24a");
    else head = path(`M1.4 ${n(top)} Q7 ${n(top - 4)} 4 ${n(top - 11)} Q0 ${n(top - 13)} -2 ${n(top - 8)}`, "none", `stroke="${wood}" stroke-width="3"`) + `<g class="sp-glow sp-orb" style="color:${m.glow || "#9fd8ff"}">${circle(1, top - 6, 2.8, m.glow || "#9fd8ff")}</g>`;
    return body + head + (v.ornate >= 2 ? path(`M-2.2 ${n(top + 5)} h4.4 v2.4 h-4.4Z`, m.trim, 'stroke-width=".9"') : "");
  }

  function wand(v, m) {
    const name = v.name.toLowerCase();
    const wood = ["copper", "starmetal", "silver"].includes(v.material) ? m.base : ["crystal", "manacrystal", "voidglass", "astral", "phoenix"].includes(v.material) ? K.material("darkwood").base : m.base;
    let tip = `<g class="sp-glow sp-orb" style="color:${m.glow || "#c8d8ff"}">${circle(0, -18, 2.4, m.glow || m.light)}</g>`;
    if (/crystal|manacrystal/.test(name)) tip = `<g class="sp-glow sp-orb" style="color:${m.glow}">${path("M0 -24 L2.6 -18 L0 -14 L-2.6 -18Z", m.base)}</g>`;
    if (/phoenix/.test(name)) tip = `<g class="sp-glow sp-flicker" style="color:${m.glow}">${path("M0 -15 Q-4 -19 -1.5 -25 Q0 -20 1.5 -25 Q4 -19 0 -15Z", m.base)}</g>`;
    if (/voidglass/.test(name)) tip = `<g class="sp-glow sp-orb" style="color:${m.glow}">${circle(0, -18, 2.8, m.base)}${dot(-0.8, -19, 0.8, m.light)}</g>`;
    if (/astral|starmetal/.test(name)) tip = `<g class="sp-glow sp-orb" style="color:${m.glow}">${path(star(0, -19, 4, 1.7, 4), m.trim, 'stroke-width=".45"')}</g>`;
    if (/galebound/.test(name)) tip = `<g class="sp-glow" style="color:#dcfff0">${path("M0 -15 a3 3 0 1 1 3 -4 a2 2 0 1 1 -2 -2", "none", 'stroke="#bfeee0" stroke-width="1.3"')}</g>`;
    return path("M-1.2 5 L-0.8 -15 L0.8 -15 L1.2 5Z", wood) + line("M-1 -2 h2 M-1 1 h2", m.trim === "#7b6b55" ? "#c9a24a" : m.trim, 0.8) + tip;
  }

  function bow(v, m, half = 22, belly = 7, heavy = false) {
    const limb = heavy ? 3.4 : 2.6;
    const curve = `M0 ${n(-half)} Q${n(belly * 1.8)} ${n(-half * 0.45)} ${n(belly * 0.55)} 0 Q${n(belly * 1.8)} ${n(half * 0.45)} 0 ${n(half)}`;
    let out = `<g class="sp-bowstring">${line(`M0.4 ${n(-half + 1)} L0.4 ${n(half - 1)}`, "#efe6d0", 0.7)}</g>`;
    out += `<g${glowAttr(m)}>${path(curve + ` L${n(-limb * 0.4)} ${n(half - 1.5)} Q${n(belly * 1.8 - limb)} ${n(half * 0.45)} ${n(belly * 0.55 - limb)} 0 Q${n(belly * 1.8 - limb)} ${n(-half * 0.45)} ${n(-limb * 0.4)} ${n(-half + 1.5)}Z`, m.base)}</g>`;
    out += path(`M${n(belly * 0.55 - 2)} -3 h3.4 v6 h-3.4Z`, m.grip, 'stroke-width="1"');
    if (v.ornate >= 2) out += line(`M${n(belly * 1.02 - limb - 0.3)} ${n(-half * 0.5)} L${n(belly * 1.02 + 0.3)} ${n(-half * 0.5 + 0.7)} M${n(belly * 1.02 - limb - 0.3)} ${n(half * 0.5)} L${n(belly * 1.02 + 0.3)} ${n(half * 0.5 - 0.7)}`, m.trim, 1.3);
    if (/scaled/i.test(v.name)) for (let i = -2; i <= 2; i++) out += line(`M${n(belly * 0.9 + Math.abs(i) * 0.3)} ${n(i * half * 0.18)} q1.4 1 0 2`, m.dark, 0.7);
    if (heavy) out += path(`M${n(belly * 0.4)} ${n(-half * 0.35)} h4 v3 h-4Z M${n(belly * 0.4)} ${n(half * 0.35 - 3)} h4 v3 h-4Z`, K.material("iron").base, 'stroke-width=".9"');
    return out;
  }
  function crossbow(v, m) {
    return path("M-2 8 L-2 -26 L2 -26 L2 8Z", K.material("darkwood").base) + path("M-14 -22 Q0 -30 14 -22 L13 -19 Q0 -26 -13 -19Z", m.base) + line("M-13 -20 L0 -12 L13 -20", "#efe6d0", 0.7) + path("M-1.4 -34 L1.4 -34 L0 -40Z M-0.6 -34 L0.6 -34 L0.6 -14 L-0.6 -14Z", K.material("iron").base, 'stroke-width=".8"');
  }

  function trident(v, m) {
    const shaft = 46;
    return path(`M-1.3 12 L-1.3 ${n(-shaft)} L1.3 ${n(-shaft)} L1.3 12Z`, m.grip === "#6b4a22" ? "#a5791c" : "#7b5634") + `<g${glowAttr(m)}>${path(`M-7 ${n(-shaft - 1)} Q-7 ${n(-shaft + 3)} 0 ${n(-shaft + 3)} Q7 ${n(-shaft + 3)} 7 ${n(-shaft - 1)} L7 ${n(-shaft - 12)} L5 ${n(-shaft - 7)} L5 ${n(-shaft - 1)} L1.2 ${n(-shaft - 1)} L1.2 ${n(-shaft - 16)} L0 ${n(-shaft - 20)} L-1.2 ${n(-shaft - 16)} L-1.2 ${n(-shaft - 1)} L-5 ${n(-shaft - 1)} L-5 ${n(-shaft - 7)} L-7 ${n(-shaft - 12)}Z`, m.base)}</g>`;
  }

  function rapier(v, m) {
    return `<g${glowAttr(m)}>${path("M-1 -3 L-1 -32 L0 -35 L1 -32 L1 -3Z", m.base)}</g>` + path("M-1.6 -1 h3.2 v6 h-3.2Z", m.grip) + path("M-1 -2 Q-8 -1 -6 5 Q-4 0 0 0Z M1 -2 Q9 -6 7 -12 Q4 -4 0 -3Z", m.trim, 'stroke-width="1"') + circle(0, 6.5, 1.8, m.trim);
  }

  /* ---------------- Masterwork weapons ---------------- */
  const MASTERWORK_WEAPONS = {
    starscale(v) {
      const blade = [];
      for (let i = 0; i < 6; i++) blade.push(path(`M${n(-2.6)} ${n(-4 - i * 5)} Q0 ${n(-9 - i * 5)} ${n(2.6)} ${n(-4 - i * 5)} L${n(2.2)} ${n(-1 - i * 5)} Q0 ${n(-5 - i * 5)} ${n(-2.2)} ${n(-1 - i * 5)}Z`, i % 2 ? "#e9f1ff" : "#cfdcf5", 'stroke-width=".8"'));
      return `<g class="sp-glow" style="color:#e2ecff">${path("M-2.6 -2 L-2.6 -32 L0 -38 L2.6 -32 L2.6 -2Z", "#dbe6fb")}${blade.join("")}</g>${path("M-7 -1 Q-8 -5 -4 -4 L4 -4 Q8 -5 7 -1Z", "#f2cf73")}<g class="mw-fx">${path(star(0, -2.5, 3.4, 1.4, 4), "#ffffff", 'stroke-width=".4"')}</g>${path("M-1.6 -1 h3.2 v6 h-3.2Z", "#3a3f63")}${circle(0, 6.4, 2, "#f2cf73")}`;
    },
    "golden-apex"() {
      return path("M-1.8 15 Q-2.6 -20 -1.6 -44 L1.6 -44 Q2.6 -20 1.8 15Z", "#e0b54a") + line("M0.4 12 Q1 -14 0.4 -42", "#fbe8a2", 0.9) + path("M-9 -44 L-11 -56 L-5 -50 L0 -60 L5 -50 L11 -56 L9 -44Z", "#f0c548") + `<g class="sp-glow sp-orb mw-fx" style="color:#ffe27a">${circle(0, -51, 4.6, "#fff4c8")}${circle(0, -51, 9, "none", 'stroke="#ffe27a" stroke-width=".9" stroke-dasharray="2 2"')}</g>` + path("M-3 -40 h6 v3 h-6Z", "#fff3c4", 'stroke-width=".8"');
    },
    giantslayer() {
      return `<g class="sp-glow" style="color:#bfe0ff">${path("M-4.6 -3 L-4.6 -44 L-2 -50 L2 -52 L4.6 -46 L4.6 -3Z", "#a9b3bd")}${line("M-1 -6 L-1 -44", "#6f7880", 1.2)}${path("M4.6 -16 l-2 -2 l2 -2 M4.6 -28 l-2 -2 l2 -2", "#6f7880", 'stroke-width=".9" fill="none"')}</g><g class="mw-fx">${line("M-3 -12 l1.2 -2 l1.2 2 M-3 -24 l1.2 -2 l1.2 2 M-3 -36 l1.2 -2 l1.2 2", "#7fd0ff", 1)}</g>${path("M-10 -1 L-9 -5 L9 -5 L10 -1Z", "#5b4630")}${path("M-2 -1 h4 v12 h-4Z", "#3e2c22")}${circle(0, 12.5, 2.6, "#5b4630")}`;
    },
    "forest-branch"() {
      const leaves = [[-5, -38, -40], [5, -44, 35], [-3, -50, -20], [4, -30, 50], [-4, -24, -60]].map(([x, y, r]) => at(x, y, path("M0 0 Q4 -3 7 0 Q4 3 0 0Z", "#7fbf5a", 'stroke-width=".8"'), r)).join("");
      return path("M-2 14 Q-4 -2 -1 -14 Q2 -26 -1.5 -38 Q0 -46 2 -52 L3.5 -51 Q2.5 -44 1.4 -38 Q4.6 -26 1.6 -14 Q-1 -2 2 14Z", "#6b4527") + leaves + `<g class="sp-glow sp-orb mw-fx" style="color:#cfffa0">${circle(1.5, -54, 3.2, "#dff7a8")}</g>`;
    },
    keening() {
      return `<g class="sp-glow" style="color:#bfe8ff">${path("M-2 -3 Q-4 -18 2 -36 Q3 -20 3 -3Z", "#c9e6f2")}${line("M-0.4 -8 q1.6 -3 0 -6 q-1.6 -3 0 -6 q1.6 -3 0 -6", "#7fb4d8", 0.8)}</g><g class="mw-fx">${path("M4 -18 q3 0 3 3 M5 -24 q4 0 4 4", "none", 'stroke="#bfe8ff" stroke-width=".8" opacity=".8"')}</g>${path("M-6 -1 Q0 -5 6 -1 L5 0 Q0 -3 -5 0Z", "#5d6f7d")}${path("M-1.5 -1 h3 v6 h-3Z", "#2c3a4a")}${circle(0, 6.2, 1.8, "#9fb6d4")}`;
    },
    sunder() {
      return path("M-1.9 8 L-1.9 -36 L1.9 -36 L1.9 8Z", "#3e3a4a") + path("M-11 -44 h17 l4 4 l-4 4 h-17Z", "#5b6b8a") + line("M-10 -42 h15", "#9fb6d4", 1) + `<g class="sp-glow mw-fx" style="color:#7fb4ff">${path("M-7 -40 l2 -3 l2 3 l-2 3Z M0 -40 l2 -3 l2 3 l-2 3Z", "#86b8ff", 'stroke-width=".7"')}</g>`;
    },
    "anvil-hammer"() {
      return path("M-2 8 L-2 -32 L2 -32 L2 8Z", "#4a3426") + path("M-12 -40 L8 -40 Q13 -40 14 -36 L4 -35 L4 -31 L8 -28 L-8 -28 L-4 -31 L-4 -35 L-12 -35Z", "#4f545b") + line("M-11 -39 h18", "#8d949c", 1) + `<g class="mw-fx">${dot(12, -38, 1.2, "#ffb46b")}${dot(-11, -37, 1, "#ffb46b")}</g>`;
    },
    beowulf() {
      return `<g class="sp-glow" style="color:#9fb4ff">${path("M-5 -3 L-5 -40 Q-6 -46 -2 -50 L0 -46 L2 -52 L3 -45 L6 -48 L5 -40 L5 -3Z", "#2a2d3a")}${line("M0 -6 L0 -40", "#4a5070", 1)}</g><g class="mw-fx">${line("M-2 -12 l3 -4 l-2 -1 l3 -5 M1 -26 l-3 4 l2 1 l-3 4", "#bcd2ff", 0.9)}</g>${path("M-11 -1 L-12 -5 L12 -5 L11 -1Z", "#1d1f2a")}${path("M-2 -1 h4 v12 h-4Z", "#121318")}${circle(0, 12.4, 2.4, "#4a5070")}`;
    },
    "royal-trident"(v) {
      return trident({ ...v, ornate: 4 }, K.material("gold")) + `<g class="mw-fx">${circle(0, -46, 2.2, "#f5f0ff", 'stroke-width=".8"')}${path("M-4 -44 q-3 3 -6 1 M4 -44 q3 3 6 1", "none", 'stroke="#ff8a8a" stroke-width="1.4"')}</g>`;
    },
    "guild-staff"() {
      return path("M-1.8 14 Q-2.6 -20 -1.6 -42 L1.6 -42 Q2.6 -20 1.8 14Z", "#3d3a33") + path("M-8 -42 L8 -42 L6 -56 L0 -60 L-6 -56Z", "#5a6b4a") + path("M-4 -54 L4 -54 L4 -47 Q0 -44 -4 -47Z", "#c9b46a", 'stroke-width=".9"') + line("M-2.6 -52 L2.6 -47 M2.6 -52 L-2.6 -47", "#3a2f22", 0.9) + `<g class="sp-glow sp-orb mw-fx" style="color:#7cff9a">${circle(0, -63, 2.6, "#b8ffc8")}</g>`;
    },
    enricher() {
      return `<g class="sp-glow" style="color:#ffd86b">${path("M-1.8 -3 L-1.8 -15 L0 -19 L1.8 -15 L1.8 -3Z", "#2b2a2e")}${line("M0.9 -4 L0.9 -15", "#e0b54a", 0.8)}</g>${path("M-5 -1 h10 v-2.4 h-10Z", "#e0b54a")}${path("M-1.5 -1 h3 v5 h-3Z", "#1a1416")}<g class="mw-fx">${circle(0, 6.4, 2.4, "#f0c548")}${line("M-1 6.4 h2", "#9a7224", 0.7)}</g>`;
    },
    "hellish-rebuke"() {
      return path("M-1.3 5 L-0.9 -15 L0.9 -15 L1.3 5Z", "#2a1414") + path("M-3 -15 Q-6 -20 -4 -24 Q-2 -19 0 -18 Q2 -19 4 -24 Q6 -20 3 -15Z", "#5a1a14") + `<g class="sp-glow sp-flicker mw-fx" style="color:#ff6a2a">${path("M0 -17 Q-3 -21 -1 -27 Q0 -23 1 -27 Q3 -21 0 -17Z", "#ff7a2e")}</g>`;
    },
    jackhammer() {
      return path("M-2.4 10 L-2.4 -34 L2.4 -34 L2.4 10Z", "#5a4630") + path("M-15 -50 L13 -50 L15 -46 L15 -36 L13 -32 L-15 -32 L-17 -36 L-17 -46Z", "#6f685f") + line("M-14 -46 h27 M-14 -41 h27 M-14 -36 h27", "#4f4942", 1) + `<g class="sp-glow mw-fx" style="color:#e8c75e">${line("M-8 -48 l3 6 l-2 2 l3 6 M6 -48 l-2 5 l3 2 l-2 6", "#e8c75e", 1.1)}</g>`;
    },
  };

  function weapon(v) {
    if (!v) return "";
    if (v.forged && window.ForgeArt) return window.ForgeArt.sprite(v);
    if (v.unique && MASTERWORK_WEAPONS[v.unique]) return MASTERWORK_WEAPONS[v.unique](v);
    const m = K.material(v.material);
    switch (v.kind) {
      case "dagger": return dagger(v, m);
      case "greatsword": return greatsword(v, m);
      case "axe": return axe(v, m, 28, false);
      case "greataxe": return axe(v, m, 42, true);
      case "mace": return mace(v, m);
      case "club": return club(v, m);
      case "hammer": return hammer(v, m, 34, v.handed === 2);
      case "spear": return spear(v, m);
      case "trident": return trident(v, m);
      case "rapier": return rapier(v, m);
      case "staff": return staff(v, m);
      case "wand": return wand(v, m);
      case "bow": return bow(v, m, 21, 6.5);
      case "longbow": return bow(v, m, 27, 7);
      case "greatbow": return bow(v, m, 30, 8.5, true);
      case "crossbow": return crossbow(v, m);
      default: return sword(v, m);
    }
  }

  // The weapon's resting angle in the world, minus the arm's idle angle.
  function gripAngle(v, hand, frontIdle, backIdle) {
    const idle = hand === "back" ? backIdle : frontIdle;
    let world = 40;
    if (["spear", "trident", "staff"].includes(v.kind) || ["golden-apex", "forest-branch", "guild-staff", "royal-trident"].includes(v.unique)) world = 6;
    else if (["bow", "longbow", "greatbow"].includes(v.kind)) world = -4;
    else if (v.kind === "crossbow") world = 80;
    else if (v.kind === "wand") world = 58;
    else if (["greatsword", "greataxe"].includes(v.kind) || v.handed === 2) world = 28;
    else if (v.kind === "dagger") world = 70;
    if (hand === "back") world += 14;
    return world - idle;
  }

  // Natural weapons drawn over the hand: beast claws and celestial talons.
  function claws(v, b) {
    const celestial = v.unique === "stardragon-claws";
    const color = celestial ? "#eef4ff" : "#efe6d0";
    const tips = [-2.4, 0, 2.4].map(x => path(`M${n(x - 1)} ${n(b.arm + 1)} Q${n(x + 2.5)} ${n(b.arm + 4)} ${n(x + 1.5)} ${n(b.arm + 7.5)} Q${n(x + 0.8)} ${n(b.arm + 4.5)} ${n(x - 1)} ${n(b.arm + 2.5)}Z`, color, 'stroke-width=".9"')).join("");
    return celestial ? `<g class="sp-glow sp-masterwork mw-stardragon-claws" style="color:#d6e6ff" data-item="${v.id}">${tips}</g>` : tips;
  }

  /* ---------------- Shields ---------------- */
  const MASTERWORK_SHIELDS = {
    // An Abyss kite shield in its own Mythilium, spiked at the shoulders, bearing a crimson
    // imp sigil whose eyes burn with void light.
    impshield(v) {
      const m = K.material(v.material || "iron"), crimson = "#a8263b";
      const kite = "M0 -15 Q9 -14 10 -8 Q9 6 0 18 Q-9 6 -10 -8 Q-9 -14 0 -15Z";
      const spikes = path("M-9.4 -11 L-13.6 -13.4 L-9.8 -7.6Z M9.4 -11 L13.6 -13.4 L9.8 -7.6Z", m.dark, 'stroke-width=".7"');
      const wings = path("M-2 -3 Q-6.6 -9 -9.2 -6.4 L-7.8 -5 L-8.6 -3 L-6.4 -3.4 L-6.4 -1 L-4.4 -2 L-3.6 0.6Z M2 -3 Q6.6 -9 9.2 -6.4 L7.8 -5 L8.6 -3 L6.4 -3.4 L6.4 -1 L4.4 -2 L3.6 0.6Z", crimson, 'stroke-width=".8"');
      const head = path("M-3 -1 Q-3.6 -5.6 0 -5.8 Q3.6 -5.6 3 -1 Q2.2 2.2 0 2.6 Q-2.2 2.2 -3 -1Z", crimson, 'stroke-width=".8"')
        + path("M-2 -5 Q-4.4 -8 -3.2 -10.6 Q-2.6 -7.8 -0.8 -5.6Z M2 -5 Q4.4 -8 3.2 -10.6 Q2.6 -7.8 0.8 -5.6Z", "#1d1414", 'stroke-width=".6"')
        + line("M-1.6 0.4 Q0 1.6 1.6 0.4", "#4a0f18", 0.7);
      const eyes = path("M-2.2 -3 L-0.6 -2.4 L-1.1 -1.8Z M2.2 -3 L0.6 -2.4 L1.1 -1.8Z", "!#c58cff", 'stroke-width=".3"');
      return path(kite, m.base) + path("M0 -12 Q7 -11 7.6 -7 Q7 4 0 14 Q-7 4 -7.6 -7 Q-7 -11 0 -12Z", "none", `stroke="${m.dark}" stroke-width=".9"`) + spikes
        + `<g class="mw-fx">${wings}${head}<g class="sp-glow" style="color:#c58cff">${eyes}</g></g>`;
    },
    transformer() {
      return circle(0, 0, 12.5, "#c27a47") + circle(0, 0, 8.4, "#7d4523") + `<g class="mw-fx">${path("M-8 0 a2.4 2.4 0 0 1 4.8 0 a2.4 2.4 0 0 1 4.8 0 a2.4 2.4 0 0 1 4.8 0", "none", 'stroke="#eab089" stroke-width="1.3"')}${dot(0, -6, 1.4, "#7fd0c0")}${dot(0, 6, 1.4, "#7fd0c0")}</g>`;
    },
  };
  // Every shield face is inked like the body behind it: a shadowed rear crescent and lower rim.
  const SHIELD_OUTLINES = {
    round: "M-11.5 0 a11.5 11.5 0 1 0 23 0 a11.5 11.5 0 1 0 -23 0Z",
    aegis: "M-12.5 0 a12.5 12.5 0 1 0 25 0 a12.5 12.5 0 1 0 -25 0Z",
    tower: "M-9 -17 L9 -17 L9 13 Q0 19 -9 13Z",
    slab: "M-10 -15 L9 -16 L11 12 L-9 14Z",
    heater: "M-10 -13 L10 -13 L10 0 Q8 11 0 15 Q-8 11 -10 0Z",
    kite: "M0 -15 Q9 -14 10 -8 Q9 6 0 18 Q-9 6 -10 -8 Q-9 -14 0 -15Z",
    impshield: "M0 -15 Q9 -14 10 -8 Q9 6 0 18 Q-9 6 -10 -8 Q-9 -14 0 -15Z",
  };
  function shieldShade(d) {
    return K.clipped(d, `<path d="M-20 -22 L-2 -22 Q-8 0 -2 22 L-20 22Z" fill="${K.INK}" opacity=".5"/><path d="M-20 7 Q0 4 20 9 L20 22 L-20 22Z" fill="${K.INK}" opacity=".3"/>` + K.hatch(-4.5, -16, 3, 32, 72, 2.6, 0.45, 0.35));
  }
  function shield(v) {
    if (!v) return "";
    const kind = v.unique && MASTERWORK_SHIELDS[v.unique] ? v.unique : v.kind;
    const outline = SHIELD_OUTLINES[kind === "round-wood" || kind === "transformer" ? (kind === "transformer" ? "aegis" : "round") : kind] || SHIELD_OUTLINES.kite;
    return shieldFace(v) + shieldShade(outline);
  }
  function shieldFace(v) {
    if (v.unique && MASTERWORK_SHIELDS[v.unique]) return MASTERWORK_SHIELDS[v.unique](v);
    const m = K.material(v.material);
    const kind = v.kind;
    // Shields are plain metal and wood; only masterworks carry a light.
    const glowWrap = inner => inner;
    if (kind === "round-wood") {
      const wood = K.material("wood");
      return circle(0, 0, 11.5, wood.base) + line("M-11 -4 h22 M-11 4 h22", wood.dark, 0.9) + circle(0, 0, 11.5, "none", `stroke="${K.material("iron").dark}" stroke-width="2"`) + circle(0, 0, 3.2, K.material("iron").base);
    }
    if (kind === "round") return circle(0, 0, 11.5, m.base) + circle(0, 0, 8.5, "none", `stroke="${m.dark}" stroke-width="1"`) + circle(0, 0, 3.4, m.light) + [0, 1, 2, 3, 4, 5].map(i => dot(Math.cos(i * 1.047) * 10, Math.sin(i * 1.047) * 10, 0.8, m.dark)).join("");
    if (kind === "tower") return path("M-9 -17 L9 -17 L9 13 Q0 19 -9 13Z", m.base) + path("M-6.5 -14 L6.5 -14 L6.5 11 Q0 15.5 -6.5 11Z", "none", `stroke="${m.trim}" stroke-width="1.4"`) + line("M0 -13 v24 M-5 -2 h10", m.trim, 1.6) + dot(0, -2, 1.4, m.trim);
    if (kind === "slab") return path("M-10 -15 L9 -16 L11 12 L-9 14Z", m.base) + line("M-6 -8 l5 3 l-2 6 M4 -11 l-2 6 l5 4 M-4 6 l6 2", m.dark, 1) + path("M-10 -15 L9 -16 L9 -12 L-10 -11Z", m.light, 'stroke-width=".8"');
    if (kind === "aegis") {
      const emblem = v.material === "starmetal" ? path(star(0, 0, 6, 2.5, 8), m.trim, 'stroke-width=".6"') : v.material === "manasteel" ? path("M0 -6 L5 0 L0 6 L-5 0Z", m.glow || m.trim) : circle(0, 0, 4, m.trim);
      return glowWrap(circle(0, 0, 12.5, m.base) + circle(0, 0, 10, "none", `stroke="${m.trim}" stroke-width="1.6"`) + emblem);
    }
    if (kind === "heater") return glowWrap(path("M-10 -13 L10 -13 L10 0 Q8 11 0 15 Q-8 11 -10 0Z", m.base) + path("M-10 -13 L10 -13 L10 -8 L-10 -8Z", m.trim, 'stroke-width=".9"') + (v.ornate >= 2 ? path("M0 -6 L4 1 L0 9 L-4 1Z", m.light, 'stroke-width=".8"') : line("M0 -7 v18", m.dark, 1.2)));
    // Kite shields and wards.
    const emblem = v.material === "celestial" ? path(star(0, -2, 5, 2.2, 8), "#f2cf73", 'stroke-width=".5"') : v.material === "voidmetal" ? circle(0, -1, 3.2, m.glow || m.light) : v.material === "damascus" ? line("M-5 -5 q2.5 -2 5 0 t5 0 M-5 1 q2.5 -2 5 0 t5 0", m.dark, 0.9) : line("M0 -11 v24 M-6 -2 h12", m.trim, 1.4);
    return glowWrap(path("M0 -15 Q9 -14 10 -8 Q9 6 0 18 Q-9 6 -10 -8 Q-9 -14 0 -15Z", m.base) + path("M0 -12 Q7 -11 7.6 -7 Q7 4 0 14 Q-7 4 -7.6 -7 Q-7 -11 0 -12Z", "none", `stroke="${m.dark}" stroke-width=".9"`) + emblem);
  }

  /* ---------------- Armor ---------------- */
  function chainTexture(x, y, w, h, color) {
    let out = "";
    for (let row = 0; row < Math.floor(h / 2.6); row++) for (let col = 0; col < Math.floor(w / 3); col++) out += `M${n(x + col * 3 + (row % 2) * 1.5)} ${n(y + row * 2.6)} q1.5 1.6 3 0 `;
    return out ? line(out, color, 0.6, 'opacity=".8"') : "";
  }
  function scaleTexture(x, y, w, h, color) {
    let out = "";
    for (let row = 0; row < Math.floor(h / 3.2); row++) for (let col = 0; col < Math.floor(w / 3.6); col++) out += `M${n(x + col * 3.6 + (row % 2) * 1.8)} ${n(y + row * 3.2)} q1.8 2.4 3.6 0 `;
    return out ? line(out, color, 0.7) : "";
  }

  // A cape hangs behind the body, so all but its outer edge is in shadow; folds run down it.
  function capeShape(d, color, S, sy) {
    const folds = line(`M${n(-S - 2)} ${n(sy + 10)} q-4 30 -3 ${n(-sy - 16)} M${n(-S * 0.5)} ${n(sy + 8)} q-2 30 0 ${n(-sy - 14)}`, K.INK, 0.8, 'opacity=".55"');
    return `<g class="sp-cape">${path(d, color)}${K.clipped(d, `<path d="M${n(-S * 0.75)} ${n(sy - 4)} Q${n(-S * 1.2)} ${n(sy * 0.4)} ${n(-S * 0.9)} 4 L${n(S * 2)} 4 L${n(S * 2)} ${n(sy - 4)}Z" fill="${K.INK}" opacity=".62"/>` + K.hatch(-S * 1.25, sy, S * 0.3, -sy, 76, 2.8, 0.45, 0.35))}${folds}</g>`;
  }

  // Each armor item becomes layers for the humanoid rig.
  function armorLayers(v, g, p, s) {
    const m = K.material(v.material);
    const female = p.sex === "female";
    const { sy, hy, S, H, T } = g;
    const torsoBase = (fill, extra = "") => path(torsoOutline(g, female), fill, extra);
    const layers = { torso: "", skirt: "", sleeve: null, hand: null, leg: null, head: null, hidesHair: false, cape: "" };
    const unique = v.unique;
    const kind = v.kind;
    const metal = (back, base) => back ? darken(base, 0.12) : base;

    if (v.type === "heavy" || unique === "godforged" || unique === "seventh-cycle") {
      const mm = unique === "godforged" ? { ...K.material("celestial"), base: "#efe2b8", trim: "#f2cf73", glow: "#fff2b8" }
        : unique === "seventh-cycle" ? { ...K.material("hellsteel"), base: "#4a1a1a", trim: "#c25a32", glow: "#ff6a2a" }
        : m;
      const chain = kind === "chain";
      if (chain) {
        layers.torso = torsoBase(mm.base) + K.clipped(torsoOutline(g, female), chainTexture(-S + 1.5, sy + 2, S * 2 - 3, T - 3, mm.dark) + path(`M${n(-H - 0.5)} ${n(hy - 2)} h${n(H * 2 + 1)} v4 h${n(-H * 2 - 1)}Z`, "#5a3d26"));
        layers.head = { back: path(`M${n(g.hx - g.R * 0.3)} ${n(g.headY - g.R * 1.05)} C${n(g.hx - g.R * 1.4)} ${n(g.headY - g.R * 0.9)} ${n(g.hx - g.R * 1.35)} ${n(g.headY + g.R * 0.8)} ${n(g.hx - g.R * 0.9)} ${n(g.headY + g.R * 1.1)} L${n(g.hx - g.R * 0.2)} ${n(g.headY + g.R * 1.0)}Z`, mm.base) + chainTexture(g.hx - g.R * 1.2, g.headY - g.R * 0.6, g.R * 1.0, g.R * 1.5, mm.dark), front: path(`M${n(g.hx + g.R * 0.85)} ${n(g.headY - g.R * 0.35)} C${n(g.hx + g.R * 0.9)} ${n(g.headY - g.R * 1.2)} ${n(g.hx - g.R * 0.95)} ${n(g.headY - g.R * 1.25)} ${n(g.hx - g.R * 1.1)} ${n(g.headY - g.R * 0.2)} L${n(g.hx - g.R * 0.45)} ${n(g.headY + g.R * 0.15)} C${n(g.hx - g.R * 0.35)} ${n(g.headY - g.R * 0.45)} ${n(g.hx + g.R * 0.2)} ${n(g.headY - g.R * 0.6)} ${n(g.hx + g.R * 0.85)} ${n(g.headY - g.R * 0.35)}Z`, mm.base) + chainTexture(g.hx - g.R * 0.8, g.headY - g.R * 0.95, g.R * 1.5, g.R * 0.5, mm.dark) };
        layers.hidesHair = true;
        layers.sleeve = back => path(`M${n(-g.armW / 2 - 1)} -2 L${n(g.armW / 2 + 1)} -2 L${n(g.armW / 2 + 0.8)} ${n(g.arm * 0.6)} L${n(-g.armW / 2 - 0.8)} ${n(g.arm * 0.6)}Z`, metal(back, mm.base)) + chainTexture(-g.armW / 2, 0, g.armW, g.arm * 0.55, mm.dark);
        layers.skirt = `<g class="sp-skirt">${path(`M${n(-H - 0.5)} ${n(hy + 2)} L${n(H + 0.5)} ${n(hy + 2)} L${n(H + 1.5)} ${n(hy + g.legs * 0.42)} L${n(-H - 1.5)} ${n(hy + g.legs * 0.42)}Z`, mm.base)}${chainTexture(-H, hy + 3, H * 2, g.legs * 0.36, mm.dark)}</g>`;
        return layers;
      }
      // Plate armor.
      const plates = [];
      plates.push(torsoBase(mm.base));
      plates.push(line(`M${n(S * 0.25)} ${n(sy + 2)} Q${n(S * 0.55)} ${n(sy + T * 0.4)} ${n(S * 0.3)} ${n(sy + T * 0.7)}`, mm.light, 1, 'opacity=".55"'));
      for (let i = 0; i < 3; i++) plates.push(line(`M${n(-H)} ${n(hy - 2 - i * 3.4)} Q0 ${n(hy - i * 3.4)} ${n(H)} ${n(hy - 2 - i * 3.4)}`, mm.dark, 0.9));
      const gorgetD = `M${n(-S * 0.55)} ${n(sy - 1)} Q0 ${n(sy + 3.5)} ${n(S * 0.65)} ${n(sy - 1)} L${n(S * 0.55)} ${n(sy - 3)} Q0 ${n(sy)} ${n(-S * 0.45)} ${n(sy - 3)}Z`;
      const gorget = path(gorgetD, mm.base, 'stroke-width="1"') + K.clipped(gorgetD, `<rect x="${n(-S)}" y="${n(sy - 4)}" width="${n(S * 0.85)}" height="8" fill="${K.INK}" opacity=".5"/>`) + line(`M${n(S * 0.05)} ${n(sy + 0.6)} Q${n(S * 0.35)} ${n(sy + 0.2)} ${n(S * 0.55)} ${n(sy - 1.4)}`, mm.light, 0.8, 'opacity=".7"');
      if (v.ornate >= 2 || unique) plates.push(line(`M${n(-S + 1)} ${n(sy + 3)} Q0 ${n(sy + 6)} ${n(S - 1)} ${n(sy + 3)}`, mm.trim, 1.2));
      if (v.ornate >= 3 && !unique) plates.push(gem(S * 0.15, sy + T * 0.35, 1.6, RARITY_GEMS[v.ornate]));
      if (v.material === "dwarven") plates.push(line(`M${n(-S * 0.3)} ${n(sy + T * 0.35)} l2 -3 l2 3 l2 -3`, mm.trim, 0.9));
      if (unique === "godforged") layers.torsoGlow = `<g class="sp-glow mw-fx" style="color:#fff2b8">${path(star(-S * 0.18, sy + T * 0.36, 4.6, 2.3, 8), "!#f2cf73", 'stroke="#6b4a12" stroke-width=".4"') + dot(-S * 0.18, sy + T * 0.36, 1.1, "#fff6d8")}</g>`;
      if (unique === "seventh-cycle") {
        const rings = [];
        for (let i = 0; i < 7; i++) rings.push(circle(-S * 0.55 + (i % 4) * S * 0.36, sy + T * (0.3 + Math.floor(i / 4) * 0.26), 1.7, "#2a0f0f", `class="sc-ring sc-ring-${i + 1}" stroke="#c25a32" stroke-width=".8"`));
        plates.push(`<g class="mw-fx">${rings.join("")}</g>`);
      }
      plates.push(path(`M${n(-H - 0.5)} ${n(hy - 1.5)} h${n(H * 2 + 1)} v3.6 h${n(-H * 2 - 1)}Z`, "#4a3326"));
      layers.torso = plates[0] + K.clipped(torsoOutline(g, female), plates.slice(1).join("")) + gorget;
      // Tassets hang over the thighs.
      layers.skirt = `<g class="sp-skirt">${path(`M${n(-H - 1)} ${n(hy + 2)} L${n(-1)} ${n(hy + 2)} L${n(-1.5)} ${n(hy + g.legs * 0.38)} L${n(-H - 2)} ${n(hy + g.legs * 0.34)}Z`, darken(mm.base, 0.08))}${path(`M${n(1)} ${n(hy + 2)} L${n(H + 1)} ${n(hy + 2)} L${n(H + 2)} ${n(hy + g.legs * 0.34)} L${n(1.5)} ${n(hy + g.legs * 0.38)}Z`, mm.base)}</g>`;
      layers.sleeve = back => {
        const w = g.armW;
        const pauldron = `M${n(-w / 2 - 2.6)} 4 Q${n(-w / 2 - 2)} -4.5 0 -4.8 Q${n(w / 2 + 2)} -4.5 ${n(w / 2 + 2.6)} 4 Q0 6 ${n(-w / 2 - 2.6)} 4Z`;
        return path(`M${n(-w / 2 - 0.6)} 2 L${n(w / 2 + 0.6)} 2 L${n(w / 2 + 0.4)} ${n(g.arm * 0.75)} L${n(-w / 2 - 0.4)} ${n(g.arm * 0.75)}Z`, metal(back, mm.base)) + line(`M${n(-w / 2)} ${n(g.arm * 0.42)} h${n(w)}`, mm.dark, 0.9)
          + path(pauldron, metal(back, mm.base)) + K.clipped(pauldron, `<path d="M${n(-w - 4)} -6 L${n(-w * 0.15)} -6 Q${n(w * 0.15)} 0 ${n(w * 0.4)} 7 L${n(-w - 4)} 7Z" fill="${K.INK}" opacity=".55"/><rect x="${n(-w - 4)}" y="1.6" width="${n(w * 2 + 8)}" height="6" fill="${K.INK}" opacity=".35"/>`) + line(`M${n(-w * 0.05)} -3.2 Q${n(w * 0.45)} -3.4 ${n(w / 2 + 1.4)} 0.4`, mm.light, 0.9, 'opacity=".75"');
      };
      layers.hand = back => {
        const w = g.armW, top = g.arm - w * 1.2, fy = g.arm - w * 0.35;
        const cuff = `M${n(-w * 0.62)} ${n(top)} L${n(w * 0.62)} ${n(top)} L${n(w * 0.72)} ${n(fy - w * 0.25)} L${n(-w * 0.72)} ${n(fy - w * 0.25)}Z`;
        const fist = `M${n(-w * 0.55)} ${n(fy - w * 0.32)} Q${n(-w * 0.68)} ${n(fy + w * 0.64)} 0 ${n(fy + w * 0.72)} Q${n(w * 0.68)} ${n(fy + w * 0.7)} ${n(w * 0.6)} ${n(fy - w * 0.34)}Z`;
        const shade = d => K.clipped(d, `<rect x="${n(-w * 1.2)}" y="${n(top - 2)}" width="${n(w * 1.15)}" height="${n(w * 3)}" fill="${K.INK}" opacity="${back ? 0.55 : 0.45}"/>`);
        return path(fist, metal(back, mm.base), 'stroke-width="1.1"') + shade(fist) + line(`M${n(-w * 0.3)} ${n(fy + w * 0.12)} h${n(w * 0.75)} M${n(-w * 0.25)} ${n(fy + w * 0.38)} h${n(w * 0.65)}`, K.INK, 0.55, 'opacity=".5"')
          + path(cuff, metal(back, mm.base), 'stroke-width="1.1"') + shade(cuff);
      };
      layers.leg = back => {
        const w = g.legW, k = g.legs * 0.46;
        const cop = `M${n(-w * 0.52)} ${n(k - w * 0.2)} Q${n(0.3)} ${n(k - w * 0.62)} ${n(w * 0.6)} ${n(k - w * 0.18)} L${n(w * 0.5)} ${n(k + w * 0.3)} Q${n(0.3)} ${n(k + w * 0.52)} ${n(-w * 0.46)} ${n(k + w * 0.3)}Z`;
        return path(`M${n(-w / 2 - 0.6)} ${n(g.legs * 0.42)} L${n(w / 2 + 0.6)} ${n(g.legs * 0.42)} L${n(w / 2 + 0.6)} ${n(g.legs - 4.5)} Q${n(w * 1.25)} ${n(g.legs - 4)} ${n(w * 1.28)} ${n(g.legs - 0.4)} L${n(-w * 0.62)} ${n(g.legs)}Z`, metal(back, mm.base))
          + path(cop, metal(back, mm.base), 'stroke-width="1"') + K.clipped(cop, `<rect x="${n(-w)}" y="${n(k + w * 0.02)}" width="${n(w * 2)}" height="${n(w)}" fill="${K.INK}" opacity=".45"/>`) + line(`M${n(-w * 0.3)} ${n(k - w * 0.3)} Q${n(0.3)} ${n(k - w * 0.48)} ${n(w * 0.4)} ${n(k - w * 0.25)}`, mm.light, 0.8, 'opacity=".7"');
      };
      // Open-faced helmets keep the face readable.
      const R = g.R, hx = g.hx, hyH = g.headY;
      const helmet = v.helmet || (v.material === "dwarven" ? "horned" : v.material === "hellsteel" || v.material === "vulcan" ? "spiked" : v.material === "lumium" || v.material === "starmetal" || unique === "godforged" ? "crested" : v.material === "blacksteel" || v.material === "voidmetal" ? "great" : "bascinet");
      let front = path(`M${n(hx + R * 0.95)} ${n(hyH - R * 0.3)} C${n(hx + R * 0.98)} ${n(hyH - R * 1.25)} ${n(hx - R * 1.0)} ${n(hyH - R * 1.35)} ${n(hx - R * 1.12)} ${n(hyH - R * 0.15)} L${n(hx - R * 1.05)} ${n(hyH + R * 0.55)} L${n(hx - R * 0.5)} ${n(hyH + R * 0.5)} L${n(hx - R * 0.35)} ${n(hyH - R * 0.15)} Q${n(hx + R * 0.3)} ${n(hyH - R * 0.62)} ${n(hx + R * 0.95)} ${n(hyH - R * 0.3)}Z`, mm.base) + line(`M${n(hx - R * 0.95)} ${n(hyH - R * 0.45)} Q${n(hx)} ${n(hyH - R * 1.15)} ${n(hx + R * 0.85)} ${n(hyH - R * 0.6)}`, mm.light, 1, 'opacity=".6"') + line(`M${n(hx + R * 0.95)} ${n(hyH - R * 0.32)} Q${n(hx + R * 0.2)} ${n(hyH - R * 0.6)} ${n(hx - R * 0.35)} ${n(hyH - R * 0.15)}`, mm.trim, 1.3);
      if (helmet === "horned") front += path(`M${n(hx - R * 0.55)} ${n(hyH - R * 0.95)} Q${n(hx - R * 1.4)} ${n(hyH - R * 1.25)} ${n(hx - R * 1.5)} ${n(hyH - R * 1.9)} Q${n(hx - R * 1.0)} ${n(hyH - R * 1.4)} ${n(hx - R * 0.25)} ${n(hyH - R * 1.15)}Z`, "#efe6d0");
      if (helmet === "spiked") front += path(`M${n(hx - R * 0.35)} ${n(hyH - R * 1.1)} L${n(hx - R * 0.15)} ${n(hyH - R * 1.75)} L${n(hx + R * 0.1)} ${n(hyH - R * 1.12)}Z M${n(hx - R * 0.85)} ${n(hyH - R * 0.85)} L${n(hx - R * 1.15)} ${n(hyH - R * 1.4)} L${n(hx - R * 0.6)} ${n(hyH - R * 1.0)}Z`, mm.dark);
      if (helmet === "crested") front += path(`M${n(hx + R * 0.3)} ${n(hyH - R * 1.1)} Q${n(hx - R * 0.6)} ${n(hyH - R * 1.85)} ${n(hx - R * 1.5)} ${n(hyH - R * 1.1)} Q${n(hx - R * 0.7)} ${n(hyH - R * 1.3)} ${n(hx - R * 0.1)} ${n(hyH - R * 0.98)}Z`, unique === "godforged" ? "#f2cf73" : v.material === "lumium" ? "#f0d488" : "#9fb6d4");
      if (helmet === "great") front += line(`M${n(hx + R * 0.95)} ${n(hyH - R * 0.1)} L${n(hx + R * 0.9)} ${n(hyH + R * 0.6)}`, mm.base, R * 0.22) + line(`M${n(hx - R * 0.05)} ${n(hyH - R * 1.2)} L${n(hx - R * 0.05)} ${n(hyH - R * 0.62)}`, mm.light, 1);
      if (helmet === "bascinet" && v.material === "iron") front += line(`M${n(hx - R * 1.15)} ${n(hyH - R * 0.15)} Q${n(hx)} ${n(hyH - R * 0.38)} ${n(hx + R * 1.15)} ${n(hyH - R * 0.32)}`, mm.dark, 2.4);
      layers.head = { back: "", front };
      layers.hidesHair = true;
      if (unique === "seventh-cycle" || unique === "godforged") layers.cape = capeShape(`M${n(-S + 1)} ${n(sy + 2)} Q${n(-S - 9)} ${n(hy * 0.4)} ${n(-S - 6)} ${n(-1)} L${n(H * 0.2)} ${n(-3)} L${n(S * 0.2)} ${n(sy + 3)}Z`, unique === "godforged" ? "#c9a24a" : "#5a1a14", S, sy);
      return layers;
    }

    if (v.type === "light" || ["forest-plate", "stormhide"].includes(unique)) {
      // Forged armor brings its own material palette; Rootbark is always living bark.
      const leather = unique === "rootbark" ? { base: "#6b4527", light: "#9a6b42", dark: "#3e2615", trim: "#7fbf5a" } : v.forged ? m : unique === "forest-plate" ? { base: "#6b4527", light: "#9a6b42", dark: "#3e2615", trim: "#7fbf5a" }
        : unique === "stormhide" ? { base: "#2f3448", light: "#5a6380", dark: "#191c28", trim: "#ffe66b", glow: "#ffe66b" }
        : kind === "padded" ? { base: "#e2d6bb", light: "#f6efdc", dark: "#a99c7c", trim: "#8a7f72" }
        : kind === "fur" ? { base: "#7b5634", light: "#a8845c", dark: "#4b3220", trim: "#cdb48c" }
        : kind === "hide" ? (v.material === "hellsteel" ? { base: "#5a1e1e", light: "#8a3a2e", dark: "#2f0f0f", trim: "#ff6a3d" } : { base: "#8f6a46", light: "#b8936a", dark: "#5a3f27", trim: "#5a3f27" })
        : kind === "scale" ? (/dragon/i.test(v.name) ? { base: "#a63b2a", light: "#e0694a", dark: "#5f1d13", trim: "#f2cf73" } : K.material("scale"))
        : kind === "leafmail" ? { base: "#7f9b4a", light: "#b5cf73", dark: "#4b6128", trim: "#e6c96b" }
        : ["chainshirt", "brigandine"].includes(kind) && !["leather", "iron"].includes(v.material) ? m
        : /hardened/i.test(v.name) ? { base: "#6b4228", light: "#93613d", dark: "#3e2615", trim: "#b48a52" }
        : kind === "chainshirt" ? K.material("steel") : K.material("leather");
      const parts = [torsoBase(leather.base)];
      if (kind === "chainshirt") parts.push(chainTexture(-S + 1.5, sy + 2, S * 2 - 3, T - 3, leather.dark));
      else if (kind === "scale" || unique === "stormhide") parts.push(scaleTexture(-S + 1.5, sy + 3, S * 2 - 3, T - 5, leather.dark));
      else if (kind === "brigandine") for (let row = 0; row < 3; row++) for (let i = 0; i < 3; i++) parts.push(dot(-S * 0.55 + i * S * 0.55, sy + T * (0.25 + row * 0.22), 0.9, leather.trim === "#3e2615" ? "#c9a24a" : leather.light));
      else if (kind === "leafmail" || unique === "forest-plate") for (let i = 0; i < 6; i++) parts.push(at(-S * 0.6 + (i % 3) * S * 0.6, sy + T * (0.25 + Math.floor(i / 3) * 0.35), path("M0 0 Q3 -3 6 0 Q3 3 0 0Z", unique === "forest-plate" ? "#7fbf5a" : leather.light, 'stroke-width=".7"'), -30 + i * 20));
      else if (kind === "padded") for (let i = 0; i < 3; i++) parts.push(line(`M${n(-S * 0.8)} ${n(sy + T * (0.28 + i * 0.22))} h${n(S * 1.6)}`, leather.dark, 0.8, 'stroke-dasharray="1.6 1.4"'));
      else parts.push(line(`M${n(S * 0.2)} ${n(sy + 2)} L${n(S * 0.1)} ${n(hy - 2)} M${n(-S * 0.5)} ${n(sy + T * 0.3)} L${n(S * 0.6)} ${n(sy + T * 0.55)}`, leather.dark, 1));
      const mantle = kind === "fur" ? path(`M${n(-S - 1)} ${n(sy + 4)} Q${n(-S)} ${n(sy - 3)} 0 ${n(sy - 2)} Q${n(S)} ${n(sy - 3)} ${n(S + 1)} ${n(sy + 4)} Q0 ${n(sy + 6)} ${n(-S - 1)} ${n(sy + 4)}Z`, "#e8dcc4") : "";
      if (unique === "stormhide") parts.push(`<g class="sp-glow mw-fx" style="color:#ffe66b">${line(`M${n(-S * 0.5)} ${n(sy + 4)} l3 5 l-2 2 l3 6 M${n(S * 0.4)} ${n(sy + 6)} l-2 5 l2 2 l-2 5`, "#ffe66b", 1)}</g>`);
      if (unique === "forest-plate") parts.push(`<g class="mw-fx">${line(`M${n(-S * 0.7)} ${n(sy + T * 0.8)} q${n(S * 0.6)} -3 ${n(S * 1.4)} 0`, "#9fd06a", 1.2)}</g>`);
      // Rootbark: moss along the hem and a leaf of each season across the chest.
      if (unique === "rootbark") parts.push(`<g class="mw-fx">${line(`M${n(-S * 0.75)} ${n(sy + T * 0.82)} q${n(S * 0.6)} -3 ${n(S * 1.5)} 0`, "#7fb05a", 1.3)}${["#f6b8d0", "#ffd36b", "#e0782a", "#cfe8ff"].map((color, i) => at(-S * 0.55 + i * S * 0.36, sy + T * 0.3 + (i % 2) * 2, path("M0 0 Q2.4 -2.4 4.8 0 Q2.4 2.4 0 0Z", color, 'stroke-width=".6"'), -30 + i * 20)).join("")}</g>`);
      parts.push(path(`M${n(-H - 0.5)} ${n(hy - 2)} h${n(H * 2 + 1)} v4 h${n(-H * 2 - 1)}Z`, darken(leather.base, 0.3)), path(`M${n(H * 0.15)} ${n(hy - 2.3)} h4 v4.6 h-4Z`, "#c9a24a", 'stroke-width=".9"'));
      if (/ranger|scout/i.test(v.name)) parts.push(line(`M${n(-S * 0.8)} ${n(sy + 2)} L${n(S * 0.6)} ${n(hy - 3)}`, "#4a3326", 2));
      layers.torso = parts[0] + K.clipped(torsoOutline(g, female), parts.slice(1).join("")) + mantle;
      layers.sleeve = back => {
        const fill = back ? darken(leather.base, 0.12) : leather.base;
        let out = path(`M${n(-g.armW / 2 - 0.6)} -2 L${n(g.armW / 2 + 0.6)} -2 L${n(g.armW / 2 + 0.4)} ${n(g.arm * 0.45)} L${n(-g.armW / 2 - 0.4)} ${n(g.arm * 0.45)}Z`, fill);
        if (kind === "chainshirt") out += chainTexture(-g.armW / 2, 0, g.armW, g.arm * 0.4, leather.dark);
        out += path(`M${n(-g.armW / 2 - 0.5)} ${n(g.arm * 0.6)} L${n(g.armW / 2 + 0.5)} ${n(g.arm * 0.6)} L${n(g.armW / 2 + 0.5)} ${n(g.arm * 0.88)} L${n(-g.armW / 2 - 0.5)} ${n(g.arm * 0.88)}Z`, back ? darken(leather.dark, 0.1) : leather.dark, 'stroke-width="1"');
        return out;
      };
      layers.leg = back => path(`M${n(-g.legW / 2 - 0.4)} ${n(g.legs * 0.5)} L${n(g.legW / 2 + 0.4)} ${n(g.legs * 0.5)} L${n(g.legW / 2 + 0.5)} ${n(g.legs - 4.5)} Q${n(g.legW * 1.22)} ${n(g.legs - 4)} ${n(g.legW * 1.25)} ${n(g.legs - 0.4)} L${n(-g.legW * 0.62)} ${n(g.legs)}Z`, back ? darken(leather.dark, 0.12) : leather.dark) + path(`M${n(-g.legW / 2 - 0.9)} ${n(g.legs * 0.5 - 1)} L${n(g.legW / 2 + 0.9)} ${n(g.legs * 0.5 - 1)} L${n(g.legW / 2 + 0.7)} ${n(g.legs * 0.5 + 2.8)} L${n(-g.legW / 2 - 0.7)} ${n(g.legs * 0.5 + 2.8)}Z`, back ? darken(leather.base, 0.12) : leather.base, 'stroke-width=".9"');
      if (kind === "chainshirt" || kind === "scale" || kind === "brigandine") layers.skirt = `<g class="sp-skirt">${path(`M${n(-H - 0.5)} ${n(hy + 2)} L${n(H + 0.5)} ${n(hy + 2)} L${n(H + 1.2)} ${n(hy + g.legs * 0.3)} L${n(-H - 1.2)} ${n(hy + g.legs * 0.3)}Z`, leather.base)}</g>`;
      if (unique === "forest-plate") layers.head = { back: "", front: [0, 1, 2].map(i => at(g.hx - g.R * (0.7 - i * 0.45), g.headY - g.R * (0.85 + (i === 1 ? 0.15 : 0)), path("M0 0 Q3 -4 0 -9 Q-3 -4 0 0Z", "#7fbf5a", 'stroke-width=".8"'), -20 + i * 20)).join("") };
      if (/ranger/i.test(v.name)) layers.head = null;
      return layers;
    }

    // Clothing: robes, cloaks, coats and garments.
    const cloth = unique === "soulbound" ? "#c9dbe8" : v.cloth || "#7b5a3a";
    const trim = unique === "soulbound" ? "#ffffff" : v.trimColor ? v.trimColor : v.ornate >= 3 ? "#f2cf73" : v.ornate >= 1 ? lighten(cloth, 0.45) : darken(cloth, 0.3);
    const dark = darken(cloth, 0.28);
    const torso = [torsoBase(cloth, unique === "soulbound" ? 'fill-opacity=".85"' : "")];
    torso.push(flat(`M${n(-S)} ${n(sy + 4)} L${n(-S * 0.55)} ${n(sy + 1)} L${n(-H * 0.8)} ${n(hy + 4)} L${n(-H)} ${n(hy + 4)}Z`, dark, 'opacity=".5"'));
    const robeLike = ["robe", "battlerobe"].includes(kind) || unique === "soulbound";
    if (robeLike) torso.push(line(`M${n(S * 0.55)} ${n(sy + 0.5)} L${n(-H * 0.2)} ${n(hy + 2)}`, trim, 1.6));
    if (kind === "noble") torso.push(line(`M${n(S * 0.1)} ${n(sy + 2)} V${n(hy - 3)}`, trim, 1.2), dot(S * 0.1, sy + T * 0.3, 0.9, "#f2cf73"), dot(S * 0.1, sy + T * 0.5, 0.9, "#f2cf73"), dot(S * 0.1, sy + T * 0.7, 0.9, "#f2cf73"));
    if (kind === "worker") torso.push(path(`M${n(-S * 0.5)} ${n(sy + T * 0.3)} L${n(S * 0.6)} ${n(sy + T * 0.3)} L${n(H)} ${n(hy + 6)} L${n(-H * 0.6)} ${n(hy + 6)}Z`, "#8a7a5f"), line(`M${n(-S * 0.4)} ${n(sy + T * 0.3)} L${n(-S * 0.3)} ${n(sy)} M${n(S * 0.5)} ${n(sy + T * 0.3)} L${n(S * 0.4)} ${n(sy)}`, "#5a4a35", 1.2));
    if (kind === "rags") torso.push(path(`M${n(-S * 0.3)} ${n(sy + T * 0.4)} l3 2 l-1 3 l-3 -1Z`, lighten(cloth, 0.15), 'stroke-width=".8"'), line(`M${n(S * 0.4)} ${n(sy + T * 0.25)} l2 3 M${n(-S * 0.6)} ${n(sy + T * 0.65)} l3 1`, dark, 0.8));
    if (["garb", "battlerobe"].includes(kind)) torso.push(path(`M${n(-H - 0.5)} ${n(hy - 3.5)} h${n(H * 2 + 1)} v4.5 h${n(-H * 2 - 1)}Z`, trim === "#f2cf73" ? "#7a5ca8" : trim));
    else if (!robeLike && kind !== "rags") torso.push(path(`M${n(-H - 0.5)} ${n(hy - 2)} h${n(H * 2 + 1)} v4 h${n(-H * 2 - 1)}Z`, "#5a3d26"));
    else if (robeLike) torso.push(line(`M${n(-H)} ${n(hy - 1)} Q0 ${n(hy + 1.5)} ${n(H)} ${n(hy - 1)}`, trim, 1.8));
    // Motifs for named robes.
    const lower = v.name.toLowerCase();
    if (/starlight/.test(lower)) for (const [x, y] of [[-S * 0.5, sy + T * 0.35], [S * 0.3, sy + T * 0.6], [-S * 0.1, sy + T * 0.8]]) torso.push(path(star(x, y, 2.2, 0.9, 4), "#f2cf73", 'stroke-width=".3"'));
    if (/phoenix/.test(lower)) torso.push(path(`M${n(-S * 0.3)} ${n(sy + T * 0.85)} Q${n(-S * 0.5)} ${n(sy + T * 0.5)} ${n(0)} ${n(sy + T * 0.35)} Q${n(-S * 0.1)} ${n(sy + T * 0.6)} ${n(S * 0.4)} ${n(sy + T * 0.6)} Q${n(S * 0.2)} ${n(sy + T * 0.8)} ${n(-S * 0.3)} ${n(sy + T * 0.85)}Z`, "#ffb04a", 'stroke-width=".8"'));
    if (/voidweave/.test(lower)) torso.push(dot(-S * 0.4, sy + T * 0.4, 0.9, "#b08cff"), dot(S * 0.3, sy + T * 0.55, 0.7, "#b08cff"), dot(-S * 0.1, sy + T * 0.75, 0.8, "#b08cff"));
    if (/moonweave/.test(lower)) torso.push(path(`M${n(-S * 0.2)} ${n(sy + T * 0.35)} a3 3 0 1 0 3 4 a2.2 2.2 0 1 1 -3 -4Z`, "#f4f2e8", 'stroke-width=".7"'));
    if (/fateweave/.test(lower)) torso.push(line(`M${n(-S * 0.8)} ${n(sy + T * 0.3)} Q0 ${n(sy + T * 0.6)} ${n(S * 0.8)} ${n(sy + T * 0.4)}`, "#f2cf73", 0.9), line(`M${n(-S * 0.7)} ${n(sy + T * 0.6)} Q0 ${n(sy + T * 0.3)} ${n(S * 0.7)} ${n(sy + T * 0.7)}`, "#c9e0ff", 0.9));
    if (/vitalweave/.test(lower)) torso.push(path(`M${n(-S * 0.1 - 2)} ${n(sy + T * 0.45)} h4 v-2 h2 v2 h4 v2 h-4 v2 h-2 v-2 h-4Z`, "#cfeedd", 'stroke-width=".6"'));
    if (unique === "soulbound") torso.push(`<g class="sp-glow mw-fx" style="color:#d8f4ff">${circle(S * 0.1, sy + T * 0.4, 2.6, "#ffffff", 'stroke="none" opacity=".85"')}</g>`);
    const clothTorso = torso[0] + K.clipped(torsoOutline(g, female), torso.slice(1).join(""));
    layers.torso = unique === "soulbound" ? `<g class="sp-glow" style="color:#d8f4ff">${clothTorso}</g>` : clothTorso;
    const wide = robeLike || kind === "garb";
    layers.sleeve = back => {
      const fill = back ? darken(cloth, 0.12) : cloth;
      return wide
        ? path(`M${n(-g.armW / 2 - 0.8)} -2 L${n(g.armW / 2 + 0.8)} -2 L${n(g.armW / 2 + 3.2)} ${n(g.arm * 0.82)} L${n(-g.armW / 2 - 1.6)} ${n(g.arm * 0.82)}Z`, fill) + line(`M${n(-g.armW / 2 - 1.4)} ${n(g.arm * 0.8)} L${n(g.armW / 2 + 3)} ${n(g.arm * 0.8)}`, trim, 1.2)
        : path(`M${n(-g.armW / 2 - 0.5)} -2 L${n(g.armW / 2 + 0.5)} -2 L${n(g.armW / 2 + 0.8)} ${n(g.arm * 0.7)} L${n(-g.armW / 2 - 0.8)} ${n(g.arm * 0.7)}Z`, fill) + (kind === "rags" ? line(`M${n(-g.armW / 2)} ${n(g.arm * 0.7)} l1.5 1.5 l1.5 -1.5 l1.5 1.5`, dark, 0.8) : "");
    };
    if (robeLike || kind === "coat") {
      const long = robeLike;
      const bottom = long ? -2 : hy + g.legs * 0.72;
      const flare = long ? H * 1.55 : H * 1.3;
      const d = kind === "coat"
        ? `M${n(-H)} ${n(hy + 2)} L${n(H)} ${n(hy + 2)} L${n(flare)} ${n(bottom)} L${n(2)} ${n(bottom - 2)} L${n(-1)} ${n(hy + 8)} L${n(-3)} ${n(bottom - 2)} L${n(-flare)} ${n(bottom)}Z`
        : `M${n(-H)} ${n(hy + 2)} L${n(H)} ${n(hy + 2)} L${n(flare)} ${n(bottom)} Q0 ${n(bottom + 3)} ${n(-flare)} ${n(bottom)}Z`;
      layers.skirt = `<g class="sp-skirt">${path(d, cloth, unique === "soulbound" ? 'fill-opacity=".75"' : "")}${line(`M${n(-flare)} ${n(bottom - 1)} Q0 ${n(bottom + 2)} ${n(flare)} ${n(bottom - 1)}`, trim, 1.4)}${line(`M${n(-H * 0.3)} ${n(hy + 4)} L${n(-flare * 0.45)} ${n(bottom)}`, dark, 1)}</g>`;
    } else if (kind === "rags") layers.skirt = `<g class="sp-skirt">${path(`M${n(-H)} ${n(hy + 2)} L${n(H)} ${n(hy + 2)} L${n(H + 1)} ${n(hy + g.legs * 0.35)} L${n(H * 0.4)} ${n(hy + g.legs * 0.28)} L${n(0)} ${n(hy + g.legs * 0.4)} L${n(-H * 0.5)} ${n(hy + g.legs * 0.3)} L${n(-H - 1)} ${n(hy + g.legs * 0.38)}Z`, cloth)}</g>`;
    // Headwear for named garments.
    const R = g.R, hx = g.hx, hyH = g.headY;
    if (/magician|enchanter/.test(lower)) layers.head = window.SpriteKit.headwearFor?.({ style: "wizard", color: cloth, band: trim, star: /enchanter/.test(lower) ? "#f2cf73" : null }, hx, hyH, R) || null;
    else if (/starlight/.test(lower)) layers.head = window.SpriteKit.headwearFor?.({ style: "wizard", color: cloth, band: "#f2cf73", star: "#f2cf73" }, hx, hyH, R) || null;
    else if (/cloak|mantle|voidweave/.test(lower)) layers.head = window.SpriteKit.headwearFor?.({ style: "hood", color: kind === "mantle" ? "#4f6b3f" : cloth }, hx, hyH, R) || null;
    else if (/moonweave|fateweave|manasilk|lumium/.test(lower)) layers.head = window.SpriteKit.headwearFor?.({ style: "circlet", color: "#e0b54a", gem: /moon/.test(lower) ? "#e8f0ff" : /fate/.test(lower) ? "#c9a0ff" : "#7fb4ff" }, hx, hyH, R) || null;
    if (layers.head && /cloak|mantle|voidweave/.test(lower)) layers.hidesHair = true;
    if (kind === "cloak" || kind === "mantle" || kind === "noble") {
      const long = kind === "cloak";
      layers.cape = capeShape(`M${n(-S + 1)} ${n(sy + 1)} Q${n(-S - (long ? 10 : 6))} ${n(hy * (long ? 0.35 : 0.75))} ${n(-S - (long ? 7 : 3))} ${n(long ? -1 : hy + 4)} L${n(H * 0.2)} ${n(long ? -3 : hy + 2)} L${n(S * 0.2)} ${n(sy + 2)}Z`, kind === "mantle" ? "#4f6b3f" : kind === "noble" ? "#8a2f3a" : cloth, S, sy);
    }
    return layers;
  }

  // The same torso silhouette the humanoid uses, shared with armor.
  function torsoOutline(g, female) {
    // Armor follows the rig's own silhouette, slightly proud of the body.
    if (K.torsoPath) return K.torsoPath({ ...g, S: g.S + 0.5, H: g.H + 0.5 }, female);
    const { sy, hy, S, H, T } = g;
    const s = female ? S * 0.88 : S, h = female ? H * 1.08 : H, w = female ? S * 0.68 : S * 0.88;
    const midY = sy + T * 0.58;
    if (female) return `M${n(-s - 0.4)} ${n(sy + 4)} Q${n(-s - 0.4)} ${n(sy - 0.4)} ${n(-s + 4)} ${n(sy - 0.4)} L${n(s - 4)} ${n(sy - 0.4)} Q${n(s + 0.4)} ${n(sy - 0.4)} ${n(s + 0.9)} ${n(sy + 4)} Q${n(s + 2.6)} ${n(sy + T * 0.3)} ${n(w + 1)} ${n(midY)} Q${n(w + 1.4)} ${n(midY + T * 0.18)} ${n(h + 0.4)} ${n(hy + 2)} Q${n(h + 0.4)} ${n(hy + 5.4)} ${n(h - 3)} ${n(hy + 5.4)} L${n(-h + 3)} ${n(hy + 5.4)} Q${n(-h - 0.4)} ${n(hy + 5.4)} ${n(-h - 0.4)} ${n(hy + 2)} Q${n(-w - 1.4)} ${n(midY + T * 0.18)} ${n(-w - 0.4)} ${n(midY)} Z`;
    return `M${n(-s - 0.4)} ${n(sy + 4)} Q${n(-s - 0.4)} ${n(sy - 0.4)} ${n(-s + 4)} ${n(sy - 0.4)} L${n(s - 4)} ${n(sy - 0.4)} Q${n(s + 0.4)} ${n(sy - 0.4)} ${n(s + 0.4)} ${n(sy + 4)} L${n(w + 0.4)} ${n(midY)} L${n(h + 0.4)} ${n(hy + 2)} Q${n(h + 0.4)} ${n(hy + 5.4)} ${n(h - 3)} ${n(hy + 5.4)} L${n(-h + 3)} ${n(hy + 5.4)} Q${n(-h - 0.4)} ${n(hy + 5.4)} ${n(-h - 0.4)} ${n(hy + 2)} L${n(-w - 0.4)} ${n(midY)} Z`;
  }

  K.equipment = { weapon, shield, claws, gripAngle, armorLayers, torsoOutline, MASTERWORK_WEAPONS, MASTERWORK_SHIELDS };
})();
