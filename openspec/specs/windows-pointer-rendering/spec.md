# windows-pointer-rendering Specification

## Purpose

Present a hand or laser pointer and desktop annotations across Windows monitors while preserving cursor alignment and monitor-local rendering.

## Requirements

### Requirement: Selectable pointer presentation
The application SHALL provide the embedded hand styles, a laser dot and three selectable pointer sizes. A selected hand SHALL tilt dynamically with smooth grip tracking while its measured fingertip remains aligned with the cursor. Laser mode SHALL display a dot instead of a hand.

#### Scenario: Move a hand pointer
- **WHEN** the user moves the cursor with a hand style active
- **THEN** the hand changes its tilt smoothly and its fingertip stays aligned with the cursor

#### Scenario: Choose a laser or size
- **WHEN** the user selects laser mode or another supported pointer size
- **THEN** the pointer uses the selected presentation without requiring an application restart

### Requirement: Distinct tray and executable imagery
The tray icon SHALL show the selected hand cropped at the wrist and SHALL update on hand selection. Laser mode SHALL use the lavender hand for the tray icon. The executable SHALL retain its fixed cookie-hand application icon independently of the selected tray style.

#### Scenario: Change hand style
- **WHEN** the user selects another hand style
- **THEN** the tray icon immediately reflects that hand without its long stick

#### Scenario: Select laser mode
- **WHEN** the user selects laser mode
- **THEN** the tray icon shows the lavender hand and the executable icon remains the cookie hand

### Requirement: Per-monitor desktop alignment
While active, the application SHALL provide an overlay for each monitor. Pointer and drawing positions SHALL remain aligned to physical desktop locations, including negative monitor coordinates, by converting to each overlay's local rendering coordinates. An active display-layout change SHALL recreate the overlays and clear the drawing while preserving active pointer mode.

#### Scenario: Draw on a secondary monitor
- **WHEN** the user points or draws on a monitor with negative desktop coordinates or a different DPI scaling
- **THEN** the visible pointer and strokes align with the corresponding desktop positions within that monitor's overlay

#### Scenario: Change monitor layout
- **WHEN** Windows reports a display-layout change while the pointer is active
- **THEN** overlays are recreated for the current monitors, existing drawings are cleared and pointer mode remains active

### Requirement: Overlay visibility boundaries
The documented Windows contract SHALL disclose that secure desktops are not covered, exclusive fullscreen or protected applications may hide the overlay, and application-window-only sharing normally excludes the separate overlay. It SHALL NOT promise universal overlay visibility or an independently extended arm or stick segment.

#### Scenario: Plan screen sharing
- **WHEN** the user consults the Windows usage documentation before screen sharing
- **THEN** it recommends sharing the entire display and describes the overlay visibility limitations
