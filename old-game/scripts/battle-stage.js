/* The animated battlefield. Combat resolves its rules immediately; this view
   mirrors the battle as sprites and replays what happened as short "beats":
   an actor's animation, then its impacts (numbers, hits, deaths, summons and
   log lines). Enemy turns wait for the beats to finish, so the battle reads
   one action at a time. The speed-up button and the Combat animations
   setting skip every animation and delay. */
(() => {
  "use strict";
  const $ = id => document.getElementById(id);
  const view = $("combat-view"), stage = $("battle-stage"), field = $("battle-field"), fxLayer = $("battle-fx"), scenery = $("battle-scenery");
  const toasts = $("battle-log-toasts"), history = $("battle-log-history"), logPin = $("battle-log-pin"), logPanel = $("battle-log");
  const turnList = $("battle-turn-list"), speedButton = $("battle-speed"), banner = $("battle-banner"), announcer = $("battle-announcer");
  if (!stage) return;
  const reducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)");

  const sprites = new Map();
  const portraitCache = new Map();
  const pendingSpawns = new Set();
  const queue = [];
  let snapshot = null, openBeat = null, closing = false, playing = false, idleWaiters = [], sceneKey = "", historyKey = "";
  let layoutFrame = 0;

  const settings = () => window.GameSettings?.state() || {};
  const enabled = () => settings().battleUi !== "classic";
  const fastForward = () => settings().battleSpeed === "instant";
  const instant = () => fastForward() || settings().combatAnimations === false || reducedMotion.matches;
  const pace = () => settings().battlePace === "slow" ? 1.5 : 1;
  const sleep = ms => instant() || ms <= 0 ? Promise.resolve() : new Promise(resolve => setTimeout(resolve, ms * pace()));
  const escape = value => String(value ?? "").replace(/[&<>"']/g, char => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" })[char]);

  /* ---------------- Mode ---------------- */
  function applyMode() {
    const on = enabled();
    view.dataset.battleUi = on ? "stage" : "classic";
    stage.hidden = !on;
    speedButton.setAttribute("aria-pressed", String(fastForward()));
    speedButton.classList.toggle("is-on", fastForward());
    speedButton.title = fastForward() ? "Speed up is on: animations are skipped" : "Speed up: skip battle animations";
    stage.classList.toggle("is-instant", instant());
  }

  /* ---------------- Sprites ---------------- */
  function lookSignature(info) { return JSON.stringify([info.look, info.team]); }

  function createSprite(info) {
    const el = document.createElement("button");
    el.type = "button";
    el.className = "battle-actor team-" + (info.ally ? "ally" : "enemy");
    el.dataset.combatTarget = info.id;
    el.innerHTML = `<span class="battle-actor-shadow" aria-hidden="true"></span><span class="battle-actor-body"><span class="battle-actor-sprite"></span></span><span class="battle-actor-status" aria-hidden="true"></span><span class="battle-actor-marker" aria-hidden="true"></span><span class="battle-actor-plate"><span class="battle-actor-name"></span><span class="battle-bar hp" aria-hidden="true"><span></span></span>${info.ally ? '<span class="battle-bar mp" aria-hidden="true"><span></span></span>' : ""}</span>`;
    const delay = (Math.random() * -2.4).toFixed(2) + "s";
    el.style.setProperty("--idle-delay", delay);
    field.appendChild(el);
    const record = { id: info.id, el, body: el.querySelector(".battle-actor-body"), sprite: el.querySelector(".battle-actor-sprite"), plate: el.querySelector(".battle-actor-plate"), statusEl: el.querySelector(".battle-actor-status"), signature: "", team: info.team, ally: info.ally, order: 0, render: null, x: 0, y: 0, scale: 1, shownHp: info.hp, shownMana: info.mana, fallen: false };
    sprites.set(info.id, record);
    return record;
  }

  function renderSprite(record, info) {
    if (!window.Sprites) return;
    const look = info.look || {};
    const result = window.Sprites.forActor(look);
    record.render = result;
    record.signature = lookSignature(info);
    record.sprite.innerHTML = result.svg;
    record.sprite.classList.toggle("is-mirrored", !info.ally);
    record.el.classList.toggle("rig-" + (result.rig || "humanoid"), true);
    if (result.aura) record.el.style.setProperty("--aura", result.aura);
    layoutSoon();
  }

  function statusIcons(info) {
    const icons = [];
    for (const status of info.statuses || []) icons.push(`<span class="battle-status kind-${status.kind}" title="${escape(status.label)}">${escape(status.icon)}</span>`);
    return icons.slice(0, 6).join("");
  }

  function updateStates(record, info, idle) {
    const el = record.el;
    el.classList.toggle("is-current", info.current);
    el.classList.toggle("is-targeted", info.targeted);
    el.classList.toggle("is-summoned", Boolean(info.summoned));
    el.classList.toggle("is-unique", Boolean(info.unique));
    el.classList.toggle("is-transforming", Boolean(info.transforming));
    el.classList.toggle("pose-guard", info.stance === "Guard");
    el.classList.toggle("pose-evade", info.stance === "Evade");
    el.classList.toggle("is-stunned", (info.statuses || []).some(status => status.kind === "stun"));
    el.disabled = !info.targetable;
    el.setAttribute("aria-pressed", String(info.targeted));
    el.setAttribute("aria-label", (info.protected ? "Protected by summon: " : info.targetable ? "Target " : "") + info.name + (info.ally ? `, ${info.hp} of ${info.maxHp} HP, ${info.mana} of ${info.maxMana} Mana` : "") + (info.alive ? "" : ", fallen"));
    el.title = info.name;
    const name = record.plate.querySelector(".battle-actor-name");
    if (name.textContent !== info.name) name.textContent = info.name;
    record.statusEl.innerHTML = statusIcons(info);
    if (idle) {
      setBars(record, info.hp, info.maxHp, info.mana, info.maxMana);
      setFallen(record, !info.alive, false);
    }
  }

  function setBars(record, hp, maxHp, mana, maxMana) {
    record.shownHp = hp;
    const hpBar = record.plate.querySelector(".battle-bar.hp > span");
    const ratio = Math.max(0, Math.min(1, hp / Math.max(1, maxHp)));
    hpBar.style.transform = `scaleX(${ratio})`;
    hpBar.parentElement.classList.toggle("is-low", ratio <= 0.3);
    const mpBar = record.plate.querySelector(".battle-bar.mp > span");
    if (mpBar && maxMana != null) { record.shownMana = mana; mpBar.style.transform = `scaleX(${Math.max(0, Math.min(1, mana / Math.max(1, maxMana)))})`; }
  }

  function setFallen(record, fallen, animate = true) {
    if (record.fallen === fallen) return;
    record.fallen = fallen;
    record.el.classList.toggle("is-fallen", fallen);
    if (fallen && animate && !instant()) {
      const frames = record.ally
        ? [{ transform: "translateY(0) rotate(0deg)", opacity: 1 }, { transform: "translateY(-6%) rotate(-12deg)", opacity: 1, offset: 0.3 }, { transform: "translateY(18%) rotate(-82deg)", opacity: 0.75 }]
        : [{ transform: "translateY(0) scale(1)", opacity: 1, filter: "brightness(1)" }, { transform: "translateY(-4%) scale(1.04)", opacity: 1, filter: "brightness(2.2)", offset: 0.25 }, { transform: "translateY(8%) scale(0.92)", opacity: 0, filter: "brightness(1)" }];
      record.body.animate(frames, { duration: 620, easing: "ease-in", fill: "none" });
    }
  }

  function removeSprite(id) {
    const record = sprites.get(id);
    if (!record) return;
    record.el.remove();
    sprites.delete(id);
  }

  /* ---------------- Layout ---------------- */
  function layoutSoon() {
    cancelAnimationFrame(layoutFrame);
    layoutFrame = requestAnimationFrame(layout);
  }
  function layout() {
    if (stage.hidden) return;
    const W = stage.clientWidth, Hs = stage.clientHeight;
    if (!W || !Hs) return;
    const compact = W < 760;
    const turnWidth = compact ? 58 : 150;
    const fieldRight = W - turnWidth - 10;
    const groundTop = Hs * 0.47, groundBottom = Hs * 0.9;
    const unit = Hs / 285;
    for (const ally of [true, false]) {
      const members = [...sprites.values()].filter(record => record.ally === ally && record.render).sort((a, b) => a.order - b.order);
      // Foes are placed largest-last so dragons and giants stand behind smaller fighters.
      if (!ally) members.sort((a, b) => (a.render.size * a.render.box.w) - (b.render.size * b.render.box.w) || a.order - b.order);
      const count = members.length;
      const crowd = count > 6 ? 0.72 : count > 4 ? 0.8 : count > 2 ? 0.9 : 1;
      const half = fieldRight / 2;
      const slots = formation(count);
      members.forEach((record, index) => {
        const [across, depth] = slots[index];
        const x = ally ? half * across : fieldRight - half * across;
        const y = groundTop + (groundBottom - groundTop) * Math.min(1, depth);
        const box = record.render.box;
        let scale = unit * record.render.size * crowd * (0.88 + 0.12 * Math.min(1, depth));
        const crowded = count > 4 ? [0.44, 0.5] : count > 2 ? [0.56, 0.58] : [0.86, 0.66];
        scale = Math.min(scale, (half * crowded[0]) / box.w, (Hs * crowded[1]) / box.h);
        record.x = x; record.y = y; record.scale = scale;
        const width = box.w * scale, height = box.h * scale;
        const feetX = record.ally ? -box.x * scale : width + box.x * scale;
        const feetY = -box.y * scale;
        const el = record.el;
        el.style.left = (x - feetX) + "px";
        el.style.top = (y - feetY) + "px";
        el.style.width = width + "px";
        el.style.height = height + "px";
        el.style.setProperty("--feet-x", feetX + "px");
        el.style.setProperty("--feet-y", feetY + "px");
        el.style.setProperty("--sprite-scale", scale);
        // Parts stack by depth; name plates rise above every figure (see styles.css).
        el.style.setProperty("--depth", String(10 + Math.round(depth * 100)));
      });
    }
    stage.style.setProperty("--turn-width", turnWidth + "px");
    stage.classList.toggle("is-compact", compact);
  }

  /* Battle formations for one side: [across, depth] where across runs from the
     screen edge (0) to the centre line (1) and depth from the back (0) to the
     front (1) of the ground. Up to eight fighters have designed places. */
  const FORMATIONS = {
    1: [[0.62, 0.55]],
    2: [[0.7, 0.3], [0.5, 0.82]],
    3: [[0.74, 0.5], [0.47, 0.16], [0.45, 0.88]],
    4: [[0.76, 0.3], [0.72, 0.86], [0.46, 0.1], [0.42, 0.6]],
    5: [[0.78, 0.5], [0.6, 0.1], [0.58, 0.92], [0.36, 0.34], [0.34, 0.76]],
    6: [[0.78, 0.3], [0.76, 0.86], [0.55, 0.08], [0.53, 0.6], [0.3, 0.32], [0.28, 0.9]],
    7: [[0.8, 0.5], [0.62, 0.1], [0.6, 0.92], [0.42, 0.34], [0.4, 0.78], [0.22, 0.08], [0.2, 0.6]],
    8: [[0.8, 0.28], [0.78, 0.84], [0.6, 0.06], [0.58, 0.56], [0.4, 0.3], [0.38, 0.9], [0.2, 0.08], [0.18, 0.6]],
  };
  function formation(count) {
    if (FORMATIONS[count]) return FORMATIONS[count];
    // Larger custom battles fall back to a staggered grid of two rows.
    const columns = Math.ceil(count / 2);
    return Array.from({ length: count }, (_, index) => {
      const column = Math.floor(index / 2), row = index % 2;
      return [0.84 - column * (0.74 / Math.max(1, columns - 1)), row ? 0.82 : 0.22 + (column % 2) * 0.12];
    });
  }

  // Page coordinates (relative to the stage) of a point on an actor's sprite.
  function anchor(id, which = "chest") {
    const record = sprites.get(id);
    if (!record || !record.render) return null;
    const box = record.render.box;
    const point = record.render.anchors?.[which] || record.render.anchors?.chest || [0, box.y + box.h * 0.5];
    const left = parseFloat(record.el.style.left) || 0, top = parseFloat(record.el.style.top) || 0;
    const sx = (point[0] - box.x) * record.scale;
    return { x: left + (record.ally ? sx : box.w * record.scale - sx), y: top + (point[1] - box.y) * record.scale, record };
  }

  /* ---------------- Scenery ---------------- */
  function updateScene(snap) {
    const key = [snap.region, snap.zone, window.BattleScenery?.timeKey?.(snap.minutes) || ""].join("|");
    if (key === sceneKey) return;
    sceneKey = key;
    scenery.innerHTML = window.BattleScenery ? window.BattleScenery.render(snap.region, snap.zone, snap.minutes) : "";
    stage.dataset.biome = window.BattleScenery?.biome(snap.region, snap.zone) || "plains";
  }

  /* ---------------- Seasons ----------------
     A boss that turns the seasons (Juila) shows the current season above the battlefield, the
     coming one once it is foretold, and tints the field with drifting petals, motes, leaves or
     snow (styles.css, "Battle seasons"). */
  let seasonBadge = null, seasonFx = null;
  function updateSeason(snap) {
    const season = snap.season || null;
    if (!seasonBadge) {
      seasonFx = document.createElement("div"); seasonFx.className = "battle-season-fx"; seasonFx.setAttribute("aria-hidden", "true");
      seasonBadge = document.createElement("p"); seasonBadge.className = "battle-season";
      scenery.after(seasonFx); stage.append(seasonBadge);
    }
    stage.dataset.season = season?.id || "";
    seasonBadge.hidden = !season;
    if (!season) return;
    const markup = `<span class="battle-season-name">${escape(season.name)}</span>${season.foretold ? `<span class="battle-season-next">${escape(season.next)} next</span>` : ""}`;
    if (seasonBadge.innerHTML !== markup) seasonBadge.innerHTML = markup;
    seasonBadge.title = season.description || season.name;
  }

  /* ---------------- Warband orders ----------------
     A warband chief (Zik Lun) shows his current order above the battlefield: its name, the
     hero his warband hunts, and whether the war drums beat it louder. */
  let orderBadge = null;
  function updateOrder(snap) {
    const order = snap.order || null;
    if (!orderBadge) { orderBadge = document.createElement("p"); orderBadge.className = "battle-season battle-order"; stage.append(orderBadge); }
    stage.dataset.order = order?.id || "";
    orderBadge.hidden = !order;
    if (!order) return;
    const markup = `<span class="battle-season-name">${escape(order.name)}</span>${order.prey ? `<span class="battle-season-next">${escape(order.preyLabel || "Hunting")} ${escape(order.prey)}</span>` : ""}${order.turnsLeft ? `<span class="battle-season-next">${order.turnsLeft} ${order.unit || "turn"}${order.turnsLeft === 1 ? "" : "s"}</span>` : ""}${order.drums ? `<span class="battle-season-next">War drums</span>` : ""}`;
    if (orderBadge.innerHTML !== markup) orderBadge.innerHTML = markup;
    orderBadge.title = order.description || order.name;
  }

  /* ---------------- Turn order ---------------- */
  function portraitFor(id, look) {
    const key = id + JSON.stringify(look || {});
    if (!portraitCache.has(key)) portraitCache.set(key, window.Sprites ? window.Sprites.portrait(look || {}) : "");
    return portraitCache.get(key);
  }
  function renderTurns(snap) {
    const byId = new Map(snap.actors.map(actor => [actor.id, actor]));
    const entries = [];
    if (snap.current && byId.has(snap.current)) entries.push({ id: snap.current, now: true });
    for (const turn of snap.turnOrder || []) entries.push({ id: turn.id });
    const markup = entries.slice(0, 11).map((entry, index) => {
      const actor = byId.get(entry.id);
      if (!actor) return "";
      return `<li class="${entry.now ? "is-now" : ""} ${actor.ally ? "team-ally" : "team-enemy"}"><span class="battle-turn-portrait">${portraitFor(actor.id, actor.look)}</span><span class="battle-turn-name">${escape(actor.label)}</span><small>${entry.now ? "Now" : index}</small></li>`;
    }).join("");
    if (turnList.innerHTML !== markup) turnList.innerHTML = markup;
  }

  /* ---------------- Log ---------------- */
  function renderHistory(log = []) {
    const key = log.length + "|" + (log[0] || "") + "|" + (log.at(-1) || "");
    if (key === historyKey) return;
    const pinnedToBottom = history.scrollHeight - history.scrollTop - history.clientHeight < 24;
    historyKey = key;
    history.innerHTML = log.map(entry => `<li class="${logKind(entry)}">${escape(entry)}</li>`).join("");
    if (pinnedToBottom || !logPanel.classList.contains("is-open")) history.scrollTop = history.scrollHeight;
  }
  function logKind(message = "") {
    if (/ falls\.|defeated|returns from where/.test(message)) return "log-fall";
    if (/turn —/.test(message)) return "log-turn";
    if (/heal|restores|recovers/i.test(message)) return "log-heal";
    if (/casts|channels|summons/.test(message)) return "log-spell";
    if (/misses|fails to escape|interrupts/.test(message)) return "log-miss";
    if (/damage/.test(message)) return "log-hit";
    if (/Victory|Defeat|Escaped/.test(message)) return "log-result";
    return "log-note";
  }
  function toast(message) {
    const item = document.createElement("li");
    item.className = "battle-toast " + logKind(message);
    item.textContent = message;
    toasts.appendChild(item);
    while (toasts.children.length > 4) toasts.firstElementChild.remove();
    const remove = () => item.remove();
    item.addEventListener("animationend", event => { if (event.animationName === "battle-toast-life") remove(); });
    setTimeout(remove, 9000);
    announcer.textContent = message;
  }
  function setLogOpen(open) {
    logPanel.classList.toggle("is-open", open);
    logPin.setAttribute("aria-expanded", String(open));
    if (open) history.scrollTop = history.scrollHeight;
  }
  logPin.addEventListener("click", () => {
    const pinned = !logPanel.classList.contains("is-pinned");
    logPanel.classList.toggle("is-pinned", pinned);
    setLogOpen(pinned);
  });
  logPanel.addEventListener("pointerenter", () => setLogOpen(true));
  logPanel.addEventListener("pointerleave", () => { if (!logPanel.classList.contains("is-pinned") && !logPanel.contains(document.activeElement)) setLogOpen(false); });
  logPanel.addEventListener("focusin", () => setLogOpen(true));
  logPanel.addEventListener("focusout", event => { if (!logPanel.contains(event.relatedTarget) && !logPanel.classList.contains("is-pinned")) setLogOpen(false); });

  /* ---------------- Floating text ---------------- */
  function floatText(id, text, kind = "damage") {
    const point = anchor(id, "top") || anchor(id, "chest");
    if (!point) return;
    const el = document.createElement("span");
    el.className = "battle-float float-" + kind;
    el.textContent = text;
    const spread = (Math.random() - 0.5) * 26;
    el.style.left = (point.x + spread) + "px";
    el.style.top = (point.y - 6) + "px";
    fxLayer.appendChild(el);
    const remove = () => el.remove();
    el.addEventListener("animationend", remove);
    setTimeout(remove, 2600);
  }

  /* ---------------- Animations ---------------- */
  function pose(record, name, ms) {
    if (!record || instant()) return;
    const el = record.el;
    el.classList.remove(name);
    void el.offsetWidth;
    el.style.setProperty("--anim-ms", Math.round(ms * pace()) + "ms");
    el.classList.add(name);
    setTimeout(() => el.classList.remove(name), ms * pace() + 40);
  }
  function move(record, dx, dy, ms, hold = 0.35) {
    if (!record || instant() || (!dx && !dy)) return Promise.resolve();
    const animation = record.body.animate([
      { transform: "translate(0, 0)" },
      { transform: `translate(${dx}px, ${dy}px)`, offset: hold },
      { transform: `translate(${dx}px, ${dy}px)`, offset: 1 - hold },
      { transform: "translate(0, 0)" },
    ], { duration: ms * pace(), easing: "ease-in-out" });
    return animation.finished.catch(() => {});
  }
  function hurt(record, critical) {
    if (!record || instant()) return;
    const dir = record.ally ? -1 : 1;
    record.body.animate([
      { transform: "translate(0,0)" },
      { transform: `translate(${dir * (critical ? 14 : 8)}px, -2px)`, offset: 0.25 },
      { transform: `translate(${dir * -3}px, 0)`, offset: 0.55 },
      { transform: "translate(0,0)" },
    ], { duration: (critical ? 420 : 320) * pace(), easing: "ease-out" });
    pose(record, "anim-hurt", critical ? 420 : 320);
  }
  function showBanner(text, kind = "") {
    if (instant()) return;
    banner.textContent = text;
    banner.className = "battle-banner is-showing " + kind;
    setTimeout(() => { banner.className = "battle-banner"; }, 1400 * pace());
  }

  /* ---------------- Events and beats ---------------- */
  const BEAT_TYPES = new Set(["turn", "strike", "spell", "item", "stance", "meditate", "run", "skill", "phase", "finish", "interrupt"]);
  function event(type, detail = {}) {
    if (!enabled()) return;
    if (type === "start") { reset(); return; }
    if (BEAT_TYPES.has(type)) {
      openBeat = { type, detail, impacts: [] };
      queue.push(openBeat);
      scheduleClose();
    } else {
      if (!openBeat) { openBeat = { type: "ambient", detail: {}, impacts: [] }; queue.push(openBeat); scheduleClose(); }
      if (type === "summon") pendingSpawns.add(detail.actorId);
      openBeat.impacts.push({ type, detail });
    }
    pump();
  }
  function scheduleClose() {
    if (closing) return;
    closing = true;
    queueMicrotask(() => { openBeat = null; closing = false; });
  }
  function pump() {
    if (playing) return;
    playing = true;
    // Let the synchronous combat action finish reporting before playback.
    setTimeout(async () => {
      try {
        while (queue.length) {
          const beat = queue.shift();
          try { await perform(beat); } catch (error) { console.error("Battle stage beat failed", error); }
        }
      } finally {
        playing = false;
        reconcile();
        const waiters = idleWaiters;
        idleWaiters = [];
        waiters.forEach(callback => { try { callback(); } catch (error) { console.error(error); } });
      }
    }, 0);
  }
  function busy() { return playing || queue.length > 0; }
  function whenIdle(callback) { if (!busy()) callback(); else idleWaiters.push(callback); }
  function reset() {
    queue.length = 0;
    openBeat = null;
    pendingSpawns.clear();
    for (const id of [...sprites.keys()]) removeSprite(id);
    toasts.innerHTML = "";
    fxLayer.innerHTML = "";
    historyKey = "";
    sceneKey = "";
    portraitCache.clear();
  }
  function reconcile() {
    if (!snapshot) return;
    pendingSpawns.clear();
    sync(snapshot);
  }

  // Applies one impact: numbers, hit reactions, deaths, summons and log lines.
  function impact(entry) {
    const { type, detail } = entry;
    const record = sprites.get(detail.actorId);
    if (type === "log") { toast(detail.message); return; }
    if (type === "season") { if (detail.name) showBanner(detail.name, "is-season season-" + detail.season); return; }
    if (type === "season-omen") return;
    if (type === "order") { if (detail.name) showBanner(detail.name, "is-order order-" + detail.order); return; }
    if (type === "summon") {
      pendingSpawns.delete(detail.actorId);
      if (record) {
        record.el.classList.remove("is-spawning");
        if (!instant()) record.body.animate([{ transform: "translateY(-14%) scale(0.6)", opacity: 0, filter: "brightness(2.5)" }, { transform: "translateY(0) scale(1)", opacity: 1, filter: "brightness(1)" }], { duration: 520 * pace(), easing: "cubic-bezier(.2,1.4,.4,1)" });
      }
      return;
    }
    if (!record) return;
    if (type === "hp") {
      const delta = detail.after - detail.before;
      if (delta < 0) {
        floatText(detail.actorId, String(-delta), detail.critical ? "crit" : "damage");
        hurt(record, detail.critical);
        window.BattleFx?.hitSpark?.(api, detail.actorId, detail.element, detail.critical);
      } else if (delta > 0) floatText(detail.actorId, "+" + delta, "heal");
      const info = snapshot?.actors.find(actor => actor.id === detail.actorId);
      setBars(record, detail.after, detail.max ?? info?.maxHp ?? Math.max(1, detail.after), record.shownMana, info?.maxMana);
      if (detail.after <= 0) setFallen(record, true, true);
      else if (record.fallen) setFallen(record, false, false);
    } else if (type === "mana") {
      if (detail.after > detail.before && detail.visible !== false) floatText(detail.actorId, "+" + (detail.after - detail.before) + " MP", "mana");
      const info = snapshot?.actors.find(actor => actor.id === detail.actorId);
      if (record.plate.querySelector(".battle-bar.mp")) setBars(record, record.shownHp, info?.maxHp ?? 1, detail.after, detail.max ?? info?.maxMana);
    } else if (type === "miss") {
      floatText(detail.actorId, "Miss", "miss");
      if (!instant()) record.body.animate([{ transform: "translate(0,0)" }, { transform: `translate(${record.ally ? -18 : 18}px, -6px)`, offset: 0.4 }, { transform: "translate(0,0)" }], { duration: 380 * pace(), easing: "ease-out" });
    } else if (type === "status") {
      floatText(detail.actorId, detail.label, detail.debuff ? "debuff" : "buff");
    } else if (type === "equipment") {
      window.BattleFx?.masterwork?.(api, detail);
    } else if (type === "stunned") {
      floatText(detail.actorId, "Stunned", "debuff");
    }
  }
  function flush(beat) {
    for (const entry of beat.impacts.splice(0)) impact(entry);
  }

  async function perform(beat) {
    const d = beat.detail || {};
    const actor = sprites.get(d.actorId);
    const target = sprites.get(d.targetId);
    if (beat.type === "ambient" || !actor && beat.type !== "finish") {
      flush(beat);
      if (beat.impacts.length || !instant()) await sleep(beat.type === "turn" ? 80 : 160);
      return;
    }
    switch (beat.type) {
      case "turn": {
        flush(beat);
        if (d.controlled) showBanner(d.actorId === "player" ? "Your turn" : `${actor.el.title}'s turn`, "is-ally");
        await sleep(120);
        return;
      }
      case "strike": {
        const ranged = d.ranged;
        if (ranged) {
          pose(actor, d.weaponKind === "crossbow" ? "anim-shoot" : "anim-shoot", 420);
          await sleep(240);
          await (window.BattleFx?.projectile?.(api, d.actorId, d.targetId, "arrow", d.element) || sleep(180));
          flush(beat);
          await sleep(220);
        } else {
          const from = anchor(d.actorId, "chest"), to = target ? anchor(d.targetId, "chest") : null;
          let dx = 0, dy = 0;
          if (from && to) {
            const reach = (target.render.box.w * target.scale) * 0.42 + 26;
            dx = (to.x - from.x) - Math.sign(to.x - from.x || 1) * reach;
            dy = (target.y - actor.y) * 0.85;
          }
          const unarmed = d.unarmed;
          const beast = actor.render?.rig && actor.render.rig !== "humanoid";
          move(actor, dx, dy, 640, 0.32);
          await sleep(200);
          pose(actor, beast ? "anim-bite" : unarmed ? "anim-punch" : d.offHand ? "anim-offhand" : "anim-attack", 380);
          await sleep(200);
          flush(beat);
          await sleep(300);
        }
        return;
      }
      case "spell": {
        pose(actor, "anim-cast", 620);
        window.BattleFx?.castCircle?.(api, d);
        await sleep(300);
        const summonIds = beat.impacts.filter(entry => entry.type === "summon").map(entry => entry.detail.actorId);
        const played = window.BattleFx?.spell ? window.BattleFx.spell(api, { ...d, summonIds }) : null;
        if (played) {
          await played.impact;
          flush(beat);
          await played.done;
        } else { await sleep(260); flush(beat); await sleep(200); }
        return;
      }
      case "item": {
        const potion = Boolean(d.potion);
        pose(actor, potion ? "anim-drink" : "anim-item", potion ? 760 : 520);
        window.BattleFx?.item?.(api, d);
        await sleep(potion ? 560 : 360);
        flush(beat);
        await sleep(220);
        return;
      }
      case "stance": {
        pose(actor, d.stance === "Guard" ? "anim-guard" : "anim-evade", 460);
        window.BattleFx?.stance?.(api, d);
        await sleep(300);
        flush(beat);
        await sleep(200);
        return;
      }
      case "meditate": {
        pose(actor, "anim-meditate", 760);
        window.BattleFx?.meditate?.(api, d);
        await sleep(520);
        flush(beat);
        await sleep(220);
        return;
      }
      case "skill": {
        pose(actor, "anim-skill", 560);
        window.BattleFx?.skill?.(api, d);
        await sleep(340);
        flush(beat);
        await sleep(240);
        return;
      }
      case "run": {
        if (d.success) {
          for (const record of sprites.values()) if (record.ally && !record.fallen && !instant()) record.body.animate([{ transform: "translateX(0) scaleX(1)" }, { transform: "translateX(-10px) scaleX(-1)", offset: 0.2 }, { transform: "translateX(-60vw) scaleX(-1)" }], { duration: 900 * pace(), easing: "ease-in", fill: "forwards" });
          await sleep(700);
        } else {
          pose(actor, "anim-stumble", 460);
          await sleep(380);
        }
        flush(beat);
        return;
      }
      case "phase": {
        pose(actor, "anim-phase", 900);
        await sleep(500);
        flush(beat);
        await sleep(300);
        return;
      }
      case "interrupt": {
        pose(actor, "anim-stumble", 420);
        flush(beat);
        await sleep(300);
        return;
      }
      case "finish": {
        flush(beat);
        const won = /^Victory/.test(d.result || ""), lost = /^Defeat/.test(d.result || "");
        if (won) for (const record of sprites.values()) if (record.ally && !record.fallen) pose(record, "anim-cheer", 1200);
        showBanner(won ? "Victory" : lost ? "Defeat" : "Escaped", won ? "is-victory" : lost ? "is-defeat" : "");
        await sleep(400);
        return;
      }
    }
    flush(beat);
  }

  /* ---------------- Sync ---------------- */
  function sync(snap) {
    snapshot = snap;
    applyMode();
    if (!snap || !enabled()) { if (!snap) reset(); return; }
    if (stage.hidden) return;
    // A finished battle keeps its battlefield even if the party has moved on.
    if (snap.active || !sceneKey) updateScene(snap);
    updateSeason(snap);
    updateOrder(snap);
    const idle = !busy();
    const seen = new Set();
    snap.actors.forEach((info, index) => {
      seen.add(info.id);
      let record = sprites.get(info.id);
      if (!record) {
        record = createSprite(info);
        if (pendingSpawns.has(info.id)) record.el.classList.add("is-spawning");
      }
      record.order = index;
      record.team = info.team;
      if (record.signature !== lookSignature(info)) renderSprite(record, info);
      updateStates(record, info, idle && !pendingSpawns.has(info.id));
    });
    for (const id of [...sprites.keys()]) if (!seen.has(id) && idle) removeSprite(id);
    layout();
    renderTurns(snap);
    renderHistory(snap.log);
  }

  /* ---------------- Controls ---------------- */
  field.addEventListener("click", event => {
    const button = event.target.closest("[data-combat-target]");
    if (!button || button.disabled) return;
    window.Combat?.chooseTargetId?.(button.dataset.combatTarget);
  });
  speedButton.addEventListener("click", () => {
    window.GameSettings?.setBattleSpeed(fastForward() ? "normal" : "instant");
  });
  window.addEventListener("statfick:settingschange", () => {
    applyMode();
    if (snapshot) sync(snapshot);
  });
  window.addEventListener("resize", layoutSoon);
  if (window.ResizeObserver) new ResizeObserver(layoutSoon).observe(stage);

  const api = { anchor, sprites, fxLayer, stage, sleep, instant, pace, pose, floatText, toast, get snapshot() { return snapshot; } };
  window.BattleStage = {
    sync, event, busy, whenIdle, reset, enabled, instant, api, logKind,
    // Pause between enemy actions; the beats themselves supply most pacing.
    enemyDelay(firstAction = false) {
      if (fastForward()) return 0;
      // Without animations the original reading delays keep the log legible.
      if (instant()) return window.GameSettings?.enemyDelay(firstAction) ?? 650;
      return settings().battlePace === "slow" ? (firstAction ? 1100 : 750) : (firstAction ? 420 : 260);
    },
  };
  applyMode();
})();
