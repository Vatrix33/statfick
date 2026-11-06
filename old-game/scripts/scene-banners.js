/* Interior scenes for the shops, the Magic Tower and the Adventurers Guild: ink-drawn
   rooms in the style of the battle sprites, lit only by their own fires, lamps and
   windows. Shops, the tower and the guild fill their whole window (1280 × 900). Castle
   cities get large, crowded halls; towns get small country rooms. Every scene is seeded
   by its settlement, so a town always shows the same room.
   The two guild interiors are PLACEHOLDER ART (port task 2, user decision G2): kept simple
   on purpose and listed in PLACEHOLDER_ART; the port remakes them fully as detailed
   illustrations (engine/Docs/Port-Tasks/README.md says what the remake keeps).
   A banner is two stacked SVGs: the static art (scene, darkness, hatching and
   lit glass) and a light layer of flames and glows that animates on its own; a scene
   with clickable objects (the guild) adds a third for their hover glow and labels. */
(() => {
  "use strict";
  const K = window.SpriteKit;
  if (!K) return;
  const { n, INK } = K;
  // The canvas of the scene being drawn; build() sets it per variant.
  let W = 1200, H = 250;
  const o2 = value => Math.round(value * 100) / 100;
  const cache = new Map();
  const BONE = "#e2d6bc";

  // Building stone, timber and cloth by region.
  const MATERIALS = {
    oak: { stone: "#857868", mortar: "#3b3029", wood: "#76522f", beam: "#4e3523", plaster: "#b6a586", floor: "#6c5a4a", cloth: "#7b2b25", trim: "#b08a40" },
    granite: { stone: "#767c84", mortar: "#30343a", wood: "#6e4f34", beam: "#45321f", plaster: "#a9a8a2", floor: "#62666c", cloth: "#2e4a6e", trim: "#a4a49a" },
    moss: { stone: "#6e7666", mortar: "#2e342a", wood: "#5e4c30", beam: "#3f3322", plaster: "#a2a486", floor: "#5a604c", cloth: "#3c5b3a", trim: "#9a9450" },
    sandstone: { stone: "#b3946a", mortar: "#5a4530", wood: "#80593a", beam: "#56391f", plaster: "#d0b488", floor: "#9a7c5a", cloth: "#9c4a22", trim: "#cc9c3e" },
    redrock: { stone: "#9c6652", mortar: "#4a2a20", wood: "#70492e", beam: "#4a2e1c", plaster: "#c4957c", floor: "#82543f", cloth: "#7a2a1a", trim: "#c48c3a" },
    limestone: { stone: "#aca698", mortar: "#55504a", wood: "#725d46", beam: "#4a3c2c", plaster: "#d0cab6", floor: "#8c8678", cloth: "#2f5c7e", trim: "#bca262" },
    bloodstone: { stone: "#684848", mortar: "#2a1a1a", wood: "#4e3229", beam: "#30201a", plaster: "#8e6c66", floor: "#503a38", cloth: "#6a1222", trim: "#a88a52" },
    basalt: { stone: "#534c48", mortar: "#1d1816", wood: "#5c402b", beam: "#35251a", plaster: "#82766a", floor: "#463e3a", cloth: "#80301a", trim: "#c4743a" },
    pale: { stone: "#8e97a2", mortar: "#3a4048", wood: "#6c5c4a", beam: "#42382c", plaster: "#bcc0c2", floor: "#6e747c", cloth: "#3a5a7a", trim: "#a8aeb4" },
  };
  const REGION_MATERIAL = {
    "stoneveil-foothills": "granite", "high-stoneveil": "granite", "stoneveil-summit": "granite",
    "gloamfen-marsh": "moss", "verdigris-jungle": "moss", "duskpine-forest": "moss",
    "saffron-expanse": "sandstone", "deep-desert": "sandstone", "crimson-divide": "redrock",
    "bloodstone-coast": "bloodstone", "cinder-isle": "basalt", "sunward-coast": "limestone", "mirrormere-basin": "pale",
  };
  const SKIES = {
    Daylight: { glass: "#d4dcd2", deep: "#a6b8ba", light: "#efe4c6", lit: true },
    Dawn: { glass: "#dcac84", deep: "#7e6e88", light: "#f2bc8e", lit: true },
    Dusk: { glass: "#dc905e", deep: "#5e4c70", light: "#f2a464", lit: true },
    Night: { glass: "#1f2c44", deep: "#121a2c", light: "#8ea6c8", lit: false },
  };
  const CLOTHES = ["#6b2a24", "#3d4a5c", "#4a5a3a", "#6a5440", "#5a4a6a", "#7a6a4a", "#3e3a36", "#8a7458", "#2f4a4a", "#7a4a2a"];
  const SKINS = ["#d4ab8a", "#c69c7a", "#a87656", "#8a5a3c", "#e2c2a2", "#6e4630"];
  const HAIRS = ["#2a1d14", "#4a3020", "#7a5530", "#a08a6a", "#c8c0b0", "#1a1414", "#8a3a1e"];
  const SPINES = ["#6b2a24", "#2e4a6e", "#4a5a3a", "#7a5a2a", "#5a3a5a", "#3a3a3a", "#8a6a3a", "#2a4a4a", "#7a3a2a"];
  const GOODS = ["#8a6a44", "#6a7a5a", "#9a8a6a", "#7a4a3a", "#5a6a7a", "#a08a5a", "#6a5a4a"];
  const GLASS = ["#b0382e", "#3456a0", "#d09a32", "#64409a", "#327e5c", "#b0382e", "#d09a32", "#3456a0"];
  const POTIONS = ["#e0503a", "#58b8f0", "#8ae070", "#c070f0", "#f0c040", "#f07ab0"];

  /* ---------- Small helpers ---------- */
  const rectD = (x, y, w, h) => `M${n(x)} ${n(y)}H${n(x + w)}V${n(y + h)}H${n(x)}Z`;
  const poly = points => "M" + points.map(([x, y]) => `${n(x)} ${n(y)}`).join("L") + "Z";
  const solid = (x, y, w, h, fill, extra = "") => `<rect x="${n(x)}" y="${n(y)}" width="${n(w)}" height="${n(h)}" fill="${K.paint(fill)}" ${extra}/>`;
  const block = (x, y, w, h, fill, extra = "") => K.path(rectD(x, y, w, h), fill, extra);
  const thin = 'stroke-width="1"';
  // A straight timber or bar of the given width between two points.
  function bar(x1, y1, x2, y2, width, fill, extra = "") {
    const dx = x2 - x1, dy = y2 - y1, length = Math.hypot(dx, dy) || 1, nx = -dy / length * width / 2, ny = dx / length * width / 2;
    return K.path(poly([[x1 + nx, y1 + ny], [x2 + nx, y2 + ny], [x2 - nx, y2 - ny], [x1 - nx, y1 - ny]]), fill, extra);
  }
  // Ink on the half of a shape turned away from the scene's key light.
  function sideShade(ctx, d, x, y, w, h, opacity = 0.5, fraction = 0.42) {
    const onLeft = x + w / 2 < ctx.keyX, sx = onLeft ? x - 2 : x + w * (1 - fraction);
    const edge = onLeft ? sx + w * fraction + 2 : sx - 4;
    return K.clipped(d, `<rect x="${n(sx)}" y="${n(y - 2)}" width="${n(w * fraction + 2)}" height="${n(h + 4)}" fill="${INK}" opacity="${opacity}"/>` + K.hatch(edge, y, 6, h, 72, 2.6, 0.5, 0.45));
  }
  // A pointed (Gothic) arch; ratio 1 is equilateral, 0.5 is round.
  function archPath(cx, base, w, h, ratio = 1) {
    const radius = w * ratio, rise = Math.sqrt(Math.max(0, radius * radius - (radius - w / 2) ** 2)), spring = base - h + rise;
    return `M${n(cx - w / 2)} ${n(base)}V${n(spring)}A${n(radius)} ${n(radius)} 0 0 1 ${n(cx)} ${n(base - h)}A${n(radius)} ${n(radius)} 0 0 1 ${n(cx + w / 2)} ${n(spring)}V${n(base)}Z`;
  }
  // Joints of the voussoirs around an arch, and of the jambs below it.
  function archJoints(cx, base, w, h, depth, ratio = 1, count = 4) {
    const radius = w * ratio, rise = Math.sqrt(Math.max(0, radius * radius - (radius - w / 2) ** 2)), spring = base - h + rise;
    let d = "";
    for (const side of [-1, 1]) {
      const centerX = cx - side * (radius - w / 2);
      const apex = Math.atan2(-rise, cx - centerX);
      const start = side < 0 ? Math.PI : 0, end = side < 0 ? apex + Math.PI * 2 : apex;
      for (let index = 1; index <= count; index++) {
        const angle = start + (end - start) * index / (count + 0.6), cos = Math.cos(angle), sin = Math.sin(angle);
        const x = centerX + cos * radius, y = spring + sin * radius;
        d += `M${n(x)} ${n(y)}L${n(x + cos * depth)} ${n(y + sin * depth)}`;
      }
      for (let y = spring + 20; y < base - 6; y += 22) d += `M${n(cx + side * w / 2)} ${n(y)}h${n(side * depth)}`;
    }
    return d;
  }

  /* ---------- Scene context and composition ---------- */
  function createContext(key, regionId, label) {
    const rng = K.seeded(key);
    const ctx = {
      id: "sb" + K.hash(key).toString(36), rng, keyX: W / 2, ambient: 0.68,
      mat: MATERIALS[REGION_MATERIAL[regionId]] || MATERIALS.oak,
      sky: SKIES[label] || SKIES.Daylight, night: label === "Night",
      lights: [], fx: [], glows: [], liveFx: [], gradients: new Map(), linears: new Map(),
      r: (a, b) => a + (b - a) * rng(),
      pick: list => list[Math.floor(rng() * list.length) % list.length],
      chance: p => rng() < p,
    };
    ctx.gradient = color => {
      if (!ctx.gradients.has(color)) ctx.gradients.set(color, `${ctx.id}g${ctx.gradients.size}`);
      return ctx.gradients.get(color);
    };
    ctx.linear = color => {
      if (!ctx.linears.has(color)) ctx.linears.set(color, `${ctx.id}l${ctx.linears.size}`);
      return ctx.linears.get(color);
    };
    ctx.light = (x, y, r, strength = 1, color = "#ffae5c") => ctx.lights.push({ x, y, r, strength, color });
    ctx.glow = (x, y, r, color, opacity = 0.7, cls = "") => ctx.glows.push(`<circle${cls ? ` class="${cls}"` : ""} cx="${n(x)}" cy="${n(y)}" r="${n(r)}" fill="url(#${ctx.gradient(color)})" opacity="${o2(opacity)}"/>`);
    ctx.emit = markup => ctx.fx.push(markup);
    ctx.live = markup => ctx.liveFx.push(markup);
    return ctx;
  }

  function compose(ctx, scene, clips) {
    const id = ctx.id;
    const warmth = ctx.lights.map(light => `<circle cx="${n(light.x)}" cy="${n(light.y)}" r="${n(light.r * 0.8)}" fill="url(#${ctx.gradient(light.color)})" opacity="${o2(0.4 * Math.min(1, light.strength))}"/>`).join("");
    const holes = ctx.lights.map(light => `<circle cx="${n(light.x)}" cy="${n(light.y)}" r="${n(light.r)}" fill="url(#${id}h)" opacity="${o2(Math.min(1, light.strength))}"/>`).join("");
    const radial = [...ctx.gradients].map(([color, gid]) => `<radialGradient id="${gid}"><stop offset="0" stop-color="${color}"/><stop offset=".42" stop-color="${color}" stop-opacity=".42"/><stop offset="1" stop-color="${color}" stop-opacity="0"/></radialGradient>`).join("");
    const linear = [...ctx.linears].map(([color, gid]) => `<linearGradient id="${gid}" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="${color}" stop-opacity=".9"/><stop offset="1" stop-color="${color}" stop-opacity="0"/></linearGradient>`).join("");
    const defs = `${clips}<defs>${radial}${linear}`
      + `<radialGradient id="${id}h"><stop offset="0" stop-color="#000"/><stop offset=".5" stop-color="#000" stop-opacity=".72"/><stop offset="1" stop-color="#000" stop-opacity="0"/></radialGradient>`
      + `<radialGradient id="${id}v" cx=".5" cy=".48" r=".6"><stop offset=".6" stop-color="${INK}" stop-opacity="0"/><stop offset="1" stop-color="${INK}" stop-opacity=".9"/></radialGradient>`
      + `<mask id="${id}m" maskUnits="userSpaceOnUse" x="0" y="0" width="${W}" height="${H}"><rect width="${W}" height="${H}" fill="#fff"/>${holes}</mask></defs>`;
    // Full-window scenes keep their top edge, where the faces and counters are, in view.
    const frame = `viewBox="0 0 ${W} ${H}" preserveAspectRatio="${H > 400 ? "xMidYMin" : "xMidYMid"} slice" aria-hidden="true" focusable="false"`;
    const art = `<svg class="sb-art" ${frame}>${defs}<g>${scene}</g>`
      + `<rect width="${W}" height="${H}" fill="${INK}" opacity="${o2(ctx.ambient)}" mask="url(#${id}m)"/>`
      + `<g mask="url(#${id}m)" opacity=".38">${K.hatch(0, 0, W, H, 58, 4.6, 0.8, 1)}</g>`
      + `<g style="mix-blend-mode:overlay">${warmth}</g>${ctx.fx.join("")}`
      + `<rect width="${W}" height="${H}" fill="url(#${id}v)"/></svg>`;
    const live = `<svg class="sb-live" ${frame}>${ctx.glows.join("")}${ctx.liveFx.join("")}</svg>`;
    // Clickable objects (the guild's): a hit box, a copy of the object that shows only its glowing
    // outline on hover or focus, and its name. Above the darkness, so the glow is never dimmed.
    const spots = ctx.spots?.length ? `<svg class="sb-spots" viewBox="0 0 ${W} ${H}" preserveAspectRatio="${H > 400 ? "xMidYMin" : "xMidYMid"} slice">`
      + `<defs><filter id="${id}glow" x="-15%" y="-15%" width="130%" height="130%" color-interpolation-filters="sRGB"><feMorphology in="SourceAlpha" operator="dilate" radius="2.5" result="grown"/><feComposite in="grown" in2="SourceAlpha" operator="out" result="ring"/><feGaussianBlur in="ring" stdDeviation="2.2" result="soft"/><feFlood flood-color="#ffd27a"/><feComposite in2="soft" operator="in" result="glow"/><feMerge><feMergeNode in="glow"/><feMergeNode in="glow"/></feMerge></filter></defs>`
      + ctx.spots.map(({ id: spotId, label, box: [x, y, w, h], markup }) => {
        const labelWidth = label.length * 8.4 + 22, lx = x + w / 2, ly = Math.max(20, y - 8);
        return `<g class="scene-spot" data-guild-spot="${spotId}" role="button" tabindex="0" aria-label="${label}">`
          + `<rect class="scene-spot-hit" x="${n(x)}" y="${n(y)}" width="${n(w)}" height="${n(h)}" fill="transparent"/>`
          + `<g class="scene-spot-glow" filter="url(#${id}glow)" pointer-events="none">${markup}</g>`
          + `<g class="scene-spot-label" pointer-events="none" transform="translate(${n(lx)} ${n(ly)})"><rect x="${n(-labelWidth / 2)}" y="-17" width="${n(labelWidth)}" height="24" rx="4"/><text y="0" text-anchor="middle">${label}</text></g></g>`;
      }).join("") + "</svg>" : "";
    // Clip paths are named by geometry; a banner-specific prefix keeps them apart from the sprites'.
    return (art + live + spots).replaceAll('id="ink', `id="${id}ink`).replaceAll("url(#ink", `url(#${id}ink`);
  }

  /* ---------- Walls, floors and ceilings ---------- */
  function masonry(ctx, x, y, w, h, color, { row = 24, min = 1.4, max = 2.7, mortar = ctx.mat.mortar, regular = false } = {}) {
    let tint = "", joints = "", bevel = "", cracks = "";
    for (let top = y, index = 0; top < y + h; top += row, index++) {
      const bottom = Math.min(top + row, y + h);
      if (top > y) joints += `M${n(x)} ${n(top)}H${n(x + w)}`;
      bevel += `M${n(x)} ${n(top + 1.8)}H${n(x + w)}`;
      let left = regular ? x - (index % 2 ? row * min / 2 : 0) : x - ctx.r(0, row * max);
      while (left < x + w) {
        const width = regular ? row * min : ctx.r(row * min, row * max), shade = ctx.r(-1, 1);
        if (left > x) joints += `M${n(left)} ${n(top)}V${n(bottom)}`;
        if (Math.abs(shade) > 0.35) tint += solid(left, top, width, bottom - top, shade > 0 ? "!#fff2d8" : "!" + INK, `opacity="${o2((Math.abs(shade) - 0.25) * 0.17)}"`);
        if (!regular && ctx.chance(0.1)) {
          const cx = left + ctx.r(4, Math.max(5, width - 4));
          cracks += `M${n(cx)} ${n(top + 2)}l${n(ctx.r(-4, 4))} ${n(ctx.r(4, row * 0.6))}l${n(ctx.r(-3, 3))} ${n(ctx.r(2, 6))}`;
        }
        left += width;
      }
    }
    return K.clipped(rectD(x, y, w, h), solid(x, y, w, h, color) + tint
      + `<path d="${bevel}" stroke="#fff2d8" stroke-width="1" opacity=".09" fill="none"/>`
      + `<path d="${joints}" stroke="${K.paint(mortar)}" stroke-width="${row < 14 ? 1.2 : 1.8}" fill="none"/>`
      + (cracks ? `<path d="${cracks}" stroke="${INK}" stroke-width=".8" opacity=".55" fill="none"/>` : ""));
  }

  function boards(ctx, x, y, w, h, color, { width = 30, horizontal = false } = {}) {
    let tint = "", seams = "", grain = "";
    const span = horizontal ? h : w, along = horizontal ? w : h;
    for (let at = 0; at < span; at += width) {
      const shade = ctx.r(-1, 1), size = Math.min(width, span - at);
      const [rx, ry, rw, rh] = horizontal ? [x, y + at, w, size] : [x + at, y, size, h];
      if (Math.abs(shade) > 0.3) tint += solid(rx, ry, rw, rh, shade > 0 ? "!#fff2d8" : "!" + INK, `opacity="${o2((Math.abs(shade) - 0.2) * 0.15)}"`);
      if (at > 0) seams += horizontal ? `M${n(x)} ${n(y + at)}H${n(x + w)}` : `M${n(x + at)} ${n(y)}V${n(y + h)}`;
      for (let line = 0; line < 2; line++) {
        const offset = at + ctx.r(3, Math.max(4, size - 3)), start = ctx.r(0, along * 0.5), length = ctx.r(along * 0.2, along * 0.5);
        grain += horizontal ? `M${n(x + start)} ${n(y + offset)}q${n(length / 2)} ${n(ctx.r(-2, 2))} ${n(length)} 0` : `M${n(x + offset)} ${n(y + start)}q${n(ctx.r(-2, 2))} ${n(length / 2)} 0 ${n(length)}`;
      }
    }
    return K.clipped(rectD(x, y, w, h), solid(x, y, w, h, color) + tint
      + `<path d="${grain}" stroke="${INK}" stroke-width=".8" opacity=".3" fill="none"/>`
      + `<path d="${seams}" stroke="${INK}" stroke-width="1.6" opacity=".8" fill="none"/>`);
  }

  function plaster(ctx, x, y, w, h, color) {
    let stains = "", cracks = "";
    for (let index = 0; index < Math.round(w * h / 6000); index++) {
      stains += `<ellipse cx="${n(x + ctx.r(0, w))}" cy="${n(y + ctx.r(0, h))}" rx="${n(ctx.r(14, 64))}" ry="${n(ctx.r(8, 30))}" fill="${ctx.chance(0.75) ? INK : "#fff2d8"}" opacity="${o2(ctx.r(0.03, 0.09))}"/>`;
      if (ctx.chance(0.25)) { const cx = x + ctx.r(0, w), cy = y + ctx.r(0, h); cracks += `M${n(cx)} ${n(cy)}l${n(ctx.r(-8, 8))} ${n(ctx.r(5, 14))}l${n(ctx.r(-6, 6))} ${n(ctx.r(4, 10))}`; }
    }
    return K.clipped(rectD(x, y, w, h), solid(x, y, w, h, color) + stains + (cracks ? `<path d="${cracks}" stroke="${INK}" stroke-width=".8" opacity=".45" fill="none"/>` : ""));
  }

  // Plaster panels in a frame of posts, a rail and braces.
  function timberWall(ctx, x, y, w, h, spacing = 210) {
    const wood = ctx.mat.beam, rail = y + h * 0.5;
    let out = plaster(ctx, x, y, w, h, ctx.mat.plaster), frame = "";
    for (let px = x + spacing * 0.5, index = 0; px < x + w + spacing; px += spacing, index++) {
      if (index % 2 === 0) frame += bar(px - spacing + 14, rail - 4, px - 10, y + 6, 9, wood, thin);
      else frame += bar(px - spacing + 10, y + 6, px - 14, rail - 4, 9, wood, thin);
    }
    frame += bar(x - 4, rail, x + w + 4, rail, 11, wood, thin);
    for (let px = x + spacing * 0.5; px < x + w + spacing; px += spacing) frame += block(px - 7, y, 14, h, wood, thin);
    return out + frame + solid(x, y, w, 8, "!" + INK, 'opacity=".35"');
  }

  function floor(ctx, top, color, kind = "flag", vx = W / 2) {
    const vy = top - 240, depth = H - top;
    const xAt = (bx, y) => vx + (bx - vx) * (y - vy) / (H - vy);
    let lines = "", stains = "";
    if (kind === "flag") {
      const rows = [top];
      for (let index = 1; index <= 4; index++) rows.push(top + depth * Math.pow(index / 4.4, 1.35));
      rows.push(H);
      for (let index = 1; index < rows.length - 1; index++) lines += `M0 ${n(rows[index])}H${W}`;
      for (let row = 0; row < rows.length - 1; row++) {
        for (let bx = -900 + (row % 2) * 52; bx <= 2100; bx += 104) lines += `M${n(xAt(bx, rows[row]))} ${n(rows[row])}L${n(xAt(bx, rows[row + 1]))} ${n(rows[row + 1])}`;
      }
    } else if (kind === "plank") {
      for (let bx = -900; bx <= 2100; bx += 46) {
        lines += `M${n(xAt(bx, top))} ${n(top)}L${n(bx)} ${H}`;
        const y = top + ctx.r(0.15, 0.9) * depth;
        lines += `M${n(xAt(bx, y))} ${n(y)}L${n(xAt(bx + 46, y))} ${n(y)}`;
      }
    }
    for (let index = 0; index < 34; index++) stains += `<ellipse cx="${n(ctx.r(0, W))}" cy="${n(top + ctx.r(4, depth))}" rx="${n(ctx.r(10, 50))}" ry="${n(ctx.r(2, 7))}" fill="${INK}" opacity="${o2(ctx.r(0.05, 0.14))}"/>`;
    return K.clipped(rectD(0, top, W, depth), solid(0, top, W, depth, color) + stains
      + `<path d="${lines}" stroke="${INK}" stroke-width="1.4" opacity=".7" fill="none"/>`
      + solid(0, top, W, 12, "!" + INK, 'opacity=".45"') + K.hatch(0, top, W, 12, 70, 3, 0.6, 0.5))
      + K.line(`M0 ${n(top)}H${W}`, "!" + INK, 2.4);
  }

  // A low timber ceiling: joists running toward the viewer and one heavy beam.
  function ceiling(ctx, beamY, vx = W / 2) {
    const wood = ctx.mat.beam;
    let joists = "";
    for (let bx = -60; bx <= W + 60; bx += 120) {
      const tx = vx + (bx - vx) * 1.5, d = poly([[bx - 8, beamY], [bx + 8, beamY], [tx + 13, -2], [tx - 13, -2]]);
      joists += K.path(d, wood, 'stroke-width="1.4"') + K.clipped(d, K.hatch(Math.min(bx, tx) - 14, 0, Math.abs(tx - bx) + 28, beamY, 70, 2.6, 0.5, 0.45));
    }
    return solid(0, 0, W, beamY, K.darken(wood, 0.55)) + K.hatch(0, 0, W, beamY, 20, 3.4, 0.6, 0.5) + joists
      + block(-4, beamY, W + 8, 16, wood) + solid(0, beamY + 16, W, 7, "!" + INK, 'opacity=".45"')
      + K.clipped(rectD(0, beamY, W, 16), K.hatch(0, beamY + 9, W, 7, 75, 2.8, 0.5, 0.5));
  }

  function post(ctx, x, top, bottom, w = 18) {
    const d = rectD(x - w / 2, top, w, bottom - top), wood = ctx.mat.beam;
    return bar(x - w / 2 - 34, top + 2, x - w / 2 + 2, top + 36, 10, wood, thin) + bar(x + w / 2 + 34, top + 2, x + w / 2 - 2, top + 36, 10, wood, thin)
      + K.path(d, wood) + sideShade(ctx, d, x - w / 2, top, w, bottom - top, 0.5);
  }

  function pillar(ctx, x, top, bottom, w, stone = ctx.mat.stone) {
    const d = rectD(x - w / 2, top, w, bottom - top);
    return K.path(d, stone) + K.clipped(d, K.line(`M${n(x - w * 0.18)} ${n(top)}V${n(bottom)}M${n(x + w * 0.18)} ${n(top)}V${n(bottom)}`, "!" + INK, 1, 'opacity=".4"'))
      + sideShade(ctx, d, x - w / 2, top, w, bottom - top, 0.55)
      + block(x - w / 2 - 7, bottom - 14, w + 14, 14, stone) + block(x - w / 2 - 4, bottom - 22, w + 8, 8, stone, thin);
  }

  // An arched opening into a deeper, darker room; `inside` is drawn within it.
  function opening(ctx, cx, base, w, h, frame, inside = "", { far = ctx.mat.stone, ratio = 1 } = {}) {
    const inner = archPath(cx, base, w, h, ratio), outer = archPath(cx, base, w + 28, h + 16, ratio);
    const room = masonry(ctx, cx - w / 2, base - h, w, h, K.darken(far, 0.5), { row: 15 })
      + solid(cx - w / 2, base - 16, w, 16, K.darken(ctx.mat.floor, 0.45)) + K.line(`M${n(cx - w / 2)} ${n(base - 16)}h${n(w)}`, "!" + INK, 1.4);
    return K.path(outer, K.lighten(frame, 0.05)) + K.line(archJoints(cx, base, w, h, 14, ratio), "!" + INK, 1.2, 'opacity=".7"')
      + K.clipped(inner, room + inside + `<rect x="${n(cx - w / 2)}" y="${n(base - h)}" width="${n(w)}" height="${n(h)}" fill="${INK}" opacity=".25"/>` + K.hatch(cx - w / 2, base - h, w, h * 0.45, 60, 3, 0.6, 0.5))
      + K.path(inner, "none", `stroke="${K.OUTLINE}" stroke-width="2.4" fill="none"`);
  }

  // Glass that shows the sky; it glows by day and holds the moon at night.
  function windowPane(ctx, x, y, w, h, { arch = "flat", frame = ctx.mat.beam, bars = 1 } = {}) {
    const glassD = arch === "point" ? archPath(x + w / 2, y + h, w, h) : arch === "round" ? archPath(x + w / 2, y + h, w, h, 0.5) : rectD(x, y, w, h);
    const sky = ctx.sky;
    let glass = K.path(glassD, "!" + sky.glass, 'stroke="none"') + K.clipped(glassD, `<rect x="${n(x)}" y="${n(y)}" width="${n(w)}" height="${n(h * 0.45)}" fill="${sky.deep}" opacity=".45"/>`
      + (ctx.night ? `<circle cx="${n(x + w * 0.68)}" cy="${n(y + h * 0.3)}" r="${n(Math.min(w, h) * 0.13)}" fill="#e8e4d0" opacity=".85"/>` : `<ellipse cx="${n(x + w * 0.3)}" cy="${n(y + h * 0.35)}" rx="${n(w * 0.3)}" ry="${n(h * 0.06)}" fill="#fff" opacity=".35"/>`)
      + K.hatch(x, y, w, h, 60, 9, 0.9, 0.5, "#3a3028") + K.hatch(x, y, w, h, 120, 9, 0.9, 0.5, "#3a3028"));
    let mullions = "";
    for (let index = 1; index <= bars; index++) mullions += `M${n(x + w * index / (bars + 1))} ${n(y)}V${n(y + h)}`;
    mullions += `M${n(x)} ${n(y + h * 0.55)}H${n(x + w)}`;
    glass += K.clipped(glassD, `<path d="${mullions}" stroke="${K.paint(frame)}" stroke-width="3.4" fill="none"/>`);
    const surround = K.path(glassD, "none", `stroke="${K.paint(frame)}" stroke-width="7" fill="none"`) + K.path(glassD, "none", `stroke="${K.OUTLINE}" stroke-width="1.6" fill="none"`);
    const sill = block(x - 6, y + h, w + 12, 6, frame, thin);
    if (sky.lit) {
      ctx.emit(glass + surround + sill);
      ctx.light(x + w / 2, y + h / 2, Math.max(w, h) * 2, 0.75, sky.light);
      ctx.glow(x + w / 2, y + h / 2, Math.max(w, h) * 0.9, sky.light, 0.32);
      return "";
    }
    ctx.glow(x + w / 2, y + h / 2, Math.max(w, h) * 0.7, sky.light, 0.18);
    return glass + surround + sill;
  }

  // A beam of window light falling across the room.
  function shaft(ctx, points, opacity = 0.2) {
    if (!ctx.sky.lit) return;
    ctx.glows.push(`<path d="${poly(points)}" fill="url(#${ctx.linear(ctx.sky.light)})" opacity="${o2(opacity)}"/>`);
  }

  /* ---------- Fire and light ---------- */
  function flamePath(ctx, cx, base, w, h, count = 5) {
    const left = cx - w / 2, step = w / count;
    let d = `M${n(left)} ${n(base)}`;
    for (let index = 0; index < count; index++) {
      const weight = 1 - Math.abs((index + 0.5) / count - 0.5) * 1.4;
      const tipX = left + step * (index + 0.5) + ctx.r(-0.2, 0.2) * step, tipY = base - h * (0.4 + 0.6 * weight) * ctx.r(0.75, 1);
      const valleyX = left + step * (index + 1), valleyY = index === count - 1 ? base : base - h * ctx.r(0.15, 0.32) * weight;
      const lean = ctx.r(-0.35, 0.35) * step, rise = base - tipY;
      d += `C${n(left + step * index + step * 0.1)} ${n(base - rise * 0.45)} ${n(tipX - step * 0.5 - lean)} ${n(tipY + rise * 0.3)} ${n(tipX + lean)} ${n(tipY)}`
        + `C${n(tipX + step * 0.05)} ${n(tipY + rise * 0.28)} ${n(valleyX - step * 0.2)} ${n(valleyY - rise * 0.12)} ${n(valleyX)} ${n(valleyY)}`;
    }
    return d + "Z";
  }
  function fire(ctx, cx, base, w, h, { radius, strength = 1 } = {}) {
    const outer = flamePath(ctx, cx, base, w, h, 5), middle = flamePath(ctx, cx, base, w * 0.7, h * 0.72, 4), core = flamePath(ctx, cx, base, w * 0.38, h * 0.42, 3);
    ctx.live(`<g class="sb-flicker"><path d="${outer}" fill="#c84a1c"/><path d="${middle}" fill="#f2922e"/><path d="${core}" fill="#ffe4a4"/></g>`);
    ctx.emit(`<ellipse cx="${n(cx)}" cy="${n(base)}" rx="${n(w * 0.55)}" ry="${n(Math.max(2, h * 0.08))}" fill="#e0602a" opacity=".85"/>`);
    ctx.glow(cx, base - h * 0.4, w * 1.2 + h, "#ff8a30", 0.6, "sb-breathe");
    ctx.light(cx, base - h * 0.45, radius || w * 2.4 + h * 2.2, strength, "#ff9a40");
  }
  function candle(ctx, x, base, h = 12, w = 4.5, light = 0.45) {
    const tip = base - h, size = Math.max(4, w * 1.4);
    ctx.live(`<path class="sb-flicker" d="M${n(x)} ${n(tip - size * 1.8)}C${n(x + size * 0.7)} ${n(tip - size)} ${n(x + size * 0.55)} ${n(tip)} ${n(x)} ${n(tip)}C${n(x - size * 0.55)} ${n(tip)} ${n(x - size * 0.7)} ${n(tip - size)} ${n(x)} ${n(tip - size * 1.8)}Z" fill="#ffd98a"/>`);
    ctx.glow(x, tip - size, size * 4, "#ffb050", 0.7);
    if (light) ctx.light(x, tip - size, 40 + size * 12, light);
    return block(x - w / 2, tip, w, h, "#e0d4b4", 'stroke-width=".9"');
  }
  function sparks(ctx, cx, cy, count = 14, spread = 60) {
    let d = "", dots = "";
    for (let index = 0; index < count; index++) {
      const angle = ctx.r(-Math.PI * 0.95, -Math.PI * 0.05), near = ctx.r(4, 12), far = near + ctx.r(5, spread * 0.5);
      d += `M${n(cx + Math.cos(angle) * near)} ${n(cy + Math.sin(angle) * near)}L${n(cx + Math.cos(angle) * far)} ${n(cy + Math.sin(angle) * far)}`;
      if (ctx.chance(0.6)) { const reach = far + ctx.r(4, spread * 0.4); dots += `<circle cx="${n(cx + Math.cos(angle) * reach)}" cy="${n(cy + Math.sin(angle) * reach + ctx.r(0, 8))}" r="1.1" fill="#ffc060"/>`; }
    }
    ctx.live(`<g class="sb-sparks"><path d="${d}" stroke="#ffd27a" stroke-width="1.4" stroke-linecap="round"/>${dots}</g>`);
    ctx.glow(cx, cy, spread * 0.6, "#ffb050", 0.6, "sb-breathe");
    ctx.light(cx, cy, spread * 2.4, 0.55, "#ffb050");
  }
  function steam(ctx, cx, top, count = 3, spread = 30) {
    let wisps = "";
    for (let index = 0; index < count; index++) {
      const x = cx + ctx.r(-spread, spread), length = ctx.r(26, 50);
      wisps += `<path class="sb-steam" style="animation-delay:-${o2(ctx.r(0, 5))}s" d="M${n(x)} ${n(top)}c${n(ctx.r(-8, 8))} -${n(length * 0.3)} ${n(ctx.r(-8, 8))} -${n(length * 0.6)} ${n(ctx.r(-6, 6))} -${n(length)}" stroke="#d8d2c6" stroke-width="${n(ctx.r(4, 8))}" stroke-linecap="round" fill="none"/>`;
    }
    ctx.live(wisps);
  }
  function lantern(ctx, x, top, y, s = 1) {
    const chain = K.line(`M${n(x)} ${n(top)}V${n(y - 12 * s)}`, "#2a2420", 1.4);
    ctx.emit(K.path(`M${n(x - 7 * s)} ${n(y - 8 * s)}L${n(x)} ${n(y - 14 * s)}L${n(x + 7 * s)} ${n(y - 8 * s)}Z`, "#2e2a26", thin)
      + solid(x - 5.5 * s, y - 8 * s, 11 * s, 14 * s, "!#ffcc78")
      + K.line(`M${n(x - 5.5 * s)} ${n(y - 8 * s)}v${n(14 * s)}M${n(x)} ${n(y - 8 * s)}v${n(14 * s)}M${n(x + 5.5 * s)} ${n(y - 8 * s)}v${n(14 * s)}`, "!#2a2420", 1.3)
      + block(x - 7 * s, y + 6 * s, 14 * s, 3 * s, "#2e2a26", thin));
    ctx.glow(x, y, 48 * s, "#ffb050", 0.75, "sb-breathe");
    ctx.light(x, y, 210 * s, 0.9);
    return chain;
  }
  function chandelier(ctx, x, top, y, w, count = 6) {
    const hook = y - w * 0.55;
    let out = K.line(`M${n(x)} ${n(top)}V${n(hook)}M${n(x)} ${n(hook)}L${n(x - w / 2)} ${n(y)}M${n(x)} ${n(hook)}L${n(x + w / 2)} ${n(y)}M${n(x)} ${n(hook)}V${n(y + w * 0.12)}`, "#2a2420", 1.3);
    out += K.ellipse(x, y, w / 2, w * 0.12, "none", `stroke="${INK}" stroke-width="${n(Math.max(2.4, w * 0.05))}" fill="none"`);
    for (let index = 0; index < count; index++) {
      const angle = Math.PI * (index + 0.5) / count;
      out += candle(ctx, x + Math.cos(angle) * w / 2, y + Math.sin(angle) * w * 0.12, Math.max(5, w * 0.08), Math.max(2.4, w * 0.035), 0);
    }
    ctx.light(x, y, w * 2.8, 0.95);
    return out;
  }

  /* ---------- People ---------- */
  const POSES = {
    stand: { back: [-12, -47], front: [12, -47] },
    hammer: { back: [17, -55], front: [8, -106], frontElbow: [20, -92], tool: [-0.55, -0.83] },
    strike: { back: [17, -55], front: [27, -60], frontElbow: [21, -77], tool: [0.25, 0.97] },
    work: { back: [17, -56], front: [22, -55] },
    counter: { back: [11, -51], front: [20, -52] },
    carry: { back: [12, -63], front: [20, -63] },
    point: { back: [-12, -47], front: [32, -88], frontElbow: [21, -81] },
    drink: { back: [-12, -48], front: [9, -84], frontElbow: [19, -68] },
    toast: { back: [-11, -50], front: [20, -104], frontElbow: [23, -86] },
    read: { back: [12, -62], front: [18, -63] },
    cast: { back: [16, -64], front: [26, -101], frontElbow: [24, -85] },
    stir: { back: [15, -62], front: [25, -58], tool: [0.6, 0.8] },
    inspect: { back: [14, -60], front: [22, -76], frontElbow: [21, -63], tool: [0.42, -0.91] },
    fold: { back: [9, -60], front: [-4, -59], frontElbow: [12, -62] },
    reach: { back: [14, -58], front: [17, -113], frontElbow: [18, -96] },
    wipe: { back: [13, -63], front: [19, -61] },
    hold: { back: [-12, -47], front: [15, -54] },
  };
  /* A figure drawn facing right with its feet at (x, base); h is its standing height.
     Options: face, pose, color, skin, hair, head (short, long, bald, hood, wide,
     witch, helm, cap, coif, kerchief), beard, robe, apron, armor, tabard, cloak,
     pack, sword (on the back), seated, walk, back (seen from behind) and hold. */
  function person(ctx, x, base, h, o = {}) {
    const f = o.face || 1, s = h / 100, pose = POSES[o.pose] || POSES.stand;
    const cloth = o.color || ctx.pick(CLOTHES), skin = o.skin || ctx.pick(SKINS), hair = o.hair || ctx.pick(HAIRS);
    const trousers = o.legs || K.darken(cloth, 0.4), sleeve = o.armor ? "#7c8086" : (o.sleeve || cloth), boot = "#2e231b", metal = "#8a8e94";
    const away = (x < ctx.keyX ? -1 : 1) * f, lift = o.seated ? 22 : 0;
    const world = (lx, ly) => [x + f * s * lx, base + s * (ly + lift)];
    return K.withPen(1.6 / Math.sqrt(s), () => {
      const shadeHalf = (d, top, bottom) => K.clipped(d, `<rect x="${away < 0 ? -26 : 1}" y="${top}" width="25" height="${bottom - top}" fill="${INK}" opacity=".55"/>`);
      const arm = (sx, sy, [hx, hy], elbow, color) => {
        const [ex, ey] = elbow || [(sx + hx) / 2 + (sx < 0 ? -3 : 3), (sy + hy) / 2 + 3];
        return K.path(K.taper(sx, sy, ex, ey, 9, 7.6), color) + K.path(K.taper(ex, ey, hx, hy, 7.6, 6.2), color) + K.circle(hx, hy, 3.5, skin);
      };
      let back = "", lower = "", torso = "", head = "", front = "", item = "";
      const [hx, hy] = pose.front, [bx, by] = pose.back;

      if (o.cloak) { const d = "M-11 -80C-21 -56-25 -27-24 -3L1 -3C-1 -30-1 -60 3 -80Z"; back += K.path(d, o.cloak) + shadeHalf(d, -82, 0); }
      if (o.pack) back += block(-20, -77, 10, 26, "#5a4630") + K.ellipse(-15, -79, 7, 4, "#6a5a40");
      if (o.sword) back += K.line("M-14 -46L3 -93", "#a4a8ac", 3) + K.line("M3 -93L6 -101", "#4a3424", 3.4) + K.line("M-1 -95L7 -91", "#6a5a3a", 2.6);
      back += arm(-10, -76, pose.back, pose.backElbow, K.darken(sleeve, 0.25));
      if (o.hold === "tongs-back") item += K.line(`M${bx} ${by}l20 9M${bx} ${by}l20 5`, "#3a3632", 1.8);

      if (o.robe) {
        const d = "M-12 -50C-14 -32-17 -14-18 -2L18 -2C17 -14 14 -32 12 -50Z";
        lower += K.path(d, o.robe === true ? cloth : o.robe) + shadeHalf(d, -52, 0) + K.line("M-17 -7Q0 -4 17 -7", "!" + INK, 0.8, 'opacity=".5"');
        if (!o.seated) lower += K.path("M2 -3L11 -3Q15 -1 15 1L2 1Z", boot);
      } else if (o.seated) {
        lower += K.path(K.capsule(-3, -27, 15, -27, 11), K.darken(trousers, 0.2)) + K.path(K.capsule(15, -27, 16, -4, 9), K.darken(trousers, 0.2))
          + K.path(K.capsule(-1, -25, 18, -25, 11), trousers) + K.path(K.capsule(18, -25, 19, -4, 9), trousers)
          + K.path("M13 -6L22 -6Q27 -4 27 0L13 0Z", boot);
      } else {
        const spread = o.walk ? 7 : 0;
        lower += K.path(K.taper(-4, -50, -5 - spread, -5, 10, 8), K.darken(trousers, 0.2)) + K.path(K.taper(4, -50, 6 + spread, -5, 10, 8), trousers)
          + K.path(`M${-10 - spread} -6L${-1 - spread} -6Q${2 - spread} -3 ${2 - spread} 0L${-11 - spread} 0Z`, boot)
          + K.path(`M${1 + spread} -6L${10 + spread} -6Q${15 + spread} -3 ${15 + spread} 0L${1 + spread} 0Z`, boot);
      }

      const torsoD = "M-11 -79C-14 -66-12 -56-12 -46L12 -46C12 -56 14 -66 11 -79C5 -82-5 -82-11 -79Z";
      torso += K.path(torsoD, o.armor ? "#7c8086" : cloth);
      if (o.armor) torso += K.clipped(torsoD, K.line("M-12 -62H12M-12 -54H12", "!" + INK, 0.9, 'opacity=".5"') + K.line("M2 -79V-48", "!#d8dce0", 1.2, 'opacity=".5"'));
      if (o.tabard) torso += K.path("M-6 -77L6 -77L8 -36L-8 -36Z", o.tabard, thin);
      torso += shadeHalf(torsoD, -82, -44);
      if (o.apron) torso += K.path("M-8 -70L9 -70L12 -24L-9 -24Z", o.apron === true ? "#5a3f2a" : o.apron) + K.line("M-6 -70L-4 -80M7 -70L5 -80", "#3a2a1c", 1.4);
      torso += block(-12.5, -51, 25, 4.5, "#3a2a1c", 'stroke-width=".9"');
      if (o.armor) torso += K.ellipse(-10, -76, 6.5, 5, "#868a90") + K.ellipse(10, -76, 6.5, 5, "#868a90");

      head += block(-3, -84, 6.5, 6, skin, thin);
      if (o.back) {
        head += K.ellipse(0, -89.5, 7.4, 8.6, o.head === "hood" ? (o.hoodColor || cloth) : hair);
      } else {
        const faceD = "M-5.5 -89.5A7 8.6 0 1 1 8.5 -89.5A7 8.6 0 1 1-5.5 -89.5Z";
        head += K.path(faceD, skin) + K.clipped(faceD, `<rect x="${away < 0 ? -6 : 3.5}" y="-99" width="6" height="20" fill="${INK}" opacity=".5"/><rect x="2.5" y="-93" width="7" height="2.8" fill="${INK}" opacity=".6"/>`)
          + K.path("M7.6 -92L10.8 -87.4L7.8 -86.6", skin, 'stroke-width=".9"');
      }
      const hat = o.hatColor || "#3a2e24";
      const HEADWEAR = {
        short: () => K.path("M-5.6 -87C-7.5 -97-1 -100.5 4.5 -99.5C8.5 -98.6 10 -95 9 -92.5C5.5 -95.5 1 -95-1 -92C-2.5 -89.6-3 -86-4.8 -82.8Z", hair),
        long: () => K.path("M-5.6 -87C-7.5 -97-1 -100.5 4.5 -99.5C8.5 -98.6 10 -95 9 -92.5C5 -95 1 -94-1 -90C-2 -84-2 -78-3 -72L-8 -73C-8 -78-7 -82-5.6 -87Z", hair),
        bald: () => "",
        hood: () => K.path("M-9 -76C-13.5 -88-10.5 -102 1.5 -103.5C9.5 -103.5 13.5 -97.5 12 -90C9.5 -95 4.5 -96.5 1 -93C-2.5 -89-3 -83-1 -77Z", o.hoodColor || cloth),
        wide: () => K.path("M-6 -95.5C-6.5 -103-3 -106.5 2 -106.5C7 -106.5 9 -102 8.6 -95.5Z", hat) + K.ellipse(1.5, -95.5, 13.5, 2.4, hat),
        witch: () => K.path("M-7 -96.5L10 -96.5C7.5 -104 3 -112-7 -123C-4 -112-4.6 -104-7 -96.5Z", o.hatColor || "#2a2430") + K.ellipse(1.5, -96, 14, 2.6, o.hatColor || "#2a2430"),
        helm: () => K.path("M-6.5 -87C-7.5 -99-2 -103.5 2.5 -103.5C8 -103.5 10.5 -99 10 -88L-6.5 -87Z", "#7c8086") + K.line("M8.6 -95L9 -84", "#5a5e64", 2),
        cap: () => K.path("M-6 -92.5C-6 -99.5 5 -101.5 9.5 -94.5L13 -93.5L-6 -91.5Z", o.hatColor || "#5a3a2a"),
        coif: () => K.path("M-7 -78C-9 -92-6 -101 2 -101.5C9.5 -101.5 11.5 -95 10 -88C7 -93 3 -94 0 -91C-2 -87-2 -82-1 -78Z", "#8a8e94"),
        kerchief: () => K.path("M-6.5 -88C-7.5 -98 0 -101 5 -100C9 -99 10.5 -96 9.5 -93Z", o.hatColor || "#7a2a24") + K.path("M-6 -90L-11 -84L-6.5 -85Z", o.hatColor || "#7a2a24"),
      };
      const headwear = o.head || "short";
      if (!(o.back && ["short", "long", "bald", "hood"].includes(headwear))) head += (HEADWEAR[headwear] || HEADWEAR.short)();
      if (o.back && headwear === "long") head += K.path("M-7 -90C-8 -82-7 -76-6 -72L6 -72C7 -76 8 -82 7 -90Z", hair);
      if (o.beard && !o.back) head += K.path(o.beard === "long" ? "M2 -85C1 -76 5 -70 9 -72C11 -76 10.5 -82 9.5 -85Z" : "M3 -84.5C3 -79.5 6.5 -77.5 9.5 -79.5C10.5 -81.5 10 -84 9.6 -85.5Z", hair);

      front += arm(10, -76, pose.front, pose.frontElbow, sleeve);
      const [tx, ty] = pose.tool || [0.42, -0.91];
      switch (o.hold) {
        case "hammer": {
          const ex = hx + tx * 22, ey = hy + ty * 22;
          item += K.line(`M${hx} ${hy}L${n(ex)} ${n(ey)}`, "#5a3e28", 3.2) + K.path(K.capsule(ex + ty * 6, ey - tx * 6, ex - ty * 6, ey + tx * 6, 7), "#4a4c50");
          break;
        }
        case "tongs": {
          item += K.line(`M${hx} ${hy}l24 7M${hx} ${hy}l24 3`, "#3a3632", 1.8);
          const [wx, wy] = world(hx + 24, hy + 5), [vx] = world(hx + 38, hy + 5);
          ctx.emit(`<path d="M${n(wx)} ${n(wy - 1.6 * s)}L${n(vx)} ${n(wy - 1 * s)}L${n(vx)} ${n(wy + 1 * s)}L${n(wx)} ${n(wy + 1.6 * s)}Z" fill="#ff9a3a"/>`);
          ctx.glow((wx + vx) / 2, wy, 26 * s, "#ff8a30", 0.6);
          break;
        }
        case "tankard":
          item += block(hx - 2, hy - 10, 8, 11, "#7a5a3a", thin) + K.line(`M${hx + 6} ${hy - 8}q4 0 4 4t-4 4`, "!" + INK, 1.2) + solid(hx - 2, hy - 11, 8, 2, "!#efe6d0");
          break;
        case "book":
          item += block(Math.min(bx, hx) - 4, hy - 9, Math.abs(hx - bx) + 10, 11, o.bookColor || "#6b2a24", thin) + solid(Math.min(bx, hx) - 2, hy - 8, Math.abs(hx - bx) + 6, 2.5, "#e6dcc2");
          break;
        case "staff":
          item += K.line(`M${hx + 1} ${hy - 44}L${hx + 3} -2`, "#5a3e28", 3.4) + K.circle(hx + 1, hy - 46, 3.6, "#6a5a3a");
          break;
        case "spear":
          item += K.line(`M${hx} -126L${hx} -1`, "#5a3e28", 3) + K.path(`M${hx} -138L${hx + 3.4} -128L${hx} -122L${hx - 3.4} -128Z`, metal, thin);
          break;
        case "sword": {
          const gx = hx + tx * 5, gy = hy + ty * 5, px = hx + tx * 50, py = hy + ty * 50;
          item += K.path(K.taper(gx, gy, px, py, 4.6, 1.4), "#b4b8bc", thin) + K.line(`M${n(gx + ty * 7)} ${n(gy - tx * 7)}L${n(gx - ty * 7)} ${n(gy + tx * 7)}`, "#6a5a3a", 2.6);
          break;
        }
        case "box":
          item += block(hx - 17, hy - 16, 24, 18, "#7a5a36") + K.line(`M${hx - 17} ${hy - 7}h24`, "!" + INK, 0.9, 'opacity=".6"');
          break;
        case "ladle":
          item += K.line(`M${hx} ${hy}L${n(hx + tx * 40)} ${n(hy + ty * 40)}`, "#5a3e28", 2.6);
          break;
        case "basket":
          item += K.path(`M${hx - 10} ${hy + 3}L${hx + 10} ${hy + 3}L${hx + 8} ${hy + 15}L${hx - 8} ${hy + 15}Z`, "#9a7a4a", thin) + K.line(`M${hx - 9} ${hy + 3}Q${hx} ${hy - 9} ${hx + 9} ${hy + 3}`, "#6a4a2a", 1.6) + K.ellipse(hx - 3, hy + 2, 4, 2.4, "#7a2a24", 'stroke-width=".8"');
          break;
        case "orb": {
          const [wx, wy] = world(hx + 2, hy - 7);
          ctx.live(`<circle class="sb-breathe" cx="${n(wx)}" cy="${n(wy)}" r="${n(4.6 * s)}" fill="${o.glowColor || "#a8e4ff"}"/>`);
          ctx.glow(wx, wy, 34 * s, o.glowColor || "#8fd6ff", 0.85, "sb-breathe");
          ctx.light(wx, wy, 150 * s, 0.6, o.glowColor || "#8fd6ff");
          break;
        }
      }
      const shadow = o.seated || o.noShadow ? "" : `<ellipse cx="0" cy="0" rx="19" ry="3.4" fill="${INK}" opacity=".45"/>`;
      const upper = back + torso + head + front + item;
      return K.at(x, base, `<g transform="scale(${(f * s).toFixed(3)} ${s.toFixed(3)})">${shadow}${lower}${lift ? `<g transform="translate(0 ${lift})">${upper}</g>` : upper}</g>`);
    });
  }

  function cat(ctx, x, base, s = 1, face = 1) {
    const color = "#1c1816", [ex, ey] = [x + face * 4 * s, base - 25 * s];
    ctx.emit(`<circle cx="${n(ex - face * 2 * s)}" cy="${n(ey)}" r="${n(0.9 * s)}" fill="#e8cf5a"/><circle cx="${n(ex + face * 1.6 * s)}" cy="${n(ey)}" r="${n(0.9 * s)}" fill="#e8cf5a"/>`);
    return K.at(x, base, `<g transform="scale(${(face * s).toFixed(3)} ${s.toFixed(3)})">${K.path("M8 -2C15 -2 18 -7 15 -13", "none", `stroke="${color}" stroke-width="2.6" stroke-linecap="round" fill="none"`)}${K.path("M-8 0C-10 -8-9 -16-4 -20C-6 -24-5 -28-1 -30L0 -35L3 -30L6 -30L8 -35L9 -29C11 -26 10 -21 7 -19C10 -15 11 -7 9 0Z", color, thin)}</g>`);
  }
  function dog(ctx, x, base, s = 1, face = 1) {
    return K.at(x, base, `<g transform="scale(${(face * s).toFixed(3)} ${s.toFixed(3)})">${K.path("M-30 0C-32 -6-28 -12-20 -13C-8 -15 8 -15 16 -12L20 -16C22 -20 28 -21 31 -18L36 -16C37 -13 35 -11 31 -11L28 -8C27 -4 24 -1 20 0Z", "#9a7650", thin)}${K.path("M22 -18L20 -24L25 -20Z", "#7a5a3a", thin)}${K.line("M-30 -3C-36 -2-40 -5-41 -9", "#9a7650", 2.6)}<circle cx="29" cy="-15" r="1" fill="${INK}"/></g>`);
  }

  /* ---------- Furnishings ---------- */
  function barrel(ctx, x, base, w, h, color = ctx.mat.wood) {
    const l = x - w / 2, r = x + w / 2, top = base - h, bulge = w * 0.1;
    const d = `M${n(l)} ${n(top + 3)}C${n(l - bulge)} ${n(top + h * 0.35)} ${n(l - bulge)} ${n(top + h * 0.65)} ${n(l)} ${n(base - 2)}Q${n(x)} ${n(base + 3)} ${n(r)} ${n(base - 2)}C${n(r + bulge)} ${n(top + h * 0.65)} ${n(r + bulge)} ${n(top + h * 0.35)} ${n(r)} ${n(top + 3)}Q${n(x)} ${n(top + 7)} ${n(l)} ${n(top + 3)}Z`;
    let staves = "";
    for (let index = 1; index < 5; index++) { const sx = l + w * index / 5, bow = (sx - x) / (w / 2) * bulge; staves += `M${n(sx)} ${n(top + 4)}Q${n(sx + bow)} ${n(top + h / 2)} ${n(sx)} ${n(base - 1)}`; }
    const hoop = t => { const y = top + h * t, b = bulge * Math.sin(Math.PI * t); return `M${n(l - b)} ${n(y)}Q${n(x)} ${n(y + 4)} ${n(r + b)} ${n(y)}`; };
    return K.path(d, color) + K.clipped(d, `<path d="${staves}" stroke="${INK}" stroke-width=".9" opacity=".5" fill="none"/>`) + sideShade(ctx, d, l, top, w, h, 0.5)
      + K.line(hoop(0.17) + hoop(0.83), "#2e2a26", 2.6) + K.ellipse(x, top + 3, w / 2, 3.2, K.darken(color, 0.15), 'stroke-width="1.2"');
  }
  function crate(ctx, x, base, w, h, color = "#8a6a44") {
    const d = rectD(x - w / 2, base - h, w, h);
    let planks = "";
    for (let y = base - h + h / 3; y < base - 2; y += h / 3) planks += `M${n(x - w / 2)} ${n(y)}h${n(w)}`;
    return K.path(d, color) + K.clipped(d, `<path d="${planks}" stroke="${INK}" stroke-width="1" opacity=".55" fill="none"/>`)
      + bar(x - w / 2 + 4, base - 4, x + w / 2 - 4, base - h + 4, 5, K.darken(color, 0.12), 'stroke-width="1"')
      + K.path(rectD(x - w / 2 + 2.5, base - h + 2.5, w - 5, h - 5), "none", `stroke="${K.paint(K.darken(color, 0.25))}" stroke-width="4" fill="none"`)
      + K.path(d, "none", `stroke="${K.OUTLINE}" stroke-width="1.6" fill="none"`) + sideShade(ctx, d, x - w / 2, base - h, w, h, 0.45);
  }
  function sack(ctx, x, base, w, h, color = "#a08a62") {
    const d = `M${n(x - w / 2)} ${n(base)}C${n(x - w / 2 - 3)} ${n(base - h * 0.55)} ${n(x - w * 0.34)} ${n(base - h * 0.92)} ${n(x - w * 0.1)} ${n(base - h * 0.9)}L${n(x - w * 0.18)} ${n(base - h - 6)}L${n(x + w * 0.16)} ${n(base - h - 7)}L${n(x + w * 0.1)} ${n(base - h * 0.9)}C${n(x + w * 0.36)} ${n(base - h * 0.9)} ${n(x + w / 2 + 3)} ${n(base - h * 0.5)} ${n(x + w / 2)} ${n(base)}Z`;
    return K.path(d, color) + K.clipped(d, K.line(`M${n(x - w * 0.2)} ${n(base - h * 0.7)}q${n(w * 0.1)} ${n(h * 0.3)} ${n(w * 0.02)} ${n(h * 0.6)}M${n(x + w * 0.15)} ${n(base - h * 0.6)}q${n(w * 0.06)} ${n(h * 0.25)} 0 ${n(h * 0.5)}`, "!" + INK, 0.9, 'opacity=".5"'))
      + sideShade(ctx, d, x - w / 2, base - h, w, h, 0.45) + K.line(`M${n(x - w * 0.14)} ${n(base - h * 0.9)}h${n(w * 0.26)}`, "#4a3424", 2);
  }
  function jar(x, base, w, h, color, lid = "#5a4a3a") {
    return K.path(`M${n(x - w / 2)} ${n(base)}V${n(base - h + 4)}Q${n(x - w / 2)} ${n(base - h + 1)} ${n(x - w / 2 + 3)} ${n(base - h + 1)}H${n(x + w / 2 - 3)}Q${n(x + w / 2)} ${n(base - h + 1)} ${n(x + w / 2)} ${n(base - h + 4)}V${n(base)}Z`, color, 'stroke-width="1.1"')
      + block(x - w / 2 + 1, base - h - 2, w - 2, 3.5, lid, 'stroke-width=".9"') + K.line(`M${n(x - w / 2 + 2.5)} ${n(base - h + 5)}V${n(base - 3)}`, "!#fff2d8", 1, 'opacity=".35"');
  }
  function bottle(x, base, h, color) {
    const w = h * 0.4;
    return K.path(`M${n(x - w / 2)} ${n(base)}V${n(base - h * 0.55)}Q${n(x - w / 2)} ${n(base - h * 0.7)} ${n(x - w * 0.2)} ${n(base - h * 0.74)}V${n(base - h)}H${n(x + w * 0.2)}V${n(base - h * 0.74)}Q${n(x + w / 2)} ${n(base - h * 0.7)} ${n(x + w / 2)} ${n(base - h * 0.55)}V${n(base)}Z`, color, 'stroke-width="1"')
      + solid(x - w * 0.22, base - h - 2, w * 0.44, 3, "#8a6a4a");
  }
  function potion(ctx, x, base, h, color) {
    const w = h * 0.5, out = bottle(x, base, h, "#3a3a42");
    ctx.emit(`<path d="M${n(x - w / 2 + 1)} ${n(base - 1)}V${n(base - h * 0.45)}H${n(x + w / 2 - 1)}V${n(base - 1)}Z" fill="${color}" opacity=".9"/>`);
    ctx.glow(x, base - h * 0.3, h * 1.3, color, 0.45);
    return out;
  }
  function goods(ctx, x, base, w, h) {
    let out = "", at = x;
    while (at < x + w - 6) {
      const kind = ctx.pick(["jar", "jar", "bottle", "box", "pot", "bolt", "bottle"]), size = Math.min(h, ctx.r(h * 0.5, h * 0.95));
      let width = size * 0.7;
      if (kind === "bottle") width = size * 0.42;
      if (kind === "box") width = size * ctx.r(0.9, 1.4);
      if (kind === "bolt") width = size * 0.5;
      if (at + width > x + w) break;
      const color = ctx.pick(GOODS), cx = at + width / 2;
      if (kind === "jar") out += jar(cx, base, width, size, color);
      else if (kind === "bottle") out += bottle(cx, base, size, ctx.pick(["#4a6a4a", "#6a4a3a", "#3a4a5a", "#7a6a3a"]));
      else if (kind === "box") out += block(at, base - size * 0.7, width, size * 0.7, color, 'stroke-width="1.1"') + K.line(`M${n(at)} ${n(base - size * 0.5)}h${n(width)}`, "!" + INK, 0.8, 'opacity=".5"');
      else if (kind === "pot") out += K.path(`M${n(at)} ${n(base - size * 0.6)}Q${n(at - 2)} ${n(base)} ${n(cx)} ${n(base)}Q${n(at + width + 2)} ${n(base)} ${n(at + width)} ${n(base - size * 0.6)}Z`, "#7a4a32", 'stroke-width="1.1"') + K.ellipse(cx, base - size * 0.6, width / 2, 2, "#5a3424", 'stroke-width=".9"');
      else out += K.ellipse(cx, base - size / 2, width / 2, size / 2, color, 'stroke-width="1.1"') + K.ellipse(cx, base - size / 2, width / 5, size / 5, K.darken(color, 0.3), 'stroke-width=".8"');
      at += width + ctx.r(1.5, 6);
    }
    return out;
  }
  function books(ctx, x, base, w, h) {
    let out = "", at = x;
    while (at < x + w - 4) {
      const bw = ctx.r(4, 8.5), bh = h * ctx.r(0.68, 1), color = ctx.pick(SPINES);
      if (at + bw > x + w) break;
      if (ctx.chance(0.05) && at + bh + 2 < x + w) {
        out += block(at, base - bw, bh, bw, color, 'stroke-width=".8"');
        at += bh + 2;
        continue;
      }
      out += block(at, base - bh, bw, bh, color, 'stroke-width=".8"') + K.line(`M${n(at + 1)} ${n(base - bh + 3)}h${n(bw - 2)}M${n(at + 1)} ${n(base - 4)}h${n(bw - 2)}`, "#c8a050", 0.8, 'opacity=".75"');
      at += bw + (ctx.chance(0.12) ? ctx.r(1, 4) : 0.4);
    }
    return out;
  }
  function potions(ctx, x, base, w, h) {
    let out = "", at = x + 2;
    while (at < x + w - 8) {
      const size = ctx.r(h * 0.55, h * 0.95);
      if (ctx.chance(0.55)) out += potion(ctx, at + size * 0.25, base, size, ctx.pick(POTIONS));
      else out += jar(at + size * 0.32, base, size * 0.64, size * 0.8, ctx.pick(GOODS));
      at += size * 0.7 + ctx.r(2, 6);
    }
    return out;
  }
  function shelving(ctx, x, top, w, h, rows, fill, wood = ctx.mat.wood) {
    let out = block(x, top, w, h, K.darken(wood, 0.5));
    const rowH = h / rows;
    for (let index = 0; index < rows; index++) {
      const shelfY = top + rowH * (index + 1);
      out += solid(x + 7, top + rowH * index, w - 14, 7, "!" + INK, 'opacity=".45"');
      out += fill(ctx, x + 10, shelfY - 5, w - 20, rowH - 12);
      out += block(x + 4, shelfY - 5, w - 8, 5, wood, 'stroke-width="1.2"');
    }
    return out + block(x, top, 7, h, wood) + block(x + w - 7, top, 7, h, wood) + block(x - 3, top - 6, w + 6, 8, wood);
  }
  function counter(ctx, x, top, w, h, color = ctx.mat.wood) {
    const front = rectD(x, top + 8, w, h - 8);
    let panels = "";
    for (let px = x + 12; px < x + w - 30; px += 64) panels += rectD(px, top + 18, Math.min(52, x + w - 12 - px), h - 28);
    return K.path(front, color) + K.path(panels, "none", `stroke="${INK}" stroke-width="1.2" opacity=".6" fill="none"`) + sideShade(ctx, front, x, top + 8, w, h - 8, 0.4, 0.3)
      + solid(x, top + 8, w, 7, "!" + INK, 'opacity=".5"') + block(x - 8, top, w + 16, 9, K.lighten(color, 0.08));
  }
  function table(ctx, cx, top, w, color = ctx.mat.wood, legHeight = 34) {
    return block(cx - w / 2 + 8, top + 6, 7, legHeight, K.darken(color, 0.2)) + block(cx + w / 2 - 15, top + 6, 7, legHeight, K.darken(color, 0.2))
      + block(cx - w / 2 + 4, top + 6, w - 8, 6, K.darken(color, 0.1), thin) + block(cx - w / 2, top, w, 7, color);
  }
  function scales(ctx, x, base, s = 1) {
    const brass = "#b08a3a";
    return K.line(`M${n(x)} ${n(base)}V${n(base - 26 * s)}M${n(x - 16 * s)} ${n(base - 24 * s)}H${n(x + 16 * s)}M${n(x - 16 * s)} ${n(base - 24 * s)}l-4 10M${n(x - 16 * s)} ${n(base - 24 * s)}l4 10M${n(x + 16 * s)} ${n(base - 24 * s)}l-4 10M${n(x + 16 * s)} ${n(base - 24 * s)}l4 10`, brass, 1.4)
      + K.path(`M${n(x - 22 * s)} ${n(base - 14 * s)}Q${n(x - 16 * s)} ${n(base - 9 * s)} ${n(x - 10 * s)} ${n(base - 14 * s)}Z`, brass, 'stroke-width=".9"')
      + K.path(`M${n(x + 10 * s)} ${n(base - 14 * s)}Q${n(x + 16 * s)} ${n(base - 9 * s)} ${n(x + 22 * s)} ${n(base - 14 * s)}Z`, brass, 'stroke-width=".9"')
      + block(x - 7 * s, base - 3, 14 * s, 3, brass, 'stroke-width=".9"');
  }
  function ledger(x, base, w = 26) {
    return K.path(`M${n(x - w / 2)} ${n(base)}L${n(x - w / 2 + 3)} ${n(base - 5)}L${n(x + w / 2 - 3)} ${n(base - 5)}L${n(x + w / 2)} ${n(base)}Z`, "#e2d6bc", 'stroke-width=".9"') + K.line(`M${n(x)} ${n(base - 5)}V${n(base)}`, "!" + INK, 0.8);
  }
  function coins(x, base, count = 4) {
    let out = "";
    for (let index = 0; index < count; index++) out += K.ellipse(x + index * 5 - count * 2.5, base - 1.5 - (index % 2) * 2, 3, 1.4, "!#c8a040", 'stroke-width=".7"');
    return out;
  }
  function ladder(ctx, x1, base, x2, top, w = 26) {
    const wood = ctx.mat.wood;
    let rungs = "";
    for (let t = 0.08; t < 1; t += 0.11) rungs += `M${n(x1 + (x2 - x1) * t - w / 2 + 2)} ${n(base + (top - base) * t)}h${n(w - 4)}`;
    return K.line(rungs, wood, 3.2) + bar(x1 - w / 2, base, x2 - w / 2, top, 5, wood, thin) + bar(x1 + w / 2, base, x2 + w / 2, top, 5, wood, thin);
  }
  function balustrade(ctx, x, y, w, h, wood = ctx.mat.wood) {
    let posts = "";
    for (let px = x + 8; px < x + w; px += 16) posts += K.path(`M${n(px - 2.4)} ${n(y + h)}C${n(px - 4.5)} ${n(y + h * 0.6)} ${n(px - 1)} ${n(y + h * 0.45)} ${n(px - 2)} ${n(y + 4)}H${n(px + 2)}C${n(px + 1)} ${n(y + h * 0.45)} ${n(px + 4.5)} ${n(y + h * 0.6)} ${n(px + 2.4)} ${n(y + h)}Z`, wood, 'stroke-width="1"');
    return posts + block(x - 2, y, w + 4, 6, K.lighten(wood, 0.05)) + block(x - 2, y + h, w + 4, 5, wood, thin);
  }

  // Emblems for signs and banners, centred on (cx, cy) at radius r.
  function emblem(kind, cx, cy, r, color) {
    const fill = K.paint(color), pen = `stroke="${K.OUTLINE}" stroke-width="1" stroke-linejoin="round"`;
    const shapes = {
      anvil: `<path d="M-10 -3H6L8 0H3L2 3L5 6H-5L-2 3L-3 0H-6Q-9-1-12-3Z" fill="${fill}" ${pen}/><path d="M3 -12L-4 -5" stroke="${fill}" stroke-width="2.4"/><rect x="1" y="-14" width="6" height="4" fill="${fill}" ${pen} transform="rotate(-45 4 -12)"/>`,
      scales: `<path d="M0 -9V7M-8 -6H8M-8 -6L-11 1H-5ZM8 -6L5 1H11Z" fill="${fill}" stroke="${fill}" stroke-width="1.6" stroke-linejoin="round"/><rect x="-4" y="6" width="8" height="2.4" fill="${fill}"/>`,
      star: `<path d="${K.star(0, 0, 10, 3.4, 4)}" fill="${fill}" ${pen}/><circle r="6.5" fill="none" stroke="${fill}" stroke-width="1.3"/>`,
      swords: `<path d="M-8 8L7 -9M8 8L-7 -9" stroke="${fill}" stroke-width="2.4" stroke-linecap="round"/><path d="M-9 3L-3 9M9 3L3 9" stroke="${fill}" stroke-width="2"/><path d="M-5 -4H5V2C5 6 0 9 0 9C0 9-5 6-5 2Z" fill="${fill}" ${pen}/>`,
      bolt: `<path d="M3 -12L-7 1H-1L-4 12L7 -2H1L5 -12Z" fill="${fill}" ${pen}/>`,
      tankard: `<path d="M-6 -7H5V8H-6Z" fill="${fill}" ${pen}/><path d="M5 -4Q10 -4 10 1Q10 5 5 5" fill="none" stroke="${fill}" stroke-width="2"/><path d="M-7 -8Q-4 -11 0 -9Q4 -11 6 -8" fill="${fill}"/>`,
    };
    return `<g transform="translate(${n(cx)} ${n(cy)}) scale(${(r / 10).toFixed(3)})">${shapes[kind] || shapes.star}</g>`;
  }
  function hangingBanner(ctx, x, top, w, h, color, kind, trim = ctx.mat.trim) {
    const l = x - w / 2, r = x + w / 2;
    const d = `M${n(l)} ${n(top)}H${n(r)}V${n(top + h)}L${n(x)} ${n(top + h - w * 0.32)}L${n(l)} ${n(top + h)}Z`;
    const inset = `M${n(l + 4)} ${n(top + 4)}H${n(r - 4)}V${n(top + h - 8)}L${n(x)} ${n(top + h - w * 0.32 - 5)}L${n(l + 4)} ${n(top + h - 8)}Z`;
    return K.path(d, color) + K.clipped(d, K.hatch(l + w * 0.24, top, w * 0.1, h, 80, 2.4, 0.6, 0.5) + K.hatch(l + w * 0.68, top, w * 0.12, h, 80, 2.4, 0.6, 0.5))
      + sideShade(ctx, d, l, top, w, h, 0.35, 0.3)
      + K.path(inset, "none", `stroke="${K.paint(trim)}" stroke-width="1.4" fill="none"`) + emblem(kind, x, top + h * 0.38, w * 0.3, trim)
      + K.line(`M${n(l - 6)} ${n(top)}H${n(r + 6)}`, "#3a2e24", 3.4) + K.circle(l - 7, top, 2.6, "#3a2e24", thin) + K.circle(r + 7, top, 2.6, "#3a2e24", thin);
  }
  function noticeBoard(ctx, x, y, w, h, count) {
    let papers = "";
    for (let index = 0; index < count; index++) {
      const pw = ctx.r(16, 24), ph = ctx.r(20, 28), px = x + 10 + pw / 2 + ctx.r(0, w - pw - 20), py = y + 8 + ph / 2 + ctx.r(0, h - ph - 16);
      let scribble = "";
      for (let line = 0; line < 4; line++) scribble += `M${n(-pw / 2 + 3)} ${n(-ph / 2 + 7 + line * 4.5)}h${n(ctx.r(pw * 0.4, pw - 6))}`;
      papers += K.at(px, py, block(-pw / 2, -ph / 2, pw, ph, BONE, 'stroke-width=".9"') + K.line(scribble, "!" + INK, 0.8, 'opacity=".55"') + K.circle(0, -ph / 2 + 2.5, 1.6, "!#a83a2a", 'stroke-width=".6"'), ctx.r(-7, 7));
    }
    return block(x, y, w, h, ctx.mat.beam) + block(x + 5, y + 5, w - 10, h - 10, "#7a5e44", thin) + papers;
  }
  function herbs(ctx, x, top, y, color = "#5a6a3a") {
    return K.line(`M${n(x)} ${n(top)}V${n(y)}`, "#4a3a2a", 1) + K.path(`M${n(x - 2)} ${n(y)}L${n(x - 9)} ${n(y + 20)}Q${n(x)} ${n(y + 26)} ${n(x + 9)} ${n(y + 20)}L${n(x + 2)} ${n(y)}Z`, color, thin)
      + K.line(`M${n(x)} ${n(y + 2)}L${n(x - 5)} ${n(y + 20)}M${n(x)} ${n(y + 2)}L${n(x + 5)} ${n(y + 20)}M${n(x)} ${n(y + 2)}V${n(y + 22)}`, "!" + INK, 0.7, 'opacity=".5"') + block(x - 3, y - 1, 6, 3, "#8a6a3a", 'stroke-width=".7"');
  }
  function hangingPot(ctx, x, top, y, r = 9) {
    return K.line(`M${n(x)} ${n(top)}V${n(y - r)}`, "#2a2420", 1) + K.path(`M${n(x - r)} ${n(y - r * 0.4)}Q${n(x - r)} ${n(y + r)} ${n(x)} ${n(y + r)}Q${n(x + r)} ${n(y + r)} ${n(x + r)} ${n(y - r * 0.4)}Z`, "#3a3634") + K.line(`M${n(x - r)} ${n(y - r * 0.4)}Q${n(x)} ${n(y - r * 1.4)} ${n(x + r)} ${n(y - r * 0.4)}`, "#2a2420", 1.2);
  }
  function sausages(ctx, x, top, y) {
    let out = K.line(`M${n(x - 14)} ${n(top)}L${n(x)} ${n(y)}L${n(x + 14)} ${n(top)}`, "#4a3a2a", 1);
    for (const dx of [-8, 0, 8]) out += K.path(K.capsule(x + dx, y - 2, x + dx * 1.3, y + 18, 6), "#7a3a28", thin);
    return out;
  }
  function trophy(ctx, x, y, s = 1) {
    const bone = "#cfc2a2";
    return K.at(x, y, `<g transform="scale(${s.toFixed(3)})">${K.path("M-22 -14H22L18 16H-18Z", ctx.mat.beam)}${K.path("M-12 -6C-16 -20-30 -30-38 -26C-30 -24-22 -16-16 -2Z", bone)}${K.path("M12 -6C16 -20 30 -30 38 -26C30 -24 22 -16 16 -2Z", bone)}${K.path("M-12 -8C-13 4-8 14 0 24C8 14 13 4 12 -8C8 -12-8 -12-12 -8Z", bone)}<ellipse cx="-5" cy="-1" rx="3" ry="2.4" fill="${INK}"/><ellipse cx="5" cy="-1" rx="3" ry="2.4" fill="${INK}"/>${K.line("M-3 14L-1 20M3 14L1 20", "!" + INK, 1.2)}</g>`);
  }

  /* ---------- The smithy's gear ---------- */
  function swordUp(len) {
    return K.path(`M-2.4 -14L-2.4 ${n(-len + 7)}L0 ${n(-len)}L2.4 ${n(-len + 7)}L2.4 -14Z`, "#a8acb0", thin) + K.line(`M0 -16V${n(-len + 9)}`, "!" + INK, 0.6, 'opacity=".45"')
      + block(-8, -15.5, 16, 3, "#6a5a3a", 'stroke-width=".9"') + block(-1.6, -12.5, 3.2, 11, "#4a3424", 'stroke-width=".8"') + K.circle(0, -0.5, 2.6, "#8a7448", 'stroke-width=".8"');
  }
  function axeUp(len) {
    return K.line(`M0 0V${n(-len)}`, "#5a3e28", 3.2) + K.path(`M0 ${n(-len + 3)}C9 ${n(-len - 1)} 15 ${n(-len + 1)} 18 ${n(-len + 5)}L17 ${n(-len + 22)}C13 ${n(-len + 19)} 7 ${n(-len + 17)} 0 ${n(-len + 16)}Z`, "#8a8e94", thin);
  }
  function spearUp(len) {
    return K.line(`M0 0V${n(-len + 12)}`, "#5a3e28", 2.8) + K.path(`M0 ${n(-len)}L3.6 ${n(-len + 10)}L0 ${n(-len + 16)}L-3.6 ${n(-len + 10)}Z`, "#9a9ea4", thin);
  }
  function halberdUp(len) {
    return spearUp(len) + K.path(`M0 ${n(-len + 18)}C10 ${n(-len + 14)} 14 ${n(-len + 20)} 14 ${n(-len + 28)}C8 ${n(-len + 26)} 4 ${n(-len + 30)} 0 ${n(-len + 32)}Z`, "#8a8e94", thin) + K.path(`M0 ${n(-len + 22)}L-9 ${n(-len + 26)}L0 ${n(-len + 28)}Z`, "#8a8e94", thin);
  }
  function maceUp(len) {
    return K.line(`M0 0V${n(-len + 8)}`, "#5a3e28", 3.2) + K.path(`M-5 ${n(-len + 12)}L-6 ${n(-len + 4)}L0 ${n(-len)}L6 ${n(-len + 4)}L5 ${n(-len + 12)}Z`, "#6e7278", thin);
  }
  const WEAPONS = { sword: swordUp, axe: axeUp, spear: spearUp, halberd: halberdUp, mace: maceUp };
  const weapon = (kind, x, base, len, angle = 0) => K.at(x, base, WEAPONS[kind](len), angle);

  function shield(ctx, x, y, r, color, kind = "round", trim = ctx.mat.trim) {
    if (kind === "round") {
      const d = `M${n(x - r)} ${n(y)}A${n(r)} ${n(r)} 0 1 1 ${n(x + r)} ${n(y)}A${n(r)} ${n(r)} 0 1 1 ${n(x - r)} ${n(y)}Z`;
      return K.path(d, color) + K.clipped(d, solid(x - r, y - r, r, r, K.lighten(color, 0.15)) + solid(x, y, r, r, K.lighten(color, 0.15))) + sideShade(ctx, d, x - r, y - r, r * 2, r * 2, 0.4)
        + K.circle(x, y, r - 2.5, "none", `stroke="${K.paint(trim)}" stroke-width="2" fill="none"`) + K.circle(x, y, r * 0.24, "#8a8e94");
    }
    const d = `M${n(x - r)} ${n(y - r * 0.9)}H${n(x + r)}V${n(y)}C${n(x + r)} ${n(y + r * 0.6)} ${n(x + r * 0.4)} ${n(y + r)} ${n(x)} ${n(y + r * 1.2)}C${n(x - r * 0.4)} ${n(y + r)} ${n(x - r)} ${n(y + r * 0.6)} ${n(x - r)} ${n(y)}Z`;
    return K.path(d, color) + K.clipped(d, K.path(`M${n(x - r)} ${n(y + r * 0.1)}L${n(x)} ${n(y - r * 0.5)}L${n(x + r)} ${n(y + r * 0.1)}V${n(y + r * 0.5)}L${n(x)} ${n(y - r * 0.1)}L${n(x - r)} ${n(y + r * 0.5)}Z`, trim, thin))
      + sideShade(ctx, d, x - r, y - r, r * 2, r * 2.2, 0.4);
  }
  function armorStand(ctx, x, base, s = 1, metal = "#80848a") {
    const body = `<path d="M-1.5 0V-108" stroke="#3a2e24" stroke-width="3.4"/>${K.path("M-14 0L-12 -4H12L14 0Z", "#3a2e24", thin)}`
      + K.path("M-15 -84C-18 -70-15 -58-14 -50L14 -50C15 -58 18 -70 15 -84C8 -88-8 -88-15 -84Z", metal)
      + K.clipped("M-15 -84C-18 -70-15 -58-14 -50L14 -50C15 -58 18 -70 15 -84C8 -88-8 -88-15 -84Z", K.line("M0 -86V-50", "!#dfe3e6", 1.4, 'opacity=".55"') + `<rect x="${x < ctx.keyX ? -20 : 3}" y="-90" width="17" height="44" fill="${INK}" opacity=".45"/>`)
      + K.path("M-14 -50L14 -50L16 -38L-16 -38Z", metal, thin) + K.path("M-15 -38L15 -38L13 -28L-13 -28Z", metal, thin)
      + K.ellipse(-15, -82, 8, 6, metal) + K.ellipse(15, -82, 8, 6, metal)
      + K.path("M-8 -94C-9 -108-3 -113 0 -113C3 -113 9 -108 8 -94L6 -90L-6 -90Z", metal) + `<rect x="-6" y="-102" width="12" height="2.4" fill="${INK}"/>`;
    return `<g transform="translate(${n(x)} ${n(base)}) scale(${s.toFixed(3)})">${body}</g>`;
  }
  function anvil(ctx, x, base, s = 1) {
    return `<g transform="translate(${n(x)} ${n(base)}) scale(${s.toFixed(3)})">`
      + K.path("M-15 0L-13 -27Q0 -30 13 -27L15 0Z", "#5a4030") + K.ellipse(0, -27.5, 13, 2.6, "#7a5a40", 'stroke-width="1"')
      + K.path("M-25 -44L19 -44L21 -38L9 -36L7 -33L14 -28L-13 -28L-7 -33L-9 -36L-23 -38Q-34 -39-43 -44Q-33 -45.6-25 -44Z", "#4a4c52")
      + K.line("M-24 -43H18", "!#a0a6ae", 1.2, 'opacity=".7"') + "</g>";
  }
  function forge(ctx, cx, base, w, h, { top = 0, brick = "#7a4636", hood = "#3a3430" } = {}) {
    const bodyTop = base - h * 0.5, l = cx - w / 2;
    const hoodD = poly([[cx - w * 0.58, bodyTop - 8], [cx - w * 0.22, top + 22], [cx - w * 0.22, top - 2], [cx + w * 0.22, top - 2], [cx + w * 0.22, top + 22], [cx + w * 0.58, bodyTop - 8]]);
    let rivets = "";
    for (let y = top + 30; y < bodyTop - 14; y += 18) { const t = (y - top) / (bodyTop - top); rivets += `M${n(cx - w * (0.22 + 0.36 * t) + 6)} ${n(y)}H${n(cx + w * (0.22 + 0.36 * t) - 6)}`; }
    let out = K.path(hoodD, hood) + K.clipped(hoodD, K.line(rivets, "!" + INK, 1, 'opacity=".5"') + K.hatch(cx - w * 0.6, top, w * 1.2, bodyTop - top, 64, 3.2, 0.7, 0.5)
      + `<rect x="${n(cx - w * 0.6)}" y="${n(bodyTop - 40)}" width="${n(w * 1.2)}" height="40" fill="#ff8a3a" opacity=".12"/>`);
    out += masonry(ctx, l, bodyTop, w, base - bodyTop, brick, { row: 10, min: 2.4, regular: true, mortar: "#2a1a14" }) + K.path(rectD(l, bodyTop, w, base - bodyTop), "none", `stroke="${K.OUTLINE}" stroke-width="1.8" fill="none"`);
    out += block(l - 9, bodyTop - 9, w + 18, 10, "#4a3a30");
    const mouth = archPath(cx, base - h * 0.07, w * 0.62, h * 0.33, 0.6);
    out += K.path(mouth, "!#120b08") + K.clipped(mouth, K.hatch(cx - w * 0.31, base - h * 0.4, w * 0.62, h * 0.33, 60, 3, 0.6, 0.6, "#3a1a0e"));
    fire(ctx, cx, base - h * 0.08, w * 0.5, h * 0.26, { radius: w * 2.3 });
    ctx.emit(`<path d="M${n(cx - w * 0.3)} ${n(base - h * 0.07)}H${n(cx + w * 0.3)}" stroke="#ff7a2a" stroke-width="3" opacity=".8"/>`);
    return out;
  }
  function bellows(ctx, x, base, chainTop = 0) {
    const y = base - 30, wood = ctx.mat.wood;
    const lever = K.line(`M${x - 44} ${y - 15}L${x - 12} ${y - 70}`, wood, 3.6) + K.line(`M${x - 12} ${y - 70}V${chainTop}`, "#2a2420", 1.4) + K.circle(x - 12, y - 70, 2.4, "#3a3632", thin);
    const body = `M${x + 18} ${y - 4}C${x} ${y - 8} ${x - 24} ${y - 17} ${x - 31} ${y - 9}C${x - 35} ${y - 3} ${x - 35} ${y + 3} ${x - 31} ${y + 9}C${x - 24} ${y + 17} ${x} ${y + 8} ${x + 18} ${y + 4}Z`;
    let pleats = "";
    for (const px of [x - 20, x - 9, x + 2]) pleats += `M${px} ${y - 10 + (px - x) * -0.18}q-4 ${10 - (px - x) * -0.18} 0 ${2 * (10 - (px - x) * -0.18)}`;
    return lever + K.line(`M${x - 14} ${y + 8}L${x - 18} ${base}M${x + 6} ${y + 5}L${x + 10} ${base}`, wood, 4)
      + K.line(`M${x - 31} ${y - 9}L${x - 46} ${y - 16}M${x - 31} ${y + 9}L${x - 46} ${y + 14}`, wood, 3.4)
      + K.path(body, "#6a4a30") + K.clipped(body, K.line(pleats, "!" + INK, 1, 'opacity=".55"') + `<rect x="${x - 36}" y="${y - 18}" width="56" height="9" fill="${K.paint(wood)}"/><rect x="${x - 36}" y="${y + 9}" width="56" height="9" fill="${K.paint(K.darken(wood, 0.2))}"/>`)
      + K.path(body, "none", `stroke="${K.OUTLINE}" stroke-width="1.6" fill="none"`)
      + K.path(`M${x + 16} ${y - 3.5}L${x + 36} ${y - 1.2}L${x + 36} ${y + 1.2}L${x + 16} ${y + 3.5}Z`, "#4a4c50", thin);
  }
  function trough(ctx, x, base, w, h) {
    ctx.emit(`<path d="M${n(x - w / 2 + 5)} ${n(base - h + 5)}H${n(x + w / 2 - 5)}" stroke="#4a6a7a" stroke-width="3" opacity=".7"/>`);
    steam(ctx, x, base - h, 4, w * 0.35);
    return block(x - w / 2, base - h, w, h, "#5a5450") + K.line(`M${n(x - w / 2 + 8)} ${n(base - h + 10)}h${n(w - 16)}`, "!" + INK, 1, 'opacity=".4"') + sideShade(ctx, rectD(x - w / 2, base - h, w, h), x - w / 2, base - h, w, h, 0.45);
  }
  function grindstone(ctx, x, base, r) {
    const wood = ctx.mat.wood;
    sparks(ctx, x - r * 0.7, base - r * 1.5, 10, 40);
    return bar(x - r, base, x - 4, base - r * 1.6, 5, wood, thin) + bar(x + r, base, x + 4, base - r * 1.6, 5, wood, thin)
      + K.circle(x, base - r * 1.6, r, "#8a847a") + K.circle(x, base - r * 1.6, r * 0.75, "none", `stroke="${INK}" stroke-width="1" opacity=".4" fill="none"`)
      + K.circle(x, base - r * 1.6, 3.4, "#3a3632") + K.line(`M${n(x)} ${n(base - r * 1.6)}l${n(r * 0.8)} ${n(r * 0.2)}`, "#3a3632", 2.4);
  }
  function weaponRack(ctx, x, base, w) {
    const wood = ctx.mat.beam;
    let out = block(x - w / 2, base - 74, 7, 74, wood) + block(x + w / 2 - 7, base - 74, 7, 74, wood) + block(x - w / 2 - 4, base - 74, w + 8, 6, wood);
    const kinds = ["sword", "spear", "axe", "sword", "halberd", "mace", "sword", "spear"];
    for (let px = x - w / 2 + 16, index = 0; px < x + w / 2 - 10; px += ctx.r(14, 20), index++) {
      const kind = kinds[(index + Math.floor(ctx.r(0, 3))) % kinds.length], len = kind === "spear" || kind === "halberd" ? ctx.r(118, 132) : ctx.r(70, 86);
      out += weapon(kind, px, base - 2, len, ctx.r(-5, 5));
    }
    return out + block(x - w / 2 - 4, base - 44, w + 8, 5, wood, thin);
  }
  function toolRail(ctx, x, y, w) {
    let out = block(x, y - 3, w, 6, ctx.mat.beam, thin);
    for (let px = x + 14; px < x + w - 10; px += ctx.r(20, 28)) {
      const kind = ctx.pick(["tongs", "hammer", "shoe", "shoe", "file"]);
      if (kind === "tongs") out += K.line(`M${n(px)} ${n(y)}l-3 34M${n(px)} ${n(y)}l3 34`, "#3a3632", 1.8);
      else if (kind === "hammer") out += K.line(`M${n(px)} ${n(y)}V${n(y + 30)}`, "#5a3e28", 3) + block(px - 7, y + 26, 14, 6, "#4a4c50", thin);
      else if (kind === "shoe") out += K.path(`M${n(px - 6)} ${n(y + 18)}V${n(y + 8)}A6 6 0 0 1 ${n(px + 6)} ${n(y + 8)}V${n(y + 18)}`, "none", `stroke="#5a5a5a" stroke-width="3" fill="none"`) + K.line(`M${n(px)} ${n(y)}V${n(y + 2)}`, "#3a3632", 1);
      else out += K.line(`M${n(px)} ${n(y)}V${n(y + 28)}`, "#6a6e74", 2.6);
    }
    return out;
  }

  /* ---------- Magic ---------- */
  function crystalBall(ctx, x, base, r, color = "#b48cff") {
    const cy = base - 8 - r;
    ctx.emit(`<circle cx="${n(x)}" cy="${n(cy)}" r="${n(r)}" fill="#1a1428"/><circle cx="${n(x)}" cy="${n(cy)}" r="${n(r)}" fill="url(#${ctx.gradient(color)})"/><circle cx="${n(x - r * 0.35)}" cy="${n(cy - r * 0.4)}" r="${n(r * 0.18)}" fill="#fff" opacity=".7"/><circle cx="${n(x)}" cy="${n(cy)}" r="${n(r)}" fill="none" stroke="${K.OUTLINE}" stroke-width="1.4"/>`);
    ctx.glow(x, cy, r * 3.2, color, 0.6, "sb-breathe");
    ctx.light(x, cy, r * 7, 0.55, color);
    return K.path(`M${n(x - r * 0.8)} ${n(base)}L${n(x - r * 0.5)} ${n(base - 9)}H${n(x + r * 0.5)}L${n(x + r * 0.8)} ${n(base)}Z`, "#8a6a32", thin);
  }
  function pedestal(ctx, x, base, h, w = 34) {
    const d = `M${n(x - w / 2)} ${n(base)}H${n(x + w / 2)}L${n(x + w * 0.3)} ${n(base - 10)}V${n(base - h + 10)}L${n(x + w / 2)} ${n(base - h)}H${n(x - w / 2)}L${n(x - w * 0.3)} ${n(base - h + 10)}V${n(base - 10)}Z`;
    return K.path(d, ctx.mat.stone) + sideShade(ctx, d, x - w / 2, base - h, w, h, 0.5);
  }
  function roseWindow(ctx, cx, cy, R, stone) {
    let glass = "", tracery = "";
    for (let index = 0; index < 12; index++) {
      const a1 = index / 12 * Math.PI * 2, a2 = (index + 1) / 12 * Math.PI * 2;
      glass += `<path d="M${n(cx)} ${n(cy)}L${n(cx + Math.cos(a1) * R)} ${n(cy + Math.sin(a1) * R)}A${n(R)} ${n(R)} 0 0 1 ${n(cx + Math.cos(a2) * R)} ${n(cy + Math.sin(a2) * R)}Z" fill="${K.mix(GLASS[index % GLASS.length], "#3a2a2a", 0.15)}"/>`;
      tracery += `M${n(cx + Math.cos(a1) * R * 0.3)} ${n(cy + Math.sin(a1) * R * 0.3)}L${n(cx + Math.cos(a1) * R)} ${n(cy + Math.sin(a1) * R)}`;
      const am = (a1 + a2) / 2;
      glass += `<circle cx="${n(cx + Math.cos(am) * R * 0.66)}" cy="${n(cy + Math.sin(am) * R * 0.66)}" r="${n(R * 0.13)}" fill="${GLASS[(index + 3) % GLASS.length]}"/>`;
      tracery += `M${n(cx + Math.cos(am) * R * 0.66 + R * 0.13)} ${n(cy + Math.sin(am) * R * 0.66)}A${n(R * 0.13)} ${n(R * 0.13)} 0 1 1 ${n(cx + Math.cos(am) * R * 0.66 - R * 0.13)} ${n(cy + Math.sin(am) * R * 0.66)}A${n(R * 0.13)} ${n(R * 0.13)} 0 1 1 ${n(cx + Math.cos(am) * R * 0.66 + R * 0.13)} ${n(cy + Math.sin(am) * R * 0.66)}`;
    }
    glass += `<circle cx="${n(cx)}" cy="${n(cy)}" r="${n(R * 0.3)}" fill="#e0b050"/><path d="${K.star(cx, cy, R * 0.24, R * 0.09, 6)}" fill="#fff2c0"/>`;
    const lead = `<path d="${tracery}" stroke="#1a1410" stroke-width="2.6" fill="none"/><circle cx="${n(cx)}" cy="${n(cy)}" r="${n(R * 0.3)}" stroke="#1a1410" stroke-width="2.6" fill="none"/>` + K.hatch(cx - R, cy - R, R * 2, R * 2, 45, 7, 0.6, 0.35, "#1a1410");
    const disc = `M${n(cx - R)} ${n(cy)}A${n(R)} ${n(R)} 0 1 1 ${n(cx + R)} ${n(cy)}A${n(R)} ${n(R)} 0 1 1 ${n(cx - R)} ${n(cy)}Z`;
    ctx.emit(`<g opacity="${ctx.night ? 0.72 : 1}">${K.clipped(disc, glass + lead)}</g>`);
    ctx.glow(cx, cy, R * 2.2, ctx.night ? "#9a8ad8" : "#f0d8a8", 0.42, "sb-breathe");
    ctx.light(cx, cy, R * 4.4, ctx.night ? 0.7 : 0.95, ctx.night ? "#a898e8" : "#f0d0a0");
    let joints = "";
    for (let index = 0; index < 20; index++) { const a = index / 20 * Math.PI * 2; joints += `M${n(cx + Math.cos(a) * R)} ${n(cy + Math.sin(a) * R)}L${n(cx + Math.cos(a) * (R + 14))} ${n(cy + Math.sin(a) * (R + 14))}`; }
    return K.circle(cx, cy, R + 14, K.lighten(stone, 0.08)) + K.line(joints, "!" + INK, 1.2, 'opacity=".7"') + K.circle(cx, cy, R, "#1a1410");
  }
  function magicCircle(ctx, cx, cy, rx, ry, color = "#8fd6ff") {
    const point = (angle, scale = 1) => [cx + Math.cos(angle) * rx * scale, cy + Math.sin(angle) * ry * scale];
    let star = "";
    for (const offset of [0, Math.PI / 3]) star += poly([0, 1, 2].map(index => point(offset - Math.PI / 2 + index * Math.PI * 2 / 3, 0.8)));
    let runes = "";
    for (let index = 0; index < 28; index++) { const angle = index / 28 * Math.PI * 2, [x1, y1] = point(angle, 0.86), [x2, y2] = point(angle, 0.94); runes += `M${n(x1)} ${n(y1)}L${n(x2)} ${n(y2)}`; }
    ctx.live(`<g class="sb-pulse" fill="none" stroke="${color}" stroke-linecap="round"><ellipse cx="${n(cx)}" cy="${n(cy)}" rx="${n(rx)}" ry="${n(ry)}" stroke-width="2.4"/><ellipse cx="${n(cx)}" cy="${n(cy)}" rx="${n(rx * 0.8)}" ry="${n(ry * 0.8)}" stroke-width="1.4"/><path d="${star}" stroke-width="1.3"/><path d="${runes}" stroke-width="1.6"/></g>`);
    ctx.glows.push(`<ellipse class="sb-breathe" cx="${n(cx)}" cy="${n(cy)}" rx="${n(rx * 1.3)}" ry="${n(ry * 2.6)}" fill="url(#${ctx.gradient(color)})" opacity=".5"/>`);
    ctx.light(cx, cy - ry, rx * 1.5, 0.85, color);
  }
  function floatingTome(ctx, x, y, color = "#8fd6ff") {
    ctx.emit(K.path(`M${n(x - 26)} ${n(y + 4)}L${n(x - 24)} ${n(y - 6)}Q${n(x - 12)} ${n(y - 11)} ${n(x)} ${n(y - 5)}Q${n(x + 12)} ${n(y - 11)} ${n(x + 24)} ${n(y - 6)}L${n(x + 26)} ${n(y + 4)}Q${n(x + 12)} ${n(y - 2)} ${n(x)} ${n(y + 4)}Q${n(x - 12)} ${n(y - 2)} ${n(x - 26)} ${n(y + 4)}Z`, "!#4a2a2a", thin)
      + K.path(`M${n(x - 23)} ${n(y + 1)}L${n(x - 22)} ${n(y - 6)}Q${n(x - 11)} ${n(y - 10)} ${n(x)} ${n(y - 5)}Q${n(x + 11)} ${n(y - 10)} ${n(x + 22)} ${n(y - 6)}L${n(x + 23)} ${n(y + 1)}Q${n(x + 11)} ${n(y - 4)} ${n(x)} ${n(y + 1)}Q${n(x - 11)} ${n(y - 4)} ${n(x - 23)} ${n(y + 1)}Z`, "!#f2ead2", 'stroke-width=".8"')
      + K.line(`M${n(x - 18)} ${n(y - 5)}h12M${n(x - 18)} ${n(y - 2)}h10M${n(x + 6)} ${n(y - 5)}h12M${n(x + 6)} ${n(y - 2)}h10`, "!#6a5a8a", 0.8));
    ctx.glow(x, y - 4, 60, color, 0.75, "sb-breathe");
    ctx.light(x, y, 160, 0.7, color);
  }
  function orrery(ctx, x, top, y, r) {
    const brass = "#b08a3a";
    let rings = "";
    for (const [angle, scale] of [[-20, 1], [35, 0.78], [80, 0.6]]) rings += K.at(x, y, K.ellipse(0, 0, r * scale, r * scale * 0.32, "none", `stroke="${K.paint(brass)}" stroke-width="2" fill="none"`) + K.circle(r * scale, 0, 3, "#6a7a9a", 'stroke-width=".8"'), angle);
    ctx.emit(`<circle cx="${n(x)}" cy="${n(y)}" r="${n(r * 0.18)}" fill="#ffcf6a"/>`);
    ctx.glow(x, y, r * 1.2, "#ffc060", 0.6, "sb-breathe");
    ctx.light(x, y, r * 3.4, 0.5, "#ffc060");
    return K.line(`M${n(x)} ${n(top)}V${n(y - r * 0.18)}`, "#3a2e24", 1.4) + rings;
  }
  function motes(ctx, x, y, w, h, count, color) {
    let out = "";
    for (let index = 0; index < count; index++) out += `<circle class="sb-mote" style="animation-delay:-${o2(ctx.r(0, 6))}s" cx="${n(x + ctx.r(0, w))}" cy="${n(y + ctx.r(0, h))}" r="${n(ctx.r(0.8, 1.9))}" fill="${color}"/>`;
    ctx.live(out);
  }
  function cauldron(ctx, cx, base, r, brew = "#72d860") {
    const rim = base - r * 1.25;
    fire(ctx, cx, base + 2, r * 1.5, r * 0.55, { radius: r * 5 });
    const body = `M${n(cx - r)} ${n(rim)}C${n(cx - r * 1.12)} ${n(base - r * 0.3)} ${n(cx - r * 0.5)} ${n(base - r * 0.05)} ${n(cx)} ${n(base - r * 0.05)}C${n(cx + r * 0.5)} ${n(base - r * 0.05)} ${n(cx + r * 1.12)} ${n(base - r * 0.3)} ${n(cx + r)} ${n(rim)}Z`;
    ctx.emit(`<ellipse cx="${n(cx)}" cy="${n(rim + 1)}" rx="${n(r * 0.88)}" ry="${n(r * 0.15)}" fill="${brew}"/><circle cx="${n(cx - r * 0.3)}" cy="${n(rim)}" r="${n(r * 0.07)}" fill="#c8ffb0"/><circle cx="${n(cx + r * 0.25)}" cy="${n(rim + 1)}" r="${n(r * 0.05)}" fill="#c8ffb0"/>`);
    ctx.glow(cx, rim - 6, r * 2.6, brew, 0.6, "sb-breathe");
    ctx.light(cx, rim - 10, r * 5.5, 0.85, brew);
    steam(ctx, cx, rim - 4, 4, r * 0.5);
    return K.line(`M${n(cx - r * 0.7)} ${n(base - r * 0.3)}l-6 ${n(r * 0.36)}M${n(cx + r * 0.7)} ${n(base - r * 0.3)}l6 ${n(r * 0.36)}`, "#2a2826", 3.4)
      + K.path(body, "#2c2a28") + sideShade(ctx, body, cx - r, rim, r * 2, r * 1.2, 0.5) + K.ellipse(cx, rim, r * 1.02, r * 0.2, "none", `stroke="#3e3c3a" stroke-width="${n(r * 0.12)}" fill="none"`);
  }

  /* ---------- Shop interiors (item rework phase 4) ----------
     Full-window scenes, 1280 × 900. The shop panels cover the lower part of the window,
     so faces, counters, racks and lights sit in the band above them (y ≈ 130–360); the
     floor and the nearer bodies show faintly through the panels. */
  const zoom = (x, y, s, markup) => `<g transform="translate(${n(x)} ${n(y)}) scale(${s}) translate(${n(-x)} ${n(-y)})">${markup}</g>`;
  // A pointed recess in a stone wall, a little darker than the wall around it.
  function recess(ctx, cx, base, w, h, stone = ctx.mat.stone) {
    const d = archPath(cx, base, w, h);
    return K.path(d, K.darken(stone, 0.22)) + K.clipped(d, masonry(ctx, cx - w / 2, base - h, w, h, K.darken(stone, 0.22), { row: 30 }) + K.hatch(cx - w / 2, base - h, w, h * 0.35, 60, 3.2, 0.6, 0.45))
      + K.path(archPath(cx, base, w + 18, h + 10), "none", `stroke="${K.paint(K.lighten(stone, 0.06))}" stroke-width="9" fill="none"`)
      + K.path(d, "none", `stroke="${K.OUTLINE}" stroke-width="2.2" fill="none"`) + K.line(archJoints(cx, base, w, h, 10), "!" + INK, 1, 'opacity=".6"');
  }
  // A shield with two weapons crossed behind it.
  function trophyArms(ctx, x, y, r, color, kind = "heater", weapons = "sword") {
    const len = r * 3.2;
    return weapon(weapons, x - r * 0.9, y + r * 1.3, len, 38) + weapon(weapons, x + r * 0.9, y + r * 1.3, len, -38) + shield(ctx, x, y, r, color, kind);
  }
  // Weapons hung across pegs on a board, in rows.
  function weaponBoard(ctx, x, y, w, h, rows) {
    let out = block(x, y, w, h, K.darken(ctx.mat.wood, 0.3)) + K.clipped(rectD(x, y, w, h), K.hatch(x, y, w, h, 80, 5, 0.6, 0.35));
    for (let row = 0; row < rows; row++) {
      const cy = y + h * (row + 0.5) / rows, kind = ctx.pick(["sword", "sword", "axe", "mace", "sword"]);
      out += K.circle(x + 18, cy + 6, 3, "#3a2e24", thin) + K.circle(x + w - 18, cy + 6, 3, "#3a2e24", thin);
      out += K.at(x + w / 2 - (kind === "sword" ? (w - 40) / 2 : (w - 60) / 2), cy, WEAPONS[kind](kind === "sword" ? w - 40 : w - 60), 90);
    }
    return out + K.path(rectD(x, y, w, h), "none", `stroke="${K.paint(ctx.mat.beam)}" stroke-width="6" fill="none"`);
  }
  // Bows and quivers hanging from pegs.
  function bowWall(ctx, x, y, w, count) {
    let out = block(x - 6, y - 8, w + 12, 8, ctx.mat.beam, thin);
    for (let index = 0; index < count; index++) {
      const bx = x + 18 + index * (w - 36) / Math.max(1, count - 1), len = ctx.r(120, 170), belly = len * 0.16;
      out += K.line(`M${n(bx)} ${n(y)}V${n(y + 8)}`, "#2a2420", 1.4)
        + K.path(`M${n(bx)} ${n(y + 8)}Q${n(bx + belly * 2)} ${n(y + 8 + len / 2)} ${n(bx)} ${n(y + 8 + len)}`, "none", `stroke="${K.paint(ctx.pick(["#6a4a2a", "#7a5a34", "#4a3424"]))}" stroke-width="5" fill="none"`)
        + K.line(`M${n(bx)} ${n(y + 8)}V${n(y + 8 + len)}`, "#d8ccb0", 0.9);
    }
    return out;
  }
  // Staffs standing in a rack, their stones glowing.
  function staffRack(ctx, x, base, w, count, glows = ["#8fd6ff", "#ff7a5a", "#9aff8a", "#c890ff", "#ffe27a", "#7ae0e0"]) {
    let out = block(x - w / 2, base - 18, w, 18, ctx.mat.wood) + block(x - w / 2, base - 170, w, 10, ctx.mat.wood, thin);
    for (let index = 0; index < count; index++) {
      const sx = x - w / 2 + 16 + index * (w - 32) / Math.max(1, count - 1), top = base - ctx.r(230, 280), color = glows[index % glows.length];
      out += K.line(`M${n(sx)} ${n(base - 4)}L${n(sx + ctx.r(-3, 3))} ${n(top)}`, ctx.pick(["#5a3e28", "#7a5a34", "#3a2a1c", "#c8b890"]), 4.4);
      out += K.path(`M${n(sx - 7)} ${n(top + 4)}L${n(sx)} ${n(top - 10)}L${n(sx + 7)} ${n(top + 4)}L${n(sx)} ${n(top + 10)}Z`, "#8a6a32", thin);
      ctx.live(`<circle class="sb-breathe" cx="${n(sx)}" cy="${n(top - 2)}" r="5.5" fill="${color}"/>`);
      ctx.glow(sx, top - 2, 34, color, 0.8, "sb-breathe");
    }
    ctx.light(x, base - 240, w * 1.4, 0.6, "#a8c8ff");
    return out;
  }
  // Rune stones glowing on a carver's bench.
  function runeStones(ctx, x, top, count) {
    let out = "";
    const art = window.ForgeArt, ids = ["cinder", "still", "charge", "gaia", "night", "heft", "precision"];
    for (let index = 0; index < count; index++) {
      const rx = x + index * 34, id = ids[index % ids.length];
      if (art?.runeStone) out += `<g transform="translate(${n(rx)} ${n(top - 15)}) scale(.8)">${art.runeStone(id)}</g>`;
      ctx.glow(rx, top - 16, 30, art?.runeColor?.(id) || "#9fd8ff", 0.6, "sb-breathe");
    }
    ctx.light(x + count * 17, top - 20, 220, 0.7, "#a8d8ff");
    return out;
  }
  // The countryside seen through an open door or window.
  function countryside(ctx, x, y, w, h) {
    const night = ctx.night, ground = night ? "!#141a22" : "!#6a7a52";
    return solid(x, y, w, h, "!" + ctx.sky.glass) + solid(x, y, w, h * 0.45, "!" + ctx.sky.deep, 'opacity=".5"')
      + K.path(`M${x} ${y + h * 0.62}Q${x + w * 0.3} ${y + h * 0.48} ${x + w * 0.62} ${y + h * 0.58}T${x + w} ${y + h * 0.54}V${y + h}H${x}Z`, night ? "!#10161c" : "!#7e8c5e", 'stroke="none"')
      + K.path(`M${x} ${y + h * 0.74}Q${x + w * 0.5} ${y + h * 0.66} ${x + w} ${y + h * 0.72}V${y + h}H${x}Z`, ground, 'stroke="none"')
      + K.path(`M${x + w * 0.72} ${y + h * 0.66}V${y + h * 0.42}`, "none", `stroke="${night ? "#0c0e12" : "#4a3a28"}" stroke-width="7"`) + K.circle(x + w * 0.72, y + h * 0.36, w * 0.13, night ? "!#0e1418" : "!#4e6a3a", 'stroke="none"')
      + K.line(`M${x} ${y + h * 0.8}H${x + w}M${x + w * 0.12} ${y + h * 0.75}V${y + h * 0.88}M${x + w * 0.4} ${y + h * 0.74}V${y + h * 0.87}M${x + w * 0.68} ${y + h * 0.75}V${y + h * 0.88}`, night ? "!#0c0e12" : "!#4a3a26", 3)
      + (night ? `<circle cx="${n(x + w * 0.25)}" cy="${n(y + h * 0.2)}" r="10" fill="#e8e4d0" opacity=".85"/>` : "");
  }

  function armsHall(ctx) {
    const m = ctx.mat, F = 430;
    ctx.keyX = 640; ctx.ambient = 0.6;
    let out = masonry(ctx, 0, 0, W, F, m.stone, { row: 32 });
    out += solid(0, 0, W, 56, "!" + INK, 'opacity=".4"') + K.hatch(0, 0, W, 56, 30, 4, 0.7, 0.45);
    // Side bays: armor, a weapon wall, bows and polearms, and shields.
    out += recess(ctx, 128, F, 200, 380) + recess(ctx, 378, F, 200, 380) + recess(ctx, 902, F, 200, 380) + recess(ctx, 1152, F, 200, 380);
    out += trophyArms(ctx, 128, 150, 34, m.cloth, "heater") + shield(ctx, 84, 236, 24, "#5a4a3a", "round") + shield(ctx, 172, 236, 24, "#3d4a5c", "round");
    out += weaponBoard(ctx, 298, 100, 160, 200, 4);
    out += bowWall(ctx, 830, 96, 144, 5);
    out += trophyArms(ctx, 1152, 150, 34, "#3d4a5c", "heater", "axe") + shield(ctx, 1108, 236, 24, m.cloth, "round") + shield(ctx, 1196, 236, 24, "#6a5440", "heater");
    // The forge, seen through the great arch behind the counter.
    // Fire draws above the scene, so the forge sits high in the arch and the centre stays clear of people.
    let forgeRoom = forge(ctx, 604, F - 74, 150, 196, { top: F - 252 }) + anvil(ctx, 714, F - 72, 1) + person(ctx, 750, F - 70, 140, { face: -1, pose: "hammer", hold: "hammer", apron: true, head: "bald", beard: true });
    sparks(ctx, 710, F - 118, 10, 34);
    out += opening(ctx, 640, F, 236, 270, m.stone, forgeRoom);
    out += roseWindow(ctx, 640, 92, 46, m.stone);
    out += pillar(ctx, 256, -4, F, 46) + pillar(ctx, 1024, -4, F, 46) + pillar(ctx, 506, -4, F, 30) + pillar(ctx, 774, -4, F, 30);
    out += hangingBanner(ctx, 256, 46, 62, 150, m.cloth, "swords") + hangingBanner(ctx, 1024, 46, 62, 150, m.cloth, "anvil");
    out += chandelier(ctx, 380, -2, 170, 150, 8) + chandelier(ctx, 900, -2, 170, 150, 8);
    out += floor(ctx, F, m.floor, "flag", 640);
    // Racks and stands along the walls.
    out += zoom(378, 466, 1.7, weaponRack(ctx, 378, 466, 170));
    out += zoom(902, 462, 1.6, weaponRack(ctx, 902, 462, 160));
    out += armorStand(ctx, 120, 520, 2.1) + armorStand(ctx, 1206, 520, 2.1, "#6e7278");
    // The clerks behind the long counter.
    out += person(ctx, 470, 456, 212, { face: 1, pose: "inspect", hold: "sword", apron: true, head: "short", beard: true, color: "#5a4a3a" });
    out += person(ctx, 836, 456, 204, { face: -1, pose: "counter", apron: "#4a3a2a", head: "bald", color: "#3d4a5c" });
    out += counter(ctx, 330, 350, 620, 104, K.darken(m.wood, 0.05));
    out += zoom(700, 350, 1.8, ledger(700, 350, 26) + coins(740, 350, 4)) + K.at(560, 344, swordUp(120), 90) + candle(ctx, 936, 350, 20, 7, 0.4);
    // Adventurers browsing, from the back of the hall to the front.
    out += person(ctx, 330, 505, 232, { face: -1, pose: "point", cloak: "#4a5a3a", head: "hood", hoodColor: "#3e4e32", pack: true });
    out += person(ctx, 476, 540, 258, { face: 1, back: true, pose: "counter", cloak: "#5a3a5a", head: "long", hair: "#7a5530" });
    out += person(ctx, 1012, 546, 262, { face: -1, pose: "hold", hold: "staff", robe: "#3d4a5c", head: "wide", hatColor: "#2a2430", beard: "long", hair: "#c8c0b0" });
    out += person(ctx, 868, 566, 206, { face: -1, pose: "fold", armor: true, head: "helm", beard: "long", hair: "#8a3a1e", tabard: "#6b2a24" });
    out += person(ctx, 200, 612, 300, { face: 1, pose: "inspect", hold: "sword", armor: true, tabard: m.cloth, head: "helm", cloak: "#3d4a5c" });
    out += person(ctx, 1110, 606, 284, { face: -1, pose: "point", cloak: "#2a2a30", head: "hood", hoodColor: "#2a2a30", sword: true });
    out += person(ctx, 330, 666, 306, { face: 1, pose: "hold", cloak: "#6b2a24", head: "short", pack: true, sword: true });
    out += person(ctx, 846, 706, 336, { face: -1, pose: "carry", hold: "box", walk: true, apron: "#4a3a2a", head: "cap" });
    out += barrel(ctx, 1240, 760, 90, 110) + crate(ctx, 40, 780, 120, 90);
    return out;
  }

  function countrySmithy(ctx) {
    const m = ctx.mat, F = 440, beam = 70;
    ctx.keyX = 360; ctx.ambient = 0.62;
    let out = timberWall(ctx, 0, beam, W, 150, 260) + masonry(ctx, 0, beam + 150, W, F - beam - 150, m.stone, { row: 30, min: 1.1, max: 2.1 });
    // The open door, with the countryside beyond.
    const dx = 940, dw = 210, dtop = 168;
    const outside = countryside(ctx, dx, dtop, dw, F - dtop);
    if (ctx.sky.lit) { ctx.emit(outside); ctx.light(dx + dw / 2, F - 80, 420, 0.8, ctx.sky.light); shaft(ctx, [[dx, F], [dx + dw, F], [dx + dw + 90, H], [dx - 260, H]], 0.3); }
    else out += outside;
    out += block(dx - 16, dtop - 16, dw + 32, 18, m.beam) + block(dx - 16, dtop, 16, F - dtop, m.beam) + block(dx + dw, dtop, 16, F - dtop, m.beam);
    out += K.path(`M${dx + 92} ${dtop - 30}V${dtop - 40}A13 13 0 1 1 ${dx + 118} ${dtop - 40}V${dtop - 30}`, "none", `stroke="#7a7a7a" stroke-width="5" fill="none"`);
    out += ceiling(ctx, beam, 520);
    out += forge(ctx, 330, F, 300, 300, { top: beam + 14 });
    out += zoom(150, F, 1.7, bellows(ctx, 150, F, F + (beam + 20 - F) / 1.7));
    out += zoom(560, 150, 1.5, toolRail(ctx, 560, 150, 230)) + shield(ctx, 880, 230, 32, m.cloth, "heater") + shield(ctx, 880, 320, 26, "#5a4a3a", "round");
    out += herbs(ctx, 1200, beam + 20, beam + 40) + herbs(ctx, 1230, beam + 20, beam + 36, "#6a5a3a");
    out += floor(ctx, F, K.darken(m.floor, 0.1), "dirt", 520);
    out += zoom(820, 470, 1.6, weaponRack(ctx, 820, 470, 120));
    out += K.line("M1196 470L1230 230", "#6a4a30", 5) + K.path("M1212 238Q1260 250 1254 300", "none", `stroke="#9a9ea4" stroke-width="5" fill="none"`) + K.line("M1170 474L1186 250M1180 250L1192 254M1186 250V236", "#6a4a30", 4);
    out += barrel(ctx, 520, 520, 86, 96, m.wood) + `<ellipse cx="520" cy="428" rx="37" ry="4.6" fill="${K.paint("#3a4a54")}"/>`;
    steam(ctx, 520, 420, 3, 18);
    out += person(ctx, 220, 520, 234, { face: 1, pose: "work", apron: "#5a4a3a", head: "short", color: "#7a6a4a" });
    out += anvil(ctx, 660, 556, 1.9);
    sparks(ctx, 648, 466, 18, 90);
    out += person(ctx, 778, 580, 280, { face: -1, pose: "hammer", hold: "hammer", apron: true, head: "bald", beard: true, color: "#6a5440" });
    out += dog(ctx, 430, 600, 1.9, 1);
    out += person(ctx, 892, 600, 262, { face: -1, pose: "hold", hold: "basket", head: "wide", hatColor: "#a08a5a", color: "#6a5a3a" });
    let logs = "";
    for (let row = 0; row < 4; row++) for (let col = 0; col < 4 - (row > 2 ? 1 : 0); col++) logs += K.circle(1160 + col * 28 + (row % 2) * 14, 700 - row * 26, 13, "#6a4a30", thin) + K.circle(1160 + col * 28 + (row % 2) * 14, 700 - row * 26, 5, "#a07a50", 'stroke-width=".6"');
    out += logs + block(40, 640, 120, 60, "#5a4030") + K.ellipse(100, 640, 60, 10, "#7a5a40") + weapon("axe", 122, 640, 76, 28);
    return out;
  }

  function grandEmporium(ctx) {
    const m = ctx.mat, F = 430, G = 120;
    ctx.keyX = 640; ctx.ambient = 0.6;
    let out = boards(ctx, 0, 0, W, F, K.darken(m.wood, 0.25), { width: 52 });
    out += windowPane(ctx, 560, 18, 160, 150, { arch: "round", frame: m.beam, bars: 2 });
    // Tall shelves to the rafters, with a ladder.
    out += shelving(ctx, 20, 30, 330, 360, 6, goods) + shelving(ctx, 930, 30, 330, 360, 6, goods);
    out += shelving(ctx, 360, 190, 170, 200, 3, goods) + shelving(ctx, 750, 190, 170, 200, 3, goods);
    out += shelving(ctx, 560, 196, 160, 64, 1, potions);
    out += ladder(ctx, 330, 430, 360, 70, 34);
    // A beam hung with the road's necessities.
    out += block(-4, G, W + 8, 14, m.beam) + solid(0, G + 14, W, 6, "!" + INK, 'opacity=".45"');
    for (const [x, kind] of [[60, "herbs"], [130, "lantern"], [210, "rope"], [290, "pot"], [450, "sausage"], [540, "herbs"], [740, "herbs"], [830, "sausage"], [990, "rope"], [1070, "lantern"], [1150, "pot"], [1220, "herbs"]]) {
      const top = G + 14;
      if (kind === "herbs") out += zoom(x, top, 1.5, herbs(ctx, x - 8, top, top + 10) + herbs(ctx, x + 8, top, top + 14, "#6a5a3a"));
      else if (kind === "lantern") out += lantern(ctx, x, top, top + 56, 1.4);
      else if (kind === "rope") out += K.line(`M${x} ${top}V${top + 16}`, "#4a3a2a", 1.4) + K.ellipse(x, top + 38, 20, 20, "none", `stroke="${K.paint("#9a8460")}" stroke-width="8" fill="none"`);
      else if (kind === "pot") out += hangingPot(ctx, x, top, top + 44, 16);
      else out += zoom(x, top, 1.6, sausages(ctx, x, top, top + 10));
    }
    out += chandelier(ctx, 640, 180, 214, 120, 7);
    out += floor(ctx, F, m.floor, "plank", 640);
    // Merchant and clerk behind the counter.
    out += person(ctx, 560, 456, 206, { face: 1, pose: "counter", apron: true, head: "cap", beard: true, color: "#3d4a5c" });
    out += person(ctx, 820, 456, 198, { face: -1, pose: "wipe", apron: "#6a5440", head: "kerchief", hatColor: "#7a2a24", hair: "#4a3020" });
    out += counter(ctx, 400, 350, 500, 104);
    out += zoom(470, 350, 1.8, scales(ctx, 470, 350)) + zoom(660, 350, 1.8, ledger(660, 350) + coins(700, 350, 5)) + candle(ctx, 418, 350, 18, 7, 0.4) + jar(760, 350, 32, 40, "#6a7a5a") + jar(800, 350, 26, 32, "#8a6a44");
    // Adventurers stocking up.
    out += person(ctx, 346, 492, 228, { face: 1, pose: "reach", apron: true, head: "short" });
    out += person(ctx, 660, 530, 252, { face: 1, back: true, pose: "counter", cloak: "#4a5a3a", head: "hood", hoodColor: "#3e4e32", pack: true });
    out += person(ctx, 1000, 540, 256, { face: -1, pose: "hold", hold: "basket", robe: "#5a4a6a", head: "kerchief", hatColor: "#7a6a4a" });
    out += person(ctx, 190, 586, 288, { face: 1, pose: "point", cloak: "#6b2a24", head: "short", pack: true, sword: true });
    out += person(ctx, 880, 600, 280, { face: -1, pose: "read", hold: "book", robe: "#3d4a5c", head: "hood", hoodColor: "#2e3a4a" });
    out += person(ctx, 1150, 640, 300, { face: -1, pose: "fold", armor: true, tabard: m.cloth, head: "helm", cloak: "#3d4a5c" });
    out += person(ctx, 520, 690, 330, { face: 1, pose: "carry", hold: "box", walk: true, apron: "#4a3a2a", head: "cap" });
    out += sack(ctx, 70, 700, 80, 90) + sack(ctx, 140, 720, 70, 76, "#8a7452") + barrel(ctx, 1240, 740, 90, 110) + crate(ctx, 1150, 820, 120, 90) + crate(ctx, 60, 840, 130, 90, "#7a5a3a");
    return out;
  }

  function countryStore(ctx) {
    const m = ctx.mat, F = 440, beam = 70;
    ctx.keyX = 700; ctx.ambient = 0.6;
    let out = boards(ctx, 0, beam, W, F - beam, m.wood, { width: 44 });
    out += windowPane(ctx, 990, 150, 190, 170, { frame: m.beam, bars: 1 });
    out += ceiling(ctx, beam, 640);
    out += zoom(160, beam + 20, 1.6, herbs(ctx, 140, beam + 20, beam + 34) + herbs(ctx, 170, beam + 20, beam + 30, "#7a6a3a") + herbs(ctx, 200, beam + 20, beam + 36)) + zoom(330, beam + 20, 1.6, sausages(ctx, 330, beam + 20, beam + 30)) + hangingPot(ctx, 1230, beam + 20, beam + 70, 16);
    out += shelving(ctx, 420, 120, 440, F - 120, 5, goods);
    out += lantern(ctx, 920, beam + 20, beam + 90, 1.4) + lantern(ctx, 280, beam + 20, beam + 84, 1.2);
    // A small iron stove keeps the room warm.
    out += block(118, 300, 110, 140, "#3a3634") + K.path("M140 400H206V430H140Z", "!#120b08") + block(160, beam + 16, 22, 230, "#2e2a28") + block(110, 296, 126, 10, "#4a4644");
    fire(ctx, 173, 428, 52, 24, { radius: 220, strength: 0.7 });
    out += floor(ctx, F, m.floor, "plank", 640);
    out += person(ctx, 760, 456, 206, { face: -1, pose: "counter", apron: true, head: "bald", beard: "long", color: "#6a5440", hair: "#c8c0b0" });
    out += counter(ctx, 520, 350, 460, 104);
    out += cat(ctx, 600, 350, 1.8, 1) + candle(ctx, 950, 350, 20, 7, 0.45) + jar(650, 350, 34, 42, "#8a6a44") + zoom(880, 350, 1.6, scales(ctx, 880, 350, 0.9) + coins(830, 350, 3));
    out += barrel(ctx, 90, 560, 100, 116) + barrel(ctx, 210, 580, 94, 108, K.darken(m.wood, 0.1)) + sack(ctx, 320, 590, 76, 84) + sack(ctx, 390, 600, 66, 70, "#8a7452");
    out += person(ctx, 1050, 590, 270, { face: -1, pose: "hold", hold: "basket", robe: "#4a5a3a", head: "kerchief", hatColor: "#6b2a24" });
    out += person(ctx, 480, 640, 300, { face: 1, pose: "point", cloak: "#5a4a3a", head: "wide", hatColor: "#4a3a2a", pack: true });
    out += dog(ctx, 820, 680, 1.8, -1);
    return out;
  }

  function towerSanctum(ctx) {
    const m = ctx.mat, F = 440;
    ctx.keyX = 640; ctx.ambient = 0.7;
    const stone = K.darken(m.stone, 0.18);
    let out = masonry(ctx, 0, 0, W, F, stone, { row: 26 });
    // A tall lancet window above the archmage, and the rose above it.
    out += K.path(archPath(640, 334, 182, 272), K.lighten(stone, 0.06)) + windowPane(ctx, 565, 80, 150, 250, { arch: "point", frame: "#3a3430", bars: 1 });
    out += roseWindow(ctx, 640, 40, 34, stone);
    // Curved bookcases climbing the tower walls, with a gallery rail.
    out += shelving(ctx, -10, 20, 400, F - 20, 8, books) + shelving(ctx, 890, 20, 400, F - 20, 8, books);
    out += balustrade(ctx, -6, 150, 400, 30) + balustrade(ctx, 886, 150, 400, 30) + block(-6, 180, 400, 8, m.wood) + block(886, 180, 400, 8, m.wood);
    out += person(ctx, 140, 180, 120, { face: 1, pose: "read", hold: "book", robe: "#3d4a5c", head: "hood" }) + person(ctx, 1150, 180, 118, { face: -1, pose: "reach", robe: "#5a4a6a", head: "short" });
    out += pillar(ctx, 420, -4, F, 40) + pillar(ctx, 860, -4, F, 40);
    out += orrery(ctx, 790, -2, 100, 44);
    out += floor(ctx, F, K.darken(m.floor, 0.18), "flag", 640);
    magicCircle(ctx, 640, 600, 420, 66, "#8fd6ff");
    magicCircle(ctx, 640, 470, 150, 18, "#c8a0ff");
    // The runecarver's bench on the left, the staff display on the right.
    out += table(ctx, 330, 360, 220, m.wood, 70) + runeStones(ctx, 248, 360, 6);
    out += person(ctx, 196, 432, 214, { face: 1, pose: "work", robe: "#2f4a4a", head: "short", beard: true, hair: "#a08a6a" });
    out += staffRack(ctx, 1000, 470, 190, 6);
    // The archmage at the lectern.
    out += person(ctx, 640, 456, 214, { face: -1, pose: "read", robe: "#3a2a5a", head: "wide", hatColor: "#2a2440", beard: "long", hair: "#d8d0c0" });
    out += K.path("M600 350L680 350L692 336L588 336Z", m.wood) + block(626, 350, 28, 106, K.darken(m.wood, 0.2)) + block(600, 452, 80, 10, m.wood, thin) + K.path("M598 334L682 334L676 326L604 326Z", "#e2d6bc", thin);
    out += pedestal(ctx, 120, 520, 66, 46) + crystalBall(ctx, 120, 454, 18, "#b48cff") + pedestal(ctx, 1180, 524, 66, 46) + crystalBall(ctx, 1180, 458, 17, "#7ae0c0");
    floatingTome(ctx, 470, 230, "#c8a0ff"); floatingTome(ctx, 820, 220);
    for (let index = 0; index < 12; index++) out += candle(ctx, 460 + ctx.r(0, 360), ctx.r(60, 300), ctx.r(10, 16), 4.4, 0.25);
    // Mages and adventurers browsing.
    out += person(ctx, 860, 540, 252, { face: -1, pose: "cast", hold: "orb", robe: m.cloth, head: "hood", hoodColor: K.darken(m.cloth, 0.15) });
    out += person(ctx, 420, 560, 262, { face: 1, pose: "point", cloak: "#2f4a4a", head: "short", pack: true });
    out += person(ctx, 1110, 620, 296, { face: -1, pose: "hold", hold: "staff", robe: "#4a2a2a", head: "witch", hatColor: "#2a1e2a", hair: "#2a1d14" });
    out += person(ctx, 560, 676, 316, { face: 1, pose: "fold", cloak: "#3d4a5c", head: "hood", hoodColor: "#2e3a4a", sword: true });
    motes(ctx, 300, 120, 680, 380, 48, "#bfe8ff");
    return out;
  }

  /* ---------- The Church of Talos ---------- */
  // The Storm Reliquary behind the altar: a dark chapel under a storm window that never clears.
  // The Church's staffs stand on the left, its wands lie in a case on the right, and the
  // Stormhide Cuirass hangs on its stand in the light of the storm.
  function stormChapel(ctx) {
    const m = ctx.mat, F = 440, gold = "#e0c060", blood = "#6a1616", vestment = "#2e3240", spark = "#bfe0ff";
    ctx.keyX = 640; ctx.ambient = 0.78;
    const stone = K.darken(m.stone, 0.34);
    let out = masonry(ctx, 0, 0, W, F, stone, { row: 26 });
    // The storm window: rain on the glass and lightning beyond it, at any hour.
    out += K.path(archPath(640, 332, 214, 300), K.lighten(stone, 0.06)) + K.line(archJoints(640, 332, 214, 300, 16), "!" + INK, 1.2, 'opacity=".7"');
    const glassD = archPath(640, 322, 180, 282);
    const bolt = "M652 66L628 150L648 153L618 262L670 138L648 135L668 66Z";
    let clouds = "";
    for (let index = 0; index < 9; index++) clouds += `<ellipse cx="${n(560 + ctx.r(0, 160))}" cy="${n(70 + ctx.r(0, 90))}" rx="${n(ctx.r(30, 60))}" ry="${n(ctx.r(10, 18))}" fill="#3a4458" opacity=".55"/>`;
    ctx.emit(K.path(glassD, "!#1a2232", 'stroke="none"') + K.clipped(glassD, `<rect x="550" y="40" width="180" height="140" fill="#0c101a" opacity=".6"/>` + clouds
      + `<path d="${bolt}" fill="#e8f2ff" opacity=".35"/>` + K.hatch(550, 40, 180, 282, 100, 6, 0.8, 0.5, "#8fa4c0")
      + `<path d="M600 40V322M680 40V322M550 196H730" stroke="#14100c" stroke-width="3.4" fill="none"/>`)
      + K.path(glassD, "none", `stroke="${K.paint(K.darken(stone, 0.2))}" stroke-width="7" fill="none"`) + K.path(glassD, "none", `stroke="${K.OUTLINE}" stroke-width="1.6" fill="none"`));
    ctx.live(`<g class="sb-storm-flash"><path d="${bolt}" fill="#fff8d8"/><path d="${glassD}" fill="#cfe0ff" opacity=".35"/></g>`);
    let rain = "";
    for (let index = 0; index < 60; index++) { const x = 558 + ctx.r(0, 164), y = 80 + ctx.r(0, 230); rain += `M${n(x)} ${n(y)}l-2 14`; }
    ctx.live(`<path class="sb-rain" d="${rain}" stroke="#bcd0ec" stroke-width="1.1" opacity=".45" fill="none"/>`);
    ctx.glow(640, 180, 190, "#8fb4ff", 0.3, "sb-breathe");
    ctx.light(640, 180, 380, 0.8, "#9ab8f0");
    // The god's golden bolt above the window, and the cult's blood-red banners.
    out += emblem("bolt", 640, 14, 22, gold);
    out += hangingBanner(ctx, 470, 22, 74, 210, blood, "bolt", gold) + hangingBanner(ctx, 810, 22, 74, 210, blood, "bolt", gold);
    out += pillar(ctx, 400, -4, F, 40) + pillar(ctx, 880, -4, F, 40);
    out += floor(ctx, F, K.darken(m.floor, 0.32), "flag", 640);
    magicCircle(ctx, 640, 610, 400, 62, "#ffe66b");
    // Left: the Church's staffs, sapphire-headed, crackling.
    out += staffRack(ctx, 190, 468, 240, 7, ["#8fd6ff", "#ffe66b", spark]);
    out += person(ctx, 330, 470, 216, { face: -1, pose: "inspect", robe: vestment, head: "hood", hoodColor: blood });
    // The altar under the window, with the high priest who keeps the reliquary.
    out += person(ctx, 640, 452, 210, { face: 1, pose: "cast", hold: "orb", glowColor: "#ffe66b", robe: vestment, head: "hood", hoodColor: "#22242c", beard: "long", hair: "#c8c0b0" });
    const altar = "M548 452V392H732V452Z";
    out += K.path(altar, K.lighten(stone, 0.1)) + sideShade(ctx, altar, 548, 392, 184, 60, 0.45)
      + K.path("M540 392H740V402L724 438H556L540 402Z", blood) + K.path("M548 398H732", "none", `stroke="${K.paint(gold)}" stroke-width="1.6" fill="none"`)
      + emblem("bolt", 640, 418, 14, gold);
    for (const x of [560, 584, 696, 720]) out += candle(ctx, x, 392, ctx.r(12, 18), 4.4, 0.35);
    // Right: the wand case, and the Stormhide Cuirass on its stand.
    out += table(ctx, 1010, 392, 220, K.darken(m.wood, 0.15), 52);
    for (let index = 0; index < 6; index++) {
      const x = 918 + index * 36, tip = ctx.pick(["#8fd6ff", "#ffe66b", spark]);
      out += K.line(`M${n(x)} ${n(388)}L${n(x + 26)} ${n(380)}`, ctx.pick(["#4a3424", "#c8b890", "#2a2a34"]), 3);
      ctx.live(`<circle class="sb-breathe" cx="${n(x + 27)}" cy="${n(380)}" r="3" fill="${tip}"/>`);
      ctx.glow(x + 27, 380, 20, tip, 0.7, "sb-breathe");
    }
    out += person(ctx, 1090, 392, 200, { face: -1, pose: "counter", robe: vestment, head: "hood", hoodColor: blood });
    out += pedestal(ctx, 1150, 640, 40, 120) + armorStand(ctx, 1150, 600, 1.9, "#4e5a68");
    out += K.line("M1124 466L1136 488L1128 490L1142 518M1170 470L1162 492L1170 494L1158 520", "!" + gold, 2);
    ctx.glow(1150, 470, 120, "#8fd6ff", 0.55, "sb-breathe");
    ctx.light(1150, 470, 260, 0.8, "#9ad0ff");
    let crackle = "";
    for (let index = 0; index < 7; index++) { const angle = ctx.r(0, Math.PI * 2), reach = ctx.r(44, 72); crackle += `M${n(1150 + Math.cos(angle) * reach)} ${n(476 + Math.sin(angle) * reach * 1.3)}l${n(ctx.r(-6, 6))} 7l5 1l-4 8`; }
    ctx.live(`<path class="sb-sparks" d="${crackle}" stroke="${spark}" stroke-width="1.4" fill="none" stroke-linecap="round"/>`);
    // An Apostle at the door and pilgrims browsing.
    out += person(ctx, 80, 640, 300, { face: 1, pose: "fold", armor: true, tabard: blood, head: "helm" });
    out += person(ctx, 520, 640, 300, { face: 1, pose: "point", cloak: "#3d4a5c", head: "short", pack: true });
    out += person(ctx, 900, 670, 318, { face: -1, pose: "hold", hold: "staff", robe: "#2a3a4a", head: "witch", hatColor: "#1e2430", hair: "#2a1d14" });
    motes(ctx, 300, 100, 680, 360, 40, spark);
    return out;
  }

  /* ---------- The Tailor (port task 3) ----------
     PLACEHOLDER ART (user decision R1): a plain room of cloth bolts, a rail of garments and two dress forms;
     the port remakes it fully. The Institute of Magic and Hearth Home borrow the Magic Tower's sanctum and the
     castle smithy for now (also placeholders). */
  function clothBolts(ctx, x, base, w, h) {
    let out = "", at = x;
    while (at < x + w - 14) {
      const width = ctx.r(14, 22), height = Math.min(h, ctx.r(h * 0.6, h * 0.95));
      out += block(at, base - height, width, height, ctx.pick(["#7b2b25", "#2e4a6e", "#3c5b3a", "#b6a586", "#5a4a6a", "#d8ccb0", "#8a6a3a"]), 'stroke-width="1"')
        + K.line(`M${n(at + 3)} ${n(base - height + 4)}V${n(base - 4)}`, "!" + INK, 0.7, 'opacity=".35"');
      at += width + 2;
    }
    return out;
  }
  function dressForm(ctx, x, base, h, color) {
    const s = h / 100;
    return K.line(`M${n(x)} ${n(base)}V${n(base - 40 * s)}M${n(x - 12 * s)} ${n(base)}h${n(24 * s)}`, "#3a2e24", 2.4)
      + K.path(`M${n(x - 16 * s)} ${n(base - 40 * s)}Q${n(x - 22 * s)} ${n(base - 80 * s)} ${n(x - 12 * s)} ${n(base - 96 * s)}H${n(x + 12 * s)}Q${n(x + 22 * s)} ${n(base - 80 * s)} ${n(x + 16 * s)} ${n(base - 40 * s)}Z`, color)
      + K.circle(x, base - 100 * s, 4 * s, "#3a2e24", thin);
  }
  function tailorShop(ctx) {
    const m = ctx.mat, F = 440, beam = 70;
    ctx.keyX = 760; ctx.ambient = 0.58;
    let out = plaster(ctx, 0, beam, W, F - beam, m.plaster) + ceiling(ctx, beam, 640);
    out += windowPane(ctx, 1020, 150, 170, 160, { frame: m.beam, bars: 1 });
    out += shelving(ctx, 60, 120, 380, F - 120, 4, clothBolts);
    // A rail of hanging garments.
    out += K.line(`M480 ${beam + 40}H900`, "#3a2e24", 4);
    for (let x = 500; x < 890; x += 46) out += K.line(`M${x} ${beam + 40}v10`, "#3a2e24", 1.4) + K.path(`M${x - 18} ${beam + 52}L${x + 18} ${beam + 52}L${x + 22} ${beam + 160}L${x - 22} ${beam + 160}Z`, ctx.pick(["#7b2b25", "#2e4a6e", "#3c5b3a", "#5a4a6a", "#b6a586"]), thin);
    out += lantern(ctx, 300, beam + 20, beam + 84, 1.2) + lantern(ctx, 950, beam + 20, beam + 90, 1.3);
    out += floor(ctx, F, m.floor, "plank", 640);
    out += dressForm(ctx, 540, 470, 210, "#ece8de") + dressForm(ctx, 1150, 480, 200, "#7b2b25");
    out += person(ctx, 800, 456, 206, { face: -1, pose: "counter", apron: true, head: "short", color: "#5a4a6a" });
    out += counter(ctx, 620, 350, 380, 104) + block(700, 340, 60, 10, "#d8ccb0", thin) + candle(ctx, 960, 350, 18, 6, 0.4);
    return out;
  }

  /* ---------- Guilds ---------- */
  /* The Adventurers Guild fills its window, and its objects are the menu (port task 2, the Guild
     Rework, 2026-11-06). PLACEHOLDER ART (user decision G2): simple ink rooms the port remakes fully,
     keeping the layout (G21): the guild on the left (bulletin board beside the counter and its clerk),
     the tavern on the right (a hearth, a table of adventurers and, in castle cities, the hooded man
     alone at a corner table), hunting trophies on the back wall. Each clickable object is recorded with
     spot(), which draws it and keeps a copy for the hover glow (G23). The room stands in the upper
     part of the window, above the docked panel (G22). */
  function spot(ctx, id, label, box, markup) {
    (ctx.spots ||= []).push({ id, label, box, markup });
    return markup;
  }
  function hearth(ctx, x, floorY, w, h, stone) {
    const mouth = `M${n(x - w * 0.32)} ${n(floorY)}V${n(floorY - h * 0.45)}Q${n(x)} ${n(floorY - h * 0.72)} ${n(x + w * 0.32)} ${n(floorY - h * 0.45)}V${n(floorY)}Z`;
    let out = masonry(ctx, x - w / 2, floorY - h, w, h, stone, { row: 16 }) + block(x - w / 2 - 10, floorY - h * 0.62, w + 20, 12, ctx.mat.beam);
    out += K.path(mouth, "!#120b08") + K.clipped(mouth, K.hatch(x - w * 0.32, floorY - h * 0.72, w * 0.64, h * 0.72, 60, 3, 0.6, 0.6, "#3a1a0e"));
    fire(ctx, x, floorY - 4, w * 0.42, h * 0.3, { radius: 320 });
    return out;
  }
  function guildHall(ctx) {
    const m = ctx.mat, F = 440;
    ctx.keyX = 1100; ctx.ambient = 0.6;
    let out = solid(0, 0, W, F, K.darken(m.stone, 0.35)) + masonry(ctx, 0, 70, W, F - 70, m.stone, { row: 26 });
    out += ceiling(ctx, 54) + floor(ctx, F, m.floor, "flag");
    for (const x of [60, 1220]) out += pillar(ctx, x, 70, F, 44);
    for (const x of [380, 900]) out += windowPane(ctx, x - 34, 100, 68, 120, { arch: "point", frame: m.stone, bars: 1 });
    for (const [x, color] of [[250, m.cloth], [1030, "#2e4a6e"]]) out += hangingBanner(ctx, x, 74, 54, 120, color, "swords");
    out += chandelier(ctx, 470, 54, 132, 150, 6) + chandelier(ctx, 930, 54, 118, 130, 5);
    // Hunting trophies on the back wall.
    out += spot(ctx, "trophies", "Trophies", [560, 96, 170, 150],
      trophyArms(ctx, 645, 190, 26, m.cloth, "heater", "sword") + trophy(ctx, 645, 132, 0.9) + trophy(ctx, 588, 176, 0.55) + trophy(ctx, 702, 176, 0.55));
    // The bulletin board, wide and crowded.
    out += spot(ctx, "board", "Bulletin board", [92, 150, 250, 200],
      block(100, 150, 236, 18, m.beam) + block(110, 336, 12, 76, m.beam) + block(314, 336, 12, 76, m.beam) + noticeBoard(ctx, 106, 166, 224, 172, 14) + block(150, 150, 136, 14, m.trim, thin));
    // The counter, its clerk and a ledger.
    const clerk = person(ctx, 486, 362, 112, { face: 1, pose: "counter", head: "short", beard: true, color: "#4a5a3a", apron: false });
    out += spot(ctx, "counter", "Counter", [352, 248, 290, 200],
      clerk + counter(ctx, 364, 352, 266, 92) + ledger(430, 352, 30) + coins(560, 352, 5) + candle(ctx, 600, 352, 12, 4, 0.4) + scales(ctx, 520, 352, 0.8));
    // The tavern: a hearth, a long table of adventurers, the hooded man in his corner.
    // Flames draw over everything, so the hearth stands clear of the people (scene-banners' rule).
    out += hearth(ctx, 1046, F, 120, 180, m.stone);
    let party = "";
    const seats = [[740, 1, { pose: "toast", hold: "tankard", head: "helm", armor: true }], [800, 1, { pose: "drink", hold: "tankard", head: "long", cloak: "#3d4a5c" }], [968, -1, { pose: "point", head: "hood", hoodColor: "#3c5b3a", color: "#3c5b3a" }], [1030, -1, { pose: "drink", hold: "tankard", head: "bald", beard: "long" }]];
    for (const [x, face, options] of seats) party += person(ctx, x - 70, 476, 104, { face, seated: true, ...options });
    party += table(ctx, 814, 436, 240, m.wood, 40) + block(754, 426, 8, 10, "#7a5a3a", thin) + block(830, 427, 8, 9, "#8a8e94", thin) + K.ellipse(880, 436, 12, 2.6, "#d8ccb0", thin);
    party += person(ctx, 814, 478, 116, { face: 1, pose: "stand", head: "short", sword: true, pack: true });
    out += spot(ctx, "tavern", "Adventurers", [640, 336, 360, 150], party);
    const hooded = person(ctx, 1218, 476, 104, { face: -1, seated: true, pose: "counter", head: "hood", hoodColor: "#1e1b18", color: "#1e1b18", sword: true })
      + table(ctx, 1170, 436, 72, m.wood, 40) + candle(ctx, 1160, 436, 10, 4, 0.5);
    out += spot(ctx, "hooded", "Hooded man", [1126, 356, 140, 128], hooded);
    return out;
  }
  function guildTown(ctx) {
    const m = ctx.mat, F = 430, beam = 40;
    ctx.keyX = 1080; ctx.ambient = 0.62;
    let out = timberWall(ctx, 0, beam, W, F - beam, 260) + ceiling(ctx, beam, 640) + floor(ctx, F, m.floor, "plank");
    out += windowPane(ctx, 420, 150, 80, 70, { frame: m.beam, bars: 1 }) + lantern(ctx, 600, beam + 16, beam + 66) + lantern(ctx, 900, beam + 16, beam + 58, 0.8);
    out += spot(ctx, "trophies", "Trophies", [600, 120, 110, 90], trophy(ctx, 655, 166, 0.75));
    out += spot(ctx, "board", "Bulletin board", [100, 170, 170, 150],
      block(106, 170, 158, 14, m.beam) + noticeBoard(ctx, 112, 184, 146, 118, 7) + block(118, 300, 10, 120, m.beam) + block(242, 300, 10, 120, m.beam));
    const clerk = person(ctx, 400, 366, 106, { face: 1, pose: "counter", head: "bald", beard: true, apron: true, color: "#6a5440" });
    out += spot(ctx, "counter", "Counter", [292, 258, 230, 180], clerk + counter(ctx, 302, 360, 210, 78) + ledger(360, 360, 26) + candle(ctx, 470, 360, 11, 4, 0.4));
    out += hearth(ctx, 1110, F, 150, 160, m.stone) + barrel(ctx, 1000, F, 36, 48, m.wood);
    let party = person(ctx, 820, 462, 102, { face: 1, seated: true, pose: "drink", hold: "tankard", head: "hood", cloak: "#3d4a5c" })
      + person(ctx, 940, 462, 100, { face: -1, seated: true, pose: "toast", hold: "tankard", head: "short", armor: true });
    party += table(ctx, 880, 424, 130, m.wood, 38) + candle(ctx, 880, 424, 10, 4, 0.45);
    out += spot(ctx, "tavern", "Adventurers", [776, 340, 210, 130], party);
    return out;
  }

  /* ---------- Assembly ---------- */
  const SCENES = {
    "blacksmith-castle": armsHall, "blacksmith-town": countrySmithy,
    "general-castle": grandEmporium, "general-town": countryStore,
    tower: towerSanctum, church: stormChapel, "guild-hall": guildHall, "guild-town": guildTown,
    tailor: tailorShop, institute: towerSanctum, hearth: armsHall,
  };
  const FULL = [1280, 900];
  const SIZES = { "blacksmith-castle": FULL, "blacksmith-town": FULL, "general-castle": FULL, "general-town": FULL, tower: FULL, church: FULL, "guild-hall": FULL, "guild-town": FULL, tailor: FULL, institute: FULL, hearth: FULL };
  // Placeholder scenes the port remakes fully (port task 2, user decision G2).
  const PLACEHOLDER_ART = { "guild-hall": true, "guild-town": true, tailor: true, institute: true, hearth: true };
  function variantFor(kind, place) {
    const castle = place?.kind === "Castle city";
    if (kind === "guild") return castle ? "guild-hall" : "guild-town";
    if (["tower", "church", "tailor", "institute", "hearth"].includes(kind)) return kind;
    return (kind === "blacksmith" ? "blacksmith" : "general") + (castle ? "-castle" : "-town");
  }
  function build(variant, key, regionId, label) {
    const size = [W, H];
    [W, H] = SIZES[variant] || [1200, 250];
    try {
      const ctx = createContext(key, regionId, label);
      const previous = K.beginInk();
      let scene = "", clips = "";
      try { scene = SCENES[variant](ctx); } finally { clips = K.endInk(previous); }
      return compose(ctx, scene, clips);
    } finally { [W, H] = size; }
  }
  function lightLabel() {
    try { return window.WorldRules?.lighting?.(window.worldState?.minutes ?? 720)?.label || "Daylight"; } catch { return "Daylight"; }
  }
  function scene(kind, place, regionId, label = lightLabel()) {
    const variant = variantFor(kind, place);
    const key = `${variant}|${place?.id || "none"}|${regionId || ""}|${label}`;
    if (!cache.has(key)) {
      if (cache.size > 24) cache.delete(cache.keys().next().value);
      cache.set(key, build(variant, key, regionId, label));
    }
    return { key, variant, markup: cache.get(key) };
  }
  // Draws the scene for "blacksmith", "general", "tower", "church" or "guild" into host.
  function show(host, kind, place, regionId) {
    if (!host) return;
    try {
      const result = scene(kind, place, regionId);
      if (host.dataset.scene !== result.key) {
        host.innerHTML = result.markup;
        host.dataset.scene = result.key;
        host.dataset.variant = result.variant;
      }
      host.hidden = false;
    } catch (error) {
      console.error("Scene banner failed", error);
      host.hidden = true;
    }
  }
  window.SceneBanners = { show, markup: (kind, place, regionId, label) => scene(kind, place, regionId, label).markup, variants: Object.keys(SCENES), variantFor, placeholderArt: variant => Boolean(PLACEHOLDER_ART[variant]) };
})();
