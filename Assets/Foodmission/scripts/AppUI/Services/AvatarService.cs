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
        private const string FACE_TEXTURE_FILENAME = "avatar_face.png";

        private GameObject _avatarControllerObject;
        private AvatarController _avatarController;

        private bool _isInitialized;
        private AvatarConfig _currentConfig = null;
        private Texture2D _cachedFaceTexture = null;

        public event System.Action OnFaceTextureChanged;

        private string FaceTexturePath => System.IO.Path.Combine(Application.persistentDataPath, FACE_TEXTURE_FILENAME);

        private readonly IStoreService _storeService;
        private readonly IAuthService _authService;

        public AvatarService(IStoreService storeService = null, IAuthService authService = null)
        {
            _storeService = storeService;
            _authService = authService;

            var activeStore = _storeService ?? App.current?.services?.GetService<IStoreService>();
            if (activeStore?.store != null)
            {
                activeStore.store.Subscribe(state => state, OnAppStateChanged);
            }
        }

        private void OnAppStateChanged(AppState state)
        {
            if (state == null) return;

            if (state.userHasAvatar && state.userAvatarConfig != null)
            {
                if (_currentConfig != state.userAvatarConfig)
                {
                    SetAvatarConfig(state.userAvatarConfig);
                }

                string path = FaceTexturePath;
                if (!System.IO.File.Exists(path) && _cachedFaceTexture == null)
                {
                    _ = EnsureFaceTextureAsync();
                }
            }
        }

        public RenderTexture AvatarCameraRenderTexture => _avatarController?.avatarCamera != null ? _avatarController.avatarCamera.targetTexture : null;
        public RenderTexture FullBodyAvatarRenderTexture => _avatarController?.fullBodyCamera != null ? _avatarController.fullBodyCamera.targetTexture : null;
        public AvatarConfig GetCurrentAvatarConfig => _currentConfig;

        public AvatarController AvatarController => _avatarController;

        public bool IsInitialized => _isInitialized;
        public bool HasSavedConfig => HasAvatar && (_currentConfig != null || PlayerPrefs.HasKey(PLAYER_PREFS_KEY));

        public bool HasAvatar
        {
            get
            {
                var storeService = _storeService ?? App.current?.services?.GetService<IStoreService>();
                AppState state = storeService?.GetAppState();
                if (state != null)
                {
                    return state.userHasAvatar;
                }
                return PlayerPrefs.GetInt(HAS_AVATAR_PREFS_KEY, 0) == 1;
            }
        }

        private const string DEFAULT_AVATAR_ADDRESS = "Assets/Foodmission/graphics/png/default-avatar.png";
        private static Texture2D s_DefaultAvatarTexture;

        public static Texture2D GetDefaultAvatarTexture()
        {
            if (s_DefaultAvatarTexture != null)
            {
                return s_DefaultAvatarTexture;
            }

            try
            {
                var handle = Addressables.LoadAssetAsync<Texture2D>(DEFAULT_AVATAR_ADDRESS);
                s_DefaultAvatarTexture = handle.WaitForCompletion();
                return s_DefaultAvatarTexture;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[AvatarService] Error loading default avatar from Addressables ({DEFAULT_AVATAR_ADDRESS}): {ex.Message}");
            }

            return null;
        }

        public Texture2D GetFaceTexture(bool allowFallback = false)
        {
            if (!HasAvatar)
            {
                return allowFallback ? GetDefaultAvatarTexture() : null;
            }

            if (_cachedFaceTexture != null)
            {
                return _cachedFaceTexture;
            }

            string path = FaceTexturePath;
            if (System.IO.File.Exists(path))
            {
                try
                {
                    byte[] bytes = System.IO.File.ReadAllBytes(path);
                    Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (ImageConversion.LoadImage(tex, bytes))
                    {
                        _cachedFaceTexture = tex;
                        return _cachedFaceTexture;
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[{GetType().Name}] Error loading face texture: {ex.Message}");
                }
            }

            return allowFallback ? GetDefaultAvatarTexture() : null;
        }

        public async Task<Texture2D> EnsureFaceTextureAsync()
        {
            if (!HasAvatar)
            {
                return GetDefaultAvatarTexture();
            }

            Texture2D tex = GetFaceTexture();
            if (tex != null && tex != GetDefaultAvatarTexture())
            {
                return tex;
            }

            LoadSavedConfig();
            await CaptureAndSaveFaceTextureAsync();
            return GetFaceTexture();
        }

        public async Task CaptureAndSaveFaceTextureAsync()
        {
            if (!_isInitialized)
            {
                await InitializeAsync();
            }

            if (_avatarController == null || _avatarController.avatarCamera == null)
            {
                Debug.LogWarning($"[{GetType().Name}] Cannot capture face texture — AvatarController or camera not ready");
                return;
            }

            try
            {
                // Small delay to allow animator, bones, and transforms to settle after instantiation/initialization
                await Task.Delay(150);

                if (_avatarController == null || _avatarController.avatarCamera == null)
                {
                    return;
                }

                if (_avatarController.AvatarAnimationController != null)
                {
                    _avatarController.AvatarAnimationController.UpdateAnimationController();
                }

                Camera cam = _avatarController.avatarCamera;
                RenderTexture previousActive = RenderTexture.active;

                RenderTexture rt = cam.targetTexture;
                bool createdRT = false;
                if (rt == null)
                {
                    rt = RenderTexture.GetTemporary(256, 256, 24, RenderTextureFormat.ARGB32);
                    cam.targetTexture = rt;
                    createdRT = true;
                }

                bool wasCamActive = cam.gameObject.activeSelf;
                cam.gameObject.SetActive(true);
                cam.Render();

                RenderTexture.active = rt;
                Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
                tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                tex.Apply();

                RenderTexture.active = previousActive;
                if (!wasCamActive)
                {
                    cam.gameObject.SetActive(false);
                }

                if (createdRT)
                {
                    cam.targetTexture = null;
                    RenderTexture.ReleaseTemporary(rt);
                }

                byte[] pngBytes = tex.EncodeToPNG();
                System.IO.File.WriteAllBytes(FaceTexturePath, pngBytes);
                if (_cachedFaceTexture != null)
                {
                    SafeDestroy(_cachedFaceTexture);
                }
                _cachedFaceTexture = tex;

                Debug.Log($"[{GetType().Name}] Face texture captured and saved to {FaceTexturePath}");
                OnFaceTextureChanged?.Invoke();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] Error capturing face texture: {ex.Message}");
            }
        }

        private static void SafeDestroy(UnityEngine.Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(obj);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(obj);
            }
        }

        public void ClearFaceTexture()
        {
            try
            {
                string path = FaceTexturePath;
                if (System.IO.File.Exists(path))
                {
                    System.IO.File.Delete(path);
                    Debug.Log($"[{GetType().Name}] Deleted avatar render file: {path}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[{GetType().Name}] Error clearing avatar face textures: {ex.Message}");
            }

            _currentConfig = null;
            PlayerPrefs.DeleteKey(PLAYER_PREFS_KEY);
            PlayerPrefs.SetInt(HAS_AVATAR_PREFS_KEY, 0);
            PlayerPrefs.Save();

            if (_cachedFaceTexture != null)
            {
                SafeDestroy(_cachedFaceTexture);
                _cachedFaceTexture = null;
            }

            OnFaceTextureChanged?.Invoke();
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
                LoadSavedConfig();
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
            if (_avatarControllerObject != null && active && !_avatarControllerObject.activeSelf)
            {
                _avatarControllerObject.SetActive(true);
            }

            if (_avatarController?.avatarCamera == null)
            {
                if (active)
                {
                    Debug.LogWarning($"[{GetType().Name}] AvatarCamera not available");
                }
                return;
            }

            _avatarController.avatarCamera.gameObject.SetActive(active);
        }

        public void SetFullBodyCameraActive(bool active)
        {
            if (_avatarControllerObject != null && active && !_avatarControllerObject.activeSelf)
            {
                _avatarControllerObject.SetActive(true);
            }

            if (_avatarController?.fullBodyCamera == null)
            {
                if (active)
                {
                    Debug.LogWarning($"[{GetType().Name}] FullBodyCamera not available");
                }
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

            if (hasAvatar)
            {
                await CaptureAndSaveFaceTextureAsync();
            }
            else
            {
                ClearFaceTexture();
            }

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
                        onboardingSurvey = state?.userOnboardingSurvey != null && state.userOnboardingSurvey.HasAnswers() ? state.userOnboardingSurvey : null,
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

        public AvatarConfig GetDefaultConfig()
        {
            return AvatarConfig.CreateDefault();
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

            if (HasAvatar && PlayerPrefs.HasKey(PLAYER_PREFS_KEY))
            {
                string json = PlayerPrefs.GetString(PLAYER_PREFS_KEY);
                AvatarConfig savedConfig = JsonUtility.FromJson<AvatarConfig>(json);
                if (savedConfig != null)
                {
                    SetAvatarConfig(savedConfig);
                    Debug.Log($"[{GetType().Name}] Avatar config loaded from PlayerPrefs");
                    return;
                }
            }

            // Fallback: apply deterministic standard default avatar
            SetAvatarConfig(GetDefaultConfig());
            Debug.Log($"[{GetType().Name}] Applied standard default avatar config");
        }
    }
}
