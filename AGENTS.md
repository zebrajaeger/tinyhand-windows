# Maintaining TinyHand for Windows

## Start here

1. Check `git status --short --branch`, recent commits/tags and `git remote -v`.
   Preserve unrelated work; commit, push and publish only when requested.
2. Read `README.md` for the Windows user contract. For build, audio, autostart or
   release work, also read `windows/README.md` and the relevant implementation.
3. Make focused changes, add regression coverage, run the applicable checks below,
   and report exactly what was verified versus what still needs user testing.

This repository is the Windows-focused fork
`https://github.com/zebrajaeger/tinyhand-windows`; `origin` should point there and
`upstream` to `https://github.com/nilbuild/tinyhand`. Preserve the original project's
attribution and MIT copyright notice. Windows development lives in `windows/`;
`Sources/TinyHand/` is the original macOS implementation and a useful behavior
reference, not a Windows build dependency. Change macOS code only for an explicit
cross-platform task. Windows tray labels are German. Documentation is paired:
`README.md` (English) / `README.de.md` (German), and `windows/README.md` (German) /
`windows/README.en.md` (English). Update both translations for user-visible changes;
keep language-switch links and links to the same-language guide intact.

## Architecture and behavior

- `windows/TinyHand.Windows/Program.cs`: STA/WPF entry point, single-instance mutex,
  and `--check-audio-resources` mode used to validate the published executable.
- `PointerController.cs`: tray menu, global hotkeys, preferences, overlay lifecycle,
  drawing state and the roughly 16 ms pose/render tick.
- `OverlayWindow.cs`, `HandPose.cs`: per-monitor overlay, input and rendering;
  the image rotates around the measured fingertip. The grip follows smoothly,
  based on the macOS `ArmView.swift` model. The separate extended stick segment
  has not been ported.
- `Ink.cs` (`InkStroke`): drawing geometry and arrowheads. Desktop drawing coordinates are
  physical screen pixels; WPF rendering uses local device-independent coordinates.
  Keep conversions explicit, including negative monitor coordinates and mixed DPI.
- `Settings.cs`: persisted style, size and sound; safe defaults and atomic writes.
  User state lives in `%USERPROFILE%\.config\TinyHand\`; `AudioLog.cs` writes
  `audio.log` there. Tests use an isolated settings path, not the user's real file.
- `TrayHandIcon.cs`: crops the selected hand at the wrist to make a readable tray
  icon. Laser uses lavender. Replace/dispose icons safely, including native HICONs.
  `app.ico` is a multi-resolution cookie-hand icon embedded into the EXE via
  `ApplicationIcon`; it is independent of the selected tray style.
- `StartupRegistration.cs`: optional current-user Run registration, value
  `TinyHand.Windows`, with a quoted absolute EXE path. It is registry-backed rather
  than a JSON preference. Tests inject an isolated registry key and clean it up.
  Portable moves need disabling/re-enabling; Task Manager can independently block
  startup. Autostart never activates the pointer.

Preserve the user contract: the pointer and tap sound default to off; restored
preferences do not activate overlays. Active overlays consume mouse clicks and
scrolling, but not normal typing. `Ctrl+Alt+H` toggles, `Ctrl+Alt+X` clears, `Esc`
clears and exits. Shift **at mouse release** adds an arrow. The tray menu is normally
used after turning off the overlay.

## Audio: preserve the headset fix

`TapSound.cs` streams embedded mono 44.1 kHz/16-bit PCM through `waveOut`.
Keep queued clips intact: restarting `SoundPlayer.Play` could truncate a quack.
More importantly, a Jabra headset lost the first short sound after pauses over
two seconds, while a second quick click worked. Merely holding the device open
did not solve it. The user confirmed the fix: continuously queue digital silence
on the **same stream** while `Active && SoundEnabled`.

Preserve the bounded clip queue, short silence queue, background pump locking and
native-buffer lifetime until `WHDR_DONE`. Disable keep-alive when the overlay or
sound is off; finish queued buffers and release the device. Resource decoding and
buffer completion do not prove audible playback: headset changes need a real
listening test, including a first click after 3–5 seconds of inactivity.

## Verification and build

Use Windows with .NET 10 SDK. Check `dotnet --version` before building: this
maintainer's machine has sometimes exposed only a runtime on PATH. A session-local
SDK was installed at
`C:\Users\Lars.Brandt\AppData\Local\Temp\opencode\tinyhand-dotnet\dotnet.exe`;
check whether it still exists rather than assuming that path on other machines.

```powershell
dotnet build windows/TinyHand.Windows -c Release
dotnet run --project windows/TinyHand.Windows.Tests -c Release
dotnet run --project windows/TinyHand.Windows.Tests -c Release -- --audio-regression
./windows/publish.ps1
git diff --check
```

The tests are a console assertion harness, **not `dotnet test`**. The default run
briefly opens real overlay windows and checks assets, geometry, settings, icons and
isolated autostart registration. `--audio-regression` is a separate audible test
requiring an audio device; CI intentionally skips it. No external test framework
is needed. The publish script builds the self-contained single-file x64 EXE and
runs `--check-audio-resources`; optional ARM64 publishing is documented in the guide.

Output: `dist/windows/win-x64/TinyHand.Windows.exe`. A running EXE locks that file;
ask the user to quit that copy or publish to an isolated directory, rather than
killing their app. `dist/`, `bin/` and `obj/` are ignored; keep artifacts out of Git.

## Release procedure

Read `.github/workflows/windows-release.yml` before changing release behavior.
Windows tags are `windows-vMAJOR.MINOR.PATCH`, separate from upstream macOS `v*`
tags. The original macOS release workflow uses Apple signing secrets and is not
the Windows release path. Discover the latest Windows tag instead of relying on
a version recorded in this document.

When publication is requested:

1. Update root/Windows documentation and release-note template for user-visible
   changes. Run regression checks and validate modified workflow YAML (actionlint
   when available).
2. Commit only relevant files, create an annotated unused Windows tag and push
   branch plus tag, preferably atomically. Existing release tags remain immutable.
3. Confirm the **Windows Release** run actually starts. Fork workflows initially
   did not run for the first tag; this was recovered through `workflow_dispatch`
   on `main`, with input `tag` set to the existing Windows tag. The workflow checks
   out that tag's source, builds/tests on Windows and publishes from a separate
   restricted write-permission job. It uses automatic `GITHUB_TOKEN`, no custom
   signing secret. Reruns replace assets while preserving edited release notes.
4. Verify successful completion **and actual release assets**: the portable
   `TinyHand-Windows-<version>-win-x64.zip` plus `.zip.sha256`. A tag/source archive,
   pushed commit or queued workflow alone is not a finished binary release.

## Known limits and follow-up work

- Published EXEs are unsigned. Downloaded files carry Mark of the Web and can
  trigger SmartScreen although the identical local build does not. Keep the warning
  in user documentation; never promise its removal or disable system-wide security.
  Trusted code signing with timestamping is a future task, not implemented.
- Size is mostly bundled .NET/WPF/WinForms runtime and roughly 62 MiB of 36 hand
  PNGs, not application logic or sounds. Image optimization and single-file
  compression were suggested but have not been implemented. Keep the portable
  no-runtime-install contract unless the user chooses a different distribution.
- No auto-updater, installer, custom hands, URL automation, floating toolbar or
  click-through mode yet. Consult the README for monitor/screen-sharing caveats.

Update this file when architectural decisions or maintenance gotchas change;
keep transient task progress and version numbers in commits/releases instead.

<!-- gitnexus:start -->
# GitNexus — Code Intelligence

This project is indexed by GitNexus as **tinyhand-windows** (1845 symbols, 4697 relationships, 162 execution flows).

> Index stale? Run `node .gitnexus/run.cjs analyze --index-only` from the project root — it auto-selects an available runner. No `.gitnexus/run.cjs` yet? Bootstrap with `npx`, `bunx`, or `pnpm dlx` — e.g. `bunx gitnexus@latest analyze` (npm 11 npx crash; #1939).

## Always Do

- **MUST run impact before editing.** Use `impact({target: "symbolName", direction: "upstream"})` or `node .gitnexus/run.cjs impact "symbolName" --direction upstream --repo .`; report callers, processes, and risk. Never substitute grep for graph analysis.
- **MUST analyze graph changes before committing.** Use `detect_changes({scope: "all"})` (MCP) or `node .gitnexus/run.cjs detect-changes --scope all --repo .` (CLI fallback). `partial: true` or `truncated: true` is not a clean check — a zero means unseen, not unaffected; re-run it. For regression review: `detect_changes({scope: "compare", base_ref: "main"})` or `node .gitnexus/run.cjs detect-changes --scope compare --base-ref "main" --repo .`.
- MUST warn on HIGH/CRITICAL `risk` pre-edit; never use `riskSharedAxes` to waive a HIGH/CRITICAL `risk` warning. Compare File/symbol: MCP File omits axes; Graph-RAG expands File.
- **MUST treat `risk: UNKNOWN` as unresolved, not as low.** An empty caller set is not evidence the symbol is unused — it can also mean the callers are not resolvable by the index (plain-object property access, dynamic dispatch, cross-language calls). `impact` pairs `UNKNOWN` with a `riskNote` saying so. Confirm with a text search before treating the symbol as safe to change or delete; do not proceed on the strength of a zero.
- **MUST use `query({search_query: "concept"})` for concepts/flows, `context({name: "symbolName"})` for a named symbol, or `impact` for blast radius, on read-only callers, dependencies, imports, or execution flow.** Graph first; text search only for empty/`UNKNOWN`/literals.
- For security review, `explain({target: "fileOrSymbol"})` lists taint findings (source→sink flows; needs `analyze --pdg`).

## Never Do

- NEVER edit a function, class, or method before MCP/CLI impact analysis.
- NEVER ignore HIGH or CRITICAL risk warnings from impact analysis, and never read `UNKNOWN` as an all-clear — it means the walk could not answer, which is the one verdict that requires confirming by other means.
- NEVER rename symbols with find-and-replace — use `rename` which understands the call graph.
- NEVER commit before MCP/CLI graph change analysis.

## Resources

| Resource | Use for |
| --- | --- |
| `gitnexus://repo/tinyhand-windows/context` | Codebase overview, check index freshness |
| `gitnexus://repo/tinyhand-windows/clusters` | All functional areas |
| `gitnexus://repo/tinyhand-windows/processes` | All execution flows |
| `gitnexus://repo/tinyhand-windows/process/{name}` | Step-by-step execution trace |

## CLI

| Task | Read this skill file |
| --- | --- |
| Understand architecture / "How does X work?" | `.claude/skills/gitnexus-exploring/SKILL.md` |
| Blast radius / "What breaks if I change X?" | `.claude/skills/gitnexus-impact-analysis/SKILL.md` |
| Trace bugs / "Why is X failing?" | `.claude/skills/gitnexus-debugging/SKILL.md` |
| Rename / extract / split / refactor | `.claude/skills/gitnexus-refactoring/SKILL.md` |
| Tools, resources, schema reference | `.claude/skills/gitnexus-guide/SKILL.md` |
| Index, status, clean, wiki CLI commands | `.claude/skills/gitnexus-cli/SKILL.md` |

<!-- gitnexus:end -->
