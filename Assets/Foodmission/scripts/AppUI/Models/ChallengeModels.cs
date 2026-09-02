using System;
using System.Text;
using Newtonsoft.Json;

namespace eu.foodmission.platform
{
    public static class ChallengeLevel
    {
        public const string Beginner = "BEGINNER";
        public const string Intermediate = "INTERMEDIATE";
        public const string Advanced = "ADVANCED";

        public static readonly string[] All = { Beginner, Intermediate, Advanced };
    }

    [Serializable]
    public class Challenge
    {
        public string id;
        public string code;
        public string dimensionId;
        public string topicId;
        public string level;
        public string title;
        public string task;
        public string whyItMatters;
        public string[] tags;
        public bool health;
        public bool foodChoice;
        public bool foodWaste;
        public bool available;
        public float? progress;
    }

    [Serializable]
    public class ChallengeProgress
    {
        public string challengeId;
        public string userId;
        public float progress;
        public bool completed;
        public string challengeTitle;
    }

    public class ChallengeFilterParams
    {
        public string dimensionCode;
        public string level;
        public bool? available;
        public string lang;
    }

    public class UpdateChallengeProgressRequest
    {
        [JsonProperty("progress", NullValueHandling = NullValueHandling.Ignore)]
        public float? progress;

        [JsonProperty("completed", NullValueHandling = NullValueHandling.Ignore)]
        public bool? completed;

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
