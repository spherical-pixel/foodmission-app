using System.Threading.Tasks;

namespace eu.foodmission.platform
{
    public interface IFoodProductService
    {
        Task<(PaginatedFoodProductResponse Result, ApiErrorResponse Error)> SearchFoodsAsync(string query, int page = 1, int pageSize = 20);

        Task<(FoodProduct Result, ApiErrorResponse Error)> GetFoodByIdAsync(string id);

        Task<(OpenFoodFactsSearchResponse Result, ApiErrorResponse Error)> SearchOpenFoodFactsAsync(string query, int page = 1, int pageSize = 20);

        Task<(FoodProduct Result, ApiErrorResponse Error)> ImportFromBarcodeAsync(string barcode);

        Task<(FoodProduct Result, ApiErrorResponse Error)> FindByBarcodeAsync(string barcode, bool includeOpenFoodFacts = false);
    }
}
