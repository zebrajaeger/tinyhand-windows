# Spec Delta

## Purpose

Give Windows presenters configurable Stream Deck buttons for TinyHand with truthful application state, safe disconnection behavior and independently installable plugin packaging.

## ADDED Requirements

### Requirement: Consistent cactus action branding
All Stream Deck keys and sidebar actions SHALL retain the tilted cactus README artwork with a readable function badge. Key labels and selection markers SHALL convey state without replacing the artwork with a solid green background. The category icon SHALL use the same application artwork, while different actions remain distinguishable.

#### Scenario: Compare sidebar and key
- **WHEN** a user views a TinyHand sidebar action and its configured key
- **THEN** they share the cactus background and corresponding function badge

#### Scenario: Toggle sound or pointer state
- **WHEN** represented state changes
- **THEN** the cactus background remains visible while labels and selection markers update

### Requirement: Presentation control actions
The plugin SHALL provide German key actions for pointer toggle, size, sound toggle, color, free-text placement, screenshot and clear-all using direct local commands, not hotkey simulation. Each press SHALL submit at most one command. Size SHALL support cycle/fixed presets; color SHALL be per-key opaque RGB. Text SHALL start free typing. Clear SHALL work while inactive and preserve activation.

#### Scenario: Configure and press size or color
- **WHEN** a valid fixed-size or color key is configured and pressed
- **THEN** TinyHand selects that configured value and previously completed annotation colors remain unchanged

#### Scenario: Use text, screenshot and clear
- **WHEN** the respective key is pressed while connected
- **THEN** TinyHand starts free-text placement, copies the cursor-monitor screenshot or clears all annotations according to that feature's contract

#### Scenario: Hold or release a key
- **WHEN** an action key is pressed and then released without another press
- **THEN** the release does not issue a second toggle or duplicate operation

### Requirement: Truthful multi-key feedback
All key instances SHALL use snapshots, not optimistic toggles: actual pointer/sound on/off, current size/preset selection and configured color/selection. Text/capture SHALL show pending/results. Unavailable and incompatible SHALL differ from off by text/symbols as well as color. Errors SHALL NOT falsely update state. Invalid configuration SHALL block submission and show actionable feedback.

#### Scenario: Update through another control
- **WHEN** tray, keyboard or another key changes a represented TinyHand value
- **THEN** every visible corresponding key updates from the authoritative state

#### Scenario: Screenshot is busy or fails
- **WHEN** capture is in progress or returns failure
- **THEN** the key shows pending or error respectively, not a success indication

#### Scenario: Invalid color
- **WHEN** a color key contains an invalid configured value
- **THEN** pressing it does not send an invalid command and displays a configuration error

### Requirement: Recovery without side effects
The plugin SHALL automatically reconnect with bounded retry intervals, obtain fresh state before enabling controls and mark cached state unavailable after disconnection. It SHALL NOT replay unacknowledged commands, automatically launch TinyHand or activate/deactivate it merely because the plugin/device connects or disconnects. Pending command queues SHALL be bounded. A timeout after sending SHALL be treated as an unknown outcome requiring state refresh, not an instruction to retry automatically.

#### Scenario: App not running
- **WHEN** the plugin is active but TinyHand is not running
- **THEN** keys show unavailable and retry connection without starting the app

#### Scenario: Disconnect after toggle
- **WHEN** the connection drops before a toggle's result is received
- **THEN** the plugin does not replay the toggle and refreshes actual state on reconnection

#### Scenario: Device reconnect
- **WHEN** a Stream Deck device is disconnected and reconnected
- **THEN** current state is shown without altering TinyHand activation or annotations

### Requirement: Optional Windows plugin distribution
The plugin SHALL have reproducible build/test/packaging and a locally installable Windows `.streamDeckPlugin`. Documentation SHALL cover versions, independent installation, key setup, reconnect and the same-user trust boundary. The portable app SHALL remain usable/buildable without Stream Deck, plugin tooling or separate Node installation. This change SHALL NOT auto-install, publish or submit the plugin to a marketplace.

#### Scenario: Install a local package
- **WHEN** a user installs the produced package into a supported Windows Stream Deck installation and starts TinyHand separately
- **THEN** its actions are available and connect without administrator rights or a separately installed Node runtime

#### Scenario: Build app only
- **WHEN** a developer runs the existing Windows app build/publish process without plugin tooling
- **THEN** it succeeds independently of the optional plugin package
