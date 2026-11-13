# Mod_Statistik 0.3.1.0 - Trefferquote in Tabelle und HUD

- Trefferquote pro Spieler und Blutmondnacht unter der Killzahl in der Tabelle sowie rechts neben den eigenen und den Gruppen-Kills.
- Die höchste angezeigte Trefferquote der Gruppe ist grün. Bei Gleichstand sind alle Bestplatzierten grün; andere Quoten bleiben weiß. Fehlende Daten und null Schüsse erscheinen als Gedankenstrich und erhalten keine Spitzenmarkierung.
- Eine abgefeuerte Patrone zählt einmal. Mindestens ein Treffer auf einen lebenden Zombie oder ein Zombie-Tier macht den Schuss erfolgreich. Tödliche Treffer zählen; Schrotpellets und Durchschüsse ergeben höchstens einen Erfolg pro Patrone.
- Erfasst werden Vanilla-Patronenwaffen während der Blutmondnacht. Bögen, Armbrüste, Raketen, Sprengstoffe, andere Projektilwaffen, Werkzeuge, Geschütztürme und Admin-Waffen sind ausgeschlossen.
- Jeder Mod-Client meldet kumulative Schusswerte; wiederholte Meldungen werden nicht doppelt gezählt. Der Host ordnet sie dem authentifizierten Spieler zu. Spieler ohne Mod behalten ihre Killstatistik und haben keine Trefferquote.
- Die Trefferhistorie wird getrennt und weltbezogen gespeichert. Die bestehende Kill-TXT behält ihr Format und ihre bisherigen Leser. Den gesamten Data-Ordner bei Aktualisierungen erhalten.
- Die laufende Nacht wird etwa einmal pro Sekunde separat übertragen. Die abgeschlossene Historie wird nur beim ersten Abruf und bei Änderungen übertragen. Einzelne Schüsse lösen keine eigenen Dateioperationen oder vollständigen Historientransfers aus.
- Killfarben, Rekorde, Rangsterne und die 60 Sekunden Nachanzeige bleiben erhalten. Die Trefferquote steht hinter der Killzahl beziehungsweise dem Stern und folgt derselben Nachanzeigezeit.

Prüfstand: Client- und Server-DLL 0.3.1.0 wurden lokal gegen die vorhandenen V3.4.0-(b10)-Bibliotheken erstellt. Alle 13 Testsuiten bestanden, einschließlich 326 Protokollprüfungen, 15 Trefferstatistik-Integrationsfällen sowie Prüfungen für Schuss-Hooks, Gleichstände, Gruppenwechsel, Farben und Positionierung. Der Nutzer hat die lokale Schusserfassung und danach die zusätzliche HUD-Anzeige im Spiel bestätigt. Diese Spielbestätigung erfolgte mit den Zwischenständen 0.3.0.0 und 0.3.0.1 gegen V3.4.0 (b10).

Die letzte bestätigte Installation auf dem PC bleibt 0.3.0.1. Version 0.3.1.0 fasst denselben bestätigten Funktionsstand für den Commit zusammen. Für diese Erweiterung wurde kein Server verändert; 0.2.0.0 bleibt die letzte öffentliche Veröffentlichung.

Weiterhin ausstehend: gemeinsamer Test mit einer echten Gruppe während des Blutmonds einschließlich Abschluss, 60 Sekunden Nachanzeige und Neustart. Die lokale Spielbestätigung deckt diese Bedingungen nicht ab.
