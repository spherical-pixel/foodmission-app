using System.Collections.Generic;
using System.Threading.Tasks;

using Unity.AppUI.MVVM;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace eu.foodmission.platform
{
    [ObservableObject]
    public partial class ShoppingListViewModel : ViewModelBase
    {
        private readonly IShoppingListService _shoppingListService;
        private readonly ILocalStorageService _localStorage;

        private const string CacheKey = "shoppinglists_cache";

        private List<ShoppingList> _allLists = new();

        [ObservableProperty]
        private List<ShoppingList> _lists = new();

        [ObservableProperty]
        private string _searchText = "";

        

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string _errorMessage = "";

        [ObservableProperty]
        private ApiErrorResponse m_ErrorDetail;

        public ShoppingListViewModel(
            IStoreService storeService,
            IShoppingListService shoppingListService,
            ILocalStorageService localStorage)
            : base(storeService)
        {
            _shoppingListService = shoppingListService;
            _localStorage = localStorage;
        }

        public async Task LoadListsAsync()
        {
            IsLoading = true;
            ErrorMessage = "";

            var (lists, error) = await _shoppingListService.GetListsAsync();

            if (error != null)
            {
                ErrorDetail = error;
                ShoppingList[] cached = _localStorage.GetValue<ShoppingListPagedResponse>(CacheKey)?.data;
                _allLists = cached != null ? new List<ShoppingList>(cached) : new List<ShoppingList>();
                ApplyFilter();

                if (cached == null || cached.Length == 0)
                {
                    ErrorMessage = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "ERROR_LOADING_LISTS");
                }

                IsLoading = false;
                return;
            }

            ErrorDetail = null;
            _localStorage.SetValue(CacheKey, new ShoppingListPagedResponse { data = lists });
            _allLists = new List<ShoppingList>(lists);
            ApplyFilter();
            IsLoading = false;
        }

        public async Task CreateListAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                ErrorMessage = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "LIST_NAME_REQUIRED");
                return;
            }

            ErrorMessage = "";
            IsLoading = true;
            var (created, error) = await _shoppingListService.CreateListAsync(name);
            IsLoading = false;

            if (error != null)
            {
                ErrorDetail = error;
                ErrorMessage = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "COULD_NOT_CREATE_LIST");
                return;
            }

            ErrorDetail = null;
            SearchText = "";
            await LoadListsAsync();
        }

        public async Task DeleteListAsync(string id)
        {
            ErrorMessage = "";
            IsLoading = true;
            var (success, error) = await _shoppingListService.DeleteListAsync(id);
            IsLoading = false;

            if (error != null)
            {
                ErrorDetail = error;
                ErrorMessage = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "COULD_NOT_DELETE_LIST");
                return;
            }

            ErrorDetail = null;
            _allLists.RemoveAll(l => l.id == id);
            ApplyFilter();
            SaveCache();
        }

        public void ApplyFilter()
        {
            if (string.IsNullOrWhiteSpace(_searchText))
            {
                Lists = new List<ShoppingList>(_allLists);
            }
            else
            {
                Lists = _allLists.FindAll(l =>
                    l.title.Contains(_searchText, System.StringComparison.OrdinalIgnoreCase));
            }
        }

        private void SaveCache()
        {
            _localStorage.SetValue(CacheKey, new ShoppingListPagedResponse { data = _allLists.ToArray() });
        }
    }
}
