using System.Threading.Tasks;

namespace eu.foodmission.platform
{
    public interface IFoodWasteService
    {
        Task<(PaginatedFoodWasteResponse Result, ApiErrorResponse Error)> GetListAsync(
            int page = 1,
            int limit = 20,
            string wasteReason = null,
            string detectionMethod = null,
            string dateFrom = null,
            string dateTo = null);

        Task<(FoodWaste Result, ApiErrorResponse Error)> CreateAsync(CreateFoodWasteRequest request);

        Task<(FoodWaste Result, ApiErrorResponse Error)> GetByIdAsync(string id);

        Task<(FoodWaste Result, ApiErrorResponse Error)> UpdateAsync(string id, UpdateFoodWasteRequest request);

        Task<(bool Success, ApiErrorResponse Error)> DeleteAsync(string id);

        Task<(FoodWasteStatistics Result, ApiErrorResponse Error)> GetStatisticsAsync(string dateFrom = null, string dateTo = null);

        Task<(FoodWasteTrends Result, ApiErrorResponse Error)> GetTrendsAsync(string dateFrom, string dateTo, string interval = "day");
    }
}
