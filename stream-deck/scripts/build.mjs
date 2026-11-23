import { mkdir, cp, writeFile, rm, readFile } from "node:fs/promises";
import { build } from "esbuild";
import { execFileSync } from "node:child_process";
import { fileURLToPath } from "node:url";
import { iconSvg, brandedIcon } from "../build/src/icons.js";
const root = new URL("../out/io.github.zebrajaeger.tinyhand.sdPlugin/", import.meta.url);
const version = JSON.parse(await readFile(new URL("../plugin-version.json", import.meta.url), "utf8"));
await mkdir(new URL("bin/", root), { recursive: true });
await cp(new URL("../assets/", import.meta.url), root, { recursive: true });
await rm(new URL("icon.svg", root), { force: true });
// The same README artwork generates the Windows ICO and Stream Deck brand images.
execFileSync("powershell.exe", ["-NoProfile", "-File", fileURLToPath(new URL("../../windows/make-app-icon.ps1", import.meta.url)),
  "-PluginOutput", fileURLToPath(root)]);
await build({ entryPoints: [new URL("../src/plugin.ts", import.meta.url).pathname.replace(/^\/([A-Z]:)/i, "$1")],
  bundle: true, platform: "node", target: "node24", format: "esm", outfile: new URL("bin/plugin.js", root).pathname.replace(/^\/([A-Z]:)/i, "$1"),
  banner: { js: 'import { createRequire } from "node:module"; const require = createRequire(import.meta.url);' } });
const names = { pointer: "Zeiger ein/aus", size: "Zeigergröße", sound: "Ton ein/aus", color: "Zeichenfarbe", text: "Text platzieren", capture: "Screenshot", clear: "Alles löschen" };
await mkdir(new URL("icons/", root), { recursive: true });
for (const id of Object.keys(names).filter(id => id !== "pointer")) await writeFile(new URL(`icons/${id}.svg`, root), iconSvg(id));
await rm(new URL("icons/pointer.svg", root), { force: true });
await cp(new URL("icon.png", root), new URL("icons/pointer.png", root));
await cp(new URL("icon@2x.png", root), new URL("icons/pointer@2x.png", root));
await mkdir(new URL("keys/", root), { recursive: true });
const brandImage = `data:image/png;base64,${(await readFile(new URL("icon@2x.png", root))).toString("base64")}`;
for (const id of Object.keys(names)) {
  await writeFile(new URL(`keys/${id}.svg`, root), brandedIcon(id, brandImage));
  await writeFile(new URL(`keys/${id}-selected.svg`, root), brandedIcon(id, brandImage, true));
}
const manifest = { $schema: "https://schemas.elgato.com/streamdeck/plugins/manifest.json", UUID: "io.github.zebrajaeger.tinyhand",
  Name: "TinyHand für Windows", Author: "zebrajaeger", Description: "TinyHand-Zeiger und Anmerkungen steuern.", Version: version.version,
  CodePath: "bin/plugin.js", Nodejs: { Version: "24" }, SDKVersion: 2, Software: { MinimumVersion: version.minimumSoftware },
  OS: [{ Platform: "windows", MinimumVersion: "10" }], Icon: "icon", CategoryIcon: "icon", Category: "TinyHand für Windows",
  URL: "https://github.com/zebrajaeger/tinyhand-windows", PropertyInspectorPath: "inspector.html",
  Actions: Object.entries(names).map(([id, name]) => ({ UUID: `io.github.zebrajaeger.tinyhand.${id}`, Name: name, Icon: `keys/${id}`,
    Controllers: ["Keypad"], SupportedInMultiActions: false, UserTitleEnabled: false,
    States: [{ Image: `keys/${id}`, TitleAlignment: "bottom" }, { Image: `keys/${id}-selected`, TitleAlignment: "bottom" }], DisableAutomaticStates: true })) };
await writeFile(new URL("manifest.json", root), JSON.stringify(manifest, null, 2));
await writeFile(new URL("package.json", root), '{"type":"module"}');
