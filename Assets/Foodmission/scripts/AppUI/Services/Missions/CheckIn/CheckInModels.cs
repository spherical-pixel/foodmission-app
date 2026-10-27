using System;
using System.Collections.Generic;
using System.Linq;

namespace eu.foodmission.platform
{
    public static class CheckInMealTypes
    {
        public static readonly string[] All = { "BREAKFAST", "LUNCH", "SNACK", "DINNER" };
        public static readonly string[] Main = { "BREAKFAST", "LUNCH", "DINNER" };
    }

    public sealed class CheckInMission
    {
        public string Code { get; }
        public string Title { get; }
        public MissionInteraction Interaction { get; }
        public DateTime? StartLocal { get; }
        /// <summary>Backend progress 0..1: day questions stop once a mission has enough.</summary>
        public float Progress { get; }

        public CheckInMission(string code, string title, MissionInteraction interaction, DateTime? startLocal, float progress = 0f)
        {
            Code = code;
            Title = title;
            Interaction = interaction ?? MissionInteraction.Unknown;
            StartLocal = startLocal;
            Progress = progress;
        }
    }

    public sealed class CheckInInputs
    {
        public readonly List<CheckInMission> Missions = new List<CheckInMission>();
        /// <summary>Local date → typeOfMeal values already logged that day.</summary>
        public readonly Dictionary<DateTime, HashSet<string>> LoggedMealTypes = new Dictionary<DateTime, HashSet<string>>();
        /// <summary>Event type → days (dayBucket) already recorded, from recentEvents.</summary>
        public readonly Dictionary<string, HashSet<DateTime>> CoveredEventDays = new Dictionary<string, HashSet<DateTime>>(StringComparer.Ordinal);
        /// <summary>Event type → local day → upper-cased array values already recorded that day (DayChecklist steps).</summary>
        public readonly Dictionary<string, Dictionary<DateTime, HashSet<string>>> RecordedValues =
            new Dictionary<string, Dictionary<DateTime, HashSet<string>>>(StringComparer.Ordinal);
        /// <summary>Mission code → days whose day question was already answered on this device.</summary>
        public readonly Dictionary<string, HashSet<DateTime>> DecidedDays = new Dictionary<string, HashSet<DateTime>>(StringComparer.OrdinalIgnoreCase);
        /// <summary>Events reported from the product detail (real food ids), for the "already counted" hint.</summary>
        public readonly List<CheckInProductReport> ProductReports = new List<CheckInProductReport>();
        public DateTime NowLocal;
    }

    /// <summary>One product-detail event: its food, time, array values and string metadata (to match fixed metadata).</summary>
    public sealed class CheckInProductReport
    {
        public string EventType { get; }
        public string Id { get; }
        public DateTime AtLocal { get; }
        public IReadOnlyCollection<string> Values { get; }
        public IReadOnlyDictionary<string, string> Metadata { get; }

        public CheckInProductReport(string eventType, string id, DateTime atLocal, IReadOnlyCollection<string> values = null,
            IReadOnlyDictionary<string, string> metadata = null)
        {
            EventType = eventType;
            Id = id;
            AtLocal = atLocal;
            Values = values ?? Array.Empty<string>();
            Metadata = metadata ?? new Dictionary<string, string>();
        }
    }

    public sealed class CheckInMealQuestion
    {
        public string EventType { get; }
        public string LabelKey { get; }
        public bool IsSwap => EventType.StartsWith("SWAP_", StringComparison.Ordinal);
        /// <summary>Meal type this question is restricted to (e.g. BREAKFAST), null = any meal.</summary>
        public string OnlyMealType { get; }
        /// <summary>Meal types it is asked for on its day: <see cref="OnlyMealType"/>, minus meals dated before the missions started.</summary>
        public IReadOnlyCollection<string> MealTypes { get; }

        public CheckInMealQuestion(string eventType, string onlyMealType, IReadOnlyCollection<string> mealTypes = null)
        {
            EventType = eventType;
            LabelKey = MealFlagLabels.KeyFor(eventType);
            OnlyMealType = onlyMealType;
            MealTypes = mealTypes ?? (onlyMealType != null ? new[] { onlyMealType } : CheckInMealTypes.All);
        }

        public bool AppliesTo(string mealType) => MealTypes.Contains(mealType);
    }

    /// <summary>One mission's question about one day (sent in that day's OTHER meal log).</summary>
    public sealed class CheckInDayQuestion
    {
        public string MissionCode { get; }
        public string MissionTitle { get; }
        public MissionReportStep Step { get; }
        public DateTime? StartLocal { get; }
        /// <summary>Days still needed when the plan was made; null = mission with a limit (never pruned).</summary>
        public int? Remaining { get; }
        /// <summary>"Had meat?" is also asked per meal that day: shown only while no such meal is chosen.</summary>
        public bool DerivedFromMeals { get; }

        public CheckInDayQuestion(string missionCode, string missionTitle, MissionReportStep step, DateTime? startLocal, int? remaining,
            bool derivedFromMeals = false)
        {
            DerivedFromMeals = derivedFromMeals;
            MissionCode = missionCode;
            MissionTitle = missionTitle;
            Step = step;
            StartLocal = startLocal;
            Remaining = remaining;
        }
    }

    public sealed class CheckInMissionRef
    {
        public string Code { get; }
        public string Title { get; }
        public DateTime? StartLocal { get; }

        public CheckInMissionRef(string code, string title, DateTime? startLocal)
        {
            Code = code;
            Title = title;
            StartLocal = startLocal;
        }
    }

    /// <summary>A mission still waiting for past days (Home missing-days nudge).</summary>
    public sealed class CheckInWaitingMission
    {
        public string Code { get; }
        public string Title { get; }
        public DateTime? StartLocal { get; }
        /// <summary>Oldest first.</summary>
        public IReadOnlyList<DateTime> Days { get; }

        public CheckInWaitingMission(string code, string title, DateTime? startLocal, IReadOnlyList<DateTime> days)
        {
            Code = code;
            Title = title;
            StartLocal = startLocal;
            Days = days;
        }
    }

    public sealed class CheckInMealDay
    {
        public DateTime Day { get; }
        public bool HasAnyLog { get; }
        public IReadOnlyList<string> OpenMealTypes { get; }
        public IReadOnlyList<CheckInMealQuestion> Questions { get; }
        /// <summary>UI.csv keys of what the missions asked that day want (see <see cref="MealStepHints"/>).</summary>
        public IReadOnlyList<string> HintKeys { get; }
        /// <summary>Day-level questions, shown above the meals.</summary>
        public IReadOnlyList<CheckInDayQuestion> DayQuestions { get; }
        /// <summary>Missions that ask something this day: day questions, and meal missions when the day has no log.</summary>
        public IReadOnlyList<CheckInMissionRef> Waiting { get; }

        public CheckInMealDay(DateTime day, bool hasAnyLog, IReadOnlyList<string> openMealTypes, IReadOnlyList<CheckInMealQuestion> questions,
            IReadOnlyList<string> hintKeys = null, IReadOnlyList<CheckInDayQuestion> dayQuestions = null, IReadOnlyList<CheckInMissionRef> waiting = null)
        {
            Day = day;
            HasAnyLog = hasAnyLog;
            OpenMealTypes = openMealTypes;
            Questions = questions;
            HintKeys = hintKeys ?? Array.Empty<string>();
            DayQuestions = dayQuestions ?? Array.Empty<CheckInDayQuestion>();
            Waiting = waiting ?? Array.Empty<CheckInMissionRef>();
        }

        public IReadOnlyList<CheckInMealQuestion> QuestionsFor(string mealType) => Questions.Where(q => q.AppliesTo(mealType)).ToList();
    }

    public sealed class CheckInDayEvent
    {
        public string MissionCode { get; }
        public string MissionTitle { get; }
        public MissionReportStep Step { get; }
        public IReadOnlyList<DateTime> OpenDays { get; }
        /// <summary>Mission start: events dated before it don't count.</summary>
        public DateTime? StartLocal { get; }

        public CheckInDayEvent(string missionCode, MissionReportStep step, IReadOnlyList<DateTime> openDays, DateTime? startLocal = null,
            string missionTitle = null)
        {
            MissionCode = missionCode;
            MissionTitle = missionTitle;
            Step = step;
            OpenDays = openDays;
            StartLocal = startLocal;
        }
    }

    public sealed class CheckInMissionStep
    {
        private static readonly IReadOnlyCollection<string> NoValues = Array.Empty<string>();

        public string MissionCode { get; }
        public string MissionTitle { get; }
        public MissionReportStep Step { get; }
        /// <summary>Mission start: day events are never dated before it.</summary>
        public DateTime? StartLocal { get; }
        /// <summary>DayChecklist: days of the window that don't count yet, oldest first. Empty for other steps.</summary>
        public IReadOnlyList<DateTime> OpenDays { get; }
        /// <summary>DayChecklist: values already recorded per day (shown locked).</summary>
        public IReadOnlyDictionary<DateTime, HashSet<string>> RecordedValues { get; }
        /// <summary>Product steps: foods already reported from the product detail since the mission started.</summary>
        public int FromProducts { get; }

        public CheckInMissionStep(string missionCode, string missionTitle, MissionReportStep step, DateTime? startLocal = null,
            IReadOnlyList<DateTime> openDays = null, IReadOnlyDictionary<DateTime, HashSet<string>> recordedValues = null, int fromProducts = 0)
        {
            FromProducts = fromProducts;
            MissionCode = missionCode;
            MissionTitle = missionTitle;
            Step = step;
            StartLocal = startLocal;
            OpenDays = openDays ?? Array.Empty<DateTime>();
            RecordedValues = recordedValues ?? new Dictionary<DateTime, HashSet<string>>();
        }

        public IReadOnlyCollection<string> RecordedValuesFor(DateTime day) =>
            RecordedValues.TryGetValue(day.Date, out HashSet<string> values) ? values : NoValues;
    }

    public sealed class CheckInPlan
    {
        public static readonly CheckInPlan Empty = new CheckInPlan(new List<CheckInMealDay>(), new List<CheckInDayEvent>(), new List<CheckInMissionStep>());

        /// <summary>Missions with nothing left to ask that only wait for their window to end (told on an up-to-date check-in).</summary>
        public IReadOnlyList<CheckInMissionRef> Secured { get; }

        public List<CheckInMealDay> MealDays { get; }
        public List<CheckInDayEvent> DayEvents { get; }
        public List<CheckInMissionStep> MissionSteps { get; }

        public int StepCount => MealDays.Count + DayEvents.Count + MissionSteps.Count;
        public bool IsEmpty => StepCount == 0;

        public CheckInPlan(List<CheckInMealDay> mealDays, List<CheckInDayEvent> dayEvents, List<CheckInMissionStep> missionSteps,
            IReadOnlyList<CheckInMissionRef> secured = null)
        {
            Secured = secured ?? Array.Empty<CheckInMissionRef>();
            MealDays = mealDays;
            DayEvents = dayEvents;
            MissionSteps = missionSteps;
        }

        /// <summary>
        /// Past days the user hasn't told anything about: no meal log at all, or an open day-event day.
        /// Days before <paramref name="notBeforeLocal"/> (e.g. when the quest was first seen) don't count.
        /// </summary>
        public int PendingPastDays(DateTime nowLocal, DateTime? notBeforeLocal = null)
        {
            DateTime today = nowLocal.Date;
            DateTime first = notBeforeLocal?.Date ?? DateTime.MinValue;
            return MealDays.Where(d => !d.HasAnyLog && (d.Questions.Count > 0 || d.DayQuestions.Count > 0)).Select(d => d.Day)
                .Concat(DayEvents.SelectMany(e => e.OpenDays))
                .Concat(MissionSteps.Where(s => s.Step.Type == MissionStepType.DayChecklist)
                    .SelectMany(s => s.OpenDays.Where(d => s.RecordedValuesFor(d).Count == 0)))
                .Where(d => d < today && d >= first)
                .Distinct()
                .Count();
        }

        /// <summary>The pending past days of <see cref="PendingPastDays"/>, per mission waiting for them.</summary>
        public IReadOnlyList<CheckInWaitingMission> WaitingMissions(DateTime nowLocal, DateTime? notBeforeLocal = null)
        {
            DateTime today = nowLocal.Date;
            DateTime first = notBeforeLocal?.Date ?? DateTime.MinValue;
            var pending = new List<(CheckInMissionRef Mission, DateTime Day)>();
            foreach (CheckInMealDay day in MealDays)
            {
                pending.AddRange(day.Waiting.Select(m => (m, day.Day)));
            }
            foreach (CheckInDayEvent dayEvent in DayEvents)
            {
                var mission = new CheckInMissionRef(dayEvent.MissionCode, dayEvent.MissionTitle, dayEvent.StartLocal);
                pending.AddRange(dayEvent.OpenDays.Select(d => (mission, d)));
            }
            foreach (CheckInMissionStep step in MissionSteps.Where(s => s.Step.Type == MissionStepType.DayChecklist))
            {
                var mission = new CheckInMissionRef(step.MissionCode, step.MissionTitle, step.StartLocal);
                pending.AddRange(step.OpenDays.Where(d => step.RecordedValuesFor(d).Count == 0).Select(d => (mission, d)));
            }

            return pending
                .Where(p => p.Day < today && p.Day >= first)
                .GroupBy(p => p.Mission.Code, StringComparer.OrdinalIgnoreCase)
                .Select(g => new CheckInWaitingMission(g.Key, g.First().Mission.Title, g.First().Mission.StartLocal,
                    g.Select(p => p.Day.Date).Distinct().OrderBy(d => d).ToList()))
                .ToList();
        }
    }

    public sealed class CheckInMealDayAnswer
    {
        /// <summary>Meal type → checked event types. A key present means the meal was chosen.</summary>
        public readonly Dictionary<string, HashSet<string>> Meals = new Dictionary<string, HashSet<string>>();
        /// <summary>Day questions: mission code → chosen option index.</summary>
        public readonly Dictionary<string, int> DayAnswers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    }

    public sealed class CheckInAnswers
    {
        public readonly List<CheckInMealDayAnswer> MealDays = new List<CheckInMealDayAnswer>();
        public readonly List<HashSet<DateTime>> DayEvents = new List<HashSet<DateTime>>();
        public readonly List<MissionStepAnswer> MissionSteps = new List<MissionStepAnswer>();

        public static CheckInAnswers For(CheckInPlan plan)
        {
            var answers = new CheckInAnswers();
            foreach (CheckInMealDay _ in plan.MealDays)
            {
                answers.MealDays.Add(new CheckInMealDayAnswer());
            }
            foreach (CheckInDayEvent _ in plan.DayEvents)
            {
                answers.DayEvents.Add(new HashSet<DateTime>());
            }
            foreach (CheckInMissionStep missionStep in plan.MissionSteps)
            {
                var answer = new MissionStepAnswer();
                // A product checklist opens with one product ready to fill
                if (missionStep.Step.Type == MissionStepType.ProductChecklist)
                {
                    answer.Products.Add(new HashSet<int>());
                }
                answers.MissionSteps.Add(answer);
            }
            return answers;
        }
    }
}
