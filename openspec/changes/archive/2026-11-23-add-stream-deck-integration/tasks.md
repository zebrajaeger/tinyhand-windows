# Tasks

## 1. Prerequisites and testable command seam

- [x] 1.1 Confirm annotation and screenshot automated prerequisites pass in order. The user explicitly permits dependent implementation before manual acceptance/spec synchronization; final acceptance still requires those checks. Run GitNexus upstream impact before editing each existing symbol and resolve unknown/high-risk findings according to repository guidance.
- [x] 1.2 Complete shared dispatcher-backed controller commands and authoritative state snapshots/events; ensure sound changes reuse atomic preference saves and `Active && SoundEnabled` keep-alive without previewing.
- [x] 1.3 Add protocol-independent tests for all allowlisted commands, invalid input/no mutation, preset cycling from arbitrary saved heights, idempotent setters, persistence warnings, text busy and actual screenshot completion results.

## 2. Local command/state host

- [x] 2.1 Implement version-1 UTF-8 newline-delimited framing, handshake, instance identity, request correlation, bounded duplicate-ID tracking, revisions, snapshots and typed error responses as defined in the design.
- [x] 2.2 Implement the current-user/current-session named-pipe host with ACL enforcement, remote rejection and client-session verification; integrate lifetime with normal startup/disposal but not audio-resource validation.
- [x] 2.3 Enforce client/frame/queue/handshake bounds, malformed/unknown/duplicate field rejection and dispatcher ordering without blocking the render loop; dispose stalled clients safely.
- [ ] 2.4 Add isolated-pipe integration tests for incompatible versions, malformed/oversized frames, duplicate IDs, client saturation, stalled output, state changes from tray/hotkeys/text/display events and disconnects before/after command mutation.
- [ ] 2.5 Verify ACL/session/remote denial with real Windows clients where available and record any isolation case not exercised; never treat pipe-name secrecy as authorization.

## 3. Optional Stream Deck plugin

- [x] 3.1 Create isolated `stream-deck/` TypeScript package using pinned official SDK/tooling and lockfile, Windows-only manifest, icons and proposed UUID `io.github.zebrajaeger.tinyhand`; validate Stream Deck 7.1+/bundled Node 24 compatibility against the current SDK.
- [x] 3.2 Implement one shared pipe connection manager with bounded retry/queueing, hello/full-state resynchronization, instance/revision handling and no automatic replay or app launching.
- [x] 3.3 Implement pointer and sound toggle actions plus authoritative on/off/unavailable/incompatible feedback; emit commands only once on key-down and bound pending presses.
- [x] 3.4 Implement size cycle/fixed-preset and per-key RGB color actions with validated property-inspector settings and actual-selection indicators.
- [x] 3.5 Implement free-text, screenshot and clear-all actions with correct busy/pending/success/error feedback and no fake completion of text or clipboard operations.
- [x] 3.6 Add plugin unit tests with mocked SDK/transport for all seven actions, multi-key synchronization, invalid configuration, queue overflow, timeouts, old revisions, process restart and reconnect without replay.
- [x] 3.7 Add a real Node-to-.NET isolated named-pipe smoke test covering handshake, command, state event and shutdown without user settings or real screenshots.

## 4. Packaging and documentation

- [x] 4.1 Add reproducible plugin build/test/package commands and ignored `.streamDeckPlugin` output; test local package manifest validation without changing the existing app publish script or release workflow.
- [x] 4.2 Add paired German/English plugin build, independent install, key setup, version support, troubleshooting and local trust-boundary instructions; update both root and both Windows README translations.
- [x] 4.3 Update lasting maintenance guidance for the new command/state seam, protocol and separate optional plugin tooling; preserve attribution/license and unsigned EXE warnings.

## 5. End-to-end acceptance

- [ ] 5.1 Run .NET 10 Release build, existing and new console/integration tests, plugin typecheck/tests/build/package validation, app publish/resource checks and `git diff --check`; verify app-only build works without Node or Stream Deck installed.
- [ ] 5.2 Install the local package on a supported Windows Stream Deck environment for acceptance only with user permission; verify all seven actions, multiple equivalent keys and state updates from tray, hotkeys and Esc.
- [ ] 5.3 Verify free-text placement/focus/typing/IME and screenshot clipboard paste on actual hardware; test busy/failed operations, app/plugin restart, device unplug/reconnect and no command replay or automatic activation.
- [ ] 5.4 Recheck sound toggles, separate audible regression and real headset first-click playback after 3-5 seconds idle; report hardware/listening checks separately from automated results.
- [ ] 5.5 Strictly validate the change and run complete GitNexus detect-changes before any separately authorized commit. Report unverified acceptance items explicitly; do not publish a release or Marketplace entry without a separate request.
