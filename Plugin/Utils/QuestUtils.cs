using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Comfort.Common;
using DynamicMaps.Data;
using EFT;
using EFT.Interactive;
using EFT.Quests;
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

        public static List<TriggerWithId> TriggersWithIds;
        public static List<LootItem> QuestItems;

        internal static void TryCaptureQuestData()
        {
            var gameWorld = Singleton<GameWorld>.Instance;

            if (TriggersWithIds == null)
            {
                TriggersWithIds = GameObject.FindObjectsOfType<TriggerWithId>().ToList();
            }

            if (QuestItems == null)
            {
                QuestItems = [];
                foreach (var lootItem in gameWorld.LootItems._iteration)
                {
                    if (lootItem?.Item != null && lootItem.Item.QuestItem)
                    {
                        QuestItems.Add(lootItem);
                    }
                }
            }
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

        internal static IEnumerable<MapMarkerDef> GetMarkerDefsForPlayer(Player player)
        {
            if (TriggersWithIds == null || QuestItems == null)
            {
                Plugin.Logger.LogWarning($"TriggersWithIds null: {TriggersWithIds == null} or QuestItems null: {QuestItems == null}");
                return null;
            }

            var markers = new List<MapMarkerDef>();

            var quests = GetIncompleteQuests(player);
            foreach (var quest in quests)
            {
                markers.AddRange(GetMarkerDefsForQuest(player, quest));
            }

            return markers;
        }

        internal static IEnumerable<MapMarkerDef> GetMarkerDefsForQuest(Player player, Quest quest)
        {
            var markers = new List<MapMarkerDef>();

            var conditions = GetIncompleteQuestConditions(player, quest);
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
            // TryCast checks the il2cpp class hierarchy, so it works however the condition wrapper was created
            IEnumerable<string> zoneIds = null;
            if (condition.TryCast<ConditionZone>() is { } zoneCondition)
            {
                zoneIds = zoneCondition.zoneIds;
            }
            else if (condition.TryCast<ConditionLaunchFlare>() is { } flareCondition)
            {
                zoneIds = [flareCondition.zoneID];
            }
            else if (condition.TryCast<ConditionVisitPlace>() is { } place)
            {
                zoneIds = [place.target];
            }
            else if (condition.TryCast<ConditionInZone>() is { } zone)
            {
                zoneIds = zone.zoneIds;
            }
            else if (condition.TryCast<ConditionFindItem>() is { } findItemCondition)
            {
                foreach (var position in GetPositionsForQuestItems(findItemCondition.target, questName, conditionDescription))
                {
                    yield return position;
                }
            }
            else if (condition.TryCast<ConditionExitName>() is { } exitCondition)
            {
                var exfils = Singleton<GameWorld>.Instance.ExfiltrationController.ExfiltrationPoints;
                var specifiedExit = exfils.FirstOrDefault(e => e.Settings.Name == exitCondition.exitName);

                if (specifiedExit != null)
                {
                    yield return MathUtils.ConvertToMapPosition(specifiedExit.transform);
                }
            }
            else if (condition.TryCast<ConditionCounterCreator>() is { } conditionCreator)
            {
                // this will recurse back into this method
                foreach (var position in GetPositionsForConditionCreator(conditionCreator, questName, conditionDescription))
                {
                    yield return position;
                }
            }

            if (zoneIds == null)
            {
                yield break;
            }

            foreach (var zoneId in zoneIds)
            {
                foreach (var position in GetPositionsForZoneId(zoneId, questName, conditionDescription))
                {
                    yield return position;
                }
            }
        }

        private static IEnumerable<Vector3> GetPositionsForConditionCreator(ConditionCounterCreator conditionCreator,
                                                                            string questName, string conditionDescription)
        {
            if (conditionCreator.Conditions == null)
            {
                yield break;
            }

            foreach (var condition in conditionCreator.Conditions.ToManaged())
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
            if (TriggersWithIds == null)
            {
                yield break;
            }

            foreach (var zone in TriggersWithIds.GetZoneTriggers(zoneId))
            {
                yield return MathUtils.ConvertToMapPosition(zone.transform.position);
            }
        }

        private static IEnumerable<Vector3> GetPositionsForQuestItems(IEnumerable<string> questItemIds, string questName,
                                                                      string conditionDescription)
        {
            foreach (var questItemId in questItemIds)
            {
                if (QuestItems == null)
                {
                    yield break;
                }

                foreach (var item in QuestItems.Where(i => i.TemplateId == questItemId))
                {
                    yield return MathUtils.ConvertToMapPosition(item.transform.position);
                }
            }
        }

        private static IEnumerable<Condition> GetIncompleteQuestConditions(Player player, Quest quest)
        {
            // TODO: Template.Conditions is a GClass reference
            if (quest?.Template?.Conditions == null)
            {
                Plugin.Logger.LogError($"GetIncompleteQuestConditions: quest.Template.Conditions is null, skipping quest");
                yield break;
            }

            // TODO: conditions is a GClass reference
            if (!quest.Template.Conditions.TryGetValue(EQuestStatus.AvailableForFinish, out var conditions) || conditions == null)
            {
                Plugin.Logger.LogError($"Quest {quest.Template.NameLocaleKey.BSGLocalized()} doesn't have conditions marked AvailableForFinish, skipping it");
                yield break;
            }

            foreach (var condition in conditions.ToManaged())
            {
                if (condition == null)
                {
                    Plugin.Logger.LogWarning($"Quest {quest.Template.NameLocaleKey.BSGLocalized()} has null condition, skipping it");
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

        private static IEnumerable<Quest> GetIncompleteQuests(Player player)
        {
            var questController = player.QuestController;
            if (questController == null)
            {
                Plugin.Logger.LogError($"Not able to get quests for player: {player.Id}, questController is null");
                yield break;
            }

            var quests = questController.ConditionalBook;
            if (quests == null)
            {
                Plugin.Logger.LogError($"Not able to get quests for player: {player.Id}, quests is null");
                yield break;
            }

            var questsList = quests.List;
            if (questsList == null)
            {
                Plugin.Logger.LogError($"Not able to get quests for player: {player.Id}, questsList is null");
                yield break;
            }

            foreach (var quest in questsList)
            {
                if (quest?.Template?.Conditions == null)
                {
                    continue;
                }

                if (quest.QuestStatus != EQuestStatus.Started)
                {
                    continue;
                }

                yield return quest;
            }
        }

        private static bool IsConditionCompleted(Player player, Quest questData, Condition condition)
        {
            // CompletedConditions is inaccurate (it doesn't reset when some quests do on death)
            // and also does not contain optional objectives, need to recheck if something is in there
            if (condition.IsNecessary && !questData.CompletedConditions.Contains(condition))
            {
                return false;
            }

            var questController = player.QuestController;
            if (questController == null)
            {
                return false;
            }

            var quests = questController.ConditionalBook;
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

        private static IEnumerable<TriggerWithId> GetZoneTriggers(this IEnumerable<TriggerWithId> triggerWithIds, string zoneId)
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
