# windows-monitor-screenshots Specification

## Purpose

Copy one annotated desktop monitor to the Windows clipboard without TinyHand pointer decorations, automatic files or changes to the presentation state.

## Requirements

### Requirement: Cursor-monitor capture
A screenshot SHALL capture the full monitor containing the Windows cursor at request acceptance, using physical pixel bounds and dimensions. Negative origins and mixed DPI SHALL NOT shift/crop the result. Unresolved or changed target topology SHALL cause failure, not monitor substitution. Only one capture SHALL run at a time; requests during capture, a pending stroke or text mode SHALL report busy without altering input.

#### Scenario: Capture a negative-origin monitor
- **WHEN** a capture is requested with the cursor on a secondary monitor left of the primary screen
- **THEN** the output covers precisely that secondary monitor at its physical pixel dimensions

#### Scenario: Capture while the pointer is off
- **WHEN** the cursor moves to another monitor in passive mode before capture is requested
- **THEN** capture selects its current monitor rather than the last active-pointer position

#### Scenario: Busy or removed monitor
- **WHEN** capture is requested during pending input or another capture, or the selected monitor disappears before acquisition completes
- **THEN** the request reports busy or an unavailable-monitor failure respectively, without committing drafts or substituting a screen

### Requirement: Annotated capture content
The image SHALL include the visible desktop background and a request-time snapshot of completed lines, arrowheads and text clipped to the selected monitor. It SHALL preserve their colors, placement and monitor-scaled appearance without rendering any annotation twice. It SHALL exclude the TinyHand hand/laser, tap animation and editor/placement UI. It SHALL NOT include unfinished annotations or capture-induced notifications.

#### Scenario: Capture active annotations
- **WHEN** the pointer is active over completed colored lines, arrows and text and a capture succeeds
- **THEN** the clipboard image includes those annotations once and contains no TinyHand pointer or tap decoration

#### Scenario: Clip cross-monitor content
- **WHEN** completed content extends across the selected monitor boundary
- **THEN** only the portion inside the selected monitor appears at the correct physical positions

### Requirement: Clipboard delivery and honest results
Capture SHALL publish a clipboard bitmap and report success only after publication. It SHALL NOT automatically write files, upload, open save dialogs or require elevation. Clipboard retries SHALL be bounded; failures SHALL return German feedback without deliberately clearing the prior clipboard first.

#### Scenario: Paste a capture
- **WHEN** capture reports success and the user pastes into an image-capable application
- **THEN** that application receives the selected monitor bitmap without a screenshot file having been created

#### Scenario: Clipboard is unavailable
- **WHEN** clipboard publication keeps failing through the bounded retry limit
- **THEN** capture reports failure rather than success, does not crash and does not deliberately clear the clipboard first

### Requirement: Capture privacy and limitations
Screenshot pixels and annotation text SHALL NOT be logged. Documentation SHALL disclose that secure desktops and some protected or exclusive-fullscreen content cannot be reliably captured and may appear blank; it SHALL NOT promise bypassing these restrictions.

#### Scenario: Assess protected-content capture
- **WHEN** a user reads capture documentation or evaluates a blank protected window
- **THEN** the limitation is explicit and successful bitmap delivery is not presented as proof of capturing protected pixels

### Requirement: Non-destructive capture lifecycle
Capture SHALL preserve pointer activation, completed annotations, selected settings, foreground focus and the existing audio keep-alive policy. Any temporary visual suppression SHALL be restored on success, failure, timeout or cancellation. A capture SHALL complete or report failure within five seconds, and normal presentation operation SHALL remain usable afterward. A German tray action SHALL make capture available without a Stream Deck.

#### Scenario: Capture fails after suppression
- **WHEN** acquisition fails or times out after temporary overlay suppression
- **THEN** presentation visuals and input routing are restored with annotations and pointer/audio state unchanged

#### Scenario: Use without hardware
- **WHEN** the user invokes the German screenshot tray command
- **THEN** it uses the same capture behavior and clipboard result as other callers without requiring a plugin
