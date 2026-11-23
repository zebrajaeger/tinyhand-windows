# Proposal

## Why

Presenters need to copy the screen they annotated without including the large TinyHand pointer. A single monitor-to-clipboard action provides a predictable capture workflow that can later be invoked from the Stream Deck.

## What Changes

- Capture the complete monitor containing the Windows cursor when the request is accepted, at that monitor's physical pixel dimensions.
- Include committed lines, arrowheads and text, clipped to that monitor, but exclude the TinyHand hand/laser, tap feedback, unfinished stroke and text-placement/editor UI.
- Put a bitmap on the Windows clipboard without starting a file-save dialog or writing an automatic screenshot file.
- Preserve activation, annotations, focus and audio state; report clipboard/capture failures in German without claiming success.
- Add a German tray command and a shared capture operation for the later Stream Deck action.

## Capabilities

### New Capabilities
- `windows-monitor-screenshots`: Cursor-monitor annotated capture, pointer exclusion, clipboard delivery and failure-safe capture lifecycle.

### Modified Capabilities
- None. This change consumes the annotation model and passive overlay lifecycle defined by `add-windows-annotations` without redefining those requirements.

## Impact

- Depends on `add-windows-annotations` being implemented and synced first. Expose one reusable capture service rather than capture logic inside tray or plugin handlers.
- Expected implementation areas: shared annotation rendering, a monitor-capture abstraction, clipboard adapter, controller/tray wiring, native interop only where required, and `windows/TinyHand.Windows.Tests`.
- Update all four paired root/Windows README files with clipboard usage and protected-content limitations during implementation.
- No external screenshot utility, administrator rights, additional runtime install, disk history, network upload, all-monitor stitching or interactive region selection.
- Hardware validation must include mixed DPI, negative monitor origins, actual clipboard paste and compositor suppression/restoration. Automated pixel tests alone do not prove protected content is capturable.
