/* The continent (port task 8, Map Rework; decisions W1–W18 in engine/Docs/Engine-Plan.md, "Decisions for task 8").
   The 17 regions are placed whole on one continent, each enlarged 1.4× (W3), where their roads already point (W6): a
   region keeps its own coordinates ("local", 1600 × 1000 units) and this module places them in continent units
   ("C"), C = origin + local × SCALE. Every point of the continent belongs to the nearest region (its territory).
   The land is natural (W17, W18): one noise-shaped field (`landAt`, positive on land) gives the coast its capes and
   bays and the mountains between neighbours their ragged feet. It moves up to EDGE units (about 30 km) from a region's
   old straight edge, out past it (wild ground that keeps the edge's rules) or in (sea or mountains); a core of
   mountains always parts two neighbours no road joins; between two a road joins lies a pass (the old border strip,
   STRIP units, with ragged ends) the road crosses with no prompt; roads, settlements, sites and exits are never cut.
   Pure rules, no page; loaded after world-region-data.js and before world-engine.js, which builds it from its
   regions (`build(regions, { ownLand })`). */
(root => {
  "use strict";
  const SCALE = 1.4, LW = 1600, LH = 1000, CW = LW * SCALE, CH = LH * SCALE, STRIP = 300;
  // A strip is walkable along its whole facing edge but these ends, so its corners never touch another strip.
  const STRIP_MARGIN = 100;
  // Top-left corner (local 0, 0) of every region on the continent, in continent units. Each is set so that the road
  // between two regions crosses its strip straight (layout draft 2, approved 2026-11-06, W6).
  const ORIGINS = Object.freeze({
    "alder-vale": { x: 0, y: 0 },
    "stoneveil-foothills": { x: 0, y: -1700 },
    "high-stoneveil": { x: -406, y: -3400 },
    "stoneveil-summit": { x: -406, y: -5100 },
    "sunward-coast": { x: 2484, y: -1700 },
    "cinder-isle": { x: 4900, y: -1600 },
    "verdant-reach": { x: -2484, y: 490 },
    "stormwake-hills": { x: -1812, y: 2134 },
    "gloamfen-marsh": { x: -4296, y: 2134 },
    "verdigris-jungle": { x: -4338, y: 3778 },
    "mirrormere-basin": { x: -1770, y: 3778 },
    "saffron-expanse": { x: -1770, y: 5422 },
    "deep-desert": { x: 714, y: 6276 },
    "crimson-divide": { x: -1770, y: 7066 },
    "bloodstone-coast": { x: -1770, y: 8710 },
    "ancient-battlefield": { x: -4254, y: 8710 },
    "duskpine-forest": { x: -6738, y: 8710 },
  });
  // The named impassable ground between neighbours no road joins (for the map's labels; passability itself is "not a
  // region's land and not a strip").
  const BARRIER_NAMES = Object.freeze([
    { name: "Western Stoneveil Spurs", x: -292, y: -126 },
    { name: "The Rainwall", x: 120, y: 1646 },
    { name: "Verdigris Range", x: -1934, y: 4500 },
    { name: "Glass Escarpment", x: 608, y: 7455 },
  ]);
  const toC = (regionId, p) => ({ x: ORIGINS[regionId].x + p.x * SCALE, y: ORIGINS[regionId].y + p.y * SCALE });
  const toLocal = (regionId, c) => ({ x: (c.x - ORIGINS[regionId].x) / SCALE, y: (c.y - ORIGINS[regionId].y) / SCALE });
  const rectOf = id => ({ x0: ORIGINS[id].x, y0: ORIGINS[id].y, x1: ORIGINS[id].x + CW, y1: ORIGINS[id].y + CH });
  const inRect = (r, c, pad = 0) => c.x >= r.x0 - pad && c.x <= r.x1 + pad && c.y >= r.y0 - pad && c.y <= r.y1 + pad;
  const rectDistance = (r, c) => Math.hypot(Math.max(r.x0 - c.x, 0, c.x - r.x1), Math.max(r.y0 - c.y, 0, c.y - r.y1));
  const IDS = Object.keys(ORIGINS);
  const RECTS = Object.fromEntries(IDS.map(id => [id, rectOf(id)]));
  // The region whose territory holds a continent point: the nearest region (its rectangle), so strips split mid-way.
  function regionAt(c) {
    let best = null, bestDistance = Infinity;
    for (const id of IDS) {
      const d = rectDistance(RECTS[id], c);
      if (d < bestDistance) { best = id; bestDistance = d; }
    }
    return best;
  }
  // The strip between two regions: the band between their facing edges, over the length they face each other.
  function stripBetween(a, b) {
    const A = RECTS[a], B = RECTS[b], horizontal = A.x1 <= B.x0 || B.x1 <= A.x0;
    if (horizontal) {
      const [left, right] = A.x1 <= B.x0 ? [A, B] : [B, A];
      return { x0: left.x1, x1: right.x0, y0: Math.max(A.y0, B.y0) + STRIP_MARGIN, y1: Math.min(A.y1, B.y1) - STRIP_MARGIN, horizontal };
    }
    const [top, bottom] = A.y1 <= B.y0 ? [A, B] : [B, A];
    return { x0: Math.max(A.x0, B.x0) + STRIP_MARGIN, x1: Math.min(A.x1, B.x1) - STRIP_MARGIN, y0: top.y1, y1: bottom.y0, horizontal };
  }
  let built = null;
  /* From the regions' data: the road links (one per pair of regions joined by a road: its two exits, the straight
     road across the strip, and each half as a local road for its own region) and the walkable strips. */
  function build(regions, options = {}) {
    const links = [], seen = new Set();
    for (const region of Object.values(regions)) for (const exit of region.exits || []) {
      if (!exit.targetRegionId || !ORIGINS[region.id] || !ORIGINS[exit.targetRegionId]) continue;
      const key = [region.id, exit.targetRegionId].sort().join("|");
      if (seen.has(key)) continue;
      const back = (regions[exit.targetRegionId].exits || []).find(other => other.targetRegionId === region.id);
      if (!back) continue;
      seen.add(key);
      const a = toC(region.id, exit.point), b = toC(exit.targetRegionId, back.point), middle = { x: (a.x + b.x) / 2, y: (a.y + b.y) / 2 };
      links.push({
        key, regions: [region.id, exit.targetRegionId], exits: [exit.id, back.id], path: [a, b], middle, strip: stripBetween(region.id, exit.targetRegionId),
        // Each region's half of the crossing, in its own coordinates (beyond its edge to the strip's middle).
        halves: { [region.id]: [exit.point, toLocal(region.id, middle)], [exit.targetRegionId]: [back.point, toLocal(exit.targetRegionId, middle)] },
      });
    }
    built = { links, regions, ownLand: options.ownLand || (() => true) };
    shape = null;
    return built;
  }
  const linksOf = regionId => (built?.links || []).filter(link => link.regions.includes(regionId));
  const linkBetween = (a, b) => (built?.links || []).find(link => link.regions.includes(a) && link.regions.includes(b)) || null;
  const inStrip = c => (built?.links || []).some(link => inRect(link.strip, c));

  /* ---------- The natural land (W17, W18) ----------
     One field on a GRID-unit grid over the whole continent, read with bilinear interpolation plus a fine noise: positive
     on land, about the distance to the coast or the mountains' foot in continent units. Built the first time it is read
     (the regions' own land comes from world-engine.js). */
  const EDGE = 400, GRID = 20, CORE = 40, HARD_CORE = 12, PAD = EDGE + 400;
  const MOUNTAINS = new Set(["high-stoneveil", "stoneveil-summit"]);
  const hash = (x, y, seed) => { let h = Math.imul(x | 0, 374761393) ^ Math.imul(y | 0, 668265263) ^ Math.imul(seed | 0, 2147483647); h = Math.imul(h ^ (h >>> 13), 1274126177); return ((h ^ (h >>> 16)) >>> 0) / 4294967296; };
  // Smooth value noise in 0..1, and its sum over octaves (normalized to 0..1).
  function noise(x, y, seed) {
    const xi = Math.floor(x), yi = Math.floor(y), fx = x - xi, fy = y - yi, u = fx * fx * (3 - 2 * fx), v = fy * fy * (3 - 2 * fy);
    const a = hash(xi, yi, seed), b = hash(xi + 1, yi, seed), c = hash(xi, yi + 1, seed), d = hash(xi + 1, yi + 1, seed);
    return a + (b - a) * u + (c - a) * v + (a - b - c + d) * u * v;
  }
  function fbm(x, y, seed, octaves, gain = 0.5) {
    let sum = 0, weight = 1, total = 0, f = 1;
    for (let k = 0; k < octaves; k++) { sum += noise(x * f + k * 31.7, y * f - k * 17.3, seed + k * 101) * weight; total += weight; weight *= gain; f *= 2.03; }
    return sum / total;
  }
  const smooth = (a, b, v) => { const t = Math.max(0, Math.min(1, (v - a) / (b - a))); return t * t * (3 - 2 * t); };
  // How far the land moves from a region's old edge here: -EDGE (bitten in) to +EDGE (reaching out), a domain-warped noise
  // over capes and bays about 1,700 units apart.
  // Capes and bays from about 1,100 units across down to 70, the middle sizes strong (gain 0.62) so no long stretch of
  // an old edge stays straight. CENTER (a little below the noise's median) leans the edge slightly outward; SPREAD lets
  // about one stretch in twelve reach the full EDGE each way.
  const CENTER = 0.47, SPREAD = 0.165;
  function margin(c) {
    const wx = (fbm(c.x / 900, c.y / 900, 41, 3) - 0.5) * 1.1, wy = (fbm(c.x / 900 + 17.3, c.y / 900 - 9.1, 43, 3) - 0.5) * 1.1;
    const n = fbm(c.x / 1100 + wx, c.y / 1100 + wy, 47, 5, 0.62);
    return EDGE * Math.max(-1, Math.min(1, (n - CENTER) / SPREAD));
  }
  // Signed distance to a region's rectangle with its corners rounded (CORNER units), negative inside: so a coast that
  // bites in turns a corner as smoothly as one that reaches out.
  const CORNER = 320;
  function roundedDistance(r, c) {
    const hx = (r.x1 - r.x0) / 2, hy = (r.y1 - r.y0) / 2, qx = Math.abs(c.x - (r.x0 + hx)) - hx + CORNER, qy = Math.abs(c.y - (r.y0 + hy)) - hy + CORNER;
    return Math.hypot(Math.max(qx, 0), Math.max(qy, 0)) + Math.min(Math.max(qx, qy), 0) - CORNER;
  }
  // The fine wiggle of the coast, below the grid (also part of the rules, so map and rules agree).
  const detail = c => (noise(c.x / 61, c.y / 61, 51) - 0.5) * 34 + (noise(c.x / 23, c.y / 23, 53) - 0.5) * 14;
  // A region's edge is land or its own sea (a coast region's water, an island's): the natural edge moves only where its
  // edge is land, sampled every 10 local units and softened over about 80.
  const edgeCache = new Map();
  function edgeLand(id, local) {
    let edges = edgeCache.get(id);
    if (!edges) {
      const own = built.ownLand, soft = list => list.map((_, i) => { let sum = 0, count = 0; for (let k = -8; k <= 8; k++) { const v = list[i + k]; if (v != null) { sum += v; count++; } } return sum / count; });
      const along = (count, at) => soft(Array.from({ length: count }, (_, i) => own(at(i * 10), id) ? 1 : 0));
      edges = { top: along(161, x => ({ x, y: 3 })), bottom: along(161, x => ({ x, y: LH - 3 })), left: along(101, y => ({ x: 3, y })), right: along(101, y => ({ x: LW - 3, y })) };
      edgeCache.set(id, edges);
    }
    const p = { x: Math.max(0, Math.min(LW, local.x)), y: Math.max(0, Math.min(LH, local.y)) };
    const sides = [["left", p.x, p.y], ["right", LW - p.x, p.y], ["top", p.y, p.x], ["bottom", LH - p.y, p.x]];
    const [side, , at] = sides.reduce((best, entry) => entry[1] < best[1] ? entry : best);
    const list = edges[side], f = at / 10, i = Math.max(0, Math.min(list.length - 2, Math.floor(f))), t = Math.max(0, Math.min(1, f - i));
    return list[i] * (1 - t) + list[i + 1] * t;
  }
  /* A coast region's own shore (W17): its straight shore (the Sunward Coast's, the Bloodstone Coast's) or its island's
     oval (Cinder Isle) moves like the rest of the coast, in that region's own units: held still at a port, never closer
     than 40 units to its roads and places, never past its box. Sampled every 2 units along the shore. */
  const shoreCache = new Map();
  function shoreShift(regionId, local) {
    const region = built?.regions && Object.values(built.regions).find(r => r.id === regionId), shape = region?.landShape;
    if (!shape) return 0;
    let entry = shoreCache.get(regionId);
    if (!entry) {
      const ports = (region.settlements || []).filter(place => place.boat), reach = EDGE / SCALE;
      const marks = [...(region.roads || []).flat(), ...(region.settlements || []), ...(region.sites || []), ...(region.exits || []).map(exit => exit.point)];
      const fade = d => smooth(60, 240, d);
      if (shape.type === "coast" || shape.type === "south-coast") {
        const vertical = shape.type === "coast", shore = vertical ? shape.shoreX : shape.shoreY, span = vertical ? LH : LW, limit = vertical ? LW - 20 : LH - 20;
        const values = new Float32Array(span / 2 + 1);
        for (let i = 0; i < values.length; i++) {
          const t = i * 2, at = vertical ? { x: shore, y: t } : { x: t, y: shore };
          let shift = margin(toC(regionId, at)) / SCALE;
          for (const port of ports) shift *= fade(Math.abs((vertical ? port.y : port.x) - t));
          let least = -Infinity;
          for (const mark of marks) if (Math.abs((vertical ? mark.y : mark.x) - t) < 90) least = Math.max(least, (vertical ? mark.x : mark.y) + 40);
          values[i] = Math.max(least - shore, Math.min(limit - shore, Math.max(-reach, Math.min(reach, shift))));
        }
        entry = { along: p => vertical ? p.y : p.x, values };
      } else if (shape.type === "ellipse") {
        const values = new Float32Array(721), portAngles = ports.map(port => Math.atan2((port.y - shape.y) / shape.ry, (port.x - shape.x) / shape.rx));
        for (let i = 0; i <= 720; i++) {
          const angle = i / 720 * 2 * Math.PI - Math.PI, at = { x: shape.x + Math.cos(angle) * shape.rx, y: shape.y + Math.sin(angle) * shape.ry };
          let shift = margin(toC(regionId, at)) / SCALE * 0.35;
          for (const portAngle of portAngles) { const gap = Math.abs(Math.atan2(Math.sin(angle - portAngle), Math.cos(angle - portAngle))); shift *= smooth(0.12, 0.45, gap); }
          // The island stays 40 units inside its box (its edges are its own sea).
          const cx = Math.cos(angle) * shape.rx, cy = Math.sin(angle) * shape.ry;
          const room = Math.min(Math.abs(cx) > 1e-6 ? ((cx > 0 ? LW - 40 - shape.x : shape.x - 40) / Math.abs(cx)) - 1 : Infinity, Math.abs(cy) > 1e-6 ? ((cy > 0 ? LH - 40 - shape.y : shape.y - 40) / Math.abs(cy)) - 1 : Infinity);
          values[i] = Math.min(room, shift / Math.hypot(cx, cy));
        }
        entry = { along: p => (Math.atan2((p.y - shape.y) / shape.ry, (p.x - shape.x) / shape.rx) + Math.PI) / (2 * Math.PI) * 1440, values };
      }
      shoreCache.set(regionId, entry);
    }
    if (!entry) return 0;
    const f = Math.max(0, Math.min(entry.values.length - 1.001, entry.along(local) / 2)), i = f | 0, t = f - i;
    return entry.values[i] * (1 - t) + entry.values[i + 1] * t;
  }
  const gapBetween = (a, b) => { const A = RECTS[a], B = RECTS[b]; return Math.hypot(Math.max(A.x0 - B.x1, B.x0 - A.x1, 0), Math.max(A.y0 - B.y1, B.y0 - A.y1, 0)); };
  let shape = null;
  function makeShape() {
    const b = bounds(), x0 = Math.floor((b.x0 - PAD) / GRID) * GRID, y0 = Math.floor((b.y0 - PAD) / GRID) * GRID;
    const cols = Math.ceil((b.x1 + PAD - x0) / GRID) + 1, rows = Math.ceil((b.y1 + PAD - y0) / GRID) + 1;
    const land = new Float32Array(cols * rows), ridge = new Float32Array(cols * rows), owner = new Uint8Array(cols * rows);
    const n = IDS.length, links = built.links, d = new Float64Array(n);
    const linked = IDS.map(a => IDS.map(z => a !== z && Boolean(linkBetween(a, z))));
    const close = IDS.map(a => IDS.map(z => a !== z && gapBetween(a, z) < 800 ? gapBetween(a, z) : -1));
    const passes = links.map(link => {
      const strip = link.strip, i = IDS.indexOf(link.regions[0]), j = IDS.indexOf(link.regions[1]);
      return { strip, i, j, seed: 60 + IDS.indexOf(link.regions[0]) * 17 + IDS.indexOf(link.regions[1]) };
    });
    for (let row = 0; row < rows; row++) for (let col = 0; col < cols; col++) {
      const c = { x: x0 + col * GRID, y: y0 + row * GRID }, cell = row * cols + col;
      let first = 0;
      for (let k = 0; k < n; k++) { d[k] = rectDistance(RECTS[IDS[k]], c); if (d[k] < d[first]) first = k; }
      owner[cell] = first;
      const R = RECTS[IDS[first]], d1 = d[first], sd = roundedDistance(R, c);
      // Mountains (not sea) where the land is not: between close neighbours, and round the Stoneveil's high massif.
      let rg = 0;
      for (let k = 0; k < n; k++) {
        const gap = close[first][k];
        if (gap < 0) continue;
        // Only between two edges of land (a strait between a coast and an island stays sea).
        const shores = Math.min(edgeLand(IDS[first], toLocal(IDS[first], c)), edgeLand(IDS[k], toLocal(IDS[k], c)));
        if (shores <= 0.01) continue;
        const lens = d1 + d[k] - gap, w = 260 + 520 * fbm(c.x / 700, c.y / 700, 71, 3, 0.6);
        rg = Math.max(rg, smooth(w, w * 0.55, lens) * smooth(0, 0.6, shores));
      }
      if (MOUNTAINS.has(IDS[first])) { const reach = 380 + 300 * Math.max(-1, Math.min(1, (fbm(c.x / 650, c.y / 650, 73, 4, 0.6) - 0.5) / 0.14)); rg = Math.max(rg, smooth(reach, reach * 0.5, d1)); }
      ridge[cell] = rg * smooth(EDGE + 60, EDGE - 40, d1);
      if (sd < -(EDGE + 60)) { land[cell] = -sd - EDGE; continue; }
      if (d1 > EDGE + 260) { land[cell] = EDGE - d1; continue; }
      // Where the edge is the region's own sea, the field sinks a little, so the fine wiggle leaves no specks of land
      // just past it.
      const shore = edgeLand(IDS[first], toLocal(IDS[first], c));
      let value = margin(c) * shore - sd - (1 - shore) * 80;
      // A core of mountains (or sea) parts every two regions, wider than the reach of either's land...
      for (let k = 0; k < n; k++) if (k !== first) value = Math.min(value, (d[k] - d1) / 2 - CORE);
      // ...but where a road joins them: the pass, the whole gap across, its ends ragged.
      for (const pass of passes) {
        if (first !== pass.i && first !== pass.j) continue;
        const S = pass.strip;
        const along = S.horizontal ? Math.max(S.y0 - c.y, c.y - S.y1) : Math.max(S.x0 - c.x, c.x - S.x1);
        const across = S.horizontal ? Math.max(S.x0 - c.x, c.x - S.x1) : Math.max(S.y0 - c.y, c.y - S.y1);
        if (along > 500 || across > 600) continue;
        const reach = -250 + 330 * noise(c.x / 520, c.y / 520, pass.seed);
        let open = reach - along - Math.max(0, across);
        for (let k = 0; k < n; k++) if (k !== first && k !== pass.i && k !== pass.j) open = Math.min(open, (d[k] - d1) / 2 - CORE);
        value = Math.max(value, open);
      }
      land[cell] = value;
    }
    // Roads, settlements, sites, exits and starting points keep their ground (but never cross the core between two
    // regions no road joins).
    const hardCore = (cell, c) => {
      let value = Infinity;
      const first = owner[cell], d1 = rectDistance(RECTS[IDS[first]], c);
      for (let k = 0; k < n; k++) if (k !== first && !linked[first][k]) value = Math.min(value, (rectDistance(RECTS[IDS[k]], c) - d1) / 2 - HARD_CORE);
      return value;
    };
    const stamp = (ax, ay, bx, by, radius) => {
      const minX = Math.min(ax, bx) - radius, maxX = Math.max(ax, bx) + radius, minY = Math.min(ay, by) - radius, maxY = Math.max(ay, by) + radius;
      const dx = bx - ax, dy = by - ay, length = dx * dx + dy * dy;
      for (let row = Math.max(0, Math.floor((minY - y0) / GRID)); row <= Math.min(rows - 1, Math.ceil((maxY - y0) / GRID)); row++)
        for (let col = Math.max(0, Math.floor((minX - x0) / GRID)); col <= Math.min(cols - 1, Math.ceil((maxX - x0) / GRID)); col++) {
          const c = { x: x0 + col * GRID, y: y0 + row * GRID }, t = length ? Math.max(0, Math.min(1, ((c.x - ax) * dx + (c.y - ay) * dy) / length)) : 0;
          const value = radius - Math.hypot(c.x - (ax + dx * t), c.y - (ay + dy * t)), cell = row * cols + col;
          if (value > land[cell]) land[cell] = Math.max(land[cell], Math.min(value, hardCore(cell, c)));
        }
    };
    for (const region of Object.values(built.regions)) {
      if (!ORIGINS[region.id]) continue;
      const C = p => toC(region.id, p);
      const paths = [...(region.roads || []), ...linksOf(region.id).map(link => link.halves[region.id])];
      for (const path of paths) for (let k = 1; k < path.length; k++) { const a = C(path[k - 1]), z = C(path[k]); stamp(a.x, a.y, z.x, z.y, 55); }
      for (const place of region.settlements || []) { const a = C(place); stamp(a.x, a.y, a.x, a.y, place.kind === "Castle city" ? 150 : 110); }
      for (const site of region.sites || []) { const a = C(site); stamp(a.x, a.y, a.x, a.y, (site.radius || 40) * SCALE + 60); }
      for (const exit of region.exits || []) { const a = C(exit.point); stamp(a.x, a.y, a.x, a.y, 80); }
      if (region.start) { const a = C(region.start); stamp(a.x, a.y, a.x, a.y, 80); }
    }
    return { x0, y0, cols, rows, land, ridge, owner };
  }
  const field = () => shape || (shape = makeShape());
  function bilinear(grid, c) {
    const s = field(), gx = Math.max(0, Math.min(s.cols - 1.001, (c.x - s.x0) / GRID)), gy = Math.max(0, Math.min(s.rows - 1.001, (c.y - s.y0) / GRID));
    const ix = gx | 0, iy = gy | 0, fx = gx - ix, fy = gy - iy, i = iy * s.cols + ix;
    return (grid[i] * (1 - fx) + grid[i + 1] * fx) * (1 - fy) + (grid[i + s.cols] * (1 - fx) + grid[i + s.cols + 1] * fx) * fy;
  }
  // The land field at a continent point: positive on land (before a region's own lakes and seas).
  const landAt = c => bilinear(field().land, c) + detail(c);
  // Where the land is not: 1 for mountains, 0 for the sea (blended between).
  const ridgeAt = c => bilinear(field().ridge, c);
  // Whether a continent point can be stood on: the natural land, and its region's own land there (`isLand` answers both
  // in the region's own coordinates).
  function passable(c, isLand) {
    const id = regionAt(c);
    return isLand(toLocal(id, c), id);
  }
  const bounds = () => {
    const xs = IDS.flatMap(id => [RECTS[id].x0, RECTS[id].x1]), ys = IDS.flatMap(id => [RECTS[id].y0, RECTS[id].y1]);
    return { x0: Math.min(...xs), y0: Math.min(...ys), x1: Math.max(...xs), y1: Math.max(...ys) };
  };
  root.ContinentRules = { SCALE, LW, LH, CW, CH, STRIP, STRIP_MARGIN, EDGE, GRID, ORIGINS, RECTS, IDS, BARRIER_NAMES, MOUNTAINS, toC, toLocal, inRect, rectDistance, regionAt, stripBetween, build, linksOf, linkBetween, inStrip, passable, bounds, landAt, ridgeAt, shoreShift, margin, noise, fbm, field, get built() { return built; } };
  if (typeof module !== "undefined" && module.exports) module.exports = root.ContinentRules;
})(globalThis);
