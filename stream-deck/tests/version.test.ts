import test from "node:test";
import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
test("authoritative metadata determines generated plugin identity and versions", () => {
  const source = JSON.parse(readFileSync(new URL("../../plugin-version.json", import.meta.url), "utf8"));
  const manifest = JSON.parse(readFileSync(new URL("../../out/io.github.zebrajaeger.tinyhand.sdPlugin/manifest.json", import.meta.url), "utf8"));
  assert.equal(manifest.Version, source.version);
  assert.equal(manifest.UUID, source.uuid);
  assert.equal(manifest.Software.MinimumVersion, source.minimumSoftware);
  assert.match(source.version, /^\d+\.\d+\.\d+\.\d+$/);
});
