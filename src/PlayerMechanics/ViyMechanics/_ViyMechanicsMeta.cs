using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace VoidTemplate.PlayerMechanics.ViyMechanics
{
    public static class _ViyMechanicsMeta
    {
        public static void Hook()
        {
            ProtoViyAI.Hook();
            ProtoViyGraphics.Hook();
            ViyBitByPlayer.Hook();
            ViyMaul.Hook();
            ViyTail.Hook();
            ViyThrowSpear.Hook();
            ViyViolence.Hook();
            VoidViySwitch.Hook();
            ViyTentacles.TentaclesPlayerHooks.Hook();
            ViyTentacles.TentaclesPlayerGraphicsHooks.Hook();
            Viy3rdBodyChunk.Hook();
        }
    }
}
