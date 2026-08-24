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
        private readonly INotificationService _notificationService;
        private readonly IFoodWasteService _foodWasteService;
        private readonly IMealService _mealService;
        private readonly IMealLogService _mealLogService;
        private readonly IMealItemService _mealItemService;

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
            IGenericFoodService genericFoodService,
            INotificationService notificationService = null,
            IFoodWasteService foodWasteService = null,
            IMealService mealService = null,
            IMealLogService mealLogService = null,
            IMealItemService mealItemService = null)
            : base(storeService)
        {
            _pantryService = pantryService;
            _foodProductService = foodProductService;
            _genericFoodService = genericFoodService;
            _notificationService = notificationService;
            _foodWasteService = foodWasteService;
            _mealService = mealService;
            _mealLogService = mealLogService;
            _mealItemService = mealItemService;
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

                // Sync local notification schedule if expiry date changed
                if (_notificationService != null && _notificationService.AreNotificationsEnabled())
                {
                    _notificationService.CancelPantryReminder(_itemId);
                    if (!string.IsNullOrEmpty(ExpiryDate) && System.DateTime.TryParse(ExpiryDate, out System.DateTime newExpDate))
                    {
                        _notificationService.SchedulePantryExpiryReminder(_itemId, ItemView?.DisplayName ?? "Item", newExpDate);
                    }
                }

                await LoadAsync(_itemId);
            }
        }

        public async Task<bool> ConsumeAsync()
        {
            if (string.IsNullOrEmpty(_itemId) || ItemView?.Item == null) return false;
            ErrorMessage = "";
            string displayName = !string.IsNullOrEmpty(ItemView.DisplayName)
                ? ItemView.DisplayName
                : LocalizationSettings.StringDatabase.GetLocalizedString("UI", "UNKNOWN");

            try
            {
                string mealId = null;
                if (_mealService != null)
                {
                    var (createdMeal, mealErr) = await _mealService.CreateMealAsync(new CreateMealRequest
                    {
                        name = displayName
                    });
                    if (mealErr != null)
                    {
                        ErrorDetail = mealErr;
                        return false;
                    }
                    mealId = createdMeal?.id;
                }

                if (!string.IsNullOrEmpty(mealId) && _mealItemService != null)
                {
                    var itemReq = new CreateMealItemRequest
                    {
                        foodProductId = !string.IsNullOrEmpty(ItemView.Item.foodProductId) ? ItemView.Item.foodProductId : null,
                        genericFoodId = !string.IsNullOrEmpty(ItemView.Item.genericFoodId) ? ItemView.Item.genericFoodId : null,
                        quantity = (int)Mathf.Max(1, Mathf.Round(Quantity > 0 ? Quantity : 1)),
                        unit = !string.IsNullOrEmpty(Unit) ? Unit : "PIECES"
                    };
                    var (_, itemErr) = await _mealItemService.CreateAsync(mealId, itemReq);
                    if (itemErr != null)
                    {
                        ErrorDetail = itemErr;
                        return false;
                    }
                }

                if (!string.IsNullOrEmpty(mealId) && _mealLogService != null)
                {
                    int hour = System.DateTime.Now.Hour;
                    string typeOfMeal = hour switch
                    {
                        >= 6 and < 12 => "BREAKFAST",
                        >= 12 and < 17 => "LUNCH",
                        _ => "DINNER"
                    };

                    var logReq = new CreateMealLogRequest
                    {
                        mealId = mealId,
                        typeOfMeal = typeOfMeal,
                        timestamp = System.DateTime.UtcNow.ToString("o"),
                        mealFromPantry = true,
                        eatenOut = false
                    };
                    var (_, logErr) = await _mealLogService.CreateAsync(logReq);
                    if (logErr != null)
                    {
                        ErrorDetail = logErr;
                        return false;
                    }
                }

                var (deleted, delErr) = await _pantryService.DeleteItemAsync(_itemId);
                if (delErr != null)
                {
                    ErrorDetail = delErr;
                    return false;
                }

                _notificationService?.CancelPantryReminder(_itemId);
                ErrorDetail = null;
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] ConsumeAsync failed: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> WasteAsync(
            string reason = WasteReason.Expired,
            float? costEstimate = null,
            string notes = null,
            float? quantity = null)
        {
            if (string.IsNullOrEmpty(_itemId) || ItemView?.Item == null) return false;
            ErrorMessage = "";

            try
            {
                float wastedQty = quantity.HasValue && quantity.Value > 0
                    ? quantity.Value
                    : (Quantity > 0 ? Quantity : ItemView.Item.quantity);

                if (_foodWasteService != null)
                {
                    var req = new CreateFoodWasteRequest
                    {
                        pantryItemId = _itemId,
                        quantity = wastedQty,
                        unit = !string.IsNullOrEmpty(Unit) ? Unit : ItemView.Item.unit,
                        wasteReason = string.IsNullOrEmpty(reason) ? WasteReason.Expired : reason,
                        detectionMethod = DetectionMethod.Manual,
                        costEstimate = costEstimate.HasValue && costEstimate.Value > 0 ? costEstimate.Value : null,
                        notes = string.IsNullOrWhiteSpace(notes) ? null : notes,
                        wastedAt = System.DateTime.UtcNow.ToString("o")
                    };

                    var (created, wasteErr) = await _foodWasteService.CreateAsync(req);
                    if (wasteErr != null)
                    {
                        ErrorDetail = wasteErr;
                        return false;
                    }
                }
                else
                {
                    var (deleted, delErr) = await _pantryService.DeleteItemAsync(_itemId);
                    if (delErr != null)
                    {
                        ErrorDetail = delErr;
                        return false;
                    }
                }

                _notificationService?.CancelPantryReminder(_itemId);
                ErrorDetail = null;
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] WasteAsync failed: {ex.Message}");
                return false;
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
                _notificationService?.CancelPantryReminder(_itemId);
                ErrorDetail = null;
            }
        }
    }
}
