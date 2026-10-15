using System.Threading.Tasks;

namespace eu.foodmission.platform
{
    public interface IMissionInputsService
    {
        /// <summary>Confirmed meal logs with meat in the last 7 days: the initial value of Nutri's question. 0 on error.</summary>
        Task<int> SuggestPreviousMeatMealsAsync();
    }
}
