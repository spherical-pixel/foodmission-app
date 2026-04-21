using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace eu.foodmission.platform
{
    public class NutriService : INutriService
    {
        private const string PREFAB_ADDRESS = "Assets/Foodmission/prefabs/NutriController.prefab";

        private GameObject _nutriController;
        private Animator _animator;
        private NutriMood _currentMood = NutriMood.Neutral;
        private Camera _nutriCamera;
        private bool _isInitialized;

        public RenderTexture NutriCameraRenderTexture => _nutriCamera != null ? _nutriCamera.targetTexture : null;

        private static readonly Dictionary<NutriMood, string> MoodToAnimation = new()
        {
            { NutriMood.Neutral, "idle_neutral" },
            { NutriMood.Happy, "idle_happy" },
            { NutriMood.VeryHappy, "idle_very_happy" },
            { NutriMood.Bored, "idle_bored" },
            { NutriMood.Sick, "idle_sick" },
            { NutriMood.Dirty, "idle_dirty" },
            { NutriMood.Talking, "talking" },
            { NutriMood.Celebration, "celebration" },
            { NutriMood.Greeting, "greeting" },
            { NutriMood.LookingDown, "looking_down" }
        };

        public NutriMood CurrentMood => _currentMood;
        public bool IsInitialized => _isInitialized;

        public async Task InitializeAsync()
        {
            if (_isInitialized)
            {
                Debug.LogWarning($"[{GetType().Name}] Already initialized");
                return;
            }

            // Reuse existing instance if present (e.g. after USS hot-reload recreates the DI container)
            var existing = GameObject.Find("NutriController");
            if (existing != null)
            {
                _nutriController = existing;
                Transform existingNutri = _nutriController.transform.Find("nutri");
                if (existingNutri != null)
                {
                    _animator = existingNutri.GetComponent<Animator>();
                }

                Transform nutriCameraObj = _nutriController.transform.Find("CameraNutri");
                if (nutriCameraObj != null)
                {
                    _nutriCamera = nutriCameraObj.GetComponent<Camera>();
                }

                if (_animator != null || _nutriCamera != null)
                {
                    _isInitialized = true;
                    Debug.Log($"[{GetType().Name}] Reused existing NutriController after hot-reload");
                }

                
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

                _nutriController = UnityEngine.Object.Instantiate(prefab);
                _nutriController.name = "NutriController";


                Transform nutriObj = _nutriController.transform.Find("nutri");
                if (nutriObj != null)
                {
                    _animator = nutriObj.GetComponent<Animator>();
                }

                Transform nutriCameraObj = _nutriController.transform.Find("CameraNutri");
                if (nutriCameraObj != null)
                {
                    _nutriCamera = nutriCameraObj.GetComponent<Camera>();
                }

                if (_animator == null || _nutriCamera == null)
                {
                    Debug.LogError($"[{GetType().Name}] Required components not found on nutri object");
                    return;
                }

                _isInitialized = true;
                Debug.Log($"[{GetType().Name}] Nutri initialized successfully");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] Error loading Nutri: {ex.Message}");
            }
        }

        public void SetActive(bool active)
        {
            if (_nutriController == null)
            {
                Debug.LogWarning($"[{GetType().Name}] NutriController not initialized");
                return;
            }

            _nutriController.SetActive(active);
        }

        public void SetMood(NutriMood mood)
        {
            if (_animator == null)
            {
                Debug.LogWarning($"[{GetType().Name}] Animator not available");
                return;
            }

            if (!MoodToAnimation.TryGetValue(mood, out var animationName))
            {
                Debug.LogWarning($"[{GetType().Name}] No animation mapping for mood: {mood}");
                return;
            }

            _currentMood = mood;
            _animator.Play(animationName);
            Debug.Log($"[{GetType().Name}] Set mood to {mood} ({animationName})");
        }
    }
}