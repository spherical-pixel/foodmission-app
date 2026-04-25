using System.Threading.Tasks;

namespace eu.foodmission.platform
{
    public interface IPantryService
    {
        // Returns pantry with embedded items. Caches pantryId internally.
        Task<Pantry> GetPantryAsync();

        // Refresh items only — uses cached pantryId
        Task<PantryItem[]> GetItemsAsync();

        // Single item by id
        Task<PantryItem> GetItemAsync(string itemId);

        // Add item — exactly one of foodId/foodCategoryId must be non-empty; the other is null
        Task<PantryItem> AddItemAsync(
            string foodId,
            string foodCategoryId,
            float quantity,
            string unit = "PIECES",
            string notes = null,
            string location = null,
            string expiryDate = null);

        // Update — all fields optional (PATCH semantics, pass null to skip)
        Task<PantryItem> UpdateItemAsync(
            string itemId,
            float? quantity,
            string unit,
            string notes,
            string location,
            string expiryDate);

        Task<bool> DeleteItemAsync(string itemId);
    }
}
