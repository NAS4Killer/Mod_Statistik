using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HarmonyLib;
using ModStatistik;

namespace UnityEngine
{
    public struct Vector3 { }
    public static class Debug { public static void LogWarning(object value) { } }
}
public static class TagGroup { public sealed class Global { } }
public struct FastTags<T>
{
    private HashSet<string> names;
    public static FastTags<T> Parse(string value) { return new FastTags<T> { names = new HashSet<string>(value.Split(',')) }; }
    public bool Test_AllSet(FastTags<T> other) { return names != null && names.IsSupersetOf(other.names); }
    public bool Test_AnySet(FastTags<T> other) { return names != null && names.Overlaps(other.names); }
}
public class Entity { public int entityId; public bool isEntityRemote; }
public class EntityAlive : Entity
{
    public bool Dead, Zombie;
    public bool IsDead() { return Dead; }
    public bool HasAnyTags(FastTags<TagGroup.Global> value) { return Zombie; }
}
public class EntityPlayerLocal : EntityAlive { }
public class ItemClass { public FastTags<TagGroup.Global> ItemTags; }
public class ItemInventoryData { public EntityAlive holdingEntity; public ItemClass item; }
public class ItemActionData { public ItemInventoryData invData; public ItemActionAttack.AttackHitInfo attackDetails; }
public class WorldRayHitInfo { public string tag = "E_BP_head"; public EntityAlive Target; public bool Kill; }
public class ItemActionAttack
{
    public class AttackHitInfo { }
    public static Entity FindHitEntity(WorldRayHitInfo hitInfo) { return hitInfo.Target; }
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Hit(WorldRayHitInfo hitInfo, int _attackerEntityId, AttackHitInfo _attackDetails)
    {
        if (hitInfo.Kill && hitInfo.Target != null) hitInfo.Target.Dead = true;
    }
}
public class ItemActionRanged
{
    public class ItemActionDataRanged : ItemActionData { }
    public int Pellets = 1, HitPellet = -1, BodyCalls;
    public bool NoAmmo;
    public EntityAlive Target;
    public Exception OriginalError;
    public Action Nested;
    [MethodImpl(MethodImplOptions.NoInlining)]
    public virtual void TryExecuteAction(ItemActionData _actionData)
    {
        BodyCalls++;
        if (NoAmmo) return;
        for (int index = 0; index < Pellets; index++)
        {
            bool found = false;
            fireShot(index, (ItemActionDataRanged)_actionData, ref found);
        }
        if (Nested != null) Nested();
        if (OriginalError != null) throw OriginalError;
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    public virtual UnityEngine.Vector3 fireShot(int _shotIdx, ItemActionDataRanged _actionData, ref bool hitEntityFound)
    {
        if (_shotIdx == HitPellet)
        {
            var info = new WorldRayHitInfo { Target = Target, Kill = true };
            ItemActionAttack.Hit(info, _actionData.invData.holdingEntity.entityId, _actionData.attackDetails);
            ItemActionAttack.Hit(info, _actionData.invData.holdingEntity.entityId, _actionData.attackDetails);
        }
        return new UnityEngine.Vector3();
    }
}
public class ItemActionLauncher : ItemActionRanged { }
namespace ModStatistik
{
    internal static class BloodmoonAccuracyRuntime
    {
        internal static bool Recording = true, ThrowRead, ThrowRecord;
        internal static int Shots, Hits;
        internal static bool IsRecording { get { if (ThrowRead) throw new Exception("runtime unavailable"); return Recording; } }
        internal static void RecordShot(bool hit)
        {
            if (ThrowRecord) throw new Exception("storage unavailable");
            Shots++; if (hit) Hits++;
        }
    }
}
internal static class HooksHarness
{
    private static void Need(bool value, string message) { if (!value) throw new Exception(message); }
    private static ItemActionRanged.ItemActionDataRanged Data(string tags)
    {
        return new ItemActionRanged.ItemActionDataRanged
        {
            invData = new ItemInventoryData { holdingEntity = new EntityPlayerLocal { entityId = 12 },
                item = new ItemClass { ItemTags = FastTags<TagGroup.Global>.Parse(tags) } },
            attackDetails = new ItemActionAttack.AttackHitInfo()
        };
    }
    private static int Main()
    {
        try
        {
            new Harmony("ModStatistik.ShotHooksHarness").PatchAll(typeof(BloodmoonShotHooks).Assembly);
            var data = Data("gun,weapon,ranged");
            var gun = new ItemActionRanged();
            gun.TryExecuteAction(data);
            Need(BloodmoonAccuracyRuntime.Shots == 1 && BloodmoonAccuracyRuntime.Hits == 0, "miss");
            gun.Pellets = 12; gun.HitPellet = 7; gun.Target = new EntityAlive { Zombie = true };
            gun.TryExecuteAction(data);
            Need(BloodmoonAccuracyRuntime.Shots == 2 && BloodmoonAccuracyRuntime.Hits == 1 && gun.Target.Dead,
                "pellet, penetration and killing hit");
            gun.TryExecuteAction(data);
            Need(BloodmoonAccuracyRuntime.Shots == 3 && BloodmoonAccuracyRuntime.Hits == 1, "corpse");
            gun.NoAmmo = true; gun.TryExecuteAction(data); gun.NoAmmo = false;
            Need(BloodmoonAccuracyRuntime.Shots == 3, "empty weapon");
            data.invData.holdingEntity.isEntityRemote = true; gun.TryExecuteAction(data);
            data.invData.holdingEntity.isEntityRemote = false;
            data.invData.holdingEntity = new EntityAlive { entityId = 12 }; gun.TryExecuteAction(data);
            data.invData.holdingEntity = new EntityPlayerLocal { entityId = 12 };
            foreach (string tags in new[] { "tool,melee", "gun,weapon,ranged,admin", "turretRanged,weapon,ranged", "archery,bow,weapon,ranged" })
                gun.TryExecuteAction(Data(tags));
            new ItemActionLauncher().TryExecuteAction(data);
            Need(BloodmoonAccuracyRuntime.Shots == 3, "remote, AI, tools, admin weapons, turret and launcher excluded");
            BloodmoonAccuracyRuntime.Recording = false; gun.TryExecuteAction(data); BloodmoonAccuracyRuntime.Recording = true;
            Need(BloodmoonAccuracyRuntime.Shots == 3, "outside bloodmoon");
            gun.Target = new EntityAlive { Zombie = true };
            gun.OriginalError = new InvalidOperationException("vanilla error");
            Exception caught = null;
            try { gun.TryExecuteAction(data); } catch (Exception error) { caught = error; }
            Need(Object.ReferenceEquals(caught, gun.OriginalError) && !BloodmoonShots.HasScope && BloodmoonAccuracyRuntime.Shots == 3,
                "original error preserved and scope restored");
            gun.OriginalError = null;
            BloodmoonAccuracyRuntime.ThrowRead = true; gun.TryExecuteAction(data); BloodmoonAccuracyRuntime.ThrowRead = false;
            BloodmoonAccuracyRuntime.ThrowRecord = true; gun.TryExecuteAction(data); BloodmoonAccuracyRuntime.ThrowRecord = false;
            Need(!BloodmoonShots.HasScope && BloodmoonAccuracyRuntime.Shots == 3, "our errors never stop firearm");
            var inner = new ItemActionRanged { Target = new EntityAlive { Zombie = true }, HitPellet = 0 };
            gun.HitPellet = -1; gun.Nested = delegate { inner.TryExecuteAction(data); };
            gun.TryExecuteAction(data); gun.Nested = null;
            Need(BloodmoonAccuracyRuntime.Shots == 5 && BloodmoonAccuracyRuntime.Hits == 2 && !BloodmoonShots.HasScope,
                "nested scopes independently finalize");
            Console.WriteLine("PASS: actual Harmony prefix/finalizer interception, local firearm filters, pellet and killing hits, misses, original exceptions and instrumentation fault isolation.");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
}
