using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using System;



namespace VoidTemplate;

public static class CreatureTemplateType
{
    public static CreatureTemplate.Type Mimicstarfish = new CreatureTemplate.Type("Mimicstar", true);

    public static CreatureTemplate.Type Outspector = new(nameof(Outspector), true);

    public static CreatureTemplate.Type OutspectorB = new(nameof(OutspectorB), true);

    public static CreatureTemplate.Type IceLizard = new("IceLizard", true);

    public static CreatureTemplate.Type DartspiderP = new("DartspiderP", true);

    public static CreatureTemplate.Type DartspiderH = new("DartspiderH", true);

    public static AbstractPhysicalObject.AbstractObjectType DartPoison = new("DartPoison", true);

    public static AbstractPhysicalObject.AbstractObjectType DartHypothermic = new("DartHypothermic", true);

    public static AbstractPhysicalObject.AbstractObjectType MiniEnergyCell = new("MiniEnergyCell", true);

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
        if (IceLizard != null)
        {
            IceLizard.Unregister();
            IceLizard = null;
        }
        if (DartspiderP != null)
        {
            DartspiderP.Unregister();
            DartspiderP = null;
        }
        if (DartspiderH != null)
        {
            DartspiderH.Unregister();
            DartspiderH = null;
        }
        if (DartPoison != null)
        {
            DartPoison.Unregister();
            DartPoison = null;
        }
        if (DartHypothermic != null)
        {
            DartHypothermic.Unregister();
            DartHypothermic = null;
        }
        if (MiniEnergyCell != null)
        {
            MiniEnergyCell.Unregister();
            MiniEnergyCell = null;
        }
    }
}

public static class SandboxUnlockID
{
    public static MultiplayerUnlocks.SandboxUnlockID Mimicstarfish = new("Mimicstar", true);

    public static MultiplayerUnlocks.SandboxUnlockID Outspector = new(nameof(Outspector), true);

    public static MultiplayerUnlocks.SandboxUnlockID OutspectorB = new(nameof(OutspectorB), true);

    public static MultiplayerUnlocks.SandboxUnlockID IceLizard = new("IceLizard", true);

    public static MultiplayerUnlocks.SandboxUnlockID DartspiderP = new("DartspiderP", true);

    public static MultiplayerUnlocks.SandboxUnlockID DartspiderH = new("DartspiderH", true);

    public static MultiplayerUnlocks.SandboxUnlockID MiniEnergyCell = new("MiniEnergyCell", true);

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
        if (IceLizard != null)
        {
            IceLizard.Unregister();
            IceLizard = null;
        }
        if (DartspiderP != null)
        {
            DartspiderP.Unregister();
            DartspiderP = null;
        }
        if (DartspiderH != null)
        {
            DartspiderH.Unregister();
            DartspiderH = null;
        }
        if (MiniEnergyCell != null)
        {
            MiniEnergyCell.Unregister();
            MiniEnergyCell = null;
        }
    }
}

