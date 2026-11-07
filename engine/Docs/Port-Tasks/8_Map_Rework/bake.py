# Bakes layout draft 6's land field (W26) for the old game (`python3 bake.py`): runs draft6.py, adds Cinder Isle to the field, quantizes it
# to 2-unit steps in -254..254 and run-length codes it into old-game/scripts/continent-shape.js.
import runpy, sys, os, base64, tempfile, numpy as np
HERE = os.path.dirname(os.path.abspath(__file__))
sys.argv = ["draft6.py", os.path.join(tempfile.gettempdir(), "continent-draft-6.png")]
g = runpy.run_path(os.path.join(HERE, "draft6.py"))
field = g["field"].astype(np.float32)
gx, gy, S, P, vnoise = g["gx"], g["gy"], g["S"], g["P"], g["vnoise"]
cx, cy = P("cinder-isle", 800, 500)
e = np.hypot((gx - cx) / (700 * S), (gy - cy) / (445 * S))
cinder = (1 + (vnoise(500, 61) - 0.5) * 0.35 - e) * 445 * S
field = np.maximum(field, cinder)
land_pic = g["land"]
print("matches the picture's land:", float(((field > 0) == land_pic).mean()))
q = np.clip(np.round(field / 2), -127, 127).astype(np.int16)
flat = (q + 127).astype(np.uint8).ravel()
out = bytearray(); i = 0; n = len(flat)
while i < n:
    v = flat[i]; j = i
    while j < n and flat[j] == v and j - i < 255: j += 1
    run = j - i
    if run >= 4: out += bytes([255, v, run]); i = j
    else: out.append(v); i += 1
b64 = base64.b64encode(bytes(out)).decode()
rows, cols = q.shape
js = f'''/* The continent's land (port task 8): layout draft 6's land field as the user approved it (W26), made by
   engine/Docs/Port-Tasks/8_Map_Rework/draft6.py and baked by bake.py there: on a grid of STEP continent units from
   (X0, Y0), each cell the field in 2-unit steps (positive on land, about the distance to the shore; held to -254..254),
   run-length coded (a byte 0-254 is one cell, value byte - 127; 255 is followed by a value byte and a count). Read by
   continent-engine.js. Generated: do not edit. */
(root => {{
  "use strict";
  root.ContinentShape = Object.freeze({{ X0: {g["X0"]}, Y0: {g["Y0"]}, STEP: {g["STEP"]}, COLS: {cols}, ROWS: {rows}, UNIT: 2,
    DATA: "{b64}" }});
  if (typeof module !== "undefined" && module.exports) module.exports = root.ContinentShape;
}})(globalThis);
'''
open(os.path.join(HERE, "..", "..", "..", "..", "old-game", "scripts", "continent-shape.js"), "w").write(js)
print("cells", n, "coded bytes", len(out), "js bytes", len(js))
