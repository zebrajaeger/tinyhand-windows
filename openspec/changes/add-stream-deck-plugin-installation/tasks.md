# Tasks

## 1. Detection and reminder policy

- [ ] 1.1 Before editing existing symbols, run GitNexus upstream impact, report callers/processes/risk and resolve UNKNOWN using source evidence; verify all edited symbols have recorded impact and no HIGH/CRITICAL warning is omitted.
- [ ] 1.2 Introduce injectable detector, clock and installation-state repository interfaces with immutable observations; verify pure tests classify absent, supported, unsupported, missing/older/matching/newer and indeterminate states.
- [ ] 1.3 Implement bounded local Stream Deck discovery from App Paths/uninstall registrations and standard install locations without requiring a running process or device; verify isolated registry/filesystem fixtures cover both registry views, quoted paths, stale entries, conflicts and missing executables.
- [ ] 1.4 Validate software executable identity/minimum version and installed plugin UUID/four-component numeric version; verify wrong UUID, missing/malformed manifests, lexicographic traps and newer-installed no-downgrade cases.
- [ ] 1.5 Implement atomic versioned reminder state in an isolated user-local file and pure seven-day/global-opt-out/per-package policy; verify missing-versus-corrupt state, deadline equality, new bundle during deferral, restart, clock rollback and write-failure suppression tests.
- [ ] 1.6 Document supported discovery boundaries, seven-day reminders and all-version opt-out in both plugin guide languages; verify paired text matches tested policy and does not promise custom-directory discovery.

## 2. Embedded package provenance and portable publishing

- [ ] 2.1 Centralize authoritative plugin version metadata and consume it in package/manifest generation; verify tests reject mismatched package identity, version and minimum-version metadata and no independent hardcoded version remains in generation.
- [ ] 2.2 Add validated package/hash/metadata build outputs from the pinned standalone plugin packer; verify typecheck, plugin tests, audit and Elgato validation pass and extracted packaged manifest agrees with metadata.
- [ ] 2.3 Add conditional embedded package/metadata resources and release-only required-package checks to the Windows project; verify app-only `dotnet build` succeeds without Node while release publish fails on absent, wrong or stale inputs.
- [ ] 2.4 Add `--check-stream-deck-plugin-resources` before normal startup and isolate both diagnostic modes; verify exit codes and no tray, overlays, detection, reminder writes or installer effects through tests and a published single-file check.
- [ ] 2.5 Extend `windows/publish.ps1` to produce/consume explicit verified plugin inputs for distributable builds and check both published resource modes; verify a clean isolated publish embeds the package and a copied-alone EXE passes checks without sidecars.
- [ ] 2.6 Update Windows release workflow to install pinned Node 24, build/test/validate the plugin from the checked-out tag and require embedding before the existing ZIP/checksum upload; verify modified workflow with actionlint where available and run equivalent build/package checks without pushing a tag or publishing a release.
- [ ] 2.7 Update both Windows guides and plugin guides for Node-required distributable builds versus app-only .NET development, authoritative versioning and diagnostics; verify documented commands against isolated outputs and preserve unsigned/portable limitations.

## 3. Safe extraction and consented installer handoff

- [ ] 3.1 Implement immutable embedded package provider and UUID/version/hash verification; verify missing/corrupt/wrong-identity resources fail safely without selecting local stale packages or downloading content.
- [ ] 3.2 Implement atomic versioned extraction under the managed local-app-data root with post-write verification and reparse/symlink refusal; verify tampered existing file, locked/unwritable path, path escape and reuse-of-matching-bytes tests never launch unverified content or write into Elgato directories.
- [ ] 3.3 Implement a launcher accepting the verified Stream Deck executable and one absolute package argument; verify mocked argument handling for spaces and no association/shell injection, and confirm direct package-open compatibility on supported installed Stream Deck before marking this task complete.
- [ ] 3.4 Add bounded 60-second installed-manifest observation with shutdown cancellation and explicit handoff/unconfirmed/installed/error results; verify canceled dialog, delayed installation, matching/newer version, shutdown and launch/extraction failures never produce false success or automatic retry.
- [ ] 3.5 Document the extra Elgato confirmation, extraction location, manual retry, no-profile-mutation and distinction between installation and runtime connectivity in both plugin guides; verify paired guidance against result-state tests.

## 4. Startup offers and German tray controls

- [ ] 4.1 Wire detection after normal tray startup with a two-second deadline, per-process offer bound, interaction guard and disposal cancellation; verify no blocking startup, no activation and no detection/offers in diagnostics or duplicate instances.
- [ ] 4.2 Add German install/update offer with Installieren/Aktualisieren, Später and Nicht mehr fragen plus close-as-Später behavior; verify UI-event tests apply the exact state transitions and do not automate Elgato's confirmation.
- [ ] 4.3 Add manual Stream-Deck-Plugin status/install/update/refresh and automatic-offer toggle controls; verify opt-out does not hide manual access, matching/newer versions cannot downgrade and re-enablement resets reminder state without immediate launch.
- [ ] 4.4 Handle indeterminate/unsupported/missing-package states and nonfatal persistence/extraction/launch errors with honest German feedback; verify no misleading success, current-process repeated offers or impact on pointer/text/audio operations.
- [ ] 4.5 Update both root READMEs, both Windows guides, both plugin guides and the release-note template for the install experience and current Esc/annotation semantics; verify translations/links agree and no automatic app updater, silent install or security-warning removal is promised.
- [ ] 4.6 Update durable maintenance guidance for embedded resources, diagnostic isolation, consent boundaries and test seams; verify development-agent dialog permission is explicitly separate from shipped app behavior.

## 5. Integrated acceptance

- [ ] 5.1 Run .NET 10 Release build, console harness, separate audio regression, plugin typecheck/tests/audit/package validation, isolated publish/resource checks, strict OpenSpec validation and `git diff --check`; record exact passing commands and keep generated artifacts out of Git.
- [ ] 5.2 With explicit permission for each real installation mutation, test detected-but-closed Stream Deck, missing plugin install, older plugin update, newer plugin protection and canceling Elgato's dialog; verify no user profiles/key assignments are changed and installed success is observation-based.
- [ ] 5.3 Test real startup repetition, seven-day policy with an isolated controllable state/clock, persistent opt-out across a new bundle, manual retry and re-enablement; verify pointer always starts off and ordinary tray/audio/input behavior remains unchanged.
- [ ] 5.4 Verify the distributed EXE alone installs offline after relocation and that normal app-only development works without Node/Stream Deck; report untested software versions or hardware cases explicitly instead of marking partial acceptance complete.

## Workflow follow-up

- Before any separately authorized commit, run complete GitNexus detect-changes and resolve partial/truncated results; commit/push only on request.
- Synchronize accepted delta specs and archive only after task status is reviewed; any unfinished acceptance requires explicit user approval and remains marked incomplete.
- Tags, release publication, Marketplace submission and previously archived pending hardware/isolation acceptance require separate authorization and are not completion criteria for this planning task.
