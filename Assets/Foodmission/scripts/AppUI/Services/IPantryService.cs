using System.Threading.Tasks;

namespace eu.foodmission.platform
{
    public interface IPantryService
    {
        // Returns pantry with embedded items. Caches pantryId internally.
        Task<(Pantry Result, ApiErrorResponse Error)> GetPantryAsync();

        // Refresh items only — uses cached pantryId
        Task<(PantryItem[] Result, ApiErrorResponse Error)> GetItemsAsync();

        // Single item by id
        Task<(PantryItem Result, ApiErrorResponse Error)> GetItemAsync(string itemId);

        // Add item — exactly one of foodProductId/genericFoodId must be non-empty; the other is null
        Task<(PantryItem Result, ApiErrorResponse Error)> AddItemAsync(
            string foodProductId,
            string genericFoodId,
            float quantity,
            string unit = "PIECES",
            string notes = null,
            string location = null,
            string expiryDate = null);

        // Update — all fields optional (PATCH semantics, pass null to skip)
        Task<(PantryItem Result, ApiErrorResponse Error)> UpdateItemAsync(
            string itemId,
            float? quantity,
            string unit,
            string notes,
            string location,
            string expiryDate,
            string foodProductId = null,
            string genericFoodId = null);

        Task<(bool Success, ApiErrorResponse Error)> DeleteItemAsync(string itemId);

        // Get expired items for waste detection
        Task<(ExpiredPantryItem[] Result, ApiErrorResponse Error)> GetExpiredItemsAsync();

        // Batch-waste: send selected expired items to waste (returns detailed result)
        Task<(BatchWasteResult Result, ApiErrorResponse Error)> BatchWasteAsync(BatchWasteRequest request);
    }
}
