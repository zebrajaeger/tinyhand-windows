import type { State, Status } from "./connection.js";

export const kinds = ["pointer", "size", "sound", "color", "text", "capture", "clear"] as const;
export type Kind = typeof kinds[number];
export type Settings = { mode?: string; height?: number; color?: string };
export function request(kind: Kind, settings: Settings): { command: string; args: object } | undefined {
  switch (kind) {
    case "pointer": return { command: "pointer.toggle", args: {} };
    case "sound": return { command: "sound.toggle", args: {} };
    case "size":
      if (!settings.mode || settings.mode === "cycle") return { command: "pointer.cycleSize", args: {} };
      return settings.mode === "fixed" && [220, 340, 480].includes(settings.height ?? 0)
        ? { command: "pointer.setSize", args: { height: settings.height } } : undefined;
    case "color": return /^#[\da-f]{6}$/i.test(settings.color ?? "#E14164")
      ? { command: "annotation.setColor", args: { color: (settings.color ?? "#E14164").toUpperCase() } } : undefined;
    case "text": return { command: "annotation.beginText", args: {} };
    case "capture": return { command: "capture.monitor", args: {} };
    case "clear": return { command: "annotation.clear", args: {} };
  }
}
export function display(kind: Kind, settings: Settings, status: Status, state?: State): { title: string; selected: boolean; color?: string } {
  if (status !== "connected" || !state) return { title: status === "incompatible" ? "! Version" : "× Offline", selected: false };
  if (!request(kind, settings)) return { title: "! Einstellung", selected: false };
  switch (kind) {
    case "pointer": return { title: state.active ? "Zeiger AN" : "Zeiger AUS", selected: state.active };
    case "sound": return { title: state.soundEnabled ? "Ton AN" : "Ton AUS", selected: state.soundEnabled };
    case "size": return { title: ({ 220: "Klein", 340: "Mittel", 480: "Groß" } as Record<number, string>)[state.pointerHeight] ?? String(state.pointerHeight),
      selected: settings.mode === "fixed" && state.pointerHeight === settings.height };
    case "color": return { title: state.annotationColor === (settings.color ?? "#E14164").toUpperCase() ? "✓ Farbe" : "Farbe", selected: state.annotationColor === (settings.color ?? "#E14164").toUpperCase(), color: settings.color ?? "#E14164" };
    case "text": return { title: state.textPhase === "idle" ? "Text" : state.textPhase === "placing" ? "Position …" : "Eingabe …", selected: state.textPhase !== "idle" };
    case "capture": return { title: state.capturePending ? "Aufnahme …" : "Screenshot", selected: state.capturePending };
    case "clear": return { title: `Löschen\n${state.annotationCount}`, selected: false };
  }
}
