"use strict";
const { contextBridge, ipcRenderer } = require("electron");
// Audio export is test-only and writes solely inside the temporary profile.
contextBridge.exposeInMainWorld("statFickDesktop", {
  saveCharacter: json => ipcRenderer.invoke("statfick:save-character", json),
  loadCharacter: () => ipcRenderer.invoke("statfick:load-character"),
  writeTestAudio: (name, bytes) => ipcRenderer.invoke("statfick:write-test-audio", name, bytes),
  captureTestScreen: (name, width, height) => ipcRenderer.invoke("statfick:capture-test-screen", name, width, height)
});
