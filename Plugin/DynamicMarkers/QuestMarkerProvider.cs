using System.Collections.Generic;
using DynamicMaps.Data;
using DynamicMaps.DynamicMarkers;
using DynamicMaps.UI.Components;
using DynamicMaps.Utils;
using EFT.Quests;

namespace DynamicMaps
{
    public class QuestMarkerProvider : IDynamicMarkerProvider
    {
        private readonly List<MapMarker> _questMarkers = [];
        private readonly HashSet<Quest> _subscribedQuests = [];
        private QuestController _questController;
        private MapView _map;

        public void OnShowInRaid(MapView map)
        {
            _map = map;
            if (GameUtils.IsScavRaid())
            {
                return;
            }

            SubscribeToQuests();
            RefreshMarkers();
        }

        public void OnHideInRaid(MapView map)
        {
            UnsubscribeFromQuests();
            TryRemoveMarkers();
            _map = null;
        }

        public void OnMapChanged(MapView map, MapDef mapDef)
        {
            if (!GameUtils.IsInRaid() || GameUtils.IsScavRaid())
            {
                return;
            }

            _map = map;
            SubscribeToQuests();
            RefreshMarkers();
        }

        public void OnRaidEnd(MapView map)
        {
            UnsubscribeFromQuests();
            QuestUtils.DiscardQuestData();
            TryRemoveMarkers();
            _map = null;
        }

        public void OnDisable(MapView map)
        {
            UnsubscribeFromQuests();
            QuestUtils.DiscardQuestData();
            TryRemoveMarkers();
            _map = null;
        }

        private void SubscribeToQuests()
        {
            var controller = GameUtils.GetMainPlayer().QuestController;
            if (!ReferenceEquals(_questController, controller))
            {
                UnsubscribeFromQuests();
                _questController = controller;
                
                if (_questController != null)
                {
                    _questController.OnNewQuestsAdded += OnNewQuest;
                }
            }

            if (_questController?.Quests == null)
                return;

            foreach (var quest in _questController.Quests)
            {
                SubscribeToQuest(quest);
            }
        }

        private void SubscribeToQuest(Quest quest)
        {
            if (quest == null || !_subscribedQuests.Add(quest))
                return;

            quest.OnConditionChanged += OnQuestConditionChanged;
            quest.OnStatusChanged += OnQuestStatusChanged;
        }

        private void UnsubscribeFromQuests()
        {
            if (_questController != null)
            {
                _questController.OnNewQuestsAdded -= OnNewQuest;
                _questController = null;
            }

            foreach (var quest in _subscribedQuests)
            {
                quest.OnConditionChanged -= OnQuestConditionChanged;
                quest.OnStatusChanged -= OnQuestStatusChanged;
            }
            _subscribedQuests.Clear();
        }

        private void OnNewQuest(Quest quest)
        {
            SubscribeToQuest(quest);
            RefreshMarkers();
        }

        private void OnQuestConditionChanged(Quest quest)
        {
            RefreshMarkers();
        }

        private void OnQuestStatusChanged(Quest quest, bool notify)
        {
            RefreshMarkers();
        }

        private void RefreshMarkers()
        {
            if (_map == null || !GameUtils.IsInRaid() || GameUtils.IsScavRaid())
                return;

            TryRemoveMarkers();
            AddQuestObjectiveMarkers(_map);
        }

        private void AddQuestObjectiveMarkers(MapView map)
        {
            QuestUtils.TryCaptureQuestData();

            var player = GameUtils.GetMainPlayer();
            if (player == null)
                return;

            var markerDefs = QuestUtils.GetMarkerDefsForPlayer(player);
            if (markerDefs == null)
                return;

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
            // do nothing
        }

        public void OnHideOutOfRaid(MapView map)
        {
            // do nothing
        }
    }
}
