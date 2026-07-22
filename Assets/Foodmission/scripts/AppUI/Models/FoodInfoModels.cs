using System.Collections.Generic;

namespace eu.foodmission.platform
{
    public enum FoodInfoType { Product, Generic }

    public record NutritionRow(string Label, float? Value, string Unit);

    public record NutritionGroup(string Title, List<NutritionRow> Rows);

    public record TrafficLight(string Label, string Level);

    public record MetaRow(string Label, string Value);

    public class AddToContextRequestedAction
    {
        public FoodInfoType FoodType;
        public string FoodId;
        public string EntryContext;
        public string FoodData;
    }
}
