using System;
using System.Text;
using Newtonsoft.Json;

namespace eu.foodmission.platform
{
    public static class QuestLevel
    {
        public const string Beginner = "BEGINNER";
        public const string Intermediate = "INTERMEDIATE";
        public const string Advanced = "ADVANCED";

        public static readonly string[] All = { Beginner, Intermediate, Advanced };
    }

    public static class QuestContentType
    {
        public const string Mission = "MISSION";
        public const string Quiz = "QUIZ";
        public const string FoodFact = "FOOD_FACT";
        public const string MicroLearning = "MICRO_LEARNING";
        public const string Challenge = "CHALLENGE";
    }

    [Serializable]
    public class QuestItem
    {
        public string id;
        public string contentType;
        public string contentCode;
        public string label;
        public int sortOrder;
    }

    [Serializable]
    public class Quest
    {
        public string id;
        public string code;
        public string dimensionId;
        public string level;
        public string name;
        public string title;
        public string description;
        public bool available;
        public QuestItem[] items;
    }

    [Serializable]
    public class QuestProgress
    {
        public string id;
        public string userId;
        public string questId;
        public string questCode;
        public string questTitle;
        public bool completed;
        public string completedAt;
        public string unlockedAt;

        [JsonProperty("progress")]
        public float progress;

        [JsonProperty("progressPercent", NullValueHandling = NullValueHandling.Ignore)]
        private float? _progressPercentFallback
        {
            set
            {
                if (value.HasValue && progress == 0)
                {
                    progress = value.Value;
                }
            }
        }

        [JsonIgnore]
        public float progressPercent
        {
            get => progress;
            set => progress = value;
        }
    }

    public class QuestFilterParams
    {
        public string dimensionCode;
        public string level;
        public string lang;
    }

    [Serializable]
    public class UpdateQuestProgressRequest
    {
        [JsonProperty("completed", NullValueHandling = NullValueHandling.Ignore)]
        public bool? completed;

        [JsonProperty("progress", NullValueHandling = NullValueHandling.Ignore)]
        public float? progress;

        [JsonIgnore]
        public float? progressPercent
        {
            get => progress;
            set => progress = value;
        }

        public byte[] ToJsonBody()
        {
            string json = JsonConvert.SerializeObject(this, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });
            return Encoding.UTF8.GetBytes(json);
        }
    }
}

