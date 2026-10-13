using System.Threading.Tasks;

namespace eu.foodmission.platform
{
    public interface IGamificationService
    {
        Task<(WalletBalance Result, ApiErrorResponse Error)> GetWalletBalanceAsync();
        Task<(UserEarnedRewardsResponse Result, ApiErrorResponse Error)> GetEarnedRewardsAsync();
        Task<(GamificationProfileResponse Result, ApiErrorResponse Error)> GetGamificationProfileAsync(int eventsLimit = 20, int walletEntriesLimit = 20);

        /// <summary>
        /// POST /users/me/gamification/onboarding-survey: every answer is optional; on first submission the backend scores
        /// each dimension (levels + segment). Returns 409 when onboarding was already applied (no rescoring).
        /// </summary>
        Task<(OnboardingSurveyResult Result, ApiErrorResponse Error)> SubmitOnboardingSurveyAsync(OnboardingSurveyData answers);

        /// <summary>GET /users/me/gamification/onboarding-survey: the questions to show, in order (texts in the app language when the backend has them).</summary>
        Task<(OnboardingSurveyQuestion[] Result, ApiErrorResponse Error)> GetOnboardingSurveyAsync();

        /// <summary>GET /users/me/gamification/dimensions: the user's level per dimension (always the six; BEGINNER before the survey).</summary>
        Task<(DimensionLevelEntry[] Result, ApiErrorResponse Error)> GetDimensionLevelsAsync();

        /// <summary>GET /users/me/gamification/progress-wheels — created at BEGINNER before the survey.</summary>
        Task<(ProgressWheel[] Result, ApiErrorResponse Error)> GetProgressWheelsAsync();

        /// <summary>GET /users/me/gamification/knowledge-progress: the three knowledge bars, without items.</summary>
        Task<(KnowledgeProgress[] Result, ApiErrorResponse Error)> GetKnowledgeProgressAsync();

        /// <summary>GET /users/me/gamification/knowledge-progress/{kind}: one bar with all its finished and open items.</summary>
        Task<(KnowledgeProgress Result, ApiErrorResponse Error)> GetKnowledgeBarAsync(string kind);
    }
}
