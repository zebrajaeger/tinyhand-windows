# windows-pointer-control Specification

## Purpose

Provide safe, explicit control of the Windows presentation pointer without activating it on launch or preventing ordinary keyboard use.

## Requirements

### Requirement: Inactive single-instance startup
The application SHALL start with the pointer inactive, including when launched at sign-in or with restored preferences. A second instance in the same Windows session SHALL report that TinyHand is already running and exit without opening another pointer controller.

#### Scenario: Start with saved preferences
- **WHEN** the user launches TinyHand with saved hand, size and enabled sound preferences
- **THEN** the tray icon is available and no pointer overlay is activated

#### Scenario: Launch another instance
- **WHEN** TinyHand is already running in the same Windows session and another instance is launched
- **THEN** the second instance reports the existing instance and exits

### Requirement: Explicit pointer controls
The application SHALL let the user toggle the pointer with Ctrl+Alt+H, a left-click on the tray icon or the tray toggle action. Esc while active and outside text placement/editing SHALL deactivate the pointer without clearing completed annotations. In text placement/editing, Esc SHALL cancel that pending text and restore the preceding pointer state without deleting completed annotations. Ctrl+Alt+X and the tray clear action SHALL clear annotations without changing pointer activation. Unavailable registered toggle or clear hotkeys SHALL produce a tray warning while leaving tray controls available. Passive annotation display SHALL NOT reserve plain Esc from other applications.

#### Scenario: Toggle on and off
- **WHEN** an available toggle control is invoked
- **THEN** pointer activation changes and completed annotations remain

#### Scenario: Exit pointer mode
- **WHEN** Esc is pressed while the pointer is active outside text mode
- **THEN** the pointer disappears, completed annotations remain and normal desktop mouse interaction resumes

#### Scenario: Clear without exiting
- **WHEN** Ctrl+Alt+X is invoked while the pointer is active
- **THEN** all annotations are removed and the pointer stays active

#### Scenario: Hotkey conflict
- **WHEN** toggle or clear hotkey registration fails
- **THEN** a tray warning is shown and tray controls remain available

#### Scenario: Escape in another application
- **WHEN** annotations are displayed passively and the user presses Esc in another application
- **THEN** TinyHand does not consume that key

### Requirement: Active input interception
Active pointer overlays SHALL consume covered desktop clicks and scrolling instead of forwarding them to underlying applications. Outside explicit text placement/editing, TinyHand SHALL NOT intercept ordinary typing. Passive annotation overlays SHALL pass mouse clicks and scrolling to underlying applications, SHALL NOT take keyboard focus and SHALL show the normal system cursor rather than the TinyHand pointer. During explicit text placement/editing the placement click and editor typing SHALL NOT reach the previously focused application. Leaving that mode SHALL restore normal input routing.

#### Scenario: Interact over an application
- **WHEN** the user clicks or scrolls while pointer mode is active
- **THEN** the underlying application does not receive those mouse interactions

#### Scenario: Type while pointing
- **WHEN** ordinary text is typed in pointer mode outside text placement/editing
- **THEN** TinyHand does not consume that typing

#### Scenario: Resume desktop use
- **WHEN** pointer mode is disabled with annotations retained
- **THEN** clicks and scrolling reach underlying applications through those annotations without focus being stolen

#### Scenario: Explicit text entry
- **WHEN** a text-placement click is followed by typing in the TinyHand editor
- **THEN** neither the placement click nor that editor input changes the previously focused application's content

### Requirement: German tray controls
The German tray menu SHALL expose existing pointer, size, sound, startup and exit controls together with annotation color, free-text placement and an explicit clear-all-annotations action. Color and text entry points SHALL be available without a Stream Deck.

#### Scenario: Open the tray menu
- **WHEN** the tray menu is opened with pointer mode off
- **THEN** it offers the supported controls including color, text placement and clearing all annotations
