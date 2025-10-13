﻿using System.Text.Json;
 using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Utils;

namespace _dynamicMapsServer;

[Injectable]
public class CustomStaticRouter : StaticRouter
{
    private static ModConfig? _modConfig;

    public CustomStaticRouter(
        JsonUtil jsonUtil) : base(
        jsonUtil,
        GetCustomRoutes()
    )
    {}

    public void PassConfig(ModConfig config)
    {
        _modConfig = config;
    }

    private static List<RouteAction> GetCustomRoutes()
    {
        return
        [
            new RouteAction<EmptyRequestData>(
                "/dynamicmaps/load",
                async (
                    url,
                    info,
                    sessionId,
                    output
                ) => await HandleRoute(url, info, sessionId)
            )
        ];
    }

    private static ValueTask<string> HandleRoute(string url, EmptyRequestData info, MongoId sessionId)
    {
        return new ValueTask<string>(JsonSerializer.Serialize(_modConfig));
    }
}