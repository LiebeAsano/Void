using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using LizardCosmetics;

namespace VoidTemplate.Creatures
{
    public class IceLizardGraphics : LizardGraphics
    {
        public float invisTime;

        public int invisAmount;

        public bool changeVisibleState;

        public bool isVisible = true;

        public IceLizardGraphics(IceLizard ow) : base(ow)
        {
            Random.State state = Random.state;
            Random.InitState(ow.abstractCreature.ID.RandomSeed);
            int num = startOfExtraSprites + extraSprites;
            if (cosmetics.Any(x => x is not LongShoulderScales) && Random.value < 0.9f)
            {
                num = AddCosmetic(num, new LongShoulderScales(this, num));
            }
            num = AddCosmetic(num, new LongShoulderScales(this, num));
            num = AddCosmetic(num, new SpineSpikes(this, num));
            if (Random.value < 0.5f)
            {
                num = AddCosmetic(num, new TailFin(this, num));
            }
            else
            {
                num = AddCosmetic(num, new TailTuft(this, num));
            }
            Random.state = state;
        }

        public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            ColorBody(sLeaser, DynamicBodyColor(0));
            Color color = rCam.PixelColorAtCoordinate(lizard.mainBodyChunk.pos);
            Color color2 = rCam.PixelColorAtCoordinate(lizard.bodyChunks[1].pos);
            Color color3 = rCam.PixelColorAtCoordinate(lizard.bodyChunks[2].pos);
            if (color == color2)
            {
                whitePickUpColor = color;
            }
            else if (color2 == color3)
            {
                whitePickUpColor = color2;
            }
            else if (color3 == color)
            {
                whitePickUpColor = color3;
            }
            else
            {
                whitePickUpColor = (color + color2 + color3) / 3f;
            }

            whiteCamoColor = whitePickUpColor;

            base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
            int num = SpriteLimbsColorStart - SpriteLimbsStart;
            for (int i = SpriteLimbsStart; i < SpriteLimbsEnd; i++)
            {
                sLeaser.sprites[i + num].color = DynamicBodyColor(0);
            }
            /*for (int i = startOfExtraSprites; i < TotalSprites; i++)
            {
                sLeaser.sprites[i].color = Color.Lerp(sLeaser.sprites[i].color, whiteCamoColor, whiteCamoColorAmount);
            }*/
            if (changeVisibleState)
            {
                if (isVisible)
                {
                    if (invisAmount >= 90)
                    {
                        MakeVisibleBodyAndHead(sLeaser, true);
                    }
                    if (invisAmount > 0)
                    {
                        invisAmount--;
                    }
                    else
                    {
                        changeVisibleState = false;
                    }
                }
                else
                {
                    if (invisAmount < 90)
                    {
                        invisAmount++;
                    }
                    else
                    {
                        changeVisibleState = false;
                    }
                }

                invisTime = 0;
            }
            whiteCamoColorAmount = Mathf.InverseLerp(0f, 90f, invisAmount);
            if (!isVisible && whiteCamoColorAmount == 1)
            {
                MakeVisibleBodyAndHead(sLeaser, false);
            }
        }

        public override void Update()
        {
            base.Update();
            invisTime++;
            if (!changeVisibleState && ((isVisible && invisTime >= Random.Range(80, 120)) || (!isVisible && invisTime >= Random.Range(120, 200))))
            {
                changeVisibleState = true;
                isVisible = !isVisible;
            }
        }

        public void MakeVisibleBodyAndHead(RoomCamera.SpriteLeaser sLeaser, bool isVisible)
        {
            sLeaser.sprites[SpriteBodyMesh].isVisible = isVisible;
            sLeaser.sprites[SpriteTail].isVisible = isVisible;
            for (int i = SpriteBodyCirclesStart; i < SpriteBodyCirclesEnd; i++)
            {
                sLeaser.sprites[i].isVisible = isVisible;
            }
            for (int i = SpriteLimbsStart; i < SpriteLimbsEnd; i++)
            {
                sLeaser.sprites[i].isVisible = isVisible;
            }
            for (int i = SpriteHeadStart; i < SpriteLimbsColorEnd; i++)
            {
                sLeaser.sprites[i].isVisible = isVisible;
            }
            for (int i = startOfExtraSprites; i < TotalSprites; i++)
            {
                sLeaser.sprites[i].isVisible = isVisible;
            }
        }
    }
}
