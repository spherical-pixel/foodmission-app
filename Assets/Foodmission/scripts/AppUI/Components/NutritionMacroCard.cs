using Unity.AppUI.UI;
using UnityEngine.UIElements;

namespace eu.foodmission.platform.Components
{
    [UxmlElement]
    public partial class NutritionMacroCard : ExVisualElement
    {
        private Text _valueText;
        private Text _labelText;

        public NutritionMacroCard()
        {
            AddToClassList("fm-fi-macro-card");

            _valueText = new Text { size = TextSize.L };
            _valueText.AddToClassList("fm-fi-macro-card__value");
            Add(_valueText);

            _labelText = new Text { size = TextSize.S };
            _labelText.AddToClassList("fm-fi-macro-card__label");
            Add(_labelText);
        }

        public void SetData(string label, float? value, string unit)
        {
            _labelText.text = label;
            if (value.HasValue && value.Value > 0)
                _valueText.text = $"{value.Value:F0} {unit}";
            else
                _valueText.text = "\u2014";  // em dash
        }
    }
}
