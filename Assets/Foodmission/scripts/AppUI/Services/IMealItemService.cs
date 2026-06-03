using System.Threading.Tasks;

namespace eu.foodmission.platform
{
    public interface IMealItemService
    {
        Task<(MealItem Result, ApiErrorResponse Error)> CreateAsync(string mealId, CreateMealItemRequest request);

        Task<(MealItemDetail[] Result, ApiErrorResponse Error)> GetByMealIdAsync(string mealId);
    }
}
