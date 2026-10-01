using System.Threading.Tasks;

namespace eu.foodmission.platform
{
    public interface ICheckInService
    {
        /// <summary>What the check-in should ask now. onlyMissionCode = single-mission mode.</summary>
        Task<(CheckInPlan Plan, ApiErrorResponse Error)> LoadPlanAsync(string onlyMissionCode = null);
    }
}
