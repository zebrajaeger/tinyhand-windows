# Proposal

## Why

TinyHand currently erases drawings when pointer mode ends and offers neither selectable ink colors nor text. A presentation workflow controlled from a Stream Deck needs persistent, click-through annotations and deliberate text input before hardware control is useful.

## What Changes

- **BREAKING**: Turning the pointer off, including Esc outside text editing, retains completed annotations instead of clearing them. Inactive annotation overlays pass mouse input through and show no hand, laser or tap animation.
- Separate annotation lifetime from pointer activation. Clear removes all annotations; application exit and monitor-layout changes also discard this session-only content.
- Add selectable opaque drawing colors, captured per stroke/text object; persist only the selected color.
- Add free text placement: arm text mode, click a position, type in a focused editor, Enter commits and Esc cancels. Return to the preceding pointer state afterward.
- Provide German tray entry points for color and text without requiring a Stream Deck, and shared controller operations for the later integration.
- Preserve release-time arrows, bounded history, physical desktop coordinates, safe inactive startup and the headset keep-alive contract.

## Capabilities

### New Capabilities
- `windows-annotation-text`: Explicit, focused free-text placement and bounded immutable committed text objects.

### Modified Capabilities
- `windows-desktop-drawing`: Retained annotations, per-element ink color and clearing of both lines and text.
- `windows-pointer-control`: Passive overlays, revised Esc behavior and the explicit text-input exception to typing passthrough.
- `windows-pointer-rendering`: Passive per-monitor presentation and layout-change cleanup in both active and inactive states.
- `windows-preferences`: Persist and validate selected annotation color without persisting annotation content.

## Impact

- Expected implementation areas: `windows/TinyHand.Windows/PointerController.cs`, `OverlayWindow.cs`, `Ink.cs`, `Settings.cs`, focused annotation model/editor components and the console test harness.
- Update `README.md`, `README.de.md`, `windows/README.md` and `windows/README.en.md` together during implementation. Update maintenance guidance for the new lifecycle and keyboard exception then, not in this planning change.
- No changes to original macOS sources, autostart registration, release workflow or audio buffer implementation.
- Foundation for `add-annotated-monitor-screenshots` and `add-stream-deck-integration`; implement and sync this change first.
- Deliberate first-version limits: no text editing/moving after commit, rich text, undo, persisted documents or custom fonts. Text uses a fixed readable default; only the ink color is selectable.
- Confirmed: annotations survive pointer-off; explicit clear removes them. Proposed first-version defaults are specified in the design for review, not represented as previously confirmed user choices.
