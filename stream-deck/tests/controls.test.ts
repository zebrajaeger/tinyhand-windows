import test from "node:test";
import assert from "node:assert/strict";
import { kinds, request, display } from "../src/controls.js";
import { validState, type State } from "../src/connection.js";
const state: State = { active: false, pointerHeight: 340, soundEnabled: false, annotationColor: "#E14164", annotationCount: 2, textPhase: "idle", capturePending: false };
test("seven direct actions and settings validation", () => {
  assert.equal(kinds.length, 7);
  for (const kind of kinds) assert.ok(request(kind, {}));
  assert.equal(request("size", { mode: "fixed", height: 999 }), undefined);
  assert.equal(request("color", { color: "red<script>" }), undefined);
  assert.deepEqual(request("size", { mode: "fixed", height: 220 }), { command: "pointer.setSize", args: { height: 220 } });
  assert.deepEqual(request("color", { color: "#abcdef" }), { command: "annotation.setColor", args: { color: "#ABCDEF" } });
});
test("state-derived feedback distinguishes offline, incompatible and off", () => {
  assert.equal(display("pointer", {}, "connected", state).title, "Zeiger AUS");
  assert.equal(display("pointer", {}, "connected", { ...state, active: true }).selected, true);
  assert.notEqual(display("pointer", {}, "unavailable").title, display("pointer", {}, "connected", state).title);
  assert.notEqual(display("pointer", {}, "incompatible").title, display("pointer", {}, "unavailable").title);
  assert.equal(display("size", { mode: "fixed", height: 340 }, "connected", state).selected, true);
  assert.equal(display("color", { color: "#e14164" }, "connected", state).selected, true);
  assert.equal(display("text", {}, "connected", { ...state, textPhase: "editing" }).title, "Eingabe …");
  assert.equal(display("capture", {}, "connected", { ...state, capturePending: true }).selected, true);
});
test("snapshot validation rejects malformed state", () => {
  assert.equal(validState(state), true);
  for (const invalid of [null, {}, { ...state, active: 1 }, { ...state, pointerHeight: -1 }, { ...state, annotationColor: "transparent" }, { ...state, annotationCount: 501 }])
    assert.equal(validState(invalid), false);
});
