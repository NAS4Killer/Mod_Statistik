# NUC-Installation 0.0.2.1

Installiert am 07.10.2026 auf nuc-server, Spielversion V 3.3.0.
Server-Mod: /home/nas4killer/7dtd-server/7dtd_data/Mods/Mod_Statistik
Container-Mod: /home/sdtdserver/serverfiles/Mods/Mod_Statistik

Die Server-DLL wurde gegen die tatsaechlichen Bibliotheken unter
7DaysToDieServer_Data/Managed gebaut. Assemblyname: Mod_Statistik.

Original: /home/nas4killer/7dtd-web/bloodmoon_kills.txt
Original-TXT mit 1524 Bytes wurde bytegleich in Data/bloodmoon_kills.txt uebernommen.
SHA256: 80e00a3652535666fd98d5dff4b9c960a1649f2cb0d00edfe5d8820c82d29f1a

Sicherung: /home/nas4killer/mod-statistik-backups/20261007-093859
Enthaelt originale TXT, JSON-Zwischenstand, Kartenstatus, Updater und Installationsnachweis.

Im vorhandenen SecondMap-Updater wurde KILL_COLLECTION_ENABLED = False gesetzt.
Dieser Schalter unterbindet die alte Kill-Auswertung und Blutmond-Dateischreibvorgaenge.
SSH und Zeit-/Positionsabfragen laufen weiter. listplayers liefert technisch weiterhin
auch Zombie-Zaehlerfelder, diese werden von der pausierten Auswertung nicht gelesen.
Die Statistik liest seit 07.10.2026 direkt die Mod-Daten; Zeit und Position bleiben beim bisherigen Verfahren.

Spielserver normal ueber LinuxGSM gestoppt/gestartet. Keine Spieler waren online.
Der echte Mod-Start ist im Serverprotokoll bestaetigt; Data ist fuer den Spielserver schreibbar.
Direkt-Sync mit einem echten Spielclient wurde vom Nutzer bestaetigt. Eine neue Blutmondnacht ist noch zu testen.

## SecondMap-Anbindung

Installiert am 07.10.2026: secondmap_mod_statistics.py sowie angepasste server.py und update_state.py.
Datenquelle: Server-Mod/Data/bloodmoon_kills.txt und bloodmoon_state-*.json.
Die TXT liefert abgeschlossene Naechte; der Mod-Status liefert laufende Kills und Blutmondstatus.
Bei mehreren Statusdateien wird keine Welt geraten; eine eindeutige Zuordnung ist dann erforderlich.
Vor dem ersten aufgezeichneten Blutmond ist noch keine Statusdatei vorhanden; die TXT-Historie bleibt verfuegbar.
Gesamt-Zombiezaehler stammen aus aufgezeichneten Mod-Werten, nicht aus einer zusaetzlichen Kill-Abfrage.

Sicherung: /home/nas4killer/mod-statistik-backups/secondmap-sync-20261007-114917
Echte HTTP-Statistik geprueft: Tage 84/77, Summen 385/274. Kartenwerte LukeDeeLuxe 152, NEK4Killer 40.
Adaptertests: Historie, laufende Wertenderungen, Abschluss und fehlerhafter JSON-Status bestanden.
Die Mod-TXT wurde bei der Umstellung nicht veraendert. Beide SecondMap-Dienste laufen.

## Server-HUD 0.0.2.2

Am 07.10.2026 lokal und auf dem NUC installiert. Server-DLL gegen die NUC-Spielbibliotheken gebaut.
Die laufende eigene Killanzeige bezieht den Blutmondstatus und die Werte ueber Direkt-Sync einmal pro Sekunde.
Das Menue muss dazu nicht geoeffnet sein. Nach empfangenem Abschluss bleibt der eigene Wert 60 echte Sekunden sichtbar.
Sicherung: /home/nas4killer/mod-statistik-backups/hud-0022-20261007-115949
Automatisierter Paket-/HUD-Test bestanden; Bestaetigung im echten Server-Blutmond steht noch aus.

## Gruppen-HUD 0.0.2.3

Lokal und auf dem NUC am 07.10.2026 installiert, inklusive templates.xml fuer die originale Gruppenzeile.
Entfernung: x232, Breite112; Killspalte x360, Breite120; Stern x486.
Die eigene Gruppenzeile beginnt ueber den anderen Mitgliedern; ihr Name haelt Abstand zum Mikrofon-Symbol.
Ohne echte Gruppe erscheint eine gekennzeichnete Vorschau mit eigener roter Zahl und weissen Beispielwerten.
Beispiele dienen nur dem Layouttest und werden nicht in Statistikdateien geschrieben.
Sicherung: /home/nas4killer/mod-statistik-backups/group-hud-0023-20261007-133030
Native XPath-Ziele, Spaltenabstaende, Gruppenrangfolge, Gleichstaende und Nullwerte automatisiert geprueft.
Sichtpruefung im laufenden Spiel und Entfernung der Vorschau stehen noch aus.

## Gruppen-Vorschau 0.0.2.4
Originale party_entry-Zeilen aus den installierten Spieltemplates uebernommen. Datenbindungen fuer den Layouttest durch Beispiele ersetzt; native Rahmen, Balken und Symbole erhalten. Rangsterne verwenden ui_game_symbol_star als Sprite.
Lokal und NUC installiert; visuelle Bestaetigung durch den Nutzer steht aus.

## Native Gruppen-Vorschau 0.0.2.5
Die Vorschau verwendet nun XUiC_PartyEntry.SetPlayer mit isolierten Testobjekten und originalen Datenbindungen. Textformatierung, Distanzfarbe und Pfeildrehung erfolgen im nativen Controller. Keine Eintragung der Testobjekte in die Welt oder die echte Gruppe. Sternposition aus gerenderter Zahlenbreite plus acht Pixeln. Build gegen Client- und NUC-Bibliotheken bestanden; Funktion im laufenden Spiel ist noch zu bestaetigen.

## Vorschau-Namenskorrektur 0.0.2.6
Nach Fehlerbild GetBindingValue name fuer previewEntry0 bis previewEntry4 wurde ausschliesslich die name-Bindung der Testobjekte mit direkten Testnamen versorgt. Echte Spieler verwenden weiterhin die native Namensabfrage. Build gegen Client- und Serverbibliotheken bestanden. Bestaetigung der Fehlerfreiheit im laufenden Spiel steht aus.
