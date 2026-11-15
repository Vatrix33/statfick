"use strict";
/* The old game's oracle (GM2-P13; Docs/Game/Hero.md, "How it works", step 2): runs the old game's own code on a module's
   case list and writes what it gives, so the port's tests compare their rules with it where there is no Node or
   Chromium. Run by Tools/Scripts/make-old-game-references.sh; never changes the old game (it only reads it).

   A module is a file in modules/ (its name is the file's without ".cjs"), CommonJS, exporting:
     where    "node" (the old game's pure modules, loaded here with require) or "client" (the old game's page in its
              headless test client, Chromium 152, for the rules inside app.js and combat.js)
     sources  the old game's files the results depend on (paths in old-game/), their SHA-256 written with the results
     cases(old)        the case list, made here in Node (no randomness but a seeded one): each case a JSON value;
                       old.json(path) reads an old-game data file, old.cases(file) a case file of cases/ (a list too
                       large or too shared to write in the module, kept as data: cases/race-cases.json)
     run(input, old)   one case's output: called here for "node" (old.require(path) loads an old-game script), in the
                       page for "client" as run(input, setup) (async allowed; the old game's globals are the page's)
     setup(old)        (client, optional) data made here once and given to every run in the page (crafted races …)
     uses     (client, optional) helper files of shared/ put before the module's source in the page, in the same scope
   The results file (Tests/Statventure.Game.Tests/Reference/old-game/<module>.json) is
     { "module", "made", "sources": { path: sha256 }, "cases": [ { "input": …, "output": … }, … ] }
   one case per line. Numbers are JSON's (V8's shortest round trip, read back exactly as doubles); the numbers JSON
   has no form for are the strings "Infinity", "-Infinity" and "NaN" (and -0 is written 0).

   node oracle.cjs node <module> <old-game> <out.json>
   node oracle.cjs client-script <module> <old-game> <script.js>    the page's script (call oracleMain)
   node oracle.cjs client-result <module> <old-game> <saved.json> <out.json>
*/
const crypto = require("node:crypto");
const fs = require("node:fs");
const path = require("node:path");

const MODULES = path.join(__dirname, "modules");

// Non-finite numbers as strings (JSON has no form for them).
const encode = (key, value) => typeof value === "number" && !Number.isFinite(value) ? String(value) : value;

function loadModule(name) {
  const file = path.join(MODULES, name + ".cjs");
  if (!fs.existsSync(file)) throw new Error("no oracle module " + name + " (" + file + ")");
  const module = require(file);
  if (!["node", "client"].includes(module.where)) throw new Error(name + ": where must be \"node\" or \"client\"");
  if (!Array.isArray(module.sources) || typeof module.cases !== "function" || typeof module.run !== "function")
    throw new Error(name + ": a module exports sources, cases(old) and run(input, old)");
  return module;
}

// What a module's case list may read of the old game: its data files, and its pure scripts (Node only).
function oldGame(folder) {
  return {
    folder,
    json: file => JSON.parse(fs.readFileSync(path.join(folder, file), "utf8")),
    require: file => require(path.join(folder, file)),
    cases: file => JSON.parse(fs.readFileSync(path.join(__dirname, "cases", file), "utf8")),
  };
}

function cases(module, old) {
  const list = module.cases(old);
  if (!Array.isArray(list) || list.length === 0) throw new Error("a module's case list is a nonempty array");
  return JSON.parse(JSON.stringify(list, encode));
}

function write(name, module, old, results, out) {
  const sources = Object.fromEntries(module.sources.map(file => [file,
    crypto.createHash("sha256").update(fs.readFileSync(path.join(old.folder, file))).digest("hex")]));
  const lines = results.map(entry => "    " + JSON.stringify({ input: entry.input, output: entry.output }, encode));
  const head = JSON.stringify({ module: name, made: "Tools/Scripts/make-old-game-references.sh " + name, sources }, null, 2);
  fs.mkdirSync(path.dirname(out), { recursive: true });
  fs.writeFileSync(out, head.slice(0, -2) + ",\n  \"cases\": [\n" + lines.join(",\n") + "\n  ]\n}\n");
  console.log(name + ": " + results.length + " cases -> " + out);
}

async function main() {
  const [command, name, folder, ...rest] = process.argv.slice(2);
  if (!command || !name || !folder) throw new Error("usage: oracle.cjs node|client-script|client-result <module> <old-game> …");
  const module = loadModule(name);
  const old = oldGame(path.resolve(folder));
  if (command === "node") {
    if (module.where !== "node") throw new Error(name + " runs in the client");
    const results = [];
    for (const input of cases(module, old)) results.push({ input, output: await module.run(input, old) });
    write(name, module, old, results, rest[0]);
  } else if (command === "client-script") {
    if (module.where !== "client") throw new Error(name + " runs in Node");
    // The page runs the module's own source in a scope of its own: its run(input) and helpers, then every case in turn;
    // the results leave through the test client's save hook (which needs a "stats" key; 10 MiB at most).
    const shared = (module.uses || []).map(file => fs.readFileSync(path.join(__dirname, "shared", file + ".js"), "utf8"));
    const source = fs.readFileSync(path.join(MODULES, name + ".cjs"), "utf8");
    const script = `(() => {
  const module = { exports: {} };
${shared.join("\n")}
${source}
  const cases = ${JSON.stringify(cases(module, old))};
  const setup = ${JSON.stringify(module.setup ? module.setup(old) : null, encode)};
  const encode = ${encode.toString()};
  window.oracleMain = async () => {
    const results = [];
    for (const input of cases) results.push({ input, output: await module.exports.run(input, setup) });
    const text = JSON.stringify({ stats: {}, oracle: results }, encode);
    if (!(await window.statFickDesktop.saveCharacter(text))) throw new Error("the save hook refused the results");
  };
})();
`;
    fs.writeFileSync(rest[0], script);
  } else if (command === "client-result") {
    const saved = JSON.parse(fs.readFileSync(rest[0], "utf8"));
    if (!Array.isArray(saved.oracle)) throw new Error("no results in " + rest[0]);
    const expected = cases(module, old);
    if (JSON.stringify(saved.oracle.map(entry => entry.input)) !== JSON.stringify(expected))
      throw new Error(name + ": the page's results aren't the case list's");
    write(name, module, old, saved.oracle, rest[1]);
  } else throw new Error("unknown command " + command);
}

main().catch(error => { console.error(error.stack || String(error)); process.exit(1); });
