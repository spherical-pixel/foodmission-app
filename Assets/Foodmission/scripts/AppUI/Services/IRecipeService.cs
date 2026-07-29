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

        Task<(Recipe Result, ApiErrorResponse Error)> CreateRecipeAsync(CreateRecipeRequest req);

        Task<(Recipe Result, ApiErrorResponse Error)> UpdateRecipeAsync(string id, CreateRecipeRequest req);

        Task<(bool Success, ApiErrorResponse Error)> DeleteRecipeAsync(string id);

        Task<(MultipleRecommendationResponse Result, ApiErrorResponse Error)> GetRecommendationsAsync(
            int expiringWithinDays = 7,
            int limit = 10,
            int offset = 0);
    }
}
