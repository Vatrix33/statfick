async function exportCinderBossMusic() {
  for (const id of ["ur-mat-false-dreamer", "ornn-god-of-the-forge"]) {
    const buffer = await Music.renderOffline(id, 44100);
    const frames = buffer.length, bytes = new Uint8Array(44 + frames * 4), view = new DataView(bytes.buffer);
    const write = (offset, value) => { for (let i = 0; i < value.length; i++) bytes[offset + i] = value.charCodeAt(i); };
    write(0, "RIFF"); view.setUint32(4, bytes.length - 8, true); write(8, "WAVE");
    write(12, "fmt "); view.setUint32(16, 16, true); view.setUint16(20, 1, true);
    view.setUint16(22, 2, true); view.setUint32(24, buffer.sampleRate, true);
    view.setUint32(28, buffer.sampleRate * 4, true); view.setUint16(32, 4, true);
    view.setUint16(34, 16, true); write(36, "data"); view.setUint32(40, frames * 4, true);
    const left = buffer.getChannelData(0), right = buffer.getChannelData(1);
    let peak = 0;
    for (let i = 0, offset = 44; i < frames; i++, offset += 4) {
      peak = Math.max(peak, Math.abs(left[i]), Math.abs(right[i]));
      view.setInt16(offset, Math.round(Math.max(-1, Math.min(1, left[i])) * 32767), true);
      view.setInt16(offset + 2, Math.round(Math.max(-1, Math.min(1, right[i])) * 32767), true);
    }
    if (!Number.isFinite(peak) || peak < .005) throw new Error("Silent or invalid theme: " + id);
    await statFickDesktop.writeTestAudio(id + ".wav", bytes);
  }
}
