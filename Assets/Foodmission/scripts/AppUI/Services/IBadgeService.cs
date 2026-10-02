using System.Threading.Tasks;

namespace eu.foodmission.platform
{
    public interface IBadgeService
    {
        /// <summary>GET /api/v1/badges/me — full catalog with the current user's earned state and progress.</summary>
        Task<(UserBadgesResponse Result, ApiErrorResponse Error)> GetMyBadgesAsync();
    }
}
