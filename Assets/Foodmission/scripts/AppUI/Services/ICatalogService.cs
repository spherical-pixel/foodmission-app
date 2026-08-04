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

        Task<(CatalogItem[] Result, ApiErrorResponse Error)> GetWeeklyMeatRangesAsync(string lang);

        Task<(CatalogItem[] Result, ApiErrorResponse Error)> GetWeeklyBeefFrequenciesAsync(string lang);

        Task<(CatalogItem[] Result, ApiErrorResponse Error)> GetWeeklyFoodWasteRangesAsync(string lang);

        Task<(CatalogItem[] Result, ApiErrorResponse Error)> GetWeeklyUpfRangesAsync(string lang);

        Task<(CatalogItem[] Result, ApiErrorResponse Error)> GetWeeklyReusableRangesAsync(string lang);

        Task<(CatalogItem[] Result, ApiErrorResponse Error)> GetUserSegmentsAsync(string lang);

        Task<(CatalogItem[] Result, ApiErrorResponse Error)> GetMotivationsAsync(string lang);

        Task<(CatalogItem[] Result, ApiErrorResponse Error)> GetProgressIndicatorKindsAsync(string lang);

        Task<(CatalogItem[] Result, ApiErrorResponse Error)> GetProgressPrecisionsAsync(string lang);

        Task<(CatalogItem[] Result, ApiErrorResponse Error)> GetWalletCurrenciesAsync(string lang);

        Task<(PaginatedCatalogResponse Result, ApiErrorResponse Error)> GetLanguagesAsync(
            string lang, string search = null);

        Task<(List<CatalogItem> Result, ApiErrorResponse Error)> GetCountriesAsync(string lang);

        Task<(List<CatalogItem> Result, ApiErrorResponse Error)> GetRegionsAsync(
            string countryCode, string lang);
    }
}
