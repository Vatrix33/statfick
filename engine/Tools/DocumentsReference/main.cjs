"use strict";
// The Documents folder reference (Docs/Assets.md, "Character files"): what Electron 44.5.0's
// app.getPath("documents") gives on Linux, the old game's default folder for character files, for
// each case of the user's environment and XDG user-dirs file. Chromium 152 looks it up through
// base::nix::GetXDGUserDirectory("DOCUMENTS", "Documents") and xdg_user_dir_lookup; each case runs in
// an Electron process of its own (Chromium caches its paths) with a temporary home folder.
//
// Usage (Tools/Scripts/make-documents-reference.sh):
//   electron --no-sandbox . --out=/abs/documents-reference.json   every case, written as JSON
//   electron --no-sandbox . --probe --probe-data=/abs/dir          one probe: prints the path
//   --dotnet=/abs/program  also runs this program (dotnet-documents.cs, built) in each case's
//                          environment: what .NET's SpecialFolder.MyDocuments gives, to compare
// In the results the temporary home folder is written "<home>" (in file contents too); a file's
// content is "text" or, for bytes that aren't UTF-8, "base64". Chromium makes the folder when it is
// missing (chrome_paths.cc: create_dir) and fails when it can't: "created" lists the folders it
// made under the home folder, with their modes.
const { app } = require("electron");
const childProcess = require("child_process");
const fs = require("fs");
const os = require("os");
const path = require("path");

const argument = name => {
  const prefix = name + "=";
  const found = process.argv.find(a => a.startsWith(prefix));
  return found ? found.slice(prefix.length) : null;
};

const MARK = "DOCUMENTS-REFERENCE ";

if (process.argv.includes("--probe")) {
  // Electron's own files go to a folder of the case's, never the real profile.
  app.setPath("userData", argument("--probe-data"));
  app.disableHardwareAcceleration();
  app.whenReady().then(() => {
    let result;
    try {
      result = { documents: app.getPath("documents") };
    } catch (e) {
      result = { error: String(e && e.message || e) };
    }
    process.stdout.write(MARK + JSON.stringify(result) + "\n");
    app.exit(0);
  });
} else {
  // An exception in Electron's main process would wait in a dialog: report it and quit instead.
  try {
    capture();
  } catch (e) {
    process.stderr.write("documents reference: " + (e && e.stack || e) + "\n");
    app.exit(1);
  }
}

function capture() {
  const DIRS = ".config/user-dirs.dirs";
  const line = value => `XDG_DOCUMENTS_DIR="${value}"\n`;
  const STANDARD = [
    "# This file is written by xdg-user-dirs-update\n",
    'XDG_DESKTOP_DIR="$HOME/Desktop"\n',
    'XDG_DOWNLOAD_DIR="$HOME/Downloads"\n',
    'XDG_DOCUMENTS_DIR="$HOME/Documents"\n',
    'XDG_MUSIC_DIR="$HOME/Music"\n',
  ].join("");
  // Each case: the files under the home folder (text, or a Buffer), environment variables ("<home>"
  // stands for the home folder; null removes the variable) and the working folder.
  const CASES = [
    { name: "no-user-dirs-file", files: {} },
    { name: "standard", files: { [DIRS]: STANDARD } },
    { name: "localized", files: { [DIRS]: line("$HOME/Dokumente") } },
    // Absolute paths inside the home folder: Chromium makes the folder, so one it may not make
    // (/srv/…) would fail or not depending on the machine.
    { name: "absolute", files: { [DIRS]: line("<home>/abs/documents") } },
    { name: "home-alone", files: { [DIRS]: line("$HOME") } },
    { name: "relative", files: { [DIRS]: line("Documents") } },
    { name: "dollar-home-without-slash", files: { [DIRS]: line("$HOMEDocs") } },
    { name: "trailing-slashes", files: { [DIRS]: line("$HOME/Docs//") } },
    { name: "home-slash", files: { [DIRS]: line("$HOME/") } },
    { name: "root", files: { [DIRS]: line("/") } },
    { name: "double-slash-inside", files: { [DIRS]: line("<home>//abs//docs") } },
    { name: "existing-file", files: { [DIRS]: line("$HOME/taken"), taken: "a file, not a folder" } },
    { name: "nested-missing", files: { [DIRS]: line("$HOME/a/b/c") } },
    { name: "last-wins", files: { [DIRS]: line("$HOME/First") + line("$HOME/Second") } },
    { name: "invalid-later-line-keeps-earlier", files: { [DIRS]: line("$HOME/Kept") + line("relative/path") } },
    { name: "spaces-and-tabs", files: { [DIRS]: '  \tXDG_DOCUMENTS_DIR \t= \t"$HOME/Spaced Out"\n' } },
    { name: "escapes", files: { [DIRS]: 'XDG_DOCUMENTS_DIR="$HOME/My \\"Docs\\" \\\\ \\x"\n' } },
    { name: "unclosed-quote", files: { [DIRS]: 'XDG_DOCUMENTS_DIR="$HOME/Open' } },
    { name: "backslash-at-end", files: { [DIRS]: 'XDG_DOCUMENTS_DIR="$HOME/end\\' } },
    { name: "escape-before-the-end", files: { [DIRS]: 'XDG_DOCUMENTS_DIR="$HOME/end\\x' } },
    { name: "text-after-quote", files: { [DIRS]: 'XDG_DOCUMENTS_DIR="$HOME/Docs" # a comment\n' } },
    { name: "comment", files: { [DIRS]: '# XDG_DOCUMENTS_DIR="$HOME/Commented"\n' } },
    { name: "no-equals", files: { [DIRS]: 'XDG_DOCUMENTS_DIR "$HOME/NoEquals"\n' } },
    { name: "no-quote", files: { [DIRS]: "XDG_DOCUMENTS_DIR=$HOME/NoQuote\n" } },
    { name: "lowercase-key", files: { [DIRS]: 'xdg_documents_dir="$HOME/Lower"\n' } },
    { name: "longer-key", files: { [DIRS]: 'XDG_DOCUMENTS_DIRS="$HOME/Longer"\n' } },
    { name: "other-keys-only", files: { [DIRS]: 'XDG_DESKTOP_DIR="$HOME/Desktop"\nXDG_MUSIC_DIR="$HOME/Music"\n' } },
    { name: "crlf", files: { [DIRS]: 'XDG_DOCUMENTS_DIR="$HOME/Windows"\r\n' } },
    { name: "crlf-unclosed", files: { [DIRS]: 'XDG_DOCUMENTS_DIR="$HOME/Crlf\r\n' } },
    { name: "non-ascii", files: { [DIRS]: line("$HOME/Документы") } },
    { name: "not-utf8", files: { [DIRS]: Buffer.concat([Buffer.from('XDG_DOCUMENTS_DIR="$HOME/Bad'), Buffer.from([0xff, 0xfe]), Buffer.from('x"\n')]) } },
    { name: "nul-byte", files: { [DIRS]: Buffer.concat([Buffer.from('XDG_DOCUMENTS_DIR="$HOME/Nul'), Buffer.from([0]), Buffer.from('Tail"\n')]) } },
    // fgets reads at most 511 bytes at a time: a longer line is cut, and its pieces are read as lines.
    { name: "long-line-cut", files: { [DIRS]: line("$HOME/" + "a".repeat(600)) } },
    { name: "long-line-piece", files: { [DIRS]: "#" + "x".repeat(510) + 'XDG_DOCUMENTS_DIR="$HOME/Piece"\n' } },
    { name: "line-of-511", files: { [DIRS]: line("$HOME/" + "b".repeat(511 - 26)) + line("$HOME/AfterExact") } },
    { name: "dirs-file-is-a-folder", files: { [DIRS + "/x"]: "" } },
    { name: "config-home", env: { XDG_CONFIG_HOME: "<home>/cfg" }, files: { "cfg/user-dirs.dirs": line("$HOME/FromConfigHome"), [DIRS]: line("$HOME/FromDotConfig") } },
    { name: "config-home-without-file", env: { XDG_CONFIG_HOME: "<home>/cfg" }, files: { [DIRS]: line("$HOME/FromDotConfig") } },
    { name: "config-home-empty", env: { XDG_CONFIG_HOME: "" }, files: { [DIRS]: line("$HOME/FromDotConfig") } },
    { name: "config-home-relative", env: { XDG_CONFIG_HOME: "rel" }, cwd: "<home>", files: { "rel/user-dirs.dirs": line("$HOME/FromRelative") } },
    { name: "config-home-trailing-slash", env: { XDG_CONFIG_HOME: "<home>/cfg/" }, files: { "cfg/user-dirs.dirs": line("$HOME/FromSlash") } },
    { name: "documents-variable-only", env: { XDG_DOCUMENTS_DIR: "<home>/from-variable" }, files: {} },
    { name: "home-trailing-slash", env: { HOME: "<home>/" }, files: { [DIRS]: line("$HOME/Docs") } },
    { name: "home-trailing-slash-without-file", env: { HOME: "<home>/" }, files: {} },
    { name: "home-unset", env: { HOME: null }, files: {} },
    { name: "home-empty", env: { HOME: "" }, files: {} },
  ];

  const out = argument("--out");
  if (!out)
    throw new Error("usage: --out=/abs/documents-reference.json");
  const scratch = fs.mkdtempSync(path.join(os.tmpdir(), "documents-reference-"));
  const results = [];
  for (const c of CASES) {
    const home = fs.mkdtempSync(path.join(scratch, c.name + "-"));
    const sub = text => (typeof text === "string" ? text.split("<home>").join(home) : text);
    for (const [relative, content] of Object.entries(c.files)) {
      const file = path.join(home, relative);
      fs.mkdirSync(path.dirname(file), { recursive: true });
      fs.writeFileSync(file, sub(content));
    }
    const env = { ...process.env };
    for (const name of ["HOME", "XDG_CONFIG_HOME", "XDG_DOCUMENTS_DIR"])
      delete env[name];
    env.HOME = home;
    for (const [name, value] of Object.entries(c.env || {}))
      if (value === null)
        delete env[name];
      else
        env[name] = sub(value);
    const cwd = c.cwd ? sub(c.cwd) : scratch;
    const ask = (program, args) => {
      const probe = childProcess.spawnSync(program, args, { env, cwd, encoding: "utf8", timeout: 60000 });
      const found = (probe.stdout || "").split("\n").find(l => l.startsWith(MARK));
      if (!found)
        throw new Error(`${c.name}: no answer from ${program} (status ${probe.status}):\n${probe.stdout}\n${probe.stderr}`);
      return JSON.parse(found.slice(MARK.length));
    };
    // Every folder under the home folder, as bytes (Chromium makes names that aren't UTF-8 as they are).
    const folders = () => {
      const found = [];
      const walk = folder => {
        for (const entry of fs.readdirSync(folder, { withFileTypes: true, encoding: "buffer" }))
          if (entry.isDirectory()) {
            const full = Buffer.concat([folder, Buffer.from("/"), entry.name]);
            found.push(full);
            walk(full);
          }
      };
      walk(Buffer.from(home));
      return found;
    };
    const before = new Set(folders().map(f => f.toString("latin1")));
    const electronData = path.join(scratch, c.name + "-electron-data");
    const answer = ask(process.execPath, [__dirname, "--no-sandbox", "--probe", "--probe-data=" + electronData]);
    // Folders Chromium made under the home folder, with their modes (not ~/.cache, where the
    // machine's font and shader caches go).
    const caches = Buffer.from(path.join(home, ".cache"));
    const created = folders().filter(f => !before.has(f.toString("latin1")) && !f.subarray(0, caches.length).equals(caches))
      .map(f => ({ folder: f.toString("utf8").split(home).join("<home>"), mode: (fs.statSync(f).mode & 0o777).toString(8) }));
    const dotnet = argument("--dotnet") ? ask(argument("--dotnet"), []).documents : null;
    const unsub = text => text.split(home).join("<home>");
    const files = {};
    for (const [relative, content] of Object.entries(c.files)) {
      const bytes = Buffer.isBuffer(content) ? content : Buffer.from(content, "utf8");
      files[relative] = Buffer.isBuffer(content) ? { base64: bytes.toString("base64") } : { text: content };
    }
    const result = { name: c.name, env: c.env || {}, files };
    if (c.cwd)
      result.cwd = c.cwd;
    if ("documents" in answer)
      result.documents = unsub(answer.documents);
    else
      result.error = answer.error;
    result.created = created;
    if (dotnet !== null)
      result.dotnet = unsub(dotnet);
    results.push(result);
    process.stderr.write(`${c.name}: ${JSON.stringify(result.documents ?? result.error)}\n`);
  }
  fs.rmSync(scratch, { recursive: true, force: true });
  const document = {
    about: "Electron's app.getPath(\"documents\") on Linux per case, from Tools/DocumentsReference (make-documents-reference.sh)",
    electron: process.versions.electron,
    chrome: process.versions.chrome,
    cases: results,
  };
  fs.writeFileSync(out, JSON.stringify(document, null, 2) + "\n");
  app.exit(0);
}
