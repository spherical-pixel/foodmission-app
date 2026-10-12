using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace eu.foodmission.platform
{
    /// <summary>
    /// OnboardingSurveyData ⇄ flat JSON object `{ "field": "VALUE" }`. Reading keeps only string values (unanswered
    /// columns come back as null), and a non-object becomes empty data, so a malformed value never breaks the profile.
    /// </summary>
    public class OnboardingSurveyDataConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => objectType == typeof(OnboardingSurveyData);

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JToken token = JToken.Load(reader);
            var map = new Dictionary<string, string>();
            if (token.Type == JTokenType.Object)
            {
                foreach (JProperty property in ((JObject)token).Properties())
                {
                    if (property.Value.Type == JTokenType.String)
                    {
                        map[property.Name] = (string)property.Value;
                    }
                }
            }
            return OnboardingSurveyData.FromMap(map);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            if (value is OnboardingSurveyData data)
            {
                foreach (KeyValuePair<string, string> pair in data.ToMap())
                {
                    writer.WritePropertyName(pair.Key);
                    writer.WriteValue(pair.Value);
                }
            }
            writer.WriteEndObject();
        }
    }
}
