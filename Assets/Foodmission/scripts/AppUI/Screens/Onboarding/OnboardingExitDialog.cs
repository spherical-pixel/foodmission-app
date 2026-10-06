using System;

using eu.foodmission.platform.Components;

using UnityEngine.Localization.Settings;
using UnityEngine.UIElements;

namespace eu.foodmission.platform
{
    /// <summary>
    /// Confirmation shown by the ✕ of the onboarding steps. Each step explains what stays pending;
    /// leaving silences Home's onboarding reminder until the next session.
    /// </summary>
    public static class OnboardingExitDialog
    {
        public const string ProfileMessage = "ONBOARDING_CLOSE_PROFILE_MESSAGE";
        public const string GoalsMessage = "ONBOARDING_CLOSE_GOALS_MESSAGE";
        public const string SurveyMessage = "ONBOARDING_CLOSE_SURVEY_MESSAGE";
        public const string LevelsMessage = "ONBOARDING_CLOSE_LEVELS_MESSAGE";

        public static void Show(VisualElement anchor, string messageKey, Action onLeave)
        {
            FMDialog.ShowConfirm(anchor,
                LocalizationSettings.StringDatabase.GetLocalizedString("UI", "ONBOARDING_CLOSE_TITLE"),
                LocalizationSettings.StringDatabase.GetLocalizedString("UI", messageKey),
                () =>
                {
                    HomeScreen.DeferOnboardingReminder();
                    onLeave?.Invoke();
                },
                confirmLabel: "@UI:ONBOARDING_CLOSE_CONFIRM",
                cancelLabel: "@UI:TXT_CONTINUE");
        }
    }
}
