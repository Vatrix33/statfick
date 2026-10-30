/* Boss intro cutscenes: a short scene and a title card when a Great Hunt boss's battle
   begins. An enemy file names its intro with "introCinematic"; combat.js plays it once
   when the encounter starts (never when a saved battle resumes). Each intro draws its own
   scene around the boss's real battle sprite, so the cutscene and the fight look alike.
   The timeline lives in styles.css under "Boss intro cutscenes". */
(() => {
  "use strict";
  const INTROS = {
    // Great Imp Ji'ruan: a void rift tears open in the night-black Alderwood, imp eyes wake in
    // the dark, his retinue pours out and the crowned imp king rises from the light.
    jiruan: {
      duration: 7000,
      location: "THE GREAT ALDERWOOD · GREAT HUNT",
      overture: "The void opened beneath the Alderwood, and the imps came crawling after it.",
      label: "LORD OF THE IMP COURT",
      title: ["Great Imp ", "Ji'ruan"],
      description: "Crown of the Void — while his court stands, no blade can reach him.",
      imps: 4,
      retinue: "Void Imp",
      scene() {
        // Trunks facing the rift catch a thin rim of its light.
        const trunk = (x, w, lean, fill, rim = false) => `<path fill="${fill}"${rim ? ` stroke="#6a44a0" stroke-width="2.5" stroke-opacity=".7"` : ""} d="M${x - w / 2} 690 C${x - w * 0.42 + lean * 0.3} 470 ${x - w * 0.3 + lean * 0.7} 220 ${x - w * 0.22 + lean} -10 L${x + w * 0.22 + lean} -10 C${x + w * 0.3 + lean * 0.7} 220 ${x + w * 0.42 + lean * 0.3} 470 ${x + w / 2} 690Z"/>`;
        const eyes = [[188, 402, 0], [298, 470, 0.35], [394, 356, 0.7], [834, 384, 0.2], [948, 452, 0.55], [1052, 362, 0.9], [702, 300, 1.1], [470, 286, 1.3]]
          .map(([x, y, delay]) => `<g class="bi-jr-eye" style="--d:${delay}s"><path d="M${x - 10} ${y} l7 -3 l2 4Z M${x + 10} ${y} l-7 -3 l-2 4Z"/></g>`).join("");
        const cracks = "M600 600 L548 632 L492 640 M600 600 L656 634 L722 646 M600 600 L582 652 L590 690 M600 600 L640 668 M548 632 L520 676 M656 634 L690 686";
        return `<svg class="bi-art" viewBox="0 0 1200 680" preserveAspectRatio="xMidYMid slice" xmlns="http://www.w3.org/2000/svg">
          <defs>
            <linearGradient id="bi-jr-sky" x2="0" y2="1"><stop stop-color="#06030b"/><stop offset=".55" stop-color="#140a20"/><stop offset="1" stop-color="#22142f"/></linearGradient>
            <radialGradient id="bi-jr-glow"><stop stop-color="#f1e6ff" stop-opacity=".9"/><stop offset=".22" stop-color="#b48cff" stop-opacity=".62"/><stop offset=".6" stop-color="#5a2a9c" stop-opacity=".22"/><stop offset="1" stop-color="#1a0d2b" stop-opacity="0"/></radialGradient>
            <linearGradient id="bi-jr-tear" x2="1"><stop stop-color="#4a2386"/><stop offset=".5" stop-color="#fbf6ff"/><stop offset="1" stop-color="#4a2386"/></linearGradient>
            <linearGradient id="bi-jr-mist" x2="0" y2="1"><stop stop-color="#8a64c2" stop-opacity="0"/><stop offset=".6" stop-color="#7a58b0" stop-opacity=".22"/><stop offset="1" stop-color="#2a1a3c" stop-opacity=".5"/></linearGradient>
            <filter id="bi-jr-soft" x="-50%" y="-50%" width="200%" height="200%"><feGaussianBlur stdDeviation="14"/></filter>
          </defs>
          <rect width="1200" height="680" fill="url(#bi-jr-sky)"/>
          <g class="bi-jr-far">${trunk(70, 60, -8, "#170e22")}${trunk(250, 46, 10, "#1a1026")}${trunk(420, 38, -6, "#1d122b", true)}${trunk(790, 40, 8, "#1d122b", true)}${trunk(960, 52, -10, "#1a1026")}${trunk(1130, 64, 6, "#170e22")}</g>
          <ellipse class="bi-jr-halo" cx="600" cy="390" rx="460" ry="340" fill="url(#bi-jr-glow)"/>
          <g class="bi-jr-rift">
            <path class="bi-jr-rift-bloom" fill="#b48cff" filter="url(#bi-jr-soft)" d="M600 120 L640 220 L618 300 L660 390 L626 470 L652 560 L600 612 L548 560 L574 470 L540 390 L582 300 L560 220Z"/>
            <path fill="url(#bi-jr-tear)" d="M600 136 L624 214 L608 266 L638 338 L614 404 L642 474 L618 542 L626 600 L600 592 L576 542 L594 474 L560 404 L588 338 L564 266 L590 214Z"/>
            <path fill="none" stroke="#fbf6ff" stroke-width="2" opacity=".75" d="M600 150 L602 590"/>
          </g>
          <path class="bi-jr-cracks" fill="none" stroke="#c58cff" stroke-width="4" stroke-linecap="round" d="${cracks}"/>
          <g class="bi-jr-mid">${trunk(-10, 110, 14, "#0c0713")}${trunk(160, 74, -12, "#0d0815", true)}${trunk(1040, 80, 12, "#0d0815", true)}${trunk(1215, 120, -14, "#0c0713")}</g>
          <path fill="#0a0610" d="M0 575 Q160 548 330 566 T640 572 T940 560 T1200 572 V680 H0Z"/>
          <g class="bi-jr-eyes" fill="#d6b4ff">${eyes}</g>
          <rect class="bi-jr-mist" y="430" width="1200" height="250" fill="url(#bi-jr-mist)"/>
          <g class="bi-jr-near" fill="#040208"><path d="M0 680 L0 520 Q60 560 90 610 Q130 590 170 640 L210 680Z"/><path d="M1200 680 L1200 500 Q1140 548 1110 600 Q1070 584 1030 636 L990 680Z"/><path d="M0 0 H260 Q190 40 140 30 Q80 70 0 60Z"/><path d="M1200 0 H930 Q1000 46 1060 34 Q1120 72 1200 66Z"/></g>
        </svg>`;
      },
    },
    // Juila the Grand Forest Matriarch: the Deepwood at dusk remembers every season at once.
    // Spring, summer, autumn and winter sweep across the canopy, roots split the earth, and the
    // tree-mother rises from them while two of her Matriarchs step out of the trees.
    juila: {
      duration: 7400,
      location: "THE DEEPWOOD · GREAT HUNT",
      overture: "The Deepwood remembers every season. Tonight it remembers them all at once.",
      label: "MOTHER OF THE DEEPWOOD",
      title: ["Juila ", "the Grand Forest Matriarch"],
      description: "Cycle of Seasons — spring regrows her court, summer burns, autumn withers, winter binds.",
      imps: 2,
      retinue: "Grove Matriarch",
      scene() {
        const trunk = (x, w, lean, fill, rim = false) => `<path fill="${fill}"${rim ? ` stroke="#9fe07a" stroke-width="2.5" stroke-opacity=".45"` : ""} d="M${x - w / 2} 700 C${x - w * 0.45 + lean * 0.3} 470 ${x - w * 0.32 + lean * 0.7} 220 ${x - w * 0.26 + lean} -10 L${x + w * 0.26 + lean} -10 C${x + w * 0.32 + lean * 0.7} 220 ${x + w * 0.45 + lean * 0.3} 470 ${x + w / 2} 700Z"/>`;
        // Four seasonal lights, swept across the canopy one after another.
        const seasons = [["#ffb3d1", 260, 150], ["#ffd36b", 520, 110], ["#ff8a3d", 760, 140], ["#bfe6ff", 980, 120]]
          .map(([color, x, y], index) => `<ellipse class="bi-ju-season" style="--i:${index}" cx="${x}" cy="${y}" rx="300" ry="190" fill="${color}" filter="url(#bi-ju-soft)"/>`).join("");
        const canopy = Array.from({ length: 22 }, (_, i) => { const x = i * 58 - 20, y = 40 + Math.sin(i * 1.7) * 26; return `<circle cx="${x}" cy="${y}" r="${62 + (i * 37) % 40}"/>`; }).join("");
        // Roots spreading from where she rises.
        const roots = [[-1, 0.18], [-0.62, 0.32], [-0.28, 0.42], [0.3, 0.4], [0.66, 0.3], [1, 0.16]]
          .map(([dx, dy], i) => `<path class="bi-ju-root" style="--d:${(i % 3) * 0.12}s" d="M600 610 Q${600 + dx * 160} ${610 + dy * 90} ${600 + dx * 420} ${610 + dy * 140 + (i % 2) * 18}"/>`).join("");
        const petals = Array.from({ length: 34 }, (_, i) => {
          const x = (i * 97) % 1200, color = ["#ffc4dc", "#ffe08a", "#ff9a4a", "#e8f4ff"][i % 4];
          return `<ellipse class="bi-ju-petal" style="--d:${((i * 0.37) % 5).toFixed(2)}s;--drift:${((i % 7) - 3) * 22}px" cx="${x}" cy="-20" rx="5" ry="3" fill="${color}"/>`;
        }).join("");
        return `<svg class="bi-art" viewBox="0 0 1200 680" preserveAspectRatio="xMidYMid slice" xmlns="http://www.w3.org/2000/svg">
          <defs>
            <linearGradient id="bi-ju-sky" x2="0" y2="1"><stop stop-color="#04110c"/><stop offset=".55" stop-color="#0b2318"/><stop offset="1" stop-color="#16301f"/></linearGradient>
            <radialGradient id="bi-ju-glow"><stop stop-color="#f4ffe0" stop-opacity=".85"/><stop offset=".25" stop-color="#b8f59a" stop-opacity=".55"/><stop offset=".65" stop-color="#3f8a3a" stop-opacity=".2"/><stop offset="1" stop-color="#0b2318" stop-opacity="0"/></radialGradient>
            <linearGradient id="bi-ju-mist" x2="0" y2="1"><stop stop-color="#9fe07a" stop-opacity="0"/><stop offset=".6" stop-color="#7fbf6a" stop-opacity=".2"/><stop offset="1" stop-color="#1f3a28" stop-opacity=".55"/></linearGradient>
            <filter id="bi-ju-soft" x="-50%" y="-50%" width="200%" height="200%"><feGaussianBlur stdDeviation="40"/></filter>
          </defs>
          <rect width="1200" height="680" fill="url(#bi-ju-sky)"/>
          <g class="bi-ju-seasons">${seasons}</g>
          <g fill="#0a1a12" opacity=".95">${canopy}</g>
          <g class="bi-jr-far">${trunk(60, 90, -10, "#0f2218")}${trunk(240, 70, 12, "#11261b")}${trunk(420, 56, -8, "#13291d", true)}${trunk(790, 60, 10, "#13291d", true)}${trunk(970, 76, -12, "#11261b")}${trunk(1150, 96, 8, "#0f2218")}</g>
          <ellipse class="bi-ju-halo" cx="600" cy="420" rx="430" ry="330" fill="url(#bi-ju-glow)"/>
          <g class="bi-ju-roots" fill="none" stroke="#6b4a2f" stroke-width="12" stroke-linecap="round">${roots}</g>
          <g class="bi-jr-mid">${trunk(-20, 150, 16, "#07130d")}${trunk(150, 96, -14, "#08150e", true)}${trunk(1050, 104, 14, "#08150e", true)}${trunk(1225, 160, -16, "#07130d")}</g>
          <path fill="#06100a" d="M0 585 Q170 560 340 576 T650 584 T950 570 T1200 582 V680 H0Z"/>
          <rect class="bi-jr-mist" y="440" width="1200" height="240" fill="url(#bi-ju-mist)"/>
          <g class="bi-ju-petals">${petals}</g>
          <g class="bi-jr-near" fill="#030805"><path d="M0 680 L0 500 Q50 560 70 620 Q110 580 150 650 L190 680Z"/><path d="M1200 680 L1200 490 Q1150 548 1120 610 Q1080 590 1040 650 L1000 680Z"/><path d="M0 0 H300 Q220 50 160 38 Q90 80 0 70Z"/><path d="M1200 0 H900 Q980 52 1050 40 Q1120 82 1200 76Z"/></g>
        </svg>`;
      },
    },
    // Tankina, Disciple of Talos: the storm altar of the Church of Talos at night. A light drizzle
    // begins, blood runs down the altar's channels, lightning flickers in the clouds, and Tankina
    // rises behind the altar between her two Apostles.
    tankina: {
      duration: 7400,
      location: "THE CHURCH OF TALOS · STORMWAKE HILLS",
      overture: "A light drizzle falls on the storm altar. Talos is listening.",
      label: "DISCIPLE OF TALOS",
      title: ["Tankina, ", "Disciple of Talos"],
      description: "The Gathering Storm — every lightning spell, yours included, darkens the sky. As the drizzle grows into a thunderstorm, she and her Apostles grow stronger.",
      imps: 2,
      retinue: "Apostle of Talos",
      scene() { return talosScene(false); },
    },
    // Her second phase: rain lashes the altar, a great bolt of Talos strikes it, and she rises as his
    // Chosen while the Thunder Boar Gorthok charges out of the storm.
    "tankina-phase": {
      duration: 7400,
      location: "THE CHURCH OF TALOS · PHASE II",
      overture: "Blood anoints the altar. Talos answers.",
      label: "PHASE II · CHOSEN BY THE STORM",
      title: ["Tankina, ", "Chosen of Talos"],
      description: "Her spells strike 30% harder, and the Thunder Boar Gorthok answers the storm.",
      before: true,
      imps: 1,
      retinue: "Thunder Boar Gorthok",
      scene() { return talosScene(true); },
    },
    // Archdevil Saffron: the Pyramid Sanctum. Hellfire braziers ignite one by one along the
    // columns, his three Servants step out of the shadows, and the Archdevil descends on burning
    // wings onto his dais.
    saffron: {
      duration: 7800,
      location: "THE PYRAMID SANCTUM · DEEP DESERT",
      overture: "The braziers of the sanctum ignite. The court assembles.",
      label: "ARCHDEVIL OF THE SANDS",
      title: ["Archdevil ", "Saffron"],
      description: "Infernal Sacrifice — when a Servant falls low, he drains it into the hellfire to heal and grow stronger, and a rift returns it later. Kill his Servants outright before he can feed.",
      imps: 3,
      retinue: ["Great Devil Servant - Protector", "Great Devil Servant - Reaver", "Great Devil Servant - Archer"],
      scene() {
        const brazier = (x, y, i) => `<g class="bi-sf-brazier" style="--d:${(0.6 + i * 0.45).toFixed(2)}s"><path d="M${x - 18} ${y} L${x + 18} ${y} L${x + 10} ${y + 16} L${x - 10} ${y + 16}Z" fill="#3a2a1a" stroke="#a8803a" stroke-width="2.5"/><path d="M${x - 4} ${y + 16} L${x - 6} ${y + 70} L${x + 6} ${y + 70} L${x + 4} ${y + 16}Z" fill="#2a1e14"/>
          <g class="bi-sf-flame"><path d="M${x - 14} ${y} Q${x - 16} ${y - 26} ${x - 2} ${y - 46} Q${x - 4} ${y - 24} ${x + 6} ${y - 36} Q${x + 18} ${y - 18} ${x + 14} ${y}Z" fill="#ff5a2a"/><path d="M${x - 7} ${y} Q${x - 8} ${y - 16} ${x} ${y - 28} Q${x + 9} ${y - 14} ${x + 7} ${y}Z" fill="#ffd27a"/></g>
          <ellipse class="bi-sf-glow" cx="${x}" cy="${y - 20}" rx="90" ry="80" fill="url(#bi-sf-fire)"/></g>`;
        const columns = [130, 330, 870, 1070].map(x => `<g fill="#3a2a1c" stroke="#5a4428" stroke-width="2"><rect x="${x - 34}" y="70" width="68" height="470"/><rect x="${x - 44}" y="60" width="88" height="22"/><rect x="${x - 44}" y="530" width="88" height="22"/></g>
          <g class="bi-sf-glyph" stroke="#ff8a3d" stroke-width="2.5" fill="none" opacity=".0"><path d="M${x - 10} 180 l20 0 l-10 18Z M${x} 240 v30 M${x - 12} 255 h24 M${x - 10} 320 a10 10 0 1 0 20 0 a10 10 0 1 0 -20 0 M${x} 380 l-12 22 h24Z M${x - 10} 450 h20 v20 h-20Z"/></g>`).join("");
        const embers = Array.from({ length: 30 }, (_, i) => `<circle class="bi-sf-ember" style="--d:${((i * 0.41) % 6).toFixed(2)}s;--drift:${((i % 7) - 3) * 18}px" cx="${(i * 149) % 1200}" cy="${380 + (i * 47) % 280}" r="${1.4 + (i % 3) * 0.8}" fill="${i % 3 ? "#ffb347" : "#ff5a2a"}"/>`).join("");
        return `<svg class="bi-art" viewBox="0 0 1200 680" preserveAspectRatio="xMidYMid slice" xmlns="http://www.w3.org/2000/svg">
          <defs>
            <linearGradient id="bi-sf-wall" x2="0" y2="1"><stop stop-color="#0d0806"/><stop offset=".6" stop-color="#2a1a10"/><stop offset="1" stop-color="#3e2614"/></linearGradient>
            <radialGradient id="bi-sf-fire"><stop stop-color="#ffd27a" stop-opacity=".55"/><stop offset=".35" stop-color="#ff5a2a" stop-opacity=".25"/><stop offset="1" stop-color="#2a1a10" stop-opacity="0"/></radialGradient>
            <radialGradient id="bi-sf-throne"><stop stop-color="#ffb347" stop-opacity=".7"/><stop offset=".4" stop-color="#c02a10" stop-opacity=".3"/><stop offset="1" stop-color="#0d0806" stop-opacity="0"/></radialGradient>
          </defs>
          <rect width="1200" height="680" fill="url(#bi-sf-wall)"/>
          <path fill="#140c08" d="M600 0 L1200 340 V0Z M600 0 L0 340 V0Z" opacity=".8"/>
          <path d="M600 30 L760 120 L600 210 L440 120Z" fill="none" stroke="#c8902a" stroke-width="3" opacity=".5"/><circle cx="600" cy="120" r="34" fill="none" stroke="#ff5a2a" stroke-width="3" class="bi-sf-sigil"/>
          ${columns}
          <ellipse class="bi-sf-halo" cx="600" cy="430" rx="420" ry="260" fill="url(#bi-sf-throne)"/>
          <g fill="#2a1c10" stroke="#a8803a" stroke-width="2.5"><path d="M380 600 L820 600 L780 560 L420 560Z"/><path d="M440 560 L760 560 L730 530 L470 530Z"/></g>
          ${brazier(230, 470, 0)}${brazier(970, 470, 1)}${brazier(430, 500, 2)}${brazier(770, 500, 3)}
          <path fill="#100a06" d="M0 600 H1200 V680 H0Z"/><path d="M0 600 H1200" stroke="#5a4428" stroke-width="3"/>
          <g>${embers}</g>
          <g class="bi-jr-near" fill="#080504"><path d="M0 680 L0 520 L60 520 L60 680Z"/><path d="M1200 680 L1200 520 L1140 520 L1140 680Z"/></g>
        </svg>`;
      },
    },
    // Gaian: the Old Growth in green gloom. Spores drift, the two Jungle Cores pulse awake among the
    // roots, and the Avatar of the Land rises out of the jungle floor.
    gaian: {
      duration: 7600,
      location: "THE OLD GROWTH · VERDIGRIS JUNGLE",
      overture: "The Old Growth stirs. Something vast is waking.",
      label: "AVATAR OF THE LAND",
      title: ["Gaian, ", "Avatar of the Land"],
      description: "The Heart of the Jungle — two Jungle Cores shield him and regrow when they fall. Bring both down together to sever him from the land.",
      imps: 2,
      retinue: "Jungle Core",
      scene() {
        const spores = Array.from({ length: 34 }, (_, i) => `<circle class="bi-ga-spore" style="--d:${((i * 0.43) % 6).toFixed(2)}s;--drift:${((i % 7) - 3) * 16}px" cx="${(i * 137) % 1200}" cy="${300 + (i * 53) % 360}" r="${1.4 + (i % 3) * 0.8}" fill="${i % 3 ? "#c8ff8a" : "#fff6b0"}"/>`).join("");
        const trunk = (x, w, lean) => `<path d="M${x - w} 680 Q${x - w * 0.6} 420 ${x - w * 0.4 + lean} 0 L${x + w * 0.4 + lean} 0 Q${x + w * 0.6} 420 ${x + w} 680Z"/><path d="M${x - w} 680 Q${x - w * 1.8} 640 ${x - w * 2.4} 680Z M${x + w} 680 Q${x + w * 1.9} 630 ${x + w * 2.6} 680Z"/>`;
        const vines = [120, 260, 410, 790, 930, 1080].map((x, i) => `<path class="bi-ga-vine" style="--d:${i * 0.4}s" d="M${x} 0 Q${x + 18} ${120 + i * 20} ${x - 8} ${220 + i * 26} T${x + 10} ${330 + i * 30}" fill="none" stroke="#1e3a18" stroke-width="5" stroke-linecap="round"/>`).join("");
        const roots = "M600 560 Q520 600 420 590 Q360 586 300 620 M600 560 Q680 604 790 592 Q860 590 920 626 M600 560 Q590 610 560 650 M600 560 Q622 612 650 652";
        return `<svg class="bi-art" viewBox="0 0 1200 680" preserveAspectRatio="xMidYMid slice" xmlns="http://www.w3.org/2000/svg">
          <defs>
            <linearGradient id="bi-ga-sky" x2="0" y2="1"><stop stop-color="#06100a"/><stop offset=".55" stop-color="#0f2414"/><stop offset="1" stop-color="#1a3a1e"/></linearGradient>
            <radialGradient id="bi-ga-glow"><stop stop-color="#e8ffb0" stop-opacity=".7"/><stop offset=".35" stop-color="#8fd85a" stop-opacity=".32"/><stop offset="1" stop-color="#0f2414" stop-opacity="0"/></radialGradient>
            <linearGradient id="bi-ga-shaft" x2="0" y2="1"><stop stop-color="#d8ffa0" stop-opacity=".35"/><stop offset="1" stop-color="#d8ffa0" stop-opacity="0"/></linearGradient>
          </defs>
          <rect width="1200" height="680" fill="url(#bi-ga-sky)"/>
          <g fill="#0a1a0c" opacity=".75">${trunk(80, 46, 10)}${trunk(300, 30, -8)}${trunk(930, 34, 6)}${trunk(1130, 52, -12)}</g>
          <path class="bi-ga-shaft" d="M520 0 L680 0 L760 560 L440 560Z" fill="url(#bi-ga-shaft)"/>
          <ellipse class="bi-ga-halo" cx="600" cy="500" rx="460" ry="250" fill="url(#bi-ga-glow)"/>
          <g fill="#06120a">${trunk(190, 58, 14)}${trunk(1010, 62, -16)}</g>
          <g>${vines}</g>
          <path fill="#0b1c0d" d="M0 560 Q200 520 420 540 T820 530 T1200 548 V680 H0Z"/>
          <path class="bi-ga-roots" fill="none" stroke="#8fd85a" stroke-width="5" stroke-linecap="round" d="${roots}"/>
          <g fill="#08160a"><path d="M0 680 L0 590 Q60 560 110 600 Q150 570 200 610 Q240 590 260 680Z"/><path d="M1200 680 L1200 580 Q1150 556 1100 596 Q1060 570 1010 612 Q980 600 960 680Z"/>
            <path d="M380 680 Q400 640 440 650 Q470 620 500 660 L520 680Z"/><path d="M700 680 Q720 646 760 652 Q790 628 820 662 L840 680Z"/></g>
          <g>${spores}</g>
          <rect class="bi-ga-mist" x="-100" y="520" width="1400" height="160" fill="#a8d8a0" opacity=".12"/>
        </svg>`;
      },
    },
    // Anatemis, the White Star: night over the Frozen Crown. The sky blazes with stars and aurora,
    // one star falls from it onto the summit, and the White Star unfurls her wings.
    anatemis: {
      duration: 8400,
      location: "THE FROZEN CROWN · STONEVEIL SUMMIT",
      overture: "Above the highest peak in the world, one star is falling.",
      label: "THE STRONGEST BEING ALIVE",
      title: ["Anatemis, ", "the White Star"],
      description: "Three trials, one per form: Mirror Scales throw your spells back until blades break them; the Golden Star calls down Sunfall; the Zenith gathers a supernova. Each form keeps the trials before it.",
      imps: 0,
      scene() { return starScene("white"); },
    },
    // The Golden Star: the sky warms to gold and a sun rises behind her.
    "anatemis-golden": {
      duration: 7200,
      location: "THE FROZEN CROWN · THE GOLDEN STAR",
      overture: "White starlight turns to gold.",
      label: "THE SECOND FORM",
      title: ["Anatemis, ", "the Golden Star"],
      description: "Sunfall — each turn she marks a hero, and the sun falls on them at her next turn unless you strike her hard enough to turn it aside. Her Mirror Scales still hold.",
      before: true,
      imps: 0,
      scene() { return starScene("gold"); },
    },
    // The Zenith of Dragonkind: everything turns to blinding white as the star in her heart ignites.
    "anatemis-zenith": {
      duration: 7600,
      location: "THE FROZEN CROWN · THE ZENITH",
      overture: "The heavens answer her.",
      label: "THE FINAL FORM",
      title: ["Anatemis, ", "the Zenith of Dragonkind"],
      description: "Supernova — she gathers a supernova over three of her turns; strike her hard enough to collapse it, or it burns every hero. Sunfall and her Mirror Scales remain.",
      before: true,
      imps: 0,
      scene() { return starScene("zenith"); },
    },
    // Ur-Mat, the False Dreamer: the Caldera falls silent. The air ripples like a dream, lava
    // freezes in mid-air, glyphs bloom across a sky turned violet, and the greatest sorcerer of
    // his age descends with his eyes closed. The world is his dream.
    "ur-mat": {
      duration: 8000,
      location: "THE CALDERA · CINDER ISLE",
      overture: "The volcano falls silent. The world holds its breath, as a sleeper does.",
      label: "THE FALSE DREAMER",
      title: ["Ur-Mat, ", "the False Dreamer"],
      description: "Dream Laws — every third turn he rewrites the law of this battle, in a fixed cycle. Learn the cycle, and endure each law until he writes the next.",
      imps: 0,
      scene() { return dreamScene(false); },
    },
    // Lucid Change: the dream cracks like glass and shatters, and the dreamer wakes.
    "ur-mat-lucid": {
      duration: 7200,
      location: "THE CALDERA · LUCID CHANGE",
      overture: "The false dream breaks.",
      label: "LUCID CHANGE",
      title: ["Ur-Mat ", "Awakens"],
      description: "Restored and empowered, he holds two dream laws at once, but his waking body fails a little more every turn.",
      before: true,
      imps: 0,
      scene() { return dreamScene(true); },
    },
    // Ancient Lich Ool: the Mass Graves at night. Green soul-fire seeps up out of the earth, his
    // three grave-knights claw their way out of the graves, his phylactery flares, and the lich
    // rises behind it.
    ool: {
      duration: 8200,
      location: "THE MASS GRAVES · ANCIENT BATTLEFIELD",
      overture: "The dead of the old war do not rest. Something keeps them.",
      label: "THE ANCIENT LICH",
      title: ["Ancient Lich ", "Ool"],
      description: "The Phylactery — while it stands he cannot truly die: he crumbles to dust and re-forms. His Legionnaires guard it from single blows; area attacks still reach it.",
      imps: 4,
      retinue: ["Gravebound Legionnaire", "Gravebound Legionnaire", "Gravebound Legionnaire", "Phylactery of Ool"],
      scene() { return graveScene(false); },
    },
    // The phylactery breaks: the crystal cracks and bursts, his soul tears out of it screaming,
    // and the soul-fire in his chest gutters to an ember.
    "ool-phylactery": {
      duration: 7200,
      location: "THE MASS GRAVES · THE PHYLACTERY BREAKS",
      overture: "The crystal cracks. Something inside it begins to scream.",
      label: "THE PHYLACTERY IS BROKEN",
      title: ["Ool, ", "Made Mortal"],
      description: "He can no longer re-form, and his spells are weaker. Bring him down for good.",
      before: true,
      imps: 0,
      scene() { return graveScene(true); },
    },
    // Ornn: night over the Lava Fields. Hammer blows ring out and sparks fly from a giant anvil
    // before a roaring forge; the smith-god looks up from his work and straightens.
    ornn: {
      duration: 7600,
      location: "THE LAVA FIELDS · CINDER ISLE",
      overture: "Hammer blows ring out across the Lava Fields.",
      label: "GOD OF THE FORGE",
      title: ["Ornn, ", "God of the Forge"],
      description: "The Living Forge — every few turns he reforges his gear at the anvil: a molten edge, hardened plate or a spiked shell. Deal him enough damage before his next turn and the metal cracks.",
      scene() { return forgeScene(false); },
    },
    // His second phase: he brings the hammer down on the anvil, the forge bursts open and the
    // Vulcanic Ram charges out of the flames.
    "ornn-phase": {
      duration: 7400,
      location: "THE LAVA FIELDS · PHASE II",
      overture: "The god strikes his anvil.",
      label: "PHASE II · THE FORGE ERUPTS",
      title: ["Ornn, ", "God of the Forge"],
      description: "The Vulcanic Ram bursts from the forge. While Ornn works at his anvil, the Ram guards him.",
      before: true,
      imps: 1,
      retinue: "Vulcanic Ram",
      scene() { return forgeScene(true); },
    },
    // The Deep Sea King: sunset on the Sunward Coast's beach. The tide draws back, the sea swells,
    // and the king rises out of the waves, water sheeting off him, and plants the Royal Trident
    // in the wet sand to name his challenge.
    "deep-sea-king": {
      duration: 7400,
      location: "THE SUNWARD COAST · GREAT HUNT",
      overture: "The tide draws back from the beach. Something regal rises with it.",
      label: "SOVEREIGN OF THE DEEP",
      title: ["Deep Sea ", "King"],
      description: "Royal Duel — he fights one champion at a time, always the one who hurt him most. Every other blow against him loses half its force.",
      scene() {
        const waves = Array.from({ length: 7 }, (_, i) => `<path class="bi-dk-wave" style="--d:${(i * 0.37).toFixed(2)}s" d="M-60 ${400 + i * 22} q60 -10 120 0 t120 0 t120 0 t120 0 t120 0 t120 0 t120 0 t120 0 t120 0 t120 0 t120 0" fill="none" stroke="${i % 2 ? "#9fd0d8" : "#d8eef0"}" stroke-width="${2 + (i % 3)}" opacity="${(0.35 + i * 0.06).toFixed(2)}"/>`).join("");
        const drops = Array.from({ length: 22 }, (_, i) => `<circle class="bi-dk-drop" style="--d:${(2.4 + (i % 7) * 0.08).toFixed(2)}s;--dx:${((i % 11) - 5) * 26}px;--dy:${-150 - (i * 37) % 160}px" cx="600" cy="560" r="${2 + (i % 3)}" fill="#d8f4f8"/>`).join("");
        const ripples = [0, 1, 2].map(i => `<ellipse class="bi-dk-ripple" style="--i:${i}" cx="690" cy="585" rx="40" ry="9" fill="none" stroke="#e8f8fa" stroke-width="3"/>`).join("");
        const gulls = Array.from({ length: 5 }, (_, i) => `<path class="bi-dk-gull" style="--d:${(i * 0.6).toFixed(1)}s" d="M${180 + i * 190} ${140 + (i % 2) * 40} q8 -7 16 0 q8 -7 16 0" fill="none" stroke="#2a2420" stroke-width="2.4"/>`).join("");
        return `<svg class="bi-art" viewBox="0 0 1200 680" preserveAspectRatio="xMidYMid slice" xmlns="http://www.w3.org/2000/svg">
          <defs>
            <linearGradient id="bi-dk-sky" x2="0" y2="1"><stop stop-color="#141c2a"/><stop offset=".45" stop-color="#3a3448"/><stop offset=".8" stop-color="#c8704a"/><stop offset="1" stop-color="#f0a860"/></linearGradient>
            <linearGradient id="bi-dk-sea" x2="0" y2="1"><stop stop-color="#2a4a5a"/><stop offset=".6" stop-color="#1f3f4f"/><stop offset="1" stop-color="#16303c"/></linearGradient>
            <linearGradient id="bi-dk-sand" x2="0" y2="1"><stop stop-color="#7a6248"/><stop offset=".4" stop-color="#5a4634"/><stop offset="1" stop-color="#2a2018"/></linearGradient>
            <radialGradient id="bi-dk-sun"><stop stop-color="#ffe8b0"/><stop offset=".5" stop-color="#ffb070" stop-opacity=".6"/><stop offset="1" stop-color="#ff8a50" stop-opacity="0"/></radialGradient>
            <linearGradient id="bi-dk-glint" x2="0" y2="1"><stop stop-color="#ffd8a0" stop-opacity=".7"/><stop offset="1" stop-color="#ffd8a0" stop-opacity="0"/></linearGradient>
          </defs>
          <rect width="1200" height="680" fill="url(#bi-dk-sky)"/>
          <circle cx="600" cy="378" r="190" fill="url(#bi-dk-sun)"/>
          <path d="M520 378 A80 80 0 0 1 680 378Z" fill="#ffd8a0"/>
          ${gulls}
          <rect class="bi-dk-sea" y="376" width="1200" height="200" fill="url(#bi-dk-sea)"/>
          <rect x="560" y="378" width="80" height="190" fill="url(#bi-dk-glint)" opacity=".5"/>
          <g class="bi-dk-swell">${waves}</g>
          <path class="bi-dk-foam" d="M0 568 Q150 556 300 566 T600 562 T900 568 T1200 560" fill="none" stroke="#f0fafa" stroke-width="5" opacity=".85"/>
          <path fill="#3a2e24" d="M0 572 Q200 562 420 574 T820 566 T1200 576 V680 H0Z"/>
          <path fill="url(#bi-dk-sand)" d="M0 590 Q240 580 520 594 T1000 586 T1200 596 V680 H0Z"/>
          ${ripples}
          ${drops}
          <g class="bi-jr-near" fill="#120e0a"><path d="M0 680 L0 560 Q50 580 90 620 Q130 604 170 650 L200 680Z"/><path d="M1200 680 L1200 540 Q1150 576 1110 620 Q1070 606 1030 650 L1000 680Z"/></g>
        </svg>`;
      },
    },
    // Zik Lun, the armored Hulk: a grey dusk in the Stoneveil Foothills. War drums roll over the
    // hills, a long column of trolls marches along the ridge under their standards, the war horn
    // blasts, and the warlord strides out between his Shieldbearer and his War-drummer.
    "zik-lun": {
      duration: 7400,
      location: "THE STONEVEIL FOOTHILLS · GREAT HUNT",
      overture: "First come the drums. Then the horn. Then the hills themselves march.",
      label: "WARLORD OF THE HILL TROLLS",
      title: ["Zik Lun, ", "the armored Hulk"],
      description: "Warband Chief — he bellows orders to his trolls, calls more with his war horn, and rages for every troll that falls.",
      imps: 2,
      retinue: ["Troll Shieldbearer", "Troll War-drummer"],
      scene() {
        // A column of troll silhouettes marching along the far ridge, standards among them.
        const marchers = Array.from({ length: 16 }, (_, i) => {
          const x = 40 + i * 74 + (i % 3) * 9, y = 398 - Math.sin(i * 0.7) * 10 - (i > 7 ? (i - 7) * 4 : 0), size = 0.75 + (i % 4) * 0.08;
          const standard = i % 5 === 2 ? `<path d="M${x + 9} ${y - 40 * size} L${x + 9} ${y - 2}" stroke="#120d0b" stroke-width="2.5"/><path class="bi-zl-flag" d="M${x + 9} ${y - 40 * size} l22 3 l-6 7 l7 8 l-23 -2Z" fill="#7a2416"/>` : "";
          return `<g class="bi-zl-marcher" style="--d:${(i * 0.13).toFixed(2)}s">${standard}<path d="M${x - 9 * size} ${y} L${x - 7 * size} ${y - 22 * size} Q${x - 10 * size} ${y - 30 * size} ${x - 3 * size} ${y - 34 * size} Q${x + 2 * size} ${y - 42 * size} ${x + 7 * size} ${y - 34 * size} Q${x + 12 * size} ${y - 28 * size} ${x + 9 * size} ${y - 20 * size} L${x + 10 * size} ${y}Z"/></g>`;
        }).join("");
        const rings = [0, 1, 2].map(i => `<ellipse class="bi-zl-horn" style="--i:${i}" cx="600" cy="300" rx="120" ry="60" fill="none" stroke="#ffd8a8" stroke-width="5"/>`).join("");
        const drums = [0, 1, 2, 3, 4, 5].map(i => `<circle class="bi-zl-drum" style="--d:${(0.35 + i * 0.42).toFixed(2)}s" cx="${i % 2 ? 900 : 300}" cy="470" r="60" fill="none" stroke="#d0603a" stroke-width="4"/>`).join("");
        const birds = Array.from({ length: 9 }, (_, i) => `<path class="bi-zl-bird" style="--d:${(3.1 + i * 0.07).toFixed(2)}s;--dx:${(i - 4) * 46}px" d="M${560 + i * 12} ${250 + (i % 3) * 10} q6 -6 12 0 q6 -6 12 0" fill="none" stroke="#1a1410" stroke-width="2.5"/>`).join("");
        return `<svg class="bi-art" viewBox="0 0 1200 680" preserveAspectRatio="xMidYMid slice" xmlns="http://www.w3.org/2000/svg">
          <defs>
            <linearGradient id="bi-zl-sky" x2="0" y2="1"><stop stop-color="#1c2129"/><stop offset=".55" stop-color="#3a3532"/><stop offset=".85" stop-color="#7a5236"/><stop offset="1" stop-color="#a8643a"/></linearGradient>
            <radialGradient id="bi-zl-glow"><stop stop-color="#ffd8a8" stop-opacity=".55"/><stop offset=".4" stop-color="#d0603a" stop-opacity=".22"/><stop offset="1" stop-color="#3a3532" stop-opacity="0"/></radialGradient>
            <linearGradient id="bi-zl-mist" x2="0" y2="1"><stop stop-color="#a89880" stop-opacity="0"/><stop offset=".6" stop-color="#8a7a66" stop-opacity=".22"/><stop offset="1" stop-color="#2a221c" stop-opacity=".55"/></linearGradient>
          </defs>
          <rect width="1200" height="680" fill="url(#bi-zl-sky)"/>
          <path fill="#2a2a2e" opacity=".85" d="M0 330 L140 250 L260 280 L420 150 L520 200 L640 90 L760 190 L880 160 L1000 260 L1200 220 V680 H0Z"/>
          <ellipse class="bi-zl-glow" cx="600" cy="420" rx="460" ry="260" fill="url(#bi-zl-glow)"/>
          ${rings}
          ${birds}
          <path fill="#1d1a18" d="M0 420 Q200 380 420 392 T820 380 T1200 400 V680 H0Z"/>
          <g class="bi-zl-column" fill="#120d0b">${marchers}</g>
          ${drums}
          <path fill="#151210" d="M0 520 Q240 488 520 506 T1000 494 T1200 510 V680 H0Z"/>
          <path fill="#0d0b0a" d="M0 600 Q170 580 340 596 T650 604 T950 590 T1200 600 V680 H0Z"/>
          <rect class="bi-jr-mist" y="440" width="1200" height="240" fill="url(#bi-zl-mist)"/>
          <g class="bi-jr-near" fill="#070605"><path d="M0 680 L0 512 Q40 548 80 600 Q120 584 160 640 L200 680Z"/><path d="M1200 680 L1200 500 Q1150 546 1120 600 Q1080 584 1040 636 L1000 680Z"/></g>
        </svg>`;
      },
    },
    // Nygheir the Wounded: dusk at the foot of the Stoneveils, on a battlefield nobody buried.
    // The mountain's foot rumbles, stones roll off a great mound, two amber eyes open in it and the
    // old dragon rises, still carrying the spears and the banner of the army that scarred him.
    nygheir: {
      duration: 7400,
      location: "THE STONEVEIL MOUNTAINFOOT · GREAT HUNT",
      overture: "The old soldiers swore the mountain's foot still breathes. Tonight it opens its eyes.",
      label: "ANCIENT OF THE STONEVEILS",
      title: ["Nygheir ", "the Wounded"],
      description: "Old Wounds — critical hits tear his scars open: he bleeds and takes more damage, but every open wound feeds his rage.",
      scene() {
        const relics = battlefield("#0a0706", "#5a3a22");
        // The mound he sleeps under; each boulder rolls its own way when he rises.
        const boulders = [[520, 560, 46, -160, 60, -40], [600, 520, 62, -20, -150, 25], [690, 556, 50, 170, 50, 50], [560, 600, 40, -120, 90, -30], [650, 604, 42, 110, 90, 35], [460, 600, 30, -200, 40, -60], [745, 600, 34, 210, 40, 70], [610, 470, 30, 30, -220, 80], [545, 500, 28, -90, -170, -50]]
          .map(([x, y, r, dx, dy, turn], i) => `<path class="bi-ny-boulder" style="--dx:${dx}px;--dy:${dy}px;--r:${turn}deg" d="${rock(x, y, r, i + 1)}"/>`).join("");
        const pebbles = Array.from({ length: 18 }, (_, i) => {
          const x = 180 + (i * 211) % 840, size = 4 + (i * 7) % 6;
          return `<path class="bi-ny-pebble" style="--d:${(1.05 + ((i * 0.53) % 2.1)).toFixed(2)}s;--fall:${560 + (i * 37) % 80}px" d="${rock(x, -16, size, i + 20)}"/>`;
        }).join("");
        const cracks = "M600 610 L548 628 L478 622 L430 640 M600 610 L660 630 L736 626 L790 646 M600 610 L590 650 L612 690 M548 628 L520 672 M660 630 L688 676";
        return `<svg class="bi-art" viewBox="0 0 1200 680" preserveAspectRatio="xMidYMid slice" xmlns="http://www.w3.org/2000/svg">
          <defs>
            <linearGradient id="bi-ny-sky" x2="0" y2="1"><stop stop-color="#07060a"/><stop offset=".5" stop-color="#191210"/><stop offset=".86" stop-color="#3f2817"/><stop offset="1" stop-color="#5a361a"/></linearGradient>
            <radialGradient id="bi-ny-glow"><stop stop-color="#ffe2b0" stop-opacity=".7"/><stop offset=".25" stop-color="#ffb04a" stop-opacity=".4"/><stop offset=".65" stop-color="#8a3f1a" stop-opacity=".16"/><stop offset="1" stop-color="#190f0a" stop-opacity="0"/></radialGradient>
            <linearGradient id="bi-ny-mist" x2="0" y2="1"><stop stop-color="#a87a52" stop-opacity="0"/><stop offset=".6" stop-color="#8a6446" stop-opacity=".2"/><stop offset="1" stop-color="#2a1c12" stop-opacity=".55"/></linearGradient>
            <filter id="bi-ny-soft" x="-50%" y="-50%" width="200%" height="200%"><feGaussianBlur stdDeviation="18"/></filter>
          </defs>
          <rect width="1200" height="680" fill="url(#bi-ny-sky)"/>
          ${mountain("#120d0b", "#8a5a32")}
          <ellipse class="bi-ny-halo" cx="600" cy="500" rx="460" ry="300" fill="url(#bi-ny-glow)"/>
          <path fill="#0d0907" d="M0 540 Q150 500 300 520 T560 506 T860 520 T1200 500 V680 H0Z"/>
          ${relics}
          <g fill="#160f0c" stroke="#3a2a1e" stroke-width="3">${boulders}</g>
          <g class="bi-ny-eyes" fill="#ffc35e"><path d="M572 528 l18 -6 l6 8 l-18 4Z"/><path d="M644 526 l-18 -6 l-6 8 l18 4Z"/></g>
          <path class="bi-ny-crack" fill="none" stroke="#ffb04a" stroke-width="4" stroke-linecap="round" d="${cracks}"/>
          <path fill="#0a0706" d="M0 600 Q170 580 340 596 T650 604 T950 590 T1200 600 V680 H0Z"/>
          <g class="bi-ny-dust" fill="#7a5a3e" filter="url(#bi-ny-soft)"><ellipse cx="600" cy="600" rx="300" ry="70"/><ellipse cx="430" cy="610" rx="150" ry="50"/><ellipse cx="780" cy="606" rx="160" ry="54"/></g>
          <g fill="#1a120d">${pebbles}</g>
          <rect class="bi-jr-mist" y="450" width="1200" height="230" fill="url(#bi-ny-mist)"/>
          <g class="bi-jr-near" fill="#050302"><path d="M0 680 L0 520 Q50 560 80 610 Q120 590 160 640 L200 680Z"/><path d="M1200 680 L1200 506 Q1150 552 1120 606 Q1080 588 1040 640 L1000 680Z"/></g>
        </svg>`;
      },
    },
    // Nygheir's second phase, "Old wounds remember": the kneeling dragon relives the battle that
    // scarred him in three flashes (the spear wall, the spears breaking on his scales, the fallen
    // knight under his banner), then every scar bursts into fire and he rises again.
    "nygheir-phase": {
      duration: 7800,
      location: "THE STONEVEIL MOUNTAINFOOT · PHASE II",
      overture: "Old wounds remember.",
      label: "PHASE II · OLD WOUNDS REMEMBER",
      title: ["Nygheir ", "the Scarred"],
      description: "His scars burst and burn: he heals, wields the mountain's magic and rages at full strength — but every wound lies open.",
      before: true,
      scene() {
        const cracks = "M600 600 L540 622 L470 616 L410 638 L340 632 M600 600 L664 624 L740 618 L800 640 L880 634 M600 600 L586 646 L604 690 M540 622 L512 670 M664 624 L694 674 M470 616 L452 660 M740 618 L760 664";
        const embers = Array.from({ length: 26 }, (_, i) => `<circle class="bi-ny-ember" style="--d:${(4.4 + ((i * 0.41) % 2.6)).toFixed(2)}s;--drift:${((i % 9) - 4) * 18}px" cx="${300 + (i * 173) % 600}" cy="${560 + (i * 29) % 80}" r="${1.6 + (i % 3)}" fill="${i % 3 ? "#ffb04a" : "#ffe2a8"}"/>`).join("");
        // Memory 1: the spear wall, nine soldiers braced under a blood-red sky.
        const soldiers = Array.from({ length: 9 }, (_, i) => {
          const x = 120 + i * 112 + (i % 2) * 18, y = 600 - (i % 3) * 14;
          return `<g transform="translate(${x} ${y})"><path d="M-14 0 L-11 -62 Q0 -70 11 -62 L14 0Z"/><circle cx="0" cy="-74" r="12"/><path d="M-13 -80 Q0 -96 13 -80Z"/><ellipse cx="14" cy="-38" rx="16" ry="24"/><path d="M-2 -40 L210 -300 L216 -296 L4 -34Z"/><path d="M210 -300 L232 -326 L218 -296Z"/></g>`;
        }).join("");
        // Memory 2: spears shatter on his scales while the arrows fall.
        const shards = [[700, 300, 30, 80], [780, 220, -40, 60], [860, 340, 70, 70], [640, 420, -20, 54], [920, 260, 110, 64], [740, 470, 45, 48], [990, 400, -70, 58]]
          .map(([x, y, turn, length]) => `<g transform="translate(${x} ${y}) rotate(${turn})"><path d="M-4 0 L-4 ${-length} L-1 ${-length - 7} L1 ${-length + 3} L4 ${-length - 4} L4 0 L2 6 L-1 2Z"/></g>`).join("");
        const arrows = Array.from({ length: 22 }, (_, i) => {
          const x = 40 + (i * 61) % 620, y = 40 + (i * 47) % 260;
          return `<g transform="translate(${x} ${y}) rotate(${28 + (i % 5) * 4})"><path d="M0 0 L60 0" stroke="#1a0a06" stroke-width="2.4"/><path d="M60 -4 L70 0 L60 4Z"/></g>`;
        }).join("");
        // Memory 3: the fallen knight, his sword broken and his banner burning.
        const banner = `<g class="bi-ny-banner"><path d="M760 610 L702 200" stroke="#2a1a10" stroke-width="10" stroke-linecap="round"/><path d="M706 222 Q800 210 892 236 L858 300 L904 368 Q808 350 722 352Z" fill="#34466e" stroke="#120806" stroke-width="5"/><path d="M714 236 Q800 226 878 248 M724 340 Q806 336 884 356" fill="none" stroke="#c8a54a" stroke-width="5"/><path d="M790 270 l22 -22 l22 22 l-22 22Z" fill="#c8a54a" stroke="#120806" stroke-width="3"/><path class="bi-ny-burn" d="M858 300 L904 368 Q880 362 864 360 L872 338 L850 330 L862 312Z" fill="#ff8a3d"/></g>`;
        const memory = (index, sky, content) => `<g class="bi-ny-memory bi-ny-memory-${index}"><rect width="1200" height="680" fill="${sky}"/>${content}<rect width="1200" height="680" fill="url(#bi-ny-vignette)"/><path d="M0 120 L1200 128 M0 410 L1200 404 M300 0 L306 680" stroke="#f6d8b0" stroke-width="1.2" opacity=".14"/><rect class="bi-ny-cut" width="1200" height="680" fill="#fff4dc"/></g>`;
        return `<svg class="bi-art" viewBox="0 0 1200 680" preserveAspectRatio="xMidYMid slice" xmlns="http://www.w3.org/2000/svg">
          <defs>
            <linearGradient id="bi-ny-night" x2="0" y2="1"><stop stop-color="#060304"/><stop offset=".55" stop-color="#1c0a07"/><stop offset="1" stop-color="#4a1a0c"/></linearGradient>
            <radialGradient id="bi-ny-fire"><stop stop-color="#ffe2a8" stop-opacity=".75"/><stop offset=".28" stop-color="#ff8a3d" stop-opacity=".45"/><stop offset=".7" stop-color="#7a240e" stop-opacity=".15"/><stop offset="1" stop-color="#1c0a07" stop-opacity="0"/></radialGradient>
          </defs>
          <rect width="1200" height="680" fill="url(#bi-ny-night)"/>
          ${mountain("#0e0605", "#7a2a12")}
          <ellipse class="bi-ny-blaze" cx="600" cy="520" rx="480" ry="320" fill="url(#bi-ny-fire)"/>
          <path fill="#0b0504" d="M0 540 Q150 500 300 520 T560 506 T860 520 T1200 500 V680 H0Z"/>
          ${battlefield("#080404", "#6a2410")}
          <path fill="#080303" d="M0 600 Q170 580 340 596 T650 604 T950 590 T1200 600 V680 H0Z"/>
          <path class="bi-ny-crack bi-ny-crack-late" fill="none" stroke="#ff9a3d" stroke-width="5" stroke-linecap="round" d="${cracks}"/>
          <g>${embers}</g>
        </svg><svg class="bi-art bi-ny-memories" viewBox="0 0 1200 680" preserveAspectRatio="xMidYMid slice" xmlns="http://www.w3.org/2000/svg">
          <defs>
            <radialGradient id="bi-ny-vignette"><stop offset=".45" stop-color="#000" stop-opacity="0"/><stop offset="1" stop-color="#050202" stop-opacity=".9"/></radialGradient>
            <radialGradient id="bi-ny-mem1"><stop stop-color="#b8502a"/><stop offset=".6" stop-color="#5a1c0c"/><stop offset="1" stop-color="#1a0604"/></radialGradient>
            <radialGradient id="bi-ny-mem2" cx=".7" cy=".4"><stop stop-color="#e8a060"/><stop offset=".5" stop-color="#7a3416"/><stop offset="1" stop-color="#1a0604"/></radialGradient>
            <linearGradient id="bi-ny-mem3" x2="0" y2="1"><stop stop-color="#3a2a26"/><stop offset=".6" stop-color="#5a3a2a"/><stop offset="1" stop-color="#1a0e0a"/></linearGradient>
          </defs>
          ${memory(1, "url(#bi-ny-mem1)", `<g fill="#120604">${soldiers}<path d="M1200 0 L1200 330 Q1100 240 980 260 Q1040 160 1010 70 Q1100 90 1200 0Z"/><path d="M0 680 L0 610 Q300 590 600 606 T1200 600 V680Z"/></g>`)}
          ${memory(2, "url(#bi-ny-mem2)", `<g fill="#120604"><path d="M1200 140 Q1010 200 930 330 Q900 380 860 400 L900 430 L870 470 L930 470 L910 520 Q1010 470 1060 400 Q1120 330 1200 300Z"/><path d="M940 330 Q990 300 1040 320 M960 380 Q1010 350 1070 368 M990 430 Q1040 400 1100 410" fill="none" stroke="#5a2410" stroke-width="5"/>${shards}${arrows}<path d="M0 680 L0 620 Q300 600 640 616 T1200 610 V680Z"/></g>`)}
          ${memory(3, "url(#bi-ny-mem3)", `<g fill="#2a1a14" opacity=".55"><ellipse cx="980" cy="150" rx="160" ry="36"/><ellipse cx="1080" cy="96" rx="120" ry="28"/></g><g fill="#160c08" stroke="#c87a3e" stroke-width="3" stroke-opacity=".75"><path d="M0 680 L0 580 Q300 548 620 580 T1200 560 V680Z"/><path d="M380 552 Q420 522 470 526 L560 520 Q600 512 620 526 L640 548 Q560 558 470 558 Z"/><circle cx="372" cy="538" r="20"/><path d="M352 532 Q372 508 392 532Z"/><path d="M660 558 L720 544 L724 550 L664 564Z"/><path d="M600 564 L628 558 L630 564Z"/></g>${banner}`)}
        </svg>`;
      },
    },
  };

  // Ornn's forge on the Lava Fields, for his intro (the work) and phase (the eruption).
  function forgeScene(phase) {
    // Sparks burst from the anvil on every hammer blow.
    const sparks = Array.from({ length: phase ? 34 : 22 }, (_, i) => {
      const angle = -Math.PI * (0.08 + 0.84 * ((i * 0.618) % 1)), reach = 70 + (i * 37) % 150;
      return `<circle class="bi-or-spark" style="--d:${((i % 4) * 0.05).toFixed(2)}s;--sx:${(Math.cos(angle) * reach).toFixed(0)}px;--sy:${(Math.sin(angle) * reach).toFixed(0)}px" cx="600" cy="500" r="${1.6 + (i % 3) * 0.8}" fill="${i % 3 ? "#ffd27a" : "#fff4d0"}"/>`;
    }).join("");
    const ash = Array.from({ length: 28 }, (_, i) => `<circle class="bi-or-ash" style="--d:${((i * 0.37) % 5).toFixed(2)}s;--drift:${((i % 7) - 3) * 14}px" cx="${(i * 151) % 1200}" cy="${420 + (i * 41) % 240}" r="${1.2 + (i % 3) * 0.7}" fill="${i % 4 ? "#ff9a4a" : "#ffe2a8"}"/>`).join("");
    const cracks = "M0 610 L140 596 L210 618 L330 600 M420 640 L520 616 L600 628 L700 612 L790 640 M860 604 L960 620 L1050 598 L1200 612 M240 660 L300 640 L380 664 M820 668 L900 646 L980 670";
    const eruption = phase ? `<g class="bi-or-erupt"><path d="M600 470 Q560 340 520 230 Q600 300 620 180 Q650 300 700 220 Q680 340 640 470Z" fill="#ff8a3d"/><path d="M600 470 Q585 380 570 300 Q605 350 615 270 Q630 350 650 300 Q640 390 630 470Z" fill="#ffe2a8"/></g>` : "";
    return `<svg class="bi-art" viewBox="0 0 1200 680" preserveAspectRatio="xMidYMid slice" xmlns="http://www.w3.org/2000/svg">
      <defs>
        <linearGradient id="bi-or-sky" x2="0" y2="1"><stop stop-color="#070304"/><stop offset=".5" stop-color="#1e0907"/><stop offset="1" stop-color="#4a160a"/></linearGradient>
        <radialGradient id="bi-or-glow"><stop stop-color="#ffe2a8" stop-opacity=".8"/><stop offset=".3" stop-color="#ff8a3d" stop-opacity=".45"/><stop offset=".7" stop-color="#7a240e" stop-opacity=".15"/><stop offset="1" stop-color="#1e0907" stop-opacity="0"/></radialGradient>
        <linearGradient id="bi-or-lava" x2="0" y2="1"><stop stop-color="#ffd27a"/><stop offset="1" stop-color="#c0400e"/></linearGradient>
      </defs>
      <rect width="1200" height="680" fill="url(#bi-or-sky)"/>
      <path fill="#120605" d="M0 420 L180 330 L300 360 L470 210 L540 236 L600 170 L660 236 L730 210 L900 360 L1020 330 L1200 420 V680 H0Z"/>
      <path fill="url(#bi-or-lava)" opacity=".85" d="M588 176 Q600 168 612 176 L630 250 Q610 300 640 360 L620 362 Q596 300 604 250Z"/>
      <path class="bi-or-smoke" fill="#2a1410" opacity=".6" d="M560 170 Q520 100 580 60 Q620 20 680 50 Q740 70 700 120 Q660 160 640 170Z"/>
      <ellipse class="bi-or-forgeglow" cx="600" cy="420" rx="460" ry="260" fill="url(#bi-or-glow)"/>
      <g class="bi-or-forge"><path fill="#1a0c08" stroke="#3a1a10" stroke-width="4" d="M440 470 L460 300 Q600 240 740 300 L760 470Z"/>
        <path fill="#3a1408" d="M500 470 L510 360 Q600 320 690 360 L700 470Z"/>
        <path class="bi-or-mouth" fill="#ff8a3d" d="M520 470 L528 380 Q600 346 672 380 L680 470Z"/>
        <path fill="#ffd27a" opacity=".7" d="M548 470 Q600 400 652 470Z"/></g>
      ${eruption}
      <path fill="#0c0504" d="M0 520 Q160 490 330 506 T660 500 T960 512 T1200 496 V680 H0Z"/>
      <path class="bi-or-cracks" fill="none" stroke="#ff8a3d" stroke-width="5" stroke-linecap="round" d="${cracks}"/>
      <g class="bi-or-anvil"><path fill="#2a2a30" stroke="#5a5a64" stroke-width="3" d="M520 500 L700 500 L716 486 L740 482 L736 472 L690 470 L688 462 L520 462 Q500 470 480 466 L478 476 Q500 484 520 486Z"/>
        <path fill="#1e1e24" d="M560 500 L660 500 L670 560 L550 560Z"/><path fill="#16161a" d="M530 560 L690 560 L700 580 L520 580Z"/>
        <path class="bi-or-hot" fill="#ffb04a" d="M560 462 L640 462 L636 456 L566 456Z"/></g>
      <g>${sparks}</g>
      <g>${ash}</g>
      <g class="bi-jr-near" fill="#060203"><path d="M0 680 L0 540 Q60 570 100 620 Q140 600 180 650 L220 680Z"/><path d="M1200 680 L1200 520 Q1150 560 1110 610 Q1070 596 1030 646 L990 680Z"/></g>
    </svg>`;
  }

  // The storm altar of the Church of Talos, for Tankina's intro (a drizzle) and phase (the strike).
  function talosScene(phase) {
    const rain = Array.from({ length: phase ? 70 : 34 }, (_, i) => {
      const x = (i * 97) % 1260 - 30, y = (i * 53) % 640, length = phase ? 42 : 26;
      return `<path class="bi-tk-rain" style="--d:${((i * 0.071) % 0.9).toFixed(2)}s" d="M${x} ${y} l-9 ${length}" stroke="#bcd2ff" stroke-width="${phase ? 1.6 : 1.1}" opacity="${phase ? 0.5 : 0.35}"/>`;
    }).join("");
    const bolt = (x, d, delay) => `<path class="bi-tk-bolt" style="--d:${delay}s" d="M${x} -10 ${d}" fill="none" stroke="#e8f0ff" stroke-width="4" stroke-linejoin="round"/>`;
    const bolts = bolt(260, "l-30 90 l40 10 l-50 120 l30 -6 l-40 110", 1.2) + bolt(980, "l25 100 l-35 14 l45 110 l-25 0 l30 90", 2.6) + bolt(140, "l20 80 l-30 20 l35 90", 4.1);
    const strike = phase ? `<path class="bi-tk-strike" d="M610 -20 l-40 140 l55 12 l-70 150 l45 -8 l-60 210" fill="none" stroke="#ffffff" stroke-width="12" stroke-linejoin="round"/><path class="bi-tk-strike" d="M610 -20 l-40 140 l55 12 l-70 150 l45 -8 l-60 210" fill="none" stroke="#9fc8ff" stroke-width="26" stroke-linejoin="round" opacity=".45"/>` : "";
    const channels = "M470 520 L470 600 M540 520 L530 612 M660 520 L670 612 M730 520 L730 600 M600 520 L600 620";
    return `<svg class="bi-art" viewBox="0 0 1200 680" preserveAspectRatio="xMidYMid slice" xmlns="http://www.w3.org/2000/svg">
      <defs>
        <linearGradient id="bi-tk-sky" x2="0" y2="1"><stop stop-color="#06070c"/><stop offset=".55" stop-color="#151a28"/><stop offset="1" stop-color="#262c3c"/></linearGradient>
        <radialGradient id="bi-tk-glow"><stop stop-color="#e8f0ff" stop-opacity=".7"/><stop offset=".35" stop-color="#9fc8ff" stop-opacity=".3"/><stop offset="1" stop-color="#151a28" stop-opacity="0"/></radialGradient>
        <filter id="bi-tk-soft" x="-50%" y="-50%" width="200%" height="200%"><feGaussianBlur stdDeviation="22"/></filter>
      </defs>
      <rect width="1200" height="680" fill="url(#bi-tk-sky)"/>
      <g class="bi-tk-clouds" fill="#1c2230">${[[200, 90, 260], [520, 60, 300], [860, 100, 280], [1100, 70, 220], [380, 150, 200], [760, 170, 240]].map(([x, y, r]) => `<ellipse cx="${x}" cy="${y}" rx="${r}" ry="${r * 0.32}"/>`).join("")}</g>
      <ellipse class="bi-tk-flicker" cx="600" cy="120" rx="520" ry="140" fill="#9fc8ff" opacity="0" filter="url(#bi-tk-soft)"/>
      ${bolts}
      <path fill="#0c0e14" d="M0 470 Q200 420 420 440 T820 430 T1200 450 V680 H0Z"/>
      <g fill="#0a0b10"><path d="M80 470 V300 L120 250 L160 300 V470Z"/><path d="M60 300 L120 210 L180 300Z"/><rect x="160" y="340" width="190" height="130"/><path d="M150 340 L255 280 L360 340Z"/><rect x="108" y="330" width="22" height="34" fill="#3a3020"/></g>
      <ellipse class="bi-tk-halo" cx="600" cy="470" rx="420" ry="260" fill="url(#bi-tk-glow)"/>
      ${strike}
      <g class="bi-tk-altar"><path fill="#1a1c24" stroke="#3a4054" stroke-width="3" d="M420 520 L780 520 L760 610 L440 610Z"/><path fill="#22252f" stroke="#3a4054" stroke-width="3" d="M440 500 L760 500 L780 520 L420 520Z"/>
        <path class="bi-tk-blood" fill="none" stroke="#9a1a1a" stroke-width="7" stroke-linecap="round" d="${channels}"/>
        <path fill="none" stroke="#e0c060" stroke-width="3" opacity=".7" d="M590 540 l-12 24 l14 0 l-12 26"/></g>
      <path fill="#07080c" d="M0 600 Q170 580 340 596 T650 604 T950 590 T1200 600 V680 H0Z"/>
      <g class="bi-tk-rainfall">${rain}</g>
      <g class="bi-jr-near" fill="#040508"><path d="M0 680 L0 520 Q50 560 80 610 Q120 590 160 640 L200 680Z"/><path d="M1200 680 L1200 500 Q1150 548 1120 602 Q1080 588 1040 640 L1000 680Z"/></g>
    </svg>`;
  }

  // Shared pieces of Nygheir's scenes: the Stoneveils behind and the battlefield nobody buried.
  /* The Frozen Crown under Anatemis's sky: a snowbound summit beneath a sky of stars and aurora.
     "white": a star falls onto the summit; "gold": the sky warms and a sun rises behind her;
     "zenith": a blinding bloom of rings and rays as the star in her heart ignites. */
  function starScene(form) {
    const gold = form === "gold", zenith = form === "zenith";
    const sky = zenith ? ["#1a1a24", "#4a4038", "#a89878"] : gold ? ["#120c06", "#3a2610", "#7a4a18"] : ["#03050e", "#0c1830", "#1e3050"];
    const stars = Array.from({ length: 70 }, (_, i) => `<circle class="bi-an-star" style="--d:${((i * 0.23) % 4).toFixed(2)}s" cx="${(i * 211) % 1200}" cy="${(i * 89) % 420}" r="${0.7 + (i % 4) * 0.5}" fill="${i % 5 ? "#ffffff" : gold ? "#ffe08a" : "#bcd8ff"}"/>`).join("");
    const aurora = [["#7fe8ff", 120], ["#b89cff", 170], ["#7fffc8", 220]].map(([c, y], i) => `<path class="bi-an-aurora" style="--d:${i * 0.7}s" d="M-100 ${y} C200 ${y - 70} 420 ${y + 50} 640 ${y - 20} S1040 ${y - 80} 1300 ${y}" fill="none" stroke="${gold ? "#ffcf6a" : c}" stroke-width="${36 - i * 8}" opacity=".22"/>`).join("");
    const peaks = `<path fill="#0c1420" d="M0 520 L140 430 L260 470 L420 330 L540 380 L600 300 L680 360 L800 320 L940 430 L1060 400 L1200 470 V680 H0Z"/><path fill="#d8e4f0" opacity=".85" d="M420 330 L470 352 L500 345 L540 380 L560 368 L600 300 L640 330 L680 360 L720 342 L800 320 L760 352 L700 380 L640 360 L600 340 L560 392 L500 370 L460 380Z"/><path fill="#e8f0f8" d="M0 600 Q300 560 600 580 T1200 590 V680 H0Z"/>`;
    const fall = form === "white" ? `<g class="bi-an-fall"><path d="M860 160 L640 330" stroke="#ffffff" stroke-width="14" stroke-linecap="round" opacity=".55"/><path d="M760 238 L640 330" stroke="#ffffff" stroke-width="8" stroke-linecap="round"/><circle cx="640" cy="330" r="46" fill="url(#bi-an-glow)"/><circle cx="640" cy="330" r="18" fill="#ffffff"/></g><circle class="bi-an-impact" cx="600" cy="330" r="60" fill="url(#bi-an-glow)"/>` : "";
    const sun = gold ? `<g class="bi-an-sun"><circle cx="600" cy="300" r="190" fill="url(#bi-an-sunfill)"/>${Array.from({ length: 16 }, (_, i) => { const a = i / 16 * Math.PI * 2; return `<path d="M${600 + Math.cos(a) * 210} ${300 + Math.sin(a) * 210} L${600 + Math.cos(a) * 320} ${300 + Math.sin(a) * 320}" stroke="#ffd27a" stroke-width="6" stroke-linecap="round" opacity=".7"/>`; }).join("")}</g>` : "";
    const bloom = zenith ? `<g class="bi-an-bloom">${[0, 1, 2, 3].map(i => `<circle class="bi-an-ring" style="--d:${(i * 0.5).toFixed(2)}s" cx="600" cy="320" r="120" fill="none" stroke="#ffffff" stroke-width="5"/>`).join("")}${Array.from({ length: 24 }, (_, i) => { const a = i / 24 * Math.PI * 2; return `<path d="M600 320 L${600 + Math.cos(a) * 900} ${320 + Math.sin(a) * 900}" stroke="#fff6dc" stroke-width="${i % 2 ? 3 : 7}" opacity=".35"/>`; }).join("")}</g>` : "";
    return `<svg class="bi-art" viewBox="0 0 1200 680" preserveAspectRatio="xMidYMid slice" xmlns="http://www.w3.org/2000/svg">
      <defs>
        <linearGradient id="bi-an-sky" x2="0" y2="1"><stop stop-color="${sky[0]}"/><stop offset=".55" stop-color="${sky[1]}"/><stop offset="1" stop-color="${sky[2]}"/></linearGradient>
        <radialGradient id="bi-an-glow"><stop stop-color="#ffffff"/><stop offset=".3" stop-color="${gold ? "#ffe08a" : "#e8f0ff"}" stop-opacity=".7"/><stop offset="1" stop-color="#ffffff" stop-opacity="0"/></radialGradient>
        <radialGradient id="bi-an-sunfill"><stop stop-color="#fff6d8"/><stop offset=".5" stop-color="#ffcf6a"/><stop offset="1" stop-color="#ff9a3a" stop-opacity="0"/></radialGradient>
        <linearGradient id="bi-an-trail" x1="1" y1="0" x2="0" y2="1"><stop stop-color="#ffffff" stop-opacity="0"/><stop offset="1" stop-color="#ffffff"/></linearGradient>
      </defs>
      <rect width="1200" height="680" fill="url(#bi-an-sky)"/>
      <g>${stars}</g>
      <g>${aurora}</g>
      ${sun}${bloom}
      <ellipse class="bi-an-halo" cx="600" cy="330" rx="460" ry="300" fill="url(#bi-an-glow)" opacity=".5"/>
      ${peaks}
      ${fall}
      <rect class="bi-an-snow" x="-100" y="560" width="1400" height="120" fill="#ffffff" opacity=".1"/>
    </svg>`;
  }
  /* The Caldera in Ur-Mat's dream: a volcanic rim under a violet sky, lava frozen in mid-air,
     ripples spreading through the air and glyphs blooming among the stars. When he wakes
     ("lucid"), the sky cracks like glass and falls in shards. */
  function dreamScene(lucid) {
    const stars = Array.from({ length: 46 }, (_, i) => `<circle class="bi-um-star" style="--d:${((i * 0.29) % 4).toFixed(2)}s" cx="${(i * 263) % 1200}" cy="${(i * 97) % 330}" r="${0.8 + (i % 3) * 0.6}" fill="${i % 4 ? "#f4ecff" : "#c9b0ff"}"/>`).join("");
    const drops = Array.from({ length: 22 }, (_, i) => { const x = 160 + (i * 173) % 880, y = 300 + (i * 61) % 170, r = 3 + (i % 4) * 2; return `<g class="bi-um-drop" style="--d:${(i * 0.13).toFixed(2)}s"><ellipse cx="${x}" cy="${y}" rx="${r}" ry="${r * 1.4}" fill="#ff7a2a"/><ellipse cx="${x}" cy="${y}" rx="${r * 2.2}" ry="${r * 2.6}" fill="none" stroke="#c9b0ff" stroke-width="1.2" stroke-dasharray="3 4" opacity=".7"/></g>`; }).join("");
    // The bloom animates an inner group, so it cannot overwrite the glyph's placement.
    const glyph = (x, y, s, i) => `<g transform="translate(${x} ${y})"><g class="bi-um-glyph" style="--d:${(0.9 + i * 0.22).toFixed(2)}s"><circle r="${s}" fill="none" stroke="#c9b0ff" stroke-width="3"/><path d="M0 ${-s * 0.7} L${s * 0.45} 0 L0 ${s * 0.7} L${-s * 0.45} 0Z M${-s * 0.8} 0 L${s * 0.8} 0 M0 ${-s * 0.95} L0 ${-s * 1.25}" fill="none" stroke="#f4ecff" stroke-width="2.4"/></g></g>`;
    const glyphs = [[170, 230, 30], [330, 170, 22], [470, 250, 34], [740, 180, 26], [900, 250, 32], [1050, 190, 22], [250, 350, 18], [980, 360, 18]].map(([x, y, s], i) => glyph(x, y, s, i)).join("");
    const ripples = [0, 1, 2].map(i => `<ellipse class="bi-um-ripple" style="--d:${(0.6 + i * 1.1).toFixed(2)}s" cx="600" cy="300" rx="120" ry="50" fill="none" stroke="#c9b0ff" stroke-width="2.5"/>`).join("");
    const cracks = lucid ? `<path class="bi-um-crack" d="M600 40 L560 120 L610 170 L540 260 L590 330 M610 170 L700 150 L760 220 M560 120 L440 90 L380 160 M540 260 L420 300 M760 220 L880 200 L950 280 M700 150 L760 60" fill="none" stroke="#ffffff" stroke-width="4" stroke-linecap="round"/>` : "";
    const shards = lucid ? Array.from({ length: 16 }, (_, i) => `<path class="bi-um-shard" style="--dx:${((i % 5) - 2) * 40}px;--dy:${300 + (i % 4) * 60}px;--spin:${(i % 2 ? 1 : -1) * (90 + i * 17)}deg" d="M${400 + (i * 47) % 420} ${60 + (i * 31) % 240} l${18 + (i % 3) * 8} ${-10 - (i % 2) * 8} l${8 + (i % 4) * 4} ${26 + (i % 3) * 6}Z" fill="#2a1f45" stroke="#f4ecff" stroke-width="1.5"/>`).join("") : "";
    return `<svg class="bi-art" viewBox="0 0 1200 680" preserveAspectRatio="xMidYMid slice" xmlns="http://www.w3.org/2000/svg">
      <defs>
        <linearGradient id="bi-um-sky" x2="0" y2="1"><stop stop-color="#0a0616"/><stop offset=".5" stop-color="#21163c"/><stop offset="1" stop-color="#3a1a28"/></linearGradient>
        <radialGradient id="bi-um-glow"><stop stop-color="#f4ecff" stop-opacity=".55"/><stop offset=".35" stop-color="#b48cff" stop-opacity=".28"/><stop offset="1" stop-color="#21163c" stop-opacity="0"/></radialGradient>
        <radialGradient id="bi-um-lava"><stop stop-color="#ffb04a"/><stop offset=".5" stop-color="#ff5a2a"/><stop offset="1" stop-color="#5a1a10" stop-opacity="0"/></radialGradient>
      </defs>
      <rect width="1200" height="680" fill="url(#bi-um-sky)"/>
      <g>${stars}</g>
      ${mountain("#140c18", "#5a3a6a")}
      <ellipse class="bi-um-lavaglow" cx="600" cy="590" rx="600" ry="110" fill="url(#bi-um-lava)"/>
      <path fill="#1a0e12" d="M0 560 Q200 530 400 552 Q600 520 800 548 T1200 556 V680 H0Z"/>
      <path class="bi-um-lavaline" d="M260 600 Q420 580 600 596 T940 592" fill="none" stroke="#ff8a3d" stroke-width="6" stroke-linecap="round"/>
      <ellipse class="bi-um-halo" cx="600" cy="320" rx="420" ry="280" fill="url(#bi-um-glow)"/>
      <g>${ripples}</g>
      <g>${glyphs}</g>
      <g>${drops}</g>
      ${cracks}${shards}
    </svg>`;
  }
  /* The Mass Graves (Ool): a moonlit field of tilted grave-stones and old spears in green mist,
     soul-fire wisps rising from the earth. When the phylactery breaks ("shatter"), a great crystal
     in its bone cage cracks, bursts into shards, and his screaming soul tears out of it. */
  function graveScene(shatter) {
    const stone = (x, y, w, h, lean) => `<g transform="translate(${x} ${y}) rotate(${lean})"><path d="M${-w / 2} 0 L${-w / 2} ${-h + w / 2} Q0 ${-h - w * 0.3} ${w / 2} ${-h + w / 2} L${w / 2} 0Z"/><path d="M${-w * 0.18} ${-h * 0.62} h${w * 0.36} M0 ${-h * 0.78} v${h * 0.36}" stroke="#2a3a2c" stroke-width="3" fill="none"/></g>`;
    const stones = [[90, 600, 46, 92, -8], [240, 610, 38, 74, 12], [380, 596, 30, 58, -14], [830, 598, 32, 60, 10], [960, 612, 44, 86, -6], [1110, 602, 40, 78, 14]].map(args => stone(...args)).join("");
    const graves = [[190, 640], [420, 652], [600, 662], [780, 652], [1010, 640]].map(([x, y]) => `<ellipse cx="${x}" cy="${y}" rx="90" ry="18" fill="#090c08"/><path d="M${x - 80} ${y - 6} Q${x - 40} ${y - 26} ${x} ${y - 16} Q${x + 44} ${y - 28} ${x + 84} ${y - 4}" fill="none" stroke="#2e3a26" stroke-width="5"/>`).join("");
    const wisps = Array.from({ length: 30 }, (_, i) => `<path class="bi-oo-wisp" style="--d:${((i * 0.37) % 6).toFixed(2)}s;--drift:${((i % 7) - 3) * 14}px" d="M${(i * 157) % 1200} ${560 + (i * 31) % 110} q-5 -10 0 -22 q5 10 0 22Z" fill="${i % 3 ? "#7cff9a" : "#d8ffd8"}"/>`).join("");
    const shards = shatter ? Array.from({ length: 14 }, (_, i) => { const a = i / 14 * Math.PI * 2; return `<path class="bi-oo-shard" style="--dx:${Math.round(Math.cos(a) * (220 + (i % 3) * 90))}px;--dy:${Math.round(Math.sin(a) * (160 + (i % 4) * 50) - 60)}px;--spin:${(i % 2 ? 1 : -1) * (160 + i * 23)}deg" d="M420 ${330 + (i % 5) * 12} l${8 + (i % 3) * 4} ${-18 - (i % 4) * 5} l${6 + (i % 2) * 4} ${20 + (i % 3) * 3}Z" fill="#26343a" stroke="#7cff9a" stroke-width="1.5"/>`; }).join("") : "";
    // The breaking phylactery: a big crystal in its cage, cracks spreading, and the soul's face.
    const vessel = shatter ? `<g class="bi-oo-vessel">
        <path d="M352 560 L488 560 L470 520 L370 520Z" fill="#2c2e2a" stroke="#5a5e52" stroke-width="3"/>
        <g class="bi-oo-crystal"><path d="M420 200 L460 300 L455 440 L420 500 L385 440 L380 300Z" fill="#1c2628" stroke="#0a0d0c" stroke-width="5"/>
          <ellipse class="bi-oo-core" cx="420" cy="350" rx="24" ry="56" fill="#7cff9a"/>
          <path class="bi-oo-cracks" d="M418 230 L432 290 L410 340 L428 400 L416 470 M432 290 L456 310 M410 340 L384 352 M428 400 L452 420" fill="none" stroke="#e8ffe8" stroke-width="4" stroke-linecap="round"/></g>
        <path d="M370 520 C330 420 350 260 412 186 M470 520 C510 420 490 260 428 186 M392 520 C372 430 380 300 416 196 M448 520 C468 430 460 300 424 196" fill="none" stroke="#d8cfb8" stroke-width="7" stroke-linecap="round"/>
        <ellipse cx="420" cy="186" rx="22" ry="8" fill="none" stroke="#d8cfb8" stroke-width="6"/>
      </g>
      <g class="bi-oo-soul"><path d="M420 330 C350 300 330 200 372 130 C392 96 448 96 468 130 C510 200 490 300 420 330Z" fill="#7cff9a" opacity=".55"/>
        <path d="M392 170 q10 -14 20 0 q-10 12 -20 0Z M428 170 q10 -14 20 0 q-10 12 -20 0Z" fill="#06100a"/>
        <path d="M404 206 Q420 196 436 206 Q440 250 420 270 Q400 250 404 206Z" fill="#06100a"/>
        <path d="M380 300 Q400 380 372 460 M460 300 Q440 380 468 460 M420 330 Q424 420 410 500" fill="none" stroke="#7cff9a" stroke-width="10" stroke-linecap="round" opacity=".4"/></g>` : "";
    return `<svg class="bi-art" viewBox="0 0 1200 680" preserveAspectRatio="xMidYMid slice" xmlns="http://www.w3.org/2000/svg">
      <defs>
        <linearGradient id="bi-oo-sky" x2="0" y2="1"><stop stop-color="#020604"/><stop offset=".55" stop-color="#0c1610"/><stop offset="1" stop-color="#16241a"/></linearGradient>
        <radialGradient id="bi-oo-glow"><stop stop-color="#d8ffd8" stop-opacity=".6"/><stop offset=".35" stop-color="#7cff9a" stop-opacity=".28"/><stop offset="1" stop-color="#0c1610" stop-opacity="0"/></radialGradient>
        <radialGradient id="bi-oo-moon"><stop stop-color="#e8f0e0"/><stop offset=".6" stop-color="#b8c8b0"/><stop offset="1" stop-color="#b8c8b0" stop-opacity="0"/></radialGradient>
      </defs>
      <rect width="1200" height="680" fill="url(#bi-oo-sky)"/>
      <circle cx="${shatter ? 960 : 300}" cy="120" r="70" fill="url(#bi-oo-moon)" opacity=".75"/>
      <path class="bi-oo-cloud" d="M-100 150 Q100 110 300 150 T700 140 T1100 160 T1400 140 V200 H-100Z" fill="#0a120c" opacity=".85"/>
      ${mountain("#0a120c", "#2a3a2c")}
      ${battlefield("#0c140e", "#3a4a3a")}
      <ellipse class="bi-oo-halo" cx="${shatter ? 420 : 600}" cy="${shatter ? 360 : 470}" rx="440" ry="260" fill="url(#bi-oo-glow)"/>
      <g fill="#0e1610" stroke="#3a4a3a" stroke-width="2">${stones}</g>
      <path fill="#0b130d" d="M0 590 Q240 566 480 584 T900 576 T1200 590 V680 H0Z"/>
      ${graves}
      ${vessel}${shards}
      <g>${wisps}</g>
      <rect class="bi-oo-mist" x="-100" y="540" width="1400" height="140" fill="#9fd8a8" opacity=".12"/>
    </svg>`;
  }
  function rock(x, y, r, seed) {
    const points = Array.from({ length: 7 }, (_, i) => {
      const angle = i / 7 * Math.PI * 2 + seed, reach = r * (0.78 + 0.22 * Math.abs(Math.sin(seed * 3.1 + i * 1.7)));
      return `${(x + Math.cos(angle) * reach).toFixed(1)} ${(y + Math.sin(angle) * reach * 0.82).toFixed(1)}`;
    });
    return "M" + points.join(" L") + "Z";
  }
  const mountain = (fill, rim) => `<path fill="${fill}" stroke="${rim}" stroke-width="2.5" stroke-opacity=".45" d="M0 430 L120 360 L210 384 L330 250 L420 290 L520 140 L600 60 L690 150 L760 120 L860 250 L950 220 L1060 330 L1200 300 V680 H0Z"/><path fill="${fill}" opacity=".7" d="M0 470 L160 420 L300 452 L460 400 L600 430 L760 396 L920 446 L1060 410 L1200 440 V680 H0Z"/>`;
  function battlefield(fill, rim) {
    const spear = (x, y, turn, length, head) => `<g transform="translate(${x} ${y}) rotate(${turn})"><path d="M-3 0 L-3 ${-length + 6} L-5 ${-length} L-1 ${-length + 4} L1.5 ${-length - 4} L3 ${-length + 6} L3 0Z"/>${head ? `<path d="M-6 ${-length + 2} L0 ${-length - 22} L6 ${-length + 2}Z"/>` : ""}</g>`;
    return `<g fill="${fill}" stroke="${rim}" stroke-width="1.5" stroke-opacity=".5">
      ${spear(150, 560, -18, 120, true)}${spear(196, 566, 12, 90, false)}${spear(250, 552, -34, 70, false)}${spear(930, 556, 22, 110, true)}${spear(1010, 566, -10, 84, false)}${spear(1090, 552, 30, 64, false)}
      <path d="M318 560 L300 330" stroke="${fill}" stroke-width="7"/><path class="bi-ny-flag" d="M302 344 Q350 336 398 350 L380 380 L404 410 Q352 400 306 404Z" fill="#23304a" stroke="#6a5a30" stroke-width="2.5"/>
      <path d="M880 566 L872 520 Q890 500 910 520 L904 566Z"/><path d="M1044 560 L1036 530 Q1052 512 1070 530 L1064 560Z"/><path d="M1030 566 L1084 566 L1076 548 L1038 548Z"/>
    </g>`;
  }

  // Fills the cutscene element for one boss. look and impLook are battle sprite looks.
  // A phase cutscene ("before": true) also shows the boss's look from before the change.
  function render(host, key, { boss, imp, before } = {}) {
    const intro = INTROS[key];
    if (!intro || !host) return null;
    const sprite = look => {
      try { return look && window.Sprites ? window.Sprites.forActor(look).svg : ""; } catch { return ""; }
    };
    host.dataset.boss = key;
    host.style.setProperty("--bi-duration", intro.duration + "ms");
    host.querySelector(".boss-intro-scene").innerHTML = intro.scene()
      + (intro.before && before ? `<div class="bi-boss bi-boss-before">${sprite(before)}</div>` : "")
      + `<div class="bi-boss">${sprite(boss)}</div>`
      + Array.from({ length: imp ? intro.imps || 0 : 0 }, (_, index) => {
        // A retinue may be one kind of fighter or a list of different ones.
        const look = Array.isArray(imp) ? imp[index % imp.length] : imp;
        return `<div class="bi-imp bi-imp-${index + 1}">${sprite({ ...look, seed: "intro-imp-" + index })}</div>`;
      }).join("");
    host.querySelector("#boss-intro-location").textContent = intro.location;
    host.querySelector("#boss-intro-overture").textContent = intro.overture;
    host.querySelector("#boss-intro-label").textContent = intro.label;
    const title = host.querySelector("#boss-intro-title");
    title.textContent = intro.title[0];
    const em = document.createElement("em");
    em.textContent = intro.title[1] || "";
    title.append(em);
    host.querySelector("#boss-intro-description").textContent = intro.description;
    return intro;
  }

  window.BossIntros = { get: key => INTROS[key] || null, keys: () => Object.keys(INTROS), render };
})();
