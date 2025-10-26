using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.Hideout;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Enums.Hideout;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Services.Mod;

namespace DynamicMaps.Server;

public record ModMetadata : AbstractModMetadata
{
    public override string ModGuid { get; init; } = "com.mpstark.dynamicmaps";
    public override string Name { get; init; } = "Dynamic Maps";
    public override string Author { get; init; } = "mpstark";
    public override List<string>? Contributors { get; init; } = [" dirtbikercj, acidphantasm"];
    public override SemanticVersioning.Version Version { get; init; } = new("1.0.3");
    public override SemanticVersioning.Range SptVersion { get; init; } = new("~4.0.0");
    public override List<string>? Incompatibilities { get; init; }
    public override Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; }
    public override string? Url { get; init; }
    public override bool? IsBundleMod { get; init; }
    public override string? License { get; init; } = "MIT";
}

[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 90000)]
public class DynamicMapsServer(
    DatabaseService databaseService,
    ModHelper modHelper,
    CustomItemService customItemService,
    CustomStaticRouter customStaticRouter)
    : IOnLoad
{
    private ModConfig? _modConfig;
    
    public Task OnLoad()
    { 
        var pathToMod = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
        _modConfig = modHelper.GetJsonDataFromFile<ModConfig>(pathToMod, "config.json");
        
        customStaticRouter.PassConfig(_modConfig);
        
        CreateNewMaps();
        return Task.CompletedTask;
    }
    
    private void CreateNewMaps()
    {
        CreateGroundZeroMap();
        CreateStreetsMap();
        CreateReserveMap();
        CreateLabsMap();
        CreateLighthouseMap();
        CreateLabyrinthMap();
    }

    private void CreateGroundZeroMap()
    {
        NewItemFromCloneDetails groundZeroMap = new NewItemFromCloneDetails()
        {
            ItemTplToClone = ItemTpl.MAP_WOODS_PLAN,
            ParentId = "567849dd4bdc2d150f8b456e",
            NewId = "6738033eb7305d3bdafe9518",
            FleaPriceRoubles = 25000,
            HandbookPriceRoubles = 32500,
            HandbookParentId = "5b47574386f77428ca22b343",
            OverrideProperties = new TemplateItemProperties
            {
                Name = "Ground Zero plan map",
                ShortName = "Ground Zero",
                Description = "A map of Ground Zero. Ignore the fact there's two separate Ground Zero maps but they're the same. Crazy.",
            },
            Locales = new Dictionary<string, LocaleDetails>()
            {
                {
                    "en",  new LocaleDetails()
                    {
                        Name = "Ground Zero plan map",
                        ShortName = "Ground Zero",
                        Description = "A map of Ground Zero. Ignore the fact there's two separate Ground Zero maps but they're the same. Crazy."
                    }
                }
            }
        };

        var assortId = "6738076415fd9232e8dae982";
        customItemService.CreateItemFromClone(groundZeroMap);
        PushToTraderAssort(Traders.THERAPIST, groundZeroMap.NewId, groundZeroMap.HandbookPriceRoubles, assortId);
    }

    private void CreateStreetsMap()
    {
        NewItemFromCloneDetails streetsMap = new NewItemFromCloneDetails()
        {
            ItemTplToClone = ItemTpl.MAP_WOODS_PLAN,
            ParentId = "567849dd4bdc2d150f8b456e",
            NewId = "673803448cb3819668d77b1b",
            FleaPriceRoubles = 25000,
            HandbookPriceRoubles = 32500,
            HandbookParentId = "5b47574386f77428ca22b343",
            OverrideProperties = new TemplateItemProperties
            {
                Name = "Streets of Tarkov plan map",
                ShortName = "Streets of Tarkov",
                Description = "A map of Streets of Tarkov. Some say it's the best map, but only if you have a nasa pc.",
            },
            Locales = new Dictionary<string, LocaleDetails>()
            {
                {
                    "en",  new LocaleDetails()
                    {
                        Name = "Streets of Tarkov plan map",
                        ShortName = "Streets of Tarkov",
                        Description = "A map of Streets of Tarkov. Some say it's the best map, but only if you have a nasa pc."
                    }
                }
            }
        };

        var assortId = "67380769ebda082cf01c3fd7";
        customItemService.CreateItemFromClone(streetsMap);
        PushToTraderAssort(Traders.THERAPIST, streetsMap.NewId, streetsMap.HandbookPriceRoubles, assortId);
    }

    private void CreateReserveMap()
    {
        NewItemFromCloneDetails reserveMap = new NewItemFromCloneDetails()
        {
            ItemTplToClone = ItemTpl.MAP_WOODS_PLAN,
            ParentId = "567849dd4bdc2d150f8b456e",
            NewId = "6738034a9713b5f42b4a8b78",
            FleaPriceRoubles = 25000,
            HandbookPriceRoubles = 32500,
            HandbookParentId = "5b47574386f77428ca22b343",
            OverrideProperties = new TemplateItemProperties
            {
                Name = "Reserve plan map",
                ShortName = "Reserve",
                Description = "A map of Reserve. For when you need to know where you are on the best map, and you think it's still 2020.",
            },
            Locales = new Dictionary<string, LocaleDetails>()
            {
                {
                    "en",  new LocaleDetails()
                    {
                        Name = "Reserve plan map",
                        ShortName = "Reserve",
                        Description = "A map of Reserve. For when you need to know where you are on the best map, and you think it's still 2020."
                    }
                }
            }
        };

        var assortId = "6738076e704fef20a1a580e6";
        customItemService.CreateItemFromClone(reserveMap);
        PushToTraderAssort(Traders.THERAPIST, reserveMap.NewId, reserveMap.HandbookPriceRoubles, assortId);
    }

    private void CreateLabsMap()
    {
        NewItemFromCloneDetails labsMap = new NewItemFromCloneDetails()
        {
            ItemTplToClone = ItemTpl.MAP_WOODS_PLAN,
            ParentId = "567849dd4bdc2d150f8b456e",
            NewId = "6738034e9d22459ad7cd1b81",
            FleaPriceRoubles = 25000,
            HandbookPriceRoubles = 32500,
            HandbookParentId = "5b47574386f77428ca22b343",
            OverrideProperties = new TemplateItemProperties
            {
                Name = "Laboratory plan map",
                ShortName = "Laboratory",
                Description = "A map of Laboratory. Thank god this isn't live, or you'd never need this map because you would get head-eyes'd in 0.3 seconds off spawn.",
            },
            Locales = new Dictionary<string, LocaleDetails>()
            {
                {
                    "en",  new LocaleDetails()
                    {
                        Name = "Laboratory plan map",
                        ShortName = "Laboratory",
                        Description = "A map of Laboratory. Thank god this isn't live, or you'd never need this map because you would get head-eyes'd in 0.3 seconds off spawn."
                    }
                }
            }
        };

        var assortId = "673807742ef49729b9dd1b0a";
        customItemService.CreateItemFromClone(labsMap);
        PushToTraderAssort(Traders.THERAPIST, labsMap.NewId, labsMap.HandbookPriceRoubles, assortId);
    }

    private void CreateLighthouseMap()
    {
        NewItemFromCloneDetails lighthouseMap = new NewItemFromCloneDetails()
        {
            ItemTplToClone = ItemTpl.MAP_WOODS_PLAN,
            ParentId = "567849dd4bdc2d150f8b456e",
            NewId = "6738035350b24a4ae4a57997",
            FleaPriceRoubles = 25000,
            HandbookPriceRoubles = 32500,
            HandbookParentId = "5b47574386f77428ca22b343",
            OverrideProperties = new TemplateItemProperties
            {
                Name = "Lighthouse plan map",
                ShortName = "Lighthouse",
                Description = "A map of Lighthouse. This should help you navigate this map..oh wait. The design is so straightforward you don't really need a map. Such map design. Wow. 10/10.\n\n NOTICE: Does not include a map of the mines to Lightkeeper. Git gud.",
            },
            Locales = new Dictionary<string, LocaleDetails>()
            {
                {
                    "en",  new LocaleDetails()
                    {
                        Name = "Lighthouse plan map",
                        ShortName = "Lighthouse",
                        Description = "A map of Lighthouse. This should help you navigate this map..oh wait. The design is so straightforward you don't really need a map. Such map design. Wow. 10/10.\n\n NOTICE: Does not include a map of the mines to Lightkeeper. Git gud."
                    }
                }
            }
        };

        var assortId = "6738077be5a03fda63c9917d";
        customItemService.CreateItemFromClone(lighthouseMap);
        PushToTraderAssort(Traders.THERAPIST, lighthouseMap.NewId, lighthouseMap.HandbookPriceRoubles, assortId);
    }
    
    private void CreateLabyrinthMap()
    {
        NewItemFromCloneDetails labyrinthMap = new NewItemFromCloneDetails()
        {
            ItemTplToClone = ItemTpl.MAP_WOODS_PLAN,
            ParentId = "567849dd4bdc2d150f8b456e",
            NewId = "68f1ad32317cc52f4c0b6fae",
            FleaPriceRoubles = 25000,
            HandbookPriceRoubles = 32500,
            HandbookParentId = "5b47574386f77428ca22b343",
            OverrideProperties = new TemplateItemProperties
            {
                Name = "Labyrinth plan map",
                ShortName = "Labyrinth",
                Description = "A map of Labyrinth. Ignore the fact there's two separate Ground Zero maps but they're the same. Crazy.",
            },
            Locales = new Dictionary<string, LocaleDetails>()
            {
                {
                    "en",  new LocaleDetails()
                    {
                        Name = "Labyrinth plan map",
                        ShortName = "Labyrinth",
                        Description = "A map of Labyrinth. Ignore the fact there's two separate Ground Zero maps but they're the same. Crazy."
                    }
                }
            }
        };

        var assortId = "68f1ad38317cc52f4c0b6faf";
        customItemService.CreateItemFromClone(labyrinthMap);
        PushToTraderAssort(Traders.THERAPIST, labyrinthMap.NewId, labyrinthMap.HandbookPriceRoubles, assortId);
    }

    private void PushToTraderAssort(MongoId traderId, MongoId itemId, double? price, MongoId assortId)
    {
        var assort = databaseService.GetTrader(traderId).Assort;

        var assortEntry = new Item()
        {
            Id = assortId,
            Template = itemId,
            ParentId = "hideout",
            SlotId = "hideout",
            Upd = new Upd()
            {
                UnlimitedCount = false,
                StackObjectsCount = 4,
                BuyRestrictionMax = 10,
                BuyRestrictionCurrent = 0
            }
        };

        var barterScheme = new BarterScheme()
        {
            Count = price,
            Template = ItemTpl.MONEY_ROUBLES
        };
        
        assort.Items.Add(assortEntry);
        assort.BarterScheme[assortId] = [[barterScheme]];
        assort.LoyalLevelItems[assortId] = 1;
    }
}
    
public class ModConfig
{
    public bool AllowShowFriendlyPlayerMarkersInRaid { get; set; }
    public bool AllowShowEnemyPlayerMarkersInRaid { get; set; }
    public bool AllowShowBossMarkersInRaid { get; set; }
    public bool AllowShowScavMarkersInRaid { get; set; }
}