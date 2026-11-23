# Verification evidence

## Automated/local checks (2026-11-23)

- .NET SDK `10.0.404`; `dotnet build windows/TinyHand.Windows -c Release`: zero warnings/errors.
- `dotnet run --project windows/TinyHand.Windows.Tests -c Release`: **389 assertions**.
- Separate `--audio-regression`: complete clips, bounded bursts, six sounds, three-second pauses, keep-alive restart and cleanup passed. This is not a new headset listening acceptance.
- In `stream-deck`: `npm run typecheck`, `npm test` (**12 tests**), `npm audit` (**zero vulnerabilities**), `npm run package` (Elgato validation and actual ZIP manifest/hash metadata passed).
- Default `windows/publish.ps1 -OutputDirectory <isolated temp directory>` passed with embedded plugin `0.1.0.6` and both diagnostic modes.
- `windows/publish.ps1 -AppOnly -OutputDirectory <separate temp directory>` passed; missing-plugin diagnostic exits 1 as intended.
- Missing `RequireStreamDeckPackage` inputs fail the build. Deliberately stale version metadata can compile, but its published resource diagnostic exits 1; default publish treats this as failure. No claim that MSBuild alone validates package bytes.
- Final EXE copied alone to `tinyhand-final-diagnostics`: both diagnostic exits 0; reminder state hash and app/Stream Deck process counts unchanged. Source confirms both modes return before mutex, tray, detector and control host; audio diagnostic intentionally writes audio.log.
- `actionlint 1.7.12 .github/workflows/windows-release.yml`: no findings. No tag, release or workflow publication performed.

## Regression scope

Registry fixtures use disposable current-user keys through injected hive/view adapters, not production registrations. Tests cover all four hive/view requests, quoted paths, bounded fallbacks, stale/missing executables and conflicts; no uninstall command is used.

Embedded resource tests cover identity/version/minimum/hash mismatch, cloned bytes, locked/unwritable target, tampering, junction refusal, versioned reuse and failed-extraction non-handoff. Coordinator tests cover shared refresh, two-second timeout, stale-status invalidation, no downgrade, launch failure, unconfirmed handoff, delayed newer observation and immediate shutdown cancellation without replay.

Live WPF offer-button and window-close tests exercise all choices. Real controller/tray tests with isolated state/clock cover startup off, repeated startup deferral, active-pointer guard, persistent opt-out/new bundle, manual retry and re-enablement without immediate install. Existing harness covers text/input/audio/overlay behavior.

The expanded failed-extraction test found an uncaught InvalidDataException. Coordinator and startup resource loading now handle it nonfatally; final harness rerun passed.

## Authorized real update

User approved one functionally equivalent plugin update `0.1.0.5` → `0.1.0.6` and targeted TinyHand restart. Installed Stream Deck: `7.6.1.23033`, initially closed. Only the old `tinyhand-cactus-publish` process was stopped; immediate restart briefly hit the still-held mutex and exited through the duplicate-instance dialog. A subsequent start of the relocated EXE displayed the update offer, with no package sidecar.

The application launched StreamDeck.exe with its managed extracted absolute package path. Desktop accessibility inspection verified the Elgato dialog explicitly named **TinyHand für Windows** before the authorized Installieren click. Installed manifest and all packaged assets match build `0.1.0.6`; persisted LastObservedVersion is `0.1.0.6`. The actual plugin transport completes a read-only handshake with the running app: connected, active=false, soundEnabled=false, textPhase=idle.

Of 63 pre-recorded profile files, 62 stayed byte-identical. Stream Deck rewrote one profile manifest; its TinyHand action plugin metadata now reports 0.1.0.6. No agent code modified profiles, deleted assignments or uninstalled plugins. Because the baseline recorded hashes rather than full contents, the complete semantic delta of that file cannot be independently reconstructed; do not claim every profile byte was unchanged.

Running app remains the relocated test EXE in the approved temp directory; final hardened build is in `tinyhand-installer-publish`. Do not silently replace the running file.

## Remaining acceptance / not passed

- **5.2:** genuine missing-plugin installation, real cancellation of Elgato's dialog, and real newer-plugin protection are not all exercised. Isolated equivalents pass, but manufacturing these states would require additional installation mutations not covered by the one-update permission. No plugin uninstall or profile deletion performed.
- **5.4:** relocated EXE installation passed without sidecars and source contains no runtime download, but network was not physically disabled for the actual installation. Other Stream Deck versions, physical device acceptance and real headset listening are not certified. App-only publish passed; full build with Node removed from the machine was not performed.

These integrated tasks remain unchecked. Completion or accepting partial acceptance requires a user decision; no scope has been weakened.

User subsequently chose **Restabnahme offen lassen**: leave 5.2 and 5.4 unchecked, perform no additional installation mutation and do not archive the change.

## Impact audit

Earlier upstream analyses reported HIGH for RefreshAsync, Extract and PluginVersion.TryParse (startup/install/resource-validation callers), explicitly warned before hardening. PointerController/startup changes traced to Program.Main. Refreshed graph now resolves class and launcher callers, but delegate-based InstallAsync and entry-point/test Run/Candidates edges can remain UNKNOWN; direct source call sites confirm their use and tests exercise them. UNKNOWN is not treated as unused or low risk. No implementation commit/push is authorized; run a complete fresh detect-changes before any later commit.
