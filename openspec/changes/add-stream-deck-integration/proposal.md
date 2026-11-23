# Proposal

## Why

TinyHand's tray is awkward to reach while its active overlay intercepts clicks, and simulated hotkeys cannot keep Stream Deck button state synchronized. A Windows plugin with a local command/state connection gives presenters direct controls and truthful feedback.

## What Changes

- Add an optional Windows-only Elgato Stream Deck plugin with seven actions: pointer toggle, pointer size, sound toggle, annotation color, free-text placement, screenshot and clear all annotations.
- Size supports a small/medium/large cycle or a fixed preset; color is configured per key. Toggle keys reflect TinyHand's real state even when changed outside Stream Deck.
- Route tray, hotkey and external operations through the same controller commands on the WPF dispatcher; provide snapshots and change notifications.
- Add a versioned, bounded, current-user/current-session Windows named-pipe bridge, with no network listener, hotkey simulation, arbitrary execution or automatic TinyHand launch.
- Distinguish application unavailable, bridge incompatible, pending operation and command failure from ordinary pointer/sound off.
- Add reproducible plugin build, tests and a locally installable `.streamDeckPlugin` package. Keep Windows app publishing independent of optional plugin tooling.

## Capabilities

### New Capabilities
- `windows-local-control`: Local command validation, session isolation, state snapshots/events, bounded processing and failure semantics.
- `stream-deck-controls`: Configurable actions, state-driven key feedback, connection recovery and optional Windows plugin packaging.

### Modified Capabilities
- None. Existing pointer, preferences and audio contracts remain authoritative; annotation and screenshot additions come from prerequisite changes.

## Impact

- Depends on `add-windows-annotations` and `add-annotated-monitor-screenshots`, implemented and synced in that order. This proposal includes the complete first usable deck, not stub buttons for missing features.
- Expected areas: controller command/state seam, app startup/disposal, a named-pipe host, tests, and a new isolated `stream-deck/` TypeScript plugin package with its own lockfile and build instructions.
- Planned developer baseline follows current Elgato documentation: Node.js 24+ and Stream Deck 7.1+; end users use Stream Deck's plugin runtime, not a separately installed Node runtime. Verify exact manifest requirements during implementation.
- Update paired root/Windows documentation and add paired plugin setup instructions during implementation. No Marketplace submission, GitHub publication, code signing, plugin auto-install, release-workflow changes, macOS integration, dial support or press-and-hold pointer mode in this scope.
- A local pipe is a boundary against other users/sessions and remote clients, not protection against malicious software already running as the same user.
