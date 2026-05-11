using System.Threading.Tasks;

namespace eu.foodmission.platform
{
    public interface IFoodService
    {
        // Searches the app's food database (paginated)
        Task<(PaginatedFoodResponse Result, ApiErrorResponse Error)> SearchFoodsAsync(string query, int page = 1, int pageSize = 20);

        // Single food lookup — results cached in memory by id
        Task<(FoodItem Result, ApiErrorResponse Error)> GetFoodByIdAsync(string id);

        // Searches OpenFoodFacts for rich product data (image, brands, nutrition)
        Task<(OpenFoodFactsSearchResponse Result, ApiErrorResponse Error)> SearchOpenFoodFactsAsync(string query, int page = 1, int pageSize = 20);

        // Imports an OpenFoodFacts product into the app DB by barcode.
        // Returns the app-DB FoodItem (with id). Throws 400 if already imported
        // — callers should fall back to FindByBarcodeAsync on statusCode 400.
        Task<(FoodItem Result, ApiErrorResponse Error)> ImportFromBarcodeAsync(string barcode);

        // Finds an existing food item by barcode in the app DB.
        Task<(FoodItem Result, ApiErrorResponse Error)> FindByBarcodeAsync(string barcode);
    }
}
