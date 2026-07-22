using System.Collections.Generic;
using System.Threading.Tasks;

namespace eu.foodmission.platform
{
    public interface ICatalogService
    {
        Task<(CatalogData Result, ApiErrorResponse Error)> LoadStartupAsync(string lang);

        Task<(CatalogItem[] Result, ApiErrorResponse Error)> GetTypeOfMealsAsync(string lang);

        Task<(CatalogItem[] Result, ApiErrorResponse Error)> GetMealCategoriesAsync(string lang);

        Task<(CatalogItem[] Result, ApiErrorResponse Error)> GetMealCoursesAsync(string lang);

        Task<(CatalogItem[] Result, ApiErrorResponse Error)> GetUnitsAsync(string lang);

        Task<(CatalogItem[] Result, ApiErrorResponse Error)> GetGroupRolesAsync(string lang);

        Task<(PaginatedCatalogResponse Result, ApiErrorResponse Error)> GetLanguagesAsync(
            string lang, string search = null);

        Task<(List<CatalogItem> Result, ApiErrorResponse Error)> GetCountriesAsync(string lang);

        Task<(List<CatalogItem> Result, ApiErrorResponse Error)> GetRegionsAsync(
            string countryCode, string lang);
    }
}
