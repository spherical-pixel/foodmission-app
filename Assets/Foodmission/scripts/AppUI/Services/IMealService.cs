using System.Threading.Tasks;

namespace eu.foodmission.platform
{
    public interface IMealService
    {
        // GET /api/v1/meals — all filters optional
        Task<PaginatedMealResponse> GetMealsAsync(
            string search = null,
            string mealCategory = null,
            string mealCourse = null,
            string dietaryPreference = null,
            string recipeId = null,
            int page = 1,
            int limit = 20);

        // GET /api/v1/meals/{id} — cached in memory by id
        Task<Meal> GetMealAsync(string id);

        // POST /api/v1/meals — name is required in request
        Task<Meal> CreateMealAsync(CreateMealRequest request);

        // PATCH /api/v1/meals/{id} — all fields optional
        Task<Meal> UpdateMealAsync(string id, UpdateMealRequest request);

        // DELETE /api/v1/meals/{id}
        Task<bool> DeleteMealAsync(string id);
    }
}
