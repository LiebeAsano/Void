using System;
using System.Runtime.CompilerServices;
using static VoidTemplate.Useful.Utils;

namespace VoidTemplate.PlayerMechanics;

internal static class GraspSave
{

    public static void Hook()
    {
        On.Player.Grabbed += Player_Grabbed;
        On.Creature.Update += Creature_Update;
    }

    private static void Creature_Update(On.Creature.orig_Update orig, Creature self, bool eu)
    {
        orig(self, eu);
        if (grabbers.TryGetValue(self.abstractCreature, out var grabbedCreatures))
        {
            Array.ForEach(self.grasps, grasp =>
            {
                if (grasp != null
            && grasp.grabbed is Player p
            && p.IsVoid()
            && grabbedCreatures.TryGetValue(p.abstractCreature, out var timer))
                {
                    timer.Value++;
                    if (timer.Value > TicksUntilDeath(p))
                    {
                        self.Stun(TicksPerSecond * 5);
                    }
                }
            });

        }
    }

    static int TicksUntilDeath(Player p)
    {
        return TicksPerSecond * (p.KarmaCap == 10 ? 40 : 60);
    }

    static ConditionalWeakTable<AbstractCreature, ConditionalWeakTable<AbstractCreature, StrongBox<int>>> grabbers = new();
    private static void Player_Grabbed(On.Player.orig_Grabbed orig, Player self, Creature.Grasp grasp)
    {
        orig(self, grasp);
        if (self.IsVoid())
        {
            ConditionalWeakTable<AbstractCreature, StrongBox<int>> grabbedCreatures;
            if (!grabbers.TryGetValue(grasp.grabber.abstractCreature, out grabbedCreatures))
            {
                grabbedCreatures = new();
            }
            grabbedCreatures.Add(self.abstractCreature, new(0));

        }
    }
}
