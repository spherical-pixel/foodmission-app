using System;
using Unity.AppUI.MVVM;
using UnityEngine;

namespace eu.foodmission.platform
{
    /// <summary>
    /// Implementation of IKeyboardService for detecting on-screen keyboard on mobile devices.
    /// Uses TouchScreenKeyboard API for iOS and native Android Java interop for Android.
    /// </summary>
    public class KeyboardService : IKeyboardService
    {
        private bool _isInitialized;
        private float _currentKeyboardHeight;
        private bool _wasKeyboardVisible;
        private bool _keyboardWasShown; // Track if we've fired the shown event

        /// <inheritdoc/>
        public float KeyboardHeight { get; private set; }

        /// <inheritdoc/>
        public bool IsKeyboardVisible { get; private set; }

        /// <inheritdoc/>
        public event Action<float> KeyboardShown;

        /// <inheritdoc/>
        public event Action KeyboardHidden;

        /// <inheritdoc/>
        public void Initialize()
        {
            if (_isInitialized)
            {
                return;
            }

            _isInitialized = true;
            KeyboardHeight = 0f;
            IsKeyboardVisible = false;
            _wasKeyboardVisible = false;

            // Subscribe to the update loop to check keyboard status
            App.shuttingDown += OnAppShuttingDown;
        }

        private void OnAppShuttingDown()
        {
            Dispose();
        }

        /// <summary>
        /// Update method that should be called every frame to check keyboard status.
        /// In a real implementation, this would be hooked into Unity's Update loop
        /// or called from a MonoBehaviour Update.
        /// </summary>
        public void Update()
        {
            if (!_isInitialized)
            {
                return;
            }

            UpdateKeyboardStatus();
        }

        /// <summary>
        /// Checks the current keyboard status and fires events if changed.
        /// </summary>
        private void UpdateKeyboardStatus()
        {
            bool isVisible = TouchScreenKeyboard.visible;
            float height = GetPlatformKeyboardHeight();

            // Check for state changes
            if (isVisible && !_wasKeyboardVisible)
            {
                // Keyboard just shown
                IsKeyboardVisible = true;
                KeyboardHeight = height;
                _currentKeyboardHeight = height;
                _keyboardWasShown = true;
                KeyboardShown?.Invoke(height);
            }
            else if (!isVisible && _wasKeyboardVisible)
            {
                // Keyboard just hidden
                IsKeyboardVisible = false;
                KeyboardHeight = 0f;
                _currentKeyboardHeight = 0f;
                _keyboardWasShown = false;
                KeyboardHidden?.Invoke();
            }
            else if (isVisible && _keyboardWasShown && height > 0 && _currentKeyboardHeight == 0)
            {
                // Keyboard was shown with 0 height, now we have the real height
                // This happens on iOS where the height is reported after the keyboard animation starts
                _currentKeyboardHeight = height;
                KeyboardHeight = height;
                // Re-fire the event with the correct height
                KeyboardShown?.Invoke(height);
            }
            else if (isVisible && Math.Abs(height - _currentKeyboardHeight) > 1f)
            {
                // Keyboard height changed (e.g., emoji keyboard vs regular)
                _currentKeyboardHeight = height;
                KeyboardHeight = height;
            }

            _wasKeyboardVisible = isVisible;
        }

        /// <summary>
        /// Gets the keyboard height using platform-specific methods.
        /// </summary>
        private float GetPlatformKeyboardHeight()
        {
#if UNITY_EDITOR
            return TouchScreenKeyboard.visible ? 400f : 0f;
#else
            try
            {
                if (!TouchScreenKeyboard.visible)
                {
                    return 0f;
                }

                float areaHeight = TouchScreenKeyboard.area.height;

                // On Android with IME keyboards (GBoard etc.), area.height often returns 0
                // even when the keyboard is visible. Fall back to estimation.
                if (areaHeight <= 0f)
                {
                    return EstimateKeyboardHeight();
                }

                return areaHeight;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[KeyboardService] Error getting keyboard height: {ex.Message}");
                return 0f;
            }
#endif
        }

        /// <summary>
        /// Estimates keyboard height as a percentage of screen when the platform API
        /// fails to report actual height (common with IME keyboards on Android).
        /// </summary>
        private static float EstimateKeyboardHeight()
        {
            float screenHeight = Screen.safeArea.height > 0 ? Screen.safeArea.height : Screen.height;
            float screenWidth = Screen.width;

            // Portrait: ~45%, Landscape: ~35%
            return screenHeight > screenWidth
                ? screenHeight * 0.45f
                : screenHeight * 0.35f;
        }

        /// <inheritdoc/>
        public float GetKeyboardHeightRatio()
        {
            float height = KeyboardHeight;
            if (height <= 0f)
            {
                return 0f;
            }

            // Use Screen.safeArea.height on mobile for accurate ratio
            float screenHeight = Screen.safeArea.height > 0 ? Screen.safeArea.height : Screen.height;

            if (screenHeight <= 0)
            {
                return 0f;
            }

            return height / screenHeight;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            App.shuttingDown -= OnAppShuttingDown;

            KeyboardShown = null;
            KeyboardHidden = null;

            _isInitialized = false;
        }
    }
}
