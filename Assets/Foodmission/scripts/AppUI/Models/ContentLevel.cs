namespace eu.foodmission.platform
{
    /// <summary>Levels shared by content (quests, missions, challenges, quizzes, food facts) and per-dimension user levels.</summary>
    public static class ContentLevel
    {
        public const string Beginner = "BEGINNER";
        public const string Intermediate = "INTERMEDIATE";
        public const string Advanced = "ADVANCED";

        public static readonly string[] All = { Beginner, Intermediate, Advanced };

        /// <summary>0 = BEGINNER, 1 = INTERMEDIATE, 2 = ADVANCED; -1 when empty or unknown.</summary>
        public static int Rank(string level)
        {
            if (string.IsNullOrEmpty(level))
            {
                return -1;
            }

            switch (level.Trim().ToUpperInvariant())
            {
                case Beginner:
                    return 0;
                case Intermediate:
                    return 1;
                case Advanced:
                    return 2;
                default:
                    return -1;
            }
        }

        public static bool IsValid(string level) => Rank(level) >= 0;

        /// <summary>Upper-case level, or null when the value is not a known level.</summary>
        public static string Normalize(string level) => IsValid(level) ? All[Rank(level)] : null;
    }
}
