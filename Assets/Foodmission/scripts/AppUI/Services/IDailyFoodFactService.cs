using System.Threading.Tasks;

namespace eu.foodmission.platform
{
    public interface IDailyFoodFactService
    {
        /// <summary>
        /// Code of the food fact Home opens today for the active quest, or null (no active quest, already shown today,
        /// nothing unread). Marks the day as used when it returns a code. Never throws.
        /// </summary>
        Task<string> GetFactToShowAsync();

        /// <summary>Forgets the current user's state. Call on logout.</summary>
        void Reset();
    }
}
