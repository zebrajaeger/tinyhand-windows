# Design

## Context

See proposal.md for scope. TinyHand is an STA WPF/WinForms tray process with a session-local single-instance mutex. Its controller has activation/size/style/clear methods, but sound changes currently live inside menu callbacks and there is no external transport. Prerequisite changes introduce shared commands, snapshots, annotations/text and a screenshot service.

This design is mandatory because the change introduces IPC, a TypeScript plugin and security/lifecycle boundaries. Inspection of the current official Elgato getting-started documentation on 2026-11-23 reports Node.js 24+ for development and Stream Deck 7.1+. Source: https://docs.elgato.com/streamdeck/sdk/introduction/getting-started/ . Pin supported SDK/tool versions and lock dependencies when implementing; no packages or tooling are installed by this planning change.

## Goals / Non-Goals

**Goals:** Real application state on every key, exactly one dispatch per accepted key action, reconnect without replay, one shared local connection and optional deployment independent of the portable app.

**Non-Goals:** Remote control across computers/users, a general automation engine, Marketplace/release publication, background application launching, dials, hold-to-point, text presets or global keyboard injection.

## Decisions

### 1. Windows named pipe, not an unauthenticated localhost port

Use a duplex byte-mode named pipe with a deterministic name containing a SHA-256-derived current-user SID identifier and Windows session ID: `TinyHand.Windows.Control.v1.<user-hash>.<session>`. Restrict access to the current user, reject remote clients, and verify connecting client's session before accepting commands. Mirror the session-local mutex boundary. A Node plugin uses Windows pipe support through `node:net`; the app remains self-contained .NET without a new server runtime or firewall rule. Validate ACL and cross-session/remote restrictions on real Windows rather than assuming a name is authorization.

The host starts with the existing controller and stops on disposal; no listener is added to the audio-resource-check execution path. At most four clients, one in-flight request per client, 16 KiB UTF-8 frames, 32 KiB outbound queue per client, and a one-second handshake deadline. Disconnect stalled/malformed/overflowing clients without blocking rendering. Do not log annotation text or clipboard content. Same-user malicious processes are outside the trust boundary; requests can only invoke the finite allowlist below.

### 2. Protocol v1 and dispatcher ordering

Newline-delimited UTF-8 JSON with protocol version `1`. Client first sends `{type:"hello", version:1}`; server replies `hello` with version, random per-process `instanceId`, capabilities and full state. Unsupported versions receive `incompatible` then disconnect. Reject duplicate JSON keys, oversized frames, missing/extra command fields and invalid types. Each request uses a connection-unique string `id` (maximum 64 ASCII characters); duplicate IDs are rejected, not executed again. Retain at most 256 IDs per connection, then request reconnect before accepting more; reconnect never replays requests.

Requests are `{type:"command", id, command, arguments}` or `{type:"getState", id}`. Replies are `{type:"result", id, ok, error?, state}`; state notifications are `{type:"state", instanceId, revision, state}`. Revisions monotonically increase per process for controller-state mutations, and every response includes the resulting current state. Client ignores stale revisions only within the same instance and resets on new instanceId. Snapshots contain `active`, `pointerHeight`, `soundEnabled`, `annotationColor`, `textPhase` (`idle|placing|editing`), `annotationCount` and `capturePending`; they never contain text or pixels.

All validation precedes mutation. Dispatcher serializes state-changing commands with tray/hotkey operations; do not mutate WPF objects on pipe threads. Get-state and cheap setters finish promptly; screenshot completion reports the actual clipboard result, not just enqueue success. Text-start success means placement is armed, not text committed. Preserve snapshot/events during later text completion, Esc, focus loss and display changes. `busy`, `invalidArgument`, `unsupportedCommand`, `incompatible`, `timeout` and `operationFailed` are distinct errors. Lost connections never trigger retries of possibly applied commands. A client timeout is an unknown outcome, not proof of failure; reconnect and refresh state.

Command allowlist:

| Command | Arguments | Behavior |
| --- | --- | --- |
| `pointer.toggle` | `{}` | Toggle once on dispatcher. |
| `pointer.setActive` | `{active:boolean}` | Idempotent explicit activation. |
| `pointer.setSize` | `{height:220|340|480}` | Set a fixed supported preset. |
| `pointer.cycleSize` | `{}` | Advance 220 -> 340 -> 480 -> 220; a legacy non-preset advances to the next larger preset or wraps. |
| `sound.toggle` | `{}` | Toggle sound enablement without preview. |
| `sound.setEnabled` | `{enabled:boolean}` | Idempotent enablement, keep selected sound. |
| `annotation.setColor` | `{color:"#RRGGBB"}` | Validate/normalize opaque RGB; future elements only. |
| `annotation.beginText` | `{}` | Arm free-text placement using prerequisite lifecycle. |
| `annotation.clear` | `{}` | Clear all committed/pending annotations; preserve activation. |
| `capture.monitor` | `{}` | Copy cursor-monitor bitmap through prerequisite service. |

No arbitrary filenames, process execution, startup-registry writes, application exit, sound-resource paths, serialized text injection or screen-coordinate commands are exposed. A malformed request causes no state change; settings save failure retains session state with a warning and reports a persistence warning rather than pretending settings were saved.

### 3. One plugin process connection, multiple independent key contexts

Place the official `@elgato/streamdeck` TypeScript plugin under `stream-deck/`, isolated from .NET builds. Use a connection manager shared by all action instances. Connect on plugin startup, retry with bounded exponential delay (0.5 to 5 seconds), and on reconnect obtain hello/full state before enabling actions. Mark all contexts unavailable immediately on disconnect; never display cached activation as authoritative. Do not automatically start TinyHand or activate the pointer. Device disconnect or plugin exit only closes its connection; it does not change TinyHand's state.

Seven key actions use `keyDown`, not both keyDown/keyUp:

- **Zeiger ein/aus**: `pointer.toggle`; on/off from snapshot.
- **Zeigergröße**: inspector choice `cycle` (default) or `fixed` with Klein/Mittel/Groß (220/340/480). Show actual height/preset; fixed keys indicate whether selected.
- **Ton ein/aus**: `sound.toggle`; show actual enabled flag, never infer from audio device activity.
- **Zeichenfarbe**: inspector RGB picker, default `#E14164`; show configured swatch and selected indication based on actual application color.
- **Text platzieren**: `annotation.beginText`; indicate idle/placement/editing and reject repeated busy starts.
- **Screenshot**: `capture.monitor`; pending until actual result, short success/error feedback without modifying active/color state.
- **Alles löschen**: `annotation.clear`; disabled/unavailable only by connection/pending safety rules, not because pointer is off.

Render title/icon states from snapshots, not optimistic toggles. Keep off, unavailable and incompatible visually distinct and use labels/symbols in addition to color. Disable repeated key presses while that key awaits a result; serialize plugin commands through one queue (maximum 16 queued, overflow rejected with feedback). Skip commands that have not been sent if the connection drops. The property inspector validates configuration locally; the app validates again. Store per-key configuration in Stream Deck settings, never annotation content or application activation. No full desktop web UI or separately operated service.

### 4. Distribution and verification boundary

Use repository-owned reverse-DNS UUID `io.github.zebrajaeger.tinyhand` as the proposed stable plugin ID; validate uniqueness/manifest during local packaging before any eventual publication. Declare Windows-only and Stream Deck 7.1+, with the matching bundled Node 24 runtime requirements per SDK manifest. Pin packages and include a lockfile; use a reproducible build/test/package command producing a locally installable `.streamDeckPlugin` in an ignored output directory. .NET app users need neither Stream Deck nor Node; plugin developers need Node but plugin users do not install it separately.

Keep `windows/publish.ps1` and existing GitHub release workflow unchanged. Provide separate plugin build/install instructions and manual acceptance, not Marketplace publication or auto-installation. Future binary distribution is a separate authorized release task.

## Risks / Trade-offs

- Named-pipe ACL/session verification may need targeted Windows interop; test with real other-session/remote clients where available and honestly record any unverified isolation case.
- A transport disconnect after mutation leaves an ambiguous result. No automatic replay, fresh state and explicit user retry are safer than double toggling or double screenshots.
- External state updates can race local presses; dispatcher ordering and monotonically revised snapshots remove blind read-modify-write behavior.
- Stream Deck focus changes can cause the foundation text editor to cancel. Verify on physical hardware with real typing, not just SDK event mocks.
- Developer/runtime versions evolve; exact manifest/tool compatibility must be verified by a local package install, not inferred from successful TypeScript compilation.

## Verification and rollout

### Subsequent user-approved presentation decisions

The fixed Windows icon, Stream Deck category and pointer action now use the tilted cactus artwork from `.github/logo-windows.png`. All keys and sidebar actions share that background with function badges, labels and selection markers instead of solid green fills. The dynamically selected on-screen hand and style-dependent tray icon remain unchanged. `windows/make-app-icon.ps1` generates multi-resolution app/brand images from the same source. The current local plugin build is 0.1.0.5; installation was confirmed through the live TinyHand-specific Stream Deck dialog without removing profiles.

Manual/hardware acceptance remains pending. The user explicitly approved dependent implementation before final acceptance/synchronization, and later approved archiving with unfinished tasks retained. Do not read archive placement as evidence of completed isolation, device, IME, DPI, screenshot-paste or listening tests.

Apply/sync annotations, then screenshots, then this change. Use isolated pipe names and settings in tests. Add real .NET/Node interoperability smoke coverage, frame/ACL/error tests and plugin tests with a mocked SDK/transport. On actual Stream Deck verify every action, multiple copies of a key, state updates from tray/Esc/hotkeys, app/plugin restart, unplug/reconnect, busy operations, failed screenshots and text focus. Recheck the audio first-click-after-idle on the real headset after sound toggles. No publication or installation is performed as part of proposal authoring.
