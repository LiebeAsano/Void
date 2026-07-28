using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Modding.Passages;
using UnityEngine;

namespace VoidTemplate.ScavDeadZones
{
    public class _ScavDeadZonesMeta
    {
        public static void Init()
        {
            ScavSpawnerManipulate.Hook();
            ScavHooks.Hook();
            SaveHooks.Hook();
            CustomPassages.Register(new ExtinctionPassage());

            //Затычка, чтобы не возникло ошибок с остуствием спрайтов для перехода.
            Futile.atlasManager.LoadAtlasFromTexture($"{VoidEnums.CustomPassageID.Extinction}A", new Texture2D(1, 1), false);
            Futile.atlasManager.LoadAtlasFromTexture($"{VoidEnums.CustomPassageID.Extinction}B", new Texture2D(1, 1), false);
        }
    }
}
