<img src="./.github/logo-windows.png" width="80" alt="TinyHand-Logo mit Cactus-Hand" />

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
lavender-Hand. Die EXE verwendet das schräge Kaktus-Logo aus der README als Symbol.

## Bedienung

| Aktion | Ergebnis |
|---|---|
| **Strg+Alt+H** oder Linksklick auf das Tray-Symbol | Zeiger ein- oder ausschalten |
| Rechtsklick auf das Tray-Symbol | Hand oder Laser, Größe und Tippgeräusch wählen oder beenden |
| Linksklick bei aktivem Zeiger | Tipp-Effekt anzeigen und optional Geräusch abspielen |
| Linke Maustaste halten und ziehen | Auf dem Bildschirm zeichnen |
| **Umschalt beim Loslassen** der linken Maustaste halten | Pfeilspitze hinzufügen |
| **Strg+Alt+X** | Zeichnung löschen |
| **Esc** | Zeiger ohne Löschen ausschalten; ausstehende Texteingabe abbrechen |

**Bei aktivem Zeiger blockiert das Overlay Mausklicks und Scrollen in den darunter
liegenden Anwendungen.** Schalte es mit **Esc** oder **Strg+Alt+H** aus, bevor du
andere Apps oder das Tray-Menü bedienst. Normale Tastatureingaben werden nicht
blockiert und können weiterhin die zuvor aktive Anwendung erreichen.

## Anmerkungen

Beim Ausschalten bleiben fertige Linien und Texte sichtbar und klickdurchlässig.
Klicks und Scrollen erreichen wieder die darunterliegenden Anwendungen.
**Strg+Alt+X** oder **Alles löschen** entfernt sämtliche Anmerkungen. Beenden und
Änderungen der Monitoranordnung verwerfen sie ebenfalls; Inhalte werden nicht gespeichert.

**Zeichenfarbe** im Tray gilt für neue Elemente; vorhandene behalten ihre Farbe.
**Text platzieren** wartet auf einen Positionsklick und öffnet einen fokussierten
Editor. Enter bestätigt; Esc oder ein Anwendungswechsel bricht ab. Nur dieser
explizite Editor nimmt Texteingaben entgegen. Text ist einzeilig (maximal 500
Unicode-Skalarwerte), hat feste Schrift/Größe und ist nach Bestätigung nicht
verschiebbar oder bearbeitbar. Zum Korrigieren löschen und neu eingeben.
**Screenshot in Zwischenablage** kopiert den vollständigen Monitor unter dem
Mauszeiger mit fertigen Anmerkungen, aber ohne Hand, Laser oder Tippanimation.
Danach in ein bildfähiges Programm einfügen. Es wird keine Datei automatisch
gespeichert. Ausstehende Zeichnungen/Texte zuvor bestätigen oder abbrechen.
Geschützte Fenster, sichere Desktops und manche Vollbildinhalte können leer oder
nicht aufnehmbar sein. Das optionale [Stream-Deck-Plugin](stream-deck/README.de.md)
bietet sieben Aktionen mit echter Zustandsrückmeldung und benötigt Stream Deck 7.1+.
Veröffentlichte EXEs enthalten das Paket und bieten Installation/Update bei erkannter
Software an. **Später** pausiert sieben Tage, **Nicht mehr fragen** unterdrückt auch
spätere Updates. Manuelle Installation und Angebotssteuerung bleiben im Tray;
Stream Decks eigenen Dialog bestätigst du selbst. App-only-Entwicklungsbuilds ohne
Paket haben keinen integrierten Installer. Paketprüfung ist automatisiert;
Hardwareabnahme steht noch aus. TinyHand bleibt ohne Plugin nutzbar.

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
- Kein Durchklickmodus mit aktivem Zeiger (passive Anmerkungen sind klickdurchlässig) und kein separat verlängertes Arm-/Stabsegment.
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

## Ideen aus anderen Forks

Die [Fork-Recherche vom 23. November 2026](research/tinyhand-forks-2026-11-23.md)
untersucht die Forks des Originalprojekts und stellt mögliche Verbesserungen für
Windows vor, mit Quellenlinks und Hinweisen zur Umsetzung. Das sind Ideen, keine
bereits implementierten Funktionen dieses Windows-Ports.

## Danksagung und Lizenz

Basierend auf [nilbuild/tinyhand](https://github.com/nilbuild/tinyhand), dem ursprünglichen
TinyHand-Projekt von **Kamran Ahmed**. Vielen Dank an das Originalprojekt und seine
Mitwirkenden für Konzept, Grafiken, Geräusche und macOS-Implementierung.

Dieser Fork steht weiterhin unter der **MIT-Lizenz**. Die Bedingungen und der
ursprüngliche Copyright-Hinweis stehen in [LICENSE](LICENSE).
