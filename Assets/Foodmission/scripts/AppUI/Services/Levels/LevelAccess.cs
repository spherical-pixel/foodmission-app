namespace eu.foodmission.platform
{
    /// <summary>Why a content item can't be opened.</summary>
    public enum ContentLockReason
    {
        None,
        /// <summary>Quests only: the previous quest of the same difficulty is not completed.</summary>
        Sequence,
        /// <summary>The item's level is above the user's level in its dimension.</summary>
        Level
    }

    /// <summary>Details for the "content above your level" dialog.</summary>
    public sealed class LevelLock
    {
        public string ItemLevel;
        public string UserLevel;
        public string DimensionCode;
        public string DimensionName;
    }

    /// <summary>Pure level rule shared by every content list. Unknown values fail open (not locked).</summary>
    public static class LevelAccess
    {
        public static bool IsLockedByLevel(string itemLevel, string userLevel, bool started)
        {
            if (started)
            {
                return false;
            }

            int itemRank = ContentLevel.Rank(itemLevel);
            int userRank = ContentLevel.Rank(userLevel);
            if (itemRank < 0 || userRank < 0)
            {
                return false;
            }
            return itemRank > userRank;
        }
    }
}
