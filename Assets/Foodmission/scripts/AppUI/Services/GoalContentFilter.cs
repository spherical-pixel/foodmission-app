using System;
using System.Collections.Generic;

using Unity.AppUI.Navigation;

namespace eu.foodmission.platform
{
    /// <summary>
    /// Hides learning content (quests, missions, challenges, quizzes, food facts) from dimensions the user did not
    /// pick in their goals. A dimension is wanted when at least one of its topics is in <c>userGoals</c>.
    /// Content the user already started or completed is always shown, and so is content whose dimension is unknown.
    /// No goals stored (legacy users) means no filtering. Create one per list rebuild: it counts what it hid.
    /// </summary>
    public sealed class GoalContentFilter
    {
        private readonly HashSet<string> _wantedDimensionCodes;
        private readonly HashSet<string> _hiddenDimensionCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private GoalContentFilter(HashSet<string> wantedDimensionCodes)
        {
            _wantedDimensionCodes = wantedDimensionCodes;
        }

        public static GoalContentFilter FromGoals(string[] userGoals)
        {
            if (userGoals == null || userGoals.Length == 0)
            {
                return new GoalContentFilter(null);
            }

            var wanted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (GoalDimensionDefinition dimension in DimensionGoalsCatalog.Dimensions)
            {
                foreach (GoalTopicDefinition topic in dimension.Topics)
                {
                    if (Array.Exists(userGoals, g => string.Equals(g, topic.Code, StringComparison.OrdinalIgnoreCase)))
                    {
                        wanted.Add(dimension.DimensionCode);
                        break;
                    }
                }
            }

            // Goals that match no known topic: don't hide anything rather than everything
            return new GoalContentFilter(wanted.Count > 0 ? wanted : null);
        }

        public bool ShowsAll => _wantedDimensionCodes == null;

        /// <summary>True once <see cref="ShouldShow"/> has hidden at least one item.</summary>
        public bool HasHiddenContent => _hiddenDimensionCodes.Count > 0;

        public int HiddenDimensionCount => _hiddenDimensionCodes.Count;

        public bool IsDimensionWanted(string dimensionCode)
        {
            return ShowsAll || string.IsNullOrEmpty(dimensionCode) || _wantedDimensionCodes.Contains(dimensionCode);
        }

        /// <summary>
        /// Whether an item of <paramref name="dimension"/> is listed. Hidden items are remembered for <see cref="HasHiddenContent"/>.
        /// </summary>
        public bool ShouldShow(Dimension dimension, bool startedOrCompleted)
        {
            if (startedOrCompleted || dimension == null || IsDimensionWanted(dimension.code))
            {
                return true;
            }

            _hiddenDimensionCodes.Add(dimension.code);
            return false;
        }

        /// <summary>
        /// Arguments for <c>go_to_onboarding_goals</c> from a content list: after saving, the goals flow
        /// navigates to <paramref name="returnAction"/> so the user lands on the refreshed list.
        /// </summary>
        public static Argument[] EditGoalsArguments(string returnAction)
        {
            return new[]
            {
                new Argument("fromEditProfile", "false"),
                new Argument("returnTo", returnAction)
            };
        }
    }
}
