using static VoidTemplate.Useful.Utils;
using static VoidTemplate.OptionInterface.OptionAccessors;

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

	}
}
