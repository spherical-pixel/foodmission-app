using System.Threading.Tasks;

namespace eu.foodmission.platform
{
    public interface IMealItemService
    {
        Task<(MealItem Result, ApiErrorResponse Error)> CreateAsync(string mealId, CreateMealItemRequest request);

        Task<(MealItemDetail[] Result, ApiErrorResponse Error)> GetByMealIdAsync(string mealId);

        Task<(MealItem Result, ApiErrorResponse Error)> UpdateAsync(string mealId, string itemId, CreateMealItemRequest request);

        Task<(bool Success, ApiErrorResponse Error)> DeleteAsync(string mealId, string itemId);
    }
}
