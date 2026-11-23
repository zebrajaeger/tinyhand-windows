<img src="./.github/logo.png" width="80" alt="TinyHand logo" />

# TinyHand for Windows

[![Latest Windows release](https://img.shields.io/github/v/release/zebrajaeger/tinyhand-windows?filter=windows-v*&display_name=tag&sort=date)](https://github.com/zebrajaeger/tinyhand-windows/releases)
[![Platform: Windows 10/11 x64](https://img.shields.io/badge/platform-Windows%2010%2F11%20x64-0078D4)](https://github.com/zebrajaeger/tinyhand-windows/releases)
[![License: MIT](https://img.shields.io/github/license/zebrajaeger/tinyhand-windows)](./LICENSE)

A tiny hand that points at things on your Windows screen. Highlight a detail,
draw on the desktop, or add arrows during a presentation or screen recording.

This is a native Windows port and fork of **[TinyHand by nilbuild](https://github.com/nilbuild/tinyhand)**.
The original macOS app, concept, hand artwork and sounds come from that project.
For the macOS version, visit [nilbuild/tinyhand](https://github.com/nilbuild/tinyhand)
or [tinyhand.app](https://tinyhand.app).

[![Download for Windows](https://img.shields.io/badge/Download_for_Windows-0078D4?style=for-the-badge)](https://github.com/zebrajaeger/tinyhand-windows/releases)

## Download and run

1. Open the [Windows releases](https://github.com/zebrajaeger/tinyhand-windows/releases).
2. Download `TinyHand-Windows-<version>-win-x64.zip` from the release assets.
3. Extract the ZIP and run **`TinyHand.Windows.exe`**.

Requires **Windows 10/11 x64**. The app is portable: no installer, administrator
rights or separate .NET installation are needed. A SHA-256 checksum is included
with each Windows release. GitHub's automatic source-code archives do not contain
the executable.

The application is **unsigned**, so Windows SmartScreen may show a warning.

After launch, look for the hand icon in the system tray, possibly under the hidden
icons. The pointer starts **off**. The tray icon follows your selected hand style;
in laser mode it shows the lavender hand. The EXE uses the cookie hand as its icon.

## Use

| Action | Result |
|---|---|
| **Ctrl+Alt+H** or left-click the tray icon | Turn the pointer on or off |
| Right-click the tray icon | Choose a hand or laser, pointer size, tap sound, or quit |
| Left-click while the pointer is on | Show a tap effect and optionally play a sound |
| Hold the left mouse button and drag | Draw on the screen |
| Hold **Shift when releasing** the left mouse button | Add an arrowhead |
| **Ctrl+Alt+X** | Clear the drawing |
| **Esc** | Clear the drawing and turn the pointer off |

**While the pointer is on, the overlay blocks clicks and scrolling in applications
underneath.** Turn it off with **Esc** or **Ctrl+Alt+H** before using other apps or
opening the tray menu. Ordinary keyboard input is not blocked and can still reach
the previously focused application.

## Features

- Built-in hand styles and a laser dot.
- Dynamic hand tilt with smooth grip tracking; the fingertip stays on the cursor.
- Screen drawing, arrows and an animated tap indicator.
- Three pointer sizes and six selectable tap sounds with previews.
- Saved hand, size and sound preferences. Tap sounds are off by default.
- An overlay on each monitor.
- A silent audio keep-alive while the pointer and tap sounds are enabled, to avoid
  losing short sounds when a headset wakes up between clicks.

Settings are stored in `%USERPROFILE%\.config\TinyHand\settings.json`.
Audio diagnostics are stored alongside them in `audio.log`.
The pointer always starts off, even when preferences are restored.

## Current limitations

This Windows port does not yet include all features of the original macOS app:

- No installer, code signing, auto-updater or built-in autostart.
- No floating toolbar, full settings window, custom hands or URL automation.
- No click-through mode or separately extended arm/stick segment.
- Monitor-layout changes clear the current drawing. Test mixed-DPI setups before
  an important presentation.
- Secure desktops such as UAC prompts are not covered; exclusive fullscreen and
  some protected applications may hide the overlay.

When screen sharing, **share the entire display**. Sharing only an application
window usually does not include TinyHand's separate overlay.

## Build and test

Development requires **Windows and the .NET 10 SDK**. The Windows app uses C# and
WPF; the original Swift/macOS sources remain in this repository.

```powershell
dotnet build windows/TinyHand.Windows -c Release
dotnet run --project windows/TinyHand.Windows.Tests -c Release
./windows/publish.ps1
```

The portable output is `dist/windows/win-x64/TinyHand.Windows.exe`.
Close any running copy from that directory before publishing again.
Tests briefly open overlay windows on the desktop. The optional audible regression
test requires a working audio device:

```powershell
dotnet run --project windows/TinyHand.Windows.Tests -c Release -- --audio-regression
```

See the [Windows development guide](windows/README.md) for audio diagnostics,
optional ARM64 builds and further implementation details. Published releases
currently provide **x64** builds only.

## Publishing releases

The [Windows Release workflow](.github/workflows/windows-release.yml) runs when a
tag matching `windows-vMAJOR.MINOR.PATCH` is pushed. It tests and builds the app,
then publishes a portable ZIP and SHA-256 checksum to this fork's GitHub Releases.
Tags with a suffix, such as `windows-v0.2.0-beta.1`, create a pre-release.
Windows release tags are separate from the original macOS tags (`v*`).

For setup and tagging instructions, see [GitHub releases in the Windows guide](windows/README.md#github-releases).

## Credits and license

Based on [nilbuild/tinyhand](https://github.com/nilbuild/tinyhand), the original
TinyHand project by **Kamran Ahmed**. Thanks to the original project and its
contributors for the app concept, artwork, sounds and macOS implementation.

This fork remains licensed under the **MIT License**. See [LICENSE](LICENSE) for
the terms and original copyright notice.
