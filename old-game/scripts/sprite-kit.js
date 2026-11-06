/* Shared drawing kit for battle sprites: colors, inked SVG primitives,
   metal and cloth palettes, and the visual reading of item catalogue entries.
   Sprites are drawn facing right with their feet at the origin.
   The look is a gothic ink style: heavy near-black outlines, a muted earthy
   palette, solid shadow shapes clipped to each body part and fine hatching.
   Colors prefixed with "!" are drawn as given (glows, magic, glints). */
(() => {
  "use strict";
  const K = window.SpriteKit = window.SpriteKit || {};
  const OUTLINE = "#130e0b";
  const INK = "#0d0907";
  const n = value => Math.round(value * 10) / 10;

  function rgb(color) {
    const value = String(color || "#000").replace("#", "");
    const full = value.length === 3 ? value.split("").map(char => char + char).join("") : value.padEnd(6, "0");
    return [0, 2, 4].map(index => parseInt(full.slice(index, index + 2), 16) || 0);
  }
  function toHex(values) { return "#" + values.map(value => Math.max(0, Math.min(255, Math.round(value))).toString(16).padStart(2, "0")).join(""); }
  function mix(first, second, amount) {
    const a = rgb(first), b = rgb(second);
    return toHex(a.map((value, index) => value + (b[index] - value) * amount));
  }
  const darken = (color, amount = 0.25) => mix(color, "#1a120d", amount);
  const lighten = (color, amount = 0.25) => mix(color, "#ffffff", amount);

  // Deterministic choices keep an enemy or companion looking the same forever.
  function hash(text) {
    let value = 2166136261;
    for (const char of String(text)) { value ^= char.charCodeAt(0); value = Math.imul(value, 16777619); }
    return value >>> 0;
  }
  function seeded(text) {
    let state = hash(text) || 1;
    return () => { state ^= state << 13; state ^= state >>> 17; state ^= state << 5; return ((state >>> 0) % 100000) / 100000; };
  }

  /* The gothic palette: colors lose most of their saturation, settle toward a
     dark umber and never reach pure white. Glows opt out with a "!" prefix. */
  const toneCache = new Map();
  function tone(color) {
    if (typeof color !== "string" || !color.startsWith("#")) return color;
    if (toneCache.has(color)) return toneCache.get(color);
    const [r, g, b] = rgb(color).map(value => value / 255);
    const max = Math.max(r, g, b), min = Math.min(r, g, b), light = (max + min) / 2;
    let hue = 0, saturation = 0;
    if (max !== min) {
      const delta = max - min;
      saturation = light > 0.5 ? delta / (2 - max - min) : delta / (max + min);
      hue = max === r ? (g - b) / delta + (g < b ? 6 : 0) : max === g ? (b - r) / delta + 2 : (r - g) / delta + 4;
      hue /= 6;
    }
    const s2 = saturation * 0.68, l2 = 0.04 + light * 0.86;
    const q = l2 < 0.5 ? l2 * (1 + s2) : l2 + s2 - l2 * s2, p2 = 2 * l2 - q;
    const channel = t => { t = (t + 1) % 1; return t < 1 / 6 ? p2 + (q - p2) * 6 * t : t < 1 / 2 ? q : t < 2 / 3 ? p2 + (q - p2) * (2 / 3 - t) * 6 : p2; };
    const toned = mix(toHex([channel(hue + 1 / 3), channel(hue), channel(hue - 1 / 3)].map(value => value * 255)), "#4a3220", 0.1);
    toneCache.set(color, toned);
    return toned;
  }
  const paint = color => typeof color === "string" && color.startsWith("!") ? color.slice(1) : tone(color);
  // Outline weight for the part being drawn; small features use a finer pen.
  let pen = 1.8;
  function withPen(width, draw) { const previous = pen; pen = width; try { return draw(); } finally { pen = previous; } }
  const stroke = (width = pen, color = OUTLINE) => `stroke="${color}" stroke-width="${width}" stroke-linejoin="round" stroke-linecap="round"`;
  /* The outline for a shape. An element keeps only the first copy of an attribute, so a
     caller's own stroke-width must replace the default rather than follow it. */
  const outline = extra => extra.includes("stroke=") ? "" : extra.includes("stroke-width=") ? `stroke="${OUTLINE}" stroke-linejoin="round" stroke-linecap="round"` : stroke();
  const path = (d, fill, extra = "") => `<path d="${d}" fill="${paint(fill)}" ${outline(extra)} ${extra}/>`;
  const flat = (d, fill, extra = "") => `<path d="${d}" fill="${paint(fill)}" stroke="none" ${extra}/>`;
  const line = (d, color, width = 1.2, extra = "") => `<path d="${d}" fill="none" stroke="${paint(color)}" stroke-width="${width}" stroke-linecap="round" stroke-linejoin="round" ${extra}/>`;
  const ellipse = (cx, cy, rx, ry, fill, extra = "") => `<ellipse cx="${n(cx)}" cy="${n(cy)}" rx="${n(rx)}" ry="${n(ry)}" fill="${paint(fill)}" ${outline(extra)} ${extra}/>`;
  const circle = (cx, cy, r, fill, extra = "") => ellipse(cx, cy, r, r, fill, extra);
  const dot = (cx, cy, r, fill, extra = "") => `<circle cx="${n(cx)}" cy="${n(cy)}" r="${n(r)}" fill="${typeof fill === "string" && fill.startsWith("!") ? fill.slice(1) : fill}" ${extra}/>`;

  /* Ink shading. Shadows are clipped to the shape they fall on; clip paths are
     named after their geometry so identical sprites can share them. */
  let clipDefs = null;
  function beginInk() { const previous = clipDefs; clipDefs = new Map(); return previous; }
  function endInk(previous = null) {
    const defs = clipDefs ? [...clipDefs].map(([id, d]) => `<clipPath id="${id}"><path d="${d}"/></clipPath>`).join("") : "";
    clipDefs = previous;
    return defs ? `<defs>${defs}</defs>` : "";
  }
  function clipped(d, content) {
    if (!content) return "";
    const id = "ink" + hash(d).toString(36) + hash(d + "|").toString(36);
    if (clipDefs) clipDefs.set(id, d);
    return `<g clip-path="url(#${id})">${content}</g>`;
  }
  // Parallel pen strokes confined to a box; callers clip them to a shape.
  function hatch(x, y, w, h, angle = 60, spacing = 2.4, width = 0.55, opacity = 0.7, color = INK) {
    const radians = angle * Math.PI / 180, dx = Math.cos(radians), dy = Math.sin(radians);
    const reach = Math.hypot(w, h);
    let d = "";
    for (let offset = -reach; offset <= reach; offset += spacing) {
      const cx = x + w / 2 - dy * offset, cy = y + h / 2 + dx * offset;
      // Trim the stroke cx,cy ± t·(dx,dy) to the box.
      let low = -reach, high = reach;
      for (const [origin, step, min, max] of [[cx, dx, x, x + w], [cy, dy, y, y + h]]) {
        if (Math.abs(step) < 1e-9) { if (origin < min || origin > max) low = high; continue; }
        const a = (min - origin) / step, b = (max - origin) / step;
        low = Math.max(low, Math.min(a, b)); high = Math.min(high, Math.max(a, b));
      }
      if (high - low > 0.5) d += `M${n(cx + dx * low)} ${n(cy + dy * low)} L${n(cx + dx * high)} ${n(cy + dy * high)} `;
    }
    return d ? `<path d="${d}" stroke="${color}" stroke-width="${width}" opacity="${opacity}" fill="none"/>` : "";
  }
  // A solid shadow with a hatched edge, on the side of a box away from the light.
  function inkShadow(x, y, w, h, fraction = 0.38, opacity = 0.76, edge = true) {
    const width = w * fraction;
    return `<rect x="${n(x - 2)}" y="${n(y - 2)}" width="${n(width + 2)}" height="${n(h + 4)}" fill="${INK}" opacity="${opacity}"/>` + (edge ? hatch(x + width, y, Math.max(1.5, w * 0.08), h, 72, 2.8, 0.45, 0.4) : "");
  }
  const group = (content, attributes = "") => `<g ${attributes}>${content}</g>`;
  const at = (x, y, content, rotate = 0, extra = "") => `<g transform="translate(${n(x)} ${n(y)})${rotate ? ` rotate(${n(rotate)})` : ""}" ${extra}>${content}</g>`;

  // A rounded limb from one joint to another.
  function capsule(x1, y1, x2, y2, width) {
    const dx = x2 - x1, dy = y2 - y1, length = Math.hypot(dx, dy) || 1;
    const nx = -dy / length * width / 2, ny = dx / length * width / 2;
    return `M${n(x1 + nx)} ${n(y1 + ny)} L${n(x2 + nx)} ${n(y2 + ny)} A${n(width / 2)} ${n(width / 2)} 0 0 1 ${n(x2 - nx)} ${n(y2 - ny)} L${n(x1 - nx)} ${n(y1 - ny)} A${n(width / 2)} ${n(width / 2)} 0 0 1 ${n(x1 + nx)} ${n(y1 + ny)}Z`;
  }
  // A tapered limb: thick at the first joint and thinner at the second.
  function taper(x1, y1, x2, y2, w1, w2) {
    const dx = x2 - x1, dy = y2 - y1, length = Math.hypot(dx, dy) || 1;
    const ux = -dy / length, uy = dx / length;
    return `M${n(x1 + ux * w1 / 2)} ${n(y1 + uy * w1 / 2)} L${n(x2 + ux * w2 / 2)} ${n(y2 + uy * w2 / 2)} A${n(w2 / 2)} ${n(w2 / 2)} 0 0 1 ${n(x2 - ux * w2 / 2)} ${n(y2 - uy * w2 / 2)} L${n(x1 - ux * w1 / 2)} ${n(y1 - uy * w1 / 2)} A${n(w1 / 2)} ${n(w1 / 2)} 0 0 1 ${n(x1 + ux * w1 / 2)} ${n(y1 + uy * w1 / 2)}Z`;
  }
  // A star/spark outline used by glints, sigils and celestial items.
  function star(cx, cy, outer, inner, points = 4, rotation = 0) {
    const steps = [];
    for (let index = 0; index < points * 2; index++) {
      const radius = index % 2 ? inner : outer, angle = Math.PI * index / points + rotation - Math.PI / 2;
      steps.push(`${n(cx + Math.cos(angle) * radius)} ${n(cy + Math.sin(angle) * radius)}`);
    }
    return "M" + steps.join(" L") + "Z";
  }

  // Metals, woods and minerals named by the catalogue.
  const MATERIALS = {
    iron: { base: "#8e949b", light: "#cdd2d6", dark: "#585d63", trim: "#7a6248", grip: "#5b3f2b" },
    steel: { base: "#b2bbc4", light: "#eef2f5", dark: "#707a84", trim: "#86684a", grip: "#4f3a2b" },
    damascus: { base: "#959ba3", light: "#dde1e4", dark: "#5b6168", trim: "#8b6236", grip: "#3e2c22", pattern: "waves" },
    dwarven: { base: "#a49d8c", light: "#e2dbc8", dark: "#6b6456", trim: "#c99f42", grip: "#5b3b25", pattern: "runes" },
    blacksteel: { base: "#40454c", light: "#88919b", dark: "#202328", trim: "#9a7c4e", grip: "#2c211c" },
    manasteel: { base: "#6f93d4", light: "#c7dbff", dark: "#3c5995", trim: "#d6e4ff", grip: "#283a66", glow: "#7fb4ff" },
    lumium: { base: "#e3ebf5", light: "#ffffff", dark: "#9aabc2", trim: "#f0d488", grip: "#7c6a4e", glow: "#fff4c8" },
    vulcan: { base: "#c45b30", light: "#f6a066", dark: "#7a2d16", trim: "#3a2a24", grip: "#2e211c", glow: "#ff8a3d" },
    hellsteel: { base: "#7e2323", light: "#c9503f", dark: "#451010", trim: "#1d1414", grip: "#1d1414", glow: "#ff4d2e" },
    voidmetal: { base: "#3d2f5e", light: "#8166b8", dark: "#20173a", trim: "#b094ef", grip: "#1b1430", glow: "#a77cff" },
    starmetal: { base: "#d2def3", light: "#ffffff", dark: "#8494b6", trim: "#f2cf73", grip: "#3a3f63", glow: "#d6e6ff" },
    gold: { base: "#e0b54a", light: "#fbe8a2", dark: "#9a7224", trim: "#fff3c4", grip: "#6b4a22", glow: "#ffd86b" },
    quickgold: { base: "#f0c548", light: "#fff2b0", dark: "#a5791c", trim: "#ffffff", grip: "#6b4a22", glow: "#ffe27a" },
    copper: { base: "#c27a47", light: "#eab089", dark: "#7d4523", trim: "#3d6b5e", grip: "#4b2f20" },
    bronze: { base: "#b78a4d", light: "#e3c48c", dark: "#76542a", trim: "#5d7f6b", grip: "#4b2f20" },
    silver: { base: "#c9d0d8", light: "#ffffff", dark: "#87909a", trim: "#9fb6d4", grip: "#3f3a47" },
    stone: { base: "#8a837a", light: "#b9b2a7", dark: "#5c564f", trim: "#6f685f", grip: "#4a3a2c" },
    wood: { base: "#9a6a3f", light: "#c8945f", dark: "#64411f", trim: "#7b6b55", grip: "#5a3a22" },
    ashwood: { base: "#c8b08a", light: "#e8d8b8", dark: "#8f7652", trim: "#6f5a3e", grip: "#6f5a3e" },
    darkwood: { base: "#6b4527", light: "#9a6b42", dark: "#3e2615", trim: "#a1824e", grip: "#3e2615" },
    elvenwood: { base: "#a9b56a", light: "#dbe3a0", dark: "#6c7638", trim: "#e6c96b", grip: "#5b6b34", glow: "#d8f59a" },
    sagewood: { base: "#7f8f5c", light: "#b4c38c", dark: "#4e5a35", trim: "#9ac27a", grip: "#4e5a35" },
    bone: { base: "#e4dccb", light: "#fffaf0", dark: "#a99d86", trim: "#7f735f", grip: "#6b5b45" },
    crystal: { base: "#9fd6ea", light: "#e9fbff", dark: "#5591ad", trim: "#d9c8ff", grip: "#4a5f7a", glow: "#bff0ff" },
    manacrystal: { base: "#6aa2f0", light: "#cfe3ff", dark: "#3462a8", trim: "#e0eaff", grip: "#2d3f6a", glow: "#86b8ff" },
    voidglass: { base: "#4a3570", light: "#9a7bd0", dark: "#241638", trim: "#c4a8ff", grip: "#21182f", glow: "#b08cff" },
    obsidian: { base: "#2f2a33", light: "#6e6578", dark: "#151218", trim: "#c25a32", grip: "#1d1a20" },
    phoenix: { base: "#e2672f", light: "#ffc36b", dark: "#9b2e16", trim: "#ffd86b", grip: "#6b2a16", glow: "#ffa04a" },
    astral: { base: "#3d4a8a", light: "#9fb2ff", dark: "#1f2756", trim: "#f4e3a2", grip: "#2a2f55", glow: "#c8d4ff" },
    celestial: { base: "#f3e7c2", light: "#ffffff", dark: "#b9a670", trim: "#7fb6ff", grip: "#6b5a8f", glow: "#fff2b8" },
    leather: { base: "#8a5a36", light: "#b78452", dark: "#55351d", trim: "#c9a46a", grip: "#3e2615" },
    scale: { base: "#5f8a5a", light: "#9cc48f", dark: "#355033", trim: "#c9b46a", grip: "#3e2615" },
  };
  // Cloth palettes for robes, coats and default clothing.
  const CLOTH = {
    linen: "#e7dcc3", wool: "#8a7f72", rag: "#9b8a6e", noble: "#5d2f4e", mage: "#3f4f8f", traveler: "#7b5a3a", worker: "#5d6f7d",
    enchanter: "#6b3f8f", silk: "#b8404f", ranger: "#4f6b3f", vital: "#3f8f6b", manasilk: "#3f6fbf", moon: "#c9cfe8", lumium: "#f3eedb",
    phoenix: "#c9452a", void: "#2d2348", starlight: "#26305f", fate: "#7a5ca8",
  };

  // Material names in the catalogue, longest first so "Black Steel" beats "Steel".
  const MATERIAL_WORDS = [
    ["dwarven steel", "dwarven"], ["black steel", "blacksteel"], ["vulcan alloy", "vulcan"], ["vulcanic", "vulcan"], ["manasteel", "manasteel"], ["manacrystal", "manacrystal"],
    ["lumium", "lumium"], ["hellsteel", "hellsteel"], ["hellhide", "hellsteel"], ["voidmetal", "voidmetal"], ["voidglass", "voidglass"], ["voidweave", "voidmetal"], ["starmetal", "starmetal"],
    ["quickgold", "quickgold"], ["damascus", "damascus"], ["silverplate", "silver"], ["celestial", "celestial"], ["astral", "astral"], ["phoenix", "phoenix"],
    ["elvenwood", "elvenwood"], ["sagewood", "sagewood"], ["copper", "copper"], ["crystal", "crystal"], ["runed", "darkwood"], ["arcanist", "voidglass"], ["apprentice", "wood"],
    ["pilgrim", "wood"], ["ash", "ashwood"], ["steel", "steel"], ["iron", "iron"], ["stone", "stone"], ["bedrock", "stone"], ["wooden", "wood"], ["crude", "wood"],
    ["reinforced", "iron"], ["scaled", "scale"], ["dragonscale", "vulcan"], ["cinderheart", "obsidian"], ["galebound", "silver"], ["tide", "crystal"], ["storm", "steel"], ["frost", "crystal"],
  ];
  function materialFor(name, fallback = "iron") {
    const lower = String(name || "").toLowerCase();
    const found = MATERIAL_WORDS.find(([word]) => lower.includes(word));
    return found ? found[1] : fallback;
  }

  // Masterwork items have hand-made designs keyed by their item id.
  const MASTERWORK_KEYS = {
    "Starscale Blade": "starscale", "Golden Apex": "golden-apex",
    "Giantslayer": "giantslayer", "Branch of the Forest": "forest-branch", "Keening": "keening", "Sunder": "sunder",
    "Anvil Hammer": "anvil-hammer", "Claw of Beowulf": "beowulf", "Royal Trident": "royal-trident", "Staff of the Guild": "guild-staff",
    "Heartless Enricher": "enricher", "Hellish Rebuke": "hellish-rebuke", "Jackhammer": "jackhammer",
    "Impshield": "impshield", "Great Forest Plate": "forest-plate", "Godforged Plate": "godforged",
    "Stormhide Cuirass": "stormhide", "Transformer": "transformer", "Seventh Cycle": "seventh-cycle", "Soulbound Barrier": "soulbound",
  };

  function weaponKind(item) {
    const lower = item.name.toLowerCase();
    if (item.type === "spell-staff") return "staff";
    if (item.type === "magic-wand") return "wand";
    if (item.type === "ranged" || item.type === "bow" || /\bbow\b|longbow|greatbow|warbow/.test(lower)) {
      if (/greatbow|ballista|cannonshot/.test(lower)) return "greatbow";
      if (/grand bolt/.test(lower)) return "crossbow";
      if (/longbow/.test(lower)) return "longbow";
      return "bow";
    }
    if (/beast claws/.test(lower)) return "beastclaws";
    if (/horns/.test(lower)) return "horns";
    if (/greatsword/.test(lower)) return "greatsword";
    if (/dagger/.test(lower)) return "dagger";
    if (/spear|piercer$/.test(lower) && !/winged/.test(lower)) return "spear";
    if (/winged piercer/.test(lower)) return "rapier";
    if (/trident/.test(lower)) return "trident";
    if (/axe/.test(lower)) return Number(item.handed) === 2 ? "greataxe" : "axe";
    if (/mace/.test(lower)) return "mace";
    if (/club/.test(lower)) return "club";
    if (/hammer|smasher/.test(lower)) return "hammer";
    return Number(item.handed) === 2 ? "greatsword" : "sword";
  }

  function armorKind(item) {
    const lower = item.name.toLowerCase();
    if (item.type === "shield") {
      if (/tower/.test(lower)) return "tower";
      if (/bedrock/.test(lower)) return "slab";
      if (/wooden/.test(lower)) return "round-wood";
      if (/iron shield/.test(lower)) return "round";
      if (/aegis/.test(lower)) return "aegis";
      if (/ward/.test(lower)) return "kite";
      if (/reinforced|black steel|vulcan/.test(lower)) return "heater";
      return "kite";
    }
    if (item.type === "heavy") {
      if (/chainmail/.test(lower)) return "chain";
      return "plate";
    }
    if (item.type === "light") {
      if (/linen/.test(lower)) return "padded";
      if (/fur/.test(lower)) return "fur";
      if (/hide|hellhide/.test(lower) && !/scale/.test(lower)) return "hide";
      if (/scale|dragonscale/.test(lower)) return "scale";
      if (/chain|chainweave|mail/.test(lower) && !/leafmail/.test(lower)) return "chainshirt";
      if (/brigandine/.test(lower)) return "brigandine";
      if (/leafmail/.test(lower)) return "leafmail";
      if (/spellguard/.test(lower)) return "brigandine";
      return "leather";
    }
    if (/rags/.test(lower)) return "rags";
    if (/cloak/.test(lower)) return "cloak";
    if (/noble/.test(lower)) return "noble";
    if (/worker/.test(lower)) return "worker";
    if (/coat/.test(lower)) return "coat";
    if (/mantle/.test(lower)) return "mantle";
    if (/battlerobe/.test(lower)) return "battlerobe";
    if (/garb/.test(lower)) return "garb";
    return "robe";
  }

  function clothFor(item) {
    const lower = item.name.toLowerCase();
    const found = [["magician", "mage"], ["enchanter", "enchanter"], ["silk battle", "silk"], ["ranger", "ranger"], ["vitalweave", "vital"], ["manasilk", "manasilk"],
      ["moonweave", "moon"], ["lumium", "lumium"], ["phoenix", "phoenix"], ["voidweave", "void"], ["starlight", "starlight"], ["fateweave", "fate"], ["noble", "noble"],
      ["traveler", "traveler"], ["worker", "worker"], ["wool", "wool"], ["rags", "rag"], ["linen", "linen"]].find(([word]) => lower.includes(word));
    return CLOTH[found ? found[1] : "traveler"];
  }

  const visualCache = new Map();
  // Reads one catalogue item into the shape the sprite renderer draws.
  function itemVisual(item) {
    if (!item || !item.id) return null;
    if (visualCache.has(item.id)) return visualCache.get(item.id);
    const unique = item.rarity === "masterwork" ? MASTERWORK_KEYS[item.name] || null : null;
    const ornate = { common: 0, uncommon: 1, rare: 2, epic: 3, legendary: 4, masterwork: 5, exotic: 5 }[item.rarity] ?? 0;
    let visual;
    if (item.forged && window.ForgeArt) visual = window.ForgeArt.visualFor(item);
    else if (item.category === "weapon" || item.id.startsWith("weapon-")) {
      const kind = weaponKind(item);
      const fallback = ["bow", "longbow", "greatbow", "crossbow"].includes(kind) ? "wood" : ["staff", "wand"].includes(kind) ? "wood" : "iron";
      visual = { slot: "weapon", id: item.id, name: item.name, kind, handed: Number(item.handed) === 2 ? 2 : 1, material: materialFor(item.name, fallback), ornate, unique, rarity: item.rarity };
    } else if (item.type === "shield") {
      visual = { slot: "shield", id: item.id, name: item.name, kind: armorKind(item), material: materialFor(item.name, "iron"), ornate, unique, rarity: item.rarity };
    } else if (item.category === "armor" || item.id.startsWith("armor-")) {
      visual = { slot: "armor", id: item.id, name: item.name, type: item.type, kind: armorKind(item), material: materialFor(item.name, item.type === "light" ? "leather" : "iron"), cloth: clothFor(item), ornate, unique, rarity: item.rarity };
    } else visual = null;
    visualCache.set(item.id, visual);
    return visual;
  }

  Object.assign(K, { OUTLINE, INK, n, rgb, mix, darken, lighten, tone, paint, withPen, hash, seeded, stroke, path, flat, line, ellipse, circle, dot, group, at, capsule, taper, star, beginInk, endInk, clipped, hatch, inkShadow, MATERIALS, CLOTH, materialFor, itemVisual, weaponKind, armorKind });
  K.material = key => MATERIALS[key] || MATERIALS.iron;
})();
