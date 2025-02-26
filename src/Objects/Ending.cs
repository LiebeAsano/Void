using UnityEngine;
using VoidTemplate.Useful;

namespace VoidTemplate.Objects;
internal class Ending : UpdatableAndDeletable
{
    #region immutable
    const int DelayTreshold = 1 * Utils.TicksPerSecond;
    static Vector2 expectedPositionOfTrigger = new(350, 360);
    const int triggerRadius = 160;
    const int timeToMoveCamera = 9 * Utils.TicksPerSecond;
    /// <summary>
    /// I am using S curve to move camera to desired position.
    /// https://www.desmos.com/calculator/eijfplyf1l
    /// this should be between 0 and 1
    /// </summary>
    const float cameraMoveSteepnessModifier = 0.15f;
    static Vector2 desiredCamOffset = new(0, 4500);
    static Vector2 initialcampos;
    private enum State
    {
        WaitingForPlayer,
        PreStartDelay,
        MovingCamera,
        End
    }
    #endregion
    #region mutable
    RoomCamera camera;
    State state;
    int timer;
    #endregion
    public Ending(Room room)
    {
        this.room = room;
    }
    public override void Update(bool eu)
    {
        switch (state)
        {
            case State.WaitingForPlayer:
                {
                    if (room.world.game.Players.Exists(x =>
                    x.realizedCreature is Player p
                    && p.IsVoid()
                    && (p.KarmaCap == 10 || room.world.game.GetStorySession.saveState.miscWorldSaveData.SSaiConversationsHad >= 8)
                    && x.Room == room.abstractRoom
                    && (p.mainBodyChunk.pos - expectedPositionOfTrigger).magnitude < triggerRadius))
                    {
                        state = State.PreStartDelay;
                        RainWorld.lockGameTimer = true;
                    }
                    break;
                }
            case State.PreStartDelay:
                {
                    timer++;
                    if (timer > DelayTreshold)
                    {
                        state = State.MovingCamera;
                        timer = 0;
                        camera = room.game.cameras[0];
                        initialcampos = camera.pos;
                    }
                    break;
                }
            case State.MovingCamera:
                {

                    camera.pos = SCurveVectors(initialcampos, initialcampos + desiredCamOffset, timer / ((float)timeToMoveCamera));
                    camera.hardLevelGfxOffset = SCurveVectors(new Vector2(), desiredCamOffset, timer / ((float)timeToMoveCamera));
                    timer++;
                    if (timer == timeToMoveCamera)
                    {
                        state = State.End;
                        slatedForDeletetion = true;
                        room.game.GetStorySession.saveState.SetEndingEncountered(true);
                        room.game.rainWorld.progression.SaveWorldStateAndProgression(false);
                        room.game.rainWorld.processManager.RequestMainProcessSwitch(ProcessManager.ProcessID.Statistics);
                    }
                    break;
                }

        }
    }
    private Vector2 SCurveVectors(Vector2 a, Vector2 b, float x)
    {
        float progress = RWCustom.Custom.SCurve(x, cameraMoveSteepnessModifier);
        return Vector2.Lerp(a, b, progress);
    }
}

