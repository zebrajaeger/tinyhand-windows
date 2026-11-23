# Tasks

## 1. Prerequisite and capture contract tests

- [ ] 1.1 Confirm `add-windows-annotations` is implemented, accepted and synced; run upstream GitNexus impact before modifying existing controller/renderer/native symbols and record risk.
- [ ] 1.2 Add injectable current-cursor/monitor topology, desktop bitmap acquisition and STA clipboard boundaries without using real clipboard content in automated tests.
- [ ] 1.3 Add tests for passive-mode fresh cursor selection, negative-origin/mixed-DPI bounds, physical output dimensions, removed monitors, concurrent capture and pending-stroke/text busy responses.

## 2. Capture and clipboard implementation

- [ ] 2.1 Add a reusable single-flight capture operation with frozen monitor/document snapshot, topology validation, five-second deadline and structured success/busy/failure results.
- [ ] 2.2 Implement temporary target-overlay visual suppression and compositor synchronization while preserving input interception/state; acquire the monitor background and restore visuals/resources on every completion/failure/cancellation path.
- [ ] 2.3 Composite committed annotation snapshot exactly once with the shared renderer and correct monitor origin/per-axis DPI; exclude pointer, tap, drafts, editor and feedback notifications.
- [ ] 2.4 Publish an immutable bitmap through STA clipboard access with at most three bounded attempts; avoid intentional prior clipboard clearing, automatic files, focus changes and sensitive logs.
- [ ] 2.5 Wire the German screenshot tray command to the shared operation, showing success only after clipboard publication and reporting busy/failure without throwing through the UI loop.

## 3. Failure and pixel regression coverage

- [ ] 3.1 Add synthetic-background pixel assertions for colored lines, arrows, text and clipping, checking no double painting or pointer decorations.
- [ ] 3.2 Inject capture/compositor/clipboard/topology exceptions, timeouts and shutdown cancellation; assert overlay restoration, native resource disposal, bounded retries and unchanged document/activation/audio policy.
- [ ] 3.3 Verify subsequent capture and normal pointer/text/clear operations remain usable after each failure; verify captured snapshot consistency across queued later commands.

## 4. Documentation and Windows acceptance

- [ ] 4.1 Update all four paired README guides for cursor-monitor selection, clipboard paste, busy input rules, no automatic files and secure/protected/fullscreen capture limitations.
- [ ] 4.2 Run .NET 10 Release build, console harness, publish/resource validation and `git diff --check`; validate this change strictly and perform full graph-change analysis before any separately authorized commit.
- [ ] 4.3 On real Windows, capture active/passive content on each negative-origin/mixed-DPI monitor and paste into an image editor; inspect dimensions, text/ink placement, no pointer/tap/ghost/doubled content, focus and presentation restoration. Report any unavailable hardware or protected-content checks honestly.
- [ ] 4.4 Accept and sync this capability before enabling the dependent Stream Deck screenshot action; do not install/publish a plugin as part of this change.
