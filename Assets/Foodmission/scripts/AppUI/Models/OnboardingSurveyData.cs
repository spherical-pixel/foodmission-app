using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace eu.foodmission.platform
{
    [Serializable]
    public class OnboardingSurveyAnswer
    {
        public string field;
        public string value;
    }

    /// <summary>
    /// Onboarding survey answers keyed by the backend field (GET …/onboarding-survey `field`); values are the option codes.
    /// Every answer is optional. An array (not a dictionary) because AppState is persisted with JsonUtility; on the wire it
    /// is a flat object (see OnboardingSurveyDataConverter).
    /// </summary>
    [Serializable]
    [JsonConverter(typeof(OnboardingSurveyDataConverter))]
    public class OnboardingSurveyData
    {
        public OnboardingSurveyAnswer[] answers = new OnboardingSurveyAnswer[0];

        public int Count => Valid().Count();

        public bool HasAnswers() => Count > 0;

        public string Get(string field)
        {
            return Valid().FirstOrDefault(a => a.field == field)?.value;
        }

        /// <summary>Null or empty value removes the answer.</summary>
        public void Set(string field, string value)
        {
            if (string.IsNullOrEmpty(field))
            {
                return;
            }

            List<OnboardingSurveyAnswer> list = Valid().Where(a => a.field != field).ToList();
            if (!string.IsNullOrEmpty(value))
            {
                list.Add(new OnboardingSurveyAnswer { field = field, value = value });
            }
            answers = list.ToArray();
        }

        public Dictionary<string, string> ToMap()
        {
            var map = new Dictionary<string, string>();
            foreach (OnboardingSurveyAnswer answer in Valid())
            {
                map[answer.field] = answer.value;
            }
            return map;
        }

        public static OnboardingSurveyData FromMap(IDictionary<string, string> map)
        {
            var data = new OnboardingSurveyData();
            if (map == null)
            {
                return data;
            }

            foreach (KeyValuePair<string, string> pair in map)
            {
                data.Set(pair.Key, pair.Value);
            }
            return data;
        }

        public static OnboardingSurveyData Of(params (string field, string value)[] answers)
        {
            var data = new OnboardingSurveyData();
            foreach ((string field, string value) in answers ?? new (string, string)[0])
            {
                data.Set(field, value);
            }
            return data;
        }

        private IEnumerable<OnboardingSurveyAnswer> Valid()
        {
            return (answers ?? new OnboardingSurveyAnswer[0])
                .Where(a => a != null && !string.IsNullOrEmpty(a.field) && !string.IsNullOrEmpty(a.value));
        }
    }
}
