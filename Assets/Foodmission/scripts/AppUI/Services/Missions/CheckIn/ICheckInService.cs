using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace eu.foodmission.platform
{
    public interface ICheckInService
    {
        /// <summary>What the check-in should ask now. onlyMissionCode = single-mission mode.</summary>
        Task<(CheckInPlan Plan, ApiErrorResponse Error)> LoadPlanAsync(string onlyMissionCode = null);

        /// <summary>
        /// Day questions answered and sent: not asked again for those days. A "No" sends nothing the backend could
        /// remember, so the device keeps it (per user and mission attempt: a restarted mission asks again).
        /// </summary>
        void RememberDayAnswers(IEnumerable<(string MissionCode, DateTime? StartLocal, DateTime Day)> answered);
    }
}
