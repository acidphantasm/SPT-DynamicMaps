import type { ILogger } from "@spt/models/spt/utils/ILogger";
import type { DatabaseServer } from "@spt/servers/DatabaseServer";
import type { IDatabaseTables } from "@spt/models/spt/server/IDatabaseTables";
import type { StaticRouterModService } from "@spt/services/mod/staticRouter/StaticRouterModService";
import type { DependencyContainer } from "tsyringe";
import type { CustomItemService } from "@spt/services/mod/CustomItemService";
import type { ImageRouter } from "@spt/routers/ImageRouter";
import type { PreSptModLoader } from "@spt/loaders/PreSptModLoader";
import type { ConfigServer } from "@spt/servers/ConfigServer";
import type { JsonUtil } from "@spt/utils/JsonUtil";
import type { ProfileHelper } from "@spt/helpers/ProfileHelper";
import type { RagfairPriceService } from "@spt/services/RagfairPriceService";
import type { ImporterUtil } from "@spt/utils/ImporterUtil";
import type { SaveServer } from "@spt/servers/SaveServer";
import type { ItemHelper } from "@spt/helpers/ItemHelper";
import type { MailSendService } from "@spt/services/MailSendService";
import type { HashUtil } from "@spt/utils/HashUtil";
import type { TraderHelper } from "@spt/helpers/TraderHelper";
import type { HttpResponseUtil } from "@spt/utils/HttpResponseUtil";
import type { FileSystemSync } from "@spt/utils/FileSystemSync";

export class InstanceManager 
{
    //#region Accessible in or after preAkiLoad
    private alpha = false;
    private version = "";

    // Instances
    public container: DependencyContainer;
    public preSptModLoader: PreSptModLoader;
    public configServer: ConfigServer;
    public saveServer: SaveServer;
    public itemHelper: ItemHelper;
    public logger: ILogger;
    public staticRouter: StaticRouterModService;
    public fs: FileSystemSync;
    public hashUtil: HashUtil;
    public httpResponseUtil: HttpResponseUtil;
    //#endregion

    //#region Acceessible in or after postDBLoad
    public database: IDatabaseTables;
    public customItem: CustomItemService;
    public imageRouter: ImageRouter;
    public jsonUtil: JsonUtil;
    public profileHelper: ProfileHelper;
    public ragfairPriceService: RagfairPriceService;
    public importerUtil: ImporterUtil;
    public customItemService: CustomItemService;
    public mailSendService: MailSendService;
    public traderHelper: TraderHelper;
    //#endregion

    // Call at the start of the mods postDBLoad method
    public preSptLoad(container: DependencyContainer): void
    {
        this.container = container;
        this.preSptModLoader = container.resolve<PreSptModLoader>("PreSptModLoader");
        this.imageRouter = container.resolve<ImageRouter>("ImageRouter");
        this.configServer = container.resolve<ConfigServer>("ConfigServer");
        this.saveServer = container.resolve<SaveServer>("SaveServer");
        this.itemHelper = container.resolve<ItemHelper>("ItemHelper");
        this.logger = container.resolve<ILogger>("WinstonLogger");
        this.staticRouter = container.resolve<StaticRouterModService>("StaticRouterModService");
        this.fs = container.resolve<FileSystemSync>("FileSystemSync");
        this.hashUtil = container.resolve<HashUtil>("HashUtil");
        this.httpResponseUtil = container.resolve<HttpResponseUtil>("HttpResponseUtil");
    }

    public postDBLoad(container: DependencyContainer): void
    {
        this.database = container.resolve<DatabaseServer>("DatabaseServer").getTables();
        this.customItem = container.resolve<CustomItemService>("CustomItemService");
        this.jsonUtil = container.resolve<JsonUtil>("JsonUtil");
        this.profileHelper = container.resolve<ProfileHelper>("ProfileHelper");
        this.ragfairPriceService = container.resolve<RagfairPriceService>("RagfairPriceService");
        this.importerUtil = container.resolve<ImporterUtil>("ImporterUtil");
        this.customItemService = container.resolve<CustomItemService>("CustomItemService");
        this.mailSendService = container.resolve<MailSendService>("MailSendService");
        this.traderHelper = container.resolve<TraderHelper>("TraderHelper");
    }
}