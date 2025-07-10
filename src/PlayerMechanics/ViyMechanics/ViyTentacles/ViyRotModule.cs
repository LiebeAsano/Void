using Menu.Remix.MixedUI;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
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

        public bool moving;

        public bool rotMode = false;

        public int rotModeTransformTime;

        public Room room
        {
            get
            {
                return player.room;
            }
        }

        public Vector2 VecInput
        {
            get
            {
                return new Vector2(player.input[0].x, player.input[0].y);
            }
        }

        public ViyRotModule(Player player)
        {
            this.player = player;
            for (int i = 0; i < 5; i++)
            {
                tentacles[i] = new(player, this, player.mainBodyChunk, 160, Custom.DegToVec(Mathf.Lerp(0, 360, i / 5)));
            }
            graphics = new(this);
            NewRoom(player.room);
        }

        public void NewRoom(Room newRoom)
        {
            for (int i = 0; i < 5; i++)
            {
                tentacles[i].NewRoom(newRoom);
            }
        }

        public void Update()
        {
            if (player.Consious && player.input[0].spec && player.input[0].y < 0)
            {
                rotModeTransformTime++;
                if (rotMode)
                {
                    for (int i = 0; i < 5; i++)
                    {
                        for (int j = 0; j < tentacles[i].tChunks.Length; j++)
                        {
                            tentacles[i].tChunks[j].pos = Vector2.Lerp(tentacles[i].tChunks[j].pos, player.mainBodyChunk.pos, Mathf.InverseLerp(0, 80, rotModeTransformTime));
                        }
                    }
                }
            }
            else if (rotModeTransformTime > 0)
            {
                rotModeTransformTime--;
            }
            if (rotModeTransformTime >= 80)
            {
                rotModeTransformTime = 0;
                SwitchTentacleMode();
            }

            if (rotMode)
            {
                unconditionalSupport = Mathf.Max(0f, unconditionalSupport - 0.025f);
                player.standing = false;
                int legsGrabbing = 0;
                for (int i = 0; i < 5; i++)
                {
                    tentacles[i].Update();
                    if (tentacles[i].atGrabDest)
                    {
                        legsGrabbing++;
                    }
                }
                if (player.Consious)
                {
                    Act(legsGrabbing);
                    player.bodyMode = BodyModeIndexExtension.Rot;
                }
            }
        }

        public void SwitchTentacleMode()
        {
            rotMode = !rotMode;
            if (rotMode)
            {
                graphics.Reset();
                player.bodyMode = BodyModeIndexExtension.Rot;
                player.standing = false;
            }
            else
            {
                player.bodyMode = Player.BodyModeIndex.Default;
            }
            room.PlaySound(SoundID.Daddy_And_Bro_Tentacle_Grab_Creature, player.mainBodyChunk.pos, player.abstractCreature);
        }

        public void Act(int legsGrabbing)
        {
            float num3 = 1.1f;
            Vector2 endPos = player.mainBodyChunk.pos + VecInput * 40;

            if (!moving)
            {
                unconditionalSupport = 1f;
                if (legsGrabbing > tentacles.Length / 2)
                {
                    num3 = 1f;
                }
                else
                {
                    num3 = 0.5f + Mathf.Lerp(0f, 0.5f, legsGrabbing / (tentacles.Length / 2));
                }
            }
            else if (legsGrabbing < tentacles.Length / 2)
            {
                num3 *= Mathf.Lerp(0.6f, 1f, legsGrabbing / (tentacles.Length / 2));
            }

            if (notFollowingPathToCurrentGoalCounter < 200 && Custom.Dist(endPos, player.mainBodyChunk.pos) > 20f)
            {
                notFollowingPathToCurrentGoalCounter++;
            }
            else if (notFollowingPathToCurrentGoalCounter > 0)
            {
                notFollowingPathToCurrentGoalCounter--;
            }

            if (notFollowingPathToCurrentGoalCounter > 100)
            {
                int num4 = 0;
                while (num4 < player.bodyChunks.Length && legsGrabbing == 0)
                {
                    if (player.bodyChunks[num4].ContactPoint.x != 0 || player.bodyChunks[num4].ContactPoint.y != 0)
                    {
                        legsGrabbing = 1;
                    }
                    num4++;
                }
            }

            if (legsGrabbing > tentacles.Length / 2 && moving)
            {
                float num6 = float.MinValue;
                int num7 = -1;
                for (int num8 = 0; num8 < tentacles.Length; num8++)
                {
                    if (tentacles[num8].atGrabDest && tentacles[num8].ReleaseScore() > num6)
                    {
                        num6 = tentacles[num8].ReleaseScore();
                        num7 = num8;
                    }
                }
                if (num7 > -1)
                {
                    List<IntVector2> list = null;
                    tentacles[num7].UpdateClimbGrabPos(ref list);
                }
            }

            float num9 = 0f;
            float num10 = 0f;
            for (int num11 = 0; num11 < tentacles.Length; num11++)
            {
                float num12 = Mathf.Pow(tentacles[num11].chunksGripping, 0.5f);
                if (tentacles[num11].atGrabDest && tentacles[num11].grabDest != null)
                {
                    num10 += Mathf.Pow(Mathf.InverseLerp(-0.1f, 0.85f, Vector2.Dot(((moveDirection.y < 0) ? player.mainBodyChunk.pos - tentacles[num11].floatGrabDest.Value : tentacles[num11].floatGrabDest.Value - player.mainBodyChunk.pos).normalized, moveDirection)), 0.8f) / tentacles.Length;
                    num12 = Mathf.Lerp(num12, 1f, 0.75f);
                }
                num9 += num12 / tentacles.Length;
            }
            num10 = Mathf.Pow(num10 * num9, 0.8f);
            num9 = Mathf.Pow(num9, 0.3f);
            num9 = Mathf.Max(num9, unconditionalSupport);
            num10 = Mathf.Max(num10, unconditionalSupport);

            player.mainBodyChunk.vel *= Mathf.Lerp(1f, 0.95f, num9);
            player.mainBodyChunk.vel.y += (player.gravity - player.buoyancy * player.mainBodyChunk.submersion) * num9 * num3 * 2;

            moving = player.input[0].x != 0 || player.input[0].y != 0;
            if (moving)
            {
                if (Custom.ManhattanDistance(player.abstractCreature.pos, Custom.MakeWorldCoordinate(new((int)endPos.x / 20, (int)endPos.y / 20), player.abstractCreature.Room.index)) < 3)
                {
                    player.mainBodyChunk.vel += Vector2.ClampMagnitude(room.MiddleOfTile((int)endPos.x / 20, (int)endPos.y / 20) - player.mainBodyChunk.pos, 30) / 30 * 0.25f * num10;
                }
                player.GoThroughFloors = room.GetWorldCoordinate(endPos).y < room.GetWorldCoordinate(player.mainBodyChunk.pos).y;
                player.mainBodyChunk.vel += Custom.DirVec(player.mainBodyChunk.pos, room.MiddleOfTile(endPos)) * 0.25f * num10;
            }
            
            moveDirection = VecInput;
        }
    }

    public static class RotCWT
    {
        internal static ConditionalWeakTable<Player, ViyRotModule> rotModule = new();

        public static bool TryGetRot(this Player player, out ViyRotModule rotControl)
        {
            return rotModule.TryGetValue(player, out rotControl);
        }
    }
}
