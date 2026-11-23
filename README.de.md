<img src="./.github/logo.png" width="80" alt="TinyHand-Logo" />

# TinyHand für Windows

[English](README.md) | **Deutsch**

[![Aktuelles Windows-Release](https://img.shields.io/github/v/release/zebrajaeger/tinyhand-windows?filter=windows-v*&display_name=tag&sort=date)](https://github.com/zebrajaeger/tinyhand-windows/releases)
[![Plattform: Windows 10/11 x64](https://img.shields.io/badge/platform-Windows%2010%2F11%20x64-0078D4)](https://github.com/zebrajaeger/tinyhand-windows/releases)
[![Lizenz: MIT](https://img.shields.io/github/license/zebrajaeger/tinyhand-windows)](./LICENSE)

Eine kleine Hand, die auf Dinge auf deinem Windows-Bildschirm zeigt. Hebe Details
hervor, zeichne auf dem Desktop oder füge bei Präsentationen und Bildschirmaufnahmen
Pfeile hinzu.

Dies ist eine native Windows-Portierung und ein Fork von
**[TinyHand von nilbuild](https://github.com/nilbuild/tinyhand)**.
Die ursprüngliche macOS-App, das Konzept, die Handgrafiken und die Geräusche stammen
aus diesem Projekt. Die macOS-Version findest du unter
[nilbuild/tinyhand](https://github.com/nilbuild/tinyhand) oder [tinyhand.app](https://tinyhand.app).

[![Download für Windows](https://img.shields.io/badge/Download_f%C3%BCr_Windows-0078D4?style=for-the-badge)](https://github.com/zebrajaeger/tinyhand-windows/releases)

## Herunterladen und starten

1. Öffne die [Windows-Releases](https://github.com/zebrajaeger/tinyhand-windows/releases).
2. Lade `TinyHand-Windows-<version>-win-x64.zip` aus den Release-Dateien herunter.
3. Entpacke das ZIP und starte **`TinyHand.Windows.exe`**.

Voraussetzung: **Windows 10/11 x64**. Die App ist portabel: Es werden weder ein
Installer noch Administratorrechte oder eine separate .NET-Installation benötigt.
Jedem Windows-Release liegt eine SHA-256-Prüfsumme bei. Die automatisch von GitHub
bereitgestellten Quellcodearchive enthalten keine ausführbare App.

Die Anwendung ist **nicht signiert**; Windows SmartScreen kann eine Warnung anzeigen.

Nach dem Start findest du das Handsymbol im Infobereich der Taskleiste, eventuell
unter den ausgeblendeten Symbolen. Der Zeiger ist zunächst **ausgeschaltet**.
Das Tray-Symbol zeigt den gewählten Handstil; im Laser-Modus erscheint die
lavender-Hand. Die EXE verwendet die Cookie-Hand als Symbol.

## Bedienung

| Aktion | Ergebnis |
|---|---|
| **Strg+Alt+H** oder Linksklick auf das Tray-Symbol | Zeiger ein- oder ausschalten |
| Rechtsklick auf das Tray-Symbol | Hand oder Laser, Größe und Tippgeräusch wählen oder beenden |
| Linksklick bei aktivem Zeiger | Tipp-Effekt anzeigen und optional Geräusch abspielen |
| Linke Maustaste halten und ziehen | Auf dem Bildschirm zeichnen |
| **Umschalt beim Loslassen** der linken Maustaste halten | Pfeilspitze hinzufügen |
| **Strg+Alt+X** | Zeichnung löschen |
| **Esc** | Zeichnung löschen und Zeiger ausschalten |

**Bei aktivem Zeiger blockiert das Overlay Mausklicks und Scrollen in den darunter
liegenden Anwendungen.** Schalte es mit **Esc** oder **Strg+Alt+H** aus, bevor du
andere Apps oder das Tray-Menü bedienst. Normale Tastatureingaben werden nicht
blockiert und können weiterhin die zuvor aktive Anwendung erreichen.

## Funktionen

- Integrierte Handstile und ein Laserpunkt.
- Dynamische Handneigung mit sanft nachgeführtem Griffpunkt; die Fingerspitze bleibt am Cursor.
- Bildschirmzeichnungen, Pfeile und ein animierter Tipp-Effekt.
- Drei Zeigergrößen und sechs wählbare Tippgeräusche mit Vorschau.
- Gespeicherte Einstellungen für Hand, Größe und Sound. Tippgeräusche sind standardmäßig aus.
- Optional **Mit Windows starten** im Tray-Menü: für den aktuellen Benutzer,
  ohne Administratorrechte. Der Zeiger bleibt beim Start aus.
- Ein Overlay pro Monitor.
- Digitale Stille zwischen Klicks bei aktiviertem Zeiger und Sound, damit kurze
  Geräusche beim Aufwachen eines Headsets nicht verloren gehen.

Einstellungen liegen unter `%USERPROFILE%\.config\TinyHand\settings.json`.
Audio-Diagnosen werden daneben in `audio.log` gespeichert.
Der Zeiger startet immer ausgeschaltet, auch bei wiederhergestellten Einstellungen.

Autostart registriert den aktuellen EXE-Speicherort für deinen Windows-Benutzer.
Lege die portable App in einem dauerhaften Ordner statt in einem temporären
Entpackverzeichnis ab. Nach dem Verschieben **Mit Windows starten** am neuen
Speicherort aus- und wieder einschalten; vor dem Löschen der App ausschalten.
Der Windows-Task-Manager kann Autostart zusätzlich unabhängig vom Tray-Schalter deaktivieren.

## Aktuelle Einschränkungen

Diese Windows-Portierung enthält noch nicht alle Funktionen der ursprünglichen macOS-App:

- Kein Installer, keine Codesignierung und kein Auto-Updater.
- Keine schwebende Werkzeugleiste, kein vollständiges Einstellungsfenster,
  keine eigenen Handgrafiken und keine URL-Automatisierung.
- Kein Durchklickmodus und kein separat verlängertes Arm-/Stabsegment.
- Änderungen der Monitoranordnung löschen die Zeichnung. Unterschiedliche
  DPI-Skalierungen vor wichtigen Präsentationen testen.
- Sichere Desktops wie UAC-Abfragen werden nicht überlagert; exklusives Vollbild
  und manche geschützten Anwendungen können das Overlay verdecken.

Bei Bildschirmfreigaben **die gesamte Anzeige teilen**. Die Freigabe eines einzelnen
Anwendungsfensters enthält das separate TinyHand-Overlay normalerweise nicht.

## Bauen und testen

Die Entwicklung benötigt **Windows und das .NET 10 SDK**. Die Windows-App nutzt
C# und WPF; der ursprüngliche Swift/macOS-Quellcode bleibt im Repository erhalten.

```powershell
dotnet build windows/TinyHand.Windows -c Release
dotnet run --project windows/TinyHand.Windows.Tests -c Release
./windows/publish.ps1
```

Die portable Ausgabe liegt unter `dist/windows/win-x64/TinyHand.Windows.exe`.
Vor erneutem Veröffentlichen eine laufende Kopie aus diesem Ordner beenden.
Tests öffnen kurz Overlay-Fenster auf dem Desktop. Der optionale hörbare
Regressionstest benötigt ein funktionierendes Audiogerät:

```powershell
dotnet run --project windows/TinyHand.Windows.Tests -c Release -- --audio-regression
```

Das [Windows-Handbuch](windows/README.md) enthält Audio-Diagnosen, optionale ARM64-Builds
und weitere Implementierungsdetails. Veröffentlichte Releases bieten derzeit nur **x64**.

## Releases veröffentlichen

Der [Windows-Release-Workflow](.github/workflows/windows-release.yml) startet beim
Push eines Tags im Format `windows-vMAJOR.MINOR.PATCH`. Er testet und baut die App
und veröffentlicht anschließend ein portables ZIP samt SHA-256-Prüfsumme in den
GitHub-Releases dieses Forks. Tags mit Zusatz, etwa `windows-v0.2.0-beta.1`, erzeugen
ein Pre-Release. Windows-Tags sind unabhängig von den ursprünglichen macOS-Tags (`v*`).

Einrichtung und Tagging sind unter [GitHub-Releases im Windows-Handbuch](windows/README.md#github-releases) beschrieben.

## Danksagung und Lizenz

Basierend auf [nilbuild/tinyhand](https://github.com/nilbuild/tinyhand), dem ursprünglichen
TinyHand-Projekt von **Kamran Ahmed**. Vielen Dank an das Originalprojekt und seine
Mitwirkenden für Konzept, Grafiken, Geräusche und macOS-Implementierung.

Dieser Fork steht weiterhin unter der **MIT-Lizenz**. Die Bedingungen und der
ursprüngliche Copyright-Hinweis stehen in [LICENSE](LICENSE).
