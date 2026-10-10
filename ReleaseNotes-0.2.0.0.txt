Mod_Statistik 0.2.0.0 - Serverbeitritt mit und ohne Mod

- Spieler ohne Client-Mod können den Server betreten und werden weiterhin in der Blutmondstatistik mitgezählt.
- Statistikübertragung verwendet den vorhandenen Standardpakettyp des Spiels. Reservierte Nachrichten werden vor der Chatanzeige abgefangen; normaler Chat bleibt erhalten.
- Server antwortet nur auf authentifizierte Statistikanfragen. Spieler ohne Mod erhalten keine Statistiknachrichten.
- Oberfläche ist in der DLL eingebettet und wird nur auf Clients mit Mod eingebunden. Spieler ohne Mod erhalten weder Blutmond-Tab noch modabhängige UI-Controller.
- Tabelle, eigene rote Killzahl, 60 Sekunden Nachanzeige und Erweiterung der originalen Gruppenzeilen bleiben für Spieler mit Mod erhalten.
- Harmony-Abhängigkeiten und Lizenzhinweise sind im Installationspaket enthalten. Der Updater entfernt alte XUi-Patchdateien mit Sicherung und Rollback.

Prüfungen: Client- und Serverbuilds, automatisierte Übertragungs-, UI- und Updater-Prüfungen. Serverbeitritt ohne Mod sowie Gegentest mit installierter Mod vom Nutzer bestätigt. Statistikdaten bei Installation erhalten.
Noch offen: gemeinsamer Gruppentest während eines Blutmonds mit Endwerten, 60 Sekunden Nachanzeige und Neustart.
Wechsel von älteren Versionen: manuell installieren, bisherigen Data-Ordner erhalten und alte Config/XUi_InGame-Patchdateien entfernen. Keine Neuinstallation durch diese Veröffentlichung.
