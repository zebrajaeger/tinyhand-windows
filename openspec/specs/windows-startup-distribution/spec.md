# windows-startup-distribution Specification

## Purpose

Offer optional per-user sign-in startup and portable Windows releases without administrator rights, runtime installation or misleading security guarantees.

## Requirements

### Requirement: Optional current-user startup registration
The German tray option `Mit Windows starten` SHALL enable or disable the `TinyHand.Windows` value under `HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run`. Enabling SHALL store the quoted absolute path of the current executable. Disabling SHALL remove only that value. A user without an existing registration SHALL start with the option off; registration SHALL NOT require administrator rights or activate the pointer at sign-in.

#### Scenario: Enable startup from a path containing spaces
- **WHEN** the user enables Windows startup
- **THEN** the current-user Run value contains the quoted absolute executable path

#### Scenario: Disable startup
- **WHEN** the user disables Windows startup
- **THEN** only the TinyHand startup value is removed and unrelated values remain intact

#### Scenario: Start at sign-in
- **WHEN** Windows launches TinyHand through its enabled startup registration
- **THEN** TinyHand starts with the pointer inactive

### Requirement: Portable-move and startup-state limitations
The documented startup contract SHALL instruct users to disable and re-enable startup after moving the portable executable and disable startup before deleting it. It SHALL disclose that Windows Task Manager may block startup independently of the Run value. The tray state SHALL reflect the presence of a nonempty TinyHand Run value rather than promise that Windows will actually launch it.

#### Scenario: Move the executable
- **WHEN** a registered portable executable is moved to another folder
- **THEN** disabling and re-enabling startup from the new location updates the stored path

#### Scenario: Windows blocks startup independently
- **WHEN** Task Manager disables startup while the Run value remains present
- **THEN** the documented contract does not interpret the tray registration state as a guarantee of sign-in launch

### Requirement: Portable Windows build
The published Windows x64 application SHALL run on Windows 10/11 without a separate .NET installation, installer or administrator rights. Publishing SHALL produce a self-contained single-file executable at `dist/windows/win-x64/TinyHand.Windows.exe` and validate its embedded audio resources. Local publishing SHALL also support the explicitly selected `win-arm64` runtime without implying that ARM64 release assets are currently provided.

#### Scenario: Publish the default runtime
- **WHEN** the maintainer successfully publishes the default Windows runtime
- **THEN** the x64 portable executable is produced and its embedded audio resources are validated

#### Scenario: Run the portable download
- **WHEN** a user extracts the x64 release ZIP on supported Windows
- **THEN** the executable can run without installing .NET separately

### Requirement: Tagged Windows release verification
The Windows release workflow SHALL support pushed `windows-vMAJOR.MINOR.PATCH` tags and manual dispatch for an existing Windows tag, build the selected tag's source, run the non-audible regression harness and validate packaged audio. The audible regression SHALL remain a local check rather than a CI acceptance claim.

#### Scenario: Verify a tagged build
- **WHEN** the workflow builds a Windows release tag
- **THEN** it runs the non-audible harness and packaged-audio validation without claiming an audible headset test

#### Scenario: Recover a missing tag-triggered run
- **WHEN** the workflow is manually dispatched for an existing Windows tag
- **THEN** it builds that tag's source rather than the current branch source

### Requirement: Windows release assets and reruns
The workflow SHALL publish `TinyHand-Windows-<version>-win-x64.zip` without debug symbols and a matching `.zip.sha256` checksum. Suffixed versions SHALL be pre-releases. Reruns SHALL replace assets without overwriting edited release notes.

#### Scenario: Publish a Windows tag
- **WHEN** a valid Windows release workflow completes successfully for a tag
- **THEN** the release contains the portable x64 ZIP and its SHA-256 checksum

#### Scenario: Publish a suffixed version
- **WHEN** the workflow publishes a version with a suffix such as `-beta.1`
- **THEN** the release is marked as a pre-release

#### Scenario: Rerun an existing release
- **WHEN** the release workflow is rerun for an existing release
- **THEN** the binary assets are replaced and manually edited release notes remain intact

### Requirement: Unsigned-download disclosure and attribution
User documentation SHALL disclose that published executables are unsigned and downloaded copies can trigger Windows SmartScreen. It SHALL NOT promise warning removal or recommend disabling system-wide security. The Windows fork SHALL preserve the original project's attribution and MIT copyright notice.

#### Scenario: Read download guidance
- **WHEN** the user consults the Windows download documentation
- **THEN** it explains the unsigned-executable warning and identifies the original TinyHand project

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
