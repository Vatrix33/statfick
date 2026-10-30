/* Ink-and-parchment finish for the regional maps. Every region's landscape is
   regraded into the same muted gothic palette and printed onto aged paper with
   burned edges, then given an inked frame, compass rose and scale. Glows that
   must stay bright (lava, lit windows) carry data-vivid and keep their colour. */
(() => {
  "use strict";
  const INK = "#1a120c", BONE = "#e6d5ae";
  const n = value => Math.round(value * 10) / 10;

  /* ---------------- Palette ---------------- */
  const toneCache = new Map();
  function rgbOf(hex) {
    const value = hex.slice(1);
    const full = value.length === 3 ? value.split("").map(char => char + char).join("") : value;
    return [0, 2, 4].map(index => parseInt(full.slice(index, index + 2), 16) / 255);
  }
  // Colours lose about half their saturation, settle toward umber and never reach white.
  function tone(hex) {
    if (toneCache.has(hex)) return toneCache.get(hex);
    const [r, g, b] = rgbOf(hex);
    const max = Math.max(r, g, b), min = Math.min(r, g, b), light = (max + min) / 2;
    let hue = 0, saturation = 0;
    if (max !== min) {
      const delta = max - min;
      saturation = light > 0.5 ? delta / (2 - max - min) : delta / (max + min);
      hue = (max === r ? (g - b) / delta + (g < b ? 6 : 0) : max === g ? (b - r) / delta + 2 : (r - g) / delta + 4) / 6;
    }
    const s2 = saturation * 0.56, l2 = 0.07 + light * 0.76;
    const q = l2 < 0.5 ? l2 * (1 + s2) : l2 + s2 - l2 * s2, p = 2 * l2 - q;
    const channel = t => { t = (t + 1) % 1; return t < 1 / 6 ? p + (q - p) * 6 * t : t < 1 / 2 ? q : t < 2 / 3 ? p + (q - p) * (2 / 3 - t) * 6 : p; };
    const umber = [0x4a, 0x32, 0x20].map(value => value / 255);
    const mixed = [channel(hue + 1 / 3), channel(hue), channel(hue - 1 / 3)].map((value, index) => value + (umber[index] - value) * 0.17);
    const result = "#" + mixed.map(value => Math.round(Math.max(0, Math.min(1, value)) * 255).toString(16).padStart(2, "0")).join("");
    toneCache.set(hex, result);
    return result;
  }
  function toneMarkup(markup) {
    return markup.replace(/<[^>]+>/g, tag => tag.includes("data-vivid") ? tag : tag.replace(/(fill|stroke|stop-color)="(#[0-9a-fA-F]{6}|#[0-9a-fA-F]{3})"/g, (match, attribute, hex) => `${attribute}="${tone(hex)}"`));
  }

  /* ---------------- Aged paper ---------------- */
  // Drawn once on a canvas and laid over the land with a multiply blend.
  let paperUrl = null;
  function seeded(seed) {
    let state = seed;
    return () => { state = state * 16807 % 2147483647; return (state - 1) / 2147483646; };
  }
  function paper() {
    if (paperUrl) return paperUrl;
    const W = 1600, H = 1000, random = seeded(90217);
    const canvas = document.createElement("canvas");
    canvas.width = W; canvas.height = H;
    const ctx = canvas.getContext("2d");
    ctx.fillStyle = "#fbf0dc";
    ctx.fillRect(0, 0, W, H);
    // Mottled fibre and old damp: many soft, faint blotches.
    for (let i = 0; i < 170; i++) {
      const x = random() * W, y = random() * H, r = 30 + random() * 230;
      const gradient = ctx.createRadialGradient(x, y, 0, x, y, r);
      const dark = random() < 0.75;
      gradient.addColorStop(0, dark ? `rgba(112,78,40,${0.035 + random() * 0.07})` : `rgba(255,248,230,${0.05 + random() * 0.08})`);
      gradient.addColorStop(1, "rgba(112,78,40,0)");
      ctx.fillStyle = gradient;
      ctx.fillRect(x - r, y - r, r * 2, r * 2);
    }
    // Fibres.
    ctx.lineCap = "round";
    for (let i = 0; i < 3200; i++) {
      const x = random() * W, y = random() * H, length = 3 + random() * 14, angle = random() * Math.PI;
      ctx.strokeStyle = `rgba(92,62,32,${0.04 + random() * 0.09})`;
      ctx.lineWidth = 0.6 + random() * 0.8;
      ctx.beginPath(); ctx.moveTo(x, y); ctx.lineTo(x + Math.cos(angle) * length, y + Math.sin(angle) * length); ctx.stroke();
    }
    // Fine grain.
    const image = ctx.getImageData(0, 0, W, H), data = image.data;
    for (let i = 0; i < data.length; i += 4) {
      const grain = (random() - 0.5) * 22;
      data[i] += grain; data[i + 1] += grain; data[i + 2] += grain * 0.9;
    }
    ctx.putImageData(image, 0, 0);
    // Burned, darkened margins with an uneven edge.
    const burn = ctx.createRadialGradient(W / 2, H / 2, H * 0.38, W / 2, H / 2, W * 0.62);
    burn.addColorStop(0, "rgba(46,28,14,0)");
    burn.addColorStop(0.62, "rgba(46,28,14,0.22)");
    burn.addColorStop(1, "rgba(30,18,8,0.78)");
    ctx.fillStyle = burn;
    ctx.fillRect(0, 0, W, H);
    for (let i = 0; i < 260; i++) {
      const side = i % 4, along = random();
      const x = side === 0 ? along * W : side === 1 ? W : side === 2 ? along * W : 0;
      const y = side === 0 ? 0 : side === 1 ? along * H : side === 2 ? H : along * H;
      const r = 14 + random() * 46;
      const gradient = ctx.createRadialGradient(x, y, 0, x, y, r);
      gradient.addColorStop(0, `rgba(28,16,8,${0.25 + random() * 0.3})`);
      gradient.addColorStop(1, "rgba(28,16,8,0)");
      ctx.fillStyle = gradient;
      ctx.fillRect(x - r, y - r, r * 2, r * 2);
    }
    paperUrl = canvas.toDataURL("image/jpeg", 0.86);
    return paperUrl;
  }

  /* ---------------- Shared symbols ---------------- */

  /* ---------------- Terrain symbols ----------------
     Engraved map marks, lit from the upper right like the battle sprites:
     the west face of every hill, peak and dune falls into hatched shadow.
     Fill colours come from currentColor so one symbol serves every region. */
  const ink = (d, width = 1.6, extra = "") => `<path d="${d}" fill="none" stroke="${INK}" stroke-width="${width}" stroke-linecap="round" stroke-linejoin="round" ${extra}/>`;
  const shadow = (d, opacity = 0.32) => `<path d="${d}" fill="${INK}" opacity="${opacity}"/>`;
  const symbols = `
    <g id="ink-hill"><path d="M-27 2Q-16 -19 -1 -20Q16 -19 28 2Z" fill="currentColor"/>${shadow("M-27 2Q-16 -19 -1 -20L-4 2Z", 0.26)}${ink("M-21 -1l5 -9M-16 1l5 -12M-11 1l5 -14M-6 1l3 -11", 0.9, 'opacity=".75"')}${ink("M-27 2Q-16 -19 -1 -20Q16 -19 28 2", 1.9)}</g>
    <g id="ink-peak"><path d="M-36 4L-8 -48L1 -35L9 -44L38 4Z" fill="currentColor"/>${shadow("M-36 4L-8 -48L-3 -21L-12 4Z", 0.36)}${ink("M-27 -6l6 -12M-22 1l7 -15M-17 3l7 -16M-30 2l4 -8", 0.9, 'opacity=".7"')}${ink("M-8 -48L-3 -21L-7 4M9 -44L13 -24", 1.1, 'opacity=".65"')}${ink("M-36 4L-8 -48L1 -35L9 -44L38 4", 2.1)}</g>
    <g id="ink-peak-snow"><use href="#ink-peak"/><path d="M-8 -48L-17 -31L-11 -33L-6 -27L-2 -33L1 -35Z" fill="#ddd6c6" stroke="${INK}" stroke-width="1.2" stroke-linejoin="round"/><path d="M9 -44L4 -35L9 -36L13 -32Z" fill="#ddd6c6" stroke="${INK}" stroke-width="1" stroke-linejoin="round"/></g>
    <g id="ink-dune"><path d="M-38 4Q-16 -15 6 -13Q24 -10 38 4Z" fill="currentColor"/>${shadow("M-38 4Q-16 -15 6 -13Q-2 -4 -6 4Z", 0.26)}${ink("M-30 0l4 -6M-24 1l4 -8M-18 1l4 -9M-12 2l3 -9", 0.8, 'opacity=".65"')}${ink("M-38 4Q-16 -15 6 -13Q24 -10 38 4", 1.6)}</g>
    <g id="ink-tuft">${ink("M0 0l-6 -13M0 0l-1.5 -18M0 0l3.5 -15M0 0l8 -10M-3 0l-8 -7", 1.4)}${ink("M-11 1h22", 1.1, 'opacity=".55"')}</g>
    <g id="ink-grass">${ink("M0 0l-3 -7M0 0l.5 -9M0 0l3.5 -6", 1.1, 'opacity=".7"')}</g>
    <g id="ink-rock"><path d="M-15 4L-12 -8L-3 -15L9 -11L16 4Z" fill="currentColor"/>${shadow("M-15 4L-12 -8L-3 -15L-1 4Z", 0.38)}${ink("M-3 -15L-1 4M-12 -8l4 5", 1, 'opacity=".6"')}${ink("M-15 4L-12 -8L-3 -15L9 -11L16 4Z", 1.7)}</g>
    <g id="ink-cross"><ellipse cy="3" rx="14" ry="4.5" fill="currentColor" stroke="${INK}" stroke-width="1.3"/>${ink("M0 2V-21M-7 -14H7", 3.2)}${ink("M1 1V-20M-6 -13H6", 1, 'stroke="#8a7458" opacity=".8"')}</g>
    <g id="ink-stone"><ellipse cy="3" rx="12" ry="4" fill="currentColor" stroke="${INK}" stroke-width="1.2"/><path d="M-6 3V-9A6 6 0 0 1 6 -9V3Z" fill="#7d776c"/>${shadow("M-6 3V-9A6 6 0 0 1 0 -15V3Z", 0.38)}${ink("M-6 3V-9A6 6 0 0 1 6 -9V3", 1.7)}${ink("M-2 -7h4M0 -9v5", 0.9, 'opacity=".7"')}</g>
    <g id="ink-blade">${ink("M0 4L6 -26", 2.6)}${ink("M1 2L6.5 -24", 1, 'stroke="#9a948a"')}${ink("M-4 -15L9 -12", 2.4)}${ink("M2 -14L4 -19", 2.2)}</g>
    <g id="ink-crater"><ellipse rx="19" ry="7.5" fill="${INK}" opacity=".3"/>${ink("M-19 0A19 7.5 0 0 0 19 0", 1.5)}${ink("M-17 -2A19 7.5 0 0 1 15 -4", 1, 'stroke="#d8c7a2" opacity=".5"')}</g>
    <g id="ink-deadtree">${ink("M0 4V-24M0 -10L-9 -20M0 -15L8 -27M-5 -15l-3 -8M4 -20l6 -3M0 -24l-3 -7", 2.2)}<ellipse cy="5" rx="9" ry="3" fill="${INK}" opacity=".35"/></g>
    <g id="ink-palm"><ellipse cy="3" rx="10" ry="3" fill="${INK}" opacity=".35"/>${ink("M0 2Q-4 -14 3 -30", 3.4)}${[[-20, -22], [-12, -40], [4, -44], [18, -36], [20, -22]].map(([x, y]) => `<path d="M3 -30Q${n((3 + x) / 2)} ${n(-30 + (y + 30) * 1.3)} ${x} ${y}Q${n((3 + x) / 2 + 2)} ${n(-30 + (y + 30) * 0.5)} 3 -30Z" fill="#4d5536" stroke="${INK}" stroke-width="1.2" stroke-linejoin="round"/>`).join("")}</g>
    <g id="ink-pillar"><path d="M-6 4V-22H6V4Z" fill="currentColor"/>${shadow("M-6 4V-22H-1V4Z", 0.38)}<path d="M-8 -22h16v-4h-16Z" fill="currentColor" stroke="${INK}" stroke-width="1.4"/>${ink("M-6 4V-22H6V4M-9 4h18M6 -22l-3 -5", 1.6)}</g>
    <g id="ink-wave">${ink("M-15 0q7.5 -7 15 0t15 0", 1.4, 'opacity=".55"')}${ink("M-9 5q5 -4 10 0", 1, 'opacity=".35"')}</g>
    <g id="ink-cliff"><path d="M-42 2L-40 -9L-30 -13L-20 -8L-9 -18L4 -12L16 -20L29 -10L42 -14L44 2Z" fill="currentColor"/>${ink("M-36 -8v9M-31 -11v12M-26 -10v11M-21 -8v10M-15 -11v12M-10 -15v16M-5 -14v15M0 -13v14M6 -12v13", 0.9, 'opacity=".7"')}${shadow("M-42 2L-40 -9L-30 -13L-20 -8L-9 -18L-10 2Z", 0.3)}${ink("M-42 2L-40 -9L-30 -13L-20 -8L-9 -18L4 -12L16 -20L29 -10L42 -14L44 2", 1.9)}</g>
    <g id="ink-spire"><path d="M-14 4L-6 -50L2 -60L13 4Z" fill="currentColor"/>${shadow("M-14 4L-6 -50L2 -60L-2 4Z", 0.38)}${ink("M-11 -6l3 -12M-9 -20l3 -12M-7 -34l2 -10", 0.9, 'opacity=".7"')}${ink("M2 -60L-2 4M6 -30l3 6", 1, 'opacity=".6"')}${ink("M-14 4L-6 -50L2 -60L13 4", 2)}<ellipse cy="5" rx="16" ry="3.5" fill="${INK}" opacity=".3"/></g>
    <g id="ink-jungle"><ellipse cy="5" rx="17" ry="4.5" fill="${INK}" opacity=".4"/><path d="M0 5V-12" stroke="#2c2018" stroke-width="3.4"/><path d="M-6 -12Q-20 -10 -18 -22Q-24 -34 -10 -36Q-6 -48 6 -42Q20 -44 18 -30Q26 -20 14 -12Z" fill="currentColor" stroke="${INK}" stroke-width="1.7" stroke-linejoin="round"/>${shadow("M-6 -12Q-20 -10 -18 -22Q-24 -34 -10 -36Q-6 -48 -1 -44Q-4 -26 2 -12Z", 0.4)}${ink("M-12 -26q4 -3 8 -1M2 -36q5 -2 9 1M6 -22q4 -2 7 1", 1, 'opacity=".55"')}${ink("M8 -38q4 0 6 3", 1, 'stroke="#9aa878" opacity=".55"')}</g>
    <g id="ink-shard"><path d="M-5 4L-2 -16L4 -9L6 4Z" fill="currentColor" stroke="${INK}" stroke-width="1.4" stroke-linejoin="round"/>${shadow("M-5 4L-2 -16L0 4Z", 0.35)}<path d="M2 -9l1 -3" stroke="#f0c8b8" stroke-width="1" data-vivid=""/></g>
  `;
  const defs = `
    <pattern id="ink-hatch" width="7" height="7" patternUnits="userSpaceOnUse" patternTransform="rotate(38)"><path d="M0 0V7" stroke="${INK}" stroke-width="1.1"/></pattern>
    <pattern id="ink-hatch-fine" width="4.5" height="4.5" patternUnits="userSpaceOnUse" patternTransform="rotate(-52)"><path d="M0 0V4.5" stroke="${INK}" stroke-width=".8"/></pattern>
    <pattern id="ink-stipple" width="18" height="18" patternUnits="userSpaceOnUse"><circle cx="3" cy="4" r="1" fill="${INK}"/><circle cx="12" cy="9" r=".8" fill="${INK}"/><circle cx="7" cy="15" r=".9" fill="${INK}"/><circle cx="16" cy="16" r=".7" fill="${INK}"/></pattern>
    ${symbols}
  `;

  /* ---------------- Placing symbols ---------------- */
  const FOOTPRINT = { "ink-hill": [27, 20], "ink-peak": [38, 48], "ink-peak-snow": [38, 48], "ink-dune": [38, 15], "ink-tuft": [11, 18], "ink-grass": [4, 9], "ink-rock": [16, 15], "ink-cross": [14, 21], "ink-stone": [12, 15], "ink-blade": [9, 26], "ink-crater": [19, 8], "ink-deadtree": [10, 30], "ink-palm": [20, 44], "ink-pillar": [9, 27], "ink-wave": [15, 6], "ink-shard": [6, 16], "ink-cliff": [43, 20], "ink-spire": [14, 60], "ink-jungle": [22, 46] };
  function segmentDistance(point, a, b) {
    const dx = b.x - a.x, dy = b.y - a.y, length = dx * dx + dy * dy || 1;
    const t = Math.max(0, Math.min(1, ((point.x - a.x) * dx + (point.y - a.y) * dy) / length));
    return Math.hypot(point.x - a.x - dx * t, point.y - a.y - dy * t);
  }
  const lineDistance = (point, line) => line.slice(1).reduce((best, next, index) => Math.min(best, segmentDistance(point, line[index], next)), Infinity);
  const inside = (point, area, scale = 1) => ((point.x - area.x) / (area.rx * scale)) ** 2 + ((point.y - area.y) / (area.ry * scale)) ** 2 <= 1;
  // Keeps symbols off water, roads, rivers, towns, lettering and the compass.
  function clearance(region, labels, compassAt) {
    const rules = window.WorldRules;
    const places = [...(region.settlements || []), ...(region.sites || [])];
    const free = point => {
      if (point.x < 40 || point.x > 1560 || point.y < 40 || point.y > 960) return false;
      if (rules && !rules.isLand(point, region.id)) return false;
      if (Math.hypot(point.x - compassAt[0], point.y - compassAt[1]) < 92 || point.x < 340 && point.y > 890) return false;
      if (places.some(place => Math.abs(point.x - place.x) < 92 && point.y - place.y > -118 && point.y - place.y < 84)) return false;
      if ((region.roads || []).some(road => lineDistance(point, road) < 16)) return false;
      if (region.river && lineDistance(point, region.river) < 34) return false;
      return !labels.some(box => Math.abs(point.x - box.x) < box.w && Math.abs(point.y - box.y) < 20);
    };
    // The whole outline of a symbol must be clear, not just the point it stands on.
    return (point, avoid = [], size = 1, symbol = "ink-rock") => {
      const [w, h] = (FOOTPRINT[symbol] || [16, 16]).map(value => value * size);
      const probes = [[0, 0], [-w, 0], [w, 0], [0, -h], [-w * 0.55, -h * 0.55], [w * 0.55, -h * 0.55], [0, -h * 0.5]];
      if (!probes.every(([dx, dy]) => free({ x: point.x + dx, y: point.y + dy }))) return false;
      return !avoid.some(area => inside(point, area));
    };
  }
  /* Scatter `count` symbols over the given ellipses or rectangles, keeping them
     apart and clear of everything above. Returns <use> markup sorted back to front. */
  function scatter(clear, { areas, count, seed, symbol, color, size = [0.8, 1.2], spacing = 40, avoid = [], flip = true }) {
    const random = seeded(seed), placed = [];
    for (let attempt = 0; attempt < count * 40 && placed.length < count; attempt++) {
      const area = areas[Math.floor(random() * areas.length)];
      let point;
      if (area.rx) { const angle = random() * Math.PI * 2, radius = Math.sqrt(random()); point = { x: area.x + Math.cos(angle) * radius * area.rx, y: area.y + Math.sin(angle) * radius * area.ry }; }
      else point = { x: area.x0 + random() * (area.x1 - area.x0), y: area.y0 + random() * (area.y1 - area.y0) };
      const scale = size[0] + random() * (size[1] - size[0]);
      const id = typeof symbol === "function" ? symbol(point) : symbol;
      if (!clear(point, avoid, scale, id)) continue;
      if (placed.some(other => Math.hypot(other.x - point.x, (other.y - point.y) * 1.6) < spacing * Math.max(scale, other.scale))) continue;
      placed.push({ ...point, id, scale, mirror: flip && random() < 0.35 });
    }
    const fill = color ? ` style="color:${tone(color)}"` : "";
    return placed.sort((a, b) => a.y - b.y).map(item => `<use href="#${item.id}" transform="translate(${n(item.x)} ${n(item.y)}) scale(${item.mirror ? -n(item.scale * 100) / 100 : n(item.scale * 100) / 100} ${n(item.scale * 100) / 100})"${fill}/>`).join("");
  }
  const WHOLE = [{ x0: 40, y0: 50, x1: 1560, y1: 960 }];
  const alternate = (...ids) => item => ids[Math.floor((item.x * 7 + item.y * 13) % ids.length)];

  // What each region adds on top of its painted land.
  const DETAILS = {
    "alder-vale": (place, region) => {
      const forests = (region.forests || []).map(forest => ({ x: forest.x, y: forest.y, rx: forest.rx * 1.12, ry: forest.ry * 1.15 }));
      return place({ areas: [{ x: 600, y: 650, rx: 250, ry: 110 }, { x: 1260, y: 760, rx: 220, ry: 120 }, { x: 1360, y: 250, rx: 180, ry: 110 }, { x: 160, y: 620, rx: 120, ry: 110 }], count: 16, seed: 11, symbol: "ink-hill", color: "#a7ad78", spacing: 62, avoid: forests })
        + place({ areas: WHOLE, count: 90, seed: 12, symbol: "ink-grass", spacing: 34, avoid: forests });
    },
    "stoneveil-foothills": (place, region) => place({ areas: [{ x0: 40, y0: 70, x1: 1560, y1: 250 }], count: 26, seed: 21, symbol: "ink-peak-snow", color: "#8f8c7c", size: [0.95, 1.5], spacing: 52 })
      + place({ areas: region.hillAreas, count: 34, seed: 22, symbol: "ink-hill", color: "#a7a675", spacing: 56 })
      + place({ areas: WHOLE, count: 70, seed: 23, symbol: "ink-grass", spacing: 34 }),
    "high-stoneveil": place => place({ areas: [{ x0: 40, y0: 80, x1: 1560, y1: 330 }], count: 30, seed: 24, symbol: "ink-peak-snow", color: "#8d9294", size: [1, 1.55], spacing: 52 })
      + place({ areas: [{ x0: 40, y0: 380, x1: 420, y1: 720 }, { x0: 1200, y0: 380, x1: 1560, y1: 720 }], count: 10, seed: 29, symbol: "ink-cliff", color: "#83898a", size: [0.9, 1.3], spacing: 60 })
      + place({ areas: [{ x0: 40, y0: 360, x1: 420, y1: 730 }, { x0: 1200, y0: 360, x1: 1560, y1: 730 }], count: 16, seed: 25, symbol: "ink-rock", color: "#7d8584", spacing: 46 })
      + place({ areas: [{ x0: 40, y0: 770, x1: 1560, y1: 950 }], count: 24, seed: 26, symbol: "ink-rock", color: "#8f948c", size: [0.6, 0.95], spacing: 40 }),
    "stoneveil-summit": place => place({ areas: [{ x0: 40, y0: 80, x1: 1560, y1: 430 }], count: 30, seed: 27, symbol: "ink-peak-snow", color: "#7f8f99", size: [1.15, 1.75], spacing: 54 })
      + place({ areas: [{ x0: 40, y0: 470, x1: 1560, y1: 640 }], count: 12, seed: 30, symbol: "ink-cliff", color: "#8b99a1", size: [0.9, 1.3], spacing: 64 })
      + place({ areas: [{ x0: 40, y0: 640, x1: 1560, y1: 950 }], count: 22, seed: 28, symbol: "ink-rock", color: "#9aa4a8", size: [0.6, 1], spacing: 42 }),
    "verdant-reach": (place, region) => place({ areas: [{ x: 1460, y: 260, rx: 120, ry: 190 }, { x: 1100, y: 900, rx: 320, ry: 70 }, { x: 1470, y: 760, rx: 100, ry: 170 }], count: 14, seed: 31, symbol: "ink-hill", color: "#acab74", spacing: 58, avoid: [region.forestLayers.outer] })
      + place({ areas: WHOLE, count: 50, seed: 32, symbol: "ink-grass", spacing: 34, avoid: [region.forestLayers.outer] }),
    "stormwake-hills": (place, region) => place({ areas: region.hillAreas, count: 44, seed: 41, symbol: "ink-hill", color: "#7f957c", size: [0.9, 1.35], spacing: 56 })
      + place({ areas: [region.stormcrest], count: 10, seed: 42, symbol: "ink-rock", color: "#6f7b78", spacing: 44 })
      + place({ areas: WHOLE, count: 60, seed: 43, symbol: "ink-grass", spacing: 36 }),
    "gloamfen-marsh": (place, region) => place({ areas: [region.deepMire, region.reedwater, ...WHOLE], count: 80, seed: 51, symbol: "ink-tuft", spacing: 40, avoid: [region.rockyRise] })
      + place({ areas: [region.deepMire, { x: 300, y: 860, rx: 240, ry: 80 }], count: 10, seed: 52, symbol: "ink-deadtree", spacing: 70 })
      + place({ areas: [region.rockyRise], count: 16, seed: 53, symbol: "ink-rock", color: "#7b7f74", size: [0.9, 1.6], spacing: 40 }),
    "verdigris-jungle": () => "",
    "mirrormere-basin": (place, region) => place({ areas: [region.marsh], count: 24, seed: 61, symbol: "ink-tuft", spacing: 34 })
      + place({ areas: [{ x: 190, y: 330, rx: 170, ry: 200 }], count: 12, seed: 62, symbol: "ink-rock", color: "#858a7a", spacing: 44 })
      + place({ areas: [{ x0: 60, y0: 820, x1: 1540, y1: 950 }], count: 18, seed: 63, symbol: "ink-dune", color: "#c8ad7c", spacing: 70 })
      + place({ areas: [{ x0: 60, y0: 600, x1: 1540, y1: 800 }], count: 40, seed: 64, symbol: "ink-grass", spacing: 34, avoid: [region.marsh] }),
    "saffron-expanse": (place, region) => place({ areas: WHOLE, count: 48, seed: 71, symbol: "ink-dune", color: "#d3b27a", size: [0.85, 1.4], spacing: 70, avoid: [{ ...region.oasis, rx: region.oasis.rx * 1.25, ry: region.oasis.ry * 1.3 }, region.saltPans] })
      + region.oasisPalms,
    "deep-desert": (place, region) => place({ areas: WHOLE, count: 40, seed: 81, symbol: "ink-dune", color: "#c6a170", size: [0.85, 1.4], spacing: 72, avoid: [region.necropolis, region.obsidianFlats] })
      + place({ areas: [region.obsidianFlats], count: 14, seed: 82, symbol: "ink-rock", color: "#4a4446", spacing: 46 })
      + place({ areas: [region.necropolis], count: 14, seed: 83, symbol: "ink-pillar", color: "#a08766", spacing: 46, avoid: [{ x: region.pyramid.x, y: region.pyramid.y - 20, rx: region.pyramid.rx * 1.75, ry: region.pyramid.ry * 1.55 }] }),
    "crimson-divide": (place, region) => place({ areas: [{ x0: 40, y0: 50, x1: 1560, y1: 480 }], count: 22, seed: 91, symbol: "ink-dune", color: "#d4b27c", spacing: 72, avoid: [region.escarpment] })
      + place({ areas: [{ x0: 40, y0: 520, x1: 1560, y1: 960 }], count: 22, seed: 92, symbol: "ink-dune", color: "#a85b52", spacing: 72, avoid: [region.bloodglass] })
      + place({ areas: [region.escarpment], count: 6, seed: 93, symbol: "ink-cliff", color: "#8a6e5c", size: [1.4, 1.9], spacing: 46 })
      + place({ areas: [{ x: 1150, y: 860, rx: 380, ry: 90 }, { x: 470, y: 930, rx: 200, ry: 40 }], count: 9, seed: 95, symbol: "ink-spire", color: "#6a4644", size: [0.8, 1.3], spacing: 46 })
      + place({ areas: [region.bloodglass], count: 16, seed: 94, symbol: "ink-shard", color: "#b9726d", size: [1.5, 2.4], spacing: 26 }),
    "bloodstone-coast": (place, region) => place({ areas: [{ x0: 40, y0: 60, x1: 1560, y1: 240 }], count: 14, seed: 101, symbol: "ink-dune", color: "#b9675a", spacing: 74, avoid: region.rockFields })
      + place({ areas: region.rockFields, count: 16, seed: 102, symbol: "ink-spire", color: "#6b4450", size: [1.1, 2.1], spacing: 36 })
      + place({ areas: [{ x0: 40, y0: 520, x1: 1560, y1: 800 }], count: 12, seed: 103, symbol: "ink-dune", color: "#8c4a4f", spacing: 80 })
      + region.seaWaves,
    "ancient-battlefield": (place, region) => place({ areas: [region.massGraves], count: 30, seed: 111, symbol: alternate("ink-cross", "ink-stone", "ink-cross"), color: "#6f665c", spacing: 34 })
      + place({ areas: [region.bonePits], count: 9, seed: 112, symbol: "ink-crater", spacing: 60 })
      + place({ areas: WHOLE, count: 26, seed: 113, symbol: "ink-blade", spacing: 50, avoid: [region.massGraves] })
      + place({ areas: WHOLE, count: 14, seed: 114, symbol: "ink-crater", spacing: 70, avoid: [region.massGraves, region.trenches] })
      + place({ areas: WHOLE, count: 9, seed: 115, symbol: "ink-deadtree", spacing: 90, avoid: [region.massGraves, region.bonePits] }),
    "duskpine-forest": (place, region) => place({ areas: [region.frostBog], count: 22, seed: 121, symbol: "ink-tuft", spacing: 34 })
      + place({ areas: [region.shadowHollow, region.frostBog], count: 8, seed: 122, symbol: "ink-deadtree", spacing: 60 }),
    "sunward-coast": (place, region) => place({ areas: [{ x0: 60, y0: 60, x1: 1060, y1: 950 }], count: 70, seed: 131, symbol: "ink-grass", spacing: 36, avoid: [region.saltMarsh] })
      + place({ areas: [region.saltMarsh], count: 20, seed: 132, symbol: "ink-tuft", spacing: 34 })
      + place({ areas: [{ x0: 1100, y0: 60, x1: 1270, y1: 950 }], count: 12, seed: 133, symbol: "ink-dune", color: "#d6bd8c", size: [0.7, 1], spacing: 70 })
      + region.seaWaves,
    "cinder-isle": (place, region) => place({ areas: [{ x: 520, y: 400, rx: 230, ry: 170 }, { x: 500, y: 760, rx: 260, ry: 90 }], count: 14, seed: 141, symbol: "ink-rock", color: "#5c5c55", spacing: 50, avoid: [region.volcano] })
      + region.seaWaves,
  };
  // Waves only where the rules say there is water.
  function seaWaves(place, region, areas, count, seed) {
    const rules = window.WorldRules;
    const random = seeded(seed), out = [];
    for (let attempt = 0; attempt < count * 30 && out.length < count; attempt++) {
      const area = areas[Math.floor(random() * areas.length)];
      const point = { x: area.x0 + random() * (area.x1 - area.x0), y: area.y0 + random() * (area.y1 - area.y0) };
      if (rules && rules.isLand(point, region.id)) continue;
      if (Math.hypot(point.x - 1478, point.y - 118) < 100 || point.x > 1540 || point.y > 950 || point.y < 60) continue;
      if (out.some(other => Math.hypot(other.x - point.x, (other.y - point.y) * 2) < 60)) continue;
      out.push(point);
    }
    return out.map(point => `<use href="#ink-wave" transform="translate(${n(point.x)} ${n(point.y)})"/>`).join("");
  }
  function details(region, markup, compassAt = [1478, 118]) {
    const build = DETAILS[region.id];
    if (!build) return "";
    const labels = [...markup.matchAll(/<text x="(\d+)" y="(\d+)">([^<]+)<\/text>/g)].map(match => ({ x: Number(match[1]), y: Number(match[2]) - 6, w: match[3].length * 7.5 + 18 }));
    const clear = clearance(region, labels, compassAt);
    const place = options => scatter(clear, options);
    const extended = { ...region };
    if (["sunward-coast", "bloodstone-coast", "cinder-isle"].includes(region.id)) {
      const areas = region.id === "sunward-coast" ? [{ x0: 1340, y0: 60, x1: 1560, y1: 950 }] : region.id === "bloodstone-coast" ? [{ x0: 50, y0: 880, x1: 1550, y1: 960 }] : [{ x0: 40, y0: 50, x1: 1560, y1: 960 }];
      extended.seaWaves = seaWaves(place, region, areas, region.id === "cinder-isle" ? 26 : 20, 151);
    }
    if (region.id === "saffron-expanse") {
      const oasis = region.oasis;
      extended.oasisPalms = [0.2, 0.75, 1.3, 2.2, 2.75, 3.5, 4.1, 4.75, 5.5].map((angle, index) => {
        const point = { x: oasis.x + Math.cos(angle) * oasis.rx * 0.82, y: oasis.y + Math.sin(angle) * oasis.ry * 0.86 };
        return clear(point, [], 0.9, "ink-palm") ? { ...point, scale: 0.9 + (index % 3) * 0.15 } : null;
      }).filter(Boolean).sort((a, b) => a.y - b.y).map(point => `<use href="#ink-palm" transform="translate(${n(point.x)} ${n(point.y)}) scale(${n(point.scale)})"/>`).join("");
    }
    return `<g class="map-ink-details" pointer-events="none">${build(place, extended)}</g>`;
  }

  /* ---------------- Frame, compass and scale ---------------- */
  function compass(x, y) {
    const ticks = Array.from({ length: 32 }, (_, index) => {
      const angle = index / 32 * Math.PI * 2, inner = index % 4 ? 41 : 37;
      return `M${n(Math.sin(angle) * inner)} ${n(-Math.cos(angle) * inner)}L${n(Math.sin(angle) * 46)} ${n(-Math.cos(angle) * 46)}`;
    }).join("");
    // Each point is split into a lit and a shadowed half, as on an engraved rose.
    const point = (angle, length, width) => {
      const sin = Math.sin(angle), cos = Math.cos(angle);
      const tip = [sin * length, -cos * length], left = [-cos * width, -sin * width], right = [cos * width, sin * width];
      return `<path d="M0 0L${n(tip[0])} ${n(tip[1])}L${n(left[0])} ${n(left[1])}Z" fill="${BONE}" stroke="${INK}" stroke-width="1.2" stroke-linejoin="round"/><path d="M0 0L${n(tip[0])} ${n(tip[1])}L${n(right[0])} ${n(right[1])}Z" fill="#3a2a1d" stroke="${INK}" stroke-width="1.2" stroke-linejoin="round"/>`;
    };
    const diagonals = [1, 3, 5, 7].map(index => point(index * Math.PI / 4, 30, 7)).join("");
    const cardinals = [0, 2, 4, 6].map(index => point(index * Math.PI / 4, index === 0 ? 66 : 56, 10)).join("");
    return `<g class="map-compass" transform="translate(${x} ${y})" pointer-events="none">
      <circle r="46" fill="#2a1d14" fill-opacity=".18" stroke="${INK}" stroke-width="1.4"/>
      <circle r="40" fill="none" stroke="${INK}" stroke-width="2.6"/>
      <path d="${ticks}" stroke="${INK}" stroke-width="1.3"/>
      ${diagonals}${cardinals}
      <circle r="5" fill="${BONE}" stroke="${INK}" stroke-width="1.6"/>
      <text y="-74" class="map-compass-north">N</text>
    </g>`;
  }
  function scaleBar(x, y) {
    const segments = [0, 1, 2, 3].map(index => `<rect x="${index * 50}" y="-4" width="50" height="8" fill="${index % 2 ? BONE : INK}"/>`).join("");
    return `<g class="map-scale" transform="translate(${x} ${y})" pointer-events="none">${segments}<rect x="0" y="-4" width="200" height="8" fill="none" stroke="${INK}" stroke-width="1.6"/><text x="100" y="-14" class="map-scale-label">5 km · on foot</text></g>`;
  }
  function frame() {
    // A double ruled border with a small fleuron in each corner.
    const corner = (x, y, sx, sy) => `<g transform="translate(${x} ${y}) scale(${sx} ${sy})"><path d="M0 26V0H26" fill="none" stroke="${INK}" stroke-width="3"/><path d="M9 9l9 0 0 9Z" fill="${INK}"/><path d="M0 40q14 -4 14 -14M40 0q-4 14 -14 14" fill="none" stroke="${INK}" stroke-width="1.6"/><circle cx="16" cy="16" r="3.2" fill="${BONE}" stroke="${INK}" stroke-width="1.4"/></g>`;
    return `<g class="map-frame" pointer-events="none">
      <rect x="12" y="12" width="1576" height="976" fill="none" stroke="${INK}" stroke-width="4" opacity=".85"/>
      <rect x="21" y="21" width="1558" height="958" fill="none" stroke="${INK}" stroke-width="1.4" opacity=".7"/>
      ${corner(12, 12, 1, 1)}${corner(1588, 12, -1, 1)}${corner(12, 988, 1, -1)}${corner(1588, 988, -1, -1)}
    </g>`;
  }

  /* ---------------- Finishing a landscape ---------------- */
  const OLD = {
    paper: /<rect width="1600" height="1000" fill="url\(#map-paper\)"[^>]*\/>/g,
    vignette: /<rect width="1600" height="1000" fill="url\(#map-vignette\)"[^>]*\/>/g,
    compass: /<g transform="translate\((\d+) (\d+)\)" fill="none" stroke="#[0-9a-f]{6}" stroke-width="2"><circle r="38"\/>.*?<\/g>/s,
    scale: /<g transform="translate\(110 938\)"[^>]*>.*?<\/g>/s,
    frame: /<rect x="17" y="17" width="1566" height="966"[^>]*\/>/g,
  };
  function finish(markup, region = null) {
    const compassMatch = OLD.compass.exec(markup);
    const hasScale = OLD.scale.test(markup);
    let body = markup.replace(OLD.paper, "").replace(OLD.vignette, "").replace(OLD.compass, "").replace(OLD.scale, "").replace(OLD.frame, "");
    body = toneMarkup(body);
    // Broad zone washes fade out at their rims instead of ending in a hard oval.
    const washes = new Map();
    body = body.replace(/<ellipse ([^>]*?)fill="(#[0-9a-f]{6})" opacity="([.\d]+)"\/>/g, (match, attributes, hex, opacity) => {
      if (Number(/rx="(\d+)"/.exec(attributes)?.[1] || 0) < 120) return match;
      washes.set("wash-" + hex.slice(1), hex);
      return `<ellipse ${attributes}fill="url(#wash-${hex.slice(1)})" opacity="${opacity}"/>`;
    });
    body = `<defs>${[...washes].map(([id, hex]) => `<radialGradient id="${id}"><stop offset=".45" stop-color="${hex}"/><stop offset=".82" stop-color="${hex}" stop-opacity=".7"/><stop offset="1" stop-color="${hex}" stop-opacity="0"/></radialGradient>`).join("")}</defs>` + body;
    const [cx, cy] = compassMatch ? [Number(compassMatch[1]), Number(compassMatch[2])] : [1478, 118];
    // Terrain symbols and the paper go over the land but under the lettering, towns and markers.
    const sheet = (region ? details(region, markup, [cx, cy]) : "") + `<image class="map-paper-sheet" href="${paper()}" width="1600" height="1000" preserveAspectRatio="none" pointer-events="none"/>`;
    const cut = body.search(/<g class="map-terrain-label"|<text class="map-river-label"|<g class="map-settlement"|<g data-map-site/);
    body = cut < 0 ? body + sheet : body.slice(0, cut) + sheet + body.slice(cut);
    return body + (hasScale ? scaleBar(110, 938) : "") + compass(cx, cy) + frame();
  }

  window.MapInk = { INK, BONE, tone, toneMarkup, paper, defs, compass, scaleBar, frame, finish, details };
})();
