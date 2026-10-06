# Mod_Statistik 0.0.0.7

Doppel-I oeffnet den Blutmond-Tab im Spielermenue.
Spieler stehen untereinander; Rekord und Blutmond-Tage stehen als Spalten.
Rot kennzeichnet den Hoechstwert je Nacht sowie den hoechsten Spielerrekord. Gleichstaende werden gemeinsam markiert. Fehlende Werte erscheinen als â€”.
Die TXT wird beim Oeffnen und alle 60 Sekunden bei offenem Tab automatisch geladen.
Die Downloadadresse kann unten eingetragen und gespeichert werden. Leere Adresse verwendet die mitgelieferte lokale Datei.
Rekorde beziehen sich auf alle abgeschlossenen Naechte der geladenen Datei.
Installation bei beendetem Spiel: ZIP in Mods entpacken.

Elf kompakte Tagesspalten pro Seite. Native Seitensteuerung unten rechts. Die aktive URL bleibt sichtbar.

Server / Lokal ist als deaktivierter Button vorbereitet. Lokaler Pfad: Data/bloodmoon_kills.txt innerhalb der Mod. Aktuell ist keine Server-URL eingerichtet.

## Updates

Der Button neben der Version folgt dem MiniMap-Ablauf: Github, Download mit Versionsnummer, Installieren mit Versionsnummer. Die letzte Aktion startet einen versteckten Installer, der auf das Ende des Spiels wartet. Neue Releases werden beim ersten Oeffnen automatisch geprueft. Release Notes oeffnen die angebotene Version.

Die eigene GitHub-Projektadresse muss in Config/UpdateRepository.txt stehen. Es ist aktuell keine Adresse hinterlegt. ZIP-Name: Mod_Statistik-vA.B.C.D.zip, stabiler Release-Tag vA.B.C.D, GitHub-Asset mit SHA256-Digest.

Updates erhalten Data/bloodmoon_kills.txt und Config/UpdateRepository.txt. Gespeicherte TXT-URL bleibt im bestehenden AppData-Verzeichnis erhalten.
