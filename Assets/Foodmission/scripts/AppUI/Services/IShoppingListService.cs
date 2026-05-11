using System.Threading.Tasks;

namespace eu.foodmission.platform
{
    public interface IShoppingListService
    {
        // Lists
        Task<(ShoppingList[] Result, ApiErrorResponse Error)> GetListsAsync();
        Task<(ShoppingList Result, ApiErrorResponse Error)> CreateListAsync(string name);
        Task<(bool Success, ApiErrorResponse Error)> UpdateListAsync(string id, string name);
        Task<(bool Success, ApiErrorResponse Error)> DeleteListAsync(string id);

        // Items
        Task<(ShoppingListItem[] Result, ApiErrorResponse Error)> GetItemsAsync(string listId);
        Task<(ShoppingListItem Result, ApiErrorResponse Error)> AddItemAsync(string listId, string foodId, float quantity, string unit = "PIECES", string notes = null, bool? checkedState = null);
        Task<(ShoppingListItem Result, ApiErrorResponse Error)> UpdateItemAsync(string listId, string itemId, float? quantity, string unit, string notes, bool? isChecked);
        Task<(bool Success, ApiErrorResponse Error)> DeleteItemAsync(string listId, string itemId);
        Task<(bool Success, ApiErrorResponse Error)> ClearCheckedItemsAsync(string listId);
    }
}
