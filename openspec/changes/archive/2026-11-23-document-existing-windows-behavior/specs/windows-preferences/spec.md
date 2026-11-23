# Spec Delta

## Purpose

Restore Windows pointer appearance and sound choices safely without persisting pointer activation or conflating autostart with JSON preferences.

## ADDED Requirements

### Requirement: Safe preference defaults and recovery
Without usable settings, the application SHALL use the lavender hand, pointer height 340, selected sound thock and disabled tap sound. Missing, malformed or unreadable settings SHALL fall back safely. An unknown hand or sound SHALL normalize to the respective default, and stored pointer height SHALL be constrained to 160 through 640.

#### Scenario: Missing or malformed settings
- **WHEN** the settings file is missing or contains malformed JSON
- **THEN** the application loads safe defaults without activating the pointer

#### Scenario: Invalid saved values
- **WHEN** saved preferences contain an unknown hand, unknown sound or out-of-range pointer height
- **THEN** hand and sound normalize to their defaults and height is constrained to the supported range

### Requirement: Preference persistence without activation
The application SHALL persist selected hand or laser, pointer height, sound enablement and selected sound under `%USERPROFILE%\.config\TinyHand\settings.json`. It SHALL restore those preferences on the next start without activating overlays. Updates SHALL replace the settings file through a temporary file rather than writing the destination in place.

#### Scenario: Save and restart
- **WHEN** the user changes appearance and sound preferences, saves successfully and restarts TinyHand
- **THEN** the selections are restored and the pointer remains inactive

#### Scenario: Replace saved preferences
- **WHEN** updated preferences are saved successfully
- **THEN** the destination settings file is replaced with the complete serialized preferences

### Requirement: Independent autostart state
Autostart SHALL be registry-backed and SHALL NOT be restored or enabled through the JSON preference file.

#### Scenario: Restore ordinary settings
- **WHEN** TinyHand reads a saved preferences file
- **THEN** this does not create or enable a Windows startup registration
