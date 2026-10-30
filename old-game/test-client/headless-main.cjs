"use strict";

// Separate entry point: never uses player settings, opens a window or a dialog.
const { app, BrowserWindow, Menu, protocol, session, ipcMain } = require("electron");
const fs = require("node:fs");
const os = require("node:os");
const path = require("node:path");
const { ORIGIN, localUrl, createAssetHandler } = require("./assets.cjs");

let window, timer, finished = false, profile, reportFile;
const report = { passed: false, headless: true, visibleWindowCount: 0, checks: [], suites: [] };

async function finish(error) {
  if (finished) return;
  finished = true;
  clearTimeout(timer);
  report.passed = !error;
  if (error) report.error = error.stack || String(error);
  try {
    if (profile && app.isReady()) session.defaultSession.flushStorageData();
    if (reportFile) {
      fs.mkdirSync(path.dirname(reportFile), { recursive: true });
      fs.writeFileSync(reportFile, JSON.stringify(report, null, 2));
    }
    console.log(JSON.stringify(report));
    if (reportFile) console.log("Report:", reportFile);
  } catch (writeError) {
    console.error("Could not write test report:", writeError.stack);
    error = error || writeError;
  }
  if (error) console.error(error.stack || error);
  app.exit(error ? 1 : 0);
}
process.on("uncaughtException", finish);
process.on("unhandledRejection", finish);
app.on("window-all-closed", () => { if (!finished) finish(new Error("Test renderer closed before completion")); });

const argument = name => process.argv.find(value => value.startsWith(name + "="))?.slice(name.length + 1);
if (process.argv.includes("--help")) {
  console.log(`Stat Fick headless test runner (no game window or player saves)
  --suite=smoke|combat|enemies|lifecycle|all   Default: smoke
  --compare                       Compare cached and uncached combat AI
  --script=/absolute/checks.js     Run a custom browser test instead
  --call=runChecks                 Optional function to call after the script
  --profile                       CPU profile of the custom --call test
  --game-folder=/absolute/Stats    Game folder to test (default: the folder above tests/)
  --report=/absolute/report.json   Optional summary report location
  --timeout=300                   Timeout in seconds (default: 300)

Every run uses a fresh temporary profile. Reports, screenshots and test saves
are kept in that temporary directory. Exit code 0 means success, 1 means failure.`);
  app.exit(0);
} else {
  try {
    const suite = argument("--suite") || "smoke";
    const script = argument("--script");
    const call = argument("--call");
    const gameFolder = argument("--game-folder");
    const timeout = Number(argument("--timeout") || 300);
    const output = argument("--report");
    if (!["smoke", "combat", "enemies", "lifecycle", "all"].includes(suite)) throw new Error("Unknown test suite: " + suite);
    for (const [name, value] of [["--script", script], ["--game-folder", gameFolder], ["--report", output]]) {
      if (value !== undefined && (!value || !path.isAbsolute(value))) throw new Error(name + " requires an absolute path");
    }
    if (output && !output.endsWith(".json")) throw new Error("--report must name a .json file");
    if (call && (!script || !/^[A-Za-z_$][\w$]*$/.test(call))) throw new Error("--call requires a script and a simple function name");
    if (!Number.isFinite(timeout) || timeout < 1 || timeout > 3600) throw new Error("--timeout must be between 1 and 3600 seconds");

    app.setName("Stat Fick Tests");
    app.setAppUserModelId("com.statfick.tests");
    app.disableHardwareAcceleration();
    profile = fs.mkdtempSync(path.join(os.tmpdir(), "stat-fick-headless-"));
    app.setPath("userData", profile);
    app.setPath("sessionData", profile);
    app.setPath("crashDumps", profile);
    reportFile = output || path.join(profile, "report.json");
    Object.assign(report, { profile, platform: process.platform, electron: process.versions.electron, packaged: app.isPackaged });
    protocol.registerSchemesAsPrivileged([{ scheme: "statfick", privileges: { standard: true, secure: true, supportFetchAPI: true, corsEnabled: true, stream: true } }]);
    timer = setTimeout(() => finish(new Error("Headless tests timed out after " + timeout + " seconds")), timeout * 1000);

    app.whenReady().then(async () => {
      Menu.setApplicationMenu(null);
      const executableFolder = process.env.PORTABLE_EXECUTABLE_DIR || (process.env.APPIMAGE ? path.dirname(process.env.APPIMAGE) : path.dirname(process.execPath));
      // The test executable lives in Stats/tests and tests the live files above
      // it. It carries only its own checks, never a copy of the game.
      const gameRoot = gameFolder || (app.isPackaged ? path.dirname(executableFolder) : path.resolve(__dirname, ".."));
      const missing = ["index.html", "styles.css", "scripts/app.js", "objects/artifacts.json", "classes", "enemies"].filter(file => !fs.existsSync(path.join(gameRoot, file)));
      if (missing.length) throw new Error("No Stat Fick game folder at " + gameRoot + " (missing " + missing.join(", ") + "). Keep this client in Stats/tests or pass --game-folder.");
      Object.assign(report, { gameRoot });
      const serve = createAssetHandler(() => [gameRoot]);
      protocol.handle("statfick", async request => {
        const response = await serve(request);
        if ((!process.argv.includes("--compare") && suite !== "lifecycle" && suite !== "all") || !request.url.includes("/scripts/combat.js") || response.status !== 200) return response;
        // Uncached comparison switch exists only in this test renderer's memory.
        const original = await response.text();
        const source = original.replace("if (!calculationFrame ||", "if (window.__disableCombatCache || !calculationFrame ||");
        if (source === original) throw new Error("Combat cache comparison hook was not found");
        const headers = new Headers(response.headers); headers.delete("Content-Length");
        return new Response(source, { status: response.status, headers });
      });
      session.defaultSession.setPermissionRequestHandler((_contents, _permission, callback) => callback(false));
      session.defaultSession.setPermissionCheckHandler(() => false);
      session.defaultSession.webRequest.onBeforeRequest((details, callback) => callback({ cancel: !localUrl(details.url) && !/^(data:|blob:)/.test(details.url) }));
      session.defaultSession.on("will-download", (_event, item) => item.cancel());
      const trusted = event => event.sender === window?.webContents && event.senderFrame === window.webContents.mainFrame && localUrl(event.senderFrame.url);
      const saveFile = path.join(profile, "desktop-test-character.json");
      ipcMain.handle("statfick:save-character", async (event, json) => {
        if (!trusted(event) || typeof json !== "string" || Buffer.byteLength(json) > 10 * 1024 * 1024) throw new Error("Invalid test save request");
        const parsed = JSON.parse(json);
        if (!(parsed.character || parsed).stats) throw new Error("Invalid test character");
        fs.writeFileSync(saveFile, json);
        return true;
      });
      ipcMain.handle("statfick:load-character", async event => {
        if (!trusted(event)) throw new Error("Invalid test load request");
        if (!fs.existsSync(saveFile)) return null;
        if (fs.statSync(saveFile).size > 10 * 1024 * 1024) throw new Error("Test save is too large");
        return { name: path.basename(saveFile), text: fs.readFileSync(saveFile, "utf8") };
      });
      ipcMain.handle("statfick:write-test-audio", async (event, name, bytes) => {
        if (!trusted(event) || typeof name !== "string" || !/^[a-z0-9-]+\.wav$/.test(name) || !(bytes instanceof Uint8Array) || bytes.length < 44 || bytes.length > 32 * 1024 * 1024) throw new Error("Invalid test audio export");
        const wav = Buffer.from(bytes);
        if (wav.toString("ascii", 0, 4) !== "RIFF" || wav.toString("ascii", 8, 12) !== "WAVE") throw new Error("Expected WAV audio");
        const target = path.join(profile, name);
        fs.writeFileSync(target, wav);
        return target;
      });
      ipcMain.handle("statfick:capture-test-screen", async (event, name, width = 1440, height = 1000) => {
        if (!trusted(event) || !/^[a-z0-9-]{1,80}$/.test(name) || !Number.isInteger(width) || !Number.isInteger(height) || width < 360 || width > 2560 || height < 600 || height > 2400) throw new Error("Invalid test screenshot");
        window.setContentSize(width, height);
        await window.webContents.executeJavaScript("document.fonts.ready.then(() => new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve))))", true);
        window.webContents.invalidate();
        // Resizing an offscreen surface is asynchronous; capture its next frame.
        await new Promise(resolve => setTimeout(resolve, 180));
        const target = path.join(profile, name + ".png");
        let capture;
        for (let attempt = 0; attempt < 5; attempt++) {
          try { capture = await window.webContents.capturePage(undefined, { stayHidden: true }); break; }
          catch (error) { if (attempt === 4) throw error; await new Promise(resolve => setTimeout(resolve, 180)); }
        }
        fs.writeFileSync(target, capture.toPNG());
        return target;
      });
      window = new BrowserWindow({
        title: "Stat Fick Tests", width: 1440, height: 950,
        show: false, skipTaskbar: true, focusable: false, frame: false,
        webPreferences: { offscreen: true, preload: path.join(__dirname, "headless-preload.cjs"), nodeIntegration: false, contextIsolation: true, sandbox: true, webSecurity: true, backgroundThrottling: false }
      });
      window.on("show", () => { report.visibleWindowCount++; window.hide(); finish(new Error("Test window must never become visible")); });
      window.webContents.setFrameRate(30);
      window.webContents.setAudioMuted(true);
      window.webContents.setWindowOpenHandler(() => ({ action: "deny" }));
      window.webContents.on("will-navigate", (event, url) => { if (!localUrl(url)) event.preventDefault(); });
      window.webContents.on("will-attach-webview", event => event.preventDefault());
      window.webContents.on("render-process-gone", (_event, details) => finish(new Error("Test renderer exited: " + details.reason)));
      await window.loadURL(ORIGIN + "/index.html");
      const execute = code => window.webContents.executeJavaScript(code, true);
      await execute(`(async () => {
        window.alert = () => { throw new Error('Unexpected alert in headless test'); };
        window.confirm = () => { throw new Error('Unexpected confirmation in headless test'); };
        window.prompt = () => { throw new Error('Unexpected prompt in headless test'); };
        for (let i=0;i<600 && !window.Music;i++) await new Promise(resolve=>setTimeout(resolve,50));
        if(!window.Music) throw new Error('Game scripts did not load');
        await waitForSourceFiles(); WorldMap.setAmbientEnabled(false);
      })()`);
      if (script) {
        await execute(fs.readFileSync(script, "utf8"));
        if (call && process.argv.includes("--profile")) {
          window.webContents.debugger.attach("1.3");
          await window.webContents.debugger.sendCommand("Profiler.enable");
          await window.webContents.debugger.sendCommand("Profiler.start");
          try { await execute(call + "()"); }
          finally {
            const result = await window.webContents.debugger.sendCommand("Profiler.stop");
            fs.writeFileSync(path.join(profile, "cpu-profile.json"), JSON.stringify(result.profile));
            window.webContents.debugger.detach();
          }
        } else if (call) await execute(call + "()");
        report.suites.push({ name: "custom", script, call, passed: true });
        const capture = await window.webContents.capturePage();
        fs.writeFileSync(path.join(profile, "custom.png"), capture.toPNG());
      } else {
        if (suite === "smoke" || suite === "all") {
          const file = path.join(profile, "smoke.json");
          const result = await require("./smoke.cjs").run({ app, window, reportFile: file, gameRoot, headless: true, close: false });
          report.suites.push({ name: "smoke", passed: result.passed, file });
          if (!result.passed) throw new Error(result.error);
        }
        const performance = () => require(path.join(__dirname, "tests", "combat-performance.cjs"));
        for (const name of ["combat", "enemies"]) {
          if (suite !== name && suite !== "all") continue;
          await window.loadURL(ORIGIN + "/index.html");
          await execute(`window.alert = window.confirm = window.prompt = () => { throw new Error('Unexpected dialog in headless test'); }; void 0;`);
          const result = await performance().run({ window, gameRoot, reportDirectory: profile, compare: process.argv.includes("--compare"), allEnemies: name === "enemies" });
          report.suites.push({ name, passed: result.passed, file: path.join(profile, name === "enemies" ? "all-enemies.json" : "results.json"), cases: result.cases || result.results.length });
        }
        if (suite === "lifecycle" || suite === "all") {
          await window.loadURL(ORIGIN + "/index.html");
          await execute(`window.alert = window.confirm = window.prompt = () => { throw new Error('Unexpected dialog in headless test'); }; void 0;`);
          const result = await require(path.join(__dirname, "tests", "combat-lifecycle-performance.cjs")).run({ window, reportDirectory: profile });
          report.suites.push({ name: "lifecycle", passed: result.passed, file: path.join(profile, "combat-lifecycle.json"), matchingSnapshots: result.matchingSnapshots });
        }
      }
      if (window.isVisible() || !window.webContents.isOffscreen() || !window.webContents.isAudioMuted()) throw new Error("Test renderer lost its headless configuration");
      report.checks.push("Offscreen rendering, no visible windows, muted audio", "Fresh temporary profile; player saves and settings never opened");
      await finish();
    }).catch(finish);
  } catch (error) { finish(error); }
}
