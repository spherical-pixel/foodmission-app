using System.Threading.Tasks;

namespace eu.foodmission.platform
{
    public interface IChallengeService
    {
        Task<(Challenge[] Result, ApiErrorResponse Error)> GetChallengesAsync(
            ChallengeFilterParams filter = null,
            string lang = null);

        Task<(ChallengeProgress[] Result, ApiErrorResponse Error)> GetUserProgressListAsync(string lang = null);

        Task<(Challenge Result, ApiErrorResponse Error)> GetChallengeAsync(
            string codeOrId,
            string lang = null);

        Task<(ChallengeProgress Result, ApiErrorResponse Error)> GetChallengeProgressAsync(
            string codeOrId,
            string lang = null);

        Task<(ChallengeProgress Result, ApiErrorResponse Error)> UpdateChallengeProgressAsync(
            string codeOrId,
            bool? completed,
            float? progress,
            string lang = null);
    }
}
