using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace VoidTemplate.PlayerMechanics.ViyMechanics.ViyTentacles
{
    public class ViyRotGraphics
    {
        public ViyRotModule rotControl;

        public ViyTentacleGraphics[] legs;

        public int totalLegSprites = 0;

        public ViyRotGraphics(ViyRotModule rotControl)
        {
            this.rotControl = rotControl;
        }

        public void InitSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            if (legs == null)
            {
                legs = new ViyTentacleGraphics[5];
                for (int i = 0; i < 5; i++)
                {
                    legs[i] = new(rotControl.tentacles[i], sLeaser.sprites.Length + totalLegSprites);
                    totalLegSprites += legs[i].sprites;
                }
            }
            Array.Resize(ref sLeaser.sprites, sLeaser.sprites.Length + totalLegSprites);
            for (int i = 0; i < 5; i++)
            {
                legs[i].InitiateSprites(sLeaser, rCam);
            }
        }

        public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            for (int i = 0; i < 5; i++)
            {
                legs[i].DrawSprite(sLeaser, rCam, timeStacker, camPos);
            }
        }

        public void Reset()
        {
            Vector2 pos = rotControl.player.mainBodyChunk.pos;
            for (int i = 0; i < 5; i++)
            {
                legs[i].Reset(pos);
                legs[i].tentacle.Reset(pos);
            }
        }

        public void Update()
        {
            for (int i = 0; i < 5; i++)
            {
                legs[i].Update();
            }
        }

        public void MoveBehindFirstSprite(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            if (legs != null)
            {
                for (int i = 0; i < 5; i++)
                {
                    legs[i].MoveBehindFirstSprite(sLeaser, rCam);
                }
            }
        }

        public void ApplyPallete(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            for (int i = 0; i < 5; i++)
            {
                legs[i].ApplyPalette(sLeaser, rCam, palette);
            }
        }
    }
}
