import net from "node:net";
import { execFile } from "node:child_process";
import { createHash } from "node:crypto";
import { EventEmitter } from "node:events";

export type State = { active: boolean; pointerHeight: number; soundEnabled: boolean; annotationColor: string;
  annotationCount: number; textPhase: "idle" | "placing" | "editing"; capturePending: boolean };
export type Result = { ok: boolean; error?: string; warning?: string };
export type Status = "unavailable" | "incompatible" | "connected";

export async function resolvePipe(): Promise<string> {
  // Read only our process token/session. No user arguments are interpolated into PowerShell.
  const identity = await new Promise<string>((resolve, reject) => execFile("powershell.exe", ["-NoProfile", "-NonInteractive", "-Command",
    "[System.Security.Principal.WindowsIdentity]::GetCurrent().User.Value; [System.Diagnostics.Process]::GetCurrentProcess().SessionId"],
  { windowsHide: true, timeout: 5000 }, (error, stdout) => error ? reject(error) : resolve(stdout)));
  const [sid, session] = identity.trim().split(/\r?\n/).map(s => s.trim());
  if (!/^S-\d+(?:-\d+)+$/.test(sid) || !/^\d+$/.test(session)) throw new Error("identityUnavailable");
  const hash = createHash("sha256").update(sid).digest("hex");
  return `\\\\.\\pipe\\TinyHand.Windows.Control.v1.${hash}.${session}`;
}

export class Connection extends EventEmitter {
  state?: State;
  status: Status = "unavailable";
  private socket?: net.Socket;
  private instance?: string;
  private revision = -1;
  private request = 0;
  private backoff = 500;
  private retry?: NodeJS.Timeout;
  private handshake?: NodeJS.Timeout;
  private stopped = false;
  private buffer = "";
  private queue: { command: string; args: object; resolve: (result: Result) => void }[] = [];
  private current?: { id: string; resolve: (result: Result) => void; timer: NodeJS.Timeout };

  constructor(private pipe: () => Promise<string> = resolvePipe, private connect = net.createConnection) { super(); }
  async start(): Promise<void> {
    if (this.stopped || this.socket) return;
    try {
      const path = await this.pipe();
      if (this.stopped) return;
      const socket = this.socket = this.connect(path);
      this.handshake = setTimeout(() => socket.destroy(), 1000);
      socket.setEncoding("utf8");
      socket.on("connect", () => socket.write(JSON.stringify({ type: "hello", version: 1 }) + "\n"));
      socket.on("data", (data: string) => this.receive(data));
      socket.on("error", () => socket.destroy());
      socket.on("close", () => { if (this.socket === socket) this.disconnect(); });
    } catch { this.disconnect(); }
  }

  command(command: string, args: object = {}): Promise<Result> {
    if (this.status !== "connected") return Promise.resolve({ ok: false, error: this.status });
    if (this.queue.length >= 16) return Promise.resolve({ ok: false, error: "busy" });
    return new Promise(resolve => { this.queue.push({ command, args, resolve }); this.flush(); });
  }

  private flush(): void {
    if (this.current || this.status !== "connected") return;
    if (this.request >= 250) { this.socket?.destroy(); return; }
    const item = this.queue.shift();
    if (!item) return;
    const id = String(++this.request);
    this.current = { id, resolve: item.resolve, timer: setTimeout(() => {
      this.current?.resolve({ ok: false, error: "unknownOutcome" });
      this.current = undefined;
      this.socket?.destroy();
    }, 6500) };
    this.socket!.write(JSON.stringify({ type: "command", id, command: item.command, arguments: item.args }) + "\n");
  }

  private receive(data: string): void {
    this.buffer += data;
    if (Buffer.byteLength(this.buffer) > 32768) { this.socket?.destroy(); return; }
    let newline: number;
    while ((newline = this.buffer.indexOf("\n")) >= 0) {
      const line = this.buffer.slice(0, newline); this.buffer = this.buffer.slice(newline + 1);
      try {
        const message = JSON.parse(line);
        if (message.type === "incompatible") { this.status = "incompatible"; this.emit("change"); this.socket?.destroy(); return; }
        if (message.type === "hello") {
          if (message.version !== 1 || typeof message.instanceId !== "string") throw new Error("incompatible");
          clearTimeout(this.handshake);
          this.instance = message.instanceId; this.revision = -1; this.request = 0;
          this.status = "connected"; this.backoff = 500;
        }
        if (message.instanceId === this.instance && Number.isSafeInteger(message.revision) && message.revision >= this.revision && validState(message.state)) {
          this.state = message.state; this.revision = message.revision; this.emit("change");
        } else if (message.type === "hello") throw new Error("invalidSnapshot");
        if (message.type !== "hello" && message.instanceId !== this.instance) continue;
        if (message.type === "result" && this.current && this.current.id === message.id) {
          clearTimeout(this.current.timer);
          this.current.resolve({ ok: message.ok === true, error: message.error, warning: message.warning });
          this.current = undefined;
        }
        this.flush();
      } catch { this.socket?.destroy(); return; }
    }
  }

  private disconnect(): void {
    clearTimeout(this.handshake);
    this.socket = undefined; this.buffer = ""; this.state = undefined;
    if (this.status !== "incompatible") this.status = "unavailable";
    if (this.current) { clearTimeout(this.current.timer); this.current.resolve({ ok: false, error: "unknownOutcome" }); this.current = undefined; }
    for (const queued of this.queue.splice(0)) queued.resolve({ ok: false, error: "unavailable" });
    this.emit("change");
    if (!this.stopped) { this.retry = setTimeout(() => { void this.start(); }, this.backoff); this.backoff = Math.min(5000, this.backoff * 2); }
  }

  stop(): void { this.stopped = true; clearTimeout(this.retry); const socket = this.socket; this.socket = undefined; socket?.destroy(); this.disconnect(); }
}

export function validState(value: unknown): value is State {
  if (!value || typeof value !== "object") return false;
  const s = value as State;
  return typeof s.active === "boolean" && Number.isInteger(s.pointerHeight) && s.pointerHeight >= 160 && s.pointerHeight <= 640
    && typeof s.soundEnabled === "boolean" && /^#[\da-f]{6}$/i.test(s.annotationColor)
    && Number.isInteger(s.annotationCount) && s.annotationCount >= 0 && s.annotationCount <= 500
    && ["idle", "placing", "editing"].includes(s.textPhase) && typeof s.capturePending === "boolean";
}
