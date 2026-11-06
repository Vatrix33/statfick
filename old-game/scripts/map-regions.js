/* What each region looks like, for map-terrain.js. A spec gives:
   - field(x, y, out, T, context): colour, height (metres), water, forest, pine, rock, wet,
     sand, snow, farm and glow at a point; shapes come from the region's own layout data,
     warped by noise so zones blend instead of ending at a line,
   - cover / treeKind / decorate: what stands on the land (trees, rocks, farms, ruins...),
   - labels: area names lettered on the map (map units; drawn at a fixed screen size).
   Layout stays as the game knows it: settlements, roads, rivers, shores and zone areas. */
(() => {
  "use strict";
  const hex = value => { const v = value.replace("#", ""); return [0, 2, 4].map(i => parseInt(v.slice(i, i + 2), 16)); };
  const P = obj => Object.fromEntries(Object.entries(obj).map(([key, value]) => [key, hex(value)]));
  const mix = (a, b, t) => t <= 0 ? a : t >= 1 ? b : [a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t, a[2] + (b[2] - a[2]) * t];
  const tint = (c, k) => [c[0] * k, c[1] * k, c[2] * k];
  const smooth = (a, b, v) => { const t = Math.max(0, Math.min(1, (v - a) / (b - a))); return t * t * (3 - 2 * t); };
  // Water for a shore: d is the signed distance past the waterline (positive in the water).
  // A crisp inked edge, a band of surf, then the deep.
  const shoreWater = (T, d, reach = 70) => d >= 0 ? 0.5 + 0.02 * Math.min(1, d / 1.5) + 0.48 * T.smooth(1.5, reach, d) : 0.5 * (1 - T.smooth(0, 3, -d));
  const randomIn = (random, e, scale = 1) => { const a = random() * Math.PI * 2, r = Math.sqrt(random()) * scale; return { x: e.x + Math.cos(a) * e.rx * r, y: e.y + Math.sin(a) * e.ry * r }; };
  // Dune height (0..1): crests across the wind, wandering, sharpened by ridged noise.
  function dunes(T, x, y, angle = 0.5, scale = 0.012) {
    const c = Math.cos(angle), s = Math.sin(angle);
    const wx = x + T.fbm(x, y, 2, 0.004, 4) * 60, wy = y + T.fbm(x + 99, y, 2, 0.004, 5) * 60;
    return T.ridged(wx * c + wy * s, (-wx * s + wy * c) * 0.3, 3, scale, 1);
  }

  /* ---------------- The colossal mountain ----------------
     The three Stoneveil regions are one mountain: the Foothills lie at its base, the High
     Stoneveil climbs its south flank and the Summit holds its peak. Each region sits in a
     shared frame, aligned at the roads that join them, so the slopes meet seamlessly. */
  const FRAME = { "stoneveil-summit": { dx: 0, dy: 0 }, "high-stoneveil": { dx: 0, dy: 1000 }, "stoneveil-foothills": { dx: 290, dy: 2000 } };
  const PEAK = { x: 800, y: 140 };
  function mountain(regionId, x, y, T) {
    const f = FRAME[regionId], X = x + f.dx, Y = y + f.dy;
    // Spurs meander a little instead of running dead straight from the peak.
    const wx = X + T.fbm(X, Y, 3, 0.004, 3) * 70, wy = Y + T.fbm(X + 500, Y, 3, 0.004, 4) * 40;
    const dx = wx - PEAK.x, dy = wy - PEAK.y, d = Math.hypot(dx * 0.92, dy);
    const cone = 8600 * Math.exp(-Math.pow(d / 1250, 1.28));
    // Ridges and couloirs radiating from the summit: ridged noise sampled on a circle whose
    // radius grows slowly with distance, so spurs run long down the flanks, widen and branch
    // toward the foot, and meet without a seam all the way around.
    const angle = Math.atan2(dy, dx), R = 85 * Math.pow(d + 1, 0.3);
    const ridges = T.ridged(Math.cos(angle) * R + 5000, Math.sin(angle) * R + 5000, 4, 0.012, 1);
    const relief = T.smooth(600, 3400, cone);
    let h = cone + (ridges - 0.45) * 1500 * relief + T.fbm(X, Y, 5, 0.006, 2) * 260 * (0.3 + relief);
    // The great south ridge the road follows, and the Dragonspine arête to the east.
    const south = Math.exp(-Math.pow((X - 800 - (Y - 1000) * 0.03) / 150, 2)) * Math.max(0, Math.min(1, (Y - 200) / 600)) * Math.max(0, 1 - (Y - 1700) / 600);
    h += south * 420 * relief;
    const spineAxis = (X - 1260) - (Y - 600) * 0.35, spine = Math.exp(-Math.pow(spineAxis / 70, 2)) * Math.max(0, Math.min(1, (1500 - Y) / 700));
    h += spine * 900;
    // Buttress walls that rise along the flank to the west and east (a ragged escarpment).
    const side = Math.abs(X - 810) + T.fbm(X, Y, 3, 0.008, 4) * 80;
    h += T.smooth(330, 430, side) * T.smooth(640, 860, Y) * (1 - T.smooth(1720, 1950, Y)) * (650 + T.fbm(X, Y, 3, 0.02, 5) * 250);
    // The Giant Steps: enormous terraces on the summit's west face.
    const steps = T.smooth(-50, 40, 455 - X - T.fbm(X, Y, 3, 0.01, 4) * 60) * (1 - T.smooth(760, 880, Y));
    if (steps > 0) { const step = 420, terraced = Math.floor(h / step) * step + Math.pow((h % step) / step, 6) * step; h += (terraced - h) * steps * 0.65; }
    return Math.max(0, h);
  }
  // slope: metres of rise per metre of ground.
  function mountainColors(h, slope, n, x, y, T, pal, out) {
    const snowLine = 3500 + n * 450, tree = 2350 + n * 300;
    const rocky = T.smooth(0.34, 0.68, slope + n * 0.08);
    let c = mix(pal.meadow, pal.alpine, T.smooth(1300, 2100, h));
    c = mix(c, pal.scree, T.smooth(2300, 3200, h + n * 200));
    const stone = mix(pal.rock, pal.dark, T.smooth(-0.1, 0.4, T.fbm(x, y, 3, 0.03, 5)));
    c = mix(c, stone, Math.max(rocky * 0.88, T.smooth(3000, 3800, h) * 0.35));
    // Snow holds on gentle ground; wind and steepness strip it from faces and ridges.
    const snow = T.smooth(snowLine - 300, snowLine + 250, h) * (1 - rocky * 0.82);
    const ice = (1 - T.smooth(0.04, 0.2, slope)) * T.smooth(snowLine + 300, snowLine + 1400, h);
    c = mix(c, mix(pal.snow, pal.ice, ice * 0.8), snow);
    out.snow = snow; out.rock = Math.max(rocky, T.smooth(2600, 3400, h) * 0.6);
    out.forest = T.smooth(1250, 1500, h) * (1 - T.smooth(tree - 200, tree + 150, h)) * (1 - rocky) * T.smooth(-0.15, 0.25, T.fbm(x, y, 3, 0.014, 3));
    out.pine = 1;
    return c;
  }
  // Hillshade strength shared by the three mountain regions, so their edges match: strong on
  // the gentle foothills, restrained on the great faces higher up.
  const mountainRelief = h => 0.8 + 3.7 * (1 - T0.smooth(900, 2300, h));
  const T0 = { smooth: (a, b, v) => { const t = Math.max(0, Math.min(1, (v - a) / (b - a))); return t * t * (3 - 2 * t); } };
  const mountainPal = P({ meadow: "#a3ab76", alpine: "#a0a184", scree: "#aaa596", rock: "#857f76", dark: "#6a655f", snow: "#f1f2ee", ice: "#d4e0e4" });

  const area = (T, x, y, e, feather = 30, warp = 28) => T.area(x, y, e, feather, warp);
  const roadSegments = (region, T) => (region.roads || []).map(path => T.segmentsOf(path));
  const nearRoads = (T, x, y, roads, limit) => roads.some(segments => T.pathDistance(x, y, segments, limit) < limit);
  const settlementsNear = (region, x, y) => (region.settlements || []).reduce((best, place) => Math.min(best, Math.hypot(x - place.x, y - place.y)), Infinity);
  const roadDistance = (T, x, y, roads, limit = 200) => roads.reduce((best, segments) => Math.min(best, T.pathDistance(x, y, segments, limit)), limit);
  // Farmland: around settlements and along roads, broken into patches by noise.
  function farmAmount(T, x, y, region, roads, { reach = 170, road = 55, patch = 0.35 } = {}) {
    const ds = settlementsNear(region, x, y), dr = roadDistance(T, x, y, roads, road + 40);
    const near = Math.max(1 - T.smooth(reach * 0.55, reach, ds), (1 - T.smooth(road * 0.45, road, dr)) * 0.85);
    return near * T.smooth(-patch, 0.1, T.fbm(x, y, 3, 0.009, 5) + (1 - T.smooth(30, 90, ds)) * 0.4);
  }

  // Farms, hedges and hamlets along the roads near settlements.
  function farmland(world, add, T, util, { radius = 120, count = 60, colors = ["#b6a866", "#9eae6c", "#c2b07a", "#8fa060"] } = {}) {
    const random = util.rng;
    if (world.spec.fields) {
      // Farmsteads among the painted fields: a house, a barn, sometimes an orchard.
      for (let k = 0; k < count * 6; k++) {
        const x = 30 + random() * 1540, y = 30 + random() * 940;
        if (world.sample("farm", x, y) < 0.55 || util.near(x, y, 4) || util.nearTown(x, y, 8) || random() > 0.22) continue;
        add({ type: "house", x, y, s: 0.9 + random() * 0.3, color: random() < 0.5 ? "#6e3b28" : "#4a3a30" });
        add({ type: "house", x: x + 2.6, y: y + 1.4, s: 0.7, color: "#5a4636" });
        if (random() < 0.35) for (let j = 0; j < 5; j++) add({ type: "tree", kind: "broad", x: x - 4 + (j % 3) * 1.6, y: y + 2 + Math.floor(j / 3) * 1.6, s: 0.55, v: random() });
      }
      return;
    }
    for (const plan of world.settlements) for (let k = 0; k < count; k++) {
      const angle = random() * Math.PI * 2, r = plan.radius + 14 + Math.sqrt(random()) * radius;
      const x = plan.place.x + Math.cos(angle) * r, y = plan.place.y + Math.sin(angle) * r * 0.8;
      if (x < 30 || y < 30 || x > 1570 || y > 970 || util.near(x, y, 5) || world.sample("water", x, y) > 0.3 || world.sample("forest", x, y) > 0.3) continue;
      add({ type: "farmfield", x, y, w: 7 + random() * 8, h: 4 + random() * 6, angle: random() * Math.PI, color: colors[k % colors.length], layer: -2 });
      if (random() < 0.3) add({ type: "hedge", x: x + 5, y: y + 3, len: 8 + random() * 8, angle: random() * Math.PI, layer: -1 });
      if (random() < 0.12) add({ type: "house", x: x + 3, y: y - 3, s: 0.9, color: random() < 0.5 ? "#6e3b28" : "#4a3a30" });
    }
  }
  function hamlets(world, add, util, count, avoid = () => false) {
    const random = util.rng;
    for (let k = 0; k < count; k++) {
      const road = world.roads[Math.floor(random() * world.roads.length)];
      if (!road) return;
      const p = road.path[Math.floor(random() * road.path.length)], side = random() < 0.5 ? -1 : 1;
      const x = p.x + side * (8 + random() * 10), y = p.y + (random() - 0.5) * 10;
      if (x < 40 || y < 40 || x > 1560 || y > 960 || util.nearTown(x, y, 30) || avoid(x, y)) continue;
      for (let j = 0; j < 3 + Math.floor(random() * 4); j++) add({ type: "house", x: x + (random() - 0.5) * 7, y: y + (random() - 0.5) * 5, s: 0.8 + random() * 0.4, color: random() < 0.5 ? "#6e3b28" : "#4a3a30" });
      add({ type: "farmfield", x: x + 7, y: y + 4, w: 8, h: 5, angle: random(), color: "#b6a866", layer: -2 });
    }
  }
  const grassCover = (colors = {}) => (grid, i, roll) => {
    if (grid.forest[i] > 0.05 || grid.rock[i] > 0.5 || grid.sand[i] > 0.5 || grid.snow[i] > 0.4) return null;
    if (roll > 0.985) return { type: "flowers", color: colors.flower || "#c9a24a", minLevel: 4 };
    if (roll > 0.955) return { type: "tuft", color: colors.tuft || "#6f7a4a", minLevel: 4 };
    return null;
  };

  const SPECS = {};

  /* ---------------- The Alder Vale ----------------
     Green rolling farmland around Alderkeep, the River Aster winding south through a shallow
     valley, the Great Alderwood in the northwest, Foxglove Copse in the east, scattered copses,
     hedged fields, hamlets and a few old stones. */
  {
    const pal = P({ grass: "#9cad6c", meadow: "#b9b87a", lush: "#86a062", floor: "#5f7848", deep: "#4a6440", bank: "#7d9c66", soil: "#a89a6c" });
    const alders = { size: 2.5, colors: ["#55744a", "#5f7c4c", "#4b6a44", "#688450"].map(hex), gap: hex("#1f2c1c") };
    const darkPines = { size: 2.2, pine: true, colors: ["#3f5a44", "#46624a", "#36503e"].map(hex), gap: hex("#141e16"), seed: 9 };
    SPECS["alder-vale"] = {
      relief: 14, canopyFrom: 0.74,
      paint(px) {
        const f = px.get("forest");
        if (f < 0.6) return;
        const pine = px.get("pine") + (MapTerrain.hash2(px.x / 6 | 0, px.y / 6 | 0, 4) - 0.5) * 0.4;
        MapTerrain.canopy(px, smooth(0.6, 0.8, f), pine > 0.5 ? darkPines : alders);
      },
      river: { width: 7.5 },
      fields: { block: 110, size: [13, 8], colors: ["#d4bf6c", "#a4ba66", "#bcc67c", "#b39866", "#cdbb62", "#98b262", "#c6c482"] },
      prepare: (region, T) => ({ river: T.segmentsOf(region.river), roads: roadSegments(region, T), forests: region.forests }),
      field(x, y, o, T, c, region) {
        const n = T.fbm(x, y, 4, 0.0045, 0), broad = T.fbm(x, y, 3, 0.0012, 2);
        const dr = T.pathDistance(x, y, c.river, 220);
        o.h = 80 + broad * 120 + n * 40 + Math.min(1, dr / 200) * 60 + T.smooth(1100, 1550, x) * 70;
        let col = mix(pal.grass, pal.meadow, T.smooth(-0.15, 0.45, n));
        col = mix(col, pal.lush, T.smooth(0.2, 0.6, broad) * 0.6);
        const great = area(T, x, y, c.forests[0], 34, 26), deep = area(T, x, y, { x: c.forests[0].x, y: c.forests[0].y, ...c.forests[0].deep }, 26, 20), fox = area(T, x, y, c.forests[1], 28, 22);
        const copse = T.smooth(0.34, 0.5, T.fbm(x, y, 3, 0.011, 3)) * T.smooth(60, 120, settlementsNear(region, x, y)) * 0.55;
        const forest = Math.max(great * 0.82, deep, fox * 0.78, copse);
        col = mix(col, pal.floor, forest * 0.75); col = mix(col, pal.deep, deep * 0.6);
        const wet = 1 - T.smooth(6, 34, dr);
        col = mix(col, pal.bank, wet * 0.5);
        o.color = col; o.forest = forest; o.pine = deep > 0.4 ? 0.7 : 0.15; o.wet = wet;
        o.farm = farmAmount(T, x, y, region, c.roads, { reach: 210, road: 60 }) * (1 - T.smooth(0.15, 0.4, forest)) * T.smooth(14, 30, dr);
      },
      treeKind: (grid, i, roll) => grid.pine[i] > 0.5 && roll < 0.4 ? "pine" : "broad",
      cover: grassCover(),
      decorate(world, add, T, util) {
        farmland(world, add, T, util, { count: 70 });
        hamlets(world, add, util, 9);
        add({ type: "windmill", x: 1158, y: 262, always: true });
        add({ type: "windmill", x: 330, y: 905, always: true });
        add({ type: "stones", x: 640, y: 610, always: true });
        add({ type: "ruin", x: 300, y: 160, s: 1.2, always: true });
        add({ type: "tower", x: 1480, y: 300, always: true });
        add({ type: "camp", x: 1290, y: 470, always: true });
      },
      labels: [
        { text: "GREAT ALDERWOOD", x: 370, y: 385 }, { text: "FOXGLOVE COPSE", x: 1335, y: 650 }, { text: "THE OPEN MEADOWS", x: 573, y: 715 }, { text: "EASTERN PLAINS", x: 1180, y: 100 },
        { text: "River Aster", x: 916, y: 500, angle: 67, kind: "river" },
      ],
    };
  }

  /* ---------------- Stoneveil Foothills ----------------
     Hilly plains where the colossal mountain begins: the north rises into pine belts and scree
     toward the mountain's foot; the River Aster comes down from it through the hills. */
  {
    const pal = P({ grass: "#a6ad74", dry: "#b8b07c", hill: "#9aa070", bank: "#80996a" });
    SPECS["stoneveil-foothills"] = {
      relief: mountainRelief,
      contours: { step: 150, major: 5 },
      river: { width: 7 },
      fields: { block: 96, size: [11, 7], colors: ["#cdb86a", "#a2b266", "#bcbe7c", "#ae9462", "#c0b26a"] },
      prepare: (region, T) => ({ river: T.segmentsOf(region.river), roads: roadSegments(region, T), hills: region.hillAreas }),
      field(x, y, o, T, c, region) {
        const n = T.fbm(x, y, 4, 0.005, 0);
        const hills = c.hills.reduce((sum, e) => sum + area(T, x, y, e, 120, 50), 0);
        const dr = T.pathDistance(x, y, c.river, 200);
        const h = mountain(region.id, x, y, T) + 120 + hills * 260 * (0.6 + T.fbm(x, y, 3, 0.012, 3) * 0.5) - (1 - Math.min(1, dr / 90)) * 60;
        o.h = h;
        let col = mix(pal.grass, pal.dry, T.smooth(-0.2, 0.4, n));
        col = mix(col, pal.hill, Math.min(1, hills) * 0.5);
        const wet = 1 - T.smooth(6, 30, dr);
        col = mix(col, pal.bank, wet * 0.5);
        o.color = col; o.wet = wet;
        o.forest = T.smooth(0.3, 0.46, T.fbm(x, y, 3, 0.011, 4)) * (0.55 + Math.min(1, hills) * 0.2); o.pine = 0.5;
        o.farm = farmAmount(T, x, y, region, c.roads, { reach: 160, road: 45, patch: 0.25 }) * (1 - Math.min(1, hills) * 0.6) * (1 - T.smooth(0.1, 0.3, o.forest)) * T.smooth(12, 26, dr) * (1 - T.smooth(1100, 1300, h));
      },
      // The mountain takes over where the land rises.
      colorize(o, slope, x, y, T) {
        if (o.h < 1100) return;
        const plain = { color: o.color, forest: o.forest }, n = T.fbm(x, y, 3, 0.01, 3);
        const rock = mountainColors(o.h, slope, n, x, y, T, mountainPal, o);
        const t = T.smooth(1100, 1500, o.h + n * 120);
        o.color = mix(plain.color, rock, t);
        o.forest = plain.forest * (1 - t) + o.forest * t; o.pine = t > 0.5 ? 1 : 0.5;
      },
      treeKind: (grid, i, roll) => grid.pine[i] > 0.5 ? "pine" : roll < 0.5 ? "broad" : "pine",
      cover(grid, i, roll) {
        if (grid.rock[i] > 0.4 && roll > 0.975) return { type: "rock", s: 0.8 + roll * 0.5, color: "#8a857a" };
        return grassCover({ tuft: "#7a7a4a" })(grid, i, roll);
      },
      decorate(world, add, T, util) {
        farmland(world, add, T, util, { count: 55 });
        hamlets(world, add, util, 7, (x, y) => y < 330);
        const random = util.rng;
        for (let k = 0; k < 26; k++) add({ type: "stones", x: 200 + random() * 1200, y: 360 + random() * 560, always: k < 4 });
        add({ type: "tower", x: 1225, y: 690, always: true }); add({ type: "ruin", x: 300, y: 520, always: true }); add({ type: "camp", x: 760, y: 330, always: true });
      },
      labels: [
        { text: "SOUTHERN HILLS", x: 240, y: 875 }, { text: "STONEVEIL PLAINS", x: 570, y: 610 }, { text: "CROWNSPIRE DOWNS", x: 1300, y: 690 }, { text: "THE MOUNTAIN'S FOOT", x: 890, y: 130 },
        { text: "River Aster", x: 815, y: 650, angle: 79, kind: "river" },
      ],
    };
  }

  /* ---------------- The High Stoneveil: the mountain's south flank ---------------- */
  {
    SPECS["high-stoneveil"] = {
      relief: mountainRelief, micro: 0.8,
      contours: { step: 200, major: 5 },
      prepare: (region, T) => ({ roads: roadSegments(region, T) }),
      field(x, y, o, T, c, region) { o.h = mountain(region.id, x, y, T); },
      colorize(o, slope, x, y, T) { o.color = mountainColors(o.h, slope, T.fbm(x, y, 3, 0.01, 3), x, y, T, mountainPal, o); },
      cover(grid, i, roll) {
        if (grid.snow[i] > 0.6) return roll > 0.97 ? { type: "snowdrift", minLevel: 2 } : null;
        if (grid.rock[i] > 0.45) return roll > 0.975 ? { type: "rock", s: 0.8 + (roll - 0.975) * 24, color: "#86827a" } : null;
        return grassCover({ tuft: "#7a7a5a", flower: "#b8b0d8" })(grid, i, roll);
      },
      decorate(world, add, T, util) {
        const random = util.rng;
        // Cliff edges where the slope breaks.
        for (let k = 0; k < 900; k++) {
          const x = 30 + random() * 1540, y = 30 + random() * 940, s = world.sample("shade", x, y);
          if (world.sample("rock", x, y) > 0.7 && s < 0.38) add({ type: "cliff", x, y, s: 0.8 + random() * 0.6, angle: (random() - 0.5) * 0.6 });
        }
        for (let k = 0; k < 140; k++) { const x = 60 + random() * 1480, y = 60 + random() * 600; if (world.sample("snow", x, y) > 0.8) add({ type: "crevasse", x, y }); }
        add({ type: "camp", x: 720, y: 820, always: true }); add({ type: "ruin", x: 900, y: 640, always: true });
      },
      over: clouds([{ x: 300, y: 760, r: 160 }, { x: 1230, y: 880, r: 190 }, { x: 560, y: 940, r: 120 }, { x: 1420, y: 640, r: 130 }]),
      labels: [
        { text: "LOWER SLOPES", x: 230, y: 895 }, { text: "WESTERN CLIFFS", x: 250, y: 560 }, { text: "EASTERN CLIFFS", x: 1350, y: 590 }, { text: "HIGH PASS", x: 960, y: 420 },
        { text: "FROZEN HEIGHTS", x: 640, y: 205 }, { text: "DRAGONSPINE", x: 1380, y: 200 },
      ],
    };
  }

  /* ---------------- Stoneveil Summit: the peak itself ---------------- */
  {
    SPECS["stoneveil-summit"] = {
      relief: mountainRelief, micro: 0.9,
      contours: { step: 200, major: 5 },
      prepare: (region, T) => ({ roads: roadSegments(region, T) }),
      field(x, y, o, T, c, region) { o.h = mountain(region.id, x, y, T); },
      colorize(o, slope, x, y, T) { o.color = mountainColors(o.h, slope, T.fbm(x, y, 3, 0.01, 3), x, y, T, mountainPal, o); },
      cover(grid, i, roll) {
        if (grid.snow[i] > 0.6) return roll > 0.975 ? { type: "snowdrift", minLevel: 2 } : null;
        if (grid.rock[i] > 0.45) return roll > 0.98 ? { type: "rock", s: 0.8 + (roll - 0.98) * 30, color: "#86827a" } : null;
        return null;
      },
      decorate(world, add, T, util) {
        const random = util.rng;
        for (let k = 0; k < 1100; k++) {
          const x = 30 + random() * 1540, y = 30 + random() * 940, s = world.sample("shade", x, y);
          if (world.sample("rock", x, y) > 0.7 && s < 0.38) add({ type: "cliff", x, y, s: 0.8 + random() * 0.7, angle: (random() - 0.5) * 0.7 });
        }
        for (let k = 0; k < 260; k++) { const x = 60 + random() * 1480, y = 40 + random() * 700; if (world.sample("snow", x, y) > 0.8) add({ type: "crevasse", x, y }); }
        for (let k = 0; k < 14; k++) add({ type: "spire", x: 1200 + random() * 330, y: 300 + random() * 450, s: 0.8 + random() * 0.6, color: "#6a6660" });
        for (let k = 0; k < 9; k++) add({ type: "bones", x: 1220 + random() * 300, y: 320 + random() * 400 });
      },
      over: clouds([{ x: 260, y: 900, r: 210 }, { x: 1320, y: 940, r: 230 }, { x: 760, y: 990, r: 170 }, { x: 1500, y: 760, r: 150 }, { x: 120, y: 720, r: 140 }]),
      labels: [
        { text: "WINDCUT ASCENT", x: 220, y: 895 }, { text: "GIANT STEPS", x: 240, y: 555 }, { text: "CLOUDBREAK RIDGE", x: 795, y: 610 }, { text: "DRAGON EYRIES", x: 1340, y: 555 },
        { text: "THE FROZEN CROWN", x: 800, y: 70 },
      ],
    };
  }

  /* ---------------- The Verdant Reach ----------------
     An immense forest in three layers: open woodland and glades at its rim, closed broadleaf
     forest within, and the Deepwood of giant elders at its heart. Narrow plains and farms lie
     east and south, where Greenwatch guards the road. */
  {
    const pal = P({ plain: "#a9b56f", meadow: "#bdbb7a", rim: "#7c955a", forest: "#5a764a", deep: "#3c573c", glade: "#98a868" });
    const crowns = { size: 2.6, colors: ["#4f7144", "#5a7b48", "#466a40", "#62804c", "#56763f"].map(hex), gap: hex("#1f2c1c") };
    const elders = { size: 4.4, colors: ["#3c5e3c", "#456840", "#355636", "#4a6a3c"].map(hex), gap: hex("#141e14"), seed: 23 };
    SPECS["verdant-reach"] = {
      relief: 9, micro: 0.4, canopyFrom: 0.7,
      fields: { block: 100, size: [12, 8] },
      prepare: (region, T) => ({ roads: roadSegments(region, T), L: region.forestLayers }),
      field(x, y, o, T, c, region) {
        const n = T.fbm(x, y, 4, 0.005, 0);
        const outer = area(T, x, y, c.L.outer, 70, 70), forest = area(T, x, y, c.L.forest, 50, 50), deep = area(T, x, y, c.L.deep, 40, 36);
        const glade = T.smooth(0.3, 0.42, T.fbm(x, y, 3, 0.011, 3)) * (1 - deep) * (1 - forest * 0.6);
        const open = T.smooth(4, 12, roadDistance(T, x, y, c.roads, 30)) * T.smooth(20, 45, settlementsNear(region, x, y));
        o.h = 90 + T.fbm(x, y, 3, 0.0018, 2) * 90 + n * 30 + forest * 30;
        let col = mix(pal.plain, pal.meadow, T.smooth(-0.1, 0.4, n));
        col = mix(col, pal.rim, outer * 0.75); col = mix(col, pal.forest, forest * 0.85); col = mix(col, pal.deep, deep);
        col = mix(col, pal.glade, glade * outer * 0.7);
        o.color = col;
        o.forest = Math.max(outer * 0.48, forest * 0.9, deep) * (1 - glade * 0.9) * open;
        o.pine = deep;
        o.farm = farmAmount(T, x, y, region, c.roads, { reach: 150, road: 45 }) * (1 - T.smooth(0.2, 0.6, outer));
      },
      treeKind: (grid, i, roll) => grid.pine[i] > 0.5 ? "elder" : roll < 0.08 ? "pine" : "broad",
      paint(px) {
        const f = px.get("forest");
        if (f < 0.5) return;
        const deep = px.get("pine") + (MapTerrain.hash2(px.x / 7 | 0, px.y / 7 | 0, 5) - 0.5) * 0.3;
        MapTerrain.canopy(px, smooth(0.5, 0.78, f), deep > 0.5 ? elders : crowns);
      },
      cover: grassCover({ flower: "#d8c060" }),
      decorate(world, add, T, util) {
        const random = util.rng, L = world.region.forestLayers;
        farmland(world, add, T, util, { count: 40 });
        hamlets(world, add, util, 6, (x, y) => world.sample("forest", x, y) > 0.3);
        add({ type: "stones", x: 455, y: 300, always: true }); add({ type: "ruin", x: 560, y: 245, s: 1.3, always: true });
        add({ type: "tower", x: 1180, y: 760, always: true }); add({ type: "camp", x: 1010, y: 640, always: true });
        add({ type: "windmill", x: 1460, y: 620, always: true }); add({ type: "windmill", x: 1130, y: 900, always: true });
        for (let k = 0; k < 40; k++) { const p = randomIn(random, L.forest, 0.9); if (!util.near(p.x, p.y, 6) && !util.nearTown(p.x, p.y, 10)) add({ type: "pool", x: p.x, y: p.y, s: 0.8 + random() * 1.4, color: "#3d5a50" }); }
        for (let k = 0; k < 12; k++) { const p = randomIn(random, L.deep, 0.8); add({ type: "stones", x: p.x, y: p.y }); }
      },
      labels: [
        { text: "THE VERDANT REACH", x: 450, y: 690 }, { text: "THE DEEPWOOD", x: 435, y: 375 }, { text: "EASTERN PLAINS", x: 1360, y: 300 }, { text: "SOUTHERN MEADOWS", x: 980, y: 925 },
      ],
    };
  }

  /* ---------------- The Sunward Coast ----------------
     Farmed coastal plains in the west, wind-combed grassland, a belt of marram dunes, a long
     pale beach and the Azure Sea; a salt marsh of tidal creeks in the south; Seabridge's piers. */
  {
    const pal = P({ plain: "#a5b46c", lush: "#93a862", wind: "#bdbb82", dune: "#dccb96", slack: "#a8ae78", beach: "#e6d8aa", marsh: "#8e9a68", mud: "#8a8460" });
    SPECS["sunward-coast"] = {
      relief: 10, micro: 0.35,
      water: { shallow: "#7fa3a6", deep: "#365d6c", foam: "#e6e0c8" },
      fields: { block: 96, size: [12, 7] },
      prepare: (region, T) => ({ roads: roadSegments(region, T), marsh: region.saltMarsh, shore: region.landShape.shoreX }),
      field(x, y, o, T, c, region) {
        const n = T.fbm(x, y, 4, 0.006, 0);
        const sea = x - (c.shore + T.fbm(x, y, 4, 0.006, 3) * 34 - 6);
        const marsh = area(T, x, y, c.marsh, 40, 40);
        const belt = T.smooth(900, 1000, x + n * 40), beach = T.smooth(1130, 1185, x + n * 25);
        const d = dunes(T, x, y, 0.15, 0.016);
        o.h = 70 - T.smooth(600, 1300, x) * 55 + n * 25 + d * 32 * belt * (1 - beach * 0.7) - marsh * 15;
        let col = mix(pal.lush, pal.plain, T.smooth(200, 600, x + n * 80));
        col = mix(col, pal.wind, T.smooth(560, 760, x + n * 60) * 0.8);
        col = mix(col, mix(pal.slack, pal.dune, T.smooth(0.25, 0.6, d + n * 0.3)), belt);
        col = mix(col, pal.beach, beach);
        // Tidal creeks wind through the salt marsh.
        const creek = 1 - T.smooth(0.006, 0.03, Math.abs(T.fbm(x, y, 3, 0.011, 4)));
        col = mix(col, pal.marsh, marsh * 0.85); col = mix(col, pal.mud, marsh * creek * 0.6);
        o.color = col;
        o.sand = Math.max(beach, belt * T.smooth(0.25, 0.6, d)); o.wet = marsh;
        o.forest = T.smooth(0.36, 0.5, T.fbm(x, y, 3, 0.012, 2)) * (1 - T.smooth(500, 800, x)) * 0.6;
        o.farm = farmAmount(T, x, y, region, c.roads, { reach: 160, road: 45 }) * (1 - T.smooth(700, 950, x)) * (1 - marsh);
        o.water = Math.max(shoreWater(T, sea, 70), marsh > 0.4 ? creek * marsh * 0.62 : 0);
      },
      cover(grid, i, roll) {
        if (grid.wet[i] > 0.5 && grid.water[i] < 0.5) return roll > 0.82 ? { type: "reeds", color: "#8a8a58" } : null;
        if (grid.sand[i] > 0.3 && grid.sand[i] < 0.95) return roll > 0.88 ? { type: "tuft", color: "#8e9058", minLevel: 2 } : null;
        return grassCover({ flower: "#d8c870" })(grid, i, roll);
      },
      decorate(world, add, T, util) {
        const random = util.rng;
        farmland(world, add, T, util, { count: 45 });
        hamlets(world, add, util, 6, (x, y) => x > 900);
        add({ type: "pier", x: 1286, y: 684, len: 34, angle: 0, always: true }); add({ type: "pier", x: 1288, y: 700, len: 26, angle: 0.15, always: true });
        for (let k = 0; k < 4; k++) add({ type: "boat", x: 1326 + k * 8, y: 678 + (k % 2) * 22, always: true });
        for (let k = 0; k < 16; k++) add({ type: "boat", x: 1345 + random() * 225, y: 60 + random() * 880 });
        add({ type: "lighthouse", x: 1294, y: 372, always: true });
        add({ type: "wreck", x: 1262, y: 905, angle: -0.5, always: true }); add({ type: "wreck", x: 1282, y: 140, angle: 0.3, s: 0.8 });
        add({ type: "windmill", x: 330, y: 320, always: true }); add({ type: "windmill", x: 700, y: 640, always: true });
        add({ type: "ruin", x: 1050, y: 90, always: true }); add({ type: "camp", x: 1010, y: 470, always: true });
      },
      labels: [
        { text: "COASTAL PLAINS", x: 270, y: 265 }, { text: "WINDWARD GRASSLAND", x: 730, y: 410 }, { text: "SALT MARSH", x: 920, y: 915 }, { text: "DUNE BELT", x: 1080, y: 195 },
        { text: "The Azure Sea", x: 1450, y: 470, angle: -90, kind: "river" },
      ],
    };
  }

  /* ---------------- Cinder Isle ----------------
     A volcanic island: a black sand shore, ember scrub, grey ash slopes cut by gullies, black
     lava fields where molten runs glow, and a caldera holding a crusted lake of fire. */
  {
    const pal = P({ sand: "#3c3836", sandGrey: "#4e4a46", scrub: "#5f5c40", scrubDry: "#706648", ash: "#6e6964", ashDark: "#4c4744", basalt: "#2f2b2b", crust: "#221d1d" });
    SPECS["cinder-isle"] = {
      relief: h => 1.3 + 4 * (1 - smooth(300, 1800, h)), micro: 0.7, contours: { step: 250, major: 4 },
      water: { shallow: "#5a767c", deep: "#1f2e38", foam: "#c4c0b0" },
      road: { fill: "#8a7c66", casing: "#2a2420" },
      prepare: (region, T) => ({ roads: roadSegments(region, T), I: region.landShape, V: region.volcano }),
      field(x, y, o, T, c) {
        const I = c.I, V = c.V;
        const e = Math.hypot((x - I.x) / I.rx, (y - I.y) / I.ry) + T.fbm(x, y, 3, 0.006, 3) * 0.02;
        const vx = (x - V.x) / V.rx, vy = (y - V.y) / V.ry, dv = Math.hypot(vx, vy), angle = Math.atan2(vy, vx);
        const R = 60 * Math.pow(dv * 300 + 1, 0.32);
        const gullies = T.ridged(Math.cos(angle) * R + 900, Math.sin(angle) * R + 900, 4, 0.02, 1);
        const cone = 2900 * Math.exp(-Math.pow(dv / 0.62, 1.5)), caldera = 1 - T.smooth(0.2, 0.31, dv);
        const h = 30 + T.fbm(x, y, 4, 0.006, 2) * 60 + cone * (0.92 + (gullies - 0.45) * 0.3 * T.smooth(0.25, 0.7, dv)) - caldera * 1100;
        o.h = Math.max(0, h * (0.2 + 0.8 * T.smooth(0, 0.12, 1 - e)));
        const n = T.fbm(x, y, 4, 0.01, 0);
        const shore = T.smooth(0.78, 0.84, e + T.fbm(x, y, 3, 0.02, 4) * 0.03);
        const slope = 1 - T.smooth(0.94, 1.04, dv), fields = 1 - T.smooth(0.62, 0.72, dv + T.fbm(x, y, 3, 0.02, 5) * 0.05);
        let col = mix(pal.scrub, pal.scrubDry, T.smooth(-0.15, 0.35, n));
        col = mix(col, mix(pal.sand, pal.sandGrey, T.smooth(-0.1, 0.4, T.fbm(x, y, 3, 0.02, 2))), shore);
        col = mix(col, mix(pal.ash, pal.ashDark, T.smooth(0.4, 0.7, gullies)), slope * (1 - shore));
        col = mix(col, pal.basalt, fields);
        // Lava runs down the gullies; the caldera holds a lake of it.
        const R2 = 110 * Math.pow(dv * 300 + 1, 0.22);
        const runs = T.smooth(0.88, 0.97, T.ridged(Math.cos(angle) * R2 + 300, Math.sin(angle) * R2 + 300, 3, 0.02, 4)) * fields * T.smooth(0.2, 0.32, dv);
        const lake = 1 - T.smooth(0.16, 0.22, dv);
        o.glow = Math.max(runs * 0.85, lake);
        o.color = mix(col, pal.crust, caldera * (1 - lake));
        o.rock = Math.max(fields, slope * 0.5); o.sand = shore;
        o.forest = (1 - slope) * (1 - shore) * T.smooth(0.25, 0.45, T.fbm(x, y, 3, 0.013, 2) + 0.2) * 0.5;
        o.water = shoreWater(T, (e - 1) * Math.min(I.rx, I.ry), 80);
      },
      treeKind: (grid, i, roll) => roll < 0.75 ? "ember" : "dead",
      paint(px) {
        const glow = px.get("glow");
        if (glow > 0.5) {
          // Black crust floats on the lava lake; it glows only at the seams.
          const c = MapTerrain.cells(px.x, px.y, 5, 31), seam = c.d2 - c.d1;
          px.glowK = seam < 0.45 ? 1 : 0.25 + 0.2 * c.id;
          if (seam >= 0.45) px.scale(0.6);
        }
        if (px.get("rock") > 0.5 && glow < 0.3 && px.level >= 2) { const c = MapTerrain.cells(px.x, px.y, 3.2, 37); if (c.d2 - c.d1 < 0.16) px.scale(0.72); }
        const sand = px.get("sand");
        if (sand > 0.3) { const k = MapTerrain.hash2(px.x * 2.5 | 0, px.y * 2.5 | 0, 13); if (k > 0.88) px.scale(1 + 0.35 * sand); else if (k < 0.1) px.scale(1 - 0.25 * sand); }
      },
      cover(grid, i, roll) {
        if (grid.sand[i] > 0.6) return roll > 0.985 ? { type: "rock", s: 0.6, color: "#3a3434" } : null;
        if (grid.rock[i] > 0.6) return roll > 0.97 ? { type: "rock", s: 0.7 + roll * 0.4, color: "#2e2a2a" } : null;
        if (grid.rock[i] < 0.2) return roll > 0.95 ? { type: "tuft", color: "#7a6a44", minLevel: 2 } : null;
        return null;
      },
      decorate(world, add, T, util) {
        const random = util.rng, V = world.region.volcano, I = world.region.landShape;
        add({ type: "pier", x: 128, y: 508, len: 32, angle: Math.PI, always: true });
        add({ type: "boat", x: 90, y: 522, always: true }); add({ type: "boat", x: 80, y: 494, always: true });
        for (let k = 0; k < 34; k++) { const p = randomIn(random, V, 0.95); if (Math.hypot((p.x - V.x) / V.rx, (p.y - V.y) / V.ry) > 0.3) add({ type: "smoke", x: p.x, y: p.y, s: 0.6 + random() * 0.6 }); }
        for (let k = 0; k < 180; k++) { const p = randomIn(random, V, 0.7); add({ type: "shard", x: p.x, y: p.y, s: 0.6 + random() * 0.8, color: "#1e1a20", shine: "#8a7aa0" }); }
        for (let k = 0; k < 70; k++) { const p = randomIn(random, V, 1); add({ type: "crater", x: p.x, y: p.y, s: 0.6 + random() * 0.9 }); }
        for (let k = 0; k < 7; k++) { const a = random() * Math.PI * 2; add({ type: "wreck", x: I.x + Math.cos(a) * I.rx * 0.9, y: I.y + Math.sin(a) * I.ry * 0.9, angle: random() - 0.5, s: 0.8 }); }
        add({ type: "ruin", x: 420, y: 300, always: true }); add({ type: "tower", x: 640, y: 820, always: true }); add({ type: "camp", x: 760, y: 610, always: true });
      },
      over: clouds([{ x: 1050, y: 420, r: 60 }, { x: 1120, y: 385, r: 85 }, { x: 1215, y: 345, r: 110 }, { x: 1330, y: 300, r: 130 }, { x: 1460, y: 250, r: 140 }], "58,52,50", 0.5),
      labels: [
        { text: "BLACK SAND SHORE", x: 380, y: 835 }, { text: "EMBER SCRUB", x: 515, y: 390 }, { text: "ASHEN SLOPE", x: 850, y: 270 }, { text: "LAVA FIELDS", x: 1220, y: 620 }, { text: "CALDERA", x: 1030, y: 520 },
      ],
    };
  }

  /* ---------------- Stormwake Hills ----------------
     Rain-soaked downs and rolling hills patched with bracken and dry-stone walls, the rocky
     Stormcrest where the Church of Talos stands, and a floodplain of standing water. */
  {
    const pal = P({ down: "#7d9862", downs2: "#88a068", hill: "#71885a", bracken: "#8a6c46", crest: "#6c6c62", flood: "#6e8c64", mud: "#6e6a52" });
    SPECS["stormwake-hills"] = {
      relief: 6, micro: 0.5, contours: { step: 50, major: 5 },
      water: { shallow: "#6a8486", deep: "#3a5258", foam: "#c8ccbc" },
      fields: { block: 80, size: [10, 6], colors: ["#7f9a58", "#90a464", "#6f8e54", "#a0a266", "#86965a"], hedge: "#55604a" },
      prepare: (region, T) => ({ roads: roadSegments(region, T), hills: region.hillAreas, crest: region.stormcrest, flood: { x: 1160, y: 680, rx: 230, ry: 145 } }),
      field(x, y, o, T, c, region) {
        const n = T.fbm(x, y, 4, 0.006, 0);
        const hills = Math.min(1.3, c.hills.reduce((sum, e) => sum + area(T, x, y, e, 90, 50), 0));
        const crest = area(T, x, y, c.crest, 70, 40), flood = area(T, x, y, c.flood, 50, 40);
        const ridges = T.ridged(x, y, 4, 0.007, 1);
        o.h = 140 + n * 70 + hills * (160 + ridges * 170) + crest * (220 + ridges * 260) - flood * 50;
        let col = mix(pal.down, pal.downs2, T.smooth(-0.2, 0.3, n));
        col = mix(col, pal.hill, Math.min(1, hills) * 0.6);
        const bracken = T.smooth(0.12, 0.3, T.fbm(x, y, 3, 0.014, 3)) * Math.min(1, hills + crest);
        col = mix(col, pal.bracken, bracken * 0.65);
        col = mix(col, pal.crest, crest * T.smooth(0.35, 0.7, ridges + 0.2));
        const pools = T.fbm(x, y, 4, 0.016, 4) + flood * 0.32 - 0.24 - (1 - flood) * 0.4;
        col = mix(col, pal.flood, flood * 0.7); col = mix(col, pal.mud, flood * T.smooth(-0.1, 0, pools) * 0.6);
        o.color = col; o.rock = crest * T.smooth(0.45, 0.8, ridges); o.wet = flood;
        o.water = pools > 0 ? 0.5 + Math.min(0.3, pools * 1.5) : 0.5 * (1 - T.smooth(0, 0.03, -pools));
        o.forest = T.smooth(0.3, 0.46, T.fbm(x, y, 3, 0.012, 2)) * 0.7 * (1 - crest) * (1 - flood * 0.7);
        o.farm = farmAmount(T, x, y, region, c.roads, { reach: 150, road: 40 }) * (1 - crest) * (1 - Math.min(1, hills) * 0.4) * (1 - flood * 0.6);
      },
      treeKind: (grid, i, roll) => grid.rock[i] > 0.3 ? "dead" : roll < 0.3 ? "pine" : "broad",
      cover(grid, i, roll) {
        if (grid.rock[i] > 0.4) return roll > 0.95 ? { type: "rock", s: 0.7 + roll * 0.5, color: "#76766c" } : null;
        if (grid.wet[i] > 0.4 && grid.water[i] < 0.5) return roll > 0.9 ? { type: "reeds", color: "#7a8456" } : null;
        return grassCover({ tuft: "#647a4a", flower: "#b8a0c8" })(grid, i, roll);
      },
      decorate(world, add, T, util) {
        const random = util.rng;
        farmland(world, add, T, util, { count: 40 });
        hamlets(world, add, util, 6);
        // Dry-stone walls across the downs.
        for (let k = 0; k < 220; k++) {
          const x = 40 + random() * 1520, y = 40 + random() * 920;
          if (world.sample("rock", x, y) > 0.3 || world.sample("water", x, y) > 0.3 || util.near(x, y, 6) || util.nearTown(x, y, 10)) continue;
          add({ type: "hedge", x, y, len: 10 + random() * 14, angle: random() * Math.PI, color: "#8a887a", layer: -1 });
        }
        for (let k = 0; k < 14; k++) add({ type: "stones", x: 200 + random() * 1300, y: 100 + random() * 800 });
        for (let k = 0; k < 30; k++) { const p = randomIn(random, world.region.stormcrest, 0.9); add({ type: "tree", kind: "dead", x: p.x, y: p.y, s: 0.9 + random() * 0.4, v: random() }); }
        add({ type: "tower", x: 1370, y: 230, always: true }); add({ type: "ruin", x: 520, y: 760, always: true }); add({ type: "camp", x: 980, y: 480, always: true });
        add({ type: "windmill", x: 1000, y: 700, always: true });
      },
      labels: [
        { text: "STORMCREST", x: 380, y: 170 }, { text: "THUNDER HILLS", x: 420, y: 610 }, { text: "FLOODPLAIN", x: 1190, y: 845 }, { text: "RAINWASHED DOWNS", x: 910, y: 145 },
      ],
    };
  }

  /* ---------------- Gloamfen Marsh ----------------
     Peat and sedge riddled with black pools, the Deep Mire where the water wins, reed-choked
     Reedwater, the heath-and-stone Stoneback Rise, and two plank causeways above it all. */
  {
    const pal = P({ peat: "#5e6444", moss: "#6b7148", sedge: "#7c7c50", mire: "#47483a", rise: "#7a7a62", heath: "#6e5c52", reed: "#8c8858" });
    SPECS["gloamfen-marsh"] = {
      relief: 5, micro: 0.45,
      water: { shallow: "#55624f", deep: "#262e28", foam: "#9c9c80" },
      road: { fill: "#8c7656", casing: "#3a2c1e", width: 2.4 },
      prepare: (region, T) => ({ roads: roadSegments(region, T), rise: region.rockyRise, mire: region.deepMire, reeds: region.reedwater }),
      field(x, y, o, T, c, region) {
        const n = T.fbm(x, y, 4, 0.008, 0);
        const rise = area(T, x, y, c.rise, 60, 40), mire = area(T, x, y, c.mire, 70, 60), reeds = area(T, x, y, c.reeds, 60, 50);
        const dry = Math.max(rise, 1 - T.smooth(3, 9, roadDistance(T, x, y, c.roads, 20)), 1 - T.smooth(25, 45, settlementsNear(region, x, y)));
        const pools = T.fbm(x, y, 4, 0.02, 3) * 0.9 + T.fbm(x, y, 2, 0.004, 5) * 0.25 + mire * 0.42 + reeds * 0.26 - 0.24 - dry * 0.6;
        const ridges = T.ridged(x, y, 4, 0.01, 1);
        o.h = 15 + n * 6 + rise * (80 + ridges * 90);
        let col = mix(pal.peat, pal.moss, T.smooth(-0.2, 0.3, n));
        col = mix(col, pal.sedge, T.smooth(0.1, 0.4, T.fbm(x, y, 3, 0.03, 2)) * 0.5);
        col = mix(col, pal.mire, mire * 0.7); col = mix(col, pal.reed, reeds * 0.5);
        col = mix(col, mix(pal.heath, pal.rise, T.smooth(0.3, 0.6, ridges)), rise);
        o.color = col; o.wet = (1 - rise) * Math.max(reeds, 0.3); o.rock = rise * T.smooth(0.5, 0.8, ridges);
        o.water = pools > 0 ? 0.5 + Math.min(0.4, pools * 1.6) : 0.5 * (1 - T.smooth(0, 0.025, -pools));
        o.forest = T.smooth(0.25, 0.42, T.fbm(x, y, 3, 0.014, 4)) * 0.38 * (1 - reeds);
        o.pine = mire;
      },
      treeKind: (grid, i, roll) => grid.pine[i] > 0.4 || roll < 0.15 ? "dead" : "scrub",
      cover(grid, i, roll) {
        if (grid.rock[i] > 0.4) return roll > 0.95 ? { type: "rock", s: 0.7 + roll * 0.4, color: "#7a786a" } : null;
        if (grid.water[i] > 0.28 && grid.water[i] < 0.5 && roll > 0.5) return { type: "reeds", color: "#8a8450" };
        if (grid.wet[i] > 0.6 && roll > 0.75) return { type: "reeds", color: "#948e58" };
        if (roll > 0.94) return { type: "tuft", color: "#8a8a58", minLevel: 2 };
        return null;
      },
      decorate(world, add, T, util) {
        const random = util.rng, R = world.region;
        for (let k = 0; k < 90; k++) { const p = randomIn(random, R.deepMire, 0.95); if (world.sample("water", p.x, p.y) < 0.5) add({ type: "wisp", x: p.x, y: p.y - 2, minLevel: 2 }); }
        for (let k = 0; k < 9; k++) add({ type: "house", x: 742 + random() * 50, y: 790 + random() * 22, s: 0.9, color: "#4a3a30" });
        for (let k = 0; k < 6; k++) { const p = randomIn(random, R.deepMire, 0.8); add({ type: "ruin", x: p.x, y: p.y, s: 0.8, color: "#6a6a5a" }); }
        for (let k = 0; k < 10; k++) { const p = randomIn(random, R.rockyRise, 0.8); add({ type: "stones", x: p.x, y: p.y }); }
        add({ type: "tower", x: 300, y: 520, always: true }); add({ type: "camp", x: 1180, y: 600, always: true });
      },
      over: clouds([{ x: 820, y: 600, r: 160 }, { x: 1250, y: 820, r: 140 }, { x: 420, y: 880, r: 120 }, { x: 1360, y: 300, r: 110 }], "176,184,160", 0.2),
      labels: [
        { text: "STONEBACK RISE", x: 300, y: 530 }, { text: "THE DEEP MIRE", x: 820, y: 680 }, { text: "REEDWATER", x: 1300, y: 235 }, { text: "EASTERN FEN", x: 1315, y: 840 },
      ],
    };
  }

  /* ---------------- Verdigris Jungle ----------------
     An unbroken canopy seen from above: crowded jungle crowns, the giants of the Old Growth,
     the sunlit High Canopy on a rise, flooded pools in the shadows and a single road. */
  {
    const pal = P({ verge: "#7c9a5a", floor: "#3c5c38", dense: "#2e4c30", old: "#24402a", high: "#3a5c38" });
    const jungle = { size: 3.4, colors: ["#3f6a3c", "#4a7a42", "#356034", "#568446", "#2f5a32"].map(hex), gap: hex("#122016"), seed: 13 };
    const giants = { size: 6, colors: ["#335a34", "#2c5030", "#3c6238", "#46683a"].map(hex), gap: hex("#0c160e"), seed: 17 };
    const sunlit = { size: 4.4, colors: ["#5a8a4a", "#6a9650", "#4e7e44", "#78a058"].map(hex), gap: hex("#1a2a1a"), seed: 19 };
    SPECS["verdigris-jungle"] = {
      relief: 8, micro: 0.4, canopyFrom: 0.62,
      water: { shallow: "#4c6a50", deep: "#22382c", foam: "#8aa088" },
      road: { fill: "#9a845e", casing: "#3e3020" },
      prepare: (region, T) => ({ roads: roadSegments(region, T), r: region }),
      field(x, y, o, T, c, region) {
        const R = c.r, n = T.fbm(x, y, 4, 0.006, 0);
        const outer = area(T, x, y, R.outerCanopy, 80, 70), dense = area(T, x, y, R.denseCanopy, 60, 60), old = area(T, x, y, R.oldGrowth, 50, 40), high = area(T, x, y, R.highCanopy, 50, 40);
        const open = T.smooth(4, 11, roadDistance(T, x, y, c.roads, 30)) * T.smooth(24, 48, settlementsNear(region, x, y));
        const pools = T.fbm(x, y, 4, 0.02, 3) + dense * 0.1 - 0.42 - (1 - open) * 0.5;
        o.h = 60 + T.fbm(x, y, 4, 0.004, 2) * 160 + high * 140 + n * 30;
        let col = mix(pal.verge, pal.floor, outer); col = mix(col, pal.dense, dense); col = mix(col, pal.old, old); col = mix(col, pal.high, high * 0.6);
        o.color = col;
        o.forest = Math.max(outer * 0.75, dense * 0.95, old, high) * open * (pools > -0.03 ? 0.25 : 1);
        o.pine = old; o.rock = high; o.wet = dense;
        o.water = pools > 0 ? 0.5 + Math.min(0.3, pools * 2) : 0.5 * (1 - T.smooth(0, 0.03, -pools));
      },
      treeKind: () => "jungle",
      paint(px) {
        const f = px.get("forest");
        if (f < 0.45) return;
        const jitter = MapTerrain.hash2(px.x / 9 | 0, px.y / 9 | 0, 7) * 0.3 - 0.15;
        MapTerrain.canopy(px, smooth(0.45, 0.75, f), px.get("pine") + jitter > 0.5 ? giants : px.get("rock") + jitter > 0.5 ? sunlit : jungle);
      },
      cover: (grid, i, roll) => grid.forest[i] < 0.3 && grid.water[i] < 0.4 && roll > 0.92 ? { type: "tuft", color: "#5a7a40", minLevel: 2 } : grid.water[i] > 0.3 && grid.water[i] < 0.5 && roll > 0.6 ? { type: "reeds", color: "#5a7a44" } : null,
      decorate(world, add, T, util) {
        const random = util.rng, R = world.region;
        // A drowned temple in the Old Growth, shrines and toppled idols elsewhere.
        const temple = { x: 470, y: 300 };
        add({ type: "ruin", x: temple.x, y: temple.y, s: 2.2, color: "#7a8a6a", always: true });
        for (let k = 0; k < 10; k++) { const a = k / 10 * Math.PI * 2; add({ type: "column", x: temple.x + Math.cos(a) * 14, y: temple.y + Math.sin(a) * 9, color: "#8a9a78", always: k % 2 === 0 }); }
        add({ type: "statue", x: temple.x, y: temple.y + 16, s: 1.6, color: "#7a8a6a", always: true });
        for (let k = 0; k < 18; k++) { const p = randomIn(random, R.denseCanopy, 0.9); add({ type: random() < 0.5 ? "statue" : "obelisk", x: p.x, y: p.y, color: "#7a8a6a" }); }
        add({ type: "camp", x: 760, y: 700, always: true }); add({ type: "tower", x: 1300, y: 300, color: "#7a8a6a", always: true });
      },
      labels: [
        { text: "OLD GROWTH", x: 420, y: 235 }, { text: "HIGH CANOPY", x: 1220, y: 315 }, { text: "DENSE JUNGLE", x: 590, y: 710 }, { text: "TANGLED VERGE", x: 1300, y: 820 },
      ],
    };
  }

  /* ---------------- Mirrormere Basin ----------------
     A great freshwater lake ringed by its road; reedbeds and fishing piers along the shore,
     the stony Windcut Bluffs to the west, a reed marsh east, and dry steppe fading south into
     the first dunes of the desert. */
  {
    const pal = P({ shore: "#9aa86a", lush: "#86a062", bluff: "#9a9478", bluffTop: "#a8a07e", steppe: "#c4b27a", steppeDry: "#ccb884", fringe: "#d8c290", marsh: "#7e9262" });
    SPECS["mirrormere-basin"] = {
      relief: 9, micro: 0.45,
      water: { shallow: "#86a8aa", deep: "#2c5868", foam: "#e2e2cc" },
      fields: { block: 90, size: [11, 7] },
      prepare: (region, T) => ({ roads: roadSegments(region, T), L: region.lake, M: region.marsh, I: region.island }),
      field(x, y, o, T, c, region) {
        const L = c.L, n = T.fbm(x, y, 4, 0.006, 0);
        const e = Math.hypot((x - L.x) / L.rx, (y - L.y) / L.ry) - T.fbm(x, y, 3, 0.008, 3) * 0.02;
        // The Institute of Magic's island (port task 3, placeholder art) stands dry in the lake's middle.
        const isle = c.I ? 1 - T.smooth(0.82, 1.05, Math.hypot((x - c.I.x) / c.I.rx, (y - c.I.y) / c.I.ry)) : 0;
        const into = (1 - e) * Math.min(L.rx, L.ry) * (1 - isle) - isle * 40;
        const marsh = area(T, x, y, c.M, 40, 40);
        const bluff = T.smooth(470, 380, x + T.fbm(x, y, 3, 0.008, 4) * 50) * (1 - T.smooth(640, 760, y));
        const steppe = T.smooth(640, 720, y + n * 40), fringe = T.smooth(790, 850, y + n * 30);
        const ridges = T.ridged(x, y, 4, 0.008, 1);
        o.h = 120 + n * 30 - (1 - T.smooth(0, 60, -into)) * 30 + bluff * (150 + ridges * 120) + fringe * dunes(T, x, y, 0.4, 0.014) * 35;
        let col = mix(pal.lush, pal.shore, T.smooth(-0.1, 0.3, n) * 0.7 + T.smooth(20, 120, -into) * 0.3);
        col = mix(col, mix(pal.bluff, pal.bluffTop, T.smooth(0.4, 0.7, ridges)), bluff);
        col = mix(col, mix(pal.steppe, pal.steppeDry, T.smooth(-0.1, 0.3, n)), steppe);
        col = mix(col, pal.fringe, fringe); col = mix(col, pal.marsh, marsh * 0.8);
        o.color = col; o.rock = bluff * T.smooth(0.5, 0.8, ridges); o.sand = fringe; o.wet = marsh;
        const pools = T.fbm(x, y, 4, 0.02, 5) + marsh * 0.25 - 0.3;
        o.water = Math.max(shoreWater(T, into, 90), marsh > 0.2 && pools > 0 ? 0.5 + Math.min(0.25, pools) : marsh > 0.2 ? 0.5 * (1 - T.smooth(0, 0.03, -pools)) * marsh : 0);
        o.forest = T.smooth(0.33, 0.47, T.fbm(x, y, 3, 0.012, 2)) * 0.55 * (1 - steppe) * (1 - bluff * 0.5) * (1 - marsh);
        o.farm = farmAmount(T, x, y, region, c.roads, { reach: 150, road: 40 }) * (1 - steppe * 0.7) * (1 - bluff) * (1 - marsh) * T.smooth(4, 14, -into);
      },
      treeKind: (grid, i, roll) => grid.sand[i] > 0.3 ? "scrub" : roll < 0.2 ? "pine" : "broad",
      cover(grid, i, roll) {
        const w = grid.water[i];
        if (w > 0.2 && w < 0.5 && roll > 0.45) return { type: "reeds", color: "#8a8a56" };
        if (grid.wet[i] > 0.5 && roll > 0.8) return { type: "reeds", color: "#8a8a56" };
        if (grid.rock[i] > 0.4) return roll > 0.95 ? { type: "rock", s: 0.7 + roll * 0.4, color: "#8a8478" } : null;
        if (grid.sand[i] > 0.5) return roll > 0.97 ? { type: "tuft", color: "#9a9058", minLevel: 2 } : null;
        return grassCover({ tuft: "#7a8050" })(grid, i, roll);
      },
      decorate(world, add, T, util) {
        const random = util.rng, L = world.region.lake;
        farmland(world, add, T, util, { count: 35 });
        add({ type: "pier", x: 1236, y: 448, len: 26, angle: Math.PI, always: true }); add({ type: "boat", x: 1200, y: 440, always: true }); add({ type: "boat", x: 1196, y: 462, always: true });
        for (let k = 0; k < 24; k++) { const p = randomIn(random, L, 0.85); add({ type: "boat", x: p.x, y: p.y }); }
        // Fishing hamlets and their jetties around the shore.
        for (let k = 0; k < 9; k++) {
          const a = random() * Math.PI * 2, sx = L.x + Math.cos(a) * L.rx * 1.04, sy = L.y + Math.sin(a) * L.ry * 1.04;
          if (sx < 400) continue;
          for (let j = 0; j < 3; j++) add({ type: "house", x: sx + Math.cos(a) * (6 + j * 3), y: sy + Math.sin(a) * (5 + j * 2) + (random() - 0.5) * 4, s: 0.9 });
          add({ type: "pier", x: sx, y: sy, len: 12, angle: a + Math.PI });
        }
        for (let k = 0; k < 500; k++) {
          const x = 40 + random() * 440, y = 40 + random() * 680;
          if (world.sample("rock", x, y) > 0.5 && world.sample("shade", x, y) < 0.4) add({ type: "cliff", x, y, s: 0.8 + random() * 0.5, angle: (random() - 0.5) * 0.8 });
        }
        for (let k = 0; k < 40; k++) add({ type: "dune", x: 80 + random() * 1440, y: 850 + random() * 120, s: 0.8 + random() * 0.8, color: "#a88a52" });
        add({ type: "stones", x: 210, y: 300, always: true }); add({ type: "tower", x: 160, y: 520, always: true }); add({ type: "camp", x: 620, y: 860, always: true });
      },
      labels: [
        { text: "Mirrormere Lake", x: 760, y: 560, kind: "river" }, { text: "WINDCUT BLUFFS", x: 205, y: 235 }, { text: "REED MARSH", x: 1320, y: 680 }, { text: "DRY STEPPE", x: 370, y: 760 }, { text: "DESERT FRINGE", x: 1160, y: 900 },
      ],
    };
  }

  /* ---------------- Saffron Expanse ----------------
     Sun-baked flats of cracked hardpan in the north, great shifting dunes to the south and
     east, white salt pans split into plates, dunes fused to glass in the southeast, and the
     palm-ringed oasis of Miragehold. */
  {
    const pal = P({ sand: "#d8b46c", light: "#e2c27c", deep: "#c89c58", flats: "#c49a64", flatsPale: "#d2ae78", salt: "#ece6d6", glass: "#d0d4cc", oasis: "#7f9a58", oasisDry: "#a8a466" });
    const saltEdge = hex("#c8bca2"), glint = hex("#fbfbf4");
    SPECS["saffron-expanse"] = {
      relief: 9, micro: 0.35,
      water: { shallow: "#6fa0a0", deep: "#2f6670", foam: "#e8e0c0" },
      road: { fill: "#e0cc9c", casing: "#8a6a40", width: 2.2 },
      prepare: (region, T) => ({ roads: roadSegments(region, T), r: region }),
      field(x, y, o, T, c, region) {
        const R = c.r, n = T.fbm(x, y, 4, 0.006, 0);
        const oasis = area(T, x, y, R.oasis, 50, 30), pond = area(T, x, y, { x: R.oasis.x + 10, y: R.oasis.y + 5, rx: R.oasis.rx * 0.32, ry: R.oasis.ry * 0.36 }, 8, 12);
        const salt = area(T, x, y, R.saltPans, 40, 40), glass = area(T, x, y, R.glassDunes, 50, 50);
        const dz = Math.max(T.smooth(560, 620, y + n * 40), T.smooth(1050, 1110, x + n * 40));
        const d = dunes(T, x, y, 0.55, 0.011);
        o.h = 200 + n * 40 + d * (8 + dz * 100 + glass * 40) * (1 - salt) * (1 - oasis * 0.8) - salt * 20;
        let col = mix(pal.flats, pal.flatsPale, T.smooth(-0.2, 0.3, n));
        col = mix(col, mix(pal.deep, pal.light, T.smooth(0.3, 0.9, d)), Math.max(dz, 0.25));
        col = mix(col, pal.salt, salt * 0.95); col = mix(col, pal.glass, glass * 0.7);
        col = mix(col, mix(pal.oasisDry, pal.oasis, T.smooth(0.3, 0.8, oasis)), oasis);
        o.color = col;
        o.sand = (1 - oasis) * Math.max(dz, 0.3); o.snow = salt; o.rock = (1 - dz) * (1 - salt) * (1 - oasis) * 0.8; o.pine = glass;
        o.forest = oasis * T.smooth(-0.2, 0.2, T.fbm(x, y, 3, 0.03, 3)) * 0.8 * (1 - pond);
        o.water = pond > 0.5 ? 0.5 + (pond - 0.5) * 0.5 : pond;
      },
      treeKind: () => "palm",
      paint(px) {
        const salt = px.get("snow"), crack = px.get("rock"), glass = px.get("pine");
        if (salt > 0.15) { const c = MapTerrain.cells(px.x, px.y, 6, 41); if (c.d2 - c.d1 < 0.34) px.tint(saltEdge, salt * 0.8); else px.scale(1 + (c.id - 0.5) * 0.06 * salt); }
        if (crack > 0.2 && px.level >= 2) { const c = MapTerrain.cells(px.x, px.y, 3.4, 43); if (c.d2 - c.d1 < 0.14) px.scale(1 - 0.2 * crack); }
        if (glass > 0.2 && MapTerrain.hash2(px.x * 2 | 0, px.y * 2 | 0, 9) > 0.975) px.tint(glint, glass * 0.9);
        if (px.level >= 4) px.light += Math.sin(px.x * 2.2 + px.y * 1.1 + Math.sin(px.y * 0.3) * 2) * 0.025 * px.get("sand");
      },
      cover(grid, i, roll) {
        if (grid.rock[i] > 0.5 && roll > 0.985) return { type: "rock", s: 0.7 + roll * 0.5, color: "#a8865c" };
        if (grid.pine[i] > 0.5 && roll > 0.97) return { type: "crystal", s: 0.7, color: "#d8e0dc" };
        return null;
      },
      decorate(world, add, T, util) {
        const random = util.rng;
        for (const [x, y] of [[1074, 600], [1080, 572], [648, 410], [600, 412], [1030, 610]]) add({ type: "tent", x, y, s: 1.1, color: "#c8a070" });
        for (let k = 0; k < 6; k++) add({ type: "tent", x: 940 + random() * 30, y: 520 + random() * 20, color: "#b8946a" });
        for (let k = 0; k < 50; k++) add({ type: "bones", x: 80 + random() * 1440, y: 80 + random() * 840 });
        for (let k = 0; k < 8; k++) add({ type: "ribcage", x: 200 + random() * 1200, y: 560 + random() * 380, s: 0.8 + random() * 0.6 });
        for (let k = 0; k < 6; k++) add({ type: "ruin", x: 150 + random() * 1300, y: 120 + random() * 400, color: "#b89a6a" });
        add({ type: "cart", x: 860, y: 140, always: true }); add({ type: "obelisk", x: 1250, y: 300, always: true, color: "#d8d0bc" });
      },
      labels: [
        { text: "MIRAGE OASIS", x: 390, y: 300 }, { text: "SALT PANS", x: 1200, y: 300 }, { text: "SHIFTING DUNES", x: 670, y: 650 }, { text: "GLASS DUNES", x: 1300, y: 770 }, { text: "SUNBURNT FLATS", x: 1000, y: 135 },
      ],
    };
  }

  /* ---------------- Deep Desert ----------------
     Gravel wastes, then towering dunes; a buried necropolis of tombs and avenues around the
     great pyramid; a plain of black obsidian glass in the southeast. */
  {
    const pal = P({ sand: "#dcc496", dune: "#d2b27c", duneLight: "#e6cc98", gravel: "#b4a07e", gravelDark: "#9a8a6c", plaza: "#c8b48a", obsidian: "#3e3846" });
    const obsHi = hex("#9a92b4");
    SPECS["deep-desert"] = {
      relief: 12, micro: 0.4,
      road: { fill: "#e0cc9c", casing: "#8a6a40", width: 2.2 },
      prepare: (region, T) => ({ roads: roadSegments(region, T), r: region }),
      field(x, y, o, T, c, region) {
        const R = c.r, n = T.fbm(x, y, 4, 0.006, 0);
        const necro = area(T, x, y, R.necropolis, 40, 30), plaza = area(T, x, y, R.pyramid, 20, 10), obs = area(T, x, y, R.obsidianFlats, 70, 60);
        const howl = Math.max(T.smooth(560, 640, x + n * 50), T.smooth(600, 660, y + n * 40));
        const d = dunes(T, x, y, 0.35, 0.008);
        o.h = 300 + n * 50 + d * (20 + howl * 150) * (1 - necro * 0.75) * (1 - obs);
        let col = mix(pal.gravel, pal.gravelDark, T.smooth(0, 0.5, T.fbm(x, y, 3, 0.03, 3)) * 0.5);
        col = mix(col, mix(pal.dune, pal.duneLight, T.smooth(0.3, 0.9, d)), howl);
        col = mix(col, pal.sand, necro * 0.7); col = mix(col, pal.plaza, plaza * 0.8); col = mix(col, pal.obsidian, obs);
        o.color = col; o.sand = howl * (1 - obs); o.rock = (1 - howl) * (1 - necro) * 0.7; o.pine = obs; o.wet = plaza;
      },
      paint(px) {
        const obs = px.get("pine"), gravel = px.get("rock"), plaza = px.get("wet");
        if (obs > 0.15) { const c = MapTerrain.cells(px.x, px.y, 5, 51); px.scale(1 + (c.id - 0.5) * 0.35 * obs); if (c.d2 - c.d1 < 0.25) px.tint(obsHi, obs * 0.8); }
        if (gravel > 0.2 && px.level >= 2 && MapTerrain.hash2(px.x * 3 | 0, px.y * 3 | 0, 3) > 0.8) px.scale(1 - 0.15 * gravel);
        if (plaza > 0.3) {
          const u = px.x / 3, vv = px.y / 3 + (Math.floor(u) & 1) * 0.5;
          if (u - Math.floor(u) < 0.08 || vv - Math.floor(vv) < 0.08) px.scale(1 - 0.16 * plaza);
        }
        if (px.level >= 4) px.light += Math.sin(px.x * 2.1 + px.y * 0.8 + Math.sin(px.y * 0.27) * 2) * 0.025 * px.get("sand");
      },
      cover(grid, i, roll) {
        if (grid.pine[i] > 0.4) return roll > 0.94 ? { type: "shard", s: 0.6 + roll * 0.6, color: "#1e1a24", shine: "#9a90b8" } : null;
        if (grid.rock[i] > 0.5) return roll > 0.985 ? { type: "rock", s: 0.6 + roll * 0.4, color: "#a08c6c" } : null;
        return null;
      },
      decorate(world, add, T, util) {
        const random = util.rng, R = world.region, P0 = R.pyramid;
        add({ type: "pyramid", x: P0.x, y: P0.y + 30, s: 2.4, always: true });
        // Processional avenues of obelisks and statues to the four winds.
        for (const [dx, dy] of [[1, 0], [-1, 0], [0, 1], [0, -1]]) for (let k = 0; k < 9; k++) {
          const r = 120 + k * 22, side = dx ? [0, 9] : [9, 0];
          for (const sgn of [-1, 1]) add({ type: k % 2 ? "statue" : "obelisk", x: P0.x + dx * r + side[0] * sgn, y: P0.y + 30 + dy * r * 0.82 + side[1] * sgn, color: "#c8b48a", always: k < 3 });
        }
        for (let k = 0; k < 140; k++) {
          const p = randomIn(random, R.necropolis, 1);
          if (((p.x - P0.x) / P0.rx) ** 2 + ((p.y - P0.y) / P0.ry) ** 2 < 1) continue;
          const pick = random();
          add({ type: pick < 0.4 ? "ruin" : pick < 0.65 ? "column" : pick < 0.8 ? "mound" : "statue", x: p.x, y: p.y, s: 0.8 + random() * 0.6, color: pick < 0.65 ? "#b8a47c" : "#a8946c" });
        }
        add({ type: "statue", x: 560, y: 760, s: 3, always: true });
        for (let k = 0; k < 60; k++) add({ type: "bones", x: 60 + random() * 1480, y: 60 + random() * 880 });
        for (let k = 0; k < 6; k++) add({ type: "ribcage", x: 900 + random() * 600, y: 80 + random() * 400, s: 1 + random() });
        for (const [x, y] of [[300, 248], [356, 252], [312, 292]]) add({ type: "tree", kind: "palm", x, y, s: 1, v: random(), always: true });
        add({ type: "tent", x: 362, y: 288, always: true });
      },
      labels: [
        { text: "OUTER WASTES", x: 270, y: 600 }, { text: "HOWLING DUNES", x: 1120, y: 280 }, { text: "OBSIDIAN FLATS", x: 1330, y: 850 }, { text: "BURIED NECROPOLIS", x: 690, y: 790 }, { text: "THE PYRAMID", x: 820, y: 190 },
      ],
    };
  }

  /* ---------------- Crimson Divide ----------------
     Golden caravan dunes turn blood-red across the divide; a sun-scorched mesa rises in the
     east, red glass flats spread in the southwest and the Heart of the Red Waste lies south. */
  {
    const pal = P({ gold: "#d6b06a", goldLight: "#e2be7a", red: "#a64a36", redDeep: "#86322a", redLight: "#bc5e42", esc: "#a8704c", escTop: "#c08a5c", glass: "#5c1c22" });
    const glassHi = hex("#c04a54");
    SPECS["crimson-divide"] = {
      relief: 12, micro: 0.4,
      road: { fill: "#e0c098", casing: "#7a4a30", width: 2.2 },
      prepare: (region, T) => ({ roads: roadSegments(region, T), r: region }),
      field(x, y, o, T, c, region) {
        const R = c.r, n = T.fbm(x, y, 4, 0.006, 0);
        const red = T.smooth(-50, 50, y - R.crimsonBoundary - T.fbm(x, y, 3, 0.004, 3) * 40);
        const esc = area(T, x, y, R.escarpment, 30, 40), glass = area(T, x, y, R.bloodglass, 45, 45), heart = T.smooth(780, 860, y + n * 40);
        const d = dunes(T, x, y, 0.62, 0.01), ridges = T.ridged(x, y, 3, 0.02, 4);
        const plateau = T.smooth(0.25, 0.6, esc);
        o.h = 250 + n * 40 + d * (40 + heart * 110) * (1 - plateau) * (1 - glass * 0.8) + plateau * (260 + ridges * 50);
        const gold = mix(pal.gold, pal.goldLight, T.smooth(0.3, 0.9, d));
        const crimson = mix(mix(pal.red, pal.redLight, T.smooth(0.3, 0.9, d)), pal.redDeep, heart * 0.6);
        let col = mix(gold, crimson, red);
        col = mix(col, mix(pal.esc, pal.escTop, T.smooth(0.5, 0.9, esc)), plateau);
        col = mix(col, pal.glass, glass);
        o.color = col; o.sand = (1 - plateau) * (1 - glass); o.rock = plateau * (1 - plateau) * 4; o.pine = glass; o.snow = red;
      },
      paint(px) {
        const glass = px.get("pine");
        if (glass > 0.15) { const c = MapTerrain.cells(px.x, px.y, 4.5, 71); px.scale(1 + (c.id - 0.5) * 0.4 * glass); if (c.d2 - c.d1 < 0.22) px.tint(glassHi, glass * 0.7); }
        if (px.level >= 4) px.light += Math.sin(px.x * 2.3 + px.y * 0.9 + Math.sin(px.y * 0.31) * 2) * 0.025 * px.get("sand");
      },
      cover(grid, i, roll) {
        if (grid.pine[i] > 0.4) return roll > 0.93 ? { type: "shard", s: 0.6 + roll * 0.7, color: "#4a1418", shine: "#e06070" } : null;
        if (grid.rock[i] > 0.6) return roll > 0.9 ? { type: "rock", s: 0.8 + roll * 0.5, color: "#9a6448" } : null;
        return null;
      },
      decorate(world, add, T, util) {
        const random = util.rng, R = world.region;
        for (let k = 0; k < 700; k++) {
          const p = randomIn(random, R.escarpment, 1.15);
          if (world.sample("rock", p.x, p.y) > 0.75) add({ type: "cliff", x: p.x, y: p.y, s: 0.8 + random() * 0.5, angle: Math.atan2(p.y - R.escarpment.y, p.x - R.escarpment.x) + Math.PI / 2 });
        }
        for (const [x, y] of [[840, 330], [790, 340], [850, 296], [770, 300]]) add({ type: "tent", x, y, s: 1.1, color: "#c89a6a" });
        for (let k = 0; k < 70; k++) add({ type: "bones", x: 60 + random() * 1480, y: 60 + random() * 880 });
        for (let k = 0; k < 40; k++) add({ type: "tree", kind: "dead", x: 60 + random() * 1480, y: 480 + random() * 480, s: 0.9 + random() * 0.4, v: random() });
        for (let k = 0; k < 9; k++) add({ type: "ribcage", x: 300 + random() * 1100, y: 600 + random() * 360, s: 1 + random() * 0.8 });
        add({ type: "tower", x: 560, y: 560, always: true }); add({ type: "ruin", x: 1180, y: 760, always: true }); add({ type: "ruin", x: 300, y: 160, always: true }); add({ type: "cart", x: 900, y: 120, always: true });
      },
      labels: [
        { text: "CARAVAN DUNES", x: 335, y: 180 }, { text: "SUNSCORCHED ESCARPMENT", x: 1270, y: 345 }, { text: "OPEN DESERT", x: 390, y: 430 }, { text: "CRIMSON DUNES", x: 1210, y: 645 },
        { text: "BLOODGLASS FLATS", x: 300, y: 730 }, { text: "HEART OF THE RED WASTE", x: 1070, y: 925 },
      ],
    };
  }

  /* ---------------- Bloodstone Coast ----------------
     Crimson dunes and crags, a forest of giant stone spires, the dark-red Blood Coast and a
     sea the colour of old wine beneath Nocturne Keep. */
  {
    const pal = P({ red: "#a44c3a", dune: "#b65a40", duneDark: "#94402e", crag: "#7a4c42", cragTop: "#8e5a4a", shore: "#6a2c28", wet: "#4a1e1e" });
    SPECS["bloodstone-coast"] = {
      relief: 11, micro: 0.5,
      water: { shallow: "#7a3034", deep: "#2a0c14", foam: "#d0a098" },
      road: { fill: "#c89070", casing: "#5a2a20" },
      prepare: (region, T) => ({ roads: roadSegments(region, T), r: region, shore: region.landShape.shoreY }),
      field(x, y, o, T, c, region) {
        const R = c.r, n = T.fbm(x, y, 4, 0.006, 0);
        const shoreY = c.shore + T.fbm(x, y, 4, 0.006, 3) * 34 - 4;
        const rocks = Math.min(1, R.rockFields.reduce((sum, e) => sum + area(T, x, y, e, 60, 50), 0));
        const coast = area(T, x, y, R.bloodShore, 50, 40);
        const d = dunes(T, x, y, 0.3, 0.011), ridges = T.ridged(x, y, 4, 0.012, 1);
        o.h = 220 - T.smooth(500, 860, y) * 200 + n * 40 + d * 50 * (1 - rocks) * (1 - coast) + rocks * ridges * 260;
        let col = mix(pal.red, mix(pal.duneDark, pal.dune, T.smooth(0.3, 0.9, d)), 0.7);
        col = mix(col, mix(pal.crag, pal.cragTop, T.smooth(0.4, 0.8, ridges)), rocks * T.smooth(0.3, 0.6, ridges + 0.2));
        col = mix(col, pal.shore, coast * 0.8);
        col = mix(col, pal.wet, T.smooth(-24, 0, y - shoreY));
        o.color = col; o.rock = rocks * T.smooth(0.4, 0.7, ridges); o.sand = 1 - rocks; o.wet = coast;
        o.water = shoreWater(T, y - shoreY, 80);
      },
      paint(px) { if (px.level >= 4) px.light += Math.sin(px.x * 2.2 + px.y * 1.2 + Math.sin(px.y * 0.29) * 2) * 0.025 * px.get("sand"); },
      cover(grid, i, roll) {
        if (grid.rock[i] > 0.4) return roll > 0.93 ? { type: "rock", s: 0.8 + roll * 0.6, color: "#7a4a40" } : null;
        if (grid.wet[i] > 0.5 && grid.water[i] < 0.45) return roll > 0.97 ? { type: "pool", s: 0.6 + roll * 0.4, color: "#5a1418" } : null;
        return null;
      },
      decorate(world, add, T, util) {
        const random = util.rng, R = world.region;
        for (let k = 0; k < 90; k++) { const p = randomIn(random, R.rockFields[1], 0.95); if (!util.near(p.x, p.y, 8)) add({ type: "spire", x: p.x, y: p.y, s: 1.2 + random() * 2, color: "#6e3a32", always: k < 12 }); }
        for (let k = 0; k < 24; k++) { const p = randomIn(random, R.rockFields[0], 0.9); add({ type: "spire", x: p.x, y: p.y, s: 0.7 + random() * 0.8, color: "#74443a" }); }
        for (let k = 0; k < 800; k++) {
          const x = 60 + random() * 1480, y = 60 + random() * 780;
          if (world.sample("rock", x, y) > 0.55 && world.sample("shade", x, y) < 0.4) add({ type: "cliff", x, y, s: 0.8 + random() * 0.5, angle: (random() - 0.5) * 0.8 });
        }
        for (let k = 0; k < 5; k++) add({ type: "wreck", x: 200 + random() * 1200, y: 835 + random() * 10, angle: random() - 0.5 });
        for (let k = 0; k < 40; k++) add({ type: "grave", x: 1190 + random() * 100, y: 740 + random() * 30 });
        for (let k = 0; k < 60; k++) add({ type: "bones", x: 60 + random() * 1480, y: 60 + random() * 760 });
        for (let k = 0; k < 30; k++) add({ type: "tree", kind: "dead", x: 60 + random() * 1480, y: 300 + random() * 500, s: 0.9 + random() * 0.4, v: random() });
      },
      labels: [
        { text: "REDWIND DUNES", x: 510, y: 165 }, { text: "WESTERN CRAGS", x: 305, y: 395 }, { text: "GIANT ROCK SPIRES", x: 1120, y: 430 }, { text: "SANGUINE WASTES", x: 665, y: 615 }, { text: "BLOOD COAST", x: 705, y: 795 },
        { text: "The Blood Tide", x: 980, y: 935, kind: "river" },
      ],
    };
  }

  /* ---------------- Ancient Battlefield ----------------
     Churned, cratered ground where the war never ended: zig-zag trenches, bone pits, rows of
     grave mounds, rusted blades and tattered banners, and a causeway broken into four pieces. */
  {
    const pal = P({ grass: "#8e8a62", dead: "#7c7454", ash: "#5a554a", mud: "#5e5040", blood: "#6e3a2e", pit: "#463c30", grave: "#70664e" });
    SPECS["ancient-battlefield"] = {
      relief: 10, micro: 0.6,
      road: { fill: "#a8a08a", casing: "#4a4236", width: 2.8 },
      prepare: (region, T) => ({ roads: roadSegments(region, T), r: region }),
      field(x, y, o, T, c, region) {
        const R = c.r, n = T.fbm(x, y, 4, 0.006, 0);
        const pits = area(T, x, y, R.bonePits, 50, 40), graves = area(T, x, y, R.massGraves, 50, 40), trench = area(T, x, y, R.trenches, 50, 40);
        const war = 1 - T.smooth(1100, 1300, x + n * 60);
        o.h = 140 + n * 60 + T.fbm(x, y, 3, 0.002, 2) * 90 + T.ridged(x, y, 3, 0.006, 1) * 70 - pits * 40 + graves * 25;
        let col = mix(pal.grass, pal.dead, T.smooth(-0.2, 0.3, n));
        col = mix(col, pal.ash, T.smooth(0.05, 0.35, T.fbm(x, y, 3, 0.008, 3)) * 0.8 * war);
        col = mix(col, pal.mud, trench * 0.7); col = mix(col, pal.pit, pits * 0.5); col = mix(col, pal.grave, graves * 0.6);
        col = mix(col, pal.blood, T.smooth(0.25, 0.45, T.fbm(x, y, 3, 0.02, 4)) * Math.max(trench, pits) * 0.5);
        o.color = col;
        o.rock = Math.max(trench * 0.9, pits * 0.7, 0.35 * war); o.wet = trench; o.pine = pits; o.snow = graves;
        o.forest = T.smooth(0.35, 0.5, T.fbm(x, y, 3, 0.012, 2)) * 0.35 * (1 - trench) * (1 - pits);
      },
      treeKind: () => "dead",
      paint(px) {
        const war = px.get("rock"), trench = px.get("wet");
        if (war > 0.1) {
          // Shell craters: a dark bowl inside a pale rim.
          const c = MapTerrain.cells(px.x, px.y, 12, 61), r = 2 + c.id * 4.5;
          if (c.id < 0.3 + war * 0.6) { const t = c.d1 / r; if (t < 0.78) px.scale(1 - 0.32 * war * (1 - t * 0.5)); else if (t < 1) px.scale(1 + 0.16 * war); }
        }
        if (trench > 0.3) {
          // Zig-zag trench lines with a pale parapet.
          const z = Math.abs(((px.y + Math.abs(((px.x / 5) % 2 + 2) % 2 - 1) * 4) % 26 + 26) % 26 - 13);
          if (z < 0.8) px.scale(1 - 0.5 * trench); else if (z < 1.5) px.scale(1 + 0.12 * trench);
        }
      },
      cover(grid, i, roll) {
        if (grid.pine[i] > 0.4 && roll > 0.8) return { type: "bones" };
        if (grid.snow[i] > 0.5 && roll > 0.9) return { type: "grave" };
        if (grid.rock[i] > 0.25 && roll > 0.96) return { type: "weapon" };
        return roll > 0.96 ? { type: "tuft", color: "#7a7450", minLevel: 2 } : null;
      },
      decorate(world, add, T, util) {
        const random = util.rng, R = world.region;
        for (let k = 0; k < 90; k++) { const p = randomIn(random, R.massGraves, 0.85); add({ type: "mound", x: p.x, y: p.y, s: 0.8 + random() * 0.6, angle: (random() - 0.5) * 0.3 }); }
        for (let k = 0; k < 40; k++) { const p = randomIn(random, R.bonePits, 0.85); add({ type: "crater", x: p.x, y: p.y, s: 1.5 + random() * 2 }); }
        for (let k = 0; k < 16; k++) add({ type: "ribcage", x: 100 + random() * 1400, y: 100 + random() * 800, s: 1 + random() });
        for (let k = 0; k < 45; k++) add({ type: "banner", x: 100 + random() * 1400, y: 100 + random() * 800, color: random() < 0.5 ? "#6e2a24" : "#3a4a6a" });
        for (let k = 0; k < 22; k++) add({ type: "cart", x: 100 + random() * 1400, y: 100 + random() * 800, s: 1 + random() * 0.6 });
        // Rubble across the gaps where the causeway is gone.
        for (const [a, b] of [[{ x: 1220, y: 540 }, { x: 1085, y: 530 }], [{ x: 780, y: 480 }, { x: 635, y: 485 }], [{ x: 350, y: 500 }, { x: 245, y: 500 }]])
          for (let k = 0; k < 26; k++) { const t = random(); add({ type: "rock", x: a.x + (b.x - a.x) * t + (random() - 0.5) * 8, y: a.y + (b.y - a.y) * t + (random() - 0.5) * 6, s: 0.5 + random() * 0.5, color: "#a8a08a" }); }
        add({ type: "tower", x: 1400, y: 760, color: "#7a7462", always: true }); add({ type: "camp", x: 1420, y: 420, always: true }); add({ type: "ruin", x: 300, y: 300, always: true });
      },
      over: clouds([{ x: 640, y: 330, r: 110 }, { x: 1150, y: 690, r: 120 }], "70,64,58", 0.22),
      labels: [
        { text: "SCAVENGER VERGE", x: 1370, y: 205 }, { text: "BONE PITS", x: 1110, y: 245 }, { text: "MASS GRAVES", x: 570, y: 230 }, { text: "SHATTERED CAUSEWAY", x: 810, y: 440 },
        { text: "BLOODIED TRENCHES", x: 1030, y: 785 }, { text: "SILENT WESTFIELD", x: 260, y: 720 },
      ],
    };
  }

  /* ---------------- Duskpine Forest ----------------
     A closed canopy of black pines and firs, frost-rimed at the eastern fringe, darkest in the
     Deep Boreal; a frozen bog in the southwest and the sunken Shadowroot Hollow. */
  {
    const pal = P({ floor: "#3e4a40", dark: "#2c3830", frostGround: "#8e9c98", bog: "#58645a", hollow: "#262a2a", clearing: "#5c6858" });
    const pines = { size: 2.3, pine: true, colors: ["#2a4640", "#30504a", "#25403b", "#36564e"].map(hex), gap: hex("#0c1412"), seed: 29 };
    const black = { size: 2.6, pine: true, colors: ["#1e322e", "#243a34", "#1a2c28"].map(hex), gap: hex("#060a09"), seed: 31 };
    const frosted = { size: 2.2, pine: true, colors: ["#6a8480", "#7a9490", "#5c7672", "#8aa4a0"].map(hex), gap: hex("#2a3634"), seed: 33 };
    const rime = hex("#dfe7e6");
    SPECS["duskpine-forest"] = {
      relief: 7, micro: 0.45, canopyFrom: 0.62,
      water: { shallow: "#8aa0a4", deep: "#3e5258", foam: "#e8eeee" },
      prepare: (region, T) => ({ roads: roadSegments(region, T), r: region }),
      field(x, y, o, T, c, region) {
        const R = c.r, n = T.fbm(x, y, 4, 0.006, 0);
        const deep = area(T, x, y, R.deepBoreal, 60, 50), bog = area(T, x, y, R.frostBog, 50, 40), hollow = area(T, x, y, R.shadowHollow, 50, 40);
        const frost = T.smooth(1080, 1180, x + n * 60);
        const open = T.smooth(4, 11, roadDistance(T, x, y, c.roads, 30)) * T.smooth(26, 50, settlementsNear(region, x, y));
        const glade = T.smooth(0.34, 0.46, T.fbm(x, y, 3, 0.012, 3)) * (1 - deep);
        const pools = T.fbm(x, y, 4, 0.024, 4) + bog * 0.3 - 0.32 - (1 - bog) * 0.4;
        o.h = 300 + T.fbm(x, y, 4, 0.003, 2) * 180 + n * 40 - hollow * 120;
        let col = mix(pal.floor, pal.clearing, glade); col = mix(col, pal.dark, deep * 0.6);
        col = mix(col, pal.frostGround, frost * 0.7); col = mix(col, pal.bog, bog); col = mix(col, pal.hollow, hollow * 0.7);
        o.color = col;
        o.forest = Math.max(0.85 * (1 - glade * 0.9), deep) * (1 - bog * 0.85) * (1 - hollow * 0.45) * open;
        o.pine = deep; o.snow = Math.max(frost, bog * 0.7); o.wet = bog; o.rock = hollow;
        o.water = pools > 0 ? 0.5 + Math.min(0.3, pools * 1.5) : 0.5 * (1 - T.smooth(0, 0.03, -pools));
      },
      treeKind: (grid, i, roll) => grid.snow[i] > 0.5 ? "frost" : grid.rock[i] > 0.5 || roll < 0.05 ? "dead" : "dusk",
      paint(px) {
        const frost = px.get("snow");
        if (frost > 0.2 && MapTerrain.hash2(px.x * 2 | 0, px.y * 2 | 0, 7) > 0.72) px.tint(rime, frost * 0.45);
        const f = px.get("forest");
        if (f < 0.45) return;
        const jitter = MapTerrain.hash2(px.x / 8 | 0, px.y / 8 | 0, 3) * 0.3 - 0.15;
        MapTerrain.canopy(px, smooth(0.45, 0.75, f), frost + jitter > 0.5 ? frosted : px.get("pine") + jitter > 0.5 ? black : pines);
      },
      cover(grid, i, roll) {
        if (grid.wet[i] > 0.5 && grid.water[i] < 0.5) return roll > 0.85 ? { type: "tuft", color: "#c8d0cc" } : null;
        if (grid.rock[i] > 0.5) return roll > 0.9 ? { type: "tree", kind: "dead", s: 0.9 } : null;
        return roll > 0.97 ? { type: "tuft", color: "#5a6a58", minLevel: 2 } : null;
      },
      decorate(world, add, T, util) {
        const random = util.rng;
        for (let k = 0; k < 16; k++) add({ type: "stones", x: 100 + random() * 1400, y: 100 + random() * 800 });
        for (let k = 0; k < 40; k++) add({ type: "bones", x: 1000 + random() * 500, y: 600 + random() * 300 });
        add({ type: "ruin", x: 560, y: 230, color: "#6a6a62", always: true }); add({ type: "camp", x: 1180, y: 470, always: true }); add({ type: "house", x: 1010, y: 520, always: true }); add({ type: "tower", x: 360, y: 640, always: true });
      },
      over: clouds([{ x: 620, y: 300, r: 160 }, { x: 1240, y: 760, r: 170 }, { x: 320, y: 780, r: 140 }, { x: 980, y: 160, r: 130 }], "150,160,160", 0.18),
      labels: [
        { text: "FROSTPINE FRINGE", x: 1370, y: 300 }, { text: "DEEP BOREAL", x: 440, y: 340 }, { text: "BLACKPINE WOODS", x: 480, y: 690 }, { text: "FROSTBOUND BOG", x: 310, y: 780 }, { text: "SHADOWROOT HOLLOW", x: 1240, y: 760 },
      ],
    };
  }

  // Soft cloud, mist or smoke banks drawn over the painted land.
  function clouds(banks, color = "246,244,236", alpha = 0.55) {
    return (ctx, world, view) => {
      for (const bank of banks) {
        if (bank.x + bank.r * 2.6 < view.x0 || bank.x - bank.r * 2.6 > view.x1 || bank.y + bank.r * 1.2 < view.y0 || bank.y - bank.r * 1.2 > view.y1) continue;
        for (let k = 0; k < 7; k++) {
          const ox = (MapTerrain.hash2(bank.x | 0, k, 5) - 0.5) * bank.r * 1.8, oy = (MapTerrain.hash2(bank.y | 0, k, 6) - 0.5) * bank.r * 0.6, r = bank.r * (0.4 + MapTerrain.hash2(k, bank.r | 0, 7) * 0.4);
          const gradient = ctx.createRadialGradient(bank.x + ox, bank.y + oy, 0, bank.x + ox, bank.y + oy, r);
          gradient.addColorStop(0, `rgba(${color},${alpha})`); gradient.addColorStop(0.6, `rgba(${color},${alpha * 0.5})`); gradient.addColorStop(1, `rgba(${color},0)`);
          ctx.fillStyle = gradient; ctx.beginPath(); ctx.ellipse(bank.x + ox, bank.y + oy, r * 1.5, r * 0.6, 0, 0, Math.PI * 2); ctx.fill();
        }
      }
    };
  }

  window.MapRegions = { spec: id => SPECS[id] || null, ids: () => Object.keys(SPECS), helpers: { P, mix, tint, area, farmland, hamlets, grassCover, clouds, mountain, mountainColors, mountainPal, roadSegments, nearRoads, settlementsNear } };
})();
