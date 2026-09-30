using System;
using System.Threading.Tasks;

namespace eu.foodmission.platform
{
    /// <summary>
    /// The challenge the user is currently working on through a helper module. In-memory only.
    /// </summary>
    public interface IChallengeSessionService
    {
        /// <summary>Raised when a session completes its challenge (reward null when none was granted).</summary>
        event Action<string, ContentReward> AutoCompleted;

        string ActiveChallengeCode { get; }

        /// <summary>Starts a session for auto-completing interactions; manual and comparator interactions cancel any session.</summary>
        void Begin(string challengeCode, ChallengeInteraction interaction);

        void Cancel();

        /// <summary>Called by modules. itemKey identifies the product/fact/waste/recipe so repeats count once.</summary>
        Task ReportAsync(ChallengeCompletionTrigger trigger, string itemKey);
    }
}
