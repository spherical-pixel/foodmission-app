using System;

namespace eu.foodmission.platform
{
    /// <summary>The user's level in one dimension. An array instead of a dictionary because AppState uses JsonUtility.</summary>
    [Serializable]
    public class DimensionLevelEntry
    {
        public string dimensionCode;
        public string level;

        public DimensionLevelEntry()
        {
        }

        public DimensionLevelEntry(string dimensionCode, string level)
        {
            this.dimensionCode = dimensionCode;
            this.level = level;
        }

        public DimensionLevelEntry Copy() => new DimensionLevelEntry(dimensionCode, level);
    }
}
