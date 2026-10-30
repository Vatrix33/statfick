"use strict";
// The page's cc layers as Chromium composites it: records a trace with cc's debug categories while a
// few frames are drawn, and reads the last LayerTreeHostImpl snapshot of the renderer's tree (the one
// with the LayoutView's layers). Each layer that draws content: Blink's debug name, compositing
// reasons, screen quad, whether its contents are opaque and why it can't use LCD text (if it can't).
const fs = require("node:fs");
const os = require("node:os");
const path = require("node:path");
const { contentTracing } = require("electron");

const CATEGORIES = ["disabled-by-default-cc.debug", "disabled-by-default-viz.quads", "disabled-by-default-devtools.timeline.layers", "cc"];

async function recordLayers(webContents) {
  const file = path.join(fs.mkdtempSync(path.join(os.tmpdir(), "statventure-trace-")), "trace.json");
  await contentTracing.startRecording({ included_categories: CATEGORIES });
  // Frames must be drawn while tracing for snapshots: a custom property change repaints nothing visible.
  for (let i = 0; i < 6; i++) {
    await webContents.executeJavaScript("document.documentElement.style.setProperty('--statventure-trace', String(Math.random())); new Promise(r => requestAnimationFrame(() => requestAnimationFrame(r)))", true);
    await new Promise(resolve => setTimeout(resolve, 80));
  }
  await contentTracing.stopRecording(file);
  await webContents.executeJavaScript("document.documentElement.style.removeProperty('--statventure-trace')", true);
  const trace = JSON.parse(fs.readFileSync(file, "utf8"));
  fs.rmSync(path.dirname(file), { recursive: true, force: true });
  const events = Array.isArray(trace) ? trace : trace.traceEvents;
  let tree = null;
  for (const event of events) {
    const snapshot = event.name === "LayerTreeHostImpl:snapshot" && event.ph === "I" && event.args?.snapshot?.active_tree;
    if (snapshot && JSON.stringify(snapshot.layers).includes("LayoutView"))
      tree = snapshot;
  }
  if (!tree)
    throw new Error("no layer tree snapshot in the trace");
  const layers = [];
  const walk = list => {
    for (const layer of list) {
      if (layer.draws_content)
        layers.push({
          name: layer.layer_name || "",
          reasons: layer.compositing_reason_ids || [],
          quad: layer.layer_quad || null,
          opaque: !!layer.contents_opaque,
          lcd: layer.lcd_text_disallowed_reason ?? null,
        });
      walk(layer.children || []);
    }
  };
  walk(tree.layers);
  return layers;
}

module.exports = { recordLayers };
