namespace eu.foodmission.platform
{
    public enum FootprintConfidence
    {
        Exact,
        Estimated
    }

    public enum ProteinClass
    {
        Beef,
        Lamb,
        Pork,
        Poultry,
        UnspecifiedMeat,
        Fish,
        Cheese,
        Eggs,
        PlantAlternative,
        Legumes
    }

    public struct FootprintMetric
    {
        public float CarbonFootprintKg;       // kg CO2e per kg
        public float CompositeImpactScore;    // 0 to 100 (PNAS 2022 linear scale)
        public FootprintConfidence Confidence;
        public string ScientificReference;
    }

    public interface IFoodFootprintCalculator
    {
        FootprintMetric CalculateProductFootprint(FoodProduct product);
        FootprintMetric CalculateGenericFoodFootprint(GenericFood food);
        bool IsProtein(GenericFood food);
        bool IsProtein(FoodProduct product);
    }
}
