using System;
using System.Collections.Generic;
using System.Linq;

namespace eu.foodmission.platform
{
    /// <summary>
    /// Decides which mission cards the product detail shows for one food (spec 2026-10-14-mission-product-report-design.md). Pure.
    /// </summary>
    public static class ProductMissionPlanner
    {
        public const string ProductIdField = "productId";
        public const string GenericFoodIdField = "genericFoodId";

        // What the product detail highlights for each event; packaging and food waste have no data there
        private static readonly Dictionary<string, ProductFocus> s_Focus = new Dictionary<string, ProductFocus>(StringComparer.Ordinal)
        {
            { ClientEventTypes.ShoppingOriginChecked, ProductFocus.Origin },
            { ClientEventTypes.ShoppingLocalChosen, ProductFocus.Origin },
            { ClientEventTypes.ShoppingCertificationChosen, ProductFocus.Labels },
            { ClientEventTypes.ShoppingDoubleLabelChosen, ProductFocus.Labels },
            { ClientEventTypes.ProcessingProductionMethodChecked, ProductFocus.Labels },
            { ClientEventTypes.ProcessingNovaChecked, ProductFocus.Nova },
            { ClientEventTypes.ProcessingNovaCategoryCompared, ProductFocus.Nova },
            { ClientEventTypes.ProcessingNovaScoreCompared, ProductFocus.Nova | ProductFocus.Scores },
            { ClientEventTypes.ProcessingGreenscoreChecked, ProductFocus.Scores },
            { ClientEventTypes.ProcessingAllScoresCompared, ProductFocus.Scores | ProductFocus.Nova },
            { ClientEventTypes.ProcessingIndicatorsCompared, ProductFocus.Scores | ProductFocus.Nova },
            { ClientEventTypes.ProcessingIngredientsReviewed, ProductFocus.Ingredients },
        };

        public static ProductFocus FocusOf(string eventType) =>
            eventType != null && s_Focus.TryGetValue(eventType, out ProductFocus focus) ? focus : ProductFocus.None;

        private const string CheckInPromptPrefix = "MISSION_Q_";
        private const string CardPromptPrefix = "MISSION_QP_";

        /// <summary>
        /// The card asks about the one product on screen ("Have you checked…?"), not how many like the check-in:
        /// MISSION_Q_X becomes MISSION_QP_X.
        /// </summary>
        public static string CardPromptKeyOf(MissionReportStep step)
        {
            string key = step?.PromptKey;
            if (key == null || !key.StartsWith(CheckInPromptPrefix, StringComparison.Ordinal))
            {
                return key;
            }
            return CardPromptPrefix + key.Substring(CheckInPromptPrefix.Length);
        }

        /// <summary>Metadata field that identifies the food for this step, null when the step isn't about a product.</summary>
        public static string IdFieldOf(MissionReportStep step)
        {
            if (step == null)
            {
                return null;
            }
            switch (step.Type)
            {
                case MissionStepType.Count:
                    return step.DistinctField == ProductIdField || step.DistinctField == GenericFoodIdField ? step.DistinctField : null;
                case MissionStepType.OptionPicker:
                    return step.DistinctField == ProductIdField ? ProductIdField : null;
                case MissionStepType.ProductChecklist:
                    return ProductIdField;
                default:
                    return null;
            }
        }

        public static IReadOnlyList<string> EventTypesOf(MissionReportStep step) =>
            IdFieldOf(step) == null ? Array.Empty<string>() : OptionsOf(step).Select(o => o.EventType).Distinct().ToList();

        public static IReadOnlyList<ProductMissionCard> Plan(ProductMissionInputs inputs)
        {
            var cards = new List<ProductMissionCard>();
            if (inputs == null || string.IsNullOrEmpty(inputs.FoodId))
            {
                return cards;
            }

            string wantedField = inputs.FoodType == FoodInfoType.Generic ? GenericFoodIdField : ProductIdField;
            foreach (CheckInMission mission in inputs.Missions)
            {
                // Past its deadline nothing counts any more (the backend fails it within the hour)
                if (mission.StartLocal.HasValue && inputs.NowLocal >= MissionDeadlines.DeadlineLocal(mission.StartLocal.Value))
                {
                    continue;
                }
                foreach (MissionReportStep step in mission.Interaction.Steps)
                {
                    string idField = IdFieldOf(step);
                    if (idField != wantedField)
                    {
                        continue;
                    }

                    List<ProductMissionOption> options = OptionsOf(step);
                    var locked = new List<int>();
                    var suggested = new List<int>();
                    for (int i = 0; i < options.Count; i++)
                    {
                        if (IsSent(inputs, options[i], mission.StartLocal))
                        {
                            locked.Add(i);
                        }
                        else if (options[i].Focus != ProductFocus.None && ProductFocusMap.HasAny(options[i].Focus, inputs.AvailableKeys))
                        {
                            suggested.Add(i);
                        }
                    }

                    inputs.Progress.TryGetValue(mission.Code, out MissionProgress progress);
                    cards.Add(new ProductMissionCard(mission.Code, mission.Title, mission.StartLocal, inputs.FoodType, inputs.FoodId,
                        progress?.progress ?? 0f, step, KindOf(step), idField, options, locked, suggested));
                }
            }
            return cards;
        }

        private static List<ProductMissionOption> OptionsOf(MissionReportStep step)
        {
            switch (step.Type)
            {
                case MissionStepType.Count:
                    return new List<ProductMissionOption> { new ProductMissionOption(step.PromptKey, step.EventType, null, null, FocusOf(step.EventType)) };
                case MissionStepType.OptionPicker:
                    return step.Options.Select(o => new ProductMissionOption(o.LabelKey, o.EventType, null, o.Metadata, FocusOf(o.EventType))).ToList();
                default:
                    ChecklistSpec spec = step.Checklist;
                    return spec.Options.Select((o, i) => spec.IsArray
                        ? new ProductMissionOption(o.LabelKey, step.EventType, spec.ValueOf(i), null, FocusOf(step.EventType))
                        : new ProductMissionOption(o.LabelKey, o.EventType, null, o.Metadata, FocusOf(o.EventType))).ToList();
            }
        }

        /// <summary>Sent since the mission started: rules only count events of the current window (a restart drops older ones).</summary>
        private static bool IsSent(ProductMissionInputs inputs, ProductMissionOption option, DateTime? startLocal)
        {
            if (!inputs.Sent.TryGetValue(option.EventType, out Dictionary<string, DateTime> values))
            {
                return false;
            }
            IEnumerable<DateTime> times = option.Value == null
                ? values.Values
                : values.Where(v => v.Key == option.Value.ToUpperInvariant()).Select(v => v.Value);
            return times.Any(t => !startLocal.HasValue || t >= startLocal.Value);
        }

        private static ProductMissionCardKind KindOf(MissionReportStep step) => step.Type switch
        {
            MissionStepType.Count => ProductMissionCardKind.Single,
            MissionStepType.OptionPicker => ProductMissionCardKind.Choice,
            _ => ProductMissionCardKind.Checklist
        };
    }
}
