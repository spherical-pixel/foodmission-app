using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace eu.foodmission.platform
{
    public class QuestProgressionService : IQuestProgressionService
    {
        private static readonly Regex CodeNumberRegex = new Regex(@"\.(\d+)$", RegexOptions.Compiled);

        public IReadOnlyList<QuestProgressionState> EvaluateProgression(
            IEnumerable<Quest> allQuests,
            IEnumerable<QuestProgress> userProgress)
        {
            if (allQuests == null) return Array.Empty<QuestProgressionState>();

            var progressById = new Dictionary<string, QuestProgress>(StringComparer.OrdinalIgnoreCase);
            var progressByCode = new Dictionary<string, QuestProgress>(StringComparer.OrdinalIgnoreCase);

            if (userProgress != null)
            {
                foreach (var p in userProgress)
                {
                    if (p == null) continue;
                    if (!string.IsNullOrEmpty(p.questId)) progressById[p.questId] = p;
                    if (!string.IsNullOrEmpty(p.questCode)) progressByCode[p.questCode] = p;
                }
            }

            var result = new List<QuestProgressionState>();

            // Group by dimension
            var grouped = allQuests.Where(q => q != null).GroupBy(q => q.dimensionId ?? string.Empty);

            foreach (var group in grouped)
            {
                var sorted = group.OrderBy(GetDifficultyOrder)
                                  .ThenBy(GetQuestSequenceNumber)
                                  .ThenBy(q => q.code ?? string.Empty)
                                  .ToList();

                Quest previousQuest = null;
                bool previousWasCompleted = true; // First quest in dimension is always unlocked

                for (int i = 0; i < sorted.Count; i++)
                {
                    var q = sorted[i];
                    QuestProgress prog = null;
                    if (!string.IsNullOrEmpty(q.id) && progressById.TryGetValue(q.id, out var pId)) prog = pId;
                    else if (!string.IsNullOrEmpty(q.code) && progressByCode.TryGetValue(q.code, out var pCode)) prog = pCode;

                    bool isCompleted = prog != null && (prog.completed || prog.progress >= 100f);
                    float progressVal = prog != null ? prog.progress : 0f;

                    // Unlocked if it's the first quest or if the previous quest was completed, or if already completed
                    bool isUnlocked = (i == 0) || previousWasCompleted || isCompleted;

                    result.Add(new QuestProgressionState
                    {
                        Quest = q,
                        IsCompleted = isCompleted,
                        IsUnlocked = isUnlocked,
                        ProgressPercent = progressVal,
                        PreviousQuest = previousQuest
                    });

                    previousQuest = q;
                    previousWasCompleted = isCompleted;
                }
            }

            return result;
        }

        public Quest GetNextQuest(string currentQuestCodeOrId, IEnumerable<Quest> allQuests)
        {
            if (string.IsNullOrEmpty(currentQuestCodeOrId) || allQuests == null) return null;

            var current = allQuests.FirstOrDefault(q =>
                string.Equals(q.id, currentQuestCodeOrId, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(q.code, currentQuestCodeOrId, StringComparison.OrdinalIgnoreCase));

            if (current == null) return null;

            var dimQuests = allQuests
                .Where(q => q != null && string.Equals(q.dimensionId, current.dimensionId, StringComparison.OrdinalIgnoreCase))
                .OrderBy(GetDifficultyOrder)
                .ThenBy(GetQuestSequenceNumber)
                .ThenBy(q => q.code ?? string.Empty)
                .ToList();

            int currentIndex = dimQuests.FindIndex(q =>
                string.Equals(q.id, current.id, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(q.code, current.code, StringComparison.OrdinalIgnoreCase));

            if (currentIndex >= 0 && currentIndex < dimQuests.Count - 1)
            {
                return dimQuests[currentIndex + 1];
            }

            return null;
        }

        public Quest GetPreviousQuest(string questCodeOrId, IEnumerable<Quest> allQuests)
        {
            if (string.IsNullOrEmpty(questCodeOrId) || allQuests == null) return null;

            var target = allQuests.FirstOrDefault(q =>
                string.Equals(q.id, questCodeOrId, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(q.code, questCodeOrId, StringComparison.OrdinalIgnoreCase));

            if (target == null) return null;

            var dimQuests = allQuests
                .Where(q => q != null && string.Equals(q.dimensionId, target.dimensionId, StringComparison.OrdinalIgnoreCase))
                .OrderBy(GetDifficultyOrder)
                .ThenBy(GetQuestSequenceNumber)
                .ThenBy(q => q.code ?? string.Empty)
                .ToList();

            int targetIndex = dimQuests.FindIndex(q =>
                string.Equals(q.id, target.id, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(q.code, target.code, StringComparison.OrdinalIgnoreCase));

            if (targetIndex > 0)
            {
                return dimQuests[targetIndex - 1];
            }

            return null;
        }

        private static int GetDifficultyOrder(Quest q)
        {
            if (q == null || string.IsNullOrEmpty(q.level)) return 1;
            return q.level.ToUpperInvariant() switch
            {
                QuestLevel.Beginner => 1,
                QuestLevel.Intermediate => 2,
                QuestLevel.Advanced => 3,
                _ => 1
            };
        }

        private static int GetQuestSequenceNumber(Quest q)
        {
            if (q == null || string.IsNullOrEmpty(q.code)) return 0;
            var match = CodeNumberRegex.Match(q.code);
            if (match.Success && int.TryParse(match.Groups[1].Value, out int num))
            {
                return num;
            }
            return 0;
        }
    }
}
