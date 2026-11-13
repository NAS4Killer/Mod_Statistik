using System;
using HarmonyLib;
using UnityEngine;

namespace ModStatistik
{
    internal static class BloodmoonShotHooks
    {
        private static bool tagsReady, warned;
        private static FastTags<TagGroup.Global> firearmTags, excludedTags, zombieTag;

        private static void EnsureTags()
        {
            if (tagsReady) return;
            firearmTags = FastTags<TagGroup.Global>.Parse("gun,weapon,ranged");
            excludedTags = FastTags<TagGroup.Global>.Parse(
                "admin,tool,melee,launcher,archery,bow,crossbow,turretRanged,turretMelee");
            zombieTag = FastTags<TagGroup.Global>.Parse("zombie");
            tagsReady = true;
        }

        internal static BloodmoonShots.Token Begin(ItemActionRanged action, ItemActionData data)
        {
            bool recording = BloodmoonAccuracyRuntime.IsRecording;
            // An excluded nested action still needs a barrier so it cannot
            // contribute a hit to an outer firearm action.
            if (!recording && !BloodmoonShots.HasScope) return default(BloodmoonShots.Token);
            bool enabled = false;
            var owner = data == null || data.invData == null ? null : data.invData.holdingEntity;
            if (recording && owner is EntityPlayerLocal && !owner.isEntityRemote &&
                action != null && action.GetType() == typeof(ItemActionRanged))
            {
                EnsureTags();
                var item = data.invData.item;
                enabled = item != null && item.ItemTags.Test_AllSet(firearmTags) &&
                    !item.ItemTags.Test_AnySet(excludedTags);
            }
            if (!enabled && !BloodmoonShots.HasScope) return default(BloodmoonShots.Token);
            return BloodmoonShots.Begin(data, data == null ? null : data.attackDetails,
                owner == null ? -1 : owner.entityId, enabled);
        }

        internal static void Hit(WorldRayHitInfo hitInfo, int shooter, ItemActionAttack.AttackHitInfo details)
        {
            if (!BloodmoonShots.WantsHit(details, shooter) || hitInfo == null || hitInfo.tag == null) return;
            var target = ItemActionAttack.FindHitEntity(hitInfo) as EntityAlive;
            // Prefix timing includes the killing hit, while rejecting corpses.
            BloodmoonShots.Hit(details, shooter,
                target != null && !target.IsDead() && target.HasAnyTags(zombieTag));
        }

        internal static void Warning(Exception error)
        {
            if (warned) return;
            warned = true;
            try { Debug.LogWarning("[Mod_Statistik] Schusserfassung: " + error.GetType().Name); }
            catch (Exception) { }
        }
    }

    [HarmonyPatch(typeof(ItemActionRanged), "TryExecuteAction")]
    internal static class BloodmoonShotAttemptPatch
    {
        private static void Prefix(ItemActionRanged __instance, ItemActionData _actionData,
            out BloodmoonShots.Token __state)
        {
            __state = default(BloodmoonShots.Token);
            try { __state = BloodmoonShotHooks.Begin(__instance, _actionData); }
            catch (Exception error) { BloodmoonShotHooks.Warning(error); }
        }

        private static Exception Finalizer(Exception __exception, BloodmoonShots.Token __state)
        {
            try
            {
                bool hit;
                if (BloodmoonShots.End(__state, __exception == null, out hit))
                    BloodmoonAccuracyRuntime.RecordShot(hit);
            }
            catch (Exception error) { BloodmoonShotHooks.Warning(error); }
            return __exception;
        }
    }

    [HarmonyPatch(typeof(ItemActionRanged), "fireShot")]
    internal static class BloodmoonShotEmissionPatch
    {
        private static void Prefix(int _shotIdx, ItemActionRanged.ItemActionDataRanged _actionData)
        {
            try { BloodmoonShots.Emitted(_actionData, _shotIdx); }
            catch (Exception error) { BloodmoonShotHooks.Warning(error); }
        }
    }

    [HarmonyPatch(typeof(ItemActionAttack), "Hit")]
    internal static class BloodmoonShotHitPatch
    {
        private static void Prefix(WorldRayHitInfo hitInfo, int _attackerEntityId,
            ItemActionAttack.AttackHitInfo _attackDetails)
        {
            try { BloodmoonShotHooks.Hit(hitInfo, _attackerEntityId, _attackDetails); }
            catch (Exception error) { BloodmoonShotHooks.Warning(error); }
        }
    }
}
