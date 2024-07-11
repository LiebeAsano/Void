namespace VoidTemplate;

public static class ColdImmunityPatch
{
    public static void Hook()
    {
        On.Creature.HypothermiaUpdate += static (orig, self) =>
        {
            orig(self);
            if (self is Player p && p.slugcatStats.name == StaticStuff.TheVoid) p.Hypothermia = 0;
        };
        On.Creature.HypothermiaBodyContactWarmup += static (orig, self, otherself, other) =>
        {
            bool result = orig(self, otherself, other);
            if (self is Player player && player.slugcatStats.name == StaticStuff.TheVoid) result = true;
            return result;
        };
    }
}