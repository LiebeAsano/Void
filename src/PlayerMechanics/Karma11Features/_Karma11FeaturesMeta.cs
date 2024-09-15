using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoidTemplate.PlayerMechanics.Karma11Features;

internal static class _Karma11FeaturesMeta
{
    public static void Hook()
    {
        EatMeatUpdate.Hook();
        FoodChange.Hook();
        NourishmentOfObjectEaten.Hook();
    }
}
