import type { DependencyContainer } from "tsyringe";
import type { IPostDBLoadMod } from "@spt/models/external/IPostDBLoadMod";
import type { IPreSptLoadMod } from "@spt/models/external/IPreSptLoadMod";
import type { Item } from "@spt/models/eft/common/tables/IItem";
import type { IBarterScheme } from "@spt/models/eft/common/tables/ITrader";
import type { NewItemFromCloneDetails } from "@spt/models/spt/mod/NewItemDetails";
import { Traders } from "@spt/models/enums/Traders";
import { InstanceManager } from "./InstanceManager";

class DynamicMaps implements IPreSptLoadMod, IPostDBLoadMod
{
    // eslint-disable-next-line @typescript-eslint/naming-convention
    protected InstanceManager: InstanceManager = new InstanceManager();

    public preSptLoad(container: DependencyContainer): void
    {
        this.InstanceManager.preSptLoad(container);
    }

    public postDBLoad(container: DependencyContainer): void
    {
        this.InstanceManager.postDBLoad(container);

        this.createNewMaps();
    }

    private createNewMaps(): void
    {
        this.createGroundZeroMap();
        this.createStreetsMap();
        this.createReserveMap();
        this.createLabsMap();
        this.createLighthouseMap();
    }

    private createGroundZeroMap(): void
    {
        const gzMap: NewItemFromCloneDetails = 
        {
            itemTplToClone: "5900b89686f7744e704a8747",
            parentId: "567849dd4bdc2d150f8b456e",
            newId: "6738033eb7305d3bdafe9518",
            fleaPriceRoubles: 25000,
            handbookPriceRoubles: 32500,
            handbookParentId: "5b47574386f77428ca22b343",
            overrideProperties: {

            },
            locales: {
                en: {
                    name: "Ground Zero plan map",
                    shortName: "Ground Zero",
                    description: ""
                }
            }
        }

        const assortId = "6738076415fd9232e8dae982";

        this.InstanceManager.customItemService.createItemFromClone(gzMap);
        this.pushToTraderAssort(Traders.THERAPIST, gzMap.newId, gzMap.handbookPriceRoubles, assortId);
    }

    private createStreetsMap(): void
    {
        const streetsMap: NewItemFromCloneDetails = 
        {
            itemTplToClone: "5900b89686f7744e704a8747",
            parentId: "567849dd4bdc2d150f8b456e",
            newId: "673803448cb3819668d77b1b",
            fleaPriceRoubles: 25000,
            handbookPriceRoubles: 32500,
            handbookParentId: "5b47574386f77428ca22b343",
            overrideProperties: {

            },
            locales: {
                en: {
                    name: "Streets of Tarkov plan",
                    shortName: "Streets",
                    description: ""
                }
            }
        }

        const assortId = "67380769ebda082cf01c3fd7";

        this.InstanceManager.customItemService.createItemFromClone(streetsMap);
        this.pushToTraderAssort(Traders.THERAPIST, streetsMap.newId, streetsMap.handbookPriceRoubles, assortId);
    }

    private createReserveMap(): void
    {
        const reserveMap: NewItemFromCloneDetails = 
        {
            itemTplToClone: "5900b89686f7744e704a8747",
            parentId: "567849dd4bdc2d150f8b456e",
            newId: "6738034a9713b5f42b4a8b78",
            fleaPriceRoubles: 25000,
            handbookPriceRoubles: 32500,
            handbookParentId: "5b47574386f77428ca22b343",
            overrideProperties: {

            },
            locales: {
                en: {
                    name: "Reserve plan",
                    shortName: "Reserve",
                    description: ""
                }
            }
        }

        const assortId = "6738076e704fef20a1a580e6";

        this.InstanceManager.customItemService.createItemFromClone(reserveMap);
        this.pushToTraderAssort(Traders.THERAPIST, reserveMap.newId, reserveMap.handbookPriceRoubles, assortId);
    }

    private createLabsMap(): void
    {
        const labsMap: NewItemFromCloneDetails = 
        {
            itemTplToClone: "5900b89686f7744e704a8747",
            parentId: "567849dd4bdc2d150f8b456e",
            newId: "6738034e9d22459ad7cd1b81",
            fleaPriceRoubles: 25000,
            handbookPriceRoubles: 32500,
            handbookParentId: "5b47574386f77428ca22b343",
            overrideProperties: {

            },
            locales: {
                en: {
                    name: "Labs plan",
                    shortName: "Labs",
                    description: ""
                }
            }
        }

        const assortId = "673807742ef49729b9dd1b0a";

        this.InstanceManager.customItemService.createItemFromClone(labsMap);
        this.pushToTraderAssort(Traders.THERAPIST, labsMap.newId, labsMap.handbookPriceRoubles, assortId);
    }

    private createLighthouseMap(): void
    {
        const lighthouseMap: NewItemFromCloneDetails = 
        {
            itemTplToClone: "5900b89686f7744e704a8747",
            parentId: "567849dd4bdc2d150f8b456e",
            newId: "6738035350b24a4ae4a57997",
            fleaPriceRoubles: 25000,
            handbookPriceRoubles: 32500,
            handbookParentId: "5b47574386f77428ca22b343",
            overrideProperties: {

            },
            locales: {
                en: {
                    name: "Lighthouse plan",
                    shortName: "Lighthouse",
                    description: ""
                }
            }
        }

        const assortId = "6738077be5a03fda63c9917d";

        this.InstanceManager.customItemService.createItemFromClone(lighthouseMap);
        this.pushToTraderAssort(Traders.THERAPIST, lighthouseMap.newId, lighthouseMap.handbookPriceRoubles, assortId);
    }

    private pushToTraderAssort(traderId: Traders, itemId: string, price: number, assortId: string): void
    {
        const assort = this.InstanceManager.database.traders[traderId].assort;

        const item: Item = {
            _id: assortId,
            _tpl: itemId,
            parentId: "hideout",
            slotId: "hideout",
            upd: {
                UnlimitedCount: false,
                StackObjectsCount: 4,
                BuyRestrictionMax: 10,
                BuyRestrictionCurrent: 0
            }
        }

        const scheme: IBarterScheme[][] = [
            [
                {
                    count: price,
                    _tpl: "5449016a4bdc2d6f028b456f"
                }
            ]
        ];

        assort.items.push(item);
        assort.barter_scheme[assortId] = scheme;
        assort.loyal_level_items[assortId] = 1;
    }
}

export const mod = new DynamicMaps();
