using System.Threading.Tasks;

namespace eu.foodmission.platform
{
    public interface IShoppingListService
    {
        // Lists
        Task<ShoppingList[]> GetListsAsync();
        Task<ShoppingList> CreateListAsync(string name, string description = null, string groupId = null);
        Task<bool> UpdateListAsync(string id, string name, string description = null);
        Task<bool> DeleteListAsync(string id);

        // Items
        Task<ShoppingListItem[]> GetItemsAsync(string listId);
        Task<ShoppingListItem> AddItemAsync(string listId, string foodId, float quantity, string unit = "PIECES", string notes = null);
        Task<ShoppingListItem> UpdateItemAsync(string listId, string itemId, float? quantity, string unit, string notes, bool? isChecked);
        Task<ShoppingListItem> ToggleItemCheckedAsync(string listId, string itemId);
        Task<bool> DeleteItemAsync(string listId, string itemId);
        Task<bool> ClearCheckedItemsAsync(string listId);
    }
}
