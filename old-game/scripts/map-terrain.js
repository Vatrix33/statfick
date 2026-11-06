/* Regional map terrain: seamless ink-and-parchment landscapes, painted procedurally in tiles.
   A region's look is a field function (map-regions.js) that tells, for any point, its colour,
   height and cover (forest, rock, water, sand, snow, marsh...). This module turns it into:
   - a field grid (CELL units per cell) computed once per region, in slices,
   - deterministic features (trees, rocks, reeds, dunes, farms, ruins...) indexed by area,
   - painted tiles at 1, 2, 4 or 8 pixels per map unit, requested by the map view.
   Shapes are warped by noise so zones blend into each other instead of ending at a line;
   relief comes from the height field as hillshade, engraved hatching and contours. */
(() => {
  "use strict";
  const W = 1600, H = 1000, CELL = 2, GW = W / CELL + 1, GH = H / CELL + 1;
  const INK = "#1a120c";
  const TILE = 256, LEVELS = [1, 2, 4, 8];
  // Port task 8: regions are drawn on one continent; their edges fade over FEATHER units into the ground between them.
  const CONTINENT = Boolean(window.ContinentRules), FEATHER = 40;

  /* ---------------- Noise ---------------- */
  function rng(seed) {
    let state = (Math.abs(Math.floor(seed)) % 2147483646) + 1;
    return () => { state = state * 16807 % 2147483647; return (state - 1) / 2147483646; };
  }
  const hashString = text => [...String(text)].reduce((value, char) => Math.imul(value ^ char.charCodeAt(0), 16777619) >>> 0, 2166136261);
  function hash2(x, y, seed = 0) {
    let h = Math.imul(x | 0, 374761393) ^ Math.imul(y | 0, 668265263) ^ Math.imul(seed | 0, 2147483647);
    h = Math.imul(h ^ (h >>> 13), 1274126177);
    return ((h ^ (h >>> 16)) >>> 0) / 4294967296;
  }
  // 2D gradient noise in about −1..1.
  function makeNoise(seed) {
    const random = rng(seed), perm = new Uint8Array(512), p = Array.from({ length: 256 }, (_, i) => i);
    for (let i = 255; i > 0; i--) { const j = Math.floor(random() * (i + 1)); [p[i], p[j]] = [p[j], p[i]]; }
    for (let i = 0; i < 512; i++) perm[i] = p[i & 255];
    const GX = [1, -1, 1, -1, 1.414, -1.414, 0, 0], GY = [1, 1, -1, -1, 0, 0, 1.414, -1.414];
    return (x, y) => {
      const xi = Math.floor(x), yi = Math.floor(y), xf = x - xi, yf = y - yi;
      const X = xi & 255, Y = yi & 255;
      const g = (ix, iy, dx, dy) => { const k = perm[ix + perm[iy]] & 7; return GX[k] * dx + GY[k] * dy; };
      const u = xf * xf * xf * (xf * (xf * 6 - 15) + 10), v = yf * yf * yf * (yf * (yf * 6 - 15) + 10);
      const a = g(X, Y, xf, yf), b = g(X + 1, Y, xf - 1, yf), c = g(X, Y + 1, xf, yf - 1), d = g(X + 1, Y + 1, xf - 1, yf - 1);
      return (a + u * (b - a)) + v * ((c + u * (d - c)) - (a + u * (b - a)));
    };
  }
  function makeToolkit(seed) {
    const noises = Array.from({ length: 6 }, (_, index) => makeNoise(seed * 31 + index * 977));
    const fbm = (x, y, octaves = 4, scale = 0.01, which = 0) => {
      const noise = noises[which % noises.length];
      let sum = 0, amp = 1, freq = scale, norm = 0;
      for (let o = 0; o < octaves; o++) { sum += noise(x * freq, y * freq) * amp; norm += amp; amp *= 0.5; freq *= 2.03; }
      return sum / norm;
    };
    const ridged = (x, y, octaves = 4, scale = 0.01, which = 1) => {
      const noise = noises[which % noises.length];
      let sum = 0, amp = 1, freq = scale, norm = 0;
      for (let o = 0; o < octaves; o++) { const n = 1 - Math.abs(noise(x * freq, y * freq)); sum += n * n * amp; norm += amp; amp *= 0.5; freq *= 2.05; }
      return sum / norm;
    };
    const clamp = (v, a = 0, b = 1) => v < a ? a : v > b ? b : v;
    const smooth = (a, b, v) => { const t = clamp((v - a) / (b - a)); return t * t * (3 - 2 * t); };
    // Membership of a noise-warped ellipse: 1 inside, 0 outside, a soft rim of `feather` units.
    const area = (x, y, e, feather = 40, warp = 40, which = 2, scale = 0.006) => {
      if (!e) return 0;
      const wx = x + fbm(x, y, 3, scale, which) * warp, wy = y + fbm(x + 311, y - 177, 3, scale, which + 1) * warp;
      const d = Math.hypot((wx - e.x) / e.rx, (wy - e.y) / e.ry);
      const rim = feather / Math.max(1, Math.min(e.rx, e.ry));
      return 1 - smooth(1 - rim, 1 + rim, d);
    };
    // Distance from a point to a polyline (array of {x, y}).
    const segmentsOf = path => (path || []).slice(1).map((end, index) => {
      const start = path[index], dx = end.x - start.x, dy = end.y - start.y;
      return { x: start.x, y: start.y, dx, dy, l2: dx * dx + dy * dy || 1, minX: Math.min(start.x, end.x), maxX: Math.max(start.x, end.x), minY: Math.min(start.y, end.y), maxY: Math.max(start.y, end.y) };
    });
    const pathDistance = (x, y, segments, limit = Infinity) => {
      let best = limit * limit;
      for (const s of segments) {
        if (x < s.minX - limit || x > s.maxX + limit || y < s.minY - limit || y > s.maxY + limit) continue;
        const t = clamp(((x - s.x) * s.dx + (y - s.y) * s.dy) / s.l2);
        const ex = x - (s.x + s.dx * t), ey = y - (s.y + s.dy * t), d2 = ex * ex + ey * ey;
        if (d2 < best) best = d2;
      }
      return Math.sqrt(best);
    };
    const hex = value => { const v = value.replace("#", ""); return [0, 2, 4].map(i => parseInt(v.slice(i, i + 2), 16)); };
    const mix = (a, b, t) => [a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t, a[2] + (b[2] - a[2]) * t];
    return { noises, fbm, ridged, clamp, smooth, area, segmentsOf, pathDistance, hex, mix, hash2, rng };
  }

  // Every colour passes through the map's ink grading, as the old SVG maps did.
  const toneCache = new Map();
  function tone(hex) {
    if (toneCache.has(hex)) return toneCache.get(hex);
    const toned = window.MapInk?.tone ? window.MapInk.tone(hex) : hex;
    const value = toned.replace("#", ""), rgb = [0, 2, 4].map(i => parseInt(value.slice(i, i + 2), 16));
    toneCache.set(hex, rgb);
    return rgb;
  }

  /* ---------------- Region world ---------------- */
  const CHANNELS = ["r", "g", "b", "h", "water", "forest", "pine", "rock", "wet", "sand", "snow", "farm", "glow"];
  function createWorld(region, spec) {
    const seed = hashString(region.id), T = makeToolkit(seed % 100000);
    const grid = Object.fromEntries(CHANNELS.map(key => [key, new Float32Array(GW * GH)]));
    const shade = new Float32Array(GW * GH);
    const context = spec.prepare ? spec.prepare(region, T) : {};
    const out = { color: [0, 0, 0] };
    let row = 0, ready = false;
    const fieldAt = (x, y) => {
      out.color = [200, 190, 150]; out.h = 0; out.water = 0; out.forest = 0; out.pine = 0; out.rock = 0; out.wet = 0; out.sand = 0; out.snow = 0; out.farm = 0; out.glow = 0;
      spec.field(x, y, out, T, context, region);
      return out;
    };
    // Computes a slice of grid rows; returns true when the grid is complete.
    function build(budgetMs = 12) {
      const end = performance.now() + budgetMs;
      while (row < GH && performance.now() < end) {
        const y = row * CELL;
        for (let column = 0; column < GW; column++) {
          const x = column * CELL, f = fieldAt(x, y), i = row * GW + column;
          grid.r[i] = f.color[0]; grid.g[i] = f.color[1]; grid.b[i] = f.color[2];
          grid.h[i] = f.h; grid.water[i] = f.water; grid.forest[i] = f.forest; grid.pine[i] = f.pine; grid.rock[i] = f.rock;
          grid.wet[i] = f.wet; grid.sand[i] = f.sand; grid.snow[i] = f.snow; grid.farm[i] = f.farm; grid.glow[i] = f.glow;
        }
        row++;
      }
      if (row < GH) return false;
      if (!ready) finish();
      return true;
    }
    function finish() {
      // Slope-dependent colours (rock on steep ground, snow that cannot hold) need the full
      // height grid, so a spec may colour cells in a second pass.
      if (spec.colorize) for (let y = 0; y < GH; y++) for (let x = 0; x < GW; x++) {
        const i = y * GW + x;
        const dx = (grid.h[y * GW + Math.min(GW - 1, x + 1)] - grid.h[y * GW + Math.max(0, x - 1)]) / (CELL * 2);
        const dy = (grid.h[Math.min(GH - 1, y + 1) * GW + x] - grid.h[Math.max(0, y - 1) * GW + x]) / (CELL * 2);
        const cell = { color: [grid.r[i], grid.g[i], grid.b[i]], h: grid.h[i], water: grid.water[i], forest: grid.forest[i], pine: grid.pine[i], rock: grid.rock[i], wet: grid.wet[i], sand: grid.sand[i], snow: grid.snow[i], farm: grid.farm[i], glow: grid.glow[i] };
        spec.colorize(cell, Math.hypot(dx, dy) / 75, x * CELL, y * CELL, T, context, region);
        grid.r[i] = cell.color[0]; grid.g[i] = cell.color[1]; grid.b[i] = cell.color[2];
        grid.forest[i] = cell.forest; grid.pine[i] = cell.pine; grid.rock[i] = cell.rock; grid.snow[i] = cell.snow; grid.wet[i] = cell.wet; grid.sand[i] = cell.sand;
      }
      // Hillshade: light from the north-west, exaggerated by the region's relief.
      const reliefAt = typeof spec.relief === "function" ? spec.relief : () => spec.relief ?? 1;
      for (let y = 0; y < GH; y++) for (let x = 0; x < GW; x++) {
        const i = y * GW + x, relief = reliefAt(grid.h[i]);
        const dx = (grid.h[y * GW + Math.min(GW - 1, x + 1)] - grid.h[y * GW + Math.max(0, x - 1)]) / (CELL * 2);
        const dy = (grid.h[Math.min(GH - 1, y + 1) * GW + x] - grid.h[Math.max(0, y - 1) * GW + x]) / (CELL * 2);
        const lit = (-dx * -0.7 - dy * -0.72) * relief * 0.016;
        shade[i] = Math.max(0, Math.min(1, 0.5 + lit));
      }
      world.features = buildFeatures(world);
      ready = true;
    }
    // Bilinear sample of a channel at a map point.
    function sample(channel, x, y) {
      const gx = Math.max(0, Math.min(GW - 1.001, x / CELL)), gy = Math.max(0, Math.min(GH - 1.001, y / CELL));
      const x0 = gx | 0, y0 = gy | 0, fx = gx - x0, fy = gy - y0, i = y0 * GW + x0, data = channel === "shade" ? shade : grid[channel];
      return (data[i] * (1 - fx) + data[i + 1] * fx) * (1 - fy) + (data[i + GW] * (1 - fx) + data[i + GW + 1] * fx) * fy;
    }
    const world = { region, spec, T, grid, shade, context, build, sample, fieldAt, features: null, get ready() { return ready; }, progress: () => row / GH, tiles: new Map() };
    world.roads = (region.roads || []).map(path => ({ path, segments: T.segmentsOf(path) }));
    world.river = region.river ? { path: region.river, segments: T.segmentsOf(region.river) } : null;
    world.settlements = (region.settlements || []).map(place => settlementPlan(place, world));
    return world;
  }

  /* ---------------- Settlements ----------------
     Castle cities are walled with towers, a keep and streets; towns are a square of houses
     around a church or hall; both sit among small fields. Their windows light the night. */
  function settlementPlan(place, world) {
    const random = rng(hashString(place.id)), castle = place.kind === "Castle city";
    const radius = castle ? 21 : 12, houses = [], fields = [];
    const count = castle ? 46 : 16;
    for (let i = 0; i < count; i++) {
      const angle = random() * Math.PI * 2, r = (castle ? 6 : 3.5) + Math.sqrt(random()) * (radius - (castle ? 9 : 4));
      const x = place.x + Math.cos(angle) * r, y = place.y + Math.sin(angle) * r * 0.82;
      houses.push({ x, y, w: 1.5 + random() * 1.4, d: 1.2 + random() * 1.1, roof: random() < 0.5 ? "#6e3b28" : "#4a3a30", lit: random() < 0.6, angle: (random() - 0.5) * 0.5 });
    }
    houses.sort((a, b) => a.y - b.y);
    const fieldCount = castle ? 14 : 9;
    for (let i = 0; i < fieldCount; i++) {
      const angle = random() * Math.PI * 2, r = radius + 6 + random() * (castle ? 22 : 16);
      const x = place.x + Math.cos(angle) * r, y = place.y + Math.sin(angle) * r * 0.8;
      fields.push({ x, y, w: 6 + random() * 7, h: 4 + random() * 5, angle: random() * Math.PI, tone: random() });
    }
    return { place, castle, radius, houses, fields, towers: castle ? 8 : 0 };
  }

  /* ---------------- Features ---------------- */
  const KEEP = { 1: 0.16, 2: 0.38, 4: 0.75, 8: 1 };
  function buildFeatures(world) {
    const { grid, spec, T, region } = world, list = [], seed = hashString(region.id + "features");
    const add = feature => { feature.rank = hash2(Math.round(feature.x * 13), Math.round(feature.y * 17), seed); feature.reach = feature.reach || REACH[feature.type]?.(feature.s || 1) || 14; list.push(feature); };
    const near = (x, y, limit) => world.roads.some(road => T.pathDistance(x, y, road.segments, limit) < limit) || (world.river && T.pathDistance(x, y, world.river.segments, limit + 4) < limit + 4);
    const nearTown = (x, y, pad = 6) => world.settlements.some(plan => Math.hypot(x - plan.place.x, y - plan.place.y) < plan.radius + pad);
    // Trees and cover, from the grid: one candidate per cell, jittered, thinned by density.
    for (let gy = 0; gy < GH - 1; gy++) for (let gx = 0; gx < GW - 1; gx++) {
      const i = gy * GW + gx, x0 = gx * CELL, y0 = gy * CELL;
      if (grid.water[i] > 0.42) continue;
      const jitter = hash2(gx, gy, seed), jitter2 = hash2(gx, gy, seed + 1), roll = hash2(gx, gy, seed + 2);
      const x = x0 + jitter * CELL, y = y0 + jitter2 * CELL;
      const forest = grid.forest[i];
      if (forest > 0.04 && roll < forest * 0.62) {
        if (near(x, y, 3.2) || nearTown(x, y, 2)) continue;
        if (spec.canopyFrom && forest > spec.canopyFrom + (jitter - 0.5) * 0.12) continue;
        const kind = spec.treeKind ? spec.treeKind(grid, i, roll, x, y) : grid.pine[i] > 0.5 ? "pine" : "broad";
        add({ type: "tree", kind, x, y, s: 0.8 + hash2(gx, gy, seed + 3) * 0.55 + forest * 0.25, v: hash2(gx, gy, seed + 4) });
        continue;
      }
      if (spec.cover) {
        const extra = spec.cover(grid, i, roll, x, y, T);
        if (extra && !near(x, y, 2.5) && !nearTown(x, y, 1)) add({ ...extra, x, y, v: hash2(gx, gy, seed + 5) });
      }
    }
    // Region decorations: farms, ruins, landmarks...
    if (spec.decorate) spec.decorate(world, add, T, { near, nearTown, rng: rng(seed + 9) });
    // Bridges where roads cross the river.
    if (world.river) for (const road of world.roads) for (let k = 1; k < road.path.length; k++) {
      const a = road.path[k - 1], b = road.path[k];
      for (const s of world.river.segments) {
        const d = (b.x - a.x) * s.dy - (b.y - a.y) * s.dx;
        if (Math.abs(d) < 1e-6) continue;
        const t = ((s.x - a.x) * s.dy - (s.y - a.y) * s.dx) / d, u = ((s.x - a.x) * (b.y - a.y) - (s.y - a.y) * (b.x - a.x)) / d;
        if (t >= 0 && t <= 1 && u >= 0 && u <= 1) list.push({ type: "bridge", x: a.x + (b.x - a.x) * t, y: a.y + (b.y - a.y) * t, angle: Math.atan2(b.y - a.y, b.x - a.x), rank: 0 });
      }
    }
    // Spatial index: 32-unit buckets; big features (pyramids, spires) are found from further away.
    const buckets = new Map();
    let maxReach = 14;
    for (const feature of list) { if (!feature.reach) feature.reach = 14; if (feature.reach > maxReach) maxReach = feature.reach; }
    for (const feature of list) {
      const key = ((feature.x / 32) | 0) + ":" + ((feature.y / 32) | 0);
      if (!buckets.has(key)) buckets.set(key, []);
      buckets.get(key).push(feature);
    }
    return { list, buckets, maxReach };
  }
  // How far a feature's drawing reaches from its anchor, by type and size.
  const REACH = { pyramid: s => 30 * s, spire: s => 9 * s + 2, statue: s => 5 * s + 2, ribcage: s => 5 * s, wreck: s => 5 * s, smoke: s => 12 * s, bridge: () => 9 };
  function featuresIn(world, x0, y0, x1, y1) {
    const result = [], b = world.features.buckets, margin = world.features.maxReach;
    for (let by = Math.floor((y0 - margin) / 32); by <= Math.floor((y1 + margin) / 32); by++)
      for (let bx = Math.floor((x0 - margin) / 32); bx <= Math.floor((x1 + margin) / 32); bx++) {
        const bucket = b.get(bx + ":" + by);
        if (bucket) for (const f of bucket) if (f.x > x0 - f.reach && f.x < x1 + f.reach && f.y > y0 - f.reach && f.y < y1 + f.reach) result.push(f);
      }
    return result.sort((a, b) => (a.layer || 0) - (b.layer || 0) || a.y - b.y);
  }

  /* ---------------- Painting ---------------- */
  const microNoise = makeNoise(5531);
  // The pixel a spec's paint(px, world) hook may recolour; get(channel) samples the field grid.
  const PX = { x: 0, y: 0, r: 0, g: 0, b: 0, i: 0, w00: 0, w10: 0, w01: 0, w11: 0, level: 1, light: 0, glowK: 1, grid: null,
    get(channel) { const a = this.grid[channel], i = this.i; return a[i] * this.w00 + a[i + 1] * this.w10 + a[i + GW] * this.w01 + a[i + GW + 1] * this.w11; },
    tint(color, amount) { if (amount <= 0) return; const t = amount > 1 ? 1 : amount; this.r += (color[0] - this.r) * t; this.g += (color[1] - this.g) * t; this.b += (color[2] - this.b) * t; },
    scale(k) { this.r *= k; this.g *= k; this.b *= k; } };
  // Worley cells: distance to the nearest and second-nearest jittered point (shared result).
  const CELLS = { d1: 0, d2: 0, id: 0, x: 0, y: 0 };
  function cells(x, y, size, seed = 0, jitter = 0.85) {
    const cx = Math.floor(x / size), cy = Math.floor(y / size);
    let d1 = 1e18, d2 = 1e18, id = 0, px = 0, py = 0;
    for (let j = -1; j <= 1; j++) for (let i = -1; i <= 1; i++) {
      const gx = cx + i, gy = cy + j;
      const qx = (gx + 0.5 + (hash2(gx, gy, seed) - 0.5) * jitter) * size, qy = (gy + 0.5 + (hash2(gx, gy, seed + 1) - 0.5) * jitter) * size;
      const d = (x - qx) * (x - qx) + (y - qy) * (y - qy);
      if (d < d1) { d2 = d1; d1 = d; id = hash2(gx, gy, seed + 2); px = qx; py = qy; } else if (d < d2) d2 = d;
    }
    CELLS.d1 = Math.sqrt(d1); CELLS.d2 = Math.sqrt(d2); CELLS.id = id; CELLS.x = px; CELLS.y = py;
    return CELLS;
  }
  /* A closed forest canopy seen from above: inked crowns lit from the north-west, dark gaps
     between them. style: { size, colors: [[r,g,b]...], gap, pine, seed }. */
  function canopy(px, density, style) {
    if (density <= 0.02) return;
    // Each crown is a lumpy disc; overlapping crowns meet along curves (the nearest crown
    // relative to its own radius wins), so the canopy reads as round crowns, not tiles.
    const size = style.size, seed = style.seed || 11, cx = Math.floor(px.x / size), cy = Math.floor(px.y / size);
    let best = 9, second = 9, bid = 0, bx = 0, by = 0, br = 1;
    for (let j = -1; j <= 1; j++) for (let i = -1; i <= 1; i++) {
      const gx = cx + i, gy = cy + j, id = hash2(gx, gy, seed + 2);
      const qx = (gx + 0.5 + (hash2(gx, gy, seed) - 0.5) * 0.9) * size, qy = (gy + 0.5 + (hash2(gx, gy, seed + 1) - 0.5) * 0.9) * size;
      const r = size * (0.55 + id * 0.32), dx = px.x - qx, dy = px.y - qy;
      let t = Math.sqrt(dx * dx + dy * dy) / r;
      if (t < 1.25) {
        const angle = Math.atan2(dy, dx);
        t *= style.pine ? 1 + 0.16 * Math.cos(angle * 7 + id * 6) : 1 + 0.07 * Math.cos(angle * 5 + id * 20) + 0.04 * Math.cos(angle * 9 + id * 31);
      }
      if (t < best) { second = best; best = t; bid = id; bx = qx; by = qy; br = r; } else if (t < second) second = t;
    }
    const color = style.colors[Math.floor(bid * 977) % style.colors.length];
    let k, base = color;
    if (best < 1) {
      const hx = (px.x - bx) / br + 0.38, hy = (px.y - by) / br + 0.42;
      k = 1.12 - Math.min(1.2, Math.hypot(hx, hy)) * 0.4;
      if (best > 0.86) k *= 0.5 + (1 - best) * 2.2;
      else if (second - best < 0.07) k *= 0.55 + (second - best) * 5;
    } else { k = 0.5; base = style.gap || color; }
    const a = density > 1 ? 1 : density;
    px.r += (base[0] * k - px.r) * a; px.g += (base[1] * k - px.g) * a; px.b += (base[2] * k - px.b) * a;
  }

  let grainTexture = null;
  function grain() {
    if (grainTexture) return grainTexture;
    const size = 256, data = new Float32Array(size * size), noise = makeNoise(7741), noise2 = makeNoise(9133);
    for (let y = 0; y < size; y++) for (let x = 0; x < size; x++) {
      // Tileable: blend four offset samples.
      const sample = (u, v) => noise(u * 0.09, v * 0.09) * 0.6 + noise2(u * 0.33, v * 0.33) * 0.4;
      const fx = x / size, fy = y / size;
      const value = sample(x, y) * (1 - fx) * (1 - fy) + sample(x - size, y) * fx * (1 - fy) + sample(x, y - size) * (1 - fx) * fy + sample(x - size, y - size) * fx * fy;
      data[y * size + x] = value;
    }
    grainTexture = { size, data };
    return grainTexture;
  }
  // Paints one tile: (tx, ty) index at a level of `level` pixels per unit.
  function paintTile(world, level, tx, ty) {
    const units = TILE / level, x0 = tx * units, y0 = ty * units;
    const canvas = document.createElement("canvas");
    canvas.width = TILE; canvas.height = TILE;
    const ctx = canvas.getContext("2d");
    const image = ctx.createImageData(TILE, TILE), data = image.data, g = grain(), spec = world.spec;
    PX.grid = world.grid;
    const water = spec.water || { shallow: "#7f9c98", deep: "#3d5a60", foam: "#d9d4b8" };
    const shallow = tone(water.shallow), deep = tone(water.deep), foam = tone(water.foam);
    const contour = spec.contours || null;
    const fields = spec.fields ? fieldStyle(spec.fields) : null;
    const micro = spec.micro ?? 0.5;
    const hatchPeriod = 5, edge = 46;
    let previousRow = new Float32Array(TILE);
    for (let py = 0; py < TILE; py++) {
      const y = y0 + (py + 0.5) / level;
      const currentRow = new Float32Array(TILE);
      for (let px = 0; px < TILE; px++) {
        const x = x0 + (px + 0.5) / level;
        const o = (py * TILE + px) * 4;
        // On the continent (port task 8) the ground past a region's edge is the neighbours' and the strips': see-through.
        if (x < 0 || y < 0 || x > W || y > H) { data[o] = 20; data[o + 1] = 14; data[o + 2] = 10; data[o + 3] = CONTINENT ? 0 : 255; continue; }
        const gx = Math.min(GW - 1.001, x / CELL), gy = Math.min(GH - 1.001, y / CELL);
        const ix = gx | 0, iy = gy | 0, fx = gx - ix, fy = gy - iy, i = iy * GW + ix;
        const w00 = (1 - fx) * (1 - fy), w10 = fx * (1 - fy), w01 = (1 - fx) * fy, w11 = fx * fy;
        const S = arr => arr[i] * w00 + arr[i + 1] * w10 + arr[i + GW] * w01 + arr[i + GW + 1] * w11;
        let r = S(world.grid.r), gg = S(world.grid.g), b = S(world.grid.b);
        const wv = S(world.grid.water), h = S(world.grid.h), sh = S(world.shade);
        if (fields && wv < 0.45) {
          const farm = S(world.grid.farm);
          if (farm > 0.04) { const crop = cropAt(world, fields, x, y, level); if (crop) { r += (crop[0] - r) * crop[3]; gg += (crop[1] - gg) * crop[3]; b += (crop[2] - b) * crop[3]; } }
        }
        currentRow[px] = h;
        const tex = g.data[((Math.floor(y * 2.1) & 255) * 256) + (Math.floor(x * 2.1) & 255)];
        const fine = g.data[((py & 255) * 256) + (px & 255)];
        if (wv > 0.5) {
          const depth = Math.min(1, (wv - 0.5) * 2.4);
          r = shallow[0] + (deep[0] - shallow[0]) * depth; gg = shallow[1] + (deep[1] - shallow[1]) * depth; b = shallow[2] + (deep[2] - shallow[2]) * depth;
          // Short wave strokes, sparse further out.
          const wave = Math.sin((x * 0.9 + Math.sin(y * 0.21) * 3) * 0.7) * 0.5 + 0.5;
          if (((py + (Math.floor(x * 0.11) & 3)) % 7 === 0) && wave > 0.72 && tex > -0.05) { r *= 0.78; gg *= 0.8; b *= 0.82; }
          if (wv < 0.58) { const t = (0.58 - wv) / 0.08; r += (foam[0] - r) * t * 0.55; gg += (foam[1] - gg) * t * 0.55; b += (foam[2] - b) * t * 0.55; }
          if (wv < 0.52) { r *= 0.45; gg *= 0.42; b *= 0.4; }
        } else {
          if (spec.paint) {
            PX.x = x; PX.y = y; PX.r = r; PX.g = gg; PX.b = b; PX.i = i; PX.w00 = w00; PX.w10 = w10; PX.w01 = w01; PX.w11 = w11; PX.level = level; PX.light = 0; PX.glowK = 1;
            spec.paint(PX, world);
            r = PX.r; gg = PX.g; b = PX.b;
          }
          // Relief: hillshade, then engraved hatching on the shadowed slopes.
          let light = (sh - 0.5) * 1.15 + (spec.paint ? PX.light : 0);
          // Fine relief below the field grid's resolution: rougher on rock, gentle on grass.
          if (level >= 2) {
            const rough = micro * (0.2 + S(world.grid.rock) * 0.8 + S(world.grid.snow) * 0.1);
            if (rough > 0.02) {
              let m = microNoise(x * 0.55, y * 0.55) - microNoise(x * 0.55 + 0.2, y * 0.55 + 0.2);
              if (level >= 4) m += (microNoise(x * 1.6 + 40, y * 1.6) - microNoise(x * 1.6 + 40.35, y * 1.6 + 0.35)) * 0.6;
              light += m * rough * 0.55;
            }
          }
          const k = 1 + light;
          r *= k; gg *= k; b *= k * 0.98;
          if (sh < 0.44) {
            const strength = Math.min(1, (0.44 - sh) * 4);
            const line = ((px + py + tx + ty) % hatchPeriod) === 0;
            if (line && fine > 0.25 - strength) { const d = 1 - 0.32 * strength; r *= d; gg *= d; b *= d; }
          }
          if (wv > 0.44) { r *= 0.55; gg *= 0.5; b *= 0.46; }
          // Contours from the height field.
          if (contour && px > 0 && py > 0) {
            const step = contour.step, band = Math.floor(h / step);
            if (band !== Math.floor(currentRow[px - 1] / step) || band !== Math.floor(previousRow[px] / step)) {
              const major = band % (contour.major || 5) === 0;
              const d = major ? 0.72 : 0.86;
              r *= d; gg *= d; b *= d;
            }
          }
          const glow = S(world.grid.glow) * (spec.paint ? PX.glowK : 1);
          if (glow > 0.05) { r += (255 - r) * glow * 0.85; gg += (120 - gg) * glow * 0.75; b += (40 - b) * glow * 0.6; }
        }
        // Paper grain and the burned edge of the sheet.
        const grainK = 1 + tex * 0.07 + fine * 0.035;
        r *= grainK; gg *= grainK; b *= grainK;
        const border = Math.min(x, y, W - x, H - y);
        // A single region's sheet burns at its edge; on the continent the edge fades into the ground beyond instead.
        let alpha = 255;
        if (CONTINENT) { if (border < FEATHER) alpha = 255 * (border / FEATHER) * (border / FEATHER) * (3 - 2 * border / FEATHER); }
        else if (border < edge) { const t = 1 - border / edge, d = 1 - t * t * 0.55; r = r * d + 34 * (1 - d) * 0.4; gg = gg * d + 24 * (1 - d) * 0.4; b = b * d + 14 * (1 - d) * 0.4; }
        data[o] = r < 0 ? 0 : r > 255 ? 255 : r; data[o + 1] = gg < 0 ? 0 : gg > 255 ? 255 : gg; data[o + 2] = b < 0 ? 0 : b > 255 ? 255 : b; data[o + 3] = alpha;
      }
      previousRow = currentRow;
    }
    ctx.putImageData(image, 0, 0);
    // Vectors in map units.
    ctx.setTransform(level, 0, 0, level, -x0 * level, -y0 * level);
    ctx.save(); ctx.beginPath(); ctx.rect(0, 0, W, H); ctx.clip();
    const px1 = 1 / level;
    const view = { x0, y0, x1: x0 + units, y1: y0 + units, level, px: px1 };
    if (spec.under) spec.under(ctx, world, view);
    drawRiver(ctx, world, view);
    drawRoads(ctx, world, view);
    for (const plan of world.settlements) if (plan.place.x + 60 > view.x0 && plan.place.x - 60 < view.x1 && plan.place.y + 60 > view.y0 && plan.place.y - 60 < view.y1) drawSettlement(ctx, plan, view, world);
    const keep = KEEP[level];
    for (const f of featuresIn(world, view.x0, view.y0, view.x1, view.y1)) {
      if (f.rank > keep && !f.always) continue;
      if (f.minLevel && level < f.minLevel) continue;
      (DRAW[f.type] || spec.draw?.[f.type])?.(ctx, f, view, world);
    }
    if (spec.over) spec.over(ctx, world, view);
    ctx.restore();
    return canvas;
  }

  /* ---------------- Field patchwork ----------------
     Farmland is painted per pixel: the land is cut into irregular blocks (nearest of jittered
     points), each ploughed in its own direction into strips of fields. A field belongs to the
     farmland when the farm channel at its centre beats its own threshold, so the farmland ends
     at field edges; hedges and lanes line the borders, furrows show on close view. */
  // Ground colours are painted as given (like the field colours of map-regions.js).
  const rgbOf = value => { const v = value.replace("#", ""); return [0, 2, 4].map(i => parseInt(v.slice(i, i + 2), 16)); };
  const fieldStyles = new Map();
  function fieldStyle(style) {
    if (!fieldStyles.has(style)) fieldStyles.set(style, { ...style, block: style.block || 70, size: style.size || [7, 5], colors: (style.colors || ["#c4b06a", "#a3ad66", "#b7b87c", "#9c8658", "#b0a272"]).map(rgbOf), hedge: rgbOf(style.hedge || "#5f7448") });
    return fieldStyles.get(style);
  }
  const blockCache = new Map();
  function cropAt(world, F, x, y, level) {
    // Nearest jittered block centre.
    const bs = F.block, cx = Math.floor(x / bs), cy = Math.floor(y / bs);
    let best = Infinity, second = Infinity, bx = 0, by = 0, key = 0;
    for (let j = -1; j <= 1; j++) for (let i = -1; i <= 1; i++) {
      const gx = cx + i, gy = cy + j, px = (gx + 0.15 + hash2(gx, gy, 41) * 0.7) * bs, py = (gy + 0.15 + hash2(gx, gy, 43) * 0.7) * bs;
      const d = (x - px) * (x - px) + (y - py) * (y - py);
      if (d < best) { second = best; best = d; bx = px; by = py; key = gx * 7919 + gy; } else if (d < second) second = d;
    }
    // Lanes between blocks.
    const gap = Math.sqrt(second) - Math.sqrt(best);
    let block = blockCache.get(key);
    if (!block) {
      const a = (hash2(key, 3, 47) - 0.5) * 1.4 + (F.angle || 0);
      block = { cos: Math.cos(a), sin: Math.sin(a), fw: F.size[0] * (0.75 + hash2(key, 5, 53) * 0.6), fd: F.size[1] * (0.75 + hash2(key, 7, 59) * 0.6) };
      blockCache.set(key, block);
      if (blockCache.size > 20000) blockCache.clear();
    }
    const dx = x - bx, dy = y - by, u = dx * block.cos + dy * block.sin, v = -dx * block.sin + dy * block.cos;
    const iu = Math.floor(u / block.fw), shift = hash2(iu, key, 61) * block.fd, iv = Math.floor((v + shift) / block.fd);
    const cu = (iu + 0.5) * block.fw, cv = (iv + 0.5) * block.fd - shift;
    const fx = bx + cu * block.cos - cv * block.sin, fy = by + cu * block.sin + cv * block.cos;
    const farm = world.sample("farm", fx, fy), roll = hash2(iu * 31 + iv, key, 67);
    if (farm < 0.3 + roll * 0.45 || world.sample("water", fx, fy) > 0.35) return null;
    const color = F.colors[Math.floor(hash2(iv, iu + key, 71) * F.colors.length)];
    let k = 1;
    const eu = Math.min(u / block.fw - iu, iu + 1 - u / block.fw) * block.fw, ev = Math.min((v + shift) / block.fd - iv, iv + 1 - (v + shift) / block.fd) * block.fd;
    const edge = Math.min(eu, ev), line = 0.18 + 0.6 / level;
    if (gap < 1.3) return [F.hedge[0], F.hedge[1], F.hedge[2], 0.7];
    k = 0.92 + hash2(iu + 5, iv + key, 79) * 0.16;
    if (edge < line) k *= 0.8;
    else if (level >= 2 && hash2(iu, iv + key, 73) < 0.55 && ((Math.floor(v * level / 1.2)) & 1)) k *= 0.94;
    return [color[0] * k, color[1] * k, color[2] * k, F.alpha || 0.7];
  }

  /* ---------------- Vector layers ---------------- */
  function strokePath(ctx, path) {
    ctx.beginPath();
    path.forEach((p, i) => i ? ctx.lineTo(p.x, p.y) : ctx.moveTo(p.x, p.y));
  }
  function drawRiver(ctx, world, view) {
    if (!world.river) return;
    const style = world.spec.river || {}, width = style.width || 7;
    const water = tone(style.color || "#6f9eaa"), light = tone(style.light || "#c8dccf");
    ctx.lineCap = "round"; ctx.lineJoin = "round";
    strokePath(ctx, world.river.path);
    ctx.strokeStyle = "rgba(60,80,50,.35)"; ctx.lineWidth = width * 2.4; ctx.stroke();
    ctx.strokeStyle = INK; ctx.lineWidth = width + 2.2 * view.px * 2; ctx.stroke();
    ctx.strokeStyle = `rgb(${water})`; ctx.lineWidth = width; ctx.stroke();
    ctx.strokeStyle = `rgba(${light},.55)`; ctx.lineWidth = width * 0.28; ctx.setLineDash([width * 1.4, width * 1.8]); ctx.stroke(); ctx.setLineDash([]);
  }
  function drawRoads(ctx, world, view) {
    const style = world.spec.road || {}, width = style.width || 2.6;
    const casing = tone(style.casing || "#5c4a32"), fill = tone(style.fill || "#d8c79a");
    ctx.lineCap = "round"; ctx.lineJoin = "round";
    for (const road of world.roads) { strokePath(ctx, road.path); ctx.strokeStyle = `rgba(${casing},.9)`; ctx.lineWidth = width + 1.6 * Math.max(view.px * 2, 0.5); ctx.stroke(); }
    for (const road of world.roads) { strokePath(ctx, road.path); ctx.strokeStyle = `rgb(${fill})`; ctx.lineWidth = width; ctx.stroke(); }
    if (view.level >= 4) for (const road of world.roads) {
      // Wheel ruts on close view.
      strokePath(ctx, road.path); ctx.strokeStyle = `rgba(${casing},.45)`; ctx.lineWidth = view.px * 0.9; ctx.setLineDash([2.2, 1.6]); ctx.stroke(); ctx.setLineDash([]);
    }
  }
  function drawSettlement(ctx, plan, view, world) {
    const { place } = plan, wallC = tone("#8a8170"), roofs = ["#6e3b28", "#4a3a30", "#5a4636"].map(tone);
    ctx.lineJoin = "round";
    if (!world.spec.fields) for (const field of plan.fields) {
      ctx.save(); ctx.translate(field.x, field.y); ctx.rotate(field.angle);
      const c = tone(field.tone < 0.5 ? "#b8a866" : "#9aa66a");
      ctx.fillStyle = `rgb(${c})`; ctx.strokeStyle = `rgba(26,18,12,.55)`; ctx.lineWidth = view.px * 1.2;
      ctx.fillRect(-field.w / 2, -field.h / 2, field.w, field.h); ctx.strokeRect(-field.w / 2, -field.h / 2, field.w, field.h);
      if (view.level >= 2) { ctx.beginPath(); for (let k = -field.w / 2 + 0.9; k < field.w / 2; k += 0.9) { ctx.moveTo(k, -field.h / 2); ctx.lineTo(k, field.h / 2); } ctx.strokeStyle = `rgba(80,64,36,.45)`; ctx.lineWidth = view.px * 0.8; ctx.stroke(); }
      ctx.restore();
    }
    // Town ground.
    ctx.beginPath(); ctx.ellipse(place.x, place.y, plan.radius, plan.radius * 0.82, 0, 0, Math.PI * 2);
    ctx.fillStyle = `rgba(${tone("#a89a7c")},.85)`; ctx.fill();
    if (plan.castle) {
      // Streets from the gates to the keep.
      ctx.strokeStyle = `rgba(${tone("#d6c8a0")},.9)`; ctx.lineWidth = 1.4;
      ctx.beginPath(); for (let k = 0; k < 4; k++) { const a = k * Math.PI / 2 + 0.3; ctx.moveTo(place.x, place.y); ctx.lineTo(place.x + Math.cos(a) * plan.radius, place.y + Math.sin(a) * plan.radius * 0.82); } ctx.stroke();
    }
    for (const house of plan.houses) {
      ctx.save(); ctx.translate(house.x, house.y); ctx.rotate(house.angle);
      ctx.fillStyle = `rgb(${roofs[Math.floor(house.w * 7) % 3]})`; ctx.strokeStyle = INK; ctx.lineWidth = Math.max(view.px * 1.1, 0.12);
      ctx.fillRect(-house.w / 2, -house.d / 2, house.w, house.d); ctx.strokeRect(-house.w / 2, -house.d / 2, house.w, house.d);
      ctx.fillStyle = "rgba(26,18,12,.35)"; ctx.fillRect(-house.w / 2, 0, house.w, house.d / 2);
      ctx.beginPath(); ctx.moveTo(-house.w / 2, 0); ctx.lineTo(house.w / 2, 0); ctx.stroke();
      ctx.restore();
    }
    if (plan.castle) {
      // Curtain wall, towers and the keep.
      ctx.beginPath(); ctx.ellipse(place.x, place.y, plan.radius, plan.radius * 0.82, 0, 0, Math.PI * 2);
      ctx.strokeStyle = INK; ctx.lineWidth = 2.2; ctx.stroke(); ctx.strokeStyle = `rgb(${wallC})`; ctx.lineWidth = 1.4; ctx.stroke();
      for (let k = 0; k < plan.towers; k++) {
        const a = k / plan.towers * Math.PI * 2, tx = place.x + Math.cos(a) * plan.radius, ty = place.y + Math.sin(a) * plan.radius * 0.82;
        ctx.beginPath(); ctx.arc(tx, ty, 1.7, 0, Math.PI * 2); ctx.fillStyle = `rgb(${wallC})`; ctx.fill(); ctx.strokeStyle = INK; ctx.lineWidth = 0.5; ctx.stroke();
      }
      ctx.fillStyle = `rgb(${tone("#958a75")})`; ctx.strokeStyle = INK; ctx.lineWidth = 0.6;
      ctx.fillRect(place.x - 4, place.y - 4, 8, 7); ctx.strokeRect(place.x - 4, place.y - 4, 8, 7);
      ctx.fillStyle = "rgba(26,18,12,.4)"; ctx.fillRect(place.x - 4, place.y, 8, 3);
      ctx.fillStyle = `rgb(${tone("#6e2a24")})`; ctx.beginPath(); ctx.moveTo(place.x, place.y - 4); ctx.lineTo(place.x, place.y - 9); ctx.lineTo(place.x + 3, place.y - 8); ctx.lineTo(place.x, place.y - 7); ctx.fill();
    } else {
      // A church or hall at the square.
      ctx.fillStyle = `rgb(${tone("#958a75")})`; ctx.strokeStyle = INK; ctx.lineWidth = 0.4;
      ctx.fillRect(place.x - 2, place.y - 3, 4, 5); ctx.strokeRect(place.x - 2, place.y - 3, 4, 5);
      ctx.fillStyle = `rgb(${tone("#3b2a24")})`; ctx.beginPath(); ctx.moveTo(place.x - 1.2, place.y - 3); ctx.lineTo(place.x, place.y - 6.5); ctx.lineTo(place.x + 1.2, place.y - 3); ctx.fill();
    }
  }

  /* ---------------- Feature drawings (map units) ---------------- */
  const C = hexes => hexes.map(tone);
  const PALETTES = {
    broad: C(["#5f7a48", "#6c8550", "#566f42"]), pine: C(["#3f5a44", "#46624a", "#36503e"]), dusk: C(["#2a4640", "#30504a", "#25403b"]),
    jungle: C(["#3f6a3c", "#4a7a42", "#355c34"]), palm: C(["#6f8a4a"]), dead: C(["#5a4a3c"]), ember: C(["#4a3a30"]), birch: C(["#8a9a5a"]),
    elder: C(["#456a42", "#3e6040", "#4e7046"]), frost: C(["#58706c", "#647c78", "#4e6662"]), scrub: C(["#6a6a40", "#7a7044", "#5e6038"]),
  };
  function tree(ctx, f, view) {
    const s = f.s * (view.level <= 1 ? 1.35 : view.level === 2 ? 1.15 : 1), x = f.x, y = f.y, lw = Math.max(view.px * 1.1, 0.14);
    const pal = PALETTES[f.kind] || PALETTES.broad, c = pal[Math.floor(f.v * pal.length) % pal.length];
    ctx.lineWidth = lw; ctx.strokeStyle = INK; ctx.lineJoin = "round";
    // Shadow.
    ctx.fillStyle = "rgba(26,18,12,.28)"; ctx.beginPath(); ctx.ellipse(x + 0.8 * s, y + 0.3 * s, 1.9 * s, 0.7 * s, 0, 0, Math.PI * 2); ctx.fill();
    if (f.kind === "pine" || f.kind === "dusk" || f.kind === "frost") {
      ctx.beginPath(); ctx.moveTo(x, y - 5.2 * s); ctx.lineTo(x - 1.9 * s, y - 0.2 * s); ctx.lineTo(x + 1.9 * s, y - 0.2 * s); ctx.closePath();
      ctx.fillStyle = `rgb(${c})`; ctx.fill(); ctx.stroke();
      ctx.beginPath(); ctx.moveTo(x, y - 5.2 * s); ctx.lineTo(x - 1.9 * s, y - 0.2 * s); ctx.lineTo(x, y - 0.2 * s); ctx.closePath(); ctx.fillStyle = "rgba(26,18,12,.32)"; ctx.fill();
      if (view.level >= 4) { ctx.beginPath(); ctx.moveTo(x - 0.9 * s, y - 2.4 * s); ctx.lineTo(x + 0.9 * s, y - 2.4 * s); ctx.moveTo(x - 1.4 * s, y - 1.2 * s); ctx.lineTo(x + 1.4 * s, y - 1.2 * s); ctx.strokeStyle = "rgba(26,18,12,.5)"; ctx.lineWidth = lw * 0.7; ctx.stroke(); }
      if (f.kind === "frost") { ctx.beginPath(); ctx.moveTo(x, y - 5.2 * s); ctx.lineTo(x - 0.8 * s, y - 3.1 * s); ctx.lineTo(x + 0.8 * s, y - 3.1 * s); ctx.closePath(); ctx.fillStyle = "rgba(236,242,240,.85)"; ctx.fill(); }
      return;
    }
    if (f.kind === "palm") {
      ctx.beginPath(); ctx.moveTo(x, y); ctx.quadraticCurveTo(x + 0.6 * s, y - 2.5 * s, x + 0.3 * s, y - 4.6 * s); ctx.strokeStyle = `rgb(${tone("#6a5238")})`; ctx.lineWidth = 0.45 * s; ctx.stroke();
      ctx.fillStyle = `rgb(${c})`; ctx.strokeStyle = INK; ctx.lineWidth = lw;
      for (let k = 0; k < 5; k++) { const a = -Math.PI / 2 + (k - 2) * 0.62; ctx.beginPath(); ctx.ellipse(x + 0.3 * s + Math.cos(a) * 1.3 * s, y - 4.6 * s + Math.sin(a) * 0.9 * s, 1.5 * s, 0.42 * s, a, 0, Math.PI * 2); ctx.fill(); ctx.stroke(); }
      return;
    }
    if (f.kind === "dead") {
      ctx.strokeStyle = `rgb(${c})`; ctx.lineWidth = 0.32 * s; ctx.lineCap = "round";
      ctx.beginPath(); ctx.moveTo(x, y); ctx.lineTo(x, y - 3.8 * s); ctx.moveTo(x, y - 2.2 * s); ctx.lineTo(x - 1.3 * s, y - 3.6 * s); ctx.moveTo(x, y - 2.8 * s); ctx.lineTo(x + 1.2 * s, y - 4 * s); ctx.moveTo(x - 0.6 * s, y - 2.9 * s); ctx.lineTo(x - 0.4 * s, y - 4.1 * s); ctx.stroke();
      return;
    }
    // Broadleaf, jungle, ember scrub: a lumpy crown on a short trunk.
    const big = f.kind === "jungle" ? 1.3 : f.kind === "elder" ? 1.5 : f.kind === "ember" || f.kind === "scrub" ? 0.65 : 1;
    ctx.beginPath(); ctx.moveTo(x, y); ctx.lineTo(x, y - 1.6 * s); ctx.strokeStyle = `rgb(${tone("#4a3628")})`; ctx.lineWidth = 0.45 * s; ctx.stroke();
    const cy = y - 2.6 * s * big, rr = 1.75 * s * big;
    ctx.beginPath();
    for (let k = 0; k < 7; k++) { const a = k / 7 * Math.PI * 2, wob = 0.82 + hash2(Math.round(f.x * 9) + k, Math.round(f.y * 9), 3) * 0.3; ctx.lineTo(x + Math.cos(a) * rr * wob, cy + Math.sin(a) * rr * wob * 0.9); }
    ctx.closePath(); ctx.fillStyle = `rgb(${c})`; ctx.fill(); ctx.strokeStyle = INK; ctx.lineWidth = lw; ctx.stroke();
    ctx.beginPath(); ctx.arc(x + rr * 0.25, cy + rr * 0.2, rr * 0.75, 0.2, Math.PI * 1.1); ctx.fillStyle = "rgba(26,18,12,.25)"; ctx.fill();
    if (view.level >= 4) { ctx.beginPath(); ctx.arc(x - rr * 0.3, cy - rr * 0.35, rr * 0.32, 0, Math.PI * 2); ctx.fillStyle = "rgba(240,230,190,.18)"; ctx.fill(); }
  }
  const DRAW = {
    tree,
    rock(ctx, f, view) {
      const s = f.s || 1, c = tone(f.color || "#8a8578"), lw = Math.max(view.px, 0.12);
      ctx.beginPath(); ctx.moveTo(f.x - 1.4 * s, f.y); ctx.lineTo(f.x - 0.9 * s, f.y - 1.3 * s); ctx.lineTo(f.x + 0.2 * s, f.y - 1.7 * s); ctx.lineTo(f.x + 1.3 * s, f.y - 0.8 * s); ctx.lineTo(f.x + 1.5 * s, f.y); ctx.closePath();
      ctx.fillStyle = `rgb(${c})`; ctx.fill(); ctx.strokeStyle = INK; ctx.lineWidth = lw; ctx.stroke();
      ctx.beginPath(); ctx.moveTo(f.x + 0.2 * s, f.y - 1.7 * s); ctx.lineTo(f.x + 1.3 * s, f.y - 0.8 * s); ctx.lineTo(f.x + 1.5 * s, f.y); ctx.lineTo(f.x + 0.1 * s, f.y); ctx.closePath(); ctx.fillStyle = "rgba(26,18,12,.35)"; ctx.fill();
    },
    tuft(ctx, f, view) {
      const c = tone(f.color || "#6f7a4a"); ctx.strokeStyle = `rgba(${c},.9)`; ctx.lineWidth = Math.max(view.px * 0.9, 0.1);
      ctx.beginPath(); ctx.moveTo(f.x, f.y); ctx.lineTo(f.x - 0.5, f.y - 1.1); ctx.moveTo(f.x, f.y); ctx.lineTo(f.x + 0.1, f.y - 1.4); ctx.moveTo(f.x, f.y); ctx.lineTo(f.x + 0.6, f.y - 1); ctx.stroke();
    },
    flowers(ctx, f, view) {
      const c = tone(f.color || "#c9a24a"); ctx.fillStyle = `rgb(${c})`;
      for (let k = 0; k < 4; k++) { ctx.beginPath(); ctx.arc(f.x + (hash2(k, f.x * 7 | 0, 2) - 0.5) * 2.2, f.y + (hash2(k, f.y * 7 | 0, 3) - 0.5) * 1.5, 0.22, 0, Math.PI * 2); ctx.fill(); }
    },
    reeds(ctx, f, view) {
      const c = tone(f.color || "#7a7a4a"); ctx.strokeStyle = `rgb(${c})`; ctx.lineWidth = Math.max(view.px * 0.9, 0.12);
      ctx.beginPath(); for (let k = -2; k <= 2; k++) { ctx.moveTo(f.x + k * 0.35, f.y); ctx.lineTo(f.x + k * 0.55, f.y - 1.8 - (k & 1) * 0.5); } ctx.stroke();
      if (view.level >= 4) { ctx.fillStyle = `rgb(${tone("#5a4030")})`; ctx.fillRect(f.x - 0.1, f.y - 2.4, 0.25, 0.7); }
    },
    pool(ctx, f, view) {
      const c = tone(f.color || "#3d5450");
      ctx.beginPath(); ctx.ellipse(f.x, f.y, (f.s || 1) * 2.4, (f.s || 1) * 1.2, f.v * 0.6, 0, Math.PI * 2);
      ctx.fillStyle = `rgb(${c})`; ctx.fill(); ctx.strokeStyle = INK; ctx.lineWidth = Math.max(view.px, 0.12); ctx.stroke();
      ctx.beginPath(); ctx.moveTo(f.x - 1, f.y - 0.2); ctx.lineTo(f.x + 0.6, f.y - 0.2); ctx.strokeStyle = "rgba(220,230,210,.35)"; ctx.stroke();
    },
    dune(ctx, f, view) {
      const c = tone(f.color || "#8a6a3a"), len = (f.s || 1) * 7;
      ctx.beginPath(); ctx.moveTo(f.x - len, f.y + len * 0.15); ctx.quadraticCurveTo(f.x, f.y - len * 0.35, f.x + len, f.y + len * 0.1);
      ctx.strokeStyle = `rgba(${c},.75)`; ctx.lineWidth = Math.max(view.px * 1.3, 0.2); ctx.stroke();
      ctx.lineTo(f.x + len * 0.6, f.y + len * 0.22); ctx.quadraticCurveTo(f.x - len * 0.1, f.y + len * 0.05, f.x - len, f.y + len * 0.15);
      ctx.fillStyle = "rgba(26,18,12,.12)"; ctx.fill();
    },
    cliff(ctx, f, view) {
      // Hachured cliff edge: a ragged line with short downward strokes.
      const len = (f.s || 1) * 6, a = f.angle || 0, dx = Math.cos(a), dy = Math.sin(a);
      ctx.strokeStyle = INK; ctx.lineWidth = Math.max(view.px * 1.3, 0.16);
      ctx.beginPath(); ctx.moveTo(f.x - dx * len, f.y - dy * len); ctx.lineTo(f.x + dx * len, f.y + dy * len); ctx.stroke();
      ctx.beginPath(); for (let k = -len; k <= len; k += 0.9) { const bx = f.x + dx * k, by = f.y + dy * k; ctx.moveTo(bx, by); ctx.lineTo(bx - dy * 1.6, by + dx * 1.6 + 0.6); }
      ctx.lineWidth = Math.max(view.px * 0.8, 0.1); ctx.stroke();
    },
    bridge(ctx, f, view) {
      ctx.save(); ctx.translate(f.x, f.y); ctx.rotate(f.angle);
      ctx.fillStyle = `rgb(${tone("#8a7458")})`; ctx.strokeStyle = INK; ctx.lineWidth = Math.max(view.px * 1.2, 0.15);
      ctx.fillRect(-7, -2.2, 14, 4.4); ctx.strokeRect(-7, -2.2, 14, 4.4);
      ctx.beginPath(); for (let k = -6; k <= 6; k += 1.5) { ctx.moveTo(k, -2.2); ctx.lineTo(k, 2.2); } ctx.lineWidth = Math.max(view.px * 0.7, 0.08); ctx.stroke();
      ctx.restore();
    },
    house(ctx, f, view) {
      const s = f.s || 1, roof = tone(f.color || "#6e3b28");
      ctx.fillStyle = `rgb(${roof})`; ctx.strokeStyle = INK; ctx.lineWidth = Math.max(view.px * 1.1, 0.12);
      ctx.fillRect(f.x - 1.2 * s, f.y - 0.9 * s, 2.4 * s, 1.8 * s); ctx.strokeRect(f.x - 1.2 * s, f.y - 0.9 * s, 2.4 * s, 1.8 * s);
      ctx.fillStyle = "rgba(26,18,12,.35)"; ctx.fillRect(f.x - 1.2 * s, f.y, 2.4 * s, 0.9 * s);
    },
    farmfield(ctx, f, view) {
      ctx.save(); ctx.translate(f.x, f.y); ctx.rotate(f.angle || 0);
      const c = tone(f.color || "#b6a866"), w = f.w || 8, h = f.h || 5;
      ctx.fillStyle = `rgba(${c},.85)`; ctx.fillRect(-w / 2, -h / 2, w, h);
      ctx.strokeStyle = "rgba(26,18,12,.45)"; ctx.lineWidth = Math.max(view.px, 0.1); ctx.strokeRect(-w / 2, -h / 2, w, h);
      if (view.level >= 2) { ctx.beginPath(); for (let k = -w / 2 + 0.8; k < w / 2; k += 0.8) { ctx.moveTo(k, -h / 2); ctx.lineTo(k, h / 2); } ctx.strokeStyle = "rgba(90,70,36,.35)"; ctx.lineWidth = Math.max(view.px * 0.7, 0.07); ctx.stroke(); }
      ctx.restore();
    },
    hedge(ctx, f, view) {
      ctx.save(); ctx.translate(f.x, f.y); ctx.rotate(f.angle || 0);
      ctx.strokeStyle = `rgba(${tone(f.color || "#4f6440")},.9)`; ctx.lineWidth = 0.8; ctx.lineCap = "round"; ctx.setLineDash([0.9, 0.5]);
      ctx.beginPath(); ctx.moveTo(-(f.len || 8) / 2, 0); ctx.lineTo((f.len || 8) / 2, 0); ctx.stroke(); ctx.setLineDash([]); ctx.restore();
    },
    ruin(ctx, f, view) {
      const s = f.s || 1, c = tone(f.color || "#8a8270"); ctx.fillStyle = `rgb(${c})`; ctx.strokeStyle = INK; ctx.lineWidth = Math.max(view.px * 1.1, 0.12);
      const blocks = [[-3, -1, 1.2, 2.6], [-1.2, -0.2, 2.4, 1], [1.6, -1.8, 1.1, 3.4], [-3, 1.4, 5.8, 0.8]];
      for (const [bx, by, bw, bh] of blocks) { ctx.fillRect(f.x + bx * s, f.y + by * s, bw * s, bh * s); ctx.strokeRect(f.x + bx * s, f.y + by * s, bw * s, bh * s); }
    },
    stones(ctx, f, view) {
      ctx.fillStyle = `rgb(${tone("#8a8578")})`; ctx.strokeStyle = INK; ctx.lineWidth = Math.max(view.px, 0.1);
      for (let k = 0; k < 7; k++) { const a = k / 7 * Math.PI * 2; ctx.fillRect(f.x + Math.cos(a) * 3 - 0.35, f.y + Math.sin(a) * 2 - 1.1, 0.7, 1.3); ctx.strokeRect(f.x + Math.cos(a) * 3 - 0.35, f.y + Math.sin(a) * 2 - 1.1, 0.7, 1.3); }
    },
    tower(ctx, f, view) {
      const c = tone(f.color || "#8a8170"); ctx.fillStyle = `rgb(${c})`; ctx.strokeStyle = INK; ctx.lineWidth = Math.max(view.px * 1.1, 0.12);
      ctx.fillRect(f.x - 1.2, f.y - 4.5, 2.4, 4.5); ctx.strokeRect(f.x - 1.2, f.y - 4.5, 2.4, 4.5);
      ctx.beginPath(); ctx.moveTo(f.x - 1.6, f.y - 4.5); ctx.lineTo(f.x, f.y - 6.6); ctx.lineTo(f.x + 1.6, f.y - 4.5); ctx.closePath(); ctx.fillStyle = `rgb(${tone("#3b2a24")})`; ctx.fill(); ctx.stroke();
    },
    windmill(ctx, f, view) {
      DRAW.tower(ctx, { ...f, color: "#a89a7c" }, view);
      ctx.strokeStyle = INK; ctx.lineWidth = Math.max(view.px * 1.2, 0.14); ctx.beginPath();
      for (let k = 0; k < 4; k++) { const a = k * Math.PI / 2 + 0.4; ctx.moveTo(f.x, f.y - 4); ctx.lineTo(f.x + Math.cos(a) * 3.2, f.y - 4 + Math.sin(a) * 3.2); } ctx.stroke();
    },
    camp(ctx, f, view) {
      ctx.fillStyle = `rgb(${tone("#a8946a")})`; ctx.strokeStyle = INK; ctx.lineWidth = Math.max(view.px, 0.1);
      ctx.beginPath(); ctx.moveTo(f.x - 1.6, f.y); ctx.lineTo(f.x, f.y - 2); ctx.lineTo(f.x + 1.6, f.y); ctx.closePath(); ctx.fill(); ctx.stroke();
      ctx.fillStyle = "#e88a3a"; ctx.beginPath(); ctx.arc(f.x + 2.6, f.y - 0.2, 0.45, 0, Math.PI * 2); ctx.fill();
    },
    bones(ctx, f, view) {
      ctx.strokeStyle = `rgb(${tone("#d8ccb0")})`; ctx.lineWidth = Math.max(view.px * 1.3, 0.16); ctx.lineCap = "round";
      ctx.beginPath(); ctx.moveTo(f.x - 1, f.y - 0.3); ctx.lineTo(f.x + 1, f.y + 0.3); ctx.moveTo(f.x - 0.4, f.y + 0.6); ctx.lineTo(f.x + 0.6, f.y - 0.6); ctx.stroke();
    },
    crater(ctx, f, view) {
      const s = f.s || 1; ctx.beginPath(); ctx.ellipse(f.x, f.y, 2.2 * s, 1.3 * s, 0, 0, Math.PI * 2);
      ctx.fillStyle = "rgba(26,18,12,.3)"; ctx.fill(); ctx.strokeStyle = `rgba(${tone("#5a4a3c")},.9)`; ctx.lineWidth = Math.max(view.px * 1.1, 0.12); ctx.stroke();
    },
    spire(ctx, f, view) {
      const s = f.s || 1, c = tone(f.color || "#7a3a32");
      ctx.beginPath(); ctx.moveTo(f.x - 1.8 * s, f.y); ctx.lineTo(f.x - 0.5 * s, f.y - 7 * s); ctx.lineTo(f.x + 0.4 * s, f.y - 8.5 * s); ctx.lineTo(f.x + 1.7 * s, f.y); ctx.closePath();
      ctx.fillStyle = `rgb(${c})`; ctx.fill(); ctx.strokeStyle = INK; ctx.lineWidth = Math.max(view.px * 1.2, 0.14); ctx.stroke();
      ctx.beginPath(); ctx.moveTo(f.x + 0.4 * s, f.y - 8.5 * s); ctx.lineTo(f.x + 1.7 * s, f.y); ctx.lineTo(f.x + 0.2 * s, f.y); ctx.closePath(); ctx.fillStyle = "rgba(26,18,12,.38)"; ctx.fill();
    },
    shard(ctx, f, view) {
      const s = f.s || 1, c = tone(f.color || "#2a2228");
      ctx.beginPath(); ctx.moveTo(f.x - 0.8 * s, f.y); ctx.lineTo(f.x - 0.1 * s, f.y - 3 * s); ctx.lineTo(f.x + 0.9 * s, f.y); ctx.closePath();
      ctx.fillStyle = `rgb(${c})`; ctx.fill(); ctx.strokeStyle = INK; ctx.lineWidth = Math.max(view.px, 0.1); ctx.stroke();
      ctx.beginPath(); ctx.moveTo(f.x - 0.1 * s, f.y - 3 * s); ctx.lineTo(f.x - 0.3 * s, f.y - 0.5 * s); ctx.strokeStyle = `rgba(${tone(f.shine || "#c8c0d0")},.7)`; ctx.stroke();
    },
    crystal(ctx, f, view) { DRAW.shard(ctx, { ...f, color: f.color || "#c8b8a0", shine: "#ffffff" }, view); },
    grave(ctx, f, view) {
      ctx.fillStyle = `rgb(${tone("#8a8578")})`; ctx.strokeStyle = INK; ctx.lineWidth = Math.max(view.px, 0.1);
      ctx.beginPath(); ctx.moveTo(f.x - 0.6, f.y); ctx.lineTo(f.x - 0.6, f.y - 1.2); ctx.arc(f.x, f.y - 1.2, 0.6, Math.PI, 0); ctx.lineTo(f.x + 0.6, f.y); ctx.closePath(); ctx.fill(); ctx.stroke();
    },
    weapon(ctx, f, view) {
      ctx.strokeStyle = `rgb(${tone("#6a5a4a")})`; ctx.lineWidth = Math.max(view.px * 1.1, 0.12);
      ctx.beginPath(); ctx.moveTo(f.x, f.y); ctx.lineTo(f.x + (f.v - 0.5) * 1.2, f.y - 2.4); ctx.moveTo(f.x - 0.5, f.y - 1.6); ctx.lineTo(f.x + 0.5, f.y - 1.6); ctx.stroke();
    },
    banner(ctx, f, view) {
      ctx.strokeStyle = INK; ctx.lineWidth = Math.max(view.px * 1.1, 0.12); ctx.beginPath(); ctx.moveTo(f.x, f.y); ctx.lineTo(f.x, f.y - 4); ctx.stroke();
      ctx.fillStyle = `rgb(${tone(f.color || "#6e2a24")})`; ctx.beginPath(); ctx.moveTo(f.x, f.y - 4); ctx.lineTo(f.x + 2, f.y - 3.4); ctx.lineTo(f.x + 0.9, f.y - 3); ctx.lineTo(f.x + 2, f.y - 2.5); ctx.lineTo(f.x, f.y - 2.2); ctx.fill();
    },
    lava(ctx, f, view) {
      ctx.strokeStyle = "#ff7a2e"; ctx.globalAlpha = 0.9; ctx.lineWidth = (f.s || 1) * 1.2; ctx.lineCap = "round";
      ctx.beginPath(); ctx.moveTo(f.x, f.y); ctx.quadraticCurveTo(f.x + 2, f.y + 3, f.x + (f.v - 0.5) * 6, f.y + 6); ctx.stroke(); ctx.globalAlpha = 1;
    },
    snowdrift(ctx, f, view) {
      ctx.strokeStyle = "rgba(255,255,250,.6)"; ctx.lineWidth = Math.max(view.px * 1.2, 0.15);
      ctx.beginPath(); ctx.moveTo(f.x - 2, f.y); ctx.quadraticCurveTo(f.x, f.y - 0.8, f.x + 2, f.y); ctx.stroke();
    },
    crevasse(ctx, f, view) {
      ctx.strokeStyle = "rgba(40,60,80,.55)"; ctx.lineWidth = Math.max(view.px * 1.1, 0.13);
      ctx.beginPath(); ctx.moveTo(f.x - 2.5, f.y + (f.v - 0.5)); ctx.lineTo(f.x - 0.5, f.y - 0.3); ctx.lineTo(f.x + 2.5, f.y + (0.5 - f.v)); ctx.stroke();
    },
    boat(ctx, f, view) {
      ctx.fillStyle = `rgb(${tone("#5a4030")})`; ctx.strokeStyle = INK; ctx.lineWidth = Math.max(view.px, 0.1);
      ctx.beginPath(); ctx.moveTo(f.x - 2.4, f.y); ctx.lineTo(f.x + 2.4, f.y); ctx.lineTo(f.x + 1.6, f.y + 0.9); ctx.lineTo(f.x - 1.6, f.y + 0.9); ctx.closePath(); ctx.fill(); ctx.stroke();
      ctx.beginPath(); ctx.moveTo(f.x, f.y); ctx.lineTo(f.x, f.y - 3.4); ctx.stroke();
      ctx.fillStyle = `rgb(${tone("#d8ccb0")})`; ctx.beginPath(); ctx.moveTo(f.x, f.y - 3.3); ctx.lineTo(f.x + 1.8, f.y - 0.6); ctx.lineTo(f.x, f.y - 0.6); ctx.fill(); ctx.stroke();
    },
    pier(ctx, f, view) {
      ctx.save(); ctx.translate(f.x, f.y); ctx.rotate(f.angle || 0);
      ctx.fillStyle = `rgb(${tone("#8a7458")})`; ctx.strokeStyle = INK; ctx.lineWidth = Math.max(view.px, 0.1);
      ctx.fillRect(0, -0.7, f.len || 8, 1.4); ctx.strokeRect(0, -0.7, f.len || 8, 1.4); ctx.restore();
    },
    lighthouse(ctx, f, view) {
      ctx.fillStyle = `rgb(${tone("#d8ccb0")})`; ctx.strokeStyle = INK; ctx.lineWidth = Math.max(view.px * 1.1, 0.12);
      ctx.beginPath(); ctx.moveTo(f.x - 1.3, f.y); ctx.lineTo(f.x - 0.8, f.y - 7); ctx.lineTo(f.x + 0.8, f.y - 7); ctx.lineTo(f.x + 1.3, f.y); ctx.closePath(); ctx.fill(); ctx.stroke();
      ctx.fillStyle = "#f2c66a"; ctx.fillRect(f.x - 0.6, f.y - 8.2, 1.2, 1.2);
    },
    obelisk(ctx, f, view) {
      const s = f.s || 1; ctx.fillStyle = `rgb(${tone(f.color || "#b8a478")})`; ctx.strokeStyle = INK; ctx.lineWidth = Math.max(view.px * 1.1, 0.12);
      ctx.beginPath(); ctx.moveTo(f.x - 0.7 * s, f.y); ctx.lineTo(f.x - 0.45 * s, f.y - 5 * s); ctx.lineTo(f.x, f.y - 6 * s); ctx.lineTo(f.x + 0.45 * s, f.y - 5 * s); ctx.lineTo(f.x + 0.7 * s, f.y); ctx.closePath(); ctx.fill(); ctx.stroke();
      ctx.fillStyle = "rgba(26,18,12,.3)"; ctx.fillRect(f.x, f.y - 5 * s, 0.6 * s, 5 * s);
    },
    column(ctx, f, view) {
      const s = f.s || 1, tall = (1.5 + f.v * 2.5) * s; ctx.fillStyle = `rgb(${tone(f.color || "#c4b48c")})`; ctx.strokeStyle = INK; ctx.lineWidth = Math.max(view.px, 0.1);
      ctx.fillRect(f.x - 0.45 * s, f.y - tall, 0.9 * s, tall); ctx.strokeRect(f.x - 0.45 * s, f.y - tall, 0.9 * s, tall);
      ctx.fillRect(f.x - 0.65 * s, f.y - tall - 0.3 * s, 1.3 * s, 0.35 * s); ctx.strokeRect(f.x - 0.65 * s, f.y - tall - 0.3 * s, 1.3 * s, 0.35 * s);
    },
    statue(ctx, f, view) {
      const s = f.s || 1; ctx.fillStyle = `rgb(${tone(f.color || "#a89a7a")})`; ctx.strokeStyle = INK; ctx.lineWidth = Math.max(view.px, 0.1);
      ctx.fillRect(f.x - 1 * s, f.y - 0.8 * s, 2 * s, 0.8 * s); ctx.strokeRect(f.x - 1 * s, f.y - 0.8 * s, 2 * s, 0.8 * s);
      ctx.beginPath(); ctx.moveTo(f.x - 0.5 * s, f.y - 0.8 * s); ctx.lineTo(f.x - 0.6 * s, f.y - 3.2 * s); ctx.arc(f.x, f.y - 3.6 * s, 0.5 * s, Math.PI * 0.8, Math.PI * 2.2); ctx.lineTo(f.x + 0.5 * s, f.y - 0.8 * s); ctx.closePath(); ctx.fill(); ctx.stroke();
    },
    wreck(ctx, f, view) {
      const s = f.s || 1; ctx.save(); ctx.translate(f.x, f.y); ctx.rotate(f.angle || -0.4);
      ctx.fillStyle = `rgb(${tone("#5a4030")})`; ctx.strokeStyle = INK; ctx.lineWidth = Math.max(view.px * 1.1, 0.12);
      ctx.beginPath(); ctx.moveTo(-4 * s, 0); ctx.quadraticCurveTo(0, 2.4 * s, 4 * s, 0); ctx.lineTo(3 * s, -0.6 * s); ctx.lineTo(-3.4 * s, -0.6 * s); ctx.closePath(); ctx.fill(); ctx.stroke();
      ctx.beginPath(); for (let k = -3; k <= 3; k += 1.2) { ctx.moveTo(k * s, -0.6 * s); ctx.lineTo(k * s * 0.9, -1.8 * s); } ctx.moveTo(-0.6 * s, -0.6 * s); ctx.lineTo(0.8 * s, -4.6 * s); ctx.stroke();
      ctx.restore();
    },
    smoke(ctx, f, view) {
      const s = f.s || 1;
      for (let k = 0; k < 4; k++) {
        const gx = f.x + k * 1.6 * s, gy = f.y - 2 * s - k * 2.4 * s, r = (1.2 + k * 0.9) * s;
        const gradient = ctx.createRadialGradient(gx, gy, 0, gx, gy, r);
        gradient.addColorStop(0, `rgba(${f.color || "70,64,60"},.42)`); gradient.addColorStop(1, `rgba(${f.color || "70,64,60"},0)`);
        ctx.fillStyle = gradient; ctx.beginPath(); ctx.arc(gx, gy, r, 0, Math.PI * 2); ctx.fill();
      }
    },
    mound(ctx, f, view) {
      const s = f.s || 1; ctx.beginPath(); ctx.ellipse(f.x, f.y, 3.2 * s, 1.5 * s, f.angle || 0, 0, Math.PI * 2);
      ctx.fillStyle = `rgba(${tone(f.color || "#6a5e48")},.9)`; ctx.fill(); ctx.strokeStyle = "rgba(26,18,12,.6)"; ctx.lineWidth = Math.max(view.px, 0.1); ctx.stroke();
      ctx.beginPath(); ctx.ellipse(f.x + 0.6 * s, f.y + 0.4 * s, 2.4 * s, 0.9 * s, f.angle || 0, 0, Math.PI); ctx.fillStyle = "rgba(26,18,12,.25)"; ctx.fill();
    },
    wisp(ctx, f, view) {
      const gradient = ctx.createRadialGradient(f.x, f.y, 0, f.x, f.y, 1.6);
      gradient.addColorStop(0, f.color || "rgba(200,255,190,.85)"); gradient.addColorStop(1, "rgba(160,230,150,0)");
      ctx.fillStyle = gradient; ctx.beginPath(); ctx.arc(f.x, f.y, 1.6, 0, Math.PI * 2); ctx.fill();
    },
    tent(ctx, f, view) {
      const s = f.s || 1; ctx.fillStyle = `rgb(${tone(f.color || "#c8b48a")})`; ctx.strokeStyle = INK; ctx.lineWidth = Math.max(view.px, 0.1);
      ctx.beginPath(); ctx.moveTo(f.x - 1.8 * s, f.y); ctx.lineTo(f.x - 0.2 * s, f.y - 1.8 * s); ctx.lineTo(f.x + 1.8 * s, f.y - 1.4 * s); ctx.lineTo(f.x + 1.6 * s, f.y); ctx.closePath(); ctx.fill(); ctx.stroke();
      ctx.beginPath(); ctx.moveTo(f.x - 0.2 * s, f.y - 1.8 * s); ctx.lineTo(f.x + 0.1 * s, f.y); ctx.stroke();
    },
    ribcage(ctx, f, view) {
      const s = f.s || 1; ctx.strokeStyle = `rgb(${tone("#d8ccb0")})`; ctx.lineWidth = Math.max(view.px * 1.4, 0.18) * s; ctx.lineCap = "round";
      ctx.beginPath(); ctx.moveTo(f.x - 4 * s, f.y); ctx.lineTo(f.x + 4 * s, f.y - 0.4 * s);
      for (let k = -3; k <= 3; k += 1.1) { ctx.moveTo(f.x + k * s, f.y - 0.2 * s); ctx.quadraticCurveTo(f.x + (k - 0.8) * s, f.y - 2.6 * s, f.x + (k + 0.3) * s, f.y - 3.2 * s); }
      ctx.stroke();
    },
    cart(ctx, f, view) {
      const s = f.s || 1; ctx.strokeStyle = INK; ctx.lineWidth = Math.max(view.px * 1.1, 0.12); ctx.fillStyle = `rgb(${tone("#6a5238")})`;
      ctx.fillRect(f.x - 2 * s, f.y - 1.4 * s, 4 * s, 1 * s); ctx.strokeRect(f.x - 2 * s, f.y - 1.4 * s, 4 * s, 1 * s);
      ctx.beginPath(); ctx.arc(f.x - 1.2 * s, f.y - 0.3 * s, 0.6 * s, 0, Math.PI * 2); ctx.moveTo(f.x + 1.8 * s, f.y - 0.3 * s); ctx.arc(f.x + 1.2 * s, f.y - 0.3 * s, 0.6 * s, 0, Math.PI * 2); ctx.stroke();
      ctx.beginPath(); ctx.moveTo(f.x + 2 * s, f.y - 1.2 * s); ctx.lineTo(f.x + 3.6 * s, f.y - 3.6 * s); ctx.stroke();
    },
    pyramid(ctx, f, view) {
      const s = f.s || 1; ctx.strokeStyle = INK; ctx.lineWidth = Math.max(view.px * 1.4, 0.2);
      ctx.beginPath(); ctx.moveTo(f.x - 26 * s, f.y + 8 * s); ctx.lineTo(f.x, f.y - 22 * s); ctx.lineTo(f.x + 26 * s, f.y + 8 * s); ctx.closePath();
      ctx.fillStyle = `rgb(${tone("#c8a868")})`; ctx.fill(); ctx.stroke();
      ctx.beginPath(); ctx.moveTo(f.x, f.y - 22 * s); ctx.lineTo(f.x + 26 * s, f.y + 8 * s); ctx.lineTo(f.x + 2 * s, f.y + 8 * s); ctx.closePath(); ctx.fillStyle = "rgba(26,18,12,.35)"; ctx.fill();
      ctx.beginPath(); for (let k = 1; k < 9; k++) { const t = k / 9; ctx.moveTo(f.x - 26 * s * t, f.y - 22 * s + 30 * s * t); ctx.lineTo(f.x + 26 * s * t, f.y - 22 * s + 30 * s * t); } ctx.strokeStyle = "rgba(26,18,12,.3)"; ctx.lineWidth = Math.max(view.px * 0.8, 0.1); ctx.stroke();
    },
  };

  // The field function of a region on its own, for coarse pictures (the continent's overview, port task 8).
  function sampler(region) {
    const spec = window.MapRegions?.spec(region.id);
    if (!spec) return null;
    const T = makeToolkit(hashString(region.id) % 100000), context = spec.prepare ? spec.prepare(region, T) : {};
    const out = { color: [0, 0, 0] };
    return {
      spec, T,
      fieldAt(x, y) {
        out.color = [200, 190, 150]; out.h = 0; out.water = 0; out.forest = 0; out.pine = 0; out.rock = 0; out.wet = 0; out.sand = 0; out.snow = 0; out.farm = 0; out.glow = 0;
        spec.field(x, y, out, T, context, region);
        return out;
      },
    };
  }

  /* ---------------- Tile cache ---------------- */
  const worlds = new Map();
  function world(region) {
    if (!worlds.has(region.id)) {
      const spec = window.MapRegions?.spec(region.id);
      if (!spec) return null;
      worlds.set(region.id, createWorld(region, spec));
    }
    return worlds.get(region.id);
  }
  function tile(worldObject, level, tx, ty) {
    const key = level + ":" + tx + ":" + ty;
    const cache = worldObject.tiles;
    if (cache.has(key)) { const canvas = cache.get(key); cache.delete(key); cache.set(key, canvas); return canvas; }
    return null;
  }
  function render(worldObject, level, tx, ty) {
    const key = level + ":" + tx + ":" + ty, cache = worldObject.tiles;
    const canvas = paintTile(worldObject, level, tx, ty);
    cache.set(key, canvas);
    // Keep the overview level forever; evict the oldest detail tiles beyond the budget.
    if (cache.size > 340) for (const k of cache.keys()) { if (!k.startsWith("1:")) { cache.delete(k); if (cache.size <= 300) break; } }
    return canvas;
  }
  // Releases other regions' memory.
  function keepOnly(regionId) { for (const id of [...worlds.keys()]) if (id !== regionId) worlds.delete(id); }
  // Keeps these regions' worlds (the continent keeps the few near the view), oldest others released.
  function keep(regionIds) { for (const id of [...worlds.keys()]) if (!regionIds.includes(id)) worlds.delete(id); }

  window.MapTerrain = { W, H, TILE, LEVELS, CELL, FEATHER, world, tile, render, keepOnly, keep, sampler, tone, makeToolkit, hash2, rng, DRAW, cells, canopy, rgbOf };
})();
