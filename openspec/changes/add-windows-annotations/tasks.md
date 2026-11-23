# Tasks

## 1. Regression coverage and safe edit preparation

- [ ] 1.1 Before editing each existing symbol, run GitNexus upstream impact, report callers/processes/risk and resolve UNKNOWN with source evidence; refresh the stale index only in the implementation phase if needed.
- [ ] 1.2 Add lifecycle assertions to the existing isolated STA console harness for active/passive transitions, retention on toggle/Esc, explicit clear, pending-stroke cancellation, layout resets and empty inactive startup.
- [ ] 1.3 Add tests for per-element color capture, legacy/invalid color settings, shared 500-element commit-time history and unchanged release-time arrow behavior.

## 2. Document and rendering lifecycle

- [ ] 2.1 Introduce the ordered session-only committed stroke/text model with immutable color/style values, separate pending input and bounded history.
- [ ] 2.2 Extract shared committed-annotation rendering with physical-coordinate origin/clip and per-axis DPI conversion; add geometry/pixel tests for text, arrows and cross-monitor clipping.
- [ ] 2.3 Refactor controller activation and overlay lifetime so passive annotations remain visible without hand/tap, focus capture, hidden system cursor or active pose/audio keep-alive loop.
- [ ] 2.4 Implement actual passive mouse/scroll hit-test transparency and explicit clear; update Esc precedence, capture-loss handling and display-layout cleanup for both active/passive modes.

## 3. Color and text entry

- [ ] 3.1 Add validated RGB preference with legacy-compatible default `#E14164` and atomic saves; expose German palette controls through shared color operations.
- [ ] 3.2 Implement text-placement and focused-editor states with click anchoring, fixed 28-DIP Segoe UI appearance, captured color and no placement tap/stroke/sound.
- [ ] 3.3 Implement Unicode/paste/IME handling, single-line normalization, 500-scalar limit, Enter commit and empty/Esc cancellation; add corresponding model/editor tests.
- [ ] 3.4 Implement focus-loss, explicit pointer command, clear, shutdown and topology cancellation precedence; verify return-to-prior-mode and non-stealing focus restoration.
- [ ] 3.5 Add German text tray action, consistent shared activation/size/sound/color/clear/text operations and a read-only controller snapshot for dependent changes.

## 4. Documentation and acceptance

- [ ] 4.1 Update both root and both Windows README translations for retained annotations, click-through passive display, new Esc semantics, clear-all, color and focused text entry/first-version limits. Update `AGENTS.md` for lasting architectural/input-contract changes.
- [ ] 4.2 Verify .NET 10 SDK availability, then run Release build, the console assertion harness, publish/resource validation and `git diff --check`; retain isolated test state and avoid overwriting a running published executable.
- [ ] 4.3 Manually verify underlying app clicks/scroll/cursor in passive mode, Unicode/IME/focus-loss editing, topology changes and negative-origin/mixed-DPI rendering; report untested hardware cases explicitly.
- [ ] 4.4 Run separate audio regression and a real headset first-click listening check after 3-5 seconds idle, confirming active-and-enabled-only keep-alive was preserved.
- [ ] 4.5 Validate this change strictly; before any separately authorized commit run complete GitNexus detect-changes and resolve partial/truncated results. Do not start the screenshot change until this foundation is accepted and its specs synced.
