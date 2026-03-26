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
            // In editor, return a simulated height if testing
            return TouchScreenKeyboard.visible ? 400f : 0f;
#elif UNITY_IOS
            // On iOS, TouchScreenKeyboard.area works reliably
            return TouchScreenKeyboard.area.height;
#elif UNITY_ANDROID
            // On Android, we need to use Java interop
            return GetAndroidKeyboardHeight();
#else
            return 0f;
#endif
        }

#if UNITY_ANDROID
        /// <summary>
        /// Gets the keyboard height on Android using Java interop.
        /// This uses the window visible display frame to calculate keyboard height.
        /// </summary>
        private float GetAndroidKeyboardHeight()
        {
            try
            {
                using (var unityPlayerClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                {
                    var unityPlayer = unityPlayerClass.GetStatic<AndroidJavaObject>("currentActivity")
                        .Get<AndroidJavaObject>("mUnityPlayer");

                    // Get the view
                    var view = unityPlayer.Call<AndroidJavaObject>("getView");

                    // Create a Rect to hold the visible display frame
                    using (var rect = new AndroidJavaObject("android.graphics.Rect"))
                    {
                        view.Call("getWindowVisibleDisplayFrame", rect);

                        // Calculate keyboard height
                        int screenHeight = Screen.height;
                        int visibleHeight = rect.Call<int>("height");
                        int keyboardHeight = screenHeight - visibleHeight;

                        // Get the mobile input field height if visible
                        if (!TouchScreenKeyboard.hideInput)
                        {
                            var dialog = unityPlayer.Get<AndroidJavaObject>("mSoftInputDialog");
                            if (dialog != null)
                            {
                                var editText = dialog.Get<AndroidJavaObject>("mInputField");
                                if (editText != null)
                                {
                                    int inputFieldHeight = editText.Call<int>("getHeight");
                                    keyboardHeight += inputFieldHeight;
                                }
                            }
                        }

                        return Mathf.Max(0, keyboardHeight);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[KeyboardService] Error getting Android keyboard height: {ex.Message}");
                return 0f;
            }
        }
#endif

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
