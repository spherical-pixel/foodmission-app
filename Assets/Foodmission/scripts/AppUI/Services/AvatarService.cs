using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace eu.foodmission.platform
{
    public class AvatarService : IAvatarService
    {
        private const string PREFAB_ADDRESS = "Assets/Foodmission/prefabs/AvatarController.prefab";
        private const string PLAYER_PREFS_KEY = "AvatarConfig";

        private GameObject _avatarControllerObject;
        private AvatarController _avatarController;

        private bool _isInitialized;

        private AvatarConfig _currentConfig = null;
        

        public RenderTexture AvatarCameraRenderTexture => _avatarController?.avatarCamera != null ? _avatarController.avatarCamera.targetTexture : null;
        public RenderTexture FullBodyAvatarRenderTexture => _avatarController?.fullBodyCamera != null ? _avatarController.fullBodyCamera.targetTexture : null;
        public AvatarConfig GetCurrentAvatarConfig => _currentConfig;
        

        public bool IsInitialized => _isInitialized;
        public bool HasSavedConfig => PlayerPrefs.HasKey(PLAYER_PREFS_KEY);


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

                if (_avatarController == null)                {
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
            _avatarController.ApplyAvatar(_currentConfig);
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

        public void SaveCurrentConfig()
        {
            if (_currentConfig == null)
            {
                Debug.LogError($"[{GetType().Name}] No config to save");
                return;
            }

            string json = JsonUtility.ToJson(_currentConfig);
            PlayerPrefs.SetString(PLAYER_PREFS_KEY, json);
            PlayerPrefs.Save();
            Debug.Log($"[{GetType().Name}] Avatar config saved");
        }

        public void LoadSavedConfig()
        {
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
                Debug.Log($"[{GetType().Name}] Avatar config loaded from saves");
            }
        }
    }
}
