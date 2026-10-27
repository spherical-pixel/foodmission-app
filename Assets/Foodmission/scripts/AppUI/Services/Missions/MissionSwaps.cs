using System;
using System.Collections.Generic;

namespace eu.foodmission.platform
{
    /// <summary>Swaps asked per day on the check-in (spec 2026-10-27 swaps).</summary>
    public static class MissionSwaps
    {
        public const string PromptKey = "MISSION_QDAY_SWAPS";
        /// <summary>Most units of one repeatable swap told for one day.</summary>
        public const int MaxPerDay = 5;

        private const string MealPromptKey = "MISSION_Q_SWAPS";

        // Done several times a day (drinks, snacks); the other swaps belong to one meal
        private static readonly HashSet<string> s_Repeatable = new HashSet<string>(StringComparer.Ordinal)
        {
            ClientEventTypes.SwapSugaryDrinkToWater, ClientEventTypes.SwapSnackToFruitNuts
        };

        // Order of the shared swap question: processing swaps, then protein swaps
        private static readonly string[] s_Order =
        {
            ClientEventTypes.SwapSugaryDrinkToWater, ClientEventTypes.SwapSnackToFruitNuts, ClientEventTypes.SwapSugaryCerealToOats,
            ClientEventTypes.SwapReadyMealToHomecooked, ClientEventTypes.SwapProcessedMeatToLegumes,
            ClientEventTypes.SwapBeefToPork, ClientEventTypes.SwapBeefToChicken, ClientEventTypes.SwapBeefToLegumes,
            ClientEventTypes.SwapPorkToChicken, ClientEventTypes.SwapPorkToLegumes, ClientEventTypes.SwapChickenToLegumes
        };

        public static bool IsRepeatable(string swap) => swap != null && s_Repeatable.Contains(swap);

        /// <summary>Position in the shared swap question (unknown swaps last).</summary>
        public static int OrderOf(string swap)
        {
            int index = Array.IndexOf(s_Order, swap);
            return index < 0 ? s_Order.Length : index;
        }

        /// <summary>The same swaps asked per meal, when another mission of the check-in asks per meal anyway.</summary>
        public static MissionReportStep AsMealReport(MissionReportStep daySwaps) =>
            new MissionReportStep(MissionStepType.MealReport, MealPromptKey, null, null, daySwaps.FixedMetadata, daySwaps.Options,
                daySwaps.MaxCount, false, null);
    }
}
