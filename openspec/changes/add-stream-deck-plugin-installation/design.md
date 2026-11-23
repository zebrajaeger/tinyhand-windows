# Design

## Context

See proposal.md for motivation. Windows is a self-contained .NET 10/WPF tray application. `Program.Main` short-circuits audio diagnostics before mutex/application startup; `PointerController` owns preferences, German menus and the dispatcher. `Settings` has atomic writes but no installer state. The plugin manifest has four numeric version components and requires Stream Deck 7.1+; currently the generated manifest version and npm package version are separate values. `windows/publish.ps1` publishes only the app; the Windows release workflow builds no plugin. The standalone plugin packer already validates a local `.streamDeckPlugin`.

This design is required because the change crosses startup UI, local state, secure file handling, project resources and release orchestration. No production implementation or configuration is changed by these artifacts.

## Goals / Non-Goals

**Goals:** A portable EXE supplies its own matching optional plugin; detection and offers have explicit, deterministic states; installer delegation stays inside consent and known local paths; tests can exercise policy without registry changes, actual dialogs or clipboard/device effects.

**Non-Goals:** A general installer, automatic app updates, background plugin replacement, Elgato-dialog automation in shipped code, Marketplace or network discovery, portable/custom Stream Deck directory discovery by disk-wide scan, profile manipulation, app/hand activation or retry loops that nag users.

## Decisions

### 1. One service boundary for detection, policy and installation

Introduce a focused installation coordinator separate from pointer/render/audio code. Inject software/plugin detector, immutable embedded-package provider, clock, user-state repository and shell launcher. Immutable observations include software path/version, package availability, installed plugin identity/version, and a reason for unavailable/unsupported/indeterminate. Pure policy maps observations and saved choices to an eligible offer; UI only renders that decision. This keeps registry/filesystem/clock behavior testable and avoids scattering installer conditions through tray callbacks.

All asynchronous completion is marshalled onto the WPF dispatcher; cancel on shutdown and reject callbacks into disposed UI. Cap detection at two seconds and permit one offer/installer handoff per process at a time. No broad filesystem or process scanning. `Program.Main` diagnostic exits and losing the single-instance mutex happen before service construction. After the tray is initialized, schedule local detection. Offer only when pointer/text interaction is idle; if it is already active when detection completes, skip the automatic offer for this process without recording a dismissal. Manual tray access remains available.

### 2. Detection is based on registered installed software, not process presence

Read current-user and machine App Paths and uninstall registrations in both registry views, then the standard per-user/Program Files Elgato locations as bounded fallbacks. Validate existence, actual executable file version, expected Stream Deck executable/product identity and normalized absolute path. Resolve shortcuts/quoted registration values safely; never treat an arbitrary uninstall command as something to execute. Deduplicate evidence. If multiple candidates disagree materially, classify indeterminate rather than pick an arbitrary installer target. Custom/portable copies not identified by these bounded sources get a manual explanatory status, not a disk scan or running-process prerequisite.

Read only the expected current-user plugin manifest at `%APPDATA%\Elgato\StreamDeck\Plugins\io.github.zebrajaeger.tinyhand.sdPlugin\manifest.json`. A missing directory is missing; a directory with missing/malformed manifest or wrong UUID is indeterminate and must not be silently overwritten. Use the bundled manifest minimum software version, initially 7.1; older Stream Deck gets an explanatory manual status without automatic offer. Compare installed plugin versions numerically as four nonnegative components, never lexicographically. Unknown or malformed versions are indeterminate; greater versions are current/newer and are never downgraded.

Alternatives rejected: process-only detection misses installed-but-closed software; app-wide directory scans are slow and overly intrusive; raw file presence alone cannot establish version or compatibility.

### 3. Persist reminder policy separately from pointer settings

Use a versioned atomic user-state file next to ordinary preferences, e.g. `%USERPROFILE%\.config\TinyHand\stream-deck-installation.json`, containing schema version, `automaticOffersEnabled`, last offered package version, `deferUntilUtc` and last observed installed version. Annotation content, Stream Deck paths and activation are not stored. Tests use an injected isolated path and clock.

Missing state enables initial offers. Invalid/unreadable existing state fails quiet, reports its recovery route in the tray and requires explicit re-enablement; do not fall back to repeatedly asking. Writes replace via a unique temporary file. Write failures show a nonfatal warning, preserve the in-memory decision and disable further automatic offers for this process. They cannot promise durable suppression across restarts, so explain the failure rather than claim the choice was saved.

Policy:

- Only supported software + a trusted embedded package + missing/older plugin is eligible.
- `automaticOffersEnabled=false` suppresses both initial installation and all later updates.
- A non-expired deferral suppresses every version until `deferUntilUtc` (UTC now + seven days). Equality at the deadline is eligible. Backward clock movement may extend silence, never shorten the chosen seven-day deadline.
- Last offered version matching the bundle suppresses repeat offers unless the user explicitly selected Später and its deadline has expired.
- A newer bundled version can be offered once, subject to opt-out/global deferral; an installed greater version remains protected.
- Closing the TinyHand offer behaves as Später. Installieren/Aktualisieren records the current offered version before handoff; cancelling Elgato later does not cause repeated automatic offers for that version. Manual retries are explicit.
- Tray re-enablement clears per-version dismissal and deferral, with no immediate installation/offer; next normal startup reevaluates.

Use a German three-choice dialog with installation/update-specific wording. The text says that Stream Deck will ask for confirmation and no TinyHand activation occurs. Do not implement hold-to-point or other unrelated presentation actions in this flow.

### 4. Verified extraction and explicit Stream Deck handoff

Bundle plugin identity `io.github.zebrajaeger.tinyhand`, four-component version, minimum software version, SHA-256 and the packaged bytes. Validate metadata against the package's actual manifest during build and resource-check mode; never deserialize executable scripts or unpack the plugin payload into Elgato directories at runtime. Runtime extraction writes only the single `.streamDeckPlugin` file into `%LOCALAPPDATA%\TinyHand\StreamDeckPlugin\<version>\` using a unique temporary file and atomic rename. Reuse an existing file only when its hash matches. Refuse symlink/reparse traversal in the managed output path, verify the final file after writing, and never launch unknown existing bytes. Extraction failure leaves the installed plugin untouched.

Launch the verified Stream Deck executable with the absolute package path as one quoted/structured argument through an injected launcher, using the documented/empirically verified local package-open mechanism. Do not blindly trust a potentially changed `.streamDeckPlugin` file association. Confirm real Stream Deck 7.1+ behavior in acceptance; if the direct invocation route cannot safely open a package, stop with an actionable manual status rather than executing an unverified handler. A closed Stream Deck may start as part of this consented handoff.

After successful launch, show “Installation in Stream Deck geöffnet” rather than “installiert”. Poll only the expected plugin manifest for up to 60 seconds (one-second interval), cancelling on shutdown. Missing/older status at timeout is unconfirmed, not failure proof; recheck on later startup and manual refresh. A valid matching-or-newer UUID/version establishes installed status; it does not prove active plugin connectivity or hardware function. No success claims from process launch alone, no automatic retries, no direct profile/plugin writes and no automatic removal of earlier version directories. These files are bounded to explicit consented installs; cleanup can be a separate future task.

### 5. Portable releases without forcing Node into app-only builds

Introduce one authoritative plugin-version metadata file used by npm packaging, generated manifest, extraction metadata and build validation. App and plugin versions are independent; compare plugin version from the package, not Git tag or EXE version. The release workflow checks out the tag first, installs pinned Node 24 tooling, runs `npm ci --ignore-scripts`, typecheck/tests/audit/package validation, then produces immutable package + hash/version metadata before publishing the EXE.

Pass explicit resource/metadata paths to MSBuild. Embedded resource items are conditional for ordinary app development; a release-only required-package property makes publish fail if resources are absent, stale or wrong. `windows/publish.ps1` orchestrates or consumes verified package inputs for its default distributable publish, and invokes `--check-stream-deck-plugin-resources` plus the existing audio check on the finished single-file EXE. An explicit app-only development build stays possible with no Node installed; it cannot silently pick up a stale package from `out/` or promise installer availability. Normal `dotnet build` remains app-only unless explicit package inputs are supplied.

Keep .NET/Node generated outputs ignored and out of Git. Runtime users need neither Node nor a sidecar; package bytes are embedded even if only the EXE is moved. Both supported publish architectures can embed the same JavaScript plugin, while the Windows release continues its documented x64 distribution. CI must validate the matching package provenance from the checked-out source, not reuse a mutable external artifact. Do not introduce Marketplace upload or a separate public plugin asset as an implicit requirement.

Alternatives rejected: sidecar files break single-EXE portability; runtime download adds offline/security/version coupling; committing built packages creates stale binaries; always requiring Node would regress existing app-only development.

## Risks / Trade-offs

- False/ambiguous install detection -> classify indeterminate and keep startup quiet; manual diagnostics explain the supported registration paths.
- User-local state cannot be written -> preserve process-local suppression, report persistence failure and never falsely promise durable opt-out.
- Extraction tampering/reparse targets -> verify package hash, managed path and post-write bytes; never launch unverified content.
- Changed Elgato installer invocation -> verify direct local-package handoff on supported software; fail safe with manual guidance rather than silently mutate Elgato folders.
- Closed/aborted installer or delayed installation -> bounded observation with unconfirmed status, no automatic retry or fabricated success.
- Embedding introduces Node into distributable builds -> isolate explicit release packaging while retaining app-only .NET builds and documenting different outputs.
- Existing package-version duplication and stale release notes -> centralize version inputs and update paired docs/template as implementation tasks, without retagging old releases.
- Signing/SmartScreen remains unchanged -> preserve unsigned EXE warning; embedded packaging does not remove Windows security prompts.

## Migration Plan

1. Add pure policy/detection tests and missing-package app-only behavior, then guarded startup/tray controls.
2. Add package provider/extraction/handoff and diagnostic mode; verify with isolated adapters before real installer acceptance.
3. Centralize plugin version and wire release packaging/embedded resources; validate published EXEs and copied-alone portability.
4. Publish only through a separately authorized release request after tests/acceptance. Existing app/plugin users keep their plugin/profile content; absent offer state defaults to one initial eligible offer.
5. Rollback disables automatic offers and removes in-app install entry points in a subsequent build without uninstalling the plugin or deleting user profiles. Diagnostic/runtime package errors fail safe and do not require a rollback to retain ordinary TinyHand use.

No unresolved user-facing decision blocks implementation; exact registered-path and invocation compatibility are bounded acceptance investigations, not permission to change consent/reminder semantics.
