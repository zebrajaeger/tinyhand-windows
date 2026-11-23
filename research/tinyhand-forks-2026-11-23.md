# TinyHand-Forks: Ideen für die Windows-Version

Stand: **23. November 2026**. Untersucht wurde das Fork-Netzwerk von
[nilbuild/tinyhand](https://github.com/nilbuild/tinyhand) mit Blick auf
[zebrajaeger/tinyhand-windows](https://github.com/zebrajaeger/tinyhand-windows).
Dies ist eine Recherche, keine Implementierung oder Funktionsprüfung der fremden Apps.

## Umfang und Methode

- Die [Repository-API](https://api.github.com/repos/nilbuild/tinyhand) meldete **81 Forks**.
- Die [direkte Fork-Liste](https://api.github.com/repos/nilbuild/tinyhand/forks?per_page=100&sort=newest)
  lieferte **80 Repositories**; Seite 2 war leer.
- Der zusätzliche Fork ist [fduarte42/tinyhand](https://github.com/fduarte42/tinyhand),
  sichtbar in der [Fork-Liste von AbdulRehman56/tinyhand](https://api.github.com/repos/AbdulRehman56/tinyhand/forks?per_page=100).
- Alle Branch-Spitzen der 80 direkt gelisteten Forks wurden mit `git ls-remote --heads`
  erfasst, nicht nur die jeweiligen Standardbranches: insgesamt **124 sichtbare
  Branches**, davon **62 Spitzen außerhalb der Originalhistorie**. Der zusätzliche
  verschachtelte Fork hat zwei sichtbare Branches.
- **47 direkte Forks** hatten ausschließlich Branch-Spitzen, die bereits in der
  Originalhistorie vorkommen. Das bedeutet: keine zusätzlichen Commits auf ihren
  aktuell sichtbaren Branches; nicht zwangsläufig synchron mit dem neuesten Original.
- **33 direkte Forks** hatten zusätzliche Commits, darunter der eigene Windows-Fork.
  Mit dem verschachtelten Fork sind das **33 fremde Repositories mit zusätzlichen Commits**.
- Zusätzliche Branches wurden über Git geladen und mit ihrem gemeinsamen Vorfahren
  zum Original verglichen. Commit-Historie und geänderte Dateien wurden gesichtet;
  die unten hervorgehobenen Ideen zusätzlich in relevanten Quelldateien geprüft.
  Separate Screenshot-/Website-Branches ohne gemeinsame Historie zählen nicht als App-Feature.
- Original-Bezugspunkt:
  [`da98f98d529971297487b0ec6ef07dc549555686`](https://github.com/nilbuild/tinyhand/tree/da98f98d529971297487b0ec6ef07dc549555686).
  Eigener Windows-Bezugspunkt:
  [`82b4f4a`](https://github.com/zebrajaeger/tinyhand-windows/tree/82b4f4a).
- Keine fremden Apps installiert oder gestartet, keine Builds oder Hörtests ausgeführt.
  Vorhandener Quellcode ist kein Nachweis, dass eine Funktion in allen Situationen funktioniert.
  Die Aufwandsschätzungen sind qualitative Einschätzungen für den vorhandenen C#/WPF-Port.

## Wichtig: Nicht jede Idee ist eine Fork-Erfindung

Das aktuelle Original enthält bereits Zusatztasten zum Durchklicken, Double-Shift-
Click-through, wählbare Zeichenfarben, zeitliches Verblassen, eigene Hände,
konfigurierbare Hotkeys, Toolbar-Favoriten, Autostart und URL-Automation.
Belege: [Preferences.swift](https://github.com/nilbuild/tinyhand/blob/da98f98d529971297487b0ec6ef07dc549555686/Sources/TinyHand/Preferences.swift),
[OverlayController.swift](https://github.com/nilbuild/tinyhand/blob/da98f98d529971297487b0ec6ef07dc549555686/Sources/TinyHand/OverlayController.swift),
[HandMaker.swift](https://github.com/nilbuild/tinyhand/blob/da98f98d529971297487b0ec6ef07dc549555686/Sources/TinyHand/HandMaker.swift),
[README](https://github.com/nilbuild/tinyhand/blob/da98f98d529971297487b0ec6ef07dc549555686/README.md).

Viele Windows- und Linux-Forks portieren diese Funktionen nur auf eine andere Plattform.
Für die eigene Windows-Version sind sie trotzdem interessante Lücken. Autostart ist
dort bereits vorhanden und deshalb kein neuer Umsetzungsvorschlag.

## Die interessantesten konkreten Ideen

### 1. Zeigen, ohne normale App-Klicks zu blockieren

**Nutzen:** Die Hand bleibt beim Vorführen sichtbar, während Buttons, Browser und
Folien bedienbar sind. Ein expliziter Zeichenmodus übernimmt dagegen die Maus.

- [sergvax: Click-through plus separat schaltbares Zeichnen](https://github.com/sergvax/tinyhand/commit/a18a86a2719e2fd0d63d49a1e43c8e988a264d98):
  Änderungen in `InputTap.swift`, `OverlayController.swift` und den Einstellungen.
- [jorgenbruntveit: Pointer Mode](https://github.com/jorgenbruntveit/tinyhand/tree/pointer-mode):
  normale Klicks standardmäßig, Präsentationsverhalten beim Halten von Option.
- [trussosw: Real-click-Modus mit Klickwelle](https://github.com/trussosw/tinyhand/commit/efebd3c14818504a2a86c7436e83395f679977fd):
  eine echte App-Interaktion kann trotzdem optisches Feedback auslösen.
- [MUL7ii: Windows-Hybridmodus](https://github.com/MUL7ii/tinyhand/blob/ea40fa0b2e/windows/src/hook.rs):
  Klick zunächst zurückhalten; ab 4 Pixel Bewegung zeichnen, sonst den Klick nach
  Loslassen an Windows weitergeben. Der Code markiert selbst eingespeiste Eingaben
  und führt `SendInput` außerhalb des Hook-Callbacks aus.

**Einordnung:** Einfaches Durchklicken gibt es bereits upstream; interessante
Fork-Varianten sind die Modi, die Rückmeldung und der Klick-/Drag-Hybrid.
**Aufwand:** mittel bis hoch. Für den Einstieg lieber zwei eindeutige Modi
„Zeigen“/„Zeichnen“ als verzögerte Klick-Replays. Der Hybrid muss insbesondere
Doppelklicks, Drag-and-drop, Textauswahl, Modifier, erhöhte Fenster und negative
Monitorpositionen korrekt behandeln. Nicht blind aus fremdem Hook-Code übernehmen.

### 2. Zeichenwerkzeuge: Strichstärke, Farbe, Ausblenden und Undo

**Nutzen:** Lesbare Markierungen auf hellen/dunklen Hintergründen und weniger
manuelles Löschen während eines Vortrags.

- [trussosw: dünn/mittel/dick und vier Farbtöne](https://github.com/trussosw/tinyhand/commit/be15d1690a98abc0f178dfd930d522b7926a1b30):
  Strichstärkenfaktoren 0,55 / 1 / 1,9; lebendig, pastell, gedämpft, dunkel;
  zusätzlich eine sehr kurze Fade-Option.
- [jorgenbruntveit: wandernde, verblassende Zeichenspur](https://github.com/jorgenbruntveit/tinyhand/commit/f42633c68521a5aabf3ed364d3d24472acace00d):
  eine Linie verschwindet vom Anfang zum Ende statt überall gleichzeitig.
  [Folgeänderung](https://github.com/jorgenbruntveit/tinyhand/commit/4d29768c1e)
  startet das Verblassen erst nach dem Loslassen.
- [AbdulRehman56: GNOME-Zeichenwerkzeuge und Undo](https://github.com/AbdulRehman56/tinyhand/blob/6f890adae4e7d14f8dfa81a10f30de3677fdbb2d/linux/companion/tinyhand@abdulrehmankhan56/extension.js):
  `_undo()`, separater Point-/Draw-Modus und Toolbar-Aktionen im Quellcode.
- [Felitendo/waypointer](https://github.com/Felitendo/waypointer/blob/7d83196e43/src/settings.rs):
  individuelle Zeichenfarbe je Pointer (`inks`) und schaltbare Klickwellen (`ripples`);
  [Raster-Renderer](https://github.com/Felitendo/waypointer/blob/7d83196e43/src/raster.rs)
  enthält einen zusätzlichen weichen Leuchteffekt.

**Einordnung:** Grundfarben und einfaches zeitliches Fade existieren upstream;
Stärken, Farbtöne, segmentweises Fade und Undo sind die interessanteren Ergänzungen.
**Aufwand:** klein bis mittel. In Windows zunächst Farbe, Breite, „letzten Strich
rückgängig“ und „nach N Sekunden ausblenden“. Koordinaten und Pfeilspitzen müssen
bei gemischtem DPI konsistent bleiben. Permanente Zeichnungen als Option behalten.

### 3. Freihandformen automatisch begradigen

**Nutzen:** Ein grober Kreis oder Rahmen wird nach dem Zeichnen zu einer sauberen
Form; Pfeile lassen sich aus Linie und nachträglich gezeichneter Spitze bilden.

[CatalystMonish: Snap Shapes](https://github.com/CatalystMonish/tinyhand/commit/d2db2444805f92a5d13654bfbd3029176ea10a36)
verbindet den Renderer mit einem gemeinsamen C99-Erkenner.
[ShapeKit-Schnittstelle](https://github.com/CatalystMonish/tinyhand/blob/2dd3c6503e/Sources/ShapeKit/include/shapekit.h)
nennt Linie, Pfeil, Kreis, Ellipse, Dreieck, Quadrat, Rechteck, Raute, Fünf-/Sechseck,
Stern und Herz. Nicht passende Striche bleiben Freihand. Zwei-Strich-Pfeile haben
eine eigene Erkennung. Es gibt [Regressionstests](https://github.com/CatalystMonish/tinyhand/blob/2dd3c6503e/Tests/shapes/shape_test.c).
[Arisaya1](https://github.com/Arisaya1/tinyhand/tree/916564a254/Sources/ShapeKit)
enthält ebenfalls eine ShapeKit-basierte Windows-Portierung.

**Aufwand:** mittel. Kein KI-Dienst nötig: lokale geometrische Heuristiken.
Für C# entweder den Algorithmus portieren oder C per P/Invoke einbinden; eine
Portierung hält die bisherige Windows-Buildkette einfacher. Opt-in, damit
Handschrift oder bewusst unregelmäßige Markierungen nicht korrigiert werden.
Zunächst Linie, Kreis und Rechteck statt gleich aller dekorativen Formen.

### 4. Spotlight und schneller Hervorhebungsring

- [AbdulRehman56: Präsentations-Toolkit](https://github.com/AbdulRehman56/tinyhand/blob/6f890adae4e7d14f8dfa81a10f30de3677fdbb2d/linux/companion/tinyhand@abdulrehmankhan56/extension.js):
  abdunkelnder Spotlight-Kreis, Klickwellen, optional angezeigte Tasten.
  Konfiguration und Profile liegen in
  [experimental.js](https://github.com/AbdulRehman56/tinyhand/blob/6f890adae4e7d14f8dfa81a10f30de3677fdbb2d/linux/companion/tinyhand@abdulrehmankhan56/experimental.js).
- [trussosw: Quick Ring](https://github.com/trussosw/tinyhand/commit/2c0c56298d82497f783c676d9da2c68fb95a1f74):
  rechte Maustaste halten, links klicken, um schnell einen Ring zu setzen.

**Aufwand:** mittel für Spotlight, kleiner für einen Ring.
Tastenanzeige nur ausdrücklich aktiviert: sie kann vertrauliche Eingaben offenlegen.
Spotlight und Zeichnungen beim Screensharing mit mehreren Monitoren prüfen.

### 5. Screenshot oder Zeichnung exportieren

- [charlesanim: Drawing Capture](https://github.com/charlesanim/tinyhand/commit/0b622244e05beccb15bf54dd9c478d885ccb9d89):
  `Capture.swift` rendert Zeichnungen als PNG und verarbeitet eine Freihand-
  Bereichsauswahl für Screenshots. Leere Zeichnungen und zu kleine Bereiche
  werden explizit abgelehnt.
- [maxnetdigital: Snapshot and Record](https://github.com/maxnetdigital/tinyhand/commit/d30077f479a5acd9b7a03f17738791a46cec87c1):
  `Capture.swift` verwendet ScreenCaptureKit/`SCStream`, der Recorder einen
  Video-Writer; Toolbar und Controller erhalten Capture-Aktionen.

**Aufwand:** mittel für PNG/Clipboard, hoch für robuste Videoaufnahme.
Empfehlung: „Screenshot mit Markierungen kopieren“ oder transparenter PNG-Export
vor einem eingebauten Recorder. Videoaufnahme überschneidet sich stark mit OBS
und Windows-Werkzeugen und macht die kleine App deutlich komplexer.
Der macOS-Capture-Code ist konzeptionelle Referenz, keine Windows-Lösung.

### 6. Handgesten und zusätzliche grafische Stile

[xynova: Gestures](https://github.com/xynova/tinyhand/commit/c7bbac616dde0decf97f3522445646a390bcdaf7)
ergänzt Line-art-Hand und Bildvarianten für OK, Shaka und Mittelfinger. Ein
gehaltener Tastenkürzel schaltet nur das Handbild um; Zielpunkt, Größe und Stick
bleiben stabil. Vorladen und Cache vermeiden Dateizugriffe bei jedem Wechsel.
Der Commit enthält zusätzliche Tests für Gesten und eigene Hände.

[jorgenbruntveit: Outline Hand](https://github.com/jorgenbruntveit/tinyhand/tree/outline-hand)
enthält Cartoon-Hand, Katzenpfote und Tap-Bursts, die bei schnellen wiederholten
Klicks stärker werden. Der `pokey-style`-Branch ergänzt einen persönlichen
Mittelfinger-Stil, nicht unbedingt für jede Präsentation geeignet.

**Aufwand:** klein für zusätzliche normale Bilder, mittel für Gesten inklusive
Hotkeys und Kalibrierung. OK/Daumen hoch als geschäftstaugliche Varianten priorisieren.
Artwork-Lizenz und zusätzliche Bildgröße vor Übernahme prüfen.
Eigene Hände selbst gibt es bereits upstream, aber nicht im Windows-Port.

### 7. Kleine Komfort- und Effizienzverbesserungen

- [alcibiadesc: Ton nur bei Taps, nicht beim Zeichnen](https://github.com/alcibiadesc/tinyhand/commit/d8ce59056b2bf65899c5c38d24aaebca0134a2f6):
  Klickton wird aufgeschoben und entfällt, wenn der Klick zum Strich wird.
  **Kleiner Aufwand**, gutes Detail bei häufiger Nutzung.
- [jorgenbruntveit: Lautstärkeregler und drei zusätzliche Sounds](https://github.com/jorgenbruntveit/tinyhand/commit/833872bdea98c316f63088e57e4119ae68ad4723).
- [p4block: eigene Sounddatei auswählen](https://github.com/p4block/tinyhand/commit/5d91bec7ba12f02ead369d194deef5105356ff11).
  Windows muss neue Formate in das bestehende PCM-/Queue-Modell überführen;
  die funktionierende Headset-Silence-Queue unbedingt erhalten.
- [trussosw: Timer nach drei Sekunden Stillstand schlafen lassen](https://github.com/trussosw/tinyhand/commit/491831508c062b125186ae1c111f775ff86781ab).
  Wake-up über Maus-/Button-/Modifier-Ereignisse, nicht nur langsames Polling.
  **Mittlerer Aufwand** im aktuellen Windows-Modell. Render-Sleep darf niemals
  den unabhängigen Audio-Keep-alive abschalten; Fade/Tap-/Pose-Animationen zuerst beenden.
- [MUL7ii: Capture-Werkzeugen automatisch Platz machen](https://github.com/MUL7ii/tinyhand/blob/ea40fa0b2e/windows/src/main.rs):
  überwacht den Vordergrund und pausiert für `ScreenClippingHost.exe`,
  `SnippingTool.exe` und `ScreenSketch.exe`. **Kleiner bis mittlerer Aufwand**,
  praktisch für Windows-Screenshots. Erst eine schaltbare Ausnahme für bekannte
  Capture-Tools, nicht beliebige Prozesse ohne nachvollziehbare Regel ausnehmen.
- [fduarte42: Sprites in Anzeigegröße cachen und transparente Ränder abschneiden](https://github.com/fduarte42/tinyhand/commit/2d8910dd6168f1fd2c40ffa8c5ef685c59d0eb50).
  Konkreter GNOME/Cairo-Fix; WPF arbeitet anders. Erst messen, nicht ungeprüft
  gleichartige Performancegewinne versprechen. Das optimiert Rendering, nicht
  automatisch die Größe des ausgelieferten Windows-EXE.
- [tumaranja: einstellbare Tap-/Stick-Schwingweite](https://github.com/tumaranja/tinyhand/commit/b21dc52a6d6646f9608f81bea1e4c271bb1f3cff).
  Für den Windows-Port erst nach Klärung des noch fehlenden separaten Stick-Segments.
- [ulentini: Audio-Freeze beim Screensharing](https://github.com/ulentini/tinyhand/commit/01d7b76e77):
  Audioarbeit auf eigene Queue und Engine-Neuaufbau nach Geräteänderungen.
  macOS-Fix, nicht Ersatz für den bestätigten Windows-Headset-Fix.

### 8. Lokale Automatisierung und Presenter-Profile

[0xdhrv: JSON Control Bridge](https://github.com/0xdhrv/tinyhand/commit/602e3f4ad3c0436bd06fa7a97b318246e3bdff2a)
geht über die bestehenden URL-Befehle hinaus: Status und Stile abfragen,
Konfiguration als validierten Batch anwenden, Favoriten setzen.
[Protokoll](https://github.com/0xdhrv/tinyhand/blob/602e3f4ad3c0436bd06fa7a97b318246e3bdff2a/docs/control-protocol.md)
und Source sehen privaten Unix-Socket, Peer-UID-Prüfung, Größenlimits und Timeouts vor.

**Nutzen:** Stream Deck, Launcher oder Skript setzt „Demo: Laser, blau, ohne Ton“
mit einer Aktion. **Aufwand:** mittel. Unter Windows eine auf den aktuellen Benutzer
beschränkte Named Pipe bzw. zunächst einfache CLI-Kommandos verwenden. Kein
ungeschützter TCP-/HTTP-Endpunkt nötig. Das bereits vorhandene Single-instance-
Verhalten muss Befehle an die laufende Instanz weiterreichen, statt sie zu verwerfen.

## Vier größere Umbauten im Detail

### QuiteHandy: Nummernstempel, Whiteboards und Lupe

Untersuchter Stand:
[`121d9d4edd12a05e8bdc46fc7adf0eae0420fd9c`](https://github.com/esreverse/quitehandy/tree/121d9d4edd12a05e8bdc46fc7adf0eae0420fd9c).

- **Nummernstempel 1–9** zum Markieren einer Reihenfolge; vergrößern, versetzen und
  umnummerieren. Sehr brauchbar für Tutorials: „zuerst hier, dann dort“.
- **Breite 1–40 pt und Deckkraft 10–100 %** unabhängig voneinander; außerdem
  Rainbow-Ink mit fortgesetztem Farbverlauf über mehrere Striche.
- **Undo/Redo** für Ink und Whiteboards mit gemeinsamer, begrenzter History.
- **Bewegliche/vergrößerbare Whiteboards** mit hellen/dunklen Flächen, eigener
  Zeichnung und PNG-/Clipboard-Export.
- **Spotlight und echte Zoom-Lupe**: Abdunklung ohne Capture, Vergrößerung mit
  ScreenCaptureKit und Ausschluss der eigenen Fenster.

Source: [Ink.swift](https://github.com/esreverse/quitehandy/blob/121d9d4edd12a05e8bdc46fc7adf0eae0420fd9c/Sources/QuiteHandy/Ink.swift),
[Preferences.swift](https://github.com/esreverse/quitehandy/blob/121d9d4edd12a05e8bdc46fc7adf0eae0420fd9c/Sources/QuiteHandy/Preferences.swift),
[History.swift](https://github.com/esreverse/quitehandy/blob/121d9d4edd12a05e8bdc46fc7adf0eae0420fd9c/Sources/QuiteHandy/History.swift),
[Whiteboard.swift](https://github.com/esreverse/quitehandy/blob/121d9d4edd12a05e8bdc46fc7adf0eae0420fd9c/Sources/QuiteHandy/Whiteboard.swift),
[OverlayController.swift](https://github.com/esreverse/quitehandy/blob/121d9d4edd12a05e8bdc46fc7adf0eae0420fd9c/Sources/QuiteHandy/OverlayController.swift).

**Vorsicht:** Pfeile sind im untersuchten Stand deaktiviert; Dokumentation teilweise
widersprüchlich. Nicht den ganzen Ink-Renderer als „verbesserte Version“ übernehmen.
Für Windows sind Nummernstempel und Undo besonders attraktive kleine Teilprojekte;
Whiteboards und Capture-Lupe wären größere, separat zu planende Features.

### Look Here Please: vollständiger Annotation-Editor und Stream Deck

Untersuchter Stand:
[`58e1954279fbecf6aa80096ded1fe4a827c84eb5`](https://github.com/austinpresley/please-look-here/tree/58e1954279fbecf6aa80096ded1fe4a827c84eb5).

- **Objektbasierte Annotationen:** Stift, Rechteck, Pfeil, Text, Nummer, Radierer
  und Highlighter; Hit-Testing, Bearbeiten und Undo/Redo statt nur einer Strichliste.
- **Radialer Werkzeugring am Cursor** als Alternative zu einer festen Toolbar.
- **Stabile Dokumente pro Display**: Overlay-Geometrie und gespeicherte Markierungen
  sind getrennt. Interessant, weil der aktuelle Windows-Port bei Monitoränderungen löscht.
- **Stream-Deck-Plugin, CLI, AppleScript und abgesicherte lokale JSON-IPC**, mit
  Zustandsantworten statt blindem Toggle.
- **Präsentations-Presets, validierte Backups und Recovery** für Einstellungen,
  Hände und zugehörige Bildvarianten.

Source: [DrawingModel.swift](https://github.com/austinpresley/please-look-here/blob/58e1954279fbecf6aa80096ded1fe4a827c84eb5/Sources/TinyHand/DrawingModel.swift),
[AnnotationResponder.swift](https://github.com/austinpresley/please-look-here/blob/58e1954279fbecf6aa80096ded1fe4a827c84eb5/Sources/TinyHand/AnnotationResponder.swift),
[RadialMenu.swift](https://github.com/austinpresley/please-look-here/blob/58e1954279fbecf6aa80096ded1fe4a827c84eb5/Sources/TinyHand/RadialMenu.swift),
[Source-Verzeichnis](https://github.com/austinpresley/please-look-here/tree/58e1954279fbecf6aa80096ded1fe4a827c84eb5/Sources/TinyHand),
[Tests](https://github.com/austinpresley/please-look-here/tree/58e1954279fbecf6aa80096ded1fe4a827c84eb5/Tests).

**Vorsicht:** README und Projektstatus passen nicht durchgehend zum tatsächlich
öffentlichen Source. Vorhandene Testdateien und dokumentierte lokale Erfolgsclaims
sind keine in dieser Recherche bestätigte Releasequalität. Als Architekturvorlage
interessant; für TinyHand Windows zunächst Text/Nummer/Highlighter auswählen,
statt unmittelbar einen kompletten Editor nachzubauen.

### PlayPointer: konkretes C#-Modell für Control und Draw

Untersuchter Stand:
[`56cc9842d9756ba3d415ca7e6e0a579f8ec673de`](https://github.com/omiusgm/playpointer/tree/56cc9842d9756ba3d415ca7e6e0a579f8ec673de).

- Separater **.NET-10-Windows-Port**, aber **WinForms/native Layered Windows**, nicht WPF.
- **Off / Control / Draw**: Im Control-Modus bleiben Hand und Zeichnung sichtbar,
  normale Eingabe erreicht Apps. Draw erzeugt eigene Eingabefenster; Alt versteckt
  sie temporär. Windows benutzt hier Raw Input statt globaler Maus-Hooks.
- **Fünf Tap-Intensitäten und begrenzte Verstärkung bei schnellen Klickserien**.
- **PNG-Import-Wizard** mit lokaler Kopie, Header-/CRC-Prüfung, Dateigröße bis
  16 MB und Bildmaßen 8–4096 px; Promptvorlage, keine integrierte Bildgenerierungs-API.
- Größerer animierter Pointer-Katalog, z. B. Bongo-Cat-/Zauberstab-artige Effekte.

Source: [Program.cs](https://github.com/omiusgm/playpointer/blob/56cc9842d9756ba3d415ca7e6e0a579f8ec673de/windows/Program.cs),
[Layers.cs](https://github.com/omiusgm/playpointer/blob/56cc9842d9756ba3d415ca7e6e0a579f8ec673de/windows/Layers.cs),
[TapImpact.cs](https://github.com/omiusgm/playpointer/blob/56cc9842d9756ba3d415ca7e6e0a579f8ec673de/windows/TapImpact.cs),
[Windows-Source](https://github.com/omiusgm/playpointer/tree/56cc9842d9756ba3d415ca7e6e0a579f8ec673de/windows).

**Vorsicht:** Der Besitzer beschreibt die Windows-Laufzeit noch als zu prüfen;
Compile/Packaging ist nicht gleich erfolgreiche Benutzung. Charakter-/Markenassets
nicht ungeprüft übernehmen. Der andere Audioansatz erfüllt nicht nachweislich
unsere Queue-/Headset-Keep-alive-Anforderungen: bestehenden Audio-Code behalten.
Das Control-/Draw-Modell ist dagegen eine besonders gut passende Inspiration.

### AiHand: markierter Screenshot als Auftrag an einen Coding-Agent

Untersuchter Stand:
[`949b8ce91b28196061ea6babcb4a3dd1bb599f6c`](https://github.com/rjiang-bb/aihand/tree/949b8ce91b28196061ea6babcb4a3dd1bb599f6c).

- Freihand, Rechteck, Pfeil und Text mit Breite und Undo.
- **Screenshot plus Markierungen und Kommentar** speichern, kopieren oder an
  Cursor Agent, Claude Code bzw. Codex weitergeben.
- Regionvideo mit Auswahl-/Aufnahme-/Preview-Zustandsautomat.
- Umfangreiches Agentfrontend: Sitzungen, Follow-ups, Attachments, Modelle,
  lokale/SSH-Workspaces und Git-Worktrees.

Source: [WindowCapture.swift](https://github.com/rjiang-bb/aihand/blob/949b8ce91b28196061ea6babcb4a3dd1bb599f6c/Sources/AiHand/WindowCapture.swift),
[Ink.swift](https://github.com/rjiang-bb/aihand/blob/949b8ce91b28196061ea6babcb4a3dd1bb599f6c/Sources/AiHand/Ink.swift),
[AgentCLI.swift](https://github.com/rjiang-bb/aihand/blob/949b8ce91b28196061ea6babcb4a3dd1bb599f6c/Sources/AiHand/AgentCLI.swift),
[Source-Verzeichnis](https://github.com/rjiang-bb/aihand/tree/949b8ce91b28196061ea6babcb4a3dd1bb599f6c/Sources/AiHand).

**Deutliche Sicherheitsgrenze:** Source enthält Cursor `--trust`/`--force`,
Claude `--dangerously-skip-permissions` und Codex
`--dangerously-bypass-approvals-and-sandbox`. Cursor Force und Claude Bypass sind
im gelesenen Stand Defaults. Keine harmlose Screenshot-Erweiterung als Ganzes;
würde den Produktumfang und die Rechte erheblich erweitern. Screenshot/Clipboard
isoliert übernehmen wäre sinnvoller als ein integrierter Agentclient mit diesen Defaults.

### Windows-Verteilung: kleineres Paket und authentifizierte Updates

Zwei weitere praktische Funde, unabhängig von sichtbaren Pointer-Effekten:

- **pietro-santoro optimiert die eingebetteten Bilder:** 36 Windows-Hand-PNGs
  belegen im untersuchten Git-Stand zusammen **5,62 MiB**. Der Guide beschreibt
  eine Reduktion auf 1024 px Höhe, und der Workflow aktiviert
  `EnableCompressionInSingleFile=true`. Das ist für unsere etwa 62 MiB Handbilder
  eine passende konkrete Referenz, ohne den portablen Self-contained-Vertrag aufzugeben.
  Nicht als gemessene EXE-Ersparnis ausgeben; Darstellungsqualität bei großen Größen
  und hohen DPI prüfen. Quellen:
  [Hand-Assets](https://github.com/pietro-santoro/tinyhand/tree/e35f5dcc33/windows/TinyHand/Assets/Hands),
  [Guide](https://github.com/pietro-santoro/tinyhand/blob/e35f5dcc33/windows/README.md),
  [Workflow](https://github.com/pietro-santoro/tinyhand/blob/e35f5dcc33/.github/workflows/windows.yml).
- **Arisaya1 enthält einen Windows-Updater mit signiertem Manifest:** ECDSA P-256,
  BCrypt-Signaturprüfung, SHA-256/Größe und ZIP-Member-/Entpackgrößenvalidierung
  sind tatsächlich im Source vorhanden; eigene Update-Tests existieren.
  Quellen: [update.cpp](https://github.com/Arisaya1/tinyhand/blob/916564a254/windows/src/update.cpp),
  [Release-Signierung](https://github.com/Arisaya1/tinyhand/blob/916564a254/windows/scripts/release_sign.py),
  [Tests](https://github.com/Arisaya1/tinyhand/blob/916564a254/Tests/windows/update_test.cpp).
  **Nicht gleich Windows-Codesignierung:** Ein signiertes Update-Manifest entfernt
  keine SmartScreen-Warnung für einen unsignierten EXE. Update-/Rollback-/Key-
  Management und gesperrte portable EXEs sind ein separates größeres Projekt.

## Inventar der direkten Forks mit zusätzlichen Commits

Links zeigen auf die relevanten Branches. Zusammenfassung auf Basis von Historie,
Dateidiff und bei hervorgehobenen Features gezielter Source-Prüfung. Ein Port-Eintrag
ist ausdrücklich kein Qualitätsurteil über das komplette Programm.

| Fork | Relevante Änderungen / Einordnung |
|---|---|
| [0xdhrv](https://github.com/0xdhrv/tinyhand/tree/f/native-control-bridge) | Lokale JSON-Automation; siehe oben. |
| [AbdulRehman56](https://github.com/AbdulRehman56/tinyhand/tree/linux-port) | Rust/X11 und GNOME-Erweiterung; Point/Draw, Gallery/Favoriten, Undo, Spotlight, Profile; Toolkit teils als experimentell eingeordnet. |
| [alcibiadesc](https://github.com/alcibiadesc/tinyhand/branches) | `quiet-strokes`, `shift-tap-mouse-mode`, `hide-toolbar-option`: Ton nur bei Tap, einfacher Shift-Moduswechsel, Toolbar verstecken. Nicht nur `main` anschauen. |
| [Arisaya1](https://github.com/Arisaya1/tinyhand/tree/windows-port) | Nativer C++-Windows-Port und ShapeKit; CI/MinGW. |
| [austinpresley/please-look-here](https://github.com/austinpresley/please-look-here) | Größerer eigenständiger Umbau; ergänzende Untersuchung unten. |
| [Ayas4y](https://github.com/Ayas4y/tinyhand) | Umfangreicher Rust-Neubau für macOS/Windows, separate Settings-App und Update-Feed. Vieles portiert bestehendes Upstream-Verhalten. |
| [CatalystMonish](https://github.com/CatalystMonish/tinyhand/tree/windows-port-and-shape-snapping) | C++-Windows-Port und geometrische Formerkennung mit Tests. |
| [charlesanim](https://github.com/charlesanim/tinyhand) | Native Pointer-Interaktion und Zeichnungs-/Screenshot-Capture. |
| [esreverse/quitehandy](https://github.com/esreverse/quitehandy) | Größerer eigenständiger Umbau; ergänzende Untersuchung unten. |
| [Felitendo/waypointer](https://github.com/Felitendo/waypointer) | Rust/Wayland-Neubau; Farben pro Pointer, Klickring, Glow, mehrsprachige UI, sortierbare Toolbar, Linux-Pakete. |
| [gitrandynow](https://github.com/gitrandynow/tinyhand/tree/remove-sparkle) | Entfernt Sparkle/Auto-Updater. Für Windows derzeit keine neue Funktion: dort gibt es noch keinen Updater. |
| [guidomariadifiore](https://github.com/guidomariadifiore/tinyhand-windows) | Nativer Windows-Port, Alt-Interaktion sowie Sound-/Render-/Compilerkorrekturen. |
| [hetrodoo](https://github.com/hetrodoo/tinyhand/tree/windows-port) | Windows-Port; primär Plattformarbeit. |
| [jorgenbruntveit](https://github.com/jorgenbruntveit/tinyhand/branches) | Viele getrennte Ideen: Pointer Mode, Trail-Fade, Lautstärke, Cartoon/Pfote, Tap-Bursts, Menü statt Toolbar; daneben macOS-Spaces-/Menürand-/Passwortdialog-Fixes. |
| [lithium91](https://github.com/lithium91/tinyhand) | Python/GTK3-Linux-Port mit DEB/RPM und GNOME-Panel-/Dock-Fix. |
| [m1ga](https://github.com/m1ga/tinyhand/tree/linux-kde-port) | Qt-Port für KDE Plasma; primär Plattformarbeit. |
| [maxnetdigital](https://github.com/maxnetdigital/tinyhand) | Snapshot, Bildschirmaufnahme und zusätzlicher Zugang zu Settings. |
| [MUL7ii](https://github.com/MUL7ii/tinyhand/tree/windows-port) | Umfangreicher Rust-Windows-Port: Toolbar, WebView2-Settings, WASAPI, DPI/Monitore, Click-/Drag-Hybrid, Capture-Tool-Ausnahmen, Rendering-/Hook-Korrekturen. Gute Windows-Referenz, aber andere Architektur. |
| [n0stalgia-mac](https://github.com/n0stalgia-mac/tinyhand/tree/classic-mac) | Classic-Mac/System-7.1-Port; offscreen Rendering und Draw-Schalter. Kurios, kaum direkte Windows-Mehrwerte. |
| [omiusgm/playpointer](https://github.com/omiusgm/playpointer) | Größerer eigenständiger Umbau; ergänzende Untersuchung unten. |
| [p4block](https://github.com/p4block/tinyhand) | Python-Wayland-Port/Arch-Packaging, sechs Sound-Previews und eigene Sounddatei. |
| [pietro-santoro](https://github.com/pietro-santoro/tinyhand/tree/windows-port) | Nativer Windows-Port, CI/Selftests, Transform-/Hook-Priority-Korrekturen. |
| [rjiang-bb/aihand](https://github.com/rjiang-bb/aihand) | Größerer eigenständiger Umbau; ergänzende Untersuchung unten. |
| [sajeeldigitali](https://github.com/sajeeldigitali/tinyhand/tree/windows) | Windows-11-Port mit CI/Selftest; wählbare Taste für echte Klicks, dokumentierte Fenster-/Input-Grenzen. |
| [sergvax](https://github.com/sergvax/tinyhand) | Click-through und separat schaltbares Zeichnen. |
| [sud868](https://github.com/sud868/tinyhand/tree/feat/gnome-extension) | GNOME-Shell-Erweiterung statt macOS-App. |
| [ThatJoeLopez](https://github.com/ThatJoeLopez/tinyhand_Windows/tree/claude/stoic-ramanujan-mqiht2) | Windows-Port inkl. eigener PNG-Decodierung und Crash-/Input-Fixes; jüngere Korrekturen nur auf Nebenbranch. |
| [tran-huy-phuc](https://github.com/tran-huy-phuc/tinyhand/tree/windows-tauri) | Rust/Tauri, Installer, Windows/macOS; Stiftbreite, Toolbar-Hinweise, Zeichnungen pro Monitor, Alt/Ctrl-Stuck-Fix. |
| [trussosw](https://github.com/trussosw/tinyhand/branches) | Separate Branches für Strichstärke/Farbtöne, Ring, Real-click und Idle-Sleep. |
| [tumaranja](https://github.com/tumaranja/tinyhand/tree/swing-range) | Schwingweite; viel zusätzliche Historie auf `main` betrifft Demos und Slider-Feinschliff. |
| [ulentini](https://github.com/ulentini/tinyhand/tree/fix/tap-sound-freeze) | macOS-Audio-Freeze-Fix beim Screensharing. |
| [xynova](https://github.com/xynova/tinyhand) | Handgesten, Line-art-Hand, Gesten-Assets/-Tests und UI-Hilfe. |
| [zebrajaeger](https://github.com/zebrajaeger/tinyhand-windows) | Eigener C#/WPF-Windows-Port; Bewertungsbasis, keine fremde Idee. |

Zusätzlich: [fduarte42](https://github.com/fduarte42/tinyhand/tree/fix/gnome-hand-render-perf)
ist ein verschachtelter Fork mit zwei GNOME-Rendering-Optimierungen gegenüber
AbdulRehman56s `linux-port`.

### Die 47 direkten Forks ohne zusätzliche Commits auf sichtbaren Branches

Repository-Namen gemäß Fork-Liste/Remote-Erfassung:

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

Das schließt private, gelöschte oder nicht als Branch veröffentlichte Arbeit nicht
aus. Auch ein auffälliges Push-Datum ist allein kein Beleg für eigene App-Änderungen.

## Vorläufige Priorisierung für diesen Windows-Port

1. **Schnelle praktische Verbesserungen:** Zeichenfarbe/-breite, Undo, optionales
   Fade, Nummernstempel und Ton nur bei echten Taps.
2. **Größter Bedienungsgewinn:** klarer Zeigen-/Zeichnen-Modus, danach kleine Toolbar.
   Normale Klicks nicht erst nach einem komplizierten Hybridmodell einführen.
3. **Besonders interessante Erweiterung:** opt-in Formerkennung für Linie/Kreis/Rechteck.
4. **Für Schulungen und Präsentationen:** Spotlight sowie Screenshot mit Markierungen.
5. **Nach Bedarf:** Gesten, eigene Hände/Sounds, Lautstärke und Stream-Deck-/CLI-Profile.
6. **Separat messen:** Rendering-/Idle-Optimierung. Recorder, KI-Funktionen und komplette
   Architekturwechsel nicht allein wegen großer Fork-Diffs übernehmen.
7. **Für kleinere Downloads:** optimierte Handbilder und Single-file-Kompression
   ausprobieren, ohne die enthaltene .NET-Runtime oder Bildqualität ungeprüft aufzugeben.

Keine Commits, Pushes oder Releases im Rahmen dieser Recherche. Quellenlinks und
Git-Snapshot sind zeitgebunden; bewegliche Branch-Links können sich später ändern.
