# Design

## Context

See proposal.md for motivation. TinyHand currently renders into separate topmost WPF windows and has no screen-capture/clipboard service. `add-windows-annotations` introduces a shared committed-annotation renderer and independent passive/active lifecycle. This change must be applied after that foundation; its specs introduce a new capability rather than duplicate pending foundation deltas.

Capture is cross-cutting and depends on compositor/native resources and STA clipboard access, so the design artifact is applicable.

## Goals / Non-Goals

**Goals:** One request produces one cursor-monitor bitmap at native pixel resolution with annotations exactly once; pointer decorations are absent; all temporary presentation changes unwind even on failure.

**Non-Goals:** Screen recording, automatic files, region selection, all-monitor stitching, capture of secure/protected content, uploads or an arbitrary scripting API.

## Decisions

### 1. Freeze capture inputs before asynchronous work

On the WPF dispatcher, read the current Windows cursor and containing `Screen.Bounds`; do not use a cached render-tick cursor because pointer-off stops ticking. Freeze the topology identity, target DPI and committed annotation snapshot. Reject requests while a stroke or text placement/editor is pending (`busy`) instead of partially committing/cancelling user input. Allow one capture in flight; further requests report busy. Complete within a five-second operation deadline or fail and restore presentation. A monitor disappearing/changing bounds before capture completion invalidates the request.

### 2. Capture desktop background, then render the document once

Temporarily suppress all TinyHand overlay visuals on the target monitor without destroying windows, changing hit-testing, deactivating pointer mode or touching the document/audio. Wait for a rendered/composited frame before copying the physical monitor bounds into a 32-bit bitmap using Windows desktop capture (initial implementation: GDI screen copy behind an injectable interface). Render the frozen committed annotations onto that background using the foundation renderer, translated by negative monitor origin and scaled with that monitor's X/Y DPI. Restore visuals promptly in `finally`, before clipboard delivery/status notifications.

This avoids both capturing the hand and double-painting retained annotations. Merely hiding the hand would not establish deterministic inclusion/exclusion of editor/tap decorations. Do not rely on capture-exclusion window flags without proving their behavior across Windows 10/11; they are not a substitute for the background-plus-document model. Keep normal mouse interception in force while active so capture does not leak clicks to applications. Suppress mutation through the short background acquisition phase; process queued controller commands afterward. Snapshot semantics define which annotations appear even if later commands run during encoding/clipboard work.

### 3. Clipboard is the only output

Create an immutable bitmap and publish it on the STA dispatcher via a clipboard adapter. Retry temporary clipboard ownership contention a bounded number of times (at most three attempts within the operation deadline). Report success only after clipboard publication succeeds. No file is written, including diagnostic screenshots. Dispose bitmaps, DCs and native handles regardless of outcome. Diagnostics contain operation/error metadata, never pixels or text content.

German tray feedback distinguishes success, busy and failure; success notification occurs only after pixels were captured, so it cannot appear in the image. Later Stream Deck commands receive the same result. Focus remains on the original foreground application; clipboard actions do not launch dialogs.

### 4. Failure and limitation boundaries

Capture failures, unsupported desktops, unavailable monitors and clipboard exceptions become structured failures plus German feedback rather than a crash or a success acknowledgment. Never deliberately clear/overwrite the existing clipboard before a new bitmap is ready; OS clipboard failure cannot promise transactional restoration of arbitrary clipboard formats. Protected windows may produce black/blank regions without an API error: documentation must disclose this and tests must not imply it is detectable or bypassable. This captures a composed desktop background and a frozen document, not a simultaneous atomic snapshot of all moving application content.

## Risks / Trade-offs

- Compositor synchronization and brief visual suppression may flicker. Validate on real Windows; restore on every failure, timeout and shutdown path. A test double alone does not establish absence of pointer ghosts.
- GDI capture behavior varies for protected/exclusive-fullscreen content. Keep limitations explicit rather than promising universal fidelity.
- Mixed-DPI WPF bitmap rendering can shift text/stroke sizes. Use physical output dimensions and per-axis DPI; compare synthetic on-screen/offscreen landmarks and manual paste results.
- The clipboard may be locked by another process; bounded retry keeps the UI responsive and leaves an honest failure result.

## Verification and rollout

Implement after `add-windows-annotations` is implemented and synced. Use fake cursor/topology, capture and clipboard adapters for negative-origin selection, dimensions, clipping, snapshot consistency, busy/deadline handling and cleanup at every injected failure. Use a synthetic background with known colored annotations for pixel assertions. Manual Windows acceptance: capture while active/passive with the cursor on each monitor, paste into an image editor, check no pointer/tap/UI/double-painted ink and verify focus, annotations, clicks and audio state afterward. Run the existing .NET build/harness/publish checks; no plugin hardware is needed for this change.
