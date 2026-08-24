using System;
using System.Threading.Tasks;

namespace eu.foodmission.platform
{
    /// <summary>
    /// Contract for unified local scheduling and push notification management.
    /// </summary>
    public interface INotificationService
    {
        /// <summary>
        /// Initializes notification channels, listeners and restores push registration state.
        /// </summary>
        Task InitializeAsync();

        /// <summary>
        /// Requests notification authorization from the OS (iOS & Android 13+).
        /// </summary>
        Task<bool> RequestPermissionsAsync();

        /// <summary>
        /// Checks if notifications are currently enabled in AppState / OS.
        /// </summary>
        bool AreNotificationsEnabled();

        /// <summary>
        /// Checks whether the user should be prompted with a NutriMessageDialog on Home screen.
        /// </summary>
        bool ShouldPromptForNotifications();

        /// <summary>
        /// User accepted notification prompt: requests OS permissions and enables notifications.
        /// </summary>
        Task<bool> AcceptNotificationsAsync();

        /// <summary>
        /// User declined notification prompt: disables notifications, clears device token and syncs.
        /// </summary>
        void DeclineNotifications();

        /// <summary>
        /// Updates notifications enabled state in Redux and local storage.
        /// </summary>
        void SetNotificationsEnabled(bool enabled);

        /// <summary>
        /// Schedules local notification reminder(s) before a pantry item expires (e.g. 48h / 24h before).
        /// </summary>
        void SchedulePantryExpiryReminder(string itemId, string itemName, DateTime expiryDate);

        /// <summary>
        /// Cancels scheduled local notification reminder for a specific pantry item (e.g. when deleted or consumed).
        /// </summary>
        void CancelPantryReminder(string itemId);

        /// <summary>
        /// Schedules a custom local notification for a specific DateTime.
        /// </summary>
        void ScheduleLocalNotification(string id, string title, string body, DateTime deliveryTime, string channelId = null, string action = null, string targetId = null);

        /// <summary>
        /// Schedules a custom local notification with a delay in seconds (ideal for quick testing/timers).
        /// </summary>
        void ScheduleLocalNotificationInSeconds(string id, string title, string body, int secondsDelay, string channelId = null, string action = null, string targetId = null);

        /// <summary>
        /// Schedules recurring daily meal logging reminder according to user preferred time.
        /// </summary>
        void ScheduleDailyMealReminder(TimeSpan preferredTime);

        /// <summary>
        /// Reschedules daily meal reminders and active pantry expiry notifications according to the preferred time.
        /// </summary>
        void RescheduleAllNotifications(TimeSpan preferredTime);

        /// <summary>
        /// Cancels a specific scheduled local notification by ID.
        /// </summary>
        void CancelNotification(string notificationId);

        /// <summary>
        /// Cancels all scheduled local notifications.
        /// </summary>
        void CancelAllNotifications();

        /// <summary>
        /// Retrieves the current device push registration info.
        /// </summary>
        DevicePushRegistration GetDevicePushRegistration();

        /// <summary>
        /// Event fired when a notification is opened/tapped by the user.
        /// </summary>
        event Action<NotificationPayload> OnNotificationOpened;

        /// <summary>
        /// Event fired when a new remote/local notification is received.
        /// </summary>
        event Action<NotificationModel> OnNotificationReceived;
    }
}
