import { readFile, writeFile } from "node:fs/promises";
import { createHash } from "node:crypto";
import { execFileSync } from "node:child_process";
import { fileURLToPath } from "node:url";
const root = new URL("../out/", import.meta.url);
const version = JSON.parse(await readFile(new URL("../plugin-version.json", import.meta.url), "utf8"));
const manifest = JSON.parse(await readFile(new URL(`${version.uuid}.sdPlugin/manifest.json`, root), "utf8"));
if (manifest.UUID !== version.uuid || manifest.Version !== version.version || manifest.Software.MinimumVersion !== version.minimumSoftware)
  throw new Error("Plugin manifest metadata mismatch");
const bytes = await readFile(new URL(`${version.uuid}.streamDeckPlugin`, root));
const packaged = JSON.parse(execFileSync("powershell.exe", ["-NoProfile", "-File", fileURLToPath(new URL("read-package-manifest.ps1", import.meta.url)),
  "-Package", fileURLToPath(new URL(`${version.uuid}.streamDeckPlugin`, root))], { encoding: "utf8" }));
if (packaged.UUID !== version.uuid || packaged.Version !== version.version || packaged.Software.MinimumVersion !== version.minimumSoftware)
  throw new Error("Packed bytes contain inconsistent plugin metadata");
await writeFile(new URL("plugin-metadata.json", root), JSON.stringify({ ...version, sha256: createHash("sha256").update(bytes).digest("hex") }, null, 2));
