using System;
using System.Collections.Generic;
using System.Linq;
using DynamicMaps.Common;
using DynamicMaps.Data;
using DynamicMaps.DynamicMarkers;
using DynamicMaps.UI.Components;
using DynamicMaps.Utils;
using EFT;
using Newtonsoft.Json;
using SPT.Common.Http;

namespace DynamicMaps
{
    public class QuestMarkerProvider : IDynamicMarkerProvider
    {
        private List<MapMarker> _questMarkers = [];

        public void OnShowInRaid(MapView map)
        {
            if (GameUtils.IsScavRaid())
            {
                return;
            }

            AddQuestObjectiveMarkers(map);
        }

        public void OnHideInRaid(MapView map)
        {
            // TODO: don't just be lazy and try to update markers
            TryRemoveMarkers();
        }

        public void OnMapChanged(MapView map, MapDef mapDef)
        {
            TryRemoveMarkers();

            if (!GameUtils.IsInRaid())
            {
                OnShowOutOfRaid(map);
            }
            else
            {
                AddQuestObjectiveMarkers(map);
            }
        }

        public void OnRaidEnd(MapView map)
        {
            QuestUtils.DiscardQuestData();
            TryRemoveMarkers();
        }

        public void OnDisable(MapView map)
        {
            TryRemoveMarkers();
        }

        private void AddQuestObjectiveMarkers(MapView map)
        {
            QuestUtils.TryCaptureQuestData(map.CurrentMapDef);

            var player = GameUtils.GetMainPlayer();

            var markerDefs = QuestUtils.GetMarkerDefsForPlayer(player.AbstractQuestControllerClass);
            foreach (var markerDef in markerDefs)
            {
                var marker = map.AddMapMarker(markerDef);
                _questMarkers.Add(marker);
            }
        }

        private void TryRemoveMarkers()
        {
            foreach (var marker in _questMarkers)
            {
                marker.ContainingMapView.RemoveMapMarker(marker);
            }
            _questMarkers.Clear();
        }

        public void OnShowOutOfRaid(MapView map)
        {
            OnHideOutOfRaid(map);

            var possibleInternalNames = map.CurrentMapDef.MapInternalNames;
            var questData = JsonConvert.DeserializeObject<List<ConditionData>>(RequestHandler.GetJson(Routes.GetQuestItemsForMap));
            var data = questData.Where(p => possibleInternalNames.Contains(p.MapName, StringComparer.OrdinalIgnoreCase)).ToList();

            QuestUtils.FillQuestDataOutOfRaid(data, map.CurrentMapDef);

            var questController = ((MainMenuControllerClass)typeof(TarkovApplication)
                .GetField("mainMenuControllerClass", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .GetValue(SPT.Reflection.Utils.ClientAppUtils.GetMainApp()))
                .QuestController;
            var markerDefs = QuestUtils.GetMarkerDefsForPlayer(questController);
            foreach (var markerDef in markerDefs)
            {
                var marker = map.AddMapMarker(markerDef);
                _questMarkers.Add(marker);
            }
        }

        public void OnHideOutOfRaid(MapView map)
        {
            TryRemoveMarkers();
            QuestUtils.DiscardQuestData();
        }
    }
}
