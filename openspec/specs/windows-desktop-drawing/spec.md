# windows-desktop-drawing Specification

## Purpose

Allow desktop annotations during pointer mode, with release-time arrow selection and predictable clearing and cancellation behavior.

## Requirements

### Requirement: Click feedback and stroke drawing
An active left mouse press SHALL display a transient animated tap indicator and begin a stroke at the desktop click position. Dragging SHALL extend that stroke and releasing SHALL finish it. A click without a usable line segment SHALL NOT produce a spurious line or arrowhead.

#### Scenario: Draw a line
- **WHEN** the user presses the left mouse button, drags and releases while pointer mode is active
- **THEN** a tap indicator appears and a completed stroke follows the drag path

#### Scenario: Click without dragging
- **WHEN** the user clicks without producing a usable line segment
- **THEN** tap feedback appears without a spurious line or arrowhead

### Requirement: Release-time arrow selection
A completed stroke SHALL have an arrowhead if and only if Shift is held at normal left-button release and the stroke has a usable direction. Holding Shift only at press SHALL NOT select an arrow. The arrowhead SHALL point along the final usable stroke direction, including vertical and reverse-direction strokes.

#### Scenario: Add Shift during a drag
- **WHEN** the user starts dragging without Shift and holds Shift at release
- **THEN** the completed usable stroke has an arrowhead

#### Scenario: Release Shift before the mouse
- **WHEN** the user starts dragging with Shift but releases Shift before the left mouse button
- **THEN** the completed stroke has no arrowhead

#### Scenario: Draw in another direction
- **WHEN** the user finishes a usable vertical or reverse-direction stroke with Shift held
- **THEN** the arrowhead follows the terminal stroke direction

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

### Requirement: Per-annotation drawing color
Users SHALL be able to select an opaque RGB annotation color from German tray controls without activating the pointer. New strokes SHALL retain the color selected at stroke start; text SHALL retain the color selected when its placement starts. Later color changes SHALL NOT recolor existing or pending elements. The default SHALL match the existing ink color `#E14164`.

#### Scenario: Change color between elements
- **WHEN** the user draws a line, selects another color and creates another line or text
- **THEN** earlier content retains its color and the new element uses the new selection

#### Scenario: Change color during drawing
- **WHEN** the selected color changes while a stroke or text placement is pending
- **THEN** the pending element keeps its original color and the next element uses the new color
