using static VoidTemplate.Useful.Utils;
namespace VoidTemplate.PlayerMechanics;

public static class ColdImmunityPatch
{
    public static void Hook()
    {
        On.Creature.HypothermiaUpdate += static (orig, self) =>
        {
            if (self is Player p && (p.IsVoid() || p.IsViy()))
            {
                p.Hypothermia = 0;
                p.HypothermiaExposure = 0f;
                return;
            }
            orig(self);
        };
        On.Creature.HypothermiaBodyContactWarmup += static (orig, self, otherself, other) =>
        {
            bool result = orig(self, otherself, other);
            if (self is Player player && (player.IsVoid() || player.IsViy())) result = true;
            return result;
        };
    }
}