using UnityEngine;

namespace eu.foodmission.platform
{
    /// <summary>
    /// MonoBehaviour helper that updates the KeyboardService every frame.
    /// Attach this to a GameObject in the scene (e.g., the same GameObject as FoodmissionAppBuilder).
    /// </summary>
    public class KeyboardServiceUpdater : MonoBehaviour
    {
        private KeyboardService _keyboardService;
        private bool _isInitialized;

        /// <summary>
        /// Initializes the updater with the KeyboardService instance.
        /// </summary>
        public void Initialize(KeyboardService keyboardService)
        {
            _keyboardService = keyboardService;
            _isInitialized = true;
        }

        private void Update()
        {
            if (_isInitialized && _keyboardService != null)
            {
                _keyboardService.Update();
            }
        }

        private void OnDestroy()
        {
            _isInitialized = false;
            _keyboardService = null;
        }
    }
}
