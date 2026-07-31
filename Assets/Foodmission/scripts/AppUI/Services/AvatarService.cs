using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.AppUI.MVVM;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace eu.foodmission.platform
{
    public class AvatarService : IAvatarService
    {
        private const string PREFAB_ADDRESS = "Assets/Foodmission/prefabs/AvatarController.prefab";
        private const string PLAYER_PREFS_KEY = "AvatarConfig";

        private const string HAS_AVATAR_PREFS_KEY = "HasAvatar";

        private GameObject _avatarControllerObject;
        private AvatarController _avatarController;

        private bool _isInitialized;
        private AvatarConfig _currentConfig = null;

        private readonly IStoreService _storeService;
        private readonly IAuthService _authService;

        public AvatarService(IStoreService storeService = null, IAuthService authService = null)
        {
            _storeService = storeService;
            _authService = authService;
        }

        public RenderTexture AvatarCameraRenderTexture => _avatarController?.avatarCamera != null ? _avatarController.avatarCamera.targetTexture : null;
        public RenderTexture FullBodyAvatarRenderTexture => _avatarController?.fullBodyCamera != null ? _avatarController.fullBodyCamera.targetTexture : null;
        public AvatarConfig GetCurrentAvatarConfig => _currentConfig;

        public bool IsInitialized => _isInitialized;
        public bool HasSavedConfig => HasAvatar && (_currentConfig != null || PlayerPrefs.HasKey(PLAYER_PREFS_KEY));

        public bool HasAvatar
        {
            get
            {
                var store = _storeService ?? App.current?.services?.GetService<IStoreService>();
                AppState state = store?.GetAppState();
                if (state != null && state.userHasAvatar)
                {
                    return true;
                }
                return PlayerPrefs.GetInt(HAS_AVATAR_PREFS_KEY, 0) == 1;
            }
        }

        public async Task InitializeAsync()
        {
            if (_isInitialized)
            {
                Debug.LogWarning($"[{GetType().Name}] Already initialized");
                return;
            }

            // Reuse existing instance if present (after USS hot-reload)
            var existing = GameObject.Find("AvatarController");
            if (existing != null)
            {
                _avatarControllerObject = existing;
                _avatarController = _avatarControllerObject.GetComponent<AvatarController>();

                if (_avatarController != null)
                {
                    _isInitialized = true;
                    Debug.Log($"[{GetType().Name}] Reused existing AvatarController");
                }
                return;
            }

            Debug.Log($"[{GetType().Name}] Loading Avatar from Addressables: {PREFAB_ADDRESS}");

            try
            {
                AsyncOperationHandle<GameObject> handle = Addressables.LoadAssetAsync<GameObject>(PREFAB_ADDRESS);
                GameObject prefab = await handle.Task;

                if (handle.Status == AsyncOperationStatus.Failed)
                {
                    Debug.LogError($"[{GetType().Name}] Failed to load from Addressables: {handle.OperationException?.Message}");
                    return;
                }

                if (prefab == null)
                {
                    Debug.LogError($"[{GetType().Name}] Failed to load prefab from Addressables");
                    return;
                }

                _avatarControllerObject = UnityEngine.Object.Instantiate(prefab);
                _avatarControllerObject.name = "AvatarController";
                _avatarControllerObject.SetActive(true);

                _avatarController = _avatarControllerObject.GetComponent<AvatarController>();

                if (_avatarController == null)
                {
                    Debug.LogError($"[{GetType().Name}] AvatarController component not found on prefab");
                    return;
                }

                _isInitialized = true;
                Debug.Log($"[{GetType().Name}] Avatar initialized successfully");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] Error loading Avatar: {ex.Message}");
                Debug.LogError(ex.StackTrace);
            }
        }

        public void SetAvatarConfig(AvatarConfig config)
        {
            _currentConfig = config;
            if (_avatarController != null && _currentConfig != null)
            {
                _avatarController.ApplyAvatar(_currentConfig);
            }
        }

        public void SetRandomConfig()
        {
            int skinColor = Random.Range(1, 10);
            AvatarConfig randomConfig = new AvatarConfig
            {
                hair = new AvatarPartConfig { idPart = Random.Range(0, 10), idColor = Random.Range(1, 10) },
                eyebrows = new AvatarPartConfig { idPart = Random.Range(1, 10), idColor = Random.Range(1, 10) },
                eyes = new AvatarPartConfig { idPart = Random.Range(1, 10), idColor = Random.Range(1, 10) },
                nose = new AvatarPartConfig { idPart = Random.Range(1, 10), idColor = skinColor },
                mouth = new AvatarPartConfig { idPart = Random.Range(1, 10), idColor = 0 },
                facialHair = new AvatarPartConfig { idPart = Random.Range(0, 5), idColor = Random.Range(1, 10) },
                skin = new AvatarPartConfig { idPart = 0, idColor = skinColor },
                tshirt = new AvatarPartConfig { idPart = 0, idColor = Random.Range(1, 10) },
                trousers = new AvatarPartConfig { idPart = 0, idColor = Random.Range(1, 10) },
                shoes = new AvatarPartConfig { idPart = 0, idColor = Random.Range(1, 10) }
            };

            SetAvatarConfig(randomConfig);
        }

        public List<Color> GetColorPalette(AvatarEditorItemEnum itemType)
        {
            if (_avatarController == null)
                return new List<Color>();

            switch (itemType)
            {
                case AvatarEditorItemEnum.Hair:
                case AvatarEditorItemEnum.Eyebrows:
                case AvatarEditorItemEnum.FacialHair:
                    return new List<Color>(_avatarController.HairColors);
                case AvatarEditorItemEnum.Eyes:
                    return new List<Color>(_avatarController.EyesColors);
                case AvatarEditorItemEnum.Nose:
                case AvatarEditorItemEnum.Mouth:
                case AvatarEditorItemEnum.Skin:
                    return new List<Color>(_avatarController.SkinColors);
                case AvatarEditorItemEnum.Tshirt:
                case AvatarEditorItemEnum.Trousers:
                case AvatarEditorItemEnum.Shoes:
                    return new List<Color>(_avatarController.ClothesColors);
                default:
                    return new List<Color>(_avatarController.ClothesColors);
            }
        }

        public int GetMaxPartCount(AvatarEditorItemEnum itemType)
        {
            if (_avatarController == null)
                return 1;

            switch (itemType)
            {
                case AvatarEditorItemEnum.Hair:
                    return _avatarController.hairParts.Count;
                case AvatarEditorItemEnum.Eyebrows:
                    return _avatarController.eyebrowTextures.Count;
                case AvatarEditorItemEnum.Eyes:
                    return _avatarController.eyeTextures.Count;
                case AvatarEditorItemEnum.Nose:
                    return _avatarController.noseParts.Count;
                case AvatarEditorItemEnum.Mouth:
                    return _avatarController.mouthTextures.Count;
                case AvatarEditorItemEnum.FacialHair:
                    return _avatarController.facialHairTextures.Count;
                case AvatarEditorItemEnum.Skin:
                case AvatarEditorItemEnum.Tshirt:
                case AvatarEditorItemEnum.Trousers:
                case AvatarEditorItemEnum.Shoes:
                    return 1;
                default:
                    return 1;
            }
        }

        public void SetAvatarCameraActive(bool active)
        {
            if (_avatarController?.avatarCamera == null)
            {
                Debug.LogWarning($"[{GetType().Name}] AvatarCamera not available");
                return;
            }

            _avatarController.avatarCamera.gameObject.SetActive(active);
        }

        public void SetFullBodyCameraActive(bool active)
        {
            if (_avatarController?.fullBodyCamera == null)
            {
                Debug.LogWarning($"[{GetType().Name}] FullBodyCamera not available");
                return;
            }

            _avatarController.fullBodyCamera.gameObject.SetActive(active);
        }

        public void SaveCurrentConfig()
        {
            SaveCurrentConfigAsync(true).ConfigureAwait(false);
        }

        public async Task SaveCurrentConfigAsync(bool hasAvatar = true)
        {
            if (_currentConfig == null && hasAvatar)
            {
                Debug.LogError($"[{GetType().Name}] No config to save");
                return;
            }

            if (hasAvatar && _currentConfig != null)
            {
                string json = JsonUtility.ToJson(_currentConfig);
                PlayerPrefs.SetString(PLAYER_PREFS_KEY, json);
                PlayerPrefs.SetInt(HAS_AVATAR_PREFS_KEY, 1);
            }
            else
            {
                PlayerPrefs.SetInt(HAS_AVATAR_PREFS_KEY, 0);
            }
            PlayerPrefs.Save();

            var store = _storeService ?? App.current?.services?.GetService<IStoreService>();
            if (store != null)
            {
                store.store.Dispatch(AppActions.setAvatar.Invoke(new AppActions.AvatarPayload(_currentConfig, hasAvatar)));
            }

            var auth = _authService ?? App.current?.services?.GetService<IAuthService>();
            if (auth != null)
            {
                AppState state = store?.GetAppState();
                var request = new ProfileUpdateRequest
                {
                    preferences = new ProfileUpdatePreferences
                    {
                        shoppingResponsibility = !string.IsNullOrEmpty(state?.userShoppingResponsibility) ? state.userShoppingResponsibility : null,
                        dietaryPreference = state?.userDietaryPreference != null && state.userDietaryPreference.Length > 0 ? state.userDietaryPreference : null,
                        onboardingSurvey = state?.userOnboardingSurvey,
                        autoAddToPantry = state?.userAutoAddToPantry ?? false,
                        avatarConfig = hasAvatar ? _currentConfig : null,
                        hasAvatar = hasAvatar
                    }
                };

                var (success, error) = await auth.UpdateProfileAsync(request);
                if (success)
                {
                    Debug.Log($"[{GetType().Name}] Avatar preferences synced with server profile (hasAvatar={hasAvatar})");
                }
                else
                {
                    Debug.LogWarning($"[{GetType().Name}] Failed to sync avatar preferences with server: {error?.message}");
                }
            }
        }

        public async Task SetHasAvatarAsync(bool hasAvatar)
        {
            if (!hasAvatar)
            {
                PlayerPrefs.SetInt(HAS_AVATAR_PREFS_KEY, 0);
                PlayerPrefs.Save();
            }

            await SaveCurrentConfigAsync(hasAvatar);
        }

        public void LoadSavedConfig()
        {
            var store = _storeService ?? App.current?.services?.GetService<IStoreService>();
            AppState state = store?.GetAppState();
            if (state != null && state.userHasAvatar && state.userAvatarConfig != null)
            {
                SetAvatarConfig(state.userAvatarConfig);
                Debug.Log($"[{GetType().Name}] Avatar config loaded from AppState Redux store");
                return;
            }

            if (!PlayerPrefs.HasKey(PLAYER_PREFS_KEY))
            {
                Debug.Log($"[{GetType().Name}] No saved config found");
                return;
            }

            string json = PlayerPrefs.GetString(PLAYER_PREFS_KEY);
            AvatarConfig savedConfig = JsonUtility.FromJson<AvatarConfig>(json);
            if (savedConfig != null)
            {
                SetAvatarConfig(savedConfig);
                Debug.Log($"[{GetType().Name}] Avatar config loaded from PlayerPrefs");
            }
        }
    }
}
