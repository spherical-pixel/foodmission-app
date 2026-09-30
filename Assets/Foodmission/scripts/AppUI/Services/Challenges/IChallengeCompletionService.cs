using System;
using System.Threading.Tasks;

namespace eu.foodmission.platform
{
    public sealed class ChallengeCompletionResult
    {
        public bool Success;
        /// <summary>Only set on the first completion of a challenge with a non-empty reward.</summary>
        public ContentReward Reward;
        public ApiErrorResponse Error;
    }

    public interface IChallengeCompletionService
    {
        /// <summary>Raised after a successful PATCH (reward may be null).</summary>
        event Action<string, ContentReward> ChallengeCompleted;

        /// <summary>True if the challenge was completed through this service in the current app session.</summary>
        bool IsCompleted(string challengeCode);

        /// <summary>PATCH progress 100 / completed true. Concurrent calls for the same code share one request;
        /// only the caller that started it receives the reward.</summary>
        Task<ChallengeCompletionResult> CompleteAsync(string challengeCode);

        /// <summary>Forgets session state (completed cache, in-flight requests). Call on logout.</summary>
        void Reset();
    }
}
