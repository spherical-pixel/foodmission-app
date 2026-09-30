using System;
using System.Collections.Generic;

using Unity.AppUI.Navigation.Generated;

namespace eu.foodmission.platform
{
    /// <summary>
    /// Code → interaction for every challenge (spec: _desarrollo/specs/2026-09-30-challenges-restructure-design.md).
    /// Codes not listed here are plain Confirm challenges, so new backend challenges always work.
    /// To upgrade a challenge (e.g. to a minigame), change its single entry.
    /// </summary>
    public static class ChallengeInteractionCatalog
    {
        private static readonly Dictionary<string, ChallengeInteraction> s_Entries = new(StringComparer.OrdinalIgnoreCase)
        {
            { "CH.A1.3", Recipes() },
            { "CH.A1.4", FoodFact("FF1.2.8") },
            { "CH.A1.5", Compare("CHALLENGE_BTN_COMPARE_FOODS", "proteins") },
            { "CH.A2.1", ShoppingList() },
            { "CH.A2.4", Search() },
            { "CH.A2.5", Search(ChallengeCompletionTrigger.ProductViewed, 1) },
            { "CH.A3.1", Search(ChallengeCompletionTrigger.ProductViewed, 1) },
            { "CH.A3.2", Search(ChallengeCompletionTrigger.ProductViewed, 1) },
            { "CH.A3.3", Search(ChallengeCompletionTrigger.ProductViewed, 1) },
            { "CH.A3.4", Search() },
            { "CH.A3.5", Search() },
            { "CH.A5.2", Pantry() },
            { "CH.A5.4", Pantry() },
            { "CH.A5.5", Pantry() },
            { "CH.A6.1", Pantry() },
            { "CH.A6.2", Pantry() },
            { "CH.A6.3", Search(ChallengeCompletionTrigger.ProductViewed, 1) },
            { "CH.A6.4", MealLog() },
            { "CH.A6.5", Search(ChallengeCompletionTrigger.ProductViewed, 3) },
            { "CH.B1.1", Compare("CHALLENGE_BTN_COMPARE_PROTEINS", "proteins") },
            { "CH.B1.2", Pantry() },
            { "CH.B1.3", FoodFact("FF1.2.2") },
            { "CH.B1.4", MealLog() },
            { "CH.B1.5", Recipes() },
            { "CH.B2.1", Search(ChallengeCompletionTrigger.ProductViewed, 1) },
            { "CH.B2.2", Search(ChallengeCompletionTrigger.ProductViewed, 3) },
            { "CH.B2.4", Search() },
            { "CH.B2.5", Search(ChallengeCompletionTrigger.ProductViewed, 1) },
            { "CH.B3.1", Search(ChallengeCompletionTrigger.ProductViewed, 1) },
            { "CH.B3.2", Search(ChallengeCompletionTrigger.ProductViewed, 1) },
            { "CH.B3.3", Search(ChallengeCompletionTrigger.ProductViewed, 1) },
            { "CH.B3.5", Search() },
            { "CH.B5.1", Pantry() },
            { "CH.B5.2", Pantry() },
            { "CH.B5.3", LogWaste(3) },
            { "CH.B5.5", WasteLog() },
            { "CH.B6.1", Pantry() },
            { "CH.B6.2", Search(ChallengeCompletionTrigger.ProductViewed, 2) },
            { "CH.B6.3", Pantry() },
            { "CH.B6.4", Search(ChallengeCompletionTrigger.ProductViewed, 1) },
            { "CH.B6.5", Search() },
            { "CH.I1.1", Recipes() },
            { "CH.I1.2", Search(ChallengeCompletionTrigger.ProductViewed, 2) },
            { "CH.I1.3", FoodFact("FF1.3.1") },
            { "CH.I1.4", ShoppingList() },
            { "CH.I1.5", Recipes(ChallengeCompletionTrigger.RecipeViewed) },
            { "CH.I2.1", Search(ChallengeCompletionTrigger.ProductViewed, 1) },
            { "CH.I2.2", Search(ChallengeCompletionTrigger.ProductViewed, 2) },
            { "CH.I2.3", Search(ChallengeCompletionTrigger.ProductViewed, 2) },
            { "CH.I2.4", Search() },
            { "CH.I3.1", Search(ChallengeCompletionTrigger.ProductViewed, 3) },
            { "CH.I3.2", Search(ChallengeCompletionTrigger.ProductViewed, 1) },
            { "CH.I3.3", Search() },
            { "CH.I3.4", Search() },
            { "CH.I3.5", Search(ChallengeCompletionTrigger.ProductViewed, 2) },
            { "CH.I5.1", WasteLog() },
            { "CH.I5.3", FoodFact("FF5.2.8") },
            { "CH.I5.4", Pantry() },
            { "CH.I5.5", LogWaste(1) },
            { "CH.I6.1", Search(ChallengeCompletionTrigger.ProductViewed, 1) },
            { "CH.I6.2", Search(ChallengeCompletionTrigger.ProductViewed, 2) },
            { "CH.I6.3", Search(ChallengeCompletionTrigger.ProductViewed, 2) },
            { "CH.I6.4", Search(ChallengeCompletionTrigger.ProductViewed, 1) },
            { "CH.I6.5", Search(ChallengeCompletionTrigger.ProductViewed, 2) },
        };

        public static IReadOnlyDictionary<string, ChallengeInteraction> Entries => s_Entries;

        public static ChallengeInteraction Get(string challengeCode)
        {
            if (string.IsNullOrWhiteSpace(challengeCode))
            {
                return ChallengeInteraction.Confirm;
            }

            return s_Entries.TryGetValue(challengeCode.Trim(), out ChallengeInteraction interaction)
                ? interaction
                : ChallengeInteraction.Confirm;
        }

        private static ChallengeInteraction Module(string action, string buttonKey,
            ChallengeCompletionTrigger trigger = ChallengeCompletionTrigger.Manual, int requiredCount = 1, string foodFactCode = null)
        {
            return new ChallengeInteraction(ChallengeInteractionType.ConfirmWithModule, action, buttonKey,
                trigger, trigger == ChallengeCompletionTrigger.Manual ? 0 : requiredCount, foodFactCode, null);
        }

        private static ChallengeInteraction Search(ChallengeCompletionTrigger trigger = ChallengeCompletionTrigger.Manual, int requiredCount = 1)
        {
            return Module(Actions.go_to_quicksearch, "CHALLENGE_BTN_OPEN_SEARCH", trigger, requiredCount);
        }

        private static ChallengeInteraction Pantry()
        {
            return Module(Actions.go_to_pantry, "CHALLENGE_BTN_OPEN_PANTRY");
        }

        private static ChallengeInteraction Recipes(ChallengeCompletionTrigger trigger = ChallengeCompletionTrigger.Manual)
        {
            return Module(Actions.go_to_recipes, "CHALLENGE_BTN_OPEN_RECIPES", trigger);
        }

        private static ChallengeInteraction FoodFact(string foodFactCode)
        {
            return Module(Actions.open_food_fact, "CHALLENGE_BTN_READ_FOOD_FACT", ChallengeCompletionTrigger.FoodFactRead, 1, foodFactCode);
        }

        private static ChallengeInteraction LogWaste(int requiredCount)
        {
            return Module(Actions.go_to_foodwaste_add, "CHALLENGE_BTN_LOG_WASTE", ChallengeCompletionTrigger.FoodWasteLogged, requiredCount);
        }

        private static ChallengeInteraction WasteLog()
        {
            return Module(Actions.go_to_foodwaste, "CHALLENGE_BTN_OPEN_WASTE");
        }

        private static ChallengeInteraction MealLog()
        {
            return Module(Actions.go_to_meallog, "CHALLENGE_BTN_OPEN_MEAL_LOG");
        }

        private static ChallengeInteraction ShoppingList()
        {
            return Module(Actions.go_to_shopping_list, "CHALLENGE_BTN_OPEN_SHOPPING_LIST");
        }

        private static ChallengeInteraction Compare(string buttonKey, string comparisonMode)
        {
            return new ChallengeInteraction(ChallengeInteractionType.Comparator, Actions.go_to_food_comparison, buttonKey,
                ChallengeCompletionTrigger.ComparisonDone, 1, null, comparisonMode);
        }
    }
}
