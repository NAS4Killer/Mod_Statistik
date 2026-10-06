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
