# TinyHand Stream Deck plugin

**English** | [Deutsch](README.de.md) · [Windows guide](../windows/README.en.md)

Optional Windows-only plugin for Stream Deck **7.1+**, with seven keypad actions:
pointer toggle, size (cycle or fixed small/medium/large), tap sound toggle, per-key
RGB annotation color, free-text placement, monitor screenshot and clear all.
No dial, hold-to-point, automatic app launching or Marketplace submission is included.

## Build and install

Developers need Node.js **24+**, npm and Windows. From this directory:

```powershell
npm ci --ignore-scripts
npm run typecheck
npm test
npm run package
```

Output: `out/io.github.zebrajaeger.tinyhand.streamDeckPlugin`. Install that file
through Stream Deck's local plugin installer (typically double-click), then start
the separately built TinyHand Windows application. Installation is a user action;
build/package commands neither install nor publish the plugin. End users need no
separate Node installation: Stream Deck supplies its plugin runtime. The portable
TinyHand build remains independent of npm and Stream Deck tooling.

Drag actions from **TinyHand für Windows** onto keys. For size choose cycle or
a fixed preset; for color choose the desired RGB value in the property inspector.
Text starts interactive position-click/typing in TinyHand, not a stored text macro.
Screenshot copies the cursor monitor with completed annotations and without the
hand to the Windows clipboard; paste it into an image-capable app. Clear also
works while the pointer is off. Annotation color affects only future elements.

## Feedback and connection

Every key retains the tilted cactus application logo as its background; a small
action badge identifies its function. Labels and a selection marker show state
without replacing the branding with a solid green background.
Keys follow actual app snapshots, including tray/keyboard changes. `× Offline`
means unavailable, not pointer-off; `! Version` means incompatible protocol.
`! Einstellung` means invalid key configuration. An ellipsis indicates a pending
request; Stream Deck's alert indicates busy/failure/persistence warning. No command
is replayed after an uncertain disconnect. Reconnection refreshes state and does
not start or activate TinyHand. Stop/start TinyHand if an older app lacks protocol v1.

The bridge is a current-user/current-session Windows named pipe, not a TCP server.
It accepts a finite command list and never exposes annotation text or screenshot
pixels in state. Other-user/session access is rejected. This is **not** a boundary
against malicious processes already running as your user. App and plugin must run
in the same interactive Windows session; do not elevate one side independently.

## Dependencies and security

Dependencies and lockfile are pinned. The current Elgato SDK uses `zod/v4-mini`
through `@elgato/utils` but declares an older vulnerable Zod range. A scoped npm
override pins **Zod 4.6.7** (fix for GHSA-mrhx-c7xc-m3w5); SDK import/action
registration, build and package validation are regression-tested. Review/remove
this override when Elgato updates its own dependency. Do not use `npm audit fix
--force` to silently downgrade the SDK.

## Verification still requiring hardware

Automated tests cover command mapping, state feedback, real named-pipe transport,
queue bounds, disconnect/no-replay and .NET/Node interoperability. They do not prove
physical key feedback or installation in a running Stream Deck application.
Manually check every key, repeated equivalent keys, tray/hotkey changes, app/plugin
restart, unplug/reconnect, text/IME/focus, clipboard paste, mixed DPI and the headset's
first audible tap after 3–5 seconds idle. Do not claim these passed without testing.

Inherited artwork/concept: TinyHand by Kamran Ahmed/nilbuild. Repository MIT license
and third-party dependency licenses continue to apply. Bundled SDK licensing is
retained by the bundler's legal comments. The Windows EXE remains unsigned.
