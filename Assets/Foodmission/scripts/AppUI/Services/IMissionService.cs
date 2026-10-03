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

        /// <summary>Restarts a FAILED mission (new attempt: progress 0, new startedAt). 400 when it has not failed.</summary>
        Task<(MissionProgress Result, ApiErrorResponse Error)> RestartMissionProgressAsync(
            string codeOrId,
            string lang = null);

        /// <summary>Gives up the mission: PATCH { failed: true }. 400 when it is completed or already failed.</summary>
        Task<(MissionProgress Result, ApiErrorResponse Error)> FailMissionProgressAsync(
            string codeOrId,
            string lang = null);

        string GetCachedCode(string missionId);
    }
}
