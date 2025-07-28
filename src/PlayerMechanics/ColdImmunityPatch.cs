using static VoidTemplate.Useful.Utils;
namespace VoidTemplate.PlayerMechanics;

public static class ColdImmunityPatch
{
	public static void Hook()
	{
		On.Creature.HypothermiaUpdate += static (orig, self) =>
		{
            if (self is Player p && p.AreVoidViy())
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
			if (self is Player player && player.AreVoidViy()) result = true;
			return result;
		};
	}
}