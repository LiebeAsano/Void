using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoidTemplate.MenuTinkery;
public static class _MenuMeta
{
    public static void Startup()
    {
        DisablePassage.Hook();
        MenuHooks.Hook();
        SelectScreenScenes.Hook();
        DreamAssociatedSound.Startup();
    }
}
