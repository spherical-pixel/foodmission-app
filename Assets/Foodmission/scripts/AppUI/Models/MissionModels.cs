using System;
using System.Text;
using Newtonsoft.Json;

namespace eu.foodmission.platform
{
    public static class MissionLevel
    {
        public const string Beginner = "BEGINNER";
        public const string Intermediate = "INTERMEDIATE";
        public const string Advanced = "ADVANCED";

        public static readonly string[] All = { Beginner, Intermediate, Advanced };
    }

    [Serializable]
    public class Mission
    {
        public string id;
        public string code;
        public string dimensionId;
        public string topicId;
        public string level;
        public string title;
        public string duration;
        public string goal;
        public string whyItMatters;
        public bool health;
        public bool foodChoice;
        public bool foodWaste;
        public bool available;
        public float? progress;
    }

    [Serializable]
    public class MissionProgress
    {
        public string missionId;
        public string missionCode;
        public string userId;
        public float progress;
        public bool completed;
        public string missionTitle;
    }

    public class MissionFilterParams
    {
        public string dimensionCode;
        public string level;
        public bool? available;
        public string lang;
    }

    public class UpdateMissionProgressRequest
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
