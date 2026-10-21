using System.Collections.Generic;
using System.Linq;

namespace eu.foodmission.platform
{
    /// <summary>
    /// Annual income dropdown shared by OnboardingProfile and EditProfile.
    /// The catalog ranges are followed by a local "Prefer not to say" option (index = catalog length).
    /// "Prefer not to say" is saved as null (clears the stored income), so it is shown again as an empty selection.
    /// </summary>
    public static class AnnualIncomeSelection
    {
        public const string PreferNotToSayLabelKey = "ONBOARDING_PROFILE.OPTION_INCOME_PREFER_NOT_TO_SAY";

        public static List<string> BuildOptions(CatalogItem[] catalog, string preferNotToSayLabel)
        {
            var options = catalog?.Select(i => i.label).ToList() ?? new List<string>();
            if (options.Count > 0)
            {
                options.Add(preferNotToSayLabel ?? "");
            }
            return options;
        }

        public static int PreferNotToSayIndex(CatalogItem[] catalog)
        {
            return catalog?.Length ?? 0;
        }

        public static bool IsPreferNotToSay(int index, CatalogItem[] catalog)
        {
            return catalog != null && catalog.Length > 0 && index == catalog.Length;
        }

        /// <summary>
        /// Catalog code for the selected index; null for no selection, "Prefer not to say" or an out-of-range index.
        /// </summary>
        public static string ToCode(int index, CatalogItem[] catalog)
        {
            if (catalog == null || index < 0 || index >= catalog.Length)
            {
                return null;
            }
            return catalog[index].code;
        }
    }
}
