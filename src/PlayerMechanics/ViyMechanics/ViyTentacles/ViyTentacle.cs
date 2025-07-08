using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions.Must;

namespace VoidTemplate.PlayerMechanics.ViyMechanics.ViyTentacles
{
    public class ViyTentacle : Tentacle
    {
        public ViyRotModule rotControl;

        public Vector2 preliminaryGrabDest;

        public Vector2 idealGrabPos;

        public Vector2 tentacleDir;

        public IntVector2 secondaryGrabPos;

        public IntVector2[] _cachedRays1 = new IntVector2[200];

        //public List<IntVector2> _cachedRays2 = new(15);

        public new List<IntVector2> scratchPath;

        public int[] chunksStickSounds;

        public int secondaryGrabBackTrackCounter;

        public int foundNoGrabPos;

        public float chunksGripping;

        public bool atGrabDest;

        public bool neededForLocomotion;
        
        public bool lastBackTrack;

        public Player player
        {
            get
            {
                return owner as Player;
            }
        }

        public ViyTentacle(Player player, ViyRotModule rotControl, BodyChunk connectedChunk, float length, Vector2 tentacleDir) : base(player, connectedChunk, length)
        {
            this.rotControl = rotControl;
            this.tentacleDir = tentacleDir;
            segments = [];
            for (int i = 0; i < (int)(idealLength / 20f); i++)
            {
                segments.Add(player.abstractCreature.pos.Tile);
            }
            tProps = new TentacleProps(false, true, false, 0.5f, 0f, 0f, 0f, 0f, 3.2f, 10f, 0.25f, 5f, 15, 60, 12, 20);
            tChunks = new TentacleChunk[(int)(length / 40f)];
            for (int j = 0; j < tChunks.Length; j++)
            {
                tChunks[j] = new TentacleChunk(this, j, (j + 1) / tChunks.Length, 3f);
            }
            chunksStickSounds = new int[tChunks.Length];
            debugViz = false;
        }

        public override IntVector2 GravityDirection()
        {
            if (UnityEngine.Random.value >= 0.5)
            {
                return new(0, -1);
            }
            return new(Tip.pos.x < connectedChunk.pos.x ? -1 : 1, -1);
        }

        public override void Update()
        {
            base.Update();
            limp = !player.Consious;
            for (int i = 0; i < tChunks.Length; i++)
            {
                tChunks[i].vel *= 0.9f;
                if (limp)
                {
                    tChunks[i].vel.y -= 0.5f;
                }
            }
            if (limp)
            {
                for (int i = 0; i < tChunks.Length; i++)
                {
                    tChunks[i].vel.y -= 0.7f;
                }
                return;
            }
            atGrabDest = false;
            if (backtrackFrom > -1)
            {
                secondaryGrabBackTrackCounter++;
                if (!lastBackTrack)
                {
                    secondaryGrabBackTrackCounter += 20;
                }
            }
            lastBackTrack = backtrackFrom > -1;
            chunksGripping = 0;
            
            Climb(ref scratchPath);
            for (int m = 0; m < tChunks.Length; m++)
            {
                for (int n = m + 1; n < tChunks.Length; n++)
                {
                    PushChunksApart(m, n);
                }
            }
        }

        public void Climb(ref List<IntVector2> path)
        {
            Vector3 vector = (Vector2)Vector3.Slerp(tentacleDir, rotControl.moveDirection, 0.5f);
            idealGrabPos = FloatBase + (Vector2)vector * idealLength * 0.7f;
            Vector2 actualGrabPos = FloatBase + (Vector2)Vector3.Slerp(vector, Custom.RNV(), Mathf.InverseLerp(20f, 200f, foundNoGrabPos))
                * idealLength * Custom.LerpMap(Mathf.Max(0, foundNoGrabPos), 20f, 200f, 0.7f, 1.2f);

            int i;
            for (i = SharedPhysics.RayTracedTilesArray(FloatBase, actualGrabPos, _cachedRays1); i >= _cachedRays1.Length; i = SharedPhysics.RayTracedTilesArray(FloatBase, actualGrabPos, _cachedRays1))
            {
                Custom.LogWarning(
                [
                    $"ViyRotTentcle Climb ray tracing limit exceeded, extending cache to {_cachedRays1.Length + 100} and trying again!"
                ]);
                Array.Resize(ref _cachedRays1, _cachedRays1.Length + 100);
            }

            bool flag = false;
            for (int j = 0; j < i - 1; j++)
            {
                if (room.GetTile(_cachedRays1[j + 1]).IsSolid())
                {
                    ConsiderGrabPos(Custom.RestrictInRect(actualGrabPos, room.TileRect(_cachedRays1[j]).Shrink(1f)), idealGrabPos);
                    flag = true;
                    break;
                }
                if (room.GetTile(_cachedRays1[j]).horizontalBeam || room.GetTile(_cachedRays1[j]).verticalBeam)
                {
                    ConsiderGrabPos(room.MiddleOfTile(_cachedRays1[j]), idealGrabPos);
                    flag = true;
                }
            }

            if (flag)
            {
                foundNoGrabPos = 0;
            }
            else
            {
                foundNoGrabPos++;
            }

            bool flag2 = secondaryGrabBackTrackCounter < 200 && SecondaryGrabPosScore(secondaryGrabPos) > 0f;
            for (int k = 0; k < tChunks.Length; k++)
            {
                if (backtrackFrom == -1 || backtrackFrom > k)
                {
                    StickToTerrain(tChunks[k]);
                    if (grabDest != null)
                    {
                        if (!atGrabDest && Custom.DistLess(tChunks[k].pos, floatGrabDest.Value, 20f))
                        {
                            atGrabDest = true;
                        }
                        if (tChunks[k].currentSegment <= grabPath.Count || !flag2)
                        {
                            tChunks[k].vel += Vector2.ClampMagnitude(floatGrabDest.Value - tChunks[k].pos, 20f) / 20f * 1.2f;
                        }
                        else if (k > 1 && segments.Count > grabPath.Count && flag2)
                        {
                            float num = Mathf.InverseLerp(grabPath.Count, segments.Count, tChunks[k].currentSegment);
                            Vector2 a = Custom.DirVec(tChunks[k - 2].pos, tChunks[k].pos) * (1f - num) * 0.6f;
                            a += Custom.DirVec(tChunks[k].pos, room.MiddleOfTile(grabDest.Value)) * Mathf.Pow(1f - num, 4f) * 2f;
                            a += Custom.DirVec(tChunks[k].pos, room.MiddleOfTile(secondaryGrabPos)) * Mathf.Pow(num, 4f) * 2f;
                            a += Custom.DirVec(tChunks[k].pos, FloatBase) * Mathf.Sin(num * 3.1415927f) * 0.3f;
                            tChunks[k].vel += a.normalized * 1.2f;
                            if (k == tChunks.Length - 1)
                            {
                                tChunks[k].vel += Vector2.ClampMagnitude(room.MiddleOfTile(secondaryGrabPos) - tChunks[k].pos, 20f) / 20f * 4.2f;
                            }
                        }
                    }
                }
            }

            if (grabDest != null)
            {
                ConsiderSecondaryGrabPos(grabDest.Value + new IntVector2(UnityEngine.Random.Range(-20, 21), UnityEngine.Random.Range(-20, 21)));
            }
            if (grabDest == null || !atGrabDest)
            {
                UpdateClimbGrabPos(ref path);
            }
        }

        public float ReleaseScore()
        {
            float num = float.MaxValue;
            for (int i = tChunks.Length / 2; i < tChunks.Length; i++)
            {
                if (Custom.DistLess(tChunks[i].pos, idealGrabPos, num))
                {
                    num = Vector2.Distance(tChunks[i].pos, idealGrabPos);
                }
            }
            return num;
        }
        public float ReleaseScoreForAngle()
        {
            return ReleaseScore() * (atGrabDest ? 1f : 1.2f) * Custom.LerpMap(Vector2.Dot(rotControl.moveDirection, Tip.pos - connectedChunk.pos), -1f, 1f, 2f, 1f);
        }

        public float GrabPosScore(Vector2 testPos, Vector2 idealGrabPos)
        {
            float num = 100f / Vector2.Distance(testPos, idealGrabPos);
            if (grabDest != null && room.GetTilePosition(testPos) == grabDest.Value)
            {
                num *= 1.5f;
            }
            for (int i = 0; i < 4; i++)
            {
                if (room.GetTile(testPos + Custom.fourDirections[i].ToVector2() * 20f).Solid)
                {
                    num *= 2f;
                    break;
                }
            }
            return num;
        }

        public void ConsiderGrabPos(Vector2 testPos, Vector2 idealGrabPos)
        {
            if (GrabPosScore(testPos, idealGrabPos) > GrabPosScore(preliminaryGrabDest, idealGrabPos))
            {
                preliminaryGrabDest = testPos;
            }
        }

        public void UpdateClimbGrabPos(ref List<IntVector2> path)
        {
            MoveGrabDest(preliminaryGrabDest, ref path);
        }

        public void ConsiderSecondaryGrabPos(IntVector2 testPos)
        {
            if (room.GetTile(testPos).Solid)
            {
                return;
            }
            if (SecondaryGrabPosScore(testPos) > SecondaryGrabPosScore(secondaryGrabPos))
            {
                secondaryGrabBackTrackCounter = 0;
                secondaryGrabPos = testPos;
            }
        }

        public float SecondaryGrabPosScore(IntVector2 testPos)
        {
            if (grabDest == null)
            {
                return 0f;
            }
            if (testPos.FloatDist(BasePos) < 7f)
            {
                return 0f;
            }
            float num = idealLength - grabPath.Count * 20f;
            if (Vector2.Distance(room.MiddleOfTile(testPos), floatGrabDest.Value) > num)
            {
                return 0f;
            }
            if (!SharedPhysics.RayTraceTilesForTerrain(room, grabDest.Value, testPos))
            {
                return 0f;
            }
            float num2 = 0f;
            for (int i = 0; i < 8; i++)
            {
                if (room.GetTile(testPos + Custom.eightDirections[i]).Solid)
                {
                    num2 += 1f;
                }
            }
            if (room.GetTile(testPos).horizontalBeam || room.GetTile(testPos).verticalBeam)
            {
                num2 += 1f;
            }
            if (num2 > 0f && testPos == secondaryGrabPos)
            {
                num2 += 1f;
            }
            if (num2 == 0f)
            {
                return 0f;
            }
            num2 += testPos.FloatDist(BasePos) / 10f;
            return num2 / (1f + Mathf.Abs(num * 0.75f - Vector2.Distance(room.MiddleOfTile(testPos), floatGrabDest.Value)) +
                Vector2.Distance(room.MiddleOfTile(testPos), room.MiddleOfTile(segments[segments.Count - 1])));
        }

        public void StickToTerrain(TentacleChunk chunk)
        {
            if (floatGrabDest != null && !Custom.DistLess(chunk.pos, floatGrabDest.Value, 200f))
            {
                return;
            }
            int num = (int)Mathf.Sign(chunk.pos.x - room.MiddleOfTile(chunk.pos).x);
            Vector2 vector = new Vector2(0f, 0f);
            IntVector2 tilePosition = room.GetTilePosition(chunk.pos);
            int i = 0;
            while (i < 8)
            {
                if (room.GetTile(tilePosition + new IntVector2(Custom.eightDirectionsDiagonalsLast[i].x * num, Custom.eightDirectionsDiagonalsLast[i].y)).Solid)
                {
                    if (Custom.eightDirectionsDiagonalsLast[i].x != 0)
                    {
                        vector.x = room.MiddleOfTile(chunk.pos).x + Custom.eightDirectionsDiagonalsLast[i].x * num * (20f - chunk.rad);
                    }
                    if (Custom.eightDirectionsDiagonalsLast[i].y != 0)
                    {
                        vector.y = room.MiddleOfTile(chunk.pos).y + Custom.eightDirectionsDiagonalsLast[i].y * (20f - chunk.rad);
                        break;
                    }
                    break;
                }
                else
                {
                    i++;
                }
            }
            if (vector.x == 0f && room.GetTile(chunk.pos).verticalBeam)
            {
                vector.x = room.MiddleOfTile(chunk.pos).x;
            }
            if (vector.y == 0f && room.GetTile(chunk.pos).horizontalBeam)
            {
                vector.y = room.MiddleOfTile(chunk.pos).y;
            }
            if (chunk.tentacleIndex > tChunks.Length / 2)
            {
                if (vector.x != 0f || vector.y != 0f)
                {
                    if (chunksStickSounds[chunk.tentacleIndex] > 10)
                    {
                        owner.room.PlaySound(SoundID.Daddy_And_Bro_Tentacle_Grab_Terrain, chunk.pos, Mathf.InverseLerp(tChunks.Length / 2, tChunks.Length - 1, chunk.tentacleIndex), 1f, owner.abstractPhysicalObject);
                    }
                    if (chunksStickSounds[chunk.tentacleIndex] > 0)
                    {
                        chunksStickSounds[chunk.tentacleIndex] = 0;
                    }
                    else
                    {
                        chunksStickSounds[chunk.tentacleIndex]--;
                    }
                }
                else
                {
                    if (chunksStickSounds[chunk.tentacleIndex] < -10)
                    {
                        owner.room.PlaySound(SoundID.Daddy_And_Bro_Tentacle_Release_Terrain, chunk.pos, Mathf.InverseLerp(tChunks.Length / 2, tChunks.Length - 1, chunk.tentacleIndex), 1f, owner.abstractPhysicalObject);
                    }
                    if (chunksStickSounds[chunk.tentacleIndex] < 0)
                    {
                        chunksStickSounds[chunk.tentacleIndex] = 0;
                    }
                    else
                    {
                        chunksStickSounds[chunk.tentacleIndex]++;
                    }
                }
            }
            if (vector.x != 0f)
            {
                chunk.vel.x = chunk.vel.x + (vector.x - chunk.pos.x) * 0.1f;
                chunk.vel.y = chunk.vel.y * 0.9f;
            }
            if (vector.y != 0f)
            {
                chunk.vel.y = chunk.vel.y + (vector.y - chunk.pos.y) * 0.1f;
                chunk.vel.x = chunk.vel.x * 0.9f;
            }
            if (vector.x != 0f || vector.y != 0f)
            {
                chunksGripping += 1f / tChunks.Length;
            }
        }
    }
}
