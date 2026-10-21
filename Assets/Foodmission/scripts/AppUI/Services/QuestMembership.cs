using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Unity.AppUI.Navigation;

using UnityEngine;

namespace eu.foodmission.platform
{
    /// <summary>Where a mission/challenge lives: whether it belongs to the current quest, and which quest to send the user to.</summary>
    public sealed class QuestMembership
    {
        public static readonly QuestMembership Unknown = new QuestMembership(null, null);

        /// <summary>null when it couldn't be checked (no service, or the current quest failed to load).</summary>
        public bool? InCurrentQuest { get; }

        /// <summary>The current quest when it contains the item; otherwise the first quest that does (available ones first). Null when none.</summary>
        public Quest Quest { get; }

        public QuestMembership(bool? inCurrentQuest, Quest quest)
        {
            InCurrentQuest = inCurrentQuest;
            Quest = quest;
        }

        /// <summary>Arguments for <c>Actions.open_quest</c>, same as the quest list and Home send.</summary>
        public static Argument[] OpenQuestArguments(Quest quest)
        {
            var args = new List<Argument>();
            if (!string.IsNullOrEmpty(quest?.code))
            {
                args.Add(new Argument("code", quest.code));
            }
            if (!string.IsNullOrEmpty(quest?.id))
            {
                args.Add(new Argument("id", quest.id));
            }
            return args.ToArray();
        }

        public static bool Contains(Quest quest, string contentType, string contentCode)
        {
            return quest?.items?.Any(i =>
                i != null &&
                string.Equals(i.contentType, contentType, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(i.contentCode, contentCode, StringComparison.OrdinalIgnoreCase)) == true;
        }

        /// <summary>
        /// GET /quests has no items, so only the quests of the item's dimension and level (when known) are loaded one by
        /// one (GetQuestAsync caches them).
        /// </summary>
        public static async Task<QuestMembership> FindAsync(IQuestService questService, string currentQuestId, string contentType, string contentCode,
            string dimensionId = null, string level = null)
        {
            if (questService == null || string.IsNullOrEmpty(contentCode))
            {
                return Unknown;
            }

            bool? inCurrent = false;
            if (!string.IsNullOrEmpty(currentQuestId))
            {
                var (current, error) = await questService.GetQuestAsync(currentQuestId);
                if (Contains(current, contentType, contentCode))
                {
                    return new QuestMembership(true, current);
                }
                if (error != null || current == null)
                {
                    inCurrent = null;
                }
            }

            Quest containing = null;
            try
            {
                var (quests, _) = await questService.GetQuestsAsync();
                var candidates = (quests ?? Array.Empty<Quest>())
                    .Where(q => q != null && q.id != currentQuestId)
                    .Where(q => SameOrUnknown(q.dimensionId, dimensionId) && SameOrUnknown(q.level, level))
                    .OrderByDescending(q => q.available);
                foreach (Quest candidate in candidates)
                {
                    Quest full = candidate;
                    if (full.items == null)
                    {
                        var (detail, _) = await questService.GetQuestAsync(!string.IsNullOrEmpty(candidate.code) ? candidate.code : candidate.id);
                        full = detail;
                    }
                    if (Contains(full, contentType, contentCode))
                    {
                        containing = full;
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[QuestMembership] FindAsync error: {ex.Message}");
            }
            return new QuestMembership(inCurrent, containing);
        }

        private static bool SameOrUnknown(string a, string b)
        {
            return string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b) || string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
        }
    }
}
