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
}
