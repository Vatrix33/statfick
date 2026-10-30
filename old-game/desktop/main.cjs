"use strict";
const { app, BrowserWindow, Menu, protocol, session, ipcMain, dialog, shell } = require("electron");
const fs = require("node:fs/promises");
const path = require("node:path");
const { ORIGIN, localUrl, createAssetHandler } = require("./assets.cjs");

app.setName("Stat Fick");
app.setAppUserModelId("com.statfick.desktop");
protocol.registerSchemesAsPrivileged([{ scheme: "statfick", privileges: { standard: true, secure: true, supportFetchAPI: true, corsEnabled: true, stream: true } }]);
let mainWindow, closing = false;
// Like any installed game, the executable plays the live files in its own
// folder. It carries no copy of them; edits apply on the next reload.
const executableFolder = process.env.PORTABLE_EXECUTABLE_DIR || (process.env.APPIMAGE ? path.dirname(process.env.APPIMAGE) : path.dirname(process.execPath));
const gameRoot = app.isPackaged ? executableFolder : path.resolve(__dirname, "..");
const requiredGameFiles = ["index.html", "styles.css", "scripts/app.js", "objects/artifacts.json", "classes", "enemies"];

if (!app.requestSingleInstanceLock()) app.quit();
else {
  app.on("second-instance", () => { if (mainWindow) { if (mainWindow.isMinimized()) mainWindow.restore(); mainWindow.show(); mainWindow.focus(); } });
  app.whenReady().then(start).catch(error => {
    console.error(error);
    dialog.showErrorBox("Stat Fick could not start", error.message);
    app.quit();
  });
}
app.on("window-all-closed", () => app.quit());

async function missingGameFiles() {
  const missing = [];
  for (const file of requiredGameFiles) {
    try { await fs.access(path.join(gameRoot, file)); } catch { missing.push(file); }
  }
  return missing;
}
function trustedSender(event) {
  return event.sender === mainWindow?.webContents && event.senderFrame === mainWindow.webContents.mainFrame && localUrl(event.senderFrame.url);
}
async function start() {
  const missing = await missingGameFiles();
  if (missing.length) throw new Error("Keep Stat Fick in its game folder, beside index.html, styles.css, scripts, objects, classes and enemies.\n\nFolder: " + gameRoot + "\nMissing: " + missing.join(", "));
  const serve = createAssetHandler(() => [gameRoot]);
  protocol.handle("statfick", request => serve(request));
  session.defaultSession.setPermissionRequestHandler((webContents, permission, callback) => callback(permission === "fullscreen" && localUrl(webContents.getURL())));
  session.defaultSession.setPermissionCheckHandler((webContents, permission) => permission === "fullscreen" && localUrl(webContents?.getURL()));
  session.defaultSession.webRequest.onBeforeRequest((details, callback) => callback({ cancel: !localUrl(details.url) && !/^(data:|blob:|devtools:)/.test(details.url) }));
  ipcMain.handle("statfick:save-character", async (event, json) => {
    if (!trustedSender(event) || typeof json !== "string" || Buffer.byteLength(json) > 10 * 1024 * 1024) throw new Error("Invalid character save request");
    const parsed = JSON.parse(json);
    if (!(parsed.character || parsed).stats) throw new Error("Invalid character save");
    const selected = await dialog.showSaveDialog(mainWindow, { title: "Save Character", defaultPath: path.join(app.getPath("documents"), "stat-fick-character.json"), filters: [{ name: "Stat Fick character", extensions: ["json"] }] });
    if (selected.canceled) return false;
    await fs.writeFile(selected.filePath, json, "utf8");
    return true;
  });
  ipcMain.handle("statfick:load-character", async event => {
    if (!trustedSender(event)) throw new Error("Invalid character load request");
    const selected = await dialog.showOpenDialog(mainWindow, { title: "Load Character", properties: ["openFile"], filters: [{ name: "Stat Fick character", extensions: ["json"] }] });
    if (selected.canceled) return null;
    const file = selected.filePaths[0];
    if ((await fs.stat(file)).size > 10 * 1024 * 1024) throw new Error("Character file is too large");
    return { name: path.basename(file), text: await fs.readFile(file, "utf8") };
  });
  mainWindow = new BrowserWindow({
    title: "Stat Fick", width: 1440, height: 950, minWidth: 820, minHeight: 600,
    show: false, backgroundColor: "#f4efe7", icon: path.join(__dirname, "icons", "icon.png"),
    webPreferences: { preload: path.join(__dirname, "preload.cjs"), nodeIntegration: false, contextIsolation: true, sandbox: true, webSecurity: true }
  });
  mainWindow.webContents.setWindowOpenHandler(() => ({ action: "deny" }));
  mainWindow.webContents.on("will-navigate", (event, url) => { if (!localUrl(url)) event.preventDefault(); });
  mainWindow.webContents.on("will-attach-webview", event => event.preventDefault());
  mainWindow.on("page-title-updated", event => { event.preventDefault(); mainWindow.setTitle("Stat Fick"); });
  mainWindow.once("ready-to-show", () => mainWindow.show());
  mainWindow.on("close", event => {
    if (closing) return;
    event.preventDefault(); closing = true;
    Promise.race([mainWindow.webContents.executeJavaScript("window.WorldMap?.pause(); if (typeof saveCharacter === 'function') saveCharacter();"), new Promise(resolve => setTimeout(resolve, 2500))]).catch(() => {}).finally(() => {
      session.defaultSession.flushStorageData(); mainWindow.destroy();
    });
  });
  Menu.setApplicationMenu(Menu.buildFromTemplate([
    { label: "File", submenu: [
      { label: "Save Character", accelerator: "CmdOrCtrl+S", click: () => mainWindow.webContents.executeJavaScript("exportBackup()", true) },
      { label: "Load Character", accelerator: "CmdOrCtrl+O", click: () => mainWindow.webContents.executeJavaScript("document.getElementById('backup-restore-button').click()", true) },
      { type: "separator" }, { label: "Open game folder", click: () => shell.openPath(gameRoot) },
      { type: "separator" }, { role: "quit" }
    ] },
    { label: "View", submenu: [{ role: "reload" }, { role: "resetZoom" }, { role: "zoomIn" }, { role: "zoomOut" }, { role: "togglefullscreen" }] },
    { label: "Help", submenu: [{ label: "About Stat Fick", click: () => dialog.showMessageBox(mainWindow, { title: "Stat Fick", message: "Stat Fick", detail: "Game files are read live from " + gameRoot + ". Choose View → Reload after editing them. This app stores its own autosave." }) }] }
  ]));
  let startupTimer;
  try {
    await Promise.race([mainWindow.loadURL(ORIGIN + "/index.html"), new Promise((resolve, reject) => { startupTimer = setTimeout(() => reject(new Error("The game window could not finish loading. Please reopen Stat Fick.")), 45000); })]);
  } finally { clearTimeout(startupTimer); }
}
