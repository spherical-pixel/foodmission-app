namespace eu.foodmission.platform
{
    /// <summary>
    /// Counts writes that can change the user's progress (meal logs, client events, mission progress requests).
    /// Services that briefly reuse progress reads compare <see cref="Version"/> so a read after a write is always fresh.
    /// </summary>
    public static class UserProgressWrites
    {
        public static long Version { get; private set; }

        public static void Record()
        {
            Version++;
        }
    }
}
