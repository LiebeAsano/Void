using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoidTemplate.Useful;

internal static class Utils
{
    public static bool In<T>(this T obj, params T[] args)
    {
        return args.Contains(obj);
    }
}
