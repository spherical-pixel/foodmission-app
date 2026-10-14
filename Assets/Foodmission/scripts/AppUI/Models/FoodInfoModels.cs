using System.Collections.Generic;

namespace eu.foodmission.platform
{
    public enum FoodInfoType { Product, Generic }

    /// <summary>Key: the UI.csv key of the label, used to highlight the row for a challenge (see ProductFocusMap).</summary>
    public record NutritionRow(string Label, float? Value, string Unit, string Key = null);

    public record NutritionGroup(string Title, List<NutritionRow> Rows);

    public record TrafficLight(string Label, string Level, string Key = null);

    public record MetaRow(string Label, string Value, string Key = null);

    public record DietaryFlags(bool? Vegan, bool? Vegetarian, bool? PalmOilFree);

    public class AddToContextRequestedAction
    {
        public FoodInfoType FoodType;
        public string FoodId;
        public string EntryContext;
        public string FoodData;
    }
}
