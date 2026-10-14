using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Unity.AppUI.MVVM;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.Networking;

namespace eu.foodmission.platform
{
    /// <summary>
    /// Auth Service for connecting with API
    /// </summary>
    public class AuthService : IAuthService
    {
        public event Action OnSessionExpired;

        private readonly IStoreService _storeService;
        private System.Threading.CancellationTokenSource _refreshTimerCts;

        // Callers arriving while a refresh/check is running share it: parallel refreshes would spend the same refresh token
        private Task<RefreshOutcome> _refreshInFlight;
        private Task<bool> _checkSessionInFlight;
        // Refresh token left in the store by the last EndSession: further 401s of that same ended session are ignored
        private string _endedSessionRefreshToken;

        private const int RefreshRetrySeconds = 30;

        private enum RefreshOutcome
        {
            Success,
            // The auth server turned the refresh token down: the session is over
            Rejected,
            // No network, timeout or server error: says nothing about the session
            Unreachable
        }

        public AuthService(IStoreService storeService)
        {
            _storeService = storeService;
        }

        /// <summary>
        /// True when an auth response proves the session is gone (Keycloak answers invalid_grant with 400).
        /// Anything else (no network, 5xx, timeouts) must not end the session.
        /// </summary>
        public static bool IsSessionRejected(long responseCode)
        {
            return responseCode == 400 || responseCode == 401 || responseCode == 403;
        }

        /// <summary>
        /// False only when the session is known to be invalid. When the backend can't be reached the stored session is kept.
        /// </summary>
        public Task<bool> CheckSessionAsync()
        {
            if (_checkSessionInFlight == null || _checkSessionInFlight.IsCompleted)
            {
                _checkSessionInFlight = CheckSessionCoreAsync();
            }
            return _checkSessionInFlight;
        }

        private async Task<bool> CheckSessionCoreAsync()
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
                return await RefreshShared() != RefreshOutcome.Rejected;
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

                int remaining = (int)(state.tokenExpiresAt - DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                int refreshRemaining = state.refreshTokenExpiresAt > 0
                    ? (int)(state.refreshTokenExpiresAt - DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                    : 0;

                if (request.result == UnityWebRequest.Result.Success)
                {
                    ScheduleProactiveRefresh(remaining, refreshRemaining);
#if UNITY_EDITOR
                    // TODO: Remove this log or change to Debug after testing
                    Debug.Log($"[DEV] Bearer token (expires in {remaining}s):\n{state.accessToken}");
#endif

                    ProfileResponse profile = await FetchProfileAsync(state.accessToken);
                    if (profile != null)
                    {
                        DispatchProfileSynced(profile);
                    }

                    return true;
                }

                if (request.responseCode == 401)
                {
                    Debug.LogWarning($"[{GetType().Name}] CheckSessionAsync token-info returned 401 — attempting refresh");
                    return await RefreshShared() != RefreshOutcome.Rejected;
                }

                if (IsSessionRejected(request.responseCode))
                {
                    return false;
                }

                // Offline or server trouble: keep the session and let the timer refresh it once the backend is back
                Debug.LogWarning($"[{GetType().Name}] CheckSessionAsync could not verify the session ({request.responseCode} {request.error}) — keeping it");
                ScheduleProactiveRefresh(remaining, refreshRemaining);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[{GetType().Name}] CheckSessionAsync exception: {ex.Message} — keeping the session");
                return true;
            }
        }

        /// <summary>
        /// De-duplicated handler for 401 Unauthorized errors returned by API requests.
        /// Attempts automatic token refresh; if the auth server rejects it, logs out and raises OnSessionExpired once.
        /// </summary>
        public async Task<bool> HandleUnauthorizedAsync()
        {
            Debug.LogWarning($"[{GetType().Name}] HTTP 401 Unauthorized detected — attempting session recovery");
            RefreshOutcome outcome = await RefreshShared();

            switch (outcome)
            {
                case RefreshOutcome.Success:
                    Debug.Log($"[{GetType().Name}] Session recovered successfully via token refresh");
                    return true;
                case RefreshOutcome.Unreachable:
                    Debug.LogWarning($"[{GetType().Name}] Session recovery could not reach the auth server — keeping the session");
                    return false;
                default:
                    EndSession();
                    return false;
            }
        }

        /// <summary>
        /// Exchanges the stored refresh token for new tokens.
        /// Dispatches tokenRefreshed on success and schedules the proactive timer.
        /// </summary>
        public async Task<bool> RefreshAsync()
        {
            return await RefreshShared() == RefreshOutcome.Success;
        }

        private Task<RefreshOutcome> RefreshShared()
        {
            if (_refreshInFlight == null || _refreshInFlight.IsCompleted)
            {
                _refreshInFlight = RefreshCoreAsync();
            }
            return _refreshInFlight;
        }

        // Every caller of a rejected refresh lands here: only the first one logs out and raises OnSessionExpired
        private void EndSession()
        {
            if (_endedSessionRefreshToken != null && _endedSessionRefreshToken == (_storeService.GetAppState().refreshToken ?? ""))
            {
                return;
            }

            Debug.LogWarning($"[{GetType().Name}] Refresh token rejected — logging out and triggering OnSessionExpired");
            Logout();
            _endedSessionRefreshToken = _storeService.GetAppState().refreshToken ?? "";
            OnSessionExpired?.Invoke();
        }

        private async Task<RefreshOutcome> RefreshCoreAsync()
        {
            AppState state = _storeService.GetAppState();

            if (string.IsNullOrEmpty(state.refreshToken))
            {
                Debug.LogWarning($"[{GetType().Name}] RefreshAsync — no refresh token stored");
                return RefreshOutcome.Rejected;
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
                    return request.result == UnityWebRequest.Result.ProtocolError && IsSessionRejected(request.responseCode)
                        ? RefreshOutcome.Rejected
                        : RefreshOutcome.Unreachable;
                }

                RefreshResponse response = JsonUtility.FromJson<RefreshResponse>(request.downloadHandler.text);

                if (response == null || string.IsNullOrEmpty(response.access_token))
                {
                    Debug.LogWarning($"[{GetType().Name}] RefreshAsync — invalid response body");
                    return RefreshOutcome.Unreachable;
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
                Debug.Log($"[{GetType().Name}] Token refreshed successfully. Access Token lifespan: {response.expires_in}s ({response.expires_in / 60f:F1} min), Refresh Token lifespan: {response.refresh_expires_in}s ({response.refresh_expires_in / 60f:F1} min)");
                return RefreshOutcome.Success;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] RefreshAsync exception: {ex.Message}");
                return RefreshOutcome.Unreachable;
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

            ScheduleRefreshIn(Math.Max(effectiveExpiry - 60, 10));
        }

        private void ScheduleRefreshIn(int delaySeconds)
        {
            _refreshTimerCts?.Cancel();
            _refreshTimerCts?.Dispose();
            _refreshTimerCts = new System.Threading.CancellationTokenSource();
            _ = ProactiveRefreshLoop(delaySeconds, _refreshTimerCts.Token);
        }

        private async Task ProactiveRefreshLoop(int delaySeconds, System.Threading.CancellationToken token)
        {
            try
            {
                await Task.Delay(delaySeconds * 1000, token);

                if (token.IsCancellationRequested)
                {
                    return;
                }

                RefreshOutcome outcome = await RefreshShared();

                if (token.IsCancellationRequested)
                {
                    return;
                }

                if (outcome == RefreshOutcome.Unreachable)
                {
                    Debug.LogWarning($"[{GetType().Name}] Proactive refresh could not reach the auth server — retrying in {RefreshRetrySeconds}s");
                    ScheduleRefreshIn(RefreshRetrySeconds);
                }
                else if (outcome == RefreshOutcome.Rejected)
                {
                    EndSession();
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
                        401 => LocalizationSettings.StringDatabase.GetLocalizedString("UI", "API_ERROR_LOGIN_401"),
                        404 => LocalizationSettings.StringDatabase.GetLocalizedString("UI", "API_ERROR_404"),
                        400 => LocalizationSettings.StringDatabase.GetLocalizedString("UI", "API_ERROR_400"),
                        500 => LocalizationSettings.StringDatabase.GetLocalizedString("UI", "API_ERROR_500"),
                        _ => LocalizationSettings.StringDatabase.GetLocalizedString("UI", "API_ERROR_CONNECTION", new object[] { request.error })
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
                    string error = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "API_ERROR_INVALID_RESPONSE");
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
                Debug.Log($"[{GetType().Name}] Login successful for user: {userId}. Access Token lifespan: {response.expires_in}s ({response.expires_in / 60f:F1} min), Refresh Token lifespan: {response.refresh_expires_in}s ({response.refresh_expires_in / 60f:F1} min)");

                var eventService = App.current?.services?.GetService<IEventService>();
                if (eventService != null)
                {
                    _ = eventService.TrackSessionStartAsync();
                }

                return (true, userId, null);
            }
            catch (Exception ex)
            {
                string error = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "API_ERROR_UNEXPECTED", new object[] { ex.Message });
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
                        400 => LocalizationSettings.StringDatabase.GetLocalizedString("UI", "API_ERROR_REGISTER_400"),
                        409 => LocalizationSettings.StringDatabase.GetLocalizedString("UI", "API_ERROR_REGISTER_409"),
                        500 => LocalizationSettings.StringDatabase.GetLocalizedString("UI", "API_ERROR_500"),
                        _ => LocalizationSettings.StringDatabase.GetLocalizedString("UI", "API_ERROR_CONNECTION", new object[] { request.error })
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
                    string errorMsg = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "API_ERROR_INVALID_RESPONSE");
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
                string error = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "API_ERROR_UNEXPECTED", new object[] { ex.Message });
                Debug.LogError($"[{GetType().Name}] Register exception: {ex}");
                _storeService.store.Dispatch(AppActions.registerFailure.Invoke(error));
                return (false, null, error);
            }
        }

        private void DispatchProfileSynced(ProfileResponse profile, bool refreshDimensionLevels = true)
        {
            var payload = new AppActions.ProfilePayload(
                yearOfBirth: profile.yearOfBirth,
                country: profile.country ?? "",
                region: profile.region ?? "",
                zip: profile.zip ?? "",
                gender: profile.gender ?? "",
                annualIncome: profile.annualIncome ?? "",
                educationLevel: profile.educationLevel ?? "",
                activityLevel: profile.activityLevel ?? "",
                language: profile.language,
                settings: profile.settings,
                dietaryPreference: profile.preferences?.dietaryPreference,
                shoppingResponsibility: profile.preferences?.shoppingResponsibility ?? "",
                onboardingSurvey: profile.preferences?.onboardingSurvey,
                lastShoppingListId: profile.preferences?.lastShoppingListId ?? "",
                autoAddToPantry: profile.preferences?.autoAddToPantry ?? false,
                avatarConfig: profile.preferences?.avatarConfig,
                hasAvatar: profile.preferences?.hasAvatar ?? false,
                motivation: profile.preferences?.motivation ?? "",
                dailyTimeCommitmentMinutes: profile.preferences?.dailyTimeCommitmentMinutes ?? 0,
                segment: !string.IsNullOrEmpty(profile.segment) ? profile.segment : (profile.preferences?.segment ?? ""),
                currentQuestId: profile.currentQuestId ?? "",
                onboardingProfileCompleted: profile.preferences?.onboardingProfileCompleted ?? false,
                onboardingProfileSkippedAt: profile.preferences?.onboardingProfileSkippedAt,
                pilotSurveyCycleState: profile.preferences?.pilotSurveyCycleState,
                pilotConsentAccepted: profile.preferences?.pilotConsentAccepted ?? false,
                goals: profile.preferences?.goals
            );
            _storeService.store.Dispatch(AppActions.profileSynced.Invoke(payload));
            _ = App.current?.services?.GetService<INutriService>()?.SyncLoadoutAsync();
            _ = SyncGamificationAsync();
            if (refreshDimensionLevels)
            {
                _ = DimensionLevels.RefreshAsync(App.current?.services?.GetService<IGamificationService>(), _storeService);
            }
        }

        public static ProfileUpdateRequest BuildSettingsRequest(AppState state)
        {
            return new ProfileUpdateRequest
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
                    notificationPreferredTime = state.notificationPreferredTime,
                    backgroundPattern = state.backgroundPattern,
                    devicePushRegistration = state.devicePushRegistration,
                    // Only a pending local change is sent, so other settings syncs don't overwrite a list chosen on another device.
                    hiddenProgressWheels = state.hiddenProgressWheelsPendingSync ? (state.hiddenProgressWheels ?? new string[0]) : null
                }
            };
        }

        public async Task SyncSettingsAsync()
        {
            AppState state = _storeService.GetAppState();
            if (string.IsNullOrEmpty(state.accessToken)) return;

            var request = BuildSettingsRequest(state);

            try
            {
                string json = request.ToJson();
                string url = $"{ApiConfig.BaseUrl}/api/v1/users/me";
                byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);

                var (success, _) = await SendPatchRequest(url, bodyRaw, state.tokenType, state.accessToken);
                if (!success)
                {
                    Debug.LogWarning($"[{GetType().Name}] SyncSettingsAsync — PATCH failed");
                }
                else if (request.settings?.hiddenProgressWheels != null)
                {
                    _storeService.store.Dispatch(AppActions.markHiddenProgressWheelsSynced.Invoke(request.settings.hiddenProgressWheels));
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] SyncSettingsAsync exception: {ex.Message}");
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

                    // Use Newtonsoft.Json (not JsonUtility) so the StringOrArrayConverter
                    // on ProfilePreferences.dietaryPreference handles both old single-string
                    // DB records and new array format during migration.
                    ProfileResponse profile = JsonConvert.DeserializeObject<ProfileResponse>(responseJson);
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
            var eventService = App.current?.services?.GetService<IEventService>();
            if (eventService != null)
            {
                _ = eventService.TrackSessionEndAsync();
            }

            string refreshToken = _storeService.GetAppState().refreshToken;
            if (!string.IsNullOrEmpty(refreshToken))
            {
                _ = RevokeTokenAsync(refreshToken);
            }

            _refreshTimerCts?.Cancel();
            _refreshTimerCts?.Dispose();
            _refreshTimerCts = null;

            var localStorage = App.current?.services?.GetService<ILocalStorageService>();
            UserLocalData.Clear(localStorage);

            var avatarService = App.current?.services?.GetService<IAvatarService>();
            avatarService?.ClearFaceTexture();

            var nutriService = App.current?.services?.GetService<INutriService>();
            nutriService?.ApplyLoadout(null);

            // Challenge state is per user: a stale session or completed cache would credit or block the next user
            App.current?.services?.GetService<IChallengeSessionService>()?.Cancel();
            App.current?.services?.GetService<IChallengeCompletionService>()?.Reset();

            // Scheduled local notifications (pantry expiry, nudges) belong to this user
            App.current?.services?.GetService<INotificationService>()?.CancelAllNotifications();

            // Nudge state is per user
            App.current?.services?.GetService<IMissionNudgeService>()?.Reset();
            App.current?.services?.GetService<IDailyFoodFactService>()?.Reset();

            HomeScreen.ResetSessionDeferredFlags();
            _storeService.store.Dispatch(AppActions.logout.Invoke());
            Debug.Log($"[{GetType().Name}] User logged out and session state fully cleaned");
        }

        private async Task RevokeTokenAsync(string token)
        {
            try
            {
                string json = new RevokeTokenRequest { token = token, tokenTypeHint = "refresh_token" }.ToJson();
                string url = $"{ApiConfig.BaseUrl}/api/v1/auth/logout";

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

                if (request.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log($"[{GetType().Name}] Token revoked successfully");
                }
                else
                {
                    Debug.LogWarning($"[{GetType().Name}] Token revocation failed: {request.responseCode} (local logout still applied)");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[{GetType().Name}] Token revocation exception: {ex.Message} (local logout still applied)");
            }
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
        /// Returns (true, null) on success, or (false, ApiErrorResponse) on failure.
        /// </summary>
        public async Task<(bool success, ApiErrorResponse error)> UpdateProfileAsync(ProfileUpdateRequest request)
        {
            AppState state = _storeService.GetAppState();

            if (string.IsNullOrEmpty(state.accessToken))
            {
                Debug.LogError($"[{GetType().Name}] UpdateProfile — no access token");
                return (false, new ApiErrorResponse { statusCode = 401, error = "NO_ACCESS_TOKEN", message = "No access token available. Please log in again." });
            }

            try
            {
                string json = request.ToJson();
                Debug.Log($"[{GetType().Name}] UpdateProfile JSON: {json}");

                string url = $"{ApiConfig.BaseUrl}/api/v1/users/me";
                byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);

                var (success, error) = await SendPatchRequest(url, bodyRaw, state.tokenType, state.accessToken);
                if (!success)
                {
                    return (false, error);
                }

                ProfileResponse profile = await FetchProfileAsync(state.accessToken);
                if (profile != null)
                {
                    // Levels saved here are dispatched by their own screen; no need to reload them on every PATCH
                    DispatchProfileSynced(profile, refreshDimensionLevels: false);
                }
                else
                {
                    Debug.LogWarning($"[{GetType().Name}] UpdateProfileAsync — PATCH succeeded but profile refresh failed");
                }

                return (true, null);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] UpdateProfileAsync exception: {ex.Message}");
                return (false, new ApiErrorResponse { statusCode = 500, error = "UNEXPECTED_ERROR", message = ex.Message });
            }
        }

        public async Task<(bool success, string error)> DeleteAccountAsync(bool deleteAll = false)
        {
            AppState state = _storeService.GetAppState();

            if (string.IsNullOrEmpty(state.accessToken))
            {
                return (false, "No access token");
            }

            try
            {
                string url = $"{ApiConfig.BaseUrl}/api/v1/users/me";
                if (deleteAll)
                {
                    url += "?deleteAll=true";
                }

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
                    Debug.Log($"[{GetType().Name}] Account deleted successfully (deleteAll={deleteAll})");
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
        /// Checks whether the basic profile is complete via GET /api/v1/users/me.
        /// Returns true if complete, false otherwise.
        /// </summary>
        public async Task<bool> CheckBasicProfileCompleteAsync()
        {
            AppState state = _storeService.GetAppState();
            if (string.IsNullOrEmpty(state.accessToken)) return false;

            try
            {
                string url = $"{ApiConfig.BaseUrl}/api/v1/users/me";
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
                    string text = request.downloadHandler.text?.Trim();
                    return bool.TryParse(text, out bool isComplete) && isComplete;
                }

                if (request.responseCode == 401)
                {
                    await HandleUnauthorizedAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] CheckBasicProfileCompleteAsync exception: {ex.Message}");
            }

            return false;
        }

        /// <summary>
        /// Fetches user gamification profile via GET /api/v1/users/me/gamification.
        /// </summary>
        public async Task<(bool success, string jsonResponse, ApiErrorResponse error)> GetGamificationProfileAsync(int eventsLimit = 10, int walletEntriesLimit = 10)
        {
            AppState state = _storeService.GetAppState();
            if (string.IsNullOrEmpty(state.accessToken))
            {
                return (false, null, new ApiErrorResponse { statusCode = 401, error = "NO_ACCESS_TOKEN", message = "No access token" });
            }

            try
            {
                string url = $"{ApiConfig.BaseUrl}/api/v1/users/me/gamification?eventsLimit={eventsLimit}&walletEntriesLimit={walletEntriesLimit}";
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
                    return (true, request.downloadHandler.text, null);
                }

                if (request.responseCode == 401)
                {
                    bool refreshed = await HandleUnauthorizedAsync();
                    if (refreshed)
                    {
                        return await GetGamificationProfileAsync(eventsLimit, walletEntriesLimit);
                    }
                }

                ApiErrorResponse errResp = ApiErrorHelper.Parse(request, "[AuthService] GetGamificationProfileAsync");
                return (false, null, errResp);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] GetGamificationProfileAsync exception: {ex.Message}");
                return (false, null, new ApiErrorResponse { statusCode = 500, error = "UNEXPECTED_ERROR", message = ex.Message });
            }
        }

        /// <summary>
        /// Synchronizes gamification profile (wallet XP/points, progress indicators, badges) with Redux store.
        /// </summary>
        public async Task SyncGamificationAsync()
        {
            var (success, json, error) = await GetGamificationProfileAsync();
            if (success && !string.IsNullOrEmpty(json))
            {
                try
                {
                    var profile = JsonConvert.DeserializeObject<GamificationProfileResponse>(json);
                    if (profile != null)
                    {
                        if (profile.wallet != null)
                        {
                            _storeService.store.Dispatch(AppActions.setWalletBalance.Invoke(
                                new AppActions.WalletPayload(profile.wallet.xp, profile.wallet.points)));
                        }

                        if (profile.progressIndicators != null)
                        {
                            _storeService.store.Dispatch(AppActions.setProgressIndicators.Invoke(profile.progressIndicators));
                        }

                        if (profile.badges != null)
                        {
                            _storeService.store.Dispatch(AppActions.setBadges.Invoke(profile.badges));
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[{GetType().Name}] SyncGamificationAsync parse error: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Sends a PATCH request for profile updates.
        /// Uses new UnityWebRequest(url, "PATCH") which works correctly with NestJS.
        /// </summary>
        private async Task<(bool success, ApiErrorResponse error)> SendPatchRequest(string url, byte[] bodyRaw, string tokenType, string accessToken)
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
                ApiErrorResponse apiError = ApiErrorHelper.Parse(request, "AuthService.UpdateProfile");
                return (false, apiError);
            }

            Debug.Log($"[{GetType().Name}] Profile updated successfully via PATCH");
            return (true, null);
        }
    }
}
