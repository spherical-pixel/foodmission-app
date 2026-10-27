using System;
using System.Linq;

namespace eu.foodmission.platform
{
    /// <summary>
    /// Single reading of a mission's progress row. With <c>status</c> (backend pr-402+) it is trusted: a FAILED row can have
    /// progress 100 and is still not completed. Without it, the legacy rule applies.
    /// </summary>
    public static class MissionProgressState
    {
        public static bool IsCompleted(MissionProgress progress)
        {
            if (progress == null)
            {
                return false;
            }
            if (!string.IsNullOrEmpty(progress.status))
            {
                return string.Equals(progress.status, ProgressStatus.Completed, StringComparison.OrdinalIgnoreCase);
            }
            return progress.completed || progress.progress >= 100f;
        }

        public static bool IsFailed(MissionProgress progress)
        {
            return progress != null && string.Equals(progress.status, ProgressStatus.Failed, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Completed or failed: final, nothing more to report until a restart.</summary>
        public static bool IsResolved(MissionProgress progress)
        {
            return IsCompleted(progress) || IsFailed(progress);
        }

        /// <summary>
        /// Whether the user started this attempt. Since backend #424 choosing a quest starts nothing: a mission that is
        /// not started is NOT_STARTED with progress 0 and no startedAt (or has no row). Started ones are IN_PROGRESS,
        /// even at 0. Older backends: restarts were NOT_STARTED with a startedAt, and rows without a status (before
        /// pr-402) started with the quest.
        /// </summary>
        /// <summary>
        /// Started, not resolved, at full progress and nothing left to report: it only waits for its window to end
        /// (rules resolved at window end). Missions with a limit are never secured: a later answer can still change them.
        /// </summary>
        public static bool IsSecured(MissionProgress progress, MissionInteraction interaction) =>
            IsStarted(progress) && !IsResolved(progress) && IsSecured(interaction, progress.progress);

        /// <param name="progressPercent">Backend progress, 0..100.</param>
        public static bool IsSecured(MissionInteraction interaction, float progressPercent) =>
            progressPercent >= 100f && interaction != null && interaction.Steps.Count > 0 &&
            interaction.Steps.All(s => s.Type == MissionStepType.DayQuestion && s.DayQuestion != null && !s.DayQuestion.HasLimit);

        public static bool IsStarted(MissionProgress progress)
        {
            if (progress == null)
            {
                return false;
            }
            if (progress.startedAt.HasValue || progress.progress > 0f || IsResolved(progress))
            {
                return true;
            }
            if (string.Equals(progress.status, ProgressStatus.InProgress, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            return string.IsNullOrEmpty(progress.status);
        }
    }
}
