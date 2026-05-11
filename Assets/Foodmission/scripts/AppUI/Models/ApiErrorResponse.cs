using System;
using UnityEngine;

namespace eu.foodmission.platform
{
    [Serializable]
    public class ApiErrorResponse
    {
        public int statusCode;
        public string message;
        public string error;
        public string traceId;
        public string path;

        public static ApiErrorResponse TryParse(string json)
        {
            if (string.IsNullOrEmpty(json))
                return null;

            try
            {
                return JsonUtility.FromJson<ApiErrorResponse>(json);
            }
            catch
            {
                return null;
            }
        }
    }
}
