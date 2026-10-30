/* Non-humanoid battle rigs: four-legged beasts, griffons, dragons and drakes,
   serpentine wyrms, floating spirits and cores. Each rig faces right with its
   feet on the origin and exposes the same animation groups as the humanoid
   (sp-upper, sp-head, sp-leg-front/back, sp-tail, sp-wings). */
(() => {
  "use strict";
  const K = window.SpriteKit;
  const { n, path, flat, line, ellipse, circle, dot, at, capsule, taper, mix, darken, lighten, star, rgb, INK } = K;

  // Glowing eyes keep their full colour inside a dark socket.
  const glowEyes = (x, y, r, color, far = false) => ellipse(x, y, r * 1.7, r * 1.15, "!" + INK, 'stroke="none"') + `<g class="sp-eyeglow" style="color:${color}">${ellipse(x, y, r * 1.25, r * 0.72, "!" + color, 'stroke="none"')}${far ? "" : dot(x + r * 0.35, y - r * 0.18, r * 0.26, "#fff8e8")}</g>`;
  // A deep-set animal eye: an ink socket, a dull iris and a single glint.
  const inkEye = (x, y, r, color = "#b07a2a") => {
    const [red, green, blue] = rgb(color);
    const iris = (red * 0.3 + green * 0.59 + blue * 0.11) / 255 < 0.28 ? "#a8742a" : color;
    return ellipse(x, y, r * 1.4, r * 0.95, "!" + INK, 'stroke="none"') + dot(x + r * 0.22, y + r * 0.08, r * 0.6, K.tone(iris)) + dot(x + r * 0.22, y + r * 0.08, r * 0.26, INK) + dot(x + r * 0.5, y - r * 0.22, r * 0.22, "#f4ead2");
  };
  // Shadow over the rear part of a box, its edge bowing back through the middle as on a rounded form.
  const rearShadow = (x, y, w, h, fraction, opacity) => `<path d="M${n(x - 8)} ${n(y - 8)} L${n(x + w * fraction)} ${n(y - 8)} Q${n(x + w * (fraction - 0.12))} ${n(y + h / 2)} ${n(x + w * fraction)} ${n(y + h + 8)} L${n(x - 8)} ${n(y + h + 8)}Z" fill="${INK}" opacity="${n(opacity)}"/>`;
  /* Light falls from the upper front, so the underside and the rear of every
     mass sink into ink, with a hatched band along the terminator. */
  function shadeMass(d, x, y, w, h, opacity = 0.74) {
    const edge = y + h * 0.56;
    return K.clipped(d, `<path d="M${n(x - 6)} ${n(edge + h * 0.08)} Q${n(x + w * 0.45)} ${n(edge - h * 0.1)} ${n(x + w + 6)} ${n(edge + h * 0.1)} L${n(x + w + 6)} ${n(y + h + 8)} L${n(x - 6)} ${n(y + h + 8)}Z" fill="${INK}" opacity="${opacity}"/>`
      + rearShadow(x, y, w, h, 0.24, opacity * 0.7)
      + K.hatch(x, edge - h * 0.24, w, h * 0.24, 68, 2.8, 0.45, 0.32)
      + K.hatch(x + w * 0.2, y, Math.max(2, w * 0.07), h, 74, 2.8, 0.45, 0.32));
  }
  // A limb in shadow along its rear edge.
  const shadeLimb = (d, w, length, back) => K.clipped(d, K.inkShadow(-w, -2, w * 2, length + 4, 0.5, back ? 0.62 : 0.68));
  // Short ink strokes that suggest coarse fur or feathers.
  function roughStrokes(x, y, w, h, count, seedText, opacity = 0.5) {
    const random = K.seeded(seedText);
    let d = "";
    for (let i = 0; i < count; i++) {
      const sx = x + random() * w, sy = y + random() * h, length = 2.5 + random() * 3.5;
      d += `M${n(sx)} ${n(sy)} q${n(-length * 0.5)} ${n(length * 0.15)} ${n(-length)} ${n(length * 0.55)} `;
    }
    return `<path d="${d}" fill="none" stroke="${INK}" stroke-width=".7" stroke-linecap="round" opacity="${opacity}"/>`;
  }
  /* A ragged fringe of tufts along an edge. Positive depth points to the right
     of the direction of travel, so a left-to-right fringe hangs downward. */
  function fringe(x1, y1, x2, y2, count, depth, color) {
    const dx = (x2 - x1) / count, dy = (y2 - y1) / count, length = Math.hypot(dx, dy) || 1;
    const nx = -dy / length * depth, ny = dx / length * depth;
    const random = K.seeded(`${n(x1)},${n(y1)},${count}`);
    let d = `M${n(x1)} ${n(y1)}`;
    for (let i = 0; i < count; i++) {
      const reach = (i % 2 ? 0.65 : 1) * (0.7 + random() * 0.55), lean = 0.2 + random() * 0.3;
      d += ` L${n(x1 + dx * (i + lean) + nx * reach)} ${n(y1 + dy * (i + lean) + ny * reach)} L${n(x1 + dx * (i + 1))} ${n(y1 + dy * (i + 1))}`;
    }
    return path(d + "Z", color, 'stroke-width="1.2"');
  }

  function overlays(p, x, y, w, h) {
    const out = [];
    if (p.corruption) {
      out.push(line(`M${n(x + w * 0.2)} ${n(y + h * 0.3)} q${n(w * 0.1)} ${n(h * 0.25)} ${n(w * 0.25)} ${n(h * 0.2)} q${n(w * 0.12)} ${n(-h * 0.1)} ${n(w * 0.2)} ${n(h * 0.25)}`, "!#b04aff", 1.3, 'class="sp-glowline" opacity=".85"'));
      for (let i = 0; i < 4; i++) out.push(path(`M${n(x + w * (0.25 + i * 0.15))} ${n(y + h * 0.05)} l${n(w * 0.04)} ${n(-h * 0.35)} l${n(w * 0.05)} ${n(h * 0.33)}Z`, "#2a1a33", 'stroke-width=".9"'));
    }
    if (p.frost) out.push(line(`M${n(x + w * 0.3)} ${n(y + h * 0.2)} l${n(w * 0.05)} ${n(h * 0.3)} M${n(x + w * 0.55)} ${n(y + h * 0.15)} l${n(-w * 0.04)} ${n(h * 0.3)}`, "#ffffff", 1, 'opacity=".9"'));
    if (p.magma) out.push(line(`M${n(x + w * 0.2)} ${n(y + h * 0.4)} l${n(w * 0.2)} ${n(h * 0.15)} l${n(w * 0.1)} ${n(-h * 0.2)} l${n(w * 0.25)} ${n(h * 0.2)}`, "!#ff7a2e", 1.5, 'class="sp-glowline"'));
    // A beast of cooled lava (the Vulcanic Ram): magma glowing through cracks in its obsidian
    // hide, and a fleece of flame along its back.
    if (p.molten) {
      const cracks = [[0.1, 0.38, 0.24, 0.58, 0.36, 0.42, 0.46, 0.6], [0.5, 0.22, 0.6, 0.46, 0.74, 0.36, 0.86, 0.52], [0.26, 0.74, 0.42, 0.86, 0.56, 0.72, 0.7, 0.84]]
        .map(points => `M${points.map((value, i) => n(i % 2 ? y + h * value : x + w * value)).join(" ").replace(/^(\S+ \S+) /, "$1 L")}`).join(" ");
      out.push(`<g class="sp-glow" style="color:${p.molten}">${line(cracks, "!" + p.molten, 1.4)}</g>`);
      const flame = (fx, fy, size, color) => flat(`M${n(fx - size * 0.5)} ${n(fy)} Q${n(fx - size * 0.55)} ${n(fy - size * 0.9)} ${n(fx + size * 0.15)} ${n(fy - size * 1.6)} Q${n(fx + size * 0.05)} ${n(fy - size * 0.8)} ${n(fx + size * 0.5)} ${n(fy - size * 0.55)} Q${n(fx + size * 0.6)} ${n(fy - size * 0.15)} ${n(fx + size * 0.4)} ${n(fy)}Z`, "!" + color);
      const tongues = [0.14, 0.3, 0.46, 0.62, 0.78].map((k, i) => flame(x + w * k, y + h * 0.16, h * (0.42 + (i % 2) * 0.16), "#ff8a3d") + flame(x + w * k + w * 0.02, y + h * 0.16, h * (0.24 + (i % 2) * 0.1), "#ffd27a")).join("");
      out.push(`<g class="sp-glow sp-flicker" style="color:#ffb04a">${tongues}</g>`);
    }
    if (p.lightning) out.push(`<g class="sp-glow" style="color:#ffe66b">${line(`M${n(x + w * 0.3)} ${n(y)} l${n(w * 0.06)} ${n(h * 0.3)} l${n(-w * 0.04)} ${n(h * 0.06)} l${n(w * 0.08)} ${n(h * 0.35)}`, "!#ffe66b", 1.2)}</g>`);
    if (p.moss) for (let i = 0; i < 3; i++) out.push(ellipse(x + w * (0.25 + i * 0.22), y + h * 0.08, w * 0.08, h * 0.12, "#6f9a4a", 'stroke-width=".8"'));
    if (p.scars) out.push(line(`M${n(x + w * 0.35)} ${n(y + h * 0.15)} l${n(w * 0.1)} ${n(h * 0.45)} M${n(x + w * 0.5)} ${n(y + h * 0.1)} l${n(w * 0.06)} ${n(h * 0.4)}`, "#7a3a2e", 1.3, 'opacity=".85"'));
    return out.join("");
  }

  /* ---------------- Four-legged beasts ---------------- */
  K.quadruped = function quadruped(p) {
    const previousInk = K.beginInk();
    const type = p.type || "wolf", wolf = type === "wolf", stag = type === "stag", lean = wolf || stag;
    // Wolves are lean and long-legged and carry the head low; stags stand taller still, the head
    // held high on a long neck under a crown of antlers.
    const len = p.len || 58, bodyH = (p.bodyH || 24) * (lean ? 0.88 : 1), legLen = (p.legLen || 20) * (stag ? 1.38 : wolf ? 1.12 : 1), headR = p.headR || 13;
    const fur = p.fur || "#8a8580", belly = p.belly || lighten(fur, 0.3), dark = darken(fur, 0.25), deep = darken(fur, 0.4);
    const by = -legLen - bodyH * 0.45;
    // Legs pivot at the body. Each rises into a haunch or shoulder so the
    // near pair reads as muscle over the flank instead of a pasted stick.
    const leg = (x, back, front) => {
      const c = back ? dark : fur;
      const w = type === "bear" || type === "boar" ? 9.5 : stag ? 5.4 : wolf ? 6.4 : 7.2;
      const t = bodyH * 0.38;
      // Forelegs narrow at the wrist; hind legs kink back at the hock.
      const d = front
        ? `M${n(-w * 0.7)} ${n(-t)} Q0 ${n(-t - 3)} ${n(w * 0.75)} ${n(-t * 0.7)} Q${n(w * 0.65)} ${n(legLen * 0.3)} ${n(w * 0.42)} ${n(legLen * 0.64)} L${n(w * 0.5)} ${n(legLen - 3)} L${n(-w * 0.45)} ${n(legLen - 3)} L${n(-w * 0.4)} ${n(legLen * 0.66)} Q${n(-w * 0.62)} ${n(legLen * 0.3)} ${n(-w * 0.7)} ${n(-t)}Z`
        : `M${n(-w * 0.85)} ${n(-t)} Q${n(w * 0.2)} ${n(-t - 4)} ${n(w * 0.9)} ${n(-t * 0.6)} Q${n(w * 1.1)} ${n(legLen * 0.2)} ${n(w * 0.8)} ${n(legLen * 0.36)} L${n(-w * 0.35)} ${n(legLen * 0.66)} L${n(w * 0.5)} ${n(legLen - 3)} L${n(-w * 0.45)} ${n(legLen - 3)} L${n(-w * 1.35)} ${n(legLen * 0.64)} Q${n(-w * 1.2)} ${n(legLen * 0.3)} ${n(-w * 0.85)} ${n(-t)}Z`;
      const foot = p.hooves ? path(`M${n(-w * 0.5)} ${n(legLen - 3.5)} h${n(w)} l1 3.5 h${n(-w - 1.5)}Z`, back ? "#231c18" : "#2f2620") : path(`M${n(-w * 0.55)} ${n(legLen - 4)} Q${n(w * 1.2)} ${n(legLen - 5)} ${n(w * 0.95)} ${n(legLen)} L${n(-w * 0.55)} ${n(legLen)}Z`, c);
      const claws = p.hooves ? "" : line(`M${n(w * 0.4)} ${n(legLen - 0.5)} l1.6 0.9 M${n(w * 0.8)} ${n(legLen - 1.5)} l1.6 0.9`, "#d8ccb0", 1);
      const shade = K.clipped(d, K.inkShadow(-w * 1.2, -t - 4, w * 2.4, legLen + t + 4, 0.5, back ? 0.62 : 0.68));
      return `<g transform="translate(${n(x)} ${n(-legLen - 2)})"><g class="sp-leg-${back ? "back" : "front"}">${path(d, c)}${shade}${foot}${claws}</g></g>`;
    };
    const farLegs = leg(len * 0.3, true, true) + leg(-len * 0.32, true, false);
    const nearLegs = leg(len * 0.22, false, true) + leg(-len * 0.38, false, false);

    // Tail
    let tail = "";
    const tx = -len * 0.48, ty = by - bodyH * 0.15;
    if (wolf) {
      const d = "M2 -2 Q-10 -3 -17 6 L-23 17 L-18 14 L-19 20 L-13 13 L-11 16 Q-8 8 2 6Z";
      tail = path(d, dark) + K.clipped(d, `<rect x="-26" y="5" width="30" height="20" fill="${INK}" opacity=".5"/>` + roughStrokes(-18, -1, 16, 12, 5, "tail" + fur, 0.5));
    } else if (type === "bear") tail = circle(-2, 0, 4, dark);
    else if (type === "boar") tail = line("M0 0 q-5 -4 -7 2 q-1 4 -5 2", deep, 1.6);
    else if (type === "ram") tail = path("M0 0 Q-6 2 -7 8 Q-3 6 1 4Z", belly);
    else if (stag) tail = path("M1 -2 Q-6 -4 -7 3 Q-3 4 1 2Z", belly);
    tail = `<g transform="translate(${n(tx)} ${n(ty)})"><g class="sp-tail">${tail}</g></g>`;

    // Body
    const bodyPath = lean
      ? `M${n(-len * 0.5)} ${n(by)} C${n(-len * 0.52)} ${n(by - bodyH * 0.8)} ${n(len * 0.05)} ${n(by - bodyH * 0.8)} ${n(len * 0.36)} ${n(by - bodyH * 0.82)} C${n(len * 0.6)} ${n(by - bodyH * 0.55)} ${n(len * 0.56)} ${n(by + bodyH * 0.62)} ${n(len * 0.3)} ${n(by + bodyH * 0.62)} Q${n(len * 0.02)} ${n(by + bodyH * 0.58)} ${n(-len * 0.22)} ${n(by + bodyH * 0.25)} C${n(-len * 0.42)} ${n(by + bodyH * 0.5)} ${n(-len * 0.56)} ${n(by + bodyH * 0.25)} ${n(-len * 0.5)} ${n(by)}Z`
      : `M${n(-len * 0.5)} ${n(by)} C${n(-len * 0.52)} ${n(by - bodyH * 0.8)} ${n(len * 0.1)} ${n(by - bodyH * (type === "bear" ? 1.05 : 0.85))} ${n(len * 0.38)} ${n(by - bodyH * 0.7)} C${n(len * 0.58)} ${n(by - bodyH * 0.5)} ${n(len * 0.55)} ${n(by + bodyH * 0.5)} ${n(len * 0.3)} ${n(by + bodyH * 0.55)} L${n(-len * 0.35)} ${n(by + bodyH * 0.55)} C${n(-len * 0.5)} ${n(by + bodyH * 0.5)} ${n(-len * 0.55)} ${n(by + bodyH * 0.2)} ${n(-len * 0.5)} ${n(by)}Z`;
    let body = "";
    if (wolf) {
      // Raised hackles along the back and a ragged fringe under the belly.
      body += fringe(-len * 0.14, by - bodyH * 0.72, len * 0.42, by - bodyH * 0.72, 8, -(p.mane ? 8 : 5), p.maneColor || dark);
      body += fringe(len * 0.32, by + bodyH * 0.55, -len * 0.14, by + bodyH * 0.38, 6, -3.8, dark);
    }
    body += path(bodyPath, fur);
    body += lean
      ? flat(`M${n(len * 0.34)} ${n(by - bodyH * 0.25)} Q${n(len * 0.58)} ${n(by)} ${n(len * 0.42)} ${n(by + bodyH * 0.6)} L${n(len * 0.3)} ${n(by + bodyH * 0.6)}Z`, belly, 'opacity=".75"')
      : flat(`M${n(-len * 0.3)} ${n(by + bodyH * 0.5)} Q0 ${n(by + bodyH * 0.15)} ${n(len * 0.32)} ${n(by + bodyH * 0.5)}Z`, belly, 'opacity=".85"');
    body += line(`M${n(-len * 0.3)} ${n(by - bodyH * 0.62)} q${n(len * 0.25)} ${n(-bodyH * 0.12)} ${n(len * 0.5)} ${n(-bodyH * 0.05)}`, lighten(fur, 0.2), 1.1, 'opacity=".6"');
    body += K.clipped(bodyPath, roughStrokes(-len * 0.45, by - bodyH * 0.7, len * 0.85, bodyH * 0.9, wolf ? 18 : 12, type + fur, 0.42));
    body += shadeMass(bodyPath, -len * 0.56, by - bodyH * 0.85, len * 1.14, bodyH * 1.5);
    if (!wolf && (type === "boar" || p.mane)) {
      const tufts = [];
      for (let i = 0; i < 6; i++) tufts.push(`M${n(-len * 0.2 + i * len * 0.1)} ${n(by - bodyH * 0.72 - (i > 2 ? 2 : 0))} l${n(len * 0.03)} ${n(-bodyH * 0.35)} l${n(len * 0.04)} ${n(bodyH * 0.32)}`);
      body += path(tufts.join(" ") + "Z", p.maneColor || deep, 'stroke-width="1"');
    }
    body += overlays(p, -len * 0.45, by - bodyH * 0.75, len * 0.9, bodyH * 1.2);

    // Head
    const hx = len * (lean ? 0.58 : 0.55) + (stag ? 4 : 0), hy = by - bodyH * (type === "boar" ? 0.2 : stag ? 1.55 : wolf ? 0.42 : 0.62);
    if (stag) {
      // A long neck rising from the shoulders to the raised head.
      const neck = `M${n(len * 0.22)} ${n(by - bodyH * 0.55)} C${n(len * 0.3)} ${n(by - bodyH * 1.2)} ${n(hx - 9)} ${n(hy - 4)} ${n(hx - 3)} ${n(hy - 6)} L${n(hx + 4)} ${n(hy + 6)} C${n(len * 0.52)} ${n(by - bodyH * 0.5)} ${n(len * 0.5)} ${n(by - bodyH * 0.2)} ${n(len * 0.42)} ${n(by)}Z`;
      body += path(neck, fur) + flat(`M${n(hx + 2)} ${n(hy + 4)} C${n(len * 0.5)} ${n(by - bodyH * 0.6)} ${n(len * 0.48)} ${n(by - bodyH * 0.25)} ${n(len * 0.42)} ${n(by)} L${n(len * 0.46)} ${n(by)} C${n(len * 0.54)} ${n(by - bodyH * 0.3)} ${n(len * 0.55)} ${n(by - bodyH * 0.7)} ${n(hx + 4)} ${n(hy + 6)}Z`, belly, 'opacity=".75"')
        + K.clipped(neck, rearShadow(len * 0.18, hy - 6, 10, by - hy + 8, 0.7, 0.5));
    }
    const R = headR;
    const P = (x, y) => `${n(hx + R * x)} ${n(hy + R * y)}`;
    const eye = (x, y, r) => p.eyeGlow ? glowEyes(x, y, r, p.eyes || "#ffb04a") : inkEye(x, y, r, p.eyes || "#c9902a");
    // The back of the skull, the jaw line and the brow above the eye fall into shadow.
    const shadeHead = (d, ex, ey) => K.clipped(d, rearShadow(hx - R * 1.2, hy - R * 1.6, R * 1.2, R * 3.2, 0.82, 0.62)
      + `<path d="M${P(-2, 0.42)} Q${P(0.6, 0.26)} ${P(2.4, 0.5)} L${P(2.4, 2)} L${P(-2, 2)}Z" fill="${INK}" opacity=".58"/>`
      + ellipse(ex - R * 0.05, ey + R * 0.02, R * 0.44, R * 0.27, "!" + INK, 'stroke="none" opacity=".42"')
      + K.hatch(hx - R * 0.38, hy - R * 1.2, R * 0.18, R * 2.2, 72, 2.6, 0.4, 0.35));
    const brow = (x1, y1, x2, y2) => line(`M${P(x1, y1)} L${P(x2, y2)}`, INK, 1.5);
    const tooth = (x, y, length, up) => path(`M${P(x - 0.06, y)} L${P(x, y + (up ? -length : length))} L${P(x + 0.07, y)}Z`, "#e6dcc4", 'stroke-width=".6"');
    let head = "";
    if (wolf) {
      const skull = `M${P(-1.0, 0.35)} C${P(-1.1, -0.75)} ${P(-0.1, -0.95)} ${P(0.5, -0.55)} Q${P(0.75, -0.36)} ${P(1.0, -0.32)} L${P(1.85, -0.1)} Q${P(2.08, 0)} ${P(2.0, 0.24)} L${P(1.8, 0.42)} L${P(0.6, 0.5)} C${P(0.1, 0.85)} ${P(-0.6, 0.9)} ${P(-1.0, 0.35)}Z`;
      const jaw = `M${P(0.25, 0.5)} L${P(1.55, 0.56)} Q${P(1.55, 0.74)} ${P(1.3, 0.8)} Q${P(0.8, 0.9)} ${P(0.35, 0.88)} Q${P(0.05, 0.8)} ${P(0.25, 0.5)}Z`;
      head = path(`M${P(-0.55, -0.5)} L${P(-0.78, -1.45)} L${P(-0.05, -0.72)}Z`, deep)
        + path(`M${P(-0.25, -0.6)} L${P(-0.36, -1.6)} L${P(0.4, -0.6)}Z`, fur) + flat(`M${P(-0.16, -0.74)} L${P(-0.27, -1.32)} L${P(0.2, -0.7)}Z`, "!" + INK, 'opacity=".6"')
        + path(`M${P(0.5, 0.44)} L${P(1.8, 0.38)} Q${P(1.6, 0.6)} ${P(1.45, 0.66)} L${P(0.5, 0.72)}Z`, "!#2a0f0c", 'stroke-width="1"')
        + `<g class="sp-jaw">${path(jaw, dark)}${K.clipped(jaw, `<rect x="${n(hx - R)}" y="${n(hy + R * 0.74)}" width="${n(R * 3)}" height="${n(R)}" fill="${INK}" opacity=".6"/>`)}${tooth(1.32, 0.62, 0.26, true)}${tooth(1.0, 0.66, 0.16, true)}</g>`
        + fringe(hx - R * 0.82, hy - R * 0.45, hx - R * 0.55, hy + R * 0.85, 5, R * 0.62, fur)
        + path(skull, fur)
        + flat(`M${P(0.3, 0.3)} L${P(1.75, 0.4)} L${P(0.45, 0.6)}Z`, belly, 'opacity=".7"')
        + K.clipped(skull, roughStrokes(hx - R * 0.95, hy - R * 0.6, R * 1.1, R * 1.2, 6, "head" + fur, 0.4))
        + shadeHead(skull, hx + R * 0.42, hy - R * 0.18)
        + line(`M${P(1.05, -0.26)} q${n(R * 0.1)} ${n(R * 0.12)} ${n(R * 0.02)} ${n(R * 0.26)} M${P(1.27, -0.2)} q${n(R * 0.1)} ${n(R * 0.12)} ${n(R * 0.02)} ${n(R * 0.24)}`, INK, 0.8, 'opacity=".7"')
        + ellipse(hx + R * 1.96, hy + R * 0.08, R * 0.14, R * 0.11, "!" + INK, 'stroke="none"')
        + line(`M${P(0.55, 0.48)} L${P(1.82, 0.4)}`, INK, 1)
        + tooth(1.55, 0.4, 0.3, false) + tooth(1.76, 0.38, 0.13, false) + tooth(0.85, 0.46, 0.17, false)
        + eye(hx + R * 0.42, hy - R * 0.18, R * 0.15) + brow(0.12, -0.42, 0.78, -0.24);
    } else if (type === "bear") {
      const skull = `M${P(-1.05, 0)} C${P(-1.05, -1.0)} ${P(0.4, -1.05)} ${P(0.75, -0.25)} Q${P(1.45, -0.1)} ${P(1.45, 0.35)} Q${P(1.2, 0.75)} ${P(0.4, 0.85)} C${P(-0.4, 0.95)} ${P(-1.05, 0.6)} ${P(-1.05, 0)}Z`;
      head = circle(hx - R * 0.72, hy - R * 0.78, R * 0.25, deep) + circle(hx - R * 0.1, hy - R * 0.9, R * 0.25, fur) + dot(hx - R * 0.1, hy - R * 0.87, R * 0.12, INK, 'opacity=".6"');
      head += path(skull, fur) + flat(`M${P(0.6, -0.05)} Q${P(1.4, -0.05)} ${P(1.4, 0.35)} Q${P(1.1, 0.65)} ${P(0.55, 0.6)}Z`, belly, 'opacity=".8"');
      head += K.clipped(skull, roughStrokes(hx - R, hy - R * 0.8, R * 1.3, R * 1.4, 7, "head" + fur, 0.4)) + shadeHead(skull, hx + R * 0.25, hy - R * 0.3);
      head += ellipse(hx + R * 1.32, hy + R * 0.08, R * 0.17, R * 0.13, "!" + INK, 'stroke="none"') + eye(hx + R * 0.25, hy - R * 0.3, R * 0.13) + brow(0.0, -0.52, 0.55, -0.4);
      head += path(`M${P(0.75, 0.42)} Q${P(1.05, 0.58)} ${P(1.32, 0.42)} L${P(1.2, 0.66)} Q${P(0.95, 0.72)} ${P(0.75, 0.42)}Z`, "!#2a0f0c", 'stroke-width=".9"') + tooth(1.18, 0.44, 0.16, false) + tooth(0.9, 0.5, 0.12, false);
    } else if (type === "boar") {
      const skull = `M${P(-1.1, -0.5)} C${P(-0.6, -1.25)} ${P(0.6, -0.85)} ${P(1.25, -0.15)} L${P(1.4, 0.55)} Q${P(0.6, 1.0)} ${P(-0.6, 0.75)} Q${P(-1.2, 0.2)} ${P(-1.1, -0.5)}Z`;
      head = fringe(hx - R * 1.05, hy - R * 0.55, hx + R * 0.35, hy - R * 0.92, 6, -R * 0.4, p.maneColor || deep);
      head += path(skull, fur) + K.clipped(skull, roughStrokes(hx - R, hy - R * 0.8, R * 1.6, R * 1.4, 8, "head" + fur, 0.4)) + shadeHead(skull, hx + R * 0.4, hy - R * 0.25);
      head += ellipse(hx + R * 1.35, hy + R * 0.2, R * 0.22, R * 0.38, "#b08070") + dot(hx + R * 1.4, hy + R * 0.08, R * 0.06, INK) + dot(hx + R * 1.4, hy + R * 0.32, R * 0.06, INK);
      head += path(`M${P(0.85, 0.55)} Q${P(1.35, 0.4)} ${P(1.25, -0.25)} Q${P(1.05, 0.3)} ${P(0.7, 0.4)}Z`, "#e6dcc4");
      head += path(`M${P(-0.55, -0.85)} L${P(-0.85, -1.4)} L${P(-0.15, -0.95)}Z`, dark) + eye(hx + R * 0.4, hy - R * 0.25, R * 0.13) + brow(0.1, -0.5, 0.65, -0.36);
    } else if (stag) {
      // A deer's long face, tall ears and branching antlers that shine like noon (p.antlerGlow).
      const skull = `M${P(-0.85, 0.05)} C${P(-0.9, -0.85)} ${P(0.2, -0.95)} ${P(0.62, -0.42)} L${P(1.75, 0.18)} Q${P(1.95, 0.42)} ${P(1.7, 0.62)} L${P(0.75, 0.82)} C${P(-0.1, 0.95)} ${P(-0.8, 0.7)} ${P(-0.85, 0.05)}Z`;
      head = path(`M${P(-0.45, -0.55)} L${P(-1.15, -1.25)} L${P(-0.2, -0.8)}Z`, dark) + path(skull, fur) + flat(`M${P(0.7, 0.35)} L${P(1.72, 0.3)} Q${P(1.6, 0.6)} ${P(0.9, 0.72)}Z`, belly, 'opacity=".7"');
      head += shadeHead(skull, hx + R * 0.3, hy - R * 0.2) + ellipse(hx + R * 1.82, hy + R * 0.32, R * 0.12, R * 0.1, "!" + INK, 'stroke="none"') + eye(hx + R * 0.3, hy - R * 0.2, R * 0.13) + brow(-0.05, -0.42, 0.55, -0.32);
      const antlerColor = p.antlerColor || "#d9c49a", A = (dx, dy) => P(-0.2 + dx, -0.75 - dy);
      const antlers = `M${A(0, 0)} C${A(-0.2, 0.8)} ${A(-0.6, 1.4)} ${A(-0.9, 2.2)} M${A(-0.15, 0.6)} Q${A(0.35, 0.9)} ${A(0.55, 1.45)} M${A(-0.45, 1.3)} Q${A(-0.1, 1.75)} ${A(0.05, 2.35)} M${A(-0.72, 1.8)} Q${A(-1.25, 2.0)} ${A(-1.5, 2.55)} M${A(0.25, 0.1)} C${A(0.6, 0.7)} ${A(0.65, 1.3)} ${A(0.95, 2.0)} M${A(0.55, 0.8)} Q${A(1.05, 0.95)} ${A(1.35, 1.35)} M${A(0.78, 1.5)} Q${A(0.6, 1.95)} ${A(0.7, 2.4)}`;
      const glowAttr = p.antlerGlow ? ` class="sp-glow" style="color:${p.antlerGlow}"` : "";
      head = `<g${glowAttr}>${line(antlers, INK, 3.6)}${line(antlers, antlerColor, 2.2)}${line(antlers, lighten(antlerColor, 0.4), 0.8, 'opacity=".7"')}</g>` + head;
    } else if (type === "ram") {
      const skull = `M${P(-0.9, 0.2)} C${P(-0.9, -0.9)} ${P(0.4, -1.0)} ${P(0.85, -0.3)} L${P(1.35, 0.45)} Q${P(1.25, 0.9)} ${P(0.6, 0.95)} C${P(-0.2, 1.0)} ${P(-0.85, 0.75)} ${P(-0.9, 0.2)}Z`;
      head = path(skull, fur) + shadeHead(skull, hx + R * 0.3, hy - R * 0.15);
      head += eye(hx + R * 0.3, hy - R * 0.15, R * 0.14) + brow(0.0, -0.38, 0.6, -0.26) + dot(hx + R * 1.22, hy + R * 0.45, R * 0.07, INK);
      const hornColor = p.hornColor || "#d9cdb4";
      const horn = `M${P(-0.3, -0.7)} C${P(-1.6, -1.6)} ${P(-2.0, 0.4)} ${P(-0.75, 0.75)} C${P(-0.4, 0.6)} ${P(-0.6, 0.2)} ${P(-0.85, 0.3)} C${P(-1.4, 0)} ${P(-1.0, -0.8)} ${P(0.1, -0.6)}Z`;
      head += `<g${p.magma ? ' class="sp-glow" style="color:#ff7a2e"' : ""}>${path(horn, hornColor)}${K.clipped(horn, `<rect x="${n(hx - R * 2.2)}" y="${n(hy)}" width="${n(R * 2.4)}" height="${n(R)}" fill="${INK}" opacity=".5"/>`)}${line(`M${P(-1.0, -1.0)} l${n(R * 0.15)} ${n(R * 0.25)} M${P(-1.4, -0.4)} l${n(R * 0.25)} ${n(R * 0.1)} M${P(-1.3, 0.25)} l${n(R * 0.22)} ${n(-R * 0.08)}`, INK, 0.9, 'opacity=".7"')}</g>`;
    }
    if (p.collar) head += line(`M${P(-0.9, 0.4)} q${n(R * 0.4)} ${n(R * 0.5)} ${n(R * 0.9)} ${n(R * 0.55)}`, p.collar, 2.2);
    if (p.lightning) head += `<g class="sp-glow" style="color:#ffe66b">${line(`M${P(-0.2, -1.1)} l${n(-R * 0.2)} ${n(-R * 0.5)} l${n(R * 0.25)} ${n(-R * 0.1)} l${n(-R * 0.2)} ${n(-R * 0.5)}`, "!#ffe66b", 1.2)}</g>`;
    head = `<g transform="translate(${n(hx - R * 0.6)} ${n(hy + R * 0.4)})"><g class="sp-head"><g transform="translate(${n(-hx + R * 0.6)} ${n(-hy - R * 0.4)})">${head}</g></g></g>`;

    const defs = K.endInk(previousInk);
    const markup = `<g class="sp-figure sp-beast sp-inked">${defs}${farLegs}${tail}<g class="sp-upper">${body}${head}</g>${nearLegs}</g>`;
    const left = -len * 0.5 - 26, right = hx + R * 2.2, top = Math.min(hy - R * (stag ? 3.4 : 1.9), by - bodyH * 0.8 - 10) - (type === "ram" ? 6 : 0);
    return { markup, box: { x: left, y: top, w: right - left, h: -top + 4 }, anchors: { head: [hx, hy], chest: [len * 0.2, by], top: [hx - 4, top + 6], feet: [0, 0], hand: [hx + R, hy + R * 0.3] }, portrait: { x: hx - R * 1.45, y: hy - R * 1.85, w: R * 3.95, h: R * 3.95 } };
  };

  /* ---------------- Griffons ---------------- */
  K.griffon = function griffon(p) {
    const previousInk = K.beginInk();
    const len = p.len || 54, bodyH = p.bodyH || 22, legLen = p.legLen || 19, headR = p.headR || 12;
    const fur = p.fur || "#c49a5c", feather = p.feather || "#f3ead8", dark = darken(fur, 0.25), fdark = darken(feather, 0.25);
    const beak = p.beak || "#e0b54a";
    const by = -legLen - bodyH * 0.45;
    const leg = (x, back, front) => {
      const c = front ? (back ? darken(beak, 0.2) : beak) : back ? dark : fur;
      const w = front ? 5 : 7.5;
      const d = `M${n(-w / 2)} 0 L${n(w / 2)} 0 L${n(w * 0.4)} ${n(legLen - 2)} L${n(-w * 0.4)} ${n(legLen - 2)}Z`;
      const foot = front ? line(`M0 ${n(legLen - 1)} l${n(w * 1.1)} 1.4 M0 ${n(legLen - 1)} l${n(w * 0.5)} 2 M0 ${n(legLen - 1)} l${n(-w * 0.7)} 1.2`, INK, 1.7) : path(`M${n(-w * 0.55)} ${n(legLen - 4)} Q${n(w * 1.2)} ${n(legLen - 5)} ${n(w * 0.95)} ${n(legLen)} L${n(-w * 0.55)} ${n(legLen)}Z`, c);
      const thighD = `M${n(-w)} -1 L${n(w)} -1 L${n(w * 0.6)} ${n(legLen * 0.38)} L${n(w * 0.25)} ${n(legLen * 0.3)} L${n(-w * 0.1)} ${n(legLen * 0.42)} L${n(-w * 0.6)} ${n(legLen * 0.32)}Z`;
      const thigh = front ? path(thighD, back ? fdark : feather, 'stroke-width="1"') + K.clipped(thighD, K.inkShadow(-w, -2, w * 2, legLen * 0.5, 0.5, 0.6)) : "";
      return `<g transform="translate(${n(x)} ${n(-legLen - 2)})"><g class="sp-leg-${back ? "back" : "front"}">${path(d, c)}${K.clipped(d, K.inkShadow(-w, -2, w * 2, legLen + 2, 0.5, back ? 0.62 : 0.66))}${thigh}${foot}</g></g>`;
    };
    const farLegs = leg(len * 0.28, true, true) + leg(-len * 0.32, true, false);
    const nearLegs = leg(len * 0.2, false, true) + leg(-len * 0.38, false, false);
    const tail = `<g transform="translate(${n(-len * 0.48)} ${n(by - bodyH * 0.1)})"><g class="sp-tail">${line("M0 0 Q-12 4 -16 -8", dark, 2.4)}${path("M-16 -8 q-6 -2 -5 -8 q3 3 7 2 q0 4 -2 6Z", darken(fur, 0.35), 'stroke-width="1"')}</g></g>`;
    const bodyD = `M${n(-len * 0.5)} ${n(by)} C${n(-len * 0.52)} ${n(by - bodyH * 0.8)} ${n(len * 0.1)} ${n(by - bodyH * 0.9)} ${n(len * 0.38)} ${n(by - bodyH * 0.75)} C${n(len * 0.6)} ${n(by - bodyH * 0.5)} ${n(len * 0.55)} ${n(by + bodyH * 0.5)} ${n(len * 0.3)} ${n(by + bodyH * 0.55)} L${n(-len * 0.35)} ${n(by + bodyH * 0.55)} C${n(-len * 0.52)} ${n(by + bodyH * 0.5)} ${n(-len * 0.55)} ${n(by + bodyH * 0.2)} ${n(-len * 0.5)} ${n(by)}Z`;
    const chestD = `M${n(len * 0.02)} ${n(by - bodyH * 0.85)} C${n(len * 0.4)} ${n(by - bodyH * 0.9)} ${n(len * 0.62)} ${n(by - bodyH * 0.3)} ${n(len * 0.42)} ${n(by + bodyH * 0.55)} L${n(len * 0.05)} ${n(by + bodyH * 0.5)} Q${n(len * 0.12)} ${n(by)} ${n(len * 0.02)} ${n(by - bodyH * 0.85)}Z`;
    const shadeBox = [-len * 0.56, by - bodyH * 0.95, len * 1.2, bodyH * 1.55];
    const body = path(bodyD, fur) + K.clipped(bodyD, roughStrokes(-len * 0.45, by - bodyH * 0.6, len * 0.5, bodyH, 9, "griffon" + fur, 0.4)) + shadeMass(bodyD, ...shadeBox)
      + path(chestD, feather) + line(`M${n(len * 0.12)} ${n(by - bodyH * 0.3)} l3 3 l3 -3 M${n(len * 0.2)} ${n(by + bodyH * 0.05)} l3 3 l3 -3 M${n(len * 0.3)} ${n(by - bodyH * 0.55)} l3 3 l3 -3`, INK, 0.8, 'opacity=".55"') + shadeMass(chestD, ...shadeBox)
      + overlays(p, -len * 0.45, by - bodyH * 0.75, len * 0.9, bodyH * 1.2);
    // Wings rise from the shoulders; each feather carries an ink shaft and a shadowed root.
    const wingColor = p.wing || mix(fur, feather, 0.4);
    const feathers = [];
    for (let i = 0; i < 6; i++) {
      const d = `M0 0 Q${n(-10 - i * 4)} ${n(-30 + i * 4)} ${n(-20 - i * 5)} ${n(-34 + i * 7)} Q${n(-10 - i * 3)} ${n(-14 + i * 4)} 0 4Z`;
      feathers.push(path(d, i % 2 ? wingColor : darken(wingColor, 0.18), 'stroke-width="1"') + line(`M0 2 Q${n(-9 - i * 3.6)} ${n(-22 + i * 4)} ${n(-18 - i * 4.6)} ${n(-32 + i * 6.6)}`, INK, 0.6, 'opacity=".5"'));
    }
    const coverts = `M0 4 Q-12 -14 -14 -24 Q-4 -18 6 -6Z`;
    const wings = `<g transform="translate(${n(len * 0.1)} ${n(by - bodyH * 0.6)})"><g class="sp-wings">${feathers.reverse().join("")}${path(coverts, darken(wingColor, 0.1), 'stroke-width="1"')}${K.clipped(coverts, `<rect x="-16" y="-14" width="24" height="20" fill="${INK}" opacity=".5"/>`)}${line("M0 0 Q-14 -26 -30 -30", lighten(wingColor, 0.25), 1, 'opacity=".7"')}</g></g>`;
    const hx = len * 0.55, hy = by - bodyH * 0.95, R = headR;
    const P = (x, y) => `${n(hx + R * x)} ${n(hy + R * y)}`;
    const skull = `M${P(-0.95, 0.6)} C${P(-1.1, -0.6)} ${P(-0.1, -1.15)} ${P(0.6, -0.6)} L${P(0.85, 0.3)} L${P(0.3, 0.95)} Q${P(-0.5, 1.05)} ${P(-0.95, 0.6)}Z`;
    const beakD = `M${P(0.5, -0.45)} Q${P(1.6, -0.45)} ${P(1.8, 0.45)} Q${P(1.55, 0.18)} ${P(1.35, 0.3)} L${P(0.7, 0.42)}Z`;
    let head = fringe(hx - R * 0.55, hy - R * 0.75, hx - R * 0.95, hy + R * 0.7, 5, R * 0.55, p.crest || fdark);
    head += path(skull, feather) + K.clipped(skull, rearShadow(hx - R * 1.2, hy - R * 1.4, R * 1.2, R * 2.8, 0.7, 0.55) + `<path d="M${P(-2, 0.45)} Q${P(0, 0.3)} ${P(1, 0.5)} L${P(1, 2)} L${P(-2, 2)}Z" fill="${INK}" opacity=".5"/>` + K.hatch(hx - R * 0.48, hy - R * 1.1, R * 0.16, R * 2.2, 72, 2.6, 0.4, 0.35));
    head += path(beakD, beak) + K.clipped(beakD, `<path d="M${P(1.2, -0.6)} Q${P(1.75, -0.2)} ${P(1.9, 0.6)} L${P(2.2, 0.6)} L${P(2.2, -0.6)}Z" fill="${INK}" opacity=".55"/><rect x="${n(hx)}" y="${n(hy + R * 0.12)}" width="${n(R * 2)}" height="${n(R)}" fill="${INK}" opacity=".45"/>`);
    head += line(`M${P(0.75, 0.08)} L${P(1.4, 0.12)}`, INK, 0.9) + dot(hx + R * 0.82, hy - R * 0.22, R * 0.05, INK);
    head += path(`M${P(-0.6, -0.6)} L${P(-1.0, -1.25)} L${P(-0.25, -0.85)}Z`, p.crest || fdark, 'stroke-width="1"');
    head += p.eyeGlow ? glowEyes(hx + R * 0.22, hy - R * 0.25, R * 0.15, p.eyes || "#ff4a4a") : inkEye(hx + R * 0.2, hy - R * 0.25, R * 0.15, p.eyes || "#b8741a");
    head += line(`M${P(-0.08, -0.55)} Q${P(0.25, -0.5)} ${P(0.62, -0.38)}`, INK, 1.6);
    if (p.crown) head += path(`M${P(-0.3, -0.85)} l1 -4 l2 2.5 l2 -4 l2 4 l2 -2.5 l0 4.5Z`, p.crown, 'stroke-width=".8"');
    head = `<g transform="translate(${n(hx - R * 0.4)} ${n(hy + R * 0.6)})"><g class="sp-head"><g transform="translate(${n(-hx + R * 0.4)} ${n(-hy - R * 0.6)})">${head}</g></g></g>`;
    const defs = K.endInk(previousInk);
    const markup = `<g class="sp-figure sp-beast sp-winged sp-inked">${defs}${farLegs}${tail}${wings}<g class="sp-upper">${body}${head}</g>${nearLegs}</g>`;
    const top = Math.min(hy - R * 1.4, by - bodyH * 0.6 - 40);
    const left = -len * 0.5 - 30, right = hx + R * 2.0;
    return { markup, box: { x: left, y: top, w: right - left, h: -top + 4 }, anchors: { head: [hx, hy], chest: [len * 0.2, by], top: [hx - 4, top + 8], feet: [0, 0], hand: [hx + R * 1.4, hy] }, portrait: { x: hx - R * 1.4, y: hy - R * 1.6, w: R * 3.4, h: R * 3.4 } };
  };

  // Rows of small arcs read as scales under the ink.
  function scaleMarks(x, y, w, h, size, opacity = 0.35) {
    let d = "";
    for (let row = 0, py = y; py < y + h; row++, py += size * 0.8) {
      for (let px = x + (row % 2 ? size / 2 : 0); px < x + w; px += size) d += `M${n(px)} ${n(py)} q${n(size / 2)} ${n(size * 0.55)} ${n(size)} 0 `;
    }
    return `<path d="${d}" fill="none" stroke="${INK}" stroke-width=".6" opacity="${opacity}"/>`;
  }

  /* ---------------- Nygheir: an old, battle-worn dragon ----------------
     Stone slabs ride his spine, the weapons of a lost army are still lodged in him and
     three old wounds cross his body. Each wound shows its state in the fight: a pale
     scar, torn open and bleeding, or (once they burst) burning like molten rock. */
  const slabRows = len => [0, 1, 2, 3, 4, 5].map(i => ({ i, t: i / 5, x: -len * 0.4 + i * len * 0.135 }));
  // Glowing seams in the stone once the dragon burns; dark cracks before that.
  const crackLine = (d, burning) => burning ? `<g class="sp-glow" style="color:${burning}">${line(d, "!" + burning, 1.1)}</g>` : line(d, INK, 0.8, 'opacity=".7"');
  function backSlabs(p, len, bodyH, by) {
    const random = K.seeded("slabs" + n(len));
    let out = "";
    for (const { i, t, x } of slabRows(len)) {
      // Small over the hips, largest over the shoulders.
      const size = 0.55 + 0.45 * Math.sin(Math.min(1, t * 1.15) * Math.PI * 0.85 + 0.25);
      const y = by - bodyH * (0.7 + Math.sin(t * Math.PI) * 0.1), w = len * (0.15 + random() * 0.025) * (0.75 + size * 0.35), h = bodyH * (0.2 + size * 0.24 + random() * 0.04);
      const j = () => (random() - 0.5) * w * 0.12;
      // A shard swept back toward the tail, its peak overhanging the rear edge.
      const d = `M${n(x - w * 0.5)} ${n(y + h * 0.35)} L${n(x - w * 0.72 + j())} ${n(y - h * 0.38)} L${n(x - w * 0.62 + j())} ${n(y - h)} L${n(x - w * 0.12 + j())} ${n(y - h * 0.74)} L${n(x + w * 0.28 + j())} ${n(y - h * 0.4)} L${n(x + w * 0.5)} ${n(y - h * 0.12)} L${n(x + w * 0.58)} ${n(y + h * 0.35)}Z`;
      const fill = mix(p.plates, i % 2 ? darken(p.scale || p.plates, 0.1) : p.plates, 0.3);
      out += path(d, fill) + K.clipped(d, `<path d="M${n(x - w)} ${n(y - h * 0.15)} L${n(x + w * 0.3)} ${n(y - h * 0.6)} L${n(x + w)} ${n(y - h * 0.4)} L${n(x + w)} ${n(y + h)} L${n(x - w)} ${n(y + h)}Z" fill="${INK}" opacity=".5"/>` + K.hatch(x - w * 0.6, y - h, w * 0.25, h * 1.3, 70, 2.6, 0.45, 0.35))
        + crackLine(`M${n(x - w * 0.5)} ${n(y - h * 0.86)} l${n(w * 0.14)} ${n(h * 0.3)} l${n(-w * 0.08)} ${n(h * 0.22)} l${n(w * 0.14)} ${n(h * 0.3)}`, p.burning);
      if (i % 2 === 0) out += path(`M${n(x - w * 0.12)} ${n(y - h * 0.74)} l${n(w * 0.05)} ${n(h * 0.2)} l${n(w * 0.12)} ${n(-h * 0.02)}Z`, "!" + K.tone(darken(p.plates, 0.5)), 'stroke="none"');
    }
    return out;
  }
  // Heavy scutes overlap along the top of the back (clipped to the body).
  function backScutes(p, len, bodyH, by, scale) {
    let out = "";
    for (const { t, x } of slabRows(len).slice(0, 5)) {
      const y = by - bodyH * (0.62 + Math.sin((t + 0.1) * Math.PI) * 0.1), w = len * 0.16, h = bodyH * 0.3;
      const d = `M${n(x - w * 0.5)} ${n(y)} Q${n(x)} ${n(y - h * 0.55)} ${n(x + w * 0.62)} ${n(y - h * 0.05)} Q${n(x + w * 0.5)} ${n(y + h * 0.5)} ${n(x - w * 0.1)} ${n(y + h * 0.55)} Q${n(x - w * 0.55)} ${n(y + h * 0.4)} ${n(x - w * 0.5)} ${n(y)}Z`;
      out += path(d, mix(p.plates, scale, 0.45), 'stroke-width="1.2"') + K.clipped(d, `<rect x="${n(x - w)}" y="${n(y + h * 0.1)}" width="${n(w * 2)}" height="${n(h)}" fill="${INK}" opacity=".42"/>`)
        + crackLine(`M${n(x - w * 0.15)} ${n(y - h * 0.2)} l${n(w * 0.12)} ${n(h * 0.28)} l${n(w * 0.1)} ${n(-h * 0.05)}`, p.burning);
    }
    return out;
  }
  function oldWounds(p, len, bodyH, by, scale) {
    // Shoulder, flank and haunch; they tear open in that order.
    const spots = [[len * 0.24, by - bodyH * 0.18, bodyH * 0.62, 24], [-len * 0.06, by - bodyH * 0.02, bodyH * 0.78, 32], [-len * 0.34, by - bodyH * 0.24, bodyH * 0.56, 18]].slice(0, p.wounds);
    const open = Math.max(0, Math.min(spots.length, Math.floor(Number(p.openWounds) || 0)));
    const tissue = mix(scale, "#e8cfb4", 0.5);
    return spots.map(([x, y, L, angle], index) => {
      const W = L * (index < open && !p.burning ? 0.15 : 0.11), seam = `M0 ${n(-L / 2)} Q${n(W)} ${n(-L * 0.1)} ${n(W * 0.2)} ${n(L / 2)} Q${n(-W * 0.9)} ${n(L * 0.05)} 0 ${n(-L / 2)}Z`;
      const jag = `M0 ${n(-L * 0.42)} l${n(W * 0.4)} ${n(L * 0.2)} l${n(-W * 0.5)} ${n(L * 0.18)} l${n(W * 0.55)} ${n(L * 0.2)} l${n(-W * 0.3)} ${n(L * 0.24)}`;
      let mark;
      if (p.burning) {
        mark = `<g class="sp-glow" style="color:${p.burning}">${path(seam, "!" + p.burning, 'stroke-width=".9"')}${line(jag, "!#ffe6a8", 1.1)}</g>`
          + `<g class="sp-flicker">${dot(W * 1.6, -L * 0.3, 0.9, "!#ffd27a")}${dot(-W * 1.3, L * 0.05, 0.7, "!#ffb04a")}</g>`;
      } else if (index < open) {
        // Torn open: raw and bleeding.
        mark = path(seam, "!#4a0d0a", 'stroke-width=".9"') + line(jag, "!#b3261c", 1.2)
          + path(`M${n(W * 0.3)} ${n(L * 0.45)} q1.1 2.4 0 4 q-1.1 -1.6 0 -4Z M${n(-W * 0.6)} ${n(L * 0.2)} q.9 2 0 3.3 q-.9 -1.3 0 -3.3Z`, "!#7a1410", 'stroke-width=".5"');
      } else {
        // An old scar: a raised seam of pale tissue.
        const lumps = `M${n(-W * 0.3)} ${n(-L * 0.3)} q${n(-W * 1.1)} ${n(L * 0.06)} ${n(-W * 0.2)} ${n(L * 0.14)} M${n(W * 0.5)} ${n(-L * 0.02)} q${n(W * 1.1)} ${n(L * 0.06)} ${n(W * 0.15)} ${n(L * 0.15)} M${n(-W * 0.2)} ${n(L * 0.26)} q${n(-W)} ${n(L * 0.05)} ${n(-W * 0.1)} ${n(L * 0.13)}`;
        mark = path(seam, tissue, 'stroke-width=".9"') + path(lumps, tissue, 'stroke-width=".7"') + line(jag, INK, 0.7, 'opacity=".7"');
      }
      return at(x, y, mark, angle);
    }).join("");
  }
  // The broken spears, arrows and a sword of the army that fought him, one spear still flying a knight's banner.
  function lodgedWeapons(len, bodyH, by) {
    const wood = "#6b4a2f", iron = "#5c5852";
    const spear = (x, y, angle, length, banner) => at(x, y, path(`M-1.4 0 L-1.4 ${n(-length + 3)} L-2 ${n(-length)} L-0.4 ${n(-length + 2)} L0.6 ${n(-length - 1.5)} L1.4 ${n(-length + 2.5)} L1.4 0Z`, wood, 'stroke-width=".9"')
      + path("M-2.2 -0.5 h4.4 v3 h-4.4Z", iron, 'stroke-width=".7"')
      + (banner ? path(`M1.4 ${n(-length * 0.8)} Q10 ${n(-length * 0.84)} 19 ${n(-length * 0.76)} L14 ${n(-length * 0.62)} L20 ${n(-length * 0.46)} Q10 ${n(-length * 0.5)} 1.4 ${n(-length * 0.44)}Z`, "#34466e", 'stroke-width=".9"')
        + K.clipped(`M1.4 ${n(-length * 0.8)} Q10 ${n(-length * 0.84)} 19 ${n(-length * 0.76)} L14 ${n(-length * 0.62)} L20 ${n(-length * 0.46)} Q10 ${n(-length * 0.5)} 1.4 ${n(-length * 0.44)}Z`, `<rect x="0" y="${n(-length * 0.6)}" width="22" height="${n(length * 0.2)}" fill="${INK}" opacity=".4"/>`)
        + line(`M2.6 ${n(-length * 0.77)} Q10 ${n(-length * 0.8)} 17 ${n(-length * 0.74)} M2.6 ${n(-length * 0.47)} Q10 ${n(-length * 0.52)} 17.5 ${n(-length * 0.49)}`, "#c8a54a", 0.8)
        + path(`M8 ${n(-length * 0.66)} l2.2 -2.4 l2.2 2.4 l-2.2 2.4Z`, "#c8a54a", 'stroke-width=".5"') : ""), angle);
    const arrow = (x, y, angle) => at(x, y, line("M0 0 L0 -13", "#7a5a3a", 1.2) + path("M0 -13 l-2.4 -3.2 l0 3.6 Z M0 -13 l2.4 -3.2 l0 3.6 Z", "#d8d0c0", 'stroke-width=".5"') + dot(0, 0, 1.1, "!#2a1410"), angle);
    const sword = (x, y, angle) => at(x, y, path("M-1.6 0 L-1.6 -7 L1.6 -7 L1.6 0Z", "#9aa0a6", 'stroke-width=".7"') + path("M-6 -7 h12 v2.2 h-12Z", iron, 'stroke-width=".8"') + path("M-1.3 -9.2 h2.6 v-8 h-2.6Z", "#3a2a1e", 'stroke-width=".7"')
      + line("M-1.3 -11 l2.6 -1 M-1.3 -13.5 l2.6 -1 M-1.3 -16 l2.6 -1", "#6b4a2f", 0.6) + circle(0, -18.6, 1.9, "#8a7a5a", 'stroke-width=".7"'), angle);
    return {
      body: spear(-len * 0.02, by - bodyH * 0.42, -32, bodyH * 0.95, false) + spear(len * 0.14, by - bodyH * 0.66, -14, bodyH * 0.85, true) + sword(-len * 0.24, by - bodyH * 0.78, 14),
      // Arrows stand in the neck, so they are drawn over it.
      neck: (x, y, s) => arrow(x, y, -28) + arrow(x + s * 0.5, y + s * 0.9, -12) + arrow(x - s * 0.6, y + s * 1.5, -40),
    };
  }
  // A wing worn to rags: the trailing edge torn into tatters and holes through the membrane.
  function tatteredWing(span, membrane, shade, bones) {
    const random = K.seeded("tattered" + n(span));
    const S = (x, y) => `${n(x * span)} ${n(y * span)}`;
    // Fingertips and the scallops between them, as in the whole wing.
    const tips = [[-26, -52], [-46, -30], [-46, -6], [-32, 6], [-14, 10], [0, 6]], controls = [[-30, -34], [-36, -18], [-30, -6], [-18, -2], [-6, 2]];
    let d = `M0 0 L${S(-6, -38)} L${S(-26, -52)}`;
    for (let i = 0; i < controls.length; i++) {
      const [ax, ay] = tips[i], [cx, cy] = controls[i], [bx, by] = tips[i + 1];
      for (let k = 1; k <= 7; k++) {
        const t = k / 8, x = (1 - t) * (1 - t) * ax + 2 * (1 - t) * t * cx + t * t * bx, y = (1 - t) * (1 - t) * ay + 2 * (1 - t) * t * cy + t * t * by;
        // Ragged bites into the membrane toward the wing's root, some of them deep.
        const roll = random(), bite = roll < 0.4 ? 0.6 + random() * 1.6 : roll < 0.7 ? random() * 0.35 : 0;
        d += ` L${S(x + (-6 - x) * bite * 0.12, y + (-30 - y) * bite * 0.12)}`;
      }
      d += ` L${S(bx, by)}`;
    }
    d += "Z";
    const holes = `M${S(-32, -30)} l${n(4 * span)} ${n(-2 * span)} l${n(2.5 * span)} ${n(4 * span)} l${n(-3 * span)} ${n(3.5 * span)} l${n(-4 * span)} ${n(-1.5 * span)}Z M${S(-22, -6)} l${n(3 * span)} ${n(-1.5 * span)} l${n(1.5 * span)} ${n(3 * span)} l${n(-3.5 * span)} ${n(1.2 * span)}Z M${S(-38, -16)} l${n(2.4 * span)} ${n(0.5 * span)} l${n(-0.5 * span)} ${n(2.5 * span)}Z`;
    const id = "tear" + K.hash(d + holes).toString(36);
    const mask = `<defs><mask id="${id}" maskUnits="userSpaceOnUse" x="${n(-70 * span)}" y="${n(-70 * span)}" width="${n(90 * span)}" height="${n(100 * span)}"><rect x="${n(-70 * span)}" y="${n(-70 * span)}" width="${n(90 * span)}" height="${n(100 * span)}" fill="#fff"/><path d="${holes}" fill="#000"/></mask></defs>`;
    return mask + `<g mask="url(#${id})">${path(d, membrane)}${K.clipped(d, shade.replace(/^<g clip-path="url\(#[^)]+\)">/, "").replace(/<\/g>$/, ""))}</g>${path(holes, "none", 'stroke-width="1"')}${bones}`;
  }
  // A crown of horns: two great swept horns (one snapped short in some old fight), brow spurs and a spiked jaw.
  function hornCrown(P, R, horn, burning) {
    let out = "";
    const great = `M${P(-0.25, -0.62)} C${P(-0.9, -1.5)} ${P(-1.9, -1.75)} ${P(-2.6, -1.25)} C${P(-2.0, -1.3)} ${P(-1.2, -1.0)} ${P(-0.55, -0.35)}Z`;
    out += path(great, horn) + K.clipped(great, `<path d="M${P(-2.8, -1.1)} Q${P(-1.4, -1.1)} ${P(-0.3, -0.3)} L${P(-0.3, 0.2)} L${P(-2.8, 0.2)}Z" fill="${INK}" opacity=".42"/>`)
      + line(`M${P(-0.9, -1.15)} l${n(R * 0.12)} ${n(R * 0.28)} M${P(-1.35, -1.4)} l${n(R * 0.1)} ${n(R * 0.26)} M${P(-1.85, -1.48)} l${n(R * 0.08)} ${n(R * 0.24)}`, INK, 0.8, 'opacity=".55"');
    const snapped = `M${P(-0.6, -0.45)} C${P(-1.05, -1.05)} ${P(-1.45, -1.15)} ${P(-1.7, -1.05)} L${P(-1.55, -0.92)} L${P(-1.72, -0.82)} L${P(-1.5, -0.74)} C${P(-1.2, -0.7)} ${P(-0.95, -0.45)} ${P(-0.8, -0.2)}Z`;
    out += path(snapped, darken(horn, 0.15)) + line(`M${P(-1.15, -0.98)} l${n(R * 0.08)} ${n(R * 0.22)}`, INK, 0.7, 'opacity=".5"');
    // Spurs along the brow, the back of the skull and the jaw.
    const spurs = [[-0.05, -0.62, -0.25, -1.05, 0.12], [-0.75, -0.05, -1.4, -0.15, 0.1], [-0.85, 0.3, -1.45, 0.45, 0.09], [-0.45, 0.78, -0.75, 1.15, 0.08], [0.15, 0.85, 0.0, 1.2, 0.07]];
    for (const [x, y, tx, ty, w] of spurs) out += path(`M${P(x - w, y)} L${P(tx, ty)} L${P(x + w, y + w * 0.5)}Z`, horn, 'stroke-width="1"');
    if (burning) out += `<g class="sp-glow" style="color:${burning}">${line(`M${P(-1.7, -1.05)} L${P(-1.55, -0.92)} L${P(-1.72, -0.82)}`, "!" + burning, 1)}</g>`;
    return out;
  }

  /* ---------------- Dragons, drakes and wyrms ---------------- */
  K.dragon = function dragon(p) {
    const previousInk = K.beginInk();
    const wyrm = p.form === "wyrm";
    const len = p.len || 78, bodyH = p.bodyH || 34, legLen = p.legLen || (wyrm ? 10 : 24), headR = p.headR || 15;
    const scale = p.scale || "#b23a2a", belly = p.belly || "#e8c48a", dark = darken(scale, 0.28), deep = darken(scale, 0.45), light = lighten(scale, 0.25);
    const horn = p.horn || "#efe6d0", wing = p.wing || darken(scale, 0.12), membrane = p.membrane || mix(scale, belly, 0.35);
    const by = -legLen - bodyH * 0.45;
    const leg = (x, back, front) => {
      const c = back ? dark : scale;
      const w = (front ? 11 : 13.5) * (p.legW || 1);
      const t = bodyH * 0.3;
      const d = front
        ? `M${n(-w * 0.65)} ${n(-t)} Q0 ${n(-t - 4)} ${n(w * 0.7)} ${n(-t * 0.7)} Q${n(w * 0.65)} ${n(legLen * 0.5)} ${n(w * 0.45)} ${n(legLen - 3)} L${n(-w * 0.45)} ${n(legLen - 3)} Q${n(-w * 0.6)} ${n(legLen * 0.4)} ${n(-w * 0.65)} ${n(-t)}Z`
        : `M${n(-w * 0.8)} ${n(-t)} Q${n(w * 0.2)} ${n(-t - 5)} ${n(w * 0.85)} ${n(-t * 0.5)} Q${n(w * 0.8)} ${n(legLen * 0.35)} ${n(w * 0.4)} ${n(legLen * 0.6)} L${n(w * 0.45)} ${n(legLen - 3)} L${n(-w * 0.45)} ${n(legLen - 3)} Q${n(-w * 0.6)} ${n(legLen * 0.4)} ${n(-w * 0.8)} ${n(-t)}Z`;
      const foot = `M${n(-w * 0.55)} ${n(legLen - 4)} Q${n(w * 1.25)} ${n(legLen - 5)} ${n(w)} ${n(legLen)} L${n(-w * 0.55)} ${n(legLen)}Z`;
      return `<g transform="translate(${n(x)} ${n(-legLen - 2)})"><g class="sp-leg-${back ? "back" : "front"}">${path(d, c)}${K.clipped(d, K.inkShadow(-w * 1.2, -t - 5, w * 2.4, legLen + t + 5, 0.5, back ? 0.62 : 0.68) + scaleMarks(-w, -t, w * 2, legLen * 0.6, 4.5, 0.3))}${path(foot, c)}${line(`M${n(w * 0.5)} ${n(legLen - 0.5)} l2.4 0.8 M${n(w * 0.9)} ${n(legLen - 1.6)} l2.4 1 M${n(w * 0.1)} ${n(legLen)} l2 0.7`, horn, 1.5)}</g></g>`;
    };
    const farLegs = wyrm ? "" : leg(len * 0.3, true, true) + leg(-len * 0.3, true, false);
    const nearLegs = wyrm ? leg(len * 0.22, false, true) + leg(-len * 0.25, false, false) : leg(len * 0.22, false, true) + leg(-len * 0.36, false, false);

    // Tail sweeps behind.
    const tailLen = wyrm ? len * 0.9 : len * 0.75;
    const tip = p.element === "fire" || p.crownfire ? `<g class="sp-flicker">${path("M0 0 q-6 -8 -2 -14 q2 6 6 4 q2 6 -4 10Z", "!#ffb04a", 'stroke-width=".9"')}</g>` : path("M0 0 l-8 -6 l2 6 l-2 6Z", dark, 'stroke-width="1"');
    const tailD = `M4 -${n(bodyH * 0.35)} Q${n(-tailLen * 0.5)} ${n(-bodyH * 0.3)} ${n(-tailLen)} ${n(bodyH * (wyrm ? 0.4 : 0.15))} Q${n(-tailLen * 0.55)} ${n(bodyH * 0.15)} 4 ${n(bodyH * 0.35)}Z`;
    const tailShade = K.clipped(tailD, `<rect x="${n(-tailLen - 10)}" y="${n(bodyH * 0.02)}" width="${n(tailLen + 20)}" height="${n(bodyH)}" fill="${INK}" opacity=".58"/>` + scaleMarks(-tailLen, -bodyH * 0.35, tailLen, bodyH * 0.4, 5, 0.3) + K.hatch(-tailLen, -bodyH * 0.12, tailLen, bodyH * 0.14, 64, 2.8, 0.45, 0.3));
    const tail = `<g transform="translate(${n(-len * 0.45)} ${n(by - bodyH * 0.05)})"><g class="sp-tail">${path(tailD, scale)}${line(`M2 ${n(bodyH * 0.18)} Q${n(-tailLen * 0.5)} ${n(bodyH * 0.12)} ${n(-tailLen * 0.92)} ${n(bodyH * (wyrm ? 0.36 : 0.13))}`, belly, 2.2, 'opacity=".7"')}${tailShade}<g transform="translate(${n(-tailLen)} ${n(bodyH * (wyrm ? 0.4 : 0.15))})">${tip}</g></g></g>`;

    // Body with belly plates and a spine ridge.
    const bodyPath = wyrm
      ? `M${n(-len * 0.5)} ${n(by - bodyH * 0.1)} C${n(-len * 0.3)} ${n(by - bodyH * 0.9)} ${n(len * 0.1)} ${n(by + bodyH * 0.1)} ${n(len * 0.42)} ${n(by - bodyH * 0.8)} C${n(len * 0.55)} ${n(by - bodyH * 0.6)} ${n(len * 0.55)} ${n(by + bodyH * 0.45)} ${n(len * 0.3)} ${n(by + bodyH * 0.5)} C${n(len * 0.05)} ${n(by + bodyH * 0.8)} ${n(-len * 0.25)} ${n(by + bodyH * 0.25)} ${n(-len * 0.5)} ${n(by + bodyH * 0.45)}Z`
      : `M${n(-len * 0.5)} ${n(by)} C${n(-len * 0.5)} ${n(by - bodyH * 0.85)} ${n(len * 0.1)} ${n(by - bodyH * 0.95)} ${n(len * 0.4)} ${n(by - bodyH * 0.7)} C${n(len * 0.6)} ${n(by - bodyH * 0.45)} ${n(len * 0.55)} ${n(by + bodyH * 0.5)} ${n(len * 0.3)} ${n(by + bodyH * 0.55)} L${n(-len * 0.35)} ${n(by + bodyH * 0.55)} C${n(-len * 0.5)} ${n(by + bodyH * 0.5)} ${n(-len * 0.55)} ${n(by + bodyH * 0.2)} ${n(-len * 0.5)} ${n(by)}Z`;
    let body = path(bodyPath, scale);
    const plates = [];
    for (let i = 0; i < 6; i++) plates.push(path(`M${n(-len * 0.3 + i * len * 0.11)} ${n(by + bodyH * 0.52)} q${n(len * 0.055)} ${n(-bodyH * 0.28)} ${n(len * 0.11)} 0`, belly, 'stroke-width=".9"'));
    body += plates.join("");
    body += K.clipped(bodyPath, scaleMarks(-len * 0.5, by - bodyH * 0.95, len, bodyH * 0.75, 6, 0.32));
    body += shadeMass(bodyPath, -len * 0.56, by - bodyH, len * 1.14, bodyH * 1.6, 0.7);
    const spikes = [];
    for (let i = 0; i < 7; i++) {
      const x = -len * 0.42 + i * len * 0.12, y = wyrm ? by - bodyH * (0.45 + Math.sin(i * 0.9) * 0.35) : by - bodyH * (0.78 + (i > 1 && i < 6 ? 0.12 : 0));
      spikes.push(`M${n(x)} ${n(y + 2)} l${n(len * 0.03)} ${n(-bodyH * (p.ancient ? 0.42 : 0.3))} l${n(len * 0.04)} ${n(bodyH * 0.3)}`);
    }
    body = (p.plates ? backSlabs(p, len, bodyH, by) : path(spikes.join(" ") + "Z", p.spikeColor || deep, 'stroke-width="1"')) + body;
    if (p.plates) body += K.clipped(bodyPath, backScutes(p, len, bodyH, by, scale));
    body += line(`M${n(-len * 0.3)} ${n(by - bodyH * 0.5)} q${n(len * 0.25)} ${n(-bodyH * 0.25)} ${n(len * 0.5)} ${n(-bodyH * 0.1)}`, light, 1.1, 'opacity=".55"');
    body += overlays(p, -len * 0.4, by - bodyH * 0.6, len * 0.8, bodyH);
    if (p.fins) body += path(`M${n(-len * 0.2)} ${n(by - bodyH * 0.6)} l${n(len * 0.08)} ${n(-bodyH * 0.6)} l${n(len * 0.1)} ${n(bodyH * 0.55)}Z M${n(len * 0.05)} ${n(by - bodyH * 0.55)} l${n(len * 0.08)} ${n(-bodyH * 0.55)} l${n(len * 0.1)} ${n(bodyH * 0.5)}Z`, p.fins, 'stroke-width="1"');
    if (p.leaves) for (let i = 0; i < 5; i++) body += at(-len * 0.35 + i * len * 0.17, by - bodyH * 0.7, path("M0 0 Q4 -4 8 0 Q4 4 0 0Z", i % 2 ? p.leaves : lighten(p.leaves, 0.2), 'stroke-width=".8"'), -60 + i * 25);
    if (p.rocks) for (let i = 0; i < 4; i++) body += path(`M${n(-len * 0.3 + i * len * 0.18)} ${n(by - bodyH * 0.68)} l3 -5 l5 1 l2 5Z`, p.rocks, 'stroke-width=".9"');
    if (p.wounds) body += oldWounds(p, len, bodyH, by, scale);
    const relics = p.relics ? lodgedWeapons(len, bodyH, by) : null;
    if (relics) body += relics.body;
    if (p.celestial && !p.constellations) body += `<g class="sp-glow mw-fx" style="color:#d6e6ff">${[[-0.25, -0.2], [0.05, -0.35], [0.25, -0.05], [-0.05, 0.1]].map(([x, y]) => path(star(len * x, by + bodyH * y, 3.2, 1.3, 4), "!#ffffff", 'stroke-width=".45"')).join("")}</g>`;
    // A star-dragon's constellations shine through her scales, star joined to star.
    if (p.constellations) {
      const figures = [[[-0.36, -0.15], [-0.22, -0.4], [-0.08, -0.22], [0.02, -0.5]], [[0.1, 0.05], [0.22, -0.18], [0.34, -0.05], [0.28, 0.2]], [[-0.3, 0.2], [-0.14, 0.12], [-0.02, 0.28]]];
      body += `<g class="sp-glow mw-fx" style="color:${p.constellations}">${figures.map(points => line(points.map(([x, y], i) => `${i ? "L" : "M"}${n(len * x)} ${n(by + bodyH * y)}`).join(" "), "!" + p.constellations, 0.7, 'opacity=".75"') + points.map(([x, y], i) => path(star(len * x, by + bodyH * y, i % 2 ? 2.2 : 3, 0.9, 4), "!#ffffff", 'stroke-width=".35"')).join("")).join("")}</g>`;
    }
    // Mirror Scales: long gleams of white across her flank while the mirror holds.
    if (p.mirror) body += K.clipped(bodyPath, `<g class="sp-glow" style="color:#ffffff">${[-0.3, -0.1, 0.12].map(x => line(`M${n(len * x)} ${n(by - bodyH * 0.8)} l${n(len * 0.16)} ${n(bodyH * 1.3)}`, "!#ffffff", 1.4, 'opacity=".55"')).join("")}</g>`);
    // A burning star in her chest: it swells while a supernova gathers.
    if (p.heartStar) body += `<g class="sp-glow sp-flicker" style="color:${p.heartStar}">${circle(len * 0.16, by - bodyH * 0.18, bodyH * (p.nova ? 0.34 : 0.22), "!" + p.heartStar, 'stroke="none" opacity=".55"')}${path(star(len * 0.16, by - bodyH * 0.18, bodyH * (p.nova ? 0.32 : 0.2), bodyH * 0.07, 4), "!#ffffff", 'stroke="none"')}</g>`;

    // Wings (dragons and drakes, not wyrms).
    let wings = "";
    if (!wyrm) {
      const span = (p.wingSpan || 1) * 1.35;
      const d = `M0 0 L${n(-6 * span)} ${n(-38 * span)} L${n(-26 * span)} ${n(-52 * span)} Q${n(-30 * span)} ${n(-34 * span)} ${n(-46 * span)} ${n(-30 * span)} Q${n(-36 * span)} ${n(-18 * span)} ${n(-46 * span)} ${n(-6 * span)} Q${n(-30 * span)} ${n(-6 * span)} ${n(-32 * span)} ${n(6 * span)} Q${n(-18 * span)} ${n(-2 * span)} ${n(-14 * span)} ${n(10 * span)} Q${n(-6 * span)} ${n(2 * span)} 0 ${n(6 * span)}Z`;
      const bones = line(`M0 0 L${n(-6 * span)} ${n(-38 * span)} L${n(-26 * span)} ${n(-52 * span)} M${n(-6 * span)} ${n(-38 * span)} L${n(-46 * span)} ${n(-30 * span)} M${n(-6 * span)} ${n(-38 * span)} L${n(-46 * span)} ${n(-6 * span)} M${n(-6 * span)} ${n(-38 * span)} L${n(-32 * span)} ${n(6 * span)}`, wing, 2.4);
      // The membrane darkens toward its trailing edge, with veins hatched along each finger.
      const shade = K.clipped(d, `<path d="M${n(-60 * span)} ${n(-22 * span)} Q${n(-24 * span)} ${n(-30 * span)} ${n(4 * span)} ${n(-12 * span)} L${n(4 * span)} ${n(14 * span)} L${n(-60 * span)} ${n(14 * span)}Z" fill="${INK}" opacity=".5"/>` + K.hatch(-50 * span, -40 * span, 46 * span, 30 * span, 62, 3, 0.45, 0.28) + line(`M${n(-6 * span)} ${n(-38 * span)} Q${n(-22 * span)} ${n(-28 * span)} ${n(-38 * span)} ${n(-18 * span)} M${n(-6 * span)} ${n(-38 * span)} Q${n(-18 * span)} ${n(-16 * span)} ${n(-24 * span)} ${n(0)}`, INK, 0.7, 'opacity=".45"'));
      if (p.tattered) {
        const tatters = tatteredWing(span, membrane, shade, bones);
        wings = `<g transform="translate(${n(len * 0.12)} ${n(by - bodyH * 0.65)})"><g class="sp-wings">${tatters}</g></g>`;
      }
      const torn = p.tattered ? "" : p.torn ? path(`M${n(-36 * span)} ${n(-20 * span)} l${n(6 * span)} ${n(4 * span)} l${n(-3 * span)} ${n(5 * span)}Z`, "!#1a120d", 'stroke="none" opacity=".75"') : "";
      // Aurora wings: soft bands of light across the membrane.
      const aurora = p.aurora ? K.clipped(d, `<g class="sp-glow" style="color:${p.aurora[0]}">${p.aurora.map((c, i) => `<path d="M${n((-60 + i * 12) * span)} ${n(-60 * span)} Q${n((-30 + i * 10) * span)} ${n(-20 * span)} ${n((-8 + i * 9) * span)} ${n(10 * span)}" fill="none" stroke="${K.paint("!" + c)}" stroke-width="${n(5 * span)}" opacity=".45"/>`).join("")}</g>`) : "";
      if (!p.tattered) wings = `<g transform="translate(${n(len * 0.12)} ${n(by - bodyH * 0.65)})"><g class="sp-wings">${path(d, membrane, p.celestial ? 'fill-opacity=".85"' : "")}${shade}${aurora}${torn}${bones}${p.wingFire ? `<g class="sp-flicker">${path(`M${n(-26 * span)} ${n(-52 * span)} q-4 -8 1 -12 q1 6 5 5Z M${n(-46 * span)} ${n(-30 * span)} q-6 -5 -2 -11 q2 5 6 5Z`, "!#ffb04a", 'stroke-width=".8"')}</g>` : ""}</g></g>`;
    }

    // Neck and head.
    const neckX = len * 0.36, neckY = by - bodyH * 0.55;
    const hx = len * (p.headReach || 0.62), hy = by - bodyH * (p.headLift ?? (wyrm ? 1.15 : 1.55)), R = headR, nw = p.neckW || 1;
    const P = (x, y) => `${n(hx + R * x)} ${n(hy + R * y)}`;
    const neckD = `M${n(neckX - 8 * nw)} ${n(neckY + 6)} C${n(neckX)} ${n(hy + 8)} ${n(hx - R * 1.2)} ${n(hy + R * 0.2)} ${n(hx - R * 0.6)} ${n(hy - R * 0.3)} L${n(hx - R * 0.2)} ${n(hy + R * 0.8)} C${n(hx - R * 0.6)} ${n(hy + R * 1.4)} ${n(neckX + 14 * nw)} ${n(neckY - 2)} ${n(neckX + 10 * nw)} ${n(neckY + 12)}Z`;
    let neck = path(neckD, scale) + line(`M${n(neckX + 6)} ${n(neckY + 8)} C${n(neckX + 10)} ${n(hy + R * 1.6)} ${n(hx - R * 0.4)} ${n(hy + R * 1.0)} ${n(hx - R * 0.25)} ${n(hy + R * 0.75)}`, belly, 2.4, 'opacity=".75"')
      + K.clipped(neckD, K.inkShadow(neckX - 12, hy - R, hx - neckX + 12, neckY - hy + R * 2, 0.42, 0.6) + scaleMarks(neckX - 8, hy - R * 0.4, hx - neckX, neckY - hy + R, 5, 0.3));
    if (relics) neck += relics.neck((neckX + hx) / 2 + R * 0.35, (neckY + hy) / 2 + R * 0.15, R * 0.45);
    const skull = `M${P(-0.9, 0.3)} C${P(-1.0, -0.75)} ${P(0.2, -0.95)} ${P(0.7, -0.45)} L${P(1.9, -0.1)} Q${P(2.1, 0.2)} ${P(1.9, 0.35)} L${P(0.5, 0.55)} C${P(-0.2, 0.75)} ${P(-0.75, 0.7)} ${P(-0.9, 0.3)}Z`;
    const jawD = `M${P(-0.3, 0.55)} L${P(1.75, 0.4)} Q${P(1.6, 0.85)} ${P(0.6, 0.95)} Q${P(-0.2, 0.95)} ${P(-0.3, 0.55)}Z`;
    const fangs = (x1, y1, x2, y2, count, length) => {
      let d = "";
      for (let i = 0; i < count; i++) {
        const t = i / count, x = x1 + (x2 - x1) * t, y = y1 + (y2 - y1) * t, size = R * 0.07 * (i === count - 1 ? 1.3 : 1);
        d += `M${n(hx + R * x)} ${n(hy + R * y)} l${n(size)} ${n(length * R * (i === count - 1 ? 1.5 : 1))} l${n(size)} ${n(-length * R * (i === count - 1 ? 1.5 : 1))}Z `;
      }
      return path(d, "#e6dcc4", 'stroke-width=".6"');
    };
    const jaw = `<g class="sp-jaw">${path(jawD, dark)}${K.clipped(jawD, `<rect x="${n(hx - R)}" y="${n(hy + R * 0.66)}" width="${n(R * 3)}" height="${n(R)}" fill="${INK}" opacity=".55"/>`)}${fangs(1.5, 0.45, 0.6, 0.55, 4, -0.16)}</g>`;
    let head = jaw + path(skull, scale);
    head += K.clipped(skull, rearShadow(hx - R * 1.1, hy - R * 1.2, R * 1.1, R * 2.4, 0.86, 0.6) + `<path d="M${P(-2, 0.3)} Q${P(0.6, 0.12)} ${P(2.4, 0.2)} L${P(2.4, 2)} L${P(-2, 2)}Z" fill="${INK}" opacity=".55"/>` + ellipse(hx + R * 0.45, hy - R * 0.22, R * 0.45, R * 0.28, "!" + INK, 'stroke="none" opacity=".4"') + scaleMarks(hx - R * 0.6, hy - R * 0.9, R * 2.4, R * 0.7, 4.5, 0.3) + K.hatch(hx - R * 0.42, hy - R * 1.1, R * 0.18, R * 2, 72, 2.6, 0.4, 0.35));
    head += fangs(0.55, 0.5, 1.85, 0.32, 5, 0.17);
    head += ellipse(hx + R * 1.75, hy - R * 0.02, R * 0.09, R * 0.06, "!" + INK, 'stroke="none"');
    head += p.eyeGlow
      ? glowEyes(hx + R * 0.45, hy - R * 0.25, R * 0.16, p.eyes || "#ffd23a")
      : ellipse(hx + R * 0.45, hy - R * 0.25, R * 0.26, R * 0.17, "!" + INK, 'stroke="none"') + ellipse(hx + R * 0.47, hy - R * 0.25, R * 0.17, R * 0.11, p.eyes || "#ffd23a", 'stroke="none"') + ellipse(hx + R * 0.49, hy - R * 0.25, R * 0.035, R * 0.1, "!" + INK, 'stroke="none"') + dot(hx + R * 0.55, hy - R * 0.3, R * 0.035, "#f4ead2");
    head += line(`M${P(0.12, -0.5)} L${P(0.8, -0.38)}`, INK, 1.7);
    // An old blade's track runs down through the eye.
    if (p.scarredEye) head += line(`M${P(0.22, -0.82)} L${P(0.42, -0.3)} L${P(0.68, 0.18)}`, mix(scale, "#e8cfb4", 0.55), 1.6) + line(`M${P(0.24, -0.78)} L${P(0.43, -0.32)} L${P(0.66, 0.14)}`, INK, 0.6, 'opacity=".8"');
    if (p.burning) head += `<g class="sp-glow sp-flicker" style="color:${p.burning}">${path(`M${P(0.7, 0.5)} L${P(1.7, 0.36)} L${P(1.55, 0.5)} L${P(0.75, 0.62)}Z`, "!" + p.burning, 'stroke="none" opacity=".9"')}</g>`;
    if (p.hornCrown) head += hornCrown(P, R, horn, p.burning);
    // A crown of light: rays rising from her brow, a ring of starlight above them.
    if (p.lightCrown) head += `<g class="sp-glow sp-flicker" style="color:${p.lightCrown}">${[-0.9, -0.55, -0.2, 0.15].map((x, i) => line(`M${P(x, -0.75)} L${P(x - 0.25, -1.55 - (i % 2) * 0.35)}`, "!" + p.lightCrown, R * 0.08, 'stroke-linecap="round"')).join("")}${ellipse(hx - R * 0.55, hy - R * 1.75, R * 0.85, R * 0.22, "none", `stroke="${K.paint("!" + p.lightCrown)}" stroke-width="${n(R * 0.07)}"`)}</g>`;
    // Horns: age and variant decide their count and shape.
    const hornCount = p.hornCrown ? 0 : p.ancient ? 3 : p.form === "drake" ? 1 : 2;
    for (let i = 0; i < hornCount; i++) {
      const d = `M${P(-(0.3 + i * 0.28), -(0.62 - i * 0.12))} Q${P(-(1.0 + i * 0.35), -(1.4 - i * 0.2))} ${P(-(1.6 + i * 0.4), -(1.5 - i * 0.35))} Q${P(-(0.95 + i * 0.3), -(0.95 - i * 0.15))} ${P(-(0.05 + i * 0.25), -(0.5 - i * 0.12))}Z`;
      head += path(d, i % 2 ? darken(horn, 0.15) : horn) + K.clipped(d, `<rect x="${n(hx - R * 2.4)}" y="${n(hy - R * (1.05 - i * 0.2))}" width="${n(R * 2.6)}" height="${n(R)}" fill="${INK}" opacity=".45"/>` + line(`M${P(-(0.7 + i * 0.3), -(1.2 - i * 0.2))} l${n(R * 0.1)} ${n(R * 0.25)} M${P(-(1.05 + i * 0.35), -(1.35 - i * 0.25))} l${n(R * 0.08)} ${n(R * 0.22)}`, INK, 0.7, 'opacity=".55"'));
    }
    if (p.crownfire) head += `<g class="sp-glow sp-flicker" style="color:#ff8a3d">${path(`M${P(-0.6, -0.8)} l${n(R * 0.1)} ${n(-R * 0.9)} l${n(R * 0.25)} ${n(R * 0.55)} l${n(R * 0.2)} ${n(-R * 0.85)} l${n(R * 0.25)} ${n(R * 0.85)} l${n(R * 0.25)} ${n(-R * 0.6)} l${n(R * 0.1)} ${n(R * 0.95)}Z`, "!#ffb04a", 'stroke-width=".9"')}</g>`;
    if (p.frill) head += path(`M${P(-0.75, -0.2)} L${P(-1.6, -0.4)} L${P(-1.25, 0.1)} L${P(-1.7, 0.5)} L${P(-0.75, 0.45)}Z`, p.frill, 'stroke-width=".9"');
    if (p.whiskers) head += line(`M${P(1.6, 0.3)} q${n(R * 0.5)} ${n(R * 0.5)} ${n(R * 0.2)} ${n(R * 1.0)} M${P(1.4, 0.4)} q${n(R * 0.3)} ${n(R * 0.6)} ${n(-R * 0.1)} ${n(R * 1.0)}`, p.whiskers, 1.2);
    head = `<g transform="translate(${n(hx - R * 0.5)} ${n(hy + R * 0.6)})"><g class="sp-head"><g transform="translate(${n(-hx + R * 0.5)} ${n(-hy - R * 0.6)})">${head}</g></g></g>`;

    const defs = K.endInk(previousInk);
    const aura = p.aura ? ` style="--sp-glow:${p.aura}"` : "";
    // A solar corona: a ring of light about her whole body (the Golden Star).
    const corona = p.corona ? `<g class="sp-glow sp-flicker" style="color:${p.corona}">${ellipse(len * 0.05, by - bodyH * 0.3, len * 0.78, bodyH * 1.45, "none", `stroke="${K.paint("!" + p.corona)}" stroke-width="2.4" opacity=".55"`)}${Array.from({ length: 12 }, (_, i) => { const a = i / 12 * Math.PI * 2; return line(`M${n(len * 0.05 + Math.cos(a) * len * 0.8)} ${n(by - bodyH * 0.3 + Math.sin(a) * bodyH * 1.5)} l${n(Math.cos(a) * 6)} ${n(Math.sin(a) * 6)}`, "!" + p.corona, 1.4, 'opacity=".7"'); }).join("")}</g>` : "";
    const markup = `<g class="sp-figure sp-dragon sp-inked${wyrm ? " sp-wyrm" : ""}${p.celestial ? " sp-celestial" : ""}"${aura}>${defs}${corona}${farLegs}${tail}${wings}<g class="sp-upper">${body}${neck}${head}</g>${nearLegs}</g>`;
    const top = Math.min(hy - R * (p.ancient || p.hornCrown ? 2.0 : 1.7), wyrm ? 0 : by - bodyH * 0.65 - 52 * 1.35 * (p.wingSpan || 1));
    const left = -len * 0.45 - tailLen - 8, right = hx + R * 2.3;
    return { markup, box: { x: left, y: top, w: right - left, h: -top + 4 }, anchors: { head: [hx + R * 0.6, hy], chest: [len * 0.2, by], top: [hx, Math.max(top + 8, hy - R * 1.6)], feet: [0, 0], hand: [hx + R * 1.8, hy + R * 0.2], mouth: [hx + R * 1.9, hy + R * 0.3] }, portrait: { x: hx - R * 1.4, y: hy - R * 1.8, w: R * 3.8, h: R * 3.8 } };
  };

  /* ---------------- Spirits, wisps and cores ---------------- */
  K.wisp = function wisp(p) {
    const previousInk = K.beginInk();
    const r = p.radius || 15, c = p.color || "#5eaadb", light = lighten(c, 0.35), dark = darken(c, 0.25);
    const float = p.float ?? 18;
    const cy = -float - r;
    const kind = p.kind || "drop";
    let bodyShape;
    if (kind === "flame") bodyShape = `M0 ${n(cy - r * 1.9)} Q${n(r * 0.5)} ${n(cy - r * 0.9)} ${n(r * 1.05)} ${n(cy - r * 0.2)} Q${n(r * 1.2)} ${n(cy + r * 0.9)} 0 ${n(cy + r)} Q${n(-r * 1.2)} ${n(cy + r * 0.9)} ${n(-r * 1.05)} ${n(cy - r * 0.2)} Q${n(-r * 0.7)} ${n(cy - r * 0.6)} ${n(-r * 0.4)} ${n(cy - r * 1.3)} Q${n(-r * 0.2)} ${n(cy - r * 0.8)} 0 ${n(cy - r * 1.9)}Z`;
    else if (kind === "rock") bodyShape = `M${n(-r * 0.9)} ${n(cy - r * 0.6)} L${n(-r * 0.2)} ${n(cy - r * 1.05)} L${n(r * 0.8)} ${n(cy - r * 0.75)} L${n(r * 1.05)} ${n(cy + r * 0.2)} L${n(r * 0.5)} ${n(cy + r)} L${n(-r * 0.6)} ${n(cy + r * 0.95)} L${n(-r * 1.1)} ${n(cy + r * 0.2)}Z`;
    else if (kind === "ghost") bodyShape = `M${n(-r)} ${n(cy)} C${n(-r)} ${n(cy - r * 1.4)} ${n(r)} ${n(cy - r * 1.4)} ${n(r)} ${n(cy)} L${n(r * 0.9)} ${n(cy + r * 1.4)} L${n(r * 0.45)} ${n(cy + r * 1.1)} L${n(0)} ${n(cy + r * 1.55)} L${n(-r * 0.45)} ${n(cy + r * 1.1)} L${n(-r * 0.9)} ${n(cy + r * 1.4)}Z`;
    // A drop whose lower edge frays into trailing wisps.
    else bodyShape = `M0 ${n(cy - r * 1.6)} Q${n(r * 1.2)} ${n(cy - r * 0.4)} ${n(r)} ${n(cy + r * 0.3)} Q${n(r * 0.95)} ${n(cy + r * 0.8)} ${n(r * 0.7)} ${n(cy + r * 1.15)} Q${n(r * 0.55)} ${n(cy + r * 0.85)} ${n(r * 0.35)} ${n(cy + r * 0.9)} Q${n(r * 0.2)} ${n(cy + r * 1.05)} ${n(r * 0.05)} ${n(cy + r * 1.35)} Q${n(-r * 0.1)} ${n(cy + r * 0.95)} ${n(-r * 0.35)} ${n(cy + r * 0.95)} Q${n(-r * 0.5)} ${n(cy + r * 1.0)} ${n(-r * 0.6)} ${n(cy + r * 1.2)} Q${n(-r * 0.95)} ${n(cy + r * 0.8)} ${n(-r)} ${n(cy + r * 0.3)} Q${n(-r * 1.2)} ${n(cy - r * 0.4)} 0 ${n(cy - r * 1.6)}Z`;
    const trail = kind === "rock" ? "" : path(`M${n(-r * 0.75)} ${n(cy + r * 0.35)} Q${n(-r * 1.55)} ${n(cy + r * 1.0)} ${n(-r * 1.25)} ${n(cy + r * 1.75)} Q${n(-r * 1.05)} ${n(cy + r * 1.25)} ${n(-r * 0.45)} ${n(cy + r * 1.3)} Q${n(-r * 0.6)} ${n(cy + r * 1.05)} ${n(r * 0.1)} ${n(cy + r * 0.95)}Z`, dark, 'fill-opacity=".7"');
    // Spirits are lit from within: a soft shadow only, deeper for stone.
    const shade = K.clipped(bodyShape, kind === "rock"
      ? K.inkShadow(-r * 1.2, cy - r * 1.2, r * 2.4, r * 2.4, 0.42, 0.62) + `<rect x="${n(-r * 1.2)}" y="${n(cy + r * 0.45)}" width="${n(r * 2.4)}" height="${n(r)}" fill="${INK}" opacity=".45"/>`
      : `<rect x="${n(-r * 1.4)}" y="${n(cy - r * 2)}" width="${n(r * 0.95)}" height="${n(r * 3.6)}" fill="${INK}" opacity=".35"/><path d="M${n(-r * 1.4)} ${n(cy + r * 0.35)} Q0 ${n(cy + r * 0.1)} ${n(r * 1.4)} ${n(cy + r * 0.45)} L${n(r * 1.4)} ${n(cy + r * 1.8)} L${n(-r * 1.4)} ${n(cy + r * 1.8)}Z" fill="${INK}" opacity=".3"/>` + K.hatch(-r * 0.5, cy - r * 1.6, r * 0.14, r * 2.8, 72, 2.6, 0.4, 0.25));
    let body = `<g class="sp-tail">${trail}</g>` + K.withPen(1.4, () => path(bodyShape, c, kind === "ghost" ? 'fill-opacity=".9"' : "")) + shade + line(`M${n(r * 0.25)} ${n(cy - r * 0.95)} q${n(r * 0.4)} ${n(r * 0.25)} ${n(r * 0.55)} ${n(r * 0.75)}`, "!" + K.tone(light), 1.3, 'opacity=".75"');
    if (kind === "flame") body += `<g class="sp-flicker">${flat(`M0 ${n(cy - r * 0.9)} Q${n(r * 0.5)} ${n(cy - r * 0.1)} ${n(r * 0.4)} ${n(cy + r * 0.5)} Q0 ${n(cy + r * 0.8)} ${n(-r * 0.4)} ${n(cy + r * 0.5)} Q${n(-r * 0.5)} ${n(cy - r * 0.1)} 0 ${n(cy - r * 0.9)}Z`, "!" + light, 'opacity=".9"')}</g>`;
    if (kind === "rock") body += K.materialDecor("stone", -r, cy - r, r * 2, r * 2, "wisp");
    if (p.leaves) body += [0, 1, 2].map(i => at(-r * 0.6 + i * r * 0.6, cy - r * 1.0 - (i === 1 ? 4 : 0), path("M0 0 Q4 -4 0 -9 Q-4 -4 0 0Z", p.leaves, 'stroke-width=".8"'), -25 + i * 25)).join("");
    if (p.flower) body += [0, 1, 2, 3, 4].map(i => ellipse(r * 0.55 + Math.cos(i * 1.256) * 2.2, cy - r * 0.9 + Math.sin(i * 1.256) * 2.2, 1.8, 1.8, p.flower, 'stroke-width=".6"')).join("") + dot(r * 0.55, cy - r * 0.9, 1.4, K.tone("#f2d06b"));
    if (p.drips) body += path(`M${n(-r * 0.4)} ${n(cy + r * 1.2)} q1.5 3 0 5 q-1.5 -2 0 -5Z M${n(r * 0.5)} ${n(cy + r * 1.1)} q1.2 2.5 0 4 q-1.2 -1.5 0 -4Z`, p.drips, 'stroke-width=".6"');
    if (p.crown) body += path(`M${n(-r * 0.5)} ${n(cy - r * 1.05)} l1.5 -6 l3 4 l3 -6 l3 6 l3 -4 l1.5 6Z`, p.crown, 'stroke-width=".9"');
    if (p.spikes) body += path(`M${n(-r)} ${n(cy - r * 0.2)} l-6 -3 l5 -2Z M${n(r)} ${n(cy - r * 0.3)} l6 -3 l-5 -2Z M${n(-r * 0.3)} ${n(cy - r * 1.3)} l-2 -6 l4 4Z`, dark, 'stroke-width=".9"');
    if (p.lantern) body += `<g class="sp-glow sp-orb" style="color:${p.lantern}">${circle(r * 1.2, cy + r * 0.6, 3, "!" + p.lantern)}</g>` + line(`M${n(r * 0.8)} ${n(cy + r * 0.2)} L${n(r * 1.2)} ${n(cy + r * 0.45)}`, INK, 1);
    if (p.halo) body += `<g class="sp-glow" style="color:${p.halo}">${ellipse(0, cy - r * 1.35, r * 0.7, r * 0.2, "none", `stroke="${p.halo}" stroke-width="1.6"`)}</g>`;
    if (p.mask) body += path(`M${n(r * 0.05)} ${n(cy - r * 0.55)} Q${n(r * 0.95)} ${n(cy - r * 0.6)} ${n(r * 0.95)} ${n(cy + r * 0.05)} Q${n(r * 0.6)} ${n(cy + r * 0.55)} ${n(r * 0.05)} ${n(cy + r * 0.35)}Z`, p.mask, 'stroke-width="1"');
    if (p.orbit) body += `<g class="sp-orbit">${[0, 1, 2].map(i => path(`M${n(Math.cos(i * 2.1) * r * 1.5 - 2)} ${n(cy + Math.sin(i * 2.1) * r * 0.5 - 2)} l3 -1 l2 3 l-3 2Z`, p.orbit, 'stroke-width=".8"')).join("")}</g>`;
    /* Face: slanted hollow sockets and a dark, moaning mouth. Glowing eyes burn
       inside the sockets; otherwise only a pinprick of the spirit's light shows. */
    const fx = r * 0.32, fy = cy - r * 0.1;
    const eyeColor = p.eyes || "#1b2a3a";
    const socket = (x, y, size) => flat(`M${n(x - size)} ${n(y - size * 0.15)} Q${n(x - size * 0.1)} ${n(y - size * 1.1)} ${n(x + size * 0.85)} ${n(y - size * 0.35)} Q${n(x + size * 0.4)} ${n(y + size * 1.0)} ${n(x - size)} ${n(y - size * 0.15)}Z`, "!" + INK, 'opacity=".92"');
    const pupil = (x, y, size) => p.glow ? `<g class="sp-eyeglow" style="color:${p.glow}">${dot(x, y, size, p.glow)}</g>` : "";
    const face = p.eyeGlow
      ? socket(fx, fy, r * 0.24) + socket(fx + r * 0.46, fy, r * 0.17) + glowEyes(fx, fy, r * 0.1, eyeColor) + glowEyes(fx + r * 0.46, fy, r * 0.075, eyeColor, true)
      : socket(fx, fy, r * 0.24) + socket(fx + r * 0.46, fy, r * 0.17) + pupil(fx + r * 0.02, fy - r * 0.03, r * 0.045) + pupil(fx + r * 0.47, fy - r * 0.02, r * 0.035);
    const mouth = p.angry
      ? flat(`M${n(fx - r * 0.06)} ${n(fy + r * 0.3)} L${n(fx + r * 0.1)} ${n(fy + r * 0.42)} L${n(fx + r * 0.2)} ${n(fy + r * 0.34)} L${n(fx + r * 0.3)} ${n(fy + r * 0.46)} L${n(fx + r * 0.42)} ${n(fy + r * 0.32)} L${n(fx + r * 0.32)} ${n(fy + r * 0.58)} Q${n(fx + r * 0.12)} ${n(fy + r * 0.62)} ${n(fx - r * 0.06)} ${n(fy + r * 0.3)}Z`, "!" + INK) + line(`M${n(fx - r * 0.16)} ${n(fy - r * 0.32)} L${n(fx + r * 0.13)} ${n(fy - r * 0.18)} M${n(fx + r * 0.32)} ${n(fy - r * 0.18)} L${n(fx + r * 0.56)} ${n(fy - r * 0.3)}`, INK, 1.3)
      : flat(`M${n(fx + r * 0.12)} ${n(fy + r * 0.3)} Q${n(fx + r * 0.3)} ${n(fy + r * 0.28)} ${n(fx + r * 0.32)} ${n(fy + r * 0.48)} Q${n(fx + r * 0.3)} ${n(fy + r * 0.78)} ${n(fx + r * 0.18)} ${n(fy + r * 0.72)} Q${n(fx + r * 0.08)} ${n(fy + r * 0.5)} ${n(fx + r * 0.12)} ${n(fy + r * 0.3)}Z`, "!" + INK, 'opacity=".88"');
    const defs = K.endInk(previousInk);
    const markup = `<g class="sp-figure sp-spirit sp-floating"${p.glow ? ` style="--sp-glow:${p.glow}"` : ""}>${defs}<g class="sp-upper"><g class="sp-head">${body}${face}${mouth}</g></g></g>`;
    const top = cy - r * (kind === "flame" ? 2.1 : 1.8) - (p.crown || p.halo ? 8 : 0);
    return { markup, box: { x: -r * 2.5, y: top, w: r * 4.2, h: -top + 4 }, anchors: { head: [fx, fy], chest: [0, cy], top: [0, top + 6], feet: [0, 0], hand: [r, cy] }, portrait: { x: -r * 1.4, y: cy - r * 1.6, w: r * 2.8, h: r * 2.8 } };
  };

  /* A heartwood seed (Gaian's Jungle Cores): a floating seed of bark split by glowing cracks,
     its heart showing through, a sprout of leaves on top and roots trailing beneath. */
  function heartSeed(p) {
    const previousInk = K.beginInk();
    const r = p.radius || 15, bark = p.color || "#6a4a2e", heart = p.heart || "#d8ff9a", leaf = p.leaf || "#6fbf4a";
    const cy = -24 - r;
    const shellD = `M0 ${n(cy - r * 1.35)} C${n(r * 0.9)} ${n(cy - r * 1.0)} ${n(r * 1.05)} ${n(cy + r * 0.5)} 0 ${n(cy + r * 1.15)} C${n(-r * 1.05)} ${n(cy + r * 0.5)} ${n(-r * 0.9)} ${n(cy - r * 1.0)} 0 ${n(cy - r * 1.35)}Z`;
    const shell = path(shellD, bark) + K.clipped(shellD, `<rect x="${n(-r * 1.2)}" y="${n(cy - r * 1.5)}" width="${n(r * 1.2)}" height="${n(r * 3)}" fill="${INK}" opacity=".45"/>`
      + line(`M${n(-r * 0.5)} ${n(cy - r * 0.8)} Q${n(-r * 0.6)} ${n(cy)} ${n(-r * 0.35)} ${n(cy + r * 0.8)} M${n(r * 0.45)} ${n(cy - r * 0.7)} Q${n(r * 0.55)} ${n(cy + r * 0.1)} ${n(r * 0.3)} ${n(cy + r * 0.85)}`, INK, 0.7, 'opacity=".55"'));
    const cracks = `<g class="sp-glow sp-orb" style="color:${heart}">${line(`M${n(-r * 0.05)} ${n(cy - r * 1.0)} L${n(r * 0.18)} ${n(cy - r * 0.4)} L${n(-r * 0.12)} ${n(cy + r * 0.1)} L${n(r * 0.1)} ${n(cy + r * 0.7)} M${n(r * 0.18)} ${n(cy - r * 0.4)} L${n(r * 0.55)} ${n(cy - r * 0.25)} M${n(-r * 0.12)} ${n(cy + r * 0.1)} L${n(-r * 0.55)} ${n(cy + r * 0.25)}`, "!" + heart, 1.6)}${circle(0, cy - r * 0.05, r * 0.2, "!" + heart)}</g>`;
    const sprout = line(`M0 ${n(cy - r * 1.3)} Q${n(r * 0.1)} ${n(cy - r * 1.7)} ${n(-r * 0.05)} ${n(cy - r * 2.0)}`, "#3e5a24", 1.6)
      + [[-1, -1.85, -40], [1, -1.75, 35], [0, -2.05, 0]].map(([x, y, t], i) => `<g transform="translate(${n(r * 0.25 * x)} ${n(cy + r * y)}) rotate(${t})">${path(`M0 0 Q${n(r * 0.3)} ${n(-r * 0.25)} 0 ${n(-r * 0.6)} Q${n(-r * 0.3)} ${n(-r * 0.25)} 0 0Z`, i === 2 ? lighten(leaf, 0.15) : leaf, 'stroke-width=".8"')}</g>`).join("");
    const roots = line(`M${n(-r * 0.2)} ${n(cy + r * 1.05)} q${n(-r * 0.3)} ${n(r * 0.5)} ${n(-r * 0.15)} ${n(r * 1.0)} M${n(r * 0.15)} ${n(cy + r * 1.08)} q${n(r * 0.25)} ${n(r * 0.45)} ${n(r * 0.1)} ${n(r * 0.95)} M0 ${n(cy + r * 1.15)} l0 ${n(r * 0.8)}`, "#4a3420", 1.4);
    const motes = `<g class="sp-glow sp-flicker" style="color:${heart}">${[[-1.3, -0.6], [1.25, -0.2], [-1.05, 0.7], [1.0, 0.9]].map(([x, y]) => circle(r * x, cy + r * y, r * 0.08, "!" + heart, 'stroke="none"')).join("")}</g>`;
    const defs = K.endInk(previousInk);
    const markup = `<g class="sp-figure sp-spirit sp-floating sp-inked">${defs}<g class="sp-upper"><g class="sp-head">${roots}${shell}${cracks}${sprout}${motes}</g></g></g>`;
    return { markup, box: { x: -r * 2, y: cy - r * 2.3, w: r * 4, h: -(cy - r * 2.3) + 4 }, anchors: { head: [0, cy], chest: [0, cy], top: [0, cy - r * 2], feet: [0, 0], hand: [r, cy] }, portrait: { x: -r * 1.6, y: cy - r * 2, w: r * 3.2, h: r * 3.4 } };
  }
  /* A lich's phylactery: a shard of dark crystal held in a cage of bones on a grave-stone
     plinth, his soul-fire burning green inside it. "cracks" (0 to 3) split the crystal as it is
     struck, the fire showing through the fissures. */
  function phylactery(p) {
    const previousInk = K.beginInk();
    const r = p.radius || 13, crystal = p.color || "#1c2628", fire = p.heart || "#7cff9a", bone = p.bone || "#d8cfb8", stone = p.stone || "#4a4a46";
    const cy = -30 - r;
    const plinthD = `M${n(-r * 1.0)} 0 L${n(-r * 0.78)} ${n(-r * 0.75)} L${n(-r * 0.92)} ${n(-r * 0.95)} L${n(r * 0.92)} ${n(-r * 0.95)} L${n(r * 0.78)} ${n(-r * 0.75)} L${n(r * 1.0)} 0Z`;
    const plinth = path(plinthD, stone) + K.clipped(plinthD, `<rect x="${n(-r * 1.1)}" y="${n(-r)}" width="${n(r * 0.9)}" height="${n(r)}" fill="${INK}" opacity=".45"/>` + K.hatch(-r, -r * 0.9, r * 2, r * 0.9, 80, 2.6, 0.5, 0.4))
      + line(`M${n(-r * 0.55)} ${n(-r * 0.5)} l${n(r * 0.2)} ${n(r * 0.25)} M${n(r * 0.4)} ${n(-r * 0.7)} l${n(-r * 0.1)} ${n(r * 0.4)}`, INK, 0.7, 'opacity=".6"');
    const shardD = `M0 ${n(cy - r * 1.55)} L${n(r * 0.42)} ${n(cy - r * 0.7)} L${n(r * 0.36)} ${n(cy + r * 0.7)} L0 ${n(cy + r * 1.35)} L${n(-r * 0.36)} ${n(cy + r * 0.7)} L${n(-r * 0.42)} ${n(cy - r * 0.7)}Z`;
    const glow = `<g class="sp-glow sp-orb" style="color:${fire}">${ellipse(0, cy, r * 0.22, r * 0.5, "!" + fire, 'stroke="none" opacity=".85"')}</g>`;
    const shard = path(shardD, crystal, 'stroke-width="1.1"') + glow + K.clipped(shardD, `<rect x="${n(-r * 0.5)}" y="${n(cy - r * 1.6)}" width="${n(r * 0.5)}" height="${n(r * 3)}" fill="${INK}" opacity=".45"/>`)
      + line(`M0 ${n(cy - r * 1.55)} L0 ${n(cy + r * 1.35)} M${n(-r * 0.42)} ${n(cy - r * 0.7)} L0 ${n(cy - r * 0.35)} L${n(r * 0.42)} ${n(cy - r * 0.7)}`, lighten(crystal, 0.35), 0.6, 'opacity=".8"');
    const level = Math.max(0, Math.min(3, p.cracks || 0));
    const fissures = [
      `M${n(-r * 0.05)} ${n(cy - r * 1.2)} L${n(r * 0.15)} ${n(cy - r * 0.6)} L${n(-r * 0.1)} ${n(cy - r * 0.1)}`,
      `M${n(r * 0.3)} ${n(cy + r * 0.2)} L${n(r * 0.02)} ${n(cy + r * 0.45)} L${n(r * 0.12)} ${n(cy + r * 1.0)} M${n(r * 0.02)} ${n(cy + r * 0.45)} L${n(-r * 0.28)} ${n(cy + r * 0.3)}`,
      `M${n(-r * 0.36)} ${n(cy - r * 0.5)} L${n(-r * 0.12)} ${n(cy - r * 0.2)} L${n(-r * 0.3)} ${n(cy + r * 0.25)} M${n(-r * 0.12)} ${n(cy - r * 0.2)} L${n(r * 0.3)} ${n(cy - r * 0.05)}`,
    ].slice(0, level);
    const cracks = level ? `<g class="sp-glow sp-flicker" style="color:${fire}">${line(fissures.join(" "), "!" + fire, 1.2)}</g>` : "";
    // The bone cage: ribs curving up around the shard from the plinth to a ring at the top.
    const rib = side => `M${n(side * r * 0.62)} ${n(-r * 0.95)} C${n(side * r * 1.15)} ${n(cy + r * 0.6)} ${n(side * r * 0.95)} ${n(cy - r * 1.2)} ${n(side * r * 0.22)} ${n(cy - r * 1.75)}`;
    const ribInner = side => `M${n(side * r * 0.3)} ${n(-r * 0.95)} C${n(side * r * 0.72)} ${n(cy + r * 0.8)} ${n(side * r * 0.62)} ${n(cy - r * 1.0)} ${n(side * r * 0.1)} ${n(cy - r * 1.7)}`;
    const cageBack = line(rib(-1) + " " + ribInner(1), darken(bone, 0.3), 2.2);
    const cageFront = line(rib(1) + " " + ribInner(-1), bone, 2.4) + line(rib(1) + " " + ribInner(-1), lighten(bone, 0.25), 0.7, 'opacity=".7"')
      + [0.15, 0.55, 0.9].map(k => line(`M${n(-r * (0.95 - k * 0.3))} ${n(cy + r * (1.1 - k * 2.2))} Q0 ${n(cy + r * (1.25 - k * 2.2))} ${n(r * (0.95 - k * 0.3))} ${n(cy + r * (1.1 - k * 2.2))}`, bone, 1.0, 'opacity=".85"')).join("")
      + ellipse(0, cy - r * 1.75, r * 0.3, r * 0.12, "none", `stroke="${K.paint(bone)}" stroke-width="1.6"`)
      + path(`M${n(-r * 0.24)} ${n(-r * 0.95)} C${n(-r * 0.24)} ${n(-r * 1.5)} ${n(r * 0.24)} ${n(-r * 1.5)} ${n(r * 0.24)} ${n(-r * 0.95)}Z`, bone, 'stroke-width=".7"') + dot(-r * 0.09, -r * 1.18, r * 0.06, "!#0c0f0b") + dot(r * 0.09, -r * 1.18, r * 0.06, "!#0c0f0b");
    const wisps = `<g class="sp-glow sp-flicker" style="color:${fire}">${[[-1.2, -0.9], [1.15, -0.3], [-1.0, 0.6], [1.0, 0.95], [0.1, -2.2]].map(([x, y], i) => circle(r * x, cy + r * y, r * (0.06 + (i % 2) * 0.03), "!" + fire, 'stroke="none"')).join("")}</g>`;
    const defs = K.endInk(previousInk);
    const markup = `<g class="sp-figure sp-spirit sp-inked">${defs}<g class="sp-upper"><g class="sp-head">${plinth}${cageBack}${shard}${cracks}${cageFront}${wisps}</g></g></g>`;
    return { markup, box: { x: -r * 1.8, y: cy - r * 2.4, w: r * 3.6, h: -(cy - r * 2.4) + 4 }, anchors: { head: [0, cy], chest: [0, cy], top: [0, cy - r * 2.0], feet: [0, 0], hand: [r, cy] }, portrait: { x: -r * 1.5, y: cy - r * 2.0, w: r * 3, h: r * 3.4 } };
  }
  K.core = function core(p) {
    if (p.style === "seed") return heartSeed(p);
    if (p.style === "phylactery") return phylactery(p);
    const previousInk = K.beginInk();
    const r = p.radius || 14, c = p.color || "#6fbf6a", heart = p.heart || "#d8ff9a";
    const cy = -22 - r;
    const vines = [0, 1, 2, 3].map(i => line(`M${n(Math.cos(i * 1.57) * r * 1.3)} ${n(cy + Math.sin(i * 1.57) * r * 1.3)} Q${n(Math.cos(i * 1.57 + 0.8) * r * 0.4)} ${n(cy + Math.sin(i * 1.57 + 0.8) * r * 0.4)} ${n(Math.cos(i * 1.57 + 1.6) * r * 1.1)} ${n(cy + Math.sin(i * 1.57 + 1.6) * r * 1.1)}`, darken(c, 0.3), 2.2)).join("");
    const shellD = `M0 ${n(cy - r * 1.3)} L${n(r * 0.95)} ${n(cy - r * 0.4)} L${n(r * 0.8)} ${n(cy + r * 0.8)} L0 ${n(cy + r * 1.3)} L${n(-r * 0.8)} ${n(cy + r * 0.8)} L${n(-r * 0.95)} ${n(cy - r * 0.4)}Z`;
    const shell = path(shellD, c) + K.clipped(shellD, `<path d="M${n(-r)} ${n(cy - r * 1.4)} L0 ${n(cy - r * 1.4)} L0 ${n(cy + r * 1.4)} L${n(-r)} ${n(cy + r * 1.4)}Z" fill="${INK}" opacity=".55"/><path d="M${n(-r)} ${n(cy + r * 0.3)} L${n(r)} ${n(cy + r * 0.7)} L${n(r)} ${n(cy + r * 1.4)} L${n(-r)} ${n(cy + r * 1.4)}Z" fill="${INK}" opacity=".4"/>` + K.hatch(-r * 0.12, cy - r * 1.3, r * 0.16, r * 2.6, 72, 2.6, 0.4, 0.35));
    const inner = `<g class="sp-glow sp-orb" style="color:${heart}">${circle(0, cy, r * 0.42, "!" + heart)}</g>`;
    const leaves = [0, 1, 2, 3, 4].map(i => at(Math.cos(i * 1.26) * r * 1.6, cy + Math.sin(i * 1.26) * r * 0.9, path("M0 0 Q4 -4 8 0 Q4 4 0 0Z", lighten(c, 0.15), 'stroke-width=".8"'), i * 72)).join("");
    const defs = K.endInk(previousInk);
    const markup = `<g class="sp-figure sp-spirit sp-floating sp-inked">${defs}<g class="sp-upper"><g class="sp-head">${vines}${shell}${line(`M0 ${n(cy - r * 1.2)} L0 ${n(cy + r * 1.2)} M${n(-r * 0.9)} ${n(cy - r * 0.4)} L${n(r * 0.9)} ${n(cy + r * 0.6)}`, INK, 0.9, 'opacity=".6"')}${inner}<g class="sp-orbit">${leaves}</g></g></g></g>`;
    return { markup, box: { x: -r * 2.2, y: cy - r * 2, w: r * 4.4, h: -(cy - r * 2) + 4 }, anchors: { head: [0, cy], chest: [0, cy], top: [0, cy - r * 1.6], feet: [0, 0], hand: [r, cy] }, portrait: { x: -r * 1.5, y: cy - r * 1.5, w: r * 3, h: r * 3 } };
  };
})();
