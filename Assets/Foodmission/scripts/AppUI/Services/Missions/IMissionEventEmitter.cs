using System.Collections.Generic;
using System.Threading.Tasks;

namespace eu.foodmission.platform
{
    public interface IMissionEventEmitter
    {
        /// <summary>Sends unsent items in order; stops at the first failure. Call again with the same list to retry.</summary>
        Task<MissionReportSendResult> SendAsync(IReadOnlyList<PendingReportItem> items);
    }
}
