/* The continent (port task 8, Map Rework; decisions W1–W29 in engine/Docs/Engine-Plan.md, "Decisions for task 8").
   The regions are placed whole on one continent, each enlarged 1.4× (W3), as layout draft 6 placed them (W26;
   engine/Docs/Port-Tasks/8_Map_Rework/): a region keeps its own coordinates ("local", 1600 × 1000 units) and this module
   places them in continent units ("C"), C = origin + local × SCALE. Every point of the continent belongs to the nearest
   region (its territory), so region rules take local points past 0–1600 (up to REACH units).
   The land is draft 6's (continent-shape.js, loaded before this): a natural coast up to about 30 km in or out from the
   regions' old straight edges (W17, W18), regions grown into the ground between them with nothing forcing two apart
   (W20, W22), two inland waters and the Deep Desert's island (W24, W25). On top of it roads, settlements, sites, exits and
   starting points always keep their ground. Three regions are empty until the port (W27–W29: `uncharted` in their data).
   Pure rules, no page; loaded after world-region-data.js and continent-shape.js and before world-engine.js, which builds
   it from its regions (`build(regions)`). */
(root => {
  "use strict";
  const SCALE = 1.4, LW = 1600, LH = 1000, CW = LW * SCALE, CH = LH * SCALE;
  // How far a region's ground reaches past its own map, in its own units (draft 6's land reaches 550; held with room).
  const REACH = 600;
  // The border strip's width between two regions a road joins (draft 2's; the road crosses it straight) and its ends.
  const STRIP = 300, STRIP_MARGIN = 100;
  // Top-left corner (local 0, 0) of every region on the continent, in continent units (layout draft 6, W26). Each
  // road between two regions crosses the ground between them straight.
  const ORIGINS = Object.freeze({
    "alder-vale": { x: 0, y: 0 },
    "stoneveil-foothills": { x: 0, y: -1700 },
    "high-stoneveil": { x: -406, y: -3400 },
    "stoneveil-summit": { x: -406, y: -5100 },
    "sunward-coast": { x: 2484, y: -1700 },
    "cinder-isle": { x: 7900, y: 490 },
    "verdant-reach": { x: -2484, y: 490 },
    "stormwake-hills": { x: -1812, y: 2134 },
    "gloamfen-marsh": { x: -4296, y: 2134 },
    "verdigris-jungle": { x: -4338, y: 3778 },
    "mirrormere-basin": { x: -1770, y: 3778 },
    "saffron-expanse": { x: -1770, y: 5422 },
    "deep-desert": { x: 714, y: 4330 },
    "crimson-divide": { x: -1770, y: 7066 },
    "bloodstone-coast": { x: -1770, y: 8710 },
    "ancient-battlefield": { x: -4254, y: 8710 },
    "duskpine-forest": { x: -4254, y: 7066 },
    "uncharted-highlands": { x: 2484, y: -56 },
    "uncharted-steppe": { x: 672, y: 1644 },
    "uncharted-delta": { x: -4296, y: 5422 },
  });
  const toC = (regionId, p) => ({ x: ORIGINS[regionId].x + p.x * SCALE, y: ORIGINS[regionId].y + p.y * SCALE });
  const toLocal = (regionId, c) => ({ x: (c.x - ORIGINS[regionId].x) / SCALE, y: (c.y - ORIGINS[regionId].y) / SCALE });
  const rectOf = id => ({ x0: ORIGINS[id].x, y0: ORIGINS[id].y, x1: ORIGINS[id].x + CW, y1: ORIGINS[id].y + CH });
  const inRect = (r, c, pad = 0) => c.x >= r.x0 - pad && c.x <= r.x1 + pad && c.y >= r.y0 - pad && c.y <= r.y1 + pad;
  const rectDistance = (r, c) => Math.hypot(Math.max(r.x0 - c.x, 0, c.x - r.x1), Math.max(r.y0 - c.y, 0, c.y - r.y1));
  const IDS = Object.keys(ORIGINS);
  const RECTS = Object.fromEntries(IDS.map(id => [id, rectOf(id)]));
  // The region whose territory holds a continent point: the nearest region (its rectangle).
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
  let built = null, shape = null;
  /* From the regions' data: the road links (one per pair of regions joined by a road: its two exits, the straight road
     across the ground between them, and each half as a local road for its own region). */
  function build(regions) {
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
        // Each region's half of the crossing, in its own coordinates (beyond its edge to the middle).
        halves: { [region.id]: [exit.point, toLocal(region.id, middle)], [exit.targetRegionId]: [back.point, toLocal(exit.targetRegionId, middle)] },
      });
    }
    built = { links, regions };
    shape = null;
    return built;
  }
  const linksOf = regionId => (built?.links || []).filter(link => link.regions.includes(regionId));
  const linkBetween = (a, b) => (built?.links || []).find(link => link.regions.includes(a) && link.regions.includes(b)) || null;
  const inStrip = c => (built?.links || []).some(link => inRect(link.strip, c));

  /* ---------- The land (draft 6, W26) ----------
     continent-shape.js's grid, decoded the first time it is read, with every road (and each crossing between regions),
     settlement, site, exit and starting point stamped onto it so it always stands on land. Read with bilinear
     interpolation: positive on land, about the distance to the shore in continent units. */
  const KEEP = { road: 55, castle: 150, town: 110, site: 60, exit: 80, start: 80 };
  // How far apart two roads' ends can be and still be walked across as one road (world-engine.js's ROAD_GAP).
  const ROAD_GAP = 200 * SCALE;
  function decode(source) {
    const alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/", text = source.DATA.replace(/=+$/, "");
    const bytes = new Uint8Array(Math.floor(text.length * 3 / 4));
    let bits = 0, value = 0, n = 0;
    for (let i = 0; i < text.length; i++) {
      value = value << 6 | alphabet.indexOf(text[i]); bits += 6;
      if (bits >= 8) { bits -= 8; bytes[n++] = value >> bits & 255; }
    }
    const land = new Float32Array(source.COLS * source.ROWS);
    let cell = 0;
    for (let i = 0; i < n && cell < land.length;) {
      if (bytes[i] === 255) { const v = (bytes[i + 1] - 127) * source.UNIT; for (let k = 0; k < bytes[i + 2]; k++) land[cell++] = v; i += 3; }
      else { land[cell++] = (bytes[i] - 127) * source.UNIT; i += 1; }
    }
    if (cell !== land.length) throw new Error(`continent-shape.js holds ${cell} cells, not ${land.length}`);
    return land;
  }
  function makeShape() {
    const source = root.ContinentShape;
    if (!source) throw new Error("continent-shape.js must load before continent-engine.js");
    const { X0: x0, Y0: y0, STEP: step, COLS: cols, ROWS: rows } = source, land = decode(source);
    // For the pictures: how near each cell is to a region's own roads, river, settlements and sites, and whose.
    const near = new Float32Array(cols * rows).fill(Infinity), nearOwner = new Int16Array(cols * rows).fill(-1);
    const mark = (a, z, radius, owner) => {
      const dx = z.x - a.x, dy = z.y - a.y, length = dx * dx + dy * dy;
      for (let row = Math.max(0, Math.floor((Math.min(a.y, z.y) - radius - y0) / step)); row <= Math.min(rows - 1, Math.ceil((Math.max(a.y, z.y) + radius - y0) / step)); row++)
        for (let col = Math.max(0, Math.floor((Math.min(a.x, z.x) - radius - x0) / step)); col <= Math.min(cols - 1, Math.ceil((Math.max(a.x, z.x) + radius - x0) / step)); col++) {
          const cx = x0 + col * step, cy = y0 + row * step, t = length ? Math.max(0, Math.min(1, ((cx - a.x) * dx + (cy - a.y) * dy) / length)) : 0;
          const d = Math.hypot(cx - (a.x + dx * t), cy - (a.y + dy * t)) - (radius - CORRIDOR), cell = row * cols + col;
          if (d < near[cell]) { near[cell] = d; nearOwner[cell] = owner; }
        }
    };
    const stamp = (a, z, radius) => {
      const dx = z.x - a.x, dy = z.y - a.y, length = dx * dx + dy * dy;
      for (let row = Math.max(0, Math.floor((Math.min(a.y, z.y) - radius - y0) / step)); row <= Math.min(rows - 1, Math.ceil((Math.max(a.y, z.y) + radius - y0) / step)); row++)
        for (let col = Math.max(0, Math.floor((Math.min(a.x, z.x) - radius - x0) / step)); col <= Math.min(cols - 1, Math.ceil((Math.max(a.x, z.x) + radius - x0) / step)); col++) {
          const cx = x0 + col * step, cy = y0 + row * step, t = length ? Math.max(0, Math.min(1, ((cx - a.x) * dx + (cy - a.y) * dy) / length)) : 0;
          const value = radius - Math.hypot(cx - (a.x + dx * t), cy - (a.y + dy * t)), cell = row * cols + col;
          if (value > land[cell]) land[cell] = value;
        }
    };
    for (const region of Object.values(built.regions)) {
      if (!ORIGINS[region.id]) continue;
      const C = p => toC(region.id, p), owner = IDS.indexOf(region.id);
      for (const path of [...(region.roads || []), ...(region.river ? [region.river] : [])])
        for (let k = 1; k < path.length; k++) mark(C(path[k - 1]), C(path[k]), CORRIDOR, owner);
      for (const place of [...(region.settlements || []), ...(region.sites || [])]) mark(C(place), C(place), CORRIDOR + 60, owner);
      const paths = [...(region.roads || []), ...linksOf(region.id).map(link => link.halves[region.id])];
      for (const path of paths) for (let k = 1; k < path.length; k++) stamp(C(path[k - 1]), C(path[k]), KEEP.road);
      // The ground across a gap between two roads (where world-engine.js's road graph walks across, the broken
      // roads of the Ancient Battlefield) is kept too.
      paths.forEach((path, index) => {
        for (const end of [path[0], path.at(-1)]) {
          let best = null, bestDistance = ROAD_GAP;
          paths.forEach((other, otherIndex) => { if (otherIndex !== index) for (const p of other) { const d = Math.hypot(C(p).x - C(end).x, C(p).y - C(end).y); if (d < bestDistance && d > 1e-7) { best = p; bestDistance = d; } } });
          if (best) stamp(C(end), C(best), KEEP.road);
        }
      });
      for (const place of region.settlements || []) stamp(C(place), C(place), place.kind === "Castle city" ? KEEP.castle : KEEP.town);
      for (const site of region.sites || []) stamp(C(site), C(site), (site.radius || 40) * SCALE + KEEP.site);
      for (const exit of region.exits || []) stamp(C(exit.point), C(exit.point), KEEP.exit);
      if (region.start && !region.uncharted) stamp(C(region.start), C(region.start), KEEP.start);
    }
    return { x0, y0, step, cols, rows, land, near, nearOwner };
  }
  const field = () => shape || (shape = makeShape());
  // The land at a continent point: positive on land (before a region's own lakes).
  function landAt(c) {
    const s = field(), gx = Math.max(0, Math.min(s.cols - 1.001, (c.x - s.x0) / s.step)), gy = Math.max(0, Math.min(s.rows - 1.001, (c.y - s.y0) / s.step));
    const ix = gx | 0, iy = gy | 0, fx = gx - ix, fy = gy - iy, i = iy * s.cols + ix, l = s.land;
    return (l[i] * (1 - fx) + l[i + 1] * fx) * (1 - fy) + (l[i + s.cols] * (1 - fx) + l[i + s.cols + 1] * fx) * fy;
  }
  // Whether a continent point can be stood on: the land, and its region's own land there (`isLand` answers both in the
  // region's own coordinates).
  function passable(c, isLand) {
    const id = regionAt(c);
    return isLand(toLocal(id, c), id);
  }
  /* ---------- For the map's pictures ----------
     A region's picture covers its territory and BLEND units past it, fading out over the line where the neighbouring
     territory starts. Pictures are drawn in IDS order: one drawn earlier stays opaque across that line (the later one
     fades in over it); one next to a region drawn later never (an uncharted one, W29) fades out itself. */
  // Each region keeps CORRIDOR units of its own picture round its roads, river, settlements and sites.
  const BLEND = 70, BEND = 480, INSIDE = 340, CORRIDOR = 70;
  const hash = (x, y, seed) => { let h = Math.imul(x | 0, 374761393) ^ Math.imul(y | 0, 668265263) ^ Math.imul(seed | 0, 2147483647); h = Math.imul(h ^ (h >>> 13), 1274126177); return ((h ^ (h >>> 16)) >>> 0) / 4294967296; };
  function noise(x, y, seed) {
    const xi = Math.floor(x), yi = Math.floor(y), fx = x - xi, fy = y - yi, u = fx * fx * (3 - 2 * fx), v = fy * fy * (3 - 2 * fy);
    const a = hash(xi, yi, seed), b = hash(xi + 1, yi, seed), c = hash(xi, yi + 1, seed), d = hash(xi + 1, yi + 1, seed);
    return a + (b - a) * u + (c - a) * v + (a - b - c + d) * u * v;
  }
  const wander = (x, y, seed) => Math.max(-0.5, Math.min(0.5, (noise(x / 800, y / 800, seed) * 0.7 + noise(x / 300, y / 300, seed + 1) * 0.3 - 0.5) * 1.6));
  // The point the pictures take a continent point for: moved up to BEND units, so where two regions' pictures meet the
  // line wanders like the approved draft's (W26) instead of running straight between their rectangles.
  const bent = c => ({ x: c.x + wander(c.x, c.y, 71) * 2 * BEND, y: c.y + wander(c.x, c.y, 73) * 2 * BEND });
  const smoothUnit = t => { const v = Math.max(0, Math.min(1, t)); return v * v * (3 - 2 * v); };
  const depthIn = (r, c) => Math.min(c.x - r.x0, r.x1 - c.x, c.y - r.y0, r.y1 - c.y);
  // The region whose picture shows at a continent point (the region empty until the port included).
  function paintedAt(c) {
    const b = bent(c);
    let best = null, bestDistance = Infinity;
    for (const id of IDS) { const d = rectDistance(RECTS[id], b); if (d < bestDistance) { best = id; bestDistance = d; } }
    return best;
  }
  // paintedAt on a grid twice as coarse as the land's, computed once (the fog and the backdrop read it for every cell).
  let painted = null;
  function paintedGrid() {
    if (painted) return painted;
    const s = field(), step = s.step * 2, cols = Math.ceil(s.cols / 2), rows = Math.ceil(s.rows / 2), owner = new Uint8Array(cols * rows);
    // (paintedAt inlined: squared distances to the rectangles, no square roots, for the 300,000 cells.)
    const boxes = IDS.map(id => RECTS[id]);
    for (let row = 0; row < rows; row++) for (let col = 0; col < cols; col++) {
      const x = s.x0 + col * step, y = s.y0 + row * step, bx = x + wander(x, y, 71) * 2 * BEND, by = y + wander(x, y, 73) * 2 * BEND;
      let best = 0, bestDistance = Infinity;
      for (let k = 0; k < boxes.length; k++) {
        const r = boxes[k], dx = bx < r.x0 ? r.x0 - bx : bx > r.x1 ? bx - r.x1 : 0, dy = by < r.y0 ? r.y0 - by : by > r.y1 ? by - r.y1 : 0, d = dx * dx + dy * dy;
        if (d < bestDistance) { bestDistance = d; best = k; }
      }
      owner[row * cols + col] = best;
    }
    painted = { x0: s.x0, y0: s.y0, step, cols, rows, owner };
    return painted;
  }
  const paintedIdAt = c => {
    const g = paintedGrid(), col = Math.max(0, Math.min(g.cols - 1, Math.round((c.x - g.x0) / g.step))), row = Math.max(0, Math.min(g.rows - 1, Math.round((c.y - g.y0) / g.step)));
    return IDS[g.owner[row * g.cols + col]];
  };
  function cover(c, regionId, drawnAfter = id => !root.WorldRules?.getRegion(id)?.uncharted) {
    const b = bent(c), own = rectDistance(RECTS[regionId], b);
    let other = Infinity, neighbour = null;
    for (const id of IDS) { if (id === regionId) continue; const d = rectDistance(RECTS[id], b); if (d < other) { other = d; neighbour = id; } }
    let weight = smoothUnit((other - own + BLEND) / (2 * BLEND));
    // A region's own map is its own: a neighbour's picture reaches at most INSIDE units into it, and never over the
    // ground round its roads, river and places.
    for (const id of IDS) {
      if (!inRect(RECTS[id], c)) continue;
      const depth = smoothUnit(depthIn(RECTS[id], c) / INSIDE);
      weight = id === regionId ? Math.max(weight, depth) : Math.min(weight, 1 - depth);
    }
    const s = field(), col = Math.round((c.x - s.x0) / s.step), row = Math.round((c.y - s.y0) / s.step);
    if (col >= 0 && row >= 0 && col < s.cols && row < s.rows) {
      const cell = row * s.cols + col, owner = s.nearOwner[cell];
      if (owner >= 0) {
        const kept = smoothUnit(1 - s.near[cell] / CORRIDOR);
        weight = IDS[owner] === regionId ? Math.max(weight, kept) : Math.min(weight, 1 - kept);
      }
    }
    if (weight <= 0) return 0;
    return IDS.indexOf(regionId) < IDS.indexOf(neighbour) && drawnAfter(neighbour) ? 1 : weight;
  }
  // The part of a region's own map its picture can need: its territory (with the blend) where there is land or the sea
  // within `sea` units of it, in its own units, on whole multiples of `snap`.
  const extents = new Map();
  function extentOf(regionId, sea = 200, snap = 16) {
    const key = regionId + ":" + sea + ":" + snap;
    if (extents.has(key)) return extents.get(key);
    const s = field(), r = RECTS[regionId], reach = REACH * SCALE + BLEND + 2 * BEND;
    let x0 = r.x0, y0 = r.y0, x1 = r.x1, y1 = r.y1;
    for (let row = Math.max(0, Math.floor((r.y0 - reach - s.y0) / s.step)); row <= Math.min(s.rows - 1, Math.ceil((r.y1 + reach - s.y0) / s.step)); row++)
      for (let col = Math.max(0, Math.floor((r.x0 - reach - s.x0) / s.step)); col <= Math.min(s.cols - 1, Math.ceil((r.x1 + reach - s.x0) / s.step)); col++) {
        const c = { x: s.x0 + col * s.step, y: s.y0 + row * s.step };
        if (s.land[row * s.cols + col] <= -sea || inRect(r, c) || cover(c, regionId, () => false) <= 0) continue;
        x0 = Math.min(x0, c.x); y0 = Math.min(y0, c.y); x1 = Math.max(x1, c.x); y1 = Math.max(y1, c.y);
      }
    const a = toLocal(regionId, { x: x0 - s.step, y: y0 - s.step }), b = toLocal(regionId, { x: x1 + s.step, y: y1 + s.step });
    const extent = { x0: Math.floor(a.x / snap) * snap, y0: Math.floor(a.y / snap) * snap, x1: Math.ceil(b.x / snap) * snap, y1: Math.ceil(b.y / snap) * snap };
    extents.set(key, extent);
    return extent;
  }
  const bounds = () => {
    const xs = IDS.flatMap(id => [RECTS[id].x0, RECTS[id].x1]), ys = IDS.flatMap(id => [RECTS[id].y0, RECTS[id].y1]);
    return { x0: Math.min(...xs), y0: Math.min(...ys), x1: Math.max(...xs), y1: Math.max(...ys) };
  };
  root.ContinentRules = { SCALE, LW, LH, CW, CH, REACH, STRIP, STRIP_MARGIN, KEEP, ORIGINS, RECTS, IDS, toC, toLocal, inRect, rectDistance, regionAt, stripBetween, build, linksOf, linkBetween, inStrip, passable, bounds, landAt, field, ROAD_GAP, BLEND, BEND, bent, paintedAt, paintedGrid, paintedIdAt, cover, extentOf, get built() { return built; } };
  if (typeof module !== "undefined" && module.exports) module.exports = root.ContinentRules;
})(globalThis);
