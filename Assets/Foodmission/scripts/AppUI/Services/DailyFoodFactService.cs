using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

using UnityEngine;

namespace eu.foodmission.platform
{
    /// <summary>
    /// Picks the food fact Home opens once a day while a quest is active: the first unread FOOD_FACT item of the quest.
    /// </summary>
    public class DailyFoodFactService : IDailyFoodFactService
    {
        private const string KeyPrefix = "daily_food_fact_";
        private const string DayFormat = "yyyy-MM-dd";

        private readonly IStoreService _storeService;
        private readonly IQuestService _questService;
        private readonly IFoodFactService _foodFactService;
        private readonly ILocalStorageService _storage;

        public Func<DateTime> NowLocal { get; set; } = () => DateTime.Now;

        public DailyFoodFactService(
            IStoreService storeService,
            IQuestService questService,
            IFoodFactService foodFactService,
            ILocalStorageService storage)
        {
            _storeService = storeService;
            _questService = questService;
            _foodFactService = foodFactService;
            _storage = storage;
        }

        public async Task<string> GetFactToShowAsync()
        {
            try
            {
                AppState state = _storeService?.GetAppState();
                if (state == null || string.IsNullOrEmpty(state.userId) || string.IsNullOrEmpty(state.userCurrentQuestId)
                    || _questService == null || _foodFactService == null)
                {
                    return null;
                }

                string today = NowLocal().ToString(DayFormat, CultureInfo.InvariantCulture);
                if (_storage?.GetValue<string>(StorageKey(state.userId), null) == today)
                {
                    return null;
                }

                var (quest, questError) = await _questService.GetQuestAsync(state.userCurrentQuestId);
                if (questError != null || quest == null)
                {
                    return null;
                }

                // Without the read list we could open a fact the user already read: skip today's fact instead
                var (progress, progressError) = await _foodFactService.GetUserProgressListAsync();
                if (progressError != null)
                {
                    return null;
                }

                string code = FirstUnreadQuestFact(quest, ReadCodes(progress));
                if (string.IsNullOrEmpty(code))
                {
                    return null;
                }

                _storage?.SetValue(StorageKey(state.userId), today);
                return code;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DailyFoodFactService] GetFactToShowAsync failed: {ex.Message}");
                return null;
            }
        }

        public void Reset()
        {
            string userId = _storeService?.GetAppState()?.userId;
            if (!string.IsNullOrEmpty(userId))
            {
                _storage?.DeleteValue(StorageKey(userId));
            }
        }

        private static HashSet<string> ReadCodes(FoodFactProgressResponse[] progress)
        {
            var read = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (FoodFactProgressResponse p in progress ?? Array.Empty<FoodFactProgressResponse>())
            {
                if (p == null)
                {
                    continue;
                }
                if (!string.IsNullOrEmpty(p.foodFactId))
                {
                    read.Add(p.foodFactId);
                }
                if (!string.IsNullOrEmpty(p.foodFactCode))
                {
                    read.Add(p.foodFactCode);
                }
            }
            return read;
        }

        private static string FirstUnreadQuestFact(Quest quest, HashSet<string> read)
        {
            return (quest.items ?? Array.Empty<QuestItem>())
                .Where(i => i != null && string.Equals(i.contentType, QuestContentType.FoodFact, StringComparison.OrdinalIgnoreCase)
                    && !string.IsNullOrEmpty(i.contentCode) && !read.Contains(i.contentCode))
                .OrderBy(i => i.sortOrder)
                .Select(i => i.contentCode)
                .FirstOrDefault();
        }

        /// <summary>Local storage key of the last day (local, yyyy-MM-dd) a user was shown a daily fact.</summary>
        public static string StorageKey(string userId) => KeyPrefix + userId;

        /// <summary>Dev time travel: moves the stored day <paramref name="days"/> days back. Empty or corrupt values are returned unchanged.</summary>
        public static string ShiftStoredDates(string value, int days)
        {
            if (string.IsNullOrEmpty(value)
                || !DateTime.TryParseExact(value, DayFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime day))
            {
                return value;
            }
            return day.AddDays(-days).ToString(DayFormat, CultureInfo.InvariantCulture);
        }
    }
}
