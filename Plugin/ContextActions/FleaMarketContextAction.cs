using DynamicMaps.API;
using DynamicMaps.Config;
using DynamicMaps.Patches;
using DynamicMaps.Utils;
using EFT.UI;
using EFT.UI.Ragfair;

namespace DynamicMaps.ContextActions
{
    using MapMarker = UI.Components.MapMarker;

    internal static class FleaMarketContextAction
    {
        internal const string ActionId = "dynamicmaps.flea-market-search";

        internal static void Register()
        {
            MapMarkerContextMenu.RegisterAction(new MapMarkerContextAction(
                ActionId,
                GetLabel,
                Execute,
                IsAvailable,
                order: 100));
        }

        private static bool IsAvailable(MapMarker marker)
        {
            if (!Settings.EnableFleaMarketContextAction.Value
                || string.IsNullOrWhiteSpace(marker.AssociatedItemId)
                || GameUtils.IsInRaid())
            {
                return false;
            }

            return ItemUiContext.Instance?.Session?.RagFair != null;
        }

        private static string GetLabel(MapMarker marker)
        {
            var itemName = $"{marker.AssociatedItemId} ShortName".BsgLocalized();
            if (string.IsNullOrWhiteSpace(itemName) || itemName.EndsWith(" ShortName"))
            {
                return "Search on Flea Market";
            }

            return $"Search {itemName} on Flea Market";
        }

        private static void Execute(MapMarker marker)
        {
            if (Plugin.Instance.Map != null && Plugin.Instance.Map.IsShowingMapScreen)
            {
                InventoryScreenShowPatch.ReturnToMapOnNextShow();
            }

            // same as the "Filter by item" context menu entry on items
            ItemUiContext.Instance.ExternalRagfairSearch(new(EFilterType.FilterSearch, marker.AssociatedItemId, true));
        }
    }
}
