using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using VoidTemplate.Useful;
using static VoidTemplate.Useful.Utils;

namespace VoidTemplate.PlayerMechanics;

internal static class SpearmasterAntiMechanic
{
    const float SecondsForDelayedDeath = 1f;
    static int TicksForDelayedDeath => (int)(SecondsForDelayedDeath * TicksPerSecond);
    static readonly ConditionalWeakTable<Player, StrongBox<int>> deathMarks = new ();
    public static void Hook()
    {
        //kill spearmaster if it tries to feed on void
        On.Spear.HitSomething += Spear_HitSomething;
        //delayed death
        On.Player.Update += Player_Update;
    }

    private static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
    {
        orig(self, eu);
        if(deathMarks.TryGetValue(self, out var deathMark))
        {
            if (self.room?.game is RainWorldGame game && (game.clock - deathMark.Value) > TicksForDelayedDeath)
                self.Die();
        }
    }

    private static bool Spear_HitSomething(On.Spear.orig_HitSomething orig, Spear self, SharedPhysics.CollisionResult result, bool eu)
    {
        if(result.obj is Player victim 
            && victim.IsVoid() 
            && self.Spear_NeedleCanFeed()
            && self.thrownBy is Player thrower)
        {
            deathMarks.Add(thrower, new(self.room.game.clock));
        }
        return orig(self, result, eu);
    }
}
