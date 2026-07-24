using System.Threading.Tasks;

using Unity.AppUI.MVVM;

using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace eu.foodmission.platform
{
    [ObservableObject]
    public partial class PantryItemDetailViewModel : ViewModelBase
    {
        private readonly IPantryService _pantryService;
        private readonly IFoodProductService _foodProductService;
        private readonly IGenericFoodService _genericFoodService;

        private string _itemId;

        [ObservableProperty]
        private PantryItemView m_ItemView;

        [ObservableProperty]
        private float m_Quantity;

        [ObservableProperty]
        private string m_Unit = "";

        [ObservableProperty]
        private string m_Notes = "";

        [ObservableProperty]
        private string m_Location = "";

        [ObservableProperty]
        private string m_ExpiryDate = "";

        [ObservableProperty]
        private bool m_IsLoading;

        [ObservableProperty]
        private bool m_IsSaving;

        [ObservableProperty]
        private string m_ErrorMessage = "";

        [ObservableProperty]
        private ApiErrorResponse m_ErrorDetail;

        public PantryItemDetailViewModel(
            IStoreService storeService,
            IPantryService pantryService,
            IFoodProductService foodProductService,
            IGenericFoodService genericFoodService)
            : base(storeService)
        {
            _pantryService = pantryService;
            _foodProductService = foodProductService;
            _genericFoodService = genericFoodService;
        }

        public async Task LoadAsync(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                return;
            }

            _itemId = itemId;
            IsLoading = true;

            var (item, error) = await _pantryService.GetItemAsync(_itemId);

            IsLoading = false;

            if (error != null)
            {
                ErrorMessage = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "COULD_NOT_LOAD_ITEM");
                ErrorDetail = error;
                return;
            }

            ErrorMessage = "";
            ErrorDetail = null;
            Quantity = item.quantity;
            Unit = item.unit ?? "";
            Notes = item.notes ?? "";
            Location = item.location ?? "";
            ExpiryDate = item.expiryDate ?? "";

            string displayName = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "UNKNOWN");

            if (!string.IsNullOrEmpty(item.foodProductId))
            {
                var (food, _) = await _foodProductService.GetFoodByIdAsync(item.foodProductId);
                displayName = food?.name ?? LocalizationSettings.StringDatabase.GetLocalizedString("UI", "UNKNOWN");
            }
            else if (!string.IsNullOrEmpty(item.genericFoodId))
            {
                var (genericFood, _) = await _genericFoodService.GetGenericFoodByIdAsync(item.genericFoodId);
                displayName = genericFood?.foodName ?? LocalizationSettings.StringDatabase.GetLocalizedString("UI", "UNKNOWN");
            }

            ItemView = new PantryItemView
            {
                Item = item,
                DisplayName = displayName,
                ImageUrl = null
            };
        }

        public async Task SaveAsync()
        {
            if (string.IsNullOrEmpty(_itemId))
            {
                return;
            }

            IsSaving = true;

            PantryItem item = ItemView?.Item;
            var (updated, error) = await _pantryService.UpdateItemAsync(
                _itemId,
                Quantity,
                Unit,
                Notes,
                Location,
                ExpiryDate);

            IsSaving = false;

            if (error != null)
            {
                ErrorMessage = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "COULD_NOT_SAVE_ITEM");
                ErrorDetail = error;
            }
            else
            {
                ErrorMessage = "";
                ErrorDetail = null;
                await LoadAsync(_itemId);
            }
        }

        public async Task DeleteAsync()
        {
            var (success, error) = await _pantryService.DeleteItemAsync(_itemId);

            if (error != null)
            {
                ErrorDetail = error;
            }
            else
            {
                ErrorDetail = null;
            }
        }
    }
}
