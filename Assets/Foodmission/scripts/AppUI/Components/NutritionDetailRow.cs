using Unity.AppUI.UI;
using UnityEngine.UIElements;

namespace eu.foodmission.platform.Components
{
    [UxmlElement]
    public partial class NutritionDetailRow : ExVisualElement
    {
        private Text _labelText;
        private Text _valueText;

        public NutritionDetailRow()
        {
            AddToClassList("fm-fi-nutrition-row");
            style.flexDirection = FlexDirection.Row;
            style.justifyContent = Justify.SpaceBetween;

            _labelText = new Text { size = TextSize.S };
            _labelText.AddToClassList("fm-fi-nutrition-row__label");
            Add(_labelText);

            _valueText = new Text { size = TextSize.S };
            _valueText.AddToClassList("fm-fi-nutrition-row__value");
            Add(_valueText);
        }

        public void SetData(string label, float? value, string unit)
        {
            _labelText.text = label;
            if (value.HasValue && value.Value > 0)
                _valueText.text = $"{value.Value:F1} {unit}";
            else
                _valueText.text = "\u2014";
        }
    }
}
