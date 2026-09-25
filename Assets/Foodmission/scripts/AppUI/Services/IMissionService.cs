using System.Threading.Tasks;

namespace eu.foodmission.platform
{
    public interface IMissionService
    {
        Task<(Mission[] Result, ApiErrorResponse Error)> GetMissionsAsync(
            MissionFilterParams filter = null,
            string lang = null);

        Task<(MissionProgress[] Result, ApiErrorResponse Error)> GetUserProgressListAsync(string lang = null);

        Task<(Mission Result, ApiErrorResponse Error)> GetMissionAsync(
            string codeOrId,
            string lang = null);

        Task<(MissionProgress Result, ApiErrorResponse Error)> GetMissionProgressAsync(
            string codeOrId,
            string lang = null);

        Task<(MissionProgress Result, ApiErrorResponse Error)> UpdateMissionProgressAsync(
            string codeOrId,
            bool? completed,
            float? progress,
            string lang = null);

        string GetCachedCode(string missionId);
    }
}
