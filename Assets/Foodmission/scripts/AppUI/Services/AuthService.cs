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
            long currentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (state.tokenExpiresAt < currentTimestamp)
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

                // Calculate token expiration
                long expiresAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + response.expires_in;

                // Create login payload and dispatch it to the store
                AppActions.LoginPayload payload = new AppActions.LoginPayload(
                    userId: response.user.id,
                    email: response.user.email,
                    // firstName: response.user.firstName,
                    // lastName: response.user.lastName,
                    accessToken: response.access_token,
                    tokenType: response.token_type,
                    expiresAt: expiresAt
                );

                _storeService.store.Dispatch(AppActions.loginSuccess.Invoke(payload));
                Debug.Log($"[{GetType().Name}] Login successful for user: {response.user.email}");

                return (true, response.user.id, null);
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

        public async Task<(bool success, string userId, string error)> RegisterAsync(string username, string email, string password)
        {
            _storeService.store.Dispatch(AppActions.registerRequest.Invoke());

            try
            {
                RegisterRequest registerData = new RegisterRequest
                {
                    username = username,
                    email = email,
                    password = password
                };

                string json = JsonUtility.ToJson(registerData);
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
                        // TODO: Add to localization table
                        400 => "Datos de registro inválidos",
                        409 => "El usuario ya existe",
                        500 => "Error del servidor",
                        _ => $"Error de conexión: {request.error}"
                    };

                    Debug.LogError($"[{GetType().Name}] Register failed: {errorMessage}");
                    _storeService.store.Dispatch(AppActions.registerFailure.Invoke(errorMessage));
                    return (false, null, errorMessage);
                }

                string responseJson = request.downloadHandler.text;
                Debug.Log($"[{GetType().Name}] Register response: {responseJson}");

                LoginResponse response = JsonUtility.FromJson<LoginResponse>(responseJson);

                if (string.IsNullOrEmpty(response?.access_token))
                {
                    // TODO: Add to localization table
                    string error = "Respuesta inválida del servidor";
                    _storeService.store.Dispatch(AppActions.registerFailure.Invoke(error));
                    return (false, null, error);
                }

                // Calculate expiration timestamp
                long expiresAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + response.expires_in;

                // Create payload with registered user data 
                AppActions.LoginPayload payload = new AppActions.LoginPayload(
                    userId: response.user.id,
                    email: response.user.email,
                    // firstName: response.user.firstName,
                    // lastName: response.user.lastName,
                    accessToken: response.access_token,
                    tokenType: response.token_type,
                    expiresAt: expiresAt
                );

                // Dispatch registerSuccess and loginSuccess
                _storeService.store.Dispatch(AppActions.registerSuccess.Invoke(response.user.id));
                _storeService.store.Dispatch(AppActions.loginSuccess.Invoke(payload));

                Debug.Log($"[{GetType().Name}] Register successful for user: {response.user.email}");
                return (true, response.user.id, null);
            }
            catch (Exception ex)
            {
                // TODO: Add to localization table
                string error = $"Error inesperado: {ex.Message}";
                Debug.LogError($"[{GetType().Name}] Register exception: {ex}");
                _storeService.store.Dispatch(AppActions.registerFailure.Invoke(error));
                return (false, null, error);
            }
        }

        public void Logout()
        {
            _storeService.store.Dispatch(AppActions.logout.Invoke());
            Debug.Log("[AuthService] User logged out");
        }
    }
}
