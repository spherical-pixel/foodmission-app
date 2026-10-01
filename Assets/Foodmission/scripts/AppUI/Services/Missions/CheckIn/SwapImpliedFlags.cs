using System;
using System.Collections.Generic;
using System.Linq;

namespace eu.foodmission.platform
{
    /// <summary>
    /// True facts a swap implies about the meal (e.g. beef → legumes means the meal had legumes).
    /// Added to swaps-only meal logs because backend v0.3.0 requires at least one flag; harmless once it doesn't.
    /// </summary>
    public static class SwapImpliedFlags
    {
        private static readonly Dictionary<string, string> s_Flags = new(StringComparer.Ordinal)
        {
            { ClientEventTypes.SwapBeefToLegumes, ClientEventTypes.MealLegumeConsumed },
            { ClientEventTypes.SwapPorkToLegumes, ClientEventTypes.MealLegumeConsumed },
            { ClientEventTypes.SwapChickenToLegumes, ClientEventTypes.MealLegumeConsumed },
            { ClientEventTypes.SwapProcessedMeatToLegumes, ClientEventTypes.MealLegumeConsumed },
            { ClientEventTypes.SwapBeefToChicken, ClientEventTypes.MealMeatConsumed },
            { ClientEventTypes.SwapBeefToPork, ClientEventTypes.MealMeatConsumed },
            { ClientEventTypes.SwapPorkToChicken, ClientEventTypes.MealMeatConsumed },
            { ClientEventTypes.SwapSugaryCerealToOats, ClientEventTypes.NutritionWholegrainChosen },
        };

        public static IReadOnlyList<string> For(IEnumerable<string> swaps) =>
            (swaps ?? Array.Empty<string>())
                .Where(s => s != null && s_Flags.ContainsKey(s))
                .Select(s => s_Flags[s])
                .Distinct()
                .ToList();
    }
}
