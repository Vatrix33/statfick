/* The continent (port task 8, Map Rework; decisions W1–W16 in engine/Docs/Engine-Plan.md, "Decisions for task 8").
   The 17 regions are placed whole on one continent, each enlarged 1.4× (W3), where their roads already point (W6): a
   region keeps its own coordinates ("local", 1600 × 1000 units) and this module places them in continent units
   ("C"), C = origin + local × SCALE. Between two regions a road joins lies a border strip (STRIP units, about 22 km)
   the road crosses with no prompt; where two regions touch with no road between them the ground is impassable (a
   ridge or the sea), so no new way opens between them. Every point of the continent belongs to the nearest region
   (its territory), so a strip is split down its middle and region rules take local points a little outside 0–1600.
   Pure rules, no page; loaded after world-region-data.js and before world-engine.js, which builds it from its
   regions (`build(regions)`). */
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
    { name: "Western Stoneveil Spurs", x: -1250, y: -600 },
    { name: "The Rainwall", x: 90, y: 1760 },
    { name: "Verdigris Range", x: -1934, y: 4500 },
    { name: "Glass Escarpment", x: 592, y: 7370 },
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
        // Each region's half of the crossing, in its own coordinates (beyond its edge to the strip's middle).
        halves: { [region.id]: [exit.point, toLocal(region.id, middle)], [exit.targetRegionId]: [back.point, toLocal(exit.targetRegionId, middle)] },
      });
    }
    built = { links, regions };
    return built;
  }
  const linksOf = regionId => (built?.links || []).filter(link => link.regions.includes(regionId));
  const linkBetween = (a, b) => (built?.links || []).find(link => link.regions.includes(a) && link.regions.includes(b)) || null;
  const inStrip = c => (built?.links || []).some(link => inRect(link.strip, c));
  // Whether a continent point can be stood on: a region's own land, or a strip between two regions a road joins.
  function passable(c, isLand) {
    const id = regionAt(c);
    if (inRect(RECTS[id], c)) return isLand(toLocal(id, c), id);
    return inStrip(c);
  }
  const bounds = () => {
    const xs = IDS.flatMap(id => [RECTS[id].x0, RECTS[id].x1]), ys = IDS.flatMap(id => [RECTS[id].y0, RECTS[id].y1]);
    return { x0: Math.min(...xs), y0: Math.min(...ys), x1: Math.max(...xs), y1: Math.max(...ys) };
  };
  root.ContinentRules = { SCALE, LW, LH, CW, CH, STRIP, STRIP_MARGIN, ORIGINS, RECTS, IDS, BARRIER_NAMES, toC, toLocal, inRect, regionAt, stripBetween, build, linksOf, linkBetween, inStrip, passable, bounds, get built() { return built; } };
  if (typeof module !== "undefined" && module.exports) module.exports = root.ContinentRules;
})(globalThis);
