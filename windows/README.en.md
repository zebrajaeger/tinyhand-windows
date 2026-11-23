# TinyHand for Windows – Development and Usage Guide

[Deutsch](README.md) | **English** · [Project overview](../README.md)

Native Windows port using C#/.NET 10 and WPF. The macOS code remains unchanged.
The existing hand artwork is embedded as resources during the build.

## Launch

After publishing, the portable output is
`dist/windows/win-x64/TinyHand.Windows.exe`. It requires neither a separate .NET
runtime installation nor administrator rights. On first launch, .NET extracts
native components into its temporary cache directory.

Target platform: Windows 10/11 x64; ARM64 can be published separately.
The EXE is unsigned; Windows may display a SmartScreen warning.

After launch, TinyHand appears in the system tray, possibly among the hidden icons.
The pointer starts off. The tray icon displays the selected hand without its stick
and updates when the style changes. Laser mode uses the lavender hand as the app icon.
The EXE uses the cookie hand as its fixed application icon.

In the tray menu, **Mit Windows starten** (start with Windows) enables launch at
sign-in for the current user without administrator rights. It is off by default.
It stores the current quoted EXE path in the `TinyHand.Windows` value under
`HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run`.
Turning it off removes only that value. The pointer still starts off at sign-in.
Keep the portable EXE in a permanent folder; after moving it, disable and re-enable
the option, and disable it before deleting the app. Windows Task Manager can also
disable startup independently of this registry entry.

## Controls

- **Ctrl+Alt+H** or left-click the tray icon: toggle the pointer.
- **Right-click the tray icon**: choose a hand/laser, clear drawings or quit.
- **Left-click in pointer mode**: show a tap effect.
- **Hold the left mouse button and drag**: draw.
- **Hold Shift when releasing**: add an arrowhead.
- **Ctrl+Alt+X**: clear the drawing.
- **Esc**: clear the drawing and exit pointer mode.

The tray menu offers three pointer sizes and six tap sounds. Selecting a sound
plays a preview; **Tippgeräusch** (tap sound) enables audio on clicks (off by default).

Hand style, size and sound preferences are stored in
`%USERPROFILE%/.config/TinyHand/settings.json`. The pointer still starts off on the
next launch. Missing or corrupt settings fall back to safe defaults.

In active mode, a transparent overlay blocks clicks and scrolling in normal
desktop applications. Press **Esc** or **Ctrl+Alt+H** before using other apps or the
tray menu. Keyboard input is not intercepted apart from registered hotkeys: text
can still reach the previously active application. Occupied hotkeys are reported
at startup.

An overlay is created for each monitor; drawing points are stored in physical
desktop coordinates. Monitor-layout changes reset the drawing. Test differing
DPI scales on the actual system before an important presentation.

## Build and test

Requires Windows with the .NET 10 SDK. Run commands from the repository root.
Before publishing, close any EXE running from the output directory.

```powershell
dotnet build windows/TinyHand.Windows -c Release
dotnet run --project windows/TinyHand.Windows.Tests -c Release
dotnet run --project windows/TinyHand.Windows.Tests -c Release -- --audio-regression
./windows/publish.ps1
# Optional for Windows on ARM:
./windows/publish.ps1 -Runtime win-arm64
```

Tests require no external test packages. They check arrow geometry, embedded
images, real overlay windows, rendering and state cleanup. Overlays briefly appear
on the current desktop. The optional `--audio-regression` test requires a working
audio device.

The publish script also runs the finished portable EXE to verify that all six
embedded WAV files can be found and decoded. Audio diagnostics are written to
`%USERPROFILE%/.config/TinyHand/audio.log`.

The audio test plays all six sounds and checks that subsequent clicks do not cut
off active clips. Playback keeps the audio device open and queues complete clips.
Rapid click bursts queue at most four clips; additional click sounds are skipped
to avoid a long audible backlog.

While pointer mode and tap sounds are enabled, the same audio stream is fed with
digital silence between clicks. This aims to prevent short sounds from being lost
when a headset wakes up. Disabling the feature stops silent playback and releases
the audio device once queued clips finish. The audio test also checks clicks after
three-second pauses.

## GitHub releases

The workflow `.github/workflows/windows-release.yml` automatically publishes a
Windows release when a `windows-vMAJOR.MINOR.PATCH` tag is pushed. It runs the
Windows regression tests, builds the portable x64 version, and uploads a ZIP and
SHA-256 checksum. The ZIP excludes debug symbols. The finished EXE is also checked
for embedded audio files. The audible `--audio-regression` test remains local
because CI runners do not provide a reliable audio device.

After committing and pushing the intended changes to `main`, for example:

```bash
git tag -a windows-v0.1.0 -m "TinyHand for Windows 0.1.0"
git push origin windows-v0.1.0
```

Tags with a suffix, such as `windows-v0.1.0-beta.1`, create a pre-release. Windows
tags are independent of the original macOS tags (`v*`). GitHub Actions must be
enabled in the fork; no additional secrets are required. The workflow uses the
automatic `GITHUB_TOKEN`, with write permissions only in the publishing job.
A failed workflow can be rerun: existing release assets are replaced, while
manually edited release notes are preserved.

If the tag build did not start, open **Actions → Windows Release → Run workflow**
on `main` and enter the existing Windows tag in the `tag` field. The build uses
that tag's source code. Keep existing tags unchanged; use a new version number
for new changes. Publication is only complete once both ZIP and checksum are
actually attached to the release.

## Not yet included / limitations

- Auto-updater, installer and code signing.
- Floating toolbar and full settings window.
- URL automation, custom hand artwork and click-through mode.
- The hand tilts dynamically with a smoothly tracked grip; its fingertip stays on
  the cursor. A separately extended arm/stick segment like the macOS version is
  not yet included.

No overlay on secure Windows desktops (UAC/sign-in); exclusive fullscreen and
some protected applications may hide the overlay.

When screen sharing, share the entire display: sharing only an application window
usually does not include the separate overlay.
