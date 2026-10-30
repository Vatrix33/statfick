/* Presentation-only responsive field-tools drawer. No character state is changed. */
(() => {
  "use strict";
  const toggle = document.getElementById("field-tools-toggle");
  const shade = document.getElementById("field-tools-shade");
  const menu = document.getElementById("field-tools-menu");
  const compact = window.matchMedia("(max-width: 840px)");
  const focusable = () => [...menu.querySelectorAll("button:not(:disabled), input:not(:disabled), a[href]")].filter(node => node.getClientRects().length);
  let open = false;
  function update() {
    const visible = !compact.matches || open;
    menu.inert = !visible;
    if (visible) menu.removeAttribute("aria-hidden");
    else menu.setAttribute("aria-hidden", "true");
    document.body.classList.toggle("field-tools-open", compact.matches && open);
    toggle.setAttribute("aria-expanded", String(compact.matches && open));
    shade.hidden = !(compact.matches && open);
  }
  function close(restoreFocus = true) {
    const wasOpen = open;
    open = false; update();
    if (wasOpen && restoreFocus) toggle.focus({ preventScroll: true });
  }
  toggle.addEventListener("click", () => {
    if (open) return close();
    open = true; update();
    focusable()[0]?.focus({ preventScroll: true });
  });
  shade.addEventListener("click", () => close());
  // Navigation opens its own dialog first; don't steal that dialog's focus.
  menu.addEventListener("click", event => {
    if (event.target.closest(".main-tools-actions button, [data-companion-open]")) close(false);
  });
  document.addEventListener("keydown", event => {
    if (!compact.matches || !open) return;
    if (event.key === "Escape") { event.preventDefault(); close(); return; }
    if (event.key !== "Tab") return;
    const controls = [...focusable(), toggle];
    const first = controls[0], last = controls.at(-1);
    if (event.shiftKey && document.activeElement === first) { event.preventDefault(); last.focus(); }
    else if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first?.focus(); }
  });
  compact.addEventListener("change", () => { close(false); });
  // Anchor skill descriptions to the viewport, including skills at the right
  // edge. Hidden descriptions must not add horizontal scroll to combat.
  function positionSkillDescription(wrap) {
    const tooltip = wrap?.querySelector(".combat-skill-tooltip");
    if (!tooltip) return;
    const rect = wrap.getBoundingClientRect();
    const width = Math.min(330, window.innerWidth - 32);
    tooltip.style.display = "block";
    const height = tooltip.offsetHeight;
    tooltip.style.removeProperty("display");
    const left = Math.max(16, Math.min(window.innerWidth - width - 16, rect.left + rect.width / 2 - width / 2));
    const top = rect.top >= height + 24 ? rect.top - height - 10 : Math.min(window.innerHeight - height - 16, rect.bottom + 10);
    tooltip.style.setProperty("--combat-tip-left", left + "px");
    tooltip.style.setProperty("--combat-tip-top", Math.max(16, top) + "px");
  }
  document.addEventListener("pointerover", event => {
    const wrap = event.target.closest(".combat-skill-wrap");
    if (wrap && !wrap.contains(event.relatedTarget)) positionSkillDescription(wrap);
  });
  document.addEventListener("focusin", event => positionSkillDescription(event.target.closest(".combat-skill-wrap")));
  window.addEventListener("resize", () => positionSkillDescription(document.querySelector(".combat-skill-wrap:is(:hover, :focus-within)")));
  update();
})();
