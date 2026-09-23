using System.Collections.Generic;
using System.Threading.Tasks;

namespace eu.foodmission.platform
{
    public interface IFoodyService
    {
        Task<(List<FoodyItem> Result, ApiErrorResponse Error)> GetItemsAsync(string type = null, bool? ownedOnly = null);
        Task<(FoodyLoadout Result, ApiErrorResponse Error)> GetLoadoutAsync();
        Task<(FoodyPurchaseResponse Result, ApiErrorResponse Error)> PurchaseItemAsync(string codeOrId);
        Task<(FoodyItem Result, ApiErrorResponse Error)> EquipItemAsync(string codeOrId);
        Task<(FoodyItem Result, ApiErrorResponse Error)> UnequipItemAsync(string codeOrId);
    }
}
