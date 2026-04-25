using System.Threading.Tasks;

using Unity.AppUI.MVVM;

using UnityEngine;

namespace eu.foodmission.platform
{
    [ObservableObject]
    public partial class PantryItemDetailViewModel : ViewModelBase
    {
        private readonly IPantryService _pantryService;
        private readonly IFoodService _foodService;
        private readonly IFoodCategoryService _foodCategoryService;

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

        public PantryItemDetailViewModel(
            IStoreService storeService,
            IPantryService pantryService,
            IFoodService foodService,
            IFoodCategoryService foodCategoryService)
            : base(storeService)
        {
            _pantryService = pantryService;
            _foodService = foodService;
            _foodCategoryService = foodCategoryService;
        }

        public async Task LoadAsync(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                return;
            }

            _itemId = itemId;
            IsLoading = true;

            PantryItem item = await _pantryService.GetItemAsync(_itemId);

            IsLoading = false;

            if (item == null)
            {
                return;
            }

            Quantity = item.quantity;
            Unit = item.unit ?? "";
            Notes = item.notes ?? "";
            Location = item.location ?? "";
            ExpiryDate = item.expiryDate ?? "";

            string displayName = "Unknown";

            if (!string.IsNullOrEmpty(item.foodId))
            {
                FoodItem food = await _foodService.GetFoodByIdAsync(item.foodId);
                displayName = food?.name ?? "Unknown";
            }
            else if (!string.IsNullOrEmpty(item.foodCategoryId))
            {
                FoodCategory category = await _foodCategoryService.GetCategoryByIdAsync(item.foodCategoryId);
                displayName = category?.name ?? "Unknown";
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

            PantryItem updated = await _pantryService.UpdateItemAsync(
                _itemId,
                Quantity,
                Unit,
                Notes,
                Location,
                ExpiryDate);

            IsSaving = false;

            if (updated != null)
            {
                await LoadAsync(_itemId);
            }
        }

        public async Task DeleteAsync()
        {
            bool success = await _pantryService.DeleteItemAsync(_itemId);

            if (success)
            {
                RaiseNavigationRequested(Unity.AppUI.Navigation.Generated.Actions.go_to_pantry);
            }
        }
    }
}
