namespace VoidTemplate.Objects.PomObjects;
using static Pom.Pom;
using static Useful.POMUtils;
using static SaveManager;
using UnityEngine;

public class TriggeredSpasm : UpdatableAndDeletable
{
    public static void Register()
    {
        RegisterFullyManagedObjectType( [
        defaultVectorField,
        new FloatField(length, 1f, 20f, 10f, displayName: "Duration"),
        new FloatField(severity, 0f, 1f, 0.5f, 0.05f, displayName: "Strength"),
        new IntegerField(conversationsHad, 0, 20, 0, ManagedFieldWithPanel.ControlType.slider, "SS Conversations had")
        ], typeof(TriggeredSpasm), "Triggered Spasm",  "The Void");
    }

    public TriggeredSpasm(Room room, PlacedObject pOjb)
    {
        data = pOjb.data as ManagedData;
        placedObject = pOjb;
        this.room = room;
    }

    #region fieldIDs
    const string length = "Length";
    const string severity = "Severity";
    const string triggerZone = "trigger zone";
    const string conversationsHad = "conversationshad";
    #endregion

    #region data accessors
    readonly ManagedData data;
    readonly PlacedObject placedObject;
    Vector2[] TriggerZone => AddRealPosition(data.GetValue<Vector2[]>(triggerZone), placedObject.pos);
    float Duration => data.GetValue<float>(length);
    float Strength => data.GetValue<float>(severity);
    int ConversationsHad => data.GetValue<int>(conversationsHad);
    #endregion

    public override void Update(bool eu)
    {
        base.Update(eu);
        if (!room.game.GetStorySession.saveState.IsValidForAppearing(room.abstractRoom.name))
        {
            slatedForDeletetion = true;
            return;
        }
        if (room.game.GetStorySession.saveState.miscWorldSaveData.SSaiConversationsHad >= ConversationsHad)
        {
            Player candidate = null;
            if (room.game.Players.Exists(player =>
                {
                    if (player.realizedCreature.room == room &&
                        PositionWithinPoly(TriggerZone, player.realizedCreature.mainBodyChunk.pos))
                    {
                        candidate = player.realizedCreature as Player;
                        return true;
                    }
                    return false;
                }))
            {
                HunterSpasms.Spasm(candidate, Duration, Strength);
                room.game.GetStorySession.saveState.DelistConvulsion(room.abstractRoom.name);
                slatedForDeletetion = true;
            }
        }
    }
}