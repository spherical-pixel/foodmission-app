using System.Threading.Tasks;

namespace eu.foodmission.platform
{
    /// <summary>
    /// Builds the display view of pantry items (resolves the food name from the product or generic food).
    /// </summary>
    public interface IPantryItemEnricher
    {
        Task<PantryItemView> EnrichAsync(PantryItem item);

        Task<PantryItemView[]> EnrichAsync(PantryItem[] items);
    }
}
