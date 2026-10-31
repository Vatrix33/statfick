"use strict";
// Audio references (Docs/Audio.md, "Method"): renders small Web Audio graphs in Chromium 152's
// OfflineAudioContext (the old game's browser, run by Tools/Scripts/make-audio-references.sh) and
// writes each render as planar little-endian float32 (channel 0's frames, then channel 1's …),
// gzipped, to <out>/<name>.f32.gz. The engine's tests build the same graphs on the engine's audio
// nodes and compare.
//
// A fixture: { name, channels, length (frames), rate, script }; the script builds its graph on
// `ctx`, the OfflineAudioContext, and may await (its body runs in an async function).
//
// Usage: electron . --fixtures=/abs/audio-fixtures.json --out=/abs/folder [--only=name,name]
const { app, BrowserWindow } = require("electron");
const fs = require("fs");
const os = require("os");
const path = require("path");
const zlib = require("zlib");

const argument = name => {
  const prefix = name + "=";
  const found = process.argv.find(a => a.startsWith(prefix));
  return found ? found.slice(prefix.length) : null;
};

app.commandLine.appendSwitch("disable-gpu");
app.commandLine.appendSwitch("autoplay-policy", "no-user-gesture-required");

app.whenReady().then(async () => {
  const fixtures = JSON.parse(fs.readFileSync(argument("--fixtures"), "utf8"));
  const out = argument("--out");
  const only = argument("--only")?.split(",");
  fs.mkdirSync(out, { recursive: true });
  const scratch = fs.mkdtempSync(path.join(os.tmpdir(), "statventure-audio-"));
  const page = path.join(scratch, "audio.html");
  fs.writeFileSync(page, "<!DOCTYPE html><html><head><meta charset=utf-8></head><body></body></html>");
  const window = new BrowserWindow({ show: false, webPreferences: { sandbox: true, contextIsolation: true, backgroundThrottling: false } });
  await window.loadFile(page);
  let failed = false;
  for (const fixture of fixtures) {
    if (only && !only.includes(fixture.name))
      continue;
    try {
      // The render comes back as base64 per channel (the bytes of its Float32Array).
      const channels = await window.webContents.executeJavaScript(`(async () => {
        const ctx = new OfflineAudioContext(${fixture.channels}, ${fixture.length}, ${fixture.rate});
        await (async () => {\n${fixture.script}\n})();
        const buffer = await ctx.startRendering();
        const result = [];
        for (let c = 0; c < buffer.numberOfChannels; c++) {
          const bytes = new Uint8Array(buffer.getChannelData(c).buffer);
          let text = "";
          for (let i = 0; i < bytes.length; i += 0x8000) text += String.fromCharCode.apply(null, bytes.subarray(i, i + 0x8000));
          result.push(btoa(text));
        }
        return result;
      })()`, true);
      const data = Buffer.concat(channels.map(c => Buffer.from(c, "base64")));
      fs.writeFileSync(path.join(out, fixture.name + ".f32.gz"), zlib.gzipSync(data, { level: 9 }));
      console.log("wrote", fixture.name);
    } catch (error) {
      failed = true;
      console.error("failed", fixture.name, error);
    }
  }
  fs.rmSync(scratch, { recursive: true, force: true });
  app.exit(failed ? 1 : 0);
});
