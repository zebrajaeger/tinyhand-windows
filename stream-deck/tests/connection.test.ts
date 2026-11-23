import test from "node:test";
import assert from "node:assert/strict";
import net from "node:net";
import { randomUUID } from "node:crypto";
import { Connection } from "../src/connection.js";
const state = { active: false, pointerHeight: 340, soundEnabled: false, annotationColor: "#E14164", annotationCount: 0, textPhase: "idle", capturePending: false };
test("real pipe transport snapshot revisions, correlation, queue bounds and no disconnect replay", async () => {
  const path = `\\\\.\\pipe\\TinyHand.NodeTest.${randomUUID()}`;
  let peer: net.Socket | undefined;
  let commandCount = 0;
  let commandSeen: (() => void) | undefined;
  const server = net.createServer(socket => {
    peer = socket;
    let buffer = "";
    socket.setEncoding("utf8");
    socket.on("data", data => {
      buffer += data;
      let n: number;
      while ((n = buffer.indexOf("\n")) >= 0) {
        const message = JSON.parse(buffer.slice(0, n)); buffer = buffer.slice(n + 1);
        if (message.type === "hello") socket.write(JSON.stringify({ type: "hello", version: 1, instanceId: "one", revision: 1, state }) + "\n");
        else { commandCount++; commandSeen?.(); }
      }
    });
  });
  await new Promise<void>(resolve => server.listen(path, resolve));
  const connection = new Connection(async () => path);
  const changed = () => new Promise<void>(resolve => connection.once("change", resolve));
  try {
    let ready = changed(); await connection.start(); await ready;
    assert.equal(connection.status, "connected");
    ready = changed(); peer!.write(JSON.stringify({ type: "state", instanceId: "one", revision: 3, state: { ...state, active: true } }) + "\n"); await ready;
    peer!.write(JSON.stringify({ type: "state", instanceId: "one", revision: 2, state }) + "\n");
    const observed = new Promise<void>(resolve => commandSeen = resolve);
    const first = connection.command("pointer.toggle");
    await observed;
    const queued = Array.from({ length: 16 }, () => connection.command("annotation.clear"));
    assert.equal((await connection.command("annotation.clear")).error, "busy");
    assert.equal(connection.state!.active, true);
    peer!.destroy();
    assert.equal((await first).error, "unknownOutcome");
    assert.ok((await Promise.all(queued)).every(r => r.error === "unavailable"));
    assert.equal(commandCount, 1);
    assert.equal(connection.status, "unavailable");
    assert.equal(connection.state, undefined);
  } finally {
    connection.stop(); peer?.destroy();
    await new Promise<void>(resolve => server.close(() => resolve()));
  }
});

test("reconnect replaces instance/revisions and never replays an uncertain toggle", async () => {
  const path = `\\\\.\\pipe\\TinyHand.NodeRestart.${randomUUID()}`;
  let generation = 0, commands = 0;
  const peers = new Set<net.Socket>();
  const server = net.createServer(socket => {
    peers.add(socket); socket.on("close", () => peers.delete(socket)); socket.setEncoding("utf8");
    const instance = String(++generation);
    let buffer = "";
    socket.on("data", data => {
      buffer += data; let n: number;
      while ((n = buffer.indexOf("\n")) >= 0) {
        const message = JSON.parse(buffer.slice(0, n)); buffer = buffer.slice(n + 1);
        if (message.type === "hello") socket.write(JSON.stringify({ type: "hello", version: 1, instanceId: instance, revision: 0, state: { ...state, active: generation > 1 } }) + "\n");
        else { commands++; socket.destroy(); }
      }
    });
  });
  await new Promise<void>(resolve => server.listen(path, resolve));
  const connection = new Connection(async () => path);
  const connected = () => new Promise<void>(resolve => {
    const listener = () => { if (connection.status === "connected") { connection.off("change", listener); resolve(); } };
    connection.on("change", listener);
  });
  try {
    const first = connected(); await connection.start(); await first;
    const again = connected();
    assert.equal((await connection.command("pointer.toggle")).error, "unknownOutcome");
    await again;
    assert.equal(connection.state!.active, true);
    assert.equal(commands, 1);
    assert.equal(generation, 2);
  } finally { connection.stop(); for (const peer of peers) peer.destroy(); await new Promise<void>(resolve => server.close(() => resolve())); }
});

test("pending command timeout invalidates connection instead of retrying", { timeout: 10000 }, async () => {
  const path = `\\\\.\\pipe\\TinyHand.NodeTimeout.${randomUUID()}`;
  let peer: net.Socket | undefined;
  let commands = 0;
  const server = net.createServer(socket => {
    peer = socket; socket.setEncoding("utf8");
    let buffer = "";
    socket.on("data", data => {
      buffer += data; let n: number;
      while ((n = buffer.indexOf("\n")) >= 0) {
        const message = JSON.parse(buffer.slice(0, n)); buffer = buffer.slice(n + 1);
        if (message.type === "hello") socket.write(JSON.stringify({ type: "hello", version: 1, instanceId: "timeout", revision: 0, state }) + "\n");
        else commands++;
      }
    });
  });
  await new Promise<void>(resolve => server.listen(path, resolve));
  const connection = new Connection(async () => path);
  try {
    const ready = new Promise<void>(resolve => connection.once("change", resolve)); await connection.start(); await ready;
    assert.equal((await connection.command("capture.monitor")).error, "unknownOutcome");
    assert.equal(commands, 1);
  } finally { connection.stop(); peer?.destroy(); await new Promise<void>(resolve => server.close(() => resolve())); }
});

test("incompatible handshake is distinct from pointer off", async () => {
  const path = `\\\\.\\pipe\\TinyHand.NodeVersion.${randomUUID()}`;
  let peer: net.Socket | undefined;
  const server = net.createServer(socket => { peer = socket; socket.once("data", () => socket.end('{"type":"incompatible","version":1}\n')); });
  await new Promise<void>(resolve => server.listen(path, resolve));
  const connection = new Connection(async () => path);
  try {
    const ready = new Promise<void>(resolve => connection.once("change", resolve)); await connection.start(); await ready;
    assert.equal(connection.status, "incompatible");
    assert.equal(connection.state, undefined);
    assert.equal((await connection.command("pointer.toggle")).error, "incompatible");
  } finally { connection.stop(); peer?.destroy(); await new Promise<void>(resolve => server.close(() => resolve())); }
});
