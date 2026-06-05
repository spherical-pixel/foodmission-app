using System.Threading.Tasks;

namespace eu.foodmission.platform
{
    public interface IRecipeService
    {
        Task<(PaginatedRecipeResponse Result, ApiErrorResponse Error)> GetRecipesAsync(
            string search = null,
            string category = null,
            string cuisineType = null,
            string difficulty = null,
            int page = 1,
            int limit = 20);

        Task<(Recipe Result, ApiErrorResponse Error)> GetRecipeAsync(string id);

        Task<(PaginatedRecipeResponse Result, ApiErrorResponse Error)> GetMyRecipesAsync(
            string search = null,
            int page = 1,
            int limit = 20);
    }
}
