using System;
using System.Collections.Generic;
using System.Linq;

using Unity.AppUI.Navigation.Generated;

namespace eu.foodmission.platform
{
    /// <summary>
    /// Code → interaction for every mission (spec: _desarrollo/specs/2026-10-01-missions-reporting-design.md, Annex A).
    /// Unknown codes are PendingRule, so a new backend mission never breaks the app.
    /// To change how one mission is reported (or make it a minigame), change its single entry.
    /// </summary>
    public static class MissionInteractionCatalog
    {
        public static readonly MissionModuleLink QuickMealLog = new MissionModuleLink(Actions.open_quick_meal_log, "MISSION_BTN_QUICK_MEAL_LOG");
        private static readonly MissionModuleLink Search = new MissionModuleLink(Actions.go_to_quicksearch, "CHALLENGE_BTN_OPEN_SEARCH");
        private static readonly MissionModuleLink Pantry = new MissionModuleLink(Actions.go_to_pantry, "CHALLENGE_BTN_OPEN_PANTRY");
        private static readonly MissionModuleLink Waste = new MissionModuleLink(Actions.go_to_foodwaste, "CHALLENGE_BTN_OPEN_WASTE");

        private const string ProductId = "productId";

        private static readonly IReadOnlyDictionary<string, object> NoMetadata = new Dictionary<string, object>();
        private static readonly MissionModuleLink[] NoModules = Array.Empty<MissionModuleLink>();

        private static readonly string[] ProteinSwaps =
        {
            ClientEventTypes.SwapBeefToPork, ClientEventTypes.SwapBeefToChicken, ClientEventTypes.SwapBeefToLegumes,
            ClientEventTypes.SwapPorkToChicken, ClientEventTypes.SwapPorkToLegumes, ClientEventTypes.SwapChickenToLegumes
        };

        private static readonly string[] ProcessingSwaps =
        {
            ClientEventTypes.SwapSugaryDrinkToWater, ClientEventTypes.SwapSnackToFruitNuts, ClientEventTypes.SwapSugaryCerealToOats,
            ClientEventTypes.SwapReadyMealToHomecooked, ClientEventTypes.SwapProcessedMeatToLegumes
        };

        private static readonly string[] ProteinSources =
        {
            "LEGUMES", "DAIRY", "EGGS", "FISH", "NUTS", "SEEDS", "TOFU", "LEAN_MEAT"
        };

        private static readonly Dictionary<string, MissionInteraction> s_Entries = new(StringComparer.OrdinalIgnoreCase)
        {
            // A1 — Diet (advanced)
            { "M.A1.1", Auto(Meals(ClientEventTypes.MealMeatConsumed, 7)) },
            { "M.A1.2", Auto(MealChoice("MISSION_Q_MEAL_PROTEIN", 10, false, ClientEventTypes.MealLegumeConsumed, ClientEventTypes.MealMeatConsumed)) },
            { "M.A1.3", Auto(Meals(ClientEventTypes.MealLegumeConsumed, 5)) },
            { "M.A1.4", Auto(Meals(ClientEventTypes.MealAlternativeStaple, 5)) },
            { "M.A1.5", Auto(Meals(ClientEventTypes.MealSustainablePlate, 10)) },
            // A2 — Product choices (advanced)
            { "M.A2.1", Pending(Search) },
            { "M.A2.2", Pending(Search) },
            { "M.A2.3", Helped(Search, Products(ClientEventTypes.ShoppingMulticriteriaPurchase, 3)) },
            { "M.A2.4", Auto(Meals(ClientEventTypes.MealSeasonalProduce, 4)) },
            { "M.A2.5", Pending(Search) },
            // A3 — Processing (advanced)
            { "M.A3.1", Helped(Search, Products(ClientEventTypes.ProcessingNovaScoreCompared, 3, Meta("comparedWith", "GREEN_SCORE"))) },
            { "M.A3.2", Helped(Search, Products(ClientEventTypes.ProcessingAllScoresCompared, 3)) },
            { "M.A3.3", Helped(Search,
                Products(ClientEventTypes.ProcessingNovaChecked, 3),
                Products(ClientEventTypes.ProcessingIngredientsReviewed, 3),
                Products(ClientEventTypes.ShoppingPackagingInfoChecked, 3)) },
            { "M.A3.4", Helped(Search, Products(ClientEventTypes.ProcessingIndicatorsCompared, 3)) },
            { "M.A3.5", Helped(Search, ProductChoice("MISSION_Q_PRODUCT_CHECKS", 5,
                ClientEventTypes.ProcessingNovaChecked, ClientEventTypes.ProcessingGreenscoreChecked,
                ClientEventTypes.ProcessingProductionMethodChecked, ClientEventTypes.ProcessingIndicatorsCompared)) },
            // A4 — Packaging (advanced)
            { "M.A4.1", Pending(Search) },
            { "M.A4.2", Manual(Times(ClientEventTypes.PackagingReusableSpotChosen, 5)) },
            { "M.A4.3", Pending(Search) },
            { "M.A4.4", Helped(Search, Products(ClientEventTypes.PackagingSmartObserved, 2)) },
            { "M.A4.5", Helped(Search, Products(ClientEventTypes.ShoppingPackagingInfoChecked, 5)) },
            // A5 — Food waste (advanced)
            { "M.A5.1", Auto(Meals(ClientEventTypes.FoodWasteHalfPlateSaved, 7)) },
            { "M.A5.2", Auto(Meals(ClientEventTypes.FoodWasteFullPlateSaved, 7)) },
            { "M.A5.3", Auto(Meals(ClientEventTypes.FoodWasteExpiredConsumed, 4)) },
            { "M.A5.4", Helped(Pantry, Days(ClientEventTypes.FoodWasteFifoOrganized, 3)) },
            { "M.A5.5", Helped(Pantry, Confirm(ClientEventTypes.FoodWasteMealPlanned), Confirm(ClientEventTypes.FoodWasteFridgePantryChecked)) },
            // A6 — Nutrition (advanced)
            { "M.A6.1", Pending() },
            { "M.A6.2", Auto(DailyMeals(ClientEventTypes.NutritionHighFibreMeal, "BREAKFAST")) },
            { "M.A6.3", Auto(DailyMeals(ClientEventTypes.NutritionAddedSugarAvoided)) },
            { "M.A6.4", Pending() },
            { "M.A6.5", Manual(Products(ClientEventTypes.NutritionPlantDiversityCount, 20, null, "genericFoodId")) },
            // B1 — Diet (beginner)
            { "M.B1.1", Auto(MealChoice("MISSION_Q_MEAL_MEAT", 7, true, ClientEventTypes.MealMeatFree, ClientEventTypes.MealMeatConsumed)) },
            { "M.B1.2", Auto(MealChoice("MISSION_Q_MEAL_MEAT", 7, true, ClientEventTypes.MealMeatFree, ClientEventTypes.MealMeatConsumed)) },
            { "M.B1.3", Auto(Meals(ClientEventTypes.MealMeatConsumed, 7)) },
            { "M.B1.4", Auto(SwapMeals(2, ProteinSwaps)) },
            { "M.B1.5", Auto(SwapMeals(2, ClientEventTypes.SwapBeefToChicken, ClientEventTypes.SwapBeefToLegumes)) },
            // B2 — Product choices (beginner)
            { "M.B2.1", Helped(Search, Products(ClientEventTypes.ShoppingOriginChecked, 5)) },
            { "M.B2.2", Auto(Meals(ClientEventTypes.MealSeasonalProduce, 5)) },
            { "M.B2.3", Helped(Search, Times(ClientEventTypes.ShoppingCertificationChosen, 3)) },
            { "M.B2.4", Auto(Meals(ClientEventTypes.MealLocalProduce, 3)) },
            { "M.B2.5", Helped(Search, Products(ClientEventTypes.ShoppingPackagingInfoChecked, 5)) },
            // B3 — Processing (beginner)
            { "M.B3.1", Helped(Search, Products(ClientEventTypes.ProcessingProductionMethodChecked, 5)) },
            { "M.B3.2", Helped(Search, Products(ClientEventTypes.ProcessingNovaChecked, 3)) },
            { "M.B3.3", Helped(Search, Products(ClientEventTypes.ProcessingIngredientsReviewed, 5)) },
            { "M.B3.4", Helped(Search, Products(ClientEventTypes.ProcessingGreenscoreChecked, 5)) },
            { "M.B3.5", Auto(SwapMeals(2, ProcessingSwaps)) },
            // B4 — Packaging (beginner)
            { "M.B4.1", Helped(Search, Products(ClientEventTypes.PackagingMaterialObserved, 5)) },
            { "M.B4.2", Helped(Search, Products(ClientEventTypes.PackagingRecyclingLabelRead, 10)) },
            { "M.B4.3", Helped(Search, Products(ClientEventTypes.PackagingMaterialObserved, 10)) },
            { "M.B4.4", Manual(Times(ClientEventTypes.PackagingReusableSpotChosen, 5)) },
            { "M.B4.5", Helped(Search, ProductChoice("MISSION_Q_PACKAGING_CHECKS", 10,
                ClientEventTypes.PackagingMaterialObserved, ClientEventTypes.PackagingRecyclingLabelRead, ClientEventTypes.PackagingReusableSpotChosen)) },
            // B5 — Food waste (beginner)
            { "M.B5.1", Auto(Meals(ClientEventTypes.FoodWasteHalfPlateSaved, 5)) },
            { "M.B5.2", Auto(Meals(ClientEventTypes.FoodWasteFullPlateSaved, 3)) },
            { "M.B5.3", Auto(Meals(ClientEventTypes.FoodWasteExpiredConsumed, 1)) },
            { "M.B5.4", Helped(Search, Products(ClientEventTypes.FoodWasteStorageInstructionsRead, 5)) },
            { "M.B5.5", Auto(MealChoice("MISSION_Q_FOOD_SAVING", 4, true,
                ClientEventTypes.FoodWasteHalfPlateSaved, ClientEventTypes.FoodWasteFullPlateSaved, ClientEventTypes.FoodWasteExpiredConsumed)) },
            // B6 — Nutrition (beginner)
            { "M.B6.1", Auto(DailyMeals(ClientEventTypes.NutritionProteinIncluded)) },
            { "M.B6.2", Auto(DailyMeals(ClientEventTypes.NutritionFruitVegServingAdded)) },
            { "M.B6.3", Auto(Meals(ClientEventTypes.SwapSugaryDrinkToWater, 3)) },
            { "M.B6.4", Auto(DailyMeals(ClientEventTypes.NutritionWholegrainChosen)) },
            { "M.B6.5", Auto(Meals(ClientEventTypes.SwapSnackToFruitNuts, 5)) },
            // I1 — Diet (intermediate)
            { "M.I1.1", Auto(SwapMeals(4, ProteinSwaps)) },
            { "M.I1.2", Auto(SwapMeals(6, ClientEventTypes.SwapBeefToPork, ClientEventTypes.SwapBeefToChicken, ClientEventTypes.SwapBeefToLegumes,
                ClientEventTypes.SwapPorkToLegumes, ClientEventTypes.SwapChickenToLegumes)) },
            { "M.I1.3", Auto(MealChoice("MISSION_Q_MEAL_MEAT", 14, false, ClientEventTypes.MealMeatFree, ClientEventTypes.MealMeatConsumed)) },
            { "M.I1.4", Auto(Meals(ClientEventTypes.MealLegumeConsumed, 4)) },
            { "M.I1.5", Manual(ProteinSourceChoice()) },
            // I2 — Product choices (intermediate)
            { "M.I2.1", Auto(Meals(ClientEventTypes.MealLocalProduce, 3)) },
            { "M.I2.2", Auto(Meals(ClientEventTypes.MealSeasonalProduce, 8)) },
            { "M.I2.3", Auto(Meals(ClientEventTypes.MealCertifiedProduct, 3)) },
            { "M.I2.4", Pending(Search) },
            { "M.I2.5", Helped(Search, Times(ClientEventTypes.ShoppingPackagingInfoChecked, 5)) },
            // I3 — Processing (intermediate)
            { "M.I3.1", Helped(Search, ProductChoice("MISSION_Q_PRODUCT_CHECKS", 5,
                ClientEventTypes.ProcessingNovaChecked, ClientEventTypes.ProcessingGreenscoreChecked, ClientEventTypes.ProcessingIngredientsReviewed)) },
            { "M.I3.2", Helped(Search, Confirm(ClientEventTypes.ProcessingNovaCategoryCompared)) },
            { "M.I3.3", Auto(SwapMeals(4, ProcessingSwaps)) },
            { "M.I3.4", Helped(Search, Products(ClientEventTypes.ProcessingNovaScoreCompared, 4)) },
            { "M.I3.5", Helped(Search, ProductChoice("MISSION_Q_PRODUCT_CHECKS", 6,
                ClientEventTypes.ProcessingNovaChecked, ClientEventTypes.ProcessingGreenscoreChecked, ClientEventTypes.ProcessingProductionMethodChecked)) },
            // I4 — Packaging (intermediate)
            { "M.I4.1", Helped(Search, Products(ClientEventTypes.ShoppingPackagingInfoChecked, 3)) },
            { "M.I4.2", Helped(Search, Products(ClientEventTypes.PackagingRecyclabilityEvaluated, 10)) },
            { "M.I4.3", Manual(Times(ClientEventTypes.PackagingReusableSpotChosen, 2)) },
            { "M.I4.4", Helped(Search, Confirm(ClientEventTypes.PackagingComparisonMade)) },
            { "M.I4.5", Helped(Search, ProductChoice("MISSION_Q_PACKAGING_CHECKS", 4,
                ClientEventTypes.PackagingReusableSpotChosen, ClientEventTypes.PackagingRecyclabilityEvaluated)) },
            // I5 — Food waste (intermediate)
            { "M.I5.1", Helped(Pantry, Confirm(ClientEventTypes.FoodWasteMealPlanned)) },
            { "M.I5.2", Auto(Meals(ClientEventTypes.FoodWasteFullPlateSaved, 3)) },
            { "M.I5.3", AutoWith(Pantry,
                Confirm(ClientEventTypes.FoodWasteFridgePantryChecked),
                MealChoice("MISSION_Q_FOOD_RESCUE", 3, false, ClientEventTypes.FoodWasteExpiredConsumed, ClientEventTypes.FoodWasteFullPlateSaved)) },
            { "M.I5.4", Pending(Waste) },
            { "M.I5.5", Auto(Meals(ClientEventTypes.FoodWasteFullPlateSaved, 3)) },
            // I6 — Nutrition (intermediate)
            { "M.I6.1", Auto(Meals(ClientEventTypes.SwapProcessedMeatToLegumes, 2)) },
            { "M.I6.2", Pending() },
            { "M.I6.3", Auto(DailyMeals(ClientEventTypes.NutritionSaltFreeTable)) },
            { "M.I6.4", Auto(Meals(ClientEventTypes.NutritionHealthyFatChosen, 5)) },
            { "M.I6.5", Manual(ProteinSourceChoice()) },
        };

        public static IReadOnlyDictionary<string, MissionInteraction> Entries => s_Entries;

        public static MissionInteraction Get(string missionCode)
        {
            if (string.IsNullOrWhiteSpace(missionCode))
            {
                return MissionInteraction.Unknown;
            }

            return s_Entries.TryGetValue(missionCode.Trim(), out MissionInteraction interaction)
                ? interaction
                : MissionInteraction.Unknown;
        }

        // ── Interaction factories ─────────────────────────────

        private static MissionInteraction Auto(params MissionReportStep[] steps) =>
            new MissionInteraction(MissionInteractionStatus.Available, MissionInteractionType.Report, new[] { QuickMealLog }, NoModules, steps);

        private static MissionInteraction AutoWith(MissionModuleLink helper, params MissionReportStep[] steps) =>
            new MissionInteraction(MissionInteractionStatus.Available, MissionInteractionType.Report, new[] { QuickMealLog }, new[] { helper }, steps);

        private static MissionInteraction Helped(MissionModuleLink helper, params MissionReportStep[] steps) =>
            new MissionInteraction(MissionInteractionStatus.Available, MissionInteractionType.Report, NoModules, new[] { helper }, steps);

        private static MissionInteraction Manual(params MissionReportStep[] steps) =>
            new MissionInteraction(MissionInteractionStatus.Available, MissionInteractionType.Report, NoModules, NoModules, steps);

        private static MissionInteraction Pending(MissionModuleLink helper = null) =>
            new MissionInteraction(MissionInteractionStatus.PendingRule, MissionInteractionType.Report, NoModules,
                helper == null ? NoModules : new[] { helper }, Array.Empty<MissionReportStep>());

        // ── Step factories ────────────────────────────────────

        private static string Q(string eventType) => "MISSION_Q_" + eventType;

        private static string QDays(string eventType) => "MISSION_QD_" + eventType;

        private static IReadOnlyDictionary<string, object> Meta(string key, object value) =>
            new Dictionary<string, object> { { key, value } };

        /// <summary>N events, each with a distinct synthetic id in <paramref name="distinctField"/>.</summary>
        private static MissionReportStep Products(string eventType, int max, IReadOnlyDictionary<string, object> fixedMetadata = null, string distinctField = ProductId) =>
            new MissionReportStep(MissionStepType.Count, Q(eventType), eventType, distinctField, fixedMetadata ?? NoMetadata, null, max, false, null);

        /// <summary>N events without distinct field.</summary>
        private static MissionReportStep Times(string eventType, int max) =>
            new MissionReportStep(MissionStepType.Count, Q(eventType), eventType, null, NoMetadata, null, max, false, null);

        private static MissionReportStep Confirm(string eventType) =>
            new MissionReportStep(MissionStepType.YesNo, Q(eventType), eventType, null, NoMetadata, null, 1, false, null);

        /// <summary>One event per picked day, dated on that day (createdAt).</summary>
        private static MissionReportStep Days(string eventType, int max) =>
            new MissionReportStep(MissionStepType.DayPicker, QDays(eventType), eventType, null, NoMetadata, null, max, false, null);

        /// <summary>N quick meal logs with a fixed flag or swap.</summary>
        private static MissionReportStep Meals(string flag, int max) =>
            new MissionReportStep(MissionStepType.MealReport, Q(flag), flag, null, NoMetadata, null, max, false, null);

        /// <summary>One quick meal log per day (up to 7) with a fixed flag; rules count by mealDayBucket.</summary>
        private static MissionReportStep DailyMeals(string flag, string fixedMealType = null) =>
            new MissionReportStep(MissionStepType.MealReport, QDays(flag), flag, null, NoMetadata, null, 7, true, fixedMealType);

        /// <summary>Quick meal logs whose flag is chosen per meal.</summary>
        private static MissionReportStep MealChoice(string promptKey, int max, bool onePerDay, params string[] flags) =>
            new MissionReportStep(MissionStepType.MealReport, promptKey, null, null, NoMetadata, flags.Select(Option).ToArray(), max, onePerDay, null);

        /// <summary>Quick meal logs whose swap is chosen per meal (a meal log may carry only swaps).</summary>
        private static MissionReportStep SwapMeals(int maxMeals, params string[] swaps) =>
            MealChoice("MISSION_Q_SWAPS", maxMeals, false, swaps);

        private static MissionReportStep ProductChoice(string promptKey, int maxPerOption, params string[] eventTypes) =>
            new MissionReportStep(MissionStepType.OptionPicker, promptKey, null, ProductId, NoMetadata, eventTypes.Select(Option).ToArray(), maxPerOption, false, null);

        private static MissionReportStep ProteinSourceChoice() =>
            new MissionReportStep(MissionStepType.OptionPicker, "MISSION_Q_PROTEIN_SOURCES", null, null, NoMetadata,
                ProteinSources.Select(s => new MissionStepOption("MISSION_OPT_PROTEIN_" + s, ClientEventTypes.NutritionProteinVarietyLogged, Meta("proteinSource", s))).ToArray(),
                1, false, null);

        private static MissionStepOption Option(string eventType) =>
            new MissionStepOption(eventType.StartsWith("SWAP_", StringComparison.Ordinal) ? eventType : "MISSION_OPT_" + eventType, eventType, NoMetadata);
    }
}
