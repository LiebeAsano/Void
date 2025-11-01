using MoreSlugcats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VoidTemplate.PlayerMechanics;

namespace VoidTemplate.Objects
{
    public class VoidDreamScript : MSCRoomSpecificScript.ArtificerDream
    {
        public static bool IsVoidDream;

        public static int StateAfterDream = 0;

        public AbstractCreature hunter;

        public AbstractCreature daddyPuppet;

        public bool shortcutsHide;

        public override void SceneSetup()
        {
            if (hunter == null)
            {
                room.game.GetStorySession.saveState.deathPersistentSaveData.karmaCap =
                room.game.GetStorySession.saveState.deathPersistentSaveData.karma = 10;
                hunter = room.game.Players[0];
                hunter.Move(new(room.abstractRoom.index, -1, -1, 1));
                for (int i = 0; i < 2; i++)
                {
                    AbstractSpear spear = new(room.world, null, room.GetWorldCoordinate(room.ShortcutLeadingToNode(1).StartTile), room.game.GetNewID(), false);
                    room.abstractRoom.AddEntity(spear);
                    spear.RealizeInRoom();
                }
                foreach (var shortcut in room.shortcutsIndex)
                {
                    if (!room.lockedShortcuts.Contains(shortcut))
                    {
                        room.lockedShortcuts.Add(shortcut);
                    }
                }
            }
            if (daddyPuppet == null)
            {
                daddyPuppet = new(room.world, StaticWorld.GetCreatureTemplate(MoreSlugcatsEnums.CreatureTemplateType.HunterDaddy), null, new(room.world.offScreenDen.index, -1, -1, 0), room.world.game.GetNewID());
                daddyPuppet.Move(new(room.abstractRoom.index, -1, -1, 0));
            }
            if (hunter != null && daddyPuppet != null)
            {
                sceneStarted = true;
            }
        }

        public override void TimedUpdate(int timer)
        {
            if (room.BeingViewed && !shortcutsHide)
            {
                var sGraphics = room.game.cameras[0].shortcutGraphics;
                for (int i = 0; i < room.shortcuts.Length; i++)
                {
                    if (sGraphics.entranceSprites.Length > i && sGraphics.entranceSprites[i, 0] != null)
                    {
                        sGraphics.entranceSprites[i, 0].isVisible = false;
                    }
                }
                shortcutsHide = true;
            }
            if (hunter.state.dead || daddyPuppet.state.dead)
            {
                room.game.VoidDreamEnd();
            }
        }
    }
}
