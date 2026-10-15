using System;

namespace eu.foodmission.platform
{
    /// <summary>
    /// Numbers a mission asks for when it starts or restarts (backend rule `inputs`). Today only M.B1.3: the meals
    /// with meat of last week, which its rule reduces by one (0 = keep a meat-free week). Pure.
    /// </summary>
    public static class MissionInputs
    {
        /// <summary>Backend bounds of previousMeatMeals (3 meals a day).</summary>
        public const int MaxPreviousMeatMeals = 21;

        private const string ReduceMeatMission = "M.B1.3";

        public static bool AsksPreviousMeatMeals(string missionCode)
        {
            return string.Equals(missionCode?.Trim(), ReduceMeatMission, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>The value to send: only for the missions that ask (the backend rejects inputs on the others).</summary>
        public static int? For(string missionCode, int? previousMeatMeals)
        {
            return AsksPreviousMeatMeals(missionCode) ? previousMeatMeals : null;
        }

        /// <summary>No meat last week: the goal becomes a meat-free week, and Nutri says so.</summary>
        public static bool IsMeatFreeGoal(string missionCode, int? previousMeatMeals)
        {
            return AsksPreviousMeatMeals(missionCode) && previousMeatMeals == 0;
        }

        /// <summary>
        /// Whether the attempt the backend returned has the meat-free goal. Read from what it stored: it ignores inputs
        /// on an already started mission, and older backends store none.
        /// </summary>
        public static bool SetsMeatFreeGoal(MissionProgress progress)
        {
            return progress?.inputs != null &&
                   progress.inputs.TryGetValue(MissionInputsRequest.PreviousMeatMealsKey, out int count) &&
                   count == 0;
        }
    }
}
