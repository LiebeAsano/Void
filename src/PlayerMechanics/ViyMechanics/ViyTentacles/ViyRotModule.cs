using RWCustom;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace VoidTemplate.PlayerMechanics.ViyMechanics.ViyTentacles
{
    public class ViyRotModule
    {
        public Player player;

        public ViyTentacle[] tentacles = new ViyTentacle[5];
        public ViyRotGraphics graphics;

        public Vector2 moveDirection;

        public int notFollowingPathToCurrentGoalCounter;
        public float unconditionalSupport;
        public float maxUpVelNoUp;
        public bool moving;

        public bool rotMode = false;
        public bool switchModeReset;
        public int rotModeTransformTime;

        public bool allowUp;
        public bool allowDown;

        private readonly Dictionary<IntVector2, int> _claimedTiles = [];
        private readonly IntVector2?[] _tentacleClaim = new IntVector2?[5];

        public Room Room => player.room;
        public Vector2 VecInput => new(player.input[0].x, player.input[0].y);

        public ViyRotModule(Player player)
        {
            this.player = player;

            for (int i = 0; i < 5; i++)
            {
                tentacles[i] = new ViyTentacle(
                    player,
                    this,
                    player.mainBodyChunk,
                    220f,
                    Custom.DegToVec(Mathf.Lerp(0f, 360f, i / 5f))
                )
                {
                    tentacleIndex = i
                };
            }

            moveDirection = Vector2.right;

            graphics = new ViyRotGraphics(this);
            NewRoom(player.room);
        }

        public void NewRoom(Room newRoom)
        {
            _claimedTiles.Clear();
            for (int i = 0; i < _tentacleClaim.Length; i++)
                _tentacleClaim[i] = null;

            for (int i = 0; i < 5; i++)
            {
                tentacles[i].NewRoom(newRoom);
                tentacles[i].Reset(player.mainBodyChunk.pos);
            }
        }

        internal bool IsTileClaimedByOther(int requesterIndex, IntVector2 tile)
            => _claimedTiles.TryGetValue(tile, out int owner) && owner != requesterIndex;

        internal bool TryClaimTile(int requesterIndex, IntVector2 tile)
        {
            if (_claimedTiles.TryGetValue(tile, out int owner))
                return owner == requesterIndex;

            ReleaseTile(requesterIndex);

            _claimedTiles[tile] = requesterIndex;
            _tentacleClaim[requesterIndex] = tile;
            return true;
        }

        internal void ReleaseTile(int requesterIndex)
        {
            IntVector2? cur = _tentacleClaim[requesterIndex];
            if (cur.HasValue && _claimedTiles.TryGetValue(cur.Value, out int owner) && owner == requesterIndex)
                _claimedTiles.Remove(cur.Value);

            _tentacleClaim[requesterIndex] = null;
        }

        internal void SyncClaimFromGrabDest(int tentacleIndex, IntVector2? grabDest)
        {
            if (!grabDest.HasValue)
            {
                ReleaseTile(tentacleIndex);
                return;
            }

            if (_tentacleClaim[tentacleIndex].HasValue && _tentacleClaim[tentacleIndex].Value == grabDest.Value)
                return;

            if (!TryClaimTile(tentacleIndex, grabDest.Value))
                tentacles[tentacleIndex].RequestRetarget();
        }

        private void TryDropFromCeiling()
        {
            if (!moving || VecInput.y >= 0) return;

            int anchored = 0;
            int anchoredAbove = 0;
            int failedSearch = 0;

            for (int i = 0; i < tentacles.Length; i++)
            {
                var t = tentacles[i];

                if (t.atGrabDest && t.grabDest != null && t.floatGrabDest != null)
                {
                    anchored++;
                    if (t.floatGrabDest.Value.y > player.mainBodyChunk.pos.y + 5f)
                        anchoredAbove++;
                }

                if (t.foundNoGrabPos > 20)
                    failedSearch++;
            }

            if (anchored > 0
                && anchoredAbove >= Mathf.CeilToInt(anchored * 0.8f)
                && failedSearch >= 3)
            {
                for (int i = 0; i < tentacles.Length; i++)
                {
                    var t = tentacles[i];
                    if (t.atGrabDest && t.floatGrabDest != null
                        && t.floatGrabDest.Value.y > player.mainBodyChunk.pos.y + 5f)
                    {
                        t.ForceReleaseGrabTile();
                        SyncClaimFromGrabDest(i, null);
                    }
                }

                unconditionalSupport = 0f;
            }
        }

        private void UpdateMoveDirection()
        {
            Vector2 inp = VecInput;
            moving = player.input[0].x != 0 || player.input[0].y != 0;

            allowUp = player.input[0].y > 0;
            allowDown = player.input[0].y < 0;

            Vector2 target = (inp != Vector2.zero) ? inp.normalized : Vector2.zero;

            if (moving)
            {
                moveDirection = Vector2.Lerp(moveDirection, target, 0.35f);
            }
            else
            {
                Vector2 idleTarget = new(moveDirection.x, 0f);
                if (idleTarget.sqrMagnitude > 0.0001f) idleTarget.Normalize();

                moveDirection = Vector2.Lerp(moveDirection, idleTarget, 0.25f);
                moveDirection.y = Mathf.Lerp(moveDirection.y, 0f, 0.55f);
            }

            if (!allowUp && moveDirection.y > 0f)
                moveDirection.y = 0f;

            if (moveDirection.sqrMagnitude < 0.0001f)
                moveDirection = Vector2.zero;
        }

        public void Update()
        {
            if (player.Consious && !switchModeReset && player.input[0].spec && player.input[0].y == 0 && player.input[0].x == 0)
            {
                rotModeTransformTime++;
                if (rotMode)
                {
                    for (int i = 0; i < 5; i++)
                        for (int j = 0; j < tentacles[i].tChunks.Length; j++)
                            tentacles[i].tChunks[j].pos = Vector2.Lerp(
                                tentacles[i].tChunks[j].pos,
                                player.mainBodyChunk.pos,
                                Mathf.InverseLerp(0, 40, rotModeTransformTime));
                }
            }
            else if (rotModeTransformTime > 0)
            {
                rotModeTransformTime = 0;
            }

            if (rotModeTransformTime >= 40)
            {
                rotModeTransformTime = 0;
                SwitchTentacleMode();
            }

            if (!player.input[0].spec)
            {
                switchModeReset = false;
            }

            if (!rotMode)
                return;

            UpdateMoveDirection();

            unconditionalSupport = Mathf.Max(0f, unconditionalSupport - 0.025f);
            player.standing = false;

            for (int i = 0; i < 5; i++)
                SyncClaimFromGrabDest(i, tentacles[i].grabDest);

            int legsGrabbing = 0;
            for (int i = 0; i < 5; i++)
            {
                tentacles[i].Update();
                if (tentacles[i].atGrabDest)
                    legsGrabbing++;

                SyncClaimFromGrabDest(i, tentacles[i].grabDest);
            }

            if (player.Consious)
            {
                Act(legsGrabbing);
                player.bodyMode = BodyModeIndexExtension.Rot;
            }
        }

        public void SwitchTentacleMode()
        {
            rotMode = !rotMode;
            switchModeReset = true;
            rotModeTransformTime = 0;

            if (rotMode)
            {
                graphics.Reset();
                player.bodyMode = BodyModeIndexExtension.Rot;
                player.standing = false;
            }
            else
            {
                _claimedTiles.Clear();
                for (int i = 0; i < _tentacleClaim.Length; i++)
                    _tentacleClaim[i] = null;

                player.bodyMode = Player.BodyModeIndex.Default;
            }
            
            Room.PlaySound(SoundID.Daddy_And_Bro_Tentacle_Grab_Creature, player.mainBodyChunk.pos, player.abstractCreature);
        }

        public void Act(int legsGrabbing)
        {
            float num3 = 1.1f;

            Vector2 rawInp = VecInput;

            Vector2 dir = (moveDirection == Vector2.zero) ? rawInp : moveDirection;
            if (!allowUp && dir.y > 0f) dir.y = 0f;
            if (dir != Vector2.zero) dir.Normalize();

            TryDropFromCeiling();

            Vector2 endPos = player.mainBodyChunk.pos + dir * 40f;

            if (!moving)
            {
                unconditionalSupport = 1f;
                if (legsGrabbing > tentacles.Length / 2) num3 = 1f;
                else num3 = 0.5f + Mathf.Lerp(0f, 0.5f, legsGrabbing / (tentacles.Length / 2f));
            }
            else if (legsGrabbing < tentacles.Length / 2)
            {
                num3 *= Mathf.Lerp(0.6f, 1f, legsGrabbing / (tentacles.Length / 2f));
            }

            if (notFollowingPathToCurrentGoalCounter < 200 && Custom.Dist(endPos, player.mainBodyChunk.pos) > 20f)
                notFollowingPathToCurrentGoalCounter++;
            else if (notFollowingPathToCurrentGoalCounter > 0)
                notFollowingPathToCurrentGoalCounter--;

            if (notFollowingPathToCurrentGoalCounter > 100)
            {
                int num4 = 0;
                while (num4 < player.bodyChunks.Length && legsGrabbing == 0)
                {
                    if (player.bodyChunks[num4].ContactPoint.x != 0 || player.bodyChunks[num4].ContactPoint.y != 0)
                        legsGrabbing = 1;
                    num4++;
                }
            }

            if (legsGrabbing > tentacles.Length / 2 && moving)
            {
                float bestRelease = float.MinValue;
                int idx = -1;
                for (int i = 0; i < tentacles.Length; i++)
                {
                    if (tentacles[i].atGrabDest && tentacles[i].ReleaseScore() > bestRelease)
                    {
                        bestRelease = tentacles[i].ReleaseScore();
                        idx = i;
                    }
                }
                if (idx > -1)
                {
                    List<IntVector2> list = null;
                    tentacles[idx].UpdateClimbGrabPos(ref list);
                    SyncClaimFromGrabDest(idx, tentacles[idx].grabDest);
                }
            }

            float grip = 0f;
            for (int i = 0; i < tentacles.Length; i++)
            {
                float g = Mathf.Pow(tentacles[i].chunksGripping, 0.5f);

                if (tentacles[i].atGrabDest && tentacles[i].grabDest != null)
                    g = Mathf.Lerp(g, 1f, 0.75f);

                grip += g / tentacles.Length;
            }

            float dirSupport = Mathf.Pow(grip, 0.8f);
            grip = Mathf.Pow(grip, 0.3f);
            grip = Mathf.Max(grip, unconditionalSupport);
            dirSupport = Mathf.Max(dirSupport, unconditionalSupport);

            player.mainBodyChunk.vel *= Mathf.Lerp(1f, 0.95f, grip);
            player.mainBodyChunk.vel.y += (player.gravity - player.buoyancy * player.mainBodyChunk.submersion) * grip * num3 * 2f;

            if (player.bodyChunks[1].ContactPoint.y == -1 && player.mainBodyChunk.ContactPoint.y != -1 && VecInput.y < 0)
            {
                int rotateDir = player.flipDirection;
                if (player.bodyChunks[0].pos.x > player.bodyChunks[1].pos.x)
                {
                    rotateDir = 1;
                }
                else if (player.bodyChunks[0].pos.x < player.bodyChunks[1].pos.x)
                {
                    rotateDir = -1;
                }
                player.bodyChunks[1].vel.x -= rotateDir / 3f;
                player.bodyChunks[0].vel += new Vector2(rotateDir * 0.4f, -1.5f);
            }
            if (player.input[0].x != 0 && player.IsTileSolid(0, player.input[0].x, 0) && !player.IsTileSolid(1, player.input[0].x, 0))
            {
                player.bodyChunks[1].vel.x += player.input[0].x;
            }
            else if (player.input[0].x != 0 && player.bodyChunks[0].pos.y > player.bodyChunks[1].pos.y && !player.IsTileSolid(0, player.input[0].x, 0) && player.IsTileSolid(1, player.input[0].x, 0))
            {
                player.bodyChunks[0].vel.x += player.input[0].x;
            }

            if (moving)
            {
                Vector2 v2 = Custom.DirVec(player.mainBodyChunk.pos, Room.MiddleOfTile(endPos)) * 0.25f * dirSupport;

                if (!allowUp) v2.y = Mathf.Min(0f, v2.y);
                else v2.y *= 0.60f;

                player.mainBodyChunk.vel += v2;

                player.GoThroughFloors = Room.GetWorldCoordinate(endPos).y < Room.GetWorldCoordinate(player.mainBodyChunk.pos).y;
            }

            if (!allowUp)
            {
                if (!allowDown)
                {
                    float lift = 0f;
                    int c = 0;
                    for (int i = 0; i < tentacles.Length; i++)
                    {
                        if (tentacles[i].atGrabDest && tentacles[i].floatGrabDest != null)
                        {
                            float dy = tentacles[i].floatGrabDest.Value.y - player.mainBodyChunk.pos.y;
                            if (dy > 0f)
                            {
                                lift += dy;
                                c++;
                            }
                        }
                    }

                    if (c > 0)
                    {
                        lift /= c;
                        float anti = Mathf.InverseLerp(10f, 140f, lift);
                        player.mainBodyChunk.vel.y += anti * 10;
                    }
                }
                var input = player.input[0];
                bool idleInput = input.x == 0 && input.y == 0;
                bool holdingTile = false;

                for (int i = 0; i < tentacles.Length; i++)
                {
                    if (tentacles[i].atGrabDest && tentacles[i].grabDest != null)
                    {
                        holdingTile = true;
                        break;
                    }
                }

                if (idleInput && holdingTile)
                {
                    float dy = player.mainBodyChunk.pos.y - player.mainBodyChunk.lastPos.y;

                    if (dy > 0f)
                        maxUpVelNoUp -= (dy > 0.03f) ? 0.03f : 0.01f;
                    else if (dy < 0f)
                        maxUpVelNoUp += (dy < -0.03f) ? 0.03f : 0.01f;
                }

                for (int i = 0; i < player.bodyChunks.Length; i++)
                    if (player.bodyChunks[i].vel.y > maxUpVelNoUp)
                        player.bodyChunks[i].vel.y = maxUpVelNoUp;
            }
        }
    }

    public static class RotCWT
    {
        public static readonly ConditionalWeakTable<Player, ViyRotModule> rotModule = new();

        public static bool TryGetRot(this Player player, out ViyRotModule rotControl)
            => rotModule.TryGetValue(player, out rotControl);
    }
}