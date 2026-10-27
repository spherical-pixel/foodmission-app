using System;
using System.Collections.Generic;

namespace eu.foodmission.platform
{
    /// <summary>Swaps whose result is still meat (beef → chicken…): the meal had meat, so it can't be meat-free.</summary>
    public static class MeatSwaps
    {
        private static readonly HashSet<string> s_EndInMeat = new(StringComparer.Ordinal)
        {
            ClientEventTypes.SwapBeefToPork, ClientEventTypes.SwapBeefToChicken, ClientEventTypes.SwapPorkToChicken
        };

        public static bool EndsInMeat(string swap) => swap != null && s_EndInMeat.Contains(swap);
    }
}
