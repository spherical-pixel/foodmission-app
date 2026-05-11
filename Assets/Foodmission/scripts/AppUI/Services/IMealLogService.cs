using System.Threading.Tasks;

namespace eu.foodmission.platform
{
    public interface IMealLogService
    {
        Task<(MealLog Result, ApiErrorResponse Error)> CreateAsync(CreateMealLogRequest request);
        Task<(PaginatedMealLogResponse Result, ApiErrorResponse Error)> GetLogsAsync(
            int page = 1,
            int limit = 20,
            string typeOfMeal = null,
            string dateFrom = null,
            string dateTo = null);
        Task<(MealLog Result, ApiErrorResponse Error)> GetLogAsync(string id);
        Task<(bool Success, ApiErrorResponse Error)> DeleteLogAsync(string id);
    }
}
