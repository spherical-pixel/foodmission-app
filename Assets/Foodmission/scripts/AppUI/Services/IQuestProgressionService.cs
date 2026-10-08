using System;
using System.Collections.Generic;

namespace eu.foodmission.platform
{
    public class QuestProgressionState
    {
        public Quest Quest { get; set; }
        public bool IsCompleted { get; set; }
        public ContentLockReason LockReason { get; set; }
        public bool IsUnlocked
        {
            get => LockReason == ContentLockReason.None;
            set => LockReason = value ? ContentLockReason.None : ContentLockReason.Sequence;
        }
        public bool IsLocked => !IsUnlocked;
        public float ProgressPercent { get; set; }
        /// <summary>Previous quest of the same difficulty in the same dimension (null for the first one).</summary>
        public Quest PreviousQuest { get; set; }
    }

    public interface IQuestProgressionService
    {
        /// <param name="userLevelForDimensionId">Maps a quest's dimensionId to the user's level; null = no level locks.</param>
        IReadOnlyList<QuestProgressionState> EvaluateProgression(
            IEnumerable<Quest> allQuests,
            IEnumerable<QuestProgress> userProgress,
            Func<string, string> userLevelForDimensionId = null);

        Quest GetNextQuest(string currentQuestCodeOrId, IEnumerable<Quest> allQuests);

        Quest GetPreviousQuest(string questCodeOrId, IEnumerable<Quest> allQuests);

        /// <summary>First quest of the level in the dimension (lowest sequence number), or null when it has none.</summary>
        Quest GetFirstQuest(string dimensionId, string level, IEnumerable<Quest> allQuests);

        /// <summary>
        /// The level the user reaches by completing the quest: the next level when the quest is of the user's level and every
        /// quest of that level in its dimension is completed (the quest itself counts as completed); null otherwise.
        /// </summary>
        string GetLevelReached(Quest completedQuest, string userLevel, IEnumerable<Quest> allQuests, IEnumerable<QuestProgress> userProgress);
    }
}
