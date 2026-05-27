using System.Threading.Tasks;

namespace eu.foodmission.platform
{
    public interface IGenericFoodService
    {
        Task<(PaginatedGenericFoodResponse Result, ApiErrorResponse Error)> SearchGenericFoodsAsync(
            string query = null, string foodGroup = null, int page = 1, int pageSize = 20);

        Task<(GenericFood Result, ApiErrorResponse Error)> GetGenericFoodByIdAsync(string id);

        Task<(string[] Result, ApiErrorResponse Error)> GetFoodGroupsAsync();
    }
}
