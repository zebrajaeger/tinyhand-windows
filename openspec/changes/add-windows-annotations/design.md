# Design

## Context

See proposal.md for motivation. `PointerController.SetActive` currently clears strokes on both transitions and owns overlay creation/disposal. `OverlayWindow` is non-activating, draws every stroke with one fixed pen and always renders a hand or laser. `InkStroke` stores physical coordinates; arrow geometry is already tested. Settings writes are atomic. Tests are an STA console harness with isolated settings, not `dotnet test`.

This is a cross-cutting lifecycle, data-model and input change, so a design artifact is required. No implementation is authorized by these artifacts.

## Goals / Non-Goals

**Goals:** Separate document, interaction mode and pointer presentation; preserve screen alignment and normal application input in passive mode; share a deterministic committed-annotation renderer with screenshots.

**Non-Goals:** A general vector editor, persistent documents, rich text, individually selectable objects, global keyboard hooks or changes to macOS/audio buffering.

## Decisions

### 1. Independent document and interaction state

Use a session-only ordered annotation document of immutable completed strokes/text, plus a separate pending stroke/editor. Retain at most 500 completed elements total, evicting the oldest only on a successful commit. A stroke captures its color on mouse-down; a text draft captures color when placement is armed. Cancelled or empty drafts consume no history slot.

Modes are pointer-active, passive and text-placement/editing. Passive overlays exist only while completed content exists, use native hit-test transparency and do not activate or hide the normal cursor. Active overlays intercept clicks/scroll and show the pointer. Passive mode has no 16 ms pose loop or audio keep-alive. Do not equate window existence with `Active`.

Turning off cancels an unfinished stroke without deleting completed objects. Clear removes all content and drafts but preserves pointer activation. Reactivation retains existing annotations. Exiting discards the document. Display-layout changes cancel drafts and clear annotations in either mode, rebuild as necessary, and preserve the preceding pointer-active value. This intentionally retains the current layout-change reset policy rather than inventing coordinate migration.

### 2. Escape precedence is explicit

During placement/editing, Esc cancels only the draft and restores the preceding pointer mode. Outside text mode, Esc deactivates the pointer without clearing committed content. Ctrl+Alt+X and the tray clear action remain the explicit clear controls. Do not register an application-wide Esc hotkey in passive mode; ordinary applications must receive Esc there.

### 3. Focused text editor rather than global typing interception

`BeginTextPlacement` remembers the preceding pointer state and foreground window, suspends stroke input and uses the next left click as an anchor without tap sound/feedback. A normal focused WPF text control accepts Unicode, clipboard paste, Backspace and IME. Fixed default appearance: Segoe UI, 28 DIPs, selected opaque ink color, no background. Text remains single-line; Enter commits non-whitespace content, Esc cancels. Enforce a 500-Unicode-scalar limit; pasted line breaks become spaces. Enter used to accept an IME composition must not also commit the annotation.

After commit/cancel restore preceding pointer state and return focus only if the editor still owns it and the preceding window still exists. Focus loss cancels the draft without stealing focus back. A second text request while placement/editing is in progress reports busy, rather than nesting editors. Pointer-off, clear, shutdown and topology changes cancel the draft; an explicit pointer command's requested state takes precedence over the remembered state.

Committed text is not editable/movable in this version. Choosing that limit keeps text entry independent of object-selection UI; it is a proposed first-version constraint, not a previously confirmed user requirement.

### 4. Color and coordinate model

Store colors as validated opaque `#RRGGBB`, default `#E14164` (existing pen color). Tray offers a small named palette (existing pink, red, yellow, green, blue, white, black); external fixed-color actions may supply any valid RGB value. Existing annotations retain their captured colors. Persist selected color through `Settings`, normalize missing/invalid values to default and retain legacy JSON compatibility.

Anchors/points are physical desktop pixels, including negative values. Font sizes and pen widths are DIPs. Convert using each target monitor's X/Y DPI, preserving current 4-DIP pen and arrow-size behavior. The shared renderer accepts document snapshot, clip rectangle, origin and per-axis DPI; it excludes all pointer/editor decorations. Both screen and future bitmap rendering use it. Explicitly test an element spanning a DPI boundary rather than silently using the primary monitor scale.

### 5. Shared application operations

Controller operations cover activation, size, sound enablement, color, begin text and clear. Tray handlers call the operations rather than duplicating settings/audio logic. A read-only snapshot includes active mode, text phase, pointer height, sound enabled, color and annotation count. The later bridge consumes this seam; no pipe/plugin belongs in this change.

## Risks / Trade-offs

- Passive WPF windows can still intercept input if native flags are incomplete: verify actual underlying clicks, scrolling, focus and cursor, not just flags.
- Text focus is an intentional exception to the old typing contract: keep it explicit and test IME, cancellation and foreground restoration on real Windows.
- Escape no longer deletes: document the breaking behavior in all four guides and update old assertions/specs together.
- Fixed styling and immutable text simplify the first version but require clear-all/re-entry for corrections.
- Mixed-DPI rendering may expose existing conversion defects: new shared transforms must be pixel-tested with negative origins and manually checked across real monitors.

## Verification and rollout

Implement this change first and sync its deltas before dependent changes. Extend the existing harness for all mode transitions, retention/eviction, color serialization, draft cancellation, arrows and renderer geometry. Run .NET 10 Release build, console harness, publish resource check and `git diff --check`. Run the audible regression separately and distinguish it from a real headset listening check after 3-5 seconds idle. Manual acceptance includes passive click-through, Unicode/IME input, focus loss and mixed-DPI topology changes. No commit/push/release is implied.
