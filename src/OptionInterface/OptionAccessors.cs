
namespace VoidTemplate.OptionInterface;

public static class OptionAccessors
{
	#region accessors

    public static bool SaintArenaSpears => cfgSaintArenaSpears.Value;
	public static bool SaintArenaAscension => cfgSaintArenaAscension.Value;
    public static bool ArenaAscensionStun => cfgArenaAscensionStun.Value;
    public static bool SimpleFood => cfgSimpleFood.Value;
    public static bool GamepadController => cfgGamepadController.Value;

    public static bool ComplexControl => cfgComplexControl.Value;
    public static bool PermaDeath => !cfgNoPermaDeath.Value;
	public static bool ForceUnlockCampaign => cfgForceUnlockCampaign.Value;
    public static int PermaDeathCycle => cfgPermaDeathCycle.Value;
    public static int EchoDeathCycle => cfgEchoDeathCycle.Value;
    #endregion

    #region configs
    internal static Configurable<bool> cfgSaintArenaSpears;
	internal static Configurable<bool> cfgSaintArenaAscension;
    internal static Configurable<bool> cfgArenaAscensionStun;
    internal static Configurable<bool> cfgGamepadController;
    internal static Configurable<bool> cfgComplexControl;
    internal static Configurable<bool> cfgSimpleFood;
	internal static Configurable<bool> cfgNoPermaDeath;
	internal static Configurable<bool> cfgForceUnlockCampaign;
    internal static Configurable<int> cfgPermaDeathCycle;
    internal static Configurable<int> cfgEchoDeathCycle;

    #endregion
}
