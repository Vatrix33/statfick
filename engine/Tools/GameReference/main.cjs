"use strict";
// Game references at a device scale: runs the live game (Stats/) in Chromium 152 (the old test
// client's Electron) as the test client does, served by its statfick:// protocol, but in a real
// window under X11 with a forced device scale factor (the client renders offscreen, which ignores
// the device scale), invisible (opacity 0). Runs a capture script (--script, --call) and writes
// its outcome as the client's report (--report: { passed, error, profile }); screenshots the
// script asks for (statFickDesktop.captureTestScreen) go to the profile directory.
//
// With --layers each captured screen's cc layers (layers.cjs) go to layers-<name>.json beside it.
//
// Usage: stat-fick-tests --no-sandbox --ozone-platform=x11 --scale=1.65 --game-folder=/abs/Stats --script=/abs/capture.js --call=uiCapture --report=/abs/report.json [--layers]
const { app, BrowserWindow, Menu, protocol, session, ipcMain } = require("electron");
const fs = require("node:fs");
const os = require("node:os");
const path = require("node:path");
const { ORIGIN, localUrl, createAssetHandler } = require("./assets.cjs");
const { recordLayers } = require("./layers.cjs");

const argument = name => process.argv.find(value => value.startsWith(name + "="))?.slice(name.length + 1);
const scale = Number(argument("--scale") || "1");
app.commandLine.appendSwitch("force-device-scale-factor", String(scale));
app.commandLine.appendSwitch("disable-gpu-compositing");
app.commandLine.appendSwitch("disable-smooth-scrolling");
app.commandLine.appendSwitch("force-color-profile", "srgb");
protocol.registerSchemesAsPrivileged([{ scheme: "statfick", privileges: { standard: true, secure: true, supportFetchAPI: true, corsEnabled: true, stream: true } }]);

const profile = fs.mkdtempSync(path.join(os.tmpdir(), "statventure-game-"));
app.setPath("userData", profile);
app.setPath("sessionData", profile);
const report = { passed: false, profile, scale };
let finished = false;
function finish(error) {
  if (finished) return;
  finished = true;
  report.passed = !error;
  if (error) report.error = error.stack || String(error);
  fs.writeFileSync(argument("--report"), JSON.stringify(report));
  app.exit(error ? 1 : 0);
}
process.on("uncaughtException", finish);
process.on("unhandledRejection", finish);
setTimeout(() => finish(new Error("timed out")), Number(argument("--timeout") || "600") * 1000);

app.whenReady().then(async () => {
  Menu.setApplicationMenu(null);
  const gameRoot = argument("--game-folder");
  const serve = createAssetHandler(() => [gameRoot]);
  protocol.handle("statfick", request => serve(request));
  session.defaultSession.setPermissionRequestHandler((_contents, _permission, callback) => callback(false));
  session.defaultSession.webRequest.onBeforeRequest((details, callback) => callback({ cancel: !localUrl(details.url) && !/^(data:|blob:)/.test(details.url) }));
  const window = new BrowserWindow({
    width: 1440, height: 950, useContentSize: true, show: false, frame: false, x: -20000, y: -20000, opacity: 0, skipTaskbar: true,
    webPreferences: { preload: path.join(__dirname, "preload.cjs"), contextIsolation: true, sandbox: true, backgroundThrottling: false, spellcheck: false },
  });
  window.showInactive();
  window.webContents.setAudioMuted(true);
  ipcMain.handle("statfick:capture-test-screen", async (_event, name, width = 1440, height = 1000) => {
    // (The window manager adds to what X11 windows ask for: only resize when the page's size differs.)
    const current = await window.webContents.executeJavaScript("[innerWidth, innerHeight]", true);
    if (current[0] !== width || current[1] !== height) {
      window.setContentSize(width, height);
      // The window manager may not give the window its size at once: wait until the page has it.
      for (let i = 0; i < 50; i++) {
        const size = await window.webContents.executeJavaScript("[innerWidth, innerHeight]", true);
        if (size[0] === width && size[1] === height) break;
        await new Promise(resolve => setTimeout(resolve, 100));
      }
    }
    await window.webContents.executeJavaScript("document.fonts.ready.then(() => new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve))))", true);
    await new Promise(resolve => setTimeout(resolve, 300));
    // The first capture can be a frame from before the page changed: take one, then the one kept.
    await window.webContents.capturePage();
    await new Promise(resolve => setTimeout(resolve, 300));
    const capture = await window.webContents.capturePage();
    const target = path.join(profile, name + ".png");
    fs.writeFileSync(target, capture.toPNG());
    if (process.argv.includes("--layers"))
      fs.writeFileSync(path.join(profile, "layers-" + name + ".json"), JSON.stringify(await recordLayers(window.webContents)));
    return target;
  });
  await window.loadURL(ORIGIN + "/index.html");
  const execute = code => window.webContents.executeJavaScript(code, true);
  await execute(`(async () => {
    for (let i = 0; i < 600 && !window.Music; i++) await new Promise(resolve => setTimeout(resolve, 50));
    if (!window.Music) throw new Error('Game scripts did not load');
    await waitForSourceFiles(); WorldMap.setAmbientEnabled(false);
  })()`);
  await execute(fs.readFileSync(argument("--script"), "utf8"));
  const call = argument("--call");
  if (call)
    await execute(`${call}()`);
  finish();
}).catch(finish);
