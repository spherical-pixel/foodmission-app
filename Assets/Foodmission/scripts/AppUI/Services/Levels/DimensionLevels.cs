using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using UnityEngine;

namespace eu.foodmission.platform
{
    /// <summary>Reads, refreshes and sanitizes the user's per-dimension levels (owned by the backend).</summary>
    public static class DimensionLevels
    {
        /// <summary>Stored level for the dimension, else BEGINNER. Never null.</summary>
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

            return ContentLevel.Beginner;
        }

        /// <summary>The effective level of every dimension, in DimensionCode.All order.</summary>
        public static DimensionLevelEntry[] Resolve(AppState state)
        {
            return DimensionCode.All.Select(code => new DimensionLevelEntry(code, GetLevel(state, code))).ToArray();
        }

        /// <summary>Known dimensions with valid levels, in DimensionCode.All order (the last entry wins on duplicates).</summary>
        public static DimensionLevelEntry[] Sanitize(IEnumerable<DimensionLevelEntry> entries)
        {
            var byCode = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (DimensionLevelEntry entry in entries ?? Enumerable.Empty<DimensionLevelEntry>())
            {
                if (entry != null && !string.IsNullOrEmpty(entry.dimensionCode) && ContentLevel.IsValid(entry.level))
                {
                    byCode[entry.dimensionCode] = ContentLevel.Normalize(entry.level);
                }
            }

            return DimensionCode.All
                .Where(code => byCode.ContainsKey(code))
                .Select(code => new DimensionLevelEntry(code, byCode[code]))
                .ToArray();
        }

        /// <summary>
        /// Reloads the levels from the backend into AppState. On error (or without a service) the stored levels are kept,
        /// so a failed request never resets the user to BEGINNER.
        /// </summary>
        public static async Task<bool> RefreshAsync(IGamificationService gamification, IStoreService store)
        {
            if (gamification == null || store?.store == null)
            {
                return false;
            }

            try
            {
                var (levels, error) = await gamification.GetDimensionLevelsAsync();
                if (error != null || levels == null)
                {
                    Debug.LogWarning($"[DimensionLevels] Could not refresh levels: {error?.statusCode} {error?.message}");
                    return false;
                }

                store.store.Dispatch(AppActions.setDimensionLevels.Invoke(Sanitize(levels)));
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DimensionLevels] RefreshAsync failed: {ex.Message}");
                return false;
            }
        }
    }
}
