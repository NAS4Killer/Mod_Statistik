using System;
using System.IO;
using HarmonyLib;
using UnityEngine;

namespace ModStatistik
{
    // Only NetPackageChat is used on the wire. No custom NetPackage subclasses
    // may be added here: the game requires every such type during login.
    internal static class StatisticsTransport
    {
        internal static void Initialize()
        {
            new Harmony("NAS4Killer.ModStatistik.Transport").PatchAll(typeof(StatisticsTransport).Assembly);
        }

        private static NetPackageChat Package(string message, int sender, EMessageSender origin)
        {
            return NetPackageManager.GetPackage<NetPackageChat>().Setup(EChatType.Whisper,
                sender, message, null, origin, GeneratedTextManager.BbCodeSupportMode.NotSupported);
        }

        internal static void SendRequest(int id, byte[] hash)
        {
            var player = GameManager.Instance.World.GetPrimaryPlayer();
            if (player == null) throw new InvalidOperationException("Spieler noch nicht bereit.");
            ConnectionManager.Instance.SendToServer(Package(StatisticsMessage.Request(id, hash),
                player.entityId, EMessageSender.SenderIdAsPlayer), false);
        }

        internal static void SendReply(ClientInfo client, int id, byte result, int index,
            int count, int total, byte[] hash, byte[] chunk)
        {
            // A direct reply to the authenticated requester; never broadcast.
            client.SendPackage(Package(StatisticsMessage.Reply(id, result, index, count,
                total, hash, chunk), -1, EMessageSender.Server));
        }

        internal static bool Process(NetPackageChat package, string message, EChatType type,
            int senderEntityId, EMessageSender origin, World world)
        {
            if (!StatisticsMessage.IsReserved(message)) return true;
            // Even invalid reserved messages must never reach chat display/broadcast.
            if (world == null || ConnectionManager.Instance == null) return false;
            bool server = GameManager.IsDedicatedServer || ConnectionManager.Instance.IsServer;
            if (type != EChatType.Whisper) return false;
            if (server)
            {
                if (origin != EMessageSender.SenderIdAsPlayer || package.Sender == null ||
                    senderEntityId != package.Sender.entityId) return false;
            }
            else if (origin != EMessageSender.Server || senderEntityId != -1) return false;
            try
            {
                var frame = StatisticsMessage.Decode(message);
                if (server && frame.IsRequest)
                    StatisticsSync.HandleRequest(package.Sender, frame.Id, frame.Hash);
                else if (!server && !frame.IsRequest)
                    StatisticsSync.HandleReply(frame.Id, frame.Result, frame.Index, frame.Count,
                        frame.Total, frame.Hash, frame.Chunk);
            }
            catch (InvalidDataException) { }
            catch (EndOfStreamException) { }
            catch (FormatException) { }
            catch (Exception e) { Debug.LogWarning("[Mod_Statistik] Uebertragung: " + e.GetType().Name); }
            return false;
        }
    }

    [HarmonyPatch(typeof(NetPackageChat), "ProcessPackage")]
    internal static class StatisticsChatPatch
    {
        private static bool Prefix(NetPackageChat __instance, string ___msg, EChatType ___chatType,
            int ___senderEntityId, EMessageSender ___msgSender, World __0)
        {
            return StatisticsTransport.Process(__instance, ___msg, ___chatType,
                ___senderEntityId, ___msgSender, __0);
        }
    }
}
