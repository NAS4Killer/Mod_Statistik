# Release Notes

## 0.1.0.0

Mod_Statistik 0.1.0.0 - Server- und Gruppenanzeige

- Dedizierte Server zeichnen laufende und abgeschlossene Blutmondnächte auf und übertragen die Statistik über die Spielverbindung.
- Die eigene rote Killzahl funktioniert auch auf dem Server. Während des Blutmonds werden Werte sekündlich angefordert, auch bei geschlossenem Menü; der Endwert bleibt 60 echte Sekunden sichtbar.
- Originale Gruppenzeilen erhalten eine linksbündige Killspalte und native Rangsterne. Eigene Zahlen sind rot, andere weiß; bei Gleichstand werden alle Führenden markiert. Sterne stehen acht Pixel hinter der jeweiligen Zahl.
- Die Testvorschau und simulierten Spieler wurden entfernt.
- Der Updater unterstützt die Gruppen-Template-Datei. Der Wechsel von älteren installierten Updatern erfordert eine manuelle Installation aus dem ZIP.
- SecondMap liest die Mod-Daten statt einer eigenen Kill-Aufzeichnung; diese externe Anbindung ist vom Mod-Paket getrennt.
- Screenshot1 dokumentiert unverändert den bestätigten Tabellenstand 0.0.2.7.

Geprüft: Client- und Serverbuilds, automatisierter Direkt-Sync mit acht Spielern, HUD-Abschluss und 60-Sekunden-Grenze, Gruppenrangfolge und Gleichstände. Echter NUC-Start und Statistikübertragung sind bestätigt.
Offen: echter Gruppentest beim Blutmond, 60 Sekunden Nachanzeige und Neustart.

## 0.0.2.0

Mod_Statistik 0.0.2.0 - Fehlerkorrektur und Anzeige nach dem Blutmond

- Die eigene rote Killzahl bleibt nach Ende des Blutmonds mit dem endgültigen Wert 60 echte Sekunden sichtbar.
- Bei Weltwechsel wird die Anzeige zurückgesetzt; eine neue Nacht verwendet ihre eigenen Werte.
- Der fehlerhafte Zugriff auf TimeOfDayIncPerSec wurde entfernt; die Realzeit wird über die Tagesdauer berechnet.

Geprüft: Build und automatisierte 60-Sekunden-Grenztests. Der Nutzer bestätigt, dass Fehlermeldungen verschwunden sind und die Anzeige nach dem Blutmond funktioniert.

## 0.0.1.0

Mod_Statistik 0.0.1.0 - Direkt-Sync und automatische Statistik

- Statistik im Mod-Ordner mit Übertragung über die Spielverbindung; URL-Leiste, Server/Lokal-Auswahl und Doppel-I entfallen.
- Automatische Blutmondaufzeichnung mit sekündlicher Erfassung während der Nacht, gesicherten Zwischenständen und Ergänzung abgeschlossener Tage im TXT-Format.
- Tabellenansicht mit Spielern untereinander, Rekorden und absteigend sortierten Tagen; Höchstwerte sind rot markiert.
- Eigene rote Killzahl im lokalen Spiel sowie laufende Werte in der Tabelle.
- Beitritte, Ausloggen, Neustarts und Zählerresets werden berücksichtigt; beschädigte TXT-Dateien werden vor dem Neuanlegen gesichert.
- Direkt-Sync prüft begrenzte Datenpakete per SHA256. Der Updater verwendet ausschließlich passende Direkt-Sync-Releases.

Geprüft: Builds, automatisierte Aufzeichnungs- und Pakettests sowie lokale Blutmondaufzeichnung und Tabelle im Spiel.
