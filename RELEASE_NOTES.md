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

# Mod_Statistik 0.0.0.3

Eigene Release Notes: ReleaseNotes-0.0.0.3.txt. Erste kompilierte Mod mit Blutmond-Tab, Doppel-I und lokalem TXT-Import; Spieltest steht aus.

# Mod_Statistik 0.0.0.2

Status: Entwicklungsstand; keine installierbare Mod und kein Release-Paket.

- TXT-Auswertung fuer abgeschlossene Blutmondnaechte mit Spieltag, Spielern und Nachtsummen implementiert.
- Erste abgeschlossene Aufzeichnung je Spieltag bleibt erhalten; doppelte Aufzeichnungen werden ignoriert.
- Fehlende Werte und tatsaechliche Nullwerte werden unterschieden. EntityPlayer wird ausgeschlossen.
- Hintergrunddownload, gespeicherte URL und quellengebundener Cache implementiert. Die vorbereitete VPS-Adresse ist im Code vorbelegt.
- Eigener TXT-Download-Dienst auf dem VPS eingerichtet; Upload und lokaler HTTP-200-Abruf geprueft. Oeffentlicher Download noch nicht bestaetigt.
- TXT mit Tag 84 = 385 und Tag 77 = 274 sowie Fehlerseiten, Neustart-Cache und Quellenwechsel getestet.

Noch offen: Spiel-Menueintegration, Doppel-I, Mod-DLL, Paket und Spielpruefung. Keine automatische Aktualisierung der TXT auf dem VPS.

# Mod_Statistik 0.0.0.1

Status: Vorbereitung, kein installierbares Release.

- Git-Projekt und Versionsdatei angelegt.
- Vereinbarter Umfang fuer Blutmond-Tab, Doppel-I und TXT-Download dokumentiert.
- gitg-Eintrag mit Sicherung und GLib-Pruefung ergaenzt; bestehende Eintraege erhalten.