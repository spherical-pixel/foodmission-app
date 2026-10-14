using System.Threading.Tasks;

using Unity.AppUI.MVVM;

using UnityEngine.Networking;

namespace eu.foodmission.platform
{
    /// <summary>
    /// Single way to send an API request: applies a timeout and refreshes an expiring access token before sending.
    /// A sent UnityWebRequest can't be replayed, so an expired token is renewed up front instead of retrying after a 401.
    /// </summary>
    public static class ApiRequest
    {
        /// <summary>Without a timeout a request on a stalled connection never finishes and the screen stays loading.</summary>
        public const int DefaultTimeoutSeconds = 30;

        private const string AuthorizationHeader = "Authorization";

        /// <param name="refreshToken">False for the auth endpoints themselves (refresh, token-info), which manage the token.</param>
        public static async Task SendAsync(this UnityWebRequest request, bool refreshToken = true)
        {
            if (request.timeout <= 0)
            {
                request.timeout = DefaultTimeoutSeconds;
            }

            if (refreshToken && !string.IsNullOrEmpty(request.GetRequestHeader(AuthorizationHeader)))
            {
                var authService = App.current?.services?.GetService<IAuthService>();
                if (authService != null)
                {
                    string header = await authService.EnsureFreshTokenAsync();
                    if (!string.IsNullOrEmpty(header))
                    {
                        request.SetRequestHeader(AuthorizationHeader, header);
                    }
                }
            }

            UnityWebRequestAsyncOperation op = request.SendWebRequest();
            while (!op.isDone)
            {
                await Task.Yield();
            }
        }
    }
}
