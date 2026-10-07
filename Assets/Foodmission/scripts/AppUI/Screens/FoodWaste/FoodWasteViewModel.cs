using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

using Unity.AppUI.MVVM;

using UnityEngine;
using UnityEngine.Localization.Settings;

namespace eu.foodmission.platform
{
    [ObservableObject]
    public partial class FoodWasteViewModel : ViewModelBase
    {
        private const string CacheKeyPrefix = "foodwaste_cache_";
        private const int PageSize = 50;
        private const int MaxPagesPerMonth = 20;

        private readonly IFoodWasteService _foodWasteService;
        private readonly IPantryService _pantryService;
        private readonly IPantryItemEnricher _pantryItemEnricher;
        private readonly ILocalStorageService _localStorage;
        private readonly IExpiredWasteBatcher _expiredWasteBatcher;

        private List<FoodWaste> _allWaste = new();
        private List<PantryItemView> _pantryItems = new();

        [ObservableProperty]
        private List<FoodWaste> _history = new();

        [ObservableProperty]
        private DateTime _selectedMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

        [ObservableProperty]
        private List<PantryItemView> _expiredItems = new();

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private ApiErrorResponse _errorDetail;

        public FoodWasteViewModel(
            IStoreService storeService,
            IFoodWasteService foodWasteService,
            IPantryService pantryService,
            IPantryItemEnricher pantryItemEnricher,
            ILocalStorageService localStorage,
            INotificationService notificationService = null,
            IChallengeSessionService challengeSession = null,
            IExpiredWasteBatcher expiredWasteBatcher = null)
            : base(storeService)
        {
            _foodWasteService = foodWasteService;
            _pantryService = pantryService;
            _pantryItemEnricher = pantryItemEnricher;
            _localStorage = localStorage;
            _expiredWasteBatcher = expiredWasteBatcher ?? new ExpiredWasteBatcher(pantryService, notificationService, challengeSession);
        }

        public async Task LoadAsync()
        {
            IsLoading = true;
            try
            {
                var historyTask = FetchMonthAsync(SelectedMonth);
                var pantryTask = _pantryService.GetPantryAsync();
                var expiredTask = _pantryService.GetExpiredItemsAsync();

                await Task.WhenAll(historyTask, pantryTask, expiredTask);

                ApiErrorResponse firstError = ApplyHistory(SelectedMonth, historyTask.Result);

                var (pantry, pantryError) = pantryTask.Result;
                if (pantryError != null)
                {
                    firstError ??= pantryError;
                    _pantryItems = new List<PantryItemView>();
                }
                else
                {
                    PantryItemView[] enriched = await _pantryItemEnricher.EnrichAsync(pantry?.items ?? Array.Empty<PantryItem>());
                    _pantryItems = new List<PantryItemView>(enriched);
                }

                var (expired, expiredError) = expiredTask.Result;
                firstError ??= expiredError;
                ExpiredItems = MapExpired(expired);

                ErrorDetail = firstError;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FoodWasteViewModel] LoadAsync failed: {ex.Message}");
                // Show what we have for the month instead of a blank screen.
                ApplyHistory(SelectedMonth, (null, new ApiErrorResponse()));
                ErrorDetail = new ApiErrorResponse
                {
                    message = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "ERROR_LOADING_WASTE_LOG")
                };
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Shows the history of the month containing <paramref name="month"/>. Pantry data is not reloaded.
        /// </summary>
        public async Task SetSelectedMonthAsync(DateTime month)
        {
            DateTime monthStart = new DateTime(month.Year, month.Month, 1);
            SelectedMonth = monthStart;

            IsLoading = true;
            try
            {
                var fetched = await FetchMonthAsync(monthStart);

                // A newer month selection won the race; drop this response.
                if (monthStart != SelectedMonth)
                {
                    return;
                }

                ApiErrorResponse error = ApplyHistory(monthStart, fetched);
                if (error != null)
                {
                    ErrorDetail = error;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FoodWasteViewModel] SetSelectedMonthAsync failed: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task<(List<FoodWaste> Records, ApiErrorResponse Error)> FetchMonthAsync(DateTime monthStart)
        {
            string dateFrom = monthStart.ToUniversalTime().ToString("o");
            string dateTo = monthStart.AddMonths(1).AddSeconds(-1).ToUniversalTime().ToString("o");

            List<FoodWaste> records = new();
            int page = 1;
            int totalPages = 1;
            do
            {
                var (response, error) = await _foodWasteService.GetListAsync(page, PageSize, null, null, dateFrom, dateTo);
                if (error != null)
                {
                    return (null, error);
                }
                if (response?.data != null)
                {
                    records.AddRange(response.data);
                }
                totalPages = response?.totalPages ?? 1;
                page++;
            }
            while (page <= totalPages && page <= MaxPagesPerMonth);

            return (records, null);
        }

        /// <summary>Applies fetched records (or the month cache on error) and returns the error, if any.</summary>
        private ApiErrorResponse ApplyHistory(DateTime monthStart, (List<FoodWaste> Records, ApiErrorResponse Error) fetched)
        {
            if (fetched.Error != null)
            {
                FoodWaste[] cached = _localStorage.GetValue<PaginatedFoodWasteResponse>(CacheKey(monthStart), null)?.data;
                _allWaste = cached != null ? new List<FoodWaste>(cached) : new List<FoodWaste>();
            }
            else
            {
                _allWaste = fetched.Records ?? new List<FoodWaste>();
                SaveCache(monthStart);
            }

            RebuildHistory();
            return fetched.Error;
        }

        /// <summary>
        /// Candidates the user can register as waste. Pantry items only for now; the single entry point
        /// lets other sources (non-pantry products) be merged later without touching the screen.
        /// </summary>
        public Task<List<PantryItemView>> SearchCandidatesAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return Task.FromResult(new List<PantryItemView>());
            }

            string q = query.Trim();
            List<PantryItemView> matches = _pantryItems
                .Where(v => !string.IsNullOrEmpty(v?.DisplayName) && v.DisplayName.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0)
                .OrderBy(v => v.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ToList();
            return Task.FromResult(matches);
        }

        public async Task<bool> DeleteWasteAsync(string wasteId)
        {
            try
            {
                var (_, error) = await _foodWasteService.DeleteAsync(wasteId);
                if (error != null)
                {
                    ErrorDetail = error;
                    return false;
                }

                _allWaste = _allWaste.FindAll(w => w.id != wasteId);
                SaveCache(SelectedMonth);
                RebuildHistory();
                ErrorDetail = null;
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FoodWasteViewModel] DeleteWasteAsync failed: {ex.Message}");
                return false;
            }
        }

        public async Task<int> BatchWasteExpiredAsync()
        {
            if (ExpiredItems == null || ExpiredItems.Count == 0)
            {
                return 0;
            }

            List<string> ids = ExpiredItems.Select(v => v.Item.id).ToList();
            var (wasted, error) = await _expiredWasteBatcher.WasteAsync(ids);

            await LoadAsync();

            // LoadAsync resets ErrorDetail; keep the batch error visible.
            if (error != null)
            {
                ErrorDetail = error;
            }

            return wasted;
        }

        public void OnWasteRecorded()
        {
            _ = LoadAsync();
        }

        private List<PantryItemView> MapExpired(ExpiredPantryItem[] expired)
        {
            List<PantryItemView> result = new();
            if (expired == null)
            {
                return result;
            }

            DateTime today = DateTime.UtcNow.Date;
            foreach (ExpiredPantryItem e in expired)
            {
                if (string.IsNullOrEmpty(e?.expiryDate) || !DateTime.TryParse(e.expiryDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime exp))
                {
                    continue;
                }
                if (exp.Date >= today)
                {
                    continue;
                }

                PantryItemView match = _pantryItems.Find(v => v.Item?.id == e.pantryItemId);
                if (match != null && !result.Contains(match))
                {
                    result.Add(match);
                }
            }
            return result;
        }

        private string CacheKey(DateTime monthStart) => CacheKeyFor(_storeService?.GetAppState()?.userId, monthStart);

        /// <summary>Per user and month: another account on the device must never see this user's waste.</summary>
        public static string CacheKeyFor(string userId, DateTime monthStart)
        {
            return CacheKeyPrefix + (userId ?? "") + "_" + monthStart.ToString("yyyy-MM", CultureInfo.InvariantCulture);
        }

        private void SaveCache(DateTime monthStart)
        {
            _localStorage.SetValue(CacheKey(monthStart), new PaginatedFoodWasteResponse { data = _allWaste.ToArray() });
        }

        private void RebuildHistory()
        {
            History = _allWaste
                .OrderByDescending(w => ParseDate(w.wastedAt) ?? DateTime.MinValue)
                .ToList();
        }

        private static DateTime? ParseDate(string isoDate)
        {
            if (string.IsNullOrEmpty(isoDate))
            {
                return null;
            }
            if (DateTime.TryParse(isoDate, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out DateTime dt))
            {
                return dt;
            }
            return null;
        }
    }
}
