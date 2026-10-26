using System;
using System.Collections.Generic;

namespace eu.foodmission.platform.Components
{
    public enum WheelAmountUnit
    {
        Grams,
        Millilitres,
        Use,
        Refill,
    }

    /// <summary>One row of the wheel explanation table: an action and what it saves on each wheel.</summary>
    public sealed class WheelScenario
    {
        private readonly Dictionary<string, float> _values;

        public string Code { get; }
        public float Amount { get; }
        public WheelAmountUnit Unit { get; }

        public WheelScenario(string code, float amount, WheelAmountUnit unit, float co2, float energy, float water, float land)
        {
            Code = code;
            Amount = amount;
            Unit = unit;
            _values = new Dictionary<string, float>(StringComparer.Ordinal)
            {
                { "CO2_REDUCTION", co2 },
                { "ENERGY_REDUCTION", energy },
                { "WATER_SAVINGS", water },
                { "LAND_USE_REDUCTION", land },
            };
        }

        public bool TryGetValue(string kind, out float value)
        {
            value = 0f;
            return kind != null && _values.TryGetValue(kind, out value);
        }
    }

    /// <summary>
    /// Content of the progress wheel explanation screen (partner text, 2026-10-26).
    /// Values mirror the backend's wheel-impact-actions.config.ts; VEGAN_DAY and VEGETARIAN_DAY exist only in the app.
    /// </summary>
    public static class ProgressWheelInfoCatalog
    {
        public const string MeasureHeadingKey = "WHEEL_INFO_MEASURE_HEADING";
        public const string ColScenarioKey = "WHEEL_INFO_COL_SCENARIO";
        public const string ColAmountKey = "WHEEL_INFO_COL_AMOUNT";
        public const string ValueKey = "WHEEL_INFO_VALUE";
        public const string SourcesHeadingKey = "WHEEL_INFO_SOURCES_HEADING";
        public const string SourcesLeadKey = "WHEEL_INFO_SOURCES_LEAD";
        public const string A11yHintKey = "WHEEL_INFO_A11Y_HINT";
        public const string DestinationLabelKey = "WHEEL_INFO_APPBAR";

        private static readonly string[] s_SharedKeys =
        {
            MeasureHeadingKey, ColScenarioKey, ColAmountKey, ValueKey,
            SourcesHeadingKey, SourcesLeadKey, A11yHintKey, DestinationLabelKey,
        };

        private static readonly Dictionary<string, int> s_ParagraphCounts = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            { "CO2_REDUCTION", 3 },
            { "ENERGY_REDUCTION", 3 },
            { "WATER_SAVINGS", 2 },
            { "LAND_USE_REDUCTION", 2 },
        };

        // Order and values as in the partner document; columns: CO₂ kg, energy kWh, water L, land m².
        private static readonly WheelScenario[] s_Scenarios =
        {
            new WheelScenario("VEGAN_DAY", 165f, WheelAmountUnit.Grams, 4.5f, 3.0f, 100f, 2.5f),
            new WheelScenario("VEGETARIAN_DAY", 165f, WheelAmountUnit.Grams, 3.5f, 2.5f, 75f, 2.0f),
            new WheelScenario("VEGAN_MEAL", 100f, WheelAmountUnit.Grams, 0.8f, 0.5f, 15f, 0.4f),
            new WheelScenario("VEGETARIAN_MEAL", 100f, WheelAmountUnit.Grams, 0.6f, 0.4f, 10f, 0.3f),
            new WheelScenario("BEEF_TO_PORK_100G", 100f, WheelAmountUnit.Grams, 2.0f, 0.7f, 20f, 2.7f),
            new WheelScenario("BEEF_TO_CHICKEN_100G", 100f, WheelAmountUnit.Grams, 2.6f, 0.4f, 44f, 2.9f),
            new WheelScenario("BEEF_TO_LEGUMES_100G", 100f, WheelAmountUnit.Grams, 3.0f, 1.9f, 50f, 3.3f),
            new WheelScenario("PORK_TO_CHICKEN_100G", 100f, WheelAmountUnit.Grams, 0.6f, 1.1f, 24f, 0.2f),
            new WheelScenario("PORK_TO_LEGUMES_100G", 100f, WheelAmountUnit.Grams, 0.9f, 2.6f, 30f, 0.6f),
            new WheelScenario("CHICKEN_TO_LEGUMES_100G", 100f, WheelAmountUnit.Grams, 0.3f, 1.5f, 6f, 0.4f),
            new WheelScenario("HALF_PLATE_SAVED", 50f, WheelAmountUnit.Grams, 0.15f, 0.2f, 8f, 0.2f),
            new WheelScenario("FULL_PLATE_SAVED", 100f, WheelAmountUnit.Grams, 0.3f, 0.4f, 15f, 0.4f),
            new WheelScenario("EXPIRED_PRODUCT_CONSUMED", 100f, WheelAmountUnit.Grams, 0.3f, 0.4f, 15f, 0.4f),
            new WheelScenario("SNACK_TO_FRUIT_OR_NUTS_30G", 30f, WheelAmountUnit.Grams, 0.1f, 0.1f, 5f, 0.05f),
            new WheelScenario("SUGARY_CEREAL_TO_OATS_FRUIT_30G", 30f, WheelAmountUnit.Grams, 0.1f, 0.1f, 5f, 0.05f),
            new WheelScenario("SUGARY_DRINK_TO_WATER_250ML", 250f, WheelAmountUnit.Millilitres, 0.1f, 0.1f, 3f, 0.01f),
            new WheelScenario("READY_MEAL_TO_HOME_COOKED_400G", 400f, WheelAmountUnit.Grams, 0.6f, 1.0f, 50f, 0.5f),
            new WheelScenario("PROCESSED_MEAT_TO_LEGUMES_50G", 50f, WheelAmountUnit.Grams, 0.4f, 0.5f, 25f, 0.3f),
            new WheelScenario("REUSABLE_CONTAINER_USE", 1f, WheelAmountUnit.Use, 0.15f, 0.3f, 2f, 0.01f),
            new WheelScenario("REFILL_PRODUCT", 1f, WheelAmountUnit.Refill, 0.2f, 0.5f, 3f, 0.02f),
        };

        // Bibliographic references: shown in English in every language.
        private static readonly string[] s_Sources =
        {
            "Dudley, N., & Alexander, S. (2017). Agriculture and biodiversity: A review. Biodiversity, 18(2–3), 45–49. https://doi.org/10.1080/14888386.2017.1351892",
            "European Environment Agency. (n.d.). Circular economy. European Environment Agency. Retrieved July 12, 2026, from https://www.eea.europa.eu/en/topics/in-depth/circular-economy",
            "Food and Agriculture Organization of the United Nations. (n.d.). Biodiversity. FAO. Retrieved June 18, 2026, from https://www.fao.org/biodiversity/en",
            "Good Food Institute. (2024). Comparative life cycle assessment of plant and animal-based meat. Retrieved June 20, 2026, from https://gfi.org/wp-content/uploads/2024/11/Comparative-life-cycle-assessment-of-plant-and-animal-based-meats_Key-findings.pdf",
            "Goodwin, L., & Lipinski, B. (2024). How much food does the world really waste? What we know—and what we don't. World Resources Institute. Retrieved June 2, 2026, from https://www.wri.org/insights/how-much-food-does-the-world-waste",
            "Our World in Data. (2023). Daily meat consumption per person. Retrieved June 15, 2026, from https://ourworldindata.org/grapher/daily-meat-consumption-per-person",
            "Our World in Data. (2019). Land use per kilogram of food product. Retrieved June 5, 2026, from https://ourworldindata.org/grapher/land-use-per-kg-poore",
            "Poore, J., & Nemecek, T. (2018). Reducing food's environmental impacts through producers and consumers. Science, 360(6392), 987–992. https://doi.org/10.1126/science.aaq0216",
            "Scarborough, P., Appleby, P. N., Mizdrak, A., Briggs, A. D. M., Travis, R. C., Bradbury, K. E., & Key, T. J. (2014). Dietary greenhouse gas emissions of meat-eaters, fish-eaters, vegetarians and vegans in the UK. Climatic Change, 125(2), 179–192. https://doi.org/10.1007/s10584-014-1169-1",
            "Springmann, M., Clark, M., Mason-D'Croz, D., Wiebe, K., Bodirsky, B. L., Lassaletta, L., de Vries, W., Vermeulen, S. J., Herrero, M., Carlson, K. M., Jonell, M., Troell, M., DeClerck, F., Gordon, L. J., Zurayk, R., Scarborough, P., Rayner, M., Loken, B., Fanzo, J., & Willett, W. (2018). Options for keeping the food system within environmental limits. Nature, 562(7728), 519–525. https://doi.org/10.1038/s41586-018-0594-0",
            "United Nations Environment Programme. (2024). Food Waste Index Report. Retrieved June 10, 2026, from https://www.unep.org/resources/publication/food-waste-index-report-2024",
            "United Nations Environment Programme. (2018). Single-use plastics: A roadmap for sustainability. Retrieved June 15, 2026, from https://www.unep.org/resources/report/single-use-plastics-roadmap-sustainability",
            "United States Environmental Protection Agency. (2021). From Farm to Kitchen: The Environmental Impacts of U.S. Food Waste. Retrieved June 25, 2026, from https://www.epa.gov/land-research/farm-kitchen-environmental-impacts-us-food-waste",
            "Water Footprint Network. (n.d.). Water footprint assessment datasets. Retrieved June 7, 2026, from https://www.waterfootprintassessmenttool.org/",
        };

        public static IReadOnlyList<WheelScenario> Scenarios => s_Scenarios;
        public static IReadOnlyList<string> Sources => s_Sources;

        private static bool IsKnownKind(string kind) => kind != null && s_ParagraphCounts.ContainsKey(kind);

        public static int ParagraphCount(string kind)
        {
            return kind != null && s_ParagraphCounts.TryGetValue(kind, out int count) ? count : 0;
        }

        public static string TitleKey(string kind) => IsKnownKind(kind) ? $"WHEEL_INFO_{kind}_TITLE" : null;
        public static string SubtitleKey(string kind) => IsKnownKind(kind) ? $"WHEEL_INFO_{kind}_SUBTITLE" : null;
        public static string ValueHeaderKey(string kind) => IsKnownKind(kind) ? $"WHEEL_INFO_{kind}_VALUE_HEADER" : null;

        /// <summary>Key of paragraph <paramref name="n"/> (1-based) of a wheel's introduction.</summary>
        public static string ParagraphKey(string kind, int n)
        {
            return n >= 1 && n <= ParagraphCount(kind) ? $"WHEEL_INFO_{kind}_P{n}" : null;
        }

        public static string ScenarioKey(string code) => string.IsNullOrEmpty(code) ? null : $"WHEEL_INFO_SCENARIO_{code}";

        public static string AmountKey(WheelAmountUnit unit)
        {
            switch (unit)
            {
                case WheelAmountUnit.Grams: return "WHEEL_INFO_AMOUNT_G";
                case WheelAmountUnit.Millilitres: return "WHEEL_INFO_AMOUNT_ML";
                case WheelAmountUnit.Use: return "WHEEL_INFO_AMOUNT_USE";
                case WheelAmountUnit.Refill: return "WHEEL_INFO_AMOUNT_REFILL";
                default: return null;
            }
        }

        public static IEnumerable<string> AllKeys()
        {
            foreach (string key in s_SharedKeys)
            {
                yield return key;
            }
            foreach (string kind in ProgressWheelText.KnownKinds)
            {
                yield return TitleKey(kind);
                yield return SubtitleKey(kind);
                yield return ValueHeaderKey(kind);
                for (int n = 1; n <= ParagraphCount(kind); n++)
                {
                    yield return ParagraphKey(kind, n);
                }
            }
            foreach (WheelScenario scenario in s_Scenarios)
            {
                yield return ScenarioKey(scenario.Code);
            }
            foreach (WheelAmountUnit unit in (WheelAmountUnit[])Enum.GetValues(typeof(WheelAmountUnit)))
            {
                yield return AmountKey(unit);
            }
        }
    }
}
