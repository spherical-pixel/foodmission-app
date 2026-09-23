using System.Threading.Tasks;

namespace eu.foodmission.platform
{
    public interface IGamificationService
    {
        Task<(WalletBalance Result, ApiErrorResponse Error)> GetWalletBalanceAsync();
        Task<(UserEarnedRewardsResponse Result, ApiErrorResponse Error)> GetEarnedRewardsAsync();
        Task<(GamificationProfileResponse Result, ApiErrorResponse Error)> GetGamificationProfileAsync(int eventsLimit = 20, int walletEntriesLimit = 20);
    }
}
