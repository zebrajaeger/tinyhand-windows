# Spec Delta

## Purpose

Provide optional complete tap sounds and prevent headset idle gaps through bounded same-stream digital silence during active sound-enabled pointer use.

## ADDED Requirements

### Requirement: Optional selectable tap sounds
The application SHALL provide six embedded tap sounds. Active left clicks SHALL request the selected sound only when tap sound is enabled. Selecting a sound from the tray SHALL preview that sound even when click sounds are disabled. Tap sound SHALL default to disabled.

#### Scenario: Click with sound disabled
- **WHEN** the user clicks in pointer mode with tap sound disabled
- **THEN** no click sound is requested

#### Scenario: Click with sound enabled
- **WHEN** the user clicks in pointer mode with tap sound enabled
- **THEN** the selected tap sound is requested

#### Scenario: Preview a selection
- **WHEN** the user selects one of the six sounds while click sounds are disabled
- **THEN** that sound is previewed and becomes the selected sound without enabling click sounds

### Requirement: Complete clips and bounded bursts
New tap requests SHALL NOT restart or truncate already queued clips. At most four unfinished audible clips SHALL be retained; further requests at that limit SHALL be dropped to prevent a long audible backlog.

#### Scenario: Request another tap during playback
- **WHEN** another click requests a sound while an earlier clip is unfinished and capacity remains
- **THEN** the complete new clip is queued without cutting off the earlier clip

#### Scenario: Rapid click burst
- **WHEN** a tap request arrives with four audible clips still unfinished
- **THEN** that additional sound request is dropped rather than increasing the audible backlog

### Requirement: Same-stream headset keep-alive
While the pointer is active and tap sound is enabled, audio SHALL continuously queue bounded digital silence between clips on the same playback stream used by tap sounds. Keep-alive SHALL NOT remain enabled when either condition is false. Disabling keep-alive SHALL allow queued audio to finish and release the playback device after pending buffers drain.

#### Scenario: First click after inactivity
- **WHEN** the pointer and tap sound stay enabled through a three-to-five-second pause and the user clicks
- **THEN** the stream has not gone idle and the complete selected clip is queued on that same stream

#### Scenario: Disable sound or pointer mode
- **WHEN** the user disables tap sound or turns off pointer mode
- **THEN** no further keep-alive silence is queued and the playback device is released after pending buffers finish

### Requirement: Resource validation and diagnostics
The published executable SHALL support `--check-audio-resources`, validate decoding of all six embedded WAV resources without activating the pointer and return zero on success or nonzero on failure. Audio diagnostics SHALL be written to `%USERPROFILE%\.config\TinyHand\audio.log`.

#### Scenario: Validate packaged audio
- **WHEN** the published executable is run with `--check-audio-resources`
- **THEN** it validates all six resources, exits with the corresponding result and does not open pointer overlays

### Requirement: Honest audible acceptance
Successful resource decoding or buffer completion SHALL NOT be treated as proof that a headset produced audible playback. Headset acceptance SHALL require a listening check including the first click after a three-to-five-second pause.

#### Scenario: Assess a headset fix
- **WHEN** a maintainer checks headset playback after an idle pause
- **THEN** acceptance distinguishes actual listening results from resource or queue checks and does not claim audibility from automation alone
