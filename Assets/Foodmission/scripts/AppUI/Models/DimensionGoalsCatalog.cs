using System;
using System.Collections.Generic;
using System.Linq;

namespace eu.foodmission.platform
{
    public class GoalTopicDefinition
    {
        public string Code { get; }

        public GoalTopicDefinition(string code)
        {
            Code = code;
        }
    }

    public class GoalDimensionDefinition
    {
        public string DimensionCode { get; }
        public string QuestionKey { get; }
        public GoalTopicDefinition[] Topics { get; }

        public GoalDimensionDefinition(string dimensionCode, string questionKey, GoalTopicDefinition[] topics)
        {
            DimensionCode = dimensionCode;
            QuestionKey = questionKey;
            Topics = topics ?? Array.Empty<GoalTopicDefinition>();
        }
    }

    public static class DimensionGoalsCatalog
    {
        public const string WelcomeNutriKey = "GOALS_WELCOME_NUTRI";

        public static readonly GoalDimensionDefinition[] Dimensions =
        {
            new GoalDimensionDefinition(
                DimensionCode.DietChanges,
                "GOALS_Q_DIM_1",
                new[]
                {
                    new GoalTopicDefinition(TopicCode.ReducingMeatConsumption),
                    new GoalTopicDefinition(TopicCode.IncreasingOtherProteinSources),
                    new GoalTopicDefinition(TopicCode.AlternativeStapleFoods)
                }
            ),
            new GoalDimensionDefinition(
                DimensionCode.ProductChoices,
                "GOALS_Q_DIM_2",
                new[]
                {
                    new GoalTopicDefinition(TopicCode.LandUse),
                    new GoalTopicDefinition(TopicCode.WaterUse),
                    new GoalTopicDefinition(TopicCode.EnergyConsumption),
                    new GoalTopicDefinition(TopicCode.CarbonFootprint),
                    new GoalTopicDefinition(TopicCode.TravelDistances)
                }
            ),
            new GoalDimensionDefinition(
                DimensionCode.ProductionMethods,
                "GOALS_Q_DIM_3",
                new[]
                {
                    new GoalTopicDefinition(TopicCode.LevelOfProcessing),
                    new GoalTopicDefinition(TopicCode.CountryOfOrigin),
                    new GoalTopicDefinition(TopicCode.FarmingProductionMethods),
                    new GoalTopicDefinition(TopicCode.BreedingMethods),
                    new GoalTopicDefinition(TopicCode.FairTradeLabour)
                }
            ),
            new GoalDimensionDefinition(
                DimensionCode.Packaging,
                "GOALS_Q_DIM_4",
                new[]
                {
                    new GoalTopicDefinition(TopicCode.SustainabilityOfPackagingMaterials),
                    new GoalTopicDefinition(TopicCode.CapacityToReusePackaging)
                }
            ),
            new GoalDimensionDefinition(
                DimensionCode.FoodWaste,
                "GOALS_Q_DIM_5",
                new[]
                {
                    new GoalTopicDefinition(TopicCode.PlateWaste),
                    new GoalTopicDefinition(TopicCode.LeftoversWaste),
                    new GoalTopicDefinition(TopicCode.ExpiredFood),
                    new GoalTopicDefinition(TopicCode.Overconsumption)
                }
            ),
            new GoalDimensionDefinition(
                DimensionCode.NutritionValues,
                "GOALS_Q_DIM_6",
                new[]
                {
                    new GoalTopicDefinition(TopicCode.Protein),
                    new GoalTopicDefinition(TopicCode.Fat),
                    new GoalTopicDefinition(TopicCode.Sugar),
                    new GoalTopicDefinition(TopicCode.Salt),
                    new GoalTopicDefinition(TopicCode.Fiber),
                    new GoalTopicDefinition(TopicCode.Vitamins),
                    new GoalTopicDefinition(TopicCode.EnergyValueCalories)
                }
            )
        };

        public static readonly string[] AllTopicCodes = Dimensions
            .SelectMany(d => d.Topics)
            .Select(t => t.Code)
            .ToArray();

        public static GoalTopicDefinition FindTopic(string topicCode)
        {
            if (string.IsNullOrEmpty(topicCode)) return null;
            return Dimensions
                .SelectMany(d => d.Topics)
                .FirstOrDefault(t => string.Equals(t.Code, topicCode, StringComparison.OrdinalIgnoreCase));
        }

        public static GoalDimensionDefinition GetDimension(int index)
        {
            if (index < 0 || index >= Dimensions.Length) return null;
            return Dimensions[index];
        }
    }
}
