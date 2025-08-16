using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using RWCustom;
using MoreSlugcats;
using Noise;

namespace VoidTemplate.Objects.SingularityRock
{
    public class MiniEnergyCell : Rock
    {
        public MiniEnergyCellAbstract abstractCell;

        public bool shouldExplode;

        public bool charged
        {
            get
            {
                return abstractCell.charged;
            }
            set
            {
                abstractCell.charged = value;
            }
        }

        public MiniEnergyCell(MiniEnergyCellAbstract abstractCell) : base(abstractCell, abstractCell.world)
        {
            this.abstractCell = abstractCell;
            firstChunk.rad = 5.5f;
            firstChunk.mass = 0.2f;
            collisionLayer = 1;
            firstChunk.loudness = 4;
        }

        public override void TerrainImpact(int chunk, IntVector2 direction, float speed, bool firstContact)
        {
            base.TerrainImpact(chunk, direction, speed, firstContact);
            if (firstContact && charged && mode == Mode.Thrown && Random.value > 0.3f)
            {
                shouldExplode = true;
            }
        }

        public override bool HitSomething(SharedPhysics.CollisionResult result, bool eu)
        {
            bool hit = base.HitSomething(result, eu);
            if (hit && Random.value > 0.3f)
            {
                shouldExplode = true;
            }
            return hit;
        }

        public override void Update(bool eu)
        {
            base.Update(eu);
            if (mode == Mode.Free && collisionLayer != 1)
            {
                ChangeCollisionLayer(1);
            }
            else if (mode != Mode.Free && collisionLayer != 2)
            {
                ChangeCollisionLayer(0);
            }
            if (shouldExplode)
            {
                ExplodeAndBroke();
            }
        }

        public void CreateBombAndExpode()
        {
            
            AbstractPhysicalObject singulartiyBomb = new(abstractCell.world, DLCSharedEnums.AbstractObjectType.SingularityBomb, null, abstractCell.pos, room.game.GetNewID());
            room.abstractRoom.AddEntity(singulartiyBomb);
            singulartiyBomb.RealizeInRoom();
            (singulartiyBomb.realizedObject as SingularityBomb).activateSingularity = true;
            Destroy();
        }

        public void ExplodeAndBroke()
        {
            if (slatedForDeletetion)
            {
                return;
            }
            Color explodeColor = new(0.2f, 0.2f, 1f);
            Vector2 pos = Vector2.Lerp(firstChunk.pos, firstChunk.lastPos, 0.35f);
            room.AddObject(new SootMark(room, pos, 80f, true));

            room.AddObject(new Explosion(room, this, pos, 7, 250f, 6.2f, 2f, 280f, 0.25f, thrownBy, 0.7f, 160f, 1f));

            room.AddObject(new Explosion.ExplosionLight(pos, 280f, 1f, 7, explodeColor));
            room.AddObject(new Explosion.ExplosionLight(pos, 230f, 1f, 3, new Color(1f, 1f, 1f)));
            room.AddObject(new ExplosionSpikes(room, pos, 14, 30f, 9f, 7f, 170f, explodeColor));
            room.AddObject(new ShockWave(pos, 330f, 0.045f, 5, false));
            for (int i = 0; i < 25; i++)
            {
                Vector2 a = Custom.RNV();
                if (room.GetTile(pos + a * 20f).Solid)
                {
                    if (!room.GetTile(pos - a * 20f).Solid)
                    {
                        a *= -1f;
                    }
                    else
                    {
                        a = Custom.RNV();
                    }
                }
                for (int j = 0; j < 3; j++)
                {
                    room.AddObject(new Spark(pos + a * Mathf.Lerp(30f, 60f, Random.value), a * Mathf.Lerp(7f, 38f, Random.value) + Custom.RNV() * 20f * Random.value, Color.Lerp(explodeColor, new Color(1f, 1f, 1f), Random.value), null, 11, 28));
                }
            }
            room.PlaySound(SoundID.Bomb_Explode, pos, abstractPhysicalObject);
            room.InGameNoise(new InGameNoise(pos, 9000f, this, 1f));
            room.PlaySound(SoundID.Zapper_Zap, pos, abstractPhysicalObject);
            room.PlaySound(SoundID.Spear_Fragment_Bounce, pos, 3, 1, abstractPhysicalObject);

            AbstractPhysicalObject singularityBomb = new(abstractCell.world, DLCSharedEnums.AbstractObjectType.SingularityBomb, null, abstractCell.pos, abstractCell.ID);
            room.abstractRoom.AddEntity(singularityBomb);
            singularityBomb.RealizeInRoom();
            Destroy();
        }

        public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            newContatiner ??= rCam.ReturnFContainer("Items");
            for (int i = 0; i < sLeaser.sprites.Length; i++)
            {
                sLeaser.sprites[i].RemoveFromContainer();
                newContatiner.AddChild(sLeaser.sprites[i]);
            }
        }

        public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            color = palette.blackColor;
        }

        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites = new FSprite[5];

            float col = charged ? 0.6638889f : 0.003333333f;

            sLeaser.sprites[0] = new TriangleMesh("Futile_White", [new TriangleMesh.Triangle(0, 1, 2)], true, false);
            sLeaser.sprites[1] = new FSprite("Circle20", true);
            sLeaser.sprites[1].color = Custom.HSL2RGB(col, 0.5f, 0.1f);
            sLeaser.sprites[1].scale = 0.7f;
            sLeaser.sprites[2] = new FSprite("Circle20", true);
            sLeaser.sprites[2].color = Custom.HSL2RGB(col, 1f, 0.35f);
            sLeaser.sprites[2].scale = 0.3f;
            sLeaser.sprites[3] = new FSprite("Circle20", true);
            sLeaser.sprites[3].color = Custom.HSL2RGB(col, 0.5f, 0.1f);
            sLeaser.sprites[3].scale = 0.3f;
            sLeaser.sprites[4] = new FSprite("Circle20", true);
            sLeaser.sprites[4].scale = 0.15f;
            AddToContainer(sLeaser, rCam, null);
        }

        public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            Vector2 spritePos = Vector2.Lerp(firstChunk.lastPos, firstChunk.pos, timeStacker);
            float spriteRotation = Custom.VecToDeg(Vector3.Slerp(lastRotation, rotation, timeStacker));
            if (mode == Mode.Thrown)
            {
                sLeaser.sprites[0].isVisible = true;
                Vector2 vector2 = Vector2.Lerp(tailPos, firstChunk.lastPos, timeStacker);
                Vector2 a = Custom.PerpendicularVector((spritePos - vector2).normalized);
                (sLeaser.sprites[0] as TriangleMesh).MoveVertice(0, spritePos + a * 2f - camPos);
                (sLeaser.sprites[0] as TriangleMesh).MoveVertice(1, spritePos - a * 2f - camPos);
                (sLeaser.sprites[0] as TriangleMesh).MoveVertice(2, vector2 - camPos);
                float num = Random.Range(0f, 0.7f);
                Color color = Color.Lerp(this.color, new Color(num, num, Random.Range(0.4f, 1f)), 0.4f);
                (sLeaser.sprites[0] as TriangleMesh).verticeColors[0] = color;
                (sLeaser.sprites[0] as TriangleMesh).verticeColors[1] = color;
                (sLeaser.sprites[0] as TriangleMesh).verticeColors[2] = color;
            }
            else
            {
                sLeaser.sprites[0].isVisible = false;
            }

            if (slatedForDeletetion || room != rCam.room)
            {
                sLeaser.RemoveAllSpritesFromContainer();
            }

            float num2 = 1f;
            num2 = Mathf.Lerp(0.2f, 1f, Mathf.Abs(num2)) * Mathf.Sign(num2);
            sLeaser.sprites[1].x = spritePos.x - camPos.x;
            sLeaser.sprites[1].y = spritePos.y - camPos.y;
            sLeaser.sprites[1].rotation = spriteRotation;
            sLeaser.sprites[1].scaleX = 0.7f * num2;
            sLeaser.sprites[2].x = spritePos.x - camPos.x - 0.75f - 1.5f * Mathf.Abs(num2);
            sLeaser.sprites[2].y = spritePos.y - camPos.y + 0.75f + 1.5f * Mathf.Abs(num2);
            sLeaser.sprites[2].rotation = spriteRotation;
            sLeaser.sprites[2].scaleX = 0.3f * num2;
            sLeaser.sprites[3].x = spritePos.x - camPos.x - 0.85f;
            sLeaser.sprites[3].y = spritePos.y - camPos.y + 0.85f;
            sLeaser.sprites[3].rotation = spriteRotation;
            sLeaser.sprites[3].scaleX = 0.3f * num2;
            sLeaser.sprites[4].x = spritePos.x - camPos.x - 0.75f;
            sLeaser.sprites[4].y = spritePos.y - camPos.y + 0.75f;
            sLeaser.sprites[4].rotation = spriteRotation;
            sLeaser.sprites[4].scaleX = 0.15f * num2;
            Color color2 = Custom.HSL2RGB(charged ? Random.Range(0.55f, 0.7f) : Random.Range(0, 0.15f), Random.Range(0.8f, 1f), Random.Range(0.3f, 0.6f));
            sLeaser.sprites[4].color = color2;

        }
    }
}
