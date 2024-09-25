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
	public static bool SimpleFood => cfgSimpleFood.Value;
	#endregion

	#region configs
	internal static Configurable<bool> cfgSaintArenaSpears;
	internal static Configurable<bool> cfgSaintArenaAscension;
	internal static Configurable<bool> cfgSimpleFood;
	#endregion
}
