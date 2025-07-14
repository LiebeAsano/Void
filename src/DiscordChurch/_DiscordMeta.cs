using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoidTemplate.DiscordChurch
{
    public class _DiscordMeta
    {
        public static void Init()
        {
            RPCLastWish.TryInitiateDiscord();
            RPCLastWish.Hook();
        }
    }
}
