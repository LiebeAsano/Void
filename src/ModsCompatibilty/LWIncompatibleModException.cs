using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoidTemplate.ModsCompatibilty
{
    public class LWIncompatibleModException(string modName) : Exception($"\nRain world: Last Wish is incompatible with \"{modName}\" mod.\nPlease disable \"{modName}\".")
    {
    }
}
