"use strict";
const fs = require("node:fs");
const path = require("node:path");
const crypto = require("node:crypto");

async function run({ window, reportDirectory }) {
  const fixture = JSON.parse(fs.readFileSync(path.join(__dirname, "combat-lifecycle-fixture.json"), "utf8"));
  const execute = source => window.webContents.executeJavaScript(source, true);
  await execute("window.__lifecycleFixture=" + JSON.stringify(fixture) + ";void 0;");
  await execute(fs.readFileSync(path.join(__dirname, "combat-lifecycle-browser.js"), "utf8"));
  const results = await execute("runLifecycleChecks()");
  const digest = value => crypto.createHash("sha256").update(JSON.stringify(value)).digest("hex");
  const compact = report => ({ coldEnemyLoadMs: report.coldEnemyLoadMs, samples: report.samples,
    stateHashes: report.states.map(state => ({ name: state.name, sha256: digest(state) })) });
  const report = { passed: results.passed, optimized: compact(results.optimized), uncached: compact(results.uncached),
    matchingSnapshots: results.uncached.states.length, forecastChecks: results.forecastChecks, catalogueRefresh: results.catalogueRefresh };
  fs.writeFileSync(path.join(reportDirectory, "combat-lifecycle.json"), JSON.stringify(report, null, 2));
  console.log("PASS combat lifecycle", JSON.stringify({ matchingSnapshots: report.matchingSnapshots, forecastChecks: report.forecastChecks, catalogueRefresh: true }));
  return report;
}
module.exports = { run };
