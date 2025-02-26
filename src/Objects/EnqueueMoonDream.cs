using System.Collections.Generic;
using static VoidTemplate.SaveManager;

namespace VoidTemplate.Objects;

internal class EnqueueMoonDream : UpdatableAndDeletable
{
    public EnqueueMoonDream(Room room) : base()
    {
        this.room = room;
        absPlayers = room.game.Players;
        saveState = room.game.GetStorySession.saveState;
    }
    List<AbstractCreature> absPlayers = null;
    SaveState saveState;
    public override void Update(bool eu)
    {
        base.Update(eu);
        if (absPlayers.Exists(absply => absply.Room == room.abstractRoom))
        {
            slatedForDeletetion = true;
            saveState.EnlistDreamIfNotSeen(Dream.Moon);
        }
    }
}