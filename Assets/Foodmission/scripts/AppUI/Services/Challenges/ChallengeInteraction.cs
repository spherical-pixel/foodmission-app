namespace eu.foodmission.platform
{
    public enum ChallengeInteractionType
    {
        Confirm,
        ConfirmWithModule,
        Comparator
    }

    public enum ChallengeCompletionTrigger
    {
        Manual,
        ProductViewed,
        FoodFactRead,
        FoodWasteLogged,
        RecipeViewed,
        ComparisonDone
    }

    /// <summary>
    /// How a challenge is presented and completed. Immutable; built by <see cref="ChallengeInteractionCatalog"/>.
    /// </summary>
    public sealed class ChallengeInteraction
    {
        public static readonly ChallengeInteraction Confirm = new ChallengeInteraction(
            ChallengeInteractionType.Confirm, null, null, ChallengeCompletionTrigger.Manual, 0, null, null);

        public ChallengeInteractionType Type { get; }
        /// <summary>Navigation action of the helper module (Actions.*), null for Confirm.</summary>
        public string ModuleAction { get; }
        /// <summary>UI.csv key for the helper button title.</summary>
        public string ModuleButtonKey { get; }
        public ChallengeCompletionTrigger Trigger { get; }
        /// <summary>Distinct items the trigger must report before completing (ProductViewed, FoodWasteLogged).</summary>
        public int RequiredCount { get; }
        /// <summary>Food fact opened by the helper button and required by FoodFactRead.</summary>
        public string FoodFactCode { get; }
        /// <summary>"mode" argument passed to FoodComparisonScreen.</summary>
        public string ComparisonMode { get; }

        public bool AutoCompletes => Trigger != ChallengeCompletionTrigger.Manual;

        public ChallengeInteraction(
            ChallengeInteractionType type,
            string moduleAction,
            string moduleButtonKey,
            ChallengeCompletionTrigger trigger,
            int requiredCount,
            string foodFactCode,
            string comparisonMode)
        {
            Type = type;
            ModuleAction = moduleAction;
            ModuleButtonKey = moduleButtonKey;
            Trigger = trigger;
            RequiredCount = requiredCount;
            FoodFactCode = foodFactCode;
            ComparisonMode = comparisonMode;
        }
    }
}
