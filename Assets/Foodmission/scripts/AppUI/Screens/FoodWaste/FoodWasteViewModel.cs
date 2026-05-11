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
    public partial class FoodWasteViewModel : ViewModelBase
    {
        private readonly IFoodWasteService _foodWasteService;
        private readonly ILocalStorageService _localStorage;

        private const string CacheKey = "foodwaste_cache";
        private const int PageSize = 20;

        private List<FoodWaste> _allWaste = new();
        private int _currentPage = 1;
        private int _totalPages = 1;

        [ObservableProperty]
        private List<FoodWasteGroup> m_Groups = new();

        [ObservableProperty]
        private bool m_IsLoading;

        [ObservableProperty]
        private string m_ErrorMessage = "";

        [ObservableProperty]
        private string m_FilterDateFrom = "";

        [ObservableProperty]
        private string m_FilterDateTo = "";

        [ObservableProperty]
        private string m_FilterWasteReason = "";

        [ObservableProperty]
        private bool m_HasMorePages;

        [ObservableProperty]
        private ApiErrorResponse m_ErrorDetail;

        public FoodWasteViewModel(
            IStoreService storeService,
            IFoodWasteService foodWasteService,
            ILocalStorageService localStorage)
            : base(storeService)
        {
            _foodWasteService = foodWasteService;
            _localStorage = localStorage;
        }

        public async Task LoadAsync(int page = 1)
        {
            IsLoading = true;
            ErrorMessage = "";

            var (response, error) = await _foodWasteService.GetListAsync(
                page, PageSize,
                string.IsNullOrEmpty(FilterWasteReason) ? null : FilterWasteReason,
                null,
                string.IsNullOrEmpty(FilterDateFrom) ? null : FilterDateFrom,
                string.IsNullOrEmpty(FilterDateTo) ? null : FilterDateTo);

            if (error != null)
            {
                ErrorDetail = error;
                bool hadData = _allWaste.Count > 0;
                LoadFromCache();
                if (hadData)
                    ErrorMessage = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "COULD_NOT_REFRESH_CACHED");
                IsLoading = false;
                return;
            }

            _currentPage = page;
            _totalPages = response.totalPages;
            HasMorePages = _currentPage < _totalPages;

            if (page == 1)
            {
                _allWaste = response.data != null
                    ? new List<FoodWaste>(response.data)
                    : new List<FoodWaste>();
            }
            else
            {
                _allWaste.AddRange(response.data ?? Array.Empty<FoodWaste>());
            }

            ErrorDetail = null;
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
            FoodWaste[] cached = _localStorage.GetValue<PaginatedFoodWasteResponse>(CacheKey)?.data;
            _allWaste = cached != null ? new List<FoodWaste>(cached) : new List<FoodWaste>();
            BuildGroups();

            if (_allWaste.Count == 0)
                ErrorMessage = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "ERROR_LOADING_WASTE_LOG");
        }

        private void SaveCache()
        {
            _localStorage.SetValue(CacheKey, new PaginatedFoodWasteResponse { data = _allWaste.ToArray() });
        }

        private void BuildGroups()
        {
            if (_allWaste.Count == 0)
            {
                Groups = new List<FoodWasteGroup>();
                return;
            }

            List<FoodWasteGroup> groups = new();

            foreach (IGrouping<string, FoodWaste> g in _allWaste.GroupBy(w => GetMonthKey(w.wastedAt)))
            {
                groups.Add(new FoodWasteGroup { MonthKey = g.Key, Items = g.ToList() });
            }

            Groups = groups;
        }

        public async Task DeleteWasteAsync(string wasteId)
        {
            var (success, error) = await _foodWasteService.DeleteAsync(wasteId);
            if (error != null)
            {
                ErrorDetail = error;
            }
            else
            {
                ErrorDetail = null;
                _allWaste = _allWaste.FindAll(w => w.id != wasteId);
                SaveCache();
                BuildGroups();
            }
        }

        public async Task<FoodWasteStatistics> LoadStatisticsAsync()
        {
            var (stats, _) = await _foodWasteService.GetStatisticsAsync(
                string.IsNullOrEmpty(FilterDateFrom) ? null : FilterDateFrom,
                string.IsNullOrEmpty(FilterDateTo) ? null : FilterDateTo);
            return stats;
        }

        private static string GetMonthKey(string isoDate)
        {
            if (string.IsNullOrEmpty(isoDate)) return LocalizationSettings.StringDatabase.GetLocalizedString("UI", "UNKNOWN");
            if (DateTime.TryParse(isoDate, out DateTime dt))
                return dt.ToString("yyyy-MM");
            return LocalizationSettings.StringDatabase.GetLocalizedString("UI", "UNKNOWN");
        }
    }

    public class FoodWasteGroup
    {
        public string MonthKey;
        public List<FoodWaste> Items;
    }
}
