using static VoidTemplate.Useful.Utils;

namespace VoidTemplate.OptionInterface;

public static class _OIMeta
{
	const string uniqueprefix = "voidmod";

	//how to add options into OptionInterface:
	//Create config in OptionAccessors (and preferably add an accessor to it)
	//Bind it here
	//Add UI to it in VoidOptionInterface
	//Preferably, if you plan on making complicated methods with OI, make them as extensions in UIExtensions

	public static void Initialize()
	{
		VoidOptionInterface voidOI = new();
		MachineConnector.SetRegisteredOI(ModID, voidOI);
		voidOI.config.configurables.Clear();

		//IMPORTANT: the creation of checkboxes uses first tag as text for checkbox
		OptionAccessors.cfgSaintArenaAscension = voidOI.config.Bind<bool>(uniqueprefix + "SaintArenaAscension", true, new ConfigurableInfo("Allows Saint to use ascension mechanic in arena", tags: "Saint ascension"));
		OptionAccessors.cfgSaintArenaSpears = voidOI.config.Bind<bool>(uniqueprefix + "SaintArenaSpears", true, new ConfigurableInfo("Allows Saint to throw spears in arena", tags: "Saint wields weapon"));
		OptionAccessors.cfgSimpleFood = voidOI.config.Bind<bool>(uniqueprefix + "SimpleFood", false, new ConfigurableInfo("Gives you whole pips when eating food instead of half pips", tags: "Simplified hunger"));
		// to be implemented
		OptionAccessors.cfgNoPermaDeath = voidOI.config.Bind<bool>(uniqueprefix + "NonPermaDeath", false, new ConfigurableInfo("Disables permadeath for Void, but also closes access to the true ending", tags: "Disable permadeath"));
		OptionAccessors.cfgForceUnlockCampaign = voidOI.config.Bind<bool>(uniqueprefix + "UnlockCampaign", false, new ConfigurableInfo("Removes the requirement to complete as Hunter to play this mod", tags: "Unlock campaign"));

	}
}
