namespace eu.foodmission.platform
{
    /// <summary>
    /// Constants for notification channel IDs and configuration in Android/iOS.
    /// </summary>
    public static class NotificationChannels
    {
        public const string PantryExpiryId = "foodmission_pantry";
        public const string PantryExpiryName = "Despensa y Caducidades";
        public const string PantryExpiryDescription = "Avisos sobre alimentos próximos a caducar en tu despensa.";

        public const string DailyRemindersId = "foodmission_reminders";
        public const string DailyRemindersName = "Recordatorios Diarios";
        public const string DailyRemindersDescription = "Recordatorios para registrar tus comidas y hábitos diarios.";

        public const string GamificationId = "foodmission_gamification";
        public const string GamificationName = "Logros y Desafíos";
        public const string GamificationDescription = "Notificaciones sobre medallas, retos de grupo y misiones.";
    }

    /// <summary>
    /// Payload structure for deep-link navigation and notification handling.
    /// </summary>
    public class NotificationPayload
    {
        public string Id { get; set; } = "";
        public string Action { get; set; } = "";
        public string TargetId { get; set; } = "";
        public string Title { get; set; } = "";
        public string Body { get; set; } = "";
        public string ChannelId { get; set; } = "";
    }
}
