using System;

namespace eu.foodmission.platform
{
    public enum NotificationType
    {
        Social,
        Badge,
        System
    }

    [Serializable]
    public class NotificationModel
    {
        public string Id;
        public string Text;
        public string Timestamp;
        public NotificationType Type;
        public bool IsRead;
    }
}
