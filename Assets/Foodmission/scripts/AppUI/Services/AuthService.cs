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
                // If refresh token expiry is known and also expired, skip the refresh attempt
                if (state.refreshTokenExpiresAt > 0 && state.refreshTokenExpiresAt < currentTimestampLong)
                {
                    Debug.Log($"[{GetType().Name}] Both access and refresh tokens expired — re-login required");
                    return false;
                }

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
                    int refreshRemaining = state.refreshTokenExpiresAt > 0
                        ? (int)(state.refreshTokenExpiresAt - DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                        : 0;
                    ScheduleProactiveRefresh(remaining, refreshRemaining);
                    Debug.Log($"[DEV] Bearer token (expires in {remaining}s):\n{state.accessToken}");

                    ProfileResponse profile = await FetchProfileAsync(state.accessToken);
                    if (profile != null)
                    {
                        DispatchProfileSynced(profile);
                    }

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
                string json = new RefreshRequest { token = state.refreshToken }.ToJson();
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

                long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                int expiresAt = (int)(now + response.expires_in);
                int refreshExpiresAt = response.refresh_expires_in > 0
                    ? (int)(now + response.refresh_expires_in)
                    : 0;

                var payload = new AppActions.TokenRefreshPayload(
                    accessToken: response.access_token,
                    tokenType: string.IsNullOrEmpty(response.token_type) ? "Bearer" : response.token_type,
                    expiresAt: expiresAt,
                    refreshToken: response.refresh_token ?? "",
                    refreshTokenExpiresAt: refreshExpiresAt
                );
                _storeService.store.Dispatch(AppActions.tokenRefreshed.Invoke(payload));

                ScheduleProactiveRefresh(response.expires_in, response.refresh_expires_in);
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
        /// Schedules a proactive token refresh 60 seconds before the earliest expiry
        /// (access token or refresh token, whichever comes first).
        /// </summary>
        private void ScheduleProactiveRefresh(int accessExpiresInSeconds, int refreshExpiresInSeconds = 0)
        {
            int effectiveExpiry = accessExpiresInSeconds;
            if (refreshExpiresInSeconds > 0)
            {
                effectiveExpiry = Math.Min(accessExpiresInSeconds, refreshExpiresInSeconds);
            }

            _refreshTimerCts?.Cancel();
            _refreshTimerCts?.Dispose();
            _refreshTimerCts = new System.Threading.CancellationTokenSource();
            _ = ProactiveRefreshLoop(effectiveExpiry, _refreshTimerCts.Token);
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

                long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                int expiresAt = (int)(now + response.expires_in);
                int refreshExpiresAt = response.refresh_expires_in > 0
                    ? (int)(now + response.refresh_expires_in)
                    : 0;

                // Fetch user profile
                ProfileResponse profile = await FetchProfileAsync(response.access_token);
                string userId = profile?.id ?? username;
                string userName = profile?.username ?? "";
                string userEmail = profile?.email ?? "";

                if (profile == null)
                {
                    Debug.LogWarning($"[{GetType().Name}] Profile fetch failed, using login input as fallback");
                }

                AppActions.LoginPayload payload = new AppActions.LoginPayload(
                    userId: userId,
                    userName: userName,
                    email: userEmail,
                    accessToken: response.access_token,
                    tokenType: response.token_type,
                    expiresAt: expiresAt,
                    refreshToken: response.refresh_token ?? "",
                    refreshTokenExpiresAt: refreshExpiresAt
                );

                _storeService.store.Dispatch(AppActions.loginSuccess.Invoke(payload));

                if (profile != null)
                {
                    DispatchProfileSynced(profile);
                }

                ScheduleProactiveRefresh(response.expires_in, response.refresh_expires_in);
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
                string json = new RegisterRequest
                {
                    username = username,
                    email = email,
                    password = password,
                    yearOfBirth = yearOfBirth > 0 ? yearOfBirth : (int?)null,
                    country = string.IsNullOrEmpty(country) ? null : country,
                    region = string.IsNullOrEmpty(region) ? null : region,
                    zip = string.IsNullOrEmpty(zip) ? null : zip
                }.ToJson();

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

        private void DispatchProfileSynced(ProfileResponse profile)
        {
            var payload = new AppActions.ProfilePayload(
                firstName: profile.firstName ?? "",
                lastName: profile.lastName ?? "",
                yearOfBirth: profile.yearOfBirth,
                country: profile.country ?? "",
                region: profile.region ?? "",
                zip: profile.zip ?? "",
                gender: profile.gender ?? "",
                annualIncome: profile.annualIncome ?? "",
                educationLevel: profile.educationLevel ?? "",
                activityLevel: profile.activityLevel ?? "",
                weightKg: profile.weightKg,
                heightCm: profile.heightCm,
                language: profile.language,
                settings: profile.settings
            );
            _storeService.store.Dispatch(AppActions.profileSynced.Invoke(payload));
        }

        public async Task SyncSettingsAsync()
        {
            AppState state = _storeService.GetAppState();
            if (string.IsNullOrEmpty(state.accessToken)) return;

            var request = new ProfileUpdateRequest
            {
                language = state.lang,
                settings = new UserSettingsDto
                {
                    theme = state.theme,
                    scale = state.scale,
                    font = state.font,
                    soundVolume = state.soundVolume,
                    musicVolume = state.musicVolume,
                    pushNotificationsEnabled = state.pushNotificationsEnabled,
                    backgroundPattern = state.backgroundPattern
                }
            };

            bool success = await UpdateProfileAsync(request);
            if (!success)
            {
                Debug.LogWarning($"[{GetType().Name}] SyncSettingsAsync — PATCH failed");
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
                string json = new ForgotPasswordRequest { email = email }.ToJson();
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
                string json = request.ToJson();
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
