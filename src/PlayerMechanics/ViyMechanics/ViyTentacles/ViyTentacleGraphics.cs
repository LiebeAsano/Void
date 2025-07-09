using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using RWCustom;
using VoidTemplate.Useful;

namespace VoidTemplate.PlayerMechanics.ViyMechanics.ViyTentacles
{
    public class ViyTentacleGraphics : RopeGraphic
    {
        public int spriteIndex;

        public int sprites;

        public ViyTentacle tentacle;

        public Player player
        {
            get
            {
                return tentacle.player;
            }
        }

        public Color ViyBodyColor
        {
            get
            {
                return new(0, 0, 0.005f);
            }
        }

        public ViyTentacleGraphics(ViyTentacle tentacle, int firstSprite) : base((int)(tentacle.idealLength / 10f))
        {
            this.tentacle = tentacle;
            this.spriteIndex = firstSprite;
            sprites = 1;
        }

        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites[spriteIndex] = TriangleMesh.MakeLongMeshAtlased(segments.Length, false, true);
        }

        public override void DrawSprite(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            sLeaser.sprites[spriteIndex].isVisible = tentacle.rotControl.rotMode;
            if (tentacle.rotControl.rotMode)
            {
                var triangleMesh = sLeaser.sprites[spriteIndex] as TriangleMesh;
                Vector2 vector = Vector2.Lerp(segments[0].lastPos, segments[0].pos, timeStacker);
                vector += Custom.DirVec(Vector2.Lerp(segments[1].lastPos, segments[1].pos, timeStacker), vector) * 1f;

                float baseWidth = 3.4f;
                float midWidth = baseWidth * 0.5f;
                float tipWidth = 0.5f;

                for (int i = 0; i < segments.Length; i++)
                {
                    Vector2 vector2 = Vector2.Lerp(segments[i].lastPos, segments[i].pos, timeStacker);
                    Vector2 normalized = (vector - vector2).normalized;
                    Vector2 a = Custom.PerpendicularVector(normalized);

                    float progress = (float)i / (segments.Length - 1);

                    float currentWidth;
                    if (progress < 0.85f)
                    {
                        currentWidth = baseWidth - (baseWidth - midWidth) * (progress / 0.85f);
                    }
                    else
                    {
                        float coneProgress = (progress - 0.85f) / 0.15f;
                        currentWidth = midWidth - (midWidth - tipWidth) * coneProgress;
                    }

                    triangleMesh.MoveVertice(i * 4, vector - a * currentWidth - camPos);
                    triangleMesh.MoveVertice(i * 4 + 1, vector + a * currentWidth - camPos);
                    triangleMesh.MoveVertice(i * 4 + 2, vector2 - a * currentWidth - camPos);
                    triangleMesh.MoveVertice(i * 4 + 3, vector2 + a * currentWidth - camPos);
                    vector = vector2;
                }
                Color body = ViyBodyColor;
                Color eyes = sLeaser.sprites[9].color;
                int num = 0;
                for (int i = 0; i < triangleMesh.vertices.Length; i++)
                {
                    if (i % 2 == 0)
                    {
                        num++;
                    }
                    triangleMesh.verticeColors[i] = Color.Lerp(body, eyes, Mathf.InverseLerp(0.88f, 0.95f, (float)num / ((triangleMesh.verticeColors.Length / 2) - 1)));
                }
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

        public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, FContainer newContainer)
        {
            FSprite sprite = sLeaser.sprites[spriteIndex];
            sprite.RemoveFromContainer();
            newContainer.AddChild(sprite);
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
