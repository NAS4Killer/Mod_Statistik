# Mod_Statistik 0.3.2.0

Blutmond-Statistik für 7 Days to Die: Kills, persönliche Rekorde, Trefferquote und Kopftreffer im Spiel vergleichen.

[Version 0.3.2.0 herunterladen](https://github.com/NAS4Killer/Mod_Statistik/releases/tag/direkt-sync-v0.3.2.0) · [Versionsübersicht](CHANGELOG.md)

## Anzeigen

Im Spielermenü zeigt der Tab **Blutmond** Spieler, Rekorde und Blutmondnächte. Je Nacht stehen Kills, Trefferquote und Kopftreffer in drei Zeilen mit eigenen Symbolen. Eine Seite enthält neun Spieler und elf Nächte; weitere Werte sind über die Seitenwahl erreichbar.

Die eigene Anzeige und die Gruppenanzeige enthalten ebenfalls die drei Werte. Die eigene Killzahl ist rot. Ein roter Stern kennzeichnet die höchste positive Killzahl in der Gruppe. Die beste angezeigte Trefferquote wird grün markiert; Gleichstände werden gemeinsam hervorgehoben. Die Anzeigen bleiben nach dem Blutmond 60 echte Sekunden sichtbar.

## Trefferquote und Kopftreffer

Die Trefferquote ist der Anteil abgefeuerter Patronen, die mindestens einen lebenden Zombie treffen. Der tödliche Treffer zählt; bereits tote Körper zählen nicht. Schrot und Durchschüsse zählen je Patrone höchstens einmal. Kopftreffer werden ebenfalls höchstens einmal je Patrone gezählt, auch wenn zuerst ein Körpertreffer und danach ein Kopftreffer erfolgt.

Erfasst werden Vanilla-Patronenwaffen während der Blutmondnacht. Bögen, Armbrüste, Raketen, Sprengstoffe, Werkzeuge, Geschütztürme und Admin-Waffen zählen nicht dazu. Nachladen und ein leerer Abzug zählen nicht als Schuss.

Fehlende Daten erscheinen als **—**. Eine bekannte Kopftrefferzahl von null wird als **0** angezeigt. Alte Nächte erhalten keine nachträglich erfundenen Kopftrefferwerte.

## Installation und Aktualisierung

1. Spiel beziehungsweise Spielserver beenden und den vorhandenen Mod-Ordner einschließlich **Data** sichern.
2. Für den PC das Clientpaket verwenden, für einen dedizierten Server das Paket mit **-server** im Dateinamen.
3. Den Ordner **Mod_Statistik** aus dem ZIP in den **Mods**-Ordner des Spiels kopieren. ModInfo.xml liegt direkt im Ordner Mod_Statistik.
4. Bei einer Aktualisierung den gesamten bisherigen **Data**-Ordner erhalten. Die mitgelieferte bloodmoon_kills.txt enthält historische Beispieldaten und darf die eigene Statistik nicht ersetzen.
5. Alte Dateien dieser Mod unter **Config/XUi_InGame** beim manuellen Umstieg entfernen. Die aktuelle Oberfläche ist in der DLL enthalten.
6. Spiel beziehungsweise Spielserver starten. Für einen neuen Aufzeichnungstest eine neue Blutmondnacht verwenden.

Für Kopftreffer im Mehrspielermodus benötigen der Spielhost beziehungsweise Server und die mitzählenden Spieler Version **0.3.2.0**. Spieler ohne Mod können beitreten und werden bei den Kills mitgezählt; ihre Schusswerte fehlen. Bei älteren Gegenstellen stehen nur die jeweils unterstützten Statistikwerte zur Verfügung.

Die Oberfläche benötigt die Mod auf dem jeweiligen PC. Der Server überträgt die Statistik über die bestehende Spielverbindung.

## Datensicherung

Die Mod speichert abgeschlossene Nächte in **Data/bloodmoon_kills.txt**, laufende Aufzeichnungen in den **bloodmoon_state-*.json**-Dateien und Schusswerte einschließlich Kopftreffern in **bloodmoon_accuracy-*.json**. Immer den gesamten Data-Ordner sichern und bei Updates erhalten.

## Projekt

https://github.com/NAS4Killer/Mod_Statistik

Die Neuerungen dieser Version stehen in **ReleaseNotes-0.3.2.0.txt**.
