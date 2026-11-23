# windows-annotation-text Specification

## Purpose

Provide deliberate free-text annotations without leaking typed text into presentation applications or changing ordinary typing outside an explicit editor.

## Requirements

### Requirement: Free-text placement lifecycle
An explicit text action SHALL await a desktop left click, then open a focused single-line editor at that position without creating a stroke, tap or sound. Enter SHALL commit non-whitespace text; Esc SHALL cancel. Both SHALL restore the preceding pointer state unless superseded by a newer pointer command, preserving earlier annotations. Repeated text requests SHALL report busy without replacing the draft.

#### Scenario: Place text from passive mode
- **WHEN** text placement is invoked with the pointer off, a position is clicked and non-empty text is committed
- **THEN** text appears at that anchor and TinyHand returns to passive click-through display

#### Scenario: Cancel from active mode
- **WHEN** text placement was invoked while active and Esc is pressed before commit
- **THEN** the draft is discarded, earlier annotations remain and pointer mode resumes active

#### Scenario: Repeated text request
- **WHEN** another text action is invoked during pending placement or editing
- **THEN** it reports busy and preserves the original draft and anchor

### Requirement: Safe and bounded text input
The focused editor SHALL support Unicode, paste, deletion and IME without global typing interception. Input SHALL be single-line, at most 500 Unicode scalars, with pasted line breaks replaced by spaces. IME composition acceptance SHALL NOT commit the annotation. Whitespace-only submission SHALL create no element. Committed text SHALL have a fixed readable default appearance and SHALL NOT be editable or movable in this version.

#### Scenario: Unicode and composition
- **WHEN** the user enters accented characters or accepts IME composition
- **THEN** valid text is preserved and composition acceptance alone does not finalize the annotation

#### Scenario: Paste multiline or oversized text
- **WHEN** multiline or more-than-500-scalar content is pasted
- **THEN** the result stays single-line and within the limit without splitting a Unicode scalar

#### Scenario: Empty submission
- **WHEN** an empty or whitespace-only draft is submitted
- **THEN** no history entry is created and text mode ends

### Requirement: Text cancellation and focus safety
Focus loss, clear, explicit pointer-off, display-layout change and shutdown SHALL cancel a pending text draft. Leaving the editor SHALL return focus to the previous application only if that window still exists and focus has not deliberately moved elsewhere. Completed text SHALL share the document's color, clear, history and session-lifetime rules.

#### Scenario: Switch application while editing
- **WHEN** the user focuses another application during a text draft
- **THEN** the draft is cancelled without committing and TinyHand does not steal focus back

#### Scenario: Pointer off while editing
- **WHEN** an explicit pointer-off command arrives during text entry
- **THEN** the draft is cancelled, the pointer ends off and committed annotations remain
