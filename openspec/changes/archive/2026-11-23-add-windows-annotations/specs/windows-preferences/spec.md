# Spec Delta

## ADDED Requirements

### Requirement: Annotation color preferences
The application SHALL atomically persist the selected opaque annotation color alongside existing preferences as an RGB value and restore it without activating the pointer. Missing, malformed or unsupported color values SHALL fall back to `#E14164`. Existing settings without a color field SHALL remain usable. Annotation content and pending text SHALL NOT be persisted in preferences.

#### Scenario: Restore selected color
- **WHEN** a valid annotation color is selected and TinyHand is restarted
- **THEN** that color is restored with the pointer inactive and an empty document

#### Scenario: Read legacy or invalid color settings
- **WHEN** settings omit the color or contain an invalid color
- **THEN** the default annotation color is used and other valid preferences retain their normal behavior
