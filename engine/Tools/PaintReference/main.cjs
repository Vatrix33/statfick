"use strict";
// Paint references at a scale: screenshots the paint fixtures in Chromium 152 (the old game's
// test client's Electron, as Tools/InputReference) on a screen of that device scale (default:
// a real forced device scale factor under X11, so Blink lays out in device pixels as it does on
// a high-density or fractionally scaled screen), optionally with a page zoom on top (--page-zoom,
// the factor of Electron's zoom level: 1.2 to its power). Each fixture is laid out in an iframe of
// its exact size at the window's corner (the window manager decides the window's own size); the
// window is invisible (opacity 0). Writes ui-<fixture>@<scale>[x<page zoom>].png and .json (every
// element's getBoundingClientRect, CSS px). A fixture's "trigger" is a class added to the body once the page has been
// styled (to start transitions); its "computed" lists properties whose getComputedStyle values are written for every
// element with an id, as ui-<fixture>@<scale>[x<page zoom>].styles.json ({ id: { property: value } }).
// With --measure=layout (the layout fixtures) it writes layout-<fixture>@<scale>[x<page zoom>].json
// instead: what the 1× layout script measures (the html and body and every element's box, client
// and scroll sizes; every text node's line rects), unrounded, with all the game's font faces.
//
// Usage: stat-fick-tests --no-sandbox --ozone-platform=x11 --fixtures=/abs/paint-fixtures.json --scale=1.65 [--page-zoom=1.2] [--measure=layout] --out=/abs/dir --fonts=/abs/fonts/dir
const { app, BrowserWindow } = require("electron");
const fs = require("fs");
const os = require("os");
const path = require("path");

const argument = name => {
  const prefix = name + "=";
  const found = process.argv.find(a => a.startsWith(prefix));
  return found ? found.slice(prefix.length) : null;
};

const scale = Number(argument("--scale") || "1");
const pageZoom = Number(argument("--page-zoom") || "1");
const layout = argument("--measure") === "layout";
app.commandLine.appendSwitch("force-device-scale-factor", String(scale));
app.commandLine.appendSwitch("disable-gpu-compositing");
app.commandLine.appendSwitch("disable-smooth-scrolling");
app.commandLine.appendSwitch("force-color-profile", "srgb");

const wait = ms => new Promise(resolve => setTimeout(resolve, ms));

app.whenReady().then(async () => {
  const fixtures = JSON.parse(fs.readFileSync(argument("--fixtures"), "utf8"));
  const fonts = argument("--fonts");
  const out = argument("--out");
  const scratch = fs.mkdtempSync(path.join(os.tmpdir(), "statventure-paint-"));
  const maxWidth = Math.max(...fixtures.map(f => f.width)), maxHeight = Math.max(...fixtures.map(f => f.height));
  // A window of whole device pixels: capturePage asks for its size in DIPs times the scale rounded
  // up, and scales the surface to that (1458 px to 1459 for 884 DIPs at 1.65, blurring everything).
  const whole = length => {
    let size = Math.ceil(length) + 20;
    while (Math.abs(size * scale - Math.round(size * scale)) > 1e-6)
      size++;
    return size;
  };
  // Off the screens, but within X11's 16-bit window positions at any scale used (-10000 DIPs is -16500 px at 1.65;
  // -20000 was -33000, out of range: under a headless cage's Xwayland such a window wasn't managed and never got the
  // focus, so focus rings were missing from the 1.65 captures; M11 step 8).
  const window = new BrowserWindow({
    width: whole(maxWidth * pageZoom), height: whole(maxHeight * pageZoom), useContentSize: true,
    show: false, frame: false, x: -10000, y: -10000, opacity: 0, skipTaskbar: true,
    webPreferences: { sandbox: true, contextIsolation: true, backgroundThrottling: false, spellcheck: false },
  });
  window.showInactive();
  for (const fixture of fixtures) {
    // The window is active unless the fixture says otherwise (focus rings show only in an active window).
    if (fixture.windowFocused === false)
      window.blur();
    else {
      window.focus();
      window.webContents.focus();
    }
    const face = (file, weight, family = "Lato", style = "normal") => `@font-face { font-family: ${family}; src: url("file://${fonts}/${file}"); font-weight: ${weight}; font-style: ${style}; }`;
    const faces = layout
      ? [face("Lato-Regular.ttf", 400), face("Lato-Semibold.ttf", "500 600"), face("Lato-Bold.ttf", "700 900"),
         face("STIXTwoText-Regular.otf", 400, '"STIX Two Text"'), face("STIXTwoText-Italic.otf", 400, '"STIX Two Text"', "italic")]
      // (The game's STIX faces too, as the game loads them: regular and italic only, so its bold
      // is synthetic; the system's STIX Two Text, which has a bold face, must not stand in.)
      : [face("Lato-Regular.ttf", 400), face("Lato-Bold.ttf", 700),
         face("STIXTwoText-Regular.otf", 400, '"STIX Two Text"'), face("STIXTwoText-Italic.otf", 400, '"STIX Two Text"', "italic")];
    const page = `<!DOCTYPE html><html><head><meta charset="utf-8"><style>${faces.join("\n")}\n${fixture.css}</style></head><body>${fixture.html}</body></html>`;
    // The fixture in a srcdoc iframe (same origin as the host page, so its document can be reached).
    const host = path.join(scratch, fixture.name + "-host.html");
    // The frame in whole device pixels (a fractional frame rect makes the compositor scale its content
    // a little): its CSS size is what the host's zoom turns into the rounded device size, a hair more.
    const zoomed = scale * pageZoom;
    const frameSize = length => Math.round(length * zoomed) / zoomed + 0.001;
    fs.writeFileSync(host, `<!DOCTYPE html><html><body style="margin:0;background:#fff"><iframe style="display:block;border:0;width:${frameSize(fixture.width)}px;height:${frameSize(fixture.height)}px"></iframe></body></html>`);
    await window.loadFile(host);
    // The page zoom first, in effect before the fixture loads (a zoom change scales scroll offsets already set).
    if (pageZoom !== 1) {
      window.webContents.setZoomFactor(pageZoom);
      for (let i = 0; i < 100 && Math.abs(await window.webContents.executeJavaScript("devicePixelRatio", true) - scale * pageZoom) > 1e-3; i++)
        await wait(20);
    }
    await window.webContents.executeJavaScript(`new Promise(resolve => { const frame = document.querySelector("iframe"); frame.onload = resolve; frame.srcdoc = ${JSON.stringify(page)}; })`, true);
    // Runs a function with the iframe's window and document in place of the page's.
    const inFrame = script => window.webContents.executeJavaScript(`(frame => ((window, document) => (${script})())(frame, frame.document))(document.querySelector("iframe").contentWindow)`, true);
    await inFrame(`async () => {
      await Promise.all([...document.fonts].map(f => f.load().catch(() => null)));
      await document.fonts.ready;
      for (const el of document.querySelectorAll("[data-scroll-left], [data-scroll-top]")) {
        if (el.dataset.scrollLeft) el.scrollLeft = Number(el.dataset.scrollLeft);
        if (el.dataset.scrollTop) el.scrollTop = Number(el.dataset.scrollTop);
      }
      for (const el of document.querySelectorAll("[data-indeterminate]")) el.indeterminate = true;
      for (const el of document.querySelectorAll("[data-focus]")) el.focus();
      for (const el of document.querySelectorAll("[data-select]")) {
        const [start, end] = el.dataset.select.split(" ").map(Number);
        el.setSelectionRange(start, end);
      }
      const selectFrom = document.querySelector("[data-select-from]"), selectTo = document.querySelector("[data-select-to]");
      if (selectFrom && selectTo) {
        const range = document.createRange();
        range.setStart(selectFrom.firstChild, Number(selectFrom.dataset.selectFrom));
        range.setEnd(selectTo.firstChild, Number(selectTo.dataset.selectTo));
        window.getSelection().removeAllRanges();
        window.getSelection().addRange(range);
      }
      window.scrollTo(${fixture.scrollX || 0}, ${fixture.scrollY || 0});
    }`);
    // A class added to the body once the page has been styled (to start transitions).
    if (fixture.trigger) {
      await window.webContents.executeJavaScript("new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve)))", true);
      await inFrame(`() => { getComputedStyle(document.body).color; document.body.classList.add(${JSON.stringify(fixture.trigger)}); }`);
    }
    await wait(400);
    await window.webContents.executeJavaScript("new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve)))", true);
    const suffix = `@${scale}${pageZoom !== 1 ? "x" + pageZoom : ""}`;
    if (layout) {
      const measured = await inFrame(`() => {
        const elements = [document.documentElement, document.body, ...document.body.querySelectorAll("*")].map(e => {
          const r = e.getBoundingClientRect();
          return [e.localName, r.x, r.y, r.width, r.height, e.clientWidth, e.clientHeight, e.scrollWidth, e.scrollHeight];
        });
        const texts = [];
        const walker = document.createTreeWalker(document.body, NodeFilter.SHOW_TEXT);
        for (let node = walker.nextNode(); node; node = walker.nextNode()) {
          const range = document.createRange();
          range.selectNodeContents(node);
          texts.push([...range.getClientRects()].map(r => [r.x, r.y, r.width, r.height]));
        }
        return { devicePixelRatio: window.devicePixelRatio, elements, texts };
      }`);
      fs.writeFileSync(path.join(out, `layout-${fixture.name}${suffix}.json`), JSON.stringify(measured));
      console.log(fixture.name, "measured", measured.elements.length, "elements at", measured.devicePixelRatio);
      continue;
    }
    const image = await window.webContents.capturePage();
    const pixelWidth = Math.round(fixture.width * scale * pageZoom), pixelHeight = Math.round(fixture.height * scale * pageZoom);
    const cropped = image.crop({ x: 0, y: 0, width: pixelWidth, height: pixelHeight });
    fs.writeFileSync(path.join(out, `ui-${fixture.name}${suffix}.png`), cropped.toPNG());
    const viewport = await inFrame("() => [window.innerWidth, window.innerHeight, window.devicePixelRatio]");
    // Every element's box (CSS px), in document order, for comparing layout numerically.
    const rects = await inFrame(`() => [...document.querySelectorAll("body, body *")].map(e => { const r = e.getBoundingClientRect(); return [e.localName, r.x, r.y, r.width, r.height]; })`);
    fs.writeFileSync(path.join(out, `ui-${fixture.name}${suffix}.json`), JSON.stringify(rects));
    // The computed values of the listed properties (getComputedStyle's) of every element with an id.
    if (fixture.computed) {
      const styles = await inFrame(`() => Object.fromEntries([...document.querySelectorAll("[id]")].map(e => {
        const style = getComputedStyle(e);
        return [e.id, Object.fromEntries(${JSON.stringify(fixture.computed)}.map(p => [p, style.getPropertyValue(p)]))];
      }))`);
      fs.writeFileSync(path.join(out, `ui-${fixture.name}${suffix}.styles.json`), JSON.stringify(styles, null, 0));
    }
    console.log(fixture.name, "capture", image.getSize().width + "x" + image.getSize().height, "->", pixelWidth + "x" + pixelHeight, "viewport", viewport.join(" "));
  }
  window.destroy();
  fs.rmSync(scratch, { recursive: true, force: true });
  app.quit();
}).catch(error => {
  console.error(error);
  app.exit(1);
});
