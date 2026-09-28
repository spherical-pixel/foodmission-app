using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Unity.AppUI.MVVM;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace eu.foodmission.platform
{
    public class NutriService : INutriService
    {
        private const string PREFAB_ADDRESS = "Assets/Foodmission/prefabs/NutriController.prefab";
        private const string PREFS_NUTRI_LOADOUT = "NutriLoadout";

        private NutriController _nutriController;

        public RenderTexture NutriCameraRenderTexture => _nutriController?.NutriCamera != null ? _nutriController.NutriCamera.targetTexture : null;

        public NutriMood CurrentMood => _nutriController?.NutriAnimationController != null ? _nutriController.NutriAnimationController.CurrentMood : NutriMood.Neutral;

        private bool _isInitialized;
        public bool IsInitialized => _isInitialized;

        private FoodyLoadout _currentLoadout;
        public FoodyLoadout CurrentLoadout => _currentLoadout;

        public async Task InitializeAsync()
        {
            if (_isInitialized)
            {
                Debug.LogWarning($"[{GetType().Name}] Already initialized");
                return;
            }

            EnsureLoadoutFromPrefs();

            // Reuse existing instance if present (e.g. after USS hot-reload recreates the DI container)
            var existing = GameObject.Find("NutriController")?.GetComponent<NutriController>();
            if (existing != null)
            {
                _nutriController = existing;
                _isInitialized = true;
                if (_currentLoadout != null)
                {
                    _nutriController.ApplyLoadout(_currentLoadout);
                }
                Debug.Log($"[{GetType().Name}] Reused existing NutriController after hot-reload");
                return;
            }

            Debug.Log($"[{GetType().Name}] Loading Nutri from Addressables: {PREFAB_ADDRESS}");

            try
            {
                AsyncOperationHandle<GameObject> handle = Addressables.LoadAssetAsync<GameObject>(PREFAB_ADDRESS);
                GameObject prefab = await handle.Task;

                if (handle.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Failed)
                {
                    Debug.LogError($"[{GetType().Name}] Failed to load from Addressables: {handle.OperationException?.Message}");
                    return;
                }

                if (prefab == null)
                {
                    Debug.LogError($"[{GetType().Name}] Failed to load prefab from Addressables");
                    return;
                }

                GameObject nutriGo = UnityEngine.Object.Instantiate(prefab);
                _nutriController = nutriGo.GetComponent<NutriController>();
                _nutriController.name = "NutriController";

                if (_currentLoadout != null)
                {
                    _nutriController.ApplyLoadout(_currentLoadout);
                }

                _isInitialized = true;
                Debug.Log($"[{GetType().Name}] Nutri initialized successfully");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] Error loading Nutri: {ex.Message}");
            }
        }

        private void EnsureLoadoutFromPrefs()
        {
            if (_currentLoadout == null && PlayerPrefs.HasKey(PREFS_NUTRI_LOADOUT))
            {
                try
                {
                    string json = PlayerPrefs.GetString(PREFS_NUTRI_LOADOUT);
                    if (!string.IsNullOrEmpty(json))
                    {
                        _currentLoadout = JsonConvert.DeserializeObject<FoodyLoadout>(json);
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[{GetType().Name}] Error reading loadout from PlayerPrefs: {ex.Message}");
                }
            }
        }

        public void SetActive(bool active)
        {
            if (_nutriController == null)
            {
                Debug.LogWarning($"[{GetType().Name}] NutriController not initialized");
                return;
            }

            _nutriController.gameObject.SetActive(active);
        }

        public void SetCameraActive(bool active)
        {
            if (_nutriController == null || _nutriController.NutriCamera == null)
            {
                Debug.LogWarning($"[{GetType().Name}] NutriCamera not available");
                return;
            }

            _nutriController.NutriCamera.gameObject.SetActive(active);
        }

        public void SetMood(NutriMood mood)
        {
            if (_nutriController?.NutriAnimationController == null)
            {
                Debug.LogWarning($"[{GetType().Name}] NutriAnimationController not available");
                return;
            }

            _nutriController.NutriAnimationController.CurrentMood = mood;
            Debug.Log($"[{GetType().Name}] Set mood to {mood}");
        }

        public void SetAction(NutriAction nutriAction)
        {
            if (_nutriController?.NutriAnimationController == null)
            {
                Debug.LogWarning($"[{GetType().Name}] NutriAnimationController not available");
                return;
            }

            _nutriController.NutriAnimationController.CurrentAction = nutriAction;
            Debug.Log($"[{GetType().Name}] Set action to {nutriAction}");
        }

        public void EquipItem(string type, int slot)
        {
            if (_currentLoadout == null) _currentLoadout = new FoodyLoadout();
            var item = new FoodyItem
            {
                type = FoodyItemType.Normalize(type),
                slot = slot,
                code = $"{FoodyItemType.Normalize(type)}_{slot}",
                equipped = true,
                owned = true
            };
            _currentLoadout.SetItemByType(type, item);
            _nutriController?.EquipItem(type, slot);
            SaveLoadoutToPrefs(_currentLoadout);
        }

        public void UnequipItem(string type)
        {
            _currentLoadout?.SetItemByType(type, null);
            _nutriController?.UnequipItem(type);
            SaveLoadoutToPrefs(_currentLoadout);
        }

        public void ApplyLoadout(FoodyLoadout loadout)
        {
            _currentLoadout = loadout;
            _nutriController?.ApplyLoadout(loadout);
            SaveLoadoutToPrefs(loadout);
        }

        private void SaveLoadoutToPrefs(FoodyLoadout loadout)
        {
            try
            {
                if (loadout != null)
                {
                    string json = JsonConvert.SerializeObject(loadout);
                    PlayerPrefs.SetString(PREFS_NUTRI_LOADOUT, json);
                }
                else
                {
                    PlayerPrefs.DeleteKey(PREFS_NUTRI_LOADOUT);
                }
                PlayerPrefs.Save();
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[{GetType().Name}] Error saving loadout to PlayerPrefs: {ex.Message}");
            }
        }

        public async Task SyncLoadoutAsync()
        {
            EnsureLoadoutFromPrefs();

            var foodyService = App.current?.services?.GetService<IFoodyService>();
            if (foodyService == null) return;

            var (loadout, error) = await foodyService.GetLoadoutAsync();
            if (error == null && loadout != null)
            {
                bool hasRemote = loadout.antennas != null || loadout.ears != null || loadout.glasses != null;
                bool hasLocal = _currentLoadout != null && (_currentLoadout.antennas != null || _currentLoadout.ears != null || _currentLoadout.glasses != null);

                if (hasRemote || !hasLocal)
                {
                    ApplyLoadout(loadout);
                }
            }
        }
    }
}