# Spec Delta

## MODIFIED Requirements

### Requirement: Drawing lifecycle and bounded history
Clearing SHALL remove completed strokes and text annotations and cancel any current stroke or text draft without changing pointer activation. Turning the pointer off SHALL cancel unfinished input but retain completed annotations visibly in passive mode; reactivation SHALL retain that content. Losing mouse capture during an ordinary stroke SHALL finalize it without an arrow. Completed strokes and text SHALL share a maximum history of 500 elements, dropping the oldest only when committing beyond that limit. Application exit and display-layout changes SHALL discard all annotations and drafts; annotation content SHALL NOT be persisted across launches.

#### Scenario: Clear a current stroke
- **WHEN** clear is invoked during a stroke or text draft
- **THEN** all completed content and pending input are removed and pointer activation remains unchanged

#### Scenario: Reactivate the pointer
- **WHEN** the pointer is turned off with completed lines and text and later turned on
- **THEN** that content remains visible throughout and is not reset by either transition

#### Scenario: Cancel unfinished drawing on deactivation
- **WHEN** the pointer is turned off during a stroke
- **THEN** that unfinished stroke is discarded and earlier completed content remains

#### Scenario: Lose capture
- **WHEN** mouse capture is lost during an ordinary drawing stroke without a deactivation or clear request
- **THEN** the stroke finishes without an arrowhead

#### Scenario: Exceed drawing history
- **WHEN** committing a stroke or text would exceed 500 completed elements
- **THEN** the oldest completed element is removed and at most 500 remain

#### Scenario: Start a new session
- **WHEN** TinyHand exits and is launched again
- **THEN** no previous annotation content is restored

## ADDED Requirements

### Requirement: Per-annotation drawing color
Users SHALL be able to select an opaque RGB annotation color from German tray controls without activating the pointer. New strokes SHALL retain the color selected at stroke start; text SHALL retain the color selected when its placement starts. Later color changes SHALL NOT recolor existing or pending elements. The default SHALL match the existing ink color `#E14164`.

#### Scenario: Change color between elements
- **WHEN** the user draws a line, selects another color and creates another line or text
- **THEN** earlier content retains its color and the new element uses the new selection

#### Scenario: Change color during drawing
- **WHEN** the selected color changes while a stroke or text placement is pending
- **THEN** the pending element keeps its original color and the next element uses the new color
