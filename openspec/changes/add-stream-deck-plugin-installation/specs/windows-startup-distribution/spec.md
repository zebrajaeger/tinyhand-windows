# Spec Delta

## ADDED Requirements

### Requirement: Embedded release plugin validation
Published Windows release EXEs SHALL embed a validated TinyHand Stream Deck package from the same source checkout with matching identity, version and SHA-256 metadata. Release checks SHALL fail if it is missing, invalid or inconsistent. End users SHALL NOT need Node, a sidecar package or a runtime network download for in-app installation. App-only developer builds SHALL remain supported outside the release publication path.

#### Scenario: Build a Windows release
- **WHEN** the Windows release pipeline publishes its portable EXE
- **THEN** it includes the verified plugin package built from that checked-out source and validates the package inside the published executable

#### Scenario: Missing or stale package input
- **WHEN** release packaging encounters missing resources, wrong plugin identity or mismatched metadata
- **THEN** publication fails rather than producing an EXE with unavailable or misleading installation support

### Requirement: Non-interactive plugin resource check
The published executable SHALL provide a non-interactive embedded-plugin resource check returning zero on valid resources and nonzero otherwise. That check and the existing audio-resource check SHALL NOT create tray/overlay windows, detect Stream Deck, offer installation, launch an installer or mutate reminder state. Ordinary startup SHALL remain pointer-inactive regardless of restored reminder choices.

#### Scenario: Validate embedded plugin in CI
- **WHEN** the plugin resource-check mode runs on a machine without Stream Deck installed
- **THEN** it validates embedded identity/version/checksum and exits without prompts or installation effects

#### Scenario: Start with saved installation choices
- **WHEN** TinyHand starts normally with saved reminder preferences
- **THEN** the pointer stays inactive and offer policy does not restore activation
