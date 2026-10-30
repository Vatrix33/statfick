"use strict";
// The test client's desktop API, as far as the capture script uses it.
const { contextBridge, ipcRenderer } = require("electron");
contextBridge.exposeInMainWorld("statFickDesktop", {
  saveCharacter: () => Promise.resolve(true),
  loadCharacter: () => Promise.resolve(null),
  captureTestScreen: (name, width, height) => ipcRenderer.invoke("statfick:capture-test-screen", name, width, height),
});
