/* Immutable geometry and metadata for regional maps beyond the Alder Vale. */
(function (root) {
  const quadratic = (first, control, second, count = 28) => Array.from({ length: count + 1 }, (_, index) => {
    const progress = index / count, inverse = 1 - progress;
    return {
      x: inverse * inverse * first.x + 2 * inverse * progress * control.x + progress * progress * second.x,
      y: inverse * inverse * first.y + 2 * inverse * progress * control.y + progress * progress * second.y,
    };
  });
  const cubic = (first, firstControl, secondControl, second, count = 42) => Array.from({ length: count + 1 }, (_, index) => {
    const progress = index / count, inverse = 1 - progress;
    return {
      x: inverse ** 3 * first.x + 3 * inverse * inverse * progress * firstControl.x + 3 * inverse * progress * progress * secondControl.x + progress ** 3 * second.x,
      y: inverse ** 3 * first.y + 3 * inverse * inverse * progress * firstControl.y + 3 * inverse * progress * progress * secondControl.y + progress ** 3 * second.y,
    };
  });
  const join = (...parts) => parts.flatMap((part, index) => index ? part.slice(1) : part);
  const stoneveilRoads = [
    join(quadratic({ x: 1120, y: 1020 }, { x: 1110, y: 960 }, { x: 1120, y: 900 }), quadratic({ x: 1120, y: 900 }, { x: 1090, y: 710 }, { x: 1040, y: 550 })),
    join(quadratic({ x: 1040, y: 550 }, { x: 800, y: 470 }, { x: 600, y: 250 }), quadratic({ x: 600, y: 250 }, { x: 530, y: 110 }, { x: 500, y: -20 })),
    quadratic({ x: 1040, y: 550 }, { x: 1310, y: 520 }, { x: 1620, y: 500 }),
  ];
  const river = join(
    cubic({ x: 850, y: 1030 }, { x: 920, y: 820 }, { x: 930, y: 690 }, { x: 840, y: 560 }),
    cubic({ x: 840, y: 560 }, { x: 750, y: 430 }, { x: 780, y: 300 }, { x: 650, y: 220 }),
    cubic({ x: 650, y: 220 }, { x: 560, y: 130 }, { x: 620, y: 70 }, { x: 580, y: -30 }),
  );
  root.WorldRegionData = {
    stoneveil: {
      id: "stoneveil-foothills",
      name: "Stoneveil Foothills",
      eyebrow: "STONEVEIL FOOTHILLS · LEVEL 10–20",
      title: "Where the plains <em>rise</em>",
      levelRange: { minimum: 10, maximum: 20 },
      start: { x: 1120, y: 948 },
      settlements: [
        { id: "crownspire", name: "Crownspire", kind: "Castle city", x: 1040, y: 550 },
        { id: "southwatch", name: "Southwatch", kind: "Town", x: 1120, y: 900 },
        { id: "stonefoot", name: "Stonefoot", kind: "Town", x: 600, y: 250 },
      ],
      roads: stoneveilRoads,
      river,
      hillAreas: [
        { x: 280, y: 580, rx: 300, ry: 250 },
        { x: 680, y: 680, rx: 280, ry: 205 },
        { x: 1380, y: 330, rx: 260, ry: 210 },
        { x: 1280, y: 620, rx: 270, ry: 180 },
      ],
      mountainfoot: { y: 315 },
      exits: [
        { id: "south-road", name: "Road to the Alder Vale", point: { x: 1120, y: 980 }, retreat: { x: 1120, y: 932 }, targetRegionId: "alder-vale", targetPoint: { x: 1120, y: 68 }, prompt: "Return south to the Alder Vale?", destinationName: "The Alder Vale" },
        { id: "mountain-road", name: "Road to the High Stoneveil", point: { x: 500, y: 20 }, retreat: { x: 510, y: 68 }, targetRegionId: "high-stoneveil", targetPoint: { x: 790, y: 948 }, prompt: "Climb north into the High Stoneveil?", destinationName: "The High Stoneveil" },
        { id: "east-road", name: "Road to the Sunward Coast", point: { x: 1580, y: 500 }, retreat: { x: 1532, y: 505 }, targetRegionId: "sunward-coast", targetPoint: { x: 68, y: 500 }, prompt: "Follow the eastern road to the Sunward Coast?", destinationName: "The Sunward Coast" },
      ],
    },
    highStoneveil: (() => {
      const road = join(
        cubic({ x: 790, y: 1020 }, { x: 870, y: 820 }, { x: 580, y: 700 }, { x: 740, y: 510 }),
        cubic({ x: 740, y: 510 }, { x: 930, y: 320 }, { x: 710, y: 200 }, { x: 820, y: -20 })
      );
      return {
        id: "high-stoneveil",
        name: "The High Stoneveil",
        eyebrow: "THE HIGH STONEVEIL · LEVEL 45–60",
        title: "Above the <em>cloud line</em>",
        description: "A snow-crowned wilderness of scree slopes, sheer cliffs and dragon-haunted summits. One narrow road climbs past the isolated town of Cairnrest.",
        levelRange: { minimum: 45, maximum: 60 },
        start: { x: 790, y: 948 },
        settlements: [{ id: "cairnrest", name: "Cairnrest", kind: "Town", guildServices: ["quest", "artifacts", "great-hunts", "recruit"], x: 740, y: 510 }],
        roads: [road], river: null,
        exits: [
          { id: "south-road", name: "Road to Stoneveil Foothills", point: { x: 790, y: 980 }, retreat: { x: 790, y: 932 }, targetRegionId: "stoneveil-foothills", targetPoint: { x: 510, y: 68 }, prompt: "Descend south to the Stoneveil Foothills?", destinationName: "Stoneveil Foothills" },
          { id: "north-road", name: "Road to Stoneveil Summit", point: { x: 820, y: 20 }, retreat: { x: 820, y: 68 }, targetRegionId: "stoneveil-summit", targetPoint: { x: 820, y: 948 }, prompt: "Climb to the Stoneveil Summit? Only ancient dragons and giants survive there.", destinationName: "Stoneveil Summit" },
        ],
      };
    })(),
    stoneveilSummit: (() => {
      const road = join(
        cubic({ x: 820, y: 1020 }, { x: 900, y: 845 }, { x: 635, y: 765 }, { x: 760, y: 610 }),
        cubic({ x: 760, y: 610 }, { x: 890, y: 485 }, { x: 700, y: 385 }, { x: 820, y: 285 })
      );
      return {
        id: "stoneveil-summit",
        name: "Stoneveil Summit",
        eyebrow: "STONEVEIL SUMMIT · LEVEL 85–95",
        title: "At the <em>roof of the world</em>",
        description: "The last road ends below a frozen crown of sheer rock and dragon eyries. Ancient dragons and giants alone endure this height; there are no settlements.",
        levelRange: { minimum: 85, maximum: 95 },
        start: { x: 820, y: 948 },
        safeRetreatRegionId: "high-stoneveil",
        settlements: [], roads: [road], river: null,
        exits: [
          { id: "south-road", name: "Road to the High Stoneveil", point: { x: 820, y: 980 }, retreat: { x: 820, y: 932 }, targetRegionId: "high-stoneveil", targetPoint: { x: 820, y: 68 }, prompt: "Descend to the High Stoneveil?", destinationName: "The High Stoneveil" },
        ],
      };
    })(),
    verdantReach: (() => {
      const greenwatch = { x: 1300, y: 520 };
      const heartgrove = { x: 820, y: 420 };
      const verdantRoads = [
        join(
          quadratic({ x: 1620, y: 500 }, { x: 1460, y: 485 }, greenwatch),
          cubic(greenwatch, { x: 1130, y: 500 }, { x: 995, y: 430 }, heartgrove)
        ),
        join(
          quadratic(greenwatch, { x: 1370, y: 680 }, { x: 1390, y: 800 }),
          quadratic({ x: 1390, y: 800 }, { x: 1360, y: 920 }, { x: 1330, y: 1020 })
        ),
      ];
      return {
        id: "verdant-reach",
        name: "The Verdant Reach",
        eyebrow: "THE VERDANT REACH · LEVEL 5–20",
        title: "Beneath the <em>elder boughs</em>",
        description: "An immense layered forest bordered by narrow plains to the east and south, with Greenwatch guarding the eastern road.",
        levelRange: { minimum: 5, maximum: 20 },
        start: { x: 1532, y: 500 },
        settlements: [
          { id: "greenwatch", name: "Greenwatch", kind: "Castle city", ...greenwatch },
          { id: "heartgrove", name: "Heartgrove", kind: "Town", ...heartgrove },
        ],
        roads: verdantRoads,
        river: null,
        forestLayers: {
          outer: { x: 585, y: 400, rx: 700, ry: 510 },
          forest: { x: 545, y: 365, rx: 500, ry: 350 },
          deep: { x: 475, y: 325, rx: 260, ry: 185 },
        },
        exits: [
          { id: "east-road", name: "Road to the Alder Vale", point: { x: 1580, y: 500 }, retreat: { x: 1532, y: 500 }, targetRegionId: "alder-vale", targetPoint: { x: 68, y: 844 }, prompt: "Return east to the Alder Vale?", destinationName: "The Alder Vale" },
          { id: "south-road", name: "Road to Stormwake Hills", point: { x: 1330, y: 980 }, retreat: { x: 1340, y: 932 }, targetRegionId: "stormwake-hills", targetPoint: { x: 850, y: 68 }, prompt: "Travel south into the Stormwake Hills?", destinationName: "Stormwake Hills" },
        ],
      };
    })(),
    stormwakeHills: (() => {
      const stormhold = { x: 800, y: 390 }, rainford = { x: 1150, y: 620 }, brackenrise = { x: 350, y: 700 };
      return {
        id: "stormwake-hills", name: "Stormwake Hills",
        eyebrow: "STORMWAKE HILLS · LEVEL 15–35", title: "Under an <em>unbroken storm</em>",
        description: "Rain never leaves these rolling hills. After dusk, lightning stalks the ridges above Stormhold, Rainford and Brackenrise.",
        levelRange: { minimum: 15, maximum: 35 }, start: { x: 850, y: 68 }, weather: "rainstorm",
        settlements: [
          { id: "stormhold", name: "Stormhold", kind: "Castle city", ...stormhold },
          { id: "rainford", name: "Rainford", kind: "Town", ...rainford },
          { id: "brackenrise", name: "Brackenrise", kind: "Town", ...brackenrise },
        ],
        roads: [
          join(quadratic({ x: 850, y: -20 }, { x: 840, y: 180 }, stormhold), cubic(stormhold, { x: 900, y: 480 }, { x: 1030, y: 550 }, rainford), quadratic(rainford, { x: 1010, y: 840 }, { x: 850, y: 1020 })),
          join(quadratic(stormhold, { x: 570, y: 475 }, brackenrise), quadratic(brackenrise, { x: 165, y: 670 }, { x: -20, y: 600 })),
        ],
        river: null,
        hillAreas: [{ x: 390, y: 360, rx: 335, ry: 250 }, { x: 510, y: 750, rx: 295, ry: 180 }, { x: 1370, y: 310, rx: 190, ry: 250 }],
        stormcrest: { x: 380, y: 275, rx: 235, ry: 175 },
        sites: [{ id: "church-of-talos", name: "Church of Talos", kind: "Church", x: 380, y: 275, radius: 38, servicePriceSilver: 12000 }],
        exits: [
          { id: "north-road", name: "Road to the Verdant Reach", point: { x: 850, y: 20 }, retreat: { x: 850, y: 68 }, targetRegionId: "verdant-reach", targetPoint: { x: 1340, y: 932 }, prompt: "Return north to the Verdant Reach?", destinationName: "The Verdant Reach" },
          { id: "south-road", name: "Road to Mirrormere Basin", point: { x: 850, y: 980 }, retreat: { x: 850, y: 932 }, targetRegionId: "mirrormere-basin", targetPoint: { x: 820, y: 68 }, prompt: "Travel south to Mirrormere Basin?", destinationName: "Mirrormere Basin" },
          { id: "west-road", name: "Road to Gloamfen Marsh", point: { x: 20, y: 600 }, retreat: { x: 68, y: 630 }, targetRegionId: "gloamfen-marsh", targetPoint: { x: 1532, y: 600 }, prompt: "Travel west into Gloamfen Marsh?", destinationName: "Gloamfen Marsh" },
        ],
      };
    })(),
    gloamfenMarsh: (() => {
      const mirewatch = { x: 670, y: 355 }, reedcross = { x: 765, y: 770 }, fork = { x: 1100, y: 570 };
      const northRoad = join(
        cubic({ x: 1620, y: 600 }, { x: 1450, y: 625 }, { x: 1250, y: 565 }, fork),
        cubic(fork, { x: 920, y: 495 }, { x: 820, y: 385 }, mirewatch),
        cubic(mirewatch, { x: 580, y: 270 }, { x: 705, y: 135 }, { x: 640, y: -20 })
      );
      const southRoad = cubic(fork, { x: 965, y: 640 }, { x: 865, y: 740 }, reedcross);
      return {
        id: "gloamfen-marsh", name: "Gloamfen Marsh",
        eyebrow: "GLOAMFEN MARSH · LEVEL 35–50", title: "Across the <em>drowned earth</em>",
        description: "A vast peat swamp threaded by two raised roads. Mirewatch guards the northern causeway, Reedcross survives on southern hummocks, and a small rocky rise breaks the wetlands to the west.",
        levelRange: { minimum: 35, maximum: 50 }, start: { x: 1532, y: 600 },
        settlements: [
          { id: "mirewatch", name: "Mirewatch", kind: "Castle city", ...mirewatch },
          { id: "reedcross", name: "Reedcross", kind: "Town", ...reedcross },
        ],
        roads: [northRoad, southRoad], river: null,
        rockyRise: { x: 310, y: 555, rx: 245, ry: 215 },
        deepMire: { x: 790, y: 580, rx: 315, ry: 270 },
        reedwater: { x: 1310, y: 245, rx: 250, ry: 175 },
        exits: [
          { id: "east-road", name: "Road to Stormwake Hills", point: { x: 1580, y: 600 }, retreat: { x: 1532, y: 600 }, targetRegionId: "stormwake-hills", targetPoint: { x: 68, y: 630 }, prompt: "Return east to Stormwake Hills?", destinationName: "Stormwake Hills" },
          { id: "north-road", name: "Road to Verdigris Jungle", point: { x: 640, y: 20 }, retreat: { x: 650, y: 68 }, targetRegionId: "verdigris-jungle", targetPoint: { x: 840, y: 932 }, prompt: "Travel north into Verdigris Jungle?", destinationName: "Verdigris Jungle" },
        ],
      };
    })(),
    verdigrisJungle: (() => {
      const canopyhold = { x: 790, y: 405 };
      return {
        id: "verdigris-jungle", name: "Verdigris Jungle",
        eyebrow: "VERDIGRIS JUNGLE · LEVEL 45–65", title: "Beneath an <em>endless canopy</em>",
        description: "An unbroken jungle of tangled roots, flooded shadows and ancient trees. One hard-won road reaches Canopyhold, the sole castle city beneath its leaves.",
        levelRange: { minimum: 45, maximum: 65 }, start: { x: 840, y: 932 },
        settlements: [{ id: "canopyhold", name: "Canopyhold", kind: "Castle city", ...canopyhold }],
        roads: [join(
          cubic({ x: 840, y: 1020 }, { x: 900, y: 830 }, { x: 680, y: 690 }, { x: 805, y: 560 }),
          quadratic({ x: 805, y: 560 }, { x: 880, y: 475 }, canopyhold)
        )], river: null,
        outerCanopy: { x: 795, y: 495, rx: 780, ry: 490 },
        denseCanopy: { x: 660, y: 380, rx: 565, ry: 365 },
        oldGrowth: { x: 475, y: 290, rx: 300, ry: 220 },
        highCanopy: { x: 1300, y: 300, rx: 245, ry: 195 },
        exits: [
          { id: "south-road", name: "Road to Gloamfen Marsh", point: { x: 840, y: 980 }, retreat: { x: 840, y: 932 }, targetRegionId: "gloamfen-marsh", targetPoint: { x: 650, y: 68 }, prompt: "Return south to Gloamfen Marsh?", destinationName: "Gloamfen Marsh" },
        ],
      };
    })(),
    mirrormereBasin: (() => {
      const lake = { x: 740, y: 420, rx: 480, ry: 310 };
      const ring = Array.from({ length: 49 }, (_, index) => ({ x: lake.x + 510 * Math.cos(index * Math.PI / 24), y: lake.y + 360 * Math.sin(index * Math.PI / 24) }));
      const lakespire = { x: 1285, y: 470 };
      return {
        id: "mirrormere-basin", name: "Mirrormere Basin",
        eyebrow: "MIRRORMERE BASIN · LEVEL 20–40", title: "Between <em>water and sand</em>",
        description: "A vast freshwater lake fills the basin. Reedbeds and stony bluffs give way to dry steppe and the first dunes of the southern desert.",
        levelRange: { minimum: 20, maximum: 40 }, start: { x: 820, y: 68 }, lake,
        settlements: [{ id: "lakespire", name: "Lakespire", kind: "Castle city", ...lakespire }],
        roads: [ring,
          quadratic({ x: 820, y: -20 }, { x: 800, y: 20 }, { x: 740, y: 60 }),
          quadratic({ x: 1250, y: 420 }, { x: 1300, y: 415 }, lakespire),
          quadratic({ x: 740, y: 780 }, { x: 870, y: 900 }, { x: 850, y: 1020 })],
        river: null, marsh: { x: 1290, y: 700, rx: 240, ry: 120 },
        exits: [
          { id: "north-road", name: "Road to Stormwake Hills", point: { x: 820, y: 20 }, retreat: { x: 820, y: 68 }, targetRegionId: "stormwake-hills", targetPoint: { x: 850, y: 932 }, prompt: "Return north to the Stormwake Hills?", destinationName: "Stormwake Hills" },
          { id: "south-road", name: "Road to the Saffron Expanse", point: { x: 850, y: 980 }, retreat: { x: 850, y: 932 }, targetRegionId: "saffron-expanse", targetPoint: { x: 821, y: 68 }, prompt: "Travel south into the Saffron Expanse?", destinationName: "Saffron Expanse" },
        ],
      };
    })(),
    saffronExpanse: (() => {
      const oasis = { x: 425, y: 355, rx: 210, ry: 140 };
      const miragehold = { x: 625, y: 390 }, dunecross = { x: 1050, y: 585 };
      const northRoad = join(quadratic({ x: 850, y: -20 }, { x: 780, y: 225 }, miragehold), cubic(miragehold, { x: 760, y: 455 }, { x: 905, y: 530 }, dunecross));
      return {
        id: "saffron-expanse", name: "Saffron Expanse",
        eyebrow: "SAFFRON EXPANSE · LEVEL 35–50", title: "Beyond the <em>last water</em>",
        description: "A vast sea of sun-baked sand. Miragehold guards a small oasis; Dunecross survives among the open dunes. The far southeast turns to glass beneath the sun.",
        levelRange: { minimum: 35, maximum: 50 }, start: { x: 821, y: 68 }, oasis,
        saltPans: { x: 1260, y: 295, rx: 260, ry: 170 }, glassDunes: { x: 1340, y: 755, rx: 250, ry: 190 },
        settlements: [
          { id: "miragehold", name: "Miragehold", kind: "Castle city", ...miragehold },
          { id: "dunecross", name: "Dunecross", kind: "Town", ...dunecross },
        ],
        roads: [northRoad,
          quadratic(dunecross, { x: 820, y: 805 }, { x: 850, y: 1020 }),
          quadratic(dunecross, { x: 1320, y: 780 }, { x: 1620, y: 820 })],
        river: null,
        exits: [
          { id: "north-road", name: "Road to Mirrormere Basin", point: { x: 850, y: 20 }, retreat: { x: 821, y: 68 }, targetRegionId: "mirrormere-basin", targetPoint: { x: 850, y: 932 }, prompt: "Return north to Mirrormere Basin?", destinationName: "Mirrormere Basin" },
          { id: "south-road", name: "Road to the Crimson Divide", point: { x: 850, y: 980 }, retreat: { x: 850, y: 932 }, targetRegionId: "crimson-divide", targetPoint: { x: 855, y: 68 }, prompt: "Travel south to the Crimson Divide?", destinationName: "Crimson Divide" },
          { id: "southeast-road", name: "Road to the Deep Desert", point: { x: 1580, y: 820 }, retreat: { x: 1532, y: 800 }, targetRegionId: "deep-desert", targetPoint: { x: 68, y: 210 }, prompt: "Travel southeast into the Deep Desert?", destinationName: "Deep Desert" },
        ],
      };
    })(),
    deepDesert: (() => {
      const lastlight = { x: 330, y: 270 };
      return {
        id: "deep-desert", name: "Deep Desert", eyebrow: "DEEP DESERT · LEVEL 45–55",
        title: "Where the sand <em>remembers</em>",
        description: "Beyond the last caravan outpost lies an immense silent waste. Wind-carved ruins circle an ancient pyramid at its heart.",
        levelRange: { minimum: 45, maximum: 55 }, start: { x: 68, y: 210 },
        pyramid: { x: 820, y: 500, rx: 175, ry: 145 }, necropolis: { x: 820, y: 500, rx: 315, ry: 250 },
        obsidianFlats: { x: 1320, y: 685, rx: 265, ry: 240 },
        settlements: [{ id: "lastlight", name: "Lastlight", kind: "Town", guildServices: ["quest", "artifacts", "great-hunts", "recruit"], ...lastlight }],
        roads: [quadratic({ x: -20, y: 210 }, { x: 135, y: 230 }, lastlight)], river: null,
        exits: [
          { id: "northwest-road", name: "Road to the Saffron Expanse", point: { x: 20, y: 210 }, retreat: { x: 68, y: 210 }, targetRegionId: "saffron-expanse", targetPoint: { x: 1532, y: 800 }, prompt: "Return northwest to the Saffron Expanse?", destinationName: "Saffron Expanse" },
        ],
      };
    })(),
    crimsonDivide: (() => {
      const amberpost = { x: 810, y: 315 };
      return {
        id: "crimson-divide", name: "Crimson Divide", eyebrow: "CRIMSON DIVIDE · LEVEL 40–55",
        title: "Where the sand <em>turns red</em>",
        description: "Golden dunes give way to blood-red sand. Amberpost is the last refuge on a single road running south through vampire-haunted wastes.",
        levelRange: { minimum: 40, maximum: 55 }, start: { x: 855, y: 68 },
        crimsonBoundary: 500,
        escarpment: { x: 1270, y: 345, rx: 250, ry: 150 },
        bloodglass: { x: 350, y: 745, rx: 260, ry: 185 },
        settlements: [{ id: "amberpost", name: "Amberpost", kind: "Town", guildServices: ["quest", "artifacts", "great-hunts", "recruit"], ...amberpost }],
        roads: [join(quadratic({ x: 850, y: -20 }, { x: 880, y: 130 }, amberpost), quadratic(amberpost, { x: 840, y: 650 }, { x: 820, y: 1020 }))],
        river: null,
        exits: [
          { id: "north-road", name: "Road to the Saffron Expanse", point: { x: 850, y: 20 }, retreat: { x: 855, y: 68 }, targetRegionId: "saffron-expanse", targetPoint: { x: 850, y: 932 }, prompt: "Return north to the Saffron Expanse?", destinationName: "Saffron Expanse" },
          { id: "south-road", name: "Road to Bloodstone Coast", point: { x: 820, y: 980 }, retreat: { x: 820, y: 932 }, targetRegionId: "bloodstone-coast", targetPoint: { x: 820, y: 68 }, prompt: "Continue south to the Bloodstone Coast?", destinationName: "Bloodstone Coast" },
        ],
      };
    })(),
    bloodstoneCoast: (() => {
      const nocturneKeep = { x: 1240, y: 794 };
      return {
        id: "bloodstone-coast", name: "Bloodstone Coast", eyebrow: "BLOODSTONE COAST · LEVEL 55–70",
        title: "Where the red sands meet <em>the blood tide</em>",
        description: "A crimson desert studded with giant stone spires slopes to a dark coast. Nocturne Keep admits only those bearing the Vampire class; outsiders find a hostile fortress instead.",
        levelRange: { minimum: 55, maximum: 70 }, start: { x: 820, y: 68 }, safeRetreatRegionId: "crimson-divide",
        landShape: { type: "south-coast", shoreY: 850 },
        rockFields: [{ x: 370, y: 310, rx: 260, ry: 210 }, { x: 1080, y: 385, rx: 305, ry: 245 }],
        bloodShore: { x: 840, y: 725, rx: 590, ry: 125 },
        settlements: [{ id: "nocturne-keep", name: "Nocturne Keep", kind: "Castle city", requiresClass: "vampire", ...nocturneKeep }],
        roads: [join(quadratic({ x: 820, y: -20 }, { x: 855, y: 240 }, { x: 685, y: 385 }), quadratic({ x: 685, y: 385 }, { x: 400, y: 520 }, { x: -20, y: 565 }))],
        river: null,
        exits: [
          { id: "north-road", name: "Road to the Crimson Divide", point: { x: 820, y: 20 }, retreat: { x: 820, y: 68 }, targetRegionId: "crimson-divide", targetPoint: { x: 820, y: 932 }, prompt: "Return north to the Crimson Divide?", destinationName: "Crimson Divide" },
          { id: "west-road", name: "Road to the Ancient Battlefield", point: { x: 20, y: 565 }, retreat: { x: 68, y: 560 }, targetRegionId: "ancient-battlefield", targetPoint: { x: 1532, y: 565 }, prompt: "Follow the blood road west to the Ancient Battlefield?", destinationName: "Ancient Battlefield" },
        ],
      };
    })(),
    ancientBattlefield: (() => {
      // The four surviving stretches deliberately do not touch. The route
      // planner must cross ruined gaps off-road instead of granting road speed.
      const roads = [
        quadratic({ x: 1620, y: 565 }, { x: 1430, y: 550 }, { x: 1220, y: 540 }),
        quadratic({ x: 1085, y: 530 }, { x: 925, y: 490 }, { x: 780, y: 480 }),
        quadratic({ x: 635, y: 485 }, { x: 480, y: 495 }, { x: 350, y: 500 }),
        quadratic({ x: 245, y: 500 }, { x: 115, y: 500 }, { x: -20, y: 500 }),
      ];
      return {
        id: "ancient-battlefield", name: "Ancient Battlefield", eyebrow: "ANCIENT BATTLEFIELD · LEVEL 65–75",
        title: "Where the war <em>never ended</em>",
        description: "Broken roadstones cut through a vast field of rusted weapons, collapsed trenches and mass graves. Scavengers, undead, vampires and renegade vampire hunters fight over what remains.",
        levelRange: { minimum: 65, maximum: 75 }, start: { x: 1532, y: 565 }, safeRetreatRegionId: "bloodstone-coast",
        bonePits: { x: 1110, y: 255, rx: 250, ry: 180 }, massGraves: { x: 580, y: 260, rx: 305, ry: 200 },
        trenches: { x: 1040, y: 760, rx: 370, ry: 160 },
        settlements: [], roads, river: null,
        exits: [
          { id: "east-road", name: "Road to Bloodstone Coast", point: { x: 1580, y: 565 }, retreat: { x: 1532, y: 565 }, targetRegionId: "bloodstone-coast", targetPoint: { x: 68, y: 560 }, prompt: "Return east to the Bloodstone Coast?", destinationName: "Bloodstone Coast" },
          { id: "west-road", name: "Road to Duskpine Forest", point: { x: 20, y: 500 }, retreat: { x: 68, y: 500 }, targetRegionId: "duskpine-forest", targetPoint: { x: 1532, y: 500 }, prompt: "Continue west into Duskpine Forest?", destinationName: "Duskpine Forest" },
        ],
      };
    })(),
    duskpineForest: (() => {
      const ravenwatch = { x: 815, y: 505 };
      return {
        id: "duskpine-forest", name: "Duskpine Forest", eyebrow: "DUSKPINE FOREST · LEVEL 60–75",
        title: "Beneath the <em>black pines</em>",
        description: "Cold fog hangs among ancient firs and black pines. Ravenwatch is the only castle city, reached by the road that survives the battlefield.",
        levelRange: { minimum: 60, maximum: 75 }, start: { x: 1532, y: 500 },
        deepBoreal: { x: 620, y: 300, rx: 340, ry: 245 },
        frostBog: { x: 315, y: 765, rx: 260, ry: 170 },
        shadowHollow: { x: 1240, y: 750, rx: 250, ry: 175 },
        settlements: [{ id: "ravenwatch", name: "Ravenwatch", kind: "Castle city", ...ravenwatch }],
        roads: [quadratic({ x: 1620, y: 500 }, { x: 1200, y: 480 }, ravenwatch)], river: null,
        exits: [{ id: "east-road", name: "Road to the Ancient Battlefield", point: { x: 1580, y: 500 }, retreat: { x: 1532, y: 500 }, targetRegionId: "ancient-battlefield", targetPoint: { x: 68, y: 500 }, prompt: "Return east to the Ancient Battlefield?", destinationName: "Ancient Battlefield" }],
      };
    })(),
    sunwardCoast: (() => {
      const westport = { x: 560, y: 500 }, dunehaven = { x: 850, y: 230 }, seabridge = { x: 1270, y: 690 };
      const coastRoads = [
        join(quadratic({ x: -20, y: 500 }, { x: 270, y: 465 }, westport), cubic(westport, { x: 780, y: 540 }, { x: 1000, y: 675 }, seabridge)),
        quadratic(westport, { x: 675, y: 300 }, dunehaven),
      ];
      return {
        id: "sunward-coast", name: "The Sunward Coast",
        eyebrow: "THE SUNWARD COAST · LEVEL 15–30", title: "Where the grass meets <em>the sea</em>",
        description: "Wind-brushed plains give way to salt marsh, dunes and a long beach. Seabridge's boats cross to Cinder Isle.",
        levelRange: { minimum: 15, maximum: 30 }, start: { x: 68, y: 500 },
        landShape: { type: "coast", shoreX: 1300 },
        settlements: [
          { id: "westport", name: "Westport", kind: "Castle city", ...westport },
          { id: "dunehaven", name: "Dunehaven", kind: "Town", ...dunehaven },
          { id: "seabridge", name: "Seabridge", kind: "Town", ...seabridge, boat: { regionId: "cinder-isle", settlementId: "emberhaven", minutes: 540 } },
        ],
        roads: coastRoads, river: null,
        saltMarsh: { x: 930, y: 825, rx: 235, ry: 145 },
        exits: [{ id: "west-road", name: "Road to Stoneveil Foothills", point: { x: 20, y: 500 }, retreat: { x: 68, y: 500 }, targetRegionId: "stoneveil-foothills", targetPoint: { x: 1532, y: 505 }, prompt: "Return west to the Stoneveil Foothills?", destinationName: "Stoneveil Foothills" }],
      };
    })(),
    cinderIsle: (() => {
      const emberhaven = { x: 135, y: 500 };
      return {
        id: "cinder-isle", name: "Cinder Isle", eyebrow: "CINDER ISLE · LEVEL 25–40",
        title: "Under the <em>smoking crown</em>",
        description: "A volcanic island of black sand, ember scrub and lava-scarred heights. Emberhaven is its only refuge.",
        levelRange: { minimum: 25, maximum: 40 }, start: { ...emberhaven },
        landShape: { type: "ellipse", x: 800, y: 500, rx: 700, ry: 445 },
        volcano: { x: 1030, y: 455, rx: 325, ry: 285 },
        settlements: [{ id: "emberhaven", name: "Emberhaven", kind: "Town", guildServices: ["quest", "artifacts", "great-hunts", "recruit"], ...emberhaven, boat: { regionId: "sunward-coast", settlementId: "seabridge", minutes: 540 } }],
        roads: [join(quadratic(emberhaven, { x: 450, y: 640 }, { x: 630, y: 650 }), quadratic({ x: 630, y: 650 }, { x: 740, y: 670 }, { x: 825, y: 630 }))],
        river: null, exits: [],
      };
    })(),
  };
  if (typeof module !== "undefined" && module.exports) module.exports = root.WorldRegionData;
})(globalThis);
