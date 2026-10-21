namespace eu.foodmission.platform
{
    /// <summary>UI table keys of the onboarding progress screen and the onboarding Skip button.</summary>
    public static class OnboardingProgressTexts
    {
        public const string SkipKey = "ONBOARDING_SKIP";
        public const string SkipStepKey = "ONBOARDING_PROGRESS_SKIP_STEP";
        public const string MinutesKey = "ONBOARDING_PROGRESS_MINUTES";
        public const string OptionalKey = "ONBOARDING_PROGRESS_OPTIONAL";
        public const string CloseMessageKey = "ONBOARDING_CLOSE_PROGRESS_MESSAGE";

        public static string StepTitleKey(OnboardingStep step) => "ONBOARDING_PROGRESS_STEP_" + StepSuffix(step);

        public static string NutriMessageKey(OnboardingStep step) => "ONBOARDING_PROGRESS_NUTRI_" + StepSuffix(step);

        /// <summary>Screen-reader state of a step row.</summary>
        public static string StateKey(OnboardingStepState state)
        {
            switch (state)
            {
                case OnboardingStepState.Current:
                    return "ONBOARDING_PROGRESS_STATE_CURRENT";
                case OnboardingStepState.Done:
                    return "ONBOARDING_PROGRESS_STATE_DONE";
                case OnboardingStepState.Skipped:
                    return "ONBOARDING_PROGRESS_STATE_SKIPPED";
                default:
                    return "ONBOARDING_PROGRESS_STATE_PENDING";
            }
        }

        /// <summary>Rough duration shown under pending steps; null for the optional avatar.</summary>
        public static int? Minutes(OnboardingStep step)
        {
            switch (step)
            {
                case OnboardingStep.Profile:
                    return 2;
                case OnboardingStep.Goals:
                    return 1;
                case OnboardingStep.Survey:
                    return 2;
                default:
                    return null;
            }
        }

        private static string StepSuffix(OnboardingStep step)
        {
            switch (step)
            {
                case OnboardingStep.Goals:
                    return "GOALS";
                case OnboardingStep.Survey:
                    return "SURVEY";
                case OnboardingStep.Avatar:
                    return "AVATAR";
                default:
                    return "PROFILE";
            }
        }
    }
}
