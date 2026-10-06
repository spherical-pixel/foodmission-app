using System;
using System.Collections.Generic;
using System.Linq;

namespace eu.foodmission.platform
{
    /// <summary>Reads, proposes and converts the user's per-dimension levels.</summary>
    public static class DimensionLevels
    {
        /// <summary>Stored level for the dimension, else the global segment, else BEGINNER. Never null.</summary>
        public static string GetLevel(AppState state, string dimensionCode)
        {
            if (state?.dimensionLevels != null && !string.IsNullOrEmpty(dimensionCode))
            {
                foreach (DimensionLevelEntry entry in state.dimensionLevels)
                {
                    if (entry != null
                        && string.Equals(entry.dimensionCode, dimensionCode, StringComparison.OrdinalIgnoreCase)
                        && ContentLevel.IsValid(entry.level))
                    {
                        return ContentLevel.Normalize(entry.level);
                    }
                }
            }

            return ContentLevel.Normalize(state?.userSegment) ?? ContentLevel.Beginner;
        }

        /// <summary>The effective level of every dimension, in DimensionCode.All order.</summary>
        public static DimensionLevelEntry[] Resolve(AppState state)
        {
            return DimensionCode.All.Select(code => new DimensionLevelEntry(code, GetLevel(state, code))).ToArray();
        }

        /// <summary>
        /// Proposal shown after the onboarding survey. Today the backend only scores one global segment, so every
        /// dimension gets it; when the backend returns one level per dimension, only this method changes.
        /// </summary>
        public static DimensionLevelEntry[] Propose(string segment)
        {
            string level = ContentLevel.Normalize(segment) ?? ContentLevel.Beginner;
            return DimensionCode.All.Select(code => new DimensionLevelEntry(code, level)).ToArray();
        }

        /// <summary>Entries from the backend map, keeping only known dimensions with valid levels.</summary>
        public static DimensionLevelEntry[] FromMap(IDictionary<string, string> map)
        {
            if (map == null)
            {
                return new DimensionLevelEntry[0];
            }

            var entries = new List<DimensionLevelEntry>();
            foreach (string code in DimensionCode.All)
            {
                foreach (KeyValuePair<string, string> pair in map)
                {
                    if (string.Equals(pair.Key, code, StringComparison.OrdinalIgnoreCase) && ContentLevel.IsValid(pair.Value))
                    {
                        entries.Add(new DimensionLevelEntry(code, ContentLevel.Normalize(pair.Value)));
                        break;
                    }
                }
            }
            return entries.ToArray();
        }

        public static Dictionary<string, string> ToMap(IEnumerable<DimensionLevelEntry> entries)
        {
            var map = new Dictionary<string, string>();
            if (entries == null)
            {
                return map;
            }

            foreach (DimensionLevelEntry entry in entries)
            {
                if (entry != null && !string.IsNullOrEmpty(entry.dimensionCode) && ContentLevel.IsValid(entry.level))
                {
                    map[entry.dimensionCode] = ContentLevel.Normalize(entry.level);
                }
            }
            return map;
        }
    }
}
