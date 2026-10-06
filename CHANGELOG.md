# 0.0.1.0

Mod_Statistik 0.0.1.0 - Direkt-Sync und automatische Blutmondstatistik

- Eigener Zweig direkt-sync: Statistikdatei im Mod-Ordner, Uebertragung ueber die Spielverbindung.
- URL-Leiste, Speichern, Server/Lokal-Auswahl und Doppel-I entfallen.
- Lokales Spiel und Server erstellen die TXT automatisch und zeichnen Blutmondnaechte auf.
- Die Spielzeit wird direkt gelesen; Kill-Zaehler werden waehrend der Nacht regulaer einmal pro echter Sekunde abgefragt.
- Oben links erscheint im lokalen Spiel die eigene Kill-Zahl waehrend des Blutmonds in Rot.
- Die laufende Nacht erscheint in der Statistik; der geoeffnete Tab wird sekuendlich aktualisiert.
- Spieler stehen untereinander, Rekord und Tage oben; Hoechstwerte werden rot markiert.
- Tage bleiben absteigend nach Tagesnummer sortiert, einschliesslich vorhandener Historie.
- Zwischenstaende werden gesichert; Neustart, Beitritte, Ausloggen und Zaehlerresets werden beruecksichtigt.
- Fertige Naechte werden im bisherigen TXT-Format ergaenzt; vorhandene Daten bleiben erhalten.
- Fehlerhafte TXT-Dateien werden vor dem Neuanlegen unveraendert gesichert.
- Rolle wird einmal je Welt bestimmt; Direkt-Sync nutzt begrenzte Pakete, SHA256 und Anfragebegrenzung.
- Release Notes und Neustarthinweis stehen links neben dem Updatebutton; Version und Blutmond-Restzeit sind zentriert.
- Der fehlerhafte GetFloat-Zugriff auf TimeOfDayIncPerSec wurde entfernt; Realzeit wird aus der Tagesdauer berechnet.
- Der Updater prueft ausschliesslich direkt-sync-Releases und die passende Paketkennung.

Pfade: Data/bloodmoon_kills.txt und Data/bloodmoon_state-<Weltkennung>.json im Mod-Ordner.
Die rote HUD-Anzeige verwendet derzeit die lokale Aufzeichnung.
Server und Clients benoetigen denselben Direkt-Sync-Stand. Eine externe Aufzeichnung darf nicht gleichzeitig dieselbe TXT beschreiben.
Bei Einstieg mitten in der Nacht beginnt die Zaehlung erst dann; bei Absturz kann der noch ungesicherte Zeitraum fehlen.

Pruefung: erfolgreicher Build gegen die installierten Spielbibliotheken;
automatisierte Aufzeichnungs-, Live-Anzeige-, Dateifehler-, Paket- und simulierte Server-/Client-Pruefungen.
Lokale Blutmond-Aufzeichnung und Tabelle wurden vom Nutzer erfolgreich im Spiel getestet.
Der abschliessende Datentyp-Fix wurde gebaut und durch die vorhandenen Realzeitberechnungspruefungen abgedeckt.

# 0.0.0.14

Mod_Statistik 0.0.0.14 - Sekuendliche Kill-Abfrage und Live-Anzeige

- Kill-Zaehler werden waehrend des Blutmonds regulaer einmal pro echter Sekunde gelesen.
- Beim Beginn werden die Startwerte sofort erfasst; Ausloggen und Weltende sichern weiterhin den letzten Stand.
- Oben links im lokalen Spiel erscheint waehrend des Blutmonds die eigene Kill-Zahl in Rot, auch bei geschlossenem Statistikmenue.
- Die laufende Nacht wird in der Tabelle angezeigt; geoeffnete Tabellen werden sekuendlich aktualisiert.
- Seitenwechsel bleiben bei neuen Kill-Werten erhalten.
- Laufende Werte liegen im Speicher; die bisherige 5-Sekunden-Sicherung und der END-Block am Nachtende bleiben erhalten.
- Direkt-Sync-Antworten enthalten ebenfalls die laufende Nacht.

Die rote HUD-Zahl nutzt in dieser Version die lokale Aufzeichnung.
Pruefungen: Build gegen Spielbibliotheken; simulierter Beginn, sekuendlicher
Abstand, Live-Tabellenwerte und Ende mit erhaltener fertiger Nacht bestanden.
HUD-Position und Darstellung muessen im Spiel visuell geprueft werden.

# 0.0.0.13

Mod_Statistik 0.0.0.13 - Automatische Blutmond-Aufzeichnung

- Lokales Spiel und Server erstellen Data/bloodmoon_kills.txt im Mod-Ordner automatisch.
- Die Mod liest die Spielzeit direkt im Spiel und beginnt zum eingestellten Blutmondstart (normal 22 Uhr).
- Start-, Zwischen- und Endwerte stammen aus den vorhandenen Zombie-Kill-Zaehlern des Spiels.
- Ausserhalb des Blutmonds werden keine Spieler-Kill-Zaehler fuer die Aufzeichnung gelesen.
- Das Ende der Nacht (normal 4 Uhr) schreibt einen END-Block im vorhandenen TXT-Format.
- Spaetere Beitritte, ausgeloggte Spieler und zurueckgesetzte Gesamtzaehler werden beruecksichtigt.
- Zwischenstaende werden bei Aenderungen alle 5 echten Sekunden sowie beim Ausloggen und Weltende gesichert.
- Gesicherte Zwischenstaende werden nach einem Neustart fortgesetzt; bereits abgeschlossene Tage werden nicht doppelt geschrieben.
- Fehlerhafte TXT-Dateien werden vor dem Neuanlegen unveraendert als .invalid-*.bak gesichert.
- Verbundene Clients schreiben keine Statistikdatei; die Direkt-Sync-Uebertragung bleibt erhalten.

Zwischenstaende: Data/bloodmoon_state-<Weltkennung>.json. Dateien bleiben bei Mod-Updates erhalten.
Beschadigte Zwischenstaende oder Schreibfehler stoppen die Aufzeichnung mit einem Hinweis.
Bei erstmaliger Aktivierung mitten in einer Nacht kann nur ab diesem Zeitpunkt gezaehlt werden.
Bei einem Absturz kann die seit der letzten Sicherung noch ungesicherte Zeit fehlen.
Die bisherige externe Server-Aufzeichnung darf nicht gleichzeitig dieselbe TXT beschreiben.

Pruefungen: Build gegen installierte Spielbibliotheken; automatisierte Start-/End-,
Neustart-, Beitritts-, Offline-, Zaehlerreset-, Duplikat- und Dateifehlerpruefungen;
simulierte direkte Server-/Client-Uebertragung bestanden.
Der Test waehrend eines echten Blutmonds steht noch aus.

# 0.0.0.12

Mod_Statistik 0.0.0.12 - Direkt-Sync

- Gastgeber-/Client-Rolle wird beim Wechsel der Spielwelt bestimmt.
- Die bestimmte Rolle bleibt fuer die Sitzung gespeichert.
- Laufende Updates und Statistikanfragen verwenden diese gespeicherte Rolle.
- Beim Verlassen und Beitreten einer Welt werden Statistikcache und Anfragen zurueckgesetzt.

Pruefung: Spielbibliotheken-Build und simulierte Server-/Client-Uebertragung;
100 Spielupdates ohne erneute Rollenabfrage sowie erneute Bestimmung beim Weltwechsel.
Die automatische Kill-Aufzeichnung ist weiterhin noch nicht eingebaut.

# 0.0.0.11 - Direkt-Sync

Mod_Statistik 0.0.0.11 - Direkt-Sync

- Eigener Entwicklungszweig direkt-sync fuer Statistik ueber die Spielverbindung.
- Einzelspieler und Gastgeber lesen Data/bloodmoon_kills.txt im Mod-Ordner.
- Verbundene Spieler erhalten die ausgewerteten Werte von der Server-Mod.
- Automatische Anfrage beim Oeffnen des Tabs und alle 5 Sekunden bei offenem Tab.
- Unveraenderte Werte werden bestaetigt, ohne die ganze Statistik erneut zu senden.
- TXT-URL, Speichern und Server / Lokal entfallen; kein VPS erforderlich.
- Begrenzte Teilpakete, Pruefsumme, Anfragebegrenzung und Erhalt letzter gueltiger Werte bei Fehlern.
- Updates beruecksichtigen ausschliesslich Direkt-Sync-Releases und pruefen die Paketkennung.

Voraussetzung: dieselbe Direkt-Sync-Version auf Server und Clients.
Die Serverdatei muss im bestehenden Blutmond-Protokollformat vorliegen.
Diese Mod liest das Protokoll; sie erzeugt keine neuen Kill-Eintraege.

Pruefung: gegen installierte Spielbibliotheken gebaut; automatisierter simulierter
Server-/Client-Ablauf einschliesslich Mehrteilpaketen, Cache, Fehlern und Weltwechsel bestanden.
Ein gemeinsamer Test mit laufendem Spielserver steht noch aus.

# 0.0.0.10

Mod_Statistik 0.0.0.10
Release-Notes-Button direkt links neben den Updatebutton verschoben.
Wenn der Release-Notes-Button nach dem Installationsauftrag verschwindet, erscheint links neben dem Updatebutton der rote Hinweis Spiel neu starten.
Doppel-I-Tastenkombination samt Hotkey-Komponente und Eingabeauswertung entfernt.
Blutmond-Tab bleibt ueber das normale Spielermenue erreichbar.
Realzeitkorrektur und zentrierte Blutmondanzeige aus 0.0.0.9 enthalten.
Nur lokal erstellt und installiert; kein Commit und kein GitHub-Upload.

# 0.0.0.9

Mod_Statistik 0.0.0.9
Realzeitberechnung korrigiert: Falls der Client keine direkte Zeitgeschwindigkeit bekommt, wird sie aus der eingestellten Dauer eines Spieltags berechnet.
Gesamte feste Blutmond-Informationszeile horizontal zentriert.
Enthaelt die vorher lokal getesteten Aenderungen aus 0.0.0.8: kurzer GitHub-Button, zentrierte Version, eingerichtete GitHub-Updatequelle und feste Anzeige von Blutmondtag, Ingame-Restzeit und Realzeit.
Countdown verwendet den Spiel-/Servertermin und beruecksichtigt angepasste Nachtzeiten.

# 0.0.0.8

Mod_Statistik 0.0.0.8
GitHub-Button oben rechts von 205 auf 105 Pixel verkuerzt; Standardbeschriftung GitHub.
Versionsanzeige mittig ueber die gesamte Kopfzeile.
Updatequelle NAS4Killer/Mod_Statistik eingerichtet; Updatequelle fehlt entfernt.
Feste Informationszeile unter den Spielerzeilen: naechster Blutmondtag samt Startzeit, verbleibende Spieltage/Stunden/Minuten und geschaetzte Realzeit.
Termin und Geschwindigkeit aus dem aktuellen Spiel-/Serverzustand; angepasste Nachtzeiten werden beruecksichtigt.
Laufender Blutmond und noch unbekannter Termin werden gesondert angezeigt.
Zwoelf Spielerzeilen pro Seite, damit die feste Blutmondanzeige in den bisherigen Menurahmen passt.
Commit und Release 0.0.0.7 bereits veroeffentlicht; 0.0.0.8 bleibt bis zum naechsten beauftragten Commit lokal.

# 0.0.0.7

Mod_Statistik 0.0.0.7
Updatebutton oben rechts neben der Version nach dem MiniMap-Ablauf.
Automatische Releasepruefung beim Oeffnen; Download und Installieren jeweils durch Buttonklick.
Verfuegbare Updates gruen; Release Notes zur angebotenen Version.
SHA256, Dateiliste, Versionsnummer und Mod-Metadaten werden vor Installation geprueft.
Versteckter Installer wartet auf Spielende, sichert Dateien und stellt sie bei Fehlern wieder her.
Lokale Statistikdatei und Updatequellen-Konfiguration bleiben bei Updates erhalten.
GitHub-Updatequelle noch nicht hinterlegt: Config/UpdateRepository.txt. Bis dahin zeigt der Button Updatequelle fehlt.

# 0.0.0.6

Mod_Statistik 0.0.0.6
Namensspalte auf 180 Pixel verkleinert, lange Namen passen durch ShrinkContent.
Elf Blutmond-Tage pro Seite; Rekordspalte direkt nach dem Namen.
TXT-URL und Quellenhinweis in der Fussleiste vertikal mittig ausgerichtet.
Button Server / Lokal als deaktivierte Vorbereitung; Umschaltfunktion folgt spaeter.
Aktuelle lokale Quelle wird konkret als Data/bloodmoon_kills.txt angezeigt.
Keine Server-URL eingerichtet; das URL-Feld zeigt eine gespeicherte Serveradresse, sobald eine eingerichtet wurde.

# 0.0.0.5

Mod_Statistik 0.0.0.5
Originalfarben der Zeilen: darkGrey 64/64/64 und darkestGrey 32/32/32 mit voller Ausgangsdeckkraft; Transparenz durch das Spiel.
Originale Kopfzeilen-Innenflaeche und native Seitenanzeige mit Zahlkasten und Pfeilen unten rechts.
Tagesspalten auf 95 Pixel verdichtet; zehn Tage je Seite.
Gespeicherte URL beim Oeffnen sichtbar. Ungespeicherte Aenderung zeigt die noch aktive Quelle zusaetzlich an.

# 0.0.0.4

Mod_Statistik 0.0.0.4
Spieler als Zeilen, Blutmond-Tage als Spalten, Rekord vor den Tagen.
Hoechstwert je Blutmond und hoechster Spielerrekord rot; Gleichstaende ebenfalls rot.
Rekorde aus allen protokollierten Naechten, unabhaengig von der angezeigten Seite.
Originalmenue: 13 Zeilen mit 46 Pixeln, Rahmen, graue Flaechen und Fussleiste.
Automatisches Laden beim Oeffnen und alle 60 Sekunden bei offenem Tab.
TXT-Laden und Aktualisieren entfernt. Speichern setzt die URL und laedt automatisch.
Tabbeschriftung Blutmond korrigiert; Navigation nur bei mehreren Seiten.

# Changelog

## 0.0.0.3

Erste Mod-DLL, zweiter Blutmond-Tab, Doppel-I, lokaler Import und optionales URL-Feld. VPS als Downloadquelle verworfen; URL startet leer.

## 0.0.0.2

TXT-Auswertung, Hintergrunddownload und Cache.

## 0.0.0.1

Git-Projekt, Versionsdatei und gepruefter gitg-Eintrag.
