using System;

using Unity.AppUI.UI;

using UnityEngine.Localization.Settings;
using UnityEngine.UIElements;

namespace eu.foodmission.platform.Components
{
    /// <summary>"Show previous levels (N)" / "Hide previous levels" button at the top of a dimension in the content lists.</summary>
    public static class PreviousLevelsRow
    {
        public const string RowClass = "fm-previous-levels-row";

        /// <summary>Null when the dimension has nothing below the user's level.</summary>
        public static VisualElement Create(int previousCount, bool showing, Action onToggle)
        {
            if (previousCount <= 0)
            {
                return null;
            }

            var button = new Unity.AppUI.UI.Button
            {
                title = showing
                    ? LocalizationSettings.StringDatabase.GetLocalizedString("UI", "LEVELS_HIDE_PREVIOUS")
                    : LocalizationSettings.StringDatabase.GetLocalizedString("UI", "LEVELS_SHOW_PREVIOUS", new object[] { previousCount }),
                quiet = true,
                size = Size.M,
                trailingIcon = showing ? "caret-down" : "caret-right"
            };
            button.AddToClassList(RowClass);
            if (onToggle != null)
            {
                button.clicked += onToggle;
            }
            return button;
        }
    }
}
