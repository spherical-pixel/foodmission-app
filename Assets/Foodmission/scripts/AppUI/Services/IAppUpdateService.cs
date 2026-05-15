using System.Threading.Tasks;

namespace eu.foodmission.platform
{
    public interface IAppUpdateService
    {
        Task<(AppVersionCheckResult Result, ApiErrorResponse Error)> CheckForUpdateAsync();
    }
}
