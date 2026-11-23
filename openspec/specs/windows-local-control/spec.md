# windows-local-control Specification

## Purpose

Provide a bounded Windows-session-local command and state interface for trusted same-user integrations without exposing remote control or duplicating application behavior.

## Requirements

### Requirement: Isolated optional local control
Running TinyHand SHALL offer a local control connection restricted to its current Windows user and interactive session, reject remote and other-user/session clients, and require no network listener, administrator rights or separately installed service. The audio-resource-check execution path SHALL NOT start the interface. Stopping TinyHand SHALL close it. Ordinary operation SHALL remain available without a plugin or client.

#### Scenario: Other user or session
- **WHEN** a remote client or a process from another Windows user or interactive session attempts control
- **THEN** it cannot obtain application state or execute a command

#### Scenario: Run standalone or validate resources
- **WHEN** TinyHand runs without a plugin or is invoked for audio resource validation
- **THEN** standalone tray/hotkeys work normally, and validation exits without opening a control endpoint

### Requirement: Versioned bounded command processing
The interface SHALL negotiate version 1, correlate results by request ID and explicitly reject incompatible versions. It SHALL validate allowlisted commands/arguments before mutation, reject duplicate IDs without re-execution and bound clients, buffering and processing time. Invalid, oversized or stalled requests SHALL fail or disconnect without mutation, crashes or blocked pointer rendering.

#### Scenario: Unsupported or malformed request
- **WHEN** a client negotiates an unsupported version or submits an invalid command or argument
- **THEN** it receives incompatibility/failure or is disconnected, and application state remains unchanged

#### Scenario: Duplicate or flooding client
- **WHEN** a client repeats an accepted request ID or exceeds connection/buffer limits
- **THEN** the command is not executed again and bounded rejection/disconnection preserves normal application use

### Requirement: Shared application commands
The interface SHALL support pointer toggle/set, size set/cycle, sound toggle/set, opaque RGB annotation color, free-text start, clear-all and monitor screenshot with normal local persistence/audio effects. Toggles SHALL execute once against authoritative state. Size presets SHALL be 220, 340 and 480; cycling SHALL select the next larger preset or wrap, including from legacy heights.

#### Scenario: Setters and toggles
- **WHEN** a client requests explicit pointer-off twice or a sound toggle once
- **THEN** pointer-off remains off without deleting annotations and sound toggles exactly once with the normal keep-alive policy

#### Scenario: Size cycle from a custom height
- **WHEN** size is 300 and the cycle command succeeds
- **THEN** size becomes 340 and the same preference/state update occurs as local size selection

#### Scenario: Start text or capture
- **WHEN** a valid client invokes text placement or screenshot
- **THEN** the prerequisite feature's focus, busy, cancellation and result behavior applies unchanged

### Requirement: Authoritative snapshots and results
Connections SHALL receive initial snapshots and ordered updates from every control source. Snapshots SHALL identify instance/revision and include pointer activation/height, sound, annotation color/count, text phase and capture-pending state, but no text or pixels. Correlated replies SHALL distinguish armed text placement from completed clipboard capture. Screenshot success SHALL mean clipboard publication; busy, invalid, incompatible or failed requests SHALL NOT report success.

#### Scenario: Change outside an integration
- **WHEN** a user disables the pointer with Esc or changes sound in the tray
- **THEN** connected clients receive the resulting authoritative state

#### Scenario: Capture failure
- **WHEN** a requested screenshot cannot be published to the clipboard
- **THEN** the corresponding result reports failure and current state rather than a successful queue acknowledgment

#### Scenario: Reconnect to a new instance
- **WHEN** TinyHand is restarted and a client reconnects
- **THEN** the client receives a new instance identity and full current snapshot rather than relying on old revisions

### Requirement: Restricted control authority
External control SHALL NOT enable autostart, execute processes, write arbitrary files or automatically launch TinyHand. Only explicitly allowlisted presentation operations SHALL be accepted.

#### Scenario: Request arbitrary execution
- **WHEN** a client requests a process launch, arbitrary file output or startup registration
- **THEN** the request is rejected without those side effects
