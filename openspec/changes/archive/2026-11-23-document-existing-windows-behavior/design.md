# Design

## Context

See `proposal.md` for motivation and scope. This is a retrospective baseline of `main` at `a36ee05`, not a reconstruction of historical proposals. Main specs are currently empty. The implementation and test harness already exist; initializing OpenSpec did not verify runtime behavior.

This cross-cutting documentation migration needs decisions about capability boundaries, evidence and promotion into main specs. Existing user-owned initialization files are not part of this change.

## Goals / Non-Goals

**Goals:** Give future changes durable, externally meaningful requirement names and explicit scenarios; preserve safety-sensitive contracts; make evidence and verification limits traceable.

**Non-Goals:** Rewrite application code, claim every scenario has automated coverage, execute audible/desktop tests during planning, reconstruct one change per commit, invent past acceptance, add unsupported features or publish a release.

## Decisions

### One retrospective baseline, six capability boundaries

Use the six capabilities in the proposal rather than one specification per class or historical commit. This keeps runtime behavior independent of implementation structure. Alternatives were a large catch-all Windows spec or replaying every old commit; both make future changes harder to isolate and imply more historical certainty than the evidence supports.

Startup and distribution share the portable deployment contract for now. They may be separated by a future explicit change if they evolve independently; no extra speculative capability is introduced here.

### New requirements mean newly documented, not newly implemented

All deltas use `ADDED Requirements` because there are no existing main specs. Every capability includes a real Purpose so synchronization does not create placeholder descriptions. The proposal and this design retain the retrospective source revision; main specs will describe current behavior without pretending the features were implemented in this documentation change.

### Behavior in specs, mechanics and evidence here

Specs avoid class names and internal framework choices. Same-stream silence, bounded queues, quoted registry paths and safe startup remain explicit because they preserve reliability and user-facing contracts. Implementation mechanics such as PCM format, native buffer lifetime and lock ownership stay in source and `AGENTS.md`, not new externally visible API promises.

### Evidence is not execution

The following paths were inspected during planning. Existing tests are coverage evidence, not successful test runs in this change. Hardware-dependent requirements retain manual acceptance boundaries. GitNexus context was used for named symbols; concept queries returned no output and the CLI reported its FTS extension unavailable. Source inspection therefore supplies the missing behavioral detail; graph completeness is not claimed. No application symbol is edited, so no symbol-edit impact walk is needed.

| Capability | Source evidence | Existing regression evidence | Limits / additional acceptance |
| --- | --- | --- | --- |
| Pointer control | `Program.cs`, `PointerController.cs`, `OverlayWindow.cs` | `windows/TinyHand.Windows.Tests/Program.cs`: inactive restored controller, injected hotkey handling and overlay opening/closing | Real typing/click interception, focus preservation, hotkey conflicts and duplicate-process launch require interactive checks |
| Rendering | `OverlayWindow.cs`, `HandPose.cs`, `TrayHandIcon.cs`, `app.ico` and project file | Default harness: embedded assets, fingertip transform, tilt convergence, tray icon replacement and rendered overlays | Physical mixed-DPI/negative-coordinate setups, display changes and screen sharing need target-system checks |
| Drawing | `Ink.cs`, `PointerController.cs`, `OverlayWindow.cs` | Default harness: horizontal arrow geometry with negative coordinates, near-duplicate samples, single-point taps, arrow lifecycle, clear and current-stroke cancellation | Vertical/reverse arrows, actual Shift timing, capture loss and 500-stroke history are source-backed; do not assume automated input coverage |
| Preferences | `Settings.cs`, `PointerController.cs` | Default harness: isolated temporary settings path, defaults, malformed JSON, normalization, round-trip and inactive restored controller | Unreadable files and interrupted writes are not claimed as fault-injection-tested |
| Audio | `TapSound.cs`, `AudioLog.cs`, `PointerController.cs`, `Program.cs` | Default harness/resource mode: WAV decoding; `AudioRegression.cs`: six clips, overlapping requests, bounded bursts, keep-alive, first request after three seconds, shutdown | Audible regression was not run during planning; hearing the first sound after a three-to-five-second pause needs a real headset listening test |
| Startup/distribution | `StartupRegistration.cs`, `windows/publish.ps1`, `.github/workflows/windows-release.yml` | Default harness: isolated per-user registry key, quoted path, persistence, moved path, unrelated-value preservation and disable; workflow invokes tests/resource check | Real sign-in, Task Manager overrides, portable execution without installed runtime and actual release assets were not tested during planning |

Source paths without a directory above are under `windows/TinyHand.Windows/`. User-contract evidence is `README.md`, `README.de.md`, `windows/README.md` and `windows/README.en.md`. No user-visible behavior changes, so the paired guides need no edits for this baseline.

Historical provenance (not evidence of a test run):

- `6f1530b`: initial Windows pointer and drawing port.
- `3abec60`: sound preferences and headset keep-alive.
- `cd74a63`: dynamic hand tilt and smooth grip tracking.
- `2cb1ae9`: selected tray hand and fixed cookie executable icon.
- `d56933c`, `a8c0190`: tagged releases and rerun/manual recovery behavior.
- `e6d7c63`: optional current-user autostart.

### Promotion is a distinct reviewed operation

Planning creates proposal, deltas, design and tasks only. Applying this documentation change verifies the baseline and prepares final evidence; synchronization promotes it into `openspec/specs/`, and archival retains the closed change. Keeping promotion distinct makes review possible before these reconstructed requirements become the project's authoritative contract. Creating main specs directly was rejected because it would lose the baseline change and its provenance.

## Risks / Trade-offs

- [Source behavior mistaken for tested acceptance] → Explicit evidence matrix and separate automated, manual and unexecuted checks; never fabricate results.
- [Incidental implementation behavior becomes a permanent contract] → Focus on documented use, safety, portability and deliberate limits; keep formulas and class layout out of specs.
- [Documentation conceals a bug by changing code to fit the baseline] → Record discrepancies and resolve spec scope before any separately authorized implementation change.
- [Retroactive history appears contemporaneous] → Preserve the retrospective label and source commit in the archived artifacts.
- [Unrelated initialization work is overwritten] → Add only this change directory; do not edit configuration, agent files or existing skills.

## Migration Plan

1. Review and validate all six deltas and this evidence record as planning artifacts.
2. In a separately requested apply step, perform baseline verification, recording exact commands/results and remaining hardware checks; do not alter runtime code to satisfy reconstructed specs.
3. Synchronize the deltas into the six main-spec paths and validate the resulting specs.
4. Archive the completed change after confirming no unfinished documentation tasks remain. Do not mark hardware checks passed merely to archive a documentation baseline; record them as limitations.

There is no runtime deployment or data migration. Before promotion, rollback means withdrawing this documentation change. After promotion, corrections should be explicit follow-up spec changes; do not rewrite archived historical evidence or discard user-owned files.

## Verification during apply

The subsequently authorized apply step reviewed the six capabilities (25 requirements, 53 scenarios) against the cited implementation, guides and actual assertion labels. Coverage wording was corrected during planning: vertical/reverse-direction arrows and focus preservation are source-backed, not assertions in the default harness. Long audio/release requirements were split to pass strict validation. No application behavior or source code changed.

Executed on Windows during apply:

- `dotnet --version` on the ordinary PATH failed because only a runtime was available.
- The session-local SDK documented in `AGENTS.md` existed; invoking its `dotnet.exe --version` returned `10.0.404`. The full local SDK path was used for both commands below rather than modifying PATH or installing another SDK.
- `dotnet build windows/TinyHand.Windows -c Release`: PASS, zero warnings and zero errors.
- `dotnet run --project windows/TinyHand.Windows.Tests -c Release`: PASS, 200 assertions. This includes real temporary overlays; the harness uses isolated settings and registry state.
- `openspec validate document-existing-windows-behavior --strict`: PASS during planning; repeated for final apply validation.
- `git diff --check`: PASS during planning; repeated for final apply validation. New untracked Markdown files were additionally checked for trailing whitespace.

Not executed: audible regression/listening, physical mixed-DPI/display-change acceptance, ordinary typing/click interception, actual sign-in or Task Manager overrides, portable publishing, runtime-free-machine execution and release-asset checks. No hardware acceptance, publish or release is claimed by completing this documentation baseline.

The user separately requested pushing the completed work, including initialization. The existing OpenSpec/GitNexus initialization files were reviewed for local paths and obvious credential markers; the only local user path detected was the SDK maintenance note already tracked in `AGENTS.md`. Initial configuration and generated skills remain unchanged by this baseline work. Commit and push do not imply a new binary release.
