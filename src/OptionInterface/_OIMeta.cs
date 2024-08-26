using static VoidTemplate.Useful.Utils;
using static VoidTemplate.OptionInterface.OptionAccessors;

namespace VoidTemplate.OptionInterface;

public static class _OIMeta
{
	const string uniqueprefix = "voidmod";

	public static void Initialize()
	{
		VoidOptionInterface voidOI = new();
		MachineConnector.SetRegisteredOI(ModID, voidOI);
		voidOI.config.configurables.Clear();

		//IMPORTANT: the creation of checkboxes uses first tag as text for checkbox
		OptionAccessors.cfgSaintArenaAscension = voidOI.config.Bind<bool>(uniqueprefix + "SaintArenaAscension", true, new ConfigurableInfo("Allows Saint to use ascension mechanics in arena", tags: "Saint Arena Ascension"));
		OptionAccessors.cfgSaintArenaSpears = voidOI.config.Bind<bool>(uniqueprefix + "SaintArenaSpears", true, new ConfigurableInfo("Allows Saint to wield weapons in arena", tags: "Saint Arena Spears"));

	}
}
