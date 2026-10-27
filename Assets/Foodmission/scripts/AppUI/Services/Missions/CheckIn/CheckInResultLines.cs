using System;
using System.Linq;

namespace eu.foodmission.platform
{
    /// <summary>One line of the check-in result: a UI.csv key and its format arguments.</summary>
    public sealed class CheckInResultLine
    {
        public string Key { get; }
        public object[] Args { get; }

        public CheckInResultLine(string key, params object[] args)
        {
            Key = key;
            Args = args;
        }
    }

    /// <summary>Concrete progress per mission after a check-in (spec 2026-10-27 swaps §8). Pure.</summary>
    public static class CheckInResultLines
    {
        public const string DoneKey = "MISSION_CHECKIN_LINE_DONE";
        public const string FailedKey = "MISSION_CHECKIN_LINE_FAILED";
        public const string SwapsKey = "MISSION_CHECKIN_LINE_SWAPS";
        public const string LevelsKey = "MISSION_CHECKIN_LINE_LEVELS";
        public const string DaysKey = "MISSION_CHECKIN_LINE_DAYS";
        public const string PercentKey = "MISSION_CHECKIN_LINE_PERCENT";

        /// <summary>"3 of 4 swaps" when the rule counts something the user knows; a percentage otherwise. Null without progress.</summary>
        public static CheckInResultLine For(string title, MissionInteraction interaction, MissionProgress progress)
        {
            if (progress == null)
            {
                return null;
            }
            if (MissionProgressState.IsCompleted(progress))
            {
                return new CheckInResultLine(DoneKey, title);
            }
            if (MissionProgressState.IsFailed(progress))
            {
                return new CheckInResultLine(FailedKey, title);
            }

            float done = Math.Clamp(progress.progress, 0f, 100f) / 100f;
            MissionReportStep step = interaction?.Steps.Count == 1 ? interaction.Steps[0] : null;
            if (step?.Type == MissionStepType.DaySwaps && step.DaySwaps != null)
            {
                int target = step.DaySwaps.Target;
                return new CheckInResultLine(step.DaySwaps.IsLevels ? LevelsKey : SwapsKey, title, Count(done, target), target);
            }
            if (step?.Type == MissionStepType.DayQuestion && step.DayQuestion != null &&
                step.DayQuestion.Coverage == DayCoverage.FlagDay && !step.DayQuestion.HasLimit)
            {
                return new CheckInResultLine(DaysKey, title, Count(done, step.MaxCount), step.MaxCount);
            }
            return new CheckInResultLine(PercentKey, title, (int)Math.Round(progress.progress, MidpointRounding.AwayFromZero));
        }

        private static int Count(float done, int target) => (int)Math.Round(done * target, MidpointRounding.AwayFromZero);
    }
}
