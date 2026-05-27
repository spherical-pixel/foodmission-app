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
    public partial class FoodWasteAddViewModel : ViewModelBase
    {
        private readonly IFoodWasteService _foodWasteService;
        private readonly IPantryService _pantryService;

        private PantryItem[] _pantryItems = Array.Empty<PantryItem>();

        [ObservableProperty]
        private List<string> m_PantryItemOptions = new();

        [ObservableProperty]
        private int m_SelectedPantryIndex = -1;

        [ObservableProperty]
        private string m_WasteReason = eu.foodmission.platform.WasteReason.Expired;

        [ObservableProperty]
        private string m_DetectionMethod = eu.foodmission.platform.DetectionMethod.Manual;

        [ObservableProperty]
        private float m_Quantity;

        [ObservableProperty]
        private float m_MaxQuantity;

        [ObservableProperty]
        private string m_CostEstimate = "";

        [ObservableProperty]
        private string m_Notes = "";

        [ObservableProperty]
        private bool m_IsLoading;

        [ObservableProperty]
        private bool m_IsSaving;

        [ObservableProperty]
        private string m_ErrorMessage = "";

        [ObservableProperty]
        private string m_SelectedFoodName = "";

        [ObservableProperty]
        private ApiErrorResponse m_ErrorDetail;

        public FoodWasteAddViewModel(
            IStoreService storeService,
            IFoodWasteService foodWasteService,
            IPantryService pantryService)
            : base(storeService)
        {
            _foodWasteService = foodWasteService;
            _pantryService = pantryService;
        }

        public async Task LoadPantryItemsAsync()
        {
            IsLoading = true;
            ErrorMessage = "";

            var (items, _) = await _pantryService.GetItemsAsync();
            _pantryItems = items ?? Array.Empty<PantryItem>();

            PantryItemOptions = _pantryItems.Select(item =>
            {
                string name = item.foodProductId ?? item.genericFoodId ?? LocalizationSettings.StringDatabase.GetLocalizedString("UI", "UNKNOWN");
                return $"{item.quantity} {item.unit} — {name}";
            }).ToList();

            IsLoading = false;
        }

        public void OnPantryItemSelected(int index)
        {
            SelectedPantryIndex = index;
            if (index >= 0 && index < _pantryItems.Length)
            {
                MaxQuantity = _pantryItems[index].quantity;
                Quantity = _pantryItems[index].quantity;
                SelectedFoodName = _pantryItems[index].foodProductId ?? _pantryItems[index].genericFoodId ?? LocalizationSettings.StringDatabase.GetLocalizedString("UI", "UNKNOWN");
            }
            else
            {
                MaxQuantity = 0;
                Quantity = 0;
                SelectedFoodName = "";
            }
        }

        public async Task<bool> SaveAsync()
        {
            if (SelectedPantryIndex < 0 || SelectedPantryIndex >= _pantryItems.Length)
            {
                ErrorMessage = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "SELECT_PANTRY_FIRST");
                return false;
            }

            if (Quantity <= 0)
            {
                ErrorMessage = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "QTY_MUST_BE_POSITIVE");
                return false;
            }

            ErrorMessage = "";
            IsSaving = true;

            PantryItem selected = _pantryItems[SelectedPantryIndex];

            float cost = 0;
            if (!string.IsNullOrEmpty(CostEstimate) && float.TryParse(CostEstimate, out float parsedCost))
                cost = parsedCost;

            var request = new CreateFoodWasteRequest
            {
                pantryItemId = selected.id,
                quantity = Quantity,
                unit = selected.unit,
                wasteReason = WasteReason,
                detectionMethod = DetectionMethod,
                notes = string.IsNullOrEmpty(Notes) ? null : Notes,
                costEstimate = cost > 0 ? cost : null,
                wastedAt = DateTime.UtcNow.ToString("o")
            };

            var (created, error) = await _foodWasteService.CreateAsync(request);
            IsSaving = false;

            if (error != null)
            {
                ErrorDetail = error;
                ErrorMessage = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "COULD_NOT_SAVE_WASTE");
                return false;
            }

            ErrorDetail = null;
            return true;
        }

        public void Reset()
        {
            SelectedPantryIndex = -1;
            WasteReason = eu.foodmission.platform.WasteReason.Expired;
            DetectionMethod = eu.foodmission.platform.DetectionMethod.Manual;
            Quantity = 0;
            MaxQuantity = 0;
            CostEstimate = "";
            Notes = "";
            ErrorMessage = "";
            SelectedFoodName = "";
        }
    }
}
