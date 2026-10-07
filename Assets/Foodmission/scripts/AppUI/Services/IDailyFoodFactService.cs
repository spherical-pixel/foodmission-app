using System.Threading.Tasks;

namespace eu.foodmission.platform
{
    public interface IDailyFoodFactService
    {
        /// <summary>
        /// Code of the food fact Home opens today for the active quest, or null (no active quest, already shown today,
        /// nothing unread). Does not mark the day: call MarkShown when Home opens it. Never throws.
        /// </summary>
        Task<string> GetFactToShowAsync();

        /// <summary>Home opened the fact: no other daily fact today.</summary>
        void MarkShown(string code);

        /// <summary>Forgets the current user's state. Call on logout.</summary>
        void Reset();
    }
}
