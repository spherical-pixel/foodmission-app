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
        /// <summary>Product missions can be reported from the product detail: the search finds the product to open.</summary>
        public static readonly MissionModuleLink FindProduct = new MissionModuleLink(Actions.go_to_quicksearch, "MISSION_BTN_FIND_PRODUCT");

        private const string ProductId = "productId";

        // What one checklist item is in the mission goal (card title and add button); null = product
        private const string Meal = "MEAL";
        private const string Purchase = "PURCHASE";

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

        private static readonly string[] SustainabilityLabels =
        {
            "EU_ORGANIC", "FAIRTRADE", "RAINFOREST_ALLIANCE", "MSC", "ASC", "EU_ECOLABEL", "OTHER"
        };

        private static readonly string[] RainbowColours =
        {
            "GREEN", "RED", "ORANGE", "YELLOW", "PURPLE", "WHITE"
        };

        private static readonly Dictionary<string, MissionInteraction> s_Entries = new(StringComparer.OrdinalIgnoreCase)
        {
            // A1 — Diet (advanced)
            { "M.A1.1", Auto(MealChoice("MISSION_Q_MEAL_MEAT", 7, false, ClientEventTypes.MealMeatFree, ClientEventTypes.MealMeatConsumed)) },
            { "M.A1.2", Auto(MealChoice("MISSION_Q_MEAL_PROTEIN", 10, false, ClientEventTypes.MealLegumeConsumed, ClientEventTypes.MealMeatConsumed)) },
            { "M.A1.3", Auto(Meals(ClientEventTypes.MealLegumeConsumed, 5)) },
            { "M.A1.4", Auto(Meals(ClientEventTypes.MealAlternativeStaple, 5)) },
            { "M.A1.5", Auto(Meals(ClientEventTypes.MealSustainablePlate, 10)) },
            // A2 — Product choices (advanced)
            { "M.A2.1", Manual(ProductFacts("MISSION_Q_ORIGIN_PLUS_ONE", Meal, 3, 2,
                new[] { ClientEventTypes.ShoppingOriginChecked, ClientEventTypes.ShoppingCertificationChosen,
                    ClientEventTypes.ProcessingProductionMethodChecked, ClientEventTypes.ShoppingSeasonalChosen },
                ClientEventTypes.ShoppingOriginChecked)) },
            { "M.A2.2", Manual(ProductValues("MISSION_Q_DOUBLE_LABEL", 1, ClientEventTypes.ShoppingDoubleLabelChosen, "labels",
                "MISSION_OPT_LABEL_", 2, SustainabilityLabels)) },
            { "M.A2.3", Manual(Products(ClientEventTypes.ShoppingMulticriteriaPurchase, 3)) },
            { "M.A2.4", Auto(Meals(ClientEventTypes.MealSeasonalProduce, 4)) },
            { "M.A2.5", Manual(ProductFacts("MISSION_Q_THREE_ASPECTS", Purchase, 4, 3,
                new[] { ClientEventTypes.ShoppingOriginChecked, ClientEventTypes.ProcessingProductionMethodChecked,
                    ClientEventTypes.ShoppingCertificationChosen, ClientEventTypes.ShoppingSeasonalChosen })) },
            // A3 — Processing (advanced)
            { "M.A3.1", Manual(Products(ClientEventTypes.ProcessingNovaScoreCompared, 3, Meta("comparedWith", "GREEN_SCORE"),
                promptKey: "MISSION_Q_M_A3_1_NOVA_GREEN_SCORE")) },
            { "M.A3.2", Manual(Products(ClientEventTypes.ProcessingAllScoresCompared, 3)) },
            { "M.A3.3", Manual(
                Products(ClientEventTypes.ProcessingNovaChecked, 3, promptKey: "MISSION_Q_M_A3_3_NOVA"),
                Products(ClientEventTypes.ProcessingIngredientsReviewed, 3, promptKey: "MISSION_Q_M_A3_3_INGREDIENTS"),
                Products(ClientEventTypes.ShoppingPackagingInfoChecked, 3, promptKey: "MISSION_Q_M_A3_3_PACKAGING_INFO")) },
            { "M.A3.4", Manual(Products(ClientEventTypes.ProcessingIndicatorsCompared, 3)) },
            { "M.A3.5", Manual(ProductChoice("MISSION_Q_PRODUCT_CHECKS", 5,
                ClientEventTypes.ProcessingNovaChecked, ClientEventTypes.ProcessingGreenscoreChecked,
                ClientEventTypes.ProcessingProductionMethodChecked, ClientEventTypes.ProcessingIndicatorsCompared)) },
            // A4 — Packaging (advanced)
            { "M.A4.1", Manual(ProductFacts("MISSION_Q_PACKAGING_END_OF_LIFE", Purchase, 3, 2,
                new[] { ClientEventTypes.PackagingMaterialObserved, ClientEventTypes.PackagingRecyclingLabelRead,
                    ClientEventTypes.PackagingRecyclabilityEvaluated },
                ClientEventTypes.PackagingMaterialObserved)) },
            { "M.A4.2", Manual(Times(ClientEventTypes.PackagingReusableSpotChosen, 5)) },
            { "M.A4.3", Manual(Products(ClientEventTypes.PackagingTradeOffReflected, 5)) },
            { "M.A4.4", Manual(Products(ClientEventTypes.PackagingSmartObserved, 2)) },
            { "M.A4.5", Manual(Products(ClientEventTypes.ShoppingPackagingInfoChecked, 5)) },
            // A5 — Food waste (advanced)
            { "M.A5.1", Auto(Meals(ClientEventTypes.FoodWasteHalfPlateSaved, 7)) },
            { "M.A5.2", Auto(Meals(ClientEventTypes.FoodWasteFullPlateSaved, 7)) },
            { "M.A5.3", Auto(Meals(ClientEventTypes.FoodWasteExpiredConsumed, 4)) },
            { "M.A5.4", Manual(Days(ClientEventTypes.FoodWasteFifoOrganized, 3)) },
            { "M.A5.5", Manual(Confirm(ClientEventTypes.FoodWasteMealPlanned, "MISSION_Q_M_A5_5_WEEK_PLANNED"), Confirm(ClientEventTypes.FoodWasteFridgePantryChecked)) },
            // A6 — Nutrition (advanced)
            { "M.A6.1", Manual(DayValues("MISSION_QD_RAINBOW_COLOURS", ClientEventTypes.NutritionRainbowColoursLogged, "colours",
                "MISSION_OPT_COLOUR_", 5, RainbowColours)) },
            { "M.A6.2", Auto(DailyMeals(ClientEventTypes.NutritionHighFibreMeal, "BREAKFAST")) },
            { "M.A6.3", Auto(AllMealsDays("MISSION_QDAY_NO_ADDED_SUGAR", ClientEventTypes.NutritionAddedSugarAvoided)) },
            { "M.A6.4", Auto(MainMealChoice("MISSION_Q_NUTRIENT_DENSE_MEALS", 21,
                ClientEventTypes.NutritionProteinIncluded, ClientEventTypes.NutritionHighFibreMeal,
                ClientEventTypes.NutritionFruitVegServingAdded, ClientEventTypes.NutritionHealthyFatChosen)) },
            { "M.A6.5", Manual(Products(ClientEventTypes.NutritionPlantDiversityCount, 20, null, "genericFoodId")) },
            // B1 — Diet (beginner)
            { "M.B1.1", Auto(MealChoice("MISSION_Q_MEAL_MEAT", 7, true, ClientEventTypes.MealMeatFree, ClientEventTypes.MealMeatConsumed)) },
            { "M.B1.2", Auto(MealChoice("MISSION_Q_MEAL_MEAT", 7, true, ClientEventTypes.MealMeatFree, ClientEventTypes.MealMeatConsumed)) },
            { "M.B1.3", Auto(MealChoice("MISSION_Q_MEAL_MEAT", 7, false, ClientEventTypes.MealMeatFree, ClientEventTypes.MealMeatConsumed)) },
            { "M.B1.4", Auto(DaySwaps(2, ProteinSwaps)) },
            { "M.B1.5", Auto(DaySwaps(2, ClientEventTypes.SwapBeefToChicken, ClientEventTypes.SwapBeefToLegumes)) },
            // B2 — Product choices (beginner)
            { "M.B2.1", Manual(Products(ClientEventTypes.ShoppingOriginChecked, 5)) },
            { "M.B2.2", Auto(Meals(ClientEventTypes.MealSeasonalProduce, 5)) },
            { "M.B2.3", Manual(Times(ClientEventTypes.ShoppingCertificationChosen, 3)) },
            { "M.B2.4", Auto(Meals(ClientEventTypes.MealLocalProduce, 3)) },
            { "M.B2.5", Manual(Products(ClientEventTypes.ShoppingPackagingInfoChecked, 5, promptKey: "MISSION_Q_M_B2_5_SUSTAINABILITY_INFO")) },
            // B3 — Processing (beginner)
            { "M.B3.1", Manual(Products(ClientEventTypes.ProcessingProductionMethodChecked, 5)) },
            { "M.B3.2", Manual(Products(ClientEventTypes.ProcessingNovaChecked, 3)) },
            { "M.B3.3", Manual(Products(ClientEventTypes.ProcessingIngredientsReviewed, 5)) },
            { "M.B3.4", Manual(Products(ClientEventTypes.ProcessingGreenscoreChecked, 5)) },
            { "M.B3.5", Auto(DaySwaps(2, ProcessingSwaps)) },
            // B4 — Packaging (beginner)
            { "M.B4.1", Manual(Products(ClientEventTypes.PackagingMaterialObserved, 5)) },
            { "M.B4.2", Manual(Products(ClientEventTypes.PackagingRecyclingLabelRead, 10)) },
            { "M.B4.3", Manual(Products(ClientEventTypes.PackagingMaterialObserved, 10)) },
            { "M.B4.4", Manual(Times(ClientEventTypes.PackagingReusableSpotChosen, 5, "MISSION_Q_M_B4_4_REUSABLE_FOUND")) },
            { "M.B4.5", Manual(ProductChoice("MISSION_Q_PACKAGING_CHECKS", 10,
                ClientEventTypes.PackagingMaterialObserved, ClientEventTypes.PackagingRecyclingLabelRead, ClientEventTypes.PackagingReusableSpotChosen)) },
            // B5 — Food waste (beginner)
            { "M.B5.1", Auto(Meals(ClientEventTypes.FoodWasteHalfPlateSaved, 5)) },
            { "M.B5.2", Auto(Meals(ClientEventTypes.FoodWasteFullPlateSaved, 3)) },
            { "M.B5.3", Auto(Meals(ClientEventTypes.FoodWasteExpiredConsumed, 1)) },
            { "M.B5.4", Manual(Products(ClientEventTypes.FoodWasteStorageInstructionsRead, 5)) },
            { "M.B5.5", Auto(FlagDays("MISSION_QDAY_FOOD_SAVED", 4,
                ClientEventTypes.FoodWasteHalfPlateSaved, ClientEventTypes.FoodWasteFullPlateSaved, ClientEventTypes.FoodWasteExpiredConsumed)) },
            // B6 — Nutrition (beginner)
            { "M.B6.1", Auto(YesDays("MISSION_QDAY_PROTEIN", ClientEventTypes.NutritionProteinIncluded)) },
            { "M.B6.2", Auto(YesDays("MISSION_QDAY_FRUIT_VEG", ClientEventTypes.NutritionFruitVegServingAdded)) },
            { "M.B6.3", Auto(DaySwaps(3, ClientEventTypes.SwapSugaryDrinkToWater)) },
            { "M.B6.4", Auto(YesDays("MISSION_QDAY_WHOLEGRAIN", ClientEventTypes.NutritionWholegrainChosen)) },
            { "M.B6.5", Auto(DaySwaps(5, ClientEventTypes.SwapSnackToFruitNuts)) },
            // I1 — Diet (intermediate)
            { "M.I1.1", Auto(DaySwaps(4, ProteinSwaps)) },
            { "M.I1.2", Auto(LevelSwaps(
                new[] { ClientEventTypes.SwapBeefToPork, ClientEventTypes.SwapBeefToChicken },
                new[] { ClientEventTypes.SwapBeefToLegumes },
                new[] { ClientEventTypes.SwapPorkToLegumes, ClientEventTypes.SwapChickenToLegumes })) },
            { "M.I1.3", Auto(MeatFreeDays(2)) },
            { "M.I1.4", Auto(Meals(ClientEventTypes.MealLegumeConsumed, 4)) },
            { "M.I1.5", Manual(ProteinSourceChoice()) },
            // I2 — Product choices (intermediate)
            { "M.I2.1", Auto(Meals(ClientEventTypes.MealLocalProduce, 3)) },
            { "M.I2.2", Auto(Meals(ClientEventTypes.MealSeasonalProduce, 8)) },
            { "M.I2.3", Auto(Meals(ClientEventTypes.MealCertifiedProduct, 3)) },
            { "M.I2.4", Manual(ProductFacts("MISSION_Q_TWO_FACTS", null, 5, 2,
                new[] { ClientEventTypes.ShoppingOriginChecked, ClientEventTypes.ShoppingCertificationChosen,
                    ClientEventTypes.ShoppingSeasonalChosen, ClientEventTypes.ShoppingLocalChosen })) },
            { "M.I2.5", Manual(Times(ClientEventTypes.ShoppingPackagingInfoChecked, 5, "MISSION_Q_M_I2_5_CLIMATE_INFO")) },
            // I3 — Processing (intermediate)
            { "M.I3.1", Manual(ProductChoice("MISSION_Q_M_I3_1_PRODUCTION_CHECKS", 5,
                ClientEventTypes.ProcessingNovaChecked, ClientEventTypes.ProcessingGreenscoreChecked, ClientEventTypes.ProcessingIngredientsReviewed)) },
            { "M.I3.2", Manual(Confirm(ClientEventTypes.ProcessingNovaCategoryCompared)) },
            { "M.I3.3", Auto(DaySwaps(4, ProcessingSwaps)) },
            { "M.I3.4", Manual(Products(ClientEventTypes.ProcessingNovaScoreCompared, 4)) },
            { "M.I3.5", Manual(ProductChoice("MISSION_Q_M_I3_5_PRODUCTION_CHECKS", 6,
                ClientEventTypes.ProcessingNovaChecked, ClientEventTypes.ProcessingGreenscoreChecked, ClientEventTypes.ProcessingProductionMethodChecked)) },
            // I4 — Packaging (intermediate)
            { "M.I4.1", Manual(Products(ClientEventTypes.ShoppingPackagingInfoChecked, 3)) },
            { "M.I4.2", Manual(Products(ClientEventTypes.PackagingRecyclabilityEvaluated, 10)) },
            { "M.I4.3", Manual(Times(ClientEventTypes.PackagingReusableSpotChosen, 2)) },
            { "M.I4.4", Manual(Confirm(ClientEventTypes.PackagingComparisonMade)) },
            { "M.I4.5", Manual(ProductChoice("MISSION_Q_M_I4_5_CIRCULAR_PACKAGING", 4,
                ClientEventTypes.PackagingReusableSpotChosen, ClientEventTypes.PackagingRecyclabilityEvaluated)) },
            // I5 — Food waste (intermediate)
            { "M.I5.1", Manual(Confirm(ClientEventTypes.FoodWasteMealPlanned)) },
            { "M.I5.2", Auto(Meals(ClientEventTypes.FoodWasteFullPlateSaved, 3)) },
            { "M.I5.3", Auto(
                Confirm(ClientEventTypes.FoodWasteFridgePantryChecked),
                MealChoice("MISSION_Q_FOOD_RESCUE", 3, false, ClientEventTypes.FoodWasteExpiredConsumed, ClientEventTypes.FoodWasteFullPlateSaved)) },
            { "M.I5.4", Auto(Meals(ClientEventTypes.FoodWasteHalfPlateSaved, 3)) },
            { "M.I5.5", Auto(Meals(ClientEventTypes.FoodWasteFullPlateSaved, 3)) },
            // I6 — Nutrition (intermediate)
            { "M.I6.1", Auto(DaySwaps(2, ClientEventTypes.SwapProcessedMeatToLegumes)) },
            { "M.I6.2", Auto(Meals(ClientEventTypes.NutritionHighFibreMeal, 21, CheckInMealTypes.Main)) },
            { "M.I6.3", Auto(AllMealsDays("MISSION_QDAY_NO_SALT", ClientEventTypes.NutritionSaltFreeTable)) },
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
            new MissionInteraction(MissionInteractionStatus.Available, MissionInteractionType.Report, new[] { QuickMealLog }, steps);

        private static MissionInteraction Manual(params MissionReportStep[] steps) =>
            new MissionInteraction(MissionInteractionStatus.Available, MissionInteractionType.Report, NoModules, steps,
                steps.Any(s => ProductMissionPlanner.IdFieldOf(s) != null) ? new[] { FindProduct } : NoModules);

        // ── Step factories ────────────────────────────────────

        private static string Q(string eventType) => "MISSION_Q_" + eventType;

        private static string QDays(string eventType) => "MISSION_QD_" + eventType;

        private static IReadOnlyDictionary<string, object> Meta(string key, object value) =>
            new Dictionary<string, object> { { key, value } };

        // promptKey: a question of the mission's own when the event's shared question doesn't explain its goal

        /// <summary>N events, each with a distinct synthetic id in <paramref name="distinctField"/>.</summary>
        private static MissionReportStep Products(string eventType, int max, IReadOnlyDictionary<string, object> fixedMetadata = null,
            string distinctField = ProductId, string promptKey = null) =>
            new MissionReportStep(MissionStepType.Count, promptKey ?? Q(eventType), eventType, distinctField, fixedMetadata ?? NoMetadata, null, max, false, null);

        /// <summary>N events without distinct field.</summary>
        private static MissionReportStep Times(string eventType, int max, string promptKey = null) =>
            new MissionReportStep(MissionStepType.Count, promptKey ?? Q(eventType), eventType, null, NoMetadata, null, max, false, null);

        private static MissionReportStep Confirm(string eventType, string promptKey = null) =>
            new MissionReportStep(MissionStepType.YesNo, promptKey ?? Q(eventType), eventType, null, NoMetadata, null, 1, false, null);

        /// <summary>One event per picked day, dated on that day (createdAt).</summary>
        private static MissionReportStep Days(string eventType, int max) =>
            new MissionReportStep(MissionStepType.DayPicker, QDays(eventType), eventType, null, NoMetadata, null, max, false, null);

        /// <summary>N quick meal logs with a fixed flag or swap, only on these meal types (none = any meal).</summary>
        private static MissionReportStep Meals(string flag, int max, params string[] mealTypes) =>
            new MissionReportStep(MissionStepType.MealReport, Q(flag), flag, null, NoMetadata, null, max, false, mealTypes);

        /// <summary>One quick meal log per day (up to 7) with a fixed flag; rules count by mealDayBucket.</summary>
        private static MissionReportStep DailyMeals(string flag, params string[] mealTypes) =>
            new MissionReportStep(MissionStepType.MealReport, QDays(flag), flag, null, NoMetadata, null, 7, true, mealTypes);

        /// <summary>Quick meal logs whose flag is chosen per meal.</summary>
        private static MissionReportStep MealChoice(string promptKey, int max, bool onePerDay, params string[] flags) =>
            new MissionReportStep(MissionStepType.MealReport, promptKey, null, null, NoMetadata, flags.Select(Option).ToArray(), max, onePerDay, null);

        /// <summary>Main meals (breakfast, lunch, dinner) whose flags are chosen per meal; flags of one meal share its meal log.</summary>
        private static MissionReportStep MainMealChoice(string promptKey, int max, params string[] flags) =>
            new MissionReportStep(MissionStepType.MealReport, promptKey, null, null, NoMetadata, flags.Select(Option).ToArray(), max, false, CheckInMealTypes.Main);

        // ── Swaps asked per day (OTHER meal logs; per meal next to meal missions, see MissionSwaps) ──

        /// <summary>The rule counts every swap event up to <paramref name="target"/>.</summary>
        private static MissionReportStep DaySwaps(int target, params string[] swaps) =>
            new MissionReportStep(MissionStepType.DaySwaps, MissionSwaps.PromptKey, null, null, NoMetadata, swaps.Select(Option).ToArray(), target, false, null,
                daySwaps: new SwapQuestionSpec(target));

        /// <summary>The rule wants one swap of each level (M.I1.2).</summary>
        private static MissionReportStep LevelSwaps(params string[][] levels) =>
            new MissionReportStep(MissionStepType.DaySwaps, MissionSwaps.PromptKey, null, null, NoMetadata, levels.SelectMany(l => l).Select(Option).ToArray(),
                levels.Length, false, null, daySwaps: new SwapQuestionSpec(levels.Length, levels));

        // ── Day questions (one OTHER meal log per day) ────────

        private static MissionStepOption Yes(string flag) => new MissionStepOption("MISSION_REPORT_YES", flag, NoMetadata);

        private static MissionStepOption No(string flag = null) => new MissionStepOption("MISSION_REPORT_NO", flag, NoMetadata);

        private static MissionReportStep DayQuestion(string promptKey, int targetDays, DayQuestionSpec spec, params MissionStepOption[] options) =>
            new MissionReportStep(MissionStepType.DayQuestion, promptKey, null, null, NoMetadata, options, targetDays, true, null, null, spec);

        /// <summary>Yes = the flag that day; the day counts once the flag is recorded.</summary>
        private static MissionReportStep YesDays(string promptKey, string flag) =>
            DayQuestion(promptKey, MissionReportDays.WindowDays, new DayQuestionSpec(DayCoverage.FlagDay, false, false, new[] { 0 }), Yes(flag), No());

        /// <summary>One of several flags that day, or none.</summary>
        private static MissionReportStep FlagDays(string promptKey, int targetDays, params string[] flags) =>
            DayQuestion(promptKey, targetDays, new DayQuestionSpec(DayCoverage.FlagDay, false, false, Enumerable.Range(0, flags.Length).ToArray()),
                flags.Select(f => new MissionStepOption(MealFlagLabels.KeyFor(f), f, NoMetadata))
                    .Append(new MissionStepOption("MISSION_QDAY_NONE", null, NoMetadata)).ToArray());

        /// <summary>"Did you eat meat?": a logged day without meat counts, so only No counts.</summary>
        private static MissionReportStep MeatFreeDays(int targetDays) =>
            DayQuestion("MISSION_QDAY_MEAT", targetDays, new DayQuestionSpec(DayCoverage.LoggedDay, false, false, new[] { 1 }),
                Yes(ClientEventTypes.MealMeatConsumed), No(ClientEventTypes.MealMeatFree));

        /// <summary>Every meal must carry the flag: Yes flags every meal log of that day, and the mission is never pruned.</summary>
        private static MissionReportStep AllMealsDays(string promptKey, string flag) =>
            DayQuestion(promptKey, MissionReportDays.WindowDays, new DayQuestionSpec(DayCoverage.LoggedDay, true, true, new[] { 0 }), Yes(flag), No());

        private static MissionReportStep ProductChoice(string promptKey, int maxPerOption, params string[] eventTypes) =>
            new MissionReportStep(MissionStepType.OptionPicker, promptKey, null, ProductId, NoMetadata, eventTypes.Select(Option).ToArray(), maxPerOption, false, null);

        private static MissionReportStep ProteinSourceChoice() =>
            new MissionReportStep(MissionStepType.OptionPicker, "MISSION_Q_PROTEIN_SOURCES", null, null, NoMetadata,
                ProteinSources.Select(s => new MissionStepOption("MISSION_OPT_PROTEIN_" + s, ClientEventTypes.NutritionProteinVarietyLogged, Meta("proteinSource", s))).ToArray(),
                1, false, null);

        /// <summary>Up to <paramref name="maxProducts"/> products, each with one event per checked fact (they share its productId).</summary>
        private static MissionReportStep ProductFacts(string promptKey, string item, int maxProducts, int minDistinct, string[] eventTypes, params string[] required) =>
            new MissionReportStep(MissionStepType.ProductChecklist, promptKey, null, null, NoMetadata, null, maxProducts, false, null,
                new ChecklistSpec(eventTypes.Select(Option).ToArray(), null, minDistinct, required.Select(r => Array.IndexOf(eventTypes, r)).ToArray(), item));

        /// <summary>Up to <paramref name="maxProducts"/> products, each sent as one event with the checked values in <paramref name="arrayField"/>.</summary>
        private static MissionReportStep ProductValues(string promptKey, int maxProducts, string eventType, string arrayField, string labelPrefix, int minDistinct, params string[] values) =>
            new MissionReportStep(MissionStepType.ProductChecklist, promptKey, eventType, null, NoMetadata, null, maxProducts, false, null,
                new ChecklistSpec(values.Select(v => ValueOption(labelPrefix, v)).ToArray(), arrayField, minDistinct, null));

        /// <summary>One event per day with the values newly checked for that day; the backend merges a day's events.</summary>
        private static MissionReportStep DayValues(string promptKey, string eventType, string arrayField, string labelPrefix, int minDistinct, params string[] values) =>
            new MissionReportStep(MissionStepType.DayChecklist, promptKey, eventType, null, NoMetadata, null, MissionReportDays.WindowDays, true, null,
                new ChecklistSpec(values.Select(v => ValueOption(labelPrefix, v)).ToArray(), arrayField, minDistinct, null));

        private static MissionStepOption ValueOption(string labelPrefix, string value) =>
            new MissionStepOption(labelPrefix + value, null, Meta(ChecklistSpec.ValueKey, value));

        private static MissionStepOption Option(string eventType) =>
            new MissionStepOption(eventType.StartsWith("SWAP_", StringComparison.Ordinal) ? eventType : "MISSION_OPT_" + eventType, eventType, NoMetadata);
    }
}
