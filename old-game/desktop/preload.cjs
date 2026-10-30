"use strict";
const { contextBridge, ipcRenderer } = require("electron");
contextBridge.exposeInMainWorld("statFickDesktop", {
  saveCharacter: json => ipcRenderer.invoke("statfick:save-character", json),
  loadCharacter: () => ipcRenderer.invoke("statfick:load-character")
});
