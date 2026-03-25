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
        private readonly string _baseUrl = "https://test.api.foodmission.eu";

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
                return false;
            }

            // Verify token
            try
            {
                string url = $"{_baseUrl}/api/v1/auth/token-info";
                using UnityWebRequest request = UnityWebRequest.Get(url);
                request.SetRequestHeader("Authorization", $"Bearer {state.accessToken}");
                request.SetRequestHeader("Accept", "application/json");

                UnityWebRequestAsyncOperation operation = request.SendWebRequest();
                while (!operation.isDone)
                {
                    await Task.Yield();
                }

                return request.result == UnityWebRequest.Result.Success;
            }
            catch
            {
                return false;
            }
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
                string url = $"{_baseUrl}/api/v1/auth/login";

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

                // Fetch user profile to get userId (backend stores keycloakId as id)
                string userId = await FetchUserIdAsync(response.access_token);
                if (string.IsNullOrEmpty(userId))
                {
                    // If profile fetch fails, use email from login as fallback identification
                    userId = username;
                    Debug.LogWarning($"[{GetType().Name}] Profile fetch failed, using username as userId");
                }

                // Create login payload and dispatch it to the store
                AppActions.LoginPayload payload = new AppActions.LoginPayload(
                    userId: userId,
                    email: username,
                    accessToken: response.access_token,
                    tokenType: response.token_type,
                    expiresAt: expiresAt
                );

                _storeService.store.Dispatch(AppActions.loginSuccess.Invoke(payload));
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

                string url = $"{_baseUrl}/api/v1/auth/register";

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
        /// Fetches user profile to get the userId from the backend
        /// </summary>
        private async Task<string> FetchUserIdAsync(string accessToken)
        {
            try
            {
                string url = $"{_baseUrl}/api/v1/auth/profile";
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

                    // Parse the profile response - it contains id, email, etc.
                    // The backend returns { id, email, firstName, lastName, keycloakId, ... }
                    var profile = JsonUtility.FromJson<ProfileResponse>(responseJson);
                    if (profile != null && !string.IsNullOrEmpty(profile.id))
                    {
                        return profile.id;
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
            _storeService.store.Dispatch(AppActions.logout.Invoke());
            Debug.Log("[AuthService] User logged out");
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
                string url = $"{_baseUrl}/api/v1/auth/forgot-password";

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
    }
}
