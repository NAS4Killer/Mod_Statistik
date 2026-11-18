using System;using System.IO;using System.Linq;using System.Collections.Generic;using ModStatistik;
namespace UnityEngine { public static class Time { public static float unscaledTime; } public static class Debug{public static void LogWarning(string message){Console.Error.WriteLine(message);}} }
public class World{public PlayerList Players=new PlayerList(); public ulong Now;public ulong GetWorldTime(){return Now;}public EntityPlayer GetPrimaryPlayer(){return Players.list.FirstOrDefault()??new EntityPlayer{entityId=1};}}
public class PlayerList{public List<EntityPlayer> list=new List<EntityPlayer>();}
public class EntityPlayer{public int entityId;public string EntityName;public int Kills,Reads;public int KilledZombies{get{Reads++;return Kills;}}}
public class PersistentPlayerData{public object PrimaryId;}
public class PersistentPlayerList{public PersistentPlayerData GetPlayerDataFromEntityID(int id){return new PersistentPlayerData{PrimaryId=id.ToString()};}}
public static class GameIO{public static string Save="accuracy-sync-world";public static string GetSaveGameDir(){return Save;}}
public enum EnumGameStats{DayLightLength,BloodMoonDay}
public static class GameStats{public static int Day=7;public static int GetInt(EnumGameStats stat){return stat==EnumGameStats.DayLightLength?18:Day;}}
public static class GameUtils{public static Tuple<int,int> CalcDuskDawnHours(int value){return Tuple.Create(22,4);}} public class GameManager {public static bool IsDedicatedServer;public static GameManager Instance=new GameManager();public World World;public PersistentPlayerList GetPersistentPlayerList(){return new PersistentPlayerList();}}
public static class ModEvents {public struct SGameUpdateData{} public delegate void Handler(ref SGameUpdateData d); public class Event{public void RegisterHandler(Handler h){}} public static Event GameUpdate=new Event();public struct SPlayerDisconnectedData{}public struct SWorldShuttingDownData{}public delegate void DisconnectHandler(ref SPlayerDisconnectedData d);public delegate void ShutdownHandler(ref SWorldShuttingDownData d);public class DisconnectEvent{public void RegisterHandler(DisconnectHandler h){}}public class ShutdownEvent{public void RegisterHandler(ShutdownHandler h){}}public static DisconnectEvent PlayerDisconnected=new DisconnectEvent();public static ShutdownEvent WorldShuttingDown=new ShutdownEvent();}
public enum NetPackageDirection{ToServer,ToClient}
public class PooledBinaryWriter:BinaryWriter{public PooledBinaryWriter(Stream s):base(s){}} public class PooledBinaryReader:BinaryReader{public PooledBinaryReader(Stream s):base(s){}}
public enum EChatType{Global,Friends,Party,Whisper,Discord}
public enum EMessageSender{None,Server,SenderIdAsPlayer}
public static class GeneratedTextManager{public enum BbCodeSupportMode{NotSupported,Supported,SupportedAndAddEscapes}}
public class NetPackage{public ClientInfo Sender;public virtual void write(PooledBinaryWriter w){w.Write((ushort)0);}public virtual void read(PooledBinaryReader r){}public virtual void ProcessPackage(World w,GameManager g){}}
public class NetPackageChat:NetPackage{
 private string msg;private EChatType chatType;private int senderEntityId;private EMessageSender msgSender;
 public static int VanillaCalls;public string Message{get{return msg;}}
 public EChatType ChatType{get{return chatType;}}public int SenderId{get{return senderEntityId;}}public EMessageSender Origin{get{return msgSender;}}
 public NetPackageChat Setup(EChatType t,int sender,string message,List<int> recipients,EMessageSender origin,GeneratedTextManager.BbCodeSupportMode mode){chatType=t;senderEntityId=sender;msg=message;msgSender=origin;return this;}
 public override void write(PooledBinaryWriter w){base.write(w);w.Write(msg);w.Write((int)chatType);w.Write(senderEntityId);w.Write((int)msgSender);}
 public override void read(PooledBinaryReader r){msg=r.ReadString();chatType=(EChatType)r.ReadInt32();senderEntityId=r.ReadInt32();msgSender=(EMessageSender)r.ReadInt32();}
 [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public override void ProcessPackage(World w,GameManager g){VanillaCalls++;}
}
public static class NetPackageManager{public static T GetPackage<T>()where T:NetPackage,new(){return new T();}}
public class ClientInfo{public int entityId;public List<byte[]> Sent=new List<byte[]>();public void SendPackage(NetPackage p){Sent.Add(TestAccuracySync.Serialize(p));}}
public class Clients{public ClientInfo Client;public ClientInfo ForEntityId(int id){return Client!=null&&Client.entityId==id?Client:null;}public bool Contains(ClientInfo c){return c==Client;}}
public class ConnectionManager{public static ConnectionManager Instance=new ConnectionManager();public int RoleReads;private bool role;public bool IsServer{get{RoleReads++;return role;}set{role=value;}}public Clients Clients=new Clients();public List<byte[]> Sent=new List<byte[]>();public void SendToServer(NetPackage p,bool b){Sent.Add(TestAccuracySync.Serialize(p));}}

internal static class TestAccuracySync
{
    private const ulong Start = 6UL * 24000UL + 22000UL;
    private const ulong End = 7UL * 24000UL + 4000UL;
    private static string output, root;
    private static float clock;
    private static int cases, failures;
    private static byte[][] initialExtended, updatedExtended;
    private static BloodmoonLog serverData;

    public static byte[] Serialize(NetPackage package)
    {
        using (var stream = new MemoryStream())
        using (var writer = new PooledBinaryWriter(stream))
        { package.write(writer); return stream.ToArray(); }
    }
    private static NetPackageChat Deserialize(byte[] bytes)
    {
        using (var reader = new PooledBinaryReader(new MemoryStream(bytes)))
        { reader.ReadUInt16(); var package = new NetPackageChat(); package.read(reader); return package; }
    }
    private static void Need(bool value, string message) { if (!value) throw new Exception(message); }
    private static void Case(string name, Action test)
    {
        cases++;
        try { test(); Console.WriteLine("PASS: " + name); }
        catch (Exception error) { failures++; Console.Error.WriteLine("FAIL: " + name + ": " + error.Message); }
    }
    private static void Setup(bool server, bool dedicated, bool history)
    {
        clock = cases * 1000f;
        root = Path.Combine(output, "case-" + cases);
        GameIO.Save = "accuracy-sync-case-" + cases;
        Directory.CreateDirectory(Path.Combine(root, "Data"));
        if (history) File.WriteAllText(Path.Combine(root, "Data", "bloodmoon_kills.txt"),
            "=== Blutmond Tag 1 END 1 ===\nHost start=0 end=2 bm_kills=2\n");
        StatisticsSync.Initialize(root);
        GameManager.IsDedicatedServer = dedicated;
        ConnectionManager.Instance = new ConnectionManager { IsServer = server && !dedicated };
        GameManager.Instance.World = new World { Now = Start };
        GameManager.Instance.World.Players.list.Add(new EntityPlayer { entityId = 1, EntityName = "Host", Kills = 10 });
        GameManager.Instance.World.Players.list.Add(new EntityPlayer { entityId = 2, EntityName = "Remote", Kills = 20 });
        GameStats.Day = 7;
        UnityEngine.Time.unscaledTime = clock;
        StatisticsSync.Tick();
        Need(BloodmoonAccuracyRuntime.Error == null && BloodmoonRecording.Error == null, "initialization errors");
    }
    private static void Advance(float seconds, ulong worldTime)
    { UnityEngine.Time.unscaledTime = clock + seconds; GameManager.Instance.World.Now = worldTime; StatisticsSync.Tick(); }
    private static ShotAccuracy Accuracy(string player)
    { return BloodmoonAccuracyRuntime.ForDay(7)[player]; }
    private static AccuracyReport Report(string session, long shots, long hits, int day = 7, bool final = false)
    { return new AccuracyReport { Session = session, Shots = shots, Hits = hits, Day = day, Final = final }; }
    private static string Key()
    {
        return BitConverter.ToString(StatisticsWire.Hash(System.Text.Encoding.UTF8.GetBytes(Path.GetFullPath(GameIO.Save))))
            .Replace("-", "").ToLowerInvariant().Substring(0, 24);
    }
    private static ClientInfo Authenticate()
    {
        var client = new ClientInfo { entityId = 2 };
        ConnectionManager.Instance.Clients.Client = client;
        return client;
    }
    private static void Send(ClientInfo authenticated, int id, AccuracyReport report, byte[] knownHash,
        int claimedSender = 2, EMessageSender origin = EMessageSender.SenderIdAsPlayer, bool headshots = false)
    {
        var packet = new NetPackageChat().Setup(EChatType.Whisper, claimedSender,
            StatisticsMessage.RequestExtended(id, knownHash, report, headshots), null, origin,
            GeneratedTextManager.BbCodeSupportMode.NotSupported);
        packet.Sender = authenticated;
        packet.ProcessPackage(GameManager.Instance.World, GameManager.Instance);
    }
    private static StatisticsDelta Delta(IEnumerable<byte[]> packets)
    {
        var transfer = new StatisticsTransfer();
        byte[] completed = null;
        foreach (var packet in packets)
        {
            var frame = StatisticsMessage.Decode(Deserialize(packet).Message);
            Need(frame.Extended && frame.Result == 0, "extended success response");
            completed = transfer.Accept(frame.Id, frame.Index, frame.Count, frame.Total, frame.Hash, frame.Chunk);
        }
        Need(completed != null, "complete response");
        return StatisticsDelta.Decode(completed);
    }
    private static StatisticsMessage LastRequest()
    { return StatisticsMessage.Decode(Deserialize(ConnectionManager.Instance.Sent.Last()).Message); }
    private static void Reply(byte[] raw, int id, bool forged = false)
    {
        var frame = StatisticsMessage.Decode(Deserialize(raw).Message);
        string text = frame.Extended ? StatisticsMessage.ReplyExtended(id, frame.Result, frame.Index,
            frame.Count, frame.Total, frame.Hash, frame.Chunk, frame.Headshots) : StatisticsMessage.Reply(id,
            frame.Result, frame.Index, frame.Count, frame.Total, frame.Hash, frame.Chunk);
        var packet = new NetPackageChat().Setup(EChatType.Whisper, forged ? 2 : -1, text, null,
            forged ? EMessageSender.SenderIdAsPlayer : EMessageSender.Server,
            GeneratedTextManager.BbCodeSupportMode.NotSupported);
        packet.ProcessPackage(GameManager.Instance.World, GameManager.Instance);
    }

    private static int Main(string[] args)
    {
        output = args.Length == 0 ? Path.Combine(Path.GetTempPath(), "accuracy-sync-" + Guid.NewGuid().ToString("N")) : args[0];
        Directory.CreateDirectory(output);
        StatisticsTransport.Initialize();

        Case("local live zero, ten shots/seven hits, one-second sampling and outside-night exclusion", delegate
        {
            Setup(true, false, false);
            Need(Accuracy("Host").Shots == 0 && Accuracy("Host").Percentage == "\u2014", "live zero");
            for (int index = 0; index < 10; index++) BloodmoonAccuracyRuntime.RecordShot(index < 7);
            Need(BloodmoonAccuracyRuntime.Report().Shots == 10 && Accuracy("Host").Shots == 0, "no sample in weapon path");
            Advance(.9f, Start + 1);
            Need(Accuracy("Host").Shots == 0, "sample deferred below one second");
            Advance(1f, Start + 2);
            Need(Accuracy("Host").Shots == 10 && Accuracy("Host").Hits == 7 && Accuracy("Host").Percentage == "70 %", "70 percent");
            Need(StatisticsSync.Snapshot().Data.Nights.Single(n => n.Day == 7).Accuracy["Host"].Percentage == "70 %", "visible live percentage");
            ShotAccuracy hudAccuracy;
            Need(StatisticsSync.TryHudAccuracy("Host", out hudAccuracy) && hudAccuracy.Percentage == "70 %", "local HUD receives live accuracy");
            GameManager.Instance.World.Now = End;
            BloodmoonAccuracyRuntime.RecordShot(true);
            Need(BloodmoonAccuracyRuntime.Report().Shots == 10, "dawn excludes further shots");
        });

        Case("five-second persistence and reload/new-session cumulative totals", delegate
        {
            Setup(true, false, false);
            string path = Path.Combine(root, "Data", "bloodmoon_accuracy-" + Key() + ".json");
            byte[] original = File.ReadAllBytes(path);
            string oldSession = BloodmoonAccuracyRuntime.Report().Session;
            for (int index = 0; index < 10; index++) BloodmoonAccuracyRuntime.RecordShot(index < 7);
            Advance(1f, Start + 1);
            Need(File.ReadAllBytes(path).SequenceEqual(original), "sample does not write immediately");
            Advance(4.99f, Start + 2);
            Need(File.ReadAllBytes(path).SequenceEqual(original), "save deferred below five seconds");
            Advance(5f, Start + 3);
            var reloaded = new BloodmoonAccuracyStore(Path.Combine(root, "Data"), Key());
            Need(reloaded.ForDay(7)["Host"].Shots == 10 && reloaded.ForDay(7)["Host"].Hits == 7, "five-second saved counters");
            var nextWorld = new World { Now = Start + 4 };
            nextWorld.Players.list.Add(new EntityPlayer { entityId = 1, EntityName = "Host", Kills = 10 });
            GameManager.Instance.World = nextWorld;
            Advance(6f, Start + 4);
            Need(BloodmoonAccuracyRuntime.Report().Session != oldSession && Accuracy("Host").Shots == 10, "reconnect retains totals with new session");
            BloodmoonAccuracyRuntime.RecordShot(true); BloodmoonAccuracyRuntime.RecordShot(true);
            Advance(7f, Start + 5);
            Need(Accuracy("Host").Shots == 12 && Accuracy("Host").Hits == 9, "new session adds only new shots");
        });

        Case("dawn captures final subsecond shot in completed counters", delegate
        {
            Setup(true, false, false);
            BloodmoonAccuracyRuntime.RecordShot(false);
            Advance(1f, End - 2);
            UnityEngine.Time.unscaledTime = clock + 1.8f;
            GameManager.Instance.World.Now = End - 1;
            BloodmoonAccuracyRuntime.RecordShot(true);
            Advance(1.9f, End);
            Need(Accuracy("Host").Shots == 2 && Accuracy("Host").Hits == 1, "last shot absent at dawn boundary");
            var reloaded = new BloodmoonAccuracyStore(Path.Combine(root, "Data"), Key());
            Need(reloaded.ForDay(7)["Host"].Shots == 2, "last shot persisted directly at dawn");
            ShotAccuracy hudAccuracy;
            Need(StatisticsSync.TryHudAccuracy("Host", out hudAccuracy) && hudAccuracy.Percentage == "50 %", "local HUD keeps final accuracy");
            Advance(2.1f, End + 1);
            UnityEngine.Time.unscaledTime = BloodmoonRecording.FinishedAt + 60f;
            StatisticsSync.Tick();
            Need(!StatisticsSync.TryHudAccuracy("Host", out hudAccuracy), "local HUD accuracy expires with kills");
        });

        Case("dawn shutdown flush persists final shot before the normal update tick", delegate
        {
            Setup(true, false, false);
            BloodmoonAccuracyRuntime.RecordShot(false);
            Advance(1f, End - 2);
            UnityEngine.Time.unscaledTime = clock + 1.8f;
            GameManager.Instance.World.Now = End - 1;
            BloodmoonAccuracyRuntime.RecordShot(true);
            UnityEngine.Time.unscaledTime = clock + 1.9f;
            GameManager.Instance.World.Now = End;
            BloodmoonAccuracyRuntime.Flush();
            Need(Accuracy("Host").Shots == 2 && Accuracy("Host").Hits == 1, "shutdown captures dawn before Tick");
            var reloaded = new BloodmoonAccuracyStore(Path.Combine(root, "Data"), Key());
            Need(reloaded.ForDay(7)["Host"].Shots == 2 && reloaded.ForDay(7)["Host"].Hits == 1,
                "shutdown saves last shot before Tick");
            Advance(2.1f, End + 1);
            Need(Accuracy("Host").Shots == 2, "later update does not duplicate final shot");
        });

        Case("quick join-fire-leave retains accuracy with zero kills without changing kill TXT", delegate
        {
            Setup(true, true, false);
            var transient = new EntityPlayer { entityId = 3, EntityName = "Transient", Kills = 30 };
            GameManager.Instance.World.Players.list.Add(transient);
            var connection = new ClientInfo { entityId = 3 };
            ConnectionManager.Instance.Clients.Client = connection;
            Advance(.1f, Start + 1);
            Need(!BloodmoonRecording.LiveNight.Kills.ContainsKey("Transient"), "player joins between kill samples");
            Send(connection, 1, Report(Guid.NewGuid().ToString("N"), 5, 3), null, 3);
            GameManager.Instance.World.Players.list.Remove(transient);
            ConnectionManager.Instance.Clients.Client = null;
            Advance(1.1f, Start + 2);
            Need(!BloodmoonRecording.LiveNight.Kills.ContainsKey("Transient"), "player leaves before first kill sample");
            var live = StatisticsSync.Snapshot().Data.Nights.Single(n => n.Day == 7);
            Need(live.Kills["Transient"] == 0 && live.Accuracy["Transient"].Shots == 5 &&
                live.Accuracy["Transient"].Percentage == "60 %", "accuracy-only player appears with zero kills");
            Advance(2.2f, End);
            var history = StatisticsSync.Snapshot().Data.Nights.Single(n => n.Day == 7);
            Need(history.Kills["Transient"] == 0 && history.Accuracy["Transient"].Hits == 3,
                "accuracy-only player survives completed-night merge");
            var killLog = BloodmoonLog.Parse(File.ReadAllText(Path.Combine(root, "Data", "bloodmoon_kills.txt")));
            Need(!killLog.Nights.Single(n => n.Day == 7).Kills.ContainsKey("Transient"),
                "derived zero-kill row does not rewrite original kill TXT");
        });

        Case("clock rollback cannot reopen completed accuracy or use the late-report grace", delegate
        {
            Setup(true, false, false);
            var client = Authenticate(); string session = Guid.NewGuid().ToString("N");
            BloodmoonAccuracyRuntime.RecordShot(true);
            Send(client, 1, Report(session, 2, 1), null);
            Advance(1.1f, End - 1);
            Advance(2.2f, End);
            Need(BloodmoonRecording.LiveNight == null && Accuracy("Host").Shots == 1 && Accuracy("Remote").Shots == 2,
                "night completed before rollback");
            GameStats.Day = 7;
            Advance(3.3f, Start + 100);
            Need(!BloodmoonAccuracyRuntime.IsRecording && BloodmoonRecording.LiveNight == null,
                "completed bloodmoon remains closed after clock rollback");
            BloodmoonAccuracyRuntime.RecordShot(true);
            Send(client, 2, Report(session, 4, 3), null);
            Need(BloodmoonAccuracyRuntime.Report().Shots == 1 && Accuracy("Host").Shots == 1 &&
                Accuracy("Remote").Shots == 2, "rollback rejects local shots and remote reports inside grace time");
            Advance(4.4f, End + 1); Send(client, 3, Report(session, 3, 2), null);
            Need(Accuracy("Remote").Shots == 3 && Accuracy("Remote").Hits == 2,
                "legitimate post-dawn report remains accepted after forward correction");
        });

        Case("authenticated reports, forged sender, replay, reordered and wrong-day frames", delegate
        {
            Setup(true, true, false);
            var client = Authenticate(); string session = Guid.NewGuid().ToString("N");
            Send(client, 1, Report(session, 10, 7), null);
            Need(Accuracy("Remote").Shots == 10 && Accuracy("Remote").Hits == 7, "authenticated report");
            long revision = BloodmoonAccuracyRuntime.Revision;
            Advance(1.1f, Start + 1);
            Send(client, 2, Report(session, 20, 14), null, 1);
            Send(new ClientInfo { entityId = 2 }, 3, Report(session, 20, 14), null);
            Send(client, 4, Report(session, 20, 14), null, 2, EMessageSender.Server);
            Need(BloodmoonAccuracyRuntime.Revision == revision, "forged identity rejected");
            Send(client, 5, Report(session, 10, 7), null);
            Need(BloodmoonAccuracyRuntime.Revision == revision, "replay idempotent");
            Advance(2.2f, Start + 2); Send(client, 6, Report(session, 9, 6), null);
            Need(BloodmoonAccuracyRuntime.Revision == revision, "older cumulative report ignored");
            Advance(3.3f, Start + 3); Send(client, 7, Report(session, 20, 14, 14), null);
            Need(BloodmoonAccuracyRuntime.Revision == revision, "wrong bloodmoon day rejected");
            Advance(4.4f, Start + 4);
            var reconnect = Authenticate();
            Send(reconnect, 1, Report(Guid.NewGuid().ToString("N"), 2, 1), null);
            Need(Accuracy("Remote").Shots == 12 && Accuracy("Remote").Hits == 8, "remote reconnect session totals added");
        });

        Case("impossible within-session hit delta rejected", delegate
        {
            Setup(true, true, false); var client = Authenticate(); string session = Guid.NewGuid().ToString("N");
            Send(client, 1, Report(session, 10, 7), null);
            Advance(1.1f, Start + 1); Send(client, 2, Report(session, 10, 8), null);
            Need(Accuracy("Remote").Hits == 7, "accepted extra hit without a newly emitted shot");
        });

        Case("final report within response rate limit retains latest counters", delegate
        {
            Setup(true, true, false); var client = Authenticate(); string session = Guid.NewGuid().ToString("N");
            Send(client, 1, Report(session, 10, 7), null);
            int replies = client.Sent.Count;
            Advance(.5f, Start + 1); Send(client, 2, Report(session, 11, 8, 7, true), null);
            Need(Accuracy("Remote").Shots == 11 && Accuracy("Remote").Hits == 8, "last shutdown report discarded below one second");
            Need(client.Sent.Count == replies, "final report preserves reply rate limit");
            Advance(.6f, Start + 2); Send(client, 3, Report(session, 12, 9, 7, true), null);
            Need(Accuracy("Remote").Shots == 11, "only one immediate final report per connection");
        });

        Case("finished-night grace accepts final reports through thirty seconds only", delegate
        {
            Setup(true, true, false); var client = Authenticate(); string session = Guid.NewGuid().ToString("N");
            Send(client, 1, Report(session, 10, 7), null);
            Advance(2f, End); GameStats.Day = 14;
            Advance(32f, End + 1); Send(client, 2, Report(session, 11, 8), null);
            Need(Accuracy("Remote").Shots == 11, "final report accepted inside grace");
            Advance(33.1f, End + 2); Send(client, 3, Report(session, 12, 9), null);
            Need(Accuracy("Remote").Shots == 11, "final report rejected beyond grace");
        });

        Case("live kills and accuracy update without retransmitting unchanged history", delegate
        {
            Setup(true, true, true); var client = Authenticate(); string session = Guid.NewGuid().ToString("N");
            Send(client, 1, Report(session, 10, 7), null);
            initialExtended = client.Sent.ToArray(); var first = Delta(initialExtended);
            Need(first.HistoryPayload != null, "initial history included");
            client.Sent.Clear();
            GameManager.Instance.World.Players.list[1].Kills += 5;
            Advance(1.1f, Start + 1); Send(client, 2, Report(session, 12, 8), first.HistoryHash);
            updatedExtended = client.Sent.ToArray(); var second = Delta(updatedExtended);
            Need(second.HistoryPayload == null && first.HistoryHash.SequenceEqual(second.HistoryHash), "unchanged history omitted");
            var live = StatisticsWire.Decode(second.LivePayload).Nights.Single();
            Need(live.Kills["Remote"] == 5 && live.Accuracy["Remote"].Shots == 12 && live.Accuracy["Remote"].Hits == 8,
                "live kill and accuracy delta");
            serverData = StatisticsSync.Snapshot().Data;
        });

        Case("extended client roundtrip, server authentication and incremental history reuse", delegate
        {
            Setup(false, false, false);
            var request = LastRequest();
            Need(request.Extended && request.Report != null && request.Report.Day == 7 && request.Report.Shots == 0, "client advertises extended protocol and zero report");
            foreach (var raw in initialExtended) Reply(raw, request.Id, true);
            Need(StatisticsSync.Snapshot().Data == null && StatisticsSync.Snapshot().Busy, "forged server reply rejected");
            foreach (var raw in initialExtended) Reply(raw, request.Id);
            var initial = StatisticsSync.Snapshot().Data;
            Need(initial.Nights.Single(n => n.Day == 7).Accuracy["Remote"].Percentage == "70 %", "extended client accuracy received");
            for (int index = 0; index < 3; index++) BloodmoonAccuracyRuntime.RecordShot(index < 2);
            Advance(1.1f, Start + 1); request = LastRequest();
            Need(request.Extended && request.Report.Shots == 3 && request.Report.Hits == 2, "client cumulative shot report");
            foreach (var raw in updatedExtended) Reply(raw, request.Id);
            var updated = StatisticsSync.Snapshot().Data;
            Need(updated.Nights.Single(n => n.Day == 7).Kills["Remote"] == 5 &&
                updated.Nights.Single(n => n.Day == 7).Accuracy["Remote"].Shots == 12, "extended live update received");
            Need(Object.ReferenceEquals(initial.Nights.Single(n => n.Day == 1), updated.Nights.Single(n => n.Day == 1)), "cached historical night reused");
            BloodmoonAccuracyRuntime.RecordShot(true); StatisticsSync.FlushAccuracy();
            Need(LastRequest().Report.Shots == 4 && LastRequest().Report.Hits == 3 && LastRequest().Report.Final,
                "shutdown flush includes newest local shot and final flag");
        });

        Case("remote HUD accuracy receives late final values without extending hold time", delegate
        {
            Setup(false, false, false);
            foreach (var raw in initialExtended) Reply(raw, LastRequest().Id);
            ShotAccuracy hud;
            Need(StatisticsSync.TryHudAccuracy("Remote", out hud) && hud.Percentage == "70 %", "remote HUD receives live accuracy");
            var completed = StatisticsSync.Snapshot().Data;
            Action<BloodmoonLog> receiveFinished = delegate(BloodmoonLog history)
            {
                byte[] encoded = StatisticsWire.EncodeAccuracy(history, 0);
                byte[] emptyLive = StatisticsWire.EncodeAccuracy(BloodmoonLog.FromNights(new BloodmoonNight[0]), 0);
                byte[] delta = StatisticsDelta.Encode(encoded, StatisticsWire.Hash(encoded), emptyLive, true);
                string text = StatisticsMessage.ReplyExtended(LastRequest().Id, 0, 0, 1, delta.Length, StatisticsWire.Hash(delta), delta);
                var packet = new NetPackageChat().Setup(EChatType.Whisper, -1, text, null, EMessageSender.Server, GeneratedTextManager.BbCodeSupportMode.NotSupported);
                packet.ProcessPackage(GameManager.Instance.World, GameManager.Instance);
            };
            Advance(2f, End); receiveFinished(completed);
            Need(StatisticsSync.TryHudAccuracy("Remote", out hud) && hud.Percentage == "70 %", "remote HUD holds initial final quote");
            var revised = BloodmoonLog.FromNights(completed.Nights.Select(n => n.Day != 7 ? n : new BloodmoonNight(n.Day,
                n.Kills.ToDictionary(p => p.Key, p => p.Value), new Dictionary<string, ShotAccuracy> { { "Remote", new ShotAccuracy(12, 9) } })));
            Advance(3.1f, End + 1); receiveFinished(revised);
            Need(StatisticsSync.TryHudAccuracy("Remote", out hud) && hud.Percentage == "75 %", "remote HUD updates late final quote");
            Advance(61.9f, End + 2);
            Need(StatisticsSync.TryHudAccuracy("Remote", out hud), "remote HUD visible before original deadline");
            Advance(62f, End + 3);
            Need(!StatisticsSync.TryHudAccuracy("Remote", out hud), "late update does not extend HUD duration");
        });

        Case("client shutdown can report before protocol discovery finishes", delegate
        {
            Setup(false, false, false);
            BloodmoonAccuracyRuntime.RecordShot(true);
            StatisticsSync.FlushAccuracy();
            Need(LastRequest().Extended && LastRequest().Report.Final && LastRequest().Report.Shots == 1,
                "undiscovered server receives immediate final counters");
        });

        Case("legacy fallback begins at two seconds and retains kill statistics", delegate
        {
            Setup(false, false, false);
            Need(LastRequest().Extended, "initial discovery"); int sent = ConnectionManager.Instance.Sent.Count;
            Advance(1.99f, Start + 1);
            Need(ConnectionManager.Instance.Sent.Count == sent, "no early fallback");
            Advance(2f, Start + 2); var request = LastRequest();
            Need(request.Extended && !request.Headshots && ConnectionManager.Instance.Sent.Count == sent + 1, "accuracy protocol at two seconds");
            Advance(4f, Start + 4); request = LastRequest();
            Need(!request.Extended && ConnectionManager.Instance.Sent.Count == sent + 2, "legacy request at four seconds");
            byte[] bytes = StatisticsWire.Encode(serverData, 7);
            var frame = StatisticsMessage.Reply(request.Id, 0, 0, 1, bytes.Length, StatisticsWire.Hash(bytes), bytes);
            var packet = new NetPackageChat().Setup(EChatType.Whisper, -1, frame, null, EMessageSender.Server, GeneratedTextManager.BbCodeSupportMode.NotSupported);
            packet.ProcessPackage(GameManager.Instance.World, GameManager.Instance);
            var snapshot = StatisticsSync.Snapshot();
            Need(snapshot.Data != null && !snapshot.Busy && snapshot.Error == null &&
                snapshot.Data.Nights.Single(n => n.Day == 7).Kills["Remote"] == 5, "legacy kill statistics remain available");
            Need(snapshot.Data.Nights.Single(n => n.Day == 7).Accuracy.Count == 0, "legacy accuracy stays unavailable");
        });

        foreach (bool dedicated in new[] { false, true })
        {
            Case((dedicated ? "dedicated" : "hosted") + " headshots, old-client compatibility, live delta and final history", delegate
            {
                Setup(true, dedicated, true); var client = Authenticate();
                var report = Report(Guid.NewGuid().ToString("N"), 10, 7); report.Headshots = 3;
                BloodmoonAccuracyRuntime.RecordShot(true, true);
                Advance(1.1f, Start + 1);
                Send(client, 1, report, null, headshots: true);
                byte[][] firstPackets = client.Sent.ToArray(); var first = Delta(firstPackets);
                Need(StatisticsMessage.Decode(Deserialize(firstPackets[0]).Message).Headshots, "new response kind");
                Need(StatisticsWire.Decode(first.LivePayload).Nights[0].Accuracy["Remote"].Headshots == 3, "server transmits remote headshots");
                if (!dedicated) Need(Accuracy("Host").Headshots == 1, "host headshot captured");
                client.Sent.Clear(); Advance(2.2f, Start + 2);
                Send(client, 2, null, null);
                var oldReply = Delta(client.Sent);
                Need(oldReply.LivePayload[0] == 3 && !StatisticsWire.Decode(oldReply.LivePayload).Nights[0].Accuracy["Remote"].Headshots.HasValue,
                    "0.3.1 client receives original format");
                client.Sent.Clear(); Advance(3.3f, Start + 3);
                report.Shots = 11; report.Hits = 8; report.Headshots = 4;
                Send(client, 3, report, first.HistoryHash, headshots: true);
                byte[][] secondPackets = client.Sent.ToArray(); var second = Delta(secondPackets);
                Need(second.HistoryPayload == null && Accuracy("Remote").Headshots == 4, "only live values sent for new shot");
                client.Sent.Clear(); Advance(5f, End);
                Send(client, 4, null, first.HistoryHash, headshots: true);
                byte[][] finishedPackets = client.Sent.ToArray(); var finished = Delta(finishedPackets);
                Need(StatisticsWire.Decode(finished.HistoryPayload).Nights.Single(n => n.Day == 7).Accuracy["Remote"].Headshots == 4,
                    "completed headshots enter history");
                Need(new BloodmoonAccuracyStore(Path.Combine(root, "Data"), Key()).ForDay(7)["Remote"].Headshots == 4,
                    "completed headshots saved on server");
                Setup(false, false, false);
                Need(LastRequest().Headshots && LastRequest().Report.Headshots == 0, "new client advertises headshots");
                foreach (var raw in firstPackets) Reply(raw, LastRequest().Id);
                ShotAccuracy hud;
                Need(StatisticsSync.TryHudAccuracy("Remote", out hud) && hud.Headshots == 3, "client HUD receives headshots");
                BloodmoonAccuracyRuntime.RecordShot(true, true); BloodmoonAccuracyRuntime.RecordShot(true, false);
                Advance(1.1f, Start + 1);
                Need(LastRequest().Headshots && LastRequest().Report.Headshots == 1 && LastRequest().Report.Hits == 2, "client reports body/head separately");
                foreach (var raw in secondPackets) Reply(raw, LastRequest().Id);
                Need(StatisticsSync.TryHudAccuracy("Remote", out hud) && hud.Headshots == 4, "client applies headshot delta");
                Advance(2.2f, End);
                foreach (var raw in finishedPackets) Reply(raw, LastRequest().Id);
                Need(StatisticsSync.TryHudAccuracy("Remote", out hud) && hud.Headshots == 4, "finished HUD holds headshots");
                BloodmoonAccuracyRuntime.RecordShot(true, true); StatisticsSync.FlushAccuracy();
                Need(LastRequest().Headshots && LastRequest().Report.Headshots == 1 && LastRequest().Report.Final,
                    "final report keeps headshots and excludes shots after dawn");
            });
        }
        Case("0.3.1 server fallback keeps accuracy protocol after discovery", delegate
        {
            Setup(false, false, false); Advance(2f, Start + 1);
            Need(LastRequest().Extended && !LastRequest().Headshots, "fallback to 0.3.1 request");
            foreach (var raw in initialExtended) Reply(raw, LastRequest().Id);
            Advance(3.1f, Start + 2);
            Need(LastRequest().Extended && !LastRequest().Headshots && LastRequest().Report.Headshots == null,
                "old-server reports remain readable");
        });

        Console.WriteLine("Accuracy integration: " + (cases - failures) + "/" + cases + " cases passed. Artifacts: " + output);
        return failures == 0 ? 0 : 1;
    }
}
