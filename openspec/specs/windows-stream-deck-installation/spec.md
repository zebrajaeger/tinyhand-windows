# windows-stream-deck-installation Specification

## Purpose

Offer optional installation and updates of the matching embedded TinyHand Stream Deck plugin while respecting user consent, quiet startup and portable Windows operation.

## Requirements

### Requirement: Installed software and plugin detection
TinyHand SHALL detect installed Stream Deck software independently of a running process or connected device and classify the TinyHand plugin as missing, older, matching, newer or indeterminate relative to the embedded version. Detection SHALL be local, non-blocking and bounded. Missing, unsupported or indeterminate software SHALL NOT trigger an automatic installation offer or prevent normal use.

#### Scenario: Installed but not running
- **WHEN** supported Stream Deck software is installed but closed and no device is connected
- **THEN** detection recognizes its installation and evaluates the TinyHand plugin without requiring process or hardware presence

#### Scenario: Absent or unreadable installation
- **WHEN** Stream Deck is absent or installation/version evidence is unreadable or contradictory
- **THEN** TinyHand starts normally without an automatic offer and reports the applicable status through manual tray access

#### Scenario: Unsupported software
- **WHEN** the detected Stream Deck version is below the embedded plugin's minimum version
- **THEN** no automatic installation offer occurs and manual access explains the required software version

### Requirement: Quiet startup offers
Normal startup SHALL offer installation when a supported installation lacks the plugin, or an update when its plugin is older, subject to saved offer preferences. Matching or newer versions SHALL receive no automatic offer. The offer SHALL occur after tray startup, at most once per process and never during pointer/text interaction. Detection and offers SHALL NOT activate the pointer or run in resource-validation modes or duplicate instances.

#### Scenario: Missing plugin at startup
- **WHEN** supported software and a missing plugin are found and reminder policy permits an offer
- **THEN** a German installation offer appears after startup while the pointer remains off

#### Scenario: Update versus downgrade
- **WHEN** the installed plugin is older, matching or newer than the bundled version
- **THEN** only the older version is eligible for an update offer and no automatic downgrade is proposed

#### Scenario: Pointer activated before detection completes
- **WHEN** detection finishes while pointer or text interaction is active
- **THEN** the automatic offer is withheld rather than interrupting interaction, with manual access still available

### Requirement: Offer choices and reminder persistence
The offer SHALL provide Installieren (Aktualisieren for updates), Später and Nicht mehr fragen. Später SHALL defer all automatic offers for at least seven days. Nicht mehr fragen SHALL persistently suppress both installation and update offers. An eligible bundled version SHALL be offered once unless explicitly deferred for later. Closing the offer SHALL behave like Später. Reminder choices SHALL survive restarts without activating TinyHand.

#### Scenario: Defer and restart
- **WHEN** Später is chosen and TinyHand restarts within seven days, even with a different bundled plugin version
- **THEN** no automatic offer appears before the deferral expires

#### Scenario: Opt out of future offers
- **WHEN** Nicht mehr fragen is chosen and a later TinyHand release embeds a newer plugin
- **THEN** automatic offers remain suppressed and manual installation/update is still accessible

#### Scenario: Installer handoff already offered
- **WHEN** installation was offered and handed off for a bundled version and the user later cancels Stream Deck's dialog
- **THEN** that version is not automatically offered again; manual retry remains available

### Requirement: Manual tray controls
German tray controls SHALL expose Stream-Deck-Plugin installation/status and an option to enable or disable automatic offers. Opting out SHALL NOT hide manual access. Matching/newer plugin versions SHALL be reported as current without downgrade installation. Re-enabling automatic offers SHALL clear prior per-version dismissal and deferral for the next eligible startup, without launching installation immediately.

#### Scenario: Install after opting out
- **WHEN** the user opens manual plugin controls after Nicht mehr fragen
- **THEN** status and an eligible install/update action remain available with explicit consent

#### Scenario: Re-enable reminders
- **WHEN** automatic offers are re-enabled through the tray
- **THEN** the next eligible startup may offer installation/update again, without an immediate installer launch

### Requirement: Verified portable package handoff
After explicit install/update consent, TinyHand SHALL verify embedded identity, version and checksum, extract only that package into a user-local versioned location and open it through a verified Stream Deck installation. No runtime download, sidecar dependency, administrator rights or direct writes to Stream Deck profiles/plugin contents SHALL be required. Shipped TinyHand SHALL NOT confirm Stream Deck's own installation dialog automatically.

#### Scenario: Move the portable executable
- **WHEN** only the distributed EXE is moved to another folder and installation is requested
- **THEN** it still supplies its embedded plugin without an adjacent package or network download

#### Scenario: Stream Deck asks for confirmation
- **WHEN** the handed-off package causes Stream Deck to display its installation dialog
- **THEN** the user retains control of that dialog and TinyHand does not click or bypass it

#### Scenario: Tampered or unwritable extraction target
- **WHEN** extracted bytes do not match trusted metadata or the extraction target cannot be safely written
- **THEN** installation reports failure and does not launch those bytes or modify Stream Deck data

### Requirement: Observed installation outcome
Opening the package SHALL be reported as an installer handoff, not installation success. TinyHand SHALL recheck installed plugin identity/version for a bounded period and on subsequent startup or manual refresh. Success SHALL require observed matching-or-newer installation. Cancellation, timeout, extraction and launch failures SHALL leave normal TinyHand use available and provide honest German status without repeatedly launching the installer.

#### Scenario: User cancels Stream Deck installation
- **WHEN** the package opens but no matching-or-newer plugin is observed within the follow-up period
- **THEN** status remains unconfirmed rather than successful and no automatic retry occurs

#### Scenario: Installation completes
- **WHEN** a recheck observes the correct plugin identity with a matching-or-newer version
- **THEN** installed status is shown and later startups do not offer a downgrade or redundant install

### Requirement: Safe persisted state and development fallback
Offer state SHALL be user-local and atomically persisted separately from activation and autostart. Missing state SHALL enable normal initial offers; malformed state SHALL fail quiet until manual re-enablement. Persistence failure SHALL be reported without repeated offers in the current process. A development EXE without an embedded package SHALL disable installation with an explanation rather than download or use an arbitrary stale local package.

#### Scenario: Malformed reminder state
- **WHEN** the reminder state cannot be parsed
- **THEN** automatic offers remain quiet and the tray permits explicit re-enablement

#### Scenario: Build without plugin tooling
- **WHEN** an app-only development build has no embedded plugin resource
- **THEN** normal app features work and plugin installation explains that this build contains no package
