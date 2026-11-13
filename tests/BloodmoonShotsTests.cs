using System;
using System.Threading;
using ModStatistik;

internal static class BloodmoonShotsTests
{
    private static void Need(bool value, string message) { if (!value) throw new Exception(message); }

    private static int Main()
    {
        try
        {
            var action = new object();
            var details = new object();
            bool hit;
            var empty = BloodmoonShots.Begin(action, details, 1, true);
            Need(!BloodmoonShots.End(empty, true, out hit) && !hit && !BloodmoonShots.HasScope,
                "empty magazine or rejected attempt is not a shot");

            var miss = BloodmoonShots.Begin(action, details, 1, true);
            BloodmoonShots.Emitted(action, 0);
            Need(BloodmoonShots.End(miss, true, out hit) && !hit, "emitted miss counted");
            Need(!BloodmoonShots.End(miss, true, out hit), "scope finalized once");

            var shotgun = BloodmoonShots.Begin(action, details, 1, true);
            for (int pellet = 0; pellet < 12; pellet++)
            {
                BloodmoonShots.Emitted(action, pellet);
                BloodmoonShots.Hit(details, 1, true);
                BloodmoonShots.Hit(details, 1, true); // penetration
            }
            Need(BloodmoonShots.End(shotgun, true, out hit) && hit,
                "shotgun pellets and penetration yield one successful shot");
            Need(!BloodmoonShots.End(miss, true, out hit), "reused scope rejects stale token");

            int shots = 0, hits = 0;
            for (int bullet = 0; bullet < 3; bullet++)
            {
                var burst = BloodmoonShots.Begin(action, details, 1, true);
                BloodmoonShots.Emitted(action, 0);
                BloodmoonShots.Hit(details, 1, bullet != 1);
                if (BloodmoonShots.End(burst, true, out hit)) { shots++; if (hit) hits++; }
            }
            Need(shots == 3 && hits == 2, "burst counts individual bullets");

            var filtered = BloodmoonShots.Begin(action, details, 1, true);
            BloodmoonShots.Hit(details, 1, true);
            Need(!BloodmoonShots.WantsHit(details, 1), "contact before emission ignored");
            BloodmoonShots.Emitted(new object(), 0);
            Need(!BloodmoonShots.WantsHit(details, 1), "another action cannot emit this shot");
            BloodmoonShots.Emitted(action, 0);
            BloodmoonShots.Hit(new object(), 1, true);
            BloodmoonShots.Hit(details, 2, true);
            BloodmoonShots.Hit(details, 1, false);
            Need(BloodmoonShots.End(filtered, true, out hit) && !hit,
                "other shooter/action, corpse, block and non-zombie do not hit");

            var killing = BloodmoonShots.Begin(action, details, 1, true);
            BloodmoonShots.Emitted(action, 0);
            bool dead = false, zombie = true;
            BloodmoonShots.Hit(details, 1, !dead && zombie); // prefix before death
            dead = true;
            BloodmoonShots.Hit(details, 1, !dead && zombie);
            Need(BloodmoonShots.End(killing, true, out hit) && hit, "killing shot retained");

            var outer = BloodmoonShots.Begin(action, details, 1, true);
            BloodmoonShots.Emitted(action, 0);
            var nestedAction = new object();
            var nestedDetails = new object();
            var inner = BloodmoonShots.Begin(nestedAction, nestedDetails, 2, true);
            BloodmoonShots.Emitted(nestedAction, 0);
            BloodmoonShots.Hit(details, 1, true);
            BloodmoonShots.Hit(nestedDetails, 2, true);
            Need(BloodmoonShots.End(inner, true, out hit) && hit, "nested independent shot");
            Need(BloodmoonShots.End(outer, true, out hit) && !hit,
                "inner shot does not contaminate outer scope");

            outer = BloodmoonShots.Begin(action, details, 1, true);
            BloodmoonShots.Emitted(action, 0);
            inner = BloodmoonShots.Begin(action, details, 1, false);
            BloodmoonShots.Emitted(action, 0);
            BloodmoonShots.Hit(details, 1, true);
            Need(!BloodmoonShots.End(inner, true, out hit), "excluded nested action is a barrier");
            BloodmoonShots.Hit(details, 1, true);
            Need(BloodmoonShots.End(outer, true, out hit) && hit, "outer scope restored after barrier");

            outer = BloodmoonShots.Begin(action, details, 1, true);
            BloodmoonShots.Emitted(action, 0);
            inner = BloodmoonShots.Begin(nestedAction, nestedDetails, 2, true);
            BloodmoonShots.Emitted(nestedAction, 0);
            BloodmoonShots.Hit(nestedDetails, 2, true);
            Need(!BloodmoonShots.End(inner, false, out hit) && !hit, "failed attempt discarded");
            BloodmoonShots.Hit(details, 1, true);
            Need(BloodmoonShots.End(outer, true, out hit) && hit && !BloodmoonShots.HasScope,
                "exception restores enclosing scope");

            outer = BloodmoonShots.Begin(action, details, 1, true);
            Exception threadError = null;
            var thread = new Thread(delegate()
            {
                try
                {
                    Need(!BloodmoonShots.HasScope, "another thread has no active scope");
                    var other = BloodmoonShots.Begin(action, details, 1, true);
                    BloodmoonShots.Emitted(action, 0);
                    bool otherHit;
                    Need(BloodmoonShots.End(other, true, out otherHit) && !otherHit, "thread-local shot");
                }
                catch (Exception error) { threadError = error; }
            });
            thread.Start(); thread.Join();
            if (threadError != null) throw threadError;
            Need(!BloodmoonShots.End(outer, true, out hit) && !BloodmoonShots.HasScope,
                "another thread cannot emit outer shot");

            Need(!BloodmoonShots.End(default(BloodmoonShots.Token), true, out hit), "default token harmless");
            Console.WriteLine("PASS: firearm scopes, misses, pellets, penetration, burst, killing hit, filters, nested restoration, exceptions, stale tokens and thread isolation.");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
}
