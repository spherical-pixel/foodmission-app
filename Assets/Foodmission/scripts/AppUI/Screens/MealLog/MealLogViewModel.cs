using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Unity.AppUI.MVVM;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace eu.foodmission.platform
{
    [ObservableObject]
    public partial class MealLogViewModel : ViewModelBase
    {
        private readonly IMealLogService _mealLogService;
        private readonly ILocalStorageService _localStorage;

        private const int PageSize = 20;

        private string CacheKey => BuildCacheKey();

        private string BuildCacheKey()
        {
            string type = string.IsNullOrEmpty(FilterTypeOfMeal) ? "all" : FilterTypeOfMeal;
            string from = string.IsNullOrEmpty(FilterDateFrom) ? "any" : FilterDateFrom;
            string to = string.IsNullOrEmpty(FilterDateTo) ? "any" : FilterDateTo;
            return $"meallog_cache_{type}_{from}_{to}";
        }

        private List<MealLog> _allLogs = new();
        private int _currentPage = 1;
        private int _totalPages = 1;

        [ObservableProperty]
        private List<MealLogGroup> m_Groups = new();

        [ObservableProperty]
        private bool m_IsLoading;

        [ObservableProperty]
        private string m_ErrorMessage = "";

        [ObservableProperty]
        private string m_FilterDateFrom = "";

        [ObservableProperty]
        private string m_FilterDateTo = "";

        [ObservableProperty]
        private string m_FilterTypeOfMeal = "";

        [ObservableProperty]
        private bool m_HasMorePages;

        [ObservableProperty]
        private ApiErrorResponse m_ErrorDetail;

        public MealLogViewModel(IStoreService storeService, IMealLogService mealLogService, ILocalStorageService localStorage)
            : base(storeService)
        {
            _mealLogService = mealLogService;
            _localStorage = localStorage;
        }

        public async Task LoadAsync(int page = 1)
        {
            IsLoading = true;
            ErrorMessage = "";

            var (response, error) = await _mealLogService.GetLogsAsync(
                page, PageSize,
                string.IsNullOrEmpty(FilterTypeOfMeal) ? null : FilterTypeOfMeal,
                string.IsNullOrEmpty(FilterDateFrom) ? null : FilterDateFrom,
                string.IsNullOrEmpty(FilterDateTo) ? null : FilterDateTo);

            if (error != null)
            {
                ErrorDetail = error;
                LoadFromCache();
                IsLoading = false;
                return;
            }
            ErrorDetail = null;

            _currentPage = page;
            _totalPages = response.totalPages;
            HasMorePages = _currentPage < _totalPages;

            if (page == 1)
            {
                _allLogs = response.data != null ? new List<MealLog>(response.data) : new List<MealLog>();
            }
            else
            {
                _allLogs.AddRange(response.data ?? Array.Empty<MealLog>());
            }

            SaveCache();
            BuildGroups();
            IsLoading = false;
        }

        public async Task LoadNextPageAsync()
        {
            if (!HasMorePages || IsLoading) return;
            await LoadAsync(_currentPage + 1);
        }

        private void LoadFromCache()
        {
            MealLog[] cached = _localStorage.GetValue<PaginatedMealLogResponse>(CacheKey)?.data;
            _allLogs = cached != null ? new List<MealLog>(cached) : new List<MealLog>();
            BuildGroups();

            if (_allLogs.Count == 0)
                ErrorMessage = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "ERROR_LOADING_MEAL_LOG");
        }

        private void SaveCache()
        {
            _localStorage.SetValue(CacheKey, new PaginatedMealLogResponse { data = _allLogs.ToArray() });
        }

        private void BuildGroups()
        {
            if (_allLogs.Count == 0)
            {
                Groups = new List<MealLogGroup>();
                return;
            }

            List<MealLogGroup> groups = new();

            foreach (IGrouping<string, MealLog> g in _allLogs.GroupBy(l => l.typeOfMeal))
            {
                groups.Add(new MealLogGroup { TypeOfMeal = g.Key, Logs = g.ToList() });
            }

            Groups = groups;
        }

        public async Task DeleteLogAsync(string logId)
        {
            var (success, error) = await _mealLogService.DeleteLogAsync(logId);
            if (error != null)
            {
                ErrorDetail = error;
            }
            else
            {
                ErrorDetail = null;
                _allLogs = _allLogs.FindAll(l => l.id != logId);
                SaveCache();
                BuildGroups();
            }
        }
    }

    public class MealLogGroup
    {
        public string TypeOfMeal;
        public List<MealLog> Logs;
    }
}
