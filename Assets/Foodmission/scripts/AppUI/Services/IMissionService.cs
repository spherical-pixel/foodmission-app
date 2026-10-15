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

        /// <summary>
        /// Starts a mission of the current quest (backend #424): IN_PROGRESS at 0 with startedAt now, opening its 7-day
        /// window. Starting it again returns it unchanged. M.B1.3 takes last week's meals with meat (0–21); other
        /// missions must get null (the backend rejects inputs there). 400 when it is not in the current quest.
        /// </summary>
        Task<(MissionProgress Result, ApiErrorResponse Error)> StartMissionProgressAsync(
            string codeOrId,
            int? previousMeatMeals = null,
            string lang = null);

        /// <summary>Restarts a FAILED mission (new attempt, new startedAt), with the same optional inputs as start. 400 when it has not failed.</summary>
        Task<(MissionProgress Result, ApiErrorResponse Error)> RestartMissionProgressAsync(
            string codeOrId,
            int? previousMeatMeals = null,
            string lang = null);

        /// <summary>Gives up the mission: PATCH { failed: true }. 400 when it is completed or already failed.</summary>
        Task<(MissionProgress Result, ApiErrorResponse Error)> FailMissionProgressAsync(
            string codeOrId,
            string lang = null);

        string GetCachedCode(string missionId);
    }
}
