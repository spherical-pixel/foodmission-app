using System;

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
    }
}
