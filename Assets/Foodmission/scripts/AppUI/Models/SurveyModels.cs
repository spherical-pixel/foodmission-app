using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace eu.foodmission.platform
{
    // ==================== Survey DTOs ====================

    [Serializable]
    public class AnswerOptionDto
    {
        [JsonProperty("value")]
        public int value;

        [JsonProperty("label")]
        public string label;
    }

    [Serializable]
    public class QuestionDto
    {
        [JsonProperty("id")]
        public string id;

        [JsonProperty("key")]
        public string key;

        [JsonProperty("text")]
        public string text;

        [JsonProperty("type")]
        public string type;

        [JsonProperty("order")]
        public int order;

        [JsonProperty("surveyId")]
        public string surveyId;

        [JsonProperty("createdAt")]
        public string createdAt;

        [JsonProperty("updatedAt")]
        public string updatedAt;

        [JsonProperty("answers")]
        public AnswerOptionDto[] answers;
    }

    [Serializable]
    public class SurveyDto
    {
        [JsonProperty("id")]
        public string id;

        [JsonProperty("slug")]
        public string slug;

        [JsonProperty("title")]
        public string title;

        [JsonProperty("description")]
        public string description;

        [JsonProperty("createdAt")]
        public string createdAt;

        [JsonProperty("updatedAt")]
        public string updatedAt;

        [JsonProperty("questions")]
        public QuestionDto[] questions;
    }

    // ==================== Submit Response Request DTOs ====================

    [Serializable]
    public class SubmitQuestionResponseDto
    {
        [JsonProperty("questionId")]
        public string questionId;

        [JsonProperty("value")]
        public int value;
    }

    [Serializable]
    public class SubmitSurveyResponseDto
    {
        [JsonProperty("responses")]
        public SubmitQuestionResponseDto[] responses;

        public string ToJson() => JsonConvert.SerializeObject(this, new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore
        });
    }

    // ==================== Survey Response DTOs ====================

    [Serializable]
    public class QuestionResponseDto
    {
        [JsonProperty("id")]
        public string id;

        [JsonProperty("questionId")]
        public string questionId;

        [JsonProperty("value")]
        public int value;

        [JsonProperty("question")]
        public QuestionDto question;
    }

    [Serializable]
    public class SurveyResponseDto
    {
        [JsonProperty("id")]
        public string id;

        [JsonProperty("userId")]
        public string userId;

        [JsonProperty("surveyId")]
        public string surveyId;

        [JsonProperty("attemptNumber")]
        public int attemptNumber;

        [JsonProperty("responses")]
        public QuestionResponseDto[] responses;

        [JsonProperty("createdAt")]
        public string createdAt;

        [JsonProperty("updatedAt")]
        public string updatedAt;
    }

    // ==================== Pilot Survey Cycle State ====================

    [Serializable]
    public class PilotSurveyCycleState
    {
        public int currentCycle = 1;
        public string cycleStartDate = ""; // "YYYY-MM-DD"
        public List<string> activeDatesInCycle = new List<string>(); // ["2026-09-01", ...]
        public List<string> completedSlugsInCycle = new List<string>();
        public List<string> skippedSlugsInCycle = new List<string>();

        public PilotSurveyCycleState Copy()
        {
            return new PilotSurveyCycleState
            {
                currentCycle = this.currentCycle,
                cycleStartDate = this.cycleStartDate,
                activeDatesInCycle = this.activeDatesInCycle != null ? new List<string>(this.activeDatesInCycle) : new List<string>(),
                completedSlugsInCycle = this.completedSlugsInCycle != null ? new List<string>(this.completedSlugsInCycle) : new List<string>(),
                skippedSlugsInCycle = this.skippedSlugsInCycle != null ? new List<string>(this.skippedSlugsInCycle) : new List<string>()
            };
        }
    }

    // ==================== Pilot Survey Scheduling Rule ====================

    public class PilotSurveyRule
    {
        public string Slug { get; set; }
        public int MinActiveDaysInCycle { get; set; }
        public int MinDaysSinceCycleStart { get; set; }
        public bool IsEndSurvey { get; set; }

        public PilotSurveyRule(string slug, int minActiveDaysInCycle, int minDaysSinceCycleStart = 0, bool isEndSurvey = false)
        {
            Slug = slug;
            MinActiveDaysInCycle = minActiveDaysInCycle;
            MinDaysSinceCycleStart = minDaysSinceCycleStart;
            IsEndSurvey = isEndSurvey;
        }
    }
}
