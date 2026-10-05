using System.Collections.Generic;
using System.Threading.Tasks;

namespace eu.foodmission.platform
{
    public interface IMissionFailureService
    {
        /// <summary>FAILED missions of the current quest whose failure the user has not been told about yet.</summary>
        Task<IReadOnlyList<MissionProgress>> GetUnacknowledgedFailuresAsync();

        /// <summary>Marks this attempt's failure as told (key: code + startedAt).</summary>
        void Acknowledge(MissionProgress progress);

        /// <summary>Acknowledges and restarts a FAILED mission.</summary>
        Task<(MissionProgress Result, ApiErrorResponse Error)> RestartAsync(MissionProgress failed);

        /// <summary>Voluntary restart of an active mission: gives it up, then restarts it.</summary>
        Task<(MissionProgress Result, ApiErrorResponse Error)> GiveUpAndRestartAsync(string missionCode);
    }
}
