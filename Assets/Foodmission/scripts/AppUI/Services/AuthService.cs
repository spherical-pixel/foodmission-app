using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace eu.foodmission.platform
{
    /// <summary>
    /// Auth Service for connecting with API
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly IStoreService _storeService;
        private System.Threading.CancellationTokenSource _refreshTimerCts;

        public AuthService(IStoreService storeService)
        {
            _storeService = storeService;
        }

        public async Task<bool> CheckSessionAsync()
        {
            AppState state = _storeService.GetAppState();

            // Check if there is a token and it has not expired
            if (string.IsNullOrEmpty(state.accessToken))
            {
                return false;
            }

            // Verify expiration
            long currentTimestampLong = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (state.tokenExpiresAt < currentTimestampLong)
            {
                Debug.Log($"[{GetType().Name}] Access token expired — attempting refresh");
                return await RefreshAsync();
            }

            // Verify token
            try
            {
                string url = $"{ApiConfig.BaseUrl}/api/v1/auth/token-info";
                using UnityWebRequest request = UnityWebRequest.Get(url);
                request.SetRequestHeader("Authorization", $"Bearer {state.accessToken}");
                request.SetRequestHeader("Accept", "application/json");

                UnityWebRequestAsyncOperation operation = request.SendWebRequest();
                while (!operation.isDone)
                {
                    await Task.Yield();
                }

                if (request.result == UnityWebRequest.Result.Success)
                {
                    int remaining = (int)(state.tokenExpiresAt - DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                    ScheduleProactiveRefresh(remaining);
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Exchanges the stored refresh token for a new access token.
        /// Dispatches tokenRefreshed on success and schedules the proactive timer.
        /// Returns false without throwing if the refresh token is missing or the request fails.
        /// </summary>
        public async Task<bool> RefreshAsync()
        {
            AppState state = _storeService.GetAppState();

            if (string.IsNullOrEmpty(state.refreshToken))
            {
                Debug.LogWarning($"[{GetType().Name}] RefreshAsync — no refresh token stored");
                return false;
            }

            try
            {
                string json = $"{{\"token\":\"{EscapeJson(state.refreshToken)}\"}}";
                string url = $"{ApiConfig.BaseUrl}/api/v1/auth/refresh";

                using UnityWebRequest request = new UnityWebRequest(url, "POST")
                {
                    uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json)),
                    downloadHandler = new DownloadHandlerBuffer()
                };
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Accept", "application/json");

                UnityWebRequestAsyncOperation operation = request.SendWebRequest();
                while (!operation.isDone)
                {
                    await Task.Yield();
                }

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning($"[{GetType().Name}] RefreshAsync failed: {request.responseCode} {request.error}");
                    return false;
                }

                RefreshResponse response = JsonUtility.FromJson<RefreshResponse>(request.downloadHandler.text);

                if (response == null || string.IsNullOrEmpty(response.access_token))
                {
                    Debug.LogWarning($"[{GetType().Name}] RefreshAsync — invalid response body");
                    return false;
                }

                int expiresAt = (int)(DateTimeOffset.UtcNow.ToUnixTimeSeconds() + response.expires_in);

                var payload = new AppActions.TokenRefreshPayload(
                    accessToken: response.access_token,
                    tokenType: string.IsNullOrEmpty(response.token_type) ? "Bearer" : response.token_type,
                    expiresAt: expiresAt,
                    refreshToken: response.refresh_token ?? ""
                );
                _storeService.store.Dispatch(AppActions.tokenRefreshed.Invoke(payload));

                ScheduleProactiveRefresh(response.expires_in);
                Debug.Log($"[{GetType().Name}] Token refreshed. Expires in {response.expires_in}s");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] RefreshAsync exception: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Schedules a proactive token refresh 60 seconds before the token expires.
        /// Cancels any previously scheduled refresh first.
        /// </summary>
        private void ScheduleProactiveRefresh(int expiresInSeconds)
        {
            _refreshTimerCts?.Cancel();
            _refreshTimerCts?.Dispose();
            _refreshTimerCts = new System.Threading.CancellationTokenSource();
            _ = ProactiveRefreshLoop(expiresInSeconds, _refreshTimerCts.Token);
        }

        private async Task ProactiveRefreshLoop(int expiresInSeconds, System.Threading.CancellationToken token)
        {
            try
            {
                int delayMs = Math.Max(expiresInSeconds - 60, 10) * 1000;
                await Task.Delay(delayMs, token);

                if (token.IsCancellationRequested)
                {
                    return;
                }

                bool success = await RefreshAsync();
                if (!success)
                {
                    Debug.LogWarning($"[{GetType().Name}] Proactive refresh failed — logging out");
                    Logout();
                }
            }
            catch (OperationCanceledException) { }
        }

        public async Task<(bool success, string userId, string error)> LoginAsync(string username, string password)
        {
            _storeService.store.Dispatch(AppActions.loginRequest.Invoke(username));

            try
            {
                LoginRequest loginData = new LoginRequest
                {
                    username = username,
                    password = password
                };

                string json = JsonUtility.ToJson(loginData);
                string url = $"{ApiConfig.BaseUrl}/api/v1/auth/login";

                using UnityWebRequest request = new UnityWebRequest(url, "POST")
                {
                    uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json)),
                    downloadHandler = new DownloadHandlerBuffer()
                };

                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Accept", "application/json");

                UnityWebRequestAsyncOperation operation = request.SendWebRequest();
                while (!operation.isDone)
                {
                    await Task.Yield();
                }

                if (request.result != UnityWebRequest.Result.Success)
                {
                    string errorMessage = request.responseCode switch
                    {
                        // TODO: Add to localization table
                        401 => "Usuario o contraseña incorrectos", 
                        404 => "Servicio no encontrado",
                        400 => "Solicitud inválida",
                        500 => "Error del servidor",
                        _ => $"Error de conexión: {request.error}"
                    };

                    Debug.LogError($"[{GetType().Name}] Login failed: {errorMessage} (Code: {request.responseCode})");
                    _storeService.store.Dispatch(AppActions.loginFailure.Invoke(errorMessage));
                    return (false, null, errorMessage);
                }

                string responseJson = request.downloadHandler.text;
                Debug.Log($"[{GetType().Name}] Login response: {responseJson}");

                LoginResponse response = JsonUtility.FromJson<LoginResponse>(responseJson);

                if (string.IsNullOrEmpty(response?.access_token))
                {
                    // TODO: Add to localization table
                    string error = "Respuesta inválida del servidor";
                    _storeService.store.Dispatch(AppActions.loginFailure.Invoke(error));
                    return (false, null, error);
                }

                // Calculate token expiration (int for JsonUtility compatibility)
                int expiresAt = (int)(DateTimeOffset.UtcNow.ToUnixTimeSeconds() + response.expires_in);

                // Fetch user profile to get userId, username and email
                ProfileResponse profile = await FetchProfileAsync(response.access_token);
                string userId = profile?.id ?? username;
                string userName = profile?.username ?? "";
                string userEmail = profile?.email ?? "";

                if (profile == null)
                {
                    Debug.LogWarning($"[{GetType().Name}] Profile fetch failed, using login input as fallback");
                }

                // Create login payload and dispatch it to the store
                AppActions.LoginPayload payload = new AppActions.LoginPayload(
                    userId: userId,
                    userName: userName,
                    email: userEmail,
                    accessToken: response.access_token,
                    tokenType: response.token_type,
                    expiresAt: expiresAt,
                    refreshToken: response.refresh_token ?? ""
                );

                _storeService.store.Dispatch(AppActions.loginSuccess.Invoke(payload));
                ScheduleProactiveRefresh(response.expires_in);
                Debug.Log($"[{GetType().Name}] Login successful for user: {userId}");

                return (true, userId, null);
            }
            catch (Exception ex)
            {
                // TODO: Add to localization table
                string error = $"Error inesperado: {ex.Message}";
                Debug.LogError($"[{GetType().Name}] Login exception: {ex}");
                _storeService.store.Dispatch(AppActions.loginFailure.Invoke(error));
                return (false, null, error);
            }
        }

        public async Task<(bool success, string userId, string error)> RegisterAsync(
            string username,
            string email,
            string password,
            int yearOfBirth = 0,
            string country = null,
            string region = null,
            string zip = null)
        {
            _storeService.store.Dispatch(AppActions.registerRequest.Invoke());

            try
            {
                // Build JSON manually to only include fields with values
                var jsonBuilder = new System.Text.StringBuilder();
                jsonBuilder.Append("{");
                jsonBuilder.AppendFormat("\"username\":\"{0}\",", EscapeJson(username));
                jsonBuilder.AppendFormat("\"email\":\"{0}\",", EscapeJson(email));
                jsonBuilder.AppendFormat("\"password\":\"{0}\"", EscapeJson(password));

                if (yearOfBirth > 0)
                {
                    jsonBuilder.AppendFormat(",\"yearOfBirth\":{0}", yearOfBirth);
                }
                if (!string.IsNullOrEmpty(country))
                {
                    jsonBuilder.AppendFormat(",\"country\":\"{0}\"", EscapeJson(country));
                }
                if (!string.IsNullOrEmpty(region))
                {
                    jsonBuilder.AppendFormat(",\"region\":\"{0}\"", EscapeJson(region));
                }
                if (!string.IsNullOrEmpty(zip))
                {
                    jsonBuilder.AppendFormat(",\"zip\":\"{0}\"", EscapeJson(zip));
                }

                jsonBuilder.Append("}");
                string json = jsonBuilder.ToString();

                string url = $"{ApiConfig.BaseUrl}/api/v1/auth/register";

                using UnityWebRequest request = new UnityWebRequest(url, "POST")
                {
                    uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json)),
                    downloadHandler = new DownloadHandlerBuffer()
                };

                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Accept", "application/json");

                UnityWebRequestAsyncOperation operation = request.SendWebRequest();
                while (!operation.isDone)
                {
                    await Task.Yield();
                }

                if (request.result != UnityWebRequest.Result.Success)
                {
                    string errorMessage = request.responseCode switch
                    {
                        400 => "Datos de registro inválidos",
                        409 => "El usuario ya existe",
                        500 => "Error del servidor",
                        _ => $"Error de conexión: {request.error}"
                    };

                    Debug.LogError($"[{GetType().Name}] Register failed: {errorMessage} (Code: {request.responseCode})");
                    _storeService.store.Dispatch(AppActions.registerFailure.Invoke(errorMessage));
                    return (false, null, errorMessage);
                }

                string responseJson = request.downloadHandler.text;
                Debug.Log($"[{GetType().Name}] Register response: {responseJson}");

                RegisterResponse response = JsonUtility.FromJson<RegisterResponse>(responseJson);

                if (response?.createdUser == null || string.IsNullOrEmpty(response.createdUser.id))
                {
                    string errorMsg = "Respuesta inválida del servidor";
                    _storeService.store.Dispatch(AppActions.registerFailure.Invoke(errorMsg));
                    return (false, null, errorMsg);
                }

                // Register successful - dispatch success
                _storeService.store.Dispatch(AppActions.registerSuccess.Invoke(response.createdUser.id));
                Debug.Log($"[{GetType().Name}] Register successful for user: {response.createdUser.username}");

                // Auto-login after successful registration
                Debug.Log($"[{GetType().Name}] Attempting auto-login for: {username}");
                var loginResult = await LoginAsync(username, password);

                if (loginResult.success)
                {
                    Debug.Log($"[{GetType().Name}] Auto-login successful after registration");
                    return (true, loginResult.userId, null);
                }
                else
                {
                    Debug.LogWarning($"[{GetType().Name}] Auto-login failed after registration: {loginResult.error}");
                    // Registration succeeded but auto-login failed - still return success
                    return (true, response.createdUser.id, null);
                }
            }
            catch (Exception ex)
            {
                string error = $"Error inesperado: {ex.Message}";
                Debug.LogError($"[{GetType().Name}] Register exception: {ex}");
                _storeService.store.Dispatch(AppActions.registerFailure.Invoke(error));
                return (false, null, error);
            }
        }

        /// <summary>
        /// Fetches user profile from GET /api/v1/auth/profile
        /// </summary>
        private async Task<ProfileResponse> FetchProfileAsync(string accessToken)
        {
            try
            {
                string url = $"{ApiConfig.BaseUrl}/api/v1/auth/profile";
                using UnityWebRequest request = UnityWebRequest.Get(url);
                request.SetRequestHeader("Authorization", $"Bearer {accessToken}");
                request.SetRequestHeader("Accept", "application/json");

                UnityWebRequestAsyncOperation operation = request.SendWebRequest();
                while (!operation.isDone)
                {
                    await Task.Yield();
                }

                if (request.result == UnityWebRequest.Result.Success)
                {
                    string responseJson = request.downloadHandler.text;
                    Debug.Log($"[{GetType().Name}] Profile response: {responseJson}");

                    ProfileResponse profile = JsonUtility.FromJson<ProfileResponse>(responseJson);
                    if (profile != null && !string.IsNullOrEmpty(profile.id))
                    {
                        return profile;
                    }
                }
                else
                {
                    Debug.LogWarning($"[{GetType().Name}] Profile fetch failed: {request.responseCode}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] Profile fetch error: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Escapes special characters for JSON strings
        /// </summary>
        private string EscapeJson(string str)
        {
            if (string.IsNullOrEmpty(str))
                return str;

            return str
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t");
        }

        public void Logout()
        {
            _refreshTimerCts?.Cancel();
            _refreshTimerCts?.Dispose();
            _refreshTimerCts = null;
            _storeService.store.Dispatch(AppActions.logout.Invoke());
            Debug.Log($"[{GetType().Name}] User logged out");
        }

        /// <summary>
        /// Requests a password reset email for the given email address.
        /// Calls POST /api/v1/auth/forgot-password on the backend.
        /// </summary>
        public async Task<(bool success, string message)> RequestPasswordResetAsync(string email)
        {
            try
            {
                var requestData = new { email = email };
                string json = JsonUtility.ToJson(requestData);
                string url = $"{ApiConfig.BaseUrl}/api/v1/auth/forgot-password";

                using UnityWebRequest request = new UnityWebRequest(url, "POST")
                {
                    uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json)),
                    downloadHandler = new DownloadHandlerBuffer()
                };

                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Accept", "application/json");

                UnityWebRequestAsyncOperation operation = request.SendWebRequest();
                while (!operation.isDone)
                {
                    await Task.Yield();
                }

                Debug.Log($"[AuthService] Password reset response: code={request.responseCode}, result={request.result}");

                if (request.result == UnityWebRequest.Result.Success)
                {
                    return (true, "Password reset email sent successfully");
                }

                string errorMessage = request.responseCode switch
                {
                    404 => "Email address not found",
                    422 => "Invalid email format",
                    500 => "Server error. Please try again later.",
                    _ => $"Request failed: {request.error}"
                };

                return (false, errorMessage);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AuthService] Password reset exception: {ex}");
                return (false, "An unexpected error occurred");
            }
        }

        /// <summary>
        /// Updates the current user's extended profile via PATCH /api/v1/users/me.
        /// Only non-null fields are included in the request body.
        /// Returns true on success, false on failure.
        /// </summary>
        public async Task<bool> UpdateProfileAsync(ProfileUpdateRequest request)
        {
            AppState state = _storeService.GetAppState();

            if (string.IsNullOrEmpty(state.accessToken))
            {
                Debug.LogError($"[{GetType().Name}] UpdateProfile — no access token");
                return false;
            }

            try
            {
                // Build JSON manually — only include non-empty fields (PATCH semantics)
                var jsonBuilder = new System.Text.StringBuilder();
                jsonBuilder.Append("{");

                bool hasField = false;

                if (!string.IsNullOrEmpty(request.gender))
                {
                    jsonBuilder.AppendFormat("\"gender\":\"{0}\"", EscapeJson(request.gender));
                    hasField = true;
                }

                if (!string.IsNullOrEmpty(request.activityLevel))
                {
                    if (hasField) jsonBuilder.Append(",");
                    jsonBuilder.AppendFormat("\"activityLevel\":\"{0}\"", EscapeJson(request.activityLevel));
                    hasField = true;
                }

                if (!string.IsNullOrEmpty(request.educationLevel))
                {
                    if (hasField) jsonBuilder.Append(",");
                    jsonBuilder.AppendFormat("\"educationLevel\":\"{0}\"", EscapeJson(request.educationLevel));
                    hasField = true;
                }

                if (!string.IsNullOrEmpty(request.annualIncome))
                {
                    if (hasField) jsonBuilder.Append(",");
                    jsonBuilder.AppendFormat("\"annualIncome\":\"{0}\"", EscapeJson(request.annualIncome));
                    hasField = true;
                }

                // Preferences nested object — only include non-empty fields
                if (request.preferences != null)
                {
                    bool hasDietary = !string.IsNullOrEmpty(request.preferences.dietaryPreference);
                    bool hasShopping = !string.IsNullOrEmpty(request.preferences.shoppingResponsibility);

                    if (hasDietary || hasShopping)
                    {
                        if (hasField) jsonBuilder.Append(",");
                        jsonBuilder.Append("\"preferences\":{");

                        bool hasPrefField = false;

                        if (hasDietary)
                        {
                            jsonBuilder.AppendFormat("\"dietaryPreference\":\"{0}\"", EscapeJson(request.preferences.dietaryPreference));
                            hasPrefField = true;
                        }

                        if (hasShopping)
                        {
                            if (hasPrefField) jsonBuilder.Append(",");
                            jsonBuilder.AppendFormat("\"shoppingResponsibility\":\"{0}\"", EscapeJson(request.preferences.shoppingResponsibility));
                        }

                        jsonBuilder.Append("}");
                        hasField = true;
                    }
                }

                jsonBuilder.Append("}");
                string json = jsonBuilder.ToString();
                Debug.Log($"[{GetType().Name}] UpdateProfile JSON: {json}");

                string url = $"{ApiConfig.BaseUrl}/api/v1/users/me";
                byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);

                bool success = await SendPatchRequest(url, bodyRaw, state.tokenType, state.accessToken);

                return success;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] UpdateProfileAsync exception: {ex.Message}");
                return false;
            }
        }

        public async Task<(bool success, string error)> DeleteAccountAsync()
        {
            AppState state = _storeService.GetAppState();

            if (string.IsNullOrEmpty(state.accessToken))
            {
                return (false, "No access token");
            }

            try
            {
                string url = $"{ApiConfig.BaseUrl}/api/v1/users/me";

                using UnityWebRequest request = new UnityWebRequest(url, "DELETE");
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Accept", "application/json");
                request.SetRequestHeader("Authorization", $"{state.tokenType} {state.accessToken}");

                UnityWebRequestAsyncOperation operation = request.SendWebRequest();
                while (!operation.isDone)
                {
                    await Task.Yield();
                }

                if (request.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log($"[{GetType().Name}] Account deleted successfully");
                    Logout();
                    return (true, null);
                }

                string responseBody = request.downloadHandler?.text ?? "no body";
                Debug.LogError($"[{GetType().Name}] DeleteAccount failed: {request.responseCode} {request.error} — Body: {responseBody}");
                return (false, $"Request failed: {request.responseCode}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] DeleteAccount exception: {ex.Message}");
                return (false, "An unexpected error occurred");
            }
        }

        /// <summary>
        /// Sends a PATCH request for profile updates.
        /// Uses new UnityWebRequest(url, "PATCH") which works correctly with NestJS.
        /// </summary>
        private async Task<bool> SendPatchRequest(string url, byte[] bodyRaw, string tokenType, string accessToken)
        {
            using UnityWebRequest request = new UnityWebRequest(url, "PATCH");
            request.uploadHandler = new UploadHandlerRaw(bodyRaw)
            {
                contentType = "application/json"
            };
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Accept", "application/json");
            request.SetRequestHeader("Authorization", $"{tokenType} {accessToken}");

            UnityWebRequestAsyncOperation operation = request.SendWebRequest();
            while (!operation.isDone)
            {
                await Task.Yield();
            }

            if (request.result != UnityWebRequest.Result.Success)
            {
                string responseBody = request.downloadHandler?.text ?? "no body";
                Debug.LogError($"[{GetType().Name}] UpdateProfile PATCH failed: {request.responseCode} {request.error} — Body: {responseBody}");
                return false;
            }

            Debug.Log($"[{GetType().Name}] Profile updated successfully via PATCH");
            return true;
        }
    }
}
