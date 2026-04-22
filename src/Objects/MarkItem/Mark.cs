using Fisobs.Core;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace VoidTemplate.Objects.MarkItem
{
    public class Mark : PlayerCarryableItem, IDrawable
    {
        public Vector2 rotation;

        public Vector2 lastRotation;

        public MarkType MType { get => (abstractPhysicalObject as MarkAbstract).markType; }

        static Mark()
        {
            Texture2D v2 = new(5, 5)
            {
                filterMode = FilterMode.Point
            };
            for (int x = 0; x < 5; x++)
            {
                for (int y = 0; y < 5; y++)
                {
                    if (x == 2 || (y == 2 && x < 2) || (y < 2 && x == 4 - y))
                        v2.SetPixel(x, y, Color.white);
                    else
                        v2.SetPixel(x, y, new());
                }
            }
            v2.Apply(true);
            if (Futile.atlasManager.DoesContainAtlas("MarkV2Tex"))
            {
                Futile.atlasManager.UnloadAtlas("MarkV2Tex");
            }
            Futile.atlasManager.LoadAtlasFromTexture("MarkV2Tex", v2, false);

            Texture2D v3 = new(5, 5)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Repeat
            };
            for (int x = 0; x < 5; x++)
            {
                for (int y = 0; y < 5; y++)
                {
                    if (x == 2 || y == 2 || (x % 4 == 0 && y % 4 == 0))
                        v3.SetPixel(x, y, Color.white);
                    else
                        v3.SetPixel(x, y, new());
                }
            }

            v3.Apply(true);
            if (Futile.atlasManager.DoesContainAtlas("MarkV3Tex"))
            {
                Futile.atlasManager.UnloadAtlas("MarkV3Tex");
            }
            Futile.atlasManager.LoadAtlasFromTexture("MarkV3Tex", v3, false);
        }

        public Mark(MarkAbstract abstractPhysicalObject) : base(abstractPhysicalObject)
        {
            bodyChunkConnections = [];
            bodyChunks = [new(this, 0, new(), 3f, 0.2f)];
            airFriction = 0.999f;
            gravity = 0.9f;
            bounce = 0.2f;
            surfaceFriction = 0.7f;
            collisionLayer = 1;
            waterFriction = 0.95f;
            buoyancy = 1.1f;
        }

        public override void Update(bool eu)
        {
            base.Update(eu);
            lastRotation = rotation;
            if (grabbedBy.Count > 0)
            {
                rotation = Custom.PerpendicularVector(Custom.DirVec(firstChunk.pos, grabbedBy[0].grabber.mainBodyChunk.pos));
                rotation.y = Mathf.Abs(rotation.y);
            }
            if (firstChunk.ContactPoint.y < 0)
            {
                rotation = (rotation - Custom.PerpendicularVector(rotation) * (0.1f * firstChunk.vel.x)).normalized;
                firstChunk.vel.x *= 0.8f;
            }
        }

        public override void PlaceInRoom(Room placeRoom)
        {
            base.PlaceInRoom(placeRoom);
            rotation = Custom.RNV();
            lastRotation = rotation;
        }

        public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            newContatiner ??= rCam.ReturnFContainer("Items");
            for (int i = 0; i < sLeaser.sprites.Length; i++)
            {
                sLeaser.sprites[i].RemoveFromContainer();
                newContatiner.AddChild(sLeaser.sprites[i]);
            }
        }

        public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            sLeaser.sprites[0].color = palette.blackColor;
            sLeaser.sprites[1].color = MType == MarkType.V3 ? Custom.HSL2RGB(0f, 0.75f, 0.6f) : Color.cyan;
        }

        public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            Vector2 pos = Vector2.Lerp(firstChunk.lastPos, firstChunk.pos, timeStacker);
            Vector2 spritePos = pos - camPos;
            Vector2 r = Vector3.Slerp(lastRotation, rotation, timeStacker);
            for (int i = 0; i < sLeaser.sprites.Length; i++)
            {
                sLeaser.sprites[i].x = spritePos.x;
                sLeaser.sprites[i].y = spritePos.y;
                sLeaser.sprites[i].rotation = Custom.VecToDeg(r);
            }
            if (slatedForDeletetion || room != rCam.room)
            {
                sLeaser.RemoveAllSpritesFromContainer();
            }
        }

        public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites =
            [
                new("pixel")
                {
                    scale = 5
                },
                new(MType != MarkType.Normal ? $"Mark{MType}Tex" : "pixel")
            ];
            AddToContainer(sLeaser, rCam, null);
        }

        public class MarkAbstract : AbstractPhysicalObject
        {
            public MarkType markType = MarkType.Normal;

            public MarkAbstract(World world, WorldCoordinate pos, EntityID ID) : base(world, CreatureTemplateType.Mark, null, pos, ID)
            {
            }

            public override void Realize()
            {
                if (realizedObject != null)
                {
                    return;
                }
                realizedObject = new Mark(this);
                for (int i = 0; i < stuckObjects.Count; i++)
                {
                    if (stuckObjects[i].A.realizedObject == null && stuckObjects[i].A != this)
                    {
                        stuckObjects[i].A.Realize();
                    }
                    if (stuckObjects[i].B.realizedObject == null && stuckObjects[i].B != this)
                    {
                        stuckObjects[i].B.Realize();
                    }
                }
            }

            public override string ToString()
            {
                return this.SaveToString(markType.ToString());
            }
        }

        public enum MarkType
        {
            Normal,
            V2,
            V3
        }
    }
}
