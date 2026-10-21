using System.Collections.Generic;
using System.Linq;

namespace eu.foodmission.platform
{
    /// <summary>
    /// Dietary preferences multi-select shared by OnboardingProfile and EditProfile.
    /// The catalog options are followed by a local "None of the above" option (index = catalog length),
    /// which is exclusive: picking it clears the rest, picking anything else clears it.
    /// "None" is saved as an empty array, so it is shown again as an empty selection.
    /// </summary>
    public static class DietaryPreferenceSelection
    {
        public const string NoneLabelKey = "ONBOARDING_PROFILE.OPTION_DIET_NONE";

        public static List<string> BuildOptions(CatalogItem[] catalog, string noneLabel)
        {
            var options = catalog?.Select(d => d.label).ToList() ?? new List<string>();
            if (options.Count > 0)
            {
                options.Add(noneLabel ?? "");
            }
            return options;
        }

        public static int NoneIndex(CatalogItem[] catalog)
        {
            return catalog?.Length ?? 0;
        }

        /// <summary>
        /// Returns the selection after applying the "None" rule to a change from previous to next.
        /// </summary>
        public static int[] Normalize(IEnumerable<int> previous, IEnumerable<int> next, int noneIndex)
        {
            int[] nextArray = next?.ToArray() ?? new int[0];
            if (!nextArray.Contains(noneIndex))
            {
                return nextArray;
            }

            bool hadNone = previous != null && previous.Contains(noneIndex);
            if (!hadNone)
            {
                return new[] { noneIndex };
            }

            int[] withoutNone = nextArray.Where(i => i != noneIndex).ToArray();
            return withoutNone.Length > 0 ? withoutNone : nextArray;
        }

        /// <summary>
        /// Catalog codes for the selected indices; "None" and out-of-range indices are skipped,
        /// so "None" or an empty selection gives an empty array (clears the preference).
        /// </summary>
        public static string[] ToCodes(IEnumerable<int> indices, CatalogItem[] catalog)
        {
            if (indices == null || catalog == null)
            {
                return new string[0];
            }

            return indices
                .Where(i => i >= 0 && i < catalog.Length)
                .Select(i => catalog[i].code)
                .ToArray();
        }
    }
}
