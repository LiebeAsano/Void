using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using RWCustom;

namespace VoidTemplate.PlayerMechanics.ViyMechanics.ViyTentacles
{
    public class ViyTentacleGraphics : RopeGraphic
    {
        public int firstSprite;

        public int sprites;

        public ViyTentacle tentacle;

        public Color ViyBodyColor
        {
            get
            {
                return new(0, 0, 0.005f);
            }
        }

        public Player player
        {
            get
            {
                return tentacle.player;
            }
        }

        public ViyTentacleGraphics(ViyTentacle tentacle, int firstSprite) : base((int)(tentacle.idealLength / 10f))
        {
            this.tentacle = tentacle;
            this.firstSprite = firstSprite;
            sprites = 1;
        }

        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites[firstSprite] = TriangleMesh.MakeLongMeshAtlased(segments.Length, false, true);
        }

        public override void DrawSprite(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            Vector2 vector = Vector2.Lerp(segments[0].lastPos, segments[0].pos, timeStacker);
            vector += Custom.DirVec(Vector2.Lerp(segments[1].lastPos, segments[1].pos, timeStacker), vector) * 1f;
            float d = 1.7f;
            for (int i = 0; i < segments.Length; i++)
            {
                Vector2 vector2 = Vector2.Lerp(segments[i].lastPos, segments[i].pos, timeStacker);
                Vector2 normalized = (vector - vector2).normalized;
                Vector2 a = Custom.PerpendicularVector(normalized);
                (sLeaser.sprites[firstSprite] as TriangleMesh).MoveVertice(i * 4, vector - a * d - camPos);
                (sLeaser.sprites[firstSprite] as TriangleMesh).MoveVertice(i * 4 + 1, vector + a * d - camPos);
                (sLeaser.sprites[firstSprite] as TriangleMesh).MoveVertice(i * 4 + 2, vector2 - a * d - camPos);
                (sLeaser.sprites[firstSprite] as TriangleMesh).MoveVertice(i * 4 + 3, vector2 + a * d - camPos);
                vector = vector2;
            }
        }

        public override void MoveSegment(int segment, Vector2 goalPos, Vector2 smoothedGoalPos)
        {
            segments[segment].vel *= 0f;
            if (tentacle.owner.room.GetTile(smoothedGoalPos).Solid && !tentacle.owner.room.GetTile(goalPos).Solid)
            {
                FloatRect floatRect = Custom.RectCollision(smoothedGoalPos, goalPos, tentacle.owner.room.TileRect(tentacle.owner.room.GetTilePosition(smoothedGoalPos)).Grow(3f));
                segments[segment].pos = new Vector2(floatRect.left, floatRect.bottom);
                return;
            }
            segments[segment].pos = smoothedGoalPos;
        }

        public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            var triangleMesh = sLeaser.sprites[firstSprite] as TriangleMesh;
            for (int i = 0; i < triangleMesh.vertices.Length; i++)
            {
                triangleMesh.verticeColors[i] = ViyBodyColor;
            }
            triangleMesh.color = ViyBodyColor;
        }

        public void MoveBehindFirstSprite(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            FSprite sprite = sLeaser.sprites[firstSprite];
            sprite.RemoveFromContainer();
            rCam.ReturnFContainer("Midground").AddChild(sprite);
            sprite.MoveBehindOtherNode(sLeaser.sprites[0]);
        }

        public override void Update()
        {
            int listCount = 0;
            AddToPositionsList(listCount++, tentacle.FloatBase);
            for (int i = 0; i < tentacle.tChunks.Length; i++)
            {
                for (int j = 1; j < tentacle.tChunks[i].rope.TotalPositions; j++)
                {
                    AddToPositionsList(listCount++, tentacle.tChunks[i].rope.GetPosition(j));
                }
            }
            AlignAndConnect(listCount);
        }

        public override void ConnectPhase(float totalRopeLength)
        {
        }
    }
}
