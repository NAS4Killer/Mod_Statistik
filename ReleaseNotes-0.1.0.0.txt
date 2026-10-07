Mod_Statistik 0.1.0.0 - Direkt-Sync und Gruppen-Killanzeige

- Dedizierte Server zeichnen Blutmond-Kills auf und uebertragen laufende sowie abgeschlossene Naechte ueber die Spielverbindung. Clients warten auf die Verbindung und schreiben keine eigene Serverstatistik.
- Die rote eigene Killzahl funktioniert auch auf dem Server und wird waehrend des Blutmonds sekündlich aktualisiert, auch bei geschlossenem Statistikmenue.
- Nach dem empfangenen Blutmondabschluss bleibt der eigene Endwert 60 echte Sekunden sichtbar.
- Originale Gruppenzeilen erhalten eine linksbuendige Killspalte: eigene Werte rot, andere weiss. Ein roter Stern markiert den hoechsten positiven Gruppenwert, bei Gleichstand alle Fuehrenden.
- Rangsterne verwenden native Spielgrafiken und stehen acht Pixel hinter der gerenderten Killzahl. Testvorschau und simulierte Spieler sind entfernt.
- SecondMap liest die Mod-Statistik; ihre alte Kill-Aufzeichnung ist deaktiviert. Zeit- und Positionsabfragen bleiben bestehen.
- Screenshot1.png dokumentiert den bestaetigten Tabellenstand 0.0.2.7 und bleibt unveraendert.

Pruefungen: Client- und Serverbuilds, automatisierter Direkt-Sync mit acht simulierten Spielern, HUD-Abschluss und 60-Sekunden-Grenze, Gruppenrangfolge und Gleichstaende. Echter NUC-Start und Direkt-Sync vom Nutzer bestaetigt.
Noch offen: echter Gruppentest waehrend eines Blutmonds, 60 Sekunden Nachanzeige und Neustart.
Diese Veroeffentlichung installiert keine neue Version auf PC oder NUC.

- Der Updater akzeptiert und installiert jetzt auch das Gruppen-Template. Aeltere installierte Updater ohne diese Korrektur benoetigen fuer diesen Stand eine manuelle Installation aus dem ZIP.
