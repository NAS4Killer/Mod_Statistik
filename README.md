# Mod_Statistik 0.1.0.0 - Direkt-Sync

Dieser Zweig uebertraegt Blutmondstatistik ueber die bestehende Spielverbindung.
Im Einzelspieler und beim Gastgeber liegt die TXT unter
`Mods/Mod_Statistik/Data/bloodmoon_kills.txt`. Auf einem dedizierten Server gilt
derselbe relative Pfad innerhalb seiner Mod-Installation.
Die Mod erstellt eine fehlende TXT und zeichnet neue Blutmondnaechte selbst auf.
Sie liest die Spielzeit im laufenden Spiel und beginnt beim konfigurierten
Blutmondstart (normal 22 Uhr). Dann erfasst sie die Zombie-Kill-Startwerte
und zaehlt die Zuwachse bis zum konfigurierten Ende (normal 4 Uhr).
Ausserhalb der Nacht werden fuer die Aufzeichnung keine Kill-Zaehler gelesen.

Zwischenstaende liegen unter Data/bloodmoon_state-<Weltkennung>.json,
werden bei Aenderungen alle 5 echten Sekunden und beim Ausloggen/Weltende
gesichert und nach einem Neustart fortgesetzt. Spaetere Beitritte beginnen
mit dem dann vorhandenen Gesamtzaehler. Werte ausgeloggter Spieler bleiben
erhalten; ein Zaehlerreset zieht keine bereits gezaehlten Kills ab.
Der END-Block entspricht dem vorhandenen TXT-Format. Abgeschlossene Tage
werden nicht doppelt geschrieben. Das Protokoll bleibt eine Datei im
Mod-Ordner; Tage bereits vorhandener Historie werden dabei beibehalten.

Fehlerhafte TXT-Dateien werden vor dem Neuanlegen unveraendert als
bloodmoon_kills.txt.invalid-*.bak gesichert. Beschadigte JSON-Zwischenstaende
und Schreibfehler stoppen die Aufzeichnung mit einem Hinweis.
Mod-Updates erhalten den gesamten vorhandenen Data-Ordner.
Wird die Mod mitten in einer Nacht aktiviert, beginnt die Zaehlung erst dann.
Bei einem Absturz koennen seit der letzten Sicherung entstandene Kills fehlen.
Die externe Server-Aufzeichnung darf nicht gleichzeitig in dieselbe TXT schreiben.


Server und Spieler brauchen dieselbe Direkt-Sync-Version dieser Mod.
Bei verbundenen Spielern liest die Mod keine eigene lokale Statistikdatei.
Beim Oeffnen des Blutmond-Tabs und alle 5 Sekunden bei offenem Tab fragt sie
den Server automatisch an. Empfangene Werte bleiben im Arbeitsspeicher.
Unveraenderte Werte werden mit einer kurzen Antwort bestaetigt.
Eine vollstaendige neue Statistik wird in begrenzten Teilpaketen uebertragen
und vor der Anzeige mit SHA256 geprueft. Bei Fehlern bleiben die letzten
gueltigen Werte sichtbar; beim Weltwechsel wird dieser Zwischenspeicher geleert.

Spieler stehen untereinander, Rekord und Tage oben. Hoechstwerte und
Rekordhalter sind rot markiert, einschliesslich Gleichstaenden.
Die zentrierte Zeile unter der Tabelle zeigt den naechsten Blutmond und
verbleibende Spielzeit sowie geschaetzte Realzeit aus dem Spielzustand.
Seitenwechsel stehen unten rechts im Stil des Originalmenues.

## Installation und Updates

Bei beendetem Spiel bzw. Server das ZIP in dessen Mods-Ordner entpacken.
Vorhandene echte Protokolldaten sichern und wieder in Data/bloodmoon_kills.txt
ablegen. Die mitgelieferte Datei ist ein alter Datenstand fuer die Anzeige; neue Naechte werden ergaenzt.

GitHub-Updates verwenden Config/UpdateRepository.txt und ausschliesslich
Release-Tags `direkt-sync-vA.B.C.D` mit
`Mod_Statistik-direkt-sync-vA.B.C.D.zip`. Die Kennung SYNC_MODE verhindert
Paketwechsel zum URL-Zweig. Der Installer erhaelt die vorhandene TXT und
Updateadresse und wartet auf das Ende des Spiels. Release Notes bzw. der
Neustarthinweis stehen links neben dem Updatebutton.

Pruefungen: erfolgreicher Build gegen die Spielbibliotheken; automatisierte
Protokoll- und simulierte Server-/Client-Pruefungen. Der gemeinsame Test mit
einem laufenden Spielserver steht noch aus.

Die Gastgeber-/Client-Rolle wird beim Weltwechsel bestimmt und fuer die Sitzung gespeichert.

Automatische Aufzeichnung durch automatisierte Ablauf- und Dateifehlerpruefungen geprueft.
Ein Test waehrend eines echten Blutmonds steht noch aus.

## Laufende Anzeige

Waehrend der Nacht werden Kill-Zaehler regulaer einmal pro echter Sekunde gelesen.
Die laufende Nacht steht in der Tabelle; nur der geoeffnete Tab wird sekuendlich aktualisiert.
Oben links im lokalen Spiel erscheint waehrend der Nacht die eigene Kill-Zahl rot.
Die HUD-Anzeige verwendet die lokale Aufzeichnung und liest keine eigenen Kill-Zaehler.
Ausloggen und Weltende sichern zusaetzlich den letzten Stand.

Die Realzeitberechnung verwendet die Tagesdauer; kein GetFloat-Zugriff auf TimeOfDayIncPerSec.

Die rote lokale Kill-Zahl bleibt nach dem Blutmond 60 echte Sekunden mit dem Endwert sichtbar.

## Dedizierter Server

Der dedizierte Spielserver zeichnet auch ohne geoeffnetes Spielermenue auf.
Die TXT liegt im Server-Mod-Ordner unter Data/bloodmoon_kills.txt.
Vor dem ersten Start wird die originale SecondMap-TXT gesichert und an diesen Pfad uebernommen.
Die alte Kill-Aufzeichnung muss bei der Umstellung angehalten werden; SSH bleibt aktiv.
Ein Client wartet auf die Verbindung und erzeugt selbst keine Aufzeichnungsdatei.
NUC-Installation und echter Direkt-Sync sind bestaetigt; der echte Gruppentest waehrend eines Blutmonds steht noch aus.

Die rote eigene Killzahl funktioniert lokal und ueber Direkt-Sync auf einem Server.
Waehrend des Blutmonds fordert der Client auch bei geschlossenem Menue einmal pro echter Sekunde die Werte an.
Nach Empfang des Abschlusses bleibt der endgueltige eigene Wert 60 echte Sekunden sichtbar.
Der uebertragene Blutmondstatus benoetigt Version 0.1.0.0 auf Server und Clients.
