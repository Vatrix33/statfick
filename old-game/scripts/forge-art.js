/* Art for forged weapons and armor: every part drawn in its manufacturer's style and
   its material's colours, for battle sprites, shop cards and item portraits, plus
   the manufacturer marks. Weapons use the sprite frame: the grip at the
   origin, pointing up (negative y), working edge toward positive x.
     Roundtable: classic knightly lines.  Dwarven: thick, blocky, riveted.
     Elven: curved and leaf-like.          Boreal Bolt: bare and minimal.
     Abyss: jagged, serrated, spiked.      Hellmade: ornate and flawless.
     Hearthforge: refined, folded steel.   Outlaw: crude and lashed together. */
(() => {
  "use strict";
  const K = window.SpriteKit;
  if (!K) return;
  const { n, path, line, ellipse, circle, dot, at, mix, darken, lighten, star } = K;

  /* ---------- Materials and makers ---------- */
  const M = (kind, base, light, dark, extra = {}) => ({ kind, base, light, dark, ...extra });
  const PALETTES = {
    "scrap-metal": M("metal", "#7d6b58", "#a88f70", "#4a3b2e", { pattern: "rust" }),
    driftwood: M("wood", "#a99d88", "#d2c7b1", "#6e6453", { pattern: "grain" }),
    linen: M("thread", "#e4d9bf", "#f6efdf", "#a89c80"),
    iron: M("metal", "#8e949b", "#cdd2d6", "#585d63"),
    steel: M("metal", "#b2bbc4", "#eef2f5", "#707a84"),
    oakwood: M("wood", "#8a6038", "#b4824e", "#563a20", { pattern: "grain" }),
    quenchwood: M("wood", "#4c3527", "#74533b", "#2a1c14", { pattern: "char" }),
    wool: M("thread", "#b8ac98", "#d8cfbe", "#7e7362"),
    "elvenleaf-string": M("thread", "#7fae5a", "#b8dc8a", "#4e7434"),
    "damascus-steel": M("metal", "#959ba3", "#dde1e4", "#5b6168", { pattern: "waves" }),
    "dwarven-steel": M("metal", "#a49d8c", "#e2dbc8", "#6b6456", { pattern: "runes" }),
    boreum: M("metal", "#9fb4c4", "#e0ecf4", "#5f7484", { pattern: "frost" }),
    "elven-birch": M("wood", "#e6e0d0", "#fbf8f0", "#9a9282", { pattern: "birch" }),
    "reinforced-wood": M("wood", "#8a6038", "#b4824e", "#563a20", { pattern: "bands" }),
    "steel-thread": M("thread", "#c8ccd0", "#eef0f2", "#8a9096"),
    magithread: M("thread", "#6ab4ff", "#c8e4ff", "#2f6aa8", { glow: "#8fd0ff" }),
    blacksteel: M("metal", "#40454c", "#88919b", "#202328"),
    lumium: M("metal", "#e3ebf5", "#ffffff", "#9aabc2", { glow: "#fff4c8" }),
    "gold-manyullin-alloy": M("metal", "#d4a03e", "#ffe08a", "#8a561e", { pattern: "rose" }),
    moonstone: M("stone", "#c8d6ea", "#ffffff", "#8a9ab8", { glow: "#dfe8ff" }),
    "boreal-pine": M("wood", "#4f5838", "#77825a", "#2c321e", { pattern: "grain" }),
    "manic-oak": M("wood", "#7e3434", "#b05454", "#441a20", { pattern: "veins", vein: "#c08cf0" }),
    "golden-thread": M("thread", "#e8c050", "#fff0b0", "#9a7a20"),
    dragonvein: M("thread", "#b8302a", "#e86a52", "#6a1410", { glow: "#ff6a4a" }),
    "vulcan-alloy": M("metal", "#c45b30", "#f6a066", "#7a2d16", { glow: "#ff8a3d" }),
    hellstone: M("stone", "#2c1c1c", "#5a2a22", "#120808", { pattern: "cracks", vein: "#ff6a2a", glow: "#ff5a2a" }),
    "abyssal-iron": M("metal", "#3a2a4a", "#6a5488", "#1a1226", { pattern: "veins", vein: "#a882ff", glow: "#9a6aff" }),
    "elven-greatwood": M("wood", "#8a9a4a", "#c0cc7a", "#4e5a26", { pattern: "leaves" }),
    foundationbark: M("stone", "#6e665a", "#9a9284", "#423c34", { pattern: "bark" }),
    voidthread: M("thread", "#7a4ad8", "#b89aff", "#3e2280", { glow: "#a07aff" }),
    "woven-mana": M("thread", "#5aa8ff", "#bfe0ff", "#2a62b0", { glow: "#7fc0ff" }),
    "star-metal": M("metal", "#d2def3", "#ffffff", "#8494b6", { glow: "#d6e6ff", pattern: "stars" }),
    nullstone: M("stone", "#3e4044", "#56585c", "#222326", { matte: true }),
    "ancient-boreum": M("metal", "#b8d8ec", "#ffffff", "#6a90aa", { glow: "#c8ecff", pattern: "frost" }),
    "lifetree-branch": M("wood", "#7a5a34", "#a88a54", "#4a3418", { pattern: "leaves", glow: "#cfffa0" }),
    "woven-starlight": M("thread", "#fff2b8", "#ffffff", "#c8b46a", { glow: "#fff4c8" }),
    "gold-vulcan-thread": M("thread", "#f0a040", "#ffd890", "#a0581a", { glow: "#ffb050" }),
    mythilium: M("metal", "#9ad8d0", "#effffb", "#4a8a84", { glow: "#bffff0" }),
  };
  const STYLE = {
    roundtable: { trim: "#c9a24a", gem: "#3f6fbf", wrap: "#5a3a26" },
    dwarven: { trim: "#b07a3a", gem: "#c0402a", wrap: "#4a3424" },
    elven: { trim: "#d8c46a", gem: "#6ac07a", wrap: "#5a6b34" },
    "boreal-bolt": { trim: "#b4bcc4", gem: null, wrap: "#34363c" },
    abyss: { trim: "#4a2a62", gem: "#a06aff", wrap: "#24182e" },
    hellmade: { trim: "#e8b84a", gem: "#c0283a", wrap: "#2a1414" },
    hearthforge: { trim: "#c27a47", gem: "#ff8a3a", wrap: "#3a2418" },
    outlaw: { trim: "#a08a5a", gem: null, wrap: "#8a7a5a" },
    "institute-of-magic": { trim: "#9aa8bc", gem: null, wrap: "#3a4458" },
    "church-of-talos": { trim: "#e8cf6a", gem: "#7aa8ff", wrap: "#2e3446" },
    vampiric: { trim: "#d8ae4a", gem: "#a0101a", wrap: "#2a0a12" },
  };
  // Jewels and gems glow in their own colours; inks write in theirs.
  const S = (base, light, dark, glow) => ({ base, light, dark, glow });
  const STONE_PALETTES = {
    diamond: S("#e6eef8", "#ffffff", "#9aaec4", "#eef6ff"), ruby: S("#c4283a", "#ff7a86", "#6a0e18", "#ff5a6a"),
    aquamarine: S("#5ac8d8", "#c4f4fa", "#2a7a88", "#8ff0ff"), zircon: S("#c4884a", "#f2ca92", "#6a4420", "#ffc070"),
    emerald: S("#24a85e", "#8af0b0", "#0e5a30", "#6aff9a"), sapphire: S("#2a5ad0", "#8ab0ff", "#102a6a", "#7aa8ff"),
    "lapis-lazuli": S("#2a3a9a", "#6a7ad8", "#141c50", "#9aaaff"), topaz: S("#e8b030", "#ffe08a", "#8a5a10", "#ffd060"),
    obsidian: S("#2c2038", "#6a5a80", "#0e0a14", "#a07aff"), jade: S("#4aa86a", "#a8e0b8", "#1e5a34", "#8adf6a"),
    cinnabar: S("#d0402a", "#ff8a6a", "#6a1a10", "#ff6a4a"), hematite: S("#56565e", "#a6a6b0", "#26262c", "#c8c8d8"),
  };
  const INK_COLORS = {
    "white-ink": "#f0ece0", "red-ink": "#ff5a42", "blue-ink": "#4a8aff", "brown-ink": "#c08048", "green-ink": "#6ad86a", "light-blue-ink": "#8ad4ff",
    "cyan-ink": "#7af0f8", "golden-ink": "#ffd050", "black-ink": "#9a7ad8", "light-green-ink": "#aaf07a", "grey-ink": "#b4b4c0", "dark-red-ink": "#e0303a",
  };
  const pal = id => PALETTES[id] || PALETTES.iron;
  const glowWrap = (p, content) => p.glow ? `<g class="sp-glow" style="color:${p.glow}">${content}</g>` : content;
  const gem = (x, y, r, color) => color ? ellipse(x, y, r, r * 1.15, "!" + color, 'stroke-width=".7"') + dot(x - r * 0.3, y - r * 0.35, r * 0.32, "#ffffff", 'opacity=".75"') : "";
  const thin = 'stroke-width=".9"';

  // Surface markings for a material along a strip from (x, y1) to (x, y2).
  function markings(p, x, y1, y2, width) {
    const length = y1 - y2, out = [];
    const at01 = t => y1 - length * t;
    if (p.pattern === "waves") for (let i = 0; i < 4; i++) out.push(line(`M${n(x - width * 0.3)} ${n(at01(0.2 + i * 0.18))} q${n(width * 0.3)} -1.6 ${n(width * 0.6)} 0`, p.dark, 0.7, 'opacity=".7"'));
    else if (p.pattern === "runes") for (let i = 0; i < 3; i++) out.push(line(`M${n(x - 0.8)} ${n(at01(0.3 + i * 0.2))} l0.8 -1.2 l0.8 1.2`, "#c99f42", 0.7));
    else if (p.pattern === "frost") for (let i = 0; i < 3; i++) out.push(line(`M${n(x)} ${n(at01(0.25 + i * 0.22) - 1.2)} v2.4 M${n(x - 1)} ${n(at01(0.25 + i * 0.22))} h2`, p.light, 0.6, 'opacity=".85"'));
    else if (p.pattern === "stars") for (let i = 0; i < 3; i++) out.push(dot(x + (i % 2 ? width * 0.15 : -width * 0.15), at01(0.25 + i * 0.22), 0.55, "!#ffffff"));
    else if (p.pattern === "cracks" || p.pattern === "veins") out.push(line(`M${n(x - width * 0.2)} ${n(at01(0.1))} l${n(width * 0.3)} ${n(-length * 0.15)} l${n(-width * 0.25)} ${n(-length * 0.15)} l${n(width * 0.3)} ${n(-length * 0.2)}`, "!" + (p.vein || p.light), 0.6, 'opacity=".9"'));
    else if (p.pattern === "rust") for (let i = 0; i < 3; i++) out.push(dot(x + (i - 1) * width * 0.2, at01(0.2 + i * 0.25), 0.7, "#8a4a2a", 'opacity=".8"'));
    else if (p.pattern === "rose") out.push(line(`M${n(x)} ${n(at01(0.1))} V${n(at01(0.9))}`, "#c0602a", 0.6, 'opacity=".55"'));
    return out.join("");
  }
  // Wood grain, birch bark and leaves along a shaft.
  function shaftMarkings(p, x, y1, y2) {
    const out = [], length = y1 - y2;
    if (p.kind !== "wood" && p.pattern !== "bark") return "";
    if (p.pattern === "birch") for (let i = 0; i < 4; i++) out.push(line(`M${n(x - 1)} ${n(y1 - length * (0.15 + i * 0.22))} h1.4`, "#2a2620", 0.8));
    else if (p.pattern === "bark") for (let i = 0; i < 4; i++) out.push(line(`M${n(x - 1.2)} ${n(y1 - length * (0.12 + i * 0.24))} l2.4 1.2`, p.dark, 0.7));
    else out.push(line(`M${n(x + 0.4)} ${n(y1 - 1)} Q${n(x + 0.9)} ${n((y1 + y2) / 2)} ${n(x + 0.3)} ${n(y2 + 1)}`, p.light, 0.6, 'opacity=".7"'));
    if (p.pattern === "leaves") out.push(at(x + 1.6, y1 - length * 0.55, path("M0 0 Q2.6 -2.2 5 0 Q2.6 2.2 0 0Z", "#7fbf5a", 'stroke-width=".6"'), -35));
    if (p.pattern === "bands") out.push(line(`M${n(x - 1.8)} ${n(y1 - length * 0.3)} h3.6 M${n(x - 1.8)} ${n(y1 - length * 0.7)} h3.6`, K.material("iron").base, 1.1));
    if (p.pattern === "veins") out.push(line(`M${n(x - 0.6)} ${n(y1 - length * 0.2)} l0.9 ${n(-length * 0.2)} l-0.7 ${n(-length * 0.2)}`, "!" + p.vein, 0.55));
    return out.join("");
  }

  /* ---------- Blades ---------- */
  // A blade from just above the guard (y = -3) to its tip, shaped by its maker.
  function bladePath(maker, L, w, rnd) {
    const tip = -3 - L, h = w / 2;
    switch (maker) {
      case "dwarven": { const W = h * 1.3; return { d: `M${n(-W)} -3 L${n(-W)} ${n(tip + 2)} L${n(W)} ${n(tip - 1)} L${n(W)} -3Z`, edge: `M${n(W - 0.7)} -4 L${n(W - 0.7)} ${n(tip)}`, width: W * 2 }; }
      case "elven": return { d: `M${n(-h * 0.7)} -3 C${n(-h * 0.7)} ${n(-3 - L * 0.4)} ${n(-h * 1.35)} ${n(tip + L * 0.32)} ${n(-h * 1.05)} ${n(tip + L * 0.16)} Q${n(-h * 0.45)} ${n(tip + 2)} ${n(h * 0.4)} ${n(tip)} Q${n(h * 0.3)} ${n(tip + 3)} ${n(h * 1.05)} ${n(tip + L * 0.2)} C${n(h * 1.3)} ${n(tip + L * 0.38)} ${n(h * 0.7)} ${n(-3 - L * 0.4)} ${n(h * 0.7)} -3Z`,
        edge: `M${n(h * 0.75)} -5 C${n(h * 0.85)} ${n(-3 - L * 0.4)} ${n(h * 1.15)} ${n(tip + L * 0.35)} ${n(h * 0.9)} ${n(tip + L * 0.2)}`, width: w * 1.2 };
      case "boreal-bolt": { const W = h * 0.82; return { d: `M${n(-W)} -3 L${n(-W)} ${n(tip + L * 0.2)} L0 ${n(tip)} L${n(W)} ${n(tip + L * 0.2)} L${n(W)} -3Z`, edge: `M${n(W - 0.5)} -4 L${n(W - 0.5)} ${n(tip + L * 0.2)}`, width: W * 2 }; }
      case "abyss": {
        let d = `M${n(-h)} -3 L${n(-h)} ${n(tip + 7)} L${n(-h - 2.2)} ${n(tip + 4)} L${n(-h * 0.2)} ${n(tip + 2.5)} L0 ${n(tip)} L${n(h * 0.6)} ${n(tip + 3)}`;
        for (let y = tip + 5, i = 0; y < -5; y += 3.4, i++) d += ` L${n(h + (i % 2 ? 0 : 1.8))} ${n(y)}`;
        return { d: d + ` L${n(h)} -3Z`, edge: "", width: w + 2 };
      }
      case "hellmade": return { d: `M${n(-h)} -3 Q${n(-h * 1.6)} -5 ${n(-h)} -7.5 L${n(-h)} ${n(tip + 5)} L0 ${n(tip)} L${n(h)} ${n(tip + 5)} L${n(h)} -7.5 Q${n(h * 1.6)} -5 ${n(h)} -3Z`, edge: `M${n(h - 0.6)} -8 L${n(h - 0.6)} ${n(tip + 5)} L0 ${n(tip + 0.8)}`, width: w * 1.3 };
      case "hearthforge": return { d: `M${n(-h)} -3 L${n(-h * 0.82)} ${n(tip + 4)} Q${n(-h * 0.3)} ${n(tip + 0.6)} 0 ${n(tip)} Q${n(h * 0.3)} ${n(tip + 0.6)} ${n(h * 0.82)} ${n(tip + 4)} L${n(h)} -3Z`, edge: `M${n(h - 0.6)} -4 L${n(h * 0.75)} ${n(tip + 4)}`, width: w };
      case "outlaw": {
        const j = () => (rnd() - 0.5) * 1.2;
        let d = `M${n(-h + j())} -3`;
        for (let t = 0.25; t < 1; t += 0.25) d += ` L${n(-h + j() + t * 1.2)} ${n(-3 - L * t)}`;
        d += ` L${n(1.4 + j())} ${n(tip + 1)}`;
        for (let t = 0.85; t > 0; t -= 0.2) d += ` L${n(h + j() + t * 1.2 - (Math.round(t * 10) % 3 === 0 ? 1.2 : 0))} ${n(-3 - L * t)}`;
        return { d: d + ` L${n(h)} -3Z`, edge: "", width: w + 1.5 };
      }
      default: return { d: `M${n(-h)} -3 L${n(-h)} ${n(tip + 5)} L0 ${n(tip)} L${n(h)} ${n(tip + 5)} L${n(h)} -3Z`, edge: `M${n(h - 0.6)} -4 L${n(h - 0.6)} ${n(tip + 5)} L0 ${n(tip + 0.8)}`, width: w };
    }
  }
  function bladeDetails(maker, p, L, w) {
    const tip = -3 - L, s = STYLE[maker];
    if (maker === "roundtable") return line(`M0 -6 L0 ${n(tip + 8)}`, p.dark, 1, 'opacity=".75"');
    if (maker === "dwarven") return line(`M${n(-w * 0.15)} -6 L${n(-w * 0.15)} ${n(tip + 6)}`, p.dark, 1.2, 'opacity=".7"') + [0.3, 0.5, 0.7].map(t => line(`M${n(w * 0.2 - 0.8)} ${n(-3 - L * t)} l0.8 -1.2 l0.8 1.2`, s.trim, 0.7)).join("");
    if (maker === "elven") return line(`M0 -5 C${n(-w * 0.2)} ${n(-3 - L * 0.45)} ${n(w * 0.1)} ${n(tip + L * 0.3)} ${n(w * 0.15)} ${n(tip + 4)}`, p.dark, 0.7, 'opacity=".7"');
    if (maker === "hellmade") return line(`M0 -9 L0 ${n(tip + 7)}`, "!" + s.trim, 0.8) + line(`M${n(-w * 0.3)} -8 q${n(w * 0.3)} -2 0 -4 q${n(-w * 0.3)} -2 0 -4 M${n(w * 0.3)} -8 q${n(-w * 0.3)} -2 0 -4 q${n(w * 0.3)} -2 0 -4`, "!" + s.trim, 0.55);
    if (maker === "hearthforge") return line(`M${n(-w * 0.2)} -5 q1 ${n(-L * 0.12)} 0 ${n(-L * 0.24)} t0 ${n(-L * 0.24)} t0 ${n(-L * 0.24)}`, p.light, 0.7, 'opacity=".9"') + line(`M${n(-w * 0.35)} -3.6 h${n(w * 0.7)}`, "!#ff9a4a", 0.8);
    if (maker === "abyss") return line(`M${n(-w * 0.1)} -6 L${n(-w * 0.2)} ${n(tip + 8)}`, p.dark, 0.9, 'opacity=".8"');
    if (maker === "outlaw") return line(`M${n(-w * 0.2)} ${n(-3 - L * 0.4)} l1.2 1 M${n(w * 0.1)} ${n(-3 - L * 0.7)} l-1 1.2`, p.dark, 0.8);
    return "";
  }
  function crossguard(maker, p, G, rnd) {
    const s = STYLE[maker], g = G / 2;
    switch (maker) {
      case "dwarven": return path(`M${n(-g - 1)} -0.6 L${n(-g - 1)} -5.8 L${n(g + 1)} -5.8 L${n(g + 1)} -0.6Z`, p.base) + dot(-g + 0.6, -3.2, 0.8, s.trim) + dot(g - 0.6, -3.2, 0.8, s.trim) + line(`M${n(-g)} -4.8 h${n(G)}`, p.light, 0.7);
      case "elven": return path(`M${n(-g)} -7 Q${n(-g * 0.45)} -1 0 -2 Q${n(g * 0.45)} -1 ${n(g)} -7 L${n(g - 1.4)} -7.2 Q${n(g * 0.45)} -3.6 0 -4.4 Q${n(-g * 0.45)} -3.6 ${n(-g + 1.4)} -7.2Z`, p.base) + gem(0, -3, 1, s.gem);
      case "boreal-bolt": return path(`M${n(-g * 0.85)} -1.8 h${n(G * 0.85)} v-1.6 h${n(-G * 0.85)}Z`, p.base, thin);
      case "abyss": return path(`M${n(-g - 1.5)} 1.5 L${n(-g * 0.6)} -1.4 L${n(-g * 0.4)} -0.6 L0 -4.8 L${n(g * 0.4)} -0.6 L${n(g * 0.6)} -1.4 L${n(g + 1.5)} 1.5 L${n(g * 0.5)} -3.6 L0 -2.4 L${n(-g * 0.5)} -3.6Z`, p.base);
      case "hellmade": return path(`M${n(-g)} -1.4 h${n(G)} v-2.8 h${n(-G)}Z`, p.base) + circle(-g - 1.2, -2.2, 1.5, "!" + s.trim, thin) + circle(g + 1.2, -2.2, 1.5, "!" + s.trim, thin) + line(`M${n(-g - 1.2)} -2.2 m-0.6 0 a0.6 0.6 0 1 0 1.2 0 M${n(g + 1.2)} -2.2 m-0.6 0 a0.6 0.6 0 1 0 1.2 0`, darken(s.trim, 0.4), 0.5) + gem(0, -2.8, 1.25, s.gem);
      case "hearthforge": return path(`M${n(-g - 0.6)} -4.6 Q${n(-g * 0.5)} -1 0 -1.2 Q${n(g * 0.5)} -1 ${n(g + 0.6)} -4.6 Q${n(g * 0.4)} -3.2 0 -4 Q${n(-g * 0.4)} -3.2 ${n(-g - 0.6)} -4.6Z`, p.base) + gem(0, -2.6, 0.9, s.gem);
      case "outlaw": { const tilt = (rnd() - 0.5) * 1.6; return path(`M${n(-g)} ${n(-1.2 + tilt)} L${n(g)} ${n(-1.6 - tilt)} L${n(g)} ${n(-4 - tilt)} L${n(-g)} ${n(-3.6 + tilt)}Z`, p.base) + line(`M-1.6 -0.6 l3.2 -3.8 M1.6 -0.6 l-3.2 -3.8`, s.wrap, 0.9); }
      default: return path(`M${n(-g)} -1.2 h${n(G)} v-3.2 h${n(-G)}Z`, p.base) + path(`M${n(-g - 1)} -0.6 h2 v-4.4 h-2Z M${n(g - 1)} -0.6 h2 v-4.4 h-2Z`, p.base, thin);
    }
  }
  function grip(maker, p, length, rnd, withPommel = true) {
    const s = STYLE[maker];
    const width = maker === "dwarven" ? 4 : maker === "elven" ? 2.8 : 3.2, h = width / 2;
    const wood = p.kind === "wood" ? p.base : s.wrap;
    let out = path(`M${n(-h)} -1 h${n(width)} v${n(length)} h${n(-width)}Z`, wood);
    if (maker === "roundtable") out += line(Array.from({ length: Math.floor(length / 1.8) }, (_, i) => `M${n(-h)} ${n(i * 1.8)} l${n(width)} 1.6`).join(" "), darken(wood, 0.35), 0.6);
    if (maker === "dwarven") out += line(`M${n(-h)} ${n(length * 0.3)} h${n(width)} M${n(-h)} ${n(length * 0.7)} h${n(width)}`, p.kind === "wood" ? K.material("iron").base : p.base, 1.2);
    if (maker === "elven") out += line(`M${n(-h)} 0 Q${n(h)} ${n(length * 0.25)} ${n(-h)} ${n(length * 0.5)} T${n(-h)} ${n(length)}`, "#7fbf5a", 0.7);
    if (maker === "abyss") out += line(Array.from({ length: Math.floor(length / 2) }, (_, i) => `M${n(-h - 0.8)} ${n(i * 2 + 0.6)} l0.8 0.6`).join(" "), p.dark, 0.7);
    if (maker === "hellmade") out += line(Array.from({ length: Math.floor(length / 1.4) }, (_, i) => `M${n(-h)} ${n(i * 1.4 + 0.4)} h${n(width)}`).join(" "), "!" + s.trim, 0.45);
    if (maker === "hearthforge") out += line(Array.from({ length: Math.floor(length / 1.2) }, (_, i) => `M${n(-h)} ${n(i * 1.2 + 0.3)} l${n(width)} 0.5`).join(" "), darken(wood, 0.3), 0.45);
    if (maker === "outlaw") out += line(`M${n(-h - 0.4)} ${n(length * 0.2)} l${n(width + 0.8)} 1.4 M${n(-h - 0.4)} ${n(length * 0.55)} l${n(width + 0.8)} -1 M${n(-h - 0.4)} ${n(length * 0.8)} l${n(width + 0.8)} 1.2`, "#a08a5a", 0.9);
    out += shaftMarkings(p, 0, length, 0);
    return out + (withPommel ? pommelCap(maker, p, length + 1.6, rnd, true) : "");
  }
  // The end cap of a grip or haft.
  function pommelCap(maker, p, y, rnd, small = false) {
    const s = STYLE[maker], r = small ? 2.1 : 2.6;
    const metal = p.kind === "wood" || p.kind === "thread" ? K.material("iron") : p;
    switch (maker) {
      case "dwarven": return path(`M${n(-r)} ${n(y - r * 0.8)} L${n(r)} ${n(y - r * 0.8)} L${n(r * 1.1)} ${n(y + r * 0.9)} L${n(-r * 1.1)} ${n(y + r * 0.9)}Z`, metal.base) + dot(0, y, 0.7, s.trim);
      case "elven": return path(`M0 ${n(y - r)} Q${n(r * 1.3)} ${n(y + 0.4)} 0 ${n(y + r * 1.6)} Q${n(-r * 1.3)} ${n(y + 0.4)} 0 ${n(y - r)}Z`, metal.base, thin) + line(`M0 ${n(y - r * 0.6)} V${n(y + r * 1.2)}`, metal.dark, 0.5);
      case "boreal-bolt": return path(`M${n(-r * 0.8)} ${n(y - r * 0.6)} h${n(r * 1.6)} v${n(r * 1.2)} h${n(-r * 1.6)}Z`, metal.base, thin);
      case "abyss": return path(`M${n(-r)} ${n(y - r * 0.6)} L${n(r)} ${n(y - r * 0.6)} L0 ${n(y + r * 2)}Z`, metal.base);
      case "hellmade": return circle(0, y, r, "!" + s.trim, thin) + gem(0, y, r * 0.55, s.gem);
      case "hearthforge": return path(`M0 ${n(y - r)} L${n(r)} ${n(y)} L0 ${n(y + r)} L${n(-r)} ${n(y)}Z`, metal.base, thin) + dot(0, y, 0.6, "!#ff9a4a");
      case "outlaw": return circle(0, y, r * 0.9, STYLE.outlaw.wrap, thin) + line(`M${n(-r)} ${n(y - 0.4)} l${n(r * 2)} 0.8`, "#5a4a32", 0.7) + (rnd() < 0.5 ? line(`M${n(r * 0.4)} ${n(y)} l1.6 1.6`, "#6a6e74", 0.8) : "");
      default: return circle(0, y, r, metal.base) + circle(0, y, r * 0.45, metal.light, 'stroke-width=".5"');
    }
  }

  /* ---------- Hafts and heads ---------- */
  function haft(maker, p, top, bottom, rnd, thick = 3.2) {
    const h = thick / 2, s = STYLE[maker];
    const wood = p.kind === "thread" ? K.material("wood").base : p.base;
    let body;
    if (maker === "elven") body = path(`M${n(-h)} ${n(bottom)} Q${n(-h - 1.2)} ${n((top + bottom) / 2)} ${n(-h + 0.4)} ${n(top)} L${n(h + 0.4)} ${n(top)} Q${n(h - 1.2)} ${n((top + bottom) / 2)} ${n(h)} ${n(bottom)}Z`, wood);
    else if (maker === "outlaw") { const bend = (rnd() - 0.5) * 2.4; body = path(`M${n(-h)} ${n(bottom)} Q${n(-h + bend)} ${n((top + bottom) / 2)} ${n(-h + 0.3)} ${n(top)} L${n(h + 0.3)} ${n(top)} Q${n(h + bend)} ${n((top + bottom) / 2)} ${n(h)} ${n(bottom)}Z`, wood); }
    else body = path(`M${n(-h)} ${n(bottom)} L${n(-h)} ${n(top)} L${n(h)} ${n(top)} L${n(h)} ${n(bottom)}Z`, maker === "dwarven" ? wood : wood, maker === "dwarven" ? 'stroke-width="1.4"' : "");
    let bands = "";
    const length = bottom - top;
    if (maker === "dwarven") bands = line(`M${n(-h)} ${n(top + length * 0.2)} h${n(thick)} M${n(-h)} ${n(top + length * 0.55)} h${n(thick)} M${n(-h)} ${n(bottom - 3)} h${n(thick)}`, K.material("iron").base, 1.4);
    if (maker === "roundtable") bands = path(`M${n(-h - 0.2)} -2 h${n(thick + 0.4)} v5 h${n(-thick - 0.4)}Z`, s.wrap, 'stroke-width=".7"');
    if (maker === "hellmade") bands = line(`M${n(-h)} ${n(top + length * 0.25)} h${n(thick)} M${n(-h)} ${n(top + length * 0.6)} h${n(thick)}`, "!" + s.trim, 1.1);
    if (maker === "hearthforge") bands = line(`M${n(-h)} ${n(top + 3)} h${n(thick)} M${n(-h)} -2 h${n(thick)} M${n(-h)} 3 h${n(thick)}`, darken(s.trim, 0.1), 0.9);
    if (maker === "abyss") bands = line(Array.from({ length: 4 }, (_, i) => `M${n(h)} ${n(top + length * (0.2 + i * 0.18))} l1.6 -1`).join(" "), p.dark, 0.9);
    if (maker === "outlaw") bands = line(`M${n(-h - 0.4)} -2 l${n(thick + 0.8)} 1.4 M${n(-h - 0.4)} 1 l${n(thick + 0.8)} -1.2`, "#a08a5a", 1);
    if (maker === "church-of-talos") bands = line(`M${n(-h)} ${n(top + length * 0.18)} l${n(thick)} -1.6 l${n(-thick)} -1.6 l${n(thick)} -1.6 M${n(-h)} ${n(top + length * 0.62)} l${n(thick)} -1.6 l${n(-thick)} -1.6`, "!" + s.trim, 0.8);
    return body + shaftMarkings(p, 0, bottom, top) + bands;
  }
  function axeHead(maker, p, top, big, rnd) {
    const s = STYLE[maker], k = big ? 1.35 : 1;
    let d, edge = "";
    switch (maker) {
      case "dwarven": d = `M-1.5 ${n(top + 2)} L${n(11 * k)} ${n(top - 3 * k)} L${n(12 * k)} ${n(top + 13 * k)} L-1.5 ${n(top + 10 * k)}Z M-1.5 ${n(top + 3)} L${n(-8 * k)} ${n(top + 1)} L${n(-8.5 * k)} ${n(top + 10 * k)} L-1.5 ${n(top + 9 * k)}Z`; edge = `M${n(11.4 * k)} ${n(top - 2 * k)} L${n(12.2 * k)} ${n(top + 12 * k)}`; break;
      case "elven": d = `M0 ${n(top + 3)} C${n(6 * k)} ${n(top - 2 * k)} ${n(14 * k)} ${n(top - 3 * k)} ${n(13 * k)} ${n(top + 4 * k)} C${n(12 * k)} ${n(top + 10 * k)} ${n(8 * k)} ${n(top + 14 * k)} ${n(2 * k)} ${n(top + 15 * k)} C${n(5 * k)} ${n(top + 10 * k)} ${n(4 * k)} ${n(top + 6 * k)} 0 ${n(top + 7 * k)}Z`; edge = `M${n(12.6 * k)} ${n(top - 1 * k)} C${n(13.4 * k)} ${n(top + 5 * k)} ${n(10 * k)} ${n(top + 11 * k)} ${n(4 * k)} ${n(top + 14 * k)}`; break;
      case "abyss": d = `M0 ${n(top + 3)} L${n(10 * k)} ${n(top - 4 * k)} L${n(9 * k)} ${n(top)} L${n(13 * k)} ${n(top + 1 * k)} L${n(11 * k)} ${n(top + 5 * k)} L${n(14 * k)} ${n(top + 8 * k)} L${n(10 * k)} ${n(top + 10 * k)} L${n(12 * k)} ${n(top + 15 * k)} L${n(1 * k)} ${n(top + 11 * k)}Z M0 ${n(top + 4)} L${n(-6 * k)} ${n(top - 3 * k)} L${n(-3 * k)} ${n(top + 6 * k)}Z`; break;
      case "hellmade": d = `M0 ${n(top + 3)} Q${n(9 * k)} ${n(top - 6 * k)} ${n(13 * k)} ${n(top + 1 * k)} Q${n(15 * k)} ${n(top + 7 * k)} ${n(13 * k)} ${n(top + 13 * k)} Q${n(9 * k)} ${n(top + 17 * k)} 0 ${n(top + 11 * k)} Q${n(5 * k)} ${n(top + 7 * k)} 0 ${n(top + 3)}Z`; edge = `M${n(13.6 * k)} ${n(top + 1 * k)} Q${n(15.4 * k)} ${n(top + 7 * k)} ${n(13.6 * k)} ${n(top + 13 * k)}`; break;
      case "hearthforge": d = `M0 ${n(top + 3)} Q${n(10 * k)} ${n(top - 3 * k)} ${n(12.5 * k)} ${n(top + 3 * k)} Q${n(13.5 * k)} ${n(top + 9 * k)} ${n(10 * k)} ${n(top + 14 * k)} Q${n(4 * k)} ${n(top + 10 * k)} 0 ${n(top + 10 * k)}Z`; edge = `M${n(12.8 * k)} ${n(top + 3 * k)} Q${n(13.8 * k)} ${n(top + 9 * k)} ${n(10.4 * k)} ${n(top + 13.6 * k)}`; break;
      case "outlaw": { const j = () => (rnd() - 0.5) * 1.6; d = `M0 ${n(top + 2)} L${n(9 * k + j())} ${n(top - 1 + j())} L${n(11 * k + j())} ${n(top + 7 * k)} L${n(8 * k + j())} ${n(top + 12 * k)} L0 ${n(top + 9 * k)}Z`; break; }
      default: d = `M0 ${n(top + 2)} Q${n(11 * k)} ${n(top - 5 * k)} ${n(11.5 * k)} ${n(top + 4 * k)} Q${n(11 * k)} ${n(top + 13 * k)} ${n(2 * k)} ${n(top + 14 * k)} L${n(2 * k)} ${n(top + 9 * k)} Q${n(1 * k)} ${n(top + 6 * k)} 0 ${n(top + 7 * k)}Z`; edge = `M${n(10.8 * k)} ${n(top - 2 * k)} Q${n(12.4 * k)} ${n(top + 4 * k)} ${n(10.6 * k)} ${n(top + 11 * k)}`;
    }
    let extra = markings(p, 6 * k, top + 10 * k, top, 6);
    if (maker === "hellmade") extra += gem(5 * k, top + 5 * k, 1.3, s.gem) + line(`M${n(2 * k)} ${n(top + 3 * k)} q${n(3 * k)} ${n(2 * k)} ${n(2 * k)} ${n(5 * k)}`, "!" + s.trim, 0.6);
    if (maker === "dwarven") extra += dot(2, top + 4, 0.8, s.trim) + dot(2, top + 8 * k, 0.8, s.trim);
    if (maker === "outlaw") extra += line(`M-1.8 ${n(top + 2)} l3.6 3 M-1.8 ${n(top + 5)} l3.6 -3`, "#a08a5a", 0.9);
    return glowWrap(p, path(d, p.base) + (edge ? line(edge, p.light, 1) : "")) + extra;
  }
  function bluntHead(maker, p, top, style, big, rnd) {
    const s = STYLE[maker], k = big ? 1.3 : 1;
    // A portable anvil (Ornn's Anvil Hammer) seen side-on, its base on the handle: a thick face
    // block of dark anvil iron with a Mythilium inlay, a curving horn, a squared heel with its
    // hardy hole, a pinched waist and a base split into two feet.
    if (style === "anvil") {
      const w = 24, h = 19, y0 = top - h, yb = top + 1, face = y0 + 6.4, waist = y0 + h * 0.66, iron = { base: "#555a62", light: "#9aa1aa", dark: "#2b2e33" };
      const body = `M${n(-w / 2)} ${n(y0)} H${n(w / 2)} Q${n(w / 2 + 8)} ${n(y0 + 0.6)} ${n(w / 2 + 13)} ${n(y0 + 3.4)} Q${n(w / 2 + 6)} ${n(y0 + 6)} ${n(w / 2 - 1)} ${n(face)} L${n(w * 0.28)} ${n(y0 + 7.5)} L${n(w * 0.24)} ${n(waist)} L${n(w * 0.42)} ${n(yb - 2.5)} L${n(w * 0.46)} ${n(yb)} H${n(-w * 0.46)} L${n(-w * 0.42)} ${n(yb - 2.5)} L${n(-w * 0.24)} ${n(waist)} L${n(-w * 0.28)} ${n(y0 + 7.5)} L${n(-w / 2 + 1)} ${n(face)} L${n(-w / 2)} ${n(y0 + 6)}Z`;
      const shade = `M${n(-w / 2)} ${n(y0 + 6)} L${n(-w / 2 + 1)} ${n(face)} L${n(-w * 0.28)} ${n(y0 + 7.5)} L${n(-w * 0.24)} ${n(waist)} L${n(-w * 0.42)} ${n(yb - 2.5)} L${n(-w * 0.46)} ${n(yb)} H${n(-w * 0.16)} L${n(-w * 0.06)} ${n(waist)} L${n(-w * 0.1)} ${n(face)}Z`;
      const head = path(body, iron.base) + path(shade, iron.dark, 'stroke="none" opacity=".55"');
      let extra = line(`M${n(-w / 2 + 0.8)} ${n(y0 + 0.9)} H${n(w / 2 + 3)}`, iron.light, 1.1)
        + line(`M${n(-w / 2 + 0.6)} ${n(face)} H${n(w / 2 - 1)}`, iron.dark, 1)
        + glowWrap(p, line(`M${n(-w / 2 + 1.5)} ${n(y0 + 3.4)} H${n(w / 2 - 1)}`, p.base, 1.4))
        + path(`M${n(-w / 2 + 2.2)} ${n(y0)} h2.6 v2 h-2.6Z`, iron.dark, 'stroke-width=".5"')
        + line(`M${n(-w * 0.42)} ${n(yb - 2.5)} H${n(w * 0.42)}`, iron.dark, 0.8, 'opacity=".7"');
      if (maker === "hearthforge") extra += line(`M${n(-w / 2 + 3)} ${n(y0 + 5)} H${n(w / 2 - 2)}`, "!#ff9a4a", 0.7, 'opacity=".9"') + dot(w / 2 + 12, y0 + 3.4, 0.8, "!#ffcf7a");
      return head + extra;
    }
    if (style === "hammer") {
      const w = (maker === "dwarven" ? 20 : 17) * (big ? 1.05 : 0.8), h = (maker === "dwarven" ? 13 : 11) * (big ? 1 : 0.8), y = top - h / 2;
      let head;
      if (maker === "abyss") head = path(`M${n(-w / 2)} ${n(y - h / 2)} h${n(w)} v${n(h)} h${n(-w)}Z`, p.base) + path(`M${n(w / 2)} ${n(y - h / 2)} l4 ${n(h / 2)} l-4 ${n(h / 2)}Z M${n(-w / 2)} ${n(y - h / 2 + 1)} l-3 -2 l1 ${n(h)} Z`, p.dark, thin);
      else if (maker === "elven") head = path(`M${n(-w / 2)} ${n(y)} Q${n(-w / 2)} ${n(y - h / 2)} ${n(-w / 4)} ${n(y - h / 2)} H${n(w / 4)} Q${n(w / 2)} ${n(y - h / 2)} ${n(w / 2)} ${n(y)} Q${n(w / 2)} ${n(y + h / 2)} ${n(w / 4)} ${n(y + h / 2)} H${n(-w / 4)} Q${n(-w / 2)} ${n(y + h / 2)} ${n(-w / 2)} ${n(y)}Z`, p.base);
      else if (maker === "outlaw") head = path(`M${n(-w / 2 + (rnd() - 0.5))} ${n(y - h / 2)} L${n(w / 2)} ${n(y - h / 2 + 1)} L${n(w / 2 - 1)} ${n(y + h / 2)} L${n(-w / 2)} ${n(y + h / 2 - 1)}Z`, p.base) + line(`M-2 ${n(y - h / 2)} l4 ${n(h)} M2 ${n(y - h / 2)} l-4 ${n(h)}`, "#a08a5a", 1);
      else head = path(`M${n(-w / 2)} ${n(y - h / 2)} h${n(w)} v${n(h)} h${n(-w)}Z`, p.base);
      let extra = line(`M${n(-w / 2 + 1)} ${n(y - h / 2 + 1.2)} h${n(w - 2)}`, p.light, 1) + markings(p, 0, y + h / 2, y - h / 2, w * 0.6);
      if (maker === "dwarven") extra += [-w / 2 + 1.6, w / 2 - 1.6].map(x => dot(x, y, 0.9, s.trim)).join("") + line(`M${n(-w / 2 + 3)} ${n(y - h / 2)} v${n(h)} M${n(w / 2 - 3)} ${n(y - h / 2)} v${n(h)}`, p.dark, 1);
      if (maker === "hellmade") extra += path(`M${n(-w / 2)} ${n(y - h / 2)} h${n(w)} v1.6 h${n(-w)}Z M${n(-w / 2)} ${n(y + h / 2 - 1.6)} h${n(w)} v1.6 h${n(-w)}Z`, "!" + s.trim, 'stroke-width=".6"') + gem(0, y, 1.5, s.gem);
      if (maker === "hearthforge") extra += line(`M${n(-w / 2 + 2)} ${n(y + h / 2 - 1.5)} h${n(w - 4)}`, "!#ff9a4a", 0.8);
      if (maker === "roundtable") extra += line(`M${n(-w / 2 + 2.5)} ${n(y - h / 2)} v${n(h)} M${n(w / 2 - 2.5)} ${n(y - h / 2)} v${n(h)}`, p.dark, 0.9);
      return glowWrap(p, head) + extra;
    }
    const r = (style === "knob" ? 6.5 : style === "flail" ? 5 : 5.5) * k, cy = top - r + 1;
    let head;
    if (maker === "abyss" || style === "flail" && maker !== "elven") {
      const spikes = Array.from({ length: 8 }, (_, i) => { const a = i * Math.PI / 4; return path(`M${n(Math.cos(a) * r * 0.8)} ${n(cy + Math.sin(a) * r * 0.8)} L${n(Math.cos(a) * r * 1.6)} ${n(cy + Math.sin(a) * r * 1.6)} L${n(Math.cos(a + 0.35) * r * 0.9)} ${n(cy + Math.sin(a + 0.35) * r * 0.9)}Z`, p.dark, 'stroke-width=".8"'); }).join("");
      head = spikes + circle(0, cy, r, p.base);
    } else if (maker === "dwarven") head = path(`M${n(-r)} ${n(cy - r * 0.7)} L${n(-r * 0.6)} ${n(cy - r)} H${n(r * 0.6)} L${n(r)} ${n(cy - r * 0.7)} V${n(cy + r * 0.7)} L${n(r * 0.6)} ${n(cy + r)} H${n(-r * 0.6)} L${n(-r)} ${n(cy + r * 0.7)}Z`, p.base) + dot(-r * 0.5, cy, 0.8, s.trim) + dot(r * 0.5, cy, 0.8, s.trim);
    else if (maker === "elven") head = circle(0, cy, r * 0.85, p.base) + [0, 1, 2, 3, 4].map(i => at(0, cy, path(`M0 ${n(-r * 0.6)} Q${n(r * 0.5)} ${n(-r * 1.2)} 0 ${n(-r * 1.6)} Q${n(-r * 0.5)} ${n(-r * 1.2)} 0 ${n(-r * 0.6)}Z`, mix(p.base, "#7fbf5a", 0.35), 'stroke-width=".7"'), i * 72)).join("");
    else if (maker === "hellmade") head = [0, 1, 2, 3, 4, 5].map(i => at(0, cy, path(`M-1.6 ${n(-r * 0.7)} L0 ${n(-r * 1.45)} L1.6 ${n(-r * 0.7)}Z`, "!" + s.trim, 'stroke-width=".6"'), i * 60)).join("") + circle(0, cy, r * 0.95, p.base) + gem(0, cy, r * 0.35, s.gem);
    else if (maker === "outlaw") head = path(`M${n(-r)} ${n(cy)} L${n(-r * 0.6)} ${n(cy - r)} L${n(r * 0.7)} ${n(cy - r * 0.9)} L${n(r)} ${n(cy + r * 0.2)} L${n(r * 0.3)} ${n(cy + r)} L${n(-r * 0.8)} ${n(cy + r * 0.7)}Z`, p.base) + [0, 1, 2].map(i => line(`M${n(-r * 0.4 + i * r * 0.4)} ${n(cy - r * 0.5)} l${n((rnd() - 0.5) * 2)} -2.4`, "#6a6e74", 0.8)).join("");
    else if (maker === "hearthforge") head = path(`M0 ${n(cy - r)} L${n(r * 0.87)} ${n(cy - r * 0.5)} L${n(r * 0.87)} ${n(cy + r * 0.5)} L0 ${n(cy + r)} L${n(-r * 0.87)} ${n(cy + r * 0.5)} L${n(-r * 0.87)} ${n(cy - r * 0.5)}Z`, p.base) + line(`M0 ${n(cy - r)} V${n(cy + r)} M${n(-r * 0.87)} ${n(cy - r * 0.5)} L${n(r * 0.87)} ${n(cy + r * 0.5)}`, p.dark, 0.6, 'opacity=".7"');
    else head = [0, 1, 2, 3, 4, 5].map(i => { const a = i * Math.PI / 3; return path(`M${n(Math.cos(a) * r * 0.6)} ${n(cy + Math.sin(a) * r * 0.6)} L${n(Math.cos(a) * r * 1.4)} ${n(cy + Math.sin(a) * r * 1.4)} L${n(Math.cos(a + 0.5) * r * 0.9)} ${n(cy + Math.sin(a + 0.5) * r * 0.9)}Z`, p.dark, 'stroke-width=".9"'); }).join("") + circle(0, cy, r, p.base);
    return glowWrap(p, head + dot(-r * 0.3, cy - r * 0.35, r * 0.25, p.light, 'opacity=".75"')) + markings(p, 0, cy + r * 0.6, cy - r * 0.6, r);
  }
  function spearhead(maker, p, top, lance) {
    const s = STYLE[maker], L = lance ? 18 : 14;
    let d;
    switch (maker) {
      case "dwarven": d = `M0 ${n(top - L)} L4.2 ${n(top - L * 0.45)} L3 ${n(top)} L-3 ${n(top)} L-4.2 ${n(top - L * 0.45)}Z`; break;
      case "elven": d = `M0.8 ${n(top - L - 2)} Q5.4 ${n(top - L * 0.55)} 2 ${n(top)} L-2 ${n(top)} Q-3.4 ${n(top - L * 0.5)} 0.8 ${n(top - L - 2)}Z`; break;
      case "abyss": d = `M0 ${n(top - L)} L2.8 ${n(top - L * 0.6)} L5 ${n(top - L * 0.4)} L2.2 ${n(top - L * 0.25)} L4 ${n(top + 1)} L0 ${n(top - 2)} L-4 ${n(top + 1)} L-2.2 ${n(top - L * 0.25)} L-5 ${n(top - L * 0.4)} L-2.8 ${n(top - L * 0.6)}Z`; break;
      case "hellmade": d = `M0 ${n(top - L)} Q4 ${n(top - L * 0.55)} 2 ${n(top - 2)} Q5.5 ${n(top - 3)} 6 ${n(top + 1)} L2 ${n(top)} L-2 ${n(top)} L-6 ${n(top + 1)} Q-5.5 ${n(top - 3)} -2 ${n(top - 2)} Q-4 ${n(top - L * 0.55)} 0 ${n(top - L)}Z`; break;
      case "hearthforge": d = `M0 ${n(top - L - 1)} Q3.6 ${n(top - L * 0.5)} 2 ${n(top)} L-2 ${n(top)} Q-3.6 ${n(top - L * 0.5)} 0 ${n(top - L - 1)}Z`; break;
      case "outlaw": d = `M0.6 ${n(top - L + 2)} L3 ${n(top - L * 0.4)} L1.8 ${n(top)} L-2.2 ${n(top)} L-2.4 ${n(top - L * 0.5)}Z`; break;
      default: d = `M0 ${n(top - L)} Q4.6 ${n(top - L * 0.5)} 2.2 ${n(top)} L-2.2 ${n(top)} Q-4.6 ${n(top - L * 0.5)} 0 ${n(top - L)}Z`;
    }
    let extra = line(`M0 ${n(top - L + 2)} L0 ${n(top - 1)}`, p.dark, 0.8) + markings(p, 0, top - 2, top - L + 3, 3);
    if (maker === "hellmade") extra += gem(0, top - 3, 1, s.gem);
    if (maker === "hearthforge") extra += line(`M-1 ${n(top - 1.5)} h2`, "!#ff9a4a", 0.7);
    if (maker === "outlaw") extra += line(`M-2 ${n(top + 1)} l4 2.6 M-2 ${n(top + 3.6)} l4 -2.6`, "#a08a5a", 0.9);
    if (lance) extra += path(`M-5 ${n(top + 10)} L5 ${n(top + 10)} L1.8 ${n(top + 2)} L-1.8 ${n(top + 2)}Z`, mix(p.base, STYLE[maker].trim, 0.2), thin);
    return glowWrap(p, path(d, p.base)) + extra;
  }

  /* ---------- Bows ---------- */
  function bowLimbs(maker, p, half, belly, heavy, compound) {
    const s = STYLE[maker], limb = heavy ? 3.4 : 2.6;
    let curve;
    if (maker === "elven" || maker === "hellmade") curve = `M${n(-1.5)} ${n(-half - 1)} Q${n(belly * 0.4)} ${n(-half + 2)} ${n(belly * 1.5)} ${n(-half * 0.5)} Q${n(belly * 0.55)} ${n(-half * 0.15)} ${n(belly * 0.55)} 0 Q${n(belly * 0.55)} ${n(half * 0.15)} ${n(belly * 1.5)} ${n(half * 0.5)} Q${n(belly * 0.4)} ${n(half - 2)} ${n(-1.5)} ${n(half + 1)}`;
    else if (maker === "boreal-bolt") curve = `M0 ${n(-half)} L${n(belly * 0.9)} ${n(-half * 0.45)} L${n(belly * 0.55)} 0 L${n(belly * 0.9)} ${n(half * 0.45)} L0 ${n(half)}`;
    else if (maker === "outlaw") curve = `M0.6 ${n(-half + 1)} Q${n(belly * 1.6)} ${n(-half * 0.4)} ${n(belly * 0.6)} 0 Q${n(belly * 1.9)} ${n(half * 0.5)} -0.4 ${n(half - 1)}`;
    else curve = `M0 ${n(-half)} Q${n(belly * 1.8)} ${n(-half * 0.45)} ${n(belly * 0.55)} 0 Q${n(belly * 1.8)} ${n(half * 0.45)} 0 ${n(half)}`;
    const body = path(curve, "none", `stroke="${K.OUTLINE}" stroke-width="${n(limb + 1.8)}" stroke-linecap="round" fill="none"`) + path(curve, "none", `stroke="${K.paint(p.base)}" stroke-width="${n(limb)}" stroke-linecap="round" fill="none"`);
    let extra = "";
    if (maker === "hearthforge") extra += path(curve, "none", `stroke="${K.paint(p.light)}" stroke-width=".6" fill="none" transform="translate(0.5 0)"`);
    if (maker === "hellmade" || maker === "elven") extra += [-1, 1].map(sign => circle(-1.2, sign * (half + 1), 1.3, "!" + s.trim, 'stroke-width=".6"')).join("");
    if (p.pattern === "birch") extra += [-0.6, -0.3, 0.3, 0.6].map(t => dot(belly * (1 - Math.abs(t)) * 1.1, half * t, 0.7, "#2a2620")).join("");
    if (p.pattern === "leaves") extra += at(belly * 1.2, -half * 0.45, path("M0 0 Q2.6 -2.2 5 0 Q2.6 2.2 0 0Z", "#7fbf5a", 'stroke-width=".6"'), -60);
    if (compound) extra += [-1, 1].map(sign => circle(0.2, sign * half, 2.4, K.material("steel").base, thin) + dot(0.2, sign * half, 0.7, K.OUTLINE)).join("");
    if (heavy) extra += path(`M${n(belly * 0.4)} ${n(-half * 0.35)} h4 v3 h-4Z M${n(belly * 0.4)} ${n(half * 0.35 - 3)} h4 v3 h-4Z`, K.material("iron").base, thin);
    return glowWrap(p, body) + extra;
  }
  function bowGrip(maker, p, belly) {
    const s = STYLE[maker], x = belly * 0.55 - 2;
    const metal = p.kind === "wood" ? K.material("iron") : p;
    switch (maker) {
      case "dwarven": return path(`M${n(x - 0.6)} -4.5 h4.6 v9 h-4.6Z`, metal.base) + dot(x + 1.7, -2.6, 0.7, s.trim) + dot(x + 1.7, 2.6, 0.7, s.trim);
      case "elven": return path(`M${n(x)} -3.6 Q${n(x + 4.4)} 0 ${n(x)} 3.6 Q${n(x + 1.6)} 0 ${n(x)} -3.6Z`, p.base, thin) + at(x + 2.6, 0, path("M0 0 Q2 -1.8 4 0 Q2 1.8 0 0Z", "#7fbf5a", 'stroke-width=".5"'), 90);
      case "boreal-bolt": return path(`M${n(x)} -3 h3.4 v6 h-3.4Z`, metal.base, thin);
      case "abyss": return path(`M${n(x)} -3.4 h3.4 v6.8 h-3.4Z M${n(x + 3.4)} -2 l2 -1.4 v2 Z M${n(x + 3.4)} 2 l2 1.4 v-2 Z`, metal.base, thin);
      case "hellmade": return path(`M${n(x)} -3.6 h3.6 v7.2 h-3.6Z`, s.wrap, thin) + line(`M${n(x)} -2 h3.6 M${n(x)} 0 h3.6 M${n(x)} 2 h3.6`, "!" + s.trim, 0.5) + gem(x + 1.8, -4.6, 0.9, s.gem);
      case "hearthforge": return path(`M${n(x)} -3.4 h3.4 v6.8 h-3.4Z`, s.wrap, thin) + line(`M${n(x)} -1.2 h3.4 M${n(x)} 1.2 h3.4`, "!#ff9a4a", 0.6);
      case "outlaw": return path(`M${n(x - 0.3)} -3.2 l4 0.4 l-0.4 6 l-3.6 -0.2Z`, "#8a7a5a", thin) + line(`M${n(x)} -1 l3.4 1.6 M${n(x)} 1.6 l3.4 -1.4`, "#5a4a32", 0.7);
      default: return path(`M${n(x)} -3 h3.4 v6 h-3.4Z`, s.wrap, thin) + dot(x + 1.7, -1.6, 0.55, s.trim) + dot(x + 1.7, 1.6, 0.55, s.trim);
    }
  }
  function bowString(p, half, strong) {
    const content = `<g class="sp-bowstring">${line(`M0.4 ${n(-half + 1)} L0.4 ${n(half - 1)}`, p.glow ? "!" + p.base : p.base, strong ? 1.1 : 0.7)}</g>`;
    return p.glow ? `<g class="sp-glow" style="color:${p.glow}">${content}</g>` : content;
  }
  function crossbowParts(parts, palettes, rnd) {
    const [body, mech, string] = parts, [bp, mp, sp] = palettes, maker = body.maker, s = STYLE[maker];
    let stock;
    if (maker === "elven") stock = path("M-1.6 8 Q-3 -8 -1.4 -27 L1.4 -27 Q0.6 -8 2.4 8Z", bp.base);
    else if (maker === "boreal-bolt") stock = path("M-1.6 8 L-1.6 -27 L1.6 -27 L1.6 8Z", bp.base, thin);
    else if (maker === "outlaw") stock = path(`M-2 8 L-1.4 -26 L1.8 -27 L2.4 8Z`, bp.base) + line("M-2 -4 l4.2 1.6", "#a08a5a", 0.9);
    else if (maker === "hellmade") stock = path("M-2.2 8 Q-2.6 -4 -1.6 -27 L1.6 -27 Q2.6 -4 2.2 8Z", bp.base) + line("M0 -24 V4", "!" + s.trim, 0.6);
    else if (maker === "hearthforge") stock = path("M-2 8 L-1.6 -27 L1.6 -27 L2 8Z", bp.base) + line("M-0.6 -24 V6", bp.light, 0.6, 'opacity=".8"');
    else stock = path("M-2 8 L-2 -26 L2 -26 L2 8Z", bp.base);
    const prod = glowWrap(bp, path("M-14 -22 Q0 -30 14 -22 L13 -19 Q0 -26 -13 -19Z", bp.base));
    const strung = line("M-13 -20 L0 -12 L13 -20", sp.glow ? "!" + sp.base : sp.base, 0.9);
    const metal = mp.kind === "wood" || mp.kind === "thread" ? K.material("iron") : mp;
    let mechanism = path("M-2.6 -14 h5.2 v6 h-5.2Z", metal.base, thin) + path("M0.6 -8 q2.6 2 1 5", "none", `stroke="${K.paint(metal.dark)}" stroke-width="1.1" fill="none"`);
    if (mech.maker === "dwarven") mechanism += dot(-1.6, -12.6, 0.6, STYLE.dwarven.trim) + dot(1.6, -12.6, 0.6, STYLE.dwarven.trim);
    if (mech.maker === "hellmade") mechanism += gem(0, -11, 0.9, STYLE.hellmade.gem);
    if (mech.maker === "outlaw") mechanism += line("M-2.6 -11 l5.2 1.6", "#a08a5a", 0.8);
    const bolt = path("M-1.4 -34 L1.4 -34 L0 -40Z M-0.6 -34 L0.6 -34 L0.6 -14 L-0.6 -14Z", K.material("iron").base, 'stroke-width=".8"');
    return stock + markings(bp, 0, 4, -24, 3) + prod + strung + mechanism + bolt;
  }

  /* ---------- Staffs and wands ---------- */
  function stone(stoneId, tier, cx, cy, scale = 1) {
    const p = STONE_PALETTES[stoneId] || STONE_PALETTES.diamond, r = [0, 2.6, 3.4, 4.4][tier] * scale;
    const d = `M${n(cx)} ${n(cy - r * 1.25)} L${n(cx + r)} ${n(cy - r * 0.25)} L${n(cx + r * 0.62)} ${n(cy + r)} L${n(cx - r * 0.62)} ${n(cy + r)} L${n(cx - r)} ${n(cy - r * 0.25)}Z`;
    const facets = `M${n(cx - r)} ${n(cy - r * 0.25)} H${n(cx + r)} M${n(cx)} ${n(cy - r * 1.25)} L${n(cx - r * 0.35)} ${n(cy - r * 0.25)} L${n(cx)} ${n(cy + r)} L${n(cx + r * 0.35)} ${n(cy - r * 0.25)}Z`;
    const flecks = stoneId === "lapis-lazuli" ? dot(cx + r * 0.3, cy + r * 0.3, 0.45, "!#f2cf73") + dot(cx - r * 0.2, cy + r * 0.55, 0.4, "!#f2cf73") : "";
    return `<g class="sp-glow sp-orb" style="color:${p.glow}">${path(d, "!" + p.base, 'stroke-width=".8"')}${line(facets, "!" + p.light, 0.45, 'opacity=".75"')}${flecks}${dot(cx - r * 0.38, cy - r * 0.5, r * 0.22, "#ffffff", 'opacity=".85"')}</g>`;
  }
  // The setting that holds the jewel or gem: back pieces behind it, front pieces over it.
  function setting(maker, cx, cy, r, metal) {
    const s = STYLE[maker] || STYLE["institute-of-magic"], trim = s.trim;
    switch (maker) {
      case "church-of-talos": return { back: path(`M${n(cx - 1.4)} ${n(cy + r * 1.4)} L${n(cx - r * 1.5)} ${n(cy + r * 0.2)} L${n(cx - r * 0.9)} ${n(cy - r * 0.1)} L${n(cx - r * 1.7)} ${n(cy - r * 1.5)} L${n(cx - r * 0.5)} ${n(cy - r * 0.4)} L${n(cx - r * 1)} ${n(cy - r * 0.2)}Z M${n(cx + 1.4)} ${n(cy + r * 1.4)} L${n(cx + r * 1.5)} ${n(cy + r * 0.2)} L${n(cx + r * 0.9)} ${n(cy - r * 0.1)} L${n(cx + r * 1.7)} ${n(cy - r * 1.5)} L${n(cx + r * 0.5)} ${n(cy - r * 0.4)} L${n(cx + r * 1)} ${n(cy - r * 0.2)}Z`, "!" + trim, 'stroke-width=".7"'), front: "" };
      case "elven": return { back: path(`M${n(cx)} ${n(cy + r * 1.6)} Q${n(cx - r * 2.2)} ${n(cy)} ${n(cx - r * 0.9)} ${n(cy - r * 1.7)} Q${n(cx - r * 0.6)} ${n(cy - r * 0.4)} ${n(cx)} ${n(cy + r * 0.9)} Q${n(cx + r * 0.6)} ${n(cy - r * 0.4)} ${n(cx + r * 0.9)} ${n(cy - r * 1.7)} Q${n(cx + r * 2.2)} ${n(cy)} ${n(cx)} ${n(cy + r * 1.6)}Z`, metal.base, 'stroke-width=".7"'), front: at(cx - r * 1.3, cy + r * 0.6, path("M0 0 Q2 -1.8 4 0 Q2 1.8 0 0Z", "#7fbf5a", 'stroke-width=".5"'), -40) };
      case "hellmade": return { back: path(`M${n(cx - r * 0.8)} ${n(cy + r * 1.3)} Q${n(cx - r * 2.2)} ${n(cy)} ${n(cx - r * 1.4)} ${n(cy - r * 1.9)} Q${n(cx - r * 1.3)} ${n(cy - r * 0.4)} ${n(cx - r * 0.4)} ${n(cy + r * 0.6)}Z M${n(cx + r * 0.8)} ${n(cy + r * 1.3)} Q${n(cx + r * 2.2)} ${n(cy)} ${n(cx + r * 1.4)} ${n(cy - r * 1.9)} Q${n(cx + r * 1.3)} ${n(cy - r * 0.4)} ${n(cx + r * 0.4)} ${n(cy + r * 0.6)}Z`, metal.dark, 'stroke-width=".7"') + path(`M${n(cx - r * 1.1)} ${n(cy + r * 1.3)} h${n(r * 2.2)} v1.3 h${n(-r * 2.2)}Z`, "!" + trim, 'stroke-width=".5"'), front: gem(cx, cy + r * 1.95, 0.7, s.gem) };
      case "abyss": return { back: "", front: line(`M${n(cx - r * 1.3)} ${n(cy + r * 1.2)} L${n(cx - r * 0.2)} ${n(cy - r * 1.8)} M${n(cx + r * 1.3)} ${n(cy + r * 1.2)} L${n(cx + r * 0.2)} ${n(cy - r * 1.8)} M${n(cx - r * 1.5)} ${n(cy)} L${n(cx + r * 1.5)} ${n(cy - r * 0.2)}`, metal.dark, 1.1) + path(`M${n(cx - r * 1.4)} ${n(cy + r * 1.2)} L${n(cx + r * 1.4)} ${n(cy + r * 1.2)} L${n(cx)} ${n(cy + r * 2.4)}Z`, metal.dark, 'stroke-width=".6"') };
      case "vampiric": return { back: path(`M${n(cx - r * 1.4)} ${n(cy + r * 0.9)} L${n(cx - r * 1.5)} ${n(cy - r * 1.3)} L${n(cx - r * 0.8)} ${n(cy - r * 0.5)} L${n(cx)} ${n(cy - r * 1.9)} L${n(cx + r * 0.8)} ${n(cy - r * 0.5)} L${n(cx + r * 1.5)} ${n(cy - r * 1.3)} L${n(cx + r * 1.4)} ${n(cy + r * 0.9)}Z`, "!" + trim, 'stroke-width=".6"') + path(`M${n(cx - r * 1.4)} ${n(cy + r * 0.4)} Q${n(cx - r * 2.6)} ${n(cy - r * 0.2)} ${n(cx - r * 2.8)} ${n(cy + r * 1.2)} Q${n(cx - r * 2.1)} ${n(cy + r * 0.8)} ${n(cx - r * 1.6)} ${n(cy + r * 1.4)}Z M${n(cx + r * 1.4)} ${n(cy + r * 0.4)} Q${n(cx + r * 2.6)} ${n(cy - r * 0.2)} ${n(cx + r * 2.8)} ${n(cy + r * 1.2)} Q${n(cx + r * 2.1)} ${n(cy + r * 0.8)} ${n(cx + r * 1.6)} ${n(cy + r * 1.4)}Z`, s.wrap, 'stroke-width=".6"'), front: path(`M${n(cx - r * 1.2)} ${n(cy + r * 1.1)} h${n(r * 2.4)} v1.2 h${n(-r * 2.4)}Z`, "!" + trim, 'stroke-width=".5"') + dot(cx, cy + r * 1.7, 0.6, "!" + s.gem) };
      // The Institute of Magic: a plain cup and two prongs.
      default: return { back: path(`M${n(cx - r * 1.15)} ${n(cy + r * 0.3)} Q${n(cx)} ${n(cy + r * 2)} ${n(cx + r * 1.15)} ${n(cy + r * 0.3)} L${n(cx + r * 0.85)} ${n(cy + r * 0.15)} Q${n(cx)} ${n(cy + r * 1.35)} ${n(cx - r * 0.85)} ${n(cy + r * 0.15)}Z`, metal.base, 'stroke-width=".6"'), front: line(`M${n(cx - r * 1.05)} ${n(cy + r * 0.25)} L${n(cx - r * 0.7)} ${n(cy - r * 0.9)} M${n(cx + r * 1.05)} ${n(cy + r * 0.25)} L${n(cx + r * 0.7)} ${n(cy - r * 0.9)}`, metal.base, 0.9) };
    }
  }
  // Script along the handle, written in the inscription maker's hand and its ink.
  function inscription(maker, inkId, x, bottom, top) {
    const ink = INK_COLORS[inkId] || "#f0ece0", length = bottom - top;
    let d = "";
    if (maker === "church-of-talos") { d = `M${n(x)} ${n(bottom)}`; for (let y = bottom - 2, i = 0; y > top; y -= 2, i++) d += ` L${n(x + (i % 2 ? -0.9 : 0.9))} ${n(y)}`; }
    else if (maker === "elven") d = `M${n(x)} ${n(bottom)} Q${n(x + 1.4)} ${n(bottom - length * 0.25)} ${n(x)} ${n(bottom - length * 0.5)} T${n(x)} ${n(top)}`;
    else if (maker === "hellmade") for (let y = bottom - 1.5; y > top; y -= 3) d += `M${n(x - 0.8)} ${n(y)} L${n(x)} ${n(y - 1)} L${n(x + 0.8)} ${n(y)} L${n(x)} ${n(y + 1)}Z `;
    else if (maker === "vampiric") for (let y = bottom - 1; y > top + 1; y -= 3.4) d += `M${n(x - 0.8)} ${n(y)} q0.8 -1.7 1.6 0 q0.8 1.7 -0.4 -2.6 `;
    else if (maker === "hearthforge") for (let y = bottom - 1; y > top; y -= 2.4) d += `M${n(x - 0.8)} ${n(y - 0.8)} L${n(x)} ${n(y)} L${n(x + 0.8)} ${n(y - 0.8)} `;
    else for (let y = bottom - 1, i = 0; y > top; y -= 1.6, i++) d += `M${n(x - 0.9)} ${n(y)} h${n(i % 3 === 2 ? 1 : 1.8)} `;
    return `<g class="sp-glow" style="color:${ink}">${line(d, "!" + ink, 0.55)}</g>`;
  }
  function caster(forged, rnd, wand, beam = false) {
    const [main, handle, inscriptionPart] = forged.parts;
    const hp = pal(handle.material), metal = K.material(["hellmade", "vampiric"].includes(main.maker) ? "gold" : main.maker === "abyss" ? "blacksteel" : main.maker === "elven" ? "elvenwood" : "silver");
    const top = wand ? -15 : -44, bottom = wand ? 5 : 14, scale = wand ? 0.72 : 1;
    const r = [0, 2.6, 3.4, 4.4][main.tier] * scale, cy = top - r - (wand ? 1 : 2);
    const set = setting(main.maker, 0, cy, r, metal);
    // A placeholder beam (the Plasma Blade): a short red line from the gem.
    const ray = beam ? `<g class="sp-glow" style="color:#ff5a4a">${line(`M0 ${n(cy - r)} L0 ${n(cy - r - 16)}`, "!#ff3b30", 1.4)}</g>` : "";
    return haft(handle.maker, hp, top + 1, bottom, rnd, wand ? 2.4 : 3) + inscription(inscriptionPart.maker, inscriptionPart.material, 0, wand ? -1 : -6, top + (wand ? 3 : 8))
      + set.back + stone(main.material, main.tier, 0, cy, scale) + set.front + ray;
  }

  /* ---------- Assembly ---------- */
  const LAYOUT = {
    longsword: { family: "blade", blade: 28, width: 4.2, guard: 11, grip: 6 },
    dagger: { family: "blade", blade: 13, width: 3.4, guard: 7, grip: 5 },
    greatsword: { family: "blade", blade: 42, width: 6.2, guard: 16, grip: 11 },
    zweihander: { family: "blade", blade: 44, width: 5.4, guard: 19, grip: 15 },
    rapier: { family: "blade", blade: 33, width: 2.2, guard: 9, grip: 6 },
    axe: { family: "axe", shaft: 28, butt: 8 },
    greataxe: { family: "axe", shaft: 42, butt: 10, big: true },
    mace: { family: "blunt", style: "mace", shaft: 22, butt: 7 },
    greathammer: { family: "blunt", style: "hammer", shaft: 40, butt: 10, big: true },
    greatclub: { family: "blunt", style: "knob", shaft: 18, butt: 6, big: true },
    flail: { family: "blunt", style: "flail", shaft: 12, butt: 5 },
    spear: { family: "spear", shaft: 44, butt: 12 },
    lance: { family: "spear", shaft: 50, butt: 10, lance: true },
    quarterstaff: { family: "staff", shaft: 46, butt: 20 },
    club: { family: "club" },
    shortbow: { family: "bow", half: 20, belly: 6.5 },
    longbow: { family: "bow", half: 27, belly: 7 },
    warbow: { family: "bow", half: 28, belly: 8, heavy: true },
    ballistabow: { family: "bow", half: 30, belly: 9, heavy: true },
    compoundbow: { family: "bow", half: 21, belly: 6, compound: true },
    crossbow: { family: "crossbow" },
    staff: { family: "caster" },
    wand: { family: "caster", wand: true },
  };
  // Masterworks keep their type's family but have shapes of their own: the Stunted Sword's
  // blade is short and broad, Bloody Mary wears a crossguard under its spearhead.
  const MASTERWORK_LAYOUT = {
    "stunted-sword": { family: "blade", blade: 19, width: 5.2, guard: 11, grip: 6 },
    "bloody-mary": { family: "spear", shaft: 50, butt: 12, guardUnderHead: 13 },
    // Ornn's Anvil Hammer: a portable anvil on a stout handle.
    "anvil-hammer": { family: "blunt", style: "anvil", shaft: 16, butt: 5 },
    // Anatemis's Quasar Jet: a longsword with no crossguard, a heavy pommel at the end of its grip.
    "quasar-jet": { family: "blade", blade: 28, width: 4.2, guard: 11, grip: 6, pommel: true },
    // PLACEHOLDER ART (the Masterwork Elementals, 2026-11-05): simple hints only; the user wants these
    // fully remade in the port (engine/Docs/Port-Tasks/README.md). Plasma Blade's design: a red laser
    // beam from its gem, like a laser sword (here a short red line). Tidepiercer's: a lance shaped like a
    // trident with a crossguard instead of a pommel (here two side prongs and Bloody Mary's crossguard).
    "plasma-blade": { family: "caster", wand: true, beam: true },
    tidepiercer: { family: "spear", shaft: 50, butt: 10, lance: true, guardUnderHead: 13, prongs: true },
  };
  const layoutOf = forged => (forged.masterwork && MASTERWORK_LAYOUT[forged.masterwork]) || LAYOUT[forged.type] || LAYOUT.longsword;
  // The sprite kind each weapon type is held like.
  const SPRITE_KIND = { longsword: "sword", dagger: "dagger", greatsword: "greatsword", zweihander: "greatsword", rapier: "rapier", axe: "axe", greataxe: "greataxe", mace: "mace", greathammer: "hammer", greatclub: "mace", flail: "mace", spear: "spear", lance: "spear", quarterstaff: "staff", club: "club", shortbow: "bow", longbow: "longbow", warbow: "greatbow", ballistabow: "greatbow", compoundbow: "bow", crossbow: "crossbow", staff: "staff", wand: "wand" };

  function drawForged(forged, id = "") {
    const layout = layoutOf(forged);
    const parts = forged.parts, palettes = parts.map(part => part && !STONE_PALETTES[part.material] ? pal(part.material) : null);
    const [main, guard, third] = parts, [mp, gp, tp] = palettes;
    const rnd = K.seeded(id || JSON.stringify(parts));
    let runeMarks = "";
    if (forged.runes?.length) runeMarks = `<g class="sp-glow" style="color:#9fd8ff">${forged.runes.map((_, i) => dot(0.9, -4 - i * 2.4, 0.55, "!#bfe8ff")).join("")}</g>`;
    switch (layout.family) {
      case "blade": {
        const shape = bladePath(main.maker, layout.blade, layout.width, rnd);
        const blade = glowWrap(mp, path(shape.d, mp.base) + (shape.edge ? line(shape.edge, mp.matte ? mp.base : mp.light, 0.9) : "") + bladeDetails(main.maker, mp, layout.blade, layout.width) + markings(mp, 0, -5, -layout.blade + 4, shape.width));
        const cup = forged.type === "rapier" ? path("M-1 -2 Q-8 -1 -6 5 Q-4 0 0 0Z M1 -2 Q9 -6 7 -12 Q4 -4 0 -3Z", gp.base, thin) : "";
        // A pommel-hilted blade (Quasar Jet) has no crossguard: its second part caps the grip instead.
        if (layout.pommel) return grip(third.maker, tp, layout.grip, rnd, false) + blade + pommelCap(guard.maker, gp, layout.grip + 1.8, rnd) + runeMarks;
        return grip(third.maker, tp, layout.grip, rnd) + blade + crossguard(guard.maker, gp, layout.guard, rnd) + cup + runeMarks;
      }
      case "axe": {
        const top = -layout.shaft;
        return haft(third.maker, tp, top, layout.butt, rnd, layout.big ? 3.6 : 3.2) + pommelCap(guard.maker, gp, layout.butt + 1.5, rnd) + axeHead(main.maker, mp, top, layout.big, rnd) + runeMarks;
      }
      case "blunt": {
        const top = -layout.shaft;
        if (layout.style === "flail") {
          const chain = Array.from({ length: 4 }, (_, i) => ellipse(1.6 + i * 1.5, top - 2 - i * 2.4, 1, 1.4, K.material("iron").base, 'stroke-width=".6"')).join("");
          return haft(third.maker, tp, top, layout.butt, rnd, 3) + pommelCap(guard.maker, gp, layout.butt + 1.4, rnd, true) + chain + `<g transform="translate(6 -3)">${bluntHead(main.maker, mp, top - 9, "flail", false, rnd)}</g>` + runeMarks;
        }
        return haft(third.maker, tp, top, layout.butt, rnd, layout.big ? 3.8 : 3.2) + pommelCap(guard.maker, gp, layout.butt + 1.5, rnd) + bluntHead(main.maker, mp, top, layout.style, layout.big, rnd) + runeMarks;
      }
      case "spear": {
        const top = -layout.shaft;
        // Placeholder prongs (Tidepiercer): two side tines beside the head, a trident's hint.
        const prongs = layout.prongs ? [-1, 1].map(side => line(`M${n(side * 2.4)} ${n(top - 1)} Q${n(side * 6.2)} ${n(top - 3)} ${n(side * 6)} ${n(top - 13)}`, mp.base, 1.7)).join("") : "";
        if (layout.guardUnderHead) return haft(third.maker, tp, top, layout.butt, rnd, 2.8) + pommelCap(third.maker, tp, layout.butt + 1.2, rnd, true) + prongs + spearhead(main.maker, mp, top, Boolean(layout.lance)) + `<g transform="translate(0 ${n(top + 4)})">${crossguard(guard.maker, gp, layout.guardUnderHead, rnd)}</g>` + runeMarks;
        return haft(third.maker, tp, top, layout.butt, rnd, 2.6) + pommelCap(guard.maker, gp, layout.butt + 1.2, rnd, true) + spearhead(main.maker, mp, top, layout.lance) + runeMarks;
      }
      case "staff": {
        const top = -layout.shaft, bottom = layout.butt;
        const shaft = haft(main.maker, mp, top, bottom, rnd, 3);
        const metal = gp.kind === "wood" || gp.kind === "thread" ? K.material("iron") : gp;
        const caps = [top + 2, bottom - 2].map(y => path(`M-2.1 ${n(y - 2.6)} h4.2 v5.2 h-4.2Z`, metal.base, thin)).join("") + path(`M-2 -3 h4 v6 h-4Z`, STYLE[guard.maker].wrap, 'stroke-width=".7"');
        return shaft + caps + (guard.maker === "hellmade" ? gem(0, top + 2, 0.9, STYLE.hellmade.gem) : "") + runeMarks;
      }
      case "club": {
        // Outlaw clubs: a knotted length of wood or scrap on a rag-wrapped grip.
        const body = path("M-1.8 -1 L-2.8 -12 Q-6 -26 -1 -31 Q5 -33 5.5 -25 Q5 -16 1.8 -12 L1.8 -1Z", mp.base) + line("M0 -14 l2 -6 M-2.5 -22 l1.5 2 M2 -27 l-1 2", darken(mp.base, 0.35), 1);
        const nails = mp.kind === "metal" ? [[-2, -24], [2.6, -27], [3, -19]].map(([x, y]) => line(`M${x} ${y} l${x > 0 ? 2 : -2} -1`, "#6a6e74", 1)).join("") : [[-2.6, -22], [3.6, -25]].map(([x, y]) => line(`M${x} ${y} l${x > 0 ? 2 : -2} -1`, "#6a6e74", 1)).join("");
        return grip(third.maker, tp, 6, rnd, false) + glowWrap(mp, body) + nails + pommelCap(guard.maker, gp, 7.6, rnd, true) + runeMarks;
      }
      case "bow": return bowLimbs(main.maker, mp, layout.half, layout.belly, layout.heavy, layout.compound) + bowString(tp, layout.half, ["warbow", "ballistabow", "crossbow", "compoundbow"].includes(forged.type)) + bowGrip(guard.maker, gp, layout.belly) + runeMarks;
      case "crossbow": return crossbowParts(parts, palettes, rnd) + runeMarks;
      case "caster": return caster(forged, rnd, layout.wand, layout.beam);
      default: return "";
    }
  }

  // Battle sprites: forged visuals carry their spec and the kind they are held like.
  // Masterworks whose hand-made battle designs predate their forged recipes.
  const SPRITE_UNIQUE = { "claw-of-beowulf": "beowulf", "stormhide-cuirass": "stormhide", "godforged-plate": "godforged", "soulbound-barrier": "soulbound" };
  const spriteUnique = key => key ? SPRITE_UNIQUE[key] || key : null;
  function visualFor(item) {
    if (!item?.forged) return null;
    const ornate = { common: 0, uncommon: 1, rare: 2, epic: 3, legendary: 4, masterwork: 5 }[item.rarity] ?? 0;
    if (item.forged.armor) return armorVisual(item, ornate);
    // A masterwork's key lets the battle play its signature hit effect.
    return { slot: "weapon", id: item.id, name: item.name, kind: SPRITE_KIND[item.forged.type] || "sword", handed: Number(item.handed) === 2 ? 2 : 1, material: "iron", ornate, unique: spriteUnique(item.forged.masterwork), rarity: item.rarity, forged: item.forged };
  }
  const spriteCache = new Map();
  function sprite(visual) {
    if (spriteCache.has(visual.id)) return spriteCache.get(visual.id);
    const markup = K.withPen(1.2, () => drawForged(visual.forged, visual.id));
    if (spriteCache.size > 300) spriteCache.clear();
    spriteCache.set(visual.id, markup);
    return markup;
  }

  // Extents of each weapon family in the sprite frame, along the weapon and across it.
  const ACROSS = { caster: l => l.wand ? [-5, 5] : [-8, 8], blade: l => [-(l.guard / 2 + 2.5), l.guard / 2 + 2.5], axe: l => l.big ? [-13, 21] : [-10, 15], blunt: l => l.style === "anvil" ? [-13, 26] : l.style === "hammer" ? [-12, 12] : l.style === "flail" ? [-3, 17] : [-11, 11], spear: l => l.guardUnderHead ? [-l.guardUnderHead / 2 - 2, l.guardUnderHead / 2 + 2] : l.lance ? [-6.5, 6.5] : [-5.5, 5.5], staff: () => [-3.5, 3.5], club: () => [-7, 7], bow: l => [-3, l.belly * 1.9 + 3], crossbow: () => [-15.5, 15.5] };
  const EXTENT = { caster: l => l.wand ? [-25, 7] : [-58, 16], blade: l => [-l.blade - 5, l.grip + 4.5], axe: l => [-l.shaft - 6, l.butt + 4], blunt: l => [-l.shaft - (l.style === "anvil" ? 21 : l.style === "hammer" ? 15 : l.style === "flail" ? 24 : 16), l.butt + 4], spear: l => [-l.shaft - (l.lance ? 20 : 16), l.butt + 4], staff: l => [-l.shaft - 3, l.butt + 1], club: () => [-34, 12], bow: l => [-l.half - 4, l.half + 4], crossbow: () => [-41, 10] };
  // A large drawing of the weapon lying across the panel, tip to the right.
  function weapon(item, size = "large") {
    if (!item?.forged) return "";
    if (item.forged.armor) return armorPortrait(item, size);
    const layout = layoutOf(item.forged);
    const [low, high] = EXTENT[layout.family](layout);
    const bow = layout.family === "bow";
    const markup = K.withPen(size === "small" ? 1.1 : 0.8, () => drawForged(item.forged, item.id));
    const pad = 3, span = high - low, [across0, across1] = ACROSS[layout.family](layout);
    const cls = `forged-weapon-art${size === "small" ? " is-small" : ""}${bow ? " is-upright" : ""}`;
    // Bows stand upright; everything else is turned to point right.
    if (bow) return `<svg class="${cls}" viewBox="${n(across0 - pad)} ${n(low - pad)} ${n(across1 - across0 + pad * 2)} ${n(span + pad * 2)}" preserveAspectRatio="xMidYMid meet" aria-hidden="true">${markup}</svg>`;
    return `<svg class="${cls}" viewBox="${n(-high - pad)} ${n(across0 - pad)} ${n(span + pad * 2)} ${n(across1 - across0 + pad * 2)}" preserveAspectRatio="xMidYMid meet" aria-hidden="true"><g transform="rotate(90)">${markup}</g></svg>`;
  }

  /* ---------- Manufacturer marks ---------- */
  const LOGOS = {
    roundtable: `<circle cx="16" cy="16" r="13" fill="#2b3f66" stroke="#c9a24a" stroke-width="1.6"/><circle cx="16" cy="16" r="7.5" fill="#5a3a26" stroke="#c9a24a" stroke-width="1.2"/>${Array.from({ length: 12 }, (_, i) => { const a = i * Math.PI / 6; return `<rect x="-1" y="-1.4" width="2" height="2.8" rx=".4" fill="#c9a24a" transform="translate(${n(16 + Math.cos(a) * 10.2)} ${n(16 + Math.sin(a) * 10.2)}) rotate(${n(i * 30)})"/>`; }).join("")}<path d="M16 3.5 L17.2 6 V20.5 H19.6 V22 H17.2 V25.5 H14.8 V22 H12.4 V20.5 H14.8 V6Z" fill="#e8ecef" stroke="#1a1a22" stroke-width=".7"/>`,
    dwarven: `<path d="M16 2.5 L28 9.5 V22.5 L16 29.5 L4 22.5 V9.5Z" fill="#3a2a1e" stroke="#b07a3a" stroke-width="1.6"/><path d="M6 19 L11 11 L14 15 L18 8 L26 19Z" fill="#6b6456" stroke="#b07a3a" stroke-width=".8"/><path d="M9.5 20.5 H22.5 L21 22.4 H18 L18.6 24.6 H13.4 L14 22.4 H11Z" fill="#c9c2b0" stroke="#1a1410" stroke-width=".7"/>`,
    elven: `<path d="M16 2.5 C25 6 28.5 15 26 22 C23 28 9 28 6 22 C3.5 15 7 6 16 2.5Z" fill="#2e3f22" stroke="#d8c46a" stroke-width="1.4"/><path d="M16 6 C22 10 23 18 16 26 C9 18 10 10 16 6Z" fill="#7fae5a" stroke="#1f2a16" stroke-width=".7"/><path d="M16 8 V24 M16 13 L19.4 10.6 M16 17 L20 14 M16 13 L12.6 10.6 M16 17 L12 14" stroke="#2e3f22" stroke-width=".8" fill="none"/>`,
    "boreal-bolt": `<rect x="3" y="3" width="26" height="26" rx="3" fill="#1c2430" stroke="#b4bcc4" stroke-width="1.4"/><path d="M16 5 V25 M16 25 L13.6 21.6 M16 25 L18.4 21.6 M13.2 7.8 L16 5 L18.8 7.8" stroke="#dfe6ec" stroke-width="1.6" fill="none" stroke-linecap="round" stroke-linejoin="round"/><path d="M9.5 13 H22.5" stroke="#dfe6ec" stroke-width="1.6" stroke-linecap="round"/><circle cx="16" cy="13" r="2.2" fill="#1c2430" stroke="#dfe6ec" stroke-width="1"/>`,
    abyss: `<path d="M16 1.5 L19 7 L25.5 4.5 L24 11 L30.5 13 L25 17 L29 22.5 L22 22 L21 29 L16 24.5 L11 29 L10 22 L3 22.5 L7 17 L1.5 13 L8 11 L6.5 4.5 L13 7Z" fill="#1c1226" stroke="#7a4aa8" stroke-width="1.2"/><path d="M8.5 16 Q16 8.5 23.5 16 Q16 23.5 8.5 16Z" fill="#3a2250" stroke="#a882ff" stroke-width=".9"/><path d="M16 11.5 L17.4 16 L16 20.5 L14.6 16Z" fill="#cfa8ff"/>`,
    hellmade: `<path d="M16 3 C20 3 24 5 26 9 C29 7 30 4 29.5 2 C32 6 31 11 27.5 13 V20 C27.5 25 22 29.5 16 30 C10 29.5 4.5 25 4.5 20 V13 C1 11 0 6 2.5 2 C2 4 3 7 6 9 C8 5 12 3 16 3Z" fill="#3a1416" stroke="#e8b84a" stroke-width="1.3"/><path d="M16 9 C19 12 21 15 18.6 19 C21 18 21.6 16 21.2 14.6 C23.6 18 22 23 16 25 C10 23 8.4 18 10.8 14.6 C10.4 16 11 18 13.4 19 C11 15 13 12 16 9Z" fill="#c0283a" stroke="#e8b84a" stroke-width=".8"/><circle cx="16" cy="20" r="1.8" fill="#ffd27a"/>`,
    hearthforge: `<path d="M5 29 V14 C5 7.5 10 3 16 3 C22 3 27 7.5 27 14 V29Z" fill="#2a1a12" stroke="#c27a47" stroke-width="1.5"/><path d="M8.5 29 V15 C8.5 10.5 12 7 16 7 C20 7 23.5 10.5 23.5 15 V29Z" fill="#120a06"/><path d="M16 11 C19.5 15 21 18.5 19 22.6 C21.6 21.6 21.4 19 21 18 C23.4 21.6 21.4 27 16 27.5 C10.6 27 8.6 21.6 11 18 C10.6 19 10.4 21.6 13 22.6 C11 18.5 12.5 15 16 11Z" fill="#ff8a3a"/><path d="M16 17 C17.6 19 18 21 16 24 C14 21 14.4 19 16 17Z" fill="#ffe0a0"/><rect x="9" y="26.6" width="14" height="2.4" fill="#6b4a32" stroke="#c27a47" stroke-width=".6"/>`,
    outlaw: `<path d="M3.5 5 L28 3.5 L29 27.5 L4.5 29Z" fill="#2a2018" stroke="#a08a5a" stroke-width="1.3" stroke-dasharray="5 1.5"/><path d="M8 7.5 L24 24.5 M24 7.5 L8 24.5" stroke="#c8b48c" stroke-width="2.4" stroke-linecap="round"/><path d="M8 7.5 L11.5 9.5 L10 11 Z M24 7.5 L20.5 9.5 L22 11Z" fill="#c8b48c"/><circle cx="16" cy="16" r="3.4" fill="#2a2018" stroke="#8a4a2a" stroke-width="1.2"/><path d="M14.6 15.2 h.1 M17.3 15.2 h.1 M14.8 17.6 L17.2 17.6" stroke="#c8b48c" stroke-width="1" stroke-linecap="round"/>`,
  };
  Object.assign(LOGOS, {
    "institute-of-magic": `<rect x="3" y="3" width="26" height="26" rx="2" fill="#1e2636" stroke="#9aa8bc" stroke-width="1.4"/><path d="M5.5 23 C9 21 13 21 16 23 C19 21 23 21 26.5 23 V12 C23 10 19 10 16 12 C13 10 9 10 5.5 12Z" fill="#e8e2d0" stroke="#1a1e28" stroke-width=".8"/><path d="M16 12 V23" stroke="#1a1e28" stroke-width=".8"/><path d="M8 14.5 h5 M8 16.5 h5 M8 18.5 h4 M19 14.5 h5 M19 16.5 h5 M19 18.5 h3" stroke="#6a7488" stroke-width=".7"/><path d="${star(16, 7, 3.4, 1.2, 4)}" fill="#cfe0ff" stroke="#1a1e28" stroke-width=".5"/>`,
    "church-of-talos": `<circle cx="16" cy="16" r="13" fill="#202838" stroke="#e8cf6a" stroke-width="1.4"/>${Array.from({ length: 12 }, (_, i) => { const a = i * Math.PI / 6; return `<path d="M${n(16 + Math.cos(a) * 13)} ${n(16 + Math.sin(a) * 13)} L${n(16 + Math.cos(a + 0.26) * 10.6)} ${n(16 + Math.sin(a + 0.26) * 10.6)}" stroke="#e8cf6a" stroke-width="1"/>`; }).join("")}<path d="M18.5 4.5 L10.5 17.2 H15.4 L12.6 27.5 L22 13.4 H16.8 L20.6 4.5Z" fill="#ffe27a" stroke="#1a1a22" stroke-width=".8" stroke-linejoin="round"/>`,
    vampiric: `<path d="M16 29.5 C9 26 5 21 5 14 V6.5 L16 3 L27 6.5 V14 C27 21 23 26 16 29.5Z" fill="#2a0a12" stroke="#d8ae4a" stroke-width="1.3"/><path d="M9.5 9 L11.5 12.5 L13.6 9.4 L16 13 L18.4 9.4 L20.5 12.5 L22.5 9 L21.6 14.6 H10.4Z" fill="#d8ae4a" stroke="#1a0608" stroke-width=".6"/><path d="M16 16.5 C13.5 14.6 9.5 15.2 7.6 17.6 C10 17.4 11 18.6 11.6 20 C12.6 18.8 14.4 18.8 16 20.4 C17.6 18.8 19.4 18.8 20.4 20 C21 18.6 22 17.4 24.4 17.6 C22.5 15.2 18.5 14.6 16 16.5Z" fill="#a0101a" stroke="#1a0608" stroke-width=".6"/><path d="M16 21 C17.6 23.2 17.8 24.6 16 26 C14.2 24.6 14.4 23.2 16 21Z" fill="#e0303a"/>`,
  });
  /* ---------- Armor ----------
     Body armor stands upright, seen from the front: the main part (plates, shell or
     weave) on the outside, the lining showing at the collar, sleeves and hem, and the
     fittings or trim holding it together. Shields show their face, rim and grip. */
  Object.assign(PALETTES, {
    silk: M("thread", "#b8404f", "#f08a96", "#6a1a26"),
    velvet: M("thread", "#5a2a6a", "#9a62aa", "#2e1238"),
    rawhide: M("leather", "#b08a5e", "#d4b48a", "#6e5232"),
    "boiled-leather": M("leather", "#6b4228", "#93613d", "#3e2615"),
    scalehide: M("leather", "#4f7a6a", "#8ab4a0", "#2a463c", { pattern: "scales" }),
    wolfhide: M("leather", "#7a7068", "#b0a69c", "#4a423c", { pattern: "fur" }),
    "wyvern-hide": M("leather", "#6a7a4a", "#a0b07a", "#3a4628", { pattern: "scales" }),
    shadowhide: M("leather", "#2e2a36", "#5a5468", "#141218"),
    drakehide: M("leather", "#8a3a22", "#c8684a", "#4a1a10", { pattern: "scales" }),
    hellhide: M("leather", "#5a1e1e", "#8a3a2e", "#2f0f0f", { pattern: "cracks", vein: "#ff6a3d" }),
    dragonhide: M("leather", "#a63b2a", "#e0694a", "#5f1d13", { pattern: "scales", glow: "#ff8a5a" }),
  });
  const ARMOR_SHAPE = { "full-plate": "plate", "splint-mail": "splint", chainmail: "chain", "scale-armor": "scale", brigandine: "brigandine", "leather-armor": "leather", robe: "robe", coat: "coat", garb: "garb", buckler: "buckler", "kite-shield": "kite", "tower-shield": "tower" };
  const sym = content => content + `<g transform="scale(-1 1)">${content}</g>`;
  const shade = shape => K.clipped(shape, `<rect x="3" y="-30" width="22" height="62" fill="${K.INK}" opacity=".2"/><rect x="-25" y="-30" width="22" height="62" fill="#ffffff" opacity=".05"/>`);
  // Mail rings, scales and fur clipped to a shape.
  function texture(shape, style, p) {
    let d = "";
    if (style === "chain") for (let row = 0; row < 22; row++) for (let col = 0; col < 18; col++) d += `M${n(-23 + col * 2.6 - (row % 2) * 1.3)} ${n(-22 + row * 2.2)} q1.3 1.4 2.6 0 `;
    else if (style === "scale") for (let row = 0; row < 17; row++) for (let col = 0; col < 15; col++) d += `M${n(-23 + col * 3.2 - (row % 2) * 1.6)} ${n(-21 + row * 2.8)} q1.6 2.6 3.2 0 `;
    else if (style === "fur") for (let row = 0; row < 9; row++) for (let col = 0; col < 8; col++) d += `M${n(-20 + col * 5.4 + (row % 2) * 2.7)} ${n(-18 + row * 5)} l-0.8 2.2 `;
    else if (style === "planks") for (let x = -12; x <= 12; x += 4.8) d += `M${n(x)} -30 V32 `;
    else if (style === "quilt") for (let y = -16; y < 30; y += 3.6) d += `M-24 ${n(y)} H24 `;
    return d ? K.clipped(shape, line(d, p.dark, style === "planks" ? 0.8 : 0.55, 'opacity=".8"')) : "";
  }
  // What a material looks like across a broad surface.
  function surface(shape, p) {
    if (p.pattern === "scales") return texture(shape, "scale", p);
    if (p.pattern === "fur") return texture(shape, "fur", p);
    if (p.kind === "wood") return texture(shape, "planks", p);
    const marks = markings(p, -5, 8, -12, 7) + markings(p, 5, 6, -10, 6);
    return marks ? K.clipped(shape, glowWrap(p, marks)) : "";
  }
  const belt = (y, width, p, buckle) => path(`M${n(-width)} ${n(y)} H${n(width)} V${n(y + 2.4)} H${n(-width)}Z`, p.base, 'stroke-width=".7"') + path(`M-1.5 ${n(y - 0.4)} h3 v3.2 h-3Z`, buckle, 'stroke-width=".6"');
  const runeDots = (count, x, y) => count ? `<g class="sp-glow" style="color:#9fd8ff">${Array.from({ length: count }, (_, i) => path(`M${n(x + i * 3)} ${n(y - 1.2)} l1 1.2 l-1 1.2 l-1 -1.2Z`, "!#bfe8ff", 'stroke-width=".35"')).join("")}</g>` : "";
  // Each maker leaves its mark on the outside of the piece.
  function armorDecor(maker, shape, mp, hem, width) {
    const s = STYLE[maker] || STYLE.outlaw, clothing = ["robe", "coat", "garb"].includes(shape);
    switch (maker) {
      case "roundtable": return line("M-5 -18.4 Q0 -14.9 5 -18.4", s.trim, 1.1) + path("M-0.7 -11 h1.4 v2.4 h2.2 v1.4 h-2.2 v4.2 h-1.4 v-4.2 h-2.2 v-1.4 h2.2Z", s.trim, 'stroke-width=".5"');
      case "dwarven": return Array.from({ length: Math.floor(width * 2 / 3) + 1 }, (_, i) => dot(-width + 1 + i * 3, hem - 1.4, 0.55, "#c99f42")).join("") + line("M-3 -7 l1.5 -2 l1.5 2 l1.5 -2 l1.5 2", "#c99f42", 0.8);
      case "elven": return line(`M-10 ${clothing ? 1 : -2} Q-4 -8 0 -5 Q4 -2 10 -8`, s.trim, 0.8) + at(-5, -6.5, path("M0 0 Q2.2 -1.8 4.4 0 Q2.2 1.8 0 0Z", "#7fbf5a", 'stroke-width=".5"'), -40) + at(5.5, -4, path("M0 0 Q2.2 -1.8 4.4 0 Q2.2 1.8 0 0Z", "#7fbf5a", 'stroke-width=".5"'), -150);
      // Spikes sit on the pauldrons, or lower on the sloping shoulders of mail and leather.
      case "abyss": return `<g transform="translate(${shape === "plate" ? "0 0" : "2 3.2"})">${sym(path("M-16 -18.5 L-18 -25 L-14 -19.4Z M-19.6 -15.4 L-24.4 -18.6 L-20.2 -13.2Z", mp.dark, 'stroke-width=".6"'))}</g>` + `<g class="sp-glow" style="color:#a882ff">${path("M-3 -7 Q0 -10 3 -7 Q0 -4 -3 -7Z", "!#3a2250", 'stroke-width=".5"') + dot(0, -7, 0.8, "!#cfa8ff")}</g>`;
      case "hellmade": return line("M-7 -10 q3.5 -3 7 0 q3.5 3 7 0", s.trim, 0.8) + line(`M-${n(width - 1)} ${n(hem - 1.6)} H${n(width - 1)}`, s.trim, 0.8) + gem(0, -6, 1.3, s.gem);
      case "hearthforge": return line("M-11 -3.2 Q0 -1.4 11 -3.2 M-11 -1.2 Q0 0.6 11 -1.2", s.trim, 0.7, 'opacity=".9"') + dot(0, -8, 0.9, "!#ff8a3a");
      case "outlaw": return path("M4.6 -9.6 l4.6 -0.8 l0.8 4.6 l-4.6 0.7Z", "#8a7a5a", 'stroke-width=".6"') + line("M4.9 -8.8 l-1 -0.6 M9.2 -10 l0.9 -0.9 M5.4 -5 l-1 0.7 M10 -5.6 l1 0.4", "#c8b48c", 0.6);
      case "institute-of-magic": return `<g class="sp-glow" style="color:#cfe0ff">${path(star(0, -8, 2.8, 1, 4), "!#cfe0ff", 'stroke-width=".4"')}</g>`;
      case "church-of-talos": return line("M-5 -18.4 Q0 -14.9 5 -18.4", s.trim, 0.9) + `<g class="sp-glow" style="color:#ffe27a">${path("M1.4 -12.4 L-1.8 -7.4 H0.4 L-0.8 -3 L2.6 -8.4 H0.4 L1.6 -12.4Z", "!#ffe27a", 'stroke-width=".4"')}</g>`;
      case "vampiric": return sym(path("M-4.6 -18.6 L-9 -25 L-7.4 -18.4Z", "#2a0a12", 'stroke-width=".6"')) + line("M-5 -18.4 Q0 -14.9 5 -18.4", s.trim, 0.8) + gem(0, -14.6, 1.1, s.gem);
      default: return "";
    }
  }
  // Rootbark is living bark whatever its parts are made of.
  const ROOTBARK = { base: "#6b4527", light: "#9a6b42", dark: "#3e2615", kind: "leather" };
  function bodyArmor(forged, shape) {
    const [main, lining, third] = forged.parts, mp = forged.masterwork === "rootbark" ? ROOTBARK : pal(main.material), lp = pal(lining.material), tp = pal(third.material), s = STYLE[main.maker] || STYLE.outlaw;
    const buckle = tp.kind === "metal" ? tp.light : "#c9a24a";
    const runes = runeDots(forged.runes?.length || 0, -8, -9);
    const neck = "M-13 -17 Q-8 -20.5 -5 -18.5 Q0 -15 5 -18.5 Q8 -20.5 13 -17";
    switch (shape) {
      case "plate": {
        const chest = neck + " L14 -5 Q12.5 5 10.5 11 L-10.5 11 Q-12.5 5 -14 -5Z";
        const under = sym(path("M-13 -15 L-20.5 5 L-16 7 L-12.5 -3Z", lp.base)) + path("M-12 9 L12 9 L13.5 23 Q0 25 -13.5 23Z", lp.base) + line("M-6 12 L-6.6 23.6 M0 12 V24.6 M6 12 L6.6 23.6", lp.dark, 0.6);
        const faulds = [2, 1, 0].map(i => path(`M${n(-10.5 - i * 0.7)} ${n(10.5 + i * 3.2)} L${n(10.5 + i * 0.7)} ${n(10.5 + i * 3.2)} L${n(11 + i * 0.7)} ${n(13.9 + i * 3.2)} Q0 ${n(15 + i * 3.2)} ${n(-11 - i * 0.7)} ${n(13.9 + i * 3.2)}Z`, i % 2 ? darken(mp.base, 0.1) : mp.base, 'stroke-width=".8"')).join("");
        const arms = sym(path("M-19.4 -2 L-21.6 5.6 L-17 7.4 L-15.4 0Z", mp.base, 'stroke-width=".8"'));
        const pauldrons = sym(path("M-11.5 -18.6 Q-19.6 -20.6 -21.6 -11 Q-19 -8.4 -15 -9 Q-12.4 -11 -11.5 -13Z", mp.base) + line("M-20.8 -13.4 Q-16.6 -12.4 -12.6 -15", mp.dark, 0.7) + line("M-14 -18.8 Q-18.6 -18.6 -20.3 -14.6", mp.light, 0.7, 'opacity=".7"') + (tp.kind === "metal" ? dot(-14.4, -16.2, 0.55, tp.light) + dot(-18.2, -15.4, 0.55, tp.light) : line("M-13.4 -11.6 L-15.4 -18.4", tp.base, 1.2)));
        const gorget = path("M-5.5 -18.6 Q0 -14.8 5.5 -18.6 L5 -21.2 Q0 -18 -5 -21.2Z", mp.base, 'stroke-width=".8"');
        return under + faulds + arms + path(chest, mp.base) + surface(chest, mp) + shade(chest) + line("M0 -15 V9.6", mp.light, 0.9, 'opacity=".75"') + line("M-10.4 3 Q0 6.6 10.4 3", mp.dark, 0.8)
          + belt(8.2, 11, tp, buckle) + pauldrons + gorget + armorDecor(main.maker, shape, mp, 22, 12) + runes;
      }
      case "splint": {
        const tunic = neck + " L14 -5 L13.4 22 Q0 24 -13.4 22 L-14 -5Z";
        const sleeves = sym(path("M-13 -15 L-20.4 4 L-16 6 L-12.5 -3Z", lp.base));
        const splints = [-11.4, -8.2, -5, -1.8, 1.4, 4.6, 7.8].map(x => {
          const top = -16 + Math.max(0, 5.5 - Math.abs(x + 0.2)) * 0.55;
          return path(`M${n(x)} ${n(top)} h2.9 V7.6 h-2.9Z`, mp.base, 'stroke-width=".7"') + (tp.kind === "metal" ? dot(x + 1.45, top + 1.2, 0.45, tp.light) + dot(x + 1.45, 6.4, 0.45, tp.light) : "");
        }).join("");
        const skirt = [-12.2, -9, -5.8, -2.6, 0.6, 3.8, 7, 10.2].map(x => path(`M${n(x)} 11 h2.9 V${n(20.6 - Math.abs(x + 0.8) * 0.12)} h-2.9Z`, darken(mp.base, 0.08), 'stroke-width=".7"')).join("");
        const pauldrons = sym(path("M-12 -18 Q-18.6 -19.6 -20.2 -12 L-15 -11 Q-13 -13 -12 -15Z", mp.base) + line("M-19.6 -14.8 L-14.2 -13.4", mp.dark, 0.7));
        return sleeves + path(tunic, lp.base) + texture(tunic, "quilt", lp) + shade(tunic) + splints + skirt + belt(8.4, 13.4, tp, buckle) + pauldrons + armorDecor(main.maker, shape, mp, 21, 12) + runes;
      }
      case "chain": {
        const hauberk = neck + " L20.6 -4 L16 -1.4 L12.8 -8 L12.6 6 L14 23 Q0 25.5 -14 23 L-12.6 6 L-12.8 -8 L-16 -1.4 L-20.6 -4Z";
        const edges = sym(path("M-20.6 -4 L-16 -1.4 L-15.2 0.8 L-19.8 -1.6Z", lp.base, 'stroke-width=".7"')) + path("M-14 23 Q0 25.5 14 23 L14.3 25.4 Q0 27.8 -14.3 25.4Z", lp.base, 'stroke-width=".7"');
        return edges + path(hauberk, mp.base) + texture(hauberk, "chain", mp) + shade(hauberk) + path("M-5.6 -18.4 Q0 -14.4 5.6 -18.4 L6 -20.8 Q0 -17 -6 -20.8Z", lp.base, 'stroke-width=".7"')
          + belt(5.6, 12.8, tp, buckle) + armorDecor(main.maker, shape, mp, 23, 13) + runes;
      }
      case "scale": case "brigandine": case "leather": {
        const vest = "M-12.5 -17 Q-8 -20 -5 -18.5 Q0 -15 5 -18.5 Q8 -20 12.5 -17 L15.6 -12 L13 -7 L13 6 L14 " + (shape === "leather" ? 14 : 17) + " Q0 " + (shape === "leather" ? 16 : 19) + " -14 " + (shape === "leather" ? 14 : 17) + " L-13 6 L-13 -7 L-15.6 -12Z";
        const hem = shape === "leather" ? 15 : 18;
        const under = sym(path("M-14.4 -14 L-20.2 3.4 L-16 5.4 L-13 -6Z", lp.base)) + path(`M-13.4 10 L13.4 10 L14 ${n(hem + 3.4)} Q0 ${n(hem + 5)} -14 ${n(hem + 3.4)}Z`, lp.base, 'stroke-width=".8"');
        let detail = "";
        if (shape === "scale") detail = texture(vest, "scale", mp);
        else if (shape === "brigandine") detail = Array.from({ length: 20 }, (_, i) => dot(-9.6 + (i % 5) * 4.8, -11 + Math.floor(i / 5) * 6.2, 0.6, tp.kind === "metal" ? tp.light : "#c9a24a")).join("") + line("M-13 -1 H13 M-13 5.2 H13 M-12.6 11.4 H12.6", mp.dark, 0.6, 'opacity=".7"');
        else detail = surface(vest, mp) + line("M-7 -16.6 Q-6 -2 -8 13 M7 -16.6 Q6 -2 8 13", mp.dark, 0.6, 'stroke-dasharray="1.4 1"') + sym(path("M-12.5 -17 Q-17.4 -17.6 -18 -12 L-15 -11.6 L-13 -14Z", darken(mp.base, 0.06), 'stroke-width=".7"'));
        const straps = shape === "leather" ? line("M-9.6 -17 L9.6 10", tp.base, 2.2) + path("M-1.4 -5 h2.8 v3 h-2.8Z", buckle, 'stroke-width=".5"') : "";
        return under + path(vest, mp.base) + (shape === "leather" ? "" : surface(vest, mp)) + detail + shade(vest) + path("M-5 -18.5 Q0 -15 5 -18.5 L5.4 -20 Q0 -16.6 -5.4 -20Z", lp.base, 'stroke-width=".6"')
          + straps + belt(shape === "leather" ? 9.4 : 6.6, 13.2, tp, buckle) + armorDecor(main.maker, shape, mp, hem, 12) + runes;
      }
      case "robe": {
        const body = "M-8 -19 Q0 -15.5 8 -19 L13.6 -16.6 L23 7 L17.6 9.6 L13 -3 L16 28 Q0 30 -16 28 L-13 -3 L-17.6 9.6 L-23 7 L-13.6 -16.6Z";
        const hood = path("M-8.6 -18 Q-11.4 -26.4 0 -27.4 Q11.4 -26.4 8.6 -18Z", mp.base) + path("M-6 -18.6 Q-7.2 -24.2 0 -24.8 Q7.2 -24.2 6 -18.6Z", lp.base, 'stroke-width=".7"');
        const cuffs = sym(path("M-23 7 L-17.6 9.6 L-16.9 7.6 L-22.1 5.2Z", lp.base, 'stroke-width=".7"'));
        const trims = glowWrap(tp, line("M0 -15.6 V28.8", tp.base, 1.6) + line("M-15.8 27.4 Q0 29.6 15.8 27.4", tp.base, 1.6) + line("M-8 -19 Q0 -15.5 8 -19", tp.base, 1.4));
        return hood + path(body, mp.base) + surface(body, mp) + shade(body) + line("M-6 2 L-8.4 27 M6 2 L8.4 27", mp.dark, 0.7, 'opacity=".6"') + cuffs + trims + path("M-12.8 1.6 H12.8 V3.8 H-12.8Z", lp.base, 'stroke-width=".6"')
          + armorDecor(main.maker, shape, mp, 28, 15) + runes;
      }
      case "coat": {
        const half = "M-5 -19 L-0.8 -10 L-1.4 24.6 L-14.6 24.6 L-13.2 -6 L-16.4 6.2 L-20.6 4.4 L-13 -17Z";
        const panel = path("M-5 -18 L5 -18 L3.6 24 L-3.6 24Z", lp.base) + line("M0 -14 V23", lp.dark, 0.6, 'stroke-dasharray="1.2 1.4"');
        const halves = sym(path(half, mp.base) + surface(half, mp) + K.clipped(half, `<rect x="-25" y="-30" width="9" height="62" fill="${K.INK}" opacity=".18"/>`) + line("M-12 9 h5", mp.dark, 0.8));
        const trims = sym(path("M-5 -19 L-0.8 -10 L-4 -8.6 L-7.4 -17.6Z", tp.base, 'stroke-width=".7"') + path("M-20.6 4.4 L-16.4 6.2 L-16.9 4 L-20.1 2.4Z", tp.base, 'stroke-width=".6"') + line("M-14.4 23.6 H-1.6", tp.base, 1.1));
        const buttons = [-4, 1.4, 6.8, 12.2].map(y => dot(-2.8, y, 0.75, tp.kind === "metal" ? tp.light : "#c9a24a")).join("");
        return panel + halves + glowWrap(tp, trims) + buttons + armorDecor(main.maker, shape, mp, 24, 14) + runes;
      }
      default: {
        const tunic = "M-12 -17 Q-6 -19 -4 -18 Q0 -15.5 4 -18 Q6 -19 12 -17 L19 -3 L15 -1 L12.6 -8 L13.6 17 Q0 19 -13.6 17 L-12.6 -8 L-15 -1 L-19 -3Z";
        const collar = path("M-4 -18 Q0 -15.5 4 -18 L0 -10.4Z", lp.base, 'stroke-width=".7"') + line("M-1.4 -15.6 L1.4 -14.4 M-1.2 -13.4 L1.2 -12.4", tp.base, 0.6);
        const sash = glowWrap(tp, path("M-12.8 3 L12.8 1 L12.9 4.5 L-12.8 6.6Z", tp.base, 'stroke-width=".7"') + path("M8 3.6 L10.6 12.2 L8 12.8 L6.6 4Z", tp.base, 'stroke-width=".6"'));
        const cuffs = sym(path("M-19 -3 L-15 -1 L-15.5 -3 L-18.4 -4.6Z", lp.base, 'stroke-width=".6"')) + line("M-13.5 15.6 Q0 17.6 13.5 15.6", lp.base, 1.2);
        return path(tunic, mp.base) + surface(tunic, mp) + shade(tunic) + collar + cuffs + sash + armorDecor(main.maker, shape, mp, 17, 13) + runes;
      }
    }
  }
  /* The Impshield's sigil: a grinning imp head between two bat wings, beaten from crimson
     metal into the face, its eyes lit with void light. Drawn around (0, cy). */
  function impInsignia(cy, scale = 1) {
    const metal = "#a8263b", dark = darken(metal, 0.4), light = "#d65a62";
    const wing = path("M-3.2 -1 Q-7.8 -7.6 -11.6 -5.2 L-10.2 -3.4 L-11.4 -1.2 L-8.9 -1.5 L-9.1 1 L-6.7 0 L-5.7 2.6 Q-4.5 1.2 -3.4 1.4Z", dark, 'stroke-width=".6"')
      + line("M-3.6 -0.2 L-10.6 -4.6 M-4.4 0.6 L-9.4 -1.2 M-4.6 1.2 L-7 0.4", metal, 0.45);
    const horn = path("M-2.6 -3.4 Q-5.2 -6.8 -4 -10 Q-3.2 -6.8 -1.2 -4Z", "#1d1414", 'stroke-width=".5"');
    const ear = path("M-3.7 -0.9 L-6.4 -2.2 L-3.6 0.4Z", metal, 'stroke-width=".5"');
    const head = path("M-3.7 1.6 Q-4.4 -4 0 -4.4 Q4.4 -4 3.7 1.6 Q2.6 5.4 0 6 Q-2.6 5.4 -3.7 1.6Z", metal, 'stroke-width=".7"')
      + path("M-3.2 -2.4 Q0 -3.9 3.2 -2.4 Q2.8 -1 0 -1.2 Q-2.8 -1 -3.2 -2.4Z", light, 'stroke-width="0" opacity=".55"');
    const face = path("M-2.6 -0.6 L-0.6 0.2 L-1.2 0.9 Z M2.6 -0.6 L0.6 0.2 L1.2 0.9 Z", "!#c58cff", 'stroke-width=".35"')
      + line("M-2.1 2.6 Q0 4.4 2.1 2.6", dark, 0.7) + line("M-1.5 2.9 l0.5 0.8 l0.5 -0.6 l0.5 0.7 l0.5 -0.7 l0.5 0.6", "#efe6d0", 0.4);
    return `<g transform="translate(0 ${n(cy)}) scale(${n(scale)})">` + sym(wing + horn + ear) + head + face + "</g>";
  }
  function shieldArt(forged, shape) {
    const [face, rimPart, gripPart] = forged.parts, fp = pal(face.material), rp = pal(rimPart.material), gp = pal(gripPart.material);
    const badge = (size, x, y) => LOGOS[face.maker] ? `<g transform="translate(${n(x - size / 2)} ${n(y - size / 2)}) scale(${n(size / 32)})">${LOGOS[face.maker]}</g>` : "";
    const runes = runeDots(forged.runes?.length || 0, -1.5 * Math.max(0, (forged.runes?.length || 1) - 1), shape === "buckler" ? -8.4 : -18);
    if (shape === "buckler") {
      const faceShape = "M-11.2 0 a11.2 11.2 0 1 0 22.4 0 a11.2 11.2 0 1 0 -22.4 0Z";
      const rivets = Array.from({ length: 10 }, (_, i) => dot(Math.cos(i * Math.PI / 5) * 12.4, Math.sin(i * Math.PI / 5) * 12.4, 0.55, rp.light)).join("");
      const spikes = face.maker === "abyss" ? path("M-1.4 -3.6 L0 -8.4 L1.4 -3.6Z", rp.dark, 'stroke-width=".5"') : "";
      return '<g transform="scale(1.2)">' + line("M10 -4.4 Q17.4 0 10 4.4", gp.base, 2.6) + circle(0, 0, 13.6, rp.base) + path(faceShape, fp.base, 'stroke-width=".8"') + surface(faceShape, fp) + shade(faceShape)
        + rivets + spikes + circle(0, 0, 4.2, rp.base, 'stroke-width=".8"') + dot(-1.2, -1.3, 1.1, rp.light, 'opacity=".8"') + (face.maker === "hellmade" ? gem(0, 0, 1.3, STYLE.hellmade.gem) : "") + runes + "</g>";
    }
    const kite = shape === "kite";
    const outer = kite ? "M0 -25 Q14 -23.6 15 -12 Q14 7 0 28 Q-14 7 -15 -12 Q-14 -23.6 0 -25Z" : "M-15 -25 L15 -25 Q17 -25 17 -23 L17 19 Q0 30 -17 19 L-17 -23 Q-17 -25 -15 -25Z";
    const inner = kite ? "M0 -21.8 Q11.6 -20.6 12.6 -11.2 Q11.6 5.6 0 23.6 Q-11.6 5.6 -12.6 -11.2 Q-11.6 -20.6 0 -21.8Z" : "M-13.2 -22.6 L13.2 -22.6 L14.4 -21.2 L14.4 17.6 Q0 26.4 -14.4 17.6 L-14.4 -21.2Z";
    const rivets = (kite ? [[0, -23.4], [10.6, -18.6], [13.6, -8], [9.4, 8], [4.6, 18], [-4.6, 18], [-9.4, 8], [-13.6, -8], [-10.6, -18.6]] : [[-15.6, -23.6], [15.6, -23.6], [15.6, -8], [-15.6, -8], [15.6, 8], [-15.6, 8], [8, 21.6], [-8, 21.6], [0, 23.6]]).map(([x, y]) => dot(x, y, 0.6, rp.light)).join("");
    const spikes = face.maker === "abyss" ? sym(path(kite ? "M-14.6 -15 L-19.4 -18 L-14.8 -10Z" : "M-17 -16 L-21.4 -18 L-17 -12Z M-17 4 L-21.4 2 L-17 8Z", rp.dark, 'stroke-width=".6"')) : "";
    const emblem = forged.masterwork === "impshield" ? impInsignia(kite ? -4 : -2, 0.95) : badge(kite ? 13 : 16, 0, kite ? -5 : -2);
    return line(kite ? "M13 -8 Q19.4 -4 13.6 0.4" : "M15.6 -6 Q22 -2 15.6 2", gp.base, 2.6) + spikes + path(outer, rp.base) + path(inner, fp.base, 'stroke-width=".8"') + surface(inner, fp) + shade(inner)
      + rivets + emblem + runes;
  }
  /* Rootbark's sigil: four leaves in the colours of the seasons around a ring of bark,
     moss trailing below. Drawn around (0, cy). */
  function rootInsignia(cy, scale = 1) {
    const leaf = (angle, color) => `<g transform="rotate(${angle})">${path("M0 -1.6 Q2.6 -4.6 0 -8.6 Q-2.6 -4.6 0 -1.6Z", color, 'stroke-width=".55"')}${line("M0 -2 L0 -7.6", darken(color, 0.35), 0.35)}</g>`;
    const leaves = leaf(-45, "#f6b8d0") + leaf(45, "#ffd36b") + leaf(135, "#e0782a") + leaf(-135, "#cfe8ff");
    const ring = circle(0, 0, 2.6, "#5a3a22", 'stroke-width=".6"') + circle(0, 0, 1.2, "#8fbf5a", 'stroke-width=".4"');
    const moss = line("M-2 2.4 q-0.6 2.2 -0.2 3.6 M0.4 2.6 q0.4 2 -0.2 3.2 M2.2 2.2 q0.8 1.6 0.4 2.8", "#6f9a4a", 0.6);
    return `<g transform="translate(0 ${n(cy)}) scale(${n(scale)})">${moss}${leaves}${ring}</g>`;
  }
  function drawArmor(forged) {
    const shape = ARMOR_SHAPE[forged.type] || "plate";
    if (forged.category === "shield") return shieldArt(forged, shape);
    return bodyArmor(forged, shape) + (forged.masterwork === "rootbark" ? rootInsignia(-6, 1.05) : "");
  }
  // Battle sprites reuse the humanoid rig's armor layers, painted in the piece's own materials.
  const ARMOR_SPRITE_KIND = { "full-plate": "plate", "splint-mail": "plate", chainmail: "chain", "scale-armor": "scale", brigandine: "brigandine", "leather-armor": "leather", robe: "robe", coat: "coat", garb: "garb", buckler: "round", "kite-shield": "kite", "tower-shield": "tower" };
  const HELMETS = { dwarven: "horned", abyss: "spiked", hellmade: "spiked", "church-of-talos": "crested", roundtable: "great" };
  function spriteMaterial(materialId, maker) {
    const key = "forged-" + materialId + "-" + maker;
    if (!K.MATERIALS[key]) {
      const p = pal(materialId), s = STYLE[maker] || STYLE.outlaw;
      K.MATERIALS[key] = { base: p.base, light: p.light, dark: p.dark, trim: s.trim, grip: s.wrap, ...(p.pattern ? { pattern: p.pattern } : {}) };
    }
    return key;
  }
  function armorVisual(item, ornate) {
    const forged = item.forged, [main, , third] = forged.parts, material = spriteMaterial(main.material, main.maker);
    // Masterwork armor with a hand-made battle design is drawn by its key.
    const base = { id: item.id, name: item.name, kind: ARMOR_SPRITE_KIND[forged.type], material, ornate, unique: spriteUnique(forged.masterwork), rarity: item.rarity, forged };
    if (forged.category === "shield") return { ...base, slot: "shield" };
    return { ...base, slot: "armor", type: forged.category, helmet: HELMETS[main.maker] || null, cloth: pal(main.material).base, trimColor: forged.category === "clothing" ? pal(third.material).base : null };
  }
  // An upright portrait of the piece for the detail panel, shop cards and the Magic Tower.
  function armorPortrait(item, size) {
    const small = size === "small", previous = K.beginInk();
    const markup = K.withPen(small ? 1.1 : 0.8, () => drawArmor(item.forged));
    const defs = K.endInk(previous);
    return `<svg class="forged-weapon-art forged-armor-art${small ? " is-small" : ""}" viewBox="-26 -29 52 60" preserveAspectRatio="xMidYMid meet" aria-hidden="true">${defs}${markup}</svg>`;
  }

  /* ---------- Runes ----------
     A carved stone with its glyph glowing in the rune's colour: futhark letters for the
     scaling runes, element signs for the elemental ones, marks for the empowering ones. */
  const G = (d, fill = false) => ({ d, fill });
  const RUNE_GLYPHS = {
    muscle: G("M-4 10V-10L4 -4V10"), heart: G("M-5 -9L0 -3L5 -9M0 -3V10"), brain: G("M-4 -10V10M-4 -5L3 0L-4 5"), reflex: G("M-5 -10L5 10M5 -10L-5 10"),
    knowledge: G("M4 -10L-4 0L4 10"), wit: G("M-3 -10V10M-3 -10L4 -4M-3 -3L4 3"), accel: G("M-5 10V-10L0 -2L5 -10V10"), fortune: G("M-5 -10L5 -3L-5 4L5 10"),
    tarot: G(star(0, 0, 9, 3.2, 4), true),
    cinder: G("M0 -10C6 -4 7 2 3 9C2 4 0 3 -1 1C-3 4 -4 7 -3 9C-7 3 -5 -3 0 -10Z", true),
    humidity: G("M0 -10C5 -2 7 3 4 7C2 10 -2 10 -4 7C-7 3 -5 -2 0 -10Z", true),
    basalt: G("M-8 8L-2 -6L1 -1L4 -8L8 8Z", true),
    breeze: G("M-8 -4H3A3 3 0 1 0 0 -7M-8 1H6A3 3 0 1 1 3 4M-8 6H0"),
    charge: G("M2 -10L-5 1H0L-2 10L5 -2H0Z", true),
    still: G("M0 -9V9M-8 -4.5L8 4.5M-8 4.5L8 -4.5M-2 -7L0 -5L2 -7M-2 7L0 5L2 7"),
    day: G("M4 0A4 4 0 1 1 -4 0A4 4 0 1 1 4 0ZM0 -10V-7M0 7V10M-10 0H-7M7 0H10M-7 -7L-5 -5M5 5L7 7M7 -7L5 -5M-5 5L-7 7"),
    night: G("M3 -9A9 9 0 1 0 3 9A7 7 0 1 1 3 -9Z", true),
    gaia: G("M-6 8C-8 -2 0 -9 7 -9C7 0 2 8 -6 8ZM-6 8L4 -5"),
    beyond: G("M-6 -1A6 6 0 1 1 6 -1V4H3V8H-3V4H-6ZM-3 -2A1.4 1.4 0 1 0 -3 -1.9M3 -2A1.4 1.4 0 1 0 3 -1.9"),
    crimson: G("M-2 -10C2 -3 4 1 2 4C0 7 -4 7 -6 4C-8 1 -6 -3 -2 -10ZM5 1C7 4 8 6 7 8C6 10 3 10 2 8", true),
    heft: G("M-9 0H6M1 -5L7 0L1 5M-4 -8V8"), precision: G("M6 0A6 6 0 1 1 -6 0A6 6 0 1 1 6 0ZM0 -10V-3M0 3V10M-10 0H-3M3 0H10"),
    "vital-hit": G("M0 -10L2.6 -2.6L10 0L2.6 2.6L0 10L-2.6 2.6L-10 0L-2.6 -2.6Z", true), empower: G("M-6 2L0 -4L6 2M-6 8L0 2L6 8M-6 -4L0 -10L6 -4"),
  };
  const ELEMENT_GLOW = { basic: "#ecebf6", fire: "#ff8a4a", water: "#5ab4ff", earth: "#d8a860", wind: "#9ae8c8", lightning: "#ffe45a", ice: "#b4ecff", holy: "#fff0a8", dark: "#bc98ff", nature: "#8ae06a", death: "#d4dcbc", blood: "#ff4a52" };
  function runeColor(runeId) {
    const rune = window.ForgeRules?.RUNES[runeId];
    if (!rune) return "#9fd8ff";
    if (rune.group === "element") return ELEMENT_GLOW[rune.effect.element] || "#9fd8ff";
    return rune.group === "scaling" ? "#9fd8ff" : "#ffd47a";
  }
  // The rune stone alone, centred on the origin, about 30 wide and 38 tall.
  function runeStone(runeId) {
    const rune = window.ForgeRules?.RUNES[runeId], glyph = RUNE_GLYPHS[runeId] || RUNE_GLYPHS.tarot, color = runeColor(runeId);
    const stone = rune?.rarity === "epic" ? { base: "#4c4258", light: "#7a6c8c", dark: "#2a2232" } : { base: "#5c626a", light: "#8a929c", dark: "#33373d" };
    const outline = "M-11 -17Q0 -20 11 -16Q14.5 0 12 16Q0 19.5 -12 16.5Q-14.5 0 -11 -17Z";
    const mark = glyph.fill ? path(glyph.d, "!" + color, 'stroke="#ffffff" stroke-opacity=".35" stroke-width=".6"') : line(glyph.d, "!" + color, 2.1);
    return path(outline, stone.base) + line("M-9 -14Q0 -16.6 9 -13.4", stone.light, 1, 'opacity=".7"') + line("M10.6 -10Q12.4 2 10.2 13.8", stone.dark, 1.4, 'opacity=".8"')
      + line(glyph.d, stone.dark, glyph.fill ? 3.4 : 3.6, 'opacity=".55"') + `<g class="sp-glow" style="color:${color}">${mark}</g>`;
  }
  function runeArt(runeId, size = "large") {
    const small = size === "small";
    const markup = K.withPen(small ? 1.2 : 1, () => runeStone(runeId));
    return `<svg class="rune-art-svg${small ? " is-small" : ""}" viewBox="-17 -22 34 44" preserveAspectRatio="xMidYMid meet" aria-hidden="true">${markup}</svg>`;
  }
  /* ---------- General goods ----------
     Potions grow more ornate with their tier; traps, camping supplies and the trinkets
     a general store buys back each get a simple drawing in the same ink style. */
  const RARITY_TIER = { common: 0, uncommon: 1, rare: 2, epic: 3, legendary: 4, masterwork: 4 };
  const RARITY_GEM = { common: "#c8c0b0", uncommon: "#5ac878", rare: "#4a8aff", epic: "#b45aff", legendary: "#ffb43a", masterwork: "#ff5a5a" };
  function potionArt(item) {
    const tier = RARITY_TIER[item.rarity] ?? 0, liquid = item.potionResource === "mana" ? "#3a6ae0" : "#d8303a", gold = "#d8ae4a";
    const glass = "!#d8e4ea";
    let body, neckTop;
    if (tier === 0) { body = "M-2.6 -6V-2C-7 -0.6 -9 3 -9 6.5A9 9 0 0 0 9 6.5C9 3 7 -0.6 2.6 -2V-6Z"; neckTop = -6; }
    else if (tier === 1) { body = "M-2.8 -10V-3C-8 -1.4 -10.5 3 -10.5 7A10.5 10.5 0 0 0 10.5 7C10.5 3 8 -1.4 2.8 -3V-10Z"; neckTop = -10; }
    else if (tier === 2) { body = "M-2.6 -12V-3L-11 14Q0 17 11 14L2.6 -3V-12Z"; neckTop = -12; }
    else if (tier === 3) { body = "M-3 -12V-6L-9 -1V12L-5 16H5L9 12V-1L3 -6V-12Z"; neckTop = -12; }
    else { body = "M-2.8 -13V-7Q-11 -4 -11 4Q-11 13 0 17Q11 13 11 4Q11 -4 2.8 -7V-13Z"; neckTop = -13; }
    const fillTop = tier === 2 ? 2 : tier === 3 ? 0 : 2;
    const liquidShape = `<clipPath id="pot${tier}${item.potionResource || "x"}"><path d="${body}"/></clipPath>`;
    let out = `<defs>${liquidShape}</defs>` + path(body, glass, 'fill-opacity=".55"');
    out += `<g clip-path="url(#pot${tier}${item.potionResource || "x"})"><rect x="-14" y="${fillTop}" width="28" height="20" fill="${liquid}"/><path d="M-14 ${fillTop}Q-4 ${fillTop - 2} 0 ${fillTop}T14 ${fillTop}" fill="none" stroke="#ffffff" stroke-opacity=".45" stroke-width=".8"/></g>`;
    out += path(body, "none") + line(`M-6 ${tier === 2 ? 6 : 3}Q-7 ${tier === 2 ? 10 : 8} -4 ${tier === 2 ? 12 : 11}`, "!#ffffff", 1.1, 'opacity=".6"');
    out += path(`M-3.4 ${neckTop - 4}H3.4V${neckTop + 0.6}H-3.4Z`, "#8a6a44", 'stroke-width=".8"');
    if (tier >= 3) out += path(`M-4 ${neckTop + 0.4}H4V${neckTop + 2.6}H-4Z`, gold, 'stroke-width=".6"') + (tier === 4 ? line("M-11 4H11M0 -7V17", gold, 1, 'opacity=".9"') : "");
    if (tier >= 2) out += path(`M-5 ${tier === 2 ? 6 : 4}H5V${tier === 2 ? 10 : 8}H-5Z`, "#e8dcc0", 'stroke-width=".5"');
    return tier === 4 ? `<g class="sp-glow" style="color:${liquid}">${out}</g>` : out;
  }
  function trapArt(item) {
    if (/spring/.test(item.id)) {
      const teeth = Array.from({ length: 7 }, (_, i) => `M${n(-12 + i * 4)} -1l2 -4l2 4`).join("");
      return ellipse(0, 6, 15, 6, "#5a5e66") + path("M-14 2Q0 -12 14 2", "none", 'stroke="#3a3e46" stroke-width="2.4"') + line(teeth, "#c8ccd0", 1.2) + path("M-14 4Q0 16 14 4", "none", 'stroke="#3a3e46" stroke-width="2.4"') + circle(0, 6, 3, "#8a8e96") + line("M14 6Q19 9 17 14", "#6a6e76", 1.4);
    }
    if (/rune/.test(item.id)) return ellipse(0, 6, 15, 7, "#4a4652") + `<g class="sp-glow" style="color:#9fd8ff">${ellipse(0, 6, 11, 4.8, "none", 'stroke="#9fd8ff" stroke-width="1.2"') + line("M-6 6L0 3L6 6L0 9Z M0 3V-4", "!#9fd8ff", 1.1)}</g>` + dot(0, -5, 1.6, "!#d8f0ff");
    return line("M-14 12V-6M14 12V-6", "#6a4a30", 2.2) + line("M-14 -2Q0 2 14 -2", "#c8b48c", 0.9) + path("M-2 -1L2 -1L3 6H-3Z", "#c9a24a", 'stroke-width=".8"') + circle(0, 7.4, 1.2, "#5a4020") + line("M-16 12H16", "#4a3a2a", 1.4);
  }
  function campArt() {
    return path("M-16 4Q-16 -4 -8 -4H10Q16 -4 16 4Q16 12 10 12H-8Q-16 12 -16 4Z", "#7a5a3a") + line("M-8 -4V12M-2 -4V12", "#5a3a20", 1) + K.circle(10, 4, 6, "#9a7a52") + K.circle(10, 4, 2.4, "#6a4a30")
      + path("M-10 -16H2V-8Q2 -5 -4 -5Q-10 -5 -10 -8Z", "#4a4e56") + line("M-11 -16H3", "#2a2e34", 1.4) + line("M-8 -19Q-6 -22 -7 -25M-3 -19Q-1 -22 -2 -25", "#c8c8c8", 0.8, 'opacity=".7"');
  }
  function trinketArt(item) {
    const lower = item.name.toLowerCase(), gemColor = RARITY_GEM[item.rarity] || "#c8c0b0", metal = item.rarity === "common" ? "#a0a4aa" : "#d8ae4a";
    const gemAt = (x, y, r) => gem(x, y, r, gemColor);
    if (/ring|signet/.test(lower)) return ellipse(0, 4, 11, 11, "none", `stroke="${K.paint(metal)}" stroke-width="3.4"`) + path("M-5 -9L0 -14L5 -9L0 -5Z", metal, 'stroke-width=".8"') + gemAt(0, -9.6, 2.6);
    if (/band|armband|anklet|torque/.test(lower)) return ellipse(0, 2, 15, 8, "none", `stroke="${K.paint(metal)}" stroke-width="4"`) + gemAt(0, 10, 2.4) + gemAt(-10, 7.6, 1.6) + gemAt(10, 7.6, 1.6);
    if (/coin|token|seal|brand|sigil/.test(lower)) return circle(0, 0, 12, metal) + circle(0, 0, 9, "none", `stroke="${K.paint(K.darken(metal, 0.3))}" stroke-width="1"`) + path(star(0, 0, 6, 2.4, 5), K.darken(metal, 0.25), 'stroke-width=".6"');
    if (/crown/.test(lower)) return path("M-14 8V-6L-7 2L0 -10L7 2L14 -6V8Z", metal) + [-7, 0, 7].map((x, i) => gemAt(x, 4, i === 1 ? 2.4 : 1.8)).join("");
    if (/gear/.test(lower)) return path(star(0, 0, 13, 9, 10), metal) + circle(0, 0, 4, "#2a2420") + gemAt(0, 0, 2.2);
    if (/lens|prism|eye|orb|pearl|bead|reservoir/.test(lower)) return `<g class="sp-glow" style="color:${gemColor}">${circle(0, 0, 11, gemColor)}</g>` + dot(-4, -4, 3, "#ffffff", 'opacity=".55"') + (/lens/.test(lower) ? line("M10 6Q16 12 12 18", metal, 1.4) : "");
    // Amulets, pendants, brooches, charms, relics, shards and stones hang from a cord.
    return line("M-10 -18Q0 -6 10 -18", "#6a5a44", 1.2) + path("M0 -10L8 0L0 14L-8 0Z", metal) + gemAt(0, 1, 3.4);
  }
  function goodsArt(item, size = "large") {
    if (!item) return "";
    const small = size === "small";
    let markup;
    if (item.category === "rune") return runeArt(item.rune, size);
    if (item.potionResource) markup = K.withPen(small ? 1.1 : 0.9, () => potionArt(item));
    else if (item.mapAction === "camp") markup = K.withPen(small ? 1.1 : 0.9, campArt);
    else if (/trap/.test(item.id)) markup = K.withPen(small ? 1.1 : 0.9, () => trapArt(item));
    else markup = K.withPen(small ? 1.1 : 0.9, () => trinketArt(item));
    return `<svg class="goods-art-svg${small ? " is-small" : ""}" viewBox="-20 -28 40 50" preserveAspectRatio="xMidYMid meet" aria-hidden="true">${markup}</svg>`;
  }
  function logo(makerId, className = "") {
    const maker = window.ForgeRules?.MAKERS[makerId];
    if (!LOGOS[makerId]) return "";
    return `<svg class="forged-logo ${className}" viewBox="0 0 32 32" role="img" aria-label="${maker ? maker.name : makerId} mark"><title>${maker ? maker.name : makerId}</title>${LOGOS[makerId]}</svg>`;
  }

  window.ForgeArt = { draw: drawForged, drawArmor, sprite, visualFor, weapon, logo, rune: runeArt, runeStone, runeColor, goods: goodsArt, palette: pal, SPRITE_KIND };
})();
