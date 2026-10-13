using System;
using System.Collections.Generic;
using System.Linq;

namespace eu.foodmission.platform
{
    /// <summary>
    /// Content below the user's level is hidden in each dimension of the lists, behind a "show previous levels" row.
    /// Only with the "All" level filter; the dimensions showing them are remembered while the screen lives.
    /// </summary>
    public static class PreviousLevels
    {
        /// <summary>The items to list: without the previous-level ones unless shown. Counts them either way (the row stays).</summary>
        public static List<T> Visible<T>(IEnumerable<T> items, Func<T, bool> isPrevious, bool showPrevious, out int previousCount)
        {
            List<T> all = items?.ToList() ?? new List<T>();
            previousCount = all.Count(isPrevious);
            return showPrevious ? all : all.Where(item => !isPrevious(item)).ToList();
        }

        /// <summary>Flips the dimension in the set; returns whether it now shows its previous levels.</summary>
        public static bool Toggle(HashSet<string> shownDimensions, string dimensionCodeOrId)
        {
            if (shownDimensions == null || string.IsNullOrEmpty(dimensionCodeOrId))
            {
                return false;
            }
            if (shownDimensions.Remove(dimensionCodeOrId))
            {
                return false;
            }
            shownDimensions.Add(dimensionCodeOrId);
            return true;
        }

        public static bool IsShown(HashSet<string> shownDimensions, Dimension dimension)
        {
            if (shownDimensions == null || dimension == null)
            {
                return false;
            }
            return (!string.IsNullOrEmpty(dimension.code) && shownDimensions.Contains(dimension.code))
                || (!string.IsNullOrEmpty(dimension.id) && shownDimensions.Contains(dimension.id));
        }
    }
}
