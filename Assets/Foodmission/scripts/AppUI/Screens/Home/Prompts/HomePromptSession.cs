namespace eu.foodmission.platform
{
    /// <summary>"Later" choices that last until the app session ends (reset on logout).</summary>
    public static class HomePromptSession
    {
        public static bool OnboardingDeferred;
        public static bool NotificationsDeferred;
        /// <summary>Nutri already invited the user to choose a quest this session.</summary>
        public static bool NoQuestReminded;
        /// <summary>Nutri already reminded the current quest's missions waiting to start this session.</summary>
        public static bool MissionsToStartReminded;

        public static void Reset()
        {
            OnboardingDeferred = false;
            NotificationsDeferred = false;
            NoQuestReminded = false;
            MissionsToStartReminded = false;
        }
    }
}
