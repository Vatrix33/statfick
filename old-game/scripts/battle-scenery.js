/* Battle backgrounds. Every region and encounter zone maps to a biome; each
   biome is drawn as layered SVG (sky, far hills, middle details, ground and
   foreground) with seeded variation, the world clock's time of day, and
   ambient motion such as drifting clouds, rain, snow, embers or fireflies. */
(() => {
  "use strict";
  const K = window.SpriteKit;
  const n = value => Math.round(value * 10) / 10;
  const W = 1600, H = 600, HORIZON = 285;

  /* ---------------- Biomes ---------------- */
  const BIOMES = {
    plains: { sky: ["#86c2e6", "#e3f2f2"], far: "#9fbf8a", mid: "#7fa86a", ground: ["#8fb46a", "#6f9a4f"], detail: "#5f8a42", trees: "round", treeColor: ["#5f8f45", "#4f7f3a"] },
    steppe: { sky: ["#9ccbe6", "#f1ecd4"], far: "#c4b98a", mid: "#b0a876", ground: ["#c4b882", "#a89c62"], detail: "#8f8452", trees: "sparse", treeColor: ["#7f8f4a", "#6a7a3a"] },
    hills: { sky: ["#8ec2e2", "#e6f0ea"], far: "#8fae84", mid: "#7a9e6a", ground: ["#86aa62", "#6a9050"], detail: "#58803f", trees: "round", treeColor: ["#5a8a45", "#4a7a3a"], hills: true },
    forest: { sky: ["#8fc0d8", "#e2eedf"], far: "#5f8a5a", mid: "#4f7a45", ground: ["#6f9450", "#557a3c"], detail: "#476a32", trees: "forest", treeColor: ["#4a7a3c", "#3f6a32"] },
    deepforest: { sky: ["#6a8f90", "#b8ccb4"], far: "#3f5f40", mid: "#36533a", ground: ["#4f6f3c", "#3f5a2f"], detail: "#334c28", trees: "forest", treeColor: ["#2f5530", "#284a2a"], dim: 0.22, fireflies: true },
    jungle: { sky: ["#8cc8c0", "#e0f0d6"], far: "#3f7f4a", mid: "#2f6a3c", ground: ["#4f8a3f", "#3a6f30"], detail: "#2f5f28", trees: "jungle", treeColor: ["#3a8a3f", "#2f7a35"], vines: true },
    marsh: { sky: ["#9aaeb4", "#d6dcc8"], far: "#7a8a6a", mid: "#64755a", ground: ["#6f7f55", "#5a6a45"], detail: "#4f5f3a", trees: "dead", treeColor: ["#4a4436", "#3f3a2f"], pools: "#5f7a7a", reeds: true, mist: true },
    frozenmarsh: { sky: ["#a8bccc", "#e8eef2"], far: "#8a9aa8", mid: "#7a8a96", ground: ["#c8d4dc", "#a8b8c4"], detail: "#8a9aa8", trees: "dead", treeColor: ["#5a5a5f", "#4a4a50"], pools: "#a8c8d8", reeds: true, snow: true },
    river: { sky: ["#86c2e6", "#e3f2f2"], far: "#9fbf8a", mid: "#7fa86a", ground: ["#8fb46a", "#6f9a4f"], detail: "#5f8a42", trees: "round", treeColor: ["#5f8f45", "#4f7f3a"], river: "#5f9fc8" },
    lake: { sky: ["#8cc4e8", "#eef6f4"], far: "#8fae9a", mid: "#7a9a84", ground: ["#a8b87a", "#8a9a5f"], detail: "#6f7f4a", trees: "sparse", treeColor: ["#5f8f55", "#4f7f45"], lake: "#5f9fc8", reeds: true },
    mountain: { sky: ["#8eb8dc", "#e6eef2"], far: "#8a94a0", mid: "#7a7f86", ground: ["#9a948a", "#7f786e"], detail: "#655f56", trees: "pine", treeColor: ["#4a6a4f", "#3f5a42"], peaks: "rock", rocks: true },
    cliffs: { sky: ["#8eb8dc", "#e6eef2"], far: "#8a94a0", mid: "#6f747c", ground: ["#8f8a80", "#77726a"], detail: "#5f5a52", trees: "none", peaks: "rock", rocks: true, cliffs: true },
    snow: { sky: ["#9cc0e2", "#f2f6fa"], far: "#c8d4e2", mid: "#aebccc", ground: ["#eef2f6", "#d0dae4"], detail: "#b8c4d0", trees: "snowpine", treeColor: ["#3f5a4a", "#34503f"], peaks: "snow", snow: true },
    summit: { sky: ["#7fa8d8", "#e8f0fa"], far: "#d8e2ee", mid: "#c0ccdc", ground: ["#f2f6fa", "#d8e2ec"], detail: "#c0ccd8", trees: "none", peaks: "snow", snow: true, clouds: "below" },
    beach: { sky: ["#7fc4ec", "#eef8f6"], far: "#5fa8c8", mid: "#e8d8a8", ground: ["#ecdcae", "#d8c48c"], detail: "#c8b47a", trees: "palm", treeColor: ["#5f9f4a", "#4f8a3f"], sea: "#4f9fc8" },
    coastplains: { sky: ["#80c2ea", "#eaf6f2"], far: "#5fa8c8", mid: "#9fbf7a", ground: ["#9fc072", "#86a85a"], detail: "#6f9446", trees: "sparse", treeColor: ["#5f8f45", "#4f7f3a"], sea: "#5aa4c8" },
    blackshore: { sky: ["#8a8f9c", "#d8c8b8"], far: "#4f6f80", mid: "#3a3634", ground: ["#4a4644", "#36322f"], detail: "#2a2624", trees: "none", sea: "#4a7f9a", embers: true },
    volcanic: { sky: ["#5a4648", "#d8946a"], far: "#4a3a3a", mid: "#3a2e2c", ground: ["#4a3a34", "#36291f"], detail: "#2a1f19", trees: "dead", treeColor: ["#2a2220", "#221a18"], volcano: true, lava: true, embers: true, dim: 0.1 },
    desert: { sky: ["#94cdec", "#f6e6c4"], far: "#e0be82", mid: "#d6b06e", ground: ["#e6c88a", "#d2ae6a"], detail: "#c0985a", trees: "cactus", treeColor: ["#6f9a5a", "#5f8a4a"], dunes: true, sand: true },
    saltflats: { sky: ["#9ccdec", "#f6f2e4"], far: "#e8dcc4", mid: "#e0d4ba", ground: ["#f2ece0", "#e2d8c4"], detail: "#d0c4ac", trees: "none", cracks: true },
    obsidian: { sky: ["#8aa8c4", "#e8d8c0"], far: "#5a5260", mid: "#3a3440", ground: ["#3a3440", "#2a2530"], detail: "#5a4a6a", trees: "none", rocks: true, glass: true },
    oasis: { sky: ["#94cdec", "#f6ecd0"], far: "#e0be82", mid: "#d6b06e", ground: ["#e6c88a", "#d2ae6a"], detail: "#c0985a", trees: "palm", treeColor: ["#5f9f4a", "#4f8a3f"], lake: "#4fa8c0", dunes: true },
    ruins: { sky: ["#c8a87a", "#f6e2b8"], far: "#d0aa6a", mid: "#b8935a", ground: ["#d8b47a", "#c09a5f"], detail: "#a8844a", trees: "none", dunes: true, pyramid: true, ruins: "sand", sand: true },
    redwaste: { sky: ["#c89a84", "#f6d8b4"], far: "#c4705a", mid: "#a8583f", ground: ["#c8744f", "#a85a3a"], detail: "#8a4a30", trees: "none", dunes: true, sand: true, rocks: true },
    redcoast: { sky: ["#a88a8a", "#f2c8b0"], far: "#5f6f80", mid: "#8a3a30", ground: ["#9a4a38", "#7a3628"], detail: "#5f2a20", trees: "dead", treeColor: ["#3a2220", "#2f1a18"], sea: "#6a4a5a", rocks: true },
    citadel: { sky: ["#3a2a3a", "#a86a6a"], far: "#4a2a30", mid: "#3a1f26", ground: ["#5a3034", "#40222a"], detail: "#2f181e", trees: "dead", treeColor: ["#2a1a1c", "#221416"], castle: "citadel", dim: 0.3 },
    battlefield: { sky: ["#8a8a92", "#d8ccb8"], far: "#7a7468", mid: "#6a6256", ground: ["#7a6f5a", "#5f5646"], detail: "#4f4636", trees: "dead", treeColor: ["#3f3a32", "#36312a"], battle: true, mist: true },
    boreal: { sky: ["#8aa8c4", "#e0eaf0"], far: "#4f6a6a", mid: "#3f5a5a", ground: ["#d8e2e8", "#b8c8d2"], detail: "#9aaebc", trees: "snowpine", treeColor: ["#2f4a44", "#263f3a"], snow: true },
    darkboreal: { sky: ["#3a4a64", "#8a9ab4"], far: "#2a3a40", mid: "#223236", ground: ["#9aaab8", "#7a8a9a"], detail: "#5a6a7a", trees: "snowpine", treeColor: ["#1f3530", "#1a2e2a"], snow: true, dim: 0.25, fireflies: true },
    storm: { sky: ["#4f5a6e", "#9aa4b4"], far: "#5f6f6a", mid: "#4f5f58", ground: ["#6a7f5a", "#566a48"], detail: "#465a3a", trees: "sparse", treeColor: ["#3f5a3a", "#344f30"], rain: true, lightning: true, hills: true, dim: 0.12 },
    rainhills: { sky: ["#6f7f90", "#c0c8cc"], far: "#7a8f80", mid: "#66806a", ground: ["#76955a", "#5f7f48"], detail: "#4f6a3a", trees: "round", treeColor: ["#4f7a45", "#456f3a"], rain: true, hills: true },
    temple: { sky: ["#4f5a6e", "#9aa4b4"], far: "#5f6f6a", mid: "#4f5f58", ground: ["#8a8a80", "#6f6f66"], detail: "#5a5a52", trees: "none", rain: true, lightning: true, temple: true, dim: 0.12 },
    town: { sky: ["#86c2e6", "#e3f2f2"], far: "#9fbf8a", mid: "#7fa86a", ground: ["#b8a88a", "#9a8a6e"], detail: "#7f705a", trees: "round", treeColor: ["#5f8f45", "#4f7f3a"], town: true, cobbles: true },
  };

  const REGION_BASE = {
    "alder-vale": "plains", "stoneveil-foothills": "hills", "high-stoneveil": "snow", "stoneveil-summit": "summit", "verdant-reach": "forest",
    "stormwake-hills": "rainhills", "gloamfen-marsh": "marsh", "verdigris-jungle": "jungle", "mirrormere-basin": "lake", "saffron-expanse": "desert",
    "deep-desert": "desert", "crimson-divide": "redwaste", "bloodstone-coast": "redcoast", "ancient-battlefield": "battlefield", "duskpine-forest": "boreal",
    "sunward-coast": "coastplains", "cinder-isle": "volcanic",
  };
  const ZONES = {
    "Plains - near Town": "plains", "Plains": "plains", "River": "river", "Forest": "forest", "Deep Forest": "deepforest", "Outer Forest": "forest",
    "Hill-Plains - near Town": "hills", "River - near Town": "river", "Mountainfoot - near Town": "mountain", "Hill-Plains": "hills", "Mountainfoot": "mountain",
    "Lower Slopes": "mountain", "Cliffside": "cliffs", "High Pass": "snow", "Frozen Heights": "snow", "Dragonspine": "snow",
    "Windcut Ascent": "summit", "Giant Steps": "summit", "Cloudbreak Ridge": "summit", "Dragon Eyries": "summit", "Frozen Crown": "summit",
    "Coastal Plains": "coastplains", "Windswept Plains": "coastplains", "Salt Marsh": "marsh", "Dunes": "beach", "Beach": "beach", "Open Sea": "beach",
    "Black Sand Shore": "blackshore", "Ember Scrub": "volcanic", "Ashen Slope": "volcanic", "Lava Fields": "volcanic", "Caldera": "volcanic",
    "Church of Talos": "temple", "Rainwashed Downs": "rainhills", "Floodplain": "river", "Thunder Hills": "storm", "Stormcrest": "storm",
    "Open Mire": "marsh", "Reedwater": "marsh", "Deep Mire": "marsh", "Rocky Rise": "marsh",
    "Jungle Verge": "jungle", "Outer Jungle": "jungle", "Dense Jungle": "jungle", "High Canopy": "jungle", "Old Growth": "jungle",
    "Open Water": "lake", "Lake Shore": "lake", "Reed Marsh": "marsh", "Windcut Bluffs": "cliffs", "Dry Steppe": "steppe", "Desert Fringe": "desert",
    "Oasis Grove": "oasis", "Sunburnt Flats": "desert", "Salt Pans": "saltflats", "Shifting Dunes": "desert", "Glass Dunes": "desert",
    "Outer Wastes": "desert", "Howling Dunes": "desert", "Obsidian Flats": "obsidian", "Buried Necropolis": "ruins", "Pyramid Sanctum": "ruins",
    "Caravan Dunes": "desert", "Open Desert": "desert", "Sunscorched Escarpment": "redwaste", "Crimson Dunes": "redwaste", "Bloodglass Flats": "redwaste", "Heart of the Red Waste": "redwaste",
    "Redwind Dunes": "redwaste", "Western Crags": "redcoast", "Sanguine Wastes": "redwaste", "Giant Rock Spires": "redcoast", "Blood Coast": "redcoast", "Vampire Citadel": "citadel",
    "Scavenger Verge": "battlefield", "Bone Pits": "battlefield", "Shattered Causeway": "battlefield", "Mass Graves": "battlefield", "Bloodied Trenches": "battlefield", "Silent Westfield": "battlefield",
    "Frostpine Fringe": "boreal", "Blackpine Woods": "boreal", "Deep Boreal": "boreal", "Frostbound Bog": "frozenmarsh", "Shadowroot Hollow": "darkboreal",
  };

  function biomeKey(regionId, zone) {
    if (zone === "Town") return "town";
    if (zone && ZONES[zone]) return ZONES[zone];
    return REGION_BASE[regionId] || "plains";
  }
  // Towns and roads take on their region's landscape, with houses or a road.
  function biomeFor(regionId, zone) {
    const key = biomeKey(regionId, zone);
    const regional = BIOMES[REGION_BASE[regionId] || "plains"] || BIOMES.plains;
    if (zone === "Town") return { ...regional, town: true, cobbles: true, ground: [K.mix(regional.ground[0], "#b8a88a", 0.55), K.mix(regional.ground[1], "#9a8a6e", 0.55)], rain: regional.rain, lightning: false, volcano: false, lava: false, pyramid: false, castle: null, battle: false, temple: false, trees: regional.trees === "forest" ? "round" : regional.trees };
    if (zone === "Road") return { ...regional, road: true, battle: false };
    return BIOMES[key] || BIOMES.plains;
  }

  function timeOfDay(minutes) {
    const hour = ((((Number(minutes) || 0) % 1440) + 1440) % 1440) / 60;
    if (hour >= 6.5 && hour < 17.5) return "day";
    if (hour >= 17.5 && hour < 20) return "dusk";
    if (hour >= 5 && hour < 6.5) return "dawn";
    return "night";
  }

  /* ---------------- Drawing helpers ---------------- */
  const poly = (points, fill, extra = "") => `<path d="M${points.map(([x, y]) => `${n(x)} ${n(y)}`).join(" L")}Z" fill="${fill}" ${extra}/>`;
  function ridge(rand, y, height, roughness, count, fill, extra = "") {
    const points = [[0, H]];
    for (let i = 0; i <= count; i++) points.push([i * W / count, y - rand() * height - (i % 2 ? roughness : 0)]);
    points.push([W, H]);
    return poly(points, fill, extra);
  }
  function hills(rand, y, height, count, fill, extra = "") {
    let d = `M0 ${H} L0 ${n(y)}`;
    const step = W / count;
    for (let i = 0; i < count; i++) {
      const x = i * step, peak = y - height * (0.5 + rand() * 0.5);
      d += ` Q${n(x + step * 0.5)} ${n(peak - height * 0.4)} ${n(x + step)} ${n(y - rand() * height * 0.3)}`;
    }
    return `<path d="${d} L${W} ${H}Z" fill="${fill}" ${extra}/>`;
  }
  function peaks(rand, b, snowy) {
    let out = "";
    for (let layer = 0; layer < 2; layer++) {
      const color = layer ? b.mid : b.far;
      const base = HORIZON - layer * 4;
      let x = -100 + rand() * 80;
      while (x < W + 100) {
        const width = 240 + rand() * 260, height = 130 + rand() * 120 - layer * 50;
        const top = base - height;
        out += poly([[x, base + 20], [x + width * 0.45, top], [x + width * 0.55, top + 8], [x + width, base + 20]], color);
        if (snowy || b.peaks === "snow") out += poly([[x + width * 0.3, top + height * 0.33], [x + width * 0.45, top], [x + width * 0.55, top + 8], [x + width * 0.7, top + height * 0.36], [x + width * 0.58, top + height * 0.3], [x + width * 0.5, top + height * 0.4], [x + width * 0.42, top + height * 0.28]], "#f6f8fc", 'opacity=".95"');
        else out += poly([[x + width * 0.45, top], [x + width * 0.55, top + 8], [x + width * 0.62, top + height * 0.5], [x + width * 0.5, top + height * 0.35]], "rgba(255,255,255,.12)");
        x += width * (0.55 + rand() * 0.3);
      }
    }
    return out;
  }
  function tree(kind, x, y, scale, colors, rand) {
    const [c1, c2] = colors || ["#5f8f45", "#4f7f3a"];
    const trunk = "#5a3f2a";
    const s = scale;
    if (kind === "round") return `<g transform="translate(${n(x)} ${n(y)}) scale(${n(s)})"><rect x="-5" y="-40" width="10" height="42" rx="3" fill="${trunk}"/><circle cx="0" cy="-58" r="30" fill="${c2}"/><circle cx="-14" cy="-48" r="20" fill="${c1}"/><circle cx="12" cy="-66" r="20" fill="${c1}"/><circle cx="6" cy="-46" r="16" fill="${c2}" opacity=".8"/></g>`;
    if (kind === "pine" || kind === "snowpine" || kind === "forest") {
      const snow = kind === "snowpine";
      let tiers = "";
      for (let i = 0; i < 4; i++) {
        const w = 44 - i * 9, top = -26 - i * 22;
        tiers += poly([[-w, top + 30], [0, top - 14], [w, top + 30]], i % 2 ? c1 : c2);
        if (snow) tiers += poly([[-w * 0.55, top + 14], [0, top - 14], [w * 0.55, top + 14], [w * 0.2, top + 8], [-w * 0.15, top + 12]], "#f2f6fa");
      }
      return `<g transform="translate(${n(x)} ${n(y)}) scale(${n(s)})"><rect x="-5" y="-26" width="10" height="28" fill="${trunk}"/>${tiers}</g>`;
    }
    if (kind === "palm") {
      const fronds = [-70, -35, 0, 35, 70, 110, -110].map(angle => `<path d="M0 -92 q${n(Math.sin(angle * Math.PI / 180) * 30)} -18 ${n(Math.sin(angle * Math.PI / 180) * 62)} ${n(-Math.cos(angle * Math.PI / 180) * 18 + 16)}" stroke="${c1}" stroke-width="9" fill="none" stroke-linecap="round"/>`).join("");
      return `<g transform="translate(${n(x)} ${n(y)}) scale(${n(s)})"><path d="M-4 0 Q-10 -50 0 -92 L6 -92 Q-2 -50 5 0Z" fill="#8a6a42"/>${fronds}<circle cx="2" cy="-90" r="5" fill="#6a4a2a"/></g>`;
    }
    if (kind === "jungle") return `<g transform="translate(${n(x)} ${n(y)}) scale(${n(s)})"><path d="M-7 0 Q-9 -60 -3 -110 L4 -110 Q2 -60 7 0Z" fill="#6a4a30"/><ellipse cx="0" cy="-120" rx="58" ry="30" fill="${c2}"/><ellipse cx="-30" cy="-104" rx="34" ry="20" fill="${c1}"/><ellipse cx="32" cy="-108" rx="32" ry="18" fill="${c1}"/><path d="M-30 -100 q-6 40 2 70 M24 -98 q8 36 -2 60" stroke="#3f7a35" stroke-width="3" fill="none"/></g>`;
    if (kind === "dead") return `<g transform="translate(${n(x)} ${n(y)}) scale(${n(s)})"><path d="M-4 0 L-3 -50 L-18 -76 M-3 -50 L4 -62 L14 -88 M4 -62 L22 -70 M-10 -64 L-22 -66" stroke="${c1}" stroke-width="5" fill="none" stroke-linecap="round"/></g>`;
    if (kind === "cactus") return `<g transform="translate(${n(x)} ${n(y)}) scale(${n(s)})"><path d="M-5 0 V-56 a5 5 0 0 1 10 0 V0Z M5 -30 h8 v-14 a4 4 0 0 1 8 0 v18 q0 4 -4 4 h-12Z M-5 -22 h-7 v-10 a4 4 0 0 0 -8 0 v14 q0 4 4 4 h11Z" fill="${c1}"/></g>`;
    if (kind === "sparse") return tree("round", x, y, scale * 0.8, colors, rand);
    return "";
  }
  function treeLine(rand, b, y, count, scale, colorShift = 0) {
    if (!b.trees || b.trees === "none") return "";
    const colors = b.treeColor && colorShift ? b.treeColor.map(color => K.darken(color, colorShift)) : b.treeColor;
    let out = "";
    const kind = b.trees === "forest" ? (rand() < 0.5 ? "pine" : "round") : b.trees;
    const total = b.trees === "sparse" || b.trees === "cactus" || b.trees === "palm" || b.trees === "dead" ? Math.ceil(count * 0.35) : count;
    for (let i = 0; i < total; i++) {
      const x = (i + rand() * 0.8) * W / total;
      out += tree(b.trees === "forest" ? (rand() < 0.5 ? "pine" : "round") : kind, x, y + rand() * 10, scale * (0.8 + rand() * 0.4), colors, rand);
    }
    return out;
  }
  function rocks(rand, y, count, color) {
    let out = "";
    for (let i = 0; i < count; i++) {
      const x = rand() * W, s = 10 + rand() * 26, yy = y + rand() * 40;
      out += poly([[x - s, yy], [x - s * 0.6, yy - s * 0.8], [x + s * 0.2, yy - s], [x + s, yy - s * 0.35], [x + s * 0.9, yy]], color);
      out += poly([[x - s * 0.6, yy - s * 0.8], [x + s * 0.2, yy - s], [x, yy - s * 0.5]], "rgba(255,255,255,.14)");
    }
    return out;
  }
  function tufts(rand, color, count, yMin = HORIZON + 30) {
    let d = "";
    for (let i = 0; i < count; i++) {
      const x = rand() * W, y = yMin + rand() * (H - yMin), s = 4 + (y - HORIZON) / 50;
      d += `M${n(x)} ${n(y)} l${n(-s * 0.6)} ${n(-s * 2)} M${n(x)} ${n(y)} l0 ${n(-s * 2.6)} M${n(x)} ${n(y)} l${n(s * 0.7)} ${n(-s * 1.8)} `;
    }
    return `<path d="${d}" stroke="${color}" stroke-width="2" stroke-linecap="round" fill="none" opacity=".7"/>`;
  }
  function houses(rand, y) {
    let out = "";
    for (let i = 0; i < 6; i++) {
      const x = 60 + i * 270 + rand() * 60, w = 90 + rand() * 50, h = 60 + rand() * 30;
      const wall = ["#e6d8bc", "#d8c8a8", "#efe2c8"][i % 3], roof = ["#a8543a", "#7a4a3a", "#8a6a4a"][i % 3];
      out += `<rect x="${n(x)}" y="${n(y - h)}" width="${n(w)}" height="${n(h)}" fill="${wall}"/>${poly([[x - 10, y - h], [x + w / 2, y - h - 46], [x + w + 10, y - h]], roof)}<rect x="${n(x + w * 0.2)}" y="${n(y - h * 0.65)}" width="16" height="18" fill="#5a6a7a"/><rect x="${n(x + w * 0.62)}" y="${n(y - h * 0.55)}" width="18" height="${n(h * 0.55)}" fill="#6a4a30"/>`;
    }
    return out;
  }
  function castle(y, kind = "castle") {
    const dark = kind === "citadel" ? "#2a1820" : "#7a7a80", window = kind === "citadel" ? "#ff6a5a" : "#3a3a40";
    let out = `<rect x="560" y="${y - 150}" width="480" height="150" fill="${dark}"/>`;
    for (const [x, h] of [[520, 210], [700, 250], [860, 260], [1040, 210]]) out += `<rect x="${x}" y="${y - h}" width="70" height="${h}" fill="${dark}"/>${poly([[x - 10, y - h], [x + 35, y - h - 60], [x + 80, y - h]], kind === "citadel" ? "#1a1016" : "#5a5a62")}<rect x="${x + 28}" y="${y - h + 40}" width="12" height="20" fill="${window}" opacity=".9"/>`;
    return out;
  }
  function battleDebris(rand, y) {
    let out = "";
    for (let i = 0; i < 9; i++) {
      const x = 40 + rand() * (W - 80), yy = y + 30 + rand() * 200, tilt = -25 + rand() * 50;
      out += `<g transform="translate(${n(x)} ${n(yy)}) rotate(${n(tilt)})"><rect x="-2" y="-46" width="4" height="46" fill="#8a8a90"/><rect x="-9" y="-12" width="18" height="4" fill="#5a4a3a"/></g>`;
    }
    for (let i = 0; i < 5; i++) {
      const x = 80 + rand() * (W - 160), yy = y + 20 + rand() * 120;
      out += `<g transform="translate(${n(x)} ${n(yy)})"><rect x="-1.5" y="-80" width="3" height="80" fill="#4a3a2a"/><path d="M1.5 -78 h34 l-8 12 l8 12 h-34Z" fill="${["#7a2a2a", "#3a4a6a", "#6a5a2a"][i % 3]}" opacity=".85"/></g>`;
    }
    for (let i = 0; i < 7; i++) out += `<ellipse cx="${n(rand() * W)}" cy="${n(y + 80 + rand() * 200)}" rx="${n(8 + rand() * 6)}" ry="4" fill="#e8e0cc" opacity=".8"/>`;
    return out;
  }

  /* ---------------- Composition ---------------- */
  const cache = new Map();
  function render(regionId, zone, minutes) {
    const key = biomeKey(regionId, zone), tod = timeOfDay(minutes);
    const cacheKey = [regionId, zone, tod].join("|");
    if (cache.has(cacheKey)) return cache.get(cacheKey);
    const b = biomeFor(regionId, zone);
    const rand = K.seeded(regionId + "|" + zone);
    const out = [];
    const night = tod === "night", dusk = tod === "dusk", dawn = tod === "dawn";
    const skyTop = night ? "#0e1530" : dusk ? K.mix(b.sky[0], "#5a4a7a", 0.55) : dawn ? K.mix(b.sky[0], "#8a7aa8", 0.4) : b.sky[0];
    const skyBottom = night ? "#2a3458" : dusk ? K.mix(b.sky[1], "#f2a06a", 0.6) : dawn ? K.mix(b.sky[1], "#f6c0b0", 0.5) : b.sky[1];
    out.push(`<defs><linearGradient id="bsc-sky" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="${skyTop}"/><stop offset="1" stop-color="${skyBottom}"/></linearGradient><linearGradient id="bsc-ground" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="${b.ground[0]}"/><stop offset="1" stop-color="${b.ground[1]}"/></linearGradient><radialGradient id="bsc-vignette" cx=".5" cy=".55" r=".75"><stop offset=".6" stop-color="#000" stop-opacity="0"/><stop offset="1" stop-color="#000" stop-opacity=".35"/></radialGradient></defs>`);
    out.push(`<rect width="${W}" height="${H}" fill="url(#bsc-sky)"/>`);
    // Sun, moon and stars.
    if (night) {
      let stars = "";
      for (let i = 0; i < 70; i++) stars += `<circle cx="${n(rand() * W)}" cy="${n(rand() * HORIZON * 0.85)}" r="${n(0.6 + rand() * 1.3)}" fill="#fff" opacity="${n(0.4 + rand() * 0.6)}"/>`;
      out.push(`<g class="bsc-stars">${stars}</g><circle cx="1260" cy="90" r="34" fill="#f2efe0"/><circle cx="1274" cy="82" r="30" fill="${skyTop}" opacity=".85"/>`);
    } else {
      const sunY = dusk || dawn ? HORIZON - 40 : 92, sunX = dusk ? 1180 : dawn ? 360 : 1240;
      out.push(`<circle cx="${sunX}" cy="${sunY}" r="${dusk || dawn ? 58 : 44}" fill="${dusk ? "#ffb070" : dawn ? "#ffd0a0" : "#fff6d6"}" opacity=".9"/><circle cx="${sunX}" cy="${sunY}" r="${dusk || dawn ? 110 : 90}" fill="${dusk ? "#ffb070" : "#fff6d6"}" opacity=".18"/>`);
    }
    // Clouds drift slowly.
    if (b.clouds !== "none") {
      let clouds = "";
      const count = b.rain || b.lightning ? 9 : 5;
      const cloudColor = b.rain || b.lightning ? "#7a8294" : night ? "#3a4466" : "#ffffff";
      for (let i = 0; i < count; i++) {
        const x = rand() * W, y = 30 + rand() * (b.clouds === "below" ? 60 : 140), s = 0.7 + rand() * 0.9;
        clouds += `<g transform="translate(${n(x)} ${n(y)}) scale(${n(s)})" opacity="${b.rain ? 0.92 : 0.82}"><ellipse cx="0" cy="0" rx="70" ry="20" fill="${cloudColor}"/><ellipse cx="-28" cy="-12" rx="34" ry="20" fill="${cloudColor}"/><ellipse cx="22" cy="-16" rx="40" ry="24" fill="${cloudColor}"/></g>`;
      }
      out.push(`<g class="bsc-clouds">${clouds}</g><g class="bsc-clouds bsc-clouds-copy">${clouds}</g>`);
    }
    // Far layer.
    if (b.volcano) out.push(`<path d="M520 ${HORIZON + 10} L760 120 L840 120 L1080 ${HORIZON + 10}Z" fill="${b.far}"/><path d="M760 120 L840 120 L820 140 L780 140Z" fill="#ff7a2e" class="bsc-glow"/><path d="M790 122 q-30 -50 10 -100 q10 40 30 20 q-10 50 -40 80Z" fill="#6a5a5a" opacity=".55" class="bsc-smoke"/>`);
    if (b.peaks) out.push(peaks(rand, b, b.snow));
    else if (b.hills || ["plains", "river", "forest", "deepforest", "storm", "rainhills", "town", "steppe", "coastplains"].includes(key)) out.push(hills(rand, HORIZON + 6, 70, 7, b.far), hills(rand, HORIZON + 14, 40, 9, b.mid, 'opacity=".9"'));
    else if (b.dunes) out.push(hills(rand, HORIZON + 8, 46, 5, b.far), hills(rand, HORIZON + 16, 30, 7, b.mid));
    else out.push(ridge(rand, HORIZON + 6, 40, 12, 12, b.far));
    if (b.sea) out.push(`<rect x="0" y="${HORIZON - 6}" width="${W}" height="60" fill="${b.sea}"/><path class="bsc-waves" d="M0 ${HORIZON + 6} q40 -6 80 0 t80 0 t80 0 t80 0 t80 0 t80 0 t80 0 t80 0 t80 0 t80 0 t80 0 t80 0 t80 0 t80 0 t80 0 t80 0 t80 0 t80 0 t80 0 t80 0 t80 0" stroke="#ffffff" stroke-opacity=".5" stroke-width="2" fill="none"/>`);
    if (b.pyramid) out.push(poly([[900, HORIZON + 10], [1120, HORIZON - 180], [1340, HORIZON + 10]], "#c8a060"), poly([[1120, HORIZON - 180], [1340, HORIZON + 10], [1180, HORIZON + 10]], "#a88048"), poly([[300, HORIZON + 10], [400, HORIZON - 80], [500, HORIZON + 10]], "#c8a060", 'opacity=".8"'));
    if (b.castle) out.push(castle(HORIZON + 10, b.castle));
    if (b.temple) out.push(`<g><rect x="640" y="${HORIZON - 130}" width="320" height="140" fill="#6a6a70"/>${poly([[620, HORIZON - 130], [800, HORIZON - 210], [980, HORIZON - 130]], "#5a5a62")}${[0, 1, 2, 3, 4].map(i => `<rect x="${660 + i * 64}" y="${HORIZON - 120}" width="18" height="120" fill="#8a8a90"/>`).join("")}<rect x="785" y="${HORIZON - 290}" width="30" height="90" fill="#5a5a62"/><path d="M800 ${HORIZON - 300} l-6 14 h5 l-4 14 l12 -18 h-5 l5 -10Z" fill="#ffe66b"/></g>`);
    if (b.town) out.push(houses(rand, HORIZON + 26));
    if (b.ruins) for (let i = 0; i < 5; i++) { const x = 120 + i * 320 + rand() * 60, h = 40 + rand() * 70; out.push(`<rect x="${n(x)}" y="${n(HORIZON + 20 - h)}" width="22" height="${n(h)}" fill="#c8a870"/><rect x="${n(x - 6)}" y="${n(HORIZON + 14 - h)}" width="34" height="8" fill="#b8985f"/>`); }
    out.push(treeLine(rand, b, HORIZON + 26, 16, 0.8, 0.15));
    // Ground plane.
    out.push(`<path d="M0 ${HORIZON + 14} Q${W * 0.3} ${HORIZON + 4} ${W * 0.55} ${HORIZON + 16} T${W} ${HORIZON + 10} L${W} ${H} L0 ${H}Z" fill="url(#bsc-ground)"/>`);
    if (b.river) out.push(`<path d="M${W * 0.62} ${HORIZON + 14} Q${W * 0.55} ${HORIZON + 120} ${W * 0.7} ${HORIZON + 200} T${W * 0.6} ${H} L${W * 0.78} ${H} Q${W * 0.86} ${HORIZON + 210} ${W * 0.74} ${HORIZON + 120} T${W * 0.66} ${HORIZON + 14}Z" fill="${b.river}" opacity=".9"/><path class="bsc-shimmer" d="M${W * 0.64} ${HORIZON + 60} q20 4 40 0 M${W * 0.7} ${HORIZON + 160} q24 4 48 0 M${W * 0.68} ${HORIZON + 250} q28 5 56 0" stroke="#ffffff" stroke-opacity=".6" stroke-width="2" fill="none"/>`);
    if (b.lake) out.push(`<ellipse cx="${W * 0.5}" cy="${HORIZON + 30}" rx="${W * 0.42}" ry="34" fill="${b.lake}" opacity=".95"/><path class="bsc-shimmer" d="M${W * 0.3} ${HORIZON + 24} q30 4 60 0 M${W * 0.55} ${HORIZON + 36} q30 4 60 0 M${W * 0.7} ${HORIZON + 22} q24 4 48 0" stroke="#ffffff" stroke-opacity=".6" stroke-width="2" fill="none"/>`);
    if (b.pools) for (let i = 0; i < 5; i++) out.push(`<ellipse cx="${n(rand() * W)}" cy="${n(HORIZON + 50 + rand() * 230)}" rx="${n(60 + rand() * 90)}" ry="${n(10 + rand() * 12)}" fill="${b.pools}" opacity=".75"/>`);
    if (b.lava) for (let i = 0; i < 3; i++) out.push(`<path class="bsc-glow" d="M${n(rand() * W)} ${H} Q${n(rand() * W)} ${n(HORIZON + 150)} ${n(rand() * W)} ${n(HORIZON + 40)}" stroke="#ff7a2e" stroke-width="${n(6 + rand() * 6)}" fill="none" stroke-linecap="round" opacity=".85"/>`);
    if (b.cracks) for (let i = 0; i < 14; i++) { const x = rand() * W, y = HORIZON + 40 + rand() * 240; out.push(`<path d="M${n(x)} ${n(y)} l${n(20 + rand() * 30)} ${n(4 - rand() * 8)} l${n(16 + rand() * 20)} ${n(6 - rand() * 12)}" stroke="${b.detail}" stroke-width="1.5" fill="none"/>`); }
    if (b.glass) for (let i = 0; i < 10; i++) { const x = rand() * W, y = HORIZON + 40 + rand() * 230; out.push(poly([[x, y], [x + 8, y - 22], [x + 16, y]], "#7a5aa8", 'opacity=".75"')); }
    if (b.cobbles) for (let row = 0; row < 8; row++) for (let i = 0; i < 26; i++) out.push(`<ellipse cx="${n(i * 64 + (row % 2) * 32 + rand() * 8)}" cy="${n(HORIZON + 40 + row * 36)}" rx="24" ry="${n(6 + row * 1.2)}" fill="${b.detail}" opacity=".35"/>`);
    if (b.road || key === "plains" || key === "hills" || key === "rainhills" || key === "storm" || key === "steppe" || key === "coastplains" || key === "river") out.push(`<path d="M${W * 0.18} ${H} Q${W * 0.32} ${HORIZON + 120} ${W * 0.46} ${HORIZON + 20} L${W * 0.5} ${HORIZON + 20} Q${W * 0.4} ${HORIZON + 140} ${W * 0.3} ${H}Z" fill="#c8b48a" opacity=".55"/>`);
    if (b.rocks) out.push(rocks(rand, HORIZON + 30, 7, b.detail));
    if (b.battle) out.push(battleDebris(rand, HORIZON));
    if (b.reeds) { let d = ""; for (let i = 0; i < 60; i++) { const x = rand() * W, y = HORIZON + 30 + rand() * 260, h = 18 + rand() * 26; d += `M${n(x)} ${n(y)} q${n(-3 + rand() * 6)} ${n(-h * 0.5)} ${n(-4 + rand() * 8)} ${n(-h)} `; } out.push(`<path d="${d}" stroke="${K.darken(b.detail, 0.1)}" stroke-width="2" fill="none" stroke-linecap="round"/>`); }
    out.push(tufts(rand, b.detail, b.snow ? 18 : 70));
    // Foreground framing trees and bushes.
    if (b.trees && b.trees !== "none" && !b.town) {
      const colors = b.treeColor.map(color => K.darken(color, 0.12));
      const kind = b.trees === "forest" ? "pine" : b.trees === "sparse" ? "round" : b.trees;
      out.push(tree(kind, -20, H + 10, 2.4, colors, rand), tree(kind, W + 30, H + 20, 2.2, colors, rand));
    }
    if (b.vines) out.push(`<path d="M120 0 q-20 120 10 220 M260 0 q20 90 -6 160 M1400 0 q24 110 -8 200 M1520 0 q-14 80 10 150" stroke="#2f6a2f" stroke-width="6" fill="none"/><g fill="#3f8a3f">${[[120, 80], [130, 160], [262, 70], [1404, 90], [1398, 170], [1516, 60]].map(([x, y]) => `<ellipse cx="${x + 10}" cy="${y}" rx="14" ry="7"/>`).join("")}</g>`);
    // Weather and ambience.
    if (b.mist) out.push(`<g class="bsc-mist"><rect x="-200" y="${HORIZON - 10}" width="${W + 400}" height="70" fill="#ffffff" opacity=".22"/><rect x="-200" y="${HORIZON + 140}" width="${W + 400}" height="50" fill="#ffffff" opacity=".12"/></g>`);
    const particles = [];
    if (b.rain) for (let i = 0; i < 80; i++) { const x = rand() * (W + 200); particles.push(`<line class="bsc-rain" style="--d:${n(0.55 + rand() * 0.45)}s;--o:${n(-rand() * 2)}s" x1="${n(x)}" y1="-30" x2="${n(x - 9)}" y2="-4" stroke="#d8e4f0" stroke-opacity=".6" stroke-width="1.6"/>`); }
    if (b.snow) for (let i = 0; i < 60; i++) particles.push(`<circle class="bsc-snow" style="--d:${n(5 + rand() * 6)}s;--o:${n(-rand() * 10)}s;--x:${n(-40 + rand() * 80)}px" cx="${n(rand() * W)}" cy="-10" r="${n(1.5 + rand() * 2.5)}" fill="#ffffff" opacity=".9"/>`);
    if (b.embers) for (let i = 0; i < 40; i++) particles.push(`<circle class="bsc-ember" style="--d:${n(4 + rand() * 5)}s;--o:${n(-rand() * 8)}s;--x:${n(-60 + rand() * 120)}px" cx="${n(rand() * W)}" cy="${H + 10}" r="${n(1.5 + rand() * 2)}" fill="#ffb04a"/>`);
    if (b.sand) for (let i = 0; i < 30; i++) particles.push(`<circle class="bsc-sand" style="--d:${n(3 + rand() * 3)}s;--o:${n(-rand() * 6)}s" cx="-20" cy="${n(HORIZON + rand() * 260)}" r="${n(1 + rand() * 1.5)}" fill="#f6e2b8" opacity=".8"/>`);
    if ((b.fireflies || night) && !b.snow && !b.sand && !b.rain) for (let i = 0; i < (night ? 26 : 12); i++) particles.push(`<circle class="bsc-firefly" style="--d:${n(3 + rand() * 4)}s;--o:${n(-rand() * 6)}s" cx="${n(rand() * W)}" cy="${n(HORIZON - 40 + rand() * 260)}" r="2.2" fill="#f6ff9a"/>`);
    if (["forest", "deepforest", "jungle"].includes(key)) for (let i = 0; i < 14; i++) particles.push(`<path class="bsc-leaf" style="--d:${n(6 + rand() * 6)}s;--o:${n(-rand() * 12)}s;--x:${n(-120 + rand() * 240)}px" d="M${n(rand() * W)} -10 q4 -4 8 0 q-4 4 -8 0Z" fill="${b.treeColor[0]}"/>`);
    out.push(`<g class="bsc-particles">${particles.join("")}</g>`);
    // Lighting: darkness, dusk warmth and a soft vignette.
    const dim = (night ? 0.5 : dusk ? 0.16 : dawn ? 0.12 : 0) + (b.dim || 0);
    if (dim) out.push(`<rect width="${W}" height="${H}" fill="${night ? "#0a1238" : dusk ? "#5a2a3a" : "#4a3a6a"}" opacity="${n(Math.min(0.62, dim))}" class="bsc-tint"/>`);
    if (dusk) out.push(`<rect width="${W}" height="${H}" fill="#ff8a4a" opacity=".12"/>`);
    if (b.lightning) out.push(`<rect class="bsc-lightning" width="${W}" height="${H}" fill="#f2f4ff" opacity="0"/>`);
    out.push(`<rect width="${W}" height="${H}" fill="url(#bsc-vignette)"/>`);
    const markup = `<svg viewBox="0 0 ${W} ${H}" preserveAspectRatio="xMidYMax slice" class="bsc bsc-${key} bsc-${tod}">${out.join("")}</svg>`;
    if (cache.size > 40) cache.clear();
    cache.set(cacheKey, markup);
    return markup;
  }

  window.BattleScenery = { render, biome: biomeKey, timeKey: timeOfDay, BIOMES, ZONES };
})();
