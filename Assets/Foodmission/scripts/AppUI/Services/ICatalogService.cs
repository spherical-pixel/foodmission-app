using System.Threading.Tasks;

namespace eu.foodmission.platform
{
    /// <summary>
    /// Interface for the Catalog service - fetches reference data from the backend.
    /// </summary>
    public interface ICatalogService
    {
        /// <summary>
        /// Loads catalog data from GET /api/v1/catalog/startup.
        /// Results are cached in memory; re-fetches if lang changes.
        /// </summary>
        /// <param name="lang">Language code for localized labels (e.g. "es", "en", "ca").</param>
        /// <returns>CatalogData with all reference lists, or null on error.</returns>
        Task<(CatalogData Result, ApiErrorResponse Error)> LoadStartupAsync(string lang);
    }
}