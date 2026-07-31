using Unity.AppUI.MVVM;
using UnityEngine;
using UnityEngine.Networking;

namespace eu.foodmission.platform
{
    public static class ApiErrorHelper
    {
        public static ApiErrorResponse Parse(UnityWebRequest request, string context, bool logAsError = true)
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
                if (logAsError)
                {
                    Debug.LogError($"{context}: {code} [{error.error}] {error.message} traceId={error.traceId}");
                    Debug.LogError($"{context}:  full log: {body}");
                }
                else
                {
                    Debug.LogWarning($"{context}: {code} [{error.error}] {error.message} traceId={error.traceId}");
                }
            }
            else
            {
                string snippet = body != null && body.Length > 200 ? body[..200] + "..." : body;
                if (logAsError)
                {
                    Debug.LogError($"{context}: {code} — {snippet}");
                }
                else
                {
                    Debug.LogWarning($"{context}: {code} — {snippet}");
                }

                error = new ApiErrorResponse
                {
                    statusCode = code,
                    message = !string.IsNullOrEmpty(request.error)
                        ? request.error
                        : $"HTTP {code}",
                    error = snippet ?? string.Empty
                };
            }

            if (code == 401 && context != null && !context.Contains("Login") && !context.Contains("Refresh") && !context.Contains("Register"))
            {
                var authService = App.current?.services?.GetService<IAuthService>();
                if (authService != null)
                {
                    _ = authService.HandleUnauthorizedAsync();
                }
            }

            return error;
        }
    }
}
