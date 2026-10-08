using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace eu.foodmission.platform
{
    public class QuestProgressionService : IQuestProgressionService
    {
        private static readonly Regex CodeNumberRegex = new Regex(@"\.(\d+)$", RegexOptions.Compiled);

        /// <summary>
        /// Development switch: when true every quest is reported as unlocked (sequence and level).
        /// Defaults to DevUnlocks.All (the FM_UNLOCK_ALL_QUESTS scripting define).
        /// </summary>
        public bool UnlockAllQuests { get; set; } = DevUnlocks.All;

        public IReadOnlyList<QuestProgressionState> EvaluateProgression(
            IEnumerable<Quest> allQuests,
            IEnumerable<QuestProgress> userProgress,
            Func<string, string> userLevelForDimensionId = null)
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

                int currentDifficulty = -1;
                Quest previousQuest = null;
                bool previousWasCompleted = true;

                foreach (var q in sorted)
                {
                    int difficulty = GetDifficultyOrder(q);
                    if (difficulty != currentDifficulty)
                    {
                        // The sequential chain restarts at the first quest of each difficulty
                        currentDifficulty = difficulty;
                        previousQuest = null;
                        previousWasCompleted = true;
                    }

                    QuestProgress prog = null;
                    if (!string.IsNullOrEmpty(q.id) && progressById.TryGetValue(q.id, out var pId))
                    {
                        prog = pId;
                    }
                    else if (!string.IsNullOrEmpty(q.code) && progressByCode.TryGetValue(q.code, out var pCode))
                    {
                        prog = pCode;
                    }

                    bool isCompleted = prog != null && (prog.completed || prog.progress >= 100f);
                    float progressVal = prog != null ? prog.progress : 0f;
                    bool started = isCompleted || progressVal > 0f;

                    var reason = ContentLockReason.None;
                    if (!UnlockAllQuests)
                    {
                        string userLevel = userLevelForDimensionId?.Invoke(q.dimensionId);
                        if (LevelAccess.IsLockedByLevel(q.level, userLevel, started))
                        {
                            reason = ContentLockReason.Level;
                        }
                        else if (!previousWasCompleted && !started)
                        {
                            reason = ContentLockReason.Sequence;
                        }
                    }

                    result.Add(new QuestProgressionState
                    {
                        Quest = q,
                        IsCompleted = isCompleted,
                        LockReason = reason,
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

            // The chain is per difficulty: completing the last quest of a difficulty unlocks nothing new
            var dimQuests = allQuests
                .Where(q => q != null
                            && string.Equals(q.dimensionId, current.dimensionId, StringComparison.OrdinalIgnoreCase)
                            && GetDifficultyOrder(q) == GetDifficultyOrder(current))
                .OrderBy(GetQuestSequenceNumber)
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
                .Where(q => q != null
                            && string.Equals(q.dimensionId, target.dimensionId, StringComparison.OrdinalIgnoreCase)
                            && GetDifficultyOrder(q) == GetDifficultyOrder(target))
                .OrderBy(GetQuestSequenceNumber)
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

        public Quest GetFirstQuest(string dimensionId, string level, IEnumerable<Quest> allQuests)
        {
            if (allQuests == null || !ContentLevel.IsValid(level)) return null;

            int difficulty = ContentLevel.Rank(level) + 1;
            return allQuests
                .Where(q => q != null
                            && string.Equals(q.dimensionId, dimensionId, StringComparison.OrdinalIgnoreCase)
                            && GetDifficultyOrder(q) == difficulty)
                .OrderBy(GetQuestSequenceNumber)
                .ThenBy(q => q.code ?? string.Empty)
                .FirstOrDefault();
        }

        public string GetLevelReached(Quest completedQuest, string userLevel, IEnumerable<Quest> allQuests, IEnumerable<QuestProgress> userProgress)
        {
            int rank = ContentLevel.Rank(userLevel);
            if (completedQuest == null || allQuests == null || rank < 0 || rank >= ContentLevel.All.Length - 1)
            {
                return null;
            }
            if (GetDifficultyOrder(completedQuest) != rank + 1)
            {
                return null;
            }

            var sameLevel = allQuests
                .Where(q => q != null
                            && string.Equals(q.dimensionId, completedQuest.dimensionId, StringComparison.OrdinalIgnoreCase)
                            && GetDifficultyOrder(q) == rank + 1)
                .ToList();

            foreach (QuestProgressionState state in EvaluateProgression(sameLevel, userProgress))
            {
                if (!state.IsCompleted && !IsSameQuest(state.Quest, completedQuest))
                {
                    return null;
                }
            }

            return ContentLevel.All[rank + 1];
        }

        public bool IsDimensionCompleted(Quest completedQuest, IEnumerable<Quest> allQuests, IEnumerable<QuestProgress> userProgress)
        {
            if (completedQuest == null || allQuests == null)
            {
                return false;
            }

            var dimension = allQuests
                .Where(q => q != null && string.Equals(q.dimensionId, completedQuest.dimensionId, StringComparison.OrdinalIgnoreCase))
                .ToList();

            return EvaluateProgression(dimension, userProgress)
                .All(state => state.IsCompleted || IsSameQuest(state.Quest, completedQuest));
        }

        private static bool IsSameQuest(Quest a, Quest b)
        {
            return (!string.IsNullOrEmpty(a.id) && string.Equals(a.id, b.id, StringComparison.OrdinalIgnoreCase)) ||
                   (!string.IsNullOrEmpty(a.code) && string.Equals(a.code, b.code, StringComparison.OrdinalIgnoreCase));
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
