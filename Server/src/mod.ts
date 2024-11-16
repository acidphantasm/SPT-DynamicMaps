import { DependencyContainer } from "tsyringe";

import { IPostDBLoadMod } from "@spt/models/external/IPostDBLoadMod";
import { DatabaseServer } from "@spt/servers/DatabaseServer";
import { IDatabaseTables } from "@spt/models/spt/server/IDatabaseTables";
import { ItemHelper } from "@spt/helpers/ItemHelper";
import { BaseClasses } from "@spt/models/enums/BaseClasses";

class Mod implements IPostDBLoadMod
{
    public postDBLoad(container: DependencyContainer): void
    {
        // get database from server
        const databaseServer = container.resolve<DatabaseServer>("DatabaseServer");

        // Get all the in-memory json found in /assets/database
        const tables: IDatabaseTables = databaseServer.getTables();

        // ---------------------------------------------------------
        // example #1
        // Make the LEDX item sellable on flea market

        // Find the ledx item by its Id
        const ledx = tables.templates.items["5c0530ee86f774697952d952"];

        // Update one of its properties to be true
        ledx._props.CanSellOnRagfair = true;

        // ---------------------------------------------------------
        // example #2
        // Get globals settings and set flea market min level to be 1
        tables.globals.config.RagFair.minUserLevel = 1;

        // ---------------------------------------------------------
        // Example #3
        // Loop over all magazines and make them weigh nothing

        // Get ItemHelper ready to use
        const itemHelper: ItemHelper = container.resolve<ItemHelper>("ItemHelper");

        // Get all items in the database as an array so we can loop over them later
        // tables.templates.items is a dictionary, the key being the items template id, the value being the objects data,
        // we want to convert it into an array so we can loop over all the items easily
        // Object.values lets us grab the 'value' part as an array and ignore the 'key' part
        const items = Object.values(tables.templates.items);

        // Use the itemHelper class to assist us in getting only magazines
        // We are filtering all items to only those with a base class of MAGAZINE (5448bc234bdc2d3c308b4569)
        const magazines = items.filter(x => itemHelper.isOfBaseclass(x._id, BaseClasses.MAGAZINE));

        // Loop over all the magazines the above code found
        for (const magazine of magazines)
        {
            // Check the magazine has a weight property before we edit it
            if (magazine._props.Weight)
            {
                // Set its weight to 0
                magazine._props.Weight = 0;
            }
        }
    }
}

export const mod = new Mod();
