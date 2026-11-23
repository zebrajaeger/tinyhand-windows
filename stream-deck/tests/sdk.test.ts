import test from "node:test";
import assert from "node:assert/strict";
import streamDeck, { action, SingletonAction } from "@elgato/streamdeck";
import { fileURLToPath } from "node:url";

test("official SDK loads with patched zod v4-mini and registers actions", () => {
  process.chdir(fileURLToPath(new URL("../../out/io.github.zebrajaeger.tinyhand.sdPlugin", import.meta.url)));
  @action({ UUID: "io.github.zebrajaeger.tinyhand.pointer" })
  class TestAction extends SingletonAction {}
  assert.doesNotThrow(() => streamDeck.actions.registerAction(new TestAction()));
});
