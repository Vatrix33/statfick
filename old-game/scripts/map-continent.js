/* The continent's painted ground (port task 8, W1, W12): everything the map draws besides the regions' own tiles.
   - The backdrop: the ground between regions on a coarse grid (CELL continent units a pixel): border strips coloured
     from the two regions' edges, coasts beyond a region's land edge, the impassable ridges between neighbours no road
     joins (rock, with hatching), and the coastline as a path; the sea is the canvas below it.
   - Overviews: a small picture of each region (its field sampled every STRIDE units, hillshaded), drawn when the map is
     zoomed out too far for its tiles, built a few rows at a time.
   - The fog over regions never entered (W12).
   PLACEHOLDER ART: the port remakes the continent as 3D terrain (W1). Loaded after map-terrain.js and map-regions.js. */
(() => {
  "use strict";
  const C = window.ContinentRules, rules = window.WorldRules, S = C.SCALE;
  const smooth = (a, b, v) => { const t = Math.max(0, Math.min(1, (v - a) / (b - a))); return t * t * (3 - 2 * t); };
  const hash = (x, y, seed = 0) => { let h = Math.imul(x | 0, 374761393) ^ Math.imul(y | 0, 668265263) ^ Math.imul(seed | 0, 2147483647); h = Math.imul(h ^ (h >>> 13), 1274126177); return ((h ^ (h >>> 16)) >>> 0) / 4294967296; };
  // Smooth value noise in 0..1.
  function noise(x, y, seed = 0) {
    const xi = Math.floor(x), yi = Math.floor(y), fx = x - xi, fy = y - yi, u = fx * fx * (3 - 2 * fx), v = fy * fy * (3 - 2 * fy);
    const a = hash(xi, yi, seed), b = hash(xi + 1, yi, seed), c = hash(xi, yi + 1, seed), d = hash(xi + 1, yi + 1, seed);
    return a + (b - a) * u + (c - a) * v + (a - b - c + d) * u * v;
  }
  const fbm = (x, y, seed) => noise(x, y, seed) * 0.55 + noise(x * 2.1, y * 2.1, seed + 7) * 0.3 + noise(x * 4.3, y * 4.3, seed + 13) * 0.15;
  const MOUNTAINS = new Set(["high-stoneveil", "stoneveil-summit"]);
  const regionOf = id => rules.getRegion(id);
  const clampLocal = p => ({ x: Math.max(0, Math.min(1600, p.x)), y: Math.max(0, Math.min(1000, p.y)) });

  /* ---------- Edge colours: each region's field along its border, every 16 units ---------- */
  const edgeCache = new Map();
  function edgeColor(id, local) {
    let edges = edgeCache.get(id);
    if (!edges) {
      const sample = window.MapTerrain.sampler(regionOf(id));
      const at = (x, y) => { if (!sample) return [150, 140, 110]; const f = sample.fieldAt(x, y); return f.water > 0.5 ? null : [...f.color]; };
      edges = { top: [], bottom: [], left: [], right: [] };
      for (let x = 0; x <= 1600; x += 16) { edges.top.push(at(x, 2)); edges.bottom.push(at(x, 998)); }
      for (let y = 0; y <= 1000; y += 16) { edges.left.push(at(2, y)); edges.right.push(at(1598, y)); }
      edgeCache.set(id, edges);
    }
    const p = clampLocal(local);
    const sides = [["left", p.x, p.y], ["right", 1600 - p.x, p.y], ["top", p.y, p.x], ["bottom", 1000 - p.y, p.x]];
    const [side, , along] = sides.reduce((best, entry) => entry[1] < best[1] ? entry : best);
    const list = edges[side], i = Math.max(0, Math.min(list.length - 1, Math.round(along / 16)));
    return list[i] || list.find(Boolean) || [120, 140, 150];
  }

  /* ---------- The backdrop ---------- */
  const CELL = 40, MARGIN = 1400;
  let backdrop = null;
  function buildBackdrop() {
    const b = C.bounds(), x0 = b.x0 - MARGIN, y0 = b.y0 - MARGIN, cols = Math.ceil((b.x1 - b.x0 + 2 * MARGIN) / CELL) + 1, rows = Math.ceil((b.y1 - b.y0 + 2 * MARGIN) / CELL) + 1;
    const land = new Float32Array(cols * rows), ridge = new Uint8Array(cols * rows);
    const canvas = document.createElement("canvas");
    canvas.width = cols; canvas.height = rows;
    const ctx = canvas.getContext("2d"), image = ctx.createImageData(cols, rows), data = image.data;
    const ids = C.IDS, links = C.built?.links || [];
    const linked = (a, z) => links.some(link => link.regions.includes(a) && link.regions.includes(z));
    const rectDistance = (r, c) => Math.hypot(Math.max(r.x0 - c.x, 0, c.x - r.x1), Math.max(r.y0 - c.y, 0, c.y - r.y1));
    for (let row = 0; row < rows; row++) for (let col = 0; col < cols; col++) {
      const c = { x: x0 + col * CELL, y: y0 + row * CELL }, i = row * cols + col;
      // The two nearest regions.
      let first = null, d1 = Infinity, second = null, d2 = Infinity;
      for (const id of ids) {
        const d = rectDistance(C.RECTS[id], c);
        if (d < d1) { second = first; d2 = d1; first = id; d1 = d; } else if (d < d2) { second = id; d2 = d; }
      }
      if (d1 === 0) {
        // Inside a region: its own land (a lake counts as land here, so the coastline follows only the sea). Its land
        // gets its edge colour, so where its tiles fade out at the edge they fade into ground, never into sea.
        const local = C.toLocal(first, c), region = regionOf(first);
        const lake = region.lake && ((local.x - region.lake.x) / region.lake.rx) ** 2 + ((local.y - region.lake.y) / region.lake.ry) ** 2 <= 1;
        const own = rules.isLand(local, first);
        land[i] = lake || own ? 1 : 0;
        const edge = Math.min(local.x, local.y, 1600 - local.x, 1000 - local.y);
        if (own && !lake && edge < 140) {
          const color = edgeColor(first, local);
          data[i * 4] = color[0]; data[i * 4 + 1] = color[1]; data[i * 4 + 2] = color[2]; data[i * 4 + 3] = 255;
        }
        continue;
      }
      const n = fbm(c.x / 700, c.y / 700, 3);
      let value = 0, kind = 0;
      if (C.inStrip(c)) value = 1;
      // Between two close regions: a ridge if no road joins them, the shoulders of the pass if one does.
      else if (d2 < 680 && d1 + d2 < 1500 && second && (!linked(first, second) || d1 > 40)) { value = 1; kind = 1; }
      else if (first === "cinder-isle") value = 0;
      else {
        const edgeLand = rules.isLand(clampLocal(C.toLocal(first, c)), first);
        value = edgeLand ? 1 - smooth(110 + n * 140, 200 + n * 160, d1) : 0;
        if (MOUNTAINS.has(first)) kind = 1;
      }
      land[i] = value; ridge[i] = kind;
      if (value <= 0.02) continue;
      // Colour: the nearest region's edge, blended with the second's across a strip; rock on ridges.
      let color = edgeColor(first, C.toLocal(first, c));
      if (second && d2 < 400) {
        const other = edgeColor(second, C.toLocal(second, c)), w = d1 / (d1 + d2);
        color = color.map((v, k) => v * (1 - w) + other[k] * w);
      }
      if (kind) {
        const r = fbm(c.x / 160, c.y / 160, 11), shade = 0.72 + r * 0.5;
        color = [128 * shade, 116 * shade, 98 * shade];
      }
      const grain = 0.92 + hash(col, row, 5) * 0.12;
      data[i * 4] = color[0] * grain; data[i * 4 + 1] = color[1] * grain; data[i * 4 + 2] = color[2] * grain;
      data[i * 4 + 3] = 255 * smooth(0.02, 0.5, value);
    }
    ctx.putImageData(image, 0, 0);
    backdrop = { canvas, x0, y0, cols, rows, coast: coastPath(land, cols, rows, x0, y0), ridges: ridgeMarks(ridge, land, cols, rows, x0, y0) };
    return backdrop;
  }
  // The 0.5 contour of the land grid (marching squares), as one path in continent units.
  function coastPath(land, cols, rows, x0, y0) {
    const path = new Path2D(), at = (col, row) => land[row * cols + col];
    const lerp = (a, b) => (0.5 - a) / ((b - a) || 1e-6);
    for (let row = 0; row < rows - 1; row++) for (let col = 0; col < cols - 1; col++) {
      const a = at(col, row), b = at(col + 1, row), c = at(col + 1, row + 1), d = at(col, row + 1);
      const code = (a > 0.5 ? 8 : 0) | (b > 0.5 ? 4 : 0) | (c > 0.5 ? 2 : 0) | (d > 0.5 ? 1 : 0);
      if (code === 0 || code === 15) continue;
      const X = v => x0 + (col + v) * CELL, Y = v => y0 + (row + v) * CELL;
      const top = [X(lerp(a, b)), Y(0)], right = [X(1), Y(lerp(b, c))], bottom = [X(lerp(d, c)), Y(1)], left = [X(0), Y(lerp(a, d))];
      const segments = { 1: [[left, bottom]], 2: [[bottom, right]], 3: [[left, right]], 4: [[top, right]], 5: [[left, top], [bottom, right]], 6: [[top, bottom]], 7: [[left, top]],
        8: [[left, top]], 9: [[top, bottom]], 10: [[top, right], [left, bottom]], 11: [[top, right]], 12: [[left, right]], 13: [[bottom, right]], 14: [[left, bottom]] }[code];
      for (const [p, q] of segments) { path.moveTo(p[0], p[1]); path.lineTo(q[0], q[1]); }
    }
    return path;
  }
  // Hatch marks on the ridges: a chevron every few cells, jittered.
  function ridgeMarks(ridge, land, cols, rows, x0, y0) {
    const marks = [];
    for (let row = 1; row < rows - 1; row += 3) for (let col = 1; col < cols - 1; col += 3) {
      const i = row * cols + col;
      if (!ridge[i] || land[i] < 0.8) continue;
      marks.push({ x: x0 + (col + hash(col, row, 9)) * CELL, y: y0 + (row + hash(col, row, 10)) * CELL, s: 0.8 + hash(col, row, 11) * 0.6 });
    }
    return marks;
  }

  /* ---------- Overviews ---------- */
  const STRIDE = 8, OW = 1600 / STRIDE + 1, OH = 1000 / STRIDE + 1;
  const overviews = new Map();
  function startOverview(id) {
    const sample = window.MapTerrain.sampler(regionOf(id));
    const job = { id, sample, row: 0, color: new Float32Array(OW * OH * 3), h: new Float32Array(OW * OH), water: new Float32Array(OW * OH), canvas: null };
    overviews.set(id, job);
    return job;
  }
  // Builds overviews of these regions (nearest first) for up to `budget` milliseconds; true while work remains.
  function buildOverviews(ids, budget = 10) {
    const end = performance.now() + budget;
    for (const id of ids) {
      const job = overviews.get(id) || startOverview(id);
      if (job.canvas) continue;
      if (!job.sample) { job.canvas = document.createElement("canvas"); continue; }
      while (job.row < OH && performance.now() < end) {
        for (let col = 0; col < OW; col++) {
          const f = job.sample.fieldAt(col * STRIDE, job.row * STRIDE), i = job.row * OW + col;
          job.color[i * 3] = f.color[0]; job.color[i * 3 + 1] = f.color[1]; job.color[i * 3 + 2] = f.color[2]; job.h[i] = f.h; job.water[i] = f.water;
        }
        job.row++;
      }
      if (job.row < OH) return true;
      finishOverview(job);
      if (performance.now() >= end) return ids.some(other => !overviews.get(other)?.canvas);
    }
    return false;
  }
  function finishOverview(job) {
    const canvas = document.createElement("canvas");
    canvas.width = OW; canvas.height = OH;
    const ctx = canvas.getContext("2d"), image = ctx.createImageData(OW, OH), data = image.data;
    const spec = job.sample.spec, water = spec.water || { shallow: "#7f9c98", deep: "#3d5a60" };
    const shallow = window.MapTerrain.tone(water.shallow), deep = window.MapTerrain.tone(water.deep);
    const relief = typeof spec.relief === "function" ? spec.relief(0) : spec.relief ?? 1;
    for (let row = 0; row < OH; row++) for (let col = 0; col < OW; col++) {
      const i = row * OW + col, o = i * 4;
      const dx = (job.h[row * OW + Math.min(OW - 1, col + 1)] - job.h[row * OW + Math.max(0, col - 1)]) / (STRIDE * 2);
      const dy = (job.h[Math.min(OH - 1, row + 1) * OW + col] - job.h[Math.max(0, row - 1) * OW + col]) / (STRIDE * 2);
      const light = (dx * 0.7 + dy * 0.72) * relief * 0.016 * 1.15;
      let r = job.color[i * 3], g = job.color[i * 3 + 1], b = job.color[i * 3 + 2];
      if (job.water[i] > 0.5) { const depth = Math.min(1, (job.water[i] - 0.5) * 2.4); r = shallow[0] + (deep[0] - shallow[0]) * depth; g = shallow[1] + (deep[1] - shallow[1]) * depth; b = shallow[2] + (deep[2] - shallow[2]) * depth; }
      else { const k = 1 + light; r *= k; g *= k; b *= k; }
      const border = Math.min(col, row, OW - 1 - col, OH - 1 - row) * STRIDE, feather = window.MapTerrain.FEATHER;
      data[o] = r; data[o + 1] = g; data[o + 2] = b; data[o + 3] = border < feather ? 255 * smooth(0, feather, border) : 255;
    }
    ctx.putImageData(image, 0, 0);
    job.canvas = canvas; job.color = job.h = job.water = null;
  }
  const overview = id => overviews.get(id)?.canvas || null;

  /* The fog over every territory never entered (W12), one soft image on the backdrop's grid: each cell is fogged when
     the region it belongs to has not been entered, blurred so the fog thins out over about 250 units. Rebuilt when the
     entered regions change. */
  let fog = null;
  function fogImage(visited) {
    const key = [...visited].sort().join(",");
    if (fog?.key === key) return fog;
    const b = backdrop || buildBackdrop(), cols = b.cols, rows = b.rows;
    let mask = new Float32Array(cols * rows);
    for (let row = 0; row < rows; row++) for (let col = 0; col < cols; col++) mask[row * cols + col] = visited.includes(C.regionAt({ x: b.x0 + col * CELL, y: b.y0 + row * CELL })) ? 0 : 1;
    const blur = (source, radius) => {
      const out = new Float32Array(source.length), tmp = new Float32Array(source.length);
      for (let row = 0; row < rows; row++) { let sum = 0; for (let col = -radius; col <= radius; col++) sum += source[row * cols + Math.max(0, Math.min(cols - 1, col))]; for (let col = 0; col < cols; col++) { tmp[row * cols + col] = sum / (2 * radius + 1); sum += source[row * cols + Math.min(cols - 1, col + radius + 1)] - source[row * cols + Math.max(0, col - radius)]; } }
      for (let col = 0; col < cols; col++) { let sum = 0; for (let row = -radius; row <= radius; row++) sum += tmp[Math.max(0, Math.min(rows - 1, row)) * cols + col]; for (let row = 0; row < rows; row++) { out[row * cols + col] = sum / (2 * radius + 1); sum += tmp[Math.min(rows - 1, row + radius + 1) * cols + col] - tmp[Math.max(0, row - radius) * cols + col]; } }
      return out;
    };
    mask = blur(blur(mask, 3), 3);
    const canvas = document.createElement("canvas");
    canvas.width = cols; canvas.height = rows;
    const ctx = canvas.getContext("2d"), image = ctx.createImageData(cols, rows);
    for (let i = 0; i < mask.length; i++) {
      if (mask[i] <= 0.001) continue;
      const col = i % cols, row = (i / cols) | 0, v = 0.86 + noise(col / 6, row / 6, 21) * 0.12 + hash(col, row, 22) * 0.03;
      image.data[i * 4] = 214 * v; image.data[i * 4 + 1] = 202 * v; image.data[i * 4 + 2] = 176 * v; image.data[i * 4 + 3] = 255 * Math.min(1, mask[i] * 1.15);
    }
    ctx.putImageData(image, 0, 0);
    fog = { key, canvas };
    return fog;
  }

  window.MapContinent = { CELL, STRIDE, backdrop: () => backdrop || buildBackdrop(), buildOverviews, overview, fogImage, noise, fbm, hash };
})();
