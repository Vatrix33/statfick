"use strict";
// Input references: plays scripted input into Chromium 152 (the old game's test client's Electron,
// see Tools/Scripts/make-input-references.sh) as real, trusted input events (sendInputEvent),
// and records the events the page saw and the state it ended in. The engine's input tests replay
// the same steps and must see the same.
//
// With --scale (a high-density or fractionally scaled screen, e.g. 1.65) the page renders in an X11
// window at that device scale instead of offscreen (offscreen rendering ignores the scale), the
// steps' positions stay window units (DIPs), and each result records the page's viewport.
//
// Usage: stat-fick-tests --no-sandbox [--ozone-platform=x11 --scale=1.65] --fixtures=/abs/input-fixtures.json --out=/abs/input-results.json [--shots=/abs/dir] --fonts=/abs/fonts/dir
const { app, BrowserWindow } = require("electron");
const fs = require("fs");
const os = require("os");
const path = require("path");

const argument = name => {
  const prefix = name + "=";
  const found = process.argv.find(a => a.startsWith(prefix));
  return found ? found.slice(prefix.length) : null;
};

const scale = Number(argument("--scale") || "1");
app.commandLine.appendSwitch("force-device-scale-factor", String(scale));
// A window out of sight gets a frame every 2 s (no presentation feedback): frames are unthrottled instead.
if (scale !== 1)
  app.commandLine.appendSwitch("disable-frame-rate-limit");
app.commandLine.appendSwitch("disable-gpu-compositing");
// Overlay scrollbars and smooth scrolling off, as the game's client.
app.commandLine.appendSwitch("disable-smooth-scrolling");

const wait = ms => new Promise(resolve => setTimeout(resolve, ms));

// What the page logs: every listed event, seen at the document in the capture phase.
const INSTRUMENT = `
(() => {
  const id = n => n && n.nodeType === 1 ? (n.id ? "#" + n.id : n.localName) : n === document ? "document" : n === window ? "window" : String(n);
  window.__log = [];
  const types = ${JSON.stringify([
    "pointerdown", "pointerup", "mousedown", "mouseup", "click", "dblclick", "auxclick", "contextmenu",
    "keydown", "keypress", "keyup", "beforeinput", "input", "change", "submit", "reset", "invalid", "toggle", "search",
    "focus", "blur", "focusin", "focusout", "select", "selectionchange", "copy", "cut", "paste", "scroll", "scrollend", "wheel",
    "gotpointercapture", "lostpointercapture",
  ])};
  // Logged only when a fixture lists them.
  const optIn = ${JSON.stringify([
    "animationstart", "animationiteration", "animationend", "animationcancel",
    "transitionrun", "transitionstart", "transitionend", "transitioncancel",
  ])};
  const only = document.querySelector("[data-log]");
  const logged = new Set(only ? only.dataset.log.split(" ") : types);
  for (const type of [...types, ...optIn]) {
    document.addEventListener(type, e => {
      if (!logged.has(type)) return;
      let line = type + " " + id(e.target);
      if (!e.isTrusted) line += " untrusted";
      if (e instanceof KeyboardEvent) line += " key=" + JSON.stringify(e.key);
      if (e instanceof MouseEvent && (type === "click" || type === "mousedown" || type === "mouseup" || type === "dblclick")) line += " detail=" + e.detail;
      if (typeof InputEvent !== "undefined" && e instanceof InputEvent) line += " inputType=" + e.inputType + (e.data != null ? " data=" + JSON.stringify(e.data) : "");
      if (type === "toggle") line += " " + e.oldState + "->" + e.newState;
      // Elapsed times to a tenth (cancel times depend on the frames).
      if (e instanceof AnimationEvent) line += " name=" + e.animationName + " elapsed=" + Math.round(e.elapsedTime * 10) / 10 + (e.pseudoElement ? " pseudo=" + e.pseudoElement : "");
      if (e instanceof TransitionEvent) line += " property=" + e.propertyName + " elapsed=" + Math.round(e.elapsedTime * 10) / 10 + (e.pseudoElement ? " pseudo=" + e.pseudoElement : "");
      if (type === "submit") line += " submitter=" + (e.submitter ? id(e.submitter) : "null");
      if ((type === "input" || type === "change") && "value" in e.target) line += " value=" + JSON.stringify(e.target.value);
      if ((type === "input" || type === "change" || type === "click") && (e.target.type === "checkbox" || e.target.type === "radio")) line += " checked=" + e.target.checked;
      if (e.defaultPrevented) line += " prevented";
      window.__log.push(line);
    }, true);
  }
  // Fixture scripts: listeners that cancel, values set from script.
  for (const el of document.querySelectorAll("[data-prevent]"))
    for (const type of el.dataset.prevent.split(" ")) el.addEventListener(type, e => e.preventDefault());
})();
`;

// The state the page ended in: per element with an id, what the fixture asks about.
const COLLECT = `
(() => {
  const states = {};
  const pseudo = [":checked", ":indeterminate", ":hover", ":active", ":focus", ":focus-visible", ":valid", ":invalid", ":user-valid", ":user-invalid", ":placeholder-shown", ":open"];
  for (const el of document.querySelectorAll("[id]")) {
    const s = {};
    if (["input", "textarea", "select"].includes(el.localName)) s.value = el.value;
    if (el.type === "checkbox" || el.type === "radio") s.checked = el.checked;
    if (el.localName === "details") s.open = el.open;
    if (el.localName === "select") s.selectedIndex = el.selectedIndex;
    if (el.hasAttribute("data-size")) s.size = [el.style.width, el.style.height];
    // A text field's scroll is its inner editor's (a textarea's its own).
    if (el.localName === "textarea" || (el.localName === "input" && ["text", "search", "url", "tel", "password", "email", "number"].includes(el.type))) s.scroll = [el.scrollLeft, el.scrollTop];
    if (typeof el.selectionStart === "number" && el.selectionStart !== null && (el.localName === "textarea" || ["text", "search", "url", "tel", "password"].includes(el.type))) {
      s.selection = [el.selectionStart, el.selectionEnd, el.selectionDirection];
    }
    if (el.localName !== "html" && el.localName !== "body") {
      const matched = pseudo.filter(p => { try { return el.matches(p); } catch { return false; } });
      s.states = matched.join(" ");
    }
    states["#" + el.id] = s;
  }
  const active = document.activeElement;
  // The page's selection (getSelection), unless a text field holds it.
  const describe = (node, offset) => {
    if (!node) return null;
    if (node.nodeType === 3) {
      const parent = node.parentNode;
      return (parent.id ? "#" + parent.id : parent.localName) + "/" + [...parent.childNodes].indexOf(node) + ":" + offset;
    }
    return (node.id ? "#" + node.id : node.localName) + ":" + offset;
  };
  const selection = document.getSelection();
  const inField = active && (active.localName === "textarea" || (active.localName === "input" && ["text", "search", "url", "tel", "password", "email", "number"].includes(active.type)));
  const pageSelection = inField ? "field" : selection && selection.rangeCount
    ? { anchor: describe(selection.anchorNode, selection.anchorOffset), focus: describe(selection.focusNode, selection.focusOffset), text: selection.toString() }
    : null;
  return {
    pageSelection,
    log: window.__log,
    states,
    activeElement: active && active !== document.body ? (active.id ? "#" + active.id : active.localName) : null,
    hasFocus: document.hasFocus(),
    scroll: [window.scrollX, window.scrollY],
  };
})()
`;

// sendInputEvent key codes for DOM key values.
const ELECTRON_KEYS = {
  " ": "Space", "Enter": "Enter", "Tab": "Tab", "Escape": "Escape", "Backspace": "Backspace", "Delete": "Delete",
  "ArrowUp": "Up", "ArrowDown": "Down", "ArrowLeft": "Left", "ArrowRight": "Right", "Home": "Home", "End": "End",
  "PageUp": "PageUp", "PageDown": "PageDown", "Shift": "Shift", "Control": "Control", "Alt": "Alt",
};

function modifiersOf(step) {
  const result = [];
  for (const m of step.modifiers || []) result.push({ Shift: "shift", Control: "control", Alt: "alt", Meta: "meta" }[m]);
  return result;
}

// Clicks in a row as the platform counts them (GTK: 400 ms, 5 px), on the steps' clock: each step
// takes 40 ms, a wait its time (the engine's replay keeps the same clock).
const STEP_MS = 40;
const clicks = { time: -Infinity, x: 0, y: 0, button: null, count: 0, now: 0 };
// Buttons held: real mouse events carry them (a move with the left button down is a drag).
const held = new Set();
const heldModifiers = () => [...held].map(b => b + "ButtonDown");
let snapshots = [];
let currentFixture = "";
let fixtureStart = 0;
let currentWindow = null;

async function play(contents, step) {
  const modifiers = modifiersOf(step);
  switch (step.kind) {
    case "move":
      contents.sendInputEvent({ type: "mouseMove", x: step.x, y: step.y, modifiers: [...modifiers, ...heldModifiers()] });
      break;
    case "down": {
      const button = step.button || "left";
      held.add(button);
      modifiers.push(...heldModifiers());
      const near = Math.hypot(step.x - clicks.x, step.y - clicks.y) <= 5;
      clicks.count = button === clicks.button && clicks.now - clicks.time <= 400 && near ? clicks.count + 1 : 1;
      Object.assign(clicks, { time: clicks.now, x: step.x, y: step.y, button });
      contents.sendInputEvent({ type: "mouseDown", x: step.x, y: step.y, button, clickCount: clicks.count, modifiers });
      break;
    }
    case "up":
      modifiers.push(...heldModifiers());
      held.delete(step.button || "left");
      contents.sendInputEvent({ type: "mouseUp", x: step.x, y: step.y, button: step.button || "left", clickCount: clicks.count, modifiers });
      break;
    case "wheel":
      // A mouse wheel: whole notches (wheel ticks) as well as pixels.
      contents.sendInputEvent({
        type: "mouseWheel", x: step.x, y: step.y, deltaX: -(step.dx || 0), deltaY: -(step.dy || 0),
        wheelTicksX: -Math.sign(step.dx || 0), wheelTicksY: -Math.sign(step.dy || 0), canScroll: true, modifiers: [...modifiers, ...heldModifiers()],
      });
      break;
    case "keydown": {
      const keyCode = ELECTRON_KEYS[step.key] || step.key;
      contents.sendInputEvent({ type: "keyDown", keyCode, modifiers });
      // A key that types sends its character too (keypress, then the text).
      const types = step.key === "Enter" || (step.key.length === 1 && !modifiers.some(m => m === "control" || m === "alt" || m === "meta"));
      if (types)
        contents.sendInputEvent({ type: "char", keyCode: step.key === "Enter" ? "\r" : step.key, modifiers });
      break;
    }
    case "keyup": {
      const keyCode = ELECTRON_KEYS[step.key] || step.key;
      contents.sendInputEvent({ type: "keyUp", keyCode, modifiers });
      break;
    }
    case "wait":
      break;
    case "snapshot":
      snapshots.push(await contents.executeJavaScript(COLLECT, true));
      break;
    case "windowBlur":
      currentWindow.blur();
      contents.executeJavaScript("0", true);
      break;
    case "windowFocus":
      currentWindow.focus();
      contents.focus();
      break;
    case "screenshot": {
      if (!argument("--shots"))
        break;
      // The frame as painted now (the caret is on for 500 ms after a selection change).
      const image = await contents.capturePage();
      fs.writeFileSync(path.join(argument("--shots"), `input-${currentFixture}-${step.name}.png`), image.toPNG());
      break;
    }
    // What the game's scripts do: element methods and properties.
    case "focus":
    case "blur":
    case "click":
    case "reset":
    case "requestSubmit":
    case "select":
    case "stepUp":
    case "stepDown":
      await contents.executeJavaScript(`document.querySelector(${JSON.stringify(step.target)}).${step.kind}()`, true);
      break;
    case "set": {
      const flag = ["checked", "open", "indeterminate", "disabled", "required", "readOnly", "inert"].includes(step.property);
      const value = flag ? step.value === "true" : step.value;
      await contents.executeJavaScript(`document.querySelector(${JSON.stringify(step.target)})[${JSON.stringify(step.property)}] = ${JSON.stringify(value)}`, true);
      break;
    }
    default:
      throw new Error("unknown step " + step.kind);
  }
  const ms = step.kind === "wait" ? step.ms : STEP_MS;
  clicks.now += ms;
  // Let the frame run (events tied to animation frames, such as selectionchange, come before the
  // next step), then keep real time with the steps' clock (timers like the spin repeat follow it).
  await contents.executeJavaScript("new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve)))", true);
  await wait(Math.max(0, fixtureStart + clicks.now - Date.now()));
}

app.whenReady().then(async () => {
  const fixtures = JSON.parse(fs.readFileSync(argument("--fixtures"), "utf8"));
  const fonts = argument("--fonts");
  const results = {};
  const scratch = fs.mkdtempSync(path.join(os.tmpdir(), "statventure-input-"));
  // One window for all fixtures: shown (for focus) but out of sight and see-through; the page renders offscreen anyway.
  const window = new BrowserWindow({
    width: 800, height: 600, useContentSize: true, show: false, frame: false,
    x: -20000, y: -20000, opacity: 0, skipTaskbar: true,
    webPreferences: { offscreen: scale === 1, sandbox: true, contextIsolation: true, backgroundThrottling: false, spellcheck: false },
  });
  currentWindow = window;
  const viewportOf = () => window.webContents.executeJavaScript("[visualViewport.width, visualViewport.height]", true);
  for (const fixture of fixtures) {
    window.setContentSize(fixture.width, fixture.height);
    const face = weight => `@font-face { font-family: Lato; src: url("file://${fonts}/Lato-${weight === 400 ? "Regular" : "Bold"}.ttf"); font-weight: ${weight}; }`;
    const page = `<!DOCTYPE html><html><head><meta charset="utf-8"><style>${face(400)}\n${face(700)}\n${fixture.css}</style></head><body>${fixture.html}</body></html>`;
    const file = path.join(scratch, fixture.name + ".html");
    fs.writeFileSync(file, page);
    await window.loadFile(file);
    await window.webContents.executeJavaScript("document.fonts.ready", true);
    // An X11 window may take a moment to get its size, and the window manager adds a few pixels
    // to it (the result records the viewport the page had).
    for (let i = 0, last = null; scale !== 1 && i < 50; i++) {
      const size = await viewportOf();
      if (Math.abs(size[0] - fixture.width) < 4 && Math.abs(size[1] - fixture.height) < 4 && last && last[0] === size[0] && last[1] === size[1]) break;
      last = size;
      await wait(100);
    }
    // Offscreen pages only get focus (focus events, :focus) when the window is shown and focused.
    window.showInactive();
    window.focus();
    window.webContents.focus();
    await wait(50);
    if (!await window.webContents.executeJavaScript("document.hasFocus()", true))
      throw new Error(fixture.name + ": the page didn't get focus");
    await window.webContents.executeJavaScript(INSTRUMENT, true);
    await wait(100);
    Object.assign(clicks, { time: -Infinity, button: null, count: 0, now: 0 });
    fixtureStart = Date.now();
    snapshots = [];
    currentFixture = fixture.name;
    held.clear();
    for (const step of fixture.steps)
      await play(window.webContents, step);
    await wait(100);
    results[fixture.name] = await window.webContents.executeJavaScript(COLLECT, true);
    console.log(fixture.name, "played", fixture.steps.length, "steps");
    results[fixture.name].snapshots = snapshots.map(s => ({ states: s.states, activeElement: s.activeElement, pageSelection: s.pageSelection }));
    if (scale !== 1)
      results[fixture.name].viewport = await viewportOf();
    // The pointer leaves, so the next page starts unhovered.
    window.webContents.sendInputEvent({ type: "mouseLeave", x: -1, y: -1 });
  }
  window.destroy();
  fs.writeFileSync(argument("--out"), JSON.stringify(results, null, 1) + "\n");
  fs.rmSync(scratch, { recursive: true, force: true });
  app.quit();
}).catch(error => {
  console.error(error);
  app.exit(1);
});
