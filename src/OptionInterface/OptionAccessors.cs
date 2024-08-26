using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VoidTemplate.PlayerMechanics;

namespace VoidTemplate.OptionInterface;

public static class OptionAccessors
{
	#region accessors
	public static bool SaintArenaSpears => cfgSaintArenaSpears.Value;
	public static bool SaintArenaAscension => cfgSaintArenaAscension.Value;
	#endregion

	#region configs
	internal static Configurable<bool> cfgSaintArenaSpears;
	internal static Configurable<bool> cfgSaintArenaAscension;
	#endregion
}
