# Proposal

## Why

Users currently have to obtain and install the Stream Deck plugin separately, while the Windows release only distributes the application. TinyHand should offer the matching plugin directly when compatible Stream Deck software is installed, without making portable use noisy or requiring another download.

## What Changes

- Embed a validated, matching `.streamDeckPlugin` package and version/hash metadata in distributed Windows EXEs; no runtime network download or sidecar file is required.
- Detect installed Stream Deck software independently of whether its process or hardware is present; compare its supported version and the installed TinyHand plugin version.
- At normal startup, offer installation for a missing plugin or an update for an older one. Keep pointer activation off and detection non-blocking.
- Provide **Installieren**, **Später** and **Nicht mehr fragen**. Defer reminders for seven days; suppress all automatic installation/update offers after opting out. Offer each bundled version at most once except after an explicit seven-day deferral.
- Add German tray installation/status and automatic-offer settings, available despite opting out.
- Extract verified embedded bytes into a user-local versioned directory and delegate installation to Stream Deck. Never automate its confirmation dialog, overwrite profiles, downgrade a newer plugin or claim success merely because the package was opened.
- Extend release packaging to build/embed the plugin reproducibly. Preserve app-only development builds without Node/Stream Deck tooling; those builds honestly report unavailable bundled installation.

## Capabilities

### New Capabilities
- `windows-stream-deck-installation`: Detection, quiet offer policy, user choices, local package extraction, installer handoff and observed installation status.

### Modified Capabilities
- `stream-deck-controls`: Embedded delivery and explicit in-app installation while retaining standalone plugin packaging and runtime independence.
- `windows-startup-distribution`: Release EXEs include a validated matching plugin resource and checks; original inactive startup and resource-check isolation remain intact.

## Impact

- Expected implementation: new detection/install-policy service and injected filesystem/clock/shell adapters, controller startup/tray wiring, isolated persisted offer state, embedded-resource project configuration, plugin packaging script, `windows/publish.ps1` and `.github/workflows/windows-release.yml`.
- Update both root README translations, both Windows guides and both plugin guides during implementation. Refresh release-note template for current annotations/Esc semantics and the new optional installation experience.
- No macOS changes, full Windows installer, app auto-updater, background plugin installation, Marketplace integration, runtime downloads, signing, purchases, tag creation or publication authorized by this change.
- The maintainer's agent permission to confirm TinyHand installation dialogs remains development-only; the shipped application must not inherit it.
- The archive's pending hardware/isolation acceptance is not silently completed by this proposal. New real installation/update tests are separate acceptance tasks.
