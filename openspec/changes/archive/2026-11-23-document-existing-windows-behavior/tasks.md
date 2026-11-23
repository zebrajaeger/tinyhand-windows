# Tasks

This is documentation-only retrospective work. Do not change application code to make it fit a reconstructed requirement. Existing test coverage is not a new passing test run. Checkbox completion below belongs to the separately requested apply workflow, not artifact creation during planning.

## 1. Review the baseline contract and evidence

- [x] 1.1 Review the six delta specs against the source revision and paired guides cited in `design.md`; verify every requirement describes existing Windows behavior, each has a WHEN/THEN scenario, and the final review records any corrections without introducing runtime changes.
- [x] 1.2 Finalize the evidence matrix in `design.md` against the actual assertions in `windows/TinyHand.Windows.Tests/Program.cs` and `AudioRegression.cs`; verify source-only, automated-coverage and manual-only claims remain distinct, including the uncovered vertical/reverse arrows, real input, physical DPI and sign-in cases.

## 2. Verify the documentation baseline without overstating runtime acceptance

- [x] 2.1 Check `dotnet --version` and SDK availability (including the documented session-local executable if needed), then run the Release build and default console regression harness when available; record exact commands, results and blockers in a verification section of `design.md`, preserving isolated settings/registry use and never substituting `dotnet test`.
- [x] 2.2 Record headset listening, mixed-DPI/display changes, actual sign-in/Task Manager behavior and real release-asset checks as not executed unless genuinely performed; verify the final evidence explicitly says no audible regression, publish or release was performed when that remains the case. These manual checks are not required to pretend a documentation-only baseline is a new hardware certification.
- [x] 2.3 Validate the complete change with `openspec validate document-existing-windows-behavior --strict` and check whitespace with `git diff --check`; verify both succeed and a final path/status review shows only intended OpenSpec artifacts were changed by this work.

## Workflow follow-up

- Synchronize the six reviewed capability deltas into `openspec/specs/` using the sync workflow; verify all main specs retain their Purpose, requirement names and scenarios and pass strict validation.
- Archive the documentation change after its tracked tasks are complete; verify the archive contains proposal, design, tasks and deltas, the six main specs remain present, and no history or test evidence was invented.
- Do not commit, push, tag or publish unless separately requested.
