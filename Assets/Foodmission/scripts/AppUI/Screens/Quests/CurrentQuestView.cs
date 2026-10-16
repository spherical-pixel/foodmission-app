using System;
using System.Collections.Generic;
using System.Linq;

namespace eu.foodmission.platform
{
    /// <summary>What the quest UIs show for one item: done, failed (missions only) and a mission's partial progress.</summary>
    public struct QuestItemStatus
    {
        public bool Completed;
        public bool Failed;
        public float MissionPercent;
        /// <summary>Current-quest mission the user hasn't started (backend #424); never on older backends.</summary>
        public bool NotStarted;
    }

    /// <summary>Pure mapping from GET current-quest to what Home's quest card and the quest detail show.</summary>
    public static class CurrentQuestView
    {
        /// <summary>One flag per item in sortOrder: done or not; everything is done once the quest is completed.</summary>
        public static bool[] TimelineStates(CurrentQuest quest)
        {
            CurrentQuestItem[] items = SortedItems(quest);
            var states = new bool[items.Length];
            for (int i = 0; i < items.Length; i++)
            {
                states[i] = quest.completed || items[i].status == CurrentQuestItemStatus.Completed;
            }
            return states;
        }

        /// <summary>The quest as the detail screen consumes it; item titles become labels.</summary>
        public static Quest ToQuest(CurrentQuest current)
        {
            if (current == null)
            {
                return null;
            }
            return new Quest
            {
                id = current.questId,
                code = current.questCode,
                name = current.name,
                description = current.description,
                // GetDimension accepts a code as well as an id
                dimensionId = current.dimensionCode,
                level = current.level,
                items = SortedItems(current)
                    .Select(it => new QuestItem
                    {
                        contentType = it.contentType,
                        contentCode = it.contentCode,
                        label = it.title,
                        sortOrder = it.sortOrder,
                    })
                    .ToArray(),
            };
        }

        /// <summary>Status per quest item, looked up by content type and code.</summary>
        public static Func<QuestItem, QuestItemStatus> StatusLookup(CurrentQuest current)
        {
            var byKey = new Dictionary<string, CurrentQuestItem>(StringComparer.OrdinalIgnoreCase);
            foreach (CurrentQuestItem it in current?.items ?? Array.Empty<CurrentQuestItem>())
            {
                if (it != null)
                {
                    byKey[Key(it.contentType, it.contentCode)] = it;
                }
            }

            return item =>
            {
                if (item == null || !byKey.TryGetValue(Key(item.contentType, item.contentCode), out CurrentQuestItem it))
                {
                    return default;
                }
                bool isMission = string.Equals(it.contentType, QuestContentType.Mission, StringComparison.OrdinalIgnoreCase);
                return new QuestItemStatus
                {
                    Completed = it.status == CurrentQuestItemStatus.Completed,
                    // A wrong quiz answer stays "not done", as before; only missions show a failed state
                    Failed = isMission && it.status == CurrentQuestItemStatus.Failed,
                    MissionPercent = isMission ? it.progress : 0f,
                    NotStarted = isMission && IsNotStarted(it),
                };
            };
        }

        /// <summary>
        /// Missions of the quest left to (re)start — not started or failed — counted only while none is running
        /// (started, not completed or failed); 0 otherwise, for a completed quest, or on backends that don't report startedAt.
        /// </summary>
        public static int MissionsWaitingToStart(CurrentQuest quest)
        {
            if (quest == null || quest.completed)
            {
                return 0;
            }

            int waiting = 0;
            foreach (CurrentQuestItem it in SortedItems(quest))
            {
                if (!string.Equals(it.contentType, QuestContentType.Mission, StringComparison.OrdinalIgnoreCase) ||
                    it.status == CurrentQuestItemStatus.Completed)
                {
                    continue;
                }
                if (it.status == CurrentQuestItemStatus.Failed)
                {
                    // Only a restart brings it back
                    waiting++;
                    continue;
                }
                if (!IsNotStarted(it))
                {
                    // A mission is running: nothing to remind
                    return 0;
                }
                waiting++;
            }
            return waiting;
        }

        /// <summary>Same rule as MissionProgressState.IsStarted, for a current-quest item.</summary>
        private static bool IsNotStarted(CurrentQuestItem it)
        {
            return it.StartedAtReported && !it.startedAt.HasValue && it.progress <= 0f &&
                   it.status != CurrentQuestItemStatus.Completed &&
                   it.status != CurrentQuestItemStatus.Failed &&
                   it.status != CurrentQuestItemStatus.InProgress;
        }

        private static CurrentQuestItem[] SortedItems(CurrentQuest quest)
        {
            return (quest?.items ?? Array.Empty<CurrentQuestItem>()).Where(it => it != null).OrderBy(it => it.sortOrder).ToArray();
        }

        private static string Key(string contentType, string contentCode) => $"{contentType}|{contentCode}";
    }
}
