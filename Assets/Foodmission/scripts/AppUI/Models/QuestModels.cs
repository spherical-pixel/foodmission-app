using System;
using System.Text;
using Newtonsoft.Json;

namespace eu.foodmission.platform
{
    public static class QuestContentType
    {
        public const string Mission = "MISSION";
        public const string Quiz = "QUIZ";
        public const string FoodFact = "FOOD_FACT";
        public const string MicroLearning = "MICRO_LEARNING";
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
        public bool completed;
        public string completedAt;
        public float progressPercent;
    }

    [Serializable]
    public class UpdateQuestProgressRequest
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public bool? completed;

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public float? progressPercent;

        public byte[] ToJsonBody()
        {
            string json = JsonConvert.SerializeObject(this);
            return Encoding.UTF8.GetBytes(json);
        }
    }
}
