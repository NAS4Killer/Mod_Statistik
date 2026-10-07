# Offene Aufgaben

- [ ] Direkt-Sync auf dem NUC-Spielserver einrichten und mit verbundenen Spielern testen.
- [ ] Bisherige Server-Aufzeichnung umstellen, damit nur die Mod ihre Statistikdatei schreibt.
- [ ] SecondMap: bisherige SSH-Abfragen abschalten, um doppelte Belastung zu vermeiden; gegebenenfalls auf Direkt-Sync umstellen. Auswirkungen auf Karten- und Spielerpositionsanzeige dabei prüfen.
- [ ] Rote Kill-Anzeige auch beim Spielen auf dem Server ermöglichen.
- [x] Wiederkehrende Fehlermeldungen zur Realzeit prüfen und beseitigen. Die Realzeit wird laut Nutzer bereits richtig angezeigt; die Fehlermeldungen treten weiterhin auf. In Release 0.0.1.0 wurde der GetFloat-Zugriff auf TimeOfDayIncPerSec entfernt. Im Spiel noch geladene Version und weitere Aufrufstellen prüfen.
- [x] Nach Ende des Blutmonds die rote Zahl oben links mit dem endgültigen eigenen Kill-Wert noch 60 echte Sekunden anzeigen, anschließend ausblenden.
