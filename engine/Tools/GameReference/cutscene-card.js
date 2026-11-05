// The boss intro card at chosen times (Docs/Scene.md, step 7, phase 2; Q32): run in the live old game by
// Tools/GameReference (Tools/Scripts/make-cutscene-references.sh). Ji'ruan's intro is rendered by the
// game's own BossIntros.render with no sprites, its SVG scene emptied (the engine shows the 3D stage
// there, so the card is compared over a flat backdrop: a fixed element behind the overlay), shown and
// set playing as combat.js does; then for each time every CSS animation in the overlay is paused and
// seeked to it (its currentTime counts from the animation's start, its delay included) and the window
// captured at 1200 × 680 as cutscene-card-<time>.png.
async function cutsceneCardCaptures() {
  const times = [0, 1.4, 2.5, 3.4, 4.6, 7];
  const host = document.getElementById("boss-intro-cinematic");
  const backdrop = document.createElement("div");
  backdrop.id = "cutscene-backdrop";
  backdrop.style.cssText = "position:fixed;inset:0;z-index:1399;background:#3a2c4a";
  document.body.append(backdrop);
  // The page under the overlay mustn't show its scrollbar beside it (the engine's document has none).
  document.documentElement.style.overflow = "hidden";
  document.body.style.overflow = "hidden";
  BossIntros.render(host, "jiruan", {});
  host.querySelector(".boss-intro-scene").innerHTML = "";
  host.hidden = false;
  void host.offsetWidth;
  host.classList.add("is-playing");
  await new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve)));
  const animations = host.getAnimations({ subtree: true });
  if (animations.length < 6) throw new Error("the card's animations didn't start: " + animations.length);
  for (const time of times) {
    for (const animation of animations) {
      animation.pause();
      animation.currentTime = time * 1000;
    }
    await statFickDesktop.captureTestScreen("cutscene-card-" + time.toFixed(2), 1200, 680);
  }
}
