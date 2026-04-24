using System.Threading.Tasks;

namespace eu.foodmission.platform
{
    public interface IFoodService
    {
        // Searches the app's food database (paginated)
        Task<PaginatedFoodResponse> SearchFoodsAsync(string query, int page = 1, int pageSize = 20);

        // Single food lookup — results cached in memory by id
        Task<FoodItem> GetFoodByIdAsync(string id);

        // Searches OpenFoodFacts for rich product data (image, brands, nutrition)
        Task<OpenFoodFactsSearchResponse> SearchOpenFoodFactsAsync(string query, int page = 1, int pageSize = 20);

        // Imports an OpenFoodFacts product into the app DB by barcode.
        // Returns the app-DB FoodItem (with id) — idempotent if already imported.
        Task<FoodItem> ImportFromBarcodeAsync(string barcode);
    }
}
