using System;
using System.Collections.Generic;

namespace eu.foodmission.platform
{
    [Serializable]
    public class LocalizationOverrides
    {
        public int version;
        public string minAppVersion;
        public string generated;
        public Dictionary<string, Dictionary<string, Dictionary<string, string>>> strings;
    }
}
