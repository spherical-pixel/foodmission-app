using System.Threading.Tasks;

namespace eu.foodmission.platform
{
    public interface IFoodFactService
    {
        Task<(PaginatedFoodFactResponse Result, ApiErrorResponse Error)> GetFoodFactsAsync(
            FoodFactFilterParams filters = null,
            int page = 1,
            int limit = 10,
            string lang = null);

        Task<(FoodFact Result, ApiErrorResponse Error)> GetFoodFactAsync(
            string codeOrId,
            string lang = null);

        Task<(FoodFact Result, ApiErrorResponse Error)> GetFoodFactByCodeAsync(
            string code,
            string lang = null);
    }
}
