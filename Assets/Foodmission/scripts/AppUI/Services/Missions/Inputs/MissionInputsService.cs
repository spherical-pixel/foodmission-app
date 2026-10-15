using System;
using System.Linq;
using System.Threading.Tasks;

using UnityEngine;

namespace eu.foodmission.platform
{
    public sealed class MissionInputsService : IMissionInputsService
    {
        public const int PageSize = 100;
        private const int MaxPages = 5;
        private const int LookbackDays = 7;

        private readonly IMealLogService _mealLogService;

        public Func<DateTime> Clock { get; set; } = () => DateTime.Now;

        public MissionInputsService(IMealLogService mealLogService)
        {
            _mealLogService = mealLogService;
        }

        public async Task<int> SuggestPreviousMeatMealsAsync()
        {
            if (_mealLogService == null)
            {
                return 0;
            }

            try
            {
                DateTime now = Clock();
                // dateTo is lte: send instants, not bare dates
                string from = now.AddDays(-LookbackDays).ToUniversalTime().ToString("o");
                string to = now.ToUniversalTime().ToString("o");
                int count = 0;
                for (int page = 1; page <= MaxPages; page++)
                {
                    var (result, error) = await _mealLogService.GetLogsAsync(page, PageSize, null, from, to);
                    if (error != null)
                    {
                        return 0;
                    }
                    if (result?.data == null)
                    {
                        break;
                    }
                    // Drafts count toward nothing until confirmed; the backend counts one MEAL_MEAT_CONSUMED per meal log
                    count += result.data.Count(log =>
                        log != null &&
                        !string.Equals(log.status, MealLogStatus.Draft, StringComparison.OrdinalIgnoreCase) &&
                        log.flags != null && log.flags.Contains(ClientEventTypes.MealMeatConsumed));
                    if (page >= result.totalPages)
                    {
                        break;
                    }
                }
                return count;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MissionInputsService] SuggestPreviousMeatMealsAsync error: {ex.Message}");
                return 0;
            }
        }
    }
}
