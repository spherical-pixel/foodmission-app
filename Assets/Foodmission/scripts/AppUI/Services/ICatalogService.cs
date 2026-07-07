using System.Collections.Generic;
using System.Threading.Tasks;

namespace eu.foodmission.platform
{
    public interface ICatalogService
    {
        Task<(CatalogData Result, ApiErrorResponse Error)> LoadStartupAsync(string lang);

        Task<(CatalogItem[] Result, ApiErrorResponse Error)> GetTypeOfMealsAsync();

        Task<(CatalogItem[] Result, ApiErrorResponse Error)> GetMealCategoriesAsync();

        Task<(CatalogItem[] Result, ApiErrorResponse Error)> GetMealCoursesAsync();

        Task<(List<CatalogItem> Result, ApiErrorResponse Error)> GetCountriesAsync();

        Task<(List<CatalogItem> Result, ApiErrorResponse Error)> GetRegionsAsync(string countryCode);
    }
}