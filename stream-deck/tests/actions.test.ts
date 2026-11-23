import test from "node:test";
import assert from "node:assert/strict";
import { Control, connection } from "../src/plugin.js";
import { kinds } from "../src/controls.js";

test("SDK adapters for all seven keys, duplicate press guard and multi-key state", async () => {
  connection.status = "connected";
  connection.state = { active: false, pointerHeight: 340, soundEnabled: false, annotationColor: "#E14164", annotationCount: 0, textPhase: "idle", capturePending: false };
  const commands: string[] = [];
  connection.command = async (command: string) => { commands.push(command); return { ok: true }; };
  const make = (id: string) => {
    const calls: string[] = [];
    return { id, calls, isKey: () => true, setTitle: async (s: string) => { calls.push(s); }, setState: async () => {}, setImage: async () => {}, showOk: async () => { calls.push("ok"); }, showAlert: async () => { calls.push("alert"); } };
  };
  for (const kind of kinds) {
    const handler = new Control(kind);
    const action = make(kind);
    const event = { action, payload: { settings: {} } } as never;
    await handler.onWillAppear(event);
    await handler.onKeyDown(event);
    assert.ok(action.calls.includes("ok"));
    handler.onWillDisappear(event);
  }
  assert.equal(commands.length, 7);
  const handler = new Control("pointer");
  const first = make("one"), second = make("two");
  const event = (action: unknown) => ({ action, payload: { settings: {} } }) as never;
  await handler.onWillAppear(event(first)); await handler.onWillAppear(event(second));
  let release!: () => void;
  connection.command = () => new Promise(resolve => { release = () => resolve({ ok: true }); });
  const pending = handler.onKeyDown(event(first));
  await handler.onKeyDown(event(first));
  // Rendering is asynchronous; wait for its SDK calls to finish, not a timed sleep.
  while (!release) await new Promise<void>(resolve => setImmediate(resolve));
  release(); await pending;
  connection.state = { ...connection.state!, active: true };
  connection.emit("change");
  await new Promise<void>(resolve => setImmediate(resolve));
  assert.ok(first.calls.includes("Zeiger AN") && second.calls.includes("Zeiger AN"));
  connection.status = "unavailable"; connection.state = undefined; connection.emit("change");
  await new Promise<void>(resolve => setImmediate(resolve));
  assert.ok(first.calls.includes("× Offline"));
  handler.onWillDisappear(event(first)); handler.onWillDisappear(event(second));
  connection.stop();
});
