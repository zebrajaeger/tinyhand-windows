# TinyHand für Windows – Entwicklungs- und Bedienungshandbuch

**Deutsch** | [English](README.en.md) · [Projektübersicht](../README.de.md)

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
Das Tray-Symbol zeigt die gewählte Hand ohne Stab und aktualisiert sich beim
Stilwechsel. Im Laser-Modus wird die lavender-Hand als App-Symbol verwendet.
Die EXE verwendet das schräge Kaktus-Logo aus der README als festes Anwendungssymbol.

Im Tray-Menü aktiviert **Mit Windows starten** den Start nach der Anmeldung für
den aktuellen Benutzer, ohne Administratorrechte. Standardmäßig ist die Option
aus. Sie schreibt den aktuellen, in Anführungszeichen gesetzten EXE-Pfad unter
`HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run` in den Wert
`TinyHand.Windows`. Beim Ausschalten wird nur dieser Wert entfernt.
Der Zeiger bleibt auch beim Autostart zunächst aus. Die portable EXE sollte in
einem dauerhaften Ordner liegen; nach dem Verschieben die Option aus- und wieder
einschalten, vor dem Löschen ausschalten. Der Windows-Task-Manager kann Autostart
zusätzlich unabhängig von diesem Registry-Eintrag deaktivieren.

## Bedienung

- **Strg+Alt+H** oder Linksklick auf das Tray-Symbol: ein-/ausschalten.
- **Rechtsklick auf das Tray-Symbol**: Handstil/Laser wählen, löschen, beenden.
- **Linksklick im Zeigemodus**: sichtbarer Tipp-Effekt.
- **Linke Maustaste halten und ziehen**: zeichnen.
- **Umschalt beim Loslassen halten**: Pfeilspitze hinzufügen.
- **Strg+Alt+X**: Zeichnung löschen.
- **Esc**: Zeigemodus ohne Löschen beenden; ausstehenden Text abbrechen.

Im Tray-Menü stehen drei Zeigergrößen und sechs Tippgeräusche zur Wahl.
Die Geräuschauswahl spielt eine Vorschau; der Schalter **Tippgeräusch** aktiviert
den Sound bei Mausklicks (standardmäßig aus).
Handstil, Größe und Sound werden unter `%USERPROFILE%/.config/TinyHand/settings.json`
gespeichert. Beim nächsten Start bleibt der Zeiger trotzdem zunächst ausgeschaltet.
Fehlende oder beschädigte Einstellungen werden durch sichere Standardwerte ersetzt.

Im aktiven Modus blockiert ein transparentes Overlay Mausklicks und Scrollen
auf den normalen Desktop-Anwendungen. Zum normalen Arbeiten und für das Tray-Menü
zuerst **Esc** oder **Strg+Alt+H** drücken. Tastatureingaben werden abgesehen von den
registrierten Hotkeys und dem expliziten Texteditor nicht abgefangen: Text kann weiterhin in die zuvor aktive
Anwendung gelangen. Belegte Hotkeys werden beim Start gemeldet.

Pro Monitor wird ein Overlay angelegt; Zeichenpunkte werden in physischen
Desktopkoordinaten gespeichert. Eine Änderung der Monitoranordnung setzt die
Zeichnung zurück. Unterschiedliche DPI-Skalierungen vor einem wichtigen Vortrag
auf dem konkreten System prüfen.

## Anmerkungen und Texteingabe

Ohne Zeiger bleiben fertige Anmerkungen in nicht aktivierenden, klickdurchlässigen
Overlays sichtbar; Hand/Laser und Audio-Keep-alive sind aus. **Alles löschen**
(`Strg+Alt+X`) entfernt Linien, Texte und ausstehende Eingaben ohne Aktivierungswechsel.
Beenden und Monitoränderungen verwerfen Inhalte; jeder Start hat ein leeres Dokument.

**Zeichenfarbe** wird gespeichert und gilt für neue Elemente. **Text platzieren**
startet die Platzierung: Position anklicken, tippen, Enter bestätigt, Esc bricht ab.
Fokusverlust bricht ohne Zurückstehlen des Fokus ab. Nur der fokussierte Texteditor
ist eine Ausnahme von der normalen Tastaturdurchleitung. Text ist einzeilig,
maximal 500 Unicode-Skalarwerte, fest 28 DIP Segoe UI und nach Bestätigung nicht
bearbeitbar. Linien und Texte teilen eine Historie von 500 Elementen; beim
Bestätigen wird nötigenfalls das älteste entfernt.

**Screenshot in Zwischenablage** nimmt den Monitor unter dem aktuellen Windows-
Mauszeiger in physischen Pixeln auf. Fertige Texte/Linien/Pfeile erscheinen einmal;
Hand, Laser und Tippanimation fehlen. Das Bild in ein anderes Programm einfügen;
keine automatische Dateiablage oder Übertragung. Ausstehender Text/Strich und
laufende Aufnahmen führen zu einer Belegt-Meldung. Fehler stellen die Overlays
wieder her und werden nicht als Erfolg gemeldet. Clipboard-Wiederholungen sind
begrenzt; das Aufnahmelimit beträgt fünf Sekunden. Sichere/geschützte Desktops und
exklusive Vollbildinhalte können leer bleiben; Clipboard-Erfolg beweist keine
Aufnahme geschützter Pixel. Das optionale [Stream-Deck-Plugin](../stream-deck/README.de.md)
wird separat für Stream Deck 7.1+ gebaut und lokal installiert. Protokoll v1 nutzt
eine begrenzte Named-Pipe für denselben Benutzer/dieselbe Windows-Sitzung, nur bei
normalem App-Start, nicht bei Ressourcenprüfung. Kein Netzwerklistener oder
automatischer App-Start. Plugin-Werkzeuge sind keine portable Build-Abhängigkeit.
Hardware-/Fremdbenutzer-/Sitzungsabnahme getrennt von automatischen Tests festhalten.

## Bauen und prüfen

Voraussetzung: Windows mit .NET 10 SDK.
Die Befehle werden im Stammverzeichnis des Repositorys ausgeführt. Vor dem
Veröffentlichen eine laufende EXE aus dem Ausgabeordner beenden.

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
Der optionale Audiotest `--audio-regression` benötigt ein funktionierendes Audiogerät.
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

## GitHub-Releases

Der Workflow `.github/workflows/windows-release.yml` veröffentlicht beim Push
eines Tags `windows-vMAJOR.MINOR.PATCH` automatisch ein Windows-Release.
Er führt die Windows-Regressionstests aus, baut die portable x64-Version und
lädt ein ZIP sowie eine SHA-256-Prüfsumme hoch. Das ZIP enthält keine Debugsymbole.
Die fertige EXE wird zusätzlich auf ihre eingebetteten Audiodateien geprüft.
Der hörbare Test `--audio-regression` bleibt ein lokaler Test, da CI-Runner kein
verlässliches Audiogerät bereitstellen.

Nach Commit und Push der gewünschten Änderungen auf `main` beispielsweise:

```bash
git tag -a windows-v0.1.0 -m "TinyHand for Windows 0.1.0"
git push origin windows-v0.1.0
```

Tags mit einem Zusatz, etwa `windows-v0.1.0-beta.1`, erzeugen ein Pre-Release.
Die Windows-Tags sind unabhängig von den ursprünglichen macOS-Tags `v*`.
Im GitHub-Fork muss GitHub Actions aktiviert sein; zusätzliche Secrets sind
nicht erforderlich. Der Workflow verwendet das automatische `GITHUB_TOKEN`
und Schreibrechte nur im Veröffentlichungsjob. Bei einem fehlgeschlagenen Lauf
kann derselbe Workflow erneut gestartet werden; bestehende Release-Assets werden
ersetzt, manuell bearbeitete Release-Notizen bleiben erhalten.
Falls der Tag-Build nicht gestartet wurde, unter **Actions → Windows Release →
Run workflow** auf `main` den vorhandenen Windows-Tag im Feld `tag` angeben.
Der Build verwendet den Quellcode dieses Tags. Vorhandene Tags nicht verschieben;
für neue Änderungen eine neue Versionsnummer verwenden. Die Veröffentlichung ist
erst vollständig, wenn ZIP und Prüfsumme tatsächlich im Release vorhanden sind.

## Noch nicht enthalten / Grenzen

- Auto-Updater, Installer und Signierung
- Schwebende Werkzeugleiste und umfassendes Einstellungsfenster
- URL-Automatisierung, eigene Handgrafiken und Durchklickmodus mit aktivem Zeiger (passive Anmerkungen sind klickdurchlässig)
- Die Hand neigt sich dynamisch mit sanft nachgeführtem Griffpunkt; die Fingerspitze
  bleibt am Cursor. Eine separate Verlängerung des Arm-/Stabsegments wie unter macOS
  ist noch nicht enthalten.

Kein Overlay über dem sicheren Windows-Desktop (UAC/Anmeldung); exklusives Vollbild
und manche geschützten Anwendungen können das Overlay verdecken.
Bei Bildschirmfreigaben die gesamte Anzeige teilen — die Freigabe nur eines
Anwendungsfensters enthält das separate Overlay normalerweise nicht.
