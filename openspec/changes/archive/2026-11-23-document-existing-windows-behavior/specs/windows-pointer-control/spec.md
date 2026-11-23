# Spec Delta

## Purpose

Provide safe, explicit control of the Windows presentation pointer without activating it on launch or preventing ordinary keyboard use.

## ADDED Requirements

### Requirement: Inactive single-instance startup
The application SHALL start with the pointer inactive, including when launched at sign-in or with restored preferences. A second instance in the same Windows session SHALL report that TinyHand is already running and exit without opening another pointer controller.

#### Scenario: Start with saved preferences
- **WHEN** the user launches TinyHand with saved hand, size and enabled sound preferences
- **THEN** the tray icon is available and no pointer overlay is activated

#### Scenario: Launch another instance
- **WHEN** TinyHand is already running in the same Windows session and another instance is launched
- **THEN** the second instance reports the existing instance and exits

### Requirement: Explicit pointer controls
The application SHALL let the user toggle the pointer with Ctrl+Alt+H, a left-click on the tray icon or the tray toggle action. Esc while active SHALL clear drawings and deactivate the pointer. Ctrl+Alt+X and the tray clear action SHALL clear drawings without deactivating an active pointer. Unavailable registered toggle or clear hotkeys SHALL produce a tray warning while leaving tray controls available.

#### Scenario: Toggle on and off
- **WHEN** the user invokes an available toggle control
- **THEN** an inactive pointer becomes active or an active pointer becomes inactive

#### Scenario: Exit pointer mode
- **WHEN** the user presses Esc while the pointer is active
- **THEN** the drawings are cleared and the overlays are closed

#### Scenario: Clear without exiting
- **WHEN** the user invokes Ctrl+Alt+X while the pointer is active
- **THEN** existing drawing strokes are removed and the pointer remains active

#### Scenario: Hotkey conflict
- **WHEN** registration of the toggle or clear hotkey fails
- **THEN** a tray warning informs the user and tray controls remain available

### Requirement: Active input interception
Active pointer overlays SHALL consume mouse clicks and scrolling over the covered desktop rather than forwarding them to underlying applications. TinyHand SHALL NOT intercept ordinary typing; it may continue reaching the previously focused application. Deactivation SHALL remove the overlays so normal mouse interaction can resume.

#### Scenario: Interact over an application
- **WHEN** the user clicks or scrolls over a covered application while the pointer is active
- **THEN** the underlying application does not receive those mouse interactions

#### Scenario: Type while pointing
- **WHEN** the user types ordinary text while the pointer is active
- **THEN** TinyHand does not consume that text as pointer input

#### Scenario: Resume desktop use
- **WHEN** the user deactivates the pointer
- **THEN** the overlay no longer blocks desktop mouse interaction

### Requirement: German tray controls
The tray menu SHALL expose hand or laser selection, pointer size, sound enablement and selection, drawing clear, per-user Windows startup and application exit, with German control labels.

#### Scenario: Open the tray menu
- **WHEN** the user right-clicks the tray icon after deactivating the pointer
- **THEN** the German menu offers the supported pointer, drawing, sound, startup and exit controls
