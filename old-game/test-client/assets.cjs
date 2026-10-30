"use strict";
const fs = require("node:fs/promises");
const path = require("node:path");
const ORIGIN = "statfick://game";
const MIME = {
  ".html": "text/html; charset=utf-8", ".js": "application/javascript; charset=utf-8",
  ".css": "text/css; charset=utf-8", ".json": "application/json; charset=utf-8",
  ".svg": "image/svg+xml", ".png": "image/png", ".ico": "image/x-icon",
  ".mp3": "audio/mpeg", ".wav": "audio/wav",
  ".ttf": "font/ttf", ".otf": "font/otf", ".woff": "font/woff", ".woff2": "font/woff2"
};
const CSP = "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; img-src 'self' data: blob:; media-src 'self' blob:; connect-src 'self'; font-src 'self' data:; frame-src 'self'; object-src 'none'; base-uri 'self'";
function localUrl(value) {
  try { const url = new URL(value); return url.protocol === "statfick:" && url.host === "game"; }
  catch { return false; }
}
function requestPath(value) {
  const url = new URL(value);
  if (!localUrl(value)) throw new Error("Invalid app origin");
  const relative = decodeURIComponent(url.pathname).replace(/^\/+/, "") || "index.html";
  if (relative.includes("\\") || relative.includes("\0") || relative.split("/").some(part => part === ".." || part === ".")) throw new Error("Invalid asset path");
  if (relative !== "index.html" && relative !== "styles.css" && !/^(scripts|objects|classes|enemies|music)(\/|$)/.test(relative)) throw new Error("Asset outside game folders");
  return relative;
}
function inside(root, file) {
  const relative = path.relative(root, file);
  return relative === "" || (!relative.startsWith(".." + path.sep) && relative !== ".." && !path.isAbsolute(relative));
}
function escapeHtml(value) {
  return value.replace(/[&<>"']/g, char => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" })[char]);
}
async function safeFile(root, relative) {
  const resolved = path.resolve(root, relative);
  if (!inside(root, resolved)) return null;
  try {
    const [realRoot, realFile] = await Promise.all([fs.realpath(root), fs.realpath(resolved)]);
    if (!inside(realRoot, realFile)) return null;
    return { file: realFile, stat: await fs.stat(realFile) };
  } catch (error) {
    if (["ENOENT", "ENOTDIR"].includes(error.code)) return null;
    throw error;
  }
}
function createAssetHandler(getRoots) {
  return async request => {
    let relative;
    try { relative = requestPath(request.url); }
    catch { return new Response("Forbidden", { status: 403 }); }
    if (!["GET", "HEAD"].includes(request.method || "GET")) return new Response("Method not allowed", { status: 405 });
    try {
      let asset;
      for (const root of [...new Set(getRoots())]) { asset = await safeFile(root, relative); if (asset) break; }
      if (!asset) return new Response("Game file not found: " + relative, { status: 404 });
      const headers = { "Cache-Control": "no-store", "Content-Security-Policy": CSP, "X-Content-Type-Options": "nosniff" };
      if (asset.stat.isDirectory()) {
        const entries = await fs.readdir(asset.file, { withFileTypes: true });
        const links = entries.sort((a, b) => a.name.localeCompare(b.name)).map(entry => `<a href="${encodeURIComponent(entry.name)}${entry.isDirectory() ? "/" : ""}">${escapeHtml(entry.name)}</a>`);
        headers["Content-Type"] = MIME[".html"];
        return new Response(request.method === "HEAD" ? null : '<!doctype html><meta charset="utf-8">' + links.join("\n"), { headers });
      }
      headers["Content-Type"] = MIME[path.extname(asset.file).toLowerCase()] || "application/octet-stream";
      headers["Accept-Ranges"] = "bytes";
      const bytes = await fs.readFile(asset.file);
      const range = request.headers?.get("range");
      if (range) {
        const match = /^bytes=(\d*)-(\d*)$/.exec(range);
        const start = match?.[1] ? Number(match[1]) : Math.max(0, bytes.length - Number(match?.[2]));
        const end = match?.[1] ? (match[2] ? Math.min(bytes.length - 1, Number(match[2])) : bytes.length - 1) : bytes.length - 1;
        if (!match || (!match[1] && !match[2]) || !Number.isSafeInteger(start) || start > end || start >= bytes.length || end < 0) {
          headers["Content-Range"] = "bytes */" + bytes.length;
          return new Response(null, { status: 416, headers });
        }
        headers["Content-Range"] = `bytes ${start}-${end}/${bytes.length}`;
        headers["Content-Length"] = String(end - start + 1);
        return new Response(request.method === "HEAD" ? null : bytes.subarray(start, end + 1), { status: 206, headers });
      }
      headers["Content-Length"] = String(bytes.length);
      return new Response(request.method === "HEAD" ? null : bytes, { headers });
    } catch (error) {
      console.error("Could not read game asset", relative, error);
      return new Response("Could not read game file", { status: 500 });
    }
  };
}
module.exports = { ORIGIN, localUrl, requestPath, inside, createAssetHandler };
