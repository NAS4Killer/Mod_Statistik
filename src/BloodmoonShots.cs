using System;

namespace ModStatistik
{
    // A scope covers one attempted cartridge discharge, including all pellets
    // and penetration hits. Completed scopes are reused; no shot history grows.
    internal static class BloodmoonShots
    {
        [ThreadStatic] private static Scope current;
        [ThreadStatic] private static Scope free;
        [ThreadStatic] private static int freeCount;
        private const int PoolLimit = 8;

        internal sealed class Scope
        {
            internal Scope Previous, NextFree;
            internal object Action, Details;
            internal int Shooter;
            internal long Version;
            internal bool Active, Enabled, Emitted, Hit, Headshot;
        }

        internal struct Token
        {
            internal Scope Scope;
            internal long Version;
        }

        internal static bool HasScope { get { return current != null; } }

        internal static Token Begin(object action, object details, int shooter, bool enabled)
        {
            Scope scope;
            if (free == null) scope = new Scope();
            else
            {
                scope = free;
                free = scope.NextFree;
                freeCount--;
            }
            scope.NextFree = null;
            scope.Previous = current;
            scope.Action = action;
            scope.Details = details;
            scope.Shooter = shooter;
            scope.Enabled = enabled;
            scope.Active = true;
            scope.Emitted = false;
            scope.Hit = false;
            scope.Headshot = false;
            scope.Version = unchecked(scope.Version + 1);
            current = scope;
            return new Token { Scope = scope, Version = scope.Version };
        }

        internal static void Emitted(object action, int pelletIndex)
        {
            var scope = current;
            if (scope != null && scope.Enabled && pelletIndex == 0 &&
                ReferenceEquals(scope.Action, action)) scope.Emitted = true;
        }

        internal static bool WantsHit(object details, int shooter)
        {
            var scope = current;
            return scope != null && scope.Enabled && scope.Emitted && !scope.Headshot &&
                scope.Shooter == shooter && ReferenceEquals(scope.Details, details);
        }

        internal static void Hit(object details, int shooter, bool liveZombie, bool headshot = false)
        {
            if (liveZombie && WantsHit(details, shooter))
            { current.Hit = true; current.Headshot |= headshot; }
        }

        // Returns true only once for a completed, actually emitted shot. The
        // generation also rejects an old token after its scope has been reused.
        internal static bool End(Token token, bool completed, out bool hit)
        { bool headshot; return End(token, completed, out hit, out headshot); }

        internal static bool End(Token token, bool completed, out bool hit, out bool headshot)
        {
            hit = false;
            headshot = false;
            var scope = token.Scope;
            if (scope == null || !scope.Active || scope.Version != token.Version ||
                !ReferenceEquals(current, scope)) return false;

            bool recorded = completed && scope.Enabled && scope.Emitted;
            hit = recorded && scope.Hit;
            headshot = hit && scope.Headshot;
            current = scope.Previous;
            scope.Active = false;
            scope.Previous = null;
            scope.Action = null;
            scope.Details = null;
            if (freeCount < PoolLimit)
            {
                scope.NextFree = free;
                free = scope;
                freeCount++;
            }
            return recorded;
        }
    }
}
