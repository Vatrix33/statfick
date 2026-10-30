/* The regional map uses the same saved clock and resources as the character sheet. */
(() => {
  const $ = id => document.getElementById(id);
  const rules = WorldRules, modal = $("map-modal"), encounterModal = $("map-encounter-modal"), transitionModal = $("map-region-transition-modal"), svg = $("regional-map");
  rules.setSettlementAccessResolver(classId => toInteger(classLevels[classId], 0) > 0);
  let travelling = false, encounterResolving = false, frame = null, lastFrame = 0, lastSave = 0, lastFullRender = 0;
  let mapHold = null, mapHoldTimer = null, mapSteeringTimer = null, suppressMapClickUntil = 0;
  const travelBonuses = () => { const effects = calculateClassEffects(); return { all: effects.travelSpeedFlat, river: effects.riverSpeedFlat }; };
  let renderedRegionId = null;
  let lastClockMinute = null, lastClockRegionId = null;
  let ambientEnabled = true, ambientLast = performance.now(), ambientCarry = 0;
  const mapShortcuts = [
    ["map-speed-half", "⇧1", ["shift+1"]], ["map-speed-normal", "⇧2", ["shift+2"]],
    ["map-speed-fast", "⇧3", ["shift+3"]], ["map-speed-double", "⇧4", ["shift+4"]],
    ["map-pause", "Space", [" "]], ["map-cancel", "X", ["x"]],
    ["map-force-encounter", "E", ["e"]], ["map-camp", "C", ["c"]], ["map-rest", "R", ["r"]],
    ["map-blacksmith", "B", ["b"]], ["map-item-shop", "G", ["g"]],
    ["map-guild", "A", ["a"]], ["map-church", "T", ["t"]], ["map-magic-tower", "W", ["w"]],
    ["map-class-respec", "L", ["l"]], ["map-stat-respec", "P", ["p"]],
    ["map-boat", "V", ["v"]], ["map-search", "F", ["f"]], ["map-track-quarry", "Q", ["q"]],
    ["map-zoom-in", "+", ["+", "="]], ["map-zoom-out", "−", ["-"]], ["map-fit", "0", ["0"]], ["map-follow", "Y", ["y"]],
  ];
  function labelShortcut(button, hint, ariaKey = hint) {
    if (!button) return;
    if (button.dataset.mapHotkey !== hint) button.dataset.mapHotkey = hint;
    if (button.getAttribute("aria-keyshortcuts") !== ariaKey) button.setAttribute("aria-keyshortcuts", ariaKey);
    const title = button.title.replace(/ · Shortcut: .*$/, "");
    const labeledTitle = `${title || button.getAttribute("aria-label") || button.textContent.trim()} · Shortcut: ${hint.startsWith("⇧") ? "Shift+" + hint.slice(1) : hint}`;
    if (button.title !== labeledTitle) button.title = labeledTitle;
  }
  function renderShortcuts() {
    for (const [id, hint] of mapShortcuts) labelShortcut($(id), hint, hint.startsWith("⇧") ? "Shift+" + hint.slice(1) : hint === "−" ? "-" : hint === "+" ? "=" : hint);
    labelShortcut(modal.querySelector(".modal-close"), "M", "M Escape");
    labelShortcut($("map-open-button"), "M");
    labelShortcut($("map-encounter-fight"), "F"); labelShortcut($("map-encounter-run"), "R");
    labelShortcut($("map-transition-travel"), "Enter"); labelShortcut($("map-transition-stay"), "Esc", "Escape Backspace");
  }
  const forests = rules.forests;
  const rainStreaks = (count, offset) => Array.from({ length: count }, (_, index) => {
    const x = (index * 977 + offset * 283) % 1640 - 20;
    const y = (index * 619 + offset * 173) % 1060 - 30;
    return `<path d="M${x} ${y}l-${offset ? 18 : 12} ${offset ? 60 : 42}"/>`;
  }).join("");
  /* Inked settlements: dark timber and stone, shadowed on the side away from the
     light, with a warm glint in the windows. */
  const ink = MapInk.INK;
  const merlons = (x1, x2, y, step = 8, height = 6) => {
    let d = `M${x1} ${y}`;
    for (let x = x1; x < x2; x += step) d += `V${y - height}H${Math.min(x2, x + step / 2)}V${y}H${Math.min(x2, x + step)}`;
    return d;
  };
  const shade = (x, y, width, height) => `<rect x="${x}" y="${y}" width="${width}" height="${height}" fill="url(#ink-hatch)" opacity=".55"/><rect x="${x}" y="${y}" width="${width}" height="${height}" fill="${ink}" opacity=".28"/>`;
  const window_ = (x, y, width = 3.4, height = 6) => `<rect x="${x}" y="${y}" width="${width}" height="${height}" fill="#d29a45" data-vivid=""/>`;
  function castleArt() {
    const tower = (x, top) => `<path d="M${x - 10} 12V${top}H${x + 10}V12Z" fill="#8a7f6c" stroke="${ink}" stroke-width="2.6"/>${shade(x - 10, top, 8, 12 - top)}<path d="M${x - 13} ${top}L${x} ${top - 26}L${x + 13} ${top}Z" fill="#3b2a24" stroke="${ink}" stroke-width="2.4" stroke-linejoin="round"/><path d="M${x - 13} ${top}L${x} ${top - 26}V${top}Z" fill="${ink}" opacity=".4"/>${window_(x - 1.5, top + 10, 3, 7)}`;
    return `<ellipse cy="13" rx="72" ry="15" fill="${ink}" opacity=".38"/>
      <path d="${merlons(-50, 50, -22)}V12H-50Z" fill="#7d7363" stroke="${ink}" stroke-width="2.6" stroke-linejoin="round"/>${shade(-50, -22, 34, 34)}
      ${tower(-56, -44)}${tower(56, -44)}
      <path d="${merlons(-24, 24, -58, 8, 7)}V12H-24Z" fill="#958a75" stroke="${ink}" stroke-width="2.6" stroke-linejoin="round"/>${shade(-24, -58, 18, 70)}
      <path d="M-14 -58L0 -86L14 -58Z" fill="#3b2a24" stroke="${ink}" stroke-width="2.4" stroke-linejoin="round"/><path d="M0 -86V-104l20 6-20 7" fill="#6e2a24" stroke="${ink}" stroke-width="2"/>
      <path d="M-11 12V-4a11 11 0 0122 0v16Z" fill="#20160f" stroke="${ink}" stroke-width="2"/>
      ${window_(-14, -44)}${window_(10.6, -44)}${window_(-2, -30, 4, 8)}
      <path d="M-50 -8H-24M24 -8H50M-24 -34H24" stroke="${ink}" stroke-width="1" opacity=".55"/>`;
  }
  function villageArt() {
    const house = (x, width, height, roof) => {
      const left = x - width / 2, top = 12 - height;
      return `<path d="M${left} 12V${top}H${left + width}V12Z" fill="#8c765a" stroke="${ink}" stroke-width="2.2"/>${shade(left, top, width * 0.38, height)}
        <path d="M${left - 3} ${top}L${x} ${top - roof}L${left + width + 3} ${top}Z" fill="#3e2b22" stroke="${ink}" stroke-width="2.2" stroke-linejoin="round"/><path d="M${left - 3} ${top}L${x} ${top - roof}V${top}Z" fill="${ink}" opacity=".4"/>
        ${window_(x + width * 0.12, top + height * 0.3)}<path d="M${x - width * 0.2} 12v-8h${width * 0.18}v8" fill="#20160f"/>`;
    };
    return `<ellipse cy="13" rx="48" ry="12" fill="${ink}" opacity=".36"/>
      <path d="M8 -26v-14h6v10" fill="#4a3a30" stroke="${ink}" stroke-width="1.6"/>
      ${house(-27, 22, 22, 16)}${house(25, 19, 18, 13)}${house(-1, 28, 30, 21)}`;
  }

  /* ---------------- The map view ----------------
     The land is painted by MapTerrain into tiles and composited on a canvas; the SVG on top
     carries everything interactive (settlements, quest markers, the route and you). Its
     viewBox is the camera, so clicks map to the land at any zoom. Markers and lettering keep
     a steady size on screen while the land grows and shrinks beneath them. */
  const terrainCanvas = $("map-terrain"), terrainContext = terrainCanvas.getContext("2d");
  const camera = { x: 800, y: 500, scale: 1, follow: true };
  const MAX_ZOOM = 10, DEFAULT_ZOOM = 3;
  let viewSize = { width: 1, height: 1, dpr: 1 }, terrainWorld = null, paintQueued = false, drawQueued = false;
  const fitScale = () => Math.min(viewSize.width / MapTerrain.W, viewSize.height / MapTerrain.H);
  const currentZoom = () => camera.scale / fitScale();
  function clampCamera() {
    const fit = fitScale();
    camera.scale = Math.max(fit, Math.min(fit * MAX_ZOOM, camera.scale));
    const halfW = viewSize.width / 2 / camera.scale, halfH = viewSize.height / 2 / camera.scale;
    camera.x = halfW * 2 >= MapTerrain.W ? MapTerrain.W / 2 : Math.max(halfW, Math.min(MapTerrain.W - halfW, camera.x));
    camera.y = halfH * 2 >= MapTerrain.H ? MapTerrain.H / 2 : Math.max(halfH, Math.min(MapTerrain.H - halfH, camera.y));
  }
  // Sizes for things that should look the same at every zoom.
  const pinScale = (size = 0.8) => size / camera.scale;
  const pin = (x, y, size) => `translate(${x} ${y}) scale(${pinScale(size).toFixed(4)})`;
  function applyCamera() {
    clampCamera();
    const w = viewSize.width / camera.scale, h = viewSize.height / camera.scale;
    svg.setAttribute("viewBox", `${(camera.x - w / 2).toFixed(2)} ${(camera.y - h / 2).toFixed(2)} ${w.toFixed(2)} ${h.toFixed(2)}`);
    const zoom = currentZoom();
    svg.classList.toggle("is-close", camera.scale >= 2.1);
    svg.classList.toggle("is-overview", zoom <= 1.35);
    for (const node of svg.querySelectorAll("[data-pin]")) node.setAttribute("transform", pin(Number(node.dataset.x), Number(node.dataset.y), Number(node.dataset.pin)));
    const character = $("map-character");
    if (character) character.setAttribute("transform", pin(worldState.position.x, worldState.position.y, 0.8));
    if (worldState.destination) $("map-marker").setAttribute("transform", pin(worldState.destination.x, worldState.destination.y, 0.8));
    $("map-zoom-out").disabled = zoom <= 1.001; $("map-zoom-in").disabled = zoom >= MAX_ZOOM - 0.001;
    $("map-follow")?.setAttribute("aria-pressed", String(camera.follow));
    renderScaleBar();
    requestDraw();
  }
  function renderScaleBar() {
    const bar = $("map-scale-bar");
    if (!bar) return;
    const unitsPerKm = rules.UNITS_PER_KM, pxPerKm = unitsPerKm * camera.scale;
    const km = [1, 2, 5, 10, 20, 25, 50].find(value => value * pxPerKm >= 70) || 50;
    bar.style.setProperty("--bar", Math.round(km * pxPerKm) + "px");
    bar.querySelector("span").textContent = km + " km";
  }
  function resizeView() {
    const viewport = $("map-viewport"), dpr = Math.min(2, window.devicePixelRatio || 1);
    const width = Math.max(1, viewport.clientWidth), height = Math.max(1, viewport.clientHeight);
    if (width === viewSize.width && height === viewSize.height && dpr === viewSize.dpr) return;
    const zoom = viewSize.width > 1 ? currentZoom() : DEFAULT_ZOOM;
    viewSize = { width, height, dpr };
    terrainCanvas.width = Math.round(width * dpr); terrainCanvas.height = Math.round(height * dpr);
    camera.scale = fitScale() * zoom;
    applyCamera();
  }
  function requestDraw() {
    if (drawQueued) return;
    drawQueued = true;
    requestAnimationFrame(() => { drawQueued = false; drawTerrain(); });
  }
  // The tile level whose resolution best matches the screen; finer levels are used when ready.
  const levelFor = scale => MapTerrain.LEVELS.find(level => level >= scale * viewSize.dpr * 0.9) || MapTerrain.LEVELS.at(-1);
  function drawTerrain() {
    const ctx = terrainContext, dpr = viewSize.dpr;
    ctx.setTransform(1, 0, 0, 1, 0, 0);
    ctx.fillStyle = "#120c08"; ctx.fillRect(0, 0, terrainCanvas.width, terrainCanvas.height);
    if (!terrainWorld?.ready) return;
    const s = camera.scale * dpr, left = camera.x - viewSize.width / 2 / camera.scale, top = camera.y - viewSize.height / 2 / camera.scale;
    ctx.imageSmoothingEnabled = true; ctx.imageSmoothingQuality = "high";
    const wanted = levelFor(camera.scale), missing = [];
    const drawLevel = (level, onlyMissing = null) => {
      const units = MapTerrain.TILE / level;
      const x0 = Math.max(0, Math.floor(left / units)), y0 = Math.max(0, Math.floor(top / units));
      const x1 = Math.min(Math.ceil(MapTerrain.W / units) - 1, Math.floor((left + viewSize.width / camera.scale) / units));
      const y1 = Math.min(Math.ceil(MapTerrain.H / units) - 1, Math.floor((top + viewSize.height / camera.scale) / units));
      for (let ty = y0; ty <= y1; ty++) for (let tx = x0; tx <= x1; tx++) {
        const tile = MapTerrain.tile(terrainWorld, level, tx, ty);
        const dx = (tx * units - left) * s, dy = (ty * units - top) * s, size = units * s;
        if (tile) ctx.drawImage(tile, Math.floor(dx), Math.floor(dy), Math.ceil(size + 1), Math.ceil(size + 1));
        else if (onlyMissing) onlyMissing.push({ level, tx, ty, distance: Math.hypot(tx * units + units / 2 - camera.x, ty * units + units / 2 - camera.y) });
      }
    };
    // Coarse level first (always complete), then the sharp one over it.
    drawLevel(1);
    if (wanted > 1) { if (wanted > 2) drawLevel(2); drawLevel(wanted, missing); }
    if (missing.length) queuePaint(missing.sort((a, b) => a.distance - b.distance));
  }
  let paintJobs = [];
  function queuePaint(jobs) {
    paintJobs = jobs;
    if (paintQueued) return;
    paintQueued = true;
    requestAnimationFrame(paintStep);
  }
  function paintStep() {
    paintQueued = false;
    if (!terrainWorld || modal.hidden) return;
    const end = performance.now() + 14;
    let painted = false;
    while (paintJobs.length && performance.now() < end) {
      const job = paintJobs.shift();
      if (!MapTerrain.tile(terrainWorld, job.level, job.tx, job.ty)) { MapTerrain.render(terrainWorld, job.level, job.tx, job.ty); painted = true; }
    }
    if (painted) requestDraw();
    if (paintJobs.length) { paintQueued = true; requestAnimationFrame(paintStep); }
  }
  // Builds the region's terrain in slices (with a progress note), then its overview tiles.
  function chartRegion(region) {
    MapTerrain.keepOnly(region.id);
    terrainWorld = MapTerrain.world(region);
    const note = $("map-charting");
    if (!terrainWorld) { note.hidden = true; return; }
    const worldObject = terrainWorld;
    const step = () => {
      if (terrainWorld !== worldObject) return;
      if (!worldObject.ready) {
        worldObject.build(14);
        note.hidden = false;
        note.textContent = `Charting ${region.name}… ${Math.round(worldObject.progress() * 100)}%`;
        requestAnimationFrame(step);
        return;
      }
      // The overview level, a tile at a time.
      const units = MapTerrain.TILE;
      for (let ty = 0; ty < Math.ceil(MapTerrain.H / units); ty++) for (let tx = 0; tx < Math.ceil(MapTerrain.W / units); tx++) {
        if (!MapTerrain.tile(worldObject, 1, tx, ty)) {
          MapTerrain.render(worldObject, 1, tx, ty);
          note.textContent = `Charting ${region.name}… inking`;
          requestAnimationFrame(step);
          return;
        }
      }
      note.hidden = true;
      renderLanterns(region);
      requestDraw();
    };
    if (worldObject.ready) { note.hidden = true; renderLanterns(region); requestDraw(); } else requestAnimationFrame(step);
  }
  function zoomAt(factor, screenX = viewSize.width / 2, screenY = viewSize.height / 2) {
    const before = { x: camera.x + (screenX - viewSize.width / 2) / camera.scale, y: camera.y + (screenY - viewSize.height / 2) / camera.scale };
    camera.scale *= factor;
    clampCamera();
    camera.x = before.x - (screenX - viewSize.width / 2) / camera.scale;
    camera.y = before.y - (screenY - viewSize.height / 2) / camera.scale;
    // While following, zoom keeps the traveller in the middle.
    if (camera.follow) { camera.x = worldState.position.x; camera.y = worldState.position.y; }
    applyCamera();
  }
  function setZoom(value) {
    camera.scale = fitScale() * Math.max(1, Math.min(MAX_ZOOM, value));
    if (camera.follow) { camera.x = worldState.position.x; camera.y = worldState.position.y; }
    applyCamera();
  }
  function centerOnPlayer() { camera.x = worldState.position.x; camera.y = worldState.position.y; applyCamera(); }
  function followPlayer() {
    if (!camera.follow) return;
    const dx = worldState.position.x - camera.x, dy = worldState.position.y - camera.y;
    if (Math.abs(dx) < 0.05 && Math.abs(dy) < 0.05) return;
    camera.x += dx * 0.18; camera.y += dy * 0.18;
    applyCamera();
  }
  function fitMap() { if (!modal.hidden) resizeView(); }

  /* ---------------- Overlay: settlements, lettering and lights ---------------- */
  const townArtFor = settlements => settlements.map(place => {
    const castle = place.kind === "Castle city";
    return `<g class="map-settlement" data-map-town="${place.id}" data-pin="0.62" data-x="${place.x}" data-y="${place.y}" transform="translate(${place.x} ${place.y})" role="button" tabindex="0" aria-label="Travel to ${place.name}"><title>${place.name} · ${place.kind}</title><circle r="72" fill="transparent"/><g class="map-settlement-icon">${castle ? castleArt() : villageArt()}</g><text class="map-town-name" y="${castle ? 48 : 42}">${place.name}</text><text class="map-town-kind" y="${castle ? 68 : 61}">${place.kind}</text></g>`;
  }).join("");
  const siteArtFor = sites => (sites || []).map(site => `<g class="map-settlement map-site" data-map-site="${site.id}" data-pin="0.62" data-x="${site.x}" data-y="${site.y}" transform="translate(${site.x} ${site.y})" role="button" tabindex="0" aria-label="Travel to ${site.name}"><title>${site.name}</title><circle r="52" fill="transparent"/><g class="map-settlement-icon"><path d="M-22 12V-14L0 -40L22 -14V12Z" fill="#7d7363" stroke="${ink}" stroke-width="2.6"/><path d="M0 -40V-62M-8 -54H8" stroke="${ink}" stroke-width="3"/></g><text class="map-town-name" y="44">${site.name}</text></g>`).join("");
  // Region roads: a waymarker at each border crossing, pointing out of the map, lettered with
  // the region it leads to. The lettering sits on the inland side so it never leaves the map.
  const exitSide = exit => {
    const p = exit.point, edges = [["west", p.x], ["east", MapTerrain.W - p.x], ["north", p.y], ["south", MapTerrain.H - p.y]];
    return edges.reduce((best, edge) => edge[1] < best[1] ? edge : best)[0];
  };
  const EXIT_ARROWS = { west: "←", east: "→", north: "↑", south: "↓" };
  const exitArtFor = exits => (exits || []).filter(exit => exit.targetRegionId).map(exit => {
    const side = exitSide(exit), angle = { east: 0, south: 90, west: 180, north: -90 }[side];
    const text = { west: `x="40" y="6" text-anchor="start"`, east: `x="-40" y="6" text-anchor="end"`, north: `y="58"`, south: `y="-50"` }[side];
    const kind = { west: `x="40" y="26" text-anchor="start"`, east: `x="-40" y="26" text-anchor="end"`, north: `y="78"`, south: `y="-30"` }[side];
    return `<g class="map-settlement map-exit" data-map-exit="${exit.id}" data-pin="0.62" data-x="${exit.point.x}" data-y="${exit.point.y}" transform="translate(${exit.point.x} ${exit.point.y})" role="button" tabindex="0" aria-label="Travel to the road toward ${escapeHtml(exit.destinationName)}"><title>${escapeHtml(exit.name)}</title><circle r="34" fill="transparent"/><g class="map-exit-icon"><circle r="21" fill="#2a1d14" stroke="${ink}" stroke-width="3"/><circle r="16" fill="none" stroke="#c9a96a" stroke-width="2"/><path transform="rotate(${angle})" d="M-9 -4H3V-10L13 0L3 10V4H-9Z" fill="#f0dfb4" stroke="${ink}" stroke-width="1.5" stroke-linejoin="round"/></g><text class="map-town-name map-exit-name" ${text}>${escapeHtml(exit.destinationName)}</text><text class="map-town-kind" ${kind}>Region road</text></g>`;
  }).join("");
  const labelsFor = region => (MapRegions.spec(region.id)?.labels || []).map(label => {
    const river = label.kind === "river";
    return `<g data-pin="0.75" data-x="${label.x}" data-y="${label.y}" transform="translate(${label.x} ${label.y})"><text class="${river ? "map-river-label" : "map-terrain-label"}"${label.angle ? ` transform="rotate(${label.angle})"` : ""}>${label.text}</text></g>`;
  }).join("");
  // Lit windows of the painted houses, and a soft glow over each place, for the night.
  function renderLanterns(region) {
    const plans = terrainWorld?.settlements || [];
    $("map-lanterns").innerHTML = plans.map(plan => `<g><ellipse cx="${plan.place.x}" cy="${plan.place.y}" rx="${plan.radius * 2.6}" ry="${plan.radius * 2.1}" fill="url(#map-lantern-halo)"/>${plan.houses.filter(house => house.lit).map(house => `<circle cx="${house.x.toFixed(1)}" cy="${(house.y - 0.2).toFixed(1)}" r=".45"/>`).join("")}</g>`).join("");
  }
  svg.insertAdjacentHTML("beforeend", `
    <defs>
      <radialGradient id="map-lantern-halo"><stop offset="0" stop-color="#ffcf75" stop-opacity=".38"/><stop offset=".6" stop-color="#ffcf75" stop-opacity=".14"/><stop offset="1" stop-color="#ffcf75" stop-opacity="0"/></radialGradient>
    </defs>
    <rect id="map-sunset" width="1600" height="1000" fill="#da7839" opacity="0" pointer-events="none"/>
    <rect id="map-night" width="1600" height="1000" fill="#08152f" opacity="0" pointer-events="none"/>
    <g id="map-lanterns" fill="#ffcf75" pointer-events="none"></g>
    <g id="map-landscape"></g>
    <path id="map-route" fill="none" stroke="#f0dfb4" stroke-width="3.5" stroke-dasharray="10 8" stroke-linecap="round" vector-effect="non-scaling-stroke" pointer-events="none"/>
    <g id="map-survey-bounds" fill="none" stroke="#f3df92" stroke-width="2.5" stroke-dasharray="12 9" opacity=".8" vector-effect="non-scaling-stroke" pointer-events="none"></g>
    <g id="map-guild-markers"></g>
    <g id="map-marker" hidden pointer-events="none"><ellipse rx="13" ry="5" fill="${MapInk.INK}" opacity=".45"/><circle r="11" fill="none" stroke="#f0dfb4" stroke-width="3"/><path d="M0 0v-42" stroke="${MapInk.INK}" stroke-width="3"/><path d="M0 -42l26 8-26 12Z" fill="#8a2c22" stroke="${MapInk.INK}" stroke-width="2.4" stroke-linejoin="round"/></g>
    <g id="map-character" role="button" tabindex="0" aria-label="Current position. Click to pause or resume travel" aria-pressed="false">
      <circle r="32" fill="transparent"/><circle class="map-character-ring" r="22" fill="#fff0c6" fill-opacity=".18" stroke="#fff2c6" stroke-width="3"/>
      <circle r="17" fill="#2a1d14" stroke="${MapInk.INK}" stroke-width="2"/><circle r="14" fill="#5e2620" stroke="#e8cf92" stroke-width="2"/><circle cy="-4.5" r="4.6" fill="#f0dfb4"/><path d="M-7.6 9Q-7.6 -.5 0 -.5T7.6 9Z" fill="#f0dfb4"/>
      <text class="map-character-name" x="33" y="6">You</text>
    </g>
  `);
  // Rain and lightning are screen weather over the whole view.
  $("map-weather").insertAdjacentHTML("beforeend", `
    <rect width="1600" height="1000" fill="#536b77" opacity=".12"/>
    <g id="map-weather-rain-far" fill="none" stroke="#e5f4f5" stroke-width="2" opacity=".47">${rainStreaks(130, 0)}</g>
    <g id="map-weather-rain-near" fill="none" stroke="#e1eef5" stroke-width="3" opacity=".61">${rainStreaks(100, 1)}</g>
    <rect id="map-weather-flash" width="1600" height="1000" fill="#dceaff" opacity="0"/>`);
  function renderRegionArt() {
    const region = rules.setActiveRegion(worldState.regionId);
    worldState.regionId = region.id;
    if (renderedRegionId === region.id) return region;
    $("map-landscape").innerHTML = labelsFor(region) + townArtFor(region.settlements || []) + siteArtFor(region.sites) + exitArtFor(region.exits);
    $("map-lanterns").innerHTML = "";
    $("map-weather").setAttribute("display", region.weather === "rainstorm" ? "inline" : "none");
    $("map-destinations").innerHTML = region.settlements.map(p => '<button type="button" data-map-town="' + p.id + '">' + p.name + '</button>').join("") + (region.sites || []).map(site => '<button type="button" data-map-site="' + site.id + '">' + site.name + '</button>').join("")
      + (region.exits || []).filter(exit => exit.targetRegionId).map(exit => '<button type="button" class="map-exit-button" data-map-exit="' + exit.id + '"><span class="map-exit-arrow" aria-hidden="true">' + EXIT_ARROWS[exitSide(exit)] + '</span> ' + escapeHtml(exit.destinationName) + '</button>').join("");
    $("map-destinations").querySelectorAll("button").forEach((button, index) => labelShortcut(button, String(index + 1)));
    $("map-region-eyebrow").textContent = region.eyebrow;
    $("map-title").innerHTML = region.title;
    $("region-title").textContent = region.name;
    $("region-description").textContent = region.description || (region.id === "alder-vale"
      ? "Grassy plains, two forests, the River Aster, and four settlements."
      : "Hill-ridden plains at the foot of the Stoneveil Range, crossed by the River Aster and three roads linking three settlements.");
    renderedRegionId = region.id;
    camera.x = worldState.position.x; camera.y = worldState.position.y; camera.follow = true;
    if (!modal.hidden) { chartRegion(region); applyCamera(); }
    return region;
  }

  function renderTimeSpeeds() {
    const speed = rules.normalizeTimeSpeed(worldState.timeSpeed);
    const locked = encounterResolving || Boolean(worldState.pendingEncounter || worldState.pendingRegionTransition || window.Combat?.active());
    for (const button of modal.querySelectorAll("[data-map-speed]")) {
      button.setAttribute("aria-pressed", String(Number(button.dataset.mapSpeed) === speed));
      button.disabled = locked;
    }
  }
  function setTimeSpeed(value) {
    if (!rules.timeSpeeds.includes(value) || encounterResolving || worldState.pendingEncounter || worldState.pendingRegionTransition || window.Combat?.active()) return;
    if (worldState.timeSpeed === value) return;
    // Account for idle time at the old rate before changing it. Keep any
    // fractional world minute so switching speeds never gains or loses time.
    advanceAmbientTime(performance.now());
    worldState.timeSpeed = value;
    renderTimeSpeeds();
    saveCharacter();
  }
  function renderClock() {
    const clockMinute = Math.floor(worldState.minutes);
    if (clockMinute === lastClockMinute && worldState.regionId === lastClockRegionId) return;
    lastClockMinute = clockMinute;
    lastClockRegionId = worldState.regionId;
    const label = 'Day ' + rules.day(worldState.minutes), time = rules.time(worldState.minutes), light = rules.lighting(worldState.minutes);
    const stormRegion = worldState.regionId === "stormwake-hills";
    const hour = ((worldState.minutes % 1440) + 1440) % 1440 / 60;
    const thunderstorm = stormRegion && (hour < 6 || hour >= 18);
    $("sheet-world-clock").textContent = label + ' · ' + time;
    $("map-day").textContent = label; $("map-clock").textContent = time;
    $("map-light-label").textContent = stormRegion ? thunderstorm ? "Thunderstorm" : "Rain" : light.label;
    $("map-night").setAttribute("opacity", stormRegion ? Math.max(light.darkness, thunderstorm ? .72 : .13) : light.darkness);
    $("map-sunset").setAttribute("opacity", stormRegion ? 0 : light.warmth);
    $("map-weather").classList.toggle("is-storm", thunderstorm);
    // Faded per element: a translucent group over the terrain canvas composites as a dark box.
    $("map-lanterns").style.setProperty("--lantern", Math.min(1, light.darkness / .66).toFixed(3));
  }
  function renderTravelPath(position = worldState.position, destination = worldState.destination) {
    $("map-character").setAttribute("transform", pin(position.x, position.y, 0.8));
    const route = destination && Array.isArray(worldState.route) && worldState.route.length ? worldState.route : destination ? [destination] : [];
    $("map-route").setAttribute("d", route.length ? [{ ...position }, ...route].map((routePoint, index) => (index ? "L" : "M") + routePoint.x + " " + routePoint.y).join("") : "");
    return route;
  }
  function isEncounterArea(zone, regionId) {
    const encounters = EncounterRules.regionRules(regionId);
    return Boolean(encounters.profiles[zone] && !encounters.profiles[zone].safe && encounters.tables[zone]?.length);
  }
  function renderGuildMarkers(markers, locked) {
    const layer = $("map-guild-markers");
    const signature = JSON.stringify([markers, locked]);
    // Keep the same hit targets across travel frames and clock refreshes. A
    // marker removed between pointerdown and pointerup cannot receive a click.
    if (layer.dataset.markers === signature) return;
    layer.dataset.markers = signature;
    layer.innerHTML = markers.map(marker => `<g class="map-guild-marker" role="button" tabindex="${locked ? -1 : 0}" aria-disabled="${locked}" aria-label="Travel to ${escapeHtml(marker.label)}" data-map-x="${marker.point.x}" data-map-y="${marker.point.y}" data-pin="0.8" data-x="${marker.point.x}" data-y="${marker.point.y}" transform="${pin(marker.point.x, marker.point.y, 0.8)}"><circle r="31" fill="transparent"/><circle r="19" fill="${marker.kind === "artifact" ? "#c59b3c" : marker.kind === "great-hunt" ? "#7d2135" : "#c7683f"}" stroke="#fff4d0" stroke-width="4"/><text y="6" text-anchor="middle" fill="#fffaf5" font-size="1.25rem">${marker.kind === "artifact" ? "◆" : marker.kind === "great-hunt" ? "★" : "!"}</text><title>${escapeHtml(marker.label)}</title></g>`).join("");
  }
  function render() {
    lastFullRender = performance.now();
    const region = renderRegionArt();
    renderClock();
    renderTimeSpeeds();
    const p = worldState.position, destination = worldState.destination, place = rules.settlementAt(p, region.id), combat = window.Combat?.active(), pendingEncounter = Boolean(worldState.pendingEncounter), pendingTransition = Boolean(worldState.pendingRegionTransition);
    const mapLocked = Boolean(combat) || pendingEncounter || pendingTransition || encounterResolving;
    const guildMarkers = window.Guild?.markers() || [], destinationsLocked = mapLocked || currentHp <= 0;
    const classEffects = calculateClassEffects();
    if (classEffects.surveyorsEye) {
      const ellipses = region.id === "alder-vale" ? rules.forests.flatMap(area => area.deep ? [area, { x: area.x, y: area.y, ...area.deep }] : [area]) : region.id === "stoneveil-foothills" ? region.hillAreas : region.id === "sunward-coast" ? [region.saltMarsh] : region.id === "stormwake-hills" ? [...region.hillAreas, region.stormcrest] : region.id === "gloamfen-marsh" ? [region.rockyRise, region.deepMire, region.reedwater] : region.id === "verdigris-jungle" ? [region.outerCanopy, region.denseCanopy, region.oldGrowth, region.highCanopy] : region.id === "mirrormere-basin" ? [region.lake, region.marsh] : region.id === "saffron-expanse" ? [region.oasis, region.saltPans, region.glassDunes] : region.id === "deep-desert" ? [region.pyramid, region.necropolis, region.obsidianFlats] : region.id === "crimson-divide" ? [region.escarpment, region.bloodglass] : region.id === "bloodstone-coast" ? [...region.rockFields, region.bloodShore] : region.id === "ancient-battlefield" ? [region.bonePits, region.massGraves, region.trenches] : region.id === "duskpine-forest" ? [region.deepBoreal, region.frostBog, region.shadowHollow] : region.id === "cinder-isle" ? [region.volcano, region.landShape] : Object.values(region.forestLayers || {});
      const towns = region.id === "verdant-reach" ? [] : region.settlements;
      $("map-survey-bounds").innerHTML = ellipses.map(area => `<ellipse cx="${area.x}" cy="${area.y}" rx="${area.rx}" ry="${area.ry}"/>`).join("") + towns.map(place => `<circle cx="${place.x}" cy="${place.y}" r="${region.id === "stoneveil-foothills" ? 120 : 110}"/>`).join("") + (region.id === "stoneveil-foothills" ? `<path d="M20 ${region.mountainfoot.y}H1580"/>` : region.id === "high-stoneveil" ? '<path d="M20 745H1580M430 20V745M1190 20V745M430 295H1190M1190 335H1580"/>' : region.id === "stoneveil-summit" ? '<path d="M20 735H1580M440 20V735M1160 20V735M440 330H1160"/>' : "");
    } else $("map-survey-bounds").innerHTML = "";
    renderGuildMarkers(guildMarkers, destinationsLocked);
    const route = renderTravelPath(p, destination);
    $("map-character").setAttribute("aria-pressed", String(travelling));
    $("map-character").setAttribute("aria-disabled", String(destinationsLocked));
    $("map-character").setAttribute("tabindex", destinationsLocked ? "-1" : "0");
    $("map-character").classList.toggle("is-travelling", travelling);
    $("map-marker").toggleAttribute("hidden", !destination);
    if (destination) $("map-marker").setAttribute("transform", pin(destination.x, destination.y, 0.8));
    const forest = region.id === "alder-vale" ? forests.find(f => ((p.x - f.x) / f.rx) ** 2 + ((p.y - f.y) / f.ry) ** 2 <= 1) : null, terrain = rules.terrainAt(p, region.id);
    const zone = rules.zoneAt(p, region.id), zoneProfile = EncounterRules.profileFor ? EncounterRules.profileFor(zone, region.id) : EncounterRules.profiles[zone];
    $("map-zone").textContent = zone;
    $("map-zone-level").textContent = zoneProfile?.safe ? "Safe" : zoneProfile ? `Lv ${zoneProfile.minimum}–${zoneProfile.maximum}` : "Unrated";
    const encounterRate = EncounterRules.encounterChance(zone, { level, regionId: region.id, encounterChanceMultiplier: Math.max(0, 1 + classEffects.encounterChancePercent / 100) });
    $("map-zone-level").title = `Travel encounter chance: ${formatValue(encounterRate)}% per ${rules.ENCOUNTER_MINUTES} in-game minutes${zoneProfile && level >= zoneProfile.maximum + 10 && !zoneProfile.safe ? ` · ${level >= zoneProfile.maximum + 20 ? 60 : 30}% less due to your level` : ""}`;
    if (zone === "Church of Talos") $("map-zone-level").textContent = worldState.defeatedSiteBosses?.includes("tankina-disciple-of-talos-level-44") ? "Sanctuary · Lv 44 challenge" : "Boss · Lv 44";
    const travelBonus = travelBonuses();
    $("map-location").textContent = place ? place.name + ' · ' + place.kind : rules.landmarkAt(p, region.id) ? rules.landmarkAt(p, region.id).name + ' · Hostile to non-Vampires' : terrain === "road" ? 'On the road · ' + rules.speedAt(p, travelBonus, region.id) + ' km/h' : terrain === "river" ? 'Crossing River Aster · ' + rules.speedAt(p, travelBonus, region.id) + ' km/h' : forest ? (forest === forests[0] ? 'Great Alderwood' : 'Foxglove Copse') + ' · ' + rules.speedAt(p, travelBonus, region.id) + ' km/h' : region.id !== "alder-vale" ? zone.replace(" - near Town", "") + ' · ' + rules.speedAt(p, travelBonus, region.id) + ' km/h' : 'Grassy plains · ' + rules.speedAt(p, travelBonus, region.id) + ' km/h';
    $("map-pause").disabled = !destination || mapLocked || currentHp <= 0;
    $("map-pause").textContent = travelling ? 'Pause travel' : destination ? 'Resume travel' : 'Pause travel';
    $("map-cancel").disabled = !destination || mapLocked;
    const canForceEncounter = Boolean(window.Guild?.rematchAt(p, true) || isEncounterArea(zone, region.id)) && !mapLocked && currentHp > 0;
    $("map-force-encounter").hidden = !canForceEncounter;
    $("map-force-encounter").disabled = !canForceEncounter;
    const restPrice = place?.kind === "Castle city" ? 25 : 15;
    const canRest = Boolean(place) && !mapLocked && !travelling && ShopRules.walletValue(currencies) >= restPrice;
    $("map-rest").hidden = !canRest;
    $("map-rest").disabled = !canRest;
    $("map-rest").textContent = 'Rest · 10 hours · ' + ShopRules.formatPrice(restPrice);
    $("map-rest").title = place ? 'Rest in ' + place.name + ' and fully restore HP and Mana' : 'Reach a settlement to rest';
    [$("map-blacksmith"), $("map-item-shop")].forEach(button => { button.hidden = !place || mapLocked || travelling; button.disabled = !place || mapLocked || travelling; });
    $("map-blacksmith").title = place ? 'Browse weapons and armor at the blacksmith in ' + place.name : 'Reach a settlement to shop';
    $("map-item-shop").title = place ? 'Browse the general store in ' + place.name : 'Reach a settlement to shop';
    const supplies = inventory.filter(id => id === "consumable-camping-supplies").length;
    $("map-camp").hidden = supplies < 1 || mapLocked || travelling;
    $("map-camp").disabled = supplies < 1 || mapLocked || travelling;
    $("map-camp").textContent = 'Camp · 8 hours' + (supplies ? ' · ×' + supplies : '');
    $("map-camp").title = supplies ? 'Consume Camping Supplies and fully restore HP and Mana' : 'Camping Supplies required';
    const canGuild = (place?.kind === "Castle city" || place?.guildServices?.includes("quest")) && !mapLocked && !travelling;
    $("map-guild").hidden = !canGuild; $("map-guild").disabled = !canGuild;
    const canTower = place?.kind === "Castle city" && !mapLocked && !travelling && Boolean(window.MagicTower);
    $("map-magic-tower").hidden = !canTower; $("map-magic-tower").disabled = !canTower;
    $("map-magic-tower").title = canTower ? "Buy staffs, wands, spells and runes, and fit runes in " + place.name : "Magic Towers stand in castle cities";
    const church = rules.siteAt(p, region.id);
    const atChurch = church?.id === "church-of-talos" && !mapLocked && !travelling && currentHp > 0;
    $("map-church").hidden = !atChurch; $("map-church").disabled = !atChurch;
    const churchUnlocked = atChurch && worldState.defeatedSiteBosses?.includes("tankina-disciple-of-talos-level-44");
    $("map-church-store").hidden = !churchUnlocked; $("map-church-store").disabled = !churchUnlocked;
    for (const [id, kind] of [["map-class-respec", "classes"], ["map-stat-respec", "stats"]]) {
      const button = $(id), unlocked = churchUnlocked;
      button.hidden = !unlocked;
      button.disabled = !unlocked || Boolean(window.ChurchOfTalos?.serviceReason(kind));
      button.textContent = (kind === "classes" ? "Respec Classes" : "Respec Stats") + " · " + ShopRules.formatPrice(church?.servicePriceSilver || 12000);
      button.title = window.ChurchOfTalos?.serviceReason(kind) || "Refund your spent points. Allocate them again on your character sheet.";
    }
    const boatDestination = place?.boat && rules.settlementsForRegion(place.boat.regionId).find(candidate => candidate.id === place.boat.settlementId);
    const canBoat = Boolean(boatDestination) && !mapLocked && !travelling && currentHp > 0;
    $("map-boat").hidden = !canBoat; $("map-boat").disabled = !canBoat;
    $("map-boat").title = boatDestination ? `Sail to ${boatDestination.name} · ${place.boat.minutes / 60} hours` : "Reach a port to travel by boat";
    const canSearch = window.Guild?.canSearch() || false;
    $("map-search").hidden = !canSearch; $("map-search").disabled = !canSearch;
    const trailSense = toInteger(classLevels.stalker, 0) >= 1;
    const quarry = trailSense && !zoneProfile?.safe ? EncounterRules.enemiesForZone(zone, region.id) : [];
    const quarrySelect = $("map-track-enemy"), oldQuarry = quarrySelect.value;
    const quarryIds = quarry.join("\u0000");
    if (quarrySelect.dataset.enemies !== quarryIds) {
      quarrySelect.innerHTML = quarry.map(name => `<option value="${escapeHtml(name)}">${escapeHtml(name)}</option>`).join("");
      quarrySelect.dataset.enemies = quarryIds;
      if (quarry.includes(oldQuarry)) quarrySelect.value = oldQuarry;
    }
    $("map-track-control").hidden = !trailSense || !quarry.length;
    const trackState = activeSkills["stalker:track-quarry"] || {};
    const canTrack = toInteger(classLevels.stalker, 0) >= 4 && quarry.length && !mapLocked && !travelling && currentHp > 0 && trackState.lastUsedDay !== todayKey();
    $("map-track-quarry").hidden = !trailSense || !quarry.length;
    $("map-track-quarry").disabled = !canTrack;
    $("map-track-quarry").title = toInteger(classLevels.stalker, 0) < 4 ? "Unlock Stalker level 4" : trackState.lastUsedDay === todayKey() ? "Already used today" : "Start an encounter with the selected local enemy";
    if (combat) $("map-instructions").textContent = 'Travel and resting are paused until combat ends.';
    else if (pendingTransition) $("map-instructions").textContent = 'Choose whether to continue into the next region or turn back.';
    else if (pendingEncounter) $("map-instructions").textContent = 'Your journey is paused until the encounter is resolved.';
    else if (currentHp <= 0) $("map-instructions").textContent = 'You cannot travel at 0 HP. Rest here if you are in a settlement.';
    else if (destination) {
      const minutes = Math.ceil(rules.routeTravelMinutes(p, route, travelBonus, region.id));
      $("map-instructions").textContent = (travelling ? 'Travelling' : 'Travel paused') + ' · Fastest route · ' + (rules.routeDistance(p, route) / rules.UNITS_PER_KM).toFixed(1) + ' km remaining · ' + Math.floor(minutes / 60) + 'h ' + (minutes % 60) + 'm · Arrive ' + rules.time(worldState.minutes + minutes) + '. Click to reroute, or hold the left mouse button to steer.';
    } else $("map-instructions").textContent = 'Click the map, a settlement, or a quest marker to travel. Hold the left mouse button and move the cursor to steer.';
    if (classEffects.surveyorsEye) {
      const nearest = rules.nearestSettlement(p, region.id);
      const nearestMarker = guildMarkers.reduce((best, marker) => Math.min(best, rules.distance(p, marker.point)), Infinity);
      $("map-instructions").textContent += ` Surveyor's Eye: ${nearest?.name || "Unknown settlement"} ${nearest ? (rules.distance(p, nearest) / rules.UNITS_PER_KM).toFixed(1) : "?"} km away${Number.isFinite(nearestMarker) ? ` · Quest marker ${(nearestMarker / rules.UNITS_PER_KM).toFixed(1)} km away` : ""}.`;
    }
    document.querySelectorAll("[data-map-town], [data-map-site], [data-map-exit]").forEach(button => {
      button.setAttribute("aria-disabled", String(destinationsLocked));
      if (button instanceof HTMLButtonElement) button.disabled = destinationsLocked;
      else button.setAttribute("tabindex", destinationsLocked ? "-1" : "0");
    });
    renderEncounterPrompt();
    renderTransitionPrompt();
    renderShortcuts();
  }
  function pause(save = true) {
    cancelMapHold();
    travelling = false; cancelAnimationFrame(frame); frame = null; lastFrame = 0;
    render();
    if (save) saveCharacter();
  }
  function encounterSummary(enemyNames) {
    const counts = enemyNames.reduce((result, name) => ({ ...result, [name]: (result[name] || 0) + 1 }), {});
    return Object.entries(counts).map(([name, count]) => count > 1 ? name + " ×" + count : name).join(", ");
  }
  function renderEncounterPrompt() {
    const encounter = worldState.pendingEncounter;
    if (!encounter || modal.hidden || window.Combat?.active()) {
      encounterModal.hidden = true;
      return;
    }
    const counts = encounter.enemies.reduce((result, name) => ({ ...result, [name]: (result[name] || 0) + 1 }), {});
    $("map-encounter-zone").textContent = encounter.zone;
    const empowered = ["hunt", "artifact"].includes(encounter.objective);
    $("map-encounter-enemies").innerHTML = Object.entries(counts).map(([name, count]) => {
      const element = Object.prototype.hasOwnProperty.call(EncounterRules.dragonElements, name) ? EncounterRules.dragonElements[name] : null;
      const accentClass = SPELL_ELEMENTS.includes(element) ? ` class="has-elemental-accent spell-element-${element}"` : "";
      return `<li${accentClass}>${escapeHtml(empowered ? "Empowered " + name : name)}<span>×${count}</span></li>`;
    }).join("");
    $("map-encounter-fight").disabled = encounterResolving;
    $("map-encounter-run").disabled = encounterResolving;
    encounterModal.hidden = false;
  }
  function renderTransitionPrompt() {
    const pending = worldState.pendingRegionTransition;
    if (!pending || modal.hidden || window.Combat?.active()) {
      transitionModal.hidden = true;
      return;
    }
    const exit = rules.exitById(pending.exitId, pending.regionId);
    if (!exit) {
      worldState.pendingRegionTransition = null;
      transitionModal.hidden = true;
      return;
    }
    $("map-transition-title").innerHTML = exit.targetRegionId ? 'Travel to <em>' + escapeHtml(exit.destinationName || "the next region") + '</em>?' : 'The road leads <em>beyond the map</em>';
    $("map-transition-intro").textContent = exit.prompt;
    $("map-transition-status").textContent = exit.targetRegionId ? 'Your journey will continue from the connecting road.' : 'This destination has not been charted yet.';
    $("map-transition-travel").hidden = !exit.targetRegionId;
    $("map-transition-travel").disabled = !exit.targetRegionId;
    transitionModal.hidden = false;
  }
  function presentRegionTransition(exit) {
    cancelMapHold();
    travelling = false; cancelAnimationFrame(frame); frame = null; lastFrame = 0;
    worldState.destination = null; worldState.route = [];
    worldState.pendingRegionTransition = { regionId: worldState.regionId, exitId: exit.id };
    $("map-status").textContent = exit.prompt;
    saveCharacter(); render(); updateModalState();
    (exit.targetRegionId ? $("map-transition-travel") : $("map-transition-stay")).focus();
  }
  function declineRegionTransition() {
    const pending = worldState.pendingRegionTransition;
    const exit = pending && rules.exitById(pending.exitId, pending.regionId);
    if (exit) worldState.position = rules.point(exit.retreat);
    worldState.pendingRegionTransition = null;
    worldState.destination = null; worldState.route = [];
    transitionModal.hidden = true;
    $("map-status").textContent = 'You turn back from the regional border.';
    saveCharacter(); render(); updateModalState();
  }
  function acceptRegionTransition() {
    const pending = worldState.pendingRegionTransition;
    const exit = pending && rules.exitById(pending.exitId, pending.regionId);
    if (!exit?.targetRegionId || !rules.regions[exit.targetRegionId]) return declineRegionTransition();
    const previousRegionId = worldState.regionId;
    worldState.regionId = exit.targetRegionId;
    rules.setActiveRegion(worldState.regionId);
    window.Music?.sync();
    worldState.position = rules.point(exit.targetPoint || rules.getRegion(worldState.regionId).start);
    worldState.destination = null; worldState.route = [];
    worldState.pendingRegionTransition = null;
    worldState.encounterTravelMinutes = 0;
    transitionModal.hidden = true;
    renderedRegionId = null;
    window.Guild?.onRegionChange(previousRegionId);
    $("map-status").textContent = 'Entered ' + rules.getRegion(worldState.regionId).name + '.';
    saveCharacter(); render(); fitMap(); updateModalState();
  }
  function presentTravelEncounter(enemyNames, zone, objective = null) {
    cancelMapHold();
    travelling = false; cancelAnimationFrame(frame); frame = null; lastFrame = 0;
    worldState.pendingEncounter = { zone, enemies: [...enemyNames], objective };
    const summary = (["hunt", "artifact"].includes(objective) ? "Empowered " : "") + encounterSummary(enemyNames);
    $("map-status").textContent = "Encounter in " + zone + ": " + summary + ".";
    $("map-encounter-status").textContent = "Fight, or attempt to escape with a +25 point Run bonus.";
    saveCharacter(); render();
    updateModalState();
    $("map-encounter-fight").focus();
  }
  function forceEncounter() {
    if (modal.hidden || encounterResolving || worldState.pendingEncounter || worldState.pendingRegionTransition || window.Combat?.active() || currentHp <= 0) return;
    const rematch = window.Guild?.rematchAt(worldState.position, true);
    if (rematch) {
      presentTravelEncounter(rematch.enemies, rematch.zone, rematch.kind);
      return;
    }
    const zone = rules.zoneAt(worldState.position, worldState.regionId);
    if (!isEncounterArea(zone, worldState.regionId)) return;
    const enemies = EncounterRules.generate(zone, { level, regionId: worldState.regionId });
    if (!enemies.length) {
      $("map-status").textContent = "No encounter is available in this area.";
      return;
    }
    presentTravelEncounter(enemies, zone);
  }
  async function startPendingEncounter(openingNote = "") {
    if (encounterResolving || !worldState.pendingEncounter) return;
    encounterResolving = true; renderEncounterPrompt();
    $("map-encounter-status").textContent = openingNote || "Preparing combat…";
    const pending = { zone: worldState.pendingEncounter.zone, enemies: [...worldState.pendingEncounter.enemies], objective: worldState.pendingEncounter.objective };
    try {
      if (!await window.Combat.startEncounter(pending.enemies, openingNote, pending.objective)) throw new Error("Combat could not start.");
      worldState.pendingEncounter = null;
      saveCharacter();
    } catch (error) {
      modal.hidden = false;
      $("map-status").textContent = "Encounter failed to load: " + error.message;
      $("map-encounter-status").textContent = "Could not begin combat. " + error.message;
    } finally {
      encounterResolving = false;
      render(); updateModalState();
    }
  }
  async function runFromPendingEncounter() {
    if (encounterResolving || !worldState.pendingEncounter) return;
    encounterResolving = true; renderEncounterPrompt();
    $("map-encounter-status").textContent = "Looking for an escape route…";
    const pending = { zone: worldState.pendingEncounter.zone, enemies: [...worldState.pendingEncounter.enemies], objective: worldState.pendingEncounter.objective };
    try {
      const result = await window.Combat.attemptTravelEscape(pending.enemies, 25, pending.objective);
      if (result.success) {
        if (pending.objective) window.Guild?.avoidObjective(pending.objective);
        worldState.pendingEncounter = null;
        encounterModal.hidden = true;
        $("map-status").textContent = "Escaped the encounter (" + formatValue(result.chance) + "% chance). Travel remains paused.";
        saveCharacter();
        return;
      }
      encounterResolving = false;
      await startPendingEncounter("Pre-combat escape failed (" + formatValue(result.chance) + "% chance).");
    } catch (error) {
      $("map-encounter-status").textContent = "Could not attempt escape. " + error.message;
    } finally {
      encounterResolving = false;
      render(); updateModalState();
    }
  }
  function checkTravelEncounter(elapsedMinutes) {
    const elapsed = Number(elapsedMinutes);
    if (!Number.isFinite(elapsed) || elapsed <= 0 || encounterResolving || worldState.pendingEncounter || worldState.pendingRegionTransition || window.Combat?.active()) return false;
    worldState.encounterTravelMinutes = Math.max(0, Number(worldState.encounterTravelMinutes) || 0) + elapsed;
    // One check per stretch of travel; the stretch grew with the regions, so a crossing
    // brings as many encounters as before.
    while (worldState.encounterTravelMinutes >= rules.ENCOUNTER_MINUTES) {
      worldState.encounterTravelMinutes -= rules.ENCOUNTER_MINUTES;
      const zone = rules.zoneAt(worldState.position, worldState.regionId);
      const encounter = EncounterRules.roll(zone, { level, regionId: worldState.regionId, encounterChanceMultiplier: Math.max(0, 1 + calculateClassEffects().encounterChancePercent / 100) });
      if (encounter?.length) {
        presentTravelEncounter(encounter, zone);
        return true;
      }
    }
    return false;
  }
  function checkGuildObjective() {
    if (encounterResolving || worldState.pendingEncounter || worldState.pendingRegionTransition || window.Combat?.active() || currentHp <= 0) return false;
    const objective = window.Guild?.objectiveAt(worldState.position);
    if (!objective) return false;
    presentTravelEncounter(objective.enemies || [objective.enemy], objective.zone, objective.kind);
    return true;
  }
  function tick(now) {
    if (!travelling) return;
    if (modal.hidden || document.hidden || window.Combat?.active() || currentHp <= 0) { pause(); return; }
    const elapsed = lastFrame ? Math.min((now - lastFrame) / 1000, 0.2) : 0;
    lastFrame = now;
    const previousZone = rules.zoneAt(worldState.position, worldState.regionId);
    const result = rules.step(worldState, rules.realTimeMinutes(elapsed, worldState.timeSpeed, true), travelBonuses());
    advanceWorldTime(result.elapsed);
    window.Guild?.onPositionChange();
    if (result.arrived) {
      const exit = rules.exitAt(worldState.position, worldState.regionId);
      if (exit) { presentRegionTransition(exit); return; }
    }
    const currentZone = rules.zoneAt(worldState.position, worldState.regionId);
    if (currentZone === "Vampire Citadel" && previousZone !== currentZone) {
      presentTravelEncounter(EncounterRules.generate(currentZone, { level, regionId: worldState.regionId }), currentZone);
      render(); return;
    }
    if (checkGuildObjective()) { render(); return; }
    if (checkTravelEncounter(result.elapsed)) { render(); return; }
    if (result.arrived) {
      travelling = false;
      const town = rules.settlementAt(worldState.position, worldState.regionId);
      $("map-status").textContent = town ? 'Arrived in ' + town.name + '. You can rest here.' : 'Destination reached.';
      saveCharacter();
    } else if (now - lastSave >= 700) { lastSave = now; saveCharacter(); }
    if (result.arrived || now - lastFullRender >= 100) render();
    else renderTravelPath();
    followPlayer();
    if (travelling) frame = requestAnimationFrame(tick);
  }
  function resume() {
    if (!worldState.destination || modal.hidden || encounterResolving || worldState.pendingEncounter || worldState.pendingRegionTransition || window.Combat?.active() || currentHp <= 0) return;
    travelling = true; lastFrame = 0;
    cancelAnimationFrame(frame); frame = requestAnimationFrame(tick); render();
  }
  function destinationAt(point, { save = true, snapToSettlement = true } = {}) {
    if (encounterResolving || worldState.pendingEncounter || worldState.pendingRegionTransition || window.Combat?.active() || currentHp <= 0) return false;
    if (!point || !rules.isLand(point, worldState.regionId)) { $("map-status").textContent = worldState.regionId === "mirrormere-basin" ? "The lake is impassable on foot. Follow the shore road." : "The open sea is impassable on foot. Use a port to sail."; return false; }
    const nearby = snapToSettlement ? rules.settlementAt(point, worldState.regionId) : null;
    const destination = rules.point(nearby || point);
    if (worldState.destination && rules.distance(worldState.destination, destination) < .5) {
      if (!travelling) resume();
      return true;
    }
    worldState.destination = destination;
    worldState.route = rules.routeBetween(worldState.position, worldState.destination, travelBonuses(), worldState.regionId);
    $("map-status").textContent = 'Fastest route set. Click another point, or hold the left mouse button to steer.';
    if (save) saveCharacter();
    if (travelling) render();
    else resume();
    return true;
  }
  function mouseMapPoint(event) {
    const matrix = svg.getScreenCTM(), bounds = $("map-viewport").getBoundingClientRect();
    if (!matrix || event.clientX < bounds.left || event.clientX > bounds.right || event.clientY < bounds.top || event.clientY > bounds.bottom) return null;
    const point = new DOMPoint(event.clientX, event.clientY).matrixTransform(matrix.inverse());
    return point.x >= 0 && point.x <= 1600 && point.y >= 0 && point.y <= 1000 ? point : null;
  }
  function cancelMapHold() {
    const held = mapHold;
    mapHold = null;
    clearTimeout(mapHoldTimer); mapHoldTimer = null;
    clearInterval(mapSteeringTimer); mapSteeringTimer = null;
    svg.classList.remove("is-mouse-steering");
    if (held?.active) suppressMapClickUntil = performance.now() + 600;
    if (held && svg.hasPointerCapture(held.id)) svg.releasePointerCapture(held.id);
    return Boolean(held?.active);
  }
  function steerHeldMouse() {
    if (!mapHold?.active) return;
    if (modal.hidden || document.hidden || encounterResolving || worldState.pendingEncounter || worldState.pendingRegionTransition || window.Combat?.active() || currentHp <= 0) { cancelMapHold(); return; }
    const point = mouseMapPoint(mapHold);
    if (!point || mapHold.lastPoint && rules.distance(point, mapHold.lastPoint) < 6) return;
    if (destinationAt(point, { save: false }) && mapHold) mapHold.lastPoint = { x: point.x, y: point.y };
  }
  let openedBefore = false;
  function open() {
    modal.hidden = false; render(); updateModalState();
    resizeView();
    // First opening: zoomed in on you. Later openings keep your zoom and follow you again.
    camera.follow = true;
    if (!openedBefore) { openedBefore = true; setZoom(DEFAULT_ZOOM); } else centerOnPlayer();
    chartRegion(rules.getRegion(worldState.regionId));
    const pendingExit = worldState.pendingRegionTransition && rules.exitById(worldState.pendingRegionTransition.exitId, worldState.pendingRegionTransition.regionId);
    (worldState.pendingEncounter ? $("map-encounter-fight") : pendingExit?.targetRegionId ? $("map-transition-travel") : worldState.pendingRegionTransition ? $("map-transition-stay") : modal.querySelector(".modal-close")).focus();
  }
  function close() {
    pause(); modal.hidden = true; renderEncounterPrompt(); renderTransitionPrompt(); updateModalState(); $("map-open-button").focus();
  }
  function rest() {
    const place = rules.settlementAt(worldState.position, worldState.regionId);
    if (!place || travelling || encounterResolving || worldState.pendingEncounter || worldState.pendingRegionTransition || window.Combat?.active()) return;
    const price = place.kind === "Castle city" ? 25 : 15;
    const remaining = ShopRules.spend(currencies, price);
    if (!remaining) return;
    currencies = remaining;
    document.querySelectorAll(".currency-input").forEach(input => { input.value = currencies[input.dataset.currency]; });
    worldState.destination = null; worldState.route = [];
    advanceWorldTime(600);
    worldState.lastRestKm = worldState.distanceTraveledKm;
    if (!calculateRaceEffects().healingDisabled) currentHp = calculateMaxHpWithArtifacts(currentStats());
    currentMana = calculateMaxManaWithEffects(currentStats());
    window.Companions?.restoreAll();
    updateDerivedValues(); renderKnownSpells(); renderSkills(); saveCharacter(); render();
    $("map-status").textContent = 'Rested 10 hours in ' + place.name + ' for ' + ShopRules.formatPrice(price) + '. HP and Mana fully restored.';
  }
  function camp() {
    if (travelling || encounterResolving || worldState.pendingEncounter || worldState.pendingRegionTransition || window.Combat?.active()) return;
    const suppliesIndex = inventory.indexOf("consumable-camping-supplies");
    if (suppliesIndex < 0) return;
    const retentionChance = Math.max(0, Math.min(100, calculateClassEffects().campingSupplyRetentionChance));
    const retained = Math.random() * 100 < retentionChance;
    if (!retained) inventory.splice(suppliesIndex, 1);
    worldState.destination = null; worldState.route = [];
    advanceWorldTime(480);
    worldState.lastRestKm = worldState.distanceTraveledKm;
    if (!calculateRaceEffects().healingDisabled) currentHp = calculateMaxHpWithArtifacts(currentStats());
    currentMana = calculateMaxManaWithEffects(currentStats());
    window.Companions?.restoreAll();
    updateDerivedValues(); renderOwnedItems(); renderKnownSpells(); renderSkills(); saveCharacter(); render();
    $("map-status").textContent = 'Camped for 8 hours. ' + (retained ? 'Camping Supplies recovered' : 'One set of Camping Supplies consumed') + '; HP and Mana fully restored.';
  }
  function travelByBoat() {
    const place = rules.settlementAt(worldState.position, worldState.regionId);
    const boat = place?.boat;
    const destination = boat && rules.settlementsForRegion(boat.regionId).find(candidate => candidate.id === boat.settlementId);
    if (!destination || travelling || encounterResolving || worldState.pendingEncounter || worldState.pendingRegionTransition || window.Combat?.active() || currentHp <= 0) return;
    const previousRegionId = worldState.regionId;
    pause(false);
    worldState.destination = null; worldState.route = [];
    advanceWorldTime(boat.minutes);
    worldState.regionId = boat.regionId;
    rules.setActiveRegion(boat.regionId);
    worldState.position = { x: destination.x, y: destination.y };
    worldState.encounterTravelMinutes = 0;
    renderedRegionId = null;
    window.Music?.sync();
    window.Guild?.onRegionChange(previousRegionId);
    $("map-status").textContent = `Sailed from ${place.name} to ${destination.name} in ${boat.minutes / 60} hours.`;
    saveCharacter(); render(); fitMap();
  }
  $("map-open-button").addEventListener("click", open);
  modal.querySelectorAll("[data-map-close]").forEach(button => button.addEventListener("click", close));
  modal.querySelectorAll("[data-map-speed]").forEach(button => button.addEventListener("click", () => setTimeSpeed(Number(button.dataset.mapSpeed))));
  $("map-pause").addEventListener("click", () => travelling ? pause() : resume());
  $("map-cancel").addEventListener("click", () => {
    if (encounterResolving || worldState.pendingEncounter || worldState.pendingRegionTransition || window.Combat?.active()) return;
    pause(false); worldState.destination = null; worldState.route = []; saveCharacter(); render();
  });
  $("map-rest").addEventListener("click", rest);
  $("map-force-encounter").addEventListener("click", forceEncounter);
  $("map-camp").addEventListener("click", camp);
  $("map-boat").addEventListener("click", travelByBoat);
  $("map-search").addEventListener("click", () => { if (window.Guild?.search()) { $("map-status").textContent = "Search complete. Return to the Adventurers Guild for your reward."; render(); } });
  $("map-track-quarry").addEventListener("click", () => {
    const zone = rules.zoneAt(worldState.position, worldState.regionId);
    const name = $("map-track-enemy").value;
    if (toInteger(classLevels.stalker, 0) < 4 || !name || !EncounterRules.enemiesForZone(zone, worldState.regionId).includes(name) || $("map-track-quarry").disabled) return;
    activeSkills["stalker:track-quarry"] = { active: false, lastUsedDay: todayKey() };
    renderSkills();
    presentTravelEncounter([name], zone);
  });
  $("map-encounter-fight").addEventListener("click", () => startPendingEncounter());
  $("map-encounter-run").addEventListener("click", runFromPendingEncounter);
  $("map-transition-stay").addEventListener("click", declineRegionTransition);
  $("map-transition-travel").addEventListener("click", acceptRegionTransition);
  $("map-zoom-in").addEventListener("click", () => zoomAt(1.5));
  $("map-zoom-out").addEventListener("click", () => zoomAt(1 / 1.5));
  $("map-fit").addEventListener("click", () => { camera.follow = false; setZoom(1); });
  $("map-follow").addEventListener("click", () => { camera.follow = true; centerOnPlayer(); });
  // Wheel zooms around the cursor; right or middle drag pans; panning stops following you.
  const viewport = $("map-viewport");
  viewport.addEventListener("wheel", event => {
    if (modal.hidden) return;
    event.preventDefault();
    const bounds = viewport.getBoundingClientRect();
    if (event.shiftKey || Math.abs(event.deltaX) > Math.abs(event.deltaY)) {
      camera.follow = false;
      camera.x += (event.deltaX || event.deltaY) / camera.scale; applyCamera();
      return;
    }
    zoomAt(Math.exp(-event.deltaY * (event.deltaMode === 1 ? 0.05 : 0.0016)), event.clientX - bounds.left, event.clientY - bounds.top);
  }, { passive: false });
  let panDrag = null;
  viewport.addEventListener("pointerdown", event => {
    if (event.button !== 1 && event.button !== 2) return;
    event.preventDefault();
    panDrag = { id: event.pointerId, x: event.clientX, y: event.clientY };
    viewport.setPointerCapture(event.pointerId);
    viewport.classList.add("is-panning");
  });
  viewport.addEventListener("pointermove", event => {
    if (event.pointerId !== panDrag?.id) return;
    camera.follow = false;
    camera.x -= (event.clientX - panDrag.x) / camera.scale; camera.y -= (event.clientY - panDrag.y) / camera.scale;
    panDrag.x = event.clientX; panDrag.y = event.clientY;
    applyCamera();
  });
  const endPan = event => { if (event.pointerId === panDrag?.id) { panDrag = null; viewport.classList.remove("is-panning"); } };
  viewport.addEventListener("pointerup", endPan); viewport.addEventListener("pointercancel", endPan);
  viewport.addEventListener("contextmenu", event => event.preventDefault());
  viewport.addEventListener("keydown", event => {
    const step = 90 / camera.scale, moves = { ArrowLeft: [-step, 0], ArrowRight: [step, 0], ArrowUp: [0, -step], ArrowDown: [0, step] };
    if (!moves[event.key] || event.target !== viewport) return;
    event.preventDefault(); camera.follow = false;
    camera.x += moves[event.key][0]; camera.y += moves[event.key][1]; applyCamera();
  });
  svg.addEventListener("pointerdown", event => {
    if (event.pointerType !== "mouse" || event.button !== 0 || modal.hidden || encounterResolving || worldState.pendingEncounter || worldState.pendingRegionTransition || window.Combat?.active() || currentHp <= 0) return;
    cancelMapHold();
    suppressMapClickUntil = 0;
    if (event.target.closest(".map-guild-marker, .map-exit")) return;
    mapHold = { id: event.pointerId, clientX: event.clientX, clientY: event.clientY, active: false, lastPoint: null };
    mapHoldTimer = setTimeout(() => {
      if (!mapHold) return;
      mapHold.active = true;
      svg.classList.add("is-mouse-steering");
      try { svg.setPointerCapture(mapHold.id); } catch { /* The pointer may already have been released. */ }
      steerHeldMouse();
      if (mapHold) mapSteeringTimer = setInterval(steerHeldMouse, 150);
    }, 180);
  });
  window.addEventListener("pointermove", event => {
    if (event.pointerId !== mapHold?.id) return;
    if (!(event.buttons & 1)) { if (cancelMapHold()) saveCharacter(); return; }
    mapHold.clientX = event.clientX; mapHold.clientY = event.clientY;
  });
  const releaseMapHold = event => {
    if (event.pointerId === mapHold?.id && cancelMapHold()) saveCharacter();
  };
  window.addEventListener("pointerup", releaseMapHold);
  window.addEventListener("pointercancel", releaseMapHold);
  svg.addEventListener("lostpointercapture", releaseMapHold);
  window.addEventListener("blur", () => { if (cancelMapHold()) saveCharacter(); });
  modal.addEventListener("click", event => {
    if (event.detail > 0 && event.target.closest("#regional-map") && performance.now() < suppressMapClickUntil) return;
    if (event.target.closest("#map-character")) {
      if (worldState.destination) travelling ? pause() : resume();
      else $("map-status").textContent = 'Click anywhere on the map to begin travelling.';
      return;
    }
    const guildMarker = event.target.closest(".map-guild-marker");
    if (guildMarker) {
      destinationAt({ x: Number(guildMarker.dataset.mapX), y: Number(guildMarker.dataset.mapY) }, { snapToSettlement: false }); return;
    }
    const town = event.target.closest("[data-map-town]");
    if (town) {
      destinationAt(rules.settlementsForRegion(worldState.regionId).find(p => p.id === town.dataset.mapTown)); return;
    }
    const site = event.target.closest("[data-map-site]");
    if (site) { destinationAt((rules.getRegion(worldState.regionId).sites || []).find(p => p.id === site.dataset.mapSite)); return; }
    const exitTarget = event.target.closest("[data-map-exit]");
    if (exitTarget) { const exit = rules.exitById(exitTarget.dataset.mapExit, worldState.regionId); if (exit) destinationAt(exit.point, { snapToSettlement: false }); return; }
    if (!event.target.closest("#regional-map")) return;
    const matrix = svg.getScreenCTM();
    if (matrix) destinationAt(new DOMPoint(event.clientX, event.clientY).matrixTransform(matrix.inverse()));
  });
  modal.addEventListener("keydown", event => {
    const target = event.target.closest("#map-character, g[data-map-town], g[data-map-site], g[data-map-exit], .map-guild-marker");
    if (target && (event.key === "Enter" || event.key === " ")) { event.preventDefault(); target.dispatchEvent(new MouseEvent("click", { bubbles: true })); }
    if (event.key === "Escape") { event.preventDefault(); close(); }
    if (event.key === "Tab") {
      const focusable = [...modal.querySelectorAll('button:not(:disabled), [tabindex="0"]')].filter(el => !el.classList.contains("modal-backdrop"));
      const first = focusable[0], last = focusable.at(-1);
      if (event.shiftKey && document.activeElement === first) { event.preventDefault(); last.focus(); }
      else if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first.focus(); }
    }
  });
  encounterModal.addEventListener("keydown", event => {
    if (event.key === "Escape") { event.preventDefault(); return; }
    if (event.key !== "Tab") return;
    const controls = [...encounterModal.querySelectorAll("button:not(:disabled)")];
    const first = controls[0], last = controls.at(-1);
    if (event.shiftKey && document.activeElement === first) { event.preventDefault(); last?.focus(); }
    else if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first?.focus(); }
  });
  transitionModal.addEventListener("keydown", event => {
    if (event.key === "Escape") { event.preventDefault(); declineRegionTransition(); return; }
    if (event.key !== "Tab") return;
    const controls = [...transitionModal.querySelectorAll("button:not(:disabled):not([hidden])")];
    const first = controls[0], last = controls.at(-1);
    if (event.shiftKey && document.activeElement === first) { event.preventDefault(); last?.focus(); }
    else if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first?.focus(); }
  });
  document.addEventListener("keydown", event => {
    if (event.defaultPrevented || event.repeat || event.isComposing || event.ctrlKey || event.altKey || event.metaKey) return;
    if (event.target instanceof Element && event.target.closest('input, textarea, select, [contenteditable]:not([contenteditable="false"])')) return;
    const mapLayers = [modal, encounterModal, transitionModal];
    if ([...document.querySelectorAll(".modal-layer:not([hidden])")].some(layer => !mapLayers.includes(layer))) return;
    const key = event.key.toLowerCase();
    if (key === "m") {
      if (document.body.classList.contains("in-combat-view")) return;
      event.preventDefault();
      if (modal.hidden) $("map-open-button").click(); else close();
      return;
    }
    if (modal.hidden) return;
    let button;
    if (!encounterModal.hidden) button = key === "f" ? $("map-encounter-fight") : key === "r" ? $("map-encounter-run") : null;
    else if (!transitionModal.hidden) button = key === "enter" ? $("map-transition-travel") : key === "backspace" ? $("map-transition-stay") : null;
    else {
      const speedDigit = event.shiftKey ? /^Digit([1-4])$/.exec(event.code)?.[1] || (/^[1-4]$/.test(key) ? key : null) : null;
      const shortcutKey = speedDigit ? "shift+" + speedDigit : key;
      const shortcut = mapShortcuts.find(([, , keys]) => keys.includes(shortcutKey));
      button = shortcut ? $(shortcut[0]) : [...$("map-destinations").querySelectorAll("button")].find(entry => entry.dataset.mapHotkey === key);
    }
    if (!button || button.disabled || button.hidden || button.closest("[hidden]") || !button.getClientRects().length) return;
    event.preventDefault(); button.click();
  });
  document.addEventListener("visibilitychange", () => {
    ambientLast = performance.now(); ambientCarry = 0;
    if (document.hidden && travelling) pause();
  });
  new ResizeObserver(fitMap).observe($("map-viewport"));
  window.addEventListener("pagehide", () => pause());
  function advanceAmbientTime(now = performance.now()) {
    const elapsedSeconds = Math.max(0, (now - ambientLast) / 1000);
    ambientLast = now;
    if (!ambientEnabled || document.hidden || travelling || encounterResolving || worldState.pendingEncounter || worldState.pendingRegionTransition || window.Combat?.active()) {
      ambientCarry = 0;
      return;
    }
    ambientCarry += rules.realTimeMinutes(elapsedSeconds, worldState.timeSpeed);
    const elapsedMinutes = Math.floor(ambientCarry);
    if (elapsedMinutes < 1) return;
    ambientCarry -= elapsedMinutes;
    if (advanceWorldTime(elapsedMinutes)) {
      render();
      saveCharacter();
    }
  }
  window.setInterval(advanceAmbientTime, 250);
  window.WorldMap = {
    pause, render, renderClock, open,
    isTravelling: () => travelling,
    checkTravelEncounter, checkGuildObjective,
    setAmbientEnabled(value) {
      ambientEnabled = Boolean(value); ambientLast = performance.now(); ambientCarry = 0;
    },
    resetView() {
      pause(false); encounterResolving = false;
      ambientLast = performance.now(); ambientCarry = 0;
      if (window.Combat?.active()) worldState.pendingEncounter = null;
      if (worldState.destination) worldState.route = rules.routeBetween(worldState.position, worldState.destination, travelBonuses(), worldState.regionId);
      encounterModal.hidden = true;
      transitionModal.hidden = true;
      $("map-status").textContent = 'Roads are fastest; rivers, marsh, sand and volcanic slopes slow travel.';
      render();
    },
  };
  if (worldState.destination) worldState.route = rules.routeBetween(worldState.position, worldState.destination, travelBonuses(), worldState.regionId);
  render();
})();
