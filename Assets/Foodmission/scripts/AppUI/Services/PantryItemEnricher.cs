using System;
using System.Threading.Tasks;

using UnityEngine.Localization.Settings;

namespace eu.foodmission.platform
{
    public class PantryItemEnricher : IPantryItemEnricher
    {
        private readonly IFoodProductService _foodProductService;
        private readonly IGenericFoodService _genericFoodService;

        public PantryItemEnricher(IFoodProductService foodProductService, IGenericFoodService genericFoodService)
        {
            _foodProductService = foodProductService;
            _genericFoodService = genericFoodService;
        }

        public async Task<PantryItemView[]> EnrichAsync(PantryItem[] items)
        {
            if (items == null || items.Length == 0)
            {
                return Array.Empty<PantryItemView>();
            }

            Task<PantryItemView>[] tasks = new Task<PantryItemView>[items.Length];
            for (int i = 0; i < items.Length; i++)
            {
                tasks[i] = EnrichAsync(items[i]);
            }

            return await Task.WhenAll(tasks);
        }

        public async Task<PantryItemView> EnrichAsync(PantryItem item)
        {
            string displayName = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "UNKNOWN");
            string imageUrl = null;

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

            return new PantryItemView
            {
                Item = item,
                DisplayName = displayName,
                ImageUrl = imageUrl
            };
        }
    }
}
