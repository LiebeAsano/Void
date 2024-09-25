
namespace VoidTemplate.OptionInterface;

public static class OptionAccessors
{
	#region accessors
	public static bool SaintArenaSpears => cfgSaintArenaSpears.Value;
	public static bool SaintArenaAscension => cfgSaintArenaAscension.Value;
	public static bool SimpleFood => cfgSimpleFood.Value;
	public static bool PermaDeath => cfgPermaDeath.Value;
	public static bool ForceUnlockCampaign => cfgForceUnlockCampaign.Value;
	#endregion

	#region configs
	internal static Configurable<bool> cfgSaintArenaSpears;
	internal static Configurable<bool> cfgSaintArenaAscension;
	internal static Configurable<bool> cfgSimpleFood;
	internal static Configurable<bool> cfgPermaDeath;
	internal static Configurable<bool> cfgForceUnlockCampaign;
	#endregion
}
