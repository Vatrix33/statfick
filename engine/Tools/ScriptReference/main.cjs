"use strict";
// Script references: runs small script scenarios in Chromium 152 (the old game's test client's
// Electron, see Tools/Scripts/make-script-references.sh) and records the order in which their
// callbacks ran (timers, microtasks, promises, frame callbacks, events, observers, animations),
// each line tagged with the frame it ran in. The engine's tests run the same scenarios written in
// C# on the engine's event loop and must log the same.
//
// The frame number counts animation frames: a frame callback registered before the scenario
// starts increments it first thing in every frame's animation frame step (so the frame's
// earlier steps, scroll and animation events, still log the previous number).
//
// A scenario may ask the harness for things only the browser can do, by logging a console line:
// "__click x y" (a real click at window coordinates), "__resize w h" (the window's new size).
//
// Usage: stat-fick-tests --no-sandbox --fixtures=/abs/script-fixtures.json --out=/abs/script-results.json --fonts=/abs/fonts/dir
const { app, BrowserWindow } = require("electron");
const fs = require("fs");
const os = require("os");
const path = require("path");

const argument = name => {
  const prefix = name + "=";
  const found = process.argv.find(a => a.startsWith(prefix));
  return found ? found.slice(prefix.length) : null;
};

app.commandLine.appendSwitch("disable-gpu-compositing");
app.commandLine.appendSwitch("disable-smooth-scrolling");

const wait = ms => new Promise(resolve => setTimeout(resolve, ms));

// Set up in the same task as the scenario, so frame 1 is the first frame after it.
const INSTRUMENT = `
  window.__log = [];
  window.__frame = 0;
  window.__frameTime = 0;
  window.log = text => window.__log.push("f" + window.__frame + " " + text);
  const __tick = time => { window.__frame++; window.__frameTime = time; requestAnimationFrame(__tick); };
  requestAnimationFrame(__tick);
`;

app.whenReady().then(async () => {
  const fixtures = JSON.parse(fs.readFileSync(argument("--fixtures"), "utf8"));
  const fonts = argument("--fonts");
  const results = {};
  const scratch = fs.mkdtempSync(path.join(os.tmpdir(), "statventure-script-"));
  const window = new BrowserWindow({
    width: 800, height: 600, useContentSize: true, show: false, frame: false,
    x: -20000, y: -20000, opacity: 0, skipTaskbar: true,
    webPreferences: { offscreen: true, sandbox: true, contextIsolation: true, backgroundThrottling: false, spellcheck: false },
  });
  const contents = window.webContents;
  contents.setFrameRate(60);
  contents.on("console-message", (event, ...legacy) => {
    // (Electron passes the message on the event; older versions as the second argument.)
    const message = event.message ?? legacy[1];
    const parts = String(message).split(" ");
    if (parts[0] === "__click") {
      const x = Number(parts[1]), y = Number(parts[2]);
      contents.sendInputEvent({ type: "mouseMove", x, y });
      contents.sendInputEvent({ type: "mouseDown", x, y, button: "left", clickCount: 1 });
      contents.sendInputEvent({ type: "mouseUp", x, y, button: "left", clickCount: 1 });
    } else if (parts[0] === "__resize")
      window.setContentSize(Number(parts[1]), Number(parts[2]));
  });
  for (const fixture of fixtures) {
    window.setContentSize(fixture.width, fixture.height);
    const face = weight => `@font-face { font-family: Lato; src: url("file://${fonts}/Lato-${weight === 400 ? "Regular" : "Bold"}.ttf"); font-weight: ${weight}; }`;
    const page = `<!DOCTYPE html><html><head><meta charset="utf-8"><style>${face(400)}\n${face(700)}\n${fixture.css}</style></head><body>${fixture.html}</body></html>`;
    const file = path.join(scratch, fixture.name + ".html");
    fs.writeFileSync(file, page);
    await window.loadFile(file);
    await contents.executeJavaScript("document.fonts.ready", true);
    window.showInactive();
    window.focus();
    contents.focus();
    await wait(100);
    // The scenario runs as a task of its own, right after a frame (so its 0 ms timers have the
    // time until the next frame).
    await contents.executeJavaScript(`window.__frame = -1; requestAnimationFrame(() => setTimeout(() => {\n${INSTRUMENT}\n{\n${fixture.script}\n}\n}, 0)); 0`, true);
    for (let i = 0; i < 600; i++) {
      if (await contents.executeJavaScript(`window.__frame >= ${fixture.frames}`, true))
        break;
      await wait(10);
    }
    // The frames before the last are complete (their lines can't change any more).
    const log = await contents.executeJavaScript("window.__log", true);
    results[fixture.name] = { log: log.filter(line => Number(line.slice(1, line.indexOf(" "))) < fixture.frames) };
    console.log(fixture.name, results[fixture.name].log.length, "lines");
  }
  window.destroy();
  fs.writeFileSync(argument("--out"), JSON.stringify(results, null, 1) + "\n");
  fs.rmSync(scratch, { recursive: true, force: true });
  app.quit();
}).catch(error => {
  console.error(error);
  app.exit(1);
});
