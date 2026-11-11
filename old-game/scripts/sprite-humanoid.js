/* The humanoid battle rig in a gothic ink style: lean, long-limbed figures
   with small angular heads, faces half lost in shadow, heavy outlines and
   solid shadows clipped to every part. One parameterised body covers every
   playable race and the humanoid monster races: proportions, skins and
   materials, heads, hair, horns, lower bodies (legs, hooves, talons, fish
   and snake tails, floating robes, roots), wing-arms, tails and wings.
   Every limb sits in its own pivot group so CSS animations can swing,
   guard, cast and recoil. */
(() => {
  "use strict";
  const K = window.SpriteKit;
  const { n, path, flat, line, ellipse, circle, dot, at, capsule, taper, mix, darken, lighten, INK, OUTLINE } = K;
  // Catalogue proportions were written for the earlier chibi rig; they are
  // read as ratios of its defaults and applied to these taller defaults.
  const RIG = { legs: 46, torso: 33, head: 8.4, shoulders: 13.5, hips: 9.6, arm: 37, armW: 7.2, legW: 8.6 };
  const CATALOGUE = { legs: 24, torso: 27, head: 20, shoulders: 11.5, hips: 10, arm: 22, armW: 8, legW: 9 };
  const shadowOf = material => ["fire", "ice", "water", "wind", "ghost", "holy", "lightning"].includes(material) ? 0.34 : material === "shadow" || material === "magma" ? 0.55 : 0.76;

  // Elemental and constructed bodies replace living skin.
  const MATERIAL_SKINS = {
    stone: { skin: "#8f877c", shade: "#655e55", detail: "#4f4942" },
    bark: { skin: "#7a5636", shade: "#523820", detail: "#3b2715" },
    fire: { skin: "#f28a3a", shade: "#c8461d", detail: "#ffe08a", glow: "#ff8a3d" },
    magma: { skin: "#3d2b27", shade: "#251816", detail: "#ff7a2e", glow: "#ff6a2a" },
    ice: { skin: "#c3eaf6", shade: "#82bcd8", detail: "#ffffff", glow: "#c4f2ff" },
    water: { skin: "#5eaadb", shade: "#3780b3", detail: "#dff4ff", glow: "#7fd0ff" },
    lightning: { skin: "#4b507c", shade: "#2c3058", detail: "#ffe66b", glow: "#ffe66b" },
    wind: { skin: "#d2efdf", shade: "#94cbb0", detail: "#ffffff", glow: "#dcfff0" },
    bone: { skin: "#e8e0cf", shade: "#b4a990", detail: "#6f6450" },
    ghost: { skin: "#d2e2ea", shade: "#9ab5c4", detail: "#ffffff", glow: "#d8f4ff" },
    metal: { skin: "#9ba4ae", shade: "#68717b", detail: "#3c4249" },
    sand: { skin: "#d9b77b", shade: "#a98850", detail: "#7a5f34" },
    sandstone: { skin: "#d6b27a", shade: "#a8834c", detail: "#e8c75e" },
    shadow: { skin: "#3b3053", shade: "#221a35", detail: "#b08cff", glow: "#a77cff" },
    blood: { skin: "#a8263b", shade: "#6e1424", detail: "#ff7a88", glow: "#ff3a5a" },
    nature: { skin: "#72ae5c", shade: "#4b7f3b", detail: "#d0f293", glow: "#b8f59a" },
    holy: { skin: "#f2e6c4", shade: "#c9b37a", detail: "#fff6cf", glow: "#ffe9a0" },
  };

  function skinOf(p) {
    const material = p.material && MATERIAL_SKINS[p.material];
    const skin = material ? material.skin : p.skin || "#e9b896";
    return { skin, shade: material ? material.shade : p.skinShade || darken(skin, 0.18), detail: material?.detail || darken(skin, 0.4), glow: material?.glow || null, material: p.material || null };
  }

  // Surface details drawn over body parts of elemental and constructed bodies.
  function materialDecor(material, x, y, w, h, seed = 0) {
    if (!material) return "";
    const s = MATERIAL_SKINS[material];
    const r = K.seeded(material + seed);
    const pick = () => r();
    const out = [];
    if (material === "stone" || material === "sandstone") {
      for (let i = 0; i < 2; i++) { const sx = x + w * (0.2 + pick() * 0.6), sy = y + h * (0.15 + pick() * 0.6); out.push(line(`M${n(sx)} ${n(sy)} l${n(w * 0.12)} ${n(h * 0.1)} l${n(-w * 0.05)} ${n(h * 0.12)}`, s.detail, 0.9)); }
      if (material === "sandstone") out.push(line(`M${n(x + w * 0.15)} ${n(y + h * 0.3)} h${n(w * 0.7)}`, s.detail, 0.8, 'opacity=".7"'));
    } else if (material === "bark") {
      for (let i = 0; i < 3; i++) { const sx = x + w * (0.2 + i * 0.28); out.push(line(`M${n(sx)} ${n(y + h * 0.1)} q${n(w * 0.06)} ${n(h * 0.4)} 0 ${n(h * 0.8)}`, s.detail, 0.9, 'opacity=".75"')); }
    } else if (material === "magma") {
      out.push(line(`M${n(x + w * 0.2)} ${n(y + h * 0.2)} l${n(w * 0.25)} ${n(h * 0.25)} l${n(-w * 0.1)} ${n(h * 0.2)} l${n(w * 0.3)} ${n(h * 0.2)}`, s.detail, 1.4, 'class="sp-glowline"'));
    } else if (material === "fire") {
      out.push(flat(`M${n(x + w * 0.3)} ${n(y + h * 0.75)} q${n(w * 0.2)} ${n(-h * 0.5)} ${n(w * 0.4)} 0 q${n(-w * 0.2)} ${n(h * 0.18)} ${n(-w * 0.4)} 0Z`, s.detail, 'opacity=".75"'));
    } else if (material === "ice") {
      out.push(line(`M${n(x + w * 0.2)} ${n(y + h * 0.15)} l${n(w * 0.3)} ${n(h * 0.35)} l${n(w * 0.3)} ${n(-h * 0.2)}`, s.detail, 0.9, 'opacity=".9"'));
    } else if (material === "water") {
      out.push(line(`M${n(x + w * 0.15)} ${n(y + h * 0.35)} q${n(w * 0.18)} ${n(-h * 0.12)} ${n(w * 0.35)} 0 t${n(w * 0.35)} 0`, s.detail, 1, 'opacity=".8"'));
    } else if (material === "lightning") {
      out.push(line(`M${n(x + w * 0.3)} ${n(y + h * 0.1)} l${n(w * 0.2)} ${n(h * 0.35)} l${n(-w * 0.15)} ${n(h * 0.1)} l${n(w * 0.25)} ${n(h * 0.4)}`, s.detail, 1.1, 'class="sp-glowline"'));
    } else if (material === "wind") {
      out.push(line(`M${n(x + w * 0.2)} ${n(y + h * 0.5)} a${n(w * 0.22)} ${n(h * 0.18)} 0 1 1 ${n(w * 0.3)} ${n(h * 0.1)}`, s.detail, 1, 'opacity=".9"'));
    } else if (material === "metal") {
      out.push(dot(x + w * 0.25, y + h * 0.2, 0.9, s.detail), dot(x + w * 0.75, y + h * 0.2, 0.9, s.detail), line(`M${n(x + w * 0.1)} ${n(y + h * 0.55)} h${n(w * 0.8)}`, s.detail, 0.8, 'opacity=".6"'));
    } else if (material === "shadow" || material === "blood") {
      out.push(line(`M${n(x + w * 0.3)} ${n(y + h * 0.2)} q${n(w * 0.3)} ${n(h * 0.3)} 0 ${n(h * 0.6)}`, s.detail, 1, 'class="sp-glowline" opacity=".85"'));
    } else if (material === "nature") {
      out.push(flat(`M${n(x + w * 0.35)} ${n(y + h * 0.4)} q${n(w * 0.2)} ${n(-h * 0.2)} ${n(w * 0.3)} ${n(h * 0.05)} q${n(-w * 0.15)} ${n(h * 0.15)} ${n(-w * 0.3)} ${n(-h * 0.05)}Z`, s.detail, 'opacity=".8"'));
    } else if (material === "holy") {
      out.push(line(`M${n(x + w * 0.5)} ${n(y + h * 0.15)} v${n(h * 0.5)} M${n(x + w * 0.3)} ${n(y + h * 0.35)} h${n(w * 0.4)}`, s.detail, 1, 'class="sp-glowline" opacity=".8"'));
    } else if (material === "bone") {
      // Skeletal ribs are drawn by the torso itself.
    }
    return out.join("");
  }

  /* ---------------- Heads ---------------- */
  // A lean three-quarter head facing right: high cranium, angular jaw.
  function headPath(hx, hy, R, shape = "round") {
    if (shape === "lizard" || shape === "skull") return chibiHead(hx, hy, R * (shape === "skull" ? 1.08 : 1.02), shape);
    const jaw = shape === "orc" ? 1.3 : shape === "troll" ? 1.38 : 1.14;
    const front = shape === "orc" || shape === "troll" ? 1.06 : 0.98;
    const brow = shape === "troll" ? -0.18 : -0.3;
    return `M${n(hx - R * 0.12)} ${n(hy - R * 1.1)} C${n(hx + R * 0.5)} ${n(hy - R * 1.16)} ${n(hx + R * 0.95)} ${n(hy - R * 0.82)} ${n(hx + R * 0.98)} ${n(hy + R * brow)} C${n(hx + R * 1.02)} ${n(hy - R * 0.08)} ${n(hx + R * 0.94)} ${n(hy + R * 0.05)} ${n(hx + R * front)} ${n(hy + R * 0.3)} C${n(hx + R * (front + 0.04))} ${n(hy + R * 0.62)} ${n(hx + R * 0.92)} ${n(hy + R * (jaw - 0.16))} ${n(hx + R * 0.62)} ${n(hy + R * jaw)} C${n(hx + R * 0.38)} ${n(hy + R * (jaw + 0.1))} ${n(hx + R * 0.06)} ${n(hy + R * (jaw - 0.06))} ${n(hx - R * 0.24)} ${n(hy + R * (jaw - 0.24))} C${n(hx - R * 0.58)} ${n(hy + R * 0.72)} ${n(hx - R * 0.82)} ${n(hy + R * 0.55)} ${n(hx - R * 0.9)} ${n(hy + R * 0.2)} C${n(hx - R * 1.04)} ${n(hy - R * 0.25)} ${n(hx - R * 0.9)} ${n(hy - R * 1.0)} ${n(hx - R * 0.12)} ${n(hy - R * 1.1)}Z`;
  }
  // Shadow falls on the back of the skull, the jaw and the eye sockets.
  function headShade(d, hx, hy, R, opacity, shape) {
    if (shape === "skull") return K.clipped(d, `<path d="M${n(hx - R * 1.4)} ${n(hy - R * 1.4)} L${n(hx - R * 0.2)} ${n(hy - R * 1.4)} Q${n(hx - R * 0.45)} ${n(hy)} ${n(hx + R * 0.1)} ${n(hy + R * 1.5)} L${n(hx - R * 1.4)} ${n(hy + R * 1.5)}Z" fill="${INK}" opacity="${opacity * 0.8}"/>`);
    const back = `<path d="M${n(hx - R * 1.4)} ${n(hy - R * 1.5)} L${n(hx - R * 0.1)} ${n(hy - R * 1.5)} Q${n(hx - R * 0.42)} ${n(hy - R * 0.2)} ${n(hx + R * 0.02)} ${n(hy + R * 0.45)} Q${n(hx + R * 0.3)} ${n(hy + R * 0.95)} ${n(hx + R * 0.52)} ${n(hy + R * 1.6)} L${n(hx - R * 1.4)} ${n(hy + R * 1.6)}Z" fill="${INK}" opacity="${opacity}"/>`;
    const jaw = `<path d="M${n(hx - R)} ${n(hy + R * 0.92)} Q${n(hx + R * 0.4)} ${n(hy + R * 0.75)} ${n(hx + R * 1.2)} ${n(hy + R * 0.98)} L${n(hx + R * 1.2)} ${n(hy + R * 1.6)} L${n(hx - R)} ${n(hy + R * 1.6)}Z" fill="${INK}" opacity="${opacity * 0.75}"/>`;
    const brow = `<path d="M${n(hx + R * 0.1)} ${n(hy - R * 0.2)} Q${n(hx + R * 0.55)} ${n(hy - R * 0.32)} ${n(hx + R * 1.1)} ${n(hy - R * 0.2)} L${n(hx + R * 1.1)} ${n(hy + R * 0.08)} Q${n(hx + R * 0.55)} ${n(hy - R * 0.02)} ${n(hx + R * 0.1)} ${n(hy + R * 0.12)}Z" fill="${INK}" opacity="${opacity * 0.42}"/>`;
    return K.clipped(d, back + jaw + brow + K.hatch(hx - R * 0.12, hy - R * 0.5, R * 0.14, R * 1.2, 68, R * 0.26, 0.35, 0.35));
  }

  // The earlier rounded heads still serve reptile snouts and skulls.
  function chibiHead(hx, hy, R, shape = "round") {
    if (shape === "lizard") {
      // A long reptilian snout extends toward the facing direction.
      return `M${n(hx - R * 0.15)} ${n(hy - R * 0.95)} C${n(hx + R * 0.45)} ${n(hy - R * 1.02)} ${n(hx + R * 0.8)} ${n(hy - R * 0.62)} ${n(hx + R * 0.95)} ${n(hy - R * 0.28)} C${n(hx + R * 1.35)} ${n(hy - R * 0.2)} ${n(hx + R * 1.62)} ${n(hy)} ${n(hx + R * 1.6)} ${n(hy + R * 0.28)} C${n(hx + R * 1.56)} ${n(hy + R * 0.55)} ${n(hx + R * 1.1)} ${n(hy + R * 0.68)} ${n(hx + R * 0.55)} ${n(hy + R * 0.82)} C${n(hx + R * 0.1)} ${n(hy + R * 0.98)} ${n(hx - R * 0.55)} ${n(hy + R * 0.9)} ${n(hx - R * 0.82)} ${n(hy + R * 0.55)} C${n(hx - R * 1.08)} ${n(hy + R * 0.15)} ${n(hx - R * 1.0)} ${n(hy - R * 0.62)} ${n(hx - R * 0.15)} ${n(hy - R * 0.95)}Z`;
    }
    if (shape === "orc" || shape === "troll") {
      const jaw = shape === "troll" ? 1.12 : 1.05;
      return `M${n(hx - R * 0.1)} ${n(hy - R * 0.95)} C${n(hx + R * 0.6)} ${n(hy - R)} ${n(hx + R)} ${n(hy - R * 0.55)} ${n(hx + R)} ${n(hy - R * 0.05)} C${n(hx + R * 1.08)} ${n(hy + R * 0.35)} ${n(hx + R * jaw)} ${n(hy + R * 0.75)} ${n(hx + R * 0.7)} ${n(hy + R * 0.98)} C${n(hx + R * 0.3)} ${n(hy + R * 1.12)} ${n(hx - R * 0.45)} ${n(hy + R * 1.05)} ${n(hx - R * 0.78)} ${n(hy + R * 0.72)} C${n(hx - R * 1.08)} ${n(hy + R * 0.38)} ${n(hx - R * 1.06)} ${n(hy - R * 0.55)} ${n(hx - R * 0.6)} ${n(hy - R * 0.85)} C${n(hx - R * 0.42)} ${n(hy - R * 0.97)} ${n(hx - R * 0.25)} ${n(hy - R * 0.97)} ${n(hx - R * 0.1)} ${n(hy - R * 0.95)}Z`;
    }
    if (shape === "skull") {
      return `M${n(hx - R * 0.1)} ${n(hy - R * 0.98)} C${n(hx + R * 0.62)} ${n(hy - R * 1.02)} ${n(hx + R)} ${n(hy - R * 0.55)} ${n(hx + R * 0.98)} ${n(hy - R * 0.05)} C${n(hx + R * 0.98)} ${n(hy + R * 0.3)} ${n(hx + R * 0.82)} ${n(hy + R * 0.42)} ${n(hx + R * 0.78)} ${n(hy + R * 0.62)} L${n(hx + R * 0.72)} ${n(hy + R * 0.9)} L${n(hx + R * 0.1)} ${n(hy + R * 0.92)} L${n(hx - R * 0.15)} ${n(hy + R * 0.6)} C${n(hx - R * 0.8)} ${n(hy + R * 0.55)} ${n(hx - R * 1.05)} ${n(hy + R * 0.1)} ${n(hx - R * 0.95)} ${n(hy - R * 0.45)} C${n(hx - R * 0.82)} ${n(hy - R * 0.85)} ${n(hx - R * 0.45)} ${n(hy - R * 0.98)} ${n(hx - R * 0.1)} ${n(hy - R * 0.98)}Z`;
    }
    return `M${n(hx - R * 0.1)} ${n(hy - R)} C${n(hx + R * 0.6)} ${n(hy - R)} ${n(hx + R)} ${n(hy - R * 0.6)} ${n(hx + R)} ${n(hy - R * 0.1)} C${n(hx + R * 1.02)} ${n(hy + R * 0.25)} ${n(hx + R * 0.95)} ${n(hy + R * 0.6)} ${n(hx + R * 0.6)} ${n(hy + R * 0.88)} C${n(hx + R * 0.3)} ${n(hy + R * 1.02)} ${n(hx - R * 0.45)} ${n(hy + R * 0.98)} ${n(hx - R * 0.75)} ${n(hy + R * 0.7)} C${n(hx - R * 1.05)} ${n(hy + R * 0.4)} ${n(hx - R * 1.05)} ${n(hy - R * 0.55)} ${n(hx - R * 0.6)} ${n(hy - R * 0.88)} C${n(hx - R * 0.4)} ${n(hy - R)} ${n(hx - R * 0.25)} ${n(hy - R)} ${n(hx - R * 0.1)} ${n(hy - R)}Z`;
  }

  function ears(style, hx, hy, R, skin, shade) {
    if (!style || style === "none") return { back: "", front: "" };
    const ex = hx - R * 0.3, ey = hy + R * 0.1;
    if (style === "round") return { back: "", front: path(`M${n(ex + R * 0.06)} ${n(ey - R * 0.2)} C${n(ex - R * 0.24)} ${n(ey - R * 0.26)} ${n(ex - R * 0.26)} ${n(ey + R * 0.24)} ${n(ex + R * 0.06)} ${n(ey + R * 0.2)}Z`, skin) + line(`M${n(ex - R * 0.02)} ${n(ey - R * 0.1)} q${n(-R * 0.1)} ${n(R * 0.1)} 0 ${n(R * 0.2)}`, shade, 0.9) };
    if (style === "pointy" || style === "long") {
      const reach = style === "long" ? 1.12 : 0.78, lift = style === "long" ? 0.82 : 0.55;
      const d = `M${n(ex + R * 0.06)} ${n(ey - R * 0.12)} Q${n(ex - R * reach * 0.55)} ${n(ey - R * lift * 0.6)} ${n(ex - R * reach)} ${n(ey - R * lift)} Q${n(ex - R * reach * 0.45)} ${n(ey + R * 0.05)} ${n(ex + R * 0.04)} ${n(ey + R * 0.2)}Z`;
      return { back: "", front: path(d, skin) + line(`M${n(ex - R * 0.05)} ${n(ey - R * 0.02)} Q${n(ex - R * reach * 0.45)} ${n(ey - R * lift * 0.45)} ${n(ex - R * reach * 0.82)} ${n(ey - R * lift * 0.85)}`, shade, 0.9) };
    }
    if (style === "goblin") {
      const d = `M${n(ex + R * 0.05)} ${n(ey - R * 0.2)} Q${n(ex - R * 0.7)} ${n(ey - R * 0.55)} ${n(ex - R * 1.35)} ${n(ey - R * 0.38)} Q${n(ex - R * 0.85)} ${n(ey + R * 0.1)} ${n(ex + R * 0.04)} ${n(ey + R * 0.25)}Z`;
      return { back: "", front: path(d, skin) + flat(`M${n(ex - R * 0.1)} ${n(ey - R * 0.1)} Q${n(ex - R * 0.7)} ${n(ey - R * 0.3)} ${n(ex - R * 1.08)} ${n(ey - R * 0.3)} Q${n(ex - R * 0.65)} ${n(ey + R * 0.02)} ${n(ex - R * 0.05)} ${n(ey + R * 0.12)}Z`, shade, 'opacity=".55"') };
    }
    if (style === "fin") {
      const d = `M${n(ex + R * 0.05)} ${n(ey - R * 0.25)} L${n(ex - R * 0.9)} ${n(ey - R * 0.55)} L${n(ex - R * 0.7)} ${n(ey - R * 0.1)} L${n(ex - R * 0.95)} ${n(ey + R * 0.25)} L${n(ex + R * 0.05)} ${n(ey + R * 0.25)}Z`;
      return { back: "", front: path(d, lighten(skin, 0.1)) + line(`M${n(ex - R * 0.05)} ${n(ey - R * 0.1)} L${n(ex - R * 0.7)} ${n(ey - R * 0.38)} M${n(ex - R * 0.05)} ${n(ey + R * 0.1)} L${n(ex - R * 0.75)} ${n(ey + R * 0.1)}`, shade, 0.8) };
    }
    // Beast ears on top of the head, drawn over the hair (port task 11's Half-Beasts, placeholder art): pointed
    // (wolf, fox, lynx) or round (bear), in the fur's colour.
    if (style === "beast" || style === "beast-round") {
      const bx = hx - R * 0.3, by = hy - R * 0.9;
      const top = style === "beast"
        ? path(`M${n(bx - R * 0.28)} ${n(by + R * 0.28)} L${n(bx - R * 0.12)} ${n(by - R * 0.52)} L${n(bx + R * 0.3)} ${n(by + R * 0.02)}Z`, skin) + path(`M${n(bx - R * 0.16)} ${n(by + R * 0.12)} L${n(bx - R * 0.1)} ${n(by - R * 0.3)} L${n(bx + R * 0.12)} ${n(by + R * 0.02)}Z`, shade || darken(skin, 0.25), 'stroke="none"')
        : ellipse(bx, by, R * 0.27, R * 0.25, skin) + ellipse(bx + R * 0.02, by + R * 0.02, R * 0.13, R * 0.12, shade || darken(skin, 0.25), 'stroke="none"');
      return { back: "", front: "", top };
    }
    if (style === "animal") {
      const d = `M${n(ex)} ${n(ey - R * 0.55)} L${n(ex - R * 0.35)} ${n(ey - R * 1.15)} L${n(ex + R * 0.25)} ${n(ey - R * 0.75)}Z`;
      return { back: path(d, skin), front: "" };
    }
    return { back: "", front: "" };
  }

  /* An inked face: deep sockets under a heavy brow, a hard nose and a thin mouth. */
  function eyes(p, hx, hy, R, s) {
    const e = p.eyes || {};
    const color = e.color || "#6a5a4a";
    const style = e.style || "round";
    const female = p.sex === "female";
    const lizard = p.head === "lizard";
    const nearX = hx + R * (lizard ? 0.5 : 0.4), farX = hx + R * (lizard ? 0.92 : 0.86), ey = hy + R * (lizard ? -0.2 : -0.05);
    const out = [];
    const glint = (x, y, r, c) => dot(x, y, r, "!" + c);
    if (style === "glow" || style === "void") {
      if (style === "glow") out.push(ellipse(nearX, ey, R * 0.22, R * 0.13, INK, 'stroke="none"'), ellipse(farX, ey, R * 0.12, R * 0.11, INK, 'stroke="none"'));
      out.push(`<g class="sp-eyeglow" style="color:${color}">`, ellipse(nearX + R * 0.02, ey, R * 0.12, R * 0.07, "!" + color, 'stroke="none"'), ellipse(farX, ey, R * 0.07, R * 0.06, "!" + color, 'stroke="none"'), "</g>");
    } else if (style === "closed") {
      // A dreamer's closed eyes: lowered lids and a line of lashes.
      out.push(path(`M${n(nearX - R * 0.2)} ${n(ey - R * 0.02)} Q${n(nearX)} ${n(ey + R * 0.1)} ${n(nearX + R * 0.21)} ${n(ey - R * 0.03)}`, "none", `fill="none" stroke="${INK}" stroke-width="${n(R * 0.06)}" stroke-linecap="round"`));
      out.push(path(`M${n(farX - R * 0.1)} ${n(ey - R * 0.02)} Q${n(farX)} ${n(ey + R * 0.08)} ${n(farX + R * 0.12)} ${n(ey - R * 0.02)}`, "none", `fill="none" stroke="${INK}" stroke-width="${n(R * 0.05)}" stroke-linecap="round"`));
      out.push(line(`M${n(nearX - R * 0.1)} ${n(ey + R * 0.05)} l${n(-R * 0.02)} ${n(R * 0.06)} M${n(nearX + R * 0.02)} ${n(ey + R * 0.07)} l0 ${n(R * 0.06)} M${n(nearX + R * 0.13)} ${n(ey + R * 0.05)} l${n(R * 0.02)} ${n(R * 0.06)}`, INK, R * 0.03));
    } else if (style === "skull") {
      out.push(ellipse(nearX, ey + R * 0.02, R * 0.24, R * 0.22, INK, 'stroke="none"'), ellipse(farX + R * 0.02, ey + R * 0.02, R * 0.12, R * 0.19, INK, 'stroke="none"'));
      if (e.color) out.push(`<g class="sp-eyeglow" style="color:${color}">`, glint(nearX, ey + R * 0.04, R * 0.07, color), glint(farX + R * 0.02, ey + R * 0.04, R * 0.05, color), "</g>");
    } else if (style === "slit") {
      out.push(ellipse(nearX, ey, R * 0.17, R * 0.13, INK, 'stroke="none"'), ellipse(nearX + R * 0.02, ey, R * 0.12, R * 0.09, "!" + color, 'stroke="none"'), ellipse(nearX + R * 0.03, ey, R * 0.025, R * 0.08, INK, 'stroke="none"'));
    } else if (style === "beady") {
      out.push(ellipse(nearX, ey, R * 0.17, R * 0.12, INK, 'stroke="none"'), ellipse(farX, ey, R * 0.1, R * 0.09, INK, 'stroke="none"'));
      out.push(glint(nearX + R * 0.03, ey, R * 0.06, e.color ? lighten(e.color, 0.2) : "#d8c8a0"), glint(farX + R * 0.01, ey, R * 0.04, e.color ? lighten(e.color, 0.2) : "#d8c8a0"));
    } else {
      // Ordinary eyes sit deep in shadow with a single glint of color.
      const narrow = style === "narrow";
      const ry = narrow ? R * 0.08 : R * 0.11, rx = R * (narrow ? 0.24 : 0.21);
      out.push(`<ellipse cx="${n(nearX)}" cy="${n(ey)}" rx="${n(rx)}" ry="${n(ry)}" fill="${INK}" transform="rotate(${narrow ? -10 : -4} ${n(nearX)} ${n(ey)})"/>`);
      out.push(`<ellipse cx="${n(farX)}" cy="${n(ey)}" rx="${n(rx * 0.5)}" ry="${n(ry * 0.92)}" fill="${INK}" transform="rotate(${narrow ? -10 : -4} ${n(farX)} ${n(ey)})"/>`);
      out.push(glint(nearX + R * 0.06, ey, R * 0.055, lighten(color, 0.35)), glint(farX + R * 0.01, ey, R * 0.035, lighten(color, 0.3)));
      if (female || p.lashes) out.push(line(`M${n(nearX + rx * 0.8)} ${n(ey - ry * 0.6)} l${n(R * 0.14)} ${n(-R * 0.1)}`, INK, 0.7));
    }
    // A heavy brow; fierce brows bear down toward the nose.
    if (style !== "void" && style !== "skull" && p.brows !== false) {
      const browColor = p.browColor || darken(p.hair?.color || "#3a2a1f", 0.35);
      const dip = e.fierce ? R * 0.12 : 0;
      const weight = female ? R * 0.08 : R * 0.13;
      out.push(line(`M${n(nearX - R * 0.26)} ${n(ey - R * 0.2)} Q${n(nearX + R * 0.05)} ${n(ey - R * 0.3)} ${n(nearX + R * 0.26)} ${n(ey - R * 0.18 + dip)}`, browColor, weight));
      out.push(line(`M${n(farX - R * 0.12)} ${n(ey - R * 0.17 + dip)} Q${n(farX)} ${n(ey - R * 0.26)} ${n(farX + R * 0.14)} ${n(ey - R * 0.2)}`, browColor, weight * 0.8));
    }
    return out.join("");
  }

  function mouthAndNose(p, hx, hy, R, s) {
    const out = [];
    const mouth = p.mouth || "line";
    const female = p.sex === "female";
    if (p.head === "lizard") {
      out.push(line(`M${n(hx + R * 0.6)} ${n(hy + R * 0.42)} Q${n(hx + R * 1.1)} ${n(hy + R * 0.46)} ${n(hx + R * 1.5)} ${n(hy + R * 0.3)}`, INK, 1.2), dot(hx + R * 1.42, hy - R * 0.02, R * 0.05, INK));
      out.push(flat(`M${n(hx + R * 0.4)} ${n(hy + R * 0.48)} Q${n(hx + R)} ${n(hy + R * 0.55)} ${n(hx + R * 1.45)} ${n(hy + R * 0.38)} L${n(hx + R * 1.3)} ${n(hy + R * 0.62)} Q${n(hx + R * 0.9)} ${n(hy + R * 0.8)} ${n(hx + R * 0.4)} ${n(hy + R * 0.75)}Z`, INK, 'opacity=".35"'));
      return out.join("");
    }
    if (p.head === "skull") {
      out.push(line(`M${n(hx + R * 0.2)} ${n(hy + R * 0.66)} h${n(R * 0.55)} M${n(hx + R * 0.32)} ${n(hy + R * 0.55)} v${n(R * 0.3)} M${n(hx + R * 0.5)} ${n(hy + R * 0.55)} v${n(R * 0.32)} M${n(hx + R * 0.66)} ${n(hy + R * 0.56)} v${n(R * 0.28)}`, INK, 0.8));
      out.push(flat(`M${n(hx + R * 0.86)} ${n(hy + R * 0.16)} l${n(-R * 0.1)} ${n(R * 0.22)} h${n(R * 0.16)}Z`, INK));
      return out.join("");
    }
    // Noses: drawn as hard ink planes.
    const nose = p.nose || "small";
    if (nose === "small") {
      const size = female ? 0.82 : 1;
      out.push(flat(`M${n(hx + R * 0.76)} ${n(hy - R * 0.08)} L${n(hx + R * (0.76 + 0.32 * size))} ${n(hy + R * 0.36 * size)} L${n(hx + R * 0.84)} ${n(hy + R * 0.42 * size)}Z`, s.skin));
      out.push(line(`M${n(hx + R * 0.78)} ${n(hy - R * 0.06)} L${n(hx + R * (0.76 + 0.32 * size))} ${n(hy + R * 0.36 * size)} L${n(hx + R * 0.84)} ${n(hy + R * 0.43 * size)}`, OUTLINE, 0.8));
      out.push(flat(`M${n(hx + R * 0.76)} ${n(hy + R * 0.08)} L${n(hx + R * 0.84)} ${n(hy + R * 0.43 * size)} L${n(hx + R * 0.66)} ${n(hy + R * 0.4 * size)}Z`, INK, 'opacity=".45"'));
    } else if (nose === "big") out.push(path(`M${n(hx + R * 0.74)} ${n(hy - R * 0.1)} Q${n(hx + R * 1.38)} ${n(hy + R * 0.2)} ${n(hx + R * 1.06)} ${n(hy + R * 0.52)} Q${n(hx + R * 0.86)} ${n(hy + R * 0.56)} ${n(hx + R * 0.74)} ${n(hy + R * 0.42)}Z`, s.skin, 'stroke-width=".9"') + flat(`M${n(hx + R * 0.74)} ${n(hy + R * 0.1)} Q${n(hx + R * 0.86)} ${n(hy + R * 0.5)} ${n(hx + R * 0.74)} ${n(hy + R * 0.42)}Z`, INK, 'opacity=".5"'));
    else if (nose === "long") out.push(path(`M${n(hx + R * 0.72)} ${n(hy - R * 0.14)} Q${n(hx + R * 1.5)} ${n(hy + R * 0.05)} ${n(hx + R * 1.75)} ${n(hy + R * 0.36)} Q${n(hx + R * 1.3)} ${n(hy + R * 0.42)} ${n(hx + R * 0.82)} ${n(hy + R * 0.4)}Z`, s.skin, 'stroke-width=".9"') + flat(`M${n(hx + R * 0.82)} ${n(hy + R * 0.2)} Q${n(hx + R * 1.3)} ${n(hy + R * 0.32)} ${n(hx + R * 1.7)} ${n(hy + R * 0.36)} Q${n(hx + R * 1.2)} ${n(hy + R * 0.42)} ${n(hx + R * 0.82)} ${n(hy + R * 0.4)}Z`, INK, 'opacity=".5"'));
    else if (nose === "snout") out.push(path(`M${n(hx + R * 0.78)} ${n(hy + R * 0.02)} Q${n(hx + R * 1.3)} ${n(hy - R * 0.02)} ${n(hx + R * 1.28)} ${n(hy + R * 0.36)} Q${n(hx + R * 1.06)} ${n(hy + R * 0.52)} ${n(hx + R * 0.8)} ${n(hy + R * 0.42)}Z`, s.skin, 'stroke-width=".9"') + dot(hx + R * 1.16, hy + R * 0.22, R * 0.05, INK) + dot(hx + R * 1.02, hy + R * 0.26, R * 0.04, INK));
    // Mouths: thin and hard.
    const mx = hx + R * 0.66, my = hy + R * 0.72;
    if (mouth === "grin") out.push(path(`M${n(mx - R * 0.24)} ${n(my - R * 0.04)} L${n(mx + R * 0.28)} ${n(my - R * 0.08)} L${n(mx + R * 0.2)} ${n(my + R * 0.1)} L${n(mx + R * 0.1)} ${n(my + R * 0.02)} L${n(mx)} ${n(my + R * 0.12)} L${n(mx - R * 0.1)} ${n(my + R * 0.03)}Z`, "#d8cfb8", 'stroke-width=".8"'));
    else if (mouth === "fangs") out.push(line(`M${n(mx - R * 0.2)} ${n(my)} Q${n(mx)} ${n(my + R * 0.04)} ${n(mx + R * 0.24)} ${n(my - R * 0.02)}`, INK, 1), flat(`M${n(mx + R * 0.02)} ${n(my + R * 0.01)} l${n(R * 0.04)} ${n(R * 0.16)} l${n(R * 0.05)} ${n(-R * 0.15)}Z`, "#e8e0cc"));
    else if (mouth === "tusks") out.push(line(`M${n(mx - R * 0.26)} ${n(my + R * 0.06)} Q${n(mx)} ${n(my + R * 0.12)} ${n(mx + R * 0.26)} ${n(my + R * 0.02)}`, INK, 1.1), path(`M${n(mx + R * 0.1)} ${n(my + R * 0.08)} q${n(R * 0.02)} ${n(-R * 0.3)} ${n(R * 0.16)} ${n(-R * 0.38)} q${n(-R * 0.02)} ${n(R * 0.2)} ${n(-R * 0.05)} ${n(R * 0.4)}Z`, "#e0d6bc", 'stroke-width=".8"'));
    else if (mouth === "teeth") out.push(path(`M${n(mx - R * 0.24)} ${n(my - R * 0.04)} h${n(R * 0.48)} l${n(-R * 0.08)} ${n(R * 0.16)} l${n(-R * 0.08)} ${n(-R * 0.1)} l${n(-R * 0.08)} ${n(R * 0.1)} l${n(-R * 0.08)} ${n(-R * 0.1)} l${n(-R * 0.08)} ${n(R * 0.1)}Z`, "#d8cfb8", 'stroke-width=".7"'));
    else if (mouth === "frown") out.push(line(`M${n(mx - R * 0.2)} ${n(my + R * 0.06)} Q${n(mx)} ${n(my - R * 0.04)} ${n(mx + R * 0.24)} ${n(my + R * 0.05)}`, INK, 1));
    else {
      out.push(line(`M${n(mx - R * 0.18)} ${n(my)} Q${n(mx + R * 0.02)} ${n(my + R * (mouth === "smile" ? 0.06 : 0.02))} ${n(mx + R * 0.24)} ${n(my - R * 0.03)}`, INK, female ? 0.8 : 1));
      if (female) out.push(ellipse(mx + R * 0.02, my + R * 0.08, R * 0.13, R * 0.045, "#7a3a36", 'stroke="none" opacity=".75"'));
    }
    // Hollow cheek and a hard line under the cheekbone.
    if (!["skull", "lizard"].includes(p.head)) out.push(flat(`M${n(hx + R * 0.28)} ${n(hy + R * 0.3)} Q${n(hx + R * 0.46)} ${n(hy + R * 0.55)} ${n(hx + R * 0.38)} ${n(hy + R * 0.86)} L${n(hx + R * 0.22)} ${n(hy + R * 0.72)}Z`, INK, 'opacity=".2"'));
    if (p.scar) out.push(line(`M${n(hx + R * 0.24)} ${n(hy - R * 0.42)} l${n(R * 0.2)} ${n(R * 0.5)}`, "#8a4030", 1, 'opacity=".9"'));
    if (p.freckles) out.push(dot(hx + R * 0.5, hy + R * 0.28, 0.45, darken(s.skin, 0.35)), dot(hx + R * 0.64, hy + R * 0.32, 0.45, darken(s.skin, 0.35)), dot(hx + R * 0.78, hy + R * 0.26, 0.45, darken(s.skin, 0.35)));
    return out.join("");
  }

  /* ---------------- Hair, beards, horns, crests ---------------- */
  function hair(h, hx, hy, R) {
    if (!h || !h.style || h.style === "bald") return { back: "", front: "" };
    const c = h.color || "#4a3526", dark = darken(c, 0.28), light = lighten(c, 0.22);
    const cap = `M${n(hx + R * 0.78)} ${n(hy - R * 0.42)} C${n(hx + R * 0.7)} ${n(hy - R * 1.18)} ${n(hx - R * 0.9)} ${n(hy - R * 1.25)} ${n(hx - R * 1.06)} ${n(hy - R * 0.25)} C${n(hx - R * 1.1)} ${n(hy + R * 0.1)} ${n(hx - R * 0.95)} ${n(hy + R * 0.35)} ${n(hx - R * 0.8)} ${n(hy + R * 0.45)} L${n(hx - R * 0.35)} ${n(hy - R * 0.1)} C${n(hx - R * 0.05)} ${n(hy - R * 0.45)} ${n(hx + R * 0.4)} ${n(hy - R * 0.5)} ${n(hx + R * 0.78)} ${n(hy - R * 0.42)}Z`;
    // Hair is lit like the head: a few strands catch the light, the back falls into ink.
    const shine = K.clipped(cap, `<path d="M${n(hx - R * 1.6)} ${n(hy - R * 1.6)} L${n(hx - R * 0.2)} ${n(hy - R * 1.6)} Q${n(hx - R * 0.55)} ${n(hy - R * 0.5)} ${n(hx - R * 0.1)} ${n(hy + R * 0.6)} L${n(hx - R * 1.6)} ${n(hy + R * 0.8)}Z" fill="${INK}" opacity=".45"/>`)
      + line(`M${n(hx - R * 0.3)} ${n(hy - R * 0.92)} Q${n(hx + R * 0.05)} ${n(hy - R * 1.04)} ${n(hx + R * 0.38)} ${n(hy - R * 0.86)}`, light, 0.8, 'opacity=".45"')
      + line(`M${n(hx - R * 0.78)} ${n(hy - R * 0.3)} Q${n(hx - R * 0.7)} ${n(hy - R * 0.8)} ${n(hx - R * 0.25)} ${n(hy - R * 0.92)}`, INK, 0.6, 'opacity=".5"');
    const fringe = (count = 3, length = 0.42) => {
      const tipY = hy - R * 0.64 + R * length * 0.32;
      const d = `M${n(hx - R * 0.2)} ${n(hy - R * 0.95)} Q${n(hx + R * 0.75)} ${n(hy - R * 0.98)} ${n(hx + R * 0.92)} ${n(tipY)} Q${n(hx + R * 0.62)} ${n(hy - R * 0.56)} ${n(hx + R * 0.25)} ${n(hy - R * 0.6)} Q${n(hx)} ${n(hy - R * 0.56)} ${n(hx - R * 0.25)} ${n(hy - R * 0.66)}Z`;
      let locks = "";
      if (count >= 3) locks += `M${n(hx + R * 0.2)} ${n(hy - R * 0.86)} Q${n(hx + R * 0.45)} ${n(hy - R * 0.72)} ${n(hx + R * 0.5)} ${n(hy - R * 0.6)} `;
      if (count >= 4) locks += `M${n(hx - R * 0.12)} ${n(hy - R * 0.88)} Q${n(hx + R * 0.08)} ${n(hy - R * 0.74)} ${n(hx + R * 0.1)} ${n(hy - R * 0.6)} `;
      // The fringe sits inside the hairline, so it is filled without an outline.
      return flat(d, c) + (locks ? line(locks, INK, 0.6, 'opacity=".55"') : "");
    };
    const style = h.style;
    let back = "", front = "";
    if (style === "crop") { front = path(cap, c) + fringe(2, 0.28) + shine; }
    else if (style === "spiky") {
      const spikes = `M${n(hx + R * 0.8)} ${n(hy - R * 0.4)} L${n(hx + R * 0.95)} ${n(hy - R * 0.95)} L${n(hx + R * 0.45)} ${n(hy - R * 0.85)} L${n(hx + R * 0.5)} ${n(hy - R * 1.38)} L${n(hx)} ${n(hy - R * 1.02)} L${n(hx - R * 0.25)} ${n(hy - R * 1.42)} L${n(hx - R * 0.5)} ${n(hy - R * 0.98)} L${n(hx - R * 1.0)} ${n(hy - R * 1.12)} L${n(hx - R * 0.95)} ${n(hy - R * 0.65)} L${n(hx - R * 1.32)} ${n(hy - R * 0.42)} L${n(hx - R * 1.02)} ${n(hy + R * 0.05)} L${n(hx - R * 0.8)} ${n(hy + R * 0.42)} L${n(hx - R * 0.3)} ${n(hy - R * 0.15)} Q${n(hx + R * 0.3)} ${n(hy - R * 0.55)} ${n(hx + R * 0.8)} ${n(hy - R * 0.4)}Z`;
      front = path(spikes, c) + shine;
    } else if (style === "swept") {
      front = path(`M${n(hx + R * 0.92)} ${n(hy - R * 0.55)} C${n(hx + R * 0.75)} ${n(hy - R * 1.35)} ${n(hx - R * 1.0)} ${n(hy - R * 1.3)} ${n(hx - R * 1.2)} ${n(hy - R * 0.1)} C${n(hx - R * 1.2)} ${n(hy + R * 0.3)} ${n(hx - R * 1.0)} ${n(hy + R * 0.5)} ${n(hx - R * 0.8)} ${n(hy + R * 0.48)} L${n(hx - R * 0.3)} ${n(hy - R * 0.15)} C${n(hx + R * 0.1)} ${n(hy - R * 0.62)} ${n(hx + R * 0.5)} ${n(hy - R * 0.68)} ${n(hx + R * 0.92)} ${n(hy - R * 0.55)}Z`, c) + line(`M${n(hx + R * 0.6)} ${n(hy - R * 0.75)} Q${n(hx)} ${n(hy - R * 1.1)} ${n(hx - R * 0.85)} ${n(hy - R * 0.5)}`, dark, 1) + shine;
    } else if (style === "curly" || style === "curls") {
      const bumps = [];
      for (let i = 0; i < 6; i++) {
        const a = Math.PI * (1.0 + i * 0.16), x = hx - R * 0.1 + Math.cos(a) * R * 0.98, y = hy - R * 0.15 + Math.sin(a) * R * 0.98;
        bumps.push(circle(x, y, R * 0.3, c));
      }
      if (style === "curls") for (let i = 0; i < 3; i++) back += circle(hx - R * (0.85 - i * 0.05), hy + R * (0.25 + i * 0.32), R * 0.27, c);
      front = bumps.join("") + flat(cap, c) + fringe(2, 0.24) + shine;
    } else if (style === "mohawk") {
      front = path(`M${n(hx + R * 0.55)} ${n(hy - R * 0.85)} L${n(hx + R * 0.3)} ${n(hy - R * 1.45)} L${n(hx)} ${n(hy - R * 1.1)} L${n(hx - R * 0.35)} ${n(hy - R * 1.5)} L${n(hx - R * 0.55)} ${n(hy - R * 1.05)} L${n(hx - R * 0.95)} ${n(hy - R * 1.25)} L${n(hx - R * 0.95)} ${n(hy - R * 0.62)} Q${n(hx - R * 0.2)} ${n(hy - R * 1.05)} ${n(hx + R * 0.55)} ${n(hy - R * 0.85)}Z`, c);
    } else if (style === "topknot") {
      front = path(cap, c) + circle(hx - R * 0.35, hy - R * 1.12, R * 0.3, c) + line(`M${n(hx - R * 0.55)} ${n(hy - R * 0.92)} h${n(R * 0.38)}`, dark, 1.2) + shine;
    } else if (style === "ponytail" || style === "highponytail") {
      const ty = style === "highponytail" ? hy - R * 0.75 : hy - R * 0.2;
      back = path(`M${n(hx - R * 0.85)} ${n(ty)} C${n(hx - R * 1.6)} ${n(ty + R * 0.2)} ${n(hx - R * 1.65)} ${n(ty + R * 1.3)} ${n(hx - R * 1.25)} ${n(ty + R * 1.75)} C${n(hx - R * 1.32)} ${n(ty + R * 1.1)} ${n(hx - R * 1.2)} ${n(ty + R * 0.55)} ${n(hx - R * 0.75)} ${n(ty + R * 0.35)}Z`, c) + line(`M${n(hx - R * 1.3)} ${n(ty + R * 0.4)} Q${n(hx - R * 1.45)} ${n(ty + R * 0.95)} ${n(hx - R * 1.3)} ${n(ty + R * 1.4)}`, dark, 1);
      front = path(cap, c) + fringe(3, 0.36) + circle(hx - R * 0.88, ty + R * 0.12, R * 0.12, h.tie || "#8a3b3b") + shine;
    } else if (style === "long" || style === "wavy") {
      const wave = style === "wavy";
      back = path(`M${n(hx - R * 0.2)} ${n(hy - R * 0.9)} C${n(hx - R * 1.35)} ${n(hy - R * 0.8)} ${n(hx - R * 1.45)} ${n(hy + R * 0.6)} ${n(hx - R * 1.3)} ${n(hy + R * 1.65)} ${wave ? `Q${n(hx - R * 1.05)} ${n(hy + R * 1.5)} ${n(hx - R * 0.95)} ${n(hy + R * 1.75)} Q${n(hx - R * 0.72)} ${n(hy + R * 1.5)}` : `L${n(hx - R * 0.95)} ${n(hy + R * 1.7)} L`} ${n(hx - R * 0.55)} ${n(hy + R * 1.62)} L${n(hx - R * 0.4)} ${n(hy + R * 0.6)}Z`, c) + line(`M${n(hx - R * 1.05)} ${n(hy + R * 0.1)} Q${n(hx - R * 1.2)} ${n(hy + R * 0.85)} ${n(hx - R * 1.05)} ${n(hy + R * 1.45)}`, dark, 1);
      front = path(cap, c) + path(`M${n(hx - R * 0.02)} ${n(hy - R * 0.6)} Q${n(hx - R * 0.16)} ${n(hy + R * 0.25)} ${n(hx - R * 0.46)} ${n(hy + R * 0.88)} Q${n(hx - R * 0.52)} ${n(hy + R * 0.2)} ${n(hx - R * 0.32)} ${n(hy - R * 0.5)}Z`, c, 'stroke-width="1"') + fringe(3, 0.45) + shine;
    } else if (style === "bob") {
      front = path(`M${n(hx + R * 0.85)} ${n(hy - R * 0.4)} C${n(hx + R * 0.8)} ${n(hy - R * 1.25)} ${n(hx - R * 1.05)} ${n(hy - R * 1.3)} ${n(hx - R * 1.15)} ${n(hy - R * 0.2)} C${n(hx - R * 1.2)} ${n(hy + R * 0.35)} ${n(hx - R * 1.05)} ${n(hy + R * 0.7)} ${n(hx - R * 0.65)} ${n(hy + R * 0.72)} L${n(hx - R * 0.35)} ${n(hy + R * 0.25)} L${n(hx - R * 0.3)} ${n(hy - R * 0.15)} C${n(hx + R * 0.1)} ${n(hy - R * 0.45)} ${n(hx + R * 0.5)} ${n(hy - R * 0.48)} ${n(hx + R * 0.85)} ${n(hy - R * 0.4)}Z`, c) + fringe(3, 0.4) + shine;
    } else if (style === "bun") {
      back = circle(hx - R * 0.95, hy - R * 0.55, R * 0.34, c) + line(`M${n(hx - R * 1.15)} ${n(hy - R * 0.62)} q${n(R * 0.2)} ${n(R * 0.18)} ${n(R * 0.4)} 0`, dark, 1);
      front = path(cap, c) + fringe(2, 0.32) + shine;
    } else if (style === "braid" || style === "twinbraids") {
      const braid = (x0, y0, length) => {
        const parts = [];
        for (let i = 0; i < length; i++) parts.push(ellipse(x0 - i * R * 0.04, y0 + i * R * 0.28, R * 0.17, R * 0.17, c));
        return parts.join("") + circle(x0 - length * R * 0.04, y0 + length * R * 0.28 - R * 0.05, R * 0.08, h.tie || "#7a5a2a");
      };
      back = braid(hx - R * 0.95, hy + R * 0.05, style === "twinbraids" ? 4 : 6) + (style === "twinbraids" ? braid(hx - R * 0.55, hy + R * 0.35, 4) : "");
      front = path(cap, c) + fringe(3, 0.34) + shine;
    } else if (style === "pixie") {
      front = path(cap, c) + fringe(4, 0.5) + shine;
    } else if (style === "messy") {
      front = path(cap, c) + path(`M${n(hx + R * 0.85)} ${n(hy - R * 0.45)} l${n(R * 0.18)} ${n(R * 0.25)} l${n(-R * 0.2)} ${n(-R * 0.05)} l${n(R * 0.05)} ${n(R * 0.3)} l${n(-R * 0.25)} ${n(-R * 0.25)}Z`, c, 'stroke-width="1"') + fringe(3, 0.38) + shine;
    } else if (style === "shaggy") {
      back = path(`M${n(hx - R * 0.6)} ${n(hy - R * 0.6)} C${n(hx - R * 1.4)} ${n(hy - R * 0.4)} ${n(hx - R * 1.35)} ${n(hy + R * 0.5)} ${n(hx - R * 1.15)} ${n(hy + R * 0.95)} L${n(hx - R * 0.95)} ${n(hy + R * 0.7)} L${n(hx - R * 0.85)} ${n(hy + R * 1.0)} L${n(hx - R * 0.55)} ${n(hy + R * 0.5)}Z`, c);
      front = path(cap, c) + fringe(3, 0.4) + shine;
    } else if (style === "leaves") {
      const leaves = [];
      for (let i = 0; i < 8; i++) {
        const a = Math.PI * (1.0 + i * 0.13), x = hx - R * 0.1 + Math.cos(a) * R * 1.02, y = hy - R * 0.1 + Math.sin(a) * R * 1.02;
        leaves.push(at(x, y, path(`M0 0 Q${n(R * 0.22)} ${n(-R * 0.25)} 0 ${n(-R * 0.55)} Q${n(-R * 0.22)} ${n(-R * 0.25)} 0 0Z`, i % 2 ? c : light, 'stroke-width="1"'), (a * 180 / Math.PI) + 90));
      }
      back = path(`M${n(hx - R * 0.3)} ${n(hy - R * 0.8)} C${n(hx - R * 1.4)} ${n(hy - R * 0.6)} ${n(hx - R * 1.3)} ${n(hy + R * 0.9)} ${n(hx - R * 1.0)} ${n(hy + R * 1.4)} L${n(hx - R * 0.5)} ${n(hy + R * 0.6)}Z`, dark);
      front = leaves.join("") + flat(cap, c) + fringe(2, 0.3);
    } else if (style === "flame") {
      front = `<g class="sp-flicker">` + path(`M${n(hx + R * 0.7)} ${n(hy - R * 0.5)} Q${n(hx + R * 0.9)} ${n(hy - R * 1.2)} ${n(hx + R * 0.35)} ${n(hy - R * 1.6)} Q${n(hx + R * 0.3)} ${n(hy - R * 1.1)} ${n(hx - R * 0.05)} ${n(hy - R * 1.05)} Q${n(hx - R * 0.15)} ${n(hy - R * 1.75)} ${n(hx - R * 0.65)} ${n(hy - R * 1.85)} Q${n(hx - R * 0.45)} ${n(hy - R * 1.25)} ${n(hx - R * 0.9)} ${n(hy - R * 1.1)} Q${n(hx - R * 1.4)} ${n(hy - R * 0.4)} ${n(hx - R * 0.95)} ${n(hy + R * 0.2)} Q${n(hx - R * 0.2)} ${n(hy - R * 0.4)} ${n(hx + R * 0.7)} ${n(hy - R * 0.5)}Z`, c) + flat(`M${n(hx + R * 0.2)} ${n(hy - R * 0.6)} Q${n(hx + R * 0.2)} ${n(hy - R * 1.1)} ${n(hx - R * 0.25)} ${n(hy - R * 1.3)} Q${n(hx - R * 0.2)} ${n(hy - R * 0.9)} ${n(hx - R * 0.6)} ${n(hy - R * 0.7)} Q${n(hx - R * 0.2)} ${n(hy - R * 0.5)} ${n(hx + R * 0.2)} ${n(hy - R * 0.6)}Z`, light) + "</g>";
    }
    return { back, front };
  }

  function beard(b, hx, hy, R) {
    if (!b || !b.style) return "";
    const c = b.color || "#5a3a24", dark = darken(c, 0.3);
    const style = b.style;
    if (style === "stubble") return flat(`M${n(hx + R * 0.15)} ${n(hy + R * 0.5)} Q${n(hx + R * 0.55)} ${n(hy + R * 1.02)} ${n(hx + R * 0.95)} ${n(hy + R * 0.45)} L${n(hx + R * 0.88)} ${n(hy + R * 0.7)} Q${n(hx + R * 0.5)} ${n(hy + R * 1.05)} ${n(hx + R * 0.12)} ${n(hy + R * 0.7)}Z`, c, 'opacity=".35"');
    if (style === "mustache") return path(`M${n(hx + R * 0.45)} ${n(hy + R * 0.42)} Q${n(hx + R * 0.7)} ${n(hy + R * 0.32)} ${n(hx + R * 0.95)} ${n(hy + R * 0.42)} Q${n(hx + R * 0.82)} ${n(hy + R * 0.55)} ${n(hx + R * 0.68)} ${n(hy + R * 0.48)} Q${n(hx + R * 0.55)} ${n(hy + R * 0.56)} ${n(hx + R * 0.45)} ${n(hy + R * 0.42)}Z`, c, 'stroke-width="1"');
    if (style === "goatee") return path(`M${n(hx + R * 0.5)} ${n(hy + R * 0.72)} Q${n(hx + R * 0.68)} ${n(hy + R * 1.25)} ${n(hx + R * 0.85)} ${n(hy + R * 0.7)}Z`, c, 'stroke-width="1"') + beard({ style: "mustache", color: c }, hx, hy, R);
    const length = style === "long" || style === "braided" ? 1.05 : style === "forked" ? 0.95 : 0.55;
    let d = `M${n(hx - R * 0.25)} ${n(hy + R * 0.2)} Q${n(hx - R * 0.05)} ${n(hy + R * 0.5)} ${n(hx + R * 0.35)} ${n(hy + R * 0.45)} Q${n(hx + R * 0.7)} ${n(hy + R * 0.35)} ${n(hx + R * 1.0)} ${n(hy + R * 0.42)} Q${n(hx + R * 1.05)} ${n(hy + R * (0.7 + length * 0.4))} ${n(hx + R * 0.7)} ${n(hy + R * (0.85 + length * 0.7))}`;
    if (style === "forked") d += ` L${n(hx + R * 0.55)} ${n(hy + R * 1.25)} L${n(hx + R * 0.42)} ${n(hy + R * 1.62)}`;
    d += ` Q${n(hx + R * 0.05)} ${n(hy + R * (0.95 + length * 0.55))} ${n(hx - R * 0.35)} ${n(hy + R * 0.55)}Z`;
    let out = path(d, c) + line(`M${n(hx + R * 0.35)} ${n(hy + R * 0.75)} Q${n(hx + R * 0.45)} ${n(hy + R * (0.95 + length * 0.4))} ${n(hx + R * 0.55)} ${n(hy + R * (1.05 + length * 0.5))}`, dark, 1);
    out += path(`M${n(hx + R * 0.45)} ${n(hy + R * 0.42)} Q${n(hx + R * 0.72)} ${n(hy + R * 0.3)} ${n(hx + R * 1.0)} ${n(hy + R * 0.43)} Q${n(hx + R * 0.82)} ${n(hy + R * 0.58)} ${n(hx + R * 0.66)} ${n(hy + R * 0.5)} Q${n(hx + R * 0.52)} ${n(hy + R * 0.58)} ${n(hx + R * 0.45)} ${n(hy + R * 0.42)}Z`, lighten(c, 0.08), 'stroke-width="1"');
    if (style === "braided") for (let i = 0; i < 3; i++) out += circle(hx + R * (0.6 - i * 0.02), hy + R * (1.2 + i * 0.22), R * 0.1, b.tie || "#c9a24a", 'stroke-width="0.8"');
    return out;
  }

  function horns(h, hx, hy, R) {
    if (!h || !h.style) return { back: "", front: "" };
    const c = h.color || "#d9cdb4", dark = darken(c, 0.3);
    const style = h.style;
    if (style === "small") return { back: "", front: path(`M${n(hx + R * 0.2)} ${n(hy - R * 0.85)} Q${n(hx + R * 0.3)} ${n(hy - R * 1.35)} ${n(hx + R * 0.05)} ${n(hy - R * 1.4)} Q${n(hx + R * 0.0)} ${n(hy - R * 1.05)} ${n(hx - R * 0.15)} ${n(hy - R * 0.9)}Z`, c) };
    if (style === "devil") return { back: path(`M${n(hx - R * 0.45)} ${n(hy - R * 0.82)} Q${n(hx - R * 0.95)} ${n(hy - R * 1.35)} ${n(hx - R * 0.55)} ${n(hy - R * 1.85)} Q${n(hx - R * 0.6)} ${n(hy - R * 1.35)} ${n(hx - R * 0.15)} ${n(hy - R * 0.95)}Z`, dark), front: path(`M${n(hx + R * 0.25)} ${n(hy - R * 0.85)} Q${n(hx + R * 0.15)} ${n(hy - R * 1.5)} ${n(hx + R * 0.7)} ${n(hy - R * 1.95)} Q${n(hx + R * 0.5)} ${n(hy - R * 1.4)} ${n(hx + R * 0.65)} ${n(hy - R * 0.95)}Z`, c) + line(`M${n(hx + R * 0.3)} ${n(hy - R * 1.2)} l${n(R * 0.22)} ${n(-R * 0.05)}`, dark, 0.8) };
    if (style === "ram" || style === "goat") {
      const curl = style === "ram" ? 1 : 0.6;
      const molten = h.molten ? `<g class="sp-glow" style="color:#ff7a2e">${line(`M${n(hx - R * 0.2)} ${n(hy - R * 0.98)} C${n(hx - R * 0.7)} ${n(hy - R * 1.3)} ${n(hx - R * 1.2)} ${n(hy - R * 0.9)} ${n(hx - R * 1.0)} ${n(hy - R * 0.35)}`, "!#ff8a3d", 0.9)}</g>` : "";
      return { back: "", front: path(`M${n(hx - R * 0.05)} ${n(hy - R * 0.85)} C${n(hx - R * 0.6)} ${n(hy - R * (1.5 + curl * 0.1))} ${n(hx - R * (1.4 + curl * 0.2))} ${n(hy - R * 1.0)} ${n(hx - R * (1.15 + curl * 0.1))} ${n(hy - R * 0.25)} C${n(hx - R * 1.0)} ${n(hy + R * 0.15 * curl)} ${n(hx - R * 0.6)} ${n(hy - R * 0.1)} ${n(hx - R * 0.72)} ${n(hy - R * 0.42)} C${n(hx - R * 0.95)} ${n(hy - R * 0.9)} ${n(hx - R * 0.45)} ${n(hy - R * 1.05)} ${n(hx + R * 0.2)} ${n(hy - R * 0.75)}Z`, c) + line(`M${n(hx - R * 0.5)} ${n(hy - R * 1.12)} l${n(-R * 0.05)} ${n(R * 0.2)} M${n(hx - R * 0.85)} ${n(hy - R * 0.95)} l${n(R * 0.05)} ${n(R * 0.2)}`, dark, 0.8) + molten };
    }
    if (style === "long") return { back: path(`M${n(hx - R * 0.35)} ${n(hy - R * 0.85)} Q${n(hx - R * 1.3)} ${n(hy - R * 1.4)} ${n(hx - R * 2.0)} ${n(hy - R * 1.5)} Q${n(hx - R * 1.2)} ${n(hy - R * 1.05)} ${n(hx - R * 0.6)} ${n(hy - R * 0.5)}Z`, dark), front: path(`M${n(hx + R * 0.1)} ${n(hy - R * 0.9)} Q${n(hx - R * 0.8)} ${n(hy - R * 1.6)} ${n(hx - R * 1.7)} ${n(hy - R * 1.85)} Q${n(hx - R * 0.8)} ${n(hy - R * 1.2)} ${n(hx - R * 0.25)} ${n(hy - R * 0.55)}Z`, c) };
    if (style === "antlers") {
      const branch = line(`M${n(hx - R * 0.2)} ${n(hy - R * 0.85)} q${n(-R * 0.2)} ${n(-R * 0.7)} ${n(-R * 0.75)} ${n(-R * 1.0)} M${n(hx - R * 0.42)} ${n(hy - R * 1.3)} q${n(R * 0.25)} ${n(-R * 0.15)} ${n(R * 0.3)} ${n(-R * 0.55)} M${n(hx - R * 0.65)} ${n(hy - R * 1.6)} q${n(-R * 0.35)} ${n(-R * 0.05)} ${n(-R * 0.55)} ${n(-R * 0.35)}`, c, R * 0.13);
      return { back: "", front: branch };
    }
    if (style === "great-antlers") {
      // A tree-mother's antlers: a wide crown of two great branching beams, one sweeping back and
      // one forward, hung with moss and flowering at the tines in the season's colour
      // (h.blossoms); h.frost rimes them white.
      const dark = darken(c, 0.3), width = Math.max(2.4, R * 0.2);
      const beam = (ox, dir, k) => {
        const x0 = hx + R * ox, y0 = hy - R * 0.8, P2 = (dx, dy) => `${n(x0 + R * dx * dir * k)} ${n(y0 - R * dy * k)}`;
        const main = `M${P2(0, 0)} C${P2(0.15, 0.9)} ${P2(0.75, 1.35)} ${P2(1.15, 2.05)} S${P2(1.55, 3.0)} ${P2(1.35, 3.6)}`;
        const tines = `M${P2(0.25, 0.9)} Q${P2(-0.2, 1.4)} ${P2(-0.35, 2.05)} M${P2(0.85, 1.6)} Q${P2(0.55, 2.2)} ${P2(0.45, 2.85)} M${P2(1.15, 2.05)} Q${P2(1.75, 2.15)} ${P2(2.2, 2.0)} M${P2(1.42, 2.75)} Q${P2(1.95, 3.0)} ${P2(2.25, 3.35)} M${P2(1.3, 3.1)} Q${P2(0.95, 3.45)} ${P2(0.85, 3.95)}`;
        const tips = [[-0.35, 2.05], [0.45, 2.85], [2.2, 2.0], [2.25, 3.35], [0.85, 3.95], [1.35, 3.6]].map(([dx, dy]) => [x0 + R * dx * dir * k, y0 - R * dy * k]);
        return { main, tines, tips, P2 };
      };
      const draw = (b, color, w) => line(b.main + " " + b.tines, INK, w + 1.8) + line(b.main + " " + b.tines, color, w);
      const backBeam = beam(-0.3, -1, 0.95), frontBeam = beam(0.25, 1, 1.05);
      const moss = h.moss ? b => line([[0.45, 1.2], [1.0, 1.85], [1.3, 2.4]].map(([dx, dy]) => `M${b.P2(dx, dy)} q${n(R * 0.06)} ${n(R * 0.4)} ${n(-R * 0.1)} ${n(R * 0.75)}`).join(" "), h.moss, 1.6, 'opacity=".9"') : () => "";
      const bloom = (tips, small) => h.blossoms ? tips.map(([x, y]) => circle(x, y, R * (small ? 0.14 : 0.18), h.blossoms, 'stroke-width=".6"') + dot(x, y, R * 0.06, lighten(h.blossoms, 0.5))).join("") : "";
      const frost = h.frost ? line(frontBeam.main + " " + backBeam.main, "!#f4fbff", 1.2, 'opacity=".7"') : "";
      return { back: draw(backBeam, dark, width * 0.9) + moss(backBeam) + bloom(backBeam.tips, true), front: draw(frontBeam, c, width) + line(frontBeam.main, lighten(c, 0.3), width * 0.35, 'opacity=".6"') + moss(frontBeam) + bloom(frontBeam.tips, false) + frost };
    }
    if (style === "jagged-crown") {
      // Barbed horns grown into a ring around the skull, the rear ones taller; void light
      // seeps through cracks in them.
      const spike = (x0, y0, bw, h, lean) => `M${n(x0 - bw / 2)} ${n(y0)} L${n(x0 - bw * 0.34 + lean * 0.3)} ${n(y0 - h * 0.42)} L${n(x0 - bw * 0.52 + lean * 0.42)} ${n(y0 - h * 0.48)} L${n(x0 + lean)} ${n(y0 - h)} L${n(x0 + bw * 0.12 + lean * 0.62)} ${n(y0 - h * 0.62)} L${n(x0 + bw * 0.36 + lean * 0.56)} ${n(y0 - h * 0.58)} L${n(x0 + bw / 2)} ${n(y0)}Z`;
      const base = hy - R * 0.78;
      const rear = [[-0.95, 0.42, 1.9, -0.55], [-0.45, 0.46, 2.45, -0.2], [0.15, 0.44, 2.25, 0.18], [0.62, 0.36, 1.6, 0.5]];
      const fore = [[-0.7, 0.36, 1.35, -0.4], [-0.1, 0.4, 1.7, 0.05], [0.45, 0.34, 1.25, 0.42]];
      const spikes = (list, fill) => path(list.map(([x, w, h, lean]) => spike(hx + R * x, base + R * 0.08, R * w, R * h, R * lean)).join(" "), fill, 'stroke-width="1.2"');
      const band = path(`M${n(hx - R * 1.02)} ${n(base + R * 0.22)} Q${n(hx - R * 0.2)} ${n(base - R * 0.2)} ${n(hx + R * 0.86)} ${n(base + R * 0.1)} L${n(hx + R * 0.8)} ${n(base + R * 0.42)} Q${n(hx - R * 0.15)} ${n(base + R * 0.12)} ${n(hx - R * 0.98)} ${n(base + R * 0.52)}Z`, c, 'stroke-width="1.2"');
      const cracks = h.glow ? `<g class="sp-glow" style="color:${h.glow}">${line(`M${n(hx - R * 0.42)} ${n(base - R * 0.4)} l${n(R * 0.08)} ${n(-R * 0.5)} l${n(-R * 0.1)} ${n(-R * 0.4)} M${n(hx + R * 0.16)} ${n(base - R * 0.35)} l${n(-R * 0.06)} ${n(-R * 0.45)} M${n(hx - R * 0.6)} ${n(base + R * 0.3)} Q${n(hx)} ${n(base - R * 0.05)} ${n(hx + R * 0.6)} ${n(base + R * 0.22)}`, "!" + h.glow, 0.7)}</g>` : "";
      const sheen = line(fore.map(([x, w, hh, lean]) => `M${n(hx + R * (x + lean * 0.5 - w * 0.1))} ${n(base - R * hh * 0.25)} l${n(R * lean * 0.25)} ${n(-R * hh * 0.4)}`).join(" "), lighten(c, 0.35), 0.6, 'opacity=".7"');
      return { back: spikes(rear, dark), front: band + spikes(fore, c) + sheen + cracks };
    }
    if (style === "crystal") return { back: "", front: path(`M${n(hx - R * 0.1)} ${n(hy - R * 0.85)} L${n(hx - R * 0.05)} ${n(hy - R * 1.6)} L${n(hx + R * 0.2)} ${n(hy - R * 0.88)}Z M${n(hx - R * 0.55)} ${n(hy - R * 0.72)} L${n(hx - R * 0.75)} ${n(hy - R * 1.3)} L${n(hx - R * 0.3)} ${n(hy - R * 0.82)}Z M${n(hx + R * 0.35)} ${n(hy - R * 0.78)} L${n(hx + R * 0.6)} ${n(hy - R * 1.25)} L${n(hx + R * 0.58)} ${n(hy - R * 0.68)}Z`, c) };
    return { back: "", front: "" };
  }

  function crest(cr, hx, hy, R) {
    if (!cr || !cr.style) return "";
    const c = cr.color || "#c9533a", dark = darken(c, 0.25);
    if (cr.style === "fin") return path(`M${n(hx + R * 0.45)} ${n(hy - R * 0.92)} L${n(hx + R * 0.05)} ${n(hy - R * 1.45)} L${n(hx - R * 0.15)} ${n(hy - R * 1.0)} L${n(hx - R * 0.55)} ${n(hy - R * 1.32)} L${n(hx - R * 0.65)} ${n(hy - R * 0.8)} L${n(hx - R * 1.05)} ${n(hy - R * 0.98)} L${n(hx - R * 0.95)} ${n(hy - R * 0.4)}Z`, c) + line(`M${n(hx + R * 0.05)} ${n(hy - R * 1.3)} L${n(hx - R * 0.05)} ${n(hy - R * 0.95)} M${n(hx - R * 0.55)} ${n(hy - R * 1.2)} L${n(hx - R * 0.55)} ${n(hy - R * 0.85)}`, dark, 0.8);
    if (cr.style === "spines") {
      const spines = [];
      for (let i = 0; i < 5; i++) { const x = hx + R * (0.4 - i * 0.32), y = hy - R * (0.95 - i * 0.12); spines.push(`M${n(x)} ${n(y)} L${n(x - R * 0.2)} ${n(y - R * 0.42)} L${n(x - R * 0.28)} ${n(y + R * 0.05)}`); }
      return path(spines.join(" ") + "Z", c, 'stroke-width="1"');
    }
    if (cr.style === "frill") return path(`M${n(hx - R * 0.55)} ${n(hy - R * 0.3)} Q${n(hx - R * 1.6)} ${n(hy - R * 0.8)} ${n(hx - R * 1.45)} ${n(hy + R * 0.2)} Q${n(hx - R * 1.65)} ${n(hy + R * 0.7)} ${n(hx - R * 0.65)} ${n(hy + R * 0.5)}Z`, c) + line(`M${n(hx - R * 0.65)} ${n(hy)} L${n(hx - R * 1.35)} ${n(hy - R * 0.45)} M${n(hx - R * 0.65)} ${n(hy + R * 0.2)} L${n(hx - R * 1.45)} ${n(hy + R * 0.2)} M${n(hx - R * 0.65)} ${n(hy + R * 0.38)} L${n(hx - R * 1.35)} ${n(hy + R * 0.6)}`, dark, 0.8);
    if (cr.style === "feathers") {
      const feathers = [];
      for (let i = 0; i < 4; i++) feathers.push(at(hx - R * (0.2 + i * 0.25), hy - R * (0.85 - i * 0.1), path(`M0 0 Q${n(R * 0.18)} ${n(-R * 0.5)} ${n(-R * 0.05)} ${n(-R * 0.95)} Q${n(-R * 0.18)} ${n(-R * 0.45)} 0 0Z`, i % 2 ? c : lighten(c, 0.2), 'stroke-width="1"'), -25 - i * 14));
      return feathers.join("");
    }
    return "";
  }

  function headwear(w, hx, hy, R) {
    if (!w || !w.style) return { back: "", front: "" };
    const c = w.color || "#e0b54a", dark = darken(c, 0.3), light = lighten(c, 0.3);
    if (w.style === "crown") return { back: "", front: path(`M${n(hx - R * 0.65)} ${n(hy - R * 0.72)} L${n(hx - R * 0.7)} ${n(hy - R * 1.32)} L${n(hx - R * 0.38)} ${n(hy - R * 1.05)} L${n(hx - R * 0.1)} ${n(hy - R * 1.45)} L${n(hx + R * 0.18)} ${n(hy - R * 1.08)} L${n(hx + R * 0.5)} ${n(hy - R * 1.35)} L${n(hx + R * 0.5)} ${n(hy - R * 0.78)} Q${n(hx - R * 0.1)} ${n(hy - R * 0.62)} ${n(hx - R * 0.65)} ${n(hy - R * 0.72)}Z`, c) + dot(hx - R * 0.1, hy - R * 0.85, R * 0.07, w.gem || "#c43a4a") + line(`M${n(hx - R * 0.6)} ${n(hy - R * 0.85)} Q${n(hx - R * 0.1)} ${n(hy - R * 0.78)} ${n(hx + R * 0.45)} ${n(hy - R * 0.9)}`, light, 0.8) };
    // An archdevil's burning crown: a band of saffron gold set with a gem, crowned by tongues of
    // hellfire that rise higher with his infernal power ("blaze" 1 to 3).
    if (w.style === "hellfire-crown") {
      const blaze = Math.max(1, Math.min(3, w.blaze || 1)), flame = w.flame || "#ff5a2a";
      const tongue = (x, height, lean) => `M${n(hx + R * (x - 0.13))} ${n(hy - R * 0.84)} Q${n(hx + R * (x - 0.12 + lean))} ${n(hy - R * (0.84 + height * 0.6))} ${n(hx + R * (x + lean * 1.6))} ${n(hy - R * (0.84 + height))} Q${n(hx + R * (x + 0.12 + lean))} ${n(hy - R * (0.84 + height * 0.5))} ${n(hx + R * (x + 0.13))} ${n(hy - R * 0.84)}Z`;
      const tongues = [[-0.5, 0.45, -0.06], [-0.18, 0.7, -0.02], [0.14, 0.62, 0.03], [0.46, 0.42, 0.07]].map(([x, h, lean]) => path(tongue(x, h * (1.0 + blaze * 0.45), lean), "!" + flame, 'stroke-width=".7"') + path(tongue(x, h * (1.0 + blaze * 0.45) * 0.55, lean * 0.5), "!#ffd27a", 'stroke="none"')).join("");
      const band = path(`M${n(hx - R * 0.72)} ${n(hy - R * 0.62)} Q${n(hx)} ${n(hy - R * 0.92)} ${n(hx + R * 0.74)} ${n(hy - R * 0.66)} L${n(hx + R * 0.74)} ${n(hy - R * 0.48)} Q${n(hx)} ${n(hy - R * 0.74)} ${n(hx - R * 0.72)} ${n(hy - R * 0.44)}Z`, c, 'stroke-width=".8"')
        + [-0.5, -0.18, 0.14, 0.46].map(x => path(`M${n(hx + R * (x - 0.08))} ${n(hy - R * 0.74)} L${n(hx + R * x)} ${n(hy - R * 0.96)} L${n(hx + R * (x + 0.08))} ${n(hy - R * 0.74)}Z`, light, 'stroke-width=".6"')).join("")
        + `<g class="sp-glow" style="color:${w.gem || "#ff4a2e"}">${dot(hx, hy - R * 0.72, R * 0.09, "!" + (w.gem || "#ff4a2e"))}</g>`;
      return { back: `<g class="sp-glow sp-flicker" style="color:${flame}">${tongues}</g>`, front: band };
    }
    // The land's own crown: a canopy of leaves over the brow, set with blossoms.
    if (w.style === "canopy-crown") {
      const blossom = w.blossom || "#f4a8c8";
      const leaf = (x, y, size, turn, fill) => `<g transform="translate(${n(hx + R * x)} ${n(hy - R * y)}) rotate(${turn})">${path(`M0 0 Q${n(size * 0.5)} ${n(-size * 0.55)} 0 ${n(-size * 1.2)} Q${n(-size * 0.5)} ${n(-size * 0.55)} 0 0Z`, fill, 'stroke-width=".8"')}${line(`M0 0 L0 ${n(-size)}`, darken(fill, 0.35), 0.5)}</g>`;
      const backLeaves = [[-0.95, 0.55, -60], [-0.7, 0.95, -35], [-0.25, 1.15, -10], [0.25, 1.15, 12], [0.7, 0.95, 38], [0.95, 0.55, 62]].map(([x, y, t], i) => leaf(x, y, R * 0.78, t, i % 2 ? darken(c, 0.15) : c)).join("");
      const frontLeaves = [[-0.75, 0.62, -48], [-0.3, 0.86, -18], [0.2, 0.88, 14], [0.66, 0.66, 44]].map(([x, y, t], i) => leaf(x, y, R * 0.55, t, i % 2 ? light : c)).join("");
      const blossoms = [[-0.52, 0.86], [0.08, 1.02], [0.58, 0.84]].map(([x, y]) => [0, 72, 144, 216, 288].map(a => `<ellipse cx="${n(hx + R * x + Math.cos(a * Math.PI / 180) * R * 0.1)}" cy="${n(hy - R * y + Math.sin(a * Math.PI / 180) * R * 0.1)}" rx="${n(R * 0.09)}" ry="${n(R * 0.06)}" transform="rotate(${a} ${n(hx + R * x + Math.cos(a * Math.PI / 180) * R * 0.1)} ${n(hy - R * y + Math.sin(a * Math.PI / 180) * R * 0.1)})" fill="${K.paint(blossom)}" stroke="#130e0b" stroke-width=".4"/>`).join("") + dot(hx + R * x, hy - R * y, R * 0.05, "#ffe66b")).join("");
      return { back: backLeaves, front: frontLeaves + blossoms };
    }
    if (w.style === "storm-crown") {
      const horn = (x, lean, height) => `M${n(hx + R * x - R * 0.09)} ${n(hy - R * 0.7)} Q${n(hx + R * (x + lean * 0.4))} ${n(hy - R * (0.7 + height * 0.6))} ${n(hx + R * (x + lean))} ${n(hy - R * (0.7 + height))} Q${n(hx + R * (x + lean * 0.3) + R * 0.1)} ${n(hy - R * (0.7 + height * 0.5))} ${n(hx + R * x + R * 0.1)} ${n(hy - R * 0.68)}Z`;
      const horns = [[-0.6, -0.45, 0.9], [-0.15, -0.1, 1.25], [0.3, 0.2, 1.0], [0.62, 0.42, 0.7]].map(([x, lean, h]) => path(horn(x, lean, h), dark, 'stroke-width=".8"')).join("");
      const band = path(`M${n(hx - R * 0.8)} ${n(hy - R * 0.6)} Q${n(hx)} ${n(hy - R * 0.9)} ${n(hx + R * 0.82)} ${n(hy - R * 0.64)} L${n(hx + R * 0.82)} ${n(hy - R * 0.46)} Q${n(hx)} ${n(hy - R * 0.72)} ${n(hx - R * 0.8)} ${n(hy - R * 0.42)}Z`, c, 'stroke-width=".9"');
      return { back: "", front: horns + band + `<g class="sp-glow" style="color:${w.gem || "#ffe66b"}">${path(`M${n(hx + R * 0.05)} ${n(hy - R * 0.92)} l${n(R * 0.1)} ${n(R * 0.14)} l${n(-R * 0.06)} 0 l${n(R * 0.08)} ${n(R * 0.14)} l${n(-R * 0.2)} ${n(-R * 0.18)} l${n(R * 0.07)} 0Z`, "!" + (w.gem || "#ffe66b"), 'stroke-width=".4"')}</g>` };
    }
    if (w.style === "coral-crown") {
      // Five thick branches of living coral, each forking once, set on a gold band with pearls.
      const branch = (x, lean, height, fork) => {
        const bx = hx + R * x, by = hy - R * 0.66, tx = bx + R * lean, ty = by - R * height, w0 = R * 0.14;
        const fx = bx + R * lean * 0.55, fy = by - R * height * 0.55, ftx = fx + R * fork * 0.32, fty = fy - R * height * 0.32;
        return `M${n(bx - w0)} ${n(by)} Q${n(bx - w0 + R * lean * 0.3)} ${n((by + ty) / 2)} ${n(tx - w0 * 0.45)} ${n(ty)} Q${n(tx)} ${n(ty - w0 * 0.8)} ${n(tx + w0 * 0.45)} ${n(ty)} L${n(fx + w0 * 0.6)} ${n(fy + R * 0.04)} L${n(ftx - w0 * 0.35)} ${n(fty)} Q${n(ftx)} ${n(fty - w0 * 0.7)} ${n(ftx + w0 * 0.35)} ${n(fty)} L${n(fx + w0 * 1.1)} ${n(fy + R * 0.16)} Q${n(bx + w0 + R * lean * 0.3)} ${n((by + fy) / 2 + R * 0.1)} ${n(bx + w0)} ${n(by)}Z`;
      };
      const coral = [[-0.55, -0.3, 0.95, -1], [-0.25, -0.12, 1.35, -1], [0.05, 0.03, 1.6, 1], [0.35, 0.18, 1.3, 1], [0.62, 0.32, 0.9, 1]]
        .map(([x, lean, h, fork], i) => path(branch(x, lean, h, fork), i % 2 ? darken(c, 0.12) : c, 'stroke-width=".7"')).join("");
      const band = path(`M${n(hx - R * 0.75)} ${n(hy - R * 0.62)} Q${n(hx)} ${n(hy - R * 0.9)} ${n(hx + R * 0.8)} ${n(hy - R * 0.66)} L${n(hx + R * 0.8)} ${n(hy - R * 0.5)} Q${n(hx)} ${n(hy - R * 0.72)} ${n(hx - R * 0.75)} ${n(hy - R * 0.45)}Z`, w.band || "#c9a54a", 'stroke-width=".9"');
      const pearls = [[-0.4, 1.35], [0.07, 2.05], [0.52, 1.6]].map(([x, y]) => dot(hx + R * x, hy - R * y, R * 0.08, "!" + (w.gem || "#f4ecdc"), 'stroke="#130e0b" stroke-width=".4"')).join("");
      return { back: "", front: coral + band + pearls + dot(hx, hy - R * 0.7, R * 0.09, "!#9fe0ff", 'stroke="#130e0b" stroke-width=".5"') };
    }
    if (w.style === "circlet") return { back: "", front: line(`M${n(hx - R * 0.9)} ${n(hy - R * 0.45)} Q${n(hx)} ${n(hy - R * 0.72)} ${n(hx + R * 0.9)} ${n(hy - R * 0.5)}`, c, R * 0.1) + dot(hx + R * 0.55, hy - R * 0.6, R * 0.08, w.gem || "#7fb4ff", 'stroke="#2b231e" stroke-width=".7"') };
    if (w.style === "wizard") {
      return { back: "", front: path(`M${n(hx - R * 1.25)} ${n(hy - R * 0.55)} Q${n(hx)} ${n(hy - R * 0.95)} ${n(hx + R * 1.2)} ${n(hy - R * 0.62)} Q${n(hx + R * 0.6)} ${n(hy - R * 0.45)} ${n(hx - R * 1.25)} ${n(hy - R * 0.55)}Z`, c) + path(`M${n(hx - R * 0.72)} ${n(hy - R * 0.72)} Q${n(hx - R * 0.4)} ${n(hy - R * 1.6)} ${n(hx - R * 1.1)} ${n(hy - R * 2.2)} Q${n(hx + R * 0.1)} ${n(hy - R * 1.8)} ${n(hx + R * 0.68)} ${n(hy - R * 0.78)}Z`, c) + line(`M${n(hx - R * 0.66)} ${n(hy - R * 0.92)} Q${n(hx)} ${n(hy - R * 1.08)} ${n(hx + R * 0.6)} ${n(hy - R * 0.95)}`, w.band || light, R * 0.12) + (w.star ? flat(K.star(hx - R * 0.3, hy - R * 1.45, R * 0.18, R * 0.07, 4), w.star) : "") };
    }
    if (w.style === "hood") {
      const back = path(`M${n(hx - R * 0.2)} ${n(hy - R * 1.1)} C${n(hx - R * 1.45)} ${n(hy - R * 1.0)} ${n(hx - R * 1.5)} ${n(hy + R * 0.6)} ${n(hx - R * 1.1)} ${n(hy + R * 1.25)} L${n(hx - R * 0.2)} ${n(hy + R * 1.1)}Z`, dark);
      const front = path(`M${n(hx + R * 0.95)} ${n(hy - R * 0.25)} C${n(hx + R * 1.05)} ${n(hy - R * 1.25)} ${n(hx - R * 1.0)} ${n(hy - R * 1.45)} ${n(hx - R * 1.2)} ${n(hy - R * 0.2)} C${n(hx - R * 1.25)} ${n(hy + R * 0.55)} ${n(hx - R * 0.95)} ${n(hy + R * 1.0)} ${n(hx - R * 0.55)} ${n(hy + R * 1.05)} L${n(hx - R * 0.45)} ${n(hy + R * 0.6)} C${n(hx - R * 0.5)} ${n(hy - R * 0.3)} ${n(hx + R * 0.1)} ${n(hy - R * 0.72)} ${n(hx + R * 0.95)} ${n(hy - R * 0.25)}Z`, c) + line(`M${n(hx + R * 0.85)} ${n(hy - R * 0.45)} C${n(hx + R * 0.2)} ${n(hy - R * 0.8)} ${n(hx - R * 0.45)} ${n(hy - R * 0.4)} ${n(hx - R * 0.55)} ${n(hy + R * 0.6)}`, dark, 1);
      return { back, front };
    }
    if (w.style === "bandana") return { back: path(`M${n(hx - R * 0.9)} ${n(hy - R * 0.2)} l${n(-R * 0.55)} ${n(R * 0.25)} l${n(R * 0.15)} ${n(R * 0.25)}Z`, c), front: path(`M${n(hx + R * 0.88)} ${n(hy - R * 0.4)} C${n(hx + R * 0.7)} ${n(hy - R * 1.15)} ${n(hx - R * 0.95)} ${n(hy - R * 1.15)} ${n(hx - R * 1.02)} ${n(hy - R * 0.25)} Q${n(hx - R * 0.1)} ${n(hy - R * 0.55)} ${n(hx + R * 0.88)} ${n(hy - R * 0.4)}Z`, c) + dot(hx - R * 0.2, hy - R * 0.75, R * 0.06, light) + dot(hx + R * 0.3, hy - R * 0.82, R * 0.06, light) };
    if (w.style === "flower") return { back: "", front: [0, 1, 2, 3, 4].map(i => ellipse(hx - R * 0.55 + Math.cos(i * 1.256) * R * 0.15, hy - R * 0.72 + Math.sin(i * 1.256) * R * 0.15, R * 0.12, R * 0.12, c, 'stroke-width=".7"')).join("") + dot(hx - R * 0.55, hy - R * 0.72, R * 0.08, "#f2d06b") };
    // A dreamer's crown: shards of dream-crystal floating in an arc above the head, touching nothing.
    if (w.style === "dream-crown") {
      const glow = w.glow || c;
      const shard = (angle, distance, size) => {
        const x = hx + Math.cos(angle) * R * distance, y = hy - R * 0.2 + Math.sin(angle) * R * distance;
        return path(`M${n(x)} ${n(y - size * 1.6)} L${n(x + size * 0.55)} ${n(y)} L${n(x)} ${n(y + size * 0.7)} L${n(x - size * 0.55)} ${n(y)}Z`, "!" + c, `stroke="${INK}" stroke-width="${n(R * 0.03)}"`);
      };
      const shards = [[-2.45, 1.18, 0.16], [-2.05, 1.32, 0.22], [-1.57, 1.42, 0.28], [-1.1, 1.32, 0.22], [-0.7, 1.18, 0.16]].map(([a, d, s]) => shard(a, d, R * s * (w.blaze ? 1.25 : 1))).join("");
      return { back: "", front: `<g class="sp-glow sp-flicker" style="color:${glow}">${shards}</g>` };
    }
    if (w.style === "bone-crown") {
      const gem = w.gem || "#7cff9a";
      const spike = (x, height, lean, width) => `M${n(hx + R * (x - width))} ${n(hy - R * 0.78)} Q${n(hx + R * (x - width * 0.6 + lean * 0.4))} ${n(hy - R * (0.78 + height * 0.55))} ${n(hx + R * (x + lean))} ${n(hy - R * (0.78 + height))} Q${n(hx + R * (x + width * 0.7 + lean * 0.5))} ${n(hy - R * (0.78 + height * 0.5))} ${n(hx + R * (x + width))} ${n(hy - R * 0.76)}Z`;
      const knuckle = (x, height, lean) => dot(hx + R * (x + lean * 0.5), hy - R * (0.78 + height * 0.52), R * 0.05, dark);
      const tall = [[-0.6, 0.8, -0.22, 0.14], [-0.24, 1.35, -0.1, 0.15], [0.14, 1.6, 0.04, 0.16], [0.52, 1.15, 0.18, 0.14]];
      const spikes = tall.map(([x, h, lean, width], i) => path(spike(x, h, lean, width), i % 2 ? c : darken(c, 0.1), 'stroke-width=".8"') + knuckle(x, h, lean)).join("");
      const band = path(`M${n(hx - R * 0.78)} ${n(hy - R * 0.6)} Q${n(hx)} ${n(hy - R * 0.92)} ${n(hx + R * 0.8)} ${n(hy - R * 0.64)} L${n(hx + R * 0.8)} ${n(hy - R * 0.46)} Q${n(hx)} ${n(hy - R * 0.74)} ${n(hx - R * 0.78)} ${n(hy - R * 0.42)}Z`, c, 'stroke-width=".8"')
        + [-0.6, -0.36, -0.12, 0.12, 0.36, 0.6].map(x => line(`M${n(hx + R * x)} ${n(hy - R * (0.5 + (1 - Math.abs(x)) * 0.16))} l0 ${n(-R * 0.16)}`, dark, 0.6)).join("")
        + [[-0.42, 0.55, -0.12, 0.1], [0.34, 0.6, 0.1, 0.1]].map(([x, h, lean, width]) => path(spike(x, h, lean, width), light, 'stroke-width=".6"')).join("")
        + `<g class="sp-glow" style="color:${gem}">${path(`M${n(hx)} ${n(hy - R * 0.88)} l${n(R * 0.1)} ${n(R * 0.14)} l${n(-R * 0.1)} ${n(R * 0.14)} l${n(-R * 0.1)} ${n(-R * 0.14)}Z`, "!" + gem, 'stroke-width=".4"')}</g>`;
      return { back: "", front: spikes + band };
    }
    if (w.style === "skullcrown") return { back: "", front: path(`M${n(hx - R * 0.75)} ${n(hy - R * 0.75)} L${n(hx - R * 0.85)} ${n(hy - R * 1.5)} L${n(hx - R * 0.45)} ${n(hy - R * 1.05)} L${n(hx - R * 0.15)} ${n(hy - R * 1.75)} L${n(hx + R * 0.1)} ${n(hy - R * 1.05)} L${n(hx + R * 0.45)} ${n(hy - R * 1.55)} L${n(hx + R * 0.55)} ${n(hy - R * 0.8)} Q${n(hx - R * 0.1)} ${n(hy - R * 0.62)} ${n(hx - R * 0.75)} ${n(hy - R * 0.75)}Z`, c) + `<g class="sp-eyeglow" style="color:${w.gem || "#7cff9a"}">` + dot(hx - R * 0.15, hy - R * 0.92, R * 0.08, w.gem || "#7cff9a") + "</g>" };
    if (w.style === "tricorn") return { back: "", front: path(`M${n(hx - R * 1.25)} ${n(hy - R * 0.55)} Q${n(hx - R * 0.6)} ${n(hy - R * 1.6)} ${n(hx + R * 0.1)} ${n(hy - R * 1.1)} Q${n(hx + R * 0.75)} ${n(hy - R * 1.55)} ${n(hx + R * 1.2)} ${n(hy - R * 0.6)} Q${n(hx)} ${n(hy - R * 0.85)} ${n(hx - R * 1.25)} ${n(hy - R * 0.55)}Z`, c) + line(`M${n(hx - R * 1.0)} ${n(hy - R * 0.68)} Q${n(hx)} ${n(hy - R * 0.98)} ${n(hx + R * 1.0)} ${n(hy - R * 0.72)}`, w.band || light, 1) };
    if (w.style === "brimhat") return { back: "", front: path(`M${n(hx - R * 1.35)} ${n(hy - R * 0.55)} Q${n(hx)} ${n(hy - R * 0.85)} ${n(hx + R * 1.35)} ${n(hy - R * 0.55)} Q${n(hx)} ${n(hy - R * 0.35)} ${n(hx - R * 1.35)} ${n(hy - R * 0.55)}Z`, c) + path(`M${n(hx - R * 0.75)} ${n(hy - R * 0.7)} Q${n(hx - R * 0.7)} ${n(hy - R * 1.45)} ${n(hx)} ${n(hy - R * 1.45)} Q${n(hx + R * 0.7)} ${n(hy - R * 1.45)} ${n(hx + R * 0.75)} ${n(hy - R * 0.7)}Z`, c) + line(`M${n(hx - R * 0.72)} ${n(hy - R * 0.85)} Q${n(hx)} ${n(hy - R * 0.98)} ${n(hx + R * 0.72)} ${n(hy - R * 0.85)}`, w.band || dark, R * 0.12) };
    return { back: "", front: "" };
  }

  /* ---------------- Warband gear (Zik Lun's trolls) ----------------
     A horned war-helm that replaces an armor's helmet, war paint, bone trophies, a war standard
     carried on the back and a war drum slung before the belly. */
  function warHelm(h, hx, hy, R) {
    const c = h.color || "#55565a", dark = darken(c, 0.3), light = lighten(c, 0.25), horn = h.horn || "#d8ccb0", trim = h.trim || "#8a2a1a";
    const hornD = (x, y, tx, ty, w) => `M${n(x - w)} ${n(y)} Q${n(x - w * 0.6)} ${n((y + ty) / 2 - R * 0.35)} ${n(tx)} ${n(ty)} Q${n(x + w * 0.3)} ${n((y + ty) / 2 - R * 0.05)} ${n(x + w)} ${n(y + w * 0.3)}Z`;
    const back = path(hornD(hx - R * 0.55, hy - R * 0.62, hx - R * 1.45, hy - R * 2.0, R * 0.2), darken(horn, 0.18));
    const domeD = `M${n(hx - R * 1.08)} ${n(hy + R * 0.05)} C${n(hx - R * 1.15)} ${n(hy - R * 1.05)} ${n(hx + R * 0.15)} ${n(hy - R * 1.45)} ${n(hx + R * 0.95)} ${n(hy - R * 0.45)} L${n(hx + R * 0.98)} ${n(hy - R * 0.18)} L${n(hx + R * 0.72)} ${n(hy - R * 0.2)} L${n(hx + R * 0.72)} ${n(hy + R * 0.3)} L${n(hx + R * 0.56)} ${n(hy + R * 0.32)} L${n(hx + R * 0.5)} ${n(hy - R * 0.22)} L${n(hx - R * 0.2)} ${n(hy - R * 0.25)} L${n(hx - R * 0.3)} ${n(hy + R * 0.45)} L${n(hx - R * 0.9)} ${n(hy + R * 0.5)}Z`;
    const dome = path(domeD, c) + K.clipped(domeD, `<path d="M${n(hx - R * 1.3)} ${n(hy - R * 0.2)} Q${n(hx)} ${n(hy - R * 0.55)} ${n(hx + R * 1.1)} ${n(hy - R * 0.1)} L${n(hx + R * 1.1)} ${n(hy + R)} L${n(hx - R * 1.3)} ${n(hy + R)}Z" fill="${INK}" opacity=".45"/>` + K.hatch(hx - R * 1.1, hy - R * 1.2, R * 0.4, R * 1.6, 72, 2.4, 0.4, 0.35))
      + line(`M${n(hx - R * 1.0)} ${n(hy - R * 0.3)} Q${n(hx)} ${n(hy - R * 0.62)} ${n(hx + R * 0.92)} ${n(hy - R * 0.32)}`, trim, R * 0.13)
      + line(`M${n(hx - R * 0.4)} ${n(hy - R * 1.1)} Q${n(hx + R * 0.2)} ${n(hy - R * 1.2)} ${n(hx + R * 0.7)} ${n(hy - R * 0.7)}`, light, 0.9, 'opacity=".6"')
      + [[-0.75, -0.5], [-0.35, -0.68], [0.1, -0.72], [0.5, -0.6]].map(([x, y]) => dot(hx + R * x, hy + R * y, R * 0.05, "#c9b38a")).join("");
    const front = dome + path(hornD(hx + R * 0.05, hy - R * 0.92, hx + R * 0.55, hy - R * 2.15, R * 0.22), horn)
      + line(`M${n(hx + R * 0.05)} ${n(hy - R * 1.25)} l${n(R * 0.18)} ${n(R * 0.05)} M${n(hx + R * 0.18)} ${n(hy - R * 1.55)} l${n(R * 0.16)} ${n(R * 0.06)}`, INK, 0.7, 'opacity=".5"');
    return { back, front };
  }
  // Talos's marks: glowing branching scars, and static crackling about the head (more as the storm grows).
  const lightningScars = (color, hx, hy, R) => `<g class="sp-glow" style="color:${color}">${line(`M${n(hx + R * 0.62)} ${n(hy - R * 0.5)} L${n(hx + R * 0.48)} ${n(hy - R * 0.18)} L${n(hx + R * 0.6)} ${n(hy - R * 0.05)} L${n(hx + R * 0.44)} ${n(hy + R * 0.32)} M${n(hx + R * 0.48)} ${n(hy - R * 0.18)} L${n(hx + R * 0.3)} ${n(hy - R * 0.08)}`, "!" + color, R * 0.06)}</g>`;
  function staticSparks(color, level, hx, hy, R) {
    const spots = [[-1.05, -0.2], [-0.9, 0.55], [0.1, -1.25], [-0.55, -1.0], [0.85, -0.9], [-1.25, 0.95], [-0.3, 1.2], [1.05, 0.1]].slice(0, 2 + level * 2);
    const bolt = ([x, y], i) => line(`M${n(hx + R * x)} ${n(hy + R * y)} l${n(R * 0.14)} ${n(R * 0.1)} l${n(-R * 0.1)} ${n(R * 0.06)} l${n(R * 0.14)} ${n(R * 0.12)}`, "!" + color, 0.9 + (i % 2) * 0.3);
    return `<g class="sp-glow sp-flicker" style="color:${color}">${spots.map(bolt).join("")}</g>`;
  }
  // Ool's sigil: a ring around a crown of three spikes over a soul-gem, glowing green.
  const oolSigil = (cx, cy, r, color) => `<g class="sp-glow" style="color:${color}">${circle(cx, cy, r, "none", `stroke="${color}" stroke-width="${n(Math.max(0.5, r * 0.16))}"`)}${path(`M${n(cx - r * 0.55)} ${n(cy + r * 0.05)} L${n(cx - r * 0.45)} ${n(cy - r * 0.5)} L${n(cx - r * 0.18)} ${n(cy - r * 0.12)} L${n(cx)} ${n(cy - r * 0.66)} L${n(cx + r * 0.18)} ${n(cy - r * 0.12)} L${n(cx + r * 0.45)} ${n(cy - r * 0.5)} L${n(cx + r * 0.55)} ${n(cy + r * 0.05)}Z`, "!" + color, 'stroke="none"')}${path(`M${n(cx)} ${n(cy + r * 0.18)} l${n(r * 0.2)} ${n(r * 0.24)} l${n(-r * 0.2)} ${n(r * 0.24)} l${n(-r * 0.2)} ${n(-r * 0.24)}Z`, "!" + color, 'stroke="none"')}</g>`;
  // A grave-knight's tabard over his plate, bearing his master's sigil.
  function graveTabard(t, g) {
    const { sy, hy, S, T } = g, cloth = t.color || "#1e2420", trim = t.trim || "#5a6a4a";
    const d = `M${n(-S * 0.32)} ${n(sy + 2)} L${n(S * 0.52)} ${n(sy + 2)} L${n(S * 0.6)} ${n(hy + g.legs * 0.4)} L${n(S * 0.42)} ${n(hy + g.legs * 0.34)} L${n(S * 0.24)} ${n(hy + g.legs * 0.44)} L${n(S * 0.04)} ${n(hy + g.legs * 0.33)} L${n(-S * 0.16)} ${n(hy + g.legs * 0.42)} L${n(-S * 0.4)} ${n(hy + g.legs * 0.36)}Z`;
    return path(d, cloth, 'stroke-width="1"') + K.clipped(d, `<rect x="${n(-S)}" y="${n(hy - 2)}" width="${n(S * 2)}" height="${n(g.legs)}" fill="${INK}" opacity=".4"/>`)
      + line(`M${n(-S * 0.28)} ${n(sy + 3.2)} L${n(S * 0.48)} ${n(sy + 3.2)} M${n(-S * 0.36)} ${n(hy + g.legs * 0.3)} L${n(S * 0.56)} ${n(hy + g.legs * 0.32)}`, trim, 0.9)
      + oolSigil(S * 0.1, sy + T * 0.42, S * 0.26, t.sigil || "#7cff9a");
  }
  // A dreamer's third eye of dream-light, an almond set upright on the brow.
  const thirdEye = (color, hx, hy, R) => `<g class="sp-eyeglow" style="color:${color}">${path(`M${n(hx + R * 0.24)} ${n(hy - R * 0.72)} Q${n(hx + R * 0.36)} ${n(hy - R * 0.55)} ${n(hx + R * 0.24)} ${n(hy - R * 0.38)} Q${n(hx + R * 0.12)} ${n(hy - R * 0.55)} ${n(hx + R * 0.24)} ${n(hy - R * 0.72)}Z`, "!" + color, `stroke="${INK}" stroke-width="${n(R * 0.03)}"`)}${dot(hx + R * 0.24, hy - R * 0.55, R * 0.05, "!" + INK)}</g>`;
  /* The glyph of a dream law, hung above its writer: a ring around a sign of the law. */
  const LAW_SIGNS = {
    "blades-of-glass": (x, y, r) => `M${n(x)} ${n(y - r * 0.62)} L${n(x)} ${n(y + r * 0.45)} M${n(x - r * 0.28)} ${n(y + r * 0.2)} L${n(x + r * 0.28)} ${n(y + r * 0.2)} M${n(x - r * 0.18)} ${n(y - r * 0.25)} L${n(x + r * 0.16)} ${n(y - r * 0.05)} L${n(x - r * 0.1)} ${n(y + r * 0.08)}`,
    "rebounding-words": (x, y, r) => `M${n(x - r * 0.4)} ${n(y + r * 0.4)} L${n(x - r * 0.4)} ${n(y - r * 0.1)} Q${n(x - r * 0.4)} ${n(y - r * 0.5)} ${n(x)} ${n(y - r * 0.5)} Q${n(x + r * 0.4)} ${n(y - r * 0.5)} ${n(x + r * 0.4)} ${n(y - r * 0.1)} L${n(x + r * 0.4)} ${n(y + r * 0.35)} M${n(x + r * 0.2)} ${n(y + r * 0.15)} L${n(x + r * 0.4)} ${n(y + r * 0.4)} L${n(x + r * 0.6)} ${n(y + r * 0.15)}`,
    "inverted-mercy": (x, y, r) => `M${n(x)} ${n(y - r * 0.5)} C${n(x - r * 0.15)} ${n(y - r * 0.05)} ${n(x - r * 0.7)} ${n(y - r * 0.05)} ${n(x - r * 0.5)} ${n(y + r * 0.35)} C${n(x - r * 0.35)} ${n(y + r * 0.6)} ${n(x - r * 0.05)} ${n(y + r * 0.45)} ${n(x)} ${n(y + r * 0.25)} C${n(x + r * 0.05)} ${n(y + r * 0.45)} ${n(x + r * 0.35)} ${n(y + r * 0.6)} ${n(x + r * 0.5)} ${n(y + r * 0.35)} C${n(x + r * 0.7)} ${n(y - r * 0.05)} ${n(x + r * 0.15)} ${n(y - r * 0.05)} ${n(x)} ${n(y - r * 0.5)}Z`,
    "slowed-time": (x, y, r) => `M${n(x - r * 0.4)} ${n(y - r * 0.55)} L${n(x + r * 0.4)} ${n(y - r * 0.55)} L${n(x - r * 0.4)} ${n(y + r * 0.55)} L${n(x + r * 0.4)} ${n(y + r * 0.55)}Z`,
    "echoing-spells": (x, y, r) => `M${n(x - r * 0.15)} ${n(y - r * 0.2)} Q${n(x + r * 0.05)} ${n(y)} ${n(x - r * 0.15)} ${n(y + r * 0.2)} M${n(x + r * 0.05)} ${n(y - r * 0.38)} Q${n(x + r * 0.35)} ${n(y)} ${n(x + r * 0.05)} ${n(y + r * 0.38)} M${n(x + r * 0.25)} ${n(y - r * 0.55)} Q${n(x + r * 0.65)} ${n(y)} ${n(x + r * 0.25)} ${n(y + r * 0.55)} M${n(x - r * 0.45)} ${n(y)} L${n(x - r * 0.2)} ${n(y)}`,
    "sealed-names": (x, y, r) => `M${n(x - r * 0.35)} ${n(y - r * 0.05)} L${n(x + r * 0.35)} ${n(y - r * 0.05)} L${n(x + r * 0.35)} ${n(y + r * 0.5)} L${n(x - r * 0.35)} ${n(y + r * 0.5)}Z M${n(x - r * 0.22)} ${n(y - r * 0.05)} L${n(x - r * 0.22)} ${n(y - r * 0.3)} Q${n(x)} ${n(y - r * 0.62)} ${n(x + r * 0.22)} ${n(y - r * 0.3)} L${n(x + r * 0.22)} ${n(y - r * 0.05)}`,
    "dreamers-ward": (x, y, r) => `M${n(x - r * 0.55)} ${n(y)} Q${n(x)} ${n(y - r * 0.45)} ${n(x + r * 0.55)} ${n(y)} Q${n(x)} ${n(y + r * 0.45)} ${n(x - r * 0.55)} ${n(y)}Z M${n(x)} ${n(y - r * 0.12)} A${n(r * 0.12)} ${n(r * 0.12)} 0 1 0 ${n(x + 0.01)} ${n(y - r * 0.12)}`,
  };
  const lawGlyph = (id, color, hx, hy, R) => LAW_SIGNS[id] ? `<g class="sp-glow sp-flicker" style="color:${color}">${circle(hx, hy - R * 2.35, R * 0.48, "none", `stroke="${color}" stroke-width="${n(R * 0.06)}"`)}${path(LAW_SIGNS[id](hx, hy - R * 2.35, R * 0.42), "none", `fill="none" stroke="${color}" stroke-width="${n(R * 0.07)}" stroke-linecap="round" stroke-linejoin="round"`)}</g>` : "";
  // Glyphs drifting about a dreaming sorcerer.
  function dreamGlyphs(color, g) {
    const { sy, hy, S, T } = g;
    const glyph = (x, y, size, turn) => `<g transform="translate(${n(x)} ${n(y)}) rotate(${turn})">${path(`M0 ${n(-size)} L${n(size * 0.6)} 0 L0 ${n(size)} L${n(-size * 0.6)} 0Z M${n(-size * 0.9)} 0 L${n(size * 0.9)} 0`, "none", `fill="none" stroke="${color}" stroke-width="${n(size * 0.22)}" stroke-linejoin="round"`)}</g>`;
    return `<g class="sp-glow sp-flicker" style="color:${color}">${[[-S * 2.1, sy + T * 0.1, 3.2, 10], [S * 2.3, sy - T * 0.15, 2.6, -15], [-S * 2.5, hy - T * 0.1, 2.4, 25], [S * 2.6, hy + 2, 3, 5]].map(([x, y, size, turn]) => glyph(x, y, size, turn)).join("")}</g>`;
  }
  // An archdevil's brand burned into a servant's brow.
  const infernalBrand = (color, hx, hy, R) => `<g class="sp-glow" style="color:${color}">${circle(hx + R * 0.2, hy - R * 0.5, R * 0.14, "none", `stroke="${color}" stroke-width="${n(R * 0.05)}"`)}${line(`M${n(hx + R * 0.2)} ${n(hy - R * 0.68)} L${n(hx + R * 0.2)} ${n(hy - R * 0.32)}`, "!" + color, R * 0.05)}</g>`;
  // The land's veins glowing through stone (dark and lifeless when cut off from the land).
  const earthVeins = (color, dim, hx, hy, R) => `<g${dim ? "" : ` class="sp-glow" style="color:${color}"`}>${line(`M${n(hx - R * 0.05)} ${n(hy - R * 0.9)} L${n(hx + R * 0.12)} ${n(hy - R * 0.55)} L${n(hx + R * 0.02)} ${n(hy - R * 0.3)} M${n(hx + R * 0.12)} ${n(hy - R * 0.55)} L${n(hx + R * 0.5)} ${n(hy - R * 0.62)} M${n(hx + R * 0.66)} ${n(hy + R * 0.05)} L${n(hx + R * 0.48)} ${n(hy + R * 0.32)}`, "!" + (dim ? "#2e3a28" : color), R * 0.07)}</g>`;
  // The smith-god's marks: forge-cracks glowing through the skin, and embers rising about him.
  const forgeCracks = (color, hx, hy, R) => `<g class="sp-glow" style="color:${color}">${line(`M${n(hx + R * 0.7)} ${n(hy - R * 0.62)} L${n(hx + R * 0.52)} ${n(hy - R * 0.36)} L${n(hx + R * 0.64)} ${n(hy - R * 0.2)} M${n(hx + R * 0.52)} ${n(hy - R * 0.36)} L${n(hx + R * 0.34)} ${n(hy - R * 0.42)} M${n(hx - R * 0.1)} ${n(hy - R * 0.78)} L${n(hx + R * 0.04)} ${n(hy - R * 0.56)} L${n(hx - R * 0.06)} ${n(hy - R * 0.44)}`, "!" + color, R * 0.06)}</g>`;
  function embers(color, level, hx, hy, R) {
    const spots = [[-1.1, -0.1], [0.9, -0.8], [-0.6, -1.15], [1.15, 0.3], [-1.3, 0.8], [0.2, -1.4], [1.3, -0.3], [-0.9, -0.7]].slice(0, 2 + level * 2);
    return `<g class="sp-glow sp-flicker" style="color:${color}">${spots.map(([x, y], i) => dot(hx + R * x, hy + R * y, R * (0.05 + (i % 3) * 0.02), "!" + (i % 2 ? color : "#ffe2a8"))).join("")}</g>`;
  }
  function pauldron(pd, x, y, w, front) {
    const metal = pd.color || "#8a6a3a", trim = pd.trim || "#e0b050";
    const d = `M${n(x - w * 0.62)} ${n(y + w * 0.38)} Q${n(x - w * 0.7)} ${n(y - w * 0.42)} ${n(x)} ${n(y - w * 0.5)} Q${n(x + w * 0.72)} ${n(y - w * 0.42)} ${n(x + w * 0.66)} ${n(y + w * 0.4)} Q${n(x)} ${n(y + w * 0.22)} ${n(x - w * 0.62)} ${n(y + w * 0.38)}Z`;
    let out = path(d, front ? metal : darken(metal, 0.2), 'stroke-width="1.2"') + K.clipped(d, `<rect x="${n(x - w)}" y="${n(y - w)}" width="${n(w * 0.7)}" height="${n(w * 2)}" fill="${INK}" opacity=".35"/>`)
      + line(`M${n(x - w * 0.55)} ${n(y + w * 0.1)} Q${n(x)} ${n(y - w * 0.12)} ${n(x + w * 0.6)} ${n(y + w * 0.12)}`, trim, 1.1)
      + dot(x - w * 0.3, y - w * 0.18, 0.9, trim) + dot(x + w * 0.32, y - w * 0.16, 0.9, trim);
    if (pd.sheen) out += `<g class="sp-glow" style="color:${pd.sheen}">${line(`M${n(x - w * 0.4)} ${n(y - w * 0.28)} Q${n(x)} ${n(y - w * 0.42)} ${n(x + w * 0.42)} ${n(y - w * 0.28)}`, "!" + pd.sheen, 1.1, 'opacity=".9"')}</g>`;
    if (pd.spikes) out += `<g class="sp-glow" style="color:${pd.spikes}">${[-0.4, 0, 0.4].map(k => path(`M${n(x + w * k - 1.6)} ${n(y - w * 0.36 + Math.abs(k) * w * 0.2)} L${n(x + w * k)} ${n(y - w * 0.95 + Math.abs(k) * w * 0.3)} L${n(x + w * k + 1.6)} ${n(y - w * 0.36 + Math.abs(k) * w * 0.2)}Z`, "!" + pd.spikes, 'stroke="#130e0b" stroke-width=".6"')).join("")}</g>`;
    return out;
  }
  const warPaint = (color, hx, hy, R) => line(`M${n(hx + R * 0.18)} ${n(hy - R * 0.02)} L${n(hx + R * 0.3)} ${n(hy + R * 0.42)} M${n(hx + R * 0.48)} ${n(hy - R * 0.08)} L${n(hx + R * 0.58)} ${n(hy + R * 0.36)}`, "!" + K.tone(color), R * 0.1, 'opacity=".85"');
  function warTrophies(t, g) {
    const { sy, hy, S } = g, bone = t.color || "#d8ccb0";
    const skull = (x, y, r) => line(`M${n(x)} ${n(hy + 1)} L${n(x)} ${n(y - r)}`, "#3a2a1e", 0.8) + path(`M${n(x - r)} ${n(y)} C${n(x - r)} ${n(y - r * 1.4)} ${n(x + r)} ${n(y - r * 1.4)} ${n(x + r)} ${n(y)} L${n(x + r * 0.6)} ${n(y + r * 0.7)} L${n(x - r * 0.6)} ${n(y + r * 0.7)}Z`, bone, 'stroke-width=".8"') + dot(x - r * 0.35, y - r * 0.15, r * 0.25, "!#1a120d") + dot(x + r * 0.35, y - r * 0.15, r * 0.25, "!#1a120d");
    const tusks = [0, 1, 2, 3, 4].map(i => { const x = -S * 0.55 + i * S * 0.3, y = sy + 3 + Math.sin(i / 4 * Math.PI) * 4; return path(`M${n(x - 1)} ${n(y)} Q${n(x - 0.6)} ${n(y + 5)} ${n(x + 1.2)} ${n(y + 6)} Q${n(x + 0.6)} ${n(y + 3)} ${n(x + 1)} ${n(y)}Z`, bone, 'stroke-width=".6"'); }).join("");
    return line(`M${n(-S * 0.65)} ${n(sy + 2)} Q0 ${n(sy + 8)} ${n(S * 0.65)} ${n(sy + 2)}`, "#3a2a1e", 0.9) + tusks + skull(S * 0.15, hy + 7, 2.6) + skull(S * 0.62, hy + 6, 2.2);
  }
  // A sash in the warband's colour, from the back shoulder across to the front hip.
  const warSash = (color, g) => { const d = `M${n(-g.S * 0.7)} ${n(g.sy + 1)} L${n(-g.S * 0.25)} ${n(g.sy - 0.5)} L${n(g.H * 0.95)} ${n(g.hy - 1)} L${n(g.H * 0.45)} ${n(g.hy + 2)}Z`; return path(d, color, 'stroke-width="1"') + K.clipped(d, `<rect x="${n(-g.S)}" y="${n(g.sy + g.T * 0.45)}" width="${n(g.S * 2)}" height="${n(g.T)}" fill="${INK}" opacity=".4"/>`); };
  function warStandard(st, g, b, top) {
    const x = -b.shoulders * 0.55, cloth = st.cloth || "#7a2416", trim = st.trim || "#d8ccb0";
    const clothD = `M${n(x)} ${n(top + 3)} L${n(x - b.shoulders * 1.5)} ${n(top + 5)} L${n(x - b.shoulders * 1.25)} ${n(top + g.T * 0.45)} L${n(x - b.shoulders * 1.55)} ${n(top + g.T * 0.85)} L${n(x)} ${n(top + g.T * 0.8)}Z`;
    const emblem = `M${n(x - b.shoulders * 0.75)} ${n(top + g.T * 0.22)} l${n(b.shoulders * 0.3)} 0 l0 ${n(g.T * 0.12)} l${n(-b.shoulders * 0.1)} 0 l0 ${n(g.T * 0.22)} l${n(-b.shoulders * 0.1)} 0 l0 ${n(-g.T * 0.22)} l${n(-b.shoulders * 0.1)} 0Z`;
    return `<g class="sp-cape">${path(clothD, cloth)}${K.clipped(clothD, `<rect x="${n(x - b.shoulders * 2)}" y="${n(top + g.T * 0.5)}" width="${n(b.shoulders * 2)}" height="${n(g.T)}" fill="${INK}" opacity=".4"/>`)}${line(`M${n(x - 1)} ${n(top + 7)} L${n(x - b.shoulders * 1.4)} ${n(top + 8.5)} M${n(x - 1)} ${n(top + g.T * 0.74)} L${n(x - b.shoulders * 1.4)} ${n(top + g.T * 0.78)}`, trim, 1.1)}${path(emblem, trim, 'stroke-width=".7"')}</g>`
      + path(`M${n(x - 1.4)} ${n(g.hy)} L${n(x - 1.4)} ${n(top)} L${n(x + 1.4)} ${n(top)} L${n(x + 1.4)} ${n(g.hy)}Z`, "#4a3322", 'stroke-width="1"') + path(`M${n(x - 3.2)} ${n(top)} L${n(x)} ${n(top - 7)} L${n(x + 3.2)} ${n(top)}Z`, "#8a8a8e", 'stroke-width=".8"');
  }
  function warDrum(d, g) {
    const { hy, S, T } = g, cx = S * 0.78, cy = hy - T * 0.12, w = S * 0.92, h = T * 0.62, wood = d.color || "#6b3a22", skin = d.skin || "#d8c8a0";
    const bodyD = `M${n(cx - w)} ${n(cy - h * 0.42)} Q${n(cx - w * 1.08)} ${n(cy)} ${n(cx - w)} ${n(cy + h * 0.42)} Q${n(cx)} ${n(cy + h * 0.6)} ${n(cx + w)} ${n(cy + h * 0.42)} Q${n(cx + w * 1.08)} ${n(cy)} ${n(cx + w)} ${n(cy - h * 0.42)}Z`;
    let ropes = "";
    for (let i = 0; i < 5; i++) { const x = cx - w * 0.8 + i * w * 0.4; ropes += `M${n(x)} ${n(cy - h * 0.38)} L${n(x + w * 0.2)} ${n(cy + h * 0.46)} `; }
    return line(`M${n(-S * 0.7)} ${n(g.sy + 2)} L${n(cx - w * 0.6)} ${n(cy - h * 0.3)}`, "#3a2a1e", 1.6)
      + path(bodyD, wood) + K.clipped(bodyD, `<rect x="${n(cx - w * 1.2)}" y="${n(cy)}" width="${n(w * 2.4)}" height="${n(h)}" fill="${INK}" opacity=".45"/>`) + line(ropes, d.trim || "#c9b38a", 0.8)
      + ellipse(cx, cy - h * 0.42, w, h * 0.2, skin, 'stroke-width="1.1"') + line(`M${n(cx - w * 0.6)} ${n(cy - h * 0.46)} Q${n(cx)} ${n(cy - h * 0.55)} ${n(cx + w * 0.55)} ${n(cy - h * 0.45)}`, lighten(skin, 0.3), 0.8, 'opacity=".7"')
      + (d.emblem ? path(`M${n(cx - 2.5)} ${n(cy - h * 0.05)} l2.5 -3 l2.5 3 l-2.5 3Z`, d.emblem, 'stroke-width=".6"') : "");
  }

  /* ---------------- Body ---------------- */
  // A broad-shouldered torso narrowing to the waist, chest toward the light.
  function torsoPath(g, female) {
    const { sy, hy, S, H, T } = g;
    const waist = female ? S * 0.6 : S * 0.76;
    const h = female ? H * 1.12 : H;
    const chest = female ? S * 0.98 : S * 0.96;
    const B = S * 0.9;
    const bust = female ? `C${n(chest + 2.2)} ${n(sy + T * 0.2)} ${n(chest + 2.6)} ${n(sy + T * 0.36)} ${n(chest - 0.5)} ${n(sy + T * 0.44)} C${n(waist + 1.2)} ${n(sy + T * 0.52)} ${n(waist)} ${n(sy + T * 0.6)} ${n(waist + 0.4)} ${n(sy + T * 0.66)}` : `C${n(chest + 1.6)} ${n(sy + T * 0.22)} ${n(chest + 0.6)} ${n(sy + T * 0.42)} ${n(waist + 1)} ${n(sy + T * 0.64)}`;
    return `M${n(-B)} ${n(sy + 6)} Q${n(-B)} ${n(sy + 0.8)} ${n(-B + 7)} ${n(sy - 0.2)} L${n(S - 6)} ${n(sy - 0.6)} Q${n(S + 1)} ${n(sy - 0.6)} ${n(S + 1)} ${n(sy + 4.5)} ${bust} C${n(waist)} ${n(sy + T * 0.78)} ${n(h + 0.8)} ${n(hy - T * 0.12)} ${n(h)} ${n(hy + 3)} L${n(-h)} ${n(hy + 3)} C${n(-h - 0.6)} ${n(hy - T * 0.12)} ${n(-waist - 1)} ${n(sy + T * 0.76)} ${n(-waist)} ${n(sy + T * 0.6)} C${n(-B * 1.02)} ${n(sy + T * 0.38)} ${n(-B - 0.4)} ${n(sy + T * 0.2)} ${n(-B)} ${n(sy + 6)}Z`;
  }
  // Ink over whatever the torso wears: the back half and the waist fall into shadow.
  function torsoShade(g, female, opacity) {
    const { sy, hy, S, H, T } = g;
    const d = torsoPath({ ...g, S: S + 2.6, H: H + 2 }, female);
    return K.clipped(d, `<path d="M${n(-S - 5)} ${n(sy - 5)} L${n(-S * 0.12)} ${n(sy - 5)} Q${n(S * 0.1)} ${n(sy + T * 0.45)} ${n(-H * 0.15)} ${n(hy + 7)} L${n(-S - 5)} ${n(hy + 7)}Z" fill="${INK}" opacity="${opacity}"/>` + K.hatch(-S * 0.3, sy, S * 0.14, T + 4, 74, 2.8, 0.4, 0.35) + `<rect x="${n(-S - 5)}" y="${n(hy - 2)}" width="${n(S * 2 + 10)}" height="7" fill="${INK}" opacity="${opacity * 0.5}"/>` + line(`M${n(-S * 0.1)} ${n(sy + T * 0.38)} Q${n(S * 0.35)} ${n(sy + T * 0.46)} ${n(S * 0.95)} ${n(sy + T * 0.36)}`, INK, 0.8, `opacity="${opacity * 0.7}"`) + line(`M${n(S * 0.7)} ${n(sy + 2)} Q${n(S * 1.05)} ${n(sy + T * 0.2)} ${n(S * 0.85)} ${n(sy + T * 0.4)}`, "!#f2e8d2", 0.7, 'opacity=".28"'));
  }

  // Void corruption: glowing cracks along the skin, dimmer on the far side.
  function veinGlow(color, d, back = false) {
    return `<g class="sp-glow" style="color:${color}"${back ? ' opacity=".55"' : ""}>${line(d, "!" + color, 0.9)}</g>`;
  }
  // Plates fused into a bare torso: one side of the chest and two belly plates; plus veins.
  function corruptTorso(p, g) {
    const { sy, hy, S, T } = g, out = [];
    if (p.voidVeins) out.push(veinGlow(p.voidVeins, `M${n(-S * 0.55)} ${n(sy + T * 0.2)} l${n(S * 0.2)} ${n(T * 0.18)} l${n(-S * 0.12)} ${n(T * 0.16)} l${n(S * 0.25)} ${n(T * 0.2)} M${n(-S * 0.4)} ${n(sy + T * 0.55)} l${n(-S * 0.3)} ${n(T * 0.1)} M${n(S * 0.05)} ${n(sy + T * 0.72)} l${n(S * 0.3)} ${n(T * 0.12)}`));
    if (p.plates) {
      const pc = p.plates.color || "#2a2433", pt = p.plates.trim || "#5b4a6e";
      out.push(path(`M${n(-S * 0.05)} ${n(sy + 2)} L${n(S * 0.92)} ${n(sy + 3)} L${n(S * 0.8)} ${n(sy + T * 0.42)} Q${n(S * 0.35)} ${n(sy + T * 0.52)} ${n(-S * 0.02)} ${n(sy + T * 0.4)}Z`, pc, 'stroke-width="1.3"'),
        line(`M${n(S * 0.05)} ${n(sy + T * 0.14)} L${n(S * 0.82)} ${n(sy + T * 0.15)} M${n(S * 0.42)} ${n(sy + 3)} L${n(S * 0.4)} ${n(sy + T * 0.46)}`, pt, 0.9),
        path(`M${n(-S * 0.6)} ${n(sy + T * 0.58)} L${n(S * 0.62)} ${n(sy + T * 0.56)} L${n(S * 0.58)} ${n(sy + T * 0.7)} L${n(-S * 0.56)} ${n(sy + T * 0.72)}Z M${n(-S * 0.52)} ${n(sy + T * 0.76)} L${n(S * 0.54)} ${n(sy + T * 0.75)} L${n(S * 0.5)} ${n(sy + T * 0.88)} L${n(-S * 0.48)} ${n(sy + T * 0.89)}Z`, pc, 'stroke-width="1.1"'),
        dot(S * 0.75, sy + T * 0.08, 0.7, pt), dot(S * 0.1, sy + T * 0.08, 0.7, pt));
    }
    return out.join("");
  }

  // Default clothing for figures that wear no armor.
  function outfitTorso(p, g, s) {
    const o = p.outfit || {};
    const female = p.sex === "female";
    const base = torsoPath(g, female);
    const color = o.color || "#5a4a3a", color2 = o.color2 || darken(color, 0.3), trim = o.trim || lighten(color, 0.3);
    const { sy, hy, S, T } = g;
    const out = [];
    const top = o.top || "tunic";
    if (top === "none" || top === "bare") {
      out.push(path(base, s.skin));
      if (p.material === "bone") out.push(line(`M${n(-S * 0.45)} ${n(sy + T * 0.26)} q${n(S * 0.5)} 3 ${n(S * 1.2)} 0 M${n(-S * 0.5)} ${n(sy + T * 0.42)} q${n(S * 0.55)} 3 ${n(S * 1.25)} 0 M${n(-S * 0.45)} ${n(sy + T * 0.58)} q${n(S * 0.5)} 3 ${n(S * 1.15)} 0 M${n(S * 0.1)} ${n(sy + 2)} v${n(T)}`, INK, 1.1), flat(`M${n(-S * 0.4)} ${n(sy + T * 0.28)} h${n(S * 1.3)} v${n(T * 0.36)} h${n(-S * 1.3)}Z`, INK, 'opacity=".35"'));
      else out.push(K.materialDecor(p.material, -S, sy, S * 2, T, "torso"));
      if (!p.material && p.chest !== false && !female) out.push(line(`M${n(S * 0.05)} ${n(sy + T * 0.34)} q${n(S * 0.4)} ${n(T * 0.1)} ${n(S * 0.85)} 0 M${n(S * 0.3)} ${n(sy + T * 0.5)} v${n(T * 0.3)} M${n(S * 0.1)} ${n(sy + T * 0.56)} h${n(S * 0.4)} M${n(S * 0.1)} ${n(sy + T * 0.7)} h${n(S * 0.35)}`, INK, 0.7, 'opacity=".55"'));
      if (top === "none" && o.belt) out.push(path(`M${n(-g.H - 0.5)} ${n(hy - 2)} h${n(g.H * 2 + 1)} v4 h${n(-g.H * 2 - 1)}Z`, o.belt));
      // Moss on the shoulders and chest; the land's veins branching through the stone.
      if (p.moss) for (const [x, y, r] of [[-0.75, 0.06, 0.2], [0.55, 0.04, 0.24], [-0.2, 0.48, 0.14], [0.35, 0.72, 0.12]]) out.push(path(`M${n(S * x - S * r)} ${n(sy + T * y)} Q${n(S * x)} ${n(sy + T * y - S * r * 1.2)} ${n(S * x + S * r)} ${n(sy + T * y)} Q${n(S * x)} ${n(sy + T * y + S * r * 0.5)} ${n(S * x - S * r)} ${n(sy + T * y)}Z`, p.moss, 'stroke-width=".7"'));
      if (p.veins) out.push(`<g${p.veinsDim ? "" : ` class="sp-glow" style="color:${p.veins}"`}>${line(`M${n(-S * 0.1)} ${n(sy + 2)} L${n(S * 0.05)} ${n(sy + T * 0.25)} L${n(-S * 0.15)} ${n(sy + T * 0.45)} L${n(S * 0.1)} ${n(sy + T * 0.7)} L${n(0)} ${n(hy - 2)} M${n(S * 0.05)} ${n(sy + T * 0.25)} L${n(S * 0.55)} ${n(sy + T * 0.32)} L${n(S * 0.8)} ${n(sy + T * 0.18)} M${n(-S * 0.15)} ${n(sy + T * 0.45)} L${n(-S * 0.6)} ${n(sy + T * 0.55)} M${n(S * 0.1)} ${n(sy + T * 0.7)} L${n(S * 0.5)} ${n(sy + T * 0.85)}`, "!" + (p.veinsDim ? "#2e3a28" : p.veins), 1.5)}</g>`);
      return out.join("");
    }
    if (top === "loincloth") {
      out.push(path(base, s.skin), K.materialDecor(p.material, -S, sy, S * 2, T, "torso"));
      if (!p.material) out.push(line(`M${n(S * 0.05)} ${n(sy + T * 0.34)} q${n(S * 0.4)} ${n(T * 0.1)} ${n(S * 0.85)} 0 M${n(S * 0.3)} ${n(sy + T * 0.5)} v${n(T * 0.3)}`, INK, 0.7, 'opacity=".5"'));
      out.push(path(`M${n(-g.H - 0.5)} ${n(hy - 3)} h${n(g.H * 2 + 1)} v5 h${n(-g.H * 2 - 1)}Z`, color2));
      out.push(path(`M${n(-2)} ${n(hy + 2)} l${n(g.H * 0.9)} 0 l${n(-1.5)} ${n(g.legs * 0.42)} l${n(-g.H * 0.4)} 2 l${n(-g.H * 0.3)} -3Z`, color), line(`M${n(g.H * 0.1)} ${n(hy + 6)} l-1 ${n(g.legs * 0.3)}`, INK, 0.7, 'opacity=".5"'));
      return out.join("");
    }
    if (top === "fur") {
      out.push(path(base, s.skin));
      out.push(path(`M${n(-S)} ${n(sy + 4)} Q${n(-S + 2)} ${n(sy - 1)} ${n(S * 0.2)} ${n(sy)} L${n(S * 0.9)} ${n(sy + T * 0.5)} L${n(g.H)} ${n(hy + 3)} L${n(-g.H)} ${n(hy + 3)}Z`, color));
      for (let i = 0; i < 6; i++) out.push(line(`M${n(-S * 0.7 + i * S * 0.3)} ${n(sy + T * 0.2 + (i % 3) * 3)} l1.2 3.4`, INK, 0.7, 'opacity=".6"'));
      return out.join("");
    }
    if (top === "leafdress") {
      out.push(path(base, color));
      for (let i = 0; i < 6; i++) out.push(at(-S * 0.7 + (i % 3) * S * 0.7, sy + T * (0.25 + Math.floor(i / 3) * 0.4), path(`M0 0 Q3.6 -3.6 0 -8.4 Q-3.6 -3.6 0 0Z`, i % 2 ? color2 : trim, 'stroke-width=".7"'), i * 40));
      return out.join("");
    }
    if (top === "feathers") {
      out.push(path(base, color));
      for (let i = 0; i < 6; i++) out.push(path(`M${n(-S * 0.8 + i * S * 0.32)} ${n(sy + T * 0.18)} q${n(S * 0.16)} ${n(T * 0.3)} 0 ${n(T * 0.58)} q${n(-S * 0.16)} ${n(-T * 0.25)} 0 ${n(-T * 0.58)}Z`, i % 2 ? color2 : trim, 'stroke-width=".7"'));
      return out.join("");
    }
    // Talos's vestments: storm-dark plate, a blood-red tabard down the front and a gold lightning bolt.
    if (top === "storm-plate") {
      out.push(path(base, color));
      for (let i = 0; i < 4; i++) out.push(line(`M${n(-S * 0.85)} ${n(sy + T * (0.28 + i * 0.17))} Q0 ${n(sy + T * (0.33 + i * 0.17))} ${n(S * 0.9)} ${n(sy + T * (0.27 + i * 0.17))}`, darken(color, 0.45), 0.8));
      const tabard = `M${n(-S * 0.18)} ${n(sy + 1)} L${n(S * 0.42)} ${n(sy + 1)} L${n(S * 0.5)} ${n(hy + 9)} L${n(S * 0.16)} ${n(hy + 6)} L${n(-S * 0.24)} ${n(hy + 9)}Z`;
      out.push(path(tabard, color2, 'stroke-width="1"'));
      out.push(path(`M${n(S * 0.18)} ${n(sy + T * 0.2)} l${n(-S * 0.14)} ${n(T * 0.24)} l${n(S * 0.12)} 0 l${n(-S * 0.1)} ${n(T * 0.26)} l${n(S * 0.26)} ${n(-T * 0.32)} l${n(-S * 0.12)} 0 l${n(S * 0.08)} ${n(-T * 0.18)}Z`, trim, 'stroke-width=".7"'));
      out.push(path(`M${n(-S - 1)} ${n(sy + 5)} Q${n(-S * 0.6)} ${n(sy - 3)} ${n(-S * 0.1)} ${n(sy + 1)} L${n(-S * 0.3)} ${n(sy + 8)} Q${n(-S * 0.75)} ${n(sy + 6)} ${n(-S - 1)} ${n(sy + 9)}Z`, lighten(color, 0.12), 'stroke-width=".9"'));
      out.push(path(`M${n(-g.H - 0.5)} ${n(hy - 1.5)} h${n(g.H * 2 + 1)} v3.6 h${n(-g.H * 2 - 1)}Z`, "#3a2a22", 'stroke-width=".9"'));
      return out.join("");
    }
    // An archdevil's robe: black cloth trimmed in saffron gold, a dark breastplate bearing the
    // Seventh Cycle's seven sigils in a ring (brighter with "sigilLevel"), and a high collar.
    if (top === "infernal-robe") {
      out.push(path(base, color));
      out.push(line(`M${n(-S * 0.05)} ${n(sy + 1)} L${n(S * 0.05)} ${n(hy)}`, trim, 1.4), line(`M${n(-S * 0.85)} ${n(sy + 4)} L${n(-g.H * 0.95)} ${n(hy)} M${n(S * 0.9)} ${n(sy + 4)} L${n(g.H * 0.95)} ${n(hy)}`, trim, 1.1, 'opacity=".8"'));
      const plate = `M${n(-S * 0.62)} ${n(sy + 2)} Q${n(0)} ${n(sy - 1)} ${n(S * 0.66)} ${n(sy + 2)} L${n(S * 0.56)} ${n(sy + T * 0.62)} Q${n(0)} ${n(sy + T * 0.74)} ${n(-S * 0.52)} ${n(sy + T * 0.62)}Z`;
      out.push(path(plate, color2, 'stroke-width="1.1"'), K.clipped(plate, `<rect x="${n(-S)}" y="${n(sy)}" width="${n(S * 0.6)}" height="${n(T)}" fill="${INK}" opacity=".35"/>`), path(plate, "none", `stroke="${K.paint(trim)}" stroke-width=".9" fill="none"`));
      const level = Math.max(1, Math.min(3, p.sigilLevel || 1)), cx = S * 0.02, cy = sy + T * 0.32, ring = S * 0.36;
      const sigils = Array.from({ length: 7 }, (_, i) => { const a = -Math.PI / 2 + i * Math.PI * 2 / 7; return dot(cx + Math.cos(a) * ring, cy + Math.sin(a) * ring * 0.85, 0.9 + level * 0.4, "!" + (o.sigils || "#ffb347")); }).join("");
      out.push(`<g class="sp-glow" style="color:${o.sigils || "#ffb347"}">${sigils}${circle(cx, cy, ring * 0.38, "none", `stroke="${o.sigils || "#ffb347"}" stroke-width="${(0.7 + level * 0.35).toFixed(2)}"`)}</g>`);
      out.push(path(`M${n(-S * 0.4)} ${n(sy - 0.5)} L${n(-S * 0.3)} ${n(sy - 4)} Q${n(S * 0.1)} ${n(sy - 2)} ${n(S * 0.5)} ${n(sy - 4)} L${n(S * 0.6)} ${n(sy - 0.5)}Z`, color, 'stroke-width=".8"'), line(`M${n(-S * 0.3)} ${n(sy - 4)} Q${n(S * 0.1)} ${n(sy - 2)} ${n(S * 0.5)} ${n(sy - 4)}`, trim, 0.9));
      out.push(path(`M${n(-g.H - 0.5)} ${n(hy - 2.4)} h${n(g.H * 2 + 1)} v4.2 h${n(-g.H * 2 - 1)}Z`, trim), path(`M${n(g.H * 0.05)} ${n(hy - 3)} h4.6 v5.4 h-4.6Z`, color2, 'stroke-width="1"'));
      return out.join("");
    }
    if (top === "dream-robe") {
      out.push(path(base, color), K.clipped(base, K.hatch(-S, sy, S * 2, T, 75, 3, 0.5, 0.3)));
      const inner = `M${n(-S * 0.1)} ${n(sy)} L${n(S * 0.5)} ${n(sy)} L${n(S * 0.42)} ${n(hy + 2)} L${n(S * 0.02)} ${n(hy + 2)}Z`;
      out.push(path(inner, "#120e20", 'stroke-width=".8"'));
      const stars = o.stars || "#f4ecff";
      out.push(`<g class="sp-glow" style="color:${stars}">${[[0.12, 0.15], [0.3, 0.3], [0.18, 0.48], [0.36, 0.62], [0.1, 0.78], [0.28, 0.9]].map(([x, y], i) => dot(S * x, sy + T * y, i % 2 ? 0.7 : 1, "!" + stars)).join("")}</g>`);
      // The open over-robe falls on either side, edged in starlit trim.
      out.push(path(`M${n(-S - 1)} ${n(sy + 2)} Q${n(-S * 0.4)} ${n(sy - 2)} ${n(-S * 0.1)} ${n(sy)} L${n(S * 0.02)} ${n(hy + 3)} L${n(-g.H - 1)} ${n(hy + 3)}Z`, color2, 'stroke-width=".9"'), line(`M${n(-S * 0.1)} ${n(sy)} L${n(S * 0.02)} ${n(hy + 3)} M${n(S * 0.5)} ${n(sy)} L${n(S * 0.42)} ${n(hy + 3)}`, trim, 1.1));
      // A high collar, pointed behind the head.
      out.push(path(`M${n(-S * 0.55)} ${n(sy + 1)} L${n(-S * 0.75)} ${n(sy - 7)} Q${n(-S * 0.2)} ${n(sy - 3)} ${n(S * 0.1)} ${n(sy - 1)} L${n(S * 0.1)} ${n(sy + 2)}Z`, color2, 'stroke-width=".8"'), line(`M${n(-S * 0.75)} ${n(sy - 7)} Q${n(-S * 0.2)} ${n(sy - 3)} ${n(S * 0.1)} ${n(sy - 1)}`, trim, 0.9));
      // A sash of dream-silk at the waist.
      out.push(path(`M${n(-g.H - 0.5)} ${n(hy - 2.4)} h${n(g.H * 2 + 1)} v4.2 h${n(-g.H * 2 - 1)}Z`, trim), path(`M${n(g.H * 0.2)} ${n(hy + 1.5)} l-1.2 ${n(g.legs * 0.32)} l2.6 -1 l0.8 ${n(-g.legs * 0.3)}Z`, trim, 'stroke-width=".7"'));
      return out.join("");
    }
    if (top === "grave-robe") {
      out.push(path(base, color), K.clipped(base, K.hatch(-S, sy, S * 2, T, 75, 3, 0.5, 0.35)));
      const cx = -S * 0.2, cy = sy + T * 0.36;
      const opening = `M${n(-S * 0.62)} ${n(sy)} L${n(S * 0.16)} ${n(sy)} L${n(-S * 0.04)} ${n(sy + T * 0.68)} L${n(-S * 0.4)} ${n(sy + T * 0.68)}Z`;
      out.push(path(opening, "!#0c0f0b", 'stroke-width=".8"'));
      const fire = p.soulFire || "#7cff9a";
      if (p.soulDimmed) out.push(path(`M${n(cx - S * 0.1)} ${n(cy + T * 0.16)} Q${n(cx)} ${n(cy - T * 0.02)} ${n(cx + S * 0.1)} ${n(cy + T * 0.16)} Q${n(cx)} ${n(cy + T * 0.24)} ${n(cx - S * 0.1)} ${n(cy + T * 0.16)}Z`, "!#2f4a34", 'stroke="none"'));
      else out.push(`<g class="sp-glow sp-flicker" style="color:${fire}">${path(`M${n(cx - S * 0.2)} ${n(cy + T * 0.2)} Q${n(cx - S * 0.24)} ${n(cy - T * 0.08)} ${n(cx - S * 0.04)} ${n(cy - T * 0.3)} Q${n(cx)} ${n(cy - T * 0.08)} ${n(cx + S * 0.08)} ${n(cy - T * 0.22)} Q${n(cx + S * 0.26)} ${n(cy)} ${n(cx + S * 0.2)} ${n(cy + T * 0.2)} Q${n(cx)} ${n(cy + T * 0.3)} ${n(cx - S * 0.2)} ${n(cy + T * 0.2)}Z`, "!" + fire, 'stroke="none" opacity=".9"')}${path(`M${n(cx - S * 0.08)} ${n(cy + T * 0.16)} Q${n(cx - S * 0.06)} ${n(cy - T * 0.04)} ${n(cx + S * 0.02)} ${n(cy - T * 0.12)} Q${n(cx + S * 0.12)} ${n(cy + T * 0.04)} ${n(cx + S * 0.08)} ${n(cy + T * 0.16)}Z`, "!#e8ffe8", 'stroke="none"')}</g>`);
      // Ribs across the opening, the breastbone down its middle.
      out.push(line(`M${n(cx)} ${n(sy + 1)} L${n(cx)} ${n(sy + T * 0.6)}`, s.skin, 1.4) + [0.14, 0.27, 0.4, 0.53].map(k => line(`M${n(-S * 0.58 + k * S * 0.32)} ${n(sy + T * k)} Q${n(cx)} ${n(sy + T * (k + 0.07))} ${n(S * 0.12 - k * S * 0.28)} ${n(sy + T * k)}`, s.skin, 1.1)).join(""));
      // A ragged mantle over the shoulders.
      const mantle = `M${n(-S - 1.5)} ${n(sy + 3)} Q${n(-S * 0.6)} ${n(sy - 2.5)} ${n(-S * 0.24)} ${n(sy - 0.5)} L${n(-S * 0.3)} ${n(sy + T * 0.3)} L${n(-S * 0.48)} ${n(sy + T * 0.22)} L${n(-S * 0.62)} ${n(sy + T * 0.36)} L${n(-S * 0.8)} ${n(sy + T * 0.24)} L${n(-S - 1.5)} ${n(sy + T * 0.32)}Z`;
      const mantleFront = `M${n(S * 0.46)} ${n(sy - 0.5)} Q${n(S * 0.8)} ${n(sy - 2)} ${n(S + 1.5)} ${n(sy + 3)} L${n(S + 1)} ${n(sy + T * 0.3)} L${n(S * 0.84)} ${n(sy + T * 0.2)} L${n(S * 0.7)} ${n(sy + T * 0.34)} L${n(S * 0.56)} ${n(sy + T * 0.22)} L${n(S * 0.4)} ${n(sy + T * 0.28)}Z`;
      out.push(path(mantle, color2, 'stroke-width=".9"'), path(mantleFront, color2, 'stroke-width=".9"'), line(`M${n(-S * 0.24)} ${n(sy - 0.5)} L${n(-S * 0.3)} ${n(sy + T * 0.3)} M${n(S * 0.46)} ${n(sy - 0.5)} L${n(S * 0.4)} ${n(sy + T * 0.28)}`, trim, 0.8));
      // A burial stole hanging down the front, his sigil stitched on it.
      const stole = `M${n(S * 0.3)} ${n(sy + T * 0.66)} L${n(S * 0.54)} ${n(sy + T * 0.66)} L${n(S * 0.6)} ${n(hy + g.legs * 0.55)} L${n(S * 0.47)} ${n(hy + g.legs * 0.5)} L${n(S * 0.36)} ${n(hy + g.legs * 0.57)}Z`;
      out.push(path(stole, color2, 'stroke-width=".8"'), oolSigil(S * 0.44, hy + g.legs * 0.12, S * 0.12, o.sigil || fire));
      // A rope girdle hung with grave-tokens.
      out.push(line(`M${n(-g.H)} ${n(hy - 1)} Q0 ${n(hy + 1.5)} ${n(g.H)} ${n(hy - 1)}`, trim, 1.6), line(`M${n(-g.H * 0.5)} ${n(hy + 0.5)} l-0.6 ${n(g.legs * 0.22)} M${n(-g.H * 0.2)} ${n(hy + 1)} l0.4 ${n(g.legs * 0.16)}`, trim, 0.7), dot(-g.H * 0.5 - 0.6, hy + 0.5 + g.legs * 0.22, 1.3, s.skin), dot(-g.H * 0.2 + 0.4, hy + 1 + g.legs * 0.16, 1.1, s.skin));
      return out.join("");
    }
    // A smith's apron: a bare chest (forge-cracked when glowing), a heavy leather apron hanging
    // past the knees on its neck strap, and a tool belt with tongs and a small hammer.
    if (top === "smith-apron") {
      out.push(path(base, s.skin));
      out.push(line(`M${n(S * 0.05)} ${n(sy + T * 0.3)} q${n(S * 0.4)} ${n(T * 0.1)} ${n(S * 0.85)} 0 M${n(-S * 0.7)} ${n(sy + T * 0.28)} q${n(S * 0.35)} ${n(T * 0.1)} ${n(S * 0.7)} 0`, INK, 0.7, 'opacity=".5"'));
      if (p.forgeCracks) out.push(`<g class="sp-glow" style="color:${p.forgeCracks}">${line(`M${n(-S * 0.55)} ${n(sy + T * 0.12)} L${n(-S * 0.35)} ${n(sy + T * 0.3)} L${n(-S * 0.5)} ${n(sy + T * 0.42)} M${n(-S * 0.35)} ${n(sy + T * 0.3)} L${n(-S * 0.12)} ${n(sy + T * 0.26)} M${n(S * 0.62)} ${n(sy + T * 0.1)} L${n(S * 0.5)} ${n(sy + T * 0.24)} L${n(S * 0.66)} ${n(sy + T * 0.34)} M${n(S * 0.1)} ${n(sy + T * 0.08)} L${n(S * 0.2)} ${n(sy + T * 0.22)} L${n(S * 0.08)} ${n(sy + T * 0.36)}`, "!" + p.forgeCracks, 1.6)}</g>`);
      const apron = `M${n(-S * 0.42)} ${n(sy + T * 0.5)} Q${n(S * 0.12)} ${n(sy + T * 0.44)} ${n(S * 0.66)} ${n(sy + T * 0.5)} L${n(g.H * 1.05)} ${n(hy + g.legs * 0.62)} Q${n(0)} ${n(hy + g.legs * 0.7)} ${n(-g.H * 0.85)} ${n(hy + g.legs * 0.62)}Z`;
      out.push(path(apron, color, 'stroke-width="1.2"'), K.clipped(apron, `<rect x="${n(-S)}" y="${n(sy)}" width="${n(S * 0.55)}" height="${n(T + g.legs)}" fill="${INK}" opacity=".35"/>` + K.hatch(-S, hy, S * 2, g.legs * 0.7, 80, 3, 0.6, 0.4)));
      // Scorch marks and a stitched hem on the leather.
      out.push(path(`M${n(S * 0.1)} ${n(hy + g.legs * 0.2)} q${n(S * 0.2)} ${n(-3)} ${n(S * 0.35)} 1 q${n(-S * 0.1)} 3 ${n(-S * 0.35)} -1Z`, darken(color, 0.4), 'stroke="none" opacity=".7"'));
      out.push(line(`M${n(-g.H * 0.78)} ${n(hy + g.legs * 0.57)} Q0 ${n(hy + g.legs * 0.64)} ${n(g.H * 0.98)} ${n(hy + g.legs * 0.57)}`, trim, 0.8, 'stroke-dasharray="1.6 1.4"'));
      out.push(line(`M${n(-S * 0.38)} ${n(sy + T * 0.5)} L${n(-S * 0.2)} ${n(sy - 0.5)} M${n(S * 0.62)} ${n(sy + T * 0.5)} L${n(S * 0.42)} ${n(sy - 0.5)}`, darken(color, 0.3), 1.6));
      out.push(path(`M${n(-g.H - 0.5)} ${n(hy - 2.4)} h${n(g.H * 2 + 1)} v4.2 h${n(-g.H * 2 - 1)}Z`, o.belt || "#3a2418"), path(`M${n(g.H * 0.1)} ${n(hy - 2.8)} h4.2 v5 h-4.2Z`, trim, 'stroke-width="1"'));
      out.push(line(`M${n(-g.H * 0.6)} ${n(hy + 2)} l-1 ${n(g.legs * 0.32)} M${n(-g.H * 0.45)} ${n(hy + 2)} l1 ${n(g.legs * 0.32)}`, "#4a4a50", 1.2));
      out.push(line(`M${n(g.H * 0.75)} ${n(hy + 2)} l0.6 ${n(g.legs * 0.22)}`, "#5a3e28", 1.4), path(`M${n(g.H * 0.75 - 2.6)} ${n(hy + 2 + g.legs * 0.22)} h5.8 v3 h-5.8Z`, "#6a6a72", 'stroke-width=".7"'));
      return out.join("");
    }
    // A sea king's armor: overlapping scallop shells over the chest, a pearl string and barnacles.
    if (top === "shell") {
      out.push(path(base, color));
      const shell = (x, y, w, h, fill) => {
        const d = `M${n(x - w / 2)} ${n(y)} Q${n(x - w * 0.55)} ${n(y - h * 0.9)} ${n(x)} ${n(y - h)} Q${n(x + w * 0.55)} ${n(y - h * 0.9)} ${n(x + w / 2)} ${n(y)} Q${n(x)} ${n(y + h * 0.18)} ${n(x - w / 2)} ${n(y)}Z`;
        return path(d, fill, 'stroke-width=".8"') + line([-0.3, -0.12, 0.06, 0.24].map(k => `M${n(x)} ${n(y + h * 0.05)} L${n(x + w * k)} ${n(y - h * 0.85)}`).join(" "), darken(fill, 0.35), 0.5, 'opacity=".8"');
      };
      for (let row = 0; row < 3; row++) for (let i = 0; i < 4; i++) out.push(shell(-S * 0.62 + i * S * 0.42 + (row % 2) * S * 0.21, sy + T * (0.42 + row * 0.2), S * 0.44, T * 0.26, (row + i) % 2 ? color2 : color));
      out.push(line(`M${n(-S * 0.7)} ${n(sy + 3)} Q0 ${n(sy + T * 0.22)} ${n(S * 0.75)} ${n(sy + 3)}`, "#3a2a1e", 0.7));
      for (let i = 0; i < 7; i++) { const t = i / 6; out.push(dot(-S * 0.7 + t * S * 1.45, sy + 3 + Math.sin(t * Math.PI) * T * 0.17, 1.15, "!#f4ecdc", 'stroke="#130e0b" stroke-width=".4"')); }
      for (const [x, y] of [[-0.5, 0.3], [0.45, 0.85], [-0.2, 0.92], [0.7, 0.4]]) out.push(circle(S * x, sy + T * y, 1.1, "#c9c0ae", 'stroke-width=".5"'));
      out.push(path(`M${n(-g.H - 0.5)} ${n(hy - 1.5)} h${n(g.H * 2 + 1)} v3.6 h${n(-g.H * 2 - 1)}Z`, trim, 'stroke-width=".9"'));
      return out.join("");
    }
    if (top === "scales") {
      out.push(path(base, color));
      for (let row = 0; row < 4; row++) for (let i = 0; i < 5; i++) out.push(line(`M${n(-S * 0.8 + i * S * 0.38 + (row % 2) * S * 0.19)} ${n(sy + T * (0.2 + row * 0.18))} q${n(S * 0.1)} 2.6 ${n(S * 0.2)} 0`, color2, 0.8));
      return out.join("");
    }
    // Shirt-like tops: tunic, vest, dress, robe; cloth hangs in inked folds.
    if (top === "vest") out.push(path(base, o.shirt || "#a8987a"), line(`M${n(S * 0.05)} ${n(sy + 2)} q${n(S * 0.12)} ${n(T * 0.2)} 0 ${n(T * 0.42)}`, INK, 0.6, 'opacity=".5"'));
    const shirt = top === "vest"
      ? `M${n(-S + 2)} ${n(sy + 1)} L${n(S * 0.05)} ${n(sy + 1)} L${n(S * 0.2)} ${n(sy + T * 0.55)} L${n(S - 1)} ${n(sy + 2)} L${n(g.H)} ${n(hy + 3)} L${n(-g.H)} ${n(hy + 3)}Z`
      : base;
    out.push(path(shirt, color));
    if (top !== "vest") out.push(line(`M${n(S * 0.15)} ${n(sy + 1)} L${n(S * 0.55)} ${n(sy + T * 0.32)}`, color2, 1.1));
    out.push(line(`M${n(S * 0.35)} ${n(sy + T * 0.5)} q-1 ${n(T * 0.2)} 1 ${n(T * 0.42)} M${n(-S * 0.05)} ${n(sy + T * 0.55)} q1.5 ${n(T * 0.2)} 0 ${n(T * 0.4)} M${n(S * 0.65)} ${n(sy + T * 0.6)} q-1 ${n(T * 0.15)} 0 ${n(T * 0.3)}`, INK, 0.65, 'opacity=".55"'));
    if (o.pattern === "stripes") for (let i = 0; i < 3; i++) out.push(line(`M${n(-S * 0.8)} ${n(sy + T * (0.3 + i * 0.2))} h${n(S * 1.6)}`, trim, 1, 'opacity=".6"'));
    if (top !== "robe") out.push(path(`M${n(-g.H - 0.5)} ${n(hy - 2.4)} h${n(g.H * 2 + 1)} v4.2 h${n(-g.H * 2 - 1)}Z`, o.belt || "#3e2c20"), path(`M${n(g.H * 0.15)} ${n(hy - 2.8)} h4.2 v5 h-4.2Z`, o.buckle || "#a8925a", 'stroke-width="1"'));
    else out.push(line(`M${n(-g.H)} ${n(hy - 1)} Q0 ${n(hy + 1)} ${n(g.H)} ${n(hy - 1)}`, trim, 1.6));
    if (o.collar !== false && top !== "vest") out.push(path(`M${n(-S * 0.3)} ${n(sy - 0.6)} Q${n(S * 0.2)} ${n(sy + 4.5)} ${n(S * 0.65)} ${n(sy - 0.6)}`, trim, 'stroke-width="1" fill-opacity="1"'));
    return out.join("");
  }

  function outfitSkirt(p, g) {
    const o = p.outfit || {};
    const top = o.top || "tunic";
    if (!["dress", "robe", "infernal-robe", "grave-robe", "dream-robe"].includes(top) && !o.skirt) return "";
    const color = o.color || "#5a4a3a", color2 = o.color2 || darken(color, 0.3);
    const long = ["robe", "infernal-robe", "grave-robe", "dream-robe"].includes(top) || o.skirt === "long";
    const bottom = long ? -2 : g.hy + g.legs * 0.5;
    const flare = long ? g.H * 1.6 : g.H * 1.35;
    const tatters = top === "grave-robe" ? Array.from({ length: 9 }, (_, i) => { const x = flare - (i + 1) * flare * 2 / 10; return `L${n(x + flare * 0.1)} ${n(bottom - 6 - (i % 3) * 3)} L${n(x)} ${n(bottom + 2 + (i % 2) * 4)}`; }).join(" ") : null;
    const d = tatters ? `M${n(-g.H)} ${n(g.hy + 2)} L${n(g.H)} ${n(g.hy + 2)} L${n(flare)} ${n(bottom)} ${tatters} L${n(-flare)} ${n(bottom)}Z` : `M${n(-g.H)} ${n(g.hy + 2)} L${n(g.H)} ${n(g.hy + 2)} L${n(flare)} ${n(bottom)} Q${n(flare * 0.3)} ${n(bottom + 2)} 0 ${n(bottom)} Q${n(-flare * 0.4)} ${n(bottom + 2.5)} ${n(-flare)} ${n(bottom)}Z`;
    const folds = line(`M${n(-g.H * 0.3)} ${n(g.hy + 4)} L${n(-flare * 0.45)} ${n(bottom)} M${n(g.H * 0.4)} ${n(g.hy + 4)} L${n(flare * 0.5)} ${n(bottom)} M${n(g.H * 0.05)} ${n(g.hy + 6)} L${n(flare * 0.05)} ${n(bottom - 1)}`, INK, 0.7, 'opacity=".55"');
    // Dream-robes are strewn with starlight down to the hem.
    const starlight = top === "dream-robe" ? `<g class="sp-glow" style="color:${o.stars || "#f4ecff"}">${[[-0.6, 0.3], [0.4, 0.45], [-0.2, 0.7], [0.75, 0.8], [-0.85, 0.88], [0.15, 0.95]].map(([x, y], i) => dot(flare * x, g.hy + (bottom - g.hy) * y, i % 2 ? 0.7 : 1, "!" + (o.stars || "#f4ecff"))).join("")}</g>` : "";
    return `<g class="sp-skirt">${path(d, color)}${folds}${K.clipped(d, K.inkShadow(-flare - 2, g.hy, flare * 2 + 4, bottom - g.hy + 4, 0.42, 0.58))}${starlight}</g>`;
  }

  // Legs hang straight from the hip: long thighs, a knee and heavy boots.
  function legShape(p, g, s, side) {
    const lower = p.lower || "legs";
    const o = p.outfit || {};
    const pants = o.pants === null ? s.skin : o.pants || "#3f362c";
    const L = g.legs, w = g.legW;
    const back = side === "back";
    const tint = c => back ? darken(c, 0.22) : c;
    const out = [];
    const shadow = K.clipped(taper(0, -2, 0, L + 2, w * 1.6, w * 1.4), K.inkShadow(-w * 1.2, -2, w * 2.4, L + 4, 0.48, back ? 0.55 : shadowOf(p.material)));
    if (lower === "goat" && p.goatBuild === "brute") {
      // Heavy reverse-jointed goat legs: a thick thigh, the hock bent back, a cloven hoof.
      const fur = tint(p.fur || "#5a4232"), furDark = darken(fur, 0.3);
      out.push(path(taper(0, -1, w * 0.45, L * 0.4, w * 1.55, w * 1.05), fur),
        path(taper(w * 0.45, L * 0.38, -w * 0.4, L * 0.68, w * 1.05, w * 0.72), fur),
        path(taper(-w * 0.4, L * 0.66, w * 0.08, L * 0.88, w * 0.72, w * 0.56), fur));
      out.push(line(`M${n(-w * 0.35)} ${n(L * 0.08)} q${n(w * 0.5)} ${n(L * 0.12)} ${n(w * 0.55)} ${n(L * 0.28)} M${n(w * 0.1)} ${n(L * 0.12)} l1.2 3.4 M${n(-w * 0.15)} ${n(L * 0.22)} l1.2 3.2`, furDark, 0.8, 'opacity=".75"'),
        path(`M${n(-w * 0.62)} ${n(L * 0.6)} l${n(-w * 0.35)} ${n(L * 0.04)} l${n(w * 0.25)} ${n(L * 0.05)} l${n(-w * 0.2)} ${n(L * 0.05)} l${n(w * 0.45)} ${n(-L * 0.02)}Z`, furDark, 'stroke-width=".8"'));
      if (p.voidVeins) out.push(veinGlow(p.voidVeins, `M${n(w * 0.25)} ${n(L * 0.04)} l${n(-w * 0.25)} ${n(L * 0.14)} l${n(w * 0.3)} ${n(L * 0.12)} M${n(w * 0.2)} ${n(L * 0.44)} l${n(-w * 0.35)} ${n(L * 0.14)}`, back));
      out.push(path(`M${n(-w * 0.32)} ${n(L * 0.86)} L${n(w * 0.52)} ${n(L * 0.86)} L${n(w * 0.78)} ${n(L)} L${n(w * 0.16)} ${n(L)} L${n(w * 0.1)} ${n(L * 0.93)} L${n(w * 0.02)} ${n(L)} L${n(-w * 0.42)} ${n(L)}Z`, tint("#17121a")));
      return out.join("") + shadow;
    }
    if (lower === "goat") {
      const fur = p.fur || "#5a4232";
      out.push(path(`M${n(-w * 0.6)} 0 Q${n(-w * 0.8)} ${n(L * 0.3)} ${n(w * 0.1)} ${n(L * 0.5)} L${n(-w * 0.2)} ${n(L * 0.84)} L${n(w * 0.4)} ${n(L * 0.86)} L${n(w * 0.7)} ${n(L * 0.47)} Q${n(w * 0.66)} ${n(L * 0.12)} ${n(w * 0.6)} 0Z`, tint(fur)));
      for (let i = 0; i < 4; i++) out.push(line(`M${n(-w * 0.4 + i * w * 0.25)} ${n(L * (0.12 + i * 0.1))} l1 3.2`, INK, 0.6, 'opacity=".6"'));
      if (p.voidVeins) out.push(veinGlow(p.voidVeins, `M${n(w * 0.2)} ${n(L * 0.04)} l${n(-w * 0.3)} ${n(L * 0.16)} l${n(w * 0.25)} ${n(L * 0.12)} l${n(-w * 0.15)} ${n(L * 0.14)}`, back));
      out.push(path(`M${n(-w * 0.35)} ${n(L * 0.82)} L${n(w * 0.6)} ${n(L * 0.82)} L${n(w * 0.72)} ${n(L)} L${n(-w * 0.4)} ${n(L)}Z`, tint("#231c18")));
      return out.join("") + shadow;
    }
    if (lower === "bird") {
      const feather = p.fur || "#6a5a4a";
      out.push(path(`M${n(-w * 0.65)} 0 Q${n(-w * 0.85)} ${n(L * 0.36)} 0 ${n(L * 0.46)} Q${n(w * 0.8)} ${n(L * 0.3)} ${n(w * 0.6)} 0Z`, tint(feather)));
      out.push(line(`M0 ${n(L * 0.44)} L${n(-w * 0.15)} ${n(L * 0.95)}`, tint("#8a7240"), 2.4));
      out.push(line(`M${n(-w * 0.15)} ${n(L * 0.95)} l${n(w * 1.0)} ${n(L * 0.05)} M${n(-w * 0.15)} ${n(L * 0.95)} l${n(w * 0.45)} ${n(L * 0.08)} M${n(-w * 0.15)} ${n(L * 0.95)} l${n(-w * 0.55)} ${n(L * 0.05)}`, tint("#2a2218"), 1.8));
      return out.join("") + shadow;
    }
    if (lower === "roots") {
      const bark = s.skin;
      out.push(path(`M${n(-w * 0.75)} 0 L${n(w * 0.75)} 0 L${n(w * 0.95)} ${n(L * 0.75)} Q${n(w * 1.7)} ${n(L * 0.95)} ${n(w * 1.9)} ${n(L)} L${n(-w * 1.3)} ${n(L)} Q${n(-w * 1.05)} ${n(L * 0.9)} ${n(-w * 0.9)} ${n(L * 0.7)}Z`, tint(bark)));
      out.push(K.materialDecor("bark", -w * 0.75, 0, w * 1.5, L * 0.8, side));
      return out.join("") + shadow;
    }
    // Ordinary legs: trousers, a knee and footwear.
    const feet = p.feet || "boots";
    const legD = taper(0, 0, 0, L - 6, w * 1.12, w * 0.7);
    out.push(path(legD, tint(pants)));
    if (o.pants === null) out.push(K.materialDecor(p.material, -w / 2, 0, w, L, side));
    out.push(line(`M${n(-w * 0.32)} ${n(L * 0.47)} q${n(w * 0.32)} ${n(w * 0.22)} ${n(w * 0.6)} ${n(-w * 0.04)}`, INK, 0.7, 'opacity=".6"'));
    if (feet === "bare" || feet === "big-bare") {
      const big = feet === "big-bare";
      out.push(path(`M${n(-w * 0.45)} ${n(L - 6)} L${n(w * 0.38)} ${n(L - 6)} Q${n(w * (big ? 1.45 : 1.05))} ${n(L - 4.5)} ${n(w * (big ? 1.55 : 1.12))} ${n(L - 0.6)} Q${n(w * 0.5)} ${n(L + 0.6)} ${n(-w * 0.55)} ${n(L)}Z`, tint(s.skin)));
      if (big && p.footHair) out.push(line(`M${n(-w * 0.2)} ${n(L - 6)} l1 -1.6 M${n(w * 0.3)} ${n(L - 6)} l1 -1.6 M${n(w * 0.7)} ${n(L - 5)} l1 -1.4`, tint(p.footHair), 1.1));
    } else if (feet === "claws" || feet === "paws") {
      out.push(path(`M${n(-w * 0.55)} ${n(L - 6)} L${n(w * 0.45)} ${n(L - 6)} Q${n(w * 1.15)} ${n(L - 4.5)} ${n(w * 1.15)} ${n(L)} L${n(-w * 0.55)} ${n(L)}Z`, tint(s.skin)));
      out.push(line(`M${n(w * 0.7)} ${n(L - 1)} l1.8 1.1 M${n(w * 0.98)} ${n(L - 2.4)} l1.8 1.3`, "#d8cfb8", 1));
    } else if (feet === "hooves") {
      out.push(path(`M${n(-w * 0.5)} ${n(L - 5)} L${n(w * 0.6)} ${n(L - 5)} L${n(w * 0.78)} ${n(L)} L${n(-w * 0.52)} ${n(L)}Z`, tint("#231c18")));
    } else {
      // Heavy boots with a folded cuff, a hard toe and a dark sole.
      const boot = o.boots || "#2e231b";
      const shoe = feet === "shoes";
      const top = shoe ? L - 6 : L * 0.62;
      out.push(path(`M${n(-w * 0.6)} ${n(top)} L${n(w * 0.55)} ${n(top)} L${n(w * 0.55)} ${n(L - 6)} Q${n(w * 1.3)} ${n(L - 5.6)} ${n(w * 1.36)} ${n(L - 1.4)} L${n(w * 1.32)} ${n(L)} L${n(-w * 0.66)} ${n(L)}Z`, tint(boot)));
      out.push(flat(`M${n(-w * 0.66)} ${n(L - 1.6)} L${n(w * 1.34)} ${n(L - 1.6)} L${n(w * 1.32)} ${n(L)} L${n(-w * 0.66)} ${n(L)}Z`, INK, 'opacity=".75"'));
      if (!shoe) out.push(path(`M${n(-w * 0.72)} ${n(top - 1)} L${n(w * 0.66)} ${n(top - 1)} L${n(w * 0.6)} ${n(top + 3.2)} L${n(-w * 0.66)} ${n(top + 3.2)}Z`, tint(lighten(boot, 0.12)), 'stroke-width="1.1"'));
      out.push(line(`M${n(w * 0.7)} ${n(L - 5)} q${n(w * 0.35)} 0.6 ${n(w * 0.5)} 2.4`, "!#d8c8a8", 0.6, 'opacity=".35"'));
    }
    return out.join("") + shadow;
  }

  // Fish tails, snake coils and floating robes replace both legs.
  function lowerBody(p, g, s) {
    const lower = p.lower;
    const color = p.tailColor || p.fur || s.skin, dark = darken(color, 0.22), light = lighten(color, 0.2);
    const hy = g.hy, H = g.H;
    // A sea king's long tail sweeps far behind him to a great crescent fin, banded in gold.
    if (lower === "fishtail" && p.tailStyle === "royal") {
      const d = `M${n(-H * 1.05)} ${n(hy + 1)} L${n(H * 1.05)} ${n(hy + 1)} Q${n(H * 1.5)} ${n(hy * 0.4)} ${n(H * 0.5)} ${n(-5)} Q${n(-H * 1.5)} ${n(-1)} ${n(-H * 3.6)} ${n(-4)} Q${n(-H * 4.6)} ${n(-6)} ${n(-H * 5.1)} ${n(-12)} L${n(-H * 4.7)} ${n(-15)} Q${n(-H * 3.5)} ${n(-11)} ${n(-H * 2.2)} ${n(-12)} Q${n(-H * 1.3)} ${n(hy * 0.3)} ${n(-H * 1.05)} ${n(hy + 1)}Z`;
      const fin = `M${n(-H * 4.8)} ${n(-13)} Q${n(-H * 5.6)} ${n(-30)} ${n(-H * 6.6)} ${n(-34)} Q${n(-H * 6.0)} ${n(-20)} ${n(-H * 6.2)} ${n(-12)} Q${n(-H * 6.4)} ${n(-2)} ${n(-H * 7.0)} ${n(6)} Q${n(-H * 5.7)} ${n(2)} ${n(-H * 4.9)} ${n(-8)}Z`;
      const belly = flat(`M${n(H * 0.8)} ${n(hy + 4)} Q${n(H * 1.15)} ${n(hy * 0.4)} ${n(H * 0.35)} ${n(-5.5)} Q${n(-H * 1.4)} ${n(-2.5)} ${n(-H * 3.4)} ${n(-5.5)} Q${n(-H * 1.4)} ${n(-6)} ${n(H * 0.05)} ${n(-8.5)} Q${n(H * 0.65)} ${n(hy * 0.45)} ${n(H * 0.25)} ${n(hy + 3)}Z`, p.bellyColor || light, 'opacity=".75"');
      let scales = "";
      for (let row = 0; row < 4; row++) for (let i = 0; i < 3; i++) scales += line(`M${n(-H * 0.8 + i * H * 0.6 + (row % 2) * H * 0.3)} ${n(hy * (0.85 - row * 0.2))} q${n(H * 0.15)} 2.5 ${n(H * 0.3)} 0`, dark, 0.9);
      for (let i = 0; i < 5; i++) scales += line(`M${n(-H * (1.2 + i * 0.6))} ${n(-9 - (i % 2) * 1.5)} q${n(H * 0.2)} 2 ${n(H * 0.4)} 0`, dark, 0.9);
      const bands = p.tailBand ? line(`M${n(-H * 1.1)} ${n(hy * 0.32)} Q${n(0)} ${n(hy * 0.25)} ${n(H * 1.3)} ${n(hy * 0.35)} M${n(-H * 3.3)} ${n(-12)} L${n(-H * 3.5)} ${n(-3)}`, p.tailBand, 1.8) : "";
      const finShade = K.clipped(fin, `<rect x="${n(-H * 7.2)}" y="${n(-36)}" width="${n(H * 2.6)}" height="16" fill="${INK}" opacity=".35"/>`) + line(`M${n(-H * 4.9)} ${n(-12)} L${n(-H * 6.4)} ${n(-30)} M${n(-H * 4.9)} ${n(-10)} L${n(-H * 6.2)} ${n(-12)} M${n(-H * 4.9)} ${n(-9)} L${n(-H * 6.7)} ${n(3)}`, dark, 0.8);
      return `<g class="sp-tail">${path(fin, light)}${finShade}${path(d, color)}${belly}${scales}${bands}${K.clipped(d, `<rect x="${n(-H * 6)}" y="${n(-16)}" width="${n(H * 7.5)}" height="10" fill="${INK}" opacity=".4"/>` + K.inkShadow(-H * 1.4, hy, H * 2.8, -hy, 0.42, 0.55))}</g>`;
    }
    if (lower === "fishtail") {
      const d = `M${n(-H)} ${n(hy + 1)} L${n(H)} ${n(hy + 1)} Q${n(H * 1.3)} ${n(hy * 0.45)} ${n(H * 0.4)} ${n(-6)} Q${n(-H * 0.2)} ${n(-3)} ${n(-H * 0.9)} ${n(-4)} Q${n(-H * 1.4)} ${n(hy * 0.4)} ${n(-H)} ${n(hy + 1)}Z`;
      const fin = `M${n(-H * 0.6)} ${n(-5)} Q${n(-H * 2.2)} ${n(-12)} ${n(-H * 2.7)} ${n(-3)} Q${n(-H * 2.0)} ${n(-1)} ${n(-H * 2.5)} ${n(4)} Q${n(-H * 1.4)} ${n(3)} ${n(-H * 0.4)} ${n(-1)}Z`;
      let scales = "";
      for (let row = 0; row < 4; row++) for (let i = 0; i < 3; i++) scales += line(`M${n(-H * 0.8 + i * H * 0.6 + (row % 2) * H * 0.3)} ${n(hy * (0.85 - row * 0.2))} q${n(H * 0.15)} 2.5 ${n(H * 0.3)} 0`, dark, 0.9);
      return `<g class="sp-tail">${path(fin, light)}${line(`M${n(-H * 1.0)} ${n(-4)} L${n(-H * 2.3)} ${n(-6)} M${n(-H * 1.0)} ${n(-2)} L${n(-H * 2.2)} ${n(2)}`, dark, 0.9)}</g>` + path(d, color) + scales + K.clipped(d, K.inkShadow(-H * 2, hy, H * 3.4, -hy + 2, 0.5, 0.6));
    }
    if (lower === "snaketail") {
      const d = `M${n(-H)} ${n(hy + 1)} L${n(H)} ${n(hy + 1)} Q${n(H * 1.4)} ${n(hy * 0.4)} ${n(H * 0.6)} ${n(-3)} Q${n(-H * 1.5)} ${n(1)} ${n(-H * 3.2)} ${n(-3)} Q${n(-H * 4.4)} ${n(-6)} ${n(-H * 4.6)} ${n(-12)} Q${n(-H * 4.0)} ${n(-4)} ${n(-H * 3.1)} ${n(-9)} Q${n(-H * 1.8)} ${n(-12)} ${n(-H * 1.1)} ${n(-14)} Q${n(-H * 1.3)} ${n(hy * 0.55)} ${n(-H)} ${n(hy + 1)}Z`;
      const belly = flat(`M${n(H * 0.75)} ${n(hy + 4)} Q${n(H * 1.1)} ${n(hy * 0.4)} ${n(H * 0.4)} ${n(-4)} Q${n(-H * 1.2)} ${n(-1)} ${n(-H * 2.8)} ${n(-4)} Q${n(-H * 1.2)} ${n(-4)} ${n(H * 0.1)} ${n(-7)} Q${n(H * 0.7)} ${n(hy * 0.45)} ${n(H * 0.2)} ${n(hy + 3)}Z`, p.bellyColor || light, 'opacity=".9"');
      let scales = "";
      for (let i = 0; i < 6; i++) scales += line(`M${n(-H * (0.6 + i * 0.55))} ${n(-8 - (i % 2) * 2)} q${n(H * 0.2)} 2 ${n(H * 0.4)} 0`, dark, 0.9);
      return `<g class="sp-tail">${path(d, color)}${belly}${scales}${K.clipped(d, `<rect x="${n(-H * 5)}" y="${n(-16)}" width="${n(H * 6.5)}" height="9" fill="${INK}" opacity=".5"/>` + K.inkShadow(-H * 1.4, hy, H * 2.8, -hy, 0.42, 0.55))}</g>`;
    }
    if (lower === "trunk") {
      // A tree-mother's body below the waist: one bark trunk that flares into roots across the
      // ground, with moss at the collar and between the roots.
      const bark = p.barkColor || "#5a4632", barkDark = darken(bark, 0.28), moss = p.moss || "#6f9a4a";
      const d = `M${n(-H * 1.05)} ${n(hy + 1)} L${n(H * 1.05)} ${n(hy + 1)} C${n(H * 1.05)} ${n(hy * 0.5)} ${n(H * 1.35)} ${n(hy * 0.18)} ${n(H * 2.1)} -3 Q${n(H * 2.9)} -1 ${n(H * 3.5)} 0 L${n(-H * 3.3)} 0 Q${n(-H * 2.7)} -1.5 ${n(-H * 2.0)} -3 C${n(-H * 1.3)} ${n(hy * 0.18)} ${n(-H * 1.05)} ${n(hy * 0.5)} ${n(-H * 1.05)} ${n(hy + 1)}Z`;
      // Great roots arch out over the ground on both sides, the far ones in shadow.
      const roots = [[-1, 1.1, 4.6, 1.3, true], [1, 1.1, 4.8, 1.3, true], [-1, 0.5, 3.2, 1, false], [1, 0.6, 3.4, 1, false], [1, 1.4, 2.6, 0.7, false]].map(([side, x0, x1, k, far]) => {
        const sx = side * H * x0, ex = side * H * x1, mid = (sx + ex) / 2;
        return path(`M${n(sx - side * 3 * k)} ${n(-9 * k)} C${n(mid)} ${n(-11 * k)} ${n(ex - side * H * 0.5)} ${n(-4 * k)} ${n(ex)} 0 L${n(ex - side * H * 0.9)} 0 Q${n(mid)} ${n(-3 * k)} ${n(sx + side * 2)} 0Z`, far ? barkDark : bark, 'stroke-width="1.1"');
      }).join("");
      const grooves = line([-0.55, -0.15, 0.3, 0.7].map((x, i) => `M${n(H * x)} ${n(hy + 4 + i * 2)} q${n(H * 0.12)} ${n(-hy * 0.35)} ${n(-H * 0.05)} ${n(-hy * 0.8)}`).join(" "), INK, 0.8, 'opacity=".6"');
      const mossD = `M${n(-H * 1.1)} ${n(hy + 1)} Q${n(-H * 0.4)} ${n(hy + 6)} ${n(H * 0.2)} ${n(hy + 3)} Q${n(H * 0.7)} ${n(hy + 7)} ${n(H * 1.1)} ${n(hy + 1)}Z`;
      return `<g class="sp-trunk">${roots}${path(d, bark)}${K.materialDecor("bark", -H * 1.05, hy, H * 2.1, -hy - 4, "trunk")}${grooves}${path(mossD, moss, 'stroke-width=".8"')}${dot(-H * 1.6, -2.2, 2.2, moss)}${dot(H * 2.2, -1.6, 1.8, moss)}${K.clipped(d, K.inkShadow(-H * 3.3, hy, H * 6.8, -hy + 2, 0.45, 0.6))}</g>`;
    }
    if (lower === "float") {
      const robe = p.outfit?.color || color;
      const d = `M${n(-H * 1.05)} ${n(hy + 1)} L${n(H * 1.05)} ${n(hy + 1)} Q${n(H * 1.5)} ${n(hy * 0.45)} ${n(H * 0.6)} ${n(-14)} Q${n(H * 0.1)} ${n(-6)} ${n(-H * 0.4)} ${n(-12)} Q${n(-H * 0.9)} ${n(-2)} ${n(-H * 1.6)} ${n(-8)} Q${n(-H * 1.4)} ${n(hy * 0.45)} ${n(-H * 1.05)} ${n(hy + 1)}Z`;
      return `<g class="sp-tail sp-wisp">${path(d, robe, 'fill-opacity=".94"')}${line(`M${n(-H * 0.2)} ${n(hy + 5)} Q${n(-H * 0.4)} ${n(hy * 0.4)} ${n(-H * 0.8)} ${n(-10)} M${n(H * 0.5)} ${n(hy + 8)} Q${n(H * 0.6)} ${n(hy * 0.4)} ${n(H * 0.3)} ${n(-16)}`, INK, 0.8, 'opacity=".55"')}${K.clipped(d, K.inkShadow(-H * 1.8, hy, H * 3.4, -hy, 0.45, 0.6))}</g>`;
    }
    return "";
  }

  // Arms hang straight down from a pivot at the shoulder.
  function armShape(p, g, s, side, sleeve) {
    const A = g.arm, w = g.armW;
    const back = side === "back";
    const tint = c => back ? darken(c, 0.22) : c;
    const out = [];
    const shadeD = taper(0, -3, 0, A - w * 0.3, w * 1.75, w * 1.25);
    const shadow = K.clipped(shadeD, K.inkShadow(-w * 1.1, -3, w * 2.2, A + 2, 0.48, back ? 0.55 : shadowOf(p.material)));
    if (p.arms === "wings") {
      const feather = p.wingColor || p.fur || "#6a5a4a", dark = darken(feather, 0.3);
      const fan = [];
      for (let i = 0; i < 6; i++) fan.push(path(`M${n(-w * 0.3)} ${n(A * (0.12 + i * 0.1))} Q${n(-w * (2.4 - i * 0.15))} ${n(A * (0.55 + i * 0.1))} ${n(-w * (1.4 - i * 0.1))} ${n(A * (1.1 + i * 0.07))} Q${n(-w * 0.2)} ${n(A * (0.72 + i * 0.09))} ${n(w * 0.2)} ${n(A * (0.22 + i * 0.1))}Z`, tint(i % 2 ? feather : dark), 'stroke-width="1"'));
      out.push(`<g class="sp-wingfeathers">${fan.join("")}</g>`, path(taper(0, 0, 0, A - 2, w, w * 0.7), tint(feather)), shadow);
      out.push(path(`M${n(-w * 0.45)} ${n(A - 3)} L${n(w * 0.55)} ${n(A - 3)} L${n(w * 0.8)} ${n(A + 3)} L${n(-w * 0.4)} ${n(A + 3)}Z`, tint("#8a7240")));
      return out.join("");
    }
    if (p.arms === "branches") {
      out.push(path(taper(0, 0, 0, A + 2, w * 1.2, w * 0.7), tint(s.skin)), K.materialDecor("bark", -w / 2, 0, w, A, side), shadow);
      out.push(line(`M0 ${n(A)} l-3.4 6 M0 ${n(A)} l1 7 M0 ${n(A)} l4.6 4.6`, tint(darken(s.skin, 0.25)), 2.2));
      if (p.leafColor) out.push(at(w * 0.3, A * 0.45, path(`M0 0 Q4.6 -3.6 8.2 0 Q4.6 3.6 0 0Z`, p.leafColor, 'stroke-width=".7"')));
      return out.join("");
    }
    out.push(path(taper(0, 0, 0, A - w * 0.5, w * 1.08, w * 0.74), tint(s.skin)));
    if (!sleeve) out.push(K.materialDecor(p.material, -w / 2, 0, w, A, side + "arm"));
    if (!sleeve && p.voidVeins) out.push(veinGlow(p.voidVeins, `M${n(w * 0.15)} ${n(A * 0.08)} l${n(-w * 0.2)} ${n(A * 0.18)} l${n(w * 0.25)} ${n(A * 0.14)} l${n(-w * 0.1)} ${n(A * 0.2)} M${n(-w * 0.05)} ${n(A * 0.4)} l${n(-w * 0.28)} ${n(A * 0.08)}`, back));
    if (!sleeve && p.plates) {
      // Plates grown into the flesh: a spiked pauldron and a forearm guard.
      const pc = tint(p.plates.color || "#2a2433"), pt = p.plates.trim || "#5b4a6e";
      out.push(path(`M${n(-w * 0.95)} ${n(w * 0.55)} Q${n(-w * 0.9)} ${n(-w * 0.75)} 0 ${n(-w * 0.85)} Q${n(w * 0.95)} ${n(-w * 0.7)} ${n(w * 0.95)} ${n(w * 0.6)} Q0 ${n(w * 0.15)} ${n(-w * 0.95)} ${n(w * 0.55)}Z`, pc, 'stroke-width="1.3"'),
        path(`M${n(-w * 0.15)} ${n(-w * 0.8)} L${n(-w * 0.45)} ${n(-w * 1.75)} L${n(w * 0.25)} ${n(-w * 0.82)}Z`, pc, 'stroke-width="1"'),
        line(`M${n(-w * 0.75)} ${n(w * 0.2)} Q0 ${n(-w * 0.2)} ${n(w * 0.8)} ${n(w * 0.25)}`, pt, 0.9),
        path(`M${n(-w * 0.62)} ${n(A * 0.55)} L${n(w * 0.6)} ${n(A * 0.53)} L${n(w * 0.52)} ${n(A * 0.8)} L${n(-w * 0.52)} ${n(A * 0.82)}Z`, pc, 'stroke-width="1.2"'),
        line(`M${n(-w * 0.55)} ${n(A * 0.62)} L${n(w * 0.55)} ${n(A * 0.61)} M${n(-w * 0.5)} ${n(A * 0.72)} L${n(w * 0.5)} ${n(A * 0.71)}`, pt, 0.8));
    }
    if (sleeve) out.push(sleeve(back));
    if (!sleeve) out.push(line(`M${n(w * 0.1)} ${n(A * 0.46)} q${n(w * 0.25)} 1.6 ${n(w * 0.45)} 0`, INK, 0.6, 'opacity=".5"'));
    out.push(shadow);
    // A heavy, closed hand; clawed races grow talons.
    const hy = A - w * 0.35;
    const hand = p.hands === "claws"
      ? path(`M${n(-w * 0.58)} ${n(hy - w * 0.35)} L${n(w * 0.58)} ${n(hy - w * 0.35)} L${n(w * 0.85)} ${n(hy + w * 1.0)} L${n(w * 0.15)} ${n(hy + w * 0.6)} L${n(-w * 0.35)} ${n(hy + w * 1.05)}Z`, tint(s.skin), 'stroke-width="1.4"')
      : path(`M${n(-w * 0.5)} ${n(hy - w * 0.3)} Q${n(-w * 0.62)} ${n(hy + w * 0.62)} ${n(0)} ${n(hy + w * 0.7)} Q${n(w * 0.62)} ${n(hy + w * 0.68)} ${n(w * 0.55)} ${n(hy - w * 0.32)}Z`, tint(s.skin), 'stroke-width="1.3"') + flat(`M${n(-w * 0.5)} ${n(hy - w * 0.3)} Q${n(-w * 0.62)} ${n(hy + w * 0.62)} 0 ${n(hy + w * 0.7)} L${n(-w * 0.05)} ${n(hy - w * 0.3)}Z`, INK, `opacity="${back ? 0.5 : 0.45}"`) + line(`M${n(w * 0.5)} ${n(hy - w * 0.05)} q${n(-w * 0.35)} ${n(w * 0.12)} ${n(-w * 0.4)} ${n(w * 0.42)}`, INK, 0.6, 'opacity=".6"');
    return { arm: out.join(""), hand };
  }

  function tailShape(t, g) {
    if (!t || !t.style) return "";
    const c = t.color || "#6f8f4f", dark = darken(c, 0.25);
    const x = -g.H + 1, y = g.hy + 2;
    const grow = 1.3;
    if (t.style === "lizard") return `<g transform="translate(${n(x)} ${n(y)}) scale(${grow})"><g class="sp-tail">${path(`M2 -3 Q-14 2 -22 16 Q-26 24 -34 24 Q-24 18 -20 8 Q-12 -6 0 -9Z`, c)}${line("M-6 2 L-16 14", dark, 1)}</g></g>`;
    if (t.style === "devil" || t.style === "imp") return `<g transform="translate(${n(x)} ${n(y)}) scale(${grow})"><g class="sp-tail">${line("M0 0 Q-14 6 -18 -6 Q-21 -16 -14 -20", c, 2.4)}${path("M-14 -20 l-5 -6 l8 1Z", c, 'stroke-width="1"')}</g></g>`;
    if (t.style === "tuft") return `<g transform="translate(${n(x)} ${n(y)}) scale(${grow})"><g class="sp-tail">${path("M0 0 Q-7 -2 -9 -8 Q-5 -6 -3 -10 Q-1 -5 2 -4Z", c, 'stroke-width="1"')}</g></g>`;
    if (t.style === "wolf") return `<g transform="translate(${n(x)} ${n(y)}) scale(${grow})"><g class="sp-tail">${path("M1 -2 Q-12 -4 -18 8 Q-12 4 -4 6Z", c)}</g></g>`;
    return "";
  }

  function wingShape(wg, g) {
    if (!wg || !wg.style) return "";
    const c = wg.color || "#6b2a2a", dark = darken(c, 0.25), light = lighten(c, 0.2);
    const sx = -3, sy = g.sy + 6;
    const span = (wg.span || 1) * 1.3;
    if (wg.style === "bat") {
      const d = `M0 0 L${n(-10 * span)} ${n(-22 * span)} L${n(-30 * span)} ${n(-28 * span)} Q${n(-26 * span)} ${n(-16 * span)} ${n(-34 * span)} ${n(-6 * span)} Q${n(-24 * span)} ${n(-4 * span)} ${n(-26 * span)} ${n(6 * span)} Q${n(-16 * span)} ${n(2 * span)} ${n(-14 * span)} ${n(12 * span)} Q${n(-8 * span)} ${n(4 * span)} 0 ${n(6 * span)}Z`;
      const veins = wg.veins ? `<g class="sp-glow" style="color:${wg.veins}">${line(`M${n(-6 * span)} ${n(-10 * span)} L${n(-22 * span)} ${n(-20 * span)} L${n(-28 * span)} ${n(-10 * span)} M${n(-12 * span)} ${n(-14 * span)} L${n(-20 * span)} ${n(-4 * span)} L${n(-22 * span)} ${n(4 * span)} M${n(-6 * span)} ${n(-4 * span)} L${n(-12 * span)} ${n(4 * span)}`, "!" + wg.veins, 0.9, 'opacity=".9"')}</g>` : "";
      return `<g transform="translate(${sx} ${n(sy)})"><g class="sp-wings">${path(d, c)}${line(`M0 0 L${n(-30 * span)} ${n(-28 * span)} M${n(-10 * span)} ${n(-22 * span)} L${n(-26 * span)} ${n(6 * span)} M${n(-10 * span)} ${n(-22 * span)} L${n(-14 * span)} ${n(12 * span)}`, dark, 1)}${veins}</g></g>`;
    }
    if (wg.style === "feather" || wg.style === "angel") {
      const feathers = [];
      for (let i = 0; i < 6; i++) feathers.push(path(`M0 0 Q${n((-12 - i * 4) * span)} ${n((-24 + i * 6) * span)} ${n((-20 - i * 4) * span)} ${n((-22 + i * 9) * span)} Q${n((-10 - i * 2) * span)} ${n((-6 + i * 4) * span)} 0 ${n(4 * span)}Z`, i % 2 ? c : light, 'stroke-width="1"'));
      return `<g transform="translate(${sx} ${n(sy)})"><g class="sp-wings">${feathers.reverse().join("")}</g></g>`;
    }
    if (wg.style === "decaying") {
      // Shrunken bat wings rotting away: a torn membrane with holes and bare finger bones.
      const k = span;
      const membrane = `M0 0 L${n(-8 * k)} ${n(-17 * k)} L${n(-19 * k)} ${n(-22 * k)} L${n(-20 * k)} ${n(-15 * k)} L${n(-25 * k)} ${n(-12 * k)} L${n(-22 * k)} ${n(-7 * k)} L${n(-26 * k)} ${n(-1 * k)} L${n(-19 * k)} ${n(-1 * k)} L${n(-18 * k)} ${n(5 * k)} L${n(-13 * k)} ${n(2 * k)} L${n(-10 * k)} ${n(8 * k)} L${n(-7 * k)} ${n(3 * k)} L0 ${n(5 * k)}Z`;
      const hole = (x, y, r) => ` M${n((x - r) * k)} ${n(y * k)} a${n(r * k)} ${n(r * 0.8 * k)} 0 1 0 ${n(2 * r * k)} 0 a${n(r * k)} ${n(r * 0.8 * k)} 0 1 0 ${n(-2 * r * k)} 0Z`;
      const holes = hole(-14, -12, 2.2) + hole(-9, -4, 1.6) + hole(-18, -5, 1.3) + hole(-5, -10, 1.1);
      const bones = line(`M0 0 L${n(-8 * k)} ${n(-17 * k)} L${n(-21 * k)} ${n(-24 * k)} M${n(-8 * k)} ${n(-17 * k)} L${n(-27 * k)} ${n(-13 * k)} M${n(-8 * k)} ${n(-17 * k)} L${n(-28 * k)} ${n(-1 * k)} M${n(-8 * k)} ${n(-17 * k)} L${n(-20 * k)} ${n(6 * k)}`, "#cfc3b0", 1.1);
      const rot = line(`M${n(-12 * k)} ${n(-16 * k)} l${n(-2 * k)} ${n(2 * k)} M${n(-16 * k)} ${n(-3 * k)} l${n(2 * k)} ${n(2 * k)}`, darken(c, 0.45), 0.9, 'opacity=".8"');
      return `<g transform="translate(${sx} ${n(sy)}) rotate(14)"><g class="sp-wings">${path(membrane + holes, c, 'fill-rule="evenodd"')}${rot}${bones}</g></g>`;
    }
    if (wg.style === "insect") return `<g transform="translate(${sx} ${n(sy)})"><g class="sp-wings">${ellipse(-12 * span, -14 * span, 8 * span, 16 * span, c, 'fill-opacity=".55" transform="rotate(-30)"')}${ellipse(-12 * span, 2 * span, 6 * span, 12 * span, c, 'fill-opacity=".5" transform="rotate(-60)"')}</g></g>`;
    return "";
  }

  /* ---------------- Assembly ---------------- */
  // Returns sprite markup plus its bounding box and useful anchors.
  K.humanoid = function humanoid(p) {
    const female = p.sex === "female";
    const b = {};
    for (const [key, value] of Object.entries(RIG)) b[key] = value * ((p.build?.[key] ?? CATALOGUE[key]) / CATALOGUE[key]);
    if (female) { b.shoulders *= 0.86; b.hips *= 1.04; b.armW *= 0.86; b.legW *= 0.9; b.head *= 0.96; }
    b.hunch = (p.build?.hunch || 0) + 3;
    b.headForward = (p.build?.headForward || 0) * 0.6 + 1.2;
    const lowerType = p.lower || "legs";
    const lift = lowerType === "float" ? 22 : lowerType === "fishtail" ? 6 : lowerType === "snaketail" ? 3 : 0;
    const hy = -b.legs - lift;
    const sy = hy - b.torso;
    const R = b.head;
    const hx = 1 + b.headForward;
    const neckLength = R * 0.6;
    const headY = sy - neckLength - R * 0.95;
    const g = { hy, sy, S: b.shoulders, H: b.hips, T: b.torso, legs: b.legs, arm: b.arm, armW: b.armW, legW: b.legW, R, hx, headY };
    const s = skinOf(p);
    const ink = shadowOf(p.material);
    const previousInk = K.beginInk();
    const eq = p.equipment || {};
    const E = K.equipment;
    // A figure whose outfit is its armor (the Deep Sea King's shells) does not draw worn armor.
    const armor = eq.armor && E && !p.outfitIsArmor ? E.armorLayers(eq.armor, g, p, s) : null;
    const primary = eq.primary || null, offhand = eq.offhand || null;
    const twoHanded = primary && primary.handed === 2 && !["beastclaws", "horns"].includes(primary.kind);
    const isBow = primary && ["bow", "longbow", "greatbow", "crossbow"].includes(primary.kind);
    const claws = primary?.kind === "beastclaws" || primary?.unique === "stardragon-claws";

    // Idle angles: arms hang close; weapons are held ready across the body.
    const frontIdle = isBow ? -20 : primary?.kind === "staff" ? -10 : twoHanded ? -28 : primary ? -24 : -8;
    const backIdle = twoHanded && !isBow ? -44 : offhand?.slot === "shield" ? -20 : offhand ? -8 : 6;
    const frontShoulder = { x: b.shoulders * 0.35, y: sy + 4 }, backShoulder = { x: -b.shoulders * 0.45, y: sy + 3.5 };
    const frontHip = { x: b.hips * 0.38, y: hy + 2 }, backHip = { x: -b.hips * 0.38, y: hy + 2 };

    // Sleeves follow clothing or armor.
    const sleeveColor = p.outfit?.sleeves === false ? null : p.outfit?.top === "vest" ? p.outfit.shirt || "#a8987a" : (p.outfit?.top && !["none", "bare", "loincloth", "fur", "scales", "feathers", "smith-apron"].includes(p.outfit.top) ? p.outfit.color || "#5a4a3a" : null);
    const sleeveFor = back => {
      if (armor?.sleeve) return armor.sleeve(back);
      if (!sleeveColor) return "";
      const c = back ? darken(sleeveColor, 0.22) : sleeveColor;
      return path(`M${n(-b.armW * 0.62)} -2 L${n(b.armW * 0.62)} -2 L${n(b.armW * 0.55)} ${n(b.arm * 0.62)} Q0 ${n(b.arm * 0.66)} ${n(-b.armW * 0.6)} ${n(b.arm * 0.62)}Z`, c) + line(`M${n(-b.armW * 0.1)} ${n(b.arm * 0.2)} q1 ${n(b.arm * 0.15)} 0 ${n(b.arm * 0.3)}`, INK, 0.6, 'opacity=".5"');
    };

    // Weapons are carried large, as in the hand of a grown fighter.
    const weaponMarkup = (visual, hand) => {
      if (!visual || !E || ["beastclaws", "horns"].includes(visual.kind)) return "";
      const grip = E.gripAngle(visual, hand, frontIdle, backIdle);
      const molten = p.moltenWeapon && hand === "front" ? ` style="filter: drop-shadow(0 0 2px ${p.moltenWeapon}) drop-shadow(0 0 5px ${p.moltenWeapon})"` : "";
      return `<g transform="translate(0 ${n(b.arm - b.armW * 0.3)}) rotate(${n(grip)}) scale(1.3)"><g class="sp-weapon${hand === "back" ? " sp-weapon-off" : ""}${visual.unique ? " sp-masterwork mw-" + visual.unique : ""}" data-item="${visual.id}"${molten}>${E.weapon(visual)}</g></g>`;
    };
    const clawMarkup = visual => visual && E ? E.claws(visual, b) : "";

    const sleeved = armor?.sleeve || sleeveColor ? sleeveFor : null;
    const frontArm = armShape(p, g, s, "front", sleeved);
    const backArm = armShape(p, g, s, "back", sleeved);
    const shield = offhand?.slot === "shield" && E ? `<g transform="translate(${n(backShoulder.x)} ${n(backShoulder.y)}) rotate(${n(backIdle)})"><g class="sp-arm-back sp-shield-arm"><g transform="translate(${n(5)} ${n(b.arm * 0.56)}) rotate(${n(-backIdle)}) scale(1.45)"><g class="sp-shield${offhand.unique ? " sp-masterwork mw-" + offhand.unique : ""}" data-item="${offhand.id}">${E.shield(offhand, b)}</g></g></g></g>` : "";

    const armGroup = (side, shoulder, idle, arm, held) => {
      const parts = typeof arm === "string" ? { arm, hand: "" } : arm;
      return `<g transform="translate(${n(shoulder.x)} ${n(shoulder.y)}) rotate(${n(idle)})"><g class="sp-arm-${side}" data-hand-y="${n(b.arm)}">${parts.arm}${held.under || ""}${parts.hand}${held.over || ""}${armor?.hand ? armor.hand(side === "back") : ""}</g></g>`;
    };
    const frontHeld = claws ? { over: clawMarkup(primary) } : { under: weaponMarkup(primary, "front") };
    const backHeld = offhand && offhand.slot !== "shield" ? { under: weaponMarkup(offhand, "back") } : claws ? { over: clawMarkup(primary) } : {};

    // Legs or a replacement lower body.
    let legs = "";
    if (["legs", "goat", "bird", "roots"].includes(lowerType)) {
      const stance = lowerType === "legs" ? { back: 9, front: -7 } : { back: 4, front: -3 };
      const legGroup = (side, hip) => `<g transform="translate(${n(hip.x)} ${n(hip.y)}) rotate(${stance[side]})"><g class="sp-leg-${side}">${legShape(p, g, s, side)}${armor?.leg ? armor.leg(side === "back") : ""}</g></g>`;
      legs = legGroup("back", backHip) + legGroup("front", frontHip);
    } else legs = lowerBody(p, g, s);

    // Head pieces are drawn with a finer pen.
    const head = K.withPen(1.25, () => {
      const hairParts = hair(p.hair, hx, headY, R);
      const earParts = /^beast/.test(p.ears || "") ? ears(p.ears, hx, headY, R, p.hair?.color || s.skin, darken(p.hair?.color || s.skin, 0.3)) : ears(p.ears || "round", hx, headY, R, s.skin, s.shade);
      const hornParts = horns(p.horns || (primary?.kind === "horns" ? { style: "ram", color: "#3d2b27", molten: true } : null), hx, headY, R);
      const hatParts = p.helm ? warHelm(p.helm, hx, headY, R) : armor?.head ? armor.head : headwear(p.headwear, hx, headY, R);
      const hideHair = armor?.hidesHair || p.helm;
      const headD = headPath(hx, headY, R, p.head);
      const headShape = path(headD, s.skin, 'stroke-width="1.5"');
      const shading = headShade(headD, hx, headY, R, ink * 0.9, p.head);
      return `<g class="sp-head">${hornParts.back}${hideHair ? "" : hairParts.back}${hatParts.back || ""}${earParts.back}${headShape}${K.materialDecor(p.material, hx - R * 0.6, headY - R * 0.6, R * 1.2, R * 1.2, "head")}${shading}${eyes(p, hx, headY, R, s)}${p.warpaint ? warPaint(p.warpaint, hx, headY, R) : ""}${p.lightningScars ? lightningScars(p.lightningScars, hx, headY, R) : ""}${p.forgeCracks ? forgeCracks(p.forgeCracks, hx, headY, R) : ""}${p.veins ? earthVeins(p.veins, p.veinsDim, hx, headY, R) : ""}${p.brand ? infernalBrand(p.brand, hx, headY, R) : ""}${p.embers ? embers(p.embers, p.emberLevel || 1, hx, headY, R) : ""}${p.static ? staticSparks(p.static, p.staticLevel || 1, hx, headY, R) : ""}${mouthAndNose(p, hx, headY, R, s)}${earParts.front}${beard(p.beard, hx, headY, R)}${hideHair ? "" : hairParts.front}${earParts.top || ""}${crest(p.crest, hx, headY, R)}${hornParts.front}${hatParts.front || ""}${p.tabard?.sigil ? oolSigil(hx + R * 0.3, headY - R * 0.68, R * 0.17, p.tabard.sigil) : ""}${p.thirdEye ? thirdEye(p.thirdEye, hx, headY, R) : ""}${p.lawGlyph ? lawGlyph(p.lawGlyph, p.glyphs || "#c9b0ff", hx, headY, R) : ""}${p.accessory === "earring" ? dot(hx - R * 0.28, headY + R * 0.42, R * 0.07, "#a8925a", 'stroke="#130e0b" stroke-width=".5"') : ""}${p.accessory === "eyepatch" ? line(`M${n(hx + R * 0.1)} ${n(headY - R * 0.6)} L${n(hx + R * 0.95)} ${n(headY + R * 0.25)}`, INK, 0.9) + ellipse(hx + R * 0.42, headY - R * 0.04, R * 0.2, R * 0.16, INK, 'stroke="none"') : ""}</g>`;
    });
    const neckD = `M${n(hx - R * 0.5)} ${n(sy + 1)} L${n(hx - R * 0.42)} ${n(headY + R * 0.6)} L${n(hx + R * 0.38)} ${n(headY + R * 0.75)} L${n(hx + R * 0.48)} ${n(sy + 1)}Z`;
    const neck = path(neckD, s.skin, 'stroke-width="1.4"') + K.clipped(neckD, `<rect x="${n(hx - R)}" y="${n(headY)}" width="${n(R * 2)}" height="${n(R * 1.35)}" fill="${INK}" opacity="${ink * 0.85}"/>` + `<rect x="${n(hx - R)}" y="${n(headY)}" width="${n(R * 0.62)}" height="${n(R * 3)}" fill="${INK}" opacity="${ink * 0.7}"/>`);

    const torso = (armor?.torso || outfitTorso(p, g, s)) + torsoShade(g, female, ink) + (armor ? "" : corruptTorso(p, g)) + (armor?.torsoGlow || "");
    const skirt = armor?.skirt ?? outfitSkirt(p, g);
    const capeColor = p.cape ? darken(p.cape, 0.15) : null;
    // A trimmed cape (the sea king's gold edge) and kelp strands hanging from it.
    const capeTrim = p.cape && p.capeTrim ? line(`M${n(-b.shoulders - 0.5)} ${n(sy + 4)} Q${n(-b.shoulders - 11)} ${n(hy * 0.45)} ${n(-b.shoulders - 7.5)} ${n(-1.5)} L${n(-b.shoulders * 0.4)} ${n(-4.5)}`, p.capeTrim, 1.4) : "";
    const kelp = p.cape && p.kelp ? [0, 1, 2, 3].map(i => line(`M${n(-b.shoulders * (0.6 + i * 0.25))} ${n(sy + 5 + i * 3)} q${n(-3 - i)} ${n(b.torso * 0.4)} ${n(-1 - i)} ${n(b.torso * 0.9 + i * 4)}`, i % 2 ? p.kelp : darken(p.kelp, 0.2), 1.6 - i * 0.15, 'opacity=".9"')).join("") : "";
    const cape = (armor?.cape || "") + (p.cape ? `<g class="sp-cape">${path(`M${n(-b.shoulders + 1)} ${n(sy + 2)} Q${n(-b.shoulders - 12)} ${n(hy * 0.45)} ${n(-b.shoulders - 8)} ${n(-1)} L${n(-b.shoulders * 0.4)} ${n(-4)} L${n(b.hips * 0.2)} ${n(-2)} L${n(b.shoulders * 0.2)} ${n(sy + 3)}Z`, capeColor)}${line(`M${n(-b.shoulders - 2)} ${n(sy + 10)} q-5 ${n(b.torso)} -4 ${n(-sy - 14)} M${n(-b.shoulders * 0.6)} ${n(sy + 8)} q-3 ${n(b.torso)} -1 ${n(-sy - 12)}`, INK, 0.8, 'opacity=".6"')}${capeTrim}${kelp}</g>` : "");
    const aura = s.glow ? ` style="--sp-glow:${s.glow}"` : "";
    const hunch = b.hunch ? ` transform="rotate(${n(b.hunch)} 0 ${n(hy)})"` : "";
    const standard = p.standard ? warStandard(p.standard, g, b, headY - R * 3.1) : "";
    const gear = (p.glyphs ? dreamGlyphs(p.glyphs, g) : "") + (p.tabard ? graveTabard(p.tabard, g) : "") + (p.sash ? warSash(p.sash, g) : "") + (p.trophies ? warTrophies(p.trophies, g) : "") + (p.drum ? warDrum(p.drum, g) : "") + (p.pauldrons ? pauldron(p.pauldrons, backShoulder.x - 2, backShoulder.y + 2.5, b.armW * 1.4, false) : "");
    const frontPauldron = p.pauldrons ? pauldron(p.pauldrons, frontShoulder.x + 2, frontShoulder.y + 2.5, b.armW * 1.5, true) : "";
    const upper = `<g${hunch}><g class="sp-upper">${standard}${cape}${armGroup("back", backShoulder, backIdle, backArm, backHeld)}${torso}${gear}${neck}${head}${shield}${armGroup("front", frontShoulder, frontIdle, frontArm, frontHeld)}${frontPauldron}</g></g>`;
    const body = `${wingShape(p.wings, g)}${tailShape(p.tail, g)}<g class="sp-legs">${legs}</g>${["legs", "goat", "bird", "roots"].includes(lowerType) ? skirt : ""}${upper}`;
    const defs = K.endInk(previousInk);
    const greatAntlers = p.horns?.style === "great-antlers";
    const tall = p.standard ? 3.6 : p.helm ? 2.3 : ["coral-crown", "storm-crown", "canopy-crown", "hellfire-crown"].includes(p.headwear?.style) ? 2.5 : p.headwear?.style === "bone-crown" ? 2.6 : p.lawGlyph ? 3.0 : p.headwear?.style === "dream-crown" ? 2.0 : greatAntlers ? 5.1 : p.headwear?.style === "wizard" || armor?.head && /wizard/.test(JSON.stringify(eq.armor?.name || "")) ? 2.5 : p.horns || ["spiky", "mohawk", "flame"].includes(p.hair?.style) ? 2.1 : 1.5;
    const top = headY - R * tall - 6;
    const left = Math.min(-b.shoulders - 18, p.standard ? -b.shoulders * 2.3 - 6 : 0, (lowerType === "snaketail" || lowerType === "trunk" ? -b.hips * 5 : lowerType === "fishtail" ? -b.hips * (p.tailStyle === "royal" ? 7.3 : 3) : -22) - (p.wings ? 40 * (p.wings.span || 1) : 0) - (p.tail ? 14 : 0), greatAntlers ? hx - R * 2.8 : 0);
    const right = Math.max(b.shoulders + 46, hx + R * (greatAntlers ? 2.9 : 2.2), lowerType === "trunk" ? b.hips * 5.1 : 0);
    const reach = frontIdle * Math.PI / 180;
    // The hunch turns the upper body about the hips; anchors on it turn too.
    const lean = b.hunch * Math.PI / 180;
    const bent = ([x, y]) => [x * Math.cos(lean) - (y - hy) * Math.sin(lean), hy + x * Math.sin(lean) + (y - hy) * Math.cos(lean)];
    const [headX, headCY] = bent([hx, headY]);
    return {
      markup: `<g class="sp-figure sp-humanoid sp-ink${s.material ? " sp-mat-" + s.material : ""}${lowerType === "float" ? " sp-floating" : ""}"${aura}>${defs}${body}</g>`,
      box: { x: left, y: top, w: right - left, h: -top + 4 },
      anchors: { head: bent([hx, headY - R * 0.2]), chest: bent([0, sy + b.torso * 0.35]), top: [headX, top + 6], feet: [0, 0], hand: bent([frontShoulder.x - b.arm * Math.sin(reach), frontShoulder.y + b.arm * Math.cos(reach)]) },
      portrait: { x: headX - R * 2.1, y: headCY - R * 2.0, w: R * 4.2, h: R * 4.2 },
    };
  };
  K.MATERIAL_SKINS = MATERIAL_SKINS;
  K.materialDecor = materialDecor;
  K.headwearFor = headwear;
  K.torsoPath = torsoPath;
})();
