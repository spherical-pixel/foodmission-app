using System;
using System.Threading.Tasks;

namespace eu.foodmission.platform
{
    public enum ChallengeReportResult
    {
        /// <summary>No session, other trigger or other food fact: nothing happened.</summary>
        Ignored,
        Counted,
        Completed,
        /// <summary>Counted, but the backend refused the completion; <see cref="IChallengeSessionService.LastError"/> says why.</summary>
        Failed
    }

    /// <summary>
    /// The challenge the user is currently working on through a helper module. In-memory only.
    /// </summary>
    public interface IChallengeSessionService
    {
        /// <summary>Raised when a session completes its challenge (reward null when none was granted).</summary>
        event Action<string, ContentReward> AutoCompleted;

        /// <summary>Raised when a session starts, ends or counts a new item, so open UI can refresh.</summary>
        event Action SessionChanged;

        string ActiveChallengeCode { get; }
        ChallengeInteraction ActiveInteraction { get; }
        string ActiveChallengeTitle { get; }
        /// <summary>Backend task text of the challenge (already localized).</summary>
        string ActiveChallengeTask { get; }
        int ReportedCount { get; }
        /// <summary>Error of the last failed completion, null otherwise.</summary>
        ApiErrorResponse LastError { get; }
        /// <summary>Enough items counted but the challenge isn't completed yet (in flight or failed): the last item can be reported again to retry.</summary>
        bool IsCompletionPending { get; }

        bool HasReported(string itemKey);

        /// <summary>Starts a session for auto-completing interactions; manual and comparator interactions cancel any session.</summary>
        void Begin(string challengeCode, ChallengeInteraction interaction, string title = null, string task = null);

        void Cancel();

        /// <summary>Called by modules. itemKey identifies the product/fact/waste/recipe so repeats count once.</summary>
        Task<ChallengeReportResult> ReportAsync(ChallengeCompletionTrigger trigger, string itemKey);
    }
}
