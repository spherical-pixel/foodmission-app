using System.Collections.Generic;

namespace eu.foodmission.platform
{
    public class QuestProgressionState
    {
        public Quest Quest { get; set; }
        public bool IsCompleted { get; set; }
        public bool IsUnlocked { get; set; }
        public bool IsLocked => !IsUnlocked;
        public float ProgressPercent { get; set; }
        public Quest PreviousQuest { get; set; }
    }

    public interface IQuestProgressionService
    {
        IReadOnlyList<QuestProgressionState> EvaluateProgression(
            IEnumerable<Quest> allQuests,
            IEnumerable<QuestProgress> userProgress);

        Quest GetNextQuest(string currentQuestCodeOrId, IEnumerable<Quest> allQuests);

        Quest GetPreviousQuest(string questCodeOrId, IEnumerable<Quest> allQuests);
    }
}
