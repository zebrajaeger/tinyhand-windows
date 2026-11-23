import streamDeck, { action, SingletonAction, type KeyDownEvent, type WillAppearEvent, type WillDisappearEvent, type DidReceiveSettingsEvent } from "@elgato/streamdeck";
import { Connection } from "./connection.js";
import { display, request, type Kind, type Settings } from "./controls.js";
import { pathToFileURL } from "node:url";
import { brandedIcon } from "./icons.js";
import { readFileSync } from "node:fs";
let brandImage = "";

export const connection = new Connection();
type Context = { action: WillAppearEvent<Settings>["action"]; settings: Settings; pending: boolean };
export class Control extends SingletonAction<Settings> {
  private contexts = new Map<string, Context>();
  constructor(private kind: Kind) {
    super();
    connection.on("change", () => { for (const context of this.contexts.values()) void this.render(context).catch(() => {}); });
  }
  override async onWillAppear(event: WillAppearEvent<Settings>): Promise<void> {
    const context = { action: event.action, settings: event.payload.settings, pending: false };
    this.contexts.set(event.action.id, context);
    await this.render(context);
  }
  override onWillDisappear(event: WillDisappearEvent<Settings>): void { this.contexts.delete(event.action.id); }
  override async onDidReceiveSettings(event: DidReceiveSettingsEvent<Settings>): Promise<void> {
    const context = this.contexts.get(event.action.id);
    if (context) { context.settings = event.payload.settings; await this.render(context); }
  }
  override async onKeyDown(event: KeyDownEvent<Settings>): Promise<void> {
    const context = this.contexts.get(event.action.id);
    if (!context || context.pending) return;
    const operation = request(this.kind, event.payload.settings);
    if (!operation) { await event.action.showAlert(); return; }
    context.pending = true;
    try {
      await this.render(context);
      const result = await connection.command(operation.command, operation.args);
      if (result.ok && !result.warning) await event.action.showOk();
      else await event.action.showAlert();
    } finally { context.pending = false; await this.render(context); }
  }
  private async render(context: Context): Promise<void> {
    if (!context.action.isKey()) return;
    const view = display(this.kind, context.settings, connection.status, connection.state);
    await context.action.setTitle(context.pending ? "…" : view.title);
    await context.action.setState(view.selected ? 1 : 0);
    await context.action.setImage(`data:image/svg+xml,${encodeURIComponent(brandedIcon(this.kind, brandImage, view.selected, view.color))}`);
  }
}
@action({ UUID: "io.github.zebrajaeger.tinyhand.pointer" }) class Pointer extends Control { constructor() { super("pointer"); } }
@action({ UUID: "io.github.zebrajaeger.tinyhand.size" }) class Size extends Control { constructor() { super("size"); } }
@action({ UUID: "io.github.zebrajaeger.tinyhand.sound" }) class Sound extends Control { constructor() { super("sound"); } }
@action({ UUID: "io.github.zebrajaeger.tinyhand.color" }) class Color extends Control { constructor() { super("color"); } }
@action({ UUID: "io.github.zebrajaeger.tinyhand.text" }) class Text extends Control { constructor() { super("text"); } }
@action({ UUID: "io.github.zebrajaeger.tinyhand.capture" }) class Capture extends Control { constructor() { super("capture"); } }
@action({ UUID: "io.github.zebrajaeger.tinyhand.clear" }) class Clear extends Control { constructor() { super("clear"); } }
if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) {
  brandImage = `data:image/png;base64,${readFileSync(new URL("../icon@2x.png", import.meta.url)).toString("base64")}`;
  for (const item of [new Pointer(), new Size(), new Sound(), new Color(), new Text(), new Capture(), new Clear()]) streamDeck.actions.registerAction(item);
  process.on("SIGTERM", () => connection.stop());
  process.on("SIGINT", () => connection.stop());
  await streamDeck.connect();
  void connection.start();
}
