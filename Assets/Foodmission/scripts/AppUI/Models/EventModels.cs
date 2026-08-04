using System;
using System.Text;
using UnityEngine;

namespace eu.foodmission.platform
{
    public static class ClientEventTypes
    {
        public const string AppSessionOpened = "APP_SESSION_OPENED";
        public const string AppSessionEnded = "APP_SESSION_ENDED";

        public static readonly string[] All = { AppSessionOpened, AppSessionEnded };
    }

    [Serializable]
    public class ClientEventMetadata
    {
        public string sessionId;
        public string platform;
        public string appVersion;
        public int durationSeconds;
    }

    [Serializable]
    public class CreateClientEventRequest
    {
        public string eventType;
        public ClientEventMetadata metadata;

        public byte[] ToJsonBody()
        {
            string json = JsonUtility.ToJson(this);
            return Encoding.UTF8.GetBytes(json);
        }
    }

    [Serializable]
    public class UserEvent
    {
        public string id;
        public string userId;
        public string eventType;
        public string source;
        public string timestamp;
        public string groupId;
    }
}
