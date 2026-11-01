"use strict";
// The full themes' references (Docs/Audio.md, step 6: opt-in, kept in the build cache): every theme
// rendered by the old game's own music.js renderOffline in Chromium 152, at 44.1 kHz, `runs` times
// (Chromium's summing order differs each time), with a suspend every render quantum so the notes'
// onended cleanups land where the engine's do. Writes <out>/<id>.min.f32.gz and <id>.max.f32.gz
// (planar float32, the per-sample minimum and maximum over the runs) and <out>/themes.json (each
// theme's frames and largest spread). Run by Tools/Scripts/make-theme-references.sh.
const fs = require("fs");
const path = require("path");
const zlib = require("zlib");

async function run(window, argument) {
  const scripts = argument("--old-game"), out = argument("--themes-out"), runs = Number(argument("--runs") || 4);
  const only = argument("--only")?.split(",");
  fs.mkdirSync(out, { recursive: true });
  for (const file of ["music-composer.js", "music-themes.js", "music.js"])
    await window.webContents.executeJavaScript(fs.readFileSync(path.join(scripts, file), "utf8") + "\n;0");
  await window.webContents.executeJavaScript(`(() => {
    const Base = window.OfflineAudioContext;
    window.OfflineAudioContext = class extends Base {
      startRendering() {
        for (let frame = 128; frame < this.length; frame += 128) this.suspend(frame / this.sampleRate).then(() => this.resume());
        return super.startRendering();
      }
    };
  })(); 0`);
  const ids = await window.webContents.executeJavaScript(`Object.keys(window.Music.scoreInfo())`);
  const indexPath = path.join(out, "themes.json");
  const index = fs.existsSync(indexPath) ? JSON.parse(fs.readFileSync(indexPath, "utf8")) : {};
  for (const id of ids) {
    if (only && !only.includes(id)) continue;
    let min = null, max = null;
    for (let r = 0; r < runs; r++) {
      const channels = await window.webContents.executeJavaScript(`(async () => {
        const buffer = await window.Music.renderOffline(${JSON.stringify(id)}, 44100);
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
      const samples = new Float32Array(data.buffer, data.byteOffset, data.length / 4);
      if (!min) { min = Float32Array.from(samples); max = Float32Array.from(samples); }
      else for (let i = 0; i < samples.length; i++) { if (samples[i] < min[i]) min[i] = samples[i]; if (samples[i] > max[i]) max[i] = samples[i]; }
    }
    let spread = 0;
    for (let i = 0; i < min.length; i++) spread = Math.max(spread, max[i] - min[i]);
    fs.writeFileSync(path.join(out, id + ".min.f32.gz"), zlib.gzipSync(Buffer.from(min.buffer), { level: 1 }));
    fs.writeFileSync(path.join(out, id + ".max.f32.gz"), zlib.gzipSync(Buffer.from(max.buffer), { level: 1 }));
    index[id] = { frames: min.length / 2, runs, spread };
    fs.writeFileSync(indexPath, JSON.stringify(index, null, 1) + "\n");
    console.log("rendered", id, runs, "times, spread", spread);
  }
}

module.exports = { run };
