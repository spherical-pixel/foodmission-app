namespace eu.foodmission.platform
{
    /// <summary>"Later" choices that last until the app session ends (reset on logout).</summary>
    public static class HomePromptSession
    {
        public static bool OnboardingDeferred;
        public static bool NotificationsDeferred;

        public static void Reset()
        {
            OnboardingDeferred = false;
            NotificationsDeferred = false;
        }
    }
}
