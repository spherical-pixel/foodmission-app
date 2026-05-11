using UnityEngine;
using UnityEngine.Networking;

namespace eu.foodmission.platform
{
    public static class ApiErrorHelper
    {
        public static ApiErrorResponse Parse(UnityWebRequest request, string context)
        {
            string body = request.downloadHandler?.text;
            int code = (int)request.responseCode;

            ApiErrorResponse error = ApiErrorResponse.TryParse(body);

            bool isValid = error != null &&
                           (error.statusCode > 0 ||
                            !string.IsNullOrEmpty(error.message) ||
                            !string.IsNullOrEmpty(error.error));

            if (isValid)
            {
                Debug.LogError($"{context}: {code} [{error.error}] {error.message} traceId={error.traceId}");
                Debug.LogError($"{context}:  full log: {body}");
            }
            else
            {
                string snippet = body != null && body.Length > 200 ? body[..200] + "..." : body;
                Debug.LogError($"{context}: {code} — {snippet}");

                error = new ApiErrorResponse
                {
                    statusCode = code,
                    message = !string.IsNullOrEmpty(request.error)
                        ? request.error
                        : $"HTTP {code}",
                    error = snippet ?? string.Empty
                };
            }

            return error;
        }
    }
}
