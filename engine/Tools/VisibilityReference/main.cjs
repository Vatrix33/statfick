"use strict";
// Visibility references: what Chromium 152 (the old game's Electron, see
// Tools/Scripts/make-visibility-references.sh) does when its window is hidden, shown and closed,
// and how it runs a hidden page's timers. The page logs through console messages, so the lines
// written while it unloads arrive too; each line starts with the page's time (performance.now(),
// rounded to a millisecond).
//
// Scenarios (each in a window of its own, shown on the screen with background throttling on, as
// the old game's window; hiding stands in for minimizing, which needs a window manager: with one,
// minimize gives the same lines):
//   events   the events of hiding, showing and closing (visibility, page transitions, focus)
//   timers   timers of each kind (an interval, chained 0 ms and 250 ms timeouts, one-shots made
//            while hidden, a new chain after a quiet minute) through 260 s hidden: Blink's wake-up
//            throttling and its intensive throttling after the grace period
//   cpu      an interval busy for 150 ms each time (it logs how long), hidden for 40 s: the
//            background CPU-time budget
//   audible  an oscillator playing until the page is hidden (as the old game's music stops),
//            hidden for 50 s: an audible page isn't throttled until 30 s after it went silent
//
// Usage: electron --no-sandbox . --out=/abs/visibility-results.json [--only=name,...]
const { app, BrowserWindow } = require("electron");
const fs = require("fs");
const os = require("os");
const path = require("path");

const argument = name => {
  const prefix = name + "=";
  const found = process.argv.find(a => a.startsWith(prefix));
  return found ? found.slice(prefix.length) : null;
};

const wait = ms => new Promise(resolve => setTimeout(resolve, ms));
// Closing a scenario's window mustn't end the app (Electron quits when the last window closes).
app.on("window-all-closed", () => {});

// Every page: some text (so it has a first contentful paint and isn't "loading" to Blink's
// scheduler) and a logger.
const HEAD = `<!DOCTYPE html><html><head><meta charset="utf-8"></head><body>
<p style="font: 20px sans-serif">Visibility reference</p>`;
const LOGGER = `
const __t0 = performance.now();
window.log = text => console.log("@" + Math.round(performance.now()) + " " + text);
document.addEventListener("visibilitychange", () => log("visibility " + document.visibilityState));
`;

const SCENARIOS = {
  events: {
    body: `<input id="field" autofocus>`,
    script: `
const name = n => n === document ? "document" : n === window ? "window" : n && n.id ? "#" + n.id : String(n && n.localName);
for (const type of ["visibilitychange", "webkitvisibilitychange", "pagehide", "pageshow", "beforeunload", "unload",
                    "freeze", "resume", "blur", "focus", "focusin", "focusout"]) {
  window.addEventListener(type, e => {
    let line = "event " + type + " target=" + name(e.target) + " phase=" + e.eventPhase + " bubbles=" + e.bubbles +
      " cancelable=" + e.cancelable + " trusted=" + e.isTrusted + " class=" + e.constructor.name;
    if ("persisted" in e) line += " persisted=" + e.persisted;
    line += " hidden=" + document.hidden + " state=" + document.visibilityState + " hasFocus=" + document.hasFocus();
    log(line);
  }, true);
}`,
    async run(window, log) {
      await wait(500);
      log("hide");
      window.hide();
      await wait(1500);
      log("show");
      window.show();
      window.focus();
      window.webContents.focus();
      await wait(1500);
      log("close");
      window.close();
      await wait(1500);
    },
  },
  timers: {
    hidden: 260000,
    script: `
let running = true;
const interval = setInterval(() => log("interval100"), 100);
(function chain0() { setTimeout(() => { if (!running) return; log("chain0"); chain0(); }, 0); })();
(function chain250() { setTimeout(() => { if (!running) return; log("chain250"); chain250(); }, 250); })();
window.oneShot = delay => { log("install oneshot" + delay); setTimeout(() => log("oneshot" + delay), delay); };
window.stopAll = () => { log("stop"); running = false; clearInterval(interval); };
window.newChain = () => { log("new chain"); let depth = 0; (function link() { setTimeout(() => { depth++; log("link" + depth); if (depth < 12) link(); }, 0); })(); };`,
    // One-shots made while hidden (nesting level 1): 10 ms ones (throttled to a second) and a 0 ms
    // one (an immediate timer, never throttled). Then every timer stops, and a minute later a new
    // 0 ms chain starts: its first six links are immediate, the deeper ones intensively throttled,
    // the first of them aligned to a second (the intensive pool hasn't woken for a minute).
    injections: [[30000, "oneShot(10)"], [45000, "oneShot(0)"], [90000, "oneShot(10)"], [125000, "stopAll()"], [190000, "newChain()"]],
  },
  cpu: {
    hidden: 40000,
    script: `
setInterval(() => { const start = performance.now(), end = start + 150; while (performance.now() < end); log("busy " + (performance.now() - start).toFixed(3)); }, 200);`,
  },
  audible: {
    hidden: 50000,
    script: `
const context = new AudioContext();
const oscillator = context.createOscillator();
const gain = context.createGain();
gain.gain.value = 0.2;
oscillator.connect(gain).connect(context.destination);
oscillator.start();
context.resume().then(() => log("audio " + context.state));
document.addEventListener("visibilitychange", () => { if (document.hidden) { oscillator.stop(); log("audio stopped"); } });
(function chain250() { setTimeout(() => { log("chain250"); chain250(); }, 250); })();`,
  },
};

app.whenReady().then(async () => {
  const results = {};
  const scratch = fs.mkdtempSync(path.join(os.tmpdir(), "statventure-visibility-"));
  const only = argument("--only")?.split(",");
  for (const [name, scenario] of Object.entries(SCENARIOS)) {
    if (only && !only.includes(name))
      continue;
    const lines = [];
    const log = text => lines.push("main " + text);
    const window = new BrowserWindow({
      width: 400, height: 300, useContentSize: true, show: false,
      webPreferences: { sandbox: true, contextIsolation: true, spellcheck: false },
    });
    window.webContents.on("console-message", (event, ...legacy) => {
      const message = String(event.message ?? legacy[1]);
      if (message.startsWith("@"))
        lines.push(message.slice(1));
    });
    const file = path.join(scratch, name + ".html");
    fs.writeFileSync(file, `${HEAD}${scenario.body || ""}<script>${LOGGER}\n${scenario.script}\n</script></body></html>`);
    await window.loadFile(file);
    window.show();
    window.focus();
    window.webContents.focus();
    await wait(3000);
    if (scenario.run)
      await scenario.run(window, log);
    else {
      log("hide");
      window.hide();
      for (const [at, script] of scenario.injections || [])
        setTimeout(() => window.webContents.executeJavaScript(script + "; 0", true).catch(() => {}), at);
      await wait(scenario.hidden);
      log("show");
      window.show();
      await wait(2000);
      window.destroy();
    }
    results[name] = { log: lines };
    console.log(name, lines.length, "lines");
  }
  fs.writeFileSync(argument("--out"), JSON.stringify(results, null, 1) + "\n");
  fs.rmSync(scratch, { recursive: true, force: true });
  app.quit();
}).catch(error => {
  console.error(error);
  app.exit(1);
});
