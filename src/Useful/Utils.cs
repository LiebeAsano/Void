using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoidTemplate.Useful;
internal static class Utils
{
    public const int TicksPerSecond = 40;
    public static string TranslateStringComplex(this string str) => RWCustom.Custom.rainWorld.inGameTranslator.Translate(str).Replace("<LINE>", "\n");
}
