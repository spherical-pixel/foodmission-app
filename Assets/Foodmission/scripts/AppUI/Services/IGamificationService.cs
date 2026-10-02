using System.Threading.Tasks;

namespace eu.foodmission.platform
{
    public interface IGamificationService
    {
        Task<(WalletBalance Result, ApiErrorResponse Error)> GetWalletBalanceAsync();
        Task<(UserEarnedRewardsResponse Result, ApiErrorResponse Error)> GetEarnedRewardsAsync();
        Task<(GamificationProfileResponse Result, ApiErrorResponse Error)> GetGamificationProfileAsync(int eventsLimit = 20, int walletEntriesLimit = 20);

        /// <summary>
        /// POST /users/me/gamification/onboarding-survey: the backend scores the five answers into a segment and,
        /// on first submission, creates the wallet and progress wheels. Returns 409 when onboarding was already applied.
        /// </summary>
        Task<(OnboardingSurveyResult Result, ApiErrorResponse Error)> SubmitOnboardingSurveyAsync(OnboardingSurveyData answers);

        /// <summary>GET /users/me/gamification/progress-wheels — empty array while the user has no segment.</summary>
        Task<(ProgressWheel[] Result, ApiErrorResponse Error)> GetProgressWheelsAsync();
    }
}
