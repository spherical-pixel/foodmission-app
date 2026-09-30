using System;
using System.Collections.Generic;

namespace eu.foodmission.platform
{
    /// <summary>
    /// Calculates food carbon footprint and composite environmental scores.
    ///
    /// Resolution order:
    /// 1. CO2 value delivered by the backend (<see cref="FoodProduct.carbonFootprint"/>) → Exact.
    /// 2. Estimate from structured taxonomy codes only — never from product/food names:
    ///    - Commercial products: OpenFoodFacts category tags. OFF stores the full ancestor chain in
    ///      categories_tags, so matching a few hub tags exactly is enough (no substring matching).
    ///    - Generic foods: NEVO food group slug (always the English slug) and, for meat, the
    ///      LanguaL facet B (source animal) codes.
    /// 3. No usable classification → default benchmark.
    ///
    /// Scientific Literature Citations:
    /// 1. Clark, M., Springmann, M., Rayner, M., Scarborough, P., Hill, J., Tilman, D.,
    ///    Macdiarmid, J. I., Fanzo, J., Bandy, L., & Harrington, R. A. (2022).
    ///    "Estimating the environmental impacts of 57,000 food products."
    ///    Proceedings of the National Academy of Sciences (PNAS), 119(33), e2120584119.
    ///    https://doi.org/10.1073/pnas.2120584119
    ///
    /// 2. Poore, J., & Nemecek, T. (2018).
    ///    "Reducing food's environmental impacts through producers and consumers."
    ///    Science, 360(6392), 987-992.
    ///
    /// 3. Gephart, J. A., et al. (2021).
    ///    "Environmental performance of blue foods."
    ///    Nature, 597, 360-365.
    /// </summary>
    public class FoodFootprintCalculator : IFoodFootprintCalculator
    {
        private const string PrimaryReference = "Clark et al. (PNAS 2022) / Poore & Nemecek (Science 2018)";

        // Benchmark kg CO2e / kg of food from Poore & Nemecek (2018) & Clark et al. (2022)
        public const float BeefFootprintKg = 60.0f;
        public const float LambFootprintKg = 24.0f;
        public const float CheeseFootprintKg = 21.0f;
        public const float PorkFootprintKg = 7.0f;
        public const float PoultryFootprintKg = 6.0f;
        public const float FishFootprintKg = 6.5f;
        public const float EggsFootprintKg = 4.5f;
        public const float TofuPlantAltFootprintKg = 3.0f;
        public const float LegumesFootprintKg = 1.2f;
        public const float GrainsFootprintKg = 1.5f;
        public const float VegetablesFootprintKg = 0.8f;
        public const float DefaultFootprintKg = 2.0f;

        // Composite Environmental Impact Scores per 100g on a 0-100 linear scale (Clark et al. PNAS 2022, Figs 3-5)
        public const float BeefCompositeScore = 33.0f;
        public const float LambCompositeScore = LambFootprintKg / BeefFootprintKg * BeefCompositeScore; // scaled from beef, same rule as exact values
        public const float CheeseCompositeScore = 12.0f;
        public const float PorkCompositeScore = 7.5f;
        public const float PoultryCompositeScore = 6.0f;
        public const float FishCompositeScore = 6.5f;
        public const float EggsCompositeScore = 4.0f;
        public const float TofuCompositeScore = 1.8f;
        public const float LegumesCompositeScore = 1.0f;
        public const float GrainsCompositeScore = 1.2f;
        public const float VegetablesCompositeScore = 0.8f;
        public const float DefaultCompositeScore = 2.0f;

        // Meat substitutes group also holds plant drinks, yoghurts and vegan cheeses (≤ 4 g protein / 100 g);
        // real meat substitutes (tofu, tempeh, seitan, veggie burgers) are all above this line in NEVO 2025.
        private const float MeatSubstituteMinProteinsPer100g = 6.0f;

        // --- NEVO food group slugs (derived from the English food group, not localized) ---
        public const string SlugMeat = "meat-and-poultry";
        public const string SlugColdMeatCuts = "cold-meat-cuts";
        private const string SlugFish = "fish-crustacean-and-shellfish";
        private const string SlugEggs = "eggs";
        private const string SlugCheese = "cheese";
        private const string SlugLegumes = "legumes";
        private const string SlugMeatSubstitutes = "meat-substitutes-and-dairy-substitutes";

        private static readonly HashSet<string> VegetableSlugs = new(StringComparer.OrdinalIgnoreCase)
        {
            "vegetables", "potatoes-and-tubers", "fruits"
        };

        private static readonly HashSet<string> GrainSlugs = new(StringComparer.OrdinalIgnoreCase)
        {
            "cereal-products-and-types-of-flour", "bread"
        };

        // --- LanguaL facet B (food source) codes as used by NEVO 2025 ---
        private static readonly HashSet<string> BeefLangualCodes = new(StringComparer.OrdinalIgnoreCase) { "B1201", "B1349" };        // cattle, calf
        private static readonly HashSet<string> LambLangualCodes = new(StringComparer.OrdinalIgnoreCase) { "B1669", "B1183" };        // lamb, sheep
        private static readonly HashSet<string> PorkLangualCodes = new(StringComparer.OrdinalIgnoreCase) { "B1136" };                 // swine
        private static readonly HashSet<string> PoultryLangualCodes = new(StringComparer.OrdinalIgnoreCase) { "B1457", "B1236", "B1316" }; // chicken, turkey, duck

        // --- OpenFoodFacts category hub tags (verified against the OFF categories taxonomy) ---
        // en:seafood is avoided on purpose: it also contains seaweeds and agar.
        // en:legumes-and-their-products is avoided on purpose: it also contains green beans, soy drinks, peanut butter and miso.
        private static readonly HashSet<string> OffCheeseTags = new(StringComparer.OrdinalIgnoreCase) { "en:cheeses" };
        private static readonly HashSet<string> OffPlantAlternativeTags = new(StringComparer.OrdinalIgnoreCase) { "en:meat-alternatives", "en:tofu", "xx:tofu" };
        private static readonly HashSet<string> OffBeefTags = new(StringComparer.OrdinalIgnoreCase) { "en:beef-and-its-products", "en:beef", "en:veal-meat" };
        private static readonly HashSet<string> OffLambTags = new(StringComparer.OrdinalIgnoreCase) { "en:lamb-meat", "en:sheep-meat", "en:goat-meat" };
        private static readonly HashSet<string> OffPorkTags = new(StringComparer.OrdinalIgnoreCase) { "en:pork-and-its-products", "en:pork" };
        private static readonly HashSet<string> OffPoultryTags = new(StringComparer.OrdinalIgnoreCase) { "en:poultries", "en:chicken-and-its-products", "en:turkey-and-its-products" };
        private static readonly HashSet<string> OffFishTags = new(StringComparer.OrdinalIgnoreCase) { "en:fishes-and-their-products", "en:crustaceans", "en:mollusc", "en:shellfish" };
        private static readonly HashSet<string> OffEggTags = new(StringComparer.OrdinalIgnoreCase) { "en:eggs" };
        private static readonly HashSet<string> OffLegumeTags = new(StringComparer.OrdinalIgnoreCase) { "en:pulses", "en:soy-beans" };
        private static readonly HashSet<string> OffUnspecifiedMeatTags = new(StringComparer.OrdinalIgnoreCase) { "en:meats", "en:prepared-meats" };

        public FootprintMetric CalculateProductFootprint(FoodProduct product)
        {
            if (product == null)
            {
                return CreateDefaultMetric();
            }

            // 1. CO2 value from the backend
            if (product.carbonFootprint.HasValue && product.carbonFootprint.Value > 0f)
            {
                float kgCo2 = product.carbonFootprint.Value;
                float composite = Math.Clamp((kgCo2 / BeefFootprintKg) * BeefCompositeScore, 0.5f, 100f);
                return new FootprintMetric
                {
                    CarbonFootprintKg = kgCo2,
                    CompositeImpactScore = composite,
                    Confidence = FootprintConfidence.Exact,
                    ScientificReference = "Direct product data / OpenFoodFacts"
                };
            }

            // 2. Estimate from OpenFoodFacts category taxonomy
            FootprintMetric? categoryMetric = ClassifyByOpenFoodFactsCategories(ResolveCategories(product));
            if (categoryMetric.HasValue)
            {
                return categoryMetric.Value;
            }

            return CreateDefaultMetric();
        }

        public FootprintMetric CalculateGenericFoodFootprint(GenericFood food)
        {
            if (food == null)
            {
                return CreateDefaultMetric();
            }

            string slug = food.foodGroupSlug ?? "";

            switch (slug)
            {
                case SlugLegumes:
                    return CreateMetric(LegumesFootprintKg, LegumesCompositeScore);
                case SlugCheese:
                    return CreateMetric(CheeseFootprintKg, CheeseCompositeScore);
                case SlugEggs:
                    return CreateMetric(EggsFootprintKg, EggsCompositeScore);
                case SlugFish:
                    return CreateMetric(FishFootprintKg, FishCompositeScore);
                case SlugMeatSubstitutes:
                    return CreateMetric(TofuPlantAltFootprintKg, TofuCompositeScore);
                case SlugMeat:
                case SlugColdMeatCuts:
                    return ClassifyMeatByLangual(food.langualCodes) ?? CreateUnspecifiedMeatMetric();
            }

            if (VegetableSlugs.Contains(slug))
            {
                return CreateMetric(VegetablesFootprintKg, VegetablesCompositeScore);
            }

            if (GrainSlugs.Contains(slug))
            {
                return CreateMetric(GrainsFootprintKg, GrainsCompositeScore);
            }

            return CreateDefaultMetric();
        }

        public bool IsProtein(GenericFood food)
        {
            if (food == null || string.IsNullOrEmpty(food.foodGroupSlug))
            {
                return false;
            }

            // meatOrFish / legume flags are not used: in NEVO they mean "contains", and are set on
            // soups, sauces, snacks and mixed dishes too.
            switch (food.foodGroupSlug)
            {
                case SlugMeat:
                case SlugColdMeatCuts:
                case SlugFish:
                case SlugEggs:
                case SlugCheese:
                case SlugLegumes:
                    return true;
                case SlugMeatSubstitutes:
                    return food.proteins.HasValue && food.proteins.Value >= MeatSubstituteMinProteinsPer100g;
                default:
                    return false;
            }
        }

        public bool IsProtein(FoodProduct product)
        {
            return ClassifyProduct(product).Count > 0;
        }

        /// <summary>
        /// Protein classes of a commercial product from its OFF category tags (empty = not a protein food).
        /// Several entries only for mixed-species meat. Shared by the footprint estimate and the category shown in the UI.
        /// </summary>
        public static IReadOnlyList<ProteinClass> ClassifyProduct(FoodProduct product)
        {
            return ClassifyOffTags(ResolveCategories(product));
        }

        // Stored products may lack categories while the embedded OFF snapshot has them.
        private static string[] ResolveCategories(FoodProduct product)
        {
            if (product == null)
            {
                return null;
            }

            if (product.categories != null && product.categories.Length > 0)
            {
                return product.categories;
            }

            return product.openFoodFactsInfo?.categories;
        }

        private static List<ProteinClass> ClassifyOffTags(string[] categories)
        {
            var classes = new List<ProteinClass>();
            if (categories == null || categories.Length == 0)
            {
                return classes;
            }

            var tags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string tag in categories)
            {
                if (!string.IsNullOrEmpty(tag))
                {
                    tags.Add(tag);
                }
            }

            // Precedence matters: grilling cheeses are also tagged as meat alternatives,
            // and meat alternatives must win over any animal-derived tag.
            if (tags.Overlaps(OffCheeseTags))
            {
                classes.Add(ProteinClass.Cheese);
                return classes;
            }

            if (tags.Overlaps(OffPlantAlternativeTags))
            {
                classes.Add(ProteinClass.PlantAlternative);
                return classes;
            }

            if (tags.Overlaps(OffBeefTags))
            {
                classes.Add(ProteinClass.Beef);
            }
            if (tags.Overlaps(OffLambTags))
            {
                classes.Add(ProteinClass.Lamb);
            }
            if (tags.Overlaps(OffPorkTags))
            {
                classes.Add(ProteinClass.Pork);
            }
            if (tags.Overlaps(OffPoultryTags))
            {
                classes.Add(ProteinClass.Poultry);
            }
            if (classes.Count > 0)
            {
                return classes;
            }

            if (tags.Overlaps(OffFishTags))
            {
                classes.Add(ProteinClass.Fish);
            }
            else if (tags.Overlaps(OffEggTags))
            {
                classes.Add(ProteinClass.Eggs);
            }
            else if (tags.Overlaps(OffLegumeTags))
            {
                classes.Add(ProteinClass.Legumes);
            }
            else if (tags.Overlaps(OffUnspecifiedMeatTags))
            {
                classes.Add(ProteinClass.UnspecifiedMeat);
            }

            return classes;
        }

        private static FootprintMetric? ClassifyByOpenFoodFactsCategories(string[] categories)
        {
            List<ProteinClass> classes = ClassifyOffTags(categories);
            return classes.Count > 0 ? AverageOf(classes) : (FootprintMetric?)null;
        }

        private static FootprintMetric? ClassifyMeatByLangual(string[] langualCodes)
        {
            if (langualCodes == null || langualCodes.Length == 0)
            {
                return null;
            }

            var codes = new HashSet<string>(langualCodes, StringComparer.OrdinalIgnoreCase);
            var species = new List<ProteinClass>();

            if (codes.Overlaps(BeefLangualCodes))
            {
                species.Add(ProteinClass.Beef);
            }
            if (codes.Overlaps(LambLangualCodes))
            {
                species.Add(ProteinClass.Lamb);
            }
            if (codes.Overlaps(PorkLangualCodes))
            {
                species.Add(ProteinClass.Pork);
            }
            if (codes.Overlaps(PoultryLangualCodes))
            {
                species.Add(ProteinClass.Poultry);
            }

            return species.Count > 0 ? AverageOf(species) : (FootprintMetric?)null;
        }

        private static FootprintMetric MetricFor(ProteinClass proteinClass)
        {
            switch (proteinClass)
            {
                case ProteinClass.Beef:
                    return CreateMetric(BeefFootprintKg, BeefCompositeScore);
                case ProteinClass.Lamb:
                    return CreateMetric(LambFootprintKg, LambCompositeScore);
                case ProteinClass.Pork:
                    return CreateMetric(PorkFootprintKg, PorkCompositeScore);
                case ProteinClass.Poultry:
                    return CreateMetric(PoultryFootprintKg, PoultryCompositeScore);
                case ProteinClass.Fish:
                    return CreateMetric(FishFootprintKg, FishCompositeScore);
                case ProteinClass.Cheese:
                    return CreateMetric(CheeseFootprintKg, CheeseCompositeScore);
                case ProteinClass.Eggs:
                    return CreateMetric(EggsFootprintKg, EggsCompositeScore);
                case ProteinClass.PlantAlternative:
                    return CreateMetric(TofuPlantAltFootprintKg, TofuCompositeScore);
                case ProteinClass.Legumes:
                    return CreateMetric(LegumesFootprintKg, LegumesCompositeScore);
                case ProteinClass.UnspecifiedMeat:
                    return CreateUnspecifiedMeatMetric();
                default:
                    return CreateDefaultMetric();
            }
        }

        private static FootprintMetric AverageOf(List<ProteinClass> classes)
        {
            var metrics = new List<FootprintMetric>(classes.Count);
            foreach (var c in classes)
            {
                metrics.Add(MetricFor(c));
            }
            return Average(metrics);
        }

        // Mixed-species products (e.g. beef & pork mince) use the mean of their species benchmarks.
        private static FootprintMetric Average(List<FootprintMetric> metrics)
        {
            if (metrics.Count == 1)
            {
                return metrics[0];
            }

            float kg = 0f;
            float composite = 0f;
            foreach (var m in metrics)
            {
                kg += m.CarbonFootprintKg;
                composite += m.CompositeImpactScore;
            }

            return CreateMetric(kg / metrics.Count, composite / metrics.Count);
        }

        // Meat whose species is unknown: conservative monogastric (poultry) benchmark.
        private static FootprintMetric CreateUnspecifiedMeatMetric()
        {
            return CreateMetric(PoultryFootprintKg, PoultryCompositeScore);
        }

        private static FootprintMetric CreateMetric(float kgCo2, float composite)
        {
            return new FootprintMetric
            {
                CarbonFootprintKg = kgCo2,
                CompositeImpactScore = composite,
                Confidence = FootprintConfidence.Estimated,
                ScientificReference = PrimaryReference
            };
        }

        private static FootprintMetric CreateDefaultMetric()
        {
            return CreateMetric(DefaultFootprintKg, DefaultCompositeScore);
        }
    }
}
