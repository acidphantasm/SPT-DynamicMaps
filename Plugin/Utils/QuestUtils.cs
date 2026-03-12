using System.Collections.Generic;
using System.Linq;
using Comfort.Common;
using DynamicMaps.Common;
using DynamicMaps.Data;
using EFT;
using EFT.Interactive;
using EFT.Quests;
using HarmonyLib;
using UnityEngine;

namespace DynamicMaps.Utils
{
    // NOTE: Most of this is adapted from work done for Prop's GTFO mod (https://github.com/dvize/GTFO)
    // this likely does not count as a "substantial portion" of the software
    // under MIT license (https://github.com/dvize/GTFO/blob/master/LICENSE.txt)
    public static class QuestUtils
    {

        // TODO: move to config
        private const string _questCategory = "Quest";
        private const string _questImagePath = "Markers/quest.png";
        private static Vector2 _questPivot = new Vector2(0.5f, 0f);
        private static Color _questColor = Color.green;
        //

        private static List<TriggerWithIdAbstraction> TriggersWithIds;
        private static List<LootItemAbstraction> QuestItems;

        internal class LootItemAbstraction
        {
            public string ItemId { get; set; }
            public Vector3 Position { get; set; }
        }

        internal static void TryCaptureQuestData(MapDef def)
        {
            var gameWorld = Singleton<GameWorld>.Instance;

            if (TriggersWithIds == null)
            {
                if (def.TriggersWithId.Any())
                {
                    TriggersWithIds = [.. def.TriggersWithId];
                }
                else
                {
                    TriggersWithIds = [.. GameObject.FindObjectsOfType<TriggerWithId>().Select(k => new TriggerWithIdAbstraction()
                    {
                        Id = k.Id,
                        Position = k.transform.position,
                        Bounds = k.GetComponent<BoxCollider>().bounds
                    })];
                }
            }

            QuestItems ??= [.. Traverse.Create(gameWorld)
                    .Field("LootItems")
                    .Field("List_0")
                    .GetValue<List<LootItem>>()
                    .Where(i => i.Item.QuestItem).Select(k => new LootItemAbstraction()
                    {
                        ItemId = k.TemplateId,
                        Position = k.transform.position
                    })];
        }

        internal static void FillQuestDataOutOfRaid(List<ConditionData> data, MapDef def)
        {
            QuestItems ??= [.. data.Select(d => new LootItemAbstraction()
                {
                    ItemId = d.ItemId,
                    Position = new Vector3(d.SpawnPoint[0], d.SpawnPoint[1], d.SpawnPoint[2])
                })];

            TriggersWithIds ??= [.. def.TriggersWithId];
        }

        internal static void DiscardQuestData()
        {
            if (TriggersWithIds != null)
            {
                TriggersWithIds.Clear();
                TriggersWithIds = null;
            }

            if (QuestItems != null)
            {
                QuestItems.Clear();
                QuestItems = null;
            }
        }

        internal static IEnumerable<MapMarkerDef> GetMarkerDefsForPlayer(AbstractQuestControllerClass questController)
        {
            if (TriggersWithIds == null || QuestItems == null || questController == null)
            {
                Plugin.Log.LogWarning($"TriggersWithIds null: {TriggersWithIds == null} or QuestItems null: {QuestItems == null} or Player null: {questController == null}");
                return null;
            }

            var markers = new List<MapMarkerDef>();

            var quests = GetIncompleteQuests(questController);
            foreach (var quest in quests)
            {
                markers.AddRange(GetMarkerDefsForQuest(questController, quest));
            }

            return markers;
        }

        internal static IEnumerable<MapMarkerDef> GetMarkerDefsForQuest(AbstractQuestControllerClass questController, QuestDataClass quest)
        {
            var markers = new List<MapMarkerDef>();

            var conditions = GetIncompleteQuestConditions(questController, quest);
            foreach (var condition in conditions)
            {
                var questName = quest.Template.NameLocaleKey.BSGLocalized();
                var conditionDescription = condition.id.BSGLocalized();

                var positions = GetPositionsForCondition(condition, questName, conditionDescription);
                foreach (var position in positions)
                {
                    var isDuplicate = false;

                    // check against previously created markers for duplicate position
                    foreach (var marker in markers)
                    {
                        if (MathUtils.ApproxEquals(marker.Position.x, position.x)
                         && MathUtils.ApproxEquals(marker.Position.y, position.y)
                         && MathUtils.ApproxEquals(marker.Position.z, position.z))
                        {
                            isDuplicate = true;
                            break;
                        }
                    }

                    if (isDuplicate)
                    {
                        continue;
                    }

                    markers.Add(CreateQuestMapMarkerDef(position, questName, conditionDescription));
                }
            }

            return markers;
        }

        private static IEnumerable<Vector3> GetPositionsForCondition(Condition condition, string questName,
                                                                    string conditionDescription)
        {
            switch (condition)
            {
                case ConditionZone zoneCondition:
                    {
                        foreach (var position in GetPositionsForZoneId(zoneCondition.zoneId, questName, conditionDescription))
                        {
                            yield return position;
                        }
                        break;
                    }
                case ConditionLaunchFlare flareCondition:
                    {
                        foreach (var position in GetPositionsForZoneId(flareCondition.zoneID, questName, conditionDescription))
                        {
                            yield return position;
                        }
                        break;
                    }
                case ConditionVisitPlace place:
                    {
                        foreach (var position in GetPositionsForZoneId(place.target, questName, conditionDescription))
                        {
                            yield return position;
                        }
                        break;
                    }
                case ConditionInZone zone:
                    {
                        foreach (var zoneId in zone.zoneIds)
                        {
                            foreach (var position in GetPositionsForZoneId(zoneId, questName, conditionDescription))
                            {
                                yield return position;
                            }
                        }
                        break;
                    }
                case ConditionFindItem findItemCondition:
                    {
                        foreach (var position in GetPositionsForQuestItems(findItemCondition.target, questName, conditionDescription))
                        {
                            yield return position;
                        }
                        break;
                    }
                case ConditionExitName exitCondition:
                    {
                        var gameWorld = Singleton<GameWorld>.Instance;
                        if (gameWorld == null || gameWorld?.ExfiltrationController == null)
                        {
                            break;
                        }
                        else
                        {
                            var exfils = gameWorld.ExfiltrationController?.ExfiltrationPoints ?? [];
                            var specifiedExit = exfils.FirstOrDefault(e => e.Settings.Name == exitCondition.exitName);

                            if (specifiedExit != null)
                            {
                                yield return MathUtils.ConvertToMapPosition(specifiedExit.transform);
                            }

                            break;
                        }
                    }
                case ConditionCounterCreator conditionCreator:
                    {
                        // this will recurse back into this method
                        foreach (var position in GetPositionsForConditionCreator(conditionCreator, questName, conditionDescription))
                        {
                            yield return position;
                        }
                        break;
                    }
                default:
                    {
                        break;
                    }
            }
        }

        private static IEnumerable<Vector3> GetPositionsForConditionCreator(ConditionCounterCreator conditionCreator,
                                                                            string questName, string conditionDescription)
        {
            var counter = conditionCreator.TemplateConditions;
            foreach (var condition in counter.Conditions)
            {
                foreach (var position in GetPositionsForCondition(condition, questName, conditionDescription))
                {
                    yield return position;
                }
            }
        }

        private static IEnumerable<Vector3> GetPositionsForZoneId(string zoneId, string questName,
                                                                  string conditionDescription)
        {
            var zones = TriggersWithIds?.GetZoneTriggers(zoneId);
            foreach (var zone in zones)
            {
                yield return MathUtils.ConvertToMapPosition(zone.Position);
            }
        }

        private static IEnumerable<Vector3> GetPositionsForQuestItems(IEnumerable<string> questItemIds, string questName,
                                                                      string conditionDescription)
        {
            foreach (var questItemId in questItemIds)
            {
                var questItems = QuestItems?.Where(i => i.ItemId == questItemId);
                foreach (var item in questItems)
                {
                    yield return MathUtils.ConvertToMapPosition(item.Position);
                }
            }
        }

        private static IEnumerable<Condition> GetIncompleteQuestConditions(AbstractQuestControllerClass player, QuestDataClass quest)
        {
            // TODO: Template.Conditions is a GClass reference
            if (quest?.Template?.Conditions == null)
            {
                Plugin.Log.LogError($"GetIncompleteQuestConditions: quest.Template.Conditions is null, skipping quest");
                yield break;
            }

            // TODO: conditions is a GClass reference
            if (!quest.Template.Conditions.TryGetValue(EQuestStatus.AvailableForFinish, out var conditions) || conditions == null)
            {
                Plugin.Log.LogError($"Quest {quest.Template.NameLocaleKey.BSGLocalized()} doesn't have conditions marked AvailableForFinish, skipping it");
                yield break;
            }

            foreach (var condition in conditions)
            {
                if (condition == null)
                {
                    Plugin.Log.LogWarning($"Quest {quest.Template.NameLocaleKey.BSGLocalized()} has null condition, skipping it");
                    continue;
                }

                // filter out completed conditions
                if (IsConditionCompleted(player, quest, condition))
                {
                    continue;
                }

                yield return condition;
            }
        }

        private static IEnumerable<QuestDataClass> GetIncompleteQuests(AbstractQuestControllerClass questController)
        {
            var quests = questController.Quests;
            if (quests == null)
            {
                Plugin.Log.LogError($"Not able to get quests for player, quests is null");
                yield break;
            }

            var questsList = quests.List_1;
            if (questsList == null)
            {
                Plugin.Log.LogError($"Not able to get quests for player, questsList is null");
                yield break;
            }

            foreach (var quest in questsList)
            {
                if (quest?.Template?.Conditions == null)
                {
                    continue;
                }

                if (quest.Status != EQuestStatus.Started)
                {
                    continue;
                }

                yield return quest;
            }
        }

        private static bool IsConditionCompleted(AbstractQuestControllerClass questController, QuestDataClass questData, Condition condition)
        {
            // CompletedConditions is inaccurate (it doesn't reset when some quests do on death)
            // and also does not contain optional objectives, need to recheck if something is in there
            if (condition.IsNecessary && !questData.CompletedConditions.Contains(condition.id))
            {
                return false;
            }

            var quests = questController.Quests;
            if (quests == null)
            {
                return false;
            }

            var quest = quests.GetConditional(questData.Id);
            if (quest == null)
            {
                return false;
            }

            return quest.IsConditionDone(condition);
        }

        private static IEnumerable<TriggerWithIdAbstraction> GetZoneTriggers(this IEnumerable<TriggerWithIdAbstraction> triggerWithIds, string zoneId)
        {
            return triggerWithIds.Where(t => t.Id == zoneId);
        }

        private static MapMarkerDef CreateQuestMapMarkerDef(Vector3 position, string questName, string conditionDescription)
        {
            return new MapMarkerDef
            {
                Category = _questCategory,
                Color = _questColor,
                ImagePath = _questImagePath,
                Position = position,
                Pivot = _questPivot,
                Text = questName
            };
        }
    }
}
