# Proposal

## Why

TinyHand for Windows already has implemented behavior, documentation and regression coverage, but no OpenSpec baseline. Capturing the current Windows contract will let future changes build on explicit requirements without reconstructing past decisions or mistaking documentation for newly implemented features.

## What Changes

- Add retrospective delta specs for six existing Windows capabilities, grounded in source code, paired user guides and the console test harness.
- Record source references and verification limits, especially audible headset playback, physical mixed-DPI setups and real sign-in behavior.
- Prepare a documentation-only baseline for review, synchronization into `openspec/specs/` and subsequent archival. These are new specifications of existing features, not new runtime capabilities.
- Preserve original macOS attribution and existing Windows behavior. No application code, workflow, release, configuration or user-guide changes are proposed.
- Do not reconstruct individual historical changes or claim their original planning/test history. Selected commits are provenance references only.

## Capabilities

### New Capabilities

- `windows-pointer-control`: Safe startup, single-instance operation, tray controls, global hotkeys and input interception.
- `windows-pointer-rendering`: Hand/laser presentation, tray imagery, per-monitor overlays and coordinate handling.
- `windows-desktop-drawing`: Stroke creation, release-time arrowheads and clearing behavior.
- `windows-preferences`: Persisted hand, size and sound preferences with safe defaults and normalization.
- `windows-tap-audio`: Sound selection/previews, bounded complete-clip playback and same-stream silence keep-alive.
- `windows-startup-distribution`: Per-user autostart, portable Windows artifacts and tagged release behavior.

### Modified Capabilities

None. The repository currently contains no main specs.

## Impact

Only OpenSpec documentation is added. Existing implementation areas are `windows/TinyHand.Windows/`, `windows/TinyHand.Windows.Tests/`, `windows/publish.ps1` and `.github/workflows/windows-release.yml`; none will be modified. The baseline describes commit `a36ee05` on `main` and is explicitly retrospective. User-owned initialization files and unrelated working-tree changes remain untouched. No commit, push, tag or publication is part of this change.
