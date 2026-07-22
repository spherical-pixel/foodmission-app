using System.Threading.Tasks;

namespace eu.foodmission.platform
{
    public interface IOpenFoodFactsClientService
    {
        Task<(OpenFoodFactsProduct Result, ApiErrorResponse Error)> GetByBarcodeAsync(string barcode);
        Task<(OpenFoodFactsSearchResponse Result, ApiErrorResponse Error)> SearchAsync(string query, int page);
    }
}
