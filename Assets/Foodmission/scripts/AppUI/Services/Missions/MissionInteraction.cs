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
        YesNo,
        ProductChecklist,
        DayChecklist
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

    /// <summary>
    /// Chips per item (a product or a day) for rules that group facts by item (backend groupBy):
    /// the item counts once <see cref="MinDistinct"/> options, including every <see cref="Required"/> one, are checked.
    /// </summary>
    public sealed class ChecklistSpec
    {
        /// <summary>Option metadata key holding the value sent in <see cref="ArrayField"/>.</summary>
        public const string ValueKey = "value";

        private const string DefaultItem = "PRODUCT";

        public IReadOnlyList<MissionStepOption> Options { get; }
        /// <summary>Null: one event per checked option (option event type). Otherwise one event whose metadata[ArrayField] holds the checked values.</summary>
        public string ArrayField { get; }
        public int MinDistinct { get; }
        /// <summary>Option indexes every counted item must include.</summary>
        public IReadOnlyList<int> Required { get; }

        /// <summary>ProductChecklist card title ("Product {0}", "Meal {0}"…), named like the mission goal.</summary>
        public string ItemLabelKey { get; }
        /// <summary>ProductChecklist add button ("Add product", "Add meal"…).</summary>
        public string AddLabelKey { get; }

        public bool IsArray => ArrayField != null;

        /// <param name="item">What one item is in the mission goal: PRODUCT (default), MEAL or PURCHASE.</param>
        public ChecklistSpec(IReadOnlyList<MissionStepOption> options, string arrayField, int minDistinct, IReadOnlyList<int> required, string item = null)
        {
            Options = options ?? Array.Empty<MissionStepOption>();
            ArrayField = arrayField;
            MinDistinct = minDistinct;
            Required = required ?? Array.Empty<int>();
            ItemLabelKey = "MISSION_CHECKLIST_" + (item ?? DefaultItem);
            AddLabelKey = "MISSION_CHECKLIST_ADD_" + (item ?? DefaultItem);
        }

        public string ValueOf(int option) =>
            option >= 0 && option < Options.Count && Options[option].Metadata.TryGetValue(ValueKey, out object value) ? value as string : null;
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
        /// <summary>MealReport: meal types that count (e.g. BREAKFAST, or the main meals), empty = any meal.</summary>
        public IReadOnlyList<string> MealTypes { get; }
        /// <summary>ProductChecklist / DayChecklist: the chips and what makes an item count. Null for other types.</summary>
        public ChecklistSpec Checklist { get; }

        /// <summary>
        /// Each report sends new synthetic ids (or plain occasions), so telling the same thing in two check-ins counts it twice:
        /// the check-in reminds the user to count only what is new. Days, yes/no and distinct values (protein sources) are safe.
        /// </summary>
        public bool AddsUpAcrossReports =>
            Type == MissionStepType.Count ||
            Type == MissionStepType.ProductChecklist ||
            (Type == MissionStepType.OptionPicker && !string.IsNullOrEmpty(DistinctField));

        public MissionReportStep(
            MissionStepType type,
            string promptKey,
            string eventType,
            string distinctField,
            IReadOnlyDictionary<string, object> fixedMetadata,
            IReadOnlyList<MissionStepOption> options,
            int maxCount,
            bool onePerDay,
            IReadOnlyList<string> mealTypes,
            ChecklistSpec checklist = null)
        {
            Type = type;
            PromptKey = promptKey;
            EventType = eventType;
            DistinctField = distinctField;
            FixedMetadata = fixedMetadata ?? new Dictionary<string, object>();
            Options = options ?? Array.Empty<MissionStepOption>();
            MaxCount = maxCount;
            OnePerDay = onePerDay;
            MealTypes = mealTypes ?? Array.Empty<string>();
            Checklist = checklist;
        }
    }

    /// <summary>How a mission is presented and reported. Immutable; built by <see cref="MissionInteractionCatalog"/>.</summary>
    public sealed class MissionInteraction
    {
        public static readonly MissionInteraction Unknown = new MissionInteraction(
            MissionInteractionStatus.PendingRule,
            MissionInteractionType.Report,
            Array.Empty<MissionModuleLink>(),
            Array.Empty<MissionReportStep>());

        public MissionInteractionStatus Status { get; }
        public MissionInteractionType Type { get; }
        /// <summary>Modules that emit the expected events by themselves. Modules that only help (search, pantry…) aren't linked.</summary>
        public IReadOnlyList<MissionModuleLink> AutoModules { get; }
        /// <summary>Modules that help report (e.g. search → product detail mission cards) without emitting events by themselves.</summary>
        public IReadOnlyList<MissionModuleLink> HelperModules { get; }
        public IReadOnlyList<MissionReportStep> Steps { get; }

        public bool CanReport => Status == MissionInteractionStatus.Available && Steps.Count > 0;

        public MissionInteraction(
            MissionInteractionStatus status,
            MissionInteractionType type,
            IReadOnlyList<MissionModuleLink> autoModules,
            IReadOnlyList<MissionReportStep> steps,
            IReadOnlyList<MissionModuleLink> helperModules = null)
        {
            Status = status;
            Type = type;
            AutoModules = autoModules ?? Array.Empty<MissionModuleLink>();
            HelperModules = helperModules ?? Array.Empty<MissionModuleLink>();
            Steps = steps ?? Array.Empty<MissionReportStep>();
        }
    }
}
