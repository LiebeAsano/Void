namespace VoidTemplate;

public static class CreatureTemplateType
{
    public static CreatureTemplate.Type Mimicstarfish = new("Mimicstar", true);
    public static CreatureTemplate.Type Outspector = new(nameof(Outspector), true);
    public static CreatureTemplate.Type OutspectorB = new(nameof(OutspectorB), true);
    public static void UnregisterValues()
    {
        if (Mimicstarfish != null)
        {
            Mimicstarfish.Unregister();
            Mimicstarfish = null;
        }
        if (Outspector != null)
        {
            Outspector.Unregister();
            Outspector = null;
        }
        if (OutspectorB != null)
        {
            OutspectorB.Unregister();
            OutspectorB = null;
        }
    }
}

public static class SandboxUnlockID
{
    public static MultiplayerUnlocks.SandboxUnlockID Mimicstarfish = new("Mimicstar", true);
    public static MultiplayerUnlocks.SandboxUnlockID Outspector = new(nameof(Outspector), true);
    public static MultiplayerUnlocks.SandboxUnlockID OutspectorB = new(nameof(OutspectorB), true);
    public static void UnregisterValues()
    {
        if (Mimicstarfish != null)
        {
            Mimicstarfish.Unregister();
            Mimicstarfish = null;
        }
        if (Outspector != null)
        {
            Outspector.Unregister();
            Outspector = null;
        }
        if (OutspectorB != null)
        {
            OutspectorB.Unregister();
            OutspectorB = null;
        }
    }
}

