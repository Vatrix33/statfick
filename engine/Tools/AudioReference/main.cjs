"use strict";
// Audio references (Docs/Audio.md, "Method"): renders small Web Audio graphs in Chromium 152's
// OfflineAudioContext (the old game's browser, run by Tools/Scripts/make-audio-references.sh) and
// writes each render as planar little-endian float32 (channel 0's frames, then channel 1's …),
// gzipped, to <out>/<name>.f32.gz. The engine's tests build the same graphs on the engine's audio
// nodes and compare.
//
// A fixture: { name, channels, length (frames), rate, script, quanta }; the script builds its graph on
// `ctx`, the OfflineAudioContext, and may await (its body runs in an async function). With
// --old-game the page has the old game's MusicComposer; --repeat=n renders each fixture n times
// (<name>.<k>.f32.gz), to see how Chromium's own renders differ.
//
// Usage: electron . --fixtures=/abs/audio-fixtures.json --out=/abs/folder [--only=name,name]
//        electron . --old-game=/abs/old-game/scripts --music-data=/abs/folder (music.cjs: the records' JSON forms)
//        electron . --old-game=/abs/old-game/scripts --music-references=/abs/Reference (music.cjs)
//        electron . --old-game=/abs/old-game/scripts --themes-out=/abs/cache/folder [--runs=4] [--only=id,id] (themes.cjs)
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
  const fixtures = argument("--fixtures") ? JSON.parse(fs.readFileSync(argument("--fixtures"), "utf8")) : [];
  const out = argument("--out");
  const only = argument("--only")?.split(",");
  if (out) fs.mkdirSync(out, { recursive: true });
  const scratch = fs.mkdtempSync(path.join(os.tmpdir(), "statventure-audio-"));
  const page = path.join(scratch, "audio.html");
  fs.writeFileSync(page, "<!DOCTYPE html><html><head><meta charset=utf-8></head><body></body></html>");
  const window = new BrowserWindow({ show: false, webPreferences: { sandbox: true, contextIsolation: true, backgroundThrottling: false } });
  await window.loadFile(page);
  if (argument("--themes-out")) {
    await require("./themes.cjs").run(window, argument);
    fs.rmSync(scratch, { recursive: true, force: true });
    app.exit(0);
    return;
  }
  if (argument("--music-data") || argument("--music-references")) {
    await require("./music.cjs").run(window, argument);
    fs.rmSync(scratch, { recursive: true, force: true });
    app.exit(0);
    return;
  }
  // The old game's composer, for the synthesizer's fixtures (they perform small themes on ctx).
  if (argument("--old-game"))
    await window.webContents.executeJavaScript(fs.readFileSync(path.join(argument("--old-game"), "music-composer.js"), "utf8") + "\n;0");
  const repeat = Number(argument("--repeat") || 1);
  let failed = false;
  for (const fixture of fixtures) for (let run = 0; run < repeat; run++) {
    if (only && !only.includes(fixture.name))
      continue;
    try {
      // The render comes back as base64 per channel (the bytes of its Float32Array).
      const channels = await window.webContents.executeJavaScript(`(async () => {
        const ctx = new OfflineAudioContext(${fixture.channels}, ${fixture.length}, ${fixture.rate});
        // Quanta fixtures: a suspend every render quantum, so main-thread work (the composer's
        // onended cleanups) lands at the next quantum as in the engine; at(time, fn) runs fn there, then
        // quantum(fn)'s functions run at every quantum.
        const callbacks = new Map(), everyQuantum = [];
        const quantum = fn => everyQuantum.push(fn);
        const at = (time, fn) => {
          const frame = Math.ceil(Math.floor(time * ctx.sampleRate) / 128) * 128;
          if (!callbacks.has(frame)) callbacks.set(frame, []);
          callbacks.get(frame).push(fn);
        };
        await (async () => {\n${fixture.script}\n})();
        if (${Boolean(fixture.quanta)})
          for (let frame = 128; frame < ctx.length; frame += 128)
            ctx.suspend(frame / ctx.sampleRate).then(() => { for (const fn of callbacks.get(frame) || []) fn(); for (const fn of everyQuantum) fn(); ctx.resume(); });
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
      const file = fixture.name + (repeat > 1 ? "." + run : "") + ".f32.gz";
      fs.writeFileSync(path.join(out, file), zlib.gzipSync(data, { level: 9 }));
      console.log("wrote", file);
    } catch (error) {
      failed = true;
      console.error("failed", fixture.name, error);
    }
  }
  fs.rmSync(scratch, { recursive: true, force: true });
  app.exit(failed ? 1 : 0);
});
