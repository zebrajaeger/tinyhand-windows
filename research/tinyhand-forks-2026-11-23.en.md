# TinyHand forks: ideas for the Windows version

**English** | [Deutsch](tinyhand-forks-2026-11-23.md)

As of **November 23, 2026**. This survey examined the fork network of
[nilbuild/tinyhand](https://github.com/nilbuild/tinyhand) with
[zebrajaeger/tinyhand-windows](https://github.com/zebrajaeger/tinyhand-windows) in mind.
This is research, not an implementation or runtime verification of the other apps.

## Scope and method

- The [repository API](https://api.github.com/repos/nilbuild/tinyhand) reported **81 forks**.
- The [direct fork list](https://api.github.com/repos/nilbuild/tinyhand/forks?per_page=100&sort=newest)
  returned **80 repositories**; page 2 was empty.
- The additional fork is [fduarte42/tinyhand](https://github.com/fduarte42/tinyhand),
  visible in the [fork list of AbdulRehman56/tinyhand](https://api.github.com/repos/AbdulRehman56/tinyhand/forks?per_page=100).
- All branch tips of the 80 directly listed forks were collected using
  `git ls-remote --heads`, not just their default branches: **124 visible branches**
  in total, with **62 tips outside the original history**. The additional nested
  fork has two visible branches.
- **47 direct forks** had only branch tips already present in the original history.
  This means no additional commits on their currently visible branches, not
  necessarily that they are synchronized with the latest original version.
- **33 direct forks** had additional commits, including this Windows fork.
  Including the nested fork, that makes **33 other repositories with additional commits**.
- Additional branches were fetched through Git and compared against their common
  ancestor with the original. Commit history and changed files were reviewed;
  the highlighted ideas below were also checked in the relevant source files.
  Separate screenshot/website branches without shared history do not count as app features.
- Original reference point:
  [`da98f98d529971297487b0ec6ef07dc549555686`](https://github.com/nilbuild/tinyhand/tree/da98f98d529971297487b0ec6ef07dc549555686).
  This Windows fork's reference point:
  [`82b4f4a`](https://github.com/zebrajaeger/tinyhand-windows/tree/82b4f4a).
- No other apps were installed or launched, and no builds or listening tests were run.
  Source code alone does not prove that a feature works in every situation.
  Effort estimates are qualitative assessments for the existing C#/WPF port.

## Important: not every idea originated in a fork

The current original already includes modifier keys for click-through, double-Shift
click-through, selectable ink colors, timed fading, custom hands, configurable
hotkeys, toolbar favorites, launch at login and URL automation.
Evidence: [Preferences.swift](https://github.com/nilbuild/tinyhand/blob/da98f98d529971297487b0ec6ef07dc549555686/Sources/TinyHand/Preferences.swift),
[OverlayController.swift](https://github.com/nilbuild/tinyhand/blob/da98f98d529971297487b0ec6ef07dc549555686/Sources/TinyHand/OverlayController.swift),
[HandMaker.swift](https://github.com/nilbuild/tinyhand/blob/da98f98d529971297487b0ec6ef07dc549555686/Sources/TinyHand/HandMaker.swift),
[README](https://github.com/nilbuild/tinyhand/blob/da98f98d529971297487b0ec6ef07dc549555686/README.md).

Many Windows and Linux forks merely port these features to another platform.
They still represent interesting gaps in this Windows version. Launch at login
is already implemented here, so it is not a new implementation proposal.

## The most interesting concrete ideas

### 1. Point without blocking normal app clicks

**Benefit:** The hand stays visible during a demo while buttons, browsers and
slides remain usable. An explicit drawing mode captures the mouse instead.

- [sergvax: click-through plus independently switchable drawing](https://github.com/sergvax/tinyhand/commit/a18a86a2719e2fd0d63d49a1e43c8e988a264d98):
  changes to `InputTap.swift`, `OverlayController.swift` and settings.
- [jorgenbruntveit: Pointer Mode](https://github.com/jorgenbruntveit/tinyhand/tree/pointer-mode):
  normal clicks by default, presentation behavior while holding Option.
- [trussosw: real-click mode with a click ripple](https://github.com/trussosw/tinyhand/commit/efebd3c14818504a2a86c7436e83395f679977fd):
  real app interaction can still trigger visual feedback.
- [MUL7ii: Windows hybrid mode](https://github.com/MUL7ii/tinyhand/blob/ea40fa0b2e/windows/src/hook.rs):
  initially holds the click; draws after 4 pixels of movement, otherwise replays
  the click to Windows after release. The code tags its own injected input and
  calls `SendInput` outside the hook callback.

**Context:** Basic click-through already exists upstream; interesting fork variants
are the modes, visual feedback and click/drag hybrid.
**Effort:** medium to high. Start with two clear modes, “Point”/“Draw”, rather than
delayed click replay. The hybrid needs to handle double-clicks, drag-and-drop,
text selection, modifiers, elevated windows and negative monitor coordinates
correctly. Do not blindly copy another fork's hook code.

### 2. Drawing tools: stroke width, color, fading and undo

**Benefit:** Readable markings on light/dark backgrounds and less manual clearing
during a presentation.

- [trussosw: thin/medium/thick and four ink tones](https://github.com/trussosw/tinyhand/commit/be15d1690a98abc0f178dfd930d522b7926a1b30):
  stroke-width factors 0.55 / 1 / 1.9; vivid, pastel, muted, deep;
  plus a very short fade option.
- [jorgenbruntveit: progressively fading ink trail](https://github.com/jorgenbruntveit/tinyhand/commit/f42633c68521a5aabf3ed364d3d24472acace00d):
  a line disappears from start to end instead of fading everywhere at once.
  A [follow-up change](https://github.com/jorgenbruntveit/tinyhand/commit/4d29768c1e)
  starts fading only after release.
- [AbdulRehman56: GNOME drawing tools and undo](https://github.com/AbdulRehman56/tinyhand/blob/6f890adae4e7d14f8dfa81a10f30de3677fdbb2d/linux/companion/tinyhand@abdulrehmankhan56/extension.js):
  `_undo()`, separate Point/Draw mode and toolbar actions in source.
- [Felitendo/waypointer](https://github.com/Felitendo/waypointer/blob/7d83196e43/src/settings.rs):
  individual ink color per pointer (`inks`) and switchable click ripples (`ripples`);
  the [raster renderer](https://github.com/Felitendo/waypointer/blob/7d83196e43/src/raster.rs)
  adds a soft glow effect.

**Context:** Basic colors and simple timed fading exist upstream; widths, tones,
segment-by-segment fading and undo are the more interesting additions.
**Effort:** low to medium. Start on Windows with color, width, “undo last stroke”
and “fade after N seconds”. Coordinates and arrowheads must remain consistent
with mixed DPI. Keep permanent drawings as an option.

### 3. Automatically straighten freehand shapes

**Benefit:** A rough circle or frame becomes a clean shape after drawing;
arrows can be built from a line and an arrowhead drawn afterwards.

[CatalystMonish: Snap Shapes](https://github.com/CatalystMonish/tinyhand/commit/d2db2444805f92a5d13654bfbd3029176ea10a36)
connects the renderer to a shared C99 recognizer.
The [ShapeKit interface](https://github.com/CatalystMonish/tinyhand/blob/2dd3c6503e/Sources/ShapeKit/include/shapekit.h)
lists line, arrow, circle, ellipse, triangle, square, rectangle, diamond, pentagon,
hexagon, star and heart. Unmatched strokes remain freehand. Two-stroke arrows
have their own recognition. There are [regression tests](https://github.com/CatalystMonish/tinyhand/blob/2dd3c6503e/Tests/shapes/shape_test.c).
[Arisaya1](https://github.com/Arisaya1/tinyhand/tree/916564a254/Sources/ShapeKit)
also contains a ShapeKit-based Windows port.

**Effort:** medium. No AI service needed: local geometric heuristics.
For C#, either port the algorithm or integrate C through P/Invoke; porting keeps
the existing Windows build chain simpler. Make it opt-in so handwriting or
deliberately irregular markings are not corrected. Start with line, circle and
rectangle rather than every decorative shape.

### 4. Spotlight and a quick highlight ring

- [AbdulRehman56: presentation toolkit](https://github.com/AbdulRehman56/tinyhand/blob/6f890adae4e7d14f8dfa81a10f30de3677fdbb2d/linux/companion/tinyhand@abdulrehmankhan56/extension.js):
  dimming spotlight circle, click ripples and optional keystroke display.
  Configuration and profiles are in
  [experimental.js](https://github.com/AbdulRehman56/tinyhand/blob/6f890adae4e7d14f8dfa81a10f30de3677fdbb2d/linux/companion/tinyhand@abdulrehmankhan56/experimental.js).
- [trussosw: Quick Ring](https://github.com/trussosw/tinyhand/commit/2c0c56298d82497f783c676d9da2c68fb95a1f74):
  hold the right mouse button and click left to place a ring quickly.

**Effort:** medium for spotlight, lower for a ring.
Keystroke display must be explicitly enabled: it can expose confidential input.
Check spotlight and drawings during screen sharing with multiple monitors.

### 5. Export a screenshot or drawing

- [charlesanim: Drawing Capture](https://github.com/charlesanim/tinyhand/commit/0b622244e05beccb15bf54dd9c478d885ccb9d89):
  `Capture.swift` renders drawings as PNG and processes a freehand region selection
  for screenshots. Empty drawings and undersized regions are explicitly rejected.
- [maxnetdigital: Snapshot and Record](https://github.com/maxnetdigital/tinyhand/commit/d30077f479a5acd9b7a03f17738791a46cec87c1):
  `Capture.swift` uses ScreenCaptureKit/`SCStream`, and the recorder uses a video
  writer; the toolbar and controller receive capture actions.

**Effort:** medium for PNG/clipboard, high for robust video recording.
Recommendation: “copy screenshot with annotations” or transparent PNG export
before a built-in recorder. Video recording overlaps substantially with OBS and
Windows tools and makes the small app much more complex.
The macOS capture code is a conceptual reference, not a Windows solution.

### 6. Hand gestures and additional graphic styles

[xynova: Gestures](https://github.com/xynova/tinyhand/commit/c7bbac616dde0decf97f3522445646a390bcdaf7)
adds a line-art hand and image variants for OK, shaka and middle finger. Holding
a shortcut changes only the hand image; the target point, size and stick remain
stable. Preloading and caching avoid file access on every switch.
The commit includes additional tests for gestures and custom hands.

[jorgenbruntveit: Outline Hand](https://github.com/jorgenbruntveit/tinyhand/tree/outline-hand)
contains a cartoon hand, cat paw and tap bursts that grow stronger on rapid
repeated clicks. The `pokey-style` branch adds a personal middle-finger style,
not necessarily suitable for every presentation.

**Effort:** low for additional ordinary images, medium for gestures including
hotkeys and calibration. Prioritize OK/thumbs-up as business-friendly variants.
Check artwork licensing and additional image size before reuse.
Custom hands themselves already exist upstream, but not in this Windows port.

### 7. Small usability and efficiency improvements

- [alcibiadesc: sound only for taps, not drawing](https://github.com/alcibiadesc/tinyhand/commit/d8ce59056b2bf65899c5c38d24aaebca0134a2f6):
  defers the click sound and skips it if the click becomes a stroke.
  **Low effort**, a useful detail for frequent use.
- [jorgenbruntveit: volume slider and three additional sounds](https://github.com/jorgenbruntveit/tinyhand/commit/833872bdea98c316f63088e57e4119ae68ad4723).
- [p4block: select a custom sound file](https://github.com/p4block/tinyhand/commit/5d91bec7ba12f02ead369d194deef5105356ff11).
  Windows must convert new formats to the existing PCM/queue model;
  preserve the working headset silence queue.
- [trussosw: let the timer sleep after three seconds without movement](https://github.com/trussosw/tinyhand/commit/491831508c062b125186ae1c111f775ff86781ab).
  Wake through mouse/button/modifier events, not just slow polling.
  **Medium effort** in the current Windows model. Render sleep must never disable
  the independent audio keep-alive; finish fade/tap/pose animations first.
- [MUL7ii: automatically step aside for capture tools](https://github.com/MUL7ii/tinyhand/blob/ea40fa0b2e/windows/src/main.rs):
  monitors the foreground and pauses for `ScreenClippingHost.exe`,
  `SnippingTool.exe` and `ScreenSketch.exe`. **Low to medium effort**, useful for
  Windows screenshots. Start with a switchable exception for known capture tools,
  not arbitrary process exclusions without a clear rule.
- [fduarte42: cache display-sized sprites and crop transparent margins](https://github.com/fduarte42/tinyhand/commit/2d8910dd6168f1fd2c40ffa8c5ef685c59d0eb50).
  A concrete GNOME/Cairo fix; WPF works differently. Measure first rather than
  promising equivalent performance gains without verification. This optimizes
  rendering, not automatically the size of the distributed Windows EXE.
- [tumaranja: adjustable tap/stick swing range](https://github.com/tumaranja/tinyhand/commit/b21dc52a6d6646f9608f81bea1e4c271bb1f3cff).
  For Windows, first resolve the still-missing separate stick segment.
- [ulentini: audio freeze during screen sharing](https://github.com/ulentini/tinyhand/commit/01d7b76e77):
  moves audio work to its own queue and rebuilds the engine after device changes.
  A macOS fix, not a replacement for the confirmed Windows headset fix.

### 8. Local automation and presenter profiles

[0xdhrv: JSON Control Bridge](https://github.com/0xdhrv/tinyhand/commit/602e3f4ad3c0436bd06fa7a97b318246e3bdff2a)
goes beyond existing URL commands: query state and styles, apply configuration as
a validated batch and set favorites.
The [protocol](https://github.com/0xdhrv/tinyhand/blob/602e3f4ad3c0436bd06fa7a97b318246e3bdff2a/docs/control-protocol.md)
and source specify a private Unix socket, peer UID checks, size limits and timeouts.

**Benefit:** A Stream Deck, launcher or script sets “Demo: laser, blue, no sound”
with one action. **Effort:** medium. On Windows, use a named pipe restricted to
the current user, or initially simple CLI commands. No unprotected TCP/HTTP endpoint
is needed. The existing single-instance behavior must forward commands to the
running instance instead of discarding them.

## Four larger rewrites in detail

### QuiteHandy: numbered stamps, whiteboards and magnifier

Examined revision:
[`121d9d4edd12a05e8bdc46fc7adf0eae0420fd9c`](https://github.com/esreverse/quitehandy/tree/121d9d4edd12a05e8bdc46fc7adf0eae0420fd9c).

- **Numbered stamps 1–9** to mark a sequence; resize, move and renumber them.
  Particularly useful for tutorials: “first here, then there”.
- **Width 1–40 pt and opacity 10–100%** independently; also rainbow ink with
  a continuous color progression across multiple strokes.
- **Undo/redo** for ink and whiteboards with a shared, bounded history.
- **Movable/resizable whiteboards** with light/dark surfaces, their own drawings
  and PNG/clipboard export.
- **Spotlight and a true zoom magnifier:** dimming without capture, magnification
  with ScreenCaptureKit and exclusion of the app's own windows.

Source: [Ink.swift](https://github.com/esreverse/quitehandy/blob/121d9d4edd12a05e8bdc46fc7adf0eae0420fd9c/Sources/QuiteHandy/Ink.swift),
[Preferences.swift](https://github.com/esreverse/quitehandy/blob/121d9d4edd12a05e8bdc46fc7adf0eae0420fd9c/Sources/QuiteHandy/Preferences.swift),
[History.swift](https://github.com/esreverse/quitehandy/blob/121d9d4edd12a05e8bdc46fc7adf0eae0420fd9c/Sources/QuiteHandy/History.swift),
[Whiteboard.swift](https://github.com/esreverse/quitehandy/blob/121d9d4edd12a05e8bdc46fc7adf0eae0420fd9c/Sources/QuiteHandy/Whiteboard.swift),
[OverlayController.swift](https://github.com/esreverse/quitehandy/blob/121d9d4edd12a05e8bdc46fc7adf0eae0420fd9c/Sources/QuiteHandy/OverlayController.swift).

**Caution:** Arrows are disabled in the examined revision; documentation is partly
contradictory. Do not adopt the whole ink renderer as an “improved version”.
For Windows, numbered stamps and undo are particularly attractive small projects;
whiteboards and the capture-based magnifier are larger features to plan separately.

### Look Here Please: full annotation editor and Stream Deck

Examined revision:
[`58e1954279fbecf6aa80096ded1fe4a827c84eb5`](https://github.com/austinpresley/please-look-here/tree/58e1954279fbecf6aa80096ded1fe4a827c84eb5).

- **Object-based annotations:** pen, rectangle, arrow, text, number, eraser and
  highlighter; hit-testing, editing and undo/redo rather than just a stroke list.
- **Radial tool menu at the cursor** as an alternative to a fixed toolbar.
- **Stable documents per display:** overlay geometry and stored annotations are
  separate. Interesting because this Windows port currently clears on monitor changes.
- **Stream Deck plugin, CLI, AppleScript and secured local JSON IPC**, with state
  responses instead of blind toggling.
- **Presentation presets, validated backups and recovery** for settings, hands
  and their associated image variants.

Source: [DrawingModel.swift](https://github.com/austinpresley/please-look-here/blob/58e1954279fbecf6aa80096ded1fe4a827c84eb5/Sources/TinyHand/DrawingModel.swift),
[AnnotationResponder.swift](https://github.com/austinpresley/please-look-here/blob/58e1954279fbecf6aa80096ded1fe4a827c84eb5/Sources/TinyHand/AnnotationResponder.swift),
[RadialMenu.swift](https://github.com/austinpresley/please-look-here/blob/58e1954279fbecf6aa80096ded1fe4a827c84eb5/Sources/TinyHand/RadialMenu.swift),
[source directory](https://github.com/austinpresley/please-look-here/tree/58e1954279fbecf6aa80096ded1fe4a827c84eb5/Sources/TinyHand),
[tests](https://github.com/austinpresley/please-look-here/tree/58e1954279fbecf6aa80096ded1fe4a827c84eb5/Tests).

**Caution:** The README and project status do not consistently match the actual
public source. Existing test files and documented local success claims are not
release quality verified by this research. Interesting as an architectural
reference; for TinyHand Windows, initially select text/number/highlighter rather
than immediately rebuilding an entire editor.

### PlayPointer: concrete C# model for Control and Draw

Examined revision:
[`56cc9842d9756ba3d415ca7e6e0a579f8ec673de`](https://github.com/omiusgm/playpointer/tree/56cc9842d9756ba3d415ca7e6e0a579f8ec673de).

- Separate **.NET 10 Windows port**, but **WinForms/native layered windows**, not WPF.
- **Off / Control / Draw:** In Control mode, the hand and drawings remain visible
  and normal input reaches apps. Draw creates separate input windows; Alt hides
  them temporarily. Windows uses Raw Input here instead of global mouse hooks.
- **Five tap intensities and bounded amplification on rapid click sequences**.
- **PNG import wizard** with a local copy, header/CRC checks, file size up to
  16 MB and image dimensions of 8–4096 px; a prompt template, not an integrated
  image-generation API.
- Larger animated pointer catalog, such as Bongo Cat/magic-wand-style effects.

Source: [Program.cs](https://github.com/omiusgm/playpointer/blob/56cc9842d9756ba3d415ca7e6e0a579f8ec673de/windows/Program.cs),
[Layers.cs](https://github.com/omiusgm/playpointer/blob/56cc9842d9756ba3d415ca7e6e0a579f8ec673de/windows/Layers.cs),
[TapImpact.cs](https://github.com/omiusgm/playpointer/blob/56cc9842d9756ba3d415ca7e6e0a579f8ec673de/windows/TapImpact.cs),
[Windows source](https://github.com/omiusgm/playpointer/tree/56cc9842d9756ba3d415ca7e6e0a579f8ec673de/windows).

**Caution:** The owner still describes Windows runtime behavior as needing testing;
compilation/packaging does not equal successful use. Do not reuse character/branded
assets without checking rights. The different audio approach does not demonstrably
meet our queue/headset keep-alive requirements: retain the existing audio code.
The Control/Draw model, on the other hand, is a particularly suitable inspiration.

### AiHand: an annotated screenshot as a task for a coding agent

Examined revision:
[`949b8ce91b28196061ea6babcb4a3dd1bb599f6c`](https://github.com/rjiang-bb/aihand/tree/949b8ce91b28196061ea6babcb4a3dd1bb599f6c).

- Freehand, rectangle, arrow and text with width and undo.
- Save, copy or send a **screenshot with annotations and a comment** to Cursor
  Agent, Claude Code or Codex.
- Region video with a selection/recording/preview state machine.
- Extensive agent frontend: sessions, follow-ups, attachments, models,
  local/SSH workspaces and Git worktrees.

Source: [WindowCapture.swift](https://github.com/rjiang-bb/aihand/blob/949b8ce91b28196061ea6babcb4a3dd1bb599f6c/Sources/AiHand/WindowCapture.swift),
[Ink.swift](https://github.com/rjiang-bb/aihand/blob/949b8ce91b28196061ea6babcb4a3dd1bb599f6c/Sources/AiHand/Ink.swift),
[AgentCLI.swift](https://github.com/rjiang-bb/aihand/blob/949b8ce91b28196061ea6babcb4a3dd1bb599f6c/Sources/AiHand/AgentCLI.swift),
[source directory](https://github.com/rjiang-bb/aihand/tree/949b8ce91b28196061ea6babcb4a3dd1bb599f6c/Sources/AiHand).

**Clear security boundary:** Source includes Cursor `--trust`/`--force`, Claude
`--dangerously-skip-permissions` and Codex
`--dangerously-bypass-approvals-and-sandbox`. Cursor Force and Claude Bypass are
defaults in the examined revision. As a whole, this is not a harmless screenshot
extension; it would significantly expand product scope and privileges. Adopting
screenshot/clipboard functionality in isolation would be more sensible than an
integrated agent client with these defaults.

### Windows distribution: smaller package and authenticated updates

Two more practical findings, independent of visible pointer effects:

- **pietro-santoro optimizes embedded images:** The 36 Windows hand PNGs total
  **5.62 MiB** in the examined Git revision. The guide describes reducing them to
  1024 px high, and the workflow enables `EnableCompressionInSingleFile=true`.
  This is a relevant concrete reference for our roughly 62 MiB of hand images,
  without giving up portable self-contained distribution. Do not present it as
  a measured EXE size reduction; check appearance at large sizes and high DPI.
  Sources: [hand assets](https://github.com/pietro-santoro/tinyhand/tree/e35f5dcc33/windows/TinyHand/Assets/Hands),
  [guide](https://github.com/pietro-santoro/tinyhand/blob/e35f5dcc33/windows/README.md),
  [workflow](https://github.com/pietro-santoro/tinyhand/blob/e35f5dcc33/.github/workflows/windows.yml).
- **Arisaya1 includes a Windows updater with a signed manifest:** ECDSA P-256,
  BCrypt signature verification, SHA-256/size checks and ZIP member/expanded-size
  validation are actually present in source; dedicated update tests exist.
  Sources: [update.cpp](https://github.com/Arisaya1/tinyhand/blob/916564a254/windows/src/update.cpp),
  [release signing](https://github.com/Arisaya1/tinyhand/blob/916564a254/windows/scripts/release_sign.py),
  [tests](https://github.com/Arisaya1/tinyhand/blob/916564a254/Tests/windows/update_test.cpp).
  **Not the same as Windows code signing:** A signed update manifest does not
  remove SmartScreen warnings for an unsigned EXE. Update/rollback/key management
  and locked portable EXEs are a separate, larger project.

## Inventory of direct forks with additional commits

Links point to the relevant branches. Summaries are based on history, file diffs
and targeted source checks for highlighted features. A port entry is explicitly
not a quality judgment on the entire program.

| Fork | Relevant changes / context |
|---|---|
| [0xdhrv](https://github.com/0xdhrv/tinyhand/tree/f/native-control-bridge) | Local JSON automation; see above. |
| [AbdulRehman56](https://github.com/AbdulRehman56/tinyhand/tree/linux-port) | Rust/X11 and GNOME extension; Point/Draw, gallery/favorites, undo, spotlight, profiles; toolkit partly described as experimental. |
| [alcibiadesc](https://github.com/alcibiadesc/tinyhand/branches) | `quiet-strokes`, `shift-tap-mouse-mode`, `hide-toolbar-option`: sound only on taps, simple Shift mode switch, hide toolbar. Look beyond `main`. |
| [Arisaya1](https://github.com/Arisaya1/tinyhand/tree/windows-port) | Native C++ Windows port and ShapeKit; CI/MinGW. |
| [austinpresley/please-look-here](https://github.com/austinpresley/please-look-here) | Larger independent rewrite; detailed examination above. |
| [Ayas4y](https://github.com/Ayas4y/tinyhand) | Extensive Rust rebuild for macOS/Windows, separate settings app and update feed. Much of it ports existing upstream behavior. |
| [CatalystMonish](https://github.com/CatalystMonish/tinyhand/tree/windows-port-and-shape-snapping) | C++ Windows port and geometric shape recognition with tests. |
| [charlesanim](https://github.com/charlesanim/tinyhand) | Native pointer interaction and drawing/screenshot capture. |
| [esreverse/quitehandy](https://github.com/esreverse/quitehandy) | Larger independent rewrite; detailed examination above. |
| [Felitendo/waypointer](https://github.com/Felitendo/waypointer) | Rust/Wayland rebuild; colors per pointer, click ring, glow, multilingual UI, reorderable toolbar, Linux packages. |
| [gitrandynow](https://github.com/gitrandynow/tinyhand/tree/remove-sparkle) | Removes Sparkle/auto-updater. Currently no new Windows feature: this port has no updater yet. |
| [guidomariadifiore](https://github.com/guidomariadifiore/tinyhand-windows) | Native Windows port, Alt interaction and sound/render/compiler fixes. |
| [hetrodoo](https://github.com/hetrodoo/tinyhand/tree/windows-port) | Windows port; primarily platform work. |
| [jorgenbruntveit](https://github.com/jorgenbruntveit/tinyhand/branches) | Many separate ideas: Pointer Mode, trail fade, volume, cartoon/paw, tap bursts, menu instead of toolbar; also macOS Spaces/menu-edge/password-dialog fixes. |
| [lithium91](https://github.com/lithium91/tinyhand) | Python/GTK3 Linux port with DEB/RPM and GNOME panel/dock fix. |
| [m1ga](https://github.com/m1ga/tinyhand/tree/linux-kde-port) | Qt port for KDE Plasma; primarily platform work. |
| [maxnetdigital](https://github.com/maxnetdigital/tinyhand) | Snapshot, screen recording and additional access to settings. |
| [MUL7ii](https://github.com/MUL7ii/tinyhand/tree/windows-port) | Extensive Rust Windows port: toolbar, WebView2 settings, WASAPI, DPI/monitors, click/drag hybrid, capture-tool exceptions, rendering/hook fixes. Good Windows reference, but different architecture. |
| [n0stalgia-mac](https://github.com/n0stalgia-mac/tinyhand/tree/classic-mac) | Classic Mac/System 7.1 port; offscreen rendering and Draw switch. A curiosity, with little direct Windows benefit. |
| [omiusgm/playpointer](https://github.com/omiusgm/playpointer) | Larger independent rewrite; detailed examination above. |
| [p4block](https://github.com/p4block/tinyhand) | Python Wayland port/Arch packaging, six sound previews and custom sound file. |
| [pietro-santoro](https://github.com/pietro-santoro/tinyhand/tree/windows-port) | Native Windows port, CI/self-tests, transform/hook-priority fixes. |
| [rjiang-bb/aihand](https://github.com/rjiang-bb/aihand) | Larger independent rewrite; detailed examination above. |
| [sajeeldigitali](https://github.com/sajeeldigitali/tinyhand/tree/windows) | Windows 11 port with CI/self-test; selectable key for real clicks, documented window/input limitations. |
| [sergvax](https://github.com/sergvax/tinyhand) | Click-through and independently switchable drawing. |
| [sud868](https://github.com/sud868/tinyhand/tree/feat/gnome-extension) | GNOME Shell extension instead of macOS app. |
| [ThatJoeLopez](https://github.com/ThatJoeLopez/tinyhand_Windows/tree/claude/stoic-ramanujan-mqiht2) | Windows port including custom PNG decoding and crash/input fixes; newer fixes only on a side branch. |
| [tran-huy-phuc](https://github.com/tran-huy-phuc/tinyhand/tree/windows-tauri) | Rust/Tauri, installer, Windows/macOS; pen width, toolbar hints, drawings per monitor, stuck Alt/Ctrl fix. |
| [trussosw](https://github.com/trussosw/tinyhand/branches) | Separate branches for width/tones, ring, real-click and idle sleep. |
| [tumaranja](https://github.com/tumaranja/tinyhand/tree/swing-range) | Swing range; much of the extra history on `main` concerns demos and slider refinements. |
| [ulentini](https://github.com/ulentini/tinyhand/tree/fix/tap-sound-freeze) | macOS audio-freeze fix during screen sharing. |
| [xynova](https://github.com/xynova/tinyhand) | Hand gestures, line-art hand, gesture assets/tests and UI help. |
| [zebrajaeger](https://github.com/zebrajaeger/tinyhand-windows) | This C#/WPF Windows port; evaluation baseline, not an external idea. |

Additionally, [fduarte42](https://github.com/fduarte42/tinyhand/tree/fix/gnome-hand-render-perf)
is a nested fork with two GNOME rendering optimizations relative to
AbdulRehman56's `linux-port`.

### The 47 direct forks without additional commits on visible branches

Repository names as recorded in the fork list/remote inspection:

```text
albarian/tinyhand                  Aleeyoo/tinyhand
alexandrethsilva/tinyhand          amba5555/tinyhandClick
apaww/tinyhand-fedora              caynine9/tinyhand
ChatGPTBuddy/tinyhand              devpeerapong/tinyhand
gabwag/tinyhand                    goose20090/tinyhand
gundusrikanth/tinyhand             hansdoebel/tinyhand
haxllo/tinyhand                    hotsync/tinyhand
hoxlo/tinyhand                     iamdooboy/tinyhand
ITGalaxyMaster/tinyhand            its-harsshhh/tinyhand
itsharex/tinyhand                  jawond/tinyhand
Lohrey/tinyhand                    lucasea777/tinyhand
luqmanoop/tinyhand                 malow005/tinyhand
marsian83/tinyhand                 mathewharwich/tinyhand
mayurnimavatg/tinyhand              Mhewski/tinyhand_fork
Mooka-Mooka/tinyhand               nkemjp2/tinyhand
Pikuseru/tinyhand                  pparker13122/tinyhand
reachforaryan/tinyhand             richardvinc/tinyhand
rom3r4/tinyhand                    screenfluent/tinyhand
secondpathstudio/tinyhand_macos    Shoaib19/tinyhand
ShoNuffWrites/tinyhand             suryanshsingh2001/tinyhand
thirdPolarity/tinyhand             trinhhaox/tinyhand
Ubaton/tinyhand                    umairbari/tinyhand
vasinl124/tinyhand                 wajahat-alip/tinyhand
wanderberi/tinyhand
```

This does not rule out private, deleted or unpublished branch work. An unusual
push date alone is also not evidence of original app changes.

## Preliminary priorities for this Windows port

1. **Quick practical improvements:** ink color/width, undo, optional fading,
   numbered stamps and sound only on actual taps.
2. **Biggest usability gain:** clear Point/Draw mode, followed by a small toolbar.
   Do not defer normal clicking until a complicated hybrid model is implemented.
3. **Particularly interesting extension:** opt-in shape recognition for line/circle/rectangle.
4. **For training and presentations:** spotlight and screenshots with annotations.
5. **As needed:** gestures, custom hands/sounds, volume and Stream Deck/CLI profiles.
6. **Measure separately:** rendering/idle optimization. Do not adopt recorders,
   AI features or complete architecture changes merely because fork diffs are large.
7. **For smaller downloads:** try optimized hand images and single-file compression
   without abandoning the bundled .NET runtime or image quality without verification.

No commits, pushes or releases were performed as part of the research itself.
Source links and the Git snapshot are time-specific; moving branch links may change later.
