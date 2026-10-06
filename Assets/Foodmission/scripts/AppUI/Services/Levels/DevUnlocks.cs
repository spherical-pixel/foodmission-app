namespace eu.foodmission.platform
{
    /// <summary>
    /// Development switch: when true nothing is locked (quest order and levels).
    /// Defaults to whether the FM_UNLOCK_ALL_QUESTS scripting define is set. Remove the define before building for the pilot.
    /// </summary>
    public static class DevUnlocks
    {
        public static bool All { get; set; } =
#if FM_UNLOCK_ALL_QUESTS
            true;
#else
            false;
#endif
    }
}
