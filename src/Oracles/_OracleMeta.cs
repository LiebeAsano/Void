using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VoidTemplate.MenuTinkery;

namespace VoidTemplate.Oracles;

internal static class _OracleMeta
{
    public static void Hook()
    {
        OracleHooks.Hook();
        SLOracle.Hook();

    }
}
