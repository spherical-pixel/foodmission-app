using System.Collections.Generic;
using System.Threading.Tasks;

namespace eu.foodmission.platform
{
    /// <summary>
    /// Moves expired pantry items to food waste in one request (POST /pantry/{id}/items/batch-waste).
    /// Shared by Pantry and Food Waste so both cancel reminders, report the waste challenge and surface
    /// partial failures the same way.
    /// </summary>
    public interface IExpiredWasteBatcher
    {
        /// <summary>
        /// Returns how many items were wasted and the error to show, if any: the request error, or one
        /// built from the per-item errors of a partial failure (HTTP 200 with <c>errors</c>).
        /// </summary>
        Task<(int WastedCount, ApiErrorResponse Error)> WasteAsync(IReadOnlyCollection<string> pantryItemIds);
    }
}
