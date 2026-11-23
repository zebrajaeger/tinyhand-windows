# TinyHand für Windows (erste Version)

Native Windows-Portierung mit C#/.NET 10 und WPF. Der macOS-Code bleibt unverändert.
Die vorhandenen Handgrafiken werden beim Build als Ressourcen eingebettet.

## Start

Die portable Ausgabe liegt nach dem Veröffentlichen unter
`dist/windows/win-x64/TinyHand.Windows.exe`. Sie benötigt keine separat installierte
.NET-Laufzeit und keine Administratorrechte. Beim ersten Start entpackt .NET native
Komponenten in sein temporäres Cacheverzeichnis.
Zielplattform: Windows 10/11 x64; ARM64 kann separat veröffentlicht werden.
Die EXE ist nicht signiert; Windows kann eine SmartScreen-Warnung anzeigen.

Nach dem Start erscheint TinyHand im Infobereich der Taskleiste, gegebenenfalls
unter den ausgeblendeten Symbolen. Der Zeiger ist zunächst ausgeschaltet.

## Bedienung

- **Strg+Alt+H** oder Linksklick auf das Tray-Symbol: ein-/ausschalten.
- **Rechtsklick auf das Tray-Symbol**: Handstil/Laser wählen, löschen, beenden.
- **Linksklick im Zeigemodus**: sichtbarer Tipp-Effekt.
- **Linke Maustaste halten und ziehen**: zeichnen.
- **Umschalt beim Loslassen halten**: Pfeilspitze hinzufügen.
- **Strg+Alt+X**: Zeichnung löschen.
- **Esc**: Zeichnung löschen und Zeigemodus beenden.

Im Tray-Menü stehen drei Zeigergrößen und sechs Tippgeräusche zur Wahl.
Die Geräuschauswahl spielt eine Vorschau; der Schalter **Tippgeräusch** aktiviert
den Sound bei Mausklicks (standardmäßig aus).
Handstil, Größe und Sound werden unter `%USERPROFILE%/.config/TinyHand/settings.json`
gespeichert. Beim nächsten Start bleibt der Zeiger trotzdem zunächst ausgeschaltet.
Fehlende oder beschädigte Einstellungen werden durch sichere Standardwerte ersetzt.

Im aktiven Modus blockiert ein transparentes Overlay Mausklicks und Scrollen
auf den normalen Desktop-Anwendungen. Zum normalen Arbeiten und für das Tray-Menü
zuerst **Esc** oder **Strg+Alt+H** drücken. Tastatureingaben werden abgesehen von den
registrierten Hotkeys nicht abgefangen: Text kann weiterhin in die zuvor aktive
Anwendung gelangen. Belegte Hotkeys werden beim Start gemeldet.

Pro Monitor wird ein Overlay angelegt; Zeichenpunkte werden in physischen
Desktopkoordinaten gespeichert. Eine Änderung der Monitoranordnung setzt die
Zeichnung zurück. Unterschiedliche DPI-Skalierungen vor einem wichtigen Vortrag
auf dem konkreten System prüfen.

## Bauen und prüfen

Voraussetzung: Windows mit .NET 10 SDK.

```powershell
dotnet build windows/TinyHand.Windows -c Release
dotnet run --project windows/TinyHand.Windows.Tests -c Release
dotnet run --project windows/TinyHand.Windows.Tests -c Release -- --audio-regression
./windows/publish.ps1
# Optional für Windows auf ARM:
./windows/publish.ps1 -Runtime win-arm64
```

Die Tests benötigen keine externen Testpakete. Sie prüfen Pfeilgeometrie,
eingebettete Bilder, echte Overlay-Fenster, Rendering und Zustandsbereinigung.
Dabei werden kurz Overlays auf dem aktuellen Desktop eingeblendet.
Zusätzlich prüft das Publish-Skript direkt mit der fertigen portablen EXE,
ob alle sechs eingebetteten WAV-Dateien gefunden und decodiert werden können.
Audio-Diagnosen stehen unter `%USERPROFILE%/.config/TinyHand/audio.log`.
Der Audiotest spielt alle sechs Geräusche ab und prüft, dass weitere Klicks
laufende Clips nicht abschneiden. Die Wiedergabe hält das Audiogerät offen und
reiht vollständige Clips aneinander. Bei sehr schnellen Klickfolgen werden
höchstens vier Clips vorgemerkt; weitere Klickgeräusche werden ausgelassen,
damit kein langer hörbarer Rückstau entsteht.
Bei aktivem Zeigemodus und eingeschaltetem Tippgeräusch wird derselbe Audiostream
zwischen Klicks mit digitaler Stille versorgt. Das soll verhindern, dass kurze
Geräusche beim Aufwachen eines Headsets verloren gehen. Beim Ausschalten endet
die Stille-Wiedergabe und das Audiogerät wird nach den vorgemerkten Clips freigegeben.
Der Audiotest prüft auch Klicks nach jeweils drei Sekunden Pause.

## Noch nicht enthalten / Grenzen

- Auto-Updater, Installer, Signierung und Autostart
- Schwebende Werkzeugleiste und umfassendes Einstellungsfenster
- URL-Automatisierung, eigene Handgrafiken und Durchklickmodus
- Die Hand neigt sich dynamisch mit sanft nachgeführtem Griffpunkt; die Fingerspitze
  bleibt am Cursor. Eine separate Verlängerung des Arm-/Stabsegments wie unter macOS
  ist noch nicht enthalten.

Kein Overlay über dem sicheren Windows-Desktop (UAC/Anmeldung); exklusives Vollbild
und manche geschützten Anwendungen können das Overlay verdecken.
Bei Bildschirmfreigaben die gesamte Anzeige teilen — die Freigabe nur eines
Anwendungsfensters enthält das separate Overlay normalerweise nicht.
