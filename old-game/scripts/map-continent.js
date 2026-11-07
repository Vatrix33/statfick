/* The continent's painted ground (port task 8; W12, W26–W29): everything the map draws besides the regions' own tiles.
   - The backdrop, on the continent's land grid (continent-shape.js, STEP units a pixel): each region's land in a colour
     from its own field (shown until its pictures are ready), the regions empty until the port as blank parchment (W29),
     and the coastline as a path; the sea is the canvas below it.
   - Overviews: a small picture of each region over its whole territory (its field sampled every STRIDE units,
     hillshaded, the continent's coast and sea, cut by its coverage), drawn when the map is zoomed out too far for its
     tiles, built a few rows at a time.
   - The fog over regions never entered (W12), along wandering lines rather than the territories' straight ones.
   PLACEHOLDER ART: the port remakes the continent as 3D terrain (W1). Loaded after map-terrain.js and map-regions.js. */
(() => {
  "use strict";
  const C = window.ContinentRules, rules = window.WorldRules;
  const smooth = (a, b, v) => { const t = Math.max(0, Math.min(1, (v - a) / (b - a))); return t * t * (3 - 2 * t); };
  const hash = (x, y, seed = 0) => { let h = Math.imul(x | 0, 374761393) ^ Math.imul(y | 0, 668265263) ^ Math.imul(seed | 0, 2147483647); h = Math.imul(h ^ (h >>> 13), 1274126177); return ((h ^ (h >>> 16)) >>> 0) / 4294967296; };
  // Smooth value noise in 0..1.
  function noise(x, y, seed = 0) {
    const xi = Math.floor(x), yi = Math.floor(y), fx = x - xi, fy = y - yi, u = fx * fx * (3 - 2 * fx), v = fy * fy * (3 - 2 * fy);
    const a = hash(xi, yi, seed), b = hash(xi + 1, yi, seed), c = hash(xi, yi + 1, seed), d = hash(xi + 1, yi + 1, seed);
    return a + (b - a) * u + (c - a) * v + (a - b - c + d) * u * v;
  }
  const fbm = (x, y, seed) => noise(x, y, seed) * 0.55 + noise(x * 2.1, y * 2.1, seed + 7) * 0.3 + noise(x * 4.3, y * 4.3, seed + 13) * 0.15;
  const regionOf = id => rules.getRegion(id);
  const uncharted = id => Boolean(regionOf(id)?.uncharted);
  const PARCHMENT = [226, 214, 186];

  /* ---------- A region's colour for its backdrop: its field's average over a coarse grid ---------- */
  const averages = new Map();
  function averageColor(id) {
    if (averages.has(id)) return averages.get(id);
    const sample = window.MapTerrain.sampler(regionOf(id));
    let color = [150, 150, 120];
    if (sample) {
      const sum = [0, 0, 0]; let n = 0;
      for (let y = 50; y < 1000; y += 100) for (let x = 50; x < 1600; x += 100) { const f = sample.fieldAt(x, y); if (f.water > 0.5) continue; sum[0] += f.color[0]; sum[1] += f.color[1]; sum[2] += f.color[2]; n++; }
      if (n) color = sum.map(v => v / n);
    }
    averages.set(id, color);
    return color;
  }

  /* ---------- The backdrop ---------- */
  let backdrop = null;
  function buildBackdrop() {
    // On the painted regions' grid (ContinentRules.paintedGrid: twice as coarse as the land's): the regions' own pictures
    // and the vector coastline carry the detail; this only fills land until they are ready, and the parchment.
    const s = C.field(), g = C.paintedGrid(), cols = g.cols, rows = g.rows, CELL = g.step;
    const canvas = document.createElement("canvas");
    canvas.width = cols; canvas.height = rows;
    const ctx = canvas.getContext("2d"), image = ctx.createImageData(cols, rows), data = image.data;
    for (let row = 0; row < rows; row++) for (let col = 0; col < cols; col++) {
      const i = row * cols + col, value = s.land[Math.min(s.rows - 1, row * 2) * s.cols + Math.min(s.cols - 1, col * 2)];
      if (value <= -4) continue;
      const id = C.IDS[g.owner[i]], v = 0.92 + noise(col / 14, row / 14, 31) * 0.1 + hash(col, row, 5) * 0.03;
      const color = (uncharted(id) ? PARCHMENT : averageColor(id)).map(k => k * v);
      data[i * 4] = color[0]; data[i * 4 + 1] = color[1]; data[i * 4 + 2] = color[2]; data[i * 4 + 3] = 255 * smooth(-4, 10, value);
    }
    ctx.putImageData(image, 0, 0);
    backdrop = { canvas, x0: g.x0, y0: g.y0, cols, rows, CELL, coast: coastPath(s.land, s.cols, s.rows, s.x0, s.y0, s.step) };
    return backdrop;
  }
  // The 0 contour of the land grid (marching squares), as one path in continent units.
  function coastPath(land, cols, rows, x0, y0, CELL) {
    const path = new Path2D(), at = (col, row) => land[row * cols + col];
    const lerp = (a, b) => (0 - a) / ((b - a) || 1e-6);
    for (let row = 0; row < rows - 1; row++) for (let col = 0; col < cols - 1; col++) {
      const a = at(col, row), b = at(col + 1, row), c = at(col + 1, row + 1), d = at(col, row + 1);
      const code = (a > 0 ? 8 : 0) | (b > 0 ? 4 : 0) | (c > 0 ? 2 : 0) | (d > 0 ? 1 : 0);
      if (code === 0 || code === 15) continue;
      const X = v => x0 + (col + v) * CELL, Y = v => y0 + (row + v) * CELL;
      const top = [X(lerp(a, b)), Y(0)], right = [X(1), Y(lerp(b, c))], bottom = [X(lerp(d, c)), Y(1)], left = [X(0), Y(lerp(a, d))];
      const segments = { 1: [[left, bottom]], 2: [[bottom, right]], 3: [[left, right]], 4: [[top, right]], 5: [[left, top], [bottom, right]], 6: [[top, bottom]], 7: [[left, top]],
        8: [[left, top]], 9: [[top, bottom]], 10: [[top, right], [left, bottom]], 11: [[top, right]], 12: [[left, right]], 13: [[bottom, right]], 14: [[left, bottom]] }[code];
      for (const [p, q] of segments) { path.moveTo(p[0], p[1]); path.lineTo(q[0], q[1]); }
    }
    return path;
  }

  /* ---------- Overviews: a region's whole territory, every STRIDE of its own units ---------- */
  const STRIDE = 8, SEA_FADE = 200;
  const overviews = new Map();
  function startOverview(id) {
    const region = regionOf(id), sample = window.MapTerrain.sampler(region), extent = C.extentOf(id, SEA_FADE);
    const OW = Math.round((extent.x1 - extent.x0) / STRIDE) + 1, OH = Math.round((extent.y1 - extent.y0) / STRIDE) + 1;
    const job = { id, region, sample, extent, OW, OH, row: 0, color: new Float32Array(OW * OH * 3), h: new Float32Array(OW * OH), water: new Float32Array(OW * OH), alpha: new Float32Array(OW * OH), canvas: null };
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
      const { region, extent, OW } = job, lake = region.lake;
      while (job.row < job.OH && performance.now() < end) {
        for (let col = 0; col < OW; col++) {
          const x = extent.x0 + col * STRIDE, y = extent.y0 + job.row * STRIDE, i = job.row * OW + col, c = C.toC(id, { x, y });
          const cover = C.cover(c, id), land = C.landAt(c);
          if (cover <= 0 || land <= -SEA_FADE) { job.alpha[i] = 0; job.water[i] = 1; continue; }
          const f = job.sample.fieldAt(x, y);
          job.color[i * 3] = f.color[0]; job.color[i * 3 + 1] = f.color[1]; job.color[i * 3 + 2] = f.color[2]; job.h[i] = f.h;
          const inLake = lake && ((x - lake.x) / lake.rx) ** 2 + ((y - lake.y) / lake.ry) ** 2 <= 1;
          job.water[i] = land <= 0 ? 0.5 + Math.min(0.5, -land / 240) : inLake || !region.landShape ? f.water : 0;
          job.alpha[i] = cover * (land <= 0 ? 1 - smooth(-SEA_FADE * 0.45, -SEA_FADE, land) : 1);
        }
        job.row++;
      }
      if (job.row < job.OH) return true;
      finishOverview(job);
      if (performance.now() >= end) return ids.some(other => !overviews.get(other)?.canvas);
    }
    return false;
  }
  function finishOverview(job) {
    const { OW, OH } = job, canvas = document.createElement("canvas");
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
      data[o] = r; data[o + 1] = g; data[o + 2] = b; data[o + 3] = 255 * Math.min(1, job.alpha[i]);
    }
    ctx.putImageData(image, 0, 0);
    job.canvas = canvas; job.color = job.h = job.water = job.alpha = null;
  }
  // A region's overview and where its top-left sample lies in its own units.
  const overview = id => { const job = overviews.get(id); return job?.canvas ? { canvas: job.canvas, x0: job.extent.x0, y0: job.extent.y0 } : null; };

  /* The fog over every territory never entered (W12), one soft image on the backdrop's grid: each cell is fogged when
     the region whose picture shows there (ContinentRules.paintedAt: the lines between regions wander) has not been
     entered, blurred so the fog thins out over about 250 units. Rebuilt when the entered regions change. */
  let fog = null;
  function fogImage(visited) {
    const key = [...visited].sort().join(",");
    if (fog?.key === key) return fog;
    const b = backdrop || buildBackdrop(), cols = b.cols, rows = b.rows, CELL = b.CELL;
    let mask = new Float32Array(cols * rows);
    const owner = C.paintedGrid().owner, seen = C.IDS.map(id => visited.includes(id));
    for (let i = 0; i < mask.length; i++) mask[i] = seen[owner[i]] ? 0 : 1;
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

  window.MapContinent = { STRIDE, backdrop: () => backdrop || buildBackdrop(), buildOverviews, overview, fogImage, noise, fbm, hash };
})();
