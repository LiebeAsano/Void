using System.Collections.Generic;
using System.Runtime.CompilerServices;
using SlugBase.SaveData;

namespace VoidTemplate.Defender;

public class RegionalPunishmentTracker
{
    // CONFIG
    const ushort ApexPredatorAfraidLimit = 3;
    public List<CreatureTemplate.Type> Apex = [CreatureTemplate.Type.RedLizard,
        CreatureTemplate.Type.RedCentipede,
        MoreSlugcats.MoreSlugcatsEnums.CreatureTemplateType.TrainLizard, 
        CreatureTemplate.Type.SpitterSpider, 
        DLCSharedEnums.CreatureTemplateType.AquaCenti,
        DLCSharedEnums.CreatureTemplateType.MirosVulture,
        CreatureTemplate.Type.KingVulture];
    
    public RegionalPunishmentTracker(World world)
    {
        this.world = world;
        if (world.game.session is not StoryGameSession) punishmentsPerTemplateTypeTemporal = [];
    }
    
    public static ConditionalWeakTable<World, RegionalPunishmentTracker> RPTField = new();
    Dictionary<string, ushort> punishmentsPerTemplateTypeTemporal;
    private World world;
    
    
    public void Punish(CreatureTemplate.Type type)
    {
        var dic = GiveDictionary();
        if(!dic.ContainsKey(type.value)) dic[type.value] = 0;
        dic[type.value]++;
        if (world.game.session is StoryGameSession { saveState: SaveState save })
        {
            save.miscWorldSaveData.GetSlugBaseData().Set($"Protector-{world.name}", dic);
        }
    }

    Dictionary<string, ushort> GiveDictionary()
    {
        if (world.game.session is StoryGameSession { saveState: SaveState save })
        {
            return save.miscWorldSaveData.GetSlugBaseData()
                .TryGet<Dictionary<string, ushort>>($"Protector-{world.name}", out var dic) ? dic : [];
        }
        return punishmentsPerTemplateTypeTemporal;
    }

    public bool IsAfraid(CreatureTemplate.Type type)
    {
        var dic = GiveDictionary();
        ushort amount = dic.TryGetValue(type.value, out var value) ? value : (ushort)0;
        return Apex.Contains(type) ? amount >= ApexPredatorAfraidLimit : amount >= 1;
    }
    
}

static class PunishmentExtensions
{
    public static void Punish(this World world, CreatureTemplate.Type type)
    {
        RegionalPunishmentTracker p;
        if (!RegionalPunishmentTracker.RPTField.TryGetValue(world, out p))
        {
            p = new(world);
            RegionalPunishmentTracker.RPTField.Add(world, p);
        }
        p.Punish(type);
    }

    public static bool IsAfraidOfDefender(this Creature crit)
    {
        World world = crit.abstractCreature.world;
        if (!RegionalPunishmentTracker.RPTField.TryGetValue(world, out RegionalPunishmentTracker p))
        {
            p = new RegionalPunishmentTracker(world);
            RegionalPunishmentTracker.RPTField.Add(world, p);
        }
        return p.IsAfraid(crit.Template.type);
    }

    public static bool IsAfraidOfDefender(this AbstractCreature crit)
    {
        World world = crit.world;
        if (!RegionalPunishmentTracker.RPTField.TryGetValue(world, out RegionalPunishmentTracker p))
        {
            p = new RegionalPunishmentTracker(world);
            RegionalPunishmentTracker.RPTField.Add(world, p);
        }
        return p.IsAfraid(crit.creatureTemplate.type);
    }
}