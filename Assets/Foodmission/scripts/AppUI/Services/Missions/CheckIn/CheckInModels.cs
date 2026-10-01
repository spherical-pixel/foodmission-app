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

        public CheckInMission(string code, string title, MissionInteraction interaction, DateTime? startLocal)
        {
            Code = code;
            Title = title;
            Interaction = interaction ?? MissionInteraction.Unknown;
            StartLocal = startLocal;
        }
    }

    public sealed class CheckInInputs
    {
        public readonly List<CheckInMission> Missions = new List<CheckInMission>();
        /// <summary>Local date → typeOfMeal values already logged that day.</summary>
        public readonly Dictionary<DateTime, HashSet<string>> LoggedMealTypes = new Dictionary<DateTime, HashSet<string>>();
        /// <summary>Event type → days (dayBucket) already recorded, from recentEvents.</summary>
        public readonly Dictionary<string, HashSet<DateTime>> CoveredEventDays = new Dictionary<string, HashSet<DateTime>>(StringComparer.Ordinal);
        public DateTime NowLocal;
    }

    public sealed class CheckInMealQuestion
    {
        public string EventType { get; }
        public string LabelKey { get; }
        public bool IsSwap => EventType.StartsWith("SWAP_", StringComparison.Ordinal);
        /// <summary>Meal type this question is restricted to (e.g. BREAKFAST), null = any meal.</summary>
        public string OnlyMealType { get; }

        public CheckInMealQuestion(string eventType, string onlyMealType)
        {
            EventType = eventType;
            LabelKey = MealFlagLabels.KeyFor(eventType);
            OnlyMealType = onlyMealType;
        }

        public bool AppliesTo(string mealType) => OnlyMealType == null || OnlyMealType == mealType;
    }

    public sealed class CheckInMealDay
    {
        public DateTime Day { get; }
        public bool HasAnyLog { get; }
        public IReadOnlyList<string> OpenMealTypes { get; }
        public IReadOnlyList<CheckInMealQuestion> Questions { get; }

        public CheckInMealDay(DateTime day, bool hasAnyLog, IReadOnlyList<string> openMealTypes, IReadOnlyList<CheckInMealQuestion> questions)
        {
            Day = day;
            HasAnyLog = hasAnyLog;
            OpenMealTypes = openMealTypes;
            Questions = questions;
        }

        public IReadOnlyList<CheckInMealQuestion> QuestionsFor(string mealType) => Questions.Where(q => q.AppliesTo(mealType)).ToList();
    }

    public sealed class CheckInDayEvent
    {
        public string MissionCode { get; }
        public MissionReportStep Step { get; }
        public IReadOnlyList<DateTime> OpenDays { get; }

        public CheckInDayEvent(string missionCode, MissionReportStep step, IReadOnlyList<DateTime> openDays)
        {
            MissionCode = missionCode;
            Step = step;
            OpenDays = openDays;
        }
    }

    public sealed class CheckInMissionStep
    {
        public string MissionCode { get; }
        public string MissionTitle { get; }
        public MissionReportStep Step { get; }

        public CheckInMissionStep(string missionCode, string missionTitle, MissionReportStep step)
        {
            MissionCode = missionCode;
            MissionTitle = missionTitle;
            Step = step;
        }
    }

    public sealed class CheckInPlan
    {
        public static readonly CheckInPlan Empty = new CheckInPlan(new List<CheckInMealDay>(), new List<CheckInDayEvent>(), new List<CheckInMissionStep>());

        public List<CheckInMealDay> MealDays { get; }
        public List<CheckInDayEvent> DayEvents { get; }
        public List<CheckInMissionStep> MissionSteps { get; }

        public int StepCount => MealDays.Count + DayEvents.Count + MissionSteps.Count;
        public bool IsEmpty => StepCount == 0;

        public CheckInPlan(List<CheckInMealDay> mealDays, List<CheckInDayEvent> dayEvents, List<CheckInMissionStep> missionSteps)
        {
            MealDays = mealDays;
            DayEvents = dayEvents;
            MissionSteps = missionSteps;
        }

        /// <summary>Past days the user hasn't told anything about: no meal log at all, or an open day-event day.</summary>
        public int PendingPastDays(DateTime nowLocal)
        {
            DateTime today = nowLocal.Date;
            return MealDays.Where(d => !d.HasAnyLog && d.Questions.Count > 0).Select(d => d.Day)
                .Concat(DayEvents.SelectMany(e => e.OpenDays))
                .Where(d => d < today)
                .Distinct()
                .Count();
        }
    }

    public sealed class CheckInMealDayAnswer
    {
        /// <summary>Meal type → checked event types. A key present means the meal was chosen.</summary>
        public readonly Dictionary<string, HashSet<string>> Meals = new Dictionary<string, HashSet<string>>();
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
            foreach (CheckInMissionStep _ in plan.MissionSteps)
            {
                answers.MissionSteps.Add(new MissionStepAnswer());
            }
            return answers;
        }
    }
}
