using System;
using System.Collections.Generic;

namespace eu.foodmission.platform
{
    /// <summary>User answer to one <see cref="MissionReportStep"/>; only the fields of the step's type are read.</summary>
    public sealed class MissionStepAnswer
    {
        public int Count;
        public bool Confirmed;
        public readonly List<DateTime> Days = new List<DateTime>();
        public readonly Dictionary<int, int> OptionCounts = new Dictionary<int, int>();
        /// <summary>ProductChecklist: checked option indexes of each product.</summary>
        public readonly List<HashSet<int>> Products = new List<HashSet<int>>();
        /// <summary>DayChecklist: day → option indexes checked in this check-in (already recorded values excluded).</summary>
        public readonly Dictionary<DateTime, HashSet<int>> DayValues = new Dictionary<DateTime, HashSet<int>>();
    }

    /// <summary>One request of a manual report. Exactly one of Event / MealLog is set.</summary>
    public sealed class PendingReportItem
    {
        public CreateClientEventRequest Event { get; }
        public CreateMealLogRequest MealLog { get; }
        public bool Sent { get; set; }

        public PendingReportItem(CreateClientEventRequest clientEvent)
        {
            Event = clientEvent;
        }

        public PendingReportItem(CreateMealLogRequest mealLog)
        {
            MealLog = mealLog;
        }
    }

    public sealed class MissionReportSendResult
    {
        public static readonly MissionReportSendResult Ok = new MissionReportSendResult(true, null);

        public bool Success { get; }
        public ApiErrorResponse Error { get; }

        private MissionReportSendResult(bool success, ApiErrorResponse error)
        {
            Success = success;
            Error = error;
        }

        public static MissionReportSendResult Fail(ApiErrorResponse error) =>
            new MissionReportSendResult(false, error ?? new ApiErrorResponse());
    }

    public sealed class MissionReportOutcome
    {
        public float Progress { get; }
        public bool Completed { get; }

        public MissionReportOutcome(float progress, bool completed)
        {
            Progress = progress;
            Completed = completed;
        }
    }

    public enum CheckInStepKind
    {
        MealDay,
        DayEvent,
        MissionStep,
        Summary
    }

    public sealed class CheckInSummaryLine
    {
        public CheckInStepKind Kind { get; }
        /// <summary>DayEvent / MissionStep: the step's question. MealDay: null (the screen formats the day).</summary>
        public string PromptKey { get; }
        public DateTime? Day { get; }
        public int Count { get; }

        public CheckInSummaryLine(CheckInStepKind kind, string promptKey, DateTime? day, int count)
        {
            Kind = kind;
            PromptKey = promptKey;
            Day = day;
            Count = count;
        }
    }

    public sealed class CheckInOutcome
    {
        public int ItemsSent { get; }
        /// <summary>Single-mission mode: the mission's refreshed progress; null in all-missions mode.</summary>
        public MissionProgress SingleMissionProgress { get; }

        /// <summary>Refreshed progress and its result line for each mission the check-in asked about (fetch failures left out).</summary>
        public IReadOnlyList<(MissionProgress Progress, CheckInResultLine Line)> Missions { get; }

        public CheckInOutcome(int itemsSent, MissionProgress singleMissionProgress,
            IReadOnlyList<(MissionProgress Progress, CheckInResultLine Line)> missions = null)
        {
            ItemsSent = itemsSent;
            SingleMissionProgress = singleMissionProgress;
            Missions = missions ?? Array.Empty<(MissionProgress, CheckInResultLine)>();
        }
    }
}
