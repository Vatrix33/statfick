/* Brief, non-blocking spell vignettes. Combat results are applied immediately. */
(() => {
  const stage = document.getElementById("tier10-cinematic");
  const art = stage?.querySelector(".tier10-cinematic-art");
  const title = stage?.querySelector(".tier10-cinematic-name");
  const caption = stage?.querySelector(".tier10-cinematic-caption");
  let timer = null;

  // Every mythical spell has its own silhouette and motion in styles.css.
  const scenes = {
    "axiom-unbound-basic-damage": {
      key: "axiom", color: "#c4b2ff", caption: "The laws of creation come apart.",
      art: `<g class="t10-symbol"><circle cx="280" cy="108" r="62"/><path d="m280 30 68 39v78l-68 39-68-39V69zM218 69l124 78m0-78-124 78M280 30v156"/></g><g class="t10-detail"><path d="m202 23-18-15m164 8 25-19M178 91l-35-5m240 4 35-6M194 176l-25 18m197-22 27 18M278 9V0m2 205v15"/><circle cx="280" cy="108" r="90" stroke-dasharray="8 13"/></g>`
    },
    "the-first-flame-fire-damage": {
      key: "flame", color: "#ff9958", caption: "The first fire remembers its name.",
      art: `<g class="t10-symbol"><path fill="currentColor" fill-opacity=".3" d="M280 26c-21 31 18 42-9 76-10-23-27-24-28-45-52 52-47 128 35 135 69 6 87-75 41-123 3 38-20 44-25 53 9-37-21-47-14-96Z"/><path d="M280 26c-21 31 18 42-9 76-10-23-27-24-28-45-52 52-47 128 35 135 69 6 87-75 41-123 3 38-20 44-25 53 9-37-21-47-14-96Z"/><path d="M280 107c-8 20-29 36-23 58 6 28 47 31 58 5 10-23-8-39-21-52-1 21-14 29-14 29 6-18-8-27 0-40Z"/></g><g class="t10-detail" fill="currentColor" stroke="none"><circle cx="206" cy="91" r="4"/><circle cx="353" cy="55" r="5"/><circle cx="379" cy="142" r="3"/><circle cx="172" cy="143" r="3"/><circle cx="331" cy="18" r="3"/></g>`
    },
    "ocean-without-shores-water-support": {
      key: "ocean", color: "#72d9e9", caption: "Every river returns to its source.",
      art: `<g class="t10-symbol"><path d="M58 131c37-43 73-43 111 0s76 43 114 0 77-43 115 0 72 43 105 0"/><path d="M58 158c37-43 73-43 111 0s76 43 114 0 77-43 115 0 72 43 105 0"/><path d="M58 185c37-43 73-43 111 0s76 43 114 0 77-43 115 0 72 43 105 0"/></g><g class="t10-detail"><circle cx="280" cy="86" r="39"/><circle cx="280" cy="86" r="63" stroke-dasharray="8 10"/><path d="M280 17c-15 23-20 34-20 46a20 20 0 0 0 40 0c0-12-5-23-20-46Z" fill="currentColor" fill-opacity=".2"/></g>`
    },
    "worldheart-bastion-earth-support": {
      key: "bastion", color: "#d9b985", caption: "The world itself becomes a shield.",
      art: `<g class="t10-symbol"><path fill="currentColor" fill-opacity=".16" d="m280 22 80 28 17 87-97 64-97-64 17-87z"/><path d="m280 22 80 28 17 87-97 64-97-64 17-87zM280 47v123m-55-97 55-26 55 26m-84 83 29 14 29-14"/><path d="m183 137-51 36 34 28 71-23m140-41 51 36-34 28-71-23"/></g><g class="t10-detail"><path d="m164 50-45 52 37 23m240-75 45 52-37 23M280 6V0m0 214v6"/><path d="m200 50-28-27m188 27 28-27"/></g>`
    },
    "crown-of-the-open-sky-wind-damage": {
      key: "sky", color: "#96e7be", caption: "No horizon can hold the wind.",
      art: `<g class="t10-symbol"><path d="M146 125c45-94 205-103 270-18 38 50-12 102-72 80-36-13-21-56 8-49 13 3 15 17 8 24"/><path d="M172 156c65 74 186 54 227-14M123 80c70-72 217-80 296-9"/></g><g class="t10-detail"><path d="m235 68 18-37 27 27 27-27 18 37-45 17z" fill="currentColor" fill-opacity=".23"/><path d="m235 68 18-37 27 27 27-27 18 37-45 17zM149 43l-25 4m301 6 25-5"/></g>`
    },
    "the-unending-winter-ice-damage": {
      key: "winter", color: "#bbebff", caption: "The last snowfall never melts.",
      art: `<g class="t10-symbol"><path d="M280 20v176M192 64l176 88m0-88-176 88M280 20l-17 17m17-17 17 17m-17 159-17-17m17 17 17-17M192 64l23 1m-23-1 11 21m165-21-23 1m23-1-11 21M192 152l23-1m-23 1 11-21m165 21-23-1m23 1-11-21"/><path d="m280 62 40 25v46l-40 25-40-25V87z" fill="currentColor" fill-opacity=".15"/></g><g class="t10-detail"><path d="m176 30-29-15m242 0-29 15M145 112h-38m346 0h-38m-239 82-29 15m242 0-29-15"/></g>`
    },
    "judgment-of-the-storm-king-lightning-damage": {
      key: "storm", color: "#e7d37a", caption: "The sky passes judgment.",
      art: `<g class="t10-symbol"><path d="m276 4-37 83h36l-24 103 83-112h-45l27-74z" fill="currentColor" fill-opacity=".35"/><path d="m276 4-37 83h36l-24 103 83-112h-45l27-74z"/><path d="m171 24-25 58h26l-18 60 56-79h-29l13-39m193 0-25 58h26l-18 60 56-79h-29l13-39"/></g><g class="t10-detail"><path d="M125 191h310M91 175h64m252 0h64m-225 29h67"/><circle cx="280" cy="112" r="91" stroke-dasharray="3 14"/></g>`
    },
    "genesis-dawn-holy-damage": {
      key: "dawn", color: "#ffe8a6", caption: "The first dawn rises again.",
      art: `<g class="t10-symbol"><path d="M70 173h420M172 173a108 108 0 0 1 216 0" fill="currentColor" fill-opacity=".24"/><path d="M172 173a108 108 0 0 1 216 0M280 14v45m-92-18 34 36m-96 29 47 19m261-19-47 19m-14-84-34 36"/></g><g class="t10-detail"><path d="M280 55v112M220 111h120M99 193h362" stroke-width="4"/><circle cx="280" cy="112" r="32" fill="currentColor" fill-opacity=".16"/></g>`
    },
    "eventide-absolute-dark-damage": {
      key: "eclipse", color: "#bd9bdf", caption: "The final night closes over the stars.",
      art: `<g class="t10-symbol"><circle cx="280" cy="107" r="74" fill="currentColor" fill-opacity=".28"/><circle cx="280" cy="107" r="74"/><path d="M280 33a74 74 0 1 0 0 148 64 64 0 1 1 0-148Z" fill="#100e1d" stroke="none"/></g><g class="t10-detail"><path d="m147 36 6-13 6 13 13 6-13 6-6 13-6-13-13-6zm260 128 5-10 5 10 10 5-10 5-5 10-5-10-10-5z" fill="currentColor" fill-opacity=".6"/><circle cx="280" cy="107" r="94" stroke-dasharray="2 14"/></g>`
    },
    "worldseed-miracle-nature-support": {
      key: "worldseed", color: "#98d88b", caption: "A forest begins in a single breath.",
      art: `<g class="t10-symbol"><path d="M280 197v-99m0 38-59-49m59 40 59-59M221 87l-34-29m152 10 32-31M280 164l-39-22m39 10 42-22"/><path d="M168 196q112-43 224 0M280 98q-31-36-48-64 44 1 48 64Zm0 0q27-45 51-67-2 48-51 67Zm-59-11q-29-13-46-40 37-4 46 40Zm118-19q17-29 47-37-5 37-47 37Z" fill="currentColor" fill-opacity=".19"/></g><g class="t10-detail"><circle cx="280" cy="165" r="5" fill="currentColor"/><path d="m268 183-36 22m61-23 37 23"/></g>`
    },
    "blood-sun-apotheosis-blood-damage": {
      key: "bloodsun", color: "#f26c7e", caption: "The crimson sun drinks the world.",
      art: `<g class="t10-symbol"><circle cx="280" cy="106" r="65" fill="currentColor" fill-opacity=".22"/><circle cx="280" cy="106" r="65"/><path d="M216 105c41-29 87 30 128 1m-129 29c43-26 90 20 131-11M280 41v130" stroke-width="4"/></g><g class="t10-detail" fill="currentColor" fill-opacity=".4"><path d="M167 38c-11 17-15 25-15 34a15 15 0 0 0 30 0c0-9-4-17-15-34Zm223 5c-11 17-15 25-15 34a15 15 0 0 0 30 0c0-9-4-17-15-34Zm-41 123c-9 14-12 22-12 29a12 12 0 0 0 24 0c0-7-3-15-12-29Zm-146-8c-9 14-12 22-12 29a12 12 0 0 0 24 0c0-7-3-15-12-29Z"/></g>`
    },
    "silence-at-the-end-death-damage": {
      key: "silence", color: "#c7b9d0", caption: "Even time forgets its final name.",
      art: `<g class="t10-symbol"><path d="M210 20h140m-140 178h140M223 20c0 59 56 52 57 89-1 37-57 30-57 89m114-178c0 59-56 52-57 89 1 37 57 30 57 89"/><path d="m241 62 39 43 39-43Zm0 118 39-50 39 50Z" fill="currentColor" fill-opacity=".24"/><path d="M280 103v15" stroke-width="4"/></g><g class="t10-detail"><path d="m151 40 15 15m243-15-15 15M149 170l17-15m243 15-17-15M280 0v12m0 196v12"/><circle cx="280" cy="109" r="96" stroke-dasharray="1 18"/></g>`
    }
  };

  function stop() {
    clearTimeout(timer);
    timer = null;
    if (!stage) return;
    stage.hidden = true;
    stage.classList.remove("is-playing");
  }

  function play(spell, caster = "Caster") {
    if (window.GameSettings?.state().combatAnimations === false) return false;
    const scene = spell?.tier === 10 ? scenes[spell.id] : null;
    if (!stage || !scene || !art || !title || !caption) return false;
    stop();
    stage.dataset.spellId = spell.id;
    stage.dataset.scene = scene.key;
    stage.style.setProperty("--t10-accent", scene.color);
    art.innerHTML = `<svg viewBox="0 0 560 220" xmlns="http://www.w3.org/2000/svg" focusable="false" aria-hidden="true"><circle class="t10-aura" cx="280" cy="110" r="105"/>${scene.art}</svg>`;
    title.textContent = spell.name;
    caption.textContent = `${caster} · ${scene.caption}`;
    stage.hidden = false;
    const duration = window.matchMedia?.("(prefers-reduced-motion: reduce)").matches ? 650 : 1900;
    // Cleanup is independent of CSS animation events, so a hidden tab cannot trap the UI.
    timer = setTimeout(stop, duration);
    void stage.offsetWidth;
    stage.classList.add("is-playing");
    return true;
  }

  window.TierTenCinematics = { play, stop, spellIds: Object.keys(scenes) };
})();
