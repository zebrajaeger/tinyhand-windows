# TinyHand Stream-Deck-Plugin

[English](README.md) | **Deutsch** · [Windows-Anleitung](../windows/README.md)

Optionales Windows-Plugin für Stream Deck **7.1+** mit sieben Tastenaktionen:
Zeiger ein/aus, Größe (zyklisch oder fest klein/mittel/groß), Ton ein/aus,
RGB-Zeichenfarbe pro Taste, freie Texteingabe, Monitor-Screenshot und Alles löschen.
Keine Drehregler, Gedrückthalten-Funktion, automatischer App-Start oder Marketplace-Veröffentlichung.

## Bauen und installieren

Veröffentlichte TinyHand-EXEs enthalten das passende Plugin. Wird kompatible
Stream-Deck-Software gefunden (auch geschlossen und ohne Gerät), bietet TinyHand
bei fehlendem oder älterem Plugin Installation/Update an. **Später** pausiert alle
Angebote sieben Tage; **Nicht mehr fragen** unterdrückt auch spätere Updates.
Im Tray bleiben **Stream-Deck-Plugin …** und **Stream-Deck-Angebote beim Start**
verfügbar. Wiederaktivieren gilt für den nächsten Start. Neuere installierte Plugins
werden nicht heruntergestuft. Unklare/zu alte Installationen führen zu keinem Popup.

TinyHand öffnet nach Zustimmung das eingebettete Paket in Stream Deck; dessen
Bestätigungsdialog bleibt deine Entscheidung. Öffnen bedeutet noch nicht Erfolg:
TinyHand prüft bis zu 60 Sekunden die installierte Version. Kein Download, keine
Profiländerung, kein automatischer Zeigerstart. Das geprüfte Paket liegt unter
`%LOCALAPPDATA%\TinyHand\StreamDeckPlugin\<Version>\`. Eine allein verschobene EXE
funktioniert weiterhin. Unregistrierte portable Stream-Deck-Kopien werden nicht
durch eine allgemeine Festplattensuche erkannt.

Installationsstatus und Laufzeitverbindung sind getrennt: Ein passendes installiertes
Manifest beweist weder ein angeschlossenes Gerät noch eine aktuelle Verbindung zu
TinyHand. Nach Abbruch oder Zeitablauf kannst du über **Stream-Deck-Plugin …** den
Status aktualisieren und manuell erneut versuchen; TinyHand wiederholt die Übergabe
nicht automatisch.

Die Datei `plugin-version.json` ist die maßgebliche Plugin-Version. Paketierung
erzeugt Paket und SHA-256-/Versionsmetadaten zum Einbetten. App-only-Entwicklungsbuilds
ohne diese Ressourcen erklären, dass die Installation nicht verfügbar ist; sie
verwenden keine zufällige alte Paketdatei und laden nichts nach.

Entwicklung benötigt Node.js **24+**, npm und Windows. In diesem Verzeichnis:

```powershell
npm ci --ignore-scripts
npm run typecheck
npm test
npm run package
```

Ausgabe: `out/io.github.zebrajaeger.tinyhand.streamDeckPlugin`. Diese Datei über
den lokalen Plugin-Installer von Stream Deck installieren (üblicherweise per
Doppelklick), danach TinyHand separat starten. Installation bleibt eine Nutzeraktion;
Build/Paketierung installieren und veröffentlichen nichts. Nutzer benötigen kein
separates Node: Stream Deck stellt die Plugin-Laufzeit. Der portable Windows-Build
bleibt unabhängig von npm und Stream-Deck-Werkzeugen.

Aktionen aus **TinyHand für Windows** auf Tasten ziehen. Größe im Einstellungsbereich
zyklisch oder fest wählen, für Farbtasten einen RGB-Wert einstellen. Text startet
Positionsklick und freie Eingabe in TinyHand statt eines Textbausteins. Screenshot
kopiert den Mausmonitor mit fertigen Anmerkungen und ohne Hand in die Zwischenablage;
danach in ein bildfähiges Programm einfügen. Löschen funktioniert auch ohne Zeiger.
Farbwechsel gelten nur für neue Elemente.

## Anzeige und Verbindung

Alle Tasten behalten das schräge Kaktus-Anwendungslogo als Hintergrund; ein kleines
Aktionssymbol kennzeichnet die Funktion. Beschriftung und Auswahlmarkierung zeigen
den Zustand, ohne das Logo durch einen vollflächigen grünen Hintergrund zu ersetzen.
Tasten folgen echten App-Zuständen einschließlich Tray-/Tastaturänderungen.
`× Offline` bedeutet nicht verfügbar, nicht ausgeschaltet; `! Version` bedeutet
inkompatibles Protokoll und `! Einstellung` eine ungültige Tastenkonfiguration.
Auslassungspunkte zeigen einen laufenden Auftrag; Stream-Deck-Warnungen melden
Belegt/Fehler/Speicherwarnungen. Nach unklarem Verbindungsabbruch wird kein Befehl
wiederholt. Wiederverbinden aktualisiert den Zustand, startet oder aktiviert aber
TinyHand nicht. Bei älteren Apps ohne Protokoll v1 die passende TinyHand-Version starten.

Die Brücke ist eine benutzer-/sitzungsgebundene Windows-Named-Pipe, kein TCP-Server.
Nur eine feste Befehlsliste ist erlaubt; Anmerkungstext und Bilddaten fehlen in
Zustandsmeldungen. Andere Benutzer/Sitzungen werden abgewiesen. Das schützt **nicht**
vor Schadsoftware, die bereits als derselbe Benutzer läuft. App und Plugin müssen
in derselben interaktiven Windows-Sitzung laufen; nicht nur eine Seite erhöhen.

## Abhängigkeiten und Sicherheit

Abhängigkeiten und Lockdatei sind festgelegt. Das aktuelle Elgato-SDK importiert
über `@elgato/utils` bereits `zod/v4-mini`, deklariert aber einen alten verwundbaren
Zod-Bereich. Ein gezieltes npm-Override legt **Zod 4.6.7** fest (Behebung von
GHSA-mrhx-c7xc-m3w5). SDK-Import/Aktionsregistrierung, Build und Paketprüfung werden
getestet. Override prüfen/entfernen, sobald Elgato die Abhängigkeit aktualisiert.
Kein blindes SDK-Downgrade durch `npm audit fix --force`.

## Noch auf Hardware zu prüfen

Tests prüfen Befehle, Anzeigen, echte Pipe-Verbindung, begrenzte Warteschlange,
Verbindungsabbruch ohne Wiederholung und .NET/Node-Zusammenspiel. Sie beweisen keine
Installation oder physische Tastenanzeige im laufenden Stream Deck. Alle Tasten,
mehrfache gleiche Tasten, Tray-/Hotkey-Änderungen, Neustart, Ab-/Anstecken, Text/IME/
Fokus, Clipboard-Einfügen, gemischte DPI und den ersten hörbaren Headset-Klick nach
3–5 Sekunden Pause manuell prüfen. Ohne Prüfung keinen Erfolg behaupten.

Ursprüngliches Konzept/Grafiken: TinyHand von Kamran Ahmed/nilbuild. MIT-Lizenz
des Projekts und Lizenzen der Abhängigkeiten gelten weiter; SDK-Lizenzkommentare
bleiben im Bundle erhalten. Die Windows-EXE bleibt unsigniert.
