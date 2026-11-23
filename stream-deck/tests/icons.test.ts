import test from "node:test";
import assert from "node:assert/strict";
import { readFileSync, existsSync } from "node:fs";
import { kinds } from "../src/controls.js";
import { iconSvg, brandedIcon } from "../src/icons.js";

test("all sidebar actions have distinct existing icons and keypad states", () => {
  const root = new URL("../../out/io.github.zebrajaeger.tinyhand.sdPlugin/", import.meta.url);
  const manifest = JSON.parse(readFileSync(new URL("manifest.json", root), "utf8"));
  assert.equal(new Set(kinds.map(kind => iconSvg(kind))).size, 7);
  for (const action of manifest.Actions) {
    assert.ok(existsSync(new URL(`${action.Icon}.svg`, root)));
    assert.equal(action.Icon, action.States[0].Image);
    assert.ok(readFileSync(new URL(`${action.Icon}.svg`, root), "utf8").includes('href="data:image/png;base64,'));
    for (const state of action.States) {
      const svg = readFileSync(new URL(`${state.Image}.svg`, root), "utf8");
      assert.ok(svg.includes('href="data:image/png;base64,'));
    }
    assert.notEqual(action.Icon, "icon");
  }
  assert.deepEqual(readFileSync(new URL("icons/pointer.png", root)), readFileSync(new URL("icon.png", root)));
});

test("all key backgrounds retain branding in either state without full green fills", () => {
  for (const kind of kinds) for (const selected of [false, true]) {
    const svg = brandedIcon(kind, "data:image/png;base64,TEST", selected);
    assert.ok(svg.includes('href="data:image/png;base64,TEST"'));
    assert.ok(!svg.includes("#4e9e71"));
    if (kind !== "pointer") assert.ok(svg.includes('translate(72 14)'));
  }
});
