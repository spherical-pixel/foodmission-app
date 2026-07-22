using System.Collections.Generic;

using Unity.AppUI.UI;
using UnityEngine.UIElements;

namespace eu.foodmission.platform.Components
{
    [UxmlElement]
    public partial class NutritionMacroCard : ExVisualElement
    {
        private Text _iconText;
        private Text _valueText;
        private Text _labelText;

        // Maps localization key fragments or unit to an emoji indicator
        private static readonly Dictionary<string, string> s_MacroEmojis = new()
        {
            { "kcal",       "🔥" },
            { "kj",         "⚡" },
            { "protein",    "💪" },
            { "fat",        "🧈" },
            { "carb",       "🌾" },
            { "sugar",      "🍬" },
            { "fiber",      "🌿" },
            { "salt",       "🧂" },
            { "sodium",     "🧂" },
            { "water",      "💧" },
        };

        public NutritionMacroCard()
        {
            AddToClassList("fm-fi-macro-card");

            _iconText = new Text { size = TextSize.M };
            _iconText.AddToClassList("fm-fi-macro-card__icon");
            Add(_iconText);

            _valueText = new Text { size = TextSize.L };
            _valueText.AddToClassList("fm-fi-macro-card__value");
            Add(_valueText);

            _labelText = new Text { size = TextSize.S };
            _labelText.AddToClassList("fm-fi-macro-card__label");
            Add(_labelText);
        }

        public void SetData(string label, float? value, string unit)
        {
            _labelText.text = label ?? "";
            _iconText.text = ResolveEmoji(label, unit);

            if (value.HasValue && value.Value > 0)
                _valueText.text = $"{value.Value:F0} {unit}";
            else
                _valueText.text = "\u2014"; // em dash
        }

        private static string ResolveEmoji(string label, string unit)
        {
            string searchText = ((label ?? "") + " " + (unit ?? "")).ToLowerInvariant();

            foreach (var kv in s_MacroEmojis)
            {
                if (searchText.Contains(kv.Key))
                    return kv.Value;
            }
            return "🥗";
        }
    }
}
