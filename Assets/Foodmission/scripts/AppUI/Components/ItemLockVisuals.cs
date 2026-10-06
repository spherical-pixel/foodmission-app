using Unity.AppUI.UI;

using UnityEngine.Localization.Settings;
using UnityEngine.UIElements;

namespace eu.foodmission.platform.Components
{
    /// <summary>Shared "locked" look for content cards (missions, challenges, quizzes, food facts).</summary>
    internal static class ItemLockVisuals
    {
        public const string LockedClass = "fm-item--locked";

        public static void Apply(VisualElement item, VisualElement statusBadge, Icon statusIcon, Unity.AppUI.UI.Text statusText, Icon arrowIcon)
        {
            item.AddToClassList(LockedClass);
            statusBadge.RemoveFromClassList("fm-quiz-status-badge--completed");
            statusBadge.RemoveFromClassList("fm-quiz-status-badge--pending");
            statusBadge.RemoveFromClassList("fm-quiz-status-badge--failed");
            statusBadge.AddToClassList("fm-quiz-status-badge--locked");
            statusIcon.iconName = "lock";
            statusIcon.style.display = DisplayStyle.Flex;
            statusText.text = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "QUEST_STATUS_LOCKED");
            arrowIcon.iconName = "lock";
            arrowIcon.style.opacity = 0.5f;
        }
    }
}
