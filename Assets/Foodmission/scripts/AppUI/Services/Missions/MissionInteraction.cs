using System;
using System.Collections.Generic;

namespace eu.foodmission.platform
{
    public enum MissionInteractionStatus
    {
        Available,
        PendingRule
    }

    public enum MissionInteractionType
    {
        Report,
        Minigame
    }

    public enum MissionStepType
    {
        Count,
        OptionPicker,
        MealReport,
        DayPicker,
        YesNo
    }

    /// <summary>Navigation action (Actions.*) plus the UI.csv key of its button.</summary>
    public sealed class MissionModuleLink
    {
        public string Action { get; }
        public string ButtonKey { get; }

        public MissionModuleLink(string action, string buttonKey)
        {
            Action = action;
            ButtonKey = buttonKey;
        }
    }

    public sealed class MissionStepOption
    {
        public string LabelKey { get; }
        /// <summary>Event type (OptionPicker) or meal-log flag / swap (MealReport).</summary>
        public string EventType { get; }
        /// <summary>Extra metadata sent with this option, e.g. proteinSource.</summary>
        public IReadOnlyDictionary<string, object> Metadata { get; }

        public bool IsSwap => EventType != null && EventType.StartsWith("SWAP_", StringComparison.Ordinal);

        public MissionStepOption(string labelKey, string eventType, IReadOnlyDictionary<string, object> metadata)
        {
            LabelKey = labelKey;
            EventType = eventType;
            Metadata = metadata ?? new Dictionary<string, object>();
        }
    }

    /// <summary>One question Nutri asks in the manual report (spec §3.2).</summary>
    public sealed class MissionReportStep
    {
        public MissionStepType Type { get; }
        public string PromptKey { get; }
        /// <summary>Count / YesNo / DayPicker: event sent. MealReport: fixed meal-log flag or swap (null when chosen per meal).</summary>
        public string EventType { get; }
        /// <summary>Metadata field filled with a synthetic distinct id (rule distinctBy), e.g. productId. Null = none.</summary>
        public string DistinctField { get; }
        /// <summary>Metadata always sent (rule where filters), e.g. comparedWith = GREEN_SCORE.</summary>
        public IReadOnlyDictionary<string, object> FixedMetadata { get; }
        public IReadOnlyList<MissionStepOption> Options { get; }
        /// <summary>Count: max value. OptionPicker: max per option. MealReport: max meals. DayPicker: max days.</summary>
        public int MaxCount { get; }
        /// <summary>MealReport: at most one meal per day (rules counted by mealDayBucket).</summary>
        public bool OnePerDay { get; }
        /// <summary>MealReport: meal type forced for every meal (e.g. BREAKFAST), null = user picks.</summary>
        public string FixedMealType { get; }

        public MissionReportStep(
            MissionStepType type,
            string promptKey,
            string eventType,
            string distinctField,
            IReadOnlyDictionary<string, object> fixedMetadata,
            IReadOnlyList<MissionStepOption> options,
            int maxCount,
            bool onePerDay,
            string fixedMealType)
        {
            Type = type;
            PromptKey = promptKey;
            EventType = eventType;
            DistinctField = distinctField;
            FixedMetadata = fixedMetadata ?? new Dictionary<string, object>();
            Options = options ?? Array.Empty<MissionStepOption>();
            MaxCount = maxCount;
            OnePerDay = onePerDay;
            FixedMealType = fixedMealType;
        }
    }

    /// <summary>How a mission is presented and reported. Immutable; built by <see cref="MissionInteractionCatalog"/>.</summary>
    public sealed class MissionInteraction
    {
        public static readonly MissionInteraction Unknown = new MissionInteraction(
            MissionInteractionStatus.PendingRule,
            MissionInteractionType.Report,
            Array.Empty<MissionModuleLink>(),
            Array.Empty<MissionModuleLink>(),
            Array.Empty<MissionReportStep>());

        public MissionInteractionStatus Status { get; }
        public MissionInteractionType Type { get; }
        /// <summary>Modules that emit the expected events by themselves.</summary>
        public IReadOnlyList<MissionModuleLink> AutoModules { get; }
        /// <summary>Modules that help with the task but do not emit events.</summary>
        public IReadOnlyList<MissionModuleLink> HelperModules { get; }
        public IReadOnlyList<MissionReportStep> Steps { get; }

        public bool CanReport => Status == MissionInteractionStatus.Available && Steps.Count > 0;

        public MissionInteraction(
            MissionInteractionStatus status,
            MissionInteractionType type,
            IReadOnlyList<MissionModuleLink> autoModules,
            IReadOnlyList<MissionModuleLink> helperModules,
            IReadOnlyList<MissionReportStep> steps)
        {
            Status = status;
            Type = type;
            AutoModules = autoModules ?? Array.Empty<MissionModuleLink>();
            HelperModules = helperModules ?? Array.Empty<MissionModuleLink>();
            Steps = steps ?? Array.Empty<MissionReportStep>();
        }
    }
}
