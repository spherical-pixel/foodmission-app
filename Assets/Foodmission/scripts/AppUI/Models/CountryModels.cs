using System;
using System.Collections.Generic;

namespace eu.foodmission.platform
{
    [Serializable]
    public class CountryData
    {
        public string country_iso;
        public string country_name_local;
        public string flag;
        public List<RegionData> regions;
    }

    [Serializable]
    public class RegionData
    {
        public string region_iso;
        public string region_name_local;
    }

    [Serializable]
    public class CountriesList
    {
        public List<CountryData> countries;
    }

    /// <summary>
    /// Utility for working with ISO 3166-1 alpha-2 country codes.
    /// </summary>
    public static class CountryUtils
    {
        /// <summary>
        /// Converts an ISO 3166-1 alpha-2 country code to its flag emoji
        /// using regional indicator symbols. "ES" → 🇪🇸, "AT" → 🇦🇹.
        /// Returns empty string for invalid input.
        /// </summary>
        public static string CountryCodeToFlag(string alpha2)
        {
            if (string.IsNullOrEmpty(alpha2) || alpha2.Length != 2) return "";
            char c1 = alpha2[0];
            char c2 = alpha2[1];
            if (c1 < 'A' || c1 > 'Z' || c2 < 'A' || c2 > 'Z') return "";
            return char.ConvertFromUtf32(0x1F1E6 + (c1 - 'A')) + char.ConvertFromUtf32(0x1F1E6 + (c2 - 'A'));
        }
    }
}
