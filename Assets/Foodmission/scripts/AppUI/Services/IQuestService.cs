using System.Threading.Tasks;

namespace eu.foodmission.platform
{
    public interface IQuestService
    {
        Task<(Quest[] Result, ApiErrorResponse Error)> GetQuestsAsync(string dimensionCode = null, string level = null, string lang = null);
        Task<(Quest Result, ApiErrorResponse Error)> GetQuestAsync(string codeOrId, string lang = null);
        Task<(QuestProgress[] Result, ApiErrorResponse Error)> GetUserProgressListAsync(string lang = null);
        Task<(QuestProgress Result, ApiErrorResponse Error)> GetQuestProgressAsync(string codeOrId, string lang = null);
        Task<(QuestProgress Result, ApiErrorResponse Error)> UpdateQuestProgressAsync(string codeOrId, bool? completed, float? progressPercent, string lang = null);
    }
}
