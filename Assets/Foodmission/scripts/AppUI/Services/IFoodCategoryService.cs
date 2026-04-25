using System.Threading.Tasks;

namespace eu.foodmission.platform
{
    public interface IFoodCategoryService
    {
        // Paginated search — query and foodGroup are both optional
        Task<PaginatedFoodCategoryResponse> SearchCategoriesAsync(
            string query = null, string foodGroup = null, int page = 1, int pageSize = 20);

        // Single category lookup — cached in memory by id
        Task<FoodCategory> GetCategoryByIdAsync(string id);

        // List all distinct food group names (e.g. "Fruits", "Dairy", "Meat")
        Task<string[]> GetFoodGroupsAsync();
    }
}
