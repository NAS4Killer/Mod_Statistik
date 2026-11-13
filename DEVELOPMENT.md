# Weiterentwicklung von Mod_Statistik

Projektstand: 13.11.2026, Version 0.3.1.0 mit Trefferquote in Tabelle und HUD.
Der Funktionsstand ist lokal im Spiel bestaetigt und noch nicht veroeffentlicht.
Veroeffentlicht bleibt 0.2.0.0, Zweig direkt-sync,
Commit f3234b3e2f6fdce4428db2f9b5e9ffbcf48a4d34.

## Einstieg und Voraussetzungen

Der gesamte Mod-Quellcode liegt in src/, Testquellen in tests/.
README.md beschreibt die Bedienung; TODO.md die offenen Arbeiten.
ServerInstallation.md enthaelt historische Installationsschritte.
Spielversion beim historischen bestaetigten Spieltest: 7 Days To Die V3.3.0 (b18).
Der lokal bestaetigte Funktionsstand wurde gegen die vorhandenen Bibliotheken von
V3.4.0 (b10) erstellt. Die Spielversion wurde aus Constants in Assembly-CSharp.dll gelesen
und entspricht der am 13.11.2026 online geprueften aktuellen stabilen Version.
Windows PowerShell und der .NET-Framework-C#-Compiler unter
%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe werden fuer den
bestehenden Build verwendet. Ein .NET-SDK allein ersetzt die Spielreferenzen nicht.
Die csproj ist ebenfalls vorhanden; der gepruefte Buildweg ist build.ps1.

Die proprietaeren Spielbibliotheken werden nicht mitgeliefert. Fuer einen neuen
Spielstand die passenden Managed-Bibliotheken aus der eigenen Spielinstallation
bzw. dem dedizierten Server bereitstellen. Harmony und seine Laufzeitabhaengigkeiten
liegen im Projekt; Herkunft und Lizenzen stehen in ThirdPartyNotices.txt.

## Builds

Client, aus dem Repository heraus:

```powershell
.\build.ps1 -GameDir 'C:\Program Files (x86)\Steam\steamapps\common\7 Days To Die'
```

Server mit den Managed-Bibliotheken des tatsaechlichen Servers:

```powershell
.\build.ps1 -ManagedDir 'D:\Serverreferenzen\7DaysToDieServer_Data\Managed' -OutputPath "$PWD\server\Mod_Statistik.dll"
```

Beide Dateien muessen den Dateinamen und internen Assemblynamen Mod_Statistik
behalten. Ein Ausgabename Mod_Statistik-server.dll fuehrt zum falschen Assemblynamen.
Der Build bettet UpdateInstaller.ps1 und die drei src/LocalUI-XML-Dateien ein und
vergleicht die DLL-Version mit VERSION. Das Server-Buildziel ueberschreibt die
Client-DLL nicht. Nach einem Versionswechsel beide Varianten neu bauen.

Version 0.3.1.0: Mod_Statistik.dll und server/Mod_Statistik.dll wurden erfolgreich
lokal gegen die vorhandenen V3.4.0-(b10)-Bibliotheken erstellt. Dieser Build der
Server-DLL ist kein Nachweis einer Installation oder eines Starts auf einem Server.

## Aufbau und verbindliche Entscheidungen

- Trefferquote im HUD: BloodmoonAccuracyRanking vergleicht die angezeigten, auf eine
  Dezimalstelle gerundeten Quoten. GroupAccuracyRanking begrenzt den Vergleich
  auf den lokalen Spieler und seine Gruppe. Alle Spitzenwerte werden gruen;
  fehlende Daten und null Schuesse sind keine Kandidaten. Bestehende Killfarben
  und Kill-Sterne bleiben erhalten. TryHudAccuracy nutzt dieselbe Haltezeit wie
  die Killanzeige; spaete Abschlussmeldungen verlaengern diese Zeit nicht.
- Pruefung des Funktionsstands 0.3.1.0: 13 Testsuiten, 15 Trefferstatistik-Integrationsfaelle sowie
  Tests der eigenen und Gruppen-Controller mit Rundungsgleichstaenden,
  Gruppenwechsel, Farben und Positionierung. Der Nutzer hat den lokalen
  Schusstest mit 0.3.0.0 und anschliessend die HUD-Anzeige mit 0.3.0.1 bestaetigt.
  Die damaligen Testablaeufe stehen in LocalTest-0.3.0.0.txt und LocalTest-0.3.0.1.txt.

- ModApi.cs: Initialisierung, Laufzeitversion und Menueeinbindung.
- BloodmoonRecording.cs: Spielanbindung, Weltwechsel und sekundenweise Abtastung.
- BloodmoonRecorder.cs: Start-/Endwerte, Zwischenstaende und Schreiben.
- BloodmoonLog.cs: TXT-Format lesen, Naechte und Spielerwerte.
- BloodmoonAccuracy.cs: unveraenderliche Schusswerte, kumulative Sitzungsberichte
  und weltbezogene Trefferhistorie in einer separaten JSON-Datei.
- BloodmoonShots.cs / BloodmoonShotHooks.cs: eine abgefeuerte Patrone und hoechstens
  einen erfolgreichen Treffer erfassen; Spielanbindung ueber Harmony.
- BloodmoonAccuracyRuntime.cs: Blutmondzeitraum, lokale Zaehler, authentifizierte
  Spielerzuordnung, sekundenweise Uebernahme und gebuendelte Speicherung.
- StatisticsSync.cs / StatisticsWire.cs: Anfragen, Cache, Hashes und Teiluebertragung.
- StatisticsMessage.cs / StatisticsTransport.cs: reservierte, abgefangene Nachrichten
  im nativen NetPackageChat; authentifizierter Anfrager, keine eigenen NetPackage-Typen.
- LocalStatisticsUi.cs und src/LocalUI/: nur lokale, private UI-Kopien erweitern.
- StatisticsView.cs / StatisticsRow.cs: Tabelle, Rekorde, Hoechstwerte und Seiten.
- BloodmoonCountdown.cs: naechster Blutmond und verbleibende reale Zeit.
- LiveKillsHud.cs / BloodmoonHudDisplay.cs: eigener Wert und 60 Sekunden Nachanzeige.
- GroupKillsEntry.cs / GroupKillsHud.cs: Erweiterung der originalen Gruppenzeilen.
- StatisticsUpdater.cs / UpdateInstaller.ps1: Releaseabfrage, ZIP-Pruefung, Installation,
  Sicherung und Rollback.

Spieler ohne Mod duerfen beitreten und werden mitgezaehlt. Nur Spieler mit Mod sehen
die Erweiterung. Keine XUi-Dateien wieder unter Config/XUi_InGame ablegen: der Server
wuerde sie an Spieler ohne die benoetigten Controller uebertragen. Ebenso keine
eigenen NetPackage-Unterklassen einfuehren: unbekannte Pakettypen verhindern den Beitritt.
Normale Chatnachrichten duerfen nicht abgefangen werden; reservierte Statistikdaten
werden weder angezeigt noch an unbeteiligte Spieler verteilt.

Namen links untereinander, Rekordspalte vor den Blutmondtagen; Tage absteigend nach
Tagesnummer. Historische Tage 84/77 bleiben deshalb vor neuen Tagen 14/7.
Tagesmaxima und hoechster Rekord rot; Gleichstaende einschliessen.
Originale Gruppenanzeige nur erweitern: eigene Zahl rot, andere weiss; roter Stern
bei hoechstem positiven Gruppenwert, auch bei Gleichstand. Vorschau ist entfernt.
Kein Doppel-I, keine VPS-URL und kein Server/Lokal-Schalter in direkt-sync.

Ab 0.3.0.0 stehen je Nacht Kills oben und Trefferquote darunter; die Tabelle behaelt
zwoelf Zeilen mit je 46 Pixeln Hoehe und elf Nachtspalten. Rekordspalte und rote
Killmaxima behalten ihre Bedeutung. Fehlende Schussdaten und null Schuesse ergeben
einen Gedankenstrich; bekannte Fehlschuesse ergeben 0 %. Die Prozentanzeige verwendet
hoechstens eine deutsche Dezimalstelle.

## Umfang der Schusserfassung ab 0.3.0.0

Eine wirklich abgefeuerte Patrone einer aktuellen Vanilla-Patronenwaffe zaehlt als
ein Schuss. Mindestens ein Treffer auf einen zum Trefferzeitpunkt lebenden Zombie
oder ein Zombie-Tier macht die Patrone erfolgreich. Der Hook vor der Schadenswirkung
schliesst den toedlichen Treffer ein. Leichen werden ausgeschlossen. Schrotpellets
und mehrere Durchschusstreffer zaehlen zusammen hoechstens einen Erfolg pro Patrone.
Nachladen, ein leerer Abzug und abgebrochene Aktionen erhoehen den Zaehler nicht.

Die Abgrenzung ist auf den vorhandenen Vanilla-Pfad ItemActionRanged und passende
Waffen-Tags begrenzt. Boegen, Armbrueste, Raketen, Sprengstoffe, andere Projektilwaffen,
Werkzeuge, Geschuetztuerme und Admin-Waffen sind ausgeschlossen. Mod-Waffen mit
abweichenden Aktionen oder Tags sind nicht Teil des geprueften Umfangs.

Der jeweilige Mod-Client zaehlt seine Schuesse und meldet kumulative Werte pro
Blutmondtag und GUID-Zaehlsitzung. Der Host ordnet die Meldung dem authentifizierten
Spieler zu und prueft Tag und Wertebereich. Diese Zaehlung vertraut den Angaben des
Mod-Clients; sie ist keine serverseitige Anti-Cheat-Treffervalidierung. Spieler ohne
Mod behalten ihre Killwerte und haben keine Trefferquote. Alte Server werden ueber
den bisherigen Kill-Sync angesprochen; alte Clients behalten den bisherigen Pfad.

## Daten und Betrieb

Data/bloodmoon_kills.txt ist die Historie. Die mitgelieferte Datei enthaelt anonymisierte
Beispieldaten (Player1 bis PlayerX), keine aktuelle Serverstatistik.
Weltbezogene bloodmoon_state-*.json speichern laufende Aufzeichnung und Wiederanlauf.
Die genaue Serialisierung ist in BloodmoonRecorder.cs definiert; bestehende Daten bei
Formatwechsel erhalten bzw. migrieren. Fehlerhafte Dateien sichern statt ueberschreiben.
Lokales Spiel zeichnet selbst auf, dedizierter Server zeichnet zentral auf;
verbundene Clients schreiben keine eigene Serverhistorie.
Abtastung einmal pro realer Sekunde, Speichern geaenderter Zwischenstaende alle fuenf
Sekunden sowie bei Abschluss/Ausloggen/Weltende. HUD-Endwert 60 reale Sekunden halten.
Datenlimits: 256 KiB pro Uebertragung, 8192 Byte pro Teil, 512 Naechte und 128 Spieler
(siehe StatisticsWire.cs); bei Erweiterungen beide Seiten und Tests anpassen.

SecondMap ist ein separates Projekt. Alte Kill-Aufzeichnung ist deaktiviert;
SSH und Zeit-/Positionsabfragen bleiben erhalten. SecondMap liest die Mod-TXT und
Weltstatusdatei; bei mehreren Welten muss die Zuordnung eindeutig sein.
Die externe Integration gehoert nicht ins Mod-Installationspaket.

Die Trefferhistorie liegt ab 0.3.0.0 separat in
Data/bloodmoon_accuracy-<Weltkennung>.json. Der Schluessel folgt derselben Weltzuordnung
wie die Kill-Zwischenstaende. bloodmoon_kills.txt und sein bestehendes Zeilenformat
bleiben erhalten; historische Zeilen ohne Trefferdaten bleiben unbekannt.
BloodmoonAccuracyStore uebernimmt kumulative Werte je Tag, stabiler Spielerkennung
und Sitzung. Doppelte oder ruecklaeufige Meldungen werden ignoriert; eine neue Sitzung
addiert ihre Werte. Namensaenderungen erzeugen keinen zweiten Eintrag derselben
Kennung. Die Laufzeitanbindung verwendet den Namen des zugehoerigen Kill-Recorders.

Apply schreibt nur in den Arbeitsspeicher. Flush ersetzt eine vollstaendig geschriebene
temporaere JSON-Datei atomar; die Laufzeitanbindung ruft dies bei Aenderungen gesammelt
alle fuenf Sekunden sowie bei Ausloggen/Weltende auf. Eine ungueltige vorhandene
Trefferdatei wird beim Laden abgelehnt und unveraendert erhalten. Grenzen: 512 Naechte,
128 Spieler pro Nacht, 64 Zaehlsitzungen pro Spieler/Nacht, 65536 Sitzungen insgesamt,
16 MiB Datei und maximal 1000000000 Schuesse je Spieler/Nacht.

Erweiterte Anfragen enthalten den Hash der abgeschlossenen Historie und optional
den kumulativen lokalen Schussbericht. Die laufende Nacht mit Kills und Trefferwerten
wird etwa einmal pro Sekunde separat uebertragen. Die abgeschlossene Historie wird
nur bei fehlendem oder geaendertem Hash angefordert. Der bisherige Pakettyp, die
Authentifizierung und das Abfangen reservierter Chatnachrichten bleiben erhalten.
Neue Schuesse loesen weder pro Schuss eine Speicherung noch eine vollstaendige
Historienuebertragung aus.

## Tests

```powershell
.\test.ps1
```

Das Skript kompiliert isolierte Tests aus den Quelltexten. Es testet TXT, Recorder,
60-Sekunden-HUD und Direkt-Sync mit acht simulierten Spielern, native Paketabfangung
und unveraenderten Chat. Testdateien liegen in einem kurzen temporaeren Pfad.
UI-Tests benoetigen zusaetzlich Original-XUi-Dateien der Spielinstallation:

```powershell
.\test.ps1 -UiDirectory 'C:\Program Files (x86)\Steam\steamapps\common\7 Days To Die\Data\Config\XUi_InGame'
```

Weitere vorhandene Tests: Countdown, Tabellenzeilen und Updater. Der Updater-Test
benoetigt ein zur Testversion passendes Installations-ZIP. Bei Versionsaenderungen
auch tests/StatisticsUpdaterTests.cs aktualisieren.
Echte Beitritte ohne Mod und anschliessend mit Mod wurden bestaetigt.
Offen bleiben der gemeinsame Blutmond-Gruppentest samt Abschluss, Nachanzeige,
Neustart und normalem Chat unter gleichzeitiger Statistikuebertragung (TODO.md).

Trefferquote 0.3.0.0: BloodmoonAccuracyTests prueft Prozentwerte, kumulative Meldungen,
Duplikate, Reihenfolge, Wiederverbindung, Namensaenderung, Neustart, Grenzen,
unveraenderte Kill-TXT und den Erhalt beschaedigter Dateien. StatisticsRowTests prueft
die zweite Zeile, unbekannte Werte, Nullschuesse, rote Killwerte und Seitenwechsel.
Diese beiden Komponentenpruefungen sowie XML-Parsing und die beibehaltene Tabellen-
groesse wurden am 13.11.2026 erfolgreich ausgefuehrt. Zusaetzliche Testquellen fuer
Schuss-Hooks, Werteuebertragung und den erweiterten Sync liegen in tests/.
Der reale lokale Spielablauf ist in LocalTest-0.3.0.0.txt beschrieben; der Nutzer
hat die Schusserfassung im Spiel bestaetigt. Auch die anschliessende HUD-Ergaenzung
wurde lokal bestaetigt. Diese Bestaetigungen ersetzen keine gemeinsamen Tests mit
einer echten Gruppe oder die ausstehenden Server- und Neustartpruefungen.

## Version und Veroeffentlichung

Autor immer NAS4Killer; niemals eine Identitaet aus einem Windows-Profilpfad ableiten.
Versionsschema vierteilig. Folgende Felder gemeinsam aendern: VERSION, ModInfo.xml,
src/AssemblyInfo.cs (alle drei Attribute), src/ModApi.cs, src/Mod_Statistik.csproj,
Versionsanzeige in src/LocalUI/windows.xml sowie README und Updater-Tests.
Danach Client und Server bauen, pruefen und ReleaseNotes-VERSION.txt erstellen.
RELEASE_NOTES.md und CHANGELOG.md zusammenfassen; nur tatsaechlich umgesetzte und
committete Aenderungen nennen, offene echte Tests ehrlich benennen.

Commit auf direkt-sync, Tag direkt-sync-vVERSION. Pakete mit package.ps1 ausschliesslich
aus diesem Commit erzeugen. Client- und Server-ZIP unterscheiden sich nur in der DLL.
GitHub-Release: https://github.com/NAS4Killer/Mod_Statistik/releases
Release-Text aus committeten Release Notes zusammenfassen, beide ZIPs anhaengen,
Branch/Tag-Ziel und Upload-SHA256 pruefen. GitHub-Anmeldung erfolgt ueber den lokalen
Credential-Manager bzw. einen eigenen autorisierten Zugang, nicht ueber gespeicherte
Kennwoerter im Projekt. Keine Zugangsdaten, private Serverpfade oder echten Spielernamen
veroeffentlichen. Screenshot1.png ist anonymisiert und zeigt den Stand 0.0.2.7.

## Aktuell installierter und veroeffentlichter Stand

Historische Installation: PC und NUC 0.1.0.2 am 10.10.2026.
Anschliessend wurde derselbe Funktionsstand als 0.2.0.0 gebaut, committet und
veroeffentlicht. Eine Installation von 0.2.0.0 wurde hier nicht beauftragt.
Historische Installationsnotizen sind kein Nachweis fuer den heutigen Serverzustand.
Sicherungen liegen ausserhalb des aktiven Mods-Verzeichnisses; Data immer erhalten.
Betrieb und Zugang zur konkreten Maschine sind externe Voraussetzungen und werden
bei Weiterarbeit gesondert autorisiert; Zugangsdaten gehoeren nicht in dieses Repository.

Stand 13.11.2026: Die letzte bestaetigte lokale Installation ist 0.3.0.1.
DLL und Versionsdateien wurden nach Installation per SHA-256 geprueft;
bestehende Mod-Daten, Abhaengigkeiten und originale Spielassemblies blieben erhalten.
Der Nutzer hat zuerst die Schusserfassung von 0.3.0.0 und danach die HUD-Ergaenzung
von 0.3.0.1 im Spiel bestaetigt. Alle 13 Testsuiten bestanden, darunter 326
Protokollpruefungen und 15 Trefferstatistik-Integrationsfaelle. Version 0.3.1.0
fasst diesen Funktionsstand zusammen; sie bezeichnet hier keine bestaetigte neue
Installation oder Veroeffentlichung. Kein Server wurde fuer diese Erweiterung veraendert.
Die verifizierte Sicherung liegt unter
E:\Git\7D2D\_Sicherungen\Mod_Statistik\20261113-130154 und umfasst das Projekt
(483 Dateien), die Spielinstallation (23180 Dateien), Spielstaende (940 Dateien)
und die gesicherten Registry-Einstellungen. Diese Sicherung liegt ausserhalb des
aktiven Projekt- und Mod-Verzeichnisses. Vor der HUD-Ergaenzung wurden Projekt,
installierte Mod und aktuelle Spielstaende unter
E:\Git\7D2D\_Sicherungen\Mod_Statistik\20261113-183759-HUD erneut gesichert und
per SHA-256 geprueft. 0.2.0.0 bleibt die letzte Veroeffentlichung.
ReleaseNotes-0.3.1.0.txt fasst die aktuelle Version zusammen; die Dateien fuer
0.3.0.0 und 0.3.0.1 dokumentieren die damaligen lokalen Teststaende.
