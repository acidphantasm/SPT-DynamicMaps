namespace DynamicMaps.Common
{

    public class ModConfig
    {
        public bool AllowShowPlayerMarker { get; set; } = true;
        public bool AllowShowFriendlyPlayerMarkersInRaid { get; set; } = true;
        public bool AllowShowEnemyPlayerMarkersInRaid { get; set; } = true;
        public bool AllowShowBossMarkersInRaid { get; set; } = true;
        public bool AllowShowScavMarkersInRaid { get; set; } = true;
        public bool AllowShowLockedDoorStatus { get; set; } = true;
        public bool AllowShowQuestsInRaid { get; set; } = true;
        public bool AllowShowExtractsInRaid { get; set; } = true;
        public bool AllowShowExtractStatusInRaid { get; set; } = true;
        public bool AllowShowTransitPointsInRaid { get; set; } = true;
        public bool AllowShowSecretExtractsInRaid { get; set; } = true;
        public bool AllowShowDroppedBackpackInRaid { get; set; } = true;
        public bool AllowShowWishlistedItemsInRaid { get; set; } = true;
        public bool AllowShowBTRInRaid { get; set; } = true;
        public bool AllowShowAirdropsInRaid { get; set; } = true;
        public bool AllowShowHiddenStashesInRaid { get; set; } = true;
        public bool AllowShowFriendlyCorpses { get; set; } = true;
        public bool AllowShowKilledCorpses { get; set; } = true;
        public bool AllowShowFriendlyKilledCorpses { get; set; } = true;
        public bool AllowShowBossCorpses { get; set; } = true;
        public bool AllowShowOtherCorpses { get; set; } = true;
        public bool AllowShowHeliCrashSiteInRaid { get; set; } = true;
        public bool AllowMiniMap { get; set; } = true;
        public bool RequireMapInInventory { get; set; } = false;
        public int ShowScavIntelLevel { get; set; } = 0;
        public int ShowPmcIntelLevel { get; set; } = 0;
        public int ShowBossIntelLevel { get; set; } = 0;
        public int ShowFriendlyIntelLevel { get; set; } = 0;
        public int ShowCorpseIntelLevel { get; set; } = 0;
        public int ShowWishListIntelLevel { get; set; } = 0;
        public int ShowHiddenStashIntelLevel { get; set; } = 0;
        public int ShowAirDropIntelLevel { get; set; } = 0;
    }
}