using System;
using System.Collections.Generic;
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

    public static class ProgressStatus
    {
        public const string NotStarted = "NOT_STARTED";
        public const string InProgress = "IN_PROGRESS";
        public const string Completed = "COMPLETED";
        public const string Failed = "FAILED";
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
        /// <summary>NOT_STARTED, IN_PROGRESS, COMPLETED or FAILED (see <see cref="ProgressStatus"/>). Null on backends before pr-402.</summary>
        public string status;
        public string missionTitle;
        public ContentReward reward;
        /// <summary>When the current attempt started (set when the quest is selected, or at a restart). Null on rows created before pr-402.</summary>
        public DateTime? startedAt;
        /// <summary>Numbers the user set when starting (backend #424), e.g. previousMeatMeals for M.B1.3; null if none.</summary>
        public Dictionary<string, int> inputs;
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

        [JsonProperty("failed", NullValueHandling = NullValueHandling.Ignore)]
        public bool? failed;

        public byte[] ToJsonBody()
        {
            string json = JsonConvert.SerializeObject(this, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });
            return Encoding.UTF8.GetBytes(json);
        }
    }

    /// <summary>
    /// Body of POST …/progress/start and …/progress/restart for missions whose rule asks for numbers
    /// (only M.B1.3: previousMeatMeals, 0–21). Other missions take no body: the backend rejects inputs there.
    /// </summary>
    public class MissionInputsRequest
    {
        public const string PreviousMeatMealsKey = "previousMeatMeals";

        [JsonProperty("inputs")]
        public Dictionary<string, int> inputs;

        public static MissionInputsRequest PreviousMeatMeals(int count)
        {
            return new MissionInputsRequest { inputs = new Dictionary<string, int> { { PreviousMeatMealsKey, count } } };
        }

        public byte[] ToJsonBody()
        {
            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(this));
        }
    }
}
