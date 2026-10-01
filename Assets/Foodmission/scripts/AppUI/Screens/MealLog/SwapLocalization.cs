using UnityEngine.Localization.Settings;

namespace eu.foodmission.platform
{
    /// <summary>UI.csv keys and labels of the swap events (SWAP_*).</summary>
    public static class SwapLocalization
    {
        public static string GetSwapLocalizationKey(string eventType)
        {
            return eventType switch
            {
                ClientEventTypes.SwapBeefToLegumes => "SWAP_BEEF_TO_LEGUMES",
                ClientEventTypes.SwapBeefToChicken => "SWAP_BEEF_TO_CHICKEN",
                ClientEventTypes.SwapBeefToPork => "SWAP_BEEF_TO_PORK",
                ClientEventTypes.SwapPorkToLegumes => "SWAP_PORK_TO_LEGUMES",
                ClientEventTypes.SwapPorkToChicken => "SWAP_PORK_TO_CHICKEN",
                ClientEventTypes.SwapChickenToLegumes => "SWAP_CHICKEN_TO_LEGUMES",
                ClientEventTypes.SwapSugaryDrinkToWater => "SWAP_SUGARY_DRINK_TO_WATER",
                ClientEventTypes.SwapSnackToFruitNuts => "SWAP_SNACK_TO_FRUIT_NUTS",
                ClientEventTypes.SwapSugaryCerealToOats => "SWAP_SUGARY_CEREAL_TO_OATS",
                ClientEventTypes.SwapReadyMealToHomecooked => "SWAP_READY_MEAL_TO_HOMECOOKED",
                ClientEventTypes.SwapProcessedMeatToLegumes => "SWAP_PROCESSED_MEAT_TO_LEGUMES",
                _ => null
            };
        }

        public static string GetSwapLocalizationTag(string eventType)
        {
            string key = GetSwapLocalizationKey(eventType);
            return !string.IsNullOrEmpty(key) ? $"@UI:{key}" : null;
        }

        public static string GetSwapDisplayName(string eventType)
        {
            string key = GetSwapLocalizationKey(eventType);
            return !string.IsNullOrEmpty(key)
                ? LocalizationSettings.StringDatabase.GetLocalizedString("UI", key)
                : eventType;
        }
    }
}
