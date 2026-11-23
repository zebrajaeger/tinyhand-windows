# Spec Delta

## MODIFIED Requirements

### Requirement: Optional Windows plugin distribution
The plugin SHALL have reproducible build/test/packaging and a locally installable Windows `.streamDeckPlugin`. Distributed TinyHand EXEs SHALL include the matching package for explicit in-app install/update. Documentation SHALL cover versions, both installation paths, reminders, reconnect and trust boundaries. App-only development builds SHALL remain usable without plugin tooling or Node. No installation SHALL occur without explicit user consent; Marketplace publication is outside this change.

#### Scenario: Install a local package
- **WHEN** a user installs the produced package into a supported Windows Stream Deck installation and starts TinyHand separately
- **THEN** its actions are available and connect without administrator rights or a separately installed Node runtime

#### Scenario: Build app only
- **WHEN** a developer runs an app-only Windows build without plugin tooling
- **THEN** ordinary TinyHand functionality builds independently and unavailable bundled installation is explicitly reported

#### Scenario: Install through TinyHand
- **WHEN** a user accepts an eligible install/update offer in a distributed TinyHand EXE
- **THEN** its matching embedded package is handed to Stream Deck while that software retains control of its confirmation dialog
