using RWCustom;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace VoidTemplate.PlayerMechanics.ViyMechanics.ViyTentacles
{
    public class ViyTentacle : Tentacle
    {
        private const float Tile = 20f;

        public ViyRotModule rotControl;

        public Vector2 preliminaryGrabDest;
        public Vector2 idealGrabPos;
        public Vector2 tentacleDir;

        public IntVector2 secondaryGrabPos;

        public IntVector2[] _cachedRays1 = new IntVector2[400];

        public new List<IntVector2> scratchPath;

        public int[] chunksStickSounds;

        public int secondaryGrabBackTrackCounter;
        public int foundNoGrabPos;

        public float chunksGripping;

        public bool atGrabDest;
        public bool neededForLocomotion;

        public bool lastBackTrack;

        public int tentacleIndex = -1;

        private bool _forceRetarget;

        public Player Player => owner as Player;

        public ViyTentacle(Player player, ViyRotModule rotControl, BodyChunk connectedChunk, float length, Vector2 tentacleDir)
            : base(player, connectedChunk, length)
        {
            this.rotControl = rotControl;
            this.tentacleDir = tentacleDir;

            tProps = new TentacleProps(false, true, false, 0.5f, 0f, 0f, 0f, 0f, 3.2f, 10f, 0.25f, 5f, 15, 60, 12, 20);

            tChunks = new TentacleChunk[(int)(length / 40f)];
            for (int j = 0; j < tChunks.Length; j++)
                tChunks[j] = new TentacleChunk(this, j, (j + 1f) / tChunks.Length, 3f);

            chunksStickSounds = new int[tChunks.Length];

            preliminaryGrabDest = connectedChunk.pos;
            secondaryGrabPos = new IntVector2((int)(connectedChunk.pos.x / Tile), (int)(connectedChunk.pos.y / Tile));

            debugViz = false;
        }

        public void RequestRetarget()
        {
            _forceRetarget = true;
            foundNoGrabPos = Math.Max(foundNoGrabPos, 60);
        }

        public override IntVector2 GravityDirection()
        {
            if (UnityEngine.Random.value >= 0.5f)
                return new IntVector2(0, -1);

            return new IntVector2(Tip.pos.x < connectedChunk.pos.x ? -1 : 1, -1);
        }

        private bool IsBeamTile(IntVector2 t)
        {
            var tile = room.GetTile(t);
            return tile.verticalBeam || tile.horizontalBeam;
        }

        private bool HasSolidNeighbor4(IntVector2 t)
        {
            for (int i = 0; i < 4; i++)
            {
                if (room.GetTile(t + Custom.fourDirections[i]).IsSolid())
                    return true;
            }
            return false;
        }

        private bool IsValidGrabTile(IntVector2 t)
        {
            if (room.GetTile(t).IsSolid()) return false;
            return IsBeamTile(t) || HasSolidNeighbor4(t);
        }

        private Vector2 SnapToBeam(Vector2 pos)
        {
            IntVector2 t = room.GetTilePosition(pos);
            var tile = room.GetTile(t);
            Vector2 mid = room.MiddleOfTile(t);

            if (tile.verticalBeam) pos.x = mid.x;
            if (tile.horizontalBeam) pos.y = mid.y;

            return pos;
        }

        public override void Update()
        {
            base.Update();

            limp = !Player.Consious;

            for (int i = 0; i < tChunks.Length; i++)
            {
                tChunks[i].vel *= 0.9f;
                if (limp) tChunks[i].vel.y -= 0.5f;
            }

            if (limp)
            {
                for (int i = 0; i < tChunks.Length; i++)
                    tChunks[i].vel.y -= 0.7f;
                return;
            }

            atGrabDest = false;

            if (backtrackFrom > -1)
            {
                secondaryGrabBackTrackCounter++;
                if (!lastBackTrack) secondaryGrabBackTrackCounter += 20;
            }

            lastBackTrack = backtrackFrom > -1;
            chunksGripping = 0f;

            Climb(ref scratchPath);

            for (int m = 0; m < tChunks.Length; m++)
            {
                if (atGrabDest)
                {
                    float num4 = (float)m / (tChunks.Length - 1);
                    if (num4 < 0.2f)
                        tChunks[m].vel += tentacleDir * Mathf.InverseLerp(0.2f, 0f, num4) * 2.5f;
                }

                for (int n = m + 1; n < tChunks.Length; n++)
                    PushChunksApart(m, n);
            }

            _forceRetarget = false;
        }

        public void Climb(ref List<IntVector2> path)
        {
            float dirBlend = rotControl.moving ? 0.85f : 0.35f;

            Vector2 moveDir = rotControl.moveDirection;
            if (moveDir == Vector2.zero) moveDir = tentacleDir;

            Vector2 baseDir = ((Vector2)Vector3.Slerp(tentacleDir, moveDir, dirBlend)).normalized;
            idealGrabPos = FloatBase + baseDir * idealLength * 0.7f;

            float explore = Mathf.InverseLerp(0f, 140f, foundNoGrabPos);

            Vector2 perp = new(-baseDir.y, baseDir.x);
            Vector2[] rayDirs =
            [
                baseDir,
                (baseDir + perp * 0.35f).normalized,
                (baseDir - perp * 0.35f).normalized
            ];

            bool foundAny = false;

            for (int r = 0; r < rayDirs.Length; r++)
            {
                Vector2 rnd = Custom.RNV();
                Vector2 dir = ((Vector2)Vector3.Slerp(rayDirs[r], rnd, explore * 0.65f)).normalized;

                float reach = idealLength * Mathf.Lerp(0.75f, 1.25f, explore);
                Vector2 target = FloatBase + dir * reach;

                int count;
                for (count = SharedPhysics.RayTracedTilesArray(FloatBase, target, _cachedRays1);
                     count >= _cachedRays1.Length;
                     count = SharedPhysics.RayTracedTilesArray(FloatBase, target, _cachedRays1))
                {
                    Array.Resize(ref _cachedRays1, _cachedRays1.Length + 100);
                }

                for (int j = 0; j < count - 1; j++)
                {
                    IntVector2 tile = _cachedRays1[j];
                    IntVector2 next = _cachedRays1[j + 1];

                    if (room.GetTile(next).IsSolid())
                    {
                        Vector2 p = Custom.RestrictInRect(target, room.TileRect(tile).Shrink(1));
                        ConsiderGrabPos(p, idealGrabPos);
                        foundAny = true;
                        break;
                    }

                    var t = room.GetTile(tile);
                    if (t.horizontalBeam || t.verticalBeam)
                    {
                        Vector2 p = room.MiddleOfTile(tile);
                        ConsiderGrabPos(p, idealGrabPos);
                        foundAny = true;
                    }
                }
            }

            if (foundAny) foundNoGrabPos = 0;
            else foundNoGrabPos++;

            bool hasSecondary = secondaryGrabBackTrackCounter < 200 && SecondaryGrabPosScore(secondaryGrabPos) > 0f;

            for (int k = 0; k < tChunks.Length; k++)
            {
                if (backtrackFrom != -1 && backtrackFrom <= k) continue;

                StickToTerrain(tChunks[k]);

                if (grabDest != null)
                {
                    if (!atGrabDest && Custom.DistLess(tChunks[k].pos, floatGrabDest.Value, Tile))
                        atGrabDest = true;

                    if (tChunks[k].currentSegment <= grabPath.Count || !hasSecondary)
                    {
                        tChunks[k].vel += Vector2.ClampMagnitude(floatGrabDest.Value - tChunks[k].pos, Tile) / Tile * 1.35f;
                    }
                    else if (k > 1 && segments.Count > grabPath.Count && hasSecondary)
                    {
                        float num = Mathf.InverseLerp(grabPath.Count, segments.Count, tChunks[k].currentSegment);
                        Vector2 a = Custom.DirVec(tChunks[k - 2].pos, tChunks[k].pos) * (1f - num) * 0.6f;
                        a += Custom.DirVec(tChunks[k].pos, room.MiddleOfTile(grabDest.Value)) * Mathf.Pow(1f - num, 4f) * 2f;
                        a += Custom.DirVec(tChunks[k].pos, room.MiddleOfTile(secondaryGrabPos)) * Mathf.Pow(num, 4f) * 2f;
                        a += Custom.DirVec(tChunks[k].pos, FloatBase) * Mathf.Sin(num * Mathf.PI) * 0.3f;

                        tChunks[k].vel += a.normalized * 1.35f;

                        if (k == tChunks.Length - 1)
                            tChunks[k].vel += Vector2.ClampMagnitude(room.MiddleOfTile(secondaryGrabPos) - tChunks[k].pos, Tile) / Tile * 4.2f;
                    }
                }
            }

            if (grabDest != null)
            {
                for (int tries = 0; tries < 3; tries++)
                    ConsiderSecondaryGrabPos(grabDest.Value + new IntVector2(UnityEngine.Random.Range(-6, 7), UnityEngine.Random.Range(-6, 7)));
            }

            if (_forceRetarget || grabDest == null || !atGrabDest || rotControl.IsTileClaimedByOther(tentacleIndex, grabDest.Value))
                UpdateClimbGrabPos(ref path);
        }

        public float ReleaseScore()
        {
            float num = float.MaxValue;
            for (int i = tChunks.Length / 2; i < tChunks.Length; i++)
            {
                float d = Vector2.Distance(tChunks[i].pos, idealGrabPos);
                if (d < num) num = d;
            }
            return num;
        }

        public float GrabPosScore(Vector2 testPos, Vector2 idealGrabPos)
        {
            testPos = SnapToBeam(testPos);

            IntVector2 tile = room.GetTilePosition(testPos);

            if (tentacleIndex >= 0 && rotControl.IsTileClaimedByOther(tentacleIndex, tile))
                return -1000f;

            if (!IsValidGrabTile(tile))
                return -800f;

            float dist = Vector2.Distance(testPos, idealGrabPos);
            if (dist < 0.01f) dist = 0.01f;

            float score = 100f / dist;

            if (IsBeamTile(tile))
                score *= 3.0f;

            if (grabDest != null && tile == grabDest.Value)
                score *= 1.35f;

            if (HasSolidNeighbor4(tile))
                score *= 1.35f;

            if (rotControl != null && !rotControl.allowUp)
            {
                float dy = testPos.y - FloatBase.y;
                if (dy > 0f)
                {
                    float t = Mathf.InverseLerp(Tile * 0.5f, Tile * 7f, dy);
                    score *= Mathf.Lerp(1f, 0.10f, t);
                }
            }

            return score;
        }

        public void ConsiderGrabPos(Vector2 testPos, Vector2 idealGrabPos)
        {
            testPos = SnapToBeam(testPos);

            if (!IsValidGrabTile(room.GetTilePosition(testPos)))
                return;

            float test = GrabPosScore(testPos, idealGrabPos);
            float cur = GrabPosScore(preliminaryGrabDest, idealGrabPos);

            if (test > cur * 1.05f)
                preliminaryGrabDest = testPos;
        }

        public void UpdateClimbGrabPos(ref List<IntVector2> path)
        {
            Vector2 bestPos = SnapToBeam(preliminaryGrabDest);
            float bestScore = GrabPosScore(bestPos, idealGrabPos);

            if (grabDest != null && !rotControl.IsTileClaimedByOther(tentacleIndex, grabDest.Value))
            {
                Vector2 curPos = SnapToBeam(room.MiddleOfTile(grabDest.Value));
                float curScore = GrabPosScore(curPos, idealGrabPos);
                if (curScore > bestScore)
                {
                    bestPos = curPos;
                    bestScore = curScore;
                }
            }

            for (int i = 0; i < 10; i++)
            {
                Vector2 cand = preliminaryGrabDest + Custom.RNV() * UnityEngine.Random.value * Tile * 2.5f;
                cand = SnapToBeam(cand);

                IntVector2 ct = room.GetTilePosition(cand);

                if (room.GetTile(cand).IsSolid()) continue;
                if (!IsValidGrabTile(ct)) continue;

                float s = GrabPosScore(cand, idealGrabPos);
                if (s > bestScore)
                {
                    bestScore = s;
                    bestPos = cand;
                }
            }

            IntVector2 bestTile = room.GetTilePosition(bestPos);

            if (tentacleIndex >= 0 && !rotControl.TryClaimTile(tentacleIndex, bestTile))
            {
                bool claimed = false;
                IntVector2 center = bestTile;

                for (int r = 1; r <= 6 && !claimed; r++)
                {
                    for (int dx = -r; dx <= r && !claimed; dx++)
                    {
                        for (int dy = -r; dy <= r && !claimed; dy++)
                        {
                            if (Math.Abs(dx) != r && Math.Abs(dy) != r) continue;

                            IntVector2 t = center + new IntVector2(dx, dy);
                            if (room.GetTile(t).IsSolid()) continue;
                            if (!IsValidGrabTile(t)) continue;

                            if (rotControl.TryClaimTile(tentacleIndex, t))
                            {
                                bestPos = SnapToBeam(room.MiddleOfTile(t));
                                claimed = true;
                            }
                        }
                    }
                }

                if (!claimed)
                    return;
            }

            bestPos = SnapToBeam(bestPos);
            MoveGrabDest(bestPos, ref path);
        }

        public float SecondaryGrabPosScore(IntVector2 testPos)
        {
            if (grabDest == null) return 0f;
            if (testPos.FloatDist(BasePos) < 7f) return 0f;

            if (!IsValidGrabTile(testPos)) return 0f;

            float spare = idealLength - grabPath.Count * Tile;
            if (Vector2.Distance(room.MiddleOfTile(testPos), floatGrabDest.Value) > spare) return 0f;

            if (!SharedPhysics.RayTraceTilesForTerrain(room, grabDest.Value, testPos)) return 0f;

            float neighbors = 0f;
            for (int i = 0; i < 8; i++)
                if (room.GetTile(testPos + Custom.eightDirections[i]).Solid) neighbors += 1f;

            if (room.GetTile(testPos).horizontalBeam || room.GetTile(testPos).verticalBeam) neighbors += 1f;
            if (neighbors > 0f && testPos == secondaryGrabPos) neighbors += 1f;
            if (neighbors == 0f) return 0f;

            neighbors += testPos.FloatDist(BasePos) / 10f;

            return neighbors / (1f
                + Mathf.Abs(spare * 0.75f - Vector2.Distance(room.MiddleOfTile(testPos), floatGrabDest.Value))
                + Vector2.Distance(room.MiddleOfTile(testPos), room.MiddleOfTile(segments[segments.Count - 1])));
        }

        public void ConsiderSecondaryGrabPos(IntVector2 testPos)
        {
            if (room.GetTile(testPos).Solid) return;
            if (!IsValidGrabTile(testPos)) return;

            float test = SecondaryGrabPosScore(testPos);
            float cur = SecondaryGrabPosScore(secondaryGrabPos);

            if (test > cur)
            {
                secondaryGrabBackTrackCounter = 0;
                secondaryGrabPos = testPos;
            }
        }

        public void StickToTerrain(TentacleChunk chunk)
        {
            if (floatGrabDest != null && !Custom.DistLess(chunk.pos, floatGrabDest.Value, 200f)) return;

            int num = (int)Mathf.Sign(chunk.pos.x - room.MiddleOfTile(chunk.pos).x);
            Vector2 vector = new(0f, 0f);
            IntVector2 tilePosition = room.GetTilePosition(chunk.pos);

            int i = 0;
            while (i < 8)
            {
                if (room.GetTile(tilePosition + new IntVector2(Custom.eightDirectionsDiagonalsLast[i].x * num, Custom.eightDirectionsDiagonalsLast[i].y)).Solid)
                {
                    if (Custom.eightDirectionsDiagonalsLast[i].x != 0)
                        vector.x = room.MiddleOfTile(chunk.pos).x + Custom.eightDirectionsDiagonalsLast[i].x * num * (Tile - chunk.rad);

                    if (Custom.eightDirectionsDiagonalsLast[i].y != 0)
                    {
                        vector.y = room.MiddleOfTile(chunk.pos).y + Custom.eightDirectionsDiagonalsLast[i].y * (Tile - chunk.rad);
                        break;
                    }
                    break;
                }
                i++;
            }

            if (vector.x == 0f && room.GetTile(chunk.pos).verticalBeam) vector.x = room.MiddleOfTile(chunk.pos).x;
            if (vector.y == 0f && room.GetTile(chunk.pos).horizontalBeam) vector.y = room.MiddleOfTile(chunk.pos).y;

            if (chunk.tentacleIndex > tChunks.Length / 2)
            {
                if (vector.x != 0f || vector.y != 0f)
                {
                    if (chunksStickSounds[chunk.tentacleIndex] > 10)
                        owner.room.PlaySound(SoundID.Daddy_And_Bro_Tentacle_Grab_Terrain, chunk.pos, Mathf.InverseLerp(tChunks.Length / 2, tChunks.Length - 1, chunk.tentacleIndex), 1f, owner.abstractPhysicalObject);

                    if (chunksStickSounds[chunk.tentacleIndex] > 0) chunksStickSounds[chunk.tentacleIndex] = 0;
                    else chunksStickSounds[chunk.tentacleIndex]--;
                }
                else
                {
                    if (chunksStickSounds[chunk.tentacleIndex] < -10)
                        owner.room.PlaySound(SoundID.Daddy_And_Bro_Tentacle_Release_Terrain, chunk.pos, Mathf.InverseLerp(tChunks.Length / 2, tChunks.Length - 1, chunk.tentacleIndex), 1f, owner.abstractPhysicalObject);

                    if (chunksStickSounds[chunk.tentacleIndex] < 0) chunksStickSounds[chunk.tentacleIndex] = 0;
                    else chunksStickSounds[chunk.tentacleIndex]++;
                }
            }

            if (vector.x != 0f)
            {
                chunk.vel.x += (vector.x - chunk.pos.x) * 0.1f;
                chunk.vel.y *= 0.9f;
            }
            if (vector.y != 0f)
            {
                chunk.vel.y += (vector.y - chunk.pos.y) * 0.1f;
                chunk.vel.x *= 0.9f;
            }

            if (vector.x != 0f || vector.y != 0f)
                chunksGripping += 1f / tChunks.Length;
        }
    }
}