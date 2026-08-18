using System;
using System.Text;
using Newtonsoft.Json;

namespace eu.foodmission.platform
{
    public static class QuizLevel
    {
        public const string Beginner = "BEGINNER";
        public const string Intermediate = "INTERMEDIATE";
        public const string Advanced = "ADVANCED";

        public static readonly string[] All = { Beginner, Intermediate, Advanced };
    }

    public static class QuizOptionLabel
    {
        public const string A = "A";
        public const string B = "B";
        public const string C = "C";
        public const string D = "D";

        public static readonly string[] All = { A, B, C, D };
    }

    [Serializable]
    public class QuizOption
    {
        public string id;
        public string label;
        public string text;
        public int sortOrder;
    }

    [Serializable]
    public class Quiz
    {
        public string id;
        public string code;
        public string topicId;
        public string question;
        public string explanation;
        public string source;
        public string level;
        public bool health;
        public bool foodChoice;
        public bool foodWaste;
        public bool available;
        public QuizOption[] options;
    }

    [Serializable]
    public class QuizProgress
    {
        public string id;
        public string userId;
        public string quizId;
        public string quizCode;
        public string question;
        public string selectedOptionId;
        public bool? isCorrect;
        public bool completed;
        public string answeredAt;
    }

    [Serializable]
    public class PaginationMeta
    {
        public int page;
        public int limit;
        public int total;
        public int totalPages;
        public bool hasNext;
        public bool hasPrevious;
    }

    [Serializable]
    public class PaginatedQuizResponse
    {
        public Quiz[] data;
        public PaginationMeta meta;
    }

    public class QuizFilterParams
    {
        public string dimensionCode;
        public string topicCode;
        public string level;
        public bool? health;
        public bool? foodChoice;
        public bool? foodWaste;
        public string search;
    }

    public class UpdateQuizProgressRequest
    {
        [JsonProperty("selectedLabel")]
        public string selectedLabel;

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
