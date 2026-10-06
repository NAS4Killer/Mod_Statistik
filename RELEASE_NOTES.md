Mod_Statistik 0.0.2.0 - Fehlerkorrektur und 60 Sekunden Nachanzeige

- Die rote eigene Kill-Zahl bleibt nach dem Blutmond 60 echte Sekunden sichtbar.
- Angezeigt wird der endgueltig erfasste Kill-Wert; anschliessend wird die Zahl ausgeblendet.
- Beim Weltwechsel wird die Nachanzeige zurueckgesetzt; eine neue Nacht zeigt ihre eigenen Werte.
- Die Realzeitberechnung verwendet die Tagesdauer ohne den fehlerhaften GetFloat-Zugriff auf TimeOfDayIncPerSec.
- TODO-Liste ergaenzt: Servereinrichtung, SecondMap-SSH-Abfragen abschalten bzw. umstellen und Server-HUD.

Pruefung: Build gegen installierte Spielbibliotheken und automatisierte Grenzwertpruefungen fuer 60 Sekunden.
Der Nutzer hat bestaetigt: Fehlermeldungen verschwunden und 60 Sekunden Nachanzeige funktionieren im Spiel.
