using UnityEngine.Localization.Settings;
using UnityEngine.UIElements;

namespace eu.foodmission.platform.Components
{
    /// <summary>"Your level: X" badge under a dimension title in the content lists (same colours as the cards' level badges).</summary>
    public static class DimensionLevelBadge
    {
        public const string TitleColumnClass = "fm-dim-title-column";
        public const string BadgeClass = "fm-dim-level-badge";

        /// <summary>Wraps the dimension title in a column and adds the user's level under it (nothing when the level is unknown).</summary>
        public static VisualElement WithTitle(VisualElement title, string userLevel)
        {
            var column = new VisualElement();
            column.AddToClassList(TitleColumnClass);
            column.Add(title);

            string level = ContentLevel.Normalize(userLevel);
            if (level == null)
            {
                return column;
            }

            var badge = new VisualElement();
            badge.AddToClassList("fm-quiz-level-badge");
            badge.AddToClassList("fm-quiz-level-badge--" + level.ToLowerInvariant());
            badge.AddToClassList(BadgeClass);

            var text = new Unity.AppUI.UI.Text
            {
                text = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "DIMENSION_LEVEL_YOURS",
                    new object[] { LocalizationSettings.StringDatabase.GetLocalizedString("UI", "SEGMENT_" + level) })
            };
            text.AddToClassList("fm-quiz-level-badge-text");
            badge.Add(text);
            column.Add(badge);
            return column;
        }
    }
}
