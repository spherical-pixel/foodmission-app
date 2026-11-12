using System;
using System.Threading.Tasks;

namespace eu.foodmission.platform
{
    public interface IMealLogService
    {
        /// <summary>A meal log was created (not as a draft) or a draft was confirmed.</summary>
        event Action<MealLog> MealLogSaved;

        Task<(MealLog Result, ApiErrorResponse Error)> CreateAsync(CreateMealLogRequest request);
        Task<(PaginatedMealLogResponse Result, ApiErrorResponse Error)> GetLogsAsync(
            int page = 1,
            int limit = 20,
            string typeOfMeal = null,
            string dateFrom = null,
            string dateTo = null);
        Task<(MealLog Result, ApiErrorResponse Error)> GetLogAsync(string id);
        Task<(MealLog Result, ApiErrorResponse Error)> UpdateLogAsync(string id, UpdateMealLogRequest request);
        Task<(bool Success, ApiErrorResponse Error)> DeleteLogAsync(string id);
        Task<(MealLog Result, ApiErrorResponse Error)> ConfirmAsync(string id, MealLogFactsRequest facts);
    }
}
