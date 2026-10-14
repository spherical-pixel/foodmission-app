using System.Collections.Generic;
using System.Threading.Tasks;

namespace eu.foodmission.platform
{
    /// <summary>Mission cards of the product detail: what to ask about one food, and sending its answers.</summary>
    public interface IProductMissionService
    {
        Task<(IReadOnlyList<ProductMissionCard> Cards, ApiErrorResponse Error)> LoadAsync(FoodInfoType type, string foodId, IReadOnlyCollection<string> availableKeys);

        Task<MissionReportSendResult> SendAsync(ProductMissionCard card, IReadOnlyCollection<int> selected);

        /// <summary>Re-plans the last loaded food with what was sent since (shared events lock in every card).</summary>
        IReadOnlyList<ProductMissionCard> Replan();
    }
}
