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
Clearing SHALL remove completed strokes and cancel the current stroke. Turning the pointer off SHALL discard drawings, and reactivation SHALL begin with an empty drawing. Losing mouse capture during a stroke SHALL finalize it without an arrow. The application SHALL retain at most 500 strokes by dropping the oldest stroke when the limit is exceeded.

#### Scenario: Clear a current stroke
- **WHEN** the user clears drawings while a stroke is in progress
- **THEN** the current stroke is canceled and no retained stroke remains

#### Scenario: Reactivate the pointer
- **WHEN** the user turns pointer mode off and later on again
- **THEN** old drawings do not reappear

#### Scenario: Lose capture
- **WHEN** mouse capture is lost during drawing
- **THEN** the current stroke ends without an arrowhead

#### Scenario: Exceed drawing history
- **WHEN** beginning another stroke would retain more than 500 strokes
- **THEN** the oldest stroke is removed and at most 500 strokes remain
